# Installation Guide for TuskLang C# CLI

This guide covers installing and setting up the TuskLang C# CLI on different platforms.

## Prerequisites

### System Requirements

- **Operating System**: Windows 10+, macOS 10.15+, or Linux (Ubuntu 18.04+, CentOS 7+)
- **.NET Runtime**: .NET 6.0 or higher
- **Memory**: Minimum 512MB RAM (2GB recommended)
- **Disk Space**: 100MB for installation

### Required Software

1. **.NET 6.0 SDK or Runtime**
   ```bash
   # Check if .NET is installed
   dotnet --version
   
   # Install .NET 6.0 (if not installed)
   # Windows: Download from https://dotnet.microsoft.com/download
   # macOS: brew install dotnet
   # Ubuntu: sudo apt-get install dotnet-sdk-6.0
   ```

2. **Git** (for version control integration)
   ```bash
   # Check if Git is installed
   git --version
   
   # Install Git (if not installed)
   # Windows: Download from https://git-scm.com/download/win
   # macOS: brew install git
   # Ubuntu: sudo apt-get install git
   ```

## Installation Methods

### Method 1: Global Tool Installation (Recommended)

Install as a global .NET tool for system-wide access:

```bash
# Install globally
dotnet tool install -g TuskLang.CLI

# Verify installation
tsk version
```

**Update the tool:**
```bash
dotnet tool update -g TuskLang.CLI
```

**Uninstall the tool:**
```bash
dotnet tool uninstall -g TuskLang.CLI
```

### Method 2: NuGet Package Installation

Add to your project as a NuGet package:

```bash
# Add to existing project
dotnet add package TuskLang.CLI

# Or add to new project
dotnet new console -n MyTuskProject
cd MyTuskProject
dotnet add package TuskLang.CLI
```

### Method 3: Manual Installation

Download and install manually:

```bash
# Clone repository
git clone https://github.com/tusklang/tusklang-csharp.git
cd tusklang-csharp

# Build and install
dotnet build -c Release
dotnet pack -c Release
dotnet tool install -g --add-source ./bin/Release TuskLang.CLI
```

## Platform-Specific Instructions

### Windows Installation

1. **Using Chocolatey:**
   ```powershell
   choco install tusklang-cli
   ```

2. **Using Scoop:**
   ```powershell
   scoop install tusklang-cli
   ```

3. **Manual Installation:**
   - Download the latest release from GitHub
   - Extract to a directory (e.g., `C:\Program Files\TuskLang\`)
   - Add the directory to your PATH environment variable

### macOS Installation

1. **Using Homebrew:**
   ```bash
   brew install tusklang/tap/tusklang-cli
   ```

2. **Using MacPorts:**
   ```bash
   sudo port install tusklang-cli
   ```

### Linux Installation

1. **Ubuntu/Debian:**
   ```bash
   # Add repository
   wget -qO- https://packages.tusklang.org/gpg | sudo apt-key add -
   echo "deb https://packages.tusklang.org/ubuntu focal main" | sudo tee /etc/apt/sources.list.d/tusklang.list
   
   # Install
   sudo apt update
   sudo apt install tusklang-cli
   ```

2. **CentOS/RHEL/Fedora:**
   ```bash
   # Add repository
   sudo dnf config-manager --add-repo https://packages.tusklang.org/rpm/tusklang.repo
   
   # Install
   sudo dnf install tusklang-cli
   ```

3. **Arch Linux:**
   ```bash
   # Using AUR
   yay -S tusklang-cli
   ```

## Verification

After installation, verify everything is working:

```bash
# Check version
tsk version

# Check help
tsk help

# Test basic functionality
tsk config get --help
```

Expected output:
```
TuskLang C# CLI v2.0.0
.NET 6.0.0
Platform: Linux x64
```

## Configuration

### Initial Setup

1. **Create a project directory:**
   ```bash
   mkdir my-tusk-project
   cd my-tusk-project
   ```

2. **Initialize the project:**
   ```bash
   tsk init
   ```

3. **Verify project structure:**
   ```bash
   ls -la
   # Should show: peanu.peanuts, .tsk/, etc.
   ```

### Environment Variables

Set these environment variables for customization:

```bash
# Configuration directory
export TSK_CONFIG_DIR="/path/to/config"

# Log level
export TSK_LOG_LEVEL="Info"

# Output format
export TSK_OUTPUT_FORMAT="json"

# Cache directory
export TSK_CACHE_DIR="/path/to/cache"
```

**Windows (PowerShell):**
```powershell
$env:TSK_CONFIG_DIR = "C:\path\to\config"
$env:TSK_LOG_LEVEL = "Info"
$env:TSK_OUTPUT_FORMAT = "json"
$env:TSK_CACHE_DIR = "C:\path\to\cache"
```

**Windows (Command Prompt):**
```cmd
set TSK_CONFIG_DIR=C:\path\to\config
set TSK_LOG_LEVEL=Info
set TSK_OUTPUT_FORMAT=json
set TSK_CACHE_DIR=C:\path\to\cache
```

## IDE Integration

### Visual Studio Code

1. **Install the TuskLang extension:**
   - Open VS Code
   - Go to Extensions (Ctrl+Shift+X)
   - Search for "TuskLang"
   - Install the official extension

2. **Configure settings:**
   ```json
   {
     "tusklang.cliPath": "tsk",
     "tusklang.autoCompile": true,
     "tusklang.watchFiles": true
   }
   ```

### Visual Studio

1. **Install the TuskLang extension:**
   - Open Visual Studio
   - Go to Extensions > Manage Extensions
   - Search for "TuskLang"
   - Install the extension

2. **Configure project settings:**
   - Right-click on project
   - Properties > TuskLang
   - Configure CLI settings

### JetBrains Rider

1. **Install the TuskLang plugin:**
   - Open Rider
   - Go to Settings > Plugins
   - Search for "TuskLang"
   - Install the plugin

2. **Configure tool integration:**
   - Settings > Tools > External Tools
   - Add TuskLang CLI commands

## Troubleshooting

### Common Installation Issues

1. **"tsk command not found"**
   ```bash
   # Check if .NET tools are in PATH
   echo $PATH | grep -i dotnet
   
   # Add .NET tools to PATH
   export PATH="$PATH:$HOME/.dotnet/tools"
   ```

2. **Permission denied errors**
   ```bash
   # Fix permissions
   sudo chmod +x $(which tsk)
   
   # Or reinstall with proper permissions
   dotnet tool uninstall -g TuskLang.CLI
   dotnet tool install -g TuskLang.CLI
   ```

3. **.NET version conflicts**
   ```bash
   # Check installed .NET versions
   dotnet --list-runtimes
   dotnet --list-sdks
   
   # Use specific version
   dotnet --fx-version 6.0.0 tsk version
   ```

### Performance Issues

1. **Slow startup:**
   ```bash
   # Clear cache
   tsk cache clear
   
   # Disable verbose logging
   export TSK_LOG_LEVEL="Error"
   ```

2. **Memory issues:**
   ```bash
   # Increase memory limit
   export DOTNET_GCHeapHardLimit=0x10000000
   
   # Or run with specific memory settings
   dotnet --gc-allow-very-large-objects tsk version
   ```

### Network Issues

1. **Proxy configuration:**
   ```bash
   # Set proxy for NuGet
   dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
   
   # Configure proxy
   export HTTP_PROXY=http://proxy.company.com:8080
   export HTTPS_PROXY=http://proxy.company.com:8080
   ```

2. **Firewall issues:**
   - Ensure outbound connections to NuGet.org are allowed
   - Check corporate firewall settings
   - Use VPN if required

## Next Steps

After successful installation:

1. **Read the Quick Start guide** to get familiar with basic commands
2. **Explore the Command Reference** for detailed command documentation
3. **Check out Examples** for real-world usage patterns
4. **Join the community** for support and updates

## Support

If you encounter issues during installation:

1. **Check the troubleshooting section** above
2. **Review system requirements** and prerequisites
3. **Search existing issues** on GitHub
4. **Create a new issue** with detailed information:
   - Operating system and version
   - .NET version
   - Installation method used
   - Error messages and logs
   - Steps to reproduce

---

The TuskLang C# CLI is designed to be easy to install and use. If you follow this guide and still encounter issues, the community is here to help! 