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
using Amazon.Lambda;
using Amazon.Lambda.Core;
using Amazon.Lambda.Model;
using Challenge.LambdaRobots.Common;
using Challenge.LambdaRobots.Server.Common;
using LambdaSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Challenge.LambdaRobots.Server.GameLoopFunction {

    public class RobotLambdaRequest {

        //--- Properties ---
        public string GameId { get; set; }
        public Robot Robot { get; set; }
    }

    public class RobotLambdaResponse {

        //--- Properties ---
        public RobotAction RobotAction { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum GameLoopState {
        Undefined,
        Continue,
        Finished,
        Error
    }

    public class FunctionRequest {

        //--- Properties ---
        public string GameId { get; set; }
    }

    public class FunctionResponse {

        //--- Properties ---
        public GameLoopState State { get; set; }
    }

    public class Function : ALambdaFunction<FunctionRequest, FunctionResponse> {

        //--- Class Fields ---
        private static Random _random = new Random();

        //--- Fields ---
        private GameTable _table;
        private IAmazonLambda _lambdaClient;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            _table = new GameTable(
                config.ReadDynamoDBTableName("GameTable"),
                new AmazonDynamoDBClient()
            );
            _lambdaClient = new AmazonLambdaClient();
        }

        public override async Task<FunctionResponse> ProcessMessageAsync(FunctionRequest request) {
            try {

                // get game state
                LogInfo($"loading game state for ID={request.GameId}");
                var game = (await _table.GetAsync<GameRecord>(request.GameId))?.Game;
                if(game == null) {
                    throw new ApplicationException($"game ID={request.GameId} not found");
                }
                var logic = new Logic(new DependencyProvider(game, DateTime.UtcNow, _random));

                // start turn
                ++game.TotalTurns;
                LogInfo($"start turn {game.TotalTurns} (max: {game.MaxTurns}): invoking {game.Robots.Count(robot => robot.State == RobotState.Alive)} robots (total: {game.Robots.Count})");
                var messageCount = game.Messages.Count;

                // invoke all robots to get their actions
                var robotResponses = await Task.WhenAll(game.Robots.Where(robot => robot.State == RobotState.Alive).Select(robot => Invoke(game, robot)));
                logic.MainLoop(robotResponses.Select(response => response.RobotAction).Where(action => action != null).ToList());

                // show new messages
                for(var i = messageCount; i < game.Messages.Count; ++i) {
                    LogInfo($"game message {i + 1}: {game.Messages[i].Text}");
                }

                // end turn
                var robotsAlive = game.Robots.Count(robot => robot.State == RobotState.Alive);
                LogInfo($"end turn {game.TotalTurns} (max: {game.MaxTurns}): {robotsAlive} robots alive");
                await _table.UpdateAsync(game);

                // check if game has not reached its max turns and if more than one robot is still alive
                return new FunctionResponse {
                    State = ((game.TotalTurns < game.MaxTurns) && (robotsAlive > 1))
                        ? GameLoopState.Continue
                        : GameLoopState.Finished
                };
            } catch(Exception e) {
                LogError(e, "error during game loop");
                return new FunctionResponse {
                    State = GameLoopState.Error
                };
            }

            // local functions
            async Task<RobotLambdaResponse> Invoke(Game game, Robot robot) {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                try {
                    var invocationTask = _lambdaClient.InvokeAsync(new InvokeRequest {
                        Payload = SerializeJson(new RobotLambdaRequest {
                            GameId = game.Id,
                            Robot = robot
                        }),
                        FunctionName = robot.LambdaArn,
                        InvocationType = InvocationType.RequestResponse
                    });

                    // check if lambda responds within time limit
                    if(await Task.WhenAny(invocationTask, Task.Delay(TimeSpan.FromSeconds(game.RobotTimeoutSeconds))) != invocationTask) {
                        LogInfo($"robot {robot.Id} invocation timed out after {stopwatch.Elapsed.TotalSeconds:N2}s");

                        // kill the robot
                        robot.State = RobotState.Dead;
                        return new RobotLambdaResponse();
                    }
                    var response = Encoding.UTF8.GetString(invocationTask.Result.Payload.ToArray());
                    var result = DeserializeJson<RobotLambdaResponse>(response);
                    LogInfo($"robot {robot.Id} responded in {stopwatch.Elapsed.TotalSeconds:N2}s:\n{response}");

                    // always set the robot id (no cheating!)
                    if(result.RobotAction != null) {
                        result.RobotAction.RobotId = robot.Id;
                    }
                    return result;
                } catch(Exception e) {
                    LogErrorAsWarning(e, $"invocation for robot {robot.Id} failed (arn: {robot.LambdaArn})");

                    // kill the robot
                    robot.State = RobotState.Dead;
                    return new RobotLambdaResponse();
                }
            }
        }
    }
}
