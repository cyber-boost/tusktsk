# Command Reference - PHP CLI

Complete reference for all TuskLang PHP CLI commands.

## Command Categories

| Category | Description | Commands |
|----------|-------------|----------|
| [Database](./db/README.md) | Database operations and management | 6 commands |
| [Development](./dev/README.md) | Development tools and utilities | 3 commands |
| [Testing](./test/README.md) | Test execution and validation | 5 commands |
| [Services](./services/README.md) | Service management and monitoring | 4 commands |
| [Cache](./cache/README.md) | Cache operations and optimization | 8 commands |
| [Configuration](./config/README.md) | Configuration management | 7 commands |
| [Binary](./binary/README.md) | Binary compilation and execution | 4 commands |
| [Peanuts](./peanuts/README.md) | Peanut configuration operations | 3 commands |
| [AI](./ai/README.md) | AI integrations and tools | 9 commands |
| [CSS](./css/README.md) | CSS utilities and processing | 2 commands |
| [License](./license/README.md) | License management | 2 commands |
| [Utility](./utility/README.md) | General utilities and helpers | 7 commands |

## Global Options

All commands support these global options:

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--help` | `-h` | Show help for this command | - |
| `--version` | `-v` | Show version information | - |
| `--json` | `-j` | Output in JSON format | false |
| `--verbose` | `-V` | Enable verbose output | false |
| `--quiet` | `-q` | Suppress output | false |
| `--debug` | `-d` | Enable debug mode | false |
| `--config` | `-c` | Specify configuration file | auto-detect |

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success |
| 1 | General error |
| 2 | Invalid arguments |
| 3 | Configuration error |
| 4 | Database error |
| 5 | Service error |
| 6 | Permission denied |
| 7 | File not found |
| 8 | Network error |
| 9 | Timeout |

## PHP-Specific Notes

### Autoloading

The CLI automatically handles PHP autoloading:

```bash
# Works with Composer autoloader
composer require tusklang/tusklang
vendor/bin/tsk version

# Works with manual includes
php -r "require 'src/PeanutConfig.php'; echo 'Loaded';"
```

### Error Handling

All commands use PHP exceptions with proper exit codes:

```php
<?php
// Example error handling in command
try {
    $result = $this->executeCommand();
    return $result;
} catch (ConfigurationException $e) {
    $this->error("Configuration error: " . $e->getMessage());
    return 3;
} catch (DatabaseException $e) {
    $this->error("Database error: " . $e->getMessage());
    return 4;
} catch (Exception $e) {
    $this->error("General error: " . $e->getMessage());
    return 1;
}
```

### Performance Optimization

The CLI is optimized for PHP's execution model:

- **Memory Management** - Efficient memory usage for large configurations
- **Caching** - Built-in caching for frequently accessed data
- **Lazy Loading** - Components loaded only when needed
- **Binary Support** - Native support for .pnt binary format

## Quick Reference

### Most Common Commands

```bash
# System information
tsk version
tsk status

# Configuration
tsk parse config.tsk
tsk config get app.name
tsk config compile config.tsk

# Database
tsk db status
tsk db migrate

# Development
tsk serve 3000
tsk test all

# Services
tsk services status
tsk services start

# Cache
tsk cache clear
tsk cache status
```

### Command Patterns

```bash
# Get help for any command
tsk <command> --help

# Use JSON output
tsk <command> --json

# Enable verbose mode
tsk <command> --verbose

# Suppress output
tsk <command> --quiet

# Debug mode
tsk --debug <command>
```

## Examples

### Basic Workflow

```bash
# 1. Check system
tsk status

# 2. Parse configuration
tsk parse config.tsk

# 3. Validate configuration
tsk validate config.tsk

# 4. Compile to binary
tsk config compile config.tsk

# 5. Load configuration
tsk config load .

# 6. Get values
tsk config get app.name
tsk config get server.port
```

### Development Workflow

```bash
# 1. Start development server
tsk serve 3000

# 2. Run tests
tsk test all

# 3. Check database
tsk db status

# 4. Clear cache
tsk cache clear

# 5. Restart services
tsk services restart
```

### Production Workflow

```bash
# 1. Compile configurations
tsk config compile config.tsk
tsk peanuts compile

# 2. Run migrations
tsk db migrate

# 3. Warm cache
tsk cache warm

# 4. Start services
tsk services start

# 5. Monitor status
tsk services status
tsk cache status
```

## Integration Examples

### PHP Script Integration

```php
<?php
// Execute CLI commands from PHP
function executeTskCommand($command, $options = []) {
    $cmd = "tsk $command";
    
    if (!empty($options['json'])) {
        $cmd .= " --json";
    }
    
    if (!empty($options['verbose'])) {
        $cmd .= " --verbose";
    }
    
    $output = [];
    $returnCode = 0;
    
    exec($cmd . " 2>&1", $output, $returnCode);
    
    return [
        'output' => $output,
        'code' => $returnCode,
        'command' => $cmd
    ];
}

// Usage examples
$result = executeTskCommand('config get app.name', ['json' => true]);
$result = executeTskCommand('db status');
$result = executeTskCommand('services list', ['verbose' => true]);
```

### Shell Script Integration

```bash
#!/bin/bash

# Check if tsk is available
if ! command -v tsk &> /dev/null; then
    echo "Error: tsk command not found"
    exit 1
fi

# Get configuration value
APP_NAME=$(tsk config get app.name --quiet)
if [ $? -eq 0 ]; then
    echo "App name: $APP_NAME"
else
    echo "Failed to get app name"
    exit 1
fi

# Check database status
tsk db status --quiet
if [ $? -eq 0 ]; then
    echo "Database: OK"
else
    echo "Database: Error"
    exit 1
fi
```

### CI/CD Integration

```yaml
# GitHub Actions example
name: TuskLang CI

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup PHP
      uses: shivammathur/setup-php@v2
      with:
        php-version: '8.2'
        extensions: pdo, json, mbstring
    
    - name: Install TuskLang
      run: |
        curl -sSL tusklang.org/tsk.sh | sudo bash
    
    - name: Run tests
      run: tsk test all
    
    - name: Check configuration
      run: |
        tsk parse config.tsk
        tsk validate config.tsk
    
    - name: Compile configuration
      run: tsk config compile config.tsk
```

## Troubleshooting

### Common Issues

#### Command Not Found

```bash
# Check if tsk is in PATH
which tsk

# Add to PATH if needed
export PATH="/opt/tusklang/bin:$PATH"

# Or use full path
/opt/tusklang/bin/tsk version
```

#### Permission Denied

```bash
# Fix permissions
sudo chmod +x /usr/local/bin/tsk

# Or run with sudo
sudo tsk db migrate
```

#### Configuration Errors

```bash
# Check configuration syntax
tsk parse config.tsk

# Validate configuration
tsk validate config.tsk

# Show configuration details
tsk config check
```

### Debug Mode

```bash
# Enable debug output
tsk --debug version

# Debug specific command
tsk --debug config get app.name

# Check logs
tail -f /var/log/tusklang/cli.log
```

## Getting Help

```bash
# General help
tsk help

# Command-specific help
tsk db --help
tsk config get --help

# List all commands
tsk --help

# Version information
tsk version
```

## Related Documentation

- [Installation Guide](../installation.md)
- [Quick Start Guide](../quickstart.md)
- [Examples](../examples/README.md)
- [Troubleshooting](../troubleshooting.md)

**Strong. Secure. Scalable.** 🐘 