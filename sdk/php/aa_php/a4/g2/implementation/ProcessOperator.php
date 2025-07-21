<?php

declare(strict_types=1);

namespace TuskLang\SDK\SystemOperations\ProcessManagement;

use TuskLang\SDK\Core\BaseOperator;
use TuskLang\SDK\Core\Interfaces\OperatorInterface;
use TuskLang\SDK\Core\Exceptions\ProcessOperationException;
use TuskLang\SDK\SystemOperations\Health\ProcessHealthMonitor;
use TuskLang\SDK\SystemOperations\IPC\InterProcessCommunication;
use TuskLang\SDK\SystemOperations\Clustering\ProcessClusterManager;

/**
 * Advanced Process Management Operator with Health Monitoring
 * 
 * Features:
 * - Intelligent process management with health monitoring
 * - Real-time process resource tracking and optimization
 * - Process communication with IPC mechanisms
 * - Automatic process recovery and failure handling
 * - Process clustering and load distribution
 * 
 * @package TuskLang\SDK\SystemOperations\ProcessManagement
 * @version 1.0.0
 * @author TuskLang AI System
 */
class ProcessOperator extends BaseOperator implements OperatorInterface
{
    private ProcessHealthMonitor $healthMonitor;
    private InterProcessCommunication $ipc;
    private ProcessClusterManager $clusterManager;
    private array $managedProcesses = [];
    private array $processGroups = [];
    private int $maxProcesses = 100;
    private int $monitoringInterval = 5; // seconds

    public function __construct(array $config = [])
    {
        parent::__construct($config);
        
        $this->healthMonitor = new ProcessHealthMonitor($config['health_config'] ?? []);
        $this->ipc = new InterProcessCommunication($config['ipc_config'] ?? []);
        $this->clusterManager = new ProcessClusterManager($config['cluster_config'] ?? []);
        
        $this->maxProcesses = $config['max_processes'] ?? 100;
        $this->monitoringInterval = $config['monitoring_interval'] ?? 5;
        
        $this->initializeOperator();
    }

    /**
     * Start a new process with monitoring
     */
    public function startProcess(string $command, array $options = []): array
    {
        try {
            $this->validateCommand($command);
            
            if (count($this->managedProcesses) >= $this->maxProcesses) {
                throw new ProcessOperationException("Maximum number of processes ({$this->maxProcesses}) reached");
            }

            $processId = $this->generateProcessId();
            $workingDir = $options['working_dir'] ?? getcwd();
            $environment = $options['environment'] ?? $_ENV;
            $timeout = $options['timeout'] ?? null;
            
            // Prepare process descriptor
            $descriptorspec = [
                0 => ["pipe", "r"], // stdin
                1 => ["pipe", "w"], // stdout
                2 => ["pipe", "w"]  // stderr
            ];
            
            // Start the process
            $process = proc_open($command, $descriptorspec, $pipes, $workingDir, $environment);
            
            if (!is_resource($process)) {
                throw new ProcessOperationException("Failed to start process: {$command}");
            }
            
            $status = proc_get_status($process);
            $realPid = $status['pid'];
            
            // Store process information
            $this->managedProcesses[$processId] = [
                'id' => $processId,
                'pid' => $realPid,
                'command' => $command,
                'process' => $process,
                'pipes' => $pipes,
                'status' => 'running',
                'started' => microtime(true),
                'options' => $options,
                'timeout' => $timeout,
                'resources' => $this->getProcessResources($realPid),
                'health_score' => 100.0
            ];
            
            // Set up health monitoring
            if ($options['enable_monitoring'] ?? true) {
                $this->healthMonitor->addProcess($processId, $realPid, $options['health_checks'] ?? []);
            }
            
            // Add to process group if specified
            if (isset($options['group'])) {
                $this->addToProcessGroup($processId, $options['group']);
            }
            
            // Set up IPC if requested
            if ($options['enable_ipc'] ?? false) {
                $this->ipc->setupProcessIPC($processId, $realPid);
            }
            
            $this->logOperation('process_started', $processId, [
                'command' => $command,
                'pid' => $realPid,
                'group' => $options['group'] ?? null
            ]);
            
            return [
                'process_id' => $processId,
                'pid' => $realPid,
                'status' => 'running',
                'started' => $this->managedProcesses[$processId]['started']
            ];

        } catch (\Exception $e) {
            $this->logOperation('process_start_error', '', [
                'command' => $command,
                'error' => $e->getMessage()
            ]);
            throw new ProcessOperationException("Process start failed: " . $e->getMessage());
        }
    }

    /**
     * Stop a process gracefully or forcefully
     */
    public function stopProcess(string $processId, array $options = []): bool
    {
        try {
            if (!isset($this->managedProcesses[$processId])) {
                throw new ProcessOperationException("Process not found: {$processId}");
            }
            
            $processInfo = $this->managedProcesses[$processId];
            $graceful = $options['graceful'] ?? true;
            $timeout = $options['timeout'] ?? 30;
            
            if ($graceful) {
                // Try graceful shutdown first
                $success = $this->gracefulStop($processInfo, $timeout);
                if (!$success && ($options['force_after_timeout'] ?? true)) {
                    $success = $this->forceStop($processInfo);
                }
            } else {
                // Force stop immediately
                $success = $this->forceStop($processInfo);
            }
            
            if ($success) {
                // Clean up resources
                $this->cleanupProcess($processId);
                
                $this->logOperation('process_stopped', $processId, [
                    'graceful' => $graceful,
                    'pid' => $processInfo['pid']
                ]);
            }
            
            return $success;

        } catch (\Exception $e) {
            $this->logOperation('process_stop_error', $processId, ['error' => $e->getMessage()]);
            throw new ProcessOperationException("Process stop failed for {$processId}: " . $e->getMessage());
        }
    }

    /**
     * Restart a process with same parameters
     */
    public function restartProcess(string $processId, array $options = []): array
    {
        try {
            if (!isset($this->managedProcesses[$processId])) {
                throw new ProcessOperationException("Process not found: {$processId}");
            }
            
            $processInfo = $this->managedProcesses[$processId];
            $originalOptions = $processInfo['options'];
            
            // Stop the current process
            $this->stopProcess($processId, $options);
            
            // Start new process with same command and options
            $result = $this->startProcess($processInfo['command'], $originalOptions);
            
            $this->logOperation('process_restarted', $processId, [
                'old_pid' => $processInfo['pid'],
                'new_pid' => $result['pid']
            ]);
            
            return $result;

        } catch (\Exception $e) {
            $this->logOperation('process_restart_error', $processId, ['error' => $e->getMessage()]);
            throw new ProcessOperationException("Process restart failed for {$processId}: " . $e->getMessage());
        }
    }

    /**
     * Get comprehensive process information
     */
    public function getProcessInfo(string $processId): array
    {
        try {
            if (!isset($this->managedProcesses[$processId])) {
                throw new ProcessOperationException("Process not found: {$processId}");
            }
            
            $processInfo = $this->managedProcesses[$processId];
            $pid = $processInfo['pid'];
            
            // Get current status
            $status = proc_get_status($processInfo['process']);
            
            // Get updated resource usage
            $resources = $this->getProcessResources($pid);
            
            // Get health information
            $health = $this->healthMonitor->getProcessHealth($processId);
            
            // Get IPC status
            $ipcStatus = $this->ipc->getProcessIPCStatus($processId);
            
            return [
                'process_id' => $processId,
                'pid' => $pid,
                'command' => $processInfo['command'],
                'status' => $status['running'] ? 'running' : 'stopped',
                'exit_code' => $status['exitcode'],
                'started' => $processInfo['started'],
                'uptime' => microtime(true) - $processInfo['started'],
                'resources' => $resources,
                'health' => $health,
                'ipc_status' => $ipcStatus,
                'group' => $this->getProcessGroup($processId),
                'options' => $processInfo['options']
            ];

        } catch (\Exception $e) {
            $this->logOperation('process_info_error', $processId, ['error' => $e->getMessage()]);
            throw new ProcessOperationException("Failed to get process info for {$processId}: " . $e->getMessage());
        }
    }

    /**
     * List all managed processes
     */
    public function listProcesses(array $filters = []): array
    {
        try {
            $processes = [];
            
            foreach ($this->managedProcesses as $processId => $processInfo) {
                $info = $this->getProcessInfo($processId);
                
                // Apply filters
                if ($this->passesFilters($info, $filters)) {
                    $processes[] = $info;
                }
            }
            
            // Sort by specified criteria
            if (isset($filters['sort_by'])) {
                $this->sortProcesses($processes, $filters['sort_by'], $filters['sort_order'] ?? 'asc');
            }
            
            $this->logOperation('processes_listed', '', ['count' => count($processes)]);
            return $processes;

        } catch (\Exception $e) {
            $this->logOperation('process_list_error', '', ['error' => $e->getMessage()]);
            throw new ProcessOperationException("Failed to list processes: " . $e->getMessage());
        }
    }

    /**
     * Send signal to process
     */
    public function sendSignal(string $processId, int $signal): bool
    {
        try {
            if (!isset($this->managedProcesses[$processId])) {
                throw new ProcessOperationException("Process not found: {$processId}");
            }
            
            $processInfo = $this->managedProcesses[$processId];
            $pid = $processInfo['pid'];
            
            $success = posix_kill($pid, $signal);
            
            $this->logOperation('signal_sent', $processId, [
                'signal' => $signal,
                'signal_name' => $this->getSignalName($signal),
                'success' => $success
            ]);
            
            return $success;

        } catch (\Exception $e) {
            $this->logOperation('signal_error', $processId, [
                'signal' => $signal,
                'error' => $e->getMessage()
            ]);
            throw new ProcessOperationException("Failed to send signal {$signal} to process {$processId}: " . $e->getMessage());
        }
    }

    /**
     * Write to process stdin
     */
    public function writeToProcess(string $processId, string $data): int
    {
        try {
            if (!isset($this->managedProcesses[$processId])) {
                throw new ProcessOperationException("Process not found: {$processId}");
            }
            
            $processInfo = $this->managedProcesses[$processId];
            $stdin = $processInfo['pipes'][0];
            
            $bytesWritten = fwrite($stdin, $data);
            fflush($stdin);
            
            $this->logOperation('data_written_to_process', $processId, [
                'bytes_written' => $bytesWritten,
                'data_length' => strlen($data)
            ]);
            
            return $bytesWritten;

        } catch (\Exception $e) {
            $this->logOperation('process_write_error', $processId, ['error' => $e->getMessage()]);
            throw new ProcessOperationException("Failed to write to process {$processId}: " . $e->getMessage());
        }
    }

    /**
     * Read from process stdout/stderr
     */
    public function readFromProcess(string $processId, string $stream = 'stdout'): string
    {
        try {
            if (!isset($this->managedProcesses[$processId])) {
                throw new ProcessOperationException("Process not found: {$processId}");
            }
            
            $processInfo = $this->managedProcesses[$processId];
            $streamIndex = $stream === 'stdout' ? 1 : 2;
            $pipe = $processInfo['pipes'][$streamIndex];
            
            // Set non-blocking mode
            stream_set_blocking($pipe, false);
            
            $data = stream_get_contents($pipe);
            
            $this->logOperation('data_read_from_process', $processId, [
                'stream' => $stream,
                'bytes_read' => strlen($data)
            ]);
            
            return $data ?: '';

        } catch (\Exception $e) {
            $this->logOperation('process_read_error', $processId, [
                'stream' => $stream,
                'error' => $e->getMessage()
            ]);
            throw new ProcessOperationException("Failed to read from process {$processId}: " . $e->getMessage());
        }
    }

    /**
     * Create process group
     */
    public function createProcessGroup(string $groupName, array $options = []): bool
    {
        try {
            if (isset($this->processGroups[$groupName])) {
                throw new ProcessOperationException("Process group already exists: {$groupName}");
            }
            
            $this->processGroups[$groupName] = [
                'name' => $groupName,
                'processes' => [],
                'options' => $options,
                'created' => microtime(true)
            ];
            
            $this->logOperation('process_group_created', '', ['group' => $groupName]);
            return true;

        } catch (\Exception $e) {
            $this->logOperation('process_group_create_error', '', [
                'group' => $groupName,
                'error' => $e->getMessage()
            ]);
            throw new ProcessOperationException("Failed to create process group {$groupName}: " . $e->getMessage());
        }
    }

    /**
     * Stop all processes in a group
     */
    public function stopProcessGroup(string $groupName, array $options = []): array
    {
        try {
            if (!isset($this->processGroups[$groupName])) {
                throw new ProcessOperationException("Process group not found: {$groupName}");
            }
            
            $group = $this->processGroups[$groupName];
            $results = [];
            
            foreach ($group['processes'] as $processId) {
                try {
                    $success = $this->stopProcess($processId, $options);
                    $results[$processId] = ['success' => $success];
                } catch (\Exception $e) {
                    $results[$processId] = ['success' => false, 'error' => $e->getMessage()];
                }
            }
            
            $this->logOperation('process_group_stopped', '', [
                'group' => $groupName,
                'processes' => count($group['processes']),
                'results' => $results
            ]);
            
            return $results;

        } catch (\Exception $e) {
            $this->logOperation('process_group_stop_error', '', [
                'group' => $groupName,
                'error' => $e->getMessage()
            ]);
            throw new ProcessOperationException("Failed to stop process group {$groupName}: " . $e->getMessage());
        }
    }

    /**
     * Monitor processes and handle failures
     */
    public function monitorProcesses(): array
    {
        try {
            $monitoringResults = [];
            $currentTime = microtime(true);
            
            foreach ($this->managedProcesses as $processId => $processInfo) {
                $monitoringResult = $this->monitorSingleProcess($processId, $currentTime);
                $monitoringResults[$processId] = $monitoringResult;
                
                // Handle failures if auto-recovery is enabled
                if (!$monitoringResult['healthy'] && ($processInfo['options']['auto_recovery'] ?? false)) {
                    $this->handleProcessFailure($processId, $monitoringResult);
                }
            }
            
            $this->logOperation('processes_monitored', '', [
                'total_processes' => count($monitoringResults),
                'healthy_processes' => count(array_filter($monitoringResults, fn($r) => $r['healthy']))
            ]);
            
            return $monitoringResults;

        } catch (\Exception $e) {
            $this->logOperation('monitoring_error', '', ['error' => $e->getMessage()]);
            throw new ProcessOperationException("Process monitoring failed: " . $e->getMessage());
        }
    }

    /**
     * Optimize process resource allocation
     */
    public function optimizeProcesses(): array
    {
        try {
            $optimizations = [];
            
            foreach ($this->managedProcesses as $processId => $processInfo) {
                $optimization = $this->optimizeSingleProcess($processId);
                $optimizations[$processId] = $optimization;
            }
            
            // Cluster optimization
            $clusterOptimization = $this->clusterManager->optimizeLoadDistribution($this->managedProcesses);
            $optimizations['cluster'] = $clusterOptimization;
            
            $this->logOperation('processes_optimized', '', $optimizations);
            return $optimizations;

        } catch (\Exception $e) {
            $this->logOperation('optimization_error', '', ['error' => $e->getMessage()]);
            throw new ProcessOperationException("Process optimization failed: " . $e->getMessage());
        }
    }

    // Private helper methods

    private function gracefulStop(array $processInfo, int $timeout): bool
    {
        // Send SIGTERM for graceful shutdown
        posix_kill($processInfo['pid'], SIGTERM);
        
        $startTime = microtime(true);
        while (microtime(true) - $startTime < $timeout) {
            $status = proc_get_status($processInfo['process']);
            if (!$status['running']) {
                proc_close($processInfo['process']);
                return true;
            }
            usleep(100000); // 100ms
        }
        
        return false;
    }

    private function forceStop(array $processInfo): bool
    {
        // Send SIGKILL for immediate termination
        posix_kill($processInfo['pid'], SIGKILL);
        
        // Wait a moment and check
        usleep(500000); // 500ms
        $status = proc_get_status($processInfo['process']);
        
        if (!$status['running']) {
            proc_close($processInfo['process']);
            return true;
        }
        
        return false;
    }

    private function cleanupProcess(string $processId): void
    {
        if (isset($this->managedProcesses[$processId])) {
            $processInfo = $this->managedProcesses[$processId];
            
            // Close pipes
            foreach ($processInfo['pipes'] as $pipe) {
                if (is_resource($pipe)) {
                    fclose($pipe);
                }
            }
            
            // Remove from health monitoring
            $this->healthMonitor->removeProcess($processId);
            
            // Clean up IPC
            $this->ipc->cleanupProcessIPC($processId);
            
            // Remove from process groups
            foreach ($this->processGroups as &$group) {
                $group['processes'] = array_filter($group['processes'], fn($pid) => $pid !== $processId);
            }
            
            // Remove from managed processes
            unset($this->managedProcesses[$processId]);
        }
    }

    private function getProcessResources(int $pid): array
    {
        // Get process resource usage
        $statFile = "/proc/{$pid}/stat";
        $statusFile = "/proc/{$pid}/status";
        
        $resources = [
            'cpu_usage' => 0.0,
            'memory_usage' => 0,
            'memory_percent' => 0.0,
            'open_files' => 0,
            'threads' => 1
        ];
        
        // Read from /proc if available (Linux)
        if (file_exists($statFile)) {
            $statData = file_get_contents($statFile);
            $statParts = explode(' ', $statData);
            
            // Calculate CPU usage (simplified)
            $utime = (int)$statParts[13];
            $stime = (int)$statParts[14];
            $resources['cpu_time'] = $utime + $stime;
            
            // Memory usage (RSS in pages * page size)
            $rss = (int)$statParts[23];
            $pageSize = 4096; // Typical page size
            $resources['memory_usage'] = $rss * $pageSize;
        }
        
        if (file_exists($statusFile)) {
            $statusData = file_get_contents($statusFile);
            
            // Extract memory info
            if (preg_match('/VmRSS:\s+(\d+)\s+kB/', $statusData, $matches)) {
                $resources['memory_usage'] = (int)$matches[1] * 1024; // Convert KB to bytes
            }
            
            // Extract thread count
            if (preg_match('/Threads:\s+(\d+)/', $statusData, $matches)) {
                $resources['threads'] = (int)$matches[1];
            }
        }
        
        return $resources;
    }

    private function addToProcessGroup(string $processId, string $groupName): void
    {
        if (!isset($this->processGroups[$groupName])) {
            $this->createProcessGroup($groupName);
        }
        
        if (!in_array($processId, $this->processGroups[$groupName]['processes'])) {
            $this->processGroups[$groupName]['processes'][] = $processId;
        }
    }

    private function getProcessGroup(string $processId): ?string
    {
        foreach ($this->processGroups as $groupName => $group) {
            if (in_array($processId, $group['processes'])) {
                return $groupName;
            }
        }
        return null;
    }

    private function passesFilters(array $processInfo, array $filters): bool
    {
        foreach ($filters as $filter => $value) {
            switch ($filter) {
                case 'status':
                    if ($processInfo['status'] !== $value) {
                        return false;
                    }
                    break;
                case 'group':
                    if ($processInfo['group'] !== $value) {
                        return false;
                    }
                    break;
                case 'min_uptime':
                    if ($processInfo['uptime'] < $value) {
                        return false;
                    }
                    break;
                case 'max_memory':
                    if ($processInfo['resources']['memory_usage'] > $value) {
                        return false;
                    }
                    break;
            }
        }
        return true;
    }

    private function sortProcesses(array &$processes, string $sortBy, string $sortOrder): void
    {
        usort($processes, function($a, $b) use ($sortBy, $sortOrder) {
            $valueA = $this->getNestedValue($a, $sortBy);
            $valueB = $this->getNestedValue($b, $sortBy);
            
            $comparison = 0;
            if (is_numeric($valueA) && is_numeric($valueB)) {
                $comparison = $valueA <=> $valueB;
            } else {
                $comparison = strcasecmp($valueA, $valueB);
            }
            
            return $sortOrder === 'desc' ? -$comparison : $comparison;
        });
    }

    private function getNestedValue(array $array, string $key)
    {
        $keys = explode('.', $key);
        $value = $array;
        
        foreach ($keys as $k) {
            if (isset($value[$k])) {
                $value = $value[$k];
            } else {
                return null;
            }
        }
        
        return $value;
    }

    private function monitorSingleProcess(string $processId, float $currentTime): array
    {
        $processInfo = $this->managedProcesses[$processId];
        $pid = $processInfo['pid'];
        
        // Check if process is still running
        $status = proc_get_status($processInfo['process']);
        $isRunning = $status['running'];
        
        // Get current resources
        $resources = $this->getProcessResources($pid);
        
        // Get health score
        $healthScore = $this->healthMonitor->calculateHealthScore($processId);
        
        // Check timeout
        $timeout = $processInfo['timeout'];
        $timedOut = $timeout && ($currentTime - $processInfo['started']) > $timeout;
        
        // Update stored info
        $this->managedProcesses[$processId]['resources'] = $resources;
        $this->managedProcesses[$processId]['health_score'] = $healthScore;
        $this->managedProcesses[$processId]['status'] = $isRunning ? 'running' : 'stopped';
        
        return [
            'process_id' => $processId,
            'healthy' => $isRunning && $healthScore > 50 && !$timedOut,
            'running' => $isRunning,
            'health_score' => $healthScore,
            'timed_out' => $timedOut,
            'resources' => $resources,
            'issues' => $this->identifyProcessIssues($processInfo, $resources, $healthScore, $timedOut)
        ];
    }

    private function identifyProcessIssues(array $processInfo, array $resources, float $healthScore, bool $timedOut): array
    {
        $issues = [];
        
        if ($healthScore < 50) {
            $issues[] = 'low_health_score';
        }
        
        if ($timedOut) {
            $issues[] = 'timeout_exceeded';
        }
        
        // Check memory usage
        $memoryLimit = $processInfo['options']['memory_limit'] ?? (256 * 1024 * 1024); // 256MB default
        if ($resources['memory_usage'] > $memoryLimit) {
            $issues[] = 'memory_limit_exceeded';
        }
        
        // Check thread count
        $threadLimit = $processInfo['options']['thread_limit'] ?? 100;
        if ($resources['threads'] > $threadLimit) {
            $issues[] = 'thread_limit_exceeded';
        }
        
        return $issues;
    }

    private function handleProcessFailure(string $processId, array $monitoringResult): void
    {
        $processInfo = $this->managedProcesses[$processId];
        $recoveryOptions = $processInfo['options']['recovery_options'] ?? [];
        
        $this->logOperation('process_failure_detected', $processId, [
            'issues' => $monitoringResult['issues'],
            'health_score' => $monitoringResult['health_score']
        ]);
        
        // Determine recovery action
        $recoveryAction = $recoveryOptions['action'] ?? 'restart';
        $maxRetries = $recoveryOptions['max_retries'] ?? 3;
        $retryCount = $processInfo['retry_count'] ?? 0;
        
        if ($retryCount < $maxRetries) {
            switch ($recoveryAction) {
                case 'restart':
                    $this->restartProcess($processId);
                    $this->managedProcesses[$processId]['retry_count'] = $retryCount + 1;
                    break;
                    
                case 'recreate':
                    $this->stopProcess($processId, ['force' => true]);
                    $this->startProcess($processInfo['command'], $processInfo['options']);
                    break;
                    
                case 'alert_only':
                    // Just log and continue monitoring
                    break;
            }
        } else {
            $this->logOperation('process_recovery_abandoned', $processId, [
                'max_retries_reached' => $maxRetries
            ]);
        }
    }

    private function optimizeSingleProcess(string $processId): array
    {
        $processInfo = $this->managedProcesses[$processId];
        $resources = $processInfo['resources'];
        $optimizations = [];
        
        // Memory optimization
        if ($resources['memory_usage'] > 100 * 1024 * 1024) { // 100MB
            $optimizations['memory'] = 'consider_memory_optimization';
        }
        
        // CPU optimization
        if (isset($resources['cpu_time'])) {
            $optimizations['cpu'] = 'monitor_cpu_usage';
        }
        
        // Thread optimization
        if ($resources['threads'] > 50) {
            $optimizations['threads'] = 'consider_thread_reduction';
        }
        
        return $optimizations;
    }

    private function getSignalName(int $signal): string
    {
        $signals = [
            SIGTERM => 'SIGTERM',
            SIGKILL => 'SIGKILL',
            SIGINT => 'SIGINT',
            SIGUSR1 => 'SIGUSR1',
            SIGUSR2 => 'SIGUSR2',
            SIGHUP => 'SIGHUP'
        ];
        
        return $signals[$signal] ?? "SIGNAL_{$signal}";
    }

    private function generateProcessId(): string
    {
        return 'proc_' . uniqid() . '_' . random_int(1000, 9999);
    }

    private function validateCommand(string $command): void
    {
        if (empty($command)) {
            throw new ProcessOperationException("Command cannot be empty");
        }
        
        // Basic command injection protection
        $dangerousChars = ['|', '&', ';', '$(', '`'];
        foreach ($dangerousChars as $char) {
            if (strpos($command, $char) !== false) {
                throw new ProcessOperationException("Potentially dangerous command detected: {$command}");
            }
        }
    }

    private function initializeOperator(): void
    {
        // Start monitoring thread if supported
        $this->healthMonitor->initialize();
        $this->ipc->initialize();
        $this->clusterManager->initialize();
        
        // Set up periodic monitoring
        if (function_exists('pcntl_signal')) {
            pcntl_signal(SIGALRM, [$this, 'monitorProcesses']);
            pcntl_alarm($this->monitoringInterval);
        }
        
        $this->logOperation('process_operator_initialized', '', [
            'max_processes' => $this->maxProcesses,
            'monitoring_interval' => $this->monitoringInterval
        ]);
    }

    private function logOperation(string $operation, string $processId, array $context = []): void
    {
        // Log operation for monitoring and debugging
        $logData = [
            'operation' => $operation,
            'process_id' => $processId,
            'timestamp' => microtime(true),
            'context' => $context
        ];
        
        // Would integrate with logging system
        error_log("ProcessOperator: " . json_encode($logData));
    }
} 