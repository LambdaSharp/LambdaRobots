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

using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Test.LambdaRobots.Server {

    public class ScanRobotsTests : _Init {

        //--- Fields ---
        private readonly ITestOutputHelper _output;

        //--- Constructors ---
        public ScanRobotsTests(ITestOutputHelper output) => _output = output;

        //--- Methods ---

        [Fact]
        public void ScanRobotInRange() {

            // arrange
            var bob = NewRobot("Bob", 500.0f, 500.0f);
            var dave = NewRobot("Dave", 600.0f, 600.0f);
            var logic = NewLogic(bob, dave);

            // act
            var robot = logic.ScanRobots(bob, 45.0f, 10.0f);

            // assert
            robot.Should().NotBe(null);
            robot.Name.Should().Be(dave.Name);
        }

        [Fact]
        public void ScanRobotOutOfRange() {

            // arrange
            var bob = NewRobot("Bob", 100.0f, 100.0f);
            var dave = NewRobot("Dave", 800.0f, 800.0f);
            var logic = NewLogic(bob, dave);

            // act
            var robot = logic.ScanRobots(bob, 45.0f, 10.0f);

            // assert
            robot.Should().Be(null);
        }

        [Fact]
        public void ScanRobotOutOfResolution() {

            // arrange
            var bob = NewRobot("Bob", 500.0f, 500.0f);
            var dave = NewRobot("Dave", 600.0f, 500.0f);
            var logic = NewLogic(bob, dave);

            // act
            var robot = logic.ScanRobots(bob, 45.0f, 10.0f);

            // assert
            robot.Should().Be(null);
        }
    }
}
