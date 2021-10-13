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

using System.Text.Json.Serialization;

namespace LambdaRobots {

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BotArmorType {

        /// <summary>
        /// 50 direct hit, 25 near hit, 12 far hit, 10 collision, +35 m/s max. speed, 30 m/s^2 deceleration (0 pts)
        /// </summary>
        UltraLight,

        /// <summary>
        /// 16 direct hit, 8 near hit, 4 far hit, 3 collision, +25 m/s max. speed, 25 m/s^2 deceleration (1 pt)
        /// </summary>
        Light,

        /// <summary>
        /// 8 direct hit, 4 near hit, 2 far hit, 2 collision, +0 m/s max. speed, 20 m/s^2 deceleration (2 pts)
        /// </summary>
        Medium,

        /// <summary>
        /// 4 direct hit, 2 near hit, 1 far hit, 1 collision, -25 m/s max. speed, 15 m/s^2 deceleration (3 pts)
        /// </summary>
        Heavy,

        /// <summary>
        /// 2 direct hit, 1 near hit, 0 far hit, 1 collision, -45 m/s max. speed, 10 m/s^2 deceleration (4 pts)
        /// </summary>
        UltraHeavy
    }
}
