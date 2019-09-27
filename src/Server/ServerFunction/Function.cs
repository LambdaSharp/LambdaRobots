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
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using LambdaSharp;
using LambdaSharp.ApiGateway;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Challenge.LambdaRobots.Server.ServerFunction {

    public class Function : ALambdaApiGatewayFunction {

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // TO-DO: add function initialization and reading configuration settings
        }

        public async Task OpenConnectionAsync(APIGatewayProxyRequest request, string username = null) {
            LogInfo($"Connected: {request.RequestContext.ConnectionId}");
            // CurrentUser = new ConnectionUser {
            //     ConnectionId = request.RequestContext.ConnectionId,
            //     UserName = username ?? $"Anonymous-{RandomString(6)}"
            // };
            // await _table.PutRowAsync(CurrentUser);
        }

        public async Task CloseConnectionAsync(APIGatewayProxyRequest request) {
            LogInfo($"Disconnected: {request.RequestContext.ConnectionId}");
            // CurrentUser = await _table.GetRowAsync<ConnectionUser>(CurrentRequest.RequestContext.ConnectionId);
            // if(CurrentUser != null) {
            //     await _table.DeleteRowAsync(CurrentUser.ConnectionId);
            // }
        }

        public async Task<JoinGameResponse> JoinGameAsync(JoinGameRequest request) {

            // TO-DO: add business logic for API Gateway resource endpoint
            return new JoinGameResponse { };
        }

        public async Task<StartGameResponse> StartGameAsync(StartGameRequest request) {

            // TO-DO: add business logic for API Gateway resource endpoint
            return new StartGameResponse { };
        }

        public async Task<StopGameResponse> StopGameAsync(StopGameRequest request) {

            // TO-DO: add business logic for API Gateway resource endpoint
            return new StopGameResponse { };
        }

        public async Task<ScanEnemiesResponse> ScanEnemiesAsync(ScanEnemiesRequest request) {

            // TO-DO: add business logic for API Gateway resource endpoint
            return new ScanEnemiesResponse { };
        }
    }
}
