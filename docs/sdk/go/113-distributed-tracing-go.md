# 🔍 Distributed Tracing & Observability with TuskLang & Go

## Introduction
Distributed tracing is the key to understanding complex, microservice architectures. TuskLang and Go let you implement comprehensive observability with config-driven tracing, metrics, and logging that spans your entire system.

## Key Features
- **OpenTelemetry integration**
- **Trace propagation across services**
- **Span management and correlation**
- **Metrics collection and aggregation**
- **Log correlation with traces**
- **Sampling strategies**
- **Trace visualization and analysis**

## Example: Tracing Config
```ini
[tracing]
backend: jaeger
endpoint: @env("JAEGER_ENDPOINT")
sampling_rate: @env("SAMPLING_RATE", 0.1)
service_name: @env("SERVICE_NAME")
metrics: @metrics("trace_duration_ms", 0)
correlation: @go("tracing.CorrelateLogs")
```

## Go: OpenTelemetry Setup
```go
package tracing

import (
    "context"
    "go.opentelemetry.io/otel"
    "go.opentelemetry.io/otel/exporters/jaeger"
    "go.opentelemetry.io/otel/sdk/resource"
    "go.opentelemetry.io/otel/sdk/trace"
    "go.opentelemetry.io/otel/semconv/v1.4.0"
)

func InitTracer(serviceName, endpoint string) (*trace.TracerProvider, error) {
    exp, err := jaeger.New(jaeger.WithCollectorEndpoint(jaeger.WithEndpoint(endpoint)))
    if err != nil {
        return nil, err
    }
    
    tp := trace.NewTracerProvider(
        trace.WithBatcher(exp),
        trace.WithResource(resource.NewWithAttributes(
            semconv.SchemaURL,
            semconv.ServiceNameKey.String(serviceName),
        )),
    )
    
    otel.SetTracerProvider(tp)
    return tp, nil
}
```

## Trace Propagation
```go
func PropagateTrace(ctx context.Context, req *http.Request) context.Context {
    // Extract trace context from headers
    ctx = otel.GetTextMapPropagator().Extract(ctx, propagation.HeaderCarrier(req.Header))
    
    // Create new span for this request
    tracer := otel.Tracer("http-server")
    ctx, span := tracer.Start(ctx, "http-request")
    defer span.End()
    
    // Add request attributes
    span.SetAttributes(
        attribute.String("http.method", req.Method),
        attribute.String("http.url", req.URL.String()),
        attribute.String("http.user_agent", req.UserAgent()),
    )
    
    return ctx
}
```

## Span Management
```go
func ProcessOrder(ctx context.Context, orderID string) error {
    tracer := otel.Tracer("order-service")
    
    ctx, span := tracer.Start(ctx, "process-order")
    defer span.End()
    
    span.SetAttributes(attribute.String("order.id", orderID))
    
    // Validate order
    if err := validateOrder(ctx, orderID); err != nil {
        span.RecordError(err)
        span.SetStatus(codes.Error, err.Error())
        return err
    }
    
    // Process payment
    if err := processPayment(ctx, orderID); err != nil {
        span.RecordError(err)
        span.SetStatus(codes.Error, err.Error())
        return err
    }
    
    // Update inventory
    if err := updateInventory(ctx, orderID); err != nil {
        span.RecordError(err)
        span.SetStatus(codes.Error, err.Error())
        return err
    }
    
    span.SetStatus(codes.Ok, "Order processed successfully")
    return nil
}

func validateOrder(ctx context.Context, orderID string) error {
    tracer := otel.Tracer("order-service")
    ctx, span := tracer.Start(ctx, "validate-order")
    defer span.End()
    
    // Validation logic
    time.Sleep(10 * time.Millisecond)
    return nil
}
```

## Metrics Collection
```go
package metrics

import (
    "go.opentelemetry.io/otel/metric"
    "go.opentelemetry.io/otel/metric/instrument"
)

type MetricsCollector struct {
    requestCounter   instrument.Int64Counter
    requestDuration  instrument.Float64Histogram
    errorCounter     instrument.Int64Counter
}

func NewMetricsCollector(meter metric.Meter) (*MetricsCollector, error) {
    requestCounter, err := meter.Int64Counter(
        "http_requests_total",
        instrument.WithDescription("Total number of HTTP requests"),
    )
    if err != nil {
        return nil, err
    }
    
    requestDuration, err := meter.Float64Histogram(
        "http_request_duration_seconds",
        instrument.WithDescription("HTTP request duration"),
    )
    if err != nil {
        return nil, err
    }
    
    errorCounter, err := meter.Int64Counter(
        "http_errors_total",
        instrument.WithDescription("Total number of HTTP errors"),
    )
    if err != nil {
        return nil, err
    }
    
    return &MetricsCollector{
        requestCounter:  requestCounter,
        requestDuration: requestDuration,
        errorCounter:    errorCounter,
    }, nil
}

func (m *MetricsCollector) RecordRequest(method, path string, duration time.Duration, err error) {
    m.requestCounter.Add(context.Background(), 1, 
        attribute.String("method", method),
        attribute.String("path", path),
    )
    
    m.requestDuration.Record(context.Background(), duration.Seconds(),
        attribute.String("method", method),
        attribute.String("path", path),
    )
    
    if err != nil {
        m.errorCounter.Add(context.Background(), 1,
            attribute.String("method", method),
            attribute.String("path", path),
        )
    }
}
```

## Log Correlation
```go
func CorrelateLogs(ctx context.Context, msg string, fields ...log.Field) {
    span := trace.SpanFromContext(ctx)
    spanContext := span.SpanContext()
    
    // Add trace and span IDs to log
    fields = append(fields,
        log.String("trace_id", spanContext.TraceID().String()),
        log.String("span_id", spanContext.SpanID().String()),
    )
    
    logger.Info(ctx, msg, fields...)
}
```

## Sampling Strategies
```go
type SamplingStrategy interface {
    ShouldSample(ctx context.Context, traceID trace.TraceID, name string, kind trace.SpanKind) bool
}

type AdaptiveSampling struct {
    baseRate    float64
    errorRate   float64
    latencyP95  time.Duration
}

func (a *AdaptiveSampling) ShouldSample(ctx context.Context, traceID trace.TraceID, name string, kind trace.SpanKind) bool {
    // Increase sampling for errors
    if hasError(ctx) {
        return rand.Float64() < a.baseRate*2
    }
    
    // Increase sampling for slow requests
    if getLatency(ctx) > a.latencyP95 {
        return rand.Float64() < a.baseRate*1.5
    }
    
    return rand.Float64() < a.baseRate
}
```

## Performance Monitoring
```go
func MonitorPerformance(ctx context.Context, operation string, fn func() error) error {
    tracer := otel.Tracer("performance")
    ctx, span := tracer.Start(ctx, operation)
    defer span.End()
    
    start := time.Now()
    err := fn()
    duration := time.Since(start)
    
    // Record performance metrics
    metrics.RecordDuration(operation, duration)
    
    if err != nil {
        span.RecordError(err)
        span.SetStatus(codes.Error, err.Error())
    } else {
        span.SetStatus(codes.Ok, "Operation completed successfully")
    }
    
    return err
}
```

## Best Practices
- **Use consistent span naming conventions**
- **Add relevant attributes to spans**
- **Implement proper error handling and status codes**
- **Use sampling to control trace volume**
- **Correlate logs with trace IDs**
- **Monitor trace performance impact**

## Performance Optimization
- **Use async span processing for high-throughput systems**
- **Implement intelligent sampling strategies**
- **Batch trace exports to reduce overhead**
- **Use efficient serialization for trace data**

## Security Considerations
- **Sanitize sensitive data in traces**
- **Implement trace access controls**
- **Use secure connections for trace exporters**
- **Comply with data privacy regulations**

## Troubleshooting
- **Monitor trace sampling rates**
- **Check trace exporter connectivity**
- **Verify span correlation across services**
- **Monitor trace storage performance**

## Conclusion
TuskLang + Go = observability that spans your entire system. See everything, understand everything. 