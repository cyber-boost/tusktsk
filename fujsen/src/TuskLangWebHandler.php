<?php
/**
 * 🌐 TuskLang Web Handler - Configuration as API Endpoints
 * =======================================================
 * "Making .tsk files serve HTTP requests like a boss"
 * 
 * Enables .tsk files to be executed as web endpoints with:
 * - #!api directive for RESTful APIs
 * - @request object for HTTP data
 * - @json() and @render() for responses
 * - Middleware support for auth/validation
 * 
 * FUJSEN Sprint - Hour 4 Implementation
 */

namespace TuskPHP\Utils;

use TuskPHP\Utils\TuskLang;
use TuskPHP\Utils\TuskLangQueryBridge;

class TuskLangWebHandler
{
    private static $instance = null;
    private $requestData = [];
    private $responseHeaders = [];
    private $middlewares = [];
    
    /**
     * Singleton pattern for web handler
     */
    public static function getInstance(): self
    {
        if (self::$instance === null) {
            self::$instance = new self();
        }
        return self::$instance;
    }
    
    /**
     * Initialize web handler with request data
     */
    public function __construct()
    {
        $this->initializeRequestData();
        $this->setDefaultHeaders();
    }
    
    /**
     * Execute .tsk file as web endpoint
     */
    public function handleRequest(string $tskFile): void
    {
        try {
            // Check if file exists
            if (!file_exists($tskFile)) {
                $this->sendError(404, "Endpoint not found: $tskFile");
                return;
            }
            
            // Read and parse .tsk file
            $content = file_get_contents($tskFile);
            
            // Check for #!api directive
            if (!$this->isApiEndpoint($content)) {
                $this->sendError(400, "Not an API endpoint - missing #!api directive");
                return;
            }
            
            // Execute middleware stack
            if (!$this->executeMiddlewares()) {
                return; // Middleware handled the response
            }
            
            // Parse and execute .tsk content
            $result = $this->executeTskContent($content);
            
            // Send response
            $this->sendResponse($result);
            
        } catch (\Exception $e) {
            error_log("TuskLang Web Handler Error: " . $e->getMessage());
            $this->sendError(500, "Internal server error: " . $e->getMessage());
        }
    }
    
    /**
     * Handle @request object access
     */
    public function getRequestData(): array
    {
        return $this->requestData;
    }
    
    /**
     * Handle @json() function
     */
    public function handleJson(mixed $data, int $statusCode = 200): array
    {
        $this->setHeader('Content-Type', 'application/json');
        http_response_code($statusCode);
        
        return [
            '__json_response' => true,
            'data' => $data,
            'status' => $statusCode
        ];
    }
    
    /**
     * Handle @render() function
     */
    public function handleRender(string $template, array $data = []): array
    {
        $this->setHeader('Content-Type', 'text/html');
        
        return [
            '__html_response' => true,
            'template' => $template,
            'data' => $data
        ];
    }
    
    /**
     * Handle @redirect() function
     */
    public function handleRedirect(string $url, int $statusCode = 302): array
    {
        $this->setHeader('Location', $url);
        http_response_code($statusCode);
        
        return [
            '__redirect_response' => true,
            'url' => $url,
            'status' => $statusCode
        ];
    }
    
    /**
     * Add middleware to the stack
     */
    public function addMiddleware(callable $middleware): void
    {
        $this->middlewares[] = $middleware;
    }
    
    /**
     * Set response header
     */
    public function setHeader(string $name, string $value): void
    {
        $this->responseHeaders[$name] = $value;
        header("$name: $value");
    }
    
    // ========================================
    // PRIVATE METHODS
    // ========================================
    
    /**
     * Initialize request data from HTTP request
     */
    private function initializeRequestData(): void
    {
        $this->requestData = [
            'method' => $_SERVER['REQUEST_METHOD'] ?? 'GET',
            'uri' => $_SERVER['REQUEST_URI'] ?? '/',
            'query' => $_GET ?? [],
            'body' => $this->getRequestBody(),
            'headers' => $this->getRequestHeaders(),
            'params' => array_merge($_GET ?? [], $_POST ?? []),
            'json' => $this->getJsonBody(),
            'ip' => $_SERVER['REMOTE_ADDR'] ?? '127.0.0.1',
            'user_agent' => $_SERVER['HTTP_USER_AGENT'] ?? '',
            'timestamp' => time()
        ];
    }
    
    /**
     * Set default response headers
     */
    private function setDefaultHeaders(): void
    {
        $this->setHeader('X-Powered-By', 'FUJSEN/TuskLang');
        $this->setHeader('Access-Control-Allow-Origin', '*');
        $this->setHeader('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, OPTIONS');
        $this->setHeader('Access-Control-Allow-Headers', 'Content-Type, Authorization, X-Requested-With');
    }
    
    /**
     * Check if content has #!api directive
     */
    private function isApiEndpoint(string $content): bool
    {
        $lines = explode("\n", $content);
        foreach ($lines as $line) {
            $line = trim($line);
            if ($line === '#!api' || $line === '#!/api' || $line === '#! api') {
                return true;
            }
            // Stop checking after first non-comment, non-empty line
            if (!empty($line) && !str_starts_with($line, '#')) {
                break;
            }
        }
        return false;
    }
    
    /**
     * Execute middleware stack
     */
    private function executeMiddlewares(): bool
    {
        foreach ($this->middlewares as $middleware) {
            $result = call_user_func($middleware, $this->requestData);
            if ($result === false) {
                return false; // Middleware rejected the request
            }
        }
        return true;
    }
    
    /**
     * Execute .tsk content with web context
     */
    private function executeTskContent(string $content): mixed
    {
        // Create enhanced parser with web functions
        $parser = new TuskLangWebParser($content, $this);
        $result = $parser->parse();
        
        // If the result contains a JSON response, check if we should return it directly
        if (is_array($result)) {
            // Check if there's a 'result' key with a JSON response
            if (isset($result['result']) && is_array($result['result']) && isset($result['result']['__json_response'])) {
                return $result['result'];
            }
            
            // Check if there's a single @ operator response
            if (count($result) === 1) {
                $singleValue = array_values($result)[0];
                if (is_array($singleValue) && isset($singleValue['__json_response'])) {
                    return $singleValue;
                }
            }
        }
        
        return $result;
    }
    
    /**
     * Send response based on result type
     */
    private function sendResponse(mixed $result): void
    {
        if (is_array($result)) {
            // Check for special response types
            if (isset($result['__json_response'])) {
                echo json_encode($result['data'], JSON_PRETTY_PRINT);
                return;
            }
            
            if (isset($result['__html_response'])) {
                // Simple template rendering
                echo $this->renderTemplate($result['template'], $result['data']);
                return;
            }
            
            if (isset($result['__redirect_response'])) {
                // Redirect already handled by headers
                return;
            }
            
            // Default: JSON response
            $this->setHeader('Content-Type', 'application/json');
            echo json_encode($result, JSON_PRETTY_PRINT);
        } else {
            // Plain text response
            $this->setHeader('Content-Type', 'text/plain');
            echo (string)$result;
        }
    }
    
    /**
     * Send error response
     */
    private function sendError(int $statusCode, string $message): void
    {
        http_response_code($statusCode);
        $this->setHeader('Content-Type', 'application/json');
        
        echo json_encode([
            'error' => true,
            'status' => $statusCode,
            'message' => $message,
            'timestamp' => time()
        ], JSON_PRETTY_PRINT);
    }
    
    /**
     * Get request body
     */
    private function getRequestBody(): string
    {
        return file_get_contents('php://input') ?: '';
    }
    
    /**
     * Get request headers
     */
    private function getRequestHeaders(): array
    {
        $headers = [];
        foreach ($_SERVER as $key => $value) {
            if (str_starts_with($key, 'HTTP_')) {
                $headerName = str_replace('_', '-', substr($key, 5));
                $headers[strtolower($headerName)] = $value;
            }
        }
        return $headers;
    }
    
    /**
     * Get JSON body if content-type is application/json
     */
    private function getJsonBody(): ?array
    {
        $contentType = $_SERVER['CONTENT_TYPE'] ?? '';
        if (str_contains($contentType, 'application/json')) {
            $body = $this->getRequestBody();
            if (!empty($body)) {
                $decoded = json_decode($body, true);
                return json_last_error() === JSON_ERROR_NONE ? $decoded : null;
            }
        }
        return null;
    }
    
    /**
     * Simple template rendering
     */
    private function renderTemplate(string $template, array $data): string
    {
        // Very basic template rendering - replace {{key}} with values
        $content = $template;
        foreach ($data as $key => $value) {
            $content = str_replace("{{$key}}", (string)$value, $content);
        }
        return $content;
    }
}

/**
 * Enhanced TuskLang parser with web functions
 */
class TuskLangWebParser
{
    private $webHandler;
    private $content;
    private $parser;
    private $currentContext = [];
    
    public function __construct(string $content, TuskLangWebHandler $webHandler)
    {
        $this->content = $content;
        $this->webHandler = $webHandler;
        $this->parser = new \TuskPHP\Utils\TuskLangParser($content);
    }
    
    /**
     * Override parse to add web functions
     */
    public function parse(): array
    {
        // First parse normally
        $result = $this->parser->parse();
        
        // Set the current context for variable resolution
        $this->currentContext = $result;
        
        // Process web functions multiple times to resolve all references
        $processed = $this->processWebFunctions($result);
        
        // Update context with processed values
        $this->currentContext = $processed;
        
        // Second pass to resolve variable references
        $processed = $this->resolveAllVariables($processed, $processed);
        
        // Third pass to handle any remaining @ operators
        $processed = $this->processWebFunctions($processed);
        
        return $processed;
    }
    
    /**
     * Process web functions in parsed data
     */
    private function processWebFunctions($data)
    {
        if (is_array($data)) {
            $result = [];
            foreach ($data as $key => $value) {
                $processedValue = $this->processWebFunctions($value);
                $result[$key] = $processedValue;
            }
            return $result;
        }
        
        if (is_string($data)) {
            // Process the string value
            $processed = $this->parseWebValue($data);
            
            // If it's still a string, check for variable references
            if (is_string($processed)) {
                $processed = $this->resolveVariableReferences($processed, $data);
            }
            
            return $processed;
        }
        
        return $data;
    }
    
    /**
     * Parse web-specific values and ALL @ operators
     */
    private function parseWebValue($value)
    {
        $value = trim($value);
        
        // Handle conditional expressions first: format == "html" ? html_response : json_response
        if (preg_match('/^(.+)\s*\?\s*(.+)\s*:\s*(.+)$/', $value, $matches)) {
            $condition = trim($matches[1]);
            $trueValue = trim($matches[2]);
            $falseValue = trim($matches[3]);
            
            // Evaluate condition
            $conditionResult = $this->evaluateCondition($condition);
            return $conditionResult ? $this->parseWebValue($trueValue) : $this->parseWebValue($falseValue);
        }
        
        // @request object access
        if (preg_match('/^@request\.([a-zA-Z_][a-zA-Z0-9_]*)$/', $value, $matches)) {
            $requestData = $this->webHandler->getRequestData();
            $property = $matches[1];
            return $requestData[$property] ?? null;
        }
        
        // @request full object
        if ($value === '@request') {
            return $this->webHandler->getRequestData();
        }
        
        // @request with fallback: @request.query.format || "json"
        if (preg_match('/^@request\.([a-zA-Z_][a-zA-Z0-9_.]*)\s*\|\|\s*"([^"]*)"$/', $value, $matches)) {
            $requestData = $this->webHandler->getRequestData();
            $path = $matches[1];
            $fallback = $matches[2];
            
            $result = $this->getNestedValue($requestData, $path);
            return $result !== null ? $result : $fallback;
        }
        
        // Database @ operators - delegate to QueryBridge
        if (preg_match('/^@(Query|TuskObject|cache|metrics|learn|optimize)\(/', $value)) {
            return $this->executeQueryBridgeOperator($value);
        }
        
        // @json() function
        if (preg_match('/^@json\((.+)\)$/', $value, $matches)) {
            $data = $this->parseComplexValue($matches[1], $this->currentContext);
            return $this->webHandler->handleJson($data);
        }
        
        // @json() with status code
        if (preg_match('/^@json\((.+),\s*(\d+)\)$/', $value, $matches)) {
            $data = $this->parseComplexValue($matches[1], $this->currentContext);
            $statusCode = (int)$matches[2];
            return $this->webHandler->handleJson($data, $statusCode);
        }
        
        // @render() function
        if (preg_match('/^@render\("([^"]+)"(?:,\s*(.+))?\)$/', $value, $matches)) {
            $template = $matches[1];
            $data = isset($matches[2]) ? $this->parseComplexValue($matches[2], $this->currentContext) : [];
            return $this->webHandler->handleRender($template, $data);
        }
        
        // @redirect() function
        if (preg_match('/^@redirect\("([^"]+)"(?:,\s*(\d+))?\)$/', $value, $matches)) {
            $url = $matches[1];
            $statusCode = isset($matches[2]) ? (int)$matches[2] : 302;
            return $this->webHandler->handleRedirect($url, $statusCode);
        }
        
        // PHP expressions: php(time())
        if (preg_match('/^php\((.+)\)$/', $value, $matches)) {
            $expr = $matches[1];
            // For security, only allow specific safe functions
            $allowedFunctions = ['time', 'date', 'uniqid', 'json_encode', 'microtime'];
            foreach ($allowedFunctions as $func) {
                if (strpos($expr, $func) !== false) {
                    try {
                        return eval("return $expr;");
                    } catch (Exception $e) {
                        error_log("PHP expression error: " . $e->getMessage());
                        return null;
                    }
                }
            }
        }
        
        // Return original value if no function matched
        return $value;
    }
    
    /**
     * Resolve variable references in the current context
     */
    private function resolveVariableReferences($value, $originalData)
    {
        // This is where we'd resolve variables like "method" -> actual method value
        // For now, return as-is since we need the full parsed context
        return $value;
    }
    
    /**
     * Resolve all variable references in the data structure
     */
    private function resolveAllVariables($data, $context)
    {
        if (is_array($data)) {
            $result = [];
            foreach ($data as $key => $value) {
                $result[$key] = $this->resolveAllVariables($value, $context);
            }
            return $result;
        }
        
        if (is_string($data)) {
            // Check if this is a variable reference (simple identifier)
            if (preg_match('/^[a-zA-Z_][a-zA-Z0-9_]*$/', $data) && isset($context[$data])) {
                // Resolve the variable
                $resolvedValue = $context[$data];
                
                // If the resolved value is also a string that might be a variable, resolve it too
                if (is_string($resolvedValue) && preg_match('/^[a-zA-Z_][a-zA-Z0-9_]*$/', $resolvedValue) && isset($context[$resolvedValue])) {
                    return $context[$resolvedValue];
                }
                
                return $resolvedValue;
            }
            
            // Handle expressions with operators like "body || \"(empty)\""
            if (preg_match('/^(.+)\s*\|\|\s*"([^"]*)"$/', $data, $matches)) {
                $varName = trim($matches[1]);
                $fallback = $matches[2];
                
                if (isset($context[$varName])) {
                    $value = $context[$varName];
                    return (!empty($value) && $value !== null) ? $value : $fallback;
                }
                
                return $fallback;
            }
        }
        
        return $data;
    }
    
    /**
     * Evaluate condition expressions like format == "html"
     */
    private function evaluateCondition($condition)
    {
        // Simple equality check
        if (preg_match('/^(.+)\s*==\s*"([^"]*)"$/', $condition, $matches)) {
            $left = trim($matches[1]);
            $right = $matches[2];
            
            // Get the actual value of the left side
            $leftValue = $this->parseWebValue($left);
            return $leftValue === $right;
        }
        
        return false;
    }
    
    /**
     * Get nested value from array using dot notation
     */
    private function getNestedValue($data, $path)
    {
        $keys = explode('.', $path);
        $current = $data;
        
        foreach ($keys as $key) {
            if (is_array($current) && isset($current[$key])) {
                $current = $current[$key];
            } else {
                return null;
            }
        }
        
        return $current;
    }
    
    /**
     * Parse complex values like object literals
     */
    private function parseComplexValue($value, $context = null)
    {
        $value = trim($value);
        
        // Object literal: {key: value, key2: value2}
        if (preg_match('/^\{(.+)\}$/', $value, $matches)) {
            return $this->parseObjectLiteral($matches[1], $context);
        }
        
        // Array literal: [item1, item2]
        if (preg_match('/^\[(.+)\]$/', $value, $matches)) {
            return $this->parseArrayLiteral($matches[1], $context);
        }
        
        // String literal
        if (preg_match('/^"([^"]*)"$/', $value, $matches)) {
            return $matches[1];
        }
        
        // Variable reference - resolve from context
        if ($context && preg_match('/^[a-zA-Z_][a-zA-Z0-9_]*$/', $value) && isset($context[$value])) {
            return $context[$value];
        }
        
        // Process as web value
        return $this->parseWebValue($value);
    }
    
    /**
     * Parse object literal syntax
     */
    private function parseObjectLiteral($content, $context = null)
    {
        $result = [];
        $content = trim($content);
        
        // Simple parsing - split by comma and parse key: value pairs
        $pairs = $this->splitByComma($content);
        
        foreach ($pairs as $pair) {
            $pair = trim($pair);
            
            // Handle quoted keys: "key": value
            if (preg_match('/^"([^"]+)"\s*:\s*(.+)$/', $pair, $matches)) {
                $key = $matches[1];
                $value = trim($matches[2]);
                $result[$key] = $this->parseComplexValue($value, $context);
            }
            // Handle unquoted keys: key: value
            elseif (preg_match('/^([a-zA-Z_][a-zA-Z0-9_]*)\s*:\s*(.+)$/', $pair, $matches)) {
                $key = trim($matches[1]);
                $value = trim($matches[2]);
                $result[$key] = $this->parseComplexValue($value, $context);
            }
        }
        
        return $result;
    }
    
    /**
     * Parse array literal syntax
     */
    private function parseArrayLiteral($content, $context = null)
    {
        $items = $this->splitByComma($content);
        $result = [];
        
        foreach ($items as $item) {
            $result[] = $this->parseComplexValue(trim($item), $context);
        }
        
        return $result;
    }
    
    /**
     * Split content by comma, respecting nested structures
     */
    private function splitByComma($content)
    {
        $items = [];
        $current = '';
        $depth = 0;
        $inString = false;
        
        for ($i = 0; $i < strlen($content); $i++) {
            $char = $content[$i];
            
            if ($char === '"' && ($i === 0 || $content[$i-1] !== '\\')) {
                $inString = !$inString;
            }
            
            if (!$inString) {
                if ($char === '{' || $char === '[') {
                    $depth++;
                } elseif ($char === '}' || $char === ']') {
                    $depth--;
                } elseif ($char === ',' && $depth === 0) {
                    $items[] = trim($current);
                    $current = '';
                    continue;
                }
            }
            
            $current .= $char;
        }
        
        if (!empty(trim($current))) {
            $items[] = trim($current);
        }
        
        return $items;
    }
    
    /**
     * Execute QueryBridge operators
     */
    private function executeQueryBridgeOperator($value)
    {
        $bridge = \TuskPHP\Utils\TuskLangQueryBridge::getInstance();
        
        // @Query() operator
        if (preg_match('/^@Query\("([^"]+)"\)(.*)$/', $value, $matches)) {
            $className = $matches[1];
            $queryChain = $matches[2];
            return $bridge->handleQuery($className, $queryChain);
        }
        
        // @cache() operator
        if (preg_match('/^@cache\("([^"]+)",\s*(.+)\)$/', $value, $matches)) {
            $ttl = $matches[1];
            $valueToCache = $this->parseComplexValue($matches[2], $this->currentContext);
            return $bridge->handleCache($ttl, $valueToCache);
        }
        
        // @metrics() operator
        if (preg_match('/^@metrics\("([^"]+)",\s*(.+)\)$/', $value, $matches)) {
            $metric = $matches[1];
            $metricValue = $this->parseComplexValue($matches[2], $this->currentContext);
            return $bridge->handleMetrics($metric, $metricValue);
        }
        
        // @learn() operator
        if (preg_match('/^@learn\("([^"]+)",\s*(.+)\)$/', $value, $matches)) {
            $pattern = $matches[1];
            $config = $this->parseComplexValue($matches[2], $this->currentContext);
            return $bridge->handleLearn($pattern, $config);
        }
        
        // @optimize() operator
        if (preg_match('/^@optimize\("([^"]+)",\s*(.+)\)$/', $value, $matches)) {
            $parameter = $matches[1];
            $config = $this->parseComplexValue($matches[2], $this->currentContext);
            return $bridge->handleOptimize($parameter, $config);
        }
        
        return $value;
    }
} 