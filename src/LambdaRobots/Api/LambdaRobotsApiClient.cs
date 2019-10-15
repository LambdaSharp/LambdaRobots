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
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LambdaRobots.Api.Model;
using Newtonsoft.Json;

namespace LambdaRobots.Api {

    public class LambdaRobotsApiClient {

        //--- Fields ---
        private readonly HttpClient _httpClient;
        private readonly string _gameApi;
        private readonly string _gameId;
        private readonly string _robotId;

        //--- Constructors ---
        public LambdaRobotsApiClient(HttpClient httpClient, string gameApi, string gameId, string robotId) {
            _httpClient = httpClient ?? throw new System.ArgumentNullException(nameof(httpClient));
            _gameApi = gameApi ?? throw new System.ArgumentNullException(nameof(gameApi));
            _gameId = gameId ?? throw new System.ArgumentNullException(nameof(gameId));
            _robotId = robotId ?? throw new System.ArgumentNullException(nameof(robotId));
        }

        //--- Methods ---

        /// <summary>
        /// Scan the game board in a given heading and resolution. The resolution
        /// specifies in the scan arc from `heading-resolution` to `heading+resolution`.
        /// The max resolution is limited to `Robot.ScannerResolution`.
        /// </summary>
        /// <param name="heading">Scan heading in degrees</param>
        /// <param name="resolution">Scan resolution in degrees</param>
        /// <returns>Distance to nearest target or `null` if no target found</returns>
        public async Task<(bool Success, bool Found, double Distance)> ScanAsync(double heading, double resolution) {

            // issue scan request to game API
            var postTask = _httpClient.PostAsync($"{_gameApi}/scan", new StringContent(JsonConvert.SerializeObject(new ScanEnemiesRequest {
                GameId = _gameId,
                RobotId = _robotId,
                Heading = heading,
                Resolution = resolution
            }), Encoding.UTF8, "application/json"));

            // wait for response or timeout
            if(await Task.WhenAny(postTask, Task.Delay(TimeSpan.FromSeconds(1.0))) != postTask) {
                return (Success: false, Found: false, Distance: 0.0);
            }

            // deserialize scan response
            var httpResponseText = await postTask.Result.Content.ReadAsStringAsync();
            var response = JsonConvert.DeserializeObject<ScanEnemiesResponse>(httpResponseText);
            return (Success: true, Found: response.Found, Distance: response.Distance);
        }
    }
}
