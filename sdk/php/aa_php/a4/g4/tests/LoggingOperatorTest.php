<?php

declare(strict_types=1);

namespace TuskLang\SDK\Tests\SystemOperations\Monitoring;

use PHPUnit\Framework\TestCase;
use TuskLang\SDK\SystemOperations\Monitoring\LoggingOperator;
use TuskLang\SDK\Core\Exceptions\LoggingOperationException;

/**
 * Comprehensive Test Suite for LoggingOperator
 * 
 * @covers \TuskLang\SDK\SystemOperations\Monitoring\LoggingOperator
 */
class LoggingOperatorTest extends TestCase
{
    private LoggingOperator $operator;
    private array $testConfig;
    private string $tempLogFile;

    protected function setUp(): void
    {
        $this->tempLogFile = sys_get_temp_dir() . '/test_' . uniqid() . '.log';
        
        $this->testConfig = [
            'handlers' => [
                'file' => [
                    'type' => 'file',
                    'path' => $this->tempLogFile,
                    'level' => 'debug'
                ]
            ],
            'ai_analysis' => [
                'enabled' => true,
                'anomaly_threshold' => 0.8
            ],
            'retention_days' => 7
        ];
        
        $this->operator = new LoggingOperator($this->testConfig);
    }

    protected function tearDown(): void
    {
        if (file_exists($this->tempLogFile)) {
            unlink($this->tempLogFile);
        }
    }

    public function testConstructorInitializesCorrectly(): void
    {
        $this->assertInstanceOf(LoggingOperator::class, $this->operator);
        $stats = $this->operator->getStatistics();
        $this->assertArrayHasKey('handlers_count', $stats);
        $this->assertEquals(1, $stats['handlers_count']);
    }

    public function testLogMessage(): void
    {
        $message = 'Test log message';
        $context = ['user_id' => 123, 'action' => 'test'];
        
        $this->operator->log('info', $message, $context);
        
        $this->assertFileExists($this->tempLogFile);
        $content = file_get_contents($this->tempLogFile);
        $this->assertStringContains($message, $content);
        $this->assertStringContains('info', $content);
    }

    public function testEmergencyLog(): void
    {
        $message = 'Emergency situation detected';
        $context = ['severity' => 'critical'];
        
        $this->operator->emergency($message, $context);
        
        $content = file_get_contents($this->tempLogFile);
        $this->assertStringContains($message, $content);
        $this->assertStringContains('emergency', strtolower($content));
    }

    public function testAlertLog(): void
    {
        $message = 'Alert: System malfunction';
        $context = ['component' => 'database'];
        
        $this->operator->alert($message, $context);
        
        $content = file_get_contents($this->tempLogFile);
        $this->assertStringContains($message, $content);
        $this->assertStringContains('alert', strtolower($content));
    }

    public function testCriticalLog(): void
    {
        $message = 'Critical error occurred';
        $context = ['error_code' => 500];
        
        $this->operator->critical($message, $context);
        
        $content = file_get_contents($this->tempLogFile);
        $this->assertStringContains($message, $content);
        $this->assertStringContains('critical', strtolower($content));
    }

    public function testErrorLog(): void
    {
        $message = 'An error has occurred';
        $context = ['exception' => 'RuntimeException'];
        
        $this->operator->error($message, $context);
        
        $content = file_get_contents($this->tempLogFile);
        $this->assertStringContains($message, $content);
        $this->assertStringContains('error', strtolower($content));
    }

    public function testWarningLog(): void
    {
        $message = 'Warning: Deprecated function used';
        $context = ['function' => 'old_function'];
        
        $this->operator->warning($message, $context);
        
        $content = file_get_contents($this->tempLogFile);
        $this->assertStringContains($message, $content);
        $this->assertStringContains('warning', strtolower($content));
    }

    public function testInfoLog(): void
    {
        $message = 'Information message';
        $context = ['status' => 'success'];
        
        $this->operator->info($message, $context);
        
        $content = file_get_contents($this->tempLogFile);
        $this->assertStringContains($message, $content);
        $this->assertStringContains('info', strtolower($content));
    }

    public function testDebugLog(): void
    {
        $message = 'Debug information';
        $context = ['debug_data' => ['key' => 'value']];
        
        $this->operator->debug($message, $context);
        
        $content = file_get_contents($this->tempLogFile);
        $this->assertStringContains($message, $content);
        $this->assertStringContains('debug', strtolower($content));
    }

    public function testLogWithComplexContext(): void
    {
        $message = 'Complex context test';
        $context = [
            'user' => [
                'id' => 123,
                'name' => 'John Doe',
                'roles' => ['admin', 'user']
            ],
            'request' => [
                'method' => 'POST',
                'url' => '/api/test',
                'headers' => ['Content-Type' => 'application/json']
            ],
            'performance' => [
                'execution_time' => 0.25,
                'memory_usage' => 1024
            ]
        ];
        
        $this->operator->info($message, $context);
        
        $content = file_get_contents($this->tempLogFile);
        $this->assertStringContains($message, $content);
        $this->assertStringContains('John Doe', $content);
        $this->assertStringContains('POST', $content);
    }

    public function testAddHandler(): void
    {
        $handlerId = $this->operator->addHandler('syslog', [
            'type' => 'syslog',
            'facility' => 'user',
            'level' => 'warning'
        ]);
        
        $this->assertNotEmpty($handlerId);
        
        $stats = $this->operator->getStatistics();
        $this->assertEquals(2, $stats['handlers_count']);
    }

    public function testRemoveHandler(): void
    {
        $handlerId = $this->operator->addHandler('test', [
            'type' => 'null',
            'level' => 'debug'
        ]);
        
        $result = $this->operator->removeHandler($handlerId);
        $this->assertTrue($result);
        
        $stats = $this->operator->getStatistics();
        $this->assertEquals(1, $stats['handlers_count']);
    }

    public function testRemoveNonExistentHandler(): void
    {
        $this->expectException(LoggingOperationException::class);
        $this->expectExceptionMessage('Handler not found');
        
        $this->operator->removeHandler('nonexistent_id');
    }

    public function testAddPattern(): void
    {
        $patternId = $this->operator->addPattern('error_pattern', '/ERROR:\s+(.+)/');
        
        $this->assertNotEmpty($patternId);
        
        // Test pattern matching
        $this->operator->error('ERROR: Database connection failed');
        
        $patterns = $this->operator->getMatchedPatterns();
        $this->assertArrayHasKey($patternId, $patterns);
        $this->assertEquals(1, $patterns[$patternId]['matches']);
    }

    public function testRemovePattern(): void
    {
        $patternId = $this->operator->addPattern('test_pattern', '/TEST:\s+(.+)/');
        
        $result = $this->operator->removePattern($patternId);
        $this->assertTrue($result);
    }

    public function testAnalyzeLogs(): void
    {
        // Generate some test logs
        $this->operator->info('User login successful', ['user_id' => 1]);
        $this->operator->warning('High memory usage detected', ['memory' => '85%']);
        $this->operator->error('Database connection timeout', ['timeout' => 30]);
        $this->operator->info('User logout', ['user_id' => 1]);
        
        $analysis = $this->operator->analyzeLogs();
        
        $this->assertArrayHasKey('total_entries', $analysis);
        $this->assertArrayHasKey('by_level', $analysis);
        $this->assertArrayHasKey('patterns_matched', $analysis);
        $this->assertArrayHasKey('trends', $analysis);
        $this->assertEquals(4, $analysis['total_entries']);
        $this->assertEquals(2, $analysis['by_level']['info']);
        $this->assertEquals(1, $analysis['by_level']['warning']);
        $this->assertEquals(1, $analysis['by_level']['error']);
    }

    public function testGetAnomalies(): void
    {
        // Generate normal patterns
        for ($i = 0; $i < 10; $i++) {
            $this->operator->info("Normal operation $i");
        }
        
        // Generate anomalous patterns
        $this->operator->error('Critical system failure - Anomaly');
        $this->operator->emergency('System compromised - Anomaly');
        
        $anomalies = $this->operator->getAnomalies();
        
        $this->assertIsArray($anomalies);
        // In a real implementation, this would contain detected anomalies
        $this->assertArrayHasKey('detected_anomalies', $anomalies);
        $this->assertArrayHasKey('confidence_scores', $anomalies);
    }

    public function testSearchLogs(): void
    {
        // Generate test logs
        $this->operator->info('User John logged in');
        $this->operator->info('User Jane logged in');
        $this->operator->warning('User John failed authentication');
        $this->operator->info('User John logged out');
        
        $results = $this->operator->searchLogs('John', [
            'level' => 'info',
            'limit' => 10
        ]);
        
        $this->assertIsArray($results);
        $this->assertCount(2, $results); // 2 info logs containing 'John'
        
        foreach ($results as $result) {
            $this->assertStringContains('John', $result['message']);
            $this->assertEquals('info', $result['level']);
        }
    }

    public function testSearchLogsWithTimeRange(): void
    {
        $startTime = time();
        
        $this->operator->info('Message 1');
        sleep(1);
        $midTime = time();
        $this->operator->info('Message 2');
        sleep(1);
        $endTime = time();
        
        $results = $this->operator->searchLogs('Message', [
            'from' => $startTime,
            'to' => $midTime
        ]);
        
        $this->assertCount(1, $results);
        $this->assertStringContains('Message 1', $results[0]['message']);
    }

    public function testGetLogStatistics(): void
    {
        // Generate various log levels
        $this->operator->debug('Debug message');
        $this->operator->info('Info message');
        $this->operator->warning('Warning message');
        $this->operator->error('Error message');
        $this->operator->critical('Critical message');
        
        $stats = $this->operator->getLogStatistics();
        
        $this->assertArrayHasKey('total_logs', $stats);
        $this->assertArrayHasKey('by_level', $stats);
        $this->assertArrayHasKey('by_handler', $stats);
        $this->assertArrayHasKey('average_logs_per_minute', $stats);
        
        $this->assertEquals(5, $stats['total_logs']);
        $this->assertEquals(1, $stats['by_level']['debug']);
        $this->assertEquals(1, $stats['by_level']['info']);
        $this->assertEquals(1, $stats['by_level']['warning']);
        $this->assertEquals(1, $stats['by_level']['error']);
        $this->assertEquals(1, $stats['by_level']['critical']);
    }

    public function testRotateLogs(): void
    {
        // Fill log file with content
        for ($i = 0; $i < 100; $i++) {
            $this->operator->info("Log entry $i");
        }
        
        $originalSize = filesize($this->tempLogFile);
        $this->assertGreaterThan(0, $originalSize);
        
        $result = $this->operator->rotateLogs();
        $this->assertTrue($result);
        
        // After rotation, current log should be smaller or reset
        $newSize = filesize($this->tempLogFile);
        $this->assertLessThanOrEqual($originalSize, $newSize);
    }

    public function testCleanupOldLogs(): void
    {
        $cleaned = $this->operator->cleanup();
        $this->assertIsInt($cleaned);
        $this->assertGreaterThanOrEqual(0, $cleaned);
    }

    public function testExportLogs(): void
    {
        // Generate test logs
        $this->operator->info('Export test 1');
        $this->operator->warning('Export test 2');
        $this->operator->error('Export test 3');
        
        $exportFile = sys_get_temp_dir() . '/export_test.json';
        
        $result = $this->operator->exportLogs($exportFile, [
            'format' => 'json',
            'from' => time() - 3600, // Last hour
            'to' => time()
        ]);
        
        $this->assertTrue($result);
        $this->assertFileExists($exportFile);
        
        $exportContent = json_decode(file_get_contents($exportFile), true);
        $this->assertIsArray($exportContent);
        $this->assertCount(3, $exportContent);
        
        // Cleanup
        unlink($exportFile);
    }

    public function testImportLogs(): void
    {
        $importData = [
            [
                'level' => 'info',
                'message' => 'Imported message 1',
                'timestamp' => time() - 100,
                'context' => ['source' => 'import']
            ],
            [
                'level' => 'warning',
                'message' => 'Imported message 2',
                'timestamp' => time() - 50,
                'context' => ['source' => 'import']
            ]
        ];
        
        $importFile = sys_get_temp_dir() . '/import_test.json';
        file_put_contents($importFile, json_encode($importData));
        
        $result = $this->operator->importLogs($importFile);
        $this->assertTrue($result);
        
        // Verify imported logs
        $results = $this->operator->searchLogs('Imported', ['limit' => 10]);
        $this->assertCount(2, $results);
        
        // Cleanup
        unlink($importFile);
    }

    public function testClearLogs(): void
    {
        // Add some logs
        $this->operator->info('Test message 1');
        $this->operator->info('Test message 2');
        
        $this->assertFileExists($this->tempLogFile);
        $this->assertGreaterThan(0, filesize($this->tempLogFile));
        
        $result = $this->operator->clearLogs();
        $this->assertTrue($result);
        
        // File should be empty or very small after clearing
        $this->assertLessThan(100, filesize($this->tempLogFile));
    }

    public function testGetLogLevel(): void
    {
        $level = $this->operator->getLogLevel();
        $this->assertIsString($level);
        $this->assertContains($level, ['emergency', 'alert', 'critical', 'error', 'warning', 'notice', 'info', 'debug']);
    }

    public function testSetLogLevel(): void
    {
        $this->operator->setLogLevel('error');
        $this->assertEquals('error', $this->operator->getLogLevel());
        
        // Test that debug messages are not logged
        $this->operator->debug('This should not be logged');
        $content = file_get_contents($this->tempLogFile);
        $this->assertStringNotContains('This should not be logged', $content);
        
        // Test that error messages are logged
        $this->operator->error('This should be logged');
        $content = file_get_contents($this->tempLogFile);
        $this->assertStringContains('This should be logged', $content);
    }

    public function testInvalidLogLevel(): void
    {
        $this->expectException(LoggingOperationException::class);
        $this->expectExceptionMessage('Invalid log level');
        
        $this->operator->setLogLevel('invalid_level');
    }

    public function testContextSanitization(): void
    {
        $sensitiveContext = [
            'password' => 'secret123',
            'api_key' => 'key_12345',
            'credit_card' => '1234-5678-9012-3456',
            'safe_data' => 'this is safe'
        ];
        
        $this->operator->info('Test with sensitive data', $sensitiveContext);
        
        $content = file_get_contents($this->tempLogFile);
        $this->assertStringNotContains('secret123', $content);
        $this->assertStringNotContains('key_12345', $content);
        $this->assertStringNotContains('1234-5678-9012-3456', $content);
        $this->assertStringContains('this is safe', $content);
    }

    public function testGetOperatorStatistics(): void
    {
        $stats = $this->operator->getStatistics();
        
        $this->assertArrayHasKey('handlers_count', $stats);
        $this->assertArrayHasKey('patterns_count', $stats);
        $this->assertArrayHasKey('total_logs', $stats);
        $this->assertArrayHasKey('logs_per_level', $stats);
        $this->assertArrayHasKey('uptime', $stats);
        
        $this->assertIsInt($stats['handlers_count']);
        $this->assertIsInt($stats['patterns_count']);
        $this->assertIsInt($stats['total_logs']);
        $this->assertIsArray($stats['logs_per_level']);
        $this->assertIsInt($stats['uptime']);
    }

    public function testLogFormattingWithCustomFormat(): void
    {
        $customFormatter = function ($level, $message, $context, $timestamp) {
            return "[{$timestamp}] CUSTOM:{$level} - {$message} - " . json_encode($context) . "\n";
        };
        
        $this->operator->setFormatter($customFormatter);
        $this->operator->info('Custom format test', ['key' => 'value']);
        
        $content = file_get_contents($this->tempLogFile);
        $this->assertStringContains('CUSTOM:info', $content);
        $this->assertStringContains('Custom format test', $content);
        $this->assertStringContains('{"key":"value"}', $content);
    }

    public function testBatchLogging(): void
    {
        $logs = [
            ['level' => 'info', 'message' => 'Batch log 1', 'context' => []],
            ['level' => 'warning', 'message' => 'Batch log 2', 'context' => ['test' => true]],
            ['level' => 'error', 'message' => 'Batch log 3', 'context' => []]
        ];
        
        $result = $this->operator->logBatch($logs);
        $this->assertTrue($result);
        
        $content = file_get_contents($this->tempLogFile);
        $this->assertStringContains('Batch log 1', $content);
        $this->assertStringContains('Batch log 2', $content);
        $this->assertStringContains('Batch log 3', $content);
    }
} 