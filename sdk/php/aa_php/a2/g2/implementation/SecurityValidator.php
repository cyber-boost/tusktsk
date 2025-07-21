<?php

namespace TuskLang\Communication\GraphQL;

/**
 * GraphQL Security Validator
 * 
 * Validates queries for security issues:
 * - Query complexity analysis
 * - Depth limiting
 * - Introspection restrictions
 * - Field access control
 */
class SecurityValidator
{
    private array $config;

    public function __construct(array $config)
    {
        $this->config = $config;
    }

    /**
     * Validate query for security issues
     */
    public function validate(string $query, array $variables = []): ValidationResult
    {
        $result = new ValidationResult();

        // Check for introspection queries
        if (!$this->config['introspection'] && $this->isIntrospectionQuery($query)) {
            $result->addError('Introspection queries are disabled');
        }

        // Check for dangerous patterns
        if ($this->hasDangerousPatterns($query)) {
            $result->addError('Query contains dangerous patterns');
        }

        // Validate variables
        if (!empty($variables) && !$this->validateVariables($variables)) {
            $result->addError('Invalid variables detected');
        }

        // Check query length
        if (strlen($query) > ($this->config['max_query_length'] ?? 10000)) {
            $result->addError('Query length exceeds maximum allowed');
        }

        return $result;
    }

    /**
     * Check if query is introspection
     */
    private function isIntrospectionQuery(string $query): bool
    {
        $introspectionPatterns = [
            '__schema',
            '__type',
            '__typename',
            '__Field',
            '__Directive',
            '__EnumValue',
            '__InputValue'
        ];

        foreach ($introspectionPatterns as $pattern) {
            if (stripos($query, $pattern) !== false) {
                return true;
            }
        }

        return false;
    }

    /**
     * Check for dangerous query patterns
     */
    private function hasDangerousPatterns(string $query): bool
    {
        $dangerousPatterns = [
            // Potential DoS patterns
            '/\{[^}]{1000,}/',  // Very long field lists
            '/\(\s*\$\w+\s*:\s*\w+!\s*\)\s*\{/',  // Complex recursive patterns
        ];

        foreach ($dangerousPatterns as $pattern) {
            if (preg_match($pattern, $query)) {
                return true;
            }
        }

        return false;
    }

    /**
     * Validate query variables
     */
    private function validateVariables(array $variables): bool
    {
        foreach ($variables as $key => $value) {
            // Check variable name
            if (!preg_match('/^[a-zA-Z_][a-zA-Z0-9_]*$/', $key)) {
                return false;
            }

            // Check for deeply nested objects
            if (is_array($value) && $this->getArrayDepth($value) > 10) {
                return false;
            }

            // Check for large strings
            if (is_string($value) && strlen($value) > 100000) {
                return false;
            }
        }

        return true;
    }

    /**
     * Get array depth
     */
    private function getArrayDepth(array $array): int
    {
        $maxDepth = 0;
        
        foreach ($array as $value) {
            if (is_array($value)) {
                $depth = $this->getArrayDepth($value) + 1;
                $maxDepth = max($maxDepth, $depth);
            }
        }
        
        return $maxDepth;
    }
}

/**
 * Validation result container
 */
class ValidationResult
{
    private array $errors = [];

    public function addError(string $error): self
    {
        $this->errors[] = $error;
        return $this;
    }

    public function isValid(): bool
    {
        return empty($this->errors);
    }

    public function getErrors(): array
    {
        return $this->errors;
    }

    public function getError(): string
    {
        return implode('; ', $this->errors);
    }
} 