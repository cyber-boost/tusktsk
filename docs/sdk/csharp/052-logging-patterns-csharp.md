# Logging Patterns in C# TuskLang

## Overview

Effective logging is crucial for debugging, monitoring, and maintaining applications. This guide covers structured logging, log levels, log aggregation, and logging best practices for C# TuskLang applications.

## 📝 Structured Logging

### Structured Logger

```csharp
public class StructuredLogger
{
    private readonly ILogger<StructuredLogger> _logger;
    private readonly TSKConfig _config;
    private readonly Dictionary<string, object> _defaultContext;
    
    public StructuredLogger(ILogger<StructuredLogger> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        _defaultContext = new Dictionary<string, object>
        {
            ["service_name"] = _config.Get<string>("app.name", "unknown"),
            ["environment"] = _config.Get<string>("app.environment", "unknown"),
            ["version"] = _config.Get<string>("app.version", "unknown")
        };
    }
    
    public void LogInformation(string message, Dictionary<string, object>? context = null)
    {
        var logContext = MergeContext(context);
        _logger.LogInformation("{Message} {@Context}", message, logContext);
    }
    
    public void LogWarning(string message, Dictionary<string, object>? context = null)
    {
        var logContext = MergeContext(context);
        _logger.LogWarning("{Message} {@Context}", message, logContext);
    }
    
    public void LogError(string message, Exception? exception = null, Dictionary<string, object>? context = null)
    {
        var logContext = MergeContext(context);
        
        if (exception != null)
        {
            _logger.LogError(exception, "{Message} {@Context}", message, logContext);
        }
        else
        {
            _logger.LogError("{Message} {@Context}", message, logContext);
        }
    }
    
    public void LogDebug(string message, Dictionary<string, object>? context = null)
    {
        var logContext = MergeContext(context);
        _logger.LogDebug("{Message} {@Context}", message, logContext);
    }
    
    public void LogTrace(string message, Dictionary<string, object>? context = null)
    {
        var logContext = MergeContext(context);
        _logger.LogTrace("{Message} {@Context}", message, logContext);
    }
    
    public void LogCritical(string message, Exception? exception = null, Dictionary<string, object>? context = null)
    {
        var logContext = MergeContext(context);
        
        if (exception != null)
        {
            _logger.LogCritical(exception, "{Message} {@Context}", message, logContext);
        }
        else
        {
            _logger.LogCritical("{Message} {@Context}", message, logContext);
        }
    }
    
    private Dictionary<string, object> MergeContext(Dictionary<string, object>? additionalContext)
    {
        var mergedContext = new Dictionary<string, object>(_defaultContext);
        
        if (additionalContext != null)
        {
            foreach (var kvp in additionalContext)
            {
                mergedContext[kvp.Key] = kvp.Value;
            }
        }
        
        return mergedContext;
    }
    
    public IDisposable BeginScope(Dictionary<string, object> scopeContext)
    {
        var fullContext = MergeContext(scopeContext);
        return _logger.BeginScope(fullContext);
    }
}

public class LoggingService
{
    private readonly StructuredLogger _logger;
    private readonly TSKConfig _config;
    private readonly ILogAggregator _logAggregator;
    
    public LoggingService(StructuredLogger logger, TSKConfig config, ILogAggregator logAggregator)
    {
        _logger = logger;
        _config = config;
        _logAggregator = logAggregator;
    }
    
    public async Task LogUserActionAsync(string userId, string action, Dictionary<string, object>? details = null)
    {
        var context = new Dictionary<string, object>
        {
            ["user_id"] = userId,
            ["action"] = action,
            ["timestamp"] = DateTime.UtcNow,
            ["session_id"] = GetCurrentSessionId()
        };
        
        if (details != null)
        {
            foreach (var kvp in details)
            {
                context[kvp.Key] = kvp.Value;
            }
        }
        
        _logger.LogInformation("User action performed", context);
        
        // Send to log aggregator
        await _logAggregator.SendLogAsync(new LogEntry
        {
            Level = LogLevel.Information,
            Message = "User action performed",
            Context = context,
            Timestamp = DateTime.UtcNow
        });
    }
    
    public async Task LogSystemEventAsync(string eventName, string description, LogLevel level = LogLevel.Information, Dictionary<string, object>? context = null)
    {
        var logContext = new Dictionary<string, object>
        {
            ["event_name"] = eventName,
            ["description"] = description,
            ["timestamp"] = DateTime.UtcNow
        };
        
        if (context != null)
        {
            foreach (var kvp in context)
            {
                logContext[kvp.Key] = kvp.Value;
            }
        }
        
        switch (level)
        {
            case LogLevel.Information:
                _logger.LogInformation("System event: {EventName}", eventName, logContext);
                break;
            case LogLevel.Warning:
                _logger.LogWarning("System event: {EventName}", eventName, logContext);
                break;
            case LogLevel.Error:
                _logger.LogError("System event: {EventName}", eventName, logContext);
                break;
            case LogLevel.Critical:
                _logger.LogCritical("System event: {EventName}", eventName, logContext);
                break;
        }
        
        // Send to log aggregator
        await _logAggregator.SendLogAsync(new LogEntry
        {
            Level = level,
            Message = $"System event: {eventName}",
            Context = logContext,
            Timestamp = DateTime.UtcNow
        });
    }
    
    public async Task LogPerformanceAsync(string operation, TimeSpan duration, Dictionary<string, object>? context = null)
    {
        var logContext = new Dictionary<string, object>
        {
            ["operation"] = operation,
            ["duration_ms"] = duration.TotalMilliseconds,
            ["timestamp"] = DateTime.UtcNow
        };
        
        if (context != null)
        {
            foreach (var kvp in context)
            {
                logContext[kvp.Key] = kvp.Value;
            }
        }
        
        var level = duration.TotalMilliseconds > 1000 ? LogLevel.Warning : LogLevel.Information;
        
        switch (level)
        {
            case LogLevel.Information:
                _logger.LogInformation("Performance: {Operation} took {Duration}ms", operation, duration.TotalMilliseconds, logContext);
                break;
            case LogLevel.Warning:
                _logger.LogWarning("Performance: {Operation} took {Duration}ms", operation, duration.TotalMilliseconds, logContext);
                break;
        }
        
        // Send to log aggregator
        await _logAggregator.SendLogAsync(new LogEntry
        {
            Level = level,
            Message = $"Performance: {operation} took {duration.TotalMilliseconds}ms",
            Context = logContext,
            Timestamp = DateTime.UtcNow
        });
    }
    
    public async Task LogSecurityEventAsync(string eventType, string description, string? userId = null, Dictionary<string, object>? context = null)
    {
        var logContext = new Dictionary<string, object>
        {
            ["event_type"] = eventType,
            ["description"] = description,
            ["ip_address"] = GetClientIpAddress(),
            ["user_agent"] = GetUserAgent(),
            ["timestamp"] = DateTime.UtcNow
        };
        
        if (!string.IsNullOrEmpty(userId))
        {
            logContext["user_id"] = userId;
        }
        
        if (context != null)
        {
            foreach (var kvp in context)
            {
                logContext[kvp.Key] = kvp.Value;
            }
        }
        
        _logger.LogWarning("Security event: {EventType}", eventType, logContext);
        
        // Send to log aggregator with security flag
        await _logAggregator.SendLogAsync(new LogEntry
        {
            Level = LogLevel.Warning,
            Message = $"Security event: {eventType}",
            Context = logContext,
            Timestamp = DateTime.UtcNow,
            IsSecurityEvent = true
        });
    }
    
    private string? GetCurrentSessionId()
    {
        // Implementation would get session ID from current context
        return null;
    }
    
    private string? GetClientIpAddress()
    {
        // Implementation would get client IP from current context
        return null;
    }
    
    private string? GetUserAgent()
    {
        // Implementation would get user agent from current context
        return null;
    }
}

public class LogEntry
{
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Context { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public bool IsSecurityEvent { get; set; } = false;
    public string? Exception { get; set; }
}
```

### Log Aggregator

```csharp
public interface ILogAggregator
{
    Task SendLogAsync(LogEntry logEntry);
    Task SendBatchAsync(List<LogEntry> logEntries);
    Task FlushAsync();
}

public class LogAggregator : ILogAggregator
{
    private readonly ILogger<LogAggregator> _logger;
    private readonly TSKConfig _config;
    private readonly Channel<LogEntry> _logChannel;
    private readonly Timer _flushTimer;
    private readonly List<LogEntry> _batchBuffer;
    private readonly object _batchLock = new();
    
    public LogAggregator(ILogger<LogAggregator> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        _logChannel = Channel.CreateUnbounded<LogEntry>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        _batchBuffer = new List<LogEntry>();
        
        var flushInterval = TimeSpan.FromSeconds(_config.Get<int>("logging.batch_flush_interval_seconds", 5));
        _flushTimer = new Timer(FlushBatch, null, flushInterval, flushInterval);
        
        // Start background processing
        _ = ProcessLogsAsync();
    }
    
    public async Task SendLogAsync(LogEntry logEntry)
    {
        try
        {
            await _logChannel.Writer.WriteAsync(logEntry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send log entry to aggregator");
        }
    }
    
    public async Task SendBatchAsync(List<LogEntry> logEntries)
    {
        try
        {
            foreach (var logEntry in logEntries)
            {
                await _logChannel.Writer.WriteAsync(logEntry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send log batch to aggregator");
        }
    }
    
    public async Task FlushAsync()
    {
        try
        {
            await FlushBatchAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush log aggregator");
        }
    }
    
    private async Task ProcessLogsAsync()
    {
        try
        {
            await foreach (var logEntry in _logChannel.Reader.ReadAllAsync())
            {
                lock (_batchLock)
                {
                    _batchBuffer.Add(logEntry);
                }
                
                var batchSize = _config.Get<int>("logging.batch_size", 100);
                if (_batchBuffer.Count >= batchSize)
                {
                    await FlushBatchAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Log processing failed");
        }
    }
    
    private async void FlushBatch(object? state)
    {
        await FlushBatchAsync();
    }
    
    private async Task FlushBatchAsync()
    {
        List<LogEntry> batchToSend;
        
        lock (_batchLock)
        {
            if (_batchBuffer.Count == 0)
            {
                return;
            }
            
            batchToSend = new List<LogEntry>(_batchBuffer);
            _batchBuffer.Clear();
        }
        
        try
        {
            await SendToExternalServiceAsync(batchToSend);
            _logger.LogDebug("Flushed {Count} log entries", batchToSend.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send log batch to external service");
            
            // Re-add failed entries to buffer
            lock (_batchLock)
            {
                _batchBuffer.InsertRange(0, batchToSend);
            }
        }
    }
    
    private async Task SendToExternalServiceAsync(List<LogEntry> logEntries)
    {
        var aggregatorUrl = _config.Get<string>("logging.aggregator_url");
        if (string.IsNullOrEmpty(aggregatorUrl))
        {
            return;
        }
        
        using var client = new HttpClient();
        var payload = new LogBatchPayload
        {
            ServiceName = _config.Get<string>("app.name", "unknown"),
            Environment = _config.Get<string>("app.environment", "unknown"),
            LogEntries = logEntries
        };
        
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await client.PostAsync(aggregatorUrl, content);
        response.EnsureSuccessStatusCode();
    }
    
    public void Dispose()
    {
        _flushTimer?.Dispose();
        _logChannel.Writer.Complete();
    }
}

public class LogBatchPayload
{
    public string ServiceName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public List<LogEntry> LogEntries { get; set; } = new();
}
```

## 📊 Log Levels and Filtering

### Log Level Manager

```csharp
public class LogLevelManager
{
    private readonly ILogger<LogLevelManager> _logger;
    private readonly TSKConfig _config;
    private readonly Dictionary<string, LogLevel> _categoryLevels;
    private readonly object _lock = new();
    
    public LogLevelManager(ILogger<LogLevelManager> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        _categoryLevels = new Dictionary<string, LogLevel>();
        
        LoadLogLevels();
    }
    
    public LogLevel GetLogLevel(string category)
    {
        lock (_lock)
        {
            if (_categoryLevels.TryGetValue(category, out var level))
            {
                return level;
            }
            
            return _categoryLevels.GetValueOrDefault("Default", LogLevel.Information);
        }
    }
    
    public void SetLogLevel(string category, LogLevel level)
    {
        lock (_lock)
        {
            _categoryLevels[category] = level;
            _logger.LogInformation("Log level for category '{Category}' set to {Level}", category, level);
        }
    }
    
    public bool ShouldLog(string category, LogLevel level)
    {
        var categoryLevel = GetLogLevel(category);
        return level >= categoryLevel;
    }
    
    public Dictionary<string, LogLevel> GetAllLogLevels()
    {
        lock (_lock)
        {
            return new Dictionary<string, LogLevel>(_categoryLevels);
        }
    }
    
    public async Task UpdateLogLevelsAsync(Dictionary<string, LogLevel> newLevels)
    {
        lock (_lock)
        {
            foreach (var kvp in newLevels)
            {
                _categoryLevels[kvp.Key] = kvp.Value;
            }
        }
        
        await SaveLogLevelsAsync();
        _logger.LogInformation("Updated {Count} log levels", newLevels.Count);
    }
    
    private void LoadLogLevels()
    {
        try
        {
            var levelsConfig = _config.GetSection("logging.levels");
            if (levelsConfig != null)
            {
                foreach (var key in levelsConfig.GetKeys())
                {
                    var levelString = levelsConfig.Get<string>(key);
                    if (Enum.TryParse<LogLevel>(levelString, true, out var level))
                    {
                        _categoryLevels[key] = level;
                    }
                }
            }
            
            // Set default if not configured
            if (!_categoryLevels.ContainsKey("Default"))
            {
                _categoryLevels["Default"] = LogLevel.Information;
            }
            
            _logger.LogInformation("Loaded {Count} log level configurations", _categoryLevels.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load log levels");
            _categoryLevels["Default"] = LogLevel.Information;
        }
    }
    
    private async Task SaveLogLevelsAsync()
    {
        try
        {
            var levelsConfig = new Dictionary<string, string>();
            foreach (var kvp in _categoryLevels)
            {
                levelsConfig[kvp.Key] = kvp.Value.ToString();
            }
            
            // This would typically save to configuration file or database
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save log levels");
        }
    }
}

public class ConditionalLogger
{
    private readonly ILogger _logger;
    private readonly LogLevelManager _logLevelManager;
    private readonly string _category;
    
    public ConditionalLogger(ILogger logger, LogLevelManager logLevelManager, string category)
    {
        _logger = logger;
        _logLevelManager = logLevelManager;
        _category = category;
    }
    
    public void LogInformation(string message, params object[] args)
    {
        if (_logLevelManager.ShouldLog(_category, LogLevel.Information))
        {
            _logger.LogInformation(message, args);
        }
    }
    
    public void LogWarning(string message, params object[] args)
    {
        if (_logLevelManager.ShouldLog(_category, LogLevel.Warning))
        {
            _logger.LogWarning(message, args);
        }
    }
    
    public void LogError(string message, Exception? exception = null, params object[] args)
    {
        if (_logLevelManager.ShouldLog(_category, LogLevel.Error))
        {
            if (exception != null)
            {
                _logger.LogError(exception, message, args);
            }
            else
            {
                _logger.LogError(message, args);
            }
        }
    }
    
    public void LogDebug(string message, params object[] args)
    {
        if (_logLevelManager.ShouldLog(_category, LogLevel.Debug))
        {
            _logger.LogDebug(message, args);
        }
    }
    
    public void LogTrace(string message, params object[] args)
    {
        if (_logLevelManager.ShouldLog(_category, LogLevel.Trace))
        {
            _logger.LogTrace(message, args);
        }
    }
}
```

## 🔍 Log Analysis

### Log Analyzer

```csharp
public class LogAnalyzer
{
    private readonly ILogger<LogAnalyzer> _logger;
    private readonly TSKConfig _config;
    private readonly IDbConnection _connection;
    
    public LogAnalyzer(ILogger<LogAnalyzer> logger, TSKConfig config, IDbConnection connection)
    {
        _logger = logger;
        _config = config;
        _connection = connection;
    }
    
    public async Task<LogAnalysisResult> AnalyzeLogsAsync(LogAnalysisRequest request)
    {
        try
        {
            var result = new LogAnalysisResult
            {
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                TotalLogs = 0,
                LogLevels = new Dictionary<LogLevel, int>(),
                TopErrors = new List<ErrorSummary>(),
                PerformanceIssues = new List<PerformanceIssue>(),
                SecurityEvents = new List<SecurityEvent>()
            };
            
            // Get total log count
            result.TotalLogs = await GetTotalLogCountAsync(request.StartTime, request.EndTime);
            
            // Analyze log levels
            result.LogLevels = await AnalyzeLogLevelsAsync(request.StartTime, request.EndTime);
            
            // Analyze top errors
            result.TopErrors = await AnalyzeTopErrorsAsync(request.StartTime, request.EndTime, request.TopErrorCount);
            
            // Analyze performance issues
            result.PerformanceIssues = await AnalyzePerformanceIssuesAsync(request.StartTime, request.EndTime);
            
            // Analyze security events
            result.SecurityEvents = await AnalyzeSecurityEventsAsync(request.StartTime, request.EndTime);
            
            _logger.LogInformation("Log analysis completed for period {StartTime} to {EndTime}", 
                request.StartTime, request.EndTime);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Log analysis failed");
            throw;
        }
    }
    
    private async Task<int> GetTotalLogCountAsync(DateTime startTime, DateTime endTime)
    {
        var query = @"
            SELECT COUNT(*) 
            FROM logs 
            WHERE timestamp BETWEEN @StartTime AND @EndTime";
        
        var parameters = new { StartTime = startTime, EndTime = endTime };
        return await _connection.ExecuteScalarAsync<int>(query, parameters);
    }
    
    private async Task<Dictionary<LogLevel, int>> AnalyzeLogLevelsAsync(DateTime startTime, DateTime endTime)
    {
        var query = @"
            SELECT level, COUNT(*) as count
            FROM logs 
            WHERE timestamp BETWEEN @StartTime AND @EndTime
            GROUP BY level
            ORDER BY count DESC";
        
        var parameters = new { StartTime = startTime, EndTime = endTime };
        var results = await _connection.QueryAsync<dynamic>(query, parameters);
        
        var levels = new Dictionary<LogLevel, int>();
        foreach (var result in results)
        {
            if (Enum.TryParse<LogLevel>(result.level.ToString(), true, out var level))
            {
                levels[level] = (int)result.count;
            }
        }
        
        return levels;
    }
    
    private async Task<List<ErrorSummary>> AnalyzeTopErrorsAsync(DateTime startTime, DateTime endTime, int count)
    {
        var query = @"
            SELECT 
                message,
                exception_type,
                COUNT(*) as occurrence_count,
                MIN(timestamp) as first_occurrence,
                MAX(timestamp) as last_occurrence
            FROM logs 
            WHERE timestamp BETWEEN @StartTime AND @EndTime
                AND level IN ('Error', 'Critical')
            GROUP BY message, exception_type
            ORDER BY occurrence_count DESC
            LIMIT @Count";
        
        var parameters = new { StartTime = startTime, EndTime = endTime, Count = count };
        var results = await _connection.QueryAsync<ErrorSummary>(query, parameters);
        
        return results.ToList();
    }
    
    private async Task<List<PerformanceIssue>> AnalyzePerformanceIssuesAsync(DateTime startTime, DateTime endTime)
    {
        var query = @"
            SELECT 
                message,
                context->>'$.operation' as operation,
                context->>'$.duration_ms' as duration_ms,
                timestamp
            FROM logs 
            WHERE timestamp BETWEEN @StartTime AND @EndTime
                AND message LIKE '%Performance%'
                AND CAST(context->>'$.duration_ms' AS REAL) > 1000
            ORDER BY timestamp DESC";
        
        var parameters = new { StartTime = startTime, EndTime = endTime };
        var results = await _connection.QueryAsync<PerformanceIssue>(query, parameters);
        
        return results.ToList();
    }
    
    private async Task<List<SecurityEvent>> AnalyzeSecurityEventsAsync(DateTime startTime, DateTime endTime)
    {
        var query = @"
            SELECT 
                message,
                context->>'$.event_type' as event_type,
                context->>'$.user_id' as user_id,
                context->>'$.ip_address' as ip_address,
                timestamp
            FROM logs 
            WHERE timestamp BETWEEN @StartTime AND @EndTime
                AND is_security_event = 1
            ORDER BY timestamp DESC";
        
        var parameters = new { StartTime = startTime, EndTime = endTime };
        var results = await _connection.QueryAsync<SecurityEvent>(query, parameters);
        
        return results.ToList();
    }
}

public class LogAnalysisRequest
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TopErrorCount { get; set; } = 10;
}

public class LogAnalysisResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TotalLogs { get; set; }
    public Dictionary<LogLevel, int> LogLevels { get; set; } = new();
    public List<ErrorSummary> TopErrors { get; set; } = new();
    public List<PerformanceIssue> PerformanceIssues { get; set; } = new();
    public List<SecurityEvent> SecurityEvents { get; set; } = new();
}

public class ErrorSummary
{
    public string Message { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public int OccurrenceCount { get; set; }
    public DateTime FirstOccurrence { get; set; }
    public DateTime LastOccurrence { get; set; }
}

public class PerformanceIssue
{
    public string Message { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public double DurationMs { get; set; }
    public DateTime Timestamp { get; set; }
}

public class SecurityEvent
{
    public string Message { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; }
}
```

## 📝 Summary

This guide covered comprehensive logging patterns for C# TuskLang applications:

- **Structured Logging**: Structured logger with context and correlation
- **Log Aggregation**: Batch processing and external service integration
- **Log Levels and Filtering**: Dynamic log level management and conditional logging
- **Log Analysis**: Log analysis tools for monitoring and debugging

These logging patterns ensure your C# TuskLang applications have comprehensive observability and debugging capabilities. 