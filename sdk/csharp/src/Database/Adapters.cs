using System;
using System.Collections.Generic;

namespace TuskLang.Database
{
    /// <summary>
    /// Database adapters configuration for TuskTsk
    /// </summary>
    public class Adapters
    {
        /// <summary>
        /// Available database adapters
        /// </summary>
        public List<DatabaseAdapter> AvailableAdapters { get; set; } = new List<DatabaseAdapter>();

        /// <summary>
        /// Default adapter
        /// </summary>
        public string DefaultAdapter { get; set; } = "sqlite";

        /// <summary>
        /// Adapter configurations
        /// </summary>
        public Dictionary<string, DatabaseAdapterConfig> Configurations { get; set; } = new Dictionary<string, DatabaseAdapterConfig>();

        /// <summary>
        /// Connection pooling settings
        /// </summary>
        public ConnectionPoolingSettings ConnectionPooling { get; set; } = new ConnectionPoolingSettings();
    }

    /// <summary>
    /// Database adapter information
    /// </summary>
    public class DatabaseAdapter
    {
        /// <summary>
        /// Adapter name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Adapter type
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Adapter version
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Whether the adapter is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Adapter capabilities
        /// </summary>
        public List<string> Capabilities { get; set; } = new List<string>();
    }

    /// <summary>
    /// Database adapter configuration
    /// </summary>
    public class DatabaseAdapterConfig
    {
        /// <summary>
        /// Connection string
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Command timeout
        /// </summary>
        public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Whether to enable connection pooling
        /// </summary>
        public bool EnableConnectionPooling { get; set; } = true;

        /// <summary>
        /// Additional parameters
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Connection pooling settings
    /// </summary>
    public class ConnectionPoolingSettings
    {
        /// <summary>
        /// Minimum pool size
        /// </summary>
        public int MinPoolSize { get; set; } = 1;

        /// <summary>
        /// Maximum pool size
        /// </summary>
        public int MaxPoolSize { get; set; } = 100;

        /// <summary>
        /// Connection lifetime
        /// </summary>
        public TimeSpan ConnectionLifetime { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Pool timeout
        /// </summary>
        public TimeSpan PoolTimeout { get; set; } = TimeSpan.FromSeconds(15);
    }
} 