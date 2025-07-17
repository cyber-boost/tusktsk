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
// pub mod cli;
// pub mod wasm;
// pub mod peanut;
// mod k8s;

pub use parser::{Parser, ParserBuilder};
// pub use parser_enhanced::{EnhancedParser, load_from_peanut};
pub use error::{TuskError, TuskResult};
pub use value::{Value, ValueType};

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

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_basic_parsing() {
        let input = r#"
app_name: "Test App"
version: "1.0.0"
debug: true
port: 8080
"#;
        
        let result = parse(input).unwrap();
        assert_eq!(result.get("app_name").unwrap(), &Value::String("Test App".to_string()));
        assert_eq!(result.get("version").unwrap(), &Value::String("1.0.0".to_string()));
        assert_eq!(result.get("debug").unwrap(), &Value::Boolean(true));
        assert_eq!(result.get("port").unwrap(), &Value::Number(8080.0));
    }

    #[test]
    fn test_nested_parsing() {
        let input = r#"
database:
  host: "localhost"
  port: 5432
  name: "testdb"
"#;
        
        let result = parse(input).unwrap();
        let database = result.get("database").unwrap().as_object().unwrap();
        assert_eq!(database.get("host").unwrap(), &Value::String("localhost".to_string()));
        assert_eq!(database.get("port").unwrap(), &Value::Number(5432.0));
        assert_eq!(database.get("name").unwrap(), &Value::String("testdb".to_string()));
    }

    #[test]
    fn test_array_parsing() {
        let input = r#"
features:
  - logging
  - metrics
  - caching
"#;
        
        let result = parse(input).unwrap();
        let features = result.get("features").unwrap().as_array().unwrap();
        assert_eq!(features.len(), 3);
        assert_eq!(features[0], Value::String("logging".to_string()));
        assert_eq!(features[1], Value::String("metrics".to_string()));
        assert_eq!(features[2], Value::String("caching".to_string()));
    }
} 