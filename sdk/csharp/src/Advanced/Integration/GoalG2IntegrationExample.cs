using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace TuskLang
{
    /// <summary>
    /// Integration example demonstrating all three goal g2 implementations working together
    /// Shows DatabaseIntegration, LoggingObservability, and ConfigurationManagement in action
    /// </summary>
    public class GoalG2IntegrationExample
    {
        private readonly DatabaseIntegration _database;
        private readonly LoggingObservability _logging;
        private readonly ConfigurationManagement _config;
        private readonly TSK _tsk;

        public GoalG2IntegrationExample()
        {
            _database = new DatabaseIntegration();
            _logging = new LoggingObservability();
            _config = new ConfigurationManagement();
            _tsk = new TSK();
        }

        /// <summary>
        /// Execute a comprehensive operation using all three systems
        /// </summary>
        public async Task<G2IntegrationResult> ExecuteComprehensiveOperation(
            Dictionary<string, object> inputs,
            string operationName = "comprehensive_operation")
        {
            var result = new G2IntegrationResult();
            var startTime = DateTime.UtcNow;

            try
            {
                // Step 1: Load and validate configuration
                await _config.LoadConfigurationAsync();
                var dbProvider = _config.GetValue<string>("database:provider", "sqlite");
                var logLevel = _config.GetValue<string>("logging:level", "Info");

                _logging.LogInfo($"Starting comprehensive operation: {operationName}", new Dictionary<string, object>
                {
                    ["operation"] = operationName,
                    ["db_provider"] = dbProvider,
                    ["log_level"] = logLevel
                });

                // Step 2: Start tracing span
                using (var span = _logging.StartSpan(operationName, new Dictionary<string, object>
                {
                    ["inputs"] = inputs.Count,
                    ["db_provider"] = dbProvider
                }))
                {
                    // Step 3: Execute database operations with logging and metrics
                    var dbResult = await ExecuteDatabaseOperations(dbProvider, inputs);
                    result.DatabaseResults = dbResult;

                    // Step 4: Process FUJSEN operations if present
                    if (inputs.ContainsKey("fujsen_code"))
                    {
                        var fujsenResult = await ExecuteFujsenWithLogging(
                            inputs["fujsen_code"].ToString(),
                            inputs);
                        result.FujsenResults = fujsenResult;
                    }

                    // Step 5: Record metrics
                    _logging.RecordMetric("operation_duration", (DateTime.UtcNow - startTime).TotalMilliseconds);
                    _logging.IncrementCounter("operations_completed", new Dictionary<string, string>
                    {
                        ["operation_type"] = operationName
                    });

                    span.SetStatus(SpanStatus.Ok);
                }

                result.Success = true;
                result.ExecutionTime = DateTime.UtcNow - startTime;

                // Step 6: Collect metrics from all systems
                result.Metrics = new G2IntegrationMetrics
                {
                    DatabaseMetrics = _database.GetMetrics(),
                    LoggingMetrics = _logging.GetMetrics(),
                    ConfigurationMetrics = _config.GetMetrics()
                };

                _logging.LogInfo($"Comprehensive operation completed successfully", new Dictionary<string, object>
                {
                    ["operation"] = operationName,
                    ["duration_ms"] = result.ExecutionTime.TotalMilliseconds,
                    ["success"] = true
                });

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Operation failed: {ex.Message}");
                result.ExecutionTime = DateTime.UtcNow - startTime;

                _logging.LogError($"Comprehensive operation failed", new Dictionary<string, object>
                {
                    ["operation"] = operationName,
                    ["error"] = ex.Message
                }, ex);

                return result;
            }
        }

        /// <summary>
        /// Execute database operations with comprehensive logging and metrics
        /// </summary>
        private async Task<List<DatabaseOperationResult>> ExecuteDatabaseOperations(
            string dbProvider,
            Dictionary<string, object> inputs)
        {
            var results = new List<DatabaseOperationResult>();

            // Execute queries if present
            if (inputs.ContainsKey("queries"))
            {
                var queries = inputs["queries"] as List<Dictionary<string, object>>;
                if (queries != null)
                {
                    foreach (var query in queries)
                    {
                        var queryText = query["sql"].ToString();
                        var parameters = query.ContainsKey("parameters") 
                            ? query["parameters"] as Dictionary<string, object> 
                            : null;

                        _logging.LogDebug($"Executing database query", new Dictionary<string, object>
                        {
                            ["provider"] = dbProvider,
                            ["query"] = queryText,
                            ["parameters"] = parameters?.Count ?? 0
                        });

                        try
                        {
                            var dbResult = await _database.ExecuteQueryAsync<Dictionary<string, object>>(
                                dbProvider, queryText, parameters);

                            results.Add(new DatabaseOperationResult
                            {
                                OperationType = "Query",
                                Success = true,
                                RowCount = dbResult.RowCount,
                                ExecutionTime = dbResult.ExecutionTime
                            });

                            _logging.LogInfo($"Database query executed successfully", new Dictionary<string, object>
                            {
                                ["provider"] = dbProvider,
                                ["rows_returned"] = dbResult.RowCount,
                                ["execution_time_ms"] = dbResult.ExecutionTime.TotalMilliseconds
                            });
                        }
                        catch (Exception ex)
                        {
                            results.Add(new DatabaseOperationResult
                            {
                                OperationType = "Query",
                                Success = false,
                                ErrorMessage = ex.Message
                            });

                            _logging.LogError($"Database query failed", new Dictionary<string, object>
                            {
                                ["provider"] = dbProvider,
                                ["query"] = queryText
                            }, ex);
                        }
                    }
                }
            }

            // Execute commands if present
            if (inputs.ContainsKey("commands"))
            {
                var commands = inputs["commands"] as List<Dictionary<string, object>>;
                if (commands != null)
                {
                    foreach (var command in commands)
                    {
                        var commandText = command["sql"].ToString();
                        var parameters = command.ContainsKey("parameters") 
                            ? command["parameters"] as Dictionary<string, object> 
                            : null;

                        _logging.LogDebug($"Executing database command", new Dictionary<string, object>
                        {
                            ["provider"] = dbProvider,
                            ["command"] = commandText
                        });

                        try
                        {
                            var dbResult = await _database.ExecuteCommandAsync(
                                dbProvider, commandText, parameters);

                            results.Add(new DatabaseOperationResult
                            {
                                OperationType = "Command",
                                Success = true,
                                RowsAffected = dbResult.RowsAffected,
                                ExecutionTime = dbResult.ExecutionTime
                            });

                            _logging.LogInfo($"Database command executed successfully", new Dictionary<string, object>
                            {
                                ["provider"] = dbProvider,
                                ["rows_affected"] = dbResult.RowsAffected,
                                ["execution_time_ms"] = dbResult.ExecutionTime.TotalMilliseconds
                            });
                        }
                        catch (Exception ex)
                        {
                            results.Add(new DatabaseOperationResult
                            {
                                OperationType = "Command",
                                Success = false,
                                ErrorMessage = ex.Message
                            });

                            _logging.LogError($"Database command failed", new Dictionary<string, object>
                            {
                                ["provider"] = dbProvider,
                                ["command"] = commandText
                            }, ex);
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Execute FUJSEN with comprehensive logging and validation
        /// </summary>
        private async Task<FujsenOperationResult> ExecuteFujsenWithLogging(
            string fujsenCode,
            Dictionary<string, object> context)
        {
            _logging.LogInfo($"Executing FUJSEN code", new Dictionary<string, object>
            {
                ["code_length"] = fujsenCode.Length,
                ["context_keys"] = context.Count
            });

            try
            {
                // Set up TSK with the FUJSEN code
                _tsk.SetSection("fujsen_section", new Dictionary<string, object>
                {
                    ["fujsen"] = fujsenCode
                });

                // Execute with context injection
                var result = await _tsk.ExecuteFujsenWithContext("fujsen_section", "fujsen", context);

                _logging.LogInfo($"FUJSEN execution completed successfully", new Dictionary<string, object>
                {
                    ["result_type"] = result?.GetType().Name ?? "null"
                });

                return new FujsenOperationResult
                {
                    Success = true,
                    Result = result,
                    ExecutionTime = TimeSpan.Zero // Would be measured in real implementation
                };
            }
            catch (Exception ex)
            {
                _logging.LogError($"FUJSEN execution failed", new Dictionary<string, object>
                {
                    ["code_length"] = fujsenCode.Length
                }, ex);

                return new FujsenOperationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Get comprehensive system health report
        /// </summary>
        public async Task<G2SystemHealthReport> GetSystemHealthReport()
        {
            var dbMetrics = _database.GetMetrics();
            var loggingMetrics = _logging.GetMetrics();
            var configMetrics = _config.GetMetrics();

            return new G2SystemHealthReport
            {
                Timestamp = DateTime.UtcNow,
                DatabaseMetrics = dbMetrics,
                LoggingMetrics = loggingMetrics,
                ConfigurationMetrics = configMetrics,
                OverallHealth = CalculateG2OverallHealth(dbMetrics, loggingMetrics, configMetrics)
            };
        }

        /// <summary>
        /// Execute batch operations with all systems integrated
        /// </summary>
        public async Task<List<G2IntegrationResult>> ExecuteBatchOperations(
            List<Dictionary<string, object>> inputsList)
        {
            var tasks = inputsList.Select(inputs => ExecuteComprehensiveOperation(inputs)).ToList();
            return await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Get configuration summary
        /// </summary>
        public async Task<ConfigurationSummary> GetConfigurationSummary()
        {
            await _config.LoadConfigurationAsync();
            
            return new ConfigurationSummary
            {
                AllKeys = _config.GetAllKeys(),
                DatabaseProvider = _config.GetValue<string>("database:provider", "unknown"),
                LogLevel = _config.GetValue<string>("logging:level", "Info"),
                ConfigurationJson = _config.ExportToJson()
            };
        }

        private G2SystemHealth CalculateG2OverallHealth(
            DatabaseMetrics dbMetrics,
            LoggingMetrics loggingMetrics,
            ConfigurationMetrics configMetrics)
        {
            // Calculate health based on various metrics
            var dbHealth = dbMetrics.CacheHits > 0 ? 1.0 : 0.5;
            var loggingHealth = loggingMetrics.GetLogCounts().GetValueOrDefault(LogLevel.Error, 0) == 0 ? 1.0 : 0.7;
            var configHealth = configMetrics.TotalLoads > 0 ? 1.0 : 0.5;

            var overallHealth = (dbHealth + loggingHealth + configHealth) / 3.0;

            if (overallHealth > 0.9)
                return G2SystemHealth.Excellent;
            else if (overallHealth > 0.7)
                return G2SystemHealth.Good;
            else if (overallHealth > 0.5)
                return G2SystemHealth.Fair;
            else
                return G2SystemHealth.Poor;
        }
    }

    public class G2IntegrationResult
    {
        public bool Success { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public TimeSpan ExecutionTime { get; set; }
        public List<DatabaseOperationResult> DatabaseResults { get; set; } = new List<DatabaseOperationResult>();
        public FujsenOperationResult FujsenResults { get; set; }
        public G2IntegrationMetrics Metrics { get; set; }
    }

    public class DatabaseOperationResult
    {
        public string OperationType { get; set; }
        public bool Success { get; set; }
        public int RowCount { get; set; }
        public int RowsAffected { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class FujsenOperationResult
    {
        public bool Success { get; set; }
        public object Result { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class G2IntegrationMetrics
    {
        public DatabaseMetrics DatabaseMetrics { get; set; }
        public LoggingMetrics LoggingMetrics { get; set; }
        public ConfigurationMetrics ConfigurationMetrics { get; set; }
    }

    public class G2SystemHealthReport
    {
        public DateTime Timestamp { get; set; }
        public DatabaseMetrics DatabaseMetrics { get; set; }
        public LoggingMetrics LoggingMetrics { get; set; }
        public ConfigurationMetrics ConfigurationMetrics { get; set; }
        public G2SystemHealth OverallHealth { get; set; }
    }

    public class ConfigurationSummary
    {
        public List<string> AllKeys { get; set; }
        public string DatabaseProvider { get; set; }
        public string LogLevel { get; set; }
        public string ConfigurationJson { get; set; }
    }

    public enum G2SystemHealth
    {
        Poor,
        Fair,
        Good,
        Excellent
    }
} 