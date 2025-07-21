<?php

declare(strict_types=1);

namespace TuskLang\A5\G6;

use TuskLang\CoreOperator;
use InvalidArgumentException;

/**
 * UtilityOperator - General purpose utility functions and helper operations
 * 
 * Provides comprehensive utility operations including data type conversion,
 * array utilities, object helpers, debugging tools, performance measurement, and misc utilities.
 */
class UtilityOperator extends CoreOperator
{
    public function getName(): string
    {
        return 'utility';
    }

    public function getDescription(): string 
    {
        return 'General purpose utility functions and helper operations';
    }

    public function getSupportedActions(): array
    {
        return [
            'type_check', 'convert_type', 'deep_clone', 'memory_usage', 'benchmark',
            'debug_info', 'var_dump', 'print_r', 'backtrace', 'system_info',
            'generate_id', 'sleep', 'microtime', 'range', 'enum_values'
        ];
    }

    public function execute(string $action, array $params = []): mixed
    {
        return match($action) {
            'type_check' => $this->typeCheck($params['value'] ?? null),
            'convert_type' => $this->convertType($params['value'] ?? null, $params['target_type'] ?? 'string'),
            'deep_clone' => $this->deepClone($params['value'] ?? null),
            'memory_usage' => $this->getMemoryUsage($params['real'] ?? false),
            'benchmark' => $this->benchmark($params['callback'] ?? null, $params['iterations'] ?? 1),
            'debug_info' => $this->getDebugInfo($params['value'] ?? null),
            'var_dump' => $this->varDump($params['value'] ?? null),
            'print_r' => $this->printR($params['value'] ?? null),
            'backtrace' => $this->getBacktrace($params['options'] ?? []),
            'system_info' => $this->getSystemInfo(),
            'generate_id' => $this->generateId($params['type'] ?? 'uniqid', $params['options'] ?? []),
            'sleep' => $this->sleep($params['seconds'] ?? 1, $params['type'] ?? 'seconds'),
            'microtime' => $this->getMicrotime($params['as_float'] ?? true),
            'range' => $this->range($params['start'] ?? 0, $params['end'] ?? 10, $params['step'] ?? 1),
            'enum_values' => $this->enumValues($params['enum'] ?? null),
            default => throw new InvalidArgumentException("Unsupported action: {$action}")
        };
    }

    /**
     * Check and analyze data type
     */
    private function typeCheck(mixed $value): array
    {
        $type = gettype($value);
        $phpType = get_debug_type($value);
        
        $analysis = [
            'type' => $type,
            'php_type' => $phpType,
            'is_scalar' => is_scalar($value),
            'is_numeric' => is_numeric($value),
            'is_countable' => is_countable($value),
            'is_iterable' => is_iterable($value),
            'is_callable' => is_callable($value),
            'is_resource' => is_resource($value),
            'is_null' => is_null($value),
            'is_empty' => empty($value),
            'size_info' => $this->getSizeInfo($value)
        ];

        // Add specific type information
        if (is_string($value)) {
            $analysis['string_info'] = [
                'length' => strlen($value),
                'mb_length' => mb_strlen($value),
                'encoding' => mb_detect_encoding($value),
                'is_json' => $this->isValidJson($value),
                'is_serialized' => $this->isSerialized($value),
                'is_base64' => $this->isBase64($value)
            ];
        } elseif (is_array($value)) {
            $analysis['array_info'] = [
                'count' => count($value),
                'is_associative' => $this->isAssociativeArray($value),
                'is_sequential' => array_is_list($value),
                'depth' => $this->getArrayDepth($value),
                'memory_size' => $this->getArrayMemorySize($value)
            ];
        } elseif (is_object($value)) {
            $analysis['object_info'] = [
                'class' => get_class($value),
                'methods' => get_class_methods($value),
                'properties' => get_object_vars($value),
                'parent_class' => get_parent_class($value),
                'interfaces' => class_implements($value),
                'traits' => class_uses($value)
            ];
        } elseif (is_resource($value)) {
            $analysis['resource_info'] = [
                'type' => get_resource_type($value),
                'id' => get_resource_id($value)
            ];
        }

        return $analysis;
    }

    /**
     * Convert value to target type
     */
    private function convertType(mixed $value, string $targetType): array
    {
        $originalType = gettype($value);
        $success = true;
        $error = null;

        try {
            $converted = match($targetType) {
                'string' => (string) $value,
                'int', 'integer' => (int) $value,
                'float', 'double' => (float) $value,
                'bool', 'boolean' => (bool) $value,
                'array' => is_array($value) ? $value : [$value],
                'object' => is_object($value) ? $value : (object) $value,
                'json' => json_encode($value),
                'serialize' => serialize($value),
                'base64' => base64_encode(serialize($value)),
                'null' => null,
                default => throw new InvalidArgumentException("Unsupported target type: {$targetType}")
            };
        } catch (\Exception $e) {
            $success = false;
            $error = $e->getMessage();
            $converted = null;
        }

        return [
            'original_value' => $value,
            'original_type' => $originalType,
            'target_type' => $targetType,
            'converted_value' => $converted,
            'converted_type' => $success ? gettype($converted) : null,
            'success' => $success,
            'error' => $error,
            'loss_of_precision' => $this->checkPrecisionLoss($value, $converted, $originalType, $targetType)
        ];
    }

    /**
     * Deep clone of complex data structures
     */
    private function deepClone(mixed $value): array
    {
        $originalSize = $this->getMemorySize($value);
        
        if (is_object($value)) {
            $cloned = clone $value;
            $cloned = unserialize(serialize($cloned)); // Deep clone
        } else {
            $cloned = unserialize(serialize($value));
        }
        
        $clonedSize = $this->getMemorySize($cloned);

        return [
            'original' => $value,
            'cloned' => $cloned,
            'original_type' => gettype($value),
            'cloned_type' => gettype($cloned),
            'original_size' => $originalSize,
            'cloned_size' => $clonedSize,
            'identical' => $value === $cloned,
            'equal' => $value == $cloned
        ];
    }

    /**
     * Get memory usage information
     */
    private function getMemoryUsage(bool $real = false): array
    {
        $current = memory_get_usage($real);
        $peak = memory_get_peak_usage($real);
        $limit = ini_get('memory_limit');
        
        return [
            'current_bytes' => $current,
            'current_mb' => round($current / 1048576, 2),
            'peak_bytes' => $peak,
            'peak_mb' => round($peak / 1048576, 2),
            'memory_limit' => $limit,
            'memory_limit_bytes' => $this->parseMemoryLimit($limit),
            'usage_percentage' => $this->calculateMemoryUsage($current, $limit),
            'real_usage' => $real
        ];
    }

    /**
     * Benchmark function execution
     */
    private function benchmark(?callable $callback, int $iterations = 1): array
    {
        if ($callback === null) {
            throw new InvalidArgumentException('Callback function is required');
        }

        if ($iterations < 1) {
            throw new InvalidArgumentException('Iterations must be at least 1');
        }

        $times = [];
        $memoryBefore = memory_get_usage();
        $results = [];

        for ($i = 0; $i < $iterations; $i++) {
            $start = hrtime(true);
            $result = $callback();
            $end = hrtime(true);
            
            $times[] = ($end - $start) / 1e6; // Convert to milliseconds
            if ($iterations <= 10) {
                $results[] = $result; // Store results for small iterations
            }
        }

        $memoryAfter = memory_get_usage();
        
        return [
            'iterations' => $iterations,
            'times_ms' => $times,
            'total_time_ms' => array_sum($times),
            'average_time_ms' => array_sum($times) / count($times),
            'min_time_ms' => min($times),
            'max_time_ms' => max($times),
            'memory_before' => $memoryBefore,
            'memory_after' => $memoryAfter,
            'memory_used' => $memoryAfter - $memoryBefore,
            'results' => $results
        ];
    }

    /**
     * Get comprehensive debug information
     */
    private function getDebugInfo(mixed $value): array
    {
        $typeInfo = $this->typeCheck($value);
        
        return [
            'value' => $value,
            'type_info' => $typeInfo,
            'debug_string' => var_export($value, true),
            'json_representation' => json_encode($value, JSON_PRETTY_PRINT),
            'serialized' => serialize($value),
            'hash' => hash('sha256', serialize($value)),
            'memory_usage' => $this->getMemorySize($value),
            'timestamp' => time(),
            'microtime' => microtime(true)
        ];
    }

    /**
     * Enhanced var_dump with return value
     */
    private function varDump(mixed $value): array
    {
        ob_start();
        var_dump($value);
        $output = ob_get_clean();
        
        return [
            'value' => $value,
            'var_dump_output' => $output,
            'lines' => substr_count($output, "\n"),
            'length' => strlen($output)
        ];
    }

    /**
     * Enhanced print_r with return value
     */
    private function printR(mixed $value): array
    {
        $output = print_r($value, true);
        
        return [
            'value' => $value,
            'print_r_output' => $output,
            'lines' => substr_count($output, "\n"),
            'length' => strlen($output)
        ];
    }

    /**
     * Get debug backtrace
     */
    private function getBacktrace(array $options = []): array
    {
        $provideObject = $options['provide_object'] ?? false;
        $ignoreArgs = $options['ignore_args'] ?? false;
        $limit = $options['limit'] ?? 0;
        
        $flags = 0;
        if (!$provideObject) $flags |= DEBUG_BACKTRACE_IGNORE_ARGS;
        if ($ignoreArgs) $flags |= DEBUG_BACKTRACE_IGNORE_ARGS;
        
        $trace = debug_backtrace($flags, $limit);
        
        // Process trace for better readability
        $processedTrace = [];
        foreach ($trace as $i => $frame) {
            $processedTrace[$i] = [
                'file' => $frame['file'] ?? 'unknown',
                'line' => $frame['line'] ?? 0,
                'function' => $frame['function'] ?? 'unknown',
                'class' => $frame['class'] ?? null,
                'type' => $frame['type'] ?? null,
                'args_count' => isset($frame['args']) ? count($frame['args']) : 0
            ];
            
            if (!$ignoreArgs && isset($frame['args'])) {
                $processedTrace[$i]['args'] = $frame['args'];
            }
        }

        return [
            'trace' => $processedTrace,
            'depth' => count($trace),
            'options' => $options,
            'current_file' => __FILE__,
            'current_line' => __LINE__
        ];
    }

    /**
     * Get system information
     */
    private function getSystemInfo(): array
    {
        return [
            'php' => [
                'version' => PHP_VERSION,
                'major_version' => PHP_MAJOR_VERSION,
                'minor_version' => PHP_MINOR_VERSION,
                'release_version' => PHP_RELEASE_VERSION,
                'sapi' => php_sapi_name(),
                'os' => PHP_OS,
                'architecture' => php_uname('m')
            ],
            'server' => [
                'software' => $_SERVER['SERVER_SOFTWARE'] ?? 'unknown',
                'hostname' => gethostname(),
                'ip' => $_SERVER['SERVER_ADDR'] ?? 'unknown',
                'port' => $_SERVER['SERVER_PORT'] ?? 'unknown'
            ],
            'memory' => $this->getMemoryUsage(),
            'extensions' => [
                'loaded' => get_loaded_extensions(),
                'count' => count(get_loaded_extensions())
            ],
            'ini_settings' => [
                'max_execution_time' => ini_get('max_execution_time'),
                'memory_limit' => ini_get('memory_limit'),
                'upload_max_filesize' => ini_get('upload_max_filesize'),
                'post_max_size' => ini_get('post_max_size'),
                'display_errors' => ini_get('display_errors'),
                'error_reporting' => ini_get('error_reporting')
            ]
        ];
    }

    /**
     * Generate various types of IDs
     */
    private function generateId(string $type, array $options = []): array
    {
        $id = match($type) {
            'uniqid' => uniqid($options['prefix'] ?? '', $options['more_entropy'] ?? false),
            'random' => $this->generateRandomId($options['length'] ?? 16),
            'timestamp' => $this->generateTimestampId($options),
            'incremental' => $this->generateIncrementalId($options),
            'hash' => $this->generateHashId($options),
            'base62' => $this->generateBase62Id($options['length'] ?? 8),
            default => throw new InvalidArgumentException("Unknown ID type: {$type}")
        };

        return [
            'id' => $id,
            'type' => $type,
            'length' => strlen($id),
            'timestamp' => time(),
            'options' => $options,
            'collision_resistant' => in_array($type, ['uniqid', 'random', 'hash'])
        ];
    }

    /**
     * Sleep with various time units
     */
    private function sleep(int|float $amount, string $type = 'seconds'): array
    {
        $startTime = microtime(true);
        
        match($type) {
            'seconds' => sleep($amount),
            'milliseconds' => usleep($amount * 1000),
            'microseconds' => usleep($amount),
            'nanoseconds' => time_nanosleep(0, $amount),
            default => throw new InvalidArgumentException("Unknown sleep type: {$type}")
        };
        
        $endTime = microtime(true);
        $actualSleep = $endTime - $startTime;

        return [
            'requested_amount' => $amount,
            'requested_type' => $type,
            'actual_sleep_seconds' => $actualSleep,
            'accuracy' => $this->calculateSleepAccuracy($amount, $type, $actualSleep)
        ];
    }

    /**
     * Get microtime with options
     */
    private function getMicrotime(bool $asFloat = true): array
    {
        $microtime = microtime($asFloat);
        $time = time();
        
        return [
            'microtime' => $microtime,
            'as_float' => $asFloat,
            'timestamp' => $time,
            'formatted' => date('Y-m-d H:i:s', $time),
            'milliseconds' => $asFloat ? intval(($microtime - $time) * 1000) : null
        ];
    }

    /**
     * Generate range of values
     */
    private function range(int|float $start, int|float $end, int|float $step = 1): array
    {
        if ($step == 0) {
            throw new InvalidArgumentException('Step cannot be zero');
        }

        if (($start < $end && $step < 0) || ($start > $end && $step > 0)) {
            throw new InvalidArgumentException('Invalid step direction');
        }

        $values = range($start, $end, $step);
        
        return [
            'values' => $values,
            'start' => $start,
            'end' => $end,
            'step' => $step,
            'count' => count($values),
            'type' => is_int($start) && is_int($end) && is_int($step) ? 'integer' : 'float'
        ];
    }

    /**
     * Get enum values (for PHP 8.1+)
     */
    private function enumValues(?string $enumClass): array
    {
        if ($enumClass === null) {
            return ['error' => 'Enum class name is required'];
        }

        if (!enum_exists($enumClass)) {
            return ['error' => "Enum class does not exist: {$enumClass}"];
        }

        $reflection = new \ReflectionEnum($enumClass);
        $cases = $reflection->getCases();
        
        $values = [];
        $names = [];
        
        foreach ($cases as $case) {
            $names[] = $case->getName();
            $values[] = $case->getValue();
        }

        return [
            'enum_class' => $enumClass,
            'case_names' => $names,
            'case_values' => $values,
            'count' => count($cases),
            'is_backed' => $reflection->isBacked(),
            'backing_type' => $reflection->isBacked() ? $reflection->getBackingType()?->getName() : null
        ];
    }

    // Helper methods

    private function getSizeInfo(mixed $value): array
    {
        if (is_string($value)) {
            return [
                'bytes' => strlen($value),
                'characters' => mb_strlen($value),
                'memory_usage' => $this->getMemorySize($value)
            ];
        } elseif (is_array($value)) {
            return [
                'count' => count($value),
                'memory_usage' => $this->getMemorySize($value)
            ];
        } elseif (is_object($value)) {
            return [
                'properties' => count(get_object_vars($value)),
                'memory_usage' => $this->getMemorySize($value)
            ];
        }
        
        return ['memory_usage' => $this->getMemorySize($value)];
    }

    private function isValidJson(string $string): bool
    {
        json_decode($string);
        return json_last_error() === JSON_ERROR_NONE;
    }

    private function isSerialized(string $string): bool
    {
        return $string === serialize(false) || @unserialize($string) !== false;
    }

    private function isBase64(string $string): bool
    {
        return base64_encode(base64_decode($string, true)) === $string;
    }

    private function isAssociativeArray(array $array): bool
    {
        return !array_is_list($array);
    }

    private function getArrayDepth(array $array, int $depth = 0): int
    {
        $maxDepth = $depth;
        
        foreach ($array as $value) {
            if (is_array($value)) {
                $currentDepth = $this->getArrayDepth($value, $depth + 1);
                $maxDepth = max($maxDepth, $currentDepth);
            }
        }
        
        return $maxDepth;
    }

    private function getArrayMemorySize(array $array): int
    {
        return strlen(serialize($array));
    }

    private function getMemorySize(mixed $value): int
    {
        return strlen(serialize($value));
    }

    private function checkPrecisionLoss(mixed $original, mixed $converted, string $originalType, string $targetType): bool
    {
        if ($originalType === 'double' && $targetType === 'integer') {
            return (float) $converted !== $original;
        }
        
        if (in_array($originalType, ['array', 'object']) && $targetType === 'string') {
            return true;
        }
        
        return false;
    }

    private function parseMemoryLimit(string $limit): int
    {
        if ($limit === '-1') {
            return -1;
        }
        
        $unit = strtolower(substr($limit, -1));
        $value = intval(substr($limit, 0, -1));
        
        return match($unit) {
            'g' => $value * 1024 * 1024 * 1024,
            'm' => $value * 1024 * 1024,
            'k' => $value * 1024,
            default => $value
        };
    }

    private function calculateMemoryUsage(int $current, string $limit): float
    {
        $limitBytes = $this->parseMemoryLimit($limit);
        
        if ($limitBytes === -1) {
            return 0; // Unlimited
        }
        
        return ($current / $limitBytes) * 100;
    }

    private function generateRandomId(int $length): string
    {
        $chars = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
        $id = '';
        
        for ($i = 0; $i < $length; $i++) {
            $id .= $chars[random_int(0, strlen($chars) - 1)];
        }
        
        return $id;
    }

    private function generateTimestampId(array $options): string
    {
        $timestamp = $options['timestamp'] ?? time();
        $prefix = $options['prefix'] ?? '';
        $suffix = $options['suffix'] ?? '';
        
        return $prefix . $timestamp . $suffix;
    }

    private function generateIncrementalId(array $options): string
    {
        static $counter = 0;
        
        $counter++;
        $prefix = $options['prefix'] ?? '';
        $padding = $options['padding'] ?? 0;
        
        return $prefix . str_pad($counter, $padding, '0', STR_PAD_LEFT);
    }

    private function generateHashId(array $options): string
    {
        $data = $options['data'] ?? microtime(true) . random_int(0, PHP_INT_MAX);
        $algorithm = $options['algorithm'] ?? 'sha256';
        $length = $options['length'] ?? null;
        
        $hash = hash($algorithm, $data);
        
        return $length ? substr($hash, 0, $length) : $hash;
    }

    private function generateBase62Id(int $length): string
    {
        $chars = '0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ';
        $id = '';
        
        for ($i = 0; $i < $length; $i++) {
            $id .= $chars[random_int(0, strlen($chars) - 1)];
        }
        
        return $id;
    }

    private function calculateSleepAccuracy(int|float $requested, string $type, float $actual): float
    {
        $requestedSeconds = match($type) {
            'seconds' => $requested,
            'milliseconds' => $requested / 1000,
            'microseconds' => $requested / 1000000,
            'nanoseconds' => $requested / 1000000000,
            default => $requested
        };
        
        if ($requestedSeconds == 0) {
            return 100;
        }
        
        return (1 - abs($actual - $requestedSeconds) / $requestedSeconds) * 100;
    }
} 