using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.ObjectPool;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

// MongoDB dependencies
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

// Redis dependencies
using StackExchange.Redis;

// Cosmos DB dependencies
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace TuskLang.Database.NoSQL
{
    /// <summary>
    /// Production-ready NoSQL database adapters for TuskLang C# SDK
    /// Implements MongoDB, Redis, and Cosmos DB adapters with enterprise-grade features
    /// </summary>
    public class NoSQLDatabaseAdapters : IDisposable
    {
        private readonly ILogger<NoSQLDatabaseAdapters> _logger;
        private readonly IMemoryCache _cache;
        private readonly ConcurrentDictionary<string, object> _adapters;
        private readonly ObjectPool<IMongoClient> _mongoClientPool;
        private readonly ObjectPool<IConnectionMultiplexer> _redisConnectionPool;
        private readonly ObjectPool<CosmosClient> _cosmosClientPool;
        private bool _disposed = false;

        public NoSQLDatabaseAdapters(ILogger<NoSQLDatabaseAdapters> logger = null, IMemoryCache cache = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<NoSQLDatabaseAdapters>.Instance;
            _cache = cache ?? new MemoryCache(new MemoryCacheOptions());
            _adapters = new ConcurrentDictionary<string, object>();
            
            // Initialize connection pools
            _mongoClientPool = new DefaultObjectPool<IMongoClient>(new MongoClientPooledObjectPolicy(), 10);
            _redisConnectionPool = new DefaultObjectPool<IConnectionMultiplexer>(new RedisConnectionPooledObjectPolicy(), 5);
            _cosmosClientPool = new DefaultObjectPool<CosmosClient>(new CosmosClientPooledObjectPolicy(), 3);
        }

        #region MongoDB Adapter - Production Ready

        /// <summary>
        /// Production-ready MongoDB adapter with connection pooling, retry policies, and performance optimization
        /// </summary>
        public class MongoDBAdapter : IDisposable
        {
            private readonly string _connectionString;
            private readonly string _databaseName;
            private IMongoClient _client;
            private IMongoDatabase _database;
            private readonly ILogger _logger;
            private readonly AsyncPolicy _retryPolicy;
            private readonly IMemoryCache _cache;
            private readonly ConcurrentDictionary<string, IMongoCollection<BsonDocument>> _collections;
            private bool _disposed = false;

            public MongoDBAdapter(string connectionString, string databaseName, ILogger logger = null, IMemoryCache cache = null)
            {
                _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
                _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
                _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
                _cache = cache ?? new MemoryCache(new MemoryCacheOptions());
                _collections = new ConcurrentDictionary<string, IMongoCollection<BsonDocument>>();

                // Configure retry policy with exponential backoff
                _retryPolicy = Policy
                    .Handle<MongoException>()
                    .Or<TimeoutException>()
                    .Or<SocketException>()
                    .WaitAndRetryAsync(
                        retryCount: 5,
                        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        onRetry: (outcome, timespan, retryCount, context) =>
                        {
                            _logger.LogWarning($"MongoDB operation failed. Retry {retryCount} in {timespan.TotalMilliseconds}ms. Error: {outcome.Message}");
                        });
            }

            public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
            {
                try
                {
                    if (_client != null)
                        return true;

                    var settings = MongoClientSettings.FromConnectionString(_connectionString);
                    settings.MaxConnectionPoolSize = 100;
                    settings.MinConnectionPoolSize = 10;
                    settings.MaxConnectionIdleTime = TimeSpan.FromMinutes(5);
                    settings.MaxConnectionLifeTime = TimeSpan.FromMinutes(30);
                    settings.ServerSelectionTimeout = TimeSpan.FromSeconds(30);
                    settings.ConnectTimeout = TimeSpan.FromSeconds(10);
                    settings.SocketTimeout = TimeSpan.FromSeconds(30);

                    _client = new MongoClient(settings);
                    _database = _client.GetDatabase(_databaseName);

                    // Test connection
                    await _database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
                    
                    _logger.LogInformation($"Successfully connected to MongoDB database: {_databaseName}");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to connect to MongoDB database: {_databaseName}");
                    return false;
                }
            }

            public async Task<bool> DisconnectAsync()
            {
                try
                {
                    _client?.Dispose();
                    _client = null;
                    _database = null;
                    _collections.Clear();
                    
                    _logger.LogInformation($"Disconnected from MongoDB database: {_databaseName}");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error disconnecting from MongoDB database: {_databaseName}");
                    return false;
                }
            }

            public async Task<List<Dictionary<string, object>>> QueryAsync(string collectionName, FilterDefinition<BsonDocument> filter = null, 
                SortDefinition<BsonDocument> sort = null, int? limit = null, int? skip = null, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var collection = GetCollection(collectionName);
                        var query = collection.Find(filter ?? FilterDefinition<BsonDocument>.Empty);

                        if (sort != null)
                            query = query.Sort(sort);
                        if (skip.HasValue)
                            query = query.Skip(skip.Value);
                        if (limit.HasValue)
                            query = query.Limit(limit.Value);

                        var documents = await query.ToListAsync(cancellationToken);
                        return documents.Select(doc => BsonDocumentToDictionary(doc)).ToList();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"MongoDB query failed for collection: {collectionName}");
                        throw;
                    }
                });
            }

            public async Task<long> CountAsync(string collectionName, FilterDefinition<BsonDocument> filter = null, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var collection = GetCollection(collectionName);
                        return await collection.CountDocumentsAsync(filter ?? FilterDefinition<BsonDocument>.Empty, cancellationToken: cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"MongoDB count failed for collection: {collectionName}");
                        throw;
                    }
                });
            }

            public async Task<string> InsertAsync(string collectionName, Dictionary<string, object> document, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var collection = GetCollection(collectionName);
                        var bsonDoc = DictionaryToBsonDocument(document);
                        
                        await collection.InsertOneAsync(bsonDoc, cancellationToken: cancellationToken);
                        return bsonDoc["_id"].ToString();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"MongoDB insert failed for collection: {collectionName}");
                        throw;
                    }
                });
            }

            public async Task<long> UpdateAsync(string collectionName, FilterDefinition<BsonDocument> filter, 
                UpdateDefinition<BsonDocument> update, bool upsert = false, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var collection = GetCollection(collectionName);
                        var options = new UpdateOptions { IsUpsert = upsert };
                        
                        var result = await collection.UpdateManyAsync(filter, update, options, cancellationToken);
                        return result.ModifiedCount;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"MongoDB update failed for collection: {collectionName}");
                        throw;
                    }
                });
            }

            public async Task<long> DeleteAsync(string collectionName, FilterDefinition<BsonDocument> filter, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var collection = GetCollection(collectionName);
                        var result = await collection.DeleteManyAsync(filter, cancellationToken);
                        return result.DeletedCount;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"MongoDB delete failed for collection: {collectionName}");
                        throw;
                    }
                });
            }

            public async Task<bool> BulkInsertAsync(string collectionName, List<Dictionary<string, object>> documents, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var collection = GetCollection(collectionName);
                        var bsonDocs = documents.Select(doc => DictionaryToBsonDocument(doc)).ToList();
                        
                        await collection.InsertManyAsync(bsonDocs, cancellationToken: cancellationToken);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"MongoDB bulk insert failed for collection: {collectionName}");
                        throw;
                    }
                });
            }

            public async Task<Dictionary<string, object>> GetServerInfoAsync(CancellationToken cancellationToken = default)
            {
                try
                {
                    var result = await _database.RunCommandAsync<BsonDocument>(new BsonDocument("serverStatus", 1), cancellationToken: cancellationToken);
                    return BsonDocumentToDictionary(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get MongoDB server info");
                    throw;
                }
            }

            public async Task<bool> CreateIndexAsync(string collectionName, IndexKeysDefinition<BsonDocument> keys, 
                CreateIndexOptions options = null, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var collection = GetCollection(collectionName);
                        var indexModel = new CreateIndexModel<BsonDocument>(keys, options);
                        await collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to create index for collection: {collectionName}");
                        throw;
                    }
                });
            }

            private IMongoCollection<BsonDocument> GetCollection(string collectionName)
            {
                return _collections.GetOrAdd(collectionName, name => _database.GetCollection<BsonDocument>(name));
            }

            private BsonDocument DictionaryToBsonDocument(Dictionary<string, object> dict)
            {
                var bsonDoc = new BsonDocument();
                foreach (var kvp in dict)
                {
                    bsonDoc.Add(kvp.Key, BsonValue.Create(kvp.Value));
                }
                return bsonDoc;
            }

            private Dictionary<string, object> BsonDocumentToDictionary(BsonDocument doc)
            {
                var dict = new Dictionary<string, object>();
                foreach (var element in doc.Elements)
                {
                    dict[element.Name] = BsonValueToObject(element.Value);
                }
                return dict;
            }

            private object BsonValueToObject(BsonValue value)
            {
                switch (value.BsonType)
                {
                    case BsonType.ObjectId:
                        return value.AsObjectId.ToString();
                    case BsonType.DateTime:
                        return value.AsDateTime;
                    case BsonType.Int32:
                        return value.AsInt32;
                    case BsonType.Int64:
                        return value.AsInt64;
                    case BsonType.Double:
                        return value.AsDouble;
                    case BsonType.String:
                        return value.AsString;
                    case BsonType.Boolean:
                        return value.AsBoolean;
                    case BsonType.Array:
                        return value.AsBsonArray.Select(v => BsonValueToObject(v)).ToList();
                    case BsonType.Document:
                        return BsonDocumentToDictionary(value.AsBsonDocument);
                    default:
                        return value.ToString();
                }
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    DisconnectAsync().Wait();
                    _disposed = true;
                }
            }
        }

        #endregion

        #region Redis Adapter - Production Ready

        /// <summary>
        /// Production-ready Redis adapter with connection pooling, caching, and pub/sub support
        /// </summary>
        public class RedisAdapter : IDisposable
        {
            private readonly string _connectionString;
            private IConnectionMultiplexer _connection;
            private IDatabase _database;
            private ISubscriber _subscriber;
            private readonly ILogger _logger;
            private readonly AsyncPolicy _retryPolicy;
            private readonly IMemoryCache _localCache;
            private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks;
            private bool _disposed = false;

            public RedisAdapter(string connectionString, ILogger logger = null, IMemoryCache localCache = null)
            {
                _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
                _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
                _localCache = localCache ?? new MemoryCache(new MemoryCacheOptions());
                _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

                // Configure retry policy
                _retryPolicy = Policy
                    .Handle<RedisException>()
                    .Or<TimeoutException>()
                    .Or<SocketException>()
                    .WaitAndRetryAsync(
                        retryCount: 3,
                        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        onRetry: (outcome, timespan, retryCount, context) =>
                        {
                            _logger.LogWarning($"Redis operation failed. Retry {retryCount} in {timespan.TotalMilliseconds}ms. Error: {outcome.Message}");
                        });
            }

            public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
            {
                try
                {
                    if (_connection != null && _connection.IsConnected)
                        return true;

                    var options = ConfigurationOptions.Parse(_connectionString);
                    options.ConnectTimeout = 10000;
                    options.SyncTimeout = 10000;
                    options.ResponseTimeout = 10000;
                    options.AbortConnect = false;
                    options.ConnectRetry = 3;

                    _connection = await ConnectionMultiplexer.ConnectAsync(options);
                    _database = _connection.GetDatabase();
                    _subscriber = _connection.GetSubscriber();

                    // Test connection
                    await _database.PingAsync();
                    
                    _logger.LogInformation("Successfully connected to Redis");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to Redis");
                    return false;
                }
            }

            public async Task<bool> DisconnectAsync()
            {
                try
                {
                    _connection?.Close();
                    _connection?.Dispose();
                    _connection = null;
                    _database = null;
                    _subscriber = null;
                    
                    _logger.LogInformation("Disconnected from Redis");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disconnecting from Redis");
                    return false;
                }
            }

            public async Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var result = await _database.StringSetAsync(key, value, expiry);
                        _localCache.Set(key, value, expiry ?? TimeSpan.FromMinutes(5));
                        return result;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Redis SET failed for key: {key}");
                        throw;
                    }
                });
            }

            public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        // Check local cache first
                        if (_localCache.TryGetValue(key, out string cachedValue))
                            return cachedValue;

                        var value = await _database.StringGetAsync(key);
                        if (value.HasValue)
                        {
                            _localCache.Set(key, value.ToString(), TimeSpan.FromMinutes(5));
                            return value.ToString();
                        }
                        return null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Redis GET failed for key: {key}");
                        throw;
                    }
                });
            }

            public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        _localCache.Remove(key);
                        return await _database.KeyDeleteAsync(key);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Redis DELETE failed for key: {key}");
                        throw;
                    }
                });
            }

            public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        return await _database.KeyExistsAsync(key);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Redis EXISTS failed for key: {key}");
                        throw;
                    }
                });
            }

            public async Task<long> IncrementAsync(string key, long value = 1, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var result = await _database.StringIncrementAsync(key, value);
                        _localCache.Remove(key);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Redis INCREMENT failed for key: {key}");
                        throw;
                    }
                });
            }

            public async Task<bool> SetHashAsync(string key, Dictionary<string, string> hash, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var hashEntries = hash.Select(kvp => new HashEntry(kvp.Key, kvp.Value)).ToArray();
                        await _database.HashSetAsync(key, hashEntries);
                        _localCache.Remove(key);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Redis HSET failed for key: {key}");
                        throw;
                    }
                });
            }

            public async Task<Dictionary<string, string>> GetHashAsync(string key, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var hashEntries = await _database.HashGetAllAsync(key);
                        return hashEntries.ToDictionary(entry => entry.Name.ToString(), entry => entry.Value.ToString());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Redis HGETALL failed for key: {key}");
                        throw;
                    }
                });
            }

            public async Task<bool> PublishAsync(string channel, string message, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var subscribers = await _subscriber.PublishAsync(channel, message);
                        return subscribers > 0;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Redis PUBLISH failed for channel: {channel}");
                        throw;
                    }
                });
            }

            public async Task SubscribeAsync(string channel, Action<string, string> messageHandler, CancellationToken cancellationToken = default)
            {
                try
                {
                    await _subscriber.SubscribeAsync(channel, (_, value) =>
                    {
                        messageHandler?.Invoke(channel, value);
                    });
                    
                    _logger.LogInformation($"Subscribed to Redis channel: {channel}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Redis SUBSCRIBE failed for channel: {channel}");
                    throw;
                }
            }

            public async Task<bool> AcquireLockAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var lockKey = $"lock:{key}";
                        var lockValue = Guid.NewGuid().ToString();
                        var result = await _database.StringSetAsync(lockKey, lockValue, expiry, When.NotExists);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Redis lock acquisition failed for key: {key}");
                        throw;
                    }
                });
            }

            public async Task<bool> ReleaseLockAsync(string key, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var lockKey = $"lock:{key}";
                        return await _database.KeyDeleteAsync(lockKey);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Redis lock release failed for key: {key}");
                        throw;
                    }
                });
            }

            public async Task<Dictionary<string, object>> GetServerInfoAsync(CancellationToken cancellationToken = default)
            {
                try
                {
                    var info = await _database.ExecuteAsync("INFO");
                    var infoString = info.ToString();
                    var lines = infoString.Split('\n');
                    var result = new Dictionary<string, object>();

                    foreach (var line in lines)
                    {
                        if (line.Contains(':'))
                        {
                            var parts = line.Split(':', 2);
                            if (parts.Length == 2)
                            {
                                result[parts[0].Trim()] = parts[1].Trim();
                            }
                        }
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get Redis server info");
                    throw;
                }
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    DisconnectAsync().Wait();
                    _disposed = true;
                }
            }
        }

        #endregion

        #region Cosmos DB Adapter - Production Ready

        /// <summary>
        /// Production-ready Cosmos DB adapter with multi-region support and advanced features
        /// </summary>
        public class CosmosDBAdapter : IDisposable
        {
            private readonly string _connectionString;
            private readonly string _databaseName;
            private CosmosClient _client;
            private Database _database;
            private readonly ILogger _logger;
            private readonly AsyncPolicy _retryPolicy;
            private readonly ConcurrentDictionary<string, Container> _containers;
            private bool _disposed = false;

            public CosmosDBAdapter(string connectionString, string databaseName, ILogger logger = null)
            {
                _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
                _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
                _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
                _containers = new ConcurrentDictionary<string, Container>();

                // Configure retry policy
                _retryPolicy = Policy
                    .Handle<CosmosException>()
                    .Or<TimeoutException>()
                    .WaitAndRetryAsync(
                        retryCount: 3,
                        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        onRetry: (outcome, timespan, retryCount, context) =>
                        {
                            _logger.LogWarning($"Cosmos DB operation failed. Retry {retryCount} in {timespan.TotalMilliseconds}ms. Error: {outcome.Message}");
                        });
            }

            public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
            {
                try
                {
                    if (_client != null)
                        return true;

                    var options = new CosmosClientOptions
                    {
                        ConnectionMode = ConnectionMode.Direct,
                        MaxRequestsPerTcpConnection = 20,
                        MaxTcpConnectionsPerEndpoint = 32,
                        RequestTimeout = TimeSpan.FromSeconds(30),
                        EnableTcpConnectionEndpointRediscovery = true
                    };

                    _client = new CosmosClient(_connectionString, options);
                    _database = await _client.CreateDatabaseIfNotExistsAsync(_databaseName, cancellationToken: cancellationToken);
                    
                    _logger.LogInformation($"Successfully connected to Cosmos DB database: {_databaseName}");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to connect to Cosmos DB database: {_databaseName}");
                    return false;
                }
            }

            public async Task<bool> DisconnectAsync()
            {
                try
                {
                    _client?.Dispose();
                    _client = null;
                    _database = null;
                    _containers.Clear();
                    
                    _logger.LogInformation($"Disconnected from Cosmos DB database: {_databaseName}");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error disconnecting from Cosmos DB database: {_databaseName}");
                    return false;
                }
            }

            public async Task<List<Dictionary<string, object>>> QueryAsync(string containerName, string query, 
                Dictionary<string, object> parameters = null, int? maxItemCount = null, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var container = GetContainer(containerName);
                        var queryDefinition = new QueryDefinition(query);

                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                queryDefinition = queryDefinition.WithParameter(param.Key, param.Value);
                            }
                        }

                        var queryOptions = new QueryRequestOptions();
                        if (maxItemCount.HasValue)
                            queryOptions.MaxItemCount = maxItemCount.Value;

                        var results = new List<Dictionary<string, object>>();
                        using var iterator = container.GetItemQueryIterator<dynamic>(queryDefinition, requestOptions: queryOptions);

                        while (iterator.HasMoreResults)
                        {
                            var response = await iterator.ReadNextAsync(cancellationToken);
                            foreach (var item in response)
                            {
                                results.Add(DynamicToDictionary(item));
                            }
                        }

                        return results;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Cosmos DB query failed for container: {containerName}");
                        throw;
                    }
                });
            }

            public async Task<Dictionary<string, object>> GetAsync(string containerName, string id, string partitionKey = null, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var container = GetContainer(containerName);
                        var partitionKeyValue = partitionKey ?? id;
                        
                        var response = await container.ReadItemAsync<dynamic>(id, new PartitionKey(partitionKeyValue), cancellationToken: cancellationToken);
                        return DynamicToDictionary(response.Resource);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Cosmos DB GET failed for container: {containerName}, id: {id}");
                        throw;
                    }
                });
            }

            public async Task<Dictionary<string, object>> CreateAsync(string containerName, Dictionary<string, object> item, 
                string partitionKey = null, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var container = GetContainer(containerName);
                        var partitionKeyValue = partitionKey ?? item.GetValueOrDefault("id", Guid.NewGuid().ToString()).ToString();
                        
                        var response = await container.CreateItemAsync(item, new PartitionKey(partitionKeyValue), cancellationToken: cancellationToken);
                        return DynamicToDictionary(response.Resource);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Cosmos DB CREATE failed for container: {containerName}");
                        throw;
                    }
                });
            }

            public async Task<Dictionary<string, object>> UpdateAsync(string containerName, string id, Dictionary<string, object> item, 
                string partitionKey = null, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var container = GetContainer(containerName);
                        var partitionKeyValue = partitionKey ?? id;
                        
                        var response = await container.ReplaceItemAsync(item, id, new PartitionKey(partitionKeyValue), cancellationToken: cancellationToken);
                        return DynamicToDictionary(response.Resource);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Cosmos DB UPDATE failed for container: {containerName}, id: {id}");
                        throw;
                    }
                });
            }

            public async Task<bool> DeleteAsync(string containerName, string id, string partitionKey = null, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var container = GetContainer(containerName);
                        var partitionKeyValue = partitionKey ?? id;
                        
                        await container.DeleteItemAsync<dynamic>(id, new PartitionKey(partitionKeyValue), cancellationToken: cancellationToken);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Cosmos DB DELETE failed for container: {containerName}, id: {id}");
                        throw;
                    }
                });
            }

            public async Task<bool> CreateContainerAsync(string containerName, string partitionKeyPath, 
                int throughput = 400, CancellationToken cancellationToken = default)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var containerProperties = new ContainerProperties(containerName, partitionKeyPath)
                        {
                            DefaultTimeToLive = -1 // No TTL
                        };

                        var throughputProperties = ThroughputProperties.CreateManualThroughput(throughput);
                        await _database.CreateContainerAsync(containerProperties, throughputProperties, cancellationToken: cancellationToken);
                        
                        _logger.LogInformation($"Created Cosmos DB container: {containerName}");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to create Cosmos DB container: {containerName}");
                        throw;
                    }
                });
            }

            public async Task<Dictionary<string, object>> GetDatabaseInfoAsync(CancellationToken cancellationToken = default)
            {
                try
                {
                    var properties = await _database.ReadAsync(cancellationToken: cancellationToken);
                    return new Dictionary<string, object>
                    {
                        ["id"] = properties.Database.Id,
                        ["etag"] = properties.ETag,
                        ["lastModified"] = properties.LastModified,
                        ["resourceId"] = properties.Database.ResourceId
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get Cosmos DB database info");
                    throw;
                }
            }

            private Container GetContainer(string containerName)
            {
                return _containers.GetOrAdd(containerName, name => _database.GetContainer(name));
            }

            private Dictionary<string, object> DynamicToDictionary(dynamic obj)
            {
                var dict = new Dictionary<string, object>();
                var json = JsonSerializer.Serialize(obj);
                var jsonDoc = JsonDocument.Parse(json);
                
                foreach (var property in jsonDoc.RootElement.EnumerateObject())
                {
                    dict[property.Name] = JsonElementToObject(property.Value);
                }
                
                return dict;
            }

            private object JsonElementToObject(JsonElement element)
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.String:
                        return element.GetString();
                    case JsonValueKind.Number:
                        if (element.TryGetInt32(out int intValue))
                            return intValue;
                        if (element.TryGetInt64(out long longValue))
                            return longValue;
                        return element.GetDouble();
                    case JsonValueKind.True:
                        return true;
                    case JsonValueKind.False:
                        return false;
                    case JsonValueKind.Null:
                        return null;
                    case JsonValueKind.Array:
                        return element.EnumerateArray().Select(JsonElementToObject).ToList();
                    case JsonValueKind.Object:
                        var obj = new Dictionary<string, object>();
                        foreach (var property in element.EnumerateObject())
                        {
                            obj[property.Name] = JsonElementToObject(property.Value);
                        }
                        return obj;
                    default:
                        return element.ToString();
                }
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    DisconnectAsync().Wait();
                    _disposed = true;
                }
            }
        }

        #endregion

        #region Connection Pool Policies

        private class MongoClientPooledObjectPolicy : IPooledObjectPolicy<IMongoClient>
        {
            public IMongoClient Create()
            {
                var settings = MongoClientSettings.FromConnectionString("mongodb://localhost:27017");
                settings.MaxConnectionPoolSize = 100;
                settings.MinConnectionPoolSize = 10;
                return new MongoClient(settings);
            }

            public bool Return(IMongoClient obj)
            {
                return true;
            }
        }

        private class RedisConnectionPooledObjectPolicy : IPooledObjectPolicy<IConnectionMultiplexer>
        {
            public IConnectionMultiplexer Create()
            {
                var options = ConfigurationOptions.Parse("localhost:6379");
                return ConnectionMultiplexer.Connect(options);
            }

            public bool Return(IConnectionMultiplexer obj)
            {
                return obj.IsConnected;
            }
        }

        private class CosmosClientPooledObjectPolicy : IPooledObjectPolicy<CosmosClient>
        {
            public CosmosClient Create()
            {
                return new CosmosClient("https://localhost:8081", "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
            }

            public bool Return(CosmosClient obj)
            {
                return true;
            }
        }

        #endregion

        #region Factory Methods

        public async Task<MongoDBAdapter> CreateMongoDBAdapterAsync(string connectionString, string databaseName, ILogger logger = null)
        {
            var adapter = new MongoDBAdapter(connectionString, databaseName, logger, _cache);
            await adapter.ConnectAsync();
            _adapters.TryAdd($"mongodb_{databaseName}", adapter);
            return adapter;
        }

        public async Task<RedisAdapter> CreateRedisAdapterAsync(string connectionString, ILogger logger = null)
        {
            var adapter = new RedisAdapter(connectionString, logger, _cache);
            await adapter.ConnectAsync();
            _adapters.TryAdd("redis", adapter);
            return adapter;
        }

        public async Task<CosmosDBAdapter> CreateCosmosDBAdapterAsync(string connectionString, string databaseName, ILogger logger = null)
        {
            var adapter = new CosmosDBAdapter(connectionString, databaseName, logger);
            await adapter.ConnectAsync();
            _adapters.TryAdd($"cosmosdb_{databaseName}", adapter);
            return adapter;
        }

        public async Task<Dictionary<string, bool>> HealthCheckAllAsync()
        {
            var results = new Dictionary<string, bool>();

            foreach (var adapter in _adapters)
            {
                try
                {
                    switch (adapter.Value)
                    {
                        case MongoDBAdapter mongoAdapter:
                            var mongoInfo = await mongoAdapter.GetServerInfoAsync();
                            results[adapter.Key] = mongoInfo != null;
                            break;
                        case RedisAdapter redisAdapter:
                            var redisInfo = await redisAdapter.GetServerInfoAsync();
                            results[adapter.Key] = redisInfo != null;
                            break;
                        case CosmosDBAdapter cosmosAdapter:
                            var cosmosInfo = await cosmosAdapter.GetDatabaseInfoAsync();
                            results[adapter.Key] = cosmosInfo != null;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Health check failed for adapter: {adapter.Key}");
                    results[adapter.Key] = false;
                }
            }

            return results;
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var adapter in _adapters.Values)
                {
                    if (adapter is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _adapters.Clear();
                _disposed = true;
            }
        }
    }
} 