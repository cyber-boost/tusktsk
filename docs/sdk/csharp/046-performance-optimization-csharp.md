# Performance Optimization in C# TuskLang

## Overview

Performance optimization is crucial for building scalable and responsive applications. This guide covers profiling, async patterns, caching strategies, and optimization techniques for C# TuskLang applications.

## 🔍 Performance Profiling

### Profiling Service

```csharp
public class PerformanceProfiler
{
    private readonly ILogger<PerformanceProfiler> _logger;
    private readonly TSKConfig _config;
    private readonly Dictionary<string, ProfilingData> _profilingData;
    private readonly object _lock = new();
    
    public PerformanceProfiler(ILogger<PerformanceProfiler> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        _profilingData = new Dictionary<string, ProfilingData>();
    }
    
    public async Task<T> ProfileAsync<T>(string operationName, Func<Task<T>> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        var startMemory = GC.GetTotalMemory(false);
        
        try
        {
            var result = await operation();
            
            stopwatch.Stop();
            var endMemory = GC.GetTotalMemory(false);
            var memoryUsed = endMemory - startMemory;
            
            RecordProfilingData(operationName, stopwatch.Elapsed, memoryUsed, true);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var endMemory = GC.GetTotalMemory(false);
            var memoryUsed = endMemory - startMemory;
            
            RecordProfilingData(operationName, stopwatch.Elapsed, memoryUsed, false);
            
            _logger.LogError(ex, "Operation {OperationName} failed after {Duration}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            
            throw;
        }
    }
    
    public T Profile<T>(string operationName, Func<T> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        var startMemory = GC.GetTotalMemory(false);
        
        try
        {
            var result = operation();
            
            stopwatch.Stop();
            var endMemory = GC.GetTotalMemory(false);
            var memoryUsed = endMemory - startMemory;
            
            RecordProfilingData(operationName, stopwatch.Elapsed, memoryUsed, true);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var endMemory = GC.GetTotalMemory(false);
            var memoryUsed = endMemory - startMemory;
            
            RecordProfilingData(operationName, stopwatch.Elapsed, memoryUsed, false);
            
            _logger.LogError(ex, "Operation {OperationName} failed after {Duration}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            
            throw;
        }
    }
    
    private void RecordProfilingData(string operationName, TimeSpan duration, long memoryUsed, bool success)
    {
        lock (_lock)
        {
            if (!_profilingData.ContainsKey(operationName))
            {
                _profilingData[operationName] = new ProfilingData();
            }
            
            var data = _profilingData[operationName];
            data.TotalCalls++;
            data.TotalDuration += duration;
            data.TotalMemoryUsed += memoryUsed;
            
            if (success)
            {
                data.SuccessfulCalls++;
            }
            else
            {
                data.FailedCalls++;
            }
            
            if (duration > data.MaxDuration)
            {
                data.MaxDuration = duration;
            }
            
            if (duration < data.MinDuration || data.MinDuration == TimeSpan.Zero)
            {
                data.MinDuration = duration;
            }
            
            if (memoryUsed > data.MaxMemoryUsed)
            {
                data.MaxMemoryUsed = memoryUsed;
            }
        }
    }
    
    public ProfilingReport GenerateReport()
    {
        lock (_lock)
        {
            var report = new ProfilingReport
            {
                GeneratedAt = DateTime.UtcNow,
                Operations = new List<OperationProfilingData>()
            };
            
            foreach (var kvp in _profilingData)
            {
                var data = kvp.Value;
                var operationData = new OperationProfilingData
                {
                    OperationName = kvp.Key,
                    TotalCalls = data.TotalCalls,
                    SuccessfulCalls = data.SuccessfulCalls,
                    FailedCalls = data.FailedCalls,
                    SuccessRate = data.TotalCalls > 0 ? (double)data.SuccessfulCalls / data.TotalCalls : 0,
                    AverageDuration = data.TotalCalls > 0 ? data.TotalDuration.TotalMilliseconds / data.TotalCalls : 0,
                    MaxDuration = data.MaxDuration.TotalMilliseconds,
                    MinDuration = data.MinDuration.TotalMilliseconds,
                    AverageMemoryUsed = data.TotalCalls > 0 ? data.TotalMemoryUsed / data.TotalCalls : 0,
                    MaxMemoryUsed = data.MaxMemoryUsed
                };
                
                report.Operations.Add(operationData);
            }
            
            return report;
        }
    }
    
    public void ClearProfilingData()
    {
        lock (_lock)
        {
            _profilingData.Clear();
        }
    }
}

public class ProfilingData
{
    public int TotalCalls { get; set; }
    public int SuccessfulCalls { get; set; }
    public int FailedCalls { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan MaxDuration { get; set; }
    public TimeSpan MinDuration { get; set; }
    public long TotalMemoryUsed { get; set; }
    public long MaxMemoryUsed { get; set; }
}

public class ProfilingReport
{
    public DateTime GeneratedAt { get; set; }
    public List<OperationProfilingData> Operations { get; set; } = new();
}

public class OperationProfilingData
{
    public string OperationName { get; set; } = string.Empty;
    public int TotalCalls { get; set; }
    public int SuccessfulCalls { get; set; }
    public int FailedCalls { get; set; }
    public double SuccessRate { get; set; }
    public double AverageDuration { get; set; }
    public double MaxDuration { get; set; }
    public double MinDuration { get; set; }
    public double AverageMemoryUsed { get; set; }
    public long MaxMemoryUsed { get; set; }
}
```

### Memory Profiler

```csharp
public class MemoryProfiler
{
    private readonly ILogger<MemoryProfiler> _logger;
    private readonly TSKConfig _config;
    private readonly Timer _profilingTimer;
    private readonly List<MemorySnapshot> _snapshots;
    private readonly object _lock = new();
    
    public MemoryProfiler(ILogger<MemoryProfiler> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        _snapshots = new List<MemorySnapshot>();
        
        var profilingInterval = TimeSpan.FromMinutes(config.Get<int>("profiling.memory_interval_minutes", 5));
        _profilingTimer = new Timer(ProfileMemory, null, profilingInterval, profilingInterval);
    }
    
    private void ProfileMemory(object? state)
    {
        try
        {
            var snapshot = new MemorySnapshot
            {
                Timestamp = DateTime.UtcNow,
                WorkingSetMB = Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0,
                PrivateMemoryMB = Process.GetCurrentProcess().PrivateMemorySize64 / 1024.0 / 1024.0,
                VirtualMemoryMB = Process.GetCurrentProcess().VirtualMemorySize64 / 1024.0 / 1024.0,
                GCTotalMemoryMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0,
                GCCollectionCounts = new Dictionary<int, int>()
            };
            
            // Record GC collection counts
            for (int i = 0; i <= GC.MaxGeneration; i++)
            {
                snapshot.GCCollectionCounts[i] = GC.CollectionCount(i);
            }
            
            lock (_lock)
            {
                _snapshots.Add(snapshot);
                
                // Keep only last 100 snapshots
                if (_snapshots.Count > 100)
                {
                    _snapshots.RemoveAt(0);
                }
            }
            
            // Check for memory warnings
            var memoryThreshold = _config.Get<double>("profiling.memory_threshold_mb", 1000);
            if (snapshot.WorkingSetMB > memoryThreshold)
            {
                _logger.LogWarning("High memory usage detected: {WorkingSetMB}MB", snapshot.WorkingSetMB);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to profile memory");
        }
    }
    
    public MemoryReport GenerateMemoryReport()
    {
        lock (_lock)
        {
            if (!_snapshots.Any())
            {
                return new MemoryReport();
            }
            
            var report = new MemoryReport
            {
                GeneratedAt = DateTime.UtcNow,
                Snapshots = new List<MemorySnapshot>(_snapshots),
                AverageWorkingSetMB = _snapshots.Average(s => s.WorkingSetMB),
                MaxWorkingSetMB = _snapshots.Max(s => s.WorkingSetMB),
                MinWorkingSetMB = _snapshots.Min(s => s.WorkingSetMB),
                AveragePrivateMemoryMB = _snapshots.Average(s => s.PrivateMemoryMB),
                MaxPrivateMemoryMB = _snapshots.Max(s => s.PrivateMemoryMB),
                MinPrivateMemoryMB = _snapshots.Min(s => s.PrivateMemoryMB),
                AverageGCTotalMemoryMB = _snapshots.Average(s => s.GCTotalMemoryMB),
                MaxGCTotalMemoryMB = _snapshots.Max(s => s.GCTotalMemoryMB),
                MinGCTotalMemoryMB = _snapshots.Min(s => s.GCTotalMemoryMB)
            };
            
            return report;
        }
    }
    
    public void Dispose()
    {
        _profilingTimer?.Dispose();
    }
}

public class MemorySnapshot
{
    public DateTime Timestamp { get; set; }
    public double WorkingSetMB { get; set; }
    public double PrivateMemoryMB { get; set; }
    public double VirtualMemoryMB { get; set; }
    public double GCTotalMemoryMB { get; set; }
    public Dictionary<int, int> GCCollectionCounts { get; set; } = new();
}

public class MemoryReport
{
    public DateTime GeneratedAt { get; set; }
    public List<MemorySnapshot> Snapshots { get; set; } = new();
    public double AverageWorkingSetMB { get; set; }
    public double MaxWorkingSetMB { get; set; }
    public double MinWorkingSetMB { get; set; }
    public double AveragePrivateMemoryMB { get; set; }
    public double MaxPrivateMemoryMB { get; set; }
    public double MinPrivateMemoryMB { get; set; }
    public double AverageGCTotalMemoryMB { get; set; }
    public double MaxGCTotalMemoryMB { get; set; }
    public double MinGCTotalMemoryMB { get; set; }
}
```

## ⚡ Async Patterns

### Async Operation Optimizer

```csharp
public class AsyncOperationOptimizer
{
    private readonly ILogger<AsyncOperationOptimizer> _logger;
    private readonly TSKConfig _config;
    private readonly SemaphoreSlim _semaphore;
    
    public AsyncOperationOptimizer(ILogger<AsyncOperationOptimizer> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        
        var maxConcurrency = config.Get<int>("performance.max_concurrency", 10);
        _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    }
    
    public async Task<T> ExecuteWithConcurrencyControlAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        
        try
        {
            return await operation();
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async Task<List<T>> ExecuteParallelAsync<T>(
        IEnumerable<Func<Task<T>>> operations,
        int maxDegreeOfParallelism = 0)
    {
        if (maxDegreeOfParallelism == 0)
        {
            maxDegreeOfParallelism = _config.Get<int>("performance.max_parallelism", Environment.ProcessorCount);
        }
        
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism
        };
        
        var results = new List<T>();
        var tasks = operations.Select(op => op()).ToList();
        
        await Task.WhenAll(tasks);
        
        foreach (var task in tasks)
        {
            results.Add(await task);
        }
        
        return results;
    }
    
    public async Task<T> ExecuteWithTimeoutAsync<T>(
        Func<Task<T>> operation,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            timeoutCts.Token, cancellationToken);
        
        try
        {
            return await operation().WaitAsync(combinedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"Operation timed out after {timeout.TotalSeconds} seconds");
        }
    }
    
    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        int maxRetries = 3,
        TimeSpan? baseDelay = null,
        Func<Exception, bool>? shouldRetry = null)
    {
        var delay = baseDelay ?? TimeSpan.FromSeconds(1);
        var lastException = (Exception?)null;
        
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (ShouldRetry(ex, shouldRetry) && attempt < maxRetries)
            {
                lastException = ex;
                
                var waitTime = delay * Math.Pow(2, attempt);
                await Task.Delay(waitTime);
                
                _logger.LogWarning(ex, "Attempt {Attempt} failed, retrying in {WaitTime}ms", 
                    attempt + 1, waitTime.TotalMilliseconds);
            }
        }
        
        throw lastException!;
    }
    
    private bool ShouldRetry(Exception exception, Func<Exception, bool>? shouldRetry)
    {
        if (shouldRetry != null)
        {
            return shouldRetry(exception);
        }
        
        return exception is TimeoutException ||
               exception is HttpRequestException ||
               exception is TaskCanceledException;
    }
}
```

### Async Cache Manager

```csharp
public class AsyncCacheManager
{
    private readonly ILogger<AsyncCacheManager> _logger;
    private readonly TSKConfig _config;
    private readonly MemoryCache _cache;
    private readonly Dictionary<string, SemaphoreSlim> _locks;
    private readonly object _lock = new();
    
    public AsyncCacheManager(ILogger<AsyncCacheManager> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
        _cache = new MemoryCache(new MemoryCacheOptions());
        _locks = new Dictionary<string, SemaphoreSlim>();
    }
    
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null)
    {
        if (_cache.TryGetValue(key, out T? cachedValue))
        {
            return cachedValue!;
        }
        
        var lockObj = GetOrCreateLock(key);
        await lockObj.WaitAsync();
        
        try
        {
            // Double-check after acquiring lock
            if (_cache.TryGetValue(key, out cachedValue))
            {
                return cachedValue!;
            }
            
            var value = await factory();
            var cacheExpiration = expiration ?? TimeSpan.FromMinutes(_config.Get<int>("cache.default_expiration_minutes", 30));
            
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheExpiration,
                SlidingExpiration = TimeSpan.FromMinutes(cacheExpiration.TotalMinutes * 0.2)
            };
            
            _cache.Set(key, value, cacheOptions);
            
            return value;
        }
        finally
        {
            lockObj.Release();
        }
    }
    
    public async Task<T?> GetAsync<T>(string key)
    {
        if (_cache.TryGetValue(key, out T? value))
        {
            return value;
        }
        
        return default;
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var cacheExpiration = expiration ?? TimeSpan.FromMinutes(_config.Get<int>("cache.default_expiration_minutes", 30));
        
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = cacheExpiration,
            SlidingExpiration = TimeSpan.FromMinutes(cacheExpiration.TotalMinutes * 0.2)
        };
        
        _cache.Set(key, value, cacheOptions);
    }
    
    public async Task RemoveAsync(string key)
    {
        _cache.Remove(key);
    }
    
    public async Task ClearAsync()
    {
        _cache.Clear();
    }
    
    private SemaphoreSlim GetOrCreateLock(string key)
    {
        lock (_lock)
        {
            if (!_locks.ContainsKey(key))
            {
                _locks[key] = new SemaphoreSlim(1, 1);
            }
            
            return _locks[key];
        }
    }
    
    public void Dispose()
    {
        _cache?.Dispose();
        
        foreach (var lockObj in _locks.Values)
        {
            lockObj?.Dispose();
        }
    }
}
```

## 🚀 Caching Strategies

### Multi-Level Cache

```csharp
public class MultiLevelCache
{
    private readonly ILogger<MultiLevelCache> _logger;
    private readonly TSKConfig _config;
    private readonly MemoryCache _l1Cache;
    private readonly IDistributedCache _l2Cache;
    private readonly Dictionary<string, SemaphoreSlim> _locks;
    private readonly object _lock = new();
    
    public MultiLevelCache(
        ILogger<MultiLevelCache> logger,
        TSKConfig config,
        IDistributedCache distributedCache)
    {
        _logger = logger;
        _config = config;
        _l1Cache = new MemoryCache(new MemoryCacheOptions());
        _l2Cache = distributedCache;
        _locks = new Dictionary<string, SemaphoreSlim>();
    }
    
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? l1Expiration = null,
        TimeSpan? l2Expiration = null)
    {
        // Try L1 cache first
        if (_l1Cache.TryGetValue(key, out T? l1Value))
        {
            return l1Value!;
        }
        
        // Try L2 cache
        var l2Value = await GetFromL2CacheAsync<T>(key);
        if (l2Value != null)
        {
            // Populate L1 cache
            var l1Exp = l1Expiration ?? TimeSpan.FromMinutes(5);
            _l1Cache.Set(key, l2Value, l1Exp);
            return l2Value;
        }
        
        // Execute factory and populate both caches
        var lockObj = GetOrCreateLock(key);
        await lockObj.WaitAsync();
        
        try
        {
            // Double-check after acquiring lock
            if (_l1Cache.TryGetValue(key, out l1Value))
            {
                return l1Value!;
            }
            
            l2Value = await GetFromL2CacheAsync<T>(key);
            if (l2Value != null)
            {
                var l1Exp = l1Expiration ?? TimeSpan.FromMinutes(5);
                _l1Cache.Set(key, l2Value, l1Exp);
                return l2Value;
            }
            
            var value = await factory();
            
            // Set in both caches
            var l1Exp = l1Expiration ?? TimeSpan.FromMinutes(5);
            var l2Exp = l2Expiration ?? TimeSpan.FromMinutes(30);
            
            _l1Cache.Set(key, value, l1Exp);
            await SetInL2CacheAsync(key, value, l2Exp);
            
            return value;
        }
        finally
        {
            lockObj.Release();
        }
    }
    
    private async Task<T?> GetFromL2CacheAsync<T>(string key)
    {
        try
        {
            var json = await _l2Cache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(json))
            {
                return JsonSerializer.Deserialize<T>(json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get value from L2 cache for key {Key}", key);
        }
        
        return default;
    }
    
    private async Task SetInL2CacheAsync<T>(string key, T value, TimeSpan expiration)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            
            await _l2Cache.SetStringAsync(key, json, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set value in L2 cache for key {Key}", key);
        }
    }
    
    private SemaphoreSlim GetOrCreateLock(string key)
    {
        lock (_lock)
        {
            if (!_locks.ContainsKey(key))
            {
                _locks[key] = new SemaphoreSlim(1, 1);
            }
            
            return _locks[key];
        }
    }
    
    public async Task InvalidateAsync(string key)
    {
        _l1Cache.Remove(key);
        await _l2Cache.RemoveAsync(key);
    }
    
    public void Dispose()
    {
        _l1Cache?.Dispose();
        
        foreach (var lockObj in _locks.Values)
        {
            lockObj?.Dispose();
        }
    }
}
```

### Cache Warming Service

```csharp
public class CacheWarmingService
{
    private readonly ILogger<CacheWarmingService> _logger;
    private readonly TSKConfig _config;
    private readonly AsyncCacheManager _cacheManager;
    private readonly Timer _warmingTimer;
    
    public CacheWarmingService(
        ILogger<CacheWarmingService> logger,
        TSKConfig config,
        AsyncCacheManager cacheManager)
    {
        _logger = logger;
        _config = config;
        _cacheManager = cacheManager;
        
        var warmingInterval = TimeSpan.FromMinutes(config.Get<int>("cache.warming_interval_minutes", 30));
        _warmingTimer = new Timer(WarmCache, null, warmingInterval, warmingInterval);
    }
    
    private async void WarmCache(object? state)
    {
        try
        {
            var warmingTasks = GetWarmingTasks();
            
            _logger.LogInformation("Starting cache warming with {TaskCount} tasks", warmingTasks.Count);
            
            var stopwatch = Stopwatch.StartNew();
            
            await Task.WhenAll(warmingTasks);
            
            stopwatch.Stop();
            
            _logger.LogInformation("Cache warming completed in {Duration}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache warming failed");
        }
    }
    
    private List<Task> GetWarmingTasks()
    {
        var tasks = new List<Task>();
        
        // Warm frequently accessed data
        tasks.Add(WarmUserDataAsync());
        tasks.Add(WarmConfigurationDataAsync());
        tasks.Add(WarmReferenceDataAsync());
        
        return tasks;
    }
    
    private async Task WarmUserDataAsync()
    {
        try
        {
            // Warm active user sessions
            var activeUsers = await GetActiveUsersAsync();
            foreach (var user in activeUsers.Take(100)) // Limit to top 100
            {
                await _cacheManager.GetOrSetAsync(
                    $"user:{user.Id}",
                    () => GetUserDataAsync(user.Id),
                    TimeSpan.FromMinutes(15));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to warm user data");
        }
    }
    
    private async Task WarmConfigurationDataAsync()
    {
        try
        {
            // Warm configuration data
            var configKeys = new[] { "app.settings", "feature.flags", "api.endpoints" };
            
            foreach (var key in configKeys)
            {
                await _cacheManager.GetOrSetAsync(
                    $"config:{key}",
                    () => GetConfigurationAsync(key),
                    TimeSpan.FromMinutes(60));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to warm configuration data");
        }
    }
    
    private async Task WarmReferenceDataAsync()
    {
        try
        {
            // Warm reference data
            var referenceData = new[] { "countries", "currencies", "languages" };
            
            foreach (var dataType in referenceData)
            {
                await _cacheManager.GetOrSetAsync(
                    $"ref:{dataType}",
                    () => GetReferenceDataAsync(dataType),
                    TimeSpan.FromHours(24));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to warm reference data");
        }
    }
    
    // Placeholder methods - implement based on your data access layer
    private async Task<List<UserDto>> GetActiveUsersAsync()
    {
        await Task.Delay(100); // Simulate database call
        return new List<UserDto>();
    }
    
    private async Task<UserDto> GetUserDataAsync(int userId)
    {
        await Task.Delay(50); // Simulate database call
        return new UserDto { Id = userId };
    }
    
    private async Task<object> GetConfigurationAsync(string key)
    {
        await Task.Delay(10); // Simulate configuration lookup
        return new { Key = key, Value = "default" };
    }
    
    private async Task<object> GetReferenceDataAsync(string dataType)
    {
        await Task.Delay(20); // Simulate reference data lookup
        return new { Type = dataType, Data = new List<object>() };
    }
    
    public void Dispose()
    {
        _warmingTimer?.Dispose();
    }
}
```

## 📊 Performance Monitoring

### Performance Metrics Collector

```csharp
public class PerformanceMetricsCollector
{
    private readonly ILogger<PerformanceMetricsCollector> _logger;
    private readonly TSKConfig _config;
    private readonly Dictionary<string, MetricCollector> _collectors;
    private readonly Timer _reportingTimer;
    
    public PerformanceMetricsCollector(ILogger<PerformanceMetricsCollector> logger, TSKConfig config)
    {
        _logger = logger;
        _config = config;
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
            await SendMetricsAsync(metrics);
            
            _logger.LogDebug("Reported {Count} metrics", metrics.Count);
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
    
    private async Task SendMetricsAsync(List<Metric> metrics)
    {
        var metricsUrl = _config.Get<string>("metrics.endpoint_url");
        if (string.IsNullOrEmpty(metricsUrl))
        {
            return;
        }
        
        using var client = new HttpClient();
        var payload = new MetricsPayload
        {
            ServiceName = _config.Get<string>("app.name", "unknown"),
            Environment = _config.Get<string>("app.environment", "unknown"),
            Timestamp = DateTime.UtcNow,
            Metrics = metrics
        };
        
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await client.PostAsync(metricsUrl, content);
        response.EnsureSuccessStatusCode();
    }
    
    public void Dispose()
    {
        _reportingTimer?.Dispose();
    }
}

public class MetricsPayload
{
    public string ServiceName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public List<Metric> Metrics { get; set; } = new();
}
```

## 📝 Summary

This guide covered comprehensive performance optimization strategies for C# TuskLang applications:

- **Performance Profiling**: Profiling service and memory profiler for identifying bottlenecks
- **Async Patterns**: Async operation optimizer with concurrency control and timeout handling
- **Caching Strategies**: Multi-level cache and cache warming service for improved performance
- **Performance Monitoring**: Metrics collection and reporting for operational insights

These performance optimization strategies ensure your C# TuskLang applications are fast, scalable, and resource-efficient. 