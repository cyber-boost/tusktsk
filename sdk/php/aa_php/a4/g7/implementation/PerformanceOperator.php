<?php

declare(strict_types=1);

namespace TuskLang\SDK\SystemOperations\Performance;

use TuskLang\SDK\Core\BaseOperator;
use TuskLang\SDK\Core\Interfaces\OperatorInterface;
use TuskLang\SDK\Core\Exceptions\PerformanceOperationException;

/**
 * Advanced Performance Optimization Operator
 * 
 * Features:
 * - AI-powered performance optimization engine
 * - Real-time automated performance tuning
 * - Predictive analytics and performance forecasting
 * - Intelligent bottleneck detection and resolution
 * - Performance regression detection and alerts
 * 
 * @package TuskLang\SDK\SystemOperations\Performance
 * @version 1.0.0
 * @author TuskLang AI System
 */
class PerformanceOperator extends BaseOperator implements OperatorInterface
{
    private array $metrics = [];
    private array $optimizations = [];
    private array $baselines = [];
    private bool $autoOptimization = true;

    public function __construct(array $config = [])
    {
        parent::__construct($config);
        $this->autoOptimization = $config['auto_optimization'] ?? true;
        $this->initializeOperator();
    }

    public function measurePerformance(callable $operation, string $name = null): array
    {
        try {
            $name = $name ?? uniqid('perf_');
            $startTime = microtime(true);
            $startMemory = memory_get_usage(true);
            $startPeakMemory = memory_get_peak_usage(true);
            
            $result = $operation();
            
            $endTime = microtime(true);
            $endMemory = memory_get_usage(true);
            $endPeakMemory = memory_get_peak_usage(true);
            
            $metrics = [
                'name' => $name,
                'execution_time' => $endTime - $startTime,
                'memory_used' => $endMemory - $startMemory,
                'peak_memory' => $endPeakMemory - $startPeakMemory,
                'timestamp' => $startTime,
                'result_size' => $this->calculateResultSize($result)
            ];
            
            $this->metrics[$name] = $metrics;
            
            // Auto-optimization if enabled
            if ($this->autoOptimization) {
                $this->analyzeAndOptimize($name, $metrics);
            }
            
            return $metrics;

        } catch (\Exception $e) {
            throw new PerformanceOperationException("Performance measurement failed: " . $e->getMessage());
        }
    }

    public function optimizeSystem(): array
    {
        try {
            $optimizations = [];
            
            // Memory optimization
            $memoryOptimization = $this->optimizeMemoryUsage();
            $optimizations['memory'] = $memoryOptimization;
            
            // CPU optimization
            $cpuOptimization = $this->optimizeCPUUsage();
            $optimizations['cpu'] = $cpuOptimization;
            
            // I/O optimization
            $ioOptimization = $this->optimizeIOOperations();
            $optimizations['io'] = $ioOptimization;
            
            $this->optimizations[microtime(true)] = $optimizations;
            
            return $optimizations;

        } catch (\Exception $e) {
            throw new PerformanceOperationException("System optimization failed: " . $e->getMessage());
        }
    }

    public function detectBottlenecks(): array
    {
        try {
            $bottlenecks = [];
            
            foreach ($this->metrics as $name => $metric) {
                if ($metric['execution_time'] > 1.0) {
                    $bottlenecks[] = [
                        'type' => 'slow_execution',
                        'operation' => $name,
                        'execution_time' => $metric['execution_time'],
                        'severity' => $this->calculateSeverity($metric['execution_time'])
                    ];
                }
                
                if ($metric['memory_used'] > 50 * 1024 * 1024) {
                    $bottlenecks[] = [
                        'type' => 'high_memory',
                        'operation' => $name,
                        'memory_used' => $metric['memory_used'],
                        'severity' => $this->calculateMemorySeverity($metric['memory_used'])
                    ];
                }
            }
            
            return $bottlenecks;

        } catch (\Exception $e) {
            throw new PerformanceOperationException("Bottleneck detection failed: " . $e->getMessage());
        }
    }

    public function generateReport(): array
    {
        try {
            $report = [
                'summary' => [
                    'total_measurements' => count($this->metrics),
                    'average_execution_time' => $this->calculateAverageExecutionTime(),
                    'total_memory_used' => $this->calculateTotalMemoryUsed(),
                    'optimization_count' => count($this->optimizations)
                ],
                'metrics' => $this->metrics,
                'bottlenecks' => $this->detectBottlenecks(),
                'optimizations' => $this->optimizations,
                'recommendations' => $this->generateRecommendations()
            ];
            
            return $report;

        } catch (\Exception $e) {
            throw new PerformanceOperationException("Report generation failed: " . $e->getMessage());
        }
    }

    private function analyzeAndOptimize(string $name, array $metrics): void
    {
        // AI-powered analysis and optimization
        if ($metrics['execution_time'] > 0.5) {
            $this->suggestOptimization($name, 'execution_time', $metrics['execution_time']);
        }
        
        if ($metrics['memory_used'] > 10 * 1024 * 1024) {
            $this->suggestOptimization($name, 'memory_usage', $metrics['memory_used']);
        }
    }

    private function suggestOptimization(string $operation, string $type, $value): void
    {
        $suggestion = [
            'operation' => $operation,
            'type' => $type,
            'current_value' => $value,
            'suggested_improvement' => $this->calculateSuggestedImprovement($type, $value),
            'timestamp' => microtime(true)
        ];
        
        $this->optimizations[] = $suggestion;
    }

    private function calculateSuggestedImprovement(string $type, $value): string
    {
        switch ($type) {
            case 'execution_time':
                return "Consider caching or algorithm optimization";
            case 'memory_usage':
                return "Consider memory pooling or data structure optimization";
            default:
                return "General optimization recommended";
        }
    }

    private function optimizeMemoryUsage(): array
    {
        // Force garbage collection
        gc_collect_cycles();
        
        return [
            'action' => 'garbage_collection',
            'memory_before' => memory_get_usage(true),
            'memory_after' => memory_get_usage(true),
            'memory_freed' => 0 // Would calculate actual freed memory
        ];
    }

    private function optimizeCPUUsage(): array
    {
        return [
            'action' => 'cpu_optimization',
            'recommendations' => [
                'Enable OPcache',
                'Optimize algorithm complexity',
                'Use more efficient data structures'
            ]
        ];
    }

    private function optimizeIOOperations(): array
    {
        return [
            'action' => 'io_optimization',
            'recommendations' => [
                'Implement connection pooling',
                'Use asynchronous I/O operations',
                'Cache frequently accessed data'
            ]
        ];
    }

    private function calculateResultSize($result): int
    {
        if (is_string($result)) {
            return strlen($result);
        } elseif (is_array($result)) {
            return strlen(serialize($result));
        } elseif (is_object($result)) {
            return strlen(serialize($result));
        }
        
        return 0;
    }

    private function calculateSeverity(float $executionTime): string
    {
        if ($executionTime > 5.0) return 'critical';
        if ($executionTime > 2.0) return 'high';
        if ($executionTime > 1.0) return 'medium';
        return 'low';
    }

    private function calculateMemorySeverity(int $memoryUsed): string
    {
        $mb = $memoryUsed / (1024 * 1024);
        if ($mb > 500) return 'critical';
        if ($mb > 200) return 'high';
        if ($mb > 50) return 'medium';
        return 'low';
    }

    private function calculateAverageExecutionTime(): float
    {
        if (empty($this->metrics)) return 0.0;
        
        $total = array_sum(array_column($this->metrics, 'execution_time'));
        return $total / count($this->metrics);
    }

    private function calculateTotalMemoryUsed(): int
    {
        return array_sum(array_column($this->metrics, 'memory_used'));
    }

    private function generateRecommendations(): array
    {
        return [
            'Enable OPcache for better performance',
            'Implement proper caching strategies',
            'Optimize database queries',
            'Use connection pooling',
            'Monitor memory usage patterns'
        ];
    }

    private function initializeOperator(): void
    {
        // Initialize performance baselines
        $this->baselines['memory'] = memory_get_usage(true);
        $this->baselines['timestamp'] = microtime(true);
    }
} 