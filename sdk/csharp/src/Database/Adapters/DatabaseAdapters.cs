using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Data.SQLite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Polly;
using Polly.Retry;
using TuskLang.Configuration;

namespace TuskLang
{
    /// <summary>
    /// Production-ready database adapters for TuskLang C# SDK
    /// </summary>
    public class DatabaseAdapters : IDisposable
    {
        private readonly Dictionary<string, IDbConnection> _connections;
        private readonly Dictionary<string, string> _connectionStrings;
        private readonly ILogger<DatabaseAdapters> _logger;
        private readonly IMemoryCache _cache;
        private bool _disposed = false;

        public DatabaseAdapters(ILogger<DatabaseAdapters> logger = null, IMemoryCache cache = null)
        {
            _connections = new Dictionary<string, IDbConnection>();
            _connectionStrings = new Dictionary<string, string>();
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseAdapters>.Instance;
            _cache = cache ?? new MemoryCache(new MemoryCacheOptions());
        }

        #region SQLite Real Implementation

        public class SQLiteAdapter : IDisposable
        {
            private readonly string _connectionString;
            private SQLiteConnection _connection;
            private readonly ILogger _logger;
            private readonly AsyncPolicy _retryPolicy;
            private bool _disposed = false;

            public SQLiteAdapter(string connectionString, ILogger logger = null)
            {
                _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
                _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
                
                _retryPolicy = Policy
                    .Handle<SQLiteException>()
                    .Or<TimeoutException>()
                    .WaitAndRetryAsync(
                        retryCount: 3,
                        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        onRetry: (outcome, timespan, retryCount, context) =>
                        {
                            _logger.LogWarning($"SQLite operation failed. Retry {retryCount} in {timespan}ms");
                        });
            }

            public SQLiteAdapter(string connectionString) : this(connectionString, null)
            {
            }

            public async Task<bool> ConnectAsync()
            {
                try
                {
                    if (_connection?.State == ConnectionState.Open)
                        return true;

                    _connection?.Dispose();
                    _connection = new SQLiteConnection(_connectionString);
                    await _connection.OpenAsync();
                    
                    _logger.LogInformation("Successfully connected to SQLite");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to SQLite");
                    return false;
                }
            }

            public async Task<bool> DisconnectAsync()
            {
                try
                {
                    if (_connection?.State == ConnectionState.Open)
                    {
                        await _connection.CloseAsync();
                    }
                    _connection?.Dispose();
                    _connection = null;
                    
                    _logger.LogInformation("Disconnected from SQLite");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to disconnect from SQLite");
                    return false;
                }
            }

            public async Task<List<Dictionary<string, object>>> QueryAsync(string sql, Dictionary<string, object> parameters = null)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    using var command = _connection.CreateCommand();
                    command.CommandText = sql;
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

                    var results = new List<Dictionary<string, object>>();
                    using var reader = await command.ExecuteReaderAsync();
                    
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }
                        results.Add(row);
                    }

                    return results;
                });
            }

            public async Task<int> ExecuteAsync(string sql, Dictionary<string, object> parameters = null)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    using var command = _connection.CreateCommand();
                    command.CommandText = sql;
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

                    return await command.ExecuteNonQueryAsync();
                });
            }

            public async Task<object> ScalarAsync(string sql, Dictionary<string, object> parameters = null)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    using var command = _connection.CreateCommand();
                    command.CommandText = sql;
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

                    var result = await command.ExecuteScalarAsync();
                    return result == DBNull.Value ? null : result;
                });
            }

            public async Task<SQLiteTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    // SQLite doesn't support isolation levels, so we ignore the parameter
                    return await _connection.BeginTransactionAsync();
                });
            }

            public async Task<bool> IsConnectedAsync()
            {
                try
                {
                    if (_connection?.State != ConnectionState.Open)
                        return false;

                    using var command = _connection.CreateCommand();
                    command.CommandText = "SELECT 1";
                    command.CommandType = CommandType.Text;
                    
                    var result = await command.ExecuteScalarAsync();
                    return result != null && result.ToString() == "1";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to check SQLite connection");
                    return false;
                }
            }

            public async Task<Dictionary<string, object>> GetServerInfoAsync()
            {
                try
                {
                    var info = new Dictionary<string, object>();
                    
                    // Get SQLite version
                    var version = await ScalarAsync("SELECT sqlite_version()");
                    info["version"] = version?.ToString() ?? "Unknown";
                    
                    // Get database file info
                    info["database"] = _connection.Database;
                    info["dataSource"] = _connection.DataSource;
                    
                    return info;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get SQLite server info");
                    return new Dictionary<string, object> { ["error"] = ex.Message };
                }
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _connection?.Dispose();
                    _disposed = true;
                }
            }
        }

        #endregion

        #region SQL Server Real Implementation

        public class SqlServerAdapter : IDisposable
        {
            private readonly string _connectionString;
            private SqlConnection _connection;
            private readonly ILogger _logger;
            private readonly AsyncPolicy _retryPolicy;
            private bool _disposed = false;

            public SqlServerAdapter(string connectionString, ILogger logger = null)
            {
                _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
                _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
                
                _retryPolicy = Policy
                    .Handle<SqlException>()
                    .Or<TimeoutException>()
                    .WaitAndRetryAsync(
                        retryCount: 3,
                        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        onRetry: (outcome, timespan, retryCount, context) =>
                        {
                            _logger.LogWarning($"SQL Server operation failed. Retry {retryCount} in {timespan}ms");
                        });
            }

            public SqlServerAdapter(string connectionString) : this(connectionString, null)
            {
            }

            public async Task<bool> ConnectAsync()
            {
                try
                {
                    if (_connection?.State == ConnectionState.Open)
                        return true;

                    _connection?.Dispose();
                    _connection = new SqlConnection(_connectionString);
                    await _connection.OpenAsync();
                    
                    _logger.LogInformation("Successfully connected to SQL Server");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to SQL Server");
                    return false;
                }
            }

            public async Task<bool> DisconnectAsync()
            {
                try
                {
                    if (_connection?.State == ConnectionState.Open)
                    {
                        await _connection.CloseAsync();
                    }
                    _connection?.Dispose();
                    _connection = null;
                    
                    _logger.LogInformation("Disconnected from SQL Server");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to disconnect from SQL Server");
                    return false;
                }
            }

            public async Task<List<Dictionary<string, object>>> QueryAsync(string sql, Dictionary<string, object> parameters = null)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    using var command = _connection.CreateCommand();
                    command.CommandText = sql;
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

                    var results = new List<Dictionary<string, object>>();
                    using var reader = await command.ExecuteReaderAsync();
                    
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }
                        results.Add(row);
                    }

                    return results;
                });
            }

            public async Task<int> ExecuteAsync(string sql, Dictionary<string, object> parameters = null)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    using var command = _connection.CreateCommand();
                    command.CommandText = sql;
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

                    return await command.ExecuteNonQueryAsync();
                });
            }

            public async Task<object> ScalarAsync(string sql, Dictionary<string, object> parameters = null)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    using var command = _connection.CreateCommand();
                    command.CommandText = sql;
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

                    var result = await command.ExecuteScalarAsync();
                    return result == DBNull.Value ? null : result;
                });
            }

            public async Task<SqlTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    var transaction = await _connection.BeginTransactionAsync(isolationLevel);
                    return (SqlTransaction)transaction;
                });
            }

            public async Task<bool> IsConnectedAsync()
            {
                try
                {
                    if (_connection?.State != ConnectionState.Open)
                        return false;

                    using var command = _connection.CreateCommand();
                    command.CommandText = "SELECT 1";
                    command.CommandType = CommandType.Text;
                    
                    var result = await command.ExecuteScalarAsync();
                    return result != null && result.ToString() == "1";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to check SQL Server connection");
                    return false;
                }
            }

            public async Task<Dictionary<string, object>> GetServerInfoAsync()
            {
                try
                {
                    var info = new Dictionary<string, object>();
                    
                    // Get SQL Server version
                    var version = await ScalarAsync("SELECT @@VERSION");
                    info["version"] = version?.ToString() ?? "Unknown";
                    
                    // Get database info
                    info["database"] = _connection.Database;
                    info["dataSource"] = _connection.DataSource;
                    info["serverVersion"] = _connection.ServerVersion?.ToString() ?? "Unknown";
                    
                    return info;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get SQL Server info");
                    return new Dictionary<string, object> { ["error"] = ex.Message };
                }
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _connection?.Dispose();
                    _disposed = true;
                }
            }
        }

        #endregion

        public async Task<SQLiteAdapter> CreateSQLiteAdapterAsync(string connectionString)
        {
            var adapter = new SQLiteAdapter(connectionString, _logger);
            await adapter.ConnectAsync();
            return adapter;
        }

        public async Task<SqlServerAdapter> CreateSqlServerAdapterAsync(string connectionString)
        {
            var adapter = new SqlServerAdapter(connectionString, _logger);
            await adapter.ConnectAsync();
            return adapter;
        }

        public async Task<Dictionary<string, bool>> HealthCheckAllAsync()
        {
            var results = new Dictionary<string, bool>();
            
            foreach (var connection in _connections)
            {
                try
                {
                    var isConnected = connection.Value.State == ConnectionState.Open;
                    results[connection.Key] = isConnected;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Health check failed for connection: {connection.Key}");
                    results[connection.Key] = false;
                }
            }
            
            return results;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var connection in _connections.Values)
                {
                    connection?.Dispose();
                }
                _connections.Clear();
                _disposed = true;
            }
        }
    }
} 