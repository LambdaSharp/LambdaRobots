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

using System.Threading.Tasks;
using Amazon.Lambda.Core;
using LambdaRobots.Protocol;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace LambdaRobots.YosemiteSamRobot.RobotFunction {

    public class LambdaRobotState {

        // NOTE: Yosemite Sam robot has no state
    }

    public class Function : ALambdaRobotFunction<LambdaRobotState> {

        //--- Methods ---
        public override async Task<LambdaRobotBuild> GetBuildAsync() {

            // TODO: this method is always invoked at the beginning of a match
            return new LambdaRobotBuild {
                Name = "Yosemite Sam",
                Engine = LambdaRobotEngineType.ExtraLarge,
                Missile = LambdaRobotMissileType.Dart
            };
        }

        public override async Task GetActionAsync() {

            // check if robot needs to accelerate
            if(Speed == 0.0) {
                SetSpeed(Robot.MaxTurnSpeed);
                SetHeading(Random.NextDouble() * 360.0);
            }

            // check if robot needs to turn
            if(X < 100.0) {

                // too close to left wall, go right
                SetHeading(45.0 + Random.NextDouble() * 90.0);
            } else if(X > (Game.BoardWidth - 100.0)) {

                // too close to right wall, go left
                SetHeading(-45.0 - Random.NextDouble() * 90.0);
            }
            if(Y < 100.0) {

                // too close to bottom wall, go up
                SetHeading(-45.0 + Random.NextDouble() * 90.0);
            } else if(Y > (Game.BoardHeight - 100.0)) {

                // too close to top wall, go down
                SetHeading(135.0 + Random.NextDouble() * 90.0);
            }

            // check if robot can fire a missile
            if(ReloadCoolDown == 0.0) {

                // fire in a random direction
                FireMissile(
                    heading: Random.NextDouble() * 360.0,
                    distance: 50.0 + Random.NextDouble() * (Robot.MissileRange - 50.0)
                );
            }
        }
    }
}
