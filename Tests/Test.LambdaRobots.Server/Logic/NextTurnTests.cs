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
using System.Collections.Generic;
using LambdaRobots;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using LambdaRobots.Bot.Model;

namespace Test.LambdaRobots.Server {

    public class NextTurnTests : _Init {

        //--- Fields ---
        private readonly ITestOutputHelper _output;

        //--- Constructors ---
        public NextTurnTests(ITestOutputHelper output) => _output = output;

        //--- Methods ---

        #region *** Movement Tests ***
        [Fact]
        public void MoveBotNorthForOneTurn() {

            // arrange
            var bot = NewBot("Bob", 500, 500);
            var logic = NewLogic(bot);
            _botActions["Bob"] = new List<Func<GetActionResponse>> {
                () => new GetActionResponse {
                    Heading = 0.0f,
                    Speed = 100.0f
                }
            };

            // act
            logic.NextTurn(1.0f);

            // assert
            bot.X.Should().Be(500.0f);
            bot.Y.Should().Be(505.0f);
            bot.Speed.Should().Be(10.0f);
        }

        [Fact]
        public void MoveBotNorthForTwoTurns() {

            // arrange
            var bot = NewBot("Bob", 500, 500);
            var logic = NewLogic(bot);
            _botActions["Bob"] = new List<Func<GetActionResponse>> {
                () => new GetActionResponse {
                    Heading = 0.0f,
                    Speed = 100.0f
                }
            };

            // act
            logic.NextTurn(1.0f);
            logic.NextTurn(1.0f);

            // assert
            bot.X.Should().Be(500.0f);
            bot.Y.Should().Be(520.0f);
            bot.Speed.Should().Be(20.0f);
        }

        [Fact]
        public void MoveBotWestForOneTurn() {

            // arrange
            var bot = NewBot("Bob", 500, 500);
            var logic = NewLogic(bot);
            _botActions["Bob"] = new List<Func<GetActionResponse>> {
                () => new GetActionResponse {
                    Heading = 90.0f,
                    Speed = 100.0f
                }
            };

            // act
            logic.NextTurn(1.0f);

            // assert
            bot.X.Should().Be(505.0f);
            bot.Y.Should().Be(500.0f);
            bot.Speed.Should().Be(10.0f);
        }
        #endregion

        #region *** Collision Tests ***
        [Fact]
        public void MoveBotCollideWithWall() {

            // arrange
            var bot = NewBot("Bob", 990.0f, 990.0f);
            bot.Heading = 30.0f;
            bot.TargetHeading = bot.Heading;
            bot.Speed = bot.MaxSpeed;
            bot.TargetSpeed = bot.Speed;
            var logic = NewLogic(bot);

            // act
            logic.NextTurn(1.0f);

            // assert
            bot.X.Should().BeLessThan(1000.0f);
            bot.Y.Should().Be(1000.0f);
            bot.Speed.Should().Be(0.0f);
            bot.Damage.Should().Be(bot.CollisionDamage);
            logic.Game.Messages.Count.Should().Be(2);
            logic.Game.Messages[0].Text.Should().Be("Bob (R0) received 2 damage by wall collision");
            logic.Game.Messages[1].Text.Should().Be("Bob is victorious! Game Over.");
        }

        [Fact]
        public void MoveBotColliedWithOtherBot() {

            // arrange
            var bob = NewBot("Bob", 500.0f, 500.0f);
            var dave = NewBot("Dave", 500.0f, 500.0f);
            var logic = NewLogic(bob, dave);

            // act
            logic.NextTurn(1.0f);

            // assert
            bob.Damage.Should().Be(bob.CollisionDamage);
            dave.Damage.Should().Be(dave.CollisionDamage);
            logic.Game.Messages.Count.Should().Be(2);
            logic.Game.Messages[0].Text.Should().Be("Bob (R0) was damaged 2 by collision with Dave (R1)");
            logic.Game.Messages[1].Text.Should().Be("Dave (R1) was damaged 2 by collision with Bob (R0)");
        }
        #endregion


        #region *** Disqualification Tests ***
        [Fact]
        public void DisqualifiedDueToFailureToRespond() {

            // arrange
            var bot = NewBot("Bob", 500.0f, 500.0f);
            var logic = NewLogic(bot);
            _botActions["Bob"] = new List<Func<GetActionResponse>> {
                () => null
            };

            // act
            logic.NextTurn(1.0f);

            // assert
            bot.Status.Should().Be(BotStatus.Dead);
        }
        #endregion
    }
}
