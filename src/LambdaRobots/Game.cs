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

namespace LambdaRobots {

    public class GameInfo {

        //--- Properties ---

        /// <summary>
        /// Unique Game ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Width of the game board.
        /// </summary>
        public double BoardWidth { get; set; }

        /// <summary>
        /// Height of the game board.
        /// </summary>
        public double BoardHeight { get; set; }

        /// <summary>
        /// Number of seconds elapsed per game turn.
        /// </summary>
        public double SecondsPerTurn { get; set; }

        /// <summary>
        /// Distance for missile impact to count as direct hit.
        /// </summary>
        public double DirectHitRange;

        /// <summary>
        /// Distance for missile impact to count as near hit.
        /// </summary>
        public double NearHitRange;

        /// <summary>
        /// Distance for missile impact to count as far hit.
        /// </summary>
        public double FarHitRange;

        /// <summary>
        /// Distance between robots to count as a collision.
        /// </summary>
        public double CollisionRange;

        /// <summary>
        /// Current game turn. Starts at `1`.
        /// </summary>
        /// <value></value>
        public int GameTurn { get; set; }

        /// <summary>
        /// Maximum number of turns before the game ends in a draw.
        /// </summary>
        public int MaxGameTurns { get; set; }

        /// <summary>
        /// Maximum number of build points a robot can use.
        /// </summary>
        /// <value></value>
        public int MaxBuildPoints { get; set; }

        /// <summary>
        /// URL for game server API.
        /// </summary>
        /// <value></value>
        public string ApiUrl { get; set; }
    }
}
