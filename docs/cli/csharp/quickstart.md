# Quick Start Guide for TuskLang C# CLI

Get up and running with TuskLang C# CLI in minutes. This guide covers the essential commands and workflows you'll use most often.

## Prerequisites

- TuskLang C# CLI installed (see [Installation Guide](./installation.md))
- Basic familiarity with command line
- .NET 6.0 or higher

## Your First TuskLang Project

### 1. Initialize a New Project

```bash
# Create a new directory
mkdir my-tusk-project
cd my-tusk-project

# Initialize TuskLang project
tsk init
```

This creates the basic project structure:
```
my-tusk-project/
├── peanu.peanuts          # Main configuration file
├── .tsk/                  # TuskLang cache directory
├── src/                   # Source code directory
└── tests/                 # Test files
```

### 2. Configure Your Project

Edit the `peanu.peanuts` file:

```ini
[app]
name: "My TuskLang App"
version: "1.0.0"

[server]
host: "localhost"
port: 8080
debug: true

[database]
driver: "sqlite"
path: "app.db"
```

### 3. Test Your Configuration

```bash
# Validate configuration
tsk config validate

# Get configuration values
tsk config get app.name
tsk config get server.port

# Check configuration status
tsk config stats
```

### 4. Run Your First Tests

```bash
# Run all tests
tsk test all

# Run specific test suites
tsk test parser
tsk test sdk
tsk test performance
```

## Essential Commands

### Configuration Management

```bash
# Get configuration value
tsk config get app.name

# Set configuration value
tsk config set app.version "1.1.0"

# Check configuration
tsk config check

# Compile to binary for performance
tsk config compile peanu.peanuts
```

### Development Workflow

```bash
# Start development server
tsk serve

# Compile TSK files
tsk compile src/main.tsk

# Optimize for production
tsk optimize src/
```

### Database Operations

```bash
# Check database status
tsk db status

# Run migrations
tsk db migrate

# Access database console
tsk db console

# Create backup
tsk db backup
```

### Testing

```bash
# Run all tests
tsk test all

# Run specific test
tsk test parser

# Run with verbose output
tsk test all --verbose

# Run tests and generate report
tsk test all --json > test-report.json
```

## Common Workflows

### Daily Development Workflow

```bash
# 1. Start your day
tsk config check
tsk db status

# 2. Make changes to configuration
# Edit peanu.peanuts file

# 3. Validate changes
tsk config validate

# 4. Test your changes
tsk test all

# 5. Start development server
tsk serve

# 6. Make code changes
# Edit your source files

# 7. Compile and test
tsk compile src/
tsk test sdk
```

### Production Deployment Workflow

```bash
# 1. Prepare for production
tsk config compile peanu.peanuts
tsk optimize src/

# 2. Run full test suite
tsk test all

# 3. Check database
tsk db status
tsk db backup

# 4. Deploy
tsk deploy --config production.pnt

# 5. Verify deployment
tsk services status
```

### Configuration Management Workflow

```bash
# 1. Create environment-specific configs
cp peanu.peanuts peanu.development.peanuts
cp peanu.peanuts peanu.production.peanuts

# 2. Edit environment configs
# Modify peanu.development.peanuts
# Modify peanu.production.peanuts

# 3. Compile all configs
tsk config compile peanu.development.peanuts
tsk config compile peanu.production.peanuts

# 4. Validate all configs
tsk config validate peanu.development.pnt
tsk config validate peanu.production.pnt
```

## C# Specific Features

### Programmatic Usage

```csharp
// Use CLI commands programmatically
using TuskLang.CLI;

// Execute commands
var result = await TskCommand.ExecuteAsync("version");
var config = await TskCommand.ExecuteAsync("config", "get", "app.name");

// Parse JSON output
var jsonResult = JsonSerializer.Deserialize<Dictionary<string, object>>(config);
```

### Integration with ASP.NET Core

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add TuskLang CLI services
builder.Services.AddTuskLangCli();

// Use in controllers
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

### Async/Await Support

```csharp
// All commands support async/await
public async Task DeployApplicationAsync()
{
    // Validate configuration
    await TskCommand.ExecuteAsync("config", "validate");
    
    // Run tests
    await TskCommand.ExecuteAsync("test", "all");
    
    // Deploy
    await TskCommand.ExecuteAsync("deploy", "--config", "production.pnt");
}
```

## Output Formats

### Human-Readable Output (Default)

```bash
tsk config get app.name
# Output: My TuskLang App

tsk test all
# Output:
# ✅ Parser tests passed (15/15)
# ✅ SDK tests passed (42/42)
# ✅ Performance tests passed (8/8)
# 
# All tests passed! 🎉
```

### JSON Output

```bash
tsk config get app.name --json
# Output: {"value": "My TuskLang App", "type": "string"}

tsk test all --json
# Output: {"total": 65, "passed": 65, "failed": 0, "duration": "2.3s"}
```

### Quiet Mode

```bash
tsk config get app.name --quiet
# Output: My TuskLang App

tsk test all --quiet
# Output: 65/65 tests passed
```

## Error Handling

### Common Errors and Solutions

1. **Configuration not found:**
   ```bash
   # Error: Configuration file not found
   # Solution: Run tsk init to create project structure
   tsk init
   ```

2. **Invalid configuration:**
   ```bash
   # Error: Invalid configuration syntax
   # Solution: Validate and fix configuration
   tsk config validate
   ```

3. **Database connection failed:**
   ```bash
   # Error: Database connection failed
   # Solution: Check database status and configuration
   tsk db status
   tsk config get database.connection_string
   ```

### Debug Mode

```bash
# Enable verbose logging
tsk --verbose config get app.name

# Debug specific command
tsk --verbose test all

# Get detailed error information
tsk config validate --verbose
```

## Performance Tips

### Optimize Configuration Loading

```bash
# Compile to binary for faster loading
tsk config compile peanu.peanuts

# Use binary config in production
tsk serve --config peanu.pnt
```

### Cache Management

```bash
# Clear cache if experiencing issues
tsk cache clear

# Check cache status
tsk cache status

# Warm cache for better performance
tsk cache warm
```

### Parallel Execution

```bash
# Run tests in parallel
tsk test all --parallel

# Compile multiple files in parallel
tsk compile src/ --parallel
```

## Next Steps

Now that you're familiar with the basics:

1. **Explore Command Reference** - Detailed documentation for each command
2. **Check out Examples** - Real-world usage patterns and workflows
3. **Learn Advanced Features** - File watching, custom serialization, etc.
4. **Join the Community** - Get help and share your experiences

## Quick Reference

### Most Used Commands

```bash
# Configuration
tsk config get <key>          # Get configuration value
tsk config set <key> <value>  # Set configuration value
tsk config validate           # Validate configuration
tsk config compile <file>     # Compile to binary

# Development
tsk serve                     # Start development server
tsk compile <file>            # Compile TSK file
tsk optimize <dir>            # Optimize for production

# Testing
tsk test all                  # Run all tests
tsk test parser               # Run parser tests
tsk test sdk                  # Run SDK tests

# Database
tsk db status                 # Check database status
tsk db migrate                # Run migrations
tsk db backup                 # Create backup

# Utilities
tsk version                   # Show version
tsk help                      # Show help
tsk --json <command>          # JSON output
tsk --verbose <command>       # Verbose logging
```

### Global Options

```bash
--json, -j     # Output in JSON format
--quiet, -q    # Suppress output
--verbose, -v  # Enable verbose logging
--help, -h     # Show help
```

---

You're now ready to start building with TuskLang C# CLI! The system is designed to be intuitive and powerful, with comprehensive error handling and helpful output to guide you through any issues. 