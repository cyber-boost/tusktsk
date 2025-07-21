using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Diagnostics;

namespace TuskLang
{
    /// <summary>
    /// Advanced performance optimization and caching system for TuskLang C# SDK
    /// Implements multi-level caching, performance monitoring, and optimization strategies
    /// </summary>
    public class PerformanceOptimization
    {
        private readonly MultiLevelCache _cache;
        private readonly PerformanceMonitor _monitor;
        private readonly OptimizationEngine _optimizer;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks;

        public PerformanceOptimization()
        {
            _cache = new MultiLevelCache();
            _monitor = new PerformanceMonitor();
            _optimizer = new OptimizationEngine();
            _locks = new ConcurrentDictionary<string, SemaphoreSlim>();
        }

        /// <summary>
        /// Execute operation with performance optimization
        /// </summary>
        public async Task<T> ExecuteWithOptimization<T>(
            Func<Task<T>> operation,
            string operationName = null,
            Dictionary<string, object> context = null)
        {
            var opName = operationName ?? operation.Method.Name;
            var cacheKey = GenerateCacheKey(opName, context);
            
            // Check cache first
            if (_cache.TryGet<T>(cacheKey, out var cachedResult))
            {
                _monitor.RecordCacheHit(opName);
                return cachedResult;
            }

            // Execute with performance monitoring
            using (var timer = _monitor.StartTimer(opName))
            {
                var result = await operation();
                
                // Cache the result
                _cache.Set(cacheKey, result, GetCacheExpiration(context));
                
                // Record performance metrics
                timer.Stop();
                _monitor.RecordExecution(opName, timer.ElapsedMilliseconds);
                
                return result;
            }
        }

        /// <summary>
        /// Execute with connection pooling and resource optimization
        /// </summary>
        public async Task<T> ExecuteWithResourceOptimization<T>(
            Func<Task<T>> operation,
            string resourceType = null,
            Dictionary<string, object> context = null)
        {
            var resourceKey = resourceType ?? "default";
            var semaphore = _locks.GetOrAdd(resourceKey, _ => new SemaphoreSlim(10, 10));

            await semaphore.WaitAsync();
            try
            {
                return await ExecuteWithOptimization(operation, resourceKey, context);
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Batch multiple operations for optimal performance
        /// </summary>
        public async Task<List<T>> ExecuteBatch<T>(
            List<Func<Task<T>>> operations,
            int maxConcurrency = 5)
        {
            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var tasks = new List<Task<T>>();

            foreach (var operation in operations)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        return await ExecuteWithOptimization(operation);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            return (await Task.WhenAll(tasks)).ToList();
        }

        /// <summary>
        /// Get performance metrics
        /// </summary>
        public PerformanceMetrics GetMetrics()
        {
            return _monitor.GetMetrics();
        }

        /// <summary>
        /// Get optimization recommendations
        /// </summary>
        public List<OptimizationRecommendation> GetRecommendations()
        {
            return _optimizer.GenerateRecommendations(_monitor.GetMetrics());
        }

        /// <summary>
        /// Clear cache
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
        }

        private string GenerateCacheKey(string operationName, Dictionary<string, object> context)
        {
            if (context == null || context.Count == 0)
                return operationName;

            var contextHash = string.Join("|", 
                context.OrderBy(kvp => kvp.Key)
                       .Select(kvp => $"{kvp.Key}:{kvp.Value}"));
            
            return $"{operationName}|{contextHash}";
        }

        private TimeSpan GetCacheExpiration(Dictionary<string, object> context)
        {
            if (context?.ContainsKey("cache_ttl") == true && 
                context["cache_ttl"] is TimeSpan ttl)
            {
                return ttl;
            }
            return TimeSpan.FromMinutes(5); // Default TTL
        }
    }

    /// <summary>
    /// Multi-level cache implementation
    /// </summary>
    public class MultiLevelCache
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _l1Cache; // Memory cache
        private readonly ConcurrentDictionary<string, CacheEntry> _l2Cache; // Extended memory cache
        private readonly int _l1MaxSize = 1000;
        private readonly int _l2MaxSize = 5000;

        public MultiLevelCache()
        {
            _l1Cache = new ConcurrentDictionary<string, CacheEntry>();
            _l2Cache = new ConcurrentDictionary<string, CacheEntry>();
        }

        public bool TryGet<T>(string key, out T value)
        {
            value = default(T);

            // Check L1 cache first
            if (_l1Cache.TryGetValue(key, out var l1Entry) && !l1Entry.IsExpired)
            {
                value = (T)l1Entry.Value;
                return true;
            }

            // Check L2 cache
            if (_l2Cache.TryGetValue(key, out var l2Entry) && !l2Entry.IsExpired)
            {
                value = (T)l2Entry.Value;
                // Promote to L1
                PromoteToL1(key, l2Entry);
                return true;
            }

            return false;
        }

        public void Set<T>(string key, T value, TimeSpan expiration)
        {
            var entry = new CacheEntry(value, DateTime.UtcNow.Add(expiration));

            // Try L1 first
            if (_l1Cache.Count < _l1MaxSize)
            {
                _l1Cache[key] = entry;
            }
            else if (_l2Cache.Count < _l2MaxSize)
            {
                _l2Cache[key] = entry;
            }
            else
            {
                // Evict from L2 and add to L1
                EvictFromL2();
                _l1Cache[key] = entry;
            }
        }

        public void Clear()
        {
            _l1Cache.Clear();
            _l2Cache.Clear();
        }

        private void PromoteToL1(string key, CacheEntry entry)
        {
            if (_l1Cache.Count >= _l1MaxSize)
            {
                // Evict from L1
                var oldestKey = _l1Cache.OrderBy(kvp => kvp.Value.CreatedAt).First().Key;
                _l1Cache.TryRemove(oldestKey, out _);
            }
            _l1Cache[key] = entry;
        }

        private void EvictFromL2()
        {
            var oldestKey = _l2Cache.OrderBy(kvp => kvp.Value.CreatedAt).First().Key;
            _l2Cache.TryRemove(oldestKey, out _);
        }
    }

    /// <summary>
    /// Performance monitoring system
    /// </summary>
    public class PerformanceMonitor
    {
        private readonly ConcurrentDictionary<string, OperationMetrics> _metrics;
        private readonly ConcurrentDictionary<string, int> _cacheHits;

        public PerformanceMonitor()
        {
            _metrics = new ConcurrentDictionary<string, OperationMetrics>();
            _cacheHits = new ConcurrentDictionary<string, int>();
        }

        public PerformanceTimer StartTimer(string operationName)
        {
            return new PerformanceTimer(operationName);
        }

        public void RecordExecution(string operationName, long elapsedMs)
        {
            var metrics = _metrics.GetOrAdd(operationName, _ => new OperationMetrics());
            metrics.RecordExecution(elapsedMs);
        }

        public void RecordCacheHit(string operationName)
        {
            _cacheHits.AddOrUpdate(operationName, 1, (key, value) => value + 1);
        }

        public PerformanceMetrics GetMetrics()
        {
            return new PerformanceMetrics
            {
                Operations = _metrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                CacheHits = _cacheHits.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }
    }

    /// <summary>
    /// Optimization engine for performance recommendations
    /// </summary>
    public class OptimizationEngine
    {
        public List<OptimizationRecommendation> GenerateRecommendations(PerformanceMetrics metrics)
        {
            var recommendations = new List<OptimizationRecommendation>();

            foreach (var operation in metrics.Operations)
            {
                var opMetrics = operation.Value;
                
                // Check for slow operations
                if (opMetrics.AverageExecutionTime > 1000) // > 1 second
                {
                    recommendations.Add(new OptimizationRecommendation
                    {
                        Type = OptimizationType.Performance,
                        Operation = operation.Key,
                        Description = $"Operation {operation.Key} is slow (avg: {opMetrics.AverageExecutionTime}ms)",
                        Priority = RecommendationPriority.High
                    });
                }

                // Check for low cache hit rates
                var cacheHits = metrics.CacheHits.GetValueOrDefault(operation.Key, 0);
                var totalExecutions = opMetrics.TotalExecutions;
                var hitRate = totalExecutions > 0 ? (double)cacheHits / totalExecutions : 0;

                if (hitRate < 0.3 && totalExecutions > 10)
                {
                    recommendations.Add(new OptimizationRecommendation
                    {
                        Type = OptimizationType.Caching,
                        Operation = operation.Key,
                        Description = $"Low cache hit rate for {operation.Key} ({hitRate:P})",
                        Priority = RecommendationPriority.Medium
                    });
                }
            }

            return recommendations;
        }
    }

    public class CacheEntry
    {
        public object Value { get; }
        public DateTime ExpiresAt { get; }
        public DateTime CreatedAt { get; }

        public bool IsExpired => DateTime.UtcNow > ExpiresAt;

        public CacheEntry(object value, DateTime expiresAt)
        {
            Value = value;
            ExpiresAt = expiresAt;
            CreatedAt = DateTime.UtcNow;
        }
    }

    public class PerformanceTimer : IDisposable
    {
        private readonly Stopwatch _stopwatch;
        private readonly string _operationName;

        public PerformanceTimer(string operationName)
        {
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
        }

        public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

        public void Stop()
        {
            _stopwatch.Stop();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
        }
    }

    public class OperationMetrics
    {
        private readonly List<long> _executionTimes = new List<long>();
        private readonly object _lock = new object();

        public int TotalExecutions => _executionTimes.Count;
        public double AverageExecutionTime => _executionTimes.Count > 0 ? _executionTimes.Average() : 0;
        public long MinExecutionTime => _executionTimes.Count > 0 ? _executionTimes.Min() : 0;
        public long MaxExecutionTime => _executionTimes.Count > 0 ? _executionTimes.Max() : 0;

        public void RecordExecution(long elapsedMs)
        {
            lock (_lock)
            {
                _executionTimes.Add(elapsedMs);
                if (_executionTimes.Count > 1000) // Keep only last 1000 measurements
                {
                    _executionTimes.RemoveAt(0);
                }
            }
        }
    }

    public class PerformanceMetrics
    {
        public Dictionary<string, OperationMetrics> Operations { get; set; }
        public Dictionary<string, int> CacheHits { get; set; }
    }

    public class OptimizationRecommendation
    {
        public OptimizationType Type { get; set; }
        public string Operation { get; set; }
        public string Description { get; set; }
        public RecommendationPriority Priority { get; set; }
    }

    public enum OptimizationType
    {
        Performance,
        Caching,
        Memory,
        Concurrency
    }

    public enum RecommendationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
} 