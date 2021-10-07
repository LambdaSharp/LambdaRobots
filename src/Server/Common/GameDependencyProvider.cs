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
using System.Threading.Tasks;
using LambdaRobots.Protocol;

namespace LambdaRobots.Server {

    public class GameDependencyProvider : IGameDependencyProvider {

        //--- Fields ---
        private Game _game;
        private Random _random;
        private readonly Func<LambdaRobot, Task<LambdaRobotBuild>> _getBuild;
        private readonly Func<LambdaRobot, Task<LambdaRobotAction>> _getAction;

        //--- Constructors ---
        public GameDependencyProvider(
            Random random,
            Func<LambdaRobot, Task<LambdaRobotBuild>> getBuild,
            Func<LambdaRobot, Task<LambdaRobotAction>> getAction
        ) {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _getBuild = getBuild ?? throw new ArgumentNullException(nameof(getBuild));
            _getAction = getAction ?? throw new ArgumentNullException(nameof(getAction));
        }

        //--- Methods ---
        public double NextRandomDouble() => _random.NextDouble();
        public Task<LambdaRobotBuild> GetRobotBuild(LambdaRobot robot) => _getBuild(robot);
        public Task<LambdaRobotAction> GetRobotAction(LambdaRobot robot) => _getAction(robot);
    }
}
