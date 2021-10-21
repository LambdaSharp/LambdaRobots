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
    public enum BotMissileType {

        /// <summary>
        /// 1,200 meters range, 250 m/s velocity, 0 direct hit bonus, 0 near hit bonus, 0 far hit bonus, 0 sec. reload (0 pts)
        /// </summary>
        Dart,

        /// <summary>
        /// 900 meters range, 200 m/s velocity, 1 direct hit bonus, 1 near hit bonus, 0 far hit bonus, 1 sec. reload (1 pt)
        /// </summary>
        Arrow,

        /// <summary>
        /// 700 meters range, 150 m/s velocity, 3 direct hit bonus, 2 near hit bonus, 1 far hit bonus, 2 sec. reload (2 pts)
        /// </summary>
        Javelin,

        /// <summary>
        /// 500 meters range, 100 m/s velocity, 6 direct hit bonus, 4 near hit bonus, 2 far hit bonus, 3 sec. reload (3 pts)
        /// </summary>
        Cannon,

        /// <summary>
        /// 350 meters range, 75 m/s velocity, 12 direct hit bonus, 8 near hit bonus, 4 far hit bonus, 5 sec. reload (4 pts)
        /// </summary>
        BFG
    }
}
