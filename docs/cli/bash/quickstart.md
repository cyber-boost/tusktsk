# Quick Start Guide

Get up and running with the TuskLang Bash CLI in minutes.

## Prerequisites

- TuskLang Bash CLI installed (see [Installation Guide](./installation.md))
- Basic familiarity with command line interfaces
- A text editor for creating configuration files

## Your First TuskLang Project

### 1. Create a Project Directory

```bash
# Create and navigate to project directory
mkdir my-tusk-project
cd my-tusk-project
```

### 2. Initialize Configuration

Create your first configuration file:

```bash
# Create a basic configuration
cat > peanu.peanuts << 'EOF'
[app]
name: "My First TuskLang App"
version: "1.0.0"

[server]
host: "localhost"
port: 3000

[database]
type: "sqlite"
path: "./data/app.db"
EOF
```

### 3. Test the CLI

```bash
# Check if CLI is working
tsk version

# Show help
tsk help

# Validate your configuration
tsk config validate
```

Expected output:
```
✅ Configuration is valid
```

### 4. Access Configuration Values

```bash
# Get specific values
tsk config get app.name
tsk config get server.port
tsk config get database.path

# Get with default values
tsk config get server.host "127.0.0.1"
```

### 5. Compile to Binary Format

```bash
# Compile for better performance
tsk peanuts compile peanu.peanuts

# This creates peanu.pnt (85% faster loading)
ls -la *.pnt
```

## Basic Commands

### Configuration Management

```bash
# Get configuration value
tsk config get server.port

# Check configuration hierarchy
tsk config check

# Validate configuration syntax
tsk config validate

# Show configuration statistics
tsk config stats
```

### Development Commands

```bash
# Start development server
tsk serve 3000

# Compile TuskLang file
tsk compile config.tsk

# Optimize configuration
tsk optimize config.tsk
```

### Testing

```bash
# Run all tests
tsk test all

# Test specific components
tsk test parser
tsk test fujsen
tsk test sdk

# Performance testing
tsk test performance
```

### Database Operations

```bash
# Check database status
tsk db status

# Initialize database
tsk db init

# Run migration
tsk db migrate schema.sql

# Backup database
tsk db backup
```

## Working with Peanut Configuration

### Understanding File Types

TuskLang supports three configuration formats:

1. **`.peanuts`** - Human-readable (INI-style)
2. **`.tsk`** - TuskLang syntax (advanced features)
3. **`.pnt`** - Binary format (85% faster)

### Creating a Configuration

```bash
# Create a simple configuration
cat > config.peanuts << 'EOF'
[app]
name: "My App"
version: "1.0.0"

[server]
host: "localhost"
port: 8080

[features]
debug: true
logging: "info"
EOF
```

### Loading Configuration

```bash
# Load and display configuration
tsk peanuts load config.peanuts

# Compile to binary
tsk peanuts compile config.peanuts

# Load binary configuration
tsk peanuts load config.pnt
```

### Hierarchical Configuration

Create a project structure with inherited configurations:

```bash
# Root configuration
cat > peanu.peanuts << 'EOF'
[app]
name: "My Project"
version: "1.0.0"

[common]
environment: "development"
log_level: "info"
EOF

# API-specific configuration
mkdir api
cat > api/peanu.peanuts << 'EOF'
[api]
version: "v1"
rate_limit: 1000

[server]
port: 3001
EOF

# Web-specific configuration
mkdir web
cat > web/peanu.peanuts << 'EOF'
[web]
static_dir: "./public"

[server]
port: 3002
EOF
```

Test hierarchical loading:

```bash
# Check hierarchy
tsk config check

# Get values from different contexts
tsk config get app.name          # From root
tsk config get api.version       # From api directory
tsk config get web.static_dir    # From web directory
```

## Common Workflows

### Development Workflow

```bash
# 1. Start development server
tsk serve 3000

# 2. In another terminal, run tests
tsk test all

# 3. Check configuration
tsk config validate

# 4. Compile for production
tsk peanuts compile peanu.peanuts
```

### Production Deployment

```bash
# 1. Validate configuration
tsk config validate

# 2. Compile to binary
tsk peanuts compile peanu.peanuts

# 3. Check database status
tsk db status

# 4. Run migrations
tsk db migrate production.sql

# 5. Start services
tsk services start
```

### Testing Workflow

```bash
# 1. Run all tests
tsk test all

# 2. Run specific test suites
tsk test parser
tsk test fujsen

# 3. Performance testing
tsk test performance

# 4. Generate test report
tsk test --json > test-report.json
```

## Advanced Features

### JSON Output

Use `--json` flag for machine-readable output:

```bash
# JSON output for automation
tsk config get server.port --json
tsk db status --json
tsk test all --json
```

### Verbose Mode

Enable detailed output with `--verbose`:

```bash
# Verbose output
tsk config validate --verbose
tsk db migrate schema.sql --verbose
tsk test all --verbose
```

### Quiet Mode

Suppress non-error output with `--quiet`:

```bash
# Quiet output for scripts
tsk config get server.port --quiet
tsk db status --quiet
```

## Integration with Shell Scripts

### Loading Configuration in Scripts

```bash
#!/bin/bash
# my-script.sh

# Load configuration
APP_NAME=$(tsk config get app.name --quiet)
SERVER_PORT=$(tsk config get server.port --quiet)

echo "Starting $APP_NAME on port $SERVER_PORT"

# Use configuration values
python3 -m http.server "$SERVER_PORT"
```

### Environment Variables

```bash
#!/bin/bash
# export-config.sh

# Export all configuration as environment variables
while IFS='=' read -r key value; do
    [[ -z "$key" || "$key" =~ ^# ]] && continue
    export "$key"="$value"
done < <(tsk peanuts load peanu.pnt)

echo "Configuration exported to environment variables"
```

## Troubleshooting

### Common Issues

#### Configuration Not Found

```bash
# Check if configuration file exists
ls -la *.peanuts *.tsk *.pnt

# Check current directory
pwd

# Use absolute path
tsk config get app.name /path/to/project
```

#### Permission Errors

```bash
# Fix permissions
chmod +x cli/main.sh

# Check file ownership
ls -la cli/main.sh
```

#### Database Connection Issues

```bash
# Check database status
tsk db status

# Initialize database if needed
tsk db init

# Check database file
ls -la *.db
```

### Getting Help

```bash
# General help
tsk help

# Command-specific help
tsk config --help
tsk db --help
tsk test --help

# Verbose help
tsk help --verbose
```

## Next Steps

Now that you're familiar with the basics:

1. **Explore [Command Reference](./commands/README.md)** for detailed command documentation
2. **Try [Examples](./examples/README.md)** for real-world use cases
3. **Read [Troubleshooting](./troubleshooting.md)** for common issues and solutions
4. **Check [PNT Guide](../bash/docs/PNT_GUIDE.md)** for advanced configuration features

## Support

- **Documentation**: [TuskLang Docs](https://tusklang.org/docs)
- **GitHub**: [TuskLang Repository](https://github.com/tusklang/tusklang-bash)
- **Community**: [TuskLang Discord](https://discord.gg/tusklang) 