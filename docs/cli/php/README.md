# TuskLang PHP CLI Documentation

Welcome to the comprehensive CLI documentation for TuskLang PHP SDK.

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

This documentation is for TuskLang PHP SDK v2.0.0

## Overview

The TuskLang PHP CLI provides a comprehensive command-line interface for working with TuskLang configurations, databases, services, and more. All commands follow the Universal CLI Command Specification, ensuring consistency across all TuskLang SDKs.

## Features

- **Universal Compliance** - Matches the TuskLang CLI specification exactly
- **PHP Integration** - Seamless integration with existing PHP SDK components
- **Comprehensive Commands** - 50+ commands across 12 categories
- **Robust Error Handling** - Proper exit codes and meaningful error messages
- **Flexible Output** - Support for both human-readable and JSON output
- **Interactive Mode** - REPL for interactive configuration management

## Installation

```bash
# Via Composer
composer require tusklang/tusklang

# Or install the CLI globally
curl -sSL tusklang.org/tsk.sh | sudo bash
```

## Quick Start

```bash
# Check system status
tsk status

# Parse a configuration file
tsk parse config.tsk

# Get a specific configuration value
tsk config get server.port

# Start development server
tsk serve 3000

# Run tests
tsk test all
```

## PHP-Specific Notes

- **Autoloading** - The CLI automatically finds and loads the TuskLang PHP SDK
- **Error Handling** - All commands use PHP exceptions with proper exit codes
- **Performance** - Optimized for PHP's execution model
- **Compatibility** - Works with PHP 8.1+ and all major frameworks

## Getting Help

```bash
# General help
tsk help

# Command-specific help
tsk db --help
tsk config get --help

# Version information
tsk version
```

## Examples

See the [Examples](./examples/README.md) section for complete usage examples and workflows.

## Support

For issues and questions:
- [GitHub Issues](https://github.com/tuskphp/tusklang/issues)
- [Documentation](https://docs.tusklang.org)
- [Community](https://community.tusklang.org)

**Strong. Secure. Scalable.** 🐘 