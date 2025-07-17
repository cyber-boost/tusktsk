# tsk peanuts load

Load and display binary peanuts file contents.

## Synopsis

```bash
tsk peanuts load [OPTIONS] <file.pnt>
```

## Description

The `tsk peanuts load` command loads and displays the contents of binary peanuts files (.pnt) in various formats for inspection and debugging.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --format | -f | Output format (table, json, yaml) | table |
| --key | -k | Show specific key only | - |
| --json | -j | Output in JSON format | false |
| --quiet | -q | Suppress output | false |
| --verbose | -v | Show verbose output | false |

## Examples

### Basic Loading

```bash
# Load and display binary file
tsk peanuts load config.pnt

# Load with JSON output
tsk peanuts load --format json config.pnt
```

### Specific Key

```bash
# Show specific configuration key
tsk peanuts load --key server.port config.pnt
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
    // Load from binary file
    config, err := peanut.LoadFromBinary("config.pnt")
    if err != nil {
        log.Fatal(err)
    }
    
    // Access configuration
    port := config.GetInt("server.port", 3000)
    fmt.Printf("Server port: %d\n", port)
}
```

## Related Commands

- [tsk peanuts compile](./compile.md) - Compile to binary
- [tsk config get](../config/get.md) - Get configuration value 