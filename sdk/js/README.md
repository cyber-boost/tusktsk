# TuskLang JavaScript SDK

**TuskLang - The Freedom Configuration Language**

Query databases, use any syntax, never bow to any king!

## 🚀 Quick Start

```bash
# Install globally
npm install -g tusklang

# Use the CLI
tsk --help

# Interactive mode
tsk
```

## 📦 Installation

```bash
npm install tusklang
```

## 🛠️ CLI Commands

The TuskLang CLI provides a comprehensive set of commands for managing configurations, databases, and development workflows.

### 🗄️ Database Commands

```bash
# Check database connection status
tsk db status

# Run migration file
tsk db migrate schema.sql

# Open interactive database console
tsk db console

# Backup database
tsk db backup [file]

# Restore from backup
tsk db restore backup.sql

# Initialize SQLite database
tsk db init
```

### 🔧 Development Commands

```bash
# Start development server
tsk serve [port]

# Compile TSK file
tsk compile config.tsk

# Optimize TSK file for production
tsk optimize config.tsk
```

### 🧪 Testing Commands

```bash
# Run all tests
tsk test all

# Run specific test suite
tsk test parser
tsk test fujsen
tsk test sdk
tsk test performance
```

### ⚙️ Service Commands

```bash
# Start all services
tsk services start

# Stop all services
tsk services stop

# Restart services
tsk services restart

# Show service status
tsk services status
```

### 📦 Cache Commands

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

### 🥜 Configuration Commands

```bash
# Get configuration value
tsk config get server.port

# Check configuration hierarchy
tsk config check

# Validate configuration
tsk config validate

# Auto-compile configuration files
tsk config compile

# Generate configuration documentation
tsk config docs

# Clear configuration cache
tsk config clear-cache

# Show configuration statistics
tsk config stats
```

### 🚀 Binary Performance Commands

```bash
# Compile to binary format
tsk binary compile config.tsk

# Execute binary file
tsk binary execute config.tskb

# Benchmark performance
tsk binary benchmark config.tsk

# Optimize binary for production
tsk binary optimize config.tskb
```

### 🤖 AI Commands

```bash
# Query Claude AI
tsk ai claude "Explain TuskLang syntax"

# Query ChatGPT
tsk ai chatgpt "JavaScript best practices"

# Analyze code with AI
tsk ai analyze app.js --focus security

# Get optimization suggestions
tsk ai optimize app.js --type performance

# Security scan with AI
tsk ai security app.js --level thorough
```

### 🛠️ Utility Commands

```bash
# Parse TSK file
tsk parse config.tsk

# Validate TSK file syntax
tsk validate config.tsk

# Convert between formats
tsk convert -i config.tsk -o config.json

# Get specific value by key path
tsk get config.tsk server.port

# Set value by key path
tsk set config.tsk server.port 8080
```

## 📝 Configuration Syntax

TuskLang supports multiple syntax styles for maximum flexibility:

### Bracket Style
```tsk
[database]
host: "localhost"
port: 5432

[server]
host: "0.0.0.0"
port: 8080
```

### Brace Style
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

### Angle Bracket Style
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

### Mixed Style
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

## 🔧 Advanced Features

### Variable References
```tsk
$app_name: "MyApp"
$port: 3000

name: $app_name
server_port: $port
```

### Database Queries
```tsk
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE status = 'active'")
```

### Date Functions
```tsk
timestamp: @date("Y-m-d H:i:s")
year: @date("Y")
month: @date("m")
```

### Conditional Expressions
```tsk
$env: "production"
debug: $env == "development" ? true : false
workers: $env == "production" ? 8 : 2
```

### Ranges
```tsk
port_range: 8000-9000
worker_range: 1-10
```

### Environment Variables
```tsk
node_env: @env("NODE_ENV", "development")
api_key: @env("API_KEY", "default_key")
```

### String Concatenation
```tsk
$base: "TuskLang"
$version: "2.0"
full_name: $base + " v" + $version
```

## 🥜 Peanut Binary Format

TuskLang supports a high-performance binary format (.pnt) that provides ~85% performance improvement over text parsing:

```bash
# Compile to binary
tsk binary compile config.tsk

# Execute binary directly
tsk binary execute config.tskb

# Benchmark performance
tsk binary benchmark config.tsk
```

## 🔌 Database Adapters

TuskLang supports multiple database adapters:

- **SQLite**: Built-in, no additional dependencies
- **PostgreSQL**: Requires `pg` package
- **MySQL**: Requires `mysql2` package
- **MongoDB**: Requires `mongodb` package
- **Redis**: Requires `redis` package

## 🧪 Testing

```bash
# Run all tests
npm test

# Run specific test suites
tsk test parser
tsk test fujsen
tsk test sdk
tsk test performance
```

## 📚 Examples

### Basic Configuration
```tsk
name: "MyApp"
version: "1.0.0"
debug: true

[database]
type: "postgres"
host: "localhost"
port: 5432
database: "myapp"
username: "postgres"
password: @env("DB_PASSWORD")

[server]
host: "0.0.0.0"
port: 8080
ssl: {
  enabled: true
  cert: "/path/to/cert"
}

[cache]
driver: "redis"
host: "localhost"
port: 6379
ttl: 300
```

### Advanced Configuration with FUJSEN
```tsk
$app_name: "AdvancedApp"
$env: @env("NODE_ENV", "development")

name: $app_name
version: "2.0.0"
debug: $env == "development" ? true : false

[database]
type: "postgres"
host: "localhost"
port: 5432
database: $app_name + "_" + $env
connection_limit: $env == "production" ? 20 : 5

[server]
host: "0.0.0.0"
port: @env("PORT", "8080")
workers: $env == "production" ? 8 : 2

[monitoring]
enabled: true
metrics: {
  user_count: @query("SELECT COUNT(*) FROM users")
  active_sessions: @query("SELECT COUNT(*) FROM sessions WHERE expires_at > NOW()")
  uptime: @date("U")
}
```

## 🔧 API Usage

```javascript
const TuskLang = require('tusklang');

// Create parser instance
const tusk = new TuskLang();

// Parse configuration
const config = tusk.parse(`
  name: "MyApp"
  version: "1.0.0"
  
  [database]
  host: "localhost"
  port: 5432
`);

// Access configuration values
console.log(config.name); // "MyApp"
console.log(config.database.host); // "localhost"
console.log(config.database.port); // 5432

// Stringify configuration
const tskString = TuskLang.stringify(config);
```

## 📖 Documentation

For more detailed documentation, visit:
- [TuskLang Official Documentation](https://tusklang.org/docs)
- [CLI Reference](https://tusklang.org/docs/cli)
- [Configuration Guide](https://tusklang.org/docs/configuration)
- [Database Integration](https://tusklang.org/docs/database)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.

## 🆘 Support

- **Documentation**: [https://tusklang.org/docs](https://tusklang.org/docs)
- **Issues**: [GitHub Issues](https://github.com/tuskphp/tusklang-js/issues)
- **Discussions**: [GitHub Discussions](https://github.com/tuskphp/tusklang-js/discussions)

---

**Strong. Secure. Scalable.** 🐘