# TuskTsk SDK Installation Guide

This guide provides installation instructions for all TuskTsk SDK implementations across different programming languages.

## Overview

TuskTsk SDK is available for the following programming languages:
- Python
- JavaScript/Node.js
- Go
- Rust
- C#/.NET
- PHP
- Ruby

## Prerequisites

### System Requirements
- **Operating System**: Linux, macOS, or Windows
- **Memory**: Minimum 4GB RAM (8GB recommended)
- **Storage**: 2GB free disk space
- **Network**: Internet connection for package downloads

### Language-Specific Requirements

#### Python
- Python 3.8 or higher
- pip package manager

#### JavaScript/Node.js
- Node.js 12.0 or higher
- npm or yarn package manager

#### Go
- Go 1.23 or higher
- Git (for module downloads)

#### Rust
- Rust 1.75 or higher
- Cargo package manager

#### C#/.NET
- .NET 8.0 SDK or higher
- Visual Studio 2022 or VS Code (recommended)

#### PHP
- PHP 8.1 or higher
- Composer package manager
- PDO and JSON extensions

#### Ruby
- Ruby 3.0 or higher
- Bundler gem

## Installation Methods

### 1. Package Managers (Recommended)

#### Python - Using pip

```bash
# Install from PyPI
pip install tusktsk

# Install with optional dependencies
pip install tusktsk[databases,dev]

# Install specific version
pip install tusktsk==2.0.2
```

#### JavaScript/Node.js - Using npm

```bash
# Install from npm
npm install tusktsk

# Install globally
npm install -g tusktsk

# Install specific version
npm install tusktsk@2.0.2
```

#### Go - Using go get

```bash
# Install from GitHub
go get github.com/cyber-boost/tusktsk/sdk/go

# Install specific version
go get github.com/cyber-boost/tusktsk/sdk/go@v2.0.2
```

#### Rust - Using Cargo

```bash
# Add to Cargo.toml
[dependencies]
tusktsk = "2.0.2"

# Or install binary
cargo install tusktsk
```

#### C#/.NET - Using NuGet

```bash
# Install via dotnet CLI
dotnet add package TuskTsk

# Install specific version
dotnet add package TuskTsk --version 2.0.2
```

#### PHP - Using Composer

```bash
# Install from Packagist
composer require tusktsk/tusktsk

# Install specific version
composer require tusktsk/tusktsk:^2.0.2
```

#### Ruby - Using RubyGems

```bash
# Install from RubyGems
gem install tusktsk

# Add to Gemfile
gem 'tusktsk', '~> 2.0.2'
```

### 2. GitHub Packages

#### Python

```bash
# Configure pip for GitHub Packages
pip config set global.extra-index-url https://npm.pkg.github.com/cyber-boost

# Install from GitHub Packages
pip install tusktsk --index-url https://npm.pkg.github.com/cyber-boost
```

#### JavaScript/Node.js

```bash
# Configure npm for GitHub Packages
npm config set @cyber-boost:registry https://npm.pkg.github.com

# Install from GitHub Packages
npm install @cyber-boost/tusktsk
```

#### Go

```bash
# Configure Go for GitHub Packages
go env -w GOPRIVATE=github.com/cyber-boost/tusktsk

# Install from GitHub Packages
go get github.com/cyber-boost/tusktsk/sdk/go
```

#### Rust

```bash
# Configure Cargo for GitHub Packages
echo "[registries.github]" >> ~/.cargo/config.toml
echo 'index = "https://github.com/cyber-boost/tusktsk-index"' >> ~/.cargo/config.toml

# Install from GitHub Packages
cargo install tusktsk --registry github
```

#### C#/.NET

```bash
# Configure NuGet for GitHub Packages
dotnet nuget add source https://nuget.pkg.github.com/cyber-boost/index.json --name github

# Install from GitHub Packages
dotnet add package TuskTsk --source github
```

#### PHP

```bash
# Configure Composer for GitHub Packages
composer config repositories.github composer https://packagist.pkg.github.com/cyber-boost

# Install from GitHub Packages
composer require cyber-boost/tusktsk
```

#### Ruby

```bash
# Configure RubyGems for GitHub Packages
gem sources --add https://rubygems.pkg.github.com/cyber-boost

# Install from GitHub Packages
gem install tusktsk --source https://rubygems.pkg.github.com/cyber-boost
```

### 3. Docker Installation

#### Using Docker Hub

```bash
# Pull and run Python SDK
docker pull ghcr.io/cyber-boost/tusktsk-python:latest
docker run -d --name tusktsk-python ghcr.io/cyber-boost/tusktsk-python:latest

# Pull and run JavaScript SDK
docker pull ghcr.io/cyber-boost/tusktsk-javascript:latest
docker run -d --name tusktsk-javascript ghcr.io/cyber-boost/tusktsk-javascript:latest
```

#### Using Docker Compose

```bash
# Clone repository
git clone https://github.com/cyber-boost/tusktsk.git
cd tusktsk

# Start all services
docker-compose up -d
```

### 4. Source Code Installation

#### Clone and Build

```bash
# Clone repository
git clone https://github.com/cyber-boost/tusktsk.git
cd tusktsk

# Python
cd sdk/python
pip install -e .

# JavaScript
cd sdk/javascript
npm install

# Go
cd sdk/go
go mod download
go build

# Rust
cd sdk/rust
cargo build --release

# C#
cd sdk/csharp
dotnet restore
dotnet build

# PHP
cd sdk/php
composer install

# Ruby
cd sdk/ruby
bundle install
```

## Configuration

### Environment Variables

```bash
# TuskTsk Configuration
export TUSKTSK_CONFIG_PATH=/path/to/config
export TUSKTSK_LOG_LEVEL=info
export TUSKTSK_ENVIRONMENT=production
```

### Configuration Files

Create a `peanut.tsk` configuration file:

```tsk
# Database configuration
database = {
    host = "localhost"
    port = 3306
    name = "tusktsk"
    user = "tusktsk"
    password = "tusktsk_pass"
}

# Logging configuration
logging = {
    level = "info"
    format = "json"
    output = "stdout"
}

# API configuration
api = {
    host = "0.0.0.0"
    port = 8080
    timeout = 30
}
```

## Verification

### Test Installation

#### Python

```python
import tusktsk
print(tusktsk.__version__)

# Test basic functionality
config = tusktsk.load_config("peanut.tsk")
print(config)
```

#### JavaScript

```javascript
const tusktsk = require('tusktsk');
console.log(tusktsk.version);

// Test basic functionality
const config = tusktsk.loadConfig('peanut.tsk');
console.log(config);
```

#### Go

```go
package main

import (
    "fmt"
    "github.com/cyber-boost/tusktsk/sdk/go"
)

func main() {
    fmt.Println("TuskTsk Go SDK loaded")
    
    // Test basic functionality
    config, err := tusktsk.LoadConfig("peanut.tsk")
    if err != nil {
        panic(err)
    }
    fmt.Println(config)
}
```

#### Rust

```rust
use tusktsk;

fn main() {
    println!("TuskTsk Rust SDK loaded");
    
    // Test basic functionality
    let config = tusktsk::load_config("peanut.tsk").unwrap();
    println!("{:?}", config);
}
```

#### C#

```csharp
using TuskTsk;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("TuskTsk C# SDK loaded");
        
        // Test basic functionality
        var config = TuskTsk.LoadConfig("peanut.tsk");
        Console.WriteLine(config);
    }
}
```

#### PHP

```php
<?php

require_once 'vendor/autoload.php';

use TuskLang\TuskTsk;

echo "TuskTsk PHP SDK loaded\n";

// Test basic functionality
$config = TuskTsk::loadConfig('peanut.tsk');
print_r($config);
```

#### Ruby

```ruby
require 'tusktsk'

puts "TuskTsk Ruby SDK loaded"

# Test basic functionality
config = TuskTsk.load_config('peanut.tsk')
puts config
```

## CLI Usage

### Python

```bash
# Install CLI
pip install tusktsk[cli]

# Use CLI
tusk --version
tusk parse config.tsk
tusk validate config.tsk
```

### JavaScript

```bash
# Install CLI
npm install -g tusktsk

# Use CLI
tsk --version
tsk parse config.tsk
tsk validate config.tsk
```

### Go

```bash
# Build CLI
cd sdk/go
go build -o tusktsk-go

# Use CLI
./tusktsk-go version
./tusktsk-go parse config.tsk
./tusktsk-go validate config.tsk
```

### Rust

```bash
# Install CLI
cargo install tusktsk

# Use CLI
tusk-rust --version
tusk-rust parse config.tsk
tusk-rust validate config.tsk
```

### C#

```bash
# Build CLI
cd sdk/csharp
dotnet build
dotnet run -- --version
dotnet run -- parse config.tsk
dotnet run -- validate config.tsk
```

### PHP

```bash
# Install CLI
composer require tusktsk/tusktsk

# Use CLI
php vendor/bin/tusktsk --version
php vendor/bin/tusktsk parse config.tsk
php vendor/bin/tusktsk validate config.tsk
```

### Ruby

```bash
# Install CLI
gem install tusktsk

# Use CLI
tusktsk --version
tusktsk parse config.tsk
tusktsk validate config.tsk
```

## Troubleshooting

### Common Issues

1. **Permission Denied**
   ```bash
   # Fix permissions
   sudo chown -R $USER:$USER ~/.local
   sudo chown -R $USER:$USER ~/.npm
   sudo chown -R $USER:$USER ~/.cargo
   ```

2. **Network Issues**
   ```bash
   # Check connectivity
   curl -I https://github.com/cyber-boost/tusktsk
   
   # Configure proxy if needed
   export HTTP_PROXY=http://proxy:port
   export HTTPS_PROXY=http://proxy:port
   ```

3. **Dependency Conflicts**
   ```bash
   # Python: Use virtual environment
   python -m venv venv
   source venv/bin/activate
   pip install tusktsk
   
   # Node.js: Clear cache
   npm cache clean --force
   
   # Go: Clean modules
   go clean -modcache
   ```

4. **Build Failures**
   ```bash
   # Install build tools
   # Ubuntu/Debian
   sudo apt-get install build-essential
   
   # macOS
   xcode-select --install
   
   # Windows
   # Install Visual Studio Build Tools
   ```

### Getting Help

- **Documentation**: https://tuskt.sk/docs
- **GitHub Issues**: https://github.com/cyber-boost/tusktsk/issues
- **Community**: https://tuskt.sk/community
- **Email Support**: packages@tuskt.sk

## Uninstallation

### Python

```bash
pip uninstall tusktsk
```

### JavaScript

```bash
npm uninstall tusktsk
npm uninstall -g tusktsk
```

### Go

```bash
go clean -i github.com/cyber-boost/tusktsk/sdk/go
```

### Rust

```bash
cargo uninstall tusktsk
```

### C#

```bash
dotnet remove package TuskTsk
```

### PHP

```bash
composer remove tusktsk/tusktsk
```

### Ruby

```bash
gem uninstall tusktsk
```

### Docker

```bash
# Remove containers
docker-compose down

# Remove images
docker rmi ghcr.io/cyber-boost/tusktsk-python
docker rmi ghcr.io/cyber-boost/tusktsk-javascript
# ... repeat for other images

# Remove volumes
docker volume prune
``` 