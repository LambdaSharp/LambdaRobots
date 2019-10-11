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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Challenge.LambdaRobots.Protocol {

    [JsonConverter(typeof(StringEnumConverter))]
    public enum LambdaRobotRadar {
        UltraShortRange,
        ShortRange,
        MidRange,
        LongRange,
        UltraLongRange
    }


    [JsonConverter(typeof(StringEnumConverter))]
    public enum LambdaRobotEngine {
        Economy,
        Compact,
        Standard,
        Large,
        ExtraLarge
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum LambdaRobotArmor {
        UltraLight,
        Light,
        Medium,
        Heavy,
        UltraHeavy
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum LambdaRobotMissile {
        Dart,
        Arrow,
        Javelin,
        Cannon,
        BFG
    }

    public class LambdaRobotBuild  {

        //--- Properties ---
        public string Name { get; set; }
        public LambdaRobotRadar Radar { get; set; } = LambdaRobotRadar.MidRange;
        public LambdaRobotEngine Engine { get; set; } = LambdaRobotEngine.Standard;
        public LambdaRobotArmor Armor { get; set; } = LambdaRobotArmor.Medium;
        public LambdaRobotMissile Missile { get; set; } = LambdaRobotMissile.Javelin;
    }
}
