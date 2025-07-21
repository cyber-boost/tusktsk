# 🐚 TuskLang Bash Installation Guide

**"We don't bow to any king" - Get TuskLang running in your shell**

Welcome to the revolutionary world of TuskLang for Bash! This guide will get you up and running with the most powerful configuration language ever created, designed specifically for shell scripting and system administration.

## 🚀 Quick Installation

### One-Line Install (Recommended)

```bash
# Direct install via curl
curl -sSL https://bash.tusklang.org/install.sh | bash

# Alternative via wget
wget -qO- https://bash.tusklang.org/install.sh | bash

# With sudo (if needed)
curl -sSL https://bash.tusklang.org/install.sh | sudo bash
```

### Manual Installation

```bash
# Download the TuskLang Bash SDK
wget https://github.com/cyber-boost/tusktsk/releases/latest/download/tusk-bash.sh

# Make executable
chmod +x tusk-bash.sh

# Install globally
sudo mv tusk-bash.sh /usr/local/bin/tusk

# Create symlink for convenience
sudo ln -sf /usr/local/bin/tusk /usr/local/bin/tusklang
```

### Package Manager Installation

#### Ubuntu/Debian
```bash
# Add TuskLang repository
curl -fsSL https://bash.tusklang.org/gpg | sudo gpg --dearmor -o /usr/share/keyrings/tusklang-archive-keyring.gpg
echo "deb [signed-by=/usr/share/keyrings/tusklang-archive-keyring.gpg] https://bash.tusklang.org/apt stable main" | sudo tee /etc/apt/sources.list.d/tusklang.list

# Install
sudo apt update
sudo apt install tusklang-bash
```

#### CentOS/RHEL/Fedora
```bash
# Add repository
sudo dnf config-manager --add-repo https://bash.tusklang.org/rpm/tusklang.repo

# Install
sudo dnf install tusklang-bash
```

## ✅ Verification

### Check Installation

```bash
# Verify TuskLang is installed
tusk --version

# Expected output:
# TuskLang Bash SDK v2.1.0
# "Configuration with a Heartbeat"

# Check available commands
tusk --help

# Test basic functionality
tusk parse --help
```

### Quick Test

```bash
# Create a test configuration
cat > test.tsk << 'EOF'
$app_name: "TuskLang Test"
$version: "1.0.0"

[server]
host: "localhost"
port: 8080
debug: true

[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", 5432)
EOF

# Parse the configuration
tusk parse test.tsk

# Get specific values
tusk get test.tsk server.host
tusk get test.tsk database.host
```

## 🔧 System Requirements

### Minimum Requirements
- **Bash**: 4.0 or higher
- **Operating System**: Linux, macOS, or WSL
- **Memory**: 128MB RAM
- **Disk Space**: 50MB free space

### Recommended Requirements
- **Bash**: 5.0 or higher
- **Operating System**: Ubuntu 20.04+, CentOS 8+, macOS 10.15+
- **Memory**: 512MB RAM
- **Disk Space**: 100MB free space

### Check Your System

```bash
# Check Bash version
bash --version

# Check available memory
free -h

# Check disk space
df -h

# Check if required tools are available
command -v curl >/dev/null 2>&1 || echo "curl not found"
command -v wget >/dev/null 2>&1 || echo "wget not found"
command -v jq >/dev/null 2>&1 || echo "jq not found (recommended)"
```

## 🐳 Docker Installation

### Quick Docker Setup

```bash
# Pull the official TuskLang image
docker pull tusklang/bash:latest

# Run TuskLang in container
docker run --rm -it -v $(pwd):/workspace tusklang/bash:latest

# Or run specific commands
docker run --rm -v $(pwd):/workspace tusklang/bash:latest tusk parse config.tsk
```

### Docker Compose Setup

```bash
# Create docker-compose.yml
cat > docker-compose.yml << 'EOF'
version: '3.8'
services:
  tusk:
    image: tusklang/bash:latest
    volumes:
      - .:/workspace
    working_dir: /workspace
    command: tusk watch config.tsk
    environment:
      - TUSK_DEBUG=1
EOF

# Run with Docker Compose
docker-compose up
```

## 🔒 Security Installation

### Secure Installation with Verification

```bash
# Download with signature verification
curl -sSL https://bash.tusklang.org/install.sh -o install.sh
curl -sSL https://bash.tusklang.org/install.sh.sig -o install.sh.sig

# Verify signature
gpg --verify install.sh.sig install.sh

# If verification passes, install
bash install.sh
```

### Isolated Installation

```bash
# Create isolated environment
mkdir ~/tusklang-isolated
cd ~/tusklang-isolated

# Download to isolated directory
curl -sSL https://bash.tusklang.org/install.sh | bash -s -- --prefix=~/tusklang-isolated

# Add to PATH only when needed
export PATH="$HOME/tusklang-isolated/bin:$PATH"
```

## 🛠️ Development Installation

### From Source

```bash
# Clone repository
git clone https://github.com/cyber-boost/tusktsk.git
cd tusklang/bash

# Install dependencies
sudo apt install bash jq curl wget

# Build and install
make install

# Or install locally
make install PREFIX=~/.local
```

### Development Environment

```bash
# Set up development environment
export TUSK_DEV=1
export TUSK_DEBUG=1
export TUSK_LOG_LEVEL=debug

# Install with development features
curl -sSL https://bash.tusklang.org/install.sh | bash -s -- --dev
```

## 🔧 Configuration

### Global Configuration

```bash
# Create global configuration directory
sudo mkdir -p /etc/tusklang

# Create global config
sudo tee /etc/tusklang/global.tsk << 'EOF'
[global]
debug: false
log_level: "info"
cache_dir: "/var/cache/tusklang"
config_dir: "/etc/tusklang"

[security]
verify_signatures: true
allow_remote_exec: false
max_file_size: "10MB"
EOF
```

### User Configuration

```bash
# Create user configuration
mkdir -p ~/.config/tusklang

cat > ~/.config/tusklang/user.tsk << 'EOF'
[user]
name: @env("USER")
home: @env("HOME")
editor: @env("EDITOR", "nano")

[preferences]
default_syntax: "traditional"
auto_backup: true
backup_count: 5
EOF
```

## 🚨 Troubleshooting

### Common Issues

#### Permission Denied
```bash
# Fix permission issues
sudo chmod +x /usr/local/bin/tusk
sudo chown $USER:$USER /usr/local/bin/tusk

# Or install to user directory
curl -sSL https://bash.tusklang.org/install.sh | bash -s -- --prefix=~/.local
```

#### Command Not Found
```bash
# Add to PATH
echo 'export PATH="/usr/local/bin:$PATH"' >> ~/.bashrc
source ~/.bashrc

# Or create symlink
sudo ln -sf /usr/local/bin/tusk /usr/bin/tusk
```

#### Network Issues
```bash
# Check connectivity
curl -I https://bash.tusklang.org

# Use alternative download
wget --no-check-certificate https://bash.tusklang.org/install.sh

# Or download from GitHub
curl -sSL https://raw.githubusercontent.com/cyber-boost/tusktsk/main/bash/install.sh | bash
```

### Debug Mode

```bash
# Enable debug mode
export TUSK_DEBUG=1
export TUSK_LOG_LEVEL=debug

# Run with verbose output
tusk parse config.tsk --verbose

# Check logs
tail -f /var/log/tusklang.log
```

### Uninstall

```bash
# Remove TuskLang
sudo rm -f /usr/local/bin/tusk
sudo rm -f /usr/local/bin/tusklang

# Remove configuration
rm -rf ~/.config/tusklang
sudo rm -rf /etc/tusklang

# Remove cache
rm -rf ~/.cache/tusklang
sudo rm -rf /var/cache/tusklang
```

## 🎯 Next Steps

Now that you have TuskLang installed, you're ready to:

1. **Learn Basic Syntax** → [002-quick-start-bash.md](002-quick-start-bash.md)
2. **Master @ Operators** → [003-basic-syntax-bash.md](003-basic-syntax-bash.md)
3. **Integrate with Databases** → [004-database-integration-bash.md](004-database-integration-bash.md)
4. **Build Advanced Scripts** → [005-advanced-features-bash.md](005-advanced-features-bash.md)

## 🔗 Resources

- **Official Website**: https://tusklang.org
- **Bash SDK Documentation**: https://bash.tusklang.org
- **GitHub Repository**: https://github.com/cyber-boost/tusktsk
- **Community Forum**: https://community.tusklang.org
- **Bug Reports**: https://github.com/cyber-boost/tusktsk/issues

---

**Ready to revolutionize your shell scripting? Let's Tusk! 🐚** 