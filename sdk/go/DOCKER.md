# Docker Setup for TuskLang Go SDK

This document provides comprehensive instructions for using Docker with the TuskLang Go SDK.

## Quick Start

### Build and Run the Go SDK Container

```bash
# Build the Docker image
docker build -t tusktsk-go-sdk .

# Run the container
docker run -it tusktsk-go-sdk --help
```

### Using Docker Compose (Recommended)

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f tusktsk-go

# Stop all services
docker-compose down
```

## Docker Configuration

### Dockerfile

The `Dockerfile` is optimized for production use with the following features:

- **Multi-stage build**: Reduces final image size
- **Security**: Non-root user execution
- **Health checks**: Automatic container health monitoring
- **Minimal base image**: Alpine Linux for smaller footprint

### Key Features

1. **Go 1.23**: Latest stable Go version
2. **CGO disabled**: Static linking for better portability
3. **Security hardening**: Non-root user, minimal permissions
4. **Health monitoring**: Built-in health checks
5. **Multi-platform**: Supports AMD64 and ARM64 architectures

## Docker Compose Services

The `docker-compose.yml` includes the following services:

### Core Services

- **tusktsk-go**: Main Go SDK application
- **mysql**: MySQL 8.0 database
- **redis**: Redis 7 cache
- **mongodb**: MongoDB 7 document store

### Additional Services

- **elasticsearch**: Search and analytics
- **nats**: High-performance messaging
- **rabbitmq**: Message queue with management UI

### Service Configuration

#### Environment Variables

```yaml
environment:
  - TUSKTSK_ENV=development
  - TUSKTSK_LOG_LEVEL=debug
```

#### Port Mappings

- **tusktsk-go**: 8080
- **mysql**: 3306
- **redis**: 6379
- **mongodb**: 27017
- **elasticsearch**: 9200
- **nats**: 4222 (client), 8222 (HTTP)
- **rabbitmq**: 5672 (AMQP), 15672 (management)

#### Volumes

- **mysql_data**: Persistent MySQL data
- **redis_data**: Persistent Redis data
- **mongodb_data**: Persistent MongoDB data
- **elasticsearch_data**: Persistent Elasticsearch data
- **rabbitmq_data**: Persistent RabbitMQ data

## Development Workflow

### Local Development

1. **Start services**:
   ```bash
   docker-compose up -d
   ```

2. **Run tests**:
   ```bash
   docker-compose exec tusktsk-go go test ./...
   ```

3. **Build binary**:
   ```bash
   docker-compose exec tusktsk-go go build -o tusktsk .
   ```

4. **Access services**:
   - MySQL: `localhost:3306`
   - Redis: `localhost:6379`
   - MongoDB: `localhost:27017`
   - Elasticsearch: `localhost:9200`
   - NATS: `localhost:4222`
   - RabbitMQ Management: `http://localhost:15672`

### Production Deployment

1. **Build production image**:
   ```bash
   docker build -t tusktsk-go-sdk:latest .
   ```

2. **Run with production config**:
   ```bash
   docker run -d \
     --name tusktsk-go-sdk \
     -p 8080:8080 \
     -e TUSKTSK_ENV=production \
     -e TUSKTSK_LOG_LEVEL=info \
     tusktsk-go-sdk:latest
   ```

## Health Checks

All services include health checks:

```bash
# Check container health
docker ps

# View health check logs
docker inspect tusktsk-go-sdk | grep -A 10 Health
```

## Troubleshooting

### Common Issues

1. **Port conflicts**:
   ```bash
   # Check port usage
   netstat -tulpn | grep :8080
   
   # Use different ports in docker-compose.yml
   ports:
     - "8081:8080"
   ```

2. **Permission issues**:
   ```bash
   # Fix volume permissions
   sudo chown -R 1001:1001 ./data
   ```

3. **Memory issues**:
   ```bash
   # Increase Docker memory limit
   # In Docker Desktop: Settings > Resources > Memory
   ```

4. **Network issues**:
   ```bash
   # Check network connectivity
   docker network ls
   docker network inspect tusktsk_tusktsk-network
   ```

### Logs and Debugging

```bash
# View all logs
docker-compose logs

# View specific service logs
docker-compose logs tusktsk-go

# Follow logs in real-time
docker-compose logs -f tusktsk-go

# Execute commands in container
docker-compose exec tusktsk-go sh
```

## Security Considerations

### Best Practices

1. **Use non-root user**: Container runs as `tusktsk` user (UID 1001)
2. **Minimal base image**: Alpine Linux reduces attack surface
3. **No secrets in images**: Use environment variables or secrets
4. **Regular updates**: Keep base images updated
5. **Vulnerability scanning**: Use Trivy or similar tools

### Security Scanning

```bash
# Scan for vulnerabilities
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  aquasec/trivy image tusktsk-go-sdk:latest

# Scan filesystem
docker run --rm -v $(pwd):/app aquasec/trivy fs /app
```

## Performance Optimization

### Build Optimization

1. **Use .dockerignore**: Excludes unnecessary files
2. **Layer caching**: Optimize Dockerfile layer order
3. **Multi-stage builds**: Reduce final image size
4. **Build cache**: Use Docker BuildKit cache

### Runtime Optimization

1. **Resource limits**: Set appropriate CPU/memory limits
2. **Volume mounts**: Use named volumes for persistence
3. **Network optimization**: Use host networking when appropriate
4. **Health checks**: Monitor container health

## Monitoring and Observability

### Metrics Collection

```bash
# Enable Docker metrics
docker run -d \
  --name cadvisor \
  --volume=/:/rootfs:ro \
  --volume=/var/run:/var/run:ro \
  --volume=/sys:/sys:ro \
  --volume=/var/lib/docker/:/var/lib/docker:ro \
  --publish=8080:8080 \
  gcr.io/cadvisor/cadvisor
```

### Logging

```bash
# Configure log drivers
docker run -d \
  --log-driver=json-file \
  --log-opt max-size=10m \
  --log-opt max-file=3 \
  tusktsk-go-sdk:latest
```

## Integration with CI/CD

### GitHub Actions

The repository includes GitHub Actions workflows for:

- **Build and test**: Automated testing on multiple platforms
- **Security scanning**: Vulnerability assessment
- **Docker publishing**: Automated image publishing
- **Release management**: Automated releases

### Local CI/CD

```bash
# Run tests locally
docker-compose -f docker-compose.test.yml up --abort-on-container-exit

# Build and push
docker build -t ghcr.io/your-org/tusktsk-go-sdk:latest .
docker push ghcr.io/your-org/tusktsk-go-sdk:latest
```

## Support

For issues and questions:

1. Check the troubleshooting section above
2. Review GitHub Issues
3. Check the main README.md
4. Contact the development team

## Contributing

When contributing to Docker configuration:

1. Test changes locally first
2. Update documentation
3. Follow security best practices
4. Include appropriate tests
5. Update version tags appropriately 