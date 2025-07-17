<?php
/**
 * TuskLang Package Registry API Tests
 * Comprehensive test suite for registry functionality
 */

require_once __DIR__ . '/../../fujsen/src/RegistryManager.php';
require_once __DIR__ . '/../../fujsen/src/CDNManager.php';

use TuskPHP\Registry\RegistryManager;
use TuskPHP\Registry\CDNManager;

class RegistryAPITest {
    private RegistryManager $registryManager;
    private CDNManager $cdnManager;
    private array $testConfig;
    
    public function __construct() {
        $this->testConfig = [
            'storage' => [
                'database' => 'sqlite::memory:', // Use in-memory SQLite for testing
                'cache' => 'redis://localhost:6379/1',
                'file_storage' => '/tmp/registry-test'
            ],
            'cdn' => [
                'edge_nodes' => [
                    'test-edge-1.tusklang.org',
                    'test-edge-2.tusklang.org'
                ],
                'sync_interval' => '1m',
                'compression' => true
            ]
        ];
        
        $this->registryManager = new RegistryManager($this->testConfig);
        $this->cdnManager = new CDNManager($this->testConfig);
    }
    
    /**
     * Run all tests
     */
    public function runAllTests(): array {
        $results = [];
        
        echo "🧪 Running TuskLang Package Registry Tests\n";
        echo "==========================================\n\n";
        
        $tests = [
            'testPackageUpload',
            'testPackageDownload',
            'testPackageMetadata',
            'testPackageSearch',
            'testDependencyResolution',
            'testCDNDistribution',
            'testCDNHealth',
            'testRateLimiting'
        ];
        
        foreach ($tests as $test) {
            echo "Running $test... ";
            $result = $this->$test();
            $results[$test] = $result;
            
            if ($result['success']) {
                echo "✅ PASSED\n";
            } else {
                echo "❌ FAILED: " . $result['error'] . "\n";
            }
        }
        
        echo "\n📊 Test Summary:\n";
        $passed = count(array_filter($results, fn($r) => $r['success']));
        $total = count($results);
        echo "Passed: $passed/$total\n";
        
        return $results;
    }
    
    /**
     * Test package upload functionality
     */
    public function testPackageUpload(): array {
        try {
            // Create test package file
            $testPackagePath = $this->createTestPackage('test-package', '1.0.0');
            
            $metadata = [
                'name' => 'test-package',
                'version' => '1.0.0',
                'description' => 'A test package for registry testing',
                'author' => 'Test Author',
                'license' => 'MIT',
                'dependencies' => [
                    ['name' => 'dependency-1', 'version' => '^1.0.0'],
                    ['name' => 'dependency-2', 'version' => '^2.0.0']
                ]
            ];
            
            $result = $this->registryManager->uploadPackage(
                'test-package',
                '1.0.0',
                $testPackagePath,
                $metadata
            );
            
            if (!$result['success']) {
                return ['success' => false, 'error' => $result['error']];
            }
            
            // Verify package was created
            $metadataResult = $this->registryManager->getPackageMetadata('test-package');
            if (!$metadataResult['success']) {
                return ['success' => false, 'error' => 'Package metadata not found after upload'];
            }
            
            // Clean up
            unlink($testPackagePath);
            
            return ['success' => true, 'data' => $result];
            
        } catch (Exception $e) {
            return ['success' => false, 'error' => $e->getMessage()];
        }
    }
    
    /**
     * Test package download functionality
     */
    public function testPackageDownload(): array {
        try {
            // First upload a test package
            $testPackagePath = $this->createTestPackage('download-test', '2.0.0');
            
            $metadata = [
                'name' => 'download-test',
                'version' => '2.0.0',
                'description' => 'Test package for download testing'
            ];
            
            $uploadResult = $this->registryManager->uploadPackage(
                'download-test',
                '2.0.0',
                $testPackagePath,
                $metadata
            );
            
            if (!$uploadResult['success']) {
                return ['success' => false, 'error' => 'Upload failed: ' . $uploadResult['error']];
            }
            
            // Test download
            $downloadResult = $this->registryManager->downloadPackage('download-test', '2.0.0');
            
            if (!$downloadResult['success']) {
                return ['success' => false, 'error' => $downloadResult['error']];
            }
            
            // Verify file exists
            if (!file_exists($downloadResult['file_path'])) {
                return ['success' => false, 'error' => 'Downloaded file not found'];
            }
            
            // Clean up
            unlink($testPackagePath);
            
            return ['success' => true, 'data' => $downloadResult];
            
        } catch (Exception $e) {
            return ['success' => false, 'error' => $e->getMessage()];
        }
    }
    
    /**
     * Test package metadata retrieval
     */
    public function testPackageMetadata(): array {
        try {
            // Upload test package
            $testPackagePath = $this->createTestPackage('metadata-test', '3.0.0');
            
            $metadata = [
                'name' => 'metadata-test',
                'version' => '3.0.0',
                'description' => 'Test package for metadata testing',
                'author' => 'Metadata Tester',
                'license' => 'Apache-2.0',
                'homepage' => 'https://example.com/metadata-test',
                'repository' => 'https://github.com/example/metadata-test'
            ];
            
            $uploadResult = $this->registryManager->uploadPackage(
                'metadata-test',
                '3.0.0',
                $testPackagePath,
                $metadata
            );
            
            if (!$uploadResult['success']) {
                return ['success' => false, 'error' => 'Upload failed: ' . $uploadResult['error']];
            }
            
            // Get metadata
            $metadataResult = $this->registryManager->getPackageMetadata('metadata-test');
            
            if (!$metadataResult['success']) {
                return ['success' => false, 'error' => $metadataResult['error']];
            }
            
            // Verify metadata fields
            $retrievedMetadata = $metadataResult['metadata'];
            $expectedFields = ['name', 'description', 'author', 'license', 'homepage', 'repository'];
            
            foreach ($expectedFields as $field) {
                if (!isset($retrievedMetadata[$field]) || $retrievedMetadata[$field] !== $metadata[$field]) {
                    return ['success' => false, 'error' => "Metadata field '$field' mismatch"];
                }
            }
            
            // Clean up
            unlink($testPackagePath);
            
            return ['success' => true, 'data' => $metadataResult];
            
        } catch (Exception $e) {
            return ['success' => false, 'error' => $e->getMessage()];
        }
    }
    
    /**
     * Test package search functionality
     */
    public function testPackageSearch(): array {
        try {
            // Upload multiple test packages
            $packages = [
                ['name' => 'search-test-1', 'version' => '1.0.0', 'description' => 'First search test package'],
                ['name' => 'search-test-2', 'version' => '1.0.0', 'description' => 'Second search test package'],
                ['name' => 'other-package', 'version' => '1.0.0', 'description' => 'Package not matching search']
            ];
            
            foreach ($packages as $package) {
                $testPackagePath = $this->createTestPackage($package['name'], $package['version']);
                
                $uploadResult = $this->registryManager->uploadPackage(
                    $package['name'],
                    $package['version'],
                    $testPackagePath,
                    $package
                );
                
                if (!$uploadResult['success']) {
                    return ['success' => false, 'error' => "Upload failed for {$package['name']}: " . $uploadResult['error']];
                }
                
                unlink($testPackagePath);
            }
            
            // Test search
            $searchResult = $this->registryManager->searchPackages('search-test', 10, 0);
            
            if (!$searchResult['success']) {
                return ['success' => false, 'error' => $searchResult['error']];
            }
            
            // Verify search results
            $results = $searchResult['results'];
            $searchTestPackages = array_filter($results, fn($r) => strpos($r['name'], 'search-test') === 0);
            
            if (count($searchTestPackages) !== 2) {
                return ['success' => false, 'error' => 'Expected 2 search-test packages, found ' . count($searchTestPackages)];
            }
            
            return ['success' => true, 'data' => $searchResult];
            
        } catch (Exception $e) {
            return ['success' => false, 'error' => $e->getMessage()];
        }
    }
    
    /**
     * Test dependency resolution
     */
    public function testDependencyResolution(): array {
        try {
            // Upload dependency packages first
            $dependencies = [
                ['name' => 'dep-1', 'version' => '1.0.0'],
                ['name' => 'dep-2', 'version' => '2.0.0']
            ];
            
            foreach ($dependencies as $dep) {
                $testPackagePath = $this->createTestPackage($dep['name'], $dep['version']);
                
                $uploadResult = $this->registryManager->uploadPackage(
                    $dep['name'],
                    $dep['version'],
                    $testPackagePath,
                    $dep
                );
                
                if (!$uploadResult['success']) {
                    return ['success' => false, 'error' => "Dependency upload failed for {$dep['name']}: " . $uploadResult['error']];
                }
                
                unlink($testPackagePath);
            }
            
            // Upload main package with dependencies
            $testPackagePath = $this->createTestPackage('dependency-test', '1.0.0');
            
            $metadata = [
                'name' => 'dependency-test',
                'version' => '1.0.0',
                'description' => 'Test package with dependencies',
                'dependencies' => [
                    ['name' => 'dep-1', 'version' => '^1.0.0'],
                    ['name' => 'dep-2', 'version' => '^2.0.0']
                ]
            ];
            
            $uploadResult = $this->registryManager->uploadPackage(
                'dependency-test',
                '1.0.0',
                $testPackagePath,
                $metadata
            );
            
            if (!$uploadResult['success']) {
                return ['success' => false, 'error' => 'Main package upload failed: ' . $uploadResult['error']];
            }
            
            // Test dependency resolution
            $dependencyResult = $this->registryManager->resolveDependencies('dependency-test', '1.0.0');
            
            if (!$dependencyResult['success']) {
                return ['success' => false, 'error' => $dependencyResult['error']];
            }
            
            // Verify dependencies
            $resolvedDeps = $dependencyResult['dependencies'];
            if (count($resolvedDeps) !== 2) {
                return ['success' => false, 'error' => 'Expected 2 dependencies, found ' . count($resolvedDeps)];
            }
            
            // Clean up
            unlink($testPackagePath);
            
            return ['success' => true, 'data' => $dependencyResult];
            
        } catch (Exception $e) {
            return ['success' => false, 'error' => $e->getMessage()];
        }
    }
    
    /**
     * Test CDN distribution
     */
    public function testCDNDistribution(): array {
        try {
            $testPackagePath = $this->createTestPackage('cdn-test', '1.0.0');
            
            $distributionResult = $this->cdnManager->distributePackage('cdn-test', '1.0.0', $testPackagePath);
            
            if (!$distributionResult['success']) {
                return ['success' => false, 'error' => $distributionResult['error']];
            }
            
            // Verify edge nodes
            $results = $distributionResult['results'];
            $edgeNodes = $distributionResult['edge_nodes'];
            
            if (count($results) !== count($edgeNodes)) {
                return ['success' => false, 'error' => 'Distribution results count mismatch'];
            }
            
            // Clean up
            unlink($testPackagePath);
            
            return ['success' => true, 'data' => $distributionResult];
            
        } catch (Exception $e) {
            return ['success' => false, 'error' => $e->getMessage()];
        }
    }
    
    /**
     * Test CDN health check
     */
    public function testCDNHealth(): array {
        try {
            $statsResult = $this->cdnManager->getCDNStats();
            
            if (!$statsResult['success']) {
                return ['success' => false, 'error' => $statsResult['error']];
            }
            
            // Verify stats structure
            $stats = $statsResult['stats'];
            $requiredFields = ['edge_nodes', 'total_packages', 'total_size', 'sync_status', 'performance'];
            
            foreach ($requiredFields as $field) {
                if (!isset($stats[$field])) {
                    return ['success' => false, 'error' => "Missing CDN stats field: $field"];
                }
            }
            
            return ['success' => true, 'data' => $statsResult];
            
        } catch (Exception $e) {
            return ['success' => false, 'error' => $e->getMessage()];
        }
    }
    
    /**
     * Test rate limiting
     */
    public function testRateLimiting(): array {
        try {
            // This is a basic test - in production, you'd test actual rate limiting
            $redis = new Redis();
            $redis->connect('localhost', 6379);
            $redis->select(3);
            
            $testKey = 'rate_limit:test-client';
            $redis->del($testKey);
            
            // Simulate rate limiting
            for ($i = 0; $i < 5; $i++) {
                $current = $redis->incr($testKey);
                if ($current === 1) {
                    $redis->expire($testKey, 60);
                }
            }
            
            $finalCount = $redis->get($testKey);
            $redis->del($testKey);
            
            if ($finalCount != 5) {
                return ['success' => false, 'error' => "Rate limiting test failed: expected 5, got $finalCount"];
            }
            
            return ['success' => true, 'data' => ['rate_limit_count' => $finalCount]];
            
        } catch (Exception $e) {
            return ['success' => false, 'error' => $e->getMessage()];
        }
    }
    
    /**
     * Create a test package file
     */
    private function createTestPackage(string $name, string $version): string {
        $tempDir = sys_get_temp_dir();
        $packagePath = "$tempDir/{$name}-{$version}.tar.gz";
        
        // Create a simple tar.gz file for testing
        $tar = new PharData($packagePath);
        $tar->addFromString('package.json', json_encode([
            'name' => $name,
            'version' => $version,
            'description' => "Test package $name version $version"
        ]));
        
        return $packagePath;
    }
}

// Run tests if called directly
if (php_sapi_name() === 'cli') {
    $test = new RegistryAPITest();
    $results = $test->runAllTests();
    
    $exitCode = 0;
    foreach ($results as $testName => $result) {
        if (!$result['success']) {
            $exitCode = 1;
            break;
        }
    }
    
    exit($exitCode);
} 