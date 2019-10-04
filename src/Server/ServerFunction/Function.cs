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
            _gameStateMachine = config.ReadText("GameLoopStateMachine");
            _stepFunctionsClient = new AmazonStepFunctionsClient();
            _lambdaClient = new AmazonLambdaClient();
        }

        public async Task OpenConnectionAsync(APIGatewayProxyRequest request, string username = null) {
            LogInfo($"Connected: {request.RequestContext.ConnectionId}");
        }

        public async Task CloseConnectionAsync(APIGatewayProxyRequest request) {
            LogInfo($"Disconnected: {request.RequestContext.ConnectionId}");
        }

        public async Task<StartGameResponse> StartGameAsync(StartGameRequest request) {

            // create a new game
            var game = new Game {
                Id = CurrentRequest.RequestContext.ConnectionId,
                State = GameState.Start,
                BoardWidth = request.BoardWidth ?? 1000.0,
                BoardHeight = request.BoardHeight ?? 1000.0,
                SecondsPerTurn = request.SecondsPerTurn ?? 1.0,
                MaxTurns = request.MaxTurns ?? 300,
                DirectHitRange = request.DirectHitRange ?? 5.0,
                NearHitRange = request.NearHitRange ?? 20.0,
                FarHitRange = request.FarHitRange ?? 40.0,
                CollisionRange = request.CollisionRange ?? 2.0,
                MinRobotStartDistance = request.MinRobotStartDistance ?? 100.0,
                RobotTimeoutSeconds = request.RobotTimeoutSeconds ?? 15.0
            };

            // store game record
            await _table.CreateAsync(new GameRecord {
                PK = game.Id,
                Game = game,
                LambdaRobotArns = request.RobotArns
            });

            // kick off game step function
            var startGame = await _stepFunctionsClient.StartExecutionAsync(new StartExecutionRequest {
                StateMachineArn = _gameStateMachine,
                Name = $"LambdaRobotsGame-{game.Id}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                Input = SerializeJson(new {
                    GameId = game.Id,
                    State = game.State
                })
            });

            // update execution ARN for game record
            await _table.UpdateAsync(new GameRecord {
                PK = game.Id,
                GameLoopArn = startGame.ExecutionArn
            }, new[] {
                nameof(GameRecord.GameLoopArn)
            });

            // return with kicked off game
            return new StartGameResponse {
                Game = game
            };
        }

        public async Task<StopGameResponse> StopGameAsync(StopGameRequest request) {

            // fetch game record from table
            var gameRecord = await _table.GetAsync<GameRecord>(request.GameId);
            if(gameRecord == null) {

                // game is already stopped, nothing further to do
                return new StopGameResponse();
            }

            // check if game state machine needs to be stopped
            if(gameRecord.GameLoopArn != null) {
                await _stepFunctionsClient.StopExecutionAsync(new StopExecutionRequest {
                    ExecutionArn = gameRecord.GameLoopArn,
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

            // fetch game record from table
            var gameRecord = await _table.GetAsync<GameRecord>(request.GameId);
            if(gameRecord == null) {
                throw AbortNotFound($"could not find a game session with ID={request.GameId ?? "<NULL>"}");
            }
            var gameLogic = new GameLogic(new DependencyProvider(
                gameRecord.Game,
                DateTime.UtcNow,
                _random,
                r => throw new NotImplementedException("not implementation for GetConfig"),
                r => throw new NotImplementedException("not implementation for GetAction")
            ));

            // identify scanning robot
            var robot = gameRecord.Game.Robots.FirstOrDefault(r => r.Id == request.RobotId);
            if(robot == null) {
                throw AbortNotFound($"could not find a robot with ID={request.RobotId}");
            }

            // find nearest enemy within scan resolution
            var distanceFound = gameLogic.ScanRobots(robot, request.Heading, request.Resolution);
            return new ScanEnemiesResponse {
                Found = distanceFound.HasValue,
                Distance = distanceFound.GetValueOrDefault()
            };
        }
    }
}
