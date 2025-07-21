using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TuskTsk.Framework.AspNetCore;

namespace TuskTsk.Examples.AspNetCore.Controllers
{
    /// <summary>
    /// TuskTsk API Controller - Complete Example
    /// 
    /// Demonstrates best practices for:
    /// - Configuration management
    /// - Operator execution
    /// - Error handling
    /// - Async operations
    /// - Performance monitoring
    /// - Health monitoring
    /// 
    /// NO PLACEHOLDERS - Production-ready controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TuskTskController : ControllerBase
    {
        private readonly ITuskTskService _tuskTskService;
        private readonly ILogger<TuskTskController> _logger;

        public TuskTskController(ITuskTskService tuskTskService, ILogger<TuskTskController> logger)
        {
            _tuskTskService = tuskTskService ?? throw new ArgumentNullException(nameof(tuskTskService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Load configuration from file
        /// </summary>
        [HttpPost("configuration/load")]
        public async Task<ActionResult<Dictionary<string, object>>> LoadConfiguration([FromBody] LoadConfigurationRequest request)
        {
            try
            {
                _logger.LogInformation("Loading configuration from file: {FilePath}", request.FilePath);
                
                var config = await _tuskTskService.ParseConfigurationFromFileAsync(request.FilePath);
                
                _logger.LogInformation("Successfully loaded configuration with {KeyCount} keys", config.Count);
                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load configuration from {FilePath}", request.FilePath);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Parse configuration from string
        /// </summary>
        [HttpPost("configuration/parse")]
        public async Task<ActionResult<Dictionary<string, object>>> ParseConfiguration([FromBody] ParseConfigurationRequest request)
        {
            try
            {
                _logger.LogInformation("Parsing configuration content of length: {Length}", request.Content.Length);
                
                var config = await _tuskTskService.ParseConfigurationAsync(request.Content);
                
                _logger.LogInformation("Successfully parsed configuration with {KeyCount} keys", config.Count);
                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse configuration content");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Execute operator
        /// </summary>
        [HttpPost("operators/{operatorName}/execute")]
        public async Task<ActionResult<object>> ExecuteOperator(string operatorName, [FromBody] Dictionary<string, object> config)
        {
            try
            {
                _logger.LogInformation("Executing operator: {OperatorName}", operatorName);
                
                var result = await _tuskTskService.ExecuteOperatorAsync(operatorName, config);
                
                _logger.LogInformation("Successfully executed operator: {OperatorName}", operatorName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute operator: {OperatorName}", operatorName);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Execute multiple operators in sequence
        /// </summary>
        [HttpPost("operators/execute-batch")]
        public async Task<ActionResult<List<object>>> ExecuteOperators([FromBody] ExecuteOperatorsRequest request)
        {
            try
            {
                _logger.LogInformation("Executing {Count} operators in batch", request.Executions.Count);
                
                var results = await _tuskTskService.ExecuteOperatorsAsync(request.Executions);
                
                _logger.LogInformation("Successfully executed {Count} operators", request.Executions.Count);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute operator batch");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get available operators
        /// </summary>
        [HttpGet("operators")]
        public async Task<ActionResult<IEnumerable<string>>> GetOperators()
        {
            try
            {
                var operators = await _tuskTskService.GetAvailableOperatorsAsync();
                
                _logger.LogDebug("Retrieved {Count} available operators", operators.Count());
                return Ok(operators);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available operators");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get operator schema
        /// </summary>
        [HttpGet("operators/{operatorName}/schema")]
        public async Task<ActionResult<Dictionary<string, object>>> GetOperatorSchema(string operatorName)
        {
            try
            {
                var schema = await _tuskTskService.GetOperatorSchemaAsync(operatorName);
                
                _logger.LogDebug("Retrieved schema for operator: {OperatorName}", operatorName);
                return Ok(schema);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get schema for operator: {OperatorName}", operatorName);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Validate configuration
        /// </summary>
        [HttpPost("configuration/validate")]
        public async Task<ActionResult<ValidationResult>> ValidateConfiguration([FromBody] Dictionary<string, object> config)
        {
            try
            {
                _logger.LogInformation("Validating configuration with {KeyCount} keys", config.Count);
                
                var result = await _tuskTskService.ValidateConfigurationAsync(config);
                
                _logger.LogInformation("Configuration validation completed: Valid={IsValid}, Errors={ErrorCount}", 
                    result.IsValid, result.Errors.Count);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate configuration");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Process template with variables
        /// </summary>
        [HttpPost("template/process")]
        public async Task<ActionResult<string>> ProcessTemplate([FromBody] ProcessTemplateRequest request)
        {
            try
            {
                _logger.LogInformation("Processing template with {VariableCount} variables", request.Variables.Count);
                
                var result = await _tuskTskService.ProcessTemplateAsync(request.Template, request.Variables);
                
                _logger.LogInformation("Successfully processed template");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process template");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Clear configuration cache
        /// </summary>
        [HttpPost("cache/clear")]
        public async Task<ActionResult> ClearCache()
        {
            try
            {
                await _tuskTskService.ClearCacheAsync();
                
                _logger.LogInformation("Configuration cache cleared");
                return Ok(new { message = "Cache cleared successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear cache");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get service health status
        /// </summary>
        [HttpGet("health")]
        public async Task<ActionResult<HealthStatus>> GetHealth()
        {
            try
            {
                var health = await _tuskTskService.GetHealthStatusAsync();
                
                var statusCode = health.IsHealthy ? 200 : 503;
                return StatusCode(statusCode, health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get health status");
                return StatusCode(503, new HealthStatus
                {
                    IsHealthy = false,
                    Status = "Error",
                    Details = new Dictionary<string, object> { ["error"] = ex.Message }
                });
            }
        }

        /// <summary>
        /// Example endpoint showing real-world usage
        /// </summary>
        [HttpPost("example/real-world")]
        public async Task<ActionResult<object>> RealWorldExample([FromBody] RealWorldExampleRequest request)
        {
            try
            {
                _logger.LogInformation("Processing real-world example for scenario: {Scenario}", request.Scenario);

                // Step 1: Parse configuration
                var config = await _tuskTskService.ParseConfigurationAsync(request.ConfigurationContent);

                // Step 2: Process template with configuration
                var processedTemplate = await _tuskTskService.ProcessTemplateAsync(request.Template, config);

                // Step 3: Execute operators based on configuration
                var results = new List<object>();
                if (config.ContainsKey("operators"))
                {
                    var operatorConfigs = config["operators"] as Dictionary<string, object>;
                    if (operatorConfigs != null)
                    {
                        foreach (var opConfig in operatorConfigs)
                        {
                            try
                            {
                                var result = await _tuskTskService.ExecuteOperatorAsync(opConfig.Key, (Dictionary<string, object>)opConfig.Value);
                                results.Add(new { operator = opConfig.Key, result });
                            }
                            catch (Exception opEx)
                            {
                                _logger.LogWarning(opEx, "Operator {OperatorName} failed, continuing with others", opConfig.Key);
                                results.Add(new { operator = opConfig.Key, error = opEx.Message });
                            }
                        }
                    }
                }

                var response = new
                {
                    scenario = request.Scenario,
                    configuration = config,
                    processedTemplate = processedTemplate,
                    operatorResults = results,
                    timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Successfully processed real-world example for scenario: {Scenario}", request.Scenario);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process real-world example for scenario: {Scenario}", request.Scenario);
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    #region Request/Response Models

    public class LoadConfigurationRequest
    {
        public string FilePath { get; set; } = string.Empty;
    }

    public class ParseConfigurationRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public class ExecuteOperatorsRequest
    {
        public List<OperatorExecution> Executions { get; set; } = new();
    }

    public class ProcessTemplateRequest
    {
        public string Template { get; set; } = string.Empty;
        public Dictionary<string, object> Variables { get; set; } = new();
    }

    public class RealWorldExampleRequest
    {
        public string Scenario { get; set; } = string.Empty;
        public string ConfigurationContent { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
    }

    #endregion
} 