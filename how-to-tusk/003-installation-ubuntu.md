# Installing TuskLang on Ubuntu

This guide covers installing TuskLang on Ubuntu and Debian-based distributions.

## System Requirements

- Ubuntu 20.04 LTS or newer (18.04 with backports)
- 64-bit architecture (x86_64 or ARM64)
- 50MB free disk space
- Internet connection for package download

## Quick Install (Recommended)

### Using the Official Script

```bash
# Download and run the installer
curl -fsSL https://get.tusklang.org | bash

# Or with wget
wget -qO- https://get.tusklang.org | bash
```

### Verify Installation

```bash
tusk --version
# Output: TuskLang 1.0.0
```

## Package Manager Installation

### Using APT Repository

1. Add the TuskLang repository key:
```bash
curl -fsSL https://packages.tusklang.org/tusklang.gpg | sudo gpg --dearmor -o /usr/share/keyrings/tusklang-archive-keyring.gpg
```

2. Add the repository:
```bash
echo "deb [signed-by=/usr/share/keyrings/tusklang-archive-keyring.gpg] https://packages.tusklang.org/ubuntu $(lsb_release -cs) main" | sudo tee /etc/apt/sources.list.d/tusklang.list
```

3. Update and install:
```bash
sudo apt update
sudo apt install tusklang
```

### Using Snap

```bash
sudo snap install tusklang --classic
```

## Manual Installation

### Download Binary

1. Download the latest release:
```bash
wget https://github.com/tusklang/tusk/releases/latest/download/tusklang-linux-amd64.tar.gz
```

2. Extract the archive:
```bash
tar -xzf tusklang-linux-amd64.tar.gz
```

3. Move to system path:
```bash
sudo mv tusk /usr/local/bin/
sudo chmod +x /usr/local/bin/tusk
```

## Building from Source

### Prerequisites

```bash
# Install build dependencies
sudo apt update
sudo apt install -y build-essential git golang-go
```

### Build Steps

1. Clone the repository:
```bash
git clone https://github.com/tusklang/tusk.git
cd tusk
```

2. Build the binary:
```bash
make build
```

3. Install system-wide:
```bash
sudo make install
```

## Post-Installation Setup

### Shell Integration

Add TuskLang to your shell:

**Bash:**
```bash
echo 'eval "$(tusk --shell-init bash)"' >> ~/.bashrc
source ~/.bashrc
```

**Zsh:**
```bash
echo 'eval "$(tusk --shell-init zsh)"' >> ~/.zshrc
source ~/.zshrc
```

### System Service Integration

For server applications, create a systemd service:

```ini
# /etc/systemd/system/tuskapp.service
[Unit]
Description=TuskLang Application
After=network.target

[Service]
Type=simple
User=tuskapp
WorkingDirectory=/opt/tuskapp
ExecStart=/usr/local/bin/tusk run app.tsk
Restart=always

[Install]
WantedBy=multi-user.target
```

Enable the service:
```bash
sudo systemctl enable tuskapp
sudo systemctl start tuskapp
```

## Configuration

### Global Configuration

Create system-wide configuration:

```bash
sudo mkdir -p /etc/tusklang
sudo tusk init --global > /etc/tusklang/config.tsk
```

### User Configuration

Create user-specific configuration:

```bash
mkdir -p ~/.config/tusklang
tusk init > ~/.config/tusklang/config.tsk
```

## Development Tools

### VSCode Extension

```bash
code --install-extension tusklang.vscode-tusk
```

### Vim Plugin

```vim
" Add to ~/.vimrc
Plug 'tusklang/vim-tusk'
```

### Syntax Highlighting

Install syntax highlighting for various editors:

```bash
# Nano
sudo cp /usr/share/tusklang/nano/tusk.nanorc /usr/share/nano/
echo "include /usr/share/nano/tusk.nanorc" >> ~/.nanorc

# Gedit
sudo cp /usr/share/tusklang/gtksourceview/tusk.lang /usr/share/gtksourceview-4/language-specs/
```

## Troubleshooting

### Permission Denied

If you get permission errors:
```bash
# Fix permissions
sudo chmod +x /usr/local/bin/tusk

# Or reinstall with proper permissions
sudo apt reinstall tusklang
```

### Command Not Found

If `tusk` command is not found:
```bash
# Check if installed
which tusk

# Add to PATH if needed
echo 'export PATH="/usr/local/bin:$PATH"' >> ~/.bashrc
source ~/.bashrc
```

### Library Dependencies

If you encounter library errors:
```bash
# Install runtime dependencies
sudo apt install -y libssl1.1 libcurl4
```

### Ubuntu-Specific Issues

**Ubuntu 18.04:**
```bash
# Enable backports
sudo add-apt-repository "deb http://archive.ubuntu.com/ubuntu $(lsb_release -sc)-backports main universe"
sudo apt update
```

**ARM64 (Raspberry Pi):**
```bash
# Download ARM64 version
wget https://github.com/tusklang/tusk/releases/latest/download/tusklang-linux-arm64.tar.gz
```

## Uninstallation

### Package Manager

```bash
# APT
sudo apt remove tusklang
sudo apt autoremove

# Snap
sudo snap remove tusklang
```

### Manual Removal

```bash
sudo rm /usr/local/bin/tusk
rm -rf ~/.config/tusklang
```

## Next Steps

- Verify your installation: [007-verify-installation.md](007-verify-installation.md)
- Create your first TuskLang file: [008-hello-world.md](008-hello-world.md)
- Learn the CLI: [010-cli-overview.md](010-cli-overview.md)