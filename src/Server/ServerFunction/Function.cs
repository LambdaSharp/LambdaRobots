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
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Challenge.LambdaRobots.Server.Common;
using Challenge.LambdaRobots.Server.ServerFunction.Model;
using LambdaSharp;
using LambdaSharp.ApiGateway;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Challenge.LambdaRobots.Server.ServerFunction {

    public class Function : ALambdaApiGatewayFunction {

        //--- Fields ---
        private GameTable _table;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            _table = new GameTable(
                config.ReadDynamoDBTableName("GameTable"),
                new AmazonDynamoDBClient()
            );
        }

        public async Task OpenConnectionAsync(APIGatewayProxyRequest request, string username = null) {
            LogInfo($"Connected: {request.RequestContext.ConnectionId}");
        }

        public async Task CloseConnectionAsync(APIGatewayProxyRequest request) {
            LogInfo($"Disconnected: {request.RequestContext.ConnectionId}");

            // TODO: enumerate all games and delete connection
        }

        public async Task<JoinGameResponse> JoinGameAsync(JoinGameRequest request) {

            // check if the game ID exists
            var gameRecord = await _table.GetAsync<GameRecord>(request.GameId);
            if(gameRecord == null) {
                throw AbortNotFound($"could not find a game session with ID={request.GameId ?? "<NULL>"}");
            }

            // register connection with game session
            var connection = new GameSessionRecord {
                PK = request.GameId,
                ConnectionId = CurrentRequest.RequestContext.ConnectionId
            };
            await _table.PutAsync(connection);
            return new JoinGameResponse {
                Game = gameRecord.Game
            };
        }

        public async Task<StartGameResponse> StartGameAsync(StartGameRequest request) {
            var game = new Game {
                BoardWidth = 1000.0,
                BoardHeight = 1000.0,
                SecondsPerTurn = 1.0,
                DirectHitRange = 5.0,
                NearHitRange = 20.0,
                FarHitRange = 40.0,
                CollisionRange = 2.0
            };

            // TODO:
            //  - store a game session
            //  - add robot with random positions and minimal distance from each other
            //  - kick of game step function
            throw Abort(CreateResponse(500, "Not Implemented"));
        }

        public async Task<StopGameResponse> StopGameAsync(StopGameRequest request) {

            // TODO:
            //  - delete game step function
            throw Abort(CreateResponse(500, "Not Implemented"));
        }

        public async Task<ScanEnemiesResponse> ScanEnemiesAsync(ScanEnemiesRequest request) {

            // check if the game ID exists
            var gameRecord = await _table.GetAsync<GameRecord>(request.GameId);
            if(gameRecord == null) {
                throw AbortNotFound($"could not find a game session with ID={request.GameId ?? "<NULL>"}");
            }

            // find nearest enemy within scan resolution
            var logic = new Logic(new DependencyProvider(gameRecord.Game, DateTime.UtcNow));
            var robot = gameRecord.Game.Robots.FirstOrDefault(r => r.Id == request.RobotId);
            if(robot == null) {
                throw AbortNotFound($"Could not find a robot with ID={request.RobotId}");
            }
            var distanceFound = logic.ScanRobots(robot, request.Heading, request.Resolution);
            return new ScanEnemiesResponse {
                Found = distanceFound.HasValue,
                Distance = distanceFound.GetValueOrDefault()
            };
        }
    }
}
