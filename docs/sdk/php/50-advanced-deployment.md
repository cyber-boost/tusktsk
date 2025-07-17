# Advanced Deployment in PHP with TuskLang

## Overview

TuskLang revolutionizes deployment by making it configuration-driven, intelligent, and adaptive. This guide covers advanced deployment patterns that leverage TuskLang's dynamic capabilities for comprehensive application deployment and infrastructure management.

## 🎯 Deployment Architecture

### Deployment Configuration

```ini
# deployment-architecture.tsk
[deployment_architecture]
strategy = "blue_green"
platform = "kubernetes"
orchestration = "helm"
monitoring = "prometheus"

[deployment_architecture.environments]
development = {
    replicas = 1,
    resources = "minimal",
    auto_scaling = false,
    monitoring = "basic"
}

staging = {
    replicas = 2,
    resources = "medium",
    auto_scaling = true,
    monitoring = "detailed"
}

production = {
    replicas = 5,
    resources = "high",
    auto_scaling = true,
    monitoring = "comprehensive"
}

[deployment_architecture.strategies]
blue_green = {
    enabled = true,
    health_check = true,
    rollback_threshold = 5,
    switchover_time = 30
}

canary = {
    enabled = true,
    traffic_split = 10,
    evaluation_period = 300,
    promotion_criteria = "success_rate > 95"
}

rolling = {
    enabled = true,
    max_unavailable = 1,
    max_surge = 1,
    health_check = true
}

[deployment_architecture.infrastructure]
kubernetes = {
    enabled = true,
    cluster = "production-cluster",
    namespace = "application"
}

docker = {
    enabled = true,
    registry = "docker.io",
    image_tag = "latest"
}

cloud = {
    provider = "aws",
    region = "us-east-1",
    vpc = "vpc-123456"
}
```

### Deployment Manager Implementation

```php
<?php
// DeploymentManager.php
class DeploymentManager
{
    private $config;
    private $kubernetes;
    private $docker;
    private $monitor;
    private $notifier;
    
    public function __construct()
    {
        $this->config = new TuskConfig('deployment-architecture.tsk');
        $this->kubernetes = new KubernetesManager();
        $this->docker = new DockerManager();
        $this->monitor = new DeploymentMonitor();
        $this->notifier = new DeploymentNotifier();
        $this->initializeDeployment();
    }
    
    private function initializeDeployment()
    {
        $strategy = $this->config->get('deployment_architecture.strategy');
        
        switch ($strategy) {
            case 'blue_green':
                $this->initializeBlueGreen();
                break;
            case 'canary':
                $this->initializeCanary();
                break;
            case 'rolling':
                $this->initializeRolling();
                break;
        }
    }
    
    public function deploy($version, $environment = 'production', $options = [])
    {
        $startTime = microtime(true);
        
        try {
            // Validate deployment
            $this->validateDeployment($version, $environment);
            
            // Build and push image
            $image = $this->buildAndPushImage($version);
            
            // Get deployment strategy
            $strategy = $this->getDeploymentStrategy($environment);
            
            // Execute deployment
            $deployment = $this->executeDeployment($strategy, $image, $environment, $options);
            
            // Monitor deployment
            $this->monitorDeployment($deployment);
            
            // Verify deployment
            $verification = $this->verifyDeployment($deployment);
            
            // Complete deployment
            $this->completeDeployment($deployment, $verification);
            
            $duration = (microtime(true) - $startTime) * 1000;
            $this->logDeployment($version, $environment, $deployment, $duration);
            
            return [
                'version' => $version,
                'environment' => $environment,
                'deployment_id' => $deployment['id'],
                'status' => 'success',
                'duration' => $duration
            ];
            
        } catch (Exception $e) {
            $this->handleDeploymentError($version, $environment, $e);
            throw $e;
        }
    }
    
    private function validateDeployment($version, $environment)
    {
        // Check if version exists
        if (!$this->versionExists($version)) {
            throw new DeploymentException("Version {$version} does not exist");
        }
        
        // Check environment configuration
        $envConfig = $this->config->get("deployment_architecture.environments.{$environment}");
        if (!$envConfig) {
            throw new DeploymentException("Environment {$environment} not configured");
        }
        
        // Check deployment prerequisites
        $this->checkPrerequisites($environment);
    }
    
    private function buildAndPushImage($version)
    {
        $dockerConfig = $this->config->get('deployment_architecture.infrastructure.docker');
        
        // Build Docker image
        $imageName = "{$dockerConfig['registry']}/application:{$version}";
        $buildResult = $this->docker->build($imageName, [
            'context' => '.',
            'dockerfile' => 'Dockerfile',
            'build_args' => [
                'VERSION' => $version,
                'ENVIRONMENT' => $environment
            ]
        ]);
        
        if (!$buildResult['success']) {
            throw new DeploymentException("Docker build failed: " . $buildResult['error']);
        }
        
        // Push image to registry
        $pushResult = $this->docker->push($imageName);
        
        if (!$pushResult['success']) {
            throw new DeploymentException("Docker push failed: " . $pushResult['error']);
        }
        
        return $imageName;
    }
    
    private function executeDeployment($strategy, $image, $environment, $options)
    {
        $deploymentId = uniqid();
        
        switch ($strategy) {
            case 'blue_green':
                return $this->executeBlueGreenDeployment($deploymentId, $image, $environment, $options);
            case 'canary':
                return $this->executeCanaryDeployment($deploymentId, $image, $environment, $options);
            case 'rolling':
                return $this->executeRollingDeployment($deploymentId, $image, $environment, $options);
            default:
                throw new DeploymentException("Unknown deployment strategy: {$strategy}");
        }
    }
    
    private function executeBlueGreenDeployment($deploymentId, $image, $environment, $options)
    {
        $blueGreenConfig = $this->config->get('deployment_architecture.strategies.blue_green');
        
        // Determine current environment (blue or green)
        $currentEnv = $this->getCurrentEnvironment($environment);
        $newEnv = $currentEnv === 'blue' ? 'green' : 'blue';
        
        // Deploy to new environment
        $deployment = $this->kubernetes->deploy($image, [
            'environment' => $newEnv,
            'replicas' => $this->getReplicaCount($environment),
            'resources' => $this->getResourceRequirements($environment),
            'health_check' => $blueGreenConfig['health_check']
        ]);
        
        // Wait for deployment to be ready
        $this->waitForDeployment($deployment['name']);
        
        // Run health checks
        $healthChecks = $this->runHealthChecks($deployment['name']);
        
        if (!$healthChecks['success']) {
            // Rollback to previous environment
            $this->rollbackDeployment($deploymentId, $currentEnv);
            throw new DeploymentException("Health checks failed");
        }
        
        // Switch traffic to new environment
        $this->switchTraffic($environment, $newEnv);
        
        // Wait for switchover
        sleep($blueGreenConfig['switchover_time']);
        
        // Verify traffic switch
        $trafficVerification = $this->verifyTrafficSwitch($environment, $newEnv);
        
        if (!$trafficVerification['success']) {
            $this->rollbackTrafficSwitch($environment, $currentEnv);
            throw new DeploymentException("Traffic switch verification failed");
        }
        
        return [
            'id' => $deploymentId,
            'strategy' => 'blue_green',
            'old_environment' => $currentEnv,
            'new_environment' => $newEnv,
            'deployment' => $deployment,
            'health_checks' => $healthChecks,
            'traffic_switch' => $trafficVerification
        ];
    }
    
    private function executeCanaryDeployment($deploymentId, $image, $environment, $options)
    {
        $canaryConfig = $this->config->get('deployment_architecture.strategies.canary');
        
        // Deploy canary version
        $canaryDeployment = $this->kubernetes->deploy($image, [
            'environment' => 'canary',
            'replicas' => 1,
            'resources' => $this->getResourceRequirements($environment),
            'labels' => ['version' => 'canary']
        ]);
        
        // Wait for canary to be ready
        $this->waitForDeployment($canaryDeployment['name']);
        
        // Route small percentage of traffic to canary
        $trafficSplit = $canaryConfig['traffic_split'];
        $this->routeTraffic($environment, 'canary', $trafficSplit);
        
        // Monitor canary performance
        $evaluationPeriod = $canaryConfig['evaluation_period'];
        $monitoringResult = $this->monitorCanary($canaryDeployment['name'], $evaluationPeriod);
        
        // Evaluate promotion criteria
        $promotionCriteria = $canaryConfig['promotion_criteria'];
        $shouldPromote = $this->evaluatePromotionCriteria($monitoringResult, $promotionCriteria);
        
        if ($shouldPromote) {
            // Promote canary to full deployment
            $fullDeployment = $this->promoteCanary($canaryDeployment, $environment);
            
            return [
                'id' => $deploymentId,
                'strategy' => 'canary',
                'canary_deployment' => $canaryDeployment,
                'full_deployment' => $fullDeployment,
                'monitoring_result' => $monitoringResult,
                'promoted' => true
            ];
        } else {
            // Rollback canary
            $this->rollbackCanary($canaryDeployment);
            
            return [
                'id' => $deploymentId,
                'strategy' => 'canary',
                'canary_deployment' => $canaryDeployment,
                'monitoring_result' => $monitoringResult,
                'promoted' => false
            ];
        }
    }
    
    private function executeRollingDeployment($deploymentId, $image, $environment, $options)
    {
        $rollingConfig = $this->config->get('deployment_architecture.strategies.rolling');
        
        // Update deployment with new image
        $deployment = $this->kubernetes->updateDeployment($environment, [
            'image' => $image,
            'max_unavailable' => $rollingConfig['max_unavailable'],
            'max_surge' => $rollingConfig['max_surge'],
            'health_check' => $rollingConfig['health_check']
        ]);
        
        // Wait for rolling update to complete
        $this->waitForRollingUpdate($deployment['name']);
        
        // Verify deployment health
        $healthChecks = $this->runHealthChecks($deployment['name']);
        
        if (!$healthChecks['success']) {
            // Rollback rolling update
            $this->rollbackRollingUpdate($deployment['name']);
            throw new DeploymentException("Rolling update health checks failed");
        }
        
        return [
            'id' => $deploymentId,
            'strategy' => 'rolling',
            'deployment' => $deployment,
            'health_checks' => $healthChecks
        ];
    }
    
    private function monitorDeployment($deployment)
    {
        $this->monitor->startMonitoring($deployment['id'], [
            'metrics' => ['response_time', 'error_rate', 'throughput'],
            'alerts' => true,
            'dashboard' => true
        ]);
    }
    
    private function verifyDeployment($deployment)
    {
        $verification = [
            'health_checks' => $this->runHealthChecks($deployment['deployment']['name']),
            'performance_tests' => $this->runPerformanceTests($deployment['deployment']['name']),
            'smoke_tests' => $this->runSmokeTests($deployment['deployment']['name'])
        ];
        
        $verification['overall_success'] = 
            $verification['health_checks']['success'] &&
            $verification['performance_tests']['success'] &&
            $verification['smoke_tests']['success'];
        
        return $verification;
    }
    
    private function completeDeployment($deployment, $verification)
    {
        if ($verification['overall_success']) {
            // Mark deployment as successful
            $this->markDeploymentSuccess($deployment['id']);
            
            // Send success notification
            $this->notifier->sendSuccessNotification($deployment);
            
            // Update deployment history
            $this->updateDeploymentHistory($deployment, 'success');
        } else {
            // Mark deployment as failed
            $this->markDeploymentFailed($deployment['id']);
            
            // Send failure notification
            $this->notifier->sendFailureNotification($deployment, $verification);
            
            // Update deployment history
            $this->updateDeploymentHistory($deployment, 'failed');
            
            throw new DeploymentException("Deployment verification failed");
        }
    }
    
    public function rollback($deploymentId, $reason = '')
    {
        $deployment = $this->getDeployment($deploymentId);
        
        if (!$deployment) {
            throw new DeploymentException("Deployment {$deploymentId} not found");
        }
        
        $rollbackResult = $this->executeRollback($deployment);
        
        // Log rollback
        $this->logRollback($deploymentId, $reason, $rollbackResult);
        
        // Send rollback notification
        $this->notifier->sendRollbackNotification($deployment, $reason);
        
        return $rollbackResult;
    }
    
    private function executeRollback($deployment)
    {
        switch ($deployment['strategy']) {
            case 'blue_green':
                return $this->rollbackBlueGreen($deployment);
            case 'canary':
                return $this->rollbackCanary($deployment);
            case 'rolling':
                return $this->rollbackRolling($deployment);
            default:
                throw new DeploymentException("Unknown deployment strategy for rollback");
        }
    }
    
    public function getDeploymentStatus($deploymentId)
    {
        $deployment = $this->getDeployment($deploymentId);
        
        if (!$deployment) {
            return ['status' => 'not_found'];
        }
        
        $status = [
            'id' => $deploymentId,
            'version' => $deployment['version'],
            'environment' => $deployment['environment'],
            'strategy' => $deployment['strategy'],
            'status' => $deployment['status'],
            'created_at' => $deployment['created_at'],
            'updated_at' => $deployment['updated_at']
        ];
        
        // Add current metrics
        $status['metrics'] = $this->monitor->getDeploymentMetrics($deploymentId);
        
        // Add health status
        $status['health'] = $this->getDeploymentHealth($deploymentId);
        
        return $status;
    }
}
```

## 🐳 Docker and Containerization

### Docker Configuration

```ini
# docker-deployment.tsk
[docker_deployment]
enabled = true
multi_stage = true
optimization = true

[docker_deployment.build]
context = "."
dockerfile = "Dockerfile"
build_args = {
    "VERSION" = @env("APP_VERSION"),
    "ENVIRONMENT" = @env("APP_ENV")
}

[docker_deployment.images]
base = "php:8.4-fpm-alpine"
nginx = "nginx:alpine"
redis = "redis:alpine"

[docker_deployment.optimization]
layer_caching = true
multi_stage_build = true
image_compression = true
security_scanning = true

[docker_deployment.registry]
url = "docker.io"
username = @env("DOCKER_USERNAME")
password = @env("DOCKER_PASSWORD")
repository = "myapp"
```

### Docker Implementation

```php
class DockerManager
{
    private $config;
    private $client;
    
    public function __construct()
    {
        $this->config = new TuskConfig('docker-deployment.tsk');
        $this->client = new Docker\Docker();
    }
    
    public function build($imageName, $options = [])
    {
        $buildConfig = $this->config->get('docker_deployment.build');
        
        $buildOptions = [
            'context' => $buildConfig['context'],
            'dockerfile' => $buildConfig['dockerfile'],
            'buildargs' => array_merge($buildConfig['build_args'], $options['build_args'] ?? [])
        ];
        
        // Add optimization options
        if ($this->config->get('docker_deployment.optimization.layer_caching')) {
            $buildOptions['cache_from'] = [$imageName];
        }
        
        try {
            $response = $this->client->imageBuild($buildOptions);
            
            // Parse build output
            $buildResult = $this->parseBuildOutput($response);
            
            return [
                'success' => $buildResult['success'],
                'image_id' => $buildResult['image_id'],
                'error' => $buildResult['error'] ?? null
            ];
            
        } catch (Exception $e) {
            return [
                'success' => false,
                'error' => $e->getMessage()
            ];
        }
    }
    
    public function push($imageName)
    {
        $registryConfig = $this->config->get('docker_deployment.registry');
        
        try {
            // Authenticate with registry
            $this->authenticateRegistry($registryConfig);
            
            // Push image
            $response = $this->client->imagePush($imageName);
            
            return [
                'success' => true,
                'digest' => $this->extractDigest($response)
            ];
            
        } catch (Exception $e) {
            return [
                'success' => false,
                'error' => $e->getMessage()
            ];
        }
    }
    
    public function pull($imageName)
    {
        try {
            $response = $this->client->imagePull($imageName);
            
            return [
                'success' => true,
                'digest' => $this->extractDigest($response)
            ];
            
        } catch (Exception $e) {
            return [
                'success' => false,
                'error' => $e->getMessage()
            ];
        }
    }
    
    public function tag($sourceImage, $targetImage)
    {
        try {
            $this->client->imageTag($sourceImage, $targetImage);
            
            return [
                'success' => true
            ];
            
        } catch (Exception $e) {
            return [
                'success' => false,
                'error' => $e->getMessage()
            ];
        }
    }
    
    public function scan($imageName)
    {
        if (!$this->config->get('docker_deployment.optimization.security_scanning')) {
            return ['success' => true, 'vulnerabilities' => []];
        }
        
        try {
            // Run security scan
            $scanResult = $this->runSecurityScan($imageName);
            
            return [
                'success' => true,
                'vulnerabilities' => $scanResult['vulnerabilities'],
                'severity' => $scanResult['severity']
            ];
            
        } catch (Exception $e) {
            return [
                'success' => false,
                'error' => $e->getMessage()
            ];
        }
    }
    
    private function parseBuildOutput($response)
    {
        $output = '';
        $imageId = null;
        $error = null;
        
        foreach ($response as $chunk) {
            $data = json_decode($chunk, true);
            
            if (isset($data['stream'])) {
                $output .= $data['stream'];
            }
            
            if (isset($data['aux']['ID'])) {
                $imageId = $data['aux']['ID'];
            }
            
            if (isset($data['error'])) {
                $error = $data['error'];
            }
        }
        
        return [
            'success' => $imageId !== null && $error === null,
            'image_id' => $imageId,
            'error' => $error,
            'output' => $output
        ];
    }
    
    private function authenticateRegistry($config)
    {
        $auth = [
            'username' => $config['username'],
            'password' => $config['password']
        ];
        
        $this->client->systemAuth($auth);
    }
    
    private function extractDigest($response)
    {
        $digest = null;
        
        foreach ($response as $chunk) {
            $data = json_decode($chunk, true);
            
            if (isset($data['aux']['Digest'])) {
                $digest = $data['aux']['Digest'];
                break;
            }
        }
        
        return $digest;
    }
    
    private function runSecurityScan($imageName)
    {
        // Use Trivy or similar tool for security scanning
        $command = "trivy image --format json {$imageName}";
        $output = shell_exec($command);
        
        $scanData = json_decode($output, true);
        
        return [
            'vulnerabilities' => $scanData['Vulnerabilities'] ?? [],
            'severity' => $this->calculateSeverity($scanData['Vulnerabilities'] ?? [])
        ];
    }
    
    private function calculateSeverity($vulnerabilities)
    {
        $severityCounts = [
            'CRITICAL' => 0,
            'HIGH' => 0,
            'MEDIUM' => 0,
            'LOW' => 0
        ];
        
        foreach ($vulnerabilities as $vuln) {
            $severity = $vuln['Severity'] ?? 'UNKNOWN';
            if (isset($severityCounts[$severity])) {
                $severityCounts[$severity]++;
            }
        }
        
        return $severityCounts;
    }
}
```

## ☸️ Kubernetes Deployment

### Kubernetes Configuration

```ini
# kubernetes-deployment.tsk
[kubernetes_deployment]
enabled = true
cluster = "production-cluster"
namespace = "application"

[kubernetes_deployment.resources]
deployment = {
    api_version = "apps/v1",
    kind = "Deployment",
    replicas = 3
}

service = {
    api_version = "v1",
    kind = "Service",
    type = "LoadBalancer"
}

ingress = {
    api_version = "networking.k8s.io/v1",
    kind = "Ingress",
    annotations = {
        "kubernetes.io/ingress.class" = "nginx",
        "cert-manager.io/cluster-issuer" = "letsencrypt-prod"
    }
}

[kubernetes_deployment.scaling]
horizontal_pod_autoscaler = {
    enabled = true,
    min_replicas = 2,
    max_replicas = 10,
    target_cpu_utilization = 70
}

vertical_pod_autoscaler = {
    enabled = true,
    mode = "Auto"
}

[kubernetes_deployment.monitoring]
service_monitor = {
    enabled = true,
    interval = "30s"
}

pod_disruption_budget = {
    enabled = true,
    min_available = 1
}
```

### Kubernetes Implementation

```php
class KubernetesManager
{
    private $config;
    private $client;
    
    public function __construct()
    {
        $this->config = new TuskConfig('kubernetes-deployment.tsk');
        $this->client = new Kubernetes\Client();
    }
    
    public function deploy($image, $options = [])
    {
        $deploymentName = $this->generateDeploymentName($options['environment']);
        
        // Create deployment manifest
        $deployment = $this->createDeploymentManifest($deploymentName, $image, $options);
        
        // Apply deployment
        $result = $this->client->createDeployment($deployment);
        
        if (!$result['success']) {
            throw new DeploymentException("Failed to create deployment: " . $result['error']);
        }
        
        // Create service
        $service = $this->createServiceManifest($deploymentName);
        $serviceResult = $this->client->createService($service);
        
        // Create ingress
        $ingress = $this->createIngressManifest($deploymentName);
        $ingressResult = $this->client->createIngress($ingress);
        
        // Set up monitoring
        if ($this->config->get('kubernetes_deployment.monitoring.service_monitor.enabled')) {
            $this->createServiceMonitor($deploymentName);
        }
        
        // Set up autoscaling
        if ($this->config->get('kubernetes_deployment.scaling.horizontal_pod_autoscaler.enabled')) {
            $this->createHorizontalPodAutoscaler($deploymentName);
        }
        
        return [
            'name' => $deploymentName,
            'deployment' => $result,
            'service' => $serviceResult,
            'ingress' => $ingressResult
        ];
    }
    
    public function updateDeployment($environment, $options = [])
    {
        $deploymentName = $this->getDeploymentName($environment);
        
        // Update deployment
        $patch = $this->createDeploymentPatch($options);
        $result = $this->client->patchDeployment($deploymentName, $patch);
        
        return [
            'name' => $deploymentName,
            'result' => $result
        ];
    }
    
    public function deleteDeployment($deploymentName)
    {
        // Delete ingress
        $this->client->deleteIngress($deploymentName);
        
        // Delete service
        $this->client->deleteService($deploymentName);
        
        // Delete deployment
        $result = $this->client->deleteDeployment($deploymentName);
        
        return $result;
    }
    
    public function getDeploymentStatus($deploymentName)
    {
        $deployment = $this->client->getDeployment($deploymentName);
        
        if (!$deployment) {
            return ['status' => 'not_found'];
        }
        
        return [
            'name' => $deploymentName,
            'replicas' => $deployment['spec']['replicas'],
            'available_replicas' => $deployment['status']['availableReplicas'] ?? 0,
            'ready_replicas' => $deployment['status']['readyReplicas'] ?? 0,
            'updated_replicas' => $deployment['status']['updatedReplicas'] ?? 0,
            'conditions' => $deployment['status']['conditions'] ?? []
        ];
    }
    
    public function scaleDeployment($deploymentName, $replicas)
    {
        $patch = [
            'spec' => [
                'replicas' => $replicas
            ]
        ];
        
        return $this->client->patchDeployment($deploymentName, $patch);
    }
    
    private function createDeploymentManifest($name, $image, $options)
    {
        $deploymentConfig = $this->config->get('kubernetes_deployment.resources.deployment');
        
        $manifest = [
            'apiVersion' => $deploymentConfig['api_version'],
            'kind' => $deploymentConfig['kind'],
            'metadata' => [
                'name' => $name,
                'namespace' => $this->config->get('kubernetes_deployment.namespace'),
                'labels' => [
                    'app' => $name,
                    'version' => $options['version'] ?? 'latest'
                ]
            ],
            'spec' => [
                'replicas' => $options['replicas'] ?? $deploymentConfig['replicas'],
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
                                'name' => 'app',
                                'image' => $image,
                                'ports' => [
                                    [
                                        'containerPort' => 80
                                    ]
                                ],
                                'resources' => $this->createResourceRequirements($options['resources']),
                                'livenessProbe' => $this->createLivenessProbe(),
                                'readinessProbe' => $this->createReadinessProbe(),
                                'env' => $this->createEnvironmentVariables($options['environment'])
                            ]
                        ]
                    ]
                ]
            ]
        ];
        
        return $manifest;
    }
    
    private function createServiceManifest($name)
    {
        $serviceConfig = $this->config->get('kubernetes_deployment.resources.service');
        
        return [
            'apiVersion' => $serviceConfig['api_version'],
            'kind' => $serviceConfig['kind'],
            'metadata' => [
                'name' => $name,
                'namespace' => $this->config->get('kubernetes_deployment.namespace')
            ],
            'spec' => [
                'type' => $serviceConfig['type'],
                'selector' => [
                    'app' => $name
                ],
                'ports' => [
                    [
                        'port' => 80,
                        'targetPort' => 80,
                        'protocol' => 'TCP'
                    ]
                ]
            ]
        ];
    }
    
    private function createIngressManifest($name)
    {
        $ingressConfig = $this->config->get('kubernetes_deployment.resources.ingress');
        
        return [
            'apiVersion' => $ingressConfig['api_version'],
            'kind' => $ingressConfig['kind'],
            'metadata' => [
                'name' => $name,
                'namespace' => $this->config->get('kubernetes_deployment.namespace'),
                'annotations' => $ingressConfig['annotations']
            ],
            'spec' => [
                'rules' => [
                    [
                        'host' => 'app.example.com',
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
    
    private function createResourceRequirements($resources)
    {
        $requirements = [];
        
        if (isset($resources['requests'])) {
            $requirements['requests'] = $resources['requests'];
        }
        
        if (isset($resources['limits'])) {
            $requirements['limits'] = $resources['limits'];
        }
        
        return $requirements;
    }
    
    private function createLivenessProbe()
    {
        return [
            'httpGet' => [
                'path' => '/health',
                'port' => 80
            ],
            'initialDelaySeconds' => 30,
            'periodSeconds' => 10
        ];
    }
    
    private function createReadinessProbe()
    {
        return [
            'httpGet' => [
                'path' => '/ready',
                'port' => 80
            ],
            'initialDelaySeconds' => 5,
            'periodSeconds' => 5
        ];
    }
    
    private function createEnvironmentVariables($environment)
    {
        return [
            [
                'name' => 'APP_ENV',
                'value' => $environment
            ],
            [
                'name' => 'APP_DEBUG',
                'value' => $environment === 'production' ? 'false' : 'true'
            ]
        ];
    }
    
    private function createHorizontalPodAutoscaler($deploymentName)
    {
        $hpaConfig = $this->config->get('kubernetes_deployment.scaling.horizontal_pod_autoscaler');
        
        $hpa = [
            'apiVersion' => 'autoscaling/v2',
            'kind' => 'HorizontalPodAutoscaler',
            'metadata' => [
                'name' => $deploymentName,
                'namespace' => $this->config->get('kubernetes_deployment.namespace')
            ],
            'spec' => [
                'scaleTargetRef' => [
                    'apiVersion' => 'apps/v1',
                    'kind' => 'Deployment',
                    'name' => $deploymentName
                ],
                'minReplicas' => $hpaConfig['min_replicas'],
                'maxReplicas' => $hpaConfig['max_replicas'],
                'metrics' => [
                    [
                        'type' => 'Resource',
                        'resource' => [
                            'name' => 'cpu',
                            'target' => [
                                'type' => 'Utilization',
                                'averageUtilization' => $hpaConfig['target_cpu_utilization']
                            ]
                        ]
                    ]
                ]
            ]
        ];
        
        return $this->client->createHorizontalPodAutoscaler($hpa);
    }
    
    private function createServiceMonitor($deploymentName)
    {
        $monitorConfig = $this->config->get('kubernetes_deployment.monitoring.service_monitor');
        
        $serviceMonitor = [
            'apiVersion' => 'monitoring.coreos.com/v1',
            'kind' => 'ServiceMonitor',
            'metadata' => [
                'name' => $deploymentName,
                'namespace' => $this->config->get('kubernetes_deployment.namespace')
            ],
            'spec' => [
                'selector' => [
                    'matchLabels' => [
                        'app' => $deploymentName
                    ]
                ],
                'endpoints' => [
                    [
                        'port' => 'http',
                        'interval' => $monitorConfig['interval']
                    ]
                ]
            ]
        ];
        
        return $this->client->createServiceMonitor($serviceMonitor);
    }
}
```

## 📋 Best Practices

### Deployment Best Practices

1. **Immutable Infrastructure**: Use immutable images and containers
2. **Blue-Green Deployments**: Minimize downtime and risk
3. **Health Checks**: Implement comprehensive health checks
4. **Rollback Strategy**: Always have a rollback plan
5. **Monitoring**: Monitor deployments in real-time
6. **Security**: Scan images and implement security policies
7. **Automation**: Automate deployment processes
8. **Documentation**: Document deployment procedures

### Integration Examples

```php
// Deployment Integration
class DeploymentIntegration
{
    private $deploymentManager;
    private $dockerManager;
    private $kubernetesManager;
    
    public function __construct()
    {
        $this->deploymentManager = new DeploymentManager();
        $this->dockerManager = new DockerManager();
        $this->kubernetesManager = new KubernetesManager();
    }
    
    public function deployApplication($version, $environment)
    {
        return $this->deploymentManager->deploy($version, $environment);
    }
    
    public function rollbackApplication($deploymentId)
    {
        return $this->deploymentManager->rollback($deploymentId);
    }
    
    public function getDeploymentStatus($deploymentId)
    {
        return $this->deploymentManager->getDeploymentStatus($deploymentId);
    }
}
```

## 🔧 Troubleshooting

### Common Issues

1. **Image Build Failures**: Check Dockerfile and build context
2. **Deployment Failures**: Check Kubernetes manifests and resources
3. **Health Check Failures**: Verify application health endpoints
4. **Rollback Issues**: Ensure previous versions are available
5. **Resource Constraints**: Monitor resource usage and limits

### Debug Configuration

```ini
# debug-deployment.tsk
[debug]
enabled = true
log_level = "verbose"
trace_deployment = true

[debug.output]
console = true
file = "/var/log/tusk-deployment-debug.log"
```

This comprehensive deployment system leverages TuskLang's configuration-driven approach to create intelligent, adaptive deployment solutions that ensure reliable and efficient application deployment across all environments. 