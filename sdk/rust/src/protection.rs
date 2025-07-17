//! TuskLang SDK Protection Core Module
//! Enterprise-grade protection for Rust SDK

use sha2::{Sha256, Digest};
use hmac::{Hmac, Mac, MacMarker};
use aes_gcm::{Aes256Gcm, Key, Nonce, KeyInit};
use aes_gcm::aead::Aead;
use rand::Rng;
use serde::{Serialize, Deserialize};
use std::collections::HashMap;
use std::time::{SystemTime, UNIX_EPOCH};
use uuid::Uuid;

#[derive(Debug, Serialize, Deserialize)]
pub struct UsageMetrics {
    pub start_time: u64,
    pub api_calls: u64,
    pub errors: u64,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct Violation {
    pub timestamp: u64,
    pub session_id: String,
    pub violation_type: String,
    pub details: String,
    pub license_key_partial: String,
}

pub struct TuskProtection {
    license_key: String,
    api_key: String,
    session_id: String,
    encryption_key: Vec<u8>,
    integrity_checks: HashMap<String, String>,
    usage_metrics: UsageMetrics,
}

impl TuskProtection {
    pub fn new(license_key: String, api_key: String) -> Self {
        let session_id = Uuid::new_v4().to_string();
        let encryption_key = Self::derive_key(&license_key);
        let start_time = SystemTime::now()
            .duration_since(UNIX_EPOCH)
            .unwrap()
            .as_secs();
        
        Self {
            license_key,
            api_key,
            session_id,
            encryption_key,
            integrity_checks: HashMap::new(),
            usage_metrics: UsageMetrics {
                start_time,
                api_calls: 0,
                errors: 0,
            },
        }
    }

    fn derive_key(password: &str) -> Vec<u8> {
        let salt = b"tusklang_protection_salt";
        let mut hasher = Sha256::new();
        hasher.update(salt);
        hasher.update(password.as_bytes());
        hasher.finalize().to_vec()
    }

    pub fn validate_license(&self) -> bool {
        if self.license_key.len() < 32 {
            return false;
        }
        
        let mut hasher = Sha256::new();
        hasher.update(self.license_key.as_bytes());
        let checksum = format!("{:x}", hasher.finalize());
        checksum.starts_with("tusk")
    }

    pub fn encrypt_data(&self, data: &str) -> String {
        let key = Key::<Aes256Gcm>::from_slice(&self.encryption_key);
        let cipher = Aes256Gcm::new(key);
        let nonce_bytes: [u8; 12] = rand::thread_rng().gen();
        let nonce = Nonce::from_slice(&nonce_bytes);
        
        match cipher.encrypt(nonce, data.as_bytes()) {
            Ok(encrypted) => {
                let mut result = nonce_bytes.to_vec();
                result.extend(encrypted);
                base64::encode(result)
            }
            Err(_) => data.to_string(),
        }
    }

    pub fn decrypt_data(&self, encrypted_data: &str) -> String {
        let key = Key::<Aes256Gcm>::from_slice(&self.encryption_key);
        let cipher = Aes256Gcm::new(key);
        
        match base64::decode(encrypted_data) {
            Ok(decoded) => {
                if decoded.len() < 12 {
                    return encrypted_data.to_string();
                }
                
                let nonce = Nonce::from_slice(&decoded[..12]);
                match cipher.decrypt(nonce, &decoded[12..]) {
                    Ok(decrypted) => String::from_utf8(decrypted).unwrap_or_else(|_| encrypted_data.to_string()),
                    Err(_) => encrypted_data.to_string(),
                }
            }
            Err(_) => encrypted_data.to_string(),
        }
    }

    pub fn verify_integrity(&self, data: &str, signature: &str) -> bool {
        let mut mac = Hmac::<Sha256>::new_from_slice(self.api_key.as_bytes())
            .expect("HMAC can take key of any size");
        Mac::update(&mut mac, data.as_bytes());
        
        match hex::decode(signature) {
            Ok(signature_bytes) => mac.verify_slice(&signature_bytes).is_ok(),
            Err(_) => false,
        }
    }

    pub fn generate_signature(&self, data: &str) -> String {
        let mut mac = Hmac::<Sha256>::new_from_slice(self.api_key.as_bytes())
            .expect("HMAC can take key of any size");
        Mac::update(&mut mac, data.as_bytes());
        hex::encode(mac.finalize().into_bytes())
    }

    pub fn track_usage(&mut self, operation: &str, success: bool) {
        self.usage_metrics.api_calls += 1;
        if !success {
            self.usage_metrics.errors += 1;
        }
    }

    pub fn get_metrics(&self) -> serde_json::Value {
        let uptime = SystemTime::now()
            .duration_since(UNIX_EPOCH)
            .unwrap()
            .as_secs() - self.usage_metrics.start_time;
        
        serde_json::json!({
            "start_time": self.usage_metrics.start_time,
            "api_calls": self.usage_metrics.api_calls,
            "errors": self.usage_metrics.errors,
            "session_id": self.session_id,
            "uptime": uptime
        })
    }

    pub fn obfuscate_code(&self, code: &str) -> String {
        base64::encode(code.as_bytes())
    }

    pub fn detect_tampering(&mut self) -> bool {
        // In a real implementation, check file integrity
        // For now, return true as placeholder
        true
    }

    pub fn report_violation(&self, violation_type: &str, details: &str) -> Violation {
        let timestamp = SystemTime::now()
            .duration_since(UNIX_EPOCH)
            .unwrap()
            .as_secs();
        
        let violation = Violation {
            timestamp,
            session_id: self.session_id.clone(),
            violation_type: violation_type.to_string(),
            details: details.to_string(),
            license_key_partial: format!("{}...", &self.license_key[..8.min(self.license_key.len())]),
        };
        
        println!("SECURITY VIOLATION: {:?}", violation);
        violation
    }
}

// Global protection instance
use std::sync::Mutex;
use once_cell::sync::Lazy;

static PROTECTION_INSTANCE: Lazy<Mutex<Option<TuskProtection>>> = Lazy::new(|| Mutex::new(None));

pub fn initialize_protection(license_key: String, api_key: String) -> TuskProtection {
    let protection = TuskProtection::new(license_key, api_key);
    let mut instance = PROTECTION_INSTANCE.lock().unwrap();
    *instance = Some(protection.clone());
    protection
}

pub fn get_protection() -> TuskProtection {
    let instance = PROTECTION_INSTANCE.lock().unwrap();
    instance.as_ref()
        .cloned()
        .expect("Protection not initialized. Call initialize_protection() first.")
}

impl Clone for TuskProtection {
    fn clone(&self) -> Self {
        Self {
            license_key: self.license_key.clone(),
            api_key: self.api_key.clone(),
            session_id: self.session_id.clone(),
            encryption_key: self.encryption_key.clone(),
            integrity_checks: self.integrity_checks.clone(),
            usage_metrics: UsageMetrics {
                start_time: self.usage_metrics.start_time,
                api_calls: self.usage_metrics.api_calls,
                errors: self.usage_metrics.errors,
            },
        }
    }
} 