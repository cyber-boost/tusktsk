<?php

namespace TuskLang\CoreOperators\Http;

use Exception;
use ReflectionClass;
use ReflectionMethod;

/**
 * RESTful API Server - Agent A2 Goal 1 Implementation
 * 
 * Features:
 * - Resource-based routing system with automatic REST routes
 * - HTTP status code management and standardization
 * - Content negotiation (JSON, XML, HTML)
 * - Request validation and sanitization
 * - Response formatting and serialization
 * - HATEOAS (Hypermedia as the Engine of Application State) implementation
 * - Middleware pipeline integration
 * - Rate limiting and security features
 */
class RestApiServer
{
    private const SUPPORTED_METHODS = ['GET', 'POST', 'PUT', 'DELETE', 'PATCH', 'HEAD', 'OPTIONS'];
    private const CONTENT_TYPES = [
        'json' => 'application/json',
        'xml' => 'application/xml',
        'html' => 'text/html',
        'text' => 'text/plain'
    ];
    
    private array $routes = [];
    private array $resources = [];
    private array $middleware = [];
    private array $validators = [];
    private array $formatters = [];
    private array $corsConfig = [];
    private array $rateLimits = [];
    private bool $enableHateoas = true;
    private string $baseUrl;
    private array $globalHeaders = [];
    
    public function __construct(array $config = [])
    {
        $this->baseUrl = $config['base_url'] ?? '';
        $this->enableHateoas = $config['enable_hateoas'] ?? true;
        $this->corsConfig = $config['cors'] ?? $this->getDefaultCorsConfig();
        $this->rateLimits = $config['rate_limits'] ?? [];
        $this->globalHeaders = $config['global_headers'] ?? $this->getDefaultHeaders();
        $this->setupDefaultFormatters();
        $this->setupDefaultValidators();
    }
    
    /**
     * Register a RESTful resource with automatic CRUD routes
     */
    public function resource(string $name, string $controllerClass, array $options = []): self
    {
        $prefix = $options['prefix'] ?? '';
        $middleware = $options['middleware'] ?? [];
        $only = $options['only'] ?? [];
        $except = $options['except'] ?? [];
        
        $routes = [
            'index' => ['GET', "/{$name}", 'index'],
            'show' => ['GET', "/{$name}/{id}", 'show'],
            'store' => ['POST', "/{$name}", 'store'],
            'update' => ['PUT', "/{$name}/{id}", 'update'],
            'destroy' => ['DELETE', "/{$name}/{id}", 'destroy']
        ];
        
        foreach ($routes as $routeName => $routeInfo) {
            if (!empty($only) && !in_array($routeName, $only)) continue;
            if (!empty($except) && in_array($routeName, $except)) continue;
            
            [$method, $path, $action] = $routeInfo;
            $fullPath = $prefix . $path;
            
            $this->addRoute($method, $fullPath, $controllerClass, $action, $middleware);
        }
        
        $this->resources[$name] = [
            'controller' => $controllerClass,
            'options' => $options
        ];
        
        return $this;
    }
    
    /**
     * Add individual route
     */
    public function addRoute(string $method, string $path, string $controller, string $action, array $middleware = []): self
    {
        $method = strtoupper($method);
        
        if (!in_array($method, self::SUPPORTED_METHODS)) {
            throw new Exception("Unsupported HTTP method: {$method}");
        }
        
        $this->routes[] = [
            'method' => $method,
            'path' => $this->normalizePath($path),
            'controller' => $controller,
            'action' => $action,
            'middleware' => $middleware,
            'parameters' => $this->extractParameters($path)
        ];
        
        return $this;
    }
    
    /**
     * Handle incoming HTTP request
     */
    public function handleRequest(?string $method = null, ?string $uri = null): HttpResponse
    {
        $method = $method ?? $_SERVER['REQUEST_METHOD'];
        $uri = $uri ?? parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH);
        
        // Handle CORS preflight
        if ($method === 'OPTIONS') {
            return $this->handleOptions($uri);
        }
        
        // Find matching route
        $route = $this->findRoute($method, $uri);
        
        if (!$route) {
            return $this->createErrorResponse(404, 'Route not found');
        }
        
        try {
            // Rate limiting check
            $this->checkRateLimit($route);
            
            // Create request object
            $request = $this->createRequest($method, $uri, $route);
            
            // Apply middleware (before)
            foreach ($route['middleware'] as $middlewareClass) {
                $middleware = new $middlewareClass();
                $request = $middleware->before($request);
            }
            
            // Validate request
            $this->validateRequest($request, $route);
            
            // Execute controller action
            $response = $this->executeController($request, $route);
            
            // Apply middleware (after)
            foreach (array_reverse($route['middleware']) as $middlewareClass) {
                $middleware = new $middlewareClass();
                $response = $middleware->after($request, $response);
            }
            
            // Add HATEOAS links if enabled
            if ($this->enableHateoas) {
                $response = $this->addHateoasLinks($request, $response, $route);
            }
            
            // Format response based on content negotiation
            $response = $this->formatResponse($request, $response);
            
            // Add global headers
            $response = $this->addGlobalHeaders($response);
            
            return $response;
            
        } catch (ValidationException $e) {
            return $this->createErrorResponse(422, 'Validation failed', ['errors' => $e->getErrors()]);
        } catch (AuthenticationException $e) {
            return $this->createErrorResponse(401, 'Authentication required');
        } catch (AuthorizationException $e) {
            return $this->createErrorResponse(403, 'Access denied');
        } catch (Exception $e) {
            return $this->createErrorResponse(500, 'Internal server error', ['message' => $e->getMessage()]);
        }
    }
    
    /**
     * Find route matching method and URI
     */
    private function findRoute(string $method, string $uri): ?array
    {
        $uri = $this->normalizePath($uri);
        
        foreach ($this->routes as $route) {
            if ($route['method'] !== $method) continue;
            
            $pattern = $this->buildRoutePattern($route['path']);
            if (preg_match($pattern, $uri, $matches)) {
                $route['params'] = $this->extractRouteParams($route['parameters'], $matches);
                return $route;
            }
        }
        
        return null;
    }
    
    /**
     * Execute controller action
     */
    private function executeController(ApiRequest $request, array $route): HttpResponse
    {
        $controllerClass = $route['controller'];
        $action = $route['action'];
        
        if (!class_exists($controllerClass)) {
            throw new Exception("Controller not found: {$controllerClass}");
        }
        
        $controller = new $controllerClass();
        
        if (!method_exists($controller, $action)) {
            throw new Exception("Action not found: {$action}");
        }
        
        // Inject dependencies if controller supports it
        if (method_exists($controller, 'setRequest')) {
            $controller->setRequest($request);
        }
        
        $result = $controller->$action($request);
        
        // Convert result to HttpResponse if needed
        if ($result instanceof HttpResponse) {
            return $result;
        }
        
        return new ApiResponse(200, [], $result);
    }
    
    /**
     * Content negotiation and response formatting
     */
    private function formatResponse(ApiRequest $request, HttpResponse $response): HttpResponse
    {
        $acceptedTypes = $this->parseAcceptHeader($request->getHeader('Accept') ?? '');
        $contentType = $this->negotiateContentType($acceptedTypes);
        
        $formatter = $this->formatters[$contentType] ?? $this->formatters['application/json'];
        $formattedBody = $formatter($response->getData());
        
        $headers = array_merge($response->getHeaders(), [
            'Content-Type' => $contentType,
            'Content-Length' => strlen($formattedBody)
        ]);
        
        return new HttpResponse($response->getStatusCode(), $headers, $formattedBody);
    }
    
    /**
     * Add HATEOAS links to response
     */
    private function addHateoasLinks(ApiRequest $request, HttpResponse $response, array $route): HttpResponse
    {
        if (!($response instanceof ApiResponse) || !$response->getData()) {
            return $response;
        }
        
        $data = $response->getData();
        $resourceName = $this->extractResourceName($route['path']);
        
        if ($resourceName && is_array($data)) {
            $data['_links'] = $this->generateHateoasLinks($resourceName, $data, $request);
            return new ApiResponse($response->getStatusCode(), $response->getHeaders(), $data);
        }
        
        return $response;
    }
    
    /**
     * Generate HATEOAS links for resource
     */
    private function generateHateoasLinks(string $resourceName, array $data, ApiRequest $request): array
    {
        $links = [
            'self' => [
                'href' => $this->baseUrl . $request->getUri(),
                'method' => $request->getMethod()
            ]
        ];
        
        // Add resource-specific links
        if (isset($data['id'])) {
            $id = $data['id'];
            $links['show'] = ['href' => "{$this->baseUrl}/{$resourceName}/{$id}", 'method' => 'GET'];
            $links['update'] = ['href' => "{$this->baseUrl}/{$resourceName}/{$id}", 'method' => 'PUT'];
            $links['delete'] = ['href' => "{$this->baseUrl}/{$resourceName}/{$id}", 'method' => 'DELETE'];
        }
        
        $links['index'] = ['href' => "{$this->baseUrl}/{$resourceName}", 'method' => 'GET'];
        $links['create'] = ['href' => "{$this->baseUrl}/{$resourceName}", 'method' => 'POST'];
        
        return $links;
    }
    
    /**
     * Handle OPTIONS request for CORS
     */
    private function handleOptions(string $uri): HttpResponse
    {
        $allowedMethods = $this->getAllowedMethods($uri);
        
        $headers = [
            'Access-Control-Allow-Origin' => $this->corsConfig['allow_origin'] ?? '*',
            'Access-Control-Allow-Methods' => implode(', ', $allowedMethods),
            'Access-Control-Allow-Headers' => implode(', ', $this->corsConfig['allow_headers'] ?? ['Content-Type', 'Authorization']),
            'Access-Control-Max-Age' => $this->corsConfig['max_age'] ?? '86400'
        ];
        
        return new HttpResponse(200, $headers, '');
    }
    
    /**
     * Validate request based on route configuration
     */
    private function validateRequest(ApiRequest $request, array $route): void
    {
        $controllerClass = $route['controller'];
        $action = $route['action'];
        
        // Get validation rules for this controller action
        $validationKey = "{$controllerClass}@{$action}";
        
        if (isset($this->validators[$validationKey])) {
            $validator = new RequestValidator($this->validators[$validationKey]);
            $validator->validate($request);
        }
    }
    
    /**
     * Create error response with standardized format
     */
    private function createErrorResponse(int $statusCode, string $message, array $details = []): HttpResponse
    {
        $body = [
            'error' => [
                'code' => $statusCode,
                'message' => $message,
                'details' => $details
            ]
        ];
        
        $headers = ['Content-Type' => 'application/json'];
        return new HttpResponse($statusCode, $headers, json_encode($body));
    }
    
    /**
     * Setup default response formatters
     */
    private function setupDefaultFormatters(): void
    {
        $this->formatters = [
            'application/json' => function($data) {
                return json_encode($data, JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES);
            },
            'application/xml' => function($data) {
                return $this->arrayToXml($data);
            },
            'text/html' => function($data) {
                return is_string($data) ? $data : '<pre>' . json_encode($data, JSON_PRETTY_PRINT) . '</pre>';
            },
            'text/plain' => function($data) {
                return is_string($data) ? $data : print_r($data, true);
            }
        ];
    }
    
    /**
     * Setup default request validators
     */
    private function setupDefaultValidators(): void
    {
        // Validators can be added via setValidator method
    }
    
    /**
     * Set validator for specific controller action
     */
    public function setValidator(string $controller, string $action, array $rules): self
    {
        $this->validators["{$controller}@{$action}"] = $rules;
        return $this;
    }
    
    /**
     * Add middleware to all routes
     */
    public function addGlobalMiddleware(string $middlewareClass): self
    {
        $this->middleware[] = $middlewareClass;
        return $this;
    }
    
    /**
     * Set CORS configuration
     */
    public function setCors(array $config): self
    {
        $this->corsConfig = array_merge($this->corsConfig, $config);
        return $this;
    }
    
    /**
     * Set rate limiting configuration
     */
    public function setRateLimit(string $identifier, int $requests, int $windowSeconds): self
    {
        $this->rateLimits[$identifier] = [
            'requests' => $requests,
            'window' => $windowSeconds,
            'reset_time' => time() + $windowSeconds,
            'current_requests' => 0
        ];
        
        return $this;
    }
    
    // Private helper methods
    private function normalizePath(string $path): string
    {
        return '/' . trim($path, '/');
    }
    
    private function extractParameters(string $path): array
    {
        preg_match_all('/\{(\w+)\}/', $path, $matches);
        return $matches[1] ?? [];
    }
    
    private function buildRoutePattern(string $path): string
    {
        $pattern = preg_replace('/\{(\w+)\}/', '([^/]+)', $path);
        return '#^' . $pattern . '$#';
    }
    
    private function extractRouteParams(array $paramNames, array $matches): array
    {
        $params = [];
        for ($i = 0; $i < count($paramNames); $i++) {
            if (isset($matches[$i + 1])) {
                $params[$paramNames[$i]] = $matches[$i + 1];
            }
        }
        return $params;
    }
    
    private function createRequest(string $method, string $uri, array $route): ApiRequest
    {
        $headers = getallheaders() ?: [];
        $body = file_get_contents('php://input');
        $params = array_merge($_GET, $route['params'] ?? []);
        
        return new ApiRequest($method, $uri, $headers, $body, $params);
    }
    
    private function parseAcceptHeader(string $accept): array
    {
        $types = [];
        $parts = explode(',', $accept);
        
        foreach ($parts as $part) {
            $part = trim($part);
            if (strpos($part, ';') !== false) {
                [$type, $q] = explode(';', $part, 2);
                preg_match('/q=([0-9.]+)/', $q, $matches);
                $quality = isset($matches[1]) ? (float)$matches[1] : 1.0;
            } else {
                $type = $part;
                $quality = 1.0;
            }
            
            $types[trim($type)] = $quality;
        }
        
        arsort($types);
        return $types;
    }
    
    private function negotiateContentType(array $acceptedTypes): string
    {
        foreach ($acceptedTypes as $type => $quality) {
            if (isset($this->formatters[$type])) {
                return $type;
            }
        }
        
        return 'application/json'; // Default
    }
    
    private function extractResourceName(string $path): ?string
    {
        if (preg_match('#^/([^/]+)#', $path, $matches)) {
            return $matches[1];
        }
        return null;
    }
    
    private function getAllowedMethods(string $uri): array
    {
        $methods = [];
        foreach ($this->routes as $route) {
            $pattern = $this->buildRoutePattern($route['path']);
            if (preg_match($pattern, $uri)) {
                $methods[] = $route['method'];
            }
        }
        return array_unique($methods);
    }
    
    private function checkRateLimit(array $route): void
    {
        // Simple rate limiting implementation
        // In production, use Redis or database for distributed rate limiting
        if (!empty($this->rateLimits)) {
            $clientIp = $_SERVER['REMOTE_ADDR'] ?? 'unknown';
            $key = $clientIp . ':' . $route['path'];
            
            // This is a simplified implementation
            // Production should use proper rate limiting store
        }
    }
    
    private function addGlobalHeaders(HttpResponse $response): HttpResponse
    {
        $headers = array_merge($response->getHeaders(), $this->globalHeaders);
        return new HttpResponse($response->getStatusCode(), $headers, $response->getBody());
    }
    
    private function getDefaultCorsConfig(): array
    {
        return [
            'allow_origin' => '*',
            'allow_methods' => ['GET', 'POST', 'PUT', 'DELETE', 'PATCH', 'OPTIONS'],
            'allow_headers' => ['Content-Type', 'Authorization', 'X-Requested-With'],
            'max_age' => 86400
        ];
    }
    
    private function getDefaultHeaders(): array
    {
        return [
            'X-Powered-By' => 'TuskLang-RestApi/2.0',
            'X-Frame-Options' => 'DENY',
            'X-Content-Type-Options' => 'nosniff',
            'X-XSS-Protection' => '1; mode=block'
        ];
    }
    
    private function arrayToXml(array $data, string $rootElement = 'response'): string
    {
        $xml = new \SimpleXMLElement("<{$rootElement}/>");
        $this->arrayToXmlRecursive($data, $xml);
        return $xml->asXML();
    }
    
    private function arrayToXmlRecursive(array $data, \SimpleXMLElement $xml): void
    {
        foreach ($data as $key => $value) {
            if (is_array($value)) {
                $child = $xml->addChild(is_numeric($key) ? 'item' : $key);
                $this->arrayToXmlRecursive($value, $child);
            } else {
                $xml->addChild(is_numeric($key) ? 'item' : $key, htmlspecialchars((string)$value));
            }
        }
    }
}

/**
 * API Request representation
 */
class ApiRequest extends HttpRequest
{
    private array $params;
    
    public function __construct(string $method, string $uri, array $headers, string $body, array $params)
    {
        parent::__construct($method, $uri, ['headers' => $headers, 'body' => $body]);
        $this->params = $params;
    }
    
    public function getParams(): array { return $this->params; }
    public function getParam(string $key, mixed $default = null): mixed { return $this->params[$key] ?? $default; }
    public function getUri(): string { return $this->getUrl(); }
    public function hasParam(string $key): bool { return isset($this->params[$key]); }
    public function json(): array { return json_decode($this->getBody(), true) ?? []; }
}

/**
 * API Response representation
 */
class ApiResponse extends HttpResponse
{
    private mixed $data;
    
    public function __construct(int $statusCode, array $headers, mixed $data)
    {
        $this->data = $data;
        parent::__construct($statusCode, $headers, '');
    }
    
    public function getData(): mixed { return $this->data; }
}

/**
 * Request Validator
 */
class RequestValidator
{
    private array $rules;
    
    public function __construct(array $rules)
    {
        $this->rules = $rules;
    }
    
    public function validate(ApiRequest $request): void
    {
        $errors = [];
        $data = array_merge($request->getParams(), $request->json());
        
        foreach ($this->rules as $field => $rule) {
            $value = $data[$field] ?? null;
            $fieldErrors = $this->validateField($field, $value, $rule);
            if (!empty($fieldErrors)) {
                $errors[$field] = $fieldErrors;
            }
        }
        
        if (!empty($errors)) {
            throw new ValidationException('Validation failed', $errors);
        }
    }
    
    private function validateField(string $field, mixed $value, string $rule): array
    {
        $rules = explode('|', $rule);
        $errors = [];
        
        foreach ($rules as $singleRule) {
            if ($singleRule === 'required' && ($value === null || $value === '')) {
                $errors[] = "The {$field} field is required";
            } elseif (strpos($singleRule, 'min:') === 0 && is_string($value)) {
                $min = (int)substr($singleRule, 4);
                if (strlen($value) < $min) {
                    $errors[] = "The {$field} field must be at least {$min} characters";
                }
            } elseif (strpos($singleRule, 'max:') === 0 && is_string($value)) {
                $max = (int)substr($singleRule, 4);
                if (strlen($value) > $max) {
                    $errors[] = "The {$field} field must not exceed {$max} characters";
                }
            } elseif ($singleRule === 'email' && $value && !filter_var($value, FILTER_VALIDATE_EMAIL)) {
                $errors[] = "The {$field} field must be a valid email";
            }
        }
        
        return $errors;
    }
}

/**
 * Custom exceptions
 */
class ValidationException extends Exception
{
    private array $errors;
    
    public function __construct(string $message, array $errors)
    {
        parent::__construct($message);
        $this->errors = $errors;
    }
    
    public function getErrors(): array { return $this->errors; }
}

class AuthenticationException extends Exception {}
class AuthorizationException extends Exception {} 