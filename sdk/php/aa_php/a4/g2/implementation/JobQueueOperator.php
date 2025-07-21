<?php

declare(strict_types=1);

namespace TuskLang\SDK\SystemOperations\ProcessManagement;

use TuskLang\SDK\Core\BaseOperator;
use TuskLang\SDK\Core\Interfaces\OperatorInterface;
use TuskLang\SDK\Core\Exceptions\JobQueueOperationException;
use TuskLang\SDK\SystemOperations\Queue\JobQueue;
use TuskLang\SDK\SystemOperations\Queue\PriorityQueue;
use TuskLang\SDK\SystemOperations\Workers\WorkerPool;

/**
 * Advanced Job Queue Operator with Fault Tolerance
 * 
 * Features:
 * - Fault-tolerant job queue with priority systems
 * - Distributed job processing across multiple workers
 * - Job scheduling with cron-like functionality
 * - Job dependency management and workflow orchestration
 * - Job retry logic with exponential backoff
 * 
 * @package TuskLang\SDK\SystemOperations\ProcessManagement
 * @version 1.0.0
 * @author TuskLang AI System
 */
class JobQueueOperator extends BaseOperator implements OperatorInterface
{
    private JobQueue $jobQueue;
    private PriorityQueue $priorityQueue;
    private WorkerPool $workerPool;
    private array $activeJobs = [];
    private array $jobQueues = [];
    private array $workflows = [];
    private int $maxRetries = 3;
    private int $maxWorkers = 10;

    public function __construct(array $config = [])
    {
        parent::__construct($config);
        
        $this->jobQueue = new JobQueue($config['queue_config'] ?? []);
        $this->priorityQueue = new PriorityQueue($config['priority_config'] ?? []);
        $this->workerPool = new WorkerPool($config['worker_config'] ?? []);
        
        $this->maxRetries = $config['max_retries'] ?? 3;
        $this->maxWorkers = $config['max_workers'] ?? 10;
        
        $this->initializeOperator();
    }

    /**
     * Add job to queue with priority and options
     */
    public function addJob(string $jobType, array $payload, array $options = []): string
    {
        try {
            $jobId = $this->generateJobId();
            $priority = $options['priority'] ?? 5; // 1-10 scale
            $delay = $options['delay'] ?? 0;
            $maxRetries = $options['max_retries'] ?? $this->maxRetries;
            $timeout = $options['timeout'] ?? 300;
            $dependencies = $options['dependencies'] ?? [];
            
            $job = [
                'id' => $jobId,
                'type' => $jobType,
                'payload' => $payload,
                'priority' => $priority,
                'status' => 'queued',
                'created_at' => microtime(true),
                'scheduled_at' => microtime(true) + $delay,
                'attempts' => 0,
                'max_retries' => $maxRetries,
                'timeout' => $timeout,
                'dependencies' => $dependencies,
                'options' => $options
            ];
            
            // Add to appropriate queue based on priority and dependencies
            if ($priority >= 8) {
                $this->priorityQueue->addHighPriorityJob($job);
            } elseif (!empty($dependencies)) {
                $this->addJobWithDependencies($job);
            } else {
                $this->jobQueue->addJob($job);
            }
            
            $this->activeJobs[$jobId] = $job;
            
            $this->logOperation('job_added', $jobId, [
                'type' => $jobType,
                'priority' => $priority,
                'delay' => $delay,
                'dependencies' => count($dependencies)
            ]);
            
            return $jobId;

        } catch (\Exception $e) {
            $this->logOperation('job_add_error', '', [
                'type' => $jobType,
                'error' => $e->getMessage()
            ]);
            throw new JobQueueOperationException("Failed to add job: " . $e->getMessage());
        }
    }

    /**
     * Process jobs from queue with workers
     */
    public function processJobs(array $options = []): array
    {
        try {
            $batchSize = $options['batch_size'] ?? 10;
            $workerCount = min($options['workers'] ?? $this->maxWorkers, $this->maxWorkers);
            $timeout = $options['timeout'] ?? 60;
            
            $processedJobs = [];
            $startTime = microtime(true);
            
            // Initialize workers
            $this->workerPool->setWorkerCount($workerCount);
            
            while ((microtime(true) - $startTime) < $timeout) {
                // Get next batch of jobs to process
                $jobs = $this->getNextJobBatch($batchSize);
                
                if (empty($jobs)) {
                    break; // No more jobs to process
                }
                
                // Distribute jobs to workers
                $workerResults = $this->distributeJobsToWorkers($jobs);
                $processedJobs = array_merge($processedJobs, $workerResults);
                
                // Update job statuses
                $this->updateJobStatuses($workerResults);
                
                // Handle failed jobs
                $this->handleFailedJobs($workerResults);
            }
            
            $result = [
                'processed_jobs' => count($processedJobs),
                'successful_jobs' => count(array_filter($processedJobs, fn($j) => $j['status'] === 'completed')),
                'failed_jobs' => count(array_filter($processedJobs, fn($j) => $j['status'] === 'failed')),
                'processing_time' => microtime(true) - $startTime,
                'worker_count' => $workerCount
            ];
            
            $this->logOperation('jobs_processed', '', $result);
            return $result;

        } catch (\Exception $e) {
            $this->logOperation('job_processing_error', '', ['error' => $e->getMessage()]);
            throw new JobQueueOperationException("Job processing failed: " . $e->getMessage());
        }
    }

    /**
     * Create workflow with job dependencies
     */
    public function createWorkflow(string $workflowName, array $jobDefinitions): string
    {
        try {
            $workflowId = $this->generateWorkflowId();
            
            $workflow = [
                'id' => $workflowId,
                'name' => $workflowName,
                'status' => 'created',
                'created_at' => microtime(true),
                'jobs' => [],
                'dependencies' => [],
                'execution_plan' => []
            ];
            
            // Validate and process job definitions
            foreach ($jobDefinitions as $jobDef) {
                $this->validateJobDefinition($jobDef);
                
                $jobId = $this->addJob($jobDef['type'], $jobDef['payload'], [
                    'workflow_id' => $workflowId,
                    'dependencies' => $jobDef['dependencies'] ?? [],
                    'priority' => $jobDef['priority'] ?? 5
                ]);
                
                $workflow['jobs'][] = $jobId;
                
                if (!empty($jobDef['dependencies'])) {
                    $workflow['dependencies'][$jobId] = $jobDef['dependencies'];
                }
            }
            
            // Generate execution plan
            $workflow['execution_plan'] = $this->generateExecutionPlan($workflow['dependencies']);
            
            $this->workflows[$workflowId] = $workflow;
            
            $this->logOperation('workflow_created', $workflowId, [
                'name' => $workflowName,
                'job_count' => count($workflow['jobs'])
            ]);
            
            return $workflowId;

        } catch (\Exception $e) {
            $this->logOperation('workflow_create_error', $workflowName, ['error' => $e->getMessage()]);
            throw new JobQueueOperationException("Workflow creation failed: " . $e->getMessage());
        }
    }

    /**
     * Execute workflow with dependency management
     */
    public function executeWorkflow(string $workflowId, array $options = []): array
    {
        try {
            if (!isset($this->workflows[$workflowId])) {
                throw new JobQueueOperationException("Workflow not found: {$workflowId}");
            }
            
            $workflow = &$this->workflows[$workflowId];
            $workflow['status'] = 'executing';
            $workflow['started_at'] = microtime(true);
            
            $executionResults = [];
            $executionPlan = $workflow['execution_plan'];
            
            foreach ($executionPlan as $phase => $jobIds) {
                $phaseResults = [];
                
                foreach ($jobIds as $jobId) {
                    // Check if dependencies are satisfied
                    if ($this->areDependenciesSatisfied($jobId, $executionResults)) {
                        $jobResult = $this->executeJob($jobId);
                        $phaseResults[$jobId] = $jobResult;
                        $executionResults[$jobId] = $jobResult;
                        
                        if ($jobResult['status'] === 'failed' && ($options['fail_fast'] ?? true)) {
                            $workflow['status'] = 'failed';
                            throw new JobQueueOperationException("Workflow failed at job: {$jobId}");
                        }
                    }
                }
                
                // Wait for all jobs in phase to complete if parallel execution
                if ($options['parallel_phases'] ?? false) {
                    $this->waitForPhaseCompletion($phaseResults);
                }
            }
            
            $workflow['status'] = 'completed';
            $workflow['completed_at'] = microtime(true);
            $workflow['execution_time'] = $workflow['completed_at'] - $workflow['started_at'];
            
            $result = [
                'workflow_id' => $workflowId,
                'status' => $workflow['status'],
                'execution_time' => $workflow['execution_time'],
                'job_results' => $executionResults
            ];
            
            $this->logOperation('workflow_executed', $workflowId, $result);
            return $result;

        } catch (\Exception $e) {
            if (isset($workflow)) {
                $workflow['status'] = 'failed';
                $workflow['error'] = $e->getMessage();
            }
            
            $this->logOperation('workflow_execution_error', $workflowId, ['error' => $e->getMessage()]);
            throw new JobQueueOperationException("Workflow execution failed: " . $e->getMessage());
        }
    }

    /**
     * Schedule recurring job with cron-like syntax
     */
    public function scheduleRecurringJob(string $jobType, array $payload, string $cronExpression, array $options = []): string
    {
        try {
            $scheduleId = $this->generateScheduleId();
            
            $schedule = [
                'id' => $scheduleId,
                'job_type' => $jobType,
                'payload' => $payload,
                'cron_expression' => $cronExpression,
                'options' => $options,
                'next_run' => $this->calculateNextRun($cronExpression),
                'last_run' => null,
                'run_count' => 0,
                'created_at' => microtime(true),
                'active' => true
            ];
            
            // Validate cron expression
            if (!$this->isValidCronExpression($cronExpression)) {
                throw new JobQueueOperationException("Invalid cron expression: {$cronExpression}");
            }
            
            // Store schedule
            $this->jobQueue->addSchedule($schedule);
            
            $this->logOperation('recurring_job_scheduled', $scheduleId, [
                'job_type' => $jobType,
                'cron_expression' => $cronExpression,
                'next_run' => date('Y-m-d H:i:s', $schedule['next_run'])
            ]);
            
            return $scheduleId;

        } catch (\Exception $e) {
            $this->logOperation('recurring_job_schedule_error', '', [
                'job_type' => $jobType,
                'cron_expression' => $cronExpression,
                'error' => $e->getMessage()
            ]);
            throw new JobQueueOperationException("Failed to schedule recurring job: " . $e->getMessage());
        }
    }

    /**
     * Get job status and information
     */
    public function getJobStatus(string $jobId): array
    {
        try {
            if (!isset($this->activeJobs[$jobId])) {
                throw new JobQueueOperationException("Job not found: {$jobId}");
            }
            
            $job = $this->activeJobs[$jobId];
            
            $status = [
                'id' => $jobId,
                'type' => $job['type'],
                'status' => $job['status'],
                'priority' => $job['priority'],
                'created_at' => $job['created_at'],
                'started_at' => $job['started_at'] ?? null,
                'completed_at' => $job['completed_at'] ?? null,
                'attempts' => $job['attempts'],
                'max_retries' => $job['max_retries'],
                'last_error' => $job['last_error'] ?? null,
                'progress' => $job['progress'] ?? 0,
                'estimated_completion' => $this->estimateJobCompletion($job)
            ];
            
            return $status;

        } catch (\Exception $e) {
            $this->logOperation('job_status_error', $jobId, ['error' => $e->getMessage()]);
            throw new JobQueueOperationException("Failed to get job status: " . $e->getMessage());
        }
    }

    /**
     * Cancel job if not yet started
     */
    public function cancelJob(string $jobId): bool
    {
        try {
            if (!isset($this->activeJobs[$jobId])) {
                throw new JobQueueOperationException("Job not found: {$jobId}");
            }
            
            $job = &$this->activeJobs[$jobId];
            
            if ($job['status'] === 'processing') {
                throw new JobQueueOperationException("Cannot cancel job in progress: {$jobId}");
            }
            
            if ($job['status'] === 'completed' || $job['status'] === 'failed') {
                throw new JobQueueOperationException("Cannot cancel finished job: {$jobId}");
            }
            
            $job['status'] = 'cancelled';
            $job['cancelled_at'] = microtime(true);
            
            // Remove from queues
            $this->jobQueue->removeJob($jobId);
            $this->priorityQueue->removeJob($jobId);
            
            $this->logOperation('job_cancelled', $jobId);
            return true;

        } catch (\Exception $e) {
            $this->logOperation('job_cancel_error', $jobId, ['error' => $e->getMessage()]);
            throw new JobQueueOperationException("Failed to cancel job: " . $e->getMessage());
        }
    }

    /**
     * Get queue statistics
     */
    public function getQueueStatistics(): array
    {
        try {
            $stats = [
                'total_jobs' => count($this->activeJobs),
                'queued_jobs' => count(array_filter($this->activeJobs, fn($j) => $j['status'] === 'queued')),
                'processing_jobs' => count(array_filter($this->activeJobs, fn($j) => $j['status'] === 'processing')),
                'completed_jobs' => count(array_filter($this->activeJobs, fn($j) => $j['status'] === 'completed')),
                'failed_jobs' => count(array_filter($this->activeJobs, fn($j) => $j['status'] === 'failed')),
                'cancelled_jobs' => count(array_filter($this->activeJobs, fn($j) => $j['status'] === 'cancelled')),
                'active_workers' => $this->workerPool->getActiveWorkerCount(),
                'total_workflows' => count($this->workflows),
                'queue_throughput' => $this->calculateThroughput(),
                'average_processing_time' => $this->calculateAverageProcessingTime()
            ];
            
            return $stats;

        } catch (\Exception $e) {
            $this->logOperation('queue_stats_error', '', ['error' => $e->getMessage()]);
            throw new JobQueueOperationException("Failed to get queue statistics: " . $e->getMessage());
        }
    }

    // Private helper methods

    private function getNextJobBatch(int $batchSize): array
    {
        $jobs = [];
        
        // Get high priority jobs first
        $highPriorityJobs = $this->priorityQueue->getNextJobs($batchSize);
        $jobs = array_merge($jobs, $highPriorityJobs);
        
        // Fill remaining batch with regular jobs
        $remainingSize = $batchSize - count($jobs);
        if ($remainingSize > 0) {
            $regularJobs = $this->jobQueue->getNextJobs($remainingSize);
            $jobs = array_merge($jobs, $regularJobs);
        }
        
        // Filter out jobs that are not ready (dependencies, scheduling)
        return array_filter($jobs, [$this, 'isJobReady']);
    }

    private function isJobReady(array $job): bool
    {
        // Check if job is scheduled to run
        if ($job['scheduled_at'] > microtime(true)) {
            return false;
        }
        
        // Check dependencies
        if (!empty($job['dependencies'])) {
            return $this->areDependenciesSatisfied($job['id'], $this->activeJobs);
        }
        
        return true;
    }

    private function distributeJobsToWorkers(array $jobs): array
    {
        $results = [];
        
        foreach ($jobs as $job) {
            $worker = $this->workerPool->getAvailableWorker();
            if ($worker) {
                $result = $worker->executeJob($job);
                $results[] = $result;
            } else {
                // No workers available, requeue job
                $this->requeueJob($job);
            }
        }
        
        return $results;
    }

    private function updateJobStatuses(array $workerResults): void
    {
        foreach ($workerResults as $result) {
            $jobId = $result['job_id'];
            if (isset($this->activeJobs[$jobId])) {
                $this->activeJobs[$jobId]['status'] = $result['status'];
                $this->activeJobs[$jobId]['completed_at'] = $result['completed_at'] ?? null;
                $this->activeJobs[$jobId]['result'] = $result['result'] ?? null;
                $this->activeJobs[$jobId]['error'] = $result['error'] ?? null;
            }
        }
    }

    private function handleFailedJobs(array $workerResults): void
    {
        foreach ($workerResults as $result) {
            if ($result['status'] === 'failed') {
                $jobId = $result['job_id'];
                $job = &$this->activeJobs[$jobId];
                
                $job['attempts']++;
                $job['last_error'] = $result['error'];
                
                if ($job['attempts'] < $job['max_retries']) {
                    // Retry with exponential backoff
                    $delay = $this->calculateRetryDelay($job['attempts']);
                    $job['scheduled_at'] = microtime(true) + $delay;
                    $job['status'] = 'queued';
                    
                    $this->jobQueue->addJob($job);
                } else {
                    // Max retries reached
                    $job['status'] = 'failed';
                }
            }
        }
    }

    private function calculateRetryDelay(int $attempt): int
    {
        // Exponential backoff with jitter
        $baseDelay = 2 ** $attempt; // 2, 4, 8, 16...
        $jitter = random_int(0, $baseDelay / 2);
        return $baseDelay + $jitter;
    }

    private function areDependenciesSatisfied(string $jobId, array $completedJobs): bool
    {
        if (!isset($this->activeJobs[$jobId])) {
            return false;
        }
        
        $dependencies = $this->activeJobs[$jobId]['dependencies'];
        
        foreach ($dependencies as $depJobId) {
            if (!isset($completedJobs[$depJobId]) || 
                $completedJobs[$depJobId]['status'] !== 'completed') {
                return false;
            }
        }
        
        return true;
    }

    private function executeJob(string $jobId): array
    {
        $job = $this->activeJobs[$jobId];
        $job['status'] = 'processing';
        $job['started_at'] = microtime(true);
        
        // Simulate job execution (in real implementation, this would call the actual job handler)
        $result = [
            'job_id' => $jobId,
            'status' => 'completed',
            'started_at' => $job['started_at'],
            'completed_at' => microtime(true),
            'result' => 'Job completed successfully'
        ];
        
        return $result;
    }

    private function addJobWithDependencies(array $job): void
    {
        // Add to dependency queue for later processing
        $this->jobQueue->addDependentJob($job);
    }

    private function validateJobDefinition(array $jobDef): void
    {
        if (!isset($jobDef['type'])) {
            throw new JobQueueOperationException("Job definition missing 'type' field");
        }
        
        if (!isset($jobDef['payload'])) {
            throw new JobQueueOperationException("Job definition missing 'payload' field");
        }
    }

    private function generateExecutionPlan(array $dependencies): array
    {
        // Topological sort to create execution plan
        $plan = [];
        $inDegree = [];
        $graph = [];
        
        // Build dependency graph
        foreach ($dependencies as $jobId => $deps) {
            $inDegree[$jobId] = count($deps);
            foreach ($deps as $depId) {
                if (!isset($graph[$depId])) {
                    $graph[$depId] = [];
                }
                $graph[$depId][] = $jobId;
            }
        }
        
        // Find jobs with no dependencies
        $queue = array_filter($inDegree, fn($degree) => $degree === 0, ARRAY_FILTER_USE_KEY);
        $phase = 0;
        
        while (!empty($queue)) {
            $plan[$phase] = array_keys($queue);
            $nextQueue = [];
            
            foreach (array_keys($queue) as $jobId) {
                if (isset($graph[$jobId])) {
                    foreach ($graph[$jobId] as $dependentJob) {
                        $inDegree[$dependentJob]--;
                        if ($inDegree[$dependentJob] === 0) {
                            $nextQueue[$dependentJob] = true;
                        }
                    }
                }
            }
            
            $queue = $nextQueue;
            $phase++;
        }
        
        return $plan;
    }

    private function waitForPhaseCompletion(array $phaseResults): void
    {
        // Wait for all jobs in phase to complete
        $maxWait = 300; // 5 minutes
        $startTime = microtime(true);
        
        while ((microtime(true) - $startTime) < $maxWait) {
            $allComplete = true;
            
            foreach ($phaseResults as $jobId => $result) {
                if ($result['status'] === 'processing') {
                    $allComplete = false;
                    break;
                }
            }
            
            if ($allComplete) {
                break;
            }
            
            usleep(100000); // 100ms
        }
    }

    private function calculateNextRun(string $cronExpression): int
    {
        // Parse cron expression and calculate next run time
        // This is a simplified implementation
        $parts = explode(' ', $cronExpression);
        if (count($parts) !== 5) {
            throw new JobQueueOperationException("Invalid cron expression format");
        }
        
        // For now, return next minute (simplified)
        return time() + 60;
    }

    private function isValidCronExpression(string $cronExpression): bool
    {
        $parts = explode(' ', $cronExpression);
        return count($parts) === 5;
    }

    private function estimateJobCompletion(array $job): ?int
    {
        if ($job['status'] !== 'processing') {
            return null;
        }
        
        // Estimate based on average processing time for job type
        $avgTime = $this->getAverageProcessingTime($job['type']);
        $elapsed = microtime(true) - ($job['started_at'] ?? microtime(true));
        
        return time() + max(0, $avgTime - $elapsed);
    }

    private function getAverageProcessingTime(string $jobType): int
    {
        // Calculate average processing time for job type
        $completedJobs = array_filter($this->activeJobs, fn($j) => 
            $j['type'] === $jobType && $j['status'] === 'completed'
        );
        
        if (empty($completedJobs)) {
            return 60; // Default 1 minute
        }
        
        $totalTime = 0;
        foreach ($completedJobs as $job) {
            $totalTime += ($job['completed_at'] - $job['started_at']);
        }
        
        return (int)($totalTime / count($completedJobs));
    }

    private function calculateThroughput(): float
    {
        // Calculate jobs processed per second
        $recentJobs = array_filter($this->activeJobs, fn($j) => 
            isset($j['completed_at']) && 
            ($j['completed_at'] > (microtime(true) - 3600)) // Last hour
        );
        
        return count($recentJobs) / 3600; // jobs per second
    }

    private function calculateAverageProcessingTime(): float
    {
        $completedJobs = array_filter($this->activeJobs, fn($j) => $j['status'] === 'completed');
        
        if (empty($completedJobs)) {
            return 0;
        }
        
        $totalTime = 0;
        foreach ($completedJobs as $job) {
            if (isset($job['started_at']) && isset($job['completed_at'])) {
                $totalTime += ($job['completed_at'] - $job['started_at']);
            }
        }
        
        return $totalTime / count($completedJobs);
    }

    private function requeueJob(array $job): void
    {
        $job['scheduled_at'] = microtime(true) + 30; // Retry in 30 seconds
        $this->jobQueue->addJob($job);
    }

    private function generateJobId(): string
    {
        return 'job_' . uniqid() . '_' . random_int(1000, 9999);
    }

    private function generateWorkflowId(): string
    {
        return 'workflow_' . uniqid() . '_' . random_int(1000, 9999);
    }

    private function generateScheduleId(): string
    {
        return 'schedule_' . uniqid() . '_' . random_int(1000, 9999);
    }

    private function initializeOperator(): void
    {
        $this->jobQueue->initialize();
        $this->priorityQueue->initialize();
        $this->workerPool->initialize($this->maxWorkers);
        
        $this->logOperation('job_queue_operator_initialized', '', [
            'max_workers' => $this->maxWorkers,
            'max_retries' => $this->maxRetries
        ]);
    }

    private function logOperation(string $operation, string $jobId, array $context = []): void
    {
        $logData = [
            'operation' => $operation,
            'job_id' => $jobId,
            'timestamp' => microtime(true),
            'context' => $context
        ];
        
        error_log("JobQueueOperator: " . json_encode($logData));
    }
} 