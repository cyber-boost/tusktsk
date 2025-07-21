<?php

namespace TuskLang\Tests\Communication\Grpc;

use PHPUnit\Framework\TestCase;
use TuskLang\Communication\Grpc\GrpcServer;
use TuskLang\Communication\Grpc\ProtobufSerializer;
use TuskLang\Communication\Grpc\ServiceRegistry;

class GrpcServerTest extends TestCase
{
    private GrpcServer $server;

    protected function setUp(): void
    {
        $this->server = new GrpcServer([
            'host' => '127.0.0.1',
            'port' => 9001,
            'enable_health_check' => true
        ]);
    }

    public function testServiceRegistration(): void
    {
        $mockService = $this->createMockService();
        
        $this->server->addService('TestService', $mockService);
        $stats = $this->server->getStats();
        
        $this->assertEquals(1, $stats['services_registered']);
    }

    public function testProtobufSerialization(): void
    {
        $serializer = new ProtobufSerializer();
        
        // Mock message
        $message = new class {
            public function serializeToString() { return 'serialized'; }
        };
        
        $result = $serializer->serialize($message);
        $this->assertEquals('serialized', $result);
    }

    public function testServiceRegistry(): void
    {
        $registry = new ServiceRegistry();
        $mockService = $this->createMockService();
        
        $registry->register('TestService', $mockService);
        $services = $registry->getServices();
        
        $this->assertContains('TestService', $services);
    }

    public function testInterceptorChain(): void
    {
        $interceptor = $this->createMockInterceptor();
        
        $this->server->addInterceptor($interceptor);
        $stats = $this->server->getStats();
        
        $this->assertEquals(1, $stats['interceptors_count']);
    }

    private function createMockService()
    {
        return new class implements \Spiral\GRPC\ServiceInterface {
            public function testMethod() {
                return 'test response';
            }
        };
    }

    private function createMockInterceptor()
    {
        return new class implements \TuskLang\Communication\Grpc\InterceptorInterface {
            public function intercept($call, $context) {
                return $call;
            }
        };
    }
} 