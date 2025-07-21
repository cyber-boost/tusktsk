//! String Processing Operators Test Suite
//! 
//! Tests for string manipulation, formatting, and processing operators.

use crate::operators::{OperatorEngine, OperatorTrait};
use crate::value::Value;
use tokio;

#[tokio::test]
async fn test_string_concat() {
    let engine = OperatorEngine::new();
    
    let params = r#"{"strings": ["Hello", " ", "World", "!"]}"#;
    let result = engine.execute("string_concat", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::String(concatenated)) = result {
        assert_eq!(concatenated, "Hello World!");
    }
}

#[tokio::test]
async fn test_string_split() {
    let engine = OperatorEngine::new();
    
    let params = r#"{"string": "apple,banana,cherry", "delimiter": ","}"#;
    let result = engine.execute("string_split", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::Array(parts)) = result {
        assert_eq!(parts.len(), 3);
        assert_eq!(parts[0], Value::String("apple".to_string()));
        assert_eq!(parts[1], Value::String("banana".to_string()));
        assert_eq!(parts[2], Value::String("cherry".to_string()));
    }
}

#[tokio::test]
async fn test_string_replace() {
    let engine = OperatorEngine::new();
    
    let params = r#"{"string": "Hello World", "search": "World", "replace": "Rust"}"#;
    let result = engine.execute("string_replace", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::String(replaced)) = result {
        assert_eq!(replaced, "Hello Rust");
    }
}

#[tokio::test]
async fn test_string_case_operations() {
    let engine = OperatorEngine::new();
    
    // Test uppercase
    let params = r#"{"string": "hello world", "operation": "uppercase"}"#;
    let result = engine.execute("string_case", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::String(upper)) = result {
        assert_eq!(upper, "HELLO WORLD");
    }
    
    // Test lowercase
    let params = r#"{"string": "HELLO WORLD", "operation": "lowercase"}"#;
    let result = engine.execute("string_case", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::String(lower)) = result {
        assert_eq!(lower, "hello world");
    }
    
    // Test title case
    let params = r#"{"string": "hello world", "operation": "titlecase"}"#;
    let result = engine.execute("string_case", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::String(title)) = result {
        assert_eq!(title, "Hello World");
    }
}

#[tokio::test]
async fn test_string_trim() {
    let engine = OperatorEngine::new();
    
    let params = r#"{"string": "  Hello World  ", "operation": "trim"}"#;
    let result = engine.execute("string_trim", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::String(trimmed)) = result {
        assert_eq!(trimmed, "Hello World");
    }
}

#[tokio::test]
async fn test_string_length() {
    let engine = OperatorEngine::new();
    
    let params = r#"{"string": "Hello World"}"#;
    let result = engine.execute("string_length", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::Number(length)) = result {
        assert_eq!(length, 11.0);
    }
}

#[tokio::test]
async fn test_string_substring() {
    let engine = OperatorEngine::new();
    
    let params = r#"{"string": "Hello World", "start": 6, "length": 5}"#;
    let result = engine.execute("string_substring", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::String(substring)) = result {
        assert_eq!(substring, "World");
    }
}

#[tokio::test]
async fn test_string_contains() {
    let engine = OperatorEngine::new();
    
    let params = r#"{"string": "Hello World", "search": "World"}"#;
    let result = engine.execute("string_contains", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::Boolean(contains)) = result {
        assert_eq!(contains, true);
    }
    
    let params = r#"{"string": "Hello World", "search": "Rust"}"#;
    let result = engine.execute("string_contains", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::Boolean(contains)) = result {
        assert_eq!(contains, false);
    }
}

#[tokio::test]
async fn test_string_regex_match() {
    let engine = OperatorEngine::new();
    
    let params = r#"{"string": "hello@example.com", "pattern": r"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"}"#;
    let result = engine.execute("string_regex", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::Boolean(matches)) = result {
        assert_eq!(matches, true);
    }
}

#[tokio::test]
async fn test_string_format() {
    let engine = OperatorEngine::new();
    
    let params = r#"{"template": "Hello {name}, you are {age} years old!", "values": {"name": "Alice", "age": 30}}"#;
    let result = engine.execute("string_format", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::String(formatted)) = result {
        assert_eq!(formatted, "Hello Alice, you are 30 years old!");
    }
}

#[tokio::test]
async fn test_string_encoding() {
    let engine = OperatorEngine::new();
    
    // Test base64 encoding
    let params = r#"{"string": "Hello World", "encoding": "base64", "operation": "encode"}"#;
    let result = engine.execute("string_encode", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::String(encoded)) = result {
        // Test decoding
        let params = format!(r#"{{"string": "{}", "encoding": "base64", "operation": "decode"}}"#, encoded);
        let result = engine.execute("string_encode", &params).await;
        
        assert!(result.is_ok());
        if let Ok(Value::String(decoded)) = result {
            assert_eq!(decoded, "Hello World");
        }
    }
}

#[tokio::test]
async fn test_string_hash() {
    let engine = OperatorEngine::new();
    
    let params = r#"{"string": "Hello World", "algorithm": "sha256"}"#;
    let result = engine.execute("string_hash", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::String(hash)) = result {
        assert_eq!(hash.len(), 64); // SHA256 produces 64 character hex string
        assert!(hash.chars().all(|c| c.is_ascii_hexdigit()));
    }
}

#[tokio::test]
async fn test_string_validation() {
    let engine = OperatorEngine::new();
    
    // Test email validation
    let params = r#"{"string": "test@example.com", "type": "email"}"#;
    let result = engine.execute("string_validate", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::Boolean(valid)) = result {
        assert_eq!(valid, true);
    }
    
    // Test URL validation
    let params = r#"{"string": "https://example.com", "type": "url"}"#;
    let result = engine.execute("string_validate", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::Boolean(valid)) = result {
        assert_eq!(valid, true);
    }
} 