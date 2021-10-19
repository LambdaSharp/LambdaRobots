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
using LambdaRobots.Bot.Model;
using LambdaRobots.Game.Model;
using LambdaRobots.Server.DataAccess;
using LambdaSharp;
using LambdaSharp.ApiGateway;

namespace LambdaRobots.Server.GameApiFunction {

    public sealed class Function : ALambdaApiGatewayFunction, IGameDependencyProvider {

        //--- Fields ---
        private DataAccessClient _dataClient;

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            _dataClient = new DataAccessClient(config.ReadDynamoDBTableName("GameTable"));
        }

        public async Task<ScanResponse> ScanEnemiesAsync(string gameId, ScanEnemiesRequest request) {

            // fetch game record from table
            var gameRecord = await _dataClient.GetGameRecordAsync(gameId);
            if(gameRecord is null) {
                throw AbortNotFound($"could not find a game session: ID = {gameId ?? "<NULL>"}");
            }
            var gameLogic = new GameLogic(gameRecord.Game, this);

            // identify scanning robot
            var robot = gameRecord.Game.Robots.FirstOrDefault(r => r.Id == request.RobotId);
            if(robot is null) {
                throw AbortNotFound($"could not find a robot: ID = {request.RobotId}");
            }

            // find nearest enemy within scan resolution
            var found = gameLogic.ScanRobots(robot, request.Heading, request.Resolution);
            if(found != null) {
                var distance = GameMath.Distance(robot.X, robot.Y, found.X, found.Y);
                var angle = GameMath.NormalizeAngle(MathF.Atan2(found.X - robot.X, found.Y - robot.Y) * 180.0f / MathF.PI);
                LogInfo($"Scanning: Heading = {GameMath.NormalizeAngle(request.Heading):N2}, Resolution = {request.Resolution:N2}, Found = R{found.Index}, Distance = {distance:N2}, Angle = {angle:N2}");
                return new ScanResponse {
                    Found = true,
                    Distance = distance
                };
            } else {
               LogInfo($"Scanning: Heading = {GameMath.NormalizeAngle(request.Heading):N2}, Resolution = {request.Resolution:N2}, Found = nothing");
                return new ScanResponse {
                    Found = false,
                    Distance = 0.0f
                };
            }
        }

        //--- IGameDependencyProvider Members ---
        DateTimeOffset IGameDependencyProvider.UtcNow => DateTimeOffset.UtcNow;
        float IGameDependencyProvider.NextRandomFloat() => throw new NotImplementedException();
        Task<GetBuildResponse> IGameDependencyProvider.GetRobotBuild(BotInfo robot) => throw new NotImplementedException();
        Task<GetActionResponse> IGameDependencyProvider.GetRobotAction(BotInfo robot) => throw new NotImplementedException();
    }
}
