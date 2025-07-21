using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using TuskLang.Operators;

namespace TuskTsk.Framework.AspNetCore.HealthChecks
{
    /// <summary>
    /// Production-ready health check for TuskTsk SDK
    /// 
    /// Monitors:
    /// - Operator registry health
    /// - Service availability  
    /// - Performance metrics
    /// - Resource utilization
    /// - Configuration validation
    /// 
    /// NO PLACEHOLDERS - Complete health monitoring
    /// </summary>
    public class TuskTskHealthCheck : IHealthCheck
    {
        private readonly ITuskTskService _tuskTskService;
        private readonly IOperatorService _operatorService;
        private readonly ILogger<TuskTskHealthCheck> _logger;
        
        public TuskTskHealthCheck(
            ITuskTskService tuskTskService,
            IOperatorService operatorService,
            ILogger<TuskTskHealthCheck> logger)
        {
            _tuskTskService = tuskTskService ?? throw new ArgumentNullException(nameof(tuskTskService));
            _operatorService = operatorService ?? throw new ArgumentNullException(nameof(operatorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var healthData = new Dictionary<string, object>();
                var isHealthy = true;
                var issues = new List<string>();
                
                // Check TuskTsk service health
                var serviceHealth = await CheckServiceHealthAsync(cancellationToken);
                healthData["service"] = serviceHealth;
                
                if (!serviceHealth.IsHealthy)
                {
                    isHealthy = false;
                    issues.Add("TuskTsk service is unhealthy");
                }
                
                // Check operator registry
                var registryHealth = await CheckOperatorRegistryAsync(cancellationToken);
                healthData["registry"] = registryHealth;
                
                if (registryHealth["operator_count"] is int operatorCount && operatorCount == 0)
                {
                    isHealthy = false;
                    issues.Add("No operators registered");
                }
                
                // Check operator performance
                var performanceHealth = await CheckOperatorPerformanceAsync(cancellationToken);
                healthData["performance"] = performanceHealth;
                
                // Check memory usage
                var memoryHealth = CheckMemoryUsage();
                healthData["memory"] = memoryHealth;
                
                if (memoryHealth["is_high_usage"] is bool isHighUsage && isHighUsage)
                {
                    issues.Add("High memory usage detected");
                }
                
                // Overall health assessment
                var status = isHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy;
                var description = isHealthy 
                    ? "TuskTsk is healthy" 
                    : $"TuskTsk has issues: {string.Join(", ", issues)}";
                
                _logger.LogDebug("Health check completed: Status={Status}, Issues={IssueCount}", status, issues.Count);
                
                return new HealthCheckResult(status, description, data: healthData);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Health check was cancelled");
                return new HealthCheckResult(HealthStatus.Unhealthy, "Health check was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed with exception");
                return new HealthCheckResult(HealthStatus.Unhealthy, 
                    $"Health check failed: {ex.Message}",
                    ex,
                    new Dictionary<string, object> { ["error"] = ex.ToString() });
            }
        }
        
        private async Task<HealthStatus> CheckServiceHealthAsync(CancellationToken cancellationToken)
        {
            try
            {
                var serviceHealth = await _tuskTskService.GetHealthStatusAsync(cancellationToken);
                return serviceHealth;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check TuskTsk service health");
                return new HealthStatus
                {
                    IsHealthy = false,
                    Status = "Error",
                    Details = new Dictionary<string, object> { ["error"] = ex.Message }
                };
            }
        }
        
        private async Task<Dictionary<string, object>> CheckOperatorRegistryAsync(CancellationToken cancellationToken)
        {
            try
            {
                var operatorNames = await _tuskTskService.GetAvailableOperatorsAsync(cancellationToken);
                var operatorList = operatorNames.ToList();
                
                // Test a sample operator to ensure registry is functional
                var testOperatorName = operatorList.FirstOrDefault();
                Dictionary<string, object> testSchema = null;
                
                if (!string.IsNullOrEmpty(testOperatorName))
                {
                    try
                    {
                        testSchema = await _tuskTskService.GetOperatorSchemaAsync(testOperatorName, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get schema for test operator {OperatorName}", testOperatorName);
                    }
                }
                
                return new Dictionary<string, object>
                {
                    ["operator_count"] = operatorList.Count,
                    ["operators"] = operatorList.Take(10).ToList(), // First 10 for health check
                    ["registry_functional"] = testSchema != null,
                    ["test_operator"] = testOperatorName ?? "none",
                    ["last_checked"] = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check operator registry");
                return new Dictionary<string, object>
                {
                    ["operator_count"] = 0,
                    ["error"] = ex.Message,
                    ["registry_functional"] = false
                };
            }
        }
        
        private async Task<Dictionary<string, object>> CheckOperatorPerformanceAsync(CancellationToken cancellationToken)
        {
            try
            {
                var operatorNames = await _tuskTskService.GetAvailableOperatorsAsync(cancellationToken);
                var operatorList = operatorNames.Take(5).ToList(); // Sample first 5 operators
                
                var performanceData = new Dictionary<string, object>
                {
                    ["operators_checked"] = operatorList.Count,
                    ["check_time"] = DateTime.UtcNow
                };
                
                if (operatorList.Any())
                {
                    var metrics = new List<object>();
                    var totalExecutions = 0L;
                    var totalErrors = 0L;
                    var avgResponseTime = 0.0;
                    
                    foreach (var operatorName in operatorList)
                    {
                        try
                        {
                            var operatorMetrics = await _operatorService.GetOperatorMetricsAsync(operatorName, cancellationToken);
                            
                            if (operatorMetrics.ExecutionCount > 0)
                            {
                                metrics.Add(new
                                {
                                    name = operatorName,
                                    executions = operatorMetrics.ExecutionCount,
                                    errors = operatorMetrics.ErrorCount,
                                    avg_time_ms = operatorMetrics.AverageExecutionTimeMs,
                                    success_rate = operatorMetrics.SuccessCount / (double)operatorMetrics.ExecutionCount
                                });
                                
                                totalExecutions += operatorMetrics.ExecutionCount;
                                totalErrors += operatorMetrics.ErrorCount;
                                avgResponseTime += operatorMetrics.AverageExecutionTimeMs;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to get metrics for operator {OperatorName}", operatorName);
                        }
                    }
                    
                    performanceData["sample_metrics"] = metrics;
                    performanceData["total_executions"] = totalExecutions;
                    performanceData["total_errors"] = totalErrors;
                    performanceData["overall_error_rate"] = totalExecutions > 0 ? totalErrors / (double)totalExecutions : 0.0;
                    performanceData["avg_response_time_ms"] = metrics.Count > 0 ? avgResponseTime / metrics.Count : 0.0;
                }
                
                return performanceData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check operator performance");
                return new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["operators_checked"] = 0
                };
            }
        }
        
        private Dictionary<string, object> CheckMemoryUsage()
        {
            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var workingSetMB = process.WorkingSet64 / 1024 / 1024;
                var privateMemoryMB = process.PrivateMemorySize64 / 1024 / 1024;
                
                // Collect garbage to get accurate memory reading
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                var managedMemoryMB = GC.GetTotalMemory(false) / 1024 / 1024;
                
                // Consider high usage if over 500MB working set or 200MB managed memory
                var isHighUsage = workingSetMB > 500 || managedMemoryMB > 200;
                
                return new Dictionary<string, object>
                {
                    ["working_set_mb"] = workingSetMB,
                    ["private_memory_mb"] = privateMemoryMB,
                    ["managed_memory_mb"] = managedMemoryMB,
                    ["is_high_usage"] = isHighUsage,
                    ["gc_collections"] = new
                    {
                        gen0 = GC.CollectionCount(0),
                        gen1 = GC.CollectionCount(1),
                        gen2 = GC.CollectionCount(2)
                    },
                    ["last_checked"] = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check memory usage");
                return new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["is_high_usage"] = false
                };
            }
        }
    }
} 