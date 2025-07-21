using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TuskTsk.Framework.AspNetCore
{
    /// <summary>
    /// Main TuskTsk service interface for ASP.NET Core integration
    /// 
    /// Provides comprehensive TuskTsk functionality through dependency injection:
    /// - Configuration parsing and management
    /// - Operator execution
    /// - Template processing
    /// - Async operations with cancellation support
    /// - Error handling and recovery
    /// 
    /// NO PLACEHOLDERS - Production ready interface
    /// </summary>
    public interface ITuskTskService
    {
        /// <summary>
        /// Parse configuration from string
        /// </summary>
        Task<Dictionary<string, object>> ParseConfigurationAsync(string content, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Parse configuration from file
        /// </summary>
        Task<Dictionary<string, object>> ParseConfigurationFromFileAsync(string filePath, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Execute operator with configuration
        /// </summary>
        Task<object> ExecuteOperatorAsync(string operatorName, Dictionary<string, object> config, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Execute multiple operators in sequence
        /// </summary>
        Task<List<object>> ExecuteOperatorsAsync(IEnumerable<OperatorExecution> executions, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Process template with variables
        /// </summary>
        Task<string> ProcessTemplateAsync(string template, Dictionary<string, object> variables, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Validate configuration
        /// </summary>
        Task<ValidationResult> ValidateConfigurationAsync(Dictionary<string, object> config, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Get available operators
        /// </summary>
        Task<IEnumerable<string>> GetAvailableOperatorsAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Get operator schema
        /// </summary>
        Task<Dictionary<string, object>> GetOperatorSchemaAsync(string operatorName, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Load configuration with caching
        /// </summary>
        Task<Dictionary<string, object>> LoadCachedConfigurationAsync(string key, Func<Task<Dictionary<string, object>>> factory, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Clear configuration cache
        /// </summary>
        Task ClearCacheAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Get service health status
        /// </summary>
        Task<HealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// Operator execution definition
    /// </summary>
    public class OperatorExecution
    {
        public string OperatorName { get; set; } = string.Empty;
        public Dictionary<string, object> Configuration { get; set; } = new();
        public Dictionary<string, object> Context { get; set; } = new();
        public int TimeoutSeconds { get; set; } = 30;
    }
    
    /// <summary>
    /// Validation result
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
    
    /// <summary>
    /// Health status
    /// </summary>
    public class HealthStatus
    {
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = "Unknown";
        public Dictionary<string, object> Details { get; set; } = new();
        public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    }
} 