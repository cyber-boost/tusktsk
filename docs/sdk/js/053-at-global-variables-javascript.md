# @global Variables - JavaScript

## Overview

The `@global` function in TuskLang provides access to global variables and shared state across your application. This is essential for JavaScript applications that need to maintain consistent state, share configuration, or manage application-wide settings that persist across different modules and requests.

## Basic Syntax

```tsk
# Simple global variable access
app_name: @global("app_name", "TuskLang App")
app_version: @global("app_version", "1.0.0")
debug_mode: @global("debug_mode", false)

# Global configuration objects
app_config: @global("app_config", {"debug": true, "log_level": "info"})
database_config: @global("database_config", {"host": "localhost", "port": 5432})
```

## JavaScript Integration

### Node.js Global Integration

```javascript
const tusk = require('tusklang');

// Load configuration with global variables
const config = tusk.load('globals.tsk');

// Access global variables
console.log(config.app_name); // "TuskLang App"
console.log(config.app_version); // "1.0.0"
console.log(config.debug_mode); // false

// Use global variables in application
const appConfig = {
  name: config.app_name,
  version: config.app_version,
  debug: config.debug_mode
};

// Dynamic global variable access
const customGlobal = await tusk.global("custom_var", "default_value");
const existingGlobal = await tusk.global("existing_var"); // Gets existing value
```

### Browser Global Integration

```javascript
// Load TuskLang configuration
const config = await tusk.load('globals.tsk');

// Use global variables in frontend
class GlobalStateManager {
  constructor() {
    this.appName = config.app_name;
    this.appVersion = config.app_version;
    this.debugMode = config.debug_mode;
  }
  
  async getGlobalVariable(name, fallback = null) {
    try {
      return await tusk.global(name, fallback);
    } catch (error) {
      console.warn(`Global variable ${name} not found, using fallback`);
      return fallback;
    }
  }
  
  async setGlobalVariable(name, value) {
    await tusk.global.set(name, value);
  }
  
  getAppInfo() {
    return {
      name: this.appName,
      version: this.appVersion,
      debug: this.debugMode
    };
  }
}
```

## Advanced Usage

### Global State Management

```tsk
# Global application state
app_state: @global("app_state", {
  "users_online": 0,
  "total_requests": 0,
  "last_restart": null,
  "maintenance_mode": false
})

# Global configuration with environment overrides
global_config: @global("global_config", {
  "api_timeout": @env("NODE_ENV") == "production" ? 30000 : 5000,
  "cache_duration": @env("NODE_ENV") == "production" ? "1h" : "5m",
  "log_level": @env("NODE_ENV") == "production" ? "error" : "debug"
})
```

### Cross-Module Communication

```tsk
# Shared data between modules
shared_data: @global("shared_data", {
  "user_sessions": {},
  "cache_entries": {},
  "active_connections": 0
})

# Module-specific globals
module_globals: {
  "auth_module": @global("auth_module", {"active_sessions": 0, "failed_attempts": 0}),
  "api_module": @global("api_module", {"requests_per_minute": 0, "error_count": 0}),
  "db_module": @global("db_module", {"connection_pool_size": 10, "active_queries": 0})
}
```

### Global Event System

```tsk
# Global event configuration
event_config: @global("event_config", {
  "listeners": {},
  "max_listeners": 100,
  "event_history": []
})

# Global metrics
global_metrics: @global("global_metrics", {
  "start_time": @date.now(),
  "total_requests": 0,
  "total_errors": 0,
  "average_response_time": 0
})
```

## JavaScript Implementation

### Custom Global Manager

```javascript
class TuskGlobalManager {
  constructor() {
    this.globals = new Map();
    this.watchers = new Map();
    this.persistentStorage = null;
  }
  
  async get(name, defaultValue = null) {
    // Check if global exists
    if (this.globals.has(name)) {
      return this.globals.get(name);
    }
    
    // Try to load from persistent storage
    if (this.persistentStorage) {
      try {
        const stored = await this.persistentStorage.get(`global_${name}`);
        if (stored !== null) {
          this.globals.set(name, stored);
          return stored;
        }
      } catch (error) {
        console.warn(`Failed to load global ${name} from storage:`, error);
      }
    }
    
    // Use default value
    if (defaultValue !== null) {
      await this.set(name, defaultValue);
      return defaultValue;
    }
    
    return null;
  }
  
  async set(name, value) {
    const oldValue = this.globals.get(name);
    
    // Set the global value
    this.globals.set(name, value);
    
    // Persist to storage if available
    if (this.persistentStorage) {
      try {
        await this.persistentStorage.set(`global_${name}`, value);
      } catch (error) {
        console.warn(`Failed to persist global ${name}:`, error);
      }
    }
    
    // Notify watchers
    this.notifyWatchers(name, value, oldValue);
    
    return value;
  }
  
  async delete(name) {
    const existed = this.globals.has(name);
    
    if (existed) {
      this.globals.delete(name);
      
      // Remove from persistent storage
      if (this.persistentStorage) {
        try {
          await this.persistentStorage.delete(`global_${name}`);
        } catch (error) {
          console.warn(`Failed to delete global ${name} from storage:`, error);
        }
      }
      
      // Notify watchers
      this.notifyWatchers(name, null, this.globals.get(name));
    }
    
    return existed;
  }
  
  async update(name, updater) {
    const currentValue = await this.get(name);
    const newValue = typeof updater === 'function' ? updater(currentValue) : updater;
    
    return await this.set(name, newValue);
  }
  
  watch(name, callback) {
    if (!this.watchers.has(name)) {
      this.watchers.set(name, new Set());
    }
    
    this.watchers.get(name).add(callback);
    
    // Return unwatch function
    return () => {
      const watchers = this.watchers.get(name);
      if (watchers) {
        watchers.delete(callback);
        if (watchers.size === 0) {
          this.watchers.delete(name);
        }
      }
    };
  }
  
  notifyWatchers(name, newValue, oldValue) {
    const watchers = this.watchers.get(name);
    if (watchers) {
      watchers.forEach(callback => {
        try {
          callback(newValue, oldValue, name);
        } catch (error) {
          console.error(`Error in global watcher for ${name}:`, error);
        }
      });
    }
  }
  
  getAll() {
    return Object.fromEntries(this.globals);
  }
  
  clear() {
    this.globals.clear();
    this.watchers.clear();
  }
  
  setPersistentStorage(storage) {
    this.persistentStorage = storage;
  }
}
```

### TypeScript Support

```typescript
interface GlobalWatcher<T = any> {
  (newValue: T, oldValue: T, name: string): void;
}

interface PersistentStorage {
  get(key: string): Promise<any>;
  set(key: string, value: any): Promise<void>;
  delete(key: string): Promise<void>;
}

class TypedGlobalManager {
  private globals: Map<string, any>;
  private watchers: Map<string, Set<GlobalWatcher>>;
  private persistentStorage: PersistentStorage | null;
  
  constructor() {
    this.globals = new Map();
    this.watchers = new Map();
    this.persistentStorage = null;
  }
  
  async get<T>(name: string, defaultValue?: T): Promise<T | null> {
    // Implementation similar to JavaScript version
    return defaultValue || null;
  }
  
  async set<T>(name: string, value: T): Promise<T> {
    // Implementation similar to JavaScript version
    return value;
  }
  
  async delete(name: string): Promise<boolean> {
    // Implementation similar to JavaScript version
    return false;
  }
  
  async update<T>(name: string, updater: (currentValue: T) => T): Promise<T> {
    // Implementation similar to JavaScript version
    return {} as T;
  }
  
  watch<T>(name: string, callback: GlobalWatcher<T>): () => void {
    // Implementation similar to JavaScript version
    return () => {};
  }
  
  getAll(): Record<string, any> {
    return Object.fromEntries(this.globals);
  }
  
  clear(): void {
    this.globals.clear();
    this.watchers.clear();
  }
  
  setPersistentStorage(storage: PersistentStorage): void {
    this.persistentStorage = storage;
  }
}
```

## Real-World Examples

### Application State Management

```tsk
# Application-wide state
app_global_state: @global("app_state", {
  "startup_time": @date.now(),
  "total_users": 0,
  "active_sessions": 0,
  "maintenance_mode": false,
  "feature_flags": {
    "new_ui": true,
    "beta_features": false,
    "analytics": true
  }
})

# User session management
session_globals: @global("sessions", {
  "active_sessions": {},
  "session_timeout": 3600,
  "max_sessions_per_user": 5
})
```

### Configuration Management

```tsk
# Global configuration
global_app_config: @global("app_config", {
  "name": "TuskLang Application",
  "version": "1.0.0",
  "environment": @env("NODE_ENV", "development"),
  "debug": @env("DEBUG", "false"),
  "features": {
    "caching": true,
    "monitoring": true,
    "logging": true
  }
})

# Database configuration
global_db_config: @global("database_config", {
  "host": @env("DB_HOST", "localhost"),
  "port": @env("DB_PORT", "5432"),
  "name": @env("DB_NAME", "myapp"),
  "pool_size": @env("DB_POOL_SIZE", "10"),
  "timeout": @env("DB_TIMEOUT", "30000")
})
```

### Performance Monitoring

```tsk
# Global performance metrics
performance_globals: @global("performance", {
  "start_time": @date.now(),
  "total_requests": 0,
  "total_errors": 0,
  "average_response_time": 0,
  "peak_memory_usage": 0,
  "uptime": 0
})

# Real-time statistics
realtime_stats: @global("stats", {
  "requests_per_minute": 0,
  "errors_per_minute": 0,
  "active_connections": 0,
  "cache_hit_rate": 0
})
```

## Performance Considerations

### Global Variable Caching

```tsk
# Cache global variables for performance
cached_globals: @cache("5m", {
  "app_name": @global("app_name"),
  "app_version": @global("app_version"),
  "debug_mode": @global("debug_mode")
})
```

### Efficient Global Management

```javascript
// Implement efficient global variable management
class EfficientGlobalManager extends TuskGlobalManager {
  constructor() {
    super();
    this.accessCount = new Map();
    this.cacheTimeout = 60000; // 1 minute
  }
  
  async get(name, defaultValue = null) {
    // Track access count
    this.accessCount.set(name, (this.accessCount.get(name) || 0) + 1);
    
    return await super.get(name, defaultValue);
  }
  
  async set(name, value) {
    // Reset access count when setting
    this.accessCount.set(name, 0);
    
    return await super.set(name, value);
  }
  
  getMostAccessed() {
    return Array.from(this.accessCount.entries())
      .sort(([, a], [, b]) => b - a)
      .slice(0, 10)
      .map(([name, count]) => ({ name, count }));
  }
  
  cleanupUnused() {
    const now = Date.now();
    const unused = [];
    
    for (const [name, lastAccess] of this.lastAccessTime) {
      if (now - lastAccess > this.cacheTimeout) {
        unused.push(name);
      }
    }
    
    unused.forEach(name => {
      this.globals.delete(name);
      this.accessCount.delete(name);
    });
    
    return unused.length;
  }
}
```

## Security Notes

- **Access Control**: Implement proper access controls for global variables
- **Validation**: Validate global variable values before setting
- **Persistence**: Secure persistent storage for sensitive globals

```javascript
// Secure global variable management
class SecureGlobalManager extends TuskGlobalManager {
  constructor() {
    super();
    this.readOnlyGlobals = new Set(['app_name', 'app_version']);
    this.sensitiveGlobals = new Set(['api_key', 'database_password']);
  }
  
  async set(name, value) {
    // Check if global is read-only
    if (this.readOnlyGlobals.has(name)) {
      throw new Error(`Global variable ${name} is read-only`);
    }
    
    // Validate sensitive globals
    if (this.sensitiveGlobals.has(name)) {
      value = this.sanitizeSensitiveValue(value);
    }
    
    return await super.set(name, value);
  }
  
  sanitizeSensitiveValue(value) {
    if (typeof value === 'string' && value.length > 0) {
      // Mask sensitive values in logs
      const masked = '*'.repeat(Math.min(value.length, 8));
      console.log(`Setting sensitive global: ${masked}`);
    }
    
    return value;
  }
  
  async get(name, defaultValue = null) {
    const value = await super.get(name, defaultValue);
    
    // Don't log sensitive values
    if (this.sensitiveGlobals.has(name)) {
      console.log(`Accessing sensitive global: ${name} (value masked)`);
    }
    
    return value;
  }
}
```

## Best Practices

1. **Naming**: Use descriptive names for global variables
2. **Validation**: Always validate global variable values
3. **Persistence**: Use persistent storage for important globals
4. **Watching**: Use watchers for reactive global changes
5. **Cleanup**: Clean up unused global variables
6. **Documentation**: Document all global variables and their purpose

## Next Steps

- Learn about [@debug mode](./054-at-debug-mode-javascript.md) for debugging capabilities
- Explore [@operator chaining](./055-at-operator-chaining-javascript.md) for complex operations
- Master [@operator nesting](./056-at-operator-nesting-javascript.md) for advanced patterns 