<?php

namespace TuskLang\Tests\Communication\Http;

use PHPUnit\Framework\TestCase;
use TuskLang\Communication\Http\RestApiServer;
use TuskLang\Communication\Http\ApiRequest;
use TuskLang\Communication\Http\ApiResponse;

/**
 * Test suite for RestApiServer
 */
class RestApiServerTest extends TestCase
{
    private RestApiServer $server;

    protected function setUp(): void
    {
        $this->server = new RestApiServer([
            'cors_enabled' => true,
            'rate_limit_enabled' => false // Disable for testing
        ]);
    }

    public function testGetRoute(): void
    {
        $this->server->get('/test', function(ApiRequest $request) {
            return ApiResponse::success(['message' => 'Hello World']);
        });
        
        $response = $this->simulateRequest('GET', '/test');
        
        $this->assertEquals(200, $response->getStatusCode());
        $data = json_decode($response->getBody(), true);
        $this->assertEquals('Hello World', $data['message']);
    }

    public function testPostRoute(): void
    {
        $this->server->post('/users', function(ApiRequest $request) {
            $data = $request->getData();
            return ApiResponse::created(['id' => 1, 'name' => $data['name']]);
        });
        
        $response = $this->simulateRequest('POST', '/users', ['name' => 'John']);
        
        $this->assertEquals(201, $response->getStatusCode());
        $data = json_decode($response->getBody(), true);
        $this->assertEquals('John', $data['name']);
    }

    public function testResourceRoutes(): void
    {
        $this->server->resource('users', TestController::class);
        
        // Test index route
        $response = $this->simulateRequest('GET', '/users');
        $this->assertEquals(200, $response->getStatusCode());
        
        // Test show route with parameter
        $response = $this->simulateRequest('GET', '/users/123');
        $this->assertEquals(200, $response->getStatusCode());
        $data = json_decode($response->getBody(), true);
        $this->assertEquals('123', $data['id']);
    }

    public function testRouteParameters(): void
    {
        $this->server->get('/users/{id}/posts/{postId}', function(ApiRequest $request) {
            return ApiResponse::success([
                'userId' => $request->getRouteParam('id'),
                'postId' => $request->getRouteParam('postId')
            ]);
        });
        
        $response = $this->simulateRequest('GET', '/users/456/posts/789');
        
        $data = json_decode($response->getBody(), true);
        $this->assertEquals('456', $data['userId']);
        $this->assertEquals('789', $data['postId']);
    }

    public function testMiddleware(): void
    {
        $this->server->middleware(function(ApiRequest $request, $next) {
            $response = $next($request);
            $response->addHeader('X-Custom-Middleware', 'executed');
            return $response;
        });
        
        $this->server->get('/middleware-test', function() {
            return ApiResponse::success(['test' => true]);
        });
        
        $response = $this->simulateRequest('GET', '/middleware-test');
        
        $this->assertEquals('executed', $response->getHeader('X-Custom-Middleware'));
    }

    public function testContentNegotiation(): void
    {
        $this->server->get('/content', function() {
            return ApiResponse::success(['message' => 'test']);
        });
        
        // Test JSON response
        $response = $this->simulateRequest('GET', '/content', null, ['Accept' => 'application/json']);
        $this->assertStringContains('application/json', $response->getHeader('Content-Type'));
        
        // Test XML response
        $response = $this->simulateRequest('GET', '/content', null, ['Accept' => 'application/xml']);
        $this->assertStringContains('application/xml', $response->getHeader('Content-Type'));
    }

    public function testValidation(): void
    {
        $this->server->post('/validate', function(ApiRequest $request) {
            $errors = $request->validate([
                'name' => 'required|string|min:3',
                'email' => 'required|email',
                'age' => 'integer|min:18'
            ]);
            
            if (!empty($errors)) {
                return ApiResponse::validationError($errors);
            }
            
            return ApiResponse::success(['valid' => true]);
        });
        
        // Test validation failure
        $response = $this->simulateRequest('POST', '/validate', ['name' => 'Jo']);
        $this->assertEquals(422, $response->getStatusCode());
        
        // Test validation success
        $response = $this->simulateRequest('POST', '/validate', [
            'name' => 'John',
            'email' => 'john@example.com',
            'age' => 25
        ]);
        $this->assertEquals(200, $response->getStatusCode());
    }

    public function testErrorHandling(): void
    {
        $this->server->get('/error', function() {
            throw new \Exception('Test error');
        });
        
        $response = $this->simulateRequest('GET', '/error');
        
        $this->assertEquals(500, $response->getStatusCode());
        $data = json_decode($response->getBody(), true);
        $this->assertArrayHasKey('error', $data);
    }

    public function testNotFound(): void
    {
        $response = $this->simulateRequest('GET', '/nonexistent');
        
        $this->assertEquals(404, $response->getStatusCode());
    }

    public function testCorsHeaders(): void
    {
        $this->server->get('/cors-test', function() {
            return ApiResponse::success([]);
        });
        
        $response = $this->simulateRequest('GET', '/cors-test');
        
        $this->assertNotNull($response->getHeader('Access-Control-Allow-Origin'));
        $this->assertNotNull($response->getHeader('Access-Control-Allow-Methods'));
    }

    public function testHateoas(): void
    {
        $server = new RestApiServer(['hateoas_enabled' => true]);
        
        $server->get('/users/{id}', function(ApiRequest $request) {
            return ApiResponse::success([
                'id' => $request->getRouteParam('id'),
                'name' => 'John Doe'
            ]);
        });
        
        $response = $this->simulateRequest('GET', '/users/123', null, [], $server);
        
        $data = json_decode($response->getBody(), true);
        $this->assertArrayHasKey('_links', $data);
        $this->assertArrayHasKey('self', $data['_links']);
    }

    /**
     * Simulate HTTP request for testing
     */
    private function simulateRequest(
        string $method, 
        string $path, 
        ?array $data = null, 
        array $headers = [],
        ?RestApiServer $server = null
    ): ApiResponse {
        $server = $server ?? $this->server;
        
        // Create mock server data
        $serverData = [
            'REQUEST_METHOD' => $method,
            'REQUEST_URI' => $path,
            'HTTP_HOST' => 'localhost',
        ];
        
        // Add headers
        foreach ($headers as $key => $value) {
            $serverData['HTTP_' . strtoupper(str_replace('-', '_', $key))] = $value;
        }
        
        // Mock global variables for testing
        $_GET = [];
        $_POST = $data ?? [];
        
        if ($data && in_array($method, ['POST', 'PUT', 'PATCH'])) {
            $serverData['CONTENT_TYPE'] = 'application/json';
        }
        
        ob_start();
        $server->handle($serverData);
        $output = ob_get_clean();
        
        // Parse the output to create response object
        $statusCode = http_response_code() ?: 200;
        $headers = [];
        
        // In a real scenario, we'd capture headers from header() calls
        // For testing, we'll create a mock response
        return new ApiResponse($statusCode, $headers, $output);
    }
}

/**
 * Test controller for resource route testing
 */
class TestController
{
    public function index(ApiRequest $request): ApiResponse
    {
        return ApiResponse::success(['users' => ['user1', 'user2']]);
    }
    
    public function show(ApiRequest $request): ApiResponse
    {
        return ApiResponse::success(['id' => $request->getRouteParam('id')]);
    }
    
    public function store(ApiRequest $request): ApiResponse
    {
        return ApiResponse::created(['created' => true]);
    }
    
    public function update(ApiRequest $request): ApiResponse
    {
        return ApiResponse::success(['updated' => true]);
    }
    
    public function destroy(ApiRequest $request): ApiResponse
    {
        return ApiResponse::noContent();
    }
} 