# 🟨 TuskLang JavaScript Production Deployment Guide

**"We don't bow to any king" - JavaScript Edition**

Deploy your TuskLang-powered JavaScript applications to production with confidence. Learn best practices for Docker, Kubernetes, cloud platforms, and monitoring.

## 🐳 Docker Deployment

### Production Dockerfile

```dockerfile
# Multi-stage build for production
FROM node:18-alpine AS builder

# Install build dependencies
RUN apk add --no-cache python3 make g++

# Set working directory
WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci --only=production

# Copy source code
COPY . .

# Build application (if needed)
RUN npm run build

# Production stage
FROM node:18-alpine AS production

# Install runtime dependencies
RUN apk add --no-cache dumb-init

# Create non-root user
RUN addgroup -g 1001 -S nodejs
RUN adduser -S nodejs -u 1001

# Set working directory
WORKDIR /app

# Copy built application
COPY --from=builder --chown=nodejs:nodejs /app ./

# Copy TuskLang configuration
COPY --chown=nodejs:nodejs config/ ./config/

# Create necessary directories
RUN mkdir -p /app/logs /app/data && chown -R nodejs:nodejs /app

# Switch to non-root user
USER nodejs

# Expose port
EXPOSE 3000

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD node healthcheck.js

# Start application
ENTRYPOINT ["dumb-init", "--"]
CMD ["node", "server.js"]
```

### Docker Compose for Production

```yaml
# docker-compose.prod.yml
version: '3.8'

services:
  app:
    build:
      context: .
      dockerfile: Dockerfile
      target: production
    container_name: tusklang-app
    restart: unless-stopped
    ports:
      - "3000:3000"
    environment:
      - NODE_ENV=production
      - DB_HOST=postgres
      - DB_PORT=5432
      - DB_NAME=myapp
      - DB_USER=postgres
      - DB_PASSWORD=${DB_PASSWORD}
      - REDIS_HOST=redis
      - REDIS_PORT=6379
      - JWT_SECRET=${JWT_SECRET}
      - API_KEY=${API_KEY}
    volumes:
      - app-logs:/app/logs
      - app-data:/app/data
    depends_on:
      - postgres
      - redis
    networks:
      - app-network
    deploy:
      resources:
        limits:
          memory: 512M
          cpus: '0.5'
        reservations:
          memory: 256M
          cpus: '0.25'

  postgres:
    image: postgres:15-alpine
    container_name: tusklang-postgres
    restart: unless-stopped
    environment:
      - POSTGRES_DB=myapp
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=${DB_PASSWORD}
    volumes:
      - postgres-data:/var/lib/postgresql/data
      - ./init.sql:/docker-entrypoint-initdb.d/init.sql
    networks:
      - app-network
    deploy:
      resources:
        limits:
          memory: 1G
          cpus: '1.0'

  redis:
    image: redis:7-alpine
    container_name: tusklang-redis
    restart: unless-stopped
    command: redis-server --appendonly yes --requirepass ${REDIS_PASSWORD}
    volumes:
      - redis-data:/data
    networks:
      - app-network
    deploy:
      resources:
        limits:
          memory: 256M
          cpus: '0.25'

  nginx:
    image: nginx:alpine
    container_name: tusklang-nginx
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/nginx/ssl
    depends_on:
      - app
    networks:
      - app-network

volumes:
  app-logs:
  app-data:
  postgres-data:
  redis-data:

networks:
  app-network:
    driver: bridge
```

### Production Configuration

```javascript
// config/production.tsk
const productionConfig = `
app {
    name: "MyApp"
    version: "1.0.0"
    environment: "production"
}

server {
    host: "0.0.0.0"
    port: @env("PORT", 3000)
    workers: @env("WORKERS", 4)
    timeout: 30000
}

database {
    host: @env("DB_HOST", "localhost")
    port: @env("DB_PORT", "5432")
    name: @env("DB_NAME", "myapp")
    user: @env("DB_USER", "postgres")
    password: @env.secure("DB_PASSWORD")
    
    # Production database settings
    ssl: true
    pool_size: 20
    connection_timeout: 5000
    idle_timeout: 30000
    
    # Read replicas for scaling
    read_replicas: [
        @env("DB_READ_REPLICA_1", "localhost"),
        @env("DB_READ_REPLICA_2", "localhost")
    ]
}

cache {
    redis {
        host: @env("REDIS_HOST", "localhost")
        port: @env("REDIS_PORT", "6379")
        password: @env.secure("REDIS_PASSWORD")
        
        # Production Redis settings
        max_retries: 3
        retry_delay: 1000
        connect_timeout: 5000
    }
    
    # Cache TTL settings
    user_cache_ttl: "30m"
    session_cache_ttl: "2h"
    query_cache_ttl: "5m"
}

security {
    # JWT settings
    jwt_secret: @env.secure("JWT_SECRET")
    jwt_expires_in: "24h"
    jwt_refresh_expires_in: "7d"
    
    # Rate limiting
    rate_limit: 100
    rate_limit_window: "15m"
    
    # CORS settings
    cors_origin: [
        "https://myapp.com",
        "https://www.myapp.com"
    ]
    
    # SSL/TLS
    force_https: true
    hsts_max_age: 31536000
    
    # Security headers
    content_security_policy: "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';"
}

logging {
    level: "error"
    format: "json"
    file: "/app/logs/app.log"
    
    # Log rotation
    max_size: "100m"
    max_files: 10
    
    # Structured logging
    include_timestamp: true
    include_request_id: true
}

monitoring {
    # Metrics collection
    metrics_enabled: true
    metrics_port: 9090
    
    # Health checks
    health_check_interval: "30s"
    health_check_timeout: "5s"
    
    # Alerting
    alert_email: @env("ALERT_EMAIL")
    alert_webhook: @env("ALERT_WEBHOOK")
}

# Performance settings
performance {
    # Query optimization
    query_timeout: 5000
    max_query_complexity: 100
    
    # Caching strategy
    cache_warmup: true
    cache_invalidation: "lazy"
    
    # Connection pooling
    db_pool_size: 20
    redis_pool_size: 10
}
`;
```

## ☁️ Cloud Platform Deployment

### AWS Deployment

```javascript
// config/aws.tsk
const awsConfig = `
app {
    name: "MyApp"
    version: "1.0.0"
    environment: "aws"
}

aws {
    region: @env("AWS_REGION", "us-east-1")
    
    # RDS Database
    rds {
        host: @env("RDS_HOST")
        port: @env("RDS_PORT", "5432")
        name: @env("RDS_DB_NAME")
        user: @env("RDS_USERNAME")
        password: @env.secure("RDS_PASSWORD")
        ssl: true
    }
    
    # ElastiCache Redis
    redis {
        host: @env("REDIS_HOST")
        port: @env("REDIS_PORT", "6379")
        password: @env.secure("REDIS_PASSWORD")
    }
    
    # S3 for file storage
    s3 {
        bucket: @env("S3_BUCKET")
        region: @env("AWS_REGION", "us-east-1")
        access_key: @env.secure("AWS_ACCESS_KEY_ID")
        secret_key: @env.secure("AWS_SECRET_ACCESS_KEY")
    }
    
    # CloudWatch logging
    cloudwatch {
        log_group: @env("CLOUDWATCH_LOG_GROUP", "/aws/ecs/myapp")
        region: @env("AWS_REGION", "us-east-1")
    }
}

# ECS/Fargate settings
ecs {
    cluster: @env("ECS_CLUSTER", "myapp-cluster")
    service: @env("ECS_SERVICE", "myapp-service")
    task_definition: @env("ECS_TASK_DEFINITION", "myapp-task")
    
    # Auto scaling
    min_capacity: @env("ECS_MIN_CAPACITY", "2")
    max_capacity: @env("ECS_MAX_CAPACITY", "10")
    target_cpu_utilization: @env("ECS_TARGET_CPU", "70")
}
`;
```

### Google Cloud Platform Deployment

```javascript
// config/gcp.tsk
const gcpConfig = `
app {
    name: "MyApp"
    version: "1.0.0"
    environment: "gcp"
}

gcp {
    project_id: @env("GCP_PROJECT_ID")
    region: @env("GCP_REGION", "us-central1")
    
    # Cloud SQL
    cloudsql {
        instance: @env("CLOUDSQL_INSTANCE")
        database: @env("CLOUDSQL_DATABASE")
        user: @env("CLOUDSQL_USER")
        password: @env.secure("CLOUDSQL_PASSWORD")
        ssl: true
    }
    
    # Memorystore Redis
    redis {
        host: @env("REDIS_HOST")
        port: @env("REDIS_PORT", "6379")
    }
    
    # Cloud Storage
    storage {
        bucket: @env("GCS_BUCKET")
        project_id: @env("GCP_PROJECT_ID")
    }
    
    # Cloud Logging
    logging {
        project_id: @env("GCP_PROJECT_ID")
        log_name: @env("CLOUD_LOGGING_NAME", "myapp-logs")
    }
}

# Cloud Run settings
cloudrun {
    service: @env("CLOUD_RUN_SERVICE", "myapp")
    region: @env("GCP_REGION", "us-central1")
    
    # Scaling
    min_instances: @env("CLOUD_RUN_MIN_INSTANCES", "0")
    max_instances: @env("CLOUD_RUN_MAX_INSTANCES", "100")
    concurrency: @env("CLOUD_RUN_CONCURRENCY", "80")
}
`;
```

### Azure Deployment

```javascript
// config/azure.tsk
const azureConfig = `
app {
    name: "MyApp"
    version: "1.0.0"
    environment: "azure"
}

azure {
    subscription_id: @env("AZURE_SUBSCRIPTION_ID")
    resource_group: @env("AZURE_RESOURCE_GROUP")
    location: @env("AZURE_LOCATION", "East US")
    
    # Azure Database for PostgreSQL
    postgresql {
        server: @env("AZURE_POSTGRES_SERVER")
        database: @env("AZURE_POSTGRES_DATABASE")
        user: @env("AZURE_POSTGRES_USER")
        password: @env.secure("AZURE_POSTGRES_PASSWORD")
        ssl: true
    }
    
    # Azure Cache for Redis
    redis {
        host: @env("AZURE_REDIS_HOST")
        port: @env("AZURE_REDIS_PORT", "6380")
        password: @env.secure("AZURE_REDIS_PASSWORD")
        ssl: true
    }
    
    # Azure Blob Storage
    storage {
        account: @env("AZURE_STORAGE_ACCOUNT")
        container: @env("AZURE_STORAGE_CONTAINER")
        key: @env.secure("AZURE_STORAGE_KEY")
    }
    
    # Application Insights
    appinsights {
        instrumentation_key: @env("AZURE_APPINSIGHTS_KEY")
        connection_string: @env("AZURE_APPINSIGHTS_CONNECTION_STRING")
    }
}

# Azure Container Instances
aci {
    resource_group: @env("AZURE_RESOURCE_GROUP")
    location: @env("AZURE_LOCATION", "East US")
    
    # Scaling
    min_replicas: @env("ACI_MIN_REPLICAS", "1")
    max_replicas: @env("ACI_MAX_REPLICAS", "10")
}
`;
```

## 🚢 Kubernetes Deployment

### Kubernetes Manifests

```yaml
# k8s/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tusklang-app
  labels:
    app: tusklang-app
spec:
  replicas: 3
  selector:
    matchLabels:
      app: tusklang-app
  template:
    metadata:
      labels:
        app: tusklang-app
    spec:
      containers:
      - name: app
        image: myapp:latest
        ports:
        - containerPort: 3000
        env:
        - name: NODE_ENV
          value: "production"
        - name: DB_HOST
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: host
        - name: DB_PASSWORD
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: password
        - name: JWT_SECRET
          valueFrom:
            secretKeyRef:
              name: app-secret
              key: jwt-secret
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
            port: 3000
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 3000
          initialDelaySeconds: 5
          periodSeconds: 5
        volumeMounts:
        - name: app-logs
          mountPath: /app/logs
        - name: app-config
          mountPath: /app/config
          readOnly: true
      volumes:
      - name: app-logs
        emptyDir: {}
      - name: app-config
        configMap:
          name: app-config
---
# k8s/service.yaml
apiVersion: v1
kind: Service
metadata:
  name: tusklang-app-service
spec:
  selector:
    app: tusklang-app
  ports:
  - protocol: TCP
    port: 80
    targetPort: 3000
  type: LoadBalancer
---
# k8s/ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: tusklang-app-ingress
  annotations:
    kubernetes.io/ingress.class: nginx
    cert-manager.io/cluster-issuer: letsencrypt-prod
spec:
  tls:
  - hosts:
    - myapp.com
    secretName: myapp-tls
  rules:
  - host: myapp.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: tusklang-app-service
            port:
              number: 80
---
# k8s/configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: app-config
data:
  production.tsk: |
    app {
        name: "MyApp"
        version: "1.0.0"
        environment: "production"
    }
    
    server {
        host: "0.0.0.0"
        port: 3000
        workers: 4
    }
    
    database {
        host: ${DB_HOST}
        port: 5432
        name: "myapp"
        user: "postgres"
        password: @env.secure("DB_PASSWORD")
        ssl: true
        pool_size: 20
    }
    
    security {
        jwt_secret: @env.secure("JWT_SECRET")
        rate_limit: 100
        cors_origin: ["https://myapp.com"]
    }
    
    logging {
        level: "error"
        format: "json"
        file: "/app/logs/app.log"
    }
---
# k8s/secrets.yaml
apiVersion: v1
kind: Secret
metadata:
  name: db-secret
type: Opaque
data:
  host: <base64-encoded-host>
  password: <base64-encoded-password>
---
apiVersion: v1
kind: Secret
metadata:
  name: app-secret
type: Opaque
data:
  jwt-secret: <base64-encoded-jwt-secret>
```

### Kubernetes Configuration with TuskLang

```javascript
// config/kubernetes.tsk
const k8sConfig = `
app {
    name: "MyApp"
    version: "1.0.0"
    environment: "kubernetes"
}

kubernetes {
    namespace: @env("K8S_NAMESPACE", "default")
    deployment: @env("K8S_DEPLOYMENT", "tusklang-app")
    service: @env("K8S_SERVICE", "tusklang-app-service")
    
    # Resource limits
    resources {
        requests {
            memory: "256Mi"
            cpu: "250m"
        }
        limits {
            memory: "512Mi"
            cpu: "500m"
        }
    }
    
    # Scaling
    replicas: @env("K8S_REPLICAS", "3")
    min_replicas: @env("K8S_MIN_REPLICAS", "1")
    max_replicas: @env("K8S_MAX_REPLICAS", "10")
    
    # Health checks
    health_check {
        path: "/health"
        port: 3000
        initial_delay: 30
        period: 10
        timeout: 5
        failure_threshold: 3
    }
}

database {
    host: @env("DB_HOST")
    port: @env("DB_PORT", "5432")
    name: @env("DB_NAME", "myapp")
    user: @env("DB_USER", "postgres")
    password: @env.secure("DB_PASSWORD")
    
    # Kubernetes-specific settings
    ssl: true
    pool_size: 20
    connection_timeout: 5000
}

redis {
    host: @env("REDIS_HOST")
    port: @env("REDIS_PORT", "6379")
    password: @env.secure("REDIS_PASSWORD")
    
    # Kubernetes Redis settings
    max_retries: 3
    retry_delay: 1000
    connect_timeout: 5000
}

monitoring {
    # Prometheus metrics
    prometheus {
        enabled: true
        port: 9090
        path: "/metrics"
    }
    
    # Kubernetes health checks
    health_check {
        enabled: true
        path: "/health"
        port: 3000
    }
    
    # Logging
    logging {
        level: "error"
        format: "json"
        output: "stdout"
    }
}
`;
```

## 📊 Monitoring and Observability

### Application Monitoring

```javascript
// monitoring/app-monitor.js
const TuskLang = require('tusklang');
const prometheus = require('prom-client');

// Initialize Prometheus metrics
const collectDefaultMetrics = prometheus.collectDefaultMetrics;
collectDefaultMetrics({ timeout: 5000 });

// Custom metrics
const httpRequestDuration = new prometheus.Histogram({
    name: 'http_request_duration_seconds',
    help: 'Duration of HTTP requests in seconds',
    labelNames: ['method', 'route', 'status_code']
});

const activeConnections = new prometheus.Gauge({
    name: 'active_connections',
    help: 'Number of active database connections'
});

const cacheHitRate = new prometheus.Gauge({
    name: 'cache_hit_rate',
    help: 'Cache hit rate percentage'
});

// TuskLang monitoring configuration
const monitoringConfig = TuskLang.parse(`
monitoring {
    # Metrics collection
    metrics {
        enabled: true
        port: 9090
        path: "/metrics"
        
        # Custom metrics
        request_duration: @metrics.histogram("http_request_duration_seconds", 0.1)
        active_connections: @metrics.gauge("active_connections")
        cache_hit_rate: @metrics.gauge("cache_hit_rate")
        
        # Business metrics
        orders_per_minute: @metrics.rate("orders_per_minute")
        revenue_per_hour: @metrics.rate("revenue_per_hour")
        user_registrations: @metrics.counter("user_registrations")
    }
    
    # Health checks
    health {
        enabled: true
        path: "/health"
        port: 3000
        
        # Database health
        database_check: @query("SELECT 1")
        
        # Redis health
        redis_check: @redis.ping()
        
        # External service health
        api_health: @http.get("https://api.example.com/health")
    }
    
    # Alerting
    alerts {
        enabled: true
        
        # High CPU usage
        high_cpu: @if(@metrics.system("cpu_usage") > 80, true, false)
        
        # High memory usage
        high_memory: @if(@metrics.system("memory_usage") > 85, true, false)
        
        # High error rate
        high_error_rate: @if(@metrics.rate("errors") > 0.05, true, false)
        
        # Database connection issues
        db_connection_issues: @if(@metrics.gauge("active_connections") < 5, true, false)
    }
    
    # Logging
    logging {
        level: "error"
        format: "json"
        
        # Structured logging
        fields {
            timestamp: @date.now()
            service: "myapp"
            version: "1.0.0"
            environment: "production"
        }
        
        # Log aggregation
        output: "stdout"
        include_request_id: true
        include_user_id: true
    }
}
`);

// Health check endpoint
app.get('/health', async (req, res) => {
    try {
        const health = await tsk.parse(TuskLang.parse(`
            health {
                status: "healthy"
                timestamp: @date.now()
                uptime: @metrics.system("uptime")
                
                # Database health
                database: @query("SELECT 1 as status")
                
                # Redis health
                redis: @redis.ping()
                
                # System metrics
                cpu_usage: @metrics.system("cpu_usage")
                memory_usage: @metrics.system("memory_usage")
                disk_usage: @metrics.system("disk_usage")
                
                # Application metrics
                active_connections: @metrics.gauge("active_connections")
                cache_hit_rate: @metrics.gauge("cache_hit_rate")
                error_rate: @metrics.rate("errors")
            }
        `));
        
        res.json(health);
    } catch (error) {
        res.status(503).json({
            status: "unhealthy",
            error: error.message,
            timestamp: new Date().toISOString()
        });
    }
});

// Metrics endpoint
app.get('/metrics', async (req, res) => {
    try {
        res.set('Content-Type', prometheus.register.contentType);
        res.end(await prometheus.register.metrics());
    } catch (error) {
        res.status(500).end(error);
    }
});
```

## 📚 Next Steps

1. **[Performance Optimization](008-performance-optimization-javascript.md)** - Optimize your applications
2. **[Security Best Practices](009-security-best-practices-javascript.md)** - Secure your applications
3. **[Testing Strategies](010-testing-strategies-javascript.md)** - Test your deployments
4. **[Scaling Applications](011-scaling-applications-javascript.md)** - Scale your applications

## 🎉 Production Deployment Complete!

You now understand how to deploy TuskLang applications to:
- ✅ **Docker** - Containerized deployments
- ✅ **Kubernetes** - Orchestrated deployments
- ✅ **AWS** - Cloud platform deployment
- ✅ **GCP** - Google Cloud deployment
- ✅ **Azure** - Microsoft Azure deployment
- ✅ **Monitoring** - Observability and alerting

**Ready to scale your TuskLang applications to production!** 