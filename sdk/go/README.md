# 🐘 tusklang-go

Native Go implementation for TuskLang configuration files (.tsk).

## Features ✅
- Full TuskLang parser (key-value, nesting, arrays, interpolation)
- Type-safe Go struct mapping with reflection
- CLI tool for parsing, validation, and codegen
- Variable interpolation support
- Comprehensive test suite
- Docker/Kubernetes ready

## Installation

```bash
# Clone the repository
git clone https://github.com/tusklang/tusklang-go.git
cd tusklang-go

# Install dependencies
go mod tidy

# Build the CLI tool
go build -o tusk-go main.go
```

## Usage

### As a Library

```go
package main

import (
    "os"
    "fmt"
    "github.com/tusklang/tusklang-go"
)

// Define your configuration struct
type Config struct {
    AppName string `tsk:"app_name"`
    Version string `tsk:"version"`
    Port    int    `tsk:"port"`
    Debug   bool   `tsk:"debug"`
}

func main() {
    // Open and parse the .tsk file
    file, _ := os.Open("config.tsk")
    parser := tusklanggo.NewParser(file)
    data, _ := parser.Parse()
    
    // Unmarshal into your struct
    var config Config
    tusklanggo.UnmarshalTSK(data, &config)
    
    // Use your configuration
    fmt.Printf("Starting %s v%s on port %d\n", config.AppName, config.Version, config.Port)
}
```

### CLI Commands

The TuskLang Go SDK provides a comprehensive CLI with all commands from the Universal CLI Command Specification:

#### 🗄️ Database Commands
```bash
tsk db status                    # Check database connection
tsk db migrate <file>           # Run migration file
tsk db console                  # Interactive database console
tsk db backup [file]            # Backup database
tsk db restore <file>           # Restore from backup
tsk db init                     # Initialize SQLite database
```

#### 🔧 Development Commands
```bash
tsk serve [port]                # Start development server
tsk compile <file>              # Compile .tsk file
tsk optimize <file>             # Optimize .tsk file
```

#### 🧪 Testing Commands
```bash
tsk test [suite]                # Run test suites
tsk test all                    # Run all tests
tsk test parser                 # Test parser only
tsk test fujsen                 # Test FUJSEN only
tsk test sdk                    # Test SDK only
tsk test performance            # Performance tests
```

#### ⚙️ Service Commands
```bash
tsk services start              # Start all services
tsk services stop               # Stop all services
tsk services restart            # Restart services
tsk services status             # Show service status
```

#### 📦 Cache Commands
```bash
tsk cache clear                 # Clear cache
tsk cache status                # Show cache status
tsk cache warm                  # Warm cache
tsk cache memcached [subcommand] # Memcached operations
```

#### 🥜 Configuration Commands
```bash
tsk config get <key.path> [dir] # Get config value
tsk config check [path]         # Check hierarchy
tsk config validate [path]      # Validate config
tsk config compile [path]       # Compile configs
tsk config docs [path]          # Generate docs
tsk config clear-cache [path]   # Clear cache
tsk config stats                # Show statistics
```

#### 🚀 Binary Commands
```bash
tsk binary compile <file.tsk>   # Compile to .tskb
tsk binary execute <file.tskb>  # Execute binary
tsk binary benchmark <file>     # Benchmark performance
tsk binary optimize <file>      # Optimize binary
```

#### 🤖 AI Commands
```bash
tsk ai claude <prompt>          # Query Claude
tsk ai chatgpt <prompt>         # Query ChatGPT
tsk ai analyze <file>           # Analyze code
tsk ai optimize <file>          # Optimization suggestions
tsk ai security <file>          # Security scan
```

#### 🛠️ Utility Commands
```bash
tsk parse <file>                # Parse TSK file
tsk validate <file>             # Validate syntax
tsk convert -i <in> -o <out>    # Convert formats
tsk get <file> <key.path>       # Get value
tsk set <file> <key.path> <val> # Set value
```

#### 🥜 Peanuts Commands
```bash
tsk peanuts compile <file>      # Compile to .pnt
tsk peanuts auto-compile [dir]  # Auto-compile all files
tsk peanuts load <file.pnt>     # Load binary file
```

#### 🎨 CSS Commands
```bash
tsk css expand <input> [output] # Expand CSS shortcodes
tsk css map                     # Show shortcode mappings
```

#### 🔐 License Commands
```bash
tsk license check               # Check license status
tsk license activate <key>      # Activate license
```

#### 📁 Project Commands
```bash
tsk init [project-name]         # Initialize new project
tsk migrate --from=<format>     # Migrate from other formats
```

### Building and Installation

```bash
# Build the CLI
make build

# Install globally
make install

# Run with arguments
make run-args ARGS="parse config.tsk"

# Build for all platforms
make build-all

# Create release packages
make release
```

## TuskLang Syntax Support

### Basic Key-Value Pairs
```tsk
app_name: "My Application"
version: "1.0.0"
debug: true
port: 8080
```

### Nested Objects
```tsk
database:
  host: "localhost"
  port: 5432
  name: "myapp"
  user: "admin"
  password: "secret"
```

### Arrays
```tsk
features:
  - logging
  - metrics
  - caching
  - authentication
```

### Variable Interpolation
```tsk
base_url: "https://api.example.com"
endpoint: "$base_url/v1/users"
```

### Comments
```tsk
# This is a comment
app_name: "My App"  # Inline comment
```

## Example Configuration

See `testdata/complex.tsk` for a comprehensive example:

```tsk
# Complex TuskLang configuration
app_name: "TuskLang Go Parser Test"
version: "1.0.0"
debug: true
port: 8080

database:
  host: "localhost"
  port: 5432
  name: "tusklang_db"
  user: "admin"
  password: "secret123"

features:
  - logging
  - metrics
  - hot_reload
  - caching
```

## Running Tests

```bash
# Run all tests
go test ./...

# Run with verbose output
go test -v ./...

# Run specific test
go test -run TestParserBasic
```

## Example Application

See the `example/` directory for a complete application demonstrating:

- Configuration loading
- Struct mapping
- Type safety
- Error handling

```bash
cd example
go run main.go ../testdata/complex.tsk
```

## Roadmap
- [x] Module scaffold
- [x] Parser implementation
- [x] CLI tool
- [x] Struct mapping
- [x] Variable interpolation
- [x] Test suite
- [ ] Docker integration
- [ ] Kubernetes manifests
- [ ] Performance benchmarks
- [ ] Documentation & examples

## Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## License

MIT License - see LICENSE file for details. 