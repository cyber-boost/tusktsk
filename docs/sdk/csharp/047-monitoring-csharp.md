# Monitoring in C# TuskLang

## Overview

Comprehensive monitoring is essential for maintaining application health and performance. This guide covers metrics collection, logging, tracing, alerting, and monitoring best practices for C# TuskLang applications.

## 📊 Metrics Collection

### Metrics Service

```csharp
public class MetricsService
{
    private readonly ILogger<MetricsService> _logger;
    private readonly TSKConfig _config;
    private readonly Dictionary<string, MetricCollector> _collectors;
    private readonly Timer _reportingTimer;
    private readonly IMetricsExporter _exporter;
    
    public MetricsService(
        ILogger<MetricsService> logger,
        TSKConfig config,
        IMetricsExporter exporter)
    {
        _logger = logger;
        _config = config;
        _exporter = exporter;
        _collectors = new Dictionary<string, MetricCollector>();
        
        var reportingInterval = TimeSpan.FromSeconds(config.Get<int>("metrics.reporting_interval_seconds", 60));
        _reportingTimer = new Timer(ReportMetrics, null, reportingInterval, reportingInterval);
    }
    
    public void RecordTiming(string operation, TimeSpan duration, Dictionary<string, object>? tags = null)
    {
        var collector = GetOrCreateCollector(operation);
        collector.RecordTiming(duration, tags);
    }
    
    public void IncrementCounter(string name, int value = 1, Dictionary<string, object>? tags = null)
    {
        var collector = GetOrCreateCollector(name);
        collector.IncrementCounter(value, tags);
    }
    
    public void RecordGauge(string name, double value, Dictionary<string, object>? tags = null)
    {
        var collector = GetOrCreateCollector(name);
        collector.RecordGauge(value, tags);
    }
    
    public void RecordHistogram(string name, double value, Dictionary<string, object>? tags = null)
    {
        var collector = GetOrCreateCollector(name);
        collector.RecordHistogram(value, tags);
    }
    
    private MetricCollector GetOrCreateCollector(string name)
    {
        lock (_collectors)
        {
            if (!_collectors.ContainsKey(name))
            {
                _collectors[name] = new MetricCollector(name);
            }
            return _collectors[name];
        }
    }
    
    private async void ReportMetrics(object? state)
    {
        try
        {
            var metrics = CollectAllMetrics();
            await _exporter.ExportAsync(metrics);
            
            _logger.LogDebug("Exported {Count} metrics", metrics.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to report metrics");
        }
    }
    
    private List<Metric> CollectAllMetrics()
    {
        var metrics = new List<Metric>();
        
        lock (_collectors)
        {
            foreach (var kvp in _collectors)
            {
                var collector = kvp.Value;
                metrics.AddRange(collector.GetMetrics());
            }
        }
        
        return metrics;
    }
    
    public MetricsReport GenerateReport()
    {
        var report = new MetricsReport
        {
            GeneratedAt = DateTime.UtcNow,
            ServiceName = _config.Get<string>("app.name", "unknown"),
            Environment = _config.Get<string>("app.environment", "unknown")
        };
        
        lock (_collectors)
        {
            foreach (var kvp in _collectors)
            {
                var collector = kvp.Value;
                var collectorReport = collector.GenerateReport();
                report.Collectors.Add(collectorReport);
            }
        }
        
        return report;
    }
    
    public void Dispose()
    {
        _reportingTimer?.Dispose();
    }
}

public class MetricCollector
{
    private readonly string _name;
    private readonly ConcurrentQueue<TimingMetric> _timings;
    private readonly ConcurrentQueue<CounterMetric> _counters;
    private readonly ConcurrentQueue<GaugeMetric> _gauges;
    private readonly ConcurrentQueue<HistogramMetric> _histograms;
    
    public MetricCollector(string name)
    {
        _name = name;
        _timings = new ConcurrentQueue<TimingMetric>();
        _counters = new ConcurrentQueue<CounterMetric>();
        _gauges = new ConcurrentQueue<GaugeMetric>();
        _histograms = new ConcurrentQueue<HistogramMetric>();
    }
    
    public void RecordTiming(TimeSpan duration, Dictionary<string, object>? tags = null)
    {
        _timings.Enqueue(new TimingMetric
        {
            Name = _name,
            Duration = duration,
            Tags = tags ?? new Dictionary<string, object>(),
            Timestamp = DateTime.UtcNow
        });
    }
    
    public void IncrementCounter(int value, Dictionary<string, object>? tags = null)
    {
        _counters.Enqueue(new CounterMetric
        {
            Name = _name,
            Value = value,
            Tags = tags ?? new Dictionary<string, object>(),
            Timestamp = DateTime.UtcNow
        });
    }
    
    public void RecordGauge(double value, Dictionary<string, object>? tags = null)
    {
        _gauges.Enqueue(new GaugeMetric
        {
            Name = _name,
            Value = value,
            Tags = tags ?? new Dictionary<string, object>(),
            Timestamp = DateTime.UtcNow
        });
    }
    
    public void RecordHistogram(double value, Dictionary<string, object>? tags = null)
    {
        _histograms.Enqueue(new HistogramMetric
        {
            Name = _name,
            Value = value,
            Tags = tags ?? new Dictionary<string, object>(),
            Timestamp = DateTime.UtcNow
        });
    }
    
    public List<Metric> GetMetrics()
    {
        var metrics = new List<Metric>();
        
        // Collect timing metrics
        while (_timings.TryDequeue(out var timing))
        {
            metrics.Add(timing);
        }
        
        // Collect counter metrics
        while (_counters.TryDequeue(out var counter))
        {
            metrics.Add(counter);
        }
        
        // Collect gauge metrics
        while (_gauges.TryDequeue(out var gauge))
        {
            metrics.Add(gauge);
        }
        
        // Collect histogram metrics
        while (_histograms.TryDequeue(out var histogram))
        {
            metrics.Add(histogram);
        }
        
        return metrics;
    }
    
    public CollectorReport GenerateReport()
    {
        var report = new CollectorReport
        {
            Name = _name,
            Metrics = GetMetrics()
        };
        
        return report;
    }
}

public class MetricsReport
{
    public DateTime GeneratedAt { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public List<CollectorReport> Collectors { get; set; } = new();
}

public class CollectorReport
{
    public string Name { get; set; } = string.Empty;
    public List<Metric> Metrics { get; set; } = new();
}
```

### Health Check Service

```csharp
public class HealthCheckService : IHealthCheck
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly TSKConfig _config;
    private readonly IDbConnection _connection;
    private readonly HttpClient _httpClient;
    
    public HealthCheckService(
        ILogger<HealthCheckService> logger,
        TSKConfig config,
        IDbConnection connection,
        HttpClient httpClient)
    {
        _logger = logger;
        _config = config;
        _connection = connection;
        _httpClient = httpClient;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var checks = new List<HealthCheckResult>();
        
        // Database health check
        checks.Add(await CheckDatabaseHealthAsync());
        
        // Configuration health check
        checks.Add(await CheckConfigurationHealthAsync());
        
        // External service health check
        checks.Add(await CheckExternalServiceHealthAsync());
        
        // Memory health check
        checks.Add(await CheckMemoryHealthAsync());
        
        var unhealthyChecks = checks.Where(c => c.Status == HealthStatus.Unhealthy).ToList();
        var degradedChecks = checks.Where(c => c.Status == HealthStatus.Degraded).ToList();
        
        if (unhealthyChecks.Any())
        {
            return HealthCheckResult.Unhealthy(
                "Health check failed",
                data: new Dictionary<string, object>
                {
                    ["unhealthy_checks"] = unhealthyChecks.Count,
                    ["degraded_checks"] = degradedChecks.Count,
                    ["unhealthy_details"] = unhealthyChecks.Select(c => c.Description).ToList()
                });
        }
        
        if (degradedChecks.Any())
        {
            return HealthCheckResult.Degraded(
                "Some health checks are degraded",
                data: new Dictionary<string, object>
                {
                    ["degraded_checks"] = degradedChecks.Count,
                    ["degraded_details"] = degradedChecks.Select(c => c.Description).ToList()
                });
        }
        
        return HealthCheckResult.Healthy("All health checks passed");
    }
    
    private async Task<HealthCheckResult> CheckDatabaseHealthAsync()
    {
        try
        {
            await _connection.OpenAsync();
            await _connection.ExecuteScalarAsync<int>("SELECT 1");
            await _connection.CloseAsync();
            
            return HealthCheckResult.Healthy("Database connection is healthy");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
    
    private async Task<HealthCheckResult> CheckConfigurationHealthAsync()
    {
        try
        {
            var requiredKeys = new[] { "database.connection_string", "api.base_url", "security.jwt_secret" };
            var missingKeys = new List<string>();
            
            foreach (var key in requiredKeys)
            {
                if (!_config.Has(key))
                {
                    missingKeys.Add(key);
                }
            }
            
            if (missingKeys.Any())
            {
                return HealthCheckResult.Unhealthy(
                    "Missing required configuration keys",
                    data: new Dictionary<string, object>
                    {
                        ["missing_keys"] = missingKeys
                    });
            }
            
            return HealthCheckResult.Healthy("Configuration is healthy");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration health check failed");
            return HealthCheckResult.Unhealthy("Configuration check failed", ex);
        }
    }
    
    private async Task<HealthCheckResult> CheckExternalServiceHealthAsync()
    {
        try
        {
            var apiUrl = _config.Get<string>("api.base_url");
            if (string.IsNullOrEmpty(apiUrl))
            {
                return HealthCheckResult.Degraded("API URL not configured");
            }
            
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
            
            var response = await _httpClient.GetAsync($"{apiUrl}/health");
            
            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("External service is healthy");
            }
            
            return HealthCheckResult.Degraded(
                "External service returned non-success status",
                data: new Dictionary<string, object>
                {
                    ["status_code"] = (int)response.StatusCode
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "External service health check failed");
            return HealthCheckResult.Degraded("External service is unavailable", ex);
        }
    }
    
    private async Task<HealthCheckResult> CheckMemoryHealthAsync()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var workingSetMB = process.WorkingSet64 / 1024.0 / 1024.0;
            var memoryThreshold = _config.Get<double>("health.memory_threshold_mb", 1000);
            
            if (workingSetMB > memoryThreshold)
            {
                return HealthCheckResult.Degraded(
                    "High memory usage detected",
                    data: new Dictionary<string, object>
                    {
                        ["working_set_mb"] = workingSetMB,
                        ["threshold_mb"] = memoryThreshold
                    });
            }
            
            return HealthCheckResult.Healthy("Memory usage is normal");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory health check failed");
            return HealthCheckResult.Unhealthy("Memory check failed", ex);
        }
    }
}
```

## 🔍 Distributed Tracing

### Tracing Service

```csharp
public class TracingService
{
    private readonly ILogger<TracingService> _logger;
    private readonly TSKConfig _config;
    private readonly Dictionary<string, TraceSpan> _activeSpans;
    private readonly object _lock = new();
    
    public TracingService(ILogger<TracingService> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        _activeSpans = new Dictionary<string, TraceSpan>();
    }
    
    public TraceSpan StartSpan(string operationName, string? parentSpanId = null)
    {
        var spanId = Guid.NewGuid().ToString();
        var traceId = parentSpanId ?? Guid.NewGuid().ToString();
        
        var span = new TraceSpan
        {
            SpanId = spanId,
            TraceId = traceId,
            OperationName = operationName,
            StartTime = DateTime.UtcNow,
            ParentSpanId = parentSpanId
        };
        
        lock (_lock)
        {
            _activeSpans[spanId] = span;
        }
        
        _logger.LogDebug("Started trace span: {SpanId} for operation: {OperationName}", spanId, operationName);
        
        return span;
    }
    
    public void EndSpan(string spanId, Dictionary<string, object>? tags = null)
    {
        lock (_lock)
        {
            if (_activeSpans.TryGetValue(spanId, out var span))
            {
                span.EndTime = DateTime.UtcNow;
                span.Duration = span.EndTime - span.StartTime;
                
                if (tags != null)
                {
                    foreach (var kvp in tags)
                    {
                        span.Tags[kvp.Key] = kvp.Value;
                    }
                }
                
                _activeSpans.Remove(spanId);
                
                // Send span to tracing system
                SendSpanAsync(span);
                
                _logger.LogDebug("Ended trace span: {SpanId} with duration: {Duration}ms", 
                    spanId, span.Duration.TotalMilliseconds);
            }
        }
    }
    
    public void AddTag(string spanId, string key, object value)
    {
        lock (_lock)
        {
            if (_activeSpans.TryGetValue(spanId, out var span))
            {
                span.Tags[key] = value;
            }
        }
    }
    
    public void AddEvent(string spanId, string eventName, Dictionary<string, object>? attributes = null)
    {
        lock (_lock)
        {
            if (_activeSpans.TryGetValue(spanId, out var span))
            {
                var traceEvent = new TraceEvent
                {
                    Name = eventName,
                    Timestamp = DateTime.UtcNow,
                    Attributes = attributes ?? new Dictionary<string, object>()
                };
                
                span.Events.Add(traceEvent);
            }
        }
    }
    
    private async void SendSpanAsync(TraceSpan span)
    {
        try
        {
            var tracingUrl = _config.Get<string>("tracing.endpoint_url");
            if (string.IsNullOrEmpty(tracingUrl))
            {
                return;
            }
            
            using var client = new HttpClient();
            var payload = new TracingPayload
            {
                ServiceName = _config.Get<string>("app.name", "unknown"),
                Environment = _config.Get<string>("app.environment", "unknown"),
                Span = span
            };
            
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync(tracingUrl, content);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send trace span {SpanId}", span.SpanId);
        }
    }
}

public class TraceSpan
{
    public string SpanId { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string? ParentSpanId { get; set; }
    public string OperationName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object> Tags { get; set; } = new();
    public List<TraceEvent> Events { get; set; } = new();
}

public class TraceEvent
{
    public string Name { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Attributes { get; set; } = new();
}

public class TracingPayload
{
    public string ServiceName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public TraceSpan Span { get; set; } = new();
}
```

## 🚨 Alerting System

### Alert Service

```csharp
public class AlertService
{
    private readonly ILogger<AlertService> _logger;
    private readonly TSKConfig _config;
    private readonly Dictionary<string, AlertRule> _alertRules;
    private readonly Timer _alertCheckTimer;
    
    public AlertService(ILogger<AlertService> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        _alertRules = new Dictionary<string, AlertRule>();
        
        LoadAlertRules();
        
        var checkInterval = TimeSpan.FromSeconds(config.Get<int>("alerts.check_interval_seconds", 30));
        _alertCheckTimer = new Timer(CheckAlerts, null, checkInterval, checkInterval);
    }
    
    public void RegisterAlertRule(AlertRule rule)
    {
        _alertRules[rule.Name] = rule;
        _logger.LogInformation("Registered alert rule: {RuleName}", rule.Name);
    }
    
    public async Task TriggerAlertAsync(string ruleName, string message, Dictionary<string, object>? context = null)
    {
        try
        {
            var alert = new Alert
            {
                RuleName = ruleName,
                Message = message,
                Context = context ?? new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow,
                Severity = _alertRules.GetValueOrDefault(ruleName)?.Severity ?? AlertSeverity.Warning
            };
            
            await SendAlertAsync(alert);
            
            _logger.LogWarning("Alert triggered: {RuleName} - {Message}", ruleName, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger alert {RuleName}", ruleName);
        }
    }
    
    private async void CheckAlerts(object? state)
    {
        try
        {
            foreach (var rule in _alertRules.Values)
            {
                if (await ShouldTriggerAlertAsync(rule))
                {
                    await TriggerAlertAsync(rule.Name, rule.Message, rule.Context);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check alerts");
        }
    }
    
    private async Task<bool> ShouldTriggerAlertAsync(AlertRule rule)
    {
        try
        {
            // Check if rule is enabled
            if (!rule.Enabled)
            {
                return false;
            }
            
            // Check if enough time has passed since last alert
            if (rule.LastTriggered.HasValue)
            {
                var timeSinceLastAlert = DateTime.UtcNow - rule.LastTriggered.Value;
                if (timeSinceLastAlert < rule.CooldownPeriod)
                {
                    return false;
                }
            }
            
            // Evaluate condition
            var condition = await EvaluateConditionAsync(rule.Condition);
            
            if (condition)
            {
                rule.LastTriggered = DateTime.UtcNow;
            }
            
            return condition;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate alert rule {RuleName}", rule.Name);
            return false;
        }
    }
    
    private async Task<bool> EvaluateConditionAsync(AlertCondition condition)
    {
        // This is a simplified implementation
        // In a real system, you would evaluate metrics, logs, or other data sources
        switch (condition.Type)
        {
            case "threshold":
                return await EvaluateThresholdConditionAsync(condition);
            case "trend":
                return await EvaluateTrendConditionAsync(condition);
            default:
                return false;
        }
    }
    
    private async Task<bool> EvaluateThresholdConditionAsync(AlertCondition condition)
    {
        // Placeholder implementation
        await Task.CompletedTask;
        return false;
    }
    
    private async Task<bool> EvaluateTrendConditionAsync(AlertCondition condition)
    {
        // Placeholder implementation
        await Task.CompletedTask;
        return false;
    }
    
    private async Task SendAlertAsync(Alert alert)
    {
        var alertUrl = _config.Get<string>("alerts.webhook_url");
        if (string.IsNullOrEmpty(alertUrl))
        {
            return;
        }
        
        using var client = new HttpClient();
        var payload = new AlertPayload
        {
            ServiceName = _config.Get<string>("app.name", "unknown"),
            Environment = _config.Get<string>("app.environment", "unknown"),
            Alert = alert
        };
        
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await client.PostAsync(alertUrl, content);
        response.EnsureSuccessStatusCode();
    }
    
    private void LoadAlertRules()
    {
        var rulesConfig = _config.GetSection("alerts.rules");
        if (rulesConfig != null)
        {
            foreach (var key in rulesConfig.GetKeys())
            {
                var ruleConfig = rulesConfig.GetSection(key);
                var rule = new AlertRule
                {
                    Name = key,
                    Message = ruleConfig.Get<string>("message", "Alert triggered"),
                    Severity = ParseAlertSeverity(ruleConfig.Get<string>("severity", "warning")),
                    Enabled = ruleConfig.Get<bool>("enabled", true),
                    CooldownPeriod = TimeSpan.FromMinutes(ruleConfig.Get<int>("cooldown_minutes", 5)),
                    Condition = new AlertCondition
                    {
                        Type = ruleConfig.Get<string>("condition.type", "threshold"),
                        Parameters = ruleConfig.GetSection("condition.parameters").ToDictionary()
                    }
                };
                
                _alertRules[key] = rule;
            }
        }
    }
    
    private AlertSeverity ParseAlertSeverity(string severity)
    {
        return severity.ToLower() switch
        {
            "critical" => AlertSeverity.Critical,
            "error" => AlertSeverity.Error,
            "warning" => AlertSeverity.Warning,
            "info" => AlertSeverity.Info,
            _ => AlertSeverity.Warning
        };
    }
    
    public void Dispose()
    {
        _alertCheckTimer?.Dispose();
    }
}

public class AlertRule
{
    public string Name { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public bool Enabled { get; set; } = true;
    public TimeSpan CooldownPeriod { get; set; } = TimeSpan.FromMinutes(5);
    public DateTime? LastTriggered { get; set; }
    public AlertCondition Condition { get; set; } = new();
    public Dictionary<string, object> Context { get; set; } = new();
}

public class AlertCondition
{
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class Alert
{
    public string RuleName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public enum AlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public class AlertPayload
{
    public string ServiceName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public Alert Alert { get; set; } = new();
}
```

## 📈 Monitoring Dashboard

### Dashboard Data Service

```csharp
public class DashboardDataService
{
    private readonly ILogger<DashboardDataService> _logger;
    private readonly TSKConfig _config;
    private readonly MetricsService _metricsService;
    private readonly HealthCheckService _healthCheckService;
    
    public DashboardDataService(
        ILogger<DashboardDataService> logger,
        TSKConfig config,
        MetricsService metricsService,
        HealthCheckService healthCheckService)
    {
        _logger = logger;
        _config = config;
        _metricsService = metricsService;
        _healthCheckService = healthCheckService;
    }
    
    public async Task<DashboardData> GetDashboardDataAsync()
    {
        var dashboardData = new DashboardData
        {
            Timestamp = DateTime.UtcNow,
            ServiceName = _config.Get<string>("app.name", "unknown"),
            Environment = _config.Get<string>("app.environment", "unknown")
        };
        
        // Get health status
        var healthContext = new HealthCheckContext();
        dashboardData.HealthStatus = await _healthCheckService.CheckHealthAsync(healthContext);
        
        // Get metrics data
        var metricsReport = _metricsService.GenerateReport();
        dashboardData.Metrics = metricsReport;
        
        // Get system information
        dashboardData.SystemInfo = await GetSystemInfoAsync();
        
        // Get recent alerts
        dashboardData.RecentAlerts = await GetRecentAlertsAsync();
        
        return dashboardData;
    }
    
    private async Task<SystemInfo> GetSystemInfoAsync()
    {
        var process = Process.GetCurrentProcess();
        
        return new SystemInfo
        {
            ProcessId = process.Id,
            WorkingSetMB = process.WorkingSet64 / 1024.0 / 1024.0,
            PrivateMemoryMB = process.PrivateMemorySize64 / 1024.0 / 1024.0,
            VirtualMemoryMB = process.VirtualMemorySize64 / 1024.0 / 1024.0,
            CpuTime = process.TotalProcessorTime,
            ThreadCount = process.Threads.Count,
            HandleCount = process.HandleCount,
            StartTime = process.StartTime,
            Uptime = DateTime.UtcNow - process.StartTime
        };
    }
    
    private async Task<List<Alert>> GetRecentAlertsAsync()
    {
        // This would typically query an alert storage system
        // For now, return an empty list
        await Task.CompletedTask;
        return new List<Alert>();
    }
}

public class DashboardData
{
    public DateTime Timestamp { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public HealthCheckResult HealthStatus { get; set; } = new();
    public MetricsReport Metrics { get; set; } = new();
    public SystemInfo SystemInfo { get; set; } = new();
    public List<Alert> RecentAlerts { get; set; } = new();
}

public class SystemInfo
{
    public int ProcessId { get; set; }
    public double WorkingSetMB { get; set; }
    public double PrivateMemoryMB { get; set; }
    public double VirtualMemoryMB { get; set; }
    public TimeSpan CpuTime { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public DateTime StartTime { get; set; }
    public TimeSpan Uptime { get; set; }
}
```

## 📝 Summary

This guide covered comprehensive monitoring strategies for C# TuskLang applications:

- **Metrics Collection**: Metrics service with various metric types and reporting
- **Health Checks**: Comprehensive health check service for system monitoring
- **Distributed Tracing**: Tracing service for request flow analysis
- **Alerting System**: Configurable alert rules and notification system
- **Monitoring Dashboard**: Dashboard data service for operational insights

These monitoring strategies ensure your C# TuskLang applications have comprehensive observability and operational excellence. 