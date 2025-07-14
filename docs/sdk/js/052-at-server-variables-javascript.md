# @server Variables - JavaScript

## Overview

The `@server` function in TuskLang provides access to server-specific variables and information, including server details, request information, and server-side configuration. This is essential for JavaScript applications that need to adapt their behavior based on server environment and request context.

## Basic Syntax

```tsk
# Server information
server_name: @server.name
server_port: @server.port
server_host: @server.host
server_protocol: @server.protocol

# Request information
request_url: @server.request.url
request_method: @server.request.method
request_ip: @server.request.ip
request_user_agent: @server.request.user_agent
```

## JavaScript Integration

### Node.js Server Integration

```javascript
const tusk = require('tusklang');

// Load configuration with server variables
const config = tusk.load('server.tsk');

// Access server information
console.log(config.server_name); // Server hostname
console.log(config.server_port); // Server port
console.log(config.server_protocol); // "http" or "https"

// Use server variables in application
const serverConfig = {
  name: config.server_name,
  port: config.server_port,
  host: config.server_host,
  protocol: config.server_protocol
};

// Dynamic server variable access
const currentRequest = await tusk.server.request;
const clientIP = await tusk.server.request.ip;
const userAgent = await tusk.server.request.user_agent;
```

### Express.js Integration

```javascript
const express = require('express');
const tusk = require('tusklang');

const app = express();

// Middleware to inject server variables
app.use(async (req, res, next) => {
  // Set server variables for this request
  await tusk.server.setRequest({
    url: req.url,
    method: req.method,
    ip: req.ip,
    user_agent: req.get('User-Agent'),
    headers: req.headers
  });
  
  next();
});

// Route using server variables
app.get('/api/info', async (req, res) => {
  const serverInfo = await tusk.server.getInfo();
  const requestInfo = await tusk.server.getRequestInfo();
  
  res.json({
    server: serverInfo,
    request: requestInfo
  });
});
```

## Advanced Usage

### Server Configuration

```tsk
# Server configuration based on environment
server_config: {
  "name": @server.name,
  "port": @server.port,
  "host": @server.host,
  "protocol": @server.protocol,
  "environment": @env("NODE_ENV", "development"),
  "version": @env("APP_VERSION", "1.0.0")
}

# Dynamic server settings
dynamic_server_settings: @server.is_https() ? {
  "ssl_enabled": true,
  "secure_cookies": true,
  "hsts_enabled": true
} : {
  "ssl_enabled": false,
  "secure_cookies": false,
  "hsts_enabled": false
}
```

### Request-Specific Variables

```tsk
# Request-based configuration
request_config: {
  "url": @server.request.url,
  "method": @server.request.method,
  "ip": @server.request.ip,
  "user_agent": @server.request.user_agent,
  "headers": @server.request.headers,
  "query": @server.request.query,
  "body": @server.request.body
}

# Conditional configuration based on request
conditional_config: @server.request.method == "POST" ? {
  "rate_limit": "strict",
  "validation": "enhanced"
} : {
  "rate_limit": "normal",
  "validation": "standard"
}
```

### Server Health and Monitoring

```tsk
# Server health information
server_health: {
  "uptime": @server.uptime,
  "memory_usage": @server.memory_usage,
  "cpu_usage": @server.cpu_usage,
  "active_connections": @server.active_connections,
  "load_average": @server.load_average
}

# Performance metrics
performance_metrics: {
  "response_time": @server.metrics.response_time,
  "requests_per_second": @server.metrics.requests_per_second,
  "error_rate": @server.metrics.error_rate,
  "throughput": @server.metrics.throughput
}
```

## JavaScript Implementation

### Custom Server Manager

```javascript
class TuskServerManager {
  constructor() {
    this.serverInfo = null;
    this.requestInfo = null;
    this.metrics = new Map();
    this.healthData = {};
  }
  
  async initialize() {
    // Initialize server information
    this.serverInfo = await this.getServerInfo();
    
    // Start monitoring
    this.startMonitoring();
    
    return this;
  }
  
  async getServerInfo() {
    const os = require('os');
    
    return {
      name: os.hostname(),
      port: process.env.PORT || 3000,
      host: process.env.HOST || 'localhost',
      protocol: process.env.NODE_ENV === 'production' ? 'https' : 'http',
      platform: os.platform(),
      arch: os.arch(),
      cpus: os.cpus().length,
      memory: os.totalmem(),
      uptime: process.uptime()
    };
  }
  
  async setRequest(requestData) {
    this.requestInfo = {
      url: requestData.url,
      method: requestData.method,
      ip: requestData.ip,
      user_agent: requestData.user_agent,
      headers: requestData.headers,
      query: requestData.query || {},
      body: requestData.body || {},
      timestamp: Date.now()
    };
  }
  
  async getRequestInfo() {
    return this.requestInfo;
  }
  
  async getServerVariable(path) {
    const parts = path.split('.');
    let current = this.serverInfo;
    
    for (const part of parts) {
      if (current && typeof current === 'object' && part in current) {
        current = current[part];
      } else {
        return null;
      }
    }
    
    return current;
  }
  
  async getHealthData() {
    const os = require('os');
    
    return {
      uptime: process.uptime(),
      memory_usage: process.memoryUsage(),
      cpu_usage: process.cpuUsage(),
      load_average: os.loadavg(),
      active_connections: this.getActiveConnections()
    };
  }
  
  async getMetrics() {
    return {
      response_time: this.metrics.get('response_time') || 0,
      requests_per_second: this.metrics.get('requests_per_second') || 0,
      error_rate: this.metrics.get('error_rate') || 0,
      throughput: this.metrics.get('throughput') || 0
    };
  }
  
  startMonitoring() {
    // Monitor server health every 30 seconds
    setInterval(async () => {
      this.healthData = await this.getHealthData();
    }, 30000);
    
    // Monitor metrics every 5 seconds
    setInterval(async () => {
      await this.updateMetrics();
    }, 5000);
  }
  
  async updateMetrics() {
    // Update performance metrics
    const currentTime = Date.now();
    
    // Calculate response time (simplified)
    const avgResponseTime = this.calculateAverageResponseTime();
    this.metrics.set('response_time', avgResponseTime);
    
    // Calculate requests per second
    const requestsPerSecond = this.calculateRequestsPerSecond();
    this.metrics.set('requests_per_second', requestsPerSecond);
    
    // Calculate error rate
    const errorRate = this.calculateErrorRate();
    this.metrics.set('error_rate', errorRate);
  }
  
  calculateAverageResponseTime() {
    // Simplified calculation
    return Math.random() * 100 + 50; // 50-150ms
  }
  
  calculateRequestsPerSecond() {
    // Simplified calculation
    return Math.random() * 10 + 5; // 5-15 RPS
  }
  
  calculateErrorRate() {
    // Simplified calculation
    return Math.random() * 0.05; // 0-5% error rate
  }
  
  getActiveConnections() {
    // Simplified active connections count
    return Math.floor(Math.random() * 100) + 10; // 10-110 connections
  }
  
  isHttps() {
    return this.serverInfo && this.serverInfo.protocol === 'https';
  }
}
```

### TypeScript Support

```typescript
interface ServerInfo {
  name: string;
  port: number;
  host: string;
  protocol: 'http' | 'https';
  platform: string;
  arch: string;
  cpus: number;
  memory: number;
  uptime: number;
}

interface RequestInfo {
  url: string;
  method: string;
  ip: string;
  user_agent: string;
  headers: Record<string, string>;
  query: Record<string, any>;
  body: any;
  timestamp: number;
}

interface HealthData {
  uptime: number;
  memory_usage: NodeJS.MemoryUsage;
  cpu_usage: NodeJS.CpuUsage;
  load_average: number[];
  active_connections: number;
}

interface PerformanceMetrics {
  response_time: number;
  requests_per_second: number;
  error_rate: number;
  throughput: number;
}

class TypedServerManager {
  private serverInfo: ServerInfo | null;
  private requestInfo: RequestInfo | null;
  private metrics: Map<string, number>;
  private healthData: HealthData | null;
  
  constructor() {
    this.serverInfo = null;
    this.requestInfo = null;
    this.metrics = new Map();
    this.healthData = null;
  }
  
  async initialize(): Promise<void> {
    this.serverInfo = await this.getServerInfo();
    this.startMonitoring();
  }
  
  async getServerInfo(): Promise<ServerInfo> {
    // Implementation similar to JavaScript version
    return {} as ServerInfo;
  }
  
  async setRequest(requestData: Partial<RequestInfo>): Promise<void> {
    this.requestInfo = {
      url: requestData.url || '',
      method: requestData.method || 'GET',
      ip: requestData.ip || '',
      user_agent: requestData.user_agent || '',
      headers: requestData.headers || {},
      query: requestData.query || {},
      body: requestData.body || {},
      timestamp: Date.now()
    };
  }
  
  async getRequestInfo(): Promise<RequestInfo | null> {
    return this.requestInfo;
  }
  
  async getHealthData(): Promise<HealthData> {
    // Implementation similar to JavaScript version
    return {} as HealthData;
  }
  
  async getMetrics(): Promise<PerformanceMetrics> {
    return {
      response_time: this.metrics.get('response_time') || 0,
      requests_per_second: this.metrics.get('requests_per_second') || 0,
      error_rate: this.metrics.get('error_rate') || 0,
      throughput: this.metrics.get('throughput') || 0
    };
  }
  
  isHttps(): boolean {
    return this.serverInfo?.protocol === 'https';
  }
  
  private startMonitoring(): void {
    // Implementation similar to JavaScript version
  }
}
```

## Real-World Examples

### Server Configuration Management

```tsk
# Server configuration based on environment
server_environment_config: {
  "development": @env("NODE_ENV") == "development" ? {
    "debug": true,
    "log_level": "debug",
    "cors_origin": "*",
    "rate_limit": "disabled"
  } : {},
  "production": @env("NODE_ENV") == "production" ? {
    "debug": false,
    "log_level": "error",
    "cors_origin": @env("ALLOWED_ORIGINS", "https://example.com"),
    "rate_limit": "strict"
  } : {}
}

# Server-specific settings
server_specific_settings: {
  "max_connections": @server.cpus * 100,
  "memory_limit": @server.memory * 0.8,
  "timeout": @server.is_https() ? 30000 : 15000
}
```

### Request-Based Configuration

```tsk
# Request-specific security settings
security_config: {
  "rate_limit": @server.request.ip in @server.blacklisted_ips ? "blocked" : "normal",
  "cors_origin": @server.request.headers.origin in @server.allowed_origins ? @server.request.headers.origin : "denied",
  "authentication": @server.request.headers.authorization ? "required" : "optional"
}

# API versioning based on request
api_version_config: @server.request.url.startsWith("/api/v2") ? {
  "version": "v2",
  "features": ["enhanced_validation", "rate_limiting", "caching"]
} : {
  "version": "v1",
  "features": ["basic_validation"]
}
```

### Monitoring and Health Checks

```tsk
# Health check configuration
health_check_config: {
  "enabled": true,
  "interval": 30000,
  "timeout": 5000,
  "thresholds": {
    "cpu_usage": 80,
    "memory_usage": 85,
    "response_time": 1000,
    "error_rate": 5
  }
}

# Performance monitoring
performance_monitoring: {
  "metrics_enabled": true,
  "collection_interval": 5000,
  "retention_period": 86400000,
  "alerts": {
    "high_cpu": @server.cpu_usage > 80,
    "high_memory": @server.memory_usage > 85,
    "high_error_rate": @server.metrics.error_rate > 5
  }
}
```

## Performance Considerations

### Server Variable Caching

```tsk
# Cache server variables for performance
cached_server_info: @cache("1m", {
  "name": @server.name,
  "port": @server.port,
  "host": @server.host,
  "protocol": @server.protocol
})
```

### Efficient Monitoring

```javascript
// Implement efficient server monitoring
class EfficientServerManager extends TuskServerManager {
  constructor() {
    super();
    this.monitoringInterval = 30000; // 30 seconds
    this.metricsInterval = 5000; // 5 seconds
  }
  
  startMonitoring() {
    // Use more efficient intervals
    setInterval(async () => {
      this.healthData = await this.getHealthData();
    }, this.monitoringInterval);
    
    setInterval(async () => {
      await this.updateMetrics();
    }, this.metricsInterval);
  }
  
  async getServerVariable(path) {
    // Cache frequently accessed variables
    const cacheKey = `server_var_${path}`;
    
    if (!this.variableCache) {
      this.variableCache = new Map();
    }
    
    if (this.variableCache.has(cacheKey)) {
      const cached = this.variableCache.get(cacheKey);
      if (Date.now() - cached.timestamp < 60000) { // 1 minute cache
        return cached.value;
      }
    }
    
    const value = await super.getServerVariable(path);
    
    this.variableCache.set(cacheKey, {
      value: value,
      timestamp: Date.now()
    });
    
    return value;
  }
}
```

## Security Notes

- **Request Validation**: Always validate server request data
- **Access Control**: Implement proper access controls for server variables
- **Data Sanitization**: Sanitize server information before logging

```javascript
// Secure server variable handling
class SecureServerManager extends TuskServerManager {
  constructor() {
    super();
    this.sensitiveHeaders = new Set(['authorization', 'cookie', 'x-api-key']);
  }
  
  async setRequest(requestData) {
    // Sanitize sensitive headers
    const sanitizedHeaders = {};
    
    Object.keys(requestData.headers || {}).forEach(key => {
      if (!this.sensitiveHeaders.has(key.toLowerCase())) {
        sanitizedHeaders[key] = requestData.headers[key];
      } else {
        sanitizedHeaders[key] = '[REDACTED]';
      }
    });
    
    requestData.headers = sanitizedHeaders;
    
    await super.setRequest(requestData);
  }
  
  async getServerVariable(path) {
    // Validate path to prevent injection
    if (!this.isValidPath(path)) {
      throw new Error(`Invalid server variable path: ${path}`);
    }
    
    return await super.getServerVariable(path);
  }
  
  isValidPath(path) {
    // Only allow alphanumeric characters, dots, and underscores
    return /^[a-zA-Z0-9._]+$/.test(path);
  }
}
```

## Best Practices

1. **Validation**: Always validate server variable paths
2. **Caching**: Cache frequently accessed server variables
3. **Monitoring**: Implement efficient server monitoring
4. **Security**: Sanitize sensitive server information
5. **Performance**: Use appropriate monitoring intervals
6. **Documentation**: Document all server variables and their usage

## Next Steps

- Master [@global variables](./053-at-global-variables-javascript.md) for global state management
- Learn about [@debug mode](./054-at-debug-mode-javascript.md) for debugging capabilities
- Explore [@operator chaining](./055-at-operator-chaining-javascript.md) for complex operations 