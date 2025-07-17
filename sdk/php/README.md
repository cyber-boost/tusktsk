# 🥜 TuskLang Enhanced for PHP

**The Freedom Configuration Language - "We don't bow to any king"**

TuskLang Enhanced brings flexible syntax and intelligent configuration to PHP. With support for multiple grouping styles, global variables, cross-file communication, and database queries right in your config files.

## 🚀 Installation

### Via Composer (Recommended)
```bash
composer require cyber-boost/tusktsk
```

### Manual Installation
```bash
git clone https://github.com/cyber-boost/tusktsk
cd tusklang/sdk/php
composer install
```

## ✨ Features

- **Flexible Syntax**: Use `[]`, `{}`, or `<>` grouping - your choice!
- **Global Variables**: `$variables` accessible across all sections
- **Cross-File Communication**: `@file.tsk.get()` and `@file.tsk.set()`
- **Database Queries**: Query databases directly in config files
- **Environment Variables**: `@env("VAR", "default")` with defaults
- **Date Functions**: `@date("Y-m-d H:i:s")` with PHP formatting
- **Conditional Expressions**: `condition ? true_val : false_val`
- **Range Syntax**: `8000-9000` notation
- **peanut.tsk Integration**: Universal configuration file support

## 📖 Quick Start

### Basic Usage
```php
<?php
require_once 'vendor/autoload.php';

use TuskLang\Enhanced\TuskLangEnhanced;

// Parse a configuration file
$parser = new TuskLangEnhanced();
$config = $parser->parseFile('config.tsk');

// Get values
$dbHost = $parser->get('database.host');
$serverPort = $parser->get('server.port');

// Or use helper functions
$config = tsk_parse_file('config.tsk');
$parser = tsk_load_from_peanut(); // Load universal config
```

### Example Configuration
```tsk
# Global variables
$app_name: "My Application"
$environment: @env("APP_ENV", "development")

# Traditional sections
[database]
host: "localhost"
port: 5432

# Curly brace objects
server {
    host: @env("SERVER_HOST", "0.0.0.0")
    port: @env("SERVER_PORT", 8080)
    workers: $environment == "production" ? 4 : 1
}

# Angle bracket objects
cache >
    driver: "redis"
    ttl: "5m"
    enabled: true
<

# Database queries
[stats]
user_count: @query("SELECT COUNT(*) FROM users")
active_sessions: @query("SELECT COUNT(*) FROM sessions WHERE active = 1")

# Conditional expressions
[scaling]
instances: $environment == "production" ? 10 : 2
log_level: $environment == "production" ? "error" : "debug"

# Arrays and ranges
[config]
allowed_ips: ["127.0.0.1", "192.168.1.0/24"]
port_range: 8000-9000
```

## 🔧 CLI Tool

The package includes a command-line tool for working with TuskLang files:

```bash
# Parse a file
./vendor/bin/tusklang parse config.tsk

# Get a specific value
./vendor/bin/tusklang get config.tsk database.host

# List all keys
./vendor/bin/tusklang keys config.tsk

# Load from peanut.tsk
./vendor/bin/tusklang peanut

# Validate syntax
./vendor/bin/tusklang validate config.tsk
```

## 🥜 peanut.tsk - Universal Configuration

TuskLang Enhanced automatically looks for `peanut.tsk` in standard locations:
- `./peanut.tsk`
- `../peanut.tsk`
- `/etc/tusklang/peanut.tsk`
- `~/.config/tusklang/peanut.tsk`
- `$TUSKLANG_CONFIG` environment variable

This universal configuration file provides default settings for database connections, caching, and other common configuration needs.

## 💾 Database Integration

TuskLang Enhanced supports database queries directly in configuration files:

```php
// Setup database connection
$parser = new TuskLangEnhanced();
$parser->loadPeanut(); // Loads database config from peanut.tsk

// Use @query() in your .tsk files
$config = $parser->parseFile('app.tsk');
```

Supported databases:
- SQLite (via PDO)
- PostgreSQL (via PDO)
- MySQL/MariaDB (via PDO)

## 🌍 System Integration

TuskLang Enhanced can reference system-installed TuskLang tools:
- `/usr/local/bin/tusk` - System CLI
- `/usr/local/lib/tusklang` - System libraries
- `/usr/bin/tusk` - Alternative installation location

## 📚 Documentation

For complete documentation and examples:
- [Official Documentation](https://tuskt.sk)
- [GitHub Repository](https://github.com/cyber-boost/tusktsk)
- [Examples](examples/)

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## 📄 License

MIT License - see [LICENSE](LICENSE) for details.

---

**"We don't bow to any king"** - TuskLang gives developers the freedom to choose their preferred syntax while maintaining intelligent configuration capabilities.