# Troubleshooting Guide for TuskLang C# CLI

This guide helps you resolve common issues and problems when using the TuskLang C# CLI.

## Common Issues

### Installation Problems

#### "tsk command not found"

**Symptoms:**
```bash
tsk version
# bash: tsk: command not found
```

**Solutions:**

1. **Check .NET tools installation:**
   ```bash
   dotnet tool list -g
   ```

2. **Reinstall the CLI:**
   ```bash
   dotnet tool uninstall -g TuskLang.CLI
   dotnet tool install -g TuskLang.CLI
   ```

3. **Add .NET tools to PATH:**
   ```bash
   # Linux/macOS
   export PATH="$PATH:$HOME/.dotnet/tools"
   
   # Windows (PowerShell)
   $env:PATH += ";$env:USERPROFILE\.dotnet\tools"
   
   # Windows (Command Prompt)
   set PATH=%PATH%;%USERPROFILE%\.dotnet\tools
   ```

4. **Verify installation:**
   ```bash
   which tsk  # Linux/macOS
   where tsk  # Windows
   ```

#### Permission Denied Errors

**Symptoms:**
```bash
tsk version
# Permission denied
```

**Solutions:**

1. **Fix file permissions:**
   ```bash
   sudo chmod +x $(which tsk)
   ```

2. **Reinstall with proper permissions:**
   ```bash
   dotnet tool uninstall -g TuskLang.CLI
   dotnet tool install -g TuskLang.CLI
   ```

3. **Check .NET tools directory permissions:**
   ```bash
   ls -la ~/.dotnet/tools/
   chmod 755 ~/.dotnet/tools/
   ```

### Configuration Issues

#### "No configuration files found"

**Symptoms:**
```bash
tsk config get app.name
# Error: No configuration files found in current directory or parents
```

**Solutions:**

1. **Initialize project:**
   ```bash
   tsk init
   ```

2. **Create configuration file manually:**
   ```bash
   # Create peanu.peanuts file
   cat > peanu.peanuts << EOF
   [app]
   name: "My App"
   version: "1.0.0"
   
   [server]
   host: "localhost"
   port: 8080
   EOF
   ```

3. **Check file location:**
   ```bash
   # Look for configuration files
   find . -name "peanu.*" -type f
   
   # Check current directory
   ls -la peanu.*
   ```

#### "Configuration key not found"

**Symptoms:**
```bash
tsk config get invalid.key
# Error: Configuration key 'invalid.key' not found
```

**Solutions:**

1. **List available keys:**
   ```bash
   tsk config get --list-keys
   ```

2. **Check configuration structure:**
   ```bash
   tsk config get --json
   ```

3. **Validate configuration:**
   ```bash
   tsk config validate
   ```

#### "Invalid configuration syntax"

**Symptoms:**
```bash
tsk config validate
# Error: Invalid configuration syntax at line 5
```

**Solutions:**

1. **Check syntax:**
   ```bash
   # Show configuration with line numbers
   cat -n peanu.peanuts
   ```

2. **Common syntax fixes:**
   ```ini
   # Correct syntax
   [section]
   key: "value"
   number: 42
   boolean: true
   
   # Common mistakes to avoid:
   # key = value    # Use : not =
   # key:value      # Space after : is required
   # [section]      # Section names in brackets
   ```

3. **Use configuration validator:**
   ```bash
   tsk config validate --verbose
   ```

### Database Issues

#### "Database connection failed"

**Symptoms:**
```bash
tsk db status
# Error: Database connection failed after 3 attempts
```

**Solutions:**

1. **Check database configuration:**
   ```bash
   tsk config get database
   ```

2. **Verify database is running:**
   ```bash
   # PostgreSQL
   sudo systemctl status postgresql
   
   # MySQL
   sudo systemctl status mysql
   
   # SQLite (check file exists)
   ls -la app.db
   ```

3. **Test connection manually:**
   ```bash
   # PostgreSQL
   psql -h localhost -U username -d database
   
   # MySQL
   mysql -h localhost -u username -p database
   
   # SQLite
   sqlite3 app.db
   ```

4. **Check connection string:**
   ```bash
   tsk config get database.connection_string
   ```

#### "Migration failed"

**Symptoms:**
```bash
tsk db migrate
# Error: Migration failed: table already exists
```

**Solutions:**

1. **Check migration status:**
   ```bash
   tsk db status
   ```

2. **Reset migrations:**
   ```bash
   tsk db migrate --reset
   ```

3. **Check for conflicts:**
   ```bash
   tsk db migrate --dry-run
   ```

### Performance Issues

#### "Command is slow"

**Symptoms:**
```bash
tsk config get app.name
# Takes several seconds to respond
```

**Solutions:**

1. **Clear cache:**
   ```bash
   tsk cache clear
   ```

2. **Use binary configuration:**
   ```bash
   tsk config compile peanu.peanuts
   tsk config get app.name --config peanu.pnt
   ```

3. **Check system resources:**
   ```bash
   # Monitor CPU and memory
   top
   htop
   
   # Check disk space
   df -h
   ```

4. **Disable verbose logging:**
   ```bash
   export TSK_LOG_LEVEL="Error"
   ```

#### "Memory usage is high"

**Symptoms:**
```bash
# High memory usage in process monitor
```

**Solutions:**

1. **Increase memory limit:**
   ```bash
   export DOTNET_GCHeapHardLimit=0x10000000
   ```

2. **Use garbage collection settings:**
   ```bash
   dotnet --gc-allow-very-large-objects tsk version
   ```

3. **Clear caches regularly:**
   ```bash
   tsk cache clear
   ```

### Network Issues

#### "Cannot connect to remote services"

**Symptoms:**
```bash
tsk ai claude "Hello"
# Error: Network connection failed
```

**Solutions:**

1. **Check network connectivity:**
   ```bash
   ping api.anthropic.com
   curl -I https://api.anthropic.com
   ```

2. **Configure proxy:**
   ```bash
   export HTTP_PROXY=http://proxy.company.com:8080
   export HTTPS_PROXY=http://proxy.company.com:8080
   ```

3. **Check firewall settings:**
   ```bash
   # Linux
   sudo ufw status
   
   # Windows
   netsh advfirewall show allprofiles
   ```

4. **Use VPN if required:**
   ```bash
   # Connect to corporate VPN first
   # Then run CLI commands
   ```

### .NET Runtime Issues

#### ".NET version conflicts"

**Symptoms:**
```bash
tsk version
# Error: .NET runtime version mismatch
```

**Solutions:**

1. **Check installed .NET versions:**
   ```bash
   dotnet --list-runtimes
   dotnet --list-sdks
   ```

2. **Use specific .NET version:**
   ```bash
   dotnet --fx-version 6.0.0 tsk version
   ```

3. **Update .NET:**
   ```bash
   # Linux
   sudo apt update && sudo apt install dotnet-sdk-6.0
   
   # macOS
   brew upgrade dotnet
   
   # Windows
   # Download from https://dotnet.microsoft.com/download
   ```

#### "Assembly loading errors"

**Symptoms:**
```bash
tsk version
# Error: Could not load file or assembly
```

**Solutions:**

1. **Clear .NET cache:**
   ```bash
   dotnet nuget locals all --clear
   ```

2. **Reinstall CLI:**
   ```bash
   dotnet tool uninstall -g TuskLang.CLI
   dotnet tool install -g TuskLang.CLI
   ```

3. **Check for conflicting packages:**
   ```bash
   dotnet list package
   ```

## Debug Mode

### Enable Verbose Logging

```bash
# Enable verbose logging for all commands
export TSK_LOG_LEVEL="Debug"

# Or use --verbose flag
tsk --verbose config get app.name
```

### Debug Specific Commands

```bash
# Debug configuration loading
tsk --verbose config validate

# Debug database operations
tsk --verbose db status

# Debug test execution
tsk --verbose test all
```

### Get Detailed Error Information

```bash
# Show stack traces
tsk --verbose config get invalid.key

# Show configuration details
tsk config validate --verbose

# Show database connection details
tsk db status --verbose
```

## Performance Optimization

### Configuration Optimization

```bash
# Compile to binary for faster loading
tsk config compile peanu.peanuts

# Use binary config in production
tsk serve --config peanu.pnt

# Enable caching
export TSK_CACHE_ENABLED=true
```

### Cache Management

```bash
# Clear all caches
tsk cache clear

# Check cache status
tsk cache status

# Warm up caches
tsk cache warm
```

### System Optimization

```bash
# Increase file descriptor limits (Linux)
ulimit -n 65536

# Optimize .NET performance
export DOTNET_GCHeapHardLimit=0x10000000
export DOTNET_GCAllowVeryLargeObjects=1
```

## Platform-Specific Issues

### Windows Issues

#### "Path too long"

**Symptoms:**
```bash
# Error: Path too long
```

**Solutions:**

1. **Enable long path support:**
   ```powershell
   # Run as Administrator
   New-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem" -Name "LongPathsEnabled" -Value 1 -PropertyType DWORD -Force
   ```

2. **Use shorter paths:**
   ```bash
   # Move project to shorter path
   C:\tsk\project
   ```

#### "PowerShell execution policy"

**Symptoms:**
```bash
# Execution policy error
```

**Solutions:**

1. **Change execution policy:**
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
   ```

2. **Use Command Prompt instead:**
   ```cmd
   tsk version
   ```

### macOS Issues

#### "Gatekeeper blocking execution"

**Symptoms:**
```bash
# Gatekeeper security error
```

**Solutions:**

1. **Allow execution:**
   ```bash
   sudo spctl --master-disable
   ```

2. **Remove quarantine attribute:**
   ```bash
   xattr -d com.apple.quarantine $(which tsk)
   ```

#### "Homebrew conflicts"

**Symptoms:**
```bash
# Multiple .NET installations
```

**Solutions:**

1. **Use Homebrew .NET:**
   ```bash
   brew install dotnet
   ```

2. **Or use official .NET:**
   ```bash
   # Remove Homebrew version
   brew uninstall dotnet
   # Install official version
   ```

### Linux Issues

#### "Missing dependencies"

**Symptoms:**
```bash
# Library not found errors
```

**Solutions:**

1. **Install required packages:**
   ```bash
   # Ubuntu/Debian
   sudo apt-get install libicu-dev libssl-dev
   
   # CentOS/RHEL
   sudo yum install libicu openssl-devel
   ```

2. **Update system:**
   ```bash
   sudo apt update && sudo apt upgrade
   ```

#### "Permission issues"

**Symptoms:**
```bash
# Permission denied errors
```

**Solutions:**

1. **Fix ownership:**
   ```bash
   sudo chown -R $USER:$USER ~/.dotnet
   ```

2. **Fix permissions:**
   ```bash
   chmod 755 ~/.dotnet/tools/
   ```

## Getting Help

### Self-Diagnosis

```bash
# Check CLI version and environment
tsk version --verbose

# Check system information
tsk system info

# Run diagnostics
tsk diagnose
```

### Collecting Information

When reporting issues, include:

1. **System information:**
   ```bash
   tsk version --verbose
   uname -a
   dotnet --version
   ```

2. **Error messages:**
   ```bash
   tsk --verbose <command> 2>&1 | tee error.log
   ```

3. **Configuration files:**
   ```bash
   cat peanu.peanuts
   tsk config get --json
   ```

4. **Steps to reproduce:**
   - Exact commands run
   - Expected vs actual behavior
   - Environment details

### Community Support

1. **Search existing issues:**
   - GitHub Issues
   - Documentation
   - Community forums

2. **Create new issue:**
   - Use issue template
   - Include all collected information
   - Provide minimal reproduction case

3. **Join community:**
   - Discord server
   - GitHub Discussions
   - Stack Overflow

## Prevention

### Best Practices

1. **Keep CLI updated:**
   ```bash
   dotnet tool update -g TuskLang.CLI
   ```

2. **Use version control:**
   ```bash
   git add peanu.peanuts
   git commit -m "Update configuration"
   ```

3. **Backup configurations:**
   ```bash
   cp peanu.peanuts peanu.peanuts.backup
   ```

4. **Test in isolated environment:**
   ```bash
   # Use Docker or virtual environment
   docker run --rm -v $(pwd):/app tusk/cli tsk test all
   ```

### Monitoring

```bash
# Monitor CLI performance
time tsk config get app.name

# Check resource usage
ps aux | grep tsk

# Monitor logs
tail -f ~/.tsk/logs/cli.log
```

---

This troubleshooting guide covers the most common issues you might encounter. If you don't find a solution here, the community is ready to help! 