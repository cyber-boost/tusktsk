<?php
/**
 * Base Operator Class
 * 
 * Provides common functionality for all @ operators including
 * validation, error handling, and context management.
 * 
 * @package TuskPHP\CoreOperators
 * @version 2.0.0
 */

namespace TuskPHP\CoreOperators;

use TuskPHP\Core\OperatorPlugin;

/**
 * Base Operator Class
 * 
 * All operators should extend this class to get common functionality
 * like validation, error handling, and context management.
 */
abstract class BaseOperator implements OperatorPlugin
{
    protected array $defaultConfig = [];
    protected array $requiredFields = [];
    protected array $optionalFields = [];
    protected string $version = '1.0.0';
    protected bool $debugMode = false;
    
    /**
     * Get operator name
     */
    abstract public function getName(): string;
    
    /**
     * Get operator version
     */
    public function getVersion(): string
    {
        return $this->version;
    }
    
    /**
     * Get operator schema
     */
    public function getSchema(): array
    {
        return [
            'name' => $this->getName(),
            'version' => $this->getVersion(),
            'description' => $this->getDescription(),
            'required_fields' => $this->requiredFields,
            'optional_fields' => $this->optionalFields,
            'default_config' => $this->defaultConfig,
            'examples' => $this->getExamples(),
            'error_codes' => $this->getErrorCodes()
        ];
    }
    
    /**
     * Get operator description
     */
    protected function getDescription(): string
    {
        return 'Base operator description - override in subclasses';
    }
    
    /**
     * Get usage examples
     */
    protected function getExamples(): array
    {
        return [
            'basic' => "@{$this->getName()}(\"example\")",
            'advanced' => "@{$this->getName()}({param: \"value\"})"
        ];
    }
    
    /**
     * Get error codes
     */
    protected function getErrorCodes(): array
    {
        return [
            'INVALID_CONFIG' => 'Invalid configuration provided',
            'EXECUTION_FAILED' => 'Operator execution failed',
            'TIMEOUT' => 'Operation timed out',
            'CONNECTION_FAILED' => 'Connection to external service failed'
        ];
    }
    
    /**
     * Validate configuration
     */
    public function validate(array $config): array
    {
        $errors = [];
        $warnings = [];
        
        // Check required fields
        foreach ($this->requiredFields as $field) {
            if (!isset($config[$field])) {
                $errors[] = "Required field '{$field}' is missing";
            }
        }
        
        // Validate field types and values
        $validation = $this->validateFields($config);
        $errors = array_merge($errors, $validation['errors']);
        $warnings = array_merge($warnings, $validation['warnings']);
        
        // Custom validation
        $customValidation = $this->customValidate($config);
        $errors = array_merge($errors, $customValidation['errors']);
        $warnings = array_merge($warnings, $customValidation['warnings']);
        
        return [
            'valid' => empty($errors),
            'errors' => $errors,
            'warnings' => $warnings
        ];
    }
    
    /**
     * Validate individual fields
     */
    protected function validateFields(array $config): array
    {
        $errors = [];
        $warnings = [];
        
        foreach ($config as $field => $value) {
            $validation = $this->validateField($field, $value);
            if (!empty($validation['errors'])) {
                $errors = array_merge($errors, $validation['errors']);
            }
            if (!empty($validation['warnings'])) {
                $warnings = array_merge($warnings, $validation['warnings']);
            }
        }
        
        return ['errors' => $errors, 'warnings' => $warnings];
    }
    
    /**
     * Validate a single field
     */
    protected function validateField(string $field, mixed $value): array
    {
        $errors = [];
        $warnings = [];
        
        // Check if field is known
        if (!in_array($field, array_merge($this->requiredFields, $this->optionalFields))) {
            $warnings[] = "Unknown field '{$field}'";
        }
        
        // Type validation
        $typeValidation = $this->validateFieldType($field, $value);
        if (!empty($typeValidation['errors'])) {
            $errors = array_merge($errors, $typeValidation['errors']);
        }
        
        return ['errors' => $errors, 'warnings' => $warnings];
    }
    
    /**
     * Validate field type
     */
    protected function validateFieldType(string $field, mixed $value): array
    {
        $errors = [];
        
        // Override in subclasses for specific type validation
        return ['errors' => $errors];
    }
    
    /**
     * Custom validation logic
     */
    protected function customValidate(array $config): array
    {
        // Override in subclasses for custom validation
        return ['errors' => [], 'warnings' => []];
    }
    
    /**
     * Execute operator
     */
    public function execute(array $config, array $context): mixed
    {
        try {
            // Merge with default configuration
            $config = array_merge($this->defaultConfig, $config);
            
            // Pre-execution hooks
            $this->beforeExecute($config, $context);
            
            // Execute the operator
            $result = $this->executeOperator($config, $context);
            
            // Post-execution hooks
            $this->afterExecute($config, $context, $result);
            
            return $result;
            
        } catch (\Exception $e) {
            $this->handleError($e, $config, $context);
            throw $e;
        }
    }
    
    /**
     * Execute the actual operator logic
     */
    abstract protected function executeOperator(array $config, array $context): mixed;
    
    /**
     * Pre-execution hook
     */
    protected function beforeExecute(array $config, array $context): void
    {
        if ($this->debugMode) {
            error_log("Executing @{$this->getName()} with config: " . json_encode($config));
        }
    }
    
    /**
     * Post-execution hook
     */
    protected function afterExecute(array $config, array $context, mixed $result): void
    {
        if ($this->debugMode) {
            error_log("Completed @{$this->getName()} with result: " . json_encode($result));
        }
    }
    
    /**
     * Handle execution errors
     */
    protected function handleError(\Exception $e, array $config, array $context): void
    {
        $errorContext = [
            'operator' => $this->getName(),
            'config' => $config,
            'context' => $context,
            'error' => $e->getMessage(),
            'trace' => $e->getTraceAsString()
        ];
        
        error_log("Operator @{$this->getName()} error: " . json_encode($errorContext));
        
        // Emit metrics if available
        $this->emitErrorMetric($e);
    }
    
    /**
     * Emit error metric
     */
    protected function emitErrorMetric(\Exception $e): void
    {
        // Override in subclasses to emit specific error metrics
    }
    
    /**
     * Get context value with fallback
     */
    protected function getContextValue(array $context, string $key, mixed $default = null): mixed
    {
        return $context[$key] ?? $default;
    }
    
    /**
     * Resolve variable references
     */
    protected function resolveVariable(mixed $value, array $context): mixed
    {
        if (is_array($value) && isset($value['variable'])) {
            $varName = $value['variable'];
            return $this->getContextValue($context, $varName);
        }
        
        return $value;
    }
    
    /**
     * Set debug mode
     */
    public function setDebugMode(bool $enabled): void
    {
        $this->debugMode = $enabled;
    }
    
    /**
     * Cleanup resources
     */
    public function cleanup(): void
    {
        // Override in subclasses to cleanup resources
    }
    
    /**
     * Create structured error response
     */
    protected function createErrorResponse(string $code, string $message, array $context = []): array
    {
        return [
            'error' => $code,
            'operator' => "@{$this->getName()}",
            'message' => $message,
            'context' => $context,
            'suggestions' => $this->getErrorSuggestions($code),
            'trace_id' => $this->generateTraceId()
        ];
    }
    
    /**
     * Get error suggestions
     */
    protected function getErrorSuggestions(string $code): array
    {
        $suggestions = [
            'INVALID_CONFIG' => [
                'Check the operator documentation for required fields',
                'Verify all field types are correct',
                'Ensure all required dependencies are available'
            ],
            'EXECUTION_FAILED' => [
                'Check external service availability',
                'Verify authentication credentials',
                'Review error logs for detailed information'
            ],
            'TIMEOUT' => [
                'Increase timeout configuration',
                'Check network connectivity',
                'Consider using retry logic'
            ],
            'CONNECTION_FAILED' => [
                'Verify service endpoint is correct',
                'Check firewall and network settings',
                'Ensure authentication is properly configured'
            ]
        ];
        
        return $suggestions[$code] ?? ['Review the operator documentation for troubleshooting'];
    }
    
    /**
     * Generate trace ID
     */
    protected function generateTraceId(): string
    {
        return uniqid($this->getName() . '-', true);
    }
    
    /**
     * Log operation
     */
    protected function log(string $level, string $message, array $context = []): void
    {
        $logEntry = [
            'timestamp' => date('Y-m-d H:i:s'),
            'level' => $level,
            'operator' => $this->getName(),
            'message' => $message,
            'context' => $context
        ];
        
        error_log(json_encode($logEntry));
    }
} 