# 🐘 TuskLang Universal CLI Command Specification

## Overview

This document defines the universal command-line interface that ALL TuskLang SDK implementations must provide. Each language SDK should implement these commands following the same syntax and behavior to ensure consistency across the ecosystem.

**Tagline**: "Strong. Secure. Scalable."

## Command Structure

```
tsk [global-options] <command> [command-options] [arguments]
```

## Global Options

- `--help, -h` - Show help for any command
- `--version, -v` - Show version information
- `--verbose` - Enable verbose output
- `--quiet, -q` - Suppress non-error output
- `--config <path>` - Use alternate config file

## Core Commands Implementation

### 🗄️ Database Commands

```bash
tsk db status                    # Check database connection status
tsk db migrate <file>           # Run migration file
tsk db console                  # Open interactive database console
tsk db backup [file]            # Backup database (default: tusklang_backup_TIMESTAMP.sql)
tsk db restore <file>           # Restore from backup file
tsk db init                     # Initialize SQLite database
```

### 🔧 Development Commands

```bash
tsk serve [port]                # Start development server (default: 8080)
tsk compile <file>              # Compile .tsk file to optimized format
tsk optimize <file>             # Optimize .tsk file for production
```

### 🧪 Testing Commands

```bash
tsk test [suite]                # Run specific test suite
tsk test all                    # Run all test suites
tsk test parser                 # Test parser functionality only
tsk test fujsen                 # Test FUJSEN operators only
tsk test sdk                    # Test SDK-specific features
tsk test performance            # Run performance benchmarks
```

### ⚙️ Service Commands

```bash
tsk services start              # Start all TuskLang services
tsk services stop               # Stop all TuskLang services
tsk services restart            # Restart all services
tsk services status             # Show status of all services
```

### 📦 Project Commands

```bash
tsk init [project-name]         # Initialize new TuskLang project
tsk migrate --from=<format>     # Migrate from other formats (json|yaml|ini|env)
```

### 🏃 Cache Commands

```bash
tsk cache clear                 # Clear all caches
tsk cache status                # Show cache status and statistics
tsk cache warm                  # Pre-warm caches
tsk cache memcached status      # Check Memcached connection status
tsk cache memcached stats       # Show detailed Memcached statistics
tsk cache memcached flush       # Flush all Memcached data
tsk cache memcached restart     # Restart Memcached service
tsk cache memcached test        # Test Memcached connection
tsk cache distributed           # Show distributed cache status
```

### 🔐 License Commands

```bash
tsk license check               # Check current license status
tsk license activate <key>      # Activate license with key
```

### 🥜 Configuration Commands

```bash
tsk config get <key.path> [dir] # Get configuration value by path
tsk config check [path]         # Check configuration hierarchy
tsk config validate [path]      # Validate entire configuration chain
tsk config compile [path]       # Auto-compile all peanu.tsk files
tsk config docs [path]          # Generate configuration documentation
tsk config clear-cache [path]   # Clear configuration cache
tsk config stats                # Show configuration performance statistics
```

### 🚀 Binary Performance Commands

```bash
tsk binary compile <file.tsk>   # Compile to binary format (.tskb)
tsk binary execute <file.tskb>  # Execute binary file directly
tsk binary benchmark <file>     # Compare binary vs text performance
tsk binary optimize <file>      # Optimize binary for production
```

### 🥜 Peanuts Commands

```bash
tsk peanuts compile <file>      # Compile .peanuts to binary .pntb
tsk peanuts auto-compile [dir]  # Auto-compile all peanuts files in directory
tsk peanuts load <file.pntb>    # Load and display binary peanuts file
```

### 🎨 CSS Commands

```bash
tsk css expand <input> [output] # Expand CSS shortcodes in file
tsk css map                     # Show all shortcode → property mappings
```

### 🤖 AI Commands

```bash
tsk ai claude <prompt>          # Query Claude AI with prompt
tsk ai chatgpt <prompt>         # Query ChatGPT with prompt
tsk ai custom <api> <prompt>    # Query custom AI API endpoint
tsk ai config                   # Show current AI configuration
tsk ai setup                    # Interactive AI API key setup
tsk ai test                     # Test all configured AI connections
tsk ai complete <file> [line] [column]  # Get AI-powered auto-completion
tsk ai analyze <file>           # Analyze file for errors and improvements
tsk ai optimize <file>          # Get performance optimization suggestions
tsk ai security <file>          # Scan for security vulnerabilities
```

### 🛠️ Utility Commands

```bash
tsk parse <file>                # Parse and display TSK file contents
tsk validate <file>             # Validate TSK file syntax
tsk convert -i <input> -o <output>  # Convert between formats
tsk get <file> <key.path>       # Get specific value by key path
tsk set <file> <key.path> <value>   # Set value by key path
tsk version                     # Show version information
tsk help [command]              # Show help for command
```

## Implementation Requirements

### 1. Command Consistency

All SDKs must:
- Use identical command names and arguments
- Return consistent exit codes (0 = success, 1 = error)
- Provide similar output formatting
- Support the same global options

### 2. Error Handling

```
Exit Codes:
0 - Success
1 - General error
2 - Invalid arguments
3 - File not found
4 - Permission denied
5 - Connection error
6 - Configuration error
7 - License error
```

### 3. Output Formatting

- Use UTF-8 encoding
- Support `--json` flag for JSON output where applicable
- Use consistent status symbols: ✅ ❌ ⚠️ 🔄 📍
- Respect `--quiet` and `--verbose` flags

### 4. Configuration Loading

Commands should check for configuration in this order:
1. Command-line specified config (`--config`)
2. Current directory `peanu.pntb` or `peanu.tsk`
3. Parent directories (walking up)
4. User home directory `~/.tusklang/config.tsk`
5. System-wide `/etc/tusklang/config.tsk`

### 5. Plugin Architecture

SDKs should support command plugins:
```
~/.tusklang/plugins/
├── my-command/
│   ├── manifest.json
│   └── command.{ext}
```

### 6. Interactive Mode

When no command is provided, enter interactive REPL:
```
$ tsk
TuskLang v2.0.0 - Interactive Mode
tsk> db status
✅ Database connected
tsk> exit
```

## Language-Specific Implementation Notes

### Python
- Use `argparse` or `click` for command parsing
- Support `python -m tusklang` as alternative

### JavaScript/Node.js
- Use `commander.js` or `yargs` for command parsing
- Provide both `tsk` and `tusklang` commands

### Ruby
- Use `thor` or `optparse` for command parsing
- Follow Ruby conventions for subcommands

### PHP
- Extend current basic CLI with all commands
- Use Symfony Console component if needed

### Go
- Use `cobra` or standard `flag` package
- Compile to single binary

### Rust
- Use `clap` for command parsing
- Focus on performance and safety

### Java
- Use `picocli` or similar
- Support both CLI and programmatic usage

### C#
- Use `System.CommandLine` or similar
- Support .NET Core global tools

### Bash
- Implement core commands only
- Use functions for command organization

## Testing Requirements

Each SDK must include CLI tests that verify:
1. All commands are available
2. Help text is consistent
3. Exit codes are correct
4. Output formatting matches specification
5. Error messages are helpful

## Documentation Requirements

Each SDK must provide:
1. README with installation instructions
2. Command reference with examples
3. Migration guide from other tools
4. Troubleshooting guide

## Version Compatibility

- CLI version must match SDK version
- Backward compatibility for at least 2 major versions
- Clear deprecation warnings for removed features

---

This specification ensures that regardless of which TuskLang SDK a developer uses, they will have access to the same powerful CLI tools with consistent behavior across all platforms.