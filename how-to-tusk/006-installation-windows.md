# Installing TuskLang on Windows

This guide covers installing TuskLang on Windows systems (Windows 10 and later).

## System Requirements

- Windows 10 version 1903 or newer (64-bit)
- Windows 11 (all versions)
- 50MB free disk space
- Administrator privileges for system-wide installation

## Quick Install Methods

### Using Winget (Windows Package Manager)

The recommended method for Windows 11 and updated Windows 10:

```powershell
# Install via winget
winget install TuskLang.Tusk

# Verify installation
tusk --version
```

### Using Chocolatey

If you have Chocolatey installed:

```powershell
# Run as Administrator
choco install tusklang

# Or with specific version
choco install tusklang --version=1.0.0
```

### Using Scoop

For user-level installation without admin rights:

```powershell
# Add the TuskLang bucket
scoop bucket add tusklang https://github.com/tusklang/scoop-bucket

# Install TuskLang
scoop install tusklang
```

## Manual Installation

### Download and Install

1. Download the installer:
   - 64-bit: [tusklang-windows-amd64.msi](https://github.com/tusklang/tusk/releases/latest/download/tusklang-windows-amd64.msi)
   - ARM64: [tusklang-windows-arm64.msi](https://github.com/tusklang/tusk/releases/latest/download/tusklang-windows-arm64.msi)

2. Run the MSI installer:
   - Double-click the downloaded file
   - Follow the installation wizard
   - Choose installation directory (default: `C:\Program Files\TuskLang`)

### Portable Installation

For a portable installation without installer:

```powershell
# Create directory
New-Item -ItemType Directory -Force -Path C:\Tools\TuskLang

# Download portable version
Invoke-WebRequest -Uri "https://github.com/tusklang/tusk/releases/latest/download/tusklang-windows-amd64.zip" -OutFile "tusklang.zip"

# Extract
Expand-Archive -Path "tusklang.zip" -DestinationPath "C:\Tools\TuskLang"

# Add to PATH manually (see below)
```

## Setting up PATH

### Automatic (Installer)

The MSI installer automatically adds TuskLang to your PATH.

### Manual PATH Configuration

1. **Via PowerShell (Administrator):**
```powershell
# Add to system PATH
[Environment]::SetEnvironmentVariable(
    "Path",
    [Environment]::GetEnvironmentVariable("Path", [EnvironmentVariableTarget]::Machine) + ";C:\Tools\TuskLang",
    [EnvironmentVariableTarget]::Machine
)
```

2. **Via GUI:**
   - Right-click "This PC" → Properties
   - Click "Advanced system settings"
   - Click "Environment Variables"
   - Under "System variables", select "Path" and click "Edit"
   - Click "New" and add `C:\Tools\TuskLang`
   - Click "OK" to save

3. **Verify PATH:**
```powershell
# Restart PowerShell/Command Prompt first
tusk --version
```

## Windows Terminal Integration

### PowerShell Profile

Add to your PowerShell profile (`$PROFILE`):

```powershell
# Create profile if it doesn't exist
if (!(Test-Path -Path $PROFILE)) {
    New-Item -ItemType File -Path $PROFILE -Force
}

# Add TuskLang initialization
Add-Content $PROFILE @"
# TuskLang shell integration
if (Get-Command tusk -ErrorAction SilentlyContinue) {
    Invoke-Expression (tusk --shell-init powershell)
}
"@
```

### Command Prompt

Create a batch file for Command Prompt integration:

```batch
REM Save as C:\Users\%USERNAME%\tusklang-init.cmd
@echo off
tusk --shell-init cmd > %TEMP%\tusk-init.cmd
call %TEMP%\tusk-init.cmd
del %TEMP%\tusk-init.cmd
```

### Windows Terminal Settings

Add a custom profile for TuskLang REPL:

```json
{
    "profiles": {
        "list": [
            {
                "name": "TuskLang REPL",
                "commandline": "tusk repl",
                "icon": "C:\\Program Files\\TuskLang\\tusk-icon.ico",
                "startingDirectory": "%USERPROFILE%\\Documents\\TuskProjects"
            }
        ]
    }
}
```

## Building from Source

### Prerequisites

1. Install Git:
```powershell
winget install Git.Git
```

2. Install Go:
```powershell
winget install GoLang.Go
```

3. Install Build Tools:
```powershell
# Visual Studio Build Tools
winget install Microsoft.VisualStudio.2022.BuildTools
```

### Build Steps

```powershell
# Clone repository
git clone https://github.com/tusklang/tusk.git
cd tusk

# Build
go build -o tusk.exe cmd/tusk/main.go

# Install
Copy-Item tusk.exe "C:\Program Files\TuskLang\" -Force
```

## Windows-Specific Features

### File Associations

Associate .tsk files with TuskLang:

```powershell
# Run as Administrator
cmd /c assoc .tsk=TuskLangFile
cmd /c ftype TuskLangFile="C:\Program Files\TuskLang\tusk.exe" "%1" %*
```

### Context Menu Integration

Add "Run with TuskLang" to context menu:

```powershell
# Create registry entry
New-Item -Path "HKCR:\TuskLangFile\shell\Run with TuskLang\command" -Force
Set-ItemProperty -Path "HKCR:\TuskLangFile\shell\Run with TuskLang\command" -Name "(Default)" -Value '"C:\Program Files\TuskLang\tusk.exe" run "%1"'
```

### Windows Service

Create a Windows service for TuskLang applications:

```powershell
# Using NSSM (Non-Sucking Service Manager)
choco install nssm

# Install service
nssm install TuskLangApp "C:\Program Files\TuskLang\tusk.exe" "serve C:\MyApp\server.tsk"

# Configure service
nssm set TuskLangApp AppDirectory C:\MyApp
nssm set TuskLangApp DisplayName "TuskLang Application"
nssm set TuskLangApp Description "My TuskLang Web Application"

# Start service
nssm start TuskLangApp
```

## Development Environment

### Visual Studio Code

```powershell
# Install VS Code
winget install Microsoft.VisualStudioCode

# Install TuskLang extension
code --install-extension tusklang.vscode-tusk
```

### Visual Studio

Install the TuskLang extension from Visual Studio Marketplace.

### Notepad++

1. Download TuskLang syntax highlighter
2. Copy to `%APPDATA%\Notepad++\userDefineLangs\`
3. Restart Notepad++

## WSL Integration

### Using TuskLang in WSL2

```bash
# Inside WSL2
curl -fsSL https://get.tusklang.org | bash

# Or use Windows binary from WSL
alias tusk='/mnt/c/Program\ Files/TuskLang/tusk.exe'
```

### Sharing Files

```powershell
# Access WSL files from Windows TuskLang
tusk run "\\wsl$\Ubuntu\home\user\project.tsk"

# Access Windows files from WSL TuskLang
tusk run /mnt/c/Users/YourName/project.tsk
```

## Troubleshooting

### Antivirus Issues

Some antivirus software may flag TuskLang. To resolve:

1. Add exclusion for `C:\Program Files\TuskLang\`
2. Or download from official sources only
3. Submit false positive report to your AV vendor

### Permission Errors

```powershell
# Run as Administrator
Start-Process powershell -Verb RunAs

# Then retry installation
```

### Path Not Updated

```powershell
# Force PATH refresh
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")

# Or restart your terminal
```

### Windows Defender SmartScreen

If SmartScreen blocks the installer:

1. Right-click the file
2. Select Properties
3. Check "Unblock"
4. Click Apply and OK

## Performance Optimization

### Windows Defender Exclusions

Add TuskLang to Defender exclusions for better performance:

```powershell
# Run as Administrator
Add-MpPreference -ExclusionPath "C:\Program Files\TuskLang"
Add-MpPreference -ExclusionProcess "tusk.exe"
```

### File System Performance

```powershell
# Enable long path support
New-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem" -Name "LongPathsEnabled" -Value 1 -PropertyType DWORD -Force
```

## Uninstallation

### Package Managers

```powershell
# Winget
winget uninstall TuskLang.Tusk

# Chocolatey
choco uninstall tusklang

# Scoop
scoop uninstall tusklang
```

### Manual Uninstallation

1. Remove from Control Panel → Programs
2. Or manually delete:

```powershell
# Remove program files
Remove-Item -Recurse -Force "C:\Program Files\TuskLang"

# Remove user data
Remove-Item -Recurse -Force "$env:APPDATA\TuskLang"

# Remove from PATH (manually edit Environment Variables)
```

## Integration with Windows Features

### Task Scheduler

Create scheduled tasks for TuskLang scripts:

```powershell
# Create scheduled task
$action = New-ScheduledTaskAction -Execute "C:\Program Files\TuskLang\tusk.exe" -Argument "run C:\Scripts\daily-task.tsk"
$trigger = New-ScheduledTaskTrigger -Daily -At 9am
Register-ScheduledTask -TaskName "TuskLangDaily" -Action $action -Trigger $trigger
```

### PowerShell Module

```powershell
# Install TuskLang PowerShell module
Install-Module -Name TuskLang -Scope CurrentUser

# Use in scripts
Import-Module TuskLang
Invoke-TuskScript -Path ".\config.tsk"
```

## Next Steps

- Verify installation: [007-verify-installation.md](007-verify-installation.md)
- Create first project: [008-hello-world.md](008-hello-world.md)
- Learn Windows-specific patterns: [009-file-structure.md](009-file-structure.md)