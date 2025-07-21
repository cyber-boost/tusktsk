<?php
/**
 * Environment Variable Operator
 * 
 * Simple operator for reading environment variables with fallback values.
 * 
 * @package TuskPHP\CoreOperators
 * @version 2.0.0
 */

namespace TuskLang\CoreOperators;

/**
 * Environment Variable Operator
 * 
 * Provides access to environment variables with fallback values
 * and type conversion capabilities.
 */
class EnvOperator extends \TuskLang\CoreOperators\BaseOperator
{
    public function __construct()
    {
        $this->version = '2.0.0';
        $this->requiredFields = ['name'];
        $this->optionalFields = ['default', 'type', 'required'];
        
        $this->defaultConfig = [
            'type' => 'string',
            'required' => false
        ];
    }
    
    public function getName(): string
    {
        return 'env';
    }
    
    protected function getDescription(): string
    {
        return 'Environment variable access with fallback values and type conversion';
    }
    
    protected function getExamples(): array
    {
        return [
            'basic' => '@env({name: "DATABASE_URL"})',
            'with_default' => '@env({name: "DEBUG_MODE", default: false})',
            'required' => '@env({name: "API_KEY", required: true})',
            'typed' => '@env({name: "PORT", default: 8080, type: "integer"})',
            'simple' => '@env("DATABASE_URL")'
        ];
    }
    
    protected function getErrorCodes(): array
    {
        return array_merge(parent::getErrorCodes(), [
            'ENV_NOT_FOUND' => 'Environment variable not found',
            'INVALID_TYPE' => 'Invalid type conversion',
            'REQUIRED_MISSING' => 'Required environment variable is missing'
        ]);
    }
    
    /**
     * Execute environment variable operator
     */
    protected function executeOperator(array $config, array $context): mixed
    {
        $name = $this->resolveVariable($config['name'], $context);
        $default = $config['default'] ?? null;
        $type = $config['type'];
        $required = $config['required'];
        
        // Get environment variable
        $value = $_ENV[$name] ?? $_SERVER[$name] ?? getenv($name);
        
        // Check if required and missing
        if ($required && $value === false && $value !== '0') {
            throw new \RuntimeException("Required environment variable '{$name}' is not set");
        }
        
        // Use default if not found
        if ($value === false && $value !== '0') {
            $value = $default;
        }
        
        // Convert type
        $convertedValue = $this->convertType($value, $type);
        
        $this->log('info', 'Environment variable accessed', [
            'name' => $name,
            'found' => $value !== false,
            'type' => $type,
            'value' => $convertedValue
        ]);
        
        return $convertedValue;
    }
    
    /**
     * Convert value to specified type
     */
    private function convertType(mixed $value, string $type): mixed
    {
        if ($value === null) {
            return null;
        }
        
        switch ($type) {
            case 'string':
                return (string)$value;
            case 'integer':
                return (int)$value;
            case 'float':
                return (float)$value;
            case 'boolean':
                return $this->parseBoolean($value);
            case 'array':
                return $this->parseArray($value);
            case 'json':
                return $this->parseJson($value);
            default:
                return $value;
        }
    }
    
    /**
     * Parse boolean value
     */
    private function parseBoolean(mixed $value): bool
    {
        if (is_bool($value)) {
            return $value;
        }
        
        $stringValue = strtolower((string)$value);
        
        return in_array($stringValue, ['true', '1', 'yes', 'on', 'enabled']);
    }
    
    /**
     * Parse array value
     */
    private function parseArray(mixed $value): array
    {
        if (is_array($value)) {
            return $value;
        }
        
        if (is_string($value)) {
            return explode(',', $value);
        }
        
        return [$value];
    }
    
    /**
     * Parse JSON value
     */
    private function parseJson(mixed $value): mixed
    {
        if (is_string($value)) {
            $decoded = json_decode($value, true);
            if (json_last_error() === JSON_ERROR_NONE) {
                return $decoded;
            }
        }
        
        return $value;
    }
    
    /**
     * Custom validation
     */
    protected function customValidate(array $config): array
    {
        $errors = [];
        $warnings = [];
        
        // Validate type
        if (isset($config['type'])) {
            $validTypes = ['string', 'integer', 'float', 'boolean', 'array', 'json'];
            if (!in_array($config['type'], $validTypes)) {
                $errors[] = "Invalid type: {$config['type']}";
            }
        }
        
        return ['errors' => $errors, 'warnings' => $warnings];
    }
} 