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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Amazon.Lambda;
using Amazon.Lambda.Core;
using Amazon.Lambda.Model;
using Challenge.LambdaRobots.Common;
using Challenge.LambdaRobots.Server.Common;
using LambdaSharp;
using System.IO;
using Amazon.Runtime;
using System.Collections.Generic;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Challenge.LambdaRobots.Server.GameTurnFunction {

    public class FunctionRequest {

        //--- Properties ---
        public string GameId { get; set; }
        public GameState State { get; set; }
    }

    public class FunctionResponse {

        //--- Properties ---
        public string GameId { get; set; }
        public GameState State { get; set; }
        public List<string> Messages { get; set; }
    }

    public class Function : ALambdaFunction<FunctionRequest, FunctionResponse> {

        //--- Class Fields ---
        private static Random _random = new Random();

        //--- Fields ---
        private GameTable _table;
        private string _gameApi;
        private IAmazonLambda _lambdaClient;
        private IAmazonApiGatewayManagementApi _amaClient;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // initialize Lambda function
            _table = new GameTable(
                config.ReadDynamoDBTableName("GameTable"),
                new AmazonDynamoDBClient()
            );
            _gameApi = config.ReadText("RestApiUrl");
            _lambdaClient = new AmazonLambdaClient();
            _amaClient = new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig {
                ServiceURL = config.ReadText("Module::WebSocket::Url")
            });
        }

        public override async Task<FunctionResponse> ProcessMessageAsync(FunctionRequest request) {

            // get game state from DynamoDB table
            LogInfo($"Loading game state for ID={request.GameId}");
            var gameRecord = await _table.GetAsync<GameRecord>(request.GameId);
            if(gameRecord == null) {
                throw new ApplicationException($"game ID={request.GameId} not found");
            }
            var game = gameRecord.Game;
            var logic = new GameLogic(new DependencyProvider(
                gameRecord.Game,
                DateTime.UtcNow,
                _random,
                robot => GetRobotConfigAsync(game, robot, gameRecord.LambdaRobotArns[gameRecord.Game.Robots.IndexOf(robot)]),
                robot => GetRobotActionAsync(game, robot, gameRecord.LambdaRobotArns[gameRecord.Game.Robots.IndexOf(robot)])
            ));

            // determine game action to take
            var messageCount = game.Messages.Count;
            try {

                // check game state
                switch(request.State) {
                case GameState.Start:

                    // start game
                    LogInfo($"Start game: initializing {game.Robots.Count(robot => robot.State == RobotState.Alive)} robots (total: {game.Robots.Count})");
                    await logic.StartAsync(gameRecord.LambdaRobotArns.Count);
                    game.State = GameState.NextTurn;
                    LogInfo($"Done: {game.Robots.Count(robot => robot.State == RobotState.Alive)} robots ready");
                    break;
                case GameState.NextTurn:

                    // next turn
                    LogInfo($"Start turn {game.TotalTurns} (max: {game.MaxTurns}): invoking {game.Robots.Count(robot => robot.State == RobotState.Alive)} robots (total: {game.Robots.Count})");
                    await logic.NextTurnAsync();
                    LogInfo($"End turn: {game.Robots.Count(robot => robot.State == RobotState.Alive)} robots alive");
                    break;
                case GameState.Finished:

                    // nothing further to do
                    break;
                default:
                    game.State = GameState.Error;
                    throw new ApplicationException($"unexpected game state: '{request.State}'");
                }
            } catch(Exception e) {
                LogError(e, "error during game loop");
                game.State = GameState.Error;
            }

            // log new game messages
            var newMessages = new List<string>();
            for(var i = messageCount; i < game.Messages.Count; ++i) {
                LogInfo($"Game message {i + 1}: {game.Messages[i].Text}");
                newMessages.Add(game.Messages[i].Text);
            }

            // notify WebSocket of new game state
            LogInfo($"Posting game update to connection: {game.Id}");
            try {
                await _amaClient.PostToConnectionAsync(new PostToConnectionRequest {
                    ConnectionId = game.Id,
                    Data = new MemoryStream(Encoding.UTF8.GetBytes(SerializeJson(game)))
                });
            } catch(AmazonServiceException e) when(e.StatusCode == System.Net.HttpStatusCode.Gone) {

                // connection has been closed, stop the game
                LogInfo($"Connection is gone");
                game.State = GameState.Finished;
            } catch(Exception e) {
                LogErrorAsWarning(e, "PostToConnectionAsync() failed");
            }

            // check if we need to update or delete the game from the game table
            if(game.State == GameState.NextTurn) {
                LogInfo($"Storing game ID={game.Id}");
                await _table.UpdateAsync(new GameRecord {
                    PK = game.Id,
                    Game = game
                }, new[] { nameof(GameRecord.Game) });
            } else {
                LogInfo($"Deleting game ID={game.Id}");
                await _table.DeleteAsync<GameRecord>(game.Id);
            }
            return new FunctionResponse {
                GameId = game.Id,
                State = game.State,
                Messages = newMessages
            };
        }

        private async Task<RobotConfig> GetRobotConfigAsync(Game game, Robot robot, string lambdaArn) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try {
                var getNameTask = _lambdaClient.InvokeAsync(new InvokeRequest {
                    Payload = SerializeJson(new RobotRequest {
                        Command = RobotCommand.GetConfig,
                        GameId = game.Id
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
                var result = DeserializeJson<RobotResponse>(response);
                LogInfo($"Robot {robot.Id} GetName responded in {stopwatch.Elapsed.TotalSeconds:N2}s:\n{response}");
                return result.RobotConfig;
            } catch(Exception e) {
                LogErrorAsWarning(e, $"Robot {robot.Id} GetName failed (arn: {lambdaArn})");
                return null;
            }
        }


        private async Task<RobotAction> GetRobotActionAsync(Game game, Robot robot, string lambdaArn) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try {
                var getActionTask = _lambdaClient.InvokeAsync(new InvokeRequest {
                    Payload = SerializeJson(new RobotRequest {
                        Command = RobotCommand.GetAction,
                        GameId = game.Id,
                        GameBoardWidth = game.BoardWidth,
                        GameBoardHeight = game.BoardHeight,
                        GameMaxTurns = game.MaxTurns,
                        GameApi = _gameApi,
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
                var result = DeserializeJson<RobotResponse>(response);
                LogInfo($"Robot {robot.Id} GetAction responded in {stopwatch.Elapsed.TotalSeconds:N2}s:\n{response}");
                return result.RobotAction;
            } catch(Exception e) {
                LogErrorAsWarning(e, $"Robot {robot.Id} GetAction failed (arn: {lambdaArn})");
                return null;
            }
        }
    }
}
