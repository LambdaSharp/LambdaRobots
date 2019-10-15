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

namespace LambdaRobots {

    [JsonConverter(typeof(StringEnumConverter))]
    public enum LambdaRobotStatus {

        /// <summary>
        /// Status has not been initialized.
        /// </summary>
        Undefined,

        /// <summary>
        /// Robot is alive.
        /// </summary>
        Alive,

        /// <summary>
        /// Robot is dead.
        /// </summary>
        Dead
    }

    public class LambdaRobot {

        //--- Fields ---

        /// <summary>
        /// Index position of robot. Starts at `0`.
        /// </summary>
        public int Index;

        /// <summary>
        /// Globally unique robot ID.
        /// </summary>
        public string Id;

        /// <summary>
        /// Robot display name.
        /// </summary>
        public string Name;

        // current state

        /// <summary>
        /// Robot status. Either `Alive` or `Dead`.
        /// </summary>
        public LambdaRobotStatus Status;

        /// <summary>
        /// Robot horizontal position.
        /// </summary>
        public double X;

        /// <summary>
        /// Robot vertical position.
        /// </summary>
        public double Y;

        /// <summary>
        /// Robot speed. Between `0` and `MaxSpeed`. (m/s)
        /// </summary>
        public double Speed;

        /// <summary>
        /// Robot heading. Between `0` and `360`. (degrees)
        /// </summary>
        public double Heading;

        /// <summary>
        /// Accumulated robot damage. Between `0` and `MaxDamage`.
        /// </summary>
        public double Damage;

        /// <summary>
        /// Number of seconds before the robot can fire another missile. (s)
        /// </summary>
        public double ReloadCoolDown;

        // target state

        /// <summary>
        /// Desired speed for robot. The current speed will be adjusted accordingly every turn. (m/s)
        /// </summary>
        public double TargetSpeed;

        /// <summary>
        /// Desired heading for robot. The heading will be adjusted accordingly every turn. (degrees)
        /// </summary>
        public double TargetHeading;

        // robot stats

        /// <summary>
        /// Total distance traveled by robot during the match. (m)
        /// </summary>
        public double TotalTravelDistance;

        /// <summary>
        /// Number of missiles fired by robot during match.
        /// </summary>
        public int TotalMissileFiredCount;

        /// <summary>
        /// Number of missiles that hit a target during match.
        /// </summary>
        public int TotalMissileHitCount;

        /// <summary>
        /// Number of confirmed kills during match.
        /// </summary>
        public int TotalKills;

        /// <summary>
        /// Damage dealt by missiles during match.
        /// </summary>
        public double TotalDamageDealt;

        /// <summary>
        /// Number of collisions with walls or other robots during match.
        /// </summary>
        public int TotalCollisions;

        /// <summary>
        /// Game turn during which the robot died. `-1` if robot is alive.
        /// </summary>
        public int TimeOfDeathGameTurn;

        // robot characteristics

        /// <summary>
        /// Maximum speed for robot. (m/s)
        /// </summary>
        public double MaxSpeed;

        /// <summary>
        /// Acceleration when speeding up. (m/s^2)
        /// </summary>
        public double Acceleration;

        /// <summary>
        /// Deceleration when speeding up. (m/s^2)
        /// </summary>
        public double Deceleration;

        /// <summary>
        /// Maximum speed at which the robot can change heading without a sudden stop. (m/s)
        /// </summary>
        public double MaxTurnSpeed;

        /// <summary>
        /// Maximum range at which the radar can detect an opponent. (m)
        /// </summary>
        public double RadarRange;

        /// <summary>
        /// Maximum degrees the radar can scan beyond the selected heading. (degrees)
        /// </summary>
        public double RadarMaxResolution;

        /// <summary>
        /// Maximum damage before the robot is destroyed.
        /// </summary>
        public double MaxDamage;

        /// <summary>
        /// Amount of damage the robot receives from a collision.
        /// </summary>
        public double CollisionDamage;

        /// <summary>
        /// Amount of damage the robot receives from a direct hit.
        /// </summary>
        public double DirectHitDamage;

        /// <summary>
        /// Amount of damage the robot receives from a near hit.
        /// </summary>
        public double NearHitDamage;

        /// <summary>
        /// Amount of damage the robot receives from a far hit.
        /// </summary>
        public double FarHitDamage;

        // missile characteristics

        /// <summary>
        /// Number of seconds between each missile launch. (s)
        /// </summary>
        public double MissileReloadCooldown;

        /// <summary>
        /// Travel velocity for missile. (m/s)
        /// </summary>
        public double MissileVelocity;

        /// <summary>
        /// Maximum range for missile. (m)
        /// </summary>
        public double MissileRange;

        /// <summary>
        /// Bonus damage on target for a direct hit.
        /// </summary>
        public double MissileDirectHitDamageBonus;

        /// <summary>
        /// Bonus damage on target for a near hit.
        /// </summary>
        public double MissileNearHitDamageBonus;

        /// <summary>
        /// Bonus damage on target for a far hit.
        /// </summary>
        public double MissileFarHitDamageBonus;
    }
}
