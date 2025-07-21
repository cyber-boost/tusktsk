<?php

declare(strict_types=1);

namespace TuskLang\A5\G3;

use TuskLang\CoreOperator;
use InvalidArgumentException;

/**
 * ValidatorOperator - Comprehensive data validation with built-in and custom rules
 * 
 * Provides comprehensive data validation operations including built-in rules,
 * custom validation functions, complex rule combinations, and validation reporting.
 */
class ValidatorOperator extends CoreOperator
{
    public function getName(): string
    {
        return 'validator';
    }

    public function getDescription(): string 
    {
        return 'Comprehensive data validation with built-in and custom rules';
    }

    public function getSupportedActions(): array
    {
        return [
            'validate', 'validate_multiple', 'custom_rule', 'validate_array',
            'conditional_validate', 'batch_validate', 'rule_exists', 'get_rules',
            'validate_nested', 'validate_schema', 'fail_fast', 'collect_errors'
        ];
    }

    public function execute(string $action, array $params = []): mixed
    {
        return match($action) {
            'validate' => $this->validateValue($params['value'] ?? null, $params['rules'] ?? [], $params['field'] ?? ''),
            'validate_multiple' => $this->validateMultiple($params['data'] ?? [], $params['validation_rules'] ?? []),
            'custom_rule' => $this->applyCustomRule($params['value'] ?? null, $params['rule'] ?? null, $params['parameters'] ?? []),
            'validate_array' => $this->validateArray($params['array'] ?? [], $params['rules'] ?? []),
            'conditional_validate' => $this->conditionalValidate($params['data'] ?? [], $params['condition'] ?? [], $params['rules'] ?? []),
            'batch_validate' => $this->batchValidate($params['datasets'] ?? [], $params['rules'] ?? []),
            'rule_exists' => $this->ruleExists($params['rule'] ?? ''),
            'get_rules' => $this->getAvailableRules(),
            'validate_nested' => $this->validateNested($params['data'] ?? [], $params['schema'] ?? []),
            'validate_schema' => $this->validateAgainstSchema($params['data'] ?? [], $params['schema'] ?? []),
            'fail_fast' => $this->validateFailFast($params['data'] ?? [], $params['rules'] ?? []),
            'collect_errors' => $this->validateCollectAll($params['data'] ?? [], $params['rules'] ?? []),
            default => throw new InvalidArgumentException("Unsupported action: {$action}")
        };
    }

    /**
     * Validate single value against rules
     */
    private function validateValue(mixed $value, array $rules, string $field = ''): array
    {
        $errors = [];
        $passed = [];

        foreach ($rules as $ruleName => $ruleParams) {
            if (is_int($ruleName)) {
                // Simple rule without parameters
                $ruleName = $ruleParams;
                $ruleParams = [];
            }

            try {
                $result = $this->applyRule($value, $ruleName, $ruleParams);
                if ($result) {
                    $passed[] = $ruleName;
                } else {
                    $errors[] = $this->formatError($field, $ruleName, $ruleParams, $value);
                }
            } catch (\Exception $e) {
                $errors[] = "Rule '{$ruleName}' failed: " . $e->getMessage();
            }
        }

        return [
            'valid' => empty($errors),
            'field' => $field,
            'value' => $value,
            'passed_rules' => $passed,
            'failed_rules' => array_keys($errors),
            'errors' => $errors
        ];
    }

    /**
     * Validate multiple fields with different rules
     */
    private function validateMultiple(array $data, array $validationRules): array
    {
        $results = [];
        $allValid = true;

        foreach ($validationRules as $field => $rules) {
            $value = $this->getNestedValue($data, $field);
            $result = $this->validateValue($value, $rules, $field);
            
            if (!$result['valid']) {
                $allValid = false;
            }
            
            $results[$field] = $result;
        }

        return [
            'valid' => $allValid,
            'results' => $results,
            'error_count' => array_sum(array_map(fn($r) => count($r['errors']), $results)),
            'all_errors' => $this->flattenErrors($results)
        ];
    }

    /**
     * Apply custom validation rule
     */
    private function applyCustomRule(mixed $value, ?callable $rule, array $parameters = []): array
    {
        if ($rule === null) {
            throw new InvalidArgumentException('Custom rule callback is required');
        }

        try {
            $result = $rule($value, ...$parameters);
            return [
                'valid' => (bool) $result,
                'value' => $value,
                'parameters' => $parameters
            ];
        } catch (\Exception $e) {
            return [
                'valid' => false,
                'value' => $value,
                'error' => $e->getMessage()
            ];
        }
    }

    /**
     * Validate array elements
     */
    private function validateArray(array $array, array $rules): array
    {
        $results = [];
        $allValid = true;

        foreach ($array as $index => $item) {
            $result = $this->validateValue($item, $rules, "[$index]");
            
            if (!$result['valid']) {
                $allValid = false;
            }
            
            $results[$index] = $result;
        }

        return [
            'valid' => $allValid,
            'results' => $results,
            'total_items' => count($array),
            'valid_items' => count(array_filter($results, fn($r) => $r['valid'])),
            'invalid_items' => count(array_filter($results, fn($r) => !$r['valid']))
        ];
    }

    /**
     * Conditional validation based on other field values
     */
    private function conditionalValidate(array $data, array $condition, array $rules): array
    {
        if (!$this->evaluateCondition($data, $condition)) {
            return [
                'valid' => true,
                'condition_met' => false,
                'message' => 'Validation skipped - condition not met'
            ];
        }

        return array_merge(
            $this->validateMultiple($data, $rules),
            ['condition_met' => true]
        );
    }

    /**
     * Batch validate multiple datasets
     */
    private function batchValidate(array $datasets, array $rules): array
    {
        $results = [];
        $stats = ['valid' => 0, 'invalid' => 0, 'total' => 0];

        foreach ($datasets as $index => $dataset) {
            $result = $this->validateMultiple($dataset, $rules);
            $results[$index] = $result;
            
            $stats['total']++;
            if ($result['valid']) {
                $stats['valid']++;
            } else {
                $stats['invalid']++;
            }
        }

        return [
            'results' => $results,
            'statistics' => $stats,
            'success_rate' => $stats['total'] > 0 ? ($stats['valid'] / $stats['total']) * 100 : 0
        ];
    }

    /**
     * Check if validation rule exists
     */
    private function ruleExists(string $rule): bool
    {
        return method_exists($this, 'validate' . ucfirst($rule)) || 
               in_array($rule, $this->getBuiltInRules());
    }

    /**
     * Get list of available validation rules
     */
    private function getAvailableRules(): array
    {
        return [
            'basic' => [
                'required', 'optional', 'nullable', 'filled'
            ],
            'type' => [
                'string', 'integer', 'numeric', 'boolean', 'array', 'object'
            ],
            'size' => [
                'min', 'max', 'between', 'size', 'min_length', 'max_length'
            ],
            'format' => [
                'email', 'url', 'alpha', 'alpha_numeric', 'regex', 'date', 'json'
            ],
            'comparison' => [
                'same', 'different', 'in', 'not_in', 'confirmed'
            ],
            'custom' => [
                'callback', 'closure', 'method'
            ]
        ];
    }

    /**
     * Validate nested data structure
     */
    private function validateNested(array $data, array $schema): array
    {
        $results = [];
        $allValid = true;

        foreach ($schema as $field => $fieldSchema) {
            $value = $this->getNestedValue($data, $field);
            
            if (isset($fieldSchema['nested']) && is_array($value)) {
                // Nested validation
                $nestedResult = $this->validateNested($value, $fieldSchema['nested']);
                $results[$field] = $nestedResult;
                
                if (!$nestedResult['valid']) {
                    $allValid = false;
                }
            } elseif (isset($fieldSchema['rules'])) {
                // Regular field validation
                $result = $this->validateValue($value, $fieldSchema['rules'], $field);
                $results[$field] = $result;
                
                if (!$result['valid']) {
                    $allValid = false;
                }
            }
        }

        return [
            'valid' => $allValid,
            'results' => $results,
            'nested_structure' => true
        ];
    }

    /**
     * Validate against JSON schema-like structure
     */
    private function validateAgainstSchema(array $data, array $schema): array
    {
        $errors = [];
        
        // Check required fields
        foreach ($schema['required'] ?? [] as $field) {
            if (!isset($data[$field]) || $data[$field] === null) {
                $errors[] = "Required field '{$field}' is missing";
            }
        }

        // Validate field properties
        foreach ($schema['properties'] ?? [] as $field => $properties) {
            if (!isset($data[$field])) {
                continue;
            }

            $value = $data[$field];
            $fieldErrors = $this->validateSchemaField($value, $properties, $field);
            $errors = array_merge($errors, $fieldErrors);
        }

        return [
            'valid' => empty($errors),
            'errors' => $errors,
            'schema_type' => $schema['type'] ?? 'object'
        ];
    }

    /**
     * Fail-fast validation (stop on first error)
     */
    private function validateFailFast(array $data, array $rules): array
    {
        foreach ($rules as $field => $fieldRules) {
            $value = $this->getNestedValue($data, $field);
            $result = $this->validateValue($value, $fieldRules, $field);
            
            if (!$result['valid']) {
                return [
                    'valid' => false,
                    'failed_field' => $field,
                    'first_error' => $result['errors'][0] ?? 'Unknown error',
                    'fail_fast' => true
                ];
            }
        }

        return [
            'valid' => true,
            'fail_fast' => true,
            'message' => 'All validations passed'
        ];
    }

    /**
     * Collect all validation errors
     */
    private function validateCollectAll(array $data, array $rules): array
    {
        return $this->validateMultiple($data, $rules);
    }

    /**
     * Apply single validation rule
     */
    private function applyRule(mixed $value, string $rule, array $params = []): bool
    {
        return match($rule) {
            'required' => $value !== null && $value !== '',
            'nullable' => true, // Always passes
            'filled' => $value !== null && $value !== '' && $value !== [],
            'string' => is_string($value),
            'integer' => is_int($value) || (is_string($value) && ctype_digit($value)),
            'numeric' => is_numeric($value),
            'boolean' => is_bool($value) || in_array($value, [0, 1, '0', '1', 'true', 'false'], true),
            'array' => is_array($value),
            'object' => is_object($value),
            'min' => is_numeric($value) && $value >= ($params[0] ?? 0),
            'max' => is_numeric($value) && $value <= ($params[0] ?? PHP_INT_MAX),
            'between' => is_numeric($value) && $value >= ($params[0] ?? 0) && $value <= ($params[1] ?? PHP_INT_MAX),
            'size' => $this->checkSize($value, $params[0] ?? 0),
            'min_length' => is_string($value) && strlen($value) >= ($params[0] ?? 0),
            'max_length' => is_string($value) && strlen($value) <= ($params[0] ?? PHP_INT_MAX),
            'email' => is_string($value) && filter_var($value, FILTER_VALIDATE_EMAIL) !== false,
            'url' => is_string($value) && filter_var($value, FILTER_VALIDATE_URL) !== false,
            'alpha' => is_string($value) && ctype_alpha($value),
            'alpha_numeric' => is_string($value) && ctype_alnum($value),
            'regex' => is_string($value) && preg_match($params[0] ?? '/.*/', $value) === 1,
            'date' => $this->isValidDate($value),
            'json' => is_string($value) && json_decode($value) !== null,
            'in' => in_array($value, $params, true),
            'not_in' => !in_array($value, $params, true),
            'same' => $value === ($params[0] ?? null),
            'different' => $value !== ($params[0] ?? null),
            'confirmed' => $this->checkConfirmation($value, $params[0] ?? null),
            default => throw new InvalidArgumentException("Unknown validation rule: {$rule}")
        };
    }

    /**
     * Check size based on value type
     */
    private function checkSize(mixed $value, int $expectedSize): bool
    {
        if (is_string($value)) {
            return strlen($value) === $expectedSize;
        } elseif (is_array($value)) {
            return count($value) === $expectedSize;
        } elseif (is_numeric($value)) {
            return $value == $expectedSize;
        }
        
        return false;
    }

    /**
     * Check if value is valid date
     */
    private function isValidDate(mixed $value): bool
    {
        if (!is_string($value)) {
            return false;
        }

        $timestamp = strtotime($value);
        return $timestamp !== false && checkdate(
            (int) date('m', $timestamp),
            (int) date('d', $timestamp),
            (int) date('Y', $timestamp)
        );
    }

    /**
     * Check confirmation (e.g., password confirmation)
     */
    private function checkConfirmation(mixed $value, mixed $confirmation): bool
    {
        return $value === $confirmation;
    }

    /**
     * Get nested value from array using dot notation
     */
    private function getNestedValue(array $data, string $key): mixed
    {
        if (str_contains($key, '.')) {
            $keys = explode('.', $key);
            $current = $data;
            
            foreach ($keys as $k) {
                if (!is_array($current) || !array_key_exists($k, $current)) {
                    return null;
                }
                $current = $current[$k];
            }
            
            return $current;
        }

        return $data[$key] ?? null;
    }

    /**
     * Format validation error message
     */
    private function formatError(string $field, string $rule, mixed $params, mixed $value): string
    {
        $fieldDisplay = empty($field) ? 'value' : $field;
        
        return match($rule) {
            'required' => "The {$fieldDisplay} field is required.",
            'string' => "The {$fieldDisplay} must be a string.",
            'integer' => "The {$fieldDisplay} must be an integer.",
            'numeric' => "The {$fieldDisplay} must be a number.",
            'boolean' => "The {$fieldDisplay} must be a boolean.",
            'array' => "The {$fieldDisplay} must be an array.",
            'min' => "The {$fieldDisplay} must be at least {$params[0]}.",
            'max' => "The {$fieldDisplay} must not be greater than {$params[0]}.",
            'min_length' => "The {$fieldDisplay} must be at least {$params[0]} characters.",
            'max_length' => "The {$fieldDisplay} must not be greater than {$params[0]} characters.",
            'email' => "The {$fieldDisplay} must be a valid email address.",
            'url' => "The {$fieldDisplay} must be a valid URL.",
            'alpha' => "The {$fieldDisplay} may only contain letters.",
            'alpha_numeric' => "The {$fieldDisplay} may only contain letters and numbers.",
            'date' => "The {$fieldDisplay} must be a valid date.",
            'json' => "The {$fieldDisplay} must be a valid JSON string.",
            default => "The {$fieldDisplay} is invalid for rule '{$rule}'."
        };
    }

    /**
     * Evaluate condition for conditional validation
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
            'in' => is_array($value) && in_array($fieldValue, $value, true),
            'not_in' => is_array($value) && !in_array($fieldValue, $value, true),
            default => false
        };
    }

    /**
     * Validate field against schema properties
     */
    private function validateSchemaField(mixed $value, array $properties, string $field): array
    {
        $errors = [];
        
        // Type validation
        if (isset($properties['type'])) {
            if (!$this->validateType($value, $properties['type'])) {
                $errors[] = "Field '{$field}' must be of type {$properties['type']}";
            }
        }
        
        // Additional validations based on type
        if (is_string($value)) {
            if (isset($properties['minLength']) && strlen($value) < $properties['minLength']) {
                $errors[] = "Field '{$field}' must be at least {$properties['minLength']} characters";
            }
            if (isset($properties['maxLength']) && strlen($value) > $properties['maxLength']) {
                $errors[] = "Field '{$field}' must not exceed {$properties['maxLength']} characters";
            }
        }
        
        return $errors;
    }

    /**
     * Validate value type against schema type
     */
    private function validateType(mixed $value, string $expectedType): bool
    {
        return match($expectedType) {
            'string' => is_string($value),
            'integer' => is_int($value),
            'number' => is_numeric($value),
            'boolean' => is_bool($value),
            'array' => is_array($value),
            'object' => is_object($value) || is_array($value),
            'null' => $value === null,
            default => true
        };
    }

    /**
     * Flatten validation errors from multiple results
     */
    private function flattenErrors(array $results): array
    {
        $errors = [];
        
        foreach ($results as $field => $result) {
            if (isset($result['errors'])) {
                foreach ($result['errors'] as $error) {
                    $errors[] = $error;
                }
            }
        }
        
        return $errors;
    }

    /**
     * Get built-in validation rules
     */
    private function getBuiltInRules(): array
    {
        return [
            'required', 'nullable', 'filled', 'string', 'integer', 'numeric',
            'boolean', 'array', 'object', 'min', 'max', 'between', 'size',
            'min_length', 'max_length', 'email', 'url', 'alpha', 'alpha_numeric',
            'regex', 'date', 'json', 'in', 'not_in', 'same', 'different', 'confirmed'
        ];
    }
} 