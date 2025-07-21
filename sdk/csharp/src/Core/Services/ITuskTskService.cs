using System;
using System.Threading.Tasks;
using TuskLang.Core;
using TuskLang.Parser;
using System.Collections.Generic;

namespace TuskTsk.Services
{
    /// <summary>
    /// Main service interface for TuskTsk operations
    /// </summary>
    public interface ITuskTskService
    {
        /// <summary>
        /// Parse TuskTsk configuration
        /// </summary>
        Task<ParserResult> ParseConfigurationAsync(string content, string sourceFile = "");

        /// <summary>
        /// Validate TuskTsk configuration
        /// </summary>
        Task<TuskLang.Core.ValidationResult> ValidateConfigurationAsync(string content, string sourceFile = "");

        /// <summary>
        /// Compile TuskTsk configuration
        /// </summary>
        Task<CompilationResult> CompileConfigurationAsync(string content, string sourceFile = "");

        /// <summary>
        /// Execute TuskTsk configuration
        /// </summary>
        Task<ExecutionResult> ExecuteConfigurationAsync(string content, string sourceFile = "");

        /// <summary>
        /// Get service health status
        /// </summary>
        Task<HealthStatus> GetHealthAsync();

        /// <summary>
        /// Get service statistics
        /// </summary>
        Task<ServiceStatistics> GetStatisticsAsync();
    }

    /// <summary>
    /// Compilation result
    /// </summary>
    public class CompilationResult
    {
        public bool IsSuccess { get; set; }
        public string Output { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public TimeSpan ProcessingTime { get; set; }
    }

    /// <summary>
    /// Execution result
    /// </summary>
    public class ExecutionResult
    {
        public bool IsSuccess { get; set; }
        public object Result { get; set; } = new object();
        public List<string> Errors { get; set; } = new List<string>();
        public TimeSpan ProcessingTime { get; set; }
    }

    /// <summary>
    /// Health status
    /// </summary>
    public class HealthStatus
    {
        public string Status { get; set; } = "Healthy";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Service statistics
    /// </summary>
    public class ServiceStatistics
    {
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public TimeSpan AverageProcessingTime { get; set; }
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
    }
} 