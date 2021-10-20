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
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LambdaRobots.Game {

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum GameStatus {
        Undefined,
        Start,
        NextTurn,
        Finished,
        Error
    }

    public sealed class GameBoard {

        //--- Properties ---
        public string Id { get; set; }

        // current state
        public GameStatus Status { get; set; }
        public DateTimeOffset LastStatusUpdate { get; set; }
        public int CurrentGameTurn { get; set; }
        public List<MissileInfo> Missiles { get; set; } = new List<MissileInfo>();
        public List<BotInfo> Bots { get; set; } = new List<BotInfo>();
        public List<Message> Messages { get; set; } = new List<Message>();

        // game characteristics
        public float BoardWidth { get; set; }
        public float BoardHeight { get; set; }
        public float DirectHitRange { get; set; }
        public float NearHitRange { get; set; }
        public float FarHitRange { get; set; }
        public float CollisionRange { get; set; }
        public float MinBotStartDistance { get; set; }
        public float BotTimeoutSeconds { get; set; }
        public int MaxTurns { get; set; }
        public int MaxBuildPoints { get; set; }
        public float MinimumSecondsPerTurn { get; set; }
    }

    public sealed class Message {

        //--- Properties ---
        public int GameTurn { get; set; }
        public string Text { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MissileStatus {
        Undefined,
        Flying,
        ExplodingDirect,
        ExplodingNear,
        ExplodingFar,
        Destroyed
    }
}
