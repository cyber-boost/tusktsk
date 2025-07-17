# 🥜 Peanut Binary Configuration Guide for Bash

A comprehensive guide to using TuskLang's high-performance binary configuration system with Bash.

## Table of Contents

1. [Installation](#installation)
2. [Quick Start](#quick-start)
3. [Core Concepts](#core-concepts)
4. [API Reference](#api-reference)
5. [Advanced Usage](#advanced-usage)
6. [Bash-Specific Features](#bash-specific-features)
7. [Integration Examples](#integration-examples)
8. [Binary Format Details](#binary-format-details)
9. [Performance Guide](#performance-guide)
10. [Troubleshooting](#troubleshooting)
11. [Migration Guide](#migration-guide)
12. [Complete Examples](#complete-examples)
13. [Quick Reference](#quick-reference)

## What is Peanut Configuration?

Peanut Configuration is TuskLang's high-performance binary configuration system that provides hierarchical configuration management with ~85% performance improvement over text parsing. This guide covers Bash-specific usage of the Peanut configuration system.

## Installation

### Prerequisites

- Bash 4.0 or higher
- TuskLang Bash SDK installed
- POSIX-compliant shell environment

### Installing the SDK

```bash
# Clone the TuskLang repository
git clone https://github.com/cyber-boost/tusktsk-bash.git
cd tusklang-bash

# Make the CLI executable
chmod +x cli/main.sh

# Add to PATH (optional)
sudo ln -s "$(pwd)/cli/main.sh" /usr/local/bin/tsk
```

### Importing PeanutConfig

```bash
# Source the PeanutConfig module
source peanut_config.sh

# Or use the CLI
tsk peanuts load config.pnt
```

## Quick Start

### Your First Peanut Configuration

1. Create a `peanu.peanuts` file:

```ini
[app]
name: "My Bash App"
version: "1.0.0"

[server]
host: "localhost"
port: 8080

[database]
type: "sqlite"
path: "./data/app.db"
```

2. Load the configuration:

```bash
# Source the module
source peanut_config.sh

# Load configuration
config=$(peanut_load)
echo "$config"
```

3. Access values:

```bash
# Get specific values
app_name=$(peanut_get "app.name" "Default App")
server_port=$(peanut_get "server.port" "3000")
db_path=$(peanut_get "database.path" "./default.db")

echo "App: $app_name"
echo "Port: $server_port"
echo "DB: $db_path"
```

## Core Concepts

### File Types

- `.peanuts` - Human-readable configuration (INI-style)
- `.tsk` - TuskLang syntax (advanced features)
- `.pnt` - Compiled binary format (85% faster)

### Hierarchical Loading

Peanut Configuration uses CSS-like cascading where child directories override parent configurations:

```
/project/
├── peanu.peanuts          # Base configuration
├── api/
│   └── peanu.peanuts      # API-specific overrides
└── web/
    └── peanu.peanuts      # Web-specific overrides
```

### Type System

The system automatically infers types from values:

```bash
# String values
app.name: "My App"

# Numeric values
server.port: 8080
cache.size: 1024

# Boolean values
debug.enabled: true
ssl.required: false

# Array values
allowed.hosts: ["localhost", "127.0.0.1"]

# Object values
database: {
  host: "localhost"
  port: 5432
  name: "myapp"
}
```

## API Reference

### PeanutConfig Functions

#### `peanut_load [directory]`

Load configuration with inheritance from the specified directory.

**Parameters:**
- `directory` (optional): Directory path (default: current directory)

**Returns:** Merged configuration as key=value pairs

**Examples:**

```bash
# Load from current directory
config=$(peanut_load)

# Load from specific directory
config=$(peanut_load "/path/to/project")

# Load and process
while IFS='=' read -r key value; do
    [[ -z "$key" || "$key" =~ ^# ]] && continue
    export "$key"="$value"
done <<< "$(peanut_load)"
```

#### `peanut_get key_path [default_value] [directory]`

Get configuration value by dot-separated path.

**Parameters:**
- `key_path`: Dot-separated key path (e.g., "server.host")
- `default_value` (optional): Default value if key not found
- `directory` (optional): Directory to load from

**Returns:** Configuration value or default

**Examples:**

```bash
# Get simple value
host=$(peanut_get "server.host" "localhost")

# Get nested value
db_port=$(peanut_get "database.postgres.port" "5432")

# Get with default
debug_level=$(peanut_get "app.debug.level" "info")
```

#### `peanut_compile_binary input_file output_file`

Compile text configuration to binary format.

**Parameters:**
- `input_file`: Source .peanuts or .tsk file
- `output_file`: Target .pnt file

**Examples:**

```bash
# Compile to binary
peanut_compile_binary "config.peanuts" "config.pnt"

# Compile with CLI
tsk peanuts compile config.peanuts
```

#### `peanut_load_binary binary_file`

Load configuration from binary format.

**Parameters:**
- `binary_file`: .pnt file path

**Returns:** Configuration as key=value pairs

**Examples:**

```bash
# Load binary config
config=$(peanut_load_binary "config.pnt")

# Load and export
while IFS='=' read -r key value; do
    export "$key"="$value"
done <<< "$(peanut_load_binary "config.pnt")"
```

#### `peanut_show_hierarchy [directory]`

Display configuration hierarchy for debugging.

**Parameters:**
- `directory` (optional): Directory to analyze

**Examples:**

```bash
# Show current directory hierarchy
peanut_show_hierarchy

# Show specific directory
peanut_show_hierarchy "/path/to/project"
```

#### `peanut_validate [directory]`

Validate configuration syntax and hierarchy.

**Parameters:**
- `directory` (optional): Directory to validate

**Returns:** Exit code 0 for valid, 1 for invalid

**Examples:**

```bash
# Validate current directory
if peanut_validate; then
    echo "Configuration is valid"
else
    echo "Configuration has errors"
fi
```

## Advanced Usage

### File Watching

```bash
# Watch for configuration changes
peanut_watch() {
    local directory="${1:-.}"
    local callback="$2"
    
    inotifywait -m -r -e modify,create,delete "$directory" | while read -r path action file; do
        if [[ "$file" =~ \.(peanuts|tsk|pnt)$ ]]; then
            echo "Configuration changed: $file"
            eval "$callback"
        fi
    done
}

# Usage
peanut_watch "." "reload_config"
```

### Custom Serialization

```bash
# Custom type handling
peanut_parse_custom() {
    local content="$1"
    local type="$2"
    
    case "$type" in
        "json")
            echo "$content" | jq -r 'to_entries | .[] | "\(.key)=\(.value)"'
            ;;
        "yaml")
            echo "$content" | yq -r 'to_entries | .[] | "\(.key)=\(.value)"'
            ;;
        *)
            echo "$content"
            ;;
    esac
}
```

### Performance Optimization

```bash
# Enable caching
export PEANUT_CACHE_ENABLED=1

# Pre-load configurations
declare -A PEANUT_CACHE

# Singleton pattern
peanut_singleton() {
    if [[ -z "${PEANUT_SINGLETON:-}" ]]; then
        PEANUT_SINGLETON=$(peanut_load)
    fi
    echo "$PEANUT_SINGLETON"
}
```

### Thread Safety

```bash
# Use file locks for concurrent access
peanut_load_safe() {
    local directory="${1:-.}"
    local lock_file="/tmp/peanut_$(echo "$directory" | md5sum | cut -d' ' -f1).lock"
    
    (
        flock -x 200
        peanut_load "$directory"
    ) 200>"$lock_file"
}
```

## Bash-Specific Features

### Associative Arrays

```bash
# Load into associative array
declare -A config
while IFS='=' read -r key value; do
    [[ -z "$key" || "$key" =~ ^# ]] && continue
    config["$key"]="$value"
done <<< "$(peanut_load)"

# Access values
echo "Server: ${config[server.host]}:${config[server.port]}"
```

### Environment Variables

```bash
# Export all configuration as environment variables
peanut_export_env() {
    local directory="${1:-.}"
    local prefix="${2:-}"
    
    while IFS='=' read -r key value; do
        [[ -z "$key" || "$key" =~ ^# ]] && continue
        export "${prefix}${key//./_}"="$value"
    done <<< "$(peanut_load "$directory")"
}

# Usage
peanut_export_env "." "APP_"
echo "$APP_SERVER_HOST"
```

### Function Integration

```bash
# Create configuration-aware functions
config_get() {
    local key="$1"
    local default="${2:-}"
    peanut_get "$key" "$default"
}

config_set() {
    local key="$1"
    local value="$2"
    # Implementation for setting values
}

# Usage
server_port=$(config_get "server.port" "3000")
config_set "debug.enabled" "true"
```

### Shell Script Integration

```bash
#!/bin/bash
# config.sh - Configuration loader

# Source PeanutConfig
source "$(dirname "$0")/peanut_config.sh"

# Load configuration
CONFIG=$(peanut_load)

# Export as environment variables
while IFS='=' read -r key value; do
    [[ -z "$key" || "$key" =~ ^# ]] && continue
    export "$key"="$value"
done <<< "$CONFIG"

# Configuration functions
get_config() {
    local key="$1"
    local default="${2:-}"
    peanut_get "$key" "$default"
}

# Usage in other scripts
source config.sh
echo "Port: $(get_config 'server.port' '3000')"
```

## Integration Examples

### Web Server Configuration

```bash
#!/bin/bash
# server.sh

source peanut_config.sh

# Load server configuration
SERVER_HOST=$(peanut_get "server.host" "localhost")
SERVER_PORT=$(peanut_get "server.port" "8080")
DB_PATH=$(peanut_get "database.path" "./data.db")

# Start server
echo "Starting server on $SERVER_HOST:$SERVER_PORT"
python3 -m http.server "$SERVER_PORT" --bind "$SERVER_HOST"
```

### Microservice Configuration

```bash
#!/bin/bash
# service.sh

source peanut_config.sh

# Load service-specific config
SERVICE_NAME=$(peanut_get "service.name" "unknown")
LOG_LEVEL=$(peanut_get "logging.level" "info")
REDIS_URL=$(peanut_get "redis.url" "redis://localhost:6379")

# Start service
echo "Starting $SERVICE_NAME with log level $LOG_LEVEL"
./service --redis="$REDIS_URL" --log-level="$LOG_LEVEL"
```

### CLI Tool Configuration

```bash
#!/bin/bash
# cli.sh

source peanut_config.sh

# Load CLI configuration
DEFAULT_OUTPUT=$(peanut_get "cli.output" "text")
VERBOSE=$(peanut_get "cli.verbose" "false")
CONFIG_PATH=$(peanut_get "cli.config" "./config.peanuts")

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --output)
            DEFAULT_OUTPUT="$2"
            shift 2
            ;;
        --verbose)
            VERBOSE="true"
            shift
            ;;
        *)
            break
            ;;
    esac
done

# Execute command
if [[ "$VERBOSE" == "true" ]]; then
    echo "Using output format: $DEFAULT_OUTPUT"
    echo "Config path: $CONFIG_PATH"
fi
```

## Binary Format Specification

### File Structure

| Offset | Size | Description |
|--------|------|-------------|
| 0 | 4 | Magic: "PNUT" |
| 4 | 4 | Version (LE) |
| 8 | 8 | Timestamp (LE) |
| 16 | 8 | SHA256 checksum |
| 24 | N | Serialized data |

### Serialization Format

Bash uses a simple key=value format for serialization:

```bash
# Binary format structure
MAGIC="PNUT"
VERSION=1
TIMESTAMP=$(date +%s)
CHECKSUM=$(echo "$DATA" | sha256sum | cut -d' ' -f1)

# Serialized data format
key1=value1
key2=value2
nested.key=value3
```

### Binary File Creation

```bash
peanut_create_binary() {
    local input_file="$1"
    local output_file="$2"
    
    # Parse input
    local data
    data=$(peanut_parse_text "$(<"$input_file")")
    
    # Create binary header
    {
        printf "PNUT"
        printf "%08x" 1 | xxd -r -p  # Version
        printf "%016x" "$(date +%s)" | xxd -r -p  # Timestamp
        echo "$data" | sha256sum | cut -d' ' -f1 | xxd -r -p  # Checksum
        echo "$data"  # Data
    } > "$output_file"
}
```

## Performance Guide

### Benchmarks

```bash
# Performance comparison script
benchmark_config() {
    local file="$1"
    local iterations=1000
    
    echo "Benchmarking $file..."
    
    # Text parsing
    start=$(date +%s%N)
    for i in $(seq $iterations); do
        peanut_parse_text "$(<"$file")" > /dev/null
    done
    text_time=$((($(date +%s%N) - start) / 1000000))
    
    # Binary loading
    local binary_file="${file%.*}.pnt"
    peanut_compile_binary "$file" "$binary_file"
    
    start=$(date +%s%N)
    for i in $(seq $iterations); do
        peanut_load_binary "$binary_file" > /dev/null
    done
    binary_time=$((($(date +%s%N) - start) / 1000000))
    
    echo "Text parsing: ${text_time}ms"
    echo "Binary loading: ${binary_time}ms"
    echo "Speedup: $((text_time / binary_time))x"
}
```

### Best Practices

1. **Always use .pnt in production**
   ```bash
   # Compile before deployment
   peanut_compile_binary "config.peanuts" "config.pnt"
   ```

2. **Cache configuration objects**
   ```bash
   # Cache loaded configuration
   declare -A CONFIG_CACHE
   CONFIG_CACHE["$PWD"]=$(peanut_load)
   ```

3. **Use file watching wisely**
   ```bash
   # Only watch in development
   if [[ "$NODE_ENV" == "development" ]]; then
       peanut_watch "." "reload_config"
   fi
   ```

4. **Optimize for Bash**
   ```bash
   # Use associative arrays for fast lookups
   declare -A config
   while IFS='=' read -r key value; do
       config["$key"]="$value"
   done <<< "$(peanut_load)"
   ```

## Troubleshooting

### Common Issues

#### File Not Found

**Problem:** Configuration file not found

**Solution:**
```bash
# Check file existence
if [[ ! -f "peanu.peanuts" ]]; then
    echo "Configuration file not found"
    exit 1
fi

# Use absolute paths
config=$(peanut_load "$(pwd)")
```

#### Checksum Mismatch

**Problem:** Binary file corruption

**Solution:**
```bash
# Recompile binary
peanut_compile_binary "config.peanuts" "config.pnt"

# Verify checksum
expected_checksum=$(echo "$data" | sha256sum | cut -d' ' -f1)
actual_checksum=$(dd if="config.pnt" bs=1 skip=16 count=32 2>/dev/null | xxd -p)
if [[ "$expected_checksum" != "$actual_checksum" ]]; then
    echo "Checksum mismatch - recompiling"
    peanut_compile_binary "config.peanuts" "config.pnt"
fi
```

#### Performance Issues

**Problem:** Slow configuration loading

**Solution:**
```bash
# Enable caching
export PEANUT_CACHE_ENABLED=1

# Use binary format
peanut_compile_binary "config.peanuts" "config.pnt"

# Pre-load in background
(peanut_load > /dev/null) &
```

### Debug Mode

```bash
# Enable debug logging
export PEANUT_DEBUG=1

# Debug function
peanut_debug() {
    if [[ "${PEANUT_DEBUG:-0}" -eq 1 ]]; then
        echo "[DEBUG] $*" >&2
    fi
}

# Usage
peanut_debug "Loading configuration from $directory"
```

## Migration Guide

### From JSON

```bash
# Convert JSON to Peanuts format
json_to_peanuts() {
    local json_file="$1"
    local peanuts_file="$2"
    
    jq -r 'to_entries | .[] | "\(.key): \(.value)"' "$json_file" > "$peanuts_file"
}

# Usage
json_to_peanuts "config.json" "peanu.peanuts"
peanut_compile_binary "peanu.peanuts" "peanu.pnt"
```

### From YAML

```bash
# Convert YAML to Peanuts format
yaml_to_peanuts() {
    local yaml_file="$1"
    local peanuts_file="$2"
    
    yq -r 'to_entries | .[] | "\(.key): \(.value)"' "$yaml_file" > "$peanuts_file"
}

# Usage
yaml_to_peanuts "config.yaml" "peanu.peanuts"
peanut_compile_binary "peanu.peanuts" "peanu.pnt"
```

### From .env

```bash
# Convert .env to Peanuts format
env_to_peanuts() {
    local env_file="$1"
    local peanuts_file="$2"
    
    # Convert KEY=value to key: value
    sed 's/^\([^=]*\)=\(.*\)$/\1: \2/' "$env_file" > "$peanuts_file"
}

# Usage
env_to_peanuts ".env" "peanu.peanuts"
peanut_compile_binary "peanu.peanuts" "peanu.pnt"
```

## Complete Examples

### Web Application Configuration

**File Structure:**
```
myapp/
├── peanu.peanuts
├── api/
│   └── peanu.peanuts
├── web/
│   └── peanu.peanuts
├── server.sh
└── config.sh
```

**peanu.peanuts:**
```ini
[app]
name: "My Web App"
version: "1.0.0"

[server]
host: "0.0.0.0"
port: 3000

[database]
type: "postgres"
host: "localhost"
port: 5432
name: "myapp"
```

**api/peanu.peanuts:**
```ini
[api]
version: "v1"
rate_limit: 1000

[server]
port: 3001
```

**web/peanu.peanuts:**
```ini
[web]
static_dir: "./public"
template_dir: "./templates"

[server]
port: 3002
```

**config.sh:**
```bash
#!/bin/bash
source peanut_config.sh

# Load base configuration
BASE_CONFIG=$(peanut_load)

# Load API configuration
cd api
API_CONFIG=$(peanut_load)
cd ..

# Load web configuration
cd web
WEB_CONFIG=$(peanut_load)
cd ..

# Export environment variables
export_config() {
    local config="$1"
    local prefix="$2"
    
    while IFS='=' read -r key value; do
        [[ -z "$key" || "$key" =~ ^# ]] && continue
        export "${prefix}${key//./_}"="$value"
    done <<< "$config"
}

export_config "$BASE_CONFIG" "APP_"
export_config "$API_CONFIG" "API_"
export_config "$WEB_CONFIG" "WEB_"
```

**server.sh:**
```bash
#!/bin/bash
source config.sh

echo "Starting $APP_APP_NAME v$APP_APP_VERSION"
echo "API server on port $API_SERVER_PORT"
echo "Web server on port $WEB_SERVER_PORT"

# Start API server
cd api
python3 -m http.server "$API_SERVER_PORT" &
API_PID=$!

# Start web server
cd ../web
python3 -m http.server "$WEB_SERVER_PORT" &
WEB_PID=$!

# Wait for servers
wait $API_PID $WEB_PID
```

### Microservice Configuration

**File Structure:**
```
microservices/
├── peanu.peanuts
├── auth-service/
│   ├── peanu.peanuts
│   └── start.sh
├── user-service/
│   ├── peanu.peanuts
│   └── start.sh
└── common/
    └── config.sh
```

**peanu.peanuts:**
```ini
[common]
environment: "production"
log_level: "info"

[redis]
host: "redis-cluster"
port: 6379

[monitoring]
enabled: true
metrics_port: 9090
```

**auth-service/peanu.peanuts:**
```ini
[service]
name: "auth-service"
port: 3001

[jwt]
secret: "your-secret-key"
expiry: 3600

[database]
type: "postgres"
host: "auth-db"
port: 5432
name: "auth"
```

**common/config.sh:**
```bash
#!/bin/bash
source peanut_config.sh

# Load common configuration
COMMON_CONFIG=$(peanut_load "../")

# Load service-specific configuration
SERVICE_CONFIG=$(peanut_load ".")

# Merge configurations
MERGED_CONFIG=$(peanut_deep_merge "$COMMON_CONFIG" "$SERVICE_CONFIG")

# Export as environment variables
while IFS='=' read -r key value; do
    [[ -z "$key" || "$key" =~ ^# ]] && continue
    export "$key"="$value"
done <<< "$MERGED_CONFIG"
```

**auth-service/start.sh:**
```bash
#!/bin/bash
source ../common/config.sh

echo "Starting $SERVICE_NAME on port $SERVICE_PORT"
echo "Environment: $COMMON_ENVIRONMENT"
echo "Log level: $COMMON_LOG_LEVEL"

# Start service
./auth-service \
    --port="$SERVICE_PORT" \
    --jwt-secret="$JWT_SECRET" \
    --jwt-expiry="$JWT_EXPIRY" \
    --db-host="$DATABASE_HOST" \
    --db-port="$DATABASE_PORT" \
    --db-name="$DATABASE_NAME"
```

### CLI Tool Configuration

**File Structure:**
```
cli-tool/
├── peanu.peanuts
├── tsk
├── config.sh
└── commands/
    ├── build.sh
    ├── test.sh
    └── deploy.sh
```

**peanu.peanuts:**
```ini
[cli]
name: "My CLI Tool"
version: "1.0.0"
default_command: "help"

[output]
format: "text"
colors: true
verbose: false

[build]
target: "release"
optimize: true

[test]
framework: "bats"
coverage: true

[deploy]
environment: "staging"
region: "us-west-2"
```

**config.sh:**
```bash
#!/bin/bash
source peanut_config.sh

# Load configuration
CONFIG=$(peanut_load)

# Parse configuration into variables
while IFS='=' read -r key value; do
    [[ -z "$key" || "$key" =~ ^# ]] && continue
    declare "$key"="$value"
done <<< "$CONFIG"

# Configuration functions
get_config() {
    local key="$1"
    local default="${2:-}"
    peanut_get "$key" "$default"
}

set_config() {
    local key="$1"
    local value="$2"
    # Implementation for setting values
}

# Output formatting
print_success() {
    if [[ "$output.colors" == "true" ]]; then
        echo -e "\033[32m✅ $*\033[0m"
    else
        echo "✅ $*"
    fi
}

print_error() {
    if [[ "$output.colors" == "true" ]]; then
        echo -e "\033[31m❌ $*\033[0m"
    else
        echo "❌ $*"
    fi
}

print_info() {
    if [[ "$output.colors" == "true" ]]; then
        echo -e "\033[34mℹ️  $*\033[0m"
    else
        echo "ℹ️  $*"
    fi
}
```

**tsk:**
```bash
#!/bin/bash
source config.sh

# Show version
if [[ "$1" == "--version" || "$1" == "-v" ]]; then
    echo "$cli.name v$cli.version"
    exit 0
fi

# Show help
if [[ "$1" == "--help" || "$1" == "-h" || -z "$1" ]]; then
    echo "Usage: $0 <command> [options]"
    echo "Commands:"
    echo "  build    Build the project"
    echo "  test     Run tests"
    echo "  deploy   Deploy to environment"
    exit 0
fi

# Execute command
case "$1" in
    "build")
        ./commands/build.sh "${@:2}"
        ;;
    "test")
        ./commands/test.sh "${@:2}"
        ;;
    "deploy")
        ./commands/deploy.sh "${@:2}"
        ;;
    *)
        print_error "Unknown command: $1"
        exit 1
        ;;
esac
```

**commands/build.sh:**
```bash
#!/bin/bash
source ../config.sh

print_info "Building project..."

# Use configuration values
if [[ "$build.optimize" == "true" ]]; then
    print_info "Building with optimizations"
    # Build with optimizations
else
    print_info "Building without optimizations"
    # Build without optimizations
fi

print_success "Build completed successfully"
```

## Quick Reference

### Common Operations

```bash
# Load config
config=$(peanut_load)

# Get value
value=$(peanut_get "key.path" "default")

# Compile to binary
peanut_compile_binary "config.peanuts" "config.pnt"

# Load binary
config=$(peanut_load_binary "config.pnt")

# Watch for changes
peanut_watch "." "reload_config"
```

### Environment Variables

```bash
# Export all config as environment variables
while IFS='=' read -r key value; do
    export "$key"="$value"
done <<< "$(peanut_load)"

# Export with prefix
while IFS='=' read -r key value; do
    export "APP_${key//./_}"="$value"
done <<< "$(peanut_load)"
```

### CLI Usage

```bash
# Load configuration
tsk peanuts load config.pnt

# Compile configuration
tsk peanuts compile config.peanuts

# Get configuration value
tsk config get server.port

# Validate configuration
tsk config validate
```

### Performance Tips

1. **Use binary format in production**
2. **Enable caching for repeated loads**
3. **Pre-compile configurations**
4. **Use associative arrays for fast lookups**
5. **Avoid repeated file system calls**

### Best Practices

1. **Always validate configuration**
2. **Use hierarchical loading**
3. **Provide sensible defaults**
4. **Handle errors gracefully**
5. **Document configuration options**

---

This guide provides everything you need to use Peanut Configuration effectively with Bash. For more information, visit the [TuskLang documentation](https://tusklang.org/docs). 