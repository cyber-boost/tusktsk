//! Core Operators Test Suite
//! 
//! Tests for the fundamental operators that form the foundation of the TuskLang system.

use tusktsk::operators::{OperatorEngine, OperatorTrait};
use tusktsk::value::Value;
use std::collections::HashMap;
use tokio;

#[tokio::test]
async fn test_variable_operator() {
    let engine = OperatorEngine::new();
    
    let params = r#"{"name": "test_var", "value": "Hello World"}"#;
    let result = engine.execute("variable", params).await;
    
    assert!(result.is_ok());
    let value = result.unwrap();
    
    if let Value::Object(map) = value {
        assert_eq!(map.get("name"), Some(&Value::String("test_var".to_string())));
        assert_eq!(map.get("value"), Some(&Value::String("Hello World".to_string())));
        assert_eq!(map.get("stored"), Some(&Value::Boolean(true)));
    } else {
        panic!("Expected object result");
    }
}

#[tokio::test]
async fn test_date_operator() {
    let engine = OperatorEngine::new();
    
    // Test default format
    let result = engine.execute("date", "{}").await;
    assert!(result.is_ok());
    
    let value = result.unwrap();
    if let Value::Object(map) = value {
        assert!(map.contains_key("timestamp"));
        assert!(map.contains_key("formatted"));
        assert!(map.contains_key("iso"));
        
        // Verify timestamp is a number
        assert!(matches!(map.get("timestamp"), Some(Value::Number(_))));
        
        // Verify formatted is a string
        assert!(matches!(map.get("formatted"), Some(Value::String(_))));
        
        // Verify ISO format is valid
        if let Some(Value::String(iso)) = map.get("iso") {
            assert!(iso.contains("T"));
            assert!(iso.contains("Z") || iso.contains("+"));
        }
    } else {
        panic!("Expected object result");
    }
}

#[tokio::test]
async fn test_date_operator_custom_format() {
    let engine = OperatorEngine::new();
    
    let params = r#"{"format": "%Y-%m-%d"}"#;
    let result = engine.execute("date", params).await;
    
    assert!(result.is_ok());
    let value = result.unwrap();
    
    if let Value::Object(map) = value {
        if let Some(Value::String(formatted)) = map.get("formatted") {
            // Should match YYYY-MM-DD pattern
            assert!(formatted.len() == 10);
            assert!(formatted.matches('-').count() == 2);
        }
    }
}

#[tokio::test]
async fn test_file_operator() {
    let engine = OperatorEngine::new();
    
    // Test with a simple path
    let params = r#"{"path": "/tmp/test.txt"}"#;
    let result = engine.execute("file", params).await;
    
    assert!(result.is_ok());
    // File operations should return appropriate metadata
}

#[tokio::test]
async fn test_math_operators() {
    let engine = OperatorEngine::new();
    
    // Test addition
    let params = r#"{"operation": "add", "a": 10, "b": 5}"#;
    let result = engine.execute("math", params).await;
    assert!(result.is_ok());
    
    if let Ok(Value::Number(sum)) = result {
        assert_eq!(sum, 15.0);
    }
    
    // Test multiplication
    let params = r#"{"operation": "multiply", "a": 10, "b": 5}"#;
    let result = engine.execute("math", params).await;
    assert!(result.is_ok());
    
    if let Ok(Value::Number(product)) = result {
        assert_eq!(product, 50.0);
    }
}

#[tokio::test]
async fn test_array_operators() {
    let engine = OperatorEngine::new();
    
    // Test array creation
    let params = r#"{"items": [1, 2, 3, "test"]}"#;
    let result = engine.execute("array", params).await;
    assert!(result.is_ok());
    
    if let Ok(Value::Array(arr)) = result {
        assert_eq!(arr.len(), 4);
        assert_eq!(arr[0], Value::Number(1.0));
        assert_eq!(arr[3], Value::String("test".to_string()));
    }
}

#[tokio::test]
async fn test_object_operators() {
    let engine = OperatorEngine::new();
    
    // Test object creation
    let params = r#"{"key1": "value1", "key2": 42, "key3": true}"#;
    let result = engine.execute("object", params).await;
    assert!(result.is_ok());
    
    if let Ok(Value::Object(obj)) = result {
        assert_eq!(obj.get("key1"), Some(&Value::String("value1".to_string())));
        assert_eq!(obj.get("key2"), Some(&Value::Number(42.0)));
        assert_eq!(obj.get("key3"), Some(&Value::Boolean(true)));
    }
}

#[tokio::test]
async fn test_invalid_operator() {
    let engine = OperatorEngine::new();
    
    let result = engine.execute("nonexistent_operator", "{}").await;
    assert!(result.is_err());
    
    // Should return appropriate error type
    if let Err(error) = result {
        assert!(error.to_string().contains("Unknown operator"));
    }
}

#[tokio::test]
async fn test_invalid_params() {
    let engine = OperatorEngine::new();
    
    // Test with malformed JSON
    let result = engine.execute("variable", "invalid json").await;
    assert!(result.is_err());
    
    // Test with missing required parameters
    let result = engine.execute("variable", "{}").await;
    assert!(result.is_err());
}

#[tokio::test]
async fn test_type_conversion_operators() {
    let engine = OperatorEngine::new();
    
    // Test string to number conversion
    let params = r#"{"value": "42", "target_type": "number"}"#;
    let result = engine.execute("convert", params).await;
    
    if result.is_ok() {
        if let Ok(Value::Number(num)) = result {
            assert_eq!(num, 42.0);
        }
    }
    
    // Test number to string conversion
    let params = r#"{"value": 42, "target_type": "string"}"#;
    let result = engine.execute("convert", params).await;
    
    if result.is_ok() {
        if let Ok(Value::String(str_val)) = result {
            assert_eq!(str_val, "42");
        }
    }
} 