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

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Challenge.LambdaRobots.Server.GameLoopFunction {

    public class FunctionRequest {

        //--- Properties ---
        public string GameId { get; set; }
    }

    public class FunctionResponse {

        //--- Properties ---
        public GameState State { get; set; }
    }

    public class Function : ALambdaFunction<FunctionRequest, FunctionResponse> {

        //--- Class Fields ---
        private static Random _random = new Random();

        //--- Fields ---
        private GameTable _table;
        private IAmazonLambda _lambdaClient;
        private IAmazonApiGatewayManagementApi _amaClient;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            _table = new GameTable(
                config.ReadDynamoDBTableName("GameTable"),
                new AmazonDynamoDBClient()
            );
            _lambdaClient = new AmazonLambdaClient();
            _amaClient = new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig {
                ServiceURL = config.ReadText("Module::WebSocket::Url")
            });
        }

        public override async Task<FunctionResponse> ProcessMessageAsync(FunctionRequest request) {

            // get game
            LogInfo($"Loading game state for ID={request.GameId}");
            var game = (await _table.GetAsync<GameRecord>(request.GameId))?.Game;
            if(game == null) {
                throw new ApplicationException($"game ID={request.GameId} not found");
            }
            var logic = new GameLogic(new DependencyProvider(
                game,
                DateTime.UtcNow,
                _random,
                robot => GetRobotConfigAsync(game, robot),
                robot => GetRobotActionAsync(game, robot)
            ));

            // update game
            var messageCount = game.Messages.Count;
            try {

                // check game state
                switch(game.State) {
                case GameState.Start:

                    // initialize game
                    LogInfo($"Start game: initializing {game.Robots.Count(robot => robot.State == RobotState.Alive)} robots (total: {game.Robots.Count})");
                    await logic.StartAsync();
                    game.State = GameState.NextTurn;
                    LogInfo($"Done: {game.Robots.Count(robot => robot.State == RobotState.Alive)} robots ready");
                    break;
                case GameState.NextTurn:

                    // next turn
                    ++game.TotalTurns;
                    LogInfo($"Start turn {game.TotalTurns} (max: {game.MaxTurns}): invoking {game.Robots.Count(robot => robot.State == RobotState.Alive)} robots (total: {game.Robots.Count})");
                    await logic.NextTurnAsync();
                    LogInfo($"End turn {game.TotalTurns} (max: {game.MaxTurns}): {game.Robots.Count(robot => robot.State == RobotState.Alive)} robots alive");

                    // TODO: compute next turn
                    break;
                case GameState.Finished:
                    break;
                default:
                    throw new ApplicationException($"unexpected game state: '{game.State}'");
                }

                // show new messages
                for(var i = messageCount; i < game.Messages.Count; ++i) {
                    LogInfo($"Game message {i + 1}: {game.Messages[i].Text}");
                }

                // update game state
                game.State = ((game.TotalTurns < game.MaxTurns) && (game.Robots.Count(robot => robot.State == RobotState.Alive) > 1))
                    ? GameState.NextTurn
                    : GameState.Finished;
            } catch(Exception e) {
                LogError(e, "error during game loop");
                game.State = GameState.Error;
            }

            // check if we need to update or delete the game from the game table
            if(game.State == GameState.NextTurn) {
                LogInfo($"Storing game ID={game.Id}");
                await _table.UpdateAsync(new GameRecord {
                    PK = game.Id,
                    Game = game
                });
            } else {
                LogInfo($"Deleting game ID={game.Id}");
                await _table.DeleteAsync<GameRecord>(game.Id);
            }

            // notify WebSocket of new game state
            LogInfo($"Posting game update to connection: {game.Id}");
            try {
                await _amaClient.PostToConnectionAsync(new PostToConnectionRequest {
                    ConnectionId = game.Id,
                    Data = new MemoryStream(Encoding.UTF8.GetBytes(SerializeJson(game)))
                });
            } catch(AmazonServiceException e) when(e.StatusCode == System.Net.HttpStatusCode.Gone) {
                LogInfo($"Connection is gone");
            } catch(Exception e) {
                LogErrorAsWarning(e, "PostToConnectionAsync() failed");
            }

            // check if game has not reached its max turns and if more than one robot is still alive
            return new FunctionResponse {
                State = game.State
            };
        }

        private async Task<RobotConfig> GetRobotConfigAsync(Game game, Robot robot) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try {
                var getNameTask = _lambdaClient.InvokeAsync(new InvokeRequest {
                    Payload = SerializeJson(new RobotRequest {
                        Command = RobotCommand.GetConfig,
                        GameId = game.Id
                    }),
                    FunctionName = robot.LambdaArn,
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
                LogErrorAsWarning(e, $"Robot {robot.Id} GetName failed (arn: {robot.LambdaArn})");
                return null;
            }
        }


        private async Task<RobotAction> GetRobotActionAsync(Game game, Robot robot) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try {
                var getActionTask = _lambdaClient.InvokeAsync(new InvokeRequest {
                    Payload = SerializeJson(new RobotRequest {
                        Command = RobotCommand.GetAction,
                        GameId = game.Id,
                        Robot = robot,

                        // TODO: pass in server REST API
                        ServerApi = "TODO"
                    }),
                    FunctionName = robot.LambdaArn,
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
                LogErrorAsWarning(e, $"Robot {robot.Id} GetAction failed (arn: {robot.LambdaArn})");
                return null;
            }
        }
    }
}
