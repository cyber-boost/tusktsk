<?php
/**
 * TuskLang CDN Manager
 * Global content delivery network for package distribution
 */

namespace TuskPHP\Registry;

use Redis;
use Exception;

class CDNManager {
    private array $config;
    private Redis $cache;
    private array $edgeNodes;
    private string $syncInterval;
    
    public function __construct(array $config) {
        $this->config = $config;
        $this->edgeNodes = $config['cdn']['edge_nodes'] ?? [];
        $this->syncInterval = $config['cdn']['sync_interval'] ?? '5m';
        $this->initializeCache();
    }
    
    /**
     * Initialize Redis cache for CDN operations
     */
    private function initializeCache(): void {
        $this->cache = new Redis();
        $this->cache->connect('localhost', 6379);
        $this->cache->select(2); // Use database 2 for CDN operations
    }
    
    /**
     * Distribute package to all edge nodes
     */
    public function distributePackage(string $packageName, string $version, string $filePath): array {
        try {
            $results = [];
            $compressedPath = $this->compressPackage($filePath);
            
            foreach ($this->edgeNodes as $edgeNode) {
                $result = $this->syncToEdgeNode($edgeNode, $packageName, $version, $compressedPath);
                $results[$edgeNode] = $result;
            }
            
            // Update distribution status
            $this->updateDistributionStatus($packageName, $version, $results);
            
            // Clean up temporary compressed file
            if (file_exists($compressedPath) && $compressedPath !== $filePath) {
                unlink($compressedPath);
            }
            
            return [
                'success' => true,
                'results' => $results,
                'edge_nodes' => $this->edgeNodes
            ];
            
        } catch (Exception $e) {
            return [
                'success' => false,
                'error' => $e->getMessage()
            ];
        }
    }
    
    /**
     * Get optimal edge node for client location
     */
    public function getOptimalEdgeNode(string $clientIP = null): string {
        if (!$clientIP) {
            $clientIP = $_SERVER['REMOTE_ADDR'] ?? '127.0.0.1';
        }
        
        // Simple geographic routing - in production, use GeoIP database
        $region = $this->getClientRegion($clientIP);
        
        $nodeMap = [
            'us-east' => 'us-east-1.tusklang.org',
            'us-west' => 'us-west-2.tusklang.org',
            'eu' => 'eu-west-1.tusklang.org',
            'ap' => 'ap-southeast-1.tusklang.org'
        ];
        
        return $nodeMap[$region] ?? $this->edgeNodes[0];
    }
    
    /**
     * Check package availability across edge nodes
     */
    public function checkPackageAvailability(string $packageName, string $version): array {
        $results = [];
        
        foreach ($this->edgeNodes as $edgeNode) {
            $url = "https://$edgeNode/packages/$packageName/$version.tar.gz";
            $available = $this->checkFileAvailability($url);
            $results[$edgeNode] = [
                'available' => $available,
                'url' => $url,
                'last_check' => date('Y-m-d H:i:s')
            ];
        }
        
        return [
            'success' => true,
            'results' => $results
        ];
    }
    
    /**
     * Purge package from all edge nodes
     */
    public function purgePackage(string $packageName, string $version): array {
        try {
            $results = [];
            
            foreach ($this->edgeNodes as $edgeNode) {
                $result = $this->purgeFromEdgeNode($edgeNode, $packageName, $version);
                $results[$edgeNode] = $result;
            }
            
            return [
                'success' => true,
                'results' => $results
            ];
            
        } catch (Exception $e) {
            return [
                'success' => false,
                'error' => $e->getMessage()
            ];
        }
    }
    
    /**
     * Get CDN statistics
     */
    public function getCDNStats(): array {
        $stats = [
            'edge_nodes' => count($this->edgeNodes),
            'total_packages' => $this->getTotalPackages(),
            'total_size' => $this->getTotalSize(),
            'sync_status' => $this->getSyncStatus(),
            'performance' => $this->getPerformanceMetrics()
        ];
        
        return [
            'success' => true,
            'stats' => $stats
        ];
    }
    
    /**
     * Compress package for distribution
     */
    private function compressPackage(string $filePath): string {
        if (!$this->config['cdn']['compression']) {
            return $filePath;
        }
        
        $compressedPath = $filePath . '.gz';
        
        $input = gzopen($filePath, 'rb');
        $output = gzopen($compressedPath, 'wb');
        
        while (!gzeof($input)) {
            gzwrite($output, gzread($input, 4096));
        }
        
        gzclose($input);
        gzclose($output);
        
        return $compressedPath;
    }
    
    /**
     * Sync package to specific edge node
     */
    private function syncToEdgeNode(string $edgeNode, string $packageName, string $version, string $filePath): array {
        try {
            $remotePath = "/var/cdn/packages/$packageName/$version.tar.gz";
            
            // Use rsync for efficient file transfer
            $command = "rsync -avz --progress $filePath $edgeNode:$remotePath";
            $output = [];
            $returnCode = 0;
            
            exec($command, $output, $returnCode);
            
            if ($returnCode === 0) {
                // Update edge node cache
                $this->updateEdgeNodeCache($edgeNode, $packageName, $version);
                
                return [
                    'success' => true,
                    'message' => 'Package synced successfully',
                    'remote_path' => $remotePath
                ];
            } else {
                return [
                    'success' => false,
                    'error' => 'Sync failed',
                    'output' => $output
                ];
            }
            
        } catch (Exception $e) {
            return [
                'success' => false,
                'error' => $e->getMessage()
            ];
        }
    }
    
    /**
     * Update distribution status in cache
     */
    private function updateDistributionStatus(string $packageName, string $version, array $results): void {
        $key = "cdn:distribution:$packageName:$version";
        $status = [
            'timestamp' => time(),
            'results' => $results,
            'edge_nodes' => $this->edgeNodes
        ];
        
        $this->cache->setex($key, 86400, json_encode($status)); // Cache for 24 hours
    }
    
    /**
     * Get client region based on IP
     */
    private function getClientRegion(string $clientIP): string {
        // Simple IP-based routing - in production, use MaxMind GeoIP
        $ipParts = explode('.', $clientIP);
        
        if (count($ipParts) === 4) {
            $firstOctet = (int)$ipParts[0];
            
            if ($firstOctet >= 1 && $firstOctet <= 126) {
                return 'us-east';
            } elseif ($firstOctet >= 128 && $firstOctet <= 191) {
                return 'us-west';
            } elseif ($firstOctet >= 192 && $firstOctet <= 223) {
                return 'eu';
            } else {
                return 'ap';
            }
        }
        
        return 'us-east'; // Default
    }
    
    /**
     * Check if file is available at URL
     */
    private function checkFileAvailability(string $url): bool {
        $ch = curl_init();
        curl_setopt($ch, CURLOPT_URL, $url);
        curl_setopt($ch, CURLOPT_NOBODY, true);
        curl_setopt($ch, CURLOPT_FOLLOWLOCATION, true);
        curl_setopt($ch, CURLOPT_TIMEOUT, 10);
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        
        $result = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        curl_close($ch);
        
        return $httpCode === 200;
    }
    
    /**
     * Purge package from specific edge node
     */
    private function purgeFromEdgeNode(string $edgeNode, string $packageName, string $version): array {
        try {
            $remotePath = "/var/cdn/packages/$packageName/$version.tar.gz";
            
            $command = "ssh $edgeNode 'rm -f $remotePath'";
            $output = [];
            $returnCode = 0;
            
            exec($command, $output, $returnCode);
            
            if ($returnCode === 0) {
                // Clear edge node cache
                $this->clearEdgeNodeCache($edgeNode, $packageName, $version);
                
                return [
                    'success' => true,
                    'message' => 'Package purged successfully'
                ];
            } else {
                return [
                    'success' => false,
                    'error' => 'Purge failed',
                    'output' => $output
                ];
            }
            
        } catch (Exception $e) {
            return [
                'success' => false,
                'error' => $e->getMessage()
            ];
        }
    }
    
    /**
     * Update edge node cache
     */
    private function updateEdgeNodeCache(string $edgeNode, string $packageName, string $version): void {
        $key = "cdn:edge:$edgeNode:$packageName:$version";
        $data = [
            'timestamp' => time(),
            'status' => 'available'
        ];
        
        $this->cache->setex($key, 3600, json_encode($data)); // Cache for 1 hour
    }
    
    /**
     * Clear edge node cache
     */
    private function clearEdgeNodeCache(string $edgeNode, string $packageName, string $version): void {
        $key = "cdn:edge:$edgeNode:$packageName:$version";
        $this->cache->del($key);
    }
    
    /**
     * Get total number of packages in CDN
     */
    private function getTotalPackages(): int {
        $keys = $this->cache->keys("cdn:distribution:*");
        return count($keys);
    }
    
    /**
     * Get total size of packages in CDN
     */
    private function getTotalSize(): int {
        $totalSize = 0;
        $keys = $this->cache->keys("cdn:distribution:*");
        
        foreach ($keys as $key) {
            $data = json_decode($this->cache->get($key), true);
            if ($data && isset($data['file_size'])) {
                $totalSize += $data['file_size'];
            }
        }
        
        return $totalSize;
    }
    
    /**
     * Get sync status across edge nodes
     */
    private function getSyncStatus(): array {
        $status = [];
        
        foreach ($this->edgeNodes as $edgeNode) {
            $keys = $this->cache->keys("cdn:edge:$edgeNode:*");
            $status[$edgeNode] = [
                'packages' => count($keys),
                'last_sync' => $this->getLastSyncTime($edgeNode)
            ];
        }
        
        return $status;
    }
    
    /**
     * Get performance metrics
     */
    private function getPerformanceMetrics(): array {
        return [
            'response_time_avg' => $this->getAverageResponseTime(),
            'throughput' => $this->getThroughput(),
            'cache_hit_rate' => $this->getCacheHitRate()
        ];
    }
    
    /**
     * Get last sync time for edge node
     */
    private function getLastSyncTime(string $edgeNode): string {
        $keys = $this->cache->keys("cdn:edge:$edgeNode:*");
        if (empty($keys)) {
            return 'Never';
        }
        
        $latest = 0;
        foreach ($keys as $key) {
            $data = json_decode($this->cache->get($key), true);
            if ($data && isset($data['timestamp'])) {
                $latest = max($latest, $data['timestamp']);
            }
        }
        
        return $latest > 0 ? date('Y-m-d H:i:s', $latest) : 'Never';
    }
    
    /**
     * Get average response time
     */
    private function getAverageResponseTime(): float {
        // Mock implementation - in production, collect real metrics
        return 45.2; // milliseconds
    }
    
    /**
     * Get throughput
     */
    private function getThroughput(): float {
        // Mock implementation - in production, collect real metrics
        return 1024.5; // MB/s
    }
    
    /**
     * Get cache hit rate
     */
    private function getCacheHitRate(): float {
        // Mock implementation - in production, collect real metrics
        return 94.7; // percentage
    }
} 