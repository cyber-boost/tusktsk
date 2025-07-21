# Installation Guide for TuskLang Go SDK

This guide provides comprehensive installation instructions for the TuskLang Go SDK.

## Prerequisites

### System Requirements

- **Go**: Version 1.22 or higher (1.23 recommended)
- **Git**: For cloning the repository
- **Docker**: Optional, for containerized deployment
- **Docker Compose**: Optional, for multi-service deployment

### Operating Systems

- **Linux**: Ubuntu 20.04+, CentOS 8+, RHEL 8+
- **macOS**: 10.15+ (Catalina or later)
- **Windows**: Windows 10+ or Windows Server 2019+

## Installation Methods

### Method 1: Go Module Installation (Recommended)

```bash
# Install via go get
go get github.com/cyber-boost/tusktsk/sdk/go@latest

# Or install specific version
go get github.com/cyber-boost/tusktsk/sdk/go@v1.0.0
```

### Method 2: Clone and Build

```bash
# Clone the repository
git clone https://github.com/cyber-boost/tusktsk.git
cd tusktsk/sdk/go

# Install dependencies
go mod download
go mod tidy

# Build the binary
go build -o tusktsk .

# Install globally (optional)
sudo cp tusktsk /usr/local/bin/
```

### Method 3: Docker Installation

```bash
# Pull the Docker image
docker pull ghcr.io/cyber-boost/tusktsk/go-sdk:latest

# Run the container
docker run -it ghcr.io/cyber-boost/tusktsk/go-sdk:latest --help
```

### Method 4: Binary Download

1. Visit the [GitHub Releases](https://github.com/cyber-boost/tusktsk/releases)
2. Download the appropriate binary for your platform
3. Extract and run:

```bash
# Linux/macOS
chmod +x tusktsk-linux-amd64
./tusktsk-linux-amd64 --help

# Windows
tusktsk-windows-amd64.exe --help
```

## Configuration

### Environment Variables

```bash
# Development environment
export TUSKTSK_ENV=development
export TUSKTSK_LOG_LEVEL=debug

# Production environment
export TUSKTSK_ENV=production
export TUSKTSK_LOG_LEVEL=info
```

### Configuration File

Create a `config.yaml` file:

```yaml
# config.yaml
environment: development
log_level: debug
database:
  host: localhost
  port: 3306
  name: tusktsk_db
  user: tusktsk_user
  password: tusktsk_password
redis:
  host: localhost
  port: 6379
  password: ""
```

## Quick Start

### 1. Basic Usage

```bash
# Check version
tusktsk --version

# Show help
tusktsk --help

# Run with configuration
tusktsk --config config.yaml
```

### 2. Docker Compose Setup

```bash
# Start all services
docker-compose up -d

# Check service status
docker-compose ps

# View logs
docker-compose logs -f tusktsk-go
```

### 3. Development Setup

```bash
# Install development dependencies
go mod download

# Run tests
go test ./...

# Run with hot reload (if using air)
air

# Build for development
go build -race -o tusktsk .
```

## Database Setup

### MySQL

```bash
# Install MySQL (Ubuntu/Debian)
sudo apt update
sudo apt install mysql-server

# Create database and user
mysql -u root -p
```

```sql
CREATE DATABASE tusktsk_db;
CREATE USER 'tusktsk_user'@'localhost' IDENTIFIED BY 'tusktsk_password';
GRANT ALL PRIVILEGES ON tusktsk_db.* TO 'tusktsk_user'@'localhost';
FLUSH PRIVILEGES;
```

### Redis

```bash
# Install Redis (Ubuntu/Debian)
sudo apt update
sudo apt install redis-server

# Start Redis
sudo systemctl start redis-server
sudo systemctl enable redis-server
```

### MongoDB (Optional)

```bash
# Install MongoDB (Ubuntu/Debian)
wget -qO - https://www.mongodb.org/static/pgp/server-7.0.asc | sudo apt-key add -
echo "deb [ arch=amd64,arm64 ] https://repo.mongodb.org/apt/ubuntu focal/mongodb-org/7.0 multiverse" | sudo tee /etc/apt/sources.list.d/mongodb-org-7.0.list
sudo apt update
sudo apt install mongodb-org

# Start MongoDB
sudo systemctl start mongod
sudo systemctl enable mongod
```

## Verification

### 1. Check Installation

```bash
# Verify Go installation
go version

# Verify SDK installation
tusktsk --version

# Check dependencies
go mod verify
```

### 2. Run Tests

```bash
# Run all tests
go test ./...

# Run tests with coverage
go test -cover ./...

# Run tests with race detection
go test -race ./...
```

### 3. Check Services

```bash
# Check MySQL connection
mysql -u tusktsk_user -p tusktsk_db -e "SELECT 1;"

# Check Redis connection
redis-cli ping

# Check MongoDB connection (if installed)
mongosh --eval "db.runCommand('ping')"
```

## Troubleshooting

### Common Issues

#### 1. Go Version Issues

```bash
# Check Go version
go version

# Update Go if needed
# Visit https://golang.org/dl/ for latest version
```

#### 2. Permission Issues

```bash
# Fix file permissions
chmod +x tusktsk

# Fix directory permissions
sudo chown -R $USER:$USER /usr/local/bin/tusktsk
```

#### 3. Database Connection Issues

```bash
# Check MySQL service
sudo systemctl status mysql

# Check Redis service
sudo systemctl status redis-server

# Check network connectivity
telnet localhost 3306
telnet localhost 6379
```

#### 4. Docker Issues

```bash
# Check Docker service
sudo systemctl status docker

# Check Docker Compose
docker-compose --version

# Clean up Docker resources
docker system prune -a
```

### Logs and Debugging

```bash
# Enable debug logging
export TUSKTSK_LOG_LEVEL=debug

# Run with verbose output
tusktsk --verbose

# Check system logs
journalctl -u tusktsk -f
```

## Security Considerations

### 1. Firewall Configuration

```bash
# Allow necessary ports
sudo ufw allow 8080/tcp  # Application port
sudo ufw allow 3306/tcp  # MySQL (if external)
sudo ufw allow 6379/tcp  # Redis (if external)
```

### 2. User Permissions

```bash
# Create dedicated user
sudo useradd -r -s /bin/false tusktsk

# Set proper ownership
sudo chown -R tusktsk:tusktsk /opt/tusktsk
```

### 3. SSL/TLS Configuration

```bash
# Generate SSL certificate
openssl req -x509 -newkey rsa:4096 -keyout key.pem -out cert.pem -days 365 -nodes

# Configure TLS in application
```

## Performance Tuning

### 1. Go Runtime

```bash
# Set Go runtime parameters
export GOMAXPROCS=4
export GOGC=100
export GOMEMLIMIT=1GiB
```

### 2. Database Optimization

```sql
-- MySQL optimization
SET GLOBAL innodb_buffer_pool_size = 1073741824;  -- 1GB
SET GLOBAL max_connections = 200;
```

### 3. System Resources

```bash
# Increase file descriptors
echo "* soft nofile 65536" | sudo tee -a /etc/security/limits.conf
echo "* hard nofile 65536" | sudo tee -a /etc/security/limits.conf
```

## Uninstallation

### 1. Remove Binary

```bash
# Remove global installation
sudo rm /usr/local/bin/tusktsk

# Remove local binary
rm tusktsk
```

### 2. Remove Docker Images

```bash
# Remove Docker image
docker rmi ghcr.io/cyber-boost/tusktsk/go-sdk:latest

# Remove all related images
docker images | grep tusktsk | awk '{print $3}' | xargs docker rmi
```

### 3. Clean Dependencies

```bash
# Remove Go module cache
go clean -modcache

# Remove vendor directory
rm -rf vendor/
```

## Support

### Getting Help

1. **Documentation**: Check this guide and README.md
2. **GitHub Issues**: Report bugs and request features
3. **Discussions**: Join community discussions
4. **Email**: Contact the development team

### Useful Commands

```bash
# Check system information
uname -a
go version
docker --version

# Check disk space
df -h

# Check memory usage
free -h

# Check network connectivity
ping google.com
```

### Log Locations

- **Application logs**: `/var/log/tusktsk/`
- **System logs**: `/var/log/syslog`
- **Docker logs**: `docker logs <container_name>`
- **Go test logs**: Console output 