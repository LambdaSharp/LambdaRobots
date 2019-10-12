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
using System.IO;
using System.Threading.Tasks;
using Challenge.LambdaRobots.Api;
using Challenge.LambdaRobots.Protocol;
using LambdaSharp;
using Newtonsoft.Json;

namespace Challenge.LambdaRobots {

    public abstract class ALambdaRobotFunction<TState> : ALambdaFunction<LambdaRobotRequest, LambdaRobotResponse> where TState : class {

        //--- Class Fields ---

        /// <summary>
        /// Initialized random number generator. Instance of [Random Class](https://docs.microsoft.com/en-us/dotnet/api/system.random?view=netstandard-2.0).
        /// </summary>
        public static Random Random { get ; private set; } = new Random();

        //--- Fields ---
        private LambdaRobotAction _action;

        //--- Properties ---

        /// <summary>
        /// Robot data structure describing the state and characteristics of the robot.
        /// </summary>
        public Challenge.LambdaRobots.Robot Robot { get; set; }

        /// <summary>
        /// Game data structure describing the state and characteristics of the game;
        /// </summary>
        public Challenge.LambdaRobots.GameInfo Game { get; set; }

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

        public double BreakingDistance => (Speed * Speed) / (2.0 * Robot.Deceleration);

        /// <summary>
        /// Robot state is automatically saved and loaded for each invocation when available.
        /// </summary>
        public TState State { get; set; }

        //--- Abstract Methods ---
        public abstract Task<LambdaRobotBuild> GetBuildAsync();
        public abstract Task GetActionAsync();

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) { }

        public override sealed async Task<LambdaRobotResponse> ProcessMessageAsync(LambdaRobotRequest request) {

            // NOTE (2019-10-03, bjorg): this method dispatches to other methods based on the incoming
            //  request; most likely, there is nothing to change here.
            LogInfo($"Request:\n{SerializeJson(request)}");

            // check if there is a state object to load
            var stateFile = Path.Combine(Path.GetTempPath(), $"state-{request.Robot.Id.Replace(':', '-')}.json");
            if(File.Exists(stateFile)) {
                try {
                    State = JsonConvert.DeserializeObject<TState>(await File.ReadAllTextAsync(stateFile));
                } catch(Exception e) {
                    LogErrorAsWarning(e, $"unable to load state file: {stateFile}");
                    State = null;
                }
            }

            // dispatch to specific method based on request command
            LambdaRobotResponse response;
            switch(request.Command) {
            case LambdaRobotCommand.GetBuild:

                // robot configuration request
                response = new LambdaRobotResponse {
                    RobotBuild = await GetBuildAsync()
                };
                break;
            case LambdaRobotCommand.GetAction:

                // robot action request
                try {

                    // capture request fields for easy access
                    Game = request.Game;
                    Robot = request.Robot;

                    // initialize a default empty action
                    _action = new LambdaRobotAction();

                    // get robot action
                    await GetActionAsync();

                    // generate response
                    response = new LambdaRobotResponse {
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

            // check if there is a state object to save
            if(State != null) {
                try {
                    await File.WriteAllTextAsync(stateFile, JsonConvert.SerializeObject(State));
                } catch(Exception e) {
                    LogErrorAsWarning(e, $"unable to save state file: {stateFile}");
                }
            } else if(File.Exists(stateFile)) {
                try {
                    File.Delete(stateFile);
                } catch(Exception e) {
                    LogErrorAsWarning(e, $"unable to delete state file: {stateFile}");
                }
            }

            // log response and return
            LogInfo($"Response:\n{SerializeJson(response)}");
            return response;
        }

        /// <summary>
        /// Fire a missile in a given direction with impact at a given distance.
        /// </summary>
        /// <param name="heading">Heading in degrees where to fire the missile to</param>
        /// <param name="distance">Distance at which the missile impacts</param>
        public void FireMissile(double heading, double distance) {
            LogInfo($"Fire Missile: Heading = {NormalizeAngle(heading):N0}, Distance = {distance:N0}");
            _action.FireMissileHeading = heading;
            _action.FireMissileDistance = distance;
        }

        /// <summary>
        /// Fire a missile in at the given coordinates.
        /// </summary>
        /// <param name="x">Target horizontal coordinate</param>
        /// <param name="y">Target vertical coordinate</param>
        public void FireMissileToXY(double x, double y) {
            var heading = AngleToXY(x, y);
            var distance = DistanceToXY(x, y);
            FireMissile(heading, distance);
        }

        /// <summary>
        /// Set heading in which the robot is moving. Current speed must be below `Robot.MaxTurnSpeed`
        /// to avoid a sudden stop.
        /// </summary>
        /// <param name="heading">Target robot heading in degrees</param>
        public void SetHeading(double heading) {
            LogInfo($"Set Heading = {NormalizeAngle(heading):N0}");
            _action.Heading = heading;
        }

        /// <summary>
        /// Set the speed for the robot. Speed is adjusted according to `Robot.Acceleration`
        /// and `Robot.Deceleration` characteristics.
        /// </summary>
        /// <param name="speed">Target robot speed</param>
        public void SetSpeed(double speed) {
            LogInfo($"Set Speed = {speed:N0}");
            _action.Speed = speed;
        }

        /// <summary>
        /// Scan the game board in a given heading and resolution. The resolution
        /// specifies in the scan arc from `heading-resolution` to `heading+resolution`.
        /// The max resolution is limited to `Robot.ScannerResolution`.
        /// </summary>
        /// <param name="heading">Scan heading in degrees</param>
        /// <param name="resolution">Scan +/- arc in degrees</param>
        /// <returns>Distance to nearest target or `null` if no target found</returns>
        public Task<double?> ScanAsync(double heading, double resolution) {
            LogInfo($"Scan: Heading = {NormalizeAngle(heading):N0}, Resolution = {resolution:N0}");
            return new LambdaRobotsApiClient(HttpClient, Game.ApiUrl, Game.Id, Robot.Id).ScanAsync(heading, resolution);
        }

        /// <summary>
        /// Determine angle in degrees relative to current robot position.
        /// Return value range from `-180` to `180` degrees.
        /// </summary>
        /// <param name="x">Target horizontal coordinate</param>
        /// <param name="y">Target vertical coordinate</param>
        /// <returns>Angle in degrees</returns>
        public double AngleToXY(double x, double y) => NormalizeAngle(Math.Atan2(x - X, y - Y) * 180.0 / Math.PI);

        /// <summary>
        /// Determine distance relative to current robot position.
        /// </summary>
        /// <param name="x">Target horizontal coordinate</param>
        /// <param name="y">Target vertical coordinate</param>
        /// <returns>Distance to target</returns>
        public double DistanceToXY(double x, double y) {
            var deltaX = x - X;
            var deltaY = y - Y;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        /// <summary>
        /// Normalize angle to be between `-180` and `180` degrees.
        /// </summary>
        /// <param name="angle">Angle in degrees to normalize</param>
        /// <returns>Angle in degrees</returns>
        public double NormalizeAngle(double angle) {
            var result = angle % 360;
            return (result < -180.0)
                ? (result + 360.0)
                : result;
        }

        /// <summary>
        /// Adjust speed and heading to move robot to specified coordinates.
        /// </summary>
        /// <param name="x">Target horizontal coordinate</param>
        /// <param name="y">Target vertical coordinate</param>
        /// <returns>Return `true` if arrived at target location</returns>
        public bool MoveToXY(double x, double y) {
            var heading = AngleToXY(x, y);
            var distance = DistanceToXY(x, y);

            // check if robot is close enough to target location
            if(distance < 1.0) {

                // close enough; stop moving
                SetSpeed(0.0);
                return true;
            }

            // NOTE: the distance required to stop the robot from moving is obtained with the following formula:
            //      Distance = Speed^2 / 2*Deceleration
            //  solving for Speed, gives us the maximum travel speed to avoid overshooting our target
            var speed = Math.Sqrt(distance * 2.0 * Robot.Deceleration);

            // check if angle needs to be adjusted
            if(Math.Abs(Heading - heading) < 0.1) {

                // check if robot is moving slow enough to turn
                if(Speed <= Robot.MaxTurnSpeed) {

                    // adjust heading towards target
                    SetHeading(heading);
                } else {

                    // adjust speed to either max-turn-speed or max-travel-speed, whichever is lower
                    SetSpeed(Math.Min(Robot.MaxTurnSpeed, speed));
                }
            } else {

                // adjust speed to max-travel-speed
                SetSpeed(speed);
            }
            return false;
        }
    }
}
