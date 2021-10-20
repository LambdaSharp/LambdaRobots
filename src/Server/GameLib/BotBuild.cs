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

namespace LambdaRobots.Server {

    public static class BotBuild {

        //--- Class Methods ---
        public static BotInfo GetDefaultBuild(string gameId, int index)
            => new BotInfo {

                // bot state
                Index = index,
                Id = $"{gameId}:R{index}",
                Status = BotStatus.Alive,
                X = 0.0f,
                Y = 0.0f,
                Speed = 0.0f,
                Heading = 0.0f,
                Damage = 0.0f,
                ReloadCoolDown = 0.0f,
                TimeOfDeathGameTurn = -1,
                TotalDamageDealt = 0.0f,
                TotalKills = 0,
                TotalMissileFiredCount = 0,
                TotalMissileHitCount = 0,
                TotalTravelDistance = 0.0f,
                TotalCollisions = 0,

                // action
                TargetHeading = 0.0f,
                TargetSpeed = 0.0f,

                // bot characteristics
                MaxSpeed = 100.0f,
                Acceleration = 10.0f,
                Deceleration = 20.0f,
                MaxTurnSpeed = 50.0f,
                RadarRange = 600.0f,
                RadarMaxResolution = 10.0f,
                MaxDamage = 100.0f,
                CollisionDamage = 2.0f,
                DirectHitDamage = 8.0f,
                NearHitDamage = 4.0f,
                FarHitDamage = 2.0f,

                // missile characteristics
                MissileReloadCooldown = 2.0f,
                MissileVelocity = 150.0f,
                MissileRange = 700.0f,
                MissileDirectHitDamageBonus = 3.0f,
                MissileNearHitDamageBonus = 2.1f,
                MissileFarHitDamageBonus = 1.0f
            };

        public static bool TrySetRadar(BotRadarType radar, BotInfo bot) {
            switch(radar) {
            case BotRadarType.UltraShortRange:
                bot.RadarRange = 200.0f;
                bot.RadarMaxResolution = 45.0f;
                break;
            case BotRadarType.ShortRange:
                bot.RadarRange = 400.0f;
                bot.RadarMaxResolution = 20.0f;
                break;
            case BotRadarType.MidRange:
                bot.RadarRange = 600.0f;
                bot.RadarMaxResolution = 10.0f;
                break;
            case BotRadarType.LongRange:
                bot.RadarRange = 800.0f;
                bot.RadarMaxResolution = 8.0f;
                break;
            case BotRadarType.UltraLongRange:
                bot.RadarRange = 1000.0f;
                bot.RadarMaxResolution = 5.0f;
                break;
            default:
                return false;
            }
            return true;
        }

        public static bool TrySetEngine(BotEngineType engine, BotInfo bot) {
            switch(engine) {
            case BotEngineType.Economy:
                bot.MaxSpeed = 60.0f;
                bot.Acceleration = 7.0f;
                break;
            case BotEngineType.Compact:
                bot.MaxSpeed = 80.0f;
                bot.Acceleration = 8.0f;
                break;
            case BotEngineType.Standard:
                bot.MaxSpeed = 100.0f;
                bot.Acceleration = 10.0f;
                break;
            case BotEngineType.Large:
                bot.MaxSpeed = 120.0f;
                bot.Acceleration = 12.0f;
                break;
            case BotEngineType.ExtraLarge:
                bot.MaxSpeed = 140.0f;
                bot.Acceleration = 13.0f;
                break;
            default:
                return false;
            }
            return true;
        }

        public static bool TrySetArmor(BotArmorType armor, BotInfo bot) {
            switch(armor) {
            case BotArmorType.UltraLight:
                bot.DirectHitDamage = 50.0f;
                bot.NearHitDamage = 25.0f;
                bot.FarHitDamage = 12.0f;
                bot.CollisionDamage = 10.0f;
                bot.MaxSpeed += 35.0f;
                bot.Deceleration = 30.0f;
                break;
            case BotArmorType.Light:
                bot.DirectHitDamage = 16.0f;
                bot.NearHitDamage = 8.0f;
                bot.FarHitDamage = 4.0f;
                bot.CollisionDamage = 3.0f;
                bot.MaxSpeed += 25.0f;
                bot.Deceleration = 25.0f;
                break;
            case BotArmorType.Medium:
                bot.DirectHitDamage = 8.0f;
                bot.NearHitDamage = 4.0f;
                bot.FarHitDamage = 2.0f;
                bot.CollisionDamage = 2.0f;
                bot.MaxSpeed += 0.0f;
                bot.Deceleration = 20.0f;
                break;
            case BotArmorType.Heavy:
                bot.DirectHitDamage = 4.0f;
                bot.NearHitDamage = 2.0f;
                bot.FarHitDamage = 1.0f;
                bot.CollisionDamage = 1.0f;
                bot.MaxSpeed += -25.0f;
                bot.Deceleration = 15.0f;
                break;
            case BotArmorType.UltraHeavy:
                bot.DirectHitDamage = 2.0f;
                bot.NearHitDamage = 1.0f;
                bot.FarHitDamage = 0.0f;
                bot.CollisionDamage = 1.0f;
                bot.MaxSpeed += -45.0f;
                bot.Deceleration = 10.0f;
                break;
            default:
                return false;
            }
            return true;
        }

        public static bool TrySetMissile(BotMissileType missile, BotInfo bot) {
            switch(missile) {
            case BotMissileType.Dart:
                bot.MissileRange = 1200.0f;
                bot.MissileVelocity = 250.0f;
                bot.MissileDirectHitDamageBonus = 0.0f;
                bot.MissileNearHitDamageBonus = 0.0f;
                bot.MissileFarHitDamageBonus = 0.0f;
                bot.MissileReloadCooldown = 0.0f;
                break;
            case BotMissileType.Arrow:
                bot.MissileRange = 900.0f;
                bot.MissileVelocity = 200.0f;
                bot.MissileDirectHitDamageBonus = 1.0f;
                bot.MissileNearHitDamageBonus = 1.0f;
                bot.MissileFarHitDamageBonus = 0.0f;
                bot.MissileReloadCooldown = 1.0f;
                break;
            case BotMissileType.Javelin:
                bot.MissileRange = 700.0f;
                bot.MissileVelocity = 150.0f;
                bot.MissileDirectHitDamageBonus = 3.0f;
                bot.MissileNearHitDamageBonus = 2.0f;
                bot.MissileFarHitDamageBonus = 1.0f;
                bot.MissileReloadCooldown = 2.0f;
                break;
            case BotMissileType.Cannon:
                bot.MissileRange = 500.0f;
                bot.MissileVelocity = 100.0f;
                bot.MissileDirectHitDamageBonus = 6.0f;
                bot.MissileNearHitDamageBonus = 4.0f;
                bot.MissileFarHitDamageBonus = 2.0f;
                bot.MissileReloadCooldown = 3.0f;
                break;
            case BotMissileType.BFG:
                bot.MissileRange = 350.0f;
                bot.MissileVelocity = 75.0f;
                bot.MissileDirectHitDamageBonus = 12.0f;
                bot.MissileNearHitDamageBonus = 8.0f;
                bot.MissileFarHitDamageBonus = 4.0f;
                bot.MissileReloadCooldown = 5.0f;
                break;
            default:
                return false;
            }
            return true;
        }
    }
}
