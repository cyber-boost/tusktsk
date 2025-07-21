<?php

namespace TuskLang\A3\G5;

/**
 * Advanced Data Processing and Analytics Engine
 * 
 * Features:
 * - Modular data processing pipelines
 * - Real-time analytics and reporting
 * - Performance monitoring with thresholds
 * - Event-driven workflow execution
 * 
 * @version 2.0.0
 * @package TuskLang\A3\G5
 */
class DataProcessor
{
    private array $processors = [];
    private array $pipelines = [];
    private array $analytics = [];
    private array $performanceMetrics = [];
    
    public function __construct()
    {
        $this->initializeBuiltInProcessors();
    }
    
    /**
     * Register a data processor
     */
    public function registerProcessor(string $name, callable $processor, array $config = []): self
    {
        $this->processors[$name] = [
            'callable' => $processor,
            'config' => $config,
            'executions' => 0,
            'total_time' => 0,
            'errors' => 0
        ];
        
        return $this;
    }
    
    /**
     * Process data through a specific processor
     */
    public function process(string $processorName, mixed $data, array $options = []): mixed
    {
        if (!isset($this->processors[$processorName])) {
            throw new \InvalidArgumentException("Processor '$processorName' not found");
        }
        
        $startTime = microtime(true);
        $processor = $this->processors[$processorName];
        
        try {
            $result = call_user_func($processor['callable'], $data, $options);
            
            // Update metrics
            $executionTime = microtime(true) - $startTime;
            $this->updateProcessorMetrics($processorName, $executionTime, true);
            
            // Record analytics
            $this->recordAnalytics('process', [
                'processor' => $processorName,
                'execution_time' => $executionTime,
                'data_size' => $this->calculateDataSize($data),
                'success' => true
            ]);
            
            return $result;
            
        } catch (\Exception $e) {
            $this->updateProcessorMetrics($processorName, microtime(true) - $startTime, false);
            
            $this->recordAnalytics('process_error', [
                'processor' => $processorName,
                'error' => $e->getMessage(),
                'data_size' => $this->calculateDataSize($data)
            ]);
            
            throw $e;
        }
    }
    
    /**
     * Create and execute a processing pipeline
     */
    public function executePipeline(array $steps, mixed $data): mixed
    {
        $pipelineId = $this->generatePipelineId();
        $startTime = microtime(true);
        
        $this->pipelines[$pipelineId] = [
            'steps' => $steps,
            'start_time' => $startTime,
            'current_step' => 0,
            'data_transformations' => []
        ];
        
        $currentData = $data;
        
        foreach ($steps as $index => $step) {
            $this->pipelines[$pipelineId]['current_step'] = $index;
            
            $stepProcessor = $step['processor'] ?? null;
            $stepOptions = $step['options'] ?? [];
            
            if (!$stepProcessor) {
                throw new \InvalidArgumentException("Step $index missing processor");
            }
            
            $beforeSize = $this->calculateDataSize($currentData);
            $currentData = $this->process($stepProcessor, $currentData, $stepOptions);
            $afterSize = $this->calculateDataSize($currentData);
            
            $this->pipelines[$pipelineId]['data_transformations'][] = [
                'step' => $index,
                'processor' => $stepProcessor,
                'before_size' => $beforeSize,
                'after_size' => $afterSize,
                'size_change' => $afterSize - $beforeSize
            ];
        }
        
        $totalTime = microtime(true) - $startTime;
        
        $this->recordAnalytics('pipeline_complete', [
            'pipeline_id' => $pipelineId,
            'steps_count' => count($steps),
            'total_time' => $totalTime,
            'transformations' => $this->pipelines[$pipelineId]['data_transformations']
        ]);
        
        return $currentData;
    }
    
    /**
     * Get real-time analytics
     */
    public function getAnalytics(string $type = null, int $limit = 100): array
    {
        if ($type) {
            return array_slice(
                array_filter($this->analytics, fn($a) => $a['type'] === $type),
                -$limit
            );
        }
        
        return array_slice($this->analytics, -$limit);
    }
    
    /**
     * Generate analytics report
     */
    public function generateReport(array $options = []): array
    {
        $timeRange = $options['time_range'] ?? 3600; // Last hour
        $cutoffTime = time() - $timeRange;
        
        $recentAnalytics = array_filter($this->analytics, function($a) use ($cutoffTime) {
            return $a['timestamp'] > $cutoffTime;
        });
        
        $report = [
            'summary' => [
                'total_events' => count($recentAnalytics),
                'time_range' => $timeRange,
                'generated_at' => date('Y-m-d H:i:s')
            ],
            'processor_performance' => [],
            'pipeline_analysis' => [],
            'error_analysis' => [],
            'trends' => []
        ];
        
        // Processor performance analysis
        foreach ($this->processors as $name => $processor) {
            if ($processor['executions'] > 0) {
                $report['processor_performance'][$name] = [
                    'executions' => $processor['executions'],
                    'average_time' => $processor['total_time'] / $processor['executions'],
                    'error_rate' => ($processor['errors'] / $processor['executions']) * 100,
                    'total_time' => $processor['total_time']
                ];
            }
        }
        
        // Pipeline analysis
        $pipelineEvents = array_filter($recentAnalytics, fn($a) => $a['type'] === 'pipeline_complete');
        if (!empty($pipelineEvents)) {
            $totalPipelineTime = array_sum(array_column($pipelineEvents, 'total_time'));
            $avgStepsCount = array_sum(array_map(fn($p) => $p['data']['steps_count'], $pipelineEvents)) / count($pipelineEvents);
            
            $report['pipeline_analysis'] = [
                'total_pipelines' => count($pipelineEvents),
                'average_execution_time' => $totalPipelineTime / count($pipelineEvents),
                'average_steps' => $avgStepsCount
            ];
        }
        
        // Error analysis
        $errorEvents = array_filter($recentAnalytics, fn($a) => $a['type'] === 'process_error');
        $report['error_analysis'] = [
            'total_errors' => count($errorEvents),
            'error_rate' => count($recentAnalytics) > 0 ? (count($errorEvents) / count($recentAnalytics)) * 100 : 0
        ];
        
        return $report;
    }
    
    private function initializeBuiltInProcessors(): void
    {
        // Data validation processor
        $this->registerProcessor('validate', function($data, $options) {
            $schema = $options['schema'] ?? [];
            
            foreach ($schema as $field => $rules) {
                if (!isset($data[$field])) {
                    throw new \InvalidArgumentException("Required field '$field' missing");
                }
                
                $value = $data[$field];
                
                if (isset($rules['type']) && gettype($value) !== $rules['type']) {
                    throw new \InvalidArgumentException("Field '$field' must be of type {$rules['type']}");
                }
                
                if (isset($rules['min']) && $value < $rules['min']) {
                    throw new \InvalidArgumentException("Field '$field' must be >= {$rules['min']}");
                }
                
                if (isset($rules['max']) && $value > $rules['max']) {
                    throw new \InvalidArgumentException("Field '$field' must be <= {$rules['max']}");
                }
            }
            
            return $data;
        });
        
        // Data transformation processor
        $this->registerProcessor('transform', function($data, $options) {
            $transformations = $options['transformations'] ?? [];
            
            foreach ($transformations as $field => $transformation) {
                if (isset($data[$field])) {
                    switch ($transformation['type']) {
                        case 'uppercase':
                            $data[$field] = strtoupper($data[$field]);
                            break;
                        case 'lowercase':
                            $data[$field] = strtolower($data[$field]);
                            break;
                        case 'multiply':
                            $data[$field] *= $transformation['factor'];
                            break;
                        case 'add':
                            $data[$field] += $transformation['value'];
                            break;
                    }
                }
            }
            
            return $data;
        });
        
        // Data aggregation processor
        $this->registerProcessor('aggregate', function($data, $options) {
            $operation = $options['operation'] ?? 'sum';
            $field = $options['field'] ?? null;
            
            if (!is_array($data)) {
                throw new \InvalidArgumentException("Aggregate processor requires array data");
            }
            
            if ($field) {
                $values = array_column($data, $field);
            } else {
                $values = $data;
            }
            
            switch ($operation) {
                case 'sum':
                    return array_sum($values);
                case 'avg':
                    return count($values) > 0 ? array_sum($values) / count($values) : 0;
                case 'min':
                    return min($values);
                case 'max':
                    return max($values);
                case 'count':
                    return count($values);
                default:
                    throw new \InvalidArgumentException("Unknown aggregation operation: $operation");
            }
        });
        
        // Data filtering processor
        $this->registerProcessor('filter', function($data, $options) {
            $conditions = $options['conditions'] ?? [];
            
            if (!is_array($data)) {
                throw new \InvalidArgumentException("Filter processor requires array data");
            }
            
            return array_filter($data, function($item) use ($conditions) {
                foreach ($conditions as $field => $condition) {
                    $value = $item[$field] ?? null;
                    $operator = $condition['operator'] ?? '=';
                    $expected = $condition['value'];
                    
                    switch ($operator) {
                        case '=':
                            if ($value != $expected) return false;
                            break;
                        case '>':
                            if ($value <= $expected) return false;
                            break;
                        case '<':
                            if ($value >= $expected) return false;
                            break;
                        case 'contains':
                            if (strpos($value, $expected) === false) return false;
                            break;
                    }
                }
                return true;
            });
        });
    }
    
    private function updateProcessorMetrics(string $name, float $executionTime, bool $success): void
    {
        $this->processors[$name]['executions']++;
        $this->processors[$name]['total_time'] += $executionTime;
        
        if (!$success) {
            $this->processors[$name]['errors']++;
        }
    }
    
    private function recordAnalytics(string $type, array $data): void
    {
        $this->analytics[] = [
            'type' => $type,
            'timestamp' => time(),
            'microtime' => microtime(true),
            'data' => $data
        ];
        
        // Keep only last 10000 analytics entries
        if (count($this->analytics) > 10000) {
            $this->analytics = array_slice($this->analytics, -5000);
        }
    }
    
    private function calculateDataSize(mixed $data): int
    {
        return strlen(serialize($data));
    }
    
    private function generatePipelineId(): string
    {
        return 'pipeline_' . uniqid();
    }
} 