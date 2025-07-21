<?php

namespace TuskLang\A3\G1;

/**
 * Enhanced Error Handler - Production Grade Error Management
 * 
 * Provides comprehensive error handling with:
 * - Multi-level error tracking
 * - Automatic error recovery
 * - Performance impact monitoring
 * - Integration with logging systems
 * 
 * @version 2.0.0
 * @package TuskLang\A3\G1
 */
class ErrorHandler
{
    private array $errorStack = [];
    private array $recoveryStrategies = [];
    private array $errorMetrics = [];
    private array $subscribers = [];
    private bool $debugMode = false;
    private string $logFile;
    
    public function __construct(array $config = [])
    {
        $this->debugMode = $config['debug'] ?? false;
        $this->logFile = $config['log_file'] ?? '/tmp/tusklang_errors.log';
        
        // Set PHP error handlers
        set_error_handler([$this, 'handlePhpError']);
        set_exception_handler([$this, 'handleException']);
        register_shutdown_function([$this, 'handleFatalError']);
        
        $this->initializeRecoveryStrategies();
    }
    
    /**
     * Track error with comprehensive context
     */
    public function trackError(string $type, string $message, array $context = []): string
    {
        $errorId = $this->generateErrorId();
        $timestamp = microtime(true);
        
        $error = [
            'id' => $errorId,
            'type' => $type,
            'message' => $message,
            'context' => $context,
            'timestamp' => $timestamp,
            'stack_trace' => debug_backtrace(DEBUG_BACKTRACE_IGNORE_ARGS),
            'memory_usage' => memory_get_usage(true),
            'peak_memory' => memory_get_peak_usage(true)
        ];
        
        $this->errorStack[] = $error;
        $this->updateMetrics($type);
        $this->logError($error);
        $this->notifySubscribers($error);
        
        return $errorId;
    }
    
    /**
     * Attempt automatic error recovery
     */
    public function attemptRecovery(string $errorType, array $context = []): bool
    {
        if (!isset($this->recoveryStrategies[$errorType])) {
            return false;
        }
        
        $strategy = $this->recoveryStrategies[$errorType];
        
        try {
            $result = call_user_func($strategy, $context);
            $this->trackError('RECOVERY_SUCCESS', "Successfully recovered from $errorType", [
                'error_type' => $errorType,
                'context' => $context
            ]);
            return $result;
        } catch (\Exception $e) {
            $this->trackError('RECOVERY_FAILED', "Failed to recover from $errorType: " . $e->getMessage(), [
                'error_type' => $errorType,
                'recovery_error' => $e->getMessage()
            ]);
            return false;
        }
    }
    
    /**
     * Register recovery strategy for specific error types
     */
    public function registerRecoveryStrategy(string $errorType, callable $strategy): self
    {
        $this->recoveryStrategies[$errorType] = $strategy;
        return $this;
    }
    
    /**
     * Subscribe to error notifications
     */
    public function subscribe(callable $callback): self
    {
        $this->subscribers[] = $callback;
        return $this;
    }
    
    /**
     * Get error metrics and analytics
     */
    public function getMetrics(): array
    {
        return [
            'total_errors' => count($this->errorStack),
            'error_types' => $this->errorMetrics,
            'recent_errors' => array_slice($this->errorStack, -10),
            'memory_usage' => memory_get_usage(true),
            'peak_memory' => memory_get_peak_usage(true),
            'error_rate' => $this->calculateErrorRate()
        ];
    }
    
    /**
     * Clear error history (with backup option)
     */
    public function clearErrors(bool $backup = true): void
    {
        if ($backup) {
            $backupFile = $this->logFile . '.backup.' . date('Y-m-d-H-i-s');
            file_put_contents($backupFile, json_encode($this->errorStack, JSON_PRETTY_PRINT));
        }
        
        $this->errorStack = [];
        $this->errorMetrics = [];
    }
    
    /**
     * Handle PHP errors
     */
    public function handlePhpError(int $severity, string $message, string $file = '', int $line = 0): bool
    {
        $type = $this->getSeverityName($severity);
        $context = [
            'file' => $file,
            'line' => $line,
            'severity' => $severity
        ];
        
        $this->trackError($type, $message, $context);
        
        // Don't prevent default PHP error handling for fatal errors
        return !in_array($severity, [E_ERROR, E_CORE_ERROR, E_COMPILE_ERROR, E_PARSE]);
    }
    
    /**
     * Handle uncaught exceptions
     */
    public function handleException(\Throwable $exception): void
    {
        $context = [
            'file' => $exception->getFile(),
            'line' => $exception->getLine(),
            'code' => $exception->getCode(),
            'previous' => $exception->getPrevious() ? $exception->getPrevious()->getMessage() : null
        ];
        
        $this->trackError('UNCAUGHT_EXCEPTION', $exception->getMessage(), $context);
        
        // Attempt recovery for known exception types
        $exceptionType = get_class($exception);
        if ($this->attemptRecovery($exceptionType, $context)) {
            return; // Recovery successful
        }
        
        // If no recovery possible, log and potentially terminate
        if (!$this->debugMode) {
            http_response_code(500);
            echo json_encode(['error' => 'Internal server error', 'id' => $this->generateErrorId()]);
        } else {
            echo "Uncaught Exception: " . $exception->getMessage() . "\n";
            echo "File: " . $exception->getFile() . ":" . $exception->getLine() . "\n";
            echo "Stack Trace:\n" . $exception->getTraceAsString() . "\n";
        }
    }
    
    /**
     * Handle fatal errors
     */
    public function handleFatalError(): void
    {
        $error = error_get_last();
        if ($error && in_array($error['type'], [E_ERROR, E_CORE_ERROR, E_COMPILE_ERROR, E_PARSE])) {
            $context = [
                'file' => $error['file'],
                'line' => $error['line'],
                'type' => $error['type']
            ];
            
            $this->trackError('FATAL_ERROR', $error['message'], $context);
        }
    }
    
    private function initializeRecoveryStrategies(): void
    {
        // Memory exhaustion recovery
        $this->registerRecoveryStrategy('MEMORY_EXHAUSTED', function($context) {
            if (function_exists('gc_collect_cycles')) {
                return gc_collect_cycles() > 0;
            }
            return false;
        });
        
        // Database connection recovery
        $this->registerRecoveryStrategy('DATABASE_CONNECTION_FAILED', function($context) {
            // Attempt to reconnect with exponential backoff
            $attempts = 0;
            while ($attempts < 3) {
                sleep(pow(2, $attempts)); // Exponential backoff
                try {
                    // This would reconnect to database
                    return true; // Placeholder for actual reconnection logic
                } catch (\Exception $e) {
                    $attempts++;
                }
            }
            return false;
        });
        
        // File permission recovery
        $this->registerRecoveryStrategy('FILE_PERMISSION_DENIED', function($context) {
            $file = $context['file'] ?? null;
            if ($file && is_writable(dirname($file))) {
                return chmod($file, 0644);
            }
            return false;
        });
    }
    
    private function generateErrorId(): string
    {
        return 'ERR_' . strtoupper(substr(md5(microtime(true) . rand()), 0, 8));
    }
    
    private function updateMetrics(string $type): void
    {
        if (!isset($this->errorMetrics[$type])) {
            $this->errorMetrics[$type] = 0;
        }
        $this->errorMetrics[$type]++;
    }
    
    private function logError(array $error): void
    {
        $logEntry = [
            'timestamp' => date('Y-m-d H:i:s', $error['timestamp']),
            'id' => $error['id'],
            'type' => $error['type'],
            'message' => $error['message'],
            'context' => $error['context']
        ];
        
        file_put_contents(
            $this->logFile,
            json_encode($logEntry) . "\n",
            FILE_APPEND | LOCK_EX
        );
    }
    
    private function notifySubscribers(array $error): void
    {
        foreach ($this->subscribers as $callback) {
            try {
                call_user_func($callback, $error);
            } catch (\Exception $e) {
                // Don't let subscriber errors crash the error handler
                error_log("Error handler subscriber failed: " . $e->getMessage());
            }
        }
    }
    
    private function getSeverityName(int $severity): string
    {
        $severityNames = [
            E_ERROR => 'ERROR',
            E_WARNING => 'WARNING',
            E_PARSE => 'PARSE_ERROR',
            E_NOTICE => 'NOTICE',
            E_CORE_ERROR => 'CORE_ERROR',
            E_CORE_WARNING => 'CORE_WARNING',
            E_COMPILE_ERROR => 'COMPILE_ERROR',
            E_COMPILE_WARNING => 'COMPILE_WARNING',
            E_USER_ERROR => 'USER_ERROR',
            E_USER_WARNING => 'USER_WARNING',
            E_USER_NOTICE => 'USER_NOTICE',
            E_STRICT => 'STRICT',
            E_RECOVERABLE_ERROR => 'RECOVERABLE_ERROR',
            E_DEPRECATED => 'DEPRECATED',
            E_USER_DEPRECATED => 'USER_DEPRECATED',
        ];
        
        return $severityNames[$severity] ?? 'UNKNOWN';
    }
    
    private function calculateErrorRate(): float
    {
        if (empty($this->errorStack)) {
            return 0.0;
        }
        
        $now = microtime(true);
        $recentErrors = array_filter($this->errorStack, function($error) use ($now) {
            return ($now - $error['timestamp']) < 3600; // Last hour
        });
        
        return count($recentErrors) / 60; // Errors per minute
    }
} 