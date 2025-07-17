# TuskLang Installation Guide

## Quick Installation

### Python SDK
```bash
# Using pip
pip install tusklang

# Using conda
conda install -c conda-forge tusklang

# From source
git clone https://github.com/tusklang/tusklang.git
cd tusklang/implementations/python
pip install -e .
```

### Go SDK
```bash
# Using go get
go get github.com/tusklang/go-sdk

# Using go mod
go mod init myproject
go get github.com/tusklang/go-sdk@latest
```

### Rust SDK
```bash
# Using cargo
cargo add tusklang

# From source
git clone https://github.com/tusklang/tusklang.git
cd tusklang/implementations/rust
cargo build --release
```

### JavaScript SDK
```bash
# Using npm
npm install tusklang

# Using yarn
yarn add tusklang

# Using pnpm
pnpm add tusklang
```

## System Requirements

### Python
- **Python**: 3.8 or higher
- **Dependencies**: cryptography, requests, zstandard
- **Platforms**: Windows, macOS, Linux

### Go
- **Go**: 1.19 or higher
- **Dependencies**: crypto/ed25519, crypto/aes
- **Platforms**: Windows, macOS, Linux, BSD

### Rust
- **Rust**: 1.70 or higher
- **Dependencies**: ed25519-dalek, aes-gcm
- **Platforms**: Windows, macOS, Linux, BSD

### JavaScript
- **Node.js**: 16 or higher
- **Browsers**: Chrome 90+, Firefox 88+, Safari 14+
- **Dependencies**: crypto-js, zlib

## Platform-Specific Installation

### Ubuntu/Debian
```bash
# Update package list
sudo apt update

# Install Python dependencies
sudo apt install python3-pip python3-cryptography

# Install TuskLang
pip3 install tusklang

# Install CLI tool
pip3 install tusklang-cli
```

### CentOS/RHEL/Fedora
```bash
# Install Python dependencies
sudo dnf install python3-pip python3-cryptography

# Install TuskLang
pip3 install tusklang

# Install CLI tool
pip3 install tusklang-cli
```

### macOS
```bash
# Using Homebrew
brew install python3
pip3 install tusklang

# Using MacPorts
sudo port install py39-pip
pip3 install tusklang
```

### Windows
```bash
# Using Chocolatey
choco install python
pip install tusklang

# Using Scoop
scoop install python
pip install tusklang

# Manual installation
# Download Python from python.org
# Run: pip install tusklang
```

### Docker
```dockerfile
# Dockerfile example
FROM python:3.11-slim

RUN pip install tusklang tusklang-cli

WORKDIR /app
COPY . .

CMD ["python", "app.py"]
```

```bash
# Build and run
docker build -t tusklang-app .
docker run -it tusklang-app
```

## Development Installation

### Prerequisites
```bash
# Install development tools
sudo apt install build-essential git python3-dev

# Install Python development dependencies
pip install pytest black flake8 mypy

# Install Go development tools
go install golang.org/x/tools/cmd/goimports@latest
go install github.com/golangci/golangci-lint/cmd/golangci-lint@latest

# Install Rust development tools
rustup component add rustfmt clippy
```

### From Source
```bash
# Clone repository
git clone https://github.com/tusklang/tusklang.git
cd tusklang

# Python development setup
cd implementations/python
pip install -e ".[dev]"
pytest

# Go development setup
cd ../go
go mod tidy
go test ./...

# Rust development setup
cd ../rust
cargo test
cargo clippy
```

## CLI Tool Installation

### Global Installation
```bash
# Python
pip install tusklang-cli

# Go
go install github.com/tusklang/cli@latest

# Verify installation
tusk --version
```

### Local Installation
```bash
# Python virtual environment
python -m venv venv
source venv/bin/activate  # Linux/macOS
venv\Scripts\activate     # Windows
pip install tusklang-cli

# Go workspace
go work init
go work use ./cli
go install ./cli
```

## Configuration Setup

### Initial Configuration
```bash
# Initialize TuskLang
tusk init

# Generate keys
tusk keys generate

# Create default config
tusk config create
```

### Environment Variables
```bash
# Set environment variables
export TUSKLANG_HOME=/opt/tusklang
export TUSKLANG_CONFIG=/etc/tusklang/config.pnt
export TUSKLANG_KEYS=/opt/tusklang/keys

# Windows
set TUSKLANG_HOME=C:\tusklang
set TUSKLANG_CONFIG=C:\tusklang\config.pnt
```

## Verification

### Test Installation
```python
# Python
python3 -c "import tusklang; print('TuskLang installed successfully!')"

# Go
go run -c "import 'github.com/tusklang/go-sdk'; fmt.Println('TuskLang installed successfully!')"

# Rust
cargo run --example hello
```

### CLI Verification
```bash
# Check version
tusk --version

# Test basic functionality
tusk validate --help

# Create test configuration
echo '{"test": "value"}' | tusk convert --input - --output test.pnt
tusk validate test.pnt
```

## Troubleshooting

### Common Issues

#### Python Import Errors
```bash
# Check Python version
python3 --version

# Reinstall with --force-reinstall
pip install --force-reinstall tusklang

# Check installation location
python3 -c "import tusklang; print(tusklang.__file__)"
```

#### Go Module Issues
```bash
# Clean module cache
go clean -modcache

# Update dependencies
go mod tidy

# Check Go version
go version
```

#### Rust Compilation Errors
```bash
# Update Rust
rustup update

# Clean and rebuild
cargo clean
cargo build

# Check Rust version
rustc --version
```

#### Permission Issues
```bash
# Fix permissions (Linux/macOS)
sudo chown -R $USER:$USER ~/.local/lib/python*/site-packages/tusklang*

# Use virtual environment
python3 -m venv venv
source venv/bin/activate
pip install tusklang
```

### Getting Help

1. **Check Documentation**: Review this guide and API docs
2. **Search Issues**: Look for similar problems on GitHub
3. **Community Support**: Ask in GitHub Discussions
4. **Email Support**: Contact support@tusklang.org

## Next Steps

After installation:

1. **Read Quick Start**: [Quick Start Guide](quick-start.md)
2. **Try Examples**: [Examples](examples.md)
3. **Learn API**: [API Reference](api.md)
4. **Join Community**: [GitHub Discussions](https://github.com/tusklang/tusklang/discussions)

---

*Need help? Check our [Troubleshooting Guide](troubleshooting.md) or contact [support@tusklang.org](mailto:support@tusklang.org)* 