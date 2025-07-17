# TuskLang Troubleshooting Guide

## Quick Diagnosis

### Check Installation
```bash
# Python
python3 -c "import tusklang; print('✓ TuskLang installed')"

# Go
go list -m github.com/tusklang/go-sdk

# Rust
cargo list | grep tusklang

# CLI
tusk --version
```

### Check Configuration
```bash
# Validate configuration file
tusk validate config.pnt

# Check file integrity
tusk info config.pnt

# Test basic operations
tusk test
```

## Common Issues

### Installation Problems

#### Python Import Errors
**Symptoms**: `ModuleNotFoundError: No module named 'tusklang'`

**Solutions**:
```bash
# Reinstall package
pip uninstall tusklang
pip install tusklang

# Check Python version (requires 3.8+)
python3 --version

# Use virtual environment
python3 -m venv venv
source venv/bin/activate  # Linux/macOS
pip install tusklang

# Check installation location
python3 -c "import tusklang; print(tusklang.__file__)"
```

#### Go Module Issues
**Symptoms**: `cannot find module "github.com/tusklang/go-sdk"`

**Solutions**:
```bash
# Clean module cache
go clean -modcache

# Update dependencies
go mod tidy

# Check Go version (requires 1.19+)
go version

# Initialize module
go mod init myproject
go get github.com/tusklang/go-sdk@latest
```

#### Rust Compilation Errors
**Symptoms**: `error: failed to compile 'tusklang'`

**Solutions**:
```bash
# Update Rust
rustup update

# Clean and rebuild
cargo clean
cargo build

# Check Rust version (requires 1.70+)
rustc --version

# Update dependencies
cargo update
```

#### Permission Errors
**Symptoms**: `Permission denied` or `Access denied`

**Solutions**:
```bash
# Fix Python permissions
sudo chown -R $USER:$USER ~/.local/lib/python*/site-packages/tusklang*

# Use user installation
pip install --user tusklang

# Fix Go permissions
sudo chown -R $USER:$USER ~/go/pkg/mod/github.com/tusklang

# Use virtual environment
python3 -m venv venv
source venv/bin/activate
pip install tusklang
```

### Configuration Issues

#### File Not Found
**Symptoms**: `FileNotFoundError: [Errno 2] No such file or directory`

**Solutions**:
```bash
# Check file exists
ls -la config.pnt

# Check file path
pwd
ls -la /path/to/config.pnt

# Create default configuration
tusk config create

# Use absolute path
tusk validate /full/path/to/config.pnt
```

#### Invalid File Format
**Symptoms**: `InvalidFormatError: Not a valid TuskLang file`

**Solutions**:
```bash
# Check file type
file config.pnt

# Validate file
tusk validate config.pnt

# Check file header
hexdump -C config.pnt | head -5

# Recreate file if corrupted
tusk config create --output config.pnt
```

#### Corrupted File
**Symptoms**: `CorruptionError: File is corrupted`

**Solutions**:
```bash
# Check file integrity
tusk info config.pnt

# Try to recover
tusk recover config.pnt --output recovered.pnt

# Restore from backup
cp config.pnt.backup config.pnt

# Recreate from source
tusk config create --from-source
```

### Security Issues

#### Encryption Errors
**Symptoms**: `EncryptionError: Failed to decrypt file`

**Solutions**:
```bash
# Check password
tusk decrypt config.pnt --password "correct-password"

# Reset encryption
tusk encrypt config.pnt --new-password "new-password"

# Check key files
ls -la ~/.tusklang/keys/

# Regenerate keys
tusk keys generate --force
```

#### Signature Errors
**Symptoms**: `SignatureError: Digital signature verification failed`

**Solutions**:
```bash
# Check signature
tusk verify config.pnt

# Re-sign file
tusk sign config.pnt

# Check public key
tusk keys list

# Regenerate signing keys
tusk keys generate --type signature
```

#### Key Management Issues
**Symptoms**: `KeyError: Cryptographic key not found`

**Solutions**:
```bash
# List available keys
tusk keys list

# Generate missing keys
tusk keys generate

# Import keys
tusk keys import --file key.pem

# Check key permissions
ls -la ~/.tusklang/keys/
chmod 600 ~/.tusklang/keys/*
```

### Performance Issues

#### Slow Loading
**Symptoms**: Configuration takes >100ms to load

**Solutions**:
```bash
# Check file size
ls -lh config.pnt

# Enable compression
tusk optimize config.pnt --compress

# Use binary format
tusk convert config.json --output config.pnt

# Profile loading
tusk profile --load config.pnt
```

#### High Memory Usage
**Symptoms**: Memory usage >50MB for small configs

**Solutions**:
```python
# Use streaming for large files
from tusklang import StreamingConfig
config = StreamingConfig("large-config.pnt")

# Load only needed sections
config.load_section("app")
config.load_section("database")
```

#### Slow Validation
**Symptoms**: Schema validation takes >10ms

**Solutions**:
```bash
# Optimize schema
tusk schema optimize schema.json

# Use lazy validation
tusk validate config.pnt --lazy

# Profile validation
tusk profile --validate config.pnt
```

### Network Issues

#### Download Failures
**Symptoms**: `DownloadError: Failed to download package`

**Solutions**:
```bash
# Check network connectivity
ping github.com

# Use different package source
pip install tusklang --index-url https://pypi.org/simple/

# Use proxy if needed
pip install tusklang --proxy http://proxy.company.com:8080

# Download manually
wget https://github.com/tusklang/tusklang/releases/latest/download/tusklang.tar.gz
```

#### API Connection Issues
**Symptoms**: `ConnectionError: Failed to connect to API`

**Solutions**:
```bash
# Check API endpoint
curl https://api.tusklang.org/health

# Use different endpoint
export TUSKLANG_API_URL=https://api2.tusklang.org

# Check firewall
sudo ufw status

# Use VPN if needed
```

### Platform-Specific Issues

#### Windows Issues
**Symptoms**: Path-related errors or permission issues

**Solutions**:
```cmd
# Use PowerShell with admin rights
Start-Process PowerShell -Verb RunAs

# Fix path issues
set PATH=%PATH%;C:\Python39\Scripts

# Use Windows Subsystem for Linux
wsl --install
wsl
pip install tusklang
```

#### macOS Issues
**Symptoms**: Permission errors or missing dependencies

**Solutions**:
```bash
# Fix Homebrew permissions
sudo chown -R $(whoami) /usr/local/bin /usr/local/lib

# Install Xcode command line tools
xcode-select --install

# Use pyenv for Python management
brew install pyenv
pyenv install 3.11.0
pyenv global 3.11.0
pip install tusklang
```

#### Linux Issues
**Symptoms**: Library dependencies or system conflicts

**Solutions**:
```bash
# Install system dependencies
sudo apt update
sudo apt install python3-dev build-essential libssl-dev

# Use system Python
sudo apt install python3-tusklang

# Fix library paths
export LD_LIBRARY_PATH=/usr/local/lib:$LD_LIBRARY_PATH
```

## Debugging Tools

### CLI Debug Mode
```bash
# Enable debug output
tusk --debug validate config.pnt

# Verbose logging
tusk --verbose --log-level DEBUG

# Show detailed info
tusk info config.pnt --detailed
```

### Python Debugging
```python
import logging
logging.basicConfig(level=logging.DEBUG)

from tusklang import TuskConfig
config = TuskConfig()

# Enable debug mode
config.debug = True

# Check internal state
print(config._data)
print(config._schema)
```

### Go Debugging
```go
import "log"

// Enable debug logging
log.SetFlags(log.LstdFlags | log.Lshortfile)

// Debug configuration
config.Debug = true
log.Printf("Config data: %+v", config.Data())
```

### Rust Debugging
```rust
use log::{debug, info};

// Enable debug logging
env_logger::init();

// Debug configuration
debug!("Config data: {:?}", config.data());
info!("Loading configuration from: {}", path);
```

## Performance Tuning

### Optimization Commands
```bash
# Optimize configuration file
tusk optimize config.pnt

# Compress large files
tusk compress config.pnt --algorithm lz4

# Profile performance
tusk profile --benchmark

# Memory usage analysis
tusk profile --memory
```

### Configuration Best Practices
```python
# Use efficient data types
config.set("numbers", [1, 2, 3])  # Good
config.set("numbers", "1,2,3")    # Avoid

# Minimize nested structures
config.set("app.name", "MyApp")   # Good
config.set("app.details.name", "MyApp")  # Avoid deep nesting

# Use appropriate validation
schema = Schema({
    "port": {"type": "integer", "min": 1, "max": 65535}
})
```

## Getting Help

### Self-Service Resources
1. **Documentation**: [docs.tusklang.org](https://docs.tusklang.org)
2. **Examples**: [github.com/tusklang/examples](https://github.com/tusklang/examples)
3. **FAQ**: [docs.tusklang.org/faq](https://docs.tusklang.org/faq)

### Community Support
1. **GitHub Discussions**: [github.com/tusklang/tusklang/discussions](https://github.com/tusklang/tusklang/discussions)
2. **Discord**: [discord.gg/tusklang](https://discord.gg/tusklang)
3. **Stack Overflow**: Tag questions with `tusklang`

### Professional Support
1. **Email Support**: [support@tusklang.org](mailto:support@tusklang.org)
2. **Commercial Support**: [zoo@phptu.sk](mailto:zoo@phptu.sk)
3. **Security Issues**: [security@tusklang.org](mailto:security@tusklang.org)

### Bug Reports
When reporting bugs, include:
- TuskLang version: `tusk --version`
- Operating system: `uname -a`
- Python/Go/Rust version
- Error message and stack trace
- Steps to reproduce
- Configuration file (if relevant)

### Feature Requests
For feature requests:
- Describe the use case
- Explain the expected behavior
- Provide examples if possible
- Check if similar features exist

---

*Still having issues? Contact [support@tusklang.org](mailto:support@tusklang.org) with detailed information about your problem.* 