<?php

declare(strict_types=1);

namespace TuskLang\A5\G2;

use TuskLang\CoreOperator;
use InvalidArgumentException;

/**
 * DataMapOperator - Data mapping, field transformation, and schema conversion
 * 
 * Provides comprehensive data mapping operations including field mapping,
 * schema transformation, data restructuring, and complex mapping rules.
 */
class DataMapOperator extends CoreOperator
{
    public function getName(): string
    {
        return 'data_map';
    }

    public function getDescription(): string 
    {
        return 'Data mapping, field transformation, and schema conversion operations';
    }

    public function getSupportedActions(): array
    {
        return [
            'map_fields', 'apply_schema', 'transform_structure', 'rename_keys',
            'flatten_data', 'nest_data', 'map_values', 'conditional_map',
            'batch_map', 'reverse_map', 'validate_mapping', 'generate_mapping'
        ];
    }

    public function execute(string $action, array $params = []): mixed
    {
        return match($action) {
            'map_fields' => $this->mapFields($params['data'] ?? [], $params['mapping'] ?? []),
            'apply_schema' => $this->applySchema($params['data'] ?? [], $params['schema'] ?? []),
            'transform_structure' => $this->transformStructure($params['data'] ?? [], $params['transformer'] ?? null),
            'rename_keys' => $this->renameKeys($params['data'] ?? [], $params['key_map'] ?? []),
            'flatten_data' => $this->flattenData($params['data'] ?? [], $params['separator'] ?? '.'),
            'nest_data' => $this->nestData($params['data'] ?? [], $params['separator'] ?? '.'),
            'map_values' => $this->mapValues($params['data'] ?? [], $params['value_map'] ?? []),
            'conditional_map' => $this->conditionalMap($params['data'] ?? [], $params['conditions'] ?? []),
            'batch_map' => $this->batchMap($params['data'] ?? [], $params['mapping'] ?? []),
            'reverse_map' => $this->reverseMap($params['data'] ?? [], $params['original_mapping'] ?? []),
            'validate_mapping' => $this->validateMapping($params['mapping'] ?? []),
            'generate_mapping' => $this->generateMapping($params['source_data'] ?? [], $params['target_schema'] ?? []),
            default => throw new InvalidArgumentException("Unsupported action: {$action}")
        };
    }

    /**
     * Map data fields according to mapping rules
     */
    private function mapFields(array $data, array $mapping): array
    {
        if (empty($mapping)) {
            throw new InvalidArgumentException('Mapping rules cannot be empty');
        }

        $result = [];

        foreach ($mapping as $targetKey => $sourceKey) {
            if (is_array($sourceKey)) {
                // Complex mapping rule
                $result[$targetKey] = $this->applyComplexMapping($data, $sourceKey);
            } else {
                // Simple field mapping
                $value = $this->getNestedValue($data, $sourceKey);
                if ($value !== null) {
                    $result[$targetKey] = $value;
                }
            }
        }

        return $result;
    }

    /**
     * Apply complex mapping rule
     */
    private function applyComplexMapping(array $data, array $rule): mixed
    {
        if (isset($rule['source'])) {
            $value = $this->getNestedValue($data, $rule['source']);
            
            // Apply transformations
            if (isset($rule['transform'])) {
                $value = $this->applyTransform($value, $rule['transform']);
            }
            
            // Apply default value if null
            if ($value === null && isset($rule['default'])) {
                $value = $rule['default'];
            }
            
            return $value;
        } elseif (isset($rule['concat'])) {
            // Concatenate multiple fields
            return $this->concatenateFields($data, $rule['concat'], $rule['separator'] ?? '');
        } elseif (isset($rule['conditional'])) {
            // Conditional mapping
            return $this->applyConditionalMapping($data, $rule['conditional']);
        }

        return null;
    }

    /**
     * Apply schema transformation
     */
    private function applySchema(array $data, array $schema): array
    {
        if (empty($schema)) {
            throw new InvalidArgumentException('Schema cannot be empty');
        }

        $result = [];

        foreach ($schema as $fieldName => $fieldSchema) {
            if (is_string($fieldSchema)) {
                // Simple field mapping
                $result[$fieldName] = $this->getNestedValue($data, $fieldSchema);
            } elseif (is_array($fieldSchema)) {
                // Complex field schema
                $result[$fieldName] = $this->applyFieldSchema($data, $fieldSchema);
            }
        }

        return $result;
    }

    /**
     * Apply field schema rules
     */
    private function applyFieldSchema(array $data, array $schema): mixed
    {
        $value = null;

        if (isset($schema['source'])) {
            $value = $this->getNestedValue($data, $schema['source']);
        }

        // Apply type conversion
        if (isset($schema['type'])) {
            $value = $this->convertType($value, $schema['type']);
        }

        // Apply validation
        if (isset($schema['validate'])) {
            if (!$this->validateValue($value, $schema['validate'])) {
                if (isset($schema['default'])) {
                    $value = $schema['default'];
                } else {
                    throw new InvalidArgumentException("Validation failed for value: " . var_export($value, true));
                }
            }
        }

        // Apply transformation
        if (isset($schema['transform'])) {
            $value = $this->applyTransform($value, $schema['transform']);
        }

        return $value;
    }

    /**
     * Transform data structure using callback
     */
    private function transformStructure(array $data, ?callable $transformer): array
    {
        if ($transformer === null) {
            throw new InvalidArgumentException('Transformer callback is required');
        }

        return $transformer($data);
    }

    /**
     * Rename array keys according to mapping
     */
    private function renameKeys(array $data, array $keyMap): array
    {
        if (empty($keyMap)) {
            return $data;
        }

        $result = [];

        foreach ($data as $key => $value) {
            $newKey = $keyMap[$key] ?? $key;
            
            if (is_array($value)) {
                $result[$newKey] = $this->renameKeys($value, $keyMap);
            } else {
                $result[$newKey] = $value;
            }
        }

        return $result;
    }

    /**
     * Flatten nested data structure
     */
    private function flattenData(array $data, string $separator = '.'): array
    {
        $result = [];

        $flatten = function($array, $prefix = '') use (&$flatten, &$result, $separator) {
            foreach ($array as $key => $value) {
                $newKey = $prefix === '' ? $key : $prefix . $separator . $key;
                
                if (is_array($value)) {
                    $flatten($value, $newKey);
                } else {
                    $result[$newKey] = $value;
                }
            }
        };

        $flatten($data);
        return $result;
    }

    /**
     * Nest flattened data structure
     */
    private function nestData(array $data, string $separator = '.'): array
    {
        $result = [];

        foreach ($data as $key => $value) {
            $keys = explode($separator, $key);
            $current = &$result;

            foreach ($keys as $k) {
                if (!isset($current[$k])) {
                    $current[$k] = [];
                }
                $current = &$current[$k];
            }

            $current = $value;
        }

        return $result;
    }

    /**
     * Map values according to value mapping
     */
    private function mapValues(array $data, array $valueMap): array
    {
        if (empty($valueMap)) {
            return $data;
        }

        $result = [];

        foreach ($data as $key => $value) {
            if (is_array($value)) {
                $result[$key] = $this->mapValues($value, $valueMap);
            } else {
                $result[$key] = $valueMap[$value] ?? $value;
            }
        }

        return $result;
    }

    /**
     * Apply conditional mapping
     */
    private function conditionalMap(array $data, array $conditions): array
    {
        $result = $data;

        foreach ($conditions as $condition) {
            if ($this->evaluateCondition($data, $condition['if'])) {
                $result = array_merge($result, $condition['then']);
            } elseif (isset($condition['else'])) {
                $result = array_merge($result, $condition['else']);
            }
        }

        return $result;
    }

    /**
     * Process multiple data records with same mapping
     */
    private function batchMap(array $data, array $mapping): array
    {
        if (!is_array($data[0] ?? null)) {
            throw new InvalidArgumentException('Batch data must be array of arrays');
        }

        $results = [];
        foreach ($data as $record) {
            $results[] = $this->mapFields($record, $mapping);
        }

        return $results;
    }

    /**
     * Reverse mapping transformation
     */
    private function reverseMap(array $data, array $originalMapping): array
    {
        $reverseMapping = [];

        foreach ($originalMapping as $target => $source) {
            if (is_string($source)) {
                $reverseMapping[$source] = $target;
            }
        }

        return $this->mapFields($data, $reverseMapping);
    }

    /**
     * Validate mapping configuration
     */
    private function validateMapping(array $mapping): array
    {
        $errors = [];
        $warnings = [];

        foreach ($mapping as $target => $source) {
            if (!is_string($target)) {
                $errors[] = "Target key must be string, got: " . gettype($target);
            }

            if (!is_string($source) && !is_array($source)) {
                $errors[] = "Source mapping for '{$target}' must be string or array";
            }

            if (is_array($source)) {
                $schemaErrors = $this->validateMappingSchema($source);
                $errors = array_merge($errors, $schemaErrors);
            }
        }

        return [
            'valid' => empty($errors),
            'errors' => $errors,
            'warnings' => $warnings
        ];
    }

    /**
     * Generate mapping from source data and target schema
     */
    private function generateMapping(array $sourceData, array $targetSchema): array
    {
        $mapping = [];
        $sourceKeys = $this->extractAllKeys($sourceData);

        foreach ($targetSchema as $targetKey => $targetInfo) {
            // Find best matching source key
            $bestMatch = $this->findBestMatch($targetKey, $sourceKeys);
            if ($bestMatch) {
                $mapping[$targetKey] = $bestMatch;
            }
        }

        return $mapping;
    }

    /**
     * Get nested value using dot notation
     */
    private function getNestedValue(array $data, string $path): mixed
    {
        if (empty($path)) {
            return $data;
        }

        $keys = explode('.', $path);
        $current = $data;

        foreach ($keys as $key) {
            if (!is_array($current) || !array_key_exists($key, $current)) {
                return null;
            }
            $current = $current[$key];
        }

        return $current;
    }

    /**
     * Apply transformation to value
     */
    private function applyTransform(mixed $value, string|callable $transform): mixed
    {
        if (is_callable($transform)) {
            return $transform($value);
        }

        return match($transform) {
            'uppercase' => is_string($value) ? strtoupper($value) : $value,
            'lowercase' => is_string($value) ? strtolower($value) : $value,
            'trim' => is_string($value) ? trim($value) : $value,
            'int' => (int) $value,
            'float' => (float) $value,
            'string' => (string) $value,
            'bool' => (bool) $value,
            default => $value
        };
    }

    /**
     * Convert value to specified type
     */
    private function convertType(mixed $value, string $type): mixed
    {
        return match($type) {
            'string' => (string) $value,
            'int', 'integer' => (int) $value,
            'float', 'double' => (float) $value,
            'bool', 'boolean' => (bool) $value,
            'array' => is_array($value) ? $value : [$value],
            'json' => is_string($value) ? json_decode($value, true) : $value,
            default => $value
        };
    }

    /**
     * Validate value against rules
     */
    private function validateValue(mixed $value, array $rules): bool
    {
        foreach ($rules as $rule => $parameter) {
            if (!$this->applyValidationRule($value, $rule, $parameter)) {
                return false;
            }
        }

        return true;
    }

    /**
     * Apply single validation rule
     */
    private function applyValidationRule(mixed $value, string $rule, mixed $parameter): bool
    {
        return match($rule) {
            'required' => $value !== null && $value !== '',
            'min_length' => is_string($value) && strlen($value) >= $parameter,
            'max_length' => is_string($value) && strlen($value) <= $parameter,
            'min' => is_numeric($value) && $value >= $parameter,
            'max' => is_numeric($value) && $value <= $parameter,
            'regex' => is_string($value) && preg_match($parameter, $value),
            'in' => in_array($value, $parameter, true),
            'email' => is_string($value) && filter_var($value, FILTER_VALIDATE_EMAIL) !== false,
            'url' => is_string($value) && filter_var($value, FILTER_VALIDATE_URL) !== false,
            default => true
        };
    }

    /**
     * Concatenate multiple fields
     */
    private function concatenateFields(array $data, array $fields, string $separator): string
    {
        $values = [];

        foreach ($fields as $field) {
            $value = $this->getNestedValue($data, $field);
            if ($value !== null) {
                $values[] = (string) $value;
            }
        }

        return implode($separator, $values);
    }

    /**
     * Apply conditional mapping rule
     */
    private function applyConditionalMapping(array $data, array $conditional): mixed
    {
        if ($this->evaluateCondition($data, $conditional['if'])) {
            return $conditional['then'];
        } elseif (isset($conditional['else'])) {
            return $conditional['else'];
        }

        return null;
    }

    /**
     * Evaluate condition
     */
    private function evaluateCondition(array $data, array $condition): bool
    {
        $field = $condition['field'] ?? '';
        $operator = $condition['operator'] ?? '==';
        $value = $condition['value'] ?? null;

        $fieldValue = $this->getNestedValue($data, $field);

        return match($operator) {
            '==' => $fieldValue == $value,
            '===' => $fieldValue === $value,
            '!=' => $fieldValue != $value,
            '!==' => $fieldValue !== $value,
            '>' => $fieldValue > $value,
            '>=' => $fieldValue >= $value,
            '<' => $fieldValue < $value,
            '<=' => $fieldValue <= $value,
            'in' => in_array($fieldValue, $value, true),
            'not_in' => !in_array($fieldValue, $value, true),
            'contains' => is_string($fieldValue) && str_contains($fieldValue, $value),
            'starts_with' => is_string($fieldValue) && str_starts_with($fieldValue, $value),
            'ends_with' => is_string($fieldValue) && str_ends_with($fieldValue, $value),
            'regex' => is_string($fieldValue) && preg_match($value, $fieldValue),
            default => false
        };
    }

    /**
     * Validate mapping schema
     */
    private function validateMappingSchema(array $schema): array
    {
        $errors = [];

        $requiredKeys = ['source'];
        foreach ($requiredKeys as $key) {
            if (!isset($schema[$key])) {
                $errors[] = "Missing required key: {$key}";
            }
        }

        if (isset($schema['type']) && !in_array($schema['type'], ['string', 'int', 'float', 'bool', 'array', 'json'])) {
            $errors[] = "Invalid type: {$schema['type']}";
        }

        return $errors;
    }

    /**
     * Extract all keys from nested array
     */
    private function extractAllKeys(array $data, string $prefix = ''): array
    {
        $keys = [];

        foreach ($data as $key => $value) {
            $fullKey = $prefix === '' ? $key : $prefix . '.' . $key;
            $keys[] = $fullKey;

            if (is_array($value)) {
                $keys = array_merge($keys, $this->extractAllKeys($value, $fullKey));
            }
        }

        return $keys;
    }

    /**
     * Find best matching key using similarity
     */
    private function findBestMatch(string $target, array $candidates): ?string
    {
        $bestMatch = null;
        $bestScore = 0;

        foreach ($candidates as $candidate) {
            $score = $this->calculateSimilarity($target, $candidate);
            if ($score > $bestScore) {
                $bestScore = $score;
                $bestMatch = $candidate;
            }
        }

        return $bestScore > 0.6 ? $bestMatch : null; // 60% similarity threshold
    }

    /**
     * Calculate string similarity
     */
    private function calculateSimilarity(string $str1, string $str2): float
    {
        $str1 = strtolower($str1);
        $str2 = strtolower($str2);

        if ($str1 === $str2) {
            return 1.0;
        }

        similar_text($str1, $str2, $percent);
        return $percent / 100;
    }
} 