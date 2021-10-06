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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Amazon.DynamoDBv2;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using LambdaRobots.Protocol;
using LambdaSharp;

namespace LambdaRobots.Server.GameTurnFunction {

    public class FunctionRequest {

        //--- Properties ---
        public string GameId { get; set; }
        public GameStatus Status { get; set; }
    }

    public class FunctionResponse {

        //--- Properties ---
        public string GameId { get; set; }
        public GameStatus Status { get; set; }
    }

    public class Function : ALambdaFunction<FunctionRequest, FunctionResponse> {

        //--- Class Fields ---
        private static Random _random = new Random();

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Fields ---
        private IAmazonLambda _lambdaClient;
        private IAmazonApiGatewayManagementApi _amaClient;
        private DynamoTable _table;
        private string _gameApiUrl;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // initialize Lambda function
            _lambdaClient = new AmazonLambdaClient();
            _amaClient = new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig {
                ServiceURL = config.ReadText("Module::WebSocket::Url")
            });
            _table = new DynamoTable(
                config.ReadDynamoDBTableName("GameTable"),
                new AmazonDynamoDBClient(),
                LambdaSerializer
            );
            _gameApiUrl = config.ReadText("RestApiUrl");
        }

        public override async Task<FunctionResponse> ProcessMessageAsync(FunctionRequest request) {

            // run game
            try {
                await GameLoopAsync(request.GameId);
            } catch(Exception) {
                return new FunctionResponse {
                    GameId = request.GameId,
                    Status = GameStatus.Error
                };
            }
            return new FunctionResponse {
                GameId = request.GameId,
                Status = GameStatus.Finished
            };
        }

        public async Task GameLoopAsync(string gameId) {

            // get game state from DynamoDB table
            LogInfo($"Loading game state: ID = {gameId}");
            var gameRecord = await _table.GetAsync<GameRecord>(gameId);
            if(gameRecord?.Game?.Status != GameStatus.Start) {

                // TODO: log diagnostics
                LogWarn($"Game state is not valid");
                return;
            }
            var game = gameRecord.Game;
            try {

                // initialize game logic
                var logic = new GameLogic(new GameDependencyProvider(
                    gameRecord.Game,
                    _random,
                    robot => GetRobotBuildAsync(gameRecord.Game, robot, gameRecord.LambdaRobotArns[gameRecord.Game.Robots.IndexOf(robot)]),
                    robot => GetRobotActionAsync(gameRecord.Game, robot, gameRecord.LambdaRobotArns[gameRecord.Game.Robots.IndexOf(robot)])
                ));

                // initialize robots
                LogInfo($"Start game: initializing {game.Robots.Count(robot => robot.Status == LambdaRobotStatus.Alive)} robots (total: {game.Robots.Count})");
                await logic.StartAsync(gameRecord.LambdaRobotArns.Count);
                game.Status = GameStatus.NextTurn;
                LogInfo($"Robots initialized: {game.Robots.Count(robot => robot.Status == LambdaRobotStatus.Alive)} robots ready");

                // loop until we're done
                var messageCount = game.Messages.Count;
                while(game.Status == GameStatus.NextTurn) {

                    // next turn
                    LogInfo($"Start turn {game.TotalTurns} (max: {game.MaxTurns}): invoking {game.Robots.Count(robot => robot.Status == LambdaRobotStatus.Alive)} robots (total: {game.Robots.Count})");
                    await logic.NextTurnAsync();
                    LogInfo($"End turn: {game.Robots.Count(robot => robot.Status == LambdaRobotStatus.Alive)} robots alive");

                    // log new game messages
                    for(var i = messageCount; i < game.Messages.Count; ++i) {
                        LogInfo($"Game message {i + 1}: {game.Messages[i].Text}");
                    }

                    // attempt to update the game record
                    LogInfo($"Storing game: ID = {gameId}");
                    await _table.UpdateAsync(new GameRecord {
                        PK = gameId,
                        Game = game
                    }, new[] { nameof(GameRecord.Game) });

                    // notify WebSocket of new game state
                    LogInfo($"Posting game update to connection: {gameId}");
                    try {
                        await _amaClient.PostToConnectionAsync(new PostToConnectionRequest {
                            ConnectionId = gameRecord.ConnectionId,
                            Data = new MemoryStream(Encoding.UTF8.GetBytes(LambdaSerializer.Serialize(new GameTurnNotification {
                                Game = game
                            })))
                        });
                    } catch(AmazonServiceException e) when(e.StatusCode == System.Net.HttpStatusCode.Gone) {

                        // connection has been closed, stop the game
                        LogInfo($"Connection is gone");
                        game.Status = GameStatus.Finished;
                        return;
                    } catch(Exception e) {
                        LogErrorAsWarning(e, "PostToConnectionAsync() failed");
                    }
                }
            } catch(Exception e) {
                LogError(e, "error during game loop");
                game.Status = GameStatus.Error;
                await _amaClient.PostToConnectionAsync(new PostToConnectionRequest {
                    ConnectionId = gameRecord.ConnectionId,
                    Data = new MemoryStream(Encoding.UTF8.GetBytes(LambdaSerializer.Serialize(new GameTurnNotification {
                        Game = game
                    })))
                });
                throw;
            } finally {

                // delete game from table
                LogInfo($"Deleting game: ID = {gameId}");
                await _table.DeleteAsync<GameRecord>(gameId);
            }
        }

        private async Task<LambdaRobotBuild> GetRobotBuildAsync(Game game, LambdaRobot robot, string lambdaArn) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try {
                var getBuildTask = _lambdaClient.InvokeAsync(new InvokeRequest {
                    Payload = LambdaSerializer.Serialize(new LambdaRobotRequest {
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
                if(await Task.WhenAny(getBuildTask, Task.Delay(TimeSpan.FromSeconds(game.RobotTimeoutSeconds))) != getBuildTask) {
                    LogInfo($"Robot {robot.Id} GetBuild timed out after {stopwatch.Elapsed.TotalSeconds:N2}s");
                    return null;
                }
                var response = Encoding.UTF8.GetString(getBuildTask.Result.Payload.ToArray());
                var result = LambdaSerializer.Deserialize<LambdaRobotResponse>(response);
                LogInfo($"Robot {robot.Id} GetBuild responded in {stopwatch.Elapsed.TotalSeconds:N2}s:\n{response}");
                return result.RobotBuild;
            } catch(Exception e) {
                LogErrorAsWarning(e, $"Robot {robot.Id} GetBuild failed (arn: {lambdaArn})");
                return null;
            }
        }

        private async Task<LambdaRobotAction> GetRobotActionAsync(Game game, LambdaRobot robot, string lambdaArn) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try {
                var getActionTask = _lambdaClient.InvokeAsync(new InvokeRequest {
                    Payload = LambdaSerializer.Serialize(new LambdaRobotRequest {
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
                            ApiUrl = _gameApiUrl + $"/{game.Id}"
                        },
                        Robot = robot
                    }),
                    FunctionName = lambdaArn,
                    InvocationType = InvocationType.RequestResponse
                });

                // check if lambda responds within time limit
                if(await Task.WhenAny(getActionTask, Task.Delay(TimeSpan.FromSeconds(game.RobotTimeoutSeconds))) != getActionTask) {
                    LogInfo($"Robot {robot.Id} GetAction timed out after {stopwatch.Elapsed.TotalSeconds:N2}s");
                    return null;
                }
                var response = Encoding.UTF8.GetString(getActionTask.Result.Payload.ToArray());
                var result = LambdaSerializer.Deserialize<LambdaRobotResponse>(response);
                LogInfo($"Robot {robot.Id} GetAction responded in {stopwatch.Elapsed.TotalSeconds:N2}s:\n{response}");
                return result.RobotAction;
            } catch(Exception e) {
                LogErrorAsWarning(e, $"Robot {robot.Id} GetAction failed (arn: {lambdaArn})");
                return null;
            }
        }
    }
}
