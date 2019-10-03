/*
 * MIT License
 *
 * Copyright (c) 2019 LambdaSharp
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Challenge.LambdaRobots.Common;

namespace Challenge.LambdaRobots.Server.Common {

    public interface IDependencyProvider {

        //--- Properties ---
        DateTime UtcNow { get; }
        Game Game { get; }

        //--- Methods ---
        double NextRandomDouble();
        Task<RobotConfig> GetRobotConfig(Robot robot);
        Task<RobotAction> GetRobotAction(Robot robot);
    }

    public class DependencyProvider : IDependencyProvider {

        //--- Fields ---
        private Game _game;
        private DateTime _utcNow;
        private Random _random;
        private readonly Func<Robot, Task<RobotConfig>> _getConfig;
        private readonly Func<Robot, Task<RobotAction>> _getAction;

        //--- Constructors ---
        public DependencyProvider(
            Game game,
            DateTime utcNow,
            Random random,
            Func<Robot, Task<RobotConfig>> getConfig,
            Func<Robot, Task<RobotAction>> getAction
        ) {
            _game = game ?? throw new ArgumentNullException(nameof(game));
            _utcNow = utcNow;
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _getConfig = getConfig ?? throw new ArgumentNullException(nameof(getConfig));
            _getAction = getAction ?? throw new ArgumentNullException(nameof(getAction));
        }

        //--- Properties ---
        public DateTime UtcNow => _utcNow;
        public Game Game => _game;

        //--- Methods ---
        public double NextRandomDouble() => _random.NextDouble();
        public Task<RobotConfig> GetRobotConfig(Robot robot) => _getConfig(robot);
        public Task<RobotAction> GetRobotAction(Robot robot) => _getAction(robot);
    }

    public class GameLogic {

        //--- Class Methods ---
        private static double Distance(double x1, double y1, double x2, double y2)
            => Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));

        private static double MinMax(double min, double value, double max)
            => Math.Max(min, Math.Min(max, value));

        private static double NormalizeAngle(double angle) {
            var result = angle % 360;
            return (result < -180.0)
                ? (result + 360.0)
                : result;
        }

        //--- Fields ---
        private IDependencyProvider _provider;

        //--- Constructors ---
        public GameLogic(IDependencyProvider provider) {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        //--- Properties ---
        public DateTime UtcNow => _provider.UtcNow;
        public Game Game => _provider.Game;

        //--- Methods ---
        public async Task StartAsync() {

            // reset game state
            Game.TotalTurns = 0;
            Game.Missiles.Clear();
            Game.Messages.Clear();
            foreach(var robot in Game.Robots) {
                robot.ReloadDelay = 0.0;
                robot.Speed = 0.0;
                robot.State = RobotState.Alive;
                robot.TargetHeading = 0.0;
                robot.TargetSpeed = 0.0;
                robot.TimeOfDeath = null;
                robot.TotalDamageDealt = 0.0;
                robot.TotalKills = 0;
                robot.TotalMissileFiredCount = 0;
                robot.TotalMissileHitCount = 0;
                robot.TotalScanCount = 0;
                robot.TotalTravelDistance = 0.0;
            }

            // get configuration for all robots
            await Task.WhenAll(Game.Robots.Select(async robot => {
                var config = await _provider.GetRobotConfig(robot);
                robot.Name = config?.Name ?? robot.Id;
                if(config == null) {
                    robot.State = RobotState.Dead;
                    AddMessage($"{robot.Name} was disqualified by failure to initialize");
                }
                return true;
            }));

            // place robots on playfield
            var marginWidth = Game.BoardWidth * 0.1;
            var marginHeight = Game.BoardHeight * 0.1;
            var attempts = 0;
        again:
            if(attempts >= 10) {
                throw new ApplicationException($"unable to place all robots with minimum separation of {Game.MinRobotStartDistance:N2}");
            }

            // assign random locations to all robots
            foreach(var robot in Game.Robots.Where(robot => robot.State == RobotState.Alive)) {
                robot.X = marginWidth + _provider.NextRandomDouble() * (Game.BoardWidth - 2.0 * marginWidth);
                robot.Y = marginHeight + _provider.NextRandomDouble() * (Game.BoardHeight - 2.0 * marginHeight);
            }

            // verify that none of the robots are too close to each other
            for(var i = 0; i < Game.Robots.Count; ++i) {
                for(var j = i + 1; j < Game.Robots.Count; ++j) {
                    if((Game.Robots[i].State == RobotState.Alive) && (Game.Robots[j].State == RobotState.Alive)) {
                        var distance = Distance(Game.Robots[i].X, Game.Robots[i].Y, Game.Robots[j].X, Game.Robots[j].Y);
                        if(distance < Game.MinRobotStartDistance) {
                            ++attempts;
                            goto again;
                        }
                    }
                }
            }
        }

        public async Task NextTurnAsync() {

            // invoke all robots to get their actions
            await Task.WhenAll(Game.Robots
                .Where(robot => robot.State == RobotState.Alive)
                .Select(async robot => {
                    ApplyRobotAction(robot, await _provider.GetRobotAction(robot));
                    return true;
                })
            );

            // move robots
            foreach(var robot in Game.Robots.Where(robot => robot.State == RobotState.Alive)) {
                MoveRobot(robot);
            }

            // update missile states
            foreach(var missile in Game.Missiles.Where(missile => missile.State == MissileState.Flying)) {
                MoveMissile(missile);

                // check if missile has hit something and came to a stop
                if(missile.State == MissileState.Exploding) {
                    AssessMissileDamage(missile);
                }
            }
            Game.Missiles.RemoveAll(missile => missile.State != MissileState.Flying);
        }

        public double? ScanRobots(Robot robot, double heading, double resolution) {
            double? result = null;
            resolution = MinMax(0.0, resolution, robot.ScannerResolution);
            FindRobotsByDistance(robot.X, robot.Y, (other, distance) => {

                // skip ourselves
                if(other.Id == robot.Id) {
                    return true;
                }

                // compute relative position
                var deltaX = other.X - robot.X;
                var deltaY = other.Y - robot.Y;

                // check if other robot is beyond scan range
                if(distance > robot.ScannerRange) {

                    // no need to enumerate more
                    return false;
                }

                // check if delta angle is within resolution limit
                var angle = Math.Atan2(deltaX, deltaY) * 180.0 / Math.PI;
                if(NormalizeAngle(Math.Abs(heading - angle)) <= resolution) {

                    // found a robot within range and resolution; stop enumerating
                    result = distance;
                    return false;
                }

                // enumerate more
                return true;
            });
            return result;
        }

        private void ApplyRobotAction(Robot robot, RobotAction action) {
            if(action == null) {

                // robot didn't respond with an action; consider it dead
                robot.State = RobotState.Dead;
                AddMessage($"{robot.Name} was disqualified by lack of action");
                return;
            }

            // update speed and heading
            robot.TargetSpeed = MinMax(0.0, action.Speed ?? robot.TargetSpeed, robot.MaxSpeed);
            robot.TargetHeading = NormalizeAngle(action.Heading ?? robot.TargetHeading);

            // fire missile if requested and possible
            if((action.FireMissileHeading.HasValue || action.FireMissileRange.HasValue) && (robot.ReloadDelay == 0.0)) {

                // update robot state
                ++robot.TotalMissileFiredCount;
                robot.ReloadDelay = robot.MissileReloadDelay;

                // add missile
                var missile = new RobotMissile {
                    Id = $"{robot.Id}:M{robot.TotalMissileFiredCount}",
                    RobotId = robot.Id,
                    State = MissileState.Flying,
                    X = robot.X,
                    Y = robot.Y,
                    Speed = robot.MissileSpeed,
                    Heading = NormalizeAngle(action.FireMissileHeading ?? robot.Heading),
                    Range = MinMax(0.0, action.FireMissileRange ?? robot.MissileRange, robot.MissileRange),
                    DirectHitDamageBonus = robot.MissileDirectHitDamageBonus,
                    NearHitDamageBonus = robot.MissileNearHitDamageBonus,
                    FarHitDamageBonus = robot.MissileFarHitDamageBonus
                };
                Game.Missiles.Add(missile);
                AddMessage($"{robot.Name} fired missile towards heading {missile.Heading:N0} with range {missile.Range:N0}");
            }
        }

        private void MoveMissile(RobotMissile missile) {
            bool collision;
            Move(
                missile.X,
                missile.Y,
                missile.Distance,
                missile.Speed,
                missile.Heading,
                missile.Range,
                out missile.X,
                out missile.Y,
                out missile.Distance,
                out collision
            );
            if(collision) {
                missile.State = MissileState.Exploding;
                missile.Speed = 0.0;
            }
        }

        private void AssessMissileDamage(RobotMissile missile) {
            FindRobotsByDistance(missile.X, missile.Y, (robot, distance) => {

                // compute damage dealt by missile
                double damage = 0.0;
                if(distance <= Game.DirectHitRange) {
                    damage = robot.DirectHitDamage + missile.DirectHitDamageBonus;
                } else if(distance <= Game.NearHitRange) {
                    damage = robot.NearHitDamage + missile.NearHitDamageBonus;
                } else if(distance <= Game.FarHitRange) {
                    damage = robot.FarHitDamage + missile.FarHitDamageBonus;
                }

                // check if any damage was dealt
                if(damage == 0.0) {

                    // stop enumerating more robots since they will be further away
                    return false;
                }

                // apply damage to target
                Damage(robot, damage);
                // record damage dealt
                var from = Game.Robots.FirstOrDefault(fromRobot => fromRobot.Id == missile.RobotId);
                if(from != null) {
                    from.TotalDamageDealt += damage;
                    ++from.TotalMissileHitCount;
                }

                // check if robot was killed
                if(robot.State == RobotState.Dead) {
                    if(from != null) {
                        ++from.TotalKills;
                    }
                    AddMessage($"{robot.Name} was killed by {from?.Name ?? "???"}");
                } else {
                    AddMessage($"{robot.Name} was damaged {damage:N0} by {from?.Name ?? "???"}");
                }
                return true;
            });
            missile.State = MissileState.Destroyed;
        }

        private void MoveRobot(Robot robot) {

            // reduce reload time if any is active
            if(robot.ReloadDelay > 0) {
                robot.ReloadDelay = Math.Max(0.0, robot.ReloadDelay - Game.SecondsPerTurn);
            }

            // compute new speed
            if(robot.TargetSpeed > robot.Speed) {
                robot.Speed = Math.Min(robot.TargetSpeed, robot.Speed + robot.Acceleration * Game.SecondsPerTurn);
            } else if(robot.TargetSpeed < robot.Speed) {
                robot.Speed = Math.Max(robot.TargetSpeed, robot.Speed + robot.Deceleration * Game.SecondsPerTurn);
            }

            // compute new heading
            if(robot.Heading != robot.TargetHeading) {
                if(robot.Speed <= robot.MaxTurnSpeed) {
                    robot.Heading = robot.TargetHeading;
                } else {
                    robot.TargetSpeed = 0;
                    AddMessage($"{robot.Name} stopped by sudden turn");
                }
            }

            // move robot
            bool collision;
            Move(
                robot.X,
                robot.Y,
                robot.TotalTravelDistance,
                robot.Speed,
                robot.Heading,
                double.MaxValue,
                out robot.X,
                out robot.Y,
                out robot.TotalTravelDistance,
                out collision
            );
            if(collision) {
                robot.Speed = 0.0;
                robot.TargetSpeed = 0.0;
                if(Damage(robot, robot.CollisionDamage)) {
                    AddMessage($"{robot.Name} was destroyed by wall collision");
                    return;
                } else {
                    AddMessage($"{robot.Name} was damaged {robot.CollisionDamage:N0} by wall collision");
                }
            }

            // check if robot collides with any other robot
            FindRobotsByDistance(robot.X, robot.Y, (other, distance) => {
                if((other.Id != robot.Id) && (distance < Game.CollisionRange)) {
                    robot.Speed = 0.0;
                    robot.TargetSpeed = 0.0;
                    if(Damage(robot, robot.CollisionDamage)) {
                        AddMessage($"{robot.Name} was destroyed by collision with {other.Name}");
                    } else {
                        AddMessage($"{robot.Name} was damaged {robot.CollisionDamage:N0} by collision with {other.Name}");
                    }
                }
                return (robot.State == RobotState.Alive) && (distance < Game.CollisionRange);
            });
        }

        private void AddMessage(string text) {
            Game.Messages.Add(new Message {
                Timestamp = UtcNow,
                Text = text
            });
        }

        private void Move(
            double startX,
            double startY,
            double startDistance,
            double speed,
            double heading,
            double range,
            out double endX,
            out double endY,
            out double endDistance,
            out bool collision
        ) {
            collision = false;

            // ensure object cannot move beyond its max range
            endDistance = startDistance + speed * Game.SecondsPerTurn;
            if(endDistance > range) {
                collision = true;
                endDistance = range;
            }

            // compute new position for object
            var delta = endDistance - startDistance;
            var sinHeading = Math.Sin(heading * Math.PI / 180.0);
            var cosHeading = Math.Cos(heading * Math.PI / 180.0);
            endX = startX + delta * sinHeading;
            endY = startY + delta * cosHeading;

            // ensure missile cannot fly past playfield boundaries
            if(endX < 0) {
                collision = true;
                delta = (0 - startX) / sinHeading;
                endX = startX + delta * sinHeading;
                endY = startY + delta * cosHeading;
                endDistance = startDistance + delta;
            } else if (endX > Game.BoardWidth) {
                collision = true;
                delta = (Game.BoardWidth - startX) / sinHeading;
                endX = startX + delta * sinHeading;
                endY = startY + delta * cosHeading;
                endDistance = startDistance + delta;
            }
            if(endY < 0) {
                collision = true;
                delta = (0 - startY) / cosHeading;
                endX = startX + delta * sinHeading;
                endY = startY + delta * cosHeading;
                endDistance = startDistance + delta;
            } else if(endY > Game.BoardHeight) {
                collision = true;
                delta = (Game.BoardHeight - startY) / cosHeading;
                endX = startX + delta * sinHeading;
                endY = startY + delta * cosHeading;
                endDistance = startDistance + delta;
            }
        }

        private void FindRobotsByDistance(double x, double y, Func<Robot, double, bool> callback) {
            foreach(var robotWithDistance in Game.Robots
                .Where(robot => robot.State == RobotState.Alive)
                .Select(robot => new {
                    Robot = robot,
                    Distance = Distance(robot.X, robot.Y, x, y)
                })
                .OrderBy(tuple => tuple.Distance)
            ) {
                if(!callback(robotWithDistance.Robot, robotWithDistance.Distance)) {
                    break;
                }
            }
        }

        private bool Damage(Robot robot, double damage) {
            robot.Damage += damage;
            if(robot.Damage >= robot.MaxDamage) {
                robot.Damage = robot.MaxDamage;
                robot.State = RobotState.Dead;
                robot.TimeOfDeath = UtcNow;
                return true;
            }
            return false;
        }
    }
}
