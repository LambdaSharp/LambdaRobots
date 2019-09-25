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

namespace Challenge.LambdaRobotsServer {

    public class Game {

        //--- Properties ---
        public double BoardWidth;
        public double BoardHeight;
        public double SecondsPerTurn;
        public double DirectHitRange;
        public double NearHitRange;
        public double FarHitRange;
        public List<RobotMissile> Missiles;
    }

    public enum RobotState {
        Undefined,
        Alive,
        Dead
    }

    public class Robot {

        //--- Properties ---
        public string Id;

        // current state
        public RobotState State;
        public double X;
        public double Y;
        public double Speed;
        public double Heading;
        public double Distance;
        public double Damage;
        public double Reload;
        public int MissileFiredCount;

        // target state
        public double TargetSpeed;
        public double TargetHeading;

        // robot characteristics
        public double MaxSpeed;
        public double Acceleration;
        public double Deceleration;
        public double MaxTurnSpeed;
        public double ScannerRange;
        public double MaxDamage;
        public double CollisionDamage;
        public double DirectHitDamage;
        public double NearHitDamage;
        public double FarHitDamage;

        // missile characteristics
        public double MissileReloadDelay;
        public double MissileSpeed;
        public double MissileRange;
        public double MissileDirectHitDamageBonus;
        public double MissileNearHitDamageBonus;
        public double MissileFarHitDamageBonus;
    }

    public enum MissileState {
        Undefined,
        Flying,
        Exploding,
        Destroyed
    }

    public class RobotMissile {

        //--- Properties ---
        public string Id;
        public string RobotId;

        // current state
        public MissileState State;
        public double X;
        public double Y;
        public double Speed;
        public double Heading;
        public double Distance;

        // missile characteristics
        public double Range;
        public double DirectHitDamageBonus;
        public double NearHitDamageBonus;
        public double FarHitDamageBonus;
    }

    public class RobotAction {

        //--- Properties ---
        public string RobotId;
        public double? Speed;
        public double? Heading;
        public double? FireMissileHeading;
        public double? FireMissileRange;
    }

    public class Logic {

        //--- Class Methods ---
        private static double Distance(double x1, double y1, double x2, double y2)
            => Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));

        //--- Fields ---
        private Game _game;
        private IEnumerable<Robot> _robots;

        //--- Constructors ---
        public Logic(Game game, IEnumerable<Robot> robots) {
            _game = game ?? throw new ArgumentNullException(nameof(game));
            _robots = robots ?? throw new ArgumentNullException(nameof(robots));
        }

        //--- Methods ---
        public void MainLoop(IEnumerable<RobotAction> actions) {

            // update robot states
            foreach(var robot in _robots.Where(robot => robot.State == RobotState.Alive)) {

                // apply action if present
                ApplyRobotAction(robot, actions.FirstOrDefault(a => a.RobotId == robot.Id));

                // move robot
                MoveRobot(robot);
            }

            // update missile states
            foreach(var missile in _game.Missiles.Where(missile => missile.State == MissileState.Flying)) {
                MoveMissile(missile);

                // check if missile has hit something and came to a stop
                if(missile.State == MissileState.Exploding) {
                    AssessMissileDamage(missile);
                }
            }
            _game.Missiles.RemoveAll(missile => missile.State != MissileState.Flying);
        }

        private void ApplyRobotAction(Robot robot, RobotAction action) {
            if(action == null) {
                return;
            }

            // update speed and heading
            robot.TargetSpeed = action.Speed ?? robot.TargetSpeed;
            robot.TargetHeading = action.Heading ?? robot.TargetHeading;

            // fire missile if requested and possible
            if((action.FireMissileHeading.HasValue || action.FireMissileRange.HasValue) && (robot.Reload == 0.0)) {

                // update robot state
                ++robot.MissileFiredCount;
                robot.Reload = robot.MissileReloadDelay;

                // add missile
                _game.Missiles.Add(new RobotMissile {
                    Id = $"{robot.Id}:{robot.MissileFiredCount}",
                    RobotId = robot.Id,
                    State = MissileState.Flying,
                    X = robot.X,
                    Y = robot.Y,
                    Speed = robot.MissileSpeed,
                    Heading = action.FireMissileHeading ?? robot.Heading,
                    Range = Math.Min(robot.MissileRange, action.FireMissileRange ?? robot.MissileRange),
                    DirectHitDamageBonus = robot.MissileDirectHitDamageBonus,
                    NearHitDamageBonus = robot.MissileNearHitDamageBonus,
                    FarHitDamageBonus = robot.MissileFarHitDamageBonus
                });
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
            RobotsByDistance(missile.X, missile.Y, (robot, distance) => {
                if(distance <= _game.DirectHitRange) {
                    robot.Damage += robot.DirectHitDamage + missile.DirectHitDamageBonus;
                } else if(distance <= _game.NearHitRange) {
                    robot.Damage += robot.NearHitDamage + missile.NearHitDamageBonus;
                } else if(distance <= _game.FarHitRange) {
                    robot.Damage += robot.FarHitDamage + missile.FarHitDamageBonus;
                }
                if(robot.Damage >= robot.MaxDamage) {
                    robot.Damage = robot.MaxDamage;
                    robot.State = RobotState.Dead;
                }
                return true;
            });
            missile.State = MissileState.Destroyed;
        }

        private void MoveRobot(Robot robot) {

            // reduce reload time if any is active
            if(robot.Reload > 0) {
                robot.Reload = Math.Max(0, robot.Reload - _game.SecondsPerTurn);
            }

            // compute new speed
            if(robot.TargetSpeed > robot.MaxSpeed) {
                robot.TargetSpeed = robot.MaxSpeed;
            } else if(robot.TargetSpeed < 0.0) {
                robot.TargetSpeed = 0.0;
            }
            if(robot.TargetSpeed > robot.Speed) {
                robot.Speed = Math.Min(robot.TargetSpeed, robot.Speed + robot.Acceleration * _game.SecondsPerTurn);
            } else if(robot.TargetSpeed < robot.Speed) {
                robot.Speed = Math.Max(robot.TargetSpeed, robot.Speed + robot.Deceleration * _game.SecondsPerTurn);
            }

            // compute new heading
            if(robot.Heading != robot.TargetHeading) {
                if(robot.Heading <= robot.MaxTurnSpeed) {
                    robot.Heading = robot.TargetHeading;
                    robot.Distance = 0.0;

                    // TODO: do we want to set a new start position?
                } else {
                    robot.TargetSpeed = 0;
                }
            }

            // move robot
            bool collision;
            Move(
                robot.X,
                robot.Y,
                robot.Distance,
                robot.Speed,
                robot.Heading,
                double.MaxValue,
                out robot.X,
                out robot.Y,
                out robot.Distance,
                out collision
            );
            if(collision) {
                robot.Speed = 0.0;
                robot.TargetSpeed = 0.0;
                robot.Damage += robot.CollisionDamage;
            }
            RobotsByDistance(robot.X, robot.Y, (other, distance) => {
                if((other.Id != robot.Id) && (distance < 2)) {

                    // damage robot that moved
                    robot.Speed = 0.0;
                    robot.TargetSpeed = 0.0;
                    robot.Damage += robot.CollisionDamage;
                    if(robot.Damage >= robot.MaxDamage) {
                        robot.Damage = robot.MaxDamage;
                        robot.State = RobotState.Dead;
                    }

                    // damage other robot as well
                    other.Speed = 0.0;
                    other.TargetSpeed = 0.0;
                    other.Damage += robot.CollisionDamage;
                    if(other.Damage >= robot.MaxDamage) {
                        other.Damage = robot.MaxDamage;
                        other.State = RobotState.Dead;
                    }
                }
                return robot.State == RobotState.Alive;
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
            endDistance = startDistance + speed * _game.SecondsPerTurn;
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
                endY = startY + delta * cosHeading;
                endDistance = startDistance + delta;
            } else if (endX > _game.BoardWidth) {
                collision = true;
                delta = (_game.BoardWidth - startX) / sinHeading;
                endY = startY + delta * cosHeading;
                endDistance = startDistance + delta;
            }
            if(endY < 0) {
                collision = true;
                delta = (0 - startY) / cosHeading;
                endX = startX + delta * sinHeading;
                endDistance = startDistance + delta;
            } else if(endY > _game.BoardHeight) {
                collision = true;
                delta = (_game.BoardHeight) / cosHeading;
                endX = startX + delta * sinHeading;
                endDistance = startDistance + delta;
            }
        }

        private void RobotsByDistance(double x, double y, Func<Robot, double, bool> callback) {
            foreach(var tuple in _robots
                .Where(robot => robot.State == RobotState.Alive)
                .Select(robot => new {
                    Robot = robot,
                    Distance = Distance(robot.X, robot.Y, x, y)
                })
                .OrderBy(tuple => tuple.Distance)
            ) {
                if(!callback(tuple.Robot, tuple.Distance)) {
                    break;
                }
            }
        }
    }
}
