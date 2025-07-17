# Installation Guide - PHP CLI

Complete installation instructions for the TuskLang PHP CLI.

## Prerequisites

### System Requirements

- **PHP**: 8.1 or higher
- **Operating System**: Linux, macOS, or Windows
- **Memory**: 128MB RAM minimum
- **Disk Space**: 50MB for installation

### PHP Extensions

Required extensions:
- `pdo` - Database operations
- `json` - JSON processing
- `mbstring` - String handling

Optional extensions (for enhanced performance):
- `msgpack` - MessagePack serialization
- `curl` - HTTP requests
- `openssl` - SSL/TLS support

### Check PHP Installation

```bash
# Check PHP version
php --version

# Check available extensions
php -m | grep -E "(pdo|json|mbstring|msgpack|curl|openssl)"
```

## Installation Methods

### Method 1: Composer (Recommended)

```bash
# Install via Composer
composer require tusklang/tusklang

# Verify installation
vendor/bin/tsk --version
```

### Method 2: Global Installation

```bash
# Download and install globally
curl -sSL tusklang.org/tsk.sh | sudo bash

# Verify installation
tsk --version
```

### Method 3: Manual Installation

```bash
# Clone the repository
git clone https://github.com/tuskphp/tusklang.git
cd tusklang/sdk/php

# Install dependencies
composer install

# Make CLI executable
chmod +x bin/tsk

# Add to PATH (optional)
sudo ln -s $(pwd)/bin/tsk /usr/local/bin/tsk
```

### Method 4: Docker

```bash
# Pull the official image
docker pull tusklang/php-cli:latest

# Run CLI commands
docker run --rm -v $(pwd):/app tusklang/php-cli tsk --version
```

## Configuration

### Environment Setup

```bash
# Set environment variables
export TUSKLANG_HOME=/opt/tusklang
export TUSKLANG_CONFIG=/etc/tusklang/config

# Add to shell profile
echo 'export TUSKLANG_HOME=/opt/tusklang' >> ~/.bashrc
echo 'export TUSKLANG_CONFIG=/etc/tusklang/config' >> ~/.bashrc
```

### Configuration Files

Create initial configuration:

```bash
# Create config directory
mkdir -p /etc/tusklang/config

# Create default configuration
cat > /etc/tusklang/config/default.tsk << 'EOF'
[cli]
version: "2.0.0"
debug: false
verbose: false

[paths]
home: "/opt/tusklang"
config: "/etc/tusklang/config"
cache: "/var/cache/tusklang"

[features]
auto_compile: true
file_watching: true
binary_support: true
EOF
```

## Verification

### Basic Functionality

```bash
# Check version
tsk version

# Check system status
tsk status

# Test configuration parsing
echo '[test] value: "hello"' > test.tsk
tsk parse test.tsk
rm test.tsk
```

### Database Connectivity

```bash
# Test database connection
tsk db status

# Check available databases
tsk db list
```

### Service Management

```bash
# Check service status
tsk services status

# List available services
tsk services list
```

## Troubleshooting

### Common Issues

#### PHP Version Too Old

```bash
# Error: PHP 8.1+ required
# Solution: Upgrade PHP

# Ubuntu/Debian
sudo apt update
sudo apt install software-properties-common
sudo add-apt-repository ppa:ondrej/php
sudo apt install php8.2 php8.2-cli php8.2-common

# macOS
brew install php@8.2
brew link php@8.2

# Windows
# Download from https://windows.php.net/download/
```

#### Missing Extensions

```bash
# Install required extensions

# Ubuntu/Debian
sudo apt install php8.2-pdo php8.2-json php8.2-mbstring

# macOS
brew install php@8.2
# Extensions are included by default

# Manual compilation
pecl install msgpack
```

#### Permission Issues

```bash
# Fix permissions
sudo chown -R $USER:$USER /opt/tusklang
sudo chmod -R 755 /opt/tusklang

# Fix CLI permissions
sudo chmod +x /usr/local/bin/tsk
```

#### PATH Issues

```bash
# Add to PATH
echo 'export PATH="/opt/tusklang/bin:$PATH"' >> ~/.bashrc
source ~/.bashrc

# Or create symlink
sudo ln -s /opt/tusklang/bin/tsk /usr/local/bin/tsk
```

### Debug Mode

```bash
# Enable debug output
export TUSKLANG_DEBUG=1
tsk --debug status

# Check logs
tail -f /var/log/tusklang/cli.log
```

### Getting Help

```bash
# General help
tsk help

# Command-specific help
tsk db --help
tsk config --help

# Debug information
tsk --debug version
```

## Next Steps

After successful installation:

1. **Read the Quick Start Guide** - [Quick Start](./quickstart.md)
2. **Explore Commands** - [Command Reference](./commands/README.md)
3. **Try Examples** - [Examples](./examples/README.md)
4. **Configure Your Environment** - [Configuration Guide](./commands/config/README.md)

## Support

If you encounter issues:

1. Check the [Troubleshooting Guide](./troubleshooting.md)
2. Search [GitHub Issues](https://github.com/tuskphp/tusklang/issues)
3. Ask the [Community](https://community.tusklang.org)
4. Review the [Documentation](https://docs.tusklang.org)

**Strong. Secure. Scalable.** 🐘 