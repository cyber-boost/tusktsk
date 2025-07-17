# @metrics - Metrics Function in Java

The `@metrics` operator provides comprehensive metrics collection and monitoring capabilities for Java applications, integrating with Spring Boot's Micrometer, Prometheus, and enterprise monitoring systems.

## Basic Syntax

```java
// TuskLang configuration
response_time: @metrics("response_time_ms", 150)
error_rate: @metrics("error_rate_percent", 2.5)
throughput: @metrics("requests_per_second", 1000)
```

```java
// Java Spring Boot integration
@Configuration
public class MetricsConfig {
    
    @Bean
    public MetricsService metricsService(MeterRegistry meterRegistry) {
        return MetricsService.builder()
            .meterRegistry(meterRegistry)
            .enablePrometheus(true)
            .enableJmx(true)
            .build();
    }
}
```

## Basic Metrics

```java
// Java metrics service
@Component
public class MetricsService {
    
    @Autowired
    private MeterRegistry meterRegistry;
    
    private final Map<String, Timer> timers = new ConcurrentHashMap<>();
    private final Map<String, Counter> counters = new ConcurrentHashMap<>();
    private final Map<String, Gauge> gauges = new ConcurrentHashMap<>();
    
    public void recordMetric(String name, double value) {
        Gauge.builder(name)
            .register(meterRegistry, () -> value);
    }
    
    public void recordMetric(String name, double value, Map<String, String> tags) {
        Gauge.builder(name)
            .tags(tags.entrySet().stream()
                .map(entry -> Tag.of(entry.getKey(), entry.getValue()))
                .collect(Collectors.toList()))
            .register(meterRegistry, () -> value);
    }
    
    public void incrementCounter(String name) {
        Counter.builder(name)
            .register(meterRegistry)
            .increment();
    }
    
    public void incrementCounter(String name, Map<String, String> tags) {
        Counter.builder(name)
            .tags(tags.entrySet().stream()
                .map(entry -> Tag.of(entry.getKey(), entry.getValue()))
                .collect(Collectors.toList()))
            .register(meterRegistry)
            .increment();
    }
    
    public Timer.Sample startTimer(String name) {
        return Timer.start(meterRegistry);
    }
    
    public void stopTimer(Timer.Sample sample, String name) {
        sample.stop(Timer.builder(name).register(meterRegistry));
    }
    
    public void stopTimer(Timer.Sample sample, String name, Map<String, String> tags) {
        sample.stop(Timer.builder(name)
            .tags(tags.entrySet().stream()
                .map(entry -> Tag.of(entry.getKey(), entry.getValue()))
                .collect(Collectors.toList()))
            .register(meterRegistry));
    }
}
```

```java
// TuskLang metrics
metrics_config: {
    # Basic metrics
    response_time: @metrics("response_time_ms", 150)
    error_rate: @metrics("error_rate_percent", 2.5)
    throughput: @metrics("requests_per_second", 1000)
    
    # Metrics with tags
    api_response_time: @metrics("api_response_time_ms", 200, {
        endpoint: "/api/users"
        method: "GET"
        status: "200"
    })
    
    # Counter metrics
    request_count: @metrics.counter("requests_total")
    error_count: @metrics.counter("errors_total")
    
    # Timer metrics
    database_query_time: @metrics.timer("database_query_time_ms")
    external_api_time: @metrics.timer("external_api_time_ms")
}
```

## Performance Metrics

```java
// Java performance metrics
@Component
public class PerformanceMetricsService {
    
    @Autowired
    private MetricsService metricsService;
    
    @Autowired
    private MeterRegistry meterRegistry;
    
    public void recordResponseTime(String endpoint, String method, int statusCode, long duration) {
        Map<String, String> tags = Map.of(
            "endpoint", endpoint,
            "method", method,
            "status", String.valueOf(statusCode)
        );
        
        Timer.builder("http.server.requests")
            .tags(tags.entrySet().stream()
                .map(entry -> Tag.of(entry.getKey(), entry.getValue()))
                .collect(Collectors.toList()))
            .register(meterRegistry)
            .record(duration, TimeUnit.MILLISECONDS);
    }
    
    public void recordThroughput(String operation, long count) {
        Counter.builder("throughput")
            .tag("operation", operation)
            .register(meterRegistry)
            .increment(count);
    }
    
    public void recordErrorRate(String operation, double errorRate) {
        Gauge.builder("error_rate")
            .tag("operation", operation)
            .register(meterRegistry, () -> errorRate);
    }
    
    public void recordMemoryUsage() {
        Runtime runtime = Runtime.getRuntime();
        long usedMemory = runtime.totalMemory() - runtime.freeMemory();
        long maxMemory = runtime.maxMemory();
        
        Gauge.builder("jvm.memory.used")
            .register(meterRegistry, () -> usedMemory);
        
        Gauge.builder("jvm.memory.max")
            .register(meterRegistry, () -> maxMemory);
        
        Gauge.builder("jvm.memory.usage")
            .register(meterRegistry, () -> (double) usedMemory / maxMemory * 100);
    }
    
    public void recordCpuUsage() {
        OperatingSystemMXBean osBean = ManagementFactory.getOperatingSystemMXBean();
        
        Gauge.builder("system.cpu.usage")
            .register(meterRegistry, () -> osBean.getSystemLoadAverage());
        
        Gauge.builder("system.cpu.count")
            .register(meterRegistry, () -> osBean.getAvailableProcessors());
    }
}
```

```java
// TuskLang performance metrics
performance_metrics: {
    # Response time metrics
    api_response_time: @metrics.performance("api_response_time_ms", 200, {
        endpoint: "/api/users"
        method: "GET"
    })
    
    # Throughput metrics
    requests_per_second: @metrics.performance("requests_per_second", 1000)
    transactions_per_second: @metrics.performance("transactions_per_second", 500)
    
    # Error rate metrics
    error_rate: @metrics.performance("error_rate_percent", 2.5)
    failure_rate: @metrics.performance("failure_rate_percent", 1.0)
    
    # Memory metrics
    memory_usage: @metrics.performance("memory_usage_percent", 75.0)
    heap_usage: @metrics.performance("heap_usage_percent", 60.0)
    
    # CPU metrics
    cpu_usage: @metrics.performance("cpu_usage_percent", 45.0)
    cpu_load: @metrics.performance("cpu_load_average", 2.5)
}
```

## Business Metrics

```java
// Java business metrics
@Component
public class BusinessMetricsService {
    
    @Autowired
    private MetricsService metricsService;
    
    @Autowired
    private MeterRegistry meterRegistry;
    
    public void recordUserRegistration(String source) {
        Counter.builder("user.registrations")
            .tag("source", source)
            .register(meterRegistry)
            .increment();
    }
    
    public void recordOrderValue(String productCategory, double value) {
        Timer.builder("order.value")
            .tag("category", productCategory)
            .register(meterRegistry)
            .record(value, TimeUnit.MILLISECONDS);
    }
    
    public void recordConversionRate(String funnel, double rate) {
        Gauge.builder("conversion.rate")
            .tag("funnel", funnel)
            .register(meterRegistry, () -> rate);
    }
    
    public void recordCustomerSatisfaction(String product, double score) {
        Gauge.builder("customer.satisfaction")
            .tag("product", product)
            .register(meterRegistry, () -> score);
    }
    
    public void recordRevenue(String region, double amount) {
        Counter.builder("revenue.total")
            .tag("region", region)
            .register(meterRegistry)
            .increment((long) amount);
    }
}
```

```java
// TuskLang business metrics
business_metrics: {
    # User metrics
    user_registrations: @metrics.business("user_registrations_total", 150)
    active_users: @metrics.business("active_users", 1000)
    user_retention: @metrics.business("user_retention_rate", 85.5)
    
    # Revenue metrics
    total_revenue: @metrics.business("total_revenue", 50000.0)
    average_order_value: @metrics.business("average_order_value", 125.0)
    revenue_growth: @metrics.business("revenue_growth_percent", 15.5)
    
    # Conversion metrics
    conversion_rate: @metrics.business("conversion_rate", 3.2)
    cart_abandonment: @metrics.business("cart_abandonment_rate", 68.0)
    
    # Customer metrics
    customer_satisfaction: @metrics.business("customer_satisfaction_score", 4.5)
    customer_lifetime_value: @metrics.business("customer_lifetime_value", 2500.0)
}
```

## Custom Metrics

```java
// Java custom metrics
@Component
public class CustomMetricsService {
    
    @Autowired
    private MetricsService metricsService;
    
    @Autowired
    private MeterRegistry meterRegistry;
    
    public void recordCustomMetric(String name, double value, String... tags) {
        if (tags.length % 2 != 0) {
            throw new IllegalArgumentException("Tags must be key-value pairs");
        }
        
        Map<String, String> tagMap = new HashMap<>();
        for (int i = 0; i < tags.length; i += 2) {
            tagMap.put(tags[i], tags[i + 1]);
        }
        
        Gauge.builder(name)
            .tags(tagMap.entrySet().stream()
                .map(entry -> Tag.of(entry.getKey(), entry.getValue()))
                .collect(Collectors.toList()))
            .register(meterRegistry, () -> value);
    }
    
    public void recordHistogram(String name, double value, String... tags) {
        if (tags.length % 2 != 0) {
            throw new IllegalArgumentException("Tags must be key-value pairs");
        }
        
        Map<String, String> tagMap = new HashMap<>();
        for (int i = 0; i < tags.length; i += 2) {
            tagMap.put(tags[i], tags[i + 1]);
        }
        
        Histogram.builder(name)
            .tags(tagMap.entrySet().stream()
                .map(entry -> Tag.of(entry.getKey(), entry.getValue()))
                .collect(Collectors.toList()))
            .register(meterRegistry)
            .record(value);
    }
    
    public void recordDistributionSummary(String name, double value, String... tags) {
        if (tags.length % 2 != 0) {
            throw new IllegalArgumentException("Tags must be key-value pairs");
        }
        
        Map<String, String> tagMap = new HashMap<>();
        for (int i = 0; i < tags.length; i += 2) {
            tagMap.put(tags[i], tags[i + 1]);
        }
        
        DistributionSummary.builder(name)
            .tags(tagMap.entrySet().stream()
                .map(entry -> Tag.of(entry.getKey(), entry.getValue()))
                .collect(Collectors.toList()))
            .register(meterRegistry)
            .record(value);
    }
}
```

```java
// TuskLang custom metrics
custom_metrics: {
    # Custom gauge metrics
    custom_gauge: @metrics.custom("custom_gauge", 42.0, {
        label1: "value1"
        label2: "value2"
    })
    
    # Custom histogram metrics
    custom_histogram: @metrics.custom.histogram("custom_histogram", 150.0, {
        bucket: "response_time"
        threshold: "200ms"
    })
    
    # Custom distribution summary
    custom_distribution: @metrics.custom.distribution("custom_distribution", 1000.0, {
        operation: "database_query"
        table: "users"
    })
    
    # Custom counter with tags
    custom_counter: @metrics.custom.counter("custom_counter", {
        operation: "file_upload"
        status: "success"
    })
}
```

## Metrics Aggregation

```java
// Java metrics aggregation
@Component
public class MetricsAggregationService {
    
    @Autowired
    private MetricsService metricsService;
    
    @Autowired
    private MeterRegistry meterRegistry;
    
    public AggregatedMetrics aggregateMetrics(String metricName, Duration window) {
        List<Meter> meters = meterRegistry.find(metricName).meters();
        
        double sum = 0.0;
        double min = Double.MAX_VALUE;
        double max = Double.MIN_VALUE;
        int count = 0;
        
        for (Meter meter : meters) {
            if (meter instanceof Gauge) {
                double value = ((Gauge) meter).value();
                sum += value;
                min = Math.min(min, value);
                max = Math.max(max, value);
                count++;
            }
        }
        
        return AggregatedMetrics.builder()
            .metricName(metricName)
            .sum(sum)
            .min(min)
            .max(max)
            .average(count > 0 ? sum / count : 0.0)
            .count(count)
            .window(window)
            .build();
    }
    
    public Map<String, Double> getPercentiles(String metricName, double... percentiles) {
        Timer timer = Timer.builder(metricName).register(meterRegistry);
        
        Map<String, Double> results = new HashMap<>();
        for (double percentile : percentiles) {
            double value = timer.percentile(percentile, TimeUnit.MILLISECONDS);
            results.put("p" + (int) (percentile * 100), value);
        }
        
        return results;
    }
}
```

```java
// TuskLang metrics aggregation
metrics_aggregation: {
    # Aggregated metrics
    aggregated_response_time: @metrics.aggregate("response_time_ms", {
        window: "5m"
        functions: ["sum", "avg", "min", "max", "count"]
    })
    
    # Percentile metrics
    response_time_percentiles: @metrics.percentiles("response_time_ms", [0.5, 0.9, 0.95, 0.99])
    
    # Rolling averages
    rolling_avg_response_time: @metrics.rolling_average("response_time_ms", "1m")
    rolling_avg_error_rate: @metrics.rolling_average("error_rate", "5m")
    
    # Rate calculations
    request_rate: @metrics.rate("requests_total", "1m")
    error_rate: @metrics.rate("errors_total", "1m")
}
```

## Metrics Export

```java
// Java metrics export
@Component
public class MetricsExportService {
    
    @Autowired
    private MetricsService metricsService;
    
    @Autowired
    private MeterRegistry meterRegistry;
    
    public String exportPrometheusFormat() {
        StringBuilder sb = new StringBuilder();
        
        for (Meter meter : meterRegistry.getMeters()) {
            if (meter instanceof Gauge) {
                Gauge gauge = (Gauge) meter;
                sb.append(formatPrometheusMetric(gauge.getId(), gauge.value()));
            } else if (meter instanceof Counter) {
                Counter counter = (Counter) meter;
                sb.append(formatPrometheusMetric(counter.getId(), counter.count()));
            } else if (meter instanceof Timer) {
                Timer timer = (Timer) meter;
                sb.append(formatPrometheusMetric(timer.getId(), timer.totalTime(TimeUnit.SECONDS)));
            }
        }
        
        return sb.toString();
    }
    
    public Map<String, Object> exportJsonFormat() {
        Map<String, Object> metrics = new HashMap<>();
        
        for (Meter meter : meterRegistry.getMeters()) {
            Map<String, Object> metricData = new HashMap<>();
            metricData.put("type", meter.getId().getType().name());
            metricData.put("tags", meter.getId().getTags());
            
            if (meter instanceof Gauge) {
                metricData.put("value", ((Gauge) meter).value());
            } else if (meter instanceof Counter) {
                metricData.put("value", ((Counter) meter).count());
            } else if (meter instanceof Timer) {
                Timer timer = (Timer) meter;
                metricData.put("total_time", timer.totalTime(TimeUnit.MILLISECONDS));
                metricData.put("count", timer.count());
                metricData.put("mean", timer.mean(TimeUnit.MILLISECONDS));
            }
            
            metrics.put(meter.getId().getName(), metricData);
        }
        
        return metrics;
    }
    
    private String formatPrometheusMetric(Meter.Id id, double value) {
        StringBuilder sb = new StringBuilder();
        sb.append(id.getName());
        
        if (!id.getTags().isEmpty()) {
            sb.append("{");
            sb.append(id.getTags().stream()
                .map(tag -> tag.getKey() + "=\"" + tag.getValue() + "\"")
                .collect(Collectors.joining(",")));
            sb.append("}");
        }
        
        sb.append(" ").append(value).append("\n");
        return sb.toString();
    }
}
```

```java
// TuskLang metrics export
metrics_export: {
    # Export to Prometheus format
    prometheus_metrics: @metrics.export.prometheus()
    
    # Export to JSON format
    json_metrics: @metrics.export.json()
    
    # Export specific metrics
    custom_export: @metrics.export.custom({
        format: "json"
        metrics: ["response_time", "error_rate", "throughput"]
        include_tags: true
    })
    
    # Export with filtering
    filtered_export: @metrics.export.filter({
        format: "prometheus"
        include: ["api.*", "database.*"]
        exclude: ["debug.*", "test.*"]
    })
}
```

## Metrics Alerting

```java
// Java metrics alerting
@Component
public class MetricsAlertingService {
    
    @Autowired
    private MetricsService metricsService;
    
    @Autowired
    private MeterRegistry meterRegistry;
    
    public void checkAlerts() {
        checkResponseTimeAlert();
        checkErrorRateAlert();
        checkMemoryUsageAlert();
        checkCpuUsageAlert();
    }
    
    private void checkResponseTimeAlert() {
        Timer responseTimeTimer = Timer.builder("http.server.requests").register(meterRegistry);
        double p95ResponseTime = responseTimeTimer.percentile(0.95, TimeUnit.MILLISECONDS);
        
        if (p95ResponseTime > 500) {
            sendAlert("High response time detected", Map.of(
                "metric", "response_time_p95",
                "value", p95ResponseTime,
                "threshold", 500
            ));
        }
    }
    
    private void checkErrorRateAlert() {
        Counter errorCounter = Counter.builder("http.server.requests")
            .tag("status", "5xx")
            .register(meterRegistry);
        
        Counter totalCounter = Counter.builder("http.server.requests").register(meterRegistry);
        
        double errorRate = totalCounter.count() > 0 ? 
            (errorCounter.count() / totalCounter.count()) * 100 : 0;
        
        if (errorRate > 5.0) {
            sendAlert("High error rate detected", Map.of(
                "metric", "error_rate",
                "value", errorRate,
                "threshold", 5.0
            ));
        }
    }
    
    private void checkMemoryUsageAlert() {
        Runtime runtime = Runtime.getRuntime();
        double memoryUsage = (double) (runtime.totalMemory() - runtime.freeMemory()) / runtime.maxMemory() * 100;
        
        if (memoryUsage > 90) {
            sendAlert("High memory usage detected", Map.of(
                "metric", "memory_usage",
                "value", memoryUsage,
                "threshold", 90
            ));
        }
    }
    
    private void checkCpuUsageAlert() {
        OperatingSystemMXBean osBean = ManagementFactory.getOperatingSystemMXBean();
        double cpuLoad = osBean.getSystemLoadAverage();
        
        if (cpuLoad > 0.8) {
            sendAlert("High CPU usage detected", Map.of(
                "metric", "cpu_load",
                "value", cpuLoad,
                "threshold", 0.8
            ));
        }
    }
    
    private void sendAlert(String message, Map<String, Object> details) {
        // Implementation for sending alerts (email, Slack, etc.)
        log.warn("Alert: {} - Details: {}", message, details);
    }
}
```

```java
// TuskLang metrics alerting
metrics_alerting: {
    # Response time alerts
    response_time_alert: @metrics.alert("response_time_ms", {
        threshold: 500
        condition: ">"
        severity: "warning"
        message: "High response time detected"
    })
    
    # Error rate alerts
    error_rate_alert: @metrics.alert("error_rate_percent", {
        threshold: 5.0
        condition: ">"
        severity: "critical"
        message: "High error rate detected"
    })
    
    # Memory usage alerts
    memory_alert: @metrics.alert("memory_usage_percent", {
        threshold: 90
        condition: ">"
        severity: "warning"
        message: "High memory usage detected"
    })
    
    # Custom alert conditions
    custom_alert: @metrics.alert.custom({
        metric: "custom_metric"
        condition: "value > threshold * 2"
        threshold: 100
        severity: "critical"
        message: "Custom alert triggered"
    })
}
```

## Metrics Testing

```java
// JUnit test for metrics
@SpringBootTest
class MetricsServiceTest {
    
    @Autowired
    private MetricsService metricsService;
    
    @Autowired
    private MeterRegistry meterRegistry;
    
    @Test
    void testRecordMetric() {
        metricsService.recordMetric("test_metric", 42.0);
        
        Gauge gauge = meterRegistry.find("test_metric").gauge();
        assertThat(gauge).isNotNull();
        assertThat(gauge.value()).isEqualTo(42.0);
    }
    
    @Test
    void testIncrementCounter() {
        metricsService.incrementCounter("test_counter");
        
        Counter counter = meterRegistry.find("test_counter").counter();
        assertThat(counter).isNotNull();
        assertThat(counter.count()).isEqualTo(1.0);
    }
    
    @Test
    void testTimer() {
        Timer.Sample sample = metricsService.startTimer("test_timer");
        Thread.sleep(100); // Simulate work
        metricsService.stopTimer(sample, "test_timer");
        
        Timer timer = meterRegistry.find("test_timer").timer();
        assertThat(timer).isNotNull();
        assertThat(timer.count()).isEqualTo(1);
        assertThat(timer.totalTime(TimeUnit.MILLISECONDS)).isGreaterThan(0);
    }
    
    @Test
    void testMetricsWithTags() {
        Map<String, String> tags = Map.of("tag1", "value1", "tag2", "value2");
        metricsService.recordMetric("test_metric_with_tags", 100.0, tags);
        
        Gauge gauge = meterRegistry.find("test_metric_with_tags").gauge();
        assertThat(gauge).isNotNull();
        assertThat(gauge.getId().getTags()).contains(
            Tag.of("tag1", "value1"),
            Tag.of("tag2", "value2")
        );
    }
}
```

```java
// TuskLang metrics testing
test_metrics: {
    # Test basic metrics
    test_metric: @metrics("test_metric", 42.0)
    assert(@test_metric == 42.0, "Metric should be recorded correctly")
    
    # Test counter metrics
    @metrics.counter("test_counter")
    assert(@metrics.get("test_counter") > 0, "Counter should be incremented")
    
    # Test timer metrics
    timer_result: @metrics.timer("test_timer", {
        operation: "test_operation"
    })
    assert(@timer_result.duration > 0, "Timer should record duration")
    
    # Test metrics with tags
    tagged_metric: @metrics("test_tagged_metric", 100.0, {
        tag1: "value1"
        tag2: "value2"
    })
    assert(@tagged_metric == 100.0, "Tagged metric should be recorded correctly")
}
```

## Best Practices

### 1. Metrics Naming Convention
```java
// Use consistent naming conventions
@Component
public class MetricsNamingService {
    
    @Autowired
    private MetricsService metricsService;
    
    public void recordHttpMetric(String endpoint, String method, int statusCode, long duration) {
        // Use dot notation for hierarchical metrics
        String metricName = "http.server.requests";
        
        Map<String, String> tags = Map.of(
            "endpoint", endpoint,
            "method", method,
            "status", String.valueOf(statusCode)
        );
        
        metricsService.recordMetric(metricName, duration, tags);
    }
    
    public void recordDatabaseMetric(String operation, String table, long duration) {
        // Use consistent naming for database metrics
        String metricName = "database.operations";
        
        Map<String, String> tags = Map.of(
            "operation", operation,
            "table", table
        );
        
        metricsService.recordMetric(metricName, duration, tags);
    }
    
    public void recordBusinessMetric(String event, String category, double value) {
        // Use business-specific naming
        String metricName = "business." + event;
        
        Map<String, String> tags = Map.of(
            "category", category
        );
        
        metricsService.recordMetric(metricName, value, tags);
    }
}
```

### 2. Metrics Cardinality Management
```java
// Manage metrics cardinality to prevent explosion
@Component
public class MetricsCardinalityService {
    
    @Autowired
    private MetricsService metricsService;
    
    public void recordUserMetric(String userId, String action) {
        // Use bounded sets for high-cardinality values
        String boundedUserId = boundUserId(userId);
        
        Map<String, String> tags = Map.of(
            "user_id", boundedUserId,
            "action", action
        );
        
        metricsService.incrementCounter("user.actions", tags);
    }
    
    private String boundUserId(String userId) {
        // Limit cardinality by using user segments instead of individual IDs
        if (userId.length() > 10) {
            return userId.substring(0, 10) + "...";
        }
        return userId;
    }
    
    public void recordRequestMetric(String url, String method) {
        // Normalize URLs to reduce cardinality
        String normalizedUrl = normalizeUrl(url);
        
        Map<String, String> tags = Map.of(
            "url", normalizedUrl,
            "method", method
        );
        
        metricsService.incrementCounter("http.requests", tags);
    }
    
    private String normalizeUrl(String url) {
        // Remove dynamic parts from URLs
        return url.replaceAll("/\\d+", "/{id}")
                 .replaceAll("/[a-f0-9]{8,}", "/{hash}");
    }
}
```

### 3. Metrics Performance Optimization
```java
// Optimize metrics collection for performance
@Component
public class MetricsPerformanceService {
    
    @Autowired
    private MetricsService metricsService;
    
    private final Map<String, Timer.Sample> activeTimers = new ConcurrentHashMap<>();
    
    public void startOperationTimer(String operationId) {
        Timer.Sample sample = Timer.start();
        activeTimers.put(operationId, sample);
    }
    
    public void stopOperationTimer(String operationId, String operationName) {
        Timer.Sample sample = activeTimers.remove(operationId);
        if (sample != null) {
            metricsService.stopTimer(sample, "operation.duration", 
                Map.of("operation", operationName));
        }
    }
    
    public void recordBatchMetrics(List<MetricData> metrics) {
        // Batch metrics recording for better performance
        for (MetricData metric : metrics) {
            metricsService.recordMetric(metric.getName(), metric.getValue(), metric.getTags());
        }
    }
    
    @Scheduled(fixedRate = 60000) // Every minute
    public void recordSystemMetrics() {
        // Record system metrics periodically
        recordMemoryMetrics();
        recordCpuMetrics();
        recordGcMetrics();
    }
    
    private void recordMemoryMetrics() {
        Runtime runtime = Runtime.getRuntime();
        long usedMemory = runtime.totalMemory() - runtime.freeMemory();
        
        metricsService.recordMetric("jvm.memory.used", usedMemory);
        metricsService.recordMetric("jvm.memory.max", runtime.maxMemory());
    }
}
```

The `@metrics` operator in Java provides comprehensive metrics collection and monitoring capabilities that enable applications to track performance, business metrics, and system health in enterprise environments. 