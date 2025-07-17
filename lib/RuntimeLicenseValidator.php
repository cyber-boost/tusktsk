<?php
/**
 * 🐘 TuskLang Runtime License Validator
 * =====================================
 * Centralized runtime license validation for all SDK operations
 * Ensures compliance across all TuskLang components
 */

namespace TuskPHP\License;

class RuntimeLicenseValidator
{
    private static $instance = null;
    private static $validated = false;
    private static $cache = [];
    private static $apiEndpoint = 'https://lic.tusklang.org/api/v1';
    private static $gracePeriod = 3600; // 1 hour
    private static $lastValidation = 0;
    
    private function __construct() {}
    
    /**
     * Get singleton instance
     */
    public static function getInstance(): self
    {
        if (self::$instance === null) {
            self::$instance = new self();
        }
        return self::$instance;
    }
    
    /**
     * Validate license for SDK operation
     */
    public static function validateForOperation(string $operation, string $sdkType = 'php'): bool
    {
        $instance = self::getInstance();
        
        // Check cache first
        $cacheKey = "{$sdkType}_{$operation}";
        if (isset(self::$cache[$cacheKey]) && self::$cache[$cacheKey]['expires'] > time()) {
            return self::$cache[$cacheKey]['valid'];
        }
        
        // Get license key
        $licenseKey = self::getLicenseKey();
        if (!$licenseKey) {
            self::logViolation("No license key for operation: {$operation}");
            return false;
        }
        
        // Validate with API
        $result = $instance->validateLicense($licenseKey, $operation, $sdkType);
        
        // Cache result
        self::$cache[$cacheKey] = [
            'valid' => $result['valid'],
            'expires' => time() + self::$gracePeriod
        ];
        
        return $result['valid'];
    }
    
    /**
     * Validate license with API
     */
    private function validateLicense(string $licenseKey, string $operation, string $sdkType): array
    {
        try {
            $data = [
                'license_key' => $licenseKey,
                'operation' => $operation,
                'sdk_type' => $sdkType,
                'installation_id' => self::getInstallationId(),
                'hostname' => gethostname(),
                'timestamp' => time(),
                'user_agent' => $_SERVER['HTTP_USER_AGENT'] ?? 'unknown'
            ];
            
            $response = $this->apiRequest('POST', '/validate-operation', $data);
            
            return [
                'valid' => $response['valid'] ?? false,
                'reason' => $response['reason'] ?? 'Unknown error',
                'license' => $response['license'] ?? null
            ];
            
        } catch (\Exception $e) {
            // Fallback to offline validation
            return $this->offlineValidation($licenseKey, $operation);
        }
    }
    
    /**
     * Offline validation (grace period)
     */
    private function offlineValidation(string $licenseKey, string $operation): array
    {
        $cacheFile = sys_get_temp_dir() . '/tusklang_operation_cache.json';
        
        if (file_exists($cacheFile)) {
            $cache = json_decode(file_get_contents($cacheFile), true);
            
            if ($cache && $cache['license_key'] === $licenseKey && $cache['expires'] > time()) {
                // Check if operation is allowed
                if (isset($cache['allowed_operations']) && in_array($operation, $cache['allowed_operations'])) {
                    return [
                        'valid' => true,
                        'reason' => 'Offline cache valid',
                        'license' => $cache['license']
                    ];
                }
            }
        }
        
        return [
            'valid' => false,
            'reason' => 'Operation not allowed offline',
            'license' => null
        ];
    }
    
    /**
     * Get license key
     */
    private static function getLicenseKey(): ?string
    {
        // Check environment variable
        if (getenv('TUSKLANG_LICENSE')) {
            return getenv('TUSKLANG_LICENSE');
        }
        
        // Check license file
        $licenseFile = __DIR__ . '/../../.license';
        if (file_exists($licenseFile)) {
            return trim(file_get_contents($licenseFile));
        }
        
        return null;
    }
    
    /**
     * Get installation ID
     */
    private static function getInstallationId(): string
    {
        $idFile = sys_get_temp_dir() . '/tusklang_installation_id';
        
        if (file_exists($idFile)) {
            return trim(file_get_contents($idFile));
        }
        
        $id = 'RUNTIME-' . strtoupper(substr(md5(uniqid(mt_rand(), true)), 0, 12));
        file_put_contents($idFile, $id);
        
        return $id;
    }
    
    /**
     * Make API request
     */
    private function apiRequest(string $method, string $endpoint, array $data = []): array
    {
        $url = self::$apiEndpoint . $endpoint;
        $headers = [
            'Content-Type: application/json',
            'User-Agent: ' . ($_SERVER['HTTP_USER_AGENT'] ?? 'TuskLang-Runtime/1.0.0'),
            'X-Installation-ID: ' . self::getInstallationId()
        ];
        
        $context = stream_context_create([
            'http' => [
                'method' => $method,
                'header' => implode("\r\n", $headers),
                'content' => $method === 'POST' ? json_encode($data) : null,
                'timeout' => 10
            ]
        ]);
        
        $response = file_get_contents($url, false, $context);
        
        if ($response === false) {
            throw new \Exception('Failed to connect to license API');
        }
        
        $result = json_decode($response, true);
        
        if (json_last_error() !== JSON_ERROR_NONE) {
            throw new \Exception('Invalid JSON response from API');
        }
        
        return $result;
    }
    
    /**
     * Log security violation
     */
    private static function logViolation(string $reason): void
    {
        $logData = [
            'timestamp' => date('Y-m-d H:i:s'),
            'reason' => $reason,
            'hostname' => gethostname(),
            'ip' => $_SERVER['SERVER_ADDR'] ?? 'unknown',
            'user_agent' => $_SERVER['HTTP_USER_AGENT'] ?? 'unknown',
            'installation_id' => self::getInstallationId()
        ];
        
        $logFile = sys_get_temp_dir() . '/tusklang_runtime_violations.log';
        file_put_contents($logFile, json_encode($logData) . "\n", FILE_APPEND | LOCK_EX);
        
        // Report to API
        try {
            $instance = self::getInstance();
            $instance->apiRequest('POST', '/violation', $logData);
        } catch (\Exception $e) {
            // Silent fail for violation reporting
        }
    }
    
    /**
     * Clear cache
     */
    public static function clearCache(): void
    {
        self::$cache = [];
    }
    
    /**
     * Get cache statistics
     */
    public static function getCacheStats(): array
    {
        return [
            'total_entries' => count(self::$cache),
            'memory_usage' => strlen(serialize(self::$cache)),
            'cache_hits' => 0, // Would need to track this
            'cache_misses' => 0 // Would need to track this
        ];
    }
    
    /**
     * Validate operation with decorator pattern
     */
    public static function requireValidLicense(string $operation, string $sdkType = 'php'): callable
    {
        return function(callable $callback) use ($operation, $sdkType) {
            if (!self::validateForOperation($operation, $sdkType)) {
                throw new \Exception("License validation failed for operation: {$operation}");
            }
            
            return $callback();
        };
    }
    
    /**
     * Middleware for web applications
     */
    public static function middleware(string $operation = 'web_request'): callable
    {
        return function($request, $response, $next) use ($operation) {
            if (!self::validateForOperation($operation, 'web')) {
                return $response->withStatus(403)->withJson([
                    'error' => 'License validation failed',
                    'operation' => $operation
                ]);
            }
            
            return $next($request, $response);
        };
    }
} 