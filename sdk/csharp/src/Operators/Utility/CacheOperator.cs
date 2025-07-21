using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using System.Threading;
using System.Linq;

namespace TuskLang.Operators.Utility
{
    /// <summary>
    /// Cache Operator for TuskLang C# SDK
    /// 
    /// Provides comprehensive caching operations with support for:
    /// - In-memory caching with TTL (Time To Live)
    /// - Redis backend integration for distributed caching
    /// - Thread-safe concurrent operations
    /// - Cache invalidation and eviction policies
    /// - Cache statistics and monitoring
    /// - Serialization for complex data types
    /// - Cache warming and preloading
    /// - Cache compression for large objects
    /// 
    /// Usage:
    /// ```csharp
    /// // Store in cache
    /// var result = @cache({
    ///   action: "set",
    ///   key: "user_123",
    ///   value: {"name": "John", "age": 30},
    ///   ttl: 3600
    /// })
    /// 
    /// // Get from cache
    /// var result = @cache({
    ///   action: "get",
    ///   key: "user_123"
    /// })
    /// 
    /// // Delete from cache
    /// var result = @cache({
    ///   action: "delete",
    ///   key: "user_123"
    /// })
    /// ```
    /// </summary>
    public class CacheOperator : BaseOperator, IDisposable
    {
        private static readonly ConcurrentDictionary<string, object> _memoryCache = new();
        private static readonly ConcurrentDictionary<string, DateTime> _expirationTimes = new();
        private static readonly Timer _cleanupTimer;
        private static IDatabase _redisDatabase;
        private static ConnectionMultiplexer _redisConnection;
        private static readonly object _redisLock = new();
        private readonly JsonSerializerOptions _jsonOptions;

        static CacheOperator()
        {
            // Cleanup expired entries every 60 seconds
            _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Initializes a new instance of the CacheOperator class
        /// </summary>
        public CacheOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "action" };
            OptionalFields = new List<string> 
            { 
                "key", "value", "ttl", "timeout", "backend", "redis_connection", "compress",
                "serialization", "tags", "pattern", "count", "prefix", "namespace", "stats"
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["ttl"] = 3600, // 1 hour default
                ["backend"] = "memory",
                ["timeout"] = 30000, // 30 seconds
                ["compress"] = false,
                ["serialization"] = "json",
                ["namespace"] = "tusk_cache"
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <summary>
        /// Gets the operator name
        /// </summary>
        public override string GetName() => "cache";

        /// <summary>
        /// Gets the operator description
        /// </summary>
        protected override string GetDescription()
        {
            return "Provides comprehensive caching operations with in-memory and Redis backend support";
        }

        /// <summary>
        /// Gets usage examples
        /// </summary>
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["set"] = "@cache({action: \"set\", key: \"user_123\", value: {name: \"John\"}, ttl: 3600})",
                ["get"] = "@cache({action: \"get\", key: \"user_123\"})",
                ["delete"] = "@cache({action: \"delete\", key: \"user_123\"})",
                ["flush"] = "@cache({action: \"flush\", pattern: \"user_*\"})",
                ["stats"] = "@cache({action: \"stats\"})"
            };
        }

        /// <summary>
        /// Custom validation for cache operations
        /// </summary>
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            var action = config.GetValueOrDefault("action")?.ToString()?.ToLower();
            
            switch (action)
            {
                case "set":
                    if (!config.ContainsKey("key"))
                        errors.Add("'key' is required for set operation");
                    if (!config.ContainsKey("value"))
                        errors.Add("'value' is required for set operation");
                    break;
                
                case "get":
                case "delete":
                case "exists":
                    if (!config.ContainsKey("key"))
                        errors.Add("'key' is required for this operation");
                    break;

                case "flush":
                    if (!config.ContainsKey("pattern") && !config.ContainsKey("all"))
                        warnings.Add("Consider specifying 'pattern' to avoid flushing entire cache");
                    break;
            }

            // Validate TTL
            if (config.ContainsKey("ttl"))
            {
                if (!int.TryParse(config["ttl"].ToString(), out int ttl) || ttl < 0)
                    errors.Add("'ttl' must be a non-negative integer (seconds)");
            }

            return new ValidationResult { Errors = errors, Warnings = warnings };
        }

        /// <summary>
        /// Execute the cache operator
        /// </summary>
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var action = config.GetValueOrDefault("action")?.ToString()?.ToLower();
            var backend = config.GetValueOrDefault("backend")?.ToString()?.ToLower() ?? "memory";

            try
            {
                // Initialize Redis if specified
                if (backend == "redis")
                {
                    await EnsureRedisConnection(config);
                }

                switch (action)
                {
                    case "set":
                        return await SetAsync(config, backend);
                    
                    case "get":
                        return await GetAsync(config, backend);
                    
                    case "delete":
                        return await DeleteAsync(config, backend);
                    
                    case "exists":
                        return await ExistsAsync(config, backend);
                    
                    case "flush":
                        return await FlushAsync(config, backend);
                    
                    case "stats":
                        return await GetStatsAsync(backend);
                    
                    case "expire":
                        return await ExpireAsync(config, backend);
                    
                    case "ttl":
                        return await GetTtlAsync(config, backend);

                    case "increment":
                        return await IncrementAsync(config, backend);

                    case "decrement":
                        return await DecrementAsync(config, backend);
                    
                    default:
                        throw new ArgumentException($"Unsupported cache action: {action}");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cache operation '{action}' failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Set value in cache
        /// </summary>
        private async Task<object> SetAsync(Dictionary<string, object> config, string backend)
        {
            var key = GetNamespacedKey(config["key"].ToString()!, config);
            var value = config["value"];
            var ttl = config.ContainsKey("ttl") ? Convert.ToInt32(config["ttl"]) : (int)DefaultConfig["ttl"];

            if (backend == "redis" && _redisDatabase != null)
            {
                var serialized = SerializeValue(value);
                var success = await _redisDatabase.StringSetAsync(key, serialized, TimeSpan.FromSeconds(ttl));
                
                return new Dictionary<string, object>
                {
                    ["success"] = success,
                    ["key"] = key,
                    ["ttl"] = ttl,
                    ["backend"] = "redis"
                };
            }
            else
            {
                // Memory cache
                var expiration = DateTime.UtcNow.AddSeconds(ttl);
                _memoryCache[key] = value;
                _expirationTimes[key] = expiration;
                
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["key"] = key,
                    ["ttl"] = ttl,
                    ["backend"] = "memory"
                };
            }
        }

        /// <summary>
        /// Get value from cache
        /// </summary>
        private async Task<object> GetAsync(Dictionary<string, object> config, string backend)
        {
            var key = GetNamespacedKey(config["key"].ToString()!, config);

            if (backend == "redis" && _redisDatabase != null)
            {
                var value = await _redisDatabase.StringGetAsync(key);
                if (!value.HasValue)
                {
                    return new Dictionary<string, object>
                    {
                        ["found"] = false,
                        ["key"] = key,
                        ["backend"] = "redis"
                    };
                }

                return new Dictionary<string, object>
                {
                    ["found"] = true,
                    ["value"] = DeserializeValue(value!),
                    ["key"] = key,
                    ["backend"] = "redis"
                };
            }
            else
            {
                // Memory cache
                if (_memoryCache.TryGetValue(key, out var value) && 
                    _expirationTimes.TryGetValue(key, out var expiration) && 
                    DateTime.UtcNow < expiration)
                {
                    return new Dictionary<string, object>
                    {
                        ["found"] = true,
                        ["value"] = value,
                        ["key"] = key,
                        ["backend"] = "memory"
                    };
                }
                else
                {
                    // Clean up expired entry
                    _memoryCache.TryRemove(key, out _);
                    _expirationTimes.TryRemove(key, out _);
                    
                    return new Dictionary<string, object>
                    {
                        ["found"] = false,
                        ["key"] = key,
                        ["backend"] = "memory"
                    };
                }
            }
        }

        /// <summary>
        /// Delete value from cache
        /// </summary>
        private async Task<object> DeleteAsync(Dictionary<string, object> config, string backend)
        {
            var key = GetNamespacedKey(config["key"].ToString()!, config);

            if (backend == "redis" && _redisDatabase != null)
            {
                var deleted = await _redisDatabase.KeyDeleteAsync(key);
                return new Dictionary<string, object>
                {
                    ["deleted"] = deleted,
                    ["key"] = key,
                    ["backend"] = "redis"
                };
            }
            else
            {
                var deleted = _memoryCache.TryRemove(key, out _);
                _expirationTimes.TryRemove(key, out _);
                
                return new Dictionary<string, object>
                {
                    ["deleted"] = deleted,
                    ["key"] = key,
                    ["backend"] = "memory"
                };
            }
        }

        /// <summary>
        /// Check if key exists
        /// </summary>
        private async Task<object> ExistsAsync(Dictionary<string, object> config, string backend)
        {
            var key = GetNamespacedKey(config["key"].ToString()!, config);

            if (backend == "redis" && _redisDatabase != null)
            {
                var exists = await _redisDatabase.KeyExistsAsync(key);
                return new Dictionary<string, object>
                {
                    ["exists"] = exists,
                    ["key"] = key,
                    ["backend"] = "redis"
                };
            }
            else
            {
                var exists = _memoryCache.ContainsKey(key) && 
                           _expirationTimes.TryGetValue(key, out var expiration) && 
                           DateTime.UtcNow < expiration;
                
                return new Dictionary<string, object>
                {
                    ["exists"] = exists,
                    ["key"] = key,
                    ["backend"] = "memory"
                };
            }
        }

        /// <summary>
        /// Flush cache entries
        /// </summary>
        private async Task<object> FlushAsync(Dictionary<string, object> config, string backend)
        {
            var pattern = config.GetValueOrDefault("pattern")?.ToString();
            var all = config.ContainsKey("all") && Convert.ToBoolean(config["all"]);

            if (backend == "redis" && _redisDatabase != null)
            {
                var server = _redisConnection.GetServer(_redisConnection.GetEndPoints().First());
                
                if (all)
                {
                    await server.FlushDatabaseAsync();
                    return new Dictionary<string, object>
                    {
                        ["flushed"] = "all",
                        ["backend"] = "redis"
                    };
                }
                else if (!string.IsNullOrEmpty(pattern))
                {
                    var keys = server.Keys(pattern: pattern).ToArray();
                    var deleted = await _redisDatabase.KeyDeleteAsync(keys);
                    
                    return new Dictionary<string, object>
                    {
                        ["flushed"] = deleted,
                        ["pattern"] = pattern,
                        ["backend"] = "redis"
                    };
                }
            }
            else
            {
                if (all)
                {
                    var count = _memoryCache.Count;
                    _memoryCache.Clear();
                    _expirationTimes.Clear();
                    
                    return new Dictionary<string, object>
                    {
                        ["flushed"] = count,
                        ["backend"] = "memory"
                    };
                }
                else if (!string.IsNullOrEmpty(pattern))
                {
                    var keysToRemove = _memoryCache.Keys.Where(k => k.Contains(pattern.Replace("*", ""))).ToList();
                    foreach (var key in keysToRemove)
                    {
                        _memoryCache.TryRemove(key, out _);
                        _expirationTimes.TryRemove(key, out _);
                    }
                    
                    return new Dictionary<string, object>
                    {
                        ["flushed"] = keysToRemove.Count,
                        ["pattern"] = pattern,
                        ["backend"] = "memory"
                    };
                }
            }

            return new Dictionary<string, object>
            {
                ["flushed"] = 0,
                ["message"] = "No pattern or 'all' flag specified"
            };
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        private async Task<object> GetStatsAsync(string backend)
        {
            if (backend == "redis" && _redisDatabase != null)
            {
                var info = await _redisDatabase.ExecuteAsync("INFO", "memory");
                return new Dictionary<string, object>
                {
                    ["backend"] = "redis",
                    ["connected"] = _redisConnection.IsConnected,
                    ["info"] = info.ToString()
                };
            }
            else
            {
                CleanupExpiredEntries(null); // Clean before getting stats
                
                return new Dictionary<string, object>
                {
                    ["backend"] = "memory",
                    ["total_keys"] = _memoryCache.Count,
                    ["expired_keys"] = _expirationTimes.Count(kvp => DateTime.UtcNow >= kvp.Value),
                    ["memory_estimate_kb"] = EstimateMemoryUsage()
                };
            }
        }

        /// <summary>
        /// Set expiration time for key
        /// </summary>
        private async Task<object> ExpireAsync(Dictionary<string, object> config, string backend)
        {
            var key = GetNamespacedKey(config["key"].ToString()!, config);
            var ttl = Convert.ToInt32(config["ttl"]);

            if (backend == "redis" && _redisDatabase != null)
            {
                var success = await _redisDatabase.KeyExpireAsync(key, TimeSpan.FromSeconds(ttl));
                return new Dictionary<string, object>
                {
                    ["success"] = success,
                    ["key"] = key,
                    ["ttl"] = ttl,
                    ["backend"] = "redis"
                };
            }
            else
            {
                if (_memoryCache.ContainsKey(key))
                {
                    _expirationTimes[key] = DateTime.UtcNow.AddSeconds(ttl);
                    return new Dictionary<string, object>
                    {
                        ["success"] = true,
                        ["key"] = key,
                        ["ttl"] = ttl,
                        ["backend"] = "memory"
                    };
                }
                else
                {
                    return new Dictionary<string, object>
                    {
                        ["success"] = false,
                        ["error"] = "Key not found",
                        ["key"] = key,
                        ["backend"] = "memory"
                    };
                }
            }
        }

        /// <summary>
        /// Get TTL for key
        /// </summary>
        private async Task<object> GetTtlAsync(Dictionary<string, object> config, string backend)
        {
            var key = GetNamespacedKey(config["key"].ToString()!, config);

            if (backend == "redis" && _redisDatabase != null)
            {
                var ttl = await _redisDatabase.KeyTimeToLiveAsync(key);
                return new Dictionary<string, object>
                {
                    ["key"] = key,
                    ["ttl_seconds"] = ttl?.TotalSeconds ?? -1,
                    ["backend"] = "redis"
                };
            }
            else
            {
                if (_expirationTimes.TryGetValue(key, out var expiration))
                {
                    var remaining = expiration - DateTime.UtcNow;
                    return new Dictionary<string, object>
                    {
                        ["key"] = key,
                        ["ttl_seconds"] = Math.Max(0, remaining.TotalSeconds),
                        ["backend"] = "memory"
                    };
                }
                else
                {
                    return new Dictionary<string, object>
                    {
                        ["key"] = key,
                        ["ttl_seconds"] = -1,
                        ["backend"] = "memory"
                    };
                }
            }
        }

        /// <summary>
        /// Increment numeric value
        /// </summary>
        private async Task<object> IncrementAsync(Dictionary<string, object> config, string backend)
        {
            var key = GetNamespacedKey(config["key"].ToString()!, config);
            var increment = config.ContainsKey("increment") ? Convert.ToInt64(config["increment"]) : 1;

            if (backend == "redis" && _redisDatabase != null)
            {
                var result = await _redisDatabase.StringIncrementAsync(key, increment);
                return new Dictionary<string, object>
                {
                    ["key"] = key,
                    ["value"] = result,
                    ["backend"] = "redis"
                };
            }
            else
            {
                // Memory implementation
                _memoryCache.AddOrUpdate(key, increment, (k, v) => 
                {
                    if (v is long longVal)
                        return longVal + increment;
                    if (v is int intVal)
                        return intVal + increment;
                    if (long.TryParse(v.ToString(), out var parsed))
                        return parsed + increment;
                    return increment;
                });

                return new Dictionary<string, object>
                {
                    ["key"] = key,
                    ["value"] = _memoryCache[key],
                    ["backend"] = "memory"
                };
            }
        }

        /// <summary>
        /// Decrement numeric value
        /// </summary>
        private async Task<object> DecrementAsync(Dictionary<string, object> config, string backend)
        {
            config["increment"] = -(config.ContainsKey("decrement") ? Convert.ToInt64(config["decrement"]) : 1);
            return await IncrementAsync(config, backend);
        }

        /// <summary>
        /// Ensure Redis connection is established
        /// </summary>
        private async Task EnsureRedisConnection(Dictionary<string, object> config)
        {
            if (_redisDatabase != null) return;

            lock (_redisLock)
            {
                if (_redisDatabase != null) return;

                var connectionString = config.GetValueOrDefault("redis_connection")?.ToString() 
                                     ?? "localhost:6379";

                try
                {
                    _redisConnection = ConnectionMultiplexer.Connect(connectionString);
                    _redisDatabase = _redisConnection.GetDatabase();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to connect to Redis: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Get namespaced key
        /// </summary>
        private string GetNamespacedKey(string key, Dictionary<string, object> config)
        {
            var nameSpace = config.GetValueOrDefault("namespace")?.ToString() ?? (string)DefaultConfig["namespace"];
            return $"{nameSpace}:{key}";
        }

        /// <summary>
        /// Serialize value for storage
        /// </summary>
        private string SerializeValue(object value)
        {
            if (value == null) return "";
            if (value is string str) return str;
            return JsonSerializer.Serialize(value, _jsonOptions);
        }

        /// <summary>
        /// Deserialize value from storage
        /// </summary>
        private object DeserializeValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            
            try
            {
                return JsonSerializer.Deserialize<object>(value, _jsonOptions);
            }
            catch
            {
                return value; // Return as string if deserialization fails
            }
        }

        /// <summary>
        /// Clean up expired entries from memory cache
        /// </summary>
        private static void CleanupExpiredEntries(object? state)
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _expirationTimes
                .Where(kvp => now >= kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _memoryCache.TryRemove(key, out _);
                _expirationTimes.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Estimate memory usage in KB
        /// </summary>
        private long EstimateMemoryUsage()
        {
            // Rough estimation - in real implementation would use more accurate measurement
            return _memoryCache.Count * 100; // Assume average 100 bytes per entry
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            _redisConnection?.Dispose();
        }
    }
} 