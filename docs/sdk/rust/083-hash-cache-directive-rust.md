# 🦀 #cache - Caching Strategies Directive - Rust Edition

**"We don't bow to any king" - Rust Edition**

The `#cache` directive in Rust creates high-performance caching strategies with zero-copy operations, async support, and seamless integration with Rust's caching ecosystem.

## Basic Syntax

```rust
use tusklang_rust::{parse, CacheDirective, CacheManager};
use redis::AsyncCommands;
use std::time::Duration;

// Simple cache directive
let cache_config = r#"
#cache user_data {
    handler: "UserCacheHandler::get"
    strategy: "redis"
    ttl: "5m"
    key: "user:{@user.id}"
    async: true
}
"#;

// Cache with fallback
let cache_fallback = r#"
#cache expensive_operation {
    handler: "ExpensiveOperationHandler::execute"
    strategy: "redis"
    ttl: "1h"
    key: "expensive:{@params.input}"
    fallback: "database"
    async: true
}
"#;

// Cache with invalidation
let cache_invalidation = r#"
#cache user_profile {
    handler: "UserProfileHandler::get"
    strategy: "redis"
    ttl: "10m"
    key: "profile:{@user.id}"
    invalidate_on: ["user_update", "profile_change"]
    async: true
}
"#;
```

## Cache Strategies with Rust

```rust
use tusklang_rust::{CacheDirective, CacheStrategy, CacheStore};

// Redis cache strategy
let redis_cache = r#"
#cache redis_cache {
    handler: "RedisCacheHandler::get"
    strategy: "redis"
    
    config: {
        connection: "default"
        ttl: "5m"
        key_prefix: "app:"
        
        serialization: {
            format: "json"
            compression: "gzip"
        }
        
        clustering: {
            enabled: true
            nodes: ["redis://localhost:6379", "redis://localhost:6380"]
            strategy: "consistent_hashing"
        }
    }
    
    key_generation: {
        pattern: "cache:{@request.path}:{@request.query.hash}"
        hash_function: "sha256"
    }
    
    on_miss: {
        source: "database"
        store_result: true
        return_data: true
    }
}
"#;

// Memory cache strategy
let memory_cache = r#"
#cache memory_cache {
    handler: "MemoryCacheHandler::get"
    strategy: "memory"
    
    config: {
        max_size: "100MB"
        max_entries: 10000
        eviction_policy: "lru"
        
        ttl: "10m"
        cleanup_interval: "1m"
    }
    
    key_generation: {
        pattern: "mem:{@function_name}:{@args.hash}"
        hash_function: "xxhash"
    }
    
    on_eviction: {
        log: true
        metrics: true
    }
}
"#;

// Multi-level cache strategy
let multi_level_cache = r#"
#cache multi_level {
    handler: "MultiLevelCacheHandler::get"
    strategy: "multi_level"
    
    levels: {
        l1: {
            type: "memory"
            ttl: "1m"
            max_size: "10MB"
        }
        
        l2: {
            type: "redis"
            ttl: "5m"
            connection: "local"
        }
        
        l3: {
            type: "redis"
            ttl: "1h"
            connection: "cluster"
        }
    }
    
    routing: {
        hot_data: ["l1", "l2"]
        warm_data: ["l2", "l3"]
        cold_data: ["l3"]
    }
    
    consistency: {
        write_through: true
        write_behind: false
        read_through: true
    }
}
"#;
```

## Cache Key Generation with Rust

```rust
use tusklang_rust::{CacheDirective, KeyGenerator, HashFunction};
use std::collections::hash_map::DefaultHasher;
use std::hash::{Hash, Hasher};

// Dynamic key generation
let dynamic_keys = r#"
#cache dynamic_cache {
    handler: "DynamicCacheHandler::get"
    strategy: "redis"
    
    key_generation: {
        pattern: "data:{@resource_type}:{@resource_id}:{@version}"
        hash_function: "sha256"
        normalize: true
        
        variables: {
            resource_type: "@request.path.segments[1]"
            resource_id: "@request.path.segments[2]"
            version: "@request.headers.api_version"
        }
    }
    
    ttl: "15m"
    async: true
}
"#;

// Composite key generation
let composite_keys = r#"
#cache composite_cache {
    handler: "CompositeCacheHandler::get"
    strategy: "redis"
    
    key_generation: {
        pattern: "composite:{@user.id}:{@permissions.hash}:{@filters.hash}"
        
        components: {
            user_id: "@user.id"
            permissions: "@user.permissions"
            filters: "@request.query.filters"
        }
        
        hash_functions: {
            permissions: "sha1"
            filters: "md5"
        }
    }
    
    ttl: "30m"
    async: true
}
"#;
```

## Cache Invalidation Strategies

```rust
use tusklang_rust::{CacheDirective, CacheInvalidation, InvalidationStrategy};

// Time-based invalidation
let time_based_invalidation = r#"
#cache time_based {
    handler: "TimeBasedCacheHandler::get"
    strategy: "redis"
    
    ttl: "1h"
    key: "time_based:{@data.id}"
    
    invalidation: {
        strategy: "ttl"
        max_age: "1h"
        stale_while_revalidate: "5m"
    }
    
    async: true
}
"#;

// Event-based invalidation
let event_based_invalidation = r#"
#cache event_based {
    handler: "EventBasedCacheHandler::get"
    strategy: "redis"
    
    ttl: "24h"
    key: "event_based:{@data.id}"
    
    invalidation: {
        strategy: "events"
        events: ["data_updated", "data_deleted", "user_changed"]
        
        patterns: {
            data_updated: "event_based:{@event.data_id}"
            data_deleted: "event_based:{@event.data_id}"
            user_changed: "event_based:*:{@event.user_id}:*"
        }
    }
    
    async: true
}
"#;

// Version-based invalidation
let version_based_invalidation = r#"
#cache version_based {
    handler: "VersionBasedCacheHandler::get"
    strategy: "redis"
    
    key: "version:{@data.id}:{@data.version}"
    
    invalidation: {
        strategy: "version"
        version_key: "data_version:{@data.id}"
        check_interval: "1m"
    }
    
    ttl: "1h"
    async: true
}
"#;
```

## Cache Performance Optimization

```rust
use tusklang_rust::{CacheDirective, CacheOptimizer, PerformanceMonitor};

// Optimized cache with compression
let optimized_cache = r#"
#cache optimized {
    handler: "OptimizedCacheHandler::get"
    strategy: "redis"
    
    optimization: {
        compression: {
            enabled: true
            algorithm: "lz4"
            threshold: "1KB"
        }
        
        serialization: {
            format: "bincode"
            optimize_size: true
        }
        
        batching: {
            enabled: true
            batch_size: 100
            timeout: "10ms"
        }
    }
    
    ttl: "5m"
    key: "optimized:{@data.id}"
    async: true
}
"#;

// Cache with metrics
let metrics_cache = r#"
#cache metrics_cache {
    handler: "MetricsCacheHandler::get"
    strategy: "redis"
    
    metrics: {
        enabled: true
        
        counters: {
            hits: "cache_hits_total"
            misses: "cache_misses_total"
            evictions: "cache_evictions_total"
        }
        
        histograms: {
            response_time: "cache_response_time_seconds"
            size: "cache_entry_size_bytes"
        }
        
        gauges: {
            memory_usage: "cache_memory_usage_bytes"
            entry_count: "cache_entry_count"
        }
    }
    
    ttl: "10m"
    key: "metrics:{@data.id}"
    async: true
}
"#;
```

## Cache Consistency Patterns

```rust
use tusklang_rust::{CacheDirective, CacheConsistency, ConsistencyPattern};

// Write-through cache
let write_through_cache = r#"
#cache write_through {
    handler: "WriteThroughCacheHandler::get"
    strategy: "redis"
    
    consistency: {
        pattern: "write_through"
        
        write_strategy: {
            update_cache: true
            update_source: true
            atomic: true
        }
        
        read_strategy: {
            read_cache_first: true
            fallback_to_source: true
            update_cache_on_miss: true
        }
    }
    
    ttl: "5m"
    key: "write_through:{@data.id}"
    async: true
}
"#;

// Write-behind cache
let write_behind_cache = r#"
#cache write_behind {
    handler: "WriteBehindCacheHandler::get"
    strategy: "redis"
    
    consistency: {
        pattern: "write_behind"
        
        write_strategy: {
            update_cache: true
            queue_for_source: true
            batch_size: 100
            flush_interval: "1s"
        }
        
        read_strategy: {
            read_cache_only: true
            no_source_fallback: true
        }
    }
    
    ttl: "10m"
    key: "write_behind:{@data.id}"
    async: true
}
"#;

// Cache-aside pattern
let cache_aside = r#"
#cache cache_aside {
    handler: "CacheAsideHandler::get"
    strategy: "redis"
    
    consistency: {
        pattern: "cache_aside"
        
        read_strategy: {
            check_cache_first: true
            load_from_source: true
            store_in_cache: true
        }
        
        write_strategy: {
            update_source: true
            invalidate_cache: true
        }
    }
    
    ttl: "15m"
    key: "cache_aside:{@data.id}"
    async: true
}
"#;
```

## Cache Warming and Preloading

```rust
use tusklang_rust::{CacheDirective, CacheWarming, PreloadStrategy};

// Cache warming directive
let cache_warming = r#"
#cache warming {
    handler: "CacheWarmingHandler::warm"
    strategy: "redis"
    
    warming: {
        enabled: true
        schedule: "0 */6 * * *"  # Every 6 hours
        
        data_sources: {
            hot_users: {
                query: "SELECT id FROM users WHERE last_active > NOW() - INTERVAL '1 day'"
                limit: 1000
                priority: "high"
            }
            
            popular_content: {
                query: "SELECT id FROM posts WHERE views > 100 ORDER BY views DESC"
                limit: 500
                priority: "medium"
            }
        }
        
        parallel_loading: true
        max_concurrent: 10
    }
    
    ttl: "1h"
    key_pattern: "warmed:{@data.id}"
    async: true
}
"#;

// Predictive cache loading
let predictive_cache = r#"
#cache predictive {
    handler: "PredictiveCacheHandler::get"
    strategy: "redis"
    
    prediction: {
        enabled: true
        algorithm: "markov_chain"
        
        patterns: {
            user_behavior: {
                track_sequence: true
                predict_next: 3
                confidence_threshold: 0.7
            }
            
            time_patterns: {
                track_hourly: true
                track_daily: true
                track_weekly: true
            }
        }
        
        preload: {
            enabled: true
            max_items: 50
            ttl: "30m"
        }
    }
    
    ttl: "5m"
    key: "predictive:{@user.id}:{@predicted_item}"
    async: true
}
"#;
```

## Cache Error Handling

```rust
use tusklang_rust::{CacheDirective, CacheError, ErrorHandler};
use thiserror::Error;

#[derive(Error, Debug)]
pub enum CacheError {
    #[error("Cache connection failed: {0}")]
    ConnectionError(String),
    
    #[error("Cache key not found: {0}")]
    KeyNotFoundError(String),
    
    #[error("Cache serialization failed: {0}")]
    SerializationError(String),
    
    #[error("Cache timeout: {0}")]
    TimeoutError(String),
}

// Cache with error handling
let error_handling_cache = r#"
#cache error_handling {
    handler: "ErrorHandlingCacheHandler::get"
    strategy: "redis"
    
    error_handling: {
        connection_errors: {
            retry: 3
            backoff: "exponential"
            fallback: "memory"
        }
        
        key_not_found: {
            load_from_source: true
            store_in_cache: true
            log: true
        }
        
        serialization_errors: {
            fallback_format: "json"
            log: true
            metrics: true
        }
        
        timeout_errors: {
            reduce_ttl: true
            use_stale_data: true
            log: true
        }
    }
    
    ttl: "10m"
    key: "error_handling:{@data.id}"
    async: true
}
"#;
```

## Integration with Rust Cache Libraries

```rust
use redis::AsyncCommands;
use moka::future::Cache as MokaCache;
use tusklang_rust::{CacheDirective, CacheIntegration};

// Redis integration
async fn redis_integration() -> Result<(), Box<dyn std::error::Error>> {
    let cache_directive = r#"
#cache redis_integration {
    handler: "RedisCacheHandler::get"
    strategy: "redis"
    connection: "default"
    ttl: "5m"
    key: "redis:{@data.id}"
    async: true
}
"#;
    
    let redis_client = redis::Client::open("redis://localhost:6379")?;
    let mut conn = redis_client.get_async_connection().await?;
    
    // Cache operations
    conn.set("key", "value").await?;
    let value: String = conn.get("key").await?;
    
    Ok(())
}

// Moka cache integration
async fn moka_integration() -> Result<(), Box<dyn std::error::Error>> {
    let cache_directive = r#"
#cache moka_integration {
    handler: "MokaCacheHandler::get"
    strategy: "memory"
    max_size: "100MB"
    ttl: "10m"
    key: "moka:{@data.id}"
    async: true
}
"#;
    
    let cache = MokaCache::builder()
        .max_capacity(1000)
        .time_to_live(Duration::from_secs(600))
        .build();
    
    cache.insert("key", "value").await;
    let value = cache.get("key").await;
    
    Ok(())
}
```

## Testing Cache Directives with Rust

```rust
use tusklang_rust::{CacheDirectiveTester, TestCache, TestData};
use tokio::test;

#[tokio::test]
async fn test_cache_directive() {
    let cache_directive = r#"
#cache test_cache {
    handler: "TestCacheHandler::get"
    strategy: "memory"
    ttl: "1m"
    key: "test:{@data.id}"
    async: true
}
"#;
    
    let tester = CacheDirectiveTester::new();
    let test_data = TestData::new("test_id", "test_value");
    
    let result = tester
        .test_cache_directive(cache_directive, &test_data)
        .execute()
        .await?;
    
    assert_eq!(result.status, "cached");
    assert_eq!(result.value, "test_value");
}

#[tokio::test]
async fn test_cache_miss() {
    let cache_directive = r#"
#cache test_cache_miss {
    handler: "TestCacheHandler::get"
    strategy: "memory"
    ttl: "1m"
    key: "test:{@data.id}"
    fallback: "source"
    async: true
}
"#;
    
    let tester = CacheDirectiveTester::new();
    let test_data = TestData::new("missing_id", "source_value");
    
    let result = tester
        .test_cache_directive(cache_directive, &test_data)
        .execute()
        .await?;
    
    assert_eq!(result.status, "miss");
    assert_eq!(result.value, "source_value");
}
```

## Performance Optimization with Rust

```rust
use tusklang_rust::{CacheDirective, PerformanceOptimizer};
use std::sync::Arc;
use tokio::sync::RwLock;

// Zero-copy cache processing
fn process_cache_zero_copy<'a>(directive: &'a str) -> CacheDirectiveResult<CacheContext<'a>> {
    let context = CacheContext::from_str(directive)?;
    Ok(context)
}

// Async cache processing with Rust futures
async fn process_cache_async(directive: &CacheDirective) -> CacheDirectiveResult<()> {
    let handler = find_handler_async(&directive.handler).await?;
    handler.execute_async().await?;
    Ok(())
}

// Cache performance monitoring
let performance_cache = r#"
#cache performance {
    handler: "PerformanceCacheHandler::get"
    strategy: "redis"
    
    performance: {
        monitoring: true
        metrics: {
            hit_rate: "gauge"
            response_time: "histogram"
            memory_usage: "gauge"
        }
        
        optimization: {
            compression: true
            serialization: "bincode"
            connection_pooling: true
        }
    }
    
    ttl: "5m"
    key: "performance:{@data.id}"
    async: true
}
"#;
```

## Security Best Practices with Rust

```rust
use tusklang_rust::{CacheDirective, SecurityValidator};
use std::collections::HashSet;

// Security validation for cache directives
struct CacheSecurityValidator {
    allowed_strategies: HashSet<String>,
    allowed_handlers: HashSet<String>,
    max_ttl: Duration,
    restricted_keys: HashSet<String>,
}

impl CacheSecurityValidator {
    fn validate_cache_directive(&self, directive: &CacheDirective) -> CacheDirectiveResult<()> {
        // Validate strategy
        if !self.allowed_strategies.contains(&directive.strategy) {
            return Err(CacheError::SecurityError(
                format!("Strategy not allowed: {}", directive.strategy)
            ));
        }
        
        // Validate handler
        if !self.allowed_handlers.contains(&directive.handler) {
            return Err(CacheError::SecurityError(
                format!("Handler not allowed: {}", directive.handler)
            ));
        }
        
        // Validate TTL
        if directive.ttl > self.max_ttl {
            return Err(CacheError::SecurityError(
                format!("TTL too long: {:?}", directive.ttl)
            ));
        }
        
        Ok(())
    }
}
```

## Best Practices for Rust Cache Directives

```rust
// 1. Use strong typing for cache configurations
#[derive(Debug, Deserialize, Serialize)]
struct CacheDirectiveConfig {
    strategy: String,
    handler: String,
    ttl: Duration,
    key: String,
    async: bool,
    optimization: Option<OptimizationConfig>,
}

// 2. Implement proper error handling
fn process_cache_directive_safe(directive: &str) -> Result<CacheDirective, Box<dyn std::error::Error>> {
    let parsed = parse(directive)?;
    
    // Validate directive
    let validator = CacheSecurityValidator::new();
    validator.validate_cache_directive(&parsed)?;
    
    Ok(parsed)
}

// 3. Use async/await for I/O operations
async fn execute_cache_directive_async(directive: &CacheDirective) -> CacheDirectiveResult<()> {
    let handler = find_handler_async(&directive.handler).await?;
    handler.execute_async().await?;
    Ok(())
}

// 4. Implement proper logging and monitoring
use tracing::{info, warn, error};

fn log_cache_execution(directive: &CacheDirective, result: &CacheDirectiveResult<()>) {
    match result {
        Ok(_) => info!("Cache directive executed successfully: {}", directive.strategy),
        Err(e) => error!("Cache directive execution failed: {} - {}", directive.strategy, e),
    }
}
```

## Next Steps

Now that you understand the `#cache` directive in Rust, explore other directive types:

- **[#rate-limit Directive](./084-hash-rate-limit-directive-rust.md)** - Rate limiting and throttling
- **[#custom Directives](./085-hash-custom-directives-rust.md)** - Building your own directives

**Ready to build high-performance caching systems with Rust and TuskLang? Let's continue with the next directive!** 