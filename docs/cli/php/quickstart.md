# Quick Start Guide - PHP CLI

Get up and running with the TuskLang PHP CLI in minutes.

## Prerequisites

- PHP 8.1+ installed
- TuskLang PHP CLI installed (see [Installation](./installation.md))
- Basic familiarity with command line

## Your First Commands

### 1. Check Installation

```bash
# Verify CLI is working
tsk version

# Check system status
tsk status
```

Expected output:
```
TuskLang PHP CLI v2.0.0
System Status: OK
PHP Version: 8.2.0
Extensions: pdo, json, mbstring ✓
```

### 2. Create Your First Configuration

```bash
# Create a simple configuration file
cat > config.tsk << 'EOF'
[app]
name: "My First App"
version: "1.0.0"
debug: true

[server]
host: "localhost"
port: 8080
workers: 4

[database]
host: "db.example.com"
port: 3306
name: "myapp"
EOF
```

### 3. Parse and Validate

```bash
# Parse the configuration
tsk parse config.tsk

# Validate the configuration
tsk validate config.tsk

# Get specific values
tsk config get app.name
tsk config get server.port
```

### 4. Compile to Binary

```bash
# Compile to binary format for better performance
tsk config compile config.tsk

# This creates config.pnt
ls -la config.*
```

## Basic Workflows

### Configuration Management

```bash
# Create a new configuration
echo '[test] value: "hello"' > test.tsk

# Parse and validate
tsk parse test.tsk
tsk validate test.tsk

# Get values
tsk config get test.value

# Clean up
rm test.tsk
```

### Database Operations

```bash
# Check database status
tsk db status

# List available databases
tsk db list

# Connect to database console
tsk db console

# Run migrations
tsk db migrate
```

### Development Server

```bash
# Start development server
tsk serve 3000

# In another terminal, test the server
curl http://localhost:3000/status
```

### Testing

```bash
# Run all tests
tsk test all

# Run specific test categories
tsk test parser
tsk test sdk
tsk test performance
```

## Common Patterns

### Configuration Hierarchy

```bash
# Create hierarchical configuration
mkdir -p config/{dev,prod}

# Base configuration
cat > config/base.tsk << 'EOF'
[app]
name: "My App"
version: "1.0.0"

[server]
host: "0.0.0.0"
port: 8080
EOF

# Development overrides
cat > config/dev/peanu.tsk << 'EOF'
[app]
environment: "development"
debug: true

[server]
port: 3000
EOF

# Production overrides
cat > config/prod/peanu.tsk << 'EOF'
[app]
environment: "production"
debug: false

[server]
port: 80
workers: 8
EOF

# Load specific environment
tsk config load config/dev
tsk config get app.environment  # "development"
```

### Service Management

```bash
# Check service status
tsk services status

# Start services
tsk services start

# Stop services
tsk services stop

# Restart services
tsk services restart
```

### Cache Operations

```bash
# Check cache status
tsk cache status

# Clear cache
tsk cache clear

# Warm cache
tsk cache warm

# Memcached operations
tsk cache memcached status
tsk cache memcached stats
```

## PHP Integration Examples

### Using CLI in PHP Scripts

```php
<?php
// Execute CLI commands from PHP
function runTskCommand($command) {
    $output = [];
    $returnCode = 0;
    
    exec("tsk $command 2>&1", $output, $returnCode);
    
    return [
        'output' => $output,
        'code' => $returnCode
    ];
}

// Get configuration value
$result = runTskCommand('config get app.name');
if ($result['code'] === 0) {
    $appName = trim($result['output'][0]);
    echo "App name: $appName\n";
}

// Check database status
$result = runTskCommand('db status');
if ($result['code'] === 0) {
    echo "Database: OK\n";
} else {
    echo "Database: Error\n";
}
```

### Configuration Loading

```php
<?php
// Load configuration using CLI
function loadConfig($path = '.') {
    $result = runTskCommand("config load $path");
    
    if ($result['code'] === 0) {
        // Parse the output as JSON
        $jsonOutput = implode("\n", $result['output']);
        return json_decode($jsonOutput, true);
    }
    
    return null;
}

// Usage
$config = loadConfig('/path/to/config');
if ($config) {
    echo "Server: {$config['server']['host']}:{$config['server']['port']}\n";
}
```

### Testing Integration

```php
<?php
// Run tests from PHP
function runTests($category = 'all') {
    $result = runTskCommand("test $category");
    
    if ($result['code'] === 0) {
        echo "✅ Tests passed\n";
        return true;
    } else {
        echo "❌ Tests failed\n";
        foreach ($result['output'] as $line) {
            echo "  $line\n";
        }
        return false;
    }
}

// Usage
if (runTests('parser')) {
    echo "Parser tests successful\n";
}
```

## Advanced Features

### JSON Output

```bash
# Get JSON output for programmatic use
tsk config get app.name --json
tsk db status --json
tsk services list --json
```

### Verbose Mode

```bash
# Get detailed output
tsk config compile config.tsk --verbose
tsk db migrate --verbose
tsk test all --verbose
```

### Debug Mode

```bash
# Enable debug output
tsk --debug parse config.tsk
tsk --debug config get app.name
```

### Quiet Mode

```bash
# Suppress output (useful in scripts)
tsk config get app.name --quiet
tsk db status --quiet
```

## Next Steps

Now that you're familiar with the basics:

1. **Explore Commands** - [Command Reference](./commands/README.md)
2. **Try Examples** - [Examples](./examples/README.md)
3. **Learn Advanced Features** - [Advanced Usage](./commands/README.md)
4. **Configure Your Environment** - [Configuration Guide](./commands/config/README.md)

## Troubleshooting

If you encounter issues:

```bash
# Get help
tsk help

# Check system status
tsk status

# Enable debug mode
tsk --debug version

# Check logs
tail -f /var/log/tusklang/cli.log
```

See the [Troubleshooting Guide](./troubleshooting.md) for more detailed solutions.

**Strong. Secure. Scalable.** 🐘 