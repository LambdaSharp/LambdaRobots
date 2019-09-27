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
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using LambdaSharp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Challenge.LambdaRobotsServer.GameLoopFunction {

    public class FunctionRequest {

        //--- Properties ---
        public string GameId { get; set; }
    }

    public class FunctionResponse {

        //--- Properties ---

        // TO-DO: add response fields
    }

    public class Function : ALambdaFunction<FunctionRequest, FunctionResponse>, IDependencyProvider {

        //--- Fields ---
        private string _gameBucketName;
        private IAmazonS3 _s3Client;
        private Game _game;

        //--- Properties ---
        DateTime IDependencyProvider.UtcNow => UtcNow;

        Game IDependencyProvider.Game => _game;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // read configuration settings
            _gameBucketName = config.ReadS3BucketName("GameBucket");

            // initialize clients
            _s3Client = new AmazonS3Client();
        }

        public override async Task<FunctionResponse> ProcessMessageAsync(FunctionRequest request) {

            // load game from S3 bucket
            _game = DeserializeJson<Game>((await _s3Client.GetObjectAsync(new GetObjectRequest {
                BucketName = _gameBucketName,
                Key = $"games/game-{request.GameId}.json"
            })).ResponseStream);

            return new FunctionResponse();
        }
    }
}
