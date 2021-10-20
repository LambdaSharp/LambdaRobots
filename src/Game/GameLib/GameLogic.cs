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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LambdaRobots.Bot.Model;

namespace LambdaRobots.Game {

    public interface IGameDependencyProvider {

        //--- Properties ---
        DateTimeOffset UtcNow { get; }

        //--- Methods ---
        float NextRandomFloat();
        Task<GetBuildResponse> GetBotBuild(BotInfo bot);
        Task<GetActionResponse> GetBotAction(BotInfo bot);
    }

    public sealed class GameLogic {

        //--- Fields ---
        private IGameDependencyProvider _provider;
        private Task<GetActionResponse>[] _pendingGetActions;

        //--- Constructors ---
        public GameLogic(GameBoard gameBoard, IGameDependencyProvider provider) {
            GameBoard = gameBoard ?? throw new ArgumentNullException(nameof(gameBoard));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        //--- Properties ---
        public GameBoard GameBoard { get; private set; }

        //--- Methods ---
        public async Task StartAsync(int botCount) {

            // reset game state
            GameBoard.LastStatusUpdate = _provider.UtcNow;
            GameBoard.CurrentGameTurn = 0;
            GameBoard.Missiles.Clear();
            GameBoard.Messages.Clear();
            GameBoard.Bots.Clear();
            for(var i = 0; i < botCount; ++i) {
                GameBoard.Bots.Add(BotBuild.GetDefaultBotBuild(GameBoard.Id, i));
            }

            // get configuration for all bots
            var messages = (await Task.WhenAll(GameBoard.Bots.Select(bot => InitializeBotAsync(bot)))).ToList();
            foreach(var message in messages) {
                AddMessage(message);
            }

            // place bots on playfield
            var marginWidth = GameBoard.BoardWidth * 0.1f;
            var marginHeight = GameBoard.BoardHeight * 0.1f;
            var attempts = 0;
        again:
            if(attempts >= 100) {
                throw new ApplicationException($"unable to place all bots with minimum separation of {GameBoard.MinBotStartDistance:N2}");
            }

            // assign random locations to all bots
            foreach(var bot in GameBoard.Bots.Where(bot => bot.Status == BotStatus.Alive)) {
                bot.X = marginWidth + _provider.NextRandomFloat() * (GameBoard.BoardWidth - 2.0f * marginWidth);
                bot.Y = marginHeight + _provider.NextRandomFloat() * (GameBoard.BoardHeight - 2.0f * marginHeight);
            }

            // verify that none of the bots are too close to each other
            for(var i = 0; i < GameBoard.Bots.Count; ++i) {
                for(var j = i + 1; j < GameBoard.Bots.Count; ++j) {
                    if((GameBoard.Bots[i].Status == BotStatus.Alive) && (GameBoard.Bots[j].Status == BotStatus.Alive)) {
                        var distance = GameMath.Distance(GameBoard.Bots[i].X, GameBoard.Bots[i].Y, GameBoard.Bots[j].X, GameBoard.Bots[j].Y);
                        if(distance < GameBoard.MinBotStartDistance) {
                            ++attempts;
                            goto again;
                        }
                    }
                }
            }
        }

        public void NextTurn(float timelapseSeconds) {

            // allocate array for bot action responses
            if(_pendingGetActions is null) {
                _pendingGetActions = new Task<GetActionResponse>[GameBoard.Bots.Count];
            }

            // increment turn counter
            ++GameBoard.CurrentGameTurn;

            // get the next action of all bots that are still alive
            for(var i = 0; i < _pendingGetActions.Length; ++i) {
                var task = _pendingGetActions[i];
                var bot = GameBoard.Bots[i];
                if(bot.Status == BotStatus.Alive) {

                    // if no tasks are pending for a bot that is alive, fetch the next bot action
                    if(task is null) {
                        _pendingGetActions[i] = _provider.GetBotAction(bot);
                    }
                } else if(!(task is null)) {

                    // bot is destroyed, clear out any pending tasks
                    _pendingGetActions[i] = null;
                }
            }

            // check which bots have a completed response and apply it
            for(var i = 0; i < _pendingGetActions.Length; ++i) {
                var task = _pendingGetActions[i];
                if(task?.IsCompleted ?? false) {
                    ApplyBotAction(GameBoard.Bots[i], task.Result);
                    _pendingGetActions[i] = null;
                }

            }

            // move bots
            foreach(var bot in GameBoard.Bots.Where(bot => bot.Status == BotStatus.Alive)) {
                MoveBot(bot, timelapseSeconds);
            }

            // update missile states
            foreach(var missile in GameBoard.Missiles) {
                switch(missile.Status) {
                case MissileStatus.Flying:

                    // move flying missiles
                    MoveMissile(missile, timelapseSeconds);
                    break;
                case MissileStatus.ExplodingDirect:

                    // missile is exploding; assess direct damage
                    AssessMissileDamage(missile);
                    missile.Status = MissileStatus.ExplodingNear;
                    break;
                case MissileStatus.ExplodingNear:

                    // missile is exploding; assess near damage
                    AssessMissileDamage(missile);
                    missile.Status = MissileStatus.ExplodingFar;
                    break;
                case MissileStatus.ExplodingFar:

                    // missile is exploding; assess far damage
                    AssessMissileDamage(missile);
                    missile.Status = MissileStatus.Destroyed;
                    break;
                default:
                    missile.Status = MissileStatus.Destroyed;
                    break;
                }
            }

            // remove destroyed missiles
            GameBoard.Missiles.RemoveAll(missile => missile.Status == MissileStatus.Destroyed);

            // update game state
            var botCount = GameBoard.Bots.Count(bot => bot.Status == BotStatus.Alive);
            if(botCount == 0) {

                // no bots left
                AddMessage("All bots have perished. Game Over.");
                GameBoard.Status = GameStatus.Finished;
                GameBoard.Missiles.Clear();
            } else if(botCount == 1) {

                // last bot standing
                AddMessage($"{GameBoard.Bots.First(bot => bot.Status == BotStatus.Alive).Name} is victorious! Game Over.");
                GameBoard.Status = GameStatus.Finished;
                GameBoard.Missiles.Clear();
            } else if(GameBoard.CurrentGameTurn >= GameBoard.MaxTurns) {

                // game has reached its turn limit
                AddMessage($"Reached max turns. {botCount:N0} bots are left. Game Over.");
                GameBoard.Status = GameStatus.Finished;
                GameBoard.Missiles.Clear();
            }
        }

        public BotInfo ScanBots(BotInfo bot, float heading, float resolution) {
            BotInfo result = null;
            resolution = GameMath.MinMax(0.01f, resolution, bot.RadarMaxResolution);
            FindBotsByDistance(bot.X, bot.Y, (other, distance) => {

                // skip ourselves
                if(other.Id == bot.Id) {
                    return true;
                }

                // compute relative position
                var deltaX = other.X - bot.X;
                var deltaY = other.Y - bot.Y;

                // check if other bot is beyond scan range
                if(distance > bot.RadarRange) {

                    // no need to enumerate more
                    return false;
                }

                // check if delta angle is within resolution limit
                var angle = MathF.Atan2(deltaX, deltaY) * 180.0f / MathF.PI;
                if(MathF.Abs(GameMath.NormalizeAngle(heading - angle)) <= resolution) {

                    // found a bot within range and resolution; stop enumerating
                    result = other;
                    return false;
                }

                // enumerate more
                return true;
            });
            return result;
        }

        private async Task<string> InitializeBotAsync(BotInfo bot) {
            var config = await _provider.GetBotBuild(bot);
            bot.Name = config?.Name ?? $"#{bot.Index}";
            if(config is null) {

                // missing config information, consider bot dead
                bot.Status = BotStatus.Dead;
                bot.TimeOfDeathGameTurn = 0;
                return $"{bot.Name} (R{bot.Index}) was disqualified due to failure to initialize";
            }

            // read bot configuration
            var success = true;
            var buildPoints = 0;
            var buildDescription = new StringBuilder();

            // read radar configuration
            buildPoints += (int)config.Radar;
            buildDescription.Append($"{config.Radar} Radar");
            success &= BotBuild.TrySetRadar(config.Radar, bot);

            // read engine configuration
            buildPoints += (int)config.Engine;
            buildDescription.Append($", {config.Engine} Engine");
            success &= BotBuild.TrySetEngine(config.Engine, bot);

            // read armor configuration
            buildPoints += (int)config.Armor;
            buildDescription.Append($", {config.Armor} Armor");
            success &= BotBuild.TrySetArmor(config.Armor, bot);

            // read missile configuration
            buildPoints += (int)config.Missile;
            buildDescription.Append($", {config.Missile} Missile");
            success &= BotBuild.TrySetMissile(config.Missile, bot);

            // check if bot respected the max build points
            if(buildPoints > GameBoard.MaxBuildPoints) {
                success = false;
            }

            // check if bot is disqualified due to a bad build
            if(!success) {
                bot.Status = BotStatus.Dead;
                bot.TimeOfDeathGameTurn = GameBoard.CurrentGameTurn;
                return $"{bot.Name} (R{bot.Index}) was disqualified due to bad configuration ({buildDescription}: {buildPoints} points)";
            }
            bot.InternalState = config.InternalStartState;
            bot.LastStatusUpdate = _provider.UtcNow;
            return $"{bot.Name} (R{bot.Index}) has joined the battle ({buildDescription}: {buildPoints} points)";
        }

        private void ApplyBotAction(BotInfo bot, GetActionResponse action) {
            var now = _provider.UtcNow;
            var secondsSinceUpdate = (float)(now - bot.LastStatusUpdate).TotalSeconds;
            bot.LastStatusUpdate = now;

            // reduce reload time if any is active
            if(bot.ReloadCoolDown > 0) {
                bot.ReloadCoolDown = MathF.Max(0.0f, bot.ReloadCoolDown - secondsSinceUpdate);
            }

            // check if any actions need to be applied
            if(action is null) {

                // bot didn't respond with an action; consider it dead
                bot.Status = BotStatus.Dead;
                bot.TimeOfDeathGameTurn = GameBoard.CurrentGameTurn;
                AddMessage($"{bot.Name} (R{bot.Index}) was disqualified by lack of action");
                return;
            }

            // update bot state
            bot.InternalState = action.BotState;

            // update speed and heading
            bot.TargetSpeed = GameMath.MinMax(0.0f, action.Speed ?? bot.TargetSpeed, bot.MaxSpeed);
            bot.TargetHeading = GameMath.NormalizeAngle(action.Heading ?? bot.TargetHeading);

            // fire missile if requested and possible
            if((action.FireMissileHeading.HasValue || action.FireMissileDistance.HasValue) && (bot.ReloadCoolDown == 0.0f)) {

                // update bot state
                ++bot.TotalMissileFiredCount;
                bot.ReloadCoolDown = bot.MissileReloadCooldown;

                // add missile
                var missile = new MissileInfo {
                    Id = $"{bot.Id}:M{bot.TotalMissileFiredCount}",
                    BotId = bot.Id,
                    Status = MissileStatus.Flying,
                    X = bot.X,
                    Y = bot.Y,
                    Speed = bot.MissileVelocity,
                    Heading = GameMath.NormalizeAngle(action.FireMissileHeading ?? bot.Heading),
                    Range = GameMath.MinMax(0.0f, action.FireMissileDistance ?? bot.MissileRange, bot.MissileRange),
                    DirectHitDamageBonus = bot.MissileDirectHitDamageBonus,
                    NearHitDamageBonus = bot.MissileNearHitDamageBonus,
                    FarHitDamageBonus = bot.MissileFarHitDamageBonus
                };
                GameBoard.Missiles.Add(missile);
            }
        }

        private void MoveMissile(MissileInfo missile, float timelapseSeconds) {
            bool collision;
            Move(
                missile.X,
                missile.Y,
                missile.Distance,
                missile.Speed,
                missile.Heading,
                missile.Range,
                timelapseSeconds,
                out var missileX,
                out var missileY,
                out var missileDistance,
                out collision
            );
            missile.X = missileX;
            missile.Y = missileY;
            missile.Distance = missileDistance;
            if(collision) {
                missile.Status = MissileStatus.ExplodingDirect;
                missile.Speed = 0.0f;
            }
        }

        private void AssessMissileDamage(MissileInfo missile) {
            FindBotsByDistance(missile.X, missile.Y, (bot, distance) => {

                // compute damage dealt by missile
                float damage = 0.0f;
                string damageType = null;
                switch(missile.Status) {
                case MissileStatus.ExplodingDirect:
                    if(distance <= GameBoard.DirectHitRange) {
                        damage = bot.DirectHitDamage + missile.DirectHitDamageBonus;
                        damageType = "direct";
                    }
                    break;
                case MissileStatus.ExplodingNear:
                    if(distance <= GameBoard.NearHitRange) {
                        damage = bot.NearHitDamage + missile.NearHitDamageBonus;
                        damageType = "near";
                    }
                    break;
                case MissileStatus.ExplodingFar:
                    if(distance <= GameBoard.FarHitRange) {
                        damage = bot.FarHitDamage + missile.FarHitDamageBonus;
                        damageType = "far";
                    }
                    break;
                }

                // check if any damage was dealt
                if(damage == 0.0f) {

                    // stop enumerating more bots since they will be further away
                    return false;
                }

                // record damage dealt
                var from = GameBoard.Bots.FirstOrDefault(fromBot => fromBot.Id == missile.BotId);
                if(from != null) {
                    from.TotalDamageDealt += damage;
                    ++from.TotalMissileHitCount;

                    // check if bot was killed
                    if(Damage(bot, damage)) {
                        ++from.TotalKills;

                        // check if bot inflicted damage to itself
                        if(bot.Id == from.Id) {
                            AddMessage($"{bot.Name} (R{bot.Index}) killed itself");
                        } else {
                            AddMessage($"{bot.Name} (R{bot.Index}) was killed by {from.Name} (R{from.Index})");
                        }
                    } else {

                        // check if bot inflicted damage to itself
                        if(bot.Id == from.Id) {
                            AddMessage($"{bot.Name} (R{bot.Index}) caused {damage:N0} {damageType} damage to itself");
                        } else {
                            AddMessage($"{bot.Name} (R{bot.Index}) received {damage:N0} {damageType} damage from {from.Name} (R{from.Index})");
                        }
                    }
                }
                return true;
            });
        }

        private void MoveBot(BotInfo bot, float timelapseSeconds) {

            // compute new heading
            if(bot.Heading != bot.TargetHeading) {
                if(bot.Speed <= bot.MaxTurnSpeed) {
                    bot.Heading = bot.TargetHeading;
                } else {
                    bot.TargetSpeed = 0;
                    bot.Heading = bot.TargetHeading;
                    AddMessage($"{bot.Name} (R{bot.Index}) stopped by sudden turn");
                }
            }

            // compute new speed
            var oldSpeed = bot.Speed;
            if(bot.TargetSpeed > bot.Speed) {
                bot.Speed = MathF.Min(bot.TargetSpeed, bot.Speed + bot.Acceleration * timelapseSeconds);
            } else if(bot.TargetSpeed < bot.Speed) {
                bot.Speed = MathF.Max(bot.TargetSpeed, bot.Speed - bot.Deceleration * timelapseSeconds);
            }
            var effectiveSpeed = (bot.Speed + oldSpeed) / 2.0f;

            // move bot
            bool collision;
            Move(
                bot.X,
                bot.Y,
                bot.TotalTravelDistance,
                effectiveSpeed,
                bot.Heading,
                float.MaxValue,
                timelapseSeconds,
                out var botX,
                out var botY,
                out var botTotalTravelDistance,
                out collision
            );
            bot.X = botX;
            bot.Y = botY;
            bot.TotalTravelDistance = botTotalTravelDistance;

            // check for collision with wall
            if(collision) {
                bot.Speed = 0.0f;
                bot.TargetSpeed = 0.0f;
                ++bot.TotalCollisions;
                if(Damage(bot, bot.CollisionDamage)) {
                    AddMessage($"{bot.Name} (R{bot.Index}) was destroyed by wall collision");
                    return;
                } else {
                    AddMessage($"{bot.Name} (R{bot.Index}) received {bot.CollisionDamage:N0} damage by wall collision");
                }
            }

            // check if bot collides with any other bot
            FindBotsByDistance(bot.X, bot.Y, (other, distance) => {
                if((other.Id != bot.Id) && (distance < GameBoard.CollisionRange)) {
                    bot.Speed = 0.0f;
                    bot.TargetSpeed = 0.0f;
                    ++bot.TotalCollisions;
                    if(Damage(bot, bot.CollisionDamage)) {
                        AddMessage($"{bot.Name} (R{bot.Index}) was destroyed by collision with {other.Name}");
                    } else {
                        AddMessage($"{bot.Name} (R{bot.Index}) was damaged {bot.CollisionDamage:N0} by collision with {other.Name} (R{other.Index})");
                    }
                }

                // keep looking for more collisions unless bot is dead or nearest bot is out of range
                return (bot.Status == BotStatus.Alive) && (distance < GameBoard.CollisionRange);
            });
        }

        private void AddMessage(string text) {
            GameBoard.Messages.Add(new Message {
                GameTurn = GameBoard.CurrentGameTurn,
                Text = text
            });
        }

        private void Move(
            float startX,
            float startY,
            float startDistance,
            float speed,
            float heading,
            float range,
            float timelapseSeconds,
            out float endX,
            out float endY,
            out float endDistance,
            out bool collision
        ) {
            collision = false;

            // ensure object cannot move beyond its max range
            endDistance = startDistance + speed * timelapseSeconds;
            if(endDistance > range) {
                collision = true;
                endDistance = range;
            }

            // compute new position for object
            var delta = endDistance - startDistance;
            var sinHeading = MathF.Sin(heading * MathF.PI / 180.0f);
            var cosHeading = MathF.Cos(heading * MathF.PI / 180.0f);
            endX = startX + delta * sinHeading;
            endY = startY + delta * cosHeading;

            // ensure missile cannot fly past playfield boundaries
            if(endX < 0) {
                collision = true;
                delta = (0 - startX) / sinHeading;
                endX = startX + delta * sinHeading;
                endY = startY + delta * cosHeading;
                endDistance = startDistance + delta;
            } else if (endX > GameBoard.BoardWidth) {
                collision = true;
                delta = (GameBoard.BoardWidth - startX) / sinHeading;
                endX = startX + delta * sinHeading;
                endY = startY + delta * cosHeading;
                endDistance = startDistance + delta;
            }
            if(endY < 0) {
                collision = true;
                delta = (0 - startY) / cosHeading;
                endX = startX + delta * sinHeading;
                endY = startY + delta * cosHeading;
                endDistance = startDistance + delta;
            } else if(endY > GameBoard.BoardHeight) {
                collision = true;
                delta = (GameBoard.BoardHeight - startY) / cosHeading;
                endX = startX + delta * sinHeading;
                endY = startY + delta * cosHeading;
                endDistance = startDistance + delta;
            }
        }

        private void FindBotsByDistance(float x, float y, Func<BotInfo, float, bool> callback) {
            foreach(var botWithDistance in GameBoard.Bots
                .Where(bot => bot.Status == BotStatus.Alive)
                .Select(bot => new {
                    Bot = bot,
                    Distance = GameMath.Distance(bot.X, bot.Y, x, y)
                })
                .OrderBy(tuple => tuple.Distance)
                .ToList()
            ) {
                if(!callback(botWithDistance.Bot, botWithDistance.Distance)) {
                    break;
                }
            }
        }

        private bool Damage(BotInfo bot, float damage) {
            bot.Damage += damage;
            if(bot.Damage >= bot.MaxDamage) {
                bot.Damage = bot.MaxDamage;
                bot.Status = BotStatus.Dead;
                bot.TimeOfDeathGameTurn = GameBoard.CurrentGameTurn;
                return true;
            }
            return false;
        }
    }
}
