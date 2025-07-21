# TuskLang Go SDK

A powerful Go SDK for the TuskLang ecosystem, providing comprehensive tools for parsing, compiling, and executing TuskLang code.

## Features

- **Advanced Parsing**: Sophisticated syntax analysis and AST generation
- **Binary Compilation**: Compile TuskLang code to binary format
- **Security Validation**: Built-in security checks and vulnerability detection
- **Performance Optimization**: High-performance code processing
- **Comprehensive Error Handling**: Detailed error reporting and recovery
- **CLI Interface**: Command-line tools for development and testing

## Installation

```bash
go get github.com/cyber-boost/tusktsk
```

## Quick Start

```go
package main

import (
    "fmt"
    "github.com/cyber-boost/tusktsk/pkg/core"
)

func main() {
    // Create SDK instance
    sdk := core.New()
    
    // Parse TuskLang code
    result, err := sdk.Parse("your tusk code here")
    if err != nil {
        fmt.Printf("Error: %v\n", err)
        return
    }
    
    fmt.Printf("Parsed %d tokens\n", len(result.Tokens))
}
```

## CLI Usage

```bash
# Parse TuskLang code
tusktsk parse yourfile.tsk

# Compile to binary
tusktsk compile yourfile.tsk

# Execute code
tusktsk execute yourfile.tsk

# Validate security
tusktsk validate yourfile.tsk
```

## Documentation

For detailed documentation, visit: https://tusklang.org/docs

## License

This project is licensed under the MIT License - see the [LICENSE-MIT](LICENSE-MIT) file for details.

## Contributing

Contributions are welcome! Please read our contributing guidelines before submitting pull requests.

## Support

- **Website**: https://tusklang.org
- **Email**: hello@tusklang.org
- **GitHub**: https://github.com/cyber-boost/tusktsk 