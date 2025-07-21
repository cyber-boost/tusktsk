using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;
using System.Data.SQLite;
using System.Transactions;
using System.Text;
using System.Dynamic;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Linq;

namespace TuskLang.Database
{
    /// <summary>
    /// High-performance ADO.NET adapter for TuskTsk
    /// Provides maximum performance with raw database access
    /// </summary>
    public class AdoNetAdapter : IDisposable
    {
        private readonly string _connectionString;
        private readonly DatabaseType _databaseType;
        private readonly ILogger<AdoNetAdapter> _logger;
        private readonly IMemoryCache _cache;
        private readonly ConcurrentQueue<DbConnection> _connectionPool;
        private readonly SemaphoreSlim _poolSemaphore;
        private readonly int _maxPoolSize;
        private bool _disposed = false;

        public enum DatabaseType
        {
            SqlServer,
            PostgreSQL,
            MySQL,
            SQLite
        }

        public AdoNetAdapter(string connectionString, DatabaseType databaseType, 
            ILogger<AdoNetAdapter> logger = null, IMemoryCache cache = null, int maxPoolSize = 20)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _databaseType = databaseType;
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AdoNetAdapter>.Instance;
            _cache = cache ?? new MemoryCache(new MemoryCacheOptions());
            _maxPoolSize = maxPoolSize;
            _connectionPool = new ConcurrentQueue<DbConnection>();
            _poolSemaphore = new SemaphoreSlim(maxPoolSize, maxPoolSize);
        }

        #region Connection Management

        /// <summary>
        /// Get connection from pool or create new one
        /// </summary>
        private async Task<DbConnection> GetConnectionAsync()
        {
            await _poolSemaphore.WaitAsync();

            if (_connectionPool.TryDequeue(out var connection))
            {
                if (connection.State == ConnectionState.Open)
                {
                    return connection;
                }
                else
                {
                    connection.Dispose();
                }
            }

            connection = CreateConnection(_connectionString, _databaseType);
            await connection.OpenAsync();
            return connection;
        }

        /// <summary>
        /// Return connection to pool
        /// </summary>
        private void ReturnConnection(DbConnection connection)
        {
            if (connection != null && connection.State == ConnectionState.Open)
            {
                if (_connectionPool.Count < _maxPoolSize)
                {
                    _connectionPool.Enqueue(connection);
                }
                else
                {
                    connection.Dispose();
                }
            }
            _poolSemaphore.Release();
        }

        private DbConnection CreateConnection(string connectionString, DatabaseType databaseType)
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

        /// <summary>
        /// Test database connection
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            DbConnection connection = null;
            try
            {
                connection = await GetConnectionAsync();
                using var command = connection.CreateCommand();
                command.CommandText = GetTestQuery();
                command.CommandType = CommandType.Text;
                await command.ExecuteScalarAsync();
                _logger.LogInformation($"Successfully tested {_databaseType} connection");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to test {_databaseType} connection");
                return false;
            }
            finally
            {
                ReturnConnection(connection);
            }
        }

        private string GetTestQuery()
        {
            return _databaseType switch
            {
                DatabaseType.SqlServer => "SELECT 1",
                DatabaseType.PostgreSQL => "SELECT 1",
                DatabaseType.MySQL => "SELECT 1",
                DatabaseType.SQLite => "SELECT 1",
                _ => "SELECT 1"
            };
        }

        #endregion

        #region Query Operations

        /// <summary>
        /// Execute query and return DataTable
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters = null, 
            int? commandTimeout = null)
        {
            DbConnection connection = null;
            try
            {
                connection = await GetConnectionAsync();
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.CommandType = CommandType.Text;
                
                if (commandTimeout.HasValue)
                    command.CommandTimeout = commandTimeout.Value;

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

                var dataTable = new DataTable();
                using var reader = await command.ExecuteReaderAsync();
                dataTable.Load(reader);
                
                _logger.LogDebug($"Executed query: {sql}, returned {dataTable.Rows.Count} rows");
                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing query: {sql}");
                throw;
            }
            finally
            {
                ReturnConnection(connection);
            }
        }

        /// <summary>
        /// Execute query and return scalar value
        /// </summary>
        public async Task<object> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters = null, 
            int? commandTimeout = null)
        {
            DbConnection connection = null;
            try
            {
                connection = await GetConnectionAsync();
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.CommandType = CommandType.Text;
                
                if (commandTimeout.HasValue)
                    command.CommandTimeout = commandTimeout.Value;

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

                var result = await command.ExecuteScalarAsync();
                _logger.LogDebug($"Executed scalar query: {sql}, result: {result}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing scalar query: {sql}");
                throw;
            }
            finally
            {
                ReturnConnection(connection);
            }
        }

        /// <summary>
        /// Execute non-query command
        /// </summary>
        public async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters = null, 
            int? commandTimeout = null)
        {
            DbConnection connection = null;
            try
            {
                connection = await GetConnectionAsync();
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.CommandType = CommandType.Text;
                
                if (commandTimeout.HasValue)
                    command.CommandTimeout = commandTimeout.Value;

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

                var rowsAffected = await command.ExecuteNonQueryAsync();
                _logger.LogDebug($"Executed non-query: {sql}, rows affected: {rowsAffected}");
                return rowsAffected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing non-query: {sql}");
                throw;
            }
            finally
            {
                ReturnConnection(connection);
            }
        }

        /// <summary>
        /// Execute stored procedure
        /// </summary>
        public async Task<DataTable> ExecuteStoredProcedureAsync(string procedureName, 
            Dictionary<string, object> parameters = null, int? commandTimeout = null)
        {
            DbConnection connection = null;
            try
            {
                connection = await GetConnectionAsync();
                using var command = connection.CreateCommand();
                command.CommandText = procedureName;
                command.CommandType = CommandType.StoredProcedure;
                
                if (commandTimeout.HasValue)
                    command.CommandTimeout = commandTimeout.Value;

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

                var dataTable = new DataTable();
                using var reader = await command.ExecuteReaderAsync();
                dataTable.Load(reader);
                
                _logger.LogDebug($"Executed stored procedure: {procedureName}, returned {dataTable.Rows.Count} rows");
                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing stored procedure: {procedureName}");
                throw;
            }
            finally
            {
                ReturnConnection(connection);
            }
        }

        #endregion

        #region Transaction Management

        /// <summary>
        /// Execute operation within transaction
        /// </summary>
        public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<DbTransaction, Task<TResult>> operation, 
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            DbConnection connection = null;
            DbTransaction transaction = null;
            try
            {
                connection = await GetConnectionAsync();
                transaction = await connection.BeginTransactionAsync(isolationLevel);
                
                var result = await operation(transaction);
                
                await transaction.CommitAsync();
                _logger.LogDebug("Transaction committed successfully");
                
                return result;
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                    _logger.LogWarning("Transaction rolled back due to error");
                }
                _logger.LogError(ex, "Error in transaction");
                throw;
            }
            finally
            {
                transaction?.Dispose();
                ReturnConnection(connection);
            }
        }

        /// <summary>
        /// Execute operation within TransactionScope
        /// </summary>
        public async Task<TResult> ExecuteInTransactionScopeAsync<TResult>(Func<Task<TResult>> operation, 
            TransactionScopeOption scopeOption = TransactionScopeOption.Required,
            System.Transactions.IsolationLevel isolationLevel = System.Transactions.IsolationLevel.ReadCommitted)
        {
            using var scope = new TransactionScope(scopeOption, 
                new TransactionOptions { IsolationLevel = isolationLevel }, 
                TransactionScopeAsyncFlowOption.Enabled);
            
            try
            {
                var result = await operation();
                scope.Complete();
                _logger.LogDebug("TransactionScope completed successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TransactionScope");
                throw;
            }
        }

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Bulk insert data
        /// </summary>
        public async Task<int> BulkInsertAsync(string tableName, DataTable data, int batchSize = 1000)
        {
            if (data == null || data.Rows.Count == 0)
                return 0;

            var totalRows = 0;
            var batches = (int)Math.Ceiling((double)data.Rows.Count / batchSize);

            for (int i = 0; i < batches; i++)
            {
                var startIndex = i * batchSize;
                var endIndex = Math.Min(startIndex + batchSize, data.Rows.Count);
                var batchData = data.Clone();

                for (int j = startIndex; j < endIndex; j++)
                {
                    batchData.ImportRow(data.Rows[j]);
                }

                totalRows += await BulkInsertBatchAsync(tableName, batchData);
            }

            _logger.LogInformation($"Bulk inserted {totalRows} rows into {tableName}");
            return totalRows;
        }

        private async Task<int> BulkInsertBatchAsync(string tableName, DataTable data)
        {
            if (data.Rows.Count == 0) return 0;

            var columns = string.Join(", ", data.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            var placeholders = string.Join(", ", data.Columns.Cast<DataColumn>().Select(c => $"@{c.ColumnName}"));
            var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({placeholders})";

            return await ExecuteInTransactionAsync(async transaction =>
            {
                var totalRows = 0;
                foreach (DataRow row in data.Rows)
                {
                    var parameters = new Dictionary<string, object>();
                    foreach (DataColumn column in data.Columns)
                    {
                        parameters[$"@{column.ColumnName}"] = row[column];
                    }
                    
                    using var command = transaction.Connection.CreateCommand();
                    command.CommandText = sql;
                    command.CommandType = CommandType.Text;
                    command.Transaction = transaction;

                    foreach (var param in parameters)
                    {
                        var dbParam = command.CreateParameter();
                        dbParam.ParameterName = param.Key;
                        dbParam.Value = param.Value ?? DBNull.Value;
                        command.Parameters.Add(dbParam);
                    }

                    totalRows += await command.ExecuteNonQueryAsync();
                }
                return totalRows;
            });
        }

        #endregion

        #region Caching

        /// <summary>
        /// Execute query with caching
        /// </summary>
        public async Task<DataTable> ExecuteQueryCachedAsync(string sql, Dictionary<string, object> parameters = null, 
            TimeSpan? cacheExpiration = null, string cacheKey = null)
        {
            var key = cacheKey ?? GenerateCacheKey(sql, parameters);
            
            if (_cache.TryGetValue(key, out DataTable cachedResult))
            {
                _logger.LogDebug($"Cache hit for query: {sql}");
                return cachedResult;
            }

            var result = await ExecuteQueryAsync(sql, parameters);
            
            var expiration = cacheExpiration ?? TimeSpan.FromMinutes(5);
            _cache.Set(key, result, expiration);
            
            _logger.LogDebug($"Cache miss for query: {sql}, cached for {expiration}");
            return result;
        }

        private string GenerateCacheKey(string sql, Dictionary<string, object> parameters)
        {
            var key = new StringBuilder(sql);
            if (parameters != null)
            {
                foreach (var param in parameters.OrderBy(p => p.Key))
                {
                    key.Append($":{param.Key}={param.Value}");
                }
            }
            return key.ToString();
        }

        /// <summary>
        /// Clear cache
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
            _logger.LogInformation("Cache cleared");
        }

        #endregion

        #region Metrics and Monitoring

        /// <summary>
        /// Get database metrics
        /// </summary>
        public AdoNetMetrics GetMetrics()
        {
            return new AdoNetMetrics
            {
                ConnectionPoolSize = _connectionPool.Count,
                AvailableConnections = _poolSemaphore.CurrentCount,
                MaxPoolSize = _maxPoolSize,
                DatabaseType = _databaseType.ToString()
            };
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            if (_disposed) return;

            while (_connectionPool.TryDequeue(out var connection))
            {
                connection?.Dispose();
            }

            _poolSemaphore?.Dispose();
            _cache?.Dispose();
            _disposed = true;
        }

        #endregion
    }

    /// <summary>
    /// Metrics for ADO.NET adapter
    /// </summary>
    public class AdoNetMetrics
    {
        public int ConnectionPoolSize { get; set; }
        public int AvailableConnections { get; set; }
        public int MaxPoolSize { get; set; }
        public string DatabaseType { get; set; }
        public long TotalQueries { get; set; }
        public long TotalCommands { get; set; }
        public double AverageQueryTime { get; set; }
        public double AverageCommandTime { get; set; }
        public long CacheHits { get; set; }
        public long CacheMisses { get; set; }
        public double CacheHitRatio => CacheHits + CacheMisses == 0 ? 0 : (double)CacheHits / (CacheHits + CacheMisses);
    }
} 