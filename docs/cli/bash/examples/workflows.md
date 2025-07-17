# Complete Workflows

Real-world workflows and automation patterns for the TuskLang Bash CLI.

## Development Setup

### Project Initialization
```bash
#!/bin/bash
# init_project.sh

echo "🚀 Initializing TuskLang project..."

# Create project structure
mkdir -p {src,config,data,migrations,tests}

# Create basic configuration
cat > peanu.peanuts << 'EOF'
[app]
name: "My TuskLang Project"
version: "1.0.0"
environment: "development"

[server]
host: "localhost"
port: 3000

[database]
type: "sqlite"
path: "./data/app.db"

[development]
debug: true
auto_reload: true
EOF

# Initialize database
tsk db init

# Compile configuration
tsk peanuts compile peanu.peanuts

echo "✅ Project initialized successfully"
```

### Development Workflow
```bash
#!/bin/bash
# dev_workflow.sh

echo "🔧 Starting development workflow..."

# Load configuration
APP_NAME=$(tsk config get app.name --quiet)
SERVER_PORT=$(tsk config get server.port --quiet)

echo "Starting $APP_NAME on port $SERVER_PORT"

# Check database status
if ! tsk db status --quiet; then
    echo "❌ Database not ready, initializing..."
    tsk db init
fi

# Start development server
tsk serve "$SERVER_PORT" &
SERVER_PID=$!

echo "✅ Development server started (PID: $SERVER_PID)"
echo "Press Ctrl+C to stop"

# Wait for server
wait $SERVER_PID
```

## Testing Workflows

### Test Suite Execution
```bash
#!/bin/bash
# run_tests.sh

echo "🧪 Running test suite..."

# Validate configuration
if ! tsk config validate --quiet; then
    echo "❌ Configuration validation failed"
    exit 1
fi

# Run all tests
echo "Running all tests..."
tsk test all

# Run specific test suites
echo "Running parser tests..."
tsk test parser

echo "Running FUJSEN tests..."
tsk test fujsen

echo "Running performance tests..."
tsk test performance

echo "✅ All tests completed"
```

### Continuous Integration
```bash
#!/bin/bash
# ci_pipeline.sh

set -e

echo "🔄 Starting CI pipeline..."

# Install dependencies (if any)
echo "Installing dependencies..."

# Validate configuration
echo "Validating configuration..."
tsk config validate

# Run tests
echo "Running tests..."
tsk test all

# Check database migrations
echo "Checking database migrations..."
tsk db status

# Build and compile
echo "Building project..."
tsk compile src/main.tsk
tsk peanuts compile peanu.peanuts

# Performance check
echo "Running performance tests..."
tsk test performance

echo "✅ CI pipeline completed successfully"
```

## Deployment Workflows

### Production Deployment
```bash
#!/bin/bash
# deploy.sh

set -e

echo "🚀 Starting production deployment..."

# Load production configuration
if [[ -f "peanu.production.peanuts" ]]; then
    tsk peanuts load peanu.production.peanuts
else
    echo "⚠️  No production config found, using default"
    tsk peanuts load peanu.peanuts
fi

# Validate configuration
echo "Validating configuration..."
tsk config validate

# Check database status
echo "Checking database status..."
if ! tsk db status --quiet; then
    echo "❌ Database not ready"
    exit 1
fi

# Run migrations
echo "Running database migrations..."
for migration in migrations/*.sql; do
    if [[ -f "$migration" ]]; then
        echo "Applying migration: $(basename "$migration")"
        tsk db migrate "$migration"
    fi
done

# Compile for production
echo "Compiling for production..."
tsk peanuts compile peanu.peanuts
tsk binary compile src/main.tsk

# Start services
echo "Starting services..."
tsk services start

echo "✅ Deployment completed successfully"
```

### Rollback Procedure
```bash
#!/bin/bash
# rollback.sh

set -e

echo "🔄 Starting rollback procedure..."

# Stop services
echo "Stopping services..."
tsk services stop

# Restore database from backup
if [[ -f "backup.sql" ]]; then
    echo "Restoring database from backup..."
    tsk db restore backup.sql --force
else
    echo "❌ No backup file found"
    exit 1
fi

# Restart services
echo "Restarting services..."
tsk services start

echo "✅ Rollback completed successfully"
```

## Automation Scripts

### Daily Maintenance
```bash
#!/bin/bash
# daily_maintenance.sh

echo "🔧 Running daily maintenance..."

# Check system health
echo "Checking system health..."
tsk db status
tsk config validate

# Clear old caches
echo "Clearing old caches..."
tsk cache clear

# Backup database
echo "Creating database backup..."
BACKUP_FILE="backup-$(date +%Y%m%d).sql"
tsk db backup "$BACKUP_FILE"

# Clean old backups (keep last 7 days)
find . -name "backup-*.sql" -mtime +7 -delete

# Update configuration cache
echo "Updating configuration cache..."
tsk config compile .

echo "✅ Daily maintenance completed"
```

### Health Monitoring
```bash
#!/bin/bash
# health_monitor.sh

echo "🏥 Running health checks..."

# Check database health
DB_STATUS=$(tsk db status --json)
if echo "$DB_STATUS" | jq -e '.status == "connected"' > /dev/null; then
    echo "✅ Database is healthy"
else
    echo "❌ Database health check failed"
    # Send alert
    echo "Database down at $(date)" >> health_alerts.log
fi

# Check configuration health
if tsk config validate --quiet; then
    echo "✅ Configuration is valid"
else
    echo "❌ Configuration validation failed"
    # Send alert
    echo "Configuration error at $(date)" >> health_alerts.log
fi

# Check service status
SERVICE_STATUS=$(tsk services status --json)
if echo "$SERVICE_STATUS" | jq -e '.status == "running"' > /dev/null; then
    echo "✅ Services are running"
else
    echo "❌ Service health check failed"
    # Send alert
    echo "Service down at $(date)" >> health_alerts.log
fi

echo "🏥 Health checks completed"
```

### Backup Automation
```bash
#!/bin/bash
# backup_automation.sh

echo "💾 Starting automated backup..."

# Create timestamped backup
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="backup_${TIMESTAMP}.sql"

# Create backup
echo "Creating backup: $BACKUP_FILE"
tsk db backup "$BACKUP_FILE" --compress

# Verify backup
echo "Verifying backup..."
if [[ -f "$BACKUP_FILE.gz" ]]; then
    echo "✅ Backup created successfully: $BACKUP_FILE.gz"
    
    # Upload to remote storage (example)
    # scp "$BACKUP_FILE.gz" backup-server:/backups/
    
    # Clean old backups (keep last 30 days)
    find . -name "backup_*.sql.gz" -mtime +30 -delete
else
    echo "❌ Backup creation failed"
    exit 1
fi

echo "💾 Backup automation completed"
```

## Monitoring and Logging

### Performance Monitoring
```bash
#!/bin/bash
# performance_monitor.sh

echo "📊 Monitoring performance..."

# Monitor configuration loading time
echo "Testing configuration performance..."
time tsk config get server.port > /dev/null

# Monitor database performance
echo "Testing database performance..."
time tsk db console --command "SELECT COUNT(*) FROM users" > /dev/null

# Monitor binary compilation
echo "Testing binary compilation performance..."
time tsk peanuts compile peanu.peanuts > /dev/null

# Generate performance report
echo "📊 Performance Report:"
echo "Configuration loading: $(time tsk config get server.port 2>&1 | grep real)"
echo "Database query: $(time tsk db console --command 'SELECT 1' 2>&1 | grep real)"
echo "Binary compilation: $(time tsk peanuts compile peanu.peanuts 2>&1 | grep real)"
```

### Log Analysis
```bash
#!/bin/bash
# log_analyzer.sh

echo "📋 Analyzing logs..."

# Check for errors in recent logs
echo "Recent errors:"
grep -i "error\|failed\|exception" ~/.cache/tsk/tsk.log | tail -10

# Check for performance issues
echo "Performance warnings:"
grep -i "slow\|timeout\|performance" ~/.cache/tsk/tsk.log | tail -10

# Generate usage statistics
echo "Usage statistics:"
echo "Total commands run: $(wc -l < ~/.cache/tsk/tsk.log)"
echo "Errors in last 24h: $(grep -c "$(date +%Y-%m-%d)" ~/.cache/tsk/tsk.log)"
```

## Emergency Procedures

### Emergency Recovery
```bash
#!/bin/bash
# emergency_recovery.sh

echo "🚨 Emergency recovery procedure..."

# Stop all services
echo "Stopping all services..."
tsk services stop

# Check system status
echo "Checking system status..."
tsk db status --verbose
tsk config validate --verbose

# Attempt recovery
echo "Attempting recovery..."

# Restart database connection
echo "Restarting database connection..."
tsk db init --force

# Validate configuration
echo "Validating configuration..."
tsk config validate

# Restart services
echo "Restarting services..."
tsk services start

# Verify recovery
echo "Verifying recovery..."
tsk db status
tsk services status

echo "🚨 Emergency recovery completed"
```

### Disaster Recovery
```bash
#!/bin/bash
# disaster_recovery.sh

echo "🌪️  Disaster recovery procedure..."

# Check for latest backup
LATEST_BACKUP=$(ls -t backup_*.sql.gz 2>/dev/null | head -1)

if [[ -z "$LATEST_BACKUP" ]]; then
    echo "❌ No backup files found"
    exit 1
fi

echo "Using backup: $LATEST_BACKUP"

# Stop all services
tsk services stop

# Restore from backup
echo "Restoring from backup..."
tsk db restore "$LATEST_BACKUP" --force

# Restart services
echo "Restarting services..."
tsk services start

# Verify recovery
echo "Verifying recovery..."
tsk db status
tsk services status

echo "🌪️  Disaster recovery completed"
```

## Best Practices

### 1. Always Use Transactions
```bash
# Use transactions for data integrity
tsk db migrate schema.sql --transaction
```

### 2. Backup Before Changes
```bash
# Always backup before major changes
tsk db backup before-changes.sql
tsk db migrate schema.sql
```

### 3. Validate Configuration
```bash
# Validate before any operation
tsk config validate
```

### 4. Monitor Performance
```bash
# Regular performance monitoring
tsk test performance
```

### 5. Use JSON for Automation
```bash
# JSON output for script parsing
STATUS=$(tsk db status --json)
```

## Next Steps

These workflows provide a foundation for:
- [Framework Integrations](./integrations.md) - Specific tool integrations
- [Advanced Patterns](../troubleshooting.md) - Troubleshooting and optimization
- [Command Reference](../commands/README.md) - Detailed command documentation 