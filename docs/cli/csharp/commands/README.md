# Command Reference

This section provides detailed documentation for all TuskLang C# CLI commands, organized by category.

## Command Categories

### [Database Commands](./db/README.md)
Database operations and management commands.

| Command | Description |
|---------|-------------|
| [tsk db status](./db/status.md) | Check database connection and status |
| [tsk db migrate](./db/migrate.md) | Run database migrations |
| [tsk db console](./db/console.md) | Access database console |
| [tsk db backup](./db/backup.md) | Create database backup |
| [tsk db restore](./db/restore.md) | Restore database from backup |
| [tsk db init](./db/init.md) | Initialize database schema |

### [Development Commands](./dev/README.md)
Development and build tools.

| Command | Description |
|---------|-------------|
| [tsk serve](./dev/serve.md) | Start development server |
| [tsk compile](./dev/compile.md) | Compile TSK files |
| [tsk optimize](./dev/optimize.md) | Optimize code for production |

### [Testing Commands](./test/README.md)
Test execution and management.

| Command | Description |
|---------|-------------|
| [tsk test all](./test/all.md) | Run all test suites |
| [tsk test parser](./test/parser.md) | Run parser tests |
| [tsk test fujsen](./test/fujsen.md) | Run FUJSEN tests |
| [tsk test sdk](./test/sdk.md) | Run SDK tests |
| [tsk test performance](./test/performance.md) | Run performance tests |

### [Configuration Commands](./config/README.md)
Configuration management and validation.

| Command | Description |
|---------|-------------|
| [tsk config get](./config/get.md) | Get configuration value |
| [tsk config check](./config/check.md) | Check configuration status |
| [tsk config validate](./config/validate.md) | Validate configuration |
| [tsk config compile](./config/compile.md) | Compile configuration to binary |
| [tsk config docs](./config/docs.md) | Generate configuration documentation |
| [tsk config clear-cache](./config/clear-cache.md) | Clear configuration cache |
| [tsk config stats](./config/stats.md) | Show configuration statistics |

### [Binary Commands](./binary/README.md)
Binary compilation and execution.

| Command | Description |
|---------|-------------|
| [tsk binary compile](./binary/compile.md) | Compile to binary format |
| [tsk binary execute](./binary/execute.md) | Execute binary files |
| [tsk binary benchmark](./binary/benchmark.md) | Benchmark binary performance |
| [tsk binary optimize](./binary/optimize.md) | Optimize binary files |

### [Peanuts Commands](./peanuts/README.md)
Peanut configuration management.

| Command | Description |
|---------|-------------|
| [tsk peanuts compile](./peanuts/compile.md) | Compile peanut configuration |
| [tsk peanuts auto-compile](./peanuts/auto-compile.md) | Auto-compile peanut files |
| [tsk peanuts load](./peanuts/load.md) | Load peanut configuration |

### [Service Commands](./services/README.md)
Service management and control.

| Command | Description |
|---------|-------------|
| [tsk services start](./services/start.md) | Start TuskLang services |
| [tsk services stop](./services/stop.md) | Stop TuskLang services |
| [tsk services restart](./services/restart.md) | Restart TuskLang services |
| [tsk services status](./services/status.md) | Check service status |

### [Cache Commands](./cache/README.md)
Cache management and optimization.

| Command | Description |
|---------|-------------|
| [tsk cache clear](./cache/clear.md) | Clear all caches |
| [tsk cache status](./cache/status.md) | Check cache status |
| [tsk cache warm](./cache/warm.md) | Warm up caches |
| [tsk cache memcached status](./cache/memcached/status.md) | Check Memcached status |
| [tsk cache memcached stats](./cache/memcached/stats.md) | Show Memcached statistics |
| [tsk cache memcached flush](./cache/memcached/flush.md) | Flush Memcached cache |
| [tsk cache memcached restart](./cache/memcached/restart.md) | Restart Memcached service |
| [tsk cache memcached test](./cache/memcached/test.md) | Test Memcached connection |

### [AI Commands](./ai/README.md)
AI integration and analysis.

| Command | Description |
|---------|-------------|
| [tsk ai claude](./ai/claude.md) | Query Claude AI |
| [tsk ai chatgpt](./ai/chatgpt.md) | Query ChatGPT |
| [tsk ai custom](./ai/custom.md) | Query custom AI service |
| [tsk ai config](./ai/config.md) | Configure AI settings |
| [tsk ai setup](./ai/setup.md) | Setup AI integration |
| [tsk ai test](./ai/test.md) | Test AI connections |
| [tsk ai complete](./ai/complete.md) | Code completion |
| [tsk ai analyze](./ai/analyze.md) | Analyze code with AI |
| [tsk ai optimize](./ai/optimize.md) | Optimize code with AI |
| [tsk ai security](./ai/security.md) | Security analysis |

### [CSS Commands](./css/README.md)
CSS utilities and tools.

| Command | Description |
|---------|-------------|
| [tsk css expand](./css/expand.md) | Expand CSS shortcodes |
| [tsk css map](./css/map.md) | Show shortcode mappings |

### [License Commands](./license/README.md)
License management and validation.

| Command | Description |
|---------|-------------|
| [tsk license check](./license/check.md) | Check license status |
| [tsk license activate](./license/activate.md) | Activate license |

### [Utility Commands](./utility/README.md)
General utility commands.

| Command | Description |
|---------|-------------|
| [tsk parse](./utility/parse.md) | Parse TSK files |
| [tsk validate](./utility/validate.md) | Validate files |
| [tsk convert](./utility/convert.md) | Convert between formats |
| [tsk get](./utility/get.md) | Get key-value pairs |
| [tsk set](./utility/set.md) | Set key-value pairs |
| [tsk version](./utility/version.md) | Show version information |
| [tsk help](./utility/help.md) | Show help information |

## Global Options

All commands support these global options:

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--json` | -j | Output in JSON format | false |
| `--quiet` | -q | Suppress output | false |
| `--verbose` | -v | Enable verbose logging | false |
| `--help` | -h | Show help | - |

## Command Structure

Commands follow a hierarchical structure:

```
tsk [category] [subcommand] [options] [arguments]
```

Examples:
```bash
tsk config get app.name
tsk db status --json
tsk test all --verbose
tsk --quiet version
```

## Exit Codes

All commands use consistent exit codes:

| Code | Description |
|------|-------------|
| 0 | Success |
| 1 | General error |
| 2 | Invalid arguments |
| 3 | Configuration error |
| 4 | Database error |
| 5 | Network error |
| 6 | Permission error |

## C# Specific Features

### Strongly Typed Commands

All commands are implemented with C# types and provide compile-time safety:

```csharp
// Command execution with type safety
var result = await TskCommand.ExecuteAsync("config", "get", "app.name");
var exitCode = await TskCommand.ExecuteAsync("test", "all");
```

### Async/Await Support

Every command operation is async:

```csharp
// Async command execution
public async Task<int> RunTestsAsync()
{
    return await TskCommand.ExecuteAsync("test", "all");
}
```

### Dependency Injection

Commands integrate with .NET DI container:

```csharp
// Register CLI services
services.AddTuskLangCli();

// Use in services
public class MyService
{
    private readonly ITskCommandService _tsk;

    public MyService(ITskCommandService tsk)
    {
        _tsk = tsk;
    }

    public async Task<string> GetConfigAsync(string key)
    {
        return await _tsk.ExecuteAsync("config", "get", key);
    }
}
```

## Output Formats

### Human-Readable Output (Default)

Commands provide user-friendly output with status symbols:

```bash
tsk test all
# Output:
# ✅ Parser tests passed (15/15)
# ✅ SDK tests passed (42/42)
# ✅ Performance tests passed (8/8)
# 
# All tests passed! 🎉
```

### JSON Output

Use `--json` flag for machine-readable output:

```bash
tsk test all --json
# Output: {"total": 65, "passed": 65, "failed": 0, "duration": "2.3s"}
```

### Quiet Mode

Use `--quiet` flag to suppress output:

```bash
tsk version --quiet
# Output: 2.0.0
```

## Error Handling

### Consistent Error Format

All commands provide consistent error messages:

```bash
tsk config get invalid.key
# Error: Configuration key 'invalid.key' not found
# 
# Available keys:
# - app.name
# - app.version
# - server.host
```

### Verbose Error Information

Use `--verbose` for detailed error information:

```bash
tsk config get invalid.key --verbose
# Error: Configuration key 'invalid.key' not found
# 
# Stack trace:
# at TuskLang.CLI.Commands.ConfigGetCommand.ExecuteAsync()
# at TuskLang.CLI.Program.MainAsync()
# 
# Configuration loaded from: /path/to/peanu.peanuts
# Available sections: app, server, database
```

## Performance Considerations

### Command Optimization

- **Fast startup**: Commands load quickly with minimal overhead
- **Efficient parsing**: Uses System.CommandLine for optimal argument parsing
- **Async I/O**: All file and network operations are async
- **Memory management**: Proper disposal patterns for resource cleanup

### Caching

Commands use intelligent caching:

```bash
# Configuration is cached after first load
tsk config get app.name  # Loads and caches
tsk config get app.name  # Uses cache

# Clear cache when needed
tsk cache clear
```

## Examples

### Basic Usage

```bash
# Get configuration value
tsk config get app.name

# Run tests
tsk test all

# Check database status
tsk db status

# Start development server
tsk serve
```

### Advanced Usage

```bash
# JSON output for scripting
tsk config get app.name --json | jq '.value'

# Verbose logging for debugging
tsk test all --verbose

# Quiet mode for automation
tsk version --quiet > version.txt
```

### Programmatic Usage

```csharp
// Execute commands programmatically
var config = await TskCommand.ExecuteAsync("config", "get", "app.name");
var testResult = await TskCommand.ExecuteAsync("test", "all", "--json");

// Parse JSON output
var result = JsonSerializer.Deserialize<TestResult>(testResult);
```

## Getting Help

### Command Help

```bash
# General help
tsk help

# Category help
tsk config help

# Command help
tsk config get --help
```

### Interactive Help

```bash
# Interactive command completion
tsk [TAB]  # Shows available categories
tsk config [TAB]  # Shows available config commands
```

---

This command reference provides comprehensive documentation for all TuskLang C# CLI commands. Each command is designed to be both powerful and easy to use, with consistent behavior and helpful output. 