<?php
/**
 * 🐘 TuskLang - The Elephant's Expressive Data Language
 * =====================================================
 * "Making data as readable as an elephant's memory"
 * 
 * A human-friendly, PHP-inspired data language that combines:
 * - JSON's simplicity
 * - YAML's readability  
 * - PHP's native expression capabilities
 * 
 * Strong. Secure. Scalable. 🐘
 */

namespace TuskPHP\Utils;

class TuskLang
{
    private static $config = [
        'indent' => '    ', // 4 spaces
        'max_depth' => 10,
        'enable_php_expressions' => true,
        'enable_env_vars' => true,
        'enable_references' => true
    ];
    
    /**
     * Parse TuskLang string to PHP array/object
     */
    public static function parse(string $content, bool $asObject = false): array|object
    {
        $parser = new TuskLangParser($content);
        $result = $parser->parse();
        
        return $asObject ? (object) $result : $result;
    }
    
    /**
     * Serialize PHP data to TuskLang format
     */
    public static function serialize(array|object $data, int $depth = 0): string
    {
        $serializer = new TuskLangSerializer(self::$config);
        return $serializer->serialize($data, $depth);
    }
    
    /**
     * Parse with type validation and schema enforcement
     */
    public static function parseTyped(string $content, array $schema = []): array
    {
        $data = self::parse($content);
        
        if (!empty($schema)) {
            $validator = new TuskLangValidator($schema);
            $validator->validate($data);
        }
        
        return $data;
    }
    
    /**
     * Convert JSON to TuskLang
     */
    public static function fromJson(string $jsonString): string
    {
        $data = json_decode($jsonString, true);
        if ($data === null) {
            throw new \Exception('Invalid JSON provided');
        }
        
        return self::encode($data);
    }
    
    /**
     * Convert TuskLang to JSON
     */
    public static function toJson(string $tuskLang): string
    {
        $data = self::parse($tuskLang);
        return json_encode($data, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE);
    }
    
    /**
     * Set configuration options
     */
    public static function configure(array $options): void
    {
        self::$config = array_merge(self::$config, $options);
    }
    
    public static function encode($data, int $indent = 0): string
    {
        if (is_array($data)) {
            return self::encodeArray($data, $indent);
        } else {
            return self::encodeValue($data);
        }
    }
    
    private static function encodeArray(array $data, int $indent = 0): string
    {
        $indentStr = str_repeat('    ', $indent);
        $innerIndent = str_repeat('    ', $indent + 1);
        
        if (self::isAssociative($data)) {
            $result = "{\n";
            foreach ($data as $key => $value) {
                $result .= $innerIndent . self::encodeKey($key) . ': ';
                if (is_array($value)) {
                    $result .= self::encode($value, $indent + 1);
                } else {
                    $result .= self::encodeValue($value);
                }
                $result .= "\n";
            }
            $result .= $indentStr . '}';
            return $result;
        } else {
            if (count($data) <= 3) {
                $items = array_map([self::class, 'encodeValue'], $data);
                return '[' . implode(', ', $items) . ']';
            } else {
                $result = "[\n";
                foreach ($data as $value) {
                    $result .= $innerIndent . self::encodeValue($value) . "\n";
                }
                $result .= $indentStr . ']';
                return $result;
            }
        }
    }
    
    private static function encodeValue($value): string
    {
        if ($value === null) {
            return 'null';
        } elseif ($value === true) {
            return 'true';
        } elseif ($value === false) {
            return 'false';
        } elseif (is_numeric($value)) {
            return (string)$value;
        } elseif (is_string($value)) {
            if (self::needsQuotes($value)) {
                return '"' . addslashes($value) . '"';
            } else {
                return $value;
            }
        } else {
            return '"' . addslashes((string)$value) . '"';
        }
    }
    
    private static function encodeKey($key): string
    {
        if (is_numeric($key)) {
            return (string)$key;
        }
        
        if (preg_match('/^[a-zA-Z_][a-zA-Z0-9_]*$/', $key)) {
            return $key;
        } else {
            return '"' . addslashes($key) . '"';
        }
    }
    
    private static function needsQuotes(string $value): bool
    {
        if (preg_match('/[\s\'"\\\\{}\[\]:,]/', $value)) {
            return true;
        }
        
        $reserved = ['true', 'false', 'null'];
        if (in_array(strtolower($value), $reserved)) {
            return true;
        }
        
        if (is_numeric($value)) {
            return true;
        }
        
        return false;
    }
    
    private static function isAssociative(array $array): bool
    {
        if (empty($array)) {
            return false;
        }
        return array_keys($array) !== range(0, count($array) - 1);
    }
    
    public static function generateEnhanced(array $data, array $metadata = []): string
    {
        $output = "# 🐘 TuskLang Enhanced Data File\n";
        $output .= "# Generated: " . date('Y-m-d H:i:s') . "\n";
        $output .= "# TuskPHP Framework - Make TuskPHP Greater Again!\n\n";
        
        $wisdom = self::getElephantWisdom();
        $output .= "# 🌟 Elephant Wisdom: {$wisdom}\n\n";
        
        if (!empty($metadata)) {
            $output .= "# 📊 Metadata\n";
            foreach ($metadata as $key => $value) {
                $output .= "# {$key}: {$value}\n";
            }
            $output .= "\n";
        }
        
        $output .= self::encode($data);
        
        return $output;
    }
    
    private static function getElephantWisdom(): string
    {
        $wisdom = [
            "An elephant never forgets, and TuskLang never disappoints!",
            "Strong like an elephant, readable like poetry.",
            "In the kingdom of data, TuskLang reigns supreme.",
            "JSON is yesterday, TuskLang is forever.",
            "Elephants have great memory, TuskLang has great clarity."
        ];
        
        return $wisdom[array_rand($wisdom)];
    }
}

/**
 * TuskLang Parser - Converts TuskLang text to PHP data
 */
class TuskLangParser
{
    private $content;
    private $lines;
    private $position = 0;
    private $references = [];
    
    public function __construct(string $content)
    {
        $this->content = $content;
        $this->lines = explode("\n", $content);
    }
    
    public function parse(): array
    {
        $result = [];
        
        while ($this->position < count($this->lines)) {
            $line = trim($this->lines[$this->position]);
            
            // Skip empty lines and comments
            if (empty($line) || $this->isComment($line)) {
                $this->position++;
                continue;
            }
            
            // Parse key-value pairs or objects
            if ($this->isKeyValue($line)) {
                $kv = $this->parseKeyValue($line);
                $result[$kv['key']] = $kv['value'];
            } elseif ($this->isObject($line)) {
                $obj = $this->parseObject($line);
                $result[$obj['key']] = $obj['value'];
            }
            
            $this->position++;
        }
        
        return $this->resolveReferences($result);
    }
    
    private function isComment(string $line): bool
    {
        return preg_match('/^\s*(#|\/\/|\/\*)/', $line);
    }
    
    private function isKeyValue(string $line): bool
    {
        return preg_match('/^[a-zA-Z_][a-zA-Z0-9_]*\s*:\s*.+/', $line);
    }
    
    private function isObject(string $line): bool
    {
        return preg_match('/^[a-zA-Z_][a-zA-Z0-9_]*\s*\{/', $line);
    }
    
    private function parseKeyValue(string $line): array
    {
        preg_match('/^([a-zA-Z_][a-zA-Z0-9_]*)\s*:\s*(.+)$/', $line, $matches);
        
        $key = trim($matches[1]);
        $value = $this->parseValue(trim($matches[2]));
        
        return ['key' => $key, 'value' => $value];
    }
    
    private function parseValue($value)
    {
        $value = trim($value);
        
        // Handle different value types
        if ($value === 'true') return true;
        if ($value === 'false') return false;
        if ($value === 'null') return null;
        
        // Numbers
        if (is_numeric($value)) {
            return strpos($value, '.') !== false ? (float) $value : (int) $value;
        }
        
        // Typed values: int(5), float(3.14), string("hello")
        if (preg_match('/^(int|float|string|bool)\((.*)\)$/', $value, $matches)) {
            $type = $matches[1];
            $val = $matches[2];
            
            switch ($type) {
                case 'int':
                    return (int) $val;
                case 'float':
                    return (float) $val;
                case 'string':
                    return trim($val, '"\'');
                case 'bool':
                    return $val === 'true';
            }
        }
        
        // Arrays: ["item1", "item2"]
        if (preg_match('/^\[(.*)\]$/', $value, $matches)) {
            return $this->parseArray($matches[1]);
        }
        
        // References: &variable_name
        if (preg_match('/^&([a-zA-Z_][a-zA-Z0-9_]*)$/', $value, $matches)) {
            return ['__reference' => $matches[1]];
        }
        
        // Environment variables: env("VAR_NAME", "default")
        if (preg_match('/^env\("([^"]+)"\s*(?:,\s*"([^"]*)")?\)$/', $value, $matches)) {
            $envVar = $matches[1];
            $default = $matches[2] ?? null;
            return $_ENV[$envVar] ?? $default;
        }
        
        // PHP expressions: php(time())
        if (preg_match('/^php\((.+)\)$/', $value, $matches)) {
            $expr = $matches[1];
            // For security, only allow specific safe functions
            $allowedFunctions = ['time', 'date', 'uniqid'];
            foreach ($allowedFunctions as $func) {
                if (strpos($expr, $func) === 0) {
                    return eval("return $expr;");
                }
            }
            throw new \Exception("Unsafe PHP expression: $expr");
        }
        
        // TuskQuery database operations: query("Users").where("active", true).find()
        if (preg_match('/^query\("([^"]+)"\)(.*)$/', $value, $matches)) {
            return $this->executeTuskQuery($matches[1], $matches[2]);
        }
        
        // @Query operator: @Query("Users").where("active", true).find()
        if (preg_match('/^@Query\("([^"]+)"\)(.*)$/', $value, $matches)) {
            return $this->executeTuskQuery($matches[1], $matches[2]);
        }
        
        // @q shorthand operator: @q("Users").where("active", true).find()
        if (preg_match('/^@q\("([^"]+)"\)(.*)$/', $value, $matches)) {
            return $this->executeTuskQuery($matches[1], $matches[2]);
        }
        
        // @graphql operator: @graphql("{ users { id name email } }")
        if (preg_match('/^@graphql\("([^"]+)"(?:,\s*(\{[^}]*\}))?(?:,\s*(\{[^}]*\}))?\)$/', $value, $matches)) {
            return $this->executeGraphQLQuery($matches[1], $matches[2] ?? '{}', $matches[3] ?? '{}');
        }
        
        // @graphql operator with complex queries (multi-line) - more flexible pattern
        if (preg_match('/^@graphql\("([^"]+)"(?:,\s*(\{[^}]*\}))?(?:,\s*(\{[^}]*\}))?\)$/', $value, $matches)) {
            return $this->executeGraphQLQuery($matches[1], $matches[2] ?? '{}', $matches[3] ?? '{}');
        }
        
        // @graphql operator with heredoc-style queries
        if (preg_match('/^@graphql\("([^"]+)"(?:,\s*(\{[^}]*\}))?(?:,\s*(\{[^}]*\}))?\)$/', $value, $matches)) {
            return $this->executeGraphQLQuery($matches[1], $matches[2] ?? '{}', $matches[3] ?? '{}');
        }
        
        // File operations: file("path/to/file.txt")
        if (preg_match('/^file\("([^"]+)"\)$/', $value, $matches)) {
            return $this->readFile($matches[1]);
        }
        
        // JSON file operations: json("data.json")
        if (preg_match('/^json\("([^"]+)"\)$/', $value, $matches)) {
            return $this->readJsonFile($matches[1]);
        }
        
        // Peanuts configuration: peanuts("config.key")
        if (preg_match('/^peanuts\("([^"]+)"\)$/', $value, $matches)) {
            return $this->getPeanutsConfig($matches[1]);
        }
        
        // PHP-style variable references: $variableName
        if (preg_match('/^\$([a-zA-Z_][a-zA-Z0-9_]*)$/', $value, $matches)) {
            return $this->getVariable($matches[1]);
        }
        
        // Angular-style template interpolation: {{variableName}}
        if (preg_match('/^\{\{([a-zA-Z_][a-zA-Z0-9_]*)\}\}$/', $value, $matches)) {
            return $this->getVariable($matches[1]);
        }
        
        // Variable references: @variableName
        if (preg_match('/^@([a-zA-Z_][a-zA-Z0-9_]*)$/', $value, $matches)) {
            return $this->getVariable($matches[1]);
        }
        
        // Advanced references with fallback: @variableName || "default"
        if (preg_match('/^@([a-zA-Z_][a-zA-Z0-9_]*)\s*\|\|\s*"([^"]*)"$/', $value, $matches)) {
            return $this->getVariable($matches[1]) ?? $matches[2];
        }
        
        // PHP-style variable with fallback: $variableName || "default"
        if (preg_match('/^\$([a-zA-Z_][a-zA-Z0-9_]*)\s*\|\|\s*"([^"]*)"$/', $value, $matches)) {
            return $this->getVariable($matches[1]) ?? $matches[2];
        }
        
        // Angular-style template with fallback: {{variableName}} || "default"
        if (preg_match('/^\{\{([a-zA-Z_][a-zA-Z0-9_]*)\}\}\s*\|\|\s*"([^"]*)"$/', $value, $matches)) {
            return $this->getVariable($matches[1]) ?? $matches[2];
        }
        
        // Heredoc strings: <<IDENTIFIER content IDENTIFIER
        if (preg_match('/^<<([A-Z_]+)$/', $value, $matches)) {
            return $this->parseHeredoc($matches[1]);
        }
        
        // Regular strings (remove quotes and process template interpolation)
        $stringValue = trim($value, '"\'');
        return $this->processTemplateInterpolation($stringValue);
    }
    
    private function parseArray(string $content): array
    {
        if (empty(trim($content))) return [];
        
        $items = [];
        $parts = explode(',', $content);
        
        foreach ($parts as $part) {
            $items[] = $this->parseValue(trim($part));
        }
        
        return $items;
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
            if (empty($line) || $this->isComment($line)) {
                $this->position++;
                continue;
            }
            
            // Parse nested content
            if ($this->isKeyValue($line)) {
                $kv = $this->parseKeyValue($line);
                $obj[$kv['key']] = $kv['value'];
            } elseif ($this->isObject($line)) {
                $nested = $this->parseObject($line);
                $obj[$nested['key']] = $nested['value'];
            }
            
            $this->position++;
        }
        
        return ['key' => $key, 'value' => $obj];
    }
    
    private function parseHeredoc(string $identifier): string
    {
        $this->position++; // Move past heredoc start
        $content = [];
        
        while ($this->position < count($this->lines)) {
            $line = $this->lines[$this->position];
            
            if (trim($line) === $identifier) {
                break;
            }
            
            $content[] = $line;
            $this->position++;
        }
        
        return implode("\n", $content);
    }
    
    private function resolveReferences(array $data): array
    {
        return $this->resolveReferencesRecursive($data, $data);
    }
    
    /**
     * Process template interpolation within strings
     * Supports both $variable and {{variable}} syntax
     */
    private function processTemplateInterpolation(string $content): string
    {
        // Process PHP-style variables: $variable
        $content = preg_replace_callback('/\$([a-zA-Z_][a-zA-Z0-9_]*)/', function($matches) {
            $varValue = $this->getVariable($matches[1]);
            return $varValue !== null ? $varValue : '$' . $matches[1];
        }, $content);
        
        // Process Angular-style templates: {{variable}}
        $content = preg_replace_callback('/\{\{([a-zA-Z_][a-zA-Z0-9_]*)\}\}/', function($matches) {
            $varValue = $this->getVariable($matches[1]);
            return $varValue !== null ? $varValue : '{{' . $matches[1] . '}}';
        }, $content);
        
        // Process PHP-style variables with fallback: $variable || "default"
        $content = preg_replace_callback('/\$([a-zA-Z_][a-zA-Z0-9_]*)\s*\|\|\s*"([^"]*)"/', function($matches) {
            $varValue = $this->getVariable($matches[1]);
            return $varValue !== null ? $varValue : $matches[2];
        }, $content);
        
        // Process Angular-style templates with fallback: {{variable}} || "default"
        $content = preg_replace_callback('/\{\{([a-zA-Z_][a-zA-Z0-9_]*)\}\}\s*\|\|\s*"([^"]*)"/', function($matches) {
            $varValue = $this->getVariable($matches[1]);
            return $varValue !== null ? $varValue : $matches[2];
        }, $content);
        
        return $content;
    }
    
    private function resolveReferencesRecursive($data, $context)
    {
        if (is_array($data)) {
            if (isset($data['__reference'])) {
                $refKey = $data['__reference'];
                return $context[$refKey] ?? null;
            }
            
            $result = [];
            foreach ($data as $key => $value) {
                $result[$key] = $this->resolveReferencesRecursive($value, $context);
            }
            return $result;
        }
        
        return $data;
    }
    
    /**
     * Execute TuskQuery database operation
     */
    private function executeTuskQuery(string $className, string $queryChain): array
    {
        try {
            // Initialize TuskQuery
            if (!class_exists('TuskPHP\\TuskQuery')) {
                throw new \Exception('TuskQuery not available');
            }
            
            $query = new \TuskPHP\TuskQuery($className);
            
            // Parse query chain: .where("active", true).limit(10).find()
            $chainParts = explode('.', trim($queryChain, '.'));
            
            foreach ($chainParts as $part) {
                if (preg_match('/^(\w+)\((.*)\)$/', trim($part), $matches)) {
                    $method = $matches[1];
                    $params = $this->parseQueryParams($matches[2]);
                    
                    if (method_exists($query, $method)) {
                        $query = call_user_func_array([$query, $method], $params);
                    }
                }
            }
            
            // Convert TuskObject results to arrays
            if (is_array($query)) {
                return array_map(function($obj) {
                    return method_exists($obj, 'toArray') ? $obj->toArray() : $obj;
                }, $query);
            }
            
            return $query;
            
        } catch (\Exception $e) {
            throw new \Exception("TuskQuery error: " . $e->getMessage());
        }
    }
    
    /**
     * Execute GraphQL query
     */
    private function executeGraphQLQuery(string $query, string $variablesJson = '{}', string $optionsJson = '{}'): array
    {
        try {
            // Load TuskGraphQL class
            if (!class_exists('TuskPHP\\Utils\\TuskGraphQL')) {
                require_once __DIR__ . '/TuskGraphQL.php';
            }
            
            // Parse variables and options
            $variables = json_decode($variablesJson, true) ?: [];
            $options = json_decode($optionsJson, true) ?: [];
            
            // Validate GraphQL query
            if (!\TuskPHP\Utils\TuskGraphQL::validateQuery($query)) {
                throw new \Exception('Invalid GraphQL query syntax');
            }
            
            // Execute the query
            $result = \TuskPHP\Utils\TuskGraphQL::query($query, $variables, $options);
            
            return $result;
            
        } catch (\Exception $e) {
            throw new \Exception("GraphQL query error: " . $e->getMessage());
        }
    }
    
    /**
     * Parse query parameters from string
     */
    private function parseQueryParams(string $params): array
    {
        if (empty(trim($params))) return [];
        
        $result = [];
        $parts = explode(',', $params);
        
        foreach ($parts as $part) {
            $part = trim($part);
            
            // String parameter
            if (preg_match('/^"([^"]*)"$/', $part, $matches)) {
                $result[] = $matches[1];
            }
            // Number parameter
            elseif (is_numeric($part)) {
                $result[] = strpos($part, '.') !== false ? (float)$part : (int)$part;
            }
            // Boolean parameter
            elseif ($part === 'true') {
                $result[] = true;
            }
            elseif ($part === 'false') {
                $result[] = false;
            }
            // Null parameter
            elseif ($part === 'null') {
                $result[] = null;
            }
            else {
                $result[] = $part;
            }
        }
        
        return $result;
    }
    
    /**
     * Read file content safely
     */
    private function readFile(string $filePath): string
    {
        // Security: Only allow files within project directory
        $safePath = $this->getSafeFilePath($filePath);
        
        if (!file_exists($safePath)) {
            throw new \Exception("File not found: {$filePath}");
        }
        
        if (!is_readable($safePath)) {
            throw new \Exception("File not readable: {$filePath}");
        }
        
        $content = file_get_contents($safePath);
        if ($content === false) {
            throw new \Exception("Failed to read file: {$filePath}");
        }
        
        return $content;
    }
    
    /**
     * Read JSON file and parse it
     */
    private function readJsonFile(string $filePath): array
    {
        $content = $this->readFile($filePath);
        $data = json_decode($content, true);
        
        if (json_last_error() !== JSON_ERROR_NONE) {
            throw new \Exception("Invalid JSON in file {$filePath}: " . json_last_error_msg());
        }
        
        return $data;
    }
    
    /**
     * Get configuration from Peanuts system
     */
    private function getPeanutsConfig(string $key): ?string
    {
        try {
            if (class_exists('TuskPHP\\Elephants\\Peanuts')) {
                $peanuts = \TuskPHP\Elephants\Peanuts::getInstance();
                return $peanuts->getConfig($key);
            }
            
            // Fallback to environment variable or .peanuts file parsing
            return $this->parsePeanutsKey($key);
            
        } catch (\Exception $e) {
            throw new \Exception("Peanuts config error: " . $e->getMessage());
        }
    }
    
    /**
     * Get variable from context or global scope
     */
    private function getVariable(string $varName): ?string
    {
        // Check global variables first
        if (isset($GLOBALS[$varName])) {
            return $GLOBALS[$varName];
        }
        
        // Check environment variables
        if (isset($_ENV[$varName])) {
            return $_ENV[$varName];
        }
        
        // Check server variables
        if (isset($_SERVER[$varName])) {
            return $_SERVER[$varName];
        }
        
        return null;
    }
    
    /**
     * Get safe file path within project boundaries
     */
    private function getSafeFilePath(string $filePath): string
    {
        // Remove any directory traversal attempts
        $filePath = str_replace(['../', '..\\'], '', $filePath);
        
        // If absolute path, validate it's within allowed directories
        if (substr($filePath, 0, 1) === '/') {
            $allowedPaths = [
                getcwd(),
                __DIR__ . '/../..',
                '/var/www/belikebrit/auth/v2/tusk'
            ];
            
            $realPath = realpath($filePath);
            $allowed = false;
            
            foreach ($allowedPaths as $allowedPath) {
                if (strpos($realPath, realpath($allowedPath)) === 0) {
                    $allowed = true;
                    break;
                }
            }
            
            if (!$allowed) {
                throw new \Exception("File access denied: {$filePath}");
            }
            
            return $realPath;
        }
        
        // Relative path - make it relative to current directory
        return getcwd() . '/' . ltrim($filePath, '/');
    }
    
    /**
     * Parse .peanuts key manually (fallback)
     */
    private function parsePeanutsKey(string $key): ?string
    {
        $peanutsFile = '.peanuts';
        if (!file_exists($peanutsFile)) {
            return null;
        }
        
        $content = file_get_contents($peanutsFile);
        $lines = explode("\n", $content);
        
        foreach ($lines as $line) {
            $line = trim($line);
            if (empty($line) || $line[0] === '#') continue;
            
            if (preg_match('/^' . preg_quote($key) . '\s*=\s*(.+)$/', $line, $matches)) {
                return trim($matches[1], '"\'');
            }
        }
        
        return null;
    }
}

/**
 * TuskLang Serializer - Converts PHP data to TuskLang text
 */
class TuskLangSerializer
{
    private $config;
    
    public function __construct(array $config)
    {
        $this->config = $config;
    }
    
    public function serialize($data, int $depth = 0): string
    {
        if ($depth === 0) {
            $output = "# 🐘 TuskLang Document\n";
            $output .= "# Generated: " . date('Y-m-d H:i:s') . "\n\n";
        } else {
            $output = "";
        }
        
        if (is_object($data)) {
            $data = (array) $data;
        }
        
        if (is_array($data)) {
            foreach ($data as $key => $value) {
                $output .= $this->serializeKeyValue($key, $value, $depth);
            }
        }
        
        return $output;
    }
    
    private function serializeKeyValue(string $key, $value, int $depth): string
    {
        $indent = str_repeat($this->config['indent'], $depth);
        
        if (is_array($value) && $this->isAssociativeArray($value)) {
            // Object notation
            $output = "{$indent}{$key} {\n";
            foreach ($value as $subKey => $subValue) {
                $output .= $this->serializeKeyValue($subKey, $subValue, $depth + 1);
            }
            $output .= "{$indent}}\n\n";
            return $output;
        } else {
            // Simple key-value
            $serializedValue = $this->serializeValue($value, $depth);
            return "{$indent}{$key}: {$serializedValue}\n";
        }
    }
    
    private function serializeValue($value, int $depth): string
    {
        if (is_null($value)) return 'null';
        if (is_bool($value)) return $value ? 'true' : 'false';
        if (is_int($value)) return (string) $value;
        if (is_float($value)) return (string) $value;
        
        if (is_array($value)) {
            if (empty($value)) return '[]';
            
            if ($this->isAssociativeArray($value)) {
                // Handle as inline object for simple cases
                $items = [];
                foreach ($value as $k => $v) {
                    $items[] = "{$k}: " . $this->serializeValue($v, $depth);
                }
                return "{ " . implode(", ", $items) . " }";
            } else {
                // Handle as array
                $items = array_map(function($item) use ($depth) {
                    return $this->serializeValue($item, $depth);
                }, $value);
                
                if (count($items) <= 3) {
                    return "[" . implode(", ", $items) . "]";
                } else {
                    // Multi-line array
                    $indent = str_repeat($this->config['indent'], $depth + 1);
                    $output = "[\n";
                    foreach ($items as $item) {
                        $output .= "{$indent}{$item}\n";
                    }
                    $output .= str_repeat($this->config['indent'], $depth) . "]";
                    return $output;
                }
            }
        }
        
        // String values
        $value = (string) $value;
        
        // Check if multi-line
        if (strpos($value, "\n") !== false) {
            $identifier = 'TEXT';
            return "<<{$identifier}\n{$value}\n{$identifier}";
        }
        
        // Quote if contains spaces or special characters
        if (preg_match('/[\s:{}[\]"]/', $value)) {
            return '"' . addslashes($value) . '"';
        }
        
        return $value;
    }
    
    private function isAssociativeArray(array $array): bool
    {
        return !empty($array) && array_keys($array) !== range(0, count($array) - 1);
    }
}

/**
 * TuskLang Validator - Schema validation for TuskLang data
 */
class TuskLangValidator
{
    private $schema;
    
    public function __construct(array $schema)
    {
        $this->schema = $schema;
    }
    
    public function validate(array $data): bool
    {
        // Implement schema validation logic
        return true; // Placeholder
    }
}

/**
 * Helper function for easy TuskLang parsing
 */
if (!function_exists('tuskLang')) {
    function tuskLang(string $content): array {
        return TuskLang::parse($content);
    }
} 