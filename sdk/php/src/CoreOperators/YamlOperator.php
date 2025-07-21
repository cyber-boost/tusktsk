<?php

namespace TuskLang\CoreOperators;

use TuskLang\CoreOperators\BaseOperator;

/**
 * YAML Operator for parsing, generating, and manipulating YAML data
 */
class YamlOperator extends BaseOperator
{
    public function execute(array $config, array $context = []): mixed
    {
        $action = $config['action'] ?? 'parse';
        $data = $config['data'] ?? '';
        $options = $config['options'] ?? [];

        try {
            switch ($action) {
                case 'parse':
                    return $this->parseYaml($data, $options);
                case 'generate':
                    return $this->generateYaml($data, $options);
                case 'validate':
                    return $this->validateYaml($data, $options);
                case 'merge':
                    return $this->mergeYaml($data, $options);
                case 'extract':
                    return $this->extractYaml($data, $options);
                case 'transform':
                    return $this->transformYaml($data, $options);
                default:
                    throw new \Exception("Unknown YAML action: $action");
            }
        } catch (\Exception $e) {
            error_log("YAML Operator error: " . $e->getMessage());
            throw $e;
        }
    }

    /**
     * Parse YAML string to array
     */
    private function parseYaml(string $yamlString, array $options): array
    {
        $pos = $options['pos'] ?? 0;
        $ndocs = $options['ndocs'] ?? null;
        $callbacks = $options['callbacks'] ?? [];

        if (!function_exists('yaml_parse')) {
            // Fallback to Symfony YAML if PHP YAML extension not available
            return $this->parseYamlFallback($yamlString, $options);
        }

        $result = yaml_parse($yamlString, $pos, $ndocs, $callbacks);
        
        if ($result === false) {
            throw new \Exception("YAML parsing failed");
        }

        return $result;
    }

    /**
     * Generate YAML from array
     */
    private function generateYaml(array $data, array $options): string
    {
        $encoding = $options['encoding'] ?? YAML_UTF8_ENCODING;
        $linebreak = $options['linebreak'] ?? YAML_LN_BREAK;
        $indent = $options['indent'] ?? 2;

        if (!function_exists('yaml_emit')) {
            // Fallback to Symfony YAML if PHP YAML extension not available
            return $this->generateYamlFallback($data, $options);
        }

        $result = yaml_emit($data, $encoding, $linebreak);
        
        if ($result === false) {
            throw new \Exception("YAML generation failed");
        }

        return $result;
    }

    /**
     * Validate YAML structure
     */
    private function validateYaml(string $yamlString, array $options): array
    {
        $schema = $options['schema'] ?? null;
        $required = $options['required'] ?? [];
        $types = $options['types'] ?? [];

        try {
            $parsed = $this->parseYaml($yamlString, $options);
            
            $errors = [];
            
            // Check required fields
            foreach ($required as $field) {
                if (!isset($parsed[$field])) {
                    $errors[] = "Required field '$field' is missing";
                }
            }
            
            // Check field types
            foreach ($types as $field => $expectedType) {
                if (isset($parsed[$field])) {
                    $actualType = gettype($parsed[$field]);
                    if ($actualType !== $expectedType) {
                        $errors[] = "Field '$field' should be $expectedType, got $actualType";
                    }
                }
            }
            
            // Validate against schema if provided
            if ($schema) {
                $schemaErrors = $this->validateAgainstSchema($parsed, $schema);
                $errors = array_merge($errors, $schemaErrors);
            }

            return [
                'valid' => empty($errors),
                'errors' => $errors,
                'data' => $parsed
            ];
        } catch (\Exception $e) {
            return [
                'valid' => false,
                'errors' => [$e->getMessage()],
                'data' => null
            ];
        }
    }

    /**
     * Merge multiple YAML documents
     */
    private function mergeYaml(array $yamlStrings, array $options): string
    {
        $strategy = $options['strategy'] ?? 'deep'; // deep, shallow, replace
        $overwrite = $options['overwrite'] ?? true;

        $merged = [];
        
        foreach ($yamlStrings as $yamlString) {
            $parsed = $this->parseYaml($yamlString, $options);
            
            if ($strategy === 'deep') {
                $merged = $this->deepMerge($merged, $parsed, $overwrite);
            } elseif ($strategy === 'shallow') {
                $merged = array_merge($merged, $parsed);
            } else { // replace
                $merged = $parsed;
            }
        }

        return $this->generateYaml($merged, $options);
    }

    /**
     * Extract specific values from YAML
     */
    private function extractYaml(string $yamlString, array $options): array
    {
        $paths = $options['paths'] ?? [];
        $parsed = $this->parseYaml($yamlString, $options);
        
        $extracted = [];
        
        foreach ($paths as $path) {
            $value = $this->getNestedValue($parsed, $path);
            if ($value !== null) {
                $extracted[$path] = $value;
            }
        }
        
        return $extracted;
    }

    /**
     * Transform YAML data structure
     */
    private function transformYaml(string $yamlString, array $options): string
    {
        $transforms = $options['transforms'] ?? [];
        $parsed = $this->parseYaml($yamlString, $options);
        
        foreach ($transforms as $transform) {
            $type = $transform['type'] ?? '';
            $path = $transform['path'] ?? '';
            $value = $transform['value'] ?? null;
            
            switch ($type) {
                case 'set':
                    $this->setNestedValue($parsed, $path, $value);
                    break;
                case 'delete':
                    $this->deleteNestedValue($parsed, $path);
                    break;
                case 'rename':
                    $newPath = $transform['newPath'] ?? '';
                    $this->renameNestedValue($parsed, $path, $newPath);
                    break;
                case 'append':
                    $this->appendNestedValue($parsed, $path, $value);
                    break;
            }
        }
        
        return $this->generateYaml($parsed, $options);
    }

    /**
     * Fallback YAML parsing using Symfony YAML
     */
    private function parseYamlFallback(string $yamlString, array $options): array
    {
        // Simple YAML parser fallback
        $lines = explode("\n", $yamlString);
        $result = [];
        $currentPath = [];
        
        foreach ($lines as $line) {
            $line = trim($line);
            if (empty($line) || strpos($line, '#') === 0) {
                continue;
            }
            
            $indent = strlen($line) - strlen(ltrim($line));
            $level = $indent / 2;
            
            // Adjust current path based on indentation
            while (count($currentPath) > $level) {
                array_pop($currentPath);
            }
            
            if (strpos($line, ':') !== false) {
                list($key, $value) = explode(':', $line, 2);
                $key = trim($key);
                $value = trim($value);
                
                if (empty($value)) {
                    // This is a parent key
                    $currentPath[] = $key;
                } else {
                    // This is a leaf key
                    $this->setNestedValue($result, implode('.', $currentPath) . '.' . $key, $value);
                }
            }
        }
        
        return $result;
    }

    /**
     * Fallback YAML generation
     */
    private function generateYamlFallback(array $data, array $options): string
    {
        $indent = $options['indent'] ?? 2;
        return $this->arrayToYaml($data, 0, $indent);
    }

    /**
     * Convert array to YAML string
     */
    private function arrayToYaml(array $data, int $level, int $indent): string
    {
        $yaml = '';
        $spaces = str_repeat(' ', $level * $indent);
        
        foreach ($data as $key => $value) {
            if (is_array($value)) {
                $yaml .= $spaces . $key . ":\n";
                $yaml .= $this->arrayToYaml($value, $level + 1, $indent);
            } else {
                $yaml .= $spaces . $key . ': ' . $this->formatValue($value) . "\n";
            }
        }
        
        return $yaml;
    }

    /**
     * Format value for YAML output
     */
    private function formatValue($value): string
    {
        if (is_string($value)) {
            if (strpos($value, "\n") !== false || strpos($value, '"') !== false) {
                return '"' . addcslashes($value, '"\\') . '"';
            }
            return $value;
        } elseif (is_bool($value)) {
            return $value ? 'true' : 'false';
        } elseif (is_null($value)) {
            return 'null';
        }
        
        return (string)$value;
    }

    /**
     * Deep merge arrays
     */
    private function deepMerge(array $array1, array $array2, bool $overwrite): array
    {
        foreach ($array2 as $key => $value) {
            if (isset($array1[$key]) && is_array($array1[$key]) && is_array($value)) {
                $array1[$key] = $this->deepMerge($array1[$key], $value, $overwrite);
            } elseif ($overwrite || !isset($array1[$key])) {
                $array1[$key] = $value;
            }
        }
        
        return $array1;
    }

    /**
     * Get nested value by dot notation path
     */
    private function getNestedValue(array $array, string $path)
    {
        $keys = explode('.', $path);
        $current = $array;
        
        foreach ($keys as $key) {
            if (!isset($current[$key])) {
                return null;
            }
            $current = $current[$key];
        }
        
        return $current;
    }

    /**
     * Set nested value by dot notation path
     */
    private function setNestedValue(array &$array, string $path, $value): void
    {
        $keys = explode('.', $path);
        $current = &$array;
        
        foreach ($keys as $key) {
            if (!isset($current[$key])) {
                $current[$key] = [];
            }
            $current = &$current[$key];
        }
        
        $current = $value;
    }

    /**
     * Delete nested value by dot notation path
     */
    private function deleteNestedValue(array &$array, string $path): void
    {
        $keys = explode('.', $path);
        $current = &$array;
        
        foreach ($keys as $i => $key) {
            if (!isset($current[$key])) {
                return;
            }
            
            if ($i === count($keys) - 1) {
                unset($current[$key]);
            } else {
                $current = &$current[$key];
            }
        }
    }

    /**
     * Rename nested value
     */
    private function renameNestedValue(array &$array, string $oldPath, string $newPath): void
    {
        $value = $this->getNestedValue($array, $oldPath);
        if ($value !== null) {
            $this->setNestedValue($array, $newPath, $value);
            $this->deleteNestedValue($array, $oldPath);
        }
    }

    /**
     * Append to nested array value
     */
    private function appendNestedValue(array &$array, string $path, $value): void
    {
        $current = $this->getNestedValue($array, $path);
        if (!is_array($current)) {
            $current = [];
        }
        
        $current[] = $value;
        $this->setNestedValue($array, $path, $current);
    }

    /**
     * Validate against schema
     */
    private function validateAgainstSchema(array $data, array $schema): array
    {
        $errors = [];
        
        foreach ($schema as $field => $rules) {
            if (isset($data[$field])) {
                $value = $data[$field];
                
                if (isset($rules['type'])) {
                    $expectedType = $rules['type'];
                    $actualType = gettype($value);
                    
                    if ($actualType !== $expectedType) {
                        $errors[] = "Field '$field' should be $expectedType, got $actualType";
                    }
                }
                
                if (isset($rules['min']) && is_numeric($value) && $value < $rules['min']) {
                    $errors[] = "Field '$field' should be >= {$rules['min']}";
                }
                
                if (isset($rules['max']) && is_numeric($value) && $value > $rules['max']) {
                    $errors[] = "Field '$field' should be <= {$rules['max']}";
                }
                
                if (isset($rules['pattern']) && is_string($value) && !preg_match($rules['pattern'], $value)) {
                    $errors[] = "Field '$field' does not match pattern {$rules['pattern']}";
                }
            } elseif (isset($rules['required']) && $rules['required']) {
                $errors[] = "Required field '$field' is missing";
            }
        }
        
        return $errors;
    }
} 