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

using LambdaRobots.Game.DataAccess.Records;
using LambdaSharp.DynamoDB.Native;

namespace LambdaRobots.Game.DataAccess {

    public static class DataModel {

        //--- Constants ---
        private const string GAME_RECORD_PK_FORMAT = "GAMES";
        private const string GAME_RECORD_SK_FORMAT = "GAME={0}";

        //--- Extension Methods ---
        public static DynamoPrimaryKey<GameRecord> GetPrimaryKey(this GameRecord record)
            => GetPrimaryKeyForGameRecord(record.GameSession.Id);

        //--- Methods ---
        public static DynamoPrimaryKey<GameRecord> GetPrimaryKeyForGameRecord(string gameId)
            => new DynamoPrimaryKey<GameRecord>(GAME_RECORD_PK_FORMAT, GAME_RECORD_SK_FORMAT, gameId);
    }
}