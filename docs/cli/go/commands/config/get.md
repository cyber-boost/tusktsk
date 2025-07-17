# tsk config get

Get configuration value using dot notation path.

## Synopsis

```bash
tsk config get [OPTIONS] <key.path> [dir]
```

## Description

The `tsk config get` command retrieves configuration values from the hierarchical configuration system using dot notation paths.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --default | -d | Default value if key not found | - |
| --type | -t | Expected value type | auto |
| --json | -j | Output in JSON format | false |
| --quiet | -q | Suppress output | false |
| --verbose | -v | Show verbose output | false |

## Examples

### Basic Usage

```bash
# Get server port
tsk config get server.port

# Get nested configuration
tsk config get database.postgres.host
```

### With Default Value

```bash
# Get with default fallback
tsk config get server.port --default 3000
```

## Go API Usage

```go
package main

import (
    "fmt"
    "log"
    "github.com/tusklang/go-sdk/peanut"
)

func main() {
    // Load configuration
    config, err := peanut.Load(".")
    if err != nil {
        log.Fatal(err)
    }
    
    // Get configuration value
    port := config.GetInt("server.port", 3000)
    fmt.Printf("Server port: %d\n", port)
}
```

## Related Commands

- [tsk config check](./check.md) - Check configuration
- [tsk config validate](./validate.md) - Validate configuration 