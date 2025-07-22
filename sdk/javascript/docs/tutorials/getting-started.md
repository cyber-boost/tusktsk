# Getting Started with TuskLang JavaScript SDK

**Complete beginner's guide to TuskLang JavaScript SDK**

## Table of Contents

- [What is TuskLang?](#what-is-tusklang)
- [Installation](#installation)
- [Your First Configuration](#your-first-configuration)
- [Basic Syntax](#basic-syntax)
- [Variables and References](#variables-and-references)
- [Database Integration](#database-integration)
- [CLI Usage](#cli-usage)
- [Next Steps](#next-steps)

## What is TuskLang?

TuskLang is a freedom configuration language that allows you to:

- **Use any syntax style** - brackets `[]`, braces `{}`, or angle brackets `><`
- **Query databases directly** in configuration files
- **Reference environment variables** and system values
- **Use conditional logic** and expressions
- **Compile to binary** for 85% performance improvement
- **Mix and match** different syntax styles in the same file

## Installation

### Prerequisites

- Node.js version 12.0 or higher
- npm or yarn package manager

### Install TuskLang

```bash
# Install globally for CLI access
npm install -g tusklang

# Or install locally in your project
npm install tusklang
```

### Verify Installation

```bash
# Check if CLI is available
tsk --version

# Should output: TuskLang v2.0.2
```

## Your First Configuration

### 1. Create a Basic Configuration File

Create a file named `config.tsk`:

```tsk
# My first TuskLang configuration
name: "MyApp"
version: "1.0.0"

[server]
host: "localhost"
port: 8080

[database]
host: "localhost"
port: 5432
database: "myapp"
```

### 2. Parse the Configuration

Create a file named `app.js`:

```javascript
const TuskLang = require('tusklang');

// Create parser instance
const tusk = new TuskLang();

// Parse configuration file
const config = tusk.parseFile('./config.tsk');

// Access configuration values
console.log('App name:', config.name);
console.log('Server port:', config.server.port);
console.log('Database:', config.database.database);
```

### 3. Run Your Application

```bash
node app.js
```

**Output:**
```
App name: MyApp
Server port: 8080
Database: myapp
```

## Basic Syntax

### Multiple Syntax Styles

TuskLang supports three syntax styles. You can use any of them:

#### Bracket Style (TOML-like)

```tsk
[server]
host: "localhost"
port: 8080

[database]
host: "localhost"
port: 5432
```

#### Brace Style (JSON-like)

```tsk
server {
  host: "localhost"
  port: 8080
}

database {
  host: "localhost"
  port: 5432
}
```

#### Angle Bracket Style

```tsk
server >
  host: "localhost"
  port: 8080
<

database >
  host: "localhost"
  port: 5432
<
```

#### Mixed Style (Freedom!)

```tsk
[server]
host: "localhost"

database {
  port: 5432
}

cache >
  ttl: 300
<
```

### Data Types

TuskLang supports all common data types:

```tsk
# Strings
name: "MyApp"
version: '1.0.0'

# Numbers
port: 8080
timeout: 30.5

# Booleans
debug: true
ssl: false

# Arrays
features: ["auth", "api", "websocket"]
ports: [8080, 8081, 8082]

# Objects
ssl: {
  enabled: true
  cert: "/path/to/cert"
  key: "/path/to/key"
}
```

## Variables and References

### Global Variables

Define variables with `$` prefix:

```tsk
# Define global variables
$app_name: "MyApp"
$port: 8080
$env: "development"

# Use variables
name: $app_name
server_port: $port
debug: $env == "development" ? true : false
```

### Environment Variables

Reference environment variables with `@env()`:

```tsk
# Get environment variable with default
node_env: @env("NODE_ENV", "development")
api_key: @env("API_KEY", "default_key")

# Required environment variable
database_url: @env("DATABASE_URL")
```

### Date Functions

Use `@date()` for current date/time:

```tsk
# Current timestamp
timestamp: @date("Y-m-d H:i:s")

# Current year
year: @date("Y")

# Unix timestamp
unix_time: @date("U")

# ISO date
iso_date: @date("c")
```

### Conditional Expressions

Use ternary operators for conditional values:

```tsk
$env: "production"

[server]
debug: $env == "development" ? true : false
workers: $env == "production" ? 8 : 2
log_level: $env == "production" ? "error" : "debug"
```

## Database Integration

### Basic Database Queries

TuskLang can execute database queries directly in configuration:

```tsk
[database]
# Count users
user_count: @query("SELECT COUNT(*) FROM users")

# Get active users
active_users: @query("SELECT COUNT(*) FROM users WHERE status = 'active'")

# Get specific user
admin_user: @query("SELECT * FROM users WHERE role = 'admin' LIMIT 1")
```

### Setting Up Database Adapter

To use database queries, you need to set up a database adapter:

```javascript
const TuskLangEnhanced = require('./tsk-enhanced.js');
const { Pool } = require('pg');

// Create PostgreSQL connection pool
const pool = new Pool({
  host: 'localhost',
  port: 5432,
  database: 'myapp',
  user: 'postgres',
  password: 'password'
});

// Create database adapter
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

// Create parser with database adapter
const tusk = new TuskLangEnhanced();
tusk.setDatabaseAdapter(pgAdapter);

// Parse configuration with database queries
const config = await tusk.parse(`
  [database]
  user_count: @query("SELECT COUNT(*) FROM users")
  active_users: @query("SELECT COUNT(*) FROM users WHERE status = 'active'")
`);

console.log('User count:', config.database.user_count);
console.log('Active users:', config.database.active_users);
```

### Supported Databases

- **SQLite**: Built-in, no additional dependencies
- **PostgreSQL**: Requires `pg` package
- **MySQL**: Requires `mysql2` package
- **MongoDB**: Requires `mongodb` package
- **Redis**: Requires `redis` package

## CLI Usage

### Basic CLI Commands

```bash
# Parse a configuration file
tsk parse config.tsk

# Validate configuration syntax
tsk validate config.tsk

# Get a specific value
tsk get config.tsk server.port

# Set a value
tsk set config.tsk server.port 8080

# Convert to JSON
tsk convert -i config.tsk -o config.json
```

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
```

### Development Commands

```bash
# Start development server
tsk serve 8080

# Compile configuration
tsk compile config.tsk

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
```

## Complete Example

### Configuration File (`config.tsk`)

```tsk
# Global variables
$app_name: "MyApp"
$env: @env("NODE_ENV", "development")

# Application configuration
name: $app_name
version: "1.0.0"
debug: $env == "development" ? true : false

# Server configuration
[server]
host: "0.0.0.0"
port: @env("PORT", "8080")
workers: $env == "production" ? 8 : 2

# Database configuration
[database]
type: "postgres"
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", "5432")
database: $app_name + "_" + $env
username: @env("DB_USER", "postgres")
password: @env("DB_PASSWORD")

# Cache configuration
[cache]
driver: "redis"
host: @env("REDIS_HOST", "localhost")
port: @env("REDIS_PORT", "6379")
ttl: 300

# Monitoring (with database queries)
[monitoring]
enabled: true
metrics: {
  user_count: @query("SELECT COUNT(*) FROM users")
  active_sessions: @query("SELECT COUNT(*) FROM sessions WHERE expires_at > NOW()")
  uptime: @date("U")
}
```

### Application Code (`app.js`)

```javascript
const TuskLangEnhanced = require('./tsk-enhanced.js');
const { Pool } = require('pg');

async function main() {
  // Set up database adapter
  const pool = new Pool({
    host: process.env.DB_HOST || 'localhost',
    port: process.env.DB_PORT || 5432,
    database: process.env.DB_NAME || 'myapp',
    user: process.env.DB_USER || 'postgres',
    password: process.env.DB_PASSWORD
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

  // Create parser
  const tusk = new TuskLangEnhanced({
    debug: process.env.NODE_ENV === 'development'
  });

  tusk.setDatabaseAdapter(pgAdapter);

  // Parse configuration
  const config = await tusk.parseFile('./config.tsk');

  // Use configuration
  console.log('Starting application...');
  console.log('App name:', config.name);
  console.log('Version:', config.version);
  console.log('Debug mode:', config.debug);
  console.log('Server port:', config.server.port);
  console.log('Database:', config.database.database);
  console.log('Cache TTL:', config.cache.ttl);
  
  if (config.monitoring.enabled) {
    console.log('User count:', config.monitoring.metrics.user_count);
    console.log('Active sessions:', config.monitoring.metrics.active_sessions);
    console.log('Uptime:', config.monitoring.metrics.uptime);
  }

  // Close database connection
  await pool.end();
}

main().catch(console.error);
```

### Environment Variables (`.env`)

```bash
NODE_ENV=development
PORT=8080
DB_HOST=localhost
DB_PORT=5432
DB_NAME=myapp
DB_USER=postgres
DB_PASSWORD=password
REDIS_HOST=localhost
REDIS_PORT=6379
```

### Run the Application

```bash
# Install dependencies
npm install pg

# Run the application
node app.js
```

## Next Steps

Now that you have the basics, explore these advanced topics:

1. **[Advanced Syntax Tutorial](./advanced-syntax.md)** - Learn about complex expressions and patterns
2. **[Database Integration Tutorial](./database-integration.md)** - Deep dive into database features
3. **[CLI Mastery Tutorial](./cli-mastery.md)** - Master all CLI commands
4. **[Performance Optimization Tutorial](./performance-optimization.md)** - Learn about binary compilation
5. **[Best Practices Guide](../best-practices.md)** - Production-ready patterns and practices

### Additional Resources

- **[API Reference](../api-reference.md)** - Complete API documentation
- **[Examples Directory](../examples/)** - Real-world configuration examples
- **[Troubleshooting Guide](../troubleshooting.md)** - Common issues and solutions
- **[Migration Guide](../migration-guide.md)** - Migrating from other configuration formats

---

**Ready to build something amazing? Start with the [Advanced Syntax Tutorial](./advanced-syntax.md)!** 