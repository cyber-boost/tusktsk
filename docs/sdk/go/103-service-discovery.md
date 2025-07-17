# Service Discovery

TuskLang provides powerful service discovery capabilities that enable dynamic, scalable microservices architectures. This guide covers comprehensive service discovery strategies for Go applications.

## Service Discovery Philosophy

### Discovery-First Architecture
```go
// Discovery-first architecture with TuskLang
type ServiceDiscovery struct {
    config *tusk.Config
    registry *ServiceRegistry
    resolver *ServiceResolver
}

func NewServiceDiscovery(config *tusk.Config) *ServiceDiscovery {
    return &ServiceDiscovery{
        config:   config,
        registry: NewServiceRegistry(config),
        resolver: NewServiceResolver(config),
    }
}

// RegisterService registers a service with the discovery system
func (sd *ServiceDiscovery) RegisterService(service *Service) error {
    // Validate service
    if err := sd.validateService(service); err != nil {
        return fmt.Errorf("service validation failed: %w", err)
    }
    
    // Register with registry
    if err := sd.registry.Register(service); err != nil {
        return fmt.Errorf("failed to register service: %w", err)
    }
    
    return nil
}

type Service struct {
    ID          string
    Name        string
    Version     string
    Address     string
    Port        int
    Health      string
    Metadata    map[string]string
    Tags        []string
    LastSeen    time.Time
}
```

### Dynamic Service Resolution
```go
// Dynamic service resolution with load balancing
type ServiceResolver struct {
    config *tusk.Config
    registry *ServiceRegistry
    loadBalancer *LoadBalancer
}

func (sr *ServiceResolver) ResolveService(serviceName string) (*Service, error) {
    // Get all instances of the service
    instances, err := sr.registry.GetServiceInstances(serviceName)
    if err != nil {
        return nil, fmt.Errorf("failed to get service instances: %w", err)
    }
    
    if len(instances) == 0 {
        return nil, fmt.Errorf("no instances found for service: %s", serviceName)
    }
    
    // Select instance using load balancer
    instance := sr.loadBalancer.SelectInstance(instances)
    if instance == nil {
        return nil, fmt.Errorf("no healthy instances available for service: %s", serviceName)
    }
    
    return instance, nil
}

func (sr *ServiceResolver) ResolveServiceWithVersion(serviceName, version string) (*Service, error) {
    // Get instances with specific version
    instances, err := sr.registry.GetServiceInstancesWithVersion(serviceName, version)
    if err != nil {
        return nil, fmt.Errorf("failed to get service instances: %w", err)
    }
    
    if len(instances) == 0 {
        return nil, fmt.Errorf("no instances found for service %s version %s", serviceName, version)
    }
    
    // Select instance using load balancer
    instance := sr.loadBalancer.SelectInstance(instances)
    if instance == nil {
        return nil, fmt.Errorf("no healthy instances available for service %s version %s", serviceName, version)
    }
    
    return instance, nil
}
```

## TuskLang Service Discovery Configuration

### Service Discovery Configuration
```tsk
# Service discovery configuration
service_discovery {
    # Registry configuration
    registry {
        type = "consul"
        address = "localhost:8500"
        token = ""
        datacenter = "dc1"
        scheme = "http"
        health_check_interval = "10s"
        deregister_after = "1m"
    }
    
    # Service registration
    registration {
        enabled = true
        auto_register = true
        service_name = "app"
        service_id = "app-{hostname}-{port}"
        service_address = "{hostname}"
        service_port = 8080
        service_tags = ["api", "v1"]
        service_meta = {
            "version" = "1.0.0"
            "environment" = "production"
        }
    }
    
    # Service resolution
    resolution {
        enabled = true
        cache_enabled = true
        cache_ttl = "30s"
        retry_policy {
            max_retries = 3
            backoff_delay = "1s"
            max_delay = "30s"
        }
    }
    
    # Health checking
    health_checking {
        enabled = true
        check_type = "http"
        check_path = "/health"
        check_interval = "10s"
        check_timeout = "5s"
        unhealthy_threshold = 3
        healthy_threshold = 2
    }
    
    # Load balancing
    load_balancing {
        strategy = "round_robin"
        health_aware = true
        session_persistence = false
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
        version = "v1"
        port = 8081
        tags = ["api", "user", "v1"]
        metadata = {
            "protocol" = "http"
            "timeout" = "30s"
            "retries" = "3"
        }
        health_check = {
            path = "/health"
            interval = "10s"
            timeout = "5s"
        }
        load_balancing = {
            strategy = "least_connections"
            health_aware = true
        }
    }
    
    # Order service
    order_service {
        name = "order-service"
        version = "v1"
        port = 8082
        tags = ["api", "order", "v1"]
        metadata = {
            "protocol" = "http"
            "timeout" = "30s"
            "retries" = "3"
        }
        health_check = {
            path = "/health"
            interval = "10s"
            timeout = "5s"
        }
        load_balancing = {
            strategy = "round_robin"
            health_aware = true
        }
    }
    
    # Payment service
    payment_service {
        name = "payment-service"
        version = "v1"
        port = 8083
        tags = ["api", "payment", "v1"]
        metadata = {
            "protocol" = "http"
            "timeout" = "30s"
            "retries" = "3"
        }
        health_check = {
            path = "/health"
            interval = "10s"
            timeout = "5s"
        }
        load_balancing = {
            strategy = "ip_hash"
            health_aware = true
        }
    }
}
```

## Go Service Discovery Implementation

### Service Registry Implementation
```go
// Service registry implementation with Consul
type ServiceRegistry struct {
    config *tusk.Config
    client *consul.Client
}

func NewServiceRegistry(config *tusk.Config) *ServiceRegistry {
    address := config.GetString("service_discovery.registry.address")
    
    client, err := consul.NewClient(&consul.Config{
        Address: address,
    })
    if err != nil {
        log.Fatalf("Failed to create Consul client: %v", err)
    }
    
    return &ServiceRegistry{
        config: config,
        client: client,
    }
}

func (sr *ServiceRegistry) Register(service *Service) error {
    // Create service registration
    registration := &consul.AgentServiceRegistration{
        ID:      service.ID,
        Name:    service.Name,
        Address: service.Address,
        Port:    service.Port,
        Tags:    service.Tags,
        Meta:    service.Metadata,
        Check: &consul.AgentServiceCheck{
            HTTP:                           fmt.Sprintf("http://%s:%d/health", service.Address, service.Port),
            Interval:                       "10s",
            Timeout:                        "5s",
            DeregisterCriticalServiceAfter: "1m",
        },
    }
    
    // Register service
    err := sr.client.Agent().ServiceRegister(registration)
    if err != nil {
        return fmt.Errorf("failed to register service: %w", err)
    }
    
    return nil
}

func (sr *ServiceRegistry) Deregister(serviceID string) error {
    err := sr.client.Agent().ServiceDeregister(serviceID)
    if err != nil {
        return fmt.Errorf("failed to deregister service: %w", err)
    }
    
    return nil
}

func (sr *ServiceRegistry) GetServiceInstances(serviceName string) ([]*Service, error) {
    // Query Consul for service instances
    services, _, err := sr.client.Health().Service(serviceName, "", true, nil)
    if err != nil {
        return nil, fmt.Errorf("failed to query service: %w", err)
    }
    
    var instances []*Service
    for _, service := range services {
        instance := &Service{
            ID:       service.Service.ID,
            Name:     service.Service.Service,
            Address:  service.Service.Address,
            Port:     service.Service.Port,
            Tags:     service.Service.Tags,
            Metadata: service.Service.Meta,
            Health:   service.Checks.AggregatedStatus(),
        }
        instances = append(instances, instance)
    }
    
    return instances, nil
}

func (sr *ServiceRegistry) GetServiceInstancesWithVersion(serviceName, version string) ([]*Service, error) {
    // Get all instances
    instances, err := sr.GetServiceInstances(serviceName)
    if err != nil {
        return nil, err
    }
    
    // Filter by version
    var filteredInstances []*Service
    for _, instance := range instances {
        if instance.Metadata["version"] == version {
            filteredInstances = append(filteredInstances, instance)
        }
    }
    
    return filteredInstances, nil
}
```

### Service Registration Implementation
```go
// Service registration implementation
type ServiceRegistrar struct {
    config *tusk.Config
    registry *ServiceRegistry
    service *Service
}

func NewServiceRegistrar(config *tusk.Config) *ServiceRegistrar {
    return &ServiceRegistrar{
        config:   config,
        registry: NewServiceRegistry(config),
    }
}

func (sr *ServiceRegistrar) RegisterService() error {
    // Create service instance
    service := &Service{
        ID:       sr.generateServiceID(),
        Name:     sr.config.GetString("service_discovery.registration.service_name"),
        Address:  sr.getServiceAddress(),
        Port:     sr.config.GetInt("service_discovery.registration.service_port"),
        Tags:     sr.config.GetStringSlice("service_discovery.registration.service_tags"),
        Metadata: sr.config.GetMap("service_discovery.registration.service_meta"),
        Health:   "healthy",
    }
    
    // Register with registry
    if err := sr.registry.Register(service); err != nil {
        return fmt.Errorf("failed to register service: %w", err)
    }
    
    sr.service = service
    
    // Start health check
    if err := sr.startHealthCheck(); err != nil {
        return fmt.Errorf("failed to start health check: %w", err)
    }
    
    return nil
}

func (sr *ServiceRegistrar) generateServiceID() string {
    hostname, _ := os.Hostname()
    port := sr.config.GetInt("service_discovery.registration.service_port")
    
    template := sr.config.GetString("service_discovery.registration.service_id", "app-{hostname}-{port}")
    template = strings.ReplaceAll(template, "{hostname}", hostname)
    template = strings.ReplaceAll(template, "{port}", strconv.Itoa(port))
    
    return template
}

func (sr *ServiceRegistrar) getServiceAddress() string {
    address := sr.config.GetString("service_discovery.registration.service_address", "{hostname}")
    if address == "{hostname}" {
        hostname, _ := os.Hostname()
        return hostname
    }
    return address
}

func (sr *ServiceRegistrar) startHealthCheck() error {
    // Start health check endpoint
    go func() {
        mux := http.NewServeMux()
        mux.HandleFunc("/health", sr.healthCheckHandler)
        
        port := sr.config.GetInt("service_discovery.registration.service_port")
        server := &http.Server{
            Addr:    fmt.Sprintf(":%d", port),
            Handler: mux,
        }
        
        if err := server.ListenAndServe(); err != nil {
            log.Printf("Health check server error: %v", err)
        }
    }()
    
    return nil
}

func (sr *ServiceRegistrar) healthCheckHandler(w http.ResponseWriter, r *http.Request) {
    w.Header().Set("Content-Type", "application/json")
    w.WriteHeader(http.StatusOK)
    
    response := map[string]interface{}{
        "status":    "healthy",
        "service":   sr.service.Name,
        "version":   sr.service.Metadata["version"],
        "timestamp": time.Now().UTC(),
    }
    
    json.NewEncoder(w).Encode(response)
}
```

## Advanced Service Discovery Features

### Service Mesh Integration
```go
// Service mesh integration
type ServiceMesh struct {
    config *tusk.Config
    discovery *ServiceDiscovery
}

func (sm *ServiceMesh) InjectSidecar(service *Service) error {
    // Check if service mesh is enabled
    if !sm.config.GetBool("service_discovery.service_mesh.enabled") {
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
    
    return nil
}

func (sm *ServiceMesh) injectSidecarProxy(service *Service) error {
    // Inject Envoy proxy sidecar
    // This is a simplified implementation
    return nil
}

func (sm *ServiceMesh) configureTrafficManagement(service *Service) error {
    // Configure traffic routing rules
    // This is a simplified implementation
    return nil
}
```

### Service Versioning
```go
// Service versioning implementation
type ServiceVersioning struct {
    config *tusk.Config
    registry *ServiceRegistry
}

func (sv *ServiceVersioning) DeployNewVersion(serviceName, newVersion string) error {
    // Deploy new version
    if err := sv.deployVersion(serviceName, newVersion); err != nil {
        return fmt.Errorf("failed to deploy new version: %w", err)
    }
    
    // Register new version
    if err := sv.registerVersion(serviceName, newVersion); err != nil {
        return fmt.Errorf("failed to register new version: %w", err)
    }
    
    // Update routing rules
    if err := sv.updateRoutingRules(serviceName, newVersion); err != nil {
        return fmt.Errorf("failed to update routing rules: %w", err)
    }
    
    return nil
}

func (sv *ServiceVersioning) deployVersion(serviceName, version string) error {
    // Deploy service version
    // This is a simplified implementation
    return nil
}

func (sv *ServiceVersioning) registerVersion(serviceName, version string) error {
    // Register service version with registry
    service := &Service{
        ID:       fmt.Sprintf("%s-%s", serviceName, version),
        Name:     serviceName,
        Version:  version,
        Address:  "localhost",
        Port:     8080,
        Metadata: map[string]string{"version": version},
    }
    
    return sv.registry.Register(service)
}

func (sv *ServiceVersioning) updateRoutingRules(serviceName, version string) error {
    // Update routing rules for new version
    // This is a simplified implementation
    return nil
}
```

### Service Monitoring
```go
// Service monitoring implementation
type ServiceMonitor struct {
    config *tusk.Config
    discovery *ServiceDiscovery
    metrics map[string]*ServiceMetrics
}

type ServiceMetrics struct {
    ServiceName   string
    InstanceCount int
    HealthyCount  int
    UnhealthyCount int
    ResponseTime  time.Duration
    ErrorRate     float64
    LastUpdated   time.Time
}

func (sm *ServiceMonitor) MonitorServices() error {
    // Get all services
    services, err := sm.discovery.registry.GetAllServices()
    if err != nil {
        return fmt.Errorf("failed to get services: %w", err)
    }
    
    // Monitor each service
    for _, service := range services {
        if err := sm.monitorService(service); err != nil {
            log.Printf("Failed to monitor service %s: %v", service.Name, err)
        }
    }
    
    return nil
}

func (sm *ServiceMonitor) monitorService(service *Service) error {
    // Get service instances
    instances, err := sm.discovery.registry.GetServiceInstances(service.Name)
    if err != nil {
        return fmt.Errorf("failed to get service instances: %w", err)
    }
    
    // Calculate metrics
    metrics := &ServiceMetrics{
        ServiceName:   service.Name,
        InstanceCount: len(instances),
        LastUpdated:   time.Now(),
    }
    
    for _, instance := range instances {
        if instance.Health == "passing" {
            metrics.HealthyCount++
        } else {
            metrics.UnhealthyCount++
        }
    }
    
    // Calculate error rate
    if metrics.InstanceCount > 0 {
        metrics.ErrorRate = float64(metrics.UnhealthyCount) / float64(metrics.InstanceCount)
    }
    
    // Store metrics
    sm.metrics[service.Name] = metrics
    
    return nil
}
```

## Service Discovery Tools and Utilities

### Service Discovery Client
```go
// Service discovery client implementation
type ServiceDiscoveryClient struct {
    config *tusk.Config
    resolver *ServiceResolver
    cache    *ServiceCache
}

type ServiceCache struct {
    services map[string]*CachedService
    mutex    sync.RWMutex
    ttl      time.Duration
}

type CachedService struct {
    Service   *Service
    CachedAt  time.Time
}

func (sdc *ServiceDiscoveryClient) GetService(serviceName string) (*Service, error) {
    // Check cache first
    if cached := sdc.cache.Get(serviceName); cached != nil {
        return cached, nil
    }
    
    // Resolve service
    service, err := sdc.resolver.ResolveService(serviceName)
    if err != nil {
        return nil, fmt.Errorf("failed to resolve service: %w", err)
    }
    
    // Cache service
    sdc.cache.Set(serviceName, service)
    
    return service, nil
}

func (sdc *ServiceDiscoveryClient) GetServiceWithRetry(serviceName string) (*Service, error) {
    retryPolicy := sdc.config.GetMap("service_discovery.resolution.retry_policy")
    maxRetries := int(retryPolicy["max_retries"].(float64))
    backoffDelay := retryPolicy["backoff_delay"].(string)
    maxDelay := retryPolicy["max_delay"].(string)
    
    delay, _ := time.ParseDuration(backoffDelay)
    maxDelayDuration, _ := time.ParseDuration(maxDelay)
    
    var lastErr error
    for i := 0; i < maxRetries; i++ {
        service, err := sdc.GetService(serviceName)
        if err == nil {
            return service, nil
        }
        
        lastErr = err
        
        // Calculate backoff delay
        backoff := delay * time.Duration(1<<i)
        if backoff > maxDelayDuration {
            backoff = maxDelayDuration
        }
        
        time.Sleep(backoff)
    }
    
    return nil, fmt.Errorf("failed to get service after %d retries: %w", maxRetries, lastErr)
}

func (sc *ServiceCache) Get(serviceName string) *Service {
    sc.mutex.RLock()
    defer sc.mutex.RUnlock()
    
    if cached, exists := sc.services[serviceName]; exists {
        if time.Since(cached.CachedAt) < sc.ttl {
            return cached.Service
        }
        // Remove expired cache entry
        delete(sc.services, serviceName)
    }
    
    return nil
}

func (sc *ServiceCache) Set(serviceName string, service *Service) {
    sc.mutex.Lock()
    defer sc.mutex.Unlock()
    
    sc.services[serviceName] = &CachedService{
        Service:  service,
        CachedAt: time.Now(),
    }
}
```

### Service Discovery Health Check
```go
// Service discovery health check implementation
type ServiceDiscoveryHealthCheck struct {
    config *tusk.Config
    discovery *ServiceDiscovery
}

func (sdhc *ServiceDiscoveryHealthCheck) CheckHealth() error {
    // Check registry connectivity
    if err := sdhc.checkRegistryHealth(); err != nil {
        return fmt.Errorf("registry health check failed: %w", err)
    }
    
    // Check service resolution
    if err := sdhc.checkServiceResolution(); err != nil {
        return fmt.Errorf("service resolution health check failed: %w", err)
    }
    
    return nil
}

func (sdhc *ServiceDiscoveryHealthCheck) checkRegistryHealth() error {
    // Check if registry is accessible
    // This is a simplified implementation
    return nil
}

func (sdhc *ServiceDiscoveryHealthCheck) checkServiceResolution() error {
    // Test service resolution
    testService := "test-service"
    _, err := sdhc.discovery.resolver.ResolveService(testService)
    if err != nil && !strings.Contains(err.Error(), "no instances found") {
        return fmt.Errorf("service resolution test failed: %w", err)
    }
    
    return nil
}
```

## Validation and Error Handling

### Service Discovery Configuration Validation
```go
// Validate service discovery configuration
func ValidateServiceDiscoveryConfig(config *tusk.Config) error {
    if config == nil {
        return errors.New("service discovery config cannot be nil")
    }
    
    // Validate registry configuration
    if !config.Has("service_discovery.registry") {
        return errors.New("missing registry configuration")
    }
    
    // Validate registration configuration
    if !config.Has("service_discovery.registration") {
        return errors.New("missing registration configuration")
    }
    
    return nil
}
```

### Error Handling
```go
// Handle service discovery errors gracefully
func handleServiceDiscoveryError(err error, context string) {
    log.Printf("Service discovery error in %s: %v", context, err)
    
    // Log additional context if available
    if sdErr, ok := err.(*ServiceDiscoveryError); ok {
        log.Printf("Service discovery context: %s", sdErr.Context)
    }
}
```

## Performance Considerations

### Service Discovery Performance Optimization
```go
// Optimize service discovery performance
type ServiceDiscoveryOptimizer struct {
    config *tusk.Config
}

func (sdo *ServiceDiscoveryOptimizer) OptimizePerformance() error {
    // Enable caching
    if sdo.config.GetBool("service_discovery.resolution.cache_enabled") {
        sdo.setupCaching()
    }
    
    // Enable connection pooling
    if sdo.config.GetBool("service_discovery.performance.connection_pooling") {
        sdo.setupConnectionPooling()
    }
    
    // Optimize resolution
    if err := sdo.optimizeResolution(); err != nil {
        return fmt.Errorf("failed to optimize resolution: %w", err)
    }
    
    return nil
}

func (sdo *ServiceDiscoveryOptimizer) setupCaching() {
    // Setup service resolution caching
    // This is a simplified implementation
}

func (sdo *ServiceDiscoveryOptimizer) setupConnectionPooling() {
    // Setup connection pooling for registry
    // This is a simplified implementation
}
```

## Service Discovery Notes

- **Service Registration**: Register services with proper metadata
- **Health Checking**: Implement comprehensive health checking
- **Service Resolution**: Use efficient service resolution strategies
- **Caching**: Implement caching for better performance
- **Service Monitoring**: Monitor service health and availability
- **Service Versioning**: Handle service versioning properly
- **Service Mesh**: Integrate with service mesh when needed
- **Error Handling**: Handle service discovery errors gracefully

## Best Practices

1. **Service Registration**: Register services with proper metadata
2. **Health Checking**: Implement robust health checking
3. **Service Resolution**: Use efficient resolution strategies
4. **Caching**: Implement caching for better performance
5. **Service Monitoring**: Monitor service health comprehensively
6. **Service Versioning**: Handle versioning properly
7. **Service Mesh**: Integrate with service mesh when needed
8. **Error Handling**: Handle errors gracefully

## Integration with TuskLang

```go
// Load service discovery configuration from TuskLang
func LoadServiceDiscoveryConfig(configPath string) (*tusk.Config, error) {
    config, err := tusk.LoadConfig(configPath)
    if err != nil {
        return nil, fmt.Errorf("failed to load service discovery config: %w", err)
    }
    
    // Validate service discovery configuration
    if err := ValidateServiceDiscoveryConfig(config); err != nil {
        return nil, fmt.Errorf("invalid service discovery config: %w", err)
    }
    
    return config, nil
}
```

This service discovery guide provides comprehensive service discovery capabilities for your Go applications using TuskLang. Remember, good service discovery is essential for dynamic, scalable microservices architectures. 