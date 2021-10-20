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

namespace LambdaRobots.Bot.Model {

    public class GetBuildResponse  {

        //--- Properties ---

        /// <summary>
        /// Name of bot.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of Radar. Affects radar scan range and resolution.
        /// </summary>
        public BotRadarType Radar { get; set; } = BotRadarType.MidRange;

        /// <summary>
        /// Type of Engine. Affects max. speed and acceleration.
        /// </summary>
        public BotEngineType Engine { get; set; } = BotEngineType.Standard;

        /// <summary>
        /// Type of Armor. Affects hit damage, collision damage, max. speed, and deceleration.
        /// </summary>
        public BotArmorType Armor { get; set; } = BotArmorType.Medium;

        /// <summary>
        /// Type of Missile. Affects weapon range, velocity, hit damage, and reload speed.
        /// </summary>
        public BotMissileType Missile { get; set; } = BotMissileType.Javelin;

        /// <summary>
        /// Internal starting state for the bot.
        /// </summary>
        public string InternalStartState { get; set; }
    }
}
