using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace TuskLang
{
    /// <summary>
    /// Advanced integration example demonstrating all three goal implementations
    /// Shows ErrorHandling, PerformanceOptimization, and SecurityValidation working together
    /// </summary>
    public class AdvancedIntegrationExample
    {
        private readonly ErrorHandling _errorHandling;
        private readonly PerformanceOptimization _performanceOptimization;
        private readonly SecurityValidation _securityValidation;
        private readonly TSK _tsk;

        public AdvancedIntegrationExample()
        {
            _errorHandling = new ErrorHandling();
            _performanceOptimization = new PerformanceOptimization();
            _securityValidation = new SecurityValidation();
            _tsk = new TSK();

            // Configure custom error handlers
            _errorHandling.AddErrorHandler(new DatabaseErrorHandler());
            _errorHandling.AddErrorHandler(new NetworkErrorHandler());
        }

        /// <summary>
        /// Execute a complex operation with all three systems integrated
        /// </summary>
        public async Task<IntegrationResult> ExecuteComplexOperation(
            Dictionary<string, object> inputs,
            string operationName = "complex_operation")
        {
            var result = new IntegrationResult();
            var startTime = DateTime.UtcNow;

            try
            {
                // Step 1: Security validation and sanitization
                var securityContext = new SecurityContext
                {
                    UserId = inputs.GetValueOrDefault("user_id")?.ToString(),
                    IsAuthenticated = true,
                    RequiredLevel = SecurityLevel.High
                };

                var validationResult = await _securityValidation.ValidateAndSanitize(inputs, securityContext);
                if (!validationResult.IsValid)
                {
                    result.Success = false;
                    result.Errors.AddRange(validationResult.Errors);
                    return result;
                }

                // Step 2: Execute with all three systems integrated
                var operationResult = await _errorHandling.ExecuteWithErrorHandling(async () =>
                {
                    return await _performanceOptimization.ExecuteWithOptimization(async () =>
                    {
                        return await _securityValidation.ExecuteWithSecurity(async () =>
                        {
                            // Simulate complex operation
                            await Task.Delay(100); // Simulate work
                            
                            // Process the sanitized inputs
                            var processedData = await ProcessData(validationResult.SanitizedData);
                            
                            // Execute FUJSEN if present
                            if (validationResult.SanitizedData.ContainsKey("fujsen_code"))
                            {
                                var fujsenResult = await ExecuteFujsenWithValidation(
                                    validationResult.SanitizedData["fujsen_code"].ToString(),
                                    validationResult.SanitizedData);
                                processedData["fujsen_result"] = fujsenResult;
                            }

                            return processedData;
                        }, validationResult.SanitizedData, operationName, securityContext);
                    }, operationName, validationResult.SanitizedData);
                }, operationName, validationResult.SanitizedData);

                result.Success = true;
                result.Data = operationResult;
                result.ExecutionTime = DateTime.UtcNow - startTime;

                // Step 3: Collect metrics from all systems
                result.Metrics = new IntegrationMetrics
                {
                    ErrorMetrics = _errorHandling.GetMetrics(),
                    PerformanceMetrics = _performanceOptimization.GetMetrics(),
                    SecurityMetrics = _securityValidation.GetMetrics(operationName),
                    OptimizationRecommendations = _performanceOptimization.GetRecommendations()
                };

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Operation failed: {ex.Message}");
                result.ExecutionTime = DateTime.UtcNow - startTime;
                return result;
            }
        }

        /// <summary>
        /// Execute batch operations with integrated systems
        /// </summary>
        public async Task<List<IntegrationResult>> ExecuteBatchOperations(
            List<Dictionary<string, object>> inputsList)
        {
            var operations = inputsList.Select(inputs => 
                new Func<Task<IntegrationResult>>(() => ExecuteComplexOperation(inputs))).ToList();

            return await _performanceOptimization.ExecuteBatch(operations, maxConcurrency: 3);
        }

        /// <summary>
        /// Execute FUJSEN with comprehensive validation
        /// </summary>
        private async Task<object> ExecuteFujsenWithValidation(
            string fujsenCode,
            Dictionary<string, object> context)
        {
            // Validate FUJSEN code for security threats
            var securityContext = new SecurityContext { RequiredLevel = SecurityLevel.High };
            var scanResult = await _securityValidation.ScanForThreats("fujsen_execution", 
                new Dictionary<string, object> { ["code"] = fujsenCode }, securityContext);

            if (scanResult.ThreatLevel > ThreatLevel.Low)
            {
                throw new SecurityThreatException($"FUJSEN code contains security threats: {scanResult.Description}");
            }

            // Execute FUJSEN with error handling and performance optimization
            return await _errorHandling.ExecuteWithErrorHandling(async () =>
            {
                return await _performanceOptimization.ExecuteWithOptimization(async () =>
                {
                    // Set up TSK with the FUJSEN code
                    _tsk.SetSection("fujsen_section", new Dictionary<string, object>
                    {
                        ["fujsen"] = fujsenCode
                    });

                    // Execute with context injection
                    return await _tsk.ExecuteFujsenWithContext("fujsen_section", "fujsen", context);
                }, "fujsen_execution", context);
            }, "fujsen_execution", context);
        }

        /// <summary>
        /// Process data with validation
        /// </summary>
        private async Task<Dictionary<string, object>> ProcessData(Dictionary<string, object> data)
        {
            var result = new Dictionary<string, object>();

            foreach (var item in data)
            {
                if (item.Key.StartsWith("process_"))
                {
                    var processedValue = await ProcessValue(item.Value);
                    result[item.Key.Replace("process_", "processed_")] = processedValue;
                }
                else
                {
                    result[item.Key] = item.Value;
                }
            }

            return result;
        }

        /// <summary>
        /// Process individual values
        /// </summary>
        private async Task<object> ProcessValue(object value)
        {
            // Simulate processing
            await Task.Delay(10);
            
            if (value is string stringValue)
            {
                return stringValue.ToUpper();
            }
            else if (value is int intValue)
            {
                return intValue * 2;
            }
            
            return value;
        }

        /// <summary>
        /// Get comprehensive system health report
        /// </summary>
        public async Task<SystemHealthReport> GetSystemHealthReport()
        {
            var errorMetrics = _errorHandling.GetMetrics();
            var performanceMetrics = _performanceOptimization.GetMetrics();
            var securityMetrics = _securityValidation.GetMetrics();
            var recommendations = _performanceOptimization.GetRecommendations();

            return new SystemHealthReport
            {
                Timestamp = DateTime.UtcNow,
                ErrorMetrics = errorMetrics,
                PerformanceMetrics = performanceMetrics,
                SecurityMetrics = securityMetrics,
                OptimizationRecommendations = recommendations,
                OverallHealth = CalculateOverallHealth(errorMetrics, performanceMetrics, securityMetrics)
            };
        }

        private SystemHealth CalculateOverallHealth(
            ErrorMetrics errorMetrics,
            PerformanceMetrics performanceMetrics,
            SecurityMetrics securityMetrics)
        {
            // Calculate health based on various metrics
            var errorRate = errorMetrics.GetSuccessRate("overall");
            var securityThreats = securityMetrics.HighThreatOperations + securityMetrics.MediumThreatOperations;
            var totalOperations = securityMetrics.TotalOperations;

            if (errorRate > 0.95 && securityThreats == 0 && totalOperations > 0)
            {
                return SystemHealth.Excellent;
            }
            else if (errorRate > 0.90 && securityThreats < totalOperations * 0.01)
            {
                return SystemHealth.Good;
            }
            else if (errorRate > 0.80)
            {
                return SystemHealth.Fair;
            }
            else
            {
                return SystemHealth.Poor;
            }
        }
    }

    /// <summary>
    /// Custom error handler for database operations
    /// </summary>
    public class DatabaseErrorHandler : ErrorHandler
    {
        public override bool CanHandle(Exception exception)
        {
            return exception.Message.Contains("database") || 
                   exception.Message.Contains("connection") ||
                   exception.Message.Contains("timeout");
        }

        public override async Task<T?> HandleAsync<T>(Exception exception, Dictionary<string, object> context)
        {
            // Implement database-specific error handling
            await Task.Delay(100); // Simulate recovery time
            
            // Return default value for the type
            return default(T);
        }
    }

    /// <summary>
    /// Custom error handler for network operations
    /// </summary>
    public class NetworkErrorHandler : ErrorHandler
    {
        public override bool CanHandle(Exception exception)
        {
            return exception.Message.Contains("network") || 
                   exception.Message.Contains("http") ||
                   exception.Message.Contains("connection");
        }

        public override async Task<T?> HandleAsync<T>(Exception exception, Dictionary<string, object> context)
        {
            // Implement network-specific error handling
            await Task.Delay(200); // Simulate retry delay
            
            // Return default value for the type
            return default(T);
        }
    }

    public class IntegrationResult
    {
        public bool Success { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public TimeSpan ExecutionTime { get; set; }
        public IntegrationMetrics Metrics { get; set; }
    }

    public class IntegrationMetrics
    {
        public ErrorMetrics ErrorMetrics { get; set; }
        public PerformanceMetrics PerformanceMetrics { get; set; }
        public SecurityMetrics SecurityMetrics { get; set; }
        public List<OptimizationRecommendation> OptimizationRecommendations { get; set; }
    }

    public class SystemHealthReport
    {
        public DateTime Timestamp { get; set; }
        public ErrorMetrics ErrorMetrics { get; set; }
        public PerformanceMetrics PerformanceMetrics { get; set; }
        public SecurityMetrics SecurityMetrics { get; set; }
        public List<OptimizationRecommendation> OptimizationRecommendations { get; set; }
        public SystemHealth OverallHealth { get; set; }
    }

    public enum SystemHealth
    {
        Poor,
        Fair,
        Good,
        Excellent
    }
} 