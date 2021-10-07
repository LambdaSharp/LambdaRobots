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

using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace LambdaRobots.Server {

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum GameStatus {
        Undefined,
        Start,
        NextTurn,
        Finished,
        Error
    }

    public class Game {

        //--- Properties ---
        public string Id { get; set; }

        // current state
        public GameStatus Status { get; set; }
        public int TotalTurns { get; set; }
        public List<LambdaRobotMissile> Missiles { get; set; } = new List<LambdaRobotMissile>();
        public List<LambdaRobot> Robots { get; set; } = new List<LambdaRobot>();
        public List<Message> Messages { get; set; } = new List<Message>();

        // game characteristics
        public double BoardWidth { get; set; }
        public double BoardHeight { get; set; }
        public double SecondsPerTurn { get; set; }
        public double DirectHitRange { get; set; }
        public double NearHitRange { get; set; }
        public double FarHitRange { get; set; }
        public double CollisionRange { get; set; }
        public double MinRobotStartDistance { get; set; }
        public double RobotTimeoutSeconds { get; set; }
        public int MaxTurns { get; set; }
        public int MaxBuildPoints { get; set; }
    }

    public class Message {

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

    public class LambdaRobotMissile {

        //--- Fields ---

        [DataMember(IsRequired = true)]
        public string Id { get; set; }

        [DataMember(IsRequired = true)]
        public string RobotId { get; set; }

        // current state

        [DataMember(IsRequired = true)]
        public MissileStatus Status { get; set; }

        [DataMember(IsRequired = true)]
        public double X { get; set; }

        [DataMember(IsRequired = true)]
        public double Y { get; set; }

        [DataMember(IsRequired = true)]
        public double Distance { get; set; }

        // missile characteristics

        [DataMember(IsRequired = true)]
        public double Speed { get; set; }

        [DataMember(IsRequired = true)]
        public double Heading { get; set; }

        [DataMember(IsRequired = true)]
        public double Range { get; set; }

        [DataMember(IsRequired = true)]
        public double DirectHitDamageBonus { get; set; }

        [DataMember(IsRequired = true)]
        public double NearHitDamageBonus { get; set; }

        [DataMember(IsRequired = true)]
        public double FarHitDamageBonus { get; set; }
    }
}
