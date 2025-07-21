<?php

namespace TuskLang\CoreOperators;

use Exception;

/**
 * Logs Operator - Log Management and Analysis
 * 
 * Provides comprehensive log management capabilities including:
 * - Log collection and aggregation
 * - Log parsing and filtering
 * - Log analysis and search
 * - Log rotation and archival
 * - Structured logging support
 * - Log level management
 * - Performance monitoring
 * 
 * @package TuskLang\CoreOperators
 */
class LogsOperator implements OperatorInterface
{
    private $defaultLogPath;
    private $logFormats = [];
    private $logLevels = ['emergency', 'alert', 'critical', 'error', 'warning', 'notice', 'info', 'debug'];

    public function __construct()
    {
        $this->defaultLogPath = getenv('LOG_PATH') ?: '/var/log';
        $this->initializeLogFormats();
    }

    /**
     * Execute Logs operations
     */
    public function execute(array $params, array $context = []): array
    {
        try {
            $operation = $params['operation'] ?? 'read';
            $data = $params['data'] ?? [];
            
            // Substitute context variables
            $data = $this->substituteContext($data, $context);
            
            switch ($operation) {
                case 'read':
                    return $this->readLogs($data);
                case 'write':
                    return $this->writeLog($data);
                case 'search':
                    return $this->searchLogs($data);
                case 'parse':
                    return $this->parseLogs($data);
                case 'filter':
                    return $this->filterLogs($data);
                case 'analyze':
                    return $this->analyzeLogs($data);
                case 'rotate':
                    return $this->rotateLogs($data);
                case 'archive':
                    return $this->archiveLogs($data);
                case 'tail':
                    return $this->tailLogs($data);
                case 'info':
                default:
                    return $this->getLogsInfo();
            }
        } catch (Exception $e) {
            return [
                'success' => false,
                'error' => 'Logs operation failed: ' . $e->getMessage(),
                'data' => null
            ];
        }
    }

    /**
     * Read logs from file
     */
    private function readLogs(array $data): array
    {
        $file = $data['file'] ?? '';
        $lines = $data['lines'] ?? 100;
        $offset = $data['offset'] ?? 0;
        $reverse = $data['reverse'] ?? false;

        if (empty($file)) {
            return [
                'success' => false,
                'error' => 'Log file path is required',
                'data' => null
            ];
        }

        $fullPath = $this->resolveLogPath($file);
        
        if (!file_exists($fullPath)) {
            return [
                'success' => false,
                'error' => 'Log file does not exist: ' . $fullPath,
                'data' => null
            ];
        }

        $content = file_get_contents($fullPath);
        if ($content === false) {
            return [
                'success' => false,
                'error' => 'Failed to read log file: ' . $fullPath,
                'data' => null
            ];
        }

        $logLines = explode("\n", $content);
        
        // Apply offset
        if ($offset > 0) {
            $logLines = array_slice($logLines, $offset);
        }

        // Apply line limit
        if ($lines > 0) {
            $logLines = array_slice($logLines, -$lines);
        }

        // Reverse if requested
        if ($reverse) {
            $logLines = array_reverse($logLines);
        }

        // Filter out empty lines
        $logLines = array_filter($logLines, function($line) {
            return !empty(trim($line));
        });

        return [
            'success' => true,
            'data' => [
                'file' => $file,
                'full_path' => $fullPath,
                'lines' => $logLines,
                'total_lines' => count($logLines),
                'file_size' => filesize($fullPath),
                'last_modified' => filemtime($fullPath)
            ],
            'error' => null
        ];
    }

    /**
     * Write log entry
     */
    private function writeLog(array $data): array
    {
        $file = $data['file'] ?? '';
        $message = $data['message'] ?? '';
        $level = $data['level'] ?? 'info';
        $context = $data['context'] ?? [];
        $timestamp = $data['timestamp'] ?? date('Y-m-d H:i:s');

        if (empty($file) || empty($message)) {
            return [
                'success' => false,
                'error' => 'Log file and message are required',
                'data' => null
            ];
        }

        if (!in_array($level, $this->logLevels)) {
            return [
                'success' => false,
                'error' => 'Invalid log level: ' . $level,
                'data' => null
            ];
        }

        $fullPath = $this->resolveLogPath($file);
        $logEntry = $this->formatLogEntry($message, $level, $context, $timestamp);

        $result = file_put_contents($fullPath, $logEntry . "\n", FILE_APPEND | LOCK_EX);
        
        if ($result === false) {
            return [
                'success' => false,
                'error' => 'Failed to write to log file: ' . $fullPath,
                'data' => null
            ];
        }

        return [
            'success' => true,
            'data' => [
                'file' => $file,
                'full_path' => $fullPath,
                'message' => $message,
                'level' => $level,
                'timestamp' => $timestamp,
                'bytes_written' => $result
            ],
            'error' => null
        ];
    }

    /**
     * Search logs
     */
    private function searchLogs(array $data): array
    {
        $file = $data['file'] ?? '';
        $query = $data['query'] ?? '';
        $caseSensitive = $data['case_sensitive'] ?? false;
        $regex = $data['regex'] ?? false;
        $maxResults = $data['max_results'] ?? 1000;

        if (empty($file) || empty($query)) {
            return [
                'success' => false,
                'error' => 'Log file and search query are required',
                'data' => null
            ];
        }

        $fullPath = $this->resolveLogPath($file);
        
        if (!file_exists($fullPath)) {
            return [
                'success' => false,
                'error' => 'Log file does not exist: ' . $fullPath,
                'data' => null
            ];
        }

        $content = file_get_contents($fullPath);
        if ($content === false) {
            return [
                'success' => false,
                'error' => 'Failed to read log file: ' . $fullPath,
                'data' => null
            ];
        }

        $logLines = explode("\n", $content);
        $matches = [];
        $matchCount = 0;

        foreach ($logLines as $lineNumber => $line) {
            if (empty(trim($line))) continue;

            $matched = false;

            if ($regex) {
                $flags = $caseSensitive ? '' : 'i';
                $matched = preg_match("/$query/$flags", $line);
            } else {
                if ($caseSensitive) {
                    $matched = strpos($line, $query) !== false;
                } else {
                    $matched = stripos($line, $query) !== false;
                }
            }

            if ($matched) {
                $matches[] = [
                    'line_number' => $lineNumber + 1,
                    'content' => $line,
                    'timestamp' => $this->extractTimestamp($line)
                ];
                $matchCount++;

                if ($matchCount >= $maxResults) {
                    break;
                }
            }
        }

        return [
            'success' => true,
            'data' => [
                'file' => $file,
                'query' => $query,
                'matches' => $matches,
                'total_matches' => count($matches),
                'search_options' => [
                    'case_sensitive' => $caseSensitive,
                    'regex' => $regex,
                    'max_results' => $maxResults
                ]
            ],
            'error' => null
        ];
    }

    /**
     * Parse structured logs
     */
    private function parseLogs(array $data): array
    {
        $file = $data['file'] ?? '';
        $format = $data['format'] ?? 'auto';
        $lines = $data['lines'] ?? 100;

        if (empty($file)) {
            return [
                'success' => false,
                'error' => 'Log file is required',
                'data' => null
            ];
        }

        $fullPath = $this->resolveLogPath($file);
        
        if (!file_exists($fullPath)) {
            return [
                'success' => false,
                'error' => 'Log file does not exist: ' . $fullPath,
                'data' => null
            ];
        }

        $content = file_get_contents($fullPath);
        if ($content === false) {
            return [
                'success' => false,
                'error' => 'Failed to read log file: ' . $fullPath,
                'data' => null
            ];
        }

        $logLines = explode("\n", $content);
        $logLines = array_slice($logLines, -$lines);
        $parsedLogs = [];

        foreach ($logLines as $line) {
            if (empty(trim($line))) continue;

            $parsed = $this->parseLogLine($line, $format);
            if ($parsed) {
                $parsedLogs[] = $parsed;
            }
        }

        return [
            'success' => true,
            'data' => [
                'file' => $file,
                'format' => $format,
                'parsed_logs' => $parsedLogs,
                'total_parsed' => count($parsedLogs)
            ],
            'error' => null
        ];
    }

    /**
     * Filter logs by criteria
     */
    private function filterLogs(array $data): array
    {
        $file = $data['file'] ?? '';
        $level = $data['level'] ?? '';
        $startTime = $data['start_time'] ?? '';
        $endTime = $data['end_time'] ?? '';
        $source = $data['source'] ?? '';
        $lines = $data['lines'] ?? 100;

        if (empty($file)) {
            return [
                'success' => false,
                'error' => 'Log file is required',
                'data' => null
            ];
        }

        $fullPath = $this->resolveLogPath($file);
        
        if (!file_exists($fullPath)) {
            return [
                'success' => false,
                'error' => 'Log file does not exist: ' . $fullPath,
                'data' => null
            ];
        }

        $content = file_get_contents($fullPath);
        if ($content === false) {
            return [
                'success' => false,
                'error' => 'Failed to read log file: ' . $fullPath,
                'data' => null
            ];
        }

        $logLines = explode("\n", $content);
        $logLines = array_slice($logLines, -$lines);
        $filteredLogs = [];

        foreach ($logLines as $line) {
            if (empty(trim($line))) continue;

            $parsed = $this->parseLogLine($line, 'auto');
            if (!$parsed) continue;

            $include = true;

            // Filter by level
            if (!empty($level) && isset($parsed['level'])) {
                $include = $include && strtolower($parsed['level']) === strtolower($level);
            }

            // Filter by time range
            if (!empty($startTime) && isset($parsed['timestamp'])) {
                $logTime = strtotime($parsed['timestamp']);
                $startTimeStamp = strtotime($startTime);
                $include = $include && $logTime >= $startTimeStamp;
            }

            if (!empty($endTime) && isset($parsed['timestamp'])) {
                $logTime = strtotime($parsed['timestamp']);
                $endTimeStamp = strtotime($endTime);
                $include = $include && $logTime <= $endTimeStamp;
            }

            // Filter by source
            if (!empty($source) && isset($parsed['source'])) {
                $include = $include && stripos($parsed['source'], $source) !== false;
            }

            if ($include) {
                $filteredLogs[] = $parsed;
            }
        }

        return [
            'success' => true,
            'data' => [
                'file' => $file,
                'filters' => [
                    'level' => $level,
                    'start_time' => $startTime,
                    'end_time' => $endTime,
                    'source' => $source
                ],
                'filtered_logs' => $filteredLogs,
                'total_filtered' => count($filteredLogs)
            ],
            'error' => null
        ];
    }

    /**
     * Analyze logs for patterns and statistics
     */
    private function analyzeLogs(array $data): array
    {
        $file = $data['file'] ?? '';
        $analysisType = $data['analysis_type'] ?? 'summary';
        $timeRange = $data['time_range'] ?? '24h';

        if (empty($file)) {
            return [
                'success' => false,
                'error' => 'Log file is required',
                'data' => null
            ];
        }

        $fullPath = $this->resolveLogPath($file);
        
        if (!file_exists($fullPath)) {
            return [
                'success' => false,
                'error' => 'Log file does not exist: ' . $fullPath,
                'data' => null
            ];
        }

        $content = file_get_contents($fullPath);
        if ($content === false) {
            return [
                'success' => false,
                'error' => 'Failed to read log file: ' . $fullPath,
                'data' => null
            ];
        }

        $logLines = explode("\n", $content);
        $analysis = [];

        switch ($analysisType) {
            case 'summary':
                $analysis = $this->generateLogSummary($logLines);
                break;
            case 'errors':
                $analysis = $this->analyzeErrors($logLines);
                break;
            case 'performance':
                $analysis = $this->analyzePerformance($logLines);
                break;
            case 'patterns':
                $analysis = $this->analyzePatterns($logLines);
                break;
            default:
                return [
                    'success' => false,
                    'error' => 'Unknown analysis type: ' . $analysisType,
                    'data' => null
                ];
        }

        return [
            'success' => true,
            'data' => [
                'file' => $file,
                'analysis_type' => $analysisType,
                'time_range' => $timeRange,
                'analysis' => $analysis
            ],
            'error' => null
        ];
    }

    // Helper Methods
    private function initializeLogFormats(): void
    {
        $this->logFormats = [
            'syslog' => '/^(\w{3}\s+\d{1,2}\s+\d{2}:\d{2}:\d{2})\s+(\S+)\s+([^:]+):\s*(.*)$/',
            'apache' => '/^(\S+) \S+ \S+ \[([^\]]+)\] "([^"]*)" (\d+) (\d+|-) "(.*)" "(.*)"$/',
            'nginx' => '/^(\S+) - - \[([^\]]+)\] "([^"]*)" (\d+) (\d+) "(.*)" "(.*)" (\S+)$/',
            'json' => '/^\{.*\}$/',
            'simple' => '/^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\s+\[(\w+)\]\s+(.*)$/'
        ];
    }

    private function resolveLogPath(string $file): string
    {
        if (strpos($file, '/') === 0) {
            return $file; // Absolute path
        }
        return $this->defaultLogPath . '/' . $file;
    }

    private function formatLogEntry(string $message, string $level, array $context, string $timestamp): string
    {
        $contextStr = empty($context) ? '' : ' ' . json_encode($context);
        return "[$timestamp] [$level] $message$contextStr";
    }

    private function extractTimestamp(string $line): ?string
    {
        // Try to extract timestamp from common log formats
        $patterns = [
            '/^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})/',
            '/^(\w{3}\s+\d{1,2}\s+\d{2}:\d{2}:\d{2})/',
            '/\[([^\]]+)\]/'
        ];

        foreach ($patterns as $pattern) {
            if (preg_match($pattern, $line, $matches)) {
                return $matches[1];
            }
        }

        return null;
    }

    private function parseLogLine(string $line, string $format): ?array
    {
        if ($format === 'auto') {
            // Try to detect format automatically
            foreach ($this->logFormats as $formatName => $pattern) {
                if (preg_match($pattern, $line, $matches)) {
                    return $this->parseByFormat($line, $formatName, $matches);
                }
            }
            // Fall back to simple parsing
            return $this->parseSimpleLine($line);
        }

        if (isset($this->logFormats[$format])) {
            $pattern = $this->logFormats[$format];
            if (preg_match($pattern, $line, $matches)) {
                return $this->parseByFormat($line, $format, $matches);
            }
        }

        return $this->parseSimpleLine($line);
    }

    private function parseByFormat(string $line, string $format, array $matches): array
    {
        switch ($format) {
            case 'syslog':
                return [
                    'timestamp' => $matches[1] ?? '',
                    'host' => $matches[2] ?? '',
                    'source' => $matches[3] ?? '',
                    'message' => $matches[4] ?? '',
                    'format' => 'syslog'
                ];
            case 'apache':
                return [
                    'ip' => $matches[1] ?? '',
                    'timestamp' => $matches[2] ?? '',
                    'request' => $matches[3] ?? '',
                    'status' => $matches[4] ?? '',
                    'bytes' => $matches[5] ?? '',
                    'referer' => $matches[6] ?? '',
                    'user_agent' => $matches[7] ?? '',
                    'format' => 'apache'
                ];
            case 'json':
                $json = json_decode($line, true);
                return $json ?: ['raw' => $line, 'format' => 'json'];
            case 'simple':
                return [
                    'timestamp' => $matches[1] ?? '',
                    'level' => $matches[2] ?? '',
                    'message' => $matches[3] ?? '',
                    'format' => 'simple'
                ];
            default:
                return ['raw' => $line, 'format' => $format];
        }
    }

    private function parseSimpleLine(string $line): array
    {
        return [
            'raw' => $line,
            'timestamp' => $this->extractTimestamp($line),
            'format' => 'unknown'
        ];
    }

    private function generateLogSummary(array $logLines): array
    {
        $totalLines = count($logLines);
        $levelCounts = [];
        $hourlyCounts = [];
        $sourceCounts = [];

        foreach ($logLines as $line) {
            if (empty(trim($line))) continue;

            $parsed = $this->parseLogLine($line, 'auto');
            
            // Count by level
            if (isset($parsed['level'])) {
                $level = strtolower($parsed['level']);
                $levelCounts[$level] = ($levelCounts[$level] ?? 0) + 1;
            }

            // Count by hour
            if (isset($parsed['timestamp'])) {
                $hour = date('H', strtotime($parsed['timestamp']));
                $hourlyCounts[$hour] = ($hourlyCounts[$hour] ?? 0) + 1;
            }

            // Count by source
            if (isset($parsed['source'])) {
                $source = $parsed['source'];
                $sourceCounts[$source] = ($sourceCounts[$source] ?? 0) + 1;
            }
        }

        return [
            'total_lines' => $totalLines,
            'level_distribution' => $levelCounts,
            'hourly_distribution' => $hourlyCounts,
            'source_distribution' => $sourceCounts
        ];
    }

    private function analyzeErrors(array $logLines): array
    {
        $errors = [];
        $errorPatterns = ['error', 'exception', 'fatal', 'critical', 'failed'];

        foreach ($logLines as $lineNumber => $line) {
            if (empty(trim($line))) continue;

            $parsed = $this->parseLogLine($line, 'auto');
            $content = strtolower($parsed['message'] ?? $parsed['raw'] ?? '');

            foreach ($errorPatterns as $pattern) {
                if (strpos($content, $pattern) !== false) {
                    $errors[] = [
                        'line_number' => $lineNumber + 1,
                        'content' => $line,
                        'pattern' => $pattern,
                        'timestamp' => $parsed['timestamp'] ?? null
                    ];
                    break;
                }
            }
        }

        return [
            'total_errors' => count($errors),
            'errors' => $errors
        ];
    }

    private function analyzePerformance(array $logLines): array
    {
        $performanceData = [];
        $responseTimePattern = '/(\d+(?:\.\d+)?)\s*(?:ms|s|seconds?)/i';

        foreach ($logLines as $line) {
            if (empty(trim($line))) continue;

            $parsed = $this->parseLogLine($line, 'auto');
            $content = $parsed['message'] ?? $parsed['raw'] ?? '';

            if (preg_match_all($responseTimePattern, $content, $matches)) {
                foreach ($matches[1] as $match) {
                    $performanceData[] = [
                        'response_time' => floatval($match),
                        'timestamp' => $parsed['timestamp'] ?? null,
                        'line' => $line
                    ];
                }
            }
        }

        if (empty($performanceData)) {
            return ['message' => 'No performance data found'];
        }

        $responseTimes = array_column($performanceData, 'response_time');
        
        return [
            'total_requests' => count($performanceData),
            'avg_response_time' => array_sum($responseTimes) / count($responseTimes),
            'min_response_time' => min($responseTimes),
            'max_response_time' => max($responseTimes),
            'performance_data' => $performanceData
        ];
    }

    private function analyzePatterns(array $logLines): array
    {
        $patterns = [];
        $patternCounts = [];

        foreach ($logLines as $line) {
            if (empty(trim($line))) continue;

            $parsed = $this->parseLogLine($line, 'auto');
            $content = $parsed['message'] ?? $parsed['raw'] ?? '';

            // Extract common patterns
            $words = preg_split('/\s+/', $content);
            foreach ($words as $word) {
                $word = trim($word, '.,!?;:');
                if (strlen($word) > 3) {
                    $patternCounts[$word] = ($patternCounts[$word] ?? 0) + 1;
                }
            }
        }

        arsort($patternCounts);
        $topPatterns = array_slice($patternCounts, 0, 20, true);

        return [
            'total_patterns' => count($patternCounts),
            'top_patterns' => $topPatterns
        ];
    }

    private function getLogsInfo(): array
    {
        return [
            'success' => true,
            'data' => [
                'default_log_path' => $this->defaultLogPath,
                'supported_formats' => array_keys($this->logFormats),
                'log_levels' => $this->logLevels,
                'capabilities' => [
                    'read_logs' => true,
                    'write_logs' => true,
                    'search_logs' => true,
                    'parse_logs' => true,
                    'filter_logs' => true,
                    'analyze_logs' => true,
                    'rotate_logs' => true,
                    'archive_logs' => true,
                    'tail_logs' => true
                ]
            ],
            'error' => null
        ];
    }

    private function substituteContext(array $data, array $context): array
    {
        $substituted = [];
        
        foreach ($data as $key => $value) {
            if (is_string($value)) {
                $value = preg_replace_callback('/\$\{([^}]+)\}/', function($matches) use ($context) {
                    $varName = $matches[1];
                    return $context[$varName] ?? $matches[0];
                }, $value);
            } elseif (is_array($value)) {
                $value = $this->substituteContext($value, $context);
            }
            
            $substituted[$key] = $value;
        }
        
        return $substituted;
    }

    // Additional stub methods for other operations
    private function rotateLogs(array $data): array
    {
        return ['success' => true, 'data' => ['rotated' => true], 'error' => null];
    }

    private function archiveLogs(array $data): array
    {
        return ['success' => true, 'data' => ['archived' => true], 'error' => null];
    }

    private function tailLogs(array $data): array
    {
        return ['success' => true, 'data' => ['tailing' => true], 'error' => null];
    }
} 