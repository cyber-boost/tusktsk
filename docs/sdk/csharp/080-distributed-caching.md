# Distributed Caching Patterns in C# with TuskLang

## Overview

Distributed caching is essential for building high-performance, scalable applications. This guide covers various caching patterns, strategies, and implementations using C# and TuskLang configuration for optimal performance and reliability.

## Table of Contents

- [Caching Strategies](#caching-strategies)
- [Redis Integration](#redis-integration)
- [Memory Cache](#memory-cache)
- [Cache-Aside Pattern](#cache-aside-pattern)
- [Write-Through Pattern](#write-through-pattern)
- [Write-Behind Pattern](#write-behind-pattern)
- [Cache Invalidation](#cache-invalidation)
- [Cache Warming](#cache-warming)
- [Cache Monitoring](#cache-monitoring)
- [Advanced Patterns](#advanced-patterns)

## Caching Strategies

### Cache Configuration

```csharp
// Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TuskLang;

var builder = WebApplication.CreateBuilder(args);

// Load TuskLang configuration
var tuskConfig = TuskConfig.Load("caching.tsk");

builder.Services.AddDistributedCaching(tuskConfig);
builder.Services.AddMemoryCache();
builder.Services.AddRedisCache(tuskConfig);

var app = builder.Build();
app.Run();
```

### TuskLang Cache Configuration

```ini
# caching.tsk
[caching]
default_ttl = @env("CACHE_DEFAULT_TTL_SECONDS", "3600")
max_memory_size = @env("CACHE_MAX_MEMORY_MB", "1024")
enable_compression = @env("CACHE_ENABLE_COMPRESSION", "true")

[redis]
enabled = @env("REDIS_ENABLED", "true")
connection_string = @env("REDIS_CONNECTION_STRING", "localhost:6379")
database = @env("REDIS_DATABASE", "0")
key_prefix = @env("REDIS_KEY_PREFIX", "app:")
timeout = @env("REDIS_TIMEOUT_MS", "5000")

[memory_cache]
enabled = @env("MEMORY_CACHE_ENABLED", "true")
size_limit = @env("MEMORY_CACHE_SIZE_LIMIT", "1000")
compaction_percentage = @env("MEMORY_CACHE_COMPACTION_PERCENTAGE", "0.1")

[cache_patterns]
cache_aside_enabled = true
write_through_enabled = true
write_behind_enabled = false
cache_warming_enabled = true

[monitoring]
metrics_enabled = true
hit_rate_threshold = @env("CACHE_HIT_RATE_THRESHOLD", "0.8")
slow_query_threshold = @env("CACHE_SLOW_QUERY_MS", "100")
```

## Redis Integration

### Redis Cache Service

```csharp
// IRedisCacheService.cs
public interface IRedisCacheService
{
    Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<TimeSpan?> GetTimeToLiveAsync(string key, CancellationToken cancellationToken = default);
    Task SetExpiryAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);
}

// RedisCacheService.cs
public class RedisCacheService : IRedisCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IConfiguration _config;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly string _keyPrefix;
    private readonly IDatabase _database;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        IConfiguration config,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _config = config;
        _logger = logger;
        _keyPrefix = _config.GetValue<string>("redis:key_prefix", "app:");
        _database = _redis.GetDatabase();
    }

    public async Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = $"{_keyPrefix}{key}";
        
        try
        {
            var value = await _database.StringGetAsync(fullKey);
            
            if (!value.HasValue)
            {
                _logger.LogDebug("Cache miss for key: {Key}", fullKey);
                return default(T);
            }

            var json = value.ToString();
            var result = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogDebug("Cache hit for key: {Key}", fullKey);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache for key: {Key}", fullKey);
            return default(T);
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var fullKey = $"{_keyPrefix}{key}";
        
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(fullKey, json, expiry);
            
            _logger.LogDebug("Cached value for key: {Key} with expiry: {Expiry}", 
                fullKey, expiry?.ToString() ?? "none");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key: {Key}", fullKey);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = $"{_keyPrefix}{key}";
        
        try
        {
            await _database.KeyDeleteAsync(fullKey);
            _logger.LogDebug("Removed cache key: {Key}", fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", fullKey);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = $"{_keyPrefix}{key}";
        
        try
        {
            return await _database.KeyExistsAsync(fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence for key: {Key}", fullKey);
            return false;
        }
    }

    public async Task<TimeSpan?> GetTimeToLiveAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = $"{_keyPrefix}{key}";
        
        try
        {
            var ttl = await _database.KeyTimeToLiveAsync(fullKey);
            return ttl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting TTL for key: {Key}", fullKey);
            return null;
        }
    }

    public async Task SetExpiryAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        var fullKey = $"{_keyPrefix}{key}";
        
        try
        {
            await _database.KeyExpireAsync(fullKey, expiry);
            _logger.LogDebug("Set expiry for key: {Key} to {Expiry}", fullKey, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting expiry for key: {Key}", fullKey);
        }
    }
}
```

### Redis Configuration

```csharp
// RedisConfiguration.cs
public static class RedisConfiguration
{
    public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetValue<string>("redis:connection_string");
        var database = config.GetValue<int>("redis:database", 0);
        var timeout = config.GetValue<int>("redis:timeout", 5000);

        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var options = ConfigurationOptions.Parse(connectionString);
            options.ConnectTimeout = timeout;
            options.SyncTimeout = timeout;
            options.AbortConnect = false;
            
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<IRedisCacheService, RedisCacheService>();
        
        return services;
    }
}
```

## Memory Cache

### Memory Cache Service

```csharp
// IMemoryCacheService.cs
public interface IMemoryCacheService
{
    T Get<T>(string key);
    void Set<T>(string key, T value, TimeSpan? expiry = null);
    void Remove(string key);
    bool TryGetValue<T>(string key, out T value);
    void Clear();
}

// MemoryCacheService.cs
public class MemoryCacheService : IMemoryCacheService
{
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private readonly ILogger<MemoryCacheService> _logger;

    public MemoryCacheService(
        IMemoryCache cache,
        IConfiguration config,
        ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _config = config;
        _logger = logger;
    }

    public T Get<T>(string key)
    {
        try
        {
            if (_cache.TryGetValue(key, out T value))
            {
                _logger.LogDebug("Memory cache hit for key: {Key}", key);
                return value;
            }

            _logger.LogDebug("Memory cache miss for key: {Key}", key);
            return default(T);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from memory cache for key: {Key}", key);
            return default(T);
        }
    }

    public void Set<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var options = new MemoryCacheEntryOptions();
            
            if (expiry.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiry;
            }

            _cache.Set(key, value, options);
            _logger.LogDebug("Set memory cache for key: {Key} with expiry: {Expiry}", 
                key, expiry?.ToString() ?? "none");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting memory cache for key: {Key}", key);
        }
    }

    public void Remove(string key)
    {
        try
        {
            _cache.Remove(key);
            _logger.LogDebug("Removed memory cache key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing memory cache key: {Key}", key);
        }
    }

    public bool TryGetValue<T>(string key, out T value)
    {
        try
        {
            return _cache.TryGetValue(key, out value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error trying to get value from memory cache for key: {Key}", key);
            value = default(T);
            return false;
        }
    }

    public void Clear()
    {
        try
        {
            if (_cache is MemoryCache memoryCache)
            {
                memoryCache.Compact(1.0);
            }
            _logger.LogInformation("Cleared memory cache");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing memory cache");
        }
    }
}
```

## Cache-Aside Pattern

### Cache-Aside Implementation

```csharp
// ICacheAsideService.cs
public interface ICacheAsideService
{
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

// CacheAsideService.cs
public class CacheAsideService : ICacheAsideService
{
    private readonly IRedisCacheService _redisCache;
    private readonly IMemoryCacheService _memoryCache;
    private readonly IConfiguration _config;
    private readonly ILogger<CacheAsideService> _logger;
    private readonly SemaphoreSlim _semaphore;

    public CacheAsideService(
        IRedisCacheService redisCache,
        IMemoryCacheService memoryCache,
        IConfiguration config,
        ILogger<CacheAsideService> logger)
    {
        _redisCache = redisCache;
        _memoryCache = memoryCache;
        _config = config;
        _logger = logger;
        _semaphore = new SemaphoreSlim(1, 1);
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        // Try memory cache first
        var memoryValue = _memoryCache.Get<T>(key);
        if (memoryValue != null)
        {
            _logger.LogDebug("Memory cache hit for key: {Key}", key);
            return memoryValue;
        }

        // Try Redis cache
        var redisValue = await _redisCache.GetAsync<T>(key, cancellationToken);
        if (redisValue != null)
        {
            // Store in memory cache for faster subsequent access
            _memoryCache.Set(key, redisValue, TimeSpan.FromMinutes(5));
            _logger.LogDebug("Redis cache hit for key: {Key}", key);
            return redisValue;
        }

        // Cache miss - acquire lock to prevent multiple simultaneous loads
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            redisValue = await _redisCache.GetAsync<T>(key, cancellationToken);
            if (redisValue != null)
            {
                _memoryCache.Set(key, redisValue, TimeSpan.FromMinutes(5));
                return redisValue;
            }

            // Load from factory
            _logger.LogInformation("Cache miss for key: {Key}, loading from factory", key);
            var value = await factory();

            // Store in both caches
            await _redisCache.SetAsync(key, value, expiry, cancellationToken);
            _memoryCache.Set(key, value, TimeSpan.FromMinutes(5));

            return value;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        // Try memory cache first
        var memoryValue = _memoryCache.Get<T>(key);
        if (memoryValue != null)
        {
            return memoryValue;
        }

        // Try Redis cache
        var redisValue = await _redisCache.GetAsync<T>(key, cancellationToken);
        if (redisValue != null)
        {
            _memoryCache.Set(key, redisValue, TimeSpan.FromMinutes(5));
        }

        return redisValue;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        await _redisCache.SetAsync(key, value, expiry, cancellationToken);
        _memoryCache.Set(key, value, TimeSpan.FromMinutes(5));
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _redisCache.RemoveAsync(key, cancellationToken);
        _memoryCache.Remove(key);
    }
}
```

## Write-Through Pattern

### Write-Through Implementation

```csharp
// IWriteThroughService.cs
public interface IWriteThroughService
{
    Task<T> CreateAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task<T> UpdateAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}

// WriteThroughService.cs
public class WriteThroughService : IWriteThroughService
{
    private readonly ICacheAsideService _cacheAside;
    private readonly IDataStore _dataStore;
    private readonly IConfiguration _config;
    private readonly ILogger<WriteThroughService> _logger;

    public WriteThroughService(
        ICacheAsideService cacheAside,
        IDataStore dataStore,
        IConfiguration config,
        ILogger<WriteThroughService> logger)
    {
        _cacheAside = cacheAside;
        _dataStore = dataStore;
        _config = config;
        _logger = logger;
    }

    public async Task<T> CreateAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Write to data store first
            await _dataStore.CreateAsync(key, value, cancellationToken);
            
            // Then update cache
            await _cacheAside.SetAsync(key, value, expiry, cancellationToken);
            
            _logger.LogInformation("Created and cached value for key: {Key}", key);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in write-through create for key: {Key}", key);
            throw;
        }
    }

    public async Task<T> UpdateAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Update data store first
            await _dataStore.UpdateAsync(key, value, cancellationToken);
            
            // Then update cache
            await _cacheAside.SetAsync(key, value, expiry, cancellationToken);
            
            _logger.LogInformation("Updated and cached value for key: {Key}", key);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in write-through update for key: {Key}", key);
            throw;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            // Delete from data store first
            await _dataStore.DeleteAsync(key, cancellationToken);
            
            // Then remove from cache
            await _cacheAside.RemoveAsync(key, cancellationToken);
            
            _logger.LogInformation("Deleted value for key: {Key} from data store and cache", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in write-through delete for key: {Key}", key);
            throw;
        }
    }
}
```

## Write-Behind Pattern

### Write-Behind Implementation

```csharp
// IWriteBehindService.cs
public interface IWriteBehindService
{
    Task<T> SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task FlushAsync(CancellationToken cancellationToken = default);
}

// WriteBehindService.cs
public class WriteBehindService : IWriteBehindService
{
    private readonly ICacheAsideService _cacheAside;
    private readonly IDataStore _dataStore;
    private readonly IConfiguration _config;
    private readonly ILogger<WriteBehindService> _logger;
    private readonly Channel<WriteOperation> _writeChannel;
    private readonly Task _backgroundTask;

    public WriteBehindService(
        ICacheAsideService cacheAside,
        IDataStore dataStore,
        IConfiguration config,
        ILogger<WriteBehindService> logger)
    {
        _cacheAside = cacheAside;
        _dataStore = dataStore;
        _config = config;
        _logger = logger;
        _writeChannel = Channel.CreateUnbounded<WriteOperation>();
        _backgroundTask = ProcessWritesAsync();
    }

    public async Task<T> SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        // Update cache immediately
        await _cacheAside.SetAsync(key, value, expiry, cancellationToken);
        
        // Queue write to data store
        var operation = new WriteOperation
        {
            Key = key,
            Value = value,
            OperationType = WriteOperationType.Set,
            Expiry = expiry
        };

        await _writeChannel.Writer.WriteAsync(operation, cancellationToken);
        
        _logger.LogDebug("Queued write operation for key: {Key}", key);
        return value;
    }

    public async Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return await _cacheAside.GetAsync<T>(key, cancellationToken);
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        _writeChannel.Writer.Complete();
        await _backgroundTask;
    }

    private async Task ProcessWritesAsync()
    {
        var batchSize = _config.GetValue<int>("cache_patterns:write_behind_batch_size", 100);
        var batchTimeout = TimeSpan.FromSeconds(_config.GetValue<int>("cache_patterns:write_behind_timeout_seconds", 30));
        
        var batch = new List<WriteOperation>();
        var timer = new Timer(_ => { }, null, batchTimeout, batchTimeout);

        try
        {
            await foreach (var operation in _writeChannel.Reader.ReadAllAsync())
            {
                batch.Add(operation);

                if (batch.Count >= batchSize)
                {
                    await ProcessBatchAsync(batch);
                    batch.Clear();
                }
            }

            // Process remaining items
            if (batch.Any())
            {
                await ProcessBatchAsync(batch);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in write-behind processing");
        }
        finally
        {
            timer.Dispose();
        }
    }

    private async Task ProcessBatchAsync(List<WriteOperation> batch)
    {
        try
        {
            foreach (var operation in batch)
            {
                switch (operation.OperationType)
                {
                    case WriteOperationType.Set:
                        await _dataStore.SetAsync(operation.Key, operation.Value);
                        break;
                    case WriteOperationType.Delete:
                        await _dataStore.DeleteAsync(operation.Key);
                        break;
                }
            }

            _logger.LogInformation("Processed {Count} write operations", batch.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing write batch of {Count} operations", batch.Count);
        }
    }
}

public class WriteOperation
{
    public string Key { get; set; }
    public object Value { get; set; }
    public WriteOperationType OperationType { get; set; }
    public TimeSpan? Expiry { get; set; }
}

public enum WriteOperationType
{
    Set,
    Delete
}
```

## Cache Invalidation

### Cache Invalidation Strategies

```csharp
// ICacheInvalidationService.cs
public interface ICacheInvalidationService
{
    Task InvalidateAsync(string key, CancellationToken cancellationToken = default);
    Task InvalidatePatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task InvalidateAllAsync(CancellationToken cancellationToken = default);
}

// CacheInvalidationService.cs
public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly IRedisCacheService _redisCache;
    private readonly IMemoryCacheService _memoryCache;
    private readonly IConfiguration _config;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationService(
        IRedisCacheService redisCache,
        IMemoryCacheService memoryCache,
        IConfiguration config,
        ILogger<CacheInvalidationService> logger)
    {
        _redisCache = redisCache;
        _memoryCache = memoryCache;
        _config = config;
        _logger = logger;
    }

    public async Task InvalidateAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _redisCache.RemoveAsync(key, cancellationToken);
            _memoryCache.Remove(key);
            
            _logger.LogInformation("Invalidated cache for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for key: {Key}", key);
        }
    }

    public async Task InvalidatePatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            // Note: This is a simplified implementation
            // In a real scenario, you'd need to scan Redis keys
            _logger.LogInformation("Pattern invalidation requested for pattern: {Pattern}", pattern);
            
            // For now, we'll just clear memory cache
            _memoryCache.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache pattern: {Pattern}", pattern);
        }
    }

    public async Task InvalidateAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Clear memory cache
            _memoryCache.Clear();
            
            // For Redis, you'd typically use FLUSHDB or scan and delete keys
            _logger.LogInformation("Invalidated all cache entries");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating all cache entries");
        }
    }
}
```

## Cache Warming

### Cache Warming Service

```csharp
// ICacheWarmingService.cs
public interface ICacheWarmingService
{
    Task WarmCacheAsync(CancellationToken cancellationToken = default);
    Task WarmSpecificKeysAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);
}

// CacheWarmingService.cs
public class CacheWarmingService : ICacheWarmingService
{
    private readonly ICacheAsideService _cacheAside;
    private readonly IConfiguration _config;
    private readonly ILogger<CacheWarmingService> _logger;
    private readonly IDataStore _dataStore;

    public CacheWarmingService(
        ICacheAsideService cacheAside,
        IConfiguration config,
        ILogger<CacheWarmingService> logger,
        IDataStore dataStore)
    {
        _cacheAside = cacheAside;
        _config = config;
        _logger = logger;
        _dataStore = dataStore;
    }

    public async Task WarmCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var warmingKeys = _config.GetSection("cache_warming:keys").Get<string[]>();
            if (warmingKeys != null)
            {
                await WarmSpecificKeysAsync(warmingKeys, cancellationToken);
            }

            _logger.LogInformation("Cache warming completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache warming");
        }
    }

    public async Task WarmSpecificKeysAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var tasks = keys.Select(key => WarmKeyAsync(key, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private async Task WarmKeyAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await _cacheAside.GetOrSetAsync(key, 
                async () => await _dataStore.GetAsync(key, cancellationToken),
                TimeSpan.FromHours(1),
                cancellationToken);
            
            _logger.LogDebug("Warmed cache for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming cache for key: {Key}", key);
        }
    }
}
```

## Cache Monitoring

### Cache Metrics

```csharp
// CacheMetrics.cs
public class CacheMetrics
{
    private readonly IConfiguration _config;
    private readonly ILogger<CacheMetrics> _logger;
    private readonly Counter _hitCounter;
    private readonly Counter _missCounter;
    private readonly Histogram _responseTimeHistogram;
    private readonly Gauge _memoryUsageGauge;

    public CacheMetrics(IConfiguration config, ILogger<CacheMetrics> logger)
    {
        _config = config;
        _logger = logger;
        
        _hitCounter = Metrics.CreateCounter("cache_hits_total", "Total cache hits");
        _missCounter = Metrics.CreateCounter("cache_misses_total", "Total cache misses");
        _responseTimeHistogram = Metrics.CreateHistogram("cache_response_time_seconds", "Cache response time");
        _memoryUsageGauge = Metrics.CreateGauge("cache_memory_usage_bytes", "Cache memory usage");
    }

    public void RecordHit(string cacheType, string key)
    {
        _hitCounter.WithLabels(cacheType).Inc();
        _logger.LogDebug("Cache hit: {CacheType} - {Key}", cacheType, key);
    }

    public void RecordMiss(string cacheType, string key)
    {
        _missCounter.WithLabels(cacheType).Inc();
        _logger.LogDebug("Cache miss: {CacheType} - {Key}", cacheType, key);
    }

    public void RecordResponseTime(string cacheType, TimeSpan duration)
    {
        _responseTimeHistogram.WithLabels(cacheType).Observe(duration.TotalSeconds);
    }

    public void RecordMemoryUsage(long bytes)
    {
        _memoryUsageGauge.Set(bytes);
    }

    public double GetHitRate(string cacheType)
    {
        var hits = _hitCounter.WithLabels(cacheType).Value;
        var misses = _missCounter.WithLabels(cacheType).Value;
        var total = hits + misses;
        
        return total > 0 ? hits / total : 0.0;
    }
}
```

### Cache Health Check

```csharp
// CacheHealthCheck.cs
public class CacheHealthCheck : IHealthCheck
{
    private readonly IRedisCacheService _redisCache;
    private readonly IMemoryCacheService _memoryCache;
    private readonly IConfiguration _config;
    private readonly ILogger<CacheHealthCheck> _logger;

    public CacheHealthCheck(
        IRedisCacheService redisCache,
        IMemoryCacheService memoryCache,
        IConfiguration config,
        ILogger<CacheHealthCheck> logger)
    {
        _redisCache = redisCache;
        _memoryCache = memoryCache;
        _config = config;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var healthChecks = new List<HealthCheckResult>();

        // Check Redis
        try
        {
            var testKey = "health_check_test";
            await _redisCache.SetAsync(testKey, "test", TimeSpan.FromSeconds(10), cancellationToken);
            var value = await _redisCache.GetAsync<string>(testKey, cancellationToken);
            
            if (value == "test")
            {
                healthChecks.Add(HealthCheckResult.Healthy("Redis cache is healthy"));
            }
            else
            {
                healthChecks.Add(HealthCheckResult.Unhealthy("Redis cache is not responding correctly"));
            }
        }
        catch (Exception ex)
        {
            healthChecks.Add(HealthCheckResult.Unhealthy("Redis cache is unhealthy", ex));
        }

        // Check Memory Cache
        try
        {
            var testKey = "memory_health_check_test";
            _memoryCache.Set(testKey, "test", TimeSpan.FromSeconds(10));
            var value = _memoryCache.Get<string>(testKey);
            
            if (value == "test")
            {
                healthChecks.Add(HealthCheckResult.Healthy("Memory cache is healthy"));
            }
            else
            {
                healthChecks.Add(HealthCheckResult.Unhealthy("Memory cache is not responding correctly"));
            }
        }
        catch (Exception ex)
        {
            healthChecks.Add(HealthCheckResult.Unhealthy("Memory cache is unhealthy", ex));
        }

        if (healthChecks.All(h => h.Status == HealthStatus.Healthy))
        {
            return HealthCheckResult.Healthy("All cache systems are healthy");
        }

        var unhealthyChecks = healthChecks.Where(h => h.Status != HealthStatus.Healthy);
        return HealthCheckResult.Unhealthy("Some cache systems are unhealthy", 
            data: unhealthyChecks.ToDictionary(h => h.Description, h => h.Exception?.Message));
    }
}
```

## Advanced Patterns

### Cache-Aside with Background Refresh

```csharp
// BackgroundRefreshCacheService.cs
public class BackgroundRefreshCacheService
{
    private readonly ICacheAsideService _cacheAside;
    private readonly IConfiguration _config;
    private readonly ILogger<BackgroundRefreshCacheService> _logger;
    private readonly Dictionary<string, Timer> _refreshTimers;

    public BackgroundRefreshCacheService(
        ICacheAsideService cacheAside,
        IConfiguration config,
        ILogger<BackgroundRefreshCacheService> logger)
    {
        _cacheAside = cacheAside;
        _config = config;
        _logger = logger;
        _refreshTimers = new Dictionary<string, Timer>();
    }

    public async Task<T> GetWithBackgroundRefreshAsync<T>(
        string key, 
        Func<Task<T>> factory, 
        TimeSpan cacheExpiry,
        TimeSpan refreshInterval,
        CancellationToken cancellationToken = default)
    {
        var value = await _cacheAside.GetOrSetAsync(key, factory, cacheExpiry, cancellationToken);
        
        // Schedule background refresh
        ScheduleBackgroundRefresh(key, factory, refreshInterval);
        
        return value;
    }

    private void ScheduleBackgroundRefresh<T>(string key, Func<Task<T>> factory, TimeSpan interval)
    {
        if (_refreshTimers.ContainsKey(key))
        {
            _refreshTimers[key].Dispose();
        }

        var timer = new Timer(async _ =>
        {
            try
            {
                var value = await factory();
                await _cacheAside.SetAsync(key, value, TimeSpan.FromHours(1));
                _logger.LogDebug("Background refresh completed for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background refresh for key: {Key}", key);
            }
        }, null, interval, interval);

        _refreshTimers[key] = timer;
    }
}
```

## Best Practices

### Caching Best Practices

1. **Cache Key Design**: Use consistent, predictable cache key patterns
2. **TTL Strategy**: Set appropriate TTL values based on data volatility
3. **Cache Size**: Monitor and limit cache size to prevent memory issues
4. **Cache Warming**: Pre-populate cache with frequently accessed data
5. **Cache Invalidation**: Implement proper cache invalidation strategies
6. **Monitoring**: Monitor cache hit rates and performance metrics

### Performance Optimization

1. **Serialization**: Use efficient serialization formats (JSON, MessagePack)
2. **Compression**: Enable compression for large objects
3. **Connection Pooling**: Use connection pooling for Redis
4. **Batch Operations**: Use batch operations when possible
5. **Async Operations**: Use async/await patterns consistently

### Security Considerations

1. **Access Control**: Implement proper access control for cache data
2. **Encryption**: Encrypt sensitive data in cache
3. **Key Isolation**: Use key prefixes to isolate different applications
4. **Network Security**: Secure network connections to cache servers

## Conclusion

Distributed caching is a powerful technique for improving application performance and scalability. By implementing these patterns with C# and TuskLang, you can build robust caching solutions that provide excellent performance while maintaining data consistency and reliability.

The combination of Redis for distributed caching and memory cache for local caching, along with proper cache invalidation and warming strategies, provides a comprehensive caching solution for modern applications. 