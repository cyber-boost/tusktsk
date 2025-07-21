# TuskLang C# SDK

A powerful, high-performance C# SDK for TuskLang configuration management and processing.

## 🚀 Features

- **Advanced Configuration Parsing** - Parse and validate TSK configuration files
- **Semantic Analysis** - Full AST-based semantic analysis with type checking
- **Database Integration** - Connection pooling for SQL Server, PostgreSQL, MySQL, and SQLite
- **Hot Reload** - Real-time configuration updates with file watching
- **CLI Tools** - Comprehensive command-line interface for all operations
- **AI Assistance** - Built-in AI-powered configuration assistance
- **Extensible Architecture** - Plugin-based operator system and custom adapters

## 📦 Installation

### NuGet Package
```bash
dotnet add package TuskLang.SDK
```

### From Source
```bash
git clone https://github.com/tusklang/csharp-sdk.git
cd csharp-sdk
dotnet build
dotnet run -- --help
```

## 🛠️ Quick Start

### Basic Usage

```csharp
using TuskLang.Core.Configuration;
using TuskLang.Parser;

// Initialize configuration manager
var configManager = new ConfigurationManager();

// Parse TSK file
var parser = new TuskTskParser();
var result = parser.Parse("config.tsk");

// Access configuration
var value = configManager.GetValue<string>("database.connection");
```

### CLI Usage

```bash
# Parse a TSK file
tsk parse config.tsk

# Compile configuration
tsk compile config.tsk

# Validate configuration
tsk validate config.tsk

# Initialize new project
tsk init

# Build project
tsk build

# Run tests
tsk test

# Start development server
tsk serve

# AI assistance
tsk ai "help me optimize this config"
```

## 📚 Documentation

### Configuration Management

The SDK provides a robust configuration management system with:

- **File Watching** - Automatic reload on file changes
- **Caching** - High-performance configuration caching
- **Validation** - Built-in configuration validation
- **Hot Reload** - Real-time updates without restart

```csharp
var configManager = new ConfigurationManager(new ConfigurationManagerOptions
{
    EnableFileWatching = true,
    CacheExpiration = TimeSpan.FromMinutes(5),
    EnableHotReload = true
});

// Subscribe to configuration changes
configManager.ConfigurationChanged += (sender, e) =>
{
    Console.WriteLine($"Configuration updated: {e.FilePath}");
};
```

### Database Integration

Support for multiple database providers with connection pooling:

```csharp
using TuskLang.Core.Connection;

// SQL Server
var sqlPool = new SqlServerConnectionPool(connectionString, maxConnections: 10);

// PostgreSQL
var pgPool = new PostgreSqlConnectionPool(connectionString, maxConnections: 10);

// MySQL
var mysqlPool = new MySqlConnectionPool(connectionString, maxConnections: 10);

// SQLite
var sqlitePool = new SqliteConnectionPool(connectionString, maxConnections: 5);
```

### Parser and AST

Advanced parsing capabilities with full AST support:

```csharp
using TuskLang.Parser;
using TuskLang.Parser.Ast;

var parser = new TuskTskParser();
var ast = parser.Parse("config.tsk");

// Semantic analysis
var analyzer = new SemanticAnalyzer();
var result = analyzer.Analyze(ast);

// Visit AST nodes
var visitor = new CustomAstVisitor();
ast.Accept(visitor);
```

## 🏗️ Architecture

### Core Components

- **Parser** - TSK file parsing and AST generation
- **Semantic Analyzer** - Type checking and validation
- **Configuration Manager** - Configuration loading and caching
- **Connection Management** - Database connection pooling
- **CLI Framework** - Command-line interface
- **Operator System** - Extensible operator registry

### Project Structure

```
src/
├── CLI/                 # Command-line interface
├── Core/               # Core functionality
│   ├── Configuration/  # Configuration management
│   └── Connection/     # Database connections
├── Parser/             # TSK parsing and AST
│   ├── Ast/           # Abstract Syntax Tree nodes
│   └── Semantic/      # Semantic analysis
└── Tests/             # Unit tests
```

## 🔧 Configuration

### TSK File Format

TSK files support a rich configuration format:

```tsk
# Database configuration
database {
    provider = "sqlserver"
    connection = "Server=localhost;Database=mydb;"
    timeout = 30
}

# Application settings
app {
    name = "MyApp"
    version = "1.0.0"
    debug = true
}

# Include other files
include "secrets.tsk"
```

### Environment Variables

```bash
export TUSK_CONFIG_PATH="/path/to/config"
export TUSK_LOG_LEVEL="Debug"
export TUSK_ENABLE_CACHE="true"
```

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run specific test suite
dotnet test --filter "Category=Parser"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Setup

```bash
git clone https://github.com/tusklang/csharp-sdk.git
cd csharp-sdk
dotnet restore
dotnet build
dotnet test
```

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- **Documentation**: [docs.tusklang.dev](https://docs.tusklang.dev)
- **Issues**: [GitHub Issues](https://github.com/tusklang/csharp-sdk/issues)
- **Discussions**: [GitHub Discussions](https://github.com/tusklang/csharp-sdk/discussions)
- **Discord**: [TuskLang Community](https://discord.gg/tusklang)

## 🚀 Roadmap

See [ROADMAP.md](ROADMAP.md) for detailed development plans and upcoming features.

## 📊 Performance

- **Parse Speed**: 10,000+ lines/second
- **Memory Usage**: <50MB for typical configurations
- **Startup Time**: <100ms cold start
- **Hot Reload**: <10ms configuration updates

---

**Built with ❤️ by the TuskLang Team** 