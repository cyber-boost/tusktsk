# tsk db init

Initialize SQLite database for development.

## Synopsis

```bash
tsk db init [OPTIONS]
```

## Description

The `tsk db init` command initializes a new SQLite database for development purposes. It creates the database file and sets up basic schema.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --database | -d | Database file path | myapp.db |
| --schema | -s | Initial schema file | - |
| --json | -j | Output in JSON format | false |
| --quiet | -q | Suppress output, only return exit code | false |
| --verbose | -v | Show verbose output | false |

## Examples

### Basic Initialization

```bash
# Initialize with default settings
tsk db init
```

**Output:**
```
🔄 Initializing SQLite database...
📍 Database file: myapp.db
📊 Schema: default
✅ Database initialized successfully
💾 File size: 28 KB
```

### Custom Database

```bash
# Initialize with custom database name
tsk db init --database development.db
```

### With Schema

```bash
# Initialize with custom schema
tsk db init --schema initial_schema.sql
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
    
    // Initialize database
    err := dbClient.Init(&cli.InitOptions{
        Database: "myapp.db",
        Schema:   "schema.sql",
    })
    if err != nil {
        log.Fatal(err)
    }
    
    fmt.Println("Database initialized successfully")
}
```

## Related Commands

- [tsk db status](./status.md) - Check database status
- [tsk db migrate](./migrate.md) - Run migrations
- [tsk db backup](./backup.md) - Create backup 