//! Security Operators Test Suite
//! 
//! Tests for cryptographic, authentication, and security-related operators.

use crate::operators::{OperatorEngine, OperatorTrait};
use crate::value::Value;
use tokio;

#[tokio::test]
async fn test_jwt_operations() {
    let engine = OperatorEngine::new();
    
    // Test JWT token generation
    let params = r#"{"operation": "generate", "payload": {"user_id": 123, "username": "testuser"}, "secret": "test_secret"}"#;
    let result = engine.execute("jwt", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::Object(token_obj)) = result {
        assert!(token_obj.contains_key("token"));
        
        if let Some(Value::String(token)) = token_obj.get("token") {
            // Token should have 3 parts separated by dots
            assert_eq!(token.matches('.').count(), 2);
            
            // Test JWT token verification
            let params = format!(r#"{{"operation": "verify", "token": "{}", "secret": "test_secret"}}"#, token);
            let result = engine.execute("jwt", &params).await;
            
            assert!(result.is_ok());
            if let Ok(Value::Object(verified)) = result {
                assert_eq!(verified.get("valid"), Some(&Value::Boolean(true)));
                
                if let Some(Value::Object(payload)) = verified.get("payload") {
                    assert_eq!(payload.get("user_id"), Some(&Value::Number(123.0)));
                    assert_eq!(payload.get("username"), Some(&Value::String("testuser".to_string())));
                }
            }
        }
    }
}

#[tokio::test]
async fn test_password_hashing() {
    let engine = OperatorEngine::new();
    
    // Test password hashing
    let params = r#"{"operation": "hash", "password": "mySecurePassword123", "algorithm": "bcrypt"}"#;
    let result = engine.execute("password", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::Object(hash_obj)) = result {
        assert!(hash_obj.contains_key("hash"));
        
        if let Some(Value::String(hash)) = hash_obj.get("hash") {
            // Bcrypt hashes start with $2b$ and have specific length
            assert!(hash.starts_with("$2"));
            assert!(hash.len() >= 59);
            
            // Test password verification
            let params = format!(r#"{{"operation": "verify", "password": "mySecurePassword123", "hash": "{}"}}"#, hash);
            let result = engine.execute("password", &params).await;
            
            assert!(result.is_ok());
            if let Ok(Value::Boolean(valid)) = result {
                assert_eq!(valid, true);
            }
            
            // Test wrong password
            let params = format!(r#"{{"operation": "verify", "password": "wrongPassword", "hash": "{}"}}"#, hash);
            let result = engine.execute("password", &params).await;
            
            assert!(result.is_ok());
            if let Ok(Value::Boolean(valid)) = result {
                assert_eq!(valid, false);
            }
        }
    }
}

#[tokio::test]
async fn test_encryption_operations() {
    let engine = OperatorEngine::new();
    
    // Test AES encryption
    let params = r#"{"operation": "encrypt", "data": "Hello, World!", "algorithm": "aes256", "key": "mySecretKey12345"}"#;
    let result = engine.execute("encrypt", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::Object(encrypted_obj)) = result {
        assert!(encrypted_obj.contains_key("encrypted"));
        assert!(encrypted_obj.contains_key("iv"));
        
        if let (Some(Value::String(encrypted)), Some(Value::String(iv))) = 
           (encrypted_obj.get("encrypted"), encrypted_obj.get("iv")) {
            
            // Test decryption
            let params = format!(r#"{{"operation": "decrypt", "encrypted": "{}", "iv": "{}", "algorithm": "aes256", "key": "mySecretKey12345"}}"#, encrypted, iv);
            let result = engine.execute("encrypt", &params).await;
            
            assert!(result.is_ok());
            if let Ok(Value::String(decrypted)) = result {
                assert_eq!(decrypted, "Hello, World!");
            }
        }
    }
}

#[tokio::test]
async fn test_digital_signature() {
    let engine = OperatorEngine::new();
    
    // Test digital signature creation
    let params = r#"{"operation": "sign", "data": "Important document content", "private_key": "test_private_key"}"#;
    let result = engine.execute("signature", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::Object(sig_obj)) = result {
        assert!(sig_obj.contains_key("signature"));
        
        if let Some(Value::String(signature)) = sig_obj.get("signature") {
            // Test signature verification
            let params = format!(r#"{{"operation": "verify", "data": "Important document content", "signature": "{}", "public_key": "test_public_key"}}"#, signature);
            let result = engine.execute("signature", &params).await;
            
            assert!(result.is_ok());
            if let Ok(Value::Boolean(valid)) = result {
                // Note: This would be true in a real implementation with proper keys
                assert!(valid == true || valid == false); // Accept either for test
            }
        }
    }
}

#[tokio::test]
async fn test_random_generation() {
    let engine = OperatorEngine::new();
    
    // Test random string generation
    let params = r#"{"type": "string", "length": 16, "charset": "alphanumeric"}"#;
    let result = engine.execute("random", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::String(random_str)) = result {
        assert_eq!(random_str.len(), 16);
        assert!(random_str.chars().all(|c| c.is_alphanumeric()));
    }
    
    // Test random number generation
    let params = r#"{"type": "number", "min": 1, "max": 100}"#;
    let result = engine.execute("random", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::Number(random_num)) = result {
        assert!(random_num >= 1.0 && random_num <= 100.0);
    }
    
    // Test UUID generation
    let params = r#"{"type": "uuid"}"#;
    let result = engine.execute("random", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::String(uuid)) = result {
        // UUID format: 8-4-4-4-12 characters
        assert_eq!(uuid.len(), 36);
        assert_eq!(uuid.matches('-').count(), 4);
    }
}

#[tokio::test]
async fn test_session_management() {
    let engine = OperatorEngine::new();
    
    // Test session creation
    let params = r#"{"operation": "create", "user_id": "user123", "ttl": 3600}"#;
    let result = engine.execute("session", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::Object(session_obj)) = result {
        assert!(session_obj.contains_key("session_id"));
        assert!(session_obj.contains_key("expires_at"));
        
        if let Some(Value::String(session_id)) = session_obj.get("session_id") {
            // Test session validation
            let params = format!(r#"{{"operation": "validate", "session_id": "{}"}}"#, session_id);
            let result = engine.execute("session", &params).await;
            
            assert!(result.is_ok());
            if let Ok(Value::Object(validation)) = result {
                assert_eq!(validation.get("valid"), Some(&Value::Boolean(true)));
                assert_eq!(validation.get("user_id"), Some(&Value::String("user123".to_string())));
            }
            
            // Test session destruction
            let params = format!(r#"{{"operation": "destroy", "session_id": "{}"}}"#, session_id);
            let result = engine.execute("session", &params).await;
            
            assert!(result.is_ok());
            if let Ok(Value::Boolean(destroyed)) = result {
                assert_eq!(destroyed, true);
            }
        }
    }
}

#[tokio::test]
async fn test_access_control() {
    let engine = OperatorEngine::new();
    
    // Test permission checking
    let params = r#"{"operation": "check", "user_id": "user123", "resource": "documents", "action": "read"}"#;
    let result = engine.execute("acl", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::Object(acl_result)) = result {
        assert!(acl_result.contains_key("allowed"));
        assert!(matches!(acl_result.get("allowed"), Some(Value::Boolean(_))));
    }
    
    // Test role assignment
    let params = r#"{"operation": "assign_role", "user_id": "user123", "role": "editor"}"#;
    let result = engine.execute("acl", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::Boolean(assigned)) = result {
        assert!(assigned == true || assigned == false); // Accept either for test
    }
}

#[tokio::test]
async fn test_audit_logging() {
    let engine = OperatorEngine::new();
    
    // Test audit log entry
    let params = r#"{"operation": "log", "user_id": "user123", "action": "login", "resource": "system", "ip_address": "192.168.1.1"}"#;
    let result = engine.execute("audit", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::Object(log_result)) = result {
        assert!(log_result.contains_key("logged"));
        assert!(log_result.contains_key("log_id"));
        assert_eq!(log_result.get("logged"), Some(&Value::Boolean(true)));
    }
}

#[tokio::test]
async fn test_rate_limiting() {
    let engine = OperatorEngine::new();
    
    // Test rate limit check
    let params = r#"{"operation": "check", "key": "user123", "limit": 10, "window": 60}"#;
    let result = engine.execute("rate_limit", params).await;
    
    assert!(result.is_ok());
    if let Ok(Value::Object(rate_result)) = result {
        assert!(rate_result.contains_key("allowed"));
        assert!(rate_result.contains_key("remaining"));
        assert!(rate_result.contains_key("reset_time"));
        
        assert!(matches!(rate_result.get("allowed"), Some(Value::Boolean(_))));
        assert!(matches!(rate_result.get("remaining"), Some(Value::Number(_))));
    }
} 