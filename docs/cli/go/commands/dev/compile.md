# tsk compile

Compile TuskLang files to binary format for production.

## Synopsis

```bash
tsk compile [OPTIONS] <file>
```

## Description

The `tsk compile` command compiles TuskLang files (.tsk, .peanuts) to binary format for improved performance in production environments.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --output | -o | Output file path | - |
| --optimize | | Enable optimization | false |
| --format | -f | Output format (binary, json) | binary |
| --json | -j | Output in JSON format | false |
| --quiet | -q | Suppress output | false |
| --verbose | -v | Show verbose output | false |

## Examples

### Basic Compilation

```bash
# Compile to default output
tsk compile app.tsk

# Compile with custom output
tsk compile -o app.bin app.tsk
```

### Optimized Compilation

```bash
# Compile with optimization
tsk compile --optimize config.tsk
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
    // Create compiler
    compiler := cli.NewCompiler()
    
    // Compile file
    err := compiler.Compile("app.tsk", &cli.CompileOptions{
        Output:    "app.bin",
        Optimize:  true,
        Format:    "binary",
    })
    if err != nil {
        log.Fatal(err)
    }
}
```

## Related Commands

- [tsk serve](./serve.md) - Start development server
- [tsk optimize](./optimize.md) - Optimize files 