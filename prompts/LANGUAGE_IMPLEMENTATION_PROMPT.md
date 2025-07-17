# TuskLang CLI Implementation Task - [LANGUAGE_NAME]

## Task Overview

You are tasked with implementing the complete TuskLang CLI command interface for the **[LANGUAGE_NAME]** SDK. This implementation must follow the Universal CLI Command Specification exactly to ensure consistency across all TuskLang SDKs.

## Current State

The [LANGUAGE_NAME] SDK currently has:
- ✅ Basic TSK parser implementation
- ✅ PeanutConfig with .pntb binary support but extension is suppose to be .pnt not .pntb (please update everywhere you find .pntb)
- ⚠️ Limited CLI with only basic commands
- ❌ Missing advanced CLI commands

## Your Mission

Implement ALL commands listed in the TuskLang Universal CLI Command Specification for [LANGUAGE_NAME]. The implementation must be identical in functionality to the PHP reference implementation.

## Implementation Requirements

### 1. File Structure
```
[LANGUAGE_SDK_PATH]/
├── cli/
│   ├── main.[ext]           # Main CLI entry point
│   ├── commands/            # Command implementations
│   │   ├── db.[ext]         # Database commands
│   │   ├── dev.[ext]        # Development commands
│   │   ├── test.[ext]       # Testing commands
│   │   ├── service.[ext]    # Service commands
│   │   ├── cache.[ext]      # Cache commands
│   │   ├── config.[ext]     # Configuration commands
│   │   ├── ai.[ext]         # AI commands
│   │   ├── binary.[ext]     # Binary commands
│   │   └── ...
│   └── utils/               # Shared utilities
```

### 2. Commands to Implement

You must implement ALL of these command categories:

#### 🗄️ Database Commands (Priority: HIGH)
- `tsk db status` - Check database connection
- `tsk db migrate <file>` - Run migration file
- `tsk db console` - Interactive database console
- `tsk db backup [file]` - Backup database
- `tsk db restore <file>` - Restore from backup
- `tsk db init` - Initialize SQLite database

#### 🔧 Development Commands (Priority: HIGH)
- `tsk serve [port]` - Start development server
- `tsk compile <file>` - Compile .tsk file
- `tsk optimize <file>` - Optimize .tsk file

#### 🧪 Testing Commands (Priority: HIGH)
- `tsk test [suite]` - Run test suites
- `tsk test all` - Run all tests
- `tsk test parser` - Test parser only
- `tsk test fujsen` - Test FUJSEN only
- `tsk test sdk` - Test SDK only
- `tsk test performance` - Performance tests

#### ⚙️ Service Commands (Priority: MEDIUM)
- `tsk services start` - Start all services
- `tsk services stop` - Stop all services
- `tsk services restart` - Restart services
- `tsk services status` - Show service status

#### 📦 Cache Commands (Priority: MEDIUM)
- `tsk cache clear` - Clear cache
- `tsk cache status` - Show cache status
- `tsk cache warm` - Warm cache
- `tsk cache memcached [subcommand]` - Memcached operations

#### 🥜 Configuration Commands (Priority: HIGH)
- `tsk config get <key.path> [dir]` - Get config value
- `tsk config check [path]` - Check hierarchy
- `tsk config validate [path]` - Validate config
- `tsk config compile [path]` - Compile configs
- `tsk config docs [path]` - Generate docs
- `tsk config clear-cache [path]` - Clear cache
- `tsk config stats` - Show statistics

#### 🚀 Binary Performance Commands (Priority: HIGH)
- `tsk binary compile <file.tsk>` - Compile to .tskb
- `tsk binary execute <file.tskb>` - Execute binary
- `tsk binary benchmark <file>` - Benchmark performance
- `tsk binary optimize <file>` - Optimize binary

#### 🤖 AI Commands (Priority: LOW)
- `tsk ai claude <prompt>` - Query Claude
- `tsk ai chatgpt <prompt>` - Query ChatGPT
- `tsk ai analyze <file>` - Analyze code
- `tsk ai optimize <file>` - Optimization suggestions
- `tsk ai security <file>` - Security scan

#### 🛠️ Utility Commands (Priority: HIGH)
- `tsk parse <file>` - Parse TSK file
- `tsk validate <file>` - Validate syntax
- `tsk convert -i <in> -o <out>` - Convert formats
- `tsk get <file> <key.path>` - Get value
- `tsk set <file> <key.path> <val>` - Set value

### 3. Language-Specific Guidelines for [LANGUAGE_NAME]

[INSERT LANGUAGE-SPECIFIC GUIDELINES HERE - e.g., for Python use argparse, for Node.js use commander, etc.]

### 4. Integration Requirements

- Use existing TSK parser implementation
- Use existing PeanutConfig for configuration management
- Maintain backward compatibility with current CLI
- Follow [LANGUAGE_NAME] best practices and idioms

### 5. Output Requirements

All commands must:
- Use UTF-8 encoding
- Support `--json` flag for JSON output
- Use these status symbols: ✅ ❌ ⚠️ 🔄 📍
- Respect `--quiet` and `--verbose` flags
- Return proper exit codes (0=success, 1=error, etc.)

### 6. Testing Requirements

Create comprehensive tests for:
- Each command with various arguments
- Error handling and edge cases
- Output formatting consistency
- Exit code correctness

### 7. Documentation Requirements

Update or create:
- README.md with all commands documented
- Command reference with examples
- Installation instructions
- Troubleshooting guide

## Reference Implementation

The PHP implementation at `/opt/tsk_git/sdk-pnt-test/php/` serves as the reference, though it currently has limited commands. Use the CLI_COMMAND_SPECIFICATION.md as your primary guide.

## Success Criteria

Your implementation is complete when:
1. ✅ All commands listed above are implemented
2. ✅ Commands behave identically to the specification
3. ✅ All tests pass
4. ✅ Documentation is complete
5. ✅ Code follows [LANGUAGE_NAME] best practices

## Example Usage

After implementation, users should be able to:

```bash
# Database operations
tsk db status
tsk db migrate schema.sql

# Development
tsk serve 3000
tsk compile config.tsk

# Testing
tsk test all
tsk test performance

# Configuration
tsk config get server.port
tsk config compile ./

# Binary operations
tsk binary compile app.tsk
tsk binary benchmark app.tsk

# And all other commands...
```

## Notes

- Start with high-priority commands first
- Reuse existing code where possible
- Ask for clarification if specifications are unclear
- Focus on [LANGUAGE_NAME]-idiomatic implementation
- Ensure cross-platform compatibility

Remember: The goal is to provide a consistent, powerful CLI experience across all TuskLang SDKs while leveraging the strengths of [LANGUAGE_NAME].

---

**IMPORTANT**: Replace [LANGUAGE_NAME] and [LANGUAGE_SDK_PATH] with the specific language and path before using this prompt.