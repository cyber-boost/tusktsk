using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TuskLang.Core.Connection
{
    public class ManagedConnection : IManagedConnection
    {
        private readonly IDbConnection _connection;
        private readonly IConnectionPool _pool;
        private readonly string _poolName;
        private readonly PerformanceMetrics _metrics;
        private readonly ILogger _logger;
        private bool _disposed = false;

        public ManagedConnection(IDbConnection connection, IConnectionPool pool, string poolName, 
            PerformanceMetrics metrics, ILogger logger)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _poolName = poolName ?? throw new ArgumentNullException(nameof(poolName));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _logger = logger;
        }

        public IDbConnection Connection => _connection;

        public string PoolName => _poolName;

        public bool IsInTransaction => _connection.State == ConnectionState.Open && 
                                     _connection.BeginTransaction() != null;

        public void EnlistInTransaction()
        {
            // Implementation for transaction enlistment
            _logger?.LogDebug($"Enlisting connection from pool '{_poolName}' in transaction");
        }

        public async Task<IDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (_connection.State != ConnectionState.Open)
            {
                await Task.Run(() => _connection.Open());
            }

            var transaction = _connection.BeginTransaction(isolationLevel);
            _metrics?.RecordTransactionStarted();
            _logger?.LogDebug($"Started transaction on connection from pool '{_poolName}'");
            
            return transaction;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _pool?.ReturnConnection(_connection);
                _disposed = true;
                _logger?.LogDebug($"Returned connection to pool '{_poolName}'");
            }
        }
    }
} 