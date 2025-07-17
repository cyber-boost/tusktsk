# 📝 Logging Patterns - TuskLang for C# - "Logging Mastery"

**Master logging patterns with TuskLang in your C# applications!**

Logging is essential for debugging, monitoring, and auditing. This guide covers structured logging, log levels, log aggregation, correlation IDs, and real-world logging scenarios for TuskLang in C# environments.

## 📋 Logging Philosophy

### "We Don't Bow to Any King"
- **Log everything** - Every action, every decision
- **Structured data** - JSON logs for easy parsing
- **Context matters** - Include relevant metadata
- **Performance first** - Async logging, no blocking
- **Searchable logs** - Make logs easy to find and analyze

## 🏗️ Structured Logging

### Example: Structured Logging Service
```csharp
// StructuredLoggingService.cs
using Serilog;
using Serilog.Events;

public class StructuredLoggingService
{
    private readonly ILogger _logger;
    private readonly TuskLang _parser;
    private readonly Dictionary<string, object> _globalProperties;
    
    public StructuredLoggingService(ILogger logger)
    {
        _logger = logger;
        _parser = new TuskLang();
        _globalProperties = new Dictionary<string, object>();
        
        LoadLoggingConfiguration();
    }
    
    private void LoadLoggingConfiguration()
    {
        var config = _parser.ParseFile("config/logging.tsk");
        
        // Set global properties
        _globalProperties["application"] = config["application_name"].ToString();
        _globalProperties["version"] = config["version"].ToString();
        _globalProperties["environment"] = config["environment"].ToString();
        
        // Configure Serilog
        var logConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", _globalProperties["application"])
            .Enrich.WithProperty("Version", _globalProperties["version"])
            .Enrich.WithProperty("Environment", _globalProperties["environment"]);
        
        // Add sinks based on configuration
        var sinks = config["sinks"] as Dictionary<string, object>;
        foreach (var sink in sinks ?? new Dictionary<string, object>())
        {
            ConfigureSink(logConfig, sink.Key, sink.Value as Dictionary<string, object>);
        }
        
        Log.Logger = logConfig.CreateLogger();
    }
    
    private void ConfigureSink(LoggerConfiguration logConfig, string sinkName, Dictionary<string, object>? sinkConfig)
    {
        switch (sinkName.ToLower())
        {
            case "console":
                logConfig.WriteTo.Console(
                    outputTemplate: sinkConfig?["output_template"]?.ToString() ?? 
                        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                );
                break;
                
            case "file":
                logConfig.WriteTo.File(
                    path: sinkConfig?["path"]?.ToString() ?? "logs/app-.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: sinkConfig?["output_template"]?.ToString() ?? 
                        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                );
                break;
                
            case "elasticsearch":
                logConfig.WriteTo.Elasticsearch(
                    new Uri(sinkConfig?["url"]?.ToString() ?? "http://localhost:9200"),
                    indexFormat: sinkConfig?["index_format"]?.ToString() ?? "logs-{0:yyyy.MM.dd}"
                );
                break;
        }
    }
    
    public void LogInformation(string message, Dictionary<string, object>? properties = null)
    {
        var enrichedProperties = MergeProperties(_globalProperties, properties);
        _logger.Information(message, enrichedProperties);
    }
    
    public void LogWarning(string message, Dictionary<string, object>? properties = null)
    {
        var enrichedProperties = MergeProperties(_globalProperties, properties);
        _logger.Warning(message, enrichedProperties);
    }
    
    public void LogError(string message, Exception? exception = null, Dictionary<string, object>? properties = null)
    {
        var enrichedProperties = MergeProperties(_globalProperties, properties);
        
        if (exception != null)
        {
            _logger.Error(exception, message, enrichedProperties);
        }
        else
        {
            _logger.Error(message, enrichedProperties);
        }
    }
    
    public void LogDebug(string message, Dictionary<string, object>? properties = null)
    {
        var enrichedProperties = MergeProperties(_globalProperties, properties);
        _logger.Debug(message, enrichedProperties);
    }
    
    private Dictionary<string, object> MergeProperties(Dictionary<string, object> global, Dictionary<string, object>? local)
    {
        var merged = new Dictionary<string, object>(global);
        if (local != null)
        {
            foreach (var kvp in local)
            {
                merged[kvp.Key] = kvp.Value;
            }
        }
        return merged;
    }
}
```

## 🔗 Correlation ID Pattern

### Example: Correlation ID Service
```csharp
// CorrelationIdService.cs
using System.Diagnostics;

public class CorrelationIdService
{
    private readonly AsyncLocal<string> _correlationId = new AsyncLocal<string>();
    private readonly ILogger<CorrelationIdService> _logger;
    
    public CorrelationIdService(ILogger<CorrelationIdService> logger)
    {
        _logger = logger;
    }
    
    public string GetCorrelationId()
    {
        if (string.IsNullOrEmpty(_correlationId.Value))
        {
            _correlationId.Value = GenerateCorrelationId();
            _logger.LogDebug("Generated new correlation ID: {CorrelationId}", _correlationId.Value);
        }
        
        return _correlationId.Value;
    }
    
    public void SetCorrelationId(string correlationId)
    {
        _correlationId.Value = correlationId;
        _logger.LogDebug("Set correlation ID: {CorrelationId}", correlationId);
    }
    
    public IDisposable CreateScope(string operationName)
    {
        var correlationId = GetCorrelationId();
        return new CorrelationScope(correlationId, operationName, _logger);
    }
    
    private string GenerateCorrelationId()
    {
        return $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
    }
}

public class CorrelationScope : IDisposable
{
    private readonly string _correlationId;
    private readonly string _operationName;
    private readonly ILogger _logger;
    private readonly Stopwatch _stopwatch;
    
    public CorrelationScope(string correlationId, string operationName, ILogger logger)
    {
        _correlationId = correlationId;
        _operationName = operationName;
        _logger = logger;
        _stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("Starting operation {OperationName} with correlation ID {CorrelationId}", 
            operationName, correlationId);
    }
    
    public void Dispose()
    {
        _stopwatch.Stop();
        _logger.LogInformation("Completed operation {OperationName} with correlation ID {CorrelationId} in {ElapsedMs}ms", 
            _operationName, _correlationId, _stopwatch.ElapsedMilliseconds);
    }
}
```

## 📊 Log Aggregation

### Example: Log Aggregation Service
```csharp
// LogAggregationService.cs
public class LogAggregationService
{
    private readonly StructuredLoggingService _loggingService;
    private readonly CorrelationIdService _correlationService;
    private readonly ILogger<LogAggregationService> _logger;
    private readonly Dictionary<string, LogMetrics> _metrics;
    
    public LogAggregationService(
        StructuredLoggingService loggingService,
        CorrelationIdService correlationService,
        ILogger<LogAggregationService> logger)
    {
        _loggingService = loggingService;
        _correlationService = correlationService;
        _logger = logger;
        _metrics = new Dictionary<string, LogMetrics>();
    }
    
    public void LogApiRequest(string method, string path, int statusCode, long durationMs)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var properties = new Dictionary<string, object>
        {
            ["correlation_id"] = correlationId,
            ["http_method"] = method,
            ["http_path"] = path,
            ["http_status_code"] = statusCode,
            ["duration_ms"] = durationMs,
            ["log_type"] = "api_request"
        };
        
        var level = statusCode >= 400 ? "Warning" : "Information";
        var message = $"{method} {path} returned {statusCode} in {durationMs}ms";
        
        switch (level)
        {
            case "Warning":
                _loggingService.LogWarning(message, properties);
                break;
            default:
                _loggingService.LogInformation(message, properties);
                break;
        }
        
        UpdateMetrics("api_requests", properties);
    }
    
    public void LogDatabaseOperation(string operation, string table, long durationMs, bool success)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var properties = new Dictionary<string, object>
        {
            ["correlation_id"] = correlationId,
            ["db_operation"] = operation,
            ["db_table"] = table,
            ["duration_ms"] = durationMs,
            ["success"] = success,
            ["log_type"] = "database_operation"
        };
        
        var level = success ? "Information" : "Error";
        var message = $"Database {operation} on {table} {(success ? "succeeded" : "failed")} in {durationMs}ms";
        
        switch (level)
        {
            case "Error":
                _loggingService.LogError(message, properties: properties);
                break;
            default:
                _loggingService.LogInformation(message, properties);
                break;
        }
        
        UpdateMetrics("database_operations", properties);
    }
    
    public void LogBusinessEvent(string eventType, string entityType, string entityId, Dictionary<string, object>? additionalData = null)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var properties = new Dictionary<string, object>
        {
            ["correlation_id"] = correlationId,
            ["event_type"] = eventType,
            ["entity_type"] = entityType,
            ["entity_id"] = entityId,
            ["log_type"] = "business_event"
        };
        
        if (additionalData != null)
        {
            foreach (var kvp in additionalData)
            {
                properties[kvp.Key] = kvp.Value;
            }
        }
        
        var message = $"Business event {eventType} for {entityType} {entityId}";
        _loggingService.LogInformation(message, properties);
        
        UpdateMetrics("business_events", properties);
    }
    
    private void UpdateMetrics(string metricType, Dictionary<string, object> properties)
    {
        if (!_metrics.ContainsKey(metricType))
        {
            _metrics[metricType] = new LogMetrics();
        }
        
        var metrics = _metrics[metricType];
        metrics.Count++;
        metrics.LastOccurrence = DateTime.UtcNow;
        
        if (properties.ContainsKey("duration_ms"))
        {
            var duration = Convert.ToInt64(properties["duration_ms"]);
            metrics.TotalDuration += duration;
            metrics.AverageDuration = metrics.TotalDuration / metrics.Count;
        }
    }
    
    public async Task<Dictionary<string, object>> GetLogMetricsAsync()
    {
        var metrics = new Dictionary<string, object>();
        
        foreach (var kvp in _metrics)
        {
            metrics[kvp.Key] = new Dictionary<string, object>
            {
                ["count"] = kvp.Value.Count,
                ["total_duration_ms"] = kvp.Value.TotalDuration,
                ["average_duration_ms"] = kvp.Value.AverageDuration,
                ["last_occurrence"] = kvp.Value.LastOccurrence.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
        
        return metrics;
    }
}

public class LogMetrics
{
    public int Count { get; set; }
    public long TotalDuration { get; set; }
    public double AverageDuration { get; set; }
    public DateTime LastOccurrence { get; set; }
}
```

## 🔍 Log Analysis

### Example: Log Analysis Service
```csharp
// LogAnalysisService.cs
public class LogAnalysisService
{
    private readonly StructuredLoggingService _loggingService;
    private readonly ILogger<LogAnalysisService> _logger;
    private readonly List<LogEntry> _logBuffer;
    private readonly int _bufferSize;
    
    public LogAnalysisService(
        StructuredLoggingService loggingService,
        ILogger<LogAnalysisService> logger)
    {
        _loggingService = loggingService;
        _logger = logger;
        _logBuffer = new List<LogEntry>();
        _bufferSize = 1000;
    }
    
    public void AddLogEntry(LogEntry entry)
    {
        _logBuffer.Add(entry);
        
        if (_logBuffer.Count >= _bufferSize)
        {
            AnalyzeLogs();
            _logBuffer.Clear();
        }
    }
    
    private void AnalyzeLogs()
    {
        var analysis = new Dictionary<string, object>();
        
        // Error rate analysis
        var errorCount = _logBuffer.Count(l => l.Level == "Error");
        var errorRate = (double)errorCount / _logBuffer.Count;
        analysis["error_rate"] = errorRate;
        
        // Performance analysis
        var apiRequests = _logBuffer.Where(l => l.Properties.ContainsKey("log_type") && 
                                               l.Properties["log_type"].ToString() == "api_request").ToList();
        
        if (apiRequests.Any())
        {
            var avgResponseTime = apiRequests.Average(r => Convert.ToInt64(r.Properties["duration_ms"]));
            analysis["avg_api_response_time_ms"] = avgResponseTime;
        }
        
        // Database performance analysis
        var dbOperations = _logBuffer.Where(l => l.Properties.ContainsKey("log_type") && 
                                                l.Properties["log_type"].ToString() == "database_operation").ToList();
        
        if (dbOperations.Any())
        {
            var avgDbTime = dbOperations.Average(r => Convert.ToInt64(r.Properties["duration_ms"]));
            analysis["avg_db_operation_time_ms"] = avgDbTime;
        }
        
        // Log analysis results
        _loggingService.LogInformation("Log analysis completed", analysis);
        
        // Alert on high error rates
        if (errorRate > 0.1) // 10% error rate threshold
        {
            _loggingService.LogWarning("High error rate detected: {ErrorRate:P}", 
                new Dictionary<string, object> { ["error_rate"] = errorRate });
        }
    }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    public Exception? Exception { get; set; }
}
```

## 🛠️ Real-World Logging Scenarios
- **API monitoring**: Log all API requests and responses
- **Database operations**: Log database queries and performance
- **Business events**: Log important business operations
- **Error tracking**: Log and analyze error patterns

## 🧩 Best Practices
- Use structured logging with JSON format
- Include correlation IDs for request tracing
- Log at appropriate levels
- Don't log sensitive data
- Use async logging for performance

## 🏁 You're Ready!

You can now:
- Implement structured logging
- Use correlation IDs for tracing
- Aggregate and analyze logs
- Monitor application performance

**Next:** [Performance Monitoring](034-performance-monitoring-csharp.md)

---

**"We don't bow to any king" - Your logging mastery, your observability excellence, your debugging power.**

Log everything. Debug anything. 📝 