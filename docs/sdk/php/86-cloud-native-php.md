# ☁️ Cloud-Native Development with TuskLang & PHP

## Introduction
Cloud-native development is the future of scalable, resilient applications. TuskLang and PHP let you build sophisticated cloud-native systems with config-driven containerization, orchestration, and monitoring that thrives in modern cloud environments.

## Key Features
- **Containerization with Docker**
- **Kubernetes deployment**
- **Service mesh integration**
- **Cloud-native databases**
- **Monitoring and observability**
- **CI/CD pipelines**
- **Auto-scaling and resilience**

## Example: Cloud-Native Configuration
```ini
[cloud_native]
platform: kubernetes
namespace: @env("K8S_NAMESPACE", "default")
replicas: @env("REPLICAS", 3)
resources: @go("cloud.ResourceRequirements")
monitoring: @go("cloud.MonitoringConfig")
scaling: @go("cloud.AutoScaling")
```

## PHP: Container Configuration
```php
<?php

namespace App\CloudNative;

use TuskLang\Config;
use TuskLang\Operators\Env;
use TuskLang\Operators\Metrics;
use TuskLang\Operators\Go;

class ContainerManager
{
    private $config;
    private $docker;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->docker = new DockerClient();
    }
    
    public function buildImage($context, $tag)
    {
        $dockerfile = $this->generateDockerfile();
        
        $buildConfig = [
            'context' => $context,
            'dockerfile' => $dockerfile,
            'tags' => [$tag],
            'buildargs' => $this->getBuildArgs()
        ];
        
        return $this->docker->build($buildConfig);
    }
    
    private function generateDockerfile()
    {
        $phpVersion = $this->config->get("container.php_version", "8.2");
        $extensions = $this->config->get("container.extensions", []);
        $packages = $this->config->get("container.packages", []);
        
        $dockerfile = "FROM php:{$phpVersion}-fpm-alpine\n\n";
        
        // Install system packages
        if (!empty($packages)) {
            $dockerfile .= "RUN apk add --no-cache " . implode(' ', $packages) . "\n\n";
        }
        
        // Install PHP extensions
        foreach ($extensions as $extension) {
            $dockerfile .= "RUN docker-php-ext-install {$extension}\n";
        }
        
        // Copy application
        $dockerfile .= "\nCOPY . /var/www/html/\n";
        $dockerfile .= "WORKDIR /var/www/html\n\n";
        
        // Install Composer dependencies
        $dockerfile .= "COPY --from=composer:latest /usr/bin/composer /usr/bin/composer\n";
        $dockerfile .= "RUN composer install --no-dev --optimize-autoloader\n\n";
        
        // Set permissions
        $dockerfile .= "RUN chown -R www-data:www-data /var/www/html\n";
        $dockerfile .= "USER www-data\n\n";
        
        // Expose port
        $dockerfile .= "EXPOSE 9000\n\n";
        
        // Start PHP-FPM
        $dockerfile .= "CMD [\"php-fpm\"]\n";
        
        return $dockerfile;
    }
    
    private function getBuildArgs()
    {
        return [
            'APP_ENV' => Env::get('APP_ENV', 'production'),
            'APP_DEBUG' => Env::get('APP_DEBUG', 'false'),
            'PHP_VERSION' => $this->config->get("container.php_version", "8.2")
        ];
    }
    
    public function runContainer($image, $config = [])
    {
        $containerConfig = array_merge([
            'Image' => $image,
            'Env' => $this->getEnvironmentVariables(),
            'ExposedPorts' => ['9000/tcp' => []],
            'HostConfig' => [
                'PortBindings' => [
                    '9000/tcp' => [['HostPort' => '9000']]
                ],
                'RestartPolicy' => ['Name' => 'unless-stopped']
            ]
        ], $config);
        
        return $this->docker->createContainer($containerConfig);
    }
    
    private function getEnvironmentVariables()
    {
        $env = [];
        
        // Add environment variables from config
        $envVars = $this->config->get("container.environment", []);
        
        foreach ($envVars as $key => $value) {
            $env[] = "{$key}={$value}";
        }
        
        // Add secrets
        $secrets = $this->config->get("container.secrets", []);
        
        foreach ($secrets as $key) {
            $value = Env::secure($key);
            $env[] = "{$key}={$value}";
        }
        
        return $env;
    }
}
```

## Kubernetes Deployment
```php
<?php

namespace App\CloudNative\Kubernetes;

use TuskLang\Config;

class KubernetesManager
{
    private $config;
    private $k8s;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->k8s = new KubernetesClient();
    }
    
    public function deploy($name, $image, $config = [])
    {
        $deployment = $this->createDeployment($name, $image, $config);
        $service = $this->createService($name, $config);
        $ingress = $this->createIngress($name, $config);
        
        // Apply resources
        $this->k8s->createDeployment($deployment);
        $this->k8s->createService($service);
        $this->k8s->createIngress($ingress);
        
        return [
            'deployment' => $deployment,
            'service' => $service,
            'ingress' => $ingress
        ];
    }
    
    private function createDeployment($name, $image, $config)
    {
        $replicas = $config['replicas'] ?? $this->config->get("kubernetes.replicas", 3);
        $resources = $this->getResourceRequirements($config);
        
        return [
            'apiVersion' => 'apps/v1',
            'kind' => 'Deployment',
            'metadata' => [
                'name' => $name,
                'namespace' => $this->config->get("kubernetes.namespace", "default")
            ],
            'spec' => [
                'replicas' => $replicas,
                'selector' => [
                    'matchLabels' => [
                        'app' => $name
                    ]
                ],
                'template' => [
                    'metadata' => [
                        'labels' => [
                            'app' => $name
                        ]
                    ],
                    'spec' => [
                        'containers' => [
                            [
                                'name' => $name,
                                'image' => $image,
                                'ports' => [
                                    ['containerPort' => 9000]
                                ],
                                'resources' => $resources,
                                'env' => $this->getK8sEnvironmentVariables(),
                                'livenessProbe' => [
                                    'httpGet' => [
                                        'path' => '/health',
                                        'port' => 9000
                                    ],
                                    'initialDelaySeconds' => 30,
                                    'periodSeconds' => 10
                                ],
                                'readinessProbe' => [
                                    'httpGet' => [
                                        'path' => '/ready',
                                        'port' => 9000
                                    ],
                                    'initialDelaySeconds' => 5,
                                    'periodSeconds' => 5
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ];
    }
    
    private function createService($name, $config)
    {
        return [
            'apiVersion' => 'v1',
            'kind' => 'Service',
            'metadata' => [
                'name' => $name,
                'namespace' => $this->config->get("kubernetes.namespace", "default")
            ],
            'spec' => [
                'selector' => [
                    'app' => $name
                ],
                'ports' => [
                    [
                        'protocol' => 'TCP',
                        'port' => 80,
                        'targetPort' => 9000
                    ]
                ],
                'type' => 'ClusterIP'
            ]
        ];
    }
    
    private function createIngress($name, $config)
    {
        $host = $config['host'] ?? "{$name}.example.com";
        
        return [
            'apiVersion' => 'networking.k8s.io/v1',
            'kind' => 'Ingress',
            'metadata' => [
                'name' => $name,
                'namespace' => $this->config->get("kubernetes.namespace", "default"),
                'annotations' => [
                    'kubernetes.io/ingress.class' => 'nginx',
                    'cert-manager.io/cluster-issuer' => 'letsencrypt-prod'
                ]
            ],
            'spec' => [
                'tls' => [
                    [
                        'hosts' => [$host],
                        'secretName' => "{$name}-tls"
                    ]
                ],
                'rules' => [
                    [
                        'host' => $host,
                        'http' => [
                            'paths' => [
                                [
                                    'path' => '/',
                                    'pathType' => 'Prefix',
                                    'backend' => [
                                        'service' => [
                                            'name' => $name,
                                            'port' => [
                                                'number' => 80
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ];
    }
    
    private function getResourceRequirements($config)
    {
        $defaultResources = $this->config->get("kubernetes.resources", [
            'requests' => [
                'memory' => '128Mi',
                'cpu' => '100m'
            ],
            'limits' => [
                'memory' => '512Mi',
                'cpu' => '500m'
            ]
        ]);
        
        return array_merge($defaultResources, $config['resources'] ?? []);
    }
    
    private function getK8sEnvironmentVariables()
    {
        $env = [];
        
        // Add environment variables from config
        $envVars = $this->config->get("kubernetes.environment", []);
        
        foreach ($envVars as $key => $value) {
            $env[] = [
                'name' => $key,
                'value' => $value
            ];
        }
        
        // Add secrets
        $secrets = $this->config->get("kubernetes.secrets", []);
        
        foreach ($secrets as $key => $secretName) {
            $env[] = [
                'name' => $key,
                'valueFrom' => [
                    'secretKeyRef' => [
                        'name' => $secretName,
                        'key' => $key
                    ]
                ]
            ];
        }
        
        return $env;
    }
}
```

## Service Mesh Integration
```php
<?php

namespace App\CloudNative\ServiceMesh;

use TuskLang\Config;

class ServiceMeshManager
{
    private $config;
    private $istio;
    
    public function __construct()
    {
        $this->config = new Config();
        $this->istio = new IstioClient();
    }
    
    public function configureMesh($serviceName, $config = [])
    {
        $virtualService = $this->createVirtualService($serviceName, $config);
        $destinationRule = $this->createDestinationRule($serviceName, $config);
        $gateway = $this->createGateway($serviceName, $config);
        
        // Apply Istio resources
        $this->istio->createVirtualService($virtualService);
        $this->istio->createDestinationRule($destinationRule);
        $this->istio->createGateway($gateway);
        
        return [
            'virtualService' => $virtualService,
            'destinationRule' => $destinationRule,
            'gateway' => $gateway
        ];
    }
    
    private function createVirtualService($serviceName, $config)
    {
        $host = $config['host'] ?? "{$serviceName}.example.com";
        
        return [
            'apiVersion' => 'networking.istio.io/v1alpha3',
            'kind' => 'VirtualService',
            'metadata' => [
                'name' => $serviceName,
                'namespace' => $this->config->get("istio.namespace", "default")
            ],
            'spec' => [
                'hosts' => [$host],
                'gateways' => ["{$serviceName}-gateway"],
                'http' => [
                    [
                        'route' => [
                            [
                                'destination' => [
                                    'host' => $serviceName,
                                    'port' => [
                                        'number' => 80
                                    ]
                                ],
                                'weight' => 100
                            ]
                        ],
                        'retries' => [
                            'attempts' => 3,
                            'perTryTimeout' => '2s'
                        ],
                        'timeout' => '10s'
                    ]
                ]
            ]
        ];
    }
    
    private function createDestinationRule($serviceName, $config)
    {
        return [
            'apiVersion' => 'networking.istio.io/v1alpha3',
            'kind' => 'DestinationRule',
            'metadata' => [
                'name' => $serviceName,
                'namespace' => $this->config->get("istio.namespace", "default")
            ],
            'spec' => [
                'host' => $serviceName,
                'trafficPolicy' => [
                    'loadBalancer' => [
                        'simple' => 'ROUND_ROBIN'
                    ],
                    'connectionPool' => [
                        'http' => [
                            'http1MaxPendingRequests' => 1024,
                            'maxRequestsPerConnection' => 1
                        ]
                    ],
                    'outlierDetection' => [
                        'consecutiveErrors' => 5,
                        'baseEjectionTime' => '30s',
                        'maxEjectionPercent' => 10
                    ]
                ]
            ]
        ];
    }
    
    private function createGateway($serviceName, $config)
    {
        $host = $config['host'] ?? "{$serviceName}.example.com";
        
        return [
            'apiVersion' => 'networking.istio.io/v1alpha3',
            'kind' => 'Gateway',
            'metadata' => [
                'name' => "{$serviceName}-gateway",
                'namespace' => $this->config->get("istio.namespace", "default")
            ],
            'spec' => [
                'selector' => [
                    'istio' => 'ingressgateway'
                ],
                'servers' => [
                    [
                        'port' => [
                            'number' => 80,
                            'name' => 'http',
                            'protocol' => 'HTTP'
                        ],
                        'hosts' => [$host]
                    ],
                    [
                        'port' => [
                            'number' => 443,
                            'name' => 'https',
                            'protocol' => 'HTTPS'
                        ],
                        'hosts' => [$host],
                        'tls' => [
                            'mode' => 'SIMPLE',
                            'credentialName' => "{$serviceName}-tls"
                        ]
                    ]
                ]
            ]
        ];
    }
}
```

## Cloud-Native Database Integration
```php
<?php

namespace App\CloudNative\Database;

use TuskLang\Config;
use TuskLang\Operators\Env;

class CloudDatabaseManager
{
    private $config;
    private $connections = [];
    
    public function __construct()
    {
        $this->config = new Config();
    }
    
    public function getConnection($name = 'default')
    {
        if (isset($this->connections[$name])) {
            return $this->connections[$name];
        }
        
        $config = $this->config->get("database.connections.{$name}", []);
        $type = $config['type'] ?? 'mysql';
        
        switch ($type) {
            case 'mysql':
                $connection = $this->createMySQLConnection($config);
                break;
                
            case 'postgresql':
                $connection = $this->createPostgreSQLConnection($config);
                break;
                
            case 'redis':
                $connection = $this->createRedisConnection($config);
                break;
                
            default:
                throw new \Exception("Unsupported database type: {$type}");
        }
        
        $this->connections[$name] = $connection;
        return $connection;
    }
    
    private function createMySQLConnection($config)
    {
        $host = $config['host'] ?? Env::get('DB_HOST', 'localhost');
        $port = $config['port'] ?? Env::get('DB_PORT', 3306);
        $database = $config['database'] ?? Env::get('DB_DATABASE', 'app');
        $username = $config['username'] ?? Env::get('DB_USERNAME', 'root');
        $password = $config['password'] ?? Env::secure('DB_PASSWORD');
        
        $dsn = "mysql:host={$host};port={$port};dbname={$database};charset=utf8mb4";
        
        $pdo = new PDO($dsn, $username, $password, [
            PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            PDO::ATTR_EMULATE_PREPARES => false
        ]);
        
        return $pdo;
    }
    
    private function createPostgreSQLConnection($config)
    {
        $host = $config['host'] ?? Env::get('DB_HOST', 'localhost');
        $port = $config['port'] ?? Env::get('DB_PORT', 5432);
        $database = $config['database'] ?? Env::get('DB_DATABASE', 'app');
        $username = $config['username'] ?? Env::get('DB_USERNAME', 'postgres');
        $password = $config['password'] ?? Env::secure('DB_PASSWORD');
        
        $dsn = "pgsql:host={$host};port={$port};dbname={$database}";
        
        $pdo = new PDO($dsn, $username, $password, [
            PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC
        ]);
        
        return $pdo;
    }
    
    private function createRedisConnection($config)
    {
        $host = $config['host'] ?? Env::get('REDIS_HOST', 'localhost');
        $port = $config['port'] ?? Env::get('REDIS_PORT', 6379);
        $password = $config['password'] ?? Env::secure('REDIS_PASSWORD');
        $database = $config['database'] ?? Env::get('REDIS_DB', 0);
        
        $redis = new Redis();
        $redis->connect($host, $port);
        
        if ($password) {
            $redis->auth($password);
        }
        
        if ($database > 0) {
            $redis->select($database);
        }
        
        return $redis;
    }
    
    public function migrate($connectionName = 'default')
    {
        $connection = $this->getConnection($connectionName);
        $migrations = $this->config->get("database.migrations", []);
        
        foreach ($migrations as $migration) {
            $this->runMigration($connection, $migration);
        }
    }
    
    private function runMigration($connection, $migration)
    {
        $sql = file_get_contents($migration['file']);
        
        try {
            $connection->exec($sql);
            echo "Migration {$migration['name']} completed successfully\n";
        } catch (\Exception $e) {
            echo "Migration {$migration['name']} failed: {$e->getMessage()}\n";
            throw $e;
        }
    }
}
```

## Best Practices
- **Use containerization for consistency**
- **Implement proper health checks**
- **Use service mesh for communication**
- **Monitor application performance**
- **Implement auto-scaling**
- **Use cloud-native databases**

## Performance Optimization
- **Optimize container images**
- **Use connection pooling**
- **Implement caching strategies**
- **Monitor resource usage**

## Security Considerations
- **Use secrets management**
- **Implement network policies**
- **Use RBAC for access control**
- **Scan container images**

## Troubleshooting
- **Check pod logs**
- **Monitor resource usage**
- **Verify service connectivity**
- **Check ingress configuration**

## Conclusion
TuskLang + PHP = cloud-native applications that are scalable, resilient, and ready for the future. Build applications that thrive in the cloud. 