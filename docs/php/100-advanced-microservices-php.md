# 🔗 Advanced Microservices with TuskLang & PHP

## Introduction
Microservices architecture enables scalable, maintainable applications. TuskLang and PHP let you implement sophisticated microservices with config-driven service discovery, inter-service communication, load balancing, and distributed tracing that creates resilient, scalable systems.

## Key Features
- **Service discovery and registration**
- **Inter-service communication**
- **Load balancing and health checks**
- **Service mesh integration**
- **Distributed tracing**
- **Circuit breaker patterns**

## Example: Microservices Configuration
```ini
[microservices]
discovery: @go("microservices.ConfigureDiscovery")
communication: @go("microservices.ConfigureCommunication")
load_balancing: @go("microservices.ConfigureLoadBalancing")
mesh: @go("microservices.ConfigureServiceMesh")
tracing: @go("microservices.ConfigureTracing")
```

## PHP: Service Discovery Implementation
```php
<?php

namespace App\Microservices;

use TuskLang\Config;
use TuskLang\Operators\Env;
use TuskLang\Operators\Metrics;
use TuskLang\Operators\Go;

class ServiceDiscovery
{
    private $config;
    private $registry;
    private $healthChecker;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->registry = $this->getRegistry();
        $this->healthChecker = new HealthChecker();
    }
    
    public function register($service)
    {
        $serviceData = [
            'id' => $service['id'],
            'name' => $service['name'],
            'host' => $service['host'],
            'port' => $service['port'],
            'version' => $service['version'],
            'endpoints' => $service['endpoints'],
            'metadata' => $service['metadata'] ?? [],
            'registered_at' => date('c')
        ];
        
        $this->registry->register($serviceData);
        
        // Start health check
        $this->healthChecker->startHealthCheck($serviceData);
        
        Metrics::record("service_registered", 1, [
            "service" => $service['name'],
            "version" => $service['version']
        ]);
        
        return $serviceData;
    }
    
    public function discover($serviceName, $version = null)
    {
        $services = $this->registry->discover($serviceName, $version);
        
        if (empty($services)) {
            throw new ServiceNotFoundException("Service not found: {$serviceName}");
        }
        
        // Filter healthy services
        $healthyServices = array_filter($services, function($service) {
            return $service['status'] === 'healthy';
        });
        
        if (empty($healthyServices)) {
            throw new ServiceUnavailableException("No healthy instances available for service: {$serviceName}");
        }
        
        // Load balance
        $selectedService = $this->loadBalance($healthyServices);
        
        Metrics::record("service_discovered", 1, [
            "service" => $serviceName,
            "version" => $selectedService['version']
        ]);
        
        return $selectedService;
    }
    
    public function deregister($serviceId)
    {
        $this->registry->deregister($serviceId);
        $this->healthChecker->stopHealthCheck($serviceId);
        
        Metrics::record("service_deregistered", 1, [
            "service_id" => $serviceId
        ]);
    }
    
    public function getServiceHealth($serviceId)
    {
        return $this->healthChecker->getHealth($serviceId);
    }
    
    private function loadBalance($services)
    {
        $strategy = $this->config->get('microservices.load_balancing.strategy', 'round_robin');
        
        switch ($strategy) {
            case 'round_robin':
                return $this->roundRobin($services);
            case 'least_connections':
                return $this->leastConnections($services);
            case 'weighted':
                return $this->weighted($services);
            case 'random':
                return $this->random($services);
            default:
                return $services[0];
        }
    }
    
    private function roundRobin($services)
    {
        static $index = 0;
        $service = $services[$index % count($services)];
        $index++;
        return $service;
    }
    
    private function leastConnections($services)
    {
        $minConnections = PHP_INT_MAX;
        $selectedService = null;
        
        foreach ($services as $service) {
            $connections = $service['metadata']['connections'] ?? 0;
            if ($connections < $minConnections) {
                $minConnections = $connections;
                $selectedService = $service;
            }
        }
        
        return $selectedService;
    }
    
    private function weighted($services)
    {
        $totalWeight = 0;
        $weights = [];
        
        foreach ($services as $service) {
            $weight = $service['metadata']['weight'] ?? 1;
            $totalWeight += $weight;
            $weights[] = $weight;
        }
        
        $random = mt_rand(1, $totalWeight);
        $currentWeight = 0;
        
        foreach ($services as $index => $service) {
            $currentWeight += $weights[$index];
            if ($random <= $currentWeight) {
                return $service;
            }
        }
        
        return $services[0];
    }
    
    private function random($services)
    {
        return $services[array_rand($services)];
    }
    
    private function getRegistry()
    {
        $type = $this->config->get('microservices.discovery.registry', 'consul');
        
        switch ($type) {
            case 'consul':
                return new ConsulRegistry($this->config);
            case 'etcd':
                return new EtcdRegistry($this->config);
            case 'zookeeper':
                return new ZookeeperRegistry($this->config);
            default:
                throw new \Exception("Unknown registry type: {$type}");
        }
    }
}

class ConsulRegistry
{
    private $config;
    private $client;
    
    public function __construct($config)
    {
        $this->config = $config;
        $this->client = new ConsulClient($config);
    }
    
    public function register($service)
    {
        $consulService = [
            'ID' => $service['id'],
            'Name' => $service['name'],
            'Address' => $service['host'],
            'Port' => $service['port'],
            'Tags' => [
                "version={$service['version']}",
                "php",
                "microservice"
            ],
            'Meta' => $service['metadata'],
            'Check' => [
                'HTTP' => "http://{$service['host']}:{$service['port']}/health",
                'Interval' => '10s',
                'Timeout' => '5s'
            ]
        ];
        
        $this->client->put("/v1/agent/service/register", $consulService);
    }
    
    public function discover($serviceName, $version = null)
    {
        $url = "/v1/health/service/{$serviceName}";
        $params = ['passing' => 'true'];
        
        if ($version) {
            $params['tag'] = "version={$version}";
        }
        
        $response = $this->client->get($url, $params);
        $services = json_decode($response, true);
        
        return array_map(function($service) {
            return [
                'id' => $service['Service']['ID'],
                'name' => $service['Service']['Service'],
                'host' => $service['Service']['Address'],
                'port' => $service['Service']['Port'],
                'version' => $this->extractVersion($service['Service']['Tags']),
                'metadata' => $service['Service']['Meta'],
                'status' => 'healthy'
            ];
        }, $services);
    }
    
    public function deregister($serviceId)
    {
        $this->client->put("/v1/agent/service/deregister/{$serviceId}");
    }
    
    private function extractVersion($tags)
    {
        foreach ($tags as $tag) {
            if (strpos($tag, 'version=') === 0) {
                return substr($tag, 8);
            }
        }
        return 'latest';
    }
}

class HealthChecker
{
    private $config;
    private $checks = [];
    
    public function __construct()
    {
        $this->config = new Config();
    }
    
    public function startHealthCheck($service)
    {
        $checkId = $service['id'];
        $interval = $this->config->get('microservices.health_check.interval', 30);
        
        $this->checks[$checkId] = [
            'service' => $service,
            'interval' => $interval,
            'last_check' => 0,
            'status' => 'unknown'
        ];
    }
    
    public function stopHealthCheck($serviceId)
    {
        unset($this->checks[$serviceId]);
    }
    
    public function getHealth($serviceId)
    {
        if (!isset($this->checks[$serviceId])) {
            return null;
        }
        
        $check = $this->checks[$serviceId];
        
        // Perform health check if needed
        if (time() - $check['last_check'] >= $check['interval']) {
            $check['status'] = $this->performHealthCheck($check['service']);
            $check['last_check'] = time();
            $this->checks[$serviceId] = $check;
        }
        
        return $check['status'];
    }
    
    private function performHealthCheck($service)
    {
        $healthEndpoint = $service['endpoints']['health'] ?? '/health';
        $url = "http://{$service['host']}:{$service['port']}{$healthEndpoint}";
        
        try {
            $response = $this->makeRequest($url);
            return $response['status'] === 200 ? 'healthy' : 'unhealthy';
        } catch (\Exception $e) {
            return 'unhealthy';
        }
    }
    
    private function makeRequest($url)
    {
        $ch = curl_init();
        curl_setopt($ch, CURLOPT_URL, $url);
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_TIMEOUT, 5);
        
        $response = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        
        curl_close($ch);
        
        return [
            'status' => $httpCode,
            'body' => $response
        ];
    }
}
```

## Inter-Service Communication
```php
<?php

namespace App\Microservices\Communication;

use TuskLang\Config;

class ServiceClient
{
    private $config;
    private $discovery;
    private $circuitBreaker;
    private $tracer;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->discovery = new ServiceDiscovery();
        $this->circuitBreaker = new CircuitBreaker();
        $this->tracer = new DistributedTracer();
    }
    
    public function call($serviceName, $method, $data = [], $options = [])
    {
        $startTime = microtime(true);
        
        try {
            // Discover service
            $service = $this->discovery->discover($serviceName);
            
            // Check circuit breaker
            if ($this->circuitBreaker->isOpen($serviceName)) {
                throw new CircuitBreakerOpenException("Circuit breaker is open for service: {$serviceName}");
            }
            
            // Start trace
            $traceId = $this->tracer->startSpan("service_call", [
                'service' => $serviceName,
                'method' => $method
            ]);
            
            // Make request
            $response = $this->makeRequest($service, $method, $data, $options);
            
            // End trace
            $this->tracer->endSpan($traceId);
            
            // Record success
            $this->circuitBreaker->recordSuccess($serviceName);
            
            $duration = (microtime(true) - $startTime) * 1000;
            
            // Record metrics
            Metrics::record("service_call_duration", $duration, [
                "service" => $serviceName,
                "method" => $method,
                "status" => "success"
            ]);
            
            return $response;
            
        } catch (\Exception $e) {
            // Record failure
            $this->circuitBreaker->recordFailure($serviceName);
            
            $duration = (microtime(true) - $startTime) * 1000;
            
            Metrics::record("service_call_duration", $duration, [
                "service" => $serviceName,
                "method" => $method,
                "status" => "failure",
                "error" => get_class($e)
            ]);
            
            throw $e;
        }
    }
    
    public function callAsync($serviceName, $method, $data = [], $options = [])
    {
        return new Promise(function($resolve, $reject) use ($serviceName, $method, $data, $options) {
            try {
                $result = $this->call($serviceName, $method, $data, $options);
                $resolve($result);
            } catch (\Exception $e) {
                $reject($e);
            }
        });
    }
    
    private function makeRequest($service, $method, $data, $options)
    {
        $protocol = $options['protocol'] ?? 'http';
        $timeout = $options['timeout'] ?? 30;
        
        switch ($protocol) {
            case 'http':
                return $this->makeHttpRequest($service, $method, $data, $timeout);
            case 'grpc':
                return $this->makeGrpcRequest($service, $method, $data, $timeout);
            case 'message_queue':
                return $this->makeMessageQueueRequest($service, $method, $data);
            default:
                throw new \Exception("Unsupported protocol: {$protocol}");
        }
    }
    
    private function makeHttpRequest($service, $method, $data, $timeout)
    {
        $url = "http://{$service['host']}:{$service['port']}/api/{$method}";
        
        $ch = curl_init();
        curl_setopt($ch, CURLOPT_URL, $url);
        curl_setopt($ch, CURLOPT_POST, true);
        curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($data));
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_TIMEOUT, $timeout);
        curl_setopt($ch, CURLOPT_HTTPHEADER, [
            'Content-Type: application/json',
            'Accept: application/json'
        ]);
        
        $response = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        
        curl_close($ch);
        
        if ($httpCode >= 400) {
            throw new ServiceException("Service call failed: HTTP {$httpCode}", $httpCode);
        }
        
        return json_decode($response, true);
    }
    
    private function makeGrpcRequest($service, $method, $data, $timeout)
    {
        // Implementation for gRPC requests
        // This would use the gRPC PHP extension
        throw new \Exception("gRPC not implemented");
    }
    
    private function makeMessageQueueRequest($service, $method, $data)
    {
        $queue = $this->config->get('microservices.message_queue');
        $client = new MessageQueueClient($queue);
        
        $message = [
            'service' => $service['name'],
            'method' => $method,
            'data' => $data,
            'timestamp' => time(),
            'message_id' => uniqid()
        ];
        
        return $client->publish($message);
    }
}

class MessageQueueClient
{
    private $config;
    private $connection;
    
    public function __construct($config)
    {
        $this->config = $config;
        $this->connection = $this->createConnection();
    }
    
    public function publish($message)
    {
        $queue = $this->config['queue_name'];
        
        return $this->connection->publish($queue, json_encode($message));
    }
    
    public function subscribe($queue, callable $callback)
    {
        $this->connection->subscribe($queue, function($message) use ($callback) {
            $data = json_decode($message, true);
            $callback($data);
        });
    }
    
    private function createConnection()
    {
        $type = $this->config['type'];
        
        switch ($type) {
            case 'redis':
                return new RedisConnection($this->config);
            case 'rabbitmq':
                return new RabbitMQConnection($this->config);
            case 'kafka':
                return new KafkaConnection($this->config);
            default:
                throw new \Exception("Unknown message queue type: {$type}");
        }
    }
}
```

## Service Mesh Integration
```php
<?php

namespace App\Microservices\Mesh;

use TuskLang\Config;

class ServiceMesh
{
    private $config;
    private $proxy;
    private $policies;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->proxy = new SidecarProxy();
        $this->policies = new PolicyManager();
    }
    
    public function handleInbound($request)
    {
        // Apply inbound policies
        $request = $this->policies->applyInbound($request);
        
        // Route to service
        $response = $this->proxy->forward($request);
        
        // Apply outbound policies
        $response = $this->policies->applyOutbound($response);
        
        return $response;
    }
    
    public function handleOutbound($request, $target)
    {
        // Apply outbound policies
        $request = $this->policies->applyOutbound($request);
        
        // Route through proxy
        $response = $this->proxy->route($request, $target);
        
        // Apply inbound policies
        $response = $this->policies->applyInbound($response);
        
        return $response;
    }
    
    public function addPolicy($policy)
    {
        $this->policies->add($policy);
    }
    
    public function getMetrics()
    {
        return $this->proxy->getMetrics();
    }
}

class SidecarProxy
{
    private $config;
    private $routes = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->loadRoutes();
    }
    
    public function forward($request)
    {
        $route = $this->findRoute($request);
        
        if (!$route) {
            throw new RouteNotFoundException("No route found for request");
        }
        
        return $this->executeRoute($route, $request);
    }
    
    public function route($request, $target)
    {
        $route = [
            'target' => $target,
            'timeout' => $this->config->get('microservices.mesh.timeout', 30),
            'retries' => $this->config->get('microservices.mesh.retries', 3)
        ];
        
        return $this->executeRoute($route, $request);
    }
    
    private function findRoute($request)
    {
        $path = $request->getPathInfo();
        $method = $request->getMethod();
        
        foreach ($this->routes as $route) {
            if ($this->matchesRoute($route, $method, $path)) {
                return $route;
            }
        }
        
        return null;
    }
    
    private function matchesRoute($route, $method, $path)
    {
        if ($route['method'] !== $method) {
            return false;
        }
        
        $pattern = $route['pattern'];
        return preg_match($pattern, $path);
    }
    
    private function executeRoute($route, $request)
    {
        $target = $route['target'];
        $timeout = $route['timeout'] ?? 30;
        $retries = $route['retries'] ?? 0;
        
        for ($attempt = 0; $attempt <= $retries; $attempt++) {
            try {
                return $this->makeRequest($target, $request, $timeout);
            } catch (\Exception $e) {
                if ($attempt === $retries) {
                    throw $e;
                }
                
                // Wait before retry
                usleep(100000); // 100ms
            }
        }
    }
    
    private function makeRequest($target, $request, $timeout)
    {
        $url = "http://{$target['host']}:{$target['port']}{$request->getPathInfo()}";
        
        $ch = curl_init();
        curl_setopt($ch, CURLOPT_URL, $url);
        curl_setopt($ch, CURLOPT_CUSTOMREQUEST, $request->getMethod());
        curl_setopt($ch, CURLOPT_POSTFIELDS, $request->getContent());
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_TIMEOUT, $timeout);
        curl_setopt($ch, CURLOPT_HTTPHEADER, $this->getHeaders($request));
        
        $response = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        
        curl_close($ch);
        
        return new Response($response, $httpCode);
    }
    
    private function getHeaders($request)
    {
        $headers = [];
        
        foreach ($request->headers->all() as $name => $values) {
            $headers[] = "{$name}: " . implode(', ', $values);
        }
        
        return $headers;
    }
    
    private function loadRoutes()
    {
        $this->routes = $this->config->get('microservices.mesh.routes', []);
    }
    
    public function getMetrics()
    {
        return [
            'requests_total' => $this->getRequestCount(),
            'requests_duration' => $this->getAverageDuration(),
            'errors_total' => $this->getErrorCount()
        ];
    }
    
    private function getRequestCount()
    {
        // Implementation to get request count
        return 0;
    }
    
    private function getAverageDuration()
    {
        // Implementation to get average duration
        return 0;
    }
    
    private function getErrorCount()
    {
        // Implementation to get error count
        return 0;
    }
}

class PolicyManager
{
    private $policies = [];
    
    public function add($policy)
    {
        $this->policies[] = $policy;
    }
    
    public function applyInbound($request)
    {
        foreach ($this->policies as $policy) {
            if ($policy->appliesTo('inbound')) {
                $request = $policy->apply($request);
            }
        }
        
        return $request;
    }
    
    public function applyOutbound($response)
    {
        foreach ($this->policies as $policy) {
            if ($policy->appliesTo('outbound')) {
                $response = $policy->apply($response);
            }
        }
        
        return $response;
    }
}

class AuthenticationPolicy
{
    private $config;
    
    public function __construct($config)
    {
        $this->config = $config;
    }
    
    public function appliesTo($direction)
    {
        return $direction === 'inbound';
    }
    
    public function apply($request)
    {
        $token = $request->headers->get('Authorization');
        
        if (!$token) {
            throw new AuthenticationException('No authorization token provided');
        }
        
        // Validate token
        $this->validateToken($token);
        
        return $request;
    }
    
    private function validateToken($token)
    {
        // Implementation to validate JWT token
        // This would verify the token signature and claims
    }
}

class RateLimitPolicy
{
    private $config;
    private $limiter;
    
    public function __construct($config)
    {
        $this->config = $config;
        $this->limiter = new RateLimiter();
    }
    
    public function appliesTo($direction)
    {
        return $direction === 'inbound';
    }
    
    public function apply($request)
    {
        $this->limiter->checkLimit($request);
        return $request;
    }
}
```

## Best Practices
- **Design services around business capabilities**
- **Use service discovery for dynamic scaling**
- **Implement circuit breakers for resilience**
- **Use distributed tracing for observability**
- **Implement proper error handling**
- **Use message queues for async communication**

## Performance Optimization
- **Use connection pooling**
- **Implement caching strategies**
- **Use load balancing**
- **Optimize network communication**

## Security Considerations
- **Implement service-to-service authentication**
- **Use encrypted communication**
- **Validate all inputs**
- **Implement proper access controls**

## Troubleshooting
- **Monitor service health**
- **Check circuit breaker status**
- **Verify service discovery**
- **Monitor distributed traces**

## Conclusion
TuskLang + PHP = microservices that are scalable, resilient, and observable. Build systems that grow with your business. 