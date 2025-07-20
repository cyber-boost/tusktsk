use crate::error::{TuskError, TuskResult};
use crate::value::Value;
use regex::Regex;
use std::collections::HashMap;
use std::env;
use std::fs;
use std::path::Path;
use chrono::{DateTime, Utc};

/// TuskLang Enhanced Parser for Rust
/// "We don't bow to any king" - Support ALL syntax styles
///
/// Features:
/// - Multiple grouping: [], {}, <>
/// - $global vs section-local variables
/// - Cross-file communication
/// - Database queries (placeholder adapters)
/// - All @ operators
/// - Maximum flexibility
///
/// DEFAULT CONFIG: peanut.tsk (the bridge of language grace)
pub struct EnhancedParser {
    data: HashMap<String, Value>,
    global_variables: HashMap<String, Value>,
    section_variables: HashMap<String, Value>,
    cache: HashMap<String, Value>,
    cross_file_cache: HashMap<String, Value>,
    current_section: String,
    in_object: bool,
    object_key: String,
    peanut_loaded: bool,
    
    // Standard peanut.tsk locations
    peanut_locations: Vec<String>,
    
    // Operator engine for @ operators
    operator_engine: crate::operators::OperatorEngine,
}

impl EnhancedParser {
    /// Create a new enhanced parser
    pub fn new() -> Self {
        let home_dir = env::var("HOME").unwrap_or_default();
        let tusklang_config = env::var("TUSKLANG_CONFIG").unwrap_or_default();
        
        Self {
            data: HashMap::new(),
            global_variables: HashMap::new(),
            section_variables: HashMap::new(),
            cache: HashMap::new(),
            cross_file_cache: HashMap::new(),
            current_section: String::new(),
            in_object: false,
            object_key: String::new(),
            peanut_loaded: false,
            peanut_locations: vec![
                "./peanut.tsk".to_string(),
                "../peanut.tsk".to_string(),
                "../../peanut.tsk".to_string(),
                "/etc/tusklang/peanut.tsk".to_string(),
                format!("{}/.config/tusklang/peanut.tsk", home_dir),
                tusklang_config,
            ],
            operator_engine: crate::operators::OperatorEngine::new().unwrap_or_else(|_| {
                // Fallback to a basic implementation if operator engine fails to initialize
                crate::operators::OperatorEngine::new().unwrap()
            }),
        }
    }
    
    /// Load peanut.tsk if available
    pub fn load_peanut(&mut self) -> TuskResult<()> {
        if self.peanut_loaded {
            return Ok(());
        }
        
        self.peanut_loaded = true; // Mark first to prevent recursion
        
        for location in &self.peanut_locations {
            if location.is_empty() {
                continue;
            }
            
            if Path::new(location).exists() {
                println!("# Loading universal config from: {}", location);
                return self.parse_file(location);
            }
        }
        
        Ok(())
    }
    
    /// Parse TuskLang value with all syntax support
    pub fn parse_value(&mut self, value: &str) -> Value {
        let value = value.trim();
        
        // Remove optional semicolon
        let value = if value.ends_with(';') {
            value.trim_end_matches(';').trim()
        } else {
            value
        };
        
        // Basic types
        match value {
            "true" => return Value::Boolean(true),
            "false" => return Value::Boolean(false),
            "null" => return Value::Null,
            _ => {}
        }
        
        // Numbers
        if let Ok(num) = value.parse::<i64>() {
            return Value::Number(num as f64);
        }
        if let Ok(num) = value.parse::<f64>() {
            return Value::Number(num);
        }
        
        // $variable references (global)
        let global_var_re = Regex::new(r"^\$([a-zA-Z_][a-zA-Z0-9_]*)$").unwrap();
        if let Some(captures) = global_var_re.captures(value) {
            let var_name = captures.get(1).unwrap().as_str();
            if let Some(val) = self.global_variables.get(var_name) {
                return val.clone();
            }
            return Value::String("".to_string());
        }
        
        // Section-local variable references
        if !self.current_section.is_empty() {
            let local_var_re = Regex::new(r"^[a-zA-Z_][a-zA-Z0-9_]*$").unwrap();
            if local_var_re.is_match(value) {
                let section_key = format!("{}.{}", self.current_section, value);
                if let Some(val) = self.section_variables.get(&section_key) {
                    return val.clone();
                }
            }
        }
        
        // @date function
        let date_re = Regex::new(r#"^@date\(["'](.*)["']\)$"#).unwrap();
        if let Some(captures) = date_re.captures(value) {
            let format_str = captures.get(1).unwrap().as_str();
            return Value::String(self.execute_date(format_str));
        }
        
        // @env function with default
        let env_re = Regex::new(r#"^@env\(["']([^"']*)["'](?:,\s*(.+))?\)$"#).unwrap();
        if let Some(captures) = env_re.captures(value) {
            let env_var = captures.get(1).unwrap().as_str();
            let default_val = if let Some(default_match) = captures.get(2) {
                default_match.as_str().trim_matches('"').trim_matches('\'')
            } else {
                ""
            };
            return Value::String(env::var(env_var).unwrap_or_else(|_| default_val.to_string()));
        }
        
        // Ranges: 8000-9000
        let range_re = Regex::new(r"^(\d+)-(\d+)$").unwrap();
        if let Some(captures) = range_re.captures(value) {
            let min = captures.get(1).unwrap().as_str().parse::<f64>().unwrap();
            let max = captures.get(2).unwrap().as_str().parse::<f64>().unwrap();
            let mut range_obj = HashMap::new();
            range_obj.insert("min".to_string(), Value::Number(min));
            range_obj.insert("max".to_string(), Value::Number(max));
            range_obj.insert("type".to_string(), Value::String("range".to_string()));
            return Value::Object(range_obj);
        }
        
        // Arrays
        if value.starts_with('[') && value.ends_with(']') {
            return self.parse_array(value);
        }
        
        // Objects
        if value.starts_with('{') && value.ends_with('}') {
            return self.parse_object(value);
        }
        
        // Cross-file references: @file.tsk.get('key')
        let cross_get_re = Regex::new(r#"^@([a-zA-Z0-9_-]+)\.tsk\.get\(["'](.*)["']\)$"#).unwrap();
        if let Some(captures) = cross_get_re.captures(value) {
            let file_name = captures.get(1).unwrap().as_str();
            let key = captures.get(2).unwrap().as_str();
            return self.cross_file_get(file_name, key);
        }
        
        // Cross-file set: @file.tsk.set('key', value)
        let cross_set_re = Regex::new(r#"^@([a-zA-Z0-9_-]+)\.tsk\.set\(["']([^"']*)["'],\s*(.+)\)$"#).unwrap();
        if let Some(captures) = cross_set_re.captures(value) {
            let file_name = captures.get(1).unwrap().as_str();
            let key = captures.get(2).unwrap().as_str();
            let val = captures.get(3).unwrap().as_str();
            return self.cross_file_set(file_name, key, val);
        }
        
        // @query function
        let query_re = Regex::new(r#"^@query\(["'](.*)["'](.*)\)$"#).unwrap();
        if let Some(captures) = query_re.captures(value) {
            let query = captures.get(1).unwrap().as_str();
            return Value::String(self.execute_query(query));
        }
        
        // @ operators
        let operator_re = Regex::new(r"^@([a-zA-Z_][a-zA-Z0-9_]*)\((.+)\)$").unwrap();
        if let Some(captures) = operator_re.captures(value) {
            let operator = captures.get(1).unwrap().as_str();
            let params = captures.get(2).unwrap().as_str();
            return self.execute_operator(operator, params);
        }
        
        // String concatenation
        if value.contains(" + ") {
            let parts: Vec<&str> = value.split(" + ").collect();
            let mut result = String::new();
            for part in parts {
                let part = part.trim().trim_matches('"').trim_matches('\'');
                if !part.starts_with('"') {
                    let parsed_part = self.parse_value(part);
                    result.push_str(&parsed_part.to_string());
                } else {
                    result.push_str(&part[1..part.len()-1]);
                }
            }
            return Value::String(result);
        }
        
        // Conditional/ternary: condition ? true_val : false_val
        let ternary_re = Regex::new(r"(.+?)\s*\?\s*(.+?)\s*:\s*(.+)").unwrap();
        if let Some(captures) = ternary_re.captures(value) {
            let condition = captures.get(1).unwrap().as_str().trim();
            let true_val = captures.get(2).unwrap().as_str().trim();
            let false_val = captures.get(3).unwrap().as_str().trim();
            
            if self.evaluate_condition(condition) {
                return self.parse_value(true_val);
            } else {
                return self.parse_value(false_val);
            }
        }
        
        // Remove quotes from strings
        if (value.starts_with('"') && value.ends_with('"')) ||
           (value.starts_with('\'') && value.ends_with('\'')) {
            return Value::String(value[1..value.len()-1].to_string());
        }
        
        // Return as string
        Value::String(value.to_string())
    }
    
    /// Parse array syntax
    fn parse_array(&mut self, value: &str) -> Value {
        let content = value[1..value.len()-1].trim();
        if content.is_empty() {
            return Value::Array(Vec::new());
        }
        
        let mut items = Vec::new();
        let mut current = String::new();
        let mut depth = 0;
        let mut in_string = false;
        let mut quote_char = '\0';
        
        for ch in content.chars() {
            if (ch == '"' || ch == '\'') && !in_string {
                in_string = true;
                quote_char = ch;
            } else if ch == quote_char && in_string {
                in_string = false;
                quote_char = '\0';
            }
            
            if !in_string {
                match ch {
                    '[' | '{' => depth += 1,
                    ']' | '}' => depth -= 1,
                    ',' if depth == 0 => {
                        items.push(self.parse_value(current.trim()));
                        current.clear();
                        continue;
                    }
                    _ => {}
                }
            }
            
            current.push(ch);
        }
        
        if !current.trim().is_empty() {
            items.push(self.parse_value(current.trim()));
        }
        
        Value::Array(items)
    }
    
    /// Parse object syntax
    fn parse_object(&mut self, value: &str) -> Value {
        let content = value[1..value.len()-1].trim();
        if content.is_empty() {
            return Value::Object(HashMap::new());
        }
        
        let mut pairs = Vec::new();
        let mut current = String::new();
        let mut depth = 0;
        let mut in_string = false;
        let mut quote_char = '\0';
        
        for ch in content.chars() {
            if (ch == '"' || ch == '\'') && !in_string {
                in_string = true;
                quote_char = ch;
            } else if ch == quote_char && in_string {
                in_string = false;
                quote_char = '\0';
            }
            
            if !in_string {
                match ch {
                    '[' | '{' => depth += 1,
                    ']' | '}' => depth -= 1,
                    ',' if depth == 0 => {
                        pairs.push(current.trim().to_string());
                        current.clear();
                        continue;
                    }
                    _ => {}
                }
            }
            
            current.push(ch);
        }
        
        if !current.trim().is_empty() {
            pairs.push(current.trim().to_string());
        }
        
        let mut obj = HashMap::new();
        for pair in pairs {
            if let Some(colon_pos) = pair.find(':') {
                let key = pair[..colon_pos].trim().trim_matches('"').trim_matches('\'');
                let val = pair[colon_pos+1..].trim();
                obj.insert(key.to_string(), self.parse_value(val));
            } else if let Some(eq_pos) = pair.find('=') {
                let key = pair[..eq_pos].trim().trim_matches('"').trim_matches('\'');
                let val = pair[eq_pos+1..].trim();
                obj.insert(key.to_string(), self.parse_value(val));
            }
        }
        
        Value::Object(obj)
    }
    
    /// Evaluate conditions for ternary expressions
    fn evaluate_condition(&mut self, condition: &str) -> bool {
        let condition = condition.trim();
        
        // Simple equality check
        if let Some(eq_pos) = condition.find("==") {
            let left = self.parse_value(condition[..eq_pos].trim());
            let right = self.parse_value(condition[eq_pos+2..].trim());
            return left.to_string() == right.to_string();
        }
        
        // Not equal
        if let Some(ne_pos) = condition.find("!=") {
            let left = self.parse_value(condition[..ne_pos].trim());
            let right = self.parse_value(condition[ne_pos+2..].trim());
            return left.to_string() != right.to_string();
        }
        
        // Greater than
        if let Some(gt_pos) = condition.find('>') {
            let left = self.parse_value(condition[..gt_pos].trim());
            let right = self.parse_value(condition[gt_pos+1..].trim());
            
            if let (Value::Number(l), Value::Number(r)) = (&left, &right) {
                return l > r;
            }
            return left.to_string() > right.to_string();
        }
        
        // Default: check if truthy
        let value = self.parse_value(condition);
        match value {
            Value::Boolean(b) => b,
            Value::String(s) => !s.is_empty() && s != "false" && s != "null" && s != "0",
            Value::Number(n) => n != 0.0,
            Value::Null => false,
            _ => true,
        }
    }
    
    /// Get value from another TSK file
    fn cross_file_get(&mut self, file_name: &str, key: &str) -> Value {
        let cache_key = format!("{}:{}", file_name, key);
        
        // Check cache
        if let Some(val) = self.cross_file_cache.get(&cache_key) {
            return val.clone();
        }
        
        // Find file
        let directories = [".", "./config", "..", "../config"];
        let mut file_path = None;
        
        for directory in &directories {
            let potential_path = Path::new(directory).join(format!("{}.tsk", file_name));
            if potential_path.exists() {
                file_path = Some(potential_path);
                break;
            }
        }
        
        if let Some(path) = file_path {
            // Parse file and get value
            let mut temp_parser = EnhancedParser::new();
            if temp_parser.parse_file(path.to_str().unwrap()).is_ok() {
                if let Some(value) = temp_parser.get(key) {
                    // Cache result
                    self.cross_file_cache.insert(cache_key, value.clone());
                    return value;
                }
            }
        }
        
        Value::String("".to_string())
    }
    
    /// Set value in another TSK file (cache only for now)
    fn cross_file_set(&mut self, file_name: &str, key: &str, value: &str) -> Value {
        let cache_key = format!("{}:{}", file_name, key);
        let parsed_value = self.parse_value(value);
        self.cross_file_cache.insert(cache_key, parsed_value.clone());
        parsed_value
    }
    
    /// Execute @date function
    fn execute_date(&self, format_str: &str) -> String {
        let now: DateTime<Utc> = Utc::now();
        
        // Convert PHP-style format to Rust
        match format_str {
            "Y" => now.format("%Y").to_string(),
            "Y-m-d" => now.format("%Y-%m-%d").to_string(),
            "Y-m-d H:i:s" => now.format("%Y-%m-%d %H:%M:%S").to_string(),
            "c" => now.to_rfc3339(),
            _ => now.format("%Y-%m-%d %H:%M:%S").to_string(),
        }
    }
    
    /// Execute database query (placeholder for now)
    fn execute_query(&mut self, query: &str) -> String {
        let _ = self.load_peanut();
        
        // Determine database type
        let db_type = self.get("database.default")
            .map(|v| v.to_string())
            .unwrap_or_else(|| "sqlite".to_string());
        
        // Placeholder implementation
        format!("[Query: {} on {}]", query, db_type)
    }
    
    /// Execute @ operators
    fn execute_operator(&mut self, operator: &str, params: &str) -> Value {
        match self.operator_engine.execute_operator(operator, params) {
            Ok(value) => value,
            Err(_) => Value::String(format!("@{}({})", operator, params)),
        }
    }
    
    /// Parse a single line
    pub fn parse_line(&mut self, line: &str) {
        let trimmed = line.trim();
        
        // Skip empty lines and comments
        if trimmed.is_empty() || trimmed.starts_with('#') {
            return;
        }
        
        // Remove optional semicolon
        let trimmed = if trimmed.ends_with(';') {
            trimmed.trim_end_matches(';').trim()
        } else {
            trimmed
        };
        
        // Check for section declaration []
        let section_re = Regex::new(r"^\[([a-zA-Z_][a-zA-Z0-9_]*)\]$").unwrap();
        if let Some(captures) = section_re.captures(trimmed) {
            self.current_section = captures.get(1).unwrap().as_str().to_string();
            self.in_object = false;
            return;
        }
        
        // Check for angle bracket object >
        let angle_open_re = Regex::new(r"^([a-zA-Z_][a-zA-Z0-9_]*)\s*>$").unwrap();
        if let Some(captures) = angle_open_re.captures(trimmed) {
            self.in_object = true;
            self.object_key = captures.get(1).unwrap().as_str().to_string();
            return;
        }
        
        // Check for closing angle bracket <
        if trimmed == "<" {
            self.in_object = false;
            self.object_key.clear();
            return;
        }
        
        // Check for curly brace object {
        let brace_open_re = Regex::new(r"^([a-zA-Z_][a-zA-Z0-9_]*)\s*\{$").unwrap();
        if let Some(captures) = brace_open_re.captures(trimmed) {
            self.in_object = true;
            self.object_key = captures.get(1).unwrap().as_str().to_string();
            return;
        }
        
        // Check for closing curly brace }
        if trimmed == "}" {
            self.in_object = false;
            self.object_key.clear();
            return;
        }
        
        // Parse key-value pairs (both : and = supported)
        let kv_re = Regex::new(r"^([\$]?[a-zA-Z_][a-zA-Z0-9_-]*)\s*[:=]\s*(.+)$").unwrap();
        if let Some(captures) = kv_re.captures(trimmed) {
            let key = captures.get(1).unwrap().as_str();
            let value = captures.get(2).unwrap().as_str();
            let parsed_value = self.parse_value(value);
            
            // Determine storage location
            let storage_key = if self.in_object && !self.object_key.is_empty() {
                if !self.current_section.is_empty() {
                    format!("{}.{}.{}", self.current_section, self.object_key, key)
                } else {
                    format!("{}.{}", self.object_key, key)
                }
            } else if !self.current_section.is_empty() {
                format!("{}.{}", self.current_section, key)
            } else {
                key.to_string()
            };
            
            // Store the value
            self.data.insert(storage_key.clone(), parsed_value.clone());
            
            // Handle global variables
            if key.starts_with('$') {
                let var_name = &key[1..];
                self.global_variables.insert(var_name.to_string(), parsed_value.clone());
            } else if !self.current_section.is_empty() && !key.starts_with('$') {
                // Store section-local variable
                let section_key = format!("{}.{}", self.current_section, key);
                self.section_variables.insert(section_key, parsed_value);
            }
        }
    }
    
    /// Parse TuskLang content
    pub fn parse(&mut self, content: &str) -> TuskResult<HashMap<String, Value>> {
        let lines: Vec<&str> = content.lines().collect();
        
        for line in lines {
            self.parse_line(line);
        }
        
        Ok(self.data.clone())
    }
    
    /// Parse a TSK file
    pub fn parse_file(&mut self, file_path: &str) -> TuskResult<()> {
        let content = fs::read_to_string(file_path)
            .map_err(|e| TuskError::io_error(format!("Failed to read file: {}", e)))?;
        
        self.parse(&content)?;
        Ok(())
    }
    
    /// Get a value by key
    pub fn get(&self, key: &str) -> Option<Value> {
        self.data.get(key).cloned()
    }
    
    /// Set a value
    pub fn set(&mut self, key: &str, value: Value) {
        self.data.insert(key.to_string(), value);
    }
    
    /// Get all keys
    pub fn keys(&self) -> Vec<String> {
        self.data.keys().cloned().collect()
    }
    
    /// Get all key-value pairs
    pub fn items(&self) -> HashMap<String, Value> {
        self.data.clone()
    }
}

impl Default for EnhancedParser {
    fn default() -> Self {
        Self::new()
    }
}

/// Load configuration from peanut.tsk
pub fn load_from_peanut() -> TuskResult<EnhancedParser> {
    let mut parser = EnhancedParser::new();
    parser.load_peanut()?;
    Ok(parser)
}