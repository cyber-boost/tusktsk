using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;
using System.Data.SQLite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.ObjectPool;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http;

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
        private readonly ObjectPool<IDbConnection> _connectionPool;
        private bool _disposed = false;

        public DatabaseAdapters(ILogger<DatabaseAdapters> logger = null, IMemoryCache cache = null)
        {
            _connections = new Dictionary<string, IDbConnection>();
            _connectionStrings = new Dictionary<string, string>();
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseAdapters>.Instance;
            _cache = cache ?? new MemoryCache(new MemoryCacheOptions());
        }

        #region PostgreSQL Real Implementation

        public class PostgreSQLAdapter : IDisposable
        {
            private readonly string _connectionString;
            private NpgsqlConnection _connection;
            private readonly ILogger _logger;
            private readonly AsyncPolicy _retryPolicy;
            private bool _disposed = false;

            public PostgreSQLAdapter(string connectionString, ILogger logger = null)
            {
                _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
                _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
                
                _retryPolicy = Policy
                    .Handle<NpgsqlException>()
                    .Or<TimeoutException>()
                    .WaitAndRetryAsync(
                        retryCount: 3,
                        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        onRetry: (outcome, timespan, retryCount, context) =>
                        {
                            _logger.LogWarning($"PostgreSQL operation failed. Retry {retryCount} in {timespan}ms");
                        });
            }

            public async Task<bool> ConnectAsync()
            {
                try
                {
                    if (_connection?.State == ConnectionState.Open)
                        return true;

                    _connection?.Dispose();
                    _connection = new NpgsqlConnection(_connectionString);
                    await _connection.OpenAsync();
                    
                    _logger.LogInformation("Successfully connected to PostgreSQL");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to PostgreSQL");
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
                    
                    _logger.LogInformation("Disconnected from PostgreSQL");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disconnecting from PostgreSQL");
                    return false;
                }
            }

            public async Task<List<Dictionary<string, object>>> QueryAsync(string sql, Dictionary<string, object> parameters = null)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    if (_connection?.State != ConnectionState.Open)
                        throw new InvalidOperationException("Not connected to PostgreSQL");

                    using var command = new NpgsqlCommand(sql, _connection);
                    
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
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

                    _logger.LogDebug($"PostgreSQL query returned {results.Count} rows");
                    return results;
                });
            }

            public async Task<int> ExecuteAsync(string sql, Dictionary<string, object> parameters = null)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    if (_connection?.State != ConnectionState.Open)
                        throw new InvalidOperationException("Not connected to PostgreSQL");

                    using var command = new NpgsqlCommand(sql, _connection);
                    
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    _logger.LogDebug($"PostgreSQL command affected {rowsAffected} rows");
                    return rowsAffected;
                });
            }

            public async Task<object> ScalarAsync(string sql, Dictionary<string, object> parameters = null)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    if (_connection?.State != ConnectionState.Open)
                        throw new InvalidOperationException("Not connected to PostgreSQL");

                    using var command = new NpgsqlCommand(sql, _connection);
                    
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }

                    var result = await command.ExecuteScalarAsync();
                    _logger.LogDebug($"PostgreSQL scalar query returned: {result}");
                    return result == DBNull.Value ? null : result;
                });
            }

            public async Task<NpgsqlTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
            {
                if (_connection?.State != ConnectionState.Open)
                    throw new InvalidOperationException("Not connected to PostgreSQL");

                var transaction = await _connection.BeginTransactionAsync(isolationLevel);
                _logger.LogDebug("PostgreSQL transaction started");
                return transaction;
            }

            public async Task<bool> IsConnectedAsync()
            {
                try
                {
                    if (_connection?.State != ConnectionState.Open)
                        return false;

                    using var command = new NpgsqlCommand("SELECT 1", _connection);
                    await command.ExecuteScalarAsync();
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            public async Task<Dictionary<string, object>> GetServerInfoAsync()
            {
                if (_connection?.State != ConnectionState.Open)
                    throw new InvalidOperationException("Not connected to PostgreSQL");

                var info = new Dictionary<string, object>();

                using var command = new NpgsqlCommand(@"
                    SELECT 
                        version() as version,
                        current_database() as database,
                        current_user as user,
                        inet_server_addr() as server_address,
                        inet_server_port() as server_port", _connection);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        info[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                }

                return info;
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

        #region MySQL Real Implementation

        public class MySQLAdapter : IDisposable
        {
            private readonly string _connectionString;
            private MySqlConnection _connection;
            private readonly ILogger _logger;
            private readonly AsyncPolicy _retryPolicy;
            private bool _disposed = false;

            public MySQLAdapter(string connectionString, ILogger logger = null)
            {
                _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
                _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
                
                _retryPolicy = Policy
                    .Handle<MySqlException>()
                    .Or<TimeoutException>()
                    .WaitAndRetryAsync(
                        retryCount: 3,
                        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        onRetry: (outcome, timespan, retryCount, context) =>
                        {
                            _logger.LogWarning($"MySQL operation failed. Retry {retryCount} in {timespan}ms");
                        });
            }

            public async Task<bool> ConnectAsync()
            {
                try
                {
                    if (_connection?.State == ConnectionState.Open)
                        return true;

                    _connection?.Dispose();
                    _connection = new MySqlConnection(_connectionString);
                    await _connection.OpenAsync();
                    
                    _logger.LogInformation("Successfully connected to MySQL");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to MySQL");
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
                    
                    _logger.LogInformation("Disconnected from MySQL");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disconnecting from MySQL");
                    return false;
                }
            }

            public async Task<List<Dictionary<string, object>>> QueryAsync(string sql, Dictionary<string, object> parameters = null)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    if (_connection?.State != ConnectionState.Open)
                        throw new InvalidOperationException("Not connected to MySQL");

                    using var command = new MySqlCommand(sql, _connection);
                    
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
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

                    _logger.LogDebug($"MySQL query returned {results.Count} rows");
                    return results;
                });
            }

            public async Task<int> ExecuteAsync(string sql, Dictionary<string, object> parameters = null)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    if (_connection?.State != ConnectionState.Open)
                        throw new InvalidOperationException("Not connected to MySQL");

                    using var command = new MySqlCommand(sql, _connection);
                    
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    _logger.LogDebug($"MySQL command affected {rowsAffected} rows");
                    return rowsAffected;
                });
            }

            public async Task<object> ScalarAsync(string sql, Dictionary<string, object> parameters = null)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    if (_connection?.State != ConnectionState.Open)
                        throw new InvalidOperationException("Not connected to MySQL");

                    using var command = new MySqlCommand(sql, _connection);
                    
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }

                    var result = await command.ExecuteScalarAsync();
                    _logger.LogDebug($"MySQL scalar query returned: {result}");
                    return result == DBNull.Value ? null : result;
                });
            }

            public async Task<MySqlTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
            {
                if (_connection?.State != ConnectionState.Open)
                    throw new InvalidOperationException("Not connected to MySQL");

                var transaction = await _connection.BeginTransactionAsync(isolationLevel);
                _logger.LogDebug("MySQL transaction started");
                return transaction;
            }

            public async Task<bool> IsConnectedAsync()
            {
                try
                {
                    if (_connection?.State != ConnectionState.Open)
                        return false;

                    using var command = new MySqlCommand("SELECT 1", _connection);
                    await command.ExecuteScalarAsync();
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            public async Task<Dictionary<string, object>> GetServerInfoAsync()
            {
                if (_connection?.State != ConnectionState.Open)
                    throw new InvalidOperationException("Not connected to MySQL");

                var info = new Dictionary<string, object>();

                using var command = new MySqlCommand(@"
                    SELECT 
                        @@version as version,
                        DATABASE() as database,
                        USER() as user,
                        @@hostname as hostname,
                        @@port as port", _connection);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        info[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                }

                return info;
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
                    _logger.LogError(ex, "Error disconnecting from SQL Server");
                    return false;
                }
            }

            public async Task<List<Dictionary<string, object>>> QueryAsync(string sql, Dictionary<string, object> parameters = null)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    if (_connection?.State != ConnectionState.Open)
                        throw new InvalidOperationException("Not connected to SQL Server");

                    using var command = new SqlCommand(sql, _connection);
                    
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
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

                    _logger.LogDebug($"SQL Server query returned {results.Count} rows");
                    return results;
                });
            }

            public async Task<int> ExecuteAsync(string sql, Dictionary<string, object> parameters = null)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    if (_connection?.State != ConnectionState.Open)
                        throw new InvalidOperationException("Not connected to SQL Server");

                    using var command = new SqlCommand(sql, _connection);
                    
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    _logger.LogDebug($"SQL Server command affected {rowsAffected} rows");
                    return rowsAffected;
                });
            }

            public async Task<object> ScalarAsync(string sql, Dictionary<string, object> parameters = null)
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    if (_connection?.State != ConnectionState.Open)
                        throw new InvalidOperationException("Not connected to SQL Server");

                    using var command = new SqlCommand(sql, _connection);
                    
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }

                    var result = await command.ExecuteScalarAsync();
                    _logger.LogDebug($"SQL Server scalar query returned: {result}");
                    return result == DBNull.Value ? null : result;
                });
            }

            public async Task<SqlTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
            {
                if (_connection?.State != ConnectionState.Open)
                    throw new InvalidOperationException("Not connected to SQL Server");

                var transaction = (SqlTransaction)await _connection.BeginTransactionAsync(isolationLevel);
                _logger.LogDebug("SQL Server transaction started");
                return transaction;
            }

            public async Task<bool> IsConnectedAsync()
            {
                try
                {
                    if (_connection?.State != ConnectionState.Open)
                        return false;

                    using var command = new SqlCommand("SELECT 1", _connection);
                    await command.ExecuteScalarAsync();
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            public async Task<Dictionary<string, object>> GetServerInfoAsync()
            {
                if (_connection?.State != ConnectionState.Open)
                    throw new InvalidOperationException("Not connected to SQL Server");

                var info = new Dictionary<string, object>();

                using var command = new SqlCommand(@"
                    SELECT 
                        @@VERSION as version,
                        DB_NAME() as database,
                        SUSER_SNAME() as user,
                        @@SERVERNAME as server_name,
                        SERVERPROPERTY('ProductVersion') as product_version", _connection);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        info[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                }

                return info;
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

        #region Factory Methods

        public async Task<PostgreSQLAdapter> CreatePostgreSQLAdapterAsync(string connectionString)
        {
            var adapter = new PostgreSQLAdapter(connectionString, _logger);
            var connected = await adapter.ConnectAsync();
            if (!connected)
                throw new InvalidOperationException("Failed to connect to PostgreSQL");
            
            _connections["postgresql"] = adapter._connection;
            _connectionStrings["postgresql"] = connectionString;
            return adapter;
        }

        public async Task<MySQLAdapter> CreateMySQLAdapterAsync(string connectionString)
        {
            var adapter = new MySQLAdapter(connectionString, _logger);
            var connected = await adapter.ConnectAsync();
            if (!connected)
                throw new InvalidOperationException("Failed to connect to MySQL");
            
            _connections["mysql"] = adapter._connection;
            _connectionStrings["mysql"] = connectionString;
            return adapter;
        }

        public async Task<SqlServerAdapter> CreateSqlServerAdapterAsync(string connectionString)
        {
            var adapter = new SqlServerAdapter(connectionString, _logger);
            var connected = await adapter.ConnectAsync();
            if (!connected)
                throw new InvalidOperationException("Failed to connect to SQL Server");
            
            _connections["sqlserver"] = adapter._connection;
            _connectionStrings["sqlserver"] = connectionString;
            return adapter;
        }

        #endregion

        #region Health Check

        public async Task<Dictionary<string, bool>> HealthCheckAllAsync()
        {
            var results = new Dictionary<string, bool>();
            var tasks = new List<Task>();

            foreach (var kvp in _connections)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        using var command = kvp.Value.CreateCommand();
                        command.CommandText = "SELECT 1";
                        await command.ExecuteScalarAsync();
                        results[kvp.Key] = true;
                    }
                    catch
                    {
                        results[kvp.Key] = false;
                    }
                }));
            }

            await Task.WhenAll(tasks);
            return results;
        }

        #endregion

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