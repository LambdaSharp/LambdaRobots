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

using System.Threading.Tasks;
using LambdaRobots.Bot.Model;
using LambdaRobots.Function;

namespace LambdaRobots.BringYourOwnRobot.RobotFunction {

    public sealed class BotState {

        // TODO: use this to define the internal state of the bot
    }

    public sealed class Function : ABotFunction<BotState> {

        //--- Methods ---
        public override async Task<GetBuildResponse> GetBuildAsync() {
            return new GetBuildResponse {

                // TODO: give your robot a name!
                Name = "BringYourOwnRobot",

                Armor = BotArmorType.Medium,
                Engine = BotEngineType.Economy,
                Missile = BotMissileType.Dart,
                Radar = BotRadarType.UltraShortRange
            };
        }

        public override async Task GetActionAsync() {

            // TODO: breath life into your robots behavior

            // NOTE: you can use the `State` property to fetch and store state across invocation.
            //  The `State` property is of type `BotState`.
        }
    }
}
