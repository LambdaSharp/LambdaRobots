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

        //--- Fields ---
        public string Id;

        // current state
        public GameStatus Status;
        public int TotalTurns;
        public List<LambdaRobotMissile> Missiles = new List<LambdaRobotMissile>();
        public List<LambdaRobot> Robots = new List<LambdaRobot>();
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
        public string Id;

        [DataMember(IsRequired = true)]
        public string RobotId;

        // current state

        [DataMember(IsRequired = true)]
        public MissileStatus Status;

        [DataMember(IsRequired = true)]
        public double X;

        [DataMember(IsRequired = true)]
        public double Y;

        [DataMember(IsRequired = true)]
        public double Distance;

        // missile characteristics

        [DataMember(IsRequired = true)]
        public double Speed;

        [DataMember(IsRequired = true)]
        public double Heading;

        [DataMember(IsRequired = true)]
        public double Range;

        [DataMember(IsRequired = true)]
        public double DirectHitDamageBonus;

        [DataMember(IsRequired = true)]
        public double NearHitDamageBonus;

        [DataMember(IsRequired = true)]
        public double FarHitDamageBonus;
    }
}
