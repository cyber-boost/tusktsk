# 🔗 Advanced Microservices with TuskLang & PHP

## Introduction
Microservices are the architecture of choice for scalable, resilient applications. TuskLang and PHP let you build sophisticated microservice systems with config-driven service discovery, communication, and monitoring that handles the complexity of distributed systems.

## Key Features
- **Service discovery and registration**
- **Inter-service communication**
- **Circuit breakers and resilience**
- **Load balancing and routing**
- **Distributed tracing**
- **Service mesh integration**
- **Monitoring and health checks**

## Example: Microservices Configuration
```ini
[microservices]
discovery: consul
consul_uri: @env("CONSUL_URI")
service_name: @env("SERVICE_NAME")
health_check: @go("microservices.HealthCheck")
circuit_breaker: @go("microservices.CircuitBreaker")
metrics: @metrics("service_calls", 0)
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
    private $consul;
    private $services = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->consul = new ConsulClient(Env::get("CONSUL_URI"));
    }
    
    public function register($serviceName, $address, $port, $tags = [])
    {
        $serviceId = "{$serviceName}-" . uniqid();
        
        $registration = [
            'ID' => $serviceId,
            'Name' => $serviceName,
            'Address' => $address,
            'Port' => $port,
            'Tags' => $tags,
            'Check' => [
                'HTTP' => "http://{$address}:{$port}/health",
                'Interval' => '10s',
                'Timeout' => '5s'
            ]
        ];
        
        $this->consul->agent()->service()->register($registration);
        
        // Store service info locally
        $this->services[$serviceName] = [
            'id' => $serviceId,
            'address' => $address,
            'port' => $port,
            'tags' => $tags
        ];
        
        return $serviceId;
    }
    
    public function discover($serviceName)
    {
        // Check local cache first
        if (isset($this->services[$serviceName])) {
            return $this->services[$serviceName];
        }
        
        // Query Consul
        $services = $this->consul->catalog()->service($serviceName);
        
        if (empty($services)) {
            throw new \Exception("Service {$serviceName} not found");
        }
        
        // Load balance between available instances
        $service = $this->loadBalance($services);
        
        // Cache service info
        $this->services[$serviceName] = $service;
        
        return $service;
    }
    
    private function loadBalance($services)
    {
        // Simple round-robin load balancing
        $index = array_rand($services);
        $service = $services[$index];
        
        return [
            'id' => $service['ServiceID'],
            'address' => $service['ServiceAddress'],
            'port' => $service['ServicePort'],
            'tags' => $service['ServiceTags']
        ];
    }
    
    public function deregister($serviceId)
    {
        $this->consul->agent()->service()->deregister($serviceId);
        
        // Remove from local cache
        foreach ($this->services as $name => $service) {
            if ($service['id'] === $serviceId) {
                unset($this->services[$name]);
                break;
            }
        }
    }
}
```

## Inter-Service Communication
```php
<?php

namespace App\Microservices\Communication;

use TuskLang\Config;
use TuskLang\Operators\Cache;
use TuskLang\Operators\Metrics;

class ServiceClient
{
    private $config;
    private $discovery;
    private $circuitBreaker;
    private $httpClient;
    
    public function __construct(ServiceDiscovery $discovery)
    {
        $this->config = new Config();
        $this->discovery = $discovery;
        $this->circuitBreaker = new CircuitBreaker();
        $this->httpClient = new HttpClient();
    }
    
    public function call($serviceName, $method, $path, $data = [], $headers = [])
    {
        // Discover service
        $service = $this->discovery->discover($serviceName);
        
        // Check circuit breaker
        if (!$this->circuitBreaker->isAllowed($serviceName)) {
            throw new CircuitBreakerOpenException("Circuit breaker is open for {$serviceName}");
        }
        
        // Build request
        $url = "http://{$service['address']}:{$service['port']}{$path}";
        $headers['Content-Type'] = 'application/json';
        
        $startTime = microtime(true);
        
        try {
            // Make HTTP request
            $response = $this->httpClient->request($method, $url, [
                'headers' => $headers,
                'json' => $data
            ]);
            
            $duration = (microtime(true) - $startTime) * 1000;
            
            // Record success
            $this->circuitBreaker->recordSuccess($serviceName);
            
            // Record metrics
            Metrics::record("service_call_duration", $duration, [
                "service" => $serviceName,
                "method" => $method,
                "status" => $response->getStatusCode()
            ]);
            
            return $response->getBody()->getContents();
            
        } catch (\Exception $e) {
            // Record failure
            $this->circuitBreaker->recordFailure($serviceName);
            
            // Record error metrics
            Metrics::record("service_call_errors", 1, [
                "service" => $serviceName,
                "method" => $method,
                "error" => get_class($e)
            ]);
            
            throw $e;
        }
    }
    
    public function callAsync($serviceName, $method, $path, $data = [], $headers = [])
    {
        // Return promise for async calls
        return new Promise(function ($resolve, $reject) use ($serviceName, $method, $path, $data, $headers) {
            try {
                $result = $this->call($serviceName, $method, $path, $data, $headers);
                $resolve($result);
            } catch (\Exception $e) {
                $reject($e);
            }
        });
    }
}
```

## Circuit Breaker Implementation
```php
<?php

namespace App\Microservices\Resilience;

use TuskLang\Config;
use TuskLang\Operators\Cache;

class CircuitBreaker
{
    private $config;
    private $redis;
    private $states = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->redis = new Redis();
    }
    
    public function isAllowed($serviceName)
    {
        $state = $this->getState($serviceName);
        
        switch ($state['status']) {
            case 'CLOSED':
                return true;
                
            case 'OPEN':
                // Check if timeout has passed
                if (time() - $state['lastFailure'] > $this->getTimeout($serviceName)) {
                    $this->setState($serviceName, 'HALF_OPEN');
                    return true;
                }
                return false;
                
            case 'HALF_OPEN':
                return true;
                
            default:
                return true;
        }
    }
    
    public function recordSuccess($serviceName)
    {
        $state = $this->getState($serviceName);
        
        if ($state['status'] === 'HALF_OPEN') {
            // Reset to closed state
            $this->setState($serviceName, 'CLOSED');
            $this->resetCounters($serviceName);
        }
    }
    
    public function recordFailure($serviceName)
    {
        $state = $this->getState($serviceName);
        $threshold = $this->getThreshold($serviceName);
        
        $failures = $state['failures'] + 1;
        
        if ($failures >= $threshold) {
            // Open circuit breaker
            $this->setState($serviceName, 'OPEN');
        } else {
            // Increment failure count
            $this->setState($serviceName, $state['status'], $failures);
        }
    }
    
    private function getState($serviceName)
    {
        $key = "circuit_breaker:{$serviceName}";
        $data = $this->redis->get($key);
        
        if (!$data) {
            return [
                'status' => 'CLOSED',
                'failures' => 0,
                'lastFailure' => 0
            ];
        }
        
        return json_decode($data, true);
    }
    
    private function setState($serviceName, $status, $failures = 0)
    {
        $key = "circuit_breaker:{$serviceName}";
        $state = [
            'status' => $status,
            'failures' => $failures,
            'lastFailure' => time()
        ];
        
        $this->redis->setex($key, 3600, json_encode($state));
    }
    
    private function getThreshold($serviceName)
    {
        return $this->config->get("circuit_breaker.threshold.{$serviceName}", 5);
    }
    
    private function getTimeout($serviceName)
    {
        return $this->config->get("circuit_breaker.timeout.{$serviceName}", 60);
    }
}
```

## Load Balancing
```php
<?php

namespace App\Microservices\LoadBalancing;

use TuskLang\Config;

class LoadBalancer
{
    private $config;
    private $strategies = [];
    
    public function __construct()
    {
        $this->config = new Config();
        $this->loadStrategies();
    }
    
    public function select($serviceName, $instances)
    {
        $strategy = $this->getStrategy($serviceName);
        
        switch ($strategy) {
            case 'round_robin':
                return $this->roundRobin($instances);
                
            case 'least_connections':
                return $this->leastConnections($instances);
                
            case 'weighted':
                return $this->weighted($instances);
                
            case 'random':
                return $this->random($instances);
                
            default:
                return $this->roundRobin($instances);
        }
    }
    
    private function roundRobin($instances)
    {
        static $index = 0;
        
        if (empty($instances)) {
            throw new \Exception("No instances available");
        }
        
        $instance = $instances[$index % count($instances)];
        $index++;
        
        return $instance;
    }
    
    private function leastConnections($instances)
    {
        $minConnections = PHP_INT_MAX;
        $selectedInstance = null;
        
        foreach ($instances as $instance) {
            $connections = $this->getConnectionCount($instance);
            
            if ($connections < $minConnections) {
                $minConnections = $connections;
                $selectedInstance = $instance;
            }
        }
        
        return $selectedInstance;
    }
    
    private function weighted($instances)
    {
        $totalWeight = 0;
        $weights = [];
        
        foreach ($instances as $instance) {
            $weight = $instance['weight'] ?? 1;
            $totalWeight += $weight;
            $weights[] = $weight;
        }
        
        $random = mt_rand(1, $totalWeight);
        $currentWeight = 0;
        
        foreach ($instances as $index => $instance) {
            $currentWeight += $weights[$index];
            
            if ($random <= $currentWeight) {
                return $instance;
            }
        }
        
        return $instances[0];
    }
    
    private function random($instances)
    {
        return $instances[array_rand($instances)];
    }
    
    private function getStrategy($serviceName)
    {
        return $this->config->get("load_balancer.strategy.{$serviceName}", "round_robin");
    }
    
    private function getConnectionCount($instance)
    {
        // This would typically query a monitoring system
        return rand(0, 100);
    }
}
```

## Distributed Tracing
```php
<?php

namespace App\Microservices\Tracing;

use TuskLang\Config;
use OpenTelemetry\API\Trace\TracerInterface;
use OpenTelemetry\API\Trace\SpanInterface;

class TracingService
{
    private $config;
    private $tracer;
    
    public function __construct(TracerInterface $tracer)
    {
        $this->config = new Config();
        $this->tracer = $tracer;
    }
    
    public function startSpan($name, $attributes = [])
    {
        $span = $this->tracer->spanBuilder($name)
            ->setAttributes($attributes)
            ->startSpan();
        
        return $span;
    }
    
    public function injectHeaders($span, &$headers)
    {
        $carrier = [];
        $this->tracer->getTextMapPropagator()->inject($carrier, $headers);
        
        foreach ($carrier as $key => $value) {
            $headers[$key] = $value;
        }
    }
    
    public function extractHeaders($headers)
    {
        $context = $this->tracer->getTextMapPropagator()->extract($headers);
        return $context;
    }
    
    public function addEvent($span, $name, $attributes = [])
    {
        $span->addEvent($name, $attributes);
    }
    
    public function setStatus($span, $code, $description = '')
    {
        $span->setStatus($code, $description);
    }
    
    public function recordException($span, $exception)
    {
        $span->recordException($exception);
        $span->setStatus(\OpenTelemetry\API\Trace\StatusCode::STATUS_ERROR, $exception->getMessage());
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
    private $istio;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->istio = new IstioClient();
    }
    
    public function configureRouting($serviceName, $rules)
    {
        $virtualService = [
            'apiVersion' => 'networking.istio.io/v1alpha3',
            'kind' => 'VirtualService',
            'metadata' => [
                'name' => $serviceName
            ],
            'spec' => [
                'hosts' => [$serviceName],
                'http' => $rules
            ]
        ];
        
        return $this->istio->createVirtualService($virtualService);
    }
    
    public function configureCircuitBreaker($serviceName, $settings)
    {
        $destinationRule = [
            'apiVersion' => 'networking.istio.io/v1alpha3',
            'kind' => 'DestinationRule',
            'metadata' => [
                'name' => $serviceName
            ],
            'spec' => [
                'host' => $serviceName,
                'trafficPolicy' => [
                    'connectionPool' => [
                        'http' => [
                            'http1MaxPendingRequests' => $settings['maxPendingRequests'] ?? 1024,
                            'maxRequestsPerConnection' => $settings['maxRequestsPerConnection'] ?? 1
                        ]
                    ],
                    'outlierDetection' => [
                        'consecutiveErrors' => $settings['consecutiveErrors'] ?? 5,
                        'baseEjectionTime' => $settings['baseEjectionTime'] ?? '30s'
                    ]
                ]
            ]
        ];
        
        return $this->istio->createDestinationRule($destinationRule);
    }
    
    public function configureRetry($serviceName, $retries)
    {
        $virtualService = [
            'apiVersion' => 'networking.istio.io/v1alpha3',
            'kind' => 'VirtualService',
            'metadata' => [
                'name' => $serviceName
            ],
            'spec' => [
                'hosts' => [$serviceName],
                'http' => [
                    [
                        'route' => [
                            [
                                'destination' => [
                                    'host' => $serviceName
                                ]
                            ]
                        ],
                        'retries' => [
                            'attempts' => $retries['attempts'] ?? 3,
                            'perTryTimeout' => $retries['timeout'] ?? '2s'
                        ]
                    ]
                ]
            ]
        ];
        
        return $this->istio->updateVirtualService($virtualService);
    }
}
```

## Best Practices
- **Use service discovery for dynamic service locations**
- **Implement circuit breakers for resilience**
- **Use distributed tracing for debugging**
- **Monitor service health and performance**
- **Implement proper error handling**
- **Use async communication when possible**

## Performance Optimization
- **Use connection pooling**
- **Implement caching strategies**
- **Use compression for inter-service communication**
- **Monitor and optimize service calls**

## Security Considerations
- **Use mTLS for inter-service communication**
- **Implement proper authentication**
- **Validate all service inputs**
- **Use secure service discovery**

## Troubleshooting
- **Monitor service health checks**
- **Check circuit breaker states**
- **Verify service discovery**
- **Monitor distributed traces**

## Conclusion
TuskLang + PHP = microservices that are resilient, observable, and scalable. Build distributed systems that handle complexity with grace. 