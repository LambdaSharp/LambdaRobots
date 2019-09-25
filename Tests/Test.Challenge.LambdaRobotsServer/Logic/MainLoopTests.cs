using System;
using System.Collections.Generic;
using Challenge.LambdaRobotsServer;
using FluentAssertions;
using Xunit;

namespace Test.Challenge.LambdaRobotsServer {

    public class MainLoopTests {

        //--- Methods ---
        [Fact]
        public void MoveRobotNorthForOneTurn() {

            // arrange
            var robot = NewRobot("1", 500, 500);
            var logic = new Logic(NewGame(), new List<Robot> {
                robot
            });

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
            var robot = NewRobot("1", 500, 500);
            var logic = new Logic(NewGame(), new List<Robot> {
                robot
            });

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
            var robot = NewRobot("1", 500, 500);
            var logic = new Logic(NewGame(), new List<Robot> {
                robot
            });

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

        [Fact]
        public void MoveRobotColliedWithWall() {

            // arrange
            var robot = NewRobot("1", 500, 500);
            robot.X = 990;
            robot.Y = 990;
            robot.Heading = 30.0;
            robot.TargetHeading = robot.Heading;
            robot.Speed = robot.MaxSpeed;
            robot.TargetSpeed = robot.Speed;
            var logic = new Logic(NewGame(), new List<Robot> {
                robot
            });

            // act
            logic.MainLoop(new RobotAction[0]);

            // assert
            robot.X.Should().BeLessThan(1000.0);
            robot.Y.Should().Be(1000.0);
            robot.Speed.Should().Be(0.0);
            robot.Damage.Should().Be(robot.CollisionDamage);
        }

        private Game NewGame() => new Game {
            BoardWidth = 1000.0,
            BoardHeight = 1000.0,
            SecondsPerTurn = 1.0,
            DirectHitRange = 5.0,
            NearHitRange = 20.0,
            FarHitRange = 40.0,
            Missiles = new List<RobotMissile>()
        };

        private Robot NewRobot(string id, double x, double y) => new Robot {

            // robot state
            Id = id,
            State = RobotState.Alive,
            X = x,
            Y = y,
            Speed = 0.0,
            Heading = 0.0,
            Distance = 0.0,
            Damage = 0.0,
            Reload = 0.0,
            MissileFiredCount = 0,

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
    }
}
