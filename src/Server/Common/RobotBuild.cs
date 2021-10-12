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
        public static LambdaRobot GetDefaultBuild(string gameId, int index)
            => new LambdaRobot {

                // robot state
                Index = index,
                Id = $"{gameId}:R{index}",
                Status = LambdaRobotStatus.Alive,
                X = 0.0,
                Y = 0.0,
                Speed = 0.0,
                Heading = 0.0,
                Damage = 0.0,
                ReloadCoolDown = 0.0,
                TimeOfDeathGameTurn = -1,
                TotalDamageDealt = 0.0,
                TotalKills = 0,
                TotalMissileFiredCount = 0,
                TotalMissileHitCount = 0,
                TotalTravelDistance = 0.0,
                TotalCollisions = 0,

                // action
                TargetHeading = 0.0,
                TargetSpeed = 0.0,

                // robot characteristics
                MaxSpeed = 100.0,
                Acceleration = 10.0,
                Deceleration = 20.0,
                MaxTurnSpeed = 50.0,
                RadarRange = 600.0,
                RadarMaxResolution = 10.0,
                MaxDamage = 100.0,
                CollisionDamage = 2.0,
                DirectHitDamage = 8.0,
                NearHitDamage = 4.0,
                FarHitDamage = 2.0,

                // missile characteristics
                MissileReloadCooldown = 2.0,
                MissileVelocity = 150.0,
                MissileRange = 700.0,
                MissileDirectHitDamageBonus = 3.0,
                MissileNearHitDamageBonus = 2.1,
                MissileFarHitDamageBonus = 1.0
            };

        public static bool TrySetRadar(LambdaRobotRadarType radar, LambdaRobot robot) {
            switch(radar) {
            case LambdaRobotRadarType.UltraShortRange:
                robot.RadarRange = 200.0;
                robot.RadarMaxResolution = 45.0;
                break;
            case LambdaRobotRadarType.ShortRange:
                robot.RadarRange = 400.0;
                robot.RadarMaxResolution = 20.0;
                break;
            case LambdaRobotRadarType.MidRange:
                robot.RadarRange = 600.0;
                robot.RadarMaxResolution = 10.0;
                break;
            case LambdaRobotRadarType.LongRange:
                robot.RadarRange = 800.0;
                robot.RadarMaxResolution = 8.0;
                break;
            case LambdaRobotRadarType.UltraLongRange:
                robot.RadarRange = 1000.0;
                robot.RadarMaxResolution = 5.0;
                break;
            default:
                return false;
            }
            return true;
        }

        public static bool TrySetEngine(LambdaRobotEngineType engine, LambdaRobot robot) {
            switch(engine) {
            case LambdaRobotEngineType.Economy:
                robot.MaxSpeed = 60.0;
                robot.Acceleration = 7.0;
                break;
            case LambdaRobotEngineType.Compact:
                robot.MaxSpeed = 80.0;
                robot.Acceleration = 8.0;
                break;
            case LambdaRobotEngineType.Standard:
                robot.MaxSpeed = 100.0;
                robot.Acceleration = 10.0;
                break;
            case LambdaRobotEngineType.Large:
                robot.MaxSpeed = 120.0;
                robot.Acceleration = 12.0;
                break;
            case LambdaRobotEngineType.ExtraLarge:
                robot.MaxSpeed = 140.0;
                robot.Acceleration = 13.0;
                break;
            default:
                return false;
            }
            return true;
        }

        public static bool TrySetArmor(LambdaRobotArmorType armor, LambdaRobot robot) {
            switch(armor) {
            case LambdaRobotArmorType.UltraLight:
                robot.DirectHitDamage = 50.0;
                robot.NearHitDamage = 25.0;
                robot.FarHitDamage = 12.0;
                robot.CollisionDamage = 10.0;
                robot.MaxSpeed += 35.0;
                robot.Deceleration = 30.0;
                break;
            case LambdaRobotArmorType.Light:
                robot.DirectHitDamage = 16.0;
                robot.NearHitDamage = 8.0;
                robot.FarHitDamage = 4.0;
                robot.CollisionDamage = 3.0;
                robot.MaxSpeed += 25.0;
                robot.Deceleration = 25.0;
                break;
            case LambdaRobotArmorType.Medium:
                robot.DirectHitDamage = 8.0;
                robot.NearHitDamage = 4.0;
                robot.FarHitDamage = 2.0;
                robot.CollisionDamage = 2.0;
                robot.MaxSpeed += 0.0;
                robot.Deceleration = 20.0;
                break;
            case LambdaRobotArmorType.Heavy:
                robot.DirectHitDamage = 4.0;
                robot.NearHitDamage = 2.0;
                robot.FarHitDamage = 1.0;
                robot.CollisionDamage = 1.0;
                robot.MaxSpeed += -25.0;
                robot.Deceleration = 15.0;
                break;
            case LambdaRobotArmorType.UltraHeavy:
                robot.DirectHitDamage = 2.0;
                robot.NearHitDamage = 1.0;
                robot.FarHitDamage = 0.0;
                robot.CollisionDamage = 1.0;
                robot.MaxSpeed += -45.0;
                robot.Deceleration = 10.0;
                break;
            default:
                return false;
            }
            return true;
        }

        public static bool TrySetMissile(LambdaRobotMissileType missile, LambdaRobot robot) {
            switch(missile) {
            case LambdaRobotMissileType.Dart:
                robot.MissileRange = 1200.0;
                robot.MissileVelocity = 250.0;
                robot.MissileDirectHitDamageBonus = 0.0;
                robot.MissileNearHitDamageBonus = 0.0;
                robot.MissileFarHitDamageBonus = 0.0;
                robot.MissileReloadCooldown = 0.0;
                break;
            case LambdaRobotMissileType.Arrow:
                robot.MissileRange = 900.0;
                robot.MissileVelocity = 200.0;
                robot.MissileDirectHitDamageBonus = 1.0;
                robot.MissileNearHitDamageBonus = 1.0;
                robot.MissileFarHitDamageBonus = 0.0;
                robot.MissileReloadCooldown = 1.0;
                break;
            case LambdaRobotMissileType.Javelin:
                robot.MissileRange = 700.0;
                robot.MissileVelocity = 150.0;
                robot.MissileDirectHitDamageBonus = 3.0;
                robot.MissileNearHitDamageBonus = 2.0;
                robot.MissileFarHitDamageBonus = 1.0;
                robot.MissileReloadCooldown = 2.0;
                break;
            case LambdaRobotMissileType.Cannon:
                robot.MissileRange = 500.0;
                robot.MissileVelocity = 100.0;
                robot.MissileDirectHitDamageBonus = 6.0;
                robot.MissileNearHitDamageBonus = 4.0;
                robot.MissileFarHitDamageBonus = 2.0;
                robot.MissileReloadCooldown = 3.0;
                break;
            case LambdaRobotMissileType.BFG:
                robot.MissileRange = 350.0;
                robot.MissileVelocity = 75.0;
                robot.MissileDirectHitDamageBonus = 12.0;
                robot.MissileNearHitDamageBonus = 8.0;
                robot.MissileFarHitDamageBonus = 4.0;
                robot.MissileReloadCooldown = 5.0;
                break;
            default:
                return false;
            }
            return true;
        }
    }
}
