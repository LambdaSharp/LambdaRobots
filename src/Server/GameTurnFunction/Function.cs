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
using Amazon.Lambda.Core;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using LambdaRobots.Protocol;
using LambdaSharp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace LambdaRobots.Server.GameTurnFunction {

    public class FunctionRequest {

        //--- Properties ---
        public string GameId { get; set; }
        public GameState State { get; set; }
        public GameLoopType GameLoopType { get; set; } = GameLoopType.StepFunction;
    }

    public class FunctionResponse {

        //--- Properties ---
        public string GameId { get; set; }
        public GameState State { get; set; }
        public GameLoopType GameLoopType { get; set; } = GameLoopType.StepFunction;
    }

    public class Function : ALambdaFunction<FunctionRequest, FunctionResponse> {

        //--- Class Fields ---
        private static Random _random = new Random();

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
                new AmazonDynamoDBClient()
            );
            _gameApiUrl = config.ReadText("RestApiUrl");
        }

        public override async Task<FunctionResponse> ProcessMessageAsync(FunctionRequest request) {

            // get game state from DynamoDB table
            LogInfo($"Loading game state: ID = {request.GameId}");
            var gameRecord = await _table.GetAsync<GameRecord>(request.GameId);
            if(gameRecord == null) {

                // game must have been stopped
                return new FunctionResponse {
                    GameId = request.GameId,
                    State = GameState.Finished,
                    GameLoopType = request.GameLoopType
                };
            }

            // compute next turn
            var game = gameRecord.Game;
            LogInfo($"Game turn {game.TotalTurns}");
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
                    LogInfo($"Start game: initializing {game.Robots.Count(robot => robot.Status == LambdaRobotStatus.Alive)} robots (total: {game.Robots.Count})");
                    await logic.StartAsync(gameRecord.LambdaRobotArns.Count);
                    game.State = GameState.NextTurn;
                    LogInfo($"Done: {game.Robots.Count(robot => robot.Status == LambdaRobotStatus.Alive)} robots ready");
                    break;
                case GameState.NextTurn:

                    // next turn
                    LogInfo($"Start turn {game.TotalTurns} (max: {game.MaxTurns}): invoking {game.Robots.Count(robot => robot.Status == LambdaRobotStatus.Alive)} robots (total: {game.Robots.Count})");
                    await logic.NextTurnAsync();
                    LogInfo($"End turn: {game.Robots.Count(robot => robot.Status == LambdaRobotStatus.Alive)} robots alive");
                    break;
                case GameState.Finished:

                    // nothing further to do
                    break;
                default:
                    game.State = GameState.Error;
                    throw new ApplicationException($"unexpected game state: '{gameRecord.Game.State}'");
                }
            } catch(Exception e) {
                LogError(e, "error during game loop");
                game.State = GameState.Error;
            }

            // log new game messages
            for(var i = messageCount; i < game.Messages.Count; ++i) {
                LogInfo($"Game message {i + 1}: {game.Messages[i].Text}");
            }

            // check if we need to update or delete the game from the game table
            if(game.State == GameState.NextTurn) {
                LogInfo($"Storing game: ID = {game.Id}");

                // attempt to update the game record
                try {
                    await _table.UpdateAsync(new GameRecord {
                        PK = game.Id,
                        Game = game
                    }, new[] { nameof(GameRecord.Game) });
                } catch {
                    LogInfo($"Storing game failed: ID = {game.Id}");

                    // the record failed to updated, because the game was stopped
                    return new FunctionResponse {
                        GameId = game.Id,
                        State = GameState.Finished,
                        GameLoopType = request.GameLoopType
                    };
                }
            } else {
                LogInfo($"Deleting game: ID = {game.Id}");
                await _table.DeleteAsync<GameRecord>(game.Id);
            }

            // notify WebSocket of new game state
            LogInfo($"Posting game update to connection: {game.Id}");
            try {
                await _amaClient.PostToConnectionAsync(new PostToConnectionRequest {
                    ConnectionId = gameRecord.ConnectionId,
                    Data = new MemoryStream(Encoding.UTF8.GetBytes(SerializeJson(new GameTurnNotification {
                        Game = game
                    })))
                });
            } catch(AmazonServiceException e) when(e.StatusCode == System.Net.HttpStatusCode.Gone) {

                // connection has been closed, stop the game
                LogInfo($"Connection is gone");
                game.State = GameState.Finished;
                await _table.DeleteAsync<GameRecord>(game.Id);
            } catch(Exception e) {
                LogErrorAsWarning(e, "PostToConnectionAsync() failed");
            }

            // check if we need to invoke the next game turn
            if((request.GameLoopType == GameLoopType.Recursive) && (game.State == GameState.NextTurn)) {
                await _lambdaClient.InvokeAsync(new InvokeRequest {
                    Payload = SerializeJson(request),
                    FunctionName = CurrentContext.FunctionName,
                    InvocationType = InvocationType.Event
                });
            }
            return new FunctionResponse {
                GameId = game.Id,
                State = game.State,
                GameLoopType = request.GameLoopType
            };
        }

        private async Task<LambdaRobotBuild> GetRobotBuildAsync(Game game, LambdaRobot robot, string lambdaArn) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try {
                var getNameTask = _lambdaClient.InvokeAsync(new InvokeRequest {
                    Payload = SerializeJson(new LambdaRobotRequest {
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
                    LogInfo($"Robot {robot.Id} GetName timed out after {stopwatch.Elapsed.TotalSeconds:N2}s");
                    return null;
                }
                var response = Encoding.UTF8.GetString(getNameTask.Result.Payload.ToArray());
                var result = DeserializeJson<LambdaRobotResponse>(response);
                LogInfo($"Robot {robot.Id} GetName responded in {stopwatch.Elapsed.TotalSeconds:N2}s:\n{response}");
                return result.RobotBuild;
            } catch(Exception e) {
                LogErrorAsWarning(e, $"Robot {robot.Id} GetName failed (arn: {lambdaArn})");
                return null;
            }
        }

        private async Task<LambdaRobotAction> GetRobotActionAsync(Game game, LambdaRobot robot, string lambdaArn) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try {
                var getActionTask = _lambdaClient.InvokeAsync(new InvokeRequest {
                    Payload = SerializeJson(new LambdaRobotRequest {
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
                    LogInfo($"Robot {robot.Id} GetAction timed out after {stopwatch.Elapsed.TotalSeconds:N2}s");
                    return null;
                }
                var response = Encoding.UTF8.GetString(getActionTask.Result.Payload.ToArray());
                var result = DeserializeJson<LambdaRobotResponse>(response);
                LogInfo($"Robot {robot.Id} GetAction responded in {stopwatch.Elapsed.TotalSeconds:N2}s:\n{response}");
                return result.RobotAction;
            } catch(Exception e) {
                LogErrorAsWarning(e, $"Robot {robot.Id} GetAction failed (arn: {lambdaArn})");
                return null;
            }
        }
    }
}
