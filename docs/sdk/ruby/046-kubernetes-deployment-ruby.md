# Kubernetes Deployment with TuskLang and Ruby

## 🐳 **Orchestrate Your Applications at Scale**

TuskLang enables seamless Kubernetes deployment with Ruby applications, providing container orchestration, auto-scaling, and high availability. Deploy your applications with confidence using industry-standard containerization and orchestration practices.

## 🚀 **Quick Start: Containerization**

### Dockerfile Configuration

```dockerfile
# Dockerfile
FROM ruby:3.2-alpine

# Install system dependencies
RUN apk add --no-cache \
    build-base \
    postgresql-dev \
    redis \
    nodejs \
    yarn

# Set working directory
WORKDIR /app

# Install TuskLang CLI
RUN curl -sSL https://tusklang.org/tsk.sh | bash

# Copy Gemfile and install dependencies
COPY Gemfile Gemfile.lock ./
RUN bundle install --jobs 4 --retry 3

# Copy application code
COPY . .

# Create TuskLang configuration
COPY config/kubernetes.tsk /app/config/

# Precompile assets
RUN bundle exec rake assets:precompile RAILS_ENV=production

# Create non-root user
RUN addgroup -g 1000 -S app && \
    adduser -u 1000 -S app -G app
USER app

# Expose port
EXPOSE 3000

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:3000/health || exit 1

# Start application
CMD ["bundle", "exec", "rails", "server", "-b", "0.0.0.0", "-p", "3000"]
```

### Multi-stage Build

```dockerfile
# Dockerfile.multi
# Build stage
FROM ruby:3.2-alpine AS builder

RUN apk add --no-cache build-base postgresql-dev nodejs yarn

WORKDIR /app

COPY Gemfile Gemfile.lock ./
RUN bundle install --jobs 4 --retry 3 --without development test

COPY . .
RUN bundle exec rake assets:precompile RAILS_ENV=production

# Production stage
FROM ruby:3.2-alpine

RUN apk add --no-cache postgresql-dev redis

WORKDIR /app

# Install TuskLang CLI
RUN curl -sSL https://tusklang.org/tsk.sh | bash

# Copy built application
COPY --from=builder /app /app
COPY --from=builder /usr/local/bundle /usr/local/bundle

# Create non-root user
RUN addgroup -g 1000 -S app && \
    adduser -u 1000 -S app -G app
USER app

EXPOSE 3000

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:3000/health || exit 1

CMD ["bundle", "exec", "rails", "server", "-b", "0.0.0.0", "-p", "3000"]
```

## 🔧 **Kubernetes Configuration**

### Namespace Configuration

```yaml
# k8s/namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: tusklang-apps
  labels:
    name: tusklang-apps
    environment: production
```

### ConfigMap for TuskLang

```yaml
# k8s/configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: tusklang-config
  namespace: tusklang-apps
data:
  kubernetes.tsk: |
    [kubernetes]
    cluster_name: @env("CLUSTER_NAME", "production-cluster")
    namespace: @env("NAMESPACE", "tusklang-apps")
    environment: @env("ENVIRONMENT", "production")
    
    [database]
    host: @env("DATABASE_HOST", "postgres-service")
    port: @env("DATABASE_PORT", "5432")
    name: @env("DATABASE_NAME", "tusklang_production")
    pool_size: @env("DB_POOL_SIZE", "10")
    
    [redis]
    host: @env("REDIS_HOST", "redis-service")
    port: @env("REDIS_PORT", "6379")
    db: @env("REDIS_DB", "0")
    
    [monitoring]
    prometheus_enabled: @env("PROMETHEUS_ENABLED", "true")
    jaeger_enabled: @env("JAEGER_ENABLED", "true")
    log_level: @env("LOG_LEVEL", "INFO")
```

### Secret Management

```yaml
# k8s/secrets.yaml
apiVersion: v1
kind: Secret
metadata:
  name: app-secrets
  namespace: tusklang-apps
type: Opaque
data:
  database_password: <base64-encoded-password>
  redis_password: <base64-encoded-password>
  jwt_secret: <base64-encoded-jwt-secret>
  api_key: <base64-encoded-api-key>
```

### Deployment Configuration

```yaml
# k8s/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tusklang-app
  namespace: tusklang-apps
  labels:
    app: tusklang-app
    version: v1.0.0
spec:
  replicas: 3
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  selector:
    matchLabels:
      app: tusklang-app
  template:
    metadata:
      labels:
        app: tusklang-app
        version: v1.0.0
      annotations:
        prometheus.io/scrape: "true"
        prometheus.io/port: "3000"
        prometheus.io/path: "/metrics"
    spec:
      containers:
      - name: tusklang-app
        image: tusklang/app:latest
        ports:
        - containerPort: 3000
          name: http
        env:
        - name: RAILS_ENV
          value: "production"
        - name: DATABASE_HOST
          value: "postgres-service"
        - name: DATABASE_PASSWORD
          valueFrom:
            secretKeyRef:
              name: app-secrets
              key: database_password
        - name: REDIS_HOST
          value: "redis-service"
        - name: REDIS_PASSWORD
          valueFrom:
            secretKeyRef:
              name: app-secrets
              key: redis_password
        - name: JWT_SECRET
          valueFrom:
            secretKeyRef:
              name: app-secrets
              key: jwt_secret
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
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /ready
            port: 3000
          initialDelaySeconds: 5
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 3
        volumeMounts:
        - name: config-volume
          mountPath: /app/config
        - name: tmp-volume
          mountPath: /app/tmp
      volumes:
      - name: config-volume
        configMap:
          name: tusklang-config
      - name: tmp-volume
        emptyDir: {}
      imagePullSecrets:
      - name: registry-secret
```

### Service Configuration

```yaml
# k8s/service.yaml
apiVersion: v1
kind: Service
metadata:
  name: tusklang-app-service
  namespace: tusklang-apps
  labels:
    app: tusklang-app
spec:
  type: ClusterIP
  ports:
  - port: 80
    targetPort: 3000
    protocol: TCP
    name: http
  selector:
    app: tusklang-app
```

### Ingress Configuration

```yaml
# k8s/ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: tusklang-app-ingress
  namespace: tusklang-apps
  annotations:
    kubernetes.io/ingress.class: "nginx"
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    nginx.ingress.kubernetes.io/force-ssl-redirect: "true"
    nginx.ingress.kubernetes.io/proxy-body-size: "50m"
    nginx.ingress.kubernetes.io/proxy-read-timeout: "300"
    nginx.ingress.kubernetes.io/proxy-send-timeout: "300"
spec:
  tls:
  - hosts:
    - app.example.com
    secretName: app-tls
  rules:
  - host: app.example.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: tusklang-app-service
            port:
              number: 80
```

## 🔄 **Horizontal Pod Autoscaler**

```yaml
# k8s/hpa.yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: tusklang-app-hpa
  namespace: tusklang-apps
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: tusklang-app
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

## 🗄️ **Database Deployment**

### PostgreSQL StatefulSet

```yaml
# k8s/postgres-statefulset.yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: postgres
  namespace: tusklang-apps
spec:
  serviceName: postgres-service
  replicas: 1
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
        image: postgres:13
        ports:
        - containerPort: 5432
          name: postgres
        env:
        - name: POSTGRES_DB
          value: "tusklang_production"
        - name: POSTGRES_USER
          value: "tusklang"
        - name: POSTGRES_PASSWORD
          valueFrom:
            secretKeyRef:
              name: app-secrets
              key: database_password
        volumeMounts:
        - name: postgres-storage
          mountPath: /var/lib/postgresql/data
        resources:
          requests:
            memory: "1Gi"
            cpu: "500m"
          limits:
            memory: "2Gi"
            cpu: "1000m"
        livenessProbe:
          exec:
            command:
            - pg_isready
            - -U
            - tusklang
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          exec:
            command:
            - pg_isready
            - -U
            - tusklang
          initialDelaySeconds: 5
          periodSeconds: 5
  volumeClaimTemplates:
  - metadata:
      name: postgres-storage
    spec:
      accessModes: ["ReadWriteOnce"]
      resources:
        requests:
          storage: 10Gi
      storageClassName: "fast-ssd"
---
apiVersion: v1
kind: Service
metadata:
  name: postgres-service
  namespace: tusklang-apps
spec:
  ports:
  - port: 5432
    targetPort: 5432
  selector:
    app: postgres
  clusterIP: None
```

### Redis Deployment

```yaml
# k8s/redis-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: redis
  namespace: tusklang-apps
spec:
  replicas: 1
  selector:
    matchLabels:
      app: redis
  template:
    metadata:
      labels:
        app: redis
    spec:
      containers:
      - name: redis
        image: redis:6-alpine
        ports:
        - containerPort: 6379
          name: redis
        command:
        - redis-server
        - --requirepass
        - $(REDIS_PASSWORD)
        env:
        - name: REDIS_PASSWORD
          valueFrom:
            secretKeyRef:
              name: app-secrets
              key: redis_password
        volumeMounts:
        - name: redis-storage
          mountPath: /data
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          exec:
            command:
            - redis-cli
            - ping
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          exec:
            command:
            - redis-cli
            - ping
          initialDelaySeconds: 5
          periodSeconds: 5
      volumes:
      - name: redis-storage
        persistentVolumeClaim:
          claimName: redis-pvc
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: redis-pvc
  namespace: tusklang-apps
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 5Gi
  storageClassName: "fast-ssd"
---
apiVersion: v1
kind: Service
metadata:
  name: redis-service
  namespace: tusklang-apps
spec:
  ports:
  - port: 6379
    targetPort: 6379
  selector:
    app: redis
```

## 📊 **Monitoring and Logging**

### Prometheus Configuration

```yaml
# k8s/prometheus-configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: prometheus-config
  namespace: monitoring
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
      - job_name: 'kubernetes-pods'
        kubernetes_sd_configs:
          - role: pod
        relabel_configs:
          - source_labels: [__meta_kubernetes_pod_annotation_prometheus_io_scrape]
            action: keep
            regex: true
          - source_labels: [__meta_kubernetes_pod_annotation_prometheus_io_path]
            action: replace
            target_label: __metrics_path__
            regex: (.+)
          - source_labels: [__address__, __meta_kubernetes_pod_annotation_prometheus_io_port]
            action: replace
            regex: ([^:]+)(?::\d+)?;(\d+)
            replacement: $1:$2
            target_label: __address__
          - action: labelmap
            regex: __meta_kubernetes_pod_label_(.+)
          - source_labels: [__meta_kubernetes_namespace]
            action: replace
            target_label: kubernetes_namespace
          - source_labels: [__meta_kubernetes_pod_name]
            action: replace
            target_label: kubernetes_pod_name
```

### Grafana Dashboard

```yaml
# k8s/grafana-configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: grafana-dashboards
  namespace: monitoring
data:
  tusklang-app-dashboard.json: |
    {
      "dashboard": {
        "title": "TuskLang Application Metrics",
        "panels": [
          {
            "title": "Request Rate",
            "type": "graph",
            "targets": [
              {
                "expr": "rate(http_requests_total[5m])",
                "legendFormat": "{{pod}}"
              }
            ]
          },
          {
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
            "title": "Error Rate",
            "type": "graph",
            "targets": [
              {
                "expr": "rate(http_requests_total{status=~\"5..\"}[5m])",
                "legendFormat": "{{pod}}"
              }
            ]
          }
        ]
      }
    }
```

## 🔐 **Security Configuration**

### Network Policies

```yaml
# k8s/network-policy.yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: tusklang-app-network-policy
  namespace: tusklang-apps
spec:
  podSelector:
    matchLabels:
      app: tusklang-app
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
      port: 3000
  - from:
    - podSelector:
        matchLabels:
          app: tusklang-app
    ports:
    - protocol: TCP
      port: 3000
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
  - to: []
    ports:
    - protocol: TCP
      port: 53
    - protocol: UDP
      port: 53
```

### Pod Security Policies

```yaml
# k8s/pod-security-policy.yaml
apiVersion: policy/v1beta1
kind: PodSecurityPolicy
metadata:
  name: tusklang-app-psp
spec:
  privileged: false
  allowPrivilegeEscalation: false
  requiredDropCapabilities:
  - ALL
  volumes:
  - 'configMap'
  - 'emptyDir'
  - 'projected'
  - 'secret'
  - 'downwardAPI'
  - 'persistentVolumeClaim'
  hostNetwork: false
  hostIPC: false
  hostPID: false
  runAsUser:
    rule: 'MustRunAsNonRoot'
  seLinux:
    rule: 'RunAsAny'
  supplementalGroups:
    rule: 'MustRunAs'
    ranges:
    - min: 1
      max: 65535
  fsGroup:
    rule: 'MustRunAs'
    ranges:
    - min: 1
      max: 65535
  readOnlyRootFilesystem: true
```

## 🚀 **Deployment Strategies**

### Blue-Green Deployment

```yaml
# k8s/blue-green-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tusklang-app-blue
  namespace: tusklang-apps
  labels:
    app: tusklang-app
    version: blue
spec:
  replicas: 3
  selector:
    matchLabels:
      app: tusklang-app
      version: blue
  template:
    metadata:
      labels:
        app: tusklang-app
        version: blue
    spec:
      containers:
      - name: tusklang-app
        image: tusklang/app:blue
        ports:
        - containerPort: 3000
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tusklang-app-green
  namespace: tusklang-apps
  labels:
    app: tusklang-app
    version: green
spec:
  replicas: 0
  selector:
    matchLabels:
      app: tusklang-app
      version: green
  template:
    metadata:
      labels:
        app: tusklang-app
        version: green
    spec:
      containers:
      - name: tusklang-app
        image: tusklang/app:green
        ports:
        - containerPort: 3000
---
apiVersion: v1
kind: Service
metadata:
  name: tusklang-app-service
  namespace: tusklang-apps
spec:
  ports:
  - port: 80
    targetPort: 3000
  selector:
    app: tusklang-app
    version: blue
```

### Canary Deployment

```yaml
# k8s/canary-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tusklang-app-stable
  namespace: tusklang-apps
spec:
  replicas: 9
  selector:
    matchLabels:
      app: tusklang-app
      track: stable
  template:
    metadata:
      labels:
        app: tusklang-app
        track: stable
    spec:
      containers:
      - name: tusklang-app
        image: tusklang/app:stable
        ports:
        - containerPort: 3000
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tusklang-app-canary
  namespace: tusklang-apps
spec:
  replicas: 1
  selector:
    matchLabels:
      app: tusklang-app
      track: canary
  template:
    metadata:
      labels:
        app: tusklang-app
        track: canary
    spec:
      containers:
      - name: tusklang-app
        image: tusklang/app:canary
        ports:
        - containerPort: 3000
```

## 🔄 **Job and CronJob Configuration**

### Database Migration Job

```yaml
# k8s/migration-job.yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: db-migration
  namespace: tusklang-apps
spec:
  template:
    spec:
      containers:
      - name: migration
        image: tusklang/app:latest
        command: ["bundle", "exec", "rails", "db:migrate"]
        env:
        - name: RAILS_ENV
          value: "production"
        - name: DATABASE_HOST
          value: "postgres-service"
        - name: DATABASE_PASSWORD
          valueFrom:
            secretKeyRef:
              name: app-secrets
              key: database_password
      restartPolicy: Never
  backoffLimit: 3
```

### Scheduled Backup Job

```yaml
# k8s/backup-cronjob.yaml
apiVersion: batch/v1
kind: CronJob
metadata:
  name: db-backup
  namespace: tusklang-apps
spec:
  schedule: "0 2 * * *"  # Daily at 2 AM
  jobTemplate:
    spec:
      template:
        spec:
          containers:
          - name: backup
            image: tusklang/app:latest
            command: ["bundle", "exec", "rails", "db:backup"]
            env:
            - name: RAILS_ENV
              value: "production"
            - name: DATABASE_HOST
              value: "postgres-service"
            - name: DATABASE_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: app-secrets
                  key: database_password
          restartPolicy: OnFailure
```

## 📊 **Resource Management**

### Resource Quotas

```yaml
# k8s/resource-quota.yaml
apiVersion: v1
kind: ResourceQuota
metadata:
  name: tusklang-apps-quota
  namespace: tusklang-apps
spec:
  hard:
    requests.cpu: "4"
    requests.memory: 8Gi
    limits.cpu: "8"
    limits.memory: 16Gi
    persistentvolumeclaims: "10"
    services: "10"
    services.loadbalancers: "2"
```

### Limit Ranges

```yaml
# k8s/limit-range.yaml
apiVersion: v1
kind: LimitRange
metadata:
  name: tusklang-apps-limits
  namespace: tusklang-apps
spec:
  limits:
  - default:
      cpu: 500m
      memory: 512Mi
    defaultRequest:
      cpu: 250m
      memory: 256Mi
    type: Container
  - max:
      cpu: 2000m
      memory: 2Gi
    min:
      cpu: 100m
      memory: 128Mi
    type: Container
```

## 🚀 **Deployment Automation**

### Helm Chart

```yaml
# helm/tusklang-app/Chart.yaml
apiVersion: v2
name: tusklang-app
description: TuskLang Ruby Application
version: 1.0.0
appVersion: "1.0.0"
```

```yaml
# helm/tusklang-app/values.yaml
replicaCount: 3

image:
  repository: tusklang/app
  tag: latest
  pullPolicy: IfNotPresent

service:
  type: ClusterIP
  port: 80

ingress:
  enabled: true
  className: nginx
  hosts:
    - host: app.example.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: app-tls
      hosts:
        - app.example.com

resources:
  limits:
    cpu: 500m
    memory: 512Mi
  requests:
    cpu: 250m
    memory: 256Mi

autoscaling:
  enabled: true
  minReplicas: 3
  maxReplicas: 10
  targetCPUUtilizationPercentage: 70
  targetMemoryUtilizationPercentage: 80

database:
  enabled: true
  storage: 10Gi

redis:
  enabled: true
  storage: 5Gi
```

### ArgoCD Application

```yaml
# argocd/application.yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: tusklang-app
  namespace: argocd
spec:
  project: default
  source:
    repoURL: https://github.com/your-org/tusklang-app
    targetRevision: HEAD
    path: k8s
  destination:
    server: https://kubernetes.default.svc
    namespace: tusklang-apps
  syncPolicy:
    automated:
      prune: true
      selfHeal: true
    syncOptions:
    - CreateNamespace=true
    - PrunePropagationPolicy=foreground
    - PruneLast=true
```

## 🧪 **Testing and Validation**

### Kustomize Configuration

```yaml
# k8s/base/kustomization.yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization

resources:
- namespace.yaml
- configmap.yaml
- secrets.yaml
- deployment.yaml
- service.yaml
- ingress.yaml
- hpa.yaml

commonLabels:
  app: tusklang-app
  version: v1.0.0
```

```yaml
# k8s/overlays/development/kustomization.yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization

resources:
- ../../base

namespace: tusklang-apps-dev

patches:
- target:
    kind: Deployment
    name: tusklang-app
  patch: |-
    - op: replace
      path: /spec/replicas
      value: 1
    - op: replace
      path: /spec/template/spec/containers/0/image
      value: tusklang/app:development
```

## 🎯 **Best Practices**

### Configuration Management

```ruby
# config/kubernetes.tsk
[kubernetes]
cluster_name: @env("CLUSTER_NAME", "production-cluster")
namespace: @env("NAMESPACE", "tusklang-apps")
environment: @env("ENVIRONMENT", "production")
node_selector: @env("NODE_SELECTOR", "app=tusklang")

[scaling]
min_replicas: @env("MIN_REPLICAS", "3")
max_replicas: @env("MAX_REPLICAS", "10")
cpu_threshold: @env("CPU_THRESHOLD", "70")
memory_threshold: @env("MEMORY_THRESHOLD", "80")

[resources]
cpu_request: @env("CPU_REQUEST", "250m")
cpu_limit: @env("CPU_LIMIT", "500m")
memory_request: @env("MEMORY_REQUEST", "256Mi")
memory_limit: @env("MEMORY_LIMIT", "512Mi")

[monitoring]
prometheus_enabled: @env("PROMETHEUS_ENABLED", "true")
grafana_enabled: @env("GRAFANA_ENABLED", "true")
jaeger_enabled: @env("JAEGER_ENABLED", "true")
```

### Health Check Implementation

```ruby
# app/controllers/health_controller.rb
class HealthController < ApplicationController
  def check
    health_status = {
      status: 'healthy',
      timestamp: Time.now.iso8601,
      version: Rails.application.config.version,
      checks: {
        database: database_healthy?,
        redis: redis_healthy?,
        kubernetes: kubernetes_healthy?
      }
    }

    if health_status[:checks].values.all?
      render json: health_status, status: :ok
    else
      health_status[:status] = 'unhealthy'
      render json: health_status, status: :service_unavailable
    end
  end

  def ready
    if ready_for_traffic?
      render json: { status: 'ready' }, status: :ok
    else
      render json: { status: 'not_ready' }, status: :service_unavailable
    end
  end

  private

  def database_healthy?
    ActiveRecord::Base.connection.execute('SELECT 1')
    true
  rescue => e
    Rails.logger.error "Database health check failed: #{e.message}"
    false
  end

  def redis_healthy?
    Redis.new.ping == 'PONG'
  rescue => e
    Rails.logger.error "Redis health check failed: #{e.message}"
    false
  end

  def kubernetes_healthy?
    # Check Kubernetes-specific health indicators
    true
  end

  def ready_for_traffic?
    database_healthy? && redis_healthy?
  end
end
```

## 🎯 **Summary**

This comprehensive guide covers Kubernetes deployment with TuskLang and Ruby, including:

- **Containerization**: Dockerfile configuration and multi-stage builds
- **Kubernetes Resources**: Deployments, Services, Ingress, and ConfigMaps
- **Scaling**: Horizontal Pod Autoscaler and resource management
- **Database**: PostgreSQL StatefulSet and Redis deployment
- **Monitoring**: Prometheus, Grafana, and health checks
- **Security**: Network policies and Pod Security Policies
- **Deployment Strategies**: Blue-green and canary deployments
- **Automation**: Helm charts and ArgoCD integration
- **Testing**: Kustomize configuration and validation
- **Best Practices**: Configuration management and health checks

The Kubernetes deployment with TuskLang provides a robust, scalable platform for running Ruby applications with high availability, automatic scaling, and comprehensive monitoring capabilities. 