# Quick Start Guide

Get up and running with TuskLang Rust CLI in minutes.

## Prerequisites

- TuskLang Rust CLI installed (see [Installation Guide](./installation.md))
- Basic familiarity with command line
- A text editor

## Your First Configuration

### Step 1: Create a Configuration File

Create your first TuskLang configuration file:

```bash
# Create a simple configuration
cat > peanu.peanuts << 'EOF'
[app]
name: "My First Rust App"
version: "1.0.0"
debug: true

[server]
host: "localhost"
port: 8080
timeout: 30

[database]
url: "postgresql://localhost/myapp"
pool_size: 10
EOF
```

### Step 2: Validate Your Configuration

```bash
# Validate the syntax
tusk-rust utility validate peanu.peanuts

# Parse and display the contents
tusk-rust utility parse peanu.peanuts
```

Expected output:
```
✅ File 'peanu.peanuts' is valid TuskLang syntax
✅ Successfully parsed 'peanu.peanuts'
📄 File contents:
[app]
name: "My First Rust App"
version: "1.0.0"
debug: true

[server]
host: "localhost"
port: 8080
timeout: 30

[database]
url: "postgresql://localhost/myapp"
pool_size: 10
```

### Step 3: Access Configuration Values

```bash
# Get specific values
tusk-rust config get app.name
tusk-rust config get server.port
tusk-rust config get database.url

# Get nested values
tusk-rust config get server.host
```

Expected output:
```
✅ "My First Rust App"
✅ 8080
✅ "postgresql://localhost/myapp"
✅ "localhost"
```

## Basic Commands

### Configuration Management

```bash
# Parse a configuration file
tusk-rust utility parse config.tsk

# Validate configuration syntax
tusk-rust utility validate config.tsk

# Get configuration values
tusk-rust config get server.port
tusk-rust config get app.name

# Check configuration hierarchy
tusk-rust config check
```

### Development Workflow

```bash
# Compile configuration to optimized format
tusk-rust dev compile peanu.peanuts

# Start development server
tusk-rust dev serve 3000

# Run tests
tusk-rust test parser
tusk-rust test all
```

### Database Operations

```bash
# Check database status
tusk-rust db status

# Run database migrations
tusk-rust db migrate schema.sql

# Backup database
tusk-rust db backup
```

### Service Management

```bash
# Check service status
tusk-rust services status

# Start all services
tusk-rust services start

# Stop all services
tusk-rust services stop
```

## Interactive Mode

Start the interactive CLI:

```bash
tusk-rust
```

You'll see:
```
TuskLang v0.1.0 - Interactive Mode
Type 'help' for commands, 'exit' to quit
tsk>
```

Try these commands:
```bash
tsk> help
tsk> db status
tsk> config get app.name
tsk> exit
```

## Advanced Configuration

### Using TuskLang Syntax

Create a more advanced configuration with TuskLang syntax:

```bash
cat > peanu.tsk << 'EOF'
# Application configuration
app_name: "Advanced Rust App"
version: "2.0.0"
environment: "${ENV:-development}"

server:
  host: "0.0.0.0"
  port: 8080
  timeout: 30s
  cors:
    origins: ["http://localhost:3000", "https://app.example.com"]
    methods: ["GET", "POST", "PUT", "DELETE"]

database:
  url: "${DATABASE_URL:-postgresql://localhost/myapp}"
  pool_size: 20
  max_connections: 100
  timeout: 5s
  ssl_mode: "require"

redis:
  url: "${REDIS_URL:-redis://localhost:6379}"
  pool_size: 10
  timeout: 1s

logging:
  level: "${LOG_LEVEL:-info}"
  format: "json"
  file: "logs/app.log"
  rotation: "daily"

features:
  - logging
  - metrics
  - caching
  - authentication
EOF
```

### Binary Compilation

Compile your configuration to binary format for better performance:

```bash
# Compile to binary format
tusk-rust binary compile peanu.tsk

# This creates peanu.tskb (binary format)
# Binary format loads 85% faster than text format
```

### Environment Variables

Use environment variables in your configuration:

```bash
# Set environment variables
export ENV=production
export DATABASE_URL=postgresql://prod-server/myapp
export LOG_LEVEL=warn

# Load configuration (will use environment variables)
tusk-rust config get app_name
tusk-rust config get database.url
```

## Common Patterns

### Configuration Validation

```bash
# Validate before deployment
tusk-rust utility validate peanu.tsk

# Check for required fields
tusk-rust config get app_name || echo "Missing app_name"
tusk-rust config get server.port || echo "Missing server.port"
```

### Configuration Comparison

```bash
# Compare configurations
tusk-rust utility parse dev.tsk > dev.json
tusk-rust utility parse prod.tsk > prod.json
diff dev.json prod.json
```

### Script Integration

```bash
#!/bin/bash
# deployment.sh

# Validate configuration
if ! tusk-rust utility validate peanu.tsk; then
    echo "Configuration validation failed"
    exit 1
fi

# Get deployment values
APP_NAME=$(tusk-rust config get app_name)
PORT=$(tusk-rust config get server.port)
ENV=$(tusk-rust config get environment)

echo "Deploying $APP_NAME on port $PORT in $ENV environment"

# Continue with deployment...
```

### CI/CD Integration

```yaml
# .github/workflows/deploy.yml
name: Deploy

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup Rust
        uses: actions-rs/toolchain@v1
        with:
          toolchain: stable
      
      - name: Install TuskLang CLI
        run: cargo install tusklang-rust
      
      - name: Validate Configuration
        run: tusk-rust utility validate peanu.tsk
      
      - name: Get Configuration Values
        run: |
          APP_NAME=$(tusk-rust config get app_name)
          PORT=$(tusk-rust config get server.port)
          echo "APP_NAME=$APP_NAME" >> $GITHUB_ENV
          echo "PORT=$PORT" >> $GITHUB_ENV
      
      - name: Deploy
        run: |
          echo "Deploying $APP_NAME on port $PORT"
          # Your deployment logic here
```

## Troubleshooting

### Common Issues

#### Configuration Not Found

```bash
# Check if configuration file exists
ls -la peanu.*

# Create default configuration if missing
echo '[app]
name: "Default App"
version: "1.0.0"' > peanu.peanuts
```

#### Parse Errors

```bash
# Check syntax errors
tusk-rust utility validate peanu.tsk

# Common fixes:
# - Check for missing colons (:)
# - Ensure proper indentation
# - Verify quotes around strings
```

#### Permission Errors

```bash
# Fix file permissions
chmod 644 peanu.tsk
chmod 644 peanu.peanuts

# Check directory permissions
ls -la .
```

## Next Steps

Now that you're familiar with the basics:

1. **Explore [Command Reference](./commands/README.md)** for all available commands
2. **Check [Examples](./examples/README.md)** for advanced usage patterns
3. **Read [Peanut Configuration Guide](../rust/docs/PNT_GUIDE.md)** for detailed configuration management
4. **Review [Troubleshooting](./troubleshooting.md)** for common issues and solutions

## Getting Help

```bash
# Get help for any command
tusk-rust --help
tusk-rust config --help
tusk-rust utility --help

# Interactive help
tusk-rust
tsk> help
```

## Support Resources

- **Documentation**: [https://tusklang.org/docs/rust](https://tusklang.org/docs/rust)
- **GitHub Issues**: [https://github.com/tusklang/tusklang-rust/issues](https://github.com/tusklang/tusklang-rust/issues)
- **Discord Community**: [https://discord.gg/tusklang](https://discord.gg/tusklang)
- **Email Support**: support@tusklang.org 