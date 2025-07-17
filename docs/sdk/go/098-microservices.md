# Microservices

TuskLang provides powerful microservices capabilities that enable scalable, distributed application architectures. This guide covers comprehensive microservices strategies for Go applications.

## Microservices Philosophy

### Service-First Architecture
```go
// Service-first architecture with TuskLang
type MicroserviceManager struct {
    config *tusk.Config
    registry *ServiceRegistry
}

func NewMicroserviceManager(config *tusk.Config) *MicroserviceManager {
    return &MicroserviceManager{
        config:   config,
        registry: NewServiceRegistry(config),
    }
}

// CreateService creates a new microservice
func (mm *MicroserviceManager) CreateService(name string, config *ServiceConfig) (*Microservice, error) {
    service := &Microservice{
        Name:   name,
        Config: config,
        Status: "created",
    }
    
    // Validate service configuration
    if err := mm.validateServiceConfig(config); err != nil {
        return nil, fmt.Errorf("service validation failed: %w", err)
    }
    
    // Register service
    if err := mm.registry.RegisterService(service); err != nil {
        return nil, fmt.Errorf("failed to register service: %w", err)
    }
    
    return service, nil
}

type Microservice struct {
    Name   string
    Config *ServiceConfig
    Status string
    Health *HealthStatus
}

type ServiceConfig struct {
    Port        int
    Endpoints   []Endpoint
    Dependencies []string
    Resources   ResourceConfig
    Scaling     ScalingConfig
}
```

### Service Discovery
```go
// Service discovery with TuskLang
type ServiceRegistry struct {
    config *tusk.Config
    services map[string]*ServiceInfo
    client   *http.Client
}

type ServiceInfo struct {
    Name        string
    Address     string
    Port        int
    Health      string
    Metadata    map[string]string
    LastUpdated time.Time
}

func (sr *ServiceRegistry) RegisterService(service *Microservice) error {
    serviceInfo := &ServiceInfo{
        Name:        service.Name,
        Address:     sr.getServiceAddress(),
        Port:        service.Config.Port,
        Health:      "healthy",
        Metadata:    make(map[string]string),
        LastUpdated: time.Now(),
    }
    
    sr.services[service.Name] = serviceInfo
    
    // Register with external service registry
    if err := sr.registerWithExternalRegistry(serviceInfo); err != nil {
        return fmt.Errorf("failed to register with external registry: %w", err)
    }
    
    return nil
}

func (sr *ServiceRegistry) DiscoverService(name string) (*ServiceInfo, error) {
    // Check local registry first
    if service, exists := sr.services[name]; exists {
        return service, nil
    }
    
    // Query external service registry
    return sr.queryExternalRegistry(name)
}
```

## TuskLang Microservices Configuration

### Service Configuration
```tsk
# Microservices configuration
microservices {
    # Service registry
    service_registry {
        type = "consul"
        address = "localhost:8500"
        health_check_interval = "30s"
        deregister_after = "1m"
    }
    
    # Service mesh
    service_mesh {
        enabled = true
        type = "istio"
        sidecar_injection = true
        traffic_management = true
        security = true
        observability = true
    }
    
    # API gateway
    api_gateway {
        enabled = true
        port = 8080
        rate_limiting = true
        authentication = true
        routing = true
        load_balancing = true
    }
    
    # Circuit breaker
    circuit_breaker {
        enabled = true
        failure_threshold = 5
        timeout = "30s"
        half_open_state = true
    }
}
```

### Service Definitions
```tsk
# Service definitions
services {
    # User service
    user_service {
        name = "user-service"
        port = 8081
        version = "v1"
        endpoints = [
            "/users",
            "/users/{id}",
            "/users/{id}/profile"
        ]
        dependencies = ["auth-service", "notification-service"]
        resources {
            cpu_request = "100m"
            memory_request = "128Mi"
            cpu_limit = "500m"
            memory_limit = "512Mi"
        }
        scaling {
            min_replicas = 2
            max_replicas = 10
            target_cpu_utilization = 70
        }
    }
    
    # Order service
    order_service {
        name = "order-service"
        port = 8082
        version = "v1"
        endpoints = [
            "/orders",
            "/orders/{id}",
            "/orders/{id}/status"
        ]
        dependencies = ["user-service", "payment-service", "inventory-service"]
        resources {
            cpu_request = "200m"
            memory_request = "256Mi"
            cpu_limit = "1000m"
            memory_limit = "1Gi"
        }
        scaling {
            min_replicas = 3
            max_replicas = 15
            target_cpu_utilization = 80
        }
    }
}
```

## Go Microservices Implementation

### Service Implementation
```go
// Microservice implementation with TuskLang
type UserService struct {
    config *tusk.Config
    db     *sql.DB
    client *http.Client
}

func NewUserService(config *tusk.Config) *UserService {
    return &UserService{
        config: config,
        client: &http.Client{Timeout: 30 * time.Second},
    }
}

// Start starts the user service
func (us *UserService) Start() error {
    // Initialize database connection
    if err := us.initializeDatabase(); err != nil {
        return fmt.Errorf("failed to initialize database: %w", err)
    }
    
    // Setup HTTP server
    server := us.setupHTTPServer()
    
    // Register service
    if err := us.registerService(); err != nil {
        return fmt.Errorf("failed to register service: %w", err)
    }
    
    // Start server
    port := us.config.GetInt("services.user_service.port", 8081)
    log.Printf("User service starting on port %d", port)
    
    return server.ListenAndServe(fmt.Sprintf(":%d", port))
}

func (us *UserService) setupHTTPServer() *http.Server {
    mux := http.NewServeMux()
    
    // Setup routes
    us.setupRoutes(mux)
    
    // Add middleware
    handler := us.addMiddleware(mux)
    
    return &http.Server{
        Handler: handler,
    }
}

func (us *UserService) setupRoutes(mux *http.ServeMux) {
    // User endpoints
    mux.HandleFunc("/users", us.handleUsers)
    mux.HandleFunc("/users/", us.handleUserByID)
    mux.HandleFunc("/health", us.handleHealth)
    mux.HandleFunc("/ready", us.handleReady)
}

func (us *UserService) handleUsers(w http.ResponseWriter, r *http.Request) {
    switch r.Method {
    case "GET":
        us.getUsers(w, r)
    case "POST":
        us.createUser(w, r)
    default:
        http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
    }
}

func (us *UserService) getUsers(w http.ResponseWriter, r *http.Request) {
    // Get users from database
    users, err := us.getUsersFromDB()
    if err != nil {
        http.Error(w, "Internal server error", http.StatusInternalServerError)
        return
    }
    
    // Return JSON response
    w.Header().Set("Content-Type", "application/json")
    json.NewEncoder(w).Encode(users)
}
```

### Service Communication
```go
// Service-to-service communication
type ServiceClient struct {
    config *tusk.Config
    client *http.Client
    registry *ServiceRegistry
}

func (sc *ServiceClient) CallService(serviceName, endpoint string, data interface{}) ([]byte, error) {
    // Discover service
    serviceInfo, err := sc.registry.DiscoverService(serviceName)
    if err != nil {
        return nil, fmt.Errorf("failed to discover service %s: %w", serviceName, err)
    }
    
    // Build request URL
    url := fmt.Sprintf("http://%s:%d%s", serviceInfo.Address, serviceInfo.Port, endpoint)
    
    // Prepare request
    var req *http.Request
    if data != nil {
        jsonData, err := json.Marshal(data)
        if err != nil {
            return nil, fmt.Errorf("failed to marshal request data: %w", err)
        }
        
        req, err = http.NewRequest("POST", url, bytes.NewBuffer(jsonData))
        if err != nil {
            return nil, fmt.Errorf("failed to create request: %w", err)
        }
        
        req.Header.Set("Content-Type", "application/json")
    } else {
        req, err = http.NewRequest("GET", url, nil)
        if err != nil {
            return nil, fmt.Errorf("failed to create request: %w", err)
        }
    }
    
    // Add tracing headers
    sc.addTracingHeaders(req)
    
    // Make request
    resp, err := sc.client.Do(req)
    if err != nil {
        return nil, fmt.Errorf("request failed: %w", err)
    }
    defer resp.Body.Close()
    
    // Read response
    body, err := io.ReadAll(resp.Body)
    if err != nil {
        return nil, fmt.Errorf("failed to read response: %w", err)
    }
    
    if resp.StatusCode != http.StatusOK {
        return nil, fmt.Errorf("service returned status %d: %s", resp.StatusCode, string(body))
    }
    
    return body, nil
}

func (sc *ServiceClient) addTracingHeaders(req *http.Request) {
    // Add distributed tracing headers
    req.Header.Set("X-Request-ID", generateRequestID())
    req.Header.Set("X-Trace-ID", generateTraceID())
}
```

### Circuit Breaker Implementation
```go
// Circuit breaker pattern implementation
type CircuitBreaker struct {
    config *tusk.Config
    state  CircuitState
    count  int
    lastFailure time.Time
    mutex  sync.RWMutex
}

type CircuitState int

const (
    StateClosed CircuitState = iota
    StateOpen
    StateHalfOpen
)

func (cb *CircuitBreaker) Execute(operation func() error) error {
    cb.mutex.RLock()
    state := cb.state
    cb.mutex.RUnlock()
    
    switch state {
    case StateOpen:
        if cb.shouldAttemptReset() {
            cb.mutex.Lock()
            cb.state = StateHalfOpen
            cb.mutex.Unlock()
        } else {
            return errors.New("circuit breaker is open")
        }
    case StateHalfOpen:
        // Allow one request to test
    case StateClosed:
        // Allow all requests
    }
    
    // Execute operation
    err := operation()
    
    cb.mutex.Lock()
    defer cb.mutex.Unlock()
    
    if err != nil {
        cb.recordFailure()
    } else {
        cb.recordSuccess()
    }
    
    return err
}

func (cb *CircuitBreaker) recordFailure() {
    cb.count++
    cb.lastFailure = time.Now()
    
    threshold := cb.config.GetInt("microservices.circuit_breaker.failure_threshold", 5)
    if cb.count >= threshold {
        cb.state = StateOpen
    }
}

func (cb *CircuitBreaker) recordSuccess() {
    cb.count = 0
    cb.state = StateClosed
}

func (cb *CircuitBreaker) shouldAttemptReset() bool {
    timeout := cb.config.GetDuration("microservices.circuit_breaker.timeout", 30*time.Second)
    return time.Since(cb.lastFailure) > timeout
}
```

## Advanced Microservices Features

### API Gateway
```go
// API gateway implementation
type APIGateway struct {
    config *tusk.Config
    routes map[string]*Route
    client *http.Client
}

type Route struct {
    Path        string
    Service     string
    Method      string
    Middleware  []Middleware
}

func (ag *APIGateway) Start() error {
    mux := http.NewServeMux()
    
    // Setup routes
    ag.setupRoutes(mux)
    
    // Add gateway middleware
    handler := ag.addGatewayMiddleware(mux)
    
    port := ag.config.GetInt("microservices.api_gateway.port", 8080)
    server := &http.Server{
        Addr:    fmt.Sprintf(":%d", port),
        Handler: handler,
    }
    
    log.Printf("API Gateway starting on port %d", port)
    return server.ListenAndServe()
}

func (ag *APIGateway) setupRoutes(mux *http.ServeMux) {
    // Load routes from configuration
    routes := ag.loadRoutesFromConfig()
    
    for _, route := range routes {
        ag.routes[route.Path] = route
        mux.HandleFunc(route.Path, ag.handleRoute(route))
    }
}

func (ag *APIGateway) handleRoute(route *Route) http.HandlerFunc {
    return func(w http.ResponseWriter, r *http.Request) {
        // Apply middleware
        for _, middleware := range route.Middleware {
            if err := middleware(w, r); err != nil {
                http.Error(w, err.Error(), http.StatusInternalServerError)
                return
            }
        }
        
        // Route request to service
        if err := ag.routeToService(route, w, r); err != nil {
            http.Error(w, "Service unavailable", http.StatusServiceUnavailable)
        }
    }
}

func (ag *APIGateway) routeToService(route *Route, w http.ResponseWriter, r *http.Request) error {
    // Discover service
    serviceInfo, err := ag.discoverService(route.Service)
    if err != nil {
        return fmt.Errorf("failed to discover service: %w", err)
    }
    
    // Build target URL
    targetURL := fmt.Sprintf("http://%s:%d%s", serviceInfo.Address, serviceInfo.Port, r.URL.Path)
    
    // Create proxy request
    proxyReq, err := http.NewRequest(r.Method, targetURL, r.Body)
    if err != nil {
        return fmt.Errorf("failed to create proxy request: %w", err)
    }
    
    // Copy headers
    for name, values := range r.Header {
        for _, value := range values {
            proxyReq.Header.Add(name, value)
        }
    }
    
    // Make request
    resp, err := ag.client.Do(proxyReq)
    if err != nil {
        return fmt.Errorf("proxy request failed: %w", err)
    }
    defer resp.Body.Close()
    
    // Copy response
    for name, values := range resp.Header {
        for _, value := range values {
            w.Header().Add(name, value)
        }
    }
    
    w.WriteHeader(resp.StatusCode)
    io.Copy(w, resp.Body)
    
    return nil
}
```

### Service Mesh Integration
```go
// Service mesh integration
type ServiceMesh struct {
    config *tusk.Config
    client *http.Client
}

func (sm *ServiceMesh) InjectSidecar(service *Microservice) error {
    // Check if sidecar injection is enabled
    if !sm.config.GetBool("microservices.service_mesh.sidecar_injection") {
        return nil
    }
    
    // Inject sidecar proxy
    if err := sm.injectSidecarProxy(service); err != nil {
        return fmt.Errorf("failed to inject sidecar proxy: %w", err)
    }
    
    // Configure traffic management
    if err := sm.configureTrafficManagement(service); err != nil {
        return fmt.Errorf("failed to configure traffic management: %w", err)
    }
    
    // Setup security policies
    if err := sm.setupSecurityPolicies(service); err != nil {
        return fmt.Errorf("failed to setup security policies: %w", err)
    }
    
    return nil
}

func (sm *ServiceMesh) injectSidecarProxy(service *Microservice) error {
    // Inject Envoy proxy sidecar
    // This is a simplified implementation
    return nil
}

func (sm *ServiceMesh) configureTrafficManagement(service *Microservice) error {
    // Configure traffic routing rules
    // This is a simplified implementation
    return nil
}
```

### Distributed Tracing
```go
// Distributed tracing implementation
type TracingManager struct {
    config *tusk.Config
    tracer *tracing.Tracer
}

func (tm *TracingManager) StartSpan(operation string, ctx context.Context) (context.Context, func()) {
    span := tm.tracer.StartSpan(operation)
    ctx = trace.ContextWithSpan(ctx, span)
    
    return ctx, func() {
        span.End()
    }
}

func (tm *TracingManager) InjectSpan(ctx context.Context, req *http.Request) {
    span := trace.SpanFromContext(ctx)
    if span != nil {
        tm.tracer.Inject(span.Context(), "http", req.Header)
    }
}

func (tm *TracingManager) ExtractSpan(req *http.Request) context.Context {
    spanContext, err := tm.tracer.Extract("http", req.Header)
    if err != nil {
        return context.Background()
    }
    
    span := tm.tracer.StartSpan("http_request", trace.ChildOf(spanContext))
    return trace.ContextWithSpan(context.Background(), span)
}
```

## Microservices Tools and Utilities

### Service Health Monitoring
```go
// Service health monitoring
type HealthMonitor struct {
    config *tusk.Config
    services map[string]*HealthStatus
}

type HealthStatus struct {
    Service     string
    Status      string
    LastCheck   time.Time
    ResponseTime time.Duration
    Error       string
}

func (hm *HealthMonitor) MonitorServices() {
    ticker := time.NewTicker(30 * time.Second)
    defer ticker.Stop()
    
    for {
        select {
        case <-ticker.C:
            hm.checkAllServices()
        }
    }
}

func (hm *HealthMonitor) checkAllServices() {
    services := hm.config.GetStringSlice("services", []string{})
    
    for _, serviceName := range services {
        go hm.checkServiceHealth(serviceName)
    }
}

func (hm *HealthMonitor) checkServiceHealth(serviceName string) {
    start := time.Now()
    
    // Get service info
    serviceInfo, err := hm.discoverService(serviceName)
    if err != nil {
        hm.updateHealthStatus(serviceName, "unhealthy", time.Since(start), err.Error())
        return
    }
    
    // Check health endpoint
    url := fmt.Sprintf("http://%s:%d/health", serviceInfo.Address, serviceInfo.Port)
    resp, err := http.Get(url)
    
    if err != nil {
        hm.updateHealthStatus(serviceName, "unhealthy", time.Since(start), err.Error())
        return
    }
    defer resp.Body.Close()
    
    if resp.StatusCode == http.StatusOK {
        hm.updateHealthStatus(serviceName, "healthy", time.Since(start), "")
    } else {
        hm.updateHealthStatus(serviceName, "unhealthy", time.Since(start), fmt.Sprintf("status %d", resp.StatusCode))
    }
}

func (hm *HealthMonitor) updateHealthStatus(serviceName, status string, responseTime time.Duration, error string) {
    hm.services[serviceName] = &HealthStatus{
        Service:      serviceName,
        Status:       status,
        LastCheck:    time.Now(),
        ResponseTime: responseTime,
        Error:        error,
    }
}
```

### Service Metrics Collection
```go
// Service metrics collection
type MetricsCollector struct {
    config *tusk.Config
    metrics map[string]*ServiceMetrics
}

type ServiceMetrics struct {
    Service       string
    RequestCount  int64
    ErrorCount    int64
    ResponseTime  time.Duration
    LastUpdated   time.Time
}

func (mc *MetricsCollector) RecordRequest(serviceName string, duration time.Duration, err error) {
    metrics, exists := mc.metrics[serviceName]
    if !exists {
        metrics = &ServiceMetrics{
            Service: serviceName,
        }
        mc.metrics[serviceName] = metrics
    }
    
    atomic.AddInt64(&metrics.RequestCount, 1)
    if err != nil {
        atomic.AddInt64(&metrics.ErrorCount, 1)
    }
    
    metrics.ResponseTime = duration
    metrics.LastUpdated = time.Now()
}

func (mc *MetricsCollector) GetMetrics(serviceName string) *ServiceMetrics {
    return mc.metrics[serviceName]
}
```

## Validation and Error Handling

### Microservices Configuration Validation
```go
// Validate microservices configuration
func ValidateMicroservicesConfig(config *tusk.Config) error {
    if config == nil {
        return errors.New("microservices config cannot be nil")
    }
    
    // Validate service registry configuration
    if !config.Has("microservices.service_registry") {
        return errors.New("missing service registry configuration")
    }
    
    // Validate service definitions
    if !config.Has("services") {
        return errors.New("missing service definitions")
    }
    
    return nil
}
```

### Error Handling
```go
// Handle microservices errors gracefully
func handleMicroserviceError(err error, context string) {
    log.Printf("Microservice error in %s: %v", context, err)
    
    // Log additional context if available
    if msErr, ok := err.(*MicroserviceError); ok {
        log.Printf("Microservice context: %s", msErr.Context)
    }
}
```

## Performance Considerations

### Microservices Performance Optimization
```go
// Optimize microservices performance
type MicroservicesOptimizer struct {
    config *tusk.Config
}

func (mo *MicroservicesOptimizer) OptimizeServices() error {
    // Enable connection pooling
    if mo.config.GetBool("microservices.performance.connection_pooling") {
        mo.setupConnectionPooling()
    }
    
    // Enable caching
    if mo.config.GetBool("microservices.performance.caching") {
        mo.setupCaching()
    }
    
    // Optimize serialization
    if err := mo.optimizeSerialization(); err != nil {
        return fmt.Errorf("failed to optimize serialization: %w", err)
    }
    
    return nil
}

func (mo *MicroservicesOptimizer) setupConnectionPooling() {
    // Setup HTTP client connection pooling
    // This is a simplified implementation
}

func (mo *MicroservicesOptimizer) setupCaching() {
    // Setup service response caching
    // This is a simplified implementation
}
```

## Microservices Notes

- **Service Discovery**: Implement service discovery for dynamic service location
- **Circuit Breakers**: Use circuit breakers for fault tolerance
- **API Gateway**: Implement API gateway for centralized routing
- **Service Mesh**: Use service mesh for advanced traffic management
- **Distributed Tracing**: Implement distributed tracing for observability
- **Health Monitoring**: Monitor service health and performance
- **Metrics Collection**: Collect comprehensive service metrics
- **Security**: Implement service-to-service security

## Best Practices

1. **Service Boundaries**: Define clear service boundaries
2. **Service Discovery**: Use service discovery for dynamic environments
3. **Circuit Breakers**: Implement circuit breakers for resilience
4. **API Gateway**: Use API gateway for centralized management
5. **Service Mesh**: Implement service mesh for advanced features
6. **Monitoring**: Monitor all services comprehensively
7. **Security**: Implement proper service-to-service security
8. **Performance**: Optimize service communication and performance

## Integration with TuskLang

```go
// Load microservices configuration from TuskLang
func LoadMicroservicesConfig(configPath string) (*tusk.Config, error) {
    config, err := tusk.LoadConfig(configPath)
    if err != nil {
        return nil, fmt.Errorf("failed to load microservices config: %w", err)
    }
    
    // Validate microservices configuration
    if err := ValidateMicroservicesConfig(config); err != nil {
        return nil, fmt.Errorf("invalid microservices config: %w", err)
    }
    
    return config, nil
}
```

This microservices guide provides comprehensive microservices capabilities for your Go applications using TuskLang. Remember, good microservices architecture is essential for scalable applications. 