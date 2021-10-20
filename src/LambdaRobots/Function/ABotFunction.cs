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

using System;
using System.Threading.Tasks;
using LambdaRobots.Bot.Model;
using LambdaRobots.Game;
using LambdaSharp;

namespace LambdaRobots.Function {

    public abstract class ABotFunction<TState> : ALambdaFunction<BotRequest, BotResponse> where TState : class, new() {

        //--- Class Fields ---

        /// <summary>
        /// Initialized random number generator. Instance of [Random Class](https://docs.microsoft.com/en-us/dotnet/api/system.random?view=netstandard-2.0).
        /// </summary>
        private static Random g_random { get ; set; } = new Random();

        //--- Class Methods ---

        /// <summary>
        /// Returns a random <c>float</c> number.
        /// </summary>
        protected static float RandomFloat() => (float)g_random.NextDouble();

        //--- Fields ---
        private GetActionResponse _action;

        //--- Constructors ---
        protected ABotFunction() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Properties ---

        /// <summary>
        /// Bot data structure describing the state and characteristics of the bot.
        /// </summary>
        public LambdaRobots.BotInfo Bot { get; set; }

        /// <summary>
        /// Game data structure describing the state and characteristics of the game;
        /// </summary>
        public LambdaRobots.GameInfo Game { get; set; }

        /// <summary>
        /// Horizontal position of bot. Value is between `0` and `Game.BoardWidth`.
        /// </summary>
        public float X => Bot.X;

        /// <summary>
        /// Vertical position of bot. Value is between `0` and `Game.BoardHeight`.
        /// </summary>
        public float Y => Bot.Y;

        /// <summary>
        /// Bot speed. Value is between `0` and `Bot.MaxSpeed`.
        /// </summary>
        public float Speed => Bot.Speed;

        /// <summary>
        /// Bot heading. Value is always between `-180` and `180`.
        /// </summary>
        public float Heading => Bot.Heading;

        /// <summary>
        /// Bot damage. Value is always between 0 and `Bot.MaxDamage`. When the value is equal to `Bot.MaxDamage` the bot is considered killed.
        /// </summary>
        public float Damage => Bot.Damage;

        /// <summary>
        /// Number of seconds until the missile launcher is ready again.
        /// </summary>
        public float ReloadCoolDown => Bot.ReloadCoolDown;

        /// <summary>
        /// How long it will take for the bot to stop.
        /// </summary>
        public float BreakingDistance => (Speed * Speed) / (2.0f * Bot.Deceleration);

        /// <summary>
        /// Bot state is automatically saved and loaded for each invocation when available.
        /// </summary>
        public TState State { get; set; }

        public ILambdaRobotsGame GameClient { get; set; }

        //--- Abstract Methods ---
        public abstract Task<GetBuildResponse> GetBuildAsync();
        public abstract Task GetActionAsync();

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) { }

        public override sealed async Task<BotResponse> ProcessMessageAsync(BotRequest request) {

            // check if there is a state object to load
            State = !string.IsNullOrEmpty(request.Bot?.InternalState)
                ? LambdaSerializer.Deserialize<TState>(request.Bot.InternalState)
                : new TState();
            LogInfo($"Starting State:\n{LambdaSerializer.Serialize(State)}");

            // dispatch to specific method based on request command
            BotResponse response;
            switch(request.Command) {
            case BotCommand.GetBuild:

                // bot configuration request
                response = new BotResponse {
                    BotBuild = await GetBuildAsync()
                };
                break;
            case BotCommand.GetAction:

                // bot action request
                try {

                    // capture request fields for easy access
                    Game = request.Game;
                    Bot = request.Bot;
                    GameClient = new LambdaRobotsGameClient(Game.ApiUrl, Bot.Id, HttpClient);

                    // initialize a default empty action
                    _action = new GetActionResponse();

                    // get bot action
                    await GetActionAsync();

                    // generate response
                    _action.BotState = LambdaSerializer.Serialize(State);
                    response = new BotResponse {
                        BotAction = _action,
                    };
                } finally {
                    Bot = null;
                    GameClient = null;
                }
                break;
            default:

                // unrecognized request
                throw new ApplicationException($"unexpected request: '{request.Command}'");
            }

            // log response and return
            LogInfo($"Final State:\n{LambdaSerializer.Serialize(State)}");
            return response;
        }

        /// <summary>
        /// Fire a missile in a given direction with impact at a given distance.
        /// A missile can only be fired if `Bot.ReloadCoolDown` is `0`.
        /// </summary>
        /// <param name="heading">Heading in degrees where to fire the missile to</param>
        /// <param name="distance">Distance at which the missile impacts</param>
        public void FireMissile(float heading, float distance) {
            LogInfo($"Fire Missile: Heading = {NormalizeAngle(heading):N2}, Distance = {distance:N2}");
            _action.FireMissileHeading = heading;
            _action.FireMissileDistance = distance;
        }

        /// <summary>
        /// Fire a missile in at the given position.
        /// A missile can only be fired if `Bot.ReloadCoolDown` is `0`.
        /// </summary>
        /// <param name="x">Target horizontal coordinate</param>
        /// <param name="y">Target vertical coordinate</param>
        public void FireMissileToXY(float x, float y) {
            var heading = AngleToXY(x, y);
            var distance = DistanceToXY(x, y);
            FireMissile(heading, distance);
        }

        /// <summary>
        /// Set heading in which the bot is moving. Current speed must be below `Bot.MaxTurnSpeed`
        /// to avoid a sudden stop.
        /// </summary>
        /// <param name="heading">Target bot heading in degrees</param>
        public void SetHeading(float heading) {
            LogInfo($"Set Heading = {NormalizeAngle(heading):N0}");
            _action.Heading = heading;
        }

        /// <summary>
        /// Set the speed for the bot. Speed is adjusted according to `Bot.Acceleration`
        /// and `Bot.Deceleration` characteristics.
        /// </summary>
        /// <param name="speed">Target bot speed</param>
        public void SetSpeed(float speed) {
            LogInfo($"Set Speed = {speed:N2}");
            _action.Speed = speed;
        }

        /// <summary>
        /// Scan the game board in a given heading and resolution.
        /// The resolution specifies in the scan arc centered on `heading` with +/- `resolution` tolerance.
        /// The max resolution is limited to `Bot.RadarMaxResolution`.
        /// </summary>
        /// <param name="heading">Scan heading in degrees</param>
        /// <param name="resolution">Scan +/- arc in degrees</param>
        /// <returns>Distance to nearest target or `null` if no target found</returns>
        public async Task<float?> ScanAsync(float heading, float resolution) {
            var response = await GameClient.ScanAsync(heading, resolution);
            var result = (response?.Found ?? false)
                ? (float?)response.Distance
                : null;
            LogInfo($"Scan: Heading = {heading:N2}, Resolution = {resolution:N2}, Found = {result?.ToString("N2") ?? "(null)"} [Success = {response != null}]");
            return result;
        }

        /// <summary>
        /// Determine angle in degrees relative to current bot position.
        /// Return value range from `-180` to `180` degrees.
        /// </summary>
        /// <param name="x">Target horizontal coordinate</param>
        /// <param name="y">Target vertical coordinate</param>
        /// <returns>Angle in degrees</returns>
        public float AngleToXY(float x, float y) => NormalizeAngle(MathF.Atan2(x - X, y - Y) * 180.0f / MathF.PI);

        /// <summary>
        /// Determine distance relative to current bot position.
        /// </summary>
        /// <param name="x">Target horizontal coordinate</param>
        /// <param name="y">Target vertical coordinate</param>
        /// <returns>Distance to target</returns>
        public float DistanceToXY(float x, float y) {
            var deltaX = x - X;
            var deltaY = y - Y;
            return MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        /// <summary>
        /// Normalize angle to be between `-180` and `180` degrees.
        /// </summary>
        /// <param name="angle">Angle in degrees to normalize</param>
        /// <returns>Angle in degrees</returns>
        public float NormalizeAngle(float angle) {
            var result = angle % 360;
            return (result < -180.0f)
                ? (result + 360.0f)
                : result;
        }

        /// <summary>
        /// Adjust speed and heading to move bot to specified coordinates.
        /// Call this method on every turn to keep adjusting the speed and heading until the destination is reached.
        /// </summary>
        /// <param name="x">Target horizontal coordinate</param>
        /// <param name="y">Target vertical coordinate</param>
        /// <returns>Returns `true` if arrived at target location</returns>
        public bool MoveToXY(float x, float y) {
            var heading = AngleToXY(x, y);
            var distance = DistanceToXY(x, y);
            LogInfo($"Move To: X = {x:N2}, Y = {y:N2}, Heading = {heading:N2}, Distance = {distance:N2}");

            // check if bot is close enough to target location
            if(distance <= Game.CollisionRange) {

                // close enough; stop moving
                SetSpeed(0.0f);
                return true;
            }

            // NOTE: the distance required to stop the bot from moving is obtained with the following formula:
            //      Distance = Speed^2 / 2*Deceleration
            //  solving for Speed, gives us the maximum travel speed to avoid overshooting our target
            var speed = MathF.Sqrt(distance * 2.0f * Bot.Deceleration) * Game.SecondsSinceLastTurn;

            // check if heading needs to be adjusted
            if(MathF.Abs(NormalizeAngle(Heading - heading)) > 0.1) {

                // check if bot is moving slow enough to turn
                if(Speed <= Bot.MaxTurnSpeed) {

                    // adjust heading towards target
                    SetHeading(heading);
                }

                // adjust speed to either max-turn-speed or max-travel-speed, whichever is lower
                SetSpeed(MathF.Min(Bot.MaxTurnSpeed, speed));
            } else {

                // adjust speed to max-travel-speed
                SetSpeed(speed);
            }
            return false;
        }
    }
}
