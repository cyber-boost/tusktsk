# TuskLang Bash CLI Documentation

Welcome to the comprehensive CLI documentation for TuskLang Bash SDK.

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

This documentation is for TuskLang Bash SDK v2.0.0

## Overview

The TuskLang Bash CLI provides a comprehensive command-line interface for managing TuskLang projects, configurations, and development workflows. Built with POSIX-compliant Bash, it offers cross-platform compatibility and integrates seamlessly with shell scripts and automation tools.

## Key Features

- **POSIX Compliance**: Works across all Unix-like systems
- **Hierarchical Configuration**: CSS-like cascading configuration system
- **Binary Performance**: 85% faster configuration loading with .pnt format
- **Modular Design**: Organized command structure for easy navigation
- **JSON Output**: Machine-readable output for automation
- **Error Handling**: Comprehensive error reporting and recovery

## Quick Examples

```bash
# Check database status
tsk db status

# Start development server
tsk serve 3000

# Run all tests
tsk test all

# Get configuration value
tsk config get server.port

# Compile to binary format
tsk binary compile app.tsk

# Query AI for help
tsk ai claude "Explain TuskLang syntax"
```

## Installation

See [Installation Guide](./installation.md) for detailed setup instructions.

## Getting Started

See [Quick Start Guide](./quickstart.md) for your first steps with the CLI.

## Support

- **Documentation**: [TuskLang Docs](https://tusklang.org/docs)
- **GitHub**: [TuskLang Repository](https://github.com/tusklang/tusklang-bash)
- **Issues**: [GitHub Issues](https://github.com/tusklang/tusklang-bash/issues) 