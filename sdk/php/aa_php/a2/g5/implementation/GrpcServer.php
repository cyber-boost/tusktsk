<?php

namespace TuskLang\Communication\Grpc;

use Grpc\Server;
use Grpc\ServerCredentials;
use Spiral\GRPC\Server as SpiralServer;
use Spiral\GRPC\ServiceInterface;
use Google\Protobuf\Internal\Message;

/**
 * Advanced gRPC Server
 * 
 * Features:
 * - High-performance RPC server
 * - Protocol Buffers support
 * - Streaming (unary, server, client, bidirectional)
 * - Authentication and authorization
 * - Health checking and service discovery
 * - Load balancing and circuit breaker
 * - Metrics and monitoring
 */
class GrpcServer
{
    private array $config;
    private array $services = [];
    private array $interceptors = [];
    private ?Server $server = null;
    private ProtobufSerializer $serializer;
    private ServiceRegistry $serviceRegistry;

    public function __construct(array $config = [])
    {
        $this->config = array_merge([
            'host' => '0.0.0.0',
            'port' => 9000,
            'max_receive_message_length' => 4 * 1024 * 1024,
            'max_send_message_length' => 4 * 1024 * 1024,
            'enable_health_check' => true,
            'enable_reflection' => false,
            'ssl_enabled' => false,
            'ssl_cert_chain' => null,
            'ssl_private_key' => null,
            'ssl_root_certs' => null,
            'keepalive_time' => 7200000,
            'keepalive_timeout' => 20000,
            'keepalive_permit_without_calls' => true,
            'max_concurrent_streams' => 1000,
            'initial_window_size' => 65536,
            'max_frame_size' => 16384
        ], $config);

        $this->serializer = new ProtobufSerializer();
        $this->serviceRegistry = new ServiceRegistry();
    }

    /**
     * Register service implementation
     */
    public function addService(string $serviceName, ServiceInterface $implementation): self
    {
        $this->services[$serviceName] = $implementation;
        $this->serviceRegistry->register($serviceName, $implementation);
        return $this;
    }

    /**
     * Add interceptor to the pipeline
     */
    public function addInterceptor(InterceptorInterface $interceptor): self
    {
        $this->interceptors[] = $interceptor;
        return $this;
    }

    /**
     * Start gRPC server
     */
    public function start(): void
    {
        $this->server = new Server([
            'grpc.max_receive_message_length' => $this->config['max_receive_message_length'],
            'grpc.max_send_message_length' => $this->config['max_send_message_length'],
            'grpc.keepalive_time_ms' => $this->config['keepalive_time'],
            'grpc.keepalive_timeout_ms' => $this->config['keepalive_timeout'],
            'grpc.keepalive_permit_without_calls' => $this->config['keepalive_permit_without_calls'],
            'grpc.http2.max_pings_without_data' => 0,
            'grpc.http2.min_time_between_pings_ms' => 10000,
        ]);

        // Register all services
        foreach ($this->services as $serviceName => $implementation) {
            $this->registerServiceMethods($serviceName, $implementation);
        }

        // Add health check service if enabled
        if ($this->config['enable_health_check']) {
            $this->addHealthCheckService();
        }

        // Add reflection service if enabled
        if ($this->config['enable_reflection']) {
            $this->addReflectionService();
        }

        $address = $this->config['host'] . ':' . $this->config['port'];
        
        if ($this->config['ssl_enabled']) {
            $credentials = ServerCredentials::createSsl(
                $this->config['ssl_root_certs'],
                [
                    'private_key' => file_get_contents($this->config['ssl_private_key']),
                    'cert_chain' => file_get_contents($this->config['ssl_cert_chain'])
                ]
            );
        } else {
            $credentials = ServerCredentials::createInsecure();
        }

        $this->server->addHttp2Port($address, $credentials);

        echo "gRPC server started on {$address}\n";
        $this->server->start();
    }

    /**
     * Stop server gracefully
     */
    public function stop(): void
    {
        if ($this->server) {
            $this->server->shutdown();
        }
    }

    /**
     * Register service methods
     */
    private function registerServiceMethods(string $serviceName, ServiceInterface $implementation): void
    {
        $reflection = new \ReflectionClass($implementation);
        $methods = $reflection->getMethods(\ReflectionMethod::IS_PUBLIC);

        foreach ($methods as $method) {
            if ($method->getDeclaringClass()->getName() === get_class($implementation)) {
                $this->registerMethod($serviceName, $method->getName(), $implementation);
            }
        }
    }

    /**
     * Register individual method
     */
    private function registerMethod(string $serviceName, string $methodName, ServiceInterface $implementation): void
    {
        $fullMethodName = "/{$serviceName}/{$methodName}";
        
        $this->server->addHttp2Port($fullMethodName, function($call) use ($implementation, $methodName) {
            return $this->handleCall($call, $implementation, $methodName);
        });
    }

    /**
     * Handle gRPC call
     */
    private function handleCall($call, ServiceInterface $implementation, string $methodName)
    {
        try {
            // Apply interceptors
            $context = new GrpcContext($call);
            $interceptedCall = $this->applyInterceptors($call, $context);

            // Get request message
            $request = $this->deserializeRequest($interceptedCall);
            
            // Invoke service method
            $response = $implementation->{$methodName}($request, $context);
            
            // Serialize and return response
            return $this->serializeResponse($response);
            
        } catch (\Exception $e) {
            return $this->handleError($e, $call);
        }
    }

    /**
     * Apply interceptor chain
     */
    private function applyInterceptors($call, GrpcContext $context)
    {
        $processedCall = $call;
        
        foreach ($this->interceptors as $interceptor) {
            $processedCall = $interceptor->intercept($processedCall, $context);
        }
        
        return $processedCall;
    }

    /**
     * Deserialize request from Protocol Buffers
     */
    private function deserializeRequest($call): Message
    {
        $data = $call->getBody();
        return $this->serializer->deserialize($data, $call->getRequestType());
    }

    /**
     * Serialize response to Protocol Buffers
     */
    private function serializeResponse(Message $response): string
    {
        return $this->serializer->serialize($response);
    }

    /**
     * Handle errors and convert to gRPC status
     */
    private function handleError(\Exception $e, $call): \Grpc\Status
    {
        $code = $this->mapExceptionToGrpcCode($e);
        $message = $e->getMessage();
        
        // Log error
        error_log("gRPC Error [{$code}]: {$message}");
        
        return new \Grpc\Status($code, $message);
    }

    /**
     * Map PHP exceptions to gRPC status codes
     */
    private function mapExceptionToGrpcCode(\Exception $e): int
    {
        switch (true) {
            case $e instanceof \InvalidArgumentException:
                return \Grpc\STATUS_INVALID_ARGUMENT;
                
            case $e instanceof \RuntimeException:
                return \Grpc\STATUS_INTERNAL;
                
            case $e instanceof \BadMethodCallException:
                return \Grpc\STATUS_UNIMPLEMENTED;
                
            case $e instanceof AuthenticationException:
                return \Grpc\STATUS_UNAUTHENTICATED;
                
            case $e instanceof AuthorizationException:
                return \Grpc\STATUS_PERMISSION_DENIED;
                
            case $e instanceof NotFoundException:
                return \Grpc\STATUS_NOT_FOUND;
                
            case $e instanceof TimeoutException:
                return \Grpc\STATUS_DEADLINE_EXCEEDED;
                
            default:
                return \Grpc\STATUS_UNKNOWN;
        }
    }

    /**
     * Add health check service
     */
    private function addHealthCheckService(): void
    {
        $healthService = new HealthCheckService($this->serviceRegistry);
        $this->addService('grpc.health.v1.Health', $healthService);
    }

    /**
     * Add reflection service for development
     */
    private function addReflectionService(): void
    {
        $reflectionService = new ReflectionService($this->serviceRegistry);
        $this->addService('grpc.reflection.v1alpha.ServerReflection', $reflectionService);
    }

    /**
     * Get server statistics
     */
    public function getStats(): array
    {
        return [
            'services_registered' => count($this->services),
            'interceptors_count' => count($this->interceptors),
            'configuration' => $this->config,
            'memory_usage' => memory_get_usage(true),
            'uptime' => time() - $_SERVER['REQUEST_TIME']
        ];
    }
}

/**
 * gRPC Context for request handling
 */
class GrpcContext
{
    private $call;
    private array $metadata = [];
    private array $headers = [];

    public function __construct($call)
    {
        $this->call = $call;
        $this->extractMetadata();
    }

    /**
     * Get call metadata
     */
    public function getMetadata(string $key = null)
    {
        if ($key) {
            return $this->metadata[$key] ?? null;
        }
        return $this->metadata;
    }

    /**
     * Set response headers
     */
    public function setHeader(string $key, string $value): self
    {
        $this->headers[$key] = $value;
        return $this;
    }

    /**
     * Get peer information
     */
    public function getPeer(): string
    {
        return $this->call->getPeer();
    }

    /**
     * Check if call is cancelled
     */
    public function isCancelled(): bool
    {
        return $this->call->isCancelled();
    }

    /**
     * Extract metadata from call
     */
    private function extractMetadata(): void
    {
        $metadata = $this->call->getMetadata();
        
        foreach ($metadata as $key => $values) {
            $this->metadata[$key] = is_array($values) ? $values[0] : $values;
        }
    }
}

/**
 * Protocol Buffers Serializer
 */
class ProtobufSerializer
{
    /**
     * Serialize message to binary
     */
    public function serialize(Message $message): string
    {
        return $message->serializeToString();
    }

    /**
     * Deserialize binary data to message
     */
    public function deserialize(string $data, string $messageType): Message
    {
        $message = new $messageType();
        $message->mergeFromString($data);
        return $message;
    }
}

/**
 * Service Registry for managing registered services
 */
class ServiceRegistry
{
    private array $services = [];

    /**
     * Register service
     */
    public function register(string $name, ServiceInterface $implementation): void
    {
        $this->services[$name] = [
            'implementation' => $implementation,
            'methods' => $this->extractMethods($implementation),
            'registered_at' => time()
        ];
    }

    /**
     * Get all registered services
     */
    public function getServices(): array
    {
        return array_keys($this->services);
    }

    /**
     * Get service information
     */
    public function getServiceInfo(string $name): ?array
    {
        return $this->services[$name] ?? null;
    }

    /**
     * Extract methods from service implementation
     */
    private function extractMethods(ServiceInterface $implementation): array
    {
        $reflection = new \ReflectionClass($implementation);
        $methods = $reflection->getMethods(\ReflectionMethod::IS_PUBLIC);
        
        $serviceMethods = [];
        foreach ($methods as $method) {
            if ($method->getDeclaringClass()->getName() === get_class($implementation)) {
                $serviceMethods[] = $method->getName();
            }
        }
        
        return $serviceMethods;
    }
}

/**
 * Interceptor interface
 */
interface InterceptorInterface
{
    public function intercept($call, GrpcContext $context);
}

/**
 * Custom exceptions
 */
class AuthenticationException extends \Exception {}
class AuthorizationException extends \Exception {}
class NotFoundException extends \Exception {}
class TimeoutException extends \Exception {} 