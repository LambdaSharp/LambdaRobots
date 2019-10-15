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
using System.Threading.Tasks;
using Amazon.ApiGatewayManagementApi;
using Amazon.DynamoDBv2;
using Amazon.Lambda;
using Amazon.Lambda.Core;
using Amazon.Lambda.Model;
using LambdaSharp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Challenge.LambdaRobots.Server.GameTurnAsyncFunction {

    public class Function : ALambdaFunction<GameRecord, GameRecord> {

        //--- Class Fields ---
        private static Random _random = new Random();

        //--- Fields ---
        private IAmazonLambda _lambdaClient;
        private DynamoTable _table;
        private GameTurnLogic _logic;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // initialize Lambda function
            _lambdaClient = new AmazonLambdaClient();
            _table = new DynamoTable(
                config.ReadDynamoDBTableName("GameTable"),
                new AmazonDynamoDBClient()
            );
            _logic = new GameTurnLogic(
                this,
                _lambdaClient,
                new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig {
                    ServiceURL = config.ReadText("Module::WebSocket::Url")
                }),
                config.ReadText("RestApiUrl"),
                _random
            );
        }

        public override async Task<GameRecord> ProcessMessageAsync(GameRecord request) {
            LogInfo($"Loading game state: ID = {request.Game.Id}");
            var gameRecord = await _table.GetAsync<GameRecord>(request.Game.Id);
            if(gameRecord == null) {

                // game must have been stopped
                return request;
            }

            await _logic.ComputeNextTurnAsync(request);
            var game = request.Game;

            // check if we need to update or delete the game from the game table
            if(game.State == GameState.NextTurn) {
                LogInfo($"Storing game: ID = {game.Id}");
                await _table.UpdateAsync(new GameRecord {
                    PK = game.Id,
                    Game = game
                }, new[] { nameof(GameRecord.Game) });
            } else {
                LogInfo($"Deleting game: ID = {game.Id}");
                await _table.DeleteAsync<GameRecord>(game.Id);
            }

            // check if we need to invoke the next game turn
            if(request.Game.State == GameState.NextTurn) {
                await _lambdaClient.InvokeAsync(new InvokeRequest {
                    Payload = SerializeJson(request),
                    FunctionName = CurrentContext.FunctionName,
                    InvocationType = InvocationType.Event
                });
            }
            return request;
        }
    }
}
