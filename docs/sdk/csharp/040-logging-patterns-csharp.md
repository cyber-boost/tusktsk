# Logging Patterns in C# TuskLang

## Overview

Effective logging is essential for monitoring, debugging, and maintaining TuskLang applications. This guide covers structured logging, correlation IDs, log aggregation, and analysis strategies for C# applications.

## 📝 Structured Logging

### Logging Configuration

```csharp
public class LoggingConfiguration
{
    public static void ConfigureLogging(IServiceCollection services, TSKConfig config)
    {
        var logLevel = config.Get<string>("logging.level", "Information");
        var logFormat = config.Get<string>("logging.format", "json");
        var logFilePath = config.Get<string>("logging.file_path", "logs/app.log");
        
        services.AddLogging(builder =>
        {
            // Set minimum log level
            builder.SetMinimumLevel(ParseLogLevel(logLevel));
            
            // Console logging
            builder.AddConsole(options =>
            {
                options.FormatterName = logFormat == "json" ? "json" : "simple";
            });
            
            // File logging
            builder.AddFile(logFilePath, options =>
            {
                options.FileSizeLimitBytes = config.Get<long>("logging.max_file_size_mb", 100) * 1024 * 1024;
                options.RetainedFileCountLimit = config.Get<int>("logging.max_files", 10);
                options.Append = true;
                options.FormatLogEntry = (msg) => $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{msg.LogLevel}] {msg.Message}";
            });
            
            // JSON formatter for structured logging
            if (logFormat == "json")
            {
                builder.AddJsonConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
                });
            }
        });
    }
    
    private static LogLevel ParseLogLevel(string level)
    {
        return level.ToLower() switch
        {
            "trace" => LogLevel.Trace,
            "debug" => LogLevel.Debug,
            "information" => LogLevel.Information,
            "warning" => LogLevel.Warning,
            "error" => LogLevel.Error,
            "critical" => LogLevel.Critical,
            _ => LogLevel.Information
        };
    }
}
```

### Structured Logger Service

```csharp
public class StructuredLogger<T>
{
    private readonly ILogger<T> _logger;
    private readonly TSKConfig _config;
    private readonly Dictionary<string, object> _defaultContext;
    
    public StructuredLogger(ILogger<T> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        _defaultContext = new Dictionary<string, object>
        {
            ["service"] = typeof(T).Name,
            ["environment"] = _config.Get<string>("app.environment", "unknown"),
            ["version"] = _config.Get<string>("app.version", "1.0.0")
        };
    }
    
    public void LogInformation(string message, Dictionary<string, object>? context = null)
    {
        var logContext = MergeContext(context);
        _logger.LogInformation("Information: {Message} {@Context}", message, logContext);
    }
    
    public void LogWarning(string message, Dictionary<string, object>? context = null)
    {
        var logContext = MergeContext(context);
        _logger.LogWarning("Warning: {Message} {@Context}", message, logContext);
    }
    
    public void LogError(Exception exception, string message, Dictionary<string, object>? context = null)
    {
        var logContext = MergeContext(context);
        logContext["exception_type"] = exception.GetType().Name;
        logContext["exception_message"] = exception.Message;
        logContext["stack_trace"] = exception.StackTrace;
        
        if (exception.InnerException != null)
        {
            logContext["inner_exception"] = exception.InnerException.Message;
        }
        
        _logger.LogError(exception, "Error: {Message} {@Context}", message, logContext);
    }
    
    public void LogPerformance(string operation, TimeSpan duration, Dictionary<string, object>? context = null)
    {
        var logContext = MergeContext(context);
        logContext["operation"] = operation;
        logContext["duration_ms"] = duration.TotalMilliseconds;
        logContext["duration_seconds"] = duration.TotalSeconds;
        
        var logLevel = duration.TotalSeconds > 1 ? LogLevel.Warning : LogLevel.Information;
        _logger.Log(logLevel, "Performance: {Operation} completed in {Duration}ms {@Context}", 
            operation, duration.TotalMilliseconds, logContext);
    }
    
    public void LogDatabaseOperation(string operation, string query, TimeSpan duration, Dictionary<string, object>? context = null)
    {
        var logContext = MergeContext(context);
        logContext["db_operation"] = operation;
        logContext["db_query"] = query;
        logContext["db_duration_ms"] = duration.TotalMilliseconds;
        
        var logLevel = duration.TotalSeconds > 0.5 ? LogLevel.Warning : LogLevel.Debug;
        _logger.Log(logLevel, "Database: {Operation} completed in {Duration}ms {@Context}", 
            operation, duration.TotalMilliseconds, logContext);
    }
    
    public void LogApiCall(string method, string url, int statusCode, TimeSpan duration, Dictionary<string, object>? context = null)
    {
        var logContext = MergeContext(context);
        logContext["http_method"] = method;
        logContext["http_url"] = url;
        logContext["http_status_code"] = statusCode;
        logContext["http_duration_ms"] = duration.TotalMilliseconds;
        
        var logLevel = statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
        _logger.Log(logLevel, "API: {Method} {Url} returned {StatusCode} in {Duration}ms {@Context}", 
            method, url, statusCode, duration.TotalMilliseconds, logContext);
    }
    
    public void LogConfigurationChange(string key, object? oldValue, object? newValue, Dictionary<string, object>? context = null)
    {
        var logContext = MergeContext(context);
        logContext["config_key"] = key;
        logContext["config_old_value"] = oldValue;
        logContext["config_new_value"] = newValue;
        
        _logger.LogInformation("Configuration: {Key} changed from {OldValue} to {NewValue} {@Context}", 
            key, oldValue, newValue, logContext);
    }
    
    private Dictionary<string, object> MergeContext(Dictionary<string, object>? additionalContext)
    {
        var merged = new Dictionary<string, object>(_defaultContext);
        
        if (additionalContext != null)
        {
            foreach (var kvp in additionalContext)
            {
                merged[kvp.Key] = kvp.Value;
            }
        }
        
        return merged;
    }
}
```

## 🔗 Correlation IDs

### Correlation ID Middleware

```csharp
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    
    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        
        // Add correlation ID to response headers
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        
        // Add correlation ID to log context
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["correlation_id"] = correlationId,
            ["request_path"] = context.Request.Path,
            ["request_method"] = context.Request.Method,
            ["user_agent"] = context.Request.Headers["User-Agent"].ToString(),
            ["client_ip"] = context.Connection.RemoteIpAddress?.ToString()
        }))
        {
            _logger.LogInformation("Request started: {Method} {Path}", 
                context.Request.Method, context.Request.Path);
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Request failed: {Method} {Path}", 
                    context.Request.Method, context.Request.Path);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogInformation("Request completed: {Method} {Path} - {StatusCode} in {Duration}ms", 
                    context.Request.Method, context.Request.Path, 
                    context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
            }
        }
    }
    
    private string GetOrCreateCorrelationId(HttpContext context)
    {
        // Check if correlation ID is provided in request headers
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            return correlationId.ToString();
        }
        
        // Check if correlation ID is provided in query string
        if (context.Request.Query.TryGetValue("correlationId", out var queryCorrelationId))
        {
            return queryCorrelationId.ToString();
        }
        
        // Generate new correlation ID
        return Guid.NewGuid().ToString();
    }
}

public class CorrelationIdService
{
    private readonly AsyncLocal<string> _correlationId = new();
    
    public string GetCorrelationId()
    {
        return _correlationId.Value ?? Guid.NewGuid().ToString();
    }
    
    public void SetCorrelationId(string correlationId)
    {
        _correlationId.Value = correlationId;
    }
    
    public IDisposable CreateScope(string correlationId)
    {
        var previousId = _correlationId.Value;
        _correlationId.Value = correlationId;
        
        return new CorrelationIdScope(previousId, this);
    }
    
    private class CorrelationIdScope : IDisposable
    {
        private readonly string _previousId;
        private readonly CorrelationIdService _service;
        
        public CorrelationIdScope(string previousId, CorrelationIdService service)
        {
            _previousId = previousId;
            _service = service;
        }
        
        public void Dispose()
        {
            _service._correlationId.Value = _previousId;
        }
    }
}
```

### Scoped Logging

```csharp
public class ScopedLogger<T>
{
    private readonly ILogger<T> _logger;
    private readonly CorrelationIdService _correlationIdService;
    private readonly Dictionary<string, object> _scopeData;
    
    public ScopedLogger(ILogger<T> logger, CorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
        _scopeData = new Dictionary<string, object>
        {
            ["correlation_id"] = _correlationIdService.GetCorrelationId(),
            ["service"] = typeof(T).Name
        };
    }
    
    public IDisposable BeginScope(string operation, Dictionary<string, object>? additionalData = null)
    {
        var scopeData = new Dictionary<string, object>(_scopeData)
        {
            ["operation"] = operation
        };
        
        if (additionalData != null)
        {
            foreach (var kvp in additionalData)
            {
                scopeData[kvp.Key] = kvp.Value;
            }
        }
        
        return _logger.BeginScope(scopeData);
    }
    
    public void LogOperationStart(string operation, Dictionary<string, object>? context = null)
    {
        var logContext = MergeContext(context);
        logContext["operation"] = operation;
        logContext["event"] = "operation_start";
        
        _logger.LogInformation("Operation started: {Operation} {@Context}", operation, logContext);
    }
    
    public void LogOperationEnd(string operation, TimeSpan duration, Dictionary<string, object>? context = null)
    {
        var logContext = MergeContext(context);
        logContext["operation"] = operation;
        logContext["duration_ms"] = duration.TotalMilliseconds;
        logContext["event"] = "operation_end";
        
        _logger.LogInformation("Operation completed: {Operation} in {Duration}ms {@Context}", 
            operation, duration.TotalMilliseconds, logContext);
    }
    
    public void LogOperationError(string operation, Exception exception, Dictionary<string, object>? context = null)
    {
        var logContext = MergeContext(context);
        logContext["operation"] = operation;
        logContext["event"] = "operation_error";
        logContext["exception_type"] = exception.GetType().Name;
        logContext["exception_message"] = exception.Message;
        
        _logger.LogError(exception, "Operation failed: {Operation} {@Context}", operation, logContext);
    }
    
    private Dictionary<string, object> MergeContext(Dictionary<string, object>? additionalContext)
    {
        var merged = new Dictionary<string, object>(_scopeData);
        
        if (additionalContext != null)
        {
            foreach (var kvp in additionalContext)
            {
                merged[kvp.Key] = kvp.Value;
            }
        }
        
        return merged;
    }
}
```

## 📊 Log Aggregation

### Log Aggregator Service

```csharp
public class LogAggregatorService
{
    private readonly ILogger<LogAggregatorService> _logger;
    private readonly TSKConfig _config;
    private readonly ConcurrentQueue<LogEntry> _logBuffer;
    private readonly Timer _flushTimer;
    private readonly int _bufferSize;
    private readonly TimeSpan _flushInterval;
    
    public LogAggregatorService(ILogger<LogAggregatorService> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        _logBuffer = new ConcurrentQueue<LogEntry>();
        
        _bufferSize = config.Get<int>("logging.buffer_size", 1000);
        _flushInterval = TimeSpan.FromSeconds(config.Get<int>("logging.flush_interval_seconds", 30));
        
        _flushTimer = new Timer(FlushLogs, null, _flushInterval, _flushInterval);
    }
    
    public void AddLogEntry(LogEntry entry)
    {
        _logBuffer.Enqueue(entry);
        
        if (_logBuffer.Count >= _bufferSize)
        {
            FlushLogs(null);
        }
    }
    
    private async void FlushLogs(object? state)
    {
        if (_logBuffer.IsEmpty)
        {
            return;
        }
        
        var logs = new List<LogEntry>();
        while (_logBuffer.TryDequeue(out var entry))
        {
            logs.Add(entry);
        }
        
        try
        {
            await SendLogsToAggregatorAsync(logs);
            _logger.LogDebug("Flushed {Count} log entries to aggregator", logs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush logs to aggregator");
            
            // Re-queue logs for retry
            foreach (var log in logs)
            {
                _logBuffer.Enqueue(log);
            }
        }
    }
    
    private async Task SendLogsToAggregatorAsync(List<LogEntry> logs)
    {
        var aggregatorUrl = _config.Get<string>("logging.aggregator_url");
        if (string.IsNullOrEmpty(aggregatorUrl))
        {
            return;
        }
        
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        
        var payload = new LogBatch
        {
            ServiceName = _config.Get<string>("app.name", "unknown"),
            Environment = _config.Get<string>("app.environment", "unknown"),
            Timestamp = DateTime.UtcNow,
            Logs = logs
        };
        
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await client.PostAsync(aggregatorUrl, content);
        response.EnsureSuccessStatusCode();
    }
    
    public void Dispose()
    {
        _flushTimer?.Dispose();
        FlushLogs(null); // Final flush
    }
}

public class LogEntry
{
    public string CorrelationId { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
    public Exception? Exception { get; set; }
}

public class LogBatch
{
    public string ServiceName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public List<LogEntry> Logs { get; set; } = new();
}
```

### Log Filtering and Sampling

```csharp
public class LogFilterService
{
    private readonly TSKConfig _config;
    private readonly Dictionary<string, LogLevel> _categoryLevels;
    private readonly List<string> _excludedPaths;
    private readonly double _samplingRate;
    
    public LogFilterService(TSKConfig config)
    {
        _config = config;
        _categoryLevels = LoadCategoryLevels();
        _excludedPaths = LoadExcludedPaths();
        _samplingRate = config.Get<double>("logging.sampling_rate", 1.0);
    }
    
    public bool ShouldLog(string category, LogLevel level, string? path = null)
    {
        // Check if path is excluded
        if (!string.IsNullOrEmpty(path) && _excludedPaths.Any(excluded => path.StartsWith(excluded)))
        {
            return false;
        }
        
        // Check category-specific log level
        if (_categoryLevels.TryGetValue(category, out var categoryLevel))
        {
            return level >= categoryLevel;
        }
        
        // Check global log level
        var globalLevel = ParseLogLevel(_config.Get<string>("logging.level", "Information"));
        return level >= globalLevel;
    }
    
    public bool ShouldSample(string category, LogLevel level)
    {
        // Always log errors and critical messages
        if (level >= LogLevel.Error)
        {
            return true;
        }
        
        // Apply sampling rate for other levels
        return Random.Shared.NextDouble() <= _samplingRate;
    }
    
    private Dictionary<string, LogLevel> LoadCategoryLevels()
    {
        var levels = new Dictionary<string, LogLevel>();
        var categoryConfig = _config.GetSection("logging.categories");
        
        if (categoryConfig != null)
        {
            foreach (var key in categoryConfig.GetKeys())
            {
                var level = categoryConfig.Get<string>(key);
                if (!string.IsNullOrEmpty(level))
                {
                    levels[key] = ParseLogLevel(level);
                }
            }
        }
        
        return levels;
    }
    
    private List<string> LoadExcludedPaths()
    {
        var excluded = new List<string>();
        var excludedConfig = _config.GetSection("logging.excluded_paths");
        
        if (excludedConfig != null)
        {
            foreach (var key in excludedConfig.GetKeys())
            {
                var path = excludedConfig.Get<string>(key);
                if (!string.IsNullOrEmpty(path))
                {
                    excluded.Add(path);
                }
            }
        }
        
        return excluded;
    }
    
    private static LogLevel ParseLogLevel(string level)
    {
        return level.ToLower() switch
        {
            "trace" => LogLevel.Trace,
            "debug" => LogLevel.Debug,
            "information" => LogLevel.Information,
            "warning" => LogLevel.Warning,
            "error" => LogLevel.Error,
            "critical" => LogLevel.Critical,
            _ => LogLevel.Information
        };
    }
}
```

## 📈 Log Analysis

### Log Analytics Service

```csharp
public class LogAnalyticsService
{
    private readonly ILogger<LogAnalyticsService> _logger;
    private readonly TSKConfig _config;
    private readonly Dictionary<string, LogMetrics> _metrics;
    private readonly Timer _analysisTimer;
    
    public LogAnalyticsService(ILogger<LogAnalyticsService> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        _metrics = new Dictionary<string, LogMetrics>();
        
        var analysisInterval = TimeSpan.FromMinutes(config.Get<int>("logging.analysis_interval_minutes", 5));
        _analysisTimer = new Timer(PerformAnalysis, null, analysisInterval, analysisInterval);
    }
    
    public void RecordLogEntry(string category, LogLevel level, string operation, TimeSpan? duration = null)
    {
        lock (_metrics)
        {
            if (!_metrics.ContainsKey(category))
            {
                _metrics[category] = new LogMetrics();
            }
            
            var metrics = _metrics[category];
            metrics.TotalLogs++;
            
            if (level == LogLevel.Error || level == LogLevel.Critical)
            {
                metrics.ErrorCount++;
            }
            
            if (!string.IsNullOrEmpty(operation))
            {
                if (!metrics.OperationCounts.ContainsKey(operation))
                {
                    metrics.OperationCounts[operation] = 0;
                }
                metrics.OperationCounts[operation]++;
            }
            
            if (duration.HasValue)
            {
                metrics.TotalDuration += duration.Value;
                metrics.OperationCount++;
                
                if (duration.Value > metrics.MaxDuration)
                {
                    metrics.MaxDuration = duration.Value;
                }
                
                if (duration.Value < metrics.MinDuration || metrics.MinDuration == TimeSpan.Zero)
                {
                    metrics.MinDuration = duration.Value;
                }
            }
        }
    }
    
    private async void PerformAnalysis(object? state)
    {
        try
        {
            var analysis = await GenerateLogAnalysisAsync();
            await SendAnalysisToMonitoringAsync(analysis);
            
            _logger.LogInformation("Log analysis completed: {TotalCategories} categories analyzed", 
                analysis.Categories.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform log analysis");
        }
    }
    
    private async Task<LogAnalysis> GenerateLogAnalysisAsync()
    {
        var analysis = new LogAnalysis
        {
            Timestamp = DateTime.UtcNow,
            ServiceName = _config.Get<string>("app.name", "unknown"),
            Environment = _config.Get<string>("app.environment", "unknown")
        };
        
        lock (_metrics)
        {
            foreach (var kvp in _metrics)
            {
                var categoryMetrics = kvp.Value;
                var categoryAnalysis = new CategoryAnalysis
                {
                    Category = kvp.Key,
                    TotalLogs = categoryMetrics.TotalLogs,
                    ErrorCount = categoryMetrics.ErrorCount,
                    ErrorRate = categoryMetrics.TotalLogs > 0 
                        ? (double)categoryMetrics.ErrorCount / categoryMetrics.TotalLogs 
                        : 0,
                    AverageDuration = categoryMetrics.OperationCount > 0 
                        ? categoryMetrics.TotalDuration.TotalMilliseconds / categoryMetrics.OperationCount 
                        : 0,
                    MaxDuration = categoryMetrics.MaxDuration.TotalMilliseconds,
                    MinDuration = categoryMetrics.MinDuration.TotalMilliseconds,
                    TopOperations = categoryMetrics.OperationCounts
                        .OrderByDescending(x => x.Value)
                        .Take(10)
                        .ToDictionary(x => x.Key, x => x.Value)
                };
                
                analysis.Categories.Add(categoryAnalysis);
            }
        }
        
        return analysis;
    }
    
    private async Task SendAnalysisToMonitoringAsync(LogAnalysis analysis)
    {
        var monitoringUrl = _config.Get<string>("logging.monitoring_url");
        if (string.IsNullOrEmpty(monitoringUrl))
        {
            return;
        }
        
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        
        var json = JsonSerializer.Serialize(analysis);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await client.PostAsync(monitoringUrl, content);
        response.EnsureSuccessStatusCode();
    }
    
    public void Dispose()
    {
        _analysisTimer?.Dispose();
    }
}

public class LogMetrics
{
    public int TotalLogs { get; set; }
    public int ErrorCount { get; set; }
    public Dictionary<string, int> OperationCounts { get; set; } = new();
    public TimeSpan TotalDuration { get; set; }
    public int OperationCount { get; set; }
    public TimeSpan MaxDuration { get; set; }
    public TimeSpan MinDuration { get; set; }
}

public class LogAnalysis
{
    public DateTime Timestamp { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public List<CategoryAnalysis> Categories { get; set; } = new();
}

public class CategoryAnalysis
{
    public string Category { get; set; } = string.Empty;
    public int TotalLogs { get; set; }
    public int ErrorCount { get; set; }
    public double ErrorRate { get; set; }
    public double AverageDuration { get; set; }
    public double MaxDuration { get; set; }
    public double MinDuration { get; set; }
    public Dictionary<string, int> TopOperations { get; set; } = new();
}
```

## 🔍 Log Search and Query

### Log Query Service

```csharp
public class LogQueryService
{
    private readonly ILogger<LogQueryService> _logger;
    private readonly TSKConfig _config;
    private readonly HttpClient _httpClient;
    
    public LogQueryService(ILogger<LogQueryService> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }
    
    public async Task<List<LogEntry>> SearchLogsAsync(LogSearchCriteria criteria)
    {
        var searchUrl = _config.Get<string>("logging.search_url");
        if (string.IsNullOrEmpty(searchUrl))
        {
            throw new InvalidOperationException("Log search URL not configured");
        }
        
        var queryParams = new List<string>();
        
        if (!string.IsNullOrEmpty(criteria.CorrelationId))
        {
            queryParams.Add($"correlationId={Uri.EscapeDataString(criteria.CorrelationId)}");
        }
        
        if (!string.IsNullOrEmpty(criteria.Level))
        {
            queryParams.Add($"level={Uri.EscapeDataString(criteria.Level)}");
        }
        
        if (!string.IsNullOrEmpty(criteria.Service))
        {
            queryParams.Add($"service={Uri.EscapeDataString(criteria.Service)}");
        }
        
        if (!string.IsNullOrEmpty(criteria.Operation))
        {
            queryParams.Add($"operation={Uri.EscapeDataString(criteria.Operation)}");
        }
        
        if (criteria.StartTime.HasValue)
        {
            queryParams.Add($"startTime={criteria.StartTime.Value:yyyy-MM-ddTHH:mm:ssZ}");
        }
        
        if (criteria.EndTime.HasValue)
        {
            queryParams.Add($"endTime={criteria.EndTime.Value:yyyy-MM-ddTHH:mm:ssZ}");
        }
        
        if (criteria.Limit.HasValue)
        {
            queryParams.Add($"limit={criteria.Limit.Value}");
        }
        
        var url = $"{searchUrl}?{string.Join("&", queryParams)}";
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var searchResult = JsonSerializer.Deserialize<LogSearchResult>(content);
        
        return searchResult?.Logs ?? new List<LogEntry>();
    }
    
    public async Task<List<LogEntry>> GetLogsByCorrelationIdAsync(string correlationId)
    {
        var criteria = new LogSearchCriteria
        {
            CorrelationId = correlationId,
            Limit = 100
        };
        
        return await SearchLogsAsync(criteria);
    }
    
    public async Task<List<LogEntry>> GetErrorLogsAsync(DateTime? startTime = null, DateTime? endTime = null)
    {
        var criteria = new LogSearchCriteria
        {
            Level = "Error",
            StartTime = startTime,
            EndTime = endTime,
            Limit = 100
        };
        
        return await SearchLogsAsync(criteria);
    }
    
    public async Task<LogStatistics> GetLogStatisticsAsync(DateTime startTime, DateTime endTime)
    {
        var statsUrl = _config.Get<string>("logging.stats_url");
        if (string.IsNullOrEmpty(statsUrl))
        {
            throw new InvalidOperationException("Log statistics URL not configured");
        }
        
        var url = $"{statsUrl}?startTime={startTime:yyyy-MM-ddTHH:mm:ssZ}&endTime={endTime:yyyy-MM-ddTHH:mm:ssZ}";
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<LogStatistics>(content) ?? new LogStatistics();
    }
}

public class LogSearchCriteria
{
    public string? CorrelationId { get; set; }
    public string? Level { get; set; }
    public string? Service { get; set; }
    public string? Operation { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? Limit { get; set; }
}

public class LogSearchResult
{
    public List<LogEntry> Logs { get; set; } = new();
    public int TotalCount { get; set; }
    public bool HasMore { get; set; }
}

public class LogStatistics
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TotalLogs { get; set; }
    public int ErrorLogs { get; set; }
    public int WarningLogs { get; set; }
    public int InfoLogs { get; set; }
    public Dictionary<string, int> LogsByService { get; set; } = new();
    public Dictionary<string, int> LogsByLevel { get; set; } = new();
    public double AverageResponseTime { get; set; }
    public double MaxResponseTime { get; set; }
}
```

## 📝 Summary

This guide covered comprehensive logging patterns for C# TuskLang applications:

- **Structured Logging**: Configuration and structured logger service with context
- **Correlation IDs**: Middleware and service for request tracing
- **Log Aggregation**: Buffered log aggregation with batch processing
- **Log Filtering**: Category-based filtering and sampling strategies
- **Log Analysis**: Metrics collection and analysis for operational insights
- **Log Search**: Query service for searching and analyzing logs

These logging patterns ensure your C# TuskLang applications have comprehensive observability and debugging capabilities. 