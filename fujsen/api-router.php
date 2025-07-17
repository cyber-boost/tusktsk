<?php
/**
 * 🚀 FUJSEN API Router - .tsk Files as HTTP Endpoints
 * =================================================
 * "Configuration files that serve HTTP requests"
 * 
 * Routes HTTP requests to .tsk files with #!api directive
 * 
 * Usage:
 * - Place .tsk files in /api/ directory
 * - Add #!api directive at top of file
 * - Access via HTTP: /api/endpoint.tsk
 * 
 * FUJSEN Sprint - Hour 4 Implementation
 */

// Include the autoloader
require_once __DIR__ . '/autoload.php';

use TuskPHP\Utils\TuskLangWebHandler;

/**
 * 🌐 FUJSEN API Router
 */
class FujsenApiRouter
{
    private $webHandler;
    private $apiDirectory;
    private $allowedMethods = ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS', 'PATCH'];
    
    public function __construct(string $apiDirectory = null)
    {
        $this->apiDirectory = $apiDirectory ?: __DIR__ . '/api';
        $this->webHandler = TuskLangWebHandler::getInstance();
        
        // Add default middlewares
        $this->addDefaultMiddlewares();
    }
    
    /**
     * Route incoming HTTP request to .tsk endpoint
     */
    public function route(): void
    {
        try {
            // Handle CORS preflight
            if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
                $this->handleCorsPrelight();
                return;
            }
            
            // Get the requested path
            $path = $this->getRequestPath();
            
            // Find matching .tsk file
            $tskFile = $this->findTskFile($path);
            
            if (!$tskFile) {
                $this->sendNotFound($path);
                return;
            }
            
            // Execute the .tsk file as endpoint
            $this->webHandler->handleRequest($tskFile);
            
        } catch (\Exception $e) {
            error_log("FUJSEN Router Error: " . $e->getMessage());
            $this->sendError(500, "Router error: " . $e->getMessage());
        }
    }
    
    /**
     * Add middleware to the web handler
     */
    public function addMiddleware(callable $middleware): void
    {
        $this->webHandler->addMiddleware($middleware);
    }
    
    /**
     * Set API directory
     */
    public function setApiDirectory(string $directory): void
    {
        $this->apiDirectory = $directory;
    }
    
    // ========================================
    // PRIVATE METHODS
    // ========================================
    
    /**
     * Get the request path from URL
     */
    private function getRequestPath(): string
    {
        $uri = $_SERVER['REQUEST_URI'] ?? '/';
        $path = parse_url($uri, PHP_URL_PATH);
        
        // Remove leading slash
        $path = ltrim($path, '/');
        
        // Remove script name if present (for non-rewrite scenarios)
        $scriptName = basename($_SERVER['SCRIPT_NAME'] ?? '');
        if (str_starts_with($path, $scriptName)) {
            $path = substr($path, strlen($scriptName));
            $path = ltrim($path, '/');
        }
        
        return $path;
    }
    
    /**
     * Find .tsk file matching the request path
     */
    private function findTskFile(string $path): ?string
    {
        // Direct .tsk file match
        $tskFile = $this->apiDirectory . '/' . $path;
        if (file_exists($tskFile) && str_ends_with($tskFile, '.tsk')) {
            return $tskFile;
        }
        
        // Add .tsk extension if not present
        if (!str_ends_with($path, '.tsk')) {
            $tskFile = $this->apiDirectory . '/' . $path . '.tsk';
            if (file_exists($tskFile)) {
                return $tskFile;
            }
        }
        
        // Try index.tsk for directory requests
        if (empty($path) || str_ends_with($path, '/')) {
            $indexFile = $this->apiDirectory . '/' . rtrim($path, '/') . '/index.tsk';
            if (file_exists($indexFile)) {
                return $indexFile;
            }
        }
        
        return null;
    }
    
    /**
     * Handle CORS preflight requests
     */
    private function handleCorsPrelight(): void
    {
        http_response_code(200);
        header('Access-Control-Allow-Origin: *');
        header('Access-Control-Allow-Methods: ' . implode(', ', $this->allowedMethods));
        header('Access-Control-Allow-Headers: Content-Type, Authorization, X-Requested-With');
        header('Access-Control-Max-Age: 86400');
        exit;
    }
    
    /**
     * Send 404 Not Found response
     */
    private function sendNotFound(string $path): void
    {
        http_response_code(404);
        header('Content-Type: application/json');
        
        echo json_encode([
            'error' => true,
            'status' => 404,
            'message' => "API endpoint not found: /$path",
            'available_endpoints' => $this->getAvailableEndpoints(),
            'timestamp' => time()
        ], JSON_PRETTY_PRINT);
    }
    
    /**
     * Send error response
     */
    private function sendError(int $statusCode, string $message): void
    {
        http_response_code($statusCode);
        header('Content-Type: application/json');
        
        echo json_encode([
            'error' => true,
            'status' => $statusCode,
            'message' => $message,
            'timestamp' => time()
        ], JSON_PRETTY_PRINT);
    }
    
    /**
     * Get list of available API endpoints
     */
    private function getAvailableEndpoints(): array
    {
        $endpoints = [];
        
        if (!is_dir($this->apiDirectory)) {
            return $endpoints;
        }
        
        $iterator = new \RecursiveIteratorIterator(
            new \RecursiveDirectoryIterator($this->apiDirectory)
        );
        
        foreach ($iterator as $file) {
            if ($file->isFile() && $file->getExtension() === 'tsk') {
                $relativePath = str_replace($this->apiDirectory . '/', '', $file->getPathname());
                $endpoints[] = '/' . $relativePath;
            }
        }
        
        return $endpoints;
    }
    
    /**
     * Add default middlewares
     */
    private function addDefaultMiddlewares(): void
    {
        // Rate limiting middleware
        $this->addMiddleware(function($request) {
            // Simple rate limiting based on IP
            $ip = $request['ip'];
            $key = "rate_limit_$ip";
            
            // Get current count from cache (using file-based cache for simplicity)
            $cacheFile = sys_get_temp_dir() . "/$key.cache";
            $count = 0;
            $resetTime = time() + 60; // 1 minute window
            
            if (file_exists($cacheFile)) {
                $data = json_decode(file_get_contents($cacheFile), true);
                if ($data && $data['reset_time'] > time()) {
                    $count = $data['count'];
                    $resetTime = $data['reset_time'];
                }
            }
            
            // Check rate limit (100 requests per minute)
            if ($count >= 100) {
                http_response_code(429);
                header('Content-Type: application/json');
                echo json_encode([
                    'error' => true,
                    'status' => 429,
                    'message' => 'Rate limit exceeded',
                    'reset_time' => $resetTime
                ]);
                return false;
            }
            
            // Increment count
            $count++;
            file_put_contents($cacheFile, json_encode([
                'count' => $count,
                'reset_time' => $resetTime
            ]));
            
            return true;
        });
        
        // Request logging middleware
        $this->addMiddleware(function($request) {
            $logEntry = [
                'timestamp' => date('Y-m-d H:i:s'),
                'method' => $request['method'],
                'uri' => $request['uri'],
                'ip' => $request['ip'],
                'user_agent' => $request['user_agent']
            ];
            
            error_log("FUJSEN API Request: " . json_encode($logEntry));
            return true;
        });
    }
}

// ========================================
// MAIN EXECUTION
// ========================================

// Only execute if this file is called directly
if (basename($_SERVER['SCRIPT_NAME']) === 'api-router.php') {
    $router = new FujsenApiRouter();
    $router->route();
} 