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
using Challenge.LambdaRobotsServer;
using FluentAssertions;
using Xunit;

namespace Test.Challenge.LambdaRobotsServer {

    public class MainLoopTests {

        //--- Types ---
        private class DependencyProvider : IDependencyProvider {

            //--- Fields ---
            private Game _game;
            private DateTime _utcNow;

            //--- Constructors ---
            public DependencyProvider(Game game, DateTime utcNow) {
                _game = game;
                _utcNow = utcNow;
            }

            //--- Properties ---
            public DateTime UtcNow => _utcNow;

            public Game Game => _game;
        }

        //--- Fields ---
        private IDependencyProvider _provider;

        //--- Properties ---
        public Game Game => _provider.Game;

        //--- Methods ---

        #region Movement Tests
        [Fact]
        public void MoveRobotNorthForOneTurn() {

            // arrange
            var robot = NewRobot("Bob", 500, 500);
            var logic = NewLogic(robot);

            // act
            logic.MainLoop(new[] {
                new RobotAction {
                    RobotId = robot.Id,
                    Heading = 0.0,
                    Speed = 100.0
                }
            });

            // assert
            robot.X.Should().Be(500);
            robot.Y.Should().Be(510);
            robot.Speed.Should().Be(10);
        }

        [Fact]
        public void MoveRobotNorthForTwoTurns() {

            // arrange
            var robot = NewRobot("Bob", 500, 500);
            var logic = NewLogic(robot);

            // act
            logic.MainLoop(new[] {
                new RobotAction {
                    RobotId = robot.Id,
                    Heading = 0.0,
                    Speed = 100.0
                }
            });
            logic.MainLoop(new RobotAction[0]);

            // assert
            robot.X.Should().Be(500);
            robot.Y.Should().Be(530);
            robot.Speed.Should().Be(20);
        }

        [Fact]
        public void MoveRobotWestForOneTurn() {

            // arrange
            var robot = NewRobot("Bob", 500, 500);
            var logic = NewLogic(robot);

            // act
            logic.MainLoop(new[] {
                new RobotAction {
                    RobotId = robot.Id,
                    Heading = 90.0,
                    Speed = 100.0
                }
            });

            // assert
            robot.X.Should().Be(510);
            robot.Y.Should().Be(500);
            robot.Speed.Should().Be(10);
        }
        #endregion

        #region Collision Tests
        [Fact]
        public void MoveRobotColliedWithWall() {

            // arrange
            var robot = NewRobot("Bob", 500, 500);
            robot.X = 990;
            robot.Y = 990;
            robot.Heading = 30.0;
            robot.TargetHeading = robot.Heading;
            robot.Speed = robot.MaxSpeed;
            robot.TargetSpeed = robot.Speed;
            var logic = NewLogic(robot);

            // act
            logic.MainLoop(new RobotAction[0]);

            // assert
            robot.X.Should().BeLessThan(1000.0);
            robot.Y.Should().Be(1000.0);
            robot.Speed.Should().Be(0.0);
            robot.Damage.Should().Be(robot.CollisionDamage);
            Game.Messages.Count.Should().Be(1);
            Game.Messages.First().Text.Should().Be("Bob was damaged 2 by wall collision");
        }
        #endregion

        private Game NewGame() => new Game {
            BoardWidth = 1000.0,
            BoardHeight = 1000.0,
            SecondsPerTurn = 1.0,
            DirectHitRange = 5.0,
            NearHitRange = 20.0,
            FarHitRange = 40.0,
            CollisionRange = 2.0
        };

        private Robot NewRobot(string id, double x, double y) => new Robot {

            // robot state
            Id = id,
            Name = id,
            State = RobotState.Alive,
            X = x,
            Y = y,
            Speed = 0.0,
            Heading = 0.0,
            TotalTravelDistance = 0.0,
            Damage = 0.0,
            ReloadDelay = 0.0,
            TotalMissileFiredCount = 0,

            // robot characteristics
            MaxSpeed = 100.0,
            Acceleration = 10.0,
            Deceleration = -20.0,
            MaxTurnSpeed = 50.0,
            ScannerRange = 600.0,
            MaxDamage = 100.0,
            CollisionDamage = 2.0,
            DirectHitDamage = 8.0,
            NearHitDamage = 4.0,
            FarHitDamage = 2.0,

            // missile characteristics
            MissileReloadDelay = 2.0,
            MissileSpeed = 50.0,
            MissileRange = 700.0,
            MissileDirectHitDamageBonus = 3.0,
            MissileNearHitDamageBonus = 2.1,
            MissileFarHitDamageBonus = 1.0
        };

        private Logic NewLogic(params Robot[] robots) {
            var game = NewGame();
            game.Robots.AddRange(robots);
            _provider = new DependencyProvider(game, new DateTime(2019, 09, 27, 14, 30, 0));
            return new Logic(_provider);
        }
    }
}
