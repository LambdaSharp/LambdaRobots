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
using LambdaRobots.Protocol;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace LambdaRobots.HotShotRobot.RobotFunction {

    public class LambdaRobotState {

        //--- Properties ---
        public bool Initialized { get; set; }
        public double ScanHeading { get; set; }
        public double LastDamage { get; set; }
        public double? TargetRange { get; set; }
        public double NoHitSweep { get; set; }
        public double ScanResolution { get; set; }
        public double? GotoX { get; set; }
        public double? GotoY { get; set; }
    }

    public class Function : ALambdaRobotFunction<LambdaRobotState> {

        //--- Properties ---
        private bool WasHurt => Damage > State.LastDamage;

        //--- Methods ---
        public override async Task<LambdaRobotBuild> GetBuildAsync() {
            return new LambdaRobotBuild {
                Name = "HotShot",
                Radar = LambdaRobotRadarType.LongRange,
                Armor = LambdaRobotArmorType.Medium,
                Engine = LambdaRobotEngineType.Compact,
                Missile = LambdaRobotMissileType.Javelin
            };
        }

        public override async Task GetActionAsync() {

            // check if the state object needs to be initialized
            if(!State.Initialized) {
                State.Initialized = true;

                // initialize state
                State.NoHitSweep = 0.0;
                State.ScanResolution = Robot.RadarMaxResolution;

                // initialize aim to point to middle of the game board
                State.ScanHeading = AngleToXY(Game.BoardWidth / 2, Game.BoardHeight / 2);
            }

            // NOTE: HotShot will do up to 2 scans each turn. Whichever area has a hit is then targetted and the
            //  scan resolution is halved. This has the effect of progressively refining the targeting after
            //  each turn, similar to binary search.

            // check if target is found in left scan area
            State.TargetRange = await ScanAsync(State.ScanHeading - State.ScanResolution, State.ScanResolution);
            if((State.TargetRange != null) && (State.TargetRange > Game.FarHitRange) && (State.TargetRange <= Robot.MissileRange)) {
                LogInfo($"Target found: ScanHeading = {State.ScanHeading:N2}, Range = {State.TargetRange:N2}");

                // update scan heading
                State.ScanHeading -= State.ScanResolution;

                // narrow scan resolution
                State.ScanResolution = Math.Max(0.1, State.ScanResolution / 2.0);
            } else {

                // check if target is found in right scan area
                State.TargetRange = await ScanAsync(State.ScanHeading + State.ScanResolution, State.ScanResolution);
                if((State.TargetRange != null) && (State.TargetRange > Game.FarHitRange) && (State.TargetRange <= Robot.MissileRange)) {
                    LogInfo($"Target found: ScanHeading = {State.ScanHeading:N2}, Range = {State.TargetRange:N2}");

                    // update scan heading
                    State.ScanHeading += State.ScanResolution;

                    // narrow scan resolution
                    State.ScanResolution = Math.Max(0.1, State.ScanResolution / 2.0);
                } else {
                    LogInfo($"No target found: ScanHeading = {State.ScanHeading:N2}");

                    // look in adjacent area
                    State.ScanHeading += 3.0 * Robot.RadarMaxResolution;

                    // reset resolution to max resolution
                    State.ScanResolution = Robot.RadarMaxResolution;

                    // increase our no-hit sweep tracker so we know when to move
                    State.NoHitSweep += 2.0 * Robot.RadarMaxResolution;
                }
            }
            State.ScanHeading = NormalizeAngle(State.ScanHeading);

            // check if a target was found that is neither too close, nor too far
            if(State.TargetRange != null) {

                // fire at target
                FireMissile(State.ScanHeading, State.TargetRange.Value);

                // reset sweep tracker to indicate we found something
                State.NoHitSweep = 0.0;
            }

            // check if we're not moving
            if((State.GotoX == null) || (State.GotoY == null)) {

                // check if we were hurt or having found a target after a full sweep
                if(WasHurt) {
                    LogInfo("Damage detected. Taking evasive action.");

                    // take evasive action!
                    State.GotoX = Game.CollisionRange + Random.NextDouble() * (Game.BoardWidth - 2.0 * Game.CollisionRange);
                    State.GotoY = Game.CollisionRange + Random.NextDouble() * (Game.BoardHeight - 2.0 * Game.CollisionRange);
                } else if((State.NoHitSweep >= 360.0) && ((State.GotoX == null) || (State.GotoY == null))) {
                    LogInfo("Nothing found in immediate surroundings. Moving to new location.");

                    // time to move to a new random location on the board
                    State.GotoX = Game.CollisionRange + Random.NextDouble() * (Game.BoardWidth - 2.0 * Game.CollisionRange);
                    State.GotoY = Game.CollisionRange + Random.NextDouble() * (Game.BoardHeight - 2.0 * Game.CollisionRange);
                }
            }

            // check if we're moving and have reached our destination
            if((State.GotoX != null) && (State.GotoY != null)) {

                // reset sweep tracker while moving
                State.NoHitSweep = 0.0;

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
