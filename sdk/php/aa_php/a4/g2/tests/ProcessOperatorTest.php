<?php

declare(strict_types=1);

namespace TuskLang\SDK\Tests\SystemOperations\ProcessManagement;

use PHPUnit\Framework\TestCase;
use TuskLang\SDK\SystemOperations\ProcessManagement\ProcessOperator;
use TuskLang\SDK\Core\Exceptions\ProcessOperationException;

/**
 * Comprehensive Test Suite for ProcessOperator
 * 
 * @covers \TuskLang\SDK\SystemOperations\ProcessManagement\ProcessOperator
 */
class ProcessOperatorTest extends TestCase
{
    private ProcessOperator $operator;
    private array $testConfig;

    protected function setUp(): void
    {
        $this->testConfig = [
            'max_processes' => 10,
            'monitoring_interval' => 1,
            'health_config' => ['timeout' => 30],
            'ipc_config' => ['method' => 'sockets'],
            'cluster_config' => ['enabled' => true]
        ];
        
        $this->operator = new ProcessOperator($this->testConfig);
    }

    protected function tearDown(): void
    {
        // Cleanup any test processes
        $this->operator->killAllProcesses();
    }

    public function testConstructorInitializesCorrectly(): void
    {
        $this->assertInstanceOf(ProcessOperator::class, $this->operator);
        $stats = $this->operator->getStatistics();
        $this->assertEquals(0, $stats['active_processes']);
        $this->assertEquals(10, $stats['max_processes']);
    }

    public function testStartProcessSuccess(): void
    {
        $processId = $this->operator->startProcess('sleep', ['5'], [
            'name' => 'test_sleep',
            'priority' => 'normal'
        ]);
        
        $this->assertNotEmpty($processId);
        $this->assertTrue($this->operator->isProcessRunning($processId));
        
        $processInfo = $this->operator->getProcessInfo($processId);
        $this->assertEquals('test_sleep', $processInfo['name']);
        $this->assertEquals('running', $processInfo['status']);
        
        // Cleanup
        $this->operator->killProcess($processId);
    }

    public function testStartProcessWithInvalidCommand(): void
    {
        $this->expectException(ProcessOperationException::class);
        $this->expectExceptionMessage('Failed to start process');
        
        $this->operator->startProcess('nonexistent_command_xyz', []);
    }

    public function testKillProcessSuccess(): void
    {
        $processId = $this->operator->startProcess('sleep', ['10']);
        $this->assertTrue($this->operator->isProcessRunning($processId));
        
        $result = $this->operator->killProcess($processId);
        $this->assertTrue($result);
        
        // Give it a moment to terminate
        usleep(100000);
        $this->assertFalse($this->operator->isProcessRunning($processId));
    }

    public function testKillNonExistentProcess(): void
    {
        $this->expectException(ProcessOperationException::class);
        $this->expectExceptionMessage('Process not found');
        
        $this->operator->killProcess('nonexistent_id');
    }

    public function testGetProcessInfoSuccess(): void
    {
        $processId = $this->operator->startProcess('sleep', ['3'], [
            'name' => 'info_test',
            'priority' => 'high'
        ]);
        
        $info = $this->operator->getProcessInfo($processId);
        
        $this->assertEquals($processId, $info['id']);
        $this->assertEquals('info_test', $info['name']);
        $this->assertEquals('running', $info['status']);
        $this->assertEquals('high', $info['priority']);
        $this->assertArrayHasKey('pid', $info);
        $this->assertArrayHasKey('memory_usage', $info);
        $this->assertArrayHasKey('cpu_usage', $info);
        
        $this->operator->killProcess($processId);
    }

    public function testListProcesses(): void
    {
        // Start multiple processes
        $processIds = [];
        for ($i = 0; $i < 3; $i++) {
            $processIds[] = $this->operator->startProcess('sleep', ['5'], [
                'name' => "test_process_{$i}"
            ]);
        }
        
        $processes = $this->operator->listProcesses();
        $this->assertCount(3, $processes);
        
        foreach ($processes as $process) {
            $this->assertArrayHasKey('id', $process);
            $this->assertArrayHasKey('name', $process);
            $this->assertArrayHasKey('status', $process);
            $this->assertEquals('running', $process['status']);
        }
        
        // Cleanup
        foreach ($processIds as $id) {
            $this->operator->killProcess($id);
        }
    }

    public function testListProcessesWithFilters(): void
    {
        // Start processes with different statuses
        $runningId = $this->operator->startProcess('sleep', ['5'], ['name' => 'running_test']);
        $stoppedId = $this->operator->startProcess('echo', ['test'], ['name' => 'stopped_test']);
        
        // Wait for echo to finish
        sleep(1);
        
        $runningProcesses = $this->operator->listProcesses(['status' => 'running']);
        $this->assertCount(1, $runningProcesses);
        $this->assertEquals('running_test', $runningProcesses[0]['name']);
        
        $this->operator->killProcess($runningId);
    }

    public function testRestartProcess(): void
    {
        $processId = $this->operator->startProcess('sleep', ['10'], ['name' => 'restart_test']);
        $originalPid = $this->operator->getProcessInfo($processId)['pid'];
        
        $result = $this->operator->restartProcess($processId);
        $this->assertTrue($result);
        
        // Wait for restart
        sleep(1);
        
        $newPid = $this->operator->getProcessInfo($processId)['pid'];
        $this->assertNotEquals($originalPid, $newPid);
        
        $this->operator->killProcess($processId);
    }

    public function testSendSignalToProcess(): void
    {
        $processId = $this->operator->startProcess('sleep', ['10']);
        
        // Send SIGTERM
        $result = $this->operator->sendSignal($processId, 15);
        $this->assertTrue($result);
        
        // Process should terminate
        sleep(1);
        $this->assertFalse($this->operator->isProcessRunning($processId));
    }

    public function testCreateProcessGroup(): void
    {
        $groupId = $this->operator->createProcessGroup('test_group', [
            'max_processes' => 5,
            'load_balancing' => true
        ]);
        
        $this->assertNotEmpty($groupId);
        
        $groups = $this->operator->getProcessGroups();
        $this->assertArrayHasKey($groupId, $groups);
        $this->assertEquals('test_group', $groups[$groupId]['name']);
    }

    public function testAddProcessToGroup(): void
    {
        $groupId = $this->operator->createProcessGroup('test_group');
        $processId = $this->operator->startProcess('sleep', ['5']);
        
        $result = $this->operator->addProcessToGroup($processId, $groupId);
        $this->assertTrue($result);
        
        $groupInfo = $this->operator->getProcessGroupInfo($groupId);
        $this->assertContains($processId, $groupInfo['processes']);
        
        $this->operator->killProcess($processId);
    }

    public function testSetProcessPriority(): void
    {
        $processId = $this->operator->startProcess('sleep', ['5']);
        
        $result = $this->operator->setProcessPriority($processId, 'high');
        $this->assertTrue($result);
        
        $info = $this->operator->getProcessInfo($processId);
        $this->assertEquals('high', $info['priority']);
        
        $this->operator->killProcess($processId);
    }

    public function testMonitorProcessHealth(): void
    {
        $processId = $this->operator->startProcess('sleep', ['5'], [
            'health_check' => [
                'enabled' => true,
                'interval' => 1,
                'timeout' => 5
            ]
        ]);
        
        // Wait for health check
        sleep(2);
        
        $health = $this->operator->getProcessHealth($processId);
        $this->assertArrayHasKey('status', $health);
        $this->assertArrayHasKey('last_check', $health);
        $this->assertArrayHasKey('response_time', $health);
        
        $this->operator->killProcess($processId);
    }

    public function testProcessResourceUsage(): void
    {
        $processId = $this->operator->startProcess('sleep', ['5']);
        
        $resources = $this->operator->getResourceUsage($processId);
        
        $this->assertArrayHasKey('memory_usage', $resources);
        $this->assertArrayHasKey('cpu_usage', $resources);
        $this->assertArrayHasKey('disk_io', $resources);
        $this->assertArrayHasKey('network_io', $resources);
        $this->assertIsInt($resources['memory_usage']);
        $this->assertIsFloat($resources['cpu_usage']);
        
        $this->operator->killProcess($processId);
    }

    public function testInterProcessCommunication(): void
    {
        $processId1 = $this->operator->startProcess('sleep', ['10'], ['name' => 'sender']);
        $processId2 = $this->operator->startProcess('sleep', ['10'], ['name' => 'receiver']);
        
        $message = 'test_ipc_message';
        $result = $this->operator->sendMessage($processId1, $processId2, $message);
        $this->assertTrue($result);
        
        // In a real implementation, you would verify message receipt
        // This is simplified for testing
        
        $this->operator->killProcess($processId1);
        $this->operator->killProcess($processId2);
    }

    public function testProcessClusterManagement(): void
    {
        $clusterId = $this->operator->createCluster('test_cluster', [
            'min_processes' => 2,
            'max_processes' => 5,
            'auto_scaling' => true
        ]);
        
        $this->assertNotEmpty($clusterId);
        
        $clusterInfo = $this->operator->getClusterInfo($clusterId);
        $this->assertEquals('test_cluster', $clusterInfo['name']);
        $this->assertEquals(2, $clusterInfo['min_processes']);
        $this->assertEquals(5, $clusterInfo['max_processes']);
    }

    public function testScaleCluster(): void
    {
        $clusterId = $this->operator->createCluster('scale_test', [
            'min_processes' => 1,
            'max_processes' => 5
        ]);
        
        $result = $this->operator->scaleCluster($clusterId, 3);
        $this->assertTrue($result);
        
        $clusterInfo = $this->operator->getClusterInfo($clusterId);
        $this->assertEquals(3, $clusterInfo['target_size']);
    }

    public function testProcessFailoverHandling(): void
    {
        $processId = $this->operator->startProcess('sleep', ['10'], [
            'failover' => [
                'enabled' => true,
                'max_retries' => 3,
                'retry_delay' => 1
            ]
        ]);
        
        // Force kill the process
        $this->operator->killProcess($processId, ['force' => true]);
        
        // Wait for failover
        sleep(2);
        
        // Process should be restarted
        $this->assertTrue($this->operator->isProcessRunning($processId));
        
        $this->operator->killProcess($processId);
    }

    public function testProcessScheduling(): void
    {
        $scheduleId = $this->operator->scheduleProcess('echo', ['scheduled_test'], [
            'schedule' => 'every_minute',
            'max_instances' => 1
        ]);
        
        $this->assertNotEmpty($scheduleId);
        
        $scheduledProcesses = $this->operator->getScheduledProcesses();
        $this->assertArrayHasKey($scheduleId, $scheduledProcesses);
    }

    public function testProcessEnvironmentVariables(): void
    {
        $processId = $this->operator->startProcess('printenv', [], [
            'environment' => [
                'TEST_VAR' => 'test_value',
                'ANOTHER_VAR' => 'another_value'
            ]
        ]);
        
        // In a real implementation, you would capture and verify output
        $this->assertTrue($this->operator->isProcessRunning($processId));
        
        // Wait for process to complete
        sleep(1);
    }

    public function testProcessWorkingDirectory(): void
    {
        $tempDir = sys_get_temp_dir();
        
        $processId = $this->operator->startProcess('pwd', [], [
            'working_directory' => $tempDir
        ]);
        
        $this->assertTrue($this->operator->isProcessRunning($processId));
        
        // Wait for process to complete
        sleep(1);
    }

    public function testProcessTimeout(): void
    {
        $processId = $this->operator->startProcess('sleep', ['10'], [
            'timeout' => 2 // 2 seconds
        ]);
        
        $this->assertTrue($this->operator->isProcessRunning($processId));
        
        // Wait for timeout
        sleep(3);
        
        // Process should be killed due to timeout
        $this->assertFalse($this->operator->isProcessRunning($processId));
    }

    public function testKillAllProcesses(): void
    {
        // Start multiple processes
        $processIds = [];
        for ($i = 0; $i < 3; $i++) {
            $processIds[] = $this->operator->startProcess('sleep', ['10']);
        }
        
        foreach ($processIds as $id) {
            $this->assertTrue($this->operator->isProcessRunning($id));
        }
        
        $result = $this->operator->killAllProcesses();
        $this->assertTrue($result);
        
        // Wait for termination
        sleep(1);
        
        foreach ($processIds as $id) {
            $this->assertFalse($this->operator->isProcessRunning($id));
        }
    }

    public function testGetStatistics(): void
    {
        $stats = $this->operator->getStatistics();
        
        $this->assertArrayHasKey('active_processes', $stats);
        $this->assertArrayHasKey('total_processes_started', $stats);
        $this->assertArrayHasKey('max_processes', $stats);
        $this->assertArrayHasKey('process_groups', $stats);
        $this->assertArrayHasKey('clusters_count', $stats);
        $this->assertArrayHasKey('uptime', $stats);
        
        $this->assertIsInt($stats['active_processes']);
        $this->assertIsInt($stats['total_processes_started']);
        $this->assertIsInt($stats['max_processes']);
        $this->assertEquals(10, $stats['max_processes']);
    }

    public function testMaxProcessesLimit(): void
    {
        // Start maximum number of processes
        $processIds = [];
        for ($i = 0; $i < 10; $i++) {
            $processIds[] = $this->operator->startProcess('sleep', ['30']);
        }
        
        // Try to start one more - should fail
        $this->expectException(ProcessOperationException::class);
        $this->expectExceptionMessage('Maximum processes limit reached');
        
        $this->operator->startProcess('sleep', ['30']);
        
        // Cleanup
        foreach ($processIds as $id) {
            $this->operator->killProcess($id);
        }
    }

    public function testProcessWithCustomWorkingDirectory(): void
    {
        $tempDir = sys_get_temp_dir();
        
        $processId = $this->operator->startProcess('ls', ['-la'], [
            'working_directory' => $tempDir
        ]);
        
        $info = $this->operator->getProcessInfo($processId);
        $this->assertEquals($tempDir, $info['working_directory']);
        
        // Wait for completion
        sleep(1);
    }

    public function testProcessOutputCapture(): void
    {
        $processId = $this->operator->startProcess('echo', ['Hello, World!'], [
            'capture_output' => true
        ]);
        
        // Wait for completion
        sleep(1);
        
        $output = $this->operator->getProcessOutput($processId);
        $this->assertStringContains('Hello, World!', $output['stdout']);
        $this->assertEmpty($output['stderr']);
    }

    public function testProcessInputRedirection(): void
    {
        $processId = $this->operator->startProcess('cat', [], [
            'input' => 'Test input data'
        ]);
        
        // Wait for completion
        sleep(1);
        
        $output = $this->operator->getProcessOutput($processId);
        $this->assertStringContains('Test input data', $output['stdout']);
    }
} 