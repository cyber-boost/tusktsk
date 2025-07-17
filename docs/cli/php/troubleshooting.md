# Troubleshooting Guide - PHP CLI

Common issues and solutions for the TuskLang PHP CLI.

## Quick Diagnosis

```bash
# Check system status
tsk status

# Check PHP version and extensions
php --version
php -m | grep -E "(pdo|json|mbstring)"

# Check CLI installation
which tsk
tsk --version

# Enable debug mode
tsk --debug status
```

## Common Issues

### 1. Command Not Found

**Problem:** `tsk: command not found`

**Solutions:**

```bash
# Check if tsk is installed
which tsk

# If not found, install globally
curl -sSL tusklang.org/tsk.sh | sudo bash

# Or add to PATH
export PATH="/opt/tusklang/bin:$PATH"
echo 'export PATH="/opt/tusklang/bin:$PATH"' >> ~/.bashrc

# Or use full path
/opt/tusklang/bin/tsk version
```

**PHP Integration:**
```php
<?php
// Use full path in PHP scripts
$tskPath = '/opt/tusklang/bin/tsk';
exec("$tskPath version", $output, $returnCode);
```

### 2. PHP Version Too Old

**Problem:** `PHP 8.1+ required`

**Solutions:**

```bash
# Ubuntu/Debian
sudo apt update
sudo apt install software-properties-common
sudo add-apt-repository ppa:ondrej/php
sudo apt install php8.2 php8.2-cli php8.2-common

# macOS
brew install php@8.2
brew link php@8.2

# Windows
# Download from https://windows.php.net/download/
```

**Verification:**
```bash
php --version
# Should show PHP 8.1 or higher
```

### 3. Missing PHP Extensions

**Problem:** `Extension 'pdo' not found`

**Solutions:**

```bash
# Install required extensions
sudo apt install php8.2-pdo php8.2-json php8.2-mbstring

# Check installed extensions
php -m | grep -E "(pdo|json|mbstring|msgpack)"

# Install optional extensions
sudo apt install php8.2-msgpack php8.2-curl php8.2-openssl
```

**Manual Installation:**
```bash
# Install via PECL
pecl install msgpack
echo "extension=msgpack.so" | sudo tee -a /etc/php/8.2/cli/php.ini
```

### 4. Permission Denied

**Problem:** `Permission denied` or `Cannot write to file`

**Solutions:**

```bash
# Fix CLI permissions
sudo chmod +x /usr/local/bin/tsk

# Fix directory permissions
sudo chown -R $USER:$USER /opt/tusklang
sudo chmod -R 755 /opt/tusklang

# Create config directory with proper permissions
sudo mkdir -p /etc/tusklang/config
sudo chown -R $USER:$USER /etc/tusklang
```

**PHP Script Permissions:**
```php
<?php
// Check if script can write to directories
$configDir = '/etc/tusklang/config';
if (!is_writable($configDir)) {
    throw new Exception("Cannot write to $configDir");
}
```

### 5. Configuration Errors

**Problem:** `Configuration error` or `Invalid configuration`

**Solutions:**

```bash
# Check configuration syntax
tsk parse config.tsk

# Validate configuration
tsk validate config.tsk

# Show configuration details
tsk config check

# Clear configuration cache
tsk config clear-cache
```

**Debug Configuration:**
```bash
# Enable verbose output
tsk config get app.name --verbose

# Debug mode
tsk --debug config get app.name

# Check configuration hierarchy
tsk config load . --verbose
```

### 6. Database Connection Issues

**Problem:** `Database connection failed`

**Solutions:**

```bash
# Check database status
tsk db status

# Test database connection
tsk db status --verbose

# Check database configuration
tsk config get database.host
tsk config get database.port
tsk config get database.name
```

**Database Troubleshooting:**
```bash
# Check if database server is running
sudo systemctl status mysql
sudo systemctl status postgresql

# Test connection manually
mysql -h localhost -u username -p database_name
psql -h localhost -U username -d database_name
```

### 7. License Issues

**Problem:** `License invalid` or `Feature not available`

**Solutions:**

```bash
# Check license status
tsk license check

# Activate license
tsk license activate YOUR_LICENSE_KEY

# Check available features
tsk license check --features

# Offline check
tsk license check --offline
```

**License Troubleshooting:**
```bash
# Force revalidation
tsk license check --force

# Verbose license information
tsk license check --verbose

# Check license configuration
tsk config get license
```

### 8. Service Management Issues

**Problem:** `Service not found` or `Service failed to start`

**Solutions:**

```bash
# Check service status
tsk services status

# List available services
tsk services list

# Start services
tsk services start

# Restart services
tsk services restart
```

**Service Debugging:**
```bash
# Verbose service information
tsk services status --verbose

# Check service logs
sudo journalctl -u tusklang-service -f

# Manual service start
sudo systemctl start tusklang-service
```

### 9. Cache Issues

**Problem:** `Cache error` or `Cache not working`

**Solutions:**

```bash
# Check cache status
tsk cache status

# Clear cache
tsk cache clear

# Warm cache
tsk cache warm

# Check memcached
tsk cache memcached status
```

**Cache Troubleshooting:**
```bash
# Check cache configuration
tsk config get cache

# Test cache connection
tsk cache memcached test

# Restart cache service
tsk cache memcached restart
```

### 10. Network Issues

**Problem:** `Network error` or `Connection timeout`

**Solutions:**

```bash
# Check network connectivity
ping tusklang.org

# Test HTTPS connection
curl -I https://tusklang.org

# Check proxy settings
echo $http_proxy
echo $https_proxy
```

**Network Configuration:**
```bash
# Set proxy if needed
export http_proxy=http://proxy.example.com:8080
export https_proxy=http://proxy.example.com:8080

# Test with proxy
curl --proxy http://proxy.example.com:8080 https://tusklang.org
```

## Debug Mode

### Enable Debug Output

```bash
# Global debug mode
tsk --debug version

# Command-specific debug
tsk --debug config get app.name
tsk --debug db status
tsk --debug services status
```

### Debug Configuration

```bash
# Check debug logs
tail -f /var/log/tusklang/cli.log

# Enable debug logging
export TUSKLANG_DEBUG=1
tsk status

# Check debug configuration
tsk config get debug
```

### PHP Debug Integration

```php
<?php
// Enable debug mode in PHP
putenv('TUSKLANG_DEBUG=1');

// Execute with debug
$output = [];
$returnCode = 0;
exec('tsk --debug status 2>&1', $output, $returnCode);

// Parse debug output
foreach ($output as $line) {
    if (strpos($line, 'DEBUG:') !== false) {
        echo "Debug: $line\n";
    }
}
```

## Performance Issues

### Slow Command Execution

**Diagnosis:**
```bash
# Time command execution
time tsk config get app.name

# Check system resources
top
free -h
df -h
```

**Solutions:**
```bash
# Use binary configuration
tsk config compile config.tsk
tsk config load config.pnt

# Clear cache
tsk cache clear

# Optimize configuration
tsk config optimize
```

### Memory Issues

**Diagnosis:**
```bash
# Check memory usage
ps aux | grep tsk
free -h

# Monitor memory during execution
/usr/bin/time -v tsk config load .
```

**Solutions:**
```bash
# Increase PHP memory limit
php -d memory_limit=512M -f /opt/tusklang/bin/tsk

# Use streaming for large configurations
tsk config load . --stream
```

## Platform-Specific Issues

### Linux Issues

```bash
# Check systemd services
sudo systemctl status tusklang

# Check SELinux
getenforce
sudo setsebool -P httpd_can_network_connect 1

# Check AppArmor
sudo aa-status
```

### macOS Issues

```bash
# Check Homebrew installation
brew list | grep php
brew services list

# Fix permissions
sudo chown -R $(whoami) /usr/local/bin
sudo chown -R $(whoami) /usr/local/lib
```

### Windows Issues

```bash
# Check PATH
echo $env:PATH

# Run as Administrator
Start-Process powershell -Verb RunAs

# Check Windows Defender
Get-MpComputerStatus
```

## Getting Help

### Self-Diagnosis

```bash
# System information
tsk status --verbose

# Configuration check
tsk config check

# Database connectivity
tsk db status --verbose

# Service status
tsk services status --verbose
```

### Log Files

```bash
# CLI logs
tail -f /var/log/tusklang/cli.log

# System logs
sudo journalctl -u tusklang -f

# PHP error logs
tail -f /var/log/php_errors.log
```

### Support Resources

1. **Documentation**: [docs.tusklang.org](https://docs.tusklang.org)
2. **GitHub Issues**: [github.com/tuskphp/tusklang/issues](https://github.com/tuskphp/tusklang/issues)
3. **Community**: [community.tusklang.org](https://community.tusklang.org)
4. **Email Support**: support@tusklang.org

### Reporting Issues

When reporting issues, include:

```bash
# System information
tsk status --json
php --version
uname -a

# Error details
tsk --debug <failing-command> 2>&1

# Configuration (sanitized)
tsk config get --json | jq 'del(.database.password)'
```

## Prevention

### Best Practices

1. **Regular Updates**: Keep TuskLang CLI updated
2. **Backup Configuration**: Regular backups of configuration files
3. **Monitor Logs**: Check logs regularly for issues
4. **Test Changes**: Test configuration changes in development
5. **Documentation**: Document custom configurations

### Maintenance Scripts

```bash
#!/bin/bash
# maintenance.sh

echo "Running TuskLang maintenance..."

# Check system status
tsk status

# Update configuration cache
tsk config clear-cache
tsk config compile

# Check database
tsk db status

# Verify services
tsk services status

# Check license
tsk license check

echo "Maintenance complete!"
```

## See Also

- [Installation Guide](./installation.md)
- [Quick Start Guide](./quickstart.md)
- [Command Reference](./commands/README.md)
- [Examples](./examples/README.md)

**Strong. Secure. Scalable.** 🐘 