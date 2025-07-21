using Dapper;
using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;
using System.Data.SQLite;
using System.Transactions;
using System.Text;
using System.Dynamic;
using Microsoft.Extensions.Caching.Memory;

namespace TuskLang.Database
{
    /// <summary>
    /// High-performance Dapper adapter for TuskTsk
    /// Provides micro-ORM functionality with maximum performance
    /// </summary>
    public class DapperAdapter : IDisposable
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<DapperAdapter> _logger;
        private readonly IMemoryCache _cache;
        private readonly string _connectionString;
        private readonly DatabaseType _databaseType;
        private bool _disposed = false;

        public enum DatabaseType
        {
            SqlServer,
            PostgreSQL,
            MySQL,
            SQLite
        }

        public DapperAdapter(string connectionString, DatabaseType databaseType, 
            ILogger<DapperAdapter> logger = null, IMemoryCache cache = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _databaseType = databaseType;
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DapperAdapter>.Instance;
            _cache = cache ?? new MemoryCache(new MemoryCacheOptions());

            _connection = CreateConnection(connectionString, databaseType);
            
            // Configure Dapper settings
            SqlMapper.Settings.CommandTimeout = 30;
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        private IDbConnection CreateConnection(string connectionString, DatabaseType databaseType)
        {
            return databaseType switch
            {
                DatabaseType.SqlServer => new SqlConnection(connectionString),
                DatabaseType.PostgreSQL => new NpgsqlConnection(connectionString),
                DatabaseType.MySQL => new MySqlConnection(connectionString),
                DatabaseType.SQLite => new SQLiteConnection(connectionString),
                _ => throw new ArgumentException($"Unsupported database type: {databaseType}")
            };
        }

        #region Connection Management

        /// <summary>
        /// Open database connection
        /// </summary>
        public async Task OpenAsync()
        {
            try
            {
                if (_connection.State != ConnectionState.Open)
                {
                    await _connection.OpenAsync();
                    _logger.LogInformation($"Opened {_databaseType} connection");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to open {_databaseType} connection");
                throw;
            }
        }

        /// <summary>
        /// Close database connection
        /// </summary>
        public async Task CloseAsync()
        {
            try
            {
                if (_connection.State == ConnectionState.Open)
                {
                    await _connection.CloseAsync();
                    _logger.LogInformation($"Closed {_databaseType} connection");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error closing {_databaseType} connection");
                throw;
            }
        }

        /// <summary>
        /// Test connection health
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                await OpenAsync();
                var result = await _connection.QuerySingleAsync<int>("SELECT 1");
                return result == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection health check failed");
                return false;
            }
        }

        #endregion

        #region Query Operations

        /// <summary>
        /// Execute query and return strongly typed results
        /// </summary>
        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, 
            IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            try
            {
                await OpenAsync();
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                var results = await _connection.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType);
                
                stopwatch.Stop();
                _logger.LogDebug($"Query executed in {stopwatch.ElapsedMilliseconds}ms, returned {results.Count()} rows");
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing query: {sql}");
                throw;
            }
        }

        /// <summary>
        /// Execute query with caching support
        /// </summary>
        public async Task<IEnumerable<T>> QueryCachedAsync<T>(string sql, object param = null, 
            TimeSpan? cacheExpiration = null, string cacheKey = null)
        {
            cacheExpiration ??= TimeSpan.FromMinutes(5);
            cacheKey ??= GenerateCacheKey(sql, param);

            if (_cache.TryGetValue(cacheKey, out IEnumerable<T> cachedResult))
            {
                _logger.LogDebug($"Cache hit for key: {cacheKey}");
                return cachedResult;
            }

            var result = await QueryAsync<T>(sql, param);
            _cache.Set(cacheKey, result, cacheExpiration.Value);
            
            _logger.LogDebug($"Cached query result with key: {cacheKey}");
            return result;
        }

        /// <summary>
        /// Execute query and return single result
        /// </summary>
        public async Task<T> QuerySingleAsync<T>(string sql, object param = null, 
            IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            try
            {
                await OpenAsync();
                var result = await _connection.QuerySingleAsync<T>(sql, param, transaction, commandTimeout, commandType);
                
                _logger.LogDebug($"Single query executed successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing single query: {sql}");
                throw;
            }
        }

        /// <summary>
        /// Execute query and return single result or default
        /// </summary>
        public async Task<T> QuerySingleOrDefaultAsync<T>(string sql, object param = null, 
            IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            try
            {
                await OpenAsync();
                var result = await _connection.QuerySingleOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType);
                
                _logger.LogDebug($"Single or default query executed successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing single or default query: {sql}");
                throw;
            }
        }

        /// <summary>
        /// Execute query and return first result
        /// </summary>
        public async Task<T> QueryFirstAsync<T>(string sql, object param = null, 
            IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            try
            {
                await OpenAsync();
                var result = await _connection.QueryFirstAsync<T>(sql, param, transaction, commandTimeout, commandType);
                
                _logger.LogDebug($"First query executed successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing first query: {sql}");
                throw;
            }
        }

        /// <summary>
        /// Execute multiple result sets query
        /// </summary>
        public async Task<SqlMapper.GridReader> QueryMultipleAsync(string sql, object param = null, 
            IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            try
            {
                await OpenAsync();
                var result = await _connection.QueryMultipleAsync(sql, param, transaction, commandTimeout, commandType);
                
                _logger.LogDebug($"Multiple query executed successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing multiple query: {sql}");
                throw;
            }
        }

        #endregion

        #region Command Operations

        /// <summary>
        /// Execute command and return affected rows
        /// </summary>
        public async Task<int> ExecuteAsync(string sql, object param = null, 
            IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            try
            {
                await OpenAsync();
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                var rowsAffected = await _connection.ExecuteAsync(sql, param, transaction, commandTimeout, commandType);
                
                stopwatch.Stop();
                _logger.LogDebug($"Command executed in {stopwatch.ElapsedMilliseconds}ms, affected {rowsAffected} rows");
                
                return rowsAffected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing command: {sql}");
                throw;
            }
        }

        /// <summary>
        /// Execute command and return scalar result
        /// </summary>
        public async Task<T> ExecuteScalarAsync<T>(string sql, object param = null, 
            IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            try
            {
                await OpenAsync();
                var result = await _connection.ExecuteScalarAsync<T>(sql, param, transaction, commandTimeout, commandType);
                
                _logger.LogDebug($"Scalar command executed successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing scalar command: {sql}");
                throw;
            }
        }

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Bulk insert using Dapper.Contrib
        /// </summary>
        public async Task<int> BulkInsertAsync<T>(IEnumerable<T> entities, IDbTransaction transaction = null) where T : class
        {
            try
            {
                await OpenAsync();
                var entityList = entities.ToList();
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                if (entityList.Count == 1)
                {
                    var id = await _connection.InsertAsync(entityList.First(), transaction);
                    stopwatch.Stop();
                    _logger.LogDebug($"Inserted 1 entity in {stopwatch.ElapsedMilliseconds}ms");
                    return 1;
                }
                else
                {
                    var rowsAffected = await _connection.InsertAsync(entityList, transaction);
                    stopwatch.Stop();
                    _logger.LogDebug($"Bulk inserted {entityList.Count} entities in {stopwatch.ElapsedMilliseconds}ms");
                    return rowsAffected;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error bulk inserting {typeof(T).Name} entities");
                throw;
            }
        }

        /// <summary>
        /// Bulk update using Dapper.Contrib
        /// </summary>
        public async Task<bool> BulkUpdateAsync<T>(IEnumerable<T> entities, IDbTransaction transaction = null) where T : class
        {
            try
            {
                await OpenAsync();
                var entityList = entities.ToList();
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                var result = await _connection.UpdateAsync(entityList, transaction);
                
                stopwatch.Stop();
                _logger.LogDebug($"Bulk updated {entityList.Count} entities in {stopwatch.ElapsedMilliseconds}ms");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error bulk updating {typeof(T).Name} entities");
                throw;
            }
        }

        /// <summary>
        /// Bulk delete using Dapper.Contrib
        /// </summary>
        public async Task<bool> BulkDeleteAsync<T>(IEnumerable<T> entities, IDbTransaction transaction = null) where T : class
        {
            try
            {
                await OpenAsync();
                var entityList = entities.ToList();
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                var result = await _connection.DeleteAsync(entityList, transaction);
                
                stopwatch.Stop();
                _logger.LogDebug($"Bulk deleted {entityList.Count} entities in {stopwatch.ElapsedMilliseconds}ms");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error bulk deleting {typeof(T).Name} entities");
                throw;
            }
        }

        #endregion

        #region CRUD Operations

        /// <summary>
        /// Get entity by ID
        /// </summary>
        public async Task<T> GetAsync<T>(object id, IDbTransaction transaction = null) where T : class
        {
            try
            {
                await OpenAsync();
                var result = await _connection.GetAsync<T>(id, transaction);
                
                _logger.LogDebug($"Retrieved {typeof(T).Name} entity with ID: {id}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting {typeof(T).Name} entity with ID: {id}");
                throw;
            }
        }

        /// <summary>
        /// Get all entities of type T
        /// </summary>
        public async Task<IEnumerable<T>> GetAllAsync<T>(IDbTransaction transaction = null) where T : class
        {
            try
            {
                await OpenAsync();
                var results = await _connection.GetAllAsync<T>(transaction);
                
                _logger.LogDebug($"Retrieved {results.Count()} {typeof(T).Name} entities");
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting all {typeof(T).Name} entities");
                throw;
            }
        }

        /// <summary>
        /// Insert entity
        /// </summary>
        public async Task<long> InsertAsync<T>(T entity, IDbTransaction transaction = null) where T : class
        {
            try
            {
                await OpenAsync();
                var id = await _connection.InsertAsync(entity, transaction);
                
                _logger.LogDebug($"Inserted {typeof(T).Name} entity with ID: {id}");
                return id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error inserting {typeof(T).Name} entity");
                throw;
            }
        }

        /// <summary>
        /// Update entity
        /// </summary>
        public async Task<bool> UpdateAsync<T>(T entity, IDbTransaction transaction = null) where T : class
        {
            try
            {
                await OpenAsync();
                var result = await _connection.UpdateAsync(entity, transaction);
                
                _logger.LogDebug($"Updated {typeof(T).Name} entity");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating {typeof(T).Name} entity");
                throw;
            }
        }

        /// <summary>
        /// Delete entity
        /// </summary>
        public async Task<bool> DeleteAsync<T>(T entity, IDbTransaction transaction = null) where T : class
        {
            try
            {
                await OpenAsync();
                var result = await _connection.DeleteAsync(entity, transaction);
                
                _logger.LogDebug($"Deleted {typeof(T).Name} entity");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting {typeof(T).Name} entity");
                throw;
            }
        }

        #endregion

        #region Stored Procedures

        /// <summary>
        /// Execute stored procedure
        /// </summary>
        public async Task<IEnumerable<T>> ExecuteStoredProcedureAsync<T>(string procedureName, object parameters = null,
            IDbTransaction transaction = null, int? commandTimeout = null)
        {
            try
            {
                await OpenAsync();
                var result = await _connection.QueryAsync<T>(procedureName, parameters, transaction, 
                    commandTimeout, CommandType.StoredProcedure);
                
                _logger.LogDebug($"Stored procedure {procedureName} executed successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing stored procedure: {procedureName}");
                throw;
            }
        }

        /// <summary>
        /// Execute stored procedure with output parameters
        /// </summary>
        public async Task<StoredProcedureResult<T>> ExecuteStoredProcedureWithOutputAsync<T>(string procedureName, 
            DynamicParameters parameters, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            try
            {
                await OpenAsync();
                var result = await _connection.QueryAsync<T>(procedureName, parameters, transaction, 
                    commandTimeout, CommandType.StoredProcedure);

                // Extract output parameters
                var outputParams = new Dictionary<string, object>();
                foreach (var paramName in parameters.ParameterNames)
                {
                    if (parameters.Get<object>(paramName) != null)
                    {
                        outputParams[paramName] = parameters.Get<object>(paramName);
                    }
                }
                
                _logger.LogDebug($"Stored procedure {procedureName} executed with output parameters");
                return new StoredProcedureResult<T>
                {
                    Data = result,
                    OutputParameters = outputParams
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing stored procedure with output: {procedureName}");
                throw;
            }
        }

        #endregion

        #region Transaction Management

        /// <summary>
        /// Execute operations within a transaction
        /// </summary>
        public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<IDbTransaction, Task<TResult>> operation, 
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            await OpenAsync();
            using var transaction = _connection.BeginTransaction(isolationLevel);
            
            try
            {
                var result = await operation(transaction);
                transaction.Commit();
                
                _logger.LogDebug("Transaction committed successfully");
                return result;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Transaction rolled back due to error");
                throw;
            }
        }

        /// <summary>
        /// Execute operations within a TransactionScope
        /// </summary>
        public async Task<TResult> ExecuteInTransactionScopeAsync<TResult>(Func<Task<TResult>> operation, 
            TransactionScopeOption scopeOption = TransactionScopeOption.Required,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            using var scope = new TransactionScope(scopeOption, new TransactionOptions
            {
                IsolationLevel = isolationLevel,
                Timeout = TimeSpan.FromMinutes(5)
            }, TransactionScopeAsyncFlowOption.Enabled);
            
            try
            {
                var result = await operation();
                scope.Complete();
                
                _logger.LogDebug("Transaction scope completed successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction scope failed");
                throw;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Generate cache key from SQL and parameters
        /// </summary>
        private string GenerateCacheKey(string sql, object param)
        {
            var keyBuilder = new StringBuilder(sql);
            
            if (param != null)
            {
                var properties = param.GetType().GetProperties();
                foreach (var prop in properties.OrderBy(p => p.Name))
                {
                    keyBuilder.Append($"|{prop.Name}:{prop.GetValue(param)}");
                }
            }
            
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(keyBuilder.ToString()));
        }

        /// <summary>
        /// Clear query cache
        /// </summary>
        public void ClearCache()
        {
            if (_cache is MemoryCache memoryCache)
            {
                memoryCache.Compact(1.0); // Remove all cached items
            }
            
            _logger.LogDebug("Query cache cleared");
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _connection?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Result container for stored procedures with output parameters
    /// </summary>
    public class StoredProcedureResult<T>
    {
        public IEnumerable<T> Data { get; set; }
        public Dictionary<string, object> OutputParameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Dapper entity base class with common properties
    /// </summary>
    public abstract class DapperEntityBase
    {
        [Key]
        public virtual long Id { get; set; }
        
        [Write(false)]
        [Computed]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [Write(false)]
        [Computed]
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Performance metrics for Dapper operations
    /// </summary>
    public class DapperMetrics
    {
        public long TotalQueries { get; set; }
        public long TotalCommands { get; set; }
        public double AverageQueryTime { get; set; }
        public double AverageCommandTime { get; set; }
        public long CacheHits { get; set; }
        public long CacheMisses { get; set; }
        public double CacheHitRatio => CacheHits + CacheMisses == 0 ? 0 : (double)CacheHits / (CacheHits + CacheMisses);
    }
} 