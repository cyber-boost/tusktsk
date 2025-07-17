# Debugging Tools in TuskLang for Rust

The debugging tools in TuskLang for Rust provide comprehensive debugging capabilities that integrate seamlessly with Rust's debugging ecosystem, offering powerful logging, tracing, profiling, and error analysis tools.

## Basic Debugging Setup

```rust
// Debug configuration with TuskLang
pub struct DebugConfig {
    pub log_level: log::LevelFilter,
    pub log_file: Option<String>,
    pub enable_tracing: bool,
    pub enable_profiling: bool,
    pub debug_mode: bool,
}

impl DebugConfig {
    pub fn from_tusk_config() -> Result<Self, Box<dyn std::error::Error + Send + Sync>> {
        let config = @config.load("debug_config.tsk");
        
        Ok(DebugConfig {
            log_level: config.get("debug.log_level", "info").parse()?,
            log_file: config.get_optional("debug.log_file"),
            enable_tracing: config.get("debug.enable_tracing", false),
            enable_profiling: config.get("debug.enable_profiling", false),
            debug_mode: config.get("debug.enabled", false),
        })
    }
    
    pub fn setup_debugging(&self) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        // Setup logging
        let mut builder = env_logger::Builder::new();
        builder.filter_level(self.log_level);
        
        if let Some(ref log_file) = self.log_file {
            let file_appender = tracing_appender::rolling::RollingFileAppender::new(
                tracing_appender::rolling::RollingFileAppender::builder()
                    .rotation(tracing_appender::rolling::Rotation::DAILY)
                    .filename_prefix("tusk")
                    .filename_suffix("log")
                    .build(log_file)?
            );
            builder.target(env_logger::Target::Pipe(Box::new(file_appender)));
        }
        
        builder.init();
        
        // Setup tracing if enabled
        if self.enable_tracing {
            let subscriber = tracing_subscriber::FmtSubscriber::builder()
                .with_max_level(tracing::Level::DEBUG)
                .with_file(true)
                .with_line_number(true)
                .with_thread_ids(true)
                .with_thread_names(true)
                .with_target(false)
                .with_ansi(false)
                .pretty()
                .init();
        }
        
        // Setup profiling if enabled
        if self.enable_profiling {
            @profiler.start("tusk_application")?;
        }
        
        @log.info("Debugging tools initialized successfully");
        Ok(())
    }
}

// Debug context for tracking execution
pub struct DebugContext {
    pub request_id: String,
    pub user_id: Option<u32>,
    pub session_id: Option<String>,
    pub start_time: std::time::Instant,
    pub debug_data: std::collections::HashMap<String, serde_json::Value>,
}

impl DebugContext {
    pub fn new() -> Self {
        Self {
            request_id: @uuid.generate(),
            user_id: None,
            session_id: None,
            start_time: std::time::Instant::now(),
            debug_data: std::collections::HashMap::new(),
        }
    }
    
    pub fn with_user(mut self, user_id: u32) -> Self {
        self.user_id = Some(user_id);
        self
    }
    
    pub fn with_session(mut self, session_id: String) -> Self {
        self.session_id = Some(session_id);
        self
    }
    
    pub fn add_debug_data(&mut self, key: &str, value: serde_json::Value) {
        self.debug_data.insert(key.to_string(), value);
    }
    
    pub fn log_debug_info(&self) {
        @log.debug("Debug context", {
            "request_id": &self.request_id,
            "user_id": self.user_id,
            "session_id": &self.session_id,
            "elapsed": self.start_time.elapsed().as_millis(),
            "debug_data": &self.debug_data,
        });
    }
}
```

## Advanced Logging System

```rust
// Enhanced logging with TuskLang operators
pub struct TuskLogger {
    context: Option<DebugContext>,
}

impl TuskLogger {
    pub fn new() -> Self {
        Self { context: None }
    }
    
    pub fn with_context(mut self, context: DebugContext) -> Self {
        self.context = Some(context);
        self
    }
    
    pub fn debug(&self, message: &str, data: Option<serde_json::Value>) {
        let log_entry = self.build_log_entry("DEBUG", message, data);
        @log.debug(&log_entry);
    }
    
    pub fn info(&self, message: &str, data: Option<serde_json::Value>) {
        let log_entry = self.build_log_entry("INFO", message, data);
        @log.info(&log_entry);
    }
    
    pub fn warn(&self, message: &str, data: Option<serde_json::Value>) {
        let log_entry = self.build_log_entry("WARN", message, data);
        @log.warn(&log_entry);
    }
    
    pub fn error(&self, message: &str, data: Option<serde_json::Value>) {
        let log_entry = self.build_log_entry("ERROR", message, data);
        @log.error(&log_entry);
    }
    
    fn build_log_entry(&self, level: &str, message: &str, data: Option<serde_json::Value>) -> serde_json::Value {
        let mut entry = serde_json::Map::new();
        
        entry.insert("level".to_string(), serde_json::Value::String(level.to_string()));
        entry.insert("message".to_string(), serde_json::Value::String(message.to_string()));
        entry.insert("timestamp".to_string(), serde_json::Value::String(@date.now().to_rfc3339()));
        entry.insert("thread_id".to_string(), serde_json::Value::String(@thread.get_id()));
        
        if let Some(ref context) = self.context {
            entry.insert("request_id".to_string(), serde_json::Value::String(context.request_id.clone()));
            if let Some(user_id) = context.user_id {
                entry.insert("user_id".to_string(), serde_json::Value::Number(user_id.into()));
            }
        }
        
        if let Some(data) = data {
            entry.insert("data".to_string(), data);
        }
        
        serde_json::Value::Object(entry)
    }
}

// Usage
let logger = TuskLogger::new()
    .with_context(DebugContext::new().with_user(123));

logger.info("User login successful", Some(serde_json::json!({
    "ip_address": "192.168.1.1",
    "user_agent": "Mozilla/5.0..."
})));
```

## Tracing and Instrumentation

```rust
// Tracing with TuskLang integration
use tracing::{info, warn, error, debug, instrument};

#[instrument(skip_all)]
pub async fn process_user_request(user_id: u32, request_data: serde_json::Value) -> Result<serde_json::Value, Box<dyn std::error::Error + Send + Sync>> {
    let span = tracing::info_span!("process_user_request", user_id = user_id);
    let _enter = span.enter();
    
    info!("Processing user request", {
        user_id = user_id,
        request_size = request_data.to_string().len(),
    });
    
    // Validate request
    let validation_result = validate_request(&request_data).await?;
    debug!("Request validation completed", { valid = validation_result.is_valid });
    
    if !validation_result.is_valid {
        warn!("Invalid request received", {
            user_id = user_id,
            errors = &validation_result.errors,
        });
        return Err("Invalid request".into());
    }
    
    // Process request
    let result = @processor.process(&request_data).await?;
    
    info!("Request processed successfully", {
        user_id = user_id,
        result_size = result.to_string().len(),
    });
    
    Ok(result)
}

// Custom tracing subscriber with TuskLang integration
pub struct TuskTracingSubscriber {
    inner: tracing_subscriber::fmt::Subscriber,
}

impl TuskTracingSubscriber {
    pub fn new() -> Self {
        let subscriber = tracing_subscriber::fmt::Subscriber::builder()
            .with_max_level(tracing::Level::DEBUG)
            .with_file(true)
            .with_line_number(true)
            .with_thread_ids(true)
            .with_thread_names(true)
            .with_target(false)
            .with_ansi(false)
            .with_timer(tracing_subscriber::fmt::time::UtcTime::rfc_3339())
            .with_writer(std::io::stdout)
            .with_filter(|metadata| {
                // Filter based on TuskLang configuration
                let debug_config = @config.get("debug.tracing.filter", "all");
                match debug_config.as_str() {
                    "all" => true,
                    "errors" => metadata.level() <= &tracing::Level::ERROR,
                    "warnings" => metadata.level() <= &tracing::Level::WARN,
                    "info" => metadata.level() <= &tracing::Level::INFO,
                    _ => true,
                }
            })
            .finish();
        
        Self { inner: subscriber }
    }
    
    pub fn with_tusk_context(self) -> Self {
        // Add TuskLang-specific context to traces
        self
    }
}

// Span attributes for TuskLang operations
#[derive(Debug)]
pub struct TuskSpanAttributes {
    pub operation: String,
    pub resource: String,
    pub duration: std::time::Duration,
    pub success: bool,
    pub error_message: Option<String>,
}

impl TuskSpanAttributes {
    pub fn new(operation: &str, resource: &str) -> Self {
        Self {
            operation: operation.to_string(),
            resource: resource.to_string(),
            duration: std::time::Duration::ZERO,
            success: true,
            error_message: None,
        }
    }
    
    pub fn with_duration(mut self, duration: std::time::Duration) -> Self {
        self.duration = duration;
        self
    }
    
    pub fn with_error(mut self, error: &str) -> Self {
        self.success = false;
        self.error_message = Some(error.to_string());
        self
    }
}

// Macro for instrumenting TuskLang operations
#[macro_export]
macro_rules! tusk_trace {
    ($operation:expr, $resource:expr, $code:block) => {{
        let start_time = std::time::Instant::now();
        let mut attributes = TuskSpanAttributes::new($operation, $resource);
        
        let result = std::panic::catch_unwind(|| $code);
        
        attributes = attributes.with_duration(start_time.elapsed());
        
        match result {
            Ok(_) => {
                tracing::info!("TuskLang operation completed", {
                    operation = &attributes.operation,
                    resource = &attributes.resource,
                    duration_ms = attributes.duration.as_millis(),
                    success = attributes.success,
                });
            }
            Err(e) => {
                attributes = attributes.with_error(&format!("{:?}", e));
                tracing::error!("TuskLang operation failed", {
                    operation = &attributes.operation,
                    resource = &attributes.resource,
                    duration_ms = attributes.duration.as_millis(),
                    success = attributes.success,
                    error = &attributes.error_message,
                });
            }
        }
        
        result
    }};
}

// Usage
let result = tusk_trace!("database_query", "users", {
    @db.table("users").where("id", 123).first()
});
```

## Error Analysis and Debugging

```rust
// Error analysis tools
pub struct ErrorAnalyzer {
    error_patterns: std::collections::HashMap<String, ErrorPattern>,
    error_history: Vec<ErrorRecord>,
}

#[derive(Clone)]
pub struct ErrorPattern {
    pub pattern: String,
    pub severity: ErrorSeverity,
    pub suggested_fix: String,
    pub frequency: u32,
}

#[derive(Clone)]
pub enum ErrorSeverity {
    Low,
    Medium,
    High,
    Critical,
}

#[derive(Clone)]
pub struct ErrorRecord {
    pub timestamp: chrono::DateTime<chrono::Utc>,
    pub error_type: String,
    pub error_message: String,
    pub stack_trace: Option<String>,
    pub context: serde_json::Value,
}

impl ErrorAnalyzer {
    pub fn new() -> Self {
        let mut analyzer = Self {
            error_patterns: std::collections::HashMap::new(),
            error_history: Vec::new(),
        };
        
        // Load error patterns from TuskLang config
        analyzer.load_error_patterns();
        
        analyzer
    }
    
    fn load_error_patterns(&mut self) {
        let patterns = @config.get("debug.error_patterns", serde_json::json!({}));
        
        for (pattern_name, pattern_data) in patterns.as_object().unwrap() {
            let pattern = ErrorPattern {
                pattern: pattern_data["pattern"].as_str().unwrap().to_string(),
                severity: match pattern_data["severity"].as_str().unwrap() {
                    "low" => ErrorSeverity::Low,
                    "medium" => ErrorSeverity::Medium,
                    "high" => ErrorSeverity::High,
                    "critical" => ErrorSeverity::Critical,
                    _ => ErrorSeverity::Medium,
                },
                suggested_fix: pattern_data["fix"].as_str().unwrap().to_string(),
                frequency: 0,
            };
            
            self.error_patterns.insert(pattern_name.clone(), pattern);
        }
    }
    
    pub fn analyze_error(&mut self, error: &dyn std::error::Error, context: serde_json::Value) -> ErrorAnalysis {
        let error_message = error.to_string();
        let error_type = std::any::type_name_of_val(error);
        
        // Record error
        let record = ErrorRecord {
            timestamp: chrono::Utc::now(),
            error_type: error_type.to_string(),
            error_message: error_message.clone(),
            stack_trace: Some(@debug.get_stack_trace()),
            context,
        };
        
        self.error_history.push(record);
        
        // Analyze patterns
        let mut matched_patterns = Vec::new();
        for (name, pattern) in &mut self.error_patterns {
            if error_message.contains(&pattern.pattern) {
                pattern.frequency += 1;
                matched_patterns.push((name.clone(), pattern.clone()));
            }
        }
        
        // Generate analysis
        ErrorAnalysis {
            error_type: error_type.to_string(),
            error_message,
            severity: self.determine_severity(&matched_patterns),
            matched_patterns,
            suggested_fixes: self.generate_fixes(&matched_patterns),
            frequency: self.get_error_frequency(error_type),
            similar_errors: self.find_similar_errors(error_type),
        }
    }
    
    fn determine_severity(&self, patterns: &[(String, ErrorPattern)]) -> ErrorSeverity {
        patterns.iter()
            .map(|(_, pattern)| &pattern.severity)
            .max_by_key(|severity| match severity {
                ErrorSeverity::Low => 1,
                ErrorSeverity::Medium => 2,
                ErrorSeverity::High => 3,
                ErrorSeverity::Critical => 4,
            })
            .unwrap_or(&ErrorSeverity::Medium)
            .clone()
    }
    
    fn generate_fixes(&self, patterns: &[(String, ErrorPattern)]) -> Vec<String> {
        patterns.iter()
            .map(|(_, pattern)| pattern.suggested_fix.clone())
            .collect()
    }
    
    fn get_error_frequency(&self, error_type: &str) -> u32 {
        self.error_history.iter()
            .filter(|record| record.error_type == error_type)
            .count() as u32
    }
    
    fn find_similar_errors(&self, error_type: &str) -> Vec<ErrorRecord> {
        self.error_history.iter()
            .filter(|record| record.error_type == error_type)
            .take(5)
            .cloned()
            .collect()
    }
    
    pub fn generate_error_report(&self) -> String {
        let mut report = String::from("Error Analysis Report\n");
        report.push_str("====================\n\n");
        
        // Most frequent errors
        let mut error_counts: std::collections::HashMap<String, u32> = std::collections::HashMap::new();
        for record in &self.error_history {
            *error_counts.entry(record.error_type.clone()).or_insert(0) += 1;
        }
        
        let mut sorted_errors: Vec<_> = error_counts.iter().collect();
        sorted_errors.sort_by_key(|(_, &count)| std::cmp::Reverse(count));
        
        report.push_str("Most Frequent Errors:\n");
        for (error_type, count) in sorted_errors.iter().take(10) {
            report.push_str(&format!("  {}: {} occurrences\n", error_type, count));
        }
        
        // Error patterns
        report.push_str("\nError Patterns:\n");
        for (name, pattern) in &self.error_patterns {
            report.push_str(&format!("  {}: {} matches\n", name, pattern.frequency));
        }
        
        report
    }
}

pub struct ErrorAnalysis {
    pub error_type: String,
    pub error_message: String,
    pub severity: ErrorSeverity,
    pub matched_patterns: Vec<(String, ErrorPattern)>,
    pub suggested_fixes: Vec<String>,
    pub frequency: u32,
    pub similar_errors: Vec<ErrorRecord>,
}

// Usage
let mut analyzer = ErrorAnalyzer::new();

match some_operation() {
    Ok(result) => result,
    Err(e) => {
        let analysis = analyzer.analyze_error(&*e, serde_json::json!({
            "operation": "user_creation",
            "user_data": user_data
        }));
        
        @log.error("Operation failed", {
            analysis = &analysis,
        });
        
        // Apply suggested fixes
        for fix in &analysis.suggested_fixes {
            @log.info("Suggested fix", { fix });
        }
        
        Err(e)
    }
}
```

## Performance Profiling

```rust
// Performance profiling with TuskLang integration
pub struct TuskProfiler {
    profiles: std::collections::HashMap<String, ProfileData>,
    active_profiles: std::collections::HashMap<String, std::time::Instant>,
}

#[derive(Clone)]
pub struct ProfileData {
    pub name: String,
    pub total_calls: u64,
    pub total_duration: std::time::Duration,
    pub min_duration: std::time::Duration,
    pub max_duration: std::time::Duration,
    pub avg_duration: std::time::Duration,
    pub memory_usage: Vec<usize>,
}

impl TuskProfiler {
    pub fn new() -> Self {
        Self {
            profiles: std::collections::HashMap::new(),
            active_profiles: std::collections::HashMap::new(),
        }
    }
    
    pub fn start_profile(&mut self, name: &str) {
        self.active_profiles.insert(name.to_string(), std::time::Instant::now());
    }
    
    pub fn end_profile(&mut self, name: &str) -> Option<std::time::Duration> {
        if let Some(start_time) = self.active_profiles.remove(name) {
            let duration = start_time.elapsed();
            self.record_profile(name, duration);
            Some(duration)
        } else {
            None
        }
    }
    
    fn record_profile(&mut self, name: &str, duration: std::time::Duration) {
        let profile = self.profiles.entry(name.to_string()).or_insert(ProfileData {
            name: name.to_string(),
            total_calls: 0,
            total_duration: std::time::Duration::ZERO,
            min_duration: std::time::Duration::MAX,
            max_duration: std::time::Duration::ZERO,
            avg_duration: std::time::Duration::ZERO,
            memory_usage: Vec::new(),
        });
        
        profile.total_calls += 1;
        profile.total_duration += duration;
        profile.min_duration = profile.min_duration.min(duration);
        profile.max_duration = profile.max_duration.max(duration);
        profile.avg_duration = profile.total_duration / profile.total_calls;
        
        // Record memory usage
        let memory_usage = @memory.get_usage();
        profile.memory_usage.push(memory_usage);
        
        // Keep only last 100 memory readings
        if profile.memory_usage.len() > 100 {
            profile.memory_usage.remove(0);
        }
    }
    
    pub fn get_profile(&self, name: &str) -> Option<&ProfileData> {
        self.profiles.get(name)
    }
    
    pub fn generate_profile_report(&self) -> String {
        let mut report = String::from("Performance Profile Report\n");
        report.push_str("==========================\n\n");
        
        for (name, profile) in &self.profiles {
            report.push_str(&format!("Profile: {}\n", name));
            report.push_str(&format!("  Total Calls: {}\n", profile.total_calls));
            report.push_str(&format!("  Total Duration: {:?}\n", profile.total_duration));
            report.push_str(&format!("  Average Duration: {:?}\n", profile.avg_duration));
            report.push_str(&format!("  Min Duration: {:?}\n", profile.min_duration));
            report.push_str(&format!("  Max Duration: {:?}\n", profile.max_duration));
            
            if !profile.memory_usage.is_empty() {
                let avg_memory: usize = profile.memory_usage.iter().sum::<usize>() / profile.memory_usage.len();
                report.push_str(&format!("  Average Memory Usage: {} bytes\n", avg_memory));
            }
            
            report.push_str("\n");
        }
        
        report
    }
}

// Profiling macro
#[macro_export]
macro_rules! profile {
    ($profiler:expr, $name:expr, $code:block) => {{
        $profiler.start_profile($name);
        let result = $code;
        $profiler.end_profile($name);
        result
    }};
}

// Usage
let mut profiler = TuskProfiler::new();

let result = profile!(profiler, "database_query", {
    @db.table("users").where("id", 123).first()
});

// Generate report
let report = profiler.generate_profile_report();
@file.write("profile_report.txt", &report).unwrap();
```

## Memory Debugging

```rust
// Memory debugging tools
pub struct MemoryDebugger {
    allocations: std::collections::HashMap<usize, AllocationInfo>,
    allocation_count: usize,
    total_allocated: usize,
}

#[derive(Clone)]
pub struct AllocationInfo {
    pub address: usize,
    pub size: usize,
    pub allocation_time: std::time::Instant,
    pub stack_trace: Vec<String>,
    pub freed: bool,
    pub free_time: Option<std::time::Instant>,
}

impl MemoryDebugger {
    pub fn new() -> Self {
        Self {
            allocations: std::collections::HashMap::new(),
            allocation_count: 0,
            total_allocated: 0,
        }
    }
    
    pub fn track_allocation(&mut self, address: usize, size: usize) {
        let allocation = AllocationInfo {
            address,
            size,
            allocation_time: std::time::Instant::now(),
            stack_trace: @debug.get_stack_trace_lines(),
            freed: false,
            free_time: None,
        };
        
        self.allocations.insert(address, allocation);
        self.allocation_count += 1;
        self.total_allocated += size;
        
        @log.debug("Memory allocation tracked", {
            address = format!("0x{:x}", address),
            size = size,
            total_allocated = self.total_allocated,
        });
    }
    
    pub fn track_deallocation(&mut self, address: usize) {
        if let Some(allocation) = self.allocations.get_mut(&address) {
            allocation.freed = true;
            allocation.free_time = Some(std::time::Instant::now());
            self.total_allocated -= allocation.size;
            
            @log.debug("Memory deallocation tracked", {
                address = format!("0x{:x}", address),
                size = allocation.size,
                lifetime_ms = allocation.free_time.unwrap().duration_since(allocation.allocation_time).as_millis(),
            });
        }
    }
    
    pub fn find_memory_leaks(&self) -> Vec<&AllocationInfo> {
        self.allocations.values()
            .filter(|allocation| !allocation.freed)
            .collect()
    }
    
    pub fn generate_memory_report(&self) -> String {
        let mut report = String::from("Memory Debug Report\n");
        report.push_str("==================\n\n");
        
        report.push_str(&format!("Total Allocations: {}\n", self.allocation_count));
        report.push_str(&format!("Currently Allocated: {} bytes\n", self.total_allocated));
        
        let leaks = self.find_memory_leaks();
        report.push_str(&format!("Memory Leaks: {}\n", leaks.len()));
        
        if !leaks.is_empty() {
            report.push_str("\nMemory Leaks:\n");
            for leak in leaks.iter().take(10) {
                report.push_str(&format!("  Address: 0x{:x}, Size: {} bytes\n", leak.address, leak.size));
                report.push_str(&format!("    Allocated at: {:?}\n", leak.allocation_time));
                report.push_str(&format!("    Stack trace:\n"));
                for line in &leak.stack_trace {
                    report.push_str(&format!("      {}\n", line));
                }
                report.push_str("\n");
            }
        }
        
        report
    }
}

// Memory debugging macro
#[macro_export]
macro_rules! debug_memory {
    ($debugger:expr, $code:block) => {{
        let start_memory = @memory.get_usage();
        let result = $code;
        let end_memory = @memory.get_usage();
        
        if end_memory > start_memory {
            @log.warn("Potential memory leak detected", {
                start_memory = start_memory,
                end_memory = end_memory,
                difference = end_memory - start_memory,
            });
        }
        
        result
    }};
}

// Usage
let mut memory_debugger = MemoryDebugger::new();

let result = debug_memory!(memory_debugger, {
    // Some operation that might leak memory
    let data = vec![0u8; 1024 * 1024]; // 1MB allocation
    // data is dropped here, so no leak
});

// Generate memory report
let report = memory_debugger.generate_memory_report();
@file.write("memory_report.txt", &report).unwrap();
```

## Interactive Debugging

```rust
// Interactive debugging console
pub struct DebugConsole {
    commands: std::collections::HashMap<String, Box<dyn Fn(&[&str]) -> Result<String, Box<dyn std::error::Error + Send + Sync>> + Send + Sync>>,
}

impl DebugConsole {
    pub fn new() -> Self {
        let mut console = Self {
            commands: std::collections::HashMap::new(),
        };
        
        // Register built-in commands
        console.register_command("help", |_| {
            Ok(String::from("Available commands:\n  help - Show this help\n  status - Show system status\n  config - Show configuration\n  memory - Show memory usage\n  errors - Show recent errors"))
        });
        
        console.register_command("status", |_| {
            let status = serde_json::json!({
                "uptime": @system.get_uptime(),
                "memory_usage": @memory.get_usage(),
                "cpu_usage": @cpu.get_usage(),
                "active_connections": @db.get_active_connections(),
                "cache_hit_rate": @cache.get_hit_rate(),
            });
            
            Ok(serde_json::to_string_pretty(&status).unwrap())
        });
        
        console.register_command("config", |args| {
            let config_key = args.get(0).unwrap_or(&"");
            let config_value = @config.get(config_key, "Not found");
            Ok(format!("{} = {}", config_key, config_value))
        });
        
        console.register_command("memory", |_| {
            let memory_info = @memory.get_detailed_info();
            Ok(serde_json::to_string_pretty(&memory_info).unwrap())
        });
        
        console.register_command("errors", |args| {
            let count = args.get(0).and_then(|s| s.parse::<usize>().ok()).unwrap_or(10);
            let errors = @log.get_recent_errors(count);
            Ok(serde_json::to_string_pretty(&errors).unwrap())
        });
        
        console
    }
    
    pub fn register_command<F>(&mut self, name: &str, handler: F)
    where
        F: Fn(&[&str]) -> Result<String, Box<dyn std::error::Error + Send + Sync>> + 'static + Send + Sync,
    {
        self.commands.insert(name.to_string(), Box::new(handler));
    }
    
    pub fn execute_command(&self, input: &str) -> Result<String, Box<dyn std::error::Error + Send + Sync>> {
        let parts: Vec<&str> = input.split_whitespace().collect();
        if parts.is_empty() {
            return Ok(String::from("No command provided"));
        }
        
        let command = parts[0];
        let args = &parts[1..];
        
        if let Some(handler) = self.commands.get(command) {
            handler(args)
        } else {
            Err(format!("Unknown command: {}", command).into())
        }
    }
    
    pub async fn start_interactive(&self) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        use tokio::io::{AsyncBufReadExt, BufReader};
        use tokio::io::AsyncWriteExt;
        
        let stdin = tokio::io::stdin();
        let mut reader = BufReader::new(stdin);
        let mut line = String::new();
        
        println!("TuskLang Debug Console");
        println!("Type 'help' for available commands, 'quit' to exit");
        
        loop {
            print!("tusk> ");
            tokio::io::stdout().flush().await?;
            
            line.clear();
            if reader.read_line(&mut line).await? == 0 {
                break;
            }
            
            let input = line.trim();
            if input == "quit" {
                break;
            }
            
            match self.execute_command(input) {
                Ok(output) => println!("{}", output),
                Err(e) => println!("Error: {}", e),
            }
        }
        
        Ok(())
    }
}

// Usage
let console = DebugConsole::new();

// Register custom command
let mut console = console;
console.register_command("users", |args| {
    let user_id = args.get(0).and_then(|s| s.parse::<u32>().ok());
    match user_id {
        Some(id) => {
            let user = @db.users.find(id)?;
            Ok(serde_json::to_string_pretty(&user).unwrap())
        }
        None => {
            let users = @db.users.all()?;
            Ok(serde_json::to_string_pretty(&users).unwrap())
        }
    }
});

// Start interactive console
tokio::spawn(async move {
    console.start_interactive().await.unwrap();
});
```

This comprehensive guide covers Rust-specific debugging patterns, ensuring type safety, performance, and integration with Rust's debugging ecosystem while maintaining the power and flexibility of TuskLang's debugging capabilities. 