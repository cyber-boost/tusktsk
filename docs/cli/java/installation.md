# Installation Guide for TuskLang Java CLI

This guide covers installing and setting up the TuskLang Java CLI on various platforms.

## Prerequisites

- **Java**: Version 8 or higher (Java 11+ recommended)
- **Maven**: Version 3.6+ or Gradle 6.0+
- **Memory**: Minimum 512MB RAM, 1GB+ recommended
- **Disk Space**: 100MB for installation

## Installation Methods

### Method 1: Maven Installation (Recommended)

```bash
# Clone the repository
git clone https://github.com/tusklang/tusklang-java-sdk.git
cd tusklang-java-sdk

# Build and install
mvn clean install

# Create executable script
echo '#!/bin/bash
java -jar target/tusklang-java-sdk-2.0.0.jar "$@"' > /usr/local/bin/tsk
chmod +x /usr/local/bin/tsk
```

### Method 2: Direct Download

```bash
# Download the JAR file
curl -sSL https://tusklang.org/java/tsk.jar -o /usr/local/bin/tsk.jar

# Create executable wrapper
echo '#!/bin/bash
java -jar /usr/local/bin/tsk.jar "$@"' > /usr/local/bin/tsk
chmod +x /usr/local/bin/tsk
```

### Method 3: Package Manager (Linux)

#### Ubuntu/Debian
```bash
# Add repository
curl -sSL https://tusklang.org/apt/tusklang.gpg | sudo apt-key add -
echo "deb https://tusklang.org/apt/ stable main" | sudo tee /etc/apt/sources.list.d/tusklang.list

# Install
sudo apt update
sudo apt install tusklang-java-cli
```

#### CentOS/RHEL
```bash
# Add repository
sudo yum install https://tusklang.org/rpm/tusklang-java-cli.rpm
```

### Method 4: Homebrew (macOS)

```bash
# Install via Homebrew
brew install tusklang/tap/tusklang-java-cli
```

## Verification

After installation, verify the setup:

```bash
# Check version
tsk version

# Check help
tsk help

# Test basic functionality
tsk config check
```

Expected output:
```
TuskLang Java CLI v2.0.0
Java Runtime: OpenJDK 11.0.12
Platform: Linux x86_64
✅ Installation verified successfully
```

## Configuration

### Environment Variables

Set these environment variables for optimal performance:

```bash
# JVM Options
export JAVA_OPTS="-Xms512m -Xmx2g -XX:+UseG1GC"

# TuskLang specific
export TSK_CONFIG_DIR="$HOME/.tusk"
export TSK_CACHE_DIR="$HOME/.tusk/cache"
export TSK_LOG_LEVEL="INFO"
```

### Configuration Files

Create configuration directory:

```bash
mkdir -p ~/.tusk
mkdir -p ~/.tusk/cache
mkdir -p ~/.tusk/logs
```

### Spring Boot Integration

For Spring Boot applications, add to `application.properties`:

```properties
# TuskLang configuration
tusk.config.auto-load=true
tusk.config.watch=true
tusk.config.cache-enabled=true
tusk.config.binary-prefer=true
```

## Platform-Specific Notes

### Linux

- **Systemd Service**: Create service file for background operation
- **Permissions**: Ensure proper file permissions for configuration access
- **Memory**: Adjust JVM heap size based on available RAM

### macOS

- **Gatekeeper**: May need to allow execution of downloaded JAR
- **Homebrew**: Recommended installation method
- **Xcode**: Install command line tools if needed

### Windows

- **PATH**: Add Java and TuskLang to system PATH
- **Antivirus**: Whitelist TuskLang JAR files
- **Line Endings**: Use LF line endings for configuration files

## Troubleshooting

### Common Issues

#### Java Not Found
```bash
# Check Java installation
java -version

# Set JAVA_HOME if needed
export JAVA_HOME=/usr/lib/jvm/java-11-openjdk
export PATH=$JAVA_HOME/bin:$PATH
```

#### Permission Denied
```bash
# Fix executable permissions
chmod +x /usr/local/bin/tsk
chmod +x /usr/local/bin/tsk.jar

# Check file ownership
sudo chown $USER:$USER /usr/local/bin/tsk*
```

#### Memory Issues
```bash
# Increase JVM heap size
export JAVA_OPTS="-Xms1g -Xmx4g"
tsk version
```

#### Network Issues
```bash
# Check connectivity
curl -I https://tusklang.org

# Use proxy if needed
export HTTP_PROXY=http://proxy.company.com:8080
export HTTPS_PROXY=http://proxy.company.com:8080
```

### Log Files

Check logs for detailed error information:

```bash
# View recent logs
tail -f ~/.tusk/logs/tsk.log

# Check error logs
cat ~/.tusk/logs/error.log
```

### Support

For additional help:

- **Documentation**: https://docs.tusklang.org/java
- **GitHub Issues**: https://github.com/tusklang/tusklang-java-sdk/issues
- **Community**: https://community.tusklang.org

## Next Steps

After successful installation:

1. [Quick Start Guide](./quickstart.md)
2. [Command Reference](./commands/README.md)
3. [Configuration Guide](../java/docs/PNT_GUIDE.md)
4. [Examples](./examples/README.md) 