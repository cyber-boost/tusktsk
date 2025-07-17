# 🐹 TuskLang Go Installation Guide

**"We don't bow to any king" - Go Edition**

Welcome to the TuskLang Go SDK installation guide. This guide will get you up and running with TuskLang in your Go projects with production-ready performance and type-safe struct mapping.

## 🚀 Quick Installation

### One-Line Install (Recommended)

```bash
# Direct install from official source
curl -sSL https://go.tusklang.org | bash

# Alternative with wget
wget -qO- https://go.tusklang.org | bash
```

### Go Module Installation

```bash
# Add to your go.mod
go get github.com/tusklang/go

# Or install globally
go install github.com/tusklang/go/cmd/tusk@latest
```

### Manual Installation

```bash
# Clone the repository
git clone https://github.com/tusklang/go
cd go

# Install the CLI tool
go install ./cmd/tusk

# Verify installation
tusk --version
```

## 📋 Prerequisites

### System Requirements

- **Go Version**: 1.21 or higher
- **Operating System**: Linux, macOS, Windows
- **Architecture**: x86_64, ARM64
- **Memory**: 512MB minimum, 2GB recommended
- **Disk Space**: 100MB for installation

### Go Environment Setup

```bash
# Verify Go installation
go version

# Set up Go modules (if not already done)
go mod init myapp
```

## 🔧 Installation Methods

### Method 1: Go Modules (Recommended for Projects)

```go
// go.mod
module myapp

go 1.21

require (
    github.com/tusklang/go v1.0.0
)
```

```bash
# Install dependencies
go mod tidy

# Verify installation
go list -m github.com/tusklang/go
```

### Method 2: Global Installation

```bash
# Install CLI tool globally
go install github.com/tusklang/go/cmd/tusk@latest

# Add to PATH (if not already done)
export PATH=$PATH:$(go env GOPATH)/bin

# Verify installation
tusk --version
```

### Method 3: Docker Installation

```dockerfile
# Dockerfile
FROM golang:1.21-alpine

# Install TuskLang
RUN go install github.com/tusklang/go/cmd/tusk@latest

# Add to PATH
ENV PATH=$PATH:/go/bin

# Verify installation
RUN tusk --version
```

## 🎯 Post-Installation Setup

### 1. Verify Installation

```bash
# Check CLI tool
tusk --version

# Check Go package
go list -m github.com/tusklang/go
```

### 2. Create Your First TSK File

```go
// main.go
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    // Create a simple TSK file
    tskContent := `
[app]
name: "My Go App"
version: "1.0.0"
debug: true

[server]
host: "localhost"
port: 8080
`
    
    data, err := parser.ParseString(tskContent)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("App: %s v%s\n", 
        data["app"].(map[string]interface{})["name"],
        data["app"].(map[string]interface{})["version"])
}
```

### 3. Test Database Integration

```go
package main

import (
    "fmt"
    "github.com/tusklang/go"
    "github.com/tusklang/go/adapters"
)

func main() {
    // Create SQLite adapter
    sqlite, err := adapters.NewSQLiteAdapter("test.db")
    if err != nil {
        panic(err)
    }
    
    // Create parser with database
    parser := tusklanggo.NewEnhancedParser()
    parser.SetDatabaseAdapter(sqlite)
    
    // Test database query
    tskContent := `
[test]
user_count: @query("SELECT COUNT(*) FROM sqlite_master")
`
    
    data, err := parser.ParseString(tskContent)
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Database test successful: %v\n", data["test"])
}
```

## 🔍 Installation Verification

### CLI Tool Verification

```bash
# Test basic commands
tusk --help
tusk version
tusk parse --help

# Test parsing
echo '[test] value: "hello"' > test.tsk
tusk parse test.tsk
```

### Go Package Verification

```go
package main

import (
    "fmt"
    "github.com/tusklang/go"
)

func main() {
    // Test basic parsing
    parser := tusklanggo.NewEnhancedParser()
    
    testContent := `
[verification]
status: "success"
version: "1.0.0"
`
    
    data, err := parser.ParseString(testContent)
    if err != nil {
        panic(fmt.Sprintf("Installation failed: %v", err))
    }
    
    fmt.Println("✅ TuskLang Go SDK installed successfully!")
    fmt.Printf("Status: %s\n", data["verification"].(map[string]interface{})["status"])
}
```

## 🛠️ Development Environment Setup

### IDE Configuration

#### VS Code

```json
// .vscode/settings.json
{
    "go.useLanguageServer": true,
    "go.toolsManagement.autoUpdate": true,
    "files.associations": {
        "*.tsk": "ini"
    }
}
```

#### GoLand

1. Install GoLand
2. Configure Go SDK (1.21+)
3. Install TuskLang plugin (if available)
4. Set up file associations for `.tsk` files

### Project Structure

```
myapp/
├── go.mod
├── go.sum
├── main.go
├── config.tsk
├── .gitignore
└── README.md
```

### .gitignore Configuration

```gitignore
# Go
*.exe
*.exe~
*.dll
*.so
*.dylib
*.test
*.out
go.work

# TuskLang
*.db
*.sqlite
*.log
.env

# IDE
.vscode/
.idea/
*.swp
*.swo
```

## 🔧 Troubleshooting

### Common Installation Issues

#### 1. Go Version Too Old

```bash
# Error: go version go1.20 linux/amd64
# Solution: Update Go to 1.21+

# Download and install Go 1.21+
wget https://go.dev/dl/go1.21.0.linux-amd64.tar.gz
sudo tar -C /usr/local -xzf go1.21.0.linux-amd64.tar.gz
export PATH=$PATH:/usr/local/go/bin
```

#### 2. Module Not Found

```bash
# Error: cannot find module "github.com/tusklang/go"
# Solution: Check network and module configuration

# Verify Go proxy settings
go env GOPROXY

# Set proxy if needed
go env -w GOPROXY=https://proxy.golang.org,direct
```

#### 3. Permission Denied

```bash
# Error: permission denied when installing globally
# Solution: Use proper permissions

# Install with proper permissions
sudo go install github.com/tusklang/go/cmd/tusk@latest

# Or install locally
go install github.com/tusklang/go/cmd/tusk@latest
export PATH=$PATH:$(go env GOPATH)/bin
```

#### 4. Database Connection Issues

```go
// Error: database connection failed
// Solution: Check database configuration

package main

import (
    "fmt"
    "github.com/tusklang/go/adapters"
)

func main() {
    // Test database connection
    sqlite, err := adapters.NewSQLiteAdapter("test.db")
    if err != nil {
        panic(fmt.Sprintf("Database connection failed: %v", err))
    }
    
    // Test query
    result, err := sqlite.Query("SELECT 1")
    if err != nil {
        panic(fmt.Sprintf("Query failed: %v", err))
    }
    
    fmt.Println("✅ Database connection successful!")
}
```

### Debug Mode

```go
package main

import (
    "github.com/tusklang/go"
)

func main() {
    // Enable debug logging
    parser := tusklanggo.NewEnhancedParser()
    parser.SetDebug(true)
    
    // Parse with debug output
    data, err := parser.ParseFile("config.tsk")
    if err != nil {
        panic(err)
    }
    
    fmt.Printf("Debug output: %+v\n", data)
}
```

## 📊 Performance Verification

### Installation Performance Test

```go
package main

import (
    "fmt"
    "time"
    "github.com/tusklang/go"
)

func main() {
    parser := tusklanggo.NewEnhancedParser()
    
    // Performance test
    start := time.Now()
    
    for i := 0; i < 1000; i++ {
        _, err := parser.ParseString(`
[test]
value: "performance test"
count: ${i}
`)
        if err != nil {
            panic(err)
        }
    }
    
    duration := time.Since(start)
    fmt.Printf("Performance: %d parses in %v (%.2f parses/sec)\n", 
        1000, duration, float64(1000)/duration.Seconds())
}
```

## 🎯 Next Steps

After successful installation:

1. **Create your first TSK configuration file**
2. **Explore type-safe struct mapping**
3. **Set up database integration**
4. **Learn about @ operators**
5. **Build your first application**

## 📚 Additional Resources

- **Official Documentation**: [docs.tusklang.org/go](https://docs.tusklang.org/go)
- **GitHub Repository**: [github.com/tusklang/go](https://github.com/tusklang/go)
- **Go Module**: [pkg.go.dev/github.com/tusklang/go](https://pkg.go.dev/github.com/tusklang/go)
- **Examples**: [examples.tusklang.org/go](https://examples.tusklang.org/go)
- **Community**: [community.tusklang.org](https://community.tusklang.org)

---

**"We don't bow to any king"** - Your TuskLang Go SDK is now ready to revolutionize your configuration management with type-safe struct mapping, database integration, and production-ready performance! 