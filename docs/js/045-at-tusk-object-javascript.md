# @tusk Object - JavaScript

## Overview

The `@tusk` object in TuskLang provides access to the core TuskLang runtime and its advanced features. This object serves as the main interface for JavaScript applications to interact with TuskLang's powerful capabilities, including configuration management, database operations, caching, and more.

## Basic Syntax

```tsk
# Access TuskLang runtime properties
tusk_version: @tusk.version
tusk_config: @tusk.config
tusk_environment: @tusk.env

# Access runtime methods
current_time: @tusk.now()
memory_usage: @tusk.memory()
process_id: @tusk.pid()
```

## JavaScript Integration

### Node.js TuskLang Integration

```javascript
const tusk = require('tusklang');

// Load configuration with TuskLang object access
const config = tusk.load('runtime.tsk');

// Access TuskLang runtime information
console.log(config.tusk_version); // "2.0.0"
console.log(config.tusk_environment); // "production"
console.log(config.current_time); // "2024-01-15T10:30:00Z"

// Use TuskLang runtime methods
const runtimeInfo = {
  version: config.tusk_version,
  environment: config.tusk_environment,
  memoryUsage: config.memory_usage,
  processId: config.process_id
};

// Dynamic runtime access
const currentConfig = await tusk.runtime.getConfig();
const systemInfo = await tusk.runtime.getSystemInfo();
```

### Browser TuskLang Integration

```javascript
// Load TuskLang configuration
const config = await tusk.load('runtime.tsk');

// Use TuskLang runtime in frontend
class TuskRuntimeManager {
  constructor() {
    this.version = config.tusk_version;
    this.environment = config.tusk_environment;
  }
  
  getRuntimeInfo() {
    return {
      version: this.version,
      environment: this.environment,
      timestamp: config.current_time,
      memory: config.memory_usage
    };
  }
  
  async getSystemStatus() {
    const status = await tusk.runtime.getStatus();
    return {
      ...status,
      config: this.getRuntimeInfo()
    };
  }
}
```

## Advanced Usage

### Configuration Management

```tsk
# Access configuration properties
app_config: @tusk.config.app
database_config: @tusk.config.database
api_config: @tusk.config.api

# Dynamic configuration access
current_config: @tusk.config.get("app.name")
nested_config: @tusk.config.get("database.connection.host")
```

### Environment Management

```tsk
# Environment-specific configurations
is_production: @tusk.env.is("production")
is_development: @tusk.env.is("development")
is_testing: @tusk.env.is("testing")

# Environment variables
node_env: @tusk.env.get("NODE_ENV")
app_port: @tusk.env.get("APP_PORT", "3000")
database_url: @tusk.env.get("DATABASE_URL")
```

### Runtime Monitoring

```tsk
# System monitoring
cpu_usage: @tusk.monitor.cpu()
memory_usage: @tusk.monitor.memory()
disk_usage: @tusk.monitor.disk()

# Performance metrics
response_time: @tusk.metrics.get("api.response_time")
error_rate: @tusk.metrics.get("api.error_rate")
throughput: @tusk.metrics.get("api.throughput")
```

## JavaScript Implementation

### TuskLang Runtime Manager

```javascript
class TuskRuntimeManager {
  constructor() {
    this.runtime = null;
    this.config = null;
    this.metrics = new Map();
  }
  
  async initialize() {
    // Initialize TuskLang runtime
    this.runtime = await tusk.runtime.initialize();
    this.config = await tusk.load('runtime.tsk');
    
    // Set up monitoring
    this.setupMonitoring();
    
    return this;
  }
  
  setupMonitoring() {
    // Monitor system resources
    setInterval(() => {
      this.updateMetrics();
    }, 5000); // Every 5 seconds
  }
  
  async updateMetrics() {
    const metrics = {
      cpu: await this.runtime.monitor.cpu(),
      memory: await this.runtime.monitor.memory(),
      disk: await this.runtime.monitor.disk(),
      timestamp: new Date().toISOString()
    };
    
    this.metrics.set('system', metrics);
    
    // Emit metrics event
    this.emit('metrics', metrics);
  }
  
  getConfig(path = null) {
    if (path) {
      return this.getNestedValue(this.config, path);
    }
    return this.config;
  }
  
  getNestedValue(obj, path) {
    return path.split('.').reduce((current, key) => {
      return current && current[key] !== undefined ? current[key] : null;
    }, obj);
  }
  
  async getSystemInfo() {
    return {
      version: this.config.tusk_version,
      environment: this.config.tusk_environment,
      uptime: process.uptime(),
      memory: process.memoryUsage(),
      cpu: process.cpuUsage(),
      pid: process.pid
    };
  }
  
  async getMetrics() {
    return {
      system: this.metrics.get('system'),
      custom: this.metrics.get('custom') || {}
    };
  }
}
```

### TypeScript Support

```typescript
interface TuskRuntimeConfig {
  tusk_version: string;
  tusk_environment: string;
  current_time: string;
  memory_usage: number;
  process_id: number;
}

interface SystemMetrics {
  cpu: number;
  memory: number;
  disk: number;
  timestamp: string;
}

interface RuntimeInfo {
  version: string;
  environment: string;
  uptime: number;
  memory: NodeJS.MemoryUsage;
  cpu: NodeJS.CpuUsage;
  pid: number;
}

class TypedTuskRuntimeManager {
  private runtime: any;
  private config: TuskRuntimeConfig;
  private metrics: Map<string, SystemMetrics>;
  
  constructor() {
    this.metrics = new Map();
  }
  
  async initialize(): Promise<void> {
    this.runtime = await tusk.runtime.initialize();
    this.config = await tusk.load('runtime.tsk');
  }
  
  getConfig(): TuskRuntimeConfig {
    return this.config;
  }
  
  async getSystemInfo(): Promise<RuntimeInfo> {
    return {
      version: this.config.tusk_version,
      environment: this.config.tusk_environment,
      uptime: process.uptime(),
      memory: process.memoryUsage(),
      cpu: process.cpuUsage(),
      pid: process.pid
    };
  }
  
  getMetrics(): Map<string, SystemMetrics> {
    return this.metrics;
  }
}
```

## Real-World Examples

### Application Runtime Management

```tsk
# Application runtime configuration
app_runtime: {
  name: @tusk.config.get("app.name"),
  version: @tusk.config.get("app.version"),
  environment: @tusk.env.get("NODE_ENV"),
  port: @tusk.env.get("APP_PORT", "3000"),
  debug: @tusk.env.get("DEBUG", "false")
}

# Database runtime configuration
db_runtime: {
  host: @tusk.config.get("database.host"),
  port: @tusk.config.get("database.port"),
  name: @tusk.config.get("database.name"),
  pool_size: @tusk.config.get("database.pool_size", "10")
}

# API runtime configuration
api_runtime: {
  base_url: @tusk.config.get("api.base_url"),
  timeout: @tusk.config.get("api.timeout", "30000"),
  retries: @tusk.config.get("api.retries", "3")
}
```

### Monitoring Dashboard

```tsk
# System health monitoring
system_health: {
  status: @tusk.monitor.status(),
  uptime: @tusk.monitor.uptime(),
  cpu_usage: @tusk.monitor.cpu(),
  memory_usage: @tusk.monitor.memory(),
  disk_usage: @tusk.monitor.disk(),
  network_status: @tusk.monitor.network()
}

# Application metrics
app_metrics: {
  response_time: @tusk.metrics.get("app.response_time"),
  error_rate: @tusk.metrics.get("app.error_rate"),
  throughput: @tusk.metrics.get("app.throughput"),
  active_connections: @tusk.metrics.get("app.active_connections")
}
```

### Environment Management

```tsk
# Environment-specific settings
production_settings: @tusk.env.is("production") ? {
  log_level: "error",
  cache_enabled: true,
  monitoring_enabled: true,
  security_level: "high"
} : {}

development_settings: @tusk.env.is("development") ? {
  log_level: "debug",
  cache_enabled: false,
  monitoring_enabled: false,
  security_level: "low"
} : {}
```

## Performance Considerations

### Runtime Optimization

```tsk
# Cache runtime information
cached_system_info: @cache("1m", @tusk.monitor.system())
cached_metrics: @cache("30s", @tusk.metrics.all())
```

### Lazy Loading

```javascript
// Load runtime information on demand
class LazyRuntimeManager {
  constructor() {
    this.cache = new Map();
    this.cacheTimeout = 60000; // 1 minute
  }
  
  async getRuntimeInfo() {
    const cacheKey = 'runtime_info';
    const cached = this.cache.get(cacheKey);
    
    if (cached && Date.now() - cached.timestamp < this.cacheTimeout) {
      return cached.data;
    }
    
    const runtimeInfo = await tusk.runtime.getSystemInfo();
    
    this.cache.set(cacheKey, {
      data: runtimeInfo,
      timestamp: Date.now()
    });
    
    return runtimeInfo;
  }
}
```

## Security Notes

- **Access Control**: Limit runtime access to authorized users
- **Configuration Security**: Secure sensitive configuration values
- **Monitoring Security**: Protect monitoring endpoints

```javascript
// Secure runtime access
class SecureRuntimeManager {
  constructor() {
    this.authorizedUsers = new Set();
  }
  
  async getRuntimeInfo(userId) {
    if (!this.authorizedUsers.has(userId)) {
      throw new Error('Unauthorized access to runtime information');
    }
    
    const runtimeInfo = await tusk.runtime.getSystemInfo();
    
    // Filter sensitive information
    return this.filterSensitiveData(runtimeInfo);
  }
  
  filterSensitiveData(data) {
    const filtered = { ...data };
    
    // Remove sensitive fields
    delete filtered.secrets;
    delete filtered.passwords;
    delete filtered.private_keys;
    
    return filtered;
  }
}
```

## Best Practices

1. **Runtime Monitoring**: Implement comprehensive runtime monitoring
2. **Configuration Management**: Use environment-specific configurations
3. **Performance Optimization**: Cache expensive runtime operations
4. **Security**: Implement proper access controls
5. **Error Handling**: Handle runtime errors gracefully
6. **Documentation**: Document runtime configuration and usage

## Next Steps

- Explore [@cache function](./046-at-cache-function-javascript.md) for caching strategies
- Master [@metrics function](./047-at-metrics-function-javascript.md) for performance monitoring
- Learn about [@learn function](./048-at-learn-function-javascript.md) for machine learning integration 