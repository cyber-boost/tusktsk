<?php
/**
 * 🥜 PeanuConfig - Hierarchical Configuration System
 * =================================================
 * "CSS-like inheritance for TuskLang configuration"
 * 
 * Features:
 * - Auto-parent lookup system (peanu.tsk files)
 * - CSS-like inheritance and override behavior
 * - Directory tree traversal for configuration resolution
 * - Performance caching of resolved configuration chains
 * - Integration with binary peanuts compilation
 * 
 * Strong. Secure. Scalable. 🐘🥜
 */

namespace TuskPHP\Utils;

use TuskPHP\Utils\TuskLang;
use TuskPHP\Utils\PeanutsBinary;

class PeanuConfig
{
    private static $instance = null;
    private $configCache = [];
    private $configChains = [];
    private $performanceMetrics = [];
    
    // Configuration file name (standardized)
    const CONFIG_FILENAME = 'peanu.tsk';
    const CACHE_TTL = 300; // 5 minutes default cache
    
    private function __construct()
    {
        $this->initializePerformanceTracking();
    }
    
    public static function getInstance(): self
    {
        if (self::$instance === null) {
            self::$instance = new self();
        }
        return self::$instance;
    }
    
    /**
     * Get configuration for a given file or directory
     * Implements hierarchical lookup with CSS-like inheritance
     */
    public function getConfig(string $filePath): array
    {
        $startTime = microtime(true);
        
        // Normalize the path
        $directory = is_file($filePath) ? dirname(realpath($filePath)) : realpath($filePath);
        
        // Check cache first
        $cacheKey = md5($directory);
        if (isset($this->configCache[$cacheKey])) {
            $cached = $this->configCache[$cacheKey];
            if (time() - $cached['timestamp'] < self::CACHE_TTL) {
                $this->updatePerformanceMetrics($startTime, true);
                return $cached['config'];
            }
        }
        
        // Build configuration chain
        $configChain = $this->buildConfigChain($directory);
        
        // Resolve inheritance (CSS-like)
        $resolvedConfig = $this->resolveInheritance($configChain);
        
        // Cache the result
        $this->configCache[$cacheKey] = [
            'config' => $resolvedConfig,
            'timestamp' => time(),
            'chain' => $configChain
        ];
        
        // Store chain for debugging
        $this->configChains[$directory] = $configChain;
        
        $this->updatePerformanceMetrics($startTime, false);
        
        return $resolvedConfig;
    }
    
    /**
     * Build configuration chain by walking up directory tree
     */
    private function buildConfigChain(string $startDirectory): array
    {
        $chain = [];
        $currentDir = $startDirectory;
        
        // Walk up the directory tree
        while ($currentDir !== '/' && strlen($currentDir) > 1) {
            $configFile = $currentDir . '/' . self::CONFIG_FILENAME;
            
            if (file_exists($configFile)) {
                $chain[] = [
                    'file' => $configFile,
                    'directory' => $currentDir,
                    'level' => count($chain),
                    'config' => $this->loadConfigFile($configFile)
                ];
            }
            
            // Move up one directory
            $parentDir = dirname($currentDir);
            if ($parentDir === $currentDir) {
                break; // Reached root
            }
            $currentDir = $parentDir;
        }
        
        // Reverse so parent configs come first (CSS inheritance order)
        return array_reverse($chain);
    }
    
    /**
     * Load and parse a single configuration file
     */
    private function loadConfigFile(string $configFile): array
    {
        try {
            // Check if binary version exists and is newer
            $binaryFile = str_replace('.tsk', '.tskb', $configFile);
            if (file_exists($binaryFile) && filemtime($binaryFile) > filemtime($configFile)) {
                // Use binary version for faster loading
                $binary = \TuskPHP\Utils\TuskBinary::getInstance();
                return $binary->executeFile($binaryFile);
            }
            
            // Parse TuskLang file
            $content = file_get_contents($configFile);
            return TuskLang::parse($content);
            
        } catch (\Exception $e) {
            error_log("PeanuConfig: Failed to load {$configFile}: " . $e->getMessage());
            return [];
        }
    }
    
    /**
     * Resolve inheritance chain (CSS-like behavior)
     */
    private function resolveInheritance(array $configChain): array
    {
        $resolved = [];
        
        // Apply configurations in order (parent to child)
        foreach ($configChain as $chainItem) {
            $config = $chainItem['config'];
            $resolved = $this->mergeConfigurations($resolved, $config);
        }
        
        return $resolved;
    }
    
    /**
     * Merge configurations with CSS-like inheritance rules
     */
    private function mergeConfigurations(array $parent, array $child): array
    {
        $result = $parent;
        
        foreach ($child as $key => $value) {
            if (is_array($value) && isset($result[$key]) && is_array($result[$key])) {
                // Deep merge for nested objects (like CSS)
                $result[$key] = $this->mergeConfigurations($result[$key], $value);
            } else {
                // Override primitive values (like CSS)
                $result[$key] = $value;
            }
        }
        
        return $result;
    }
    
    /**
     * Get configuration value with dot notation
     */
    public function get(string $filePath, string $keyPath, $default = null)
    {
        $config = $this->getConfig($filePath);
        
        $keys = explode('.', $keyPath);
        $value = $config;
        
        foreach ($keys as $key) {
            if (is_array($value) && isset($value[$key])) {
                $value = $value[$key];
            } else {
                return $default;
            }
        }
        
        return $value;
    }
    
    /**
     * Check if configuration exists in hierarchy
     */
    public function has(string $filePath, string $keyPath): bool
    {
        return $this->get($filePath, $keyPath, '__NOT_FOUND__') !== '__NOT_FOUND__';
    }
    
    /**
     * Get the configuration chain for debugging
     */
    public function getConfigChain(string $filePath): array
    {
        $directory = is_file($filePath) ? dirname(realpath($filePath)) : realpath($filePath);
        return $this->configChains[$directory] ?? [];
    }
    
    /**
     * Clear configuration cache
     */
    public function clearCache(string $filePath = null): void
    {
        if ($filePath) {
            $directory = is_file($filePath) ? dirname(realpath($filePath)) : realpath($filePath);
            $cacheKey = md5($directory);
            unset($this->configCache[$cacheKey]);
            unset($this->configChains[$directory]);
        } else {
            $this->configCache = [];
            $this->configChains = [];
        }
    }
    
    /**
     * Auto-compile all peanu.tsk files in hierarchy
     */
    public function autoCompileHierarchy(string $rootPath): array
    {
        $results = [
            'compiled' => [],
            'skipped' => [],
            'errors' => []
        ];
        
        // Find all peanu.tsk files
        $configFiles = $this->findConfigFiles($rootPath);
        
        foreach ($configFiles as $configFile) {
            try {
                // Check if binary compilation would help
                $binaryFile = str_replace('.tsk', '.tskb', $configFile);
                
                if (!file_exists($binaryFile) || filemtime($configFile) > filemtime($binaryFile)) {
                    // Compile to binary for faster loading
                    $binary = \TuskPHP\Utils\TuskBinary::getInstance();
                    $success = $binary->compileFile($configFile, $binaryFile, [
                        'jit_optimize' => true,
                        'compress' => true,
                        'optimize_binary' => true
                    ]);
                    
                    if ($success) {
                        $results['compiled'][] = $configFile;
                    } else {
                        $results['errors'][] = [
                            'file' => $configFile,
                            'error' => 'Binary compilation failed'
                        ];
                    }
                } else {
                    $results['skipped'][] = $configFile;
                }
                
            } catch (\Exception $e) {
                $results['errors'][] = [
                    'file' => $configFile,
                    'error' => $e->getMessage()
                ];
            }
        }
        
        return $results;
    }
    
    /**
     * Find all peanu.tsk files recursively
     */
    private function findConfigFiles(string $rootPath, bool $recursive = true): array
    {
        $files = [];
        
        // Check current directory
        $configFile = $rootPath . '/' . self::CONFIG_FILENAME;
        if (file_exists($configFile)) {
            $files[] = $configFile;
        }
        
        if ($recursive) {
            $iterator = new \RecursiveIteratorIterator(
                new \RecursiveDirectoryIterator($rootPath, \RecursiveDirectoryIterator::SKIP_DOTS),
                \RecursiveIteratorIterator::SELF_FIRST
            );
            
            foreach ($iterator as $item) {
                if ($item->isDir()) {
                    $dirConfigFile = $item->getPathname() . '/' . self::CONFIG_FILENAME;
                    if (file_exists($dirConfigFile)) {
                        $files[] = $dirConfigFile;
                    }
                }
            }
        }
        
        return $files;
    }
    
    /**
     * Validate configuration hierarchy
     */
    public function validateHierarchy(string $filePath): array
    {
        $chain = $this->buildConfigChain(
            is_file($filePath) ? dirname(realpath($filePath)) : realpath($filePath)
        );
        
        $validation = [
            'valid' => true,
            'errors' => [],
            'warnings' => [],
            'chain_length' => count($chain),
            'total_size' => 0
        ];
        
        foreach ($chain as $item) {
            try {
                // Validate each config file
                $config = $item['config'];
                $fileSize = filesize($item['file']);
                $validation['total_size'] += $fileSize;
                
                // Check for common issues
                if (empty($config)) {
                    $validation['warnings'][] = "Empty configuration: " . $item['file'];
                }
                
                if ($fileSize > 100000) { // 100KB
                    $validation['warnings'][] = "Large config file: " . $item['file'] . " (" . number_format($fileSize) . " bytes)";
                }
                
            } catch (\Exception $e) {
                $validation['valid'] = false;
                $validation['errors'][] = $item['file'] . ": " . $e->getMessage();
            }
        }
        
        return $validation;
    }
    
    /**
     * Generate configuration documentation
     */
    public function generateDocs(string $filePath): string
    {
        $chain = $this->getConfigChain($filePath);
        $config = $this->getConfig($filePath);
        
        $docs = "# 🥜 Configuration Hierarchy Documentation\n\n";
        $docs .= "**Generated for:** `$filePath`\n\n";
        
        // Show inheritance chain
        $docs .= "## Inheritance Chain\n\n";
        foreach ($chain as $i => $item) {
            $level = str_repeat("  ", $i);
            $docs .= $level . ($i + 1) . ". `" . $item['file'] . "`";
            if ($i === 0) {
                $docs .= " *(root)*";
            } elseif ($i === count($chain) - 1) {
                $docs .= " *(final)*";
            }
            $docs .= "\n";
        }
        
        // Show resolved configuration
        $docs .= "\n## Final Resolved Configuration\n\n";
        $docs .= "```tsk\n";
        $docs .= $this->arrayToTuskLang($config);
        $docs .= "\n```\n";
        
        return $docs;
    }
    
    /**
     * Convert array back to TuskLang format (basic)
     */
    private function arrayToTuskLang(array $array, int $indent = 0): string
    {
        $result = '';
        $indentStr = str_repeat('    ', $indent);
        
        foreach ($array as $key => $value) {
            if (is_array($value)) {
                $result .= $indentStr . $key . ":\n";
                $result .= $this->arrayToTuskLang($value, $indent + 1);
            } else {
                $result .= $indentStr . $key . ': ';
                if (is_string($value)) {
                    $result .= '"' . addslashes($value) . '"';
                } elseif (is_bool($value)) {
                    $result .= $value ? 'true' : 'false';
                } else {
                    $result .= $value;
                }
                $result .= "\n";
            }
        }
        
        return $result;
    }
    
    /**
     * Performance tracking
     */
    private function initializePerformanceTracking(): void
    {
        $this->performanceMetrics = [
            'lookups' => 0,
            'cache_hits' => 0,
            'cache_misses' => 0,
            'total_time' => 0,
            'avg_chain_length' => 0
        ];
    }
    
    private function updatePerformanceMetrics(float $startTime, bool $cacheHit): void
    {
        $this->performanceMetrics['lookups']++;
        $this->performanceMetrics['total_time'] += microtime(true) - $startTime;
        
        if ($cacheHit) {
            $this->performanceMetrics['cache_hits']++;
        } else {
            $this->performanceMetrics['cache_misses']++;
        }
    }
    
    /**
     * Get performance statistics
     */
    public function getPerformanceStats(): array
    {
        $stats = $this->performanceMetrics;
        
        if ($stats['lookups'] > 0) {
            $stats['avg_lookup_time'] = $stats['total_time'] / $stats['lookups'];
            $stats['cache_hit_ratio'] = $stats['cache_hits'] / $stats['lookups'];
        }
        
        return $stats;
    }
} 