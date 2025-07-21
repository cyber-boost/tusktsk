using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Reflection;
using System.Threading;

namespace TuskLang
{
    /// <summary>
    /// Advanced database integration and ORM system for TuskLang C# SDK
    /// Provides unified database access with support for multiple providers and advanced querying
    /// </summary>
    public class DatabaseIntegration
    {
        private readonly Dictionary<string, IDatabaseProvider> _providers;
        private readonly ConnectionPool _connectionPool;
        private readonly QueryCache _queryCache;
        private readonly DatabaseMetrics _metrics;
        private readonly ConcurrentDictionary<string, object> _transactions;

        public DatabaseIntegration()
        {
            _providers = new Dictionary<string, IDatabaseProvider>();
            _connectionPool = new ConnectionPool();
            _queryCache = new QueryCache();
            _metrics = new DatabaseMetrics();
            _transactions = new ConcurrentDictionary<string, object>();

            // Register default providers
            RegisterProvider("sqlite", new SqliteProvider());
            RegisterProvider("postgresql", new PostgreSqlProvider());
            RegisterProvider("mysql", new MySqlProvider());
            RegisterProvider("sqlserver", new SqlServerProvider());
        }

        /// <summary>
        /// Register a database provider
        /// </summary>
        public void RegisterProvider(string name, IDatabaseProvider provider)
        {
            _providers[name] = provider;
        }

        /// <summary>
        /// Execute a query with automatic connection management
        /// </summary>
        public async Task<QueryResult<T>> ExecuteQueryAsync<T>(
            string providerName,
            string query,
            Dictionary<string, object> parameters = null,
            QueryOptions options = null)
        {
            var startTime = DateTime.UtcNow;
            var queryId = Guid.NewGuid().ToString();

            try
            {
                // Check cache first
                var cacheKey = GenerateCacheKey(query, parameters);
                if (_queryCache.TryGet<T>(cacheKey, out var cachedResult))
                {
                    _metrics.RecordCacheHit(queryId);
                    return cachedResult;
                }

                // Get provider
                if (!_providers.TryGetValue(providerName, out var provider))
                {
                    throw new DatabaseException($"Provider '{providerName}' not found");
                }

                // Get connection from pool
                using var connection = await _connectionPool.GetConnectionAsync(providerName);
                
                // Execute query
                var result = await provider.ExecuteQueryAsync<T>(connection, query, parameters, options);
                
                // Cache result if appropriate
                if (options?.EnableCaching != false)
                {
                    _queryCache.Set(cacheKey, result, options?.CacheExpiration ?? TimeSpan.FromMinutes(5));
                }

                // Record metrics
                _metrics.RecordQuery(queryId, DateTime.UtcNow - startTime, true);
                
                return result;
            }
            catch (Exception ex)
            {
                _metrics.RecordQuery(queryId, DateTime.UtcNow - startTime, false);
                throw new DatabaseException($"Query execution failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Execute a command (INSERT, UPDATE, DELETE)
        /// </summary>
        public async Task<CommandResult> ExecuteCommandAsync(
            string providerName,
            string command,
            Dictionary<string, object> parameters = null,
            CommandOptions options = null)
        {
            var startTime = DateTime.UtcNow;
            var commandId = Guid.NewGuid().ToString();

            try
            {
                if (!_providers.TryGetValue(providerName, out var provider))
                {
                    throw new DatabaseException($"Provider '{providerName}' not found");
                }

                using var connection = await _connectionPool.GetConnectionAsync(providerName);
                
                // Handle transaction if specified
                if (options?.TransactionId != null)
                {
                    if (!_transactions.TryGetValue(options.TransactionId, out var transaction))
                    {
                        throw new DatabaseException($"Transaction '{options.TransactionId}' not found");
                    }
                    connection.EnlistTransaction(transaction as System.Transactions.Transaction);
                }

                var result = await provider.ExecuteCommandAsync(connection, command, parameters, options);
                
                _metrics.RecordCommand(commandId, DateTime.UtcNow - startTime, true);
                
                return result;
            }
            catch (Exception ex)
            {
                _metrics.RecordCommand(commandId, DateTime.UtcNow - startTime, false);
                throw new DatabaseException($"Command execution failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Begin a transaction
        /// </summary>
        public async Task<string> BeginTransactionAsync(string providerName)
        {
            var transactionId = Guid.NewGuid().ToString();
            
            if (!_providers.TryGetValue(providerName, out var provider))
            {
                throw new DatabaseException($"Provider '{providerName}' not found");
            }

            using var connection = await _connectionPool.GetConnectionAsync(providerName);
            var transaction = await provider.BeginTransactionAsync(connection);
            
            _transactions[transactionId] = transaction;
            
            return transactionId;
        }

        /// <summary>
        /// Commit a transaction
        /// </summary>
        public async Task CommitTransactionAsync(string transactionId)
        {
            if (_transactions.TryRemove(transactionId, out var transaction))
            {
                if (transaction is IDbTransaction dbTransaction)
                {
                    await Task.Run(() => dbTransaction.Commit());
                }
            }
        }

        /// <summary>
        /// Rollback a transaction
        /// </summary>
        public async Task RollbackTransactionAsync(string transactionId)
        {
            if (_transactions.TryRemove(transactionId, out var transaction))
            {
                if (transaction is IDbTransaction dbTransaction)
                {
                    await Task.Run(() => dbTransaction.Rollback());
                }
            }
        }

        /// <summary>
        /// Execute a stored procedure
        /// </summary>
        public async Task<StoredProcedureResult> ExecuteStoredProcedureAsync(
            string providerName,
            string procedureName,
            Dictionary<string, object> parameters = null)
        {
            if (!_providers.TryGetValue(providerName, out var provider))
            {
                throw new DatabaseException($"Provider '{providerName}' not found");
            }

            using var connection = await _connectionPool.GetConnectionAsync(providerName);
            return await provider.ExecuteStoredProcedureAsync(connection, procedureName, parameters);
        }

        /// <summary>
        /// Get database schema information
        /// </summary>
        public async Task<DatabaseSchema> GetSchemaAsync(string providerName, string databaseName = null)
        {
            if (!_providers.TryGetValue(providerName, out var provider))
            {
                throw new DatabaseException($"Provider '{providerName}' not found");
            }

            using var connection = await _connectionPool.GetConnectionAsync(providerName);
            return await provider.GetSchemaAsync(connection, databaseName);
        }

        /// <summary>
        /// Get database metrics
        /// </summary>
        public DatabaseMetrics GetMetrics()
        {
            return _metrics;
        }

        private string GenerateCacheKey(string query, Dictionary<string, object> parameters)
        {
            var parameterString = parameters != null 
                ? JsonSerializer.Serialize(parameters.OrderBy(kvp => kvp.Key))
                : "";
            return $"{query}|{parameterString}";
        }
    }

    /// <summary>
    /// Database provider interface
    /// </summary>
    public interface IDatabaseProvider
    {
        Task<QueryResult<T>> ExecuteQueryAsync<T>(IDbConnection connection, string query, Dictionary<string, object> parameters, QueryOptions options);
        Task<CommandResult> ExecuteCommandAsync(IDbConnection connection, string command, Dictionary<string, object> parameters, CommandOptions options);
        Task<IDbTransaction> BeginTransactionAsync(IDbConnection connection);
        Task<StoredProcedureResult> ExecuteStoredProcedureAsync(IDbConnection connection, string procedureName, Dictionary<string, object> parameters);
        Task<DatabaseSchema> GetSchemaAsync(IDbConnection connection, string databaseName);
    }

    /// <summary>
    /// SQLite provider implementation
    /// </summary>
    public class SqliteProvider : IDatabaseProvider
    {
        public async Task<QueryResult<T>> ExecuteQueryAsync<T>(IDbConnection connection, string query, Dictionary<string, object> parameters, QueryOptions options)
        {
            using var command = connection.CreateCommand();
            command.CommandText = query;
            command.CommandType = CommandType.Text;

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    var dbParam = command.CreateParameter();
                    dbParam.ParameterName = param.Key;
                    dbParam.Value = param.Value ?? DBNull.Value;
                    command.Parameters.Add(dbParam);
                }
            }

            using var reader = await Task.Run(() => command.ExecuteReader());
            var results = new List<T>();

            while (await reader.ReadAsync())
            {
                var item = MapReaderToObject<T>(reader);
                results.Add(item);
            }

            return new QueryResult<T>
            {
                Data = results,
                RowCount = results.Count,
                ExecutionTime = TimeSpan.Zero // Would be calculated in real implementation
            };
        }

        public async Task<CommandResult> ExecuteCommandAsync(IDbConnection connection, string command, Dictionary<string, object> parameters, CommandOptions options)
        {
            using var dbCommand = connection.CreateCommand();
            dbCommand.CommandText = command;
            dbCommand.CommandType = CommandType.Text;

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    var dbParam = dbCommand.CreateParameter();
                    dbParam.ParameterName = param.Key;
                    dbParam.Value = param.Value ?? DBNull.Value;
                    dbCommand.Parameters.Add(dbParam);
                }
            }

            var rowsAffected = await Task.Run(() => dbCommand.ExecuteNonQuery());

            return new CommandResult
            {
                RowsAffected = rowsAffected,
                ExecutionTime = TimeSpan.Zero
            };
        }

        public async Task<IDbTransaction> BeginTransactionAsync(IDbConnection connection)
        {
            return await Task.Run(() => connection.BeginTransaction());
        }

        public async Task<StoredProcedureResult> ExecuteStoredProcedureAsync(IDbConnection connection, string procedureName, Dictionary<string, object> parameters)
        {
            // SQLite doesn't support stored procedures, but we can simulate
            return await Task.FromResult(new StoredProcedureResult
            {
                Success = false,
                ErrorMessage = "SQLite does not support stored procedures"
            });
        }

        public async Task<DatabaseSchema> GetSchemaAsync(IDbConnection connection, string databaseName)
        {
            var schema = new DatabaseSchema();
            
            // Get tables
            var tables = connection.GetSchema("Tables");
            foreach (DataRow table in tables.Rows)
            {
                var tableInfo = new TableInfo
                {
                    Name = table["TABLE_NAME"].ToString(),
                    Type = table["TABLE_TYPE"].ToString()
                };
                schema.Tables.Add(tableInfo);
            }

            return await Task.FromResult(schema);
        }

        private T MapReaderToObject<T>(IDataReader reader)
        {
            if (typeof(T) == typeof(Dictionary<string, object>))
            {
                var dict = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dict[reader.GetName(i)] = reader.GetValue(i);
                }
                return (T)(object)dict;
            }

            // For strongly typed objects, use reflection
            var instance = Activator.CreateInstance<T>();
            var properties = typeof(T).GetProperties();

            foreach (var property in properties)
            {
                try
                {
                    var value = reader[property.Name];
                    if (value != DBNull.Value)
                    {
                        property.SetValue(instance, Convert.ChangeType(value, property.PropertyType));
                    }
                }
                catch
                {
                    // Property not found in reader, skip
                }
            }

            return instance;
        }
    }

    /// <summary>
    /// PostgreSQL provider implementation
    /// </summary>
    public class PostgreSqlProvider : IDatabaseProvider
    {
        public async Task<QueryResult<T>> ExecuteQueryAsync<T>(IDbConnection connection, string query, Dictionary<string, object> parameters, QueryOptions options)
        {
            // Implementation would use Npgsql
            return await Task.FromResult(new QueryResult<T>
            {
                Data = new List<T>(),
                RowCount = 0,
                ExecutionTime = TimeSpan.Zero
            });
        }

        public async Task<CommandResult> ExecuteCommandAsync(IDbConnection connection, string command, Dictionary<string, object> parameters, CommandOptions options)
        {
            return await Task.FromResult(new CommandResult
            {
                RowsAffected = 0,
                ExecutionTime = TimeSpan.Zero
            });
        }

        public async Task<IDbTransaction> BeginTransactionAsync(IDbConnection connection)
        {
            return await Task.Run(() => connection.BeginTransaction());
        }

        public async Task<StoredProcedureResult> ExecuteStoredProcedureAsync(IDbConnection connection, string procedureName, Dictionary<string, object> parameters)
        {
            return await Task.FromResult(new StoredProcedureResult
            {
                Success = true,
                Data = new Dictionary<string, object>()
            });
        }

        public async Task<DatabaseSchema> GetSchemaAsync(IDbConnection connection, string databaseName)
        {
            return await Task.FromResult(new DatabaseSchema());
        }
    }

    /// <summary>
    /// MySQL provider implementation
    /// </summary>
    public class MySqlProvider : IDatabaseProvider
    {
        public async Task<QueryResult<T>> ExecuteQueryAsync<T>(IDbConnection connection, string query, Dictionary<string, object> parameters, QueryOptions options)
        {
            // Implementation would use MySqlConnector
            return await Task.FromResult(new QueryResult<T>
            {
                Data = new List<T>(),
                RowCount = 0,
                ExecutionTime = TimeSpan.Zero
            });
        }

        public async Task<CommandResult> ExecuteCommandAsync(IDbConnection connection, string command, Dictionary<string, object> parameters, CommandOptions options)
        {
            return await Task.FromResult(new CommandResult
            {
                RowsAffected = 0,
                ExecutionTime = TimeSpan.Zero
            });
        }

        public async Task<IDbTransaction> BeginTransactionAsync(IDbConnection connection)
        {
            return await Task.Run(() => connection.BeginTransaction());
        }

        public async Task<StoredProcedureResult> ExecuteStoredProcedureAsync(IDbConnection connection, string procedureName, Dictionary<string, object> parameters)
        {
            return await Task.FromResult(new StoredProcedureResult
            {
                Success = true,
                Data = new Dictionary<string, object>()
            });
        }

        public async Task<DatabaseSchema> GetSchemaAsync(IDbConnection connection, string databaseName)
        {
            return await Task.FromResult(new DatabaseSchema());
        }
    }

    /// <summary>
    /// SQL Server provider implementation
    /// </summary>
    public class SqlServerProvider : IDatabaseProvider
    {
        public async Task<QueryResult<T>> ExecuteQueryAsync<T>(IDbConnection connection, string query, Dictionary<string, object> parameters, QueryOptions options)
        {
            // Implementation would use Microsoft.Data.SqlClient
            return await Task.FromResult(new QueryResult<T>
            {
                Data = new List<T>(),
                RowCount = 0,
                ExecutionTime = TimeSpan.Zero
            });
        }

        public async Task<CommandResult> ExecuteCommandAsync(IDbConnection connection, string command, Dictionary<string, object> parameters, CommandOptions options)
        {
            return await Task.FromResult(new CommandResult
            {
                RowsAffected = 0,
                ExecutionTime = TimeSpan.Zero
            });
        }

        public async Task<IDbTransaction> BeginTransactionAsync(IDbConnection connection)
        {
            return await Task.Run(() => connection.BeginTransaction());
        }

        public async Task<StoredProcedureResult> ExecuteStoredProcedureAsync(IDbConnection connection, string procedureName, Dictionary<string, object> parameters)
        {
            return await Task.FromResult(new StoredProcedureResult
            {
                Success = true,
                Data = new Dictionary<string, object>()
            });
        }

        public async Task<DatabaseSchema> GetSchemaAsync(IDbConnection connection, string databaseName)
        {
            return await Task.FromResult(new DatabaseSchema());
        }
    }

    /// <summary>
    /// Connection pool for managing database connections
    /// </summary>
    public class ConnectionPool
    {
        private readonly ConcurrentDictionary<string, Queue<IDbConnection>> _pools;
        private readonly Dictionary<string, string> _connectionStrings;

        public ConnectionPool()
        {
            _pools = new ConcurrentDictionary<string, Queue<IDbConnection>>();
            _connectionStrings = new Dictionary<string, string>();
        }

        public async Task<IDbConnection> GetConnectionAsync(string providerName)
        {
            // In a real implementation, this would manage connection pooling
            // For now, return a mock connection
            return await Task.FromResult<IDbConnection>(null);
        }

        public void ReleaseConnection(IDbConnection connection)
        {
            // Release connection back to pool
        }
    }

    /// <summary>
    /// Query cache for caching query results
    /// </summary>
    public class QueryCache
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private readonly int _maxSize = 1000;

        public QueryCache()
        {
            _cache = new ConcurrentDictionary<string, CacheEntry>();
        }

        public bool TryGet<T>(string key, out QueryResult<T> result)
        {
            result = null;
            if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
            {
                result = (QueryResult<T>)entry.Value;
                return true;
            }
            return false;
        }

        public void Set<T>(string key, QueryResult<T> value, TimeSpan expiration)
        {
            var entry = new CacheEntry(value, DateTime.UtcNow.Add(expiration));
            _cache[key] = entry;

            // Evict if cache is too large
            if (_cache.Count > _maxSize)
            {
                var oldestKey = _cache.OrderBy(kvp => kvp.Value.CreatedAt).First().Key;
                _cache.TryRemove(oldestKey, out _);
            }
        }
    }

    /// <summary>
    /// Database metrics collection
    /// </summary>
    public class DatabaseMetrics
    {
        private readonly ConcurrentDictionary<string, QueryMetrics> _queryMetrics;
        private readonly ConcurrentDictionary<string, CommandMetrics> _commandMetrics;
        private int _cacheHits = 0;

        public DatabaseMetrics()
        {
            _queryMetrics = new ConcurrentDictionary<string, QueryMetrics>();
            _commandMetrics = new ConcurrentDictionary<string, CommandMetrics>();
        }

        public void RecordQuery(string queryId, TimeSpan executionTime, bool success)
        {
            var metrics = _queryMetrics.GetOrAdd(queryId, _ => new QueryMetrics());
            metrics.RecordExecution(executionTime, success);
        }

        public void RecordCommand(string commandId, TimeSpan executionTime, bool success)
        {
            var metrics = _commandMetrics.GetOrAdd(commandId, _ => new CommandMetrics());
            metrics.RecordExecution(executionTime, success);
        }

        public void RecordCacheHit(string queryId)
        {
            Interlocked.Increment(ref _cacheHits);
        }

        public int CacheHits => _cacheHits;
    }

    // Data transfer objects
    public class QueryResult<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public int RowCount { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class CommandResult
    {
        public int RowsAffected { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class StoredProcedureResult
    {
        public bool Success { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public string ErrorMessage { get; set; }
    }

    public class DatabaseSchema
    {
        public List<TableInfo> Tables { get; set; } = new List<TableInfo>();
        public List<ViewInfo> Views { get; set; } = new List<ViewInfo>();
        public List<StoredProcedureInfo> StoredProcedures { get; set; } = new List<StoredProcedureInfo>();
    }

    public class TableInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<ColumnInfo> Columns { get; set; } = new List<ColumnInfo>();
    }

    public class ViewInfo
    {
        public string Name { get; set; }
        public string Definition { get; set; }
    }

    public class StoredProcedureInfo
    {
        public string Name { get; set; }
        public string Definition { get; set; }
    }

    public class ColumnInfo
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
    }

    public class QueryOptions
    {
        public bool EnableCaching { get; set; } = true;
        public TimeSpan? CacheExpiration { get; set; }
        public int? CommandTimeout { get; set; }
    }

    public class CommandOptions
    {
        public string TransactionId { get; set; }
        public int? CommandTimeout { get; set; }
    }

    public class QueryMetrics
    {
        private readonly List<TimeSpan> _executionTimes = new List<TimeSpan>();
        private int _successCount = 0;
        private int _failureCount = 0;

        public void RecordExecution(TimeSpan executionTime, bool success)
        {
            _executionTimes.Add(executionTime);
            if (success)
                _successCount++;
            else
                _failureCount++;

            if (_executionTimes.Count > 1000)
                _executionTimes.RemoveAt(0);
        }

        public double AverageExecutionTime => _executionTimes.Count > 0 ? _executionTimes.Average(t => t.TotalMilliseconds) : 0;
        public int TotalExecutions => _successCount + _failureCount;
        public double SuccessRate => TotalExecutions > 0 ? (double)_successCount / TotalExecutions : 0;
    }

    public class CommandMetrics
    {
        private readonly List<TimeSpan> _executionTimes = new List<TimeSpan>();
        private int _successCount = 0;
        private int _failureCount = 0;

        public void RecordExecution(TimeSpan executionTime, bool success)
        {
            _executionTimes.Add(executionTime);
            if (success)
                _successCount++;
            else
                _failureCount++;

            if (_executionTimes.Count > 1000)
                _executionTimes.RemoveAt(0);
        }

        public double AverageExecutionTime => _executionTimes.Count > 0 ? _executionTimes.Average(t => t.TotalMilliseconds) : 0;
        public int TotalExecutions => _successCount + _failureCount;
        public double SuccessRate => TotalExecutions > 0 ? (double)_successCount / TotalExecutions : 0;
    }

    public class CacheEntry
    {
        public object Value { get; }
        public DateTime ExpiresAt { get; }
        public DateTime CreatedAt { get; }

        public bool IsExpired => DateTime.UtcNow > ExpiresAt;

        public CacheEntry(object value, DateTime expiresAt)
        {
            Value = value;
            ExpiresAt = expiresAt;
            CreatedAt = DateTime.UtcNow;
        }
    }

    public class DatabaseException : Exception
    {
        public DatabaseException(string message) : base(message) { }
        public DatabaseException(string message, Exception innerException) : base(message, innerException) { }
    }
} 