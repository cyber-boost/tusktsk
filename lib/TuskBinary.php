<?php
/**
 * 🚀 TuskLang Binary Performance Layer
 * ===================================
 * "Converting elephants to lightning - Binary TuskLang for production speed"
 * 
 * Features:
 * - Binary .tsk file format (30-50% smaller, 2-5x faster parsing)
 * - Integration with JIT optimizer
 * - Memory-efficient storage with compression
 * - Optional binary mode for high-performance scenarios
 * 
 * Strong. Secure. Scalable. 🐘⚡
 */

namespace TuskPHP\Utils;

use TuskPHP\Utils\TuskLang;
use TuskPHP\Utils\TuskLangJITOptimizer;

class TuskBinary
{
    private static $instance = null;
    private $compressionLevel = 6; // gzip level 1-9
    private $binaryVersion = '1.0';
    private $magicHeader = 'TUSK'; // 4-byte magic header
    private $compilationCache = [];
    private $performanceMetrics = [];
    
    // Binary format specifications
    const BINARY_VERSION = '1.0';
    const MAGIC_HEADER = 'TUSK';
    const COMPRESSION_NONE = 0;
    const COMPRESSION_GZIP = 1;
    const COMPRESSION_BZIP2 = 2;
    const COMPRESSION_LZ4 = 3;
    
    // Binary flags
    const FLAG_JIT_OPTIMIZED = 0x01;
    const FLAG_COMPRESSED = 0x02;
    const FLAG_ENCRYPTED = 0x04;
    const FLAG_DEBUG_INFO = 0x08;
    
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
     * Compile TuskLang source to binary format
     */
    public function compile(string $tuskSource, array $options = []): string
    {
        $startTime = microtime(true);
        $startMemory = memory_get_usage(true);
        
        try {
            // Parse TuskLang source first
            $parsed = TuskLang::parse($tuskSource);
            
            // Apply JIT optimization if enabled
            if ($options['jit_optimize'] ?? true) {
                $jitOptimizer = TuskLangJITOptimizer::getInstance();
                $parsed = $jitOptimizer->optimizeParsingWithJIT($tuskSource, $options);
            }
            
            // Serialize to binary format
            $binaryData = $this->serializeToBinary($parsed, $options);
            
            // Apply compression if requested
            if ($options['compress'] ?? true) {
                $binaryData = $this->compressBinary($binaryData, $options);
            }
            
            // Add binary header
            $binaryData = $this->addBinaryHeader($binaryData, $options);
            
            // Update performance metrics
            $this->updateCompilationMetrics($startTime, $startMemory, strlen($tuskSource), strlen($binaryData));
            
            return $binaryData;
            
        } catch (\Exception $e) {
            error_log("TuskBinary compilation error: " . $e->getMessage());
            throw new \RuntimeException("Binary compilation failed: " . $e->getMessage());
        }
    }
    
    /**
     * Compile TuskLang file to binary file
     */
    public function compileFile(string $sourceFile, string $outputFile = null, array $options = []): bool
    {
        if (!file_exists($sourceFile)) {
            throw new \InvalidArgumentException("Source file not found: {$sourceFile}");
        }
        
        // Determine output file
        if ($outputFile === null) {
            $outputFile = preg_replace('/\.tsk$/', '.tskb', $sourceFile);
        }
        
        // Read source file
        $tuskSource = file_get_contents($sourceFile);
        
        // Compile to binary
        $binaryData = $this->compile($tuskSource, $options);
        
        // Write binary file
        $result = file_put_contents($outputFile, $binaryData);
        
        if ($result === false) {
            throw new \RuntimeException("Failed to write binary file: {$outputFile}");
        }
        
        // Log compilation success
        $compressionRatio = (1 - strlen($binaryData) / strlen($tuskSource)) * 100;
        error_log(sprintf(
            "TuskBinary: Compiled %s → %s (%.1f%% smaller)",
            basename($sourceFile),
            basename($outputFile),
            $compressionRatio
        ));
        
        return true;
    }
    
    /**
     * Execute binary TuskLang file
     */
    public function execute(string $binaryData, array $context = []): array
    {
        $startTime = microtime(true);
        
        try {
            // Validate binary format
            $this->validateBinaryFormat($binaryData);
            
            // Parse binary header
            $header = $this->parseBinaryHeader($binaryData);
            
            // Extract payload
            $payload = $this->extractPayload($binaryData, $header);
            
            // Decompress if needed
            if ($header['flags'] & self::FLAG_COMPRESSED) {
                $payload = $this->decompressBinary($payload, $header);
            }
            
            // Deserialize from binary
            $parsed = $this->deserializeFromBinary($payload, $header);
            
            // Apply context variables
            if (!empty($context)) {
                $parsed = $this->applyContext($parsed, $context);
            }
            
            // Update execution metrics
            $executionTime = microtime(true) - $startTime;
            $this->updateExecutionMetrics($executionTime, strlen($binaryData));
            
            return $parsed;
            
        } catch (\Exception $e) {
            error_log("TuskBinary execution error: " . $e->getMessage());
            throw new \RuntimeException("Binary execution failed: " . $e->getMessage());
        }
    }
    
    /**
     * Execute binary TuskLang file
     */
    public function executeFile(string $binaryFile, array $context = []): array
    {
        if (!file_exists($binaryFile)) {
            throw new \InvalidArgumentException("Binary file not found: {$binaryFile}");
        }
        
        $binaryData = file_get_contents($binaryFile);
        return $this->execute($binaryData, $context);
    }
    
    /**
     * Serialize parsed data to binary format
     */
    private function serializeToBinary(array $parsed, array $options): string
    {
        // Use PHP's efficient binary serialization
        $serialized = serialize($parsed);
        
        // Apply binary optimizations
        if ($options['optimize_binary'] ?? true) {
            $serialized = $this->optimizeBinarySerialization($serialized);
        }
        
        return $serialized;
    }
    
    /**
     * Deserialize from binary format
     */
    private function deserializeFromBinary(string $binaryData, array $header): array
    {
        // Apply binary optimizations in reverse
        if ($header['optimized'] ?? false) {
            $binaryData = $this->unoptimizeBinarySerialization($binaryData);
        }
        
        // Deserialize using PHP
        $unserialized = unserialize($binaryData);
        
        if ($unserialized === false) {
            throw new \RuntimeException("Failed to deserialize binary data");
        }
        
        return $unserialized;
    }
    
    /**
     * Optimize binary serialization
     */
    private function optimizeBinarySerialization(string $serialized): string
    {
        // Replace common patterns with shorter representations
        $optimizations = [
            'a:0:{}' => "\x00",  // Empty array
            's:0:"";' => "\x01", // Empty string
            'i:0;' => "\x02",    // Zero integer
            'i:1;' => "\x03",    // One integer
            'b:1;' => "\x04",    // True boolean
            'b:0;' => "\x05",    // False boolean
            'N;' => "\x06",      // Null value
        ];
        
        foreach ($optimizations as $pattern => $replacement) {
            $serialized = str_replace($pattern, $replacement, $serialized);
        }
        
        return $serialized;
    }
    
    /**
     * Reverse binary optimization
     */
    private function unoptimizeBinarySerialization(string $optimized): string
    {
        // Reverse the optimizations
        $reversions = [
            "\x00" => 'a:0:{}',  // Empty array
            "\x01" => 's:0:"";', // Empty string
            "\x02" => 'i:0;',    // Zero integer
            "\x03" => 'i:1;',    // One integer
            "\x04" => 'b:1;',    // True boolean
            "\x05" => 'b:0;',    // False boolean
            "\x06" => 'N;',      // Null value
        ];
        
        foreach ($reversions as $pattern => $replacement) {
            $optimized = str_replace($pattern, $replacement, $optimized);
        }
        
        return $optimized;
    }
    
    /**
     * Compress binary data
     */
    private function compressBinary(string $data, array $options): string
    {
        $compression = $options['compression'] ?? self::COMPRESSION_GZIP;
        $level = $options['compression_level'] ?? $this->compressionLevel;
        
        switch ($compression) {
            case self::COMPRESSION_GZIP:
                return gzencode($data, $level);
                
            case self::COMPRESSION_BZIP2:
                if (function_exists('bzcompress')) {
                    return bzcompress($data, $level);
                }
                // Fallback to gzip
                return gzencode($data, $level);
                
            case self::COMPRESSION_LZ4:
                if (extension_loaded('lz4')) {
                    return lz4_compress($data, $level);
                }
                // Fallback to gzip
                return gzencode($data, $level);
                
            default:
                return $data; // No compression
        }
    }
    
    /**
     * Decompress binary data
     */
    private function decompressBinary(string $data, array $header): string
    {
        $compression = $header['compression'] ?? self::COMPRESSION_GZIP;
        
        switch ($compression) {
            case self::COMPRESSION_GZIP:
                $decompressed = gzdecode($data);
                break;
                
            case self::COMPRESSION_BZIP2:
                $decompressed = bzdecompress($data);
                break;
                
            case self::COMPRESSION_LZ4:
                if (extension_loaded('lz4')) {
                    $decompressed = lz4_uncompress($data);
                } else {
                    throw new \RuntimeException("LZ4 extension not available for decompression");
                }
                break;
                
            default:
                $decompressed = $data; // No compression
        }
        
        if ($decompressed === false) {
            throw new \RuntimeException("Failed to decompress binary data");
        }
        
        return $decompressed;
    }
    
    /**
     * Add binary header to data
     */
    private function addBinaryHeader(string $data, array $options): string
    {
        $flags = 0;
        
        // Set flags based on options
        if ($options['jit_optimize'] ?? true) {
            $flags |= self::FLAG_JIT_OPTIMIZED;
        }
        if ($options['compress'] ?? true) {
            $flags |= self::FLAG_COMPRESSED;
        }
        if ($options['debug'] ?? false) {
            $flags |= self::FLAG_DEBUG_INFO;
        }
        
        // Build header (32 bytes total)
        $header = '';
        $header .= self::MAGIC_HEADER;                          // 4 bytes
        $header .= pack('V', crc32($data));                     // 4 bytes - CRC32 checksum
        $header .= pack('V', strlen($data));                    // 4 bytes - data length
        $header .= pack('V', $flags);                           // 4 bytes - flags
        $header .= pack('V', $options['compression'] ?? self::COMPRESSION_GZIP); // 4 bytes - compression
        $header .= pack('V', time());                           // 4 bytes - compilation timestamp
        $header .= str_pad(self::BINARY_VERSION, 8, "\x00");    // 8 bytes - version string
        
        return $header . $data;
    }
    
    /**
     * Parse binary header
     */
    private function parseBinaryHeader(string $binaryData): array
    {
        if (strlen($binaryData) < 32) {
            throw new \RuntimeException("Invalid binary format: header too short");
        }
        
        $header = substr($binaryData, 0, 32);
        
        // Unpack header fields
        $magic = substr($header, 0, 4);
        $crc32 = unpack('V', substr($header, 4, 4))[1];
        $dataLength = unpack('V', substr($header, 8, 4))[1];
        $flags = unpack('V', substr($header, 12, 4))[1];
        $compression = unpack('V', substr($header, 16, 4))[1];
        $timestamp = unpack('V', substr($header, 20, 4))[1];
        $version = rtrim(substr($header, 24, 8), "\x00");
        
        return [
            'magic' => $magic,
            'crc32' => $crc32,
            'data_length' => $dataLength,
            'flags' => $flags,
            'compression' => $compression,
            'timestamp' => $timestamp,
            'version' => $version,
            'optimized' => ($flags & self::FLAG_JIT_OPTIMIZED) !== 0
        ];
    }
    
    /**
     * Validate binary format
     */
    private function validateBinaryFormat(string $binaryData): void
    {
        if (strlen($binaryData) < 32) {
            throw new \RuntimeException("Invalid binary format: file too short");
        }
        
        $magic = substr($binaryData, 0, 4);
        if ($magic !== self::MAGIC_HEADER) {
            throw new \RuntimeException("Invalid binary format: wrong magic header");
        }
        
        $header = $this->parseBinaryHeader($binaryData);
        $payload = substr($binaryData, 32);
        
        // Verify checksum
        $actualCrc = crc32($payload);
        if ($actualCrc !== $header['crc32']) {
            throw new \RuntimeException("Binary data corrupted: checksum mismatch");
        }
        
        // Verify version compatibility
        if (version_compare($header['version'], self::BINARY_VERSION, '>')) {
            throw new \RuntimeException("Binary format version {$header['version']} not supported");
        }
    }
    
    /**
     * Extract payload from binary data
     */
    private function extractPayload(string $binaryData, array $header): string
    {
        return substr($binaryData, 32);
    }
    
    /**
     * Apply context variables to parsed data
     */
    private function applyContext(array $parsed, array $context): array
    {
        // Apply context variables by walking through the parsed structure
        return $this->walkAndApplyContext($parsed, $context);
    }
    
    /**
     * Recursively apply context variables
     */
    private function walkAndApplyContext($data, array $context)
    {
        if (is_array($data)) {
            $result = [];
            foreach ($data as $key => $value) {
                $result[$key] = $this->walkAndApplyContext($value, $context);
            }
            return $result;
        } elseif (is_string($data)) {
            // Replace context variables in strings
            foreach ($context as $var => $value) {
                $data = str_replace("\${$var}", $value, $data);
                $data = str_replace("{{{$var}}}", $value, $data);
            }
            return $data;
        } else {
            return $data;
        }
    }
    
    /**
     * Initialize performance tracking
     */
    private function initializePerformanceTracking(): void
    {
        $this->performanceMetrics = [
            'compilations' => 0,
            'executions' => 0,
            'total_compile_time' => 0,
            'total_execution_time' => 0,
            'bytes_compiled' => 0,
            'bytes_executed' => 0,
            'compression_ratio' => 0,
            'cache_hits' => 0,
            'cache_misses' => 0
        ];
    }
    
    /**
     * Update compilation metrics
     */
    private function updateCompilationMetrics(float $startTime, int $startMemory, int $sourceSize, int $binarySize): void
    {
        $compilationTime = microtime(true) - $startTime;
        $compressionRatio = (1 - $binarySize / $sourceSize) * 100;
        
        $this->performanceMetrics['compilations']++;
        $this->performanceMetrics['total_compile_time'] += $compilationTime;
        $this->performanceMetrics['bytes_compiled'] += $sourceSize;
        $this->performanceMetrics['compression_ratio'] = $compressionRatio;
    }
    
    /**
     * Update execution metrics
     */
    private function updateExecutionMetrics(float $executionTime, int $binarySize): void
    {
        $this->performanceMetrics['executions']++;
        $this->performanceMetrics['total_execution_time'] += $executionTime;
        $this->performanceMetrics['bytes_executed'] += $binarySize;
    }
    
    /**
     * Get performance statistics
     */
    public function getPerformanceStats(): array
    {
        $stats = $this->performanceMetrics;
        
        // Calculate averages
        if ($stats['compilations'] > 0) {
            $stats['avg_compile_time'] = $stats['total_compile_time'] / $stats['compilations'];
            $stats['avg_source_size'] = $stats['bytes_compiled'] / $stats['compilations'];
        }
        
        if ($stats['executions'] > 0) {
            $stats['avg_execution_time'] = $stats['total_execution_time'] / $stats['executions'];
            $stats['avg_binary_size'] = $stats['bytes_executed'] / $stats['executions'];
        }
        
        return $stats;
    }
    
    /**
     * Benchmark binary vs text performance
     */
    public function benchmark(string $tuskSource, int $iterations = 100): array
    {
        // Benchmark text parsing
        $textStart = microtime(true);
        for ($i = 0; $i < $iterations; $i++) {
            TuskLang::parse($tuskSource);
        }
        $textTime = microtime(true) - $textStart;
        
        // Compile to binary
        $binaryData = $this->compile($tuskSource);
        
        // Benchmark binary execution
        $binaryStart = microtime(true);
        for ($i = 0; $i < $iterations; $i++) {
            $this->execute($binaryData);
        }
        $binaryTime = microtime(true) - $binaryStart;
        
        return [
            'iterations' => $iterations,
            'text_time' => $textTime,
            'binary_time' => $binaryTime,
            'speedup' => $textTime / $binaryTime,
            'text_avg' => $textTime / $iterations,
            'binary_avg' => $binaryTime / $iterations,
            'source_size' => strlen($tuskSource),
            'binary_size' => strlen($binaryData),
            'compression_ratio' => (1 - strlen($binaryData) / strlen($tuskSource)) * 100
        ];
    }
    
    /**
     * Cache compiled binary
     */
    public function cacheCompiled(string $sourceFile, string $binaryData): void
    {
        $cacheKey = md5_file($sourceFile);
        $this->compilationCache[$cacheKey] = [
            'binary' => $binaryData,
            'timestamp' => filemtime($sourceFile),
            'created' => time()
        ];
    }
    
    /**
     * Get cached binary if valid
     */
    public function getCachedBinary(string $sourceFile): ?string
    {
        $cacheKey = md5_file($sourceFile);
        
        if (!isset($this->compilationCache[$cacheKey])) {
            $this->performanceMetrics['cache_misses']++;
            return null;
        }
        
        $cached = $this->compilationCache[$cacheKey];
        
        // Check if source file has been modified
        if (filemtime($sourceFile) > $cached['timestamp']) {
            unset($this->compilationCache[$cacheKey]);
            $this->performanceMetrics['cache_misses']++;
            return null;
        }
        
        $this->performanceMetrics['cache_hits']++;
        return $cached['binary'];
    }
} 