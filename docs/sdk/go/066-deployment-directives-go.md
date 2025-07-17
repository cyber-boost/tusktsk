# Deployment Directives - Go

## 🎯 What Are Deployment Directives?

Deployment directives (`#deployment`) in TuskLang allow you to define deployment strategies, containerization, infrastructure configuration, and environment management directly in your configuration files.

```go
// Deployment directives define your entire deployment system
type DeploymentConfig struct {
    Strategies  map[string]string `tsk:"#deployment_strategies"`
    Containers  map[string]string `tsk:"#deployment_containers"`
    Infrastructure map[string]string `tsk:"#deployment_infrastructure"`
    Environments map[string]string `tsk:"#deployment_environments"`
}
```

## 🚀 Why Deployment Directives Matter

### Traditional Deployment Development
```go
// Old way - scattered across multiple files
func main() {
    // Deployment configuration scattered
    dockerfile := `FROM golang:1.21-alpine
WORKDIR /app
COPY . .
RUN go build -o main .
EXPOSE 8080
CMD ["./main"]`
    
    // Manual deployment steps
    // docker build -t myapp .
    // docker run -p 8080:8080 myapp
    // kubectl apply -f k8s/
}
```

### TuskLang Deployment Directives - Declarative & Dynamic
```tsk
# deployment.tsk - Complete deployment definition
deployment_strategies: #deployment("""
    blue_green -> Blue-green deployment
        strategy: "blue_green"
        health_check: "/health"
        rollback_threshold: 0.05
        switch_timeout: 30s
    
    rolling -> Rolling deployment
        strategy: "rolling"
        max_unavailable: 1
        max_surge: 2
        health_check: "/ready"
    
    canary -> Canary deployment
        strategy: "canary"
        initial_traffic: 0.1
        increment: 0.1
        interval: 5m
        max_traffic: 1.0
""")

deployment_containers: #deployment("""
    app_container -> Application container
        image: "myapp:latest"
        port: 8080
        resources:
            cpu: "500m"
            memory: "512Mi"
        health_check: "/health"
        readiness_check: "/ready"
    
    sidecar_container -> Sidecar container
        image: "nginx:alpine"
        port: 80
        resources:
            cpu: "100m"
            memory: "128Mi"
""")

deployment_infrastructure: #deployment("""
    kubernetes -> Kubernetes deployment
        namespace: "production"
        replicas: 3
        autoscaling:
            min: 2
            max: 10
            target_cpu: 70
        ingress:
            host: "api.example.com"
            tls: true
""")
```

## 📋 Deployment Directive Types

### 1. **Strategy Directives** (`#deployment_strategies`)
- Deployment strategy definitions
- Blue-green, rolling, canary deployments
- Health check configuration
- Rollback strategies

### 2. **Container Directives** (`#deployment_containers`)
- Container image configuration
- Resource requirements
- Health and readiness checks
- Sidecar containers

### 3. **Infrastructure Directives** (`#deployment_infrastructure`)
- Kubernetes configuration
- Cloud provider settings
- Load balancer configuration
- Auto-scaling rules

### 4. **Environment Directives** (`#deployment_environments`)
- Environment-specific settings
- Configuration management
- Secret management
- Feature flags

## 🔧 Basic Deployment Directive Syntax

### Simple Deployment Strategy
```tsk
# Basic deployment strategy
rolling_deploy: #deployment("rolling -> max_unavailable:1,max_surge:2")
```

### Deployment Strategy with Configuration
```tsk
# Deployment strategy with detailed configuration
blue_green_deploy: #deployment("""
    strategy: "blue_green"
    health_check: "/health"
    rollback_threshold: 0.05
    switch_timeout: 30s
    pre_deployment_hook: "backup_database"
    post_deployment_hook: "run_migrations"
""")
```

### Multiple Deployment Strategies
```tsk
# Multiple deployment strategies
deployment_strategies: #deployment("""
    production -> blue_green -> health_check:/health,rollback:0.05
    staging -> rolling -> max_unavailable:1,max_surge:2
    development -> canary -> initial:0.1,increment:0.1
""")
```

## 🎯 Go Integration Patterns

### Struct Tags for Deployment Directives
```go
type DeploymentConfig struct {
    // Deployment strategies
    Strategies string `tsk:"#deployment_strategies"`
    
    // Container configuration
    Containers string `tsk:"#deployment_containers"`
    
    // Infrastructure configuration
    Infrastructure string `tsk:"#deployment_infrastructure"`
    
    // Environment configuration
    Environments string `tsk:"#deployment_environments"`
}
```

### Deployment Application Setup
```go
package main

import (
    "github.com/tusklang/go-sdk"
    "github.com/gorilla/mux"
    "net/http"
)

func main() {
    // Load deployment configuration
    config := tusk.LoadConfig("deployment.tsk")
    
    var deploymentConfig DeploymentConfig
    config.Unmarshal(&deploymentConfig)
    
    // Create deployment system from directives
    deployment := tusk.NewDeploymentSystem(deploymentConfig)
    
    // Create router
    router := mux.NewRouter()
    
    // Apply deployment middleware
    tusk.ApplyDeploymentMiddleware(router, deployment)
    
    // Start server
    http.ListenAndServe(":8080", router)
}
```

### Deployment Handler Implementation
```go
package deployment

import (
    "context"
    "fmt"
    "net/http"
    "time"
    "k8s.io/client-go/kubernetes"
    "k8s.io/client-go/rest"
)

// Deployment manager
type DeploymentManager struct {
    clientset *kubernetes.Clientset
    config    DeploymentConfig
}

func NewDeploymentManager(config DeploymentConfig) (*DeploymentManager, error) {
    // Load Kubernetes config
    kubeConfig, err := rest.InClusterConfig()
    if err != nil {
        return nil, fmt.Errorf("failed to load kubeconfig: %v", err)
    }
    
    // Create clientset
    clientset, err := kubernetes.NewForConfig(kubeConfig)
    if err != nil {
        return nil, fmt.Errorf("failed to create clientset: %v", err)
    }
    
    return &DeploymentManager{
        clientset: clientset,
        config:    config,
    }, nil
}

// Blue-green deployment
func (dm *DeploymentManager) BlueGreenDeploy(ctx context.Context, appName, newImage string) error {
    // Get current deployment
    currentDeployment, err := dm.getCurrentDeployment(ctx, appName)
    if err != nil {
        return fmt.Errorf("failed to get current deployment: %v", err)
    }
    
    // Create new deployment (green)
    greenDeployment := dm.createGreenDeployment(currentDeployment, newImage)
    if err := dm.createDeployment(ctx, greenDeployment); err != nil {
        return fmt.Errorf("failed to create green deployment: %v", err)
    }
    
    // Wait for green deployment to be ready
    if err := dm.waitForDeploymentReady(ctx, greenDeployment.Name); err != nil {
        return fmt.Errorf("green deployment not ready: %v", err)
    }
    
    // Run health checks
    if err := dm.runHealthChecks(ctx, greenDeployment); err != nil {
        return fmt.Errorf("health checks failed: %v", err)
    }
    
    // Switch traffic to green deployment
    if err := dm.switchTraffic(ctx, appName, greenDeployment.Name); err != nil {
        return fmt.Errorf("failed to switch traffic: %v", err)
    }
    
    // Scale down blue deployment
    if err := dm.scaleDownDeployment(ctx, currentDeployment.Name); err != nil {
        return fmt.Errorf("failed to scale down blue deployment: %v", err)
    }
    
    return nil
}

// Rolling deployment
func (dm *DeploymentManager) RollingDeploy(ctx context.Context, appName, newImage string) error {
    // Update deployment image
    deployment, err := dm.getDeployment(ctx, appName)
    if err != nil {
        return fmt.Errorf("failed to get deployment: %v", err)
    }
    
    // Update container image
    deployment.Spec.Template.Spec.Containers[0].Image = newImage
    
    // Apply rolling update
    if err := dm.updateDeployment(ctx, deployment); err != nil {
        return fmt.Errorf("failed to update deployment: %v", err)
    }
    
    // Wait for rollout to complete
    if err := dm.waitForRollout(ctx, appName); err != nil {
        return fmt.Errorf("rollout failed: %v", err)
    }
    
    return nil
}

// Canary deployment
func (dm *DeploymentManager) CanaryDeploy(ctx context.Context, appName, newImage string) error {
    // Create canary deployment
    canaryDeployment := dm.createCanaryDeployment(appName, newImage)
    if err := dm.createDeployment(ctx, canaryDeployment); err != nil {
        return fmt.Errorf("failed to create canary deployment: %v", err)
    }
    
    // Gradually increase traffic
    trafficPercentages := []float64{0.1, 0.25, 0.5, 0.75, 1.0}
    
    for _, percentage := range trafficPercentages {
        // Update traffic split
        if err := dm.updateTrafficSplit(ctx, appName, canaryDeployment.Name, percentage); err != nil {
            return fmt.Errorf("failed to update traffic split: %v", err)
        }
        
        // Wait for interval
        time.Sleep(5 * time.Minute)
        
        // Check metrics
        if err := dm.checkCanaryMetrics(ctx, canaryDeployment.Name); err != nil {
            return fmt.Errorf("canary metrics check failed: %v", err)
        }
    }
    
    // Promote canary to stable
    if err := dm.promoteCanary(ctx, appName, canaryDeployment.Name); err != nil {
        return fmt.Errorf("failed to promote canary: %v", err)
    }
    
    return nil
}

// Health check handler
func (dm *DeploymentManager) HealthCheck(w http.ResponseWriter, r *http.Request) {
    // Perform health checks
    checks := map[string]bool{
        "database": dm.checkDatabaseHealth(),
        "redis":    dm.checkRedisHealth(),
        "external_api": dm.checkExternalAPIHealth(),
    }
    
    // Check if all health checks pass
    allHealthy := true
    for name, healthy := range checks {
        if !healthy {
            allHealthy = false
            log.Printf("Health check failed: %s", name)
        }
    }
    
    if allHealthy {
        w.WriteHeader(http.StatusOK)
        w.Write([]byte("healthy"))
    } else {
        w.WriteHeader(http.StatusServiceUnavailable)
        w.Write([]byte("unhealthy"))
    }
}

// Readiness check handler
func (dm *DeploymentManager) ReadinessCheck(w http.ResponseWriter, r *http.Request) {
    // Perform readiness checks
    checks := map[string]bool{
        "database_connection": dm.checkDatabaseConnection(),
        "cache_connection":    dm.checkCacheConnection(),
        "configuration_loaded": dm.checkConfigurationLoaded(),
    }
    
    // Check if all readiness checks pass
    allReady := true
    for name, ready := range checks {
        if !ready {
            allReady = false
            log.Printf("Readiness check failed: %s", name)
        }
    }
    
    if allReady {
        w.WriteHeader(http.StatusOK)
        w.Write([]byte("ready"))
    } else {
        w.WriteHeader(http.StatusServiceUnavailable)
        w.Write([]byte("not ready"))
    }
}

// Helper methods
func (dm *DeploymentManager) getCurrentDeployment(ctx context.Context, appName string) (*appsv1.Deployment, error) {
    return dm.clientset.AppsV1().Deployments("default").Get(ctx, appName, metav1.GetOptions{})
}

func (dm *DeploymentManager) createGreenDeployment(current *appsv1.Deployment, newImage string) *appsv1.Deployment {
    green := current.DeepCopy()
    green.Name = fmt.Sprintf("%s-green", current.Name)
    green.Spec.Template.Spec.Containers[0].Image = newImage
    return green
}

func (dm *DeploymentManager) waitForDeploymentReady(ctx context.Context, deploymentName string) error {
    for {
        deployment, err := dm.getDeployment(ctx, deploymentName)
        if err != nil {
            return err
        }
        
        if deployment.Status.ReadyReplicas == *deployment.Spec.Replicas {
            return nil
        }
        
        time.Sleep(5 * time.Second)
    }
}

func (dm *DeploymentManager) runHealthChecks(ctx context.Context, deployment *appsv1.Deployment) error {
    // Implementation depends on your health check logic
    return nil
}

func (dm *DeploymentManager) switchTraffic(ctx context.Context, appName, greenDeploymentName string) error {
    // Implementation depends on your traffic management (e.g., Istio, nginx)
    return nil
}

func (dm *DeploymentManager) scaleDownDeployment(ctx context.Context, deploymentName string) error {
    deployment, err := dm.getDeployment(ctx, deploymentName)
    if err != nil {
        return err
    }
    
    replicas := int32(0)
    deployment.Spec.Replicas = &replicas
    
    return dm.updateDeployment(ctx, deployment)
}
```

## 🔄 Advanced Deployment Patterns

### Multi-Environment Deployment
```tsk
# Multi-environment deployment configuration
environment_deployments: #deployment("""
    production -> Production deployment
        strategy: "blue_green"
        replicas: 5
        resources:
            cpu: "1000m"
            memory: "2Gi"
        autoscaling:
            min: 3
            max: 10
        monitoring: true
        alerting: true
    
    staging -> Staging deployment
        strategy: "rolling"
        replicas: 2
        resources:
            cpu: "500m"
            memory: "1Gi"
        autoscaling:
            min: 1
            max: 5
        monitoring: true
        alerting: false
    
    development -> Development deployment
        strategy: "canary"
        replicas: 1
        resources:
            cpu: "250m"
            memory: "512Mi"
        autoscaling: false
        monitoring: false
        alerting: false
""")
```

### Infrastructure as Code
```tsk
# Infrastructure as code configuration
infrastructure_code: #deployment("""
    kubernetes -> Kubernetes infrastructure
        namespace: "production"
        ingress:
            host: "api.example.com"
            tls: true
            annotations:
                cert-manager.io/cluster-issuer: "letsencrypt-prod"
        service:
            type: "ClusterIP"
            port: 8080
        configmap:
            name: "app-config"
            data:
                environment: "production"
                log_level: "info"
        secret:
            name: "app-secrets"
            type: "Opaque"
    
    monitoring -> Monitoring infrastructure
        prometheus: true
        grafana: true
        alertmanager: true
        jaeger: true
        loki: true
""")
```

## 🛡️ Security Features

### Deployment Security Configuration
```tsk
# Deployment security configuration
deployment_security: #deployment("""
    container_security -> Container security
        run_as_non_root: true
        read_only_root_filesystem: true
        allow_privilege_escalation: false
        capabilities:
            drop: ["ALL"]
        seccomp_profile: "runtime/default"
    
    network_security -> Network security
        network_policy: true
        ingress_rules:
            - from: ["app=frontend"]
              ports: ["8080"]
        egress_rules:
            - to: ["app=database"]
              ports: ["5432"]
    
    secret_management -> Secret management
        vault_integration: true
        secret_rotation: true
        encryption_at_rest: true
        encryption_in_transit: true
""")
```

## ⚡ Performance Optimization

### Deployment Performance Configuration
```tsk
# Deployment performance configuration
deployment_performance: #deployment("""
    resource_optimization -> Resource optimization
        cpu_requests: "500m"
        cpu_limits: "1000m"
        memory_requests: "512Mi"
        memory_limits: "1Gi"
        hpa_enabled: true
        vpa_enabled: true
    
    scaling_configuration -> Scaling configuration
        horizontal_scaling:
            min_replicas: 2
            max_replicas: 10
            target_cpu_utilization: 70
            target_memory_utilization: 80
        vertical_scaling:
            enabled: true
            update_policy: "Auto"
    
    caching_strategy -> Caching strategy
        redis_cluster: true
        cache_size: "2Gi"
        persistence: true
        backup_enabled: true
""")
```

## 🔧 Error Handling

### Deployment Error Configuration
```tsk
# Deployment error handling configuration
deployment_errors: #deployment("""
    rollback_strategy -> Rollback strategy
        automatic_rollback: true
        rollback_threshold: 0.05
        rollback_window: 10m
        health_check_failure_threshold: 3
    
    failure_handling -> Failure handling
        max_retries: 3
        retry_delay: 30s
        exponential_backoff: true
        circuit_breaker: true
    
    monitoring_and_alerting -> Monitoring and alerting
        deployment_failure_alert: true
        health_check_alert: true
        resource_usage_alert: true
        notification_channels: ["slack", "email"]
""")
```

## 🎯 Real-World Example

### Complete Deployment Configuration
```tsk
# deployment-config.tsk - Complete deployment configuration

# Environment configuration
environment: #env("ENVIRONMENT", "development")
version: #env("APP_VERSION", "1.0.0")

# Deployment strategies
strategies: #deployment("""
    # Production deployment
    production -> Production deployment strategy
        strategy: "blue_green"
        health_check: "/health"
        readiness_check: "/ready"
        rollback_threshold: 0.05
        switch_timeout: 30s
        pre_deployment_hook: "backup_database"
        post_deployment_hook: "run_migrations"
        monitoring: true
        alerting: true
    
    # Staging deployment
    staging -> Staging deployment strategy
        strategy: "rolling"
        max_unavailable: 1
        max_surge: 2
        health_check: "/health"
        readiness_check: "/ready"
        monitoring: true
        alerting: false
    
    # Development deployment
    development -> Development deployment strategy
        strategy: "canary"
        initial_traffic: 0.1
        increment: 0.1
        interval: 5m
        max_traffic: 1.0
        health_check: "/health"
        monitoring: false
        alerting: false
""")

# Container configuration
containers: #deployment("""
    # Main application container
    app_container -> Main application container
        image: "myapp:#env('APP_VERSION')"
        port: 8080
        resources:
            cpu_requests: "500m"
            cpu_limits: "1000m"
            memory_requests: "512Mi"
            memory_limits: "1Gi"
        health_check:
            path: "/health"
            initial_delay: 30s
            period: 10s
            timeout: 5s
            failure_threshold: 3
        readiness_check:
            path: "/ready"
            initial_delay: 5s
            period: 5s
            timeout: 3s
            failure_threshold: 3
        security:
            run_as_non_root: true
            read_only_root_filesystem: true
            allow_privilege_escalation: false
        environment:
            - name: "ENVIRONMENT"
              value: "#env('ENVIRONMENT')"
            - name: "LOG_LEVEL"
              value: "info"
    
    # Sidecar containers
    nginx_sidecar -> Nginx sidecar container
        image: "nginx:alpine"
        port: 80
        resources:
            cpu_requests: "100m"
            cpu_limits: "200m"
            memory_requests: "128Mi"
            memory_limits: "256Mi"
        config:
            nginx_conf: "/etc/nginx/nginx.conf"
    
    redis_sidecar -> Redis sidecar container
        image: "redis:alpine"
        port: 6379
        resources:
            cpu_requests: "200m"
            cpu_limits: "500m"
            memory_requests: "256Mi"
            memory_limits: "512Mi"
        persistence:
            enabled: true
            size: "1Gi"
""")

# Infrastructure configuration
infrastructure: #deployment("""
    # Kubernetes configuration
    kubernetes -> Kubernetes deployment configuration
        namespace: "#env('ENVIRONMENT')"
        replicas: #if(#env('ENVIRONMENT') == 'production', 5, 2)
        autoscaling:
            enabled: true
            min_replicas: #if(#env('ENVIRONMENT') == 'production', 3, 1)
            max_replicas: #if(#env('ENVIRONMENT') == 'production', 10, 5)
            target_cpu_utilization: 70
            target_memory_utilization: 80
        ingress:
            enabled: true
            host: "#env('ENVIRONMENT')-api.example.com"
            tls: true
            annotations:
                cert-manager.io/cluster-issuer: "letsencrypt-prod"
                nginx.ingress.kubernetes.io/ssl-redirect: "true"
        service:
            type: "ClusterIP"
            port: 8080
            target_port: 8080
        configmap:
            name: "app-config"
            data:
                environment: "#env('ENVIRONMENT')"
                log_level: "info"
                database_url: "#env('DATABASE_URL')"
        secret:
            name: "app-secrets"
            type: "Opaque"
            data:
                api_key: "#env('API_KEY')"
                jwt_secret: "#env('JWT_SECRET')"
    
    # Monitoring infrastructure
    monitoring -> Monitoring infrastructure
        prometheus: true
        grafana: true
        alertmanager: true
        jaeger: true
        loki: true
        retention: #if(#env('ENVIRONMENT') == 'production', '30d', '7d')
    
    # Storage configuration
    storage -> Storage configuration
        persistent_volumes: true
        backup_enabled: true
        backup_schedule: "0 2 * * *"
        retention_policy: "30d"
""")

# Security configuration
security: #deployment("""
    # Container security
    container_security -> Container security configuration
        run_as_non_root: true
        read_only_root_filesystem: true
        allow_privilege_escalation: false
        capabilities:
            drop: ["ALL"]
        seccomp_profile: "runtime/default"
        apparmor_profile: "runtime/default"
    
    # Network security
    network_security -> Network security configuration
        network_policy: true
        ingress_rules:
            - from: ["app=frontend"]
              ports: ["8080"]
        egress_rules:
            - to: ["app=database"]
              ports: ["5432"]
            - to: ["app=redis"]
              ports: ["6379"]
        tls_enabled: true
        mTLS_enabled: true
    
    # Secret management
    secret_management -> Secret management configuration
        vault_integration: true
        secret_rotation: true
        rotation_interval: "24h"
        encryption_at_rest: true
        encryption_in_transit: true
        access_control: true
        audit_logging: true
""")

# Performance configuration
performance: #deployment("""
    # Resource optimization
    resource_optimization -> Resource optimization configuration
        cpu_requests: "500m"
        cpu_limits: "1000m"
        memory_requests: "512Mi"
        memory_limits: "1Gi"
        hpa_enabled: true
        vpa_enabled: true
        resource_quotas: true
    
    # Scaling configuration
    scaling_configuration -> Scaling configuration
        horizontal_scaling:
            min_replicas: #if(#env('ENVIRONMENT') == 'production', 3, 1)
            max_replicas: #if(#env('ENVIRONMENT') == 'production', 10, 5)
            target_cpu_utilization: 70
            target_memory_utilization: 80
            target_custom_metric: "requests_per_second"
        vertical_scaling:
            enabled: true
            update_policy: "Auto"
            min_allowed: "100m"
            max_allowed: "2Gi"
    
    # Caching strategy
    caching_strategy -> Caching strategy configuration
        redis_cluster: true
        cache_size: "2Gi"
        persistence: true
        backup_enabled: true
        replication: true
        sentinel_enabled: true
""")

# Error handling
error_handling: #deployment("""
    # Rollback strategy
    rollback_strategy -> Rollback strategy configuration
        automatic_rollback: true
        rollback_threshold: 0.05
        rollback_window: 10m
        health_check_failure_threshold: 3
        readiness_check_failure_threshold: 3
        max_rollback_history: 10
    
    # Failure handling
    failure_handling -> Failure handling configuration
        max_retries: 3
        retry_delay: 30s
        exponential_backoff: true
        circuit_breaker: true
        timeout: 300s
        graceful_shutdown: true
        shutdown_timeout: 30s
    
    # Monitoring and alerting
    monitoring_and_alerting -> Monitoring and alerting configuration
        deployment_failure_alert: true
        health_check_alert: true
        resource_usage_alert: true
        performance_alert: true
        notification_channels: ["slack", "email", "pagerduty"]
        alert_thresholds:
            cpu_usage: 80
            memory_usage: 80
            error_rate: 5
            response_time: 2
""")
```

### Go Deployment Application Implementation
```go
package main

import (
    "fmt"
    "log"
    "net/http"
    "github.com/tusklang/go-sdk"
    "github.com/gorilla/mux"
)

type DeploymentConfig struct {
    Environment    string `tsk:"environment"`
    Version        string `tsk:"version"`
    Strategies     string `tsk:"strategies"`
    Containers     string `tsk:"containers"`
    Infrastructure string `tsk:"infrastructure"`
    Security       string `tsk:"security"`
    Performance    string `tsk:"performance"`
    ErrorHandling  string `tsk:"error_handling"`
}

func main() {
    // Load deployment configuration
    config := tusk.LoadConfig("deployment-config.tsk")
    
    var deploymentConfig DeploymentConfig
    if err := config.Unmarshal(&deploymentConfig); err != nil {
        log.Fatal("Failed to load deployment config:", err)
    }
    
    // Create deployment system from directives
    deployment := tusk.NewDeploymentSystem(deploymentConfig)
    
    // Create router
    router := mux.NewRouter()
    
    // Apply deployment middleware
    tusk.ApplyDeploymentMiddleware(router, deployment)
    
    // Setup routes
    setupRoutes(router, deployment)
    
    // Start server
    addr := fmt.Sprintf(":%s", #env("PORT", "8080"))
    log.Printf("Starting deployment server on %s in %s mode", addr, deploymentConfig.Environment)
    
    if err := http.ListenAndServe(addr, router); err != nil {
        log.Fatal("Deployment server failed:", err)
    }
}

func setupRoutes(router *mux.Router, deployment *tusk.DeploymentSystem) {
    // Health check endpoints
    router.HandleFunc("/health", deployment.HealthCheck).Methods("GET")
    router.HandleFunc("/ready", deployment.ReadyCheck).Methods("GET")
    
    // Deployment management endpoints
    deploymentRouter := router.PathPrefix("/deployment").Subrouter()
    deploymentRouter.Use(authMiddleware)
    
    deploymentRouter.HandleFunc("/deploy", deployment.DeployHandler).Methods("POST")
    deploymentRouter.HandleFunc("/rollback", deployment.RollbackHandler).Methods("POST")
    deploymentRouter.HandleFunc("/status", deployment.StatusHandler).Methods("GET")
    deploymentRouter.HandleFunc("/logs", deployment.LogsHandler).Methods("GET")
    
    // API routes
    api := router.PathPrefix("/api").Subrouter()
    api.Use(deployment.Middleware())
    
    api.HandleFunc("/users", usersHandler).Methods("GET", "POST")
    api.HandleFunc("/users/{id}", userHandler).Methods("GET", "PUT", "DELETE")
}
```

## 🎯 Best Practices

### 1. **Use Appropriate Deployment Strategies**
```tsk
# Choose the right deployment strategy for each environment
strategy_selection: #deployment("""
    # Blue-green for production (zero downtime)
    production -> blue_green -> health_check:/health,rollback:0.05
    
    # Rolling for staging (controlled updates)
    staging -> rolling -> max_unavailable:1,max_surge:2
    
    # Canary for development (gradual rollout)
    development -> canary -> initial:0.1,increment:0.1
""")
```

### 2. **Implement Proper Health Checks**
```go
// Comprehensive health checks
func (dm *DeploymentManager) comprehensiveHealthCheck() bool {
    checks := map[string]bool{
        "database": dm.checkDatabaseHealth(),
        "redis":    dm.checkRedisHealth(),
        "external_api": dm.checkExternalAPIHealth(),
        "disk_space": dm.checkDiskSpace(),
        "memory": dm.checkMemoryUsage(),
    }
    
    for name, healthy := range checks {
        if !healthy {
            log.Printf("Health check failed: %s", name)
            return false
        }
    }
    
    return true
}
```

### 3. **Use Environment-Specific Configuration**
```tsk
# Different deployment settings for different environments
environment_deployment: #if(
    #env("ENVIRONMENT") == "production",
    #deployment("""
        replicas: 5
        resources: cpu:1000m,memory:2Gi
        autoscaling: min:3,max:10
        monitoring: true
    """),
    #deployment("""
        replicas: 2
        resources: cpu:500m,memory:1Gi
        autoscaling: min:1,max:5
        monitoring: false
    """)
)
```

### 4. **Monitor Deployment Performance**
```go
// Deployment performance monitoring
func monitorDeploymentPerformance(deploymentName string, startTime time.Time) {
    duration := time.Since(startTime)
    
    // Record metrics
    metrics := map[string]interface{}{
        "deployment": deploymentName,
        "duration":   duration.Seconds(),
        "timestamp":  time.Now(),
    }
    
    if err := recordDeploymentMetrics(metrics); err != nil {
        log.Printf("Failed to record deployment metrics: %v", err)
    }
    
    // Alert on slow deployments
    if duration > 5*time.Minute {
        log.Printf("Slow deployment: %s took %v", deploymentName, duration)
    }
}
```

## 🎯 Summary

Deployment directives in TuskLang provide a powerful, declarative way to define deployment systems. They enable:

- **Declarative deployment configuration** that is easy to understand and maintain
- **Flexible deployment strategies** including blue-green, rolling, and canary deployments
- **Comprehensive infrastructure management** with Kubernetes and cloud provider integration
- **Built-in security features** including container security and network policies
- **Performance optimization** with resource management and auto-scaling

The Go SDK seamlessly integrates deployment directives with existing Go deployment libraries, making them feel like native Go features while providing the power and flexibility of TuskLang's directive system.

**Next**: Explore scaling directives, security directives, and other specialized directive types in the following guides. 