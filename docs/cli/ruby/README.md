# TuskLang Ruby CLI Documentation

Welcome to the comprehensive CLI documentation for TuskLang Ruby SDK.

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

This documentation is for TuskLang Ruby SDK v2.0.0

## Installation

```bash
# Install the gem
gem install tusk_lang

# Verify installation
tsk --version
```

## Quick Start

```bash
# Show help
tsk --help

# Check database status
tsk db status

# Start development server
tsk serve 3000

# Run all tests
tsk test all

# Get configuration value
tsk config get server.port
```

## Ruby Integration

The CLI integrates seamlessly with Ruby applications:

```ruby
# Programmatic usage
require 'tusk_lang'

# Load configuration
config = TuskLang::TSK.from_file('config.tsk')

# Execute FUJSEN functions
result = config.execute_fujsen('processing', 'transform', data)
```

## Features

- **Universal Commands**: All commands work identically across TuskLang SDKs
- **Ruby Idioms**: Follows Ruby conventions and best practices
- **Rails Integration**: Perfect for Rails applications
- **Jekyll Support**: Built-in Jekyll integration
- **Performance**: Binary compilation and caching
- **AI Integration**: Claude, ChatGPT, and custom AI APIs
- **Complete Testing**: Comprehensive test suite

## Getting Help

```bash
# General help
tsk --help

# Command-specific help
tsk help db
tsk help config
tsk help ai

# Version information
tsk --version
```

## Examples

See the [Examples](./examples/README.md) section for complete workflows and integration patterns. 