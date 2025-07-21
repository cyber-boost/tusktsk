<?php

declare(strict_types=1);

namespace TuskLang\SDK\SystemOperations\Monitoring;

use TuskLang\SDK\Core\BaseOperator;
use TuskLang\SDK\Core\Interfaces\OperatorInterface;
use TuskLang\SDK\Core\Exceptions\MetricsOperationException;
use TuskLang\SDK\SystemOperations\AI\MetricsAnalysisEngine;
use TuskLang\SDK\SystemOperations\Storage\TimeSeriesStorage;
use TuskLang\SDK\SystemOperations\Alerting\PredictiveAlerting;

/**
 * Advanced AI-Powered Metrics Collection & Analytics Operator
 * 
 * Features:
 * - Real-time metrics collection with intelligent sampling
 * - AI-powered anomaly detection and predictive analytics  
 * - High-performance time-series data storage and querying
 * - Custom metric aggregation with advanced mathematical functions
 * - Automated trend analysis and performance forecasting
 * - Multi-dimensional metric indexing and correlation analysis
 * 
 * @package TuskLang\SDK\SystemOperations\Monitoring
 * @version 1.0.0
 * @author TuskLang AI System
 */
class MetricsOperator extends BaseOperator implements OperatorInterface
{
    private MetricsAnalysisEngine $analysisEngine;
    private TimeSeriesStorage $storage;
    private PredictiveAlerting $alerting;
    private array $metrics = [];
    private array $collectors = [];
    private array $aggregators = [];
    private int $retentionDays = 365;
    private int $samplingInterval = 60; // seconds

    public function __construct(array $config = [])
    {
        parent::__construct($config);
        
        $this->analysisEngine = new MetricsAnalysisEngine($config['analysis_config'] ?? []);
        $this->storage = new TimeSeriesStorage($config['storage_config'] ?? []);
        $this->alerting = new PredictiveAlerting($config['alerting_config'] ?? []);
        
        $this->retentionDays = $config['retention_days'] ?? 365;
        $this->samplingInterval = $config['sampling_interval'] ?? 60;
        
        $this->initializeOperator();
    }

    /**
     * Record a metric value with timestamp and metadata
     */
    public function recordMetric(string $name, $value, array $tags = [], int $timestamp = null): bool
    {
        try {
            $timestamp = $timestamp ?? time();
            $metricId = $this->generateMetricId();
            
            $metric = [
                'id' => $metricId,
                'name' => $name,
                'value' => $value,
                'timestamp' => $timestamp,
                'tags' => $tags,
                'type' => $this->determineMetricType($value),
                'metadata' => $this->extractMetadata($name, $value, $tags)
            ];

            // Store in time-series database
            $this->storage->store($metric);
            
            // Real-time analysis
            $anomaly = $this->analysisEngine->detectAnomaly($name, $value, $tags);
            if ($anomaly) {
                $this->alerting->triggerAnomalyAlert($anomaly);
            }
            
            // Update in-memory cache
            $this->metrics[$name][] = $metric;
            
            $this->log('info', "Metric recorded: {$name} = {$value}", ['metric_id' => $metricId]);
            
            return true;
        } catch (\Exception $e) {
            throw new MetricsOperationException("Failed to record metric: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Record multiple metrics in batch for efficiency
     */
    public function recordBatch(array $metrics): bool
    {
        try {
            $processedMetrics = [];
            $anomalies = [];
            
            foreach ($metrics as $metric) {
                $metricId = $this->generateMetricId();
                $timestamp = $metric['timestamp'] ?? time();
                
                $processedMetric = [
                    'id' => $metricId,
                    'name' => $metric['name'],
                    'value' => $metric['value'],
                    'timestamp' => $timestamp,
                    'tags' => $metric['tags'] ?? [],
                    'type' => $this->determineMetricType($metric['value']),
                    'metadata' => $this->extractMetadata($metric['name'], $metric['value'], $metric['tags'] ?? [])
                ];
                
                $processedMetrics[] = $processedMetric;
                
                // Batch anomaly detection
                $anomaly = $this->analysisEngine->detectAnomaly($metric['name'], $metric['value'], $metric['tags'] ?? []);
                if ($anomaly) {
                    $anomalies[] = $anomaly;
                }
            }
            
            // Batch storage operation
            $this->storage->storeBatch($processedMetrics);
            
            // Process anomalies
            if (!empty($anomalies)) {
                $this->alerting->triggerBatchAnomalyAlerts($anomalies);
            }
            
            $this->log('info', "Batch metrics recorded", ['count' => count($processedMetrics)]);
            
            return true;
        } catch (\Exception $e) {
            throw new MetricsOperationException("Failed to record batch metrics: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Query metrics with advanced filtering and aggregation
     */
    public function queryMetrics(string $name, array $filters = [], int $from = null, int $to = null): array
    {
        try {
            $from = $from ?? (time() - 3600); // Last hour by default
            $to = $to ?? time();
            
            $results = $this->storage->query($name, $filters, $from, $to);
            
            // Apply AI-powered analysis
            $analysis = $this->analysisEngine->analyzeResults($results);
            
            return [
                'data' => $results,
                'analysis' => $analysis,
                'metadata' => [
                    'query_time' => microtime(true) - $_SERVER['REQUEST_TIME_FLOAT'],
                    'result_count' => count($results),
                    'time_range' => ['from' => $from, 'to' => $to]
                ]
            ];
        } catch (\Exception $e) {
            throw new MetricsOperationException("Failed to query metrics: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Aggregate metrics using advanced mathematical functions
     */
    public function aggregate(string $name, string $function, array $filters = [], int $interval = 300): array
    {
        try {
            $supportedFunctions = ['sum', 'avg', 'min', 'max', 'count', 'median', 'percentile', 'stddev', 'variance'];
            
            if (!in_array($function, $supportedFunctions)) {
                throw new MetricsOperationException("Unsupported aggregation function: {$function}");
            }
            
            $results = $this->storage->aggregate($name, $function, $filters, $interval);
            
            // AI-enhanced aggregation analysis
            $insights = $this->analysisEngine->generateAggregationInsights($results, $function);
            
            return [
                'aggregation' => $results,
                'insights' => $insights,
                'function' => $function,
                'interval' => $interval
            ];
        } catch (\Exception $e) {
            throw new MetricsOperationException("Failed to aggregate metrics: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Generate predictive analytics for metrics
     */
    public function generateForecast(string $name, int $horizonHours = 24, array $filters = []): array
    {
        try {
            $historicalData = $this->queryMetrics($name, $filters, time() - (7 * 24 * 3600)); // Last 7 days
            
            $forecast = $this->analysisEngine->generateForecast(
                $historicalData['data'], 
                $horizonHours
            );
            
            return [
                'forecast' => $forecast,
                'confidence' => $forecast['confidence'] ?? 0.0,
                'horizon_hours' => $horizonHours,
                'generated_at' => time()
            ];
        } catch (\Exception $e) {
            throw new MetricsOperationException("Failed to generate forecast: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Create custom metric collector
     */
    public function createCollector(string $name, callable $collector, int $interval = 60): string
    {
        try {
            $collectorId = uniqid('collector_');
            
            $this->collectors[$collectorId] = [
                'id' => $collectorId,
                'name' => $name,
                'collector' => $collector,
                'interval' => $interval,
                'last_run' => 0,
                'status' => 'active'
            ];
            
            $this->log('info', "Custom collector created: {$name}", ['collector_id' => $collectorId]);
            
            return $collectorId;
        } catch (\Exception $e) {
            throw new MetricsOperationException("Failed to create collector: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Run metric collectors
     */
    public function runCollectors(): int
    {
        try {
            $executed = 0;
            $now = time();
            
            foreach ($this->collectors as $collectorId => $collector) {
                if ($collector['status'] !== 'active') {
                    continue;
                }
                
                if (($now - $collector['last_run']) >= $collector['interval']) {
                    try {
                        $metrics = $collector['collector']();
                        
                        if (is_array($metrics)) {
                            $this->recordBatch($metrics);
                        }
                        
                        $this->collectors[$collectorId]['last_run'] = $now;
                        $executed++;
                        
                    } catch (\Exception $e) {
                        $this->log('error', "Collector execution failed: {$collector['name']}", [
                            'error' => $e->getMessage(),
                            'collector_id' => $collectorId
                        ]);
                    }
                }
            }
            
            return $executed;
        } catch (\Exception $e) {
            throw new MetricsOperationException("Failed to run collectors: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Get comprehensive metrics analytics dashboard data
     */
    public function getDashboardData(array $metricNames = [], int $timeRange = 3600): array
    {
        try {
            $dashboardData = [
                'overview' => [],
                'trends' => [],
                'anomalies' => [],
                'predictions' => [],
                'performance' => []
            ];
            
            $now = time();
            $from = $now - $timeRange;
            
            foreach ($metricNames as $metricName) {
                // Basic statistics
                $data = $this->queryMetrics($metricName, [], $from, $now);
                $dashboardData['overview'][$metricName] = [
                    'current' => end($data['data'])['value'] ?? 0,
                    'average' => array_sum(array_column($data['data'], 'value')) / count($data['data']),
                    'count' => count($data['data'])
                ];
                
                // Trend analysis
                $dashboardData['trends'][$metricName] = $this->analysisEngine->calculateTrend($data['data']);
                
                // Anomalies
                $dashboardData['anomalies'][$metricName] = $this->analysisEngine->findRecentAnomalies($metricName, $from);
                
                // Predictions
                $dashboardData['predictions'][$metricName] = $this->generateForecast($metricName, 1);
            }
            
            // System performance metrics
            $dashboardData['performance'] = [
                'query_performance' => $this->storage->getPerformanceStats(),
                'storage_usage' => $this->storage->getStorageStats(),
                'active_collectors' => count(array_filter($this->collectors, fn($c) => $c['status'] === 'active'))
            ];
            
            return $dashboardData;
        } catch (\Exception $e) {
            throw new MetricsOperationException("Failed to generate dashboard data: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Clean up old metrics based on retention policy
     */
    public function cleanup(): int
    {
        try {
            $cutoffTime = time() - ($this->retentionDays * 24 * 3600);
            $deleted = $this->storage->cleanup($cutoffTime);
            
            $this->log('info', "Metrics cleanup completed", ['deleted_count' => $deleted]);
            
            return $deleted;
        } catch (\Exception $e) {
            throw new MetricsOperationException("Failed to cleanup metrics: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Get operator statistics and health information
     */
    public function getStatistics(): array
    {
        return [
            'metrics_count' => array_sum(array_map('count', $this->metrics)),
            'collectors_count' => count($this->collectors),
            'active_collectors' => count(array_filter($this->collectors, fn($c) => $c['status'] === 'active')),
            'storage_stats' => $this->storage->getStorageStats(),
            'retention_days' => $this->retentionDays,
            'sampling_interval' => $this->samplingInterval,
            'uptime' => time() - $this->getStartTime()
        ];
    }

    // Private helper methods
    private function generateMetricId(): string
    {
        return uniqid('metric_') . '_' . random_int(1000, 9999);
    }

    private function determineMetricType($value): string
    {
        if (is_int($value)) return 'integer';
        if (is_float($value)) return 'float';
        if (is_bool($value)) return 'boolean';
        if (is_string($value)) return 'string';
        return 'unknown';
    }

    private function extractMetadata(string $name, $value, array $tags): array
    {
        return [
            'source' => 'metrics_operator',
            'host' => gethostname(),
            'process_id' => getmypid(),
            'tags_count' => count($tags),
            'value_type' => $this->determineMetricType($value)
        ];
    }

    private function initializeOperator(): void
    {
        $this->log('info', 'MetricsOperator initialized', [
            'retention_days' => $this->retentionDays,
            'sampling_interval' => $this->samplingInterval
        ]);
    }
} 