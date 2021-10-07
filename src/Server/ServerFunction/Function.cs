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
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.Lambda;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Model;
using LambdaRobots.Api.Model;
using LambdaRobots.Server.ServerFunction.Model;
using LambdaSharp;
using LambdaSharp.ApiGateway;

namespace LambdaRobots.Server.ServerFunction {

    public class Function : ALambdaApiGatewayFunction {

        //--- Class Fields ---
        private static Random _random = new Random();

        //--- Fields ---
        private DynamoTable _table;
        private IAmazonLambda _lambdaClient;
        private string _gameTurnFunctionArn;

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            _table = new DynamoTable(
                config.ReadDynamoDBTableName("GameTable"),
                new AmazonDynamoDBClient(),
                LambdaSerializer
            );
            _lambdaClient = new AmazonLambdaClient();
            _gameTurnFunctionArn = config.ReadText("GameTurnFunction");
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
                Status = GameStatus.Start,
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
            LogInfo($"Kicking off Game Turn lambda: Name = {_gameTurnFunctionArn}");
            await _lambdaClient.InvokeAsync(new InvokeRequest {
                Payload = LambdaSerializer.Serialize(new {
                    GameId = game.Id,
                    Status = game.Status
                }),
                FunctionName = _gameTurnFunctionArn,
                InvocationType = InvocationType.Event
            });

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

            // delete game record
            LogInfo($"Deleting game record: ID = {request.GameId}");
            await _table.DeleteAsync<GameRecord>(request.GameId);

            // update game state to indicated it was stopped
            gameRecord.Game.Status = GameStatus.Finished;
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

        public async Task<ScanEnemiesResponse> ScanEnemiesAsync(string gameId, ScanEnemiesRequest request) {

            // fetch game record from table
            var gameRecord = await _table.GetAsync<GameRecord>(gameId);
            if(gameRecord == null) {
                throw AbortNotFound($"could not find a game session: ID = {gameId ?? "<NULL>"}");
            }
            var gameLogic = new GameLogic(gameRecord.Game, new GameDependencyProvider(
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
                var distance = GameMath.Distance(robot.X, robot.Y, found.X, found.Y);
                var angle = GameMath.NormalizeAngle(Math.Atan2(found.X - robot.X, found.Y - robot.Y) * 180.0 / Math.PI);
                LogInfo($"Scanning: Heading = {GameMath.NormalizeAngle(request.Heading):N2}, Resolution = {request.Resolution:N2}, Found = R{found.Index}, Distance = {distance:N2}, Angle = {angle:N2}");
                return new ScanEnemiesResponse {
                    Found = true,
                    Distance = distance
                };
            } else {
               LogInfo($"Scanning: Heading = {GameMath.NormalizeAngle(request.Heading):N2}, Resolution = {request.Resolution:N2}, Found = nothing");
                return new ScanEnemiesResponse {
                    Found = false,
                    Distance = 0.0
                };
            }
        }
    }
}
