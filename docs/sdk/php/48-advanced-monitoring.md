# Advanced Monitoring in PHP with TuskLang

## Overview

TuskLang revolutionizes monitoring by making it configuration-driven, intelligent, and adaptive. This guide covers advanced monitoring patterns that leverage TuskLang's dynamic capabilities for comprehensive observability and proactive issue detection.

## 🎯 Monitoring Architecture

### Monitoring Configuration

```ini
# monitoring-architecture.tsk
[monitoring_architecture]
strategy = "full_stack_observability"
platform = "prometheus"
distributed_tracing = "jaeger"
logging = "elasticsearch"

[monitoring_architecture.layers]
infrastructure = {
    metrics = "node_exporter",
    alerting = "alertmanager",
    visualization = "grafana"
}

application = {
    metrics = "prometheus",
    tracing = "jaeger",
    logging = "fluentd"
}

business = {
    metrics = "custom",
    kpis = "dashboard",
    reporting = "automated"
}

[monitoring_architecture.metrics]
system_metrics = true
application_metrics = true
business_metrics = true
custom_metrics = true

[monitoring_architecture.alerting]
enabled = true
channels = ["slack", "email", "pagerduty"]
escalation = true
snooze = true

[monitoring_architecture.retention]
metrics = 30
logs = 90
traces = 7
alerts = 365
```

### Monitoring Manager Implementation

```php
<?php
// MonitoringManager.php
class MonitoringManager
{
    private $config;
    private $metrics;
    private $tracing;
    private $logging;
    private $alerting;
    private $dashboard;
    
    public function __construct()
    {
        $this->config = new TuskConfig('monitoring-architecture.tsk');
        $this->metrics = new MetricsCollector();
        $this->tracing = new DistributedTracer();
        $this->logging = new LogAggregator();
        $this->alerting = new AlertManager();
        $this->dashboard = new DashboardManager();
        $this->initializeMonitoring();
    }
    
    private function initializeMonitoring()
    {
        $strategy = $this->config->get('monitoring_architecture.strategy');
        
        switch ($strategy) {
            case 'full_stack_observability':
                $this->initializeFullStackObservability();
                break;
            case 'application_performance':
                $this->initializeApplicationPerformance();
                break;
            case 'infrastructure_monitoring':
                $this->initializeInfrastructureMonitoring();
                break;
        }
    }
    
    public function monitorRequest($request, $context = [])
    {
        $startTime = microtime(true);
        $traceId = uniqid();
        
        try {
            // Start distributed trace
            $span = $this->tracing->startSpan('request_processing', [
                'trace_id' => $traceId,
                'request_id' => $request['id'] ?? uniqid(),
                'user_id' => $context['user_id'] ?? null
            ]);
            
            // Record request metrics
            $this->recordRequestMetrics($request, $context);
            
            // Process request
            $response = $this->processRequest($request, $context);
            
            // Record response metrics
            $this->recordResponseMetrics($response, $context);
            
            // End trace
            $this->tracing->endSpan($span, [
                'status' => 'success',
                'duration' => (microtime(true) - $startTime) * 1000
            ]);
            
            // Log request
            $this->logRequest($request, $response, $context, $startTime);
            
            return $response;
            
        } catch (Exception $e) {
            // Record error metrics
            $this->recordErrorMetrics($e, $context);
            
            // End trace with error
            $this->tracing->endSpan($span, [
                'status' => 'error',
                'error' => $e->getMessage(),
                'duration' => (microtime(true) - $startTime) * 1000
            ]);
            
            // Log error
            $this->logError($e, $request, $context, $startTime);
            
            throw $e;
        }
    }
    
    private function recordRequestMetrics($request, $context)
    {
        $metrics = [
            'request_count' => 1,
            'request_size' => strlen(json_encode($request)),
            'user_agent' => $context['user_agent'] ?? 'unknown',
            'ip_address' => $context['ip_address'] ?? 'unknown'
        ];
        
        $this->metrics->record('requests', $metrics, [
            'method' => $request['method'] ?? 'unknown',
            'endpoint' => $request['endpoint'] ?? 'unknown',
            'status' => 'started'
        ]);
    }
    
    private function recordResponseMetrics($response, $context)
    {
        $metrics = [
            'response_count' => 1,
            'response_size' => strlen(json_encode($response)),
            'response_time' => $context['duration'] ?? 0
        ];
        
        $this->metrics->record('responses', $metrics, [
            'status_code' => $response['status_code'] ?? 200,
            'endpoint' => $context['endpoint'] ?? 'unknown'
        ]);
    }
    
    private function recordErrorMetrics($exception, $context)
    {
        $metrics = [
            'error_count' => 1,
            'error_type' => get_class($exception)
        ];
        
        $this->metrics->record('errors', $metrics, [
            'error_message' => $exception->getMessage(),
            'endpoint' => $context['endpoint'] ?? 'unknown'
        ]);
    }
    
    public function getSystemHealth()
    {
        $health = [
            'status' => 'healthy',
            'checks' => [],
            'timestamp' => time()
        ];
        
        // Check infrastructure health
        $infrastructureHealth = $this->checkInfrastructureHealth();
        $health['checks']['infrastructure'] = $infrastructureHealth;
        
        // Check application health
        $applicationHealth = $this->checkApplicationHealth();
        $health['checks']['application'] = $applicationHealth;
        
        // Check business health
        $businessHealth = $this->checkBusinessHealth();
        $health['checks']['business'] = $businessHealth;
        
        // Determine overall status
        $health['status'] = $this->determineOverallStatus($health['checks']);
        
        return $health;
    }
    
    private function checkInfrastructureHealth()
    {
        $checks = [];
        
        // CPU usage
        $checks['cpu'] = $this->checkCPUUsage();
        
        // Memory usage
        $checks['memory'] = $this->checkMemoryUsage();
        
        // Disk usage
        $checks['disk'] = $this->checkDiskUsage();
        
        // Network connectivity
        $checks['network'] = $this->checkNetworkConnectivity();
        
        return $checks;
    }
    
    private function checkApplicationHealth()
    {
        $checks = [];
        
        // Database connectivity
        $checks['database'] = $this->checkDatabaseHealth();
        
        // Cache connectivity
        $checks['cache'] = $this->checkCacheHealth();
        
        // External services
        $checks['external_services'] = $this->checkExternalServices();
        
        // Application performance
        $checks['performance'] = $this->checkApplicationPerformance();
        
        return $checks;
    }
    
    private function checkBusinessHealth()
    {
        $checks = [];
        
        // Key performance indicators
        $checks['kpis'] = $this->checkKPIs();
        
        // Business metrics
        $checks['business_metrics'] = $this->checkBusinessMetrics();
        
        // User experience
        $checks['user_experience'] = $this->checkUserExperience();
        
        return $checks;
    }
    
    private function determineOverallStatus($checks)
    {
        $criticalIssues = 0;
        $warningIssues = 0;
        
        foreach ($checks as $category => $categoryChecks) {
            foreach ($categoryChecks as $check) {
                if ($check['status'] === 'critical') {
                    $criticalIssues++;
                } elseif ($check['status'] === 'warning') {
                    $warningIssues++;
                }
            }
        }
        
        if ($criticalIssues > 0) {
            return 'critical';
        } elseif ($warningIssues > 0) {
            return 'warning';
        } else {
            return 'healthy';
        }
    }
}
```

## 📊 Metrics Collection

### Metrics Configuration

```ini
# metrics-collection.tsk
[metrics_collection]
enabled = true
sampling_rate = 0.1
retention_days = 30

[metrics_collection.types]
counter = {
    enabled = true,
    aggregation = "sum",
    retention = 30
}

gauge = {
    enabled = true,
    aggregation = "avg",
    retention = 30
}

histogram = {
    enabled = true,
    buckets = [0.1, 0.5, 1, 2, 5, 10],
    retention = 30
}

summary = {
    enabled = true,
    quantiles = [0.5, 0.9, 0.95, 0.99],
    retention = 30
}

[metrics_collection.dimensions]
service = true
endpoint = true
method = true
status_code = true
user_id = true
environment = true

[metrics_collection.exporters]
prometheus = {
    enabled = true,
    endpoint = "/metrics",
    port = 9090
}

cloudwatch = {
    enabled = true,
    namespace = "ApplicationMetrics",
    region = "us-east-1"
}

datadog = {
    enabled = true,
    api_key = @env("DATADOG_API_KEY"),
    endpoint = "https://api.datadoghq.com"
}
```

### Metrics Collection Implementation

```php
class MetricsCollector
{
    private $config;
    private $metrics = [];
    private $exporters = [];
    
    public function __construct()
    {
        $this->config = new TuskConfig('metrics-collection.tsk');
        $this->initializeExporters();
    }
    
    private function initializeExporters()
    {
        $exporters = $this->config->get('metrics_collection.exporters');
        
        foreach ($exporters as $name => $config) {
            if ($config['enabled']) {
                $this->exporters[$name] = $this->createExporter($name, $config);
            }
        }
    }
    
    public function record($metricName, $value, $labels = [])
    {
        if (!$this->config->get('metrics_collection.enabled')) {
            return;
        }
        
        // Apply sampling
        if (rand(1, 100) > ($this->config->get('metrics_collection.sampling_rate') * 100)) {
            return;
        }
        
        $metric = [
            'name' => $metricName,
            'value' => $value,
            'labels' => $this->enrichLabels($labels),
            'timestamp' => time()
        ];
        
        $this->metrics[] = $metric;
        
        // Export metrics
        $this->exportMetrics([$metric]);
    }
    
    public function increment($metricName, $labels = [])
    {
        $this->record($metricName, 1, $labels);
    }
    
    public function gauge($metricName, $value, $labels = [])
    {
        $this->record($metricName, $value, $labels);
    }
    
    public function histogram($metricName, $value, $labels = [])
    {
        $histogramConfig = $this->config->get('metrics_collection.types.histogram');
        $buckets = $histogramConfig['buckets'];
        
        foreach ($buckets as $bucket) {
            if ($value <= $bucket) {
                $this->record("{$metricName}_bucket", 1, array_merge($labels, ['le' => $bucket]));
            }
        }
        
        $this->record("{$metricName}_sum", $value, $labels);
        $this->record("{$metricName}_count", 1, $labels);
    }
    
    public function summary($metricName, $value, $labels = [])
    {
        $summaryConfig = $this->config->get('metrics_collection.types.summary');
        $quantiles = $summaryConfig['quantiles'];
        
        foreach ($quantiles as $quantile) {
            $this->record("{$metricName}_quantile", $value, array_merge($labels, ['quantile' => $quantile]));
        }
        
        $this->record("{$metricName}_sum", $value, $labels);
        $this->record("{$metricName}_count", 1, $labels);
    }
    
    private function enrichLabels($labels)
    {
        $dimensions = $this->config->get('metrics_collection.dimensions');
        $enrichedLabels = $labels;
        
        // Add service name
        if ($dimensions['service']) {
            $enrichedLabels['service'] = $this->getServiceName();
        }
        
        // Add environment
        if ($dimensions['environment']) {
            $enrichedLabels['environment'] = $this->getEnvironment();
        }
        
        return $enrichedLabels;
    }
    
    private function exportMetrics($metrics)
    {
        foreach ($this->exporters as $name => $exporter) {
            try {
                $exporter->export($metrics);
            } catch (Exception $e) {
                error_log("Failed to export metrics to {$name}: " . $e->getMessage());
            }
        }
    }
    
    public function getMetrics($metricName, $timeRange = 3600)
    {
        $sql = "
            SELECT 
                name,
                value,
                labels,
                timestamp
            FROM metrics 
            WHERE name = ? AND timestamp > ?
            ORDER BY timestamp DESC
        ";
        
        $result = $this->database->query($sql, [$metricName, time() - $timeRange]);
        return $result->fetchAll();
    }
    
    public function aggregateMetrics($metricName, $aggregation, $timeRange = 3600)
    {
        $sql = "
            SELECT 
                {$aggregation}(value) as aggregated_value,
                COUNT(*) as count
            FROM metrics 
            WHERE name = ? AND timestamp > ?
        ";
        
        $result = $this->database->query($sql, [$metricName, time() - $timeRange]);
        return $result->fetch();
    }
}

class PrometheusExporter
{
    private $config;
    
    public function __construct($config)
    {
        $this->config = $config;
    }
    
    public function export($metrics)
    {
        $prometheusMetrics = [];
        
        foreach ($metrics as $metric) {
            $prometheusMetrics[] = $this->formatPrometheusMetric($metric);
        }
        
        // Store metrics for Prometheus scraping
        $this->storeMetrics($prometheusMetrics);
    }
    
    private function formatPrometheusMetric($metric)
    {
        $labels = [];
        foreach ($metric['labels'] as $key => $value) {
            $labels[] = "{$key}=\"{$value}\"";
        }
        
        $labelString = !empty($labels) ? '{' . implode(',', $labels) . '}' : '';
        
        return "{$metric['name']}{$labelString} {$metric['value']} {$metric['timestamp']}";
    }
}
```

## 🔍 Distributed Tracing

### Tracing Configuration

```ini
# distributed-tracing.tsk
[distributed_tracing]
enabled = true
sampling_rate = 0.1
max_trace_duration = 300

[distributed_tracing.provider]
jaeger = {
    enabled = true,
    endpoint = "http://jaeger:14268/api/traces",
    service_name = "php-application"
}

zipkin = {
    enabled = false,
    endpoint = "http://zipkin:9411/api/v2/spans"
}

[distributed_tracing.spans]
database_queries = true
http_requests = true
cache_operations = true
queue_operations = true
custom_operations = true

[distributed_tracing.tags]
user_id = true
request_id = true
session_id = true
environment = true
version = true
```

### Distributed Tracing Implementation

```php
class DistributedTracer
{
    private $config;
    private $provider;
    private $currentTrace = null;
    
    public function __construct()
    {
        $this->config = new TuskConfig('distributed-tracing.tsk');
        $this->provider = $this->createProvider();
    }
    
    private function createProvider()
    {
        $jaegerConfig = $this->config->get('distributed_tracing.provider.jaeger');
        
        if ($jaegerConfig['enabled']) {
            return new JaegerProvider($jaegerConfig);
        }
        
        $zipkinConfig = $this->config->get('distributed_tracing.provider.zipkin');
        
        if ($zipkinConfig['enabled']) {
            return new ZipkinProvider($zipkinConfig);
        }
        
        return new NoOpProvider();
    }
    
    public function startTrace($operationName, $context = [])
    {
        if (!$this->config->get('distributed_tracing.enabled')) {
            return null;
        }
        
        // Apply sampling
        if (rand(1, 100) > ($this->config->get('distributed_tracing.sampling_rate') * 100)) {
            return null;
        }
        
        $traceId = $context['trace_id'] ?? $this->generateTraceId();
        $spanId = $this->generateSpanId();
        
        $span = [
            'trace_id' => $traceId,
            'span_id' => $spanId,
            'operation_name' => $operationName,
            'start_time' => microtime(true),
            'tags' => $this->enrichTags($context),
            'parent_span_id' => $context['parent_span_id'] ?? null
        ];
        
        $this->currentTrace = $span;
        
        return $span;
    }
    
    public function endSpan($span, $context = [])
    {
        if (!$span) {
            return;
        }
        
        $span['end_time'] = microtime(true);
        $span['duration'] = ($span['end_time'] - $span['start_time']) * 1000000; // microseconds
        
        // Add context to tags
        foreach ($context as $key => $value) {
            $span['tags'][$key] = $value;
        }
        
        // Send span to provider
        $this->provider->sendSpan($span);
    }
    
    public function addSpan($operationName, $context = [])
    {
        if (!$this->currentTrace) {
            return null;
        }
        
        $childSpan = [
            'trace_id' => $this->currentTrace['trace_id'],
            'span_id' => $this->generateSpanId(),
            'operation_name' => $operationName,
            'start_time' => microtime(true),
            'tags' => $this->enrichTags($context),
            'parent_span_id' => $this->currentTrace['span_id']
        ];
        
        return $childSpan;
    }
    
    public function injectTraceContext($carrier)
    {
        if (!$this->currentTrace) {
            return $carrier;
        }
        
        $carrier['X-Trace-ID'] = $this->currentTrace['trace_id'];
        $carrier['X-Span-ID'] = $this->currentTrace['span_id'];
        
        return $carrier;
    }
    
    public function extractTraceContext($carrier)
    {
        $traceId = $carrier['X-Trace-ID'] ?? null;
        $spanId = $carrier['X-Span-ID'] ?? null;
        
        if ($traceId && $spanId) {
            return [
                'trace_id' => $traceId,
                'span_id' => $spanId
            ];
        }
        
        return null;
    }
    
    private function enrichTags($context)
    {
        $tags = $context;
        $tagConfig = $this->config->get('distributed_tracing.tags');
        
        // Add user ID if available
        if ($tagConfig['user_id'] && isset($context['user_id'])) {
            $tags['user.id'] = $context['user_id'];
        }
        
        // Add request ID if available
        if ($tagConfig['request_id'] && isset($context['request_id'])) {
            $tags['request.id'] = $context['request_id'];
        }
        
        // Add environment
        if ($tagConfig['environment']) {
            $tags['environment'] = $this->getEnvironment();
        }
        
        // Add version
        if ($tagConfig['version']) {
            $tags['version'] = $this->getVersion();
        }
        
        return $tags;
    }
    
    private function generateTraceId()
    {
        return bin2hex(random_bytes(16));
    }
    
    private function generateSpanId()
    {
        return bin2hex(random_bytes(8));
    }
}

class JaegerProvider
{
    private $config;
    private $client;
    
    public function __construct($config)
    {
        $this->config = $config;
        $this->client = new GuzzleHttp\Client();
    }
    
    public function sendSpan($span)
    {
        $jaegerSpan = $this->convertToJaegerFormat($span);
        
        $this->client->post($this->config['endpoint'], [
            'json' => [
                'spans' => [$jaegerSpan]
            ]
        ]);
    }
    
    private function convertToJaegerFormat($span)
    {
        return [
            'traceId' => $span['trace_id'],
            'spanId' => $span['span_id'],
            'operationName' => $span['operation_name'],
            'startTime' => $span['start_time'] * 1000000, // Convert to microseconds
            'duration' => $span['duration'],
            'tags' => $this->convertTags($span['tags']),
            'references' => $this->convertReferences($span)
        ];
    }
    
    private function convertTags($tags)
    {
        $jaegerTags = [];
        
        foreach ($tags as $key => $value) {
            $jaegerTags[] = [
                'key' => $key,
                'vType' => 'STRING',
                'vStr' => (string) $value
            ];
        }
        
        return $jaegerTags;
    }
    
    private function convertReferences($span)
    {
        if (!$span['parent_span_id']) {
            return [];
        }
        
        return [
            [
                'refType' => 'CHILD_OF',
                'traceId' => $span['trace_id'],
                'spanId' => $span['parent_span_id']
            ]
        ];
    }
}
```

## 📝 Logging and Log Aggregation

### Logging Configuration

```ini
# logging-aggregation.tsk
[logging_aggregation]
enabled = true
level = "info"
format = "json"

[logging_aggregation.levels]
emergency = 0
alert = 1
critical = 2
error = 3
warning = 4
notice = 5
info = 6
debug = 7

[logging_aggregation.output]
file = {
    enabled = true,
    path = "/var/log/application.log",
    max_size = "100MB",
    max_files = 10
}

elasticsearch = {
    enabled = true,
    endpoint = "http://elasticsearch:9200",
    index = "application-logs"
}

fluentd = {
    enabled = true,
    endpoint = "fluentd:24224",
    tag = "application"
}

[logging_aggregation.fields]
timestamp = true
level = true
message = true
context = true
user_id = true
request_id = true
session_id = true
ip_address = true
user_agent = true
```

### Logging Implementation

```php
class LogAggregator
{
    private $config;
    private $outputs = [];
    private $formatter;
    
    public function __construct()
    {
        $this->config = new TuskConfig('logging-aggregation.tsk');
        $this->formatter = new LogFormatter();
        $this->initializeOutputs();
    }
    
    private function initializeOutputs()
    {
        $outputs = $this->config->get('logging_aggregation.output');
        
        foreach ($outputs as $name => $config) {
            if ($config['enabled']) {
                $this->outputs[$name] = $this->createOutput($name, $config);
            }
        }
    }
    
    public function log($level, $message, $context = [])
    {
        if (!$this->config->get('logging_aggregation.enabled')) {
            return;
        }
        
        // Check log level
        $currentLevel = $this->config->get('logging_aggregation.level');
        $levels = $this->config->get('logging_aggregation.levels');
        
        if ($levels[$level] > $levels[$currentLevel]) {
            return;
        }
        
        $logEntry = $this->createLogEntry($level, $message, $context);
        $formattedEntry = $this->formatter->format($logEntry);
        
        // Send to all outputs
        foreach ($this->outputs as $name => $output) {
            try {
                $output->write($formattedEntry);
            } catch (Exception $e) {
                error_log("Failed to write to {$name}: " . $e->getMessage());
            }
        }
    }
    
    public function emergency($message, $context = [])
    {
        $this->log('emergency', $message, $context);
    }
    
    public function alert($message, $context = [])
    {
        $this->log('alert', $message, $context);
    }
    
    public function critical($message, $context = [])
    {
        $this->log('critical', $message, $context);
    }
    
    public function error($message, $context = [])
    {
        $this->log('error', $message, $context);
    }
    
    public function warning($message, $context = [])
    {
        $this->log('warning', $message, $context);
    }
    
    public function notice($message, $context = [])
    {
        $this->log('notice', $message, $context);
    }
    
    public function info($message, $context = [])
    {
        $this->log('info', $message, $context);
    }
    
    public function debug($message, $context = [])
    {
        $this->log('debug', $message, $context);
    }
    
    private function createLogEntry($level, $message, $context)
    {
        $entry = [
            'timestamp' => date('c'),
            'level' => $level,
            'message' => $message,
            'context' => $context
        ];
        
        // Add standard fields
        $fields = $this->config->get('logging_aggregation.fields');
        
        if ($fields['user_id'] && isset($context['user_id'])) {
            $entry['user_id'] = $context['user_id'];
        }
        
        if ($fields['request_id'] && isset($context['request_id'])) {
            $entry['request_id'] = $context['request_id'];
        }
        
        if ($fields['session_id'] && isset($context['session_id'])) {
            $entry['session_id'] = $context['session_id'];
        }
        
        if ($fields['ip_address'] && isset($context['ip_address'])) {
            $entry['ip_address'] = $context['ip_address'];
        }
        
        if ($fields['user_agent'] && isset($context['user_agent'])) {
            $entry['user_agent'] = $context['user_agent'];
        }
        
        return $entry;
    }
    
    public function searchLogs($query, $filters = [], $timeRange = 3600)
    {
        $elasticsearch = $this->outputs['elasticsearch'] ?? null;
        
        if (!$elasticsearch) {
            throw new Exception("Elasticsearch output not configured");
        }
        
        return $elasticsearch->search($query, $filters, $timeRange);
    }
    
    public function getLogStats($timeRange = 86400)
    {
        $sql = "
            SELECT 
                level,
                COUNT(*) as count,
                MIN(timestamp) as first_log,
                MAX(timestamp) as last_log
            FROM logs 
            WHERE timestamp > ?
            GROUP BY level
            ORDER BY count DESC
        ";
        
        $result = $this->database->query($sql, [date('c', time() - $timeRange)]);
        return $result->fetchAll();
    }
}

class ElasticsearchOutput
{
    private $config;
    private $client;
    
    public function __construct($config)
    {
        $this->config = $config;
        $this->client = new Elasticsearch\Client([
            'hosts' => [$config['endpoint']]
        ]);
    }
    
    public function write($logEntry)
    {
        $this->client->index([
            'index' => $this->config['index'],
            'body' => $logEntry
        ]);
    }
    
    public function search($query, $filters = [], $timeRange = 3600)
    {
        $searchParams = [
            'index' => $this->config['index'],
            'body' => [
                'query' => [
                    'bool' => [
                        'must' => [
                            ['query_string' => ['query' => $query]],
                            ['range' => [
                                'timestamp' => [
                                    'gte' => date('c', time() - $timeRange)
                                ]
                            ]]
                        ]
                    ]
                ],
                'sort' => [
                    'timestamp' => ['order' => 'desc']
                ]
            ]
        ];
        
        // Add filters
        foreach ($filters as $field => $value) {
            $searchParams['body']['query']['bool']['filter'][] = [
                'term' => [$field => $value]
            ];
        }
        
        $response = $this->client->search($searchParams);
        
        return [
            'total' => $response['hits']['total']['value'],
            'logs' => array_map(function($hit) {
                return $hit['_source'];
            }, $response['hits']['hits'])
        ];
    }
}
```

## 🚨 Alerting and Notification

### Alerting Configuration

```ini
# alerting-notification.tsk
[alerting_notification]
enabled = true
escalation = true
snooze = true

[alerting_notification.channels]
slack = {
    enabled = true,
    webhook_url = @env("SLACK_WEBHOOK_URL"),
    channel = "#alerts"
}

email = {
    enabled = true,
    smtp_host = @env("SMTP_HOST"),
    smtp_port = @env("SMTP_PORT"),
    smtp_user = @env("SMTP_USER"),
    smtp_pass = @env("SMTP_PASS")
}

pagerduty = {
    enabled = true,
    api_key = @env("PAGERDUTY_API_KEY"),
    service_id = @env("PAGERDUTY_SERVICE_ID")
}

[alerting_notification.rules]
high_cpu = {
    condition = "cpu_usage > 80",
    duration = 300,
    severity = "warning",
    channels = ["slack", "email"]
}

high_memory = {
    condition = "memory_usage > 90",
    duration = 300,
    severity = "critical",
    channels = ["slack", "email", "pagerduty"]
}

error_rate = {
    condition = "error_rate > 5",
    duration = 600,
    severity = "critical",
    channels = ["slack", "email", "pagerduty"]
}

response_time = {
    condition = "response_time > 2000",
    duration = 300,
    severity = "warning",
    channels = ["slack"]
}
```

### Alerting Implementation

```php
class AlertManager
{
    private $config;
    private $channels = [];
    private $rules = [];
    private $escalation;
    
    public function __construct()
    {
        $this->config = new TuskConfig('alerting-notification.tsk');
        $this->escalation = new EscalationManager();
        $this->initializeChannels();
        $this->initializeRules();
    }
    
    private function initializeChannels()
    {
        $channels = $this->config->get('alerting_notification.channels');
        
        foreach ($channels as $name => $config) {
            if ($config['enabled']) {
                $this->channels[$name] = $this->createChannel($name, $config);
            }
        }
    }
    
    private function initializeRules()
    {
        $rules = $this->config->get('alerting_notification.rules');
        
        foreach ($rules as $name => $config) {
            $this->rules[$name] = $config;
        }
    }
    
    public function checkAlerts($metrics)
    {
        if (!$this->config->get('alerting_notification.enabled')) {
            return;
        }
        
        foreach ($this->rules as $ruleName => $rule) {
            $triggered = $this->evaluateRule($rule, $metrics);
            
            if ($triggered) {
                $this->triggerAlert($ruleName, $rule, $metrics);
            }
        }
    }
    
    private function evaluateRule($rule, $metrics)
    {
        $condition = $rule['condition'];
        $duration = $rule['duration'];
        
        // Parse condition
        $parsedCondition = $this->parseCondition($condition);
        
        // Check if condition is met for the required duration
        return $this->checkConditionDuration($parsedCondition, $duration);
    }
    
    private function parseCondition($condition)
    {
        // Simple condition parser for metrics > value format
        if (preg_match('/(\w+)\s*([><=]+)\s*(\d+)/', $condition, $matches)) {
            return [
                'metric' => $matches[1],
                'operator' => $matches[2],
                'value' => (float) $matches[3]
            ];
        }
        
        throw new InvalidArgumentException("Invalid condition format: {$condition}");
    }
    
    private function checkConditionDuration($condition, $duration)
    {
        $sql = "
            SELECT COUNT(*) as count
            FROM metrics 
            WHERE name = ? 
            AND timestamp > ?
            AND value {$condition['operator']} ?
        ";
        
        $result = $this->database->query($sql, [
            $condition['metric'],
            time() - $duration,
            $condition['value']
        ]);
        
        $row = $result->fetch();
        
        // Check if condition was met for the entire duration
        $expectedCount = $duration / 60; // Assuming metrics are collected every minute
        return $row['count'] >= $expectedCount * 0.8; // 80% threshold
    }
    
    private function triggerAlert($ruleName, $rule, $metrics)
    {
        $alert = [
            'id' => uniqid(),
            'rule' => $ruleName,
            'severity' => $rule['severity'],
            'message' => $this->generateAlertMessage($ruleName, $rule, $metrics),
            'timestamp' => time(),
            'metrics' => $metrics
        ];
        
        // Store alert
        $this->storeAlert($alert);
        
        // Send notifications
        $this->sendNotifications($alert, $rule['channels']);
        
        // Handle escalation
        if ($this->config->get('alerting_notification.escalation')) {
            $this->escalation->handleEscalation($alert);
        }
    }
    
    private function sendNotifications($alert, $channels)
    {
        foreach ($channels as $channelName) {
            $channel = $this->channels[$channelName] ?? null;
            
            if ($channel) {
                try {
                    $channel->send($alert);
                } catch (Exception $e) {
                    error_log("Failed to send alert to {$channelName}: " . $e->getMessage());
                }
            }
        }
    }
    
    private function generateAlertMessage($ruleName, $rule, $metrics)
    {
        $message = "Alert: {$ruleName}\n";
        $message .= "Severity: {$rule['severity']}\n";
        $message .= "Condition: {$rule['condition']}\n";
        $message .= "Current metrics:\n";
        
        foreach ($metrics as $metric => $value) {
            $message .= "  {$metric}: {$value}\n";
        }
        
        return $message;
    }
    
    public function snoozeAlert($alertId, $duration = 3600)
    {
        if (!$this->config->get('alerting_notification.snooze')) {
            throw new Exception("Snooze not enabled");
        }
        
        $sql = "
            UPDATE alerts 
            SET snoozed_until = ? 
            WHERE id = ?
        ";
        
        $this->database->execute($sql, [time() + $duration, $alertId]);
    }
    
    public function acknowledgeAlert($alertId, $userId)
    {
        $sql = "
            UPDATE alerts 
            SET acknowledged_by = ?, acknowledged_at = ? 
            WHERE id = ?
        ";
        
        $this->database->execute($sql, [$userId, time(), $alertId]);
    }
}

class SlackChannel
{
    private $config;
    private $client;
    
    public function __construct($config)
    {
        $this->config = $config;
        $this->client = new GuzzleHttp\Client();
    }
    
    public function send($alert)
    {
        $message = $this->formatSlackMessage($alert);
        
        $this->client->post($this->config['webhook_url'], [
            'json' => $message
        ]);
    }
    
    private function formatSlackMessage($alert)
    {
        $color = $this->getSeverityColor($alert['severity']);
        
        return [
            'channel' => $this->config['channel'],
            'attachments' => [
                [
                    'color' => $color,
                    'title' => "Alert: {$alert['rule']}",
                    'text' => $alert['message'],
                    'fields' => [
                        [
                            'title' => 'Severity',
                            'value' => $alert['severity'],
                            'short' => true
                        ],
                        [
                            'title' => 'Time',
                            'value' => date('c', $alert['timestamp']),
                            'short' => true
                        ]
                    ]
                ]
            ]
        ];
    }
    
    private function getSeverityColor($severity)
    {
        switch ($severity) {
            case 'critical':
                return '#ff0000';
            case 'warning':
                return '#ffa500';
            case 'info':
                return '#0000ff';
            default:
                return '#808080';
        }
    }
}
```

## 📋 Best Practices

### Monitoring Best Practices

1. **Full Stack Observability**: Monitor all layers of the application
2. **Proactive Monitoring**: Detect issues before they impact users
3. **Distributed Tracing**: Track requests across service boundaries
4. **Structured Logging**: Use structured logs for better analysis
5. **Alert Fatigue**: Avoid too many alerts, focus on actionable ones
6. **Metrics Retention**: Balance retention with storage costs
7. **Dashboard Design**: Create meaningful dashboards
8. **Incident Response**: Plan and practice incident response

### Integration Examples

```php
// Monitoring Integration
class MonitoringIntegration
{
    private $monitor;
    private $metrics;
    private $tracer;
    private $logger;
    private $alerts;
    
    public function __construct()
    {
        $this->monitor = new MonitoringManager();
        $this->metrics = new MetricsCollector();
        $this->tracer = new DistributedTracer();
        $this->logger = new LogAggregator();
        $this->alerts = new AlertManager();
    }
    
    public function monitorApplication($request, $context)
    {
        // Monitor request
        $response = $this->monitor->monitorRequest($request, $context);
        
        // Check system health
        $health = $this->monitor->getSystemHealth();
        
        // Check alerts
        $this->alerts->checkAlerts($health);
        
        return $response;
    }
}
```

## 🔧 Troubleshooting

### Common Issues

1. **High Cardinality**: Limit metric dimensions
2. **Storage Costs**: Optimize retention policies
3. **Performance Impact**: Sample metrics and traces
4. **Alert Noise**: Tune alert thresholds
5. **Data Loss**: Ensure reliable delivery

### Debug Configuration

```ini
# debug-monitoring.tsk
[debug]
enabled = true
log_level = "verbose"
trace_monitoring = true

[debug.output]
console = true
file = "/var/log/tusk-monitoring-debug.log"
```

This comprehensive monitoring system leverages TuskLang's configuration-driven approach to create intelligent, adaptive monitoring solutions that provide full observability across all application layers. 