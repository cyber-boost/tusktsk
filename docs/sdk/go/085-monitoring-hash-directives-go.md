# Monitoring Hash Directives in TuskLang for Go

## Overview

Monitoring hash directives in TuskLang provide powerful monitoring and observability configuration capabilities directly in your configuration files. These directives enable you to define sophisticated monitoring strategies, metrics collection, alerting policies, and observability patterns with Go integration for production-ready applications.

## Basic Monitoring Directive Syntax

```go
// TuskLang monitoring directive
#monitoring {
    metrics: {
        prometheus: {
            enabled: true
            port: 9090
            path: "/metrics"
            namespace: "myapp"
            labels: {
                environment: "@env('ENV')"
                version: "@env('VERSION')"
            }
        }
        
        custom: {
            enabled: true
            interval: "30s"
            storage: "influxdb"
            retention: "30d"
        }
    }
    
    health_checks: {
        liveness: {
            enabled: true
            path: "/health/live"
            timeout: "5s"
            checks: ["database", "redis", "external_api"]
        }
        
        readiness: {
            enabled: true
            path: "/health/ready"
            timeout: "10s"
            checks: ["database", "redis", "cache", "queue"]
        }
        
        startup: {
            enabled: true
            path: "/health/startup"
            timeout: "30s"
            checks: ["database_migration", "cache_warmup"]
        }
    }
    
    logging: {
        structured: {
            enabled: true
            level: "@env('LOG_LEVEL', 'info')"
            format: "json"
            output: "stdout"
            fields: {
                service: "myapp"
                environment: "@env('ENV')"
            }
        }
        
        file: {
            enabled: true
            path: "/var/log/myapp"
            max_size: "100MB"
            max_age: "7d"
            max_backups: 5
        }
    }
    
    tracing: {
        jaeger: {
            enabled: true
            endpoint: "@env('JAEGER_ENDPOINT')"
            service_name: "myapp"
            sampling_rate: 0.1
        }
        
        zipkin: {
            enabled: true
            endpoint: "@env('ZIPKIN_ENDPOINT')"
            service_name: "myapp"
        }
    }
    
    alerting: {
        prometheus: {
            enabled: true
            rules: {
                high_error_rate: {
                    condition: "rate(http_requests_total{status=~\"5..\"}[5m]) > 0.1"
                    severity: "critical"
                    message: "High error rate detected"
                }
                
                high_latency: {
                    condition: "histogram_quantile(0.95, http_request_duration_seconds) > 1"
                    severity: "warning"
                    message: "High latency detected"
                }
            }
        }
        
        slack: {
            enabled: true
            webhook: "@env('SLACK_WEBHOOK')"
            channel: "#alerts"
        }
    }
}
```

## Go Integration

```go
package main

import (
    "context"
    "fmt"
    "log"
    "net/http"
    "os"
    "time"
    
    "github.com/prometheus/client_golang/prometheus"
    "github.com/prometheus/client_golang/prometheus/promhttp"
    "go.uber.org/zap"
    "github.com/tusklang/go-sdk"
)

type MonitoringDirective struct {
    Metrics     map[string]MetricConfig     `tsk:"metrics"`
    HealthChecks map[string]HealthCheck     `tsk:"health_checks"`
    Logging     map[string]LoggingConfig    `tsk:"logging"`
    Tracing     map[string]TracingConfig    `tsk:"tracing"`
    Alerting    map[string]AlertingConfig   `tsk:"alerting"`
}

type MetricConfig struct {
    Enabled   bool                   `tsk:"enabled"`
    Config    map[string]interface{} `tsk:",inline"`
}

type HealthCheck struct {
    Enabled bool     `tsk:"enabled"`
    Path    string   `tsk:"path"`
    Timeout string   `tsk:"timeout"`
    Checks  []string `tsk:"checks"`
}

type LoggingConfig struct {
    Enabled bool                   `tsk:"enabled"`
    Config  map[string]interface{} `tsk:",inline"`
}

type TracingConfig struct {
    Enabled bool                   `tsk:"enabled"`
    Config  map[string]interface{} `tsk:",inline"`
}

type AlertingConfig struct {
    Enabled bool                   `tsk:"enabled"`
    Config  map[string]interface{} `tsk:",inline"`
}

type MonitoringManager struct {
    directive    MonitoringDirective
    metrics      *MetricsCollector
    healthChecks *HealthChecker
    logger       *zap.Logger
    tracer       *Tracer
    alerter      *Alerter
}

type MetricsCollector struct {
    httpRequestsTotal   *prometheus.CounterVec
    httpRequestDuration *prometheus.HistogramVec
    httpRequestsInFlight *prometheus.GaugeVec
    customMetrics       map[string]prometheus.Collector
}

type HealthChecker struct {
    checks map[string]HealthCheckFunc
    cache  map[string]HealthStatus
    mu     sync.RWMutex
}

type HealthCheckFunc func() error

type HealthStatus struct {
    Status    string    `json:"status"`
    Message   string    `json:"message,omitempty"`
    Timestamp time.Time `json:"timestamp"`
}

type Tracer struct {
    enabled bool
    // Tracing implementation would go here
}

type Alerter struct {
    prometheus *PrometheusAlerter
    slack      *SlackAlerter
}

type PrometheusAlerter struct {
    rules map[string]AlertRule
}

type AlertRule struct {
    Condition string `json:"condition"`
    Severity  string `json:"severity"`
    Message   string `json:"message"`
}

type SlackAlerter struct {
    webhook string
    channel string
}

func main() {
    // Load monitoring configuration
    config, err := tusk.LoadFile("monitoring-config.tsk")
    if err != nil {
        log.Fatalf("Error loading monitoring config: %v", err)
    }
    
    var monitoringDirective MonitoringDirective
    if err := config.Get("#monitoring", &monitoringDirective); err != nil {
        log.Fatalf("Error parsing monitoring directive: %v", err)
    }
    
    // Initialize monitoring manager
    monitoringManager := NewMonitoringManager(monitoringDirective)
    defer monitoringManager.Close()
    
    // Setup HTTP server with monitoring
    mux := http.NewServeMux()
    
    // Add monitoring endpoints
    monitoringManager.SetupEndpoints(mux)
    
    // Add application endpoints
    mux.HandleFunc("/api/users", monitoringManager.withMonitoring(handleUsers))
    mux.HandleFunc("/api/products", monitoringManager.withMonitoring(handleProducts))
    
    log.Println("Server starting on :8080")
    log.Fatal(http.ListenAndServe(":8080", mux))
}

func NewMonitoringManager(directive MonitoringDirective) *MonitoringManager {
    manager := &MonitoringManager{
        directive: directive,
    }
    
    // Initialize metrics
    if prometheus, exists := directive.Metrics["prometheus"]; exists && prometheus.Enabled {
        manager.metrics = manager.createMetricsCollector(prometheus.Config)
    }
    
    // Initialize health checks
    manager.healthChecks = manager.createHealthChecker(directive.HealthChecks)
    
    // Initialize logging
    if structured, exists := directive.Logging["structured"]; exists && structured.Enabled {
        manager.logger = manager.createLogger(structured.Config)
    }
    
    // Initialize tracing
    if jaeger, exists := directive.Tracing["jaeger"]; exists && jaeger.Enabled {
        manager.tracer = manager.createTracer(jaeger.Config)
    }
    
    // Initialize alerting
    manager.alerter = manager.createAlerter(directive.Alerting)
    
    return manager
}

func (mm *MonitoringManager) createMetricsCollector(config map[string]interface{}) *MetricsCollector {
    namespace := config["namespace"].(string)
    
    collector := &MetricsCollector{
        customMetrics: make(map[string]prometheus.Collector),
    }
    
    // HTTP request metrics
    collector.httpRequestsTotal = prometheus.NewCounterVec(
        prometheus.CounterOpts{
            Namespace: namespace,
            Name:      "http_requests_total",
            Help:      "Total number of HTTP requests",
        },
        []string{"method", "path", "status"},
    )
    
    collector.httpRequestDuration = prometheus.NewHistogramVec(
        prometheus.HistogramOpts{
            Namespace: namespace,
            Name:      "http_request_duration_seconds",
            Help:      "HTTP request duration in seconds",
            Buckets:   prometheus.DefBuckets,
        },
        []string{"method", "path"},
    )
    
    collector.httpRequestsInFlight = prometheus.NewGaugeVec(
        prometheus.GaugeOpts{
            Namespace: namespace,
            Name:      "http_requests_in_flight",
            Help:      "Number of HTTP requests currently in flight",
        },
        []string{"method", "path"},
    )
    
    // Register metrics
    prometheus.MustRegister(
        collector.httpRequestsTotal,
        collector.httpRequestDuration,
        collector.httpRequestsInFlight,
    )
    
    return collector
}

func (mm *MonitoringManager) createHealthChecker(healthChecks map[string]HealthCheck) *HealthChecker {
    checker := &HealthChecker{
        checks: make(map[string]HealthCheckFunc),
        cache:  make(map[string]HealthStatus),
    }
    
    // Register health check functions
    checker.checks["database"] = mm.checkDatabase
    checker.checks["redis"] = mm.checkRedis
    checker.checks["external_api"] = mm.checkExternalAPI
    checker.checks["cache"] = mm.checkCache
    checker.checks["queue"] = mm.checkQueue
    checker.checks["database_migration"] = mm.checkDatabaseMigration
    checker.checks["cache_warmup"] = mm.checkCacheWarmup
    
    return checker
}

func (mm *MonitoringManager) createLogger(config map[string]interface{}) *zap.Logger {
    level := config["level"].(string)
    format := config["format"].(string)
    
    var zapConfig zap.Config
    
    switch level {
    case "debug":
        zapConfig.Level = zap.NewAtomicLevelAt(zap.DebugLevel)
    case "info":
        zapConfig.Level = zap.NewAtomicLevelAt(zap.InfoLevel)
    case "warn":
        zapConfig.Level = zap.NewAtomicLevelAt(zap.WarnLevel)
    case "error":
        zapConfig.Level = zap.NewAtomicLevelAt(zap.ErrorLevel)
    default:
        zapConfig.Level = zap.NewAtomicLevelAt(zap.InfoLevel)
    }
    
    if format == "json" {
        zapConfig.Encoding = "json"
    } else {
        zapConfig.Encoding = "console"
    }
    
    zapConfig.OutputPaths = []string{"stdout"}
    zapConfig.ErrorOutputPaths = []string{"stderr"}
    
    logger, err := zapConfig.Build()
    if err != nil {
        log.Fatalf("Error creating logger: %v", err)
    }
    
    return logger
}

func (mm *MonitoringManager) createTracer(config map[string]interface{}) *Tracer {
    return &Tracer{
        enabled: true,
    }
}

func (mm *MonitoringManager) createAlerter(alerting map[string]AlertingConfig) *Alerter {
    alerter := &Alerter{}
    
    if prometheus, exists := alerting["prometheus"]; exists && prometheus.Enabled {
        alerter.prometheus = mm.createPrometheusAlerter(prometheus.Config)
    }
    
    if slack, exists := alerting["slack"]; exists && slack.Enabled {
        alerter.slack = mm.createSlackAlerter(slack.Config)
    }
    
    return alerter
}

func (mm *MonitoringManager) createPrometheusAlerter(config map[string]interface{}) *PrometheusAlerter {
    rules := make(map[string]AlertRule)
    
    if rulesConfig, exists := config["rules"]; exists {
        rulesMap := rulesConfig.(map[string]interface{})
        for name, ruleConfig := range rulesMap {
            rule := ruleConfig.(map[string]interface{})
            rules[name] = AlertRule{
                Condition: rule["condition"].(string),
                Severity:  rule["severity"].(string),
                Message:   rule["message"].(string),
            }
        }
    }
    
    return &PrometheusAlerter{
        rules: rules,
    }
}

func (mm *MonitoringManager) createSlackAlerter(config map[string]interface{}) *SlackAlerter {
    return &SlackAlerter{
        webhook: config["webhook"].(string),
        channel: config["channel"].(string),
    }
}

// Setup monitoring endpoints
func (mm *MonitoringManager) SetupEndpoints(mux *http.ServeMux) {
    // Prometheus metrics endpoint
    if mm.metrics != nil {
        mux.Handle("/metrics", promhttp.Handler())
    }
    
    // Health check endpoints
    for name, healthCheck := range mm.directive.HealthChecks {
        if healthCheck.Enabled {
            mux.HandleFunc(healthCheck.Path, mm.handleHealthCheck(name, healthCheck))
        }
    }
}

// Monitoring middleware
func (mm *MonitoringManager) withMonitoring(handler http.HandlerFunc) http.HandlerFunc {
    return func(w http.ResponseWriter, r *http.Request) {
        start := time.Now()
        
        // Track in-flight requests
        if mm.metrics != nil {
            mm.metrics.httpRequestsInFlight.WithLabelValues(r.Method, r.URL.Path).Inc()
            defer mm.metrics.httpRequestsInFlight.WithLabelValues(r.Method, r.URL.Path).Dec()
        }
        
        // Create response writer wrapper
        wrappedWriter := &responseWriter{ResponseWriter: w}
        
        // Execute handler
        handler(wrappedWriter, r)
        
        // Record metrics
        if mm.metrics != nil {
            duration := time.Since(start).Seconds()
            
            mm.metrics.httpRequestsTotal.WithLabelValues(
                r.Method,
                r.URL.Path,
                strconv.Itoa(wrappedWriter.status),
            ).Inc()
            
            mm.metrics.httpRequestDuration.WithLabelValues(
                r.Method,
                r.URL.Path,
            ).Observe(duration)
        }
        
        // Log request
        if mm.logger != nil {
            mm.logger.Info("HTTP request",
                zap.String("method", r.Method),
                zap.String("path", r.URL.Path),
                zap.Int("status", wrappedWriter.status),
                zap.Duration("duration", time.Since(start)),
                zap.String("user_agent", r.UserAgent()),
            )
        }
    }
}

func (mm *MonitoringManager) handleHealthCheck(name string, healthCheck HealthCheck) http.HandlerFunc {
    return func(w http.ResponseWriter, r *http.Request) {
        timeout, _ := time.ParseDuration(healthCheck.Timeout)
        ctx, cancel := context.WithTimeout(r.Context(), timeout)
        defer cancel()
        
        status := mm.healthChecks.RunChecks(ctx, healthCheck.Checks)
        
        w.Header().Set("Content-Type", "application/json")
        
        if status.Healthy {
            w.WriteHeader(http.StatusOK)
        } else {
            w.WriteHeader(http.StatusServiceUnavailable)
        }
        
        json.NewEncoder(w).Encode(status)
    }
}

// Health check implementations
func (mm *MonitoringManager) checkDatabase() error {
    // Implement database health check
    return nil
}

func (mm *MonitoringManager) checkRedis() error {
    // Implement Redis health check
    return nil
}

func (mm *MonitoringManager) checkExternalAPI() error {
    // Implement external API health check
    return nil
}

func (mm *MonitoringManager) checkCache() error {
    // Implement cache health check
    return nil
}

func (mm *MonitoringManager) checkQueue() error {
    // Implement queue health check
    return nil
}

func (mm *MonitoringManager) checkDatabaseMigration() error {
    // Implement database migration health check
    return nil
}

func (mm *MonitoringManager) checkCacheWarmup() error {
    // Implement cache warmup health check
    return nil
}

// HealthChecker methods
func (hc *HealthChecker) RunChecks(ctx context.Context, checkNames []string) HealthStatus {
    hc.mu.Lock()
    defer hc.mu.Unlock()
    
    var failedChecks []string
    
    for _, checkName := range checkNames {
        if check, exists := hc.checks[checkName]; exists {
            if err := check(); err != nil {
                failedChecks = append(failedChecks, checkName)
            }
        }
    }
    
    status := HealthStatus{
        Timestamp: time.Now(),
    }
    
    if len(failedChecks) == 0 {
        status.Status = "healthy"
    } else {
        status.Status = "unhealthy"
        status.Message = fmt.Sprintf("Failed checks: %v", failedChecks)
    }
    
    return status
}

// Alerter methods
func (a *Alerter) SendAlert(severity, message string) error {
    if a.slack != nil {
        return a.slack.SendAlert(severity, message)
    }
    return nil
}

func (sa *SlackAlerter) SendAlert(severity, message string) error {
    // Implement Slack alert sending
    return nil
}

// Helper types
type responseWriter struct {
    http.ResponseWriter
    status int
}

func (rw *responseWriter) WriteHeader(code int) {
    rw.status = code
    rw.ResponseWriter.WriteHeader(code)
}

// Handler functions
func handleUsers(w http.ResponseWriter, r *http.Request) {
    w.Header().Set("Content-Type", "application/json")
    w.Write([]byte(`{"users": []}`))
}

func handleProducts(w http.ResponseWriter, r *http.Request) {
    w.Header().Set("Content-Type", "application/json")
    w.Write([]byte(`{"products": []}`))
}

func (mm *MonitoringManager) Close() error {
    if mm.logger != nil {
        mm.logger.Sync()
    }
    return nil
}
```

## Advanced Monitoring Features

### Custom Metrics

```go
// TuskLang configuration with custom metrics
#monitoring {
    custom_metrics: {
        business_metrics: {
            enabled: true
            metrics: {
                active_users: {
                    type: "gauge"
                    description: "Number of active users"
                    labels: ["user_type", "region"]
                }
                
                revenue_per_hour: {
                    type: "counter"
                    description: "Revenue generated per hour"
                    labels: ["product", "channel"]
                }
                
                order_processing_time: {
                    type: "histogram"
                    description: "Order processing time"
                    buckets: [0.1, 0.5, 1.0, 2.0, 5.0]
                }
            }
        }
    }
}
```

### Distributed Tracing

```go
// TuskLang configuration with distributed tracing
#monitoring {
    distributed_tracing: {
        enabled: true
        sampling: {
            rate: 0.1
            rules: {
                high_priority: {
                    condition: "path =~ '/api/v1/.*'"
                    rate: 1.0
                }
                
                low_priority: {
                    condition: "path =~ '/static/.*'"
                    rate: 0.01
                }
            }
        }
        
        propagation: {
            headers: ["X-Trace-ID", "X-Span-ID"]
            format: "w3c"
        }
    }
}
```

## Performance Considerations

- **Metrics Collection**: Use efficient metrics collection to minimize overhead
- **Sampling**: Implement sampling for high-volume metrics and traces
- **Caching**: Cache health check results to reduce load
- **Async Processing**: Use goroutines for non-blocking monitoring operations
- **Storage Optimization**: Optimize storage for metrics and logs

## Security Notes

- **Access Control**: Implement proper access control for monitoring endpoints
- **Data Privacy**: Ensure sensitive data is not exposed in metrics or logs
- **Authentication**: Require authentication for monitoring endpoints
- **Encryption**: Encrypt monitoring data in transit and at rest
- **Audit Logging**: Log access to monitoring endpoints

## Best Practices

1. **Comprehensive Coverage**: Monitor all critical system components
2. **Meaningful Metrics**: Collect metrics that provide actionable insights
3. **Alert Thresholds**: Set appropriate alert thresholds to avoid noise
4. **Documentation**: Document monitoring policies and procedures
5. **Testing**: Test monitoring systems regularly
6. **Review**: Regularly review and update monitoring configurations

## Integration Examples

### With Gin Framework

```go
import (
    "github.com/gin-gonic/gin"
    "github.com/tusklang/go-sdk"
)

func setupGinMonitoring(config tusk.Config) *gin.Engine {
    var monitoringDirective MonitoringDirective
    config.Get("#monitoring", &monitoringDirective)
    
    monitoringManager := NewMonitoringManager(monitoringDirective)
    
    router := gin.Default()
    
    // Add monitoring middleware
    router.Use(monitoringMiddleware(monitoringManager))
    
    return router
}

func monitoringMiddleware(monitoringManager *MonitoringManager) gin.HandlerFunc {
    return func(c *gin.Context) {
        // Implement monitoring logic
        c.Set("monitoring", monitoringManager)
        c.Next()
    }
}
```

### With Echo Framework

```go
import (
    "github.com/labstack/echo/v4"
    "github.com/tusklang/go-sdk"
)

func setupEchoMonitoring(config tusk.Config) *echo.Echo {
    var monitoringDirective MonitoringDirective
    config.Get("#monitoring", &monitoringDirective)
    
    monitoringManager := NewMonitoringManager(monitoringDirective)
    
    e := echo.New()
    
    // Add monitoring middleware
    e.Use(monitoringMiddleware(monitoringManager))
    
    return e
}
```

This comprehensive monitoring hash directives documentation provides Go developers with everything they need to build sophisticated monitoring and observability systems using TuskLang's powerful directive system. 