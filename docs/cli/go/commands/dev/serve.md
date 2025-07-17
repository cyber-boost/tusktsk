# tsk serve

Start development server for local development and testing.

## Synopsis

```bash
tsk serve [OPTIONS] [port]
```

## Description

The `tsk serve` command starts a development server for local development and testing. It provides hot reloading, debugging capabilities, and development-specific features.

## Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| --help | -h | Show help for this command | - |
| --config | -c | Configuration file path | peanu.peanuts |
| --hot-reload | -r | Enable hot reloading | false |
| --host | | Server host address | localhost |
| --timeout | -t | Request timeout in seconds | 30 |
| --json | -j | Output in JSON format | false |
| --quiet | -q | Suppress output | false |
| --verbose | -v | Show verbose output | false |

## Examples

### Basic Server

```bash
# Start server on default port
tsk serve

# Start server on specific port
tsk serve 8080
```

### Hot Reloading

```bash
# Start server with hot reloading
tsk serve --hot-reload 3000
```

### Custom Configuration

```bash
# Start server with custom config
tsk serve --config dev.peanuts 9000
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
    // Create server client
    server := cli.NewServer()
    
    // Start server
    err := server.Start(&cli.ServerOptions{
        Port:      8080,
        Host:      "localhost",
        HotReload: true,
    })
    if err != nil {
        log.Fatal(err)
    }
}
```

## Related Commands

- [tsk compile](./compile.md) - Compile TuskLang files
- [tsk optimize](./optimize.md) - Optimize files 