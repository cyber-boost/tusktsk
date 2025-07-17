<?php
/**
 * 🐘 TuskLang Enhanced - The Freedom Parser
 * ========================================
 * "We don't bow to any king" - Support ALL syntax styles
 * 
 * Features:
 * - Multiple grouping styles: [], {}, <>
 * - $global vs section-local variables
 * - Cross-file communication
 * - Flexible, developer-friendly syntax
 * 
 * PHP: The mother of all mothers!
 */

namespace TuskPHP\Utils;

require_once __DIR__ . '/TuskLang.php';

class TuskLangEnhanced extends TuskLang
{
    /**
     * Enhanced parser with full flexibility
     */
    public static function parse(string $content, bool $asObject = false): array|object
    {
        $parser = new TuskLangEnhancedParser($content);
        $result = $parser->parse();
        
        return $asObject ? (object) $result : $result;
    }
}

/**
 * Enhanced Parser - Supports ALL syntax styles
 */
class TuskLangEnhancedParser extends TuskLangParser
{
    private $content;
    private $lines;
    private $position = 0;
    private $currentSection = null;
    private $globalVariables = [];  // $var globals
    private $sectionVariables = []; // section-local vars
    private $parsedData = [];
    private $crossFileCache = [];   // Cache for @file.tsk operations
    
    public function __construct(string $content)
    {
        $this->content = $content;
        $this->lines = explode("\n", $content);
    }
    
    public function parse(): array
    {
        $result = [];
        
        while ($this->position < count($this->lines)) {
            $line = $this->lines[$this->position];
            $trimmedLine = trim($line);
            
            // Skip empty lines and comments
            if (empty($trimmedLine) || $this->isCommentLine($trimmedLine)) {
                $this->position++;
                continue;
            }
            
            // Remove optional semicolon at end
            $trimmedLine = rtrim($trimmedLine, ';');
            
            // Check for section declarations
            if ($this->isSectionDeclaration($trimmedLine)) {
                $this->currentSection = $this->parseSectionDeclaration($trimmedLine);
                if (!isset($result[$this->currentSection])) {
                    $result[$this->currentSection] = [];
                }
                $this->sectionVariables[$this->currentSection] = [];
            }
            // Check for angle bracket object start
            elseif ($this->isAngleBracketObject($trimmedLine)) {
                $obj = $this->parseAngleBracketObject($trimmedLine);
                if ($this->currentSection) {
                    $result[$this->currentSection][$obj['key']] = $obj['value'];
                } else {
                    $result[$obj['key']] = $obj['value'];
                }
            }
            // Check for curly brace object
            elseif ($this->isObject($trimmedLine)) {
                $obj = $this->parseObject($trimmedLine);
                if ($this->currentSection) {
                    $result[$this->currentSection][$obj['key']] = $obj['value'];
                } else {
                    $result[$obj['key']] = $obj['value'];
                }
            }
            // Parse key-value pairs
            elseif ($this->isKeyValue($trimmedLine)) {
                $kv = $this->parseEnhancedKeyValue($trimmedLine);
                
                // Store in appropriate location
                if ($this->currentSection) {
                    $result[$this->currentSection][$kv['key']] = $kv['value'];
                    
                    // Store section-local variable if not $global
                    if (!str_starts_with($kv['key'], '$')) {
                        $this->sectionVariables[$this->currentSection][$kv['key']] = $kv['value'];
                    }
                } else {
                    $result[$kv['key']] = $kv['value'];
                }
                
                // Store global variables
                if (str_starts_with($kv['key'], '$')) {
                    $this->globalVariables[substr($kv['key'], 1)] = $kv['value'];
                }
                
                $this->parsedData[$kv['key']] = $kv['value'];
            }
            
            $this->position++;
        }
        
        return $this->resolveAllReferences($result);
    }
    
    private function isCommentLine(string $line): bool
    {
        return preg_match('/^\s*(#|\/\/|\/\*)/', $line);
    }
    
    private function isObject(string $line): bool
    {
        return preg_match('/^[a-zA-Z_][a-zA-Z0-9_]*\s*\{/', $line);
    }
    
    private function parseObject(string $line): array
    {
        preg_match('/^([a-zA-Z_][a-zA-Z0-9_]*)\s*\{/', $line, $matches);
        $key = trim($matches[1]);
        
        $this->position++; // Move past opening line
        $obj = [];
        
        while ($this->position < count($this->lines)) {
            $line = trim($this->lines[$this->position]);
            
            // End of object
            if ($line === '}') {
                break;
            }
            
            // Skip empty lines and comments
            if (empty($line) || $this->isCommentLine($line)) {
                $this->position++;
                continue;
            }
            
            // Remove optional semicolon
            $line = rtrim($line, ';');
            
            // Parse nested content
            if ($this->isKeyValue($line)) {
                $kv = $this->parseEnhancedKeyValue($line);
                $obj[$kv['key']] = $kv['value'];
            } elseif ($this->isObject($line)) {
                $nested = $this->parseObject($line);
                $obj[$nested['key']] = $nested['value'];
            } elseif ($this->isAngleBracketObject($line)) {
                $nested = $this->parseAngleBracketObject($line);
                $obj[$nested['key']] = $nested['value'];
            }
            
            $this->position++;
        }
        
        return ['key' => $key, 'value' => $obj];
    }
    
    private function isSectionDeclaration(string $line): bool
    {
        return preg_match('/^\[([a-zA-Z_][a-zA-Z0-9_]*)\]$/', $line);
    }
    
    private function parseSectionDeclaration(string $line): string
    {
        preg_match('/^\[([a-zA-Z_][a-zA-Z0-9_]*)\]$/', $line, $matches);
        return $matches[1];
    }
    
    private function isAngleBracketObject(string $line): bool
    {
        return preg_match('/^([a-zA-Z_][a-zA-Z0-9_]*)\s*>$/', $line);
    }
    
    private function parseAngleBracketObject(string $line): array
    {
        preg_match('/^([a-zA-Z_][a-zA-Z0-9_]*)\s*>$/', $line, $matches);
        $key = trim($matches[1]);
        
        $this->position++; // Move past opening line
        $obj = [];
        
        while ($this->position < count($this->lines)) {
            $line = trim($this->lines[$this->position]);
            
            // End of angle bracket object
            if ($line === '<') {
                break;
            }
            
            // Skip empty lines and comments
            if (empty($line) || $this->isCommentLine($line)) {
                $this->position++;
                continue;
            }
            
            // Remove optional semicolon
            $line = rtrim($line, ';');
            
            // Parse nested content
            if ($this->isKeyValue($line)) {
                $kv = $this->parseEnhancedKeyValue($line);
                $obj[$kv['key']] = $kv['value'];
            } elseif ($this->isObject($line)) {
                $nested = $this->parseObject($line);
                $obj[$nested['key']] = $nested['value'];
            } elseif ($this->isAngleBracketObject($line)) {
                $nested = $this->parseAngleBracketObject($line);
                $obj[$nested['key']] = $nested['value'];
            }
            
            $this->position++;
        }
        
        return ['key' => $key, 'value' => $obj];
    }
    
    protected function isKeyValue(string $line): bool
    {
        // Support both : and = for assignment
        return preg_match('/^[$]?[a-zA-Z_][a-zA-Z0-9_\-]*\s*[:=]\s*.+/', $line);
    }
    
    private function parseEnhancedKeyValue(string $line): array
    {
        // Support both : and = for assignment
        preg_match('/^([$]?[a-zA-Z_][a-zA-Z0-9_\-]*)\s*[:=]\s*(.+)$/', $line, $matches);
        
        $key = trim($matches[1]);
        $value = $this->parseEnhancedValue(trim($matches[2]));
        
        return ['key' => $key, 'value' => $value];
    }
    
    protected function parseValue($value)
    {
        return $this->parseEnhancedValue($value);
    }
    
    private function parseEnhancedValue($value)
    {
        $value = trim($value);
        
        // Handle basic types first
        if ($value === 'true') return true;
        if ($value === 'false') return false;
        if ($value === 'null') return null;
        
        // $variable references (global)
        if (preg_match('/^\$([a-zA-Z_][a-zA-Z0-9_]*)$/', $value, $matches)) {
            return $this->globalVariables[$matches[1]] ?? null;
        }
        
        // Section-local variable references (no $)
        if (preg_match('/^([a-zA-Z_][a-zA-Z0-9_]*)$/', $value, $matches) && 
            $this->currentSection && 
            isset($this->sectionVariables[$this->currentSection][$matches[1]])) {
            return $this->sectionVariables[$this->currentSection][$matches[1]];
        }
        
        // Cross-file references: @file.tsk.get('key')
        if (preg_match('/^@([a-zA-Z0-9_\-]+)\.tsk\.get\([\'"]([^\'"]+)[\'"]\)$/', $value, $matches)) {
            return $this->crossFileGet($matches[1], $matches[2]);
        }
        
        // Cross-file set: @file.tsk.set('key', value)
        if (preg_match('/^@([a-zA-Z0-9_\-]+)\.tsk\.set\([\'"]([^\'"]+)[\'"],\s*(.+)\)$/', $value, $matches)) {
            return $this->crossFileSet($matches[1], $matches[2], $this->parseEnhancedValue($matches[3]));
        }
        
        // Date function: @date('Y-m-d')
        if (preg_match('/^@date\([\'"]([^\'"]+)[\'"]\)$/', $value, $matches)) {
            return date($matches[1]);
        }
        
        // Ranges: 8888-9999
        if (preg_match('/^(\d+)-(\d+)$/', $value, $matches)) {
            return [
                'min' => (int)$matches[1],
                'max' => (int)$matches[2],
                'type' => 'range'
            ];
        }
        
        // @ operators (enhanced)
        if (preg_match('/^@([a-zA-Z_][a-zA-Z0-9_]*)\((.*)\)$/', $value, $matches)) {
            return $this->executeOperator($matches[1], $matches[2]);
        }
        
        // Fall back to parent parsing for all other cases
        return parent::parseValue($value);
    }
    
    private function crossFileGet(string $filename, string $key)
    {
        $fullPath = $this->resolveFilePath($filename . '.tsk');
        
        // Check cache first
        $cacheKey = $fullPath . ':' . $key;
        if (isset($this->crossFileCache[$cacheKey])) {
            return $this->crossFileCache[$cacheKey];
        }
        
        // Parse the file
        if (file_exists($fullPath)) {
            $content = file_get_contents($fullPath);
            $parser = new self($content);
            $data = $parser->parse();
            
            // Navigate to the key (supports dot notation)
            $result = $this->navigateToKey($data, $key);
            
            // Cache the result
            $this->crossFileCache[$cacheKey] = $result;
            
            return $result;
        }
        
        return null;
    }
    
    private function crossFileSet(string $filename, string $key, $value)
    {
        $fullPath = $this->resolveFilePath($filename . '.tsk');
        
        // Read existing file or create new data
        $data = [];
        if (file_exists($fullPath)) {
            $content = file_get_contents($fullPath);
            $parser = new self($content);
            $data = $parser->parse();
        }
        
        // Set the value (supports dot notation)
        $this->setNestedValue($data, $key, $value);
        
        // Write back to file
        $serialized = TuskLang::serialize($data);
        file_put_contents($fullPath, $serialized);
        
        // Update cache
        $cacheKey = $fullPath . ':' . $key;
        $this->crossFileCache[$cacheKey] = $value;
        
        return $value;
    }
    
    private function navigateToKey(array $data, string $key)
    {
        $parts = explode('.', $key);
        $current = $data;
        
        foreach ($parts as $part) {
            if (isset($current[$part])) {
                $current = $current[$part];
            } else {
                return null;
            }
        }
        
        return $current;
    }
    
    private function setNestedValue(array &$data, string $key, $value)
    {
        $parts = explode('.', $key);
        $current = &$data;
        
        for ($i = 0; $i < count($parts) - 1; $i++) {
            $part = $parts[$i];
            if (!isset($current[$part])) {
                $current[$part] = [];
            }
            $current = &$current[$part];
        }
        
        $current[$parts[count($parts) - 1]] = $value;
    }
    
    private function executeOperator(string $operator, string $params): mixed
    {
        switch ($operator) {
            case 'date':
                $format = trim($params, '"\'');
                return date($format);
                
            case 'cache':
                // Parse cache parameters
                if (preg_match('/^[\'"]([^\'"]+)[\'"],\s*(.+)$/', $params, $matches)) {
                    $ttl = $matches[1];
                    $value = $this->parseEnhancedValue($matches[2]);
                    return $this->cacheValue($ttl, $value);
                }
                break;
                
            case 'learn':
            case 'optimize':
            case 'metrics':
                // These would integrate with FUJSEN's advanced features
                return $this->executeFujsenOperator($operator, $params);
                
            default:
                // Unknown operator, return as-is for backward compatibility
                return "@{$operator}({$params})";
        }
    }
    
    private function cacheValue(string $ttl, $value)
    {
        // Simple in-memory cache for now
        // In production, this would use Redis/Memcached
        static $cache = [];
        
        $key = md5(serialize($value));
        
        if (isset($cache[$key]) && $cache[$key]['expires'] > time()) {
            return $cache[$key]['value'];
        }
        
        // Parse TTL (5m, 1h, etc)
        $seconds = $this->parseTTL($ttl);
        
        $cache[$key] = [
            'value' => $value,
            'expires' => time() + $seconds
        ];
        
        return $value;
    }
    
    private function parseTTL(string $ttl): int
    {
        if (preg_match('/^(\d+)([smhd])$/', $ttl, $matches)) {
            $value = (int)$matches[1];
            $unit = $matches[2];
            
            switch ($unit) {
                case 's': return $value;
                case 'm': return $value * 60;
                case 'h': return $value * 3600;
                case 'd': return $value * 86400;
            }
        }
        
        return 300; // Default 5 minutes
    }
    
    private function executeFujsenOperator(string $operator, string $params)
    {
        // This would integrate with FUJSEN's advanced features
        // For now, return a placeholder
        return "@{$operator}({$params})";
    }
    
    private function resolveFilePath(string $filename): string
    {
        // Check in current directory first
        if (file_exists($filename)) {
            return realpath($filename);
        }
        
        // Check in common config directories
        $searchPaths = [
            './',
            './config/',
            '../',
            '../config/',
            __DIR__ . '/../../config/',
            getcwd() . '/'
        ];
        
        foreach ($searchPaths as $path) {
            $fullPath = $path . $filename;
            if (file_exists($fullPath)) {
                return realpath($fullPath);
            }
        }
        
        // Default to current directory
        return getcwd() . '/' . $filename;
    }
    
    private function resolveAllReferences(array $data): array
    {
        // Resolve references with enhanced context
        $context = array_merge(
            $data,
            ['$globals' => $this->globalVariables],
            ['$sections' => $this->sectionVariables]
        );
        
        return $this->resolveReferencesRecursive($data, $context);
    }
    
    private function resolveReferencesRecursive($data, $context)
    {
        if (is_array($data)) {
            $result = [];
            foreach ($data as $key => $value) {
                $result[$key] = $this->resolveReferencesRecursive($value, $context);
            }
            return $result;
        }
        
        return $data;
    }
}