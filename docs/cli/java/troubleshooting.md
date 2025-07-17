# Troubleshooting Guide

Common issues and solutions for TuskLang Java CLI.

## Quick Diagnosis

### Check Installation

```bash
# Verify CLI installation
tsk version

# Check Java version
java -version

# Verify environment
echo $JAVA_HOME
echo $PATH
```

### Check Configuration

```bash
# Validate configuration
tsk config validate ./

# Check configuration hierarchy
tsk config check ./

# Show configuration statistics
tsk config stats
```

## Common Issues

### Installation Problems

#### Java Not Found

**Symptoms:**
```bash
❌ Error: Java runtime not found
```

**Solutions:**
```bash
# Install Java 8+
sudo apt update
sudo apt install openjdk-11-jdk

# Set JAVA_HOME
export JAVA_HOME=/usr/lib/jvm/java-11-openjdk-amd64
export PATH=$JAVA_HOME/bin:$PATH

# Verify installation
java -version
```

#### Permission Denied

**Symptoms:**
```bash
❌ Permission denied: tsk
```

**Solutions:**
```bash
# Fix executable permissions
chmod +x /usr/local/bin/tsk
chmod +x /usr/local/bin/tsk.jar

# Check ownership
sudo chown $USER:$USER /usr/local/bin/tsk*
```

#### Memory Issues

**Symptoms:**
```bash
❌ OutOfMemoryError: Java heap space
```

**Solutions:**
```bash
# Increase JVM heap size
export JAVA_OPTS="-Xms1g -Xmx4g -XX:+UseG1GC"

# Check available memory
free -h

# Monitor memory usage
tsk version --verbose
```

### Configuration Issues

#### File Not Found

**Symptoms:**
```bash
❌ Configuration file not found: peanu.peanuts
```

**Solutions:**
```bash
# Check file hierarchy
tsk config check ./

# Create default configuration
cat > peanu.peanuts << EOF
[app]
name: "My Application"
version: "1.0.0"

[server]
host: "localhost"
port: 8080
EOF

# Verify file exists
ls -la peanu.peanuts
```

#### Invalid Configuration

**Symptoms:**
```bash
❌ Invalid configuration syntax at line 5
```

**Solutions:**
```bash
# Validate configuration
tsk config validate ./

# Check syntax
tsk parse peanu.peanuts

# Show detailed errors
tsk config validate ./ --verbose
```

#### Binary Compilation Failed

**Symptoms:**
```bash
❌ Failed to compile configuration to binary
```

**Solutions:**
```bash
# Check file permissions
ls -la *.peanuts

# Recompile with verbose output
tsk config compile ./ --verbose

# Check disk space
df -h

# Verify MessagePack dependency
mvn dependency:tree | grep msgpack
```

### Database Issues

#### Connection Refused

**Symptoms:**
```bash
❌ Database connection failed: Connection refused
```

**Solutions:**
```bash
# Check database service
sudo systemctl status postgresql

# Verify connection string
tsk db status --verbose

# Test network connectivity
telnet localhost 5432

# Check firewall
sudo ufw status
```

#### Migration Failures

**Symptoms:**
```bash
❌ Migration failed: table already exists
```

**Solutions:**
```bash
# Check migration status
tsk db console
> SELECT * FROM schema_migrations;

# Force migration
tsk db migrate migrations/ --force

# Rollback and retry
tsk db restore backup.sql
tsk db migrate migrations/
```

#### Performance Issues

**Symptoms:**
```bash
⚠️ Slow database queries detected
```

**Solutions:**
```bash
# Analyze performance
tsk db status --performance

# Check slow queries
tsk db console
> EXPLAIN QUERY PLAN SELECT * FROM users WHERE email = 'test@example.com';

# Optimize indexes
tsk db console
> CREATE INDEX idx_users_email ON users(email);
```

### Development Server Issues

#### Port Already in Use

**Symptoms:**
```bash
❌ Port 8080 is already in use
```

**Solutions:**
```bash
# Find process using port
sudo lsof -i :8080

# Kill process
sudo kill -9 <PID>

# Use different port
tsk serve 8081

# Check all listening ports
netstat -tlnp
```

#### Hot Reload Not Working

**Symptoms:**
```bash
⚠️ File changes not detected
```

**Solutions:**
```bash
# Enable file watching
tsk serve 8080 --watch

# Check file permissions
ls -la src/

# Verify file system support
mount | grep inotify

# Manual reload
curl -X POST http://localhost:8080/reload
```

### Testing Issues

#### Test Failures

**Symptoms:**
```bash
❌ Test suite failed: 5 tests failed
```

**Solutions:**
```bash
# Run tests with verbose output
tsk test all --verbose

# Run specific test
tsk test parser

# Check test configuration
tsk test --config

# Debug test environment
tsk test --debug
```

#### Performance Test Failures

**Symptoms:**
```bash
❌ Performance test failed: Response time exceeded threshold
```

**Solutions:**
```bash
# Run performance analysis
tsk test performance --detailed

# Check system resources
htop

# Optimize JVM settings
export JAVA_OPTS="-XX:+UseG1GC -XX:MaxGCPauseMillis=200"

# Run with profiling
tsk test performance --profile
```

### Cache Issues

#### Cache Connection Failed

**Symptoms:**
```bash
❌ Cache connection failed: Connection refused
```

**Solutions:**
```bash
# Check cache service
tsk cache status

# Start cache service
tsk cache memcached start

# Verify cache configuration
tsk config get cache.host
tsk config get cache.port

# Test cache connection
tsk cache memcached test
```

#### Cache Performance Issues

**Symptoms:**
```bash
⚠️ Cache hit ratio below 80%
```

**Solutions:**
```bash
# Check cache statistics
tsk cache memcached stats

# Warm cache
tsk cache warm

# Clear cache
tsk cache clear

# Optimize cache settings
tsk config set cache.ttl 3600
```

## Debug Mode

### Enable Debug Logging

```bash
# Set debug level
export TSK_LOG_LEVEL=DEBUG

# Run command with debug
tsk serve 8080 --debug

# Check debug logs
tail -f ~/.tusk/logs/debug.log
```

### Verbose Output

```bash
# Get detailed information
tsk db status --verbose
tsk config validate ./ --verbose
tsk test all --verbose

# JSON output for parsing
tsk db status --json
tsk config stats --json
```

### System Information

```bash
# Get system details
tsk version --system

# Check environment
tsk version --env

# Verify dependencies
tsk version --deps
```

## Performance Troubleshooting

### Memory Leaks

**Detection:**
```bash
# Monitor memory usage
jstat -gc <PID> 1000

# Check heap dump
jmap -dump:format=b,file=heap.hprof <PID>
```

**Solutions:**
```bash
# Increase heap size
export JAVA_OPTS="-Xms2g -Xmx8g"

# Enable GC logging
export JAVA_OPTS="$JAVA_OPTS -Xloggc:gc.log -XX:+PrintGCDetails"

# Use G1GC
export JAVA_OPTS="$JAVA_OPTS -XX:+UseG1GC"
```

### Slow Startup

**Detection:**
```bash
# Measure startup time
time tsk version

# Profile startup
tsk version --profile-startup
```

**Solutions:**
```bash
# Optimize classpath
export CLASSPATH=/path/to/optimized/jars

# Use parallel class loading
export JAVA_OPTS="$JAVA_OPTS -XX:+UseParallelGC"

# Reduce logging during startup
export TSK_LOG_LEVEL=WARN
```

### Network Issues

**Detection:**
```bash
# Test network connectivity
ping tusklang.org

# Check DNS resolution
nslookup tusklang.org

# Test HTTP connectivity
curl -I https://tusklang.org
```

**Solutions:**
```bash
# Use proxy if needed
export HTTP_PROXY=http://proxy.company.com:8080
export HTTPS_PROXY=http://proxy.company.com:8080

# Configure DNS
echo "nameserver 8.8.8.8" | sudo tee -a /etc/resolv.conf

# Check firewall rules
sudo iptables -L
```

## Recovery Procedures

### Configuration Recovery

```bash
# Restore from backup
cp peanu.peanuts.backup peanu.peanuts

# Recompile configuration
tsk config compile ./

# Validate restored configuration
tsk config validate ./
```

### Database Recovery

```bash
# Restore from backup
tsk db restore backup_20241219.sql

# Verify data integrity
tsk db status --verbose

# Run integrity checks
tsk db console
> PRAGMA integrity_check;
```

### Service Recovery

```bash
# Restart services
tsk services restart

# Check service status
tsk services status

# Verify all services
tsk services status --all
```

## Getting Help

### Log Files

```bash
# View recent logs
tail -f ~/.tusk/logs/tsk.log

# Check error logs
cat ~/.tusk/logs/error.log

# View debug logs
cat ~/.tusk/logs/debug.log
```

### Support Resources

- **Documentation**: https://docs.tusklang.org/java
- **GitHub Issues**: https://github.com/tusklang/tusklang-java-sdk/issues
- **Community Forum**: https://community.tusklang.org
- **Stack Overflow**: Tag questions with `tusklang-java`

### Reporting Issues

When reporting issues, include:

1. **Version information**: `tsk version --verbose`
2. **System information**: `tsk version --system`
3. **Error logs**: Relevant log file contents
4. **Steps to reproduce**: Detailed reproduction steps
5. **Expected vs actual behavior**: Clear description

### Example Issue Report

```bash
# Collect diagnostic information
tsk version --verbose > version.txt
tsk version --system > system.txt
tail -n 100 ~/.tusk/logs/error.log > error.log

# Package for support
tar -czf issue-report.tar.gz version.txt system.txt error.log
```

## Prevention

### Best Practices

1. **Regular Updates**: Keep TuskLang CLI updated
2. **Backup Strategy**: Regular configuration and database backups
3. **Monitoring**: Set up monitoring for critical services
4. **Testing**: Comprehensive test coverage
5. **Documentation**: Maintain up-to-date documentation

### Maintenance Schedule

- **Daily**: Check service status and logs
- **Weekly**: Run performance tests and backups
- **Monthly**: Update dependencies and security patches
- **Quarterly**: Review and optimize configuration

## Quick Reference

### Common Commands

```bash
# Health check
tsk version && tsk db status && tsk config check

# Reset configuration
rm -f *.pnt && tsk config compile ./

# Restart everything
tsk services restart && tsk cache clear

# Debug mode
export TSK_LOG_LEVEL=DEBUG && tsk <command> --verbose
```

### Emergency Procedures

```bash
# Stop all services
tsk services stop

# Clear all caches
tsk cache clear

# Restore from backup
tsk db restore latest_backup.sql

# Restart with safe mode
tsk serve 8080 --safe-mode
``` 