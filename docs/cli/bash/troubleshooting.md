# Troubleshooting Guide

Common issues and solutions for the TuskLang Bash CLI.

## Quick Diagnosis

### Check CLI Status

```bash
# Verify CLI installation
tsk version

# Check if CLI is executable
ls -la $(which tsk)

# Test basic functionality
tsk help
```

### Check Environment

```bash
# Verify Bash version
bash --version

# Check PATH
echo $PATH

# Check environment variables
env | grep TSK
```

## Common Issues

### Installation Problems

#### Command Not Found

**Symptoms:**
```bash
tsk: command not found
```

**Solutions:**

1. **Check installation path:**
   ```bash
   # Find tsk executable
   find /usr -name "tsk" 2>/dev/null
   find /opt -name "tsk" 2>/dev/null
   ```

2. **Add to PATH:**
   ```bash
   # Add to PATH temporarily
   export PATH="$HOME/.local/bin:$PATH"
   
   # Add to PATH permanently
   echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.bashrc
   source ~/.bashrc
   ```

3. **Create symlink:**
   ```bash
   # Create symlink
   sudo ln -s /path/to/tsk /usr/local/bin/tsk
   ```

#### Permission Denied

**Symptoms:**
```bash
bash: ./cli/main.sh: Permission denied
```

**Solutions:**

1. **Fix permissions:**
   ```bash
   chmod +x cli/main.sh
   ```

2. **Check file ownership:**
   ```bash
   ls -la cli/main.sh
   sudo chown $USER:$USER cli/main.sh
   ```

3. **Check filesystem permissions:**
   ```bash
   mount | grep "noexec"
   ```

### Configuration Issues

#### Configuration File Not Found

**Symptoms:**
```bash
❌ Configuration file not found: peanu.peanuts
```

**Solutions:**

1. **Check current directory:**
   ```bash
   pwd
   ls -la *.peanuts *.tsk *.pnt
   ```

2. **Use absolute path:**
   ```bash
   tsk config get app.name /path/to/project
   ```

3. **Create configuration:**
   ```bash
   # Create basic configuration
   cat > peanu.peanuts << 'EOF'
   [app]
   name: "My App"
   version: "1.0.0"
   EOF
   ```

#### Invalid Configuration Syntax

**Symptoms:**
```bash
❌ Configuration validation failed
```

**Solutions:**

1. **Validate configuration:**
   ```bash
   tsk config validate --verbose
   ```

2. **Check syntax:**
   ```bash
   # Check for common syntax errors
   grep -n "=" peanu.peanuts
   grep -n "\[" peanu.peanuts
   ```

3. **Use linting:**
   ```bash
   # Check for syntax errors
   tsk config validate peanu.peanuts
   ```

### Database Issues

#### Database Connection Failed

**Symptoms:**
```bash
❌ Database connection failed after 3 attempts
```

**Solutions:**

1. **Check database service:**
   ```bash
   # PostgreSQL
   sudo systemctl status postgresql
   
   # MySQL
   sudo systemctl status mysql
   
   # SQLite
   ls -la *.db
   ```

2. **Check connection parameters:**
   ```bash
   # Verify configuration
   tsk config get database.host
   tsk config get database.port
   tsk config get database.name
   ```

3. **Test connection manually:**
   ```bash
   # PostgreSQL
   psql -h localhost -p 5432 -U postgres -d myapp
   
   # MySQL
   mysql -h localhost -P 3306 -u root -p myapp
   
   # SQLite
   sqlite3 data/app.db ".tables"
   ```

#### Migration Errors

**Symptoms:**
```bash
❌ Migration failed: syntax error
```

**Solutions:**

1. **Check migration syntax:**
   ```bash
   # Validate SQL syntax
   tsk db migrate migration.sql --dry-run
   ```

2. **Check database state:**
   ```bash
   # Check current schema
   tsk db console -c "\d"
   ```

3. **Rollback if needed:**
   ```bash
   # Restore from backup
   tsk db restore backup.sql
   ```

### Performance Issues

#### Slow Configuration Loading

**Symptoms:**
```bash
# Configuration takes several seconds to load
```

**Solutions:**

1. **Use binary format:**
   ```bash
   # Compile to binary
   tsk peanuts compile peanu.peanuts
   
   # Use binary in scripts
   tsk peanuts load peanu.pnt
   ```

2. **Enable caching:**
   ```bash
   # Enable configuration caching
   export PEANUT_CACHE_ENABLED=1
   ```

3. **Optimize file structure:**
   ```bash
   # Reduce hierarchy depth
   tsk config check
   ```

#### High Memory Usage

**Symptoms:**
```bash
# CLI uses excessive memory
```

**Solutions:**

1. **Check for memory leaks:**
   ```bash
   # Monitor memory usage
   ps aux | grep tsk
   ```

2. **Limit cache size:**
   ```bash
   # Set cache limits
   export PEANUT_CACHE_SIZE=100
   ```

3. **Use streaming for large files:**
   ```bash
   # Process large files in chunks
   tsk config get large.key --stream
   ```

### Network Issues

#### Download Failures

**Symptoms:**
```bash
curl: (7) Failed to connect to tusklang.org
```

**Solutions:**

1. **Check network connectivity:**
   ```bash
   # Test connectivity
   ping tusklang.org
   curl -I https://tusklang.org
   ```

2. **Use alternative download:**
   ```bash
   # Try wget instead of curl
   wget -qO- https://tusklang.org/install-bash.sh | bash
   ```

3. **Check proxy settings:**
   ```bash
   # Set proxy if needed
   export http_proxy="http://proxy:port"
   export https_proxy="http://proxy:port"
   ```

#### API Connection Issues

**Symptoms:**
```bash
❌ Failed to connect to AI API
```

**Solutions:**

1. **Check API configuration:**
   ```bash
   # Verify API settings
   tsk ai config
   ```

2. **Test API connectivity:**
   ```bash
   # Test API connection
   tsk ai test
   ```

3. **Check API keys:**
   ```bash
   # Verify API keys
   echo $OPENAI_API_KEY
   echo $ANTHROPIC_API_KEY
   ```

## Debug Mode

### Enable Debug Output

```bash
# Set debug environment variables
export TSK_DEBUG="true"
export TSK_VERBOSE="true"
export PEANUT_DEBUG="true"

# Run commands with debug output
tsk config get app.name --verbose
```

### Debug Functions

```bash
# Debug configuration loading
peanut_debug() {
    if [[ "${PEANUT_DEBUG:-0}" -eq 1 ]]; then
        echo "[DEBUG] $*" >&2
    fi
}

# Debug CLI operations
tsk_debug() {
    if [[ "${TSK_DEBUG:-0}" -eq 1 ]]; then
        echo "[TSK DEBUG] $*" >&2
    fi
}
```

### Log Files

```bash
# View log files
tail -f ~/.cache/tsk/tsk.log
tail -f ~/.cache/tsk/peanut.log

# Clear logs
rm ~/.cache/tsk/*.log
```

## Platform-Specific Issues

### Linux Issues

#### SELinux Restrictions

**Symptoms:**
```bash
Permission denied due to SELinux
```

**Solutions:**
```bash
# Check SELinux status
sestatus

# Allow execution
chcon -t bin_t cli/main.sh

# Or disable SELinux temporarily
sudo setenforce 0
```

#### AppArmor Restrictions

**Symptoms:**
```bash
Permission denied due to AppArmor
```

**Solutions:**
```bash
# Check AppArmor status
sudo aa-status

# Create AppArmor profile
sudo aa-genprof tsk
```

### macOS Issues

#### System Integrity Protection

**Symptoms:**
```bash
Operation not permitted
```

**Solutions:**
```bash
# Check SIP status
csrutil status

# Temporarily disable SIP (requires reboot)
csrutil disable
```

#### Homebrew Path Issues

**Symptoms:**
```bash
tsk: command not found (after Homebrew install)
```

**Solutions:**
```bash
# Update PATH
echo 'export PATH="/usr/local/bin:$PATH"' >> ~/.zshrc
source ~/.zshrc

# Reinstall
brew uninstall tusklang-bash
brew install tusklang-bash
```

### Windows Issues

#### WSL Path Problems

**Symptoms:**
```bash
Path not found in WSL
```

**Solutions:**
```bash
# Check WSL path
echo $PATH

# Add to WSL PATH
echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.bashrc
source ~/.bashrc
```

#### Git Bash Compatibility

**Symptoms:**
```bash
Syntax error in Git Bash
```

**Solutions:**
```bash
# Use POSIX-compliant syntax
# Avoid Bash-specific features

# Check Git Bash version
bash --version
```

## Recovery Procedures

### Reset Configuration

```bash
# Backup current configuration
cp -r ~/.tsk ~/.tsk.backup

# Reset configuration
rm -rf ~/.tsk
mkdir ~/.tsk

# Restore if needed
cp -r ~/.tsk.backup/* ~/.tsk/
```

### Reinstall CLI

```bash
# Remove current installation
sudo rm /usr/local/bin/tsk
rm -rf ~/.cache/tsk

# Reinstall
curl -sSL https://tusklang.org/install-bash.sh | bash
```

### Database Recovery

```bash
# Restore from backup
tsk db restore backup.sql

# Or reinitialize
tsk db init
tsk db migrate schema.sql
```

## Getting Help

### Self-Diagnosis

```bash
# Run diagnostic script
tsk diagnose

# Check system information
tsk system info

# Generate debug report
tsk debug report > debug-report.txt
```

### Community Support

- **GitHub Issues**: [Report a bug](https://github.com/tusklang/tusklang-bash/issues)
- **Documentation**: [TuskLang Docs](https://tusklang.org/docs)
- **Discord**: [TuskLang Community](https://discord.gg/tusklang)
- **Stack Overflow**: [TuskLang tag](https://stackoverflow.com/questions/tagged/tusklang)

### Providing Debug Information

When reporting issues, include:

```bash
# System information
uname -a
bash --version
tsk version

# Environment
env | grep TSK
env | grep PEANUT

# Debug output
TSK_DEBUG=1 TSK_VERBOSE=1 tsk [command] 2>&1

# Log files
cat ~/.cache/tsk/tsk.log
cat ~/.cache/tsk/peanut.log
```

## Prevention

### Best Practices

1. **Regular backups** of configuration and databases
2. **Version control** for configuration files
3. **Testing** in development before production
4. **Monitoring** of CLI performance and errors
5. **Documentation** of custom configurations

### Maintenance

```bash
# Regular maintenance script
#!/bin/bash
# maintenance.sh

echo "Running TuskLang CLI maintenance..."

# Update CLI
tsk update

# Clear old caches
tsk cache clear

# Validate configurations
tsk config validate

# Check database health
tsk db status

echo "Maintenance complete"
```

---

For additional help, consult the [Command Reference](./commands/README.md) or [Examples](./examples/README.md). 