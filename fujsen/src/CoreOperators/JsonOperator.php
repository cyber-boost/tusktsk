<?php
/**
 * JSON Operator
 * 
 * JSON manipulation operator for encoding, decoding, and processing
 * JSON data with validation and formatting options.
 * 
 * @package TuskPHP\CoreOperators
 * @version 2.0.0
 */

namespace TuskPHP\CoreOperators;

/**
 * JSON Operator
 * 
 * Provides JSON manipulation capabilities including encoding, decoding,
 * validation, and formatting with various options.
 */
class JsonOperator extends BaseOperator
{
    public function __construct()
    {
        $this->version = '2.0.0';
        $this->requiredFields = ['data'];
        $this->optionalFields = [
            'action', 'pretty', 'validate', 'schema', 'options'
        ];
        
        $this->defaultConfig = [
            'action' => 'encode',
            'pretty' => false,
            'validate' => false,
            'options' => []
        ];
    }
    
    public function getName(): string
    {
        return 'json';
    }
    
    protected function getDescription(): string
    {
        return 'JSON manipulation operator for encoding, decoding, and processing JSON data with validation and formatting options';
    }
    
    protected function getExamples(): array
    {
        return [
            'encode' => '@json({data: {name: "John", age: 30}, pretty: true})',
            'decode' => '@json({action: "decode", data: "{\"name\":\"John\"}"})',
            'validate' => '@json({action: "validate", data: json_string, schema: schema_object})',
            'merge' => '@json({action: "merge", data: [obj1, obj2, obj3]})',
            'extract' => '@json({action: "extract", data: json_object, path: "users[0].name"})',
            'simple' => '@json({name: "value"})'
        ];
    }
    
    protected function getErrorCodes(): array
    {
        return array_merge(parent::getErrorCodes(), [
            'JSON_ENCODE_ERROR' => 'JSON encoding failed',
            'JSON_DECODE_ERROR' => 'JSON decoding failed',
            'INVALID_SCHEMA' => 'Invalid JSON schema',
            'VALIDATION_FAILED' => 'JSON validation failed',
            'INVALID_PATH' => 'Invalid JSON path'
        ]);
    }
    
    /**
     * Execute JSON operator
     */
    protected function executeOperator(array $config, array $context): mixed
    {
        $action = $config['action'];
        $data = $this->resolveVariable($config['data'], $context);
        
        switch ($action) {
            case 'encode':
                return $this->encodeJson($data, $config);
            case 'decode':
                return $this->decodeJson($data, $config);
            case 'validate':
                return $this->validateJson($data, $config, $context);
            case 'merge':
                return $this->mergeJson($data, $config);
            case 'extract':
                return $this->extractFromJson($data, $config, $context);
            case 'transform':
                return $this->transformJson($data, $config, $context);
            default:
                // Default to encode
                return $this->encodeJson($data, $config);
        }
    }
    
    /**
     * Encode data to JSON
     */
    private function encodeJson(mixed $data, array $config): string
    {
        $pretty = $config['pretty'];
        $options = $config['options'] ?? [];
        
        // Build JSON options
        $jsonOptions = 0;
        
        if ($pretty) {
            $jsonOptions |= JSON_PRETTY_PRINT;
        }
        
        if (isset($options['unescaped_slashes']) && $options['unescaped_slashes']) {
            $jsonOptions |= JSON_UNESCAPED_SLASHES;
        }
        
        if (isset($options['unicode_escape']) && $options['unicode_escape']) {
            $jsonOptions |= JSON_UNESCAPED_UNICODE;
        }
        
        if (isset($options['force_object']) && $options['force_object']) {
            $jsonOptions |= JSON_FORCE_OBJECT;
        }
        
        $result = json_encode($data, $jsonOptions);
        
        if ($result === false) {
            $error = json_last_error_msg();
            throw new \RuntimeException("JSON encoding failed: {$error}");
        }
        
        $this->log('info', 'JSON encoded', [
            'pretty' => $pretty,
            'options' => $jsonOptions,
            'length' => strlen($result)
        ]);
        
        return $result;
    }
    
    /**
     * Decode JSON string
     */
    private function decodeJson(string $data, array $config): mixed
    {
        $assoc = $config['assoc'] ?? true;
        $depth = $config['depth'] ?? 512;
        $options = $config['options'] ?? 0;
        
        $result = json_decode($data, $assoc, $depth, $options);
        
        if ($result === null && json_last_error() !== JSON_ERROR_NONE) {
            $error = json_last_error_msg();
            throw new \RuntimeException("JSON decoding failed: {$error}");
        }
        
        $this->log('info', 'JSON decoded', [
            'assoc' => $assoc,
            'depth' => $depth,
            'type' => gettype($result)
        ]);
        
        return $result;
    }
    
    /**
     * Validate JSON against schema
     */
    private function validateJson(mixed $data, array $config, array $context): bool
    {
        $schema = $this->resolveVariable($config['schema'], $context);
        
        if (!$schema) {
            // Simple validation - just check if it's valid JSON
            if (is_string($data)) {
                json_decode($data);
                return json_last_error() === JSON_ERROR_NONE;
            }
            return true;
        }
        
        // Basic schema validation (simplified)
        return $this->validateAgainstSchema($data, $schema);
    }
    
    /**
     * Merge multiple JSON objects
     */
    private function mergeJson(array $data, array $config): array
    {
        $strategy = $config['strategy'] ?? 'recursive';
        
        if (empty($data)) {
            return [];
        }
        
        $result = $data[0];
        
        for ($i = 1; $i < count($data); $i++) {
            $result = $this->mergeObjects($result, $data[$i], $strategy);
        }
        
        $this->log('info', 'JSON merged', [
            'count' => count($data),
            'strategy' => $strategy
        ]);
        
        return $result;
    }
    
    /**
     * Extract value from JSON using path
     */
    private function extractFromJson(mixed $data, array $config, array $context): mixed
    {
        $path = $this->resolveVariable($config['path'], $context);
        
        if (!$path) {
            return $data;
        }
        
        $result = $this->extractByPath($data, $path);
        
        $this->log('info', 'JSON extracted', [
            'path' => $path,
            'found' => $result !== null
        ]);
        
        return $result;
    }
    
    /**
     * Transform JSON data
     */
    private function transformJson(mixed $data, array $config, array $context): mixed
    {
        $transform = $this->resolveVariable($config['transform'], $context);
        
        if (!$transform || !is_callable($transform)) {
            return $data;
        }
        
        $result = call_user_func($transform, $data);
        
        $this->log('info', 'JSON transformed', [
            'transform' => gettype($transform)
        ]);
        
        return $result;
    }
    
    /**
     * Validate data against schema
     */
    private function validateAgainstSchema(mixed $data, array $schema): bool
    {
        // Basic schema validation (simplified implementation)
        foreach ($schema as $key => $rule) {
            if (!isset($data[$key])) {
                if (isset($rule['required']) && $rule['required']) {
                    return false;
                }
                continue;
            }
            
            $value = $data[$key];
            
            // Type validation
            if (isset($rule['type'])) {
                if (!$this->validateType($value, $rule['type'])) {
                    return false;
                }
            }
            
            // Pattern validation
            if (isset($rule['pattern']) && is_string($value)) {
                if (!preg_match($rule['pattern'], $value)) {
                    return false;
                }
            }
            
            // Range validation
            if (isset($rule['min']) && is_numeric($value)) {
                if ($value < $rule['min']) {
                    return false;
                }
            }
            
            if (isset($rule['max']) && is_numeric($value)) {
                if ($value > $rule['max']) {
                    return false;
                }
            }
        }
        
        return true;
    }
    
    /**
     * Validate value type
     */
    private function validateType(mixed $value, string $type): bool
    {
        switch ($type) {
            case 'string':
                return is_string($value);
            case 'number':
                return is_numeric($value);
            case 'integer':
                return is_int($value);
            case 'boolean':
                return is_bool($value);
            case 'array':
                return is_array($value);
            case 'object':
                return is_object($value);
            case 'null':
                return $value === null;
            default:
                return true;
        }
    }
    
    /**
     * Merge two objects
     */
    private function mergeObjects(array $obj1, array $obj2, string $strategy): array
    {
        if ($strategy === 'replace') {
            return array_merge($obj1, $obj2);
        }
        
        // Recursive merge
        $result = $obj1;
        
        foreach ($obj2 as $key => $value) {
            if (isset($result[$key]) && is_array($result[$key]) && is_array($value)) {
                $result[$key] = $this->mergeObjects($result[$key], $value, $strategy);
            } else {
                $result[$key] = $value;
            }
        }
        
        return $result;
    }
    
    /**
     * Extract value by path
     */
    private function extractByPath(mixed $data, string $path): mixed
    {
        $parts = explode('.', $path);
        $current = $data;
        
        foreach ($parts as $part) {
            // Handle array access
            if (preg_match('/^(.+)\[(\d+)\]$/', $part, $matches)) {
                $key = $matches[1];
                $index = (int)$matches[2];
                
                if (!isset($current[$key]) || !is_array($current[$key])) {
                    return null;
                }
                
                if (!isset($current[$key][$index])) {
                    return null;
                }
                
                $current = $current[$key][$index];
            } else {
                if (!isset($current[$part])) {
                    return null;
                }
                
                $current = $current[$part];
            }
        }
        
        return $current;
    }
    
    /**
     * Custom validation
     */
    protected function customValidate(array $config): array
    {
        $errors = [];
        $warnings = [];
        
        // Validate action
        if (isset($config['action'])) {
            $validActions = ['encode', 'decode', 'validate', 'merge', 'extract', 'transform'];
            if (!in_array($config['action'], $validActions)) {
                $errors[] = "Invalid action: {$config['action']}";
            }
        }
        
        // Validate options
        if (isset($config['options']) && !is_array($config['options'])) {
            $errors[] = "Options must be an array";
        }
        
        return ['errors' => $errors, 'warnings' => $warnings];
    }
} 