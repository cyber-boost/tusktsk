# Advanced Monitoring and Observability

TuskLang enables PHP developers to implement sophisticated monitoring and observability patterns with confidence. This guide covers advanced monitoring strategies, observability tools, and performance tracking.

## Table of Contents
- [Monitoring Configuration](#monitoring-configuration)
- [Metrics Collection](#metrics-collection)
- [Logging Strategies](#logging-strategies)
- [Distributed Tracing](#distributed-tracing)
- [Alerting Systems](#alerting-systems)
- [Best Practices](#best-practices)

## Monitoring Configuration

```php
// config/monitoring.tsk
monitoring = {
    metrics = {
        provider = "prometheus"
        endpoint = "/metrics"
        collection_interval = "15s"
        
        types = {
            counters = true
            gauges = true
            histograms = true
            summaries = true
        }
        
        labels = {
            application = "@env('APP_NAME')"
            environment = "@env('APP_ENV')"
            version = "@env('APP_VERSION')"
            instance = "@env('HOSTNAME')"
        }
    }
    
    logging = {
        provider = "elasticsearch"
        level = "@env('LOG_LEVEL', 'info')"
        format = "json"
        structured = true
        
        outputs = {
            file = {
                enabled = true
                path = "/var/log/app/app.log"
                max_size = "100MB"
                max_files = 10
            }
            elasticsearch = {
                enabled = true
                hosts = ["@env('ELASTICSEARCH_HOST')"]
                index = "app-logs"
                retention = "30d"
            }
            stdout = {
                enabled = "@env('LOG_STDOUT', 'false')"
            }
        }
        
        correlation = {
            enabled = true
            header = "X-Correlation-ID"
            generate_if_missing = true
        }
    }
    
    tracing = {
        provider = "jaeger"
        enabled = true
        sampling_rate = 0.1
        propagation = true
        
        spans = {
            database = true
            http = true
            queue = true
            cache = true
        }
    }
    
    alerting = {
        provider = "alertmanager"
        enabled = true
        
        rules = [
            {
                name = "high_error_rate"
                condition = "error_rate > 5%"
                duration = "5m"
                severity = "critical"
                notification = "slack"
            }
            {
                name = "high_response_time"
                condition = "response_time_p95 > 2s"
                duration = "5m"
                severity = "warning"
                notification = "email"
            }
            {
                name = "high_memory_usage"
                condition = "memory_usage > 80%"
                duration = "10m"
                severity = "warning"
                notification = "pagerduty"
            }
        ]
        
        notifications = {
            slack = {
                webhook = "@env('SLACK_WEBHOOK')"
                channel = "#alerts"
            }
            email = {
                smtp = "@env('SMTP_HOST')"
                from = "@env('ALERT_EMAIL_FROM')"
                to = "@env('ALERT_EMAIL_TO')"
            }
            pagerduty = {
                api_key = "@env('PAGERDUTY_API_KEY')"
                service_id = "@env('PAGERDUTY_SERVICE_ID')"
            }
        }
    }
    
    health_checks = {
        enabled = true
        endpoint = "/health"
        
        checks = [
            {
                name = "database"
                type = "database"
                timeout = 5
            }
            {
                name = "cache"
                type = "redis"
                timeout = 3
            }
            {
                name = "queue"
                type = "queue"
                timeout = 5
            }
            {
                name = "external_api"
                type = "http"
                url = "@env('EXTERNAL_API_HEALTH_URL')"
                timeout = 10
            }
        ]
    }
}
```

## Metrics Collection

```php
<?php
// app/Infrastructure/Monitoring/MetricsCollector.php

namespace App\Infrastructure\Monitoring;

use TuskLang\Metrics\MetricsCollectorInterface;
use TuskLang\Metrics\Counter;
use TuskLang\Metrics\Gauge;
use TuskLang\Metrics\Histogram;
use TuskLang\Metrics\Summary;

class MetricsCollector implements MetricsCollectorInterface
{
    private array $counters = [];
    private array $gauges = [];
    private array $histograms = [];
    private array $summaries = [];
    private array $config;
    
    public function __construct(array $config)
    {
        $this->config = $config;
        $this->initializeMetrics();
    }
    
    public function incrementCounter(string $name, array $labels = [], int $value = 1): void
    {
        $key = $this->getMetricKey($name, $labels);
        
        if (!isset($this->counters[$key])) {
            $this->counters[$key] = new Counter($name, $labels);
        }
        
        $this->counters[$key]->increment($value);
    }
    
    public function setGauge(string $name, float $value, array $labels = []): void
    {
        $key = $this->getMetricKey($name, $labels);
        
        if (!isset($this->gauges[$key])) {
            $this->gauges[$key] = new Gauge($name, $labels);
        }
        
        $this->gauges[$key]->set($value);
    }
    
    public function observeHistogram(string $name, float $value, array $labels = []): void
    {
        $key = $this->getMetricKey($name, $labels);
        
        if (!isset($this->histograms[$key])) {
            $this->histograms[$key] = new Histogram($name, $labels);
        }
        
        $this->histograms[$key]->observe($value);
    }
    
    public function observeSummary(string $name, float $value, array $labels = []): void
    {
        $key = $this->getMetricKey($name, $labels);
        
        if (!isset($this->summaries[$key])) {
            $this->summaries[$key] = new Summary($name, $labels);
        }
        
        $this->summaries[$key]->observe($value);
    }
    
    public function recordResponseTime(string $endpoint, float $duration): void
    {
        $this->observeHistogram('http_request_duration_seconds', $duration, [
            'endpoint' => $endpoint,
            'method' => $_SERVER['REQUEST_METHOD'] ?? 'unknown'
        ]);
    }
    
    public function recordDatabaseQuery(string $query, float $duration): void
    {
        $this->observeHistogram('database_query_duration_seconds', $duration, [
            'query' => $this->normalizeQuery($query)
        ]);
    }
    
    public function recordCacheOperation(string $operation, string $key, float $duration): void
    {
        $this->observeHistogram('cache_operation_duration_seconds', $duration, [
            'operation' => $operation,
            'key' => $this->normalizeKey($key)
        ]);
    }
    
    public function recordQueueOperation(string $operation, string $queue, float $duration): void
    {
        $this->observeHistogram('queue_operation_duration_seconds', $duration, [
            'operation' => $operation,
            'queue' => $queue
        ]);
    }
    
    public function recordMemoryUsage(): void
    {
        $memoryUsage = memory_get_usage(true);
        $memoryPeak = memory_get_peak_usage(true);
        
        $this->setGauge('memory_usage_bytes', $memoryUsage);
        $this->setGauge('memory_peak_usage_bytes', $memoryPeak);
    }
    
    public function recordError(string $type, string $message): void
    {
        $this->incrementCounter('errors_total', [
            'type' => $type,
            'message' => $this->normalizeMessage($message)
        ]);
    }
    
    public function getMetrics(): array
    {
        return [
            'counters' => array_map(fn($counter) => $counter->toArray(), $this->counters),
            'gauges' => array_map(fn($gauge) => $gauge->toArray(), $this->gauges),
            'histograms' => array_map(fn($histogram) => $histogram->toArray(), $this->histograms),
            'summaries' => array_map(fn($summary) => $summary->toArray(), $this->summaries)
        ];
    }
    
    public function exportPrometheus(): string
    {
        $output = "# HELP Application metrics\n";
        $output .= "# TYPE application_metrics counter\n\n";
        
        foreach ($this->counters as $counter) {
            $output .= $counter->toPrometheus() . "\n";
        }
        
        foreach ($this->gauges as $gauge) {
            $output .= $gauge->toPrometheus() . "\n";
        }
        
        foreach ($this->histograms as $histogram) {
            $output .= $histogram->toPrometheus() . "\n";
        }
        
        foreach ($this->summaries as $summary) {
            $output .= $summary->toPrometheus() . "\n";
        }
        
        return $output;
    }
    
    private function initializeMetrics(): void
    {
        // Initialize default metrics
        $this->setGauge('application_start_time', microtime(true));
        $this->setGauge('application_version', 1.0, ['version' => $this->config['labels']['version']]);
    }
    
    private function getMetricKey(string $name, array $labels): string
    {
        ksort($labels);
        return $name . ':' . md5(serialize($labels));
    }
    
    private function normalizeQuery(string $query): string
    {
        // Normalize SQL queries for grouping
        $normalized = preg_replace('/\s+/', ' ', trim($query));
        $normalized = preg_replace('/\d+/', 'N', $normalized);
        return substr($normalized, 0, 100);
    }
    
    private function normalizeKey(string $key): string
    {
        // Normalize cache keys for grouping
        return substr($key, 0, 50);
    }
    
    private function normalizeMessage(string $message): string
    {
        // Normalize error messages for grouping
        return substr($message, 0, 100);
    }
}

// app/Infrastructure/Monitoring/PerformanceMonitor.php

namespace App\Infrastructure\Monitoring;

class PerformanceMonitor
{
    private MetricsCollector $metrics;
    private array $timers = [];
    
    public function __construct(MetricsCollector $metrics)
    {
        $this->metrics = $metrics;
    }
    
    public function startTimer(string $name): void
    {
        $this->timers[$name] = microtime(true);
    }
    
    public function stopTimer(string $name, array $labels = []): float
    {
        if (!isset($this->timers[$name])) {
            throw new \RuntimeException("Timer '{$name}' not started");
        }
        
        $duration = microtime(true) - $this->timers[$name];
        unset($this->timers[$name]);
        
        $this->metrics->observeHistogram($name . '_duration_seconds', $duration, $labels);
        
        return $duration;
    }
    
    public function measureDatabaseQuery(callable $query, array $labels = []): mixed
    {
        $this->startTimer('database_query');
        
        try {
            $result = $query();
            $this->stopTimer('database_query', $labels);
            return $result;
        } catch (\Exception $e) {
            $this->stopTimer('database_query', $labels);
            throw $e;
        }
    }
    
    public function measureCacheOperation(callable $operation, array $labels = []): mixed
    {
        $this->startTimer('cache_operation');
        
        try {
            $result = $operation();
            $this->stopTimer('cache_operation', $labels);
            return $result;
        } catch (\Exception $e) {
            $this->stopTimer('cache_operation', $labels);
            throw $e;
        }
    }
    
    public function measureHttpRequest(callable $request, array $labels = []): mixed
    {
        $this->startTimer('http_request');
        
        try {
            $result = $request();
            $this->stopTimer('http_request', $labels);
            return $result;
        } catch (\Exception $e) {
            $this->stopTimer('http_request', $labels);
            throw $e;
        }
    }
}
```

## Logging Strategies

```php
<?php
// app/Infrastructure/Logging/StructuredLogger.php

namespace App\Infrastructure\Logging;

use TuskLang\Logging\LoggerInterface;
use TuskLang\Logging\LogLevel;

class StructuredLogger implements LoggerInterface
{
    private array $config;
    private string $correlationId;
    private array $context = [];
    
    public function __construct(array $config)
    {
        $this->config = $config;
        $this->correlationId = $this->getCorrelationId();
    }
    
    public function emergency(string $message, array $context = []): void
    {
        $this->log(LogLevel::EMERGENCY, $message, $context);
    }
    
    public function alert(string $message, array $context = []): void
    {
        $this->log(LogLevel::ALERT, $message, $context);
    }
    
    public function critical(string $message, array $context = []): void
    {
        $this->log(LogLevel::CRITICAL, $message, $context);
    }
    
    public function error(string $message, array $context = []): void
    {
        $this->log(LogLevel::ERROR, $message, $context);
    }
    
    public function warning(string $message, array $context = []): void
    {
        $this->log(LogLevel::WARNING, $message, $context);
    }
    
    public function notice(string $message, array $context = []): void
    {
        $this->log(LogLevel::NOTICE, $message, $context);
    }
    
    public function info(string $message, array $context = []): void
    {
        $this->log(LogLevel::INFO, $message, $context);
    }
    
    public function debug(string $message, array $context = []): void
    {
        $this->log(LogLevel::DEBUG, $message, $context);
    }
    
    public function log($level, string $message, array $context = []): void
    {
        $logEntry = $this->createLogEntry($level, $message, $context);
        
        if ($this->shouldLog($level)) {
            $this->writeLog($logEntry);
        }
    }
    
    public function addContext(string $key, mixed $value): void
    {
        $this->context[$key] = $value;
    }
    
    public function removeContext(string $key): void
    {
        unset($this->context[$key]);
    }
    
    public function clearContext(): void
    {
        $this->context = [];
    }
    
    private function createLogEntry($level, string $message, array $context): array
    {
        $entry = [
            'timestamp' => date('c'),
            'level' => $level,
            'message' => $message,
            'correlation_id' => $this->correlationId,
            'application' => $this->config['labels']['application'],
            'environment' => $this->config['labels']['environment'],
            'version' => $this->config['labels']['version'],
            'instance' => $this->config['labels']['instance'],
            'context' => array_merge($this->context, $context)
        ];
        
        // Add request information if available
        if (isset($_SERVER['REQUEST_URI'])) {
            $entry['request'] = [
                'method' => $_SERVER['REQUEST_METHOD'] ?? 'unknown',
                'uri' => $_SERVER['REQUEST_URI'],
                'user_agent' => $_SERVER['HTTP_USER_AGENT'] ?? 'unknown',
                'ip' => $_SERVER['REMOTE_ADDR'] ?? 'unknown'
            ];
        }
        
        // Add performance metrics
        $entry['performance'] = [
            'memory_usage' => memory_get_usage(true),
            'memory_peak' => memory_get_peak_usage(true),
            'execution_time' => microtime(true) - $_SERVER['REQUEST_TIME_FLOAT'] ?? 0
        ];
        
        return $entry;
    }
    
    private function shouldLog($level): bool
    {
        $levels = [
            LogLevel::EMERGENCY => 0,
            LogLevel::ALERT => 1,
            LogLevel::CRITICAL => 2,
            LogLevel::ERROR => 3,
            LogLevel::WARNING => 4,
            LogLevel::NOTICE => 5,
            LogLevel::INFO => 6,
            LogLevel::DEBUG => 7
        ];
        
        $currentLevel = $levels[$this->config['logging']['level']] ?? 6;
        $messageLevel = $levels[$level] ?? 6;
        
        return $messageLevel <= $currentLevel;
    }
    
    private function writeLog(array $entry): void
    {
        $formattedEntry = $this->formatLogEntry($entry);
        
        foreach ($this->config['logging']['outputs'] as $output => $config) {
            if ($config['enabled'] ?? false) {
                $this->writeToOutput($output, $config, $formattedEntry);
            }
        }
    }
    
    private function formatLogEntry(array $entry): string
    {
        if ($this->config['logging']['format'] === 'json') {
            return json_encode($entry, JSON_UNESCAPED_SLASHES);
        }
        
        return sprintf(
            '[%s] %s: %s %s',
            $entry['timestamp'],
            strtoupper($entry['level']),
            $entry['message'],
            !empty($entry['context']) ? json_encode($entry['context']) : ''
        );
    }
    
    private function writeToOutput(string $output, array $config, string $entry): void
    {
        switch ($output) {
            case 'file':
                $this->writeToFile($config['path'], $entry);
                break;
            case 'elasticsearch':
                $this->writeToElasticsearch($config, $entry);
                break;
            case 'stdout':
                echo $entry . PHP_EOL;
                break;
        }
    }
    
    private function writeToFile(string $path, string $entry): void
    {
        $dir = dirname($path);
        if (!is_dir($dir)) {
            mkdir($dir, 0755, true);
        }
        
        file_put_contents($path, $entry . PHP_EOL, FILE_APPEND | LOCK_EX);
    }
    
    private function writeToElasticsearch(array $config, string $entry): void
    {
        $client = new \Elasticsearch\Client([
            'hosts' => $config['hosts']
        ]);
        
        $params = [
            'index' => $config['index'],
            'body' => json_decode($entry, true)
        ];
        
        try {
            $client->index($params);
        } catch (\Exception $e) {
            // Fallback to file logging if Elasticsearch fails
            error_log("Failed to write to Elasticsearch: " . $e->getMessage());
            $this->writeToFile('/var/log/app/elasticsearch_fallback.log', $entry);
        }
    }
    
    private function getCorrelationId(): string
    {
        if ($this->config['logging']['correlation']['enabled']) {
            $header = $this->config['logging']['correlation']['header'];
            $correlationId = $_SERVER['HTTP_' . strtoupper(str_replace('-', '_', $header))] ?? null;
            
            if (!$correlationId && ($this->config['logging']['correlation']['generate_if_missing'] ?? false)) {
                $correlationId = uniqid('req_', true);
            }
            
            return $correlationId ?? 'unknown';
        }
        
        return 'disabled';
    }
}
```

## Distributed Tracing

```php
<?php
// app/Infrastructure/Tracing/TraceManager.php

namespace App\Infrastructure\Tracing;

use TuskLang\Tracing\TraceManagerInterface;
use TuskLang\Tracing\Span;
use TuskLang\Tracing\Tracer;

class TraceManager implements TraceManagerInterface
{
    private Tracer $tracer;
    private array $config;
    private ?Span $currentSpan = null;
    
    public function __construct(Tracer $tracer, array $config)
    {
        $this->tracer = $tracer;
        $this->config = $config;
    }
    
    public function startSpan(string $name, array $attributes = []): Span
    {
        if ($this->currentSpan) {
            $span = $this->tracer->startSpan($name, [
                'parent' => $this->currentSpan,
                'attributes' => $attributes
            ]);
        } else {
            $span = $this->tracer->startSpan($name, [
                'attributes' => $attributes
            ]);
        }
        
        $this->currentSpan = $span;
        return $span;
    }
    
    public function endSpan(Span $span): void
    {
        $span->end();
        
        if ($this->currentSpan === $span) {
            $this->currentSpan = $span->getParent();
        }
    }
    
    public function addEvent(string $name, array $attributes = []): void
    {
        if ($this->currentSpan) {
            $this->currentSpan->addEvent($name, $attributes);
        }
    }
    
    public function setAttribute(string $key, mixed $value): void
    {
        if ($this->currentSpan) {
            $this->currentSpan->setAttribute($key, $value);
        }
    }
    
    public function traceDatabaseQuery(callable $query, string $sql, array $params = []): mixed
    {
        $span = $this->startSpan('database.query', [
            'db.system' => 'mysql',
            'db.statement' => $sql,
            'db.parameters' => json_encode($params)
        ]);
        
        try {
            $result = $query();
            $span->setAttribute('db.result_count', is_array($result) ? count($result) : 1);
            return $result;
        } catch (\Exception $e) {
            $span->setAttribute('error', true);
            $span->setAttribute('error.message', $e->getMessage());
            throw $e;
        } finally {
            $this->endSpan($span);
        }
    }
    
    public function traceHttpRequest(callable $request, string $method, string $url): mixed
    {
        $span = $this->startSpan('http.request', [
            'http.method' => $method,
            'http.url' => $url
        ]);
        
        try {
            $result = $request();
            $span->setAttribute('http.status_code', $result['status_code'] ?? 200);
            return $result;
        } catch (\Exception $e) {
            $span->setAttribute('error', true);
            $span->setAttribute('error.message', $e->getMessage());
            throw $e;
        } finally {
            $this->endSpan($span);
        }
    }
    
    public function traceCacheOperation(callable $operation, string $operation_type, string $key): mixed
    {
        $span = $this->startSpan('cache.operation', [
            'cache.operation' => $operation_type,
            'cache.key' => $key
        ]);
        
        try {
            $result = $operation();
            $span->setAttribute('cache.hit', $result !== null);
            return $result;
        } catch (\Exception $e) {
            $span->setAttribute('error', true);
            $span->setAttribute('error.message', $e->getMessage());
            throw $e;
        } finally {
            $this->endSpan($span);
        }
    }
    
    public function getCurrentSpan(): ?Span
    {
        return $this->currentSpan;
    }
    
    public function injectHeaders(array &$headers): void
    {
        if ($this->currentSpan) {
            $this->tracer->inject($this->currentSpan, $headers);
        }
    }
    
    public function extractHeaders(array $headers): ?Span
    {
        return $this->tracer->extract($headers);
    }
}
```

## Alerting Systems

```php
<?php
// app/Infrastructure/Alerting/AlertManager.php

namespace App\Infrastructure\Alerting;

use TuskLang\Alerting\AlertManagerInterface;
use TuskLang\Alerting\Alert;
use TuskLang\Alerting\AlertRule;

class AlertManager implements AlertManagerInterface
{
    private array $rules = [];
    private array $notifications = [];
    private array $config;
    
    public function __construct(array $config)
    {
        $this->config = $config;
        $this->loadRules();
        $this->loadNotifications();
    }
    
    public function addRule(AlertRule $rule): void
    {
        $this->rules[] = $rule;
    }
    
    public function evaluateRules(array $metrics): array
    {
        $alerts = [];
        
        foreach ($this->rules as $rule) {
            if ($rule->evaluate($metrics)) {
                $alert = new Alert(
                    $rule->getName(),
                    $rule->getSeverity(),
                    $rule->getMessage(),
                    $metrics
                );
                
                $alerts[] = $alert;
                $this->sendNotification($alert);
            }
        }
        
        return $alerts;
    }
    
    public function sendNotification(Alert $alert): void
    {
        $notificationType = $this->getNotificationType($alert->getSeverity());
        
        if (isset($this->notifications[$notificationType])) {
            $notification = $this->notifications[$notificationType];
            $notification->send($alert);
        }
    }
    
    private function loadRules(): void
    {
        foreach ($this->config['alerting']['rules'] as $ruleConfig) {
            $rule = new AlertRule(
                $ruleConfig['name'],
                $ruleConfig['condition'],
                $ruleConfig['duration'],
                $ruleConfig['severity'],
                $ruleConfig['message'] ?? ''
            );
            
            $this->addRule($rule);
        }
    }
    
    private function loadNotifications(): void
    {
        foreach ($this->config['alerting']['notifications'] as $type => $config) {
            $this->notifications[$type] = $this->createNotification($type, $config);
        }
    }
    
    private function createNotification(string $type, array $config): NotificationInterface
    {
        return match($type) {
            'slack' => new SlackNotification($config),
            'email' => new EmailNotification($config),
            'pagerduty' => new PagerDutyNotification($config),
            default => throw new \RuntimeException("Unknown notification type: {$type}")
        };
    }
    
    private function getNotificationType(string $severity): string
    {
        return match($severity) {
            'critical' => 'pagerduty',
            'warning' => 'slack',
            default => 'email'
        };
    }
}

// app/Infrastructure/Alerting/AlertRule.php

namespace App\Infrastructure\Alerting;

class AlertRule
{
    private string $name;
    private string $condition;
    private string $duration;
    private string $severity;
    private string $message;
    private array $evaluationHistory = [];
    
    public function __construct(string $name, string $condition, string $duration, string $severity, string $message = '')
    {
        $this->name = $name;
        $this->condition = $condition;
        $this->duration = $duration;
        $this->severity = $severity;
        $this->message = $message;
    }
    
    public function evaluate(array $metrics): bool
    {
        $result = $this->evaluateCondition($metrics);
        $this->evaluationHistory[] = [
            'timestamp' => time(),
            'result' => $result
        ];
        
        // Keep only recent evaluations
        $cutoff = time() - $this->parseDuration($this->duration);
        $this->evaluationHistory = array_filter(
            $this->evaluationHistory,
            fn($eval) => $eval['timestamp'] >= $cutoff
        );
        
        // Check if condition has been true for the required duration
        $trueCount = count(array_filter($this->evaluationHistory, fn($eval) => $eval['result']));
        $totalCount = count($this->evaluationHistory);
        
        return $totalCount > 0 && $trueCount === $totalCount;
    }
    
    private function evaluateCondition(array $metrics): bool
    {
        // Simple condition evaluation - in practice, you'd use a more sophisticated parser
        $condition = str_replace(['error_rate', 'response_time_p95', 'memory_usage'], [
            $metrics['error_rate'] ?? 0,
            $metrics['response_time_p95'] ?? 0,
            $metrics['memory_usage'] ?? 0
        ], $this->condition);
        
        return eval("return {$condition};");
    }
    
    private function parseDuration(string $duration): int
    {
        $value = (int) $duration;
        $unit = substr($duration, -1);
        
        return match($unit) {
            's' => $value,
            'm' => $value * 60,
            'h' => $value * 3600,
            'd' => $value * 86400,
            default => $value
        };
    }
    
    public function getName(): string
    {
        return $this->name;
    }
    
    public function getSeverity(): string
    {
        return $this->severity;
    }
    
    public function getMessage(): string
    {
        return $this->message;
    }
}
```

## Best Practices

```php
// config/monitoring-best-practices.tsk
monitoring_best_practices = {
    metrics = {
        collect_business_metrics = true
        collect_technical_metrics = true
        use_meaningful_names = true
        add_appropriate_labels = true
    }
    
    logging = {
        use_structured_logging = true
        include_correlation_ids = true
        log_at_appropriate_levels = true
        avoid_logging_sensitive_data = true
    }
    
    tracing = {
        trace_critical_paths = true
        use_meaningful_span_names = true
        add_relevant_attributes = true
        sample_appropriately = true
    }
    
    alerting = {
        set_meaningful_thresholds = true
        avoid_alert_fatigue = true
        use_escalation_policies = true
        test_alert_channels = true
    }
    
    performance = {
        minimize_monitoring_overhead = true
        use_async_processing = true
        implement_caching = true
        optimize_data_storage = true
    }
    
    security = {
        secure_monitoring_endpoints = true
        encrypt_sensitive_data = true
        implement_access_controls = true
        audit_monitoring_access = true
    }
}

// Example usage in PHP
class MonitoringBestPractices
{
    public function implementBestPractices(): void
    {
        // 1. Configure monitoring
        $config = TuskLang::load('monitoring');
        $metrics = new MetricsCollector($config['metrics']);
        $logger = new StructuredLogger($config['logging']);
        $tracer = new TraceManager(new Tracer(), $config['tracing']);
        $alertManager = new AlertManager($config['alerting']);
        
        // 2. Set up performance monitoring
        $performanceMonitor = new PerformanceMonitor($metrics);
        
        // 3. Monitor application startup
        $metrics->setGauge('application_start_time', microtime(true));
        $logger->info('Application started', [
            'version' => $config['labels']['version'],
            'environment' => $config['labels']['environment']
        ]);
        
        // 4. Monitor request processing
        $startTime = microtime(true);
        
        try {
            $tracer->startSpan('http.request', [
                'http.method' => $_SERVER['REQUEST_METHOD'],
                'http.url' => $_SERVER['REQUEST_URI']
            ]);
            
            // Process request
            $result = $this->processRequest();
            
            $tracer->endSpan($tracer->getCurrentSpan());
            
            // Record metrics
            $duration = microtime(true) - $startTime;
            $metrics->recordResponseTime($_SERVER['REQUEST_URI'], $duration);
            $metrics->incrementCounter('http_requests_total', ['status' => 'success']);
            
            $logger->info('Request processed successfully', [
                'duration' => $duration,
                'status' => 'success'
            ]);
            
        } catch (\Exception $e) {
            $tracer->endSpan($tracer->getCurrentSpan());
            
            $duration = microtime(true) - $startTime;
            $metrics->recordResponseTime($_SERVER['REQUEST_URI'], $duration);
            $metrics->incrementCounter('http_requests_total', ['status' => 'error']);
            $metrics->recordError('http_request', $e->getMessage());
            
            $logger->error('Request failed', [
                'duration' => $duration,
                'error' => $e->getMessage(),
                'trace' => $e->getTraceAsString()
            ]);
        }
        
        // 5. Evaluate alert rules
        $currentMetrics = $metrics->getMetrics();
        $alerts = $alertManager->evaluateRules($currentMetrics);
        
        if (!empty($alerts)) {
            $logger->warning('Alerts triggered', ['alerts' => $alerts]);
        }
        
        // 6. Export metrics for Prometheus
        if ($_SERVER['REQUEST_URI'] === '/metrics') {
            header('Content-Type: text/plain');
            echo $metrics->exportPrometheus();
            exit;
        }
        
        // 7. Health check endpoint
        if ($_SERVER['REQUEST_URI'] === '/health') {
            $healthStatus = $this->performHealthChecks();
            header('Content-Type: application/json');
            echo json_encode($healthStatus);
            exit;
        }
        
        // 8. Log and monitor
        $logger->info('Monitoring implemented', [
            'metrics_collected' => count($currentMetrics),
            'alerts_triggered' => count($alerts),
            'tracing_enabled' => $config['tracing']['enabled']
        ]);
    }
    
    private function processRequest(): mixed
    {
        // Simulate request processing
        return ['status' => 'success'];
    }
    
    private function performHealthChecks(): array
    {
        $checks = [
            'database' => $this->checkDatabase(),
            'cache' => $this->checkCache(),
            'queue' => $this->checkQueue()
        ];
        
        $overallStatus = !in_array(false, $checks, true) ? 'healthy' : 'unhealthy';
        
        return [
            'status' => $overallStatus,
            'checks' => $checks,
            'timestamp' => date('c')
        ];
    }
    
    private function checkDatabase(): bool
    {
        // Implement database health check
        return true;
    }
    
    private function checkCache(): bool
    {
        // Implement cache health check
        return true;
    }
    
    private function checkQueue(): bool
    {
        // Implement queue health check
        return true;
    }
}
```

This comprehensive guide covers advanced monitoring and observability in TuskLang with PHP integration. The monitoring system is designed to be comprehensive, performant, and actionable while maintaining the rebellious spirit of TuskLang development. 