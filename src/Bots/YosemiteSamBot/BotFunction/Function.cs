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

namespace LambdaRobots.YosemiteSamBot.BotFunction {

    public sealed class BotState {

        // NOTE: Yosemite Sam bot has no internal state
    }

    public sealed class Function : ABotFunction<BotState> {

        //--- Methods ---
        public override async Task<GetBuildResponse> GetBuildAsync() {

            // TODO: this method is always invoked at the beginning of a match
            return new GetBuildResponse {
                Name = "Yosemite Sam",
                Engine = BotEngineType.ExtraLarge,
                Missile = BotMissileType.Dart
            };
        }

        public override async Task GetActionAsync() {

            // check if bot needs to accelerate
            if(Speed == 0.0f) {
                SetSpeed(Bot.MaxTurnSpeed);
                SetHeading(RandomFloat() * 360.0f);
            }

            // check if bot needs to turn
            if(X < 100.0f) {

                // too close to left wall, go right
                SetHeading(45.0f + RandomFloat() * 90.0f);
            } else if(X > (GameBoard.BoardWidth - 100.0f)) {

                // too close to right wall, go left
                SetHeading(-45.0f - RandomFloat() * 90.0f);
            }
            if(Y < 100.0f) {

                // too close to bottom wall, go up
                SetHeading(-45.0f + RandomFloat() * 90.0f);
            } else if(Y > (GameBoard.BoardHeight - 100.0f)) {

                // too close to top wall, go down
                SetHeading(135.0f + RandomFloat() * 90.0f);
            }

            // check if bot can fire a missile
            if(ReloadCoolDown == 0.0f) {

                // fire in a random direction
                FireMissile(
                    heading: RandomFloat() * 360.0f,
                    distance: 50.0f + RandomFloat() * (Bot.MissileRange - 50.0f)
                );
            }
        }
    }
}
