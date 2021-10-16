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
    public enum BotEngineType {

        /// <summary>
        /// 60 m/s max. speed, 7 m/s^2 acceleration (0 pts)
        /// </summary>
        Economy,

        /// <summary>
        ///  80 m/s max. speed, 8 m/s^2 acceleration (1 pt)
        /// </summary>
        Compact,

        /// <summary>
        /// 100 m/s max. speed, 10 m/s^2 acceleration (2 pts)
        /// </summary>
        Standard,

        /// <summary>
        /// 120 m/s max. speed, 12 m/s^2 acceleration (3 pts)
        /// </summary>
        Large,

        /// <summary>
        /// 140 m/s max. speed, 13 m/s^2 acceleration (4 pts)
        /// </summary>
        ExtraLarge
    }
}
