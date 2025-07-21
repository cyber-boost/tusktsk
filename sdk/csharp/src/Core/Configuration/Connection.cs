using System;
using System.Collections.Generic;

namespace TuskLang.Configuration
{
    /// <summary>
    /// Connection configuration for TuskTsk
    /// </summary>
    public class Connection
    {
        /// <summary>
        /// Connection name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Connection type
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Connection string or configuration
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Additional connection parameters
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Whether the connection is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Connection timeout
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Maximum retry attempts
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Retry delay
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    }
} 