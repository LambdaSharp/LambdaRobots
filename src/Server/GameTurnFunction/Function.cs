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

    public class Function : ALambdaFunction<FunctionRequest, FunctionResponse>, IGameDependencyProvider {

        //--- Class Fields ---
        private readonly static Random _random = new Random();

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Fields ---
        private IAmazonLambda _lambdaClient;
        private IAmazonApiGatewayManagementApi _amaClient;
        private DynamoTable _table;
        private string _gameApiUrl;
        private GameRecord _gameRecord;

        //--- Properties ---
        public GameRecord GameRecord {
            get => _gameRecord ?? throw new InvalidOperationException();
            set => _gameRecord = value;
        }

        public Game Game => GameRecord.Game;

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
            GameRecord = await _table.GetAsync<GameRecord>(gameId);
            try {
                if(GameRecord?.Game?.Status != GameStatus.Start) {

                    // TODO: better log diagnostics
                    LogWarn($"Game state is not valid");
                    return;
                }
                try {

                    // initialize game logic
                    var logic = new GameLogic(this.Game, this);

                    // initialize robots
                    LogInfo($"Start game: initializing {Game.Robots.Count(robot => robot.Status == LambdaRobotStatus.Alive)} robots (total: {Game.Robots.Count})");
                    await logic.StartAsync(GameRecord.LambdaRobotArns.Count);
                    Game.Status = GameStatus.NextTurn;
                    LogInfo($"Robots initialized: {Game.Robots.Count(robot => robot.Status == LambdaRobotStatus.Alive)} robots ready");

                    // loop until we're done
                    var messageCount = Game.Messages.Count;
                    while(Game.Status == GameStatus.NextTurn) {

                        // next turn
                        LogInfo($"Start turn {Game.CurrentGameTurn} (max: {Game.MaxTurns}): invoking {Game.Robots.Count(robot => robot.Status == LambdaRobotStatus.Alive)} robots (total: {Game.Robots.Count})");
                        await logic.NextTurnAsync();
                        for(var i = messageCount; i < Game.Messages.Count; ++i) {
                            LogInfo($"Game message {i + 1}: {Game.Messages[i].Text}");
                        }
                        LogInfo($"End turn: {Game.Robots.Count(robot => robot.Status == LambdaRobotStatus.Alive)} robots alive");

                        // attempt to update the game record
                        LogInfo($"Storing game: ID = {gameId}");
                        await _table.UpdateAsync(new GameRecord {
                            PK = gameId,
                            Game = Game
                        }, new[] { nameof(Function.Game) });

                        // notify WebSocket of new game state
                        if(!await TryPostGameUpdateAsync(GameRecord.ConnectionId, Game)) {
                            return;
                        }
                    }
                } catch(Exception e) {
                    LogError(e, "Error during game loop");

                    // notify frontend that the game is over
                    Game.Status = GameStatus.Error;
                    await TryPostGameUpdateAsync(GameRecord.ConnectionId, Game);
                    throw;
                } finally {

                    // delete game from table
                    LogInfo($"Deleting game: ID = {gameId}");
                    await _table.DeleteAsync<GameRecord>(gameId);
                }
            } finally {
                GameRecord = null;
            }
        }

        private async Task<bool> TryPostGameUpdateAsync(string connectionId, Game game) {
            LogInfo($"Posting game update to connection: {game.Id}");
            try {
                var json = LambdaSerializer.Serialize(new GameTurnNotification {
                    Game = game
                });
                await _amaClient.PostToConnectionAsync(new PostToConnectionRequest {
                    ConnectionId = connectionId,
                    Data = new MemoryStream(Encoding.UTF8.GetBytes(json))
                });
            } catch(AmazonServiceException e) when(e.StatusCode == System.Net.HttpStatusCode.Gone) {

                // connection has been closed, stop the game
                LogInfo($"Connection is gone");
                game.Status = GameStatus.Finished;
                return false;
            } catch(Exception e) {
                LogErrorAsWarning(e, "Failed posting game update");
            }
            return true;
        }

        //--- IGameDependencyProvider Members ---
        double IGameDependencyProvider.NextRandomDouble() => _random.NextDouble();

        async Task<LambdaRobotBuild> IGameDependencyProvider.GetRobotBuild(LambdaRobot robot) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try {
                var lambdaArn = GameRecord.LambdaRobotArns[robot.Index];
                try {
                    var getBuildTask = _lambdaClient.InvokeAsync(new InvokeRequest {
                        Payload = LambdaSerializer.Serialize(new LambdaRobotRequest {
                            Command = LambdaRobotCommand.GetBuild,
                            Game = new GameInfo {
                                Id = Game.Id,
                                BoardWidth = Game.BoardWidth,
                                BoardHeight = Game.BoardHeight,
                                DirectHitRange = Game.DirectHitRange,
                                NearHitRange = Game.NearHitRange,
                                FarHitRange = Game.FarHitRange,
                                CollisionRange = Game.CollisionRange,
                                GameTurn = Game.CurrentGameTurn,
                                MaxGameTurns = Game.MaxTurns,
                                MaxBuildPoints = Game.MaxBuildPoints,
                                SecondsPerTurn = Game.SecondsPerTurn
                            },
                            Robot = robot
                        }),
                        FunctionName = lambdaArn,
                        InvocationType = InvocationType.RequestResponse
                    });

                    // check if lambda responds within time limit
                    if(await Task.WhenAny(getBuildTask, Task.Delay(TimeSpan.FromSeconds(Game.RobotTimeoutSeconds))) != getBuildTask) {
                        LogInfo($"Robot {robot.Id} GetBuild timed out after {stopwatch.Elapsed.TotalSeconds:N2}s");
                        return null;
                    }
                    var response = Encoding.UTF8.GetString(getBuildTask.Result.Payload.ToArray());
                    var result = LambdaSerializer.Deserialize<LambdaRobotResponse>(response);
                    LogInfo($"Robot {robot.Id} GetBuild responded in {stopwatch.Elapsed.TotalSeconds:N2}s:\n{response}");
                    return result.RobotBuild;
                } catch(Exception e) {
                    LogErrorAsInfo(e, $"Robot {robot.Id} GetBuild failed (arn: {lambdaArn ?? "<NULL>"})");
                    return null;
                }
            } catch(Exception e) {
                LogErrorAsWarning(e, $"Robot R{robot.Index} is out of range");
                return null;
            }
        }

        async Task<LambdaRobotAction> IGameDependencyProvider.GetRobotAction(LambdaRobot robot) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try {
                var lambdaArn = GameRecord.LambdaRobotArns[robot.Index];
                try {
                    var getActionTask = _lambdaClient.InvokeAsync(new InvokeRequest {
                        Payload = LambdaSerializer.Serialize(new LambdaRobotRequest {
                            Command = LambdaRobotCommand.GetAction,
                            Game = new GameInfo {
                                Id = Game.Id,
                                BoardWidth = Game.BoardWidth,
                                BoardHeight = Game.BoardHeight,
                                DirectHitRange = Game.DirectHitRange,
                                NearHitRange = Game.NearHitRange,
                                FarHitRange = Game.FarHitRange,
                                CollisionRange = Game.CollisionRange,
                                GameTurn = Game.CurrentGameTurn,
                                MaxGameTurns = Game.MaxTurns,
                                MaxBuildPoints = Game.MaxBuildPoints,
                                SecondsPerTurn = Game.SecondsPerTurn,
                                ApiUrl = _gameApiUrl + $"/{Game.Id}"
                            },
                            Robot = robot
                        }),
                        FunctionName = lambdaArn,
                        InvocationType = InvocationType.RequestResponse
                    });

                    // check if lambda responds within time limit
                    if(await Task.WhenAny(getActionTask, Task.Delay(TimeSpan.FromSeconds(Game.RobotTimeoutSeconds))) != getActionTask) {
                        LogInfo($"Robot {robot.Id} GetAction timed out after {stopwatch.Elapsed.TotalSeconds:N2}s");
                        return null;
                    }
                    var response = Encoding.UTF8.GetString(getActionTask.Result.Payload.ToArray());
                    var result = LambdaSerializer.Deserialize<LambdaRobotResponse>(response);
                    LogInfo($"Robot {robot.Id} GetAction responded in {stopwatch.Elapsed.TotalSeconds:N2}s:\n{response}");
                    return result.RobotAction;
                } catch(Exception e) {
                    LogErrorAsInfo(e, $"Robot {robot.Id} GetAction failed (arn: {lambdaArn})");
                    return null;
                }
            } catch(Exception e) {
                LogErrorAsWarning(e, $"Robot R{robot.Index} is out of range");
                return null;
            }
        }
    }
}
