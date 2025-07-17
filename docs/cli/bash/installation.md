# Installation Guide

This guide covers installing the TuskLang Bash CLI on various platforms.

## Prerequisites

- **Bash 4.0+**: Required for associative arrays and advanced features
- **POSIX-compliant shell**: For cross-platform compatibility
- **curl/wget**: For downloading installation scripts
- **git**: For cloning the repository

### Check Your Bash Version

```bash
bash --version
```

If your version is below 4.0, upgrade Bash:

```bash
# Ubuntu/Debian
sudo apt update && sudo apt install bash

# CentOS/RHEL
sudo yum install bash

# macOS (using Homebrew)
brew install bash
```

## Installation Methods

### Method 1: Direct Download (Recommended)

```bash
# Download and install
curl -sSL https://tusklang.org/install-bash.sh | bash

# Or using wget
wget -qO- https://tusklang.org/install-bash.sh | bash
```

### Method 2: Git Clone

```bash
# Clone the repository
git clone https://github.com/tusklang/tusklang-bash.git
cd tusklang-bash

# Make CLI executable
chmod +x cli/main.sh

# Add to PATH (optional)
sudo ln -s "$(pwd)/cli/main.sh" /usr/local/bin/tsk
```

### Method 3: Package Manager

#### Ubuntu/Debian

```bash
# Add repository
curl -fsSL https://tusklang.org/gpg | sudo apt-key add -
echo "deb https://tusklang.org/apt stable main" | sudo tee /etc/apt/sources.list.d/tusklang.list

# Install
sudo apt update
sudo apt install tusklang-bash
```

#### CentOS/RHEL

```bash
# Add repository
sudo yum install https://tusklang.org/rpm/tusklang-bash.rpm

# Install
sudo yum install tusklang-bash
```

#### macOS

```bash
# Using Homebrew
brew tap tusklang/tap
brew install tusklang-bash

# Using MacPorts
sudo port install tusklang-bash
```

## Verification

After installation, verify the CLI is working:

```bash
# Check version
tsk version

# Show help
tsk help

# Test basic functionality
tsk config get --help
```

Expected output:
```
TuskLang Bash CLI v2.0.0
A powerful command-line interface for TuskLang development

Usage: tsk [global-options] <command> [command-options] [arguments]
```

## Configuration

### Environment Variables

Set these environment variables for customization:

```bash
# CLI configuration
export TSK_CONFIG_DIR="$HOME/.tsk"
export TSK_CACHE_DIR="$HOME/.cache/tsk"
export TSK_LOG_LEVEL="info"

# Development settings
export TSK_DEV_MODE="false"
export TSK_VERBOSE="false"
```

### Shell Integration

Add to your shell profile (`~/.bashrc`, `~/.zshrc`, etc.):

```bash
# TuskLang Bash CLI
export PATH="$HOME/.local/bin:$PATH"

# Auto-completion (if available)
if command -v tsk >/dev/null 2>&1; then
    source <(tsk completion bash)
fi
```

## Platform-Specific Notes

### Linux

The CLI works on all major Linux distributions:

- **Ubuntu/Debian**: Full support with package manager
- **CentOS/RHEL**: Full support with package manager
- **Arch Linux**: Available in AUR
- **Alpine Linux**: Compatible with POSIX shell

### macOS

- **Homebrew**: Recommended installation method
- **System Integrity Protection**: May require additional permissions
- **Terminal.app**: Full compatibility
- **iTerm2**: Enhanced features supported

### Windows

For Windows users, install via:

- **WSL (Windows Subsystem for Linux)**: Recommended
- **Git Bash**: Full compatibility
- **Cygwin**: Compatible with POSIX shell
- **PowerShell**: Limited compatibility

### WSL Installation

```bash
# Install WSL
wsl --install

# Install Ubuntu on WSL
wsl --install -d Ubuntu

# Follow Linux installation instructions
```

## Troubleshooting

### Common Issues

#### Permission Denied

```bash
# Fix executable permissions
chmod +x cli/main.sh

# Check file ownership
ls -la cli/main.sh
```

#### Command Not Found

```bash
# Check PATH
echo $PATH

# Add to PATH manually
export PATH="$HOME/.local/bin:$PATH"

# Or create symlink
sudo ln -s "$(pwd)/cli/main.sh" /usr/local/bin/tsk
```

#### Bash Version Too Old

```bash
# Check current version
bash --version

# Install newer version
# See prerequisites section above
```

#### Network Issues

```bash
# Check connectivity
curl -I https://tusklang.org

# Use alternative download
wget -qO- https://tusklang.org/install-bash.sh | bash
```

### Debug Mode

Enable debug mode for troubleshooting:

```bash
# Set debug environment
export TSK_DEBUG="true"
export TSK_VERBOSE="true"

# Run with debug output
tsk --verbose version
```

### Log Files

Check log files for errors:

```bash
# View logs
tail -f ~/.cache/tsk/tsk.log

# Clear logs
rm ~/.cache/tsk/tsk.log
```

## Uninstallation

### Remove CLI

```bash
# Remove symlink
sudo rm /usr/local/bin/tsk

# Remove package (if installed via package manager)
sudo apt remove tusklang-bash  # Ubuntu/Debian
sudo yum remove tusklang-bash  # CentOS/RHEL
brew uninstall tusklang-bash   # macOS

# Remove configuration
rm -rf ~/.tsk
rm -rf ~/.cache/tsk
```

### Clean Environment

Remove from shell profile:

```bash
# Edit shell profile
nano ~/.bashrc

# Remove TuskLang-related lines
# Save and reload
source ~/.bashrc
```

## Next Steps

After successful installation:

1. **Read the [Quick Start Guide](./quickstart.md)**
2. **Explore [Command Reference](./commands/README.md)**
3. **Try [Examples](./examples/README.md)**
4. **Check [Troubleshooting](./troubleshooting.md)** if you encounter issues

## Support

If you encounter installation issues:

- **GitHub Issues**: [Report a bug](https://github.com/tusklang/tusklang-bash/issues)
- **Documentation**: [TuskLang Docs](https://tusklang.org/docs)
- **Community**: [TuskLang Discord](https://discord.gg/tusklang) 