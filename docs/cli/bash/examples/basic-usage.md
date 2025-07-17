# Basic Usage Examples

Common usage patterns and simple examples for the TuskLang Bash CLI.

## Basic CLI Usage

### Check CLI Installation
```bash
# Verify CLI is installed and working
tsk version

# Show help
tsk help

# Check specific command help
tsk config --help
```

### Configuration Management

#### Create Basic Configuration
```bash
# Create a simple configuration file
cat > peanu.peanuts << 'EOF'
[app]
name: "My TuskLang App"
version: "1.0.0"

[server]
host: "localhost"
port: 3000

[database]
type: "sqlite"
path: "./data/app.db"
EOF
```

#### Load and Access Configuration
```bash
# Load configuration
tsk peanuts load peanu.peanuts

# Get specific values
tsk config get app.name
tsk config get server.port
tsk config get database.path

# Get with default values
tsk config get server.host "127.0.0.1"
```

#### Compile to Binary Format
```bash
# Compile for better performance
tsk peanuts compile peanu.peanuts

# Load binary configuration
tsk peanuts load peanu.pnt
```

### Database Operations

#### Initialize Database
```bash
# Initialize SQLite database
tsk db init

# Check database status
tsk db status
```

#### Run Migrations
```bash
# Create migration file
cat > migrations/001_create_users.sql << 'EOF'
CREATE TABLE users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    email TEXT UNIQUE NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);
EOF

# Run migration
tsk db migrate migrations/001_create_users.sql
```

#### Database Console
```bash
# Open interactive console
tsk db console

# Execute single command
tsk db console --command "SELECT COUNT(*) FROM users"
```

## Performance Optimization

### Use Binary Configuration
```bash
# Always compile to binary for production
tsk peanuts compile peanu.peanuts

# Use binary in scripts
#!/bin/bash
APP_NAME=$(tsk config get app.name --quiet)
SERVER_PORT=$(tsk config get server.port --quiet)
```

### Enable Caching
```bash
# Enable configuration caching
export PEANUT_CACHE_ENABLED=1

# Use cached configuration
tsk config get server.port
```

### Optimize Database Operations
```bash
# Use transactions for multiple operations
tsk db migrate --transaction schema.sql

# Backup before major changes
tsk db backup before-migration.sql
tsk db migrate schema.sql
```

## Error Handling

### Check Command Status
```bash
#!/bin/bash
# safe_operation.sh

# Check if command succeeded
if tsk db status --quiet; then
    echo "Database is ready"
else
    echo "Database connection failed"
    exit 1
fi
```

### Handle Configuration Errors
```bash
#!/bin/bash
# load_config.sh

# Load configuration with error handling
if config=$(tsk peanuts load peanu.pnt 2>/dev/null); then
    echo "Configuration loaded successfully"
else
    echo "Failed to load configuration"
    exit 1
fi
```

### Validate Before Operations
```bash
#!/bin/bash
# deploy.sh

# Validate configuration before deployment
if ! tsk config validate; then
    echo "Configuration validation failed"
    exit 1
fi

# Check database status
if ! tsk db status --quiet; then
    echo "Database is not ready"
    exit 1
fi

echo "Ready for deployment"
```

## Common Patterns

### Environment-Specific Configuration
```bash
# Create environment-specific configs
cat > peanu.development.peanuts << 'EOF'
[app]
environment: "development"
debug: true

[server]
port: 3000
EOF

cat > peanu.production.peanuts << 'EOF'
[app]
environment: "production"
debug: false

[server]
port: 80
EOF

# Use appropriate config
if [[ "$NODE_ENV" == "production" ]]; then
    tsk peanuts load peanu.production.peanuts
else
    tsk peanuts load peanu.development.peanuts
fi
```

### Configuration in Scripts
```bash
#!/bin/bash
# start_server.sh

# Load configuration
source <(tsk peanuts load peanu.pnt)

# Extract values
APP_NAME=$(echo "$config" | grep "^app.name=" | cut -d'=' -f2)
SERVER_PORT=$(echo "$config" | grep "^server.port=" | cut -d'=' -f2)

echo "Starting $APP_NAME on port $SERVER_PORT"
python3 -m http.server "$SERVER_PORT"
```

### Health Checks
```bash
#!/bin/bash
# health_check.sh

# Check database health
if tsk db status --json | jq -e '.status == "connected"'; then
    echo "✅ Database is healthy"
else
    echo "❌ Database is unhealthy"
    exit 1
fi

# Check configuration
if tsk config validate --quiet; then
    echo "✅ Configuration is valid"
else
    echo "❌ Configuration has errors"
    exit 1
fi
```

## Best Practices

### 1. Always Use Binary Format in Production
```bash
# Development: use text format for easy editing
# Production: always compile to binary
tsk peanuts compile peanu.peanuts
```

### 2. Validate Configuration Before Use
```bash
# Always validate before operations
tsk config validate
```

### 3. Use Quiet Mode in Scripts
```bash
# Use --quiet for script automation
PORT=$(tsk config get server.port --quiet)
```

### 4. Handle Errors Gracefully
```bash
#!/bin/bash
# robust_script.sh

set -e  # Exit on any error

# Check prerequisites
tsk db status --quiet || exit 1
tsk config validate --quiet || exit 1

# Proceed with operations
echo "All checks passed"
```

### 5. Use JSON Output for Automation
```bash
# JSON output for parsing
DB_STATUS=$(tsk db status --json)
DB_CONNECTED=$(echo "$DB_STATUS" | jq -r '.status')

if [[ "$DB_CONNECTED" == "connected" ]]; then
    echo "Database is ready"
fi
```

## Troubleshooting Tips

### Configuration Issues
```bash
# Check if configuration file exists
ls -la *.peanuts *.tsk *.pnt

# Validate configuration syntax
tsk config validate --verbose

# Check configuration hierarchy
tsk config check
```

### Database Issues
```bash
# Check database status
tsk db status --verbose

# Test connection manually
tsk db console --command "SELECT 1"

# Check database file
ls -la *.db
```

### Performance Issues
```bash
# Use binary format
tsk peanuts compile peanu.peanuts

# Enable caching
export PEANUT_CACHE_ENABLED=1

# Check performance
time tsk config get server.port
```

## Next Steps

Now that you're familiar with basic usage:
- Explore [Complete Workflows](./workflows.md) for real-world scenarios
- Check [Framework Integrations](./integrations.md) for specific use cases
- Review [Command Reference](../commands/README.md) for detailed documentation 