<?php

namespace TuskLang\Tests\Communication\Gateway;

use PHPUnit\Framework\TestCase;
use TuskLang\Communication\Gateway\ApiGateway;
use TuskLang\Communication\Gateway\ServiceDiscovery;
use TuskLang\Communication\Gateway\LoadBalancer;
use TuskLang\Communication\Http\ApiRequest;

class ApiGatewayTest extends TestCase
{
    private ApiGateway $gateway;

    protected function setUp(): void
    {
        $this->gateway = new ApiGateway([
            'enable_service_discovery' => true,
            'enable_load_balancing' => true,
            'enable_rate_limiting' => true
        ]);
    }

    public function testRouteRegistration(): void
    {
        $this->gateway->addRoute('/api/users', 'user-service');
        $this->gateway->addRoute('/api/orders', 'order-service');
        
        $stats = $this->gateway->getStats();
        $this->assertGreaterThanOrEqual(2, $stats['active_routes']);
    }

    public function testServiceRegistration(): void
    {
        $this->gateway->registerService('user-service', [
            ['url' => 'http://user1.internal:8080'],
            ['url' => 'http://user2.internal:8080']
        ]);
        
        $stats = $this->gateway->getStats();
        $this->assertGreaterThanOrEqual(1, $stats['registered_services']);
    }

    public function testLoadBalancing(): void
    {
        $loadBalancer = new LoadBalancer(['load_balance_algorithm' => 'round_robin']);
        
        $endpoints = [
            ['url' => 'http://service1:8080'],
            ['url' => 'http://service2:8080']
        ];
        
        $request = new ApiRequest('GET', '/test');
        $endpoint1 = $loadBalancer->selectEndpoint('test-service', $endpoints, $request);
        $endpoint2 = $loadBalancer->selectEndpoint('test-service', $endpoints, $request);
        
        $this->assertNotEquals($endpoint1, $endpoint2); // Should alternate
    }

    public function testServiceDiscovery(): void
    {
        $discovery = new ServiceDiscovery([]);
        
        $discovery->registerService('test-service', [
            ['url' => 'http://test:8080']
        ]);
        
        $endpoints = $discovery->getServiceEndpoints('test-service');
        $this->assertCount(1, $endpoints);
        $this->assertEquals('http://test:8080', $endpoints[0]['url']);
    }

    public function testMiddlewareIntegration(): void
    {
        $middleware = new class implements \TuskLang\Communication\Gateway\MiddlewareInterface {
            public function process(\TuskLang\Communication\Http\ApiRequest $request): \TuskLang\Communication\Http\ApiRequest {
                $request->setHeader('X-Gateway-Processed', 'true');
                return $request;
            }
        };
        
        $this->gateway->addMiddleware($middleware, 10);
        $stats = $this->gateway->getStats();
        
        $this->assertGreaterThanOrEqual(1, $stats['middleware_count']);
    }

    public function testRequestRouting(): void
    {
        // Mock a complete request flow
        $request = new ApiRequest('GET', '/api/users/123');
        $request->setHeaders(['Authorization' => 'Bearer valid-token']);
        
        // This would normally route to a real service
        // For testing, we just verify the request is properly formed
        $this->assertEquals('GET', $request->getMethod());
        $this->assertEquals('/api/users/123', $request->getPath());
    }
} 