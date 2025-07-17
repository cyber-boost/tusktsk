# Installation Guide for TuskLang Go CLI

This guide covers installing and setting up the TuskLang Go CLI on various platforms.

## Prerequisites

### System Requirements

- **Operating System**: Linux, macOS, or Windows
- **Go Version**: 1.21 or higher
- **Memory**: 512MB RAM minimum, 2GB recommended
- **Disk Space**: 100MB for installation

### Required Software

1. **Go Programming Language**
   ```bash
   # Check Go version
   go version
   
   # Should output: go version go1.21.x linux/amd64 (or similar)
   ```

2. **Git** (for cloning repository)
   ```bash
   # Check Git version
   git --version
   ```

3. **Make** (for build automation)
   ```bash
   # Check Make version
   make --version
   ```

## Installation Methods

### Method 1: From Source (Recommended)

#### Step 1: Clone Repository

```bash
# Clone the TuskLang Go SDK
git clone https://github.com/tusklang/go-sdk.git
cd go-sdk
```

#### Step 2: Install Dependencies

```bash
# Download and install dependencies
go mod tidy
go mod download
```

#### Step 3: Build and Install

```bash
# Build the CLI
make build

# Install globally (requires sudo on Linux/macOS)
make install

# Or install to user directory
make install-user
```

#### Step 4: Verify Installation

```bash
# Check if tsk command is available
tsk version

# Should output: TuskLang Go CLI v2.0.0
```

### Method 2: Using Go Install

```bash
# Install directly from GitHub
go install github.com/tusklang/go-sdk/cmd/tsk@latest

# Verify installation
tsk version
```

### Method 3: Using Package Managers

#### Linux (Ubuntu/Debian)

```bash
# Add repository (if available)
curl -fsSL https://tusklang.org/install.sh | sudo bash

# Install package
sudo apt update
sudo apt install tusklang-go-cli
```

#### macOS (Homebrew)

```bash
# Add tap (if available)
brew tap tusklang/tap

# Install package
brew install tusklang-go-cli
```

#### Windows (Chocolatey)

```powershell
# Install package (if available)
choco install tusklang-go-cli
```

## Platform-Specific Instructions

### Linux

#### Ubuntu/Debian

```bash
# Install dependencies
sudo apt update
sudo apt install git make gcc

# Install Go (if not already installed)
wget https://go.dev/dl/go1.21.0.linux-amd64.tar.gz
sudo tar -C /usr/local -xzf go1.21.0.linux-amd64.tar.gz

# Add Go to PATH
echo 'export PATH=$PATH:/usr/local/go/bin' >> ~/.bashrc
source ~/.bashrc

# Install TuskLang CLI
git clone https://github.com/tusklang/go-sdk.git
cd go-sdk
make install
```

#### CentOS/RHEL/Fedora

```bash
# Install dependencies
sudo yum install git make gcc
# or for newer versions:
sudo dnf install git make gcc

# Install Go (if not already installed)
wget https://go.dev/dl/go1.21.0.linux-amd64.tar.gz
sudo tar -C /usr/local -xzf go1.21.0.linux-amd64.tar.gz

# Add Go to PATH
echo 'export PATH=$PATH:/usr/local/go/bin' >> ~/.bashrc
source ~/.bashrc

# Install TuskLang CLI
git clone https://github.com/tusklang/go-sdk.git
cd go-sdk
make install
```

### macOS

#### Using Homebrew

```bash
# Install Go
brew install go

# Install TuskLang CLI
git clone https://github.com/tusklang/go-sdk.git
cd go-sdk
make install
```

#### Manual Installation

```bash
# Install Go
wget https://go.dev/dl/go1.21.0.darwin-amd64.tar.gz
sudo tar -C /usr/local -xzf go1.21.0.darwin-amd64.tar.gz

# Add Go to PATH
echo 'export PATH=$PATH:/usr/local/go/bin' >> ~/.zshrc
source ~/.zshrc

# Install TuskLang CLI
git clone https://github.com/tusklang/go-sdk.git
cd go-sdk
make install
```

### Windows

#### Using Chocolatey

```powershell
# Install Go
choco install golang

# Install TuskLang CLI
git clone https://github.com/tusklang/go-sdk.git
cd go-sdk
make install
```

#### Manual Installation

1. **Download Go**: Visit https://go.dev/dl/ and download the Windows installer
2. **Install Go**: Run the installer and follow the prompts
3. **Add to PATH**: Ensure Go is added to your system PATH
4. **Install TuskLang CLI**:
   ```powershell
   git clone https://github.com/tusklang/go-sdk.git
   cd go-sdk
   make install
   ```

## Configuration

### Environment Variables

Set these environment variables for optimal performance:

```bash
# Add to ~/.bashrc, ~/.zshrc, or system environment
export TUSKLANG_CACHE_DIR="$HOME/.tusklang/cache"
export TUSKLANG_CONFIG_DIR="$HOME/.tusklang/config"
export TUSKLANG_LOG_LEVEL="info"
```

### Initial Setup

```bash
# Initialize TuskLang configuration
tsk init

# This creates:
# - ~/.tusklang/config/
# - ~/.tusklang/cache/
# - Default configuration files
```

## Verification

### Basic Functionality

```bash
# Check version
tsk version

# Check help
tsk help

# Test database connection
tsk db status

# Test configuration loading
tsk config check .
```

### Advanced Verification

```bash
# Run all tests
tsk test all

# Check CLI performance
tsk binary benchmark test.tsk

# Validate configuration
tsk config validate .
```

## Troubleshooting

### Common Installation Issues

#### 1. "Command not found: tsk"

**Solution:**
```bash
# Check if binary exists
ls -la /usr/local/bin/tsk

# If not found, reinstall
cd go-sdk
make install

# Or add to PATH manually
export PATH=$PATH:$GOPATH/bin
```

#### 2. "Permission denied"

**Solution:**
```bash
# Use user installation
make install-user

# Or fix permissions
sudo chmod +x /usr/local/bin/tsk
```

#### 3. "Go modules disabled"

**Solution:**
```bash
# Enable Go modules
export GO111MODULE=on

# Or set globally
go env -w GO111MODULE=on
```

#### 4. "Network timeout during download"

**Solution:**
```bash
# Set Go proxy
export GOPROXY=https://goproxy.cn,direct

# Or use direct mode
export GOPROXY=direct
```

### Platform-Specific Issues

#### Linux

```bash
# Fix library dependencies
sudo apt install libc6-dev

# Fix permission issues
sudo chown -R $USER:$USER ~/.tusklang
```

#### macOS

```bash
# Fix Homebrew permissions
sudo chown -R $(whoami) /usr/local/bin /usr/local/lib /usr/local/sbin
chmod u+w /usr/local/bin /usr/local/lib /usr/local/sbin

# Fix Go installation
brew reinstall go
```

#### Windows

```powershell
# Fix PATH issues
$env:PATH += ";$env:GOPATH\bin"

# Fix Git line endings
git config --global core.autocrlf true
```

## Uninstallation

### Remove Binary

```bash
# Remove CLI binary
sudo rm /usr/local/bin/tsk

# Or if installed to user directory
rm ~/go/bin/tsk
```

### Remove Configuration

```bash
# Remove configuration files
rm -rf ~/.tusklang

# Remove cache
rm -rf ~/.cache/tusklang
```

### Remove Source Code

```bash
# Remove cloned repository
rm -rf ~/go-sdk
```

## Next Steps

After successful installation:

1. **Read the Quick Start Guide**: [Quick Start](./quickstart.md)
2. **Explore Commands**: [Command Reference](./commands/README.md)
3. **Try Examples**: [Examples](./examples/README.md)
4. **Configure Your Environment**: Set up your development environment

## Support

If you encounter issues during installation:

1. **Check the Troubleshooting section** above
2. **Review the logs**: `tsk --verbose version`
3. **Search existing issues**: [GitHub Issues](https://github.com/tusklang/go-sdk/issues)
4. **Create a new issue**: Include your platform, Go version, and error details

## Version History

| Version | Release Date | Changes |
|---------|--------------|---------|
| 2.0.0 | 2024-12-19 | Initial release with full CLI support |
| 1.0.0 | 2024-12-01 | Basic SDK functionality | 