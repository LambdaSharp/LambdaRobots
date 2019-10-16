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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LambdaRobots.Protocol;

namespace LambdaRobots.Server {

    public interface IGameDependencyProvider {

        //--- Properties ---
        Game Game { get; }

        //--- Methods ---
        double NextRandomDouble();
        Task<LambdaRobotBuild> GetRobotBuild(LambdaRobot robot);
        Task<LambdaRobotAction> GetRobotAction(LambdaRobot robot);
    }

    public class GameDependencyProvider : IGameDependencyProvider {

        //--- Fields ---
        private Game _game;
        private Random _random;
        private readonly Func<LambdaRobot, Task<LambdaRobotBuild>> _getBuild;
        private readonly Func<LambdaRobot, Task<LambdaRobotAction>> _getAction;

        //--- Constructors ---
        public GameDependencyProvider(
            Game game,
            Random random,
            Func<LambdaRobot, Task<LambdaRobotBuild>> getBuild,
            Func<LambdaRobot, Task<LambdaRobotAction>> getAction
        ) {
            _game = game ?? throw new ArgumentNullException(nameof(game));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _getBuild = getBuild ?? throw new ArgumentNullException(nameof(getBuild));
            _getAction = getAction ?? throw new ArgumentNullException(nameof(getAction));
        }

        //--- Properties ---
        public Game Game => _game;

        //--- Methods ---
        public double NextRandomDouble() => _random.NextDouble();
        public Task<LambdaRobotBuild> GetRobotBuild(LambdaRobot robot) => _getBuild(robot);
        public Task<LambdaRobotAction> GetRobotAction(LambdaRobot robot) => _getAction(robot);
    }

    public class GameLogic {

        //--- Class Methods ---
        public static double Distance(double x1, double y1, double x2, double y2)
            => Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));

        public static double MinMax(double min, double value, double max)
            => Math.Max(min, Math.Min(max, value));

        public static double NormalizeAngle(double angle) {
            var result = angle % 360;
            return (result <= -180.0)
                ? (result + 360.0)
                : (result > 180.0)
                ? (result - 360.0)
                : result;
        }

        //--- Fields ---
        private IGameDependencyProvider _provider;

        //--- Constructors ---
        public GameLogic(IGameDependencyProvider provider) {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        //--- Properties ---
        public Game Game => _provider.Game;

        //--- Methods ---
        public async Task StartAsync(int robotCount) {

            // reset game state
            Game.TotalTurns = 0;
            Game.Missiles.Clear();
            Game.Messages.Clear();
            Game.Robots.Clear();
            for(var i = 0; i < robotCount; ++i) {
                Game.Robots.Add(new LambdaRobot {

                    // robot state
                    Index = i,
                    Id = $"{Game.Id}:R{i}",
                    Status = LambdaRobotStatus.Alive,
                    X = 0.0,
                    Y = 0.0,
                    Speed = 0.0,
                    Heading = 0.0,
                    Damage = 0.0,
                    ReloadCoolDown = 0.0,
                    TimeOfDeathGameTurn = -1,
                    TotalDamageDealt = 0.0,
                    TotalKills = 0,
                    TotalMissileFiredCount = 0,
                    TotalMissileHitCount = 0,
                    TotalTravelDistance = 0.0,
                    TotalCollisions = 0,

                    // action
                    TargetHeading = 0.0,
                    TargetSpeed = 0.0,

                    // robot characteristics
                    MaxSpeed = 100.0,
                    Acceleration = 10.0,
                    Deceleration = 20.0,
                    MaxTurnSpeed = 50.0,
                    RadarRange = 600.0,
                    RadarMaxResolution = 10.0,
                    MaxDamage = 100.0,
                    CollisionDamage = 2.0,
                    DirectHitDamage = 8.0,
                    NearHitDamage = 4.0,
                    FarHitDamage = 2.0,

                    // missile characteristics
                    MissileReloadCooldown = 2.0,
                    MissileVelocity = 150.0,
                    MissileRange = 700.0,
                    MissileDirectHitDamageBonus = 3.0,
                    MissileNearHitDamageBonus = 2.1,
                    MissileFarHitDamageBonus = 1.0
                });
            }

            // get configuration for all robots
            var messages = (await Task.WhenAll(Game.Robots.Select(async robot => {
                var config = await _provider.GetRobotBuild(robot);
                robot.Name = config?.Name ?? "???";
                if(config == null) {
                    robot.Status = LambdaRobotStatus.Dead;
                    robot.TimeOfDeathGameTurn = 0;
                    return $"{robot.Name} (R{robot.Index}) was disqualified due to failure to initialize";
                }

                // read robot configuration
                var disqualified = false;
                var buildPoints = 0;
                var buildDescription = new StringBuilder();

                // read radar configuration
                buildPoints += (int)config.Radar;
                buildDescription.Append($"{config.Radar} Radar");
                switch(config.Radar) {
                case LambdaRobotRadarType.UltraShortRange:
                    robot.RadarRange = 200.0;
                    robot.RadarMaxResolution = 45.0;
                    break;
                case LambdaRobotRadarType.ShortRange:
                    robot.RadarRange = 400.0;
                    robot.RadarMaxResolution = 20.0;
                    break;
                case LambdaRobotRadarType.MidRange:
                    robot.RadarRange = 600.0;
                    robot.RadarMaxResolution = 10.0;
                    break;
                case LambdaRobotRadarType.LongRange:
                    robot.RadarRange = 800.0;
                    robot.RadarMaxResolution = 8.0;
                    break;
                case LambdaRobotRadarType.UltraLongRange:
                    robot.RadarRange = 1000.0;
                    robot.RadarMaxResolution = 5.0;
                    break;
                default:
                    disqualified = true;
                    break;
                }

                // read engine configuration
                buildPoints += (int)config.Engine;
                buildDescription.Append($", {config.Engine} Engine");
                switch(config.Engine) {
                case LambdaRobotEngineType.Economy:
                    robot.MaxSpeed = 60.0;
                    robot.Acceleration = 7.0;
                    break;
                case LambdaRobotEngineType.Compact:
                    robot.MaxSpeed = 80.0;
                    robot.Acceleration = 8.0;
                    break;
                case LambdaRobotEngineType.Standard:
                    robot.MaxSpeed = 100.0;
                    robot.Acceleration = 10.0;
                    break;
                case LambdaRobotEngineType.Large:
                    robot.MaxSpeed = 120.0;
                    robot.Acceleration = 12.0;
                    break;
                case LambdaRobotEngineType.ExtraLarge:
                    robot.MaxSpeed = 140.0;
                    robot.Acceleration = 13.0;
                    break;
                default:
                    disqualified = true;
                    break;
                }

                // read armor configuration
                buildPoints += (int)config.Armor;
                buildDescription.Append($", {config.Armor} Armor");
                switch(config.Armor) {
                case LambdaRobotArmorType.UltraLight:
                    robot.DirectHitDamage = 50.0;
                    robot.NearHitDamage = 25.0;
                    robot.FarHitDamage = 12.0;
                    robot.CollisionDamage = 10.0;
                    robot.MaxSpeed += 35.0;
                    robot.Deceleration = 30.0;
                    break;
                case LambdaRobotArmorType.Light:
                    robot.DirectHitDamage = 16.0;
                    robot.NearHitDamage = 8.0;
                    robot.FarHitDamage = 4.0;
                    robot.CollisionDamage = 3.0;
                    robot.MaxSpeed += 25.0;
                    robot.Deceleration = 25.0;
                    break;
                case LambdaRobotArmorType.Medium:
                    robot.DirectHitDamage = 8.0;
                    robot.NearHitDamage = 4.0;
                    robot.FarHitDamage = 2.0;
                    robot.CollisionDamage = 2.0;
                    robot.MaxSpeed += 0.0;
                    robot.Deceleration = 20.0;
                    break;
                case LambdaRobotArmorType.Heavy:
                    robot.DirectHitDamage = 4.0;
                    robot.NearHitDamage = 2.0;
                    robot.FarHitDamage = 1.0;
                    robot.CollisionDamage = 1.0;
                    robot.MaxSpeed += -25.0;
                    robot.Deceleration = 15.0;
                    break;
                case LambdaRobotArmorType.UltraHeavy:
                    robot.DirectHitDamage = 2.0;
                    robot.NearHitDamage = 1.0;
                    robot.FarHitDamage = 0.0;
                    robot.CollisionDamage = 1.0;
                    robot.MaxSpeed += -45.0;
                    robot.Deceleration = 10.0;
                    break;
                default:
                    disqualified = true;
                    break;
                }

                // read missile configuration
                buildPoints += (int)config.Missile;
                buildDescription.Append($", {config.Missile} Missile");
                switch(config.Missile) {
                case Protocol.LambdaRobotMissileType.Dart:
                    robot.MissileRange = 1200.0;
                    robot.MissileVelocity = 250.0;
                    robot.MissileDirectHitDamageBonus = 0.0;
                    robot.MissileNearHitDamageBonus = 0.0;
                    robot.MissileFarHitDamageBonus = 0.0;
                    robot.MissileReloadCooldown = 0.0;
                    break;
                case Protocol.LambdaRobotMissileType.Arrow:
                    robot.MissileRange = 900.0;
                    robot.MissileVelocity = 200.0;
                    robot.MissileDirectHitDamageBonus = 1.0;
                    robot.MissileNearHitDamageBonus = 1.0;
                    robot.MissileFarHitDamageBonus = 0.0;
                    robot.MissileReloadCooldown = 1.0;
                    break;
                case Protocol.LambdaRobotMissileType.Javelin:
                    robot.MissileRange = 700.0;
                    robot.MissileVelocity = 150.0;
                    robot.MissileDirectHitDamageBonus = 3.0;
                    robot.MissileNearHitDamageBonus = 2.0;
                    robot.MissileFarHitDamageBonus = 1.0;
                    robot.MissileReloadCooldown = 2.0;
                    break;
                case Protocol.LambdaRobotMissileType.Cannon:
                    robot.MissileRange = 500.0;
                    robot.MissileVelocity = 100.0;
                    robot.MissileDirectHitDamageBonus = 6.0;
                    robot.MissileNearHitDamageBonus = 4.0;
                    robot.MissileFarHitDamageBonus = 2.0;
                    robot.MissileReloadCooldown = 3.0;
                    break;
                case Protocol.LambdaRobotMissileType.BFG:
                    robot.MissileRange = 350.0;
                    robot.MissileVelocity = 75.0;
                    robot.MissileDirectHitDamageBonus = 12.0;
                    robot.MissileNearHitDamageBonus = 8.0;
                    robot.MissileFarHitDamageBonus = 4.0;
                    robot.MissileReloadCooldown = 5.0;
                    break;
                default:
                    disqualified = true;
                    break;
                }

                // check if robot respected the max build points
                if(buildPoints > Game.MaxBuildPoints) {
                    disqualified = true;
                }

                // check if robot is disqualified due to bad build
                if(disqualified) {
                    robot.Status = LambdaRobotStatus.Dead;
                    robot.TimeOfDeathGameTurn = Game.TotalTurns;
                    return $"{robot.Name} (R{robot.Index}) was disqualified due to bad configuration ({buildDescription}: {buildPoints} points)";
                }
                return $"{robot.Name} (R{robot.Index}) has joined the battle ({buildDescription}: {buildPoints} points)";
            }))).ToList();
            foreach(var message in messages) {
                AddMessage(message);
            }

            // place robots on playfield
            var marginWidth = Game.BoardWidth * 0.1;
            var marginHeight = Game.BoardHeight * 0.1;
            var attempts = 0;
        again:
            if(attempts >= 100) {
                throw new ApplicationException($"unable to place all robots with minimum separation of {Game.MinRobotStartDistance:N2}");
            }

            // assign random locations to all robots
            foreach(var robot in Game.Robots.Where(robot => robot.Status == LambdaRobotStatus.Alive)) {
                robot.X = marginWidth + _provider.NextRandomDouble() * (Game.BoardWidth - 2.0 * marginWidth);
                robot.Y = marginHeight + _provider.NextRandomDouble() * (Game.BoardHeight - 2.0 * marginHeight);
            }

            // verify that none of the robots are too close to each other
            for(var i = 0; i < Game.Robots.Count; ++i) {
                for(var j = i + 1; j < Game.Robots.Count; ++j) {
                    if((Game.Robots[i].Status == LambdaRobotStatus.Alive) && (Game.Robots[j].Status == LambdaRobotStatus.Alive)) {
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

            // increment turn counter
            ++Game.TotalTurns;

            // invoke all robots to get their actions
            await Task.WhenAll(Game.Robots
                .Where(robot => robot.Status == LambdaRobotStatus.Alive)
                .Select(async robot => {
                    ApplyRobotAction(robot, await _provider.GetRobotAction(robot));
                    return true;
                })
            );

            // move robots
            foreach(var robot in Game.Robots.Where(robot => robot.Status == LambdaRobotStatus.Alive)) {
                MoveRobot(robot);
            }

            // update missile states
            foreach(var missile in Game.Missiles) {
                switch(missile.Status) {
                case MissileStatus.Flying:

                    // move flying missiles
                    MoveMissile(missile);
                    break;
                case MissileStatus.ExplodingDirect:

                    // missile is exploding; assess direct damage
                    AssessMissileDamage(missile);
                    missile.Status = MissileStatus.ExplodingNear;
                    break;
                case MissileStatus.ExplodingNear:

                    // missile is exploding; assess near damage
                    AssessMissileDamage(missile);
                    missile.Status = MissileStatus.ExplodingFar;
                    break;
                case MissileStatus.ExplodingFar:

                    // missile is exploding; assess far damage
                    AssessMissileDamage(missile);
                    missile.Status = MissileStatus.Destroyed;
                    break;
                default:
                    missile.Status = MissileStatus.Destroyed;
                    break;
                }
            }

            // remove destroyed missiles
            Game.Missiles.RemoveAll(missile => missile.Status == MissileStatus.Destroyed);

            // update game state
            var robotCount = Game.Robots.Count(robot => robot.Status == LambdaRobotStatus.Alive);
            if(robotCount == 0) {

                // no robots left
                AddMessage("All robots have perished. Game Over.");
                Game.Status = GameStatus.Finished;
                Game.Missiles.Clear();
            } else if((robotCount == 1) && (Game.Robots.Count > 1)) {

                // last robot standing of many
                AddMessage($"{Game.Robots.First(robot => robot.Status == LambdaRobotStatus.Alive).Name} is victorious! Game Over.");
                Game.Status = GameStatus.Finished;
                Game.Missiles.Clear();
            } else if(Game.TotalTurns >= Game.MaxTurns) {

                // game has reached its turn limit
                AddMessage($"Reached max turns. {robotCount:N0} robots are left. Game Over.");
                Game.Status = GameStatus.Finished;
                Game.Missiles.Clear();
            }
        }

        public LambdaRobot ScanRobots(LambdaRobot robot, double heading, double resolution) {
            LambdaRobot result = null;
            resolution = MinMax(0.01, resolution, robot.RadarMaxResolution);
            FindRobotsByDistance(robot.X, robot.Y, (other, distance) => {

                // skip ourselves
                if(other.Id == robot.Id) {
                    return true;
                }

                // compute relative position
                var deltaX = other.X - robot.X;
                var deltaY = other.Y - robot.Y;

                // check if other robot is beyond scan range
                if(distance > robot.RadarRange) {

                    // no need to enumerate more
                    return false;
                }

                // check if delta angle is within resolution limit
                var angle = Math.Atan2(deltaX, deltaY) * 180.0 / Math.PI;
                if(Math.Abs(NormalizeAngle(heading - angle)) <= resolution) {

                    // found a robot within range and resolution; stop enumerating
                    result = other;
                    return false;
                }

                // enumerate more
                return true;
            });
            return result;
        }

        private void ApplyRobotAction(LambdaRobot robot, LambdaRobotAction action) {

            // reduce reload time if any is active
            if(robot.ReloadCoolDown > 0) {
                robot.ReloadCoolDown = Math.Max(0.0, robot.ReloadCoolDown - Game.SecondsPerTurn);
            }

            // check if any actions need to be applied
            if(action == null) {

                // robot didn't respond with an action; consider it dead
                robot.Status = LambdaRobotStatus.Dead;
                robot.TimeOfDeathGameTurn = Game.TotalTurns;
                AddMessage($"{robot.Name} (R{robot.Index}) was disqualified by lack of action");
                return;
            }

            // update speed and heading
            robot.TargetSpeed = MinMax(0.0, action.Speed ?? robot.TargetSpeed, robot.MaxSpeed);
            robot.TargetHeading = NormalizeAngle(action.Heading ?? robot.TargetHeading);

            // fire missile if requested and possible
            if((action.FireMissileHeading.HasValue || action.FireMissileDistance.HasValue) && (robot.ReloadCoolDown == 0.0)) {

                // update robot state
                ++robot.TotalMissileFiredCount;
                robot.ReloadCoolDown = robot.MissileReloadCooldown;

                // add missile
                var missile = new LambdaRobotMissile {
                    Id = $"{robot.Id}:M{robot.TotalMissileFiredCount}",
                    RobotId = robot.Id,
                    Status = MissileStatus.Flying,
                    X = robot.X,
                    Y = robot.Y,
                    Speed = robot.MissileVelocity,
                    Heading = NormalizeAngle(action.FireMissileHeading ?? robot.Heading),
                    Range = MinMax(0.0, action.FireMissileDistance ?? robot.MissileRange, robot.MissileRange),
                    DirectHitDamageBonus = robot.MissileDirectHitDamageBonus,
                    NearHitDamageBonus = robot.MissileNearHitDamageBonus,
                    FarHitDamageBonus = robot.MissileFarHitDamageBonus
                };
                Game.Missiles.Add(missile);
            }
        }

        private void MoveMissile(LambdaRobotMissile missile) {
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
                missile.Status = MissileStatus.ExplodingDirect;
                missile.Speed = 0.0;
            }
        }

        private void AssessMissileDamage(LambdaRobotMissile missile) {
            FindRobotsByDistance(missile.X, missile.Y, (robot, distance) => {

                // compute damage dealt by missile
                double damage = 0.0;
                string damageType = null;
                switch(missile.Status) {
                case MissileStatus.ExplodingDirect:
                    if(distance <= Game.DirectHitRange) {
                        damage = robot.DirectHitDamage + missile.DirectHitDamageBonus;
                        damageType = "direct";
                    }
                    break;
                case MissileStatus.ExplodingNear:
                    if(distance <= Game.NearHitRange) {
                        damage = robot.NearHitDamage + missile.NearHitDamageBonus;
                        damageType = "near";
                    }
                    break;
                case MissileStatus.ExplodingFar:
                    if(distance <= Game.FarHitRange) {
                        damage = robot.FarHitDamage + missile.FarHitDamageBonus;
                        damageType = "far";
                    }
                    break;
                }

                // check if any damage was dealt
                if(damage == 0.0) {

                    // stop enumerating more robots since they will be further away
                    return false;
                }

                // record damage dealt
                var from = Game.Robots.FirstOrDefault(fromRobot => fromRobot.Id == missile.RobotId);
                if(from != null) {
                    from.TotalDamageDealt += damage;
                    ++from.TotalMissileHitCount;

                    // check if robot was killed
                    if(Damage(robot, damage)) {
                        ++from.TotalKills;

                        // check if robot inflicted damage to itself
                        if(robot.Id == from.Id) {
                            AddMessage($"{robot.Name} (R{robot.Index}) killed itself");
                        } else {
                            AddMessage($"{robot.Name} (R{robot.Index}) was killed by {from.Name}");
                        }
                    } else {

                        // check if robot inflicted damage to itself
                        if(robot.Id == from.Id) {
                            AddMessage($"{robot.Name} (R{robot.Index}) caused {damage:N0} {damageType} damage to itself");
                        } else {
                            AddMessage($"{robot.Name} (R{robot.Index}) received {damage:N0} {damageType} damage from {from.Name} (R{from.Index})");
                        }
                    }
                }
                return true;
            });
        }

        private void MoveRobot(LambdaRobot robot) {

            // compute new heading
            if(robot.Heading != robot.TargetHeading) {
                if(robot.Speed <= robot.MaxTurnSpeed) {
                    robot.Heading = robot.TargetHeading;
                } else {
                    robot.TargetSpeed = 0;
                    robot.Heading = robot.TargetHeading;
                    AddMessage($"{robot.Name} (R{robot.Index}) stopped by sudden turn");
                }
            }

            // compute new speed
            var oldSpeed = robot.Speed;
            if(robot.TargetSpeed > robot.Speed) {
                robot.Speed = Math.Min(robot.TargetSpeed, robot.Speed + robot.Acceleration * Game.SecondsPerTurn);
            } else if(robot.TargetSpeed < robot.Speed) {
                robot.Speed = Math.Max(robot.TargetSpeed, robot.Speed - robot.Deceleration * Game.SecondsPerTurn);
            }
            var effectiveSpeed = (robot.Speed + oldSpeed) / 2.0;

            // move robot
            bool collision;
            Move(
                robot.X,
                robot.Y,
                robot.TotalTravelDistance,
                effectiveSpeed,
                robot.Heading,
                double.MaxValue,
                out robot.X,
                out robot.Y,
                out robot.TotalTravelDistance,
                out collision
            );

            // check for collision with wall
            if(collision) {
                robot.Speed = 0.0;
                robot.TargetSpeed = 0.0;
                ++robot.TotalCollisions;
                if(Damage(robot, robot.CollisionDamage)) {
                    AddMessage($"{robot.Name} (R{robot.Index}) was destroyed by wall collision");
                    return;
                } else {
                    AddMessage($"{robot.Name} (R{robot.Index}) received {robot.CollisionDamage:N0} damage by wall collision");
                }
            }

            // check if robot collides with any other robot
            FindRobotsByDistance(robot.X, robot.Y, (other, distance) => {
                if((other.Id != robot.Id) && (distance < Game.CollisionRange)) {
                    robot.Speed = 0.0;
                    robot.TargetSpeed = 0.0;
                    ++robot.TotalCollisions;
                    if(Damage(robot, robot.CollisionDamage)) {
                        AddMessage($"{robot.Name} (R{robot.Index}) was destroyed by collision with {other.Name}");
                    } else {
                        AddMessage($"{robot.Name} (R{robot.Index}) was damaged {robot.CollisionDamage:N0} by collision with {other.Name} (R{other.Index})");
                    }
                }

                // keep looking for more collisions unless robot is dead or nearest robot is out of range
                return (robot.Status == LambdaRobotStatus.Alive) && (distance < Game.CollisionRange);
            });
        }

        private void AddMessage(string text) {
            Game.Messages.Add(new Message {
                GameTurn = Game.TotalTurns,
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

        private void FindRobotsByDistance(double x, double y, Func<LambdaRobot, double, bool> callback) {
            foreach(var robotWithDistance in Game.Robots
                .Where(robot => robot.Status == LambdaRobotStatus.Alive)
                .Select(robot => new {
                    Robot = robot,
                    Distance = Distance(robot.X, robot.Y, x, y)
                })
                .OrderBy(tuple => tuple.Distance)
                .ToList()
            ) {
                if(!callback(robotWithDistance.Robot, robotWithDistance.Distance)) {
                    break;
                }
            }
        }

        private bool Damage(LambdaRobot robot, double damage) {
            robot.Damage += damage;
            if(robot.Damage >= robot.MaxDamage) {
                robot.Damage = robot.MaxDamage;
                robot.Status = LambdaRobotStatus.Dead;
                robot.TimeOfDeathGameTurn = Game.TotalTurns;
                return true;
            }
            return false;
        }
    }
}
