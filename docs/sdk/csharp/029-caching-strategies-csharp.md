# 🗃️ Caching Strategies - TuskLang for C# - "Cache Mastery"

**Master caching strategies with TuskLang in your C# applications!**

Caching is essential for performance and scalability. This guide covers in-memory caching, distributed caching, Redis integration, and real-world caching scenarios for TuskLang in C# environments.

## ⚡ Caching Philosophy

### "We Don't Bow to Any King"
- **Speed first** - Cache what matters most
- **Smart invalidation** - Invalidate intelligently
- **Distributed caching** - Scale across instances
- **Cache warming** - Pre-populate critical data
- **Performance monitoring** - Track cache effectiveness

## 🧠 In-Memory Caching

### Example: In-Memory Cache Service
```csharp
// InMemoryCacheService.cs
using Microsoft.Extensions.Caching.Memory;

public class InMemoryCacheService
{
    private readonly IMemoryCache _cache;
    private readonly TuskLang _parser;
    private readonly ILogger<InMemoryCacheService> _logger;
    
    public InMemoryCacheService(IMemoryCache cache, ILogger<InMemoryCacheService> logger)
    {
        _cache = cache;
        _parser = new TuskLang();
        _logger = logger;
    }
    
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        if (_cache.TryGetValue(key, out T? cachedValue))
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return cachedValue;
        }
        
        _logger.LogDebug("Cache miss for key: {Key}", key);
        
        var value = await factory();
        
        var cacheEntryOptions = new MemoryCacheEntryOptions();
        if (expiration.HasValue)
        {
            cacheEntryOptions.AbsoluteExpirationRelativeToNow = expiration;
        }
        
        _cache.Set(key, value, cacheEntryOptions);
        
        _logger.LogInformation("Cached value for key: {Key} with expiration: {Expiration}", 
            key, expiration?.ToString() ?? "default");
        
        return value;
    }
    
    public void Remove(string key)
    {
        _cache.Remove(key);
        _logger.LogInformation("Removed cache entry for key: {Key}", key);
    }
    
    public async Task<Dictionary<string, object>> GetCacheStatisticsAsync()
    {
        var stats = new Dictionary<string, object>();
        
        // Note: IMemoryCache doesn't provide built-in statistics
        // This would typically come from a custom implementation or monitoring
        stats["cache_type"] = "in_memory";
        stats["implementation"] = "Microsoft.Extensions.Caching.Memory";
        
        return stats;
    }
}
```

## 🌐 Distributed Caching

### Example: Redis Cache Service
```csharp
// RedisCacheService.cs
using StackExchange.Redis;

public class RedisCacheService
{
    private readonly IDatabase _database;
    private readonly TuskLang _parser;
    private readonly ILogger<RedisCacheService> _logger;
    
    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _database = redis.GetDatabase();
        _parser = new TuskLang();
        _logger = logger;
    }
    
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            
            if (value.HasValue)
            {
                _logger.LogDebug("Redis cache hit for key: {Key}", key);
                return JsonSerializer.Deserialize<T>(value);
            }
            
            _logger.LogDebug("Redis cache miss for key: {Key}", key);
            return default(T);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get value from Redis for key: {Key}", key);
            return default(T);
        }
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, serializedValue, expiration);
            
            _logger.LogInformation("Cached value in Redis for key: {Key} with expiration: {Expiration}", 
                key, expiration?.ToString() ?? "no expiration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set value in Redis for key: {Key}", key);
            throw;
        }
    }
    
    public async Task<bool> RemoveAsync(string key)
    {
        try
        {
            var result = await _database.KeyDeleteAsync(key);
            _logger.LogInformation("Removed Redis cache entry for key: {Key}", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove Redis cache entry for key: {Key}", key);
            return false;
        }
    }
    
    public async Task<Dictionary<string, object>> GetCacheStatisticsAsync()
    {
        var stats = new Dictionary<string, object>();
        
        try
        {
            var server = _database.Multiplexer.GetServer(_database.Multiplexer.GetEndPoints().First());
            var info = await server.InfoAsync();
            
            stats["cache_type"] = "redis";
            stats["connected_clients"] = info.FirstOrDefault(x => x.Key == "connected_clients").Value;
            stats["used_memory"] = info.FirstOrDefault(x => x.Key == "used_memory_human").Value;
            stats["total_commands_processed"] = info.FirstOrDefault(x => x.Key == "total_commands_processed").Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Redis statistics");
            stats["error"] = ex.Message;
        }
        
        return stats;
    }
}
```

## 🔄 Cache Warming

### Example: Cache Warming Service
```csharp
// CacheWarmingService.cs
public class CacheWarmingService
{
    private readonly InMemoryCacheService _memoryCache;
    private readonly RedisCacheService _redisCache;
    private readonly TuskLang _parser;
    private readonly ILogger<CacheWarmingService> _logger;
    
    public CacheWarmingService(
        InMemoryCacheService memoryCache,
        RedisCacheService redisCache,
        ILogger<CacheWarmingService> logger)
    {
        _memoryCache = memoryCache;
        _redisCache = redisCache;
        _parser = new TuskLang();
        _logger = logger;
    }
    
    public async Task WarmCacheAsync()
    {
        _logger.LogInformation("Starting cache warming process");
        
        try
        {
            // Load cache warming configuration
            var config = _parser.ParseFile("config/cache-warming.tsk");
            var warmingTasks = config["warming_tasks"] as List<object>;
            
            var tasks = new List<Task>();
            
            foreach (var task in warmingTasks ?? Enumerable.Empty<object>())
            {
                var taskDict = task as Dictionary<string, object>;
                if (taskDict != null)
                {
                    tasks.Add(WarmCacheTaskAsync(taskDict));
                }
            }
            
            await Task.WhenAll(tasks);
            
            _logger.LogInformation("Cache warming completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache warming failed");
            throw;
        }
    }
    
    private async Task WarmCacheTaskAsync(Dictionary<string, object> task)
    {
        var taskName = task["name"].ToString();
        var cacheType = task["cache_type"].ToString();
        var key = task["key"].ToString();
        var dataSource = task["data_source"].ToString();
        var expiration = TimeSpan.FromMinutes(int.Parse(task["expiration_minutes"].ToString()));
        
        _logger.LogInformation("Warming cache task: {TaskName}", taskName);
        
        try
        {
            var data = await LoadDataFromSourceAsync(dataSource);
            
            switch (cacheType.ToLower())
            {
                case "memory":
                    await _memoryCache.GetOrSetAsync(key, () => Task.FromResult(data), expiration);
                    break;
                case "redis":
                    await _redisCache.SetAsync(key, data, expiration);
                    break;
                default:
                    _logger.LogWarning("Unknown cache type: {CacheType}", cacheType);
                    break;
            }
            
            _logger.LogInformation("Successfully warmed cache for task: {TaskName}", taskName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to warm cache for task: {TaskName}", taskName);
        }
    }
    
    private async Task<object> LoadDataFromSourceAsync(string dataSource)
    {
        // This would load data from various sources (database, API, file, etc.)
        // For now, return placeholder data
        await Task.Delay(100); // Simulate data loading
        
        return new Dictionary<string, object>
        {
            ["source"] = dataSource,
            ["loaded_at"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            ["data"] = "Sample data from " + dataSource
        };
    }
}
```

## 📊 Cache Performance Monitoring

### Example: Cache Performance Service
```csharp
// CachePerformanceService.cs
public class CachePerformanceService
{
    private readonly InMemoryCacheService _memoryCache;
    private readonly RedisCacheService _redisCache;
    private readonly TuskLang _parser;
    private readonly ILogger<CachePerformanceService> _logger;
    private readonly Dictionary<string, CacheMetrics> _metrics;
    
    public CachePerformanceService(
        InMemoryCacheService memoryCache,
        RedisCacheService redisCache,
        ILogger<CachePerformanceService> logger)
    {
        _memoryCache = memoryCache;
        _redisCache = redisCache;
        _parser = new TuskLang();
        _logger = logger;
        _metrics = new Dictionary<string, CacheMetrics>();
    }
    
    public void RecordCacheHit(string cacheType, string key)
    {
        var metricKey = $"{cacheType}_{key}";
        if (!_metrics.ContainsKey(metricKey))
        {
            _metrics[metricKey] = new CacheMetrics();
        }
        
        _metrics[metricKey].Hits++;
        _metrics[metricKey].LastAccess = DateTime.UtcNow;
    }
    
    public void RecordCacheMiss(string cacheType, string key)
    {
        var metricKey = $"{cacheType}_{key}";
        if (!_metrics.ContainsKey(metricKey))
        {
            _metrics[metricKey] = new CacheMetrics();
        }
        
        _metrics[metricKey].Misses++;
        _metrics[metricKey].LastAccess = DateTime.UtcNow;
    }
    
    public async Task<Dictionary<string, object>> GetPerformanceReportAsync()
    {
        var report = new Dictionary<string, object>();
        
        // Memory cache statistics
        var memoryStats = await _memoryCache.GetCacheStatisticsAsync();
        report["memory_cache"] = memoryStats;
        
        // Redis cache statistics
        var redisStats = await _redisCache.GetCacheStatisticsAsync();
        report["redis_cache"] = redisStats;
        
        // Performance metrics
        var performanceMetrics = _metrics.Select(kvp => new Dictionary<string, object>
        {
            ["key"] = kvp.Key,
            ["hits"] = kvp.Value.Hits,
            ["misses"] = kvp.Value.Misses,
            ["hit_rate"] = kvp.Value.HitRate,
            ["last_access"] = kvp.Value.LastAccess.ToString("yyyy-MM-dd HH:mm:ss")
        }).ToList();
        
        report["performance_metrics"] = performanceMetrics;
        
        // Overall statistics
        var totalHits = _metrics.Values.Sum(m => m.Hits);
        var totalMisses = _metrics.Values.Sum(m => m.Misses);
        var overallHitRate = totalHits + totalMisses > 0 ? (double)totalHits / (totalHits + totalMisses) : 0;
        
        report["overall_statistics"] = new Dictionary<string, object>
        {
            ["total_hits"] = totalHits,
            ["total_misses"] = totalMisses,
            ["overall_hit_rate"] = overallHitRate,
            ["total_requests"] = totalHits + totalMisses
        };
        
        return report;
    }
}

public class CacheMetrics
{
    public int Hits { get; set; }
    public int Misses { get; set; }
    public DateTime LastAccess { get; set; }
    public double HitRate => Hits + Misses > 0 ? (double)Hits / (Hits + Misses) : 0;
}
```

## 🛠️ Real-World Caching Scenarios
- **Database query caching**: Cache frequently accessed database results
- **API response caching**: Cache external API responses
- **Session caching**: Cache user session data
- **Configuration caching**: Cache application configuration

## 🧩 Best Practices
- Cache frequently accessed data
- Use appropriate cache expiration times
- Implement cache warming for critical data
- Monitor cache performance and hit rates
- Use distributed caching for scalability

## 🏁 You're Ready!

You can now:
- Implement in-memory and distributed caching
- Use Redis for high-performance caching
- Implement cache warming strategies
- Monitor cache performance

**Next:** [Security Best Practices](030-security-csharp.md)

---

**"We don't bow to any king" - Your cache mastery, your performance excellence, your speed optimization.**

Cache intelligently. Perform exceptionally. 🗃️ 