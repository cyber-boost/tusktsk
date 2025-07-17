# TuskLang Python CLI Documentation

Welcome to the comprehensive CLI documentation for TuskLang Python SDK.

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

This documentation is for TuskLang Python SDK v2.0.0

## Overview

The TuskLang Python CLI provides a comprehensive command-line interface for managing TuskLang applications, configurations, and development workflows. It includes powerful features like AI integration, binary compilation, and automated testing.

## Key Features

- **Database Management**: Connect, migrate, backup, and manage databases
- **Development Tools**: Serve applications, compile code, and optimize performance
- **Testing Framework**: Run comprehensive test suites with various options
- **Service Management**: Start, stop, and monitor application services
- **Cache Operations**: Manage caching layers including Memcached
- **Configuration Management**: Load, validate, and manage application configs
- **Binary Compilation**: Compile TuskLang files to optimized binary format
- **AI Integration**: Claude, ChatGPT, and custom AI API support
- **Utility Functions**: Parse, validate, and convert between formats

## Getting Started

1. **Install the CLI**:
   ```bash
   python setup_cli.py
   ```

2. **Verify Installation**:
   ```bash
   tsk version
   ```

3. **Get Help**:
   ```bash
   tsk help
   ```

4. **Interactive Mode**:
   ```bash
   tsk
   ```

## Python-Specific Features

- **Type Hints**: Full type annotation support
- **Async/Await**: Asynchronous command execution
- **Virtual Environments**: Automatic detection and activation
- **Package Management**: Integration with pip, poetry, and conda
- **IDE Integration**: Works with PyCharm, VS Code, and other IDEs

## Examples

### Basic Development Workflow

```bash
# Start development server
tsk serve 3000

# Run tests
tsk test all

# Check database status
tsk db status

# Validate configuration
tsk config validate
```

### AI-Powered Development

```bash
# Set up AI services
tsk ai setup

# Get code completion
tsk ai complete app.tsk 10 5

# Analyze code for issues
tsk ai analyze app.tsk

# Get optimization suggestions
tsk ai optimize app.tsk
```

### Configuration Management

```bash
# Get configuration value
tsk config get server.port

# Compile configuration
tsk config compile

# Generate documentation
tsk config docs
```

## Global Options

All commands support these global options:

```bash
--verbose, -v          # Enable verbose output
--quiet, -q            # Suppress non-error output
--json                 # Output in JSON format
--config <file>        # Use alternate config file
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success |
| 1 | General error |
| 2 | Configuration error |
| 3 | Database error |
| 4 | Service error |
| 5 | AI service error |

## Support

- **Documentation**: [TuskLang Docs](https://tusklang.org/docs)
- **Issues**: [GitHub Issues](https://github.com/tusklang/python-sdk/issues)
- **Community**: [TuskLang Discord](https://discord.gg/tusklang)

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details. 