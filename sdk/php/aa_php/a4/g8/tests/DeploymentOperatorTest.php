<?php

declare(strict_types=1);

namespace TuskLang\SDK\Tests\SystemOperations\Deployment;

use PHPUnit\Framework\TestCase;
use TuskLang\SDK\SystemOperations\Deployment\DeploymentOperator;
use TuskLang\SDK\Core\Exceptions\DeploymentOperationException;

/**
 * Comprehensive Test Suite for DeploymentOperator
 * 
 * @covers \TuskLang\SDK\SystemOperations\Deployment\DeploymentOperator
 */
class DeploymentOperatorTest extends TestCase
{
    private DeploymentOperator $operator;
    private array $testConfig;

    protected function setUp(): void
    {
        $this->testConfig = [
            'auto_rollback' => true,
            'health_check_timeout' => 30,
            'deployment_timeout' => 300,
            'strategies' => ['blue-green', 'canary', 'rolling']
        ];
        
        $this->operator = new DeploymentOperator($this->testConfig);
    }

    protected function tearDown(): void
    {
        // Cleanup any test deployments
        $this->operator->cleanup();
    }

    public function testConstructorInitializesCorrectly(): void
    {
        $this->assertInstanceOf(DeploymentOperator::class, $this->operator);
        $stats = $this->operator->getStatistics();
        $this->assertArrayHasKey('total_deployments', $stats);
        $this->assertEquals(0, $stats['total_deployments']);
    }

    public function testDeployApplicationBlueGreen(): void
    {
        $deploymentId = $this->operator->deployApplication('test-app', 'v1.0.0', [
            'strategy' => 'blue-green',
            'environment' => 'staging',
            'replicas' => 3,
            'health_check' => [
                'path' => '/health',
                'timeout' => 30
            ]
        ]);
        
        $this->assertNotEmpty($deploymentId);
        
        $status = $this->operator->getDeploymentStatus($deploymentId);
        $this->assertArrayHasKey('id', $status);
        $this->assertArrayHasKey('strategy', $status);
        $this->assertArrayHasKey('status', $status);
        $this->assertEquals('blue-green', $status['strategy']);
        $this->assertEquals($deploymentId, $status['id']);
    }

    public function testDeployApplicationCanary(): void
    {
        $deploymentId = $this->operator->deployApplication('test-app', 'v2.0.0', [
            'strategy' => 'canary',
            'environment' => 'production',
            'canary_percentage' => 10,
            'promotion_steps' => [10, 25, 50, 100]
        ]);
        
        $this->assertNotEmpty($deploymentId);
        
        $status = $this->operator->getDeploymentStatus($deploymentId);
        $this->assertEquals('canary', $status['strategy']);
        $this->assertArrayHasKey('canary_percentage', $status);
    }

    public function testDeployApplicationRolling(): void
    {
        $deploymentId = $this->operator->deployApplication('test-app', 'v3.0.0', [
            'strategy' => 'rolling',
            'environment' => 'production',
            'batch_size' => 2,
            'max_unavailable' => 1
        ]);
        
        $this->assertNotEmpty($deploymentId);
        
        $status = $this->operator->getDeploymentStatus($deploymentId);
        $this->assertEquals('rolling', $status['strategy']);
        $this->assertArrayHasKey('batch_size', $status);
    }

    public function testDeployApplicationWithInvalidStrategy(): void
    {
        $this->expectException(DeploymentOperationException::class);
        $this->expectExceptionMessage('Unsupported deployment strategy');
        
        $this->operator->deployApplication('test-app', 'v1.0.0', [
            'strategy' => 'invalid-strategy'
        ]);
    }

    public function testRollbackDeployment(): void
    {
        $deploymentId = $this->operator->deployApplication('test-app', 'v1.0.0', [
            'strategy' => 'blue-green',
            'environment' => 'staging'
        ]);
        
        $rollbackId = $this->operator->rollbackDeployment($deploymentId, 'Testing rollback functionality');
        
        $this->assertNotEmpty($rollbackId);
        
        $rollbackStatus = $this->operator->getRollbackStatus($rollbackId);
        $this->assertArrayHasKey('id', $rollbackStatus);
        $this->assertArrayHasKey('reason', $rollbackStatus);
        $this->assertArrayHasKey('status', $rollbackStatus);
        $this->assertEquals('Testing rollback functionality', $rollbackStatus['reason']);
    }

    public function testRollbackNonExistentDeployment(): void
    {
        $this->expectException(DeploymentOperationException::class);
        $this->expectExceptionMessage('Deployment not found');
        
        $this->operator->rollbackDeployment('nonexistent-deployment', 'Test rollback');
    }

    public function testGetDeploymentStatus(): void
    {
        $deploymentId = $this->operator->deployApplication('test-app', 'v1.0.0', [
            'strategy' => 'blue-green'
        ]);
        
        $status = $this->operator->getDeploymentStatus($deploymentId);
        
        $this->assertIsArray($status);
        $this->assertArrayHasKey('id', $status);
        $this->assertArrayHasKey('application', $status);
        $this->assertArrayHasKey('version', $status);
        $this->assertArrayHasKey('strategy', $status);
        $this->assertArrayHasKey('status', $status);
        $this->assertArrayHasKey('created_at', $status);
        
        $this->assertEquals($deploymentId, $status['id']);
        $this->assertEquals('test-app', $status['application']);
        $this->assertEquals('v1.0.0', $status['version']);
    }

    public function testListDeployments(): void
    {
        // Create multiple deployments
        $deployment1 = $this->operator->deployApplication('app1', 'v1.0.0', ['strategy' => 'blue-green']);
        $deployment2 = $this->operator->deployApplication('app2', 'v2.0.0', ['strategy' => 'canary']);
        $deployment3 = $this->operator->deployApplication('app3', 'v3.0.0', ['strategy' => 'rolling']);
        
        $deployments = $this->operator->listDeployments();
        
        $this->assertIsArray($deployments);
        $this->assertCount(3, $deployments);
        
        $deploymentIds = array_column($deployments, 'id');
        $this->assertContains($deployment1, $deploymentIds);
        $this->assertContains($deployment2, $deploymentIds);
        $this->assertContains($deployment3, $deploymentIds);
    }

    public function testListDeploymentsWithFilters(): void
    {
        $this->operator->deployApplication('app1', 'v1.0.0', ['strategy' => 'blue-green']);
        $this->operator->deployApplication('app2', 'v2.0.0', ['strategy' => 'canary']);
        $this->operator->deployApplication('app3', 'v3.0.0', ['strategy' => 'blue-green']);
        
        $blueGreenDeployments = $this->operator->listDeployments(['strategy' => 'blue-green']);
        $this->assertCount(2, $blueGreenDeployments);
        
        $canaryDeployments = $this->operator->listDeployments(['strategy' => 'canary']);
        $this->assertCount(1, $canaryDeployments);
    }

    public function testCreateDeploymentPlan(): void
    {
        $planId = $this->operator->createDeploymentPlan('test-plan', [
            'applications' => [
                ['name' => 'frontend', 'version' => 'v1.0.0'],
                ['name' => 'backend', 'version' => 'v2.0.0'],
                ['name' => 'database', 'version' => 'v1.5.0']
            ],
            'strategy' => 'blue-green',
            'orchestration' => 'sequential'
        ]);
        
        $this->assertNotEmpty($planId);
        
        $plan = $this->operator->getDeploymentPlan($planId);
        $this->assertArrayHasKey('id', $plan);
        $this->assertArrayHasKey('name', $plan);
        $this->assertArrayHasKey('applications', $plan);
        $this->assertEquals('test-plan', $plan['name']);
        $this->assertCount(3, $plan['applications']);
    }

    public function testExecuteDeploymentPlan(): void
    {
        $planId = $this->operator->createDeploymentPlan('execute-test', [
            'applications' => [
                ['name' => 'app1', 'version' => 'v1.0.0'],
                ['name' => 'app2', 'version' => 'v2.0.0']
            ],
            'strategy' => 'rolling'
        ]);
        
        $executionId = $this->operator->executeDeploymentPlan($planId, [
            'environment' => 'staging',
            'dry_run' => true
        ]);
        
        $this->assertNotEmpty($executionId);
        
        $execution = $this->operator->getDeploymentPlanExecution($executionId);
        $this->assertArrayHasKey('id', $execution);
        $this->assertArrayHasKey('plan_id', $execution);
        $this->assertArrayHasKey('status', $execution);
        $this->assertEquals($planId, $execution['plan_id']);
    }

    public function testHealthCheck(): void
    {
        $deploymentId = $this->operator->deployApplication('test-app', 'v1.0.0', [
            'strategy' => 'blue-green',
            'health_check' => [
                'enabled' => true,
                'path' => '/health',
                'timeout' => 30,
                'retries' => 3
            ]
        ]);
        
        $healthStatus = $this->operator->performHealthCheck($deploymentId);
        
        $this->assertIsArray($healthStatus);
        $this->assertArrayHasKey('status', $healthStatus);
        $this->assertArrayHasKey('checks', $healthStatus);
        $this->assertArrayHasKey('timestamp', $healthStatus);
        
        $this->assertContains($healthStatus['status'], ['healthy', 'unhealthy', 'unknown']);
    }

    public function testValidateDeployment(): void
    {
        $validationResult = $this->operator->validateDeployment([
            'application' => 'test-app',
            'version' => 'v1.0.0',
            'strategy' => 'blue-green',
            'environment' => 'production'
        ]);
        
        $this->assertIsArray($validationResult);
        $this->assertArrayHasKey('valid', $validationResult);
        $this->assertArrayHasKey('errors', $validationResult);
        $this->assertArrayHasKey('warnings', $validationResult);
        
        $this->assertIsBool($validationResult['valid']);
        $this->assertIsArray($validationResult['errors']);
        $this->assertIsArray($validationResult['warnings']);
    }

    public function testValidateDeploymentWithErrors(): void
    {
        $validationResult = $this->operator->validateDeployment([
            'application' => '',  // Invalid: empty application name
            'version' => 'invalid-version',
            'strategy' => 'nonexistent-strategy'
        ]);
        
        $this->assertFalse($validationResult['valid']);
        $this->assertNotEmpty($validationResult['errors']);
    }

    public function testCreateSnapshot(): void
    {
        $deploymentId = $this->operator->deployApplication('test-app', 'v1.0.0', [
            'strategy' => 'blue-green'
        ]);
        
        $snapshotId = $this->operator->createSnapshot($deploymentId, [
            'name' => 'pre-deployment-snapshot',
            'description' => 'Snapshot before deployment'
        ]);
        
        $this->assertNotEmpty($snapshotId);
        
        $snapshot = $this->operator->getSnapshot($snapshotId);
        $this->assertArrayHasKey('id', $snapshot);
        $this->assertArrayHasKey('name', $snapshot);
        $this->assertArrayHasKey('deployment_id', $snapshot);
        $this->assertEquals($deploymentId, $snapshot['deployment_id']);
        $this->assertEquals('pre-deployment-snapshot', $snapshot['name']);
    }

    public function testRestoreFromSnapshot(): void
    {
        $deploymentId = $this->operator->deployApplication('test-app', 'v1.0.0', [
            'strategy' => 'blue-green'
        ]);
        
        $snapshotId = $this->operator->createSnapshot($deploymentId, ['name' => 'test-snapshot']);
        
        $restoreId = $this->operator->restoreFromSnapshot($snapshotId, [
            'environment' => 'staging'
        ]);
        
        $this->assertNotEmpty($restoreId);
        
        $restoreStatus = $this->operator->getRestoreStatus($restoreId);
        $this->assertArrayHasKey('id', $restoreStatus);
        $this->assertArrayHasKey('snapshot_id', $restoreStatus);
        $this->assertArrayHasKey('status', $restoreStatus);
    }

    public function testScaleDeployment(): void
    {
        $deploymentId = $this->operator->deployApplication('test-app', 'v1.0.0', [
            'strategy' => 'rolling',
            'replicas' => 3
        ]);
        
        $result = $this->operator->scaleDeployment($deploymentId, 5);
        $this->assertTrue($result);
        
        $status = $this->operator->getDeploymentStatus($deploymentId);
        $this->assertEquals(5, $status['target_replicas']);
    }

    public function testPauseDeployment(): void
    {
        $deploymentId = $this->operator->deployApplication('test-app', 'v1.0.0', [
            'strategy' => 'canary'
        ]);
        
        $result = $this->operator->pauseDeployment($deploymentId);
        $this->assertTrue($result);
        
        $status = $this->operator->getDeploymentStatus($deploymentId);
        $this->assertEquals('paused', $status['status']);
    }

    public function testResumeDeployment(): void
    {
        $deploymentId = $this->operator->deployApplication('test-app', 'v1.0.0', [
            'strategy' => 'canary'
        ]);
        
        $this->operator->pauseDeployment($deploymentId);
        $result = $this->operator->resumeDeployment($deploymentId);
        $this->assertTrue($result);
        
        $status = $this->operator->getDeploymentStatus($deploymentId);
        $this->assertEquals('running', $status['status']);
    }

    public function testGetDeploymentLogs(): void
    {
        $deploymentId = $this->operator->deployApplication('test-app', 'v1.0.0', [
            'strategy' => 'blue-green'
        ]);
        
        $logs = $this->operator->getDeploymentLogs($deploymentId);
        
        $this->assertIsArray($logs);
        $this->assertArrayHasKey('logs', $logs);
        $this->assertArrayHasKey('total', $logs);
        $this->assertIsArray($logs['logs']);
        $this->assertIsInt($logs['total']);
    }

    public function testGetDeploymentMetrics(): void
    {
        $deploymentId = $this->operator->deployApplication('test-app', 'v1.0.0', [
            'strategy' => 'blue-green'
        ]);
        
        $metrics = $this->operator->getDeploymentMetrics($deploymentId);
        
        $this->assertIsArray($metrics);
        $this->assertArrayHasKey('cpu_usage', $metrics);
        $this->assertArrayHasKey('memory_usage', $metrics);
        $this->assertArrayHasKey('network_io', $metrics);
        $this->assertArrayHasKey('disk_io', $metrics);
        $this->assertArrayHasKey('request_rate', $metrics);
        $this->assertArrayHasKey('error_rate', $metrics);
    }

    public function testConfigureAutoScaling(): void
    {
        $deploymentId = $this->operator->deployApplication('test-app', 'v1.0.0', [
            'strategy' => 'rolling',
            'replicas' => 3
        ]);
        
        $result = $this->operator->configureAutoScaling($deploymentId, [
            'min_replicas' => 2,
            'max_replicas' => 10,
            'target_cpu_utilization' => 70,
            'scale_up_cooldown' => 300,
            'scale_down_cooldown' => 600
        ]);
        
        $this->assertTrue($result);
        
        $autoScalingConfig = $this->operator->getAutoScalingConfig($deploymentId);
        $this->assertArrayHasKey('min_replicas', $autoScalingConfig);
        $this->assertArrayHasKey('max_replicas', $autoScalingConfig);
        $this->assertEquals(2, $autoScalingConfig['min_replicas']);
        $this->assertEquals(10, $autoScalingConfig['max_replicas']);
    }

    public function testSetDeploymentEnvironmentVariables(): void
    {
        $deploymentId = $this->operator->deployApplication('test-app', 'v1.0.0', [
            'strategy' => 'blue-green'
        ]);
        
        $envVars = [
            'DATABASE_URL' => 'postgresql://localhost:5432/testdb',
            'REDIS_URL' => 'redis://localhost:6379',
            'LOG_LEVEL' => 'debug'
        ];
        
        $result = $this->operator->setEnvironmentVariables($deploymentId, $envVars);
        $this->assertTrue($result);
        
        $currentEnvVars = $this->operator->getEnvironmentVariables($deploymentId);
        $this->assertArrayHasKey('DATABASE_URL', $currentEnvVars);
        $this->assertArrayHasKey('REDIS_URL', $currentEnvVars);
        $this->assertArrayHasKey('LOG_LEVEL', $currentEnvVars);
        $this->assertEquals('debug', $currentEnvVars['LOG_LEVEL']);
    }

    public function testDeploymentRiskAssessment(): void
    {
        $riskAssessment = $this->operator->assessDeploymentRisk('test-app', 'v2.0.0', [
            'strategy' => 'blue-green',
            'environment' => 'production',
            'breaking_changes' => true,
            'database_migrations' => true
        ]);
        
        $this->assertIsArray($riskAssessment);
        $this->assertArrayHasKey('risk_level', $riskAssessment);
        $this->assertArrayHasKey('risk_score', $riskAssessment);
        $this->assertArrayHasKey('risk_factors', $riskAssessment);
        $this->assertArrayHasKey('mitigation_recommendations', $riskAssessment);
        
        $this->assertContains($riskAssessment['risk_level'], ['low', 'medium', 'high', 'critical']);
        $this->assertIsFloat($riskAssessment['risk_score']);
        $this->assertIsArray($riskAssessment['risk_factors']);
        $this->assertIsArray($riskAssessment['mitigation_recommendations']);
    }

    public function testGenerateDeploymentReport(): void
    {
        $deploymentId = $this->operator->deployApplication('test-app', 'v1.0.0', [
            'strategy' => 'blue-green'
        ]);
        
        $report = $this->operator->generateDeploymentReport($deploymentId, [
            'include_metrics' => true,
            'include_logs' => true,
            'include_health_checks' => true
        ]);
        
        $this->assertIsArray($report);
        $this->assertArrayHasKey('deployment_info', $report);
        $this->assertArrayHasKey('execution_summary', $report);
        $this->assertArrayHasKey('performance_metrics', $report);
        $this->assertArrayHasKey('health_checks', $report);
        $this->assertArrayHasKey('recommendations', $report);
    }

    public function testCleanupFailedDeployments(): void
    {
        // This would create some failed deployments in a real scenario
        $cleaned = $this->operator->cleanupFailedDeployments([
            'older_than_days' => 7,
            'keep_snapshots' => true
        ]);
        
        $this->assertIsInt($cleaned);
        $this->assertGreaterThanOrEqual(0, $cleaned);
    }

    public function testGetOperatorStatistics(): void
    {
        // Create some deployments for statistics
        $this->operator->deployApplication('app1', 'v1.0.0', ['strategy' => 'blue-green']);
        $this->operator->deployApplication('app2', 'v2.0.0', ['strategy' => 'canary']);
        
        $stats = $this->operator->getStatistics();
        
        $this->assertArrayHasKey('total_deployments', $stats);
        $this->assertArrayHasKey('active_deployments', $stats);
        $this->assertArrayHasKey('successful_deployments', $stats);
        $this->assertArrayHasKey('failed_deployments', $stats);
        $this->assertArrayHasKey('rollback_count', $stats);
        $this->assertArrayHasKey('deployment_strategies', $stats);
        $this->assertArrayHasKey('uptime', $stats);
        
        $this->assertIsInt($stats['total_deployments']);
        $this->assertIsInt($stats['active_deployments']);
        $this->assertIsInt($stats['successful_deployments']);
        $this->assertIsInt($stats['failed_deployments']);
        $this->assertEquals(2, $stats['total_deployments']);
    }

    public function testDeploymentWithCustomHealthCheck(): void
    {
        $deploymentId = $this->operator->deployApplication('test-app', 'v1.0.0', [
            'strategy' => 'canary',
            'health_check' => [
                'type' => 'http',
                'endpoint' => 'http://localhost:8080/health',
                'method' => 'GET',
                'expected_status' => 200,
                'timeout' => 10,
                'interval' => 30,
                'retries' => 3
            ]
        ]);
        
        $this->assertNotEmpty($deploymentId);
        
        $healthStatus = $this->operator->performHealthCheck($deploymentId);
        $this->assertArrayHasKey('endpoint', $healthStatus['checks']);
    }

    public function testDeploymentTimeout(): void
    {
        $this->expectException(DeploymentOperationException::class);
        $this->expectExceptionMessage('Deployment timeout');
        
        // This would simulate a deployment that takes too long
        $this->operator->deployApplication('slow-app', 'v1.0.0', [
            'strategy' => 'blue-green',
            'timeout' => 1 // 1 second timeout
        ]);
    }

    public function testParallelDeployments(): void
    {
        $deployments = $this->operator->deployApplications([
            ['name' => 'app1', 'version' => 'v1.0.0', 'strategy' => 'blue-green'],
            ['name' => 'app2', 'version' => 'v2.0.0', 'strategy' => 'canary'],
            ['name' => 'app3', 'version' => 'v3.0.0', 'strategy' => 'rolling']
        ], [
            'parallel' => true,
            'max_concurrent' => 2
        ]);
        
        $this->assertIsArray($deployments);
        $this->assertCount(3, $deployments);
        
        foreach ($deployments as $deployment) {
            $this->assertArrayHasKey('id', $deployment);
            $this->assertArrayHasKey('status', $deployment);
        }
    }
} 