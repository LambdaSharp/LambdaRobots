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

namespace LambdaRobots.Protocol {

    [JsonConverter(typeof(StringEnumConverter))]
    public enum LambdaRobotRadarType {

        /// <summary>
        /// 200 meters range, 45 degrees resolution (0 pts)
        /// </summary>
        UltraShortRange,

        /// <summary>
        /// 400 meters range, 20 degrees resolution (1 pt)
        /// </summary>
        ShortRange,

        /// <summary>
        /// 600 meters range, 10 degrees resolution (2 pts)
        /// </summary>
        MidRange,

        /// <summary>
        /// 800 meters range, 8 degrees resolution (3 pts)
        /// </summary>
        LongRange,

        /// <summary>
        /// 1,000 meters range, 5 degrees resolution (4 pts)
        /// </summary>
        UltraLongRange
    }


    [JsonConverter(typeof(StringEnumConverter))]
    public enum LambdaRobotEngineType {

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

    [JsonConverter(typeof(StringEnumConverter))]
    public enum LambdaRobotArmorType {

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

    [JsonConverter(typeof(StringEnumConverter))]
    public enum LambdaRobotMissileType {

        /// <summary>
        /// 350 meters range, 250 m/s velocity, 0 direct hit bonus, 0 near hit bonus, 0 far hit bonus, 0 sec. reload (0 pts)
        /// </summary>
        Dart,

        /// <summary>
        /// 500 meters range, 200 m/s velocity, 1 direct hit bonus, 1 near hit bonus, 0 far hit bonus, 1 sec. reload (1 pt)
        /// </summary>
        Arrow,

        /// <summary>
        /// 700 meters range, 150 m/s velocity, 3 direct hit bonus, 2 near hit bonus, 1 far hit bonus, 2 sec. reload (2 pts)
        /// </summary>
        Javelin,

        /// <summary>
        /// 900 meters range, 100 m/s velocity, 6 direct hit bonus, 4 near hit bonus, 2 far hit bonus, 3 sec. reload (3 pts)
        /// </summary>
        Cannon,

        /// <summary>
        /// 1,200 meters range, 75 m/s velocity, 12 direct hit bonus, 8 near hit bonus, 4 far hit bonus, 5 sec. reload (4 pts)
        /// </summary>
        BFG
    }

    public class LambdaRobotBuild  {

        //--- Properties ---

        /// <summary>
        /// Name of Lambda-Robot.
        /// </summary>
        /// <value></value>
        public string Name { get; set; }

        /// <summary>
        /// Type of Radar. Affects radar scan range and resolution.
        /// </summary>
        /// <value></value>
        public LambdaRobotRadarType Radar { get; set; } = LambdaRobotRadarType.MidRange;

        /// <summary>
        /// Type of Engine. Affects max. speed and acceleration.
        /// </summary>
        /// <value></value>
        public LambdaRobotEngineType Engine { get; set; } = LambdaRobotEngineType.Standard;

        /// <summary>
        /// Type of Armor. Affects hit damage, collision damage, max. speed, and deceleration.
        /// </summary>
        /// <value></value>
        public LambdaRobotArmorType Armor { get; set; } = LambdaRobotArmorType.Medium;

        /// <summary>
        /// Type of Missile. Affects weapon range, velocity, hit damage, and reload speed.
        /// </summary>
        /// <value></value>
        public LambdaRobotMissileType Missile { get; set; } = LambdaRobotMissileType.Javelin;
    }
}
