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
using Challenge.LambdaRobots.Protocol;
using LambdaSharp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Challenge.LambdaRobots.Robot.RobotFunction {

    public class LambdaRobotState {

        // TODO: add any state the robot needs to track between invocations (must be serializable to JSON)
    }

    public class Function : ALambdaRobotFunction<LambdaRobotState> {

        //--- Properties ---
        public string Name { get; set; }

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // read robot name from configuration; default to function name if need be
            Name = config.ReadText("RobotName");
            if(string.IsNullOrWhiteSpace(Name)) {
                Name = CurrentContext.FunctionName;
            }
        }

        public override async Task<LambdaRobotConfig> GetConfigAsync() {

            // TODO: this method is always invoked at the beginning of a match
            return new LambdaRobotConfig {
                Name = Name
            };
        }

        public override async Task GetActionAsync() {

            // check if robot needs to accelerate
            if(Speed < 40.0) {
                SetSpeed(40.0);
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
#if true

                // fire in a random direction
                FireMissile(
                    heading: Random.NextDouble() * 360.0,
                    distance: 50.0 + Random.NextDouble() * (Robot.MissileRange - 50.0)
                );
#else

                // scan in the direction we're heading
                var scanHeading = Heading;
                var resolution = 10.0;
                double? distance = null;

                // keep scanning until the direction is precise enough
                while(resolution > 1.0) {
                    for(var i = 0; i < 3; ++i) {
                        var currentScanHeading = scanHeading + ((i - 1) * 2.0 * resolution);
                        distance = await Scan(currentScanHeading, resolution);

                        // check if we found something
                        if(distance.HasValue) {

                            // center scanner in the direction where we found something
                            scanHeading = currentScanHeading + resolution * 0.5;

                            // narrow the scan resolution for a more precise heading reading
                            resolution = resolution / 3.0;
                            continue;
                        }
                    }

                    // scanner didn't find anything
                    break;
                }

                // check if a target was found
                if(distance.HasValue) {
                    Fire(scanHeading, distance.Value);
                }
#endif
            }
        }
    }
}
