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

    public static class RobotBuild {

        //--- Class Methods ---
        public static BotInfo GetDefaultBuild(string gameId, int index)
            => new BotInfo {

                // robot state
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

                // robot characteristics
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

        public static bool TrySetRadar(BotRadarType radar, BotInfo robot) {
            switch(radar) {
            case BotRadarType.UltraShortRange:
                robot.RadarRange = 200.0f;
                robot.RadarMaxResolution = 45.0f;
                break;
            case BotRadarType.ShortRange:
                robot.RadarRange = 400.0f;
                robot.RadarMaxResolution = 20.0f;
                break;
            case BotRadarType.MidRange:
                robot.RadarRange = 600.0f;
                robot.RadarMaxResolution = 10.0f;
                break;
            case BotRadarType.LongRange:
                robot.RadarRange = 800.0f;
                robot.RadarMaxResolution = 8.0f;
                break;
            case BotRadarType.UltraLongRange:
                robot.RadarRange = 1000.0f;
                robot.RadarMaxResolution = 5.0f;
                break;
            default:
                return false;
            }
            return true;
        }

        public static bool TrySetEngine(BotEngineType engine, BotInfo robot) {
            switch(engine) {
            case BotEngineType.Economy:
                robot.MaxSpeed = 60.0f;
                robot.Acceleration = 7.0f;
                break;
            case BotEngineType.Compact:
                robot.MaxSpeed = 80.0f;
                robot.Acceleration = 8.0f;
                break;
            case BotEngineType.Standard:
                robot.MaxSpeed = 100.0f;
                robot.Acceleration = 10.0f;
                break;
            case BotEngineType.Large:
                robot.MaxSpeed = 120.0f;
                robot.Acceleration = 12.0f;
                break;
            case BotEngineType.ExtraLarge:
                robot.MaxSpeed = 140.0f;
                robot.Acceleration = 13.0f;
                break;
            default:
                return false;
            }
            return true;
        }

        public static bool TrySetArmor(BotArmorType armor, BotInfo robot) {
            switch(armor) {
            case BotArmorType.UltraLight:
                robot.DirectHitDamage = 50.0f;
                robot.NearHitDamage = 25.0f;
                robot.FarHitDamage = 12.0f;
                robot.CollisionDamage = 10.0f;
                robot.MaxSpeed += 35.0f;
                robot.Deceleration = 30.0f;
                break;
            case BotArmorType.Light:
                robot.DirectHitDamage = 16.0f;
                robot.NearHitDamage = 8.0f;
                robot.FarHitDamage = 4.0f;
                robot.CollisionDamage = 3.0f;
                robot.MaxSpeed += 25.0f;
                robot.Deceleration = 25.0f;
                break;
            case BotArmorType.Medium:
                robot.DirectHitDamage = 8.0f;
                robot.NearHitDamage = 4.0f;
                robot.FarHitDamage = 2.0f;
                robot.CollisionDamage = 2.0f;
                robot.MaxSpeed += 0.0f;
                robot.Deceleration = 20.0f;
                break;
            case BotArmorType.Heavy:
                robot.DirectHitDamage = 4.0f;
                robot.NearHitDamage = 2.0f;
                robot.FarHitDamage = 1.0f;
                robot.CollisionDamage = 1.0f;
                robot.MaxSpeed += -25.0f;
                robot.Deceleration = 15.0f;
                break;
            case BotArmorType.UltraHeavy:
                robot.DirectHitDamage = 2.0f;
                robot.NearHitDamage = 1.0f;
                robot.FarHitDamage = 0.0f;
                robot.CollisionDamage = 1.0f;
                robot.MaxSpeed += -45.0f;
                robot.Deceleration = 10.0f;
                break;
            default:
                return false;
            }
            return true;
        }

        public static bool TrySetMissile(BotMissileType missile, BotInfo robot) {
            switch(missile) {
            case BotMissileType.Dart:
                robot.MissileRange = 1200.0f;
                robot.MissileVelocity = 250.0f;
                robot.MissileDirectHitDamageBonus = 0.0f;
                robot.MissileNearHitDamageBonus = 0.0f;
                robot.MissileFarHitDamageBonus = 0.0f;
                robot.MissileReloadCooldown = 0.0f;
                break;
            case BotMissileType.Arrow:
                robot.MissileRange = 900.0f;
                robot.MissileVelocity = 200.0f;
                robot.MissileDirectHitDamageBonus = 1.0f;
                robot.MissileNearHitDamageBonus = 1.0f;
                robot.MissileFarHitDamageBonus = 0.0f;
                robot.MissileReloadCooldown = 1.0f;
                break;
            case BotMissileType.Javelin:
                robot.MissileRange = 700.0f;
                robot.MissileVelocity = 150.0f;
                robot.MissileDirectHitDamageBonus = 3.0f;
                robot.MissileNearHitDamageBonus = 2.0f;
                robot.MissileFarHitDamageBonus = 1.0f;
                robot.MissileReloadCooldown = 2.0f;
                break;
            case BotMissileType.Cannon:
                robot.MissileRange = 500.0f;
                robot.MissileVelocity = 100.0f;
                robot.MissileDirectHitDamageBonus = 6.0f;
                robot.MissileNearHitDamageBonus = 4.0f;
                robot.MissileFarHitDamageBonus = 2.0f;
                robot.MissileReloadCooldown = 3.0f;
                break;
            case BotMissileType.BFG:
                robot.MissileRange = 350.0f;
                robot.MissileVelocity = 75.0f;
                robot.MissileDirectHitDamageBonus = 12.0f;
                robot.MissileNearHitDamageBonus = 8.0f;
                robot.MissileFarHitDamageBonus = 4.0f;
                robot.MissileReloadCooldown = 5.0f;
                break;
            default:
                return false;
            }
            return true;
        }
    }
}
