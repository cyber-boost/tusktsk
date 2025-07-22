# TuskLang JavaScript SDK - Best Practices

**Production-ready patterns and practices for TuskLang JavaScript SDK**

## Table of Contents

- [Configuration Organization](#configuration-organization)
- [Security Best Practices](#security-best-practices)
- [Performance Optimization](#performance-optimization)
- [Error Handling](#error-handling)
- [Testing Strategies](#testing-strategies)
- [Deployment Patterns](#deployment-patterns)
- [Monitoring and Observability](#monitoring-and-observability)
- [Code Organization](#code-organization)

## Configuration Organization

### 1. Hierarchical Configuration Structure

Organize your configuration files in a logical hierarchy:

```
config/
├── base.tsk              # Base configuration
├── development.tsk       # Development overrides
├── staging.tsk          # Staging overrides
├── production.tsk       # Production overrides
├── database.tsk         # Database-specific config
├── security.tsk         # Security settings
└── monitoring.tsk       # Monitoring settings
```

### 2. Environment-Specific Configuration

```tsk
# base.tsk - Base configuration
$app_name: "MyApp"
$version: "1.0.0"

[server]
host: "0.0.0.0"
port: 8080

[database]
type: "postgres"
host: "localhost"
port: 5432

# development.tsk - Development overrides
$env: "development"

[server]
port: 3000

[database]
database: $app_name + "_dev"

[logging]
level: "debug"
console: true

# production.tsk - Production overrides
$env: "production"

[server]
port: 80
workers: 8

[database]
database: $app_name + "_prod"
pool: {
  max: 20
}

[logging]
level: "warn"
file: "/var/log/app.log"
```

### 3. Configuration Validation

Always validate your configuration:

```javascript
const TuskLang = require('tusklang');

// Define validation schema
const schema = {
  required: ['name', 'version', 'server', 'database'],
  properties: {
    name: { type: 'string', minLength: 1 },
    version: { type: 'string', pattern: '^\\d+\\.\\d+\\.\\d+$' },
    server: {
      type: 'object',
      required: ['host', 'port'],
      properties: {
        host: { type: 'string' },
        port: { type: 'number', minimum: 1, maximum: 65535 }
      }
    },
    database: {
      type: 'object',
      required: ['type', 'host', 'port', 'database'],
      properties: {
        type: { type: 'string', enum: ['postgres', 'mysql', 'sqlite'] },
        host: { type: 'string' },
        port: { type: 'number' },
        database: { type: 'string' }
      }
    }
  }
};

// Validate configuration
const tusk = new TuskLang();
const config = tusk.parseFile('./config/app.tsk');
const errors = tusk.validate(config, schema);

if (errors.length > 0) {
  console.error('Configuration validation failed:');
  errors.forEach(error => {
    console.error(`- ${error.path}: ${error.message}`);
  });
  process.exit(1);
}
```

### 4. Sensitive Data Management

Never store sensitive data in configuration files:

```tsk
# ❌ Bad - Sensitive data in config
[database]
password: "secret123"

# ✅ Good - Use environment variables
[database]
password: @env("DB_PASSWORD")
```

Use a secure secrets management system:

```javascript
// Load secrets from external service
const AWS = require('aws-sdk');
const secretsManager = new AWS.SecretsManager();

async function loadSecrets() {
  const secret = await secretsManager.getSecretValue({
    SecretId: 'myapp/database'
  }).promise();
  
  return JSON.parse(secret.SecretString);
}

// Use in configuration
const secrets = await loadSecrets();
const config = tusk.parse(`
  [database]
  password: "${secrets.password}"
`);
```

## Security Best Practices

### 1. Environment Variable Security

```bash
# .env.example - Template file
NODE_ENV=development
DB_HOST=localhost
DB_PORT=5432
DB_NAME=myapp
DB_USER=postgres
DB_PASSWORD=your_password_here
JWT_SECRET=your_jwt_secret_here

# .env - Actual values (never commit)
NODE_ENV=production
DB_HOST=prod-db.example.com
DB_PORT=5432
DB_NAME=myapp_prod
DB_USER=app_user
DB_PASSWORD=super_secret_password_123
JWT_SECRET=very_long_random_secret_key_here
```

### 2. Database Query Security

Always use parameterized queries:

```tsk
# ❌ Bad - SQL injection risk
user: @query("SELECT * FROM users WHERE id = " + $user_id)

# ✅ Good - Parameterized query
user: @query("SELECT * FROM users WHERE id = ?", $user_id)
```

### 3. Input Validation

Validate all configuration inputs:

```javascript
const validator = {
  validatePort(port) {
    const num = parseInt(port);
    if (isNaN(num) || num < 1 || num > 65535) {
      throw new Error(`Invalid port: ${port}`);
    }
    return num;
  },
  
  validateHost(host) {
    if (!host || typeof host !== 'string') {
      throw new Error(`Invalid host: ${host}`);
    }
    return host;
  },
  
  validateDatabaseConfig(config) {
    return {
      host: this.validateHost(config.host),
      port: this.validatePort(config.port),
      database: config.database,
      username: config.username,
      password: config.password
    };
  }
};
```

### 4. Access Control

Implement proper access control for configuration:

```javascript
class SecureConfigManager {
  constructor() {
    this.config = null;
    this.permissions = new Map();
  }
  
  setPermission(userId, path, permission) {
    const key = `${userId}:${path}`;
    this.permissions.set(key, permission);
  }
  
  getConfig(userId, path) {
    const key = `${userId}:${path}`;
    const permission = this.permissions.get(key);
    
    if (!permission || !permission.includes('read')) {
      throw new Error('Access denied');
    }
    
    return this.getNestedValue(this.config, path);
  }
  
  setConfig(userId, path, value) {
    const key = `${userId}:${path}`;
    const permission = this.permissions.get(key);
    
    if (!permission || !permission.includes('write')) {
      throw new Error('Access denied');
    }
    
    this.setNestedValue(this.config, path, value);
  }
}
```

## Performance Optimization

### 1. Binary Compilation

Use binary compilation for production:

```javascript
const { PeanutConfig } = require('tusklang');

// Compile configuration for production
await PeanutConfig.compile('config/app.tsk', 'config/app.pnt', {
  optimize: true,
  compress: true,
  includeSourceMap: false
});

// Load binary configuration
const config = PeanutConfig.load({
  files: ['config/app.pnt']
});
```

### 2. Caching Strategy

Implement intelligent caching:

```javascript
class ConfigCache {
  constructor() {
    this.cache = new Map();
    this.ttl = 300000; // 5 minutes
  }
  
  get(key) {
    const item = this.cache.get(key);
    if (!item) return null;
    
    if (Date.now() - item.timestamp > this.ttl) {
      this.cache.delete(key);
      return null;
    }
    
    return item.value;
  }
  
  set(key, value) {
    this.cache.set(key, {
      value,
      timestamp: Date.now()
    });
  }
  
  clear() {
    this.cache.clear();
  }
}

// Use with TuskLang
const cache = new ConfigCache();
const tusk = new TuskLang();

function getConfig(filePath) {
  const cached = cache.get(filePath);
  if (cached) return cached;
  
  const config = tusk.parseFile(filePath);
  cache.set(filePath, config);
  return config;
}
```

### 3. Lazy Loading

Load configuration only when needed:

```javascript
class LazyConfigLoader {
  constructor() {
    this.configs = new Map();
    this.loaders = new Map();
  }
  
  register(name, loader) {
    this.loaders.set(name, loader);
  }
  
  async get(name) {
    if (this.configs.has(name)) {
      return this.configs.get(name);
    }
    
    const loader = this.loaders.get(name);
    if (!loader) {
      throw new Error(`No loader registered for: ${name}`);
    }
    
    const config = await loader();
    this.configs.set(name, config);
    return config;
  }
}

// Usage
const loader = new LazyConfigLoader();

loader.register('app', () => tusk.parseFile('./config/app.tsk'));
loader.register('database', () => tusk.parseFile('./config/database.tsk'));

// Config is loaded only when accessed
const appConfig = await loader.get('app');
const dbConfig = await loader.get('database');
```

### 4. Database Connection Pooling

Optimize database connections:

```javascript
const { Pool } = require('pg');

class DatabaseManager {
  constructor(config) {
    this.pools = new Map();
    this.config = config;
  }
  
  getPool(name = 'default') {
    if (this.pools.has(name)) {
      return this.pools.get(name);
    }
    
    const poolConfig = this.config.database[name] || this.config.database;
    const pool = new Pool({
      host: poolConfig.host,
      port: poolConfig.port,
      database: poolConfig.database,
      user: poolConfig.username,
      password: poolConfig.password,
      max: poolConfig.pool?.max || 10,
      min: poolConfig.pool?.min || 2,
      idleTimeoutMillis: poolConfig.pool?.idle_timeout || 30000,
      connectionTimeoutMillis: poolConfig.pool?.acquire_timeout || 60000
    });
    
    this.pools.set(name, pool);
    return pool;
  }
  
  async closeAll() {
    for (const pool of this.pools.values()) {
      await pool.end();
    }
    this.pools.clear();
  }
}
```

## Error Handling

### 1. Configuration Error Handling

```javascript
class ConfigurationError extends Error {
  constructor(message, file, line, column) {
    super(message);
    this.name = 'ConfigurationError';
    this.file = file;
    this.line = line;
    this.column = column;
  }
}

function parseConfigSafely(filePath) {
  try {
    const tusk = new TuskLang();
    return tusk.parseFile(filePath);
  } catch (error) {
    if (error.name === 'ParseError') {
      throw new ConfigurationError(
        error.message,
        filePath,
        error.line,
        error.column
      );
    }
    throw error;
  }
}
```

### 2. Database Error Handling

```javascript
class DatabaseError extends Error {
  constructor(message, sql, params) {
    super(message);
    this.name = 'DatabaseError';
    this.sql = sql;
    this.params = params;
  }
}

async function executeQuerySafely(pool, sql, params = []) {
  try {
    const client = await pool.connect();
    try {
      const result = await client.query(sql, params);
      return result.rows;
    } finally {
      client.release();
    }
  } catch (error) {
    throw new DatabaseError(error.message, sql, params);
  }
}
```

### 3. Graceful Degradation

```javascript
class ResilientConfigLoader {
  constructor() {
    this.fallbacks = new Map();
  }
  
  addFallback(name, fallbackConfig) {
    this.fallbacks.set(name, fallbackConfig);
  }
  
  async loadConfig(filePath) {
    try {
      return await parseConfigSafely(filePath);
    } catch (error) {
      console.warn(`Failed to load config from ${filePath}:`, error.message);
      
      const fallback = this.fallbacks.get(filePath);
      if (fallback) {
        console.log(`Using fallback configuration for ${filePath}`);
        return fallback;
      }
      
      throw error;
    }
  }
}
```

## Testing Strategies

### 1. Unit Testing Configuration

```javascript
const { describe, it, expect, beforeEach } = require('jest');
const TuskLang = require('tusklang');

describe('Configuration Parsing', () => {
  let tusk;
  
  beforeEach(() => {
    tusk = new TuskLang();
  });
  
  it('should parse basic configuration', () => {
    const config = tusk.parse(`
      name: "TestApp"
      version: "1.0.0"
      
      [server]
      port: 8080
    `);
    
    expect(config.name).toBe('TestApp');
    expect(config.version).toBe('1.0.0');
    expect(config.server.port).toBe(8080);
  });
  
  it('should handle environment variables', () => {
    process.env.TEST_PORT = '3000';
    
    const config = tusk.parse(`
      port: @env("TEST_PORT", "8080")
    `);
    
    expect(config.port).toBe('3000');
  });
  
  it('should validate configuration', () => {
    const schema = {
      required: ['name', 'port'],
      properties: {
        name: { type: 'string' },
        port: { type: 'number' }
      }
    };
    
    const config = { name: 'Test', port: 8080 };
    const errors = tusk.validate(config, schema);
    
    expect(errors).toHaveLength(0);
  });
});
```

### 2. Integration Testing

```javascript
describe('Database Integration', () => {
  let tusk;
  let mockPool;
  
  beforeEach(() => {
    tusk = new TuskLangEnhanced();
    
    // Mock database pool
    mockPool = {
      connect: jest.fn().mockResolvedValue({
        query: jest.fn().mockResolvedValue({
          rows: [{ count: 42 }]
        }),
        release: jest.fn()
      })
    };
    
    const adapter = {
      async query(sql, params) {
        const client = await mockPool.connect();
        try {
          const result = await client.query(sql, params);
          return result.rows;
        } finally {
          client.release();
        }
      }
    };
    
    tusk.setDatabaseAdapter(adapter);
  });
  
  it('should execute database queries', async () => {
    const config = await tusk.parse(`
      [stats]
      user_count: @query("SELECT COUNT(*) as count FROM users")
    `);
    
    expect(config.stats.user_count).toBe(42);
    expect(mockPool.connect).toHaveBeenCalled();
  });
});
```

### 3. Performance Testing

```javascript
describe('Performance Tests', () => {
  it('should parse large configuration quickly', () => {
    const largeConfig = generateLargeConfig(1000);
    
    const start = process.hrtime.bigint();
    const tusk = new TuskLang();
    const config = tusk.parse(largeConfig);
    const end = process.hrtime.bigint();
    
    const duration = Number(end - start) / 1000000; // milliseconds
    expect(duration).toBeLessThan(100); // Should parse in under 100ms
  });
  
  it('should handle binary compilation efficiently', async () => {
    const { PeanutConfig } = require('tusklang');
    
    const start = process.hrtime.bigint();
    await PeanutConfig.compile('test-config.tsk', 'test-config.pnt');
    const end = process.hrtime.bigint();
    
    const duration = Number(end - start) / 1000000;
    expect(duration).toBeLessThan(500); // Should compile in under 500ms
  });
});
```

## Deployment Patterns

### 1. Docker Configuration

```dockerfile
# Dockerfile
FROM node:16-alpine

WORKDIR /app

# Copy package files
COPY package*.json ./
RUN npm ci --only=production

# Copy application code
COPY src/ ./src/
COPY config/ ./config/

# Copy binary configurations
COPY *.pnt ./

# Create non-root user
RUN addgroup -g 1001 -S nodejs
RUN adduser -S nodejs -u 1001

# Change ownership
RUN chown -R nodejs:nodejs /app
USER nodejs

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD node -e "require('http').get('http://localhost:8080/health', (res) => { process.exit(res.statusCode === 200 ? 0 : 1) })"

EXPOSE 8080
CMD ["node", "src/app.js"]
```

### 2. Kubernetes Configuration

```yaml
# k8s/configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: app-config
data:
  app.tsk: |
    name: "MyApp"
    version: "1.0.0"
    
    [server]
    host: "0.0.0.0"
    port: 8080

---
# k8s/secret.yaml
apiVersion: v1
kind: Secret
metadata:
  name: app-secrets
type: Opaque
data:
  DB_PASSWORD: <base64-encoded-password>
  JWT_SECRET: <base64-encoded-jwt-secret>

---
# k8s/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: app
spec:
  replicas: 3
  selector:
    matchLabels:
      app: myapp
  template:
    metadata:
      labels:
        app: myapp
    spec:
      containers:
      - name: app
        image: myapp:latest
        ports:
        - containerPort: 8080
        env:
        - name: NODE_ENV
          value: "production"
        - name: DB_PASSWORD
          valueFrom:
            secretKeyRef:
              name: app-secrets
              key: DB_PASSWORD
        - name: JWT_SECRET
          valueFrom:
            secretKeyRef:
              name: app-secrets
              key: JWT_SECRET
        volumeMounts:
        - name: config
          mountPath: /app/config
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
      volumes:
      - name: config
        configMap:
          name: app-config
```

### 3. CI/CD Pipeline

```yaml
# .github/workflows/deploy.yml
name: Deploy Application

on:
  push:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - uses: actions/setup-node@v3
      with:
        node-version: '16'
    - run: npm ci
    - run: npm test
    - run: npm run lint

  build:
    needs: test
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - uses: actions/setup-node@v3
      with:
        node-version: '16'
    - run: npm ci
    - run: npm run build:config
    - run: docker build -t myapp:${{ github.sha }} .
    - run: docker push myapp:${{ github.sha }}

  deploy:
    needs: build
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - run: kubectl set image deployment/app app=myapp:${{ github.sha }}
```

## Monitoring and Observability

### 1. Configuration Monitoring

```javascript
class ConfigurationMonitor {
  constructor() {
    this.metrics = {
      parseTime: [],
      validationErrors: 0,
      reloadCount: 0
    };
  }
  
  recordParseTime(duration) {
    this.metrics.parseTime.push(duration);
    if (this.metrics.parseTime.length > 100) {
      this.metrics.parseTime.shift();
    }
  }
  
  recordValidationError() {
    this.metrics.validationErrors++;
  }
  
  recordReload() {
    this.metrics.reloadCount++;
  }
  
  getMetrics() {
    const parseTimes = this.metrics.parseTime;
    return {
      averageParseTime: parseTimes.length > 0 
        ? parseTimes.reduce((a, b) => a + b, 0) / parseTimes.length 
        : 0,
      validationErrors: this.metrics.validationErrors,
      reloadCount: this.metrics.reloadCount,
      lastParseTime: parseTimes[parseTimes.length - 1] || 0
    };
  }
}
```

### 2. Health Checks

```javascript
class ConfigurationHealthCheck {
  constructor(configManager) {
    this.configManager = configManager;
  }
  
  async check() {
    const checks = {
      configLoaded: false,
      configValid: false,
      databaseConnected: false,
      lastReload: null
    };
    
    try {
      // Check if configuration is loaded
      const config = this.configManager.getConfig();
      checks.configLoaded = !!config;
      
      // Validate configuration
      const errors = this.configManager.validate();
      checks.configValid = errors.length === 0;
      
      // Check database connection
      if (config?.database) {
        checks.databaseConnected = await this.checkDatabase(config.database);
      }
      
      // Get last reload time
      checks.lastReload = this.configManager.getLastReloadTime();
      
    } catch (error) {
      console.error('Health check failed:', error);
    }
    
    return {
      status: checks.configLoaded && checks.configValid ? 'healthy' : 'unhealthy',
      checks,
      timestamp: new Date().toISOString()
    };
  }
  
  async checkDatabase(dbConfig) {
    try {
      const { Pool } = require('pg');
      const pool = new Pool(dbConfig);
      const client = await pool.connect();
      await client.query('SELECT 1');
      client.release();
      await pool.end();
      return true;
    } catch (error) {
      return false;
    }
  }
}
```

### 3. Logging Strategy

```javascript
class ConfigurationLogger {
  constructor(config) {
    this.config = config;
    this.logger = this.createLogger();
  }
  
  createLogger() {
    const winston = require('winston');
    
    const transports = [];
    
    if (this.config.logging.console) {
      transports.push(new winston.transports.Console({
        format: winston.format.combine(
          winston.format.timestamp(),
          winston.format.errors({ stack: true }),
          winston.format.json()
        )
      }));
    }
    
    if (this.config.logging.file) {
      transports.push(new winston.transports.File({
        filename: this.config.logging.file,
        format: winston.format.combine(
          winston.format.timestamp(),
          winston.format.errors({ stack: true }),
          winston.format.json()
        )
      }));
    }
    
    return winston.createLogger({
      level: this.config.logging.level,
      transports,
      defaultMeta: { service: 'configuration' }
    });
  }
  
  logConfigLoad(filePath, duration) {
    this.logger.info('Configuration loaded', {
      file: filePath,
      duration,
      timestamp: new Date().toISOString()
    });
  }
  
  logConfigError(error, filePath) {
    this.logger.error('Configuration error', {
      error: error.message,
      file: filePath,
      stack: error.stack,
      timestamp: new Date().toISOString()
    });
  }
  
  logConfigReload(reason) {
    this.logger.info('Configuration reloaded', {
      reason,
      timestamp: new Date().toISOString()
    });
  }
}
```

## Code Organization

### 1. Modular Configuration Structure

```javascript
// config/index.js
const TuskLang = require('tusklang');
const path = require('path');

class ConfigurationManager {
  constructor() {
    this.tusk = new TuskLang();
    this.configs = new Map();
    this.watchers = new Map();
  }
  
  async loadConfig(name, filePath) {
    const config = await this.tusk.parseFile(filePath);
    this.configs.set(name, config);
    return config;
  }
  
  getConfig(name) {
    return this.configs.get(name);
  }
  
  watchConfig(name, filePath, callback) {
    const fs = require('fs');
    const watcher = fs.watch(filePath, async (eventType) => {
      if (eventType === 'change') {
        try {
          const config = await this.loadConfig(name, filePath);
          callback(config);
        } catch (error) {
          console.error(`Failed to reload config ${name}:`, error);
        }
      }
    });
    
    this.watchers.set(name, watcher);
  }
  
  stopWatching(name) {
    const watcher = this.watchers.get(name);
    if (watcher) {
      watcher.close();
      this.watchers.delete(name);
    }
  }
}

module.exports = ConfigurationManager;
```

### 2. Service Layer Pattern

```javascript
// services/configService.js
class ConfigService {
  constructor() {
    this.configManager = new ConfigurationManager();
    this.cache = new ConfigCache();
    this.monitor = new ConfigurationMonitor();
    this.logger = new ConfigurationLogger();
  }
  
  async initialize() {
    // Load base configuration
    await this.configManager.loadConfig('base', './config/base.tsk');
    
    // Load environment-specific configuration
    const env = process.env.NODE_ENV || 'development';
    await this.configManager.loadConfig('env', `./config/${env}.tsk`);
    
    // Set up file watching in development
    if (env === 'development') {
      this.configManager.watchConfig('base', './config/base.tsk', (config) => {
        this.logger.logConfigReload('base config changed');
        this.cache.clear();
      });
    }
  }
  
  get(path) {
    const cached = this.cache.get(path);
    if (cached) return cached;
    
    const value = this.getNestedValue(this.getMergedConfig(), path);
    this.cache.set(path, value);
    return value;
  }
  
  getMergedConfig() {
    const base = this.configManager.getConfig('base');
    const env = this.configManager.getConfig('env');
    return { ...base, ...env };
  }
  
  getNestedValue(obj, path) {
    return path.split('.').reduce((current, key) => current?.[key], obj);
  }
  
  async validate() {
    const config = this.getMergedConfig();
    const schema = await this.loadValidationSchema();
    return this.tusk.validate(config, schema);
  }
}
```

### 3. Dependency Injection

```javascript
// container.js
class Container {
  constructor() {
    this.services = new Map();
    this.singletons = new Map();
  }
  
  register(name, factory, singleton = false) {
    this.services.set(name, { factory, singleton });
  }
  
  async resolve(name) {
    const service = this.services.get(name);
    if (!service) {
      throw new Error(`Service not registered: ${name}`);
    }
    
    if (service.singleton) {
      if (this.singletons.has(name)) {
        return this.singletons.get(name);
      }
      
      const instance = await service.factory(this);
      this.singletons.set(name, instance);
      return instance;
    }
    
    return await service.factory(this);
  }
  
  async resolveAll() {
    const resolved = {};
    for (const [name] of this.services) {
      resolved[name] = await this.resolve(name);
    }
    return resolved;
  }
}

// Usage
const container = new Container();

container.register('configService', async (container) => {
  const service = new ConfigService();
  await service.initialize();
  return service;
}, true);

container.register('databaseService', async (container) => {
  const configService = await container.resolve('configService');
  const dbConfig = configService.get('database');
  return new DatabaseService(dbConfig);
}, true);

// Resolve all services
const services = await container.resolveAll();
```

---

**These best practices will help you build robust, secure, and maintainable applications with TuskLang JavaScript SDK. Remember to adapt these patterns to your specific use case and requirements.** 