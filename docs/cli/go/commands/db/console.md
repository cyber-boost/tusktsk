# tsk db console

Interactive database console for executing SQL queries and managing database operations.

## Synopsis

```bash
tsk db console [OPTIONS]
```

## Description

The `tsk db console` command provides an interactive SQL console for direct database access. It supports SQL query execution, result formatting, and basic database administration tasks.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --file | -f | Execute SQL from file | - |
| --query | -q | Execute single query and exit | - |
| --output | -o | Output format (table, csv, json) | table |
| --timeout | -t | Query timeout in seconds | 30 |
| --verbose | -v | Show verbose output | false |

## Examples

### Interactive Console

```bash
# Start interactive console
tsk db console
```

**Output:**
```
🔌 Connected to database: myapp
📍 Host: localhost:5432
👤 User: postgres
📊 Database: myapp

SQL> SELECT * FROM users LIMIT 5;
+----+----------+------------------+---------------------+
| id | name     | email            | created_at          |
+----+----------+------------------+---------------------+
| 1  | John Doe | john@example.com | 2024-12-19 10:00:00 |
| 2  | Jane Doe | jane@example.com | 2024-12-19 10:01:00 |
+----+----------+------------------+---------------------+
2 rows in set (0.002s)

SQL> DESCRIBE users;
+------------+--------------+------+-----+---------+----------------+
| Field      | Type         | Null | Key | Default | Extra          |
+------------+--------------+------+-----+---------+----------------+
| id         | serial       | NO   | PRI | NULL    | auto_increment |
| name       | varchar(255) | NO   |     | NULL    |                |
| email      | varchar(255) | NO   | UNI | NULL    |                |
| created_at | timestamp    | YES  |     | now()   |                |
+------------+--------------+------+-----+---------+----------------+

SQL> \q
👋 Disconnected from database
```

### Execute File

```bash
# Execute SQL from file
tsk db console --file queries.sql
```

### Single Query

```bash
# Execute single query
tsk db console --query "SELECT COUNT(*) FROM users"
```

**Output:**
```
+----------+
| count    |
+----------+
| 42       |
+----------+
1 row in set (0.001s)
```

### JSON Output

```bash
# Get results in JSON format
tsk db console --query "SELECT * FROM users LIMIT 3" --output json
```

**Output:**
```json
{
  "query": "SELECT * FROM users LIMIT 3",
  "duration_ms": 1,
  "rows_affected": 3,
  "results": [
    {
      "id": 1,
      "name": "John Doe",
      "email": "john@example.com",
      "created_at": "2024-12-19T10:00:00Z"
    },
    {
      "id": 2,
      "name": "Jane Doe",
      "email": "jane@example.com",
      "created_at": "2024-12-19T10:01:00Z"
    },
    {
      "id": 3,
      "name": "Bob Smith",
      "email": "bob@example.com",
      "created_at": "2024-12-19T10:02:00Z"
    }
  ]
}
```

## Go API Usage

```go
package main

import (
    "fmt"
    "log"
    "github.com/tusklang/go-sdk/cli"
)

func main() {
    // Create database client
    dbClient := cli.NewDatabaseClient()
    
    // Execute query
    result, err := dbClient.Query("SELECT COUNT(*) FROM users")
    if err != nil {
        log.Fatal(err)
    }
    
    // Print results
    fmt.Printf("Query executed in %v\n", result.Duration)
    fmt.Printf("Rows returned: %d\n", len(result.Rows))
    
    for _, row := range result.Rows {
        fmt.Printf("%+v\n", row)
    }
}
```

## Console Commands

### Built-in Commands

| Command | Description |
|---------|-------------|
| `\q` or `\quit` | Exit console |
| `\h` or `\help` | Show help |
| `\t` or `\tables` | List tables |
| `\d <table>` | Describe table |
| `\l` or `\list` | List databases |
| `\u` or `\users` | List users |
| `\c <db>` | Connect to database |
| `\s` or `\status` | Show status |
| `\clear` | Clear screen |
| `\history` | Show command history |

### Examples

```sql
-- List all tables
SQL> \t
+------------------+
| Tables           |
+------------------+
| users            |
| posts            |
| comments         |
| categories       |
+------------------+

-- Describe table structure
SQL> \d users
+------------+--------------+------+-----+---------+----------------+
| Field      | Type         | Null | Key | Default | Extra          |
+------------+--------------+------+-----+---------+----------------+
| id         | serial       | NO   | PRI | NULL    | auto_increment |
| name       | varchar(255) | NO   |     | NULL    |                |
| email      | varchar(255) | NO   | UNI | NULL    |                |
| created_at | timestamp    | YES  |     | now()   |                |
+------------+--------------+------+-----+---------+----------------+

-- Show database status
SQL> \s
+------------------+------------------+
| Property         | Value            |
+------------------+------------------+
| Server version   | PostgreSQL 14.5  |
| Protocol version | 3                |
| Connection ID    | 12345            |
| Current database | myapp            |
| Current user     | postgres         |
| SSL connection   | No               |
+------------------+------------------+
```

## Output Formats

### Table Format (Default)

```
+----+----------+------------------+
| id | name     | email            |
+----+----------+------------------+
| 1  | John Doe | john@example.com |
| 2  | Jane Doe | jane@example.com |
+----+----------+------------------+
```

### CSV Format

```bash
tsk db console --query "SELECT * FROM users" --output csv
```

**Output:**
```csv
id,name,email
1,John Doe,john@example.com
2,Jane Doe,jane@example.com
```

### JSON Format

```bash
tsk db console --query "SELECT * FROM users" --output json
```

**Output:**
```json
{
  "results": [
    {"id": 1, "name": "John Doe", "email": "john@example.com"},
    {"id": 2, "name": "Jane Doe", "email": "jane@example.com"}
  ]
}
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success - Console exited normally |
| 1 | Error - Database connection failed |
| 2 | Error - Query execution failed |
| 3 | Error - Invalid command |

## Related Commands

- [tsk db status](./status.md) - Check database status
- [tsk db migrate](./migrate.md) - Run migrations
- [tsk db backup](./backup.md) - Create backup
- [tsk db restore](./restore.md) - Restore from backup

## Notes

- **Connection**: Uses configuration from `peanu.peanuts`
- **History**: Command history is saved between sessions
- **Timeout**: Queries timeout after 30 seconds by default
- **Transactions**: Use `BEGIN`, `COMMIT`, `ROLLBACK` for transactions
- **Security**: Be careful with destructive operations

## See Also

- [Database Commands Overview](./README.md)
- [Configuration Guide](../../../go/docs/PNT_GUIDE.md)
- [Examples](../../examples/database-console.md) 