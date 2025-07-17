# 🦀 #cron - Scheduled Task Directive - Rust Edition

**"We don't bow to any king" - Rust Edition**

The `#cron` directive in Rust creates scheduled tasks with zero-copy execution, async support, and seamless integration with Rust's background job processing ecosystem.

## Basic Syntax

```rust
use tusklang_rust::{parse, CronDirective, CronScheduler};
use tokio::time::{Duration, interval};
use chrono::{DateTime, Utc};

// Simple cron job with Rust handler
let cron_config = r#"
#cron "0 * * * *" {
    handler: "HourlyJob::execute"
    description: "Run every hour"
    async: true
}
"#;

// Cron job with parameters
let backup_job = r#"
#cron "0 2 * * *" {
    handler: "BackupJob::execute"
    description: "Daily backup at 2 AM"
    timeout: "30m"
    retry: 3
}
"#;

// Cron job with environment
let cleanup_job = r#"
#cron "0 0 * * 0" {
    handler: "CleanupJob::execute"
    description: "Weekly cleanup on Sunday"
    environment: "production"
    priority: "low"
}
"#;
```

## Cron Expressions with Rust Parsing

```rust
use tusklang_rust::{CronDirective, CronExpression, CronParser};
use cron::Schedule;
use chrono::{DateTime, Utc};

// Standard cron expressions
let standard_cron = r#"
#cron "*/5 * * * *" {
    handler: "FrequentJob::execute"
    description: "Every 5 minutes"
    async: true
}

#cron "0 9 * * 1-5" {
    handler: "WorkdayJob::execute"
    description: "Weekdays at 9 AM"
    timezone: "America/New_York"
}

#cron "0 0 1 * *" {
    handler: "MonthlyJob::execute"
    description: "First day of each month"
    timeout: "1h"
}
"#;

// Advanced cron expressions
let advanced_cron = r#"
#cron "0 12 * * MON,WED,FRI" {
    handler: "AlternateJob::execute"
    description: "Monday, Wednesday, Friday at noon"
    async: true
}

#cron "0 0 1 1 *" {
    handler: "YearlyJob::execute"
    description: "New Year's Day"
    timeout: "2h"
    retry: 5
}

#cron "0 */6 * * *" {
    handler: "SixHourJob::execute"
    description: "Every 6 hours"
    async: true
}
"#;
```

## Async Job Processing with Rust

```rust
use tusklang_rust::{CronDirective, AsyncJob, JobContext};
use tokio::task::JoinHandle;
use std::sync::Arc;

// Async job with Rust futures
let async_job = r#"
#cron "0 * * * *" {
    handler: "AsyncDataJob::execute"
    description: "Async data processing every hour"
    
    async: true
    timeout: "10m"
    concurrency: 5
    
    context: {
        database_pool: "default"
        cache_client: "redis"
        queue: "background"
    }
    
    error_handling: {
        retry: 3
        backoff: "exponential"
        max_delay: "1h"
    }
}
"#;

// Background job with Rust async runtime
let background_job = r#"
#cron "0 0 * * *" {
    handler: "BackgroundCleanupJob::execute"
    description: "Daily background cleanup"
    
    runtime: {
        type: "tokio"
        workers: 4
        stack_size: "2MB"
    }
    
    async: true
    timeout: "30m"
    
    dependencies: {
        database: "available"
        redis: "available"
        storage: "available"
    }
}
"#;
```

## Job Context and State Management

```rust
use tusklang_rust::{CronDirective, JobContext, JobState};
use std::collections::HashMap;
use serde::{Deserialize, Serialize};

#[derive(Debug, Deserialize, Serialize)]
struct JobContext {
    job_id: String,
    schedule: String,
    last_run: Option<DateTime<Utc>>,
    next_run: DateTime<Utc>,
    run_count: u64,
    state: JobState,
}

// Job context with Rust state management
let context_job = r#"
#cron "0 */2 * * *" {
    handler: "ContextAwareJob::execute"
    description: "Context-aware job every 2 hours"
    
    context: {
        job_id: "@generate_job_id()"
        schedule: "@cron.schedule"
        last_run: "@cron.last_run"
        next_run: "@cron.next_run"
        run_count: "@cron.run_count"
    }
    
    state: {
        persistent: true
        storage: "redis"
        ttl: "24h"
    }
    
    async: true
    timeout: "5m"
}
"#;

// Stateful job with Rust persistence
let stateful_job = r#"
#cron "0 0 * * *" {
    handler: "StatefulJob::execute"
    description: "Stateful daily job"
    
    state_management: {
        enabled: true
        storage: "postgres"
        table: "job_states"
        
        fields: {
            last_processed_id: "bigint"
            processed_count: "integer"
            errors: "jsonb"
            metadata: "jsonb"
        }
    }
    
    async: true
    timeout: "1h"
}
"#;
```

## Error Handling and Retry Logic

```rust
use tusklang_rust::{CronDirective, JobError, RetryStrategy};
use thiserror::Error;

#[derive(Error, Debug)]
pub enum JobError {
    #[error("Job timeout: {0}")]
    TimeoutError(String),
    
    #[error("Job failed: {0}")]
    ExecutionError(String),
    
    #[error("Dependency unavailable: {0}")]
    DependencyError(String),
    
    #[error("Resource exhausted: {0}")]
    ResourceError(String),
}

// Error handling with Rust Result types
let error_handling_job = r#"
#cron "0 * * * *" {
    handler: "ErrorProneJob::execute"
    description: "Job with comprehensive error handling"
    
    error_handling: {
        retry: {
            max_attempts: 3
            strategy: "exponential"
            initial_delay: "1m"
            max_delay: "1h"
        }
        
        on_timeout: {
            action: "retry"
            log: true
            notify: "admin"
        }
        
        on_failure: {
            action: "retry"
            log: true
            store_error: true
        }
        
        on_success: {
            log: true
            update_state: true
        }
    }
    
    async: true
    timeout: "10m"
}
"#;

// Circuit breaker pattern
let circuit_breaker_job = r#"
#cron "*/5 * * * *" {
    handler: "CircuitBreakerJob::execute"
    description: "Job with circuit breaker"
    
    circuit_breaker: {
        enabled: true
        failure_threshold: 5
        recovery_timeout: "5m"
        half_open_max_calls: 3
    }
    
    async: true
    timeout: "2m"
}
"#;
```

## Job Dependencies and Orchestration

```rust
use tusklang_rust::{CronDirective, JobDependency, JobOrchestrator};

// Job dependencies with Rust async patterns
let dependent_job = r#"
#cron "0 2 * * *" {
    handler: "DependentJob::execute"
    description: "Job with dependencies"
    
    dependencies: {
        required: {
            database_backup: "completed"
            log_rotation: "completed"
            cache_clear: "completed"
        }
        
        optional: {
            analytics_export: "available"
            monitoring_check: "available"
        }
        
        timeout: "30m"
        retry: 3
    }
    
    async: true
    timeout: "1h"
}
"#;

// Job orchestration with Rust futures
let orchestrated_job = r#"
#cron "0 0 * * *" {
    handler: "OrchestratedJob::execute"
    description: "Orchestrated daily job"
    
    orchestration: {
        parallel: {
            data_export: "DataExportJob::execute"
            log_analysis: "LogAnalysisJob::execute"
            metrics_collection: "MetricsJob::execute"
        }
        
        sequential: {
            cleanup: "CleanupJob::execute"
            notification: "NotificationJob::execute"
        }
        
        conditional: {
            if: "@data_export.success"
            then: "SuccessJob::execute"
            else: "FailureJob::execute"
        }
    }
    
    async: true
    timeout: "2h"
}
"#;
```

## Performance Monitoring and Metrics

```rust
use tusklang_rust::{CronDirective, JobMetrics, PerformanceMonitor};
use std::time::Instant;

// Performance monitoring with Rust metrics
let monitored_job = r#"
#cron "0 * * * *" {
    handler: "MonitoredJob::execute"
    description: "Job with performance monitoring"
    
    monitoring: {
        enabled: true
        metrics: {
            execution_time: "histogram"
            memory_usage: "gauge"
            cpu_usage: "gauge"
            error_rate: "counter"
        }
        
        alerts: {
            execution_time: {
                threshold: "5m"
                action: "notify"
            }
            
            error_rate: {
                threshold: "10%"
                action: "retry"
            }
        }
    }
    
    async: true
    timeout: "10m"
}
"#;

// Custom metrics collection
let metrics_job = r#"
#cron "*/10 * * * *" {
    handler: "MetricsJob::execute"
    description: "Custom metrics collection"
    
    metrics: {
        collection: {
            system_metrics: "@collect_system_metrics()"
            application_metrics: "@collect_app_metrics()"
            business_metrics: "@collect_business_metrics()"
        }
        
        storage: {
            destination: "prometheus"
            retention: "30d"
            aggregation: "5m"
        }
    }
    
    async: true
    timeout: "2m"
}
"#;
```

## Resource Management and Limits

```rust
use tusklang_rust::{CronDirective, ResourceLimits, ResourceManager};

// Resource limits with Rust resource management
let resource_limited_job = r#"
#cron "0 * * * *" {
    handler: "ResourceLimitedJob::execute"
    description: "Job with resource limits"
    
    resources: {
        memory: {
            limit: "512MB"
            reservation: "256MB"
        }
        
        cpu: {
            limit: "2 cores"
            reservation: "1 core"
        }
        
        disk: {
            limit: "1GB"
            path: "/tmp/jobs"
        }
        
        network: {
            bandwidth: "10MB/s"
            connections: 10
        }
    }
    
    async: true
    timeout: "15m"
}
"#;

// Resource pooling
let pooled_job = r#"
#cron "*/5 * * * *" {
    handler: "PooledJob::execute"
    description: "Job with resource pooling"
    
    resource_pool: {
        database: {
            pool: "default"
            max_connections: 10
            timeout: "30s"
        }
        
        redis: {
            pool: "cache"
            max_connections: 5
            timeout: "10s"
        }
        
        http: {
            pool: "api"
            max_connections: 20
            timeout: "60s"
        }
    }
    
    async: true
    timeout: "5m"
}
"#;
```

## Integration with Rust Job Queues

```rust
use tusklang_rust::{CronDirective, JobQueue, QueueIntegration};
use tokio::sync::mpsc;

// Integration with job queues
let queued_job = r#"
#cron "0 * * * *" {
    handler: "QueuedJob::execute"
    description: "Job with queue integration"
    
    queue: {
        name: "background"
        priority: "normal"
        retry: 3
        timeout: "10m"
        
        routing: {
            high_priority: "high_queue"
            normal: "default_queue"
            low_priority: "low_queue"
        }
    }
    
    async: true
    timeout: "5m"
}
"#;

// Multiple queue integration
let multi_queue_job = r#"
#cron "0 0 * * *" {
    handler: "MultiQueueJob::execute"
    description: "Job with multiple queues"
    
    queues: {
        data_processing: {
            name: "data_queue"
            priority: "high"
            workers: 5
        }
        
        notifications: {
            name: "notification_queue"
            priority: "low"
            workers: 2
        }
        
        cleanup: {
            name: "cleanup_queue"
            priority: "normal"
            workers: 3
        }
    }
    
    async: true
    timeout: "1h"
}
"#;
```

## Testing Cron Directives with Rust

```rust
use tusklang_rust::{CronDirectiveTester, TestJob, TestSchedule};
use tokio::test;

#[tokio::test]
async fn test_cron_directive() {
    let cron_directive = r#"
#cron "*/1 * * * *" {
    handler: "TestJob::execute"
    description: "Test job every minute"
    async: true
    timeout: "30s"
}
"#;
    
    let tester = CronDirectiveTester::new();
    let result = tester
        .test_cron_directive(cron_directive)
        .wait_for_execution()
        .await?;
    
    assert_eq!(result.status, "completed");
    assert!(result.execution_time < Duration::from_secs(30));
}

#[tokio::test]
async fn test_cron_with_dependencies() {
    let cron_directive = r#"
#cron "0 * * * *" {
    handler: "DependentJob::execute"
    description: "Job with dependencies"
    
    dependencies: {
        required: {
            database: "available"
        }
    }
    
    async: true
    timeout: "5m"
}
"#;
    
    let tester = CronDirectiveTester::new();
    let result = tester
        .test_cron_directive(cron_directive)
        .with_dependency("database", "available")
        .wait_for_execution()
        .await?;
    
    assert_eq!(result.status, "completed");
}
```

## Performance Optimization with Rust

```rust
use tusklang_rust::{CronDirective, PerformanceOptimizer};
use std::sync::Arc;
use tokio::sync::RwLock;

// Zero-copy cron processing
fn process_cron_zero_copy<'a>(directive: &'a str) -> CronDirectiveResult<CronContext<'a>> {
    let context = CronContext::from_str(directive)?;
    Ok(context)
}

// Async cron processing with Rust futures
async fn process_cron_async(directive: &CronDirective) -> CronDirectiveResult<()> {
    let handler = find_handler_async(&directive.handler).await?;
    handler.execute_async().await?;
    Ok(())
}

// Job scheduling optimization
let optimized_job = r#"
#cron "0 * * * *" {
    handler: "OptimizedJob::execute"
    description: "Optimized job"
    
    optimization: {
        parallel_execution: true
        resource_pooling: true
        caching: true
        batch_processing: true
    }
    
    async: true
    timeout: "10m"
}
"#;
```

## Security Best Practices with Rust

```rust
use tusklang_rust::{CronDirective, SecurityValidator};
use std::collections::HashSet;

// Security validation for cron directives
struct CronSecurityValidator {
    allowed_handlers: HashSet<String>,
    allowed_schedules: HashSet<String>,
    max_timeout: Duration,
    restricted_resources: HashSet<String>,
}

impl CronSecurityValidator {
    fn validate_cron_directive(&self, directive: &CronDirective) -> CronDirectiveResult<()> {
        // Validate handler
        if !self.allowed_handlers.contains(&directive.handler) {
            return Err(JobError::SecurityError(
                format!("Handler not allowed: {}", directive.handler)
            ));
        }
        
        // Validate schedule
        if !self.allowed_schedules.contains(&directive.schedule) {
            return Err(JobError::SecurityError(
                format!("Schedule not allowed: {}", directive.schedule)
            ));
        }
        
        // Validate timeout
        if directive.timeout > self.max_timeout {
            return Err(JobError::SecurityError(
                format!("Timeout too long: {:?}", directive.timeout)
            ));
        }
        
        Ok(())
    }
}
```

## Best Practices for Rust Cron Directives

```rust
// 1. Use strong typing for cron configurations
#[derive(Debug, Deserialize, Serialize)]
struct CronDirectiveConfig {
    schedule: String,
    handler: String,
    description: String,
    async: bool,
    timeout: Duration,
    retry: Option<u32>,
    dependencies: Vec<JobDependency>,
}

// 2. Implement proper error handling
fn process_cron_directive_safe(directive: &str) -> Result<CronDirective, Box<dyn std::error::Error>> {
    let parsed = parse(directive)?;
    
    // Validate directive
    let validator = CronSecurityValidator::new();
    validator.validate_cron_directive(&parsed)?;
    
    Ok(parsed)
}

// 3. Use async/await for I/O operations
async fn execute_cron_directive_async(directive: &CronDirective) -> CronDirectiveResult<()> {
    let handler = find_handler_async(&directive.handler).await?;
    handler.execute_async().await?;
    Ok(())
}

// 4. Implement proper logging and monitoring
use tracing::{info, warn, error};

fn log_cron_execution(directive: &CronDirective, result: &CronDirectiveResult<()>) {
    match result {
        Ok(_) => info!("Cron directive executed successfully: {}", directive.schedule),
        Err(e) => error!("Cron directive execution failed: {} - {}", directive.schedule, e),
    }
}
```

## Next Steps

Now that you understand the `#cron` directive in Rust, explore other directive types:

- **[#middleware Directive](./081-hash-middleware-directive-rust.md)** - Request processing pipeline
- **[#auth Directive](./082-hash-auth-directive-rust.md)** - Authentication and authorization
- **[#cache Directive](./083-hash-cache-directive-rust.md)** - Caching strategies
- **[#rate-limit Directive](./084-hash-rate-limit-directive-rust.md)** - Rate limiting and throttling

**Ready to build robust scheduled tasks with Rust and TuskLang? Let's continue with the next directive!** 