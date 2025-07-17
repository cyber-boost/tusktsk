# Quick Start Guide

Get up and running with TuskLang Ruby CLI in minutes.

## First Steps

### 1. Verify Installation

```bash
# Check if CLI is installed
tsk --version
```

**Expected output:**
```
TuskLang Ruby SDK v2.0.0
```

### 2. Show Available Commands

```bash
# See all available commands
tsk --help
```

**Expected output:**
```
Usage: tsk [OPTIONS] COMMAND [ARGS]...

TuskLang Ruby SDK CLI

Options:
  --help, -h      Show this help message
  --version, -v   Show version information
  --verbose       Enable verbose output
  --quiet         Suppress output
  --config PATH   Configuration file path
  --json          Output in JSON format

Commands:
  db       Database operations
  dev      Development tools
  test     Test execution
  services Service management
  cache    Cache operations
  config   Configuration management
  binary   Binary compilation
  peanuts  Peanut configuration
  ai       AI integrations
  css      CSS utilities
  license  License management
  utility  General utilities
```

## Basic Commands

### Configuration Management

```bash
# Create a simple configuration
cat > peanu.peanuts << EOF
[app]
name: "My First App"
version: "1.0.0"

[server]
host: "localhost"
port: 3000
EOF

# Get a configuration value
tsk config get app.name

# Check configuration hierarchy
tsk config check
```

### Database Operations

```bash
# Initialize a database
tsk db init

# Check database status
tsk db status

# Open database console
tsk db console
```

### Development Server

```bash
# Start development server
tsk serve 3000

# Server will be available at http://localhost:3000
```

## Your First Project

### 1. Create Project Structure

```bash
# Create project directory
mkdir my-tusk-app
cd my-tusk-app

# Initialize project
mkdir -p {config,lib,spec}
```

### 2. Create Configuration

```bash
# Create main configuration
cat > peanu.peanuts << EOF
[app]
name: "My TuskLang App"
version: "1.0.0"
environment: "development"

[server]
host: "localhost"
port: 3000
timeout: 30

[database]
adapter: "sqlite3"
database: "app.db"

[features]
debug: true
logging: true
EOF
```

### 3. Initialize Database

```bash
# Initialize SQLite database
tsk db init

# Verify database
tsk db status
```

### 4. Start Development

```bash
# Start development server
tsk serve 3000

# In another terminal, check configuration
tsk config get server.port
```

## Common Workflows

### Development Workflow

```bash
# 1. Start development server
tsk serve 3000

# 2. Run tests
tsk test all

# 3. Check configuration
tsk config validate

# 4. Compile configuration to binary
tsk config compile
```

### AI-Assisted Development

```bash
# Generate Ruby class
tsk ai claude "Create a User class with email validation"

# Analyze existing code
tsk ai analyze lib/user.rb

# Optimize performance
tsk ai optimize lib/performance_critical.rb
```

### Configuration Management

```bash
# Get configuration value
tsk config get server.port

# Check configuration hierarchy
tsk config check

# Validate configuration
tsk config validate

# Compile to binary for performance
tsk config compile
```

## Integration Examples

### Rails Integration

```ruby
# In config/application.rb
class Application < Rails::Application
  # Load TuskLang configuration
  config_loader = PeanutConfig.new
  app_config = config_loader.load(Rails.root.join('config'))
  
  # Apply configuration
  config.app_name = app_config['app']['name']
  config.debug_mode = app_config['app']['debug']
end
```

### Jekyll Integration

```ruby
# In _plugins/tusk_lang.rb
Jekyll::Hooks.register :site, :after_init do |site|
  # Load TuskLang configuration
  config = TuskLang::TSK.from_file('_config.tsk')
  
  # Merge with Jekyll config
  site.config.merge!(config.to_hash)
end
```

### CLI Tool Integration

```ruby
#!/usr/bin/env ruby
require 'tusk_lang'

# Load configuration
config = TuskLang::TSK.from_file('config.tsk')

# Use configuration values
puts "App: #{config.get_value('app', 'name')}"
puts "Port: #{config.get_value('server', 'port')}"
```

## Next Steps

### Explore Commands

```bash
# Database commands
tsk help db

# Configuration commands
tsk help config

# AI commands
tsk help ai

# Development commands
tsk help dev
```

### Read Documentation

- [Command Reference](./commands/README.md) - Complete command documentation
- [Examples](./examples/README.md) - Real-world usage examples
- [PNT Guide](../ruby/docs/PNT_GUIDE.md) - Configuration system guide

### Advanced Features

- **Binary Compilation**: Compile configurations for 85% faster loading
- **AI Integration**: Use Claude and ChatGPT for code generation
- **File Watching**: Automatic reloading on configuration changes
- **Hierarchical Configuration**: CSS-like cascading configuration inheritance

## Troubleshooting

### Common Issues

```bash
# Command not found
gem install tusk_lang

# Permission denied
sudo gem install tusk_lang

# Database connection failed
tsk db init

# Configuration not found
ls -la peanu.peanuts
```

### Getting Help

```bash
# General help
tsk --help

# Command-specific help
tsk help [command]

# Version information
tsk --version
```

## Examples Directory

The [Examples](./examples/README.md) section contains complete, working examples for:

- Web applications
- CLI tools
- Rails applications
- Jekyll sites
- Microservices
- Testing workflows

Start with the basic examples and work your way up to more complex integrations! 