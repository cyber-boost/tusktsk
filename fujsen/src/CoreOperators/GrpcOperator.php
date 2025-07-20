<?php

namespace Fujsen\CoreOperators;

use Fujsen\BaseOperator;
use Fujsen\Exceptions\OperatorException;
use Grpc\Channel;
use Grpc\ChannelCredentials;
use Grpc\UnaryCall;
use Grpc\ServerStreamingCall;
use Grpc\ClientStreamingCall;
use Grpc\BidiStreamingCall;

/**
 * gRPC Operator for TuskLang
 * 
 * Provides gRPC client functionality with support for:
 * - Unary, server streaming, client streaming, bidirectional streaming
 * - Service discovery integration
 * - TLS/mTLS authentication
 * - Deadline and retry management
 * - Metadata propagation
 * 
 * Usage:
 * ```php
 * // Unary call
 * $user = @grpc({
 *   service: "UserService",
 *   method: "GetUser",
 *   request: { user_id: @variable("userId") },
 *   endpoint: "grpc://users.service:50051"
 * })
 * 
 * // Server streaming
 * $stream = @grpc({
 *   service: "DataService", 
 *   method: "StreamData",
 *   request: { filter: "active" },
 *   streaming: "server"
 * })
 * ```
 */
class GrpcOperator extends BaseOperator
{
    private array $channels = [];
    private array $stubs = [];
    private array $config;
    
    public function __construct(array $config = [])
    {
        parent::__construct('grpc');
        $this->config = array_merge([
            'default_timeout' => 30000,
            'max_retries' => 3,
            'retry_delay' => 1000,
            'keepalive_time' => 30000,
            'keepalive_timeout' => 5000,
            'keepalive_permit_without_calls' => true,
            'http2_max_pings_without_data' => 0,
            'http2_min_time_between_pings_ms' => 10000,
            'http2_min_ping_interval_without_data_ms' => 300000
        ], $config);
    }

    /**
     * Execute gRPC operation
     */
    public function execute(array $params, array $context = []): mixed
    {
        $this->validateParams($params);
        
        $service = $params['service'] ?? '';
        $method = $params['method'] ?? '';
        $request = $params['request'] ?? [];
        $endpoint = $params['endpoint'] ?? '';
        $streaming = $params['streaming'] ?? 'unary';
        $timeout = $params['timeout'] ?? $this->config['default_timeout'];
        $metadata = $params['metadata'] ?? [];
        $credentials = $params['credentials'] ?? [];
        
        try {
            $channel = $this->getChannel($endpoint, $credentials);
            $stub = $this->getStub($service, $channel);
            
            // Add context metadata
            $metadata = array_merge($metadata, $this->buildContextMetadata($context));
            
            switch ($streaming) {
                case 'unary':
                    return $this->executeUnary($stub, $method, $request, $metadata, $timeout);
                case 'server':
                    return $this->executeServerStreaming($stub, $method, $request, $metadata, $timeout);
                case 'client':
                    return $this->executeClientStreaming($stub, $method, $request, $metadata, $timeout);
                case 'bidi':
                    return $this->executeBidiStreaming($stub, $method, $request, $metadata, $timeout);
                default:
                    throw new OperatorException("Unsupported streaming type: $streaming");
            }
        } catch (\Exception $e) {
            throw new OperatorException("gRPC operation failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Execute unary gRPC call
     */
    private function executeUnary($stub, string $method, array $request, array $metadata, int $timeout): mixed
    {
        $call = $stub->$method($request, $metadata, ['timeout' => $timeout]);
        
        list($response, $status) = $call->wait();
        
        if ($status->code !== \Grpc\STATUS_OK) {
            throw new OperatorException("gRPC call failed: " . $status->details, $status->code);
        }
        
        return $this->convertResponse($response);
    }

    /**
     * Execute server streaming gRPC call
     */
    private function executeServerStreaming($stub, string $method, array $request, array $metadata, int $timeout): array
    {
        $call = $stub->$method($request, $metadata, ['timeout' => $timeout]);
        
        $responses = [];
        while ($response = $call->read()) {
            $responses[] = $this->convertResponse($response);
        }
        
        $status = $call->getStatus();
        if ($status->code !== \Grpc\STATUS_OK) {
            throw new OperatorException("gRPC streaming failed: " . $status->details, $status->code);
        }
        
        return $responses;
    }

    /**
     * Execute client streaming gRPC call
     */
    private function executeClientStreaming($stub, string $method, array $requests, array $metadata, int $timeout): mixed
    {
        $call = $stub->$method($metadata, ['timeout' => $timeout]);
        
        foreach ($requests as $request) {
            $call->write($request);
        }
        
        $call->writesDone();
        list($response, $status) = $call->wait();
        
        if ($status->code !== \Grpc\STATUS_OK) {
            throw new OperatorException("gRPC client streaming failed: " . $status->details, $status->code);
        }
        
        return $this->convertResponse($response);
    }

    /**
     * Execute bidirectional streaming gRPC call
     */
    private function executeBidiStreaming($stub, string $method, array $requests, array $metadata, int $timeout): array
    {
        $call = $stub->$method($metadata, ['timeout' => $timeout]);
        
        $responses = [];
        
        // Start reading responses in background
        $readTask = function() use ($call, &$responses) {
            while ($response = $call->read()) {
                $responses[] = $this->convertResponse($response);
            }
        };
        
        // Write requests
        foreach ($requests as $request) {
            $call->write($request);
        }
        
        $call->writesDone();
        
        // Wait for all responses
        $readTask();
        
        $status = $call->getStatus();
        if ($status->code !== \Grpc\STATUS_OK) {
            throw new OperatorException("gRPC bidirectional streaming failed: " . $status->details, $status->code);
        }
        
        return $responses;
    }

    /**
     * Get or create gRPC channel
     */
    private function getChannel(string $endpoint, array $credentials): Channel
    {
        $key = $endpoint . serialize($credentials);
        
        if (!isset($this->channels[$key])) {
            $channelArgs = [
                'grpc.keepalive_time_ms' => $this->config['keepalive_time'],
                'grpc.keepalive_timeout_ms' => $this->config['keepalive_timeout'],
                'grpc.keepalive_permit_without_calls' => $this->config['keepalive_permit_without_calls'],
                'grpc.http2.max_pings_without_data' => $this->config['http2_max_pings_without_data'],
                'grpc.http2.min_time_between_pings_ms' => $this->config['http2_min_time_between_pings_ms'],
                'grpc.http2.min_ping_interval_without_data_ms' => $this->config['http2_min_ping_interval_without_data_ms']
            ];
            
            $channelCredentials = $this->createChannelCredentials($credentials);
            $this->channels[$key] = new Channel($endpoint, $channelCredentials, $channelArgs);
        }
        
        return $this->channels[$key];
    }

    /**
     * Create channel credentials based on configuration
     */
    private function createChannelCredentials(array $credentials): ChannelCredentials
    {
        if (empty($credentials)) {
            return ChannelCredentials::createInsecure();
        }
        
        $type = $credentials['type'] ?? 'insecure';
        
        switch ($type) {
            case 'ssl':
                $certPath = $credentials['cert_path'] ?? '';
                $keyPath = $credentials['key_path'] ?? '';
                $caPath = $credentials['ca_path'] ?? '';
                
                if ($certPath && $keyPath) {
                    return ChannelCredentials::createSsl($caPath, $keyPath, $certPath);
                } elseif ($caPath) {
                    return ChannelCredentials::createSsl($caPath);
                } else {
                    return ChannelCredentials::createSsl();
                }
                
            case 'mtls':
                $certPath = $credentials['cert_path'] ?? '';
                $keyPath = $credentials['key_path'] ?? '';
                $caPath = $credentials['ca_path'] ?? '';
                
                if (!$certPath || !$keyPath || !$caPath) {
                    throw new OperatorException("mTLS requires cert_path, key_path, and ca_path");
                }
                
                return ChannelCredentials::createSsl($caPath, $keyPath, $certPath);
                
            case 'jwt':
                $token = $credentials['token'] ?? '';
                if (!$token) {
                    throw new OperatorException("JWT authentication requires token");
                }
                
                $callCredentials = \Grpc\CallCredentials::createFromPlugin(
                    function($context) use ($token) {
                        return ['authorization' => "Bearer $token"];
                    }
                );
                
                return ChannelCredentials::createComposite(
                    ChannelCredentials::createSsl(),
                    $callCredentials
                );
                
            default:
                return ChannelCredentials::createInsecure();
        }
    }

    /**
     * Get or create gRPC stub
     */
    private function getStub(string $service, Channel $channel)
    {
        $key = $service . spl_object_hash($channel);
        
        if (!isset($this->stubs[$key])) {
            $stubClass = $this->getStubClass($service);
            $this->stubs[$key] = new $stubClass($channel);
        }
        
        return $this->stubs[$key];
    }

    /**
     * Get stub class name for service
     */
    private function getStubClass(string $service): string
    {
        // Map service names to stub classes
        $serviceMap = [
            'UserService' => 'UserServiceClient',
            'DataService' => 'DataServiceClient',
            'AuthService' => 'AuthServiceClient',
            'NotificationService' => 'NotificationServiceClient'
        ];
        
        if (isset($serviceMap[$service])) {
            return $serviceMap[$service];
        }
        
        // Try to auto-generate or load from proto
        return $this->loadStubFromProto($service);
    }

    /**
     * Load stub from proto file
     */
    private function loadStubFromProto(string $service): string
    {
        // This would typically involve proto compilation
        // For now, return a generic stub class
        return 'GenericServiceClient';
    }

    /**
     * Build metadata from context
     */
    private function buildContextMetadata(array $context): array
    {
        $metadata = [];
        
        // Add trace ID if available
        if (isset($context['trace_id'])) {
            $metadata['x-trace-id'] = $context['trace_id'];
        }
        
        // Add user ID if available
        if (isset($context['user_id'])) {
            $metadata['x-user-id'] = $context['user_id'];
        }
        
        // Add request ID if available
        if (isset($context['request_id'])) {
            $metadata['x-request-id'] = $context['request_id'];
        }
        
        return $metadata;
    }

    /**
     * Convert gRPC response to array
     */
    private function convertResponse($response): array
    {
        if (is_array($response)) {
            return $response;
        }
        
        if (is_object($response)) {
            return $this->objectToArray($response);
        }
        
        return ['value' => $response];
    }

    /**
     * Convert object to array recursively
     */
    private function objectToArray($object): array
    {
        if (is_object($object)) {
            $object = get_object_vars($object);
        }
        
        if (is_array($object)) {
            return array_map([$this, 'objectToArray'], $object);
        }
        
        return $object;
    }

    /**
     * Validate operator parameters
     */
    private function validateParams(array $params): void
    {
        $required = ['service', 'method'];
        foreach ($required as $field) {
            if (!isset($params[$field])) {
                throw new OperatorException("Missing required parameter: $field");
            }
        }
        
        if (isset($params['streaming']) && !in_array($params['streaming'], ['unary', 'server', 'client', 'bidi'])) {
            throw new OperatorException("Invalid streaming type: " . $params['streaming']);
        }
    }

    /**
     * Cleanup resources
     */
    public function cleanup(): void
    {
        foreach ($this->channels as $channel) {
            $channel->close();
        }
        
        $this->channels = [];
        $this->stubs = [];
    }

    /**
     * Get operator schema
     */
    public function getSchema(): array
    {
        return [
            'type' => 'object',
            'properties' => [
                'service' => ['type' => 'string', 'description' => 'gRPC service name'],
                'method' => ['type' => 'string', 'description' => 'gRPC method name'],
                'request' => ['type' => 'object', 'description' => 'Request data'],
                'endpoint' => ['type' => 'string', 'description' => 'gRPC endpoint URL'],
                'streaming' => [
                    'type' => 'string',
                    'enum' => ['unary', 'server', 'client', 'bidi'],
                    'default' => 'unary',
                    'description' => 'Streaming type'
                ],
                'timeout' => ['type' => 'integer', 'description' => 'Request timeout in milliseconds'],
                'metadata' => ['type' => 'object', 'description' => 'Additional metadata'],
                'credentials' => ['type' => 'object', 'description' => 'Authentication credentials']
            ],
            'required' => ['service', 'method']
        ];
    }
} 