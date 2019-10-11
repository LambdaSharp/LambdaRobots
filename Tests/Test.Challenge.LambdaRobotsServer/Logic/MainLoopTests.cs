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
using Challenge.LambdaRobots;
using Challenge.LambdaRobots.Protocol;
using Challenge.LambdaRobots.Server;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Test.Challenge.LambdaRobots.Server {

    public class MainLoopTests {

        //--- Fields ---
        private readonly ITestOutputHelper _output;
        private IGameDependencyProvider _provider;
        private Dictionary<string, List<Func<LambdaRobotAction>>> _robotActions = new Dictionary<string, List<Func<LambdaRobotAction>>>();

        //--- Constructors ---
        public MainLoopTests(ITestOutputHelper output) {
            _output = output;
        }

        //--- Properties ---
        private Game Game => _provider.Game;

        //--- Methods ---

        #region *** Movement Tests ***
        [Fact]
        public void MoveRobotNorthForOneTurn() {

            // arrange
            var robot = NewRobot("Bob", 500, 500);
            var logic = NewLogic(robot);
            _robotActions["Bob"] = new List<Func<LambdaRobotAction>> {
                () => new LambdaRobotAction {
                    Heading = 0.0,
                    Speed = 100.0
                }
            };

            // act
            logic.NextTurnAsync().Wait();

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
            _robotActions["Bob"] = new List<Func<LambdaRobotAction>> {
                () => new LambdaRobotAction {
                    Heading = 0.0,
                    Speed = 100.0
                }
            };

            // act
            logic.NextTurnAsync().Wait();
            logic.NextTurnAsync().Wait();

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
            _robotActions["Bob"] = new List<Func<LambdaRobotAction>> {
                () => new LambdaRobotAction {
                    Heading = 90.0,
                    Speed = 100.0
                }
            };

            // act
            logic.NextTurnAsync().Wait();

            // assert
            robot.X.Should().Be(510);
            robot.Y.Should().Be(500);
            robot.Speed.Should().Be(10);
        }
        #endregion

        #region *** Collision Tests ***
        [Fact]
        public void MoveRobotCollideWithWall() {

            // arrange
            var robot = NewRobot("Bob", 990.0, 990.0);
            robot.Heading = 30.0;
            robot.TargetHeading = robot.Heading;
            robot.Speed = robot.MaxSpeed;
            robot.TargetSpeed = robot.Speed;
            var logic = NewLogic(robot);

            // act
            logic.NextTurnAsync().Wait();

            // assert
            robot.X.Should().BeLessThan(1000.0);
            robot.Y.Should().Be(1000.0);
            robot.Speed.Should().Be(0.0);
            robot.Damage.Should().Be(robot.CollisionDamage);
            Game.Messages.Count.Should().Be(2);
            Game.Messages[0].Text.Should().Be("Bob was damaged 2 by wall collision");
            Game.Messages[1].Text.Should().Be("Bob is victorious! Game Over.");
        }

        [Fact]
        public void MoveRobotColliedWithOtherRobot() {

            // arrange
            var bob = NewRobot("Bob", 500.0, 500.0);
            var dave = NewRobot("Dave", 500.0, 500.0);
            var logic = NewLogic(bob, dave);

            // act
            logic.NextTurnAsync().Wait();

            // assert
            bob.Damage.Should().Be(bob.CollisionDamage);
            dave.Damage.Should().Be(dave.CollisionDamage);
            Game.Messages.Count.Should().Be(2);
            Game.Messages[0].Text.Should().Be("Bob was damaged 2 by collision with Dave");
            Game.Messages[1].Text.Should().Be("Dave was damaged 2 by collision with Bob");
        }
        #endregion

        #region *** Scan Tests ***
        [Fact]
        public void ScanRobotInRange() {

            // arrange
            var bob = NewRobot("Bob", 500.0, 500.0);
            var dave = NewRobot("Dave", 600.0, 600.0);
            var logic = NewLogic(bob, dave);

            // act
            var distance = logic.ScanRobots(bob, 45.0, 10.0);

            // assert
            distance.Should().NotBe(null);
            distance.Should().BeInRange(140.0, 142.0);
        }

        [Fact]
        public void ScanRobotOutOfRange() {

            // arrange
            var bob = NewRobot("Bob", 100.0, 100.0);
            var dave = NewRobot("Dave", 800.0, 800.0);
            var logic = NewLogic(bob, dave);

            // act
            var distance = logic.ScanRobots(bob, 45.0, 10.0);

            // assert
            distance.Should().Be(null);
        }

        [Fact]
        public void ScanRobotOutOfResolution() {

            // arrange
            var bob = NewRobot("Bob", 500.0, 500.0);
            var dave = NewRobot("Dave", 600.0, 500.0);
            var logic = NewLogic(bob, dave);

            // act
            var distance = logic.ScanRobots(bob, 45.0, 10.0);

            // assert
            distance.Should().Be(null);
        }
        #endregion

        #region *** Disqualification Tests ***
        [Fact]
        public void DisqualifiedDueToFailureToRespond() {

            // arrange
            var robot = NewRobot("Bob", 500, 500);
            var logic = NewLogic(robot);
            _robotActions["Bob"] = new List<Func<LambdaRobotAction>> {
                () => null
            };

            // act
            logic.NextTurnAsync().Wait();

            // assert
            robot.State.Should().Be(RobotState.Dead);
        }
        #endregion

        private Game NewGame() => new Game {
            Id = "Test",
            BoardWidth = 1000.0,
            BoardHeight = 1000.0,
            SecondsPerTurn = 1.0,
            DirectHitRange = 5.0,
            NearHitRange = 20.0,
            FarHitRange = 40.0,
            CollisionRange = 5.0,
            MinRobotStartDistance = 100.0,
            RobotTimeoutSeconds = 10.0,
            TotalTurns = 0,
            MaxTurns = 300
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
            ReloadCoolDown = 0.0,
            TotalMissileFiredCount = 0,

            // robot characteristics
            MaxSpeed = 100.0,
            Acceleration = 10.0,
            Deceleration = 20.0,
            MaxTurnSpeed = 50.0,
            ScannerRange = 600.0,
            ScannerResolution = 10.0,
            MaxDamage = 100.0,
            CollisionDamage = 2.0,
            DirectHitDamage = 8.0,
            NearHitDamage = 4.0,
            FarHitDamage = 2.0,

            // missile characteristics
            MissileReloadCooldown = 5.0,
            MissileSpeed = 50.0,
            MissileRange = 700.0,
            MissileDirectHitDamageBonus = 3.0,
            MissileNearHitDamageBonus = 2.1,
            MissileFarHitDamageBonus = 1.0
        };

        private GameLogic NewLogic(params Robot[] robots) {
            var game = NewGame();
            game.Robots.AddRange(robots);
            _provider = new GameDependencyProvider(
                game,
                new Random(100),
                async robot => new LambdaRobotConfig {
                    Name = robot.Id
                },
                async robot => {

                    // destructively fetch next action from dictionary or null if none exist
                    if(_robotActions.TryGetValue(robot.Id, out var actions) && (actions?.Any() ?? false)) {
                        var action = actions.First();
                        actions.RemoveAt(0);
                        return action();
                    }
                    return new LambdaRobotAction();
                }
            );
            return new GameLogic(_provider);
        }
    }
}
