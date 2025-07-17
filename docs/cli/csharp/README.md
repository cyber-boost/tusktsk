# TuskLang C# CLI Documentation

Welcome to the comprehensive CLI documentation for TuskLang C# SDK.

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

This documentation is for TuskLang C# SDK v2.0.0

## Overview

The TuskLang C# CLI provides a comprehensive set of tools for working with TuskLang projects, configuration management, testing, and development workflows. Built on .NET 6+ and using System.CommandLine, it offers a modern, type-safe command-line experience.

## Key Features

- **Type-safe commands** with strong typing and validation
- **Async/await support** throughout the entire CLI
- **JSON output** for machine-readable responses
- **Verbose logging** for debugging and troubleshooting
- **Cross-platform** support (Windows, macOS, Linux)
- **Dependency injection** integration
- **Configuration management** with hierarchical loading
- **Performance optimization** tools

## Installation

```bash
# Using dotnet CLI
dotnet tool install -g TuskLang.CLI

# Using NuGet
dotnet add package TuskLang.CLI
```

## Quick Start

```bash
# Check installation
tsk version

# Initialize a new project
tsk init

# Load configuration
tsk config get app.name

# Run tests
tsk test all

# Start development server
tsk serve
```

## Global Options

All commands support these global options:

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--json` | -j | Output in JSON format | false |
| `--quiet` | -q | Suppress output | false |
| `--verbose` | -v | Enable verbose logging | false |
| `--help` | -h | Show help | - |

## C# Specific Features

### Strongly Typed Commands

All commands use C# types and provide compile-time safety:

```csharp
// Programmatic usage
var result = await TskCommand.ExecuteAsync(new[] { "version" });
var config = await TskCommand.ExecuteAsync(new[] { "config", "get", "app.name" });
```

### Async/Await Support

Every command operation is async:

```csharp
// Async command execution
var exitCode = await Program.MainAsync(args);
```

### Dependency Injection

Commands integrate with .NET DI container:

```csharp
// Register CLI services
services.AddTuskLangCli();
```

## Examples

### Basic Workflow

```bash
# 1. Initialize project
tsk init

# 2. Configure settings
tsk config set app.name "My C# App"
tsk config set server.port 8080

# 3. Run tests
tsk test all

# 4. Start development
tsk serve
```

### Advanced Usage

```bash
# Compile configuration to binary
tsk config compile peanu.peanuts

# Run performance benchmarks
tsk binary benchmark

# Analyze code with AI
tsk ai analyze src/

# Deploy with custom config
tsk deploy --config production.pnt
```

## Integration with .NET Projects

### ASP.NET Core Integration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add TuskLang CLI services
builder.Services.AddTuskLangCli();

// Use CLI commands in controllers
[ApiController]
public class DeployController : ControllerBase
{
    private readonly ITskCommandService _tsk;

    public DeployController(ITskCommandService tsk)
    {
        _tsk = tsk;
    }

    [HttpPost("deploy")]
    public async Task<IActionResult> Deploy()
    {
        var result = await _tsk.ExecuteAsync("deploy", "--config", "production.pnt");
        return Ok(result);
    }
}
```

### Console Application Integration

```csharp
// Program.cs
class Program
{
    static async Task<int> Main(string[] args)
    {
        // Use TuskLang CLI commands
        if (args.Length > 0 && args[0] == "config")
        {
            return await TskCommand.ExecuteAsync(args);
        }

        // Your application logic
        Console.WriteLine("My C# Application");
        return 0;
    }
}
```

## Performance

The C# CLI is optimized for performance:

- **Fast startup** with minimal overhead
- **Efficient command parsing** using System.CommandLine
- **Async I/O** for all file and network operations
- **Memory-efficient** with proper disposal patterns
- **Caching** for frequently accessed data

## Troubleshooting

### Common Issues

1. **Command not found**: Ensure TuskLang.CLI is installed globally
2. **Permission errors**: Run with appropriate permissions
3. **Configuration issues**: Check `tsk config validate`

### Debug Mode

```bash
# Enable verbose logging
tsk --verbose version

# Debug specific command
tsk --verbose config get app.name
```

### Getting Help

```bash
# General help
tsk help

# Command-specific help
tsk config help
tsk db help

# List all commands
tsk --help
```

## Contributing

The CLI is built with extensibility in mind:

```csharp
// Custom command
public class CustomCommand : Command
{
    public CustomCommand() : base("custom", "Custom command")
    {
        this.SetHandler(ExecuteAsync);
    }

    private async Task<int> ExecuteAsync()
    {
        // Your custom logic
        return 0;
    }
}
```

## Support

- **Documentation**: This guide and command-specific docs
- **Examples**: See [examples](./examples/README.md) directory
- **Issues**: Report bugs and feature requests
- **Community**: Join the TuskLang community

---

This CLI provides everything you need to work with TuskLang projects efficiently in C#. Each command is designed to be both powerful and easy to use, with comprehensive error handling and helpful output. 