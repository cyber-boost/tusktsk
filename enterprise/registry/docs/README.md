# TuskLang Package Registry Documentation

## Overview

The TuskLang Package Registry is a secure, high-performance package distribution system designed for the TuskLang ecosystem. It provides enterprise-grade security, scalability, and reliability for package management.

## Features

### Security
- **Authentication & Authorization**: JWT-based authentication with role-based access control
- **Package Signing**: Digital signatures with RSA and Ed25519 algorithms
- **Content Validation**: Malware detection and package integrity verification
- **Download Protection**: Secure download channels with integrity checks
- **Access Control**: Fine-grained permissions and ACL management

### Performance
- **Multi-Level Caching**: Memory, disk, and CDN caching for optimal performance
- **Load Balancing**: Advanced load balancing with health monitoring
- **Auto-Scaling**: Automatic scaling based on load and performance metrics
- **CDN Integration**: Global content distribution network

### Reliability
- **Backup & Recovery**: Comprehensive backup and disaster recovery system
- **Monitoring**: Real-time usage monitoring and security event logging
- **High Availability**: Multi-server deployment with failover capabilities

## Architecture

### Core Components

```
registry/
├── auth/                 # Authentication and authorization
│   ├── authentication.py
│   ├── user_management.py
│   └── permission_manager.py
├── security/            # Security systems
│   ├── access_control.py
│   ├── package_storage.py
│   ├── download_protection.py
│   ├── checksum_validator.py
│   ├── package_signing.py
│   ├── signature_verification.py
│   └── malware_detector.py
├── monitoring/          # Monitoring and analytics
│   ├── usage_monitor.py
│   └── security_logger.py
├── validation/          # Package validation
│   ├── package_validator.py
│   └── malware_detector.py
├── backup/             # Backup and recovery
│   ├── backup_manager.py
│   └── recovery_manager.py
├── performance/        # Performance optimization
│   ├── cache_manager.py
│   └── load_balancer.py
└── docs/              # Documentation
    ├── README.md
    ├── API.md
    └── SECURITY.md
```

### Data Flow

1. **Package Upload**: Packages are validated, signed, and stored securely
2. **Package Download**: Downloads are verified, cached, and distributed via CDN
3. **Access Control**: All operations are authenticated and authorized
4. **Monitoring**: All activities are logged and monitored for security

## Installation

### Prerequisites

- Python 3.8+
- PostgreSQL 12+
- Redis 6+
- Nginx (for production)

### Quick Start

```bash
# Clone the repository
git clone https://github.com/tusklang/registry.git
cd registry

# Install dependencies
pip install -r requirements.txt

# Configure environment
cp config.example.py config.py
# Edit config.py with your settings

# Initialize database
python -m registry.db.init

# Start the registry
python -m registry.server
```

### Production Deployment

```bash
# Using Docker
docker-compose up -d

# Using Kubernetes
kubectl apply -f k8s/
```

## Configuration

### Environment Variables

```bash
# Database
REGISTRY_DB_HOST=localhost
REGISTRY_DB_PORT=5432
REGISTRY_DB_NAME=registry
REGISTRY_DB_USER=registry
REGISTRY_DB_PASSWORD=secure_password

# Security
REGISTRY_SECRET_KEY=your-secret-key
REGISTRY_JWT_SECRET=your-jwt-secret
REGISTRY_SIGNING_KEY_PATH=/path/to/signing.key

# Storage
REGISTRY_STORAGE_PATH=/var/registry/storage
REGISTRY_BACKUP_PATH=/var/registry/backups
REGISTRY_CACHE_PATH=/var/registry/cache

# Performance
REGISTRY_CACHE_SIZE=1024
REGISTRY_MAX_UPLOAD_SIZE=100MB
REGISTRY_CDN_ENABLED=true
```

### Configuration File

```python
# config.py
import os

class Config:
    # Database
    DATABASE_URL = os.getenv('REGISTRY_DB_URL', 'postgresql://localhost/registry')
    
    # Security
    SECRET_KEY = os.getenv('REGISTRY_SECRET_KEY', 'dev-secret-key')
    JWT_SECRET = os.getenv('REGISTRY_JWT_SECRET', 'dev-jwt-secret')
    
    # Storage
    STORAGE_PATH = os.getenv('REGISTRY_STORAGE_PATH', '/var/registry/storage')
    BACKUP_PATH = os.getenv('REGISTRY_BACKUP_PATH', '/var/registry/backups')
    
    # Performance
    CACHE_SIZE = int(os.getenv('REGISTRY_CACHE_SIZE', 1024))
    MAX_UPLOAD_SIZE = os.getenv('REGISTRY_MAX_UPLOAD_SIZE', '100MB')
    
    # CDN
    CDN_ENABLED = os.getenv('REGISTRY_CDN_ENABLED', 'true').lower() == 'true'
    CDN_ENDPOINTS = os.getenv('REGISTRY_CDN_ENDPOINTS', '').split(',')
```

## API Reference

### Authentication

All API endpoints require authentication unless specified otherwise.

```bash
# Get API token
curl -X POST https://registry.tusklang.org/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username": "user", "password": "password"}'

# Use token
curl -H "Authorization: Bearer <token>" \
  https://registry.tusklang.org/api/v1/packages
```

### Package Management

#### Upload Package

```bash
curl -X POST https://registry.tusklang.org/api/v1/packages \
  -H "Authorization: Bearer <token>" \
  -F "package=@my-package.tsk" \
  -F "metadata=@package.json"
```

#### Download Package

```bash
curl -H "Authorization: Bearer <token>" \
  https://registry.tusklang.org/api/v1/packages/my-package/download
```

#### Search Packages

```bash
curl -H "Authorization: Bearer <token>" \
  "https://registry.tusklang.org/api/v1/packages/search?q=web&limit=10"
```

### User Management

#### Create User

```bash
curl -X POST https://registry.tusklang.org/api/v1/users \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "newuser",
    "email": "user@example.com",
    "role": "publisher"
  }'
```

#### Update Permissions

```bash
curl -X PUT https://registry.tusklang.org/api/v1/users/user-id/permissions \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "permissions": ["read_packages", "publish_packages"]
  }'
```

## Security Guidelines

### Package Security

1. **Always verify package signatures** before installation
2. **Use HTTPS** for all registry communications
3. **Implement rate limiting** to prevent abuse
4. **Monitor for suspicious activity** and malware
5. **Keep signing keys secure** and rotate regularly

### Access Control

1. **Use least privilege principle** for user permissions
2. **Implement multi-factor authentication** for admin accounts
3. **Regularly audit user permissions** and access logs
4. **Use API tokens** instead of passwords for automation
5. **Monitor failed authentication attempts**

### Infrastructure Security

1. **Keep systems updated** with security patches
2. **Use firewall rules** to restrict access
3. **Encrypt data at rest** and in transit
4. **Implement backup encryption** and secure storage
5. **Monitor system logs** for security events

## Monitoring and Maintenance

### Health Checks

```bash
# Check registry health
curl https://registry.tusklang.org/health

# Check database connectivity
curl https://registry.tusklang.org/health/db

# Check storage status
curl https://registry.tusklang.org/health/storage
```

### Metrics

The registry provides comprehensive metrics via Prometheus:

```bash
# Get metrics
curl https://registry.tusklang.org/metrics
```

Key metrics include:
- Package upload/download rates
- Authentication success/failure rates
- Cache hit rates
- Response times
- Error rates

### Backup and Recovery

```bash
# Create backup
curl -X POST https://registry.tusklang.org/api/v1/backup \
  -H "Authorization: Bearer <admin-token>"

# List backups
curl -H "Authorization: Bearer <admin-token>" \
  https://registry.tusklang.org/api/v1/backup

# Restore backup
curl -X POST https://registry.tusklang.org/api/v1/backup/backup-id/restore \
  -H "Authorization: Bearer <admin-token>"
```

## Troubleshooting

### Common Issues

#### Package Upload Fails

1. Check file size limits
2. Verify package format
3. Check user permissions
4. Review validation errors

#### Download Issues

1. Verify package exists
2. Check user permissions
3. Verify signature
4. Check CDN status

#### Performance Issues

1. Check cache hit rates
2. Monitor server resources
3. Review load balancer status
4. Check database performance

### Logs

Registry logs are available at:
- Application logs: `/var/log/registry/app.log`
- Access logs: `/var/log/registry/access.log`
- Security logs: `/var/log/registry/security.log`
- Error logs: `/var/log/registry/error.log`

### Support

For support and issues:
- Documentation: https://docs.tusklang.org/registry
- Issues: https://github.com/tusklang/registry/issues
- Security: security@tusklang.org

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

This project is licensed under the MIT License - see [LICENSE](LICENSE) for details. 