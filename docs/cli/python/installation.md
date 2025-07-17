# Installation Guide

This guide covers installing the TuskLang Python CLI on different platforms and environments.

## Prerequisites

### System Requirements

- **Python**: 3.8 or higher
- **Operating System**: Linux, macOS, or Windows
- **Memory**: 512MB RAM minimum
- **Disk Space**: 100MB free space

### Python Environment

Ensure you have Python 3.8+ installed:

```bash
# Check Python version
python --version
# or
python3 --version
```

## Installation Methods

### Method 1: Automated Setup (Recommended)

Use the provided setup script for the easiest installation:

```bash
# Clone the repository
git clone https://github.com/tusklang/python-sdk.git
cd python-sdk

# Run the setup script
python setup_cli.py
```

The setup script will:
- Install required dependencies
- Create the global `tsk` command
- Set up AI configuration
- Generate bash completion
- Validate the installation

### Method 2: Manual Installation

#### Step 1: Install Dependencies

```bash
# Install required packages
pip install requests>=2.25.0
pip install psutil>=5.8.0
pip install pyyaml>=6.0
pip install colorama>=0.4.4
```

#### Step 2: Install the SDK

```bash
# From PyPI (when available)
pip install tusklang-python

# Or from source
git clone https://github.com/tusklang/python-sdk.git
cd python-sdk
pip install -e .
```

#### Step 3: Create CLI Script

Create a `tsk` script in your PATH:

```bash
#!/usr/bin/env python3
import sys
from pathlib import Path

# Add the SDK directory to Python path
sdk_path = Path(__file__).parent / "sdk-pnt-test" / "python"
sys.path.insert(0, str(sdk_path))

# Import and run the CLI
from cli.main import main

if __name__ == '__main__':
    sys.exit(main())
```

Make it executable:

```bash
chmod +x tsk
sudo mv tsk /usr/local/bin/
```

### Method 3: Using pipx (Recommended for Development)

```bash
# Install pipx if not already installed
python -m pip install --user pipx
python -m pipx ensurepath

# Install the CLI
pipx install git+https://github.com/tusklang/python-sdk.git
```

## Platform-Specific Instructions

### Linux

#### Ubuntu/Debian

```bash
# Install system dependencies
sudo apt update
sudo apt install python3 python3-pip python3-venv

# Install the CLI
python3 setup_cli.py
```

#### CentOS/RHEL/Fedora

```bash
# Install system dependencies
sudo dnf install python3 python3-pip
# or for older versions
sudo yum install python3 python3-pip

# Install the CLI
python3 setup_cli.py
```

#### Arch Linux

```bash
# Install from AUR (when available)
yay -S tusklang-python-cli

# Or install manually
python setup_cli.py
```

### macOS

#### Using Homebrew

```bash
# Install Python if not already installed
brew install python

# Install the CLI
python3 setup_cli.py
```

#### Using MacPorts

```bash
# Install Python
sudo port install python39

# Install the CLI
python3.9 setup_cli.py
```

### Windows

#### Using Chocolatey

```bash
# Install Python
choco install python

# Install the CLI
python setup_cli.py
```

#### Using Scoop

```bash
# Install Python
scoop install python

# Install the CLI
python setup_cli.py
```

#### Manual Windows Installation

1. Download Python from [python.org](https://python.org)
2. Install with "Add to PATH" option
3. Open Command Prompt or PowerShell
4. Run the setup script:

```cmd
python setup_cli.py
```

## Virtual Environment Installation

### Using venv

```bash
# Create virtual environment
python -m venv tusklang-env

# Activate virtual environment
# On Linux/macOS:
source tusklang-env/bin/activate
# On Windows:
tusklang-env\Scripts\activate

# Install the CLI
python setup_cli.py
```

### Using conda

```bash
# Create conda environment
conda create -n tusklang python=3.9

# Activate environment
conda activate tusklang

# Install the CLI
python setup_cli.py
```

## Docker Installation

### Using Docker

```bash
# Pull the official image (when available)
docker pull tusklang/python-cli:latest

# Run the CLI
docker run --rm -v $(pwd):/workspace tusklang/python-cli:latest tsk version
```

### Using Docker Compose

Create `docker-compose.yml`:

```yaml
version: '3.8'
services:
  tusklang-cli:
    image: tusklang/python-cli:latest
    volumes:
      - .:/workspace
    working_dir: /workspace
    command: tsk
```

Run:

```bash
docker-compose run --rm tusklang-cli version
```

## IDE Integration

### VS Code

1. Install the Python extension
2. Open the project folder
3. Select the Python interpreter
4. Use the integrated terminal to run CLI commands

### PyCharm

1. Open the project in PyCharm
2. Configure the Python interpreter
3. Use the terminal to run CLI commands
4. Set up run configurations for common commands

### Jupyter Notebook

```python
# Install in Jupyter environment
!pip install tusklang-python

# Use in notebooks
import subprocess
result = subprocess.run(['tsk', 'version'], capture_output=True, text=True)
print(result.stdout)
```

## Post-Installation Setup

### 1. Verify Installation

```bash
# Check version
tsk version

# Test basic functionality
tsk help
```

### 2. Configure AI Services (Optional)

```bash
# Set up AI services
tsk ai setup

# Test AI connections
tsk ai test
```

### 3. Set Up Bash Completion

Add to your `~/.bashrc`:

```bash
source ~/.tsk/tsk-completion.bash
```

Reload your shell:

```bash
source ~/.bashrc
```

### 4. Configure Environment Variables

```bash
# Add to your shell profile
export TSK_CONFIG_PATH="/path/to/config"
export TSK_VERBOSE="true"
export TSK_JSON_OUTPUT="false"
```

## Troubleshooting

### Common Issues

#### Permission Denied

```bash
# Fix permissions
chmod +x /usr/local/bin/tsk

# Or install for current user only
python setup_cli.py --user
```

#### Python Not Found

```bash
# Use python3 explicitly
python3 setup_cli.py

# Or create an alias
alias python=python3
```

#### Import Errors

```bash
# Check Python path
python -c "import sys; print(sys.path)"

# Install in development mode
pip install -e .
```

#### Missing Dependencies

```bash
# Install missing packages
pip install -r requirements.txt

# Or install individually
pip install requests psutil pyyaml colorama
```

### Getting Help

```bash
# Show help
tsk help

# Show version
tsk version

# Enable verbose output
tsk --verbose help
```

## Uninstallation

### Remove the CLI

```bash
# Remove the script
sudo rm /usr/local/bin/tsk

# Remove configuration
rm -rf ~/.tsk

# Remove from pipx
pipx uninstall tusklang-python
```

### Clean Up Dependencies

```bash
# Remove packages
pip uninstall tusklang-python requests psutil pyyaml colorama

# Remove virtual environment
rm -rf tusklang-env
```

## Next Steps

After installation, you can:

1. **Read the Quick Start Guide**: [Quick Start](./quickstart.md)
2. **Explore Commands**: [Command Reference](./commands/README.md)
3. **Try Examples**: [Examples](./examples/README.md)
4. **Get Help**: `tsk help`

## Support

If you encounter issues during installation:

- **Documentation**: [TuskLang Docs](https://tusklang.org/docs)
- **Issues**: [GitHub Issues](https://github.com/tusklang/python-sdk/issues)
- **Community**: [TuskLang Discord](https://discord.gg/tusklang) 