<?php
/**
 * 🥜 TuskLang Enhanced for PHP - The Freedom Parser
 * =================================================
 * "We don't bow to any king" - Support ALL syntax styles
 * 
 * Features:
 * - Multiple grouping: [], {}, <>
 * - $global vs section-local variables
 * - Cross-file communication
 * - Database queries (with PDO adapters)
 * - All @ operators (85 total)
 * - Maximum flexibility
 * 
 * DEFAULT CONFIG: peanut.tsk (the bridge of language grace)
 */

namespace TuskLang;

use TuskLang\OperatorRegistry;

class TuskLangEnhanced
{
    private array $data = [];
    public array $globalVariables = [];
    private array $sectionVariables = [];
    private array $cache = [];
    private array $crossFileCache = [];
    private string $currentSection = '';
    private bool $inObject = false;
    private string $objectKey = '';
    private bool $peanutLoaded = false;
    
    // Standard peanut.tsk locations
    private array $peanutLocations = [
        './peanut.tsk',
        '../peanut.tsk', 
        '../../peanut.tsk',
        '/etc/tusklang/peanut.tsk',
        '~/.config/tusklang/peanut.tsk'
    ];
    
    // Database connection (for queries)
    private ?\PDO $pdo = null;
    
    public function __construct()
    {
        // Add environment variable location
        if (!empty($_ENV['TUSKLANG_CONFIG'])) {
            $this->peanutLocations[] = $_ENV['TUSKLANG_CONFIG'];
        }
    }
    
    /**
     * Load peanut.tsk if available
     */
    public function loadPeanut(): void
    {
        if ($this->peanutLoaded) {
            return;
        }
        
        // Mark as loaded first to prevent recursion
        $this->peanutLoaded = true;
        
        foreach ($this->peanutLocations as $location) {
            if (empty($location)) continue;
            
            // Expand ~ to home directory
            if (str_starts_with($location, '~/')) {
                $location = $_SERVER['HOME'] . '/' . substr($location, 2);
            }
            
            if (file_exists($location)) {
                echo "# Loading universal config from: $location\n";
                try {
                    $content = file_get_contents($location);
                    $this->parsePeanutBasic($content);
                    return;
                } catch (Exception $e) {
                    echo "# Warning: Could not load $location: " . $e->getMessage() . "\n";
                    continue;
                }
            }
        }
    }
    
    /**
     * Basic parsing for peanut.tsk to avoid recursion
     */
    private function parsePeanutBasic(string $content): void
    {
        $lines = explode("\n", $content);
        $currentSection = null;
        
        foreach ($lines as $line) {
            $trimmed = trim($line);
            if (empty($trimmed) || str_starts_with($trimmed, '#')) {
                continue;
            }
            
            // Section headers
            if (preg_match('/^\[([^\]]+)\]$/', $trimmed, $matches)) {
                $currentSection = $matches[1];
                continue;
            }
            
            // Key-value pairs with basic parsing only
            if ($currentSection && (str_contains($trimmed, '=') || str_contains($trimmed, ':'))) {
                $separator = str_contains($trimmed, '=') ? '=' : ':';
                [$key, $value] = explode($separator, $trimmed, 2);
                $key = trim($key);
                $value = trim($value, ' "\'\t\r\n');
                
                // Store in section variables for reference
                $sectionKey = "$currentSection.$key";
                $this->sectionVariables[$sectionKey] = $value;
            }
        }
    }
    
    /**
     * Parse TuskLang value with all syntax support
     */
    public function parseValue(string $value): mixed
    {
        $value = trim($value);
        
        // Remove optional semicolon
        if (str_ends_with($value, ';')) {
            $value = trim(substr($value, 0, -1));
        }
        
        // Basic types
        if ($value === 'true') return true;
        if ($value === 'false') return false;
        if ($value === 'null') return null;
        
        // Numbers
        if (preg_match('/^-?\d+$/', $value)) {
            return (int) $value;
        }
        if (preg_match('/^-?\d+\.\d+$/', $value)) {
            return (float) $value;
        }
        
        // $variable references (global)
        if (preg_match('/^\$([a-zA-Z_][a-zA-Z0-9_]*)$/', $value, $matches)) {
            $varName = $matches[1];
            return $this->globalVariables[$varName] ?? '';
        }
        
        // Section-local variable references
        if (!empty($this->currentSection) && preg_match('/^[a-zA-Z_][a-zA-Z0-9_]*$/', $value)) {
            $sectionKey = "{$this->currentSection}.$value";
            if (isset($this->sectionVariables[$sectionKey])) {
                return $this->sectionVariables[$sectionKey];
            }
        }
        
        // @date function
        if (preg_match('/^@date\(["\'](.*)["\']\)$/', $value, $matches)) {
            $format = $matches[1];
            return $this->executeDate($format);
        }
        
        // @env function with default
        if (preg_match('/^@env\(["\']([^"\']*)["\'](?:,\s*(.+))?\)$/', $value, $matches)) {
            $envVar = $matches[1];
            $defaultVal = isset($matches[2]) ? trim($matches[2], ' "\'\t\r\n') : '';
            return $_ENV[$envVar] ?? $defaultVal;
        }
        
        // Ranges: 8000-9000
        if (preg_match('/^(\d+)-(\d+)$/', $value, $matches)) {
            return [
                'min' => (int) $matches[1],
                'max' => (int) $matches[2],
                'type' => 'range'
            ];
        }
        
        // Arrays
        if (str_starts_with($value, '[') && str_ends_with($value, ']')) {
            return $this->parseArray($value);
        }
        
        // Objects
        if (str_starts_with($value, '{') && str_ends_with($value, '}')) {
            return $this->parseObject($value);
        }
        
        // Cross-file references: @file.tsk.get('key')
        if (preg_match('/^@([a-zA-Z0-9_-]+)\.tsk\.get\(["\'](.*)["\']\)$/', $value, $matches)) {
            $fileName = $matches[1];
            $key = $matches[2];
            return $this->crossFileGet($fileName, $key);
        }
        
        // Cross-file set: @file.tsk.set('key', value)
        if (preg_match('/^@([a-zA-Z0-9_-]+)\.tsk\.set\(["\']([^"\']*)["\'],\s*(.+)\)$/', $value, $matches)) {
            $fileName = $matches[1];
            $key = $matches[2];
            $val = $matches[3];
            return $this->crossFileSet($fileName, $key, $val);
        }
        
        // @query function
        if (preg_match('/^@query\(["\'](.*)["\'](.*)\)$/', $value, $matches)) {
            $query = $matches[1];
            return $this->executeQuery($query);
        }
        
        // @ operators
        if (preg_match('/^@([a-zA-Z_][a-zA-Z0-9_]*)\((.+)\)$/', $value, $matches)) {
            $operator = $matches[1];
            $params = $matches[2];
            return $this->executeOperator($operator, $params);
        }
        
        // String concatenation
        if (str_contains($value, ' + ')) {
            $parts = explode(' + ', $value);
            $result = '';
            foreach ($parts as $part) {
                $part = trim($part, ' "\'\t\r\n');
                $parsedPart = !str_starts_with($part, '"') ? $this->parseValue($part) : substr($part, 1, -1);
                $result .= (string) $parsedPart;
            }
            return $result;
        }
        
        // Conditional/ternary: condition ? true_val : false_val
        if (preg_match('/(.+?)\s*\?\s*(.+?)\s*:\s*(.+)/', $value, $matches)) {
            $condition = trim($matches[1]);
            $trueVal = trim($matches[2]);
            $falseVal = trim($matches[3]);
            
            if ($this->evaluateCondition($condition)) {
                return $this->parseValue($trueVal);
            } else {
                return $this->parseValue($falseVal);
            }
        }
        
        // Remove quotes from strings
        if ((str_starts_with($value, '"') && str_ends_with($value, '"')) ||
            (str_starts_with($value, "'") && str_ends_with($value, "'"))) {
            return substr($value, 1, -1);
        }
        
        // Return as-is
        return $value;
    }
    
    /**
     * Parse array syntax
     */
    private function parseArray(string $value): array
    {
        $content = trim(substr($value, 1, -1));
        if (empty($content)) {
            return [];
        }
        
        $items = [];
        $current = '';
        $depth = 0;
        $inString = false;
        $quoteChar = null;
        
        for ($i = 0; $i < strlen($content); $i++) {
            $char = $content[$i];
            
            if (in_array($char, ['"', "'"], true) && !$inString) {
                $inString = true;
                $quoteChar = $char;
            } elseif ($char === $quoteChar && $inString) {
                $inString = false;
                $quoteChar = null;
            }
            
            if (!$inString) {
                if (in_array($char, ['[', '{'], true)) {
                    $depth++;
                } elseif (in_array($char, [']', '}'], true)) {
                    $depth--;
                } elseif ($char === ',' && $depth === 0) {
                    $items[] = $this->parseValue(trim($current));
                    $current = '';
                    continue;
                }
            }
            
            $current .= $char;
        }
        
        if (!empty(trim($current))) {
            $items[] = $this->parseValue(trim($current));
        }
        
        return $items;
    }
    
    /**
     * Parse object syntax
     */
    private function parseObject(string $value): array
    {
        $content = trim(substr($value, 1, -1));
        if (empty($content)) {
            return [];
        }
        
        $pairs = [];
        $current = '';
        $depth = 0;
        $inString = false;
        $quoteChar = null;
        
        for ($i = 0; $i < strlen($content); $i++) {
            $char = $content[$i];
            
            if (in_array($char, ['"', "'"], true) && !$inString) {
                $inString = true;
                $quoteChar = $char;
            } elseif ($char === $quoteChar && $inString) {
                $inString = false;
                $quoteChar = null;
            }
            
            if (!$inString) {
                if (in_array($char, ['[', '{'], true)) {
                    $depth++;
                } elseif (in_array($char, [']', '}'], true)) {
                    $depth--;
                } elseif ($char === ',' && $depth === 0) {
                    $pairs[] = trim($current);
                    $current = '';
                    continue;
                }
            }
            
            $current .= $char;
        }
        
        if (!empty(trim($current))) {
            $pairs[] = trim($current);
        }
        
        $obj = [];
        foreach ($pairs as $pair) {
            if (str_contains($pair, ':')) {
                [$key, $val] = explode(':', $pair, 2);
                $key = trim($key, ' "\'\t\r\n');
                $val = trim($val);
                $obj[$key] = $this->parseValue($val);
            } elseif (str_contains($pair, '=')) {
                [$key, $val] = explode('=', $pair, 2);
                $key = trim($key, ' "\'\t\r\n');
                $val = trim($val);
                $obj[$key] = $this->parseValue($val);
            }
        }
        
        return $obj;
    }
    
    /**
     * Evaluate conditions for ternary expressions
     */
    private function evaluateCondition(string $condition): bool
    {
        $condition = trim($condition);
        
        // Simple equality check
        if (preg_match('/(.+?)\s*==\s*(.+)/', $condition, $matches)) {
            $left = $this->parseValue(trim($matches[1]));
            $right = $this->parseValue(trim($matches[2]));
            return (string) $left === (string) $right;
        }
        
        // Not equal
        if (preg_match('/(.+?)\s*!=\s*(.+)/', $condition, $matches)) {
            $left = $this->parseValue(trim($matches[1]));
            $right = $this->parseValue(trim($matches[2]));
            return (string) $left !== (string) $right;
        }
        
        // Greater than
        if (preg_match('/(.+?)\s*>\s*(.+)/', $condition, $matches)) {
            $left = $this->parseValue(trim($matches[1]));
            $right = $this->parseValue(trim($matches[2]));
            if (is_numeric($left) && is_numeric($right)) {
                return (float) $left > (float) $right;
            }
            return (string) $left > (string) $right;
        }
        
        // Default: check if truthy
        $value = $this->parseValue($condition);
        return !empty($value) && !in_array($value, [false, null, 0, '0', 'false', 'null'], true);
    }
    
    /**
     * Get value from another TSK file
     */
    private function crossFileGet(string $fileName, string $key): mixed
    {
        $cacheKey = "$fileName:$key";
        
        // Check cache
        if (isset($this->crossFileCache[$cacheKey])) {
            return $this->crossFileCache[$cacheKey];
        }
        
        // Find file
        $filePath = null;
        foreach (['.', './config', '..', '../config'] as $directory) {
            $potentialPath = "$directory/$fileName.tsk";
            if (file_exists($potentialPath)) {
                $filePath = $potentialPath;
                break;
            }
        }
        
        if (!$filePath) {
            return '';
        }
        
        // Parse file and get value
        $tempParser = new self();
        $tempParser->parseFile($filePath);
        
        $keys = explode('.', $key);
        $value = $tempParser->data;
        foreach ($keys as $k) {
            if (is_array($value) && isset($value[$k])) {
                $value = $value[$k];
            } else {
                $value = '';
                break;
            }
        }
        
        // Cache result
        $this->crossFileCache[$cacheKey] = $value;
        
        return $value;
    }
    
    /**
     * Set value in another TSK file (cache only for now)
     */
    private function crossFileSet(string $fileName, string $key, string $value): mixed
    {
        $cacheKey = "$fileName:$key";
        $parsedValue = $this->parseValue($value);
        $this->crossFileCache[$cacheKey] = $parsedValue;
        return $parsedValue;
    }
    
    /**
     * Execute @date function
     */
    private function executeDate(string $format): string
    {
        // Convert PHP-style format to date() function format
        $formatMap = [
            'Y' => 'Y',  // 4-digit year
            'Y-m-d' => 'Y-m-d',
            'Y-m-d H:i:s' => 'Y-m-d H:i:s',
            'c' => 'c'  // ISO 8601 date
        ];
        
        if (isset($formatMap[$format])) {
            return date($formatMap[$format]);
        } else {
            return date('Y-m-d H:i:s');
        }
    }
    
    /**
     * Execute database query
     */
    private function executeQuery(string $query): mixed
    {
        $this->loadPeanut();
        
        // Get database configuration
        $dbType = $this->sectionVariables['database.default'] ?? 'sqlite';
        
        try {
            if (!$this->pdo) {
                $this->setupDatabase($dbType);
            }
            
            if ($this->pdo) {
                $stmt = $this->pdo->prepare($query);
                $stmt->execute();
                return $stmt->fetchAll(\PDO::FETCH_ASSOC);
            }
        } catch (\Exception $e) {
            // Return placeholder result
            return "[Query: $query on $dbType - Error: " . $e->getMessage() . "]";
        }
        
        return "[Query: $query on $dbType]";
    }
    
    /**
     * Setup database connection
     */
    private function setupDatabase(string $dbType): void
    {
        try {
            switch ($dbType) {
                case 'sqlite':
                    $filename = $this->sectionVariables['database.sqlite.filename'] ?? './tusklang.db';
                    $this->pdo = new \PDO("sqlite:$filename");
                    break;
                    
                case 'postgres':
                case 'postgresql':
                    $host = $this->sectionVariables['database.postgres.host'] ?? 'localhost';
                    $port = $this->sectionVariables['database.postgres.port'] ?? 5432;
                    $dbname = $this->sectionVariables['database.postgres.database'] ?? 'tusklang';
                    $user = $this->sectionVariables['database.postgres.user'] ?? 'postgres';
                    $password = $this->sectionVariables['database.postgres.password'] ?? '';
                    
                    $dsn = "pgsql:host=$host;port=$port;dbname=$dbname";
                    $this->pdo = new \PDO($dsn, $user, $password);
                    break;
                    
                case 'mysql':
                    $host = $this->sectionVariables['database.mysql.host'] ?? 'localhost';
                    $port = $this->sectionVariables['database.mysql.port'] ?? 3306;
                    $dbname = $this->sectionVariables['database.mysql.database'] ?? 'tusklang';
                    $user = $this->sectionVariables['database.mysql.user'] ?? 'root';
                    $password = $this->sectionVariables['database.mysql.password'] ?? '';
                    
                    $dsn = "mysql:host=$host;port=$port;dbname=$dbname;charset=utf8mb4";
                    $this->pdo = new \PDO($dsn, $user, $password);
                    break;
            }
            
            if ($this->pdo) {
                $this->pdo->setAttribute(\PDO::ATTR_ERRMODE, \PDO::ERRMODE_EXCEPTION);
            }
        } catch (\Exception $e) {
            echo "# Warning: Could not connect to $dbType database: " . $e->getMessage() . "\n";
        }
    }
    
    /**
     * Execute @ operators
     */
    private function executeOperator(string $operator, string $params): mixed
    {
        // Parse parameters into configuration array
        $config = $this->parseOperatorParams($params);
        
        // Create context with global variables
        $context = [
            'global_variables' => $this->globalVariables,
            'section_variables' => $this->sectionVariables,
            'current_section' => $this->currentSection
        ];
        
        try {
            // Use the OperatorRegistry to execute the operator
            $registry = OperatorRegistry::getInstance();
            return $registry->executeOperator($operator, $config, $context);
        } catch (\Exception $e) {
            error_log("Operator '$operator' execution failed: " . $e->getMessage());
            return "@$operator($params)"; // Fallback to string representation
        }
    }
    
    /**
     * Parse operator parameters into configuration array
     */
    private function parseOperatorParams(string $params): array
    {
        $config = [];
        
        // Handle simple string parameters
        if (preg_match('/^["\'](.*)["\']$/', trim($params), $matches)) {
            return ['value' => $matches[1]];
        }
        
        // Handle key-value pairs
        if (preg_match_all('/(\w+)\s*:\s*([^,\s]+)/', $params, $matches, PREG_SET_ORDER)) {
            foreach ($matches as $match) {
                $key = $match[1];
                $value = $match[2];
                
                // Remove quotes if present
                if (preg_match('/^["\'](.*)["\']$/', $value, $quoteMatches)) {
                    $value = $quoteMatches[1];
                }
                
                $config[$key] = $value;
            }
        }
        
        return $config;
    }
    
    /**
     * Parse a single line
     */
    public function parseLine(string $line): void
    {
        $trimmed = trim($line);
        
        // Skip empty lines and comments
        if (empty($trimmed) || str_starts_with($trimmed, '#')) {
            return;
        }
        
        // Remove optional semicolon
        if (str_ends_with($trimmed, ';')) {
            $trimmed = trim(substr($trimmed, 0, -1));
        }
        
        // Check for section declaration []
        if (preg_match('/^\[([a-zA-Z_][a-zA-Z0-9_]*)\]$/', $trimmed, $matches)) {
            $this->currentSection = $matches[1];
            $this->inObject = false;
            return;
        }
        
        // Check for angle bracket object >
        if (preg_match('/^([a-zA-Z_][a-zA-Z0-9_]*)\s*>$/', $trimmed, $matches)) {
            $this->inObject = true;
            $this->objectKey = $matches[1];
            return;
        }
        
        // Check for closing angle bracket <
        if ($trimmed === '<') {
            $this->inObject = false;
            $this->objectKey = '';
            return;
        }
        
        // Check for curly brace object {
        if (preg_match('/^([a-zA-Z_][a-zA-Z0-9_]*)\s*\{$/', $trimmed, $matches)) {
            $this->inObject = true;
            $this->objectKey = $matches[1];
            return;
        }
        
        // Check for closing curly brace }
        if ($trimmed === '}') {
            $this->inObject = false;
            $this->objectKey = '';
            return;
        }
        
        // Parse key-value pairs (both : and = supported)
        if (preg_match('/^([\$]?[a-zA-Z_][a-zA-Z0-9_-]*)\s*[:=]\s*(.+)$/', $trimmed, $matches)) {
            $key = $matches[1];
            $value = $matches[2];
            $parsedValue = $this->parseValue($value);
            
            // Determine storage location
            if ($this->inObject && !empty($this->objectKey)) {
                if (!empty($this->currentSection)) {
                    $storageKey = "{$this->currentSection}.{$this->objectKey}.$key";
                } else {
                    $storageKey = "{$this->objectKey}.$key";
                }
            } elseif (!empty($this->currentSection)) {
                $storageKey = "{$this->currentSection}.$key";
            } else {
                $storageKey = $key;
            }
            
            // Store the value
            $this->data[$storageKey] = $parsedValue;
            
            // Handle global variables
            if (str_starts_with($key, '$')) {
                $varName = substr($key, 1);
                $this->globalVariables[$varName] = $parsedValue;
            } elseif (!empty($this->currentSection) && !str_starts_with($key, '$')) {
                // Store section-local variable
                $sectionKey = "{$this->currentSection}.$key";
                $this->sectionVariables[$sectionKey] = $parsedValue;
            }
        }
    }
    
    /**
     * Parse TuskLang content
     */
    public function parse(string $content): array
    {
        $lines = explode("\n", $content);
        
        foreach ($lines as $line) {
            $this->parseLine($line);
        }
        
        return $this->data;
    }
    
    /**
     * Parse a TSK file
     */
    public function parseFile(string $filePath): array
    {
        if (!file_exists($filePath)) {
            throw new \Exception("File not found: $filePath");
        }
        
        $content = file_get_contents($filePath);
        return $this->parse($content);
    }
    
    /**
     * Get a value by key
     */
    public function get(string $key): mixed
    {
        return $this->data[$key] ?? null;
    }
    
    /**
     * Set a value
     */
    public function set(string $key, mixed $value): void
    {
        $this->data[$key] = $value;
    }
    
    /**
     * Get all keys
     */
    public function keys(): array
    {
        $keys = array_keys($this->data);
        sort($keys);
        return $keys;
    }
    
    /**
     * Get all key-value pairs
     */
    public function items(): array
    {
        $items = [];
        foreach ($this->keys() as $key) {
            $items[] = [$key, $this->data[$key]];
        }
        return $items;
    }
    
    /**
     * Convert to array
     */
    public function toArray(): array
    {
        return $this->data;
    }
    
    /**
     * Export as environment variables
     */
    public function export(string $prefix = 'TSK_'): void
    {
        foreach ($this->data as $key => $value) {
            $varName = $prefix . strtoupper(str_replace(['.', '-'], '_', $key));
            putenv("$varName=" . (string) $value);
        }
    }
}

// Convenience functions
function tsk_parse(string $content): array
{
    $parser = new TuskLangEnhanced();
    return $parser->parse($content);
}

function tsk_parse_file(string $filePath): array
{
    $parser = new TuskLangEnhanced();
    return $parser->parseFile($filePath);
}

function tsk_load_from_peanut(): TuskLangEnhanced
{
    $parser = new TuskLangEnhanced();
    $parser->loadPeanut();
    return $parser;
}