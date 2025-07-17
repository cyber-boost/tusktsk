# Deployment Strategies in C# TuskLang

## Overview

Effective deployment strategies are crucial for maintaining application availability and minimizing downtime. This guide covers blue-green deployments, canary deployments, rollback strategies, and deployment best practices for C# TuskLang applications.

## 🔵🔴 Blue-Green Deployment

### Blue-Green Deployment Service

```csharp
public class BlueGreenDeploymentService
{
    private readonly ILogger<BlueGreenDeploymentService> _logger;
    private readonly TSKConfig _config;
    private readonly IDbConnection _connection;
    private readonly HttpClient _httpClient;
    
    public BlueGreenDeploymentService(
        ILogger<BlueGreenDeploymentService> logger,
        TSKConfig config,
        IDbConnection connection,
        HttpClient httpClient)
    {
        _logger = logger;
        _config = config;
        _connection = connection;
        _httpClient = httpClient;
    }
    
    public async Task<DeploymentResult> DeployAsync(DeploymentRequest request)
    {
        try
        {
            _logger.LogInformation("Starting blue-green deployment for version {Version}", request.Version);
            
            // Determine current environment
            var currentEnvironment = await GetCurrentEnvironmentAsync();
            var targetEnvironment = currentEnvironment == "blue" ? "green" : "blue";
            
            // Deploy to target environment
            var deploymentResult = await DeployToEnvironmentAsync(targetEnvironment, request);
            
            if (!deploymentResult.Success)
            {
                return deploymentResult;
            }
            
            // Run health checks
            var healthCheckResult = await RunHealthChecksAsync(targetEnvironment);
            
            if (!healthCheckResult.Success)
            {
                _logger.LogError("Health checks failed for {Environment}", targetEnvironment);
                await RollbackDeploymentAsync(targetEnvironment);
                return DeploymentResult.Failure("Health checks failed");
            }
            
            // Switch traffic to new environment
            await SwitchTrafficAsync(targetEnvironment);
            
            // Update configuration
            await UpdateDeploymentConfigurationAsync(targetEnvironment, request.Version);
            
            _logger.LogInformation("Blue-green deployment completed successfully. Active environment: {Environment}", targetEnvironment);
            
            return DeploymentResult.Success(targetEnvironment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blue-green deployment failed");
            return DeploymentResult.Failure(ex.Message);
        }
    }
    
    private async Task<string> GetCurrentEnvironmentAsync()
    {
        var query = "SELECT active_environment FROM deployment_config WHERE id = 1";
        var result = await _connection.QueryFirstOrDefaultAsync<string>(query);
        return result ?? "blue";
    }
    
    private async Task<DeploymentResult> DeployToEnvironmentAsync(string environment, DeploymentRequest request)
    {
        try
        {
            var deploymentUrl = _config.Get<string>($"deployment.{environment}.url");
            if (string.IsNullOrEmpty(deploymentUrl))
            {
                return DeploymentResult.Failure($"Deployment URL not configured for {environment}");
            }
            
            var payload = new
            {
                version = request.Version,
                environment = environment,
                configuration = request.Configuration
            };
            
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(deploymentUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return DeploymentResult.Failure($"Deployment failed: {errorContent}");
            }
            
            return DeploymentResult.Success(environment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deploy to {Environment}", environment);
            return DeploymentResult.Failure(ex.Message);
        }
    }
    
    private async Task<HealthCheckResult> RunHealthChecksAsync(string environment)
    {
        try
        {
            var healthCheckUrl = _config.Get<string>($"deployment.{environment}.health_url");
            if (string.IsNullOrEmpty(healthCheckUrl))
            {
                return HealthCheckResult.Failure("Health check URL not configured");
            }
            
            var maxRetries = _config.Get<int>("deployment.health_check.max_retries", 3);
            var retryDelay = TimeSpan.FromSeconds(_config.Get<int>("deployment.health_check.retry_delay_seconds", 10));
            
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var response = await _httpClient.GetAsync(healthCheckUrl);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var healthData = await response.Content.ReadAsStringAsync();
                        var healthStatus = JsonSerializer.Deserialize<HealthStatus>(healthData);
                        
                        if (healthStatus?.Status == "healthy")
                        {
                            return HealthCheckResult.Success();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Health check attempt {Attempt} failed for {Environment}", i + 1, environment);
                }
                
                if (i < maxRetries - 1)
                {
                    await Task.Delay(retryDelay);
                }
            }
            
            return HealthCheckResult.Failure("Health checks failed after all retries");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for {Environment}", environment);
            return HealthCheckResult.Failure(ex.Message);
        }
    }
    
    private async Task SwitchTrafficAsync(string targetEnvironment)
    {
        try
        {
            var loadBalancerUrl = _config.Get<string>("deployment.load_balancer.url");
            if (string.IsNullOrEmpty(loadBalancerUrl))
            {
                _logger.LogWarning("Load balancer URL not configured, skipping traffic switch");
                return;
            }
            
            var payload = new
            {
                active_environment = targetEnvironment,
                switch_time = DateTime.UtcNow
            };
            
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(loadBalancerUrl, content);
            response.EnsureSuccessStatusCode();
            
            _logger.LogInformation("Traffic switched to {Environment}", targetEnvironment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch traffic to {Environment}", targetEnvironment);
            throw;
        }
    }
    
    private async Task UpdateDeploymentConfigurationAsync(string environment, string version)
    {
        try
        {
            var query = @"
                UPDATE deployment_config 
                SET active_environment = @Environment, 
                    active_version = @Version, 
                    last_deployment = @LastDeployment
                WHERE id = 1";
            
            var parameters = new
            {
                Environment = environment,
                Version = version,
                LastDeployment = DateTime.UtcNow
            };
            
            await _connection.ExecuteAsync(query, parameters);
            
            _logger.LogInformation("Deployment configuration updated: {Environment} - {Version}", environment, version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update deployment configuration");
            throw;
        }
    }
    
    private async Task RollbackDeploymentAsync(string environment)
    {
        try
        {
            _logger.LogWarning("Rolling back deployment for {Environment}", environment);
            
            var rollbackUrl = _config.Get<string>($"deployment.{environment}.rollback_url");
            if (!string.IsNullOrEmpty(rollbackUrl))
            {
                var response = await _httpClient.PostAsync(rollbackUrl, null);
                response.EnsureSuccessStatusCode();
            }
            
            _logger.LogInformation("Rollback completed for {Environment}", environment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback deployment for {Environment}", environment);
        }
    }
}

public class DeploymentRequest
{
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public bool AutoRollback { get; set; } = true;
}

public class DeploymentResult
{
    public bool Success { get; set; }
    public string? Environment { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public static DeploymentResult Success(string environment)
    {
        return new DeploymentResult
        {
            Success = true,
            Environment = environment
        };
    }
    
    public static DeploymentResult Failure(string errorMessage)
    {
        return new DeploymentResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}

public class HealthCheckResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    public static HealthCheckResult Success()
    {
        return new HealthCheckResult { Success = true };
    }
    
    public static HealthCheckResult Failure(string errorMessage)
    {
        return new HealthCheckResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}

public class HealthStatus
{
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
}
```

## 🐦 Canary Deployment

### Canary Deployment Service

```csharp
public class CanaryDeploymentService
{
    private readonly ILogger<CanaryDeploymentService> _logger;
    private readonly TSKConfig _config;
    private readonly IDbConnection _connection;
    private readonly HttpClient _httpClient;
    private readonly MetricsService _metricsService;
    
    public CanaryDeploymentService(
        ILogger<CanaryDeploymentService> logger,
        TSKConfig config,
        IDbConnection connection,
        HttpClient httpClient,
        MetricsService metricsService)
    {
        _logger = logger;
        _config = config;
        _connection = connection;
        _httpClient = httpClient;
        _metricsService = metricsService;
    }
    
    public async Task<CanaryDeploymentResult> DeployCanaryAsync(CanaryDeploymentRequest request)
    {
        try
        {
            _logger.LogInformation("Starting canary deployment for version {Version}", request.Version);
            
            // Deploy canary version
            var deploymentResult = await DeployCanaryVersionAsync(request);
            
            if (!deploymentResult.Success)
            {
                return CanaryDeploymentResult.Failure("Canary deployment failed");
            }
            
            // Start traffic routing with initial percentage
            await StartCanaryTrafficAsync(request.InitialTrafficPercentage);
            
            // Monitor canary performance
            var monitoringResult = await MonitorCanaryAsync(request);
            
            if (!monitoringResult.Success)
            {
                await RollbackCanaryAsync();
                return CanaryDeploymentResult.Failure("Canary monitoring failed");
            }
            
            // Gradually increase traffic
            await GradualTrafficIncreaseAsync(request);
            
            // Promote to full deployment if successful
            if (request.AutoPromote)
            {
                await PromoteCanaryAsync(request.Version);
            }
            
            _logger.LogInformation("Canary deployment completed successfully");
            
            return CanaryDeploymentResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Canary deployment failed");
            await RollbackCanaryAsync();
            return CanaryDeploymentResult.Failure(ex.Message);
        }
    }
    
    private async Task<DeploymentResult> DeployCanaryVersionAsync(CanaryDeploymentRequest request)
    {
        try
        {
            var canaryUrl = _config.Get<string>("deployment.canary.url");
            if (string.IsNullOrEmpty(canaryUrl))
            {
                return DeploymentResult.Failure("Canary deployment URL not configured");
            }
            
            var payload = new
            {
                version = request.Version,
                environment = "canary",
                configuration = request.Configuration,
                traffic_percentage = request.InitialTrafficPercentage
            };
            
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(canaryUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return DeploymentResult.Failure($"Canary deployment failed: {errorContent}");
            }
            
            return DeploymentResult.Success("canary");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deploy canary version");
            return DeploymentResult.Failure(ex.Message);
        }
    }
    
    private async Task StartCanaryTrafficAsync(double trafficPercentage)
    {
        try
        {
            var loadBalancerUrl = _config.Get<string>("deployment.load_balancer.url");
            if (string.IsNullOrEmpty(loadBalancerUrl))
            {
                _logger.LogWarning("Load balancer URL not configured, skipping traffic routing");
                return;
            }
            
            var payload = new
            {
                canary_traffic_percentage = trafficPercentage,
                start_time = DateTime.UtcNow
            };
            
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(loadBalancerUrl, content);
            response.EnsureSuccessStatusCode();
            
            _logger.LogInformation("Canary traffic started at {Percentage}%", trafficPercentage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start canary traffic");
            throw;
        }
    }
    
    private async Task<MonitoringResult> MonitorCanaryAsync(CanaryDeploymentRequest request)
    {
        try
        {
            var monitoringDuration = request.MonitoringDuration;
            var checkInterval = TimeSpan.FromSeconds(_config.Get<int>("deployment.canary.check_interval_seconds", 30));
            var checks = (int)(monitoringDuration.TotalSeconds / checkInterval.TotalSeconds);
            
            var successChecks = 0;
            var totalChecks = 0;
            
            for (int i = 0; i < checks; i++)
            {
                var healthResult = await CheckCanaryHealthAsync();
                var metricsResult = await CheckCanaryMetricsAsync();
                
                totalChecks++;
                
                if (healthResult.Success && metricsResult.Success)
                {
                    successChecks++;
                }
                
                await Task.Delay(checkInterval);
            }
            
            var successRate = (double)successChecks / totalChecks;
            var requiredSuccessRate = _config.Get<double>("deployment.canary.required_success_rate", 0.95);
            
            if (successRate >= requiredSuccessRate)
            {
                return MonitoringResult.Success(successRate);
            }
            else
            {
                return MonitoringResult.Failure($"Success rate {successRate:P} below required {requiredSuccessRate:P}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Canary monitoring failed");
            return MonitoringResult.Failure(ex.Message);
        }
    }
    
    private async Task<HealthCheckResult> CheckCanaryHealthAsync()
    {
        try
        {
            var healthUrl = _config.Get<string>("deployment.canary.health_url");
            if (string.IsNullOrEmpty(healthUrl))
            {
                return HealthCheckResult.Failure("Health check URL not configured");
            }
            
            var response = await _httpClient.GetAsync(healthUrl);
            
            if (response.IsSuccessStatusCode)
            {
                var healthData = await response.Content.ReadAsStringAsync();
                var healthStatus = JsonSerializer.Deserialize<HealthStatus>(healthData);
                
                if (healthStatus?.Status == "healthy")
                {
                    return HealthCheckResult.Success();
                }
            }
            
            return HealthCheckResult.Failure("Health check failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Canary health check failed");
            return HealthCheckResult.Failure(ex.Message);
        }
    }
    
    private async Task<MetricsCheckResult> CheckCanaryMetricsAsync()
    {
        try
        {
            var errorRateThreshold = _config.Get<double>("deployment.canary.error_rate_threshold", 0.05);
            var responseTimeThreshold = _config.Get<double>("deployment.canary.response_time_threshold_ms", 1000);
            
            // Get metrics for canary deployment
            var errorRate = await GetCanaryErrorRateAsync();
            var responseTime = await GetCanaryResponseTimeAsync();
            
            if (errorRate <= errorRateThreshold && responseTime <= responseTimeThreshold)
            {
                return MetricsCheckResult.Success(errorRate, responseTime);
            }
            else
            {
                return MetricsCheckResult.Failure($"Error rate: {errorRate:P}, Response time: {responseTime}ms");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Canary metrics check failed");
            return MetricsCheckResult.Failure(ex.Message);
        }
    }
    
    private async Task<double> GetCanaryErrorRateAsync()
    {
        // This would typically query metrics from your monitoring system
        // For now, return a placeholder value
        await Task.CompletedTask;
        return 0.02; // 2% error rate
    }
    
    private async Task<double> GetCanaryResponseTimeAsync()
    {
        // This would typically query metrics from your monitoring system
        // For now, return a placeholder value
        await Task.CompletedTask;
        return 500; // 500ms response time
    }
    
    private async Task GradualTrafficIncreaseAsync(CanaryDeploymentRequest request)
    {
        try
        {
            var steps = request.TrafficIncreaseSteps;
            var stepDelay = request.StepDelay;
            
            foreach (var step in steps)
            {
                await UpdateCanaryTrafficAsync(step);
                await Task.Delay(stepDelay);
                
                // Check health after each step
                var healthResult = await CheckCanaryHealthAsync();
                if (!healthResult.Success)
                {
                    throw new Exception($"Health check failed at {step}% traffic");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gradual traffic increase failed");
            throw;
        }
    }
    
    private async Task UpdateCanaryTrafficAsync(double percentage)
    {
        try
        {
            var loadBalancerUrl = _config.Get<string>("deployment.load_balancer.url");
            if (string.IsNullOrEmpty(loadBalancerUrl))
            {
                return;
            }
            
            var payload = new
            {
                canary_traffic_percentage = percentage,
                update_time = DateTime.UtcNow
            };
            
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(loadBalancerUrl, content);
            response.EnsureSuccessStatusCode();
            
            _logger.LogInformation("Canary traffic updated to {Percentage}%", percentage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update canary traffic");
            throw;
        }
    }
    
    private async Task PromoteCanaryAsync(string version)
    {
        try
        {
            _logger.LogInformation("Promoting canary version {Version} to production", version);
            
            // Deploy to production
            var productionUrl = _config.Get<string>("deployment.production.url");
            if (!string.IsNullOrEmpty(productionUrl))
            {
                var payload = new { version = version };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(productionUrl, content);
                response.EnsureSuccessStatusCode();
            }
            
            // Switch all traffic to production
            await UpdateCanaryTrafficAsync(0);
            
            _logger.LogInformation("Canary version {Version} promoted to production", version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to promote canary version {Version}", version);
            throw;
        }
    }
    
    private async Task RollbackCanaryAsync()
    {
        try
        {
            _logger.LogWarning("Rolling back canary deployment");
            
            // Stop canary traffic
            await UpdateCanaryTrafficAsync(0);
            
            // Rollback canary deployment
            var rollbackUrl = _config.Get<string>("deployment.canary.rollback_url");
            if (!string.IsNullOrEmpty(rollbackUrl))
            {
                var response = await _httpClient.PostAsync(rollbackUrl, null);
                response.EnsureSuccessStatusCode();
            }
            
            _logger.LogInformation("Canary deployment rolled back");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback canary deployment");
        }
    }
}

public class CanaryDeploymentRequest
{
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public double InitialTrafficPercentage { get; set; } = 5.0;
    public List<double> TrafficIncreaseSteps { get; set; } = new() { 10, 25, 50, 75, 100 };
    public TimeSpan StepDelay { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan MonitoringDuration { get; set; } = TimeSpan.FromMinutes(30);
    public bool AutoPromote { get; set; } = true;
}

public class CanaryDeploymentResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public static CanaryDeploymentResult Success()
    {
        return new CanaryDeploymentResult { Success = true };
    }
    
    public static CanaryDeploymentResult Failure(string errorMessage)
    {
        return new CanaryDeploymentResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}

public class MonitoringResult
{
    public bool Success { get; set; }
    public double SuccessRate { get; set; }
    public string? ErrorMessage { get; set; }
    
    public static MonitoringResult Success(double successRate)
    {
        return new MonitoringResult
        {
            Success = true,
            SuccessRate = successRate
        };
    }
    
    public static MonitoringResult Failure(string errorMessage)
    {
        return new MonitoringResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}

public class MetricsCheckResult
{
    public bool Success { get; set; }
    public double ErrorRate { get; set; }
    public double ResponseTime { get; set; }
    public string? ErrorMessage { get; set; }
    
    public static MetricsCheckResult Success(double errorRate, double responseTime)
    {
        return new MetricsCheckResult
        {
            Success = true,
            ErrorRate = errorRate,
            ResponseTime = responseTime
        };
    }
    
    public static MetricsCheckResult Failure(string errorMessage)
    {
        return new MetricsCheckResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
```

## 🔄 Rollback Strategies

### Rollback Service

```csharp
public class RollbackService
{
    private readonly ILogger<RollbackService> _logger;
    private readonly TSKConfig _config;
    private readonly IDbConnection _connection;
    private readonly HttpClient _httpClient;
    
    public RollbackService(
        ILogger<RollbackService> logger,
        TSKConfig config,
        IDbConnection connection,
        HttpClient httpClient)
    {
        _logger = logger;
        _config = config;
        _connection = connection;
        _httpClient = httpClient;
    }
    
    public async Task<RollbackResult> RollbackAsync(RollbackRequest request)
    {
        try
        {
            _logger.LogWarning("Starting rollback to version {TargetVersion}", request.TargetVersion);
            
            // Get deployment history
            var deploymentHistory = await GetDeploymentHistoryAsync();
            var targetDeployment = deploymentHistory.FirstOrDefault(d => d.Version == request.TargetVersion);
            
            if (targetDeployment == null)
            {
                return RollbackResult.Failure($"Version {request.TargetVersion} not found in deployment history");
            }
            
            // Perform rollback
            var rollbackResult = await PerformRollbackAsync(targetDeployment);
            
            if (!rollbackResult.Success)
            {
                return rollbackResult;
            }
            
            // Update deployment configuration
            await UpdateDeploymentConfigurationAsync(targetDeployment);
            
            // Run post-rollback health checks
            var healthCheckResult = await RunPostRollbackHealthChecksAsync();
            
            if (!healthCheckResult.Success)
            {
                _logger.LogError("Post-rollback health checks failed");
                return RollbackResult.Failure("Post-rollback health checks failed");
            }
            
            _logger.LogInformation("Rollback to version {TargetVersion} completed successfully", request.TargetVersion);
            
            return RollbackResult.Success(targetDeployment.Version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rollback failed");
            return RollbackResult.Failure(ex.Message);
        }
    }
    
    private async Task<List<DeploymentRecord>> GetDeploymentHistoryAsync()
    {
        var query = @"
            SELECT version, environment, configuration, deployed_at, status
            FROM deployment_history
            ORDER BY deployed_at DESC";
        
        var records = await _connection.QueryAsync<DeploymentRecord>(query);
        return records.ToList();
    }
    
    private async Task<RollbackResult> PerformRollbackAsync(DeploymentRecord targetDeployment)
    {
        try
        {
            var rollbackUrl = _config.Get<string>("deployment.rollback.url");
            if (string.IsNullOrEmpty(rollbackUrl))
            {
                return RollbackResult.Failure("Rollback URL not configured");
            }
            
            var payload = new
            {
                target_version = targetDeployment.Version,
                target_environment = targetDeployment.Environment,
                configuration = targetDeployment.Configuration,
                rollback_time = DateTime.UtcNow
            };
            
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(rollbackUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return RollbackResult.Failure($"Rollback failed: {errorContent}");
            }
            
            return RollbackResult.Success(targetDeployment.Version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform rollback");
            return RollbackResult.Failure(ex.Message);
        }
    }
    
    private async Task UpdateDeploymentConfigurationAsync(DeploymentRecord deployment)
    {
        try
        {
            var query = @"
                UPDATE deployment_config 
                SET active_version = @Version, 
                    active_environment = @Environment,
                    last_rollback = @LastRollback
                WHERE id = 1";
            
            var parameters = new
            {
                deployment.Version,
                deployment.Environment,
                LastRollback = DateTime.UtcNow
            };
            
            await _connection.ExecuteAsync(query, parameters);
            
            _logger.LogInformation("Deployment configuration updated for rollback: {Version}", deployment.Version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update deployment configuration");
            throw;
        }
    }
    
    private async Task<HealthCheckResult> RunPostRollbackHealthChecksAsync()
    {
        try
        {
            var healthCheckUrl = _config.Get<string>("deployment.health_check.url");
            if (string.IsNullOrEmpty(healthCheckUrl))
            {
                return HealthCheckResult.Failure("Health check URL not configured");
            }
            
            var maxRetries = _config.Get<int>("deployment.rollback.health_check.max_retries", 5);
            var retryDelay = TimeSpan.FromSeconds(_config.Get<int>("deployment.rollback.health_check.retry_delay_seconds", 30));
            
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var response = await _httpClient.GetAsync(healthCheckUrl);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var healthData = await response.Content.ReadAsStringAsync();
                        var healthStatus = JsonSerializer.Deserialize<HealthStatus>(healthData);
                        
                        if (healthStatus?.Status == "healthy")
                        {
                            return HealthCheckResult.Success();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Post-rollback health check attempt {Attempt} failed", i + 1);
                }
                
                if (i < maxRetries - 1)
                {
                    await Task.Delay(retryDelay);
                }
            }
            
            return HealthCheckResult.Failure("Post-rollback health checks failed after all retries");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Post-rollback health check failed");
            return HealthCheckResult.Failure(ex.Message);
        }
    }
    
    public async Task<RollbackResult> AutoRollbackAsync(string failedVersion, string reason)
    {
        try
        {
            _logger.LogWarning("Auto-rollback triggered for version {FailedVersion}: {Reason}", failedVersion, reason);
            
            // Get the previous successful deployment
            var deploymentHistory = await GetDeploymentHistoryAsync();
            var previousDeployment = deploymentHistory
                .Where(d => d.Status == "success" && d.Version != failedVersion)
                .OrderByDescending(d => d.DeployedAt)
                .FirstOrDefault();
            
            if (previousDeployment == null)
            {
                return RollbackResult.Failure("No previous successful deployment found for auto-rollback");
            }
            
            var request = new RollbackRequest
            {
                TargetVersion = previousDeployment.Version,
                Reason = $"Auto-rollback due to: {reason}",
                AutoRollback = true
            };
            
            return await RollbackAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-rollback failed");
            return RollbackResult.Failure(ex.Message);
        }
    }
}

public class RollbackRequest
{
    public string TargetVersion { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool AutoRollback { get; set; } = false;
}

public class RollbackResult
{
    public bool Success { get; set; }
    public string? Version { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public static RollbackResult Success(string version)
    {
        return new RollbackResult
        {
            Success = true,
            Version = version
        };
    }
    
    public static RollbackResult Failure(string errorMessage)
    {
        return new RollbackResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}

public class DeploymentRecord
{
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string Configuration { get; set; } = string.Empty;
    public DateTime DeployedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
```

## 📝 Summary

This guide covered comprehensive deployment strategies for C# TuskLang applications:

- **Blue-Green Deployment**: Complete environment switching with health checks and traffic routing
- **Canary Deployment**: Gradual rollout with monitoring and automatic promotion
- **Rollback Strategies**: Automated rollback with health checks and deployment history
- **Deployment Best Practices**: Health monitoring, traffic management, and configuration updates

These deployment strategies ensure your C# TuskLang applications can be deployed safely with minimal downtime and maximum reliability. 