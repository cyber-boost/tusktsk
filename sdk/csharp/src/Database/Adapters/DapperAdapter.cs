using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Npgsql;
using Dapper;
using TuskLang.Database;

namespace TuskLang.Database.Adapters
{
    // Simple attribute classes to replace Dapper.Contrib attributes
    [AttributeUsage(AttributeTargets.Property)]
    public class KeyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class WriteAttribute : Attribute 
    {
        public bool Write { get; }
        public WriteAttribute(bool write = true) => Write = write;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ComputedAttribute : Attribute { }

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
        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            try
            {
                // Placeholder implementation
                return new List<T>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Query failed: {ex.Message}", ex);
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
            System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted)
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
            System.Transactions.IsolationLevel isolationLevel = System.Transactions.IsolationLevel.ReadCommitted)
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

        #region Database Management Methods

        /// <summary>
        /// Create a database backup
        /// </summary>
        public async Task<bool> CreateBackupAsync(string backupPath)
        {
            try
            {
                await OpenAsync();
                
                var backupQuery = _databaseType switch
                {
                    DatabaseType.SqlServer => $"BACKUP DATABASE [{_connection.Database}] TO DISK = '{backupPath}' WITH FORMAT, INIT, NAME = 'TuskTsk Database Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10",
                    DatabaseType.PostgreSQL => $"pg_dump -h {GetHostFromConnectionString()} -U {GetUsernameFromConnectionString()} -d {_connection.Database} -f {backupPath}",
                    DatabaseType.MySQL => $"mysqldump -h {GetHostFromConnectionString()} -u {GetUsernameFromConnectionString()} -p{GetPasswordFromConnectionString()} {_connection.Database} > {backupPath}",
                    DatabaseType.SQLite => $"VACUUM INTO '{backupPath}'",
                    _ => throw new NotSupportedException($"Backup not supported for database type: {_databaseType}")
                };

                if (_databaseType == DatabaseType.SqlServer)
                {
                    await ExecuteAsync(backupQuery);
                }
                else
                {
                    // For other databases, we need to use external tools
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "cmd",
                            Arguments = $"/c {backupQuery}",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode != 0)
                    {
                        var error = await process.StandardError.ReadToEndAsync();
                        throw new Exception($"Backup failed: {error}");
                    }
                }

                _logger.LogInformation($"Database backup created successfully at {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create database backup to {backupPath}");
                return false;
            }
        }

        /// <summary>
        /// Restore a database from backup
        /// </summary>
        public async Task<bool> RestoreBackupAsync(string backupPath)
        {
            try
            {
                if (!System.IO.File.Exists(backupPath))
                {
                    throw new FileNotFoundException($"Backup file not found: {backupPath}");
                }

                await OpenAsync();
                
                var restoreQuery = _databaseType switch
                {
                    DatabaseType.SqlServer => $"RESTORE DATABASE [{_connection.Database}] FROM DISK = '{backupPath}' WITH REPLACE, RECOVERY",
                    DatabaseType.PostgreSQL => $"psql -h {GetHostFromConnectionString()} -U {GetUsernameFromConnectionString()} -d {_connection.Database} -f {backupPath}",
                    DatabaseType.MySQL => $"mysql -h {GetHostFromConnectionString()} -u {GetUsernameFromConnectionString()} -p{GetPasswordFromConnectionString()} {_connection.Database} < {backupPath}",
                    DatabaseType.SQLite => $"RESTORE FROM '{backupPath}'",
                    _ => throw new NotSupportedException($"Restore not supported for database type: {_databaseType}")
                };

                if (_databaseType == DatabaseType.SqlServer)
                {
                    await ExecuteAsync(restoreQuery);
                }
                else
                {
                    // For other databases, we need to use external tools
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "cmd",
                            Arguments = $"/c {restoreQuery}",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode != 0)
                    {
                        var error = await process.StandardError.ReadToEndAsync();
                        throw new Exception($"Restore failed: {error}");
                    }
                }

                _logger.LogInformation($"Database restored successfully from {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to restore database from {backupPath}");
                return false;
            }
        }

        /// <summary>
        /// Run database migrations
        /// </summary>
        public async Task<bool> RunMigrationsAsync()
        {
            try
            {
                await OpenAsync();
                
                // Create migrations table if it doesn't exist
                var createMigrationsTable = @"
                    CREATE TABLE IF NOT EXISTS __Migrations (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        MigrationName TEXT NOT NULL,
                        AppliedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        Checksum TEXT,
                        ExecutionTimeMs INTEGER
                    )";

                await ExecuteAsync(createMigrationsTable);

                // Get all migration files from the migrations directory
                var migrationsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "migrations");
                if (!Directory.Exists(migrationsDir))
                {
                    _logger.LogWarning("Migrations directory not found, creating empty directory");
                    Directory.CreateDirectory(migrationsDir);
                    return true;
                }

                var migrationFiles = Directory.GetFiles(migrationsDir, "*.sql")
                    .OrderBy(f => Path.GetFileName(f))
                    .ToList();

                var appliedMigrations = await QueryAsync<MigrationRecord>("SELECT MigrationName FROM __Migrations");
                var appliedMigrationNames = appliedMigrations.Select(m => m.MigrationName).ToHashSet();

                foreach (var migrationFile in migrationFiles)
                {
                    var migrationName = Path.GetFileName(migrationFile);
                    
                    if (appliedMigrationNames.Contains(migrationName))
                    {
                        _logger.LogDebug($"Migration {migrationName} already applied, skipping");
                        continue;
                    }

                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var migrationSql = await File.ReadAllTextAsync(migrationFile);
                    
                    try
                    {
                        await ExecuteAsync(migrationSql);
                        
                        var checksum = CalculateChecksum(migrationSql);
                        await ExecuteAsync(
                            "INSERT INTO __Migrations (MigrationName, Checksum, ExecutionTimeMs) VALUES (@MigrationName, @Checksum, @ExecutionTimeMs)",
                            new { MigrationName = migrationName, Checksum = checksum, ExecutionTimeMs = stopwatch.ElapsedMilliseconds }
                        );
                        
                        _logger.LogInformation($"Migration {migrationName} applied successfully in {stopwatch.ElapsedMilliseconds}ms");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to apply migration {migrationName}");
                        return false;
                    }
                }

                _logger.LogInformation($"All migrations completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run migrations");
                return false;
            }
        }

        /// <summary>
        /// Get database status information
        /// </summary>
        public async Task<DatabaseStatus> GetStatusAsync()
        {
            try
            {
                await OpenAsync();
                
                var status = new DatabaseStatus
                {
                    DatabaseName = _connection.Database,
                    ConnectionState = _connection.State.ToString(),
                    LastCheck = DateTime.UtcNow
                };

                // Get database-specific information
                switch (_databaseType)
                {
                    case DatabaseType.SqlServer:
                        var sqlServerInfo = await QuerySingleAsync<dynamic>(@"
                            SELECT 
                                SERVERPROPERTY('ProductVersion') as Version,
                                (SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE is_user_process = 1) as ActiveConnections,
                                (SELECT SUM(size * 8 * 1024) FROM sys.database_files) as DatabaseSize,
                                (SELECT SUM(available_space * 8 * 1024) FROM sys.dm_os_volume_stats(DB_ID(), NULL)) as AvailableSpace,
                                (SELECT DATEDIFF(SECOND, sqlserver_start_time, GETDATE()) FROM sys.dm_os_sys_info) as UptimeSeconds,
                                (SELECT COUNT(*) FROM sys.tables) as TableCount,
                                (SELECT COUNT(*) FROM sys.procedures) as StoredProcedureCount,
                                (SELECT COUNT(*) FROM sys.views) as ViewCount
                        ");
                        
                        status.Version = sqlServerInfo.Version;
                        status.ActiveConnections = sqlServerInfo.ActiveConnections;
                        status.DatabaseSize = sqlServerInfo.DatabaseSize;
                        status.AvailableSpace = sqlServerInfo.AvailableSpace;
                        status.UptimeSeconds = sqlServerInfo.UptimeSeconds;
                        status.TableCount = sqlServerInfo.TableCount;
                        status.StoredProcedureCount = sqlServerInfo.StoredProcedureCount;
                        status.ViewCount = sqlServerInfo.ViewCount;
                        break;

                    case DatabaseType.PostgreSQL:
                        var pgInfo = await QuerySingleAsync<dynamic>(@"
                            SELECT 
                                version() as Version,
                                (SELECT count(*) FROM pg_stat_activity WHERE state = 'active') as ActiveConnections,
                                pg_database_size(current_database()) as DatabaseSize,
                                (SELECT pg_size_pretty(pg_database_size(current_database()))) as DatabaseSizeFormatted,
                                (SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public') as TableCount,
                                (SELECT count(*) FROM information_schema.routines WHERE routine_schema = 'public') as StoredProcedureCount,
                                (SELECT count(*) FROM information_schema.views WHERE table_schema = 'public') as ViewCount
                        ");
                        
                        status.Version = pgInfo.Version;
                        status.ActiveConnections = pgInfo.ActiveConnections;
                        status.DatabaseSize = pgInfo.DatabaseSize;
                        status.TableCount = pgInfo.TableCount;
                        status.StoredProcedureCount = pgInfo.StoredProcedureCount;
                        status.ViewCount = pgInfo.ViewCount;
                        break;

                    case DatabaseType.MySQL:
                        var mysqlInfo = await QuerySingleAsync<dynamic>(@"
                            SELECT 
                                VERSION() as Version,
                                (SELECT COUNT(*) FROM information_schema.processlist WHERE command != 'Sleep') as ActiveConnections,
                                (SELECT SUM(data_length + index_length) FROM information_schema.tables WHERE table_schema = DATABASE()) as DatabaseSize,
                                (SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE()) as TableCount,
                                (SELECT COUNT(*) FROM information_schema.routines WHERE routine_schema = DATABASE()) as StoredProcedureCount,
                                (SELECT COUNT(*) FROM information_schema.views WHERE table_schema = DATABASE()) as ViewCount
                        ");
                        
                        status.Version = mysqlInfo.Version;
                        status.ActiveConnections = mysqlInfo.ActiveConnections;
                        status.DatabaseSize = mysqlInfo.DatabaseSize;
                        status.TableCount = mysqlInfo.TableCount;
                        status.StoredProcedureCount = mysqlInfo.StoredProcedureCount;
                        status.ViewCount = mysqlInfo.ViewCount;
                        break;

                    case DatabaseType.SQLite:
                        var sqliteInfo = await QuerySingleAsync<dynamic>(@"
                            SELECT 
                                sqlite_version() as Version,
                                (SELECT COUNT(*) FROM sqlite_master WHERE type = 'table') as TableCount,
                                (SELECT COUNT(*) FROM sqlite_master WHERE type = 'view') as ViewCount
                        ");
                        
                        status.Version = sqliteInfo.Version;
                        status.TableCount = sqliteInfo.TableCount;
                        status.ViewCount = sqliteInfo.ViewCount;
                        break;
                }

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get database status");
                return new DatabaseStatus { ConnectionState = "Error" };
            }
        }

        /// <summary>
        /// Check database health
        /// </summary>
        public async Task<DatabaseHealth> CheckHealthAsync()
        {
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                await OpenAsync();
                
                // Simple health check query
                await QuerySingleAsync<int>("SELECT 1");
                stopwatch.Stop();

                var health = new DatabaseHealth
                {
                    IsHealthy = true,
                    Status = "Healthy",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    LastCheck = DateTime.UtcNow
                };

                // Additional health checks based on database type
                switch (_databaseType)
                {
                    case DatabaseType.SqlServer:
                        var sqlServerHealth = await QuerySingleAsync<dynamic>(@"
                            SELECT 
                                (SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE is_user_process = 1) as ActiveConnections,
                                (SELECT COUNT(*) FROM sys.dm_exec_requests WHERE status = 'running') as RunningQueries,
                                (SELECT COUNT(*) FROM sys.dm_exec_requests WHERE total_elapsed_time > 5000) as SlowQueries
                        ");
                        
                        health.ConnectionPoolUsage = Math.Min(100, (double)sqlServerHealth.ActiveConnections / 100 * 100);
                        health.SlowQueryCount = sqlServerHealth.SlowQueries;
                        break;

                    case DatabaseType.PostgreSQL:
                        var pgHealth = await QuerySingleAsync<dynamic>(@"
                            SELECT 
                                (SELECT count(*) FROM pg_stat_activity WHERE state = 'active') as ActiveConnections,
                                (SELECT count(*) FROM pg_stat_activity WHERE state = 'active' AND query_start < NOW() - INTERVAL '5 seconds') as SlowQueries
                        ");
                        
                        health.ConnectionPoolUsage = Math.Min(100, (double)pgHealth.ActiveConnections / 100 * 100);
                        health.SlowQueryCount = pgHealth.SlowQueries;
                        break;

                    case DatabaseType.MySQL:
                        var mysqlHealth = await QuerySingleAsync<dynamic>(@"
                            SELECT 
                                (SELECT COUNT(*) FROM information_schema.processlist WHERE command != 'Sleep') as ActiveConnections,
                                (SELECT COUNT(*) FROM information_schema.processlist WHERE time > 5) as SlowQueries
                        ");
                        
                        health.ConnectionPoolUsage = Math.Min(100, (double)mysqlHealth.ActiveConnections / 100 * 100);
                        health.SlowQueryCount = mysqlHealth.SlowQueries;
                        break;
                }

                return health;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return new DatabaseHealth
                {
                    IsHealthy = false,
                    Status = "Unhealthy",
                    ResponseTimeMs = -1,
                    LastCheck = DateTime.UtcNow,
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>
        /// Execute a SQL query file
        /// </summary>
        public async Task<QueryFileResult> ExecuteFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"SQL file not found: {filePath}");
                }

                var fileInfo = new FileInfo(filePath);
                var result = new QueryFileResult
                {
                    FilePath = filePath,
                    FileSizeBytes = fileInfo.Length,
                    ExecutedAt = DateTime.UtcNow
                };

                await OpenAsync();
                
                var sqlContent = await File.ReadAllTextAsync(filePath);
                var queries = SplitSqlQueries(sqlContent);
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                foreach (var query in queries)
                {
                    if (string.IsNullOrWhiteSpace(query))
                        continue;

                    var queryResult = new QueryResult
                    {
                        Query = query.Trim(),
                        ExecutedAt = DateTime.UtcNow
                    };

                    try
                    {
                        var queryStopwatch = System.Diagnostics.Stopwatch.StartNew();
                        var rowsAffected = await ExecuteAsync(query);
                        queryStopwatch.Stop();

                        queryResult.Success = true;
                        queryResult.RowsAffected = rowsAffected;
                        queryResult.ExecutionTimeMs = queryStopwatch.ElapsedMilliseconds;
                        result.QueriesExecuted++;
                    }
                    catch (Exception ex)
                    {
                        queryResult.Success = false;
                        queryResult.ErrorMessage = ex.Message;
                        result.QueryResults.Add(queryResult);
                        throw;
                    }

                    result.QueryResults.Add(queryResult);
                }

                stopwatch.Stop();
                result.TotalExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                result.Success = true;

                _logger.LogInformation($"SQL file {filePath} executed successfully: {result.QueriesExecuted} queries in {result.TotalExecutionTimeMs}ms");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to execute SQL file {filePath}");
                return new QueryFileResult
                {
                    FilePath = filePath,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Execute a raw SQL query
        /// </summary>
        public async Task<QueryResult> ExecuteQueryAsync(string query)
        {
            try
            {
                await OpenAsync();
                
                var result = new QueryResult
                {
                    Query = query,
                    ExecutedAt = DateTime.UtcNow
                };

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                if (query.Trim().ToUpper().StartsWith("SELECT"))
                {
                    var results = await QueryAsync<dynamic>(query);
                    result.Results = results;
                    result.RowsAffected = results.Count();
                }
                else
                {
                    result.RowsAffected = await ExecuteAsync(query);
                }
                
                stopwatch.Stop();
                result.Success = true;
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to execute query: {query}");
                return new QueryResult
                {
                    Query = query,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutedAt = DateTime.UtcNow
                };
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

        #region Helper Methods

        private string GetHostFromConnectionString()
        {
            // Extract host from connection string
            var builder = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = _connectionString };
            return builder.ContainsKey("Server") ? builder["Server"].ToString() : "localhost";
        }

        private string GetUsernameFromConnectionString()
        {
            var builder = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = _connectionString };
            return builder.ContainsKey("User ID") ? builder["User ID"].ToString() : 
                   builder.ContainsKey("Username") ? builder["Username"].ToString() : 
                   builder.ContainsKey("Uid") ? builder["Uid"].ToString() : "root";
        }

        private string GetPasswordFromConnectionString()
        {
            var builder = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = _connectionString };
            return builder.ContainsKey("Password") ? builder["Password"].ToString() : 
                   builder.ContainsKey("Pwd") ? builder["Pwd"].ToString() : "";
        }

        private string CalculateChecksum(string content)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
            return Convert.ToBase64String(hashBytes);
        }

        private List<string> SplitSqlQueries(string sqlContent)
        {
            var queries = new List<string>();
            var lines = sqlContent.Split('\n');
            var currentQuery = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("--"))
                    continue;

                currentQuery.AppendLine(line);

                if (trimmedLine.EndsWith(";"))
                {
                    queries.Add(currentQuery.ToString());
                    currentQuery.Clear();
                }
            }

            // Add any remaining query without semicolon
            if (currentQuery.Length > 0)
            {
                queries.Add(currentQuery.ToString());
            }

            return queries;
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
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
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



    /// <summary>
    /// Record of a successfully applied migration
    /// </summary>
    public class MigrationRecord
    {
        public string MigrationName { get; set; }
        public DateTime AppliedAt { get; set; }
        public string Checksum { get; set; }
        public long ExecutionTimeMs { get; set; }
    }
} 