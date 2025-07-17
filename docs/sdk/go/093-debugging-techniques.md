# Debugging Techniques

TuskLang provides powerful debugging capabilities that go beyond traditional debugging tools. This guide covers advanced debugging techniques for Go applications.

## Debugging Philosophy

### Observability-First Approach
```go
// Comprehensive debugging with observability
type Debugger struct {
    logger    *log.Logger
    tracer    *tracing.Tracer
    metrics   *metrics.Collector
    profiler  *profiling.Profiler
    config    *tusk.Config
}

func NewDebugger(config *tusk.Config) *Debugger {
    return &Debugger{
        logger:   setupLogger(config),
        tracer:   setupTracer(config),
        metrics:  setupMetrics(config),
        profiler: setupProfiler(config),
        config:   config,
    }
}

func (d *Debugger) DebugOperation(ctx context.Context, operation string, fn func() error) error {
    // Start tracing
    span := d.tracer.StartSpan(operation)
    defer span.End()
    
    // Start profiling
    d.profiler.Start(operation)
    defer d.profiler.Stop(operation)
    
    // Record metrics
    start := time.Now()
    defer func() {
        d.metrics.RecordDuration(operation, time.Since(start))
    }()
    
    // Execute operation
    err := fn()
    
    // Log result
    if err != nil {
        d.logger.Printf("Operation %s failed: %v", operation, err)
        span.SetError(err)
    } else {
        d.logger.Printf("Operation %s completed successfully", operation)
    }
    
    return err
}
```

### Structured Debugging
```go
// Structured debugging with context
type DebugContext struct {
    RequestID   string
    UserID      string
    Operation   string
    Timestamp   time.Time
    Metadata    map[string]interface{}
}

func (dc *DebugContext) WithMetadata(key string, value interface{}) *DebugContext {
    newCtx := *dc
    newCtx.Metadata[key] = value
    return &newCtx
}

func (dc *DebugContext) Log(level string, message string, args ...interface{}) {
    logData := map[string]interface{}{
        "request_id": dc.RequestID,
        "user_id":    dc.UserID,
        "operation":  dc.Operation,
        "timestamp":  dc.Timestamp,
        "level":      level,
        "message":    fmt.Sprintf(message, args...),
        "metadata":   dc.Metadata,
    }
    
    jsonData, _ := json.Marshal(logData)
    log.Printf("DEBUG: %s", string(jsonData))
}
```

## TuskLang Debug Configuration

### Debug Environment Setup
```tsk
# Debug configuration
debug_environment {
    # Logging configuration
    logging {
        level = "debug"
        format = "json"
        output = "stdout"
        file_path = "debug.log"
        max_size = "100MB"
        max_age = "7d"
    }
    
    # Tracing configuration
    tracing {
        enabled = true
        sampler_rate = 1.0
        exporter = "jaeger"
        endpoint = "http://localhost:14268/api/traces"
    }
    
    # Metrics configuration
    metrics {
        enabled = true
        exporter = "prometheus"
        port = 9090
        path = "/metrics"
    }
    
    # Profiling configuration
    profiling {
        enabled = true
        cpu_profile = true
        memory_profile = true
        goroutine_profile = true
        output_dir = "profiles"
    }
    
    # Debug settings
    settings {
        verbose = true
        show_sql = true
        show_http_requests = true
        show_cache_operations = true
        slow_query_threshold = "100ms"
    }
}
```

### Debug Data Collection
```tsk
# Debug data collection
debug_data {
    # Request tracking
    request_tracking {
        enabled = true
        track_headers = true
        track_body = true
        max_body_size = "1MB"
        sensitive_fields = ["password", "token", "secret"]
    }
    
    # Performance monitoring
    performance_monitoring {
        enabled = true
        track_database_queries = true
        track_http_requests = true
        track_cache_operations = true
        track_goroutines = true
    }
    
    # Error tracking
    error_tracking {
        enabled = true
        capture_stack_traces = true
        capture_context = true
        max_errors_per_minute = 100
    }
}
```

## Go Debugging Implementation

### Advanced Logging
```go
// Advanced logging with structured data
type StructuredLogger struct {
    logger *log.Logger
    config *tusk.Config
}

func (sl *StructuredLogger) LogWithContext(ctx context.Context, level string, message string, fields map[string]interface{}) {
    logEntry := map[string]interface{}{
        "timestamp": time.Now().UTC(),
        "level":     level,
        "message":   message,
        "fields":    fields,
    }
    
    // Add context information
    if requestID := ctx.Value("request_id"); requestID != nil {
        logEntry["request_id"] = requestID
    }
    
    if userID := ctx.Value("user_id"); userID != nil {
        logEntry["user_id"] = userID
    }
    
    // Add trace information
    if span := trace.SpanFromContext(ctx); span != nil {
        logEntry["trace_id"] = span.SpanContext().TraceID().String()
        logEntry["span_id"] = span.SpanContext().SpanID().String()
    }
    
    jsonData, _ := json.Marshal(logEntry)
    sl.logger.Printf("%s", string(jsonData))
}

func (sl *StructuredLogger) Debug(ctx context.Context, message string, fields map[string]interface{}) {
    sl.LogWithContext(ctx, "DEBUG", message, fields)
}

func (sl *StructuredLogger) Info(ctx context.Context, message string, fields map[string]interface{}) {
    sl.LogWithContext(ctx, "INFO", message, fields)
}

func (sl *StructuredLogger) Error(ctx context.Context, message string, err error, fields map[string]interface{}) {
    if fields == nil {
        fields = make(map[string]interface{})
    }
    fields["error"] = err.Error()
    fields["stack_trace"] = getStackTrace()
    sl.LogWithContext(ctx, "ERROR", message, fields)
}
```

### Request Tracing
```go
// Request tracing with detailed context
type RequestTracer struct {
    tracer *tracing.Tracer
    config *tusk.Config
}

func (rt *RequestTracer) TraceRequest(ctx context.Context, req *http.Request) (context.Context, func()) {
    // Extract trace information from headers
    traceID := req.Header.Get("X-Trace-ID")
    if traceID == "" {
        traceID = generateTraceID()
    }
    
    // Create span for request
    span := rt.tracer.StartSpan("http_request")
    span.SetTag("http.method", req.Method)
    span.SetTag("http.url", req.URL.String())
    span.SetTag("http.user_agent", req.UserAgent())
    span.SetTag("trace.id", traceID)
    
    // Add span to context
    ctx := trace.ContextWithSpan(ctx, span)
    
    // Add trace ID to context
    ctx = context.WithValue(ctx, "trace_id", traceID)
    
    return ctx, func() {
        span.Finish()
    }
}

func (rt *RequestTracer) TraceDatabaseQuery(ctx context.Context, query string, args []interface{}) (context.Context, func()) {
    span := rt.tracer.StartSpan("database_query")
    span.SetTag("db.query", query)
    span.SetTag("db.args", args)
    
    ctx = trace.ContextWithSpan(ctx, span)
    
    return ctx, func() {
        span.Finish()
    }
}
```

### Performance Profiling
```go
// Performance profiling with detailed metrics
type PerformanceProfiler struct {
    config *tusk.Config
    profiles map[string]*Profile
}

type Profile struct {
    Name      string
    StartTime time.Time
    EndTime   time.Time
    Duration  time.Duration
    Metrics   map[string]interface{}
}

func (pp *PerformanceProfiler) StartProfile(name string) *Profile {
    profile := &Profile{
        Name:      name,
        StartTime: time.Now(),
        Metrics:   make(map[string]interface{}),
    }
    
    pp.profiles[name] = profile
    return profile
}

func (pp *PerformanceProfiler) EndProfile(name string) error {
    profile, exists := pp.profiles[name]
    if !exists {
        return fmt.Errorf("profile %s not found", name)
    }
    
    profile.EndTime = time.Now()
    profile.Duration = profile.EndTime.Sub(profile.StartTime)
    
    // Record metrics
    pp.recordMetrics(profile)
    
    return nil
}

func (pp *PerformanceProfiler) recordMetrics(profile *Profile) {
    // Record duration
    profile.Metrics["duration_ms"] = profile.Duration.Milliseconds()
    
    // Record memory usage
    var m runtime.MemStats
    runtime.ReadMemStats(&m)
    profile.Metrics["memory_alloc"] = m.Alloc
    profile.Metrics["memory_total_alloc"] = m.TotalAlloc
    profile.Metrics["memory_sys"] = m.Sys
    
    // Record goroutine count
    profile.Metrics["goroutines"] = runtime.NumGoroutine()
}
```

## Advanced Debugging Features

### Memory Debugging
```go
// Memory debugging and leak detection
type MemoryDebugger struct {
    config *tusk.Config
    snapshots []MemorySnapshot
}

type MemorySnapshot struct {
    Timestamp time.Time
    Alloc     uint64
    TotalAlloc uint64
    Sys       uint64
    NumGC     uint32
    Goroutines int
}

func (md *MemoryDebugger) TakeSnapshot() MemorySnapshot {
    var m runtime.MemStats
    runtime.ReadMemStats(&m)
    
    snapshot := MemorySnapshot{
        Timestamp:   time.Now(),
        Alloc:       m.Alloc,
        TotalAlloc:  m.TotalAlloc,
        Sys:         m.Sys,
        NumGC:       m.NumGC,
        Goroutines:  runtime.NumGoroutine(),
    }
    
    md.snapshots = append(md.snapshots, snapshot)
    return snapshot
}

func (md *MemoryDebugger) DetectLeaks() []LeakReport {
    if len(md.snapshots) < 2 {
        return nil
    }
    
    var leaks []LeakReport
    
    for i := 1; i < len(md.snapshots); i++ {
        prev := md.snapshots[i-1]
        curr := md.snapshots[i]
        
        // Check for memory growth
        if curr.Alloc > prev.Alloc*2 {
            leaks = append(leaks, LeakReport{
                Type:      "memory_growth",
                Severity:  "high",
                Message:   fmt.Sprintf("Memory allocation increased from %d to %d", prev.Alloc, curr.Alloc),
                Timestamp: curr.Timestamp,
            })
        }
        
        // Check for goroutine leaks
        if curr.Goroutines > prev.Goroutines*2 {
            leaks = append(leaks, LeakReport{
                Type:      "goroutine_leak",
                Severity:  "high",
                Message:   fmt.Sprintf("Goroutines increased from %d to %d", prev.Goroutines, curr.Goroutines),
                Timestamp: curr.Timestamp,
            })
        }
    }
    
    return leaks
}

type LeakReport struct {
    Type      string
    Severity  string
    Message   string
    Timestamp time.Time
}
```

### Goroutine Debugging
```go
// Goroutine debugging and monitoring
type GoroutineDebugger struct {
    config *tusk.Config
}

func (gd *GoroutineDebugger) MonitorGoroutines(ctx context.Context) {
    ticker := time.NewTicker(5 * time.Second)
    defer ticker.Stop()
    
    for {
        select {
        case <-ctx.Done():
            return
        case <-ticker.C:
            gd.checkGoroutines()
        }
    }
}

func (gd *GoroutineDebugger) checkGoroutines() {
    count := runtime.NumGoroutine()
    
    // Check for excessive goroutines
    threshold := gd.config.GetInt("debug_settings.goroutine_threshold", 1000)
    if count > threshold {
        log.Printf("WARNING: High goroutine count: %d", count)
        
        // Dump goroutine stack traces
        if gd.config.GetBool("debug_settings.dump_goroutines") {
            gd.dumpGoroutines()
        }
    }
}

func (gd *GoroutineDebugger) dumpGoroutines() {
    buf := make([]byte, 1<<20)
    n := runtime.Stack(buf, true)
    
    filename := fmt.Sprintf("goroutines_%d.txt", time.Now().Unix())
    os.WriteFile(filename, buf[:n], 0644)
    
    log.Printf("Goroutine dump written to %s", filename)
}
```

### Database Query Debugging
```go
// Database query debugging and optimization
type QueryDebugger struct {
    config *tusk.Config
    queries []QueryInfo
}

type QueryInfo struct {
    SQL        string
    Args       []interface{}
    Duration   time.Duration
    Timestamp  time.Time
    StackTrace string
}

func (qd *QueryDebugger) DebugQuery(sql string, args []interface{}, fn func() error) error {
    start := time.Now()
    
    // Capture stack trace
    stackTrace := getStackTrace()
    
    err := fn()
    
    duration := time.Since(start)
    
    // Record query information
    queryInfo := QueryInfo{
        SQL:        sql,
        Args:       args,
        Duration:   duration,
        Timestamp:  time.Now(),
        StackTrace: stackTrace,
    }
    
    qd.queries = append(qd.queries, queryInfo)
    
    // Check for slow queries
    slowThreshold := qd.config.GetDuration("debug_settings.slow_query_threshold", 100*time.Millisecond)
    if duration > slowThreshold {
        log.Printf("SLOW QUERY: %s (%.2fms)", sql, duration.Seconds()*1000)
        log.Printf("Stack trace: %s", stackTrace)
    }
    
    return err
}

func (qd *QueryDebugger) GetSlowQueries(threshold time.Duration) []QueryInfo {
    var slowQueries []QueryInfo
    
    for _, query := range qd.queries {
        if query.Duration > threshold {
            slowQueries = append(slowQueries, query)
        }
    }
    
    return slowQueries
}
```

## Debug Tools and Utilities

### Debug Middleware
```go
// Debug middleware for HTTP requests
func DebugMiddleware(config *tusk.Config) func(http.Handler) http.Handler {
    return func(next http.Handler) http.Handler {
        return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
            start := time.Now()
            
            // Create debug context
            debugCtx := &DebugContext{
                RequestID: generateRequestID(),
                Operation: r.Method + " " + r.URL.Path,
                Timestamp: time.Now(),
                Metadata:  make(map[string]interface{}),
            }
            
            // Add debug context to request
            ctx := context.WithValue(r.Context(), "debug_context", debugCtx)
            r = r.WithContext(ctx)
            
            // Log request
            if config.GetBool("debug_settings.show_http_requests") {
                debugCtx.Log("INFO", "HTTP Request", map[string]interface{}{
                    "method": r.Method,
                    "url":    r.URL.String(),
                    "headers": r.Header,
                })
            }
            
            // Execute request
            next.ServeHTTP(w, r)
            
            // Log response
            duration := time.Since(start)
            debugCtx.Log("INFO", "HTTP Response", map[string]interface{}{
                "duration_ms": duration.Milliseconds(),
            })
        })
    }
}
```

### Debug Utilities
```go
// Debug utilities for common debugging tasks
type DebugUtils struct {
    config *tusk.Config
}

func (du *DebugUtils) DumpObject(obj interface{}, name string) {
    data, err := json.MarshalIndent(obj, "", "  ")
    if err != nil {
        log.Printf("Failed to marshal %s: %v", name, err)
        return
    }
    
    filename := fmt.Sprintf("debug_%s_%d.json", name, time.Now().Unix())
    os.WriteFile(filename, data, 0644)
    
    log.Printf("Object %s dumped to %s", name, filename)
}

func (du *DebugUtils) CompareObjects(obj1, obj2 interface{}, name string) {
    data1, _ := json.Marshal(obj1)
    data2, _ := json.Marshal(obj2)
    
    if bytes.Equal(data1, data2) {
        log.Printf("Objects %s are identical", name)
    } else {
        log.Printf("Objects %s are different", name)
        
        // Save both objects for comparison
        du.DumpObject(obj1, name+"_1")
        du.DumpObject(obj2, name+"_2")
    }
}

func (du *DebugUtils) MonitorFunction(name string, fn func()) {
    start := time.Now()
    
    // Capture memory before
    var memBefore runtime.MemStats
    runtime.ReadMemStats(&memBefore)
    
    // Execute function
    fn()
    
    // Capture memory after
    var memAfter runtime.MemStats
    runtime.ReadMemStats(&memAfter)
    
    duration := time.Since(start)
    memoryDelta := int64(memAfter.Alloc) - int64(memBefore.Alloc)
    
    log.Printf("Function %s: duration=%.2fms, memory_delta=%d bytes", 
        name, duration.Seconds()*1000, memoryDelta)
}
```

## Validation and Error Handling

### Debug Configuration Validation
```go
// Validate debug configuration
func ValidateDebugConfig(config *tusk.Config) error {
    if config == nil {
        return errors.New("debug config cannot be nil")
    }
    
    // Validate logging configuration
    if !config.Has("debug_environment.logging") {
        return errors.New("missing logging configuration")
    }
    
    // Validate tracing configuration
    if config.GetBool("debug_environment.tracing.enabled") {
        if config.GetString("debug_environment.tracing.exporter") == "" {
            return errors.New("tracing exporter must be specified when tracing is enabled")
        }
    }
    
    return nil
}
```

### Error Handling
```go
// Handle debug errors gracefully
func handleDebugError(err error, context string) {
    log.Printf("Debug error in %s: %v", context, err)
    
    // Don't let debug errors affect production code
    if os.Getenv("DEBUG_MODE") != "true" {
        return
    }
    
    // Additional debug error handling
    if debugErr, ok := err.(*DebugError); ok {
        log.Printf("Debug context: %s", debugErr.Context)
    }
}
```

## Performance Considerations

### Debug Performance Impact
```go
// Minimize debug performance impact
type DebugPerformanceMonitor struct {
    config *tusk.Config
    metrics map[string]time.Duration
}

func (dpm *DebugPerformanceMonitor) MeasureDebugOverhead(operation string, fn func()) {
    start := time.Now()
    fn()
    duration := time.Since(start)
    
    dpm.metrics[operation] = duration
    
    // Log if debug overhead is significant
    threshold := dpm.config.GetDuration("debug_settings.overhead_threshold", 10*time.Millisecond)
    if duration > threshold {
        log.Printf("WARNING: Debug overhead for %s: %.2fms", operation, duration.Seconds()*1000)
    }
}
```

## Debugging Notes

- **Observability**: Implement comprehensive observability for better debugging
- **Structured Logging**: Use structured logging for better log analysis
- **Tracing**: Implement distributed tracing for complex systems
- **Profiling**: Use profiling to identify performance bottlenecks
- **Memory Debugging**: Monitor memory usage and detect leaks
- **Goroutine Debugging**: Track goroutine count and detect leaks
- **Query Debugging**: Monitor database query performance
- **Error Tracking**: Capture detailed error information with context

## Best Practices

1. **Debug Configuration**: Use TuskLang for flexible debug configuration
2. **Structured Debugging**: Use structured data for better debugging
3. **Performance Monitoring**: Monitor debug overhead
4. **Error Context**: Capture rich context for errors
5. **Memory Management**: Monitor memory usage and detect leaks
6. **Goroutine Management**: Track goroutine count and detect leaks
7. **Query Optimization**: Monitor and optimize database queries
8. **Production Safety**: Ensure debug code doesn't affect production

## Integration with TuskLang

```go
// Load debug configuration from TuskLang
func LoadDebugConfig(configPath string) (*tusk.Config, error) {
    config, err := tusk.LoadConfig(configPath)
    if err != nil {
        return nil, fmt.Errorf("failed to load debug config: %w", err)
    }
    
    // Validate debug configuration
    if err := ValidateDebugConfig(config); err != nil {
        return nil, fmt.Errorf("invalid debug config: %w", err)
    }
    
    return config, nil
}
```

This debugging techniques guide provides comprehensive debugging capabilities for your Go applications using TuskLang. Remember, good debugging is the key to maintaining reliable software. 