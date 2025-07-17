# tsk optimize

Optimize TuskLang files for better performance.

## Synopsis

```bash
tsk optimize [OPTIONS] <file>
```

## Description

The `tsk optimize` command optimizes TuskLang files for improved performance by removing unnecessary code, optimizing data structures, and applying performance enhancements.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --output | -o | Output file path | - |
| --level | -l | Optimization level (1-3) | 2 |
| --json | -j | Output in JSON format | false |
| --quiet | -q | Suppress output | false |
| --verbose | -v | Show verbose output | false |

## Examples

### Basic Optimization

```bash
# Optimize with default level
tsk optimize config.tsk

# Optimize with custom output
tsk optimize -o optimized.tsk config.tsk
```

### High-Level Optimization

```bash
# Maximum optimization
tsk optimize --level 3 app.tsk
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
    // Create optimizer
    optimizer := cli.NewOptimizer()
    
    // Optimize file
    err := optimizer.Optimize("app.tsk", &cli.OptimizeOptions{
        Output: "optimized.tsk",
        Level:  3,
    })
    if err != nil {
        log.Fatal(err)
    }
}
```

## Related Commands

- [tsk serve](./serve.md) - Start development server
- [tsk compile](./compile.md) - Compile files 