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
pub mod error;
pub mod value;
pub mod validation;

// Core G15 modules - our main focus
pub mod ai_ml_engine;
pub mod observability;
pub mod workflow_orchestration;

// Operators module has 220+ compilation errors - needs systematic fixing
// pub mod operators;

// Temporarily disable all other modules until core issues are resolved
// pub mod cache;
// pub mod config_manager;
// pub mod security;
// pub mod monitoring;
// pub mod memory_manager;
// pub mod task_scheduler;
// pub mod metrics_collector;
// pub mod network;
// pub mod serialization;
// pub mod database;
// pub mod filesystem;
// pub mod logging;
// pub mod web;
// pub mod template;
// pub mod session;
// pub mod api_gateway;
// pub mod event_system;
// pub mod plugin_system;
// pub mod examples;
// pub mod operators;
// pub mod enterprise;
// pub mod advanced_parsing;
// pub mod code_generator;
// pub mod code_analyzer;
// pub mod data_visualization;
// pub mod advanced_security;
// pub mod analytics_bi;
// pub mod api_management;
// pub mod microservices;
// pub mod config_deployment;
// pub mod performance_profiling;
// pub mod cloud_native;
// pub mod event_streaming;
// pub mod advanced_caching;

#[cfg(test)]
mod tests;

pub use parser::{Parser, ParserBuilder};
pub use error::{TuskError, TuskResult};
pub use value::{Value, ValueType};
pub use validation::{SchemaValidator, SchemaBuilder, ConfigSchema, ValidationRule, ValidationResult};

// G15 module exports
pub use ai_ml_engine::{AIMLEngineManager, AIMLConfig, ModelRegistry, InferenceEngine, FeatureStore, PipelineOrchestrator, ModelMonitoring};
pub use observability::{ObservabilityManager, ObservabilityConfig, TracingSystem, MetricsSystem, LoggingSystem, APMSystem, TopologyMapper, AnomalyDetector};
pub use workflow_orchestration::{WorkflowOrchestrationManager, OrchestrationConfig, WorkflowEngine, TaskScheduler, EventProcessor, WorkflowMonitor, IntegrationHub};

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
    for (key, value) in config {
        output.push_str(&" ".repeat(indent));
        output.push_str(key);
        output.push_str(" = ");
        serialize_value(value, output, indent)?;
        output.push('\n');
    }
    Ok(())
}

fn serialize_value(value: &Value, output: &mut String, indent: usize) -> TuskResult<()> {
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
                serialize_value(item, output, indent)?;
            }
            output.push(']');
        }
        Value::Object(obj) => {
            output.push_str("{\n");
            for (key, val) in obj {
                output.push_str(&" ".repeat(indent + 2));
                output.push_str(key);
                output.push_str(" = ");
                serialize_value(val, output, indent + 2)?;
                output.push('\n');
            }
            output.push_str(&" ".repeat(indent));
            output.push('}');
        }
        Value::Null => {
            output.push_str("null");
        }
    }
    Ok(())
}



 