<?php

namespace TuskLang\Tests\Communication\Http;

use PHPUnit\Framework\TestCase;
use TuskLang\Communication\Http\HttpClient;
use TuskLang\Communication\Http\HttpResponse;
use TuskLang\Communication\Http\HttpException;

/**
 * Test suite for HttpClient
 */
class HttpClientTest extends TestCase
{
    private HttpClient $client;

    protected function setUp(): void
    {
        $this->client = new HttpClient([
            'timeout' => 5,
            'verify_ssl' => false // For testing
        ]);
    }

    public function testGetRequest(): void
    {
        $response = $this->client->get('https://httpbin.org/get');
        
        $this->assertInstanceOf(HttpResponse::class, $response);
        $this->assertEquals(200, $response->getStatusCode());
        $this->assertTrue($response->isSuccess());
        
        $data = $response->getJson();
        $this->assertIsArray($data);
        $this->assertArrayHasKey('url', $data);
    }

    public function testPostRequest(): void
    {
        $postData = ['test' => 'data', 'number' => 123];
        $response = $this->client->post('https://httpbin.org/post', $postData);
        
        $this->assertEquals(200, $response->getStatusCode());
        
        $data = $response->getJson();
        $this->assertArrayHasKey('json', $data);
        $this->assertEquals($postData, $data['json']);
    }

    public function testPutRequest(): void
    {
        $putData = ['updated' => true];
        $response = $this->client->put('https://httpbin.org/put', $putData);
        
        $this->assertEquals(200, $response->getStatusCode());
        
        $data = $response->getJson();
        $this->assertEquals($putData, $data['json']);
    }

    public function testDeleteRequest(): void
    {
        $response = $this->client->delete('https://httpbin.org/delete');
        
        $this->assertEquals(200, $response->getStatusCode());
    }

    public function testCustomHeaders(): void
    {
        $this->client->setHeaders([
            'X-Custom-Header' => 'test-value',
            'Authorization' => 'Bearer token123'
        ]);
        
        $response = $this->client->get('https://httpbin.org/headers');
        $data = $response->getJson();
        
        $this->assertArrayHasKey('X-Custom-Header', $data['headers']);
        $this->assertEquals('test-value', $data['headers']['X-Custom-Header']);
    }

    public function testAuthentication(): void
    {
        $this->client->setAuth('basic', [
            'username' => 'testuser',
            'password' => 'testpass'
        ]);
        
        $response = $this->client->get('https://httpbin.org/basic-auth/testuser/testpass');
        
        $this->assertEquals(200, $response->getStatusCode());
        
        $data = $response->getJson();
        $this->assertTrue($data['authenticated']);
    }

    public function testRateLimiting(): void
    {
        $client = new HttpClient(['rate_limit' => 2]);
        
        // First two requests should work
        $client->get('https://httpbin.org/get');
        $client->get('https://httpbin.org/get');
        
        // Third request should fail
        $this->expectException(HttpException::class);
        $this->expectExceptionMessage('Rate limit exceeded');
        $client->get('https://httpbin.org/get');
    }

    public function testMiddleware(): void
    {
        $middlewareCalled = false;
        
        $this->client->addMiddleware(function($request, $next) use (&$middlewareCalled) {
            $middlewareCalled = true;
            $request->addHeader('X-Middleware', 'processed');
            return $next($request);
        });
        
        $response = $this->client->get('https://httpbin.org/headers');
        $data = $response->getJson();
        
        $this->assertTrue($middlewareCalled);
        $this->assertArrayHasKey('X-Middleware', $data['headers']);
    }

    public function testInvalidUrl(): void
    {
        $this->expectException(HttpException::class);
        $this->client->get('invalid-url');
    }

    public function test404Response(): void
    {
        $response = $this->client->get('https://httpbin.org/status/404');
        
        $this->assertEquals(404, $response->getStatusCode());
        $this->assertTrue($response->isClientError());
        $this->assertFalse($response->isSuccess());
    }

    public function testTimeout(): void
    {
        $client = new HttpClient(['timeout' => 1]);
        
        $this->expectException(HttpException::class);
        $client->get('https://httpbin.org/delay/5');
    }

    public function testRetryMechanism(): void
    {
        $client = new HttpClient(['retry_attempts' => 2, 'retry_delay' => 100]);
        
        // This should retry on 5xx errors
        $response = $client->get('https://httpbin.org/status/500');
        
        // Eventually should still return the error response
        $this->assertEquals(500, $response->getStatusCode());
    }

    public function testJsonResponse(): void
    {
        $response = $this->client->get('https://httpbin.org/json');
        
        $this->assertEquals('application/json', $response->getHeader('Content-Type'));
        $this->assertIsArray($response->getJson());
    }

    public function testResponseHeaders(): void
    {
        $response = $this->client->get('https://httpbin.org/response-headers', [
            'headers' => ['Accept' => 'application/json']
        ]);
        
        $headers = $response->getHeaders();
        $this->assertIsArray($headers);
        $this->assertArrayHasKey('Content-Type', $headers);
    }
} 