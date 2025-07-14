# TuskLang CLI Overview

The TuskLang command-line interface (CLI) provides powerful tools for development, testing, and deployment. This guide covers all CLI commands and their usage.

## Basic Usage

```bash
tusk [command] [options] [arguments]
```

### Global Options

```bash
-h, --help              Show help for any command
-v, --version           Display version information
--verbose              Enable verbose output
--debug                Enable debug mode
--quiet                Suppress non-error output
--no-color             Disable colored output
--config <path>        Use specific config file
--env <environment>    Set environment (dev, staging, prod)
```

## Core Commands

### run - Execute TuskLang Files

Execute a TuskLang file:

```bash
# Basic execution
tusk run app.tsk

# With arguments
tusk run script.tsk --arg1 value1 --arg2 value2

# With environment
tusk run app.tsk --env production

# Watch mode (auto-reload on changes)
tusk run app.tsk --watch

# Dry run (parse without executing)
tusk run app.tsk --dry-run
```

Options:
- `--watch, -w` - Watch for file changes
- `--env, -e` - Set environment
- `--vars` - Pass variables as JSON
- `--timeout` - Set execution timeout
- `--dry-run` - Parse without executing

### serve - Start HTTP Server

Run a TuskLang file as an HTTP server:

```bash
# Start server
tusk serve server.tsk

# Custom port
tusk serve server.tsk --port 3000

# With HTTPS
tusk serve server.tsk --ssl --cert server.crt --key server.key

# Development mode with hot reload
tusk serve server.tsk --dev

# Production mode
tusk serve server.tsk --env production --workers 4
```

Options:
- `--port, -p` - Port number (default: 8080)
- `--host, -h` - Host address (default: localhost)
- `--ssl` - Enable HTTPS
- `--cert` - SSL certificate file
- `--key` - SSL key file
- `--dev` - Development mode
- `--workers` - Number of worker processes

### repl - Interactive Shell

Start an interactive TuskLang session:

```bash
# Start REPL
tusk repl

# With preloaded file
tusk repl --load config.tsk

# With specific environment
tusk repl --env development

# With history file
tusk repl --history ~/.tusk_history
```

REPL Commands:
- `.help` - Show REPL commands
- `.exit` - Exit REPL
- `.clear` - Clear screen
- `.load <file>` - Load a file
- `.save <file>` - Save session
- `.vars` - Show variables
- `.env` - Show environment

### init - Initialize Project

Create a new TuskLang project:

```bash
# Interactive initialization
tusk init

# With template
tusk init --template webapp

# In specific directory
tusk init myproject

# Non-interactive with options
tusk init --name "MyApp" --author "John Doe" --license MIT
```

Templates:
- `minimal` - Bare minimum setup
- `webapp` - Web application
- `api` - REST API
- `cli` - Command-line tool
- `library` - Reusable library

### compile - Compile to Binary

Compile TuskLang files to standalone executables:

```bash
# Basic compilation
tusk compile app.tsk

# Specify output
tusk compile app.tsk -o myapp

# Cross-compilation
tusk compile app.tsk --target linux-arm64

# Optimization levels
tusk compile app.tsk --optimize 3

# Include assets
tusk compile app.tsk --assets ./public
```

Targets:
- `linux-amd64`
- `linux-arm64`
- `darwin-amd64` (macOS Intel)
- `darwin-arm64` (macOS M1)
- `windows-amd64`
- `windows-arm64`

## Development Commands

### test - Run Tests

Execute test files:

```bash
# Run all tests
tusk test

# Specific test file
tusk test user.test.tsk

# Test directory
tusk test ./tests

# With coverage
tusk test --coverage

# Watch mode
tusk test --watch

# Parallel execution
tusk test --parallel 4
```

Options:
- `--coverage` - Generate coverage report
- `--watch` - Re-run on changes
- `--fail-fast` - Stop on first failure
- `--parallel` - Number of parallel tests
- `--filter` - Run specific test patterns

### check - Validate Syntax

Check TuskLang files for syntax errors:

```bash
# Check single file
tusk check app.tsk

# Check directory
tusk check ./src

# With auto-fix
tusk check app.tsk --fix

# Strict mode
tusk check app.tsk --strict

# Output format
tusk check app.tsk --format json
```

### format - Code Formatting

Format TuskLang files:

```bash
# Format file
tusk format app.tsk

# Format directory
tusk format ./src

# Check formatting without changes
tusk format app.tsk --check

# Custom style
tusk format app.tsk --style compact

# Write changes
tusk format app.tsk --write
```

Styles:
- `standard` - Default style
- `compact` - Minimal whitespace
- `expanded` - Maximum readability

### lint - Code Quality

Analyze code quality and patterns:

```bash
# Lint file
tusk lint app.tsk

# With specific rules
tusk lint app.tsk --rules strict

# Ignore patterns
tusk lint ./src --ignore "*.test.tsk"

# Custom config
tusk lint --config .tusklint.tsk

# Fix issues
tusk lint app.tsk --fix
```

## Package Management

### install - Install Dependencies

Install project dependencies:

```bash
# Install from tusk.config.tsk
tusk install

# Install specific package
tusk install tusklang/http

# Install with version
tusk install tusklang/http@1.2.0

# Dev dependency
tusk install --dev tusklang/test

# Global installation
tusk install -g tusklang/cli-tools
```

### publish - Publish Package

Publish a package to the registry:

```bash
# Publish current package
tusk publish

# Dry run
tusk publish --dry-run

# With tag
tusk publish --tag beta

# Skip tests
tusk publish --skip-tests
```

### search - Search Packages

Search the package registry:

```bash
# Search by name
tusk search http

# Search with filters
tusk search database --author tusklang

# Show details
tusk search redis --detail
```

## Utility Commands

### docs - Generate Documentation

Generate documentation from TuskLang files:

```bash
# Generate docs
tusk docs

# Specify output
tusk docs --output ./documentation

# Format options
tusk docs --format html
tusk docs --format markdown

# Include private items
tusk docs --private
```

### config - Configuration Management

Manage TuskLang configuration:

```bash
# Show current config
tusk config show

# Get specific value
tusk config get author

# Set value
tusk config set author "John Doe"

# Edit config
tusk config edit

# Reset to defaults
tusk config reset
```

### cache - Cache Management

Manage TuskLang cache:

```bash
# Show cache info
tusk cache info

# Clear all cache
tusk cache clear

# Clear specific cache
tusk cache clear --type compiled

# Set cache directory
tusk cache set-dir /tmp/tusk-cache
```

### plugin - Plugin Management

Manage TuskLang plugins:

```bash
# List plugins
tusk plugin list

# Install plugin
tusk plugin install auth-jwt

# Remove plugin
tusk plugin remove auth-jwt

# Update plugins
tusk plugin update

# Create plugin
tusk plugin create my-plugin
```

## Advanced Commands

### debug - Debugging Tools

Debug TuskLang applications:

```bash
# Start debugger
tusk debug app.tsk

# Set breakpoint
tusk debug app.tsk --break 10

# Remote debugging
tusk debug --remote :9229

# Memory profiling
tusk debug app.tsk --profile memory
```

### benchmark - Performance Testing

Benchmark TuskLang code:

```bash
# Run benchmark
tusk benchmark app.tsk

# Specific functions
tusk benchmark app.tsk --function processData

# Compare versions
tusk benchmark --compare old.tsk new.tsk

# Output format
tusk benchmark app.tsk --format csv
```

### migrate - Data Migration

Run data migrations:

```bash
# Run pending migrations
tusk migrate up

# Rollback last migration
tusk migrate down

# Create migration
tusk migrate create add_users_table

# Check status
tusk migrate status
```

## Environment Variables

TuskLang CLI respects these environment variables:

```bash
TUSK_HOME          # TuskLang installation directory
TUSK_CONFIG        # Default config file path
TUSK_ENV           # Default environment
TUSK_DEBUG         # Enable debug mode
TUSK_NO_COLOR      # Disable colors
TUSK_CACHE_DIR     # Cache directory
TUSK_LOG_LEVEL     # Log level (debug, info, warn, error)
```

## Configuration Files

### .tuskrc

Global configuration in home directory:

```tusk
# ~/.tuskrc
defaults:
    author: "Your Name"
    license: "MIT"
    environment: "development"

aliases:
    dev: "run --watch --env development"
    prod: "serve --env production --workers 4"
    t: "test"
```

### Project Configuration

Project-specific configuration:

```tusk
# .tusk/config.tsk
cli:
    aliases:
        start: "serve main.tsk --dev"
        build: "compile main.tsk -o dist/app"
        deploy: "run deploy.tsk --env production"
```

## Command Combinations

### Development Workflow

```bash
# Start development server with watching and debugging
tusk serve app.tsk --dev --watch --debug

# Run tests in watch mode with coverage
tusk test --watch --coverage --parallel 4

# Format, lint, and check before commit
tusk format . --write && tusk lint --fix && tusk check
```

### Production Deployment

```bash
# Build optimized binary
tusk compile app.tsk -o app --optimize 3 --target linux-amd64

# Run production server
tusk serve app.tsk --env production --workers auto --port 80
```

### CI/CD Pipeline

```bash
# CI testing script
tusk check --strict && \
tusk lint && \
tusk test --coverage --fail-fast && \
tusk compile --target multi
```

## Shell Completion

### Bash

```bash
# Add to ~/.bashrc
eval "$(tusk completion bash)"
```

### Zsh

```bash
# Add to ~/.zshrc
eval "$(tusk completion zsh)"
```

### Fish

```bash
# Add to ~/.config/fish/config.fish
tusk completion fish | source
```

### PowerShell

```powershell
# Add to $PROFILE
tusk completion powershell | Out-String | Invoke-Expression
```

## Troubleshooting CLI

### Common Issues

**Command not found:**
```bash
# Check PATH
echo $PATH
which tusk

# Add to PATH if missing
export PATH="/usr/local/bin:$PATH"
```

**Permission denied:**
```bash
# Check file permissions
ls -la $(which tusk)

# Fix permissions
chmod +x $(which tusk)
```

**Version mismatch:**
```bash
# Check version
tusk --version

# Update TuskLang
tusk self-update
```

### Debug Output

```bash
# Maximum verbosity
tusk --debug --verbose run app.tsk

# Log to file
tusk run app.tsk --debug 2> debug.log

# Trace execution
TUSK_TRACE=1 tusk run app.tsk
```

## Best Practices

1. **Use aliases** for common commands
2. **Enable shell completion** for efficiency
3. **Use watch mode** during development
4. **Run checks** before committing
5. **Document custom scripts** in tusk.config.tsk
6. **Use environment variables** for configuration
7. **Leverage parallel execution** for tests
8. **Profile performance** regularly

## Next Steps

- Start with [Basic Syntax](011-comments.md)
- Learn about [Key-Value Basics](012-key-value-basics.md)
- Master [@ Operators](031-at-operator-intro.md)
- Explore [Advanced Features](050-at-http-host.md)