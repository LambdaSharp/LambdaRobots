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

namespace LambdaRobots.TargetRobot.RobotFunction {

    public class LambdaRobotState {

        // NOTE: target robot has no state
    }

    public class Function : ALambdaRobotFunction<LambdaRobotState> {

        //--- Methods ---
        public override async Task<LambdaRobotBuild> GetBuildAsync() {
            return new LambdaRobotBuild {
                Name = "Target",
                Armor = LambdaRobotArmorType.Medium,
                Engine = LambdaRobotEngineType.Economy,
                Missile = LambdaRobotMissileType.Dart,
                Radar = LambdaRobotRadarType.UltraShortRange
            };
        }

        public override async Task GetActionAsync() {

            // NOTE: target robot is stationary and doesn't do anything
        }
    }
}
