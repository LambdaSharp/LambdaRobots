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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Challenge.LambdaRobots.Common {

    [JsonConverter(typeof(StringEnumConverter))]
    public enum RobotState {
        Undefined,
        Alive,
        Dead
    }

    public class Robot {

        //--- Fields ---
        public string Id;
        public string Name;

        // current state
        public RobotState State;
        public double X;
        public double Y;
        public double Speed;
        public double Heading;
        public double Damage;
        public double ReloadDelay;

        // target state
        public double TargetSpeed;
        public double TargetHeading;

        // robot stats
        public double TotalTravelDistance;
        public int TotalMissileFiredCount;
        public int TotalMissileHitCount;
        public int TotalScanCount;
        public int TotalKills;
        public double TotalDamageDealt;
        public DateTime? TimeOfDeath;

        // robot characteristics
        public double MaxSpeed;
        public double Acceleration;
        public double Deceleration;
        public double MaxTurnSpeed;
        public double ScannerRange;
        public double ScannerResolution;
        public double MaxDamage;
        public double CollisionDamage;
        public double DirectHitDamage;
        public double NearHitDamage;
        public double FarHitDamage;

        // missile characteristics
        public double MissileReloadDelay;
        public double MissileSpeed;
        public double MissileRange;
        public double MissileDirectHitDamageBonus;
        public double MissileNearHitDamageBonus;
        public double MissileFarHitDamageBonus;
    }
}
