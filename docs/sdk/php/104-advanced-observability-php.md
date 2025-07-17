# 🔍 Advanced Observability with TuskLang & PHP

## Introduction
Observability is the key to understanding and maintaining complex systems. TuskLang and PHP let you implement sophisticated observability with config-driven logging, metrics collection, distributed tracing, intelligent alerting, and comprehensive dashboards that provide complete visibility into your applications.

## Key Features
- **Structured logging and log aggregation**
- **Metrics collection and monitoring**
- **Distributed tracing and correlation**
- **Intelligent alerting and notification**
- **Real-time dashboards**
- **Performance analysis**

## Example: Observability Configuration
```ini
[observability]
logging: @go("observability.ConfigureLogging")
metrics: @go("observability.ConfigureMetrics")
tracing: @go("observability.ConfigureTracing")
alerting: @go("observability.ConfigureAlerting")
dashboards: @go("observability.ConfigureDashboards")
```

## PHP: Observability Manager Implementation
```php
<?php

namespace App\Observability;

use TuskLang\Config;
use TuskLang\Operators\Env;
use TuskLang\Operators\Metrics;
use TuskLang\Operators\Go;

class ObservabilityManager
{
    private $config;
    private $logger;
    private $metrics;
    private $tracer;
    private $alerter;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->logger = new StructuredLogger();
        $this->metrics = new MetricsCollector();
        $this->tracer = new DistributedTracer();
        $this->alerter = new AlertManager();
    }
    
    public function observe($context, $operation, $data = [])
    {
        $startTime = microtime(true);
        $traceId = $this->tracer->startTrace($operation);
        
        try {
            // Log operation start
            $this->logger->info("Operation started", [
                'operation' => $operation,
                'context' => $context,
                'trace_id' => $traceId,
                'data' => $data
            ]);
            
            // Record operation metrics
            $this->metrics->increment("operations_total", [
                'operation' => $operation,
                'status' => 'started'
            ]);
            
            // Execute operation
            $result = $this->executeOperation($operation, $data);
            
            // Record success
            $this->recordSuccess($operation, $traceId, $result);
            
            return $result;
            
        } catch (\Exception $e) {
            // Record failure
            $this->recordFailure($operation, $traceId, $e);
            throw $e;
        } finally {
            $duration = (microtime(true) - $startTime) * 1000;
            
            // Record duration
            $this->metrics->histogram("operation_duration", $duration, [
                'operation' => $operation
            ]);
            
            // End trace
            $this->tracer->endTrace($traceId);
        }
    }
    
    public function log($level, $message, $context = [])
    {
        $this->logger->log($level, $message, $context);
    }
    
    public function recordMetric($name, $value, $labels = [])
    {
        $this->metrics->record($name, $value, $labels);
    }
    
    public function startSpan($operation, $attributes = [])
    {
        return $this->tracer->startSpan($operation, $attributes);
    }
    
    public function endSpan($spanId, $attributes = [])
    {
        $this->tracer->endSpan($spanId, $attributes);
    }
    
    public function alert($alert, $data = [])
    {
        $this->alerter->send($alert, $data);
    }
    
    private function executeOperation($operation, $data)
    {
        // This would execute the actual operation
        // For now, just return a mock result
        return ['status' => 'success', 'data' => $data];
    }
    
    private function recordSuccess($operation, $traceId, $result)
    {
        $this->logger->info("Operation completed", [
            'operation' => $operation,
            'trace_id' => $traceId,
            'result' => $result
        ]);
        
        $this->metrics->increment("operations_total", [
            'operation' => $operation,
            'status' => 'success'
        ]);
    }
    
    private function recordFailure($operation, $traceId, $exception)
    {
        $this->logger->error("Operation failed", [
            'operation' => $operation,
            'trace_id' => $traceId,
            'error' => $exception->getMessage(),
            'stack_trace' => $exception->getTraceAsString()
        ]);
        
        $this->metrics->increment("operations_total", [
            'operation' => $operation,
            'status' => 'failed'
        ]);
        
        $this->metrics->increment("errors_total", [
            'operation' => $operation,
            'error_type' => get_class($exception)
        ]);
    }
}

class StructuredLogger
{
    private $config;
    private $handlers = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->loadHandlers();
    }
    
    public function log($level, $message, $context = [])
    {
        $logEntry = $this->createLogEntry($level, $message, $context);
        
        foreach ($this->handlers as $handler) {
            $handler->handle($logEntry);
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
        return [
            'timestamp' => date('c'),
            'level' => $level,
            'message' => $message,
            'context' => $context,
            'trace_id' => $this->getCurrentTraceId(),
            'span_id' => $this->getCurrentSpanId(),
            'host' => gethostname(),
            'pid' => getmypid(),
            'memory_usage' => memory_get_usage(true),
            'peak_memory' => memory_get_peak_usage(true)
        ];
    }
    
    private function loadHandlers()
    {
        $handlers = $this->config->get('observability.logging.handlers', []);
        
        foreach ($handlers as $handler) {
            $this->handlers[] = new $handler['class']($handler['config']);
        }
    }
    
    private function getCurrentTraceId()
    {
        // Get from current trace context
        return null; // Implementation depends on trace context
    }
    
    private function getCurrentSpanId()
    {
        // Get from current trace context
        return null; // Implementation depends on trace context
    }
}

class ElasticsearchLogHandler
{
    private $config;
    private $client;
    
    public function __construct($config)
    {
        $this->config = $config;
        $this->client = new ElasticsearchClient($config);
    }
    
    public function handle($logEntry)
    {
        $index = $this->getIndexName();
        
        $this->client->index([
            'index' => $index,
            'body' => $logEntry
        ]);
    }
    
    private function getIndexName()
    {
        $prefix = $this->config->get('index_prefix', 'logs');
        $date = date('Y.m.d');
        
        return "{$prefix}-{$date}";
    }
}

class MetricsCollector
{
    private $config;
    private $storage;
    private $counters = [];
    private $gauges = [];
    private $histograms = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->storage = $this->getStorage();
    }
    
    public function increment($name, $labels = [])
    {
        $key = $this->getMetricKey($name, $labels);
        
        if (!isset($this->counters[$key])) {
            $this->counters[$key] = 0;
        }
        
        $this->counters[$key]++;
        
        $this->storage->increment($name, $labels);
    }
    
    public function gauge($name, $value, $labels = [])
    {
        $key = $this->getMetricKey($name, $labels);
        $this->gauges[$key] = $value;
        
        $this->storage->gauge($name, $value, $labels);
    }
    
    public function histogram($name, $value, $labels = [])
    {
        $key = $this->getMetricKey($name, $labels);
        
        if (!isset($this->histograms[$key])) {
            $this->histograms[$key] = [];
        }
        
        $this->histograms[$key][] = $value;
        
        $this->storage->histogram($name, $value, $labels);
    }
    
    public function record($name, $value, $labels = [])
    {
        $this->storage->record($name, $value, $labels);
    }
    
    public function getMetrics($names = [], $timeRange = '1h')
    {
        return $this->storage->query($names, $timeRange);
    }
    
    private function getMetricKey($name, $labels)
    {
        $labelString = '';
        
        foreach ($labels as $key => $value) {
            $labelString .= "{$key}={$value},";
        }
        
        return "{$name}:{$labelString}";
    }
    
    private function getStorage()
    {
        $type = $this->config->get('observability.metrics.storage', 'prometheus');
        
        switch ($type) {
            case 'prometheus':
                return new PrometheusStorage($this->config);
            case 'influxdb':
                return new InfluxDBStorage($this->config);
            case 'statsd':
                return new StatsDStorage($this->config);
            default:
                throw new \Exception("Unknown metrics storage: {$type}");
        }
    }
}

class PrometheusStorage
{
    private $config;
    private $metrics = [];
    
    public function __construct($config)
    {
        $this->config = $config;
    }
    
    public function increment($name, $labels)
    {
        $this->recordMetric($name, 1, $labels, 'counter');
    }
    
    public function gauge($name, $value, $labels)
    {
        $this->recordMetric($name, $value, $labels, 'gauge');
    }
    
    public function histogram($name, $value, $labels)
    {
        $this->recordMetric($name, $value, $labels, 'histogram');
    }
    
    public function record($name, $value, $labels)
    {
        $this->recordMetric($name, $value, $labels, 'gauge');
    }
    
    public function query($names, $timeRange)
    {
        // Implementation to query Prometheus
        return [];
    }
    
    private function recordMetric($name, $value, $labels, $type)
    {
        $metric = [
            'name' => $name,
            'value' => $value,
            'labels' => $labels,
            'type' => $type,
            'timestamp' => time()
        ];
        
        $this->metrics[] = $metric;
        
        // Export to Prometheus format
        $this->exportToPrometheus($metric);
    }
    
    private function exportToPrometheus($metric)
    {
        $labels = '';
        
        foreach ($metric['labels'] as $key => $value) {
            $labels .= "{$key}=\"{$value}\",";
        }
        
        if ($labels) {
            $labels = '{' . rtrim($labels, ',') . '}';
        }
        
        $prometheusLine = "{$metric['name']}{$labels} {$metric['value']} {$metric['timestamp']}";
        
        // Write to Prometheus file
        $file = $this->config->get('prometheus_file', '/tmp/metrics.prom');
        file_put_contents($file, $prometheusLine . "\n", FILE_APPEND | LOCK_EX);
    }
}

class DistributedTracer
{
    private $config;
    private $currentTrace;
    private $spans = [];
    
    public function __construct()
    {
        $this->config = new Config();
    }
    
    public function startTrace($operation)
    {
        $traceId = $this->generateTraceId();
        
        $this->currentTrace = [
            'trace_id' => $traceId,
            'operation' => $operation,
            'start_time' => microtime(true),
            'spans' => []
        ];
        
        return $traceId;
    }
    
    public function startSpan($operation, $attributes = [])
    {
        $spanId = $this->generateSpanId();
        
        $span = [
            'span_id' => $spanId,
            'trace_id' => $this->currentTrace['trace_id'],
            'operation' => $operation,
            'start_time' => microtime(true),
            'attributes' => $attributes
        ];
        
        $this->spans[$spanId] = $span;
        
        return $spanId;
    }
    
    public function endSpan($spanId, $attributes = [])
    {
        if (!isset($this->spans[$spanId])) {
            return;
        }
        
        $span = $this->spans[$spanId];
        $span['end_time'] = microtime(true);
        $span['duration'] = ($span['end_time'] - $span['start_time']) * 1000;
        $span['attributes'] = array_merge($span['attributes'], $attributes);
        
        $this->currentTrace['spans'][] = $span;
        
        unset($this->spans[$spanId]);
    }
    
    public function endTrace($traceId)
    {
        if ($this->currentTrace && $this->currentTrace['trace_id'] === $traceId) {
            $this->currentTrace['end_time'] = microtime(true);
            $this->currentTrace['duration'] = ($this->currentTrace['end_time'] - $this->currentTrace['start_time']) * 1000;
            
            // Send trace to storage
            $this->sendTrace($this->currentTrace);
            
            // Clear current trace
            $this->currentTrace = null;
            $this->spans = [];
        }
    }
    
    public function addAttribute($key, $value)
    {
        if ($this->currentTrace) {
            $this->currentTrace['attributes'][$key] = $value;
        }
    }
    
    private function generateTraceId()
    {
        return bin2hex(random_bytes(16));
    }
    
    private function generateSpanId()
    {
        return bin2hex(random_bytes(8));
    }
    
    private function sendTrace($trace)
    {
        $storage = $this->getTraceStorage();
        $storage->store($trace);
    }
    
    private function getTraceStorage()
    {
        $type = $this->config->get('observability.tracing.storage', 'jaeger');
        
        switch ($type) {
            case 'jaeger':
                return new JaegerStorage($this->config);
            case 'zipkin':
                return new ZipkinStorage($this->config);
            case 'elastic':
                return new ElasticStorage($this->config);
            default:
                throw new \Exception("Unknown trace storage: {$type}");
        }
    }
}

class AlertManager
{
    private $config;
    private $rules = [];
    private $notifiers = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->loadRules();
        $this->loadNotifiers();
    }
    
    public function send($alert, $data = [])
    {
        // Check if alert should be triggered
        if ($this->shouldTriggerAlert($alert, $data)) {
            $this->triggerAlert($alert, $data);
        }
    }
    
    public function addRule($rule)
    {
        $this->rules[] = $rule;
    }
    
    public function addNotifier($notifier)
    {
        $this->notifiers[] = $notifier;
    }
    
    private function loadRules()
    {
        $rules = $this->config->get('observability.alerting.rules', []);
        
        foreach ($rules as $rule) {
            $this->rules[] = new AlertRule($rule);
        }
    }
    
    private function loadNotifiers()
    {
        $notifiers = $this->config->get('observability.alerting.notifiers', []);
        
        foreach ($notifiers as $notifier) {
            $this->notifiers[] = new $notifier['class']($notifier['config']);
        }
    }
    
    private function shouldTriggerAlert($alert, $data)
    {
        foreach ($this->rules as $rule) {
            if ($rule->matches($alert, $data)) {
                return true;
            }
        }
        
        return false;
    }
    
    private function triggerAlert($alert, $data)
    {
        $alertData = [
            'alert' => $alert,
            'data' => $data,
            'timestamp' => date('c'),
            'severity' => $alert['severity'] ?? 'info'
        ];
        
        foreach ($this->notifiers as $notifier) {
            $notifier->notify($alertData);
        }
    }
}

class AlertRule
{
    private $config;
    
    public function __construct($config)
    {
        $this->config = $config;
    }
    
    public function matches($alert, $data)
    {
        $conditions = $this->config['conditions'] ?? [];
        
        foreach ($conditions as $condition) {
            if (!$this->evaluateCondition($condition, $alert, $data)) {
                return false;
            }
        }
        
        return true;
    }
    
    private function evaluateCondition($condition, $alert, $data)
    {
        switch ($condition['type']) {
            case 'threshold':
                return $this->evaluateThreshold($condition, $data);
            case 'pattern':
                return $this->evaluatePattern($condition, $alert);
            case 'frequency':
                return $this->evaluateFrequency($condition, $alert);
            default:
                return true;
        }
    }
    
    private function evaluateThreshold($condition, $data)
    {
        $value = $data[$condition['field']] ?? 0;
        $threshold = $condition['threshold'];
        $operator = $condition['operator'] ?? 'gt';
        
        switch ($operator) {
            case 'gt':
                return $value > $threshold;
            case 'lt':
                return $value < $threshold;
            case 'eq':
                return $value == $threshold;
            default:
                return false;
        }
    }
    
    private function evaluatePattern($condition, $alert)
    {
        $pattern = $condition['pattern'];
        $field = $condition['field'];
        $value = $alert[$field] ?? '';
        
        return preg_match($pattern, $value);
    }
    
    private function evaluateFrequency($condition, $alert)
    {
        $timeWindow = $condition['time_window'] ?? 300; // 5 minutes
        $maxOccurrences = $condition['max_occurrences'] ?? 1;
        
        $occurrences = $this->getAlertOccurrences($alert['name'], $timeWindow);
        
        return $occurrences >= $maxOccurrences;
    }
    
    private function getAlertOccurrences($alertName, $timeWindow)
    {
        // Implementation to get alert occurrences in time window
        return 0; // Placeholder
    }
}

class EmailNotifier
{
    private $config;
    private $mailer;
    
    public function __construct($config)
    {
        $this->config = $config;
        $this->mailer = new Mailer($config);
    }
    
    public function notify($alertData)
    {
        $recipients = $this->config['recipients'] ?? [];
        $subject = $this->formatSubject($alertData);
        $body = $this->formatBody($alertData);
        
        foreach ($recipients as $recipient) {
            $this->mailer->send($recipient, $subject, $body);
        }
    }
    
    private function formatSubject($alertData)
    {
        $severity = strtoupper($alertData['severity']);
        $alertName = $alertData['alert']['name'];
        
        return "[{$severity}] {$alertName}";
    }
    
    private function formatBody($alertData)
    {
        $body = "Alert: {$alertData['alert']['name']}\n";
        $body .= "Severity: {$alertData['severity']}\n";
        $body .= "Time: {$alertData['timestamp']}\n";
        $body .= "Message: {$alertData['alert']['message']}\n\n";
        
        if (!empty($alertData['data'])) {
            $body .= "Data:\n";
            $body .= json_encode($alertData['data'], JSON_PRETTY_PRINT);
        }
        
        return $body;
    }
}

class SlackNotifier
{
    private $config;
    private $client;
    
    public function __construct($config)
    {
        $this->config = $config;
        $this->client = new HttpClient();
    }
    
    public function notify($alertData)
    {
        $webhookUrl = $this->config['webhook_url'];
        $channel = $this->config['channel'] ?? '#alerts';
        
        $message = $this->formatMessage($alertData);
        
        $payload = [
            'channel' => $channel,
            'text' => $message,
            'attachments' => [
                [
                    'color' => $this->getColorForSeverity($alertData['severity']),
                    'fields' => $this->formatFields($alertData)
                ]
            ]
        ];
        
        $this->client->post($webhookUrl, $payload, [
            'Content-Type' => 'application/json'
        ]);
    }
    
    private function formatMessage($alertData)
    {
        $severity = strtoupper($alertData['severity']);
        $alertName = $alertData['alert']['name'];
        $message = $alertData['alert']['message'];
        
        return "*[{$severity}] {$alertName}*\n{$message}";
    }
    
    private function getColorForSeverity($severity)
    {
        $colors = [
            'critical' => '#ff0000',
            'error' => '#ff6600',
            'warning' => '#ffcc00',
            'info' => '#00ccff'
        ];
        
        return $colors[$severity] ?? '#cccccc';
    }
    
    private function formatFields($alertData)
    {
        $fields = [
            [
                'title' => 'Severity',
                'value' => $alertData['severity'],
                'short' => true
            ],
            [
                'title' => 'Time',
                'value' => $alertData['timestamp'],
                'short' => true
            ]
        ];
        
        if (!empty($alertData['data'])) {
            $fields[] = [
                'title' => 'Data',
                'value' => json_encode($alertData['data']),
                'short' => false
            ];
        }
        
        return $fields;
    }
}
```

## Best Practices
- **Use structured logging with consistent formats**
- **Collect comprehensive metrics**
- **Implement distributed tracing**
- **Set up intelligent alerting**
- **Create meaningful dashboards**
- **Monitor application performance**

## Performance Optimization
- **Use efficient log storage**
- **Implement metrics sampling**
- **Optimize trace collection**
- **Use caching for dashboards**

## Security Considerations
- **Sanitize log data**
- **Secure monitoring endpoints**
- **Implement access controls**
- **Monitor security events**

## Troubleshooting
- **Check log aggregation**
- **Verify metrics collection**
- **Monitor trace sampling**
- **Test alert delivery**

## Conclusion
TuskLang + PHP = observability that provides complete visibility into your applications. See everything, know everything, act on everything. 