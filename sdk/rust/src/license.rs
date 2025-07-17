//! TuskLang SDK License Validation Module
//! Enterprise-grade license validation for Rust SDK

use serde::{Serialize, Deserialize};
use std::collections::HashMap;
use std::time::{SystemTime, UNIX_EPOCH};
use std::path::{Path, PathBuf};
use std::fs::{self, create_dir_all};
use std::io::{Read, Write};
use uuid::Uuid;
use sha2::{Sha256, Digest};
use hmac::{Hmac, Mac};
use reqwest::Client;
use tokio::time::{Duration, timeout};
use dirs::home_dir;
use log::{info, warn, error};

#[derive(Debug, Serialize, Deserialize)]
pub struct LicenseInfo {
    pub license_key: String,
    pub session_id: String,
    pub validation: ValidationResult,
    pub expiration: ExpirationResult,
    pub cache_status: String,
    pub validation_count: usize,
    pub warnings: usize,
    pub cached_data: Option<serde_json::Value>,
    pub cache_age: Option<u64>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct ValidationResult {
    pub valid: bool,
    pub error: Option<String>,
    pub checksum: Option<String>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct ExpirationResult {
    pub expired: bool,
    pub error: Option<String>,
    pub expiration_date: Option<String>,
    pub days_remaining: Option<i64>,
    pub days_overdue: Option<i64>,
    pub warning: Option<bool>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct ValidationAttempt {
    pub timestamp: u64,
    pub success: bool,
    pub details: String,
    pub session_id: String,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct ExpirationWarning {
    pub timestamp: u64,
    pub days_remaining: i64,
}

#[derive(Debug, Serialize, Deserialize)]
struct OfflineCacheData {
    license_key_hash: String,
    license_data: serde_json::Value,
    timestamp: u64,
    expiration: ExpirationResult,
}

pub struct TuskLicense {
    license_key: String,
    api_key: String,
    session_id: String,
    license_cache: HashMap<String, (serde_json::Value, u64, u64)>, // data, timestamp, expires
    validation_history: Vec<ValidationAttempt>,
    expiration_warnings: Vec<ExpirationWarning>,
    http_client: Client,
    cache_dir: PathBuf,
    cache_file: PathBuf,
    offline_cache: Option<OfflineCacheData>,
}

impl TuskLicense {
    pub fn new(license_key: String, api_key: String) -> Self {
        Self::new_with_cache_dir(license_key, api_key, None)
    }
    
    pub fn new_with_cache_dir(license_key: String, api_key: String, cache_dir: Option<PathBuf>) -> Self {
        // Set up cache directory
        let cache_dir = cache_dir.unwrap_or_else(|| {
            let home = home_dir().unwrap_or_else(|| PathBuf::from("/tmp"));
            home.join(".tusk").join("license_cache")
        });
        
        // Create cache directory if it doesn't exist
        let _ = create_dir_all(&cache_dir);
        
        // Generate cache file name based on license key hash
        let mut hasher = md5::Md5::new();
        hasher.update(license_key.as_bytes());
        let key_hash = format!("{:x}", hasher.finalize());
        let cache_file = cache_dir.join(format!("{}.cache", key_hash));
        
        let mut license = Self {
            license_key,
            api_key,
            session_id: Uuid::new_v4().to_string(),
            license_cache: HashMap::new(),
            validation_history: Vec::new(),
            expiration_warnings: Vec::new(),
            http_client: Client::new(),
            cache_dir,
            cache_file,
            offline_cache: None,
        };
        
        // Load offline cache if exists
        license.load_offline_cache();
        
        license
    }

    pub fn validate_license_key(&self) -> ValidationResult {
        if self.license_key.len() < 32 {
            return ValidationResult {
                valid: false,
                error: Some("Invalid license key format".to_string()),
                checksum: None,
            };
        }

        if !self.license_key.starts_with("TUSK-") {
            return ValidationResult {
                valid: false,
                error: Some("Invalid license key prefix".to_string()),
                checksum: None,
            };
        }

        let mut hasher = Sha256::new();
        hasher.update(self.license_key.as_bytes());
        let checksum = format!("{:x}", hasher.finalize());

        if !checksum.starts_with("tusk") {
            return ValidationResult {
                valid: false,
                error: Some("Invalid license key checksum".to_string()),
                checksum: None,
            };
        }

        ValidationResult {
            valid: true,
            error: None,
            checksum: Some(checksum),
        }
    }

    pub async fn verify_license_server(&mut self, server_url: Option<&str>) -> Result<serde_json::Value, String> {
        let url = server_url.unwrap_or("https://api.tusklang.org/v1/license");
        
        let timestamp = SystemTime::now()
            .duration_since(UNIX_EPOCH)
            .unwrap()
            .as_secs();

        let mut data = serde_json::json!({
            "license_key": self.license_key,
            "session_id": self.session_id,
            "timestamp": timestamp
        });

        // Generate signature
        let mut mac = Hmac::<Sha256>::new_from_slice(self.api_key.as_bytes())
            .expect("HMAC can take key of any size");
        mac.update(serde_json::to_string(&data).unwrap().as_bytes());
        let signature = hex::encode(mac.finalize().into_bytes());
        
        data["signature"] = serde_json::Value::String(signature);

        let timeout_duration = Duration::from_secs(10);
        let response = timeout(
            timeout_duration,
            self.http_client
                .post(url)
                .header("Authorization", format!("Bearer {}", self.api_key))
                .header("Content-Type", "application/json")
                .json(&data)
                .send()
        ).await;
        
        let response = match response {
            Ok(Ok(resp)) => resp,
            Ok(Err(e)) => {
                warn!("Network error during license validation: {}", e);
                return self.fallback_to_offline_cache(&format!("Network error: {}", e));
            },
            Err(_) => {
                warn!("License validation request timeout");
                return self.fallback_to_offline_cache("Request timeout");
            }
        };

        if response.status().is_success() {
            let result: serde_json::Value = match response.json().await {
                Ok(json) => json,
                Err(e) => {
                    warn!("Failed to parse server response: {}", e);
                    return self.fallback_to_offline_cache(&format!("Invalid response format: {}", e));
                }
            };
            
            let expires = SystemTime::now()
                .duration_since(UNIX_EPOCH)
                .unwrap()
                .as_secs() + 3600; // 1 hour cache

            self.license_cache.insert(
                self.license_key.clone(),
                (result.clone(), timestamp, expires)
            );
            
            // Save to offline cache
            self.save_offline_cache(&result);

            Ok(result)
        } else {
            warn!("Server returned error: {}", response.status());
            self.fallback_to_offline_cache(&format!("Server error: {}", response.status()))
        }
    }

    pub fn check_license_expiration(&mut self) -> ExpirationResult {
        let parts: Vec<&str> = self.license_key.split('-').collect();
        if parts.len() < 4 {
            return ExpirationResult {
                expired: true,
                error: Some("Invalid license key format".to_string()),
                expiration_date: None,
                days_remaining: None,
                days_overdue: None,
                warning: None,
            };
        }

        let expiration_str = parts[parts.len() - 1];
        let expiration_timestamp = match u64::from_str_radix(expiration_str, 16) {
            Ok(timestamp) => timestamp,
            Err(_) => {
                return ExpirationResult {
                    expired: true,
                    error: Some("Invalid expiration timestamp".to_string()),
                    expiration_date: None,
                    days_remaining: None,
                    days_overdue: None,
                    warning: None,
                };
            }
        };

        let expiration_date = UNIX_EPOCH + Duration::from_secs(expiration_timestamp);
        let current_time = SystemTime::now();

        if expiration_date < current_time {
            let days_overdue = current_time
                .duration_since(expiration_date)
                .unwrap()
                .as_secs() / 86400;

            return ExpirationResult {
                expired: true,
                error: None,
                expiration_date: Some(expiration_date.to_string()),
                days_remaining: None,
                days_overdue: Some(days_overdue as i64),
                warning: None,
            };
        }

        let days_remaining = expiration_date
            .duration_since(current_time)
            .unwrap()
            .as_secs() / 86400;

        if days_remaining <= 30 {
            self.expiration_warnings.push(ExpirationWarning {
                timestamp: SystemTime::now()
                    .duration_since(UNIX_EPOCH)
                    .unwrap()
                    .as_secs(),
                days_remaining: days_remaining as i64,
            });
        }

        ExpirationResult {
            expired: false,
            error: None,
            expiration_date: Some(expiration_date.to_string()),
            days_remaining: Some(days_remaining as i64),
            days_overdue: None,
            warning: Some(days_remaining <= 30),
        }
    }

    pub fn validate_license_permissions(&self, feature: &str) -> Result<bool, String> {
        if let Some((data, _, expires)) = self.license_cache.get(&self.license_key) {
            let current_time = SystemTime::now()
                .duration_since(UNIX_EPOCH)
                .unwrap()
                .as_secs();

            if current_time < *expires {
                if let Some(features) = data.get("features") {
                    if let Some(features_array) = features.as_array() {
                        if features_array.iter().any(|f| f.as_str() == Some(feature)) {
                            return Ok(true);
                        } else {
                            return Err("Feature not licensed".to_string());
                        }
                    }
                }
            }
        }

        // Fallback validation
        match feature {
            "basic" | "core" | "standard" => Ok(true),
            "premium" | "enterprise" => {
                if self.license_key.to_uppercase().contains("PREMIUM") ||
                   self.license_key.to_uppercase().contains("ENTERPRISE") {
                    Ok(true)
                } else {
                    Err("Premium license required".to_string())
                }
            }
            _ => Err("Unknown feature".to_string()),
        }
    }

    pub fn get_license_info(&mut self) -> LicenseInfo {
        let validation_result = self.validate_license_key();
        let expiration_result = self.check_license_expiration();

        let mut info = LicenseInfo {
            license_key: format!("{}...{}", 
                &self.license_key[..8.min(self.license_key.len())],
                &self.license_key[self.license_key.len().saturating_sub(4)..]
            ),
            session_id: self.session_id.clone(),
            validation: validation_result,
            expiration: expiration_result,
            cache_status: if self.license_cache.contains_key(&self.license_key) {
                "cached".to_string()
            } else {
                "not_cached".to_string()
            },
            validation_count: self.validation_history.len(),
            warnings: self.expiration_warnings.len(),
            cached_data: None,
            cache_age: None,
        };

        if let Some((data, timestamp, _)) = self.license_cache.get(&self.license_key) {
            info.cached_data = Some(data.clone());
            info.cache_age = Some(
                SystemTime::now()
                    .duration_since(UNIX_EPOCH)
                    .unwrap()
                    .as_secs() - timestamp
            );
        }

        info
    }

    pub fn log_validation_attempt(&mut self, success: bool, details: &str) {
        self.validation_history.push(ValidationAttempt {
            timestamp: SystemTime::now()
                .duration_since(UNIX_EPOCH)
                .unwrap()
                .as_secs(),
            success,
            details: details.to_string(),
            session_id: self.session_id.clone(),
        });
    }

    pub fn get_validation_history(&self) -> &[ValidationAttempt] {
        &self.validation_history
    }

    pub fn clear_validation_history(&mut self) {
        self.validation_history.clear();
    }
    
    fn load_offline_cache(&mut self) {
        match fs::read_to_string(&self.cache_file) {
            Ok(content) => {
                match serde_json::from_str::<OfflineCacheData>(&content) {
                    Ok(cached_data) => {
                        // Verify the cache is for the correct license key
                        let mut hasher = Sha256::new();
                        hasher.update(self.license_key.as_bytes());
                        let key_hash = format!("{:x}", hasher.finalize());
                        
                        if cached_data.license_key_hash == key_hash {
                            self.offline_cache = Some(cached_data);
                            info!("Loaded offline license cache");
                        } else {
                            warn!("Offline cache key mismatch");
                            self.offline_cache = None;
                        }
                    },
                    Err(e) => {
                        error!("Failed to parse offline cache: {}", e);
                        self.offline_cache = None;
                    }
                }
            },
            Err(_) => {
                // Cache file doesn't exist
                self.offline_cache = None;
            }
        }
    }
    
    fn save_offline_cache(&mut self, license_data: &serde_json::Value) {
        let mut hasher = Sha256::new();
        hasher.update(self.license_key.as_bytes());
        let key_hash = format!("{:x}", hasher.finalize());
        
        let cache_data = OfflineCacheData {
            license_key_hash: key_hash,
            license_data: license_data.clone(),
            timestamp: SystemTime::now()
                .duration_since(UNIX_EPOCH)
                .unwrap()
                .as_secs(),
            expiration: self.check_license_expiration(),
        };
        
        match serde_json::to_string_pretty(&cache_data) {
            Ok(json) => {
                match fs::write(&self.cache_file, json) {
                    Ok(_) => {
                        self.offline_cache = Some(cache_data);
                        info!("Saved license data to offline cache");
                    },
                    Err(e) => {
                        error!("Failed to save offline cache: {}", e);
                    }
                }
            },
            Err(e) => {
                error!("Failed to serialize cache data: {}", e);
            }
        }
    }
    
    fn fallback_to_offline_cache(&self, error_msg: &str) -> Result<serde_json::Value, String> {
        if let Some(ref cache) = self.offline_cache {
            let cache_age = SystemTime::now()
                .duration_since(UNIX_EPOCH)
                .unwrap()
                .as_secs() - cache.timestamp;
            let cache_age_days = cache_age as f64 / 86400.0;
            
            // Check if cached license is not expired
            if !cache.expiration.expired {
                warn!("Using offline license cache (age: {:.1} days)", cache_age_days);
                let mut result = cache.license_data.clone();
                if let Some(obj) = result.as_object_mut() {
                    obj.insert("offline_mode".to_string(), serde_json::Value::Bool(true));
                    obj.insert("cache_age_days".to_string(), serde_json::Value::Number(
                        serde_json::Number::from_f64(cache_age_days).unwrap()
                    ));
                    obj.insert("warning".to_string(), serde_json::Value::String(
                        format!("Operating in offline mode due to: {}", error_msg)
                    ));
                }
                Ok(result)
            } else {
                Err(format!("License expired and server unreachable: {}", error_msg))
            }
        } else {
            Err(format!("No offline cache available: {}", error_msg))
        }
    }
}

// Global license instance
use std::sync::Mutex;
use once_cell::sync::Lazy;

static LICENSE_INSTANCE: Lazy<Mutex<Option<TuskLicense>>> = Lazy::new(|| Mutex::new(None));

pub fn initialize_license(license_key: String, api_key: String) -> TuskLicense {
    initialize_license_with_cache_dir(license_key, api_key, None)
}

pub fn initialize_license_with_cache_dir(license_key: String, api_key: String, cache_dir: Option<PathBuf>) -> TuskLicense {
    let license = TuskLicense::new_with_cache_dir(license_key, api_key, cache_dir);
    let mut instance = LICENSE_INSTANCE.lock().unwrap();
    *instance = Some(license.clone());
    license
}

pub fn get_license() -> TuskLicense {
    let instance = LICENSE_INSTANCE.lock().unwrap();
    instance.as_ref()
        .cloned()
        .expect("License not initialized. Call initialize_license() first.")
}

impl Clone for TuskLicense {
    fn clone(&self) -> Self {
        Self {
            license_key: self.license_key.clone(),
            api_key: self.api_key.clone(),
            session_id: self.session_id.clone(),
            license_cache: self.license_cache.clone(),
            validation_history: self.validation_history.clone(),
            expiration_warnings: self.expiration_warnings.clone(),
            http_client: Client::new(),
        }
    }
} 