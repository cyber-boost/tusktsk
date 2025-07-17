# Troubleshooting Guide

Common issues and solutions for TuskLang Go CLI.

## Quick Diagnosis

### Check CLI Status

```bash
# Verify CLI installation
tsk version

# Check configuration
tsk config check .

# Test database connection
tsk db status

# Validate syntax
tsk validate peanu.peanuts
```

## Common Issues

### 1. Command Not Found

**Problem:** `tsk: command not found`

**Solutions:**

```bash
# Check if binary exists
which tsk
ls -la /usr/local/bin/tsk

# Reinstall CLI
cd go-sdk
make install

# Add to PATH manually
export PATH=$PATH:$GOPATH/bin
echo 'export PATH=$PATH:$GOPATH/bin' >> ~/.bashrc
```

**Prevention:**
- Always use `make install` after building
- Ensure Go bin directory is in PATH

### 2. Permission Denied

**Problem:** `permission denied` when running commands

**Solutions:**

```bash
# Fix binary permissions
sudo chmod +x /usr/local/bin/tsk

# Use user installation
make install-user

# Fix ownership
sudo chown $USER:$USER /usr/local/bin/tsk
```

**Prevention:**
- Use `make install-user` for user-only installation
- Check file permissions after installation

### 3. Configuration File Not Found

**Problem:** `configuration file not found`

**Solutions:**

```bash
# Check if file exists
ls -la peanu.peanuts

# Create default configuration
tsk init

# Check configuration hierarchy
tsk config check .

# Create minimal configuration
cat > peanu.peanuts << EOF
[app]
name: "My App"
version: "1.0.0"
EOF
```

**Prevention:**
- Always run `tsk init` in new projects
- Use version control for configuration files

### 4. Database Connection Failed

**Problem:** `database connection failed`

**Solutions:**

```bash
# Check database status
tsk db status

# Initialize database
tsk db init

# Check configuration
tsk config get database.host
tsk config get database.port

# Test connection manually
psql -h localhost -p 5432 -U postgres -d myapp
```

**Common Causes:**
- Database server not running
- Wrong credentials
- Network connectivity issues
- Firewall blocking connection

### 5. Go Module Issues

**Problem:** `go: module not found` or dependency errors

**Solutions:**

```bash
# Enable Go modules
export GO111MODULE=on
go env -w GO111MODULE=on

# Download dependencies
go mod tidy
go mod download

# Clear module cache
go clean -modcache

# Update dependencies
go get -u ./...
```

**Prevention:**
- Always use Go modules (Go 1.11+)
- Keep go.mod and go.sum in version control

### 6. Binary Compilation Errors

**Problem:** `failed to compile binary` or `.pnt file issues`

**Solutions:**

```bash
# Check source file syntax
tsk validate peanu.peanuts

# Recompile binary
tsk peanuts compile peanu.peanuts

# Check file permissions
ls -la *.pnt

# Verify binary integrity
tsk peanuts verify peanu.pnt
```

**Common Causes:**
- Syntax errors in source file
- Insufficient disk space
- Permission issues
- Corrupted source file

### 7. Performance Issues

**Problem:** Slow command execution or high memory usage

**Solutions:**

```bash
# Check system resources
top
df -h

# Clear cache
tsk cache clear

# Use binary format
tsk peanuts compile peanu.peanuts

# Optimize configuration
tsk optimize peanu.peanuts
```

**Optimization Tips:**
- Use `.pnt` binary format in production
- Implement proper caching strategies
- Monitor memory usage
- Use connection pooling for databases

## Platform-Specific Issues

### Linux

#### Library Dependencies

```bash
# Install required libraries
sudo apt install libc6-dev build-essential

# Fix shared library issues
sudo ldconfig
```

#### Permission Issues

```bash
# Fix ownership
sudo chown -R $USER:$USER ~/.tusklang

# Fix permissions
chmod 755 ~/.tusklang
chmod 644 ~/.tusklang/config/*
```

### macOS

#### Homebrew Issues

```bash
# Fix Homebrew permissions
sudo chown -R $(whoami) /usr/local/bin /usr/local/lib /usr/local/sbin
chmod u+w /usr/local/bin /usr/local/lib /usr/local/sbin

# Reinstall Go
brew reinstall go
```

#### Gatekeeper Issues

```bash
# Allow execution
sudo xattr -rd com.apple.quarantine /usr/local/bin/tsk

# Or install to user directory
make install-user
```

### Windows

#### PATH Issues

```powershell
# Add Go bin to PATH
$env:PATH += ";$env:GOPATH\bin"

# Add to system PATH permanently
[Environment]::SetEnvironmentVariable("PATH", $env:PATH + ";$env:GOPATH\bin", "User")
```

#### Git Line Endings

```powershell
# Fix line endings
git config --global core.autocrlf true

# Re-clone repository
git clone https://github.com/tusklang/go-sdk.git
```

## Debugging Techniques

### Enable Verbose Output

```bash
# Verbose mode for all commands
tsk --verbose db status
tsk --verbose config check .
tsk --verbose test all
```

### Enable Debug Logging

```bash
# Set debug environment variable
export TUSKLANG_DEBUG=true

# Or set in configuration
echo 'debug: true' >> peanu.peanuts
```

### Check Logs

```bash
# View CLI logs
tail -f ~/.tusklang/logs/cli.log

# View system logs
journalctl -u tusklang-cli -f
```

### Profile Performance

```bash
# Profile Go application
go test -cpuprofile=cpu.prof -memprofile=mem.prof ./...

# Analyze profiles
go tool pprof cpu.prof
go tool pprof mem.prof
```

## Error Codes Reference

| Code | Description | Common Causes |
|------|-------------|---------------|
| 0 | Success | - |
| 1 | General error | Configuration issues, file not found |
| 2 | Configuration error | Invalid syntax, missing required fields |
| 3 | Database error | Connection failed, query error |
| 4 | Permission error | Insufficient privileges |
| 5 | Network error | Connection timeout, DNS issues |
| 6 | Validation error | Data validation failed |
| 7 | Compilation error | Syntax error, missing dependencies |
| 8 | Timeout error | Operation exceeded time limit |

## Recovery Procedures

### Configuration Recovery

```bash
# Backup current configuration
cp peanu.peanuts peanu.peanuts.backup

# Restore from backup
cp peanu.peanuts.backup peanu.peanuts

# Validate restored configuration
tsk config validate .
```

### Database Recovery

```bash
# Create backup
tsk db backup recovery_backup.sql

# Restore from backup
tsk db restore recovery_backup.sql

# Verify restoration
tsk db status
```

### Binary Recovery

```bash
# Recompile from source
tsk peanuts compile peanu.peanuts

# Verify binary
tsk peanuts verify peanu.pnt

# Test binary loading
tsk peanuts load peanu.pnt
```

## Prevention Best Practices

### 1. Configuration Management

- Use version control for all configuration files
- Implement configuration validation in CI/CD
- Use environment-specific configurations
- Regular backup of configuration files

### 2. Database Management

- Implement automated backups
- Monitor database health regularly
- Use connection pooling
- Implement proper error handling

### 3. Development Practices

- Write comprehensive tests
- Use linting and formatting tools
- Implement proper logging
- Monitor performance metrics

### 4. Deployment Practices

- Use binary format in production
- Implement health checks
- Monitor resource usage
- Set up alerting for critical issues

## Getting Help

### Self-Service Resources

1. **Documentation**: [TuskLang Docs](https://tusklang.org/docs)
2. **Examples**: [GitHub Examples](https://github.com/tusklang/go-sdk/examples)
3. **FAQ**: [Frequently Asked Questions](https://tusklang.org/faq)

### Community Support

1. **GitHub Issues**: [Report bugs](https://github.com/tusklang/go-sdk/issues)
2. **GitHub Discussions**: [Ask questions](https://github.com/tusklang/go-sdk/discussions)
3. **Discord**: [Join community](https://discord.gg/tusklang)
4. **Stack Overflow**: [Tag: tusklang](https://stackoverflow.com/questions/tagged/tusklang)

### Professional Support

- **Enterprise Support**: [Contact sales](https://tusklang.org/enterprise)
- **Consulting**: [Professional services](https://tusklang.org/consulting)
- **Training**: [Workshops and courses](https://tusklang.org/training)

## Reporting Issues

When reporting issues, include:

1. **Environment Details**:
   - Operating system and version
   - Go version
   - TuskLang CLI version
   - Hardware specifications

2. **Error Information**:
   - Complete error message
   - Command that caused the error
   - Relevant configuration files
   - Log files

3. **Reproduction Steps**:
   - Step-by-step instructions
   - Sample configuration files
   - Expected vs actual behavior

4. **Additional Context**:
   - Previous working state
   - Recent changes made
   - Related issues or discussions

Example issue report:

```
**Environment:**
- OS: Ubuntu 22.04 LTS
- Go: 1.21.0
- TuskLang CLI: 2.0.0
- Architecture: x86_64

**Error:**
```
❌ Database connection failed
📍 Host: localhost:5432
💬 Error: connection refused
```

**Steps to Reproduce:**
1. Run `tsk db status`
2. Error occurs immediately

**Configuration:**
```ini
[database]
host: "localhost"
port: 5432
name: "myapp"
```

**Expected Behavior:**
Should connect to database successfully
``` 