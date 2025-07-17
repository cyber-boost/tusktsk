# TuskLang [LANGUAGE] CLI Usage Guide

## Installation

```bash
# Using curl
curl -sSL https://init.tusklang.org/[language] | bash

# Using wget
wget -qO- https://init.tusklang.org/[language] | bash

# Manual installation
tar -xzf tusklang-[language]-2.0.0.tar.gz
cd tusklang-[language]-2.0.0
./install.sh
```

## Basic Usage

```bash
# Show help
tsk --help

# Show version
tsk --version

# Parse a TSK file
tsk parse config.tsk

# Get a specific value
tsk get config.tsk server.port
```

## Database Commands

### Status and Connection
```bash
# Check database connection
tsk db status

# Initialize SQLite database
tsk db init

# Open interactive console
tsk db console
```

### Migrations and Backups
```bash
# Run migration
tsk db migrate schema.sql

# Backup database
tsk db backup
tsk db backup mybackup.sql

# Restore from backup
tsk db restore backup.sql
```

## Development Commands

### Development Server
```bash
# Start on default port (8080)
tsk serve

# Start on custom port
tsk serve 3000

# With hot reload
tsk serve --hot-reload
```

### Compilation and Optimization
```bash
# Compile TSK file
tsk compile app.tsk

# Optimize for production
tsk optimize app.tsk

# Compile to binary format
tsk binary compile app.tsk
```

## Testing Commands

```bash
# Run all tests
tsk test all

# Run specific test suite
tsk test parser
tsk test fujsen
tsk test sdk

# Performance benchmarks
tsk test performance

# With verbose output
tsk test all --verbose
```

## Configuration Commands

### Peanut Configuration
```bash
# Get configuration value
tsk config get server.port

# Check configuration hierarchy
tsk config check

# Validate configuration
tsk config validate

# Compile all peanut files
tsk config compile

# Show performance stats
tsk config stats
```

### Binary Peanut Files
```bash
# Compile .peanuts to .pntb
tsk peanuts compile config.peanuts

# Auto-compile directory
tsk peanuts auto-compile ./config/

# Load binary config
tsk peanuts load config.pntb
```

## Cache Commands

```bash
# Clear all caches
tsk cache clear

# Show cache status
tsk cache status

# Warm cache
tsk cache warm

# Memcached operations
tsk cache memcached status
tsk cache memcached flush
tsk cache memcached stats
```

## AI Integration Commands

```bash
# Query AI assistants
tsk ai claude "How do I optimize my TSK config?"
tsk ai chatgpt "Explain FUJSEN operators"

# Code analysis
tsk ai analyze config.tsk
tsk ai optimize app.tsk
tsk ai security scan.tsk

# Setup AI credentials
tsk ai setup
```

## Service Management

```bash
# Start all services
tsk services start

# Stop services
tsk services stop

# Restart services
tsk services restart

# Check status
tsk services status
```

## Format Conversion

```bash
# Convert between formats
tsk convert -i config.json -o config.tsk
tsk convert -i config.yaml -o config.tsk
tsk convert -i config.tsk -o config.json

# Supported formats: json, yaml, ini, env, tsk
```

## Advanced Features

### Project Initialization
```bash
# Create new project
tsk init myproject

# With template
tsk init myproject --template=web

# Migrate from other format
tsk migrate --from=json
```

### CSS Shortcodes
```bash
# Expand CSS shortcodes
tsk css expand styles.css
tsk css expand input.css output.css

# Show shortcode mappings
tsk css map
```

### License Management
```bash
# Check license status
tsk license check

# Activate license
tsk license activate YOUR-LICENSE-KEY
```

## Environment Variables

- `TUSKLANG_HOME` - TuskLang installation directory
- `TUSKLANG_CONFIG` - Default config file path
- `TUSKLANG_CACHE` - Cache directory
- `TUSKLANG_DEBUG` - Enable debug output

## Configuration Files

TuskLang looks for configuration in this order:
1. Command-line specified (`--config`)
2. `./peanu.pntb` or `./peanu.tsk`
3. Parent directories (walking up)
4. `~/.tusklang/config.tsk`
5. `/etc/tusklang/config.tsk`

## Common Workflows

### Setting Up a New Project
```bash
# Initialize project
tsk init myapp

# Create configuration
cat > peanu.peanuts << EOF
[app]
name: "myapp"
version: "1.0.0"

[server]
port: 8080
host: "localhost"
EOF

# Compile to binary
tsk peanuts compile peanu.peanuts

# Start development
tsk serve
```

### Running Tests Before Deployment
```bash
# Validate configuration
tsk config validate

# Run all tests
tsk test all

# Check performance
tsk test performance

# Optimize for production
tsk optimize app.tsk
tsk binary compile app.tsk
```

### Database Migration Workflow
```bash
# Check current status
tsk db status

# Create backup
tsk db backup pre-migration.sql

# Run migration
tsk db migrate migrations/v2.sql

# Verify
tsk db console
```

## Troubleshooting

### Command Not Found
```bash
# Add to PATH
export PATH="$PATH:/usr/local/bin"

# Or create alias
alias tsk="/path/to/tusklang/bin/tsk"
```

### Permission Errors
```bash
# Fix permissions
chmod +x /usr/local/bin/tsk

# Use sudo for system-wide install
sudo tsk cache clear
```

### Debug Mode
```bash
# Enable debug output
export TUSKLANG_DEBUG=1
tsk parse config.tsk

# Or use verbose flag
tsk --verbose parse config.tsk
```

## Getting Help

```bash
# General help
tsk help

# Command-specific help
tsk help db
tsk help config
tsk help test

# Online documentation
tsk help --online
```

## Version Information

```bash
# Show version
tsk version

# Show detailed info
tsk version --verbose

# Check for updates
tsk version --check-updates
```

---

For more information, visit [https://tusklang.org/docs](https://tusklang.org/docs)