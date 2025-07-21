# 🦀 #rate-limit - Rate Limiting and Throttling Directive - Rust Edition

**"We don't bow to any king" - Rust Edition**

The `#rate-limit` directive in Rust creates robust rate limiting and throttling systems with zero-copy operations, async support, and seamless integration with Rust's concurrency ecosystem.

## Basic Syntax

```rust
use tusklang_rust::{parse, RateLimitDirective, RateLimiter};
use std::time::{Duration, Instant};
use std::collections::HashMap;

// Simple rate limiting directive
let rate_limit_config = r#"
#rate-limit api_requests {
    handler: "ApiRateLimitHandler::check"
    strategy: "sliding_window"
    limit: 100
    window: "1h"
    key: "@request.ip"
    async: true
}
"#;

// Rate limiting with different strategies
let token_bucket = r#"
#rate-limit token_bucket {
    handler: "TokenBucketHandler::check"
    strategy: "token_bucket"
    limit: 1000
    window: "1d"
    burst: 50
    key: "@user.id"
    async: true
}
"#;

// Rate limiting with headers
let rate_limit_headers = r#"
#rate-limit with_headers {
    handler: "HeaderRateLimitHandler::check"
    strategy: "fixed_window"
    limit: 100
    window: "1h"
    key: "@request.ip"
    
    headers: {
        "X-RateLimit-Limit": "@limit"
        "X-RateLimit-Remaining": "@remaining"
        "X-RateLimit-Reset": "@reset"
    }
    
    async: true
}
"#;
```

## Rate Limiting Strategies with Rust

```rust
use tusklang_rust::{RateLimitDirective, RateLimitStrategy, Limiter};

// Sliding window rate limiting
let sliding_window = r#"
#rate-limit sliding_window {
    handler: "SlidingWindowHandler::check"
    strategy: "sliding_window"
    
    config: {
        limit: 100
        window: "1h"
        precision: "1s"
        
        storage: {
            type: "redis"
            connection: "default"
            key_prefix: "rate_limit:"
        }
        
        key_generator: "@request.ip"
    }
    
    on_exceeded: {
        status: 429
        message: "Rate limit exceeded"
        retry_after: "@reset_time"
        log: true
    }
    
    async: true
}
"#;

// Fixed window rate limiting
let fixed_window = r#"
#rate-limit fixed_window {
    handler: "FixedWindowHandler::check"
    strategy: "fixed_window"
    
    config: {
        limit: 1000
        window: "1d"
        
        storage: {
            type: "memory"
            cleanup_interval: "1h"
        }
        
        key_generator: "@user.id"
    }
    
    on_exceeded: {
        status: 429
        message: "Daily limit exceeded"
        retry_after: "@next_window"
        log: true
    }
    
    async: true
}
"#;

// Token bucket rate limiting
let token_bucket = r#"
#rate-limit token_bucket {
    handler: "TokenBucketHandler::check"
    strategy: "token_bucket"
    
    config: {
        capacity: 1000
        refill_rate: 100
        refill_time: "1h"
        
        storage: {
            type: "redis"
            connection: "default"
            key_prefix: "tokens:"
        }
        
        key_generator: "@user.id"
    }
    
    on_exceeded: {
        status: 429
        message: "Rate limit exceeded"
        retry_after: "@next_refill"
        log: true
    }
    
    async: true
}
"#;
```

## Key Generation Strategies

```rust
use tusklang_rust::{RateLimitDirective, KeyGenerator, KeyStrategy};

// IP-based rate limiting
let ip_based = r#"
#rate-limit ip_based {
    handler: "IpRateLimitHandler::check"
    strategy: "sliding_window"
    
    key_generation: {
        strategy: "ip"
        source: "@request.ip"
        normalize: true
        
        fallbacks: {
            x_forwarded_for: "@request.headers.x_forwarded_for"
            x_real_ip: "@request.headers.x_real_ip"
            x_client_ip: "@request.headers.x_client_ip"
        }
    }
    
    limit: 100
    window: "1h"
    async: true
}
"#;

// User-based rate limiting
let user_based = r#"
#rate-limit user_based {
    handler: "UserRateLimitHandler::check"
    strategy: "token_bucket"
    
    key_generation: {
        strategy: "user"
        source: "@user.id"
        
        fallbacks: {
            session_id: "@session.id"
            api_key: "@request.headers.x_api_key"
        }
    }
    
    limit: 1000
    window: "1d"
    burst: 100
    async: true
}
"#;

// Composite key generation
let composite_key = r#"
#rate-limit composite {
    handler: "CompositeRateLimitHandler::check"
    strategy: "sliding_window"
    
    key_generation: {
        strategy: "composite"
        components: {
            user: "@user.id"
            endpoint: "@request.path"
            method: "@request.method"
        }
        
        separator: ":"
        hash_function: "sha256"
    }
    
    limit: 50
    window: "1h"
    async: true
}
"#;
```

## Storage Backends

```rust
use tusklang_rust::{RateLimitDirective, StorageBackend, StorageConfig};

// Redis storage backend
let redis_storage = r#"
#rate-limit redis_storage {
    handler: "RedisRateLimitHandler::check"
    strategy: "sliding_window"
    
    storage: {
        type: "redis"
        connection: "default"
        key_prefix: "rate_limit:"
        
        config: {
            pool_size: 10
            timeout: "5s"
            retry_attempts: 3
        }
        
        clustering: {
            enabled: true
            nodes: ["redis://localhost:6379", "redis://localhost:6380"]
            strategy: "consistent_hashing"
        }
    }
    
    limit: 100
    window: "1h"
    async: true
}
"#;

// Memory storage backend
let memory_storage = r#"
#rate-limit memory_storage {
    handler: "MemoryRateLimitHandler::check"
    strategy: "fixed_window"
    
    storage: {
        type: "memory"
        
        config: {
            max_entries: 10000
            cleanup_interval: "1h"
            eviction_policy: "lru"
        }
        
        sharding: {
            enabled: true
            shards: 16
            hash_function: "xxhash"
        }
    }
    
    limit: 1000
    window: "1d"
    async: true
}
"#;

// Database storage backend
let database_storage = r#"
#rate-limit database_storage {
    handler: "DatabaseRateLimitHandler::check"
    strategy: "sliding_window"
    
    storage: {
        type: "database"
        connection: "default"
        table: "rate_limits"
        
        config: {
            cleanup_interval: "1h"
            batch_size: 1000
            index_fields: ["key", "window_start"]
        }
        
        schema: {
            key: "varchar(255)"
            window_start: "timestamp"
            count: "integer"
            created_at: "timestamp"
        }
    }
    
    limit: 100
    window: "1h"
    async: true
}
"#;
```

## Rate Limiting Headers and Responses

```rust
use tusklang_rust::{RateLimitDirective, HeaderManager, ResponseFormatter};

// Rate limiting with standard headers
let standard_headers = r#"
#rate-limit standard_headers {
    handler: "StandardHeaderHandler::check"
    strategy: "sliding_window"
    
    headers: {
        "X-RateLimit-Limit": "@limit"
        "X-RateLimit-Remaining": "@remaining"
        "X-RateLimit-Reset": "@reset_time"
        "X-RateLimit-Used": "@used"
    }
    
    response_format: {
        on_success: {
            status: 200
            headers: "@rate_limit_headers"
        }
        
        on_exceeded: {
            status: 429
            headers: "@rate_limit_headers"
            body: {
                error: "rate_limit_exceeded"
                message: "Rate limit exceeded"
                retry_after: "@retry_after"
                limit: "@limit"
                remaining: "@remaining"
                reset: "@reset_time"
            }
        }
    }
    
    limit: 100
    window: "1h"
    async: true
}
"#;

// Custom header format
let custom_headers = r#"
#rate-limit custom_headers {
    handler: "CustomHeaderHandler::check"
    strategy: "token_bucket"
    
    headers: {
        "X-API-Limit": "@limit"
        "X-API-Remaining": "@remaining"
        "X-API-Reset": "@reset_time"
        "X-API-Burst": "@burst_capacity"
    }
    
    response_format: {
        on_success: {
            status: 200
            headers: "@custom_headers"
        }
        
        on_exceeded: {
            status: 429
            headers: "@custom_headers"
            body: {
                code: "RATE_LIMIT_EXCEEDED"
                message: "API rate limit exceeded"
                details: {
                    limit: "@limit"
                    remaining: "@remaining"
                    reset: "@reset_time"
                    retry_after: "@retry_after"
                }
            }
        }
    }
    
    limit: 1000
    window: "1d"
    burst: 100
    async: true
}
"#;
```

## Adaptive Rate Limiting

```rust
use tusklang_rust::{RateLimitDirective, AdaptiveLimiter, AdaptiveStrategy};

// Adaptive rate limiting based on user behavior
let adaptive_behavior = r#"
#rate-limit adaptive_behavior {
    handler: "AdaptiveBehaviorHandler::check"
    strategy: "adaptive"
    
    adaptive: {
        enabled: true
        strategy: "behavior_based"
        
        factors: {
            user_reputation: {
                weight: 0.3
                source: "@user.reputation_score"
                range: [0, 100]
            }
            
            request_quality: {
                weight: 0.2
                source: "@request.quality_score"
                range: [0, 1]
            }
            
            historical_behavior: {
                weight: 0.5
                source: "@user.behavior_score"
                range: [0, 100]
            }
        }
        
        limits: {
            trusted: {
                limit: 1000
                window: "1h"
                burst: 100
            }
            
            normal: {
                limit: 100
                window: "1h"
                burst: 10
            }
            
            restricted: {
                limit: 10
                window: "1h"
                burst: 1
            }
        }
    }
    
    key: "@user.id"
    async: true
}
"#;

// Adaptive rate limiting based on system load
let adaptive_load = r#"
#rate-limit adaptive_load {
    handler: "AdaptiveLoadHandler::check"
    strategy: "adaptive"
    
    adaptive: {
        enabled: true
        strategy: "load_based"
        
        metrics: {
            cpu_usage: {
                weight: 0.4
                threshold: 80
                source: "@system.cpu_usage"
            }
            
            memory_usage: {
                weight: 0.3
                threshold: 85
                source: "@system.memory_usage"
            }
            
            response_time: {
                weight: 0.3
                threshold: "500ms"
                source: "@system.avg_response_time"
            }
        }
        
        scaling: {
            normal_load: {
                multiplier: 1.0
                limit: 100
            }
            
            high_load: {
                multiplier: 0.5
                limit: 50
            }
            
            critical_load: {
                multiplier: 0.2
                limit: 20
            }
        }
    }
    
    key: "@request.ip"
    async: true
}
"#;
```

## Rate Limiting for Different Endpoints

```rust
use tusklang_rust::{RateLimitDirective, EndpointLimiter, PathMatcher};

// Endpoint-specific rate limiting
let endpoint_specific = r#"
#rate-limit endpoint_specific {
    handler: "EndpointSpecificHandler::check"
    strategy: "sliding_window"
    
    endpoints: {
        "/api/users": {
            limit: 100
            window: "1h"
            key: "@user.id"
        }
        
        "/api/posts": {
            limit: 50
            window: "1h"
            key: "@user.id"
        }
        
        "/api/comments": {
            limit: 200
            window: "1h"
            key: "@user.id"
        }
        
        "/api/admin/*": {
            limit: 1000
            window: "1h"
            key: "@user.id"
            roles: ["admin"]
        }
    }
    
    default: {
        limit: 10
        window: "1h"
        key: "@request.ip"
    }
    
    async: true
}
"#;

// Method-specific rate limiting
let method_specific = r#"
#rate-limit method_specific {
    handler: "MethodSpecificHandler::check"
    strategy: "token_bucket"
    
    methods: {
        GET: {
            limit: 1000
            window: "1h"
            burst: 100
            key: "@request.ip"
        }
        
        POST: {
            limit: 100
            window: "1h"
            burst: 10
            key: "@user.id"
        }
        
        PUT: {
            limit: 50
            window: "1h"
            burst: 5
            key: "@user.id"
        }
        
        DELETE: {
            limit: 10
            window: "1h"
            burst: 1
            key: "@user.id"
            roles: ["admin"]
        }
    }
    
    async: true
}
"#;
```

## Integration with Rust Web Frameworks

```rust
use actix_web::{web, App, HttpServer};
use tusklang_rust::{RateLimitDirective, ActixIntegration};

// Actix-web integration
async fn create_actix_app() -> App<()> {
    let rate_limit_directives = parse(r#"
#rate-limit api_requests {
    handler: "ApiRateLimitHandler::check"
    strategy: "sliding_window"
    limit: 100
    window: "1h"
    key: "@request.ip"
    async: true
}

#rate-limit user_requests {
    handler: "UserRateLimitHandler::check"
    strategy: "token_bucket"
    limit: 1000
    window: "1d"
    key: "@user.id"
    async: true
}
"#)?;
    
    App::new()
        .wrap(RateLimitDirective::create_actix_middleware(rate_limit_directives))
}

// Axum integration
use axum::{Router, middleware as axum_middleware};
use tusklang_rust::AxumIntegration;

async fn create_axum_app() -> Router {
    let rate_limit_directives = parse(r#"
#rate-limit api_requests {
    handler: "ApiRateLimitHandler::check"
    strategy: "sliding_window"
    limit: 100
    window: "1h"
    key: "@request.ip"
    async: true
}
"#)?;
    
    Router::new()
        .layer(RateLimitDirective::create_axum_layer(rate_limit_directives))
}
```

## Testing Rate Limit Directives with Rust

```rust
use tusklang_rust::{RateLimitDirectiveTester, TestRateLimit, TestRequest};
use tokio::test;

#[tokio::test]
async fn test_rate_limit_directive() {
    let rate_limit_directive = r#"
#rate-limit test_limit {
    handler: "TestRateLimitHandler::check"
    strategy: "sliding_window"
    limit: 5
    window: "1m"
    key: "@request.ip"
    async: true
}
"#;
    
    let tester = RateLimitDirectiveTester::new();
    let test_request = TestRequest::new("192.168.1.1");
    
    // Test within limit
    for i in 0..5 {
        let result = tester
            .test_rate_limit_directive(rate_limit_directive, &test_request)
            .execute()
            .await?;
        
        assert_eq!(result.status, "allowed");
        assert_eq!(result.remaining, 4 - i);
    }
    
    // Test exceeding limit
    let result = tester
        .test_rate_limit_directive(rate_limit_directive, &test_request)
        .execute()
        .await?;
    
    assert_eq!(result.status, "exceeded");
    assert_eq!(result.remaining, 0);
}

#[tokio::test]
async fn test_token_bucket_directive() {
    let rate_limit_directive = r#"
#rate-limit test_bucket {
    handler: "TestTokenBucketHandler::check"
    strategy: "token_bucket"
    limit: 10
    window: "1m"
    burst: 5
    key: "@request.ip"
    async: true
}
"#;
    
    let tester = RateLimitDirectiveTester::new();
    let test_request = TestRequest::new("192.168.1.1");
    
    // Test burst capacity
    for i in 0..5 {
        let result = tester
            .test_rate_limit_directive(rate_limit_directive, &test_request)
            .execute()
            .await?;
        
        assert_eq!(result.status, "allowed");
        assert_eq!(result.remaining, 4 - i);
    }
    
    // Test rate limiting
    let result = tester
        .test_rate_limit_directive(rate_limit_directive, &test_request)
        .execute()
        .await?;
    
    assert_eq!(result.status, "exceeded");
}
```

## Performance Optimization with Rust

```rust
use tusklang_rust::{RateLimitDirective, PerformanceOptimizer};
use std::sync::Arc;
use tokio::sync::RwLock;

// Zero-copy rate limit processing
fn process_rate_limit_zero_copy<'a>(directive: &'a str) -> RateLimitDirectiveResult<RateLimitContext<'a>> {
    let context = RateLimitContext::from_str(directive)?;
    Ok(context)
}

// Async rate limit processing with Rust futures
async fn process_rate_limit_async(directive: &RateLimitDirective) -> RateLimitDirectiveResult<()> {
    let handler = find_handler_async(&directive.handler).await?;
    handler.execute_async().await?;
    Ok(())
}

// Rate limit caching
let cached_rate_limit = r#"
#rate-limit cached {
    handler: "CachedRateLimitHandler::check"
    strategy: "sliding_window"
    
    cache: {
        enabled: true
        ttl: "1m"
        key: "rate_limit:{@request.ip}"
        strategy: "memory"
    }
    
    limit: 100
    window: "1h"
    async: true
}
"#;
```

## Security Best Practices with Rust

```rust
use tusklang_rust::{RateLimitDirective, SecurityValidator};
use std::collections::HashSet;

// Security validation for rate limit directives
struct RateLimitSecurityValidator {
    allowed_strategies: HashSet<String>,
    allowed_handlers: HashSet<String>,
    max_limit: u32,
    min_window: Duration,
    restricted_keys: HashSet<String>,
}

impl RateLimitSecurityValidator {
    fn validate_rate_limit_directive(&self, directive: &RateLimitDirective) -> RateLimitDirectiveResult<()> {
        // Validate strategy
        if !self.allowed_strategies.contains(&directive.strategy) {
            return Err(RateLimitError::SecurityError(
                format!("Strategy not allowed: {}", directive.strategy)
            ));
        }
        
        // Validate handler
        if !self.allowed_handlers.contains(&directive.handler) {
            return Err(RateLimitError::SecurityError(
                format!("Handler not allowed: {}", directive.handler)
            ));
        }
        
        // Validate limit
        if directive.limit > self.max_limit {
            return Err(RateLimitError::SecurityError(
                format!("Limit too high: {}", directive.limit)
            ));
        }
        
        // Validate window
        if directive.window < self.min_window {
            return Err(RateLimitError::SecurityError(
                format!("Window too short: {:?}", directive.window)
            ));
        }
        
        Ok(())
    }
}
```

## Best Practices for Rust Rate Limit Directives

```rust
// 1. Use strong typing for rate limit configurations
#[derive(Debug, Deserialize, Serialize)]
struct RateLimitDirectiveConfig {
    strategy: String,
    handler: String,
    limit: u32,
    window: Duration,
    key: String,
    async: bool,
    headers: Option<HashMap<String, String>>,
}

// 2. Implement proper error handling
fn process_rate_limit_directive_safe(directive: &str) -> Result<RateLimitDirective, Box<dyn std::error::Error>> {
    let parsed = parse(directive)?;
    
    // Validate directive
    let validator = RateLimitSecurityValidator::new();
    validator.validate_rate_limit_directive(&parsed)?;
    
    Ok(parsed)
}

// 3. Use async/await for I/O operations
async fn execute_rate_limit_directive_async(directive: &RateLimitDirective) -> RateLimitDirectiveResult<()> {
    let handler = find_handler_async(&directive.handler).await?;
    handler.execute_async().await?;
    Ok(())
}

// 4. Implement proper logging and monitoring
use tracing::{info, warn, error};

fn log_rate_limit_execution(directive: &RateLimitDirective, result: &RateLimitDirectiveResult<()>) {
    match result {
        Ok(_) => info!("Rate limit directive executed successfully: {}", directive.strategy),
        Err(e) => error!("Rate limit directive execution failed: {} - {}", directive.strategy, e),
    }
}
```

## Next Steps

Now that you understand the `#rate-limit` directive in Rust, explore the final directive type:

- **[#custom Directives](./085-hash-custom-directives-rust.md)** - Building your own directives

**Ready to build robust rate limiting systems with Rust and TuskLang? Let's continue with the final directive!** 