# Load Balancing

TuskLang provides powerful load balancing capabilities that enable scalable, high-availability applications. This guide covers comprehensive load balancing strategies for Go applications.

## Load Balancing Philosophy

### Distribution-First Design
```go
// Distribution-first design with TuskLang
type LoadBalancer struct {
    config *tusk.Config
    strategy LoadBalancingStrategy
    healthChecker *HealthChecker
    servers []*Server
}

func NewLoadBalancer(config *tusk.Config) *LoadBalancer {
    return &LoadBalancer{
        config:       config,
        strategy:     NewLoadBalancingStrategy(config),
        healthChecker: NewHealthChecker(config),
        servers:      make([]*Server, 0),
    }
}

// RouteRequest routes a request to an appropriate server
func (lb *LoadBalancer) RouteRequest(req *http.Request) (*Server, error) {
    // Get healthy servers
    healthyServers := lb.getHealthyServers()
    if len(healthyServers) == 0 {
        return nil, errors.New("no healthy servers available")
    }
    
    // Select server using strategy
    server := lb.strategy.SelectServer(healthyServers, req)
    if server == nil {
        return nil, errors.New("no server selected")
    }
    
    return server, nil
}

type Server struct {
    ID       string
    Address  string
    Port     int
    Weight   int
    Health   string
    Load     float64
    LastSeen time.Time
}
```

### Health-Aware Routing
```go
// Health-aware routing with automatic failover
type HealthAwareLoadBalancer struct {
    config *tusk.Config
    balancer *LoadBalancer
    healthChecker *HealthChecker
}

func (halb *HealthAwareLoadBalancer) Start() error {
    // Start health checking
    if err := halb.healthChecker.Start(); err != nil {
        return fmt.Errorf("failed to start health checker: %w", err)
    }
    
    // Start load balancer
    if err := halb.balancer.Start(); err != nil {
        return fmt.Errorf("failed to start load balancer: %w", err)
    }
    
    return nil
}

func (halb *HealthAwareLoadBalancer) RouteRequest(req *http.Request) (*Server, error) {
    // Get only healthy servers
    healthyServers := halb.getHealthyServers()
    if len(healthyServers) == 0 {
        return nil, errors.New("no healthy servers available")
    }
    
    // Route to healthy server
    return halb.balancer.RouteRequest(req)
}

func (halb *HealthAwareLoadBalancer) getHealthyServers() []*Server {
    var healthyServers []*Server
    
    for _, server := range halb.balancer.servers {
        if halb.healthChecker.IsHealthy(server) {
            healthyServers = append(healthyServers, server)
        }
    }
    
    return healthyServers
}
```

## TuskLang Load Balancing Configuration

### Load Balancer Configuration
```tsk
# Load balancing configuration
load_balancing {
    # Load balancer settings
    balancer {
        type = "round_robin"
        port = 8080
        max_connections = 10000
        connection_timeout = "30s"
        read_timeout = "30s"
        write_timeout = "30s"
    }
    
    # Server pool configuration
    server_pool {
        # Server definitions
        servers = [
            {
                id = "server-1"
                address = "192.168.1.10"
                port = 8081
                weight = 100
                health_check = {
                    enabled = true
                    path = "/health"
                    interval = "10s"
                    timeout = "5s"
                    unhealthy_threshold = 3
                    healthy_threshold = 2
                }
            },
            {
                id = "server-2"
                address = "192.168.1.11"
                port = 8081
                weight = 100
                health_check = {
                    enabled = true
                    path = "/health"
                    interval = "10s"
                    timeout = "5s"
                    unhealthy_threshold = 3
                    healthy_threshold = 2
                }
            }
        ]
        
        # Pool settings
        settings = {
            min_servers = 1
            max_servers = 10
            auto_scaling = true
            scale_up_threshold = 80
            scale_down_threshold = 20
        }
    }
    
    # Load balancing strategies
    strategies {
        # Round robin
        round_robin {
            enabled = true
            weighted = true
        }
        
        # Least connections
        least_connections {
            enabled = true
            consider_health = true
        }
        
        # IP hash
        ip_hash {
            enabled = true
            hash_function = "md5"
        }
        
        # Least response time
        least_response_time {
            enabled = true
            sample_size = 100
        }
    }
    
    # Session persistence
    session_persistence {
        enabled = true
        type = "cookie"
        cookie_name = "session_id"
        timeout = "30m"
    }
}
```

### Health Check Configuration
```tsk
# Health check configuration
health_checking {
    # Health check settings
    settings {
        enabled = true
        interval = "10s"
        timeout = "5s"
        unhealthy_threshold = 3
        healthy_threshold = 2
        grace_period = "30s"
    }
    
    # Health check types
    checks {
        # HTTP health check
        http {
            enabled = true
            path = "/health"
            method = "GET"
            expected_status = 200
            expected_body = "healthy"
        }
        
        # TCP health check
        tcp {
            enabled = true
            port = 8081
            timeout = "5s"
        }
        
        # Custom health check
        custom {
            enabled = false
            command = "curl -f http://localhost:8081/health"
            timeout = "10s"
        }
    }
    
    # Health check reporting
    reporting {
        enabled = true
        metrics = true
        alerts = true
        log_level = "info"
    }
}
```

## Go Load Balancing Implementation

### Round Robin Load Balancer
```go
// Round robin load balancer implementation
type RoundRobinLoadBalancer struct {
    config *tusk.Config
    servers []*Server
    current int
    mutex   sync.Mutex
}

func NewRoundRobinLoadBalancer(config *tusk.Config) *RoundRobinLoadBalancer {
    return &RoundRobinLoadBalancer{
        config:  config,
        servers: make([]*Server, 0),
        current: 0,
    }
}

func (rrlb *RoundRobinLoadBalancer) SelectServer(servers []*Server, req *http.Request) *Server {
    rrlb.mutex.Lock()
    defer rrlb.mutex.Unlock()
    
    if len(servers) == 0 {
        return nil
    }
    
    // Select next server in round-robin fashion
    server := servers[rrlb.current]
    rrlb.current = (rrlb.current + 1) % len(servers)
    
    return server
}

func (rrlb *RoundRobinLoadBalancer) AddServer(server *Server) {
    rrlb.mutex.Lock()
    defer rrlb.mutex.Unlock()
    
    rrlb.servers = append(rrlb.servers, server)
}

func (rrlb *RoundRobinLoadBalancer) RemoveServer(serverID string) {
    rrlb.mutex.Lock()
    defer rrlb.mutex.Unlock()
    
    for i, server := range rrlb.servers {
        if server.ID == serverID {
            rrlb.servers = append(rrlb.servers[:i], rrlb.servers[i+1:]...)
            break
        }
    }
}
```

### Least Connections Load Balancer
```go
// Least connections load balancer implementation
type LeastConnectionsLoadBalancer struct {
    config *tusk.Config
    servers map[string]*Server
    mutex   sync.RWMutex
}

func NewLeastConnectionsLoadBalancer(config *tusk.Config) *LeastConnectionsLoadBalancer {
    return &LeastConnectionsLoadBalancer{
        config:  config,
        servers: make(map[string]*Server),
    }
}

func (lclb *LeastConnectionsLoadBalancer) SelectServer(servers []*Server, req *http.Request) *Server {
    lclb.mutex.RLock()
    defer lclb.mutex.RUnlock()
    
    if len(servers) == 0 {
        return nil
    }
    
    // Find server with least connections
    var selectedServer *Server
    minConnections := int64(math.MaxInt64)
    
    for _, server := range servers {
        if server.Connections < minConnections {
            minConnections = server.Connections
            selectedServer = server
        }
    }
    
    return selectedServer
}

func (lclb *LeastConnectionsLoadBalancer) IncrementConnections(serverID string) {
    lclb.mutex.Lock()
    defer lclb.mutex.Unlock()
    
    if server, exists := lclb.servers[serverID]; exists {
        atomic.AddInt64(&server.Connections, 1)
    }
}

func (lclb *LeastConnectionsLoadBalancer) DecrementConnections(serverID string) {
    lclb.mutex.Lock()
    defer lclb.mutex.Unlock()
    
    if server, exists := lclb.servers[serverID]; exists {
        atomic.AddInt64(&server.Connections, -1)
    }
}
```

### Health Checker Implementation
```go
// Health checker implementation
type HealthChecker struct {
    config *tusk.Config
    client *http.Client
    checks map[string]*HealthCheck
}

type HealthCheck struct {
    ServerID string
    URL      string
    Interval time.Duration
    Timeout  time.Duration
    Healthy  bool
    LastCheck time.Time
}

func NewHealthChecker(config *tusk.Config) *HealthChecker {
    timeout := config.GetDuration("health_checking.settings.timeout", 5*time.Second)
    
    return &HealthChecker{
        config: config,
        client: &http.Client{Timeout: timeout},
        checks: make(map[string]*HealthCheck),
    }
}

func (hc *HealthChecker) Start() error {
    interval := hc.config.GetDuration("health_checking.settings.interval", 10*time.Second)
    
    // Start health checking goroutine
    go func() {
        ticker := time.NewTicker(interval)
        defer ticker.Stop()
        
        for {
            select {
            case <-ticker.C:
                hc.performHealthChecks()
            }
        }
    }()
    
    return nil
}

func (hc *HealthChecker) performHealthChecks() {
    for _, check := range hc.checks {
        go hc.performHealthCheck(check)
    }
}

func (hc *HealthChecker) performHealthCheck(check *HealthCheck) {
    // Perform HTTP health check
    resp, err := hc.client.Get(check.URL)
    if err != nil {
        check.Healthy = false
        check.LastCheck = time.Now()
        return
    }
    defer resp.Body.Close()
    
    // Check response status
    expectedStatus := hc.config.GetInt("health_checking.checks.http.expected_status", 200)
    check.Healthy = resp.StatusCode == expectedStatus
    check.LastCheck = time.Now()
}

func (hc *HealthChecker) IsHealthy(server *Server) bool {
    if check, exists := hc.checks[server.ID]; exists {
        return check.Healthy
    }
    return true // Assume healthy if no check configured
}
```

## Advanced Load Balancing Features

### Session Persistence
```go
// Session persistence implementation
type SessionPersistence struct {
    config *tusk.Config
    sessions map[string]*Session
    mutex    sync.RWMutex
}

type Session struct {
    ID       string
    ServerID string
    Created  time.Time
    LastUsed time.Time
}

func (sp *SessionPersistence) GetServerForSession(sessionID string) string {
    sp.mutex.RLock()
    defer sp.mutex.RUnlock()
    
    if session, exists := sp.sessions[sessionID]; exists {
        // Update last used time
        session.LastUsed = time.Now()
        return session.ServerID
    }
    
    return ""
}

func (sp *SessionPersistence) BindSessionToServer(sessionID, serverID string) {
    sp.mutex.Lock()
    defer sp.mutex.Unlock()
    
    sp.sessions[sessionID] = &Session{
        ID:       sessionID,
        ServerID: serverID,
        Created:  time.Now(),
        LastUsed: time.Now(),
    }
}

func (sp *SessionPersistence) CleanupExpiredSessions() {
    sp.mutex.Lock()
    defer sp.mutex.Unlock()
    
    timeout := sp.config.GetDuration("load_balancing.session_persistence.timeout", 30*time.Minute)
    cutoff := time.Now().Add(-timeout)
    
    for sessionID, session := range sp.sessions {
        if session.LastUsed.Before(cutoff) {
            delete(sp.sessions, sessionID)
        }
    }
}
```

### Auto Scaling
```go
// Auto scaling implementation
type AutoScaler struct {
    config *tusk.Config
    balancer *LoadBalancer
    metrics  *MetricsCollector
}

func (as *AutoScaler) Start() error {
    interval := as.config.GetDuration("load_balancing.server_pool.settings.scale_interval", 60*time.Second)
    
    // Start auto scaling goroutine
    go func() {
        ticker := time.NewTicker(interval)
        defer ticker.Stop()
        
        for {
            select {
            case <-ticker.C:
                as.checkScaling()
            }
        }
    }()
    
    return nil
}

func (as *AutoScaler) checkScaling() {
    // Get current load
    currentLoad := as.metrics.GetAverageLoad()
    
    // Check if scaling is needed
    scaleUpThreshold := as.config.GetFloat("load_balancing.server_pool.settings.scale_up_threshold", 80.0)
    scaleDownThreshold := as.config.GetFloat("load_balancing.server_pool.settings.scale_down_threshold", 20.0)
    
    if currentLoad > scaleUpThreshold {
        as.scaleUp()
    } else if currentLoad < scaleDownThreshold {
        as.scaleDown()
    }
}

func (as *AutoScaler) scaleUp() {
    // Add new server
    server := as.createNewServer()
    if server != nil {
        as.balancer.AddServer(server)
        log.Printf("Scaled up: added server %s", server.ID)
    }
}

func (as *AutoScaler) scaleDown() {
    // Remove least loaded server
    server := as.getLeastLoadedServer()
    if server != nil {
        as.balancer.RemoveServer(server.ID)
        log.Printf("Scaled down: removed server %s", server.ID)
    }
}

func (as *AutoScaler) createNewServer() *Server {
    // Create new server instance
    // This is a simplified implementation
    return &Server{
        ID:      fmt.Sprintf("server-%d", time.Now().Unix()),
        Address: "192.168.1.100",
        Port:    8081,
        Weight:  100,
        Health:  "healthy",
    }
}
```

### Load Balancing Metrics
```go
// Load balancing metrics collection
type MetricsCollector struct {
    config *tusk.Config
    metrics map[string]*ServerMetrics
    mutex   sync.RWMutex
}

type ServerMetrics struct {
    ServerID        string
    Requests        int64
    Responses       int64
    Errors          int64
    ResponseTime    time.Duration
    Connections     int64
    LastUpdated     time.Time
}

func (mc *MetricsCollector) RecordRequest(serverID string) {
    mc.mutex.Lock()
    defer mc.mutex.Unlock()
    
    metrics := mc.getOrCreateMetrics(serverID)
    atomic.AddInt64(&metrics.Requests, 1)
    metrics.LastUpdated = time.Now()
}

func (mc *MetricsCollector) RecordResponse(serverID string, responseTime time.Duration) {
    mc.mutex.Lock()
    defer mc.mutex.Unlock()
    
    metrics := mc.getOrCreateMetrics(serverID)
    atomic.AddInt64(&metrics.Responses, 1)
    metrics.ResponseTime = responseTime
    metrics.LastUpdated = time.Now()
}

func (mc *MetricsCollector) RecordError(serverID string) {
    mc.mutex.Lock()
    defer mc.mutex.Unlock()
    
    metrics := mc.getOrCreateMetrics(serverID)
    atomic.AddInt64(&metrics.Errors, 1)
    metrics.LastUpdated = time.Now()
}

func (mc *MetricsCollector) GetAverageLoad() float64 {
    mc.mutex.RLock()
    defer mc.mutex.RUnlock()
    
    if len(mc.metrics) == 0 {
        return 0.0
    }
    
    var totalLoad float64
    for _, metrics := range mc.metrics {
        if metrics.Requests > 0 {
            load := float64(metrics.Responses) / float64(metrics.Requests) * 100
            totalLoad += load
        }
    }
    
    return totalLoad / float64(len(mc.metrics))
}

func (mc *MetricsCollector) getOrCreateMetrics(serverID string) *ServerMetrics {
    if metrics, exists := mc.metrics[serverID]; exists {
        return metrics
    }
    
    metrics := &ServerMetrics{ServerID: serverID}
    mc.metrics[serverID] = metrics
    return metrics
}
```

## Load Balancing Tools and Utilities

### Load Balancer Proxy
```go
// Load balancer proxy implementation
type LoadBalancerProxy struct {
    config *tusk.Config
    balancer *LoadBalancer
    server   *http.Server
}

func (lbp *LoadBalancerProxy) Start() error {
    mux := http.NewServeMux()
    mux.HandleFunc("/", lbp.handleRequest)
    
    port := lbp.config.GetInt("load_balancing.balancer.port", 8080)
    lbp.server = &http.Server{
        Addr:    fmt.Sprintf(":%d", port),
        Handler: mux,
    }
    
    log.Printf("Load balancer starting on port %d", port)
    return lbp.server.ListenAndServe()
}

func (lbp *LoadBalancerProxy) handleRequest(w http.ResponseWriter, r *http.Request) {
    start := time.Now()
    
    // Select server
    server, err := lbp.balancer.RouteRequest(r)
    if err != nil {
        http.Error(w, "No servers available", http.StatusServiceUnavailable)
        return
    }
    
    // Forward request to server
    if err := lbp.forwardRequest(server, w, r); err != nil {
        http.Error(w, "Failed to forward request", http.StatusBadGateway)
        return
    }
    
    // Record metrics
    responseTime := time.Since(start)
    lbp.recordMetrics(server.ID, responseTime)
}

func (lbp *LoadBalancerProxy) forwardRequest(server *Server, w http.ResponseWriter, r *http.Request) error {
    // Create target URL
    targetURL := fmt.Sprintf("http://%s:%d%s", server.Address, server.Port, r.URL.Path)
    
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
    resp, err := http.DefaultClient.Do(proxyReq)
    if err != nil {
        return fmt.Errorf("failed to forward request: %w", err)
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

### Load Balancer Configuration Manager
```go
// Load balancer configuration manager
type LoadBalancerConfigManager struct {
    config *tusk.Config
    balancer *LoadBalancer
}

func (lbcm *LoadBalancerConfigManager) ReloadConfiguration() error {
    // Reload configuration from file
    newConfig, err := tusk.LoadConfig("load_balancer.tsk")
    if err != nil {
        return fmt.Errorf("failed to reload configuration: %w", err)
    }
    
    // Update load balancer configuration
    if err := lbcm.updateBalancerConfig(newConfig); err != nil {
        return fmt.Errorf("failed to update balancer configuration: %w", err)
    }
    
    return nil
}

func (lbcm *LoadBalancerConfigManager) updateBalancerConfig(config *tusk.Config) error {
    // Update server pool
    if err := lbcm.updateServerPool(config); err != nil {
        return fmt.Errorf("failed to update server pool: %w", err)
    }
    
    // Update load balancing strategy
    if err := lbcm.updateStrategy(config); err != nil {
        return fmt.Errorf("failed to update strategy: %w", err)
    }
    
    return nil
}

func (lbcm *LoadBalancerConfigManager) updateServerPool(config *tusk.Config) error {
    // Get server configurations
    servers := config.GetArray("load_balancing.server_pool.servers")
    
    // Update server pool
    for _, serverConfig := range servers {
        if config, ok := serverConfig.(map[string]interface{}); ok {
            server := &Server{
                ID:      config["id"].(string),
                Address: config["address"].(string),
                Port:    int(config["port"].(float64)),
                Weight:  int(config["weight"].(float64)),
                Health:  "healthy",
            }
            
            lbcm.balancer.AddServer(server)
        }
    }
    
    return nil
}
```

## Validation and Error Handling

### Load Balancer Configuration Validation
```go
// Validate load balancer configuration
func ValidateLoadBalancerConfig(config *tusk.Config) error {
    if config == nil {
        return errors.New("load balancer config cannot be nil")
    }
    
    // Validate balancer configuration
    if !config.Has("load_balancing.balancer") {
        return errors.New("missing balancer configuration")
    }
    
    // Validate server pool configuration
    if !config.Has("load_balancing.server_pool") {
        return errors.New("missing server pool configuration")
    }
    
    return nil
}
```

### Error Handling
```go
// Handle load balancer errors gracefully
func handleLoadBalancerError(err error, context string) {
    log.Printf("Load balancer error in %s: %v", context, err)
    
    // Log additional context if available
    if lbErr, ok := err.(*LoadBalancerError); ok {
        log.Printf("Load balancer context: %s", lbErr.Context)
    }
}
```

## Performance Considerations

### Load Balancer Performance Optimization
```go
// Optimize load balancer performance
type LoadBalancerOptimizer struct {
    config *tusk.Config
}

func (lbo *LoadBalancerOptimizer) OptimizePerformance() error {
    // Enable connection pooling
    if lbo.config.GetBool("load_balancing.performance.connection_pooling") {
        lbo.setupConnectionPooling()
    }
    
    // Enable request buffering
    if lbo.config.GetBool("load_balancing.performance.request_buffering") {
        lbo.setupRequestBuffering()
    }
    
    // Optimize routing
    if err := lbo.optimizeRouting(); err != nil {
        return fmt.Errorf("failed to optimize routing: %w", err)
    }
    
    return nil
}

func (lbo *LoadBalancerOptimizer) setupConnectionPooling() {
    // Setup connection pooling for better performance
    // This is a simplified implementation
}

func (lbo *LoadBalancerOptimizer) setupRequestBuffering() {
    // Setup request buffering for better throughput
    // This is a simplified implementation
}
```

## Load Balancing Notes

- **Health Checking**: Implement comprehensive health checking
- **Session Persistence**: Use session persistence for stateful applications
- **Auto Scaling**: Implement auto scaling for dynamic workloads
- **Metrics Collection**: Collect comprehensive load balancing metrics
- **Configuration Management**: Manage load balancer configuration dynamically
- **Error Handling**: Handle load balancer errors gracefully
- **Performance Optimization**: Optimize load balancer performance
- **Monitoring**: Monitor load balancer health and performance

## Best Practices

1. **Health Checking**: Implement robust health checking
2. **Session Persistence**: Use session persistence when needed
3. **Auto Scaling**: Implement auto scaling for dynamic workloads
4. **Metrics Collection**: Collect comprehensive metrics
5. **Configuration Management**: Manage configuration dynamically
6. **Error Handling**: Handle errors gracefully
7. **Performance Optimization**: Optimize for performance
8. **Monitoring**: Monitor health and performance

## Integration with TuskLang

```go
// Load load balancer configuration from TuskLang
func LoadLoadBalancerConfig(configPath string) (*tusk.Config, error) {
    config, err := tusk.LoadConfig(configPath)
    if err != nil {
        return nil, fmt.Errorf("failed to load load balancer config: %w", err)
    }
    
    // Validate load balancer configuration
    if err := ValidateLoadBalancerConfig(config); err != nil {
        return nil, fmt.Errorf("invalid load balancer config: %w", err)
    }
    
    return config, nil
}
```

This load balancing guide provides comprehensive load balancing capabilities for your Go applications using TuskLang. Remember, good load balancing is essential for scalable, high-availability applications. 