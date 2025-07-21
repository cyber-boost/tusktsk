<?php

declare(strict_types=1);

namespace TuskLang\A5\G1;

use TuskLang\CoreOperator;
use InvalidArgumentException;
use JsonException;

/**
 * JsonOperator - Complete JSON parsing, validation, manipulation, and transformation
 * 
 * Provides comprehensive JSON operations including parsing, validation,
 * transformation, querying, and manipulation with full error handling.
 */
class JsonOperator extends CoreOperator
{
    public function getName(): string
    {
        return 'json';
    }

    public function getDescription(): string 
    {
        return 'Complete JSON parsing, validation, manipulation, and transformation operations';
    }

    public function getSupportedActions(): array
    {
        return [
            'parse', 'stringify', 'validate', 'transform', 'merge',
            'extract', 'query', 'minify', 'prettify', 'schema_validate'
        ];
    }

    public function execute(string $action, array $params = []): mixed
    {
        return match($action) {
            'parse' => $this->parseJson($params['json'] ?? '', $params['options'] ?? []),
            'stringify' => $this->stringifyJson($params['data'] ?? null, $params['options'] ?? []),
            'validate' => $this->validateJson($params['json'] ?? ''),
            'transform' => $this->transformJson($params['json'] ?? '', $params['transformer'] ?? null),
            'merge' => $this->mergeJson($params['json1'] ?? '', $params['json2'] ?? '', $params['options'] ?? []),
            'extract' => $this->extractFromJson($params['json'] ?? '', $params['path'] ?? ''),
            'query' => $this->queryJson($params['json'] ?? '', $params['query'] ?? ''),
            'minify' => $this->minifyJson($params['json'] ?? ''),
            'prettify' => $this->prettifyJson($params['json'] ?? '', $params['options'] ?? []),
            'schema_validate' => $this->validateSchema($params['json'] ?? '', $params['schema'] ?? ''),
            default => throw new InvalidArgumentException("Unsupported action: {$action}")
        };
    }

    /**
     * Parse JSON string with comprehensive error handling
     */
    private function parseJson(string $json, array $options = []): array|object|null
    {
        if (empty($json)) {
            throw new InvalidArgumentException('JSON string cannot be empty');
        }

        $flags = JSON_THROW_ON_ERROR;
        if ($options['big_int_as_string'] ?? false) {
            $flags |= JSON_BIGINT_AS_STRING;
        }
        if ($options['invalid_utf8_ignore'] ?? false) {
            $flags |= JSON_INVALID_UTF8_IGNORE;
        }

        try {
            $depth = $options['depth'] ?? 512;
            $result = json_decode($json, $options['assoc'] ?? true, $depth, $flags);
            
            return $result;
        } catch (JsonException $e) {
            throw new InvalidArgumentException("Invalid JSON: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Convert data to JSON string with formatting options
     */
    private function stringifyJson(mixed $data, array $options = []): string
    {
        if ($data === null && !($options['encode_null'] ?? true)) {
            return '';
        }

        $flags = JSON_THROW_ON_ERROR;
        
        if ($options['pretty'] ?? false) {
            $flags |= JSON_PRETTY_PRINT;
        }
        if ($options['unescaped_slashes'] ?? false) {
            $flags |= JSON_UNESCAPED_SLASHES;
        }
        if ($options['unescaped_unicode'] ?? false) {
            $flags |= JSON_UNESCAPED_UNICODE;
        }
        if ($options['numeric_check'] ?? false) {
            $flags |= JSON_NUMERIC_CHECK;
        }

        try {
            $depth = $options['depth'] ?? 512;
            return json_encode($data, $flags, $depth);
        } catch (JsonException $e) {
            throw new InvalidArgumentException("JSON encoding failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Validate JSON string structure
     */
    private function validateJson(string $json): array
    {
        if (empty($json)) {
            return ['valid' => false, 'error' => 'Empty JSON string'];
        }

        try {
            json_decode($json, true, 512, JSON_THROW_ON_ERROR);
            return ['valid' => true, 'error' => null];
        } catch (JsonException $e) {
            return [
                'valid' => false, 
                'error' => $e->getMessage(),
                'code' => $e->getCode()
            ];
        }
    }

    /**
     * Transform JSON using callback function or transformation rules
     */
    private function transformJson(string $json, ?callable $transformer): string
    {
        if ($transformer === null) {
            throw new InvalidArgumentException('Transformer callback is required');
        }

        $data = $this->parseJson($json);
        $transformed = $this->applyTransformation($data, $transformer);
        
        return $this->stringifyJson($transformed, ['pretty' => true]);
    }

    /**
     * Apply transformation recursively to data structure
     */
    private function applyTransformation(mixed $data, callable $transformer): mixed
    {
        if (is_array($data)) {
            $result = [];
            foreach ($data as $key => $value) {
                $transformedKey = $transformer($key, 'key');
                $transformedValue = is_array($value) || is_object($value) 
                    ? $this->applyTransformation($value, $transformer)
                    : $transformer($value, 'value');
                    
                $result[$transformedKey] = $transformedValue;
            }
            return $result;
        } elseif (is_object($data)) {
            $result = new \stdClass();
            foreach ($data as $key => $value) {
                $transformedKey = $transformer($key, 'key');
                $transformedValue = is_array($value) || is_object($value)
                    ? $this->applyTransformation($value, $transformer) 
                    : $transformer($value, 'value');
                    
                $result->$transformedKey = $transformedValue;
            }
            return $result;
        }
        
        return $transformer($data, 'value');
    }

    /**
     * Merge two JSON strings with conflict resolution
     */
    private function mergeJson(string $json1, string $json2, array $options = []): string
    {
        $data1 = $this->parseJson($json1);
        $data2 = $this->parseJson($json2);
        
        $strategy = $options['strategy'] ?? 'override'; // override, keep, merge
        $merged = $this->deepMerge($data1, $data2, $strategy);
        
        return $this->stringifyJson($merged, ['pretty' => true]);
    }

    /**
     * Deep merge arrays/objects with strategy
     */
    private function deepMerge(mixed $data1, mixed $data2, string $strategy): mixed
    {
        if (!is_array($data1) || !is_array($data2)) {
            return match($strategy) {
                'keep' => $data1,
                'override' => $data2,
                'merge' => $data2,
                default => $data2
            };
        }

        $result = $data1;
        foreach ($data2 as $key => $value) {
            if (isset($result[$key])) {
                if (is_array($result[$key]) && is_array($value)) {
                    $result[$key] = $this->deepMerge($result[$key], $value, $strategy);
                } else {
                    $result[$key] = match($strategy) {
                        'keep' => $result[$key],
                        'override' => $value,
                        'merge' => $value,
                        default => $value
                    };
                }
            } else {
                $result[$key] = $value;
            }
        }
        
        return $result;
    }

    /**
     * Extract value from JSON using dot notation path
     */
    private function extractFromJson(string $json, string $path): mixed
    {
        $data = $this->parseJson($json);
        return $this->extractByPath($data, $path);
    }

    /**
     * Extract value by dot notation path
     */
    private function extractByPath(mixed $data, string $path): mixed
    {
        if (empty($path)) {
            return $data;
        }

        $keys = explode('.', $path);
        $current = $data;
        
        foreach ($keys as $key) {
            if (is_array($current) && isset($current[$key])) {
                $current = $current[$key];
            } elseif (is_object($current) && isset($current->$key)) {
                $current = $current->$key;
            } else {
                return null;
            }
        }
        
        return $current;
    }

    /**
     * Query JSON using simple query syntax
     */
    private function queryJson(string $json, string $query): array
    {
        $data = $this->parseJson($json);
        return $this->executeQuery($data, $query);
    }

    /**
     * Execute simple query on JSON data
     */
    private function executeQuery(mixed $data, string $query): array
    {
        // Simple query implementation - can be extended for complex queries
        $results = [];
        
        if (is_array($data)) {
            foreach ($data as $item) {
                if ($this->matchesQuery($item, $query)) {
                    $results[] = $item;
                }
            }
        } elseif ($this->matchesQuery($data, $query)) {
            $results[] = $data;
        }
        
        return $results;
    }

    /**
     * Check if data matches simple query
     */
    private function matchesQuery(mixed $data, string $query): bool
    {
        // Simple implementation - extend for complex queries
        if (is_string($data)) {
            return stripos($data, $query) !== false;
        } elseif (is_array($data)) {
            return in_array($query, array_values($data), true);
        }
        
        return false;
    }

    /**
     * Minify JSON by removing whitespace
     */
    private function minifyJson(string $json): string
    {
        $data = $this->parseJson($json);
        return $this->stringifyJson($data);
    }

    /**
     * Prettify JSON with formatting
     */
    private function prettifyJson(string $json, array $options = []): string
    {
        $data = $this->parseJson($json);
        $prettyOptions = array_merge(['pretty' => true], $options);
        return $this->stringifyJson($data, $prettyOptions);
    }

    /**
     * Validate JSON against schema (basic implementation)
     */
    private function validateSchema(string $json, string $schema): array
    {
        try {
            $data = $this->parseJson($json);
            $schemaData = $this->parseJson($schema);
            
            $isValid = $this->validateAgainstSchema($data, $schemaData);
            
            return [
                'valid' => $isValid,
                'errors' => $isValid ? [] : ['Schema validation failed']
            ];
        } catch (Exception $e) {
            return [
                'valid' => false,
                'errors' => [$e->getMessage()]
            ];
        }
    }

    /**
     * Basic schema validation (can be extended)
     */
    private function validateAgainstSchema(mixed $data, mixed $schema): bool
    {
        // Basic implementation - extend for full JSON Schema support
        if (!is_array($schema)) {
            return true;
        }

        if (isset($schema['type'])) {
            $expectedType = $schema['type'];
            $actualType = gettype($data);
            
            $typeMap = [
                'string' => 'string',
                'number' => ['integer', 'double'],
                'integer' => 'integer',
                'boolean' => 'boolean',
                'array' => 'array',
                'object' => ['array', 'object']
            ];
            
            $validTypes = is_array($typeMap[$expectedType] ?? []) 
                ? $typeMap[$expectedType] 
                : [$typeMap[$expectedType] ?? $expectedType];
                
            if (!in_array($actualType, $validTypes, true)) {
                return false;
            }
        }

        return true;
    }
} 