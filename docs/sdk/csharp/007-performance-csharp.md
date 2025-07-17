# ⚡ Performance Optimization - TuskLang for C# - "Lightning Fast Configuration"

**Optimize your TuskLang configuration for production - From milliseconds to microseconds, every optimization counts!**

Performance is critical in production environments. TuskLang provides powerful optimization features that can transform your configuration from a bottleneck into a performance accelerator. Learn how to make your configuration lightning fast.

## 🎯 Performance Philosophy

### "We Don't Bow to Any King"
- **Millisecond responses** - Configuration should be instant
- **Intelligent caching** - Cache what matters, skip what doesn't
- **Database optimization** - Efficient queries and connection pooling
- **Memory efficiency** - Minimal memory footprint
- **Scalability** - Handle thousands of requests per second

### Why Performance Matters?
- **User experience** - Fast configuration means fast applications
- **Resource efficiency** - Lower CPU and memory usage
- **Scalability** - Handle more load with fewer resources
- **Cost optimization** - Reduce infrastructure costs
- **Reliability** - Fast systems are more reliable

## 🚀 Caching Strategies

### Intelligent Caching with @cache

```ini
# app.tsk - Advanced caching strategies
[performance_optimized]
# Cache expensive database queries
user_count: @cache("5m", @query("SELECT COUNT(*) FROM users"))
active_users: @cache("1m", @query("SELECT COUNT(*) FROM users WHERE active = 1"))

# Cache external API calls
weather_data: @cache("30m", @http("GET", "https://api.weatherapi.com/v1/current.json?key=${api_key}&q=London"))
stock_price: @cache("1m", @http("GET", "https://api.stockapi.com/v1/quote?symbol=${symbol}"))

# Cache ML predictions
load_prediction: @cache("30s", @predict("server_load", @metrics("cpu_usage", 75)))
optimal_workers: @cache("5m", @learn("worker_count", 4))

# Adaptive caching based on load
adaptive_cache: @cache(@if(@metrics("cpu_usage", 0) > 80, "30s", "5m"), @query("SELECT * FROM heavy_table"))
```

### C# Caching Implementation

```csharp
using TuskLang;
using TuskLang.Caching;
using Microsoft.Extensions.Caching.Distributed;

public class OptimizedConfigurationService
{
    private readonly TuskLang _parser;
    private readonly ICacheProvider _cacheProvider;
    private readonly IDistributedCache _distributedCache;
    
    public OptimizedConfigurationService()
    {
        _parser = new TuskLang();
        
        // Multi-layer caching
        _cacheProvider = new MultiLayerCacheProvider(new ICacheProvider[]
        {
            new MemoryCacheProvider(),           // L1: In-memory cache
            new RedisCacheProvider(new RedisConfig
            {
                Host = "localhost",
                Port = 6379
            }),                                  // L2: Redis cache
            new DatabaseCacheProvider()          // L3: Database cache
        });
        
        _parser.SetCacheProvider(_cacheProvider);
    }
    
    public async Task<Dictionary<string, object>> GetOptimizedConfigurationAsync(string filePath)
    {
        // Parse with intelligent caching
        var config = _parser.ParseFile(filePath);
        
        // Monitor cache performance
        var stats = await _cacheProvider.GetStatsAsync();
        Console.WriteLine($"L1 Hit Rate: {stats.LayerStats[0].HitRate:P}");
        Console.WriteLine($"L2 Hit Rate: {stats.LayerStats[1].HitRate:P}");
        Console.WriteLine($"L3 Hit Rate: {stats.LayerStats[2].HitRate:P}");
        
        return config;
    }
    
    public async Task PreloadCacheAsync()
    {
        // Preload frequently accessed data
        var preloadTasks = new[]
        {
            PreloadUserCountAsync(),
            PreloadActiveUsersAsync(),
            PreloadWeatherDataAsync()
        };
        
        await Task.WhenAll(preloadTasks);
    }
    
    private async Task PreloadUserCountAsync()
    {
        var key = "user_count";
        var value = await _cacheProvider.GetAsync(key);
        if (value == null)
        {
            // Load from database and cache
            var count = await GetUserCountFromDatabaseAsync();
            await _cacheProvider.SetAsync(key, count, TimeSpan.FromMinutes(5));
        }
    }
    
    private async Task<int> GetUserCountFromDatabaseAsync()
    {
        // Simulate database query
        await Task.Delay(100);
        return 1000;
    }
}
```

### Cache Invalidation Strategies

```csharp
public class CacheInvalidationService
{
    private readonly ICacheProvider _cacheProvider;
    
    public CacheInvalidationService(ICacheProvider cacheProvider)
    {
        _cacheProvider = cacheProvider;
    }
    
    public async Task InvalidateUserCacheAsync(int userId)
    {
        // Invalidate user-specific cache
        await _cacheProvider.InvalidateAsync($"user_{userId}_*");
        await _cacheProvider.InvalidateAsync("user_count");
        await _cacheProvider.InvalidateAsync("active_users");
    }
    
    public async Task InvalidateProductCacheAsync(int productId)
    {
        // Invalidate product-specific cache
        await _cacheProvider.InvalidateAsync($"product_{productId}_*");
        await _cacheProvider.InvalidateAsync("product_stats");
    }
    
    public async Task InvalidateAllCacheAsync()
    {
        // Invalidate all cache (use sparingly)
        await _cacheProvider.InvalidateAsync("*");
    }
    
    public async Task InvalidatePatternAsync(string pattern)
    {
        // Invalidate cache by pattern
        await _cacheProvider.InvalidateAsync(pattern);
    }
}
```

## 🗄️ Database Optimization

### Connection Pooling

```csharp
using TuskLang;
using TuskLang.Adapters;

public class OptimizedDatabaseService
{
    private readonly TuskLang _parser;
    private readonly IDatabaseAdapter _adapter;
    
    public OptimizedDatabaseService()
    {
        _parser = new TuskLang();
        
        // Optimized PostgreSQL adapter with connection pooling
        _adapter = new PostgreSQLAdapter(new PostgreSQLConfig
        {
            Host = "localhost",
            Port = 5432,
            Database = "myapp",
            User = "postgres",
            Password = "secret",
            SslMode = "require"
        }, new PoolConfig
        {
            MaxOpenConns = 50,           // Maximum open connections
            MaxIdleConns = 20,           // Maximum idle connections
            ConnMaxLifetime = 300000,    // Connection max lifetime (5 minutes)
            ConnMaxIdleTime = 60000,     // Connection max idle time (1 minute)
            MaxRetries = 3,              // Maximum retry attempts
            RetryDelay = 1000            // Retry delay in milliseconds
        });
        
        _parser.SetDatabaseAdapter(_adapter);
    }
    
    public async Task<Dictionary<string, object>> GetOptimizedConfigurationAsync(string filePath)
    {
        // Monitor database performance
        var stats = await _adapter.GetStatsAsync();
        Console.WriteLine($"Active Connections: {stats.ActiveConnections}");
        Console.WriteLine($"Idle Connections: {stats.IdleConnections}");
        Console.WriteLine($"Total Queries: {stats.TotalQueries}");
        Console.WriteLine($"Average Query Time: {stats.AverageQueryTime:F2}ms");
        
        return _parser.ParseFile(filePath);
    }
}
```

### Query Optimization

```ini
# app.tsk - Optimized database queries
[optimized_queries]
# Use indexes effectively
user_by_email: @query("SELECT id, name, email FROM users WHERE email = ? LIMIT 1", $email)

# Use efficient aggregations
user_stats: @query("""
    SELECT 
        COUNT(*) as total_users,
        COUNT(CASE WHEN active = 1 THEN 1 END) as active_users,
        COUNT(CASE WHEN created_at > ? THEN 1 END) as new_users
    FROM users
""", @date.subtract("30d"))

# Use pagination for large datasets
recent_orders: @query("""
    SELECT id, user_id, total, created_at 
    FROM orders 
    WHERE created_at > ? 
    ORDER BY created_at DESC 
    LIMIT 50
""", @date.subtract("7d"))

# Use efficient joins
user_orders: @query("""
    SELECT u.name, COUNT(o.id) as order_count, SUM(o.total) as total_spent
    FROM users u
    INNER JOIN orders o ON u.id = o.user_id
    WHERE u.active = 1
    GROUP BY u.id, u.name
    HAVING COUNT(o.id) > 0
    ORDER BY total_spent DESC
    LIMIT 100
""")
```

### C# Query Optimization

```csharp
public class QueryOptimizationService
{
    private readonly IDatabaseAdapter _adapter;
    
    public QueryOptimizationService(IDatabaseAdapter adapter)
    {
        _adapter = adapter;
    }
    
    public async Task<Dictionary<string, object>> GetOptimizedUserStatsAsync()
    {
        // Use prepared statements for repeated queries
        var preparedQuery = await _adapter.PrepareAsync(@"
            SELECT 
                COUNT(*) as total_users,
                COUNT(CASE WHEN active = 1 THEN 1 END) as active_users,
                COUNT(CASE WHEN created_at > ? THEN 1 END) as new_users
            FROM users
        ");
        
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var result = await preparedQuery.ExecuteAsync(new object[] { thirtyDaysAgo });
        
        return result.FirstOrDefault() as Dictionary<string, object> ?? new Dictionary<string, object>();
    }
    
    public async Task<List<Dictionary<string, object>>> GetPaginatedUsersAsync(int page, int pageSize)
    {
        var offset = page * pageSize;
        var query = @"
            SELECT id, name, email, created_at
            FROM users
            ORDER BY created_at DESC
            LIMIT ? OFFSET ?
        ";
        
        return await _adapter.QueryAsync(query, new object[] { pageSize, offset });
    }
    
    public async Task<Dictionary<string, object>> GetUserWithOrdersAsync(int userId)
    {
        // Use efficient single query instead of multiple queries
        var query = @"
            SELECT 
                u.id, u.name, u.email,
                COUNT(o.id) as order_count,
                SUM(o.total) as total_spent,
                MAX(o.created_at) as last_order
            FROM users u
            LEFT JOIN orders o ON u.id = o.user_id
            WHERE u.id = ?
            GROUP BY u.id, u.name, u.email
        ";
        
        var result = await _adapter.QueryAsync(query, new object[] { userId });
        return result.FirstOrDefault() as Dictionary<string, object> ?? new Dictionary<string, object>();
    }
}
```

## 🧠 Machine Learning Optimization

### Optimized ML Integration

```ini
# app.tsk - Optimized ML configuration
[ml_optimized]
# Cache ML predictions
optimal_cache_ttl: @cache("10m", @learn("cache_ttl", "5m"))
best_worker_count: @cache("5m", @learn("worker_count", 4))

# Batch ML predictions
load_predictions: @cache("1m", @predict.batch(["server_load", "memory_usage", "request_rate"], @metrics.batch(["cpu_usage", "memory_usage", "requests_per_second"])))

# Adaptive ML based on performance
ml_enabled: @if(@metrics("cpu_usage", 0) < 70, true, false)
prediction_interval: @if($ml_enabled, "30s", "5m")
```

### C# ML Optimization

```csharp
using TuskLang;
using TuskLang.MachineLearning;

public class OptimizedMLService
{
    private readonly TuskLang _parser;
    private readonly IMLProvider _mlProvider;
    private readonly ICacheProvider _cacheProvider;
    
    public OptimizedMLService()
    {
        _parser = new TuskLang();
        _mlProvider = new TuskMLProvider();
        _cacheProvider = new RedisCacheProvider(new RedisConfig
        {
            Host = "localhost",
            Port = 6379
        });
        
        _parser.SetMLProvider(_mlProvider);
        _parser.SetCacheProvider(_cacheProvider);
    }
    
    public async Task<Dictionary<string, object>> GetOptimizedMLConfigurationAsync(string filePath)
    {
        // Preload ML models
        await PreloadMLModelsAsync();
        
        // Parse with optimized ML
        return _parser.ParseFile(filePath);
    }
    
    private async Task PreloadMLModelsAsync()
    {
        var modelNames = new[] { "cache_ttl", "worker_count", "server_load", "memory_usage" };
        
        var preloadTasks = modelNames.Select(async modelName =>
        {
            var model = await _mlProvider.LoadModelAsync(modelName);
            if (model != null)
            {
                Console.WriteLine($"Loaded ML model: {modelName}");
            }
        });
        
        await Task.WhenAll(preloadTasks);
    }
    
    public async Task BatchPredictAsync(Dictionary<string, object> features)
    {
        // Batch predictions for better performance
        var predictions = await _mlProvider.BatchPredictAsync(new Dictionary<string, object[]>
        {
            ["server_load"] = new object[] { features["cpu_usage"], features["memory_usage"] },
            ["memory_usage"] = new object[] { features["memory_usage"], features["disk_usage"] },
            ["request_rate"] = new object[] { features["requests_per_second"], features["response_time"] }
        });
        
        // Cache batch predictions
        foreach (var prediction in predictions)
        {
            await _cacheProvider.SetAsync($"prediction_{prediction.Key}", prediction.Value, TimeSpan.FromMinutes(5));
        }
    }
}
```

## 🌐 HTTP and API Optimization

### Optimized HTTP Integration

```ini
# app.tsk - Optimized HTTP configuration
[http_optimized]
# Cache external API calls
weather_data: @cache("30m", @http("GET", "https://api.weatherapi.com/v1/current.json?key=${weather_api_key}&q=London"))
stock_price: @cache("1m", @http("GET", "https://api.stockapi.com/v1/quote?symbol=${stock_symbol}"))

# Use connection pooling for HTTP
api_config: @http("GET", "https://api.example.com/config", {
    "timeout": "5s",
    "connection_pool": true,
    "max_connections": 100
})

# Batch HTTP requests
batch_data: @http.batch([
    "GET https://api.example.com/users",
    "GET https://api.example.com/products",
    "GET https://api.example.com/orders"
], {
    "timeout": "10s",
    "parallel": true
})
```

### C# HTTP Optimization

```csharp
using TuskLang;
using TuskLang.Http;

public class OptimizedHttpService
{
    private readonly TuskLang _parser;
    private readonly IHttpProvider _httpProvider;
    private readonly ICacheProvider _cacheProvider;
    
    public OptimizedHttpService()
    {
        _parser = new TuskLang();
        
        // Optimized HTTP provider with connection pooling
        _httpProvider = new TuskHttpProvider(new HttpClient(), new HttpConfig
        {
            Timeout = TimeSpan.FromSeconds(10),
            MaxConnections = 100,
            ConnectionLifetime = TimeSpan.FromMinutes(5),
            RetryAttempts = 3,
            RetryDelay = TimeSpan.FromSeconds(1)
        });
        
        _cacheProvider = new RedisCacheProvider(new RedisConfig
        {
            Host = "localhost",
            Port = 6379
        });
        
        _parser.SetHttpProvider(_httpProvider);
        _parser.SetCacheProvider(_cacheProvider);
    }
    
    public async Task<Dictionary<string, object>> GetOptimizedHttpConfigurationAsync(string filePath)
    {
        // Preload frequently accessed APIs
        await PreloadAPIDataAsync();
        
        return _parser.ParseFile(filePath);
    }
    
    private async Task PreloadAPIDataAsync()
    {
        var apiEndpoints = new[]
        {
            "https://api.weatherapi.com/v1/current.json?key=${weather_api_key}&q=London",
            "https://api.stockapi.com/v1/quote?symbol=${stock_symbol}",
            "https://api.example.com/config"
        };
        
        var preloadTasks = apiEndpoints.Select(async endpoint =>
        {
            var cacheKey = $"api_{endpoint.GetHashCode()}";
            var cached = await _cacheProvider.GetAsync(cacheKey);
            
            if (cached == null)
            {
                try
                {
                    var response = await _httpProvider.GetAsync(endpoint);
                    await _cacheProvider.SetAsync(cacheKey, response, TimeSpan.FromMinutes(30));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to preload API: {endpoint}, Error: {ex.Message}");
                }
            }
        });
        
        await Task.WhenAll(preloadTasks);
    }
    
    public async Task<Dictionary<string, object>> BatchHttpRequestsAsync(string[] urls)
    {
        // Execute HTTP requests in parallel
        var tasks = urls.Select(url => _httpProvider.GetAsync(url));
        var responses = await Task.WhenAll(tasks);
        
        var result = new Dictionary<string, object>();
        for (int i = 0; i < urls.Length; i++)
        {
            result[urls[i]] = responses[i];
        }
        
        return result;
    }
}
```

## 📊 Metrics and Monitoring Optimization

### Optimized Metrics Collection

```ini
# app.tsk - Optimized metrics configuration
[metrics_optimized]
# Cache metrics to reduce collection overhead
cached_cpu: @cache("10s", @metrics("cpu_usage", 0))
cached_memory: @cache("10s", @metrics("memory_usage", 0))
cached_disk: @cache("30s", @metrics("disk_usage", 0))

# Batch metrics collection
system_metrics: @metrics.batch(["cpu_usage", "memory_usage", "disk_usage", "network_io"], 0)

# Adaptive metrics collection
metrics_interval: @if(@metrics("cpu_usage", 0) > 80, "5s", "30s")
```

### C# Metrics Optimization

```csharp
using TuskLang;
using TuskLang.Metrics;

public class OptimizedMetricsService
{
    private readonly TuskLang _parser;
    private readonly IMetricsCollector _metricsCollector;
    private readonly ICacheProvider _cacheProvider;
    private readonly Timer _metricsTimer;
    
    public OptimizedMetricsService()
    {
        _parser = new TuskLang();
        _metricsCollector = new PrometheusMetricsCollector();
        _cacheProvider = new RedisCacheProvider(new RedisConfig
        {
            Host = "localhost",
            Port = 6379
        });
        
        _parser.SetMetricsCollector(_metricsCollector);
        _parser.SetCacheProvider(_cacheProvider);
        
        // Adaptive metrics collection
        _metricsTimer = new Timer(CollectMetricsAsync, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }
    
    private async void CollectMetricsAsync(object state)
    {
        try
        {
            var metrics = await _metricsCollector.CollectAsync();
            
            // Cache metrics to reduce collection overhead
            foreach (var metric in metrics)
            {
                var cacheKey = $"metric_{metric.Key}";
                await _cacheProvider.SetAsync(cacheKey, metric.Value, TimeSpan.FromSeconds(10));
            }
            
            _parser.SetMetricsContext(metrics);
            
            // Adjust collection interval based on load
            var cpuUsage = Convert.ToDouble(metrics["cpu_usage"]);
            var interval = cpuUsage > 80 ? TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(30);
            _metricsTimer.Change(interval, interval);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Metrics collection failed: {ex.Message}");
        }
    }
    
    public async Task<Dictionary<string, object>> GetOptimizedMetricsConfigurationAsync(string filePath)
    {
        return _parser.ParseFile(filePath);
    }
    
    public async Task<Dictionary<string, object>> BatchCollectMetricsAsync(string[] metricNames)
    {
        // Collect multiple metrics in a single operation
        var metrics = await _metricsCollector.BatchCollectAsync(metricNames);
        
        // Cache batch metrics
        foreach (var metric in metrics)
        {
            var cacheKey = $"metric_{metric.Key}";
            await _cacheProvider.SetAsync(cacheKey, metric.Value, TimeSpan.FromSeconds(10));
        }
        
        return metrics;
    }
}
```

## 🔧 Memory Optimization

### Memory-Efficient Configuration

```csharp
using TuskLang;
using System.Collections.Concurrent;

public class MemoryOptimizedConfigurationService
{
    private readonly TuskLang _parser;
    private readonly ConcurrentDictionary<string, WeakReference<Dictionary<string, object>>> _configCache;
    private readonly Timer _cleanupTimer;
    
    public MemoryOptimizedConfigurationService()
    {
        _parser = new TuskLang();
        _configCache = new ConcurrentDictionary<string, WeakReference<Dictionary<string, object>>>();
        
        // Cleanup unused configurations every 5 minutes
        _cleanupTimer = new Timer(CleanupUnusedConfigs, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }
    
    public Dictionary<string, object> GetMemoryOptimizedConfiguration(string filePath)
    {
        // Check if configuration is already in memory
        if (_configCache.TryGetValue(filePath, out var weakRef) && weakRef.TryGetTarget(out var cachedConfig))
        {
            return cachedConfig;
        }
        
        // Parse configuration
        var config = _parser.ParseFile(filePath);
        
        // Store with weak reference to allow garbage collection
        _configCache[filePath] = new WeakReference<Dictionary<string, object>>(config);
        
        return config;
    }
    
    private void CleanupUnusedConfigs(object state)
    {
        var keysToRemove = new List<string>();
        
        foreach (var kvp in _configCache)
        {
            if (!kvp.Value.TryGetTarget(out _))
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            _configCache.TryRemove(key, out _);
        }
        
        Console.WriteLine($"Cleaned up {keysToRemove.Count} unused configurations");
    }
    
    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}
```

## 🧪 Performance Testing

### Comprehensive Performance Tests

```csharp
using Xunit;
using TuskLang;
using System.Diagnostics;

public class PerformanceTests
{
    [Fact]
    public async Task TestConfigurationParsePerformance()
    {
        // Arrange
        var parser = new TuskLang();
        var stopwatch = new Stopwatch();
        
        // Act
        stopwatch.Start();
        var config = parser.ParseFile("app.tsk");
        stopwatch.Stop();
        
        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 100, $"Configuration parsing took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");
    }
    
    [Fact]
    public async Task TestCachedConfigurationPerformance()
    {
        // Arrange
        var service = new OptimizedConfigurationService();
        var stopwatch = new Stopwatch();
        
        // Act - First call (cache miss)
        stopwatch.Start();
        var config1 = await service.GetOptimizedConfigurationAsync("app.tsk");
        stopwatch.Stop();
        var firstCallTime = stopwatch.ElapsedMilliseconds;
        
        // Second call (cache hit)
        stopwatch.Restart();
        var config2 = await service.GetOptimizedConfigurationAsync("app.tsk");
        stopwatch.Stop();
        var secondCallTime = stopwatch.ElapsedMilliseconds;
        
        // Assert
        Assert.True(secondCallTime < firstCallTime * 0.1, $"Cache hit should be 10x faster. First: {firstCallTime}ms, Second: {secondCallTime}ms");
    }
    
    [Fact]
    public async Task TestDatabaseQueryPerformance()
    {
        // Arrange
        var service = new OptimizedDatabaseService();
        var stopwatch = new Stopwatch();
        
        // Act
        stopwatch.Start();
        var config = await service.GetOptimizedConfigurationAsync("app.tsk");
        stopwatch.Stop();
        
        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 500, $"Database configuration took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
    }
    
    [Fact]
    public async Task TestConcurrentConfigurationAccess()
    {
        // Arrange
        var service = new OptimizedConfigurationService();
        var tasks = new List<Task<Dictionary<string, object>>>();
        var stopwatch = new Stopwatch();
        
        // Act
        stopwatch.Start();
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(service.GetOptimizedConfigurationAsync("app.tsk"));
        }
        
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Assert
        Assert.Equal(100, results.Length);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"Concurrent access took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
    }
}
```

## 📈 Performance Monitoring

### Real-Time Performance Monitoring

```csharp
public class PerformanceMonitor
{
    private readonly Dictionary<string, List<long>> _responseTimes;
    private readonly object _lock = new object();
    
    public PerformanceMonitor()
    {
        _responseTimes = new Dictionary<string, List<long>>();
    }
    
    public void RecordResponseTime(string operation, long milliseconds)
    {
        lock (_lock)
        {
            if (!_responseTimes.ContainsKey(operation))
            {
                _responseTimes[operation] = new List<long>();
            }
            
            _responseTimes[operation].Add(milliseconds);
            
            // Keep only last 1000 measurements
            if (_responseTimes[operation].Count > 1000)
            {
                _responseTimes[operation].RemoveAt(0);
            }
        }
    }
    
    public PerformanceStats GetStats(string operation)
    {
        lock (_lock)
        {
            if (!_responseTimes.ContainsKey(operation))
            {
                return new PerformanceStats();
            }
            
            var times = _responseTimes[operation];
            if (times.Count == 0)
            {
                return new PerformanceStats();
            }
            
            return new PerformanceStats
            {
                Count = times.Count,
                Average = times.Average(),
                Min = times.Min(),
                Max = times.Max(),
                P95 = CalculatePercentile(times, 95),
                P99 = CalculatePercentile(times, 99)
            };
        }
    }
    
    private double CalculatePercentile(List<long> values, int percentile)
    {
        var sorted = values.OrderBy(x => x).ToList();
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        return sorted[index];
    }
}

public class PerformanceStats
{
    public int Count { get; set; }
    public double Average { get; set; }
    public long Min { get; set; }
    public long Max { get; set; }
    public double P95 { get; set; }
    public double P99 { get; set; }
}
```

## 🎯 Best Practices

### 1. Cache Strategically

```ini
# Good: Cache expensive operations
[optimized]
expensive_query: @cache("5m", @query("SELECT COUNT(*) FROM large_table"))
api_response: @cache("30s", @http("GET", "https://api.example.com/data"))
ml_prediction: @cache("1m", @predict("server_load", @metrics("cpu_usage", 75)))

# Good: Use adaptive caching
adaptive_cache: @cache(@if(@metrics("cpu_usage", 0) > 80, "30s", "5m"), @query("SELECT * FROM heavy_table"))
```

### 2. Optimize Database Queries

```ini
# Good: Use efficient queries
[optimized_queries]
user_count: @query("SELECT COUNT(*) FROM users WHERE active = 1")
user_stats: @query("""
    SELECT 
        COUNT(*) as total_users,
        COUNT(CASE WHEN active = 1 THEN 1 END) as active_users
    FROM users
""")
```

### 3. Monitor Performance

```csharp
// Good: Monitor performance metrics
public async Task<Dictionary<string, object>> GetMonitoredConfigurationAsync(string filePath)
{
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        var config = await GetOptimizedConfigurationAsync(filePath);
        stopwatch.Stop();
        
        _performanceMonitor.RecordResponseTime("configuration_parse", stopwatch.ElapsedMilliseconds);
        
        return config;
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        _performanceMonitor.RecordResponseTime("configuration_error", stopwatch.ElapsedMilliseconds);
        throw;
    }
}
```

### 4. Use Connection Pooling

```csharp
// Good: Use connection pooling
var adapter = new PostgreSQLAdapter(new PostgreSQLConfig
{
    Host = "localhost",
    Database = "myapp"
}, new PoolConfig
{
    MaxOpenConns = 50,
    MaxIdleConns = 20,
    ConnMaxLifetime = 300000
});
```

## 🎉 You're Ready!

You've mastered performance optimization with TuskLang! You can now:

- ✅ **Implement intelligent caching** - Multi-layer caching strategies
- ✅ **Optimize database queries** - Connection pooling and efficient queries
- ✅ **Optimize ML integration** - Batch predictions and model caching
- ✅ **Optimize HTTP requests** - Connection pooling and request batching
- ✅ **Optimize metrics collection** - Adaptive collection and caching
- ✅ **Optimize memory usage** - Weak references and cleanup strategies
- ✅ **Monitor performance** - Real-time performance tracking
- ✅ **Test performance** - Comprehensive performance testing

## 🔥 What's Next?

Ready to deploy to production? Explore:

1. **[Production Deployment](008-production-csharp.md)** - Deploy to production environments
2. **[Best Practices](009-best-practices-csharp.md)** - Production best practices and patterns
3. **[Troubleshooting](010-troubleshooting-csharp.md)** - Common issues and solutions

---

**"We don't bow to any king" - Your performance, your optimization, your speed.**

Make your configuration lightning fast! ⚡ 