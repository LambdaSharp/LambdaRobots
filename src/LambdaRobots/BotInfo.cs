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

using System;

namespace LambdaRobots {

    public sealed class BotInfo {

        //--- Properties ---

        /// <summary>
        /// Index position of bot. Starts at `0`.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Globally unique bot ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// bot display name.
        /// </summary>
        public string Name { get; set; }

        // current state

        /// <summary>
        /// Bot status. Either `Alive` or `Dead`.
        /// </summary>
        public BotStatus Status { get; set; }

        /// <summary>
        /// Timestamp of when the bot status was last updated.
        /// </summary>
        public DateTimeOffset LastStatusUpdate { get; set; }

        /// <summary>
        /// Bot horizontal position.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Bot vertical position.
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Bot speed. Between `0` and `MaxSpeed`. (m/s)
        /// </summary>
        public float Speed { get; set; }

        /// <summary>
        /// Bot heading. Between `0` and `360`. (degrees)
        /// </summary>
        public float Heading { get; set; }

        /// <summary>
        /// Accumulated bot damage. Between `0` and `MaxDamage`.
        /// </summary>
        public float Damage { get; set; }

        /// <summary>
        /// Number of seconds before the bot can fire another missile. (s)
        /// </summary>
        public float ReloadCoolDown { get; set; }

        // target state

        /// <summary>
        /// Desired speed for bot. The current speed will be adjusted accordingly every turn. (m/s)
        /// </summary>
        public float TargetSpeed { get; set; }

        /// <summary>
        /// Desired heading for bot. The heading will be adjusted accordingly every turn. (degrees)
        /// </summary>
        public float TargetHeading { get; set; }

        // bot stats

        /// <summary>
        /// Total distance traveled by bot during the match. (m)
        /// </summary>
        public float TotalTravelDistance { get; set; }

        /// <summary>
        /// Number of missiles fired by bot during match.
        /// </summary>
        public int TotalMissileFiredCount { get; set; }

        /// <summary>
        /// Number of missiles that hit a target during match.
        /// </summary>
        public int TotalMissileHitCount { get; set; }

        /// <summary>
        /// Number of confirmed kills during match.
        /// </summary>
        public int TotalKills { get; set; }

        /// <summary>
        /// Damage dealt by missiles during match.
        /// </summary>
        public float TotalDamageDealt { get; set; }

        /// <summary>
        /// Number of collisions with walls or other bots during match.
        /// </summary>
        public int TotalCollisions { get; set; }

        /// <summary>
        /// Game turn during which the bot died. `-1` if bot is alive.
        /// </summary>
        public int TimeOfDeathGameTurn { get; set; }

        // bot characteristics

        /// <summary>
        /// Maximum speed for bot. (m/s)
        /// </summary>
        public float MaxSpeed { get; set; }

        /// <summary>
        /// Acceleration when speeding up. (m/s^2)
        /// </summary>
        public float Acceleration { get; set; }

        /// <summary>
        /// Deceleration when speeding up. (m/s^2)
        /// </summary>
        public float Deceleration { get; set; }

        /// <summary>
        /// Maximum speed at which the bot can change heading without a sudden stop. (m/s)
        /// </summary>
        public float MaxTurnSpeed { get; set; }

        /// <summary>
        /// Maximum range at which the radar can detect an opponent. (m)
        /// </summary>
        public float RadarRange { get; set; }

        /// <summary>
        /// Maximum degrees the radar can scan beyond the selected heading. (degrees)
        /// </summary>
        public float RadarMaxResolution { get; set; }

        /// <summary>
        /// Maximum damage before the bot is destroyed.
        /// </summary>
        public float MaxDamage { get; set; }

        /// <summary>
        /// Amount of damage the bot receives from a collision.
        /// </summary>
        public float CollisionDamage { get; set; }

        /// <summary>
        /// Amount of damage the bot receives from a direct hit.
        /// </summary>
        public float DirectHitDamage { get; set; }

        /// <summary>
        /// Amount of damage the bot receives from a near hit.
        /// </summary>
        public float NearHitDamage { get; set; }

        /// <summary>
        /// Amount of damage the bot receives from a far hit.
        /// </summary>
        public float FarHitDamage { get; set; }

        // missile characteristics

        /// <summary>
        /// Number of seconds between each missile launch. (s)
        /// </summary>
        public float MissileReloadCooldown { get; set; }

        /// <summary>
        /// Travel velocity for missile. (m/s)
        /// </summary>
        public float MissileVelocity { get; set; }

        /// <summary>
        /// Maximum range for missile. (m)
        /// </summary>
        public float MissileRange { get; set; }

        /// <summary>
        /// Bonus damage on target for a direct hit.
        /// </summary>
        public float MissileDirectHitDamageBonus { get; set; }

        /// <summary>
        /// Bonus damage on target for a near hit.
        /// </summary>
        public float MissileNearHitDamageBonus { get; set; }

        /// <summary>
        /// Bonus damage on target for a far hit.
        /// </summary>
        public float MissileFarHitDamageBonus { get; set; }

        /// <summary>
        /// Internal state.
        /// </summary>
        public string InternalState { get; set; }
    }
}
