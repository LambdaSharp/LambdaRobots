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
using LambdaRobots.Function;

namespace LambdaRobots.HotShotBot.BotFunction {

    public sealed class BotState {

        //--- Properties ---
        public bool Initialized { get; set; }
        public float ScanHeading { get; set; }
        public float LastDamage { get; set; }
        public float? TargetRange { get; set; }
        public float NoHitSweep { get; set; }
        public float ScanResolution { get; set; }
        public float? GotoX { get; set; }
        public float? GotoY { get; set; }
    }

    public sealed class Function : ABotFunction<BotState> {

        //--- Properties ---
        private bool WasHurt => Damage > State.LastDamage;

        //--- Methods ---
        public override async Task<GetBuildResponse> GetBuildAsync() {
            return new GetBuildResponse {
                Name = "HotShot",
                Radar = BotRadarType.LongRange,
                Armor = BotArmorType.Medium,
                Engine = BotEngineType.Compact,
                Missile = BotMissileType.Javelin
            };
        }

        public override async Task GetActionAsync() {

            // check if the state object needs to be initialized
            if(!State.Initialized) {
                State.Initialized = true;

                // initialize state
                State.NoHitSweep = 0.0f;
                State.ScanResolution = Bot.RadarMaxResolution;

                // initialize aim to point to middle of the game board
                State.ScanHeading = AngleToXY(GameSession.BoardWidth / 2, GameSession.BoardHeight / 2);
            }

            // NOTE: HotShot will do up to 2 scans each turn. Whichever area has a hit is then targetted and the
            //  scan resolution is halved. This has the effect of progressively refining the targeting after
            //  each turn, similar to binary search.

            // check if target is found in left scan area
            float scanHeading = State.ScanHeading - State.ScanResolution;
            State.TargetRange = await ScanAsync(scanHeading, State.ScanResolution);
            if((State.TargetRange != null) && (State.TargetRange > GameSession.FarHitRange) && (State.TargetRange <= Bot.MissileRange)) {
                LogInfo($"Target found: ScanHeading = {scanHeading:N2}, Range = {State.TargetRange:N2}");

                // update scan heading
                State.ScanHeading = scanHeading;

                // narrow scan resolution
                State.ScanResolution = MathF.Max(0.1f, State.ScanResolution / 2.0f);
            } else {

                // check if target is found in right scan area
                scanHeading = State.ScanHeading + State.ScanResolution;
                State.TargetRange = await ScanAsync(scanHeading, State.ScanResolution);
                if((State.TargetRange != null) && (State.TargetRange > GameSession.FarHitRange) && (State.TargetRange <= Bot.MissileRange)) {
                    LogInfo($"Target found: ScanHeading = {scanHeading:N2}, Range = {State.TargetRange:N2}");

                    // update scan heading
                    State.ScanHeading = scanHeading;

                    // narrow scan resolution
                    State.ScanResolution = MathF.Max(0.1f, State.ScanResolution / 2.0f);
                } else {
                    State.TargetRange = null;
                    LogInfo($"No target found: ScanHeading = {State.ScanHeading:N2} +/- {State.ScanResolution:N2}");

                    // look in adjacent area
                    State.ScanHeading += 3.0f * Bot.RadarMaxResolution;

                    // reset resolution to max resolution
                    State.ScanResolution = Bot.RadarMaxResolution;

                    // increase our no-hit sweep tracker so we know when to move
                    State.NoHitSweep += 2.0f * Bot.RadarMaxResolution;
                }
            }
            State.ScanHeading = NormalizeAngle(State.ScanHeading);

            // check if a target was found that is neither too close, nor too far
            if(State.TargetRange != null) {

                // fire at target
                FireMissile(State.ScanHeading, State.TargetRange.Value);

                // reset sweep tracker to indicate we found something
                State.NoHitSweep = 0.0f;
            }

            // check if we're not moving
            if((State.GotoX is null) || (State.GotoY is null)) {

                // check if we were hurt or having found a target after a full sweep
                if(WasHurt) {
                    LogInfo("Damage detected. Taking evasive action.");

                    // take evasive action!
                    State.GotoX = GameSession.CollisionRange + RandomFloat() * (GameSession.BoardWidth - 2.0f * GameSession.CollisionRange);
                    State.GotoY = GameSession.CollisionRange + RandomFloat() * (GameSession.BoardHeight - 2.0f * GameSession.CollisionRange);
                } else if((State.NoHitSweep >= 360.0) && ((State.GotoX is null) || (State.GotoY is null))) {
                    LogInfo("Nothing found in immediate surroundings. Moving to new location.");

                    // time to move to a new random location on the board
                    State.GotoX = GameSession.CollisionRange + RandomFloat() * (GameSession.BoardWidth - 2.0f * GameSession.CollisionRange);
                    State.GotoY = GameSession.CollisionRange + RandomFloat() * (GameSession.BoardHeight - 2.0f * GameSession.CollisionRange);
                }
            }

            // check if we're moving and have reached our destination
            if((State.GotoX != null) && (State.GotoY != null)) {

                // reset sweep tracker while moving
                State.NoHitSweep = 0.0f;

                // stop moving once we have reached our destination
                if(MoveToXY(State.GotoX.Value, State.GotoY.Value)) {
                    State.GotoX = null;
                    State.GotoY = null;
                }
            }

            // store current damage so we can detect if we got hurt
            State.LastDamage = Damage;
        }
    }
}
