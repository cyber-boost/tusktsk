# @debug Mode - JavaScript

## Overview

The `@debug` function in TuskLang provides comprehensive debugging capabilities that allow you to inspect, log, and trace execution flow in your applications. This is essential for JavaScript applications that need detailed debugging information, performance profiling, and error tracking during development and troubleshooting.

## Basic Syntax

```tsk
# Simple debug mode activation
debug_enabled: @debug("enabled", true)
debug_level: @debug("level", "info")
debug_output: @debug("output", "console")

# Debug configuration
debug_config: @debug("config", {
  "enabled": true,
  "level": "debug",
  "output": "file",
  "file_path": "/var/log/debug.log"
})
```

## JavaScript Integration

### Node.js Debug Integration

```javascript
const tusk = require('tusklang');

// Load configuration with debug settings
const config = tusk.load('debug.tsk');

// Access debug configuration
console.log(config.debug_enabled); // true
console.log(config.debug_level); // "info"
console.log(config.debug_output); // "console"

// Use debug configuration in application
const debugConfig = {
  enabled: config.debug_enabled,
  level: config.debug_level,
  output: config.debug_output
};

// Dynamic debug operations
await tusk.debug.log("Application started", { level: "info" });
await tusk.debug.trace("Function execution", { function: "processData" });
await tusk.debug.profile("Database query", { query: "SELECT * FROM users" });
```

### Browser Debug Integration

```javascript
// Load TuskLang configuration
const config = await tusk.load('debug.tsk');

// Use debug configuration in frontend
class DebugManager {
  constructor() {
    this.enabled = config.debug_enabled;
    this.level = config.debug_level;
    this.output = config.debug_output;
  }
  
  async log(message, data = {}) {
    if (!this.enabled) return;
    
    await tusk.debug.log(message, {
      level: this.level,
      data: data,
      timestamp: Date.now()
    });
  }
  
  async trace(functionName, args = {}) {
    if (!this.enabled) return;
    
    await tusk.debug.trace(`Function: ${functionName}`, {
      arguments: args,
      stack: new Error().stack
    });
  }
  
  async profile(operation, data = {}) {
    if (!this.enabled) return;
    
    const startTime = performance.now();
    
    return {
      start: () => {
        this.startTime = startTime;
      },
      end: async () => {
        const duration = performance.now() - startTime;
        await tusk.debug.profile(operation, {
          duration: duration,
          data: data
        });
      }
    };
  }
}
```

## Advanced Usage

### Debug Levels and Filtering

```tsk
# Debug levels configuration
debug_levels: @debug("levels", {
  "error": 0,
  "warn": 1,
  "info": 2,
  "debug": 3,
  "trace": 4
})

# Conditional debug based on environment
conditional_debug: @env("NODE_ENV") == "development" ? @debug("enabled", true) : @debug("enabled", false)

# Debug with specific filters
filtered_debug: @debug("filters", {
  "modules": ["database", "api"],
  "functions": ["processData", "validateInput"],
  "exclude": ["password", "token"]
})
```

### Debug Output Configuration

```tsk
# Multiple debug outputs
debug_outputs: @debug("outputs", {
  "console": true,
  "file": true,
  "remote": false,
  "file_path": "/var/log/app-debug.log",
  "remote_url": "https://debug.example.com/api/logs"
})

# Structured debug logging
structured_debug: @debug("structured", {
  "format": "json",
  "include_timestamp": true,
  "include_stack_trace": true,
  "include_context": true
})
```

### Performance Debugging

```tsk
# Performance debugging configuration
performance_debug: @debug("performance", {
  "enabled": true,
  "threshold": 1000,
  "track_memory": true,
  "track_cpu": true,
  "track_queries": true
})

# Memory debugging
memory_debug: @debug("memory", {
  "track_allocations": true,
  "track_garbage_collection": true,
  "leak_detection": true,
  "heap_snapshots": false
})
```

## JavaScript Implementation

### Custom Debug Manager

```javascript
class TuskDebugManager {
  constructor() {
    this.enabled = false;
    this.level = 'info';
    this.output = 'console';
    this.filters = {};
    this.loggers = new Map();
    this.traces = new Map();
    this.profiles = new Map();
  }
  
  async initialize(config) {
    this.enabled = config.enabled || false;
    this.level = config.level || 'info';
    this.output = config.output || 'console';
    this.filters = config.filters || {};
    
    // Initialize loggers
    await this.initializeLoggers();
    
    return this;
  }
  
  async initializeLoggers() {
    // Console logger
    if (this.output === 'console' || this.output === 'both') {
      this.loggers.set('console', new ConsoleLogger());
    }
    
    // File logger
    if (this.output === 'file' || this.output === 'both') {
      this.loggers.set('file', new FileLogger(this.config.file_path));
    }
    
    // Remote logger
    if (this.output === 'remote' || this.output === 'both') {
      this.loggers.set('remote', new RemoteLogger(this.config.remote_url));
    }
  }
  
  async log(message, data = {}) {
    if (!this.enabled || !this.shouldLog(data.level || 'info')) {
      return;
    }
    
    const logEntry = {
      timestamp: new Date().toISOString(),
      level: data.level || 'info',
      message: message,
      data: data,
      context: this.getContext()
    };
    
    // Apply filters
    if (this.shouldFilter(logEntry)) {
      return;
    }
    
    // Send to all loggers
    for (const logger of this.loggers.values()) {
      try {
        await logger.log(logEntry);
      } catch (error) {
        console.error('Logger error:', error);
      }
    }
  }
  
  async trace(functionName, data = {}) {
    if (!this.enabled) return;
    
    const traceId = this.generateTraceId();
    const traceData = {
      id: traceId,
      function: functionName,
      startTime: Date.now(),
      data: data,
      stack: new Error().stack
    };
    
    this.traces.set(traceId, traceData);
    
    await this.log(`TRACE START: ${functionName}`, {
      level: 'trace',
      trace_id: traceId,
      data: data
    });
    
    return traceId;
  }
  
  async endTrace(traceId, result = null) {
    if (!this.enabled || !this.traces.has(traceId)) return;
    
    const trace = this.traces.get(traceId);
    const duration = Date.now() - trace.startTime;
    
    await this.log(`TRACE END: ${trace.function}`, {
      level: 'trace',
      trace_id: traceId,
      duration: duration,
      result: result
    });
    
    this.traces.delete(traceId);
  }
  
  async profile(operation, data = {}) {
    if (!this.enabled) return null;
    
    const profileId = this.generateProfileId();
    const profileData = {
      id: profileId,
      operation: operation,
      startTime: Date.now(),
      data: data
    };
    
    this.profiles.set(profileId, profileData);
    
    await this.log(`PROFILE START: ${operation}`, {
      level: 'debug',
      profile_id: profileId,
      data: data
    });
    
    return {
      id: profileId,
      end: async (result = null) => {
        await this.endProfile(profileId, result);
      }
    };
  }
  
  async endProfile(profileId, result = null) {
    if (!this.enabled || !this.profiles.has(profileId)) return;
    
    const profile = this.profiles.get(profileId);
    const duration = Date.now() - profile.startTime;
    
    await this.log(`PROFILE END: ${profile.operation}`, {
      level: 'debug',
      profile_id: profileId,
      duration: duration,
      result: result
    });
    
    this.profiles.delete(profileId);
  }
  
  shouldLog(level) {
    const levels = { error: 0, warn: 1, info: 2, debug: 3, trace: 4 };
    const currentLevel = levels[this.level] || 2;
    const messageLevel = levels[level] || 2;
    
    return messageLevel <= currentLevel;
  }
  
  shouldFilter(logEntry) {
    // Check module filters
    if (this.filters.modules && this.filters.modules.length > 0) {
      const module = logEntry.data.module;
      if (module && !this.filters.modules.includes(module)) {
        return true;
      }
    }
    
    // Check function filters
    if (this.filters.functions && this.filters.functions.length > 0) {
      const functionName = logEntry.data.function;
      if (functionName && !this.filters.functions.includes(functionName)) {
        return true;
      }
    }
    
    // Check exclude filters
    if (this.filters.exclude && this.filters.exclude.length > 0) {
      const message = JSON.stringify(logEntry);
      if (this.filters.exclude.some(filter => message.includes(filter))) {
        return true;
      }
    }
    
    return false;
  }
  
  getContext() {
    return {
      pid: process.pid,
      memory: process.memoryUsage(),
      uptime: process.uptime()
    };
  }
  
  generateTraceId() {
    return `trace_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }
  
  generateProfileId() {
    return `profile_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }
}

class ConsoleLogger {
  async log(logEntry) {
    const colors = {
      error: '\x1b[31m',
      warn: '\x1b[33m',
      info: '\x1b[36m',
      debug: '\x1b[35m',
      trace: '\x1b[90m'
    };
    
    const color = colors[logEntry.level] || '';
    const reset = '\x1b[0m';
    
    console.log(`${color}[${logEntry.level.toUpperCase()}]${reset} ${logEntry.message}`, logEntry.data);
  }
}

class FileLogger {
  constructor(filePath) {
    this.filePath = filePath;
    this.fs = require('fs').promises;
  }
  
  async log(logEntry) {
    const logLine = JSON.stringify(logEntry) + '\n';
    await this.fs.appendFile(this.filePath, logLine);
  }
}

class RemoteLogger {
  constructor(url) {
    this.url = url;
  }
  
  async log(logEntry) {
    try {
      await fetch(this.url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(logEntry)
      });
    } catch (error) {
      console.error('Remote logging failed:', error);
    }
  }
}
```

### TypeScript Support

```typescript
interface DebugConfig {
  enabled: boolean;
  level: 'error' | 'warn' | 'info' | 'debug' | 'trace';
  output: 'console' | 'file' | 'remote' | 'both';
  filters?: {
    modules?: string[];
    functions?: string[];
    exclude?: string[];
  };
  file_path?: string;
  remote_url?: string;
}

interface LogEntry {
  timestamp: string;
  level: string;
  message: string;
  data: any;
  context: any;
}

interface TraceData {
  id: string;
  function: string;
  startTime: number;
  data: any;
  stack: string;
}

interface ProfileData {
  id: string;
  operation: string;
  startTime: number;
  data: any;
}

interface ProfileResult {
  id: string;
  end: (result?: any) => Promise<void>;
}

class TypedDebugManager {
  private enabled: boolean;
  private level: string;
  private output: string;
  private filters: any;
  private loggers: Map<string, any>;
  private traces: Map<string, TraceData>;
  private profiles: Map<string, ProfileData>;
  
  constructor() {
    this.enabled = false;
    this.level = 'info';
    this.output = 'console';
    this.filters = {};
    this.loggers = new Map();
    this.traces = new Map();
    this.profiles = new Map();
  }
  
  async initialize(config: DebugConfig): Promise<void> {
    // Implementation similar to JavaScript version
  }
  
  async log(message: string, data: any = {}): Promise<void> {
    // Implementation similar to JavaScript version
  }
  
  async trace(functionName: string, data: any = {}): Promise<string> {
    // Implementation similar to JavaScript version
    return '';
  }
  
  async endTrace(traceId: string, result?: any): Promise<void> {
    // Implementation similar to JavaScript version
  }
  
  async profile(operation: string, data: any = {}): Promise<ProfileResult | null> {
    // Implementation similar to JavaScript version
    return null;
  }
  
  async endProfile(profileId: string, result?: any): Promise<void> {
    // Implementation similar to JavaScript version
  }
}
```

## Real-World Examples

### Application Debugging

```tsk
# Application-wide debug configuration
app_debug: @debug("app", {
  "enabled": @env("DEBUG", "false"),
  "level": @env("LOG_LEVEL", "info"),
  "output": @env("LOG_OUTPUT", "console"),
  "filters": {
    "modules": ["database", "api", "auth"],
    "exclude": ["password", "token", "secret"]
  }
})

# Module-specific debugging
module_debug: {
  "database": @debug("database", {"enabled": true, "level": "debug", "track_queries": true}),
  "api": @debug("api", {"enabled": true, "level": "info", "track_requests": true}),
  "auth": @debug("auth", {"enabled": false, "level": "error"})
}
```

### Performance Debugging

```tsk
# Performance monitoring debug
performance_debug: @debug("performance", {
  "enabled": true,
  "track_memory": true,
  "track_cpu": true,
  "track_queries": true,
  "threshold": 1000,
  "output": "file",
  "file_path": "/var/log/performance.log"
})

# Memory leak debugging
memory_debug: @debug("memory", {
  "enabled": @env("NODE_ENV") == "development",
  "track_allocations": true,
  "track_garbage_collection": true,
  "leak_detection": true,
  "heap_snapshots": false
})
```

### Error Tracking

```tsk
# Error tracking debug
error_debug: @debug("errors", {
  "enabled": true,
  "level": "error",
  "include_stack_trace": true,
  "include_context": true,
  "output": "both",
  "file_path": "/var/log/errors.log",
  "remote_url": "https://error-tracking.example.com/api/errors"
})
```

## Performance Considerations

### Debug Mode Optimization

```tsk
# Conditional debug based on performance impact
lightweight_debug: @debug("lightweight", {
  "enabled": true,
  "level": "error",
  "output": "console",
  "disable_stack_traces": true,
  "disable_context": true
})
```

### Debug Caching

```javascript
// Implement debug caching for performance
class CachedDebugManager extends TuskDebugManager {
  constructor() {
    super();
    this.cache = new Map();
    this.cacheTimeout = 60000; // 1 minute
  }
  
  async log(message, data = {}) {
    const cacheKey = this.generateCacheKey(message, data);
    
    // Check cache for duplicate messages
    if (this.cache.has(cacheKey)) {
      const cached = this.cache.get(cacheKey);
      if (Date.now() - cached.timestamp < this.cacheTimeout) {
        cached.count++;
        return;
      }
    }
    
    // Cache new message
    this.cache.set(cacheKey, {
      timestamp: Date.now(),
      count: 1
    });
    
    await super.log(message, data);
  }
  
  generateCacheKey(message, data) {
    return `${message}_${JSON.stringify(data)}`;
  }
}
```

## Security Notes

- **Sensitive Data**: Never log sensitive information in debug mode
- **Access Control**: Implement proper access controls for debug logs
- **Data Sanitization**: Sanitize debug data before logging

```javascript
// Secure debug implementation
class SecureDebugManager extends TuskDebugManager {
  constructor() {
    super();
    this.sensitivePatterns = [
      /password/i,
      /token/i,
      /secret/i,
      /key/i,
      /auth/i
    ];
  }
  
  async log(message, data = {}) {
    // Sanitize sensitive data
    const sanitizedData = this.sanitizeData(data);
    const sanitizedMessage = this.sanitizeMessage(message);
    
    await super.log(sanitizedMessage, sanitizedData);
  }
  
  sanitizeData(data) {
    if (typeof data === 'object') {
      const sanitized = {};
      
      Object.keys(data).forEach(key => {
        if (this.sensitivePatterns.some(pattern => pattern.test(key))) {
          sanitized[key] = '[REDACTED]';
        } else {
          sanitized[key] = this.sanitizeData(data[key]);
        }
      });
      
      return sanitized;
    }
    
    return data;
  }
  
  sanitizeMessage(message) {
    return this.sensitivePatterns.reduce((sanitized, pattern) => {
      return sanitized.replace(pattern, '[REDACTED]');
    }, message);
  }
}
```

## Best Practices

1. **Environment-Based**: Enable debug mode only in development
2. **Level Management**: Use appropriate debug levels
3. **Performance**: Minimize performance impact of debugging
4. **Security**: Never log sensitive information
5. **Filtering**: Use filters to reduce noise
6. **Documentation**: Document debug configuration and usage

## Next Steps

- Explore [@operator chaining](./055-at-operator-chaining-javascript.md) for complex operations
- Master [@operator nesting](./056-at-operator-nesting-javascript.md) for advanced patterns
- Learn about [@operator composition](./057-at-operator-composition-javascript.md) for reusable components 