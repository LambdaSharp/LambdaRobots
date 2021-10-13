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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LambdaRobots;
using LambdaRobots.Bot.Model;
using LambdaRobots.Server;

namespace Test.LambdaRobots.Server {

    public abstract class _Init : IGameDependencyProvider {

        //--- Fields ---
        protected IGameDependencyProvider _provider;
        protected Dictionary<string, List<Func<GetActionResponse>>> _robotActions = new Dictionary<string, List<Func<GetActionResponse>>>();
        protected readonly Random _random = new Random();

        //--- Methods ---
        protected Game NewGame() => new Game {
            Id = "Test",
            BoardWidth = 1000.0,
            BoardHeight = 1000.0,
            SecondsPerTurn = 1.0,
            DirectHitRange = 5.0,
            NearHitRange = 20.0,
            FarHitRange = 40.0,
            CollisionRange = 5.0,
            MinRobotStartDistance = 100.0,
            RobotTimeoutSeconds = 10.0,
            CurrentGameTurn = 0,
            MaxTurns = 300,
            MaxBuildPoints = 8
        };

        protected BotInfo NewRobot(string id, double x, double y) => new BotInfo {

            // robot state
            Id = id,
            Name = id,
            Status = BotStatus.Alive,
            X = x,
            Y = y,
            Speed = 0.0,
            Heading = 0.0,
            TotalTravelDistance = 0.0,
            Damage = 0.0,
            ReloadCoolDown = 0.0,
            TotalMissileFiredCount = 0,

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
            MissileReloadCooldown = 5.0,
            MissileVelocity = 50.0,
            MissileRange = 700.0,
            MissileDirectHitDamageBonus = 3.0,
            MissileNearHitDamageBonus = 2.1,
            MissileFarHitDamageBonus = 1.0
        };

        protected GameLogic NewLogic(params BotInfo[] robots) {
            var game = NewGame();
            game.Robots.AddRange(robots);
            for(var i = 0; i < game.Robots.Count; ++i) {
                game.Robots[i].Index = i;
            }
            _provider = this;
            return new GameLogic(game, _provider);
        }

        //--- IGameDependencyProvider Members ---
        double IGameDependencyProvider.NextRandomDouble() => _random.NextDouble();

        async Task<GetBuildResponse> IGameDependencyProvider.GetRobotBuild(BotInfo robot) => new GetBuildResponse {
            Name = robot.Id
        };

        async Task<GetActionResponse> IGameDependencyProvider.GetRobotAction(BotInfo robot) {

            // destructively fetch next action from dictionary or null if none exist
            if(_robotActions.TryGetValue(robot.Id, out var actions) && (actions?.Any() ?? false)) {
                var action = actions.First();
                actions.RemoveAt(0);
                return action();
            }
            return new GetActionResponse();
        }
    }
}
