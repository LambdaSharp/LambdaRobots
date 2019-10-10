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
        /// <summary>
        /// Initialized random number generator. Instance of [Random Class](https://docs.microsoft.com/en-us/dotnet/api/system.random?view=netstandard-2.0).
        /// </summary>
        public static Random Random { get ; private set; } = new Random();

        //--- Fields ---
        private RobotAction _action;
        private string _gameApi;

        //--- Properties ---

        /// <summary>
        /// Robot name used to identify the robot during a game.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Robot data structure describing the state and characteristics of the robot.
        /// </summary>
        public Challenge.LambdaRobots.Common.Robot Robot { get; set; }

        /// <summary>
        /// Horizontal position of robot. Value is between `0` and `GameBoardWidth`.
        /// </summary>
        public double X => Robot.X;

        /// <summary>
        /// Vertical position of robot. Value is between `0` and `GameBoardHeight`.
        /// </summary>
        public double Y => Robot.Y;

        /// <summary>
        /// Robot speed. Value is between `0` and `Robot.MaxSpeed`.
        /// </summary>
        public double Speed => Robot.Speed;

        /// <summary>
        /// Robot heading. Value is always between `-180` and `180`.
        /// </summary>
        public double Heading => Robot.Heading;

        /// <summary>
        /// Robot damage. Value is always between 0 and `Robot.MaxDamage`. When the value is equal to `Robot.MaxDamage` the robot is considered killed.
        /// </summary>
        public double Damage => Robot.Damage;

        /// <summary>
        /// Number of seconds until the missile launcher is ready again.
        /// </summary>
        public double ReloadCoolDown => Robot.ReloadCoolDown;

        /// <summary>
        /// Unique Game ID.
        /// </summary>
        public string GameId { get; private set; }

        /// <summary>
        /// Width of the game board.
        /// </summary>
        public double GameBoardWidth { get; private set; }

        /// <summary>
        /// Height of the game board.
        /// </summary>
        public double GameBoardHeight { get; private set; }

        /// <summary>
        /// Seconds elapsed per game turn.
        /// </summary>
        /// <value></value>
        public double GameSecondsPerTurn { get; private set; }

        /// <summary>
        /// Maximum number of turns before the game ends in a draw.
        /// </summary>
        public int GameMaxTurns { get; private set; }

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

        public override sealed async Task<RobotResponse> ProcessMessageAsync(RobotRequest request) {
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
                    GameSecondsPerTurn = request.GameSecondsPerTurn;
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

        /// <summary>
        /// Fire a missile in a given direction with impact at a given distance.
        /// </summary>
        /// <param name="heading">Heading in degrees where to fire the missile to</param>
        /// <param name="distance">Distance at which the missile impacts</param>
        public void Fire(double heading, double distance) {
            _action.FireMissileHeading = heading;
            _action.FireMissileDistance = distance;
        }

        /// <summary>
        /// Set heading in which the robot is moving. Current speed must be below `Robot.MaxTurnSpeed`
        /// to avoid a sudden stop.
        /// </summary>
        /// <param name="heading">Target robot heading in degrees</param>
        public void SetHeading(double heading) => _action.Heading = heading;

        /// <summary>
        /// Set the speed for the robot. Speed is adjusted according to `Robot.Acceleration`
        /// and `Robot.Deceleration` characteristics.
        /// </summary>
        /// <param name="speed">Target robot speed</param>
        public void SetSpeed(double speed) => _action.Speed = speed;

        /// <summary>
        /// Scan the game board in a given heading and resolution. The resolution
        /// specifies in the scan arc from `heading-resolution` to `heading+resolution`.
        /// The max resolution is limited to `Robot.ScannerResolution`.
        /// </summary>
        /// <param name="heading">Scan heading in degrees</param>
        /// <param name="resolution">Scan resolution in degrees</param>
        /// <returns>Distance to nearest target or `null` if no target found</returns>
        public async Task<double?> ScanForEnemies(double heading, double resolution) {
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

        /// <summary>
        /// Determine angle in degrees relative to current robot position.
        /// </summary>
        /// <param name="x">Target horizontal coordinate</param>
        /// <param name="y">Target vertical coordinate</param>
        /// <returns>Angle in degrees</returns>
        public double AngleTo(double x, double y) => Math.Atan2(x - X, y - Y) * 180.0 / Math.PI;

        /// <summary>
        /// Determine distance relative to current robot position.
        /// </summary>
        /// <param name="x">Target horizontal coordinate</param>
        /// <param name="y">Target vertical coordinate</param>
        /// <returns>Distance to target</returns>
        public double DistanceTo(double x, double y) {
            var deltaX = x - X;
            var deltaY = y - Y;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }
    }
}
