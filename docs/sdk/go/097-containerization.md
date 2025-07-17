# Containerization

TuskLang provides powerful containerization capabilities that streamline the deployment and management of Go applications. This guide covers comprehensive containerization strategies.

## Containerization Philosophy

### Container-First Development
```go
// Container-first development with TuskLang
type ContainerManager struct {
    config *tusk.Config
    client *http.Client
}

func NewContainerManager(config *tusk.Config) *ContainerManager {
    return &ContainerManager{
        config: config,
        client: &http.Client{Timeout: 30 * time.Second},
    }
}

// BuildContainer builds a container image for the application
func (cm *ContainerManager) BuildContainer(platform string) error {
    // Load container configuration
    containerConfig, err := cm.loadContainerConfig()
    if err != nil {
        return fmt.Errorf("failed to load container config: %w", err)
    }
    
    // Generate Dockerfile
    if err := cm.generateDockerfile(containerConfig); err != nil {
        return fmt.Errorf("failed to generate Dockerfile: %w", err)
    }
    
    // Build container image
    if err := cm.buildImage(containerConfig, platform); err != nil {
        return fmt.Errorf("failed to build container image: %w", err)
    }
    
    return nil
}

type ContainerConfig struct {
    BaseImage    string
    BuildArgs    map[string]string
    Environment  map[string]string
    Ports        []int
    Volumes      []string
    Commands     []string
    HealthCheck  HealthCheck
}
```

### Multi-Stage Builds
```go
// Multi-stage build support
type MultiStageBuilder struct {
    config *tusk.Config
}

func (msb *MultiStageBuilder) BuildMultiStage() error {
    // Build stage
    if err := msb.buildStage(); err != nil {
        return fmt.Errorf("build stage failed: %w", err)
    }
    
    // Test stage
    if err := msb.testStage(); err != nil {
        return fmt.Errorf("test stage failed: %w", err)
    }
    
    // Production stage
    if err := msb.productionStage(); err != nil {
        return fmt.Errorf("production stage failed: %w", err)
    }
    
    return nil
}

func (msb *MultiStageBuilder) buildStage() error {
    // Build the application
    cmd := exec.Command("go", "build", "-o", "app", "./cmd/app")
    cmd.Stdout = os.Stdout
    cmd.Stderr = os.Stderr
    
    return cmd.Run()
}

func (msb *MultiStageBuilder) testStage() error {
    // Run tests
    cmd := exec.Command("go", "test", "./...")
    cmd.Stdout = os.Stdout
    cmd.Stderr = os.Stderr
    
    return cmd.Run()
}

func (msb *MultiStageBuilder) productionStage() error {
    // Create production image
    return nil
}
```

## TuskLang Container Configuration

### Container Environment Setup
```tsk
# Container configuration
container_environment {
    # Base image configuration
    base_image {
        name = "golang:1.21-alpine"
        version = "1.21"
        platform = "linux/amd64"
        multi_arch = true
    }
    
    # Build configuration
    build {
        context = "."
        dockerfile = "Dockerfile"
        target = "production"
        cache_from = ["app:latest"]
        build_args = {
            "VERSION" = "$CI_COMMIT_SHA"
            "BUILD_DATE" = "$(date -u +'%Y-%m-%dT%H:%M:%SZ')"
        }
    }
    
    # Runtime configuration
    runtime {
        user = "1000:1000"
        working_dir = "/app"
        entrypoint = ["./app"]
        command = []
        environment = {
            "GIN_MODE" = "release"
            "TZ" = "UTC"
        }
    }
    
    # Health check configuration
    health_check {
        command = ["curl", "-f", "http://localhost:8080/health"]
        interval = "30s"
        timeout = "10s"
        retries = 3
        start_period = "40s"
    }
    
    # Security configuration
    security {
        run_as_non_root = true
        read_only_root = true
        no_new_privileges = true
        capabilities_drop = ["ALL"]
        security_opt = ["no-new-privileges"]
    }
}
```

### Multi-Platform Configuration
```tsk
# Multi-platform configuration
multi_platform {
    # Supported platforms
    platforms = [
        "linux/amd64",
        "linux/arm64",
        "linux/arm/v7",
        "linux/arm/v6"
    ]
    
    # Platform-specific configurations
    platform_configs {
        "linux/amd64" {
            base_image = "golang:1.21-alpine"
            build_args = {
                "GOARCH" = "amd64"
                "GOOS" = "linux"
            }
        }
        
        "linux/arm64" {
            base_image = "golang:1.21-alpine"
            build_args = {
                "GOARCH" = "arm64"
                "GOOS" = "linux"
            }
        }
    }
    
    # Registry configuration
    registry {
        url = "registry.example.com"
        username = "$REGISTRY_USERNAME"
        password = "$REGISTRY_PASSWORD"
        push = true
        tag_latest = true
    }
}
```

## Go Containerization Implementation

### Dockerfile Generation
```go
// Dockerfile generator with TuskLang configuration
type DockerfileGenerator struct {
    config *tusk.Config
}

func (dg *DockerfileGenerator) GenerateDockerfile() error {
    // Load container configuration
    containerConfig, err := dg.loadContainerConfig()
    if err != nil {
        return fmt.Errorf("failed to load container config: %w", err)
    }
    
    // Generate Dockerfile content
    content := dg.generateDockerfileContent(containerConfig)
    
    // Write Dockerfile
    if err := os.WriteFile("Dockerfile", []byte(content), 0644); err != nil {
        return fmt.Errorf("failed to write Dockerfile: %w", err)
    }
    
    return nil
}

func (dg *DockerfileGenerator) generateDockerfileContent(config *ContainerConfig) string {
    template := `# Multi-stage build
FROM {{.BaseImage}} AS builder

# Set working directory
WORKDIR /app

# Copy go mod files
COPY go.mod go.sum ./

# Download dependencies
RUN go mod download

# Copy source code
COPY . .

# Build the application
RUN CGO_ENABLED=0 GOOS=linux go build -a -installsuffix cgo -o app ./cmd/app

# Production stage
FROM alpine:latest

# Install ca-certificates for HTTPS requests
RUN apk --no-cache add ca-certificates

# Create non-root user
RUN addgroup -g 1000 app && \
    adduser -D -s /bin/sh -u 1000 -G app app

# Set working directory
WORKDIR /app

# Copy binary from builder stage
COPY --from=builder /app/app .

# Change ownership
RUN chown app:app /app/app

# Switch to non-root user
USER app

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Run the application
ENTRYPOINT ["./app"]
`
    
    // Execute template
    tmpl, err := template.New("dockerfile").Parse(template)
    if err != nil {
        return ""
    }
    
    var buf bytes.Buffer
    tmpl.Execute(&buf, config)
    
    return buf.String()
}
```

### Container Build System
```go
// Container build system
type ContainerBuildSystem struct {
    config *tusk.Config
}

func (cbs *ContainerBuildSystem) BuildContainer(platform string) error {
    // Load build configuration
    buildConfig, err := cbs.loadBuildConfig()
    if err != nil {
        return fmt.Errorf("failed to load build config: %w", err)
    }
    
    // Set build arguments
    buildArgs := cbs.prepareBuildArgs(buildConfig)
    
    // Build container image
    if err := cbs.buildImage(buildArgs, platform); err != nil {
        return fmt.Errorf("failed to build image: %w", err)
    }
    
    // Tag image
    if err := cbs.tagImage(buildConfig); err != nil {
        return fmt.Errorf("failed to tag image: %w", err)
    }
    
    return nil
}

func (cbs *ContainerBuildSystem) buildImage(args []string, platform string) error {
    // Prepare docker build command
    cmdArgs := []string{"buildx", "build", "--platform", platform}
    cmdArgs = append(cmdArgs, args...)
    cmdArgs = append(cmdArgs, ".")
    
    cmd := exec.Command("docker", cmdArgs...)
    cmd.Stdout = os.Stdout
    cmd.Stderr = os.Stderr
    
    return cmd.Run()
}

func (cbs *ContainerBuildSystem) prepareBuildArgs(config *BuildConfig) []string {
    var args []string
    
    // Add build arguments
    for key, value := range config.BuildArgs {
        args = append(args, "--build-arg", fmt.Sprintf("%s=%s", key, value))
    }
    
    // Add target
    if config.Target != "" {
        args = append(args, "--target", config.Target)
    }
    
    // Add cache from
    for _, cache := range config.CacheFrom {
        args = append(args, "--cache-from", cache)
    }
    
    return args
}
```

### Container Registry Management
```go
// Container registry management
type RegistryManager struct {
    config *tusk.Config
    client *http.Client
}

func (rm *RegistryManager) PushImage(imageName, tag string) error {
    // Login to registry
    if err := rm.loginToRegistry(); err != nil {
        return fmt.Errorf("failed to login to registry: %w", err)
    }
    
    // Push image
    if err := rm.pushImage(imageName, tag); err != nil {
        return fmt.Errorf("failed to push image: %w", err)
    }
    
    return nil
}

func (rm *RegistryManager) loginToRegistry() error {
    username := rm.config.GetString("multi_platform.registry.username")
    password := rm.config.GetString("multi_platform.registry.password")
    registry := rm.config.GetString("multi_platform.registry.url")
    
    cmd := exec.Command("docker", "login", registry, "-u", username, "-p", password)
    cmd.Stdout = os.Stdout
    cmd.Stderr = os.Stderr
    
    return cmd.Run()
}

func (rm *RegistryManager) pushImage(imageName, tag string) error {
    fullImageName := fmt.Sprintf("%s:%s", imageName, tag)
    
    cmd := exec.Command("docker", "push", fullImageName)
    cmd.Stdout = os.Stdout
    cmd.Stderr = os.Stderr
    
    return cmd.Run()
}
```

## Advanced Containerization Features

### Container Orchestration
```go
// Container orchestration with Kubernetes
type ContainerOrchestrator struct {
    config *tusk.Config
    client *kubernetes.Clientset
}

func (co *ContainerOrchestrator) DeployToKubernetes(deployment *Deployment) error {
    // Create deployment
    if err := co.createDeployment(deployment); err != nil {
        return fmt.Errorf("failed to create deployment: %w", err)
    }
    
    // Create service
    if err := co.createService(deployment); err != nil {
        return fmt.Errorf("failed to create service: %w", err)
    }
    
    // Create ingress
    if err := co.createIngress(deployment); err != nil {
        return fmt.Errorf("failed to create ingress: %w", err)
    }
    
    return nil
}

func (co *ContainerOrchestrator) createDeployment(deployment *Deployment) error {
    // Generate Kubernetes deployment manifest
    manifest := co.generateDeploymentManifest(deployment)
    
    // Apply manifest
    if err := co.applyManifest(manifest); err != nil {
        return fmt.Errorf("failed to apply deployment manifest: %w", err)
    }
    
    return nil
}

func (co *ContainerOrchestrator) generateDeploymentManifest(deployment *Deployment) string {
    template := `apiVersion: apps/v1
kind: Deployment
metadata:
  name: {{.Name}}
  labels:
    app: {{.Name}}
spec:
  replicas: {{.Replicas}}
  selector:
    matchLabels:
      app: {{.Name}}
  template:
    metadata:
      labels:
        app: {{.Name}}
    spec:
      containers:
      - name: {{.Name}}
        image: {{.Image}}
        ports:
        - containerPort: {{.Port}}
        env:
        {{range .Environment}}
        - name: {{.Name}}
          value: "{{.Value}}"
        {{end}}
        resources:
          requests:
            memory: "{{.MemoryRequest}}"
            cpu: "{{.CPURequest}}"
          limits:
            memory: "{{.MemoryLimit}}"
            cpu: "{{.CPULimit}}"
        livenessProbe:
          httpGet:
            path: /health
            port: {{.Port}}
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: {{.Port}}
          initialDelaySeconds: 5
          periodSeconds: 5
`
    
    // Execute template
    tmpl, err := template.New("deployment").Parse(template)
    if err != nil {
        return ""
    }
    
    var buf bytes.Buffer
    tmpl.Execute(&buf, deployment)
    
    return buf.String()
}

type Deployment struct {
    Name           string
    Image          string
    Replicas       int
    Port           int
    Environment    []EnvironmentVariable
    MemoryRequest  string
    CPURequest     string
    MemoryLimit    string
    CPULimit       string
}

type EnvironmentVariable struct {
    Name  string
    Value string
}
```

### Container Security
```go
// Container security management
type ContainerSecurity struct {
    config *tusk.Config
}

func (cs *ContainerSecurity) ScanImage(imageName string) error {
    // Run security scan
    if err := cs.runSecurityScan(imageName); err != nil {
        return fmt.Errorf("security scan failed: %w", err)
    }
    
    // Check for vulnerabilities
    vulnerabilities, err := cs.getVulnerabilities(imageName)
    if err != nil {
        return fmt.Errorf("failed to get vulnerabilities: %w", err)
    }
    
    // Handle vulnerabilities
    if len(vulnerabilities) > 0 {
        if err := cs.handleVulnerabilities(vulnerabilities); err != nil {
            return fmt.Errorf("failed to handle vulnerabilities: %w", err)
        }
    }
    
    return nil
}

func (cs *ContainerSecurity) runSecurityScan(imageName string) error {
    // Run Trivy security scan
    cmd := exec.Command("trivy", "image", "--severity", "HIGH,CRITICAL", imageName)
    cmd.Stdout = os.Stdout
    cmd.Stderr = os.Stderr
    
    return cmd.Run()
}

func (cs *ContainerSecurity) getVulnerabilities(imageName string) ([]Vulnerability, error) {
    // Parse vulnerability report
    // This is a simplified implementation
    return []Vulnerability{}, nil
}

type Vulnerability struct {
    ID          string
    Severity    string
    Package     string
    Version     string
    Description string
}
```

### Container Monitoring
```go
// Container monitoring and metrics
type ContainerMonitor struct {
    config *tusk.Config
    client *http.Client
}

func (cm *ContainerMonitor) MonitorContainer(containerID string) error {
    // Get container metrics
    metrics, err := cm.getContainerMetrics(containerID)
    if err != nil {
        return fmt.Errorf("failed to get container metrics: %w", err)
    }
    
    // Check resource usage
    if err := cm.checkResourceUsage(metrics); err != nil {
        return fmt.Errorf("resource usage check failed: %w", err)
    }
    
    // Send metrics to monitoring system
    if err := cm.sendMetrics(metrics); err != nil {
        return fmt.Errorf("failed to send metrics: %w", err)
    }
    
    return nil
}

func (cm *ContainerMonitor) getContainerMetrics(containerID string) (*ContainerMetrics, error) {
    // Get container stats from Docker API
    resp, err := cm.client.Get(fmt.Sprintf("http://localhost/containers/%s/stats", containerID))
    if err != nil {
        return nil, err
    }
    defer resp.Body.Close()
    
    var stats ContainerStats
    if err := json.NewDecoder(resp.Body).Decode(&stats); err != nil {
        return nil, err
    }
    
    return &ContainerMetrics{
        ContainerID: containerID,
        CPUUsage:    stats.CPUStats.CPUUsage.TotalUsage,
        MemoryUsage: stats.MemoryStats.Usage,
        NetworkIO:   stats.Networks,
        Timestamp:   time.Now(),
    }, nil
}

type ContainerMetrics struct {
    ContainerID string
    CPUUsage    uint64
    MemoryUsage uint64
    NetworkIO   map[string]NetworkStats
    Timestamp   time.Time
}

type ContainerStats struct {
    CPUStats    CPUStats    `json:"cpu_stats"`
    MemoryStats MemoryStats `json:"memory_stats"`
    Networks    map[string]NetworkStats `json:"networks"`
}

type CPUStats struct {
    CPUUsage CPUUsage `json:"cpu_usage"`
}

type CPUUsage struct {
    TotalUsage uint64 `json:"total_usage"`
}

type MemoryStats struct {
    Usage uint64 `json:"usage"`
}

type NetworkStats struct {
    RxBytes uint64 `json:"rx_bytes"`
    TxBytes uint64 `json:"tx_bytes"`
}
```

## Container Tools and Utilities

### Container Health Check
```go
// Container health check utilities
type ContainerHealthChecker struct {
    config *tusk.Config
    client *http.Client
}

func (chc *ContainerHealthChecker) CheckHealth(containerID string) error {
    // Check container status
    if err := chc.checkContainerStatus(containerID); err != nil {
        return fmt.Errorf("container status check failed: %w", err)
    }
    
    // Check application health
    if err := chc.checkApplicationHealth(containerID); err != nil {
        return fmt.Errorf("application health check failed: %w", err)
    }
    
    // Check resource health
    if err := chc.checkResourceHealth(containerID); err != nil {
        return fmt.Errorf("resource health check failed: %w", err)
    }
    
    return nil
}

func (chc *ContainerHealthChecker) checkContainerStatus(containerID string) error {
    // Check if container is running
    cmd := exec.Command("docker", "inspect", "--format", "{{.State.Status}}", containerID)
    output, err := cmd.Output()
    if err != nil {
        return fmt.Errorf("failed to inspect container: %w", err)
    }
    
    status := strings.TrimSpace(string(output))
    if status != "running" {
        return fmt.Errorf("container is not running: %s", status)
    }
    
    return nil
}

func (chc *ContainerHealthChecker) checkApplicationHealth(containerID string) error {
    // Get container port
    port := chc.config.GetString("container_environment.runtime.health_check.port", "8080")
    
    // Check health endpoint
    url := fmt.Sprintf("http://localhost:%s/health", port)
    resp, err := chc.client.Get(url)
    if err != nil {
        return fmt.Errorf("health check request failed: %w", err)
    }
    defer resp.Body.Close()
    
    if resp.StatusCode != http.StatusOK {
        return fmt.Errorf("health check failed with status: %d", resp.StatusCode)
    }
    
    return nil
}
```

### Container Logging
```go
// Container logging management
type ContainerLogger struct {
    config *tusk.Config
}

func (cl *ContainerLogger) CollectLogs(containerID string) error {
    // Get container logs
    logs, err := cl.getContainerLogs(containerID)
    if err != nil {
        return fmt.Errorf("failed to get container logs: %w", err)
    }
    
    // Parse and structure logs
    structuredLogs := cl.parseLogs(logs)
    
    // Send logs to logging system
    if err := cl.sendLogs(structuredLogs); err != nil {
        return fmt.Errorf("failed to send logs: %w", err)
    }
    
    return nil
}

func (cl *ContainerLogger) getContainerLogs(containerID string) ([]byte, error) {
    cmd := exec.Command("docker", "logs", containerID)
    return cmd.Output()
}

func (cl *ContainerLogger) parseLogs(logs []byte) []StructuredLog {
    var structuredLogs []StructuredLog
    
    lines := strings.Split(string(logs), "\n")
    for _, line := range lines {
        if line != "" {
            structuredLog := StructuredLog{
                Message:   line,
                Timestamp: time.Now(),
                Level:     cl.detectLogLevel(line),
            }
            structuredLogs = append(structuredLogs, structuredLog)
        }
    }
    
    return structuredLogs
}

type StructuredLog struct {
    Message   string
    Timestamp time.Time
    Level     string
}
```

## Validation and Error Handling

### Container Configuration Validation
```go
// Validate container configuration
func ValidateContainerConfig(config *tusk.Config) error {
    if config == nil {
        return errors.New("container config cannot be nil")
    }
    
    // Validate base image configuration
    if !config.Has("container_environment.base_image") {
        return errors.New("missing base image configuration")
    }
    
    // Validate build configuration
    if !config.Has("container_environment.build") {
        return errors.New("missing build configuration")
    }
    
    return nil
}
```

### Error Handling
```go
// Handle container errors gracefully
func handleContainerError(err error, context string) {
    log.Printf("Container error in %s: %v", context, err)
    
    // Log additional context if available
    if containerErr, ok := err.(*ContainerError); ok {
        log.Printf("Container context: %s", containerErr.Context)
    }
}
```

## Performance Considerations

### Container Performance Optimization
```go
// Optimize container performance
type ContainerOptimizer struct {
    config *tusk.Config
}

func (co *ContainerOptimizer) OptimizeContainer() error {
    // Optimize image size
    if err := co.optimizeImageSize(); err != nil {
        return fmt.Errorf("failed to optimize image size: %w", err)
    }
    
    // Optimize build time
    if err := co.optimizeBuildTime(); err != nil {
        return fmt.Errorf("failed to optimize build time: %w", err)
    }
    
    // Optimize runtime performance
    if err := co.optimizeRuntime(); err != nil {
        return fmt.Errorf("failed to optimize runtime: %w", err)
    }
    
    return nil
}

func (co *ContainerOptimizer) optimizeImageSize() error {
    // Use multi-stage builds
    // Use Alpine base images
    // Remove unnecessary files
    return nil
}

func (co *ContainerOptimizer) optimizeBuildTime() error {
    // Use build cache
    // Optimize layer ordering
    // Use .dockerignore
    return nil
}
```

## Containerization Notes

- **Multi-Stage Builds**: Use multi-stage builds for smaller images
- **Security**: Implement container security best practices
- **Monitoring**: Monitor container health and performance
- **Logging**: Implement structured logging for containers
- **Orchestration**: Use Kubernetes for container orchestration
- **Registry**: Use container registries for image storage
- **CI/CD**: Integrate containerization into CI/CD pipelines
- **Performance**: Optimize container size and build times

## Best Practices

1. **Multi-Stage Builds**: Use multi-stage builds for efficiency
2. **Security**: Implement security scanning and best practices
3. **Monitoring**: Monitor container health and metrics
4. **Logging**: Use structured logging for better observability
5. **Orchestration**: Use proper container orchestration
6. **Registry**: Use secure container registries
7. **CI/CD**: Integrate containerization into pipelines
8. **Performance**: Optimize container performance

## Integration with TuskLang

```go
// Load container configuration from TuskLang
func LoadContainerConfig(configPath string) (*tusk.Config, error) {
    config, err := tusk.LoadConfig(configPath)
    if err != nil {
        return nil, fmt.Errorf("failed to load container config: %w", err)
    }
    
    // Validate container configuration
    if err := ValidateContainerConfig(config); err != nil {
        return nil, fmt.Errorf("invalid container config: %w", err)
    }
    
    return config, nil
}
```

This containerization guide provides comprehensive containerization capabilities for your Go applications using TuskLang. Remember, good containerization is essential for scalable deployments. 