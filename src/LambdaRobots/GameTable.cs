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
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;
using LambdaSharp;

namespace LambdaRobots {

    public interface IDynamoTableRecord { }

    public interface IDynamoTableSingletonRecord : IDynamoTableRecord {

        //--- Properties ---
        string SK { get; }
    }

    public interface IGameTableMultiRecord : IDynamoTableRecord {

        //--- Properties ---
        string SKPrefix { get; }
    }

    public class DynamoTable {

        //--- Fields ---
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly Table _table;
        private readonly ILambdaSerializer _serializer;

        //--- Constructors ---
        public DynamoTable(string tableName, IAmazonDynamoDB dynamoDbClient, ILambdaSerializer serializer) {
            _dynamoDbClient = dynamoDbClient ?? throw new ArgumentNullException(nameof(dynamoDbClient));
            _table = Table.LoadTable(_dynamoDbClient, tableName ?? throw new ArgumentNullException(nameof(tableName)));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        //--- Methods ---
        public async Task CreateAsync<T>(T record) where T : IDynamoTableRecord {
            await _table.PutItemAsync(Document.FromJson(_serializer.Serialize(record)), new PutItemOperationConfig {
                ConditionalExpression = new Expression {
                    ExpressionStatement = "attribute_not_exists(PK)"
                }
            });
        }

        public async Task UpdateAsync<T>(T record) where T : IDynamoTableRecord {
            await _table.PutItemAsync(Document.FromJson(JsonSerializer.Serialize(record)), new PutItemOperationConfig {
                ConditionalExpression = new Expression {
                    ExpressionStatement = "attribute_exists(PK)"
                }
            });
        }

        public async Task UpdateAsync<T>(T record, IEnumerable<string> columnsToUpdate) where T : IDynamoTableRecord {
            var document = Document.FromJson(_serializer.Serialize(record));
            if(columnsToUpdate.Any()) {
                foreach(var key in document.Keys.Where(key => (key != "PK") && (key != "SK") && !columnsToUpdate.Contains(key)).ToList()) {
                    document.Remove(key);
                }
            }
            await _table.UpdateItemAsync(document, new UpdateItemOperationConfig());
        }

        public Task CreateOrUpdateAsync<T>(T record) => _table.PutItemAsync(Document.FromJson(_serializer.Serialize(record)));

        public Task<T> GetAsync<T>(string pk) where T : IDynamoTableSingletonRecord, new()
            => GetAsync<T>(pk, new T().SK);

        public async Task<T> GetAsync<T>(string pk, string sk) where T : IDynamoTableRecord {
            var record = await _table.GetItemAsync(new Dictionary<string, DynamoDBEntry> {
                ["PK"] = pk,
                ["SK"] = sk
            });
            return (record != null)
                ? _serializer.Deserialize<T>(record.ToJson())
                : default(T);
        }

        public Task DeleteAsync<T>(string pk) where T : IDynamoTableSingletonRecord, new()
            => DeleteAsync(pk, new T().SK);

        public Task DeleteAsync(string pk, string sk)
            => _table.DeleteItemAsync(pk, sk);

        public async Task<IEnumerable<T>> GetAllRecordsAsync<T>(string pk) where T : IGameTableMultiRecord, new() {
            var records = await _table.Query(pk, new Expression {
                ExpressionStatement = "begins_with(SK, :sortkeyprefix)",
                ExpressionAttributeValues = {
                    [":sortkeyprefix"] = new T().SKPrefix
                }
            }).GetRemainingAsync();
            return records.Select(record => _serializer.Deserialize<T>(record.ToJson())).ToList();
        }
    }
}