# TuskLang JavaScript SDK - API Reference

**Complete API documentation for TuskLang JavaScript SDK v2.0.2**

## Table of Contents

- [Core Classes](#core-classes)
- [CLI Commands](#cli-commands)
- [Configuration Parsing](#configuration-parsing)
- [Database Integration](#database-integration)
- [Binary Format](#binary-format)
- [Error Handling](#error-handling)
- [Performance Optimization](#performance-optimization)
- [TypeScript Support](#typescript-support)

## Core Classes

### TuskLang

The main parser class for TuskLang configuration files.

```javascript
const TuskLang = require('tusklang');
const tusk = new TuskLang(options);
```

#### Constructor Options

```javascript
const options = {
  // Enable debug logging
  debug: false,
  
  // Custom database adapter
  databaseAdapter: null,
  
  // Environment variables prefix
  envPrefix: 'TUSK_',
  
  // Strict mode (throws on errors)
  strict: true,
  
  // Allow unknown properties
  allowUnknown: false
};
```

#### Methods

##### `parse(configString, options)`

Parse a TuskLang configuration string.

```javascript
const config = tusk.parse(`
  name: "MyApp"
  version: "1.0.0"
  
  [database]
  host: "localhost"
  port: 5432
`);

console.log(config.name); // "MyApp"
console.log(config.database.host); // "localhost"
```

**Parameters:**
- `configString` (string): TuskLang configuration string
- `options` (object, optional): Parse options

**Returns:** Parsed configuration object

##### `parseFile(filePath, options)`

Parse a TuskLang configuration file.

```javascript
const config = tusk.parseFile('./config.tsk');
```

**Parameters:**
- `filePath` (string): Path to configuration file
- `options` (object, optional): Parse options

**Returns:** Parsed configuration object

##### `stringify(config, options)`

Convert a configuration object back to TuskLang format.

```javascript
const tskString = tusk.stringify(config, {
  format: 'bracket', // 'bracket', 'brace', 'angle'
  indent: 2,
  includeComments: true
});
```

**Parameters:**
- `config` (object): Configuration object
- `options` (object, optional): Stringify options

**Returns:** TuskLang formatted string

##### `validate(config)`

Validate a configuration object against a schema.

```javascript
const schema = {
  required: ['name', 'version'],
  properties: {
    name: { type: 'string' },
    version: { type: 'string' },
    port: { type: 'number', min: 1, max: 65535 }
  }
};

const errors = tusk.validate(config, schema);
if (errors.length > 0) {
  console.error('Validation errors:', errors);
}
```

**Parameters:**
- `config` (object): Configuration object
- `schema` (object): Validation schema

**Returns:** Array of validation errors

##### `setDatabaseAdapter(adapter)`

Set a custom database adapter for @query functions.

```javascript
const mysqlAdapter = {
  async query(sql, params) {
    // Execute MySQL query
    return await mysql.execute(sql, params);
  }
};

tusk.setDatabaseAdapter(mysqlAdapter);
```

**Parameters:**
- `adapter` (object): Database adapter with query method

### TuskLangEnhanced

Enhanced version with advanced features like variables, conditionals, and database queries.

```javascript
const TuskLangEnhanced = require('./tsk-enhanced.js');
const tusk = new TuskLangEnhanced();
```

#### Additional Methods

##### `setVariable(name, value)`

Set a global variable.

```javascript
tusk.setVariable('env', 'production');
tusk.setVariable('port', 8080);
```

##### `getVariable(name)`

Get a global variable.

```javascript
const env = tusk.getVariable('env');
```

##### `clearVariables()`

Clear all global variables.

```javascript
tusk.clearVariables();
```

### PeanutConfig

High-performance binary configuration system.

```javascript
const { PeanutConfig } = require('tusklang');
```

#### Static Methods

##### `PeanutConfig.load(options)`

Load configuration from current directory.

```javascript
const config = PeanutConfig.load({
  // Search paths
  paths: ['./config', '../config'],
  
  // File names to search
  files: ['peanu.peanuts', 'config.tsk', 'app.pnt'],
  
  // Environment
  env: process.env.NODE_ENV || 'development',
  
  // Watch for changes
  watch: true
});
```

##### `PeanutConfig.compile(inputFile, outputFile, options)`

Compile configuration to binary format.

```javascript
await PeanutConfig.compile(
  'config.tsk',
  'config.pnt',
  {
    optimize: true,
    compress: true,
    includeSourceMap: false
  }
);
```

##### `PeanutConfig.validate(filePath)`

Validate a configuration file.

```javascript
const errors = PeanutConfig.validate('config.tsk');
if (errors.length > 0) {
  console.error('Validation errors:', errors);
}
```

## CLI Commands

### Database Commands

```bash
# Check database connection
tsk db status

# Run migration
tsk db migrate schema.sql

# Interactive console
tsk db console

# Backup database
tsk db backup backup.sql

# Restore from backup
tsk db restore backup.sql

# Initialize database
tsk db init
```

### Development Commands

```bash
# Start development server
tsk serve 8080

# Compile configuration
tsk compile config.tsk

# Optimize for production
tsk optimize config.tsk

# Watch for changes
tsk watch config.tsk
```

### Testing Commands

```bash
# Run all tests
tsk test all

# Run specific test suite
tsk test parser
tsk test fujsen
tsk test sdk
tsk test performance

# Run with coverage
tsk test --coverage

# Run in watch mode
tsk test --watch
```

### Service Commands

```bash
# Start all services
tsk services start

# Stop all services
tsk services stop

# Restart services
tsk services restart

# Show status
tsk services status

# Show logs
tsk services logs
```

### Cache Commands

```bash
# Clear all caches
tsk cache clear

# Show cache status
tsk cache status

# Pre-warm caches
tsk cache warm

# Memcached operations
tsk cache memcached status
tsk cache memcached stats
tsk cache memcached flush
```

### Configuration Commands

```bash
# Get configuration value
tsk config get server.port

# Set configuration value
tsk config set server.port 8080

# Check configuration hierarchy
tsk config check

# Validate configuration
tsk config validate

# Auto-compile configuration
tsk config compile

# Generate documentation
tsk config docs

# Clear configuration cache
tsk config clear-cache

# Show statistics
tsk config stats
```

### Binary Commands

```bash
# Compile to binary
tsk binary compile config.tsk

# Execute binary file
tsk binary execute config.pnt

# Benchmark performance
tsk binary benchmark config.tsk

# Optimize binary
tsk binary optimize config.pnt

# Decompile binary
tsk binary decompile config.pnt
```

### AI Commands

```bash
# Query Claude AI
tsk ai claude "Explain TuskLang syntax"

# Query ChatGPT
tsk ai chatgpt "JavaScript best practices"

# Analyze code
tsk ai analyze app.js --focus security

# Get optimization suggestions
tsk ai optimize app.js --type performance

# Security scan
tsk ai security app.js --level thorough
```

### Utility Commands

```bash
# Parse TSK file
tsk parse config.tsk

# Validate syntax
tsk validate config.tsk

# Convert formats
tsk convert -i config.tsk -o config.json

# Get value by path
tsk get config.tsk server.port

# Set value by path
tsk set config.tsk server.port 8080

# Show help
tsk --help
tsk <command> --help
```

## Configuration Parsing

### Syntax Styles

TuskLang supports multiple syntax styles for maximum flexibility:

#### Bracket Style (TOML-like)

```tsk
[database]
host: "localhost"
port: 5432

[server]
host: "0.0.0.0"
port: 8080
```

#### Brace Style (JSON-like)

```tsk
database {
  host: "localhost"
  port: 5432
}

server {
  host: "0.0.0.0"
  port: 8080
}
```

#### Angle Bracket Style

```tsk
database >
  host: "localhost"
  port: 5432
<

server >
  host: "0.0.0.0"
  port: 8080
<
```

#### Mixed Style

```tsk
[database]
host: "localhost"

server {
  port: 8080
}

cache >
  ttl: 300
<
```

### Variables and References

```tsk
# Global variables
$app_name: "MyApp"
$port: 3000

# Use variables
name: $app_name
server_port: $port

# Section-local variables
[api]
$timeout: 30
endpoint: "/api/v1"
config_timeout: $timeout
```

### Conditional Expressions

```tsk
$env: "production"

[server]
debug: $env == "development" ? true : false
workers: $env == "production" ? 8 : 2
log_level: $env == "production" ? "error" : "debug"
```

### Date Functions

```tsk
timestamp: @date("Y-m-d H:i:s")
year: @date("Y")
month: @date("m")
iso_date: @date("c")
unix_timestamp: @date("U")
```

### Ranges

```tsk
port_range: 8000-9000
worker_range: 1-10
version_range: "1.0-2.0"
```

### Environment Variables

```tsk
node_env: @env("NODE_ENV", "development")
api_key: @env("API_KEY", "default_key")
database_url: @env("DATABASE_URL")
```

### String Concatenation

```tsk
$base: "TuskLang"
$version: "2.0"
full_name: $base + " v" + $version
api_url: "https://api.example.com/" + $base
```

### Database Queries

```tsk
[database]
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE status = 'active'")
rate_limit: @query("SELECT rate_limit FROM plans WHERE id = ?", 1)
```

## Database Integration

### Supported Databases

- **SQLite**: Built-in, no additional dependencies
- **PostgreSQL**: Requires `pg` package
- **MySQL**: Requires `mysql2` package
- **MongoDB**: Requires `mongodb` package
- **Redis**: Requires `redis` package

### Database Adapters

#### SQLite Adapter

```javascript
const sqlite3 = require('better-sqlite3');

const sqliteAdapter = {
  async query(sql, params = []) {
    const db = sqlite3('./database.db');
    const stmt = db.prepare(sql);
    return stmt.all(params);
  }
};

tusk.setDatabaseAdapter(sqliteAdapter);
```

#### PostgreSQL Adapter

```javascript
const { Pool } = require('pg');

const pool = new Pool({
  host: 'localhost',
  port: 5432,
  database: 'myapp',
  user: 'postgres',
  password: 'password'
});

const pgAdapter = {
  async query(sql, params = []) {
    const client = await pool.connect();
    try {
      const result = await client.query(sql, params);
      return result.rows;
    } finally {
      client.release();
    }
  }
};

tusk.setDatabaseAdapter(pgAdapter);
```

#### MySQL Adapter

```javascript
const mysql = require('mysql2/promise');

const connection = await mysql.createConnection({
  host: 'localhost',
  port: 3306,
  database: 'myapp',
  user: 'root',
  password: 'password'
});

const mysqlAdapter = {
  async query(sql, params = []) {
    const [rows] = await connection.execute(sql, params);
    return rows;
  }
};

tusk.setDatabaseAdapter(mysqlAdapter);
```

## Binary Format

### Compilation

```javascript
const { PeanutConfig } = require('tusklang');

// Compile to binary
await PeanutConfig.compile('config.tsk', 'config.pnt', {
  optimize: true,
  compress: true,
  includeSourceMap: false
});
```

### Execution

```javascript
// Load binary configuration
const config = PeanutConfig.load({
  files: ['config.pnt']
});
```

### Performance Benefits

- **85% faster loading** compared to text parsing
- **Reduced memory usage** with binary format
- **Type safety** with compiled validation
- **Production optimization** with compression

## Error Handling

### Parse Errors

```javascript
try {
  const config = tusk.parse(invalidConfig);
} catch (error) {
  if (error.name === 'ParseError') {
    console.error('Parse error at line', error.line);
    console.error('Column:', error.column);
    console.error('Message:', error.message);
  }
}
```

### Validation Errors

```javascript
const errors = tusk.validate(config, schema);
if (errors.length > 0) {
  errors.forEach(error => {
    console.error(`Validation error at ${error.path}: ${error.message}`);
  });
}
```

### Database Errors

```javascript
try {
  const result = await tusk.parse(configWithQuery);
} catch (error) {
  if (error.name === 'DatabaseError') {
    console.error('Database error:', error.message);
    console.error('SQL:', error.sql);
    console.error('Parameters:', error.params);
  }
}
```

## Performance Optimization

### Caching

```javascript
// Enable caching
const tusk = new TuskLang({
  cache: {
    enabled: true,
    ttl: 300, // 5 minutes
    maxSize: 100
  }
});
```

### Binary Compilation

```javascript
// Compile for production
await PeanutConfig.compile('config.tsk', 'config.pnt', {
  optimize: true,
  compress: true
});
```

### Lazy Loading

```javascript
// Load configuration lazily
const config = PeanutConfig.load({
  lazy: true,
  watch: false
});

// Access triggers loading
const port = config.get('server.port');
```

## TypeScript Support

### Type Definitions

```typescript
import { TuskLang, TuskLangEnhanced, PeanutConfig } from 'tusklang';

interface DatabaseConfig {
  host: string;
  port: number;
  database: string;
  username: string;
  password: string;
}

interface ServerConfig {
  host: string;
  port: number;
  ssl?: {
    enabled: boolean;
    cert: string;
  };
}

interface AppConfig {
  name: string;
  version: string;
  database: DatabaseConfig;
  server: ServerConfig;
}

const tusk = new TuskLangEnhanced();
const config: AppConfig = tusk.parse(configString);
```

### Type Validation

```typescript
import { validateConfig } from 'tusklang';

const schema = {
  type: 'object',
  required: ['name', 'version'],
  properties: {
    name: { type: 'string' },
    version: { type: 'string' },
    port: { type: 'number', minimum: 1, maximum: 65535 }
  }
};

const errors = validateConfig(config, schema);
```

## Examples

### Complete Application Configuration

```tsk
# Global variables
$app_name: "MyApp"
$env: @env("NODE_ENV", "development")

# Application configuration
name: $app_name
version: "1.0.0"
debug: $env == "development" ? true : false

# Database configuration
[database]
type: "postgres"
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", "5432")
database: $app_name + "_" + $env
username: @env("DB_USER", "postgres")
password: @env("DB_PASSWORD")
connection_limit: $env == "production" ? 20 : 5

# Server configuration
[server]
host: "0.0.0.0"
port: @env("PORT", "8080")
workers: $env == "production" ? 8 : 2
ssl: {
  enabled: $env == "production" ? true : false
  cert: @env("SSL_CERT", "/path/to/cert")
  key: @env("SSL_KEY", "/path/to/key")
}

# Cache configuration
[cache]
driver: "redis"
host: @env("REDIS_HOST", "localhost")
port: @env("REDIS_PORT", "6379")
ttl: 300

# Monitoring configuration
[monitoring]
enabled: true
metrics: {
  user_count: @query("SELECT COUNT(*) FROM users")
  active_sessions: @query("SELECT COUNT(*) FROM sessions WHERE expires_at > NOW()")
  uptime: @date("U")
}
```

### API Usage Example

```javascript
const TuskLangEnhanced = require('./tsk-enhanced.js');
const { Pool } = require('pg');

// Set up database adapter
const pool = new Pool({
  host: 'localhost',
  port: 5432,
  database: 'myapp',
  user: 'postgres',
  password: 'password'
});

const pgAdapter = {
  async query(sql, params = []) {
    const client = await pool.connect();
    try {
      const result = await client.query(sql, params);
      return result.rows;
    } finally {
      client.release();
    }
  }
};

// Create parser instance
const tusk = new TuskLangEnhanced({
  debug: process.env.NODE_ENV === 'development',
  strict: true
});

tusk.setDatabaseAdapter(pgAdapter);

// Parse configuration
const config = await tusk.parse(`
  name: "MyApp"
  version: "1.0.0"
  
  [database]
  user_count: @query("SELECT COUNT(*) FROM users")
  active_users: @query("SELECT COUNT(*) FROM users WHERE status = 'active'")
  
  [server]
  port: @env("PORT", "8080")
  workers: @env("NODE_ENV") == "production" ? 8 : 2
`);

console.log('App name:', config.name);
console.log('User count:', config.database.user_count);
console.log('Active users:', config.database.active_users);
console.log('Server port:', config.server.port);
console.log('Workers:', config.server.workers);
```

---

**For more information, visit [TuskLang Documentation](https://tuskt.sk/docs)** 