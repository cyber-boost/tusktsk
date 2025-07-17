# ☕ TuskLang Java Production Deployment Guide

**"We don't bow to any king" - Java Edition**

Master production deployment of TuskLang Java applications with comprehensive coverage of Docker containerization, Kubernetes orchestration, monitoring, security, and scaling strategies.

## 🐳 Docker Containerization

### Multi-Stage Dockerfile

```dockerfile
# Build stage
FROM openjdk:17-alpine AS builder

WORKDIR /build

# Copy Maven files
COPY pom.xml .
COPY src ./src

# Install dependencies and build
RUN apk add --no-cache maven && \
    mvn clean package -DskipTests

# Runtime stage
FROM openjdk:17-alpine

WORKDIR /app

# Install TuskLang CLI
RUN apk add --no-cache curl && \
    curl -L -o /usr/local/bin/tusk https://github.com/tusklang/java/releases/latest/download/tusk-cli.jar && \
    chmod +x /usr/local/bin/tusk

# Create non-root user
RUN addgroup -g 1001 -S appgroup && \
    adduser -u 1001 -S appuser -G appgroup

# Copy application
COPY --from=builder /build/target/tusk-app-1.0.0.jar app.jar

# Copy TuskLang configuration
COPY config.tsk config.tsk

# Create necessary directories
RUN mkdir -p /app/logs /app/data && \
    chown -R appuser:appgroup /app

USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/actuator/health || exit 1

# Expose port
EXPOSE 8080

# Run application
CMD ["java", "-jar", "app.jar"]
```

### Docker Compose

```yaml
version: '3.8'

services:
  app:
    build: .
    ports:
      - "8080:8080"
    environment:
      - APP_ENV=production
      - DB_HOST=postgres
      - REDIS_HOST=redis
    depends_on:
      - postgres
      - redis
    volumes:
      - app-logs:/app/logs
      - app-data:/app/data
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/actuator/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  postgres:
    image: postgres:15-alpine
    environment:
      POSTGRES_DB: myapp
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: secret
    volumes:
      - postgres-data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    restart: unless-stopped

  redis:
    image: redis:7-alpine
    command: redis-server --appendonly yes
    volumes:
      - redis-data:/data
    ports:
      - "6379:6379"
    restart: unless-stopped

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
    restart: unless-stopped

volumes:
  postgres-data:
  redis-data:
  app-logs:
  app-data:
```

### Nginx Configuration

```nginx
events {
    worker_connections 1024;
}

http {
    upstream app_servers {
        server app:8080;
    }

    server {
        listen 80;
        server_name example.com;
        return 301 https://$server_name$request_uri;
    }

    server {
        listen 443 ssl http2;
        server_name example.com;

        ssl_certificate /etc/nginx/ssl/cert.pem;
        ssl_certificate_key /etc/nginx/ssl/key.pem;
        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers ECDHE-RSA-AES256-GCM-SHA512:DHE-RSA-AES256-GCM-SHA512:ECDHE-RSA-AES256-GCM-SHA384:DHE-RSA-AES256-GCM-SHA384;
        ssl_prefer_server_ciphers off;

        location / {
            proxy_pass http://app_servers;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_connect_timeout 30s;
            proxy_send_timeout 30s;
            proxy_read_timeout 30s;
        }

        location /actuator/health {
            proxy_pass http://app_servers;
            access_log off;
        }
    }
}
```

## ☸️ Kubernetes Deployment

### Deployment Configuration

```yaml
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
        - containerPort: 8080
        env:
        - name: APP_ENV
          value: "production"
        - name: DB_HOST
          valueFrom:
            configMapKeyRef:
              name: app-config
              key: db_host
        - name: DB_PASSWORD
          valueFrom:
            secretKeyRef:
              name: app-secrets
              key: db_password
        - name: JWT_SECRET
          valueFrom:
            secretKeyRef:
              name: app-secrets
              key: jwt_secret
        - name: REDIS_HOST
          value: "redis-service"
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /actuator/health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /actuator/health
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 3
        volumeMounts:
        - name: config-volume
          mountPath: /app/config
        - name: logs-volume
          mountPath: /app/logs
      volumes:
      - name: config-volume
        configMap:
          name: app-config
      - name: logs-volume
        emptyDir: {}
      imagePullSecrets:
      - name: registry-secret
```

### Service Configuration

```yaml
apiVersion: v1
kind: Service
metadata:
  name: tusk-app-service
  labels:
    app: tusk-app
spec:
  selector:
    app: tusk-app
  ports:
  - port: 80
    targetPort: 8080
    protocol: TCP
  type: ClusterIP
---
apiVersion: v1
kind: Service
metadata:
  name: tusk-app-external
  labels:
    app: tusk-app
spec:
  selector:
    app: tusk-app
  ports:
  - port: 80
    targetPort: 8080
    protocol: TCP
  type: LoadBalancer
```

### Ingress Configuration

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: tusk-app-ingress
  annotations:
    kubernetes.io/ingress.class: "nginx"
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    nginx.ingress.kubernetes.io/force-ssl-redirect: "true"
spec:
  tls:
  - hosts:
    - example.com
    secretName: tusk-app-tls
  rules:
  - host: example.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: tusk-app-service
            port:
              number: 80
```

### ConfigMap and Secrets

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: app-config
data:
  db_host: "postgres-service"
  db_port: "5432"
  db_name: "myapp"
  redis_host: "redis-service"
  redis_port: "6379"
  app_name: "TuskLang App"
  app_version: "1.0.0"
---
apiVersion: v1
kind: Secret
metadata:
  name: app-secrets
type: Opaque
data:
  db_password: c2VjcmV0 # base64 encoded
  jwt_secret: c2VjcmV0LWtleQ== # base64 encoded
  api_key: YXBpLWtleQ== # base64 encoded
```

### Horizontal Pod Autoscaler

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: tusk-app-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: tusk-app
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
  behavior:
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
      - type: Percent
        value: 10
        periodSeconds: 60
    scaleUp:
      stabilizationWindowSeconds: 60
      policies:
      - type: Percent
        value: 50
        periodSeconds: 60
```

## 📊 Monitoring and Observability

### Prometheus Configuration

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: prometheus-config
data:
  prometheus.yml: |
    global:
      scrape_interval: 15s
      evaluation_interval: 15s

    rule_files:
      - "alert_rules.yml"

    alerting:
      alertmanagers:
        - static_configs:
            - targets:
              - alertmanager:9093

    scrape_configs:
      - job_name: 'tusk-app'
        static_configs:
          - targets: ['tusk-app-service:8080']
        metrics_path: '/actuator/prometheus'
        scrape_interval: 5s

      - job_name: 'postgres'
        static_configs:
          - targets: ['postgres-exporter:9187']

      - job_name: 'redis'
        static_configs:
          - targets: ['redis-exporter:9121']
```

### Grafana Dashboard

```json
{
  "dashboard": {
    "title": "TuskLang Application Dashboard",
    "panels": [
      {
        "title": "Application Response Time",
        "type": "graph",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))",
            "legendFormat": "95th percentile"
          }
        ]
      },
      {
        "title": "Database Connections",
        "type": "graph",
        "targets": [
          {
            "expr": "pg_stat_database_numbackends",
            "legendFormat": "Active connections"
          }
        ]
      },
      {
        "title": "Cache Hit Rate",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(redis_commands_total{cmd=\"get\"}[5m]) / rate(redis_commands_total[5m])",
            "legendFormat": "Cache hit rate"
          }
        ]
      }
    ]
  }
}
```

### Application Metrics

```java
import io.micrometer.core.instrument.MeterRegistry;
import org.springframework.stereotype.Component;
import org.tusklang.java.TuskLang;

@Component
public class MetricsService {
    private final MeterRegistry meterRegistry;
    private final TuskLang parser;
    
    public MetricsService(MeterRegistry meterRegistry, TuskLang parser) {
        this.meterRegistry = meterRegistry;
        this.parser = parser;
    }
    
    public void recordApiCall(String endpoint, long duration) {
        meterRegistry.counter("api_calls_total", "endpoint", endpoint).increment();
        meterRegistry.timer("api_response_time", "endpoint", endpoint)
            .record(duration, java.util.concurrent.TimeUnit.MILLISECONDS);
    }
    
    public void recordDatabaseQuery(String query, long duration) {
        meterRegistry.counter("database_queries_total", "query", query).increment();
        meterRegistry.timer("database_query_time", "query", query)
            .record(duration, java.util.concurrent.TimeUnit.MILLISECONDS);
    }
    
    public void recordCacheHit(String cache, boolean hit) {
        meterRegistry.counter("cache_operations_total", "cache", cache, "result", hit ? "hit" : "miss").increment();
    }
    
    public Map<String, Object> getCustomMetrics() {
        return parser.parse("""
            [metrics]
            active_users: @metrics.gauge("active_users", @request.active_user_count)
            memory_usage: @metrics.gauge("memory_usage_bytes", @runtime.memory_used)
            cpu_usage: @metrics.gauge("cpu_usage_percent", @runtime.cpu_usage)
            """);
    }
}
```

## 🔒 Security Configuration

### Security Context

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tusk-app
spec:
  template:
    spec:
      securityContext:
        runAsNonRoot: true
        runAsUser: 1001
        runAsGroup: 1001
        fsGroup: 1001
        capabilities:
          drop:
            - ALL
      containers:
      - name: app
        securityContext:
          allowPrivilegeEscalation: false
          readOnlyRootFilesystem: true
          capabilities:
            drop:
              - ALL
        volumeMounts:
        - name: tmp
          mountPath: /tmp
        - name: logs
          mountPath: /app/logs
      volumes:
      - name: tmp
        emptyDir: {}
      - name: logs
        emptyDir: {}
```

### Network Policies

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: tusk-app-network-policy
spec:
  podSelector:
    matchLabels:
      app: tusk-app
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - namespaceSelector:
        matchLabels:
          name: ingress-nginx
    ports:
    - protocol: TCP
      port: 8080
  egress:
  - to:
    - podSelector:
        matchLabels:
          app: postgres
    ports:
    - protocol: TCP
      port: 5432
  - to:
    - podSelector:
        matchLabels:
          app: redis
    ports:
    - protocol: TCP
      port: 6379
  - ports:
    - port: 53
      protocol: UDP
    - port: 53
      protocol: TCP
```

### RBAC Configuration

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: tusk-app-sa
  namespace: default
---
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  name: tusk-app-role
rules:
- apiGroups: [""]
  resources: ["configmaps", "secrets"]
  verbs: ["get", "list", "watch"]
- apiGroups: [""]
  resources: ["pods", "services"]
  verbs: ["get", "list", "watch"]
---
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  name: tusk-app-role-binding
subjects:
- kind: ServiceAccount
  name: tusk-app-sa
  namespace: default
roleRef:
  kind: Role
  name: tusk-app-role
  apiGroup: rbac.authorization.k8s.io
```

## 🔄 CI/CD Pipeline

### GitHub Actions

```yaml
name: Deploy TuskLang Application

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Set up JDK 17
      uses: actions/setup-java@v3
      with:
        java-version: '17'
        distribution: 'temurin'
    
    - name: Cache Maven packages
      uses: actions/cache@v3
      with:
        path: ~/.m2
        key: ${{ runner.os }}-m2-${{ hashFiles('**/pom.xml') }}
        restore-keys: ${{ runner.os }}-m2
    
    - name: Run tests
      run: mvn test
    
    - name: Validate TuskLang config
      run: java -jar tusk.jar validate config.tsk

  build:
    needs: test
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Set up JDK 17
      uses: actions/setup-java@v3
      with:
        java-version: '17'
        distribution: 'temurin'
    
    - name: Build application
      run: mvn clean package -DskipTests
    
    - name: Build Docker image
      run: docker build -t tusk-app:${{ github.sha }} .
    
    - name: Push to registry
      run: |
        echo ${{ secrets.REGISTRY_PASSWORD }} | docker login -u ${{ secrets.REGISTRY_USERNAME }} --password-stdin
        docker tag tusk-app:${{ github.sha }} ${{ secrets.REGISTRY }}/tusk-app:${{ github.sha }}
        docker push ${{ secrets.REGISTRY }}/tusk-app:${{ github.sha }}

  deploy:
    needs: build
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    steps:
    - uses: actions/checkout@v3
    
    - name: Set up kubectl
      uses: azure/setup-kubectl@v3
    
    - name: Configure kubectl
      run: |
        echo "${{ secrets.KUBE_CONFIG }}" | base64 -d > kubeconfig
        export KUBECONFIG=kubeconfig
    
    - name: Deploy to Kubernetes
      run: |
        kubectl set image deployment/tusk-app app=${{ secrets.REGISTRY }}/tusk-app:${{ github.sha }}
        kubectl rollout status deployment/tusk-app
```

### Jenkins Pipeline

```groovy
pipeline {
    agent any
    
    environment {
        DOCKER_REGISTRY = 'registry.example.com'
        IMAGE_NAME = 'tusk-app'
        KUBE_NAMESPACE = 'production'
    }
    
    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }
        
        stage('Test') {
            steps {
                sh 'mvn test'
                sh 'java -jar tusk.jar validate config.tsk'
            }
        }
        
        stage('Build') {
            steps {
                sh 'mvn clean package -DskipTests'
                sh 'docker build -t ${DOCKER_REGISTRY}/${IMAGE_NAME}:${BUILD_NUMBER} .'
            }
        }
        
        stage('Push') {
            steps {
                withCredentials([usernamePassword(credentialsId: 'docker-registry', usernameVariable: 'USERNAME', passwordVariable: 'PASSWORD')]) {
                    sh 'echo $PASSWORD | docker login -u $USERNAME --password-stdin ${DOCKER_REGISTRY}'
                    sh 'docker push ${DOCKER_REGISTRY}/${IMAGE_NAME}:${BUILD_NUMBER}'
                }
            }
        }
        
        stage('Deploy') {
            steps {
                withCredentials([file(credentialsId: 'kubeconfig', variable: 'KUBECONFIG')]) {
                    sh 'kubectl set image deployment/tusk-app app=${DOCKER_REGISTRY}/${IMAGE_NAME}:${BUILD_NUMBER} -n ${KUBE_NAMESPACE}'
                    sh 'kubectl rollout status deployment/tusk-app -n ${KUBE_NAMESPACE}'
                }
            }
        }
    }
    
    post {
        always {
            cleanWs()
        }
        success {
            echo 'Deployment successful!'
        }
        failure {
            echo 'Deployment failed!'
        }
    }
}
```

## 📈 Scaling Strategies

### Vertical Scaling

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tusk-app
spec:
  template:
    spec:
      containers:
      - name: app
        resources:
          requests:
            memory: "1Gi"
            cpu: "500m"
          limits:
            memory: "2Gi"
            cpu: "1000m"
        env:
        - name: JAVA_OPTS
          value: "-Xms512m -Xmx1g -XX:+UseG1GC"
```

### Horizontal Scaling

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: tusk-app-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: tusk-app
  minReplicas: 3
  maxReplicas: 20
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
  - type: Object
    object:
      metric:
        name: requests-per-second
      describedObject:
        apiVersion: networking.k8s.io/v1
        kind: Ingress
        name: tusk-app-ingress
      target:
        type: Value
        value: 1000
```

### Database Scaling

```yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: postgres
spec:
  serviceName: postgres
  replicas: 3
  selector:
    matchLabels:
      app: postgres
  template:
    metadata:
      labels:
        app: postgres
    spec:
      containers:
      - name: postgres
        image: postgres:15
        env:
        - name: POSTGRES_DB
          value: myapp
        - name: POSTGRES_USER
          value: postgres
        - name: POSTGRES_PASSWORD
          valueFrom:
            secretKeyRef:
              name: postgres-secret
              key: password
        ports:
        - containerPort: 5432
        volumeMounts:
        - name: postgres-data
          mountPath: /var/lib/postgresql/data
  volumeClaimTemplates:
  - metadata:
      name: postgres-data
    spec:
      accessModes: ["ReadWriteOnce"]
      resources:
        requests:
          storage: 10Gi
```

## 🔧 Troubleshooting

### Common Issues

1. **Memory Issues**
```bash
# Check memory usage
kubectl top pods

# Check memory limits
kubectl describe pod tusk-app-pod

# Adjust memory settings
kubectl patch deployment tusk-app -p '{"spec":{"template":{"spec":{"containers":[{"name":"app","resources":{"limits":{"memory":"2Gi"}}}]}}}}'
```

2. **Database Connection Issues**
```bash
# Check database connectivity
kubectl exec -it tusk-app-pod -- curl -f http://localhost:8080/actuator/health

# Check database logs
kubectl logs postgres-pod

# Test database connection
kubectl exec -it tusk-app-pod -- java -jar tusk.jar query "SELECT 1" --database postgresql://postgres-service:5432/myapp
```

3. **Configuration Issues**
```bash
# Check configuration
kubectl get configmap app-config -o yaml

# Update configuration
kubectl patch configmap app-config -p '{"data":{"app_name":"Updated App"}}'

# Restart deployment
kubectl rollout restart deployment tusk-app
```

### Debug Commands

```bash
# Get pod logs
kubectl logs tusk-app-pod

# Follow logs
kubectl logs -f tusk-app-pod

# Execute commands in pod
kubectl exec -it tusk-app-pod -- /bin/sh

# Check resource usage
kubectl top pods
kubectl top nodes

# Check events
kubectl get events --sort-by=.metadata.creationTimestamp
```

## 📚 Next Steps

1. **Implement monitoring** - Set up Prometheus, Grafana, and alerting
2. **Add security** - Configure RBAC, network policies, and secrets management
3. **Optimize performance** - Tune JVM settings and database configuration
4. **Set up backup** - Implement database and configuration backups
5. **Plan disaster recovery** - Create recovery procedures and testing

---

**"We don't bow to any king"** - You now have complete mastery of production deployment for TuskLang Java applications! From Docker containerization to Kubernetes orchestration, you can deploy scalable, secure, and monitored applications in production environments. 