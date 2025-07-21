using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TuskLang.Database
{
    /// <summary>
    /// Interface for database adapters in TuskTsk
    /// </summary>
    public interface IDatabaseAdapter : IDisposable
    {
        /// <summary>
        /// Database connection string
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        /// Database type
        /// </summary>
        string DatabaseType { get; }

        /// <summary>
        /// Whether the adapter is connected
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Connect to the database
        /// </summary>
        Task ConnectAsync();

        /// <summary>
        /// Disconnect from the database
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Execute a query and return results
        /// </summary>
        Task<IEnumerable<dynamic>> QueryAsync(string sql, object parameters = null);

        /// <summary>
        /// Execute a command and return the number of affected rows
        /// </summary>
        Task<int> ExecuteAsync(string sql, object parameters = null);

        /// <summary>
        /// Execute a query and return a single result
        /// </summary>
        Task<dynamic> QuerySingleAsync(string sql, object parameters = null);

        /// <summary>
        /// Execute a query and return the first result
        /// </summary>
        Task<dynamic> QueryFirstAsync(string sql, object parameters = null);

        /// <summary>
        /// Begin a transaction
        /// </summary>
        Task<IDatabaseTransaction> BeginTransactionAsync();

        /// <summary>
        /// Test the database connection
        /// </summary>
        Task<bool> TestConnectionAsync();

        /// <summary>
        /// Get database schema information
        /// </summary>
        Task<DatabaseSchema> GetSchemaAsync();

        /// <summary>
        /// Execute a stored procedure
        /// </summary>
        Task<IEnumerable<dynamic>> ExecuteStoredProcedureAsync(string procedureName, object parameters = null);

        /// <summary>
        /// Create a database backup
        /// </summary>
        Task<bool> CreateBackupAsync(string backupPath);

        /// <summary>
        /// Restore a database from backup
        /// </summary>
        Task<bool> RestoreBackupAsync(string backupPath);

        /// <summary>
        /// Run database migrations
        /// </summary>
        Task<bool> RunMigrationsAsync();

        /// <summary>
        /// Get database status information
        /// </summary>
        Task<DatabaseStatus> GetStatusAsync();

        /// <summary>
        /// Check database health
        /// </summary>
        Task<DatabaseHealth> CheckHealthAsync();

        /// <summary>
        /// Execute a SQL query file
        /// </summary>
        Task<QueryFileResult> ExecuteFileAsync(string filePath);

        /// <summary>
        /// Execute a raw SQL query
        /// </summary>
        Task<QueryResult> ExecuteQueryAsync(string query);
    }

    /// <summary>
    /// Database transaction interface
    /// </summary>
    public interface IDatabaseTransaction : IDisposable
    {
        /// <summary>
        /// Commit the transaction
        /// </summary>
        Task CommitAsync();

        /// <summary>
        /// Rollback the transaction
        /// </summary>
        Task RollbackAsync();
    }

    /// <summary>
    /// Database schema information
    /// </summary>
    public class DatabaseSchema
    {
        /// <summary>
        /// Database name
        /// </summary>
        public string DatabaseName { get; set; } = string.Empty;

        /// <summary>
        /// Database version
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Tables in the database
        /// </summary>
        public List<TableInfo> Tables { get; set; } = new List<TableInfo>();

        /// <summary>
        /// Views in the database
        /// </summary>
        public List<ViewInfo> Views { get; set; } = new List<ViewInfo>();

        /// <summary>
        /// Stored procedures in the database
        /// </summary>
        public List<StoredProcedureInfo> StoredProcedures { get; set; } = new List<StoredProcedureInfo>();
    }

    /// <summary>
    /// Table information
    /// </summary>
    public class TableInfo
    {
        /// <summary>
        /// Table name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Table schema
        /// </summary>
        public string Schema { get; set; } = string.Empty;

        /// <summary>
        /// Columns in the table
        /// </summary>
        public List<ColumnInfo> Columns { get; set; } = new List<ColumnInfo>();

        /// <summary>
        /// Primary key columns
        /// </summary>
        public List<string> PrimaryKeyColumns { get; set; } = new List<string>();

        /// <summary>
        /// Foreign key relationships
        /// </summary>
        public List<ForeignKeyInfo> ForeignKeys { get; set; } = new List<ForeignKeyInfo>();
    }

    /// <summary>
    /// Column information
    /// </summary>
    public class ColumnInfo
    {
        /// <summary>
        /// Column name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Column data type
        /// </summary>
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// Whether the column is nullable
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Whether the column is a primary key
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// Whether the column is an identity column
        /// </summary>
        public bool IsIdentity { get; set; }

        /// <summary>
        /// Default value for the column
        /// </summary>
        public string DefaultValue { get; set; } = string.Empty;

        /// <summary>
        /// Maximum length for the column
        /// </summary>
        public int? MaxLength { get; set; }
    }

    /// <summary>
    /// View information
    /// </summary>
    public class ViewInfo
    {
        /// <summary>
        /// View name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// View schema
        /// </summary>
        public string Schema { get; set; } = string.Empty;

        /// <summary>
        /// View definition
        /// </summary>
        public string Definition { get; set; } = string.Empty;
    }

    /// <summary>
    /// Stored procedure information
    /// </summary>
    public class StoredProcedureInfo
    {
        /// <summary>
        /// Stored procedure name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Stored procedure schema
        /// </summary>
        public string Schema { get; set; } = string.Empty;

        /// <summary>
        /// Parameters for the stored procedure
        /// </summary>
        public List<ParameterInfo> Parameters { get; set; } = new List<ParameterInfo>();
    }

    /// <summary>
    /// Parameter information
    /// </summary>
    public class ParameterInfo
    {
        /// <summary>
        /// Parameter name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Parameter data type
        /// </summary>
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// Whether the parameter is an output parameter
        /// </summary>
        public bool IsOutput { get; set; }

        /// <summary>
        /// Whether the parameter is nullable
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Default value for the parameter
        /// </summary>
        public string DefaultValue { get; set; } = string.Empty;
    }

    /// <summary>
    /// Foreign key information
    /// </summary>
    public class ForeignKeyInfo
    {
        /// <summary>
        /// Foreign key name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Referenced table
        /// </summary>
        public string ReferencedTable { get; set; } = string.Empty;

        /// <summary>
        /// Referenced schema
        /// </summary>
        public string ReferencedSchema { get; set; } = string.Empty;

        /// <summary>
        /// Column mappings
        /// </summary>
        public Dictionary<string, string> ColumnMappings { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Database status information
    /// </summary>
    public class DatabaseStatus
    {
        /// <summary>
        /// Database name
        /// </summary>
        public string DatabaseName { get; set; } = string.Empty;

        /// <summary>
        /// Database version
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Connection state
        /// </summary>
        public string ConnectionState { get; set; } = string.Empty;

        /// <summary>
        /// Number of active connections
        /// </summary>
        public int ActiveConnections { get; set; }

        /// <summary>
        /// Database size in bytes
        /// </summary>
        public long DatabaseSize { get; set; }

        /// <summary>
        /// Available space in bytes
        /// </summary>
        public long AvailableSpace { get; set; }

        /// <summary>
        /// Last backup date
        /// </summary>
        public DateTime? LastBackupDate { get; set; }

        /// <summary>
        /// Uptime in seconds
        /// </summary>
        public long UptimeSeconds { get; set; }

        /// <summary>
        /// Number of tables
        /// </summary>
        public int TableCount { get; set; }

        /// <summary>
        /// Number of stored procedures
        /// </summary>
        public int StoredProcedureCount { get; set; }

        /// <summary>
        /// Number of views
        /// </summary>
        public int ViewCount { get; set; }

        /// <summary>
        /// Database size in MB (computed property)
        /// </summary>
        public long Size => DatabaseSize / (1024 * 1024);

        /// <summary>
        /// Uptime as TimeSpan (computed property)
        /// </summary>
        public TimeSpan Uptime => TimeSpan.FromSeconds(UptimeSeconds);

        /// <summary>
        /// Last check time
        /// </summary>
        public DateTime LastCheck { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Database health information
    /// </summary>
    public class DatabaseHealth
    {
        /// <summary>
        /// Whether the database is healthy
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Health status message
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Response time in milliseconds
        /// </summary>
        public long ResponseTimeMs { get; set; }

        /// <summary>
        /// Connection pool usage percentage
        /// </summary>
        public double ConnectionPoolUsage { get; set; }

        /// <summary>
        /// CPU usage percentage
        /// </summary>
        public double CpuUsage { get; set; }

        /// <summary>
        /// Memory usage percentage
        /// </summary>
        public double MemoryUsage { get; set; }

        /// <summary>
        /// Disk usage percentage
        /// </summary>
        public double DiskUsage { get; set; }

        /// <summary>
        /// Number of slow queries
        /// </summary>
        public int SlowQueryCount { get; set; }

        /// <summary>
        /// Number of failed queries
        /// </summary>
        public int FailedQueryCount { get; set; }

        /// <summary>
        /// Last health check time
        /// </summary>
        public DateTime LastCheck { get; set; }

        /// <summary>
        /// List of errors
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// List of warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Response time as TimeSpan (computed property)
        /// </summary>
        public TimeSpan ResponseTime => TimeSpan.FromMilliseconds(ResponseTimeMs);

        /// <summary>
        /// Error message (computed property)
        /// </summary>
        public string ErrorMessage => Errors.Count > 0 ? string.Join("; ", Errors) : string.Empty;
    }

    /// <summary>
    /// Query execution result
    /// </summary>
    public class QueryResult
    {
        /// <summary>
        /// Whether the query was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Number of rows affected
        /// </summary>
        public int RowsAffected { get; set; }

        /// <summary>
        /// Execution time in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Query results
        /// </summary>
        public IEnumerable<dynamic> Results { get; set; } = new List<dynamic>();

        /// <summary>
        /// Error message if query failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// The SQL query that was executed
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// When the query was executed
        /// </summary>
        public DateTime ExecutedAt { get; set; }

        /// <summary>
        /// Query data (alias for Results)
        /// </summary>
        public IEnumerable<dynamic> Data => Results;

        /// <summary>
        /// Whether the query was successful (alias for Success)
        /// </summary>
        public bool IsSuccess => Success;
    }

    /// <summary>
    /// SQL file execution result
    /// </summary>
    public class QueryFileResult
    {
        /// <summary>
        /// Whether the file execution was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// File path that was executed
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Number of queries executed
        /// </summary>
        public int QueriesExecuted { get; set; }

        /// <summary>
        /// Total execution time in milliseconds
        /// </summary>
        public long TotalExecutionTimeMs { get; set; }

        /// <summary>
        /// Results from each query
        /// </summary>
        public List<QueryResult> QueryResults { get; set; } = new List<QueryResult>();

        /// <summary>
        /// Error message if file execution failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when file was executed
        /// </summary>
        public DateTime ExecutedAt { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSizeBytes { get; set; }
    }
} 