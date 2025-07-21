using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;
using System.Data.SQLite;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace TuskLang.Database
{
    /// <summary>
    /// Production-ready connection management and transaction system
    /// Provides pooling, ACID compliance, and performance optimization
    /// </summary>
    public class ConnectionManagementSystem : IDisposable
    {
        private readonly ILogger<ConnectionManagementSystem> _logger;
        private readonly ConcurrentDictionary<string, IConnectionPool> _connectionPools;
        private readonly ConcurrentDictionary<string, TransactionManager> _transactionManagers;
        private readonly ConnectionManagerOptions _options;
        private readonly Timer _healthCheckTimer;
        private readonly PerformanceMetrics _metrics;
        private bool _disposed = false;

        public ConnectionManagementSystem(IOptions<ConnectionManagerOptions> options = null, 
            ILogger<ConnectionManagementSystem> logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ConnectionManagementSystem>.Instance;
            _options = options?.Value ?? new ConnectionManagerOptions();
            _connectionPools = new ConcurrentDictionary<string, IConnectionPool>();
            _transactionManagers = new ConcurrentDictionary<string, TransactionManager>();
            _metrics = new PerformanceMetrics();

            // Start health check timer
            _healthCheckTimer = new Timer(PerformHealthCheck, null, 
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            _logger.LogInformation("Connection management system initialized");
        }

        #region Pool Management

        /// <summary>
        /// Register a connection pool for a database provider
        /// </summary>
        public void RegisterPool(string name, string connectionString, DatabaseProvider provider, 
            ConnectionPoolOptions poolOptions = null)
        {
            var options = poolOptions ?? new ConnectionPoolOptions();
            var pool = CreateConnectionPool(connectionString, provider, options);
            
            _connectionPools[name] = pool;
            _transactionManagers[name] = new TransactionManager(_logger);

            _logger.LogInformation($"Registered connection pool '{name}' for {provider} provider");
        }

        /// <summary>
        /// Create connection pool based on provider type
        /// </summary>
        private IConnectionPool CreateConnectionPool(string connectionString, DatabaseProvider provider, 
            ConnectionPoolOptions options)
        {
            return provider switch
            {
                DatabaseProvider.SqlServer => new SqlServerConnectionPool(connectionString, options, _logger),
                DatabaseProvider.PostgreSQL => new PostgreSqlConnectionPool(connectionString, options, _logger),
                DatabaseProvider.MySQL => new MySqlConnectionPool(connectionString, options, _logger),
                DatabaseProvider.SQLite => new SqliteConnectionPool(connectionString, options, _logger),
                _ => throw new ArgumentException($"Unsupported database provider: {provider}")
            };
        }

        #endregion

        #region Connection Management

        /// <summary>
        /// Get connection from pool with automatic management
        /// </summary>
        public async Task<IManagedConnection> GetConnectionAsync(string poolName, 
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (!_connectionPools.TryGetValue(poolName, out var pool))
                {
                    throw new InvalidOperationException($"Connection pool '{poolName}' not found");
                }

                var connection = await pool.GetConnectionAsync(cancellationToken);
                stopwatch.Stop();
                
                _metrics.RecordConnectionAcquisition(stopwatch.Elapsed);
                _logger.LogDebug($"Acquired connection from pool '{poolName}' in {stopwatch.ElapsedMilliseconds}ms");
                
                return new ManagedConnection(connection, pool, poolName, _metrics, _logger);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metrics.RecordConnectionError();
                _logger.LogError(ex, $"Failed to acquire connection from pool '{poolName}'");
                throw;
            }
        }

        /// <summary>
        /// Execute operation with managed connection
        /// </summary>
        public async Task<T> ExecuteWithConnectionAsync<T>(string poolName, 
            Func<IManagedConnection, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            using var connection = await GetConnectionAsync(poolName, cancellationToken);
            return await operation(connection);
        }

        /// <summary>
        /// Execute operation with managed connection (no return value)
        /// </summary>
        public async Task ExecuteWithConnectionAsync(string poolName, 
            Func<IManagedConnection, Task> operation, CancellationToken cancellationToken = default)
        {
            using var connection = await GetConnectionAsync(poolName, cancellationToken);
            await operation(connection);
        }

        #endregion

        #region Transaction Management

        /// <summary>
        /// Execute operation within transaction
        /// </summary>
        public async Task<T> ExecuteInTransactionAsync<T>(string poolName, 
            Func<IManagedConnection, IDbTransaction, Task<T>> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
        {
            using var connection = await GetConnectionAsync(poolName, cancellationToken);
            
            if (!_transactionManagers.TryGetValue(poolName, out var transactionManager))
            {
                throw new InvalidOperationException($"Transaction manager for pool '{poolName}' not found");
            }

            return await transactionManager.ExecuteInTransactionAsync(connection, operation, isolationLevel);
        }

        /// <summary>
        /// Execute operation within distributed transaction
        /// </summary>
        public async Task<T> ExecuteInDistributedTransactionAsync<T>(
            Dictionary<string, Func<IManagedConnection, Task>> operations,
            Func<Task<T>> finalOperation,
            TransactionScopeOption scopeOption = TransactionScopeOption.Required,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            TimeSpan timeout = default)
        {
            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = isolationLevel,
                Timeout = timeout == default ? TimeSpan.FromMinutes(10) : timeout
            };

            using var scope = new TransactionScope(scopeOption, transactionOptions, 
                TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                // Execute all database operations
                var tasks = new List<Task>();
                foreach (var kvp in operations)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        using var connection = await GetConnectionAsync(kvp.Key);
                        connection.EnlistInTransaction();
                        await kvp.Value(connection);
                    }));
                }

                await Task.WhenAll(tasks);

                // Execute final operation
                var result = await finalOperation();

                // Complete the transaction
                scope.Complete();
                
                _logger.LogDebug("Distributed transaction completed successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Distributed transaction failed, rolling back");
                throw;
            }
        }

        /// <summary>
        /// Begin manual transaction
        /// </summary>
        public async Task<IManagedTransaction> BeginTransactionAsync(string poolName,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
        {
            var connection = await GetConnectionAsync(poolName, cancellationToken);
            
            if (!_transactionManagers.TryGetValue(poolName, out var transactionManager))
            {
                throw new InvalidOperationException($"Transaction manager for pool '{poolName}' not found");
            }

            var transaction = await transactionManager.BeginTransactionAsync(connection, isolationLevel);
            return new ManagedTransaction(transaction, connection, transactionManager, _logger);
        }

        #endregion

        #region Health Monitoring

        /// <summary>
        /// Perform health check on all connection pools
        /// </summary>
        private async void PerformHealthCheck(object state)
        {
            try
            {
                var tasks = new List<Task>();
                
                foreach (var kvp in _connectionPools)
                {
                    tasks.Add(CheckPoolHealthAsync(kvp.Key, kvp.Value));
                }

                await Task.WhenAll(tasks);
                _logger.LogDebug("Health check completed for all connection pools");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health check");
            }
        }

        /// <summary>
        /// Check individual pool health
        /// </summary>
        private async Task CheckPoolHealthAsync(string poolName, IConnectionPool pool)
        {
            try
            {
                var healthStatus = await pool.CheckHealthAsync();
                
                if (!healthStatus.IsHealthy)
                {
                    _logger.LogWarning($"Pool '{poolName}' is unhealthy: {healthStatus.Message}");
                    
                    // Attempt to recover the pool
                    await pool.RecoverAsync();
                }
                else
                {
                    _logger.LogDebug($"Pool '{poolName}' is healthy - Active: {healthStatus.ActiveConnections}, Available: {healthStatus.AvailableConnections}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Health check failed for pool '{poolName}'");
            }
        }

        /// <summary>
        /// Get comprehensive health status
        /// </summary>
        public async Task<SystemHealthStatus> GetSystemHealthAsync()
        {
            var status = new SystemHealthStatus
            {
                OverallHealth = true,
                CheckTime = DateTime.UtcNow,
                PoolStatuses = new Dictionary<string, PoolHealthStatus>()
            };

            var tasks = new List<Task<(string Name, PoolHealthStatus Status)>>();

            foreach (var kvp in _connectionPools)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var health = await kvp.Value.CheckHealthAsync();
                        return (kvp.Key, health);
                    }
                    catch (Exception ex)
                    {
                        return (kvp.Key, new PoolHealthStatus
                        {
                            IsHealthy = false,
                            Message = ex.Message,
                            ActiveConnections = 0,
                            AvailableConnections = 0
                        });
                    }
                }));
            }

            var results = await Task.WhenAll(tasks);

            foreach (var (name, poolStatus) in results)
            {
                status.PoolStatuses[name] = poolStatus;
                if (!poolStatus.IsHealthy)
                {
                    status.OverallHealth = false;
                }
            }

            return status;
        }

        #endregion

        #region Performance Metrics

        /// <summary>
        /// Get performance metrics for all pools
        /// </summary>
        public PerformanceMetrics GetPerformanceMetrics()
        {
            return _metrics;
        }

        /// <summary>
        /// Get detailed pool statistics
        /// </summary>
        public Dictionary<string, PoolStatistics> GetPoolStatistics()
        {
            var statistics = new Dictionary<string, PoolStatistics>();

            foreach (var kvp in _connectionPools)
            {
                statistics[kvp.Key] = kvp.Value.GetStatistics();
            }

            return statistics;
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _healthCheckTimer?.Dispose();

                // Dispose all connection pools
                foreach (var pool in _connectionPools.Values)
                {
                    pool.Dispose();
                }

                // Dispose all transaction managers
                foreach (var manager in _transactionManagers.Values)
                {
                    manager.Dispose();
                }

                _connectionPools.Clear();
                _transactionManagers.Clear();
                _disposed = true;

                _logger.LogInformation("Connection management system disposed");
            }
        }
    }

    #region Supporting Classes

    /// <summary>
    /// Database provider types
    /// </summary>
    public enum DatabaseProvider
    {
        SqlServer,
        PostgreSQL,
        MySQL,
        SQLite
    }

    /// <summary>
    /// Connection manager options
    /// </summary>
    public class ConnectionManagerOptions
    {
        public int DefaultPoolSize { get; set; } = 20;
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(60);
        public bool EnableHealthChecks { get; set; } = true;
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Connection pool options
    /// </summary>
    public class ConnectionPoolOptions
    {
        public int MinPoolSize { get; set; } = 5;
        public int MaxPoolSize { get; set; } = 100;
        public TimeSpan ConnectionLifetime { get; set; } = TimeSpan.FromMinutes(30);
        public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public bool ValidateConnections { get; set; } = true;
        public int RetryAttempts { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    }

    /// <summary>
    /// Performance metrics tracking
    /// </summary>
    public class PerformanceMetrics
    {
        private long _connectionsCreated = 0;
        private long _connectionsDestroyed = 0;
        private long _connectionErrors = 0;
        private long _transactionsStarted = 0;
        private long _transactionsCommitted = 0;
        private long _transactionsRolledBack = 0;
        private readonly List<TimeSpan> _connectionAcquisitionTimes = new List<TimeSpan>();
        private readonly object _lock = new object();

        public long ConnectionsCreated => _connectionsCreated;
        public long ConnectionsDestroyed => _connectionsDestroyed;
        public long ConnectionErrors => _connectionErrors;
        public long ActiveConnections => _connectionsCreated - _connectionsDestroyed;
        public long TransactionsStarted => _transactionsStarted;
        public long TransactionsCommitted => _transactionsCommitted;
        public long TransactionsRolledBack => _transactionsRolledBack;

        public TimeSpan AverageConnectionAcquisitionTime
        {
            get
            {
                lock (_lock)
                {
                    return _connectionAcquisitionTimes.Count > 0
                        ? TimeSpan.FromTicks(_connectionAcquisitionTimes.Sum(t => t.Ticks) / _connectionAcquisitionTimes.Count)
                        : TimeSpan.Zero;
                }
            }
        }

        public void RecordConnectionCreated() => Interlocked.Increment(ref _connectionsCreated);
        public void RecordConnectionDestroyed() => Interlocked.Increment(ref _connectionsDestroyed);
        public void RecordConnectionError() => Interlocked.Increment(ref _connectionErrors);
        public void RecordTransactionStarted() => Interlocked.Increment(ref _transactionsStarted);
        public void RecordTransactionCommitted() => Interlocked.Increment(ref _transactionsCommitted);
        public void RecordTransactionRolledBack() => Interlocked.Increment(ref _transactionsRolledBack);

        public void RecordConnectionAcquisition(TimeSpan time)
        {
            lock (_lock)
            {
                _connectionAcquisitionTimes.Add(time);
                if (_connectionAcquisitionTimes.Count > 1000)
                {
                    _connectionAcquisitionTimes.RemoveAt(0);
                }
            }
        }
    }

    /// <summary>
    /// Pool health status
    /// </summary>
    public class PoolHealthStatus
    {
        public bool IsHealthy { get; set; }
        public string Message { get; set; }
        public int ActiveConnections { get; set; }
        public int AvailableConnections { get; set; }
        public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// System health status
    /// </summary>
    public class SystemHealthStatus
    {
        public bool OverallHealth { get; set; }
        public DateTime CheckTime { get; set; }
        public Dictionary<string, PoolHealthStatus> PoolStatuses { get; set; }
    }

    /// <summary>
    /// Pool statistics
    /// </summary>
    public class PoolStatistics
    {
        public int TotalConnections { get; set; }
        public int ActiveConnections { get; set; }
        public int AvailableConnections { get; set; }
        public int FailedConnections { get; set; }
        public TimeSpan AverageConnectionTime { get; set; }
        public DateTime LastActivity { get; set; }
    }

    #endregion

    #region Interfaces

    /// <summary>
    /// Connection pool interface
    /// </summary>
    public interface IConnectionPool : IDisposable
    {
        Task<IDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default);
        void ReturnConnection(IDbConnection connection);
        Task<PoolHealthStatus> CheckHealthAsync();
        Task RecoverAsync();
        PoolStatistics GetStatistics();
    }

    /// <summary>
    /// Managed connection interface
    /// </summary>
    public interface IManagedConnection : IDisposable
    {
        IDbConnection Connection { get; }
        string PoolName { get; }
        bool IsInTransaction { get; }
        void EnlistInTransaction();
        Task<IDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
    }

    /// <summary>
    /// Managed transaction interface
    /// </summary>
    public interface IManagedTransaction : IDisposable
    {
        IDbTransaction Transaction { get; }
        IManagedConnection Connection { get; }
        bool IsActive { get; }
        Task CommitAsync();
        Task RollbackAsync();
    }

    #endregion
} 