# tsk peanuts compile

Compile .peanuts or .tsk files to binary .pnt format.

## Synopsis

```bash
tsk peanuts compile [OPTIONS] <file>
```

## Description

The `tsk peanuts compile` command compiles human-readable configuration files (.peanuts, .tsk) to high-performance binary format (.pnt) for production use.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --output | -o | Output file path | - |
| --optimize | | Enable optimization | false |
| --json | -j | Output in JSON format | false |
| --quiet | -q | Suppress output | false |
| --verbose | -v | Show verbose output | false |

## Examples

### Basic Compilation

```bash
# Compile peanuts file
tsk peanuts compile config.peanuts

# Compile TuskLang file
tsk peanuts compile app.tsk
```

### With Custom Output

```bash
# Specify output file
tsk peanuts compile -o config.pnt config.peanuts
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
    
    // Compile to binary
    err = config.CompileToBinary("config.peanuts", "config.pnt")
    if err != nil {
        log.Fatal(err)
    }
}
```

## Related Commands

- [tsk peanuts load](./load.md) - Load binary file
- [tsk config compile](../config/compile.md) - Compile configuration 