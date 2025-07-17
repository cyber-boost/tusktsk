# 🚀 Deployment Strategies - Python

**"We don't bow to any king" - Deployment Edition**

TuskLang provides flexible deployment strategies for various environments, from development to production.

## 🐳 Docker Deployment

### Basic Docker Setup

```dockerfile
# Dockerfile for TuskLang Python application
FROM python:3.11-slim

# Set working directory
WORKDIR /app

# Install system dependencies
RUN apt-get update && apt-get install -y \
    gcc \
    libpq-dev \
    && rm -rf /var/lib/apt/lists/*

# Install TuskLang
RUN pip install tusklang

# Copy requirements and install Python dependencies
COPY requirements.txt .
RUN pip install -r requirements.txt

# Copy application code
COPY . .

# Copy TSK configuration
COPY app.tsk /app/

# Create non-root user
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

# Expose port
EXPOSE 8000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD python -c "import requests; requests.get('http://localhost:8000/health')"

# Run application
CMD ["python", "app.py"]
```

### Multi-stage Docker Build

```dockerfile
# Multi-stage Dockerfile for optimized production build
FROM python:3.11-slim as builder

# Install build dependencies
RUN apt-get update && apt-get install -y \
    gcc \
    libpq-dev \
    && rm -rf /var/lib/apt/lists/*

# Create virtual environment
RUN python -m venv /opt/venv
ENV PATH="/opt/venv/bin:$PATH"

# Install dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Production stage
FROM python:3.11-slim

# Install runtime dependencies
RUN apt-get update && apt-get install -y \
    libpq5 \
    && rm -rf /var/lib/apt/lists/*

# Copy virtual environment
COPY --from=builder /opt/venv /opt/venv
ENV PATH="/opt/venv/bin:$PATH"

# Set working directory
WORKDIR /app

# Copy application
COPY --chown=1000:1000 . .

# Copy TSK configuration
COPY --chown=1000:1000 app.tsk /app/

# Create non-root user
RUN useradd -m -u 1000 appuser
USER appuser

# Expose port
EXPOSE 8000

# Run application
CMD ["python", "app.py"]
```

### Docker Compose Setup

```yaml
# docker-compose.yml
version: '3.8'

services:
  app:
    build: .
    ports:
      - "8000:8000"
    environment:
      - DATABASE_URL=postgresql://postgres:password@db:5432/myapp
      - REDIS_URL=redis://redis:6379/0
      - APP_ENV=production
    depends_on:
      - db
      - redis
    volumes:
      - ./logs:/app/logs
    restart: unless-stopped

  db:
    image: postgres:15
    environment:
      - POSTGRES_DB=myapp
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=password
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/nginx/ssl
    depends_on:
      - app

volumes:
  postgres_data:
  redis_data:
```

## ☸️ Kubernetes Deployment

### Basic Kubernetes Setup

```yaml
# k8s-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tusk-app
  labels:
    app: tusk-app
spec:
  replicas: 3
  selector:
    matchLabels:
      app: tusk-app
  template:
    metadata:
      labels:
        app: tusk-app
    spec:
      containers:
      - name: app
        image: tusk-app:latest
        ports:
        - containerPort: 8000
        env:
        - name: DATABASE_URL
          valueFrom:
            secretKeyRef:
              name: app-secrets
              key: database-url
        - name: REDIS_URL
          valueFrom:
            secretKeyRef:
              name: app-secrets
              key: redis-url
        - name: APP_ENV
          value: "production"
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 8000
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 8000
          initialDelaySeconds: 5
          periodSeconds: 5
        volumeMounts:
        - name: config
          mountPath: /app/config
      volumes:
      - name: config
        configMap:
          name: app-config

---
apiVersion: v1
kind: Service
metadata:
  name: tusk-service
spec:
  selector:
    app: tusk-app
  ports:
  - port: 80
    targetPort: 8000
  type: LoadBalancer

---
apiVersion: v1
kind: ConfigMap
metadata:
  name: app-config
data:
  app.tsk: |
    $app_name: "TuskApp"
    $version: "1.0.0"
    
    [server]
    host: "0.0.0.0"
    port: 8000
    
    [database]
    url: @env("DATABASE_URL")
    
    [cache]
    redis_url: @env("REDIS_URL")

---
apiVersion: v1
kind: Secret
metadata:
  name: app-secrets
type: Opaque
data:
  database-url: cG9zdGdyZXNxbDovL3Bvc3RncmVzOnBhc3N3b3JkQGRiOjU0MzIvbXlhcHA=
  redis-url: cmVkaXM6Ly9yZWRpczo2Mzc5LzA=
```

### Kubernetes with Helm

```yaml
# values.yaml
replicaCount: 3

image:
  repository: tusk-app
  tag: latest
  pullPolicy: IfNotPresent

service:
  type: LoadBalancer
  port: 80

resources:
  requests:
    memory: 256Mi
    cpu: 250m
  limits:
    memory: 512Mi
    cpu: 500m

env:
  APP_ENV: production
  DATABASE_URL: postgresql://postgres:password@db:5432/myapp
  REDIS_URL: redis://redis:6379/0

config:
  app_name: "TuskApp"
  version: "1.0.0"

secrets:
  database-url: "postgresql://postgres:password@db:5432/myapp"
  redis-url: "redis://redis:6379/0"
```

```yaml
# templates/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: {{ include "tusk.fullname" . }}
  labels:
    {{- include "tusk.labels" . | nindent 4 }}
spec:
  replicas: {{ .Values.replicaCount }}
  selector:
    matchLabels:
      {{- include "tusk.selectorLabels" . | nindent 6 }}
  template:
    metadata:
      labels:
        {{- include "tusk.selectorLabels" . | nindent 8 }}
    spec:
      containers:
      - name: {{ .Chart.Name }}
        image: "{{ .Values.image.repository }}:{{ .Values.image.tag }}"
        imagePullPolicy: {{ .Values.image.pullPolicy }}
        ports:
        - containerPort: 8000
        env:
        {{- range $key, $value := .Values.env }}
        - name: {{ $key }}
          value: {{ $value | quote }}
        {{- end }}
        {{- range $key, $value := .Values.secrets }}
        - name: {{ $key }}
          valueFrom:
            secretKeyRef:
              name: {{ include "tusk.fullname" $ }}-secrets
              key: {{ $key }}
        {{- end }}
        resources:
          {{- toYaml .Values.resources | nindent 10 }}
```

## 🚀 CI/CD Pipeline

### GitHub Actions Pipeline

```yaml
# .github/workflows/deploy.yml
name: Deploy TuskLang App

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    
    services:
      postgres:
        image: postgres:15
        env:
          POSTGRES_PASSWORD: postgres
          POSTGRES_DB: test_db
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 5432:5432
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Set up Python
      uses: actions/setup-python@v4
      with:
        python-version: '3.11'
    
    - name: Install dependencies
      run: |
        python -m pip install --upgrade pip
        pip install -r requirements.txt
        pip install tusklang
    
    - name: Run tests
      env:
        DATABASE_URL: postgresql://postgres:postgres@localhost:5432/test_db
      run: |
        python -m pytest tests/
    
    - name: Run TuskLang validation
      run: |
        tsk validate app.tsk
    
    - name: Build Docker image
      run: |
        docker build -t tusk-app:${{ github.sha }} .

  deploy:
    needs: test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Configure AWS credentials
      uses: aws-actions/configure-aws-credentials@v2
      with:
        aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
        aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
        aws-region: us-west-2
    
    - name: Login to Amazon ECR
      id: login-ecr
      uses: aws-actions/amazon-ecr-login@v1
    
    - name: Build and push Docker image
      env:
        ECR_REGISTRY: ${{ steps.login-ecr.outputs.registry }}
        ECR_REPOSITORY: tusk-app
        IMAGE_TAG: ${{ github.sha }}
      run: |
        docker build -t $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG .
        docker push $ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG
    
    - name: Deploy to EKS
      run: |
        aws eks update-kubeconfig --name tusk-cluster --region us-west-2
        kubectl set image deployment/tusk-app app=$ECR_REGISTRY/$ECR_REPOSITORY:$IMAGE_TAG
```

### GitLab CI Pipeline

```yaml
# .gitlab-ci.yml
stages:
  - test
  - build
  - deploy

variables:
  DOCKER_DRIVER: overlay2
  DOCKER_TLS_CERTDIR: "/certs"

test:
  stage: test
  image: python:3.11
  services:
    - postgres:15
  variables:
    POSTGRES_DB: test_db
    POSTGRES_USER: postgres
    POSTGRES_PASSWORD: postgres
    DATABASE_URL: postgresql://postgres:postgres@postgres:5432/test_db
  before_script:
    - pip install -r requirements.txt
    - pip install tusklang
  script:
    - python -m pytest tests/
    - tsk validate app.tsk
  coverage: '/TOTAL.*\s+(\d+%)$/'

build:
  stage: build
  image: docker:latest
  services:
    - docker:dind
  before_script:
    - docker login -u $CI_REGISTRY_USER -p $CI_REGISTRY_PASSWORD $CI_REGISTRY
  script:
    - docker build -t $CI_REGISTRY_IMAGE:$CI_COMMIT_SHA .
    - docker push $CI_REGISTRY_IMAGE:$CI_COMMIT_SHA
    - |
      if [ "$CI_COMMIT_BRANCH" = "$CI_DEFAULT_BRANCH" ]; then
        docker tag $CI_REGISTRY_IMAGE:$CI_COMMIT_SHA $CI_REGISTRY_IMAGE:latest
        docker push $CI_REGISTRY_IMAGE:latest
      fi
  only:
    - main

deploy:
  stage: deploy
  image: alpine:latest
  before_script:
    - apk add --no-cache curl
    - curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
    - chmod +x kubectl
    - mv kubectl /usr/local/bin/
  script:
    - kubectl config set-cluster k8s --server="$KUBE_URL" --insecure-skip-tls-verify=true
    - kubectl config set-credentials admin --token="$KUBE_TOKEN"
    - kubectl config set-context default --cluster=k8s --user=admin
    - kubectl config use-context default
    - kubectl set image deployment/tusk-app app=$CI_REGISTRY_IMAGE:$CI_COMMIT_SHA
  only:
    - main
```

## ☁️ Cloud Deployment

### AWS ECS Deployment

```yaml
# task-definition.json
{
  "family": "tusk-app",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "256",
  "memory": "512",
  "executionRoleArn": "arn:aws:iam::123456789012:role/ecsTaskExecutionRole",
  "taskRoleArn": "arn:aws:iam::123456789012:role/ecsTaskRole",
  "containerDefinitions": [
    {
      "name": "tusk-app",
      "image": "123456789012.dkr.ecr.us-west-2.amazonaws.com/tusk-app:latest",
      "portMappings": [
        {
          "containerPort": 8000,
          "protocol": "tcp"
        }
      ],
      "environment": [
        {
          "name": "APP_ENV",
          "value": "production"
        },
        {
          "name": "DATABASE_URL",
          "value": "postgresql://user:pass@db.cluster.amazonaws.com:5432/myapp"
        }
      ],
      "secrets": [
        {
          "name": "SECRET_KEY",
          "valueFrom": "arn:aws:secretsmanager:us-west-2:123456789012:secret:app-secret-key"
        }
      ],
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/tusk-app",
          "awslogs-region": "us-west-2",
          "awslogs-stream-prefix": "ecs"
        }
      },
      "healthCheck": {
        "command": ["CMD-SHELL", "python -c \"import requests; requests.get('http://localhost:8000/health')\""],
        "interval": 30,
        "timeout": 5,
        "retries": 3,
        "startPeriod": 60
      }
    }
  ]
}
```

### Google Cloud Run

```yaml
# cloudbuild.yaml
steps:
  - name: 'gcr.io/cloud-builders/docker'
    args: ['build', '-t', 'gcr.io/$PROJECT_ID/tusk-app:$COMMIT_SHA', '.']
  
  - name: 'gcr.io/cloud-builders/docker'
    args: ['push', 'gcr.io/$PROJECT_ID/tusk-app:$COMMIT_SHA']
  
  - name: 'gcr.io/cloud-builders/gcloud'
    args:
      - 'run'
      - 'deploy'
      - 'tusk-app'
      - '--image'
      - 'gcr.io/$PROJECT_ID/tusk-app:$COMMIT_SHA'
      - '--region'
      - 'us-central1'
      - '--platform'
      - 'managed'
      - '--allow-unauthenticated'
      - '--set-env-vars'
      - 'APP_ENV=production,DATABASE_URL=postgresql://user:pass@/db?host=/cloudsql/$PROJECT_ID:us-central1:db'

images:
  - 'gcr.io/$PROJECT_ID/tusk-app:$COMMIT_SHA'
```

## 🔧 Environment Configuration

### Production Configuration

```ini
# production.tsk
$environment: "production"
$app_name: "TuskApp"
$version: "1.0.0"

[server]
host: "0.0.0.0"
port: @env("PORT", 8000)
workers: @env("WORKERS", 4)
timeout: @env("TIMEOUT", 30)

[database]
url: @env("DATABASE_URL")
pool_size: @env("DB_POOL_SIZE", 20)
max_overflow: @env("DB_MAX_OVERFLOW", 30)
ssl_mode: @env("DB_SSL_MODE", "require")

[cache]
redis_url: @env("REDIS_URL")
ttl: @env("CACHE_TTL", 300)

[security]
secret_key: @env("SECRET_KEY")
jwt_secret: @env("JWT_SECRET")
bcrypt_rounds: @env("BCRYPT_ROUNDS", 12)

[logging]
level: @env("LOG_LEVEL", "info")
format: @env("LOG_FORMAT", "json")
file: @env("LOG_FILE", "/app/logs/app.log")

[monitoring]
metrics_enabled: @env("METRICS_ENABLED", true)
health_check_interval: @env("HEALTH_CHECK_INTERVAL", 30)
```

### Staging Configuration

```ini
# staging.tsk
$environment: "staging"
$app_name: "TuskApp"
$version: "1.0.0"

[server]
host: "0.0.0.0"
port: @env("PORT", 8000)
workers: @env("WORKERS", 2)
timeout: @env("TIMEOUT", 30)

[database]
url: @env("DATABASE_URL")
pool_size: @env("DB_POOL_SIZE", 10)
max_overflow: @env("DB_MAX_OVERFLOW", 15)
ssl_mode: @env("DB_SSL_MODE", "prefer")

[cache]
redis_url: @env("REDIS_URL")
ttl: @env("CACHE_TTL", 300)

[security]
secret_key: @env("SECRET_KEY")
jwt_secret: @env("JWT_SECRET")
bcrypt_rounds: @env("BCRYPT_ROUNDS", 10)

[logging]
level: @env("LOG_LEVEL", "debug")
format: @env("LOG_FORMAT", "text")
file: @env("LOG_FILE", "/app/logs/app.log")

[monitoring]
metrics_enabled: @env("METRICS_ENABLED", true)
health_check_interval: @env("HEALTH_CHECK_INTERVAL", 30)
```

## 🚀 Deployment Scripts

### Deployment Automation

```bash
#!/bin/bash
# deploy.sh

set -e

# Configuration
APP_NAME="tusk-app"
DOCKER_REGISTRY="your-registry.com"
VERSION=$(git rev-parse --short HEAD)
ENVIRONMENT=${1:-production}

echo "Deploying $APP_NAME version $VERSION to $ENVIRONMENT"

# Build Docker image
echo "Building Docker image..."
docker build -t $DOCKER_REGISTRY/$APP_NAME:$VERSION .
docker tag $DOCKER_REGISTRY/$APP_NAME:$VERSION $DOCKER_REGISTRY/$APP_NAME:latest

# Push to registry
echo "Pushing to registry..."
docker push $DOCKER_REGISTRY/$APP_NAME:$VERSION
docker push $DOCKER_REGISTRY/$APP_NAME:latest

# Deploy to Kubernetes
echo "Deploying to Kubernetes..."
kubectl set image deployment/$APP_NAME app=$DOCKER_REGISTRY/$APP_NAME:$VERSION

# Wait for deployment
echo "Waiting for deployment to complete..."
kubectl rollout status deployment/$APP_NAME

# Health check
echo "Performing health check..."
sleep 10
curl -f http://localhost/health || exit 1

echo "Deployment completed successfully!"
```

### Rollback Script

```bash
#!/bin/bash
# rollback.sh

set -e

APP_NAME="tusk-app"
DOCKER_REGISTRY="your-registry.com"

echo "Rolling back $APP_NAME..."

# Get previous version
PREVIOUS_VERSION=$(kubectl get deployment $APP_NAME -o jsonpath='{.spec.template.spec.containers[0].image}' | cut -d: -f2)

if [ "$PREVIOUS_VERSION" = "latest" ]; then
    echo "Cannot rollback from latest tag"
    exit 1
fi

# Rollback deployment
kubectl rollout undo deployment/$APP_NAME

# Wait for rollback
echo "Waiting for rollback to complete..."
kubectl rollout status deployment/$APP_NAME

# Health check
echo "Performing health check..."
sleep 10
curl -f http://localhost/health || exit 1

echo "Rollback completed successfully!"
```

## 🎯 Deployment Best Practices

### 1. Environment Management
- Use separate configurations for each environment
- Store secrets securely (not in code)
- Use environment variables for configuration
- Implement proper logging and monitoring

### 2. Containerization
- Use multi-stage builds for smaller images
- Implement health checks
- Use non-root users
- Optimize layer caching

### 3. Orchestration
- Use rolling updates for zero-downtime deployments
- Implement proper resource limits
- Use horizontal pod autoscaling
- Monitor application health

### 4. Security
- Scan images for vulnerabilities
- Use secrets management
- Implement network policies
- Regular security updates

### 5. Monitoring
- Implement comprehensive logging
- Use metrics collection
- Set up alerting
- Monitor application performance

## 🚀 Next Steps

1. **Choose deployment strategy** (Docker, Kubernetes, Cloud)
2. **Set up CI/CD pipeline** for automated deployments
3. **Configure environment-specific** TSK files
4. **Implement monitoring** and logging
5. **Test deployment process** thoroughly

---

**"We don't bow to any king"** - TuskLang provides flexible deployment strategies for any environment. Choose your deployment method, configure with TSK, and deploy with confidence! 