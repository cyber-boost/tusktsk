# Installing TuskLang on macOS

This guide covers installing TuskLang on macOS systems (Intel and Apple Silicon).

## System Requirements

- macOS 10.15 (Catalina) or newer
- 64-bit processor (Intel x86_64 or Apple Silicon ARM64)
- 50MB free disk space
- Xcode Command Line Tools (for building from source)

## Quick Install (Recommended)

### Using Homebrew

The easiest way to install TuskLang on macOS:

```bash
# Install Homebrew if you haven't already
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# Add TuskLang tap
brew tap tusklang/tusk

# Install TuskLang
brew install tusklang
```

### Using the Official Installer

```bash
# Download and run the installer
curl -fsSL https://get.tusklang.org | bash

# The installer automatically detects your architecture (Intel/M1)
```

## Architecture-Specific Installation

### Apple Silicon (M1/M2/M3)

```bash
# Download ARM64 binary directly
curl -L https://github.com/tusklang/tusk/releases/latest/download/tusklang-darwin-arm64.tar.gz -o tusklang.tar.gz

# Extract and install
tar -xzf tusklang.tar.gz
sudo mv tusk /usr/local/bin/
sudo chmod +x /usr/local/bin/tusk
```

### Intel Macs

```bash
# Download x86_64 binary
curl -L https://github.com/tusklang/tusk/releases/latest/download/tusklang-darwin-amd64.tar.gz -o tusklang.tar.gz

# Extract and install
tar -xzf tusklang.tar.gz
sudo mv tusk /usr/local/bin/
sudo chmod +x /usr/local/bin/tusk
```

## Package Managers

### MacPorts

```bash
# Install via MacPorts
sudo port install tusklang
```

### Nix

```bash
# Install via Nix
nix-env -iA nixpkgs.tusklang
```

## Building from Source

### Prerequisites

Install Xcode Command Line Tools:
```bash
xcode-select --install
```

Install build dependencies:
```bash
# Using Homebrew
brew install go git

# Or download Go directly
curl -L https://go.dev/dl/go1.21.darwin-amd64.pkg -o go.pkg
sudo installer -pkg go.pkg -target /
```

### Build Steps

1. Clone the repository:
```bash
git clone https://github.com/tusklang/tusk.git
cd tusk
```

2. Build for your architecture:
```bash
# Auto-detect architecture
make build

# Or specify architecture
make build-darwin-arm64  # For M1/M2/M3
make build-darwin-amd64  # For Intel
```

3. Install system-wide:
```bash
sudo make install
```

## Shell Integration

### Zsh (Default on macOS)

Add to your `~/.zshrc`:
```bash
# TuskLang completion and aliases
eval "$(tusk --shell-init zsh)"

# Add to PATH if installed manually
export PATH="/usr/local/bin:$PATH"
```

Reload your shell:
```bash
source ~/.zshrc
```

### Bash

If you use bash, add to `~/.bash_profile`:
```bash
# TuskLang completion
eval "$(tusk --shell-init bash)"

# Add to PATH
export PATH="/usr/local/bin:$PATH"
```

### Fish

Add to `~/.config/fish/config.fish`:
```fish
# TuskLang initialization
tusk --shell-init fish | source
```

## macOS-Specific Configuration

### Gatekeeper and Security

If macOS blocks the binary:

```bash
# Remove quarantine attribute
xattr -d com.apple.quarantine /usr/local/bin/tusk

# Or allow in System Preferences
# System Preferences > Security & Privacy > General > "Allow apps downloaded from"
```

### Launch Agents

Create a launch agent for TuskLang services:

```xml
<!-- ~/Library/LaunchAgents/org.tusklang.agent.plist -->
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>org.tusklang.agent</string>
    <key>ProgramArguments</key>
    <array>
        <string>/usr/local/bin/tusk</string>
        <string>serve</string>
        <string>/Users/YOUR_USERNAME/tuskapp/server.tsk</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <true/>
    <key>StandardOutPath</key>
    <string>/tmp/tusklang.log</string>
    <key>StandardErrorPath</key>
    <string>/tmp/tusklang.error.log</string>
</dict>
</plist>
```

Load the agent:
```bash
launchctl load ~/Library/LaunchAgents/org.tusklang.agent.plist
```

## Development Tools

### VSCode

```bash
# Install VSCode if needed
brew install --cask visual-studio-code

# Install TuskLang extension
code --install-extension tusklang.vscode-tusk
```

### Sublime Text

```bash
# Install Package Control first, then:
# Cmd+Shift+P > Package Control: Install Package > TuskLang
```

### Vim/Neovim

```bash
# Using vim-plug
# Add to ~/.vimrc or ~/.config/nvim/init.vim
Plug 'tusklang/vim-tusk'

# Using Homebrew
brew install neovim
```

### JetBrains IDEs

Install the TuskLang plugin from the JetBrains Marketplace.

## Environment Setup

### Global Configuration

```bash
# Create config directory
mkdir -p ~/.config/tusklang

# Initialize global config
tusk init --global > ~/.config/tusklang/config.tsk
```

### Project Templates

```bash
# Create project templates directory
mkdir -p ~/Library/Application\ Support/TuskLang/templates

# Add custom template
cat > ~/Library/Application\ Support/TuskLang/templates/webapp.tsk << 'EOF'
# Web Application Template
app:
    name: @project.name
    version: "1.0.0"
    
server:
    port: @env.PORT || 3000
    host: "localhost"
EOF
```

## Troubleshooting

### Command Not Found

If `tusk` is not found after installation:

```bash
# Check installation location
which tusk

# Add to PATH in ~/.zshrc
echo 'export PATH="/usr/local/bin:$PATH"' >> ~/.zshrc
source ~/.zshrc

# For Homebrew on M1 Macs
echo 'export PATH="/opt/homebrew/bin:$PATH"' >> ~/.zshrc
source ~/.zshrc
```

### Permission Issues

```bash
# Fix permissions
sudo chmod +x /usr/local/bin/tusk

# Fix ownership
sudo chown $(whoami) /usr/local/bin/tusk
```

### Rosetta 2 (M1 Macs running Intel binaries)

```bash
# Install Rosetta 2 if needed
softwareupdate --install-rosetta

# Force Intel mode (not recommended)
arch -x86_64 tusk run app.tsk
```

### SSL Certificate Issues

```bash
# Update certificates
brew install ca-certificates

# Or update macOS
softwareupdate --all --install --force
```

## Performance Optimization

### File System Events

For better performance with file watching:

```bash
# Increase file descriptor limit
echo "ulimit -n 10240" >> ~/.zshrc

# For system-wide changes, create:
# /Library/LaunchDaemons/limit.maxfiles.plist
```

### Memory Management

```bash
# Set TuskLang memory limits
export TUSK_MAX_MEMORY=4G
export TUSK_GC_INTERVAL=1000
```

## Uninstallation

### Homebrew

```bash
brew uninstall tusklang
brew untap tusklang/tusk
```

### Manual Removal

```bash
# Remove binary
sudo rm /usr/local/bin/tusk

# Remove configuration
rm -rf ~/.config/tusklang
rm -rf ~/Library/Application\ Support/TuskLang

# Remove launch agents
launchctl unload ~/Library/LaunchAgents/org.tusklang.agent.plist
rm ~/Library/LaunchAgents/org.tusklang.agent.plist
```

## Integration with macOS Features

### Spotlight Integration

Create a Spotlight importer for .tsk files:

```bash
# Install importer
cp -r /usr/local/share/tusklang/TuskLangImporter.mdimporter ~/Library/Spotlight/
```

### Quick Look Plugin

```bash
# Install Quick Look plugin for .tsk files
cp -r /usr/local/share/tusklang/TuskLangQL.qlgenerator ~/Library/QuickLook/
qlmanage -r
```

## Next Steps

- Verify your installation: [007-verify-installation.md](007-verify-installation.md)
- Create your first TuskLang project: [008-hello-world.md](008-hello-world.md)
- Set up your development environment: [009-file-structure.md](009-file-structure.md)