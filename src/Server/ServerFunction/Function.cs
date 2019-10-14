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
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.Lambda;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Model;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Challenge.LambdaRobots.Api.Model;
using Challenge.LambdaRobots.Server;
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
        private string _gameTurnAsyncFunctionArn;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            _table = new GameTable(
                config.ReadDynamoDBTableName("GameTable"),
                new AmazonDynamoDBClient()
            );
            _gameStateMachine = config.ReadText("GameLoopStateMachine");
            _stepFunctionsClient = new AmazonStepFunctionsClient();
            _lambdaClient = new AmazonLambdaClient();
            _gameTurnAsyncFunctionArn = config.ReadText("GameTurnAsyncFunction");
        }

        public async Task OpenConnectionAsync(APIGatewayProxyRequest request, string username = null) {
            LogInfo($"Connected: {request.RequestContext.ConnectionId}");
        }

        public async Task CloseConnectionAsync(APIGatewayProxyRequest request) {
            LogInfo($"Disconnected: {request.RequestContext.ConnectionId}");
        }

        public async Task<StartGameResponse> StartGameAsync(StartGameRequest request) {
            LogInfo($"Starting a new game: ConnectionId = {CurrentRequest.RequestContext.ConnectionId}");

            // create a new game
            var game = new Game {
                Id = Guid.NewGuid().ToString("N"),
                State = GameState.Start,
                BoardWidth = request.BoardWidth ?? 1000.0,
                BoardHeight = request.BoardHeight ?? 1000.0,
                SecondsPerTurn = request.SecondsPerTurn ?? 0.5,
                MaxTurns = request.MaxTurns ?? 300,
                MaxBuildPoints = request.MaxBuildPoints ?? 8,
                DirectHitRange = request.DirectHitRange ?? 5.0,
                NearHitRange = request.NearHitRange ?? 20.0,
                FarHitRange = request.FarHitRange ?? 40.0,
                CollisionRange = request.CollisionRange ?? 8.0,
                MinRobotStartDistance = request.MinRobotStartDistance ?? 50.0,
                RobotTimeoutSeconds = request.RobotTimeoutSeconds ?? 15.0
            };
            var gameRecord = new GameRecord {
                PK = game.Id,
                Game = game,
                LambdaRobotArns = request.RobotArns,
                ConnectionId = CurrentRequest.RequestContext.ConnectionId
            };

            // store game record
            await _table.CreateAsync(gameRecord);

            // dispatch game loop
            switch(request.GameLoopType) {
            case GameLoopType.Recursive:
                LogInfo($"Kicking off Game Turn lambda: Name = {_gameTurnAsyncFunctionArn}");
                await _lambdaClient.InvokeAsync(new InvokeRequest {
                    Payload = SerializeJson(gameRecord),
                    FunctionName = _gameTurnAsyncFunctionArn,
                    InvocationType = InvocationType.Event
                });
                break;
            case GameLoopType.StepFunction:

                // kick off game step function
                var startGameId = $"LambdaRobotsGame-{game.Id}";
                LogInfo($"Kicking off Step Function: Name = {startGameId}");
                var startGame = await _stepFunctionsClient.StartExecutionAsync(new StartExecutionRequest {
                    StateMachineArn = _gameStateMachine,
                    Name = startGameId,
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
                break;
            default:
                throw new ApplicationException($"unsupported: GameLoopType = {request.GameLoopType}");
            }

            // return with kicked off game
            return new StartGameResponse {
                Game = game
            };
        }

        public async Task<StopGameResponse> StopGameAsync(StopGameRequest request) {
            LogInfo($"Stop game: ConnectionId = {CurrentRequest.RequestContext.ConnectionId}");

            // fetch game record from table
            var gameRecord = await _table.GetAsync<GameRecord>(request.GameId);
            if(gameRecord == null) {
                LogInfo("No game found to stop");

                // game is already stopped, nothing further to do
                return new StopGameResponse();
            }

            // check if game state machine needs to be stopped
            if(gameRecord.GameLoopArn != null) {
                LogInfo($"Stopping Step Function: Name = {gameRecord.GameLoopArn}");
                await _stepFunctionsClient.StopExecutionAsync(new StopExecutionRequest {
                    ExecutionArn = gameRecord.GameLoopArn,
                    Cause = "user requested game to be stopped"
                });
            }

            // delete game record
            LogInfo($"Deleting game record: ID = {request.GameId}");
            await _table.DeleteAsync<GameRecord>(request.GameId);

            // update game state to indicated it was stopped
            gameRecord.Game.State = GameState.Finished;
            ++gameRecord.Game.TotalTurns;
            gameRecord.Game.Messages.Add(new Message {
                GameTurn = gameRecord.Game.TotalTurns,
                Text = "Game stopped."
            });

            // return final game state
            return new StopGameResponse {
                Game = gameRecord.Game
            };
        }

        public async Task<ScanEnemiesResponse> ScanEnemiesAsync(ScanEnemiesRequest request) {

            // fetch game record from table
            var gameRecord = await _table.GetAsync<GameRecord>(request.GameId);
            if(gameRecord == null) {
                throw AbortNotFound($"could not find a game session: ID = {request.GameId ?? "<NULL>"}");
            }
            var gameLogic = new GameLogic(new GameDependencyProvider(
                gameRecord.Game,
                _random,
                r => throw new NotImplementedException("not implementation for GetBuild"),
                r => throw new NotImplementedException("not implementation for GetAction")
            ));

            // identify scanning robot
            var robot = gameRecord.Game.Robots.FirstOrDefault(r => r.Id == request.RobotId);
            if(robot == null) {
                throw AbortNotFound($"could not find a robot: ID = {request.RobotId}");
            }

            // find nearest enemy within scan resolution
            var found = gameLogic.ScanRobots(robot, request.Heading, request.Resolution);
            if(found != null) {
                var distance = GameLogic.Distance(robot.X, robot.Y, found.X, found.Y);
                var angle = GameLogic.NormalizeAngle(Math.Atan2(found.X - robot.X, found.Y - robot.Y) * 180.0 / Math.PI);
                LogInfo($"Scanning: Heading = {request.Heading:N2}, Resolution = {request.Resolution:N2}, Found = R{found.Index}, Distance = {distance:N2}, Angle = {angle:N2}");
                return new ScanEnemiesResponse {
                    Found = true,
                    Distance = distance
                };
            } else {
               LogInfo($"Scanning: Heading = {request.Heading:N2}, Resolution = {request.Resolution:N2}, Found = nothing");
                return new ScanEnemiesResponse {
                    Found = false,
                    Distance = 0.0
                };
            }
        }
    }
}
