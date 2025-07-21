using System;
using System.Data;
using System.Threading.Tasks;

namespace TuskLang.Core.Connection
{
    /// <summary>
    /// Interface for transaction managers that handle database transactions
    /// </summary>
    public interface ITransactionManager
    {
        /// <summary>
        /// Execute an operation within a transaction
        /// </summary>
        Task<T> ExecuteInTransactionAsync<T>(
            IManagedConnection connection,
            Func<IManagedConnection, IDbTransaction, Task<T>> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);

        /// <summary>
        /// Begin a new transaction
        /// </summary>
        Task<IDbTransaction> BeginTransactionAsync(
            IManagedConnection connection,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
    }

    /// <summary>
    /// Default transaction manager implementation
    /// </summary>
    public class DefaultTransactionManager : ITransactionManager
    {
        public async Task<T> ExecuteInTransactionAsync<T>(
            IManagedConnection connection,
            Func<IManagedConnection, IDbTransaction, Task<T>> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            using var transaction = await BeginTransactionAsync(connection, isolationLevel);
            
            try
            {
                var result = await operation(connection, transaction);
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IDbTransaction> BeginTransactionAsync(
            IManagedConnection connection,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            return await connection.BeginTransactionAsync(isolationLevel);
        }
    }
} 