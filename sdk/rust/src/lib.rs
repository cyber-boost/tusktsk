//! # TuskLang Rust Implementation
//! 
//! Ultra-fast Rust implementation of TuskLang configuration parser.
//! 
//! ## Features
//! - Lightning-fast parsing with zero-copy operations
//! - WebAssembly support for browser environments
//! - Type-safe configuration with Serde integration
//! - Memory-efficient parsing with minimal allocations
//! - Comprehensive error handling with detailed diagnostics

pub mod parser;
// pub mod parser_enhanced;
pub mod error;
pub mod value;
pub mod validation;
pub mod cache;
pub mod config_manager;
pub mod security;
pub mod monitoring;
pub mod memory_manager;
pub mod task_scheduler;
pub mod metrics_collector;
pub mod network;
pub mod serialization;
pub mod cache;
pub mod database;
pub mod filesystem;
pub mod logging;
pub mod web;
pub mod template;
pub mod session;
pub mod api_gateway;
pub mod event_system;
pub mod plugin_system;
pub mod examples;
pub mod operators;
pub mod enterprise;
pub mod database;
// pub mod cli;
// pub mod wasm;
// pub mod peanut;
// mod k8s;

#[cfg(test)]
mod tests;



pub use parser::{Parser, ParserBuilder};
// pub use parser_enhanced::{EnhancedParser, load_from_peanut};
pub use error::{TuskError, TuskResult};
pub use value::{Value, ValueType};
pub use validation::{SchemaValidator, SchemaBuilder, ConfigSchema, ValidationRule, ValidationResult};
pub use cache::{CacheManager, ThreadSafeCache, PerformanceMonitor, CacheStats, OperationStats};
pub use config_manager::{ConfigManager, ConfigEnvironment, ConfigSource, ConfigMetadata, ConfigChangeEvent};
pub use security::{SecurityManager, SecureConfig, EncryptionAlgorithm, SecurityLevel, KeyMetadata, EncryptedData};
pub use monitoring::{MonitoringSystem, PerformanceMonitor as MonitoringPerformanceMonitor, MetricType, HealthStatus, AlertRule};
pub use memory_manager::{MemoryManager, MemoryPool, PoolBuilder, MemoryStats, PoolStats};
pub use task_scheduler::{TaskScheduler, SchedulerBuilder, TaskPriority, TaskStatus, TaskMetadata, SchedulerStats};
pub use metrics_collector::{MetricsCollector, CollectorBuilder, MetricRegistry, MetricType as CollectorMetricType, MetricValue, MetricLabels};
pub use network::{NetworkManager, NetworkClient, NetworkServer, ConnectionPool, Protocol, NetworkMessage, ConnectionConfig, PoolStats as NetworkPoolStats};
pub use serialization::{SerializationSystem, SerializationBuilder, SerializationFormat, CompressionAlgorithm, SerializedData, SchemaDefinition};
pub use cache::{CacheSystem, DistributedCache, CacheBuilder, EvictionPolicy, CacheResult, CacheStats, CacheNode};
pub use database::{DatabaseManager, DatabasePool, QueryBuilder, DatabaseConfig, DatabaseType, QueryResult, Value as DbValue};
pub use filesystem::{FileSystemManager, FileWatcher, FileSystemEvent, FileMetadata, WatcherConfig, FileSystemBuilder};
pub use logging::{Logger, LoggerBuilder, LogLevel, LogEntry, LogStats, LoggingConfig};
pub use web::{HttpServer, Router, HttpRequest, HttpResponse, HttpMethod, HttpStatus, WebFrameworkBuilder, middleware};
pub use template::{TemplateEngine, TemplateContext, TemplateValue, TemplateBuilder};
pub use session::{SessionManager, SessionAuth, SessionData, SessionConfig, SessionBuilder, SessionStats, MemorySessionStorage};
pub use api_gateway::{ApiGateway, ApiGatewayBuilder, ApiRoute, RateLimit, AuthConfig, CircuitBreakerConfig, GatewayConfig, GatewayStats};
pub use event_system::{EventStream, EventBus, EventSystemBuilder, Event, EventType, EventPriority, StreamConfig, StreamStats};
pub use plugin_system::{PluginManager, PluginSystemBuilder, Plugin, PluginMetadata, PluginConfig, PluginStatus, PluginContext};

use serde::{Deserialize, Serialize};
use std::collections::HashMap;

/// Main configuration type that can hold any TuskLang value
pub type Config = HashMap<String, Value>;

/// Parse a TuskLang string into a Config
pub fn parse(input: &str) -> TuskResult<Config> {
    let mut parser = Parser::new();
    parser.parse(input)
}

/// Parse a TuskLang string into a typed struct
pub fn parse_into<T>(input: &str) -> TuskResult<T>
where
    T: for<'de> Deserialize<'de>,
{
    let config = parse(input)?;
    let json = serde_json::to_value(config)?;
    Ok(T::deserialize(json)?)
}

/// Serialize a Config back to TuskLang format
pub fn serialize(config: &Config) -> TuskResult<String> {
    let mut output = String::new();
    serialize_config(config, &mut output, 0)?;
    Ok(output)
}

fn serialize_config(config: &Config, output: &mut String, indent: usize) -> TuskResult<()> {
    let indent_str = "  ".repeat(indent);
    
    for (key, value) in config {
        output.push_str(&indent_str);
        output.push_str(key);
        output.push_str(": ");
        
        match value {
            Value::String(s) => {
                output.push('"');
                output.push_str(s);
                output.push('"');
            }
            Value::Number(n) => {
                output.push_str(&n.to_string());
            }
            Value::Boolean(b) => {
                output.push_str(&b.to_string());
            }
            Value::Array(arr) => {
                output.push('\n');
                for item in arr {
                    output.push_str(&indent_str);
                    output.push_str("  - ");
                    serialize_value(item, output)?;
                    output.push('\n');
                }
                output.pop(); // Remove trailing newline
                continue;
            }
            Value::Object(obj) => {
                output.push('\n');
                serialize_config(obj, output, indent + 1)?;
                continue;
            }
            Value::Null => {
                output.push_str("null");
            }
        }
        output.push('\n');
    }
    
    Ok(())
}

fn serialize_value(value: &Value, output: &mut String) -> TuskResult<()> {
    match value {
        Value::String(s) => {
            output.push('"');
            output.push_str(s);
            output.push('"');
        }
        Value::Number(n) => {
            output.push_str(&n.to_string());
        }
        Value::Boolean(b) => {
            output.push_str(&b.to_string());
        }
        Value::Array(arr) => {
            output.push('[');
            for (i, item) in arr.iter().enumerate() {
                if i > 0 {
                    output.push_str(", ");
                }
                serialize_value(item, output)?;
            }
            output.push(']');
        }
        Value::Object(obj) => {
            output.push('{');
            for (i, (key, val)) in obj.iter().enumerate() {
                if i > 0 {
                    output.push_str(", ");
                }
                output.push('"');
                output.push_str(key);
                output.push_str("\": ");
                serialize_value(val, output)?;
            }
            output.push('}');
        }
        Value::Null => {
            output.push_str("null");
        }
    }
    Ok(())
}

// pub mod protection;  // Temporarily disabled due to crypto API issues

// Re-export protection functions
// pub use protection::{initialize_protection, get_protection, TuskProtection};



 