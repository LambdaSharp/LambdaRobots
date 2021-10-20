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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using LambdaRobots.Bot.Model;
using LambdaRobots.Function;
using LambdaSharp.Logging;

namespace LambdaRobots.Bot {

    public interface ILambdaRobotsBot {

        //--- Methods ---
        Task<GetBuildResponse> GetBuild(GetBuildRequest request);
        Task<GetActionResponse> GetAction(GetActionRequest request);
    }

    public sealed class LambdaRobotsBotClient : ILambdaRobotsBot {

        //--- Fields ---
        private readonly string _botId;
        private readonly string _lambdaArn;
        private readonly TimeSpan _requestTimeout;
        private readonly ILambdaSharpLogger _logger;
        private IAmazonLambda _lambdaClient;

        //--- Constructors ---
        public LambdaRobotsBotClient(string botId, string lambdaArn, TimeSpan requestTimeout, IAmazonLambda lambdaClient = null, ILambdaSharpLogger logger = null) {
            _botId = botId ?? throw new ArgumentNullException(nameof(botId));
            _lambdaArn = lambdaArn ?? throw new ArgumentNullException(nameof(lambdaArn));
            _requestTimeout = requestTimeout;
            _lambdaClient = lambdaClient ?? new AmazonLambdaClient();
            _logger = logger;
        }

        //--- Methods ---
        public async Task<GetBuildResponse> GetBuild(GetBuildRequest request) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try {
                var getBuildTask = _lambdaClient.InvokeAsync(new InvokeRequest {
                    Payload = JsonSerializer.Serialize(new BotRequest {
                        Command = BotCommand.GetBuild,
                        Session = request.Session,
                        Bot = request.Bot
                    }),
                    FunctionName = _lambdaArn,
                    InvocationType = InvocationType.RequestResponse
                });

                // check if lambda responds within time limit
                if(await Task.WhenAny(getBuildTask, Task.Delay(_requestTimeout)) != getBuildTask) {
                    _logger?.LogInfo($"Bot {_botId} GetBuild timed out after {stopwatch.Elapsed.TotalSeconds:N2}s");
                    return null;
                }
                var response = Encoding.UTF8.GetString(getBuildTask.Result.Payload.ToArray());
                var result = JsonSerializer.Deserialize<BotResponse>(response);
                _logger?.LogInfo($"Bot {_botId} GetBuild responded in {stopwatch.Elapsed.TotalSeconds:N2}s:\n{response}");
                return result.BotBuild;
            } catch(Exception e) {
                _logger?.LogErrorAsInfo(e, $"Bot {_botId} GetBuild failed (arn: {_lambdaArn ?? "<NULL>"})");
                return null;
            }
        }

        public async Task<GetActionResponse> GetAction(GetActionRequest request) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try {
                var getActionTask = _lambdaClient.InvokeAsync(new InvokeRequest {
                    Payload = JsonSerializer.Serialize(new BotRequest {
                        Command = BotCommand.GetAction,
                        Session = request.Session,
                        Bot = request.Bot
                    }),
                    FunctionName = _lambdaArn,
                    InvocationType = InvocationType.RequestResponse
                });

                // check if lambda responds within time limit
                if(await Task.WhenAny(getActionTask, Task.Delay(_requestTimeout)) != getActionTask) {
                    _logger?.LogInfo($"Bot {_botId} GetAction timed out after {stopwatch.Elapsed.TotalSeconds:N2}s");
                    return null;
                }
                var response = Encoding.UTF8.GetString(getActionTask.Result.Payload.ToArray());
                var result = JsonSerializer.Deserialize<BotResponse>(response);
                _logger?.LogInfo($"Bot {_botId} GetAction responded in {stopwatch.Elapsed.TotalSeconds:N2}s:\n{response}");
                return result.BotAction;
            } catch(Exception e) {
                _logger?.LogErrorAsInfo(e, $"Bot {_botId} GetAction failed (arn: {_lambdaArn})");
                return null;
            }
        }
    }
}
