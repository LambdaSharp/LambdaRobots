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
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Challenge.LambdaRobots.Protocol;
using Newtonsoft.Json;
using LambdaSharp.Logger;
using System.Collections.Generic;
using Amazon.ApiGatewayManagementApi.Model;
using System.IO;
using Amazon.Runtime;
using System.Linq;
using Amazon.ApiGatewayManagementApi;

namespace Challenge.LambdaRobots.Server {

    public class GameTurnLogic {

        //--- Fields ---
        private readonly ILambdaLogLevelLogger _logger;
        private readonly IAmazonLambda _lambdaClient;
        private readonly IAmazonApiGatewayManagementApi _amaClient;
        private readonly string _gameApiUrl;
        private readonly Random _random;

        //--- Constructors ---
        public GameTurnLogic(
            ILambdaLogLevelLogger logger,
            IAmazonLambda lambdaClient,
            IAmazonApiGatewayManagementApi amaClient,
            string gameApiUrl,
            Random random
        ) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _lambdaClient = lambdaClient ?? throw new ArgumentNullException(nameof(lambdaClient));
            _amaClient = amaClient ?? throw new ArgumentNullException(nameof(amaClient));
            _gameApiUrl = gameApiUrl ?? throw new ArgumentNullException(nameof(gameApiUrl));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        //--- Methods ---
        public async Task<GameRecord> ComputeNextTurnAsync(GameRecord gameRecord) {

            // get game state from DynamoDB table
            _logger.LogInfo($"Game turn {gameRecord.Game.TotalTurns}");
            var game = gameRecord.Game;
            var logic = new GameLogic(new GameDependencyProvider(
                gameRecord.Game,
                _random,
                robot => GetRobotBuildAsync(game, robot, gameRecord.LambdaRobotArns[gameRecord.Game.Robots.IndexOf(robot)]),
                robot => GetRobotActionAsync(game, robot, gameRecord.LambdaRobotArns[gameRecord.Game.Robots.IndexOf(robot)])
            ));

            // determine game action to take
            var messageCount = game.Messages.Count;
            try {

                // check game state
                switch(gameRecord.Game.State) {
                case GameState.Start:

                    // start game
                    _logger.LogInfo($"Start game: initializing {game.Robots.Count(robot => robot.State == RobotState.Alive)} robots (total: {game.Robots.Count})");
                    await logic.StartAsync(gameRecord.LambdaRobotArns.Count);
                    game.State = GameState.NextTurn;
                    _logger.LogInfo($"Done: {game.Robots.Count(robot => robot.State == RobotState.Alive)} robots ready");
                    break;
                case GameState.NextTurn:

                    // next turn
                    _logger.LogInfo($"Start turn {game.TotalTurns} (max: {game.MaxTurns}): invoking {game.Robots.Count(robot => robot.State == RobotState.Alive)} robots (total: {game.Robots.Count})");
                    await logic.NextTurnAsync();
                    _logger.LogInfo($"End turn: {game.Robots.Count(robot => robot.State == RobotState.Alive)} robots alive");
                    break;
                case GameState.Finished:

                    // nothing further to do
                    break;
                default:
                    game.State = GameState.Error;
                    throw new ApplicationException($"unexpected game state: '{gameRecord.Game.State}'");
                }
            } catch(Exception e) {
                _logger.LogError(e, "error during game loop");
                game.State = GameState.Error;
            }

            // log new game messages
            for(var i = messageCount; i < game.Messages.Count; ++i) {
                _logger.LogInfo($"Game message {i + 1}: {game.Messages[i].Text}");
            }

            // notify WebSocket of new game state
            _logger.LogInfo($"Posting game update to connection: {game.Id}");
            try {
                await _amaClient.PostToConnectionAsync(new PostToConnectionRequest {
                    ConnectionId = gameRecord.ConnectionId,
                    Data = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new GameTurnNotification {
                        Game = game
                    })))
                });
            } catch(AmazonServiceException e) when(e.StatusCode == System.Net.HttpStatusCode.Gone) {

                // connection has been closed, stop the game
                _logger.LogInfo($"Connection is gone");
                game.State = GameState.Finished;
            } catch(Exception e) {
                _logger.LogErrorAsWarning(e, "PostToConnectionAsync() failed");
            }
            return gameRecord;
        }

        private async Task<LambdaRobotBuild> GetRobotBuildAsync(Game game, Robot robot, string lambdaArn) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try {
                var getNameTask = _lambdaClient.InvokeAsync(new InvokeRequest {
                    Payload = JsonConvert.SerializeObject(new LambdaRobotRequest {
                        Command = LambdaRobotCommand.GetBuild,
                        Game = new GameInfo {
                            Id = game.Id,
                            BoardWidth = game.BoardWidth,
                            BoardHeight = game.BoardHeight,
                            DirectHitRange = game.DirectHitRange,
                            NearHitRange = game.NearHitRange,
                            FarHitRange = game.FarHitRange,
                            CollisionRange = game.CollisionRange,
                            GameTurn = game.TotalTurns,
                            MaxGameTurns = game.MaxTurns,
                            MaxBuildPoints = game.MaxBuildPoints,
                            SecondsPerTurn = game.SecondsPerTurn
                        },
                        Robot = robot
                    }),
                    FunctionName = lambdaArn,
                    InvocationType = InvocationType.RequestResponse
                });

                // check if lambda responds within time limit
                if(await Task.WhenAny(getNameTask, Task.Delay(TimeSpan.FromSeconds(game.RobotTimeoutSeconds))) != getNameTask) {
                    _logger.LogInfo($"Robot {robot.Id} GetName timed out after {stopwatch.Elapsed.TotalSeconds:N2}s");
                    return null;
                }
                var response = Encoding.UTF8.GetString(getNameTask.Result.Payload.ToArray());
                var result = JsonConvert.DeserializeObject<LambdaRobotResponse>(response);
                _logger.LogInfo($"Robot {robot.Id} GetName responded in {stopwatch.Elapsed.TotalSeconds:N2}s:\n{response}");
                return result.RobotBuild;
            } catch(Exception e) {
                _logger.LogErrorAsWarning(e, $"Robot {robot.Id} GetName failed (arn: {lambdaArn})");
                return null;
            }
        }

        private async Task<LambdaRobotAction> GetRobotActionAsync(Game game, Robot robot, string lambdaArn) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try {
                var getActionTask = _lambdaClient.InvokeAsync(new InvokeRequest {
                    Payload = JsonConvert.SerializeObject(new LambdaRobotRequest {
                        Command = LambdaRobotCommand.GetAction,
                        Game = new GameInfo {
                            Id = game.Id,
                            BoardWidth = game.BoardWidth,
                            BoardHeight = game.BoardHeight,
                            DirectHitRange = game.DirectHitRange,
                            NearHitRange = game.NearHitRange,
                            FarHitRange = game.FarHitRange,
                            CollisionRange = game.CollisionRange,
                            GameTurn = game.TotalTurns,
                            MaxGameTurns = game.MaxTurns,
                            MaxBuildPoints = game.MaxBuildPoints,
                            SecondsPerTurn = game.SecondsPerTurn,
                            ApiUrl = _gameApiUrl
                        },
                        Robot = robot
                    }),
                    FunctionName = lambdaArn,
                    InvocationType = InvocationType.RequestResponse
                });

                // check if lambda responds within time limit
                if(await Task.WhenAny(getActionTask, Task.Delay(TimeSpan.FromSeconds(game.RobotTimeoutSeconds))) != getActionTask) {
                    _logger.LogInfo($"Robot {robot.Id} GetAction timed out after {stopwatch.Elapsed.TotalSeconds:N2}s");
                    return null;
                }
                var response = Encoding.UTF8.GetString(getActionTask.Result.Payload.ToArray());
                var result = JsonConvert.DeserializeObject<LambdaRobotResponse>(response);
                _logger.LogInfo($"Robot {robot.Id} GetAction responded in {stopwatch.Elapsed.TotalSeconds:N2}s:\n{response}");
                return result.RobotAction;
            } catch(Exception e) {
                _logger.LogErrorAsWarning(e, $"Robot {robot.Id} GetAction failed (arn: {lambdaArn})");
                return null;
            }
        }
    }
}
