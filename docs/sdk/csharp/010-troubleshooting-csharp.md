# 🔧 Troubleshooting - TuskLang for C# - "Solve Any Problem"

**Master the art of debugging TuskLang - From simple syntax errors to complex production issues!**

Every technology has its challenges, and TuskLang is no exception. Learn how to diagnose, debug, and resolve common issues that you'll encounter in development and production environments.

## 🎯 Troubleshooting Philosophy

### "We Don't Bow to Any King"
- **Systematic approach** - Methodical problem-solving techniques
- **Root cause analysis** - Find the real source of issues
- **Prevention strategies** - Learn from problems to prevent future issues
- **Documentation** - Document solutions for team knowledge
- **Continuous improvement** - Use issues to improve your system

### Why Troubleshooting Skills Matter?
- **Faster resolution** - Solve problems quickly and efficiently
- **Reduced downtime** - Minimize service interruptions
- **Better understanding** - Deep knowledge of how TuskLang works
- **Team productivity** - Help others solve similar problems
- **System reliability** - Build more robust configurations

## 🔍 Diagnostic Tools

### 1. TuskLang CLI Debugging

```bash
# Validate configuration syntax
tusk validate config/app.tsk

# Parse with verbose output
tusk parse config/app.tsk --verbose

# Test configuration with sample data
tusk test config/app.tsk --data test-data.json

# Profile configuration performance
tusk profile config/app.tsk --iterations 1000

# Debug specific operators
tusk debug config/app.tsk --operator @query
```

### 2. C# Debugging Tools

```csharp
// DebugConfigurationService.cs
using TuskLang;
using Microsoft.Extensions.Logging;

public class DebugConfigurationService
{
    private readonly TuskLang _parser;
    private readonly ILogger<DebugConfigurationService> _logger;
    
    public DebugConfigurationService(ILogger<DebugConfigurationService> logger)
    {
        _parser = new TuskLang();
        _logger = logger;
        
        // Enable verbose logging
        _parser.EnableVerboseLogging();
    }
    
    public async Task<DebugResult> DebugConfigurationAsync(string filePath)
    {
        var result = new DebugResult
        {
            FilePath = filePath,
            StartTime = DateTime.UtcNow
        };
        
        try
        {
            _logger.LogInformation("Starting configuration debug for {FilePath}", filePath);
            
            // Step 1: File validation
            result.FileExists = File.Exists(filePath);
            if (!result.FileExists)
            {
                result.Errors.Add("Configuration file does not exist");
                return result;
            }
            
            result.FileSize = new FileInfo(filePath).Length;
            result.FileContent = await File.ReadAllTextAsync(filePath);
            
            // Step 2: Syntax validation
            var validationResult = _parser.ValidateWithDetails(filePath);
            result.IsValid = validationResult.IsValid;
            result.ValidationErrors = validationResult.Errors.Select(e => e.Message).ToList();
            
            if (!result.IsValid)
            {
                _logger.LogError("Configuration validation failed: {Errors}", 
                    string.Join(", ", result.ValidationErrors));
                return result;
            }
            
            // Step 3: Parse configuration
            var stopwatch = Stopwatch.StartNew();
            var config = _parser.ParseFile(filePath);
            stopwatch.Stop();
            
            result.ParseTime = stopwatch.ElapsedMilliseconds;
            result.Configuration = config;
            
            // Step 4: Analyze configuration
            result.Sections = config.Keys.ToList();
            result.TotalKeys = CountKeys(config);
            
            _logger.LogInformation("Configuration debug completed successfully in {Duration}ms", 
                result.ParseTime);
            
            return result;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Unexpected error: {ex.Message}");
            result.Exception = ex;
            
            _logger.LogError(ex, "Configuration debug failed");
            return result;
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
        }
    }
    
    private int CountKeys(Dictionary<string, object> config)
    {
        int count = 0;
        foreach (var kvp in config)
        {
            count++;
            if (kvp.Value is Dictionary<string, object> nested)
            {
                count += CountKeys(nested);
            }
        }
        return count;
    }
}

public class DebugResult
{
    public string FilePath { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    
    public bool FileExists { get; set; }
    public long FileSize { get; set; }
    public string FileContent { get; set; } = string.Empty;
    
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new List<string>();
    
    public long ParseTime { get; set; }
    public Dictionary<string, object>? Configuration { get; set; }
    
    public List<string> Sections { get; set; } = new List<string>();
    public int TotalKeys { get; set; }
    
    public List<string> Errors { get; set; } = new List<string>();
    public Exception? Exception { get; set; }
}
```

### 3. Performance Profiling

```csharp
// PerformanceProfiler.cs
using TuskLang;
using System.Diagnostics;

public class PerformanceProfiler
{
    private readonly TuskLang _parser;
    private readonly ILogger<PerformanceProfiler> _logger;
    
    public PerformanceProfiler(ILogger<PerformanceProfiler> logger)
    {
        _parser = new TuskLang();
        _logger = logger;
    }
    
    public async Task<PerformanceProfile> ProfileConfigurationAsync(string filePath, int iterations = 100)
    {
        var profile = new PerformanceProfile
        {
            FilePath = filePath,
            Iterations = iterations
        };
        
        var times = new List<long>();
        
        _logger.LogInformation("Starting performance profiling for {FilePath} with {Iterations} iterations", 
            filePath, iterations);
        
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var config = _parser.ParseFile(filePath);
                stopwatch.Stop();
                times.Add(stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                profile.Errors.Add($"Iteration {i}: {ex.Message}");
            }
        }
        
        if (times.Count > 0)
        {
            profile.AverageTime = times.Average();
            profile.MinTime = times.Min();
            profile.MaxTime = times.Max();
            profile.MedianTime = CalculateMedian(times);
            profile.P95Time = CalculatePercentile(times, 95);
            profile.P99Time = CalculatePercentile(times, 99);
        }
        
        _logger.LogInformation("Performance profiling completed. Average: {Average}ms, P95: {P95}ms", 
            profile.AverageTime, profile.P95Time);
        
        return profile;
    }
    
    private double CalculateMedian(List<long> values)
    {
        var sorted = values.OrderBy(x => x).ToList();
        int count = sorted.Count;
        
        if (count == 0) return 0;
        if (count % 2 == 0)
        {
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
        }
        return sorted[count / 2];
    }
    
    private double CalculatePercentile(List<long> values, int percentile)
    {
        var sorted = values.OrderBy(x => x).ToList();
        int index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        return sorted[Math.Max(0, index)];
    }
}

public class PerformanceProfile
{
    public string FilePath { get; set; } = string.Empty;
    public int Iterations { get; set; }
    
    public double AverageTime { get; set; }
    public long MinTime { get; set; }
    public long MaxTime { get; set; }
    public double MedianTime { get; set; }
    public double P95Time { get; set; }
    public double P99Time { get; set; }
    
    public List<string> Errors { get; set; } = new List<string>();
}
```

## 🚨 Common Issues and Solutions

### 1. Syntax Errors

#### Problem: Invalid TSK Syntax

```ini
# Problem: Missing closing brace
[app]
name: "MyApp"
[database
host: "localhost"  # Missing closing brace
```

**Solution:**
```ini
# Solution: Proper syntax
[app]
name: "MyApp"

[database]
host: "localhost"
```

#### Problem: Invalid @ Operator Usage

```ini
# Problem: Invalid @ operator
[config]
value: @invalid_operator("test")
```

**Solution:**
```ini
# Solution: Use valid @ operator
[config]
value: @env("TEST_VAR", "default")
```

#### C# Error Handling:

```csharp
public class SyntaxErrorHandler
{
    private readonly TuskLang _parser;
    private readonly ILogger<SyntaxErrorHandler> _logger;
    
    public SyntaxErrorHandler(ILogger<SyntaxErrorHandler> logger)
    {
        _parser = new TuskLang();
        _logger = logger;
    }
    
    public async Task<SyntaxValidationResult> ValidateSyntaxAsync(string filePath)
    {
        var result = new SyntaxValidationResult
        {
            FilePath = filePath
        };
        
        try
        {
            // Validate syntax
            var validationResult = _parser.ValidateWithDetails(filePath);
            result.IsValid = validationResult.IsValid;
            result.Errors = validationResult.Errors;
            
            if (!result.IsValid)
            {
                _logger.LogError("Syntax validation failed for {FilePath}: {Errors}", 
                    filePath, string.Join(", ", validationResult.Errors.Select(e => e.Message)));
            }
            
            return result;
        }
        catch (Exception ex)
        {
            result.Errors.Add(new ValidationError
            {
                Line = 0,
                Column = 0,
                Message = $"Unexpected error: {ex.Message}"
            });
            
            _logger.LogError(ex, "Syntax validation failed for {FilePath}", filePath);
            return result;
        }
    }
    
    public async Task<string> AutoFixSyntaxAsync(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);
        
        // Common auto-fixes
        content = content
            .Replace("[database\n", "[database]\n")  // Fix missing closing brace
            .Replace("@invalid_operator", "@env")    // Fix invalid operator
            .Replace(": \"value\"\n[", ":\n[")       // Fix missing newline
            .Replace(":value", ": value");           // Fix missing space
        
        var tempPath = filePath + ".fixed";
        await File.WriteAllTextAsync(tempPath, content);
        
        _logger.LogInformation("Auto-fixed syntax and saved to {TempPath}", tempPath);
        return tempPath;
    }
}

public class SyntaxValidationResult
{
    public string FilePath { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
}

public class ValidationError
{
    public int Line { get; set; }
    public int Column { get; set; }
    public string Message { get; set; } = string.Empty;
}
```

### 2. Database Connection Issues

#### Problem: Database Connection Failed

```ini
# Problem: Database connection fails
[database]
host: "invalid-host"
port: 5432
name: "myapp"
user: "postgres"
password: "wrong-password"
```

**Solution:**
```ini
# Solution: Proper database configuration with fallbacks
[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", "5432")
name: @env("DB_NAME", "myapp")
user: @env("DB_USER", "postgres")
password: @env.secure("DB_PASSWORD")
ssl: @if($environment == "production", true, false)
```

#### C# Database Troubleshooting:

```csharp
public class DatabaseTroubleshooter
{
    private readonly ILogger<DatabaseTroubleshooter> _logger;
    
    public DatabaseTroubleshooter(ILogger<DatabaseTroubleshooter> logger)
    {
        _logger = logger;
    }
    
    public async Task<DatabaseDiagnosticResult> DiagnoseDatabaseAsync(string connectionString)
    {
        var result = new DatabaseDiagnosticResult
        {
            ConnectionString = connectionString,
            StartTime = DateTime.UtcNow
        };
        
        try
        {
            // Test connection
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            result.CanConnect = true;
            
            // Test basic query
            using var command = new NpgsqlCommand("SELECT 1", connection);
            var queryResult = await command.ExecuteScalarAsync();
            result.CanQuery = queryResult != null;
            
            // Test specific tables
            result.Tables = await GetTablesAsync(connection);
            
            // Test performance
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 10; i++)
            {
                using var perfCommand = new NpgsqlCommand("SELECT COUNT(*) FROM information_schema.tables", connection);
                await perfCommand.ExecuteScalarAsync();
            }
            stopwatch.Stop();
            result.AverageQueryTime = stopwatch.ElapsedMilliseconds / 10.0;
            
            _logger.LogInformation("Database diagnostic completed successfully");
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Database connection failed: {ex.Message}");
            _logger.LogError(ex, "Database diagnostic failed");
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
        }
        
        return result;
    }
    
    private async Task<List<string>> GetTablesAsync(NpgsqlConnection connection)
    {
        var tables = new List<string>();
        
        using var command = new NpgsqlCommand(@"
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = 'public'
            ORDER BY table_name", connection);
        
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }
        
        return tables;
    }
    
    public async Task<bool> TestConfigurationAsync(Dictionary<string, object> config)
    {
        try
        {
            var database = config["database"] as Dictionary<string, object>;
            if (database == null)
            {
                _logger.LogError("Database configuration section not found");
                return false;
            }
            
            var connectionString = BuildConnectionString(database);
            var diagnostic = await DiagnoseDatabaseAsync(connectionString);
            
            return diagnostic.CanConnect && diagnostic.CanQuery;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database configuration test failed");
            return false;
        }
    }
    
    private string BuildConnectionString(Dictionary<string, object> database)
    {
        var host = database["host"]?.ToString() ?? "localhost";
        var port = database["port"]?.ToString() ?? "5432";
        var name = database["name"]?.ToString() ?? "postgres";
        var user = database["user"]?.ToString() ?? "postgres";
        var password = database["password"]?.ToString() ?? "";
        
        return $"Host={host};Port={port};Database={name};Username={user};Password={password}";
    }
}

public class DatabaseDiagnosticResult
{
    public string ConnectionString { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    
    public bool CanConnect { get; set; }
    public bool CanQuery { get; set; }
    public List<string> Tables { get; set; } = new List<string>();
    public double AverageQueryTime { get; set; }
    
    public List<string> Errors { get; set; } = new List<string>();
}
```

### 3. Environment Variable Issues

#### Problem: Missing Environment Variables

```ini
# Problem: Environment variable not set
[config]
api_key: @env("API_KEY")  # API_KEY not set
```

**Solution:**
```ini
# Solution: Provide default values
[config]
api_key: @env("API_KEY", "")  # Empty string default
debug_mode: @env("DEBUG", "false")  # Boolean default
port: @env("PORT", "8080")  # Numeric default
```

#### C# Environment Variable Troubleshooting:

```csharp
public class EnvironmentVariableTroubleshooter
{
    private readonly ILogger<EnvironmentVariableTroubleshooter> _logger;
    
    public EnvironmentVariableTroubleshooter(ILogger<EnvironmentVariableTroubleshooter> logger)
    {
        _logger = logger;
    }
    
    public async Task<EnvironmentDiagnosticResult> DiagnoseEnvironmentAsync(string[] requiredVariables)
    {
        var result = new EnvironmentDiagnosticResult
        {
            StartTime = DateTime.UtcNow
        };
        
        foreach (var variable in requiredVariables)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            var status = new EnvironmentVariableStatus
            {
                Name = variable,
                IsSet = !string.IsNullOrEmpty(value),
                Value = value ?? "NOT_SET",
                IsSecure = IsSecureVariable(variable)
            };
            
            result.Variables.Add(status);
            
            if (!status.IsSet)
            {
                result.MissingVariables.Add(variable);
                _logger.LogWarning("Environment variable {Variable} is not set", variable);
            }
        }
        
        result.EndTime = DateTime.UtcNow;
        result.Duration = result.EndTime - result.StartTime;
        
        return result;
    }
    
    private bool IsSecureVariable(string variableName)
    {
        var securePatterns = new[] { "PASSWORD", "SECRET", "KEY", "TOKEN", "CREDENTIAL" };
        return securePatterns.Any(pattern => variableName.ToUpper().Contains(pattern));
    }
    
    public async Task<bool> ValidateConfigurationAsync(Dictionary<string, object> config)
    {
        var requiredVariables = ExtractRequiredVariables(config);
        var diagnostic = await DiagnoseEnvironmentAsync(requiredVariables);
        
        if (diagnostic.MissingVariables.Any())
        {
            _logger.LogError("Missing required environment variables: {Variables}", 
                string.Join(", ", diagnostic.MissingVariables));
            return false;
        }
        
        return true;
    }
    
    private string[] ExtractRequiredVariables(Dictionary<string, object> config)
    {
        var variables = new HashSet<string>();
        ExtractVariablesRecursive(config, variables);
        return variables.ToArray();
    }
    
    private void ExtractVariablesRecursive(Dictionary<string, object> config, HashSet<string> variables)
    {
        foreach (var kvp in config)
        {
            if (kvp.Value is string strValue)
            {
                // Extract @env() calls
                var matches = Regex.Matches(strValue, @"@env\(""([^""]+)""");
                foreach (Match match in matches)
                {
                    variables.Add(match.Groups[1].Value);
                }
            }
            else if (kvp.Value is Dictionary<string, object> nested)
            {
                ExtractVariablesRecursive(nested, variables);
            }
        }
    }
}

public class EnvironmentDiagnosticResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    
    public List<EnvironmentVariableStatus> Variables { get; set; } = new List<EnvironmentVariableStatus>();
    public List<string> MissingVariables { get; set; } = new List<string>();
}

public class EnvironmentVariableStatus
{
    public string Name { get; set; } = string.Empty;
    public bool IsSet { get; set; }
    public string Value { get; set; } = string.Empty;
    public bool IsSecure { get; set; }
}
```

### 4. Performance Issues

#### Problem: Slow Configuration Parsing

```ini
# Problem: Expensive operations without caching
[performance]
user_count: @query("SELECT COUNT(*) FROM users")  # No caching
api_data: @http("GET", "https://api.example.com/data")  # No caching
```

**Solution:**
```ini
# Solution: Use caching for expensive operations
[performance]
user_count: @cache("5m", @query("SELECT COUNT(*) FROM users"))
api_data: @cache("30s", @http("GET", "https://api.example.com/data"))
ml_prediction: @cache("1m", @predict("server_load", @metrics("cpu_usage", 75)))
```

#### C# Performance Troubleshooting:

```csharp
public class PerformanceTroubleshooter
{
    private readonly ILogger<PerformanceTroubleshooter> _logger;
    private readonly IMetricsCollector _metricsCollector;
    
    public PerformanceTroubleshooter(
        ILogger<PerformanceTroubleshooter> logger,
        IMetricsCollector metricsCollector)
    {
        _logger = logger;
        _metricsCollector = metricsCollector;
    }
    
    public async Task<PerformanceDiagnosticResult> DiagnosePerformanceAsync(string filePath)
    {
        var result = new PerformanceDiagnosticResult
        {
            FilePath = filePath,
            StartTime = DateTime.UtcNow
        };
        
        try
        {
            var parser = new TuskLang();
            
            // Profile parsing performance
            var parseTimes = new List<long>();
            for (int i = 0; i < 10; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var config = parser.ParseFile(filePath);
                stopwatch.Stop();
                parseTimes.Add(stopwatch.ElapsedMilliseconds);
            }
            
            result.AverageParseTime = parseTimes.Average();
            result.MinParseTime = parseTimes.Min();
            result.MaxParseTime = parseTimes.Max();
            result.ParseTimeVariance = CalculateVariance(parseTimes);
            
            // Analyze configuration complexity
            var config = parser.ParseFile(filePath);
            result.Sections = config.Keys.Count;
            result.TotalKeys = CountKeys(config);
            result.OperatorCount = CountOperators(await File.ReadAllTextAsync(filePath));
            
            // Check for performance issues
            result.Issues = await IdentifyPerformanceIssuesAsync(filePath);
            
            _logger.LogInformation("Performance diagnostic completed. Average parse time: {Time}ms", 
                result.AverageParseTime);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Performance diagnostic failed: {ex.Message}");
            _logger.LogError(ex, "Performance diagnostic failed");
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
        }
        
        return result;
    }
    
    private double CalculateVariance(List<long> values)
    {
        if (values.Count == 0) return 0;
        
        var mean = values.Average();
        var variance = values.Select(x => Math.Pow(x - mean, 2)).Average();
        return variance;
    }
    
    private int CountKeys(Dictionary<string, object> config)
    {
        int count = 0;
        foreach (var kvp in config)
        {
            count++;
            if (kvp.Value is Dictionary<string, object> nested)
            {
                count += CountKeys(nested);
            }
        }
        return count;
    }
    
    private int CountOperators(string content)
    {
        var operatorPatterns = new[] { "@env", "@query", "@http", "@cache", "@predict", "@learn" };
        return operatorPatterns.Sum(pattern => Regex.Matches(content, pattern).Count);
    }
    
    private async Task<List<string>> IdentifyPerformanceIssuesAsync(string filePath)
    {
        var issues = new List<string>();
        var content = await File.ReadAllTextAsync(filePath);
        
        // Check for uncached expensive operations
        if (Regex.IsMatch(content, @"@query\([^)]+\)") && !Regex.IsMatch(content, @"@cache\([^)]+, @query"))
        {
            issues.Add("Database queries without caching detected");
        }
        
        if (Regex.IsMatch(content, @"@http\([^)]+\)") && !Regex.IsMatch(content, @"@cache\([^)]+, @http"))
        {
            issues.Add("HTTP requests without caching detected");
        }
        
        // Check for nested expensive operations
        if (Regex.IsMatch(content, @"@cache\([^)]+, @cache\("))
        {
            issues.Add("Nested caching operations detected");
        }
        
        // Check for too many operators
        var operatorCount = CountOperators(content);
        if (operatorCount > 50)
        {
            issues.Add($"Too many operators ({operatorCount}) - consider simplifying");
        }
        
        return issues;
    }
}

public class PerformanceDiagnosticResult
{
    public string FilePath { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    
    public double AverageParseTime { get; set; }
    public long MinParseTime { get; set; }
    public long MaxParseTime { get; set; }
    public double ParseTimeVariance { get; set; }
    
    public int Sections { get; set; }
    public int TotalKeys { get; set; }
    public int OperatorCount { get; set; }
    
    public List<string> Issues { get; set; } = new List<string>();
    public List<string> Errors { get; set; } = new List<string>();
}
```

## 🔧 Advanced Troubleshooting

### 1. Memory Issues

```csharp
public class MemoryTroubleshooter
{
    private readonly ILogger<MemoryTroubleshooter> _logger;
    
    public MemoryTroubleshooter(ILogger<MemoryTroubleshooter> logger)
    {
        _logger = logger;
    }
    
    public async Task<MemoryDiagnosticResult> DiagnoseMemoryAsync()
    {
        var result = new MemoryDiagnosticResult
        {
            StartTime = DateTime.UtcNow
        };
        
        // Get memory information
        var process = Process.GetCurrentProcess();
        result.WorkingSet = process.WorkingSet64;
        result.PrivateMemory = process.PrivateMemorySize64;
        result.VirtualMemory = process.VirtualMemorySize64;
        
        // Force garbage collection to get accurate readings
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        result.WorkingSetAfterGC = process.WorkingSet64;
        result.PrivateMemoryAfterGC = process.PrivateMemorySize64;
        
        // Check for memory leaks
        result.MemoryLeakDetected = result.WorkingSetAfterGC > 100 * 1024 * 1024; // 100MB threshold
        
        result.EndTime = DateTime.UtcNow;
        result.Duration = result.EndTime - result.StartTime;
        
        return result;
    }
}

public class MemoryDiagnosticResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    
    public long WorkingSet { get; set; }
    public long PrivateMemory { get; set; }
    public long VirtualMemory { get; set; }
    
    public long WorkingSetAfterGC { get; set; }
    public long PrivateMemoryAfterGC { get; set; }
    
    public bool MemoryLeakDetected { get; set; }
}
```

### 2. Network Issues

```csharp
public class NetworkTroubleshooter
{
    private readonly ILogger<NetworkTroubleshooter> _logger;
    
    public NetworkTroubleshooter(ILogger<NetworkTroubleshooter> logger)
    {
        _logger = logger;
    }
    
    public async Task<NetworkDiagnosticResult> DiagnoseNetworkAsync(string[] endpoints)
    {
        var result = new NetworkDiagnosticResult
        {
            StartTime = DateTime.UtcNow
        };
        
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(10);
        
        foreach (var endpoint in endpoints)
        {
            var endpointResult = new EndpointTestResult
            {
                Endpoint = endpoint
            };
            
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await httpClient.GetAsync(endpoint);
                stopwatch.Stop();
                
                endpointResult.IsReachable = response.IsSuccessStatusCode;
                endpointResult.ResponseTime = stopwatch.ElapsedMilliseconds;
                endpointResult.StatusCode = (int)response.StatusCode;
            }
            catch (Exception ex)
            {
                endpointResult.IsReachable = false;
                endpointResult.Error = ex.Message;
                _logger.LogError(ex, "Network test failed for {Endpoint}", endpoint);
            }
            
            result.Endpoints.Add(endpointResult);
        }
        
        result.EndTime = DateTime.UtcNow;
        result.Duration = result.EndTime - result.StartTime;
        
        return result;
    }
}

public class NetworkDiagnosticResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    
    public List<EndpointTestResult> Endpoints { get; set; } = new List<EndpointTestResult>();
}

public class EndpointTestResult
{
    public string Endpoint { get; set; } = string.Empty;
    public bool IsReachable { get; set; }
    public long ResponseTime { get; set; }
    public int StatusCode { get; set; }
    public string Error { get; set; } = string.Empty;
}
```

## 🎯 Troubleshooting Checklist

### 1. Configuration Issues
- ✅ **Check file syntax** - Validate TSK syntax
- ✅ **Verify file path** - Ensure file exists and is accessible
- ✅ **Check permissions** - Verify read permissions
- ✅ **Validate operators** - Ensure @ operators are correct

### 2. Database Issues
- ✅ **Test connection** - Verify database connectivity
- ✅ **Check credentials** - Validate username/password
- ✅ **Verify permissions** - Ensure database user has required permissions
- ✅ **Test queries** - Validate SQL syntax and performance

### 3. Environment Issues
- ✅ **Check variables** - Verify all required environment variables are set
- ✅ **Validate values** - Ensure environment variable values are correct
- ✅ **Check scope** - Verify environment variable scope (user vs system)

### 4. Performance Issues
- ✅ **Profile parsing** - Measure configuration parse time
- ✅ **Check caching** - Verify caching is properly configured
- ✅ **Monitor resources** - Check CPU, memory, and network usage
- ✅ **Analyze queries** - Review database query performance

### 5. Security Issues
- ✅ **Validate secrets** - Ensure secrets are properly encrypted
- ✅ **Check permissions** - Verify file and database permissions
- ✅ **Audit access** - Review who has access to configuration
- ✅ **Test encryption** - Verify encryption/decryption works

## 🎉 You're Ready!

You've mastered troubleshooting TuskLang! You can now:

- ✅ **Diagnose issues systematically** - Use methodical problem-solving
- ✅ **Use debugging tools** - Leverage CLI and C# debugging tools
- ✅ **Solve common problems** - Handle syntax, database, and environment issues
- ✅ **Optimize performance** - Identify and fix performance bottlenecks
- ✅ **Monitor systems** - Track metrics and detect issues early
- ✅ **Prevent problems** - Use best practices to avoid common issues

## 🔥 What's Next?

Ready to scale and optimize? Explore:

1. **[Scaling Strategies](011-scaling-csharp.md)** - Handle massive scale
2. **[Advanced Patterns](012-advanced-patterns-csharp.md)** - Complex use cases
3. **[Integration Guides](013-integration-csharp.md)** - Third-party integrations

---

**"We don't bow to any king" - Your troubleshooting skills, your problem-solving power.**

Solve any problem with confidence! 🔧 