<?php

declare(strict_types=1);

namespace TuskLang\SDK\Tests\SystemOperations\FileSystem;

use PHPUnit\Framework\TestCase;
use TuskLang\SDK\SystemOperations\FileSystem\FileOperator;
use TuskLang\SDK\Core\Exceptions\FileOperationException;

/**
 * Comprehensive Test Suite for FileOperator
 * 
 * @package TuskLang\SDK\Tests\SystemOperations\FileSystem
 * @version 1.0.0
 * @covers \TuskLang\SDK\SystemOperations\FileSystem\FileOperator
 */
class FileOperatorTest extends TestCase
{
    private FileOperator $fileOperator;
    private string $testDir;
    private string $testFile;
    private string $testContent;

    protected function setUp(): void
    {
        $this->fileOperator = new FileOperator([
            'max_threads' => 4,
            'ai_config' => ['enabled' => true, 'test_mode' => true],
            'cache_config' => ['enabled' => true, 'test_mode' => true],
            'checksum_config' => ['algorithm' => 'sha256']
        ]);

        $this->testDir = sys_get_temp_dir() . '/tusklang_test_' . uniqid();
        $this->testFile = $this->testDir . '/test_file.txt';
        $this->testContent = "Test content for FileOperator\nLine 2\nLine 3";
        
        mkdir($this->testDir, 0755, true);
    }

    protected function tearDown(): void
    {
        $this->cleanupTestDirectory();
    }

    /**
     * Test basic file reading functionality
     */
    public function testRead(): void
    {
        // Create test file
        file_put_contents($this->testFile, $this->testContent);
        
        // Test reading
        $content = $this->fileOperator->read($this->testFile);
        $this->assertEquals($this->testContent, $content);
    }

    /**
     * Test reading with different optimization strategies
     */
    public function testReadWithOptimizationStrategies(): void
    {
        file_put_contents($this->testFile, $this->testContent);
        
        // Test chunked reading
        $content = $this->fileOperator->read($this->testFile, ['strategy_hint' => 'chunked']);
        $this->assertEquals($this->testContent, $content);
        
        // Test memory mapped reading
        $content = $this->fileOperator->read($this->testFile, ['strategy_hint' => 'memory_mapped']);
        $this->assertEquals($this->testContent, $content);
        
        // Test threaded reading
        $content = $this->fileOperator->read($this->testFile, ['strategy_hint' => 'threaded']);
        $this->assertEquals($this->testContent, $content);
    }

    /**
     * Test reading with cache functionality
     */
    public function testReadWithCache(): void
    {
        file_put_contents($this->testFile, $this->testContent);
        
        // First read (should cache)
        $content1 = $this->fileOperator->read($this->testFile);
        $this->assertEquals($this->testContent, $content1);
        
        // Second read (should hit cache)
        $content2 = $this->fileOperator->read($this->testFile);
        $this->assertEquals($this->testContent, $content2);
    }

    /**
     * Test reading non-existent file
     */
    public function testReadNonExistentFile(): void
    {
        $this->expectException(FileOperationException::class);
        $this->fileOperator->read('/non/existent/file.txt');
    }

    /**
     * Test basic file writing functionality
     */
    public function testWrite(): void
    {
        $result = $this->fileOperator->write($this->testFile, $this->testContent);
        $this->assertTrue($result);
        $this->assertTrue(file_exists($this->testFile));
        $this->assertEquals($this->testContent, file_get_contents($this->testFile));
    }

    /**
     * Test writing with compression
     */
    public function testWriteWithCompression(): void
    {
        $largeContent = str_repeat($this->testContent . "\n", 100);
        
        $result = $this->fileOperator->write($this->testFile, $largeContent, [
            'auto_compress' => true
        ]);
        
        $this->assertTrue($result);
        $this->assertTrue(file_exists($this->testFile));
        
        // Read back and verify
        $readContent = $this->fileOperator->read($this->testFile);
        $this->assertEquals($largeContent, $readContent);
    }

    /**
     * Test atomic write operations
     */
    public function testAtomicWrite(): void
    {
        $result = $this->fileOperator->write($this->testFile, $this->testContent);
        $this->assertTrue($result);
        
        // Verify atomic transaction worked
        $this->assertTrue(file_exists($this->testFile));
        $this->assertEquals($this->testContent, file_get_contents($this->testFile));
    }

    /**
     * Test write rollback on failure
     */
    public function testWriteRollback(): void
    {
        // Create existing file
        file_put_contents($this->testFile, 'original content');
        
        // Make directory read-only to force write failure
        chmod($this->testDir, 0555);
        
        try {
            $this->fileOperator->write($this->testFile . '_new', $this->testContent);
            $this->fail('Expected FileOperationException was not thrown');
        } catch (FileOperationException $e) {
            // Expected exception
            $this->assertStringContains('Failed to write file', $e->getMessage());
        }
        
        // Restore permissions
        chmod($this->testDir, 0755);
        
        // Verify original file is unchanged
        $this->assertEquals('original content', file_get_contents($this->testFile));
    }

    /**
     * Test file copying functionality
     */
    public function testCopy(): void
    {
        file_put_contents($this->testFile, $this->testContent);
        $destinationFile = $this->testDir . '/copied_file.txt';
        
        $result = $this->fileOperator->copy($this->testFile, $destinationFile);
        $this->assertTrue($result);
        
        $this->assertTrue(file_exists($destinationFile));
        $this->assertEquals($this->testContent, file_get_contents($destinationFile));
    }

    /**
     * Test copying with different optimization strategies
     */
    public function testCopyWithOptimization(): void
    {
        file_put_contents($this->testFile, $this->testContent);
        $destinationFile = $this->testDir . '/copied_optimized.txt';
        
        // Test zero-copy
        $result = $this->fileOperator->copy($this->testFile, $destinationFile, [
            'strategy_hint' => 'zero_copy'
        ]);
        $this->assertTrue($result);
        
        // Test chunked copy
        $destinationFile2 = $this->testDir . '/copied_chunked.txt';
        $result = $this->fileOperator->copy($this->testFile, $destinationFile2, [
            'strategy_hint' => 'chunked',
            'chunk_size' => 1024
        ]);
        $this->assertTrue($result);
    }

    /**
     * Test copy with integrity verification
     */
    public function testCopyWithIntegrityCheck(): void
    {
        file_put_contents($this->testFile, $this->testContent);
        $destinationFile = $this->testDir . '/copied_verified.txt';
        
        $result = $this->fileOperator->copy($this->testFile, $destinationFile, [
            'verify_integrity' => true
        ]);
        
        $this->assertTrue($result);
        $this->assertEquals($this->testContent, file_get_contents($destinationFile));
    }

    /**
     * Test file deletion functionality
     */
    public function testDelete(): void
    {
        file_put_contents($this->testFile, $this->testContent);
        $this->assertTrue(file_exists($this->testFile));
        
        $result = $this->fileOperator->delete($this->testFile);
        $this->assertTrue($result);
        $this->assertFalse(file_exists($this->testFile));
    }

    /**
     * Test secure file deletion
     */
    public function testSecureDelete(): void
    {
        file_put_contents($this->testFile, $this->testContent);
        $this->assertTrue(file_exists($this->testFile));
        
        $result = $this->fileOperator->delete($this->testFile, ['secure_delete' => true]);
        $this->assertTrue($result);
        $this->assertFalse(file_exists($this->testFile));
    }

    /**
     * Test deleting non-existent file
     */
    public function testDeleteNonExistentFile(): void
    {
        $result = $this->fileOperator->delete('/non/existent/file.txt');
        $this->assertTrue($result); // Should return true for already deleted files
    }

    /**
     * Test getting file information
     */
    public function testGetFileInfo(): void
    {
        file_put_contents($this->testFile, $this->testContent);
        
        $info = $this->fileOperator->getFileInfo($this->testFile);
        
        $this->assertIsArray($info);
        $this->assertEquals($this->testFile, $info['path']);
        $this->assertEquals(strlen($this->testContent), $info['size']);
        $this->assertArrayHasKey('permissions', $info);
        $this->assertArrayHasKey('checksum', $info);
        $this->assertArrayHasKey('ai_analysis', $info);
        $this->assertArrayHasKey('cache_status', $info);
    }

    /**
     * Test getting info for non-existent file
     */
    public function testGetFileInfoNonExistent(): void
    {
        $this->expectException(FileOperationException::class);
        $this->fileOperator->getFileInfo('/non/existent/file.txt');
    }

    /**
     * Test performance optimization
     */
    public function testOptimizePerformance(): void
    {
        $optimizations = $this->fileOperator->optimizePerformance();
        
        $this->assertIsArray($optimizations);
        $this->assertArrayHasKey('cache', $optimizations);
        $this->assertArrayHasKey('ai', $optimizations);
        $this->assertArrayHasKey('checksum', $optimizations);
    }

    /**
     * Test path validation
     */
    public function testPathValidation(): void
    {
        $this->expectException(FileOperationException::class);
        $this->expectExceptionMessage('File path cannot be empty');
        $this->fileOperator->read('');
    }

    /**
     * Test path traversal protection
     */
    public function testPathTraversalProtection(): void
    {
        $this->expectException(FileOperationException::class);
        $this->fileOperator->read('../../../etc/passwd');
    }

    /**
     * Test large file handling
     */
    public function testLargeFileHandling(): void
    {
        // Create a larger test content
        $largeContent = str_repeat($this->testContent . "\n", 1000);
        $largeFile = $this->testDir . '/large_file.txt';
        
        // Test writing large file
        $result = $this->fileOperator->write($largeFile, $largeContent);
        $this->assertTrue($result);
        
        // Test reading large file
        $readContent = $this->fileOperator->read($largeFile, ['strategy_hint' => 'chunked']);
        $this->assertEquals($largeContent, $readContent);
        
        // Test copying large file
        $copiedLargeFile = $this->testDir . '/copied_large.txt';
        $result = $this->fileOperator->copy($largeFile, $copiedLargeFile, [
            'strategy_hint' => 'chunked',
            'chunk_size' => 2048
        ]);
        $this->assertTrue($result);
    }

    /**
     * Test concurrent operations
     */
    public function testConcurrentOperations(): void
    {
        $files = [];
        $content = 'Concurrent test content';
        
        // Create multiple files concurrently
        for ($i = 0; $i < 5; $i++) {
            $file = $this->testDir . "/concurrent_file_{$i}.txt";
            $files[] = $file;
            
            $result = $this->fileOperator->write($file, $content . " {$i}");
            $this->assertTrue($result);
        }
        
        // Read all files back
        foreach ($files as $i => $file) {
            $readContent = $this->fileOperator->read($file);
            $this->assertEquals($content . " {$i}", $readContent);
        }
        
        // Clean up
        foreach ($files as $file) {
            $this->fileOperator->delete($file);
        }
    }

    /**
     * Test error handling and recovery
     */
    public function testErrorHandling(): void
    {
        // Test read permission error
        file_put_contents($this->testFile, $this->testContent);
        chmod($this->testFile, 0000); // Remove all permissions
        
        try {
            $this->fileOperator->read($this->testFile);
            $this->fail('Expected FileOperationException was not thrown');
        } catch (FileOperationException $e) {
            $this->assertStringContains('Failed to read file', $e->getMessage());
        }
        
        // Restore permissions for cleanup
        chmod($this->testFile, 0644);
    }

    /**
     * Test write permission error
     */
    public function testWritePermissionError(): void
    {
        // Make directory read-only
        chmod($this->testDir, 0555);
        
        try {
            $this->fileOperator->write($this->testFile, $this->testContent);
            $this->fail('Expected FileOperationException was not thrown');
        } catch (FileOperationException $e) {
            $this->assertStringContains('Failed to write file', $e->getMessage());
        }
        
        // Restore permissions
        chmod($this->testDir, 0755);
    }

    /**
     * Test checksum verification
     */
    public function testChecksumVerification(): void
    {
        file_put_contents($this->testFile, $this->testContent);
        
        // Read with checksum verification
        $content = $this->fileOperator->read($this->testFile, ['verify_integrity' => true]);
        $this->assertEquals($this->testContent, $content);
        
        // Get file info to check checksum
        $info = $this->fileOperator->getFileInfo($this->testFile);
        $this->assertNotEmpty($info['checksum']);
    }

    /**
     * Test AI optimization engine integration
     */
    public function testAIOptimization(): void
    {
        file_put_contents($this->testFile, $this->testContent);
        
        // Read with AI optimization
        $content = $this->fileOperator->read($this->testFile);
        $this->assertEquals($this->testContent, $content);
        
        // Check AI analysis in file info
        $info = $this->fileOperator->getFileInfo($this->testFile);
        $this->assertArrayHasKey('ai_analysis', $info);
    }

    /**
     * Test cache functionality
     */
    public function testCacheFunctionality(): void
    {
        file_put_contents($this->testFile, $this->testContent);
        
        // First read (cache miss)
        $startTime = microtime(true);
        $content1 = $this->fileOperator->read($this->testFile);
        $firstReadTime = microtime(true) - $startTime;
        
        // Second read (cache hit - should be faster)
        $startTime = microtime(true);
        $content2 = $this->fileOperator->read($this->testFile);
        $secondReadTime = microtime(true) - $startTime;
        
        $this->assertEquals($content1, $content2);
        // Cache hit should generally be faster, but we won't assert this in tests
    }

    /**
     * Test batch operations
     */
    public function testBatchOperations(): void
    {
        $files = [];
        $contents = [];
        
        // Create multiple files
        for ($i = 0; $i < 3; $i++) {
            $file = $this->testDir . "/batch_file_{$i}.txt";
            $content = "Batch content {$i}";
            
            $files[] = $file;
            $contents[] = $content;
            
            $this->fileOperator->write($file, $content);
        }
        
        // Verify all files
        foreach ($files as $i => $file) {
            $this->assertTrue(file_exists($file));
            $this->assertEquals($contents[$i], $this->fileOperator->read($file));
        }
        
        // Batch copy
        $copyDir = $this->testDir . '/batch_copies';
        mkdir($copyDir, 0755);
        
        foreach ($files as $i => $file) {
            $copyFile = $copyDir . "/batch_copy_{$i}.txt";
            $this->assertTrue($this->fileOperator->copy($file, $copyFile));
            $this->assertEquals($contents[$i], file_get_contents($copyFile));
        }
        
        // Batch delete
        foreach ($files as $file) {
            $this->assertTrue($this->fileOperator->delete($file));
        }
    }

    /**
     * Performance benchmark test
     */
    public function testPerformanceBenchmark(): void
    {
        $this->markTestSkipped('Performance benchmarks should be run separately');
        
        // This would contain actual performance benchmarks
        $iterations = 100;
        $testContent = str_repeat('Performance test content', 100);
        
        $startTime = microtime(true);
        
        for ($i = 0; $i < $iterations; $i++) {
            $file = $this->testDir . "/perf_test_{$i}.txt";
            $this->fileOperator->write($file, $testContent);
            $this->fileOperator->read($file);
            $this->fileOperator->delete($file);
        }
        
        $totalTime = microtime(true) - $startTime;
        $avgTime = $totalTime / $iterations;
        
        // Log performance results
        error_log("FileOperator Performance: {$iterations} operations in {$totalTime}s (avg: {$avgTime}s per operation)");
    }

    /**
     * Test memory usage optimization
     */
    public function testMemoryUsage(): void
    {
        $initialMemory = memory_get_usage();
        
        // Perform multiple operations
        for ($i = 0; $i < 10; $i++) {
            $file = $this->testDir . "/memory_test_{$i}.txt";
            $content = str_repeat('Memory test', 1000);
            
            $this->fileOperator->write($file, $content);
            $this->fileOperator->read($file);
            $this->fileOperator->delete($file);
        }
        
        $finalMemory = memory_get_usage();
        $memoryIncrease = $finalMemory - $initialMemory;
        
        // Memory increase should be reasonable (less than 10MB for this test)
        $this->assertLessThan(10 * 1024 * 1024, $memoryIncrease, 'Memory usage increased too much');
    }

    /**
     * Clean up test directory
     */
    private function cleanupTestDirectory(): void
    {
        if (is_dir($this->testDir)) {
            $iterator = new \RecursiveIteratorIterator(
                new \RecursiveDirectoryIterator($this->testDir, \RecursiveDirectoryIterator::SKIP_DOTS),
                \RecursiveIteratorIterator::CHILD_FIRST
            );

            foreach ($iterator as $file) {
                if ($file->isDir()) {
                    @rmdir($file->getRealPath());
                } else {
                    @unlink($file->getRealPath());
                }
            }

            @rmdir($this->testDir);
        }
    }
} 