using System;
using System.Collections.Generic;

namespace TuskLang.Configuration
{
    /// <summary>
    /// Peanuts configuration for TuskTsk
    /// </summary>
    public class Peanuts
    {
        /// <summary>
        /// Peanuts server URL
        /// </summary>
        public string ServerUrl { get; set; } = "https://peanuts.tuskt.sk";

        /// <summary>
        /// API key for authentication
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Whether to enable Peanuts integration
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Request timeout
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

        /// <summary>
        /// Additional configuration parameters
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
} 