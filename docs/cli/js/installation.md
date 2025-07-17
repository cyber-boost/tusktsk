# Installation Guide for TuskLang JavaScript CLI

This guide covers installing and setting up the TuskLang JavaScript CLI on different platforms.

## Prerequisites

### System Requirements

- **Node.js**: Version 16.0 or higher
- **npm**: Version 7.0 or higher (comes with Node.js)
- **Operating System**: Linux, macOS, or Windows

### Node.js Installation

#### Linux (Ubuntu/Debian)
```bash
# Using NodeSource repository
curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
sudo apt-get install -y nodejs

# Verify installation
node --version
npm --version
```

#### macOS
```bash
# Using Homebrew
brew install node

# Or download from nodejs.org
# https://nodejs.org/en/download/

# Verify installation
node --version
npm --version
```

#### Windows
```bash
# Download installer from nodejs.org
# https://nodejs.org/en/download/

# Or using Chocolatey
choco install nodejs

# Verify installation
node --version
npm --version
```

## Installing TuskLang JavaScript SDK

### Global Installation (Recommended)

```bash
# Install globally for CLI access
npm install -g tusklang

# Verify installation
tsk --version
```

### Local Installation

```bash
# Install in project
npm install tusklang

# Use with npx
npx tsk --version
```

### Development Installation

```bash
# Clone repository
git clone https://github.com/tusklang/tusklang.git
cd tusklang/js

# Install dependencies
npm install

# Link for development
npm link

# Verify installation
tsk --version
```

## Configuration

### Environment Variables

```bash
# Set Node.js environment
export NODE_ENV=development

# Set TuskLang configuration directory
export TUSKLANG_CONFIG_DIR=./config

# Set debug mode
export TUSKLANG_DEBUG=true
```

### Package.json Scripts

```json
{
  "scripts": {
    "tsk": "tsk",
    "dev": "tsk serve 3000",
    "test": "tsk test all",
    "build": "tsk config compile",
    "start": "tsk services start"
  }
}
```

## IDE Integration

### Visual Studio Code

Install the TuskLang extension for syntax highlighting and IntelliSense:

```bash
# Install extension
code --install-extension tusklang.tusk

# Or search for "TuskLang" in VS Code extensions
```

### WebStorm/IntelliJ

1. Go to Settings → Plugins
2. Search for "TuskLang"
3. Install the TuskLang plugin
4. Restart IDE

### Vim/Neovim

```vim
" Add to .vimrc
autocmd BufRead,BufNewFile *.tsk,*.peanuts set filetype=tusk
```

## Verification

### Test Installation

```bash
# Check version
tsk --version

# Check help
tsk --help

# Test basic functionality
tsk parse --help
```

### Create Test Configuration

```bash
# Create test file
cat > test.tsk << EOF
app {
    name: "Test App"
    version: "1.0.0"
}
EOF

# Parse test file
tsk parse test.tsk

# Clean up
rm test.tsk
```

## Troubleshooting

### Common Issues

#### Permission Denied
```bash
# Fix npm permissions
sudo chown -R $USER:$GROUP ~/.npm
sudo chown -R $USER:$GROUP ~/.config

# Or use nvm for Node.js management
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.0/install.sh | bash
nvm install 18
nvm use 18
```

#### Command Not Found
```bash
# Check PATH
echo $PATH

# Add npm global bin to PATH
export PATH="$PATH:$(npm config get prefix)/bin"

# Or reinstall globally
npm uninstall -g tusklang
npm install -g tusklang
```

#### Version Conflicts
```bash
# Clear npm cache
npm cache clean --force

# Remove global installation
npm uninstall -g tusklang

# Reinstall
npm install -g tusklang
```

### Platform-Specific Issues

#### Windows
```bash
# Run PowerShell as Administrator
# Install Windows Build Tools
npm install --global --production windows-build-tools

# Or use WSL for Linux environment
wsl --install
```

#### macOS
```bash
# Fix Homebrew permissions
sudo chown -R $(whoami) /usr/local/bin /usr/local/lib /usr/local/sbin
chmod u+w /usr/local/bin /usr/local/lib /usr/local/sbin

# Or use nvm
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.0/install.sh | bash
```

#### Linux
```bash
# Install build essentials
sudo apt-get install build-essential

# Fix npm permissions
mkdir ~/.npm-global
npm config set prefix '~/.npm-global'
export PATH=~/.npm-global/bin:$PATH
```

## Next Steps

After installation, you can:

1. [Read the Quick Start Guide](./quickstart.md)
2. [Explore Command Reference](./commands/README.md)
3. [Check out Examples](./examples/README.md)
4. [Learn about PNT Configuration](../js/docs/PNT_GUIDE.md)

## Support

If you encounter issues:

1. Check the [Troubleshooting Guide](./troubleshooting.md)
2. Search [GitHub Issues](https://github.com/tusklang/tusklang/issues)
3. Ask in the [Discord Community](https://discord.gg/tusklang)
4. Review the [Documentation](https://tusklang.org/docs) 