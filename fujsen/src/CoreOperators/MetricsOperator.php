<?php
/**
 * Metrics Operator
 * 
 * Enhanced metrics operator with support for various metric types,
 * aggregation, and multiple backends.
 * 
 * @package TuskPHP\CoreOperators
 * @version 2.0.0
 */

namespace TuskPHP\CoreOperators;

/**
 * Metrics Operator
 * 
 * Provides comprehensive metrics collection and retrieval capabilities
 * with support for counters, gauges, histograms, and custom metrics.
 */
class MetricsOperator extends BaseOperator
{
    private array $backends = [];
    private array $metrics = [];
    private array $aggregators = [];
    
    public function __construct()
    {
        $this->version = '2.0.0';
        $this->requiredFields = ['name'];
        $this->optionalFields = [
            'value', 'type', 'tags', 'backend', 'aggregation',
            'unit', 'description', 'timestamp'
        ];
        
        $this->defaultConfig = [
            'type' => 'counter',
            'backend' => 'memory',
            'aggregation' => 'sum',
            'unit' => null,
            'tags' => []
        ];
        
        $this->initializeBackends();
        $this->initializeAggregators();
    }
    
    public function getName(): string
    {
        return 'metrics';
    }
    
    protected function getDescription(): string
    {
        return 'Comprehensive metrics collection and retrieval with support for counters, gauges, histograms, and custom metrics';
    }
    
    protected function getExamples(): array
    {
        return [
            'counter' => '@metrics({name: "requests_total", value: 1, type: "counter"})',
            'gauge' => '@metrics({name: "cpu_usage", value: 75.5, type: "gauge"})',
            'histogram' => '@metrics({name: "response_time", value: 150, type: "histogram"})',
            'with_tags' => '@metrics({name: "api_calls", value: 1, tags: {endpoint: "/users", method: "GET"}})',
            'retrieve' => '@metrics({name: "requests_total"})',
            'aggregated' => '@metrics({name: "response_time", aggregation: "avg", period: "1h"})'
        ];
    }
    
    protected function getErrorCodes(): array
    {
        return array_merge(parent::getErrorCodes(), [
            'METRIC_NOT_FOUND' => 'Metric not found',
            'INVALID_METRIC_TYPE' => 'Invalid metric type',
            'AGGREGATION_FAILED' => 'Metric aggregation failed',
            'BACKEND_UNAVAILABLE' => 'Metrics backend is not available',
            'INVALID_TAGS' => 'Invalid metric tags'
        ]);
    }
    
    /**
     * Initialize metrics backends
     */
    private function initializeBackends(): void
    {
        // Memory backend (default)
        $this->backends['memory'] = new MetricsBackends\MemoryBackend();
        
        // Redis backend
        if (extension_loaded('redis')) {
            $this->backends['redis'] = new MetricsBackends\RedisBackend();
        }
        
        // Prometheus backend
        $this->backends['prometheus'] = new MetricsBackends\PrometheusBackend();
        
        // StatsD backend
        $this->backends['statsd'] = new MetricsBackends\StatsDBackend();
    }
    
    /**
     * Initialize aggregators
     */
    private function initializeAggregators(): void
    {
        $this->aggregators = [
            'sum' => new MetricsAggregators\SumAggregator(),
            'avg' => new MetricsAggregators\AverageAggregator(),
            'min' => new MetricsAggregators\MinAggregator(),
            'max' => new MetricsAggregators\MaxAggregator(),
            'count' => new MetricsAggregators\CountAggregator(),
            'percentile' => new MetricsAggregators\PercentileAggregator()
        ];
    }
    
    /**
     * Execute metrics operator
     */
    protected function executeOperator(array $config, array $context): mixed
    {
        $name = $this->resolveVariable($config['name'], $context);
        $backend = $config['backend'];
        $backendInstance = $this->getBackend($backend);
        
        // If value is provided, store the metric
        if (isset($config['value'])) {
            $value = $this->resolveVariable($config['value'], $context);
            $tags = $this->resolveTags($config['tags'] ?? [], $context);
            $timestamp = $config['timestamp'] ?? time();
            
            $metric = [
                'name' => $name,
                'value' => $value,
                'type' => $config['type'],
                'tags' => $tags,
                'timestamp' => $timestamp,
                'unit' => $config['unit'] ?? null
            ];
            
            $backendInstance->store($metric);
            
            $this->log('info', 'Metric stored', [
                'name' => $name,
                'value' => $value,
                'type' => $config['type'],
                'backend' => $backend
            ]);
            
            return $value;
        }
        
        // Otherwise, retrieve the metric
        $aggregation = $config['aggregation'] ?? 'latest';
        $period = $config['period'] ?? null;
        
        $result = $backendInstance->retrieve($name, $aggregation, $period, $config['tags'] ?? []);
        
        $this->log('info', 'Metric retrieved', [
            'name' => $name,
            'aggregation' => $aggregation,
            'value' => $result,
            'backend' => $backend
        ]);
        
        return $result;
    }
    
    /**
     * Get metrics backend
     */
    private function getBackend(string $name): MetricsBackends\MetricsBackendInterface
    {
        if (!isset($this->backends[$name])) {
            throw new \InvalidArgumentException("Unknown metrics backend: {$name}");
        }
        
        return $this->backends[$name];
    }
    
    /**
     * Resolve tags
     */
    private function resolveTags(array $tags, array $context): array
    {
        $resolved = [];
        
        foreach ($tags as $key => $value) {
            $resolved[$key] = $this->resolveVariable($value, $context);
        }
        
        return $resolved;
    }
    
    /**
     * Custom validation
     */
    protected function customValidate(array $config): array
    {
        $errors = [];
        $warnings = [];
        
        // Validate metric type
        if (isset($config['type'])) {
            $validTypes = ['counter', 'gauge', 'histogram', 'summary'];
            if (!in_array($config['type'], $validTypes)) {
                $errors[] = "Invalid metric type: {$config['type']}";
            }
        }
        
        // Validate backend
        if (isset($config['backend']) && !isset($this->backends[$config['backend']])) {
            $errors[] = "Unknown metrics backend: {$config['backend']}";
        }
        
        // Validate aggregation
        if (isset($config['aggregation']) && !isset($this->aggregators[$config['aggregation']])) {
            $errors[] = "Unknown aggregation: {$config['aggregation']}";
        }
        
        // Validate tags
        if (isset($config['tags']) && !is_array($config['tags'])) {
            $errors[] = "Tags must be an array";
        }
        
        return ['errors' => $errors, 'warnings' => $warnings];
    }
    
    /**
     * Get metrics statistics
     */
    public function getStatistics(): array
    {
        $stats = [];
        
        foreach ($this->backends as $name => $backend) {
            $stats[$name] = $backend->getStatistics();
        }
        
        return $stats;
    }
    
    /**
     * Cleanup resources
     */
    public function cleanup(): void
    {
        foreach ($this->backends as $backend) {
            $backend->cleanup();
        }
    }
}

/**
 * Metrics Backend Interface
 */
interface MetricsBackendInterface
{
    public function store(array $metric): bool;
    public function retrieve(string $name, string $aggregation, ?string $period, array $tags): mixed;
    public function getStatistics(): array;
    public function cleanup(): void;
}

/**
 * Memory Metrics Backend
 */
class MemoryBackend implements MetricsBackendInterface
{
    private array $metrics = [];
    private array $stats = [
        'stored' => 0,
        'retrieved' => 0,
        'errors' => 0
    ];
    
    public function store(array $metric): bool
    {
        $key = $this->getMetricKey($metric['name'], $metric['tags']);
        
        if (!isset($this->metrics[$key])) {
            $this->metrics[$key] = [];
        }
        
        $this->metrics[$key][] = $metric;
        
        // Keep only last 1000 values per metric
        if (count($this->metrics[$key]) > 1000) {
            array_shift($this->metrics[$key]);
        }
        
        $this->stats['stored']++;
        return true;
    }
    
    public function retrieve(string $name, string $aggregation, ?string $period, array $tags): mixed
    {
        $key = $this->getMetricKey($name, $tags);
        
        if (!isset($this->metrics[$key]) || empty($this->metrics[$key])) {
            $this->stats['errors']++;
            return null;
        }
        
        $values = $this->metrics[$key];
        
        // Filter by period if specified
        if ($period) {
            $cutoff = time() - $this->parsePeriod($period);
            $values = array_filter($values, fn($m) => $m['timestamp'] >= $cutoff);
        }
        
        if (empty($values)) {
            return null;
        }
        
        // Apply aggregation
        $result = $this->applyAggregation($values, $aggregation);
        $this->stats['retrieved']++;
        
        return $result;
    }
    
    public function getStatistics(): array
    {
        return array_merge($this->stats, [
            'metrics' => count($this->metrics),
            'total_values' => array_sum(array_map('count', $this->metrics))
        ]);
    }
    
    public function cleanup(): void
    {
        // Remove old metrics (older than 24 hours)
        $cutoff = time() - 86400;
        
        foreach ($this->metrics as $key => $values) {
            $this->metrics[$key] = array_filter($values, fn($m) => $m['timestamp'] >= $cutoff);
        }
    }
    
    private function getMetricKey(string $name, array $tags): string
    {
        $tagString = '';
        if (!empty($tags)) {
            ksort($tags);
            $tagString = ':' . json_encode($tags);
        }
        
        return $name . $tagString;
    }
    
    private function parsePeriod(string $period): int
    {
        $units = [
            's' => 1,
            'm' => 60,
            'h' => 3600,
            'd' => 86400
        ];
        
        if (preg_match('/^(\d+)([smhd])$/', strtolower($period), $matches)) {
            $value = (int)$matches[1];
            $unit = $matches[2];
            
            if (isset($units[$unit])) {
                return $value * $units[$unit];
            }
        }
        
        return 3600; // Default 1 hour
    }
    
    private function applyAggregation(array $values, string $aggregation): mixed
    {
        $metricValues = array_column($values, 'value');
        
        switch ($aggregation) {
            case 'latest':
                return end($values)['value'];
            case 'sum':
                return array_sum($metricValues);
            case 'avg':
                return array_sum($metricValues) / count($metricValues);
            case 'min':
                return min($metricValues);
            case 'max':
                return max($metricValues);
            case 'count':
                return count($metricValues);
            default:
                return end($values)['value'];
        }
    }
}

/**
 * Redis Metrics Backend
 */
class RedisBackend implements MetricsBackendInterface
{
    private ?\Redis $redis = null;
    private array $stats = [
        'stored' => 0,
        'retrieved' => 0,
        'errors' => 0
    ];
    
    public function __construct()
    {
        $this->initializeRedis();
    }
    
    private function initializeRedis(): void
    {
        try {
            $this->redis = new \Redis();
            $this->redis->connect(
                $_ENV['REDIS_HOST'] ?? 'localhost',
                $_ENV['REDIS_PORT'] ?? 6379
            );
            
            if (isset($_ENV['REDIS_PASSWORD'])) {
                $this->redis->auth($_ENV['REDIS_PASSWORD']);
            }
            
            if (isset($_ENV['REDIS_DATABASE'])) {
                $this->redis->select($_ENV['REDIS_DATABASE']);
            }
        } catch (\Exception $e) {
            error_log("Redis connection failed: " . $e->getMessage());
            $this->redis = null;
        }
    }
    
    public function store(array $metric): bool
    {
        if (!$this->redis) {
            return false;
        }
        
        try {
            $key = $this->getMetricKey($metric['name'], $metric['tags']);
            $data = json_encode($metric);
            
            // Store in sorted set with timestamp as score
            $this->redis->zAdd($key, $metric['timestamp'], $data);
            
            // Set expiry (24 hours)
            $this->redis->expire($key, 86400);
            
            $this->stats['stored']++;
            return true;
        } catch (\Exception $e) {
            error_log("Redis metrics store failed: " . $e->getMessage());
            $this->stats['errors']++;
            return false;
        }
    }
    
    public function retrieve(string $name, string $aggregation, ?string $period, array $tags): mixed
    {
        if (!$this->redis) {
            return null;
        }
        
        try {
            $key = $this->getMetricKey($name, $tags);
            
            // Get time range
            $start = 0;
            $end = '+inf';
            
            if ($period) {
                $cutoff = time() - $this->parsePeriod($period);
                $start = $cutoff;
            }
            
            // Get values in time range
            $values = $this->redis->zRangeByScore($key, $start, $end);
            
            if (empty($values)) {
                return null;
            }
            
            // Parse values
            $metrics = [];
            foreach ($values as $value) {
                $metric = json_decode($value, true);
                if ($metric) {
                    $metrics[] = $metric;
                }
            }
            
            // Apply aggregation
            $result = $this->applyAggregation($metrics, $aggregation);
            $this->stats['retrieved']++;
            
            return $result;
        } catch (\Exception $e) {
            error_log("Redis metrics retrieve failed: " . $e->getMessage());
            $this->stats['errors']++;
            return null;
        }
    }
    
    public function getStatistics(): array
    {
        if (!$this->redis) {
            return array_merge($this->stats, ['connected' => false]);
        }
        
        try {
            $info = $this->redis->info();
            return array_merge($this->stats, [
                'connected' => true,
                'used_memory' => $info['used_memory'] ?? 0
            ]);
        } catch (\Exception $e) {
            return array_merge($this->stats, ['connected' => false]);
        }
    }
    
    public function cleanup(): void
    {
        // Redis handles cleanup automatically with TTL
    }
    
    private function getMetricKey(string $name, array $tags): string
    {
        $tagString = '';
        if (!empty($tags)) {
            ksort($tags);
            $tagString = ':' . json_encode($tags);
        }
        
        return "metrics:{$name}{$tagString}";
    }
    
    private function parsePeriod(string $period): int
    {
        $units = [
            's' => 1,
            'm' => 60,
            'h' => 3600,
            'd' => 86400
        ];
        
        if (preg_match('/^(\d+)([smhd])$/', strtolower($period), $matches)) {
            $value = (int)$matches[1];
            $unit = $matches[2];
            
            if (isset($units[$unit])) {
                return $value * $units[$unit];
            }
        }
        
        return 3600; // Default 1 hour
    }
    
    private function applyAggregation(array $values, string $aggregation): mixed
    {
        $metricValues = array_column($values, 'value');
        
        switch ($aggregation) {
            case 'latest':
                return end($values)['value'];
            case 'sum':
                return array_sum($metricValues);
            case 'avg':
                return array_sum($metricValues) / count($metricValues);
            case 'min':
                return min($metricValues);
            case 'max':
                return max($metricValues);
            case 'count':
                return count($metricValues);
            default:
                return end($values)['value'];
        }
    }
}

/**
 * Prometheus Metrics Backend
 */
class PrometheusBackend implements MetricsBackendInterface
{
    private string $endpoint;
    private array $stats = [
        'stored' => 0,
        'retrieved' => 0,
        'errors' => 0
    ];
    
    public function __construct()
    {
        $this->endpoint = $_ENV['PROMETHEUS_ENDPOINT'] ?? 'http://localhost:9090';
    }
    
    public function store(array $metric): bool
    {
        // Prometheus uses pull model, so we don't push metrics
        // This is a placeholder for future push gateway integration
        $this->stats['stored']++;
        return true;
    }
    
    public function retrieve(string $name, string $aggregation, ?string $period, array $tags): mixed
    {
        try {
            $query = $this->buildPromQLQuery($name, $aggregation, $period, $tags);
            $url = $this->endpoint . '/api/v1/query?query=' . urlencode($query);
            
            $response = file_get_contents($url);
            if ($response === false) {
                $this->stats['errors']++;
                return null;
            }
            
            $data = json_decode($response, true);
            if (!$data || $data['status'] !== 'success') {
                $this->stats['errors']++;
                return null;
            }
            
            $result = $data['data']['result'][0]['value'][1] ?? null;
            $this->stats['retrieved']++;
            
            return $result;
        } catch (\Exception $e) {
            error_log("Prometheus query failed: " . $e->getMessage());
            $this->stats['errors']++;
            return null;
        }
    }
    
    public function getStatistics(): array
    {
        return array_merge($this->stats, [
            'endpoint' => $this->endpoint,
            'connected' => $this->testConnection()
        ]);
    }
    
    public function cleanup(): void
    {
        // No cleanup needed for Prometheus
    }
    
    private function buildPromQLQuery(string $name, string $aggregation, ?string $period, array $tags): string
    {
        $query = $name;
        
        // Add tags
        if (!empty($tags)) {
            $tagConditions = [];
            foreach ($tags as $key => $value) {
                $tagConditions[] = "{$key}=\"{$value}\"";
            }
            $query .= '{' . implode(',', $tagConditions) . '}';
        }
        
        // Add aggregation
        if ($aggregation !== 'latest') {
            $query = "{$aggregation}({$query})";
        }
        
        // Add time range
        if ($period) {
            $duration = $this->parsePeriod($period);
            $query .= "[{$duration}s]";
        }
        
        return $query;
    }
    
    private function parsePeriod(string $period): int
    {
        $units = [
            's' => 1,
            'm' => 60,
            'h' => 3600,
            'd' => 86400
        ];
        
        if (preg_match('/^(\d+)([smhd])$/', strtolower($period), $matches)) {
            $value = (int)$matches[1];
            $unit = $matches[2];
            
            if (isset($units[$unit])) {
                return $value * $units[$unit];
            }
        }
        
        return 3600; // Default 1 hour
    }
    
    private function testConnection(): bool
    {
        try {
            $url = $this->endpoint . '/api/v1/query?query=up';
            $response = file_get_contents($url);
            return $response !== false;
        } catch (\Exception $e) {
            return false;
        }
    }
}

/**
 * StatsD Metrics Backend
 */
class StatsDBackend implements MetricsBackendInterface
{
    private string $host;
    private int $port;
    private ?resource $socket = null;
    private array $stats = [
        'stored' => 0,
        'retrieved' => 0,
        'errors' => 0
    ];
    
    public function __construct()
    {
        $this->host = $_ENV['STATSD_HOST'] ?? 'localhost';
        $this->port = (int)($_ENV['STATSD_PORT'] ?? 8125);
    }
    
    public function store(array $metric): bool
    {
        try {
            $message = $this->formatStatsDMessage($metric);
            $this->sendMessage($message);
            
            $this->stats['stored']++;
            return true;
        } catch (\Exception $e) {
            error_log("StatsD store failed: " . $e->getMessage());
            $this->stats['errors']++;
            return false;
        }
    }
    
    public function retrieve(string $name, string $aggregation, ?string $period, array $tags): mixed
    {
        // StatsD is push-only, so we can't retrieve metrics
        // This would require integration with a backend like Graphite
        $this->stats['errors']++;
        return null;
    }
    
    public function getStatistics(): array
    {
        return array_merge($this->stats, [
            'host' => $this->host,
            'port' => $this->port,
            'connected' => $this->socket !== null
        ]);
    }
    
    public function cleanup(): void
    {
        if ($this->socket) {
            fclose($this->socket);
            $this->socket = null;
        }
    }
    
    private function formatStatsDMessage(array $metric): string
    {
        $name = $metric['name'];
        $value = $metric['value'];
        $type = $metric['type'];
        
        // Add tags
        if (!empty($metric['tags'])) {
            $tags = [];
            foreach ($metric['tags'] as $key => $value) {
                $tags[] = "{$key}={$value}";
            }
            $name .= ':' . implode(',', $tags);
        }
        
        switch ($type) {
            case 'counter':
                return "{$name}:{$value}|c";
            case 'gauge':
                return "{$name}:{$value}|g";
            case 'histogram':
                return "{$name}:{$value}|h";
            default:
                return "{$name}:{$value}|c";
        }
    }
    
    private function sendMessage(string $message): void
    {
        if (!$this->socket) {
            $this->socket = fsockopen('udp://' . $this->host, $this->port, $errno, $errstr);
            if (!$this->socket) {
                throw new \RuntimeException("Failed to connect to StatsD: {$errstr}");
            }
        }
        
        fwrite($this->socket, $message);
    }
} 