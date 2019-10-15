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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace LambdaRobots.Server.ServerFunction.Model {

    public class StartGameRequest {

        //--- Properties ---
        [JsonRequired]
        public string Action { get; set; }

        [JsonRequired]
        public List<string> RobotArns { get; set; } = new List<string>();

        // optional board initialization settings
        public GameLoopType GameLoopType { get; set; } = GameLoopType.StepFunction;
        public double? BoardWidth { get; set; }
        public double? BoardHeight { get; set; }
        public double? SecondsPerTurn { get; set; }
        public int? MaxTurns { get; set; }
        public int? MaxBuildPoints { get; set; }
        public double? DirectHitRange { get; set; }
        public double? NearHitRange { get; set; }
        public double? FarHitRange { get; set; }
        public double? CollisionRange { get; set; }
        public double? MinRobotStartDistance { get; set; }
        public double? RobotTimeoutSeconds { get; set; }
    }

    public class StartGameResponse {

        //--- Properties ---
        [JsonRequired]
        public Game Game { get; set; }
    }
}