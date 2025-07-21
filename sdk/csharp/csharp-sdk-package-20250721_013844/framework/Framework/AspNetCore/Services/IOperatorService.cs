using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TuskTsk.Framework.AspNetCore
{
    /// <summary>
    /// Operator service interface for ASP.NET Core integration
    /// 
    /// Provides async operator execution with:
    /// - Cancellation token support
    /// - Performance monitoring
    /// - Error handling
    /// - Resource management
    /// </summary>
    public interface IOperatorService
    {
        /// <summary>
        /// Execute operator asynchronously
        /// </summary>
        Task<object> ExecuteOperatorAsync(string operatorName, Dictionary<string, object> config, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Execute operator asynchronously with context
        /// </summary>
        Task<object> ExecuteOperatorAsync(string operatorName, Dictionary<string, object> config, Dictionary<string, object> context, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Validate operator configuration
        /// </summary>
        Task<ValidationResult> ValidateOperatorAsync(string operatorName, Dictionary<string, object> config, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Get operator performance metrics
        /// </summary>
        Task<OperatorMetrics> GetOperatorMetricsAsync(string operatorName, CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// Operator performance metrics
    /// </summary>
    public class OperatorMetrics
    {
        public string OperatorName { get; set; } = string.Empty;
        public long ExecutionCount { get; set; }
        public double AverageExecutionTimeMs { get; set; }
        public double LastExecutionTimeMs { get; set; }
        public long SuccessCount { get; set; }
        public long ErrorCount { get; set; }
        public DateTime LastExecuted { get; set; }
        public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
    }
}

/// <summary>
/// Production-ready operator service implementation
/// </summary>
public class OperatorService : IOperatorService
{
    private readonly ILogger<OperatorService> _logger;
    private readonly TuskTskOptions _options;
    private readonly ConcurrentDictionary<string, OperatorMetrics> _metrics;
    
    public OperatorService(ILogger<OperatorService> logger, IOptions<TuskTskOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _metrics = new ConcurrentDictionary<string, OperatorMetrics>();
    }
    
    public async Task<object> ExecuteOperatorAsync(string operatorName, Dictionary<string, object> config, CancellationToken cancellationToken = default)
    {
        return await ExecuteOperatorAsync(operatorName, config, new Dictionary<string, object>(), cancellationToken);
    }
    
    public async Task<object> ExecuteOperatorAsync(string operatorName, Dictionary<string, object> config, Dictionary<string, object> context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var metrics = _metrics.GetOrAdd(operatorName, _ => new OperatorMetrics { OperatorName = operatorName });
        
        try
        {
            _logger.LogDebug("Executing operator {OperatorName}", operatorName);
            
            var result = await OperatorRegistry.ExecuteOperatorAsync(operatorName, config, context);
            
            stopwatch.Stop();
            UpdateMetrics(metrics, stopwatch.ElapsedMilliseconds, true);
            
            _logger.LogDebug("Successfully executed operator {OperatorName} in {ElapsedMs}ms", 
                operatorName, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            UpdateMetrics(metrics, stopwatch.ElapsedMilliseconds, false);
            
            _logger.LogError(ex, "Failed to execute operator {OperatorName}", operatorName);
            throw;
        }
    }
    
    public async Task<ValidationResult> ValidateOperatorAsync(string operatorName, Dictionary<string, object> config, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await Task.Run(() => OperatorRegistry.ValidateOperator(operatorName, config), cancellationToken);
            return new ValidationResult
            {
                IsValid = result.IsValid,
                Errors = result.Errors,
                Warnings = result.Warnings
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate operator {OperatorName}", operatorName);
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<string> { ex.Message }
            };
        }
    }
    
    public async Task<OperatorMetrics> GetOperatorMetricsAsync(string operatorName, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_metrics.GetOrAdd(operatorName, _ => new OperatorMetrics { OperatorName = operatorName }));
    }
    
    private void UpdateMetrics(OperatorMetrics metrics, long elapsedMs, bool success)
    {
        lock (metrics)
        {
            metrics.ExecutionCount++;
            metrics.LastExecutionTimeMs = elapsedMs;
            metrics.LastExecuted = DateTime.UtcNow;
            
            if (success)
            {
                metrics.SuccessCount++;
            }
            else
            {
                metrics.ErrorCount++;
            }
            
            // Calculate running average
            metrics.AverageExecutionTimeMs = (metrics.AverageExecutionTimeMs * (metrics.ExecutionCount - 1) + elapsedMs) / metrics.ExecutionCount;
        }
    }
} 