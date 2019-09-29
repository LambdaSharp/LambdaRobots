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
                var gameRecord = await _table.GetAsync<GameRecord>(request.GameId);
                if(gameRecord == null) {
                    throw new ApplicationException($"game ID={request.GameId} not found");
                }
                var logic = new Logic(new DependencyProvider(gameRecord.Game, DateTime.UtcNow, _random));

                // invoke all robot to get their actions
                var payload = new RobotLambdaRequest {
                    GameId = gameRecord.Game.Id
                };
                var timeout = TimeSpan.FromSeconds(gameRecord.Game.RobotTimeoutSeconds);
                var robotActions = await Task.WhenAll(gameRecord.Game.Robots.Select(robot => Invoke(robot.Id, payload, timeout)));
                logic.MainLoop(robotActions);

                // update game state
                ++gameRecord.Game.TotalTurns;
                await _table.UpdateAsync(gameRecord.Game);

                // check if game has not reached its max turns and if more than one robot is still alive
                return new FunctionResponse {
                    State = (
                            (gameRecord.Game.TotalTurns < gameRecord.Game.MaxTurns)
                            && (gameRecord.Game.Robots.Count(robot => robot.State == RobotState.Alive) > 1)
                        )
                        ? GameLoopState.Continue
                        : GameLoopState.Finished
                };
            } catch(Exception e) {
                LogError(e);
                return new FunctionResponse {
                    State = GameLoopState.Error
                };
            }

            // local functions
            async Task<RobotAction> Invoke(string lambdaArn, RobotLambdaRequest payload, TimeSpan timeout) {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var invocationTask = _lambdaClient.InvokeAsync(new InvokeRequest {
                    Payload = SerializeJson(payload),
                    FunctionName = lambdaArn,
                    InvocationType = InvocationType.RequestResponse
                });

                // check if lambda response within time limit
                RobotAction result;
                if(await Task.WhenAny(invocationTask, Task.Delay(timeout)) == invocationTask) {
                    var response = Encoding.UTF8.GetString(invocationTask.Result.Payload.ToArray());
                    result = DeserializeJson<RobotAction>(response);
                    LogInfo($"robot {lambdaArn} responded in {stopwatch.Elapsed.TotalSeconds:N2}s:\n{response}");
                } else {
                    result = new RobotAction();
                    LogInfo($"robot {lambdaArn} invocation timed out after {stopwatch.Elapsed.TotalSeconds:N2}s");
                }

                // always set the robot id (no cheating!)
                result.RobotId = lambdaArn;
                return result;
            }
        }
    }
}
