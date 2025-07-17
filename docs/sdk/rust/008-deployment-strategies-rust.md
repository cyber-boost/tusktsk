# 🦀 TuskLang Rust Deployment Strategies

**"We don't bow to any king" - Rust Edition**

Master deployment strategies for TuskLang Rust applications. From Docker containers to Kubernetes clusters, from cloud platforms to edge computing - learn how to deploy your applications with zero downtime, maximum performance, and complete reliability.

## 🐳 Docker Deployment

### Basic Dockerfile

```dockerfile
# Multi-stage build for optimized production image
FROM rust:1.70-alpine AS builder

# Install build dependencies
RUN apk add --no-cache musl-dev openssl-dev

WORKDIR /app

# Install TuskLang CLI
RUN cargo install tusklang-cli

# Copy dependency files
COPY Cargo.toml Cargo.lock ./

# Create dummy main.rs to build dependencies
RUN mkdir src && echo "fn main() {}" > src/main.rs
RUN cargo build --release
RUN rm -rf src

# Copy source code
COPY src ./src
COPY config.tsk ./

# Build the application
RUN cargo build --release

# Runtime stage
FROM alpine:latest

# Install runtime dependencies
RUN apk add --no-cache ca-certificates tzdata

WORKDIR /app

# Copy binary and TSK configuration
COPY --from=builder /app/target/release/app .
COPY --from=builder /usr/local/cargo/bin/tusk /usr/local/bin/
COPY --from=builder /app/config.tsk .

# Create non-root user
RUN addgroup -g 1001 -S appgroup && \
    adduser -u 1001 -S appuser -G appgroup

# Set ownership
RUN chown -R appuser:appgroup /app

USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD tusk health-check config.tsk || exit 1

# Expose port
EXPOSE 8080

# Run application
CMD ["./app"]
```

### Advanced Dockerfile with Multi-Architecture Support

```dockerfile
# Use buildx for multi-architecture builds
FROM --platform=$BUILDPLATFORM rust:1.70-alpine AS builder

# Install build dependencies
RUN apk add --no-cache musl-dev openssl-dev pkgconfig

WORKDIR /app

# Install TuskLang CLI
RUN cargo install tusklang-cli

# Copy dependency files
COPY Cargo.toml Cargo.lock ./

# Create dummy main.rs to build dependencies
RUN mkdir src && echo "fn main() {}" > src/main.rs
RUN cargo build --release
RUN rm -rf src

# Copy source code
COPY src ./src
COPY config.tsk ./

# Build the application
RUN cargo build --release

# Runtime stage
FROM alpine:latest

# Install runtime dependencies
RUN apk add --no-cache ca-certificates tzdata

WORKDIR /app

# Copy binary and TSK configuration
COPY --from=builder /app/target/release/app .
COPY --from=builder /usr/local/cargo/bin/tusk /usr/local/bin/
COPY --from=builder /app/config.tsk .

# Create non-root user
RUN addgroup -g 1001 -S appgroup && \
    adduser -u 1001 -S appuser -G appgroup

# Set ownership
RUN chown -R appuser:appgroup /app

USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD tusk health-check config.tsk || exit 1

# Expose port
EXPOSE 8080

# Run application
CMD ["./app"]
```

### Docker Compose Setup

```yaml
version: '3.8'

services:
  app:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      - APP_ENV=production
      - DB_PASSWORD=${DB_PASSWORD}
      - JWT_SECRET=${JWT_SECRET}
    volumes:
      - ./config:/app/config:ro
      - app-logs:/app/logs
    depends_on:
      - postgres
      - redis
    networks:
      - app-network
    restart: unless-stopped
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 2G
        reservations:
          cpus: '0.5'
          memory: 512M

  postgres:
    image: postgres:15-alpine
    environment:
      - POSTGRES_DB=tuskapp
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=${DB_PASSWORD}
    volumes:
      - postgres-data:/var/lib/postgresql/data
      - ./init.sql:/docker-entrypoint-initdb.d/init.sql:ro
    networks:
      - app-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 30s
      timeout: 10s
      retries: 3

  redis:
    image: redis:7-alpine
    command: redis-server --appendonly yes
    volumes:
      - redis-data:/data
    networks:
      - app-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 30s
      timeout: 10s
      retries: 3

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      - ./ssl:/etc/nginx/ssl:ro
    depends_on:
      - app
    networks:
      - app-network
    restart: unless-stopped

volumes:
  postgres-data:
  redis-data:
  app-logs:

networks:
  app-network:
    driver: bridge
```

### Production Configuration (`config.tsk`)

```tsk
app_name: "TuskApp"
version: "1.0.0"
environment: @env("APP_ENV", "production")

[server]
host: "0.0.0.0"
port: 8080
workers: @if($environment == "production", 4, 1)

[database]
host: @env("DB_HOST", "postgres")
port: @env("DB_PORT", 5432)
name: @env("DB_NAME", "tuskapp")
user: @env("DB_USER", "postgres")
password: @env("DB_PASSWORD")
ssl_mode: @if($environment == "production", "require", "disable")

[cache]
redis_host: @env("REDIS_HOST", "redis")
redis_port: @env("REDIS_PORT", 6379)
redis_db: @env("REDIS_DB", 0)
ttl: "5m"

[security]
jwt_secret: @env("JWT_SECRET")
bcrypt_rounds: 12
session_timeout: "24h"

[logging]
level: @if($environment == "production", "info", "debug")
format: @if($environment == "production", "json", "text")
file: @if($environment == "production", "/app/logs/app.log", "console")

[monitoring]
metrics_port: 9090
health_check_interval: "30s"
prometheus_enabled: true

[deployment]
docker_image: "tuskapp:latest"
container_name: "tuskapp"
restart_policy: "unless-stopped"
```

## ☸️ Kubernetes Deployment

### Basic Kubernetes Manifests

```yaml
# ConfigMap for TSK configuration
apiVersion: v1
kind: ConfigMap
metadata:
  name: tuskapp-config
  namespace: default
data:
  config.tsk: |
    app_name: "TuskApp"
    version: "1.0.0"
    environment: "production"
    
    [server]
    host: "0.0.0.0"
    port: 8080
    workers: 4
    
    [database]
    host: "postgres-service"
    port: 5432
    name: "tuskapp"
    user: "postgres"
    password: "${DB_PASSWORD}"
    
    [cache]
    redis_host: "redis-service"
    redis_port: 6379
    ttl: "5m"
    
    [security]
    jwt_secret: "${JWT_SECRET}"
    
    [logging]
    level: "info"
    format: "json"
    file: "/app/logs/app.log"
```

```yaml
# Secret for sensitive data
apiVersion: v1
kind: Secret
metadata:
  name: tuskapp-secrets
  namespace: default
type: Opaque
data:
  db-password: <base64-encoded-password>
  jwt-secret: <base64-encoded-jwt-secret>
```

```yaml
# Deployment
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tuskapp
  namespace: default
  labels:
    app: tuskapp
spec:
  replicas: 3
  selector:
    matchLabels:
      app: tuskapp
  template:
    metadata:
      labels:
        app: tuskapp
    spec:
      containers:
      - name: app
        image: tuskapp:latest
        ports:
        - containerPort: 8080
        env:
        - name: APP_ENV
          value: "production"
        - name: DB_PASSWORD
          valueFrom:
            secretKeyRef:
              name: tuskapp-secrets
              key: db-password
        - name: JWT_SECRET
          valueFrom:
            secretKeyRef:
              name: tuskapp-secrets
              key: jwt-secret
        volumeMounts:
        - name: config
          mountPath: /app/config
        - name: logs
          mountPath: /app/logs
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
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
      volumes:
      - name: config
        configMap:
          name: tuskapp-config
      - name: logs
        emptyDir: {}
```

```yaml
# Service
apiVersion: v1
kind: Service
metadata:
  name: tuskapp-service
  namespace: default
spec:
  selector:
    app: tuskapp
  ports:
  - protocol: TCP
    port: 80
    targetPort: 8080
  type: ClusterIP
```

```yaml
# Ingress
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: tuskapp-ingress
  namespace: default
  annotations:
    nginx.ingress.kubernetes.io/rewrite-target: /
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
spec:
  tls:
  - hosts:
    - tuskapp.example.com
    secretName: tuskapp-tls
  rules:
  - host: tuskapp.example.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: tuskapp-service
            port:
              number: 80
```

### Advanced Kubernetes Setup with Helm

```yaml
# values.yaml
replicaCount: 3

image:
  repository: tuskapp
  tag: latest
  pullPolicy: IfNotPresent

service:
  type: ClusterIP
  port: 80

ingress:
  enabled: true
  className: nginx
  annotations:
    nginx.ingress.kubernetes.io/rewrite-target: /
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
  hosts:
    - host: tuskapp.example.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: tuskapp-tls
      hosts:
        - tuskapp.example.com

resources:
  limits:
    cpu: 500m
    memory: 512Mi
  requests:
    cpu: 250m
    memory: 256Mi

autoscaling:
  enabled: true
  minReplicas: 2
  maxReplicas: 10
  targetCPUUtilizationPercentage: 80
  targetMemoryUtilizationPercentage: 80

config:
  app_name: "TuskApp"
  version: "1.0.0"
  environment: "production"
  
  server:
    host: "0.0.0.0"
    port: 8080
    workers: 4
  
  database:
    host: "postgres-service"
    port: 5432
    name: "tuskapp"
    user: "postgres"
  
  cache:
    redis_host: "redis-service"
    redis_port: 6379
    ttl: "5m"
  
  security:
    bcrypt_rounds: 12
    session_timeout: "24h"
  
  logging:
    level: "info"
    format: "json"
  
  monitoring:
    metrics_port: 9090
    health_check_interval: "30s"
    prometheus_enabled: true

secrets:
  db_password: ""
  jwt_secret: ""
```

```yaml
# templates/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: {{ include "tuskapp.fullname" . }}
  labels:
    {{- include "tuskapp.labels" . | nindent 4 }}
spec:
  {{- if not .Values.autoscaling.enabled }}
  replicas: {{ .Values.replicaCount }}
  {{- end }}
  selector:
    matchLabels:
      {{- include "tuskapp.selectorLabels" . | nindent 6 }}
  template:
    metadata:
      labels:
        {{- include "tuskapp.selectorLabels" . | nindent 8 }}
    spec:
      containers:
        - name: {{ .Chart.Name }}
          image: "{{ .Values.image.repository }}:{{ .Values.image.tag }}"
          imagePullPolicy: {{ .Values.image.pullPolicy }}
          ports:
            - name: http
              containerPort: 8080
              protocol: TCP
          env:
            - name: APP_ENV
              value: {{ .Values.config.environment | quote }}
            - name: DB_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: {{ include "tuskapp.fullname" . }}-secrets
                  key: db-password
            - name: JWT_SECRET
              valueFrom:
                secretKeyRef:
                  name: {{ include "tuskapp.fullname" . }}-secrets
                  key: jwt-secret
          volumeMounts:
            - name: config
              mountPath: /app/config
            - name: logs
              mountPath: /app/logs
          resources:
            {{- toYaml .Values.resources | nindent 12 }}
          livenessProbe:
            httpGet:
              path: /health
              port: http
            initialDelaySeconds: 30
            periodSeconds: 10
          readinessProbe:
            httpGet:
              path: /ready
              port: http
            initialDelaySeconds: 5
            periodSeconds: 5
      volumes:
        - name: config
          configMap:
            name: {{ include "tuskapp.fullname" . }}-config
        - name: logs
          emptyDir: {}
```

## ☁️ Cloud Platform Deployment

### AWS ECS Deployment

```yaml
# task-definition.json
{
  "family": "tuskapp",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "512",
  "memory": "1024",
  "executionRoleArn": "arn:aws:iam::123456789012:role/ecsTaskExecutionRole",
  "taskRoleArn": "arn:aws:iam::123456789012:role/ecsTaskRole",
  "containerDefinitions": [
    {
      "name": "tuskapp",
      "image": "123456789012.dkr.ecr.us-east-1.amazonaws.com/tuskapp:latest",
      "portMappings": [
        {
          "containerPort": 8080,
          "protocol": "tcp"
        }
      ],
      "environment": [
        {
          "name": "APP_ENV",
          "value": "production"
        },
        {
          "name": "DB_HOST",
          "value": "tuskapp-db.cluster-xyz.us-east-1.rds.amazonaws.com"
        }
      ],
      "secrets": [
        {
          "name": "DB_PASSWORD",
          "valueFrom": "arn:aws:secretsmanager:us-east-1:123456789012:secret:tuskapp/db-password"
        },
        {
          "name": "JWT_SECRET",
          "valueFrom": "arn:aws:secretsmanager:us-east-1:123456789012:secret:tuskapp/jwt-secret"
        }
      ],
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/tuskapp",
          "awslogs-region": "us-east-1",
          "awslogs-stream-prefix": "ecs"
        }
      },
      "healthCheck": {
        "command": ["CMD-SHELL", "tusk health-check /app/config/config.tsk || exit 1"],
        "interval": 30,
        "timeout": 5,
        "retries": 3,
        "startPeriod": 60
      }
    }
  ]
}
```

```yaml
# ecs-service.yaml
apiVersion: ecs/v1
kind: Service
metadata:
  name: tuskapp-service
spec:
  cluster: tuskapp-cluster
  taskDefinition: tuskapp
  desiredCount: 3
  launchType: FARGATE
  networkConfiguration:
    awsvpcConfiguration:
      subnets:
        - subnet-12345678
        - subnet-87654321
      securityGroups:
        - sg-12345678
      assignPublicIp: ENABLED
  loadBalancers:
    - targetGroupArn: arn:aws:elasticloadbalancing:us-east-1:123456789012:targetgroup/tuskapp-tg/1234567890123456
      containerName: tuskapp
      containerPort: 8080
  deploymentConfiguration:
    maximumPercent: 200
    minimumHealthyPercent: 100
  autoScalingPolicies:
    - policyName: cpu-scaling
      targetTrackingScalingPolicyConfiguration:
        predefinedMetricSpecification:
          predefinedMetricType: ECSServiceAverageCPUUtilization
        targetValue: 70.0
        scaleInCooldown: 300
        scaleOutCooldown: 300
```

### Google Cloud Run Deployment

```yaml
# cloud-run.yaml
apiVersion: serving.knative.dev/v1
kind: Service
metadata:
  name: tuskapp
  namespace: default
spec:
  template:
    metadata:
      annotations:
        autoscaling.knative.dev/minScale: "2"
        autoscaling.knative.dev/maxScale: "10"
        autoscaling.knative.dev/target: "70"
    spec:
      containerConcurrency: 80
      timeoutSeconds: 300
      containers:
      - image: gcr.io/my-project/tuskapp:latest
        ports:
        - containerPort: 8080
        env:
        - name: APP_ENV
          value: "production"
        - name: DB_HOST
          value: "10.0.0.1"
        - name: DB_PASSWORD
          valueFrom:
            secretKeyRef:
              name: tuskapp-secrets
              key: db-password
        - name: JWT_SECRET
          valueFrom:
            secretKeyRef:
              name: tuskapp-secrets
              key: jwt-secret
        resources:
          limits:
            cpu: "1000m"
            memory: "1Gi"
          requests:
            cpu: "500m"
            memory: "512Mi"
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
```

### Azure Container Instances

```yaml
# azure-container-instance.yaml
apiVersion: 2019-12-01
location: eastus
properties:
  containers:
  - name: tuskapp
    properties:
      image: myregistry.azurecr.io/tuskapp:latest
      ports:
      - port: 8080
        protocol: TCP
      environmentVariables:
      - name: APP_ENV
        value: "production"
      - name: DB_HOST
        value: "tuskapp-db.database.windows.net"
      - name: DB_PASSWORD
        secureValue: "{{DB_PASSWORD}}"
      - name: JWT_SECRET
        secureValue: "{{JWT_SECRET}}"
      resources:
        requests:
          memoryInGB: 1
          cpu: 0.5
        limits:
          memoryInGB: 2
          cpu: 1
      volumeMounts:
      - name: config
        mountPath: /app/config
      - name: logs
        mountPath: /app/logs
  volumes:
  - name: config
    azureFile:
      shareName: tuskapp-config
      storageAccountName: tuskappstorage
      storageAccountKey: "{{STORAGE_ACCOUNT_KEY}}"
  - name: logs
    azureFile:
      shareName: tuskapp-logs
      storageAccountName: tuskappstorage
      storageAccountKey: "{{STORAGE_ACCOUNT_KEY}}"
  osType: Linux
  restartPolicy: Always
  ipAddress:
    type: Public
    ports:
    - protocol: TCP
      port: 8080
  tags:
    environment: production
    app: tuskapp
```

## 🔄 CI/CD Pipeline

### GitHub Actions Pipeline

```yaml
# .github/workflows/deploy.yml
name: Deploy TuskApp

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup Rust
      uses: actions-rs/toolchain@v1
      with:
        toolchain: stable
        override: true
    
    - name: Install TuskLang CLI
      run: cargo install tusklang-cli
    
    - name: Validate TSK configuration
      run: tusk validate config.tsk
    
    - name: Run tests
      run: cargo test --all-features
    
    - name: Run integration tests
      run: cargo test --test integration_tests
    
    - name: Run performance tests
      run: cargo test --test performance_tests

  build:
    needs: test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup Docker Buildx
      uses: docker/setup-buildx-action@v2
    
    - name: Log in to Container Registry
      uses: docker/login-action@v2
      with:
        registry: ${{ env.REGISTRY }}
        username: ${{ github.actor }}
        password: ${{ secrets.GITHUB_TOKEN }}
    
    - name: Extract metadata
      id: meta
      uses: docker/metadata-action@v4
      with:
        images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
        tags: |
          type=ref,event=branch
          type=ref,event=pr
          type=semver,pattern={{version}}
          type=semver,pattern={{major}}.{{minor}}
          type=sha
    
    - name: Build and push Docker image
      uses: docker/build-push-action@v4
      with:
        context: .
        platforms: linux/amd64,linux/arm64
        push: true
        tags: ${{ steps.meta.outputs.tags }}
        labels: ${{ steps.meta.outputs.labels }}
        cache-from: type=gha
        cache-to: type=gha,mode=max

  deploy-staging:
    needs: build
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    environment: staging
    
    steps:
    - name: Deploy to staging
      run: |
        echo "Deploying to staging environment"
        # Add your staging deployment commands here
        kubectl set image deployment/tuskapp tuskapp=${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:${{ github.sha }}

  deploy-production:
    needs: build
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    environment: production
    
    steps:
    - name: Deploy to production
      run: |
        echo "Deploying to production environment"
        # Add your production deployment commands here
        kubectl set image deployment/tuskapp tuskapp=${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:${{ github.sha }}
```

### GitLab CI/CD Pipeline

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
  image: rust:1.70
  before_script:
    - cargo install tusklang-cli
  script:
    - tusk validate config.tsk
    - cargo test --all-features
    - cargo test --test integration_tests
    - cargo test --test performance_tests
  coverage: '/All tests passed/'

build:
  stage: build
  image: docker:20.10.16
  services:
    - docker:20.10.16-dind
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

deploy-staging:
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
    - kubectl set image deployment/tuskapp tuskapp=$CI_REGISTRY_IMAGE:$CI_COMMIT_SHA -n staging
  environment:
    name: staging
  only:
    - main

deploy-production:
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
    - kubectl set image deployment/tuskapp tuskapp=$CI_REGISTRY_IMAGE:$CI_COMMIT_SHA -n production
  environment:
    name: production
  when: manual
  only:
    - main
```

## 🔍 Monitoring and Observability

### Prometheus Configuration

```yaml
# prometheus.yml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

rule_files:
  - "tuskapp-rules.yml"

scrape_configs:
  - job_name: 'tuskapp'
    static_configs:
      - targets: ['tuskapp-service:8080']
    metrics_path: '/metrics'
    scrape_interval: 5s

  - job_name: 'tuskapp-health'
    static_configs:
      - targets: ['tuskapp-service:8080']
    metrics_path: '/health'
    scrape_interval: 30s
```

```yaml
# tuskapp-rules.yml
groups:
  - name: tuskapp
    rules:
      - alert: HighErrorRate
        expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.1
        for: 2m
        labels:
          severity: warning
        annotations:
          summary: "High error rate detected"
          description: "Error rate is {{ $value }} errors per second"

      - alert: HighResponseTime
        expr: histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 1
        for: 2m
        labels:
          severity: warning
        annotations:
          summary: "High response time detected"
          description: "95th percentile response time is {{ $value }} seconds"

      - alert: DatabaseConnectionIssues
        expr: up{job="tuskapp"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "TuskApp is down"
          description: "TuskApp has been down for more than 1 minute"
```

### Grafana Dashboard

```json
{
  "dashboard": {
    "id": null,
    "title": "TuskApp Dashboard",
    "tags": ["tuskapp", "rust"],
    "timezone": "browser",
    "panels": [
      {
        "id": 1,
        "title": "Request Rate",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(http_requests_total[5m])",
            "legendFormat": "{{method}} {{endpoint}}"
          }
        ]
      },
      {
        "id": 2,
        "title": "Response Time",
        "type": "graph",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))",
            "legendFormat": "95th percentile"
          }
        ]
      },
      {
        "id": 3,
        "title": "Error Rate",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(http_requests_total{status=~\"5..\"}[5m])",
            "legendFormat": "5xx errors"
          }
        ]
      },
      {
        "id": 4,
        "title": "Database Connections",
        "type": "stat",
        "targets": [
          {
            "expr": "tuskapp_database_connections_active",
            "legendFormat": "Active connections"
          }
        ]
      }
    ]
  }
}
```

## 🚀 Performance Optimization

### Resource Optimization

```rust
use tusklang_rust::{parse, Parser};
use std::sync::Arc;

// Optimized parser with connection pooling
async fn setup_optimized_parser() -> Result<Arc<Parser>, Box<dyn std::error::Error>> {
    let mut parser = Parser::new();
    
    // Setup database with connection pooling
    let postgres = PostgreSQLAdapter::with_pool(PostgreSQLConfig {
        host: "localhost".to_string(),
        port: 5432,
        database: "tuskapp".to_string(),
        user: "postgres".to_string(),
        password: "secret".to_string(),
        ssl_mode: "disable".to_string(),
    }, PoolConfig {
        max_open_conns: 50,
        max_idle_conns: 20,
        conn_max_lifetime: Duration::from_secs(300),
        conn_max_idle_time: Duration::from_secs(60),
    }).await?;
    
    parser.set_database_adapter(postgres);
    
    // Setup caching
    let redis_cache = RedisCache::new(RedisConfig {
        host: "localhost".to_string(),
        port: 6379,
        db: 0,
    }).await?;
    
    parser.set_cache(redis_cache);
    
    Ok(Arc::new(parser))
}
```

### Health Check Implementation

```rust
use actix_web::{get, HttpResponse, Result};
use tusklang_rust::{parse, Parser};
use std::sync::Arc;

#[get("/health")]
async fn health_check(parser: web::Data<Arc<Parser>>) -> Result<HttpResponse> {
    // Check database connectivity
    let db_healthy = match parser.query("SELECT 1").await {
        Ok(_) => true,
        Err(_) => false,
    };
    
    // Check cache connectivity
    let cache_healthy = match parser.cache_ping().await {
        Ok(_) => true,
        Err(_) => false,
    };
    
    // Check TSK configuration
    let config_healthy = match parser.get_config() {
        Ok(_) => true,
        Err(_) => false,
    };
    
    let overall_healthy = db_healthy && cache_healthy && config_healthy;
    
    let status = if overall_healthy { 200 } else { 503 };
    
    Ok(HttpResponse::build(status)
        .json(json!({
            "status": if overall_healthy { "healthy" } else { "unhealthy" },
            "timestamp": chrono::Utc::now().to_rfc3339(),
            "checks": {
                "database": db_healthy,
                "cache": cache_healthy,
                "configuration": config_healthy
            }
        })))
}

#[get("/ready")]
async fn readiness_check(parser: web::Data<Arc<Parser>>) -> Result<HttpResponse> {
    // Check if application is ready to receive traffic
    let ready = match parser.query("SELECT 1").await {
        Ok(_) => true,
        Err(_) => false,
    };
    
    let status = if ready { 200 } else { 503 };
    
    Ok(HttpResponse::build(status)
        .json(json!({
            "ready": ready,
            "timestamp": chrono::Utc::now().to_rfc3339()
        })))
}
```

## 🎯 What You've Learned

1. **Docker deployment** - Multi-stage builds, optimization, and best practices
2. **Kubernetes deployment** - Manifests, Helm charts, and scaling strategies
3. **Cloud platform deployment** - AWS ECS, Google Cloud Run, Azure Container Instances
4. **CI/CD pipelines** - GitHub Actions, GitLab CI/CD, automated testing and deployment
5. **Monitoring and observability** - Prometheus, Grafana, health checks, and alerting
6. **Performance optimization** - Resource management, connection pooling, and caching
7. **Security best practices** - Secrets management, non-root containers, and network policies

## 🚀 Next Steps

1. **Choose your deployment strategy** - Select the platform that best fits your needs
2. **Set up CI/CD** - Implement automated testing and deployment pipelines
3. **Configure monitoring** - Set up comprehensive monitoring and alerting
4. **Optimize performance** - Implement resource optimization and caching strategies
5. **Plan for scaling** - Design your deployment for horizontal scaling and high availability

---

**You now have complete deployment mastery with TuskLang Rust!** From Docker containers to Kubernetes clusters, from cloud platforms to edge computing - TuskLang gives you the tools to deploy your applications with zero downtime, maximum performance, and complete reliability. 