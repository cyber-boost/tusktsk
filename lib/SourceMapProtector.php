<?php
/**
 * 🐘 TuskLang Source Map Protector
 * ================================
 * Protects source maps and debugging information
 * Prevents reverse engineering through source maps
 */

namespace TuskPHP\License;

class SourceMapProtector
{
    private static $protectedMaps = [];
    private static $encryptionKey = null;
    private static $mapDir = '/var/lib/tusklang/sourcemaps';
    
    /**
     * Initialize source map protection
     */
    public static function init(): void
    {
        if (!is_dir(self::$mapDir)) {
            mkdir(self::$mapDir, 0700, true);
        }
        
        self::$encryptionKey = self::getEncryptionKey();
    }
    
    /**
     * Protect source map file
     */
    public static function protectSourceMap(string $sourceMapPath, string $outputPath = null): bool
    {
        if (!file_exists($sourceMapPath)) {
            return false;
        }
        
        $sourceMapContent = file_get_contents($sourceMapPath);
        $protectedContent = self::encryptSourceMap($sourceMapContent);
        
        if ($outputPath === null) {
            $outputPath = $sourceMapPath . '.protected';
        }
        
        $result = file_put_contents($outputPath, $protectedContent);
        
        if ($result !== false) {
            chmod($outputPath, 0600);
            self::$protectedMaps[$sourceMapPath] = $outputPath;
            return true;
        }
        
        return false;
    }
    
    /**
     * Encrypt source map content
     */
    private static function encryptSourceMap(string $content): string
    {
        $key = self::getEncryptionKey();
        $iv = random_bytes(16);
        
        $encrypted = openssl_encrypt(
            $content,
            'AES-256-CBC',
            $key,
            OPENSSL_RAW_DATA,
            $iv
        );
        
        $encoded = base64_encode($iv . $encrypted);
        
        // Add protection header
        $header = [
            'version' => '1.0',
            'protected' => true,
            'timestamp' => time(),
            'algorithm' => 'AES-256-CBC'
        ];
        
        return json_encode($header) . "\n" . $encoded;
    }
    
    /**
     * Decrypt source map content (for authorized debugging)
     */
    public static function decryptSourceMap(string $protectedPath): ?string
    {
        if (!file_exists($protectedPath)) {
            return null;
        }
        
        $content = file_get_contents($protectedPath);
        $lines = explode("\n", $content, 2);
        
        if (count($lines) < 2) {
            return null;
        }
        
        $header = json_decode($lines[0], true);
        $encoded = $lines[1];
        
        if (!$header || !isset($header['protected']) || !$header['protected']) {
            return null;
        }
        
        $decoded = base64_decode($encoded);
        $iv = substr($decoded, 0, 16);
        $encrypted = substr($decoded, 16);
        
        $key = self::getEncryptionKey();
        
        $decrypted = openssl_decrypt(
            $encrypted,
            'AES-256-CBC',
            $key,
            OPENSSL_RAW_DATA,
            $iv
        );
        
        return $decrypted ?: null;
    }
    
    /**
     * Obfuscate source map references
     */
    public static function obfuscateMapReferences(string $filePath): bool
    {
        if (!file_exists($filePath)) {
            return false;
        }
        
        $content = file_get_contents($filePath);
        
        // Remove source map comments
        $content = preg_replace('/\/\/# sourceMappingURL=.*$/m', '', $content);
        $content = preg_replace('/\/\*# sourceMappingURL=.*?\*\//s', '', $content);
        
        // Remove source map files
        $content = preg_replace('/\/\/# sourceURL=.*$/m', '', $content);
        
        return file_put_contents($filePath, $content) !== false;
    }
    
    /**
     * Protect JavaScript source maps
     */
    public static function protectJavaScriptMaps(string $buildDir): array
    {
        $results = [
            'protected' => 0,
            'failed' => 0,
            'files' => []
        ];
        
        $mapFiles = glob($buildDir . '/**/*.map', GLOB_BRACE);
        
        foreach ($mapFiles as $mapFile) {
            $protectedFile = $mapFile . '.protected';
            
            if (self::protectSourceMap($mapFile, $protectedFile)) {
                $results['protected']++;
                $results['files'][] = [
                    'original' => $mapFile,
                    'protected' => $protectedFile
                ];
                
                // Remove original map file
                unlink($mapFile);
            } else {
                $results['failed']++;
            }
        }
        
        return $results;
    }
    
    /**
     * Protect CSS source maps
     */
    public static function protectCSSMaps(string $buildDir): array
    {
        $results = [
            'protected' => 0,
            'failed' => 0,
            'files' => []
        ];
        
        $cssFiles = glob($buildDir . '/**/*.css', GLOB_BRACE);
        
        foreach ($cssFiles as $cssFile) {
            // Remove source map references from CSS
            if (self::obfuscateMapReferences($cssFile)) {
                $results['protected']++;
                $results['files'][] = $cssFile;
            } else {
                $results['failed']++;
            }
        }
        
        return $results;
    }
    
    /**
     * Protect TypeScript source maps
     */
    public static function protectTypeScriptMaps(string $buildDir): array
    {
        $results = [
            'protected' => 0,
            'failed' => 0,
            'files' => []
        ];
        
        $tsFiles = glob($buildDir . '/**/*.ts', GLOB_BRACE);
        
        foreach ($tsFiles as $tsFile) {
            // Remove source map references from TypeScript
            if (self::obfuscateMapReferences($tsFile)) {
                $results['protected']++;
                $results['files'][] = $tsFile;
            } else {
                $results['failed']++;
            }
        }
        
        return $results;
    }
    
    /**
     * Generate fake source maps for obfuscation
     */
    public static function generateFakeSourceMaps(string $filePath, string $outputPath): bool
    {
        $content = file_get_contents($filePath);
        $lines = substr_count($content, "\n") + 1;
        
        $fakeMap = [
            'version' => 3,
            'file' => basename($filePath),
            'sourceRoot' => '',
            'sources' => ['obfuscated.js'],
            'names' => [],
            'mappings' => str_repeat('A', $lines * 10) // Fake mappings
        ];
        
        return file_put_contents($outputPath, json_encode($fakeMap)) !== false;
    }
    
    /**
     * Remove debug information from compiled files
     */
    public static function removeDebugInfo(string $filePath): bool
    {
        if (!file_exists($filePath)) {
            return false;
        }
        
        $content = file_get_contents($filePath);
        
        // Remove console.log statements
        $content = preg_replace('/console\.(log|debug|info|warn|error)\s*\([^)]*\);?\s*/', '', $content);
        
        // Remove debugger statements
        $content = preg_replace('/debugger;?\s*/', '', $content);
        
        // Remove source map references
        $content = preg_replace('/\/\/# sourceMappingURL=.*$/m', '', $content);
        
        // Remove comments (optional)
        $content = preg_replace('/\/\*.*?\*\//s', '', $content);
        $content = preg_replace('/\/\/.*$/m', '', $content);
        
        return file_put_contents($filePath, $content) !== false;
    }
    
    /**
     * Get encryption key
     */
    private static function getEncryptionKey(): string
    {
        if (self::$encryptionKey === null) {
            $keyFile = '/var/lib/tusklang/.sourcemap_key';
            
            if (file_exists($keyFile)) {
                self::$encryptionKey = trim(file_get_contents($keyFile));
            } else {
                // Generate new encryption key
                self::$encryptionKey = bin2hex(random_bytes(32));
                
                // Ensure directory exists
                $dir = dirname($keyFile);
                if (!is_dir($dir)) {
                    mkdir($dir, 0700, true);
                }
                
                // Save encryption key
                file_put_contents($keyFile, self::$encryptionKey);
                chmod($keyFile, 0600);
            }
        }
        
        return self::$encryptionKey;
    }
    
    /**
     * Get protection statistics
     */
    public static function getStats(): array
    {
        return [
            'protected_maps' => count(self::$protectedMaps),
            'map_directory' => self::$mapDir,
            'encryption_algorithm' => 'AES-256-CBC'
        ];
    }
    
    /**
     * Clean up protected maps
     */
    public static function cleanup(): int
    {
        $cleaned = 0;
        
        foreach (self::$protectedMaps as $original => $protected) {
            if (file_exists($protected)) {
                unlink($protected);
                $cleaned++;
            }
        }
        
        self::$protectedMaps = [];
        
        return $cleaned;
    }
} 