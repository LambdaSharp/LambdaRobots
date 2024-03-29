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
using LambdaRobots.Game;

namespace Test.LambdaRobots.Game {

    public abstract class _Init : IGameDependencyProvider {

        //--- Fields ---
        protected IGameDependencyProvider _provider;
        protected Dictionary<string, List<Func<GetActionResponse>>> _botActions = new Dictionary<string, List<Func<GetActionResponse>>>();
        protected readonly Random _random = new Random();

        //--- Methods ---
        protected GameSession NewGameSession() => new GameSession {
            Id = "Test",
            BoardWidth = 1000.0f,
            BoardHeight = 1000.0f,
            DirectHitRange = 5.0f,
            NearHitRange = 20.0f,
            FarHitRange = 40.0f,
            CollisionRange = 5.0f,
            MinBotStartDistance = 100.0f,
            BotTimeoutSeconds = 10.0f,
            CurrentGameTurn = 0,
            MaxTurns = 1000,
            MaxBuildPoints = 8,
            LastStatusUpdate = DateTimeOffset.UtcNow
        };

        protected BotInfo NewBot(string id, float x, float y) => new BotInfo {

            // bot state
            Id = id,
            Name = id,
            Status = BotStatus.Alive,
            X = x,
            Y = y,
            Speed = 0.0f,
            Heading = 0.0f,
            TotalTravelDistance = 0.0f,
            Damage = 0.0f,
            ReloadCoolDown = 0.0f,
            TotalMissileFiredCount = 0,

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
            MissileReloadCooldown = 5.0f,
            MissileVelocity = 50.0f,
            MissileRange = 700.0f,
            MissileDirectHitDamageBonus = 3.0f,
            MissileNearHitDamageBonus = 2.1f,
            MissileFarHitDamageBonus = 1.0f
        };

        protected GameLogic NewLogic(params BotInfo[] bots) {
            var game = NewGameSession();
            game.Bots.AddRange(bots);
            for(var i = 0; i < game.Bots.Count; ++i) {
                game.Bots[i].Index = i;
            }
            _provider = this;
            return new GameLogic(game, _provider);
        }

        //--- IGameDependencyProvider Members ---
        DateTimeOffset IGameDependencyProvider.UtcNow => DateTimeOffset.UtcNow;
        float IGameDependencyProvider.NextRandomFloat() => (float)_random.NextDouble();

        async Task<GetBuildResponse> IGameDependencyProvider.GetBotBuild(BotInfo bot) => new GetBuildResponse {
            Name = bot.Id
        };

        async Task<GetActionResponse> IGameDependencyProvider.GetBotAction(BotInfo bot) {

            // destructively fetch next action from dictionary or null if none exist
            if(_botActions.TryGetValue(bot.Id, out var actions) && (actions?.Any() ?? false)) {
                var action = actions.First();
                actions.RemoveAt(0);
                return action();
            }
            return new GetActionResponse();
        }
    }
}
