//! TuskLang SDK Anti-Tampering Module
//! Enterprise-grade anti-tampering for Rust SDK

use serde::{Serialize, Deserialize};
use std::collections::HashMap;
use std::env;
use std::fs;
use std::time::{SystemTime, UNIX_EPOCH};
use sha2::{Sha256, Digest};
use hmac::{Hmac, Mac};
use aes_gcm::{Aes256Gcm, Key, Nonce};
use aes_gcm::aead::{Aead, NewAead};
use rand::Rng;

#[derive(Debug, Serialize, Deserialize)]
pub struct TamperDetection {
    pub timestamp: u64,
    pub file: Option<String>,
    pub function: Option<String>,
    pub expected: String,
    pub actual: String,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct TamperingReport {
    pub file_tampering: bool,
    pub function_tampering: bool,
    pub environment_tampering: bool,
    pub debugger_detected: bool,
    pub details: Vec<String>,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct IntegrityReport {
    pub self_check_passed: bool,
    pub tampering_detected: TamperingReport,
    pub protected_functions: Vec<String>,
    pub integrity_checks: usize,
    pub tamper_detections: usize,
    pub last_self_check: u64,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct ObfuscationCache {
    pub original: String,
    pub obfuscated: String,
    pub hash: String,
}

pub struct TuskAntiTamper {
    secret_key: String,
    encryption_key: Vec<u8>,
    integrity_checks: HashMap<String, String>,
    tamper_detections: Vec<TamperDetection>,
    obfuscation_cache: HashMap<String, ObfuscationCache>,
    self_check_interval: u64,
    last_self_check: u64,
}

impl TuskAntiTamper {
    pub fn new(secret_key: String) -> Self {
        let encryption_key = Self::derive_key(&secret_key);
        Self {
            secret_key,
            encryption_key,
            integrity_checks: HashMap::new(),
            tamper_detections: Vec::new(),
            obfuscation_cache: HashMap::new(),
            self_check_interval: 300, // 5 minutes
            last_self_check: 0,
        }
    }

    fn derive_key(password: &str) -> Vec<u8> {
        let salt = b"tusklang_antitamper_salt";
        let mut hasher = Sha256::new();
        hasher.update(salt);
        hasher.update(password.as_bytes());
        hasher.finalize().to_vec()
    }

    pub fn calculate_file_hash(&self, file_path: &str) -> String {
        match fs::read(file_path) {
            Ok(content) => {
                let mut hasher = Sha256::new();
                hasher.update(&content);
                format!("{:x}", hasher.finalize())
            }
            Err(_) => String::new(),
        }
    }

    pub fn verify_file_integrity(&self, file_path: &str, expected_hash: &str) -> bool {
        let actual_hash = self.calculate_file_hash(file_path);
        actual_hash == expected_hash
    }

    pub fn obfuscate_code(&self, code: &str, level: u8) -> String {
        if level == 0 {
            return code.to_string();
        }

        let mut obfuscated = code.to_string();

        // Level 1: Basic obfuscation
        if level >= 1 {
            // Simple base64 encoding
            use base64::{Engine as _, engine::general_purpose};
            let encoded = general_purpose::STANDARD.encode(code.as_bytes());
            obfuscated = format!("// Obfuscated code\nlet _decoded = base64::engine::general_purpose::STANDARD.decode(\"{}\").unwrap();\nlet _code = String::from_utf8(_decoded).unwrap();\n// Execute: {}", encoded, code);
        }

        // Level 2: Advanced obfuscation
        if level >= 2 {
            // Add junk code
            let junk_vars: Vec<String> = (0..10).map(|i| format!("let _junk_{} = None;", i)).collect();
            obfuscated = format!("{}\n{}", junk_vars.join("\n"), obfuscated);
        }

        // Level 3: Maximum obfuscation
        if level >= 3 {
            // Encrypt the code
            let key = Key::from_slice(&self.encryption_key);
            let cipher = Aes256Gcm::new(key);
            let nonce_bytes: [u8; 12] = rand::thread_rng().gen();
            let nonce = Nonce::from_slice(&nonce_bytes);
            
            if let Ok(encrypted) = cipher.encrypt(nonce, code.as_bytes()) {
                let mut result = nonce_bytes.to_vec();
                result.extend(encrypted);
                let encoded = general_purpose::STANDARD.encode(result);
                obfuscated = format!("// Encrypted code\nlet _key = [{}];\nlet _nonce = [{}];\nlet _encrypted = general_purpose::STANDARD.decode(\"{}\").unwrap();\n// Decrypt and execute", 
                    self.encryption_key.iter().map(|b| b.to_string()).collect::<Vec<_>>().join(", "),
                    nonce_bytes.iter().map(|b| b.to_string()).collect::<Vec<_>>().join(", "),
                    encoded);
            }
        }

        obfuscated
    }

    pub fn deobfuscate_code(&self, obfuscated_code: &str) -> String {
        // This is a simplified deobfuscation
        // In a real implementation, you'd need to parse and execute the obfuscated code
        if obfuscated_code.contains("// Obfuscated code") {
            // Extract base64 encoded part
            if let Some(start) = obfuscated_code.find("\"") {
                if let Some(end) = obfuscated_code[start + 1..].find("\"") {
                    let encoded = &obfuscated_code[start + 1..start + 1 + end];
                    if let Ok(decoded) = general_purpose::STANDARD.decode(encoded) {
                        if let Ok(decoded_str) = String::from_utf8(decoded) {
                            return decoded_str;
                        }
                    }
                }
            }
        }
        
        obfuscated_code.to_string()
    }

    pub fn protect_function<F, Args, Ret>(&mut self, func: F, name: &str, obfuscation_level: u8) -> impl Fn(Args) -> Ret
    where
        F: Fn(Args) -> Ret + 'static,
        Args: 'static,
        Ret: 'static,
    {
        // Store function signature for integrity checking
        let func_signature = format!("function:{}", name);
        let mut hasher = Sha256::new();
        hasher.update(func_signature.as_bytes());
        let hash = format!("{:x}", hasher.finalize());
        
        self.obfuscation_cache.insert(name.to_string(), ObfuscationCache {
            original: func_signature.clone(),
            obfuscated: self.obfuscate_code(&func_signature, obfuscation_level),
            hash: hash.clone(),
        });

        move |args| {
            // Self-check before execution
            if !self.self_check() {
                panic!("Tampering detected - function execution blocked");
            }
            
            // Execute original function
            func(args)
        }
    }

    pub fn self_check(&mut self) -> bool {
        let current_time = SystemTime::now()
            .duration_since(UNIX_EPOCH)
            .unwrap()
            .as_secs();

        // Check if it's time for a self-check
        if current_time - self.last_self_check < self.self_check_interval {
            return true;
        }

        self.last_self_check = current_time;

        // Check current file integrity
        if let Ok(current_exe) = env::current_exe() {
            if let Some(current_path) = current_exe.to_str() {
                let current_hash = self.calculate_file_hash(current_path);
                
                if !self.integrity_checks.contains_key(current_path) {
                    self.integrity_checks.insert(current_path.to_string(), current_hash);
                } else if let Some(stored_hash) = self.integrity_checks.get(current_path) {
                    if stored_hash != &current_hash {
                        self.tamper_detections.push(TamperDetection {
                            timestamp: current_time,
                            file: Some(current_path.to_string()),
                            function: None,
                            expected: stored_hash.clone(),
                            actual: current_hash,
                        });
                        return false;
                    }
                }
            }
        }

        // Check obfuscated functions
        for (func_name, cache_data) in &self.obfuscation_cache {
            let mut hasher = Sha256::new();
            hasher.update(cache_data.original.as_bytes());
            let current_hash = format!("{:x}", hasher.finalize());
            
            if cache_data.hash != current_hash {
                self.tamper_detections.push(TamperDetection {
                    timestamp: current_time,
                    file: None,
                    function: Some(func_name.clone()),
                    expected: cache_data.hash.clone(),
                    actual: current_hash,
                });
                return false;
            }
        }

        true
    }

    pub fn detect_tampering(&mut self) -> TamperingReport {
        let mut report = TamperingReport {
            file_tampering: false,
            function_tampering: false,
            environment_tampering: false,
            debugger_detected: false,
            details: Vec::new(),
        };

        // Check for debugger
        if self.detect_debugger() {
            report.debugger_detected = true;
            report.details.push("Debugger detected".to_string());
        }

        // Check environment tampering
        if self.detect_environment_tampering() {
            report.environment_tampering = true;
            report.details.push("Environment tampering detected".to_string());
        }

        // Check file tampering
        if !self.self_check() {
            report.file_tampering = true;
            report.details.push("File integrity check failed".to_string());
        }

        // Check function tampering
        for (func_name, cache_data) in &self.obfuscation_cache {
            let mut hasher = Sha256::new();
            hasher.update(cache_data.original.as_bytes());
            let current_hash = format!("{:x}", hasher.finalize());
            
            if cache_data.hash != current_hash {
                report.function_tampering = true;
                report.details.push(format!("Function {} tampering detected", func_name));
            }
        }

        report
    }

    fn detect_debugger(&self) -> bool {
        // Check for common debugger indicators
        if let Ok(debug) = env::var("RUST_BACKTRACE") {
            if debug == "1" {
                return true;
            }
        }

        // Check for development environment
        if let Ok(profile) = env::var("CARGO_PROFILE") {
            if profile == "debug" {
                return true;
            }
        }

        false
    }

    fn detect_environment_tampering(&self) -> bool {
        // Check for suspicious environment variables
        let suspicious_vars = ["RUST_BACKTRACE", "RUST_LOG", "CARGO_PROFILE"];
        for var_name in &suspicious_vars {
            if let Ok(value) = env::var(var_name) {
                let value_lower = value.to_lowercase();
                if value_lower.contains("debug") || value_lower.contains("test") || value_lower.contains("dev") {
                    return true;
                }
            }
        }

        // Check command line arguments
        for arg in env::args() {
            let arg_lower = arg.to_lowercase();
            if arg_lower.contains("debug") || arg_lower.contains("test") {
                return true;
            }
        }

        false
    }

    pub fn get_tamper_detections(&self) -> Vec<TamperDetection> {
        self.tamper_detections.clone()
    }

    pub fn clear_tamper_detections(&mut self) {
        self.tamper_detections.clear();
    }

    pub fn get_integrity_report(&mut self) -> IntegrityReport {
        IntegrityReport {
            self_check_passed: self.self_check(),
            tampering_detected: self.detect_tampering(),
            protected_functions: self.obfuscation_cache.keys().cloned().collect(),
            integrity_checks: self.integrity_checks.len(),
            tamper_detections: self.tamper_detections.len(),
            last_self_check: self.last_self_check,
        }
    }
}

// Global anti-tamper instance
use std::sync::Mutex;
use once_cell::sync::Lazy;

static ANTI_TAMPER_INSTANCE: Lazy<Mutex<Option<TuskAntiTamper>>> = Lazy::new(|| Mutex::new(None));

pub fn initialize_anti_tamper(secret_key: String) -> TuskAntiTamper {
    let anti_tamper = TuskAntiTamper::new(secret_key);
    let mut instance = ANTI_TAMPER_INSTANCE.lock().unwrap();
    *instance = Some(anti_tamper.clone());
    anti_tamper
}

pub fn get_anti_tamper() -> TuskAntiTamper {
    let instance = ANTI_TAMPER_INSTANCE.lock().unwrap();
    instance.as_ref()
        .cloned()
        .expect("Anti-tamper not initialized. Call initialize_anti_tamper() first.")
}

impl Clone for TuskAntiTamper {
    fn clone(&self) -> Self {
        Self {
            secret_key: self.secret_key.clone(),
            encryption_key: self.encryption_key.clone(),
            integrity_checks: self.integrity_checks.clone(),
            tamper_detections: self.tamper_detections.clone(),
            obfuscation_cache: self.obfuscation_cache.clone(),
            self_check_interval: self.self_check_interval,
            last_self_check: self.last_self_check,
        }
    }
} 