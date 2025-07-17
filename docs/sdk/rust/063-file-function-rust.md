# 📁 @file Function in Rust

The `@file` function in TuskLang provides secure and efficient file system operations in Rust, with comprehensive error handling, type safety, and performance optimizations.

## Basic Usage

```rust
// Basic file operations
let content = @file.read("config.tusk")?;
@file.write("log.txt", "Application started")?;
let exists = @file.exists("data.json")?;

// File operations with paths
let config = @file.read("/etc/app/config.tusk")?;
@file.write("/var/log/app.log", @log_content)?;
let is_file = @file.is_file("path/to/file.txt")?;
let is_dir = @file.is_directory("path/to/directory")?;
```

## File System Operations

```rust
// Comprehensive file system operations
struct FileSystemManager {
    base_path: std::path::PathBuf,
    permissions: FilePermissions,
    cache: std::collections::HashMap<String, (Vec<u8>, std::time::Instant)>,
}

impl FileSystemManager {
    fn new(base_path: &str) -> Result<Self, Box<dyn std::error::Error>> {
        let base_path = std::path::PathBuf::from(base_path);
        
        // Ensure base path exists and is accessible
        if !base_path.exists() {
            std::fs::create_dir_all(&base_path)?;
        }
        
        Ok(Self {
            base_path,
            permissions: FilePermissions::new(),
            cache: std::collections::HashMap::new(),
        })
    }
    
    // Read file with caching
    fn read_file(&mut self, path: &str) -> Result<String, Box<dyn std::error::Error>> {
        let full_path = self.base_path.join(path);
        
        // Validate path security
        self.validate_path(&full_path)?;
        
        // Check cache first
        let cache_key = full_path.to_string_lossy().to_string();
        if let Some((content, timestamp)) = self.cache.get(&cache_key) {
            if timestamp.elapsed() < std::time::Duration::from_secs(300) { // 5 minutes cache
                return Ok(String::from_utf8_lossy(content).to_string());
            }
        }
        
        // Read file
        let content = std::fs::read_to_string(&full_path)?;
        
        // Cache content
        self.cache.insert(cache_key, (content.as_bytes().to_vec(), std::time::Instant::now()));
        
        Ok(content)
    }
    
    // Write file with atomic operation
    fn write_file(&self, path: &str, content: &str) -> Result<(), Box<dyn std::error::Error>> {
        let full_path = self.base_path.join(path);
        
        // Validate path security
        self.validate_path(&full_path)?;
        
        // Ensure directory exists
        if let Some(parent) = full_path.parent() {
            std::fs::create_dir_all(parent)?;
        }
        
        // Write atomically using temporary file
        let temp_path = full_path.with_extension("tmp");
        std::fs::write(&temp_path, content)?;
        std::fs::rename(&temp_path, &full_path)?;
        
        Ok(())
    }
    
    // Check file existence
    fn file_exists(&self, path: &str) -> Result<bool, Box<dyn std::error::Error>> {
        let full_path = self.base_path.join(path);
        self.validate_path(&full_path)?;
        Ok(full_path.exists())
    }
    
    // Get file information
    fn get_file_info(&self, path: &str) -> Result<FileInfo, Box<dyn std::error::Error>> {
        let full_path = self.base_path.join(path);
        self.validate_path(&full_path)?;
        
        let metadata = std::fs::metadata(&full_path)?;
        
        Ok(FileInfo {
            path: full_path.to_string_lossy().to_string(),
            size: metadata.len(),
            is_file: metadata.is_file(),
            is_directory: metadata.is_dir(),
            created: metadata.created().ok(),
            modified: metadata.modified().ok(),
            permissions: metadata.permissions(),
        })
    }
    
    // Validate path security
    fn validate_path(&self, path: &std::path::Path) -> Result<(), Box<dyn std::error::Error>> {
        // Check for path traversal attempts
        if path.to_string_lossy().contains("..") {
            return Err("Path traversal attempt detected".into());
        }
        
        // Ensure path is within base directory
        if !path.starts_with(&self.base_path) {
            return Err("Path outside allowed directory".into());
        }
        
        // Check file permissions
        self.permissions.check_access(path)?;
        
        Ok(())
    }
}

#[derive(Debug)]
struct FileInfo {
    path: String,
    size: u64,
    is_file: bool,
    is_directory: bool,
    created: Option<std::time::SystemTime>,
    modified: Option<std::time::SystemTime>,
    permissions: std::fs::Permissions,
}

struct FilePermissions {
    allowed_extensions: std::collections::HashSet<String>,
    max_file_size: u64,
    read_only_paths: std::collections::HashSet<String>,
}

impl FilePermissions {
    fn new() -> Self {
        let allowed_extensions = vec![
            "txt", "json", "yaml", "yml", "toml", "tusk", "md", "log"
        ].into_iter().map(|s| s.to_string()).collect();
        
        let read_only_paths = vec![
            "/etc/", "/var/log/", "/proc/"
        ].into_iter().map(|s| s.to_string()).collect();
        
        Self {
            allowed_extensions,
            max_file_size: 10 * 1024 * 1024, // 10MB
            read_only_paths,
        }
    }
    
    fn check_access(&self, path: &std::path::Path) -> Result<(), Box<dyn std::error::Error>> {
        let path_str = path.to_string_lossy();
        
        // Check file extension
        if let Some(extension) = path.extension() {
            let ext = extension.to_string_lossy().to_lowercase();
            if !self.allowed_extensions.contains(&ext) {
                return Err(format!("File extension '{}' not allowed", ext).into());
            }
        }
        
        // Check if path is read-only
        for read_only_path in &self.read_only_paths {
            if path_str.starts_with(read_only_path) {
                return Err("Path is read-only".into());
            }
        }
        
        Ok(())
    }
}
```

## File Type Operations

```rust
// File type-specific operations
struct FileTypeManager {
    handlers: std::collections::HashMap<String, Box<dyn FileHandler>>,
}

trait FileHandler {
    fn read(&self, path: &str) -> Result<Value, Box<dyn std::error::Error>>;
    fn write(&self, path: &str, content: &Value) -> Result<(), Box<dyn std::error::Error>>;
    fn validate(&self, content: &str) -> Result<(), Box<dyn std::error::Error>>;
}

impl FileTypeManager {
    fn new() -> Self {
        let mut manager = Self {
            handlers: std::collections::HashMap::new(),
        };
        
        // Register file handlers
        manager.register_handler("json", Box::new(JsonFileHandler));
        manager.register_handler("yaml", Box::new(YamlFileHandler));
        manager.register_handler("toml", Box::new(TomlFileHandler));
        manager.register_handler("tusk", Box::new(TuskFileHandler));
        
        manager
    }
    
    fn register_handler(&mut self, extension: &str, handler: Box<dyn FileHandler>) {
        self.handlers.insert(extension.to_string(), handler);
    }
    
    fn get_handler(&self, path: &str) -> Option<&Box<dyn FileHandler>> {
        let extension = std::path::Path::new(path)
            .extension()
            .and_then(|ext| ext.to_str())
            .unwrap_or("");
        
        self.handlers.get(extension)
    }
}

// JSON file handler
struct JsonFileHandler;

impl FileHandler for JsonFileHandler {
    fn read(&self, path: &str) -> Result<Value, Box<dyn std::error::Error>> {
        let content = std::fs::read_to_string(path)?;
        let value = serde_json::from_str(&content)?;
        Ok(value)
    }
    
    fn write(&self, path: &str, content: &Value) -> Result<(), Box<dyn std::error::Error>> {
        let json_string = serde_json::to_string_pretty(content)?;
        std::fs::write(path, json_string)?;
        Ok(())
    }
    
    fn validate(&self, content: &str) -> Result<(), Box<dyn std::error::Error>> {
        serde_json::from_str::<serde_json::Value>(content)?;
        Ok(())
    }
}

// YAML file handler
struct YamlFileHandler;

impl FileHandler for YamlFileHandler {
    fn read(&self, path: &str) -> Result<Value, Box<dyn std::error::Error>> {
        let content = std::fs::read_to_string(path)?;
        let value = serde_yaml::from_str(&content)?;
        Ok(value)
    }
    
    fn write(&self, path: &str, content: &Value) -> Result<(), Box<dyn std::error::Error>> {
        let yaml_string = serde_yaml::to_string(content)?;
        std::fs::write(path, yaml_string)?;
        Ok(())
    }
    
    fn validate(&self, content: &str) -> Result<(), Box<dyn std::error::Error>> {
        serde_yaml::from_str::<serde_yaml::Value>(content)?;
        Ok(())
    }
}

// TOML file handler
struct TomlFileHandler;

impl FileHandler for TomlFileHandler {
    fn read(&self, path: &str) -> Result<Value, Box<dyn std::error::Error>> {
        let content = std::fs::read_to_string(path)?;
        let value = toml::from_str(&content)?;
        Ok(value)
    }
    
    fn write(&self, path: &str, content: &Value) -> Result<(), Box<dyn std::error::Error>> {
        let toml_string = toml::to_string_pretty(content)?;
        std::fs::write(path, toml_string)?;
        Ok(())
    }
    
    fn validate(&self, content: &str) -> Result<(), Box<dyn std::error::Error>> {
        toml::from_str::<toml::Value>(content)?;
        Ok(())
    }
}

// TuskLang file handler
struct TuskFileHandler;

impl FileHandler for TuskFileHandler {
    fn read(&self, path: &str) -> Result<Value, Box<dyn std::error::Error>> {
        let content = std::fs::read_to_string(path)?;
        let value = @parse_tusk(content)?;
        Ok(value)
    }
    
    fn write(&self, path: &str, content: &Value) -> Result<(), Box<dyn std::error::Error>> {
        let tusk_string = @serialize_tusk(content)?;
        std::fs::write(path, tusk_string)?;
        Ok(())
    }
    
    fn validate(&self, content: &str) -> Result<(), Box<dyn std::error::Error>> {
        @parse_tusk(content)?;
        Ok(())
    }
}
```

## File Monitoring and Events

```rust
// File system monitoring
struct FileMonitor {
    watchers: std::collections::HashMap<String, std::sync::mpsc::Sender<FileEvent>>,
    event_handlers: std::collections::HashMap<FileEventType, Vec<Box<dyn Fn(FileEvent) + Send>>>,
}

#[derive(Debug, Clone)]
struct FileEvent {
    event_type: FileEventType,
    path: String,
    timestamp: std::time::Instant,
    metadata: std::collections::HashMap<String, String>,
}

#[derive(Debug, Clone)]
enum FileEventType {
    Created,
    Modified,
    Deleted,
    Renamed { old_path: String },
}

impl FileMonitor {
    fn new() -> Self {
        Self {
            watchers: std::collections::HashMap::new(),
            event_handlers: std::collections::HashMap::new(),
        }
    }
    
    // Watch directory for changes
    fn watch_directory(&mut self, path: &str) -> Result<(), Box<dyn std::error::Error>> {
        let (tx, rx) = std::sync::mpsc::channel();
        
        let path = path.to_string();
        let event_handlers = self.event_handlers.clone();
        
        std::thread::spawn(move || {
            // Use notify crate for file system watching
            let (notify_tx, notify_rx) = std::sync::mpsc::channel();
            
            let mut watcher = notify::recommended_watcher(move |res| {
                if let Ok(event) = res {
                    let _ = notify_tx.send(event);
                }
            }).unwrap();
            
            watcher.watch(&path, notify::RecursiveMode::Recursive).unwrap();
            
            for event in notify_rx {
                let file_event = FileEvent {
                    event_type: FileEventType::Modified,
                    path: event.paths.first().unwrap().to_string_lossy().to_string(),
                    timestamp: std::time::Instant::now(),
                    metadata: std::collections::HashMap::new(),
                };
                
                // Send to main thread
                let _ = tx.send(file_event);
            }
        });
        
        self.watchers.insert(path, tx);
        
        Ok(())
    }
    
    // Register event handler
    fn on_event(&mut self, event_type: FileEventType, handler: Box<dyn Fn(FileEvent) + Send>) {
        self.event_handlers.entry(event_type).or_insert_with(Vec::new).push(handler);
    }
    
    // Process events
    fn process_events(&self) {
        for (_, rx) in &self.watchers {
            while let Ok(event) = rx.try_recv() {
                if let Some(handlers) = self.event_handlers.get(&event.event_type) {
                    for handler in handlers {
                        handler(event.clone());
                    }
                }
            }
        }
    }
}
```

## File Compression and Archiving

```rust
// File compression utilities
struct FileCompressor;

impl FileCompressor {
    // Compress file using gzip
    fn compress_gzip(&self, input_path: &str, output_path: &str) -> Result<(), Box<dyn std::error::Error>> {
        let input = std::fs::File::open(input_path)?;
        let output = std::fs::File::create(output_path)?;
        
        let mut encoder = flate2::write::GzEncoder::new(output, flate2::Compression::default());
        std::io::copy(&mut std::io::BufReader::new(input), &mut encoder)?;
        encoder.finish()?;
        
        Ok(())
    }
    
    // Decompress gzip file
    fn decompress_gzip(&self, input_path: &str, output_path: &str) -> Result<(), Box<dyn std::error::Error>> {
        let input = std::fs::File::open(input_path)?;
        let output = std::fs::File::create(output_path)?;
        
        let mut decoder = flate2::read::GzDecoder::new(input);
        std::io::copy(&mut decoder, &mut std::io::BufWriter::new(output))?;
        
        Ok(())
    }
    
    // Create tar archive
    fn create_tar(&self, source_dir: &str, output_path: &str) -> Result<(), Box<dyn std::error::Error>> {
        let output = std::fs::File::create(output_path)?;
        let mut builder = tar::Builder::new(output);
        
        let source_path = std::path::Path::new(source_dir);
        builder.append_dir_all(".", source_path)?;
        builder.finish()?;
        
        Ok(())
    }
    
    // Extract tar archive
    fn extract_tar(&self, archive_path: &str, extract_dir: &str) -> Result<(), Box<dyn std::error::Error>> {
        let input = std::fs::File::open(archive_path)?;
        let mut archive = tar::Archive::new(input);
        
        archive.unpack(extract_dir)?;
        
        Ok(())
    }
}
```

## File Search and Indexing

```rust
// File search and indexing
struct FileIndexer {
    index: std::collections::HashMap<String, Vec<FileIndexEntry>>,
    search_cache: std::collections::HashMap<String, Vec<String>>,
}

#[derive(Debug, Clone)]
struct FileIndexEntry {
    path: String,
    size: u64,
    modified: std::time::SystemTime,
    content_hash: String,
    metadata: std::collections::HashMap<String, String>,
}

impl FileIndexer {
    fn new() -> Self {
        Self {
            index: std::collections::HashMap::new(),
            search_cache: std::collections::HashMap::new(),
        }
    }
    
    // Index directory
    fn index_directory(&mut self, path: &str) -> Result<(), Box<dyn std::error::Error>> {
        let entries = std::fs::read_dir(path)?;
        
        for entry in entries {
            let entry = entry?;
            let path = entry.path();
            
            if path.is_file() {
                let metadata = std::fs::metadata(&path)?;
                let content_hash = self.calculate_content_hash(&path)?;
                
                let index_entry = FileIndexEntry {
                    path: path.to_string_lossy().to_string(),
                    size: metadata.len(),
                    modified: metadata.modified()?,
                    content_hash,
                    metadata: self.extract_metadata(&path)?,
                };
                
                // Index by file extension
                if let Some(extension) = path.extension() {
                    let ext = extension.to_string_lossy().to_lowercase();
                    self.index.entry(ext).or_insert_with(Vec::new).push(index_entry);
                }
            }
        }
        
        Ok(())
    }
    
    // Search files
    fn search_files(&self, query: &str) -> Result<Vec<String>, Box<dyn std::error::Error>> {
        // Check cache first
        if let Some(cached) = self.search_cache.get(query) {
            return Ok(cached.clone());
        }
        
        let mut results = Vec::new();
        
        for (_, entries) in &self.index {
            for entry in entries {
                if entry.path.contains(query) || 
                   entry.metadata.values().any(|v| v.contains(query)) {
                    results.push(entry.path.clone());
                }
            }
        }
        
        // Cache results
        self.search_cache.insert(query.to_string(), results.clone());
        
        Ok(results)
    }
    
    // Calculate content hash
    fn calculate_content_hash(&self, path: &std::path::Path) -> Result<String, Box<dyn std::error::Error>> {
        use sha2::{Sha256, Digest};
        
        let content = std::fs::read(path)?;
        let mut hasher = Sha256::new();
        hasher.update(&content);
        let result = hasher.finalize();
        
        Ok(format!("{:x}", result))
    }
    
    // Extract metadata
    fn extract_metadata(&self, path: &std::path::Path) -> Result<std::collections::HashMap<String, String>, Box<dyn std::error::Error>> {
        let mut metadata = std::collections::HashMap::new();
        
        // Extract basic metadata
        metadata.insert("extension".to_string(), 
            path.extension().and_then(|ext| ext.to_str()).unwrap_or("").to_string());
        
        metadata.insert("filename".to_string(), 
            path.file_name().and_then(|name| name.to_str()).unwrap_or("").to_string());
        
        // Extract content-based metadata for supported file types
        if let Some(extension) = path.extension() {
            match extension.to_string_lossy().to_lowercase().as_str() {
                "json" => {
                    if let Ok(content) = std::fs::read_to_string(path) {
                        if let Ok(json) = serde_json::from_str::<serde_json::Value>(&content) {
                            if let Some(title) = json.get("title").and_then(|v| v.as_str()) {
                                metadata.insert("title".to_string(), title.to_string());
                            }
                        }
                    }
                }
                "md" => {
                    if let Ok(content) = std::fs::read_to_string(path) {
                        // Extract first heading
                        for line in content.lines() {
                            if line.starts_with('#') {
                                metadata.insert("title".to_string(), line.trim_start_matches('#').trim().to_string());
                                break;
                            }
                        }
                    }
                }
                _ => {}
            }
        }
        
        Ok(metadata)
    }
}
```

## Best Practices

### 1. Always Validate File Paths
```rust
// Secure file path validation
fn secure_file_operation(path: &str) -> Result<(), Box<dyn std::error::Error>> {
    let fs_manager = FileSystemManager::new("/safe/base/path")?;
    
    // Validate path before operation
    let full_path = std::path::PathBuf::from(path);
    fs_manager.validate_path(&full_path)?;
    
    // Perform file operation
    fs_manager.read_file(path)?;
    
    Ok(())
}
```

### 2. Use Appropriate File Handlers
```rust
// Use type-specific file handlers
fn handle_file_by_type(path: &str) -> Result<Value, Box<dyn std::error::Error>> {
    let type_manager = FileTypeManager::new();
    
    if let Some(handler) = type_manager.get_handler(path) {
        handler.read(path)
    } else {
        // Fallback to raw file read
        let content = std::fs::read_to_string(path)?;
        Ok(Value::String(content))
    }
}
```

### 3. Implement File Caching
```rust
// Implement file caching for performance
fn cached_file_read(path: &str) -> Result<String, Box<dyn std::error::Error>> {
    let cache_key = format!("file:{}", path);
    
    // Check cache first
    if let Some(cached) = @cache.get(&cache_key) {
        return Ok(cached);
    }
    
    // Read file
    let content = std::fs::read_to_string(path)?;
    
    // Cache for 5 minutes
    @cache.put(&cache_key, &content, std::time::Duration::from_secs(300))?;
    
    Ok(content)
}
```

### 4. Monitor File Changes
```rust
// Monitor important files for changes
fn setup_file_monitoring() -> Result<(), Box<dyn std::error::Error>> {
    let mut monitor = FileMonitor::new();
    
    // Watch configuration directory
    monitor.watch_directory("/etc/app/config")?;
    
    // Handle configuration file changes
    monitor.on_event(FileEventType::Modified, Box::new(|event| {
        if event.path.ends_with(".tusk") {
            @log("Configuration file changed: {}", event.path);
            @reload_configuration();
        }
    }));
    
    Ok(())
}
```

### 5. Use Atomic File Operations
```rust
// Use atomic operations for file writes
fn atomic_file_write(path: &str, content: &str) -> Result<(), Box<dyn std::error::Error>> {
    let temp_path = format!("{}.tmp", path);
    
    // Write to temporary file
    std::fs::write(&temp_path, content)?;
    
    // Atomically rename to target file
    std::fs::rename(&temp_path, path)?;
    
    Ok(())
}
```

The `@file` function in Rust provides comprehensive file system operations with security, performance, and reliability features. 