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
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Challenge.LambdaRobots.Common;
using LambdaSharp;

namespace Challenge.LambdaRobots.Robot.RobotFunction {

    public abstract class ALambdaRobotFunction : ALambdaFunction<RobotRequest, RobotResponse> {

        //--- Class Fields ---
        public static Random Random { get ; private set; } = new Random();

        //--- Fields ---
        private RobotAction _action;

        //--- Properties ---
        public string Name { get; set; }
        public Challenge.LambdaRobots.Common.Robot Robot { get; set; }
        public double X => Robot.X;
        public double Y => Robot.Y;
        public double Speed => Robot.Speed;
        public double Heading => Robot.Heading;
        public double Damage => Robot.Damage;
        public double ReloadCoolDown => Robot.ReloadCoolDown;
        public string GameId { get; private set; }
        public double GameBoardWidth { get; private set; }
        public double GameBoardHeight { get; private set; }
        public int GameMaxTurns { get; private set; }
        private string _gameApi;

        //--- Abstract Methods ---
        public abstract Task<RobotConfig> GetConfigAsync();
        public abstract Task GetActionAsync();

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // read robot name from configuration; default to function name if need be
            Name = config.ReadText("RobotName");
            if(string.IsNullOrWhiteSpace(Name)) {
                Name = CurrentContext.FunctionName;
            }
        }

        public override async Task<RobotResponse> ProcessMessageAsync(RobotRequest request) {
            LogInfo($"Request:\n{SerializeJson(request)}");

            // NOTE (2019-10-03, bjorg): this method dispatches to other methods based on the incoming
            //  request; most likely, there is nothing to change here.
            RobotResponse response;
            switch(request.Command) {
            case RobotCommand.GetConfig:

                // robot configuration request
                response = new RobotResponse {
                    RobotConfig = await GetConfigAsync()
                };
                break;
            case RobotCommand.GetAction:

                // robot action request
                try {

                    // capture request fields for easy access
                    GameId = request.GameId;
                    GameBoardWidth = request.GameBoardWidth;
                    GameBoardHeight = request.GameBoardHeight;
                    GameMaxTurns = request.GameMaxTurns;
                    _gameApi = request.GameApi;
                    Robot = request.Robot;

                    // initialize a default empty action
                    _action = new RobotAction();

                    // get robot action
                    await GetActionAsync();

                    // generate response
                    response = new RobotResponse {
                        RobotAction = _action
                    };
                } finally {
                    Robot = null;
                }
                break;
            default:

                // unrecognized request
                throw new ApplicationException($"unexpected request: '{request.Command}'");
            }
            LogInfo($"Response:\n{SerializeJson(response)}");
            return response;
        }

        public void Fire(double heading, double range) {
            _action.FireMissileHeading = heading;
            _action.FireMissileRange = range;
        }

        public void SetHeading(double heading) => _action.Heading = heading;
        public void SetSpeed(double speed) => _action.Speed = speed;

        public async Task<double?> Scan(double heading, double resolution) {
            var httpResponse = await HttpClient.PostAsync($"{_gameApi}/scan", new StringContent(SerializeJson(new ScanEnemiesRequest {
                GameId = GameId,
                RobotId = Robot.Id,
                Heading = heading,
                Resolution = resolution
            }), Encoding.UTF8, "application/json"));
            var httpResponseText = await httpResponse.Content.ReadAsStringAsync();
            var response = DeserializeJson<ScanEnemiesResponse>(httpResponseText);
            LogInfo($"ScanResponse: {httpResponseText}");
            return response.Found ? response.Distance : (double?)null;
        }
    }
}
