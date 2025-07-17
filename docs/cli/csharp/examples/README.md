# Examples

This section provides practical examples and real-world usage patterns for the TuskLang C# CLI.

## Example Categories

### [Basic Usage](./basic-usage.md)
Common patterns and everyday commands for getting started with TuskLang C# CLI.

### [Workflows](./workflows.md)
Complete workflows for development, testing, and deployment scenarios.

### [Integrations](./integrations.md)
Examples of integrating TuskLang C# CLI with popular frameworks and tools.

## Quick Examples

### Configuration Management

```bash
# Initialize a new project
tsk init

# Set up basic configuration
cat > peanu.peanuts << EOF
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
EOF

# Validate configuration
tsk config validate

# Get configuration values
tsk config get app.name
tsk config get server.port

# Compile to binary for performance
tsk config compile peanu.peanuts
```

### Development Workflow

```bash
# Start development server
tsk serve

# In another terminal, run tests
tsk test all

# Compile TSK files
tsk compile src/main.tsk

# Check database status
tsk db status

# Run migrations
tsk db migrate
```

### Testing

```bash
# Run all tests
tsk test all

# Run specific test suites
tsk test parser
tsk test sdk
tsk test performance

# Run tests with verbose output
tsk test all --verbose

# Generate test report
tsk test all --json > test-report.json
```

### Database Operations

```bash
# Check database status
tsk db status

# Initialize database
tsk db init

# Run migrations
tsk db migrate

# Create backup
tsk db backup

# Access database console
tsk db console
```

### AI Integration

```bash
# Query Claude AI
tsk ai claude "Explain TuskLang configuration"

# Analyze code with AI
tsk ai analyze src/

# Get code completion
tsk ai complete "function processData"

# Security analysis
tsk ai security src/
```

## C# Integration Examples

### ASP.NET Core Integration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add TuskLang CLI services
builder.Services.AddTuskLangCli();

var app = builder.Build();

// Use in controllers
[ApiController]
[Route("[controller]")]
public class ConfigController : ControllerBase
{
    private readonly ITskCommandService _tsk;

    public ConfigController(ITskCommandService tsk)
    {
        _tsk = tsk;
    }

    [HttpGet("app-name")]
    public async Task<IActionResult> GetAppName()
    {
        var appName = await _tsk.ExecuteAsync("config", "get", "app.name");
        return Ok(new { name = appName });
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
        if (args.Length > 0 && args[0] == "config")
        {
            // Delegate to TuskLang CLI
            return await TskCommand.ExecuteAsync(args);
        }

        // Your application logic
        var config = new PeanutConfig();
        var appName = await config.GetAsync<string>("app.name");
        
        Console.WriteLine($"Starting {appName}");
        return 0;
    }
}
```

### Unit Testing Integration

```csharp
// TestHelper.cs
public static class TestHelper
{
    public static async Task<string> GetTestConfigAsync(string key)
    {
        return await TskCommand.ExecuteAsync("config", "get", key, "--config", "test.pnt");
    }

    public static async Task<bool> RunTestsAsync()
    {
        var result = await TskCommand.ExecuteAsync("test", "all", "--json");
        var testResult = JsonSerializer.Deserialize<TestResult>(result);
        return testResult.Passed == testResult.Total;
    }
}

// MyTests.cs
[TestClass]
public class MyTests
{
    [TestMethod]
    public async Task TestConfiguration()
    {
        var appName = await TestHelper.GetTestConfigAsync("app.name");
        Assert.AreEqual("Test App", appName);
    }

    [TestMethod]
    public async Task TestAllTestsPass()
    {
        var allPassed = await TestHelper.RunTestsAsync();
        Assert.IsTrue(allPassed);
    }
}
```

## Real-World Scenarios

### Web Application Development

```bash
# 1. Initialize project
mkdir my-web-app
cd my-web-app
tsk init

# 2. Configure for development
cat > peanu.development.peanuts << EOF
[app]
name: "My Web App"
version: "1.0.0"

[server]
host: "localhost"
port: 3000
debug: true

[database]
driver: "sqlite"
path: "dev.db"
EOF

# 3. Configure for production
cat > peanu.production.peanuts << EOF
[app]
name: "My Web App"
version: "1.0.0"

[server]
host: "0.0.0.0"
port: 8080
debug: false

[database]
driver: "postgresql"
connection_string: "postgresql://user:pass@localhost/prod"
EOF

# 4. Compile configurations
tsk config compile peanu.development.peanuts
tsk config compile peanu.production.peanuts

# 5. Run tests
tsk test all

# 6. Start development server
tsk serve --config peanu.development.pnt
```

### Microservice Development

```bash
# 1. Create service structure
mkdir user-service
cd user-service
tsk init

# 2. Configure service
cat > peanu.peanuts << EOF
[service]
name: "User Service"
version: "2.1.0"

[server]
host: "0.0.0.0"
port: 3001

[database]
driver: "postgresql"
connection_string: "postgresql://user:pass@localhost/users"

[redis]
host: "localhost"
port: 6379
db: 0

[logging]
level: "info"
format: "json"
EOF

# 3. Initialize database
tsk db init
tsk db migrate

# 4. Run service tests
tsk test all

# 5. Start service
tsk serve --config peanu.pnt
```

### CI/CD Pipeline Integration

```yaml
# .github/workflows/deploy.yml
name: Deploy

on:
  push:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '6.0.x'
      
      - name: Install TuskLang CLI
        run: dotnet tool install -g TuskLang.CLI
      
      - name: Validate configuration
        run: tsk config validate
      
      - name: Run tests
        run: tsk test all
      
      - name: Compile configuration
        run: tsk config compile peanu.peanuts
      
      - name: Deploy
        run: tsk deploy --config peanu.pnt
```

## Performance Examples

### Configuration Optimization

```bash
# Benchmark configuration loading
tsk config benchmark

# Compare text vs binary loading
time tsk config get app.name --config peanu.peanuts
time tsk config get app.name --config peanu.pnt

# Warm up cache
tsk cache warm

# Monitor performance
tsk config stats
```

### Database Optimization

```bash
# Check database performance
tsk db status --verbose

# Optimize database
tsk db optimize

# Monitor database metrics
tsk db stats
```

## Security Examples

### Configuration Security

```bash
# Validate configuration security
tsk config validate --security

# Check for sensitive data
tsk config audit

# Encrypt sensitive values
tsk config encrypt database.password
```

### AI Security Analysis

```bash
# Security scan with AI
tsk ai security src/

# Vulnerability analysis
tsk ai analyze --security src/

# Code review with AI
tsk ai review src/main.tsk
```

## Advanced Examples

### Custom Command Integration

```csharp
// CustomCommand.cs
public class CustomCommand : Command
{
    public CustomCommand() : base("custom", "Custom command")
    {
        this.AddOption(new Option<string>("--input", "Input file"));
        this.AddOption(new Option<string>("--output", "Output file"));
        this.SetHandler(ExecuteAsync);
    }

    private async Task<int> ExecuteAsync(string input, string output)
    {
        // Your custom logic
        var result = await ProcessFileAsync(input);
        await File.WriteAllTextAsync(output, result);
        return 0;
    }
}

// Program.cs
var rootCommand = new RootCommand("My CLI");
rootCommand.AddCommand(new CustomCommand());
return await rootCommand.InvokeAsync(args);
```

### Plugin System

```csharp
// IPlugin.cs
public interface IPlugin
{
    string Name { get; }
    Task<int> ExecuteAsync(string[] args);
}

// PluginLoader.cs
public class PluginLoader
{
    public static async Task<int> LoadAndExecuteAsync(string pluginName, string[] args)
    {
        // Load plugin dynamically
        var plugin = LoadPlugin(pluginName);
        return await plugin.ExecuteAsync(args);
    }
}
```

## Best Practices

### Configuration Management

1. **Use hierarchical configuration:**
   ```bash
   # Base configuration
   peanu.peanuts
   
   # Environment-specific overrides
   peanu.development.peanuts
   peanu.production.peanuts
   ```

2. **Compile to binary for production:**
   ```bash
   tsk config compile peanu.production.peanuts
   ```

3. **Validate configurations:**
   ```bash
   tsk config validate peanu.production.pnt
   ```

### Error Handling

```csharp
public async Task<string> GetConfigSafelyAsync(string key)
{
    try
    {
        return await TskCommand.ExecuteAsync("config", "get", key);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get config: {Key}", key);
        return null;
    }
}
```

### Performance Optimization

```csharp
// Use singleton pattern for configuration
public static class AppConfig
{
    private static readonly Lazy<PeanutConfig> _config = new(() => new PeanutConfig());
    public static PeanutConfig Instance => _config.Value;
}

// Cache frequently accessed values
private static readonly ConcurrentDictionary<string, object> _cache = new();

public async Task<T> GetCachedConfigAsync<T>(string key)
{
    if (_cache.TryGetValue(key, out var cached))
        return (T)cached;

    var value = await AppConfig.Instance.GetAsync<T>(key);
    _cache[key] = value;
    return value;
}
```

---

These examples demonstrate practical usage patterns and best practices for the TuskLang C# CLI. Each example is designed to be production-ready and follows C# conventions and idioms. 