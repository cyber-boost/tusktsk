# TuskLang Go CLI Documentation

Welcome to the comprehensive CLI documentation for TuskLang Go SDK.

## Quick Links

- [Installation](./installation.md)
- [Quick Start](./quickstart.md)
- [Command Reference](./commands/README.md)
- [Examples](./examples/README.md)
- [Troubleshooting](./troubleshooting.md)

## Command Categories

- [Database Commands](./commands/db/README.md) - Database operations
- [Development Commands](./commands/dev/README.md) - Development tools
- [Testing Commands](./commands/test/README.md) - Test execution
- [Service Commands](./commands/services/README.md) - Service management
- [Cache Commands](./commands/cache/README.md) - Cache operations
- [Configuration Commands](./commands/config/README.md) - Config management
- [Binary Commands](./commands/binary/README.md) - Binary compilation
- [Peanuts Commands](./commands/peanuts/README.md) - Peanut config
- [AI Commands](./commands/ai/README.md) - AI integrations
- [CSS Commands](./commands/css/README.md) - CSS utilities
- [License Commands](./commands/license/README.md) - License management
- [Utility Commands](./commands/utility/README.md) - General utilities

## Version

This documentation is for TuskLang Go SDK v2.0.0

## Installation

### Prerequisites

- Go 1.21 or higher
- Git

### Quick Install

```bash
# Clone the repository
git clone https://github.com/tusklang/go-sdk.git
cd go-sdk

# Install dependencies
go mod tidy

# Build and install CLI
make install
```

### Verify Installation

```bash
tsk version
```

## Basic Usage

```bash
# Check CLI version
tsk version

# Get help
tsk help

# Check database status
tsk db status

# Start development server
tsk serve 3000

# Run tests
tsk test all

# Compile configuration
tsk config compile .
```

## Go-Specific Features

The Go CLI provides several language-specific features:

- **Struct Mapping**: Direct mapping to Go structs
- **Type Safety**: Compile-time type checking
- **Concurrency**: Thread-safe configuration access
- **Performance**: Optimized binary format support
- **Context Support**: Context-aware operations

## Examples

### Web Application

```bash
# Start development server
tsk serve 8080

# Run tests
tsk test all

# Compile configuration to binary
tsk peanuts compile peanu.peanuts

# Check database connection
tsk db status
```

### Microservice

```bash
# Start services
tsk services start

# Check service status
tsk services status

# Monitor cache
tsk cache status

# Validate configuration
tsk config validate .
```

## Troubleshooting

### Common Issues

1. **Command not found**: Ensure `tsk` is in your PATH
2. **Permission denied**: Run with appropriate permissions
3. **Configuration errors**: Check file syntax and permissions

### Getting Help

```bash
# General help
tsk help

# Command-specific help
tsk db help
tsk config help

# Verbose output
tsk --verbose db status
```

## Contributing

To contribute to the Go CLI:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/tusklang/go-sdk/blob/main/LICENSE) file for details. 