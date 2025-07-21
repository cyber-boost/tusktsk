<?php

declare(strict_types=1);

namespace TuskLang\SDK\SystemOperations\CloudServices;

use TuskLang\SDK\Core\BaseOperator;
use TuskLang\SDK\Core\Interfaces\OperatorInterface;
use TuskLang\SDK\Core\Exceptions\CloudOperationException;
use TuskLang\SDK\SystemOperations\AI\CloudOptimizationEngine;
use TuskLang\SDK\SystemOperations\Security\AWSSecurityManager;
use TuskLang\SDK\SystemOperations\Cost\CostOptimizer;

/**
 * Advanced AWS Operations Operator with AI-Powered Optimization
 * 
 * Features:
 * - Comprehensive AWS SDK integration
 * - Intelligent AWS service auto-discovery
 * - AWS resource management with cost optimization
 * - AWS Lambda function deployment and management
 * - AWS CloudWatch metrics and logging integration
 * 
 * @package TuskLang\SDK\SystemOperations\CloudServices
 * @version 1.0.0
 * @author TuskLang AI System
 */
class AWSOperator extends BaseOperator implements OperatorInterface
{
    private CloudOptimizationEngine $optimizationEngine;
    private AWSSecurityManager $securityManager;
    private CostOptimizer $costOptimizer;
    private array $awsClients = [];
    private array $discoveredServices = [];
    private string $region;
    private array $credentials;

    public function __construct(array $config = [])
    {
        parent::__construct($config);
        
        $this->optimizationEngine = new CloudOptimizationEngine($config['optimization_config'] ?? []);
        $this->securityManager = new AWSSecurityManager($config['security_config'] ?? []);
        $this->costOptimizer = new CostOptimizer($config['cost_config'] ?? []);
        
        $this->region = $config['region'] ?? 'us-east-1';
        $this->credentials = $config['credentials'] ?? [];
        
        $this->initializeOperator();
    }

    /**
     * Initialize AWS SDK clients
     */
    public function initializeAWSClients(array $services = []): array
    {
        try {
            $defaultServices = ['ec2', 's3', 'lambda', 'cloudwatch', 'iam', 'rds', 'elasticache'];
            $servicesToInit = !empty($services) ? $services : $defaultServices;
            
            $initializedClients = [];
            
            foreach ($servicesToInit as $service) {
                try {
                    $client = $this->createAWSClient($service);
                    $this->awsClients[$service] = $client;
                    $initializedClients[$service] = [
                        'status' => 'initialized',
                        'client_class' => get_class($client),
                        'region' => $this->region
                    ];
                } catch (\Exception $e) {
                    $initializedClients[$service] = [
                        'status' => 'failed',
                        'error' => $e->getMessage()
                    ];
                }
            }
            
            $this->logOperation('aws_clients_initialized', '', [
                'services' => $servicesToInit,
                'results' => $initializedClients
            ]);
            
            return $initializedClients;

        } catch (\Exception $e) {
            $this->logOperation('aws_client_init_error', '', ['error' => $e->getMessage()]);
            throw new CloudOperationException("AWS client initialization failed: " . $e->getMessage());
        }
    }

    /**
     * Auto-discover AWS services and resources
     */
    public function discoverAWSServices(): array
    {
        try {
            $discoveryResults = [];
            
            // Discover EC2 instances
            $discoveryResults['ec2'] = $this->discoverEC2Resources();
            
            // Discover S3 buckets
            $discoveryResults['s3'] = $this->discoverS3Resources();
            
            // Discover Lambda functions
            $discoveryResults['lambda'] = $this->discoverLambdaResources();
            
            // Discover RDS instances
            $discoveryResults['rds'] = $this->discoverRDSResources();
            
            // Discover IAM resources
            $discoveryResults['iam'] = $this->discoverIAMResources();
            
            // Apply AI optimization suggestions
            $optimizationSuggestions = $this->optimizationEngine->analyzeAWSResources($discoveryResults);
            $discoveryResults['optimization_suggestions'] = $optimizationSuggestions;
            
            // Store discovered services
            $this->discoveredServices = $discoveryResults;
            
            $this->logOperation('aws_services_discovered', '', [
                'services_count' => count($discoveryResults),
                'total_resources' => $this->countTotalResources($discoveryResults)
            ]);
            
            return $discoveryResults;

        } catch (\Exception $e) {
            $this->logOperation('aws_discovery_error', '', ['error' => $e->getMessage()]);
            throw new CloudOperationException("AWS service discovery failed: " . $e->getMessage());
        }
    }

    /**
     * Deploy Lambda function
     */
    public function deployLambdaFunction(string $functionName, array $config): array
    {
        try {
            $this->validateLambdaConfig($config);
            
            if (!isset($this->awsClients['lambda'])) {
                throw new CloudOperationException("Lambda client not initialized");
            }
            
            $lambdaClient = $this->awsClients['lambda'];
            
            // Check if function exists
            $functionExists = $this->lambdaFunctionExists($functionName);
            
            if ($functionExists) {
                // Update existing function
                $result = $this->updateLambdaFunction($functionName, $config);
                $operation = 'updated';
            } else {
                // Create new function
                $result = $this->createLambdaFunction($functionName, $config);
                $operation = 'created';
            }
            
            // Set up CloudWatch monitoring if requested
            if ($config['enable_monitoring'] ?? true) {
                $this->setupLambdaMonitoring($functionName, $config['monitoring_config'] ?? []);
            }
            
            // Apply security configurations
            if ($config['apply_security'] ?? true) {
                $this->securityManager->applyLambdaSecurity($functionName, $config['security_config'] ?? []);
            }
            
            $deploymentResult = [
                'function_name' => $functionName,
                'operation' => $operation,
                'function_arn' => $result['FunctionArn'],
                'version' => $result['Version'],
                'code_size' => $result['CodeSize'],
                'timeout' => $result['Timeout'],
                'memory_size' => $result['MemorySize'],
                'runtime' => $result['Runtime'],
                'last_modified' => $result['LastModified']
            ];
            
            $this->logOperation('lambda_function_deployed', $functionName, $deploymentResult);
            return $deploymentResult;

        } catch (\Exception $e) {
            $this->logOperation('lambda_deploy_error', $functionName, ['error' => $e->getMessage()]);
            throw new CloudOperationException("Lambda function deployment failed for {$functionName}: " . $e->getMessage());
        }
    }

    /**
     * Manage EC2 instances
     */
    public function manageEC2Instance(string $action, array $config): array
    {
        try {
            if (!isset($this->awsClients['ec2'])) {
                throw new CloudOperationException("EC2 client not initialized");
            }
            
            $ec2Client = $this->awsClients['ec2'];
            
            switch ($action) {
                case 'launch':
                    $result = $this->launchEC2Instance($config);
                    break;
                case 'terminate':
                    $result = $this->terminateEC2Instance($config['instance_id']);
                    break;
                case 'stop':
                    $result = $this->stopEC2Instance($config['instance_id']);
                    break;
                case 'start':
                    $result = $this->startEC2Instance($config['instance_id']);
                    break;
                case 'reboot':
                    $result = $this->rebootEC2Instance($config['instance_id']);
                    break;
                default:
                    throw new CloudOperationException("Unknown EC2 action: {$action}");
            }
            
            $this->logOperation('ec2_instance_managed', $config['instance_id'] ?? 'new', [
                'action' => $action,
                'result' => $result
            ]);
            
            return $result;

        } catch (\Exception $e) {
            $this->logOperation('ec2_management_error', $config['instance_id'] ?? '', [
                'action' => $action,
                'error' => $e->getMessage()
            ]);
            throw new CloudOperationException("EC2 management failed for action {$action}: " . $e->getMessage());
        }
    }

    /**
     * Manage S3 resources
     */
    public function manageS3Resource(string $action, array $config): array
    {
        try {
            if (!isset($this->awsClients['s3'])) {
                throw new CloudOperationException("S3 client not initialized");
            }
            
            $s3Client = $this->awsClients['s3'];
            
            switch ($action) {
                case 'create_bucket':
                    $result = $this->createS3Bucket($config['bucket_name'], $config);
                    break;
                case 'delete_bucket':
                    $result = $this->deleteS3Bucket($config['bucket_name']);
                    break;
                case 'upload_object':
                    $result = $this->uploadS3Object($config['bucket'], $config['key'], $config['body'], $config);
                    break;
                case 'download_object':
                    $result = $this->downloadS3Object($config['bucket'], $config['key'], $config);
                    break;
                case 'delete_object':
                    $result = $this->deleteS3Object($config['bucket'], $config['key']);
                    break;
                case 'list_objects':
                    $result = $this->listS3Objects($config['bucket'], $config);
                    break;
                default:
                    throw new CloudOperationException("Unknown S3 action: {$action}");
            }
            
            $this->logOperation('s3_resource_managed', $config['bucket_name'] ?? $config['bucket'] ?? '', [
                'action' => $action,
                'result' => $result
            ]);
            
            return $result;

        } catch (\Exception $e) {
            $this->logOperation('s3_management_error', $config['bucket_name'] ?? $config['bucket'] ?? '', [
                'action' => $action,
                'error' => $e->getMessage()
            ]);
            throw new CloudOperationException("S3 management failed for action {$action}: " . $e->getMessage());
        }
    }

    /**
     * Monitor CloudWatch metrics
     */
    public function getCloudWatchMetrics(array $config): array
    {
        try {
            if (!isset($this->awsClients['cloudwatch'])) {
                throw new CloudOperationException("CloudWatch client not initialized");
            }
            
            $cloudWatchClient = $this->awsClients['cloudwatch'];
            
            $namespace = $config['namespace'];
            $metricName = $config['metric_name'];
            $dimensions = $config['dimensions'] ?? [];
            $startTime = $config['start_time'] ?? date('c', strtotime('-1 hour'));
            $endTime = $config['end_time'] ?? date('c');
            $period = $config['period'] ?? 300; // 5 minutes
            $statistics = $config['statistics'] ?? ['Average'];
            
            $params = [
                'Namespace' => $namespace,
                'MetricName' => $metricName,
                'Dimensions' => $dimensions,
                'StartTime' => $startTime,
                'EndTime' => $endTime,
                'Period' => $period,
                'Statistics' => $statistics
            ];
            
            $result = $cloudWatchClient->getMetricStatistics($params);
            
            // Process and enhance the results
            $processedMetrics = $this->processCloudWatchMetrics($result['Datapoints']);
            
            $metricsResult = [
                'namespace' => $namespace,
                'metric_name' => $metricName,
                'period' => $period,
                'datapoints' => $processedMetrics,
                'statistics' => $this->calculateMetricStatistics($processedMetrics)
            ];
            
            $this->logOperation('cloudwatch_metrics_retrieved', $metricName, [
                'namespace' => $namespace,
                'datapoints_count' => count($processedMetrics)
            ]);
            
            return $metricsResult;

        } catch (\Exception $e) {
            $this->logOperation('cloudwatch_metrics_error', $config['metric_name'] ?? '', ['error' => $e->getMessage()]);
            throw new CloudOperationException("CloudWatch metrics retrieval failed: " . $e->getMessage());
        }
    }

    /**
     * Optimize AWS costs
     */
    public function optimizeAWSCosts(): array
    {
        try {
            $optimizationResults = [];
            
            // EC2 cost optimization
            $optimizationResults['ec2'] = $this->costOptimizer->optimizeEC2Costs($this->discoveredServices['ec2'] ?? []);
            
            // S3 cost optimization
            $optimizationResults['s3'] = $this->costOptimizer->optimizeS3Costs($this->discoveredServices['s3'] ?? []);
            
            // Lambda cost optimization
            $optimizationResults['lambda'] = $this->costOptimizer->optimizeLambdaCosts($this->discoveredServices['lambda'] ?? []);
            
            // RDS cost optimization
            $optimizationResults['rds'] = $this->costOptimizer->optimizeRDSCosts($this->discoveredServices['rds'] ?? []);
            
            // Calculate potential savings
            $totalSavings = $this->calculateTotalSavings($optimizationResults);
            $optimizationResults['total_potential_savings'] = $totalSavings;
            
            // Apply automatic optimizations if configured
            if ($this->config['auto_apply_optimizations'] ?? false) {
                $appliedOptimizations = $this->applyAutomaticOptimizations($optimizationResults);
                $optimizationResults['applied_optimizations'] = $appliedOptimizations;
            }
            
            $this->logOperation('aws_costs_optimized', '', [
                'services_optimized' => array_keys($optimizationResults),
                'potential_savings' => $totalSavings
            ]);
            
            return $optimizationResults;

        } catch (\Exception $e) {
            $this->logOperation('cost_optimization_error', '', ['error' => $e->getMessage()]);
            throw new CloudOperationException("AWS cost optimization failed: " . $e->getMessage());
        }
    }

    /**
     * Get comprehensive AWS account overview
     */
    public function getAccountOverview(): array
    {
        try {
            $overview = [
                'account_info' => $this->getAccountInfo(),
                'region' => $this->region,
                'services' => $this->discoveredServices,
                'cost_analysis' => $this->getCostAnalysis(),
                'security_status' => $this->securityManager->getSecurityStatus(),
                'recommendations' => $this->optimizationEngine->getRecommendations(),
                'resource_summary' => $this->getResourceSummary()
            ];
            
            $this->logOperation('account_overview_generated', '', [
                'services_count' => count($overview['services']),
                'recommendations_count' => count($overview['recommendations'])
            ]);
            
            return $overview;

        } catch (\Exception $e) {
            $this->logOperation('account_overview_error', '', ['error' => $e->getMessage()]);
            throw new CloudOperationException("Account overview generation failed: " . $e->getMessage());
        }
    }

    // Private helper methods

    private function createAWSClient(string $service): object
    {
        $clientClass = "Aws\\" . ucfirst($service) . "\\" . ucfirst($service) . "Client";
        
        if (!class_exists($clientClass)) {
            throw new CloudOperationException("AWS client class not found: {$clientClass}");
        }
        
        $config = [
            'region' => $this->region,
            'version' => 'latest'
        ];
        
        // Add credentials if provided
        if (!empty($this->credentials)) {
            $config['credentials'] = $this->credentials;
        }
        
        return new $clientClass($config);
    }

    private function discoverEC2Resources(): array
    {
        if (!isset($this->awsClients['ec2'])) {
            return ['error' => 'EC2 client not available'];
        }
        
        $ec2Client = $this->awsClients['ec2'];
        
        try {
            $instances = $ec2Client->describeInstances();
            $volumes = $ec2Client->describeVolumes();
            $securityGroups = $ec2Client->describeSecurityGroups();
            
            return [
                'instances' => $this->processEC2Instances($instances['Reservations']),
                'volumes' => $this->processEC2Volumes($volumes['Volumes']),
                'security_groups' => $this->processSecurityGroups($securityGroups['SecurityGroups'])
            ];
        } catch (\Exception $e) {
            return ['error' => $e->getMessage()];
        }
    }

    private function discoverS3Resources(): array
    {
        if (!isset($this->awsClients['s3'])) {
            return ['error' => 'S3 client not available'];
        }
        
        $s3Client = $this->awsClients['s3'];
        
        try {
            $buckets = $s3Client->listBuckets();
            $processedBuckets = [];
            
            foreach ($buckets['Buckets'] as $bucket) {
                $bucketInfo = [
                    'name' => $bucket['Name'],
                    'created' => $bucket['CreationDate']->format('Y-m-d H:i:s'),
                    'region' => $this->getBucketRegion($bucket['Name']),
                    'versioning' => $this->getBucketVersioning($bucket['Name']),
                    'encryption' => $this->getBucketEncryption($bucket['Name'])
                ];
                $processedBuckets[] = $bucketInfo;
            }
            
            return [
                'buckets' => $processedBuckets,
                'total_buckets' => count($processedBuckets)
            ];
        } catch (\Exception $e) {
            return ['error' => $e->getMessage()];
        }
    }

    private function discoverLambdaResources(): array
    {
        if (!isset($this->awsClients['lambda'])) {
            return ['error' => 'Lambda client not available'];
        }
        
        $lambdaClient = $this->awsClients['lambda'];
        
        try {
            $functions = $lambdaClient->listFunctions();
            $processedFunctions = [];
            
            foreach ($functions['Functions'] as $function) {
                $functionInfo = [
                    'name' => $function['FunctionName'],
                    'arn' => $function['FunctionArn'],
                    'runtime' => $function['Runtime'],
                    'memory_size' => $function['MemorySize'],
                    'timeout' => $function['Timeout'],
                    'code_size' => $function['CodeSize'],
                    'last_modified' => $function['LastModified']
                ];
                $processedFunctions[] = $functionInfo;
            }
            
            return [
                'functions' => $processedFunctions,
                'total_functions' => count($processedFunctions)
            ];
        } catch (\Exception $e) {
            return ['error' => $e->getMessage()];
        }
    }

    private function discoverRDSResources(): array
    {
        if (!isset($this->awsClients['rds'])) {
            return ['error' => 'RDS client not available'];
        }
        
        $rdsClient = $this->awsClients['rds'];
        
        try {
            $instances = $rdsClient->describeDBInstances();
            $processedInstances = [];
            
            foreach ($instances['DBInstances'] as $instance) {
                $instanceInfo = [
                    'identifier' => $instance['DBInstanceIdentifier'],
                    'class' => $instance['DBInstanceClass'],
                    'engine' => $instance['Engine'],
                    'engine_version' => $instance['EngineVersion'],
                    'status' => $instance['DBInstanceStatus'],
                    'allocated_storage' => $instance['AllocatedStorage'],
                    'multi_az' => $instance['MultiAZ'],
                    'publicly_accessible' => $instance['PubliclyAccessible']
                ];
                $processedInstances[] = $instanceInfo;
            }
            
            return [
                'instances' => $processedInstances,
                'total_instances' => count($processedInstances)
            ];
        } catch (\Exception $e) {
            return ['error' => $e->getMessage()];
        }
    }

    private function discoverIAMResources(): array
    {
        if (!isset($this->awsClients['iam'])) {
            return ['error' => 'IAM client not available'];
        }
        
        $iamClient = $this->awsClients['iam'];
        
        try {
            $users = $iamClient->listUsers();
            $roles = $iamClient->listRoles();
            $policies = $iamClient->listPolicies(['Scope' => 'Local']);
            
            return [
                'users' => array_map(fn($u) => [
                    'name' => $u['UserName'],
                    'created' => $u['CreateDate']->format('Y-m-d H:i:s'),
                    'path' => $u['Path']
                ], $users['Users']),
                'roles' => array_map(fn($r) => [
                    'name' => $r['RoleName'],
                    'created' => $r['CreateDate']->format('Y-m-d H:i:s'),
                    'path' => $r['Path']
                ], $roles['Roles']),
                'policies' => array_map(fn($p) => [
                    'name' => $p['PolicyName'],
                    'arn' => $p['Arn'],
                    'created' => $p['CreateDate']->format('Y-m-d H:i:s')
                ], $policies['Policies'])
            ];
        } catch (\Exception $e) {
            return ['error' => $e->getMessage()];
        }
    }

    private function lambdaFunctionExists(string $functionName): bool
    {
        try {
            $this->awsClients['lambda']->getFunction(['FunctionName' => $functionName]);
            return true;
        } catch (\Exception $e) {
            return false;
        }
    }

    private function createLambdaFunction(string $functionName, array $config): array
    {
        $params = [
            'FunctionName' => $functionName,
            'Runtime' => $config['runtime'],
            'Role' => $config['role'],
            'Handler' => $config['handler'],
            'Code' => $config['code'],
            'Description' => $config['description'] ?? '',
            'Timeout' => $config['timeout'] ?? 30,
            'MemorySize' => $config['memory_size'] ?? 128,
            'Environment' => $config['environment'] ?? []
        ];
        
        return $this->awsClients['lambda']->createFunction($params);
    }

    private function updateLambdaFunction(string $functionName, array $config): array
    {
        // Update function code
        if (isset($config['code'])) {
            $this->awsClients['lambda']->updateFunctionCode([
                'FunctionName' => $functionName,
                'ZipFile' => $config['code']['ZipFile'] ?? null,
                'S3Bucket' => $config['code']['S3Bucket'] ?? null,
                'S3Key' => $config['code']['S3Key'] ?? null
            ]);
        }
        
        // Update function configuration
        $configParams = [
            'FunctionName' => $functionName,
            'Runtime' => $config['runtime'] ?? null,
            'Role' => $config['role'] ?? null,
            'Handler' => $config['handler'] ?? null,
            'Description' => $config['description'] ?? null,
            'Timeout' => $config['timeout'] ?? null,
            'MemorySize' => $config['memory_size'] ?? null,
            'Environment' => $config['environment'] ?? null
        ];
        
        // Remove null values
        $configParams = array_filter($configParams, fn($v) => $v !== null);
        
        return $this->awsClients['lambda']->updateFunctionConfiguration($configParams);
    }

    private function validateLambdaConfig(array $config): void
    {
        $required = ['runtime', 'role', 'handler', 'code'];
        
        foreach ($required as $field) {
            if (!isset($config[$field])) {
                throw new CloudOperationException("Required Lambda configuration field missing: {$field}");
            }
        }
    }

    private function countTotalResources(array $discoveryResults): int
    {
        $total = 0;
        foreach ($discoveryResults as $service => $resources) {
            if (is_array($resources)) {
                foreach ($resources as $resourceType => $resourceList) {
                    if (is_array($resourceList)) {
                        $total += count($resourceList);
                    }
                }
            }
        }
        return $total;
    }

    private function initializeOperator(): void
    {
        // Initialize components
        $this->optimizationEngine->initialize();
        $this->securityManager->initialize();
        $this->costOptimizer->initialize();
        
        $this->logOperation('aws_operator_initialized', '', [
            'region' => $this->region,
            'credentials_provided' => !empty($this->credentials)
        ]);
    }

    private function logOperation(string $operation, string $resourceId, array $context = []): void
    {
        $logData = [
            'operation' => $operation,
            'resource_id' => $resourceId,
            'timestamp' => microtime(true),
            'region' => $this->region,
            'context' => $context
        ];
        
        error_log("AWSOperator: " . json_encode($logData));
    }
} 