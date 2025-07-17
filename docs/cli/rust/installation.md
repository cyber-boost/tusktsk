# Installation Guide

Complete installation guide for TuskLang Rust CLI.

## Prerequisites

### System Requirements

- **Operating System**: Linux, macOS, or Windows
- **Rust**: 1.70+ (for async/await support)
- **Cargo**: Latest stable version
- **Memory**: 50MB available RAM
- **Disk Space**: 10MB for installation

### Rust Installation

If you don't have Rust installed:

```bash
# Install Rust using rustup
curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh

# Or on Windows
# Download from https://rustup.rs/

# Reload shell environment
source ~/.cargo/env

# Verify installation
rustc --version
cargo --version
```

## Installation Methods

### Method 1: Install from Crates.io (Recommended)

```bash
# Install the latest stable version
cargo install tusklang-rust

# Verify installation
tusk-rust --version
```

### Method 2: Build from Source

```bash
# Clone the repository
git clone https://github.com/tusklang/tusklang-rust.git
cd tusklang-rust

# Build in release mode
cargo build --release

# Install globally
cargo install --path .

# Verify installation
tusk-rust --version
```

### Method 3: Development Installation

```bash
# Clone the repository
git clone https://github.com/tusklang/tusklang-rust.git
cd tusklang-rust

# Build in development mode
cargo build

# Run directly from build directory
./target/debug/tusk-rust --version

# Or install for development
cargo install --path . --force
```

## Platform-Specific Instructions

### Linux

```bash
# Ubuntu/Debian
sudo apt update
sudo apt install build-essential curl
curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh
source ~/.cargo/env
cargo install tusklang-rust

# CentOS/RHEL/Fedora
sudo dnf install gcc curl
curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh
source ~/.cargo/env
cargo install tusklang-rust

# Arch Linux
sudo pacman -S rust cargo
cargo install tusklang-rust
```

### macOS

```bash
# Using Homebrew
brew install rust
cargo install tusklang-rust

# Using rustup (recommended)
curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh
source ~/.cargo/env
cargo install tusklang-rust
```

### Windows

```powershell
# Using Chocolatey
choco install rust
cargo install tusklang-rust

# Using rustup (recommended)
# Download rustup-init.exe from https://rustup.rs/
# Run the installer and follow the prompts
cargo install tusklang-rust
```

## Verification

After installation, verify that everything works:

```bash
# Check version
tusk-rust --version

# Check help
tusk-rust --help

# Test basic functionality
echo 'app_name: "Test App"' > test.tsk
tusk-rust utility parse test.tsk

# Clean up test file
rm test.tsk
```

Expected output:
```
tusk-rust 0.1.0
Ultra-fast Rust TuskLang parser and CLI tool

✅ Successfully parsed 'test.tsk'
📄 File contents:
app_name: "Test App"
```

## Configuration Setup

### Create Initial Configuration

```bash
# Create a basic configuration file
cat > peanu.peanuts << 'EOF'
[app]
name: "My Rust App"
version: "1.0.0"
debug: false

[server]
host: "localhost"
port: 8080
timeout: 30

[database]
url: "postgresql://localhost/myapp"
pool_size: 10
EOF

# Test the configuration
tusk-rust config get app.name
tusk-rust utility validate peanu.peanuts
```

### Environment Setup

```bash
# Add to your shell profile (~/.bashrc, ~/.zshrc, etc.)
export TUSKLANG_CONFIG_PATH="$HOME/.tusklang"
export TUSKLANG_CACHE_PATH="$HOME/.cache/tusklang"

# Create configuration directories
mkdir -p "$TUSKLANG_CONFIG_PATH"
mkdir -p "$TUSKLANG_CACHE_PATH"

# Create global configuration
cat > "$TUSKLANG_CONFIG_PATH/config.tsk" << 'EOF'
# Global TuskLang configuration
defaults:
  timeout: 30s
  retries: 3
  log_level: "info"

paths:
  config: "./config"
  cache: "./cache"
  logs: "./logs"
EOF
```

## Dependencies

### Required Dependencies

The following dependencies are automatically installed with the Rust CLI:

- **clap**: Command-line argument parsing
- **serde**: Serialization/deserialization
- **nom**: Parser combinators
- **dirs**: Cross-platform directory handling
- **tokio**: Async runtime (optional)

### Optional Dependencies

For enhanced functionality:

```bash
# Install additional features
cargo install tusklang-rust --features full

# Features include:
# - wasm: WebAssembly support
# - full: All features enabled
```

## Troubleshooting

### Common Issues

#### Command Not Found

```bash
# Check if cargo bin is in PATH
echo $PATH | grep cargo

# Add cargo bin to PATH
export PATH="$HOME/.cargo/bin:$PATH"

# Reinstall if needed
cargo install --force tusklang-rust
```

#### Build Errors

```bash
# Update Rust toolchain
rustup update

# Clean and rebuild
cargo clean
cargo build

# Check for system dependencies
# Linux: sudo apt install build-essential
# macOS: xcode-select --install
# Windows: Install Visual Studio Build Tools
```

#### Permission Errors

```bash
# Fix cargo permissions
sudo chown -R $USER:$USER ~/.cargo

# Or install for current user only
cargo install --user tusklang-rust
```

#### Memory Issues

```bash
# Increase memory limits for large files
export RUSTFLAGS="-C link-arg=-Wl,-rpath,$(rustc --print sysroot)/lib"

# Or use release build
cargo install --release tusklang-rust
```

### Platform-Specific Issues

#### Linux

```bash
# Install required system libraries
sudo apt install pkg-config libssl-dev

# Fix linking issues
export PKG_CONFIG_PATH="/usr/lib/x86_64-linux-gnu/pkgconfig"
```

#### macOS

```bash
# Install Xcode command line tools
xcode-select --install

# Fix OpenSSL issues
export OPENSSL_ROOT_DIR=$(brew --prefix openssl)
export OPENSSL_LIB_DIR=$(brew --prefix openssl)/lib
```

#### Windows

```powershell
# Install Visual Studio Build Tools
# Download from: https://visualstudio.microsoft.com/downloads/

# Fix PATH issues
$env:PATH += ";$env:USERPROFILE\.cargo\bin"

# Use PowerShell for better compatibility
```

## Uninstallation

```bash
# Remove the binary
cargo uninstall tusklang-rust

# Remove configuration files
rm -rf ~/.tusklang
rm -rf ~/.cache/tusklang

# Remove from PATH (if manually added)
# Edit ~/.bashrc, ~/.zshrc, etc. and remove the export lines
```

## Next Steps

After successful installation:

1. **Read the [Quick Start Guide](./quickstart.md)** to get started
2. **Explore [Command Reference](./commands/README.md)** for available commands
3. **Check [Examples](./examples/README.md)** for usage patterns
4. **Review [Troubleshooting](./troubleshooting.md)** for common issues

## Support

If you encounter issues during installation:

- **Documentation**: [https://tusklang.org/docs/rust](https://tusklang.org/docs/rust)
- **GitHub Issues**: [https://github.com/tusklang/tusklang-rust/issues](https://github.com/tusklang/tusklang-rust/issues)
- **Discord Community**: [https://discord.gg/tusklang](https://discord.gg/tusklang)
- **Email Support**: support@tusklang.org 