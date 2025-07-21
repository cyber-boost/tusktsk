# TuskTsk SDK Docker Setup

This document provides comprehensive instructions for setting up and using Docker containers for the TuskTsk SDK implementations.

## Overview

The TuskTsk SDK provides Docker containers for all supported programming languages:
- Python
- JavaScript/Node.js
- Go
- Rust
- C#/.NET
- PHP
- Ruby

## Prerequisites

- Docker Engine 20.10+
- Docker Compose 2.0+
- At least 8GB RAM available for Docker
- 20GB free disk space

## Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/cyber-boost/tusktsk.git
cd tusktsk
```

### 2. Start All Services

```bash
docker-compose up -d
```

This will start:
- All 7 language SDK containers
- Database services (MySQL, Redis, MongoDB, Elasticsearch)
- Message queue services (NATS, RabbitMQ)
- Monitoring services (Prometheus, Grafana)

### 3. Verify Installation

```bash
# Check all containers are running
docker-compose ps

# Check container health
docker-compose ps --format "table {{.Name}}\t{{.Status}}\t{{.Ports}}"
```

## Individual Language Containers

### Python SDK

```bash
# Build Python container
docker build -f Dockerfile.python -t tusktsk-python .

# Run Python container
docker run -d --name tusktsk-python tusktsk-python

# Execute commands in Python container
docker exec -it tusktsk-python python -c "import tusktsk; print(tusktsk.__version__)"
```

### JavaScript SDK

```bash
# Build JavaScript container
docker build -f Dockerfile.nodejs -t tusktsk-javascript .

# Run JavaScript container
docker run -d --name tusktsk-javascript tusktsk-javascript

# Execute commands in JavaScript container
docker exec -it tusktsk-javascript node -e "console.log('TuskTsk JavaScript SDK ready')"
```

### Go SDK

```bash
# Build Go container
docker build -f Dockerfile.go -t tusktsk-go .

# Run Go container
docker run -d --name tusktsk-go tusktsk-go

# Execute commands in Go container
docker exec -it tusktsk-go ./tusktsk-go version
```

### Rust SDK

```bash
# Build Rust container
docker build -f Dockerfile.rust -t tusktsk-rust .

# Run Rust container
docker run -d --name tusktsk-rust tusktsk-rust

# Execute commands in Rust container
docker exec -it tusktsk-rust ./tusk-rust --version
```

### C# SDK

```bash
# Build C# container
docker build -f Dockerfile.dotnet -t tusktsk-dotnet .

# Run C# container
docker run -d --name tusktsk-dotnet tusktsk-dotnet

# Execute commands in C# container
docker exec -it tusktsk-dotnet dotnet TuskTsk.dll --version
```

### PHP SDK

```bash
# Build PHP container
docker build -f Dockerfile.php -t tusktsk-php .

# Run PHP container
docker run -d --name tusktsk-php tusktsk-php

# Execute commands in PHP container
docker exec -it tusktsk-php php -r "echo 'TuskTsk PHP SDK ready';"
```

### Ruby SDK

```bash
# Build Ruby container
docker build -f Dockerfile.ruby -t tusktsk-ruby .

# Run Ruby container
docker run -d --name tusktsk-ruby tusktsk-ruby

# Execute commands in Ruby container
docker exec -it tusktsk-ruby ruby -e "puts 'TuskTsk Ruby SDK ready'"
```

## Development Environment

### Using Docker Compose for Development

```bash
# Start only specific services
docker-compose up -d tusktsk-python mysql redis

# Start with volume mounts for development
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# View logs
docker-compose logs -f tusktsk-python

# Execute commands in running containers
docker-compose exec tusktsk-python python -c "import tusktsk"
```

### Development with Hot Reload

Create a `docker-compose.dev.yml` file:

```yaml
version: '3.8'

services:
  tusktsk-python:
    volumes:
      - ./sdk/python:/app
      - python_cache:/root/.cache/pip
    environment:
      - PYTHONPATH=/app
      - FLASK_ENV=development
    command: ["python", "-m", "flask", "run", "--host=0.0.0.0", "--port=8000"]

  tusktsk-javascript:
    volumes:
      - ./sdk/javascript:/app
      - node_modules:/app/node_modules
    environment:
      - NODE_ENV=development
    command: ["npm", "run", "dev"]
```

## Database Services

### MySQL

```bash
# Connect to MySQL
docker-compose exec mysql mysql -u tusktsk -p tusktsk

# Default credentials:
# Username: tusktsk
# Password: tusktsk_pass
# Database: tusktsk
# Port: 3306
```

### Redis

```bash
# Connect to Redis
docker-compose exec redis redis-cli

# Default port: 6379
```

### MongoDB

```bash
# Connect to MongoDB
docker-compose exec mongodb mongosh -u tusktsk -p tusktsk_pass

# Default credentials:
# Username: tusktsk
# Password: tusktsk_pass
# Database: tusktsk
# Port: 27017
```

### Elasticsearch

```bash
# Check Elasticsearch health
curl http://localhost:9200/_cluster/health

# Default port: 9200
```

## Message Queue Services

### NATS

```bash
# Connect to NATS
docker-compose exec nats nats-sub "test"

# Default ports:
# Client: 4222
# HTTP: 8222
```

### RabbitMQ

```bash
# Access RabbitMQ Management UI
# URL: http://localhost:15672
# Username: tusktsk
# Password: tusktsk_pass

# Default ports:
# AMQP: 5672
# Management: 15672
```

## Monitoring and Observability

### Prometheus

```bash
# Access Prometheus UI
# URL: http://localhost:9090

# Check targets
curl http://localhost:9090/api/v1/targets
```

### Grafana

```bash
# Access Grafana UI
# URL: http://localhost:3000
# Username: admin
# Password: tusktsk_admin
```

## Health Checks

All containers include health checks. Monitor them with:

```bash
# Check health status
docker-compose ps

# View health check logs
docker inspect --format='{{json .State.Health}}' tusktsk-python | jq
```

## Troubleshooting

### Common Issues

1. **Port Conflicts**
   ```bash
   # Check what's using a port
   sudo netstat -tulpn | grep :3306
   
   # Change ports in docker-compose.yml
   ports:
     - "3307:3306"  # Use 3307 instead of 3306
   ```

2. **Memory Issues**
   ```bash
   # Increase Docker memory limit
   # In Docker Desktop: Settings > Resources > Memory
   ```

3. **Permission Issues**
   ```bash
   # Fix file permissions
   sudo chown -R $USER:$USER .
   ```

4. **Container Won't Start**
   ```bash
   # Check logs
   docker-compose logs tusktsk-python
   
   # Remove and recreate
   docker-compose down
   docker-compose up -d
   ```

### Debugging

```bash
# Enter a running container
docker-compose exec tusktsk-python bash

# View container details
docker inspect tusktsk-python

# Check resource usage
docker stats

# View network configuration
docker network ls
docker network inspect tusktsk_tusktsk-network
```

## Production Deployment

### Building Production Images

```bash
# Build all production images
docker-compose -f docker-compose.yml -f docker-compose.prod.yml build

# Push to registry
docker-compose -f docker-compose.yml -f docker-compose.prod.yml push
```

### Production Configuration

Create `docker-compose.prod.yml`:

```yaml
version: '3.8'

services:
  tusktsk-python:
    image: ghcr.io/cyber-boost/tusktsk-python:latest
    restart: unless-stopped
    environment:
      - NODE_ENV=production
    deploy:
      resources:
        limits:
          memory: 512M
        reservations:
          memory: 256M
```

## Security Considerations

1. **Non-root Users**: All containers run as non-root users
2. **Secrets Management**: Use Docker secrets for sensitive data
3. **Network Isolation**: Containers are isolated in a custom network
4. **Health Checks**: All containers have health checks enabled
5. **Resource Limits**: Set appropriate resource limits in production

## Performance Optimization

1. **Multi-stage Builds**: Used for Go and Rust containers
2. **Layer Caching**: Optimized Dockerfile layer ordering
3. **Volume Mounts**: Use named volumes for persistent data
4. **Resource Limits**: Configure appropriate memory and CPU limits

## Support

For issues and questions:
- GitHub Issues: https://github.com/cyber-boost/tusktsk/issues
- Documentation: https://tuskt.sk/docs
- Community: https://tuskt.sk/community 