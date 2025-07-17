# Logging Patterns in C# with TuskLang

## Overview

This guide covers comprehensive logging patterns for C# applications using TuskLang, including structured logging, log levels, and centralized logging strategies.

## Table of Contents

1. [Structured Logging](#structured-logging)
2. [Log Levels](#log-levels)
3. [Centralized Logging](#centralized-logging)
4. [TuskLang Integration](#tusklang-integration)

## Structured Logging

### Basic Structured Logging

```csharp
public class StructuredLogger
{
    private readonly ILogger _logger;
    private readonly TuskLang _config;

    public StructuredLogger(ILogger logger, TuskLang config)
    {
        _logger = logger;
        _config = config;
    }

    public void LogUserAction(string userId, string action, object data)
    {
        var logData = new Dictionary<string, object>
        {
            ["userId"] = userId,
            ["action"] = action,
            ["data"] = data,
            ["timestamp"] = DateTime.UtcNow,
            ["environment"] = _config.GetValue<string>("app.environment", "Unknown")
        };

        _logger.LogInformation("User action: {Action} by user {UserId} with data {@Data}", 
            action, userId, logData);
    }

    public void LogDatabaseOperation(string operation, TimeSpan duration, bool success)
    {
        var logData = new Dictionary<string, object>
        {
            ["operation"] = operation,
            ["durationMs"] = duration.TotalMilliseconds,
            ["success"] = success,
            ["timestamp"] = DateTime.UtcNow
        };

        if (success)
        {
            _logger.LogInformation("Database operation {Operation} completed in {Duration}ms", 
                operation, duration.TotalMilliseconds);
        }
        else
        {
            _logger.LogError("Database operation {Operation} failed after {Duration}ms", 
                operation, duration.TotalMilliseconds);
        }
    }

    public void LogApiCall(string endpoint, HttpStatusCode statusCode, TimeSpan duration)
    {
        var level = statusCode.IsSuccessStatusCode ? LogLevel.Information : LogLevel.Warning;
        
        _logger.Log(level, "API call to {Endpoint} returned {StatusCode} in {Duration}ms", 
            endpoint, statusCode, duration.TotalMilliseconds);
    }
}
```

### Advanced Structured Logging

```csharp
public class AdvancedLogger
{
    private readonly ILogger _logger;
    private readonly TuskLang _config;

    public AdvancedLogger(ILogger logger, TuskLang config)
    {
        _logger = logger;
        _config = config;
    }

    public IDisposable BeginScope(string operation, Dictionary<string, object> properties)
    {
        var scopeData = new Dictionary<string, object>(properties)
        {
            ["operation"] = operation,
            ["timestamp"] = DateTime.UtcNow,
            ["environment"] = _config.GetValue<string>("app.environment", "Unknown")
        };

        return _logger.BeginScope(scopeData);
    }

    public void LogWithCorrelation(string message, string correlationId, params object[] args)
    {
        var logData = new Dictionary<string, object>
        {
            ["correlationId"] = correlationId,
            ["timestamp"] = DateTime.UtcNow
        };

        _logger.LogInformation("[{CorrelationId}] {Message}", correlationId, 
            string.Format(message, args));
    }

    public void LogSecurityEvent(string eventType, string userId, string ipAddress, bool success)
    {
        var level = success ? LogLevel.Information : LogLevel.Warning;
        
        var logData = new Dictionary<string, object>
        {
            ["eventType"] = eventType,
            ["userId"] = userId,
            ["ipAddress"] = ipAddress,
            ["success"] = success,
            ["timestamp"] = DateTime.UtcNow
        };

        _logger.Log(level, "Security event: {EventType} for user {UserId} from {IpAddress} - Success: {Success}", 
            eventType, userId, ipAddress, success);
    }
}
```

## Log Levels

### Log Level Strategy

```csharp
public class LogLevelStrategy
{
    private readonly ILogger _logger;
    private readonly TuskLang _config;

    public LogLevelStrategy(ILogger logger, TuskLang config)
    {
        _logger = logger;
        _config = config;
    }

    public void LogDebug(string message, params object[] args)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(message, args);
        }
    }

    public void LogInformation(string message, params object[] args)
    {
        _logger.LogInformation(message, args);
    }

    public void LogWarning(string message, Exception exception = null, params object[] args)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception, message, args);
        }
        else
        {
            _logger.LogWarning(message, args);
        }
    }

    public void LogError(string message, Exception exception = null, params object[] args)
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

    public void LogCritical(string message, Exception exception = null, params object[] args)
    {
        if (exception != null)
        {
            _logger.LogCritical(exception, message, args);
        }
        else
        {
            _logger.LogCritical(message, args);
        }
    }
}
```

### Category-Based Logging

```csharp
public static class LogCategories
{
    public const string Database = "Database";
    public const string Api = "API";
    public const string Security = "Security";
    public const string Performance = "Performance";
    public const string TuskLang = "TuskLang";
}

public class CategoryLogger
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly TuskLang _config;

    public CategoryLogger(ILoggerFactory loggerFactory, TuskLang config)
    {
        _loggerFactory = loggerFactory;
        _config = config;
    }

    public ILogger GetLogger(string category)
    {
        return _loggerFactory.CreateLogger(category);
    }

    public void LogDatabaseOperation(string operation, TimeSpan duration, bool success)
    {
        var logger = GetLogger(LogCategories.Database);
        
        if (success)
        {
            logger.LogInformation("Database operation {Operation} completed in {Duration}ms", 
                operation, duration.TotalMilliseconds);
        }
        else
        {
            logger.LogError("Database operation {Operation} failed after {Duration}ms", 
                operation, duration.TotalMilliseconds);
        }
    }

    public void LogApiCall(string endpoint, HttpStatusCode statusCode, TimeSpan duration)
    {
        var logger = GetLogger(LogCategories.Api);
        
        var level = statusCode.IsSuccessStatusCode ? LogLevel.Information : LogLevel.Warning;
        
        logger.Log(level, "API call to {Endpoint} returned {StatusCode} in {Duration}ms", 
            endpoint, statusCode, duration.TotalMilliseconds);
    }

    public void LogSecurityEvent(string eventType, string userId, bool success)
    {
        var logger = GetLogger(LogCategories.Security);
        
        var level = success ? LogLevel.Information : LogLevel.Warning;
        
        logger.Log(level, "Security event: {EventType} for user {UserId} - Success: {Success}", 
            eventType, userId, success);
    }
}
```

## Centralized Logging

### Log Aggregation

```csharp
public class LogAggregator
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly string _logEndpoint;
    private readonly TuskLang _config;

    public LogAggregator(ILogger logger, HttpClient httpClient, string logEndpoint, TuskLang config)
    {
        _logger = logger;
        _httpClient = httpClient;
        _logEndpoint = logEndpoint;
        _config = config;
    }

    public async Task SendLogAsync(LogEntry entry)
    {
        try
        {
            var json = JsonSerializer.Serialize(entry);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(_logEndpoint, content);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to send log to aggregator. Status: {StatusCode}", 
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending log to aggregator");
        }
    }

    public async Task SendBatchAsync(IEnumerable<LogEntry> entries)
    {
        var batch = new LogBatch
        {
            Entries = entries.ToList(),
            Timestamp = DateTime.UtcNow,
            Source = Environment.MachineName,
            Environment = _config.GetValue<string>("app.environment", "Unknown")
        };

        try
        {
            var json = JsonSerializer.Serialize(batch);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_logEndpoint}/batch", content);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to send log batch. Status: {StatusCode}", 
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending log batch to aggregator");
        }
    }
}

public class LogEntry
{
    public string Level { get; set; }
    public string Message { get; set; }
    public string Category { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Properties { get; set; }
    public string Exception { get; set; }
    public string CorrelationId { get; set; }
}

public class LogBatch
{
    public List<LogEntry> Entries { get; set; }
    public DateTime Timestamp { get; set; }
    public string Source { get; set; }
    public string Environment { get; set; }
}
```

### Logging Configuration

```csharp
public class LoggingConfiguration
{
    public static IHostBuilder ConfigureLogging(IHostBuilder builder, TuskLang config)
    {
        return builder.ConfigureLogging((context, logging) =>
        {
            logging.ClearProviders();
            
            // Console logging with structured format
            logging.AddConsole(options =>
            {
                options.FormatterName = "json";
            });

            // File logging
            var logFilePath = config.GetValue<string>("logging.filePath", "logs/app-{Date}.log");
            logging.AddFile(logFilePath, options =>
            {
                options.FileSizeLimitBytes = config.GetValue<int>("logging.maxFileSizeMB", 10) * 1024 * 1024;
                options.RetainedFileCountLimit = config.GetValue<int>("logging.retainedFileCount", 30);
                options.Append = true;
            });

            // Application Insights
            var appInsightsKey = config.GetValue<string>("logging.appInsightsKey", "");
            if (!string.IsNullOrEmpty(appInsightsKey))
            {
                logging.AddApplicationInsights();
            }

            // Custom TuskLang logging
            logging.AddProvider(new TuskLangLoggingProvider(config));
        });
    }
}
```

## TuskLang Integration

### TuskLang Logging Provider

```csharp
public class TuskLangLoggingProvider : ILoggerProvider
{
    private readonly TuskLang _config;
    private readonly Dictionary<string, TuskLangLogger> _loggers;

    public TuskLangLoggingProvider(TuskLang config)
    {
        _config = config;
        _loggers = new Dictionary<string, TuskLangLogger>();
    }

    public ILogger CreateLogger(string categoryName)
    {
        if (!_loggers.ContainsKey(categoryName))
        {
            _loggers[categoryName] = new TuskLangLogger(categoryName, _config);
        }

        return _loggers[categoryName];
    }

    public void Dispose()
    {
        foreach (var logger in _loggers.Values)
        {
            logger.Dispose();
        }
        _loggers.Clear();
    }
}

public class TuskLangLogger : ILogger
{
    private readonly string _categoryName;
    private readonly TuskLang _config;
    private readonly TuskLang _tuskLang;

    public TuskLangLogger(string categoryName, TuskLang config)
    {
        _categoryName = categoryName;
        _config = config;
        _tuskLang = new TuskLang(config);
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        var minLevel = _config.GetValue<string>("logging.minimumLevel", "Information");
        var currentLevel = Enum.Parse<LogLevel>(minLevel, true);
        return logLevel >= currentLevel;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, 
        Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        
        // Log to TuskLang configuration
        var logEntry = new Dictionary<string, object>
        {
            ["level"] = logLevel.ToString(),
            ["category"] = _categoryName,
            ["message"] = message,
            ["timestamp"] = DateTime.UtcNow,
            ["exception"] = exception?.ToString()
        };

        _tuskLang.SetValue($"logs.{DateTime.UtcNow:yyyyMMdd}.{Guid.NewGuid()}", logEntry);
    }

    public void Dispose()
    {
        _tuskLang?.Dispose();
    }

    private class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new NullScope();
        private NullScope() { }
        public void Dispose() { }
    }
}
```

### TuskLang Log Configuration

```csharp
public class TuskLangLogConfig
{
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;
    public bool EnableStructuredLogging { get; set; } = true;
    public bool EnablePerformanceLogging { get; set; } = true;
    public bool EnableAuditLogging { get; set; } = true;
    public string LogFilePath { get; set; } = "logs/tusklang.log";
    public int MaxLogFileSizeMB { get; set; } = 100;
    public int RetainedFileCount { get; set; } = 30;
}

public static class TuskLangLoggingExtensions
{
    public static ILoggingBuilder AddTuskLang(this ILoggingBuilder builder, 
        TuskLangLogConfig config)
    {
        builder.AddProvider(new TuskLangLoggingProvider(config));
        return builder;
    }

    public static ILoggingBuilder AddTuskLang(this ILoggingBuilder builder, 
        Action<TuskLangLogConfig> configure)
    {
        var config = new TuskLangLogConfig();
        configure(config);
        
        builder.AddProvider(new TuskLangLoggingProvider(config));
        return builder;
    }
}
```

## Summary

This comprehensive logging patterns guide covers:

- **Structured Logging**: Using dictionaries and objects for rich log data
- **Log Levels**: Appropriate use of Debug, Information, Warning, Error, and Critical
- **Centralized Logging**: Log aggregation and batch processing
- **TuskLang Integration**: Custom logging provider and configuration

The patterns ensure comprehensive observability while maintaining performance and security in production C# applications integrated with TuskLang. 