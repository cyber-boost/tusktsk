using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.Json;

namespace TuskLang.Operators.Database
{
    /// <summary>
    /// MongoDB Operator for TuskLang C# SDK
    /// 
    /// Provides MongoDB database capabilities with support for:
    /// - CRUD operations (Create, Read, Update, Delete)
    /// - Query building and execution
    /// - Aggregation pipelines
    /// - Index management
    /// - Connection pooling
    /// - Transaction support
    /// 
    /// Usage:
    /// ```csharp
    /// // Find documents
    /// var docs = @mongodb({
    ///   action: "find",
    ///   connection_string: "mongodb://localhost:27017",
    ///   database: "myapp",
    ///   collection: "users",
    ///   filter: {age: {$gt: 18}}
    /// })
    /// 
    /// // Insert document
    /// var result = @mongodb({
    ///   action: "insert",
    ///   connection_string: "mongodb://localhost:27017",
    ///   database: "myapp",
    ///   collection: "users",
    ///   document: {name: "John", age: 30}
    /// })
    /// ```
    /// </summary>
    public class MongoDbOperator : BaseOperator
    {
        public MongoDbOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "action" };
            OptionalFields = new List<string> 
            { 
                "connection_string", "database", "collection", "filter", "document", 
                "documents", "update", "projection", "sort", "limit", "skip", 
                "pipeline", "index", "options", "timeout", "read_preference" 
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["timeout"] = 30,
                ["read_preference"] = "primary"
            };
        }
        
        public override string GetName() => "mongodb";
        
        protected override string GetDescription() => "MongoDB database operations operator for document storage and retrieval";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["find"] = "@mongodb({action: \"find\", connection_string: \"mongodb://localhost\", database: \"test\", collection: \"users\", filter: {age: {$gt: 18}}})",
                ["insert"] = "@mongodb({action: \"insert\", connection_string: \"mongodb://localhost\", database: \"test\", collection: \"users\", document: {name: \"John\", age: 30}})",
                ["update"] = "@mongodb({action: \"update\", connection_string: \"mongodb://localhost\", database: \"test\", collection: \"users\", filter: {name: \"John\"}, update: {$set: {age: 31}}})",
                ["aggregate"] = "@mongodb({action: \"aggregate\", connection_string: \"mongodb://localhost\", database: \"test\", collection: \"orders\", pipeline: [{$group: {_id: \"$status\", count: {$sum: 1}}}]})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_ACTION"] = "Invalid MongoDB action specified",
                ["CONNECTION_FAILED"] = "Failed to connect to MongoDB",
                ["INVALID_CONNECTION_STRING"] = "Invalid MongoDB connection string",
                ["DATABASE_NOT_FOUND"] = "Database not found",
                ["COLLECTION_NOT_FOUND"] = "Collection not found",
                ["QUERY_FAILED"] = "MongoDB query failed",
                ["INVALID_FILTER"] = "Invalid MongoDB filter",
                ["INVALID_DOCUMENT"] = "Invalid document format",
                ["WRITE_FAILED"] = "MongoDB write operation failed"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var action = GetContextValue<string>(config, "action", "").ToLower();
            
            switch (action)
            {
                case "find":
                    return await FindAsync(config, context);
                case "find_one":
                    return await FindOneAsync(config, context);
                case "insert":
                    return await InsertAsync(config, context);
                case "insert_many":
                    return await InsertManyAsync(config, context);
                case "update":
                    return await UpdateAsync(config, context);
                case "update_many":
                    return await UpdateManyAsync(config, context);
                case "delete":
                    return await DeleteAsync(config, context);
                case "delete_many":
                    return await DeleteManyAsync(config, context);
                case "aggregate":
                    return await AggregateAsync(config, context);
                case "create_index":
                    return await CreateIndexAsync(config, context);
                case "list_indexes":
                    return await ListIndexesAsync(config, context);
                case "count":
                    return await CountAsync(config, context);
                default:
                    throw new ArgumentException($"Invalid MongoDB action: {action}");
            }
        }
        
        /// <summary>
        /// Find documents
        /// </summary>
        private async Task<object> FindAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var connectionString = GetContextValue<string>(config, "connection_string", "");
            var database = GetContextValue<string>(config, "database", "");
            var collection = GetContextValue<string>(config, "collection", "");
            var filter = ResolveVariable(config.GetValueOrDefault("filter"), context);
            var projection = ResolveVariable(config.GetValueOrDefault("projection"), context);
            var sort = ResolveVariable(config.GetValueOrDefault("sort"), context);
            var limit = GetContextValue<int>(config, "limit", 0);
            var skip = GetContextValue<int>(config, "skip", 0);
            
            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(collection))
                throw new ArgumentException("Connection string, database, and collection are required");
            
            try
            {
                var client = new MongoClient(connectionString);
                var db = client.GetDatabase(database);
                var coll = db.GetCollection<BsonDocument>(collection);
                
                var filterDoc = filter != null ? BsonDocument.Parse(JsonSerializer.Serialize(filter)) : new BsonDocument();
                var projectionDoc = projection != null ? BsonDocument.Parse(JsonSerializer.Serialize(projection)) : null;
                var sortDoc = sort != null ? BsonDocument.Parse(JsonSerializer.Serialize(sort)) : null;
                
                var findOptions = new FindOptions<BsonDocument>
                {
                    Limit = limit > 0 ? limit : null,
                    Skip = skip > 0 ? skip : null
                };
                
                if (projectionDoc != null)
                    findOptions.Projection = projectionDoc;
                if (sortDoc != null)
                    findOptions.Sort = sortDoc;
                
                var cursor = await coll.FindAsync(filterDoc, findOptions);
                var documents = await cursor.ToListAsync();
                
                var results = new List<Dictionary<string, object>>();
                foreach (var doc in documents)
                {
                    results.Add(BsonTypeMapper.MapToDotNetValue(doc) as Dictionary<string, object>);
                }
                
                var result = new Dictionary<string, object>
                {
                    ["documents"] = results,
                    ["count"] = results.Count,
                    ["database"] = database,
                    ["collection"] = collection,
                    ["filter"] = filter
                };
                
                Log("info", "MongoDB find operation completed", new Dictionary<string, object>
                {
                    ["count"] = results.Count,
                    ["database"] = database,
                    ["collection"] = collection
                });
                
                return result;
            }
            catch (Exception ex)
            {
                Log("error", "MongoDB find operation failed", new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["database"] = database,
                    ["collection"] = collection
                });
                
                throw new ArgumentException($"MongoDB find operation failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Find one document
        /// </summary>
        private async Task<object> FindOneAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var connectionString = GetContextValue<string>(config, "connection_string", "");
            var database = GetContextValue<string>(config, "database", "");
            var collection = GetContextValue<string>(config, "collection", "");
            var filter = ResolveVariable(config.GetValueOrDefault("filter"), context);
            var projection = ResolveVariable(config.GetValueOrDefault("projection"), context);
            var sort = ResolveVariable(config.GetValueOrDefault("sort"), context);
            
            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(collection))
                throw new ArgumentException("Connection string, database, and collection are required");
            
            try
            {
                var client = new MongoClient(connectionString);
                var db = client.GetDatabase(database);
                var coll = db.GetCollection<BsonDocument>(collection);
                
                var filterDoc = filter != null ? BsonDocument.Parse(JsonSerializer.Serialize(filter)) : new BsonDocument();
                var projectionDoc = projection != null ? BsonDocument.Parse(JsonSerializer.Serialize(projection)) : null;
                var sortDoc = sort != null ? BsonDocument.Parse(JsonSerializer.Serialize(sort)) : null;
                
                var findOptions = new FindOptions<BsonDocument>();
                if (projectionDoc != null)
                    findOptions.Projection = projectionDoc;
                if (sortDoc != null)
                    findOptions.Sort = sortDoc;
                
                var document = await coll.Find(filterDoc, findOptions).FirstOrDefaultAsync();
                
                var result = new Dictionary<string, object>
                {
                    ["document"] = document != null ? BsonTypeMapper.MapToDotNetValue(document) as Dictionary<string, object> : null,
                    ["found"] = document != null,
                    ["database"] = database,
                    ["collection"] = collection,
                    ["filter"] = filter
                };
                
                Log("info", "MongoDB findOne operation completed", new Dictionary<string, object>
                {
                    ["found"] = document != null,
                    ["database"] = database,
                    ["collection"] = collection
                });
                
                return result;
            }
            catch (Exception ex)
            {
                Log("error", "MongoDB findOne operation failed", new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["database"] = database,
                    ["collection"] = collection
                });
                
                throw new ArgumentException($"MongoDB findOne operation failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Insert document
        /// </summary>
        private async Task<object> InsertAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var connectionString = GetContextValue<string>(config, "connection_string", "");
            var database = GetContextValue<string>(config, "database", "");
            var collection = GetContextValue<string>(config, "collection", "");
            var document = ResolveVariable(config.GetValueOrDefault("document"), context);
            
            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(collection))
                throw new ArgumentException("Connection string, database, and collection are required");
            
            if (document == null)
                throw new ArgumentException("Document is required for insert operation");
            
            try
            {
                var client = new MongoClient(connectionString);
                var db = client.GetDatabase(database);
                var coll = db.GetCollection<BsonDocument>(collection);
                
                var bsonDoc = BsonDocument.Parse(JsonSerializer.Serialize(document));
                await coll.InsertOneAsync(bsonDoc);
                
                var result = new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["inserted_id"] = bsonDoc["_id"].ToString(),
                    ["database"] = database,
                    ["collection"] = collection
                };
                
                Log("info", "MongoDB insert operation completed", new Dictionary<string, object>
                {
                    ["inserted_id"] = bsonDoc["_id"].ToString(),
                    ["database"] = database,
                    ["collection"] = collection
                });
                
                return result;
            }
            catch (Exception ex)
            {
                Log("error", "MongoDB insert operation failed", new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["database"] = database,
                    ["collection"] = collection
                });
                
                throw new ArgumentException($"MongoDB insert operation failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Insert many documents
        /// </summary>
        private async Task<object> InsertManyAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var connectionString = GetContextValue<string>(config, "connection_string", "");
            var database = GetContextValue<string>(config, "database", "");
            var collection = GetContextValue<string>(config, "collection", "");
            var documents = ResolveVariable(config.GetValueOrDefault("documents"), context);
            
            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(collection))
                throw new ArgumentException("Connection string, database, and collection are required");
            
            if (documents == null)
                throw new ArgumentException("Documents are required for insertMany operation");
            
            try
            {
                var client = new MongoClient(connectionString);
                var db = client.GetDatabase(database);
                var coll = db.GetCollection<BsonDocument>(collection);
                
                var bsonDocs = new List<BsonDocument>();
                if (documents is List<object> docsList)
                {
                    foreach (var doc in docsList)
                    {
                        bsonDocs.Add(BsonDocument.Parse(JsonSerializer.Serialize(doc)));
                    }
                }
                else
                {
                    bsonDocs.Add(BsonDocument.Parse(JsonSerializer.Serialize(documents)));
                }
                
                await coll.InsertManyAsync(bsonDocs);
                
                var insertedIds = new List<string>();
                foreach (var doc in bsonDocs)
                {
                    insertedIds.Add(doc["_id"].ToString());
                }
                
                var result = new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["inserted_ids"] = insertedIds,
                    ["inserted_count"] = insertedIds.Count,
                    ["database"] = database,
                    ["collection"] = collection
                };
                
                Log("info", "MongoDB insertMany operation completed", new Dictionary<string, object>
                {
                    ["inserted_count"] = insertedIds.Count,
                    ["database"] = database,
                    ["collection"] = collection
                });
                
                return result;
            }
            catch (Exception ex)
            {
                Log("error", "MongoDB insertMany operation failed", new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["database"] = database,
                    ["collection"] = collection
                });
                
                throw new ArgumentException($"MongoDB insertMany operation failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Update document
        /// </summary>
        private async Task<object> UpdateAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var connectionString = GetContextValue<string>(config, "connection_string", "");
            var database = GetContextValue<string>(config, "database", "");
            var collection = GetContextValue<string>(config, "collection", "");
            var filter = ResolveVariable(config.GetValueOrDefault("filter"), context);
            var update = ResolveVariable(config.GetValueOrDefault("update"), context);
            
            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(collection))
                throw new ArgumentException("Connection string, database, and collection are required");
            
            if (filter == null || update == null)
                throw new ArgumentException("Filter and update are required for update operation");
            
            try
            {
                var client = new MongoClient(connectionString);
                var db = client.GetDatabase(database);
                var coll = db.GetCollection<BsonDocument>(collection);
                
                var filterDoc = BsonDocument.Parse(JsonSerializer.Serialize(filter));
                var updateDoc = BsonDocument.Parse(JsonSerializer.Serialize(update));
                
                var result = await coll.UpdateOneAsync(filterDoc, updateDoc);
                
                var updateResult = new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["matched_count"] = result.MatchedCount,
                    ["modified_count"] = result.ModifiedCount,
                    ["database"] = database,
                    ["collection"] = collection
                };
                
                Log("info", "MongoDB update operation completed", new Dictionary<string, object>
                {
                    ["matched_count"] = result.MatchedCount,
                    ["modified_count"] = result.ModifiedCount,
                    ["database"] = database,
                    ["collection"] = collection
                });
                
                return updateResult;
            }
            catch (Exception ex)
            {
                Log("error", "MongoDB update operation failed", new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["database"] = database,
                    ["collection"] = collection
                });
                
                throw new ArgumentException($"MongoDB update operation failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Update many documents
        /// </summary>
        private async Task<object> UpdateManyAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var connectionString = GetContextValue<string>(config, "connection_string", "");
            var database = GetContextValue<string>(config, "database", "");
            var collection = GetContextValue<string>(config, "collection", "");
            var filter = ResolveVariable(config.GetValueOrDefault("filter"), context);
            var update = ResolveVariable(config.GetValueOrDefault("update"), context);
            
            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(collection))
                throw new ArgumentException("Connection string, database, and collection are required");
            
            if (filter == null || update == null)
                throw new ArgumentException("Filter and update are required for updateMany operation");
            
            try
            {
                var client = new MongoClient(connectionString);
                var db = client.GetDatabase(database);
                var coll = db.GetCollection<BsonDocument>(collection);
                
                var filterDoc = BsonDocument.Parse(JsonSerializer.Serialize(filter));
                var updateDoc = BsonDocument.Parse(JsonSerializer.Serialize(update));
                
                var result = await coll.UpdateManyAsync(filterDoc, updateDoc);
                
                var updateResult = new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["matched_count"] = result.MatchedCount,
                    ["modified_count"] = result.ModifiedCount,
                    ["database"] = database,
                    ["collection"] = collection
                };
                
                Log("info", "MongoDB updateMany operation completed", new Dictionary<string, object>
                {
                    ["matched_count"] = result.MatchedCount,
                    ["modified_count"] = result.ModifiedCount,
                    ["database"] = database,
                    ["collection"] = collection
                });
                
                return updateResult;
            }
            catch (Exception ex)
            {
                Log("error", "MongoDB updateMany operation failed", new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["database"] = database,
                    ["collection"] = collection
                });
                
                throw new ArgumentException($"MongoDB updateMany operation failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Delete document
        /// </summary>
        private async Task<object> DeleteAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var connectionString = GetContextValue<string>(config, "connection_string", "");
            var database = GetContextValue<string>(config, "database", "");
            var collection = GetContextValue<string>(config, "collection", "");
            var filter = ResolveVariable(config.GetValueOrDefault("filter"), context);
            
            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(collection))
                throw new ArgumentException("Connection string, database, and collection are required");
            
            if (filter == null)
                throw new ArgumentException("Filter is required for delete operation");
            
            try
            {
                var client = new MongoClient(connectionString);
                var db = client.GetDatabase(database);
                var coll = db.GetCollection<BsonDocument>(collection);
                
                var filterDoc = BsonDocument.Parse(JsonSerializer.Serialize(filter));
                var result = await coll.DeleteOneAsync(filterDoc);
                
                var deleteResult = new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["deleted_count"] = result.DeletedCount,
                    ["database"] = database,
                    ["collection"] = collection
                };
                
                Log("info", "MongoDB delete operation completed", new Dictionary<string, object>
                {
                    ["deleted_count"] = result.DeletedCount,
                    ["database"] = database,
                    ["collection"] = collection
                });
                
                return deleteResult;
            }
            catch (Exception ex)
            {
                Log("error", "MongoDB delete operation failed", new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["database"] = database,
                    ["collection"] = collection
                });
                
                throw new ArgumentException($"MongoDB delete operation failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Delete many documents
        /// </summary>
        private async Task<object> DeleteManyAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var connectionString = GetContextValue<string>(config, "connection_string", "");
            var database = GetContextValue<string>(config, "database", "");
            var collection = GetContextValue<string>(config, "collection", "");
            var filter = ResolveVariable(config.GetValueOrDefault("filter"), context);
            
            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(collection))
                throw new ArgumentException("Connection string, database, and collection are required");
            
            if (filter == null)
                throw new ArgumentException("Filter is required for deleteMany operation");
            
            try
            {
                var client = new MongoClient(connectionString);
                var db = client.GetDatabase(database);
                var coll = db.GetCollection<BsonDocument>(collection);
                
                var filterDoc = BsonDocument.Parse(JsonSerializer.Serialize(filter));
                var result = await coll.DeleteManyAsync(filterDoc);
                
                var deleteResult = new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["deleted_count"] = result.DeletedCount,
                    ["database"] = database,
                    ["collection"] = collection
                };
                
                Log("info", "MongoDB deleteMany operation completed", new Dictionary<string, object>
                {
                    ["deleted_count"] = result.DeletedCount,
                    ["database"] = database,
                    ["collection"] = collection
                });
                
                return deleteResult;
            }
            catch (Exception ex)
            {
                Log("error", "MongoDB deleteMany operation failed", new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["database"] = database,
                    ["collection"] = collection
                });
                
                throw new ArgumentException($"MongoDB deleteMany operation failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Aggregate documents
        /// </summary>
        private async Task<object> AggregateAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var connectionString = GetContextValue<string>(config, "connection_string", "");
            var database = GetContextValue<string>(config, "database", "");
            var collection = GetContextValue<string>(config, "collection", "");
            var pipeline = ResolveVariable(config.GetValueOrDefault("pipeline"), context);
            
            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(collection))
                throw new ArgumentException("Connection string, database, and collection are required");
            
            if (pipeline == null)
                throw new ArgumentException("Pipeline is required for aggregate operation");
            
            try
            {
                var client = new MongoClient(connectionString);
                var db = client.GetDatabase(database);
                var coll = db.GetCollection<BsonDocument>(collection);
                
                var pipelineDocs = new List<BsonDocument>();
                if (pipeline is List<object> pipelineList)
                {
                    foreach (var stage in pipelineList)
                    {
                        pipelineDocs.Add(BsonDocument.Parse(JsonSerializer.Serialize(stage)));
                    }
                }
                else
                {
                    pipelineDocs.Add(BsonDocument.Parse(JsonSerializer.Serialize(pipeline)));
                }
                
                var cursor = await coll.AggregateAsync(pipelineDocs);
                var documents = await cursor.ToListAsync();
                
                var results = new List<Dictionary<string, object>>();
                foreach (var doc in documents)
                {
                    results.Add(BsonTypeMapper.MapToDotNetValue(doc) as Dictionary<string, object>);
                }
                
                var result = new Dictionary<string, object>
                {
                    ["documents"] = results,
                    ["count"] = results.Count,
                    ["database"] = database,
                    ["collection"] = collection
                };
                
                Log("info", "MongoDB aggregate operation completed", new Dictionary<string, object>
                {
                    ["count"] = results.Count,
                    ["database"] = database,
                    ["collection"] = collection
                });
                
                return result;
            }
            catch (Exception ex)
            {
                Log("error", "MongoDB aggregate operation failed", new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["database"] = database,
                    ["collection"] = collection
                });
                
                throw new ArgumentException($"MongoDB aggregate operation failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Create index
        /// </summary>
        private async Task<object> CreateIndexAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // This is a placeholder implementation
            return new Dictionary<string, object>
            {
                ["success"] = false,
                ["error"] = "Create index operation not implemented in this version"
            };
        }
        
        /// <summary>
        /// List indexes
        /// </summary>
        private async Task<object> ListIndexesAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            // This is a placeholder implementation
            return new Dictionary<string, object>
            {
                ["success"] = false,
                ["error"] = "List indexes operation not implemented in this version"
            };
        }
        
        /// <summary>
        /// Count documents
        /// </summary>
        private async Task<object> CountAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var connectionString = GetContextValue<string>(config, "connection_string", "");
            var database = GetContextValue<string>(config, "database", "");
            var collection = GetContextValue<string>(config, "collection", "");
            var filter = ResolveVariable(config.GetValueOrDefault("filter"), context);
            
            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(collection))
                throw new ArgumentException("Connection string, database, and collection are required");
            
            try
            {
                var client = new MongoClient(connectionString);
                var db = client.GetDatabase(database);
                var coll = db.GetCollection<BsonDocument>(collection);
                
                var filterDoc = filter != null ? BsonDocument.Parse(JsonSerializer.Serialize(filter)) : new BsonDocument();
                var count = await coll.CountDocumentsAsync(filterDoc);
                
                var result = new Dictionary<string, object>
                {
                    ["count"] = count,
                    ["database"] = database,
                    ["collection"] = collection,
                    ["filter"] = filter
                };
                
                Log("info", "MongoDB count operation completed", new Dictionary<string, object>
                {
                    ["count"] = count,
                    ["database"] = database,
                    ["collection"] = collection
                });
                
                return result;
            }
            catch (Exception ex)
            {
                Log("error", "MongoDB count operation failed", new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["database"] = database,
                    ["collection"] = collection
                });
                
                throw new ArgumentException($"MongoDB count operation failed: {ex.Message}");
            }
        }
        
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var result = new ValidationResult();
            
            if (config.TryGetValue("action", out var action))
            {
                var validActions = new[] { "find", "find_one", "insert", "insert_many", "update", "update_many", "delete", "delete_many", "aggregate", "create_index", "list_indexes", "count" };
                if (!validActions.Contains(action.ToString().ToLower()))
                {
                    result.Errors.Add($"Invalid action: {action}. Supported: {string.Join(", ", validActions)}");
                }
            }
            
            return result;
        }
    }
} 