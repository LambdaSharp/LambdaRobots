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
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Model;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Challenge.LambdaRobots.Common;
using Challenge.LambdaRobots.Server.Common;
using Challenge.LambdaRobots.Server.ServerFunction.Model;
using LambdaSharp;
using LambdaSharp.ApiGateway;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Challenge.LambdaRobots.Server.ServerFunction {

    public class Function : ALambdaApiGatewayFunction {

        //--- Class Fields ---
        private static Random _random = new Random();

        //--- Fields ---
        private GameTable _table;
        private string _gameStateMachine;
        private IAmazonStepFunctions _stepFunctionsClient;
        private IAmazonLambda _lambdaClient;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            _table = new GameTable(
                config.ReadDynamoDBTableName("GameTable"),
                new AmazonDynamoDBClient()
            );
            _gameStateMachine = config.ReadText("GameStateMachine");
            _stepFunctionsClient = new AmazonStepFunctionsClient();
            _lambdaClient = new AmazonLambdaClient();
        }

        public async Task OpenConnectionAsync(APIGatewayProxyRequest request, string username = null) {
            LogInfo($"Connected: {request.RequestContext.ConnectionId}");
        }

        public async Task CloseConnectionAsync(APIGatewayProxyRequest request) {
            LogInfo($"Disconnected: {request.RequestContext.ConnectionId}");

            // stop the game, just in case it's still going
            await StopGameAsync(new StopGameRequest {
                GameId = request.RequestContext.ConnectionId
            });
        }

        public async Task<StartGameResponse> StartGameAsync(StartGameRequest request) {

            // create a new game
            var game = new Game {
                Id = CurrentRequest.RequestContext.ConnectionId,
                BoardWidth = 1000.0,
                BoardHeight = 1000.0,
                SecondsPerTurn = 1.0,
                DirectHitRange = 5.0,
                NearHitRange = 20.0,
                FarHitRange = 40.0,
                CollisionRange = 2.0,
                MinRobotStartDistance = 100.0,
                RobotTimeoutSeconds = 10.0,
                TotalTurns = 0,
                MaxTurns = 300
            };
            var logic = new Logic(new DependencyProvider(game, DateTime.UtcNow, _random));

            // collect names
            game.Robots.AddRange((await Task.WhenAll(request.RobotArns.Select(async robotArn => {

                // create new robot data structure
                var robot = new Robot {

                    // robot state
                    Id = $"{game.Id}:R{game.Robots.Count}",
                    LambdaArn = robotArn,
                    State = RobotState.Alive,
                    X = 0.0,
                    Y = 0.0,
                    Speed = 0.0,
                    Heading = 0.0,
                    TotalTravelDistance = 0.0,
                    Damage = 0.0,
                    ReloadDelay = 0.0,
                    TotalMissileFiredCount = 0,

                    // robot characteristics
                    MaxSpeed = 100.0,
                    Acceleration = 10.0,
                    Deceleration = -20.0,
                    MaxTurnSpeed = 50.0,
                    ScannerRange = 600.0,
                    ScannerResolution = 10.0,
                    MaxDamage = 100.0,
                    CollisionDamage = 2.0,
                    DirectHitDamage = 8.0,
                    NearHitDamage = 4.0,
                    FarHitDamage = 2.0,

                    // missile characteristics
                    MissileReloadDelay = 2.0,
                    MissileSpeed = 50.0,
                    MissileRange = 700.0,
                    MissileDirectHitDamageBonus = 3.0,
                    MissileNearHitDamageBonus = 2.1,
                    MissileFarHitDamageBonus = 1.0
                };

                // get robot configuration
                await GetRobotConfig(robot);
                return robot;
            }))).Where(robot => robot.State == RobotState.Alive).ToList());

            // reset game
            logic.Reset();

            // create game record
            await _table.CreateAsync(new GameRecord {
                PK = game.Id,
                Game = game
            });

            // kick off game step function
            var startGame = await _stepFunctionsClient.StartExecutionAsync(new StartExecutionRequest {
                StateMachineArn = _gameStateMachine,
                Name = $"LambdaRobotsGame-{game.Id}",
                Input = SerializeJson(new {
                    GameId = game.Id
                })
            });

            // update execution ARN for game record
            await _table.UpdateAsync(new GameRecord {
                PK = game.Id,
                GameExecutionArn = startGame.ExecutionArn
            }, new[] { nameof(GameRecord.GameExecutionArn) });

            // return with kicked off game
            return new StartGameResponse {
                Game = game
            };

            // local functions
            async Task<RobotResponse> GetRobotConfig(Robot robot) {
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

                        // kill the robot
                        robot.State = RobotState.Dead;
                        return new RobotResponse();
                    }
                    var response = Encoding.UTF8.GetString(getNameTask.Result.Payload.ToArray());
                    var result = DeserializeJson<RobotResponse>(response);
                    LogInfo($"Robot {robot.Id} GetName responded in {stopwatch.Elapsed.TotalSeconds:N2}s:\n{response}");

                    // always set the robot name
                    if(result.RobotConfig == null) {
                        result.RobotConfig = new RobotConfig{
                            Name = $"Robot-{robot.Id}"
                        };
                    }
                    return result;
                } catch(Exception e) {
                    LogErrorAsWarning(e, $"Robot {robot.Id} GetName failed (arn: {robot.LambdaArn})");

                    // kill the robot
                    robot.State = RobotState.Dead;
                    return new RobotResponse();
                }
            }
        }

        public async Task<StopGameResponse> StopGameAsync(StopGameRequest request) {

            // attempt to fetch game from table
            var gameRecord = await _table.GetAsync<GameRecord>(request.GameId);
            if(gameRecord == null) {

                // game is already stopped, nothing further to do
                return new StopGameResponse();
            }

            // check if game state machine needs to be stopped
            if(gameRecord.GameExecutionArn != null) {
                await _stepFunctionsClient.StopExecutionAsync(new StopExecutionRequest {
                    ExecutionArn = gameRecord.GameExecutionArn,
                    Cause = "user requested game to be stopped"
                });
            }

            // delete game record
            await _table.DeleteAsync<GameRecord>(request.GameId);

            // return final game state
            return new StopGameResponse {
                Game = gameRecord.Game
            };
        }

        public async Task<ScanEnemiesResponse> ScanEnemiesAsync(ScanEnemiesRequest request) {

            // check if the game ID exists
            var gameRecord = await _table.GetAsync<GameRecord>(request.GameId);
            if(gameRecord == null) {
                throw AbortNotFound($"could not find a game session with ID={request.GameId ?? "<NULL>"}");
            }

            // find nearest enemy within scan resolution
            var logic = new Logic(new DependencyProvider(gameRecord.Game, DateTime.UtcNow, _random));
            var robot = gameRecord.Game.Robots.FirstOrDefault(r => r.Id == request.RobotId);
            if(robot == null) {
                throw AbortNotFound($"could not find a robot with ID={request.RobotId}");
            }
            var distanceFound = logic.ScanRobots(robot, request.Heading, request.Resolution);
            return new ScanEnemiesResponse {
                Found = distanceFound.HasValue,
                Distance = distanceFound.GetValueOrDefault()
            };
        }
    }
}
