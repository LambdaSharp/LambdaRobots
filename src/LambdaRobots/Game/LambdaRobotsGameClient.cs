﻿/*
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
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LambdaRobots.Game.Model;

namespace LambdaRobots.Game {

    public interface ILambdaRobotsGame {

        //--- Methods ---

        /// <summary>
        /// Scan the game board in a given heading and resolution. The resolution
        /// specifies in the scan arc from `heading-resolution` to `heading+resolution`.
        /// The max resolution is limited to `Bot.ScannerResolution`.
        /// </summary>
        /// <param name="heading">Scan heading in degrees</param>
        /// <param name="resolution">Scan resolution in degrees</param>
        /// <returns>Distance to nearest target or `null` if no target found</returns>
        Task<ScanResponse> ScanAsync(float heading, float resolution);
    }

    public sealed class LambdaRobotsGameClient : ILambdaRobotsGame {

        //--- Fields ---
        private readonly HttpClient _httpClient;
        private readonly string _gameApi;
        private readonly string _botId;

        //--- Constructors ---
        public LambdaRobotsGameClient(string gameApi, string botId, HttpClient httpClient = null) {
            _gameApi = gameApi ?? throw new ArgumentNullException(nameof(gameApi));
            _botId = botId ?? throw new ArgumentNullException(nameof(botId));
            _httpClient = httpClient ?? new HttpClient();
        }

        //--- Methods ---
        public async Task<ScanResponse> ScanAsync(float heading, float resolution) {

            // issue scan request to game API
            var postTask = _httpClient.PostAsync($"{_gameApi}/scan", new StringContent(JsonSerializer.Serialize(new ScanEnemiesRequest {
                BotId = _botId,
                Heading = heading,
                Resolution = resolution
            }), Encoding.UTF8, "application/json"));

            // wait for response or timeout
            if(await Task.WhenAny(postTask, Task.Delay(TimeSpan.FromSeconds(1.0))) != postTask) {
                return null;
            }

            // deserialize scan response
            var httpResponseText = await postTask.Result.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ScanResponse>(httpResponseText);
        }
    }
}
