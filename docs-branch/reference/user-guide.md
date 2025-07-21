# TuskLang Cloud-Native Integration User Guide

## Table of Contents

1. [Overview](#overview)
2. [Quick Start](#quick-start)
3. [Installation](#installation)
4. [Configuration](#configuration)
5. [Usage Examples](#usage-examples)
6. [Monitoring](#monitoring)
7. [Troubleshooting](#troubleshooting)
8. [Advanced Features](#advanced-features)
9. [API Reference](#api-reference)
10. [Best Practices](#best-practices)

## Overview

The TuskLang Cloud-Native Integration provides a comprehensive Kubernetes operator that enables seamless deployment and management of TuskLang configurations across multiple cloud providers, service meshes, and observability platforms.

### Key Features

- **Multi-Cloud Support**: AWS, Google Cloud Platform, and Azure integration
- **Service Mesh Integration**: Istio, Linkerd, and Consul support
- **Observability**: OpenTelemetry, Prometheus, and Grafana integration
- **GitOps**: ArgoCD and Flux integration for declarative deployments
- **Container Management**: Docker build, push, and pull operations
- **Serverless**: AWS Lambda, GCP Cloud Functions, and Azure Functions support
- **Security**: Comprehensive RBAC, network policies, and secret management

## Quick Start

### Prerequisites

- Kubernetes cluster (1.24+)
- Helm 3.x
- kubectl configured
- Cloud provider credentials (optional)

### Installation

```bash
# Add the Helm repository
helm repo add tusklang https://charts.tuskt.sk
helm repo update

# Install the operator
helm install tusk-operator tusklang/tusk-operator \
  --namespace tusk-system \
  --create-namespace
```

### Create Your First Configuration

```yaml
apiVersion: tusklang.io/v1alpha1
kind: TuskConfig
metadata:
  name: my-app-config
  namespace: default
spec:
  config:
    application:
      name: my-app
      version: "1.0.0"
    database:
      host: "localhost"
      port: 5432
      name: "myapp"
    api:
      port: 8080
      cors:
        enabled: true
        origins: ["*"]
  
  cloudProvider:
    aws:
      region: "us-east-1"
      services:
        s3:
          bucket: "my-app-storage"
        secretsManager:
          region: "us-east-1"
  
  observability:
    metrics:
      enabled: true
      port: 9090
    tracing:
      enabled: true
      provider: "jaeger"
      endpoint: "http://jaeger:14268"
  
  serviceMesh:
    istio:
      namespace: "istio-system"
      virtualService:
        enabled: true
        hosts: ["my-app.example.com"]
```

Apply the configuration:

```bash
kubectl apply -f my-app-config.yaml
```

## Installation

### Standard Installation

```bash
# Install with default settings
helm install tusk-operator tusklang/tusk-operator \
  --namespace tusk-system \
  --create-namespace
```

### Production Installation

```bash
# Install with production settings
helm install tusk-operator tusklang/tusk-operator \
  --namespace tusk-system \
  --create-namespace \
  --set operator.replicas=3 \
  --set operator.resources.limits.cpu=1000m \
  --set operator.resources.limits.memory=2Gi \
  --set observability.metrics.enabled=true \
  --set observability.tracing.enabled=true \
  --set serviceMesh.istio.enabled=true \
  --set cloudProviders.aws.enabled=true
```

### High Availability Installation

```bash
# Install with high availability
helm install tusk-operator tusklang/tusk-operator \
  --namespace tusk-system \
  --create-namespace \
  --set operator.replicas=3 \
  --set highAvailability.enabled=true \
  --set highAvailability.podDisruptionBudget.enabled=true \
  --set highAvailability.horizontalPodAutoscaler.enabled=true \
  --set highAvailability.horizontalPodAutoscaler.minReplicas=3 \
  --set highAvailability.horizontalPodAutoscaler.maxReplicas=10
```

## Configuration

### Basic Configuration

The TuskLang operator supports extensive configuration through Helm values:

```yaml
# values.yaml
operator:
  replicas: 3
  image:
    repository: tusklang/tusk-operator
    tag: "2.0.1"
  
  resources:
    limits:
      cpu: 1000m
      memory: 2Gi
    requests:
      cpu: 500m
      memory: 1Gi

observability:
  metrics:
    enabled: true
    port: 8080
  tracing:
    enabled: true
    provider: "jaeger"
    endpoint: "http://jaeger:14268"

cloudProviders:
  aws:
    enabled: true
    region: "us-east-1"
  gcp:
    enabled: false
  azure:
    enabled: false

serviceMesh:
  istio:
    enabled: true
    namespace: "istio-system"
```

### Cloud Provider Configuration

#### AWS Configuration

```yaml
cloudProviders:
  aws:
    enabled: true
    region: "us-east-1"
    credentials:
      accessKeyId: "AKIA..."
      secretAccessKey: "..."
      roleArn: "arn:aws:iam::123456789012:role/tusk-operator"
    services:
      s3:
        bucket: "my-app-storage"
        prefix: "configs/"
      secretsManager:
        region: "us-east-1"
        prefix: "tusklang/"
      parameterStore:
        region: "us-east-1"
        prefix: "/tusklang/"
      lambda:
        region: "us-east-1"
        functionName: "tusk-processor"
      ecr:
        region: "us-east-1"
        repository: "tusk-images"
```

#### GCP Configuration

```yaml
cloudProviders:
  gcp:
    enabled: true
    project: "my-gcp-project"
    region: "us-central1"
    credentials:
      serviceAccountKey: "..."
    services:
      secretManager:
        project: "my-gcp-project"
        prefix: "tusklang/"
      storage:
        bucket: "my-app-storage"
        prefix: "configs/"
      cloudRun:
        region: "us-central1"
```

#### Azure Configuration

```yaml
cloudProviders:
  azure:
    enabled: true
    subscription: "my-subscription"
    resourceGroup: "my-resource-group"
    location: "eastus"
    credentials:
      tenantId: "..."
      clientId: "..."
      clientSecret: "..."
    services:
      keyVault:
        vault: "my-key-vault"
        prefix: "tusklang/"
      blobStorage:
        account: "mystorageaccount"
        container: "configs"
```

### Service Mesh Configuration

#### Istio Configuration

```yaml
serviceMesh:
  istio:
    enabled: true
    namespace: "istio-system"
    virtualService:
      enabled: true
      hosts: ["my-app.example.com"]
      gateways: ["my-gateway"]
    destinationRule:
      enabled: true
      host: "my-app-service"
      trafficPolicy:
        loadBalancer:
          simple: "ROUND_ROBIN"
    peerAuthentication:
      enabled: true
      mtls:
        mode: "PERMISSIVE"
```

#### Linkerd Configuration

```yaml
serviceMesh:
  linkerd:
    enabled: true
    namespace: "linkerd"
```

#### Consul Configuration

```yaml
serviceMesh:
  consul:
    enabled: true
    namespace: "consul"
```

### Observability Configuration

#### Metrics Configuration

```yaml
observability:
  metrics:
    enabled: true
    port: 8080
    path: "/metrics"
    serviceMonitor:
      enabled: true
      interval: "30s"
```

#### Tracing Configuration

```yaml
observability:
  tracing:
    enabled: true
    provider: "jaeger"  # jaeger, zipkin, otel
    endpoint: "http://jaeger:14268"
    samplingRate: 0.1
```

#### Logging Configuration

```yaml
observability:
  logging:
    level: "info"
    format: "json"
    structured: true
```

### GitOps Configuration

#### ArgoCD Configuration

```yaml
gitOps:
  argocd:
    enabled: true
    namespace: "argocd"
    apiUrl: "http://argocd-server:8080"
    authToken: "..."
    applications:
      - name: "my-app"
        project: "default"
        source:
          repoURL: "https://github.com/my-org/my-repo"
          targetRevision: "main"
          path: "k8s/"
        destination:
          server: "https://kubernetes.default.svc"
          namespace: "default"
        syncPolicy:
          automated:
            prune: true
            selfHeal: true
```

#### Flux Configuration

```yaml
gitOps:
  flux:
    enabled: true
    namespace: "flux-system"
    repositories:
      - name: "my-repo"
        url: "https://github.com/my-org/my-repo"
        ref:
          branch: "main"
    kustomizations:
      - name: "my-app"
        path: "k8s/"
        sourceRef:
          name: "my-repo"
        targetNamespace: "default"
```

## Usage Examples

### Basic TuskConfig Example

```yaml
apiVersion: tusklang.io/v1alpha1
kind: TuskConfig
metadata:
  name: simple-app
  namespace: default
spec:
  config:
    app:
      name: "simple-app"
      version: "1.0.0"
      port: 8080
    database:
      type: "postgres"
      host: "postgres-service"
      port: 5432
      name: "simpleapp"
  
  observability:
    metrics:
      enabled: true
      port: 9090
    logging:
      level: "info"
      format: "json"
```

### Advanced TuskConfig Example

```yaml
apiVersion: tusklang.io/v1alpha1
kind: TuskConfig
metadata:
  name: production-app
  namespace: production
spec:
  config:
    application:
      name: "production-app"
      version: "2.1.0"
      environment: "production"
    
    database:
      type: "postgres"
      host: "prod-postgres"
      port: 5432
      name: "prodapp"
      sslMode: "require"
      poolSize: 20
    
    cache:
      type: "redis"
      host: "prod-redis"
      port: 6379
      database: 0
    
    api:
      port: 8080
      cors:
        enabled: true
        origins: ["https://app.example.com"]
      rateLimit:
        enabled: true
        requestsPerMinute: 1000
    
    security:
      jwt:
        secret: "prod-jwt-secret"
        expiration: "24h"
      encryption:
        algorithm: "AES-256"
        key: "prod-encryption-key"
  
  cloudProvider:
    aws:
      region: "us-west-2"
      services:
        s3:
          bucket: "prod-app-storage"
          prefix: "data/"
        secretsManager:
          region: "us-west-2"
          prefix: "prod/"
        parameterStore:
          region: "us-west-2"
          prefix: "/prod/"
        lambda:
          region: "us-west-2"
          functionName: "prod-processor"
          timeout: 30
          memorySize: 512
    
    gcp:
      project: "prod-project"
      region: "us-west1"
      services:
        secretManager:
          project: "prod-project"
          prefix: "prod/"
        storage:
          bucket: "prod-app-storage"
          prefix: "data/"
  
  serviceMesh:
    istio:
      namespace: "istio-system"
      virtualService:
        enabled: true
        hosts: ["app.example.com"]
        gateways: ["app-gateway"]
        routes:
          - match:
              - uri:
                  prefix: "/api"
            route:
              - destination:
                  host: "app-service"
                  port:
                    number: 8080
      destinationRule:
        enabled: true
        host: "app-service"
        trafficPolicy:
          loadBalancer:
            simple: "LEAST_CONN"
          connectionPool:
            tcp:
              maxConnections: 100
            http:
              http1MaxPendingRequests: 1000
              maxRequestsPerConnection: 10
      peerAuthentication:
        enabled: true
        mtls:
          mode: "STRICT"
  
  observability:
    metrics:
      enabled: true
      port: 9090
      path: "/metrics"
      serviceMonitor:
        enabled: true
        interval: "30s"
    tracing:
      enabled: true
      provider: "jaeger"
      endpoint: "http://jaeger:14268"
      samplingRate: 0.1
    logging:
      level: "info"
      format: "json"
      structured: true
      outputs:
        - stdout
        - elasticsearch
        - splunk
  
  gitOps:
    argocd:
      enabled: true
      namespace: "argocd"
      apiUrl: "http://argocd-server:8080"
      applications:
        - name: "production-app"
          project: "production"
          source:
            repoURL: "https://github.com/my-org/prod-repo"
            targetRevision: "main"
            path: "k8s/production/"
          destination:
            server: "https://kubernetes.default.svc"
            namespace: "production"
          syncPolicy:
            automated:
              prune: true
              selfHeal: true
            syncOptions:
              - "CreateNamespace=true"
              - "PrunePropagationPolicy=foreground"
  
  helm:
    enabled: true
    charts:
      - name: "app-dependencies"
        repository: "https://charts.example.com"
        version: "1.2.0"
        namespace: "production"
        values:
          postgres:
            enabled: true
            persistence:
              enabled: true
              size: 100Gi
          redis:
            enabled: true
            persistence:
              enabled: true
              size: 50Gi
  
  containers:
    enabled: true
    images:
      - name: "app-image"
        dockerfile: "Dockerfile"
        buildContext: "."
        registry:
          url: "123456789012.dkr.ecr.us-west-2.amazonaws.com"
          repository: "my-app"
        tags:
          - "latest"
          - "v2.1.0"
        buildArgs:
          BUILD_ENV: "production"
        labels:
          version: "2.1.0"
          environment: "production"
  
  serverless:
    awsLambda:
      enabled: true
      region: "us-west-2"
      functions:
        - name: "data-processor"
          runtime: "nodejs18.x"
          handler: "index.handler"
          timeout: 30
          memorySize: 512
          environment:
            NODE_ENV: "production"
            DB_HOST: "prod-postgres"
          vpcConfig:
            subnetIds: ["subnet-123", "subnet-456"]
            securityGroupIds: ["sg-123"]
  
  highAvailability:
    enabled: true
    replicas: 3
    podDisruptionBudget:
      enabled: true
      minAvailable: 2
    horizontalPodAutoscaler:
      enabled: true
      minReplicas: 3
      maxReplicas: 10
      targetCPUUtilizationPercentage: 70
      targetMemoryUtilizationPercentage: 80
  
  database:
    enabled: true
    type: "postgres"
    host: "prod-postgres"
    port: 5432
    name: "prodapp"
    username: "produser"
    password: "prodpass"
    sslMode: "require"
  
  security:
    podSecurityStandards:
      level: "restricted"
      version: "v1.24"
    networkPolicies:
      enabled: true
      policies:
        - name: "app-network-policy"
          podSelector:
            matchLabels:
              app: "production-app"
          policyTypes:
            - Ingress
            - Egress
          ingress:
            - from:
                - namespaceSelector:
                    matchLabels:
                      name: "production"
              ports:
                - protocol: TCP
                  port: 8080
  
  monitoring:
    enabled: true
    prometheus:
      enabled: true
      retention: "30d"
      storage:
        type: "persistent"
        size: "100Gi"
    grafana:
      enabled: true
      adminPassword: "secure-password"
      dashboards:
        - name: "app-dashboard"
          url: "https://grafana.example.com/d/app-dashboard"
    alertmanager:
      enabled: true
      config: |
        global:
          slack_api_url: 'https://hooks.slack.com/services/...'
        route:
          group_by: ['alertname']
          group_wait: 10s
          group_interval: 10s
          repeat_interval: 1h
          receiver: 'slack-notifications'
        receivers:
          - name: 'slack-notifications'
            slack_configs:
              - channel: '#alerts'
                title: '{{ .GroupLabels.alertname }}'
                text: '{{ range .Alerts }}{{ .Annotations.summary }}{{ end }}'
  
  backup:
    enabled: true
    schedule: "0 2 * * *"
    retention: "90d"
    storage:
      type: "s3"
      bucket: "prod-backups"
      prefix: "tusklang/"
  
  debug:
    enabled: false
    logLevel: "debug"
    debugEndpoints: false
    profiling: false
```

## Monitoring

### Prometheus Metrics

The TuskLang operator exposes comprehensive metrics:

```bash
# Get metrics
curl http://tusk-operator:8080/metrics

# Key metrics
tusk_operator_health_status
tusk_configs_total
tusk_reconciliations_total
tusk_reconciliation_duration_seconds
tusk_errors_total
tusk_aws_operations_total
tusk_gcp_operations_total
tusk_azure_operations_total
tusk_istio_operations_total
tusk_argocd_sync_status
tusk_helm_installations_total
```

### Grafana Dashboard

Import the provided Prometheus dashboard for comprehensive monitoring:

```bash
# Import dashboard
kubectl apply -f sdk/rust/deployments/monitoring/prometheus-dashboard.json
```

### Alerting Rules

```yaml
apiVersion: monitoring.coreos.com/v1
kind: PrometheusRule
metadata:
  name: tusk-operator-alerts
  namespace: monitoring
spec:
  groups:
  - name: tusk-operator
    rules:
    - alert: TuskOperatorDown
      expr: tusk_operator_health_status == 0
      for: 1m
      labels:
        severity: critical
      annotations:
        summary: "TuskLang operator is down"
        description: "The TuskLang operator has been down for more than 1 minute"
    
    - alert: HighErrorRate
      expr: rate(tusk_errors_total[5m]) > 0.1
      for: 2m
      labels:
        severity: warning
      annotations:
        summary: "High error rate detected"
        description: "Error rate is {{ $value }} errors per second"
    
    - alert: SlowReconciliation
      expr: histogram_quantile(0.95, rate(tusk_reconciliation_duration_seconds_bucket[5m])) > 30
      for: 5m
      labels:
        severity: warning
      annotations:
        summary: "Slow reconciliation detected"
        description: "95th percentile reconciliation time is {{ $value }} seconds"
```

## Troubleshooting

### Common Issues

#### Operator Not Starting

```bash
# Check operator logs
kubectl logs -n tusk-system deployment/tusk-operator

# Check operator status
kubectl get pods -n tusk-system
kubectl describe pod -n tusk-system -l app.kubernetes.io/name=tusk-operator
```

#### Configuration Not Reconciling

```bash
# Check TuskConfig status
kubectl get tuskconfig -A
kubectl describe tuskconfig my-config -n default

# Check events
kubectl get events -n default --sort-by='.lastTimestamp'
```

#### Cloud Provider Issues

```bash
# Check cloud provider credentials
kubectl get secret -n tusk-system

# Test cloud provider connectivity
kubectl exec -n tusk-system deployment/tusk-operator -- tusk test-cloud-provider
```

#### Service Mesh Issues

```bash
# Check Istio installation
kubectl get pods -n istio-system

# Check VirtualService status
kubectl get virtualservice -A
kubectl describe virtualservice my-vs -n default
```

### Debug Mode

Enable debug mode for detailed logging:

```yaml
debug:
  enabled: true
  logLevel: "debug"
  debugEndpoints: true
  profiling: true
```

### Health Checks

```bash
# Operator health
curl http://tusk-operator:8080/health

# Readiness check
curl http://tusk-operator:8080/ready

# Metrics endpoint
curl http://tusk-operator:8080/metrics
```

## Advanced Features

### Custom Resource Definitions

The operator creates custom resources for advanced configurations:

```yaml
apiVersion: tusklang.io/v1alpha1
kind: TuskConfig
metadata:
  name: advanced-config
spec:
  # ... configuration ...
  status:
    phase: "Running"
    conditions:
      - type: "Ready"
        status: "True"
        lastTransitionTime: "2025-01-23T15:30:00Z"
    observedGeneration: 1
    lastReconcileTime: "2025-01-23T15:30:00Z"
```

### Webhooks

Configure admission webhooks for validation:

```yaml
apiVersion: admissionregistration.k8s.io/v1
kind: ValidatingWebhookConfiguration
metadata:
  name: tusk-operator-webhook
webhooks:
  - name: tusklang.io
    clientConfig:
      service:
        namespace: tusk-system
        name: tusk-operator-webhook
        path: "/validate"
    rules:
      - apiGroups: ["tusklang.io"]
        apiVersions: ["v1alpha1"]
        operations: ["CREATE", "UPDATE"]
        resources: ["tuskconfigs"]
```

### Custom Metrics

Add custom metrics to your configurations:

```yaml
observability:
  metrics:
    enabled: true
    customMetrics:
      - name: "app_requests_total"
        help: "Total number of requests"
        type: "counter"
        labels: ["method", "endpoint", "status"]
      - name: "app_response_time_seconds"
        help: "Response time in seconds"
        type: "histogram"
        buckets: [0.1, 0.5, 1.0, 2.0, 5.0]
```

## API Reference

### TuskConfig CRD

```yaml
apiVersion: apiextensions.k8s.io/v1
kind: CustomResourceDefinition
metadata:
  name: tuskconfigs.tusklang.io
spec:
  group: tusklang.io
  names:
    kind: TuskConfig
    listKind: TuskConfigList
    plural: tuskconfigs
    singular: tuskconfig
  scope: Namespaced
  versions:
    - name: v1alpha1
      served: true
      storage: true
      schema:
        openAPIV3Schema:
          type: object
          properties:
            spec:
              type: object
              properties:
                config:
                  type: object
                cloudProvider:
                  type: object
                serviceMesh:
                  type: object
                observability:
                  type: object
                gitOps:
                  type: object
                helm:
                  type: object
                containers:
                  type: object
                serverless:
                  type: object
            status:
              type: object
              properties:
                phase:
                  type: string
                  enum: [Pending, Running, Succeeded, Failed]
                conditions:
                  type: array
                  items:
                    type: object
```

### REST API

The operator exposes a REST API for programmatic access:

```bash
# Get all configurations
curl http://tusk-operator:8080/api/v1/configs

# Get specific configuration
curl http://tusk-operator:8080/api/v1/configs/default/my-config

# Create configuration
curl -X POST http://tusk-operator:8080/api/v1/configs \
  -H "Content-Type: application/json" \
  -d @config.json

# Update configuration
curl -X PUT http://tusk-operator:8080/api/v1/configs/default/my-config \
  -H "Content-Type: application/json" \
  -d @updated-config.json

# Delete configuration
curl -X DELETE http://tusk-operator:8080/api/v1/configs/default/my-config
```

## Best Practices

### Security

1. **Use RBAC**: Configure appropriate roles and bindings
2. **Network Policies**: Restrict network access
3. **Secret Management**: Use external secrets for sensitive data
4. **Pod Security**: Enable pod security standards
5. **TLS**: Use TLS for all communications

### Performance

1. **Resource Limits**: Set appropriate CPU and memory limits
2. **Horizontal Scaling**: Enable HPA for automatic scaling
3. **Caching**: Use Redis for caching frequently accessed data
4. **Connection Pooling**: Configure database connection pools
5. **Monitoring**: Monitor performance metrics

### Reliability

1. **High Availability**: Deploy multiple replicas
2. **Health Checks**: Configure liveness and readiness probes
3. **Backup**: Enable automated backups
4. **Disaster Recovery**: Test recovery procedures
5. **Rolling Updates**: Use rolling update strategy

### Observability

1. **Metrics**: Enable comprehensive metrics collection
2. **Tracing**: Use distributed tracing for debugging
3. **Logging**: Implement structured logging
4. **Alerting**: Configure meaningful alerts
5. **Dashboards**: Create informative dashboards

### GitOps

1. **Declarative**: Use declarative configurations
2. **Version Control**: Store all configurations in Git
3. **Automation**: Automate deployment processes
4. **Rollback**: Enable easy rollback procedures
5. **Audit**: Maintain audit trails

---

For more information, visit [https://tuskt.sk](https://tuskt.sk) or contact support at support@cyberboost.com. 