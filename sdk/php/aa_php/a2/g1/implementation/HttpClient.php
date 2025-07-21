<?php

namespace TuskLang\Communication\Http;

use Psr\Http\Message\RequestInterface;
use Psr\Http\Message\ResponseInterface;

/**
 * Advanced HTTP Client with comprehensive feature set
 * 
 * Features:
 * - All HTTP methods support
 * - SSL/TLS with certificate validation
 * - Middleware pipeline
 * - Authentication handling
 * - Connection pooling
 * - Rate limiting
 * - Retry mechanisms
 */
class HttpClient
{
    private array $config;
    private array $middleware = [];
    private array $connectionPool = [];
    private array $rateLimiter = [];
    private HttpHeaders $headers;
    private HttpAuth $auth;
    private HttpSecurity $security;

    public function __construct(array $config = [])
    {
        $this->config = array_merge([
            'timeout' => 30,
            'connect_timeout' => 10,
            'max_redirects' => 5,
            'user_agent' => 'TuskLang-HttpClient/1.0',
            'verify_ssl' => true,
            'verify_host' => true,
            'allow_redirects' => true,
            'decode_gzip' => true,
            'keep_alive' => true,
            'max_connections' => 100,
            'connection_timeout' => 300,
            'retry_attempts' => 3,
            'retry_delay' => 1000, // milliseconds
            'rate_limit' => 60, // requests per minute
            'enable_http2' => true,
            'enable_compression' => true
        ], $config);

        $this->headers = new HttpHeaders();
        $this->auth = new HttpAuth();
        $this->security = new HttpSecurity();
        
        $this->initializeDefaults();
    }

    /**
     * HTTP GET request
     */
    public function get(string $url, array $options = []): HttpResponse
    {
        return $this->request('GET', $url, $options);
    }

    /**
     * HTTP POST request
     */
    public function post(string $url, $data = null, array $options = []): HttpResponse
    {
        $options['data'] = $data;
        return $this->request('POST', $url, $options);
    }

    /**
     * HTTP PUT request
     */
    public function put(string $url, $data = null, array $options = []): HttpResponse
    {
        $options['data'] = $data;
        return $this->request('PUT', $url, $options);
    }

    /**
     * HTTP DELETE request
     */
    public function delete(string $url, array $options = []): HttpResponse
    {
        return $this->request('DELETE', $url, $options);
    }

    /**
     * HTTP PATCH request
     */
    public function patch(string $url, $data = null, array $options = []): HttpResponse
    {
        $options['data'] = $data;
        return $this->request('PATCH', $url, $options);
    }

    /**
     * HTTP HEAD request
     */
    public function head(string $url, array $options = []): HttpResponse
    {
        return $this->request('HEAD', $url, $options);
    }

    /**
     * HTTP OPTIONS request
     */
    public function options(string $url, array $options = []): HttpResponse
    {
        return $this->request('OPTIONS', $url, $options);
    }

    /**
     * Main request method with comprehensive features
     */
    public function request(string $method, string $url, array $options = []): HttpResponse
    {
        $startTime = microtime(true);
        
        try {
            // Validate and prepare request
            $this->validateRequest($method, $url, $options);
            $this->checkRateLimit($url);
            
            // Build request object
            $request = $this->buildRequest($method, $url, $options);
            
            // Apply middleware pipeline
            $response = $this->executeMiddleware($request, function($req) use ($options) {
                return $this->executeRequest($req, $options);
            });
            
            // Log and return response
            $this->logRequest($request, $response, microtime(true) - $startTime);
            return $response;
            
        } catch (\Exception $e) {
            $this->logError($method, $url, $e);
            
            // Retry logic
            if ($this->shouldRetry($e, $options)) {
                return $this->retryRequest($method, $url, $options);
            }
            
            throw new HttpException("HTTP request failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Execute the actual HTTP request
     */
    private function executeRequest(HttpRequest $request, array $options): HttpResponse
    {
        $ch = $this->createCurlHandle($request, $options);
        
        try {
            $responseData = curl_exec($ch);
            
            if ($responseData === false) {
                throw new HttpException('cURL Error: ' . curl_error($ch));
            }
            
            $info = curl_getinfo($ch);
            $headerSize = curl_getinfo($ch, CURLINFO_HEADER_SIZE);
            
            $headers = substr($responseData, 0, $headerSize);
            $body = substr($responseData, $headerSize);
            
            curl_close($ch);
            
            return new HttpResponse(
                $info['http_code'],
                $this->parseHeaders($headers),
                $body,
                $info
            );
            
        } finally {
            if (is_resource($ch)) {
                curl_close($ch);
            }
        }
    }

    /**
     * Create and configure cURL handle
     */
    private function createCurlHandle(HttpRequest $request, array $options): \CurlHandle
    {
        $ch = curl_init();
        
        // Basic options
        curl_setopt_array($ch, [
            CURLOPT_URL => $request->getUrl(),
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_HEADER => true,
            CURLOPT_TIMEOUT => $this->config['timeout'],
            CURLOPT_CONNECTTIMEOUT => $this->config['connect_timeout'],
            CURLOPT_MAXREDIRS => $this->config['max_redirects'],
            CURLOPT_FOLLOWLOCATION => $this->config['allow_redirects'],
            CURLOPT_USERAGENT => $this->config['user_agent'],
            CURLOPT_ENCODING => $this->config['decode_gzip'] ? '' : null,
            CURLOPT_HTTP_VERSION => $this->config['enable_http2'] ? CURL_HTTP_VERSION_2_0 : CURL_HTTP_VERSION_1_1,
        ]);
        
        // SSL/TLS configuration
        if ($this->config['verify_ssl']) {
            curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, true);
            curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, $this->config['verify_host'] ? 2 : 0);
        } else {
            curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, false);
            curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, 0);
        }
        
        // Method-specific configuration
        $this->configureCurlMethod($ch, $request);
        
        // Headers
        $headers = $request->getHeaders();
        if (!empty($headers)) {
            curl_setopt($ch, CURLOPT_HTTPHEADER, $headers);
        }
        
        // Connection pooling
        if ($this->config['keep_alive']) {
            curl_setopt($ch, CURLOPT_TCP_KEEPALIVE, 1);
            curl_setopt($ch, CURLOPT_TCP_KEEPIDLE, 120);
            curl_setopt($ch, CURLOPT_TCP_KEEPINTVL, 60);
        }
        
        return $ch;
    }

    /**
     * Configure cURL for specific HTTP methods
     */
    private function configureCurlMethod(\CurlHandle $ch, HttpRequest $request): void
    {
        $method = $request->getMethod();
        $data = $request->getData();
        
        switch (strtoupper($method)) {
            case 'POST':
                curl_setopt($ch, CURLOPT_POST, true);
                if ($data !== null) {
                    curl_setopt($ch, CURLOPT_POSTFIELDS, $this->prepareData($data));
                }
                break;
                
            case 'PUT':
                curl_setopt($ch, CURLOPT_CUSTOMREQUEST, 'PUT');
                if ($data !== null) {
                    curl_setopt($ch, CURLOPT_POSTFIELDS, $this->prepareData($data));
                }
                break;
                
            case 'PATCH':
                curl_setopt($ch, CURLOPT_CUSTOMREQUEST, 'PATCH');
                if ($data !== null) {
                    curl_setopt($ch, CURLOPT_POSTFIELDS, $this->prepareData($data));
                }
                break;
                
            case 'DELETE':
                curl_setopt($ch, CURLOPT_CUSTOMREQUEST, 'DELETE');
                if ($data !== null) {
                    curl_setopt($ch, CURLOPT_POSTFIELDS, $this->prepareData($data));
                }
                break;
                
            case 'HEAD':
                curl_setopt($ch, CURLOPT_NOBODY, true);
                break;
                
            case 'OPTIONS':
                curl_setopt($ch, CURLOPT_CUSTOMREQUEST, 'OPTIONS');
                break;
        }
    }

    /**
     * Prepare request data for transmission
     */
    private function prepareData($data): string
    {
        if (is_string($data)) {
            return $data;
        }
        
        if (is_array($data) || is_object($data)) {
            return json_encode($data);
        }
        
        return (string) $data;
    }

    /**
     * Add middleware to the pipeline
     */
    public function addMiddleware(callable $middleware): self
    {
        $this->middleware[] = $middleware;
        return $this;
    }

    /**
     * Execute middleware pipeline
     */
    private function executeMiddleware(HttpRequest $request, callable $handler): HttpResponse
    {
        $pipeline = array_reverse($this->middleware);
        
        $next = $handler;
        foreach ($pipeline as $middleware) {
            $next = function($req) use ($middleware, $next) {
                return $middleware($req, $next);
            };
        }
        
        return $next($request);
    }

    /**
     * Set authentication credentials
     */
    public function setAuth(string $type, array $credentials): self
    {
        $this->auth->configure($type, $credentials);
        return $this;
    }

    /**
     * Set default headers
     */
    public function setHeaders(array $headers): self
    {
        $this->headers->setDefaults($headers);
        return $this;
    }

    /**
     * Rate limiting check
     */
    private function checkRateLimit(string $url): void
    {
        $host = parse_url($url, PHP_URL_HOST);
        $now = time();
        
        if (!isset($this->rateLimiter[$host])) {
            $this->rateLimiter[$host] = ['requests' => [], 'limit' => $this->config['rate_limit']];
        }
        
        $limiter = &$this->rateLimiter[$host];
        $limiter['requests'] = array_filter($limiter['requests'], fn($time) => $now - $time < 60);
        
        if (count($limiter['requests']) >= $limiter['limit']) {
            throw new HttpException('Rate limit exceeded for host: ' . $host);
        }
        
        $limiter['requests'][] = $now;
    }

    /**
     * Build request object from parameters
     */
    private function buildRequest(string $method, string $url, array $options): HttpRequest
    {
        $request = new HttpRequest($method, $url);
        
        // Add data if present
        if (isset($options['data'])) {
            $request->setData($options['data']);
        }
        
        // Merge headers
        $headers = array_merge(
            $this->headers->getDefaults(),
            $options['headers'] ?? []
        );
        
        // Add authentication headers
        $authHeaders = $this->auth->getHeaders($request);
        $headers = array_merge($headers, $authHeaders);
        
        $request->setHeaders($headers);
        
        return $request;
    }

    /**
     * Parse HTTP response headers
     */
    private function parseHeaders(string $headerString): array
    {
        $headers = [];
        $lines = explode("\r\n", trim($headerString));
        
        foreach ($lines as $line) {
            if (strpos($line, ':') !== false) {
                [$key, $value] = explode(':', $line, 2);
                $headers[trim($key)] = trim($value);
            }
        }
        
        return $headers;
    }

    /**
     * Initialize default configurations
     */
    private function initializeDefaults(): void
    {
        $this->headers->setDefaults([
            'Accept' => 'application/json, text/plain, */*',
            'Accept-Encoding' => 'gzip, deflate, br',
            'Connection' => 'keep-alive',
            'User-Agent' => $this->config['user_agent']
        ]);
    }

    /**
     * Validate request parameters
     */
    private function validateRequest(string $method, string $url, array $options): void
    {
        if (empty($url)) {
            throw new HttpException('URL cannot be empty');
        }
        
        if (!filter_var($url, FILTER_VALIDATE_URL)) {
            throw new HttpException('Invalid URL format: ' . $url);
        }
        
        $allowedMethods = ['GET', 'POST', 'PUT', 'DELETE', 'PATCH', 'HEAD', 'OPTIONS'];
        if (!in_array(strtoupper($method), $allowedMethods)) {
            throw new HttpException('Unsupported HTTP method: ' . $method);
        }
    }

    /**
     * Determine if request should be retried
     */
    private function shouldRetry(\Exception $e, array $options): bool
    {
        $attempts = $options['_retry_attempts'] ?? 0;
        return $attempts < $this->config['retry_attempts'] && $this->isRetriableError($e);
    }

    /**
     * Check if error is retriable
     */
    private function isRetriableError(\Exception $e): bool
    {
        // Network errors, timeouts, 5xx responses are retriable
        $message = $e->getMessage();
        return strpos($message, 'timeout') !== false ||
               strpos($message, 'connection') !== false ||
               ($e instanceof HttpException && $e->getCode() >= 500);
    }

    /**
     * Retry failed request
     */
    private function retryRequest(string $method, string $url, array $options): HttpResponse
    {
        $attempts = $options['_retry_attempts'] ?? 0;
        $options['_retry_attempts'] = $attempts + 1;
        
        // Exponential backoff
        $delay = $this->config['retry_delay'] * pow(2, $attempts);
        usleep($delay * 1000);
        
        return $this->request($method, $url, $options);
    }

    /**
     * Log successful requests
     */
    private function logRequest(HttpRequest $request, HttpResponse $response, float $duration): void
    {
        if ($this->config['debug'] ?? false) {
            error_log(sprintf(
                "HTTP %s %s -> %d (%.3fs)",
                $request->getMethod(),
                $request->getUrl(),
                $response->getStatusCode(),
                $duration
            ));
        }
    }

    /**
     * Log request errors
     */
    private function logError(string $method, string $url, \Exception $e): void
    {
        error_log(sprintf(
            "HTTP %s %s failed: %s",
            $method,
            $url,
            $e->getMessage()
        ));
    }
} 