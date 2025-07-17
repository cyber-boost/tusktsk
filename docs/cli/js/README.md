# TuskLang JavaScript CLI Documentation

Welcome to the comprehensive CLI documentation for TuskLang JavaScript SDK.

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

This documentation is for TuskLang JavaScript SDK v2.0.0

## Installation

```bash
# Install TuskLang JavaScript SDK
npm install tusklang

# Install CLI globally
npm install -g tusklang

# Or use npx
npx tusklang --help
```

## Quick Start

```bash
# Check CLI version
tsk --version

# Get help
tsk --help

# Parse a TSK file
tsk parse config.tsk

# Start development server
tsk serve 3000

# Run tests
tsk test all
```

## JavaScript-Specific Features

- **Promise-based API** for async operations
- **TypeScript support** with full type definitions
- **ES Modules** and CommonJS compatibility
- **Node.js integration** with Express.js, Next.js, and more
- **File watching** for development workflows
- **Binary compilation** for production performance

## Examples

### Basic Configuration

```javascript
// Load configuration
const { PeanutConfig } = require('tusklang');
const config = PeanutConfig.load();

// Access values
const port = config.get('server.port', 3000);
const host = config.get('server.host', 'localhost');
```

### CLI Integration

```javascript
// Use CLI programmatically
const { execSync } = require('child_process');

// Compile configuration
execSync('tsk config compile ./config');

// Run tests
execSync('tsk test all');
```

### Development Workflow

```bash
# 1. Create configuration
echo '[app]' > peanu.peanuts
echo 'name: "My App"' >> peanu.peanuts

# 2. Validate syntax
tsk validate peanu.peanuts

# 3. Compile to binary
tsk config compile

# 4. Start development server
tsk serve 3000

# 5. Run tests
tsk test all
```

## Support

- **Documentation**: [TuskLang Docs](https://tusklang.org/docs)
- **GitHub**: [TuskLang Repository](https://github.com/tusklang/tusklang)
- **Issues**: [GitHub Issues](https://github.com/tusklang/tusklang/issues)
- **Community**: [Discord Server](https://discord.gg/tusklang) 