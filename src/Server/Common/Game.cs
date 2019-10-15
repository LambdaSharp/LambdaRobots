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
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LambdaRobots.Server {

    [JsonConverter(typeof(StringEnumConverter))]
    public enum GameState {
        Undefined,
        Start,
        NextTurn,
        Finished,
        Error
    }

    public class Game {

        //--- Fields ---
        public string Id;

        // current state
        public GameState State;
        public int TotalTurns;
        public List<RobotMissile> Missiles = new List<RobotMissile>();
        public List<Robot> Robots = new List<Robot>();
        public List<Message> Messages = new List<Message>();

        // game characteristics
        public double BoardWidth;
        public double BoardHeight;
        public double SecondsPerTurn;
        public double DirectHitRange;
        public double NearHitRange;
        public double FarHitRange;
        public double CollisionRange;
        public double MinRobotStartDistance;
        public double RobotTimeoutSeconds;
        public int MaxTurns;
        public int MaxBuildPoints;
    }

    public class Message {

        //--- Fields ---
        public int GameTurn;
        public string Text;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum MissileState {
        Undefined,
        Flying,
        ExplodingDirect,
        ExplodingNear,
        ExplodingFar,
        Destroyed
    }

    public class RobotMissile {

        //--- Fields ---

        [JsonRequired]
        public string Id;

        [JsonRequired]
        public string RobotId;

        // current state

        [JsonRequired]
        public MissileState State;

        [JsonRequired]
        public double X;

        [JsonRequired]
        public double Y;

        [JsonRequired]
        public double Distance;

        // missile characteristics

        [JsonRequired]
        public double Speed;

        [JsonRequired]
        public double Heading;

        [JsonRequired]
        public double Range;

        [JsonRequired]
        public double DirectHitDamageBonus;

        [JsonRequired]
        public double NearHitDamageBonus;

        [JsonRequired]
        public double FarHitDamageBonus;
    }
}
