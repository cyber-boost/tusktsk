# Performance Monitoring in C# TuskLang

## Overview

Performance monitoring is essential for maintaining optimal application performance. This guide covers metrics collection, profiling, dashboards, and optimization strategies for C# TuskLang applications.

## 📊 Metrics Collection

### Performance Metrics Service

```csharp
public class PerformanceMetricsService
{
    private readonly ILogger<PerformanceMetricsService> _logger;
    private readonly TSKConfig _config;
    private readonly Dictionary<string, MetricCollector> _collectors;
    private readonly Timer _reportingTimer;
    private readonly IMetricsExporter _exporter;
    
    public PerformanceMetricsService(ILogger<PerformanceMetricsService> logger, TSKConfig config, IMetricsExporter exporter)
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
}

public abstract class Metric
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Tags { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public abstract string Type { get; }
}

public class TimingMetric : Metric
{
    public TimeSpan Duration { get; set; }
    public override string Type => "timing";
}

public class CounterMetric : Metric
{
    public int Value { get; set; }
    public override string Type => "counter";
}

public class GaugeMetric : Metric
{
    public double Value { get; set; }
    public override string Type => "gauge";
}

public class HistogramMetric : Metric
{
    public double Value { get; set; }
    public override string Type => "histogram";
}
```

### Metrics Exporters

```csharp
public interface IMetricsExporter
{
    Task ExportAsync(List<Metric> metrics);
}

public class PrometheusMetricsExporter : IMetricsExporter
{
    private readonly ILogger<PrometheusMetricsExporter> _logger;
    private readonly TSKConfig _config;
    private readonly HttpClient _httpClient;
    
    public PrometheusMetricsExporter(ILogger<PrometheusMetricsExporter> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        _httpClient = new HttpClient();
    }
    
    public async Task ExportAsync(List<Metric> metrics)
    {
        var prometheusUrl = _config.Get<string>("metrics.prometheus_url");
        if (string.IsNullOrEmpty(prometheusUrl))
        {
            return;
        }
        
        var prometheusMetrics = ConvertToPrometheusFormat(metrics);
        
        var content = new StringContent(prometheusMetrics, Encoding.UTF8, "text/plain");
        
        var response = await _httpClient.PostAsync(prometheusUrl, content);
        response.EnsureSuccessStatusCode();
    }
    
    private string ConvertToPrometheusFormat(List<Metric> metrics)
    {
        var lines = new List<string>();
        
        foreach (var metric in metrics)
        {
            var tags = string.Join(",", metric.Tags.Select(kvp => $"{kvp.Key}=\"{kvp.Value}\""));
            var metricName = $"tusklang_{metric.Name}";
            
            switch (metric)
            {
                case TimingMetric timing:
                    lines.Add($"{metricName}_duration_ms{{{tags}}} {timing.Duration.TotalMilliseconds}");
                    break;
                    
                case CounterMetric counter:
                    lines.Add($"{metricName}_total{{{tags}}} {counter.Value}");
                    break;
                    
                case GaugeMetric gauge:
                    lines.Add($"{metricName}{{{tags}}} {gauge.Value}");
                    break;
                    
                case HistogramMetric histogram:
                    lines.Add($"{metricName}_bucket{{{tags}}} {histogram.Value}");
                    break;
            }
        }
        
        return string.Join("\n", lines);
    }
}

public class InfluxDBMetricsExporter : IMetricsExporter
{
    private readonly ILogger<InfluxDBMetricsExporter> _logger;
    private readonly TSKConfig _config;
    private readonly HttpClient _httpClient;
    
    public InfluxDBMetricsExporter(ILogger<InfluxDBMetricsExporter> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        _httpClient = new HttpClient();
    }
    
    public async Task ExportAsync(List<Metric> metrics)
    {
        var influxUrl = _config.Get<string>("metrics.influxdb_url");
        var database = _config.Get<string>("metrics.influxdb_database", "tusklang_metrics");
        
        if (string.IsNullOrEmpty(influxUrl))
        {
            return;
        }
        
        var influxMetrics = ConvertToInfluxFormat(metrics, database);
        
        var content = new StringContent(influxMetrics, Encoding.UTF8, "application/octet-stream");
        
        var response = await _httpClient.PostAsync($"{influxUrl}/write?db={database}", content);
        response.EnsureSuccessStatusCode();
    }
    
    private string ConvertToInfluxFormat(List<Metric> metrics, string database)
    {
        var lines = new List<string>();
        
        foreach (var metric in metrics)
        {
            var tags = string.Join(",", metric.Tags.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            var measurement = $"tusklang_{metric.Name}";
            
            switch (metric)
            {
                case TimingMetric timing:
                    lines.Add($"{measurement},{tags} duration_ms={timing.Duration.TotalMilliseconds} {GetTimestamp(metric.Timestamp)}");
                    break;
                    
                case CounterMetric counter:
                    lines.Add($"{measurement},{tags} value={counter.Value} {GetTimestamp(metric.Timestamp)}");
                    break;
                    
                case GaugeMetric gauge:
                    lines.Add($"{measurement},{tags} value={gauge.Value} {GetTimestamp(metric.Timestamp)}");
                    break;
                    
                case HistogramMetric histogram:
                    lines.Add($"{measurement},{tags} value={histogram.Value} {GetTimestamp(metric.Timestamp)}");
                    break;
            }
        }
        
        return string.Join("\n", lines);
    }
    
    private long GetTimestamp(DateTime timestamp)
    {
        return new DateTimeOffset(timestamp).ToUnixTimeNanoseconds();
    }
}
```

## 🔍 Performance Profiling

### Profiling Middleware

```csharp
public class PerformanceProfilingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceProfilingMiddleware> _logger;
    private readonly PerformanceMetricsService _metricsService;
    
    public PerformanceProfilingMiddleware(
        RequestDelegate next,
        ILogger<PerformanceProfilingMiddleware> logger,
        PerformanceMetricsService metricsService)
    {
        _next = next;
        _logger = logger;
        _metricsService = metricsService;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path;
        var method = context.Request.Method;
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            var duration = stopwatch.Elapsed;
            var statusCode = context.Response.StatusCode;
            
            // Record metrics
            _metricsService.RecordTiming("http_request_duration", duration, new Dictionary<string, object>
            {
                ["path"] = path,
                ["method"] = method,
                ["status_code"] = statusCode
            });
            
            _metricsService.IncrementCounter("http_requests_total", 1, new Dictionary<string, object>
            {
                ["path"] = path,
                ["method"] = method,
                ["status_code"] = statusCode
            });
            
            // Log slow requests
            if (duration.TotalSeconds > 1)
            {
                _logger.LogWarning("Slow request detected: {Method} {Path} took {Duration}ms", 
                    method, path, duration.TotalMilliseconds);
            }
        }
    }
}

public class DatabaseProfilingInterceptor : IDbInterceptor
{
    private readonly PerformanceMetricsService _metricsService;
    private readonly ILogger<DatabaseProfilingInterceptor> _logger;
    
    public DatabaseProfilingInterceptor(PerformanceMetricsService metricsService, ILogger<DatabaseProfilingInterceptor> logger)
    {
        _metricsService = metricsService;
        _logger = logger;
    }
    
    public async Task<T> InterceptAsync<T>(Func<Task<T>> operation, string operationName, string? sql = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await operation();
            
            stopwatch.Stop();
            var duration = stopwatch.Elapsed;
            
            // Record metrics
            _metricsService.RecordTiming("database_operation_duration", duration, new Dictionary<string, object>
            {
                ["operation"] = operationName,
                ["sql"] = sql ?? "unknown"
            });
            
            _metricsService.IncrementCounter("database_operations_total", 1, new Dictionary<string, object>
            {
                ["operation"] = operationName
            });
            
            // Log slow queries
            if (duration.TotalSeconds > 0.5)
            {
                _logger.LogWarning("Slow database operation: {Operation} took {Duration}ms", 
                    operationName, duration.TotalMilliseconds);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _metricsService.IncrementCounter("database_operation_errors", 1, new Dictionary<string, object>
            {
                ["operation"] = operationName,
                ["error_type"] = ex.GetType().Name
            });
            
            throw;
        }
    }
}

public interface IDbInterceptor
{
    Task<T> InterceptAsync<T>(Func<Task<T>> operation, string operationName, string? sql = null);
}
```

### Memory Profiling

```csharp
public class MemoryProfiler
{
    private readonly ILogger<MemoryProfiler> _logger;
    private readonly PerformanceMetricsService _metricsService;
    private readonly Timer _profilingTimer;
    private readonly TSKConfig _config;
    
    public MemoryProfiler(ILogger<MemoryProfiler> logger, PerformanceMetricsService metricsService, TSKConfig config)
    {
        _logger = logger;
        _metricsService = metricsService;
        _config = config;
        
        var profilingInterval = TimeSpan.FromMinutes(config.Get<int>("profiling.memory_interval_minutes", 5));
        _profilingTimer = new Timer(ProfileMemory, null, profilingInterval, profilingInterval);
    }
    
    private void ProfileMemory(object? state)
    {
        try
        {
            var process = Process.GetCurrentProcess();
            
            // Record memory metrics
            _metricsService.RecordGauge("memory_working_set_mb", process.WorkingSet64 / 1024.0 / 1024.0);
            _metricsService.RecordGauge("memory_private_mb", process.PrivateMemorySize64 / 1024.0 / 1024.0);
            _metricsService.RecordGauge("memory_virtual_mb", process.VirtualMemorySize64 / 1024.0 / 1024.0);
            
            // Record GC metrics
            for (int i = 0; i <= GC.MaxGeneration; i++)
            {
                var collectionCount = GC.CollectionCount(i);
                _metricsService.RecordGauge($"gc_collection_count_gen_{i}", collectionCount);
            }
            
            var totalMemory = GC.GetTotalMemory(false);
            _metricsService.RecordGauge("gc_total_memory_mb", totalMemory / 1024.0 / 1024.0);
            
            // Log memory warnings
            var workingSetMB = process.WorkingSet64 / 1024.0 / 1024.0;
            var memoryThreshold = _config.Get<double>("profiling.memory_threshold_mb", 1000);
            
            if (workingSetMB > memoryThreshold)
            {
                _logger.LogWarning("High memory usage detected: {WorkingSetMB}MB", workingSetMB);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to profile memory");
        }
    }
    
    public void Dispose()
    {
        _profilingTimer?.Dispose();
    }
}
```

## 📈 Performance Dashboards

### Dashboard Data Service

```csharp
public class PerformanceDashboardService
{
    private readonly ILogger<PerformanceDashboardService> _logger;
    private readonly TSKConfig _config;
    private readonly IMetricsRepository _metricsRepository;
    
    public PerformanceDashboardService(ILogger<PerformanceDashboardService> logger, TSKConfig config, IMetricsRepository metricsRepository)
    {
        _logger = logger;
        _config = config;
        _metricsRepository = metricsRepository;
    }
    
    public async Task<DashboardData> GetDashboardDataAsync(DateTime startTime, DateTime endTime)
    {
        var dashboardData = new DashboardData
        {
            TimeRange = new TimeRange { Start = startTime, End = endTime },
            ServiceName = _config.Get<string>("app.name", "unknown"),
            Environment = _config.Get<string>("app.environment", "unknown")
        };
        
        // Get HTTP request metrics
        dashboardData.HttpMetrics = await GetHttpMetricsAsync(startTime, endTime);
        
        // Get database metrics
        dashboardData.DatabaseMetrics = await GetDatabaseMetricsAsync(startTime, endTime);
        
        // Get memory metrics
        dashboardData.MemoryMetrics = await GetMemoryMetricsAsync(startTime, endTime);
        
        // Get error metrics
        dashboardData.ErrorMetrics = await GetErrorMetricsAsync(startTime, endTime);
        
        // Get performance trends
        dashboardData.PerformanceTrends = await GetPerformanceTrendsAsync(startTime, endTime);
        
        return dashboardData;
    }
    
    private async Task<HttpMetrics> GetHttpMetricsAsync(DateTime startTime, DateTime endTime)
    {
        var metrics = await _metricsRepository.GetMetricsAsync("http_request_duration", startTime, endTime);
        
        return new HttpMetrics
        {
            TotalRequests = metrics.Count,
            AverageResponseTime = metrics.Average(m => ((TimingMetric)m).Duration.TotalMilliseconds),
            MaxResponseTime = metrics.Max(m => ((TimingMetric)m).Duration.TotalMilliseconds),
            MinResponseTime = metrics.Min(m => ((TimingMetric)m).Duration.TotalMilliseconds),
            RequestsPerSecond = metrics.Count / (endTime - startTime).TotalSeconds,
            StatusCodeDistribution = metrics
                .GroupBy(m => m.Tags.GetValueOrDefault("status_code", "unknown"))
                .ToDictionary(g => g.Key.ToString(), g => g.Count())
        };
    }
    
    private async Task<DatabaseMetrics> GetDatabaseMetricsAsync(DateTime startTime, DateTime endTime)
    {
        var metrics = await _metricsRepository.GetMetricsAsync("database_operation_duration", startTime, endTime);
        
        return new DatabaseMetrics
        {
            TotalOperations = metrics.Count,
            AverageOperationTime = metrics.Average(m => ((TimingMetric)m).Duration.TotalMilliseconds),
            MaxOperationTime = metrics.Max(m => ((TimingMetric)m).Duration.TotalMilliseconds),
            MinOperationTime = metrics.Min(m => ((TimingMetric)m).Duration.TotalMilliseconds),
            OperationsPerSecond = metrics.Count / (endTime - startTime).TotalSeconds,
            OperationDistribution = metrics
                .GroupBy(m => m.Tags.GetValueOrDefault("operation", "unknown"))
                .ToDictionary(g => g.Key.ToString(), g => g.Count())
        };
    }
    
    private async Task<MemoryMetrics> GetMemoryMetricsAsync(DateTime startTime, DateTime endTime)
    {
        var workingSetMetrics = await _metricsRepository.GetMetricsAsync("memory_working_set_mb", startTime, endTime);
        var gcMetrics = await _metricsRepository.GetMetricsAsync("gc_total_memory_mb", startTime, endTime);
        
        return new MemoryMetrics
        {
            AverageWorkingSetMB = workingSetMetrics.Average(m => ((GaugeMetric)m).Value),
            MaxWorkingSetMB = workingSetMetrics.Max(m => ((GaugeMetric)m).Value),
            MinWorkingSetMB = workingSetMetrics.Min(m => ((GaugeMetric)m).Value),
            AverageGCTotalMemoryMB = gcMetrics.Average(m => ((GaugeMetric)m).Value),
            MaxGCTotalMemoryMB = gcMetrics.Max(m => ((GaugeMetric)m).Value)
        };
    }
    
    private async Task<ErrorMetrics> GetErrorMetricsAsync(DateTime startTime, DateTime endTime)
    {
        var errorMetrics = await _metricsRepository.GetMetricsAsync("database_operation_errors", startTime, endTime);
        
        return new ErrorMetrics
        {
            TotalErrors = errorMetrics.Sum(m => ((CounterMetric)m).Value),
            ErrorsPerSecond = errorMetrics.Sum(m => ((CounterMetric)m).Value) / (endTime - startTime).TotalSeconds,
            ErrorDistribution = errorMetrics
                .GroupBy(m => m.Tags.GetValueOrDefault("error_type", "unknown"))
                .ToDictionary(g => g.Key.ToString(), g => g.Sum(m => ((CounterMetric)m).Value))
        };
    }
    
    private async Task<List<PerformanceTrend>> GetPerformanceTrendsAsync(DateTime startTime, DateTime endTime)
    {
        var trends = new List<PerformanceTrend>();
        
        // Get hourly trends
        var interval = TimeSpan.FromHours(1);
        var currentTime = startTime;
        
        while (currentTime < endTime)
        {
            var intervalEnd = currentTime.Add(interval);
            var metrics = await _metricsRepository.GetMetricsAsync("http_request_duration", currentTime, intervalEnd);
            
            if (metrics.Any())
            {
                trends.Add(new PerformanceTrend
                {
                    Timestamp = currentTime,
                    AverageResponseTime = metrics.Average(m => ((TimingMetric)m).Duration.TotalMilliseconds),
                    RequestCount = metrics.Count,
                    ErrorCount = metrics.Count(m => m.Tags.GetValueOrDefault("status_code", 200).ToString() == "500")
                });
            }
            
            currentTime = intervalEnd;
        }
        
        return trends;
    }
}

public class DashboardData
{
    public TimeRange TimeRange { get; set; } = new();
    public string ServiceName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public HttpMetrics HttpMetrics { get; set; } = new();
    public DatabaseMetrics DatabaseMetrics { get; set; } = new();
    public MemoryMetrics MemoryMetrics { get; set; } = new();
    public ErrorMetrics ErrorMetrics { get; set; } = new();
    public List<PerformanceTrend> PerformanceTrends { get; set; } = new();
}

public class TimeRange
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}

public class HttpMetrics
{
    public int TotalRequests { get; set; }
    public double AverageResponseTime { get; set; }
    public double MaxResponseTime { get; set; }
    public double MinResponseTime { get; set; }
    public double RequestsPerSecond { get; set; }
    public Dictionary<string, int> StatusCodeDistribution { get; set; } = new();
}

public class DatabaseMetrics
{
    public int TotalOperations { get; set; }
    public double AverageOperationTime { get; set; }
    public double MaxOperationTime { get; set; }
    public double MinOperationTime { get; set; }
    public double OperationsPerSecond { get; set; }
    public Dictionary<string, int> OperationDistribution { get; set; } = new();
}

public class MemoryMetrics
{
    public double AverageWorkingSetMB { get; set; }
    public double MaxWorkingSetMB { get; set; }
    public double MinWorkingSetMB { get; set; }
    public double AverageGCTotalMemoryMB { get; set; }
    public double MaxGCTotalMemoryMB { get; set; }
}

public class ErrorMetrics
{
    public int TotalErrors { get; set; }
    public double ErrorsPerSecond { get; set; }
    public Dictionary<string, int> ErrorDistribution { get; set; } = new();
}

public class PerformanceTrend
{
    public DateTime Timestamp { get; set; }
    public double AverageResponseTime { get; set; }
    public int RequestCount { get; set; }
    public int ErrorCount { get; set; }
}
```

## 🚀 Performance Optimization

### Caching Strategies

```csharp
public class PerformanceOptimizer
{
    private readonly ILogger<PerformanceOptimizer> _logger;
    private readonly TSKConfig _config;
    private readonly PerformanceMetricsService _metricsService;
    private readonly IDistributedCache _cache;
    
    public PerformanceOptimizer(
        ILogger<PerformanceOptimizer> logger,
        TSKConfig config,
        PerformanceMetricsService metricsService,
        IDistributedCache cache)
    {
        _logger = logger;
        _config = config;
        _metricsService = metricsService;
        _cache = cache;
    }
    
    public async Task<T> GetOrSetCacheAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        var cacheKey = $"perf_opt_{key}";
        var cacheExpiration = expiration ?? TimeSpan.FromMinutes(_config.Get<int>("cache.default_expiration_minutes", 30));
        
        try
        {
            var cachedValue = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedValue))
            {
                _metricsService.IncrementCounter("cache_hits", 1, new Dictionary<string, object> { ["key"] = key });
                return JsonSerializer.Deserialize<T>(cachedValue)!;
            }
            
            _metricsService.IncrementCounter("cache_misses", 1, new Dictionary<string, object> { ["key"] = key });
            
            var value = await factory();
            var json = JsonSerializer.Serialize(value);
            
            await _cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheExpiration
            });
            
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache operation failed for key: {Key}", key);
            return await factory();
        }
    }
    
    public async Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation, string operationName, TimeSpan? timeout = null)
    {
        var operationTimeout = timeout ?? TimeSpan.FromSeconds(_config.Get<int>("performance.default_timeout_seconds", 30));
        
        using var cts = new CancellationTokenSource(operationTimeout);
        
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await operation().WaitAsync(cts.Token);
            stopwatch.Stop();
            
            _metricsService.RecordTiming($"{operationName}_duration", stopwatch.Elapsed);
            
            return result;
        }
        catch (OperationCanceledException)
        {
            _metricsService.IncrementCounter("timeout_errors", 1, new Dictionary<string, object> { ["operation"] = operationName });
            throw new TimeoutException($"Operation {operationName} timed out after {operationTimeout.TotalSeconds} seconds");
        }
    }
    
    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, int maxRetries = 3)
    {
        var retryCount = 0;
        var lastException = (Exception?)null;
        
        while (retryCount <= maxRetries)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var result = await operation();
                stopwatch.Stop();
                
                _metricsService.RecordTiming($"{operationName}_duration", stopwatch.Elapsed);
                
                if (retryCount > 0)
                {
                    _metricsService.IncrementCounter("retry_success", 1, new Dictionary<string, object>
                    {
                        ["operation"] = operationName,
                        ["retry_count"] = retryCount
                    });
                }
                
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                retryCount++;
                
                _metricsService.IncrementCounter("retry_attempts", 1, new Dictionary<string, object>
                {
                    ["operation"] = operationName,
                    ["retry_count"] = retryCount
                });
                
                if (retryCount <= maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount - 1)); // Exponential backoff
                    await Task.Delay(delay);
                }
            }
        }
        
        _metricsService.IncrementCounter("retry_failures", 1, new Dictionary<string, object>
        {
            ["operation"] = operationName,
            ["retry_count"] = retryCount
        });
        
        throw lastException!;
    }
}
```

## 📝 Summary

This guide covered comprehensive performance monitoring strategies for C# TuskLang applications:

- **Metrics Collection**: Performance metrics service with timing, counters, gauges, and histograms
- **Metrics Exporters**: Prometheus and InfluxDB exporters for metrics aggregation
- **Performance Profiling**: HTTP and database profiling middleware with interceptors
- **Memory Profiling**: Memory usage monitoring and garbage collection metrics
- **Performance Dashboards**: Dashboard data service with comprehensive metrics visualization
- **Performance Optimization**: Caching strategies, timeout handling, and retry mechanisms

These performance monitoring strategies ensure your C# TuskLang applications are optimized, monitored, and performant in production environments. 