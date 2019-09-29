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
using Challenge.LambdaRobots.Common;

namespace Challenge.LambdaRobots.Server.Common {

    public class Game {

        //--- Fields ---
        public string Id;
        public double BoardWidth;
        public double BoardHeight;
        public double SecondsPerTurn;
        public double DirectHitRange;
        public double NearHitRange;
        public double FarHitRange;
        public double CollisionRange;
        public double MinRobotStartDistance;
        public List<RobotMissile> Missiles = new List<RobotMissile>();
        public List<Robot> Robots = new List<Robot>();
        public List<Message> Messages = new List<Message>();
    }

    public class Message {

        //--- Fields ---
        public DateTime Timestamp;
        public string Text;
    }

    public enum MissileState {
        Undefined,
        Flying,
        Exploding,
        Destroyed
    }

    public class RobotMissile {

        //--- Fields ---
        public string Id;
        public string RobotId;

        // current state
        public MissileState State;
        public double X;
        public double Y;
        public double Distance;

        // missile characteristics
        public double Speed;
        public double Heading;
        public double Range;
        public double DirectHitDamageBonus;
        public double NearHitDamageBonus;
        public double FarHitDamageBonus;
    }

    public class RobotAction {

        //--- Fields ---
        public string RobotId;
        public double? Speed;
        public double? Heading;
        public double? FireMissileHeading;
        public double? FireMissileRange;
    }
}
