# 🔍 Observability with TuskLang & PHP

## Introduction
Observability is the foundation of reliable, production-ready applications. TuskLang and PHP let you build comprehensive observability systems with config-driven logging, metrics, tracing, and alerting that give you complete visibility into your applications.

## Key Features
- **Structured logging and log aggregation**
- **Metrics collection and monitoring**
- **Distributed tracing**
- **Alerting and notification systems**
- **Dashboard and visualization**
- **Performance monitoring**

## Example: Observability Configuration
```ini
[observability]
logging: @go("observability.ConfigureLogging")
metrics: @go("observability.ConfigureMetrics")
tracing: @go("observability.ConfigureTracing")
alerting: @go("observability.ConfigureAlerting")
dashboard: @go("observability.ConfigureDashboard")
```

## PHP: Logging Implementation
```php
<?php

namespace App\Observability;

use TuskLang\Config;
use TuskLang\Operators\Env;
use TuskLang\Operators\Metrics;
use TuskLang\Operators\Go;

class Logger
{
    private $config;
    private $handlers = [];
    private $processors = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->setupHandlers();
        $this->setupProcessors();
    }
    
    public function log($level, $message, array $context = [])
    {
        $record = [
            'message' => $message,
            'context' => $context,
            'level' => $level,
            'level_name' => $this->getLevelName($level),
            'channel' => $this->config->get('logging.channel', 'app'),
            'datetime' => new \DateTime(),
            'extra' => []
        ];
        
        // Add trace information
        $record['extra']['trace'] = $this->getTraceInfo();
        
        // Process record through processors
        foreach ($this->processors as $processor) {
            $record = $processor->process($record);
        }
        
        // Send to handlers
        foreach ($this->handlers as $handler) {
            if ($handler->isHandling($record)) {
                $handler->handle($record);
            }
        }
        
        // Record metrics
        Metrics::record("log_entries", 1, [
            "level" => $level,
            "channel" => $record['channel']
        ]);
    }
    
    private function setupHandlers()
    {
        $handlers = $this->config->get('logging.handlers', []);
        
        foreach ($handlers as $name => $config) {
            switch ($config['type']) {
                case 'stream':
                    $this->handlers[] = new StreamHandler($config['path'], $config['level']);
                    break;
                    
                case 'syslog':
                    $this->handlers[] = new SyslogHandler($config['ident'], $config['facility'], $config['level']);
                    break;
                    
                case 'elasticsearch':
                    $this->handlers[] = new ElasticsearchHandler($config['host'], $config['index'], $config['level']);
                    break;
                    
                case 'slack':
                    $this->handlers[] = new SlackHandler($config['webhook'], $config['channel'], $config['level']);
                    break;
            }
        }
    }
    
    private function setupProcessors()
    {
        $processors = $this->config->get('logging.processors', []);
        
        foreach ($processors as $processor) {
            switch ($processor['type']) {
                case 'memory_usage':
                    $this->processors[] = new MemoryUsageProcessor();
                    break;
                    
                case 'process_id':
                    $this->processors[] = new ProcessIdProcessor();
                    break;
                    
                case 'uid':
                    $this->processors[] = new UidProcessor();
                    break;
                    
                case 'web':
                    $this->processors[] = new WebProcessor();
                    break;
            }
        }
    }
    
    private function getTraceInfo()
    {
        $trace = debug_backtrace(DEBUG_BACKTRACE_IGNORE_ARGS, 3);
        
        return [
            'file' => $trace[1]['file'] ?? '',
            'line' => $trace[1]['line'] ?? '',
            'function' => $trace[1]['function'] ?? '',
            'class' => $trace[1]['class'] ?? ''
        ];
    }
    
    public function emergency($message, array $context = [])
    {
        $this->log(Logger::EMERGENCY, $message, $context);
    }
    
    public function alert($message, array $context = [])
    {
        $this->log(Logger::ALERT, $message, $context);
    }
    
    public function critical($message, array $context = [])
    {
        $this->log(Logger::CRITICAL, $message, $context);
    }
    
    public function error($message, array $context = [])
    {
        $this->log(Logger::ERROR, $message, $context);
    }
    
    public function warning($message, array $context = [])
    {
        $this->log(Logger::WARNING, $message, $context);
    }
    
    public function notice($message, array $context = [])
    {
        $this->log(Logger::NOTICE, $message, $context);
    }
    
    public function info($message, array $context = [])
    {
        $this->log(Logger::INFO, $message, $context);
    }
    
    public function debug($message, array $context = [])
    {
        $this->log(Logger::DEBUG, $message, $context);
    }
}
```

## Metrics Collection
```php
<?php

namespace App\Observability\Metrics;

use TuskLang\Config;
use TuskLang\Operators\Metrics;

class MetricsCollector
{
    private $config;
    private $collectors = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->setupCollectors();
    }
    
    public function increment($name, $value = 1, array $labels = [])
    {
        foreach ($this->collectors as $collector) {
            $collector->increment($name, $value, $labels);
        }
        
        // Also record in TuskLang metrics
        Metrics::record($name, $value, $labels);
    }
    
    public function gauge($name, $value, array $labels = [])
    {
        foreach ($this->collectors as $collector) {
            $collector->gauge($name, $value, $labels);
        }
    }
    
    public function histogram($name, $value, array $labels = [])
    {
        foreach ($this->collectors as $collector) {
            $collector->histogram($name, $value, $labels);
        }
    }
    
    public function timing($name, $callback, array $labels = [])
    {
        $start = microtime(true);
        $result = $callback();
        $duration = (microtime(true) - $start) * 1000; // Convert to milliseconds
        
        $this->histogram($name, $duration, $labels);
        
        return $result;
    }
    
    private function setupCollectors()
    {
        $collectors = $this->config->get('metrics.collectors', []);
        
        foreach ($collectors as $collector) {
            switch ($collector['type']) {
                case 'prometheus':
                    $this->collectors[] = new PrometheusCollector($collector['host'], $collector['port']);
                    break;
                    
                case 'statsd':
                    $this->collectors[] = new StatsDCollector($collector['host'], $collector['port']);
                    break;
                    
                case 'datadog':
                    $this->collectors[] = new DataDogCollector($collector['api_key'], $collector['app_key']);
                    break;
            }
        }
    }
}

class PrometheusCollector
{
    private $registry;
    private $metrics = [];
    
    public function __construct($host, $port)
    {
        $this->registry = new \Prometheus\CollectorRegistry();
    }
    
    public function increment($name, $value = 1, array $labels = [])
    {
        $counter = $this->getOrCreateCounter($name, $labels);
        $counter->inc($value);
    }
    
    public function gauge($name, $value, array $labels = [])
    {
        $gauge = $this->getOrCreateGauge($name, $labels);
        $gauge->set($value);
    }
    
    public function histogram($name, $value, array $labels = [])
    {
        $histogram = $this->getOrCreateHistogram($name, $labels);
        $histogram->observe($value);
    }
    
    private function getOrCreateCounter($name, $labels)
    {
        $key = "counter_{$name}_" . md5(serialize($labels));
        
        if (!isset($this->metrics[$key])) {
            $this->metrics[$key] = $this->registry->getOrRegisterCounter('app', $name, '', array_keys($labels));
        }
        
        return $this->metrics[$key];
    }
    
    private function getOrCreateGauge($name, $labels)
    {
        $key = "gauge_{$name}_" . md5(serialize($labels));
        
        if (!isset($this->metrics[$key])) {
            $this->metrics[$key] = $this->registry->getOrRegisterGauge('app', $name, '', array_keys($labels));
        }
        
        return $this->metrics[$key];
    }
    
    private function getOrCreateHistogram($name, $labels)
    {
        $key = "histogram_{$name}_" . md5(serialize($labels));
        
        if (!isset($this->metrics[$key])) {
            $this->metrics[$key] = $this->registry->getOrRegisterHistogram('app', $name, '', array_keys($labels));
        }
        
        return $this->metrics[$key];
    }
}
```

## Distributed Tracing
```php
<?php

namespace App\Observability\Tracing;

use TuskLang\Config;
use OpenTelemetry\API\Trace\TracerInterface;
use OpenTelemetry\API\Trace\SpanInterface;

class TracingService
{
    private $config;
    private $tracer;
    private $spans = [];
    
    public function __construct(TracerInterface $tracer)
    {
        $this->config = new Config();
        $this->tracer = $tracer;
    }
    
    public function startSpan($name, array $attributes = [])
    {
        $span = $this->tracer->spanBuilder($name)
            ->setAttributes($attributes)
            ->startSpan();
        
        $spanId = uniqid();
        $this->spans[$spanId] = $span;
        
        return $spanId;
    }
    
    public function endSpan($spanId)
    {
        if (isset($this->spans[$spanId])) {
            $this->spans[$spanId]->end();
            unset($this->spans[$spanId]);
        }
    }
    
    public function addEvent($spanId, $name, array $attributes = [])
    {
        if (isset($this->spans[$spanId])) {
            $this->spans[$spanId]->addEvent($name, $attributes);
        }
    }
    
    public function setStatus($spanId, $code, $description = '')
    {
        if (isset($this->spans[$spanId])) {
            $this->spans[$spanId]->setStatus($code, $description);
        }
    }
    
    public function recordException($spanId, \Exception $exception)
    {
        if (isset($this->spans[$spanId])) {
            $this->spans[$spanId]->recordException($exception);
            $this->spans[$spanId]->setStatus(\OpenTelemetry\API\Trace\StatusCode::STATUS_ERROR, $exception->getMessage());
        }
    }
    
    public function injectHeaders($spanId, &$headers)
    {
        if (isset($this->spans[$spanId])) {
            $carrier = [];
            $this->tracer->getTextMapPropagator()->inject($carrier, $headers);
            
            foreach ($carrier as $key => $value) {
                $headers[$key] = $value;
            }
        }
    }
    
    public function extractHeaders($headers)
    {
        return $this->tracer->getTextMapPropagator()->extract($headers);
    }
    
    public function traceFunction($name, callable $function, array $attributes = [])
    {
        $spanId = $this->startSpan($name, $attributes);
        
        try {
            $result = $function();
            $this->setStatus($spanId, \OpenTelemetry\API\Trace\StatusCode::STATUS_OK);
            return $result;
        } catch (\Exception $e) {
            $this->recordException($spanId, $e);
            throw $e;
        } finally {
            $this->endSpan($spanId);
        }
    }
}
```

## Alerting System
```php
<?php

namespace App\Observability\Alerting;

use TuskLang\Config;

class AlertingService
{
    private $config;
    private $notifiers = [];
    private $rules = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->setupNotifiers();
        $this->loadRules();
    }
    
    public function checkAlerts()
    {
        foreach ($this->rules as $rule) {
            if ($this->evaluateRule($rule)) {
                $this->triggerAlert($rule);
            }
        }
    }
    
    private function evaluateRule($rule)
    {
        $metric = $rule['metric'];
        $operator = $rule['operator'];
        $threshold = $rule['threshold'];
        $duration = $rule['duration'] ?? '5m';
        
        $currentValue = $this->getMetricValue($metric, $duration);
        
        switch ($operator) {
            case '>':
                return $currentValue > $threshold;
                
            case '<':
                return $currentValue < $threshold;
                
            case '>=':
                return $currentValue >= $threshold;
                
            case '<=':
                return $currentValue <= $threshold;
                
            case '==':
                return $currentValue == $threshold;
                
            default:
                return false;
        }
    }
    
    private function triggerAlert($rule)
    {
        $alert = [
            'name' => $rule['name'],
            'message' => $rule['message'],
            'severity' => $rule['severity'] ?? 'warning',
            'timestamp' => date('c'),
            'metric' => $rule['metric'],
            'value' => $this->getMetricValue($rule['metric']),
            'threshold' => $rule['threshold']
        ];
        
        foreach ($this->notifiers as $notifier) {
            $notifier->send($alert);
        }
    }
    
    private function setupNotifiers()
    {
        $notifiers = $this->config->get('alerting.notifiers', []);
        
        foreach ($notifiers as $notifier) {
            switch ($notifier['type']) {
                case 'email':
                    $this->notifiers[] = new EmailNotifier($notifier['to'], $notifier['from']);
                    break;
                    
                case 'slack':
                    $this->notifiers[] = new SlackNotifier($notifier['webhook'], $notifier['channel']);
                    break;
                    
                case 'pagerduty':
                    $this->notifiers[] = new PagerDutyNotifier($notifier['api_key'], $notifier['service_id']);
                    break;
                    
                case 'webhook':
                    $this->notifiers[] = new WebhookNotifier($notifier['url'], $notifier['headers']);
                    break;
            }
        }
    }
    
    private function loadRules()
    {
        $this->rules = $this->config->get('alerting.rules', []);
    }
    
    private function getMetricValue($metric, $duration = '5m')
    {
        // This would typically query a metrics database
        // For now, return a mock value
        return rand(0, 100);
    }
}

class EmailNotifier
{
    private $to;
    private $from;
    
    public function __construct($to, $from)
    {
        $this->to = $to;
        $this->from = $from;
    }
    
    public function send($alert)
    {
        $subject = "[{$alert['severity']}] {$alert['name']}";
        $message = $this->formatAlertMessage($alert);
        
        $headers = [
            'From' => $this->from,
            'Content-Type' => 'text/html; charset=UTF-8'
        ];
        
        mail($this->to, $subject, $message, $headers);
    }
    
    private function formatAlertMessage($alert)
    {
        return "
            <h2>Alert: {$alert['name']}</h2>
            <p><strong>Message:</strong> {$alert['message']}</p>
            <p><strong>Severity:</strong> {$alert['severity']}</p>
            <p><strong>Metric:</strong> {$alert['metric']}</p>
            <p><strong>Current Value:</strong> {$alert['value']}</p>
            <p><strong>Threshold:</strong> {$alert['threshold']}</p>
            <p><strong>Time:</strong> {$alert['timestamp']}</p>
        ";
    }
}

class SlackNotifier
{
    private $webhook;
    private $channel;
    
    public function __construct($webhook, $channel)
    {
        $this->webhook = $webhook;
        $this->channel = $channel;
    }
    
    public function send($alert)
    {
        $payload = [
            'channel' => $this->channel,
            'text' => $this->formatAlertMessage($alert),
            'attachments' => [
                [
                    'color' => $this->getSeverityColor($alert['severity']),
                    'fields' => [
                        [
                            'title' => 'Metric',
                            'value' => $alert['metric'],
                            'short' => true
                        ],
                        [
                            'title' => 'Value',
                            'value' => $alert['value'],
                            'short' => true
                        ],
                        [
                            'title' => 'Threshold',
                            'value' => $alert['threshold'],
                            'short' => true
                        ]
                    ]
                ]
            ]
        ];
        
        $this->sendWebhook($payload);
    }
    
    private function formatAlertMessage($alert)
    {
        return "[{$alert['severity']}] {$alert['name']}: {$alert['message']}";
    }
    
    private function getSeverityColor($severity)
    {
        switch ($severity) {
            case 'critical':
                return '#ff0000';
            case 'warning':
                return '#ffaa00';
            default:
                return '#00ff00';
        }
    }
    
    private function sendWebhook($payload)
    {
        $ch = curl_init($this->webhook);
        curl_setopt($ch, CURLOPT_POST, true);
        curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($payload));
        curl_setopt($ch, CURLOPT_HTTPHEADER, ['Content-Type: application/json']);
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_exec($ch);
        curl_close($ch);
    }
}
```

## Best Practices
- **Use structured logging with consistent formats**
- **Collect metrics for all critical operations**
- **Implement distributed tracing for microservices**
- **Set up comprehensive alerting**
- **Monitor application performance**
- **Use correlation IDs across services**

## Performance Optimization
- **Use async logging for high-throughput applications**
- **Implement sampling for traces**
- **Use efficient metrics storage**
- **Optimize alert evaluation**

## Security Considerations
- **Sanitize log data**
- **Use secure connections for metrics**
- **Implement access controls for dashboards**
- **Protect sensitive information in traces**

## Troubleshooting
- **Monitor log aggregation**
- **Check metrics collection**
- **Verify trace propagation**
- **Test alert delivery**

## Conclusion
TuskLang + PHP = observability that gives you complete visibility into your applications. Monitor everything, understand everything. 