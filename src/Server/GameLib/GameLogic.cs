/*
 * MIT License
 *
 * Copyright (c) 2019-2021 LambdaSharp
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
using LambdaRobots.Bot.Model;

namespace LambdaRobots.Server {

    public interface IGameDependencyProvider {

        //--- Methods ---
        float NextRandomFloat();
        Task<GetBuildResponse> GetRobotBuild(BotInfo robot);
        Task<GetActionResponse> GetRobotAction(BotInfo robot);
    }

    public class GameLogic {

        //--- Fields ---
        private IGameDependencyProvider _provider;

        //--- Constructors ---
        public GameLogic(Game game, IGameDependencyProvider provider) {
            Game = game ?? throw new ArgumentNullException(nameof(game));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        //--- Properties ---
        public Game Game { get; private set; }

        //--- Methods ---
        public async Task StartAsync(int robotCount) {

            // reset game state
            Game.CurrentGameTurn = 0;
            Game.Missiles.Clear();
            Game.Messages.Clear();
            Game.Robots.Clear();
            for(var i = 0; i < robotCount; ++i) {
                Game.Robots.Add(RobotBuild.GetDefaultBuild(Game.Id, i));
            }

            // get configuration for all robots
            var messages = (await Task.WhenAll(Game.Robots.Select(robot => InitializeRobotAsync(robot)))).ToList();
            foreach(var message in messages) {
                AddMessage(message);
            }

            // place robots on playfield
            var marginWidth = Game.BoardWidth * 0.1f;
            var marginHeight = Game.BoardHeight * 0.1f;
            var attempts = 0;
        again:
            if(attempts >= 100) {
                throw new ApplicationException($"unable to place all robots with minimum separation of {Game.MinRobotStartDistance:N2}");
            }

            // assign random locations to all robots
            foreach(var robot in Game.Robots.Where(robot => robot.Status == BotStatus.Alive)) {
                robot.X = marginWidth + _provider.NextRandomFloat() * (Game.BoardWidth - 2.0f * marginWidth);
                robot.Y = marginHeight + _provider.NextRandomFloat() * (Game.BoardHeight - 2.0f * marginHeight);
            }

            // verify that none of the robots are too close to each other
            for(var i = 0; i < Game.Robots.Count; ++i) {
                for(var j = i + 1; j < Game.Robots.Count; ++j) {
                    if((Game.Robots[i].Status == BotStatus.Alive) && (Game.Robots[j].Status == BotStatus.Alive)) {
                        var distance = GameMath.Distance(Game.Robots[i].X, Game.Robots[i].Y, Game.Robots[j].X, Game.Robots[j].Y);
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
            ++Game.CurrentGameTurn;

            // invoke all robots to get their actions
            await Task.WhenAll(Game.Robots
                .Where(robot => robot.Status == BotStatus.Alive)
                .Select(async robot => {
                    ApplyRobotAction(robot, await _provider.GetRobotAction(robot));
                    return true;
                })
            );

            // move robots
            foreach(var robot in Game.Robots.Where(robot => robot.Status == BotStatus.Alive)) {
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
            var robotCount = Game.Robots.Count(robot => robot.Status == BotStatus.Alive);
            if(robotCount == 0) {

                // no robots left
                AddMessage("All robots have perished. Game Over.");
                Game.Status = GameStatus.Finished;
                Game.Missiles.Clear();
            } else if(robotCount == 1) {

                // last robot standing
                AddMessage($"{Game.Robots.First(robot => robot.Status == BotStatus.Alive).Name} is victorious! Game Over.");
                Game.Status = GameStatus.Finished;
                Game.Missiles.Clear();
            } else if(Game.CurrentGameTurn >= Game.MaxTurns) {

                // game has reached its turn limit
                AddMessage($"Reached max turns. {robotCount:N0} robots are left. Game Over.");
                Game.Status = GameStatus.Finished;
                Game.Missiles.Clear();
            }
        }

        public BotInfo ScanRobots(BotInfo robot, float heading, float resolution) {
            BotInfo result = null;
            resolution = GameMath.MinMax(0.01f, resolution, robot.RadarMaxResolution);
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
                var angle = MathF.Atan2(deltaX, deltaY) * 180.0f / MathF.PI;
                if(MathF.Abs(GameMath.NormalizeAngle(heading - angle)) <= resolution) {

                    // found a robot within range and resolution; stop enumerating
                    result = other;
                    return false;
                }

                // enumerate more
                return true;
            });
            return result;
        }

        private async Task<string> InitializeRobotAsync(BotInfo robot) {
            var config = await _provider.GetRobotBuild(robot);
            robot.Name = config?.Name ?? $"#{robot.Index}";
            if(config is null) {

                // missing config information, consider robot dead
                robot.Status = BotStatus.Dead;
                robot.TimeOfDeathGameTurn = 0;
                return $"{robot.Name} (R{robot.Index}) was disqualified due to failure to initialize";
            }

            // read robot configuration
            var success = true;
            var buildPoints = 0;
            var buildDescription = new StringBuilder();

            // read radar configuration
            buildPoints += (int)config.Radar;
            buildDescription.Append($"{config.Radar} Radar");
            success &= RobotBuild.TrySetRadar(config.Radar, robot);

            // read engine configuration
            buildPoints += (int)config.Engine;
            buildDescription.Append($", {config.Engine} Engine");
            success &= RobotBuild.TrySetEngine(config.Engine, robot);

            // read armor configuration
            buildPoints += (int)config.Armor;
            buildDescription.Append($", {config.Armor} Armor");
            success &= RobotBuild.TrySetArmor(config.Armor, robot);

            // read missile configuration
            buildPoints += (int)config.Missile;
            buildDescription.Append($", {config.Missile} Missile");
            success &= RobotBuild.TrySetMissile(config.Missile, robot);

            // check if robot respected the max build points
            if(buildPoints > Game.MaxBuildPoints) {
                success = false;
            }

            // check if robot is disqualified due to a bad build
            if(!success) {
                robot.Status = BotStatus.Dead;
                robot.TimeOfDeathGameTurn = Game.CurrentGameTurn;
                return $"{robot.Name} (R{robot.Index}) was disqualified due to bad configuration ({buildDescription}: {buildPoints} points)";
            }
            robot.InternalState = config.InternalStartState;
            return $"{robot.Name} (R{robot.Index}) has joined the battle ({buildDescription}: {buildPoints} points)";
        }

        private void ApplyRobotAction(BotInfo robot, GetActionResponse action) {

            // reduce reload time if any is active
            if(robot.ReloadCoolDown > 0) {
                robot.ReloadCoolDown = MathF.Max(0.0f, robot.ReloadCoolDown - Game.SecondsPerTurn);
            }

            // check if any actions need to be applied
            if(action is null) {

                // robot didn't respond with an action; consider it dead
                robot.Status = BotStatus.Dead;
                robot.TimeOfDeathGameTurn = Game.CurrentGameTurn;
                AddMessage($"{robot.Name} (R{robot.Index}) was disqualified by lack of action");
                return;
            }

            // update robot state
            robot.InternalState = action.RobotState;

            // update speed and heading
            robot.TargetSpeed = GameMath.MinMax(0.0f, action.Speed ?? robot.TargetSpeed, robot.MaxSpeed);
            robot.TargetHeading = GameMath.NormalizeAngle(action.Heading ?? robot.TargetHeading);

            // fire missile if requested and possible
            if((action.FireMissileHeading.HasValue || action.FireMissileDistance.HasValue) && (robot.ReloadCoolDown == 0.0f)) {

                // update robot state
                ++robot.TotalMissileFiredCount;
                robot.ReloadCoolDown = robot.MissileReloadCooldown;

                // add missile
                var missile = new MissileInfo {
                    Id = $"{robot.Id}:M{robot.TotalMissileFiredCount}",
                    RobotId = robot.Id,
                    Status = MissileStatus.Flying,
                    X = robot.X,
                    Y = robot.Y,
                    Speed = robot.MissileVelocity,
                    Heading = GameMath.NormalizeAngle(action.FireMissileHeading ?? robot.Heading),
                    Range = GameMath.MinMax(0.0f, action.FireMissileDistance ?? robot.MissileRange, robot.MissileRange),
                    DirectHitDamageBonus = robot.MissileDirectHitDamageBonus,
                    NearHitDamageBonus = robot.MissileNearHitDamageBonus,
                    FarHitDamageBonus = robot.MissileFarHitDamageBonus
                };
                Game.Missiles.Add(missile);
            }
        }

        private void MoveMissile(MissileInfo missile) {
            bool collision;
            Move(
                missile.X,
                missile.Y,
                missile.Distance,
                missile.Speed,
                missile.Heading,
                missile.Range,
                out var missileX,
                out var missileY,
                out var missileDistance,
                out collision
            );
            missile.X = missileX;
            missile.Y = missileY;
            missile.Distance = missileDistance;
            if(collision) {
                missile.Status = MissileStatus.ExplodingDirect;
                missile.Speed = 0.0f;
            }
        }

        private void AssessMissileDamage(MissileInfo missile) {
            FindRobotsByDistance(missile.X, missile.Y, (robot, distance) => {

                // compute damage dealt by missile
                float damage = 0.0f;
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
                if(damage == 0.0f) {

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

        private void MoveRobot(BotInfo robot) {

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
                robot.Speed = MathF.Min(robot.TargetSpeed, robot.Speed + robot.Acceleration * Game.SecondsPerTurn);
            } else if(robot.TargetSpeed < robot.Speed) {
                robot.Speed = MathF.Max(robot.TargetSpeed, robot.Speed - robot.Deceleration * Game.SecondsPerTurn);
            }
            var effectiveSpeed = (robot.Speed + oldSpeed) / 2.0f;

            // move robot
            bool collision;
            Move(
                robot.X,
                robot.Y,
                robot.TotalTravelDistance,
                effectiveSpeed,
                robot.Heading,
                float.MaxValue,
                out var robotX,
                out var robotY,
                out var robotTotalTravelDistance,
                out collision
            );
            robot.X = robotX;
            robot.Y = robotY;
            robot.TotalTravelDistance = robotTotalTravelDistance;

            // check for collision with wall
            if(collision) {
                robot.Speed = 0.0f;
                robot.TargetSpeed = 0.0f;
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
                    robot.Speed = 0.0f;
                    robot.TargetSpeed = 0.0f;
                    ++robot.TotalCollisions;
                    if(Damage(robot, robot.CollisionDamage)) {
                        AddMessage($"{robot.Name} (R{robot.Index}) was destroyed by collision with {other.Name}");
                    } else {
                        AddMessage($"{robot.Name} (R{robot.Index}) was damaged {robot.CollisionDamage:N0} by collision with {other.Name} (R{other.Index})");
                    }
                }

                // keep looking for more collisions unless robot is dead or nearest robot is out of range
                return (robot.Status == BotStatus.Alive) && (distance < Game.CollisionRange);
            });
        }

        private void AddMessage(string text) {
            Game.Messages.Add(new Message {
                GameTurn = Game.CurrentGameTurn,
                Text = text
            });
        }

        private void Move(
            float startX,
            float startY,
            float startDistance,
            float speed,
            float heading,
            float range,
            out float endX,
            out float endY,
            out float endDistance,
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
            var sinHeading = MathF.Sin(heading * MathF.PI / 180.0f);
            var cosHeading = MathF.Cos(heading * MathF.PI / 180.0f);
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

        private void FindRobotsByDistance(float x, float y, Func<BotInfo, float, bool> callback) {
            foreach(var robotWithDistance in Game.Robots
                .Where(robot => robot.Status == BotStatus.Alive)
                .Select(robot => new {
                    Robot = robot,
                    Distance = GameMath.Distance(robot.X, robot.Y, x, y)
                })
                .OrderBy(tuple => tuple.Distance)
                .ToList()
            ) {
                if(!callback(robotWithDistance.Robot, robotWithDistance.Distance)) {
                    break;
                }
            }
        }

        private bool Damage(BotInfo robot, float damage) {
            robot.Damage += damage;
            if(robot.Damage >= robot.MaxDamage) {
                robot.Damage = robot.MaxDamage;
                robot.Status = BotStatus.Dead;
                robot.TimeOfDeathGameTurn = Game.CurrentGameTurn;
                return true;
            }
            return false;
        }
    }
}
