# TuskLang Go SDK

A comprehensive Go SDK for the TuskLang ecosystem, providing powerful tools for parsing, compiling, and executing TuskLang code with enterprise-grade features.

## Features

- **Advanced Parsing**: Sophisticated lexical analysis and AST generation
- **Binary Compilation**: Efficient compilation to binary format
- **Security Validation**: Built-in security scanning and validation
- **Performance Optimization**: High-performance execution engine
- **Comprehensive Error Handling**: Detailed error reporting and debugging
- **CLI Interface**: Full-featured command-line interface
- **Configuration Management**: Flexible configuration system
- **Operator System**: Extensible operator framework

## Installation

```bash
go get github.com/cyber-boost/tusktsk/sdk/go
```

## Quick Start

```go
package main

import (
    "fmt"
    "github.com/cyber-boost/tusktsk/sdk/go/pkg/core"
)

func main() {
    // Create SDK instance
    sdk := core.New()
    
    // Parse TuskLang code
    code := `app_name: "My App"
version: "1.0.0"
debug: true`
    
    result, err := sdk.Parse(code)
    if err != nil {
        fmt.Printf("Parse error: %v\n", err)
        return
    }
    
    fmt.Printf("Parsed %d tokens\n", len(result.Tokens))
    fmt.Printf("Generated %d AST nodes\n", len(result.AST))
}
```

## CLI Usage

The SDK includes a comprehensive command-line interface:

```bash
# Parse TuskLang code
tusktsk parse config.tsk

# Compile to binary
tusktsk compile script.tsk -o output.bin

# Execute code
tusktsk execute script.tsk

# Validate for security issues
tusktsk validate config.tsk

# Show version
tusktsk version
```

## Project Structure

```
├── cmd/                    # Command-line applications
│   ├── tsk/               # TSK CLI tool
│   └── peanut/            # Peanut CLI tool
├── pkg/                   # Public packages
│   ├── core/              # Core SDK functionality
│   ├── cli/               # CLI framework
│   ├── config/            # Configuration management
│   ├── security/          # Security features
│   ├── operators/         # Operator system
│   └── utils/             # Utility functions
├── internal/              # Internal packages
│   ├── parser/            # Parsing engine
│   ├── binary/            # Binary handling
│   └── error/             # Error handling
├── examples/              # Usage examples
├── docs/                  # Documentation
└── tests/                 # Test files
```

## Core Components

### Parser

The parser provides lexical analysis and AST generation:

```go
parser := parser.New()
result, err := parser.Parse(code)
```

### Binary Handler

Handles binary compilation and execution:

```go
binary := binary.New()
compileResult, err := binary.Compile(parseResult)
executeResult, err := binary.Execute(compileResult)
```

### Security Manager

Provides security validation and encryption:

```go
security := security.New()
validationResult := security.ValidateCode(code)
encrypted, err := security.Encrypt(data, key)
```

### Configuration

Flexible configuration management:

```go
config := config.New()
err := config.LoadFromFile("config.tsk")
value := config.GetString("app_name")
```

## Examples

### Basic Configuration Parsing

```go
package main

import (
    "fmt"
    "github.com/cyber-boost/tusktsk/sdk/go/pkg/core"
)

func main() {
    sdk := core.New()
    
    config := `
app_name: "My Application"
version: "1.0.0"
debug: true
port: 8080
database:
  host: "localhost"
  port: 5432
  name: "myapp"
`
    
    result, err := sdk.Parse(config)
    if err != nil {
        fmt.Printf("Error: %v\n", err)
        return
    }
    
    fmt.Printf("Configuration parsed successfully\n")
    fmt.Printf("Tokens: %d\n", len(result.Tokens))
    fmt.Printf("AST Nodes: %d\n", len(result.AST))
}
```

### Security Validation

```go
package main

import (
    "fmt"
    "github.com/cyber-boost/tusktsk/sdk/go/pkg/security"
)

func main() {
    security := security.New()
    
    code := `password: "secret123"
api_key: "sk-1234567890"`
    
    result := security.ValidateCode(code)
    
    if !result.Valid {
        fmt.Printf("Security issues found:\n")
        for _, issue := range result.Issues {
            fmt.Printf("- %s: %s\n", issue.Severity, issue.Message)
        }
    } else {
        fmt.Printf("Code passed security validation (Score: %d/100)\n", result.Score)
    }
}
```

### Custom Operators

```go
package main

import (
    "fmt"
    "github.com/cyber-boost/tusktsk/sdk/go/pkg/operators"
)

func main() {
    registry := operators.NewOperatorRegistry()
    
    // Register custom operator
    registry.RegisterOperator("++", operators.OperatorTypeUnary, 7, func(args ...interface{}) (interface{}, error) {
        if len(args) != 1 {
            return nil, fmt.Errorf("++ operator requires exactly 1 argument")
        }
        
        if val, ok := args[0].(int); ok {
            return val + 1, nil
        }
        
        return nil, fmt.Errorf("++ operator only supports integers")
    })
    
    // Use the operator
    if op, exists := registry.GetOperator("++"); exists {
        result, err := op.Function(5)
        if err != nil {
            fmt.Printf("Error: %v\n", err)
            return
        }
        fmt.Printf("5++ = %v\n", result)
    }
}
```

## Testing

Run the test suite:

```bash
go test ./...
```

Run tests with coverage:

```bash
go test -cover ./...
```

Run specific test:

```bash
go test ./internal/parser -v
```

## Building

Build the SDK:

```bash
go build -o tusktsk .
```

Build for specific platforms:

```bash
GOOS=linux GOARCH=amd64 go build -o tusktsk-linux-amd64 .
GOOS=darwin GOARCH=amd64 go build -o tusktsk-darwin-amd64 .
GOOS=windows GOARCH=amd64 go build -o tusktsk-windows-amd64.exe .
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Run the test suite
6. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For support and questions:

- Create an issue on GitHub
- Check the documentation in the `docs/` directory
- Review the examples in the `examples/` directory

## Version History

- **v1.0.0** - Initial release with core functionality
  - Basic parsing and AST generation
  - Binary compilation and execution
  - Security validation
  - CLI interface
  - Configuration management
  - Operator system

## Roadmap

- [ ] Advanced optimization passes
- [ ] JIT compilation
- [ ] WebAssembly support
- [ ] Plugin system
- [ ] IDE integration
- [ ] Performance profiling
- [ ] Distributed execution
- [ ] Cloud deployment tools 
## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Run the test suite
6. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For support and questions:

- Create an issue on GitHub
- Check the documentation in the `docs/` directory
- Review the examples in the `examples/` directory

## Version History

- **v1.0.0** - Initial release with core functionality
  - Basic parsing and AST generation
  - Binary compilation and execution
  - Security validation
  - CLI interface
  - Configuration management
  - Operator system

## Roadmap

- [ ] Advanced optimization passes
- [ ] JIT compilation
- [ ] WebAssembly support
- [ ] Plugin system
- [ ] IDE integration
- [ ] Performance profiling
- [ ] Distributed execution
- [ ] Cloud deployment tools 