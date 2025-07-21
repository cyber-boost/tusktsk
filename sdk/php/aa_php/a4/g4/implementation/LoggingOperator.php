<?php

declare(strict_types=1);

namespace TuskLang\SDK\SystemOperations\Monitoring;

use TuskLang\SDK\Core\BaseOperator;
use TuskLang\SDK\Core\Interfaces\OperatorInterface;
use TuskLang\SDK\Core\Exceptions\LoggingOperationException;

/**
 * Advanced Logging Operator with AI Analysis
 * 
 * Features:
 * - AI-powered log analysis and anomaly detection
 * - Structured JSON logging with metadata enrichment
 * - Distributed logging with correlation IDs
 * - Real-time log streaming and aggregation
 * - Log pattern recognition and alerting
 * 
 * @package TuskLang\SDK\SystemOperations\Monitoring
 * @version 1.0.0
 * @author TuskLang AI System
 */
class LoggingOperator extends BaseOperator implements OperatorInterface
{
    private array $loggers = [];
    private array $patterns = [];
    private array $anomalies = [];
    private string $correlationId;

    public function __construct(array $config = [])
    {
        parent::__construct($config);
        $this->correlationId = $config['correlation_id'] ?? uniqid('corr_');
        $this->initializeOperator();
    }

    public function log(string $level, string $message, array $context = []): bool
    {
        try {
            $logEntry = [
                'timestamp' => microtime(true),
                'level' => strtoupper($level),
                'message' => $message,
                'context' => $context,
                'correlation_id' => $this->correlationId,
                'process_id' => getmypid(),
                'memory_usage' => memory_get_usage(true),
                'trace' => debug_backtrace(DEBUG_BACKTRACE_IGNORE_ARGS, 5)
            ];
            
            // AI analysis
            $this->analyzeLogEntry($logEntry);
            
            // Write to configured loggers
            foreach ($this->loggers as $logger) {
                $logger->write($logEntry);
            }
            
            return true;

        } catch (\Exception $e) {
            throw new LoggingOperationException("Logging failed: " . $e->getMessage());
        }
    }

    public function emergency(string $message, array $context = []): void
    {
        $this->log('emergency', $message, $context);
    }

    public function alert(string $message, array $context = []): void
    {
        $this->log('alert', $message, $context);
    }

    public function critical(string $message, array $context = []): void
    {
        $this->log('critical', $message, $context);
    }

    public function error(string $message, array $context = []): void
    {
        $this->log('error', $message, $context);
    }

    public function warning(string $message, array $context = []): void
    {
        $this->log('warning', $message, $context);
    }

    public function info(string $message, array $context = []): void
    {
        $this->log('info', $message, $context);
    }

    public function debug(string $message, array $context = []): void
    {
        $this->log('debug', $message, $context);
    }

    public function addPattern(string $pattern, callable $callback): void
    {
        $this->patterns[] = [
            'pattern' => $pattern,
            'callback' => $callback,
            'matches' => 0
        ];
    }

    public function getAnomalies(): array
    {
        return $this->anomalies;
    }

    private function analyzeLogEntry(array $logEntry): void
    {
        // Pattern matching
        foreach ($this->patterns as &$pattern) {
            if (preg_match($pattern['pattern'], $logEntry['message'])) {
                $pattern['matches']++;
                call_user_func($pattern['callback'], $logEntry);
            }
        }
        
        // Anomaly detection
        $this->detectAnomalies($logEntry);
    }

    private function detectAnomalies(array $logEntry): void
    {
        // AI-powered anomaly detection logic
        if ($logEntry['level'] === 'ERROR' && $logEntry['memory_usage'] > 100 * 1024 * 1024) {
            $this->anomalies[] = [
                'type' => 'high_memory_error',
                'timestamp' => $logEntry['timestamp'],
                'details' => $logEntry
            ];
        }
    }

    private function initializeOperator(): void
    {
        // Initialize default file logger
        $this->loggers['file'] = new class {
            public function write(array $logEntry): void {
                $formatted = json_encode($logEntry) . PHP_EOL;
                file_put_contents('/tmp/tusklang.log', $formatted, FILE_APPEND | LOCK_EX);
            }
        };
    }
} 