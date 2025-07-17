# Basic Usage Examples

Common usage patterns for TuskLang Ruby SDK.

## Configuration Management

```bash
# Get configuration value
tsk config get server.port

# Check configuration
tsk config check

# Validate configuration
tsk config validate
```

## Database Operations

```bash
# Initialize database
tsk db init

# Check status
tsk db status

# Run migration
tsk db migrate schema.sql
```

## Development Workflow

```bash
# Start server
tsk serve 3000

# Run tests
tsk test all

# Compile configuration
tsk config compile
```
