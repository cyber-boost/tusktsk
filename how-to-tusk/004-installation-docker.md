# Installing TuskLang with Docker

Docker provides a consistent, isolated environment for running TuskLang across any platform.

## Prerequisites

- Docker 20.10+ installed
- Docker Compose 2.0+ (optional, for multi-container setups)
- Basic understanding of Docker concepts

## Official Docker Image

### Quick Start

Pull and run the official TuskLang image:

```bash
# Pull the latest image
docker pull tusklang/tusk:latest

# Run interactive shell
docker run -it tusklang/tusk:latest

# Run a specific .tsk file
docker run -v $(pwd):/app tusklang/tusk:latest tusk run /app/config.tsk
```

### Available Tags

- `latest` - Latest stable release
- `1.0.0` - Specific version
- `alpine` - Minimal Alpine Linux base
- `ubuntu` - Ubuntu-based image
- `dev` - Development builds (unstable)

## Using TuskLang in Docker

### Basic Dockerfile

Create a `Dockerfile` for your TuskLang application:

```dockerfile
# Use official TuskLang base image
FROM tusklang/tusk:latest

# Set working directory
WORKDIR /app

# Copy your .tsk files
COPY . /app

# Run your application
CMD ["tusk", "run", "app.tsk"]
```

### Multi-Stage Build

For production deployments with minimal image size:

```dockerfile
# Build stage
FROM tusklang/tusk:latest AS builder
WORKDIR /build
COPY . .
RUN tusk compile app.tsk -o app

# Runtime stage
FROM alpine:latest
RUN apk add --no-cache ca-certificates
COPY --from=builder /build/app /usr/local/bin/
CMD ["app"]
```

### Alpine-Based Image

For the smallest possible image:

```dockerfile
FROM tusklang/tusk:alpine

# Install additional dependencies if needed
RUN apk add --no-cache curl jq

WORKDIR /app
COPY config.tsk .

# Health check
HEALTHCHECK --interval=30s --timeout=3s \
  CMD tusk check config.tsk || exit 1

CMD ["tusk", "serve", "config.tsk"]
```

## Docker Compose Integration

### Basic Compose File

Create `docker-compose.yml`:

```yaml
version: '3.8'

services:
  tuskapp:
    image: tusklang/tusk:latest
    volumes:
      - ./app:/app
    environment:
      - TUSK_ENV=production
      - DB_HOST=postgres
    command: tusk run /app/server.tsk
    ports:
      - "8080:8080"
    depends_on:
      - postgres

  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: tuskapp
      POSTGRES_USER: tusk
      POSTGRES_PASSWORD: secret
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

### Development Compose

For development with hot reload:

```yaml
version: '3.8'

services:
  tuskdev:
    image: tusklang/tusk:latest
    volumes:
      - .:/app
      - tusk_cache:/tmp/tusk
    environment:
      - TUSK_ENV=development
      - TUSK_HOT_RELOAD=true
    command: tusk dev /app/server.tsk
    ports:
      - "3000:3000"
    networks:
      - tusknet

  redis:
    image: redis:alpine
    networks:
      - tusknet

networks:
  tusknet:
    driver: bridge

volumes:
  tusk_cache:
```

## Volume Management

### Persistent Configuration

```bash
# Create named volume for configuration
docker volume create tusk-config

# Run with persistent config
docker run -v tusk-config:/etc/tusklang tusklang/tusk:latest
```

### Development Workflow

```bash
# Mount current directory
docker run -it -v $(pwd):/workspace -w /workspace tusklang/tusk:latest bash

# Inside container
tusk init
tusk run app.tsk
```

## Environment Variables

### Pass Environment Variables

```bash
# Single variable
docker run -e API_KEY=secret tusklang/tusk:latest

# Multiple variables
docker run \
  -e DB_HOST=localhost \
  -e DB_PORT=5432 \
  -e APP_ENV=production \
  tusklang/tusk:latest

# From .env file
docker run --env-file .env tusklang/tusk:latest
```

### TuskLang-Specific Variables

```bash
# Configure TuskLang runtime
docker run \
  -e TUSK_LOG_LEVEL=debug \
  -e TUSK_CACHE_DIR=/tmp/tusk \
  -e TUSK_PLUGINS_DIR=/opt/tusk/plugins \
  tusklang/tusk:latest
```

## Networking

### Container Communication

```yaml
# docker-compose.yml
services:
  api:
    image: tusklang/tusk:latest
    environment:
      - BACKEND_URL=http://backend:8080
    networks:
      - app-network

  backend:
    image: tusklang/tusk:latest
    networks:
      - app-network

networks:
  app-network:
    driver: bridge
```

### Host Networking

```bash
# Use host network (Linux only)
docker run --network host tusklang/tusk:latest
```

## Building Custom Images

### Extended Functionality

```dockerfile
FROM tusklang/tusk:latest

# Install system dependencies
RUN apt-get update && apt-get install -y \
    curl \
    git \
    && rm -rf /var/lib/apt/lists/*

# Install TuskLang plugins
RUN tusk plugin install database http cache

# Copy custom operators
COPY operators/ /usr/local/lib/tusk/operators/

# Set up application
WORKDIR /app
COPY . .

# Compile if needed
RUN tusk compile server.tsk

EXPOSE 8080
CMD ["tusk", "serve", "server.tsk"]
```

### Security Hardening

```dockerfile
FROM tusklang/tusk:alpine

# Create non-root user
RUN addgroup -g 1001 -S tusk && \
    adduser -u 1001 -S tusk -G tusk

# Copy files with correct ownership
COPY --chown=tusk:tusk . /app

# Switch to non-root user
USER tusk

WORKDIR /app
CMD ["tusk", "run", "app.tsk"]
```

## Debugging in Docker

### Interactive Debugging

```bash
# Start with debug mode
docker run -it -e TUSK_DEBUG=true tusklang/tusk:latest

# Attach to running container
docker exec -it <container_id> tusk repl
```

### Logging

```yaml
# docker-compose.yml with logging
services:
  app:
    image: tusklang/tusk:latest
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
    environment:
      - TUSK_LOG_FORMAT=json
      - TUSK_LOG_LEVEL=info
```

## Performance Optimization

### Caching Layers

```dockerfile
FROM tusklang/tusk:latest

# Cache dependencies first
COPY requirements.tsk .
RUN tusk install

# Then copy application code
COPY . .
RUN tusk build
```

### Resource Limits

```yaml
# docker-compose.yml
services:
  tuskapp:
    image: tusklang/tusk:latest
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 1G
        reservations:
          cpus: '0.5'
          memory: 256M
```

## CI/CD Integration

### GitHub Actions

```yaml
# .github/workflows/docker.yml
name: Docker Build

on: [push]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      
      - name: Build Docker image
        run: docker build -t myapp:${{ github.sha }} .
      
      - name: Test with TuskLang
        run: docker run myapp:${{ github.sha }} tusk test
```

### GitLab CI

```yaml
# .gitlab-ci.yml
test:
  image: tusklang/tusk:latest
  script:
    - tusk test
    - tusk lint

build:
  image: docker:latest
  services:
    - docker:dind
  script:
    - docker build -t $CI_REGISTRY_IMAGE:$CI_COMMIT_SHA .
    - docker push $CI_REGISTRY_IMAGE:$CI_COMMIT_SHA
```

## Troubleshooting

### Common Issues

**Permission Denied:**
```bash
# Fix file permissions
docker run --user $(id -u):$(id -g) -v $(pwd):/app tusklang/tusk:latest
```

**Cannot Find File:**
```bash
# Use absolute paths in container
docker run -v $(pwd):/data tusklang/tusk:latest tusk run /data/app.tsk
```

**Memory Issues:**
```bash
# Increase memory limit
docker run -m 2g tusklang/tusk:latest
```

## Next Steps

- Learn Docker-specific TuskLang patterns
- Set up development environment: [009-file-structure.md](009-file-structure.md)
- Deploy to Kubernetes with TuskLang configs