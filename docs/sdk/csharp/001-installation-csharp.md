# 🚀 Installing TuskLang for C# - "We Don't Bow to Any King"

**Configuration with a Heartbeat - .NET Edition**

TuskLang brings revolutionary configuration capabilities to the C# ecosystem. Say goodbye to static JSON files and hello to dynamic, database-powered configuration that adapts to your preferred syntax.

## 🎯 Why TuskLang for C#?

### The Problem with Traditional Configuration
- **Static JSON files** that can't adapt to runtime conditions
- **Environment variables** scattered across deployment scripts
- **Hard-coded values** that require redeployment for changes
- **No database integration** in configuration
- **Limited syntax flexibility** - you're stuck with one format

### The TuskLang Solution
- **Dynamic configuration** with real-time database queries
- **Multiple syntax styles** - use `[]`, `{}`, or `<>` as you prefer
- **Cross-file communication** with global variables
- **@ operator system** for environment, caching, and ML
- **ASP.NET Core integration** out of the box

## 📦 Installation Methods

### Method 1: NuGet Package (Recommended)

```bash
# Add to your project
dotnet add package TuskLang

# Or add to specific project
dotnet add MyProject/MyProject.csproj package TuskLang
```

### Method 2: Package Manager Console

```powershell
Install-Package TuskLang
```

### Method 3: Direct Package Reference

Add to your `.csproj` file:

```xml
<ItemGroup>
    <PackageReference Include="TuskLang" Version="1.0.0" />
</ItemGroup>
```

### Method 4: From Source (Development)

```bash
# Clone the repository
git clone https://github.com/tusklang/csharp
cd csharp

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run tests
dotnet test

# Install locally
dotnet pack
dotnet add package TuskLang --source ./bin/Debug/
```

## 🔧 .NET Version Requirements

### Minimum Requirements
- **.NET 6.0** or higher
- **C# 10.0** or higher
- **ASP.NET Core 6.0** (for web integration)

### Recommended
- **.NET 8.0** (LTS)
- **C# 12.0**
- **ASP.NET Core 8.0**

## 🏗️ Project Setup

### 1. Basic Console Application

```csharp
// Program.cs
using TuskLang;

var parser = new TuskLang();
var config = parser.ParseFile("app.tsk");

Console.WriteLine($"App Name: {config["app"]["name"]}");
```

### 2. ASP.NET Core Web Application

```csharp
// Program.cs
using TuskLang;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add TuskLang services
builder.Services.AddTuskLang("app.tsk");

var app = builder.Build();

app.MapGet("/", (TuskConfig config) => 
    $"Hello from {config.AppName} v{config.Version}!");

app.Run();
```

### 3. Class Library Integration

```csharp
// MyLibrary.csproj
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="TuskLang" Version="1.0.0" />
  </ItemGroup>
</Project>

// ConfigurationService.cs
using TuskLang;

public class ConfigurationService
{
    private readonly TuskLang _parser;
    
    public ConfigurationService()
    {
        _parser = new TuskLang();
    }
    
    public T ParseConfiguration<T>(string filePath) where T : class, new()
    {
        return _parser.ParseFile<T>(filePath);
    }
}
```

## 🎛️ Configuration File Setup

### Create Your First TSK File

```ini
# app.tsk - Traditional INI style
[app]
name: "MyAwesomeApp"
version: "1.0.0"
debug: true

[database]
host: "localhost"
port: 5432
name: "myapp"
```

### Alternative Syntax Styles

```json
// app.tsk - JSON-like style
{
    "app": {
        "name": "MyAwesomeApp",
        "version": "1.0.0",
        "debug": true
    },
    "database": {
        "host": "localhost",
        "port": 5432,
        "name": "myapp"
    }
}
```

```xml
<!-- app.tsk - XML-inspired style -->
<app>
    <name>MyAwesomeApp</name>
    <version>1.0.0</version>
    <debug>true</debug>
</app>
<database>
    <host>localhost</host>
    <port>5432</port>
    <name>myapp</name>
</database>
```

## 🔌 Database Integration Setup

### SQLite Setup

```csharp
using TuskLang.Adapters;

// Basic SQLite setup
var sqliteAdapter = new SQLiteAdapter("app.db");
var parser = new TuskLang();
parser.SetDatabaseAdapter(sqliteAdapter);

// TSK file with database queries
var tskContent = @"
[stats]
user_count: @query(""SELECT COUNT(*) FROM users"")
active_users: @query(""SELECT COUNT(*) FROM users WHERE active = 1"")
";
```

### PostgreSQL Setup

```csharp
using TuskLang.Adapters;

// PostgreSQL setup
var postgresAdapter = new PostgreSQLAdapter(new PostgreSQLConfig
{
    Host = "localhost",
    Port = 5432,
    Database = "myapp",
    User = "postgres",
    Password = "secret"
});

var parser = new TuskLang();
parser.SetDatabaseAdapter(postgresAdapter);
```

## 🛠️ Development Tools

### Visual Studio Integration

1. **Install the TuskLang Extension**
   - Open Visual Studio
   - Go to Extensions → Manage Extensions
   - Search for "TuskLang"
   - Install the extension

2. **Syntax Highlighting**
   - `.tsk` files will have syntax highlighting
   - IntelliSense support for @ operators
   - Error detection and validation

### Visual Studio Code Integration

1. **Install the TuskLang Extension**
   ```bash
   code --install-extension tusklang.tusk
   ```

2. **Configure settings.json**
   ```json
   {
     "files.associations": {
       "*.tsk": "tusk"
     },
     "tusk.validateOnSave": true,
     "tusk.autoComplete": true
   }
   ```

### JetBrains Rider Integration

1. **Install the TuskLang Plugin**
   - Go to Settings → Plugins
   - Search for "TuskLang"
   - Install the plugin

2. **Configure File Associations**
   - Settings → Editor → File Types
   - Add `*.tsk` as TuskLang files

## 🔍 Verification

### Test Your Installation

```csharp
// Test.cs
using TuskLang;

class Program
{
    static void Main()
    {
        var parser = new TuskLang();
        
        var testContent = @"
[test]
message: ""Hello from TuskLang!""
timestamp: @date.now()
";
        
        var result = parser.Parse(testContent);
        
        Console.WriteLine($"Message: {result["test"]["message"]}");
        Console.WriteLine($"Timestamp: {result["test"]["timestamp"]}");
        Console.WriteLine("✅ TuskLang is working correctly!");
    }
}
```

### Run the Test

```bash
dotnet run Test.cs
```

Expected output:
```
Message: Hello from TuskLang!
Timestamp: 2024-01-15T10:30:00Z
✅ TuskLang is working correctly!
```

## 🚨 Troubleshooting

### Common Issues

#### 1. Package Not Found
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore --force
```

#### 2. Version Conflicts
```xml
<!-- In your .csproj -->
<ItemGroup>
    <PackageReference Include="TuskLang" Version="1.0.0" />
    <PackageReference Include="System.Text.Json" Version="8.0.0" />
</ItemGroup>
```

#### 3. Database Connection Issues
```csharp
// Enable verbose logging
var parser = new TuskLang();
parser.EnableVerboseLogging();

// Check connection
try
{
    var adapter = new SQLiteAdapter("app.db");
    var test = adapter.Query("SELECT 1");
    Console.WriteLine("Database connection successful");
}
catch (Exception ex)
{
    Console.WriteLine($"Database error: {ex.Message}");
}
```

#### 4. ASP.NET Core Integration Issues
```csharp
// Ensure proper service registration
builder.Services.AddTuskLang("app.tsk");

// Verify configuration injection
app.MapGet("/config", (TuskConfig config) => 
    Results.Ok(new { config.AppName, config.Version }));
```

## 📚 Next Steps

Now that you have TuskLang installed, explore:

1. **[Quick Start Guide](002-quick-start-csharp.md)** - Build your first dynamic configuration
2. **[Basic Syntax](003-basic-syntax-csharp.md)** - Master TuskLang syntax flexibility
3. **[Database Integration](004-database-integration-csharp.md)** - Connect to your databases
4. **[Advanced Features](005-advanced-features-csharp.md)** - Unleash the full power

## 🎉 Welcome to the Revolution

You've just joined the TuskLang revolution. No more static configuration files. No more environment variable hell. No more redeployment for config changes.

**Configuration with a Heartbeat** - Welcome to the future of .NET configuration.

---

*"We don't bow to any king" - Your configuration, your rules.* 