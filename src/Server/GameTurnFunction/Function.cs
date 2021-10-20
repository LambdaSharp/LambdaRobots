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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Amazon.Lambda;
using Amazon.Runtime;
using LambdaRobots.Bot;
using LambdaRobots.Bot.Model;
using LambdaRobots.Server.DataAccess;
using LambdaRobots.Server.DataAccess.Records;
using LambdaSharp;
using LambdaSharp.EventBridge;

namespace LambdaRobots.Server.GameTurnFunction {

    public class Function : ALambdaEventFunction<GameKickOffEvent>, IGameDependencyProvider {

        //--- Class Fields ---
        private readonly static Random _random = new Random();

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Fields ---
        private IAmazonLambda _lambdaClient;
        private IAmazonApiGatewayManagementApi _amaClient;
        private DataAccessClient _dataClient;
        private string _gameApiUrl;
        private GameRecord _gameRecord;

        //--- Properties ---
        public GameRecord GameRecord {
            get => _gameRecord ?? throw new InvalidOperationException();
            set => _gameRecord = value;
        }

        public Game Game => GameRecord.Game;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // initialize Lambda function
            _lambdaClient = new AmazonLambdaClient();
            _amaClient = new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig {
                ServiceURL = config.ReadText("Module::WebSocket::Url")
            });
            _dataClient = new DataAccessClient(config.ReadDynamoDBTableName("GameTable"));
            _gameApiUrl = config.ReadText("RestApiUrl");
        }

        public override async Task ProcessEventAsync(GameKickOffEvent message) {
            var gameId = message.GameId;

            // get game state from DynamoDB table
            LogInfo($"Loading game state: ID = {gameId}");
            GameRecord = await _dataClient.GetGameRecordAsync(gameId);
            try {
                if(GameRecord?.Game?.Status != GameStatus.Start) {
                    LogWarn($"Game state is invalid: ID = {gameId}");
                    return;
                }
                try {

                    // initialize game logic
                    var logic = new GameLogic(this.Game, this);

                    // initialize bots
                    LogInfo($"Start game: initializing {Game.Bots.Count(bot => bot.Status == BotStatus.Alive)} bots (total: {Game.Bots.Count})");
                    await logic.StartAsync(GameRecord.BotArns.Count);
                    Game.Status = GameStatus.NextTurn;
                    LogInfo($"Bots initialized: {Game.Bots.Count(bot => bot.Status == BotStatus.Alive)} bots ready");

                    // loop until we're done
                    var messageCount = Game.Messages.Count;
                    while(Game.Status == GameStatus.NextTurn) {
                        var now = ((IGameDependencyProvider)this).UtcNow;

                        // make sure turns are not too fast
                        var timeSinceLastTurn = now - Game.LastStatusUpdate;
                        var minimumTurnTimespan = TimeSpan.FromSeconds(Game.MinimumSecondsPerTurn);
                        if(timeSinceLastTurn < minimumTurnTimespan) {
                            await Task.Delay(minimumTurnTimespan - timeSinceLastTurn);
                        }
                        var timelapseSeconds = (float)timeSinceLastTurn.TotalSeconds;
                        Game.LastStatusUpdate = now;

                        // next turn
                        LogInfo($"Start turn {Game.CurrentGameTurn} (max: {Game.MaxTurns}): invoking {Game.Bots.Count(bot => bot.Status == BotStatus.Alive)} bots (total: {Game.Bots.Count})");
                        logic.NextTurn(timelapseSeconds);
                        for(var i = messageCount; i < Game.Messages.Count; ++i) {
                            LogInfo($"Game message {i + 1}: {Game.Messages[i].Text}");
                        }
                        LogInfo($"End turn {Game.CurrentGameTurn}: {Game.Bots.Count(bot => bot.Status == BotStatus.Alive)} bots alive");

                        // attempt to update the game record
                        LogInfo($"Storing game: ID = {gameId}");
                        if(!await _dataClient.UpdateGameRecordAsync(gameId, Game)) {
                            throw new ApplicationException("unable to update game record");
                        }

                        // notify WebSocket of new game state
                        if(!await TryPostGameUpdateAsync(GameRecord.ConnectionId, Game)) {
                            return;
                        }
                    }
                } catch(Exception e) {
                    LogError(e, "Error during game loop");

                    // notify frontend that the game is over
                    Game.Status = GameStatus.Error;
                    await TryPostGameUpdateAsync(GameRecord.ConnectionId, Game);
                } finally {

                    // delete game from table
                    LogInfo($"Deleting game: ID = {gameId}");
                    await _dataClient.DeleteGameRecordAsync(gameId);
                }
            } finally {
                GameRecord = null;
            }
        }

        private async Task<bool> TryPostGameUpdateAsync(string connectionId, Game game) {
            LogInfo($"Posting game update to connection: {game.Id}");
            try {
                var json = LambdaSerializer.Serialize(new GameTurnNotification {
                    Game = game
                });
                await _amaClient.PostToConnectionAsync(new PostToConnectionRequest {
                    ConnectionId = connectionId,
                    Data = new MemoryStream(Encoding.UTF8.GetBytes(json))
                });
            } catch(AmazonServiceException e) when(e.StatusCode == System.Net.HttpStatusCode.Gone) {

                // connection has been closed, stop the game
                LogInfo($"Connection is gone");
                game.Status = GameStatus.Finished;
                return false;
            } catch(Exception e) {
                LogErrorAsWarning(e, "Failed posting game update");
            }
            return true;
        }

        //--- IGameDependencyProvider Members ---
        DateTimeOffset IGameDependencyProvider.UtcNow => DateTimeOffset.UtcNow;
        float IGameDependencyProvider.NextRandomFloat() => (float)_random.NextDouble();

        Task<GetBuildResponse> IGameDependencyProvider.GetBotBuild(BotInfo bot) {
            try {
                var client = new LambdaRobotsBotClient(bot.Id, GameRecord.BotArns[bot.Index], TimeSpan.FromSeconds(Game.BotTimeoutSeconds), _lambdaClient, this);
                return client.GetBuild(new GetBuildRequest {
                    GameInfo = new GameInfo {
                        Id = Game.Id,
                        BoardWidth = Game.BoardWidth,
                        BoardHeight = Game.BoardHeight,
                        DirectHitRange = Game.DirectHitRange,
                        NearHitRange = Game.NearHitRange,
                        FarHitRange = Game.FarHitRange,
                        CollisionRange = Game.CollisionRange,
                        CurrentGameTurn = Game.CurrentGameTurn,
                        MaxGameTurns = Game.MaxTurns,
                        MaxBuildPoints = Game.MaxBuildPoints,
                        SecondsSinceLastTurn = Game.MinimumSecondsPerTurn
                    },
                    Bot = bot
                });
            } catch(Exception e) {
                LogErrorAsWarning(e, $"Bot R{bot.Index} is out of range");
                return null;
            }
        }

        Task<GetActionResponse> IGameDependencyProvider.GetBotAction(BotInfo bot) {
            try {
                var client = new LambdaRobotsBotClient(bot.Id, GameRecord.BotArns[bot.Index], TimeSpan.FromSeconds(Game.BotTimeoutSeconds), _lambdaClient, this);
                return client.GetAction(new GetActionRequest {
                    GameInfo = new GameInfo {
                        Id = Game.Id,
                        BoardWidth = Game.BoardWidth,
                        BoardHeight = Game.BoardHeight,
                        DirectHitRange = Game.DirectHitRange,
                        NearHitRange = Game.NearHitRange,
                        FarHitRange = Game.FarHitRange,
                        CollisionRange = Game.CollisionRange,
                        CurrentGameTurn = Game.CurrentGameTurn,
                        MaxGameTurns = Game.MaxTurns,
                        MaxBuildPoints = Game.MaxBuildPoints,
                        SecondsSinceLastTurn = (float)(Game.LastStatusUpdate - bot.LastStatusUpdate).TotalSeconds,
                        ApiUrl = _gameApiUrl + $"/{Game.Id}"
                    },
                    Bot = bot
                });
            } catch(Exception e) {
                LogErrorAsWarning(e, $"Bot R{bot.Index} is out of range");
                return null;
            }
        }
    }
}
