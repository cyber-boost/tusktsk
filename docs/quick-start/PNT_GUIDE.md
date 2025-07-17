# 🥜 Peanut Binary Configuration Guide for JavaScript

A comprehensive guide to using TuskLang's high-performance binary configuration system with JavaScript.

## Table of Contents

- [What is Peanut Configuration?](#what-is-peanut-configuration)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Core Concepts](#core-concepts)
- [API Reference](#api-reference)
- [Advanced Usage](#advanced-usage)
- [JavaScript-Specific Features](#javascript-specific-features)
- [Integration Examples](#integration-examples)
- [Binary Format Details](#binary-format-details)
- [Performance Guide](#performance-guide)
- [Troubleshooting](#troubleshooting)
- [Migration Guide](#migration-guide)
- [Complete Examples](#complete-examples)
- [Quick Reference](#quick-reference)

## What is Peanut Configuration?

Peanut Configuration is TuskLang's high-performance binary configuration system that provides:

- **85% faster loading** compared to text-based formats
- **Hierarchical configuration** with CSS-like cascading
- **Type inference** and validation
- **Binary format** for production deployments
- **File watching** for development

The system supports three file formats:
- `.peanuts` - Human-readable configuration
- `.tsk` - TuskLang syntax with advanced features
- `.pnt` - Compiled binary format (fastest)

## Installation

### Prerequisites

- Node.js version 16.0 or higher
- npm or yarn package manager
- TuskLang JavaScript SDK installed

### Installing the SDK

```bash
# Install TuskLang JavaScript SDK
npm install tusklang

# Or with yarn
yarn add tusklang
```

### Importing PeanutConfig

```javascript
// CommonJS
const { PeanutConfig } = require('tusklang');

// ES Modules
import { PeanutConfig } from 'tusklang';

// TypeScript
import { PeanutConfig, ConfigOptions } from 'tusklang';
```

## Quick Start

### Your First Peanut Configuration

1. Create a `peanu.peanuts` file:

```ini
[app]
name: "My JavaScript App"
version: "1.0.0"

[server]
host: "localhost"
port: 8080

[database]
host: "localhost"
port: 5432
name: "myapp"
```

2. Load the configuration:

```javascript
const { PeanutConfig } = require('tusklang');

// Load configuration from current directory
const config = PeanutConfig.load();

// Access values
console.log(config.get('app.name')); // "My JavaScript App"
console.log(config.get('server.port')); // 8080
```

3. Use in your application:

```javascript
const express = require('express');
const { PeanutConfig } = require('tusklang');

const config = PeanutConfig.load();
const app = express();

app.listen(config.get('server.port'), config.get('server.host'), () => {
    console.log(`Server running on ${config.get('server.host')}:${config.get('server.port')}`);
});
```

## Core Concepts

### File Types

- **`.peanuts`** - Human-readable configuration with INI-like syntax
- **`.tsk`** - TuskLang syntax with advanced features like variables and functions
- **`.pnt`** - Compiled binary format for production (85% faster loading)

### Hierarchical Loading

Configuration files are loaded in a hierarchical manner, similar to CSS cascading:

```
project/
├── peanu.peanuts          # Base configuration
├── src/
│   ├── peanu.peanuts      # Overrides base config
│   └── api/
│       └── peanu.peanuts  # API-specific overrides
```

```javascript
// Load with hierarchical resolution
const config = PeanutConfig.load('./src/api');

// Values are resolved from most specific to least specific
console.log(config.get('server.port')); // From api/peanu.peanuts
```

### Type System

PeanutConfig automatically infers types from values:

```javascript
const config = PeanutConfig.load();

// String values
config.get('app.name'); // string: "My App"

// Numeric values
config.get('server.port'); // number: 8080

// Boolean values
config.get('debug.enabled'); // boolean: true

// Array values
config.get('features'); // array: ["auth", "api", "websocket"]

// Object values
config.get('database'); // object: { host: "localhost", port: 5432 }
```

## API Reference

### PeanutConfig Class

#### Constructor

```javascript
const config = new PeanutConfig(options);
```

**Options:**
- `directory` (string): Configuration directory path
- `watch` (boolean): Enable file watching (default: false)
- `autoCompile` (boolean): Auto-compile to binary (default: true)
- `cache` (boolean): Enable caching (default: true)

#### Methods

##### load(directory, options)

Load configuration from directory with hierarchical resolution.

```javascript
// Load from current directory
const config = PeanutConfig.load();

// Load from specific directory
const config = PeanutConfig.load('./config');

// Load with options
const config = PeanutConfig.load('./config', {
    watch: true,
    autoCompile: false
});
```

**Parameters:**
- `directory` (string): Configuration directory path
- `options` (object): Configuration options

**Returns:** PeanutConfig instance

##### get(keyPath, defaultValue)

Get configuration value by key path.

```javascript
const config = PeanutConfig.load();

// Simple key
const appName = config.get('app.name');

// Nested key
const dbPort = config.get('database.options.port');

// With default value
const timeout = config.get('server.timeout', 30);

// Array access
const firstFeature = config.get('features.0');

// Object access
const dbConfig = config.get('database');
```

**Parameters:**
- `keyPath` (string): Dot-notation key path
- `defaultValue` (any): Default value if key not found

**Returns:** Configuration value

##### set(keyPath, value)

Set configuration value by key path.

```javascript
const config = PeanutConfig.load();

// Set simple value
config.set('app.version', '2.0.0');

// Set nested value
config.set('database.options.pool_size', 20);

// Set array value
config.set('features.2', 'websocket');
```

**Parameters:**
- `keyPath` (string): Dot-notation key path
- `value` (any): Value to set

**Returns:** void

##### compile(inputFile, outputFile)

Compile configuration file to binary format.

```javascript
// Compile .peanuts to .pnt
PeanutConfig.compile('config.peanuts', 'config.pnt');

// Compile .tsk to .pnt
PeanutConfig.compile('config.tsk', 'config.pnt');

// Auto-detect input format
PeanutConfig.compile('config', 'config.pnt');
```

**Parameters:**
- `inputFile` (string): Input configuration file path
- `outputFile` (string): Output binary file path

**Returns:** Promise<void>

##### watch(callback)

Watch for configuration file changes.

```javascript
const config = PeanutConfig.load();

config.watch((event, filename) => {
    console.log(`Configuration changed: ${filename}`);
    
    // Reload configuration
    config.reload();
});
```

**Parameters:**
- `callback` (function): Change event callback

**Returns:** void

##### reload()

Reload configuration from files.

```javascript
const config = PeanutConfig.load();

// Manual reload
config.reload();

// Reload with new directory
config.reload('./new-config');
```

**Parameters:**
- `directory` (string, optional): New configuration directory

**Returns:** void

##### validate()

Validate configuration against schema.

```javascript
const config = PeanutConfig.load();

try {
    config.validate();
    console.log('Configuration is valid');
} catch (error) {
    console.error('Configuration validation failed:', error.message);
}
```

**Returns:** boolean

##### export(format)

Export configuration to different formats.

```javascript
const config = PeanutConfig.load();

// Export as JSON
const json = config.export('json');

// Export as YAML
const yaml = config.export('yaml');

// Export as INI
const ini = config.export('ini');
```

**Parameters:**
- `format` (string): Export format ('json', 'yaml', 'ini')

**Returns:** string

## Advanced Usage

### File Watching

```javascript
const { PeanutConfig } = require('tusklang');

const config = PeanutConfig.load('./config', { watch: true });

config.watch((event, filename) => {
    switch (event) {
        case 'change':
            console.log(`Configuration file changed: ${filename}`);
            config.reload();
            break;
        case 'add':
            console.log(`New configuration file: ${filename}`);
            break;
        case 'unlink':
            console.log(`Configuration file removed: ${filename}`);
            break;
    }
});

// Stop watching
config.unwatch();
```

### Custom Serialization

```javascript
const { PeanutConfig } = require('tusklang');

class CustomPeanutConfig extends PeanutConfig {
    // Custom type handling
    serializeValue(value) {
        if (value instanceof Date) {
            return { type: 'date', value: value.toISOString() };
        }
        return super.serializeValue(value);
    }

    deserializeValue(data) {
        if (data.type === 'date') {
            return new Date(data.value);
        }
        return super.deserializeValue(data);
    }
}

const config = new CustomPeanutConfig();
config.set('app.created', new Date());
```

### Performance Optimization

```javascript
const { PeanutConfig } = require('tusklang');

// Singleton pattern for production
class ConfigManager {
    constructor() {
        if (ConfigManager.instance) {
            return ConfigManager.instance;
        }
        
        this.config = PeanutConfig.load('./config', {
            cache: true,
            autoCompile: true
        });
        
        ConfigManager.instance = this;
    }
    
    get(key, defaultValue) {
        return this.config.get(key, defaultValue);
    }
}

// Use singleton
const config = new ConfigManager();
```

### Thread Safety

```javascript
const { PeanutConfig } = require('tusklang');

// For Node.js worker threads
const config = PeanutConfig.load('./config', {
    cache: true,
    lockFile: true // Prevents concurrent access issues
});

// Safe concurrent access
async function safeConfigAccess() {
    await config.lock();
    try {
        const value = config.get('some.key');
        return value;
    } finally {
        config.unlock();
    }
}
```

## JavaScript-Specific Features

### Promise-based API

```javascript
const { PeanutConfig } = require('tusklang');

// Async loading
async function loadConfig() {
    try {
        const config = await PeanutConfig.loadAsync('./config');
        return config;
    } catch (error) {
        console.error('Failed to load configuration:', error);
        throw error;
    }
}

// Async compilation
async function compileConfig() {
    try {
        await PeanutConfig.compileAsync('config.peanuts', 'config.pnt');
        console.log('Configuration compiled successfully');
    } catch (error) {
        console.error('Compilation failed:', error);
    }
}
```

### TypeScript Support

```typescript
import { PeanutConfig, ConfigOptions } from 'tusklang';

// Type-safe configuration interface
interface AppConfig {
    app: {
        name: string;
        version: string;
    };
    server: {
        host: string;
        port: number;
    };
    database: {
        host: string;
        port: number;
        name: string;
    };
}

// Type-safe configuration loading
const config = await PeanutConfig.load<AppConfig>('./config');

// Type-safe value access
const appName: string = config.get('app.name');
const serverPort: number = config.get('server.port');
```

### ES Modules Support

```javascript
// ES Module import
import { PeanutConfig } from 'tusklang';

// Dynamic import
const { PeanutConfig } = await import('tusklang');

// Named exports
import { PeanutConfig, compileConfig, validateConfig } from 'tusklang';
```

### Node.js Integration

```javascript
const { PeanutConfig } = require('tusklang');
const path = require('path');

// Environment-specific configuration
const env = process.env.NODE_ENV || 'development';
const configDir = path.join(__dirname, 'config', env);

const config = PeanutConfig.load(configDir);

// Use in Express.js
const express = require('express');
const app = express();

app.listen(config.get('server.port'), () => {
    console.log(`Server running on port ${config.get('server.port')}`);
});
```

## Integration Examples

### Express.js Integration

```javascript
const express = require('express');
const { PeanutConfig } = require('tusklang');

class App {
    constructor() {
        this.app = express();
        this.config = PeanutConfig.load('./config');
        this.setupMiddleware();
        this.setupRoutes();
    }

    setupMiddleware() {
        // CORS configuration
        const corsOptions = {
            origin: this.config.get('cors.allowedOrigins', ['http://localhost:3000']),
            credentials: this.config.get('cors.credentials', true)
        };
        
        this.app.use(require('cors')(corsOptions));
    }

    setupRoutes() {
        this.app.get('/api/status', (req, res) => {
            res.json({
                status: 'ok',
                version: this.config.get('app.version'),
                environment: this.config.get('app.environment')
            });
        });
    }

    start() {
        const port = this.config.get('server.port', 3000);
        const host = this.config.get('server.host', 'localhost');
        
        this.app.listen(port, host, () => {
            console.log(`Server running on ${host}:${port}`);
        });
    }
}

new App().start();
```

### React Integration

```javascript
// config/peanut-config.js
import { PeanutConfig } from 'tusklang';

class ReactPeanutConfig {
    constructor() {
        this.config = PeanutConfig.load('./config');
    }

    get(key, defaultValue) {
        return this.config.get(key, defaultValue);
    }

    // React-specific configuration
    getApiUrl() {
        return this.config.get('api.baseUrl');
    }

    getFeatureFlags() {
        return this.config.get('features', {});
    }
}

export default new ReactPeanutConfig();

// App.js
import React from 'react';
import config from './config/peanut-config';

function App() {
    const apiUrl = config.getApiUrl();
    const features = config.getFeatureFlags();

    return (
        <div>
            <h1>{config.get('app.name')}</h1>
            {features.chat && <ChatComponent />}
        </div>
    );
}
```

### Next.js Integration

```javascript
// next.config.js
const { PeanutConfig } = require('tusklang');

const config = PeanutConfig.load('./config');

module.exports = {
    env: {
        API_URL: config.get('api.baseUrl'),
        APP_NAME: config.get('app.name'),
    },
    serverRuntimeConfig: {
        database: config.get('database'),
    },
    publicRuntimeConfig: {
        features: config.get('features'),
    },
};

// pages/api/status.js
export default function handler(req, res) {
    const { serverRuntimeConfig } = getConfig();
    
    res.status(200).json({
        status: 'ok',
        database: serverRuntimeConfig.database.host
    });
}
```

## Binary Format Details

### File Structure

| Offset | Size | Description |
|--------|------|-------------|
| 0 | 4 | Magic: "PNUT" |
| 4 | 4 | Version (LE) |
| 8 | 8 | Timestamp (LE) |
| 16 | 8 | SHA256 checksum |
| 24 | N | Serialized data |

### Serialization Format

```javascript
// Binary format specification
const BINARY_HEADER = {
    MAGIC: Buffer.from('PNUT'),
    VERSION: 1,
    HEADER_SIZE: 24
};

// Serialization example
function serializeConfig(config) {
    const data = JSON.stringify(config);
    const checksum = crypto.createHash('sha256').update(data).digest();
    
    const header = Buffer.alloc(24);
    BINARY_HEADER.MAGIC.copy(header, 0);
    header.writeUInt32LE(BINARY_HEADER.VERSION, 4);
    header.writeBigUInt64LE(BigInt(Date.now()), 8);
    checksum.copy(header, 16);
    
    return Buffer.concat([header, Buffer.from(data)]);
}
```

## Performance Guide

### Benchmarks

```javascript
const { PeanutConfig } = require('tusklang');
const fs = require('fs');

// Performance comparison
async function benchmark() {
    const iterations = 1000;
    
    // JSON loading
    const jsonStart = Date.now();
    for (let i = 0; i < iterations; i++) {
        const data = JSON.parse(fs.readFileSync('config.json', 'utf8'));
    }
    const jsonTime = Date.now() - jsonStart;
    
    // Peanut binary loading
    const peanutStart = Date.now();
    for (let i = 0; i < iterations; i++) {
        const config = PeanutConfig.loadBinary('config.pnt');
    }
    const peanutTime = Date.now() - peanutStart;
    
    console.log(`JSON: ${jsonTime}ms`);
    console.log(`Peanut: ${peanutTime}ms`);
    console.log(`Speedup: ${(jsonTime / peanutTime).toFixed(2)}x`);
}

benchmark();
```

### Best Practices

1. **Always use .pnt in production**
   ```javascript
   // Development
   const config = PeanutConfig.load('./config');
   
   // Production
   const config = PeanutConfig.loadBinary('./config.pnt');
   ```

2. **Cache configuration objects**
   ```javascript
   // Singleton pattern
   class ConfigCache {
       static instance = null;
       
       static getInstance() {
           if (!ConfigCache.instance) {
               ConfigCache.instance = PeanutConfig.load('./config');
           }
           return ConfigCache.instance;
       }
   }
   ```

3. **Use file watching wisely**
   ```javascript
   // Only in development
   const isDev = process.env.NODE_ENV === 'development';
   const config = PeanutConfig.load('./config', { watch: isDev });
   ```

4. **Optimize for your use case**
   ```javascript
   // For frequent access
   const config = PeanutConfig.load('./config', { cache: true });
   
   // For memory-constrained environments
   const config = PeanutConfig.load('./config', { cache: false });
   ```

## Troubleshooting

### Common Issues

#### File Not Found

```javascript
try {
    const config = PeanutConfig.load('./config');
} catch (error) {
    if (error.code === 'ENOENT') {
        console.error('Configuration directory not found');
        console.log('Creating default configuration...');
        
        // Create default configuration
        const defaultConfig = {
            app: { name: 'Default App', version: '1.0.0' },
            server: { host: 'localhost', port: 3000 }
        };
        
        fs.writeFileSync('peanu.peanuts', JSON.stringify(defaultConfig, null, 2));
    }
}
```

#### Checksum Mismatch

```javascript
try {
    const config = PeanutConfig.loadBinary('config.pnt');
} catch (error) {
    if (error.message.includes('checksum')) {
        console.error('Binary file corrupted, recompiling...');
        
        // Recompile from source
        PeanutConfig.compile('config.peanuts', 'config.pnt');
        const config = PeanutConfig.loadBinary('config.pnt');
    }
}
```

#### Performance Issues

```javascript
// Check if binary file exists
if (!fs.existsSync('config.pnt')) {
    console.log('Compiling configuration to binary...');
    PeanutConfig.compile('config.peanuts', 'config.pnt');
}

// Use binary loading for better performance
const config = PeanutConfig.loadBinary('config.pnt');
```

### Debug Mode

```javascript
// Enable debug logging
const config = PeanutConfig.load('./config', {
    debug: true,
    verbose: true
});

// Debug specific operations
config.debug = true;
config.get('some.key'); // Will log debug information
```

## Migration Guide

### From JSON

```javascript
// Old JSON configuration
const config = require('./config.json');

// Migration to Peanut
const { PeanutConfig } = require('tusklang');

// Convert JSON to .peanuts format
const peanutConfig = `[app]
name: "${config.app.name}"
version: "${config.app.version}"

[server]
host: "${config.server.host}"
port: ${config.server.port}
`;

fs.writeFileSync('peanu.peanuts', peanutConfig);

// Use PeanutConfig
const newConfig = PeanutConfig.load();
```

### From YAML

```javascript
const yaml = require('js-yaml');
const { PeanutConfig } = require('tusklang');

// Load YAML
const yamlConfig = yaml.load(fs.readFileSync('config.yaml', 'utf8'));

// Convert to .peanuts format
function yamlToPeanuts(obj, prefix = '') {
    let result = '';
    
    for (const [key, value] of Object.entries(obj)) {
        const fullKey = prefix ? `${prefix}.${key}` : key;
        
        if (typeof value === 'object' && !Array.isArray(value)) {
            result += `[${fullKey}]\n`;
            result += yamlToPeanuts(value, fullKey);
        } else {
            const formattedValue = typeof value === 'string' ? `"${value}"` : value;
            result += `${key}: ${formattedValue}\n`;
        }
    }
    
    return result;
}

const peanutConfig = yamlToPeanuts(yamlConfig);
fs.writeFileSync('peanu.peanuts', peanutConfig);
```

### From .env

```javascript
const dotenv = require('dotenv');
const { PeanutConfig } = require('tusklang');

// Load .env
dotenv.config();

// Convert to .peanuts format
const envVars = process.env;
let peanutConfig = '[app]\n';

for (const [key, value] of Object.entries(envVars)) {
    if (key.startsWith('APP_')) {
        const configKey = key.replace('APP_', '').toLowerCase();
        peanutConfig += `${configKey}: "${value}"\n`;
    }
}

fs.writeFileSync('peanu.peanuts', peanutConfig);
```

## Complete Examples

### Web Application Configuration

```javascript
// config/peanu.peanuts
[app]
name: "My Web App"
version: "2.0.0"
environment: "production"

[server]
host: "0.0.0.0"
port: 3000
timeout: 30

[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: "secret"

[redis]
host: "localhost"
port: 6379
db: 0

[features]
auth: true
api: true
websocket: true
chat: false

[cors]
allowedOrigins: ["http://localhost:3000", "https://myapp.com"]
credentials: true
```

```javascript
// app.js
const express = require('express');
const { PeanutConfig } = require('tusklang');
const cors = require('cors');
const helmet = require('helmet');

class WebApp {
    constructor() {
        this.app = express();
        this.config = PeanutConfig.load('./config');
        this.setupMiddleware();
        this.setupRoutes();
    }

    setupMiddleware() {
        // Security
        this.app.use(helmet());
        
        // CORS
        const corsOptions = {
            origin: this.config.get('cors.allowedOrigins'),
            credentials: this.config.get('cors.credentials')
        };
        this.app.use(cors(corsOptions));
        
        // Body parsing
        this.app.use(express.json());
    }

    setupRoutes() {
        // Health check
        this.app.get('/health', (req, res) => {
            res.json({
                status: 'ok',
                version: this.config.get('app.version'),
                environment: this.config.get('app.environment')
            });
        });

        // API routes
        if (this.config.get('features.api')) {
            this.app.use('/api', require('./routes/api'));
        }

        // WebSocket
        if (this.config.get('features.websocket')) {
            const WebSocket = require('ws');
            const wss = new WebSocket.Server({ server: this.server });
            require('./websocket')(wss, this.config);
        }
    }

    start() {
        const port = this.config.get('server.port');
        const host = this.config.get('server.host');
        
        this.server = this.app.listen(port, host, () => {
            console.log(`Server running on ${host}:${port}`);
            console.log(`Environment: ${this.config.get('app.environment')}`);
        });
    }
}

new WebApp().start();
```

### Microservice Configuration

```javascript
// config/peanu.peanuts
[service]
name: "user-service"
version: "1.0.0"
port: 3001

[database]
host: "user-db"
port: 5432
name: "users"
pool_size: 10

[redis]
host: "redis"
port: 6379
db: 1

[api]
rate_limit: 1000
timeout: 30

[logging]
level: "info"
format: "json"
```

```javascript
// service.js
const { PeanutConfig } = require('tusklang');
const { Pool } = require('pg');
const Redis = require('ioredis');

class UserService {
    constructor() {
        this.config = PeanutConfig.load('./config');
        this.setupDatabase();
        this.setupRedis();
        this.setupServer();
    }

    setupDatabase() {
        this.db = new Pool({
            host: this.config.get('database.host'),
            port: this.config.get('database.port'),
            database: this.config.get('database.name'),
            max: this.config.get('database.pool_size')
        });
    }

    setupRedis() {
        this.redis = new Redis({
            host: this.config.get('redis.host'),
            port: this.config.get('redis.port'),
            db: this.config.get('redis.db')
        });
    }

    setupServer() {
        const express = require('express');
        this.app = express();
        
        // Rate limiting
        const rateLimit = require('express-rate-limit');
        this.app.use(rateLimit({
            windowMs: 15 * 60 * 1000,
            max: this.config.get('api.rate_limit')
        }));
        
        this.setupRoutes();
    }

    setupRoutes() {
        this.app.get('/users/:id', async (req, res) => {
            try {
                const user = await this.getUser(req.params.id);
                res.json(user);
            } catch (error) {
                res.status(500).json({ error: error.message });
            }
        });
    }

    async getUser(id) {
        // Check cache first
        const cached = await this.redis.get(`user:${id}`);
        if (cached) {
            return JSON.parse(cached);
        }

        // Query database
        const result = await this.db.query('SELECT * FROM users WHERE id = $1', [id]);
        const user = result.rows[0];

        // Cache result
        await this.redis.setex(`user:${id}`, 300, JSON.stringify(user));

        return user;
    }

    start() {
        const port = this.config.get('service.port');
        this.app.listen(port, () => {
            console.log(`${this.config.get('service.name')} running on port ${port}`);
        });
    }
}

new UserService().start();
```

### CLI Tool Configuration

```javascript
// config/peanu.peanuts
[cli]
name: "my-cli"
version: "1.0.0"
description: "A powerful CLI tool"

[commands]
default: "help"
timeout: 30

[output]
format: "table"
colors: true
verbose: false

[api]
base_url: "https://api.example.com"
timeout: 10
retries: 3
```

```javascript
// cli.js
#!/usr/bin/env node

const { Command } = require('commander');
const { PeanutConfig } = require('tusklang');
const chalk = require('chalk');

class CLI {
    constructor() {
        this.config = PeanutConfig.load('./config');
        this.program = new Command();
        this.setupCommands();
    }

    setupCommands() {
        this.program
            .name(this.config.get('cli.name'))
            .version(this.config.get('cli.version'))
            .description(this.config.get('cli.description'));

        // List command
        this.program
            .command('list')
            .description('List items')
            .option('-v, --verbose', 'Verbose output')
            .action(async (options) => {
                await this.listItems(options);
            });

        // Get command
        this.program
            .command('get <id>')
            .description('Get item by ID')
            .action(async (id) => {
                await this.getItem(id);
            });
    }

    async listItems(options) {
        try {
            const response = await fetch(`${this.config.get('api.base_url')}/items`, {
                timeout: this.config.get('api.timeout') * 1000
            });
            
            const items = await response.json();
            
            if (this.config.get('output.format') === 'table') {
                this.displayTable(items);
            } else {
                console.log(JSON.stringify(items, null, 2));
            }
        } catch (error) {
            console.error(chalk.red('Error:'), error.message);
            process.exit(1);
        }
    }

    async getItem(id) {
        try {
            const response = await fetch(`${this.config.get('api.base_url')}/items/${id}`, {
                timeout: this.config.get('api.timeout') * 1000
            });
            
            const item = await response.json();
            console.log(JSON.stringify(item, null, 2));
        } catch (error) {
            console.error(chalk.red('Error:'), error.message);
            process.exit(1);
        }
    }

    displayTable(items) {
        const Table = require('cli-table3');
        const table = new Table({
            head: ['ID', 'Name', 'Status']
        });

        items.forEach(item => {
            table.push([item.id, item.name, item.status]);
        });

        console.log(table.toString());
    }

    run() {
        this.program.parse();
    }
}

new CLI().run();
```

## Quick Reference

### Common Operations

```javascript
// Load configuration
const config = PeanutConfig.load();

// Get value with default
const value = config.get('key.path', defaultValue);

// Set value
config.set('key.path', value);

// Compile to binary
await PeanutConfig.compile('config.peanuts', 'config.pnt');

// Load binary
const config = PeanutConfig.loadBinary('config.pnt');

// Watch for changes
config.watch((event, filename) => {
    console.log(`File ${filename} changed`);
});

// Export to different formats
const json = config.export('json');
const yaml = config.export('yaml');
```

### Configuration File Examples

```ini
# peanu.peanuts
[app]
name: "My App"
version: "1.0.0"

[server]
host: "localhost"
port: 3000

[database]
host: "localhost"
port: 5432
name: "myapp"
```

```javascript
// peanu.tsk
$app_name: "My App"
$version: "1.0.0"

app {
    name: $app_name
    version: $version
}

server {
    host: "localhost"
    port: 3000
}

database {
    host: "localhost"
    port: 5432
    name: "myapp"
}
```

### Error Handling

```javascript
try {
    const config = PeanutConfig.load('./config');
    const value = config.get('required.key');
} catch (error) {
    if (error.code === 'ENOENT') {
        console.error('Configuration file not found');
    } else if (error.message.includes('Invalid key')) {
        console.error('Invalid configuration key');
    } else {
        console.error('Configuration error:', error.message);
    }
}
```

---

This guide provides everything you need to use Peanut Configuration effectively with JavaScript. For more information, visit the [TuskLang documentation](https://tusklang.org). 