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
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Challenge.LambdaRobots.Common;
using LambdaSharp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Challenge.LambdaRobots.Robot.RobotFunction {

    public class Function : ALambdaFunction<RobotRequest, RobotResponse> {

        //--- Fields ---
        private string _name;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // TO-DO: add robot initialization and reading configuration settings
            _name = config.ReadText("RobotName");
            if(string.IsNullOrWhiteSpace(_name)) {
                _name = CurrentContext.FunctionName;
            }
        }

        public override async Task<RobotResponse> ProcessMessageAsync(RobotRequest request) {

            // NOTE (2019-10-03, bjorg): this method dispatches to other methods based on the incoming
            //  request; most likely, there is nothing to change here.
            LogInfo($"Command: {request.Command}");
            RobotResponse response;
            switch(request.Command) {
            case RobotCommand.GetConfig:

                // robot configuration request
                response = new RobotResponse {
                    RobotConfig = GetConfig()
                };
                break;
            case RobotCommand.GetAction:

                // robot action request
                response = new RobotResponse {
                    RobotAction = await GetActionAsync(request)
                };
                break;
            default:

                // unrecognized request
                throw new ApplicationException($"unexpected request: '{request.Command}'");
            }
            LogInfo($"Response:\n{SerializeJson(response)}");
            return response;
        }

        private RobotConfig GetConfig() {

            // TODO: this method is always invoked at the beginning of a match
            return new RobotConfig {
                Name = _name
            };
        }

        private async Task<RobotAction> GetActionAsync(RobotRequest request) {

            // TODO: this method is invoked for every turn of a match
            return new RobotAction {

                // TODO: set Speed and Heading fields to direct the robot
                // Speed = 50.0,
                // Heading = 90.0,

                // TODO: set FireMissileHeading and FireMissileRange to fire a rocket
                // FireMissileHeading = 270.0,
                // FireMissileRange = 500.0
            };
        }
    }
}
