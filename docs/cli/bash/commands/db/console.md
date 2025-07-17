# tsk db console

Open interactive database console for direct database access.

## Synopsis

```bash
tsk db console [OPTIONS] [--command <sql>]
```

## Description

The `tsk db console` command opens an interactive database console, allowing you to execute SQL commands directly against your database. It provides a convenient way to explore data, test queries, and perform administrative tasks.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --command | -c | Execute single SQL command and exit | - |
| --file | -f | Execute SQL commands from file | - |
| --output | -o | Output format (table, csv, json) | table |
| --timeout | -t | Command timeout in seconds | 30 |

## Examples

### Interactive Console
```bash
# Open interactive database console
tsk db console
```

Expected output:
```
🔌 Connected to database: myapp
📊 Database: PostgreSQL 13.4
📍 Host: localhost:5432
💡 Type 'help' for available commands, 'quit' to exit

myapp=>
```

### Execute Single Command
```bash
# Execute a single SQL command
tsk db console --command "SELECT COUNT(*) FROM users"
```

Expected output:
```
🔌 Connected to database: myapp
📍 Executing: SELECT COUNT(*) FROM users
┌─────────┐
│ count   │
├─────────┤
│ 42      │
└─────────┘
✅ Command completed
```

### Execute from File
```bash
# Execute SQL commands from file
tsk db console --file queries.sql
```

### JSON Output
```bash
# Get results in JSON format
tsk db console --command "SELECT * FROM users LIMIT 3" --output json
```

Expected output:
```json
{
  "results": [
    {
      "id": 1,
      "name": "John Doe",
      "email": "john@example.com"
    },
    {
      "id": 2,
      "name": "Jane Smith",
      "email": "jane@example.com"
    }
  ],
  "count": 2
}
```

## Interactive Commands

When in the console, you can use these special commands:

- `help` - Show available commands
- `tables` - List all tables
- `describe <table>` - Show table structure
- `quit` or `exit` - Exit console
- `clear` - Clear screen
- `history` - Show command history

## Bash API Usage

```bash
#!/bin/bash
# db_query.sh

# Execute a query and capture result
user_count=$(tsk db console --command "SELECT COUNT(*) FROM users" --output json | jq -r '.results[0].count')

echo "Total users: $user_count"
```

### Database Health Check
```bash
#!/bin/bash
# health_check.sh

# Check database tables
echo "Checking database tables..."
tsk db console --command "\dt" --output table

# Check database size
echo "Database size:"
tsk db console --command "SELECT pg_size_pretty(pg_database_size(current_database()))" --output table
```

## Related Commands

- [tsk db status](./status.md) - Check database status
- [tsk db migrate](./migrate.md) - Run migrations
- [tsk db backup](./backup.md) - Backup database
- [tsk db restore](./restore.md) - Restore database 