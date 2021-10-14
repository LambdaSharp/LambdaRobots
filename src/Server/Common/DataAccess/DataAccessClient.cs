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
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using LambdaRobots.Server.DataAccess.Records;
using LambdaSharp.DynamoDB.Native;
using LambdaSharp.DynamoDB.Serialization;
using LambdaSharp.DynamoDB.Serialization.Converters;

namespace LambdaRobots.Server.DataAccess {

    public class DataAccessClient {

        //--- Types ---
        private class DynamoFloatConverter : ADynamoAttributeConverter {

            //--- Class Fields ---
            public static readonly DynamoFloatConverter Instance = new DynamoFloatConverter();

            //--- Methods ---
            public override bool CanConvert(Type typeToConvert) => (typeToConvert == typeof(float)) || (typeToConvert == typeof(float?));

            public override AttributeValue ToAttributeValue(object value, Type targetType, DynamoSerializerOptions options)
                => new AttributeValue {
                    N = ((float)value).ToString(CultureInfo.InvariantCulture)
                };

            public override object FromNumber(string value, Type targetType, DynamoSerializerOptions options) => float.Parse(value, CultureInfo.InvariantCulture);
        }

        //--- Constructors ---
        public DataAccessClient(string tableName, IAmazonDynamoDB dynamoClient = null)
            => Table = new DynamoTable(tableName, dynamoClient, new DynamoTableOptions {
                SerializerOptions = new DynamoSerializerOptions {
                    Converters = {
                        new DynamoFloatConverter()
                    }
                }
            });

        //--- Properties ---
        private IDynamoTable Table { get; }

        //--- Methods ---
        public Task<bool> CreateGameRecordAsync(GameRecord record, CancellationToken cancellationToken = default)

            // specify DynamoDB PutItem operation
            => Table.PutItem(record.GetPrimaryKey(), record)

                // game record cannot yet exist
                .WithCondition(record => DynamoCondition.DoesNotExist(record))

                // execute PutItem operation
                .ExecuteAsync(cancellationToken);

        public Task<GameRecord> GetGameRecordAsync(string gameId, CancellationToken cancellationToken = default)
            => Table.GetItemAsync(DataModel.GetPrimaryKeyForGameRecord(gameId), consistentRead: true, cancellationToken);

        public Task<bool> UpdateGameRecordAsync(string gameId, Game game, CancellationToken cancellationToken = default)
            => Table.UpdateItem(DataModel.GetPrimaryKeyForGameRecord(gameId))

                // game record must exist
                .WithCondition(record => DynamoCondition.Exists(record))

                // update Game attribute
                .Set(record => record.Game, game)

                // execute UpdateItem operation
                .ExecuteAsync(cancellationToken);

        public Task<bool> DeleteGameRecordAsync(string gameId, CancellationToken cancellationToken = default)
            => Table.DeleteItemAsync(DataModel.GetPrimaryKeyForGameRecord(gameId), cancellationToken);
    }
}