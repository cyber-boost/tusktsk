# Advanced Containerization in PHP with TuskLang

## Overview

TuskLang revolutionizes containerization by making it configuration-driven, intelligent, and adaptive. This guide covers advanced containerization patterns that leverage TuskLang's dynamic capabilities for optimal deployment, scaling, and management.

## 🎯 Container Architecture

### Container Configuration

```ini
# container-architecture.tsk
[container_architecture]
orchestrator = "kubernetes"
container_runtime = "docker"
registry = "harbor.example.com"

[container_architecture.environments]
development = {
    replicas = 1,
    resources = { cpu = "100m", memory = "128Mi" },
    autoscaling = false,
    monitoring = "basic"
}

staging = {
    replicas = 2,
    resources = { cpu = "200m", memory = "256Mi" },
    autoscaling = true,
    monitoring = "standard"
}

production = {
    replicas = 3,
    resources = { cpu = "500m", memory = "512Mi" },
    autoscaling = true,
    monitoring = "advanced",
    high_availability = true
}

[container_architecture.services]
web_service = {
    image = "php:8.4-fpm",
    ports = [80, 443],
    health_check = "/health",
    liveness_probe = "/health",
    readiness_probe = "/ready"
}

api_service = {
    image = "php:8.4-cli",
    ports = [8000],
    health_check = "/api/health",
    liveness_probe = "/api/health",
    readiness_probe = "/api/ready"
}

worker_service = {
    image = "php:8.4-cli",
    ports = [],
    health_check = "/worker/health",
    liveness_probe = "/worker/health",
    readiness_probe = "/worker/ready"
}
```

### Container Manager Implementation

```php
<?php
// ContainerManager.php
class ContainerManager
{
    private $config;
    private $kubernetes;
    private $docker;
    private $registry;
    private $monitoring;
    
    public function __construct()
    {
        $this->config = new TuskConfig('container-architecture.tsk');
        $this->kubernetes = new KubernetesClient();
        $this->docker = new DockerClient();
        $this->registry = new RegistryClient();
        $this->monitoring = new ContainerMonitoring();
        $this->initializeContainerization();
    }
    
    private function initializeContainerization()
    {
        $orchestrator = $this->config->get('container_architecture.orchestrator');
        $runtime = $this->config->get('container_architecture.container_runtime');
        
        // Initialize orchestrator
        switch ($orchestrator) {
            case 'kubernetes':
                $this->initializeKubernetes();
                break;
            case 'docker_swarm':
                $this->initializeDockerSwarm();
                break;
            case 'nomad':
                $this->initializeNomad();
                break;
        }
        
        // Initialize runtime
        switch ($runtime) {
            case 'docker':
                $this->initializeDocker();
                break;
            case 'containerd':
                $this->initializeContainerd();
                break;
        }
    }
    
    public function deployService($serviceName, $environment = 'production')
    {
        $startTime = microtime(true);
        
        try {
            // Get service configuration
            $serviceConfig = $this->config->get("container_architecture.services.{$serviceName}");
            $envConfig = $this->config->get("container_architecture.environments.{$environment}");
            
            // Build container image
            $image = $this->buildImage($serviceName, $serviceConfig);
            
            // Push to registry
            $this->pushImage($image);
            
            // Deploy to orchestrator
            $deployment = $this->createDeployment($serviceName, $serviceConfig, $envConfig);
            
            // Configure monitoring
            $this->configureMonitoring($serviceName, $envConfig);
            
            // Configure autoscaling
            if ($envConfig['autoscaling']) {
                $this->configureAutoscaling($serviceName, $envConfig);
            }
            
            $duration = (microtime(true) - $startTime) * 1000;
            $this->recordDeploymentMetrics($serviceName, $environment, $duration);
            
            return $deployment;
            
        } catch (Exception $e) {
            $this->handleDeploymentError($serviceName, $environment, $e);
            throw $e;
        }
    }
    
    private function buildImage($serviceName, $serviceConfig)
    {
        $dockerfile = $this->generateDockerfile($serviceName, $serviceConfig);
        $buildContext = $this->createBuildContext($serviceName);
        
        $imageName = "{$serviceName}:{$this->getImageTag()}";
        
        $buildResult = $this->docker->build($buildContext, $dockerfile, $imageName);
        
        if (!$buildResult['success']) {
            throw new ContainerBuildException("Failed to build image: {$buildResult['error']}");
        }
        
        return $imageName;
    }
    
    private function generateDockerfile($serviceName, $serviceConfig)
    {
        $baseImage = $serviceConfig['image'];
        $ports = $serviceConfig['ports'];
        
        $dockerfile = "FROM {$baseImage}\n\n";
        
        // Install dependencies
        $dockerfile .= "RUN apt-get update && apt-get install -y \\\n";
        $dockerfile .= "    curl \\\n";
        $dockerfile .= "    git \\\n";
        $dockerfile .= "    unzip \\\n";
        $dockerfile .= "    && rm -rf /var/lib/apt/lists/*\n\n";
        
        // Install PHP extensions
        $dockerfile .= "RUN docker-php-ext-install \\\n";
        $dockerfile .= "    pdo_mysql \\\n";
        $dockerfile .= "    opcache \\\n";
        $dockerfile .= "    && docker-php-ext-enable opcache\n\n";
        
        // Copy application code
        $dockerfile .= "COPY . /var/www/html/\n";
        $dockerfile .= "WORKDIR /var/www/html\n\n";
        
        // Install Composer dependencies
        $dockerfile .= "COPY --from=composer:latest /usr/bin/composer /usr/bin/composer\n";
        $dockerfile .= "RUN composer install --no-dev --optimize-autoloader\n\n";
        
        // Configure PHP
        $dockerfile .= "COPY docker/php.ini /usr/local/etc/php/\n";
        $dockerfile .= "COPY docker/opcache.ini /usr/local/etc/php/conf.d/\n\n";
        
        // Expose ports
        foreach ($ports as $port) {
            $dockerfile .= "EXPOSE {$port}\n";
        }
        
        // Health check
        if (isset($serviceConfig['health_check'])) {
            $dockerfile .= "HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \\\n";
            $dockerfile .= "    CMD curl -f {$serviceConfig['health_check']} || exit 1\n\n";
        }
        
        // Start command
        $dockerfile .= "CMD [\"php\", \"-S\", \"0.0.0.0:8000\", \"-t\", \"/var/www/html/public\"]\n";
        
        return $dockerfile;
    }
    
    private function createDeployment($serviceName, $serviceConfig, $envConfig)
    {
        $deployment = [
            'apiVersion' => 'apps/v1',
            'kind' => 'Deployment',
            'metadata' => [
                'name' => $serviceName,
                'labels' => [
                    'app' => $serviceName,
                    'environment' => $envConfig['environment'] ?? 'production'
                ]
            ],
            'spec' => [
                'replicas' => $envConfig['replicas'],
                'selector' => [
                    'matchLabels' => [
                        'app' => $serviceName
                    ]
                ],
                'template' => [
                    'metadata' => [
                        'labels' => [
                            'app' => $serviceName
                        ]
                    ],
                    'spec' => [
                        'containers' => [
                            [
                                'name' => $serviceName,
                                'image' => $this->getImageUrl($serviceName),
                                'ports' => $this->createPorts($serviceConfig['ports']),
                                'resources' => [
                                    'requests' => $envConfig['resources'],
                                    'limits' => [
                                        'cpu' => $envConfig['resources']['cpu'],
                                        'memory' => $envConfig['resources']['memory']
                                    ]
                                ],
                                'livenessProbe' => $this->createProbe($serviceConfig['liveness_probe']),
                                'readinessProbe' => $this->createProbe($serviceConfig['readiness_probe']),
                                'env' => $this->createEnvironmentVariables($serviceName, $envConfig)
                            ]
                        ]
                    ]
                ]
            ]
        ];
        
        return $this->kubernetes->createDeployment($deployment);
    }
    
    private function createProbe($probePath)
    {
        return [
            'httpGet' => [
                'path' => $probePath,
                'port' => 8000
            ],
            'initialDelaySeconds' => 30,
            'periodSeconds' => 10,
            'timeoutSeconds' => 5,
            'failureThreshold' => 3
        ];
    }
    
    private function createEnvironmentVariables($serviceName, $envConfig)
    {
        $envVars = [
            [
                'name' => 'APP_ENV',
                'value' => $envConfig['environment'] ?? 'production'
            ],
            [
                'name' => 'SERVICE_NAME',
                'value' => $serviceName
            ],
            [
                'name' => 'KUBERNETES_SERVICE_HOST',
                'valueFrom' => [
                    'fieldRef' => [
                        'fieldPath' => 'status.hostIP'
                    ]
                ]
            ]
        ];
        
        // Add service-specific environment variables
        $serviceEnvVars = $this->config->get("container_architecture.environment_variables.{$serviceName}", []);
        foreach ($serviceEnvVars as $key => $value) {
            $envVars[] = [
                'name' => $key,
                'value' => $value
            ];
        }
        
        return $envVars;
    }
}
```

## 🔄 Kubernetes Integration

### Kubernetes Configuration

```ini
# kubernetes-integration.tsk
[kubernetes_integration]
cluster_name = "production-cluster"
namespace = "default"
context = "production"

[kubernetes_integration.resources]
deployments = true
services = true
ingress = true
configmaps = true
secrets = true
persistent_volumes = true

[kubernetes_integration.networking]
service_mesh = "istio"
load_balancer = "nginx"
ingress_controller = "nginx-ingress"

[kubernetes_integration.storage]
storage_class = "fast-ssd"
backup_enabled = true
backup_retention = 30

[kubernetes_integration.security]
rbac_enabled = true
network_policies = true
pod_security_policies = true
```

### Kubernetes Integration Implementation

```php
class KubernetesIntegration
{
    private $config;
    private $client;
    private $namespace;
    
    public function __construct()
    {
        $this->config = new TuskConfig('kubernetes-integration.tsk');
        $this->client = new KubernetesClient();
        $this->namespace = $this->config->get('kubernetes_integration.namespace');
    }
    
    public function createService($serviceName, $serviceConfig)
    {
        $service = [
            'apiVersion' => 'v1',
            'kind' => 'Service',
            'metadata' => [
                'name' => $serviceName,
                'namespace' => $this->namespace,
                'labels' => [
                    'app' => $serviceName
                ]
            ],
            'spec' => [
                'selector' => [
                    'app' => $serviceName
                ],
                'ports' => $this->createServicePorts($serviceConfig['ports']),
                'type' => 'ClusterIP'
            ]
        ];
        
        return $this->client->createService($service);
    }
    
    public function createIngress($serviceName, $serviceConfig)
    {
        $ingress = [
            'apiVersion' => 'networking.k8s.io/v1',
            'kind' => 'Ingress',
            'metadata' => [
                'name' => $serviceName,
                'namespace' => $this->namespace,
                'annotations' => [
                    'kubernetes.io/ingress.class' => 'nginx',
                    'nginx.ingress.kubernetes.io/ssl-redirect' => 'true',
                    'nginx.ingress.kubernetes.io/force-ssl-redirect' => 'true'
                ]
            ],
            'spec' => [
                'tls' => [
                    [
                        'hosts' => [$this->getDomain($serviceName)],
                        'secretName' => "{$serviceName}-tls"
                    ]
                ],
                'rules' => [
                    [
                        'host' => $this->getDomain($serviceName),
                        'http' => [
                            'paths' => [
                                [
                                    'path' => '/',
                                    'pathType' => 'Prefix',
                                    'backend' => [
                                        'service' => [
                                            'name' => $serviceName,
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
        
        return $this->client->createIngress($ingress);
    }
    
    public function createConfigMap($serviceName, $configData)
    {
        $configMap = [
            'apiVersion' => 'v1',
            'kind' => 'ConfigMap',
            'metadata' => [
                'name' => "{$serviceName}-config",
                'namespace' => $this->namespace
            ],
            'data' => $configData
        ];
        
        return $this->client->createConfigMap($configMap);
    }
    
    public function createSecret($serviceName, $secretData)
    {
        $secret = [
            'apiVersion' => 'v1',
            'kind' => 'Secret',
            'metadata' => [
                'name' => "{$serviceName}-secret",
                'namespace' => $this->namespace
            ],
            'type' => 'Opaque',
            'data' => $this->encodeSecretData($secretData)
        ];
        
        return $this->client->createSecret($secret);
    }
    
    public function scaleDeployment($serviceName, $replicas)
    {
        return $this->client->scaleDeployment($serviceName, $this->namespace, $replicas);
    }
    
    public function getPodStatus($serviceName)
    {
        $pods = $this->client->getPods($this->namespace, [
            'app' => $serviceName
        ]);
        
        $status = [
            'total' => count($pods),
            'running' => 0,
            'pending' => 0,
            'failed' => 0
        ];
        
        foreach ($pods as $pod) {
            $phase = $pod['status']['phase'];
            $status[$phase]++;
        }
        
        return $status;
    }
    
    private function createServicePorts($ports)
    {
        $servicePorts = [];
        
        foreach ($ports as $port) {
            $servicePorts[] = [
                'name' => "port-{$port}",
                'port' => $port,
                'targetPort' => $port,
                'protocol' => 'TCP'
            ];
        }
        
        return $servicePorts;
    }
    
    private function encodeSecretData($data)
    {
        $encoded = [];
        
        foreach ($data as $key => $value) {
            $encoded[$key] = base64_encode($value);
        }
        
        return $encoded;
    }
}
```

## 🧠 Container Orchestration

### Orchestration Configuration

```ini
# container-orchestration.tsk
[container_orchestration]
strategy = "blue_green"
rollback_enabled = true
canary_deployment = true

[container_orchestration.deployment]
max_surge = 1
max_unavailable = 0
progress_deadline = 600
revision_history_limit = 10

[container_orchestration.rollback]
automatic = true
failure_threshold = 0.1
health_check_grace_period = 60

[container_orchestration.canary]
traffic_split = 0.1
evaluation_period = 300
success_criteria = "error_rate < 0.05"
```

### Orchestration Implementation

```php
class ContainerOrchestration
{
    private $config;
    private $kubernetes;
    private $monitoring;
    
    public function __construct()
    {
        $this->config = new TuskConfig('container-orchestration.tsk');
        $this->kubernetes = new KubernetesIntegration();
        $this->monitoring = new ContainerMonitoring();
    }
    
    public function deployWithStrategy($serviceName, $image, $strategy = null)
    {
        $strategy = $strategy ?: $this->config->get('container_orchestration.strategy');
        
        switch ($strategy) {
            case 'blue_green':
                return $this->blueGreenDeployment($serviceName, $image);
            case 'rolling':
                return $this->rollingDeployment($serviceName, $image);
            case 'canary':
                return $this->canaryDeployment($serviceName, $image);
            default:
                throw new InvalidArgumentException("Unknown deployment strategy: {$strategy}");
        }
    }
    
    private function blueGreenDeployment($serviceName, $image)
    {
        // Get current deployment
        $currentDeployment = $this->kubernetes->getDeployment($serviceName);
        $currentColor = $this->getCurrentColor($currentDeployment);
        $newColor = $currentColor === 'blue' ? 'green' : 'blue';
        
        // Create new deployment
        $newDeployment = $this->createColoredDeployment($serviceName, $image, $newColor);
        
        // Wait for new deployment to be ready
        $this->waitForDeploymentReady($newDeployment['metadata']['name']);
        
        // Switch traffic
        $this->switchTraffic($serviceName, $newColor);
        
        // Clean up old deployment
        $this->cleanupOldDeployment($serviceName, $currentColor);
        
        return $newDeployment;
    }
    
    private function canaryDeployment($serviceName, $image)
    {
        $trafficSplit = $this->config->get('container_orchestration.canary.traffic_split');
        $evaluationPeriod = $this->config->get('container_orchestration.canary.evaluation_period');
        $successCriteria = $this->config->get('container_orchestration.canary.success_criteria');
        
        // Create canary deployment
        $canaryDeployment = $this->createCanaryDeployment($serviceName, $image);
        
        // Gradually increase traffic
        $this->graduallyIncreaseTraffic($serviceName, $trafficSplit);
        
        // Monitor canary performance
        $startTime = time();
        while (time() - $startTime < $evaluationPeriod) {
            $metrics = $this->monitoring->getServiceMetrics($serviceName);
            
            if ($this->evaluateSuccessCriteria($metrics, $successCriteria)) {
                // Canary successful, promote to full deployment
                $this->promoteCanary($serviceName);
                return $canaryDeployment;
            }
            
            sleep(30); // Check every 30 seconds
        }
        
        // Canary failed, rollback
        $this->rollbackCanary($serviceName);
        throw new CanaryDeploymentFailedException("Canary deployment failed success criteria");
    }
    
    private function rollingDeployment($serviceName, $image)
    {
        $maxSurge = $this->config->get('container_orchestration.deployment.max_surge');
        $maxUnavailable = $this->config->get('container_orchestration.deployment.max_unavailable');
        
        $deployment = [
            'spec' => [
                'strategy' => [
                    'type' => 'RollingUpdate',
                    'rollingUpdate' => [
                        'maxSurge' => $maxSurge,
                        'maxUnavailable' => $maxUnavailable
                    ]
                ]
            ]
        ];
        
        return $this->kubernetes->updateDeployment($serviceName, $deployment);
    }
    
    private function evaluateSuccessCriteria($metrics, $criteria)
    {
        // Parse success criteria
        if (preg_match('/error_rate < ([\d.]+)/', $criteria, $matches)) {
            $threshold = floatval($matches[1]);
            return $metrics['error_rate'] < $threshold;
        }
        
        return true; // Default to success
    }
    
    public function rollbackDeployment($serviceName, $revision = null)
    {
        if (!$this->config->get('container_orchestration.rollback_enabled')) {
            throw new RollbackNotEnabledException("Rollback is not enabled");
        }
        
        if ($revision === null) {
            $revision = $this->getPreviousRevision($serviceName);
        }
        
        return $this->kubernetes->rollbackDeployment($serviceName, $revision);
    }
    
    private function getPreviousRevision($serviceName)
    {
        $revisions = $this->kubernetes->getDeploymentRevisions($serviceName);
        
        if (count($revisions) < 2) {
            throw new NoPreviousRevisionException("No previous revision available");
        }
        
        return $revisions[count($revisions) - 2];
    }
}
```

## 📊 Container Monitoring

### Container Monitoring Configuration

```ini
# container-monitoring.tsk
[container_monitoring]
enabled = true
metrics_collection = true
log_aggregation = true
alerting = true

[container_monitoring.metrics]
cpu_usage = true
memory_usage = true
network_io = true
disk_io = true
container_health = true

[container_monitoring.logging]
driver = "fluentd"
retention = 30
compression = true
search_enabled = true

[container_monitoring.alerting]
cpu_threshold = 0.8
memory_threshold = 0.9
disk_threshold = 0.85
restart_threshold = 5
```

### Container Monitoring Implementation

```php
class ContainerMonitoring
{
    private $config;
    private $metrics;
    private $logs;
    private $alerts;
    
    public function __construct()
    {
        $this->config = new TuskConfig('container-monitoring.tsk');
        $this->metrics = new MetricsCollector();
        $this->logs = new LogAggregator();
        $this->alerts = new AlertManager();
    }
    
    public function collectMetrics($podName, $namespace)
    {
        if (!$this->config->get('container_monitoring.metrics_collection')) {
            return;
        }
        
        $metrics = [];
        
        if ($this->config->get('container_monitoring.metrics.cpu_usage')) {
            $metrics['cpu_usage'] = $this->getCPUUsage($podName, $namespace);
        }
        
        if ($this->config->get('container_monitoring.metrics.memory_usage')) {
            $metrics['memory_usage'] = $this->getMemoryUsage($podName, $namespace);
        }
        
        if ($this->config->get('container_monitoring.metrics.network_io')) {
            $metrics['network_io'] = $this->getNetworkIO($podName, $namespace);
        }
        
        if ($this->config->get('container_monitoring.metrics.disk_io')) {
            $metrics['disk_io'] = $this->getDiskIO($podName, $namespace);
        }
        
        $this->storeMetrics($podName, $namespace, $metrics);
        $this->checkAlerts($podName, $namespace, $metrics);
        
        return $metrics;
    }
    
    public function collectLogs($podName, $namespace)
    {
        if (!$this->config->get('container_monitoring.log_aggregation')) {
            return;
        }
        
        $driver = $this->config->get('container_monitoring.logging.driver');
        
        switch ($driver) {
            case 'fluentd':
                return $this->collectLogsWithFluentd($podName, $namespace);
            case 'logstash':
                return $this->collectLogsWithLogstash($podName, $namespace);
            case 'filebeat':
                return $this->collectLogsWithFilebeat($podName, $namespace);
        }
    }
    
    public function getServiceMetrics($serviceName, $timeRange = 3600)
    {
        $sql = "
            SELECT 
                AVG(cpu_usage) as avg_cpu,
                MAX(cpu_usage) as max_cpu,
                AVG(memory_usage) as avg_memory,
                MAX(memory_usage) as max_memory,
                COUNT(*) as data_points
            FROM container_metrics 
            WHERE service_name = ? AND timestamp > ?
        ";
        
        $result = $this->database->query($sql, [$serviceName, time() - $timeRange]);
        return $result->fetch();
    }
    
    private function checkAlerts($podName, $namespace, $metrics)
    {
        if (!$this->config->get('container_monitoring.alerting')) {
            return;
        }
        
        $cpuThreshold = $this->config->get('container_monitoring.alerting.cpu_threshold');
        $memoryThreshold = $this->config->get('container_monitoring.alerting.memory_threshold');
        
        if ($metrics['cpu_usage'] > $cpuThreshold) {
            $this->alerts->trigger('high_cpu_usage', [
                'pod' => $podName,
                'namespace' => $namespace,
                'cpu_usage' => $metrics['cpu_usage'],
                'threshold' => $cpuThreshold
            ]);
        }
        
        if ($metrics['memory_usage'] > $memoryThreshold) {
            $this->alerts->trigger('high_memory_usage', [
                'pod' => $podName,
                'namespace' => $namespace,
                'memory_usage' => $metrics['memory_usage'],
                'threshold' => $memoryThreshold
            ]);
        }
    }
    
    private function getCPUUsage($podName, $namespace)
    {
        // Implementation to get CPU usage from Kubernetes metrics API
        $metrics = $this->kubernetes->getPodMetrics($podName, $namespace);
        return $metrics['cpu'] ?? 0;
    }
    
    private function getMemoryUsage($podName, $namespace)
    {
        // Implementation to get memory usage from Kubernetes metrics API
        $metrics = $this->kubernetes->getPodMetrics($podName, $namespace);
        return $metrics['memory'] ?? 0;
    }
}
```

## 📋 Best Practices

### Containerization Best Practices

1. **Multi-Stage Builds**: Use multi-stage Dockerfiles for smaller images
2. **Security Scanning**: Scan images for vulnerabilities before deployment
3. **Resource Limits**: Set appropriate CPU and memory limits
4. **Health Checks**: Implement proper health check endpoints
5. **Logging Strategy**: Use structured logging and log aggregation
6. **Configuration Management**: Use ConfigMaps and Secrets for configuration
7. **Backup Strategy**: Implement persistent volume backups
8. **Monitoring**: Monitor container health and performance

### Integration Examples

```php
// Dockerfile Generation
class DockerfileGenerator
{
    public function generateMultiStage($serviceName, $config)
    {
        $dockerfile = "FROM php:8.4-fpm AS base\n\n";
        $dockerfile .= "# Install system dependencies\n";
        $dockerfile .= "RUN apt-get update && apt-get install -y \\\n";
        $dockerfile .= "    git \\\n";
        $dockerfile .= "    curl \\\n";
        $dockerfile .= "    libpng-dev \\\n";
        $dockerfile .= "    libonig-dev \\\n";
        $dockerfile .= "    libxml2-dev \\\n";
        $dockerfile .= "    zip \\\n";
        $dockerfile .= "    unzip\n\n";
        
        $dockerfile .= "# Install PHP extensions\n";
        $dockerfile .= "RUN docker-php-ext-install pdo_mysql mbstring exif pcntl bcmath gd\n\n";
        
        $dockerfile .= "FROM base AS composer\n\n";
        $dockerfile .= "COPY --from=composer:latest /usr/bin/composer /usr/bin/composer\n";
        $dockerfile .= "COPY . /var/www/html/\n";
        $dockerfile .= "WORKDIR /var/www/html\n";
        $dockerfile .= "RUN composer install --no-dev --optimize-autoloader\n\n";
        
        $dockerfile .= "FROM base AS production\n\n";
        $dockerfile .= "COPY --from=composer /var/www/html /var/www/html\n";
        $dockerfile .= "WORKDIR /var/www/html\n";
        $dockerfile .= "RUN chown -R www-data:www-data /var/www/html\n";
        $dockerfile .= "USER www-data\n\n";
        
        $dockerfile .= "EXPOSE 8000\n";
        $dockerfile .= "CMD [\"php\", \"-S\", \"0.0.0.0:8000\", \"-t\", \"/var/www/html/public\"]\n";
        
        return $dockerfile;
    }
}
```

## 🔧 Troubleshooting

### Common Issues

1. **Image Build Failures**: Check Dockerfile syntax and dependencies
2. **Deployment Failures**: Verify Kubernetes manifests and resource limits
3. **Service Discovery Issues**: Check service and ingress configurations
4. **Resource Exhaustion**: Monitor CPU and memory usage
5. **Network Connectivity**: Verify network policies and service mesh

### Debug Configuration

```ini
# debug-containerization.tsk
[debug]
enabled = true
log_level = "verbose"
trace_deployments = true

[debug.output]
console = true
file = "/var/log/tusk-containerization-debug.log"
```

This comprehensive containerization system leverages TuskLang's configuration-driven approach to create intelligent, scalable, and manageable containerized applications that adapt to deployment needs automatically. 