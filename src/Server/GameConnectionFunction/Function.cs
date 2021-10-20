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
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using LambdaRobots.Server.DataAccess;
using LambdaRobots.Server.DataAccess.Records;
using LambdaRobots.Server.GameConnectionFunction.Model;
using LambdaSharp;
using LambdaSharp.ApiGateway;

namespace LambdaRobots.Server.GameConnectionFunction {

    public class Function : ALambdaApiGatewayFunction {

        //--- Class Fields ---
        private readonly static Random _random = new Random();

        //--- Fields ---
        private DataAccessClient _dataClient;
        private string _gameTurnFunctionArn;

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            _dataClient = new DataAccessClient(config.ReadDynamoDBTableName("GameTable"));
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
                BoardWidth = request.BoardWidth ?? 1000.0f,
                BoardHeight = request.BoardHeight ?? 1000.0f,
                SecondsPerTurn = request.SecondsPerTurn ?? 0.5f,
                MaxTurns = request.MaxTurns ?? 1500,
                MaxBuildPoints = request.MaxBuildPoints ?? 8,
                DirectHitRange = request.DirectHitRange ?? 5.0f,
                NearHitRange = request.NearHitRange ?? 20.0f,
                FarHitRange = request.FarHitRange ?? 40.0f,
                CollisionRange = request.CollisionRange ?? 8.0f,
                MinBotStartDistance = request.MinBotStartDistance ?? 50.0f,
                BotTimeoutSeconds = request.BotTimeoutSeconds ?? 15.0f
            };
            var gameRecord = new GameRecord {
                GameId = game.Id,
                Game = game,
                BotArns = request.BotArns,
                ConnectionId = CurrentRequest.RequestContext.ConnectionId,
                Expire = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds()
            };

            // store game record
            await _dataClient.CreateGameRecordAsync(gameRecord);

            // dispatch game loop
            LogInfo($"Kicking off game: ID = {game.Id}");
            LogEvent(new GameKickOffEvent {
                GameId = game.Id,
                Status = game.Status
            });

            // return with kicked off game
            return new StartGameResponse {
                Game = game
            };
        }

        public async Task<StopGameResponse> StopGameAsync(StopGameRequest request) {
            LogInfo($"Stop game: ConnectionId = {CurrentRequest.RequestContext.ConnectionId}");

            // fetch game record from table
            var gameRecord = await _dataClient.GetGameRecordAsync(request.GameId);
            if(gameRecord is null) {
                LogInfo("No game found to stop");

                // game is already stopped, nothing further to do
                return new StopGameResponse();
            }

            // delete game record
            LogInfo($"Deleting game record: ID = {request.GameId}");
            await _dataClient.DeleteGameRecordAsync(request.GameId);

            // update game state to indicated it was stopped
            gameRecord.Game.Status = GameStatus.Finished;
            ++gameRecord.Game.CurrentGameTurn;
            gameRecord.Game.Messages.Add(new Message {
                GameTurn = gameRecord.Game.CurrentGameTurn,
                Text = "Game stopped."
            });

            // return final game state
            return new StopGameResponse {
                Game = gameRecord.Game
            };
        }
    }
}
