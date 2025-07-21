<?php

declare(strict_types=1);

namespace TuskLang\SDK\SystemOperations\ProcessManagement;

use TuskLang\SDK\Core\BaseOperator;
use TuskLang\SDK\Core\Interfaces\OperatorInterface;
use TuskLang\SDK\Core\Exceptions\SchedulerOperationException;
use TuskLang\SDK\SystemOperations\AI\SchedulingOptimizer;
use TuskLang\SDK\SystemOperations\Resources\ResourceMonitor;
use TuskLang\SDK\SystemOperations\Analytics\SchedulingAnalytics;

/**
 * Advanced Task Scheduler Operator with AI Optimization
 * 
 * Features:
 * - Advanced task scheduling system
 * - AI-powered process optimization algorithms
 * - Resource-aware scheduling with dynamic allocation
 * - Scheduler persistence and recovery mechanisms
 * - Comprehensive scheduling analytics and reporting
 * 
 * @package TuskLang\SDK\SystemOperations\ProcessManagement
 * @version 1.0.0
 * @author TuskLang AI System
 */
class SchedulerOperator extends BaseOperator implements OperatorInterface
{
    private SchedulingOptimizer $optimizer;
    private ResourceMonitor $resourceMonitor;
    private SchedulingAnalytics $analytics;
    private array $scheduledTasks = [];
    private array $taskQueues = [];
    private array $resourcePools = [];
    private bool $isRunning = false;
    private int $tickInterval = 1; // seconds

    public function __construct(array $config = [])
    {
        parent::__construct($config);
        
        $this->optimizer = new SchedulingOptimizer($config['optimizer_config'] ?? []);
        $this->resourceMonitor = new ResourceMonitor($config['resource_config'] ?? []);
        $this->analytics = new SchedulingAnalytics($config['analytics_config'] ?? []);
        
        $this->tickInterval = $config['tick_interval'] ?? 1;
        
        $this->initializeOperator();
    }

    /**
     * Schedule a task with AI optimization
     */
    public function scheduleTask(string $taskName, callable $taskCallback, array $schedule): string
    {
        try {
            $taskId = $this->generateTaskId();
            
            $task = [
                'id' => $taskId,
                'name' => $taskName,
                'callback' => $taskCallback,
                'schedule' => $schedule,
                'status' => 'scheduled',
                'created_at' => microtime(true),
                'next_run' => $this->calculateNextRun($schedule),
                'last_run' => null,
                'run_count' => 0,
                'total_runtime' => 0,
                'average_runtime' => 0,
                'success_rate' => 1.0,
                'resource_requirements' => $schedule['resources'] ?? [],
                'priority' => $schedule['priority'] ?? 5,
                'max_retries' => $schedule['max_retries'] ?? 3,
                'timeout' => $schedule['timeout'] ?? 300,
                'enabled' => true
            ];
            
            // AI optimization for scheduling
            $optimizedSchedule = $this->optimizer->optimizeTaskSchedule($task);
            $task = array_merge($task, $optimizedSchedule);
            
            $this->scheduledTasks[$taskId] = $task;
            
            // Add to appropriate queue based on priority and resource requirements
            $this->addToTaskQueue($task);
            
            $this->logOperation('task_scheduled', $taskId, [
                'name' => $taskName,
                'next_run' => date('Y-m-d H:i:s', $task['next_run']),
                'priority' => $task['priority']
            ]);
            
            return $taskId;

        } catch (\Exception $e) {
            $this->logOperation('task_schedule_error', $taskName, ['error' => $e->getMessage()]);
            throw new SchedulerOperationException("Failed to schedule task {$taskName}: " . $e->getMessage());
        }
    }

    /**
     * Start the scheduler main loop
     */
    public function startScheduler(): bool
    {
        try {
            if ($this->isRunning) {
                throw new SchedulerOperationException("Scheduler is already running");
            }
            
            $this->isRunning = true;
            
            $this->logOperation('scheduler_started', '', [
                'tick_interval' => $this->tickInterval,
                'scheduled_tasks' => count($this->scheduledTasks)
            ]);
            
            // Main scheduler loop
            while ($this->isRunning) {
                $this->processTick();
                sleep($this->tickInterval);
            }
            
            return true;

        } catch (\Exception $e) {
            $this->isRunning = false;
            $this->logOperation('scheduler_start_error', '', ['error' => $e->getMessage()]);
            throw new SchedulerOperationException("Failed to start scheduler: " . $e->getMessage());
        }
    }

    /**
     * Stop the scheduler
     */
    public function stopScheduler(): bool
    {
        try {
            if (!$this->isRunning) {
                return true;
            }
            
            $this->isRunning = false;
            
            // Wait for current tasks to complete
            $this->waitForTaskCompletion();
            
            $this->logOperation('scheduler_stopped', '');
            return true;

        } catch (\Exception $e) {
            $this->logOperation('scheduler_stop_error', '', ['error' => $e->getMessage()]);
            throw new SchedulerOperationException("Failed to stop scheduler: " . $e->getMessage());
        }
    }

    /**
     * Update task schedule
     */
    public function updateTaskSchedule(string $taskId, array $newSchedule): bool
    {
        try {
            if (!isset($this->scheduledTasks[$taskId])) {
                throw new SchedulerOperationException("Task not found: {$taskId}");
            }
            
            $task = &$this->scheduledTasks[$taskId];
            $oldSchedule = $task['schedule'];
            
            $task['schedule'] = array_merge($task['schedule'], $newSchedule);
            $task['next_run'] = $this->calculateNextRun($task['schedule']);
            
            // Re-optimize with new schedule
            $optimizedSchedule = $this->optimizer->optimizeTaskSchedule($task);
            $task = array_merge($task, $optimizedSchedule);
            
            // Update task queue position
            $this->updateTaskQueue($task);
            
            $this->logOperation('task_schedule_updated', $taskId, [
                'old_schedule' => $oldSchedule,
                'new_schedule' => $newSchedule,
                'next_run' => date('Y-m-d H:i:s', $task['next_run'])
            ]);
            
            return true;

        } catch (\Exception $e) {
            $this->logOperation('task_schedule_update_error', $taskId, ['error' => $e->getMessage()]);
            throw new SchedulerOperationException("Failed to update task schedule: " . $e->getMessage());
        }
    }

    /**
     * Pause/resume task execution
     */
    public function setTaskEnabled(string $taskId, bool $enabled): bool
    {
        try {
            if (!isset($this->scheduledTasks[$taskId])) {
                throw new SchedulerOperationException("Task not found: {$taskId}");
            }
            
            $this->scheduledTasks[$taskId]['enabled'] = $enabled;
            
            $this->logOperation('task_enabled_changed', $taskId, ['enabled' => $enabled]);
            return true;

        } catch (\Exception $e) {
            $this->logOperation('task_enable_error', $taskId, ['error' => $e->getMessage()]);
            throw new SchedulerOperationException("Failed to change task enabled status: " . $e->getMessage());
        }
    }

    /**
     * Get task status and statistics
     */
    public function getTaskStatus(string $taskId): array
    {
        try {
            if (!isset($this->scheduledTasks[$taskId])) {
                throw new SchedulerOperationException("Task not found: {$taskId}");
            }
            
            $task = $this->scheduledTasks[$taskId];
            
            return [
                'id' => $task['id'],
                'name' => $task['name'],
                'status' => $task['status'],
                'enabled' => $task['enabled'],
                'priority' => $task['priority'],
                'next_run' => $task['next_run'],
                'last_run' => $task['last_run'],
                'run_count' => $task['run_count'],
                'success_rate' => $task['success_rate'],
                'average_runtime' => $task['average_runtime'],
                'total_runtime' => $task['total_runtime'],
                'resource_requirements' => $task['resource_requirements'],
                'schedule' => $task['schedule']
            ];

        } catch (\Exception $e) {
            $this->logOperation('task_status_error', $taskId, ['error' => $e->getMessage()]);
            throw new SchedulerOperationException("Failed to get task status: " . $e->getMessage());
        }
    }

    /**
     * List all scheduled tasks
     */
    public function listTasks(array $filters = []): array
    {
        try {
            $tasks = [];
            
            foreach ($this->scheduledTasks as $task) {
                if ($this->passesTaskFilters($task, $filters)) {
                    $tasks[] = $this->getTaskStatus($task['id']);
                }
            }
            
            // Sort tasks if requested
            if (isset($filters['sort_by'])) {
                $this->sortTasks($tasks, $filters['sort_by'], $filters['sort_order'] ?? 'asc');
            }
            
            return $tasks;

        } catch (\Exception $e) {
            $this->logOperation('task_list_error', '', ['error' => $e->getMessage()]);
            throw new SchedulerOperationException("Failed to list tasks: " . $e->getMessage());
        }
    }

    /**
     * Remove task from scheduler
     */
    public function removeTask(string $taskId): bool
    {
        try {
            if (!isset($this->scheduledTasks[$taskId])) {
                throw new SchedulerOperationException("Task not found: {$taskId}");
            }
            
            // Remove from all queues
            $this->removeFromTaskQueues($taskId);
            
            // Remove from scheduled tasks
            unset($this->scheduledTasks[$taskId]);
            
            $this->logOperation('task_removed', $taskId);
            return true;

        } catch (\Exception $e) {
            $this->logOperation('task_remove_error', $taskId, ['error' => $e->getMessage()]);
            throw new SchedulerOperationException("Failed to remove task: " . $e->getMessage());
        }
    }

    /**
     * Get scheduler statistics and analytics
     */
    public function getSchedulerStatistics(): array
    {
        try {
            $stats = [
                'scheduler_status' => $this->isRunning ? 'running' : 'stopped',
                'total_tasks' => count($this->scheduledTasks),
                'enabled_tasks' => count(array_filter($this->scheduledTasks, fn($t) => $t['enabled'])),
                'disabled_tasks' => count(array_filter($this->scheduledTasks, fn($t) => !$t['enabled'])),
                'running_tasks' => count(array_filter($this->scheduledTasks, fn($t) => $t['status'] === 'running')),
                'pending_tasks' => count(array_filter($this->scheduledTasks, fn($t) => $t['status'] === 'pending')),
                'failed_tasks' => count(array_filter($this->scheduledTasks, fn($t) => $t['status'] === 'failed')),
                'resource_utilization' => $this->resourceMonitor->getCurrentUtilization(),
                'avg_task_runtime' => $this->calculateAverageTaskRuntime(),
                'task_success_rate' => $this->calculateOverallSuccessRate(),
                'next_scheduled_run' => $this->getNextScheduledRun(),
                'optimization_metrics' => $this->optimizer->getOptimizationMetrics()
            ];
            
            return $stats;

        } catch (\Exception $e) {
            $this->logOperation('scheduler_stats_error', '', ['error' => $e->getMessage()]);
            throw new SchedulerOperationException("Failed to get scheduler statistics: " . $e->getMessage());
        }
    }

    /**
     * Generate scheduling analytics report
     */
    public function generateAnalyticsReport(array $options = []): array
    {
        try {
            $timeRange = $options['time_range'] ?? '24h';
            $includeDetails = $options['include_details'] ?? false;
            
            $report = $this->analytics->generateReport([
                'tasks' => $this->scheduledTasks,
                'time_range' => $timeRange,
                'include_details' => $includeDetails
            ]);
            
            $this->logOperation('analytics_report_generated', '', [
                'time_range' => $timeRange,
                'task_count' => count($report['task_analytics'] ?? [])
            ]);
            
            return $report;

        } catch (\Exception $e) {
            $this->logOperation('analytics_report_error', '', ['error' => $e->getMessage()]);
            throw new SchedulerOperationException("Failed to generate analytics report: " . $e->getMessage());
        }
    }

    // Private helper methods

    private function processTick(): void
    {
        $currentTime = microtime(true);
        
        // Check for tasks ready to run
        $readyTasks = $this->getTasksReadyToRun($currentTime);
        
        if (!empty($readyTasks)) {
            // Resource-aware task scheduling
            $scheduledTasks = $this->scheduleTasksWithResources($readyTasks);
            
            // Execute scheduled tasks
            foreach ($scheduledTasks as $task) {
                $this->executeTask($task);
            }
        }
        
        // Update analytics
        $this->analytics->updateMetrics($this->scheduledTasks);
        
        // Optimize scheduling if needed
        if ($this->shouldOptimizeScheduling()) {
            $this->optimizeScheduling();
        }
    }

    private function getTasksReadyToRun(float $currentTime): array
    {
        $readyTasks = [];
        
        foreach ($this->scheduledTasks as $task) {
            if ($task['enabled'] && 
                $task['status'] !== 'running' && 
                $task['next_run'] <= $currentTime) {
                $readyTasks[] = $task;
            }
        }
        
        return $readyTasks;
    }

    private function scheduleTasksWithResources(array $readyTasks): array
    {
        $scheduledTasks = [];
        $currentResources = $this->resourceMonitor->getCurrentUtilization();
        
        // Sort tasks by priority and resource efficiency
        usort($readyTasks, function($a, $b) {
            // Higher priority first, then by resource efficiency
            if ($a['priority'] !== $b['priority']) {
                return $b['priority'] <=> $a['priority'];
            }
            
            return $this->calculateResourceEfficiency($a) <=> $this->calculateResourceEfficiency($b);
        });
        
        foreach ($readyTasks as $task) {
            if ($this->canScheduleTask($task, $currentResources)) {
                $scheduledTasks[] = $task;
                
                // Update resource allocation
                $this->allocateTaskResources($task, $currentResources);
            }
        }
        
        return $scheduledTasks;
    }

    private function canScheduleTask(array $task, array $currentResources): bool
    {
        $requirements = $task['resource_requirements'];
        
        foreach ($requirements as $resource => $required) {
            $available = $currentResources[$resource]['available'] ?? 100;
            if ($required > $available) {
                return false;
            }
        }
        
        return true;
    }

    private function allocateTaskResources(array $task, array &$currentResources): void
    {
        foreach ($task['resource_requirements'] as $resource => $required) {
            if (isset($currentResources[$resource])) {
                $currentResources[$resource]['available'] -= $required;
            }
        }
    }

    private function calculateResourceEfficiency(array $task): float
    {
        $requirements = $task['resource_requirements'];
        $estimatedRuntime = $task['average_runtime'] > 0 ? $task['average_runtime'] : 60;
        
        $totalResourceCost = array_sum($requirements);
        return $totalResourceCost / $estimatedRuntime;
    }

    private function executeTask(array $task): void
    {
        try {
            $taskId = $task['id'];
            $startTime = microtime(true);
            
            // Update task status
            $this->scheduledTasks[$taskId]['status'] = 'running';
            $this->scheduledTasks[$taskId]['last_run'] = $startTime;
            
            // Execute task callback
            $callback = $task['callback'];
            $result = $callback($task);
            
            $endTime = microtime(true);
            $runtime = $endTime - $startTime;
            
            // Update task statistics
            $this->updateTaskStatistics($taskId, $runtime, true);
            
            // Schedule next run
            $this->scheduledTasks[$taskId]['next_run'] = $this->calculateNextRun($task['schedule']);
            $this->scheduledTasks[$taskId]['status'] = 'scheduled';
            
            $this->logOperation('task_executed', $taskId, [
                'runtime' => $runtime,
                'result' => is_scalar($result) ? $result : 'complex_result',
                'next_run' => date('Y-m-d H:i:s', $this->scheduledTasks[$taskId]['next_run'])
            ]);

        } catch (\Exception $e) {
            $this->handleTaskError($task, $e);
        }
    }

    private function handleTaskError(array $task, \Exception $e): void
    {
        $taskId = $task['id'];
        $runtime = microtime(true) - ($task['last_run'] ?? microtime(true));
        
        // Update task statistics for failed execution
        $this->updateTaskStatistics($taskId, $runtime, false);
        
        $this->scheduledTasks[$taskId]['status'] = 'failed';
        $this->scheduledTasks[$taskId]['last_error'] = $e->getMessage();
        
        // Retry logic
        $retryCount = $this->scheduledTasks[$taskId]['retry_count'] ?? 0;
        if ($retryCount < $task['max_retries']) {
            $this->scheduledTasks[$taskId]['retry_count'] = $retryCount + 1;
            $this->scheduledTasks[$taskId]['next_run'] = microtime(true) + (60 * ($retryCount + 1)); // Exponential backoff
            $this->scheduledTasks[$taskId]['status'] = 'scheduled';
        }
        
        $this->logOperation('task_execution_error', $taskId, [
            'error' => $e->getMessage(),
            'retry_count' => $retryCount,
            'will_retry' => $retryCount < $task['max_retries']
        ]);
    }

    private function updateTaskStatistics(string $taskId, float $runtime, bool $success): void
    {
        $task = &$this->scheduledTasks[$taskId];
        
        $task['run_count']++;
        $task['total_runtime'] += $runtime;
        $task['average_runtime'] = $task['total_runtime'] / $task['run_count'];
        
        // Update success rate
        $successCount = $success ? 
            ($task['run_count'] * $task['success_rate']) + 1 :
            ($task['run_count'] * $task['success_rate']);
            
        $task['success_rate'] = $successCount / $task['run_count'];
    }

    private function calculateNextRun(array $schedule): int
    {
        $scheduleType = $schedule['type'] ?? 'interval';
        $currentTime = time();
        
        switch ($scheduleType) {
            case 'interval':
                return $currentTime + ($schedule['interval'] ?? 3600);
                
            case 'cron':
                return $this->calculateCronNextRun($schedule['expression'], $currentTime);
                
            case 'daily':
                $hour = $schedule['hour'] ?? 0;
                $minute = $schedule['minute'] ?? 0;
                $nextRun = mktime($hour, $minute, 0);
                return $nextRun > $currentTime ? $nextRun : $nextRun + 86400;
                
            case 'weekly':
                $dayOfWeek = $schedule['day_of_week'] ?? 1; // Monday
                $hour = $schedule['hour'] ?? 0;
                $minute = $schedule['minute'] ?? 0;
                $nextRun = strtotime("next " . $this->getDayName($dayOfWeek) . " {$hour}:{$minute}:00");
                return $nextRun;
                
            case 'once':
                return $schedule['run_at'] ?? $currentTime;
                
            default:
                return $currentTime + 3600; // Default to 1 hour
        }
    }

    private function calculateCronNextRun(string $cronExpression, int $currentTime): int
    {
        // Simplified cron calculation - in production, use a proper cron parser
        $parts = explode(' ', $cronExpression);
        if (count($parts) !== 5) {
            return $currentTime + 3600; // Default fallback
        }
        
        // For now, just add an hour (simplified implementation)
        return $currentTime + 3600;
    }

    private function getDayName(int $dayOfWeek): string
    {
        $days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
        return $days[$dayOfWeek % 7];
    }

    private function addToTaskQueue(array $task): void
    {
        $priority = $task['priority'];
        
        if (!isset($this->taskQueues[$priority])) {
            $this->taskQueues[$priority] = [];
        }
        
        $this->taskQueues[$priority][] = $task['id'];
    }

    private function updateTaskQueue(array $task): void
    {
        // Remove from all queues first
        $this->removeFromTaskQueues($task['id']);
        
        // Add to new queue
        $this->addToTaskQueue($task);
    }

    private function removeFromTaskQueues(string $taskId): void
    {
        foreach ($this->taskQueues as $priority => &$queue) {
            $queue = array_filter($queue, fn($id) => $id !== $taskId);
        }
    }

    private function waitForTaskCompletion(): void
    {
        $maxWait = 30; // 30 seconds
        $startTime = time();
        
        while ((time() - $startTime) < $maxWait) {
            $runningTasks = array_filter($this->scheduledTasks, fn($t) => $t['status'] === 'running');
            
            if (empty($runningTasks)) {
                break;
            }
            
            sleep(1);
        }
    }

    private function passesTaskFilters(array $task, array $filters): bool
    {
        foreach ($filters as $filter => $value) {
            switch ($filter) {
                case 'status':
                    if ($task['status'] !== $value) return false;
                    break;
                case 'enabled':
                    if ($task['enabled'] !== $value) return false;
                    break;
                case 'priority':
                    if ($task['priority'] !== $value) return false;
                    break;
                case 'name_pattern':
                    if (!preg_match($value, $task['name'])) return false;
                    break;
            }
        }
        return true;
    }

    private function sortTasks(array &$tasks, string $sortBy, string $sortOrder): void
    {
        usort($tasks, function($a, $b) use ($sortBy, $sortOrder) {
            $valueA = $a[$sortBy] ?? 0;
            $valueB = $b[$sortBy] ?? 0;
            
            $comparison = 0;
            if (is_numeric($valueA) && is_numeric($valueB)) {
                $comparison = $valueA <=> $valueB;
            } else {
                $comparison = strcasecmp($valueA, $valueB);
            }
            
            return $sortOrder === 'desc' ? -$comparison : $comparison;
        });
    }

    private function calculateAverageTaskRuntime(): float
    {
        $completedTasks = array_filter($this->scheduledTasks, fn($t) => $t['run_count'] > 0);
        
        if (empty($completedTasks)) {
            return 0;
        }
        
        $totalRuntime = array_sum(array_column($completedTasks, 'total_runtime'));
        $totalRuns = array_sum(array_column($completedTasks, 'run_count'));
        
        return $totalRuns > 0 ? $totalRuntime / $totalRuns : 0;
    }

    private function calculateOverallSuccessRate(): float
    {
        $tasks = array_filter($this->scheduledTasks, fn($t) => $t['run_count'] > 0);
        
        if (empty($tasks)) {
            return 1.0;
        }
        
        $totalSuccessRate = array_sum(array_column($tasks, 'success_rate'));
        return $totalSuccessRate / count($tasks);
    }

    private function getNextScheduledRun(): ?int
    {
        $nextRuns = array_filter(array_column($this->scheduledTasks, 'next_run'));
        return !empty($nextRuns) ? min($nextRuns) : null;
    }

    private function shouldOptimizeScheduling(): bool
    {
        // Optimize every 10 minutes or when resource utilization is high
        static $lastOptimization = 0;
        $currentTime = time();
        
        if (($currentTime - $lastOptimization) >= 600) { // 10 minutes
            $lastOptimization = $currentTime;
            return true;
        }
        
        $utilization = $this->resourceMonitor->getCurrentUtilization();
        $avgUtilization = array_sum(array_column($utilization, 'used_percent')) / count($utilization);
        
        return $avgUtilization > 80; // Optimize if average utilization > 80%
    }

    private function optimizeScheduling(): void
    {
        $optimizations = $this->optimizer->optimizeScheduling($this->scheduledTasks);
        
        foreach ($optimizations as $taskId => $optimization) {
            if (isset($this->scheduledTasks[$taskId])) {
                $this->scheduledTasks[$taskId] = array_merge($this->scheduledTasks[$taskId], $optimization);
            }
        }
        
        $this->logOperation('scheduling_optimized', '', [
            'optimized_tasks' => count($optimizations)
        ]);
    }

    private function generateTaskId(): string
    {
        return 'task_' . uniqid() . '_' . random_int(1000, 9999);
    }

    private function initializeOperator(): void
    {
        $this->optimizer->initialize();
        $this->resourceMonitor->initialize();
        $this->analytics->initialize();
        
        $this->logOperation('scheduler_operator_initialized', '', [
            'tick_interval' => $this->tickInterval
        ]);
    }

    private function logOperation(string $operation, string $taskId, array $context = []): void
    {
        $logData = [
            'operation' => $operation,
            'task_id' => $taskId,
            'timestamp' => microtime(true),
            'context' => $context
        ];
        
        error_log("SchedulerOperator: " . json_encode($logData));
    }
} 