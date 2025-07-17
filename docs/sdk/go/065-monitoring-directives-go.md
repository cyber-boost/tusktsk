# Monitoring Directives - Go

## 🎯 What Are Monitoring Directives?

Monitoring directives (`#monitoring`) in TuskLang allow you to define metrics collection, alerting, logging, and observability directly in your configuration files. They transform static config into executable monitoring logic.

```go
// Monitoring directives define your entire observability system
type MonitoringConfig struct {
    Metrics    map[string]string `tsk:"#monitoring_metrics"`
    Alerts     map[string]string `tsk:"#monitoring_alerts"`
    Logging    map[string]string `tsk:"#monitoring_logging"`
    Tracing    map[string]string `tsk:"#monitoring_tracing"`
}
```

## 🚀 Why Monitoring Directives Matter

### Traditional Monitoring Development
```go
// Old way - scattered across multiple files
func main() {
    // Monitoring configuration scattered
    prometheusRegistry := prometheus.NewRegistry()
    prometheusHandler := promhttp.HandlerFor(prometheusRegistry, promhttp.HandlerOpts{})
    
    // Metrics defined in code
    requestCounter := prometheus.NewCounterVec(
        prometheus.CounterOpts{
            Name: "http_requests_total",
            Help: "Total number of HTTP requests",
        },
        []string{"method", "endpoint", "status"},
    )
    prometheusRegistry.MustRegister(requestCounter)
    
    // Manual metric recording
    func handleRequest(w http.ResponseWriter, r *http.Request) {
        start := time.Now()
        // ... handle request
        duration := time.Since(start)
        
        requestCounter.WithLabelValues(r.Method, r.URL.Path, "200").Inc()
        requestDuration.WithLabelValues(r.Method, r.URL.Path).Observe(duration.Seconds())
    }
}
```

### TuskLang Monitoring Directives - Declarative & Dynamic
```tsk
# monitoring.tsk - Complete monitoring definition
monitoring_metrics: #monitoring("""
    http_requests -> HTTP request metrics
        type: "counter"
        labels: ["method", "endpoint", "status"]
        help: "Total number of HTTP requests"
        buckets: [0.1, 0.5, 1, 2, 5]
    
    request_duration -> Request duration metrics
        type: "histogram"
        labels: ["method", "endpoint"]
        help: "HTTP request duration in seconds"
        buckets: [0.1, 0.5, 1, 2, 5, 10]
    
    active_connections -> Active connection metrics
        type: "gauge"
        labels: ["service"]
        help: "Number of active connections"
    
    error_rate -> Error rate metrics
        type: "counter"
        labels: ["service", "error_type"]
        help: "Total number of errors"
        alert_threshold: 0.05
""")

monitoring_alerts: #monitoring("""
    high_error_rate -> High error rate alert
        condition: "error_rate > 0.05"
        duration: "5m"
        severity: "critical"
        notification: ["email", "slack"]
        message: "Error rate is above 5% for 5 minutes"
    
    slow_response_time -> Slow response time alert
        condition: "request_duration_p95 > 2"
        duration: "2m"
        severity: "warning"
        notification: ["slack"]
        message: "95th percentile response time is above 2 seconds"
    
    high_memory_usage -> High memory usage alert
        condition: "memory_usage > 0.8"
        duration: "1m"
        severity: "critical"
        notification: ["email", "slack", "pagerduty"]
        message: "Memory usage is above 80%"
""")

monitoring_logging: #monitoring("""
    structured_logging -> Structured logging configuration
        level: "info"
        format: "json"
        output: "stdout"
        fields: ["timestamp", "level", "service", "trace_id"]
    
    error_logging -> Error logging configuration
        level: "error"
        format: "json"
        output: "stderr"
        include_stack_trace: true
        include_context: true
""")
```

## 📋 Monitoring Directive Types

### 1. **Metrics Directives** (`#monitoring_metrics`)
- Metric definitions and types
- Label configuration
- Bucket specifications
- Alert thresholds

### 2. **Alert Directives** (`#monitoring_alerts`)
- Alert condition definitions
- Severity levels
- Notification channels
- Alert duration and thresholds

### 3. **Logging Directives** (`#monitoring_logging`)
- Log level configuration
- Log format specification
- Output destinations
- Structured logging fields

### 4. **Tracing Directives** (`#monitoring_tracing`)
- Distributed tracing configuration
- Trace sampling rates
- Span attributes
- Trace propagation

## 🔧 Basic Monitoring Directive Syntax

### Simple Metric Definition
```tsk
# Basic metric definition
request_counter: #monitoring("http_requests_total -> counter -> method,endpoint,status")
```

### Metric with Configuration
```tsk
# Metric with detailed configuration
request_duration: #monitoring("""
    name: "http_request_duration_seconds"
    type: "histogram"
    labels: ["method", "endpoint"]
    help: "HTTP request duration in seconds"
    buckets: [0.1, 0.5, 1, 2, 5, 10]
""")
```

### Multiple Metrics
```tsk
# Multiple metrics in one directive
application_metrics: #monitoring("""
    requests -> http_requests_total -> counter -> method,endpoint,status
    duration -> http_request_duration_seconds -> histogram -> method,endpoint
    connections -> active_connections -> gauge -> service
    errors -> error_count -> counter -> service,error_type
""")
```

## 🎯 Go Integration Patterns

### Struct Tags for Monitoring Directives
```go
type MonitoringConfig struct {
    // Metric definitions
    Metrics string `tsk:"#monitoring_metrics"`
    
    // Alert definitions
    Alerts string `tsk:"#monitoring_alerts"`
    
    // Logging configuration
    Logging string `tsk:"#monitoring_logging"`
    
    // Tracing configuration
    Tracing string `tsk:"#monitoring_tracing"`
}
```

### Monitoring Application Setup
```go
package main

import (
    "github.com/tusklang/go-sdk"
    "github.com/gorilla/mux"
    "net/http"
)

func main() {
    // Load monitoring configuration
    config := tusk.LoadConfig("monitoring.tsk")
    
    var monitoringConfig MonitoringConfig
    config.Unmarshal(&monitoringConfig)
    
    // Create monitoring system from directives
    monitoring := tusk.NewMonitoringSystem(monitoringConfig)
    
    // Create router
    router := mux.NewRouter()
    
    // Apply monitoring middleware
    tusk.ApplyMonitoringMiddleware(router, monitoring)
    
    // Start server
    http.ListenAndServe(":8080", router)
}
```

### Monitoring Handler Implementation
```go
package monitoring

import (
    "context"
    "net/http"
    "time"
    "github.com/prometheus/client_golang/prometheus"
    "github.com/prometheus/client_golang/prometheus/promhttp"
    "go.uber.org/zap"
    "go.opentelemetry.io/otel"
    "go.opentelemetry.io/otel/trace"
)

// Metrics collector
type MetricsCollector struct {
    requestCounter   *prometheus.CounterVec
    requestDuration  *prometheus.HistogramVec
    activeConnections *prometheus.GaugeVec
    errorCounter     *prometheus.CounterVec
}

func NewMetricsCollector() *MetricsCollector {
    return &MetricsCollector{
        requestCounter: prometheus.NewCounterVec(
            prometheus.CounterOpts{
                Name: "http_requests_total",
                Help: "Total number of HTTP requests",
            },
            []string{"method", "endpoint", "status"},
        ),
        requestDuration: prometheus.NewHistogramVec(
            prometheus.HistogramOpts{
                Name:    "http_request_duration_seconds",
                Help:    "HTTP request duration in seconds",
                Buckets: []float64{0.1, 0.5, 1, 2, 5, 10},
            },
            []string{"method", "endpoint"},
        ),
        activeConnections: prometheus.NewGaugeVec(
            prometheus.GaugeOpts{
                Name: "active_connections",
                Help: "Number of active connections",
            },
            []string{"service"},
        ),
        errorCounter: prometheus.NewCounterVec(
            prometheus.CounterOpts{
                Name: "error_count",
                Help: "Total number of errors",
            },
            []string{"service", "error_type"},
        ),
    }
}

func (mc *MetricsCollector) Register(registry prometheus.Registerer) {
    registry.MustRegister(
        mc.requestCounter,
        mc.requestDuration,
        mc.activeConnections,
        mc.errorCounter,
    )
}

func (mc *MetricsCollector) RecordRequest(method, endpoint, status string, duration time.Duration) {
    mc.requestCounter.WithLabelValues(method, endpoint, status).Inc()
    mc.requestDuration.WithLabelValues(method, endpoint).Observe(duration.Seconds())
}

func (mc *MetricsCollector) RecordError(service, errorType string) {
    mc.errorCounter.WithLabelValues(service, errorType).Inc()
}

func (mc *MetricsCollector) SetActiveConnections(service string, count int) {
    mc.activeConnections.WithLabelValues(service).Set(float64(count))
}

// Alert manager
type AlertManager struct {
    alerts map[string]Alert
    logger *zap.Logger
}

type Alert struct {
    Name       string            `json:"name"`
    Condition  string            `json:"condition"`
    Duration   time.Duration     `json:"duration"`
    Severity   string            `json:"severity"`
    Notifications []string       `json:"notifications"`
    Message    string            `json:"message"`
    Threshold  float64           `json:"threshold"`
}

func NewAlertManager(alerts map[string]Alert, logger *zap.Logger) *AlertManager {
    am := &AlertManager{
        alerts: alerts,
        logger: logger,
    }
    
    // Start alert evaluation goroutine
    go am.evaluateAlerts()
    
    return am
}

func (am *AlertManager) evaluateAlerts() {
    ticker := time.NewTicker(30 * time.Second)
    defer ticker.Stop()
    
    for range ticker.C {
        for name, alert := range am.alerts {
            if am.evaluateCondition(alert) {
                am.triggerAlert(name, alert)
            }
        }
    }
}

func (am *AlertManager) evaluateCondition(alert Alert) bool {
    // Implementation depends on your metrics storage
    // This is a simplified example
    switch alert.Condition {
    case "error_rate > 0.05":
        return getErrorRate() > 0.05
    case "request_duration_p95 > 2":
        return getRequestDurationP95() > 2
    case "memory_usage > 0.8":
        return getMemoryUsage() > 0.8
    default:
        return false
    }
}

func (am *AlertManager) triggerAlert(name string, alert Alert) {
    am.logger.Error("Alert triggered",
        zap.String("alert", name),
        zap.String("severity", alert.Severity),
        zap.String("message", alert.Message),
    )
    
    // Send notifications
    for _, notification := range alert.Notifications {
        am.sendNotification(notification, alert)
    }
}

func (am *AlertManager) sendNotification(channel string, alert Alert) {
    switch channel {
    case "email":
        sendEmailAlert(alert)
    case "slack":
        sendSlackAlert(alert)
    case "pagerduty":
        sendPagerDutyAlert(alert)
    }
}

// Structured logger
type StructuredLogger struct {
    logger *zap.Logger
}

func NewStructuredLogger(config LoggingConfig) (*StructuredLogger, error) {
    var zapConfig zap.Config
    
    switch config.Level {
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
    
    switch config.Format {
    case "json":
        zapConfig.Encoding = "json"
    case "console":
        zapConfig.Encoding = "console"
    default:
        zapConfig.Encoding = "json"
    }
    
    logger, err := zapConfig.Build()
    if err != nil {
        return nil, err
    }
    
    return &StructuredLogger{logger: logger}, nil
}

func (sl *StructuredLogger) Info(msg string, fields ...zap.Field) {
    sl.logger.Info(msg, fields...)
}

func (sl *StructuredLogger) Error(msg string, fields ...zap.Field) {
    sl.logger.Error(msg, fields...)
}

func (sl *StructuredLogger) WithContext(ctx context.Context) *zap.Logger {
    // Add trace context to logger
    if span := trace.SpanFromContext(ctx); span != nil {
        return sl.logger.With(
            zap.String("trace_id", span.SpanContext().TraceID().String()),
            zap.String("span_id", span.SpanContext().SpanID().String()),
        )
    }
    return sl.logger
}

// Distributed tracer
type DistributedTracer struct {
    tracer trace.Tracer
}

func NewDistributedTracer(serviceName string) *DistributedTracer {
    tracer := otel.Tracer(serviceName)
    return &DistributedTracer{tracer: tracer}
}

func (dt *DistributedTracer) StartSpan(ctx context.Context, name string, opts ...trace.SpanStartOption) (context.Context, trace.Span) {
    return dt.tracer.Start(ctx, name, opts...)
}

func (dt *DistributedTracer) AddEvent(ctx context.Context, name string, attrs ...trace.SpanStartOption) {
    if span := trace.SpanFromContext(ctx); span != nil {
        span.AddEvent(name, attrs...)
    }
}

func (dt *DistributedTracer) SetAttributes(ctx context.Context, attrs ...trace.SpanStartOption) {
    if span := trace.SpanFromContext(ctx); span != nil {
        span.SetAttributes(attrs...)
    }
}
```

## 🔄 Advanced Monitoring Patterns

### Custom Metrics
```tsk
# Custom metrics configuration
custom_metrics: #monitoring("""
    business_metrics -> Business-specific metrics
        user_registrations -> counter -> source,plan
        revenue -> counter -> product,region
        conversion_rate -> gauge -> funnel_step
        customer_satisfaction -> histogram -> service,rating
    
    application_metrics -> Application-specific metrics
        cache_hit_rate -> gauge -> cache_name
        database_connections -> gauge -> database
        queue_length -> gauge -> queue_name
        job_duration -> histogram -> job_type
""")
```

### Dynamic Alerts
```tsk
# Dynamic alert configuration
dynamic_alerts: #monitoring("""
    adaptive_alerts -> Adaptive alert thresholds
        error_rate -> #if(load > 80, 0.1, 0.05)
        response_time -> #if(load > 80, 5, 2)
        memory_usage -> #if(load > 80, 0.9, 0.8)
    
    time_based_alerts -> Time-based alert thresholds
        peak_hours -> #if(hour >= 9 && hour <= 17, 0.05, 0.1)
        weekend_alerts -> #if(day == "saturday" || day == "sunday", false, true)
""")
```

### Multi-Environment Monitoring
```tsk
# Multi-environment monitoring
environment_monitoring: #monitoring("""
    production -> Production monitoring
        metrics_retention: 30d
        alert_channels: ["email", "slack", "pagerduty"]
        log_level: "info"
        trace_sampling: 0.1
    
    staging -> Staging monitoring
        metrics_retention: 7d
        alert_channels: ["slack"]
        log_level: "debug"
        trace_sampling: 1.0
    
    development -> Development monitoring
        metrics_retention: 1d
        alert_channels: ["slack"]
        log_level: "debug"
        trace_sampling: 1.0
""")
```

## 🛡️ Security Features

### Monitoring Security Configuration
```tsk
# Monitoring security configuration
monitoring_security: #monitoring("""
    metrics_security -> Metrics security
        authentication: true
        authorization: true
        roles: ["admin", "monitoring"]
        endpoints: ["/metrics", "/health"]
    
    log_security -> Log security
        encryption: true
        retention: 30d
        access_control: true
        audit_logging: true
    
    trace_security -> Trace security
        sensitive_data_filtering: true
        pii_redaction: true
        access_control: true
        encryption: true
""")
```

### Go Security Implementation
```go
package security

import (
    "crypto/tls"
    "net/http"
    "strings"
)

// Secure metrics handler
type SecureMetricsHandler struct {
    handler http.Handler
    auth    AuthProvider
}

func NewSecureMetricsHandler(handler http.Handler, auth AuthProvider) *SecureMetricsHandler {
    return &SecureMetricsHandler{
        handler: handler,
        auth:    auth,
    }
}

func (smh *SecureMetricsHandler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
    // Check authentication
    if !smh.auth.IsAuthenticated(r) {
        http.Error(w, "Unauthorized", http.StatusUnauthorized)
        return
    }
    
    // Check authorization
    if !smh.auth.HasRole(r, "admin") && !smh.auth.HasRole(r, "monitoring") {
        http.Error(w, "Forbidden", http.StatusForbidden)
        return
    }
    
    // Serve metrics
    smh.handler.ServeHTTP(w, r)
}

// Secure log handler
type SecureLogHandler struct {
    logger *zap.Logger
    encryptor Encryptor
}

func NewSecureLogHandler(logger *zap.Logger, encryptor Encryptor) *SecureLogHandler {
    return &SecureLogHandler{
        logger:    logger,
        encryptor: encryptor,
    }
}

func (slh *SecureLogHandler) LogSecure(level zapcore.Level, msg string, fields ...zap.Field) {
    // Encrypt sensitive fields
    encryptedFields := make([]zap.Field, len(fields))
    for i, field := range fields {
        if isSensitiveField(field.Key) {
            encryptedValue, err := slh.encryptor.Encrypt(field.String)
            if err != nil {
                encryptedValue = "[ENCRYPTION_ERROR]"
            }
            encryptedFields[i] = zap.String(field.Key, encryptedValue)
        } else {
            encryptedFields[i] = field
        }
    }
    
    // Log with encrypted fields
    switch level {
    case zapcore.DebugLevel:
        slh.logger.Debug(msg, encryptedFields...)
    case zapcore.InfoLevel:
        slh.logger.Info(msg, encryptedFields...)
    case zapcore.WarnLevel:
        slh.logger.Warn(msg, encryptedFields...)
    case zapcore.ErrorLevel:
        slh.logger.Error(msg, encryptedFields...)
    }
}

// Sensitive field detection
func isSensitiveField(key string) bool {
    sensitiveKeys := []string{
        "password", "token", "secret", "key", "auth",
        "credit_card", "ssn", "email", "phone",
    }
    
    keyLower := strings.ToLower(key)
    for _, sensitive := range sensitiveKeys {
        if strings.Contains(keyLower, sensitive) {
            return true
        }
    }
    
    return false
}
```

## ⚡ Performance Optimization

### Monitoring Performance Configuration
```tsk
# Monitoring performance configuration
monitoring_performance: #monitoring("""
    metrics_performance -> Metrics performance
        batch_size: 100
        flush_interval: 10s
        buffer_size: 1000
        compression: true
    
    logging_performance -> Logging performance
        async_logging: true
        buffer_size: 1000
        flush_interval: 5s
        compression: true
    
    tracing_performance -> Tracing performance
        sampling_rate: 0.1
        batch_size: 50
        flush_interval: 5s
        max_queue_size: 1000
""")
```

### Go Performance Implementation
```go
package performance

import (
    "context"
    "sync"
    "time"
)

// Batched metrics collector
type BatchedMetricsCollector struct {
    metrics []Metric
    mu      sync.Mutex
    batchSize int
    flushInterval time.Duration
    stopChan chan bool
}

type Metric struct {
    Name   string
    Value  float64
    Labels map[string]string
    Type   string
    Timestamp time.Time
}

func NewBatchedMetricsCollector(batchSize int, flushInterval time.Duration) *BatchedMetricsCollector {
    bmc := &BatchedMetricsCollector{
        metrics:       make([]Metric, 0, batchSize),
        batchSize:     batchSize,
        flushInterval: flushInterval,
        stopChan:      make(chan bool),
    }
    
    // Start flush goroutine
    go bmc.flushLoop()
    
    return bmc
}

func (bmc *BatchedMetricsCollector) RecordMetric(name string, value float64, labels map[string]string, metricType string) {
    bmc.mu.Lock()
    defer bmc.mu.Unlock()
    
    metric := Metric{
        Name:      name,
        Value:     value,
        Labels:    labels,
        Type:      metricType,
        Timestamp: time.Now(),
    }
    
    bmc.metrics = append(bmc.metrics, metric)
    
    // Flush if batch is full
    if len(bmc.metrics) >= bmc.batchSize {
        bmc.flush()
    }
}

func (bmc *BatchedMetricsCollector) flush() {
    if len(bmc.metrics) == 0 {
        return
    }
    
    // Send metrics to storage
    go func(metrics []Metric) {
        if err := sendMetrics(metrics); err != nil {
            log.Printf("Failed to send metrics: %v", err)
        }
    }(append([]Metric{}, bmc.metrics...))
    
    // Clear batch
    bmc.metrics = bmc.metrics[:0]
}

func (bmc *BatchedMetricsCollector) flushLoop() {
    ticker := time.NewTicker(bmc.flushInterval)
    defer ticker.Stop()
    
    for {
        select {
        case <-ticker.C:
            bmc.mu.Lock()
            bmc.flush()
            bmc.mu.Unlock()
        case <-bmc.stopChan:
            return
        }
    }
}

func (bmc *BatchedMetricsCollector) Stop() {
    close(bmc.stopChan)
    
    // Final flush
    bmc.mu.Lock()
    bmc.flush()
    bmc.mu.Unlock()
}

// Async logger
type AsyncLogger struct {
    logger *zap.Logger
    buffer chan LogEntry
    stopChan chan bool
}

type LogEntry struct {
    Level   zapcore.Level
    Message string
    Fields  []zap.Field
}

func NewAsyncLogger(logger *zap.Logger, bufferSize int) *AsyncLogger {
    al := &AsyncLogger{
        logger:   logger,
        buffer:   make(chan LogEntry, bufferSize),
        stopChan: make(chan bool),
    }
    
    // Start processing goroutine
    go al.processLogs()
    
    return al
}

func (al *AsyncLogger) Log(level zapcore.Level, msg string, fields ...zap.Field) {
    entry := LogEntry{
        Level:   level,
        Message: msg,
        Fields:  fields,
    }
    
    select {
    case al.buffer <- entry:
        // Log entry queued successfully
    default:
        // Buffer full, log synchronously
        al.logger.Log(level, msg, fields...)
    }
}

func (al *AsyncLogger) processLogs() {
    for {
        select {
        case entry := <-al.buffer:
            al.logger.Log(entry.Level, entry.Message, entry.Fields...)
        case <-al.stopChan:
            return
        }
    }
}

func (al *AsyncLogger) Stop() {
    close(al.stopChan)
    
    // Process remaining logs
    for {
        select {
        case entry := <-al.buffer:
            al.logger.Log(entry.Level, entry.Message, entry.Fields...)
        default:
            return
        }
    }
}
```

## 🔧 Error Handling

### Monitoring Error Configuration
```tsk
# Monitoring error handling configuration
monitoring_errors: #monitoring("""
    metrics_error -> Metrics collection error
        retry_attempts: 3
        retry_delay: 1s
        fallback: "memory"
        log_level: error
    
    alert_error -> Alert delivery error
        retry_attempts: 5
        retry_delay: 30s
        fallback: "email"
        log_level: error
    
    log_error -> Logging error
        fallback: "stderr"
        log_level: error
        alert: true
    
    trace_error -> Tracing error
        fallback: "local"
        log_level: warn
        alert: false
""")
```

### Go Error Handler Implementation
```go
package errors

import (
    "context"
    "log"
    "time"
)

// Monitoring error types
type MonitoringError struct {
    Type    string `json:"type"`
    Message string `json:"message"`
    Component string `json:"component"`
    Retries  int    `json:"retries"`
}

// Monitoring error handlers
func HandleMonitoringError(err error, component string, retries int) {
    monitoringError := MonitoringError{
        Type:      "monitoring_error",
        Message:   err.Error(),
        Component: component,
        Retries:   retries,
    }
    
    log.Printf("Monitoring error in %s: %v", component, err)
    
    // Record error metrics
    recordMonitoringError(monitoringError)
    
    // Alert on critical errors
    if isCriticalMonitoringError(err) {
        sendMonitoringAlert(monitoringError)
    }
}

// Fallback monitoring system
type FallbackMonitoring struct {
    primary   MonitoringSystem
    fallback  MonitoringSystem
}

func NewFallbackMonitoring(primary, fallback MonitoringSystem) *FallbackMonitoring {
    return &FallbackMonitoring{
        primary:  primary,
        fallback: fallback,
    }
}

func (fm *FallbackMonitoring) RecordMetric(name string, value float64, labels map[string]string) error {
    // Try primary system
    if err := fm.primary.RecordMetric(name, value, labels); err == nil {
        return nil
    }
    
    // Use fallback system
    log.Printf("Primary monitoring failed, using fallback: %v", err)
    return fm.fallback.RecordMetric(name, value, labels)
}

func (fm *FallbackMonitoring) SendAlert(alert Alert) error {
    // Try primary system
    if err := fm.primary.SendAlert(alert); err == nil {
        return nil
    }
    
    // Use fallback system
    log.Printf("Primary alert system failed, using fallback: %v", err)
    return fm.fallback.SendAlert(alert)
}

func (fm *FallbackMonitoring) Log(level string, message string, fields map[string]interface{}) error {
    // Try primary system
    if err := fm.primary.Log(level, message, fields); err == nil {
        return nil
    }
    
    // Use fallback system
    log.Printf("Primary logging failed, using fallback: %v", err)
    return fm.fallback.Log(level, message, fields)
}
```

## 🎯 Real-World Example

### Complete Monitoring Configuration
```tsk
# monitoring-config.tsk - Complete monitoring configuration

# Environment configuration
environment: #env("ENVIRONMENT", "development")
debug_mode: #env("DEBUG", "false")

# Metrics configuration
metrics: #monitoring("""
    # HTTP metrics
    http_requests -> HTTP request metrics
        type: "counter"
        labels: ["method", "endpoint", "status"]
        help: "Total number of HTTP requests"
        buckets: [0.1, 0.5, 1, 2, 5]
    
    request_duration -> Request duration metrics
        type: "histogram"
        labels: ["method", "endpoint"]
        help: "HTTP request duration in seconds"
        buckets: [0.1, 0.5, 1, 2, 5, 10]
    
    # Application metrics
    active_connections -> Active connection metrics
        type: "gauge"
        labels: ["service"]
        help: "Number of active connections"
    
    error_rate -> Error rate metrics
        type: "counter"
        labels: ["service", "error_type"]
        help: "Total number of errors"
        alert_threshold: 0.05
    
    # Business metrics
    user_registrations -> User registration metrics
        type: "counter"
        labels: ["source", "plan"]
        help: "Total number of user registrations"
    
    revenue -> Revenue metrics
        type: "counter"
        labels: ["product", "region"]
        help: "Total revenue"
    
    # System metrics
    memory_usage -> Memory usage metrics
        type: "gauge"
        labels: ["service"]
        help: "Memory usage percentage"
        alert_threshold: 0.8
    
    cpu_usage -> CPU usage metrics
        type: "gauge"
        labels: ["service"]
        help: "CPU usage percentage"
        alert_threshold: 0.9
    
    # Database metrics
    database_connections -> Database connection metrics
        type: "gauge"
        labels: ["database"]
        help: "Number of database connections"
    
    query_duration -> Query duration metrics
        type: "histogram"
        labels: ["database", "query_type"]
        help: "Database query duration in seconds"
        buckets: [0.01, 0.1, 0.5, 1, 2, 5]
""")

# Alerts configuration
alerts: #monitoring("""
    # Error rate alerts
    high_error_rate -> High error rate alert
        condition: "error_rate > 0.05"
        duration: "5m"
        severity: "critical"
        notification: ["email", "slack", "pagerduty"]
        message: "Error rate is above 5% for 5 minutes"
        threshold: 0.05
    
    # Performance alerts
    slow_response_time -> Slow response time alert
        condition: "request_duration_p95 > 2"
        duration: "2m"
        severity: "warning"
        notification: ["slack"]
        message: "95th percentile response time is above 2 seconds"
        threshold: 2
    
    # System alerts
    high_memory_usage -> High memory usage alert
        condition: "memory_usage > 0.8"
        duration: "1m"
        severity: "critical"
        notification: ["email", "slack", "pagerduty"]
        message: "Memory usage is above 80%"
        threshold: 0.8
    
    high_cpu_usage -> High CPU usage alert
        condition: "cpu_usage > 0.9"
        duration: "1m"
        severity: "critical"
        notification: ["email", "slack", "pagerduty"]
        message: "CPU usage is above 90%"
        threshold: 0.9
    
    # Database alerts
    database_connection_limit -> Database connection limit alert
        condition: "database_connections > 80"
        duration: "1m"
        severity: "warning"
        notification: ["slack"]
        message: "Database connections are above 80% of limit"
        threshold: 80
    
    slow_queries -> Slow queries alert
        condition: "query_duration_p95 > 1"
        duration: "5m"
        severity: "warning"
        notification: ["slack"]
        message: "95th percentile query duration is above 1 second"
        threshold: 1
""")

# Logging configuration
logging: #monitoring("""
    # Structured logging
    structured_logging -> Structured logging configuration
        level: "info"
        format: "json"
        output: "stdout"
        fields: ["timestamp", "level", "service", "trace_id", "user_id"]
        retention: 30d
    
    # Error logging
    error_logging -> Error logging configuration
        level: "error"
        format: "json"
        output: "stderr"
        include_stack_trace: true
        include_context: true
        retention: 90d
    
    # Access logging
    access_logging -> Access logging configuration
        level: "info"
        format: "json"
        output: "stdout"
        fields: ["timestamp", "method", "path", "status", "duration", "ip"]
        retention: 30d
    
    # Security logging
    security_logging -> Security logging configuration
        level: "warn"
        format: "json"
        output: "stderr"
        fields: ["timestamp", "event", "user_id", "ip", "action"]
        retention: 365d
        encryption: true
""")

# Tracing configuration
tracing: #monitoring("""
    # Distributed tracing
    distributed_tracing -> Distributed tracing configuration
        service_name: "myapp"
        sampling_rate: 0.1
        span_attributes: ["http.method", "http.url", "http.status_code"]
        trace_headers: ["X-Trace-ID", "X-Span-ID"]
    
    # Trace storage
    trace_storage -> Trace storage configuration
        backend: "jaeger"
        url: #env("JAEGER_URL")
        batch_size: 50
        flush_interval: 5s
        max_queue_size: 1000
    
    # Trace security
    trace_security -> Trace security configuration
        sensitive_data_filtering: true
        pii_redaction: true
        access_control: true
        encryption: true
""")

# Performance configuration
performance: #monitoring("""
    # Metrics performance
    metrics_performance -> Metrics performance configuration
        batch_size: 100
        flush_interval: 10s
        buffer_size: 1000
        compression: true
        async_collection: true
    
    # Logging performance
    logging_performance -> Logging performance configuration
        async_logging: true
        buffer_size: 1000
        flush_interval: 5s
        compression: true
        batch_size: 100
    
    # Tracing performance
    tracing_performance -> Tracing performance configuration
        sampling_rate: 0.1
        batch_size: 50
        flush_interval: 5s
        max_queue_size: 1000
        async_processing: true
""")

# Security configuration
security: #monitoring("""
    # Metrics security
    metrics_security -> Metrics security configuration
        authentication: true
        authorization: true
        roles: ["admin", "monitoring"]
        endpoints: ["/metrics", "/health", "/ready"]
        tls_enabled: true
    
    # Log security
    log_security -> Log security configuration
        encryption: true
        retention: 30d
        access_control: true
        audit_logging: true
        sensitive_data_filtering: true
    
    # Trace security
    trace_security -> Trace security configuration
        sensitive_data_filtering: true
        pii_redaction: true
        access_control: true
        encryption: true
        retention: 7d
""")

# Error handling
error_handling: #monitoring("""
    # Metrics error handling
    metrics_error -> Metrics collection error handling
        retry_attempts: 3
        retry_delay: 1s
        exponential_backoff: true
        fallback: "memory"
        log_level: error
        alert: true
    
    # Alert error handling
    alert_error -> Alert delivery error handling
        retry_attempts: 5
        retry_delay: 30s
        exponential_backoff: true
        fallback: "email"
        log_level: error
        alert: true
    
    # Log error handling
    log_error -> Logging error handling
        fallback: "stderr"
        log_level: error
        alert: true
        bypass_on_error: true
    
    # Trace error handling
    trace_error -> Tracing error handling
        fallback: "local"
        log_level: warn
        alert: false
        bypass_on_error: true
""")
```

### Go Monitoring Application Implementation
```go
package main

import (
    "fmt"
    "log"
    "net/http"
    "github.com/tusklang/go-sdk"
    "github.com/gorilla/mux"
)

type MonitoringConfig struct {
    Environment    string `tsk:"environment"`
    DebugMode      bool   `tsk:"debug_mode"`
    Metrics        string `tsk:"metrics"`
    Alerts         string `tsk:"alerts"`
    Logging        string `tsk:"logging"`
    Tracing        string `tsk:"tracing"`
    Performance    string `tsk:"performance"`
    Security       string `tsk:"security"`
    ErrorHandling  string `tsk:"error_handling"`
}

func main() {
    // Load monitoring configuration
    config := tusk.LoadConfig("monitoring-config.tsk")
    
    var monitoringConfig MonitoringConfig
    if err := config.Unmarshal(&monitoringConfig); err != nil {
        log.Fatal("Failed to load monitoring config:", err)
    }
    
    // Create monitoring system from directives
    monitoring := tusk.NewMonitoringSystem(monitoringConfig)
    
    // Create router
    router := mux.NewRouter()
    
    // Apply monitoring middleware
    tusk.ApplyMonitoringMiddleware(router, monitoring)
    
    // Setup routes
    setupRoutes(router, monitoring)
    
    // Start server
    addr := fmt.Sprintf(":%s", #env("PORT", "8080"))
    log.Printf("Starting monitoring server on %s in %s mode", addr, monitoringConfig.Environment)
    
    if err := http.ListenAndServe(addr, router); err != nil {
        log.Fatal("Monitoring server failed:", err)
    }
}

func setupRoutes(router *mux.Router, monitoring *tusk.MonitoringSystem) {
    // Health check endpoints
    router.HandleFunc("/health", healthHandler).Methods("GET")
    router.HandleFunc("/ready", readyHandler).Methods("GET")
    router.HandleFunc("/metrics", monitoring.MetricsHandler).Methods("GET")
    
    // Monitoring management endpoints
    monitoringRouter := router.PathPrefix("/monitoring").Subrouter()
    monitoringRouter.Use(authMiddleware)
    
    monitoringRouter.HandleFunc("/alerts", monitoring.AlertsHandler).Methods("GET", "POST")
    monitoringRouter.HandleFunc("/logs", monitoring.LogsHandler).Methods("GET")
    monitoringRouter.HandleFunc("/traces", monitoring.TracesHandler).Methods("GET")
    
    // API routes with monitoring
    api := router.PathPrefix("/api").Subrouter()
    
    // Apply monitoring to all API routes
    api.Use(monitoring.Middleware())
    
    // User routes
    api.HandleFunc("/users", usersHandler).Methods("GET", "POST")
    api.HandleFunc("/users/{id}", userHandler).Methods("GET", "PUT", "DELETE")
    
    // Business routes
    api.HandleFunc("/revenue", revenueHandler).Methods("GET")
    api.HandleFunc("/registrations", registrationsHandler).Methods("GET")
}
```

## 🎯 Best Practices

### 1. **Use Appropriate Metric Types**
```tsk
# Choose the right metric type for each use case
metric_types: #monitoring("""
    # Counters for cumulative values
    requests -> http_requests_total -> counter
    
    # Gauges for current values
    connections -> active_connections -> gauge
    
    # Histograms for distributions
    duration -> request_duration_seconds -> histogram
""")
```

### 2. **Implement Proper Error Handling**
```go
// Comprehensive monitoring error handling
func handleMonitoringError(err error, component string) {
    log.Printf("Monitoring error in %s: %v", component, err)
    
    // Record error metrics
    recordMonitoringError(component, err)
    
    // Use fallback if available
    if fallbackMonitoring != nil {
        log.Printf("Using fallback monitoring for %s", component)
        // Implement fallback logic
    }
    
    // Alert on critical errors
    if isCriticalMonitoringError(err) {
        sendMonitoringAlert(component, err)
    }
}
```

### 3. **Use Environment-Specific Configuration**
```tsk
# Different monitoring settings for different environments
environment_monitoring: #if(
    #env("ENVIRONMENT") == "production",
    #monitoring("""
        sampling_rate: 0.1
        alert_channels: ["email", "slack", "pagerduty"]
        log_level: "info"
        retention: 30d
    """),
    #monitoring("""
        sampling_rate: 1.0
        alert_channels: ["slack"]
        log_level: "debug"
        retention: 7d
    """)
)
```

### 4. **Monitor Monitoring Performance**
```go
// Monitoring performance monitoring
func monitorMonitoringPerformance(component string, startTime time.Time) {
    duration := time.Since(startTime)
    
    // Record metrics
    metrics := map[string]interface{}{
        "component": component,
        "duration":  duration.Milliseconds(),
        "timestamp": time.Now(),
    }
    
    if err := recordMonitoringMetrics(metrics); err != nil {
        log.Printf("Failed to record monitoring metrics: %v", err)
    }
    
    // Alert on slow monitoring operations
    if duration > 100*time.Millisecond {
        log.Printf("Slow monitoring operation in %s: %v", component, duration)
    }
}
```

## 🎯 Summary

Monitoring directives in TuskLang provide a powerful, declarative way to define observability systems. They enable:

- **Declarative monitoring configuration** that is easy to understand and maintain
- **Flexible metric collection** with various metric types and labels
- **Comprehensive alerting** with multiple notification channels
- **Built-in security features** including authentication and encryption
- **Performance optimization** with batching, caching, and async processing

The Go SDK seamlessly integrates monitoring directives with existing Go monitoring libraries, making them feel like native Go features while providing the power and flexibility of TuskLang's directive system.

**Next**: Explore deployment directives, scaling directives, and other specialized directive types in the following guides. 