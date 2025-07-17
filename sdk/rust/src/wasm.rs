use crate::{parse, serialize, Config, TuskResult};
use wasm_bindgen::prelude::*;
use serde::{Deserialize, Serialize};

#[cfg(feature = "wasm")]
use wasm_bindgen::JsCast;

/// WebAssembly-compatible TuskLang parser
#[wasm_bindgen]
pub struct TuskLangWasm {
    config: Option<Config>,
}

#[wasm_bindgen]
impl TuskLangWasm {
    /// Create a new TuskLang parser instance
    #[wasm_bindgen(constructor)]
    pub fn new() -> Self {
        Self { config: None }
    }

    /// Parse TuskLang string into internal representation
    pub fn parse(&mut self, input: &str) -> Result<(), JsValue> {
        let config = parse(input).map_err(|e| JsValue::from_str(&e.to_string()))?;
        self.config = Some(config);
        Ok(())
    }

    /// Get the parsed configuration as JSON string
    pub fn to_json(&self) -> Result<String, JsValue> {
        let config = self.config.as_ref()
            .ok_or_else(|| JsValue::from_str("No configuration loaded. Call parse() first."))?;
        
        serde_json::to_string_pretty(config)
            .map_err(|e| JsValue::from_str(&e.to_string()))
    }

    /// Get the parsed configuration as TuskLang string
    pub fn to_tsk(&self) -> Result<String, JsValue> {
        let config = self.config.as_ref()
            .ok_or_else(|| JsValue::from_str("No configuration loaded. Call parse() first."))?;
        
        serialize(config).map_err(|e| JsValue::from_str(&e.to_string()))
    }

    /// Get a specific value from the configuration
    pub fn get(&self, key: &str) -> Result<JsValue, JsValue> {
        let config = self.config.as_ref()
            .ok_or_else(|| JsValue::from_str("No configuration loaded. Call parse() first."))?;
        
        if let Some(value) = config.get(key) {
            match value {
                crate::value::Value::String(s) => Ok(JsValue::from_str(s)),
                crate::value::Value::Number(n) => Ok(JsValue::from_f64(*n)),
                crate::value::Value::Boolean(b) => Ok(JsValue::from_bool(*b)),
                crate::value::Value::Null => Ok(JsValue::NULL),
                _ => Ok(JsValue::from_str(&value.to_string())),
            }
        } else {
            Ok(JsValue::UNDEFINED)
        }
    }

    /// Check if a key exists in the configuration
    pub fn has(&self, key: &str) -> bool {
        self.config.as_ref()
            .map(|config| config.contains_key(key))
            .unwrap_or(false)
    }

    /// Get all keys in the configuration
    pub fn keys(&self) -> Result<js_sys::Array, JsValue> {
        let config = self.config.as_ref()
            .ok_or_else(|| JsValue::from_str("No configuration loaded. Call parse() first."))?;
        
        let array = js_sys::Array::new();
        for key in config.keys() {
            array.push(&JsValue::from_str(key));
        }
        Ok(array)
    }

    /// Validate TuskLang syntax
    pub fn validate(input: &str) -> Result<bool, JsValue> {
        match parse(input) {
            Ok(_) => Ok(true),
            Err(_) => Ok(false),
        }
    }

    /// Get validation error details
    pub fn validate_with_error(input: &str) -> Result<ValidationResult, JsValue> {
        match parse(input) {
            Ok(_) => Ok(ValidationResult {
                valid: true,
                error: None,
                line: None,
            }),
            Err(e) => Ok(ValidationResult {
                valid: false,
                error: Some(e.to_string()),
                line: e.line_number(),
            }),
        }
    }
}

/// Result of validation operation
#[derive(Serialize, Deserialize)]
pub struct ValidationResult {
    pub valid: bool,
    pub error: Option<String>,
    pub line: Option<usize>,
}

/// Convert TuskLang to JSON
#[wasm_bindgen]
pub fn tsk_to_json(input: &str) -> Result<String, JsValue> {
    let config = parse(input).map_err(|e| JsValue::from_str(&e.to_string()))?;
    serde_json::to_string_pretty(&config)
        .map_err(|e| JsValue::from_str(&e.to_string()))
}

/// Convert JSON to TuskLang
#[wasm_bindgen]
pub fn json_to_tsk(input: &str) -> Result<String, JsValue> {
    let config: Config = serde_json::from_str(input)
        .map_err(|e| JsValue::from_str(&e.to_string()))?;
    serialize(&config).map_err(|e| JsValue::from_str(&e.to_string()))
}

/// Convert YAML to TuskLang
#[wasm_bindgen]
pub fn yaml_to_tsk(input: &str) -> Result<String, JsValue> {
    let config: Config = serde_yaml::from_str(input)
        .map_err(|e| JsValue::from_str(&e.to_string()))?;
    serialize(&config).map_err(|e| JsValue::from_str(&e.to_string()))
}

/// Parse TuskLang and return as JavaScript object
#[wasm_bindgen]
pub fn parse_to_js(input: &str) -> Result<JsValue, JsValue> {
    let config = parse(input).map_err(|e| JsValue::from_str(&e.to_string()))?;
    JsValue::from_serde(&config)
        .map_err(|e| JsValue::from_str(&e.to_string()))
}

/// Benchmark parsing performance
#[wasm_bindgen]
pub fn benchmark_parse(input: &str, iterations: usize) -> Result<BenchmarkResult, JsValue> {
    let start = web_sys::window()
        .unwrap()
        .performance()
        .unwrap()
        .now();
    
    for _ in 0..iterations {
        parse(input).map_err(|e| JsValue::from_str(&e.to_string()))?;
    }
    
    let end = web_sys::window()
        .unwrap()
        .performance()
        .unwrap()
        .now();
    
    let total_time = end - start;
    let avg_time = total_time / iterations as f64;
    let parses_per_second = 1000.0 / avg_time;
    
    Ok(BenchmarkResult {
        total_time_ms: total_time,
        average_time_ms: avg_time,
        parses_per_second,
        iterations,
    })
}

/// Result of benchmark operation
#[derive(Serialize, Deserialize)]
pub struct BenchmarkResult {
    pub total_time_ms: f64,
    pub average_time_ms: f64,
    pub parses_per_second: f64,
    pub iterations: usize,
}

/// JavaScript console logging for debugging
#[wasm_bindgen]
pub fn log(message: &str) {
    web_sys::console::log_1(&JsValue::from_str(message));
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_wasm_parser() {
        let mut parser = TuskLangWasm::new();
        let input = r#"
app_name: "Test App"
version: "1.0.0"
debug: true
"#;
        
        assert!(parser.parse(input).is_ok());
        assert!(parser.has("app_name"));
        assert!(!parser.has("missing_key"));
    }
} 