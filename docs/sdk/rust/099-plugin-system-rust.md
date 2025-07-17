# Plugin System in TuskLang for Rust

The plugin system in TuskLang for Rust provides a powerful, type-safe way to extend application functionality through dynamic loading and trait-based interfaces, leveraging Rust's trait system and dynamic linking capabilities.

## Basic Plugin Architecture

```rust
// Core plugin trait
pub trait Plugin: Send + Sync {
    fn name(&self) -> &str;
    fn version(&self) -> &str;
    fn description(&self) -> &str;
    fn initialize(&mut self, context: &PluginContext) -> Result<(), Box<dyn std::error::Error + Send + Sync>>;
    fn shutdown(&mut self) -> Result<(), Box<dyn std::error::Error + Send + Sync>>;
}

// Plugin context for shared resources
pub struct PluginContext {
    pub config: std::sync::Arc<tokio::sync::RwLock<serde_json::Value>>,
    pub database: std::sync::Arc<Database>,
    pub cache: std::sync::Arc<Cache>,
    pub logger: std::sync::Arc<Logger>,
}

// Plugin registry
pub struct PluginRegistry {
    plugins: std::collections::HashMap<String, Box<dyn Plugin>>,
    context: PluginContext,
}

impl PluginRegistry {
    pub fn new(context: PluginContext) -> Self {
        Self {
            plugins: std::collections::HashMap::new(),
            context,
        }
    }
    
    pub fn register_plugin(&mut self, plugin: Box<dyn Plugin>) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        let name = plugin.name().to_string();
        
        // Initialize the plugin
        plugin.initialize(&self.context)?;
        
        self.plugins.insert(name.clone(), plugin);
        @log.info(&format!("Plugin '{}' registered successfully", name));
        
        Ok(())
    }
    
    pub fn get_plugin<T: Plugin + 'static>(&self, name: &str) -> Option<&T> {
        self.plugins.get(name)?.as_any().downcast_ref::<T>()
    }
    
    pub fn list_plugins(&self) -> Vec<PluginInfo> {
        self.plugins
            .iter()
            .map(|(name, plugin)| PluginInfo {
                name: name.clone(),
                version: plugin.version().to_string(),
                description: plugin.description().to_string(),
            })
            .collect()
    }
    
    pub async fn shutdown_all(&mut self) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        for (name, plugin) in self.plugins.iter_mut() {
            if let Err(e) = plugin.shutdown() {
                @log.error(&format!("Failed to shutdown plugin '{}': {}", name, e));
            }
        }
        Ok(())
    }
}
```

## Dynamic Plugin Loading

```rust
// Dynamic plugin loader using libloading
use libloading::{Library, Symbol};

pub struct DynamicPluginLoader {
    libraries: Vec<Library>,
}

impl DynamicPluginLoader {
    pub fn new() -> Self {
        Self {
            libraries: Vec::new(),
        }
    }
    
    pub unsafe fn load_plugin(&mut self, path: &str) -> Result<Box<dyn Plugin>, Box<dyn std::error::Error + Send + Sync>> {
        let library = Library::new(path)?;
        
        // Get the plugin creation function
        let create_plugin: Symbol<fn() -> Box<dyn Plugin>> = library.get(b"create_plugin")?;
        let plugin = create_plugin();
        
        // Store the library to keep it loaded
        self.libraries.push(library);
        
        Ok(plugin)
    }
    
    pub fn load_plugins_from_directory(&mut self, directory: &str) -> Result<Vec<Box<dyn Plugin>>, Box<dyn std::error::Error + Send + Sync>> {
        let mut plugins = Vec::new();
        
        for entry in std::fs::read_dir(directory)? {
            let entry = entry?;
            let path = entry.path();
            
            if path.extension().and_then(|s| s.to_str()) == Some("so") {
                match unsafe { self.load_plugin(path.to_str().unwrap()) } {
                    Ok(plugin) => plugins.push(plugin),
                    Err(e) => {
                        @log.error(&format!("Failed to load plugin from {:?}: {}", path, e));
                    }
                }
            }
        }
        
        Ok(plugins)
    }
}

// Plugin creation function signature (must be exported by plugins)
#[no_mangle]
pub extern "C" fn create_plugin() -> Box<dyn Plugin> {
    Box::new(MyPlugin::new())
}
```

## Specialized Plugin Traits

```rust
// Database plugin trait
pub trait DatabasePlugin: Plugin {
    async fn execute_query(&self, query: &str, params: &[serde_json::Value]) 
        -> Result<serde_json::Value, Box<dyn std::error::Error + Send + Sync>>;
    
    async fn create_table(&self, table_name: &str, schema: &str) 
        -> Result<(), Box<dyn std::error::Error + Send + Sync>>;
    
    async fn backup_database(&self, backup_path: &str) 
        -> Result<(), Box<dyn std::error::Error + Send + Sync>>;
}

// HTTP plugin trait
pub trait HttpPlugin: Plugin {
    async fn handle_request(&self, request: &HttpRequest) 
        -> Result<HttpResponse, Box<dyn std::error::Error + Send + Sync>>;
    
    fn register_routes(&self, router: &mut Router);
    
    fn middleware(&self) -> Option<Box<dyn Middleware + Send + Sync>>;
}

// Authentication plugin trait
pub trait AuthPlugin: Plugin {
    async fn authenticate(&self, credentials: &AuthCredentials) 
        -> Result<AuthResult, Box<dyn std::error::Error + Send + Sync>>;
    
    async fn authorize(&self, user: &User, resource: &str, action: &str) 
        -> Result<bool, Box<dyn std::error::Error + Send + Sync>>;
    
    fn get_user_permissions(&self, user_id: u32) -> Result<Vec<String>, Box<dyn std::error::Error + Send + Sync>>;
}

// Notification plugin trait
pub trait NotificationPlugin: Plugin {
    async fn send_notification(&self, notification: &Notification) 
        -> Result<(), Box<dyn std::error::Error + Send + Sync>>;
    
    fn supported_channels(&self) -> Vec<String>;
    
    async fn test_connection(&self) -> Result<bool, Box<dyn std::error::Error + Send + Sync>>;
}
```

## Plugin Implementation Examples

```rust
// Database plugin implementation
pub struct PostgresPlugin {
    connection_pool: Option<sqlx::PgPool>,
    config: serde_json::Value,
}

impl PostgresPlugin {
    pub fn new() -> Self {
        Self {
            connection_pool: None,
            config: serde_json::Value::Null,
        }
    }
}

impl Plugin for PostgresPlugin {
    fn name(&self) -> &str { "postgres" }
    fn version(&self) -> &str { "1.0.0" }
    fn description(&self) -> &str { "PostgreSQL database plugin" }
    
    fn initialize(&mut self, context: &PluginContext) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        let config = context.config.read().await;
        let db_url = config["database"]["postgres_url"].as_str()
            .ok_or("Missing postgres_url configuration")?;
        
        let pool = sqlx::PgPool::connect(db_url).await?;
        self.connection_pool = Some(pool);
        self.config = config.clone();
        
        @log.info("PostgreSQL plugin initialized successfully");
        Ok(())
    }
    
    fn shutdown(&mut self) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        if let Some(pool) = &self.connection_pool {
            pool.close().await;
        }
        @log.info("PostgreSQL plugin shutdown successfully");
        Ok(())
    }
}

impl DatabasePlugin for PostgresPlugin {
    async fn execute_query(&self, query: &str, params: &[serde_json::Value]) 
        -> Result<serde_json::Value, Box<dyn std::error::Error + Send + Sync>> {
        
        let pool = self.connection_pool.as_ref()
            .ok_or("Database not initialized")?;
        
        let result = sqlx::query(query)
            .execute(pool)
            .await?;
        
        Ok(serde_json::to_value(result)?)
    }
    
    async fn create_table(&self, table_name: &str, schema: &str) 
        -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        
        let pool = self.connection_pool.as_ref()
            .ok_or("Database not initialized")?;
        
        sqlx::query(schema)
            .execute(pool)
            .await?;
        
        @log.info(&format!("Table '{}' created successfully", table_name));
        Ok(())
    }
    
    async fn backup_database(&self, backup_path: &str) 
        -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        
        let config = &self.config;
        let db_name = config["database"]["name"].as_str()
            .ok_or("Missing database name")?;
        
        let output = std::process::Command::new("pg_dump")
            .arg("-h").arg(&config["database"]["host"])
            .arg("-U").arg(&config["database"]["user"])
            .arg("-d").arg(db_name)
            .arg("-f").arg(backup_path)
            .output()?;
        
        if output.status.success() {
            @log.info(&format!("Database backup created at: {}", backup_path));
            Ok(())
        } else {
            Err(format!("Backup failed: {}", String::from_utf8_lossy(&output.stderr)).into())
        }
    }
}

// HTTP plugin implementation
pub struct ApiPlugin {
    routes: Vec<Route>,
    middleware: Option<Box<dyn Middleware + Send + Sync>>,
}

impl ApiPlugin {
    pub fn new() -> Self {
        Self {
            routes: Vec::new(),
            middleware: None,
        }
    }
}

impl Plugin for ApiPlugin {
    fn name(&self) -> &str { "api" }
    fn version(&self) -> &str { "1.0.0" }
    fn description(&self) -> &str { "REST API plugin" }
    
    fn initialize(&mut self, _context: &PluginContext) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        // Register default routes
        self.routes.push(Route::new("GET", "/api/health", Self::health_check));
        self.routes.push(Route::new("GET", "/api/version", Self::get_version));
        
        @log.info("API plugin initialized successfully");
        Ok(())
    }
    
    fn shutdown(&mut self) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        @log.info("API plugin shutdown successfully");
        Ok(())
    }
}

impl HttpPlugin for ApiPlugin {
    async fn handle_request(&self, request: &HttpRequest) 
        -> Result<HttpResponse, Box<dyn std::error::Error + Send + Sync>> {
        
        // Find matching route
        for route in &self.routes {
            if route.matches(request) {
                return route.handler(request).await;
            }
        }
        
        // Return 404 if no route matches
        Ok(HttpResponse::not_found())
    }
    
    fn register_routes(&self, router: &mut Router) {
        for route in &self.routes {
            router.add_route(route.clone());
        }
    }
    
    fn middleware(&self) -> Option<Box<dyn Middleware + Send + Sync>> {
        self.middleware.clone()
    }
}

impl ApiPlugin {
    async fn health_check(_request: &HttpRequest) -> Result<HttpResponse, Box<dyn std::error::Error + Send + Sync>> {
        Ok(HttpResponse::json(serde_json::json!({
            "status": "healthy",
            "timestamp": @date.now()
        })))
    }
    
    async fn get_version(_request: &HttpRequest) -> Result<HttpResponse, Box<dyn std::error::Error + Send + Sync>> {
        Ok(HttpResponse::json(serde_json::json!({
            "version": env!("CARGO_PKG_VERSION"),
            "name": env!("CARGO_PKG_NAME")
        })))
    }
}
```

## Plugin Configuration Management

```rust
// Plugin configuration system
pub struct PluginConfig {
    plugins: std::collections::HashMap<String, PluginSettings>,
}

#[derive(Clone, serde::Serialize, serde::Deserialize)]
pub struct PluginSettings {
    pub enabled: bool,
    pub config: serde_json::Value,
    pub dependencies: Vec<String>,
    pub load_order: Option<u32>,
}

impl PluginConfig {
    pub fn from_file(path: &str) -> Result<Self, Box<dyn std::error::Error + Send + Sync>> {
        let content = @file.read(path)?;
        let config: PluginConfig = serde_json::from_str(&content)?;
        Ok(config)
    }
    
    pub fn from_tusk_config(config: &str) -> Result<Self, Box<dyn std::error::Error + Send + Sync>> {
        let config_map = @json_decode(config);
        let mut plugin_config = PluginConfig {
            plugins: std::collections::HashMap::new(),
        };
        
        for (name, settings) in config_map {
            if let Ok(plugin_settings) = serde_json::from_value(settings) {
                plugin_config.plugins.insert(name, plugin_settings);
            }
        }
        
        Ok(plugin_config)
    }
    
    pub fn get_plugin_settings(&self, name: &str) -> Option<&PluginSettings> {
        self.plugins.get(name)
    }
    
    pub fn is_plugin_enabled(&self, name: &str) -> bool {
        self.plugins.get(name)
            .map(|settings| settings.enabled)
            .unwrap_or(false)
    }
    
    pub fn get_load_order(&self) -> Vec<String> {
        let mut plugins: Vec<_> = self.plugins.iter().collect();
        plugins.sort_by_key(|(_, settings)| settings.load_order.unwrap_or(u32::MAX));
        plugins.into_iter().map(|(name, _)| name.clone()).collect()
    }
}

// Usage with TuskLang configuration
let plugin_config = PluginConfig::from_tusk_config(r#"{
    "postgres": {
        "enabled": true,
        "config": {
            "host": "localhost",
            "port": 5432,
            "database": "mydb",
            "user": "postgres"
        },
        "dependencies": [],
        "load_order": 1
    },
    "api": {
        "enabled": true,
        "config": {
            "port": 8080,
            "host": "0.0.0.0"
        },
        "dependencies": ["postgres"],
        "load_order": 2
    },
    "auth": {
        "enabled": true,
        "config": {
            "jwt_secret": "@env.get('JWT_SECRET')",
            "session_timeout": 3600
        },
        "dependencies": ["postgres"],
        "load_order": 3
    }
}"#)?;
```

## Plugin Dependency Management

```rust
// Plugin dependency resolver
pub struct DependencyResolver {
    plugins: std::collections::HashMap<String, PluginSettings>,
}

impl DependencyResolver {
    pub fn new(plugins: std::collections::HashMap<String, PluginSettings>) -> Self {
        Self { plugins }
    }
    
    pub fn resolve_load_order(&self) -> Result<Vec<String>, Box<dyn std::error::Error + Send + Sync>> {
        let mut visited = std::collections::HashSet::new();
        let mut temp_visited = std::collections::HashSet::new();
        let mut order = Vec::new();
        
        for plugin_name in self.plugins.keys() {
            if !visited.contains(plugin_name) {
                self.dfs(plugin_name, &mut visited, &mut temp_visited, &mut order)?;
            }
        }
        
        Ok(order)
    }
    
    fn dfs(&self, plugin_name: &str, visited: &mut std::collections::HashSet<String>, 
           temp_visited: &mut std::collections::HashSet<String>, order: &mut Vec<String>) 
        -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        
        if temp_visited.contains(plugin_name) {
            return Err(format!("Circular dependency detected: {}", plugin_name).into());
        }
        
        if visited.contains(plugin_name) {
            return Ok(());
        }
        
        temp_visited.insert(plugin_name.to_string());
        
        if let Some(settings) = self.plugins.get(plugin_name) {
            for dependency in &settings.dependencies {
                self.dfs(dependency, visited, temp_visited, order)?;
            }
        }
        
        temp_visited.remove(plugin_name);
        visited.insert(plugin_name.to_string());
        order.push(plugin_name.to_string());
        
        Ok(())
    }
    
    pub fn validate_dependencies(&self) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        for (plugin_name, settings) in &self.plugins {
            for dependency in &settings.dependencies {
                if !self.plugins.contains_key(dependency) {
                    return Err(format!("Plugin '{}' depends on '{}' which is not available", 
                                     plugin_name, dependency).into());
                }
            }
        }
        Ok(())
    }
}
```

## Plugin Hot Reloading

```rust
// Hot reloading plugin manager
pub struct HotReloadPluginManager {
    registry: PluginRegistry,
    config: PluginConfig,
    plugin_paths: std::collections::HashMap<String, String>,
    file_watcher: notify::Watcher,
    reload_tx: tokio::sync::mpsc::UnboundedSender<ReloadEvent>,
}

impl HotReloadPluginManager {
    pub fn new(registry: PluginRegistry, config: PluginConfig) -> Result<Self, Box<dyn std::error::Error + Send + Sync>> {
        let (reload_tx, reload_rx) = tokio::sync::mpsc::unbounded_channel();
        
        let mut watcher = notify::recommended_watcher(move |res| {
            if let Ok(event) = res {
                if let Err(e) = reload_tx.send(ReloadEvent::FileChanged(event)) {
                    @log.error(&format!("Failed to send reload event: {}", e));
                }
            }
        })?;
        
        Ok(Self {
            registry,
            config,
            plugin_paths: std::collections::HashMap::new(),
            file_watcher: watcher,
            reload_tx,
        })
    }
    
    pub async fn start_hot_reload(&mut self) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        // Watch plugin directories
        for (plugin_name, settings) in &self.config.plugins {
            if settings.enabled {
                if let Some(plugin_path) = self.get_plugin_path(plugin_name) {
                    self.file_watcher.watch(&plugin_path, notify::RecursiveMode::NonRecursive)?;
                    self.plugin_paths.insert(plugin_name.clone(), plugin_path);
                }
            }
        }
        
        // Start reload handler
        tokio::spawn(async move {
            self.handle_reload_events().await;
        });
        
        Ok(())
    }
    
    async fn handle_reload_events(&mut self) {
        while let Some(event) = self.reload_rx.recv().await {
            match event {
                ReloadEvent::FileChanged(notify_event) => {
                    for path in notify_event.paths {
                        if let Some(plugin_name) = self.get_plugin_name_from_path(&path) {
                            @log.info(&format!("Reloading plugin: {}", plugin_name));
                            
                            if let Err(e) = self.reload_plugin(&plugin_name).await {
                                @log.error(&format!("Failed to reload plugin {}: {}", plugin_name, e));
                            }
                        }
                    }
                }
            }
        }
    }
    
    async fn reload_plugin(&mut self, plugin_name: &str) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        // Shutdown existing plugin
        if let Some(plugin) = self.registry.plugins.remove(plugin_name) {
            plugin.shutdown()?;
        }
        
        // Load new plugin
        if let Some(plugin_path) = self.plugin_paths.get(plugin_name) {
            let mut loader = DynamicPluginLoader::new();
            let plugin = unsafe { loader.load_plugin(plugin_path)? };
            self.registry.register_plugin(plugin)?;
        }
        
        Ok(())
    }
}

enum ReloadEvent {
    FileChanged(notify::Event),
}
```

## Plugin Testing Framework

```rust
// Plugin testing utilities
pub struct PluginTestFramework {
    test_context: PluginContext,
}

impl PluginTestFramework {
    pub fn new() -> Self {
        let config = std::sync::Arc::new(tokio::sync::RwLock::new(serde_json::json!({
            "test_mode": true,
            "database": {
                "url": "sqlite::memory:"
            }
        })));
        
        let test_context = PluginContext {
            config,
            database: std::sync::Arc::new(Database::new_test()),
            cache: std::sync::Arc::new(Cache::new_test()),
            logger: std::sync::Arc::new(Logger::new_test()),
        };
        
        Self { test_context }
    }
    
    pub async fn test_plugin<P: Plugin>(&self, plugin: &mut P) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        // Test initialization
        plugin.initialize(&self.test_context)?;
        
        // Test plugin functionality
        self.test_plugin_functionality(plugin).await?;
        
        // Test shutdown
        plugin.shutdown()?;
        
        Ok(())
    }
    
    async fn test_plugin_functionality<P: Plugin>(&self, plugin: &P) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        // Test database plugin
        if let Some(db_plugin) = plugin.as_any().downcast_ref::<dyn DatabasePlugin>() {
            self.test_database_plugin(db_plugin).await?;
        }
        
        // Test HTTP plugin
        if let Some(http_plugin) = plugin.as_any().downcast_ref::<dyn HttpPlugin>() {
            self.test_http_plugin(http_plugin).await?;
        }
        
        // Test auth plugin
        if let Some(auth_plugin) = plugin.as_any().downcast_ref::<dyn AuthPlugin>() {
            self.test_auth_plugin(auth_plugin).await?;
        }
        
        Ok(())
    }
    
    async fn test_database_plugin(&self, plugin: &dyn DatabasePlugin) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        // Test query execution
        let result = plugin.execute_query("SELECT 1 as test", &[]).await?;
        assert!(result["test"].as_i64() == Some(1));
        
        // Test table creation
        plugin.create_table("test_table", "CREATE TABLE test_table (id INTEGER PRIMARY KEY)").await?;
        
        Ok(())
    }
    
    async fn test_http_plugin(&self, plugin: &dyn HttpPlugin) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        let request = HttpRequest::new("GET", "/test");
        let response = plugin.handle_request(&request).await?;
        assert!(response.status_code() == 200 || response.status_code() == 404);
        
        Ok(())
    }
    
    async fn test_auth_plugin(&self, plugin: &dyn AuthPlugin) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        let credentials = AuthCredentials::new("test_user", "test_password");
        let result = plugin.authenticate(&credentials).await?;
        assert!(result.is_success() || result.is_failure());
        
        Ok(())
    }
}

#[cfg(test)]
mod plugin_tests {
    use super::*;
    
    #[tokio::test]
    async fn test_postgres_plugin() {
        let framework = PluginTestFramework::new();
        let mut plugin = PostgresPlugin::new();
        
        framework.test_plugin(&mut plugin).await.unwrap();
    }
    
    #[tokio::test]
    async fn test_api_plugin() {
        let framework = PluginTestFramework::new();
        let mut plugin = ApiPlugin::new();
        
        framework.test_plugin(&mut plugin).await.unwrap();
    }
    
    #[tokio::test]
    async fn test_dependency_resolution() {
        let config = PluginConfig::from_tusk_config(r#"{
            "plugin_a": {
                "enabled": true,
                "dependencies": ["plugin_b"],
                "load_order": 2
            },
            "plugin_b": {
                "enabled": true,
                "dependencies": [],
                "load_order": 1
            }
        }"#).unwrap();
        
        let resolver = DependencyResolver::new(config.plugins);
        let order = resolver.resolve_load_order().unwrap();
        
        assert_eq!(order, vec!["plugin_b", "plugin_a"]);
    }
}
```

## Plugin Security

```rust
// Plugin security manager
pub struct PluginSecurityManager {
    allowed_apis: std::collections::HashSet<String>,
    sandbox_config: SandboxConfig,
}

#[derive(Clone)]
pub struct SandboxConfig {
    pub allow_network: bool,
    pub allow_file_system: bool,
    pub allow_database: bool,
    pub max_memory_mb: usize,
    pub max_execution_time_sec: u64,
}

impl PluginSecurityManager {
    pub fn new() -> Self {
        let mut allowed_apis = std::collections::HashSet::new();
        allowed_apis.insert("log".to_string());
        allowed_apis.insert("config".to_string());
        
        Self {
            allowed_apis,
            sandbox_config: SandboxConfig {
                allow_network: false,
                allow_file_system: false,
                allow_database: false,
                max_memory_mb: 100,
                max_execution_time_sec: 30,
            },
        }
    }
    
    pub fn validate_plugin(&self, plugin: &dyn Plugin) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        // Check plugin signature
        self.verify_plugin_signature(plugin)?;
        
        // Validate plugin permissions
        self.validate_plugin_permissions(plugin)?;
        
        // Check for malicious code patterns
        self.scan_for_malicious_code(plugin)?;
        
        Ok(())
    }
    
    fn verify_plugin_signature(&self, _plugin: &dyn Plugin) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        // Implement digital signature verification
        // This would check if the plugin was signed by a trusted authority
        Ok(())
    }
    
    fn validate_plugin_permissions(&self, plugin: &dyn Plugin) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        // Check if plugin requests permissions it shouldn't have
        let plugin_name = plugin.name();
        
        // Example: Only allow database plugins to access database
        if plugin_name.contains("database") && !self.sandbox_config.allow_database {
            return Err("Plugin requires database access but it's not allowed".into());
        }
        
        Ok(())
    }
    
    fn scan_for_malicious_code(&self, _plugin: &dyn Plugin) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        // Implement code scanning for malicious patterns
        // This could include checking for:
        // - Unsafe code blocks
        // - Network access attempts
        // - File system access
        // - System calls
        Ok(())
    }
    
    pub fn create_sandboxed_context(&self, context: PluginContext) -> SandboxedPluginContext {
        SandboxedPluginContext {
            inner: context,
            security_manager: self.clone(),
        }
    }
}

pub struct SandboxedPluginContext {
    inner: PluginContext,
    security_manager: PluginSecurityManager,
}

impl SandboxedPluginContext {
    pub async fn execute_with_timeout<F, T>(&self, timeout: std::time::Duration, operation: F) 
        -> Result<T, Box<dyn std::error::Error + Send + Sync>>
    where
        F: std::future::Future<Output = Result<T, Box<dyn std::error::Error + Send + Sync>>> + Send,
        T: Send,
    {
        tokio::time::timeout(timeout, operation).await
            .map_err(|_| "Operation timed out".into())?
    }
    
    pub fn check_memory_usage(&self) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
        let current_memory = @memory.get_usage();
        let max_memory = self.security_manager.sandbox_config.max_memory_mb * 1024 * 1024;
        
        if current_memory > max_memory {
            return Err("Memory usage exceeded limit".into());
        }
        
        Ok(())
    }
}
```

This comprehensive guide covers Rust-specific plugin system patterns, ensuring type safety, security, and integration with Rust's trait system while maintaining the power and flexibility of TuskLang's plugin capabilities. 