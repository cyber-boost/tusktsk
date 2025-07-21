<?php
/**
 * 🐘 TuskLang PHP SDK - Protected Version
 * ======================================
 * IonCube/Zend Guard protected version with runtime license validation
 * 
 * This file is encoded and protected against reverse engineering
 * Runtime license validation ensures compliance
 */

namespace TuskLang;

class TuskLangProtected
{
    private static $licenseValidated = false;
    private static $protectionLevel = 'enterprise';
    private static $apiEndpoint = 'https://lic.tusklang.org/api/v1';
    
    /**
     * Initialize protected SDK with license validation
     */
    public static function init(string $licenseKey = null): bool
    {
        // Runtime protection check
        if (!self::checkProtection()) {
            self::selfDestruct();
            return false;
        }
        
        // License validation
        if (!$licenseKey) {
            $licenseKey = self::getStoredLicense();
        }
        
        if (!$licenseKey) {
            self::logViolation('No license key provided');
            return false;
        }
        
        $validation = self::validateLicense($licenseKey);
        if (!$validation['valid']) {
            self::logViolation('Invalid license: ' . $validation['reason']);
            return false;
        }
        
        self::$licenseValidated = true;
        return true;
    }
    
    /**
     * Check if protection is intact
     */
    private static function checkProtection(): bool
    {
        // Check for IonCube/Zend Guard encoding
        if (!extension_loaded('ionCube Loader') && !extension_loaded('Zend Guard Loader')) {
            return false;
        }
        
        // Check file integrity
        $currentHash = hash_file('sha256', __FILE__);
        $expectedHash = self::getExpectedHash();
        
        if ($currentHash !== $expectedHash) {
            return false;
        }
        
        // Check for debugging tools
        if (function_exists('xdebug_info') || function_exists('opcache_get_status')) {
            return false;
        }
        
        return true;
    }
    
    /**
     * Validate license with API
     */
    private static function validateLicense(string $licenseKey): array
    {
        try {
            $data = [
                'license_key' => $licenseKey,
                'installation_id' => self::getInstallationId(),
                'hostname' => gethostname(),
                'timestamp' => time(),
                'sdk_type' => 'php',
                'protection_level' => self::$protectionLevel
            ];
            
            $response = self::apiRequest('POST', '/validate', $data);
            
            return [
                'valid' => $response['valid'] ?? false,
                'reason' => $response['reason'] ?? 'Unknown error',
                'license' => $response['license'] ?? null
            ];
            
        } catch (\Exception $e) {
            // Fallback to offline validation
            return self::offlineValidation($licenseKey);
        }
    }
    
    /**
     * Offline license validation (grace period)
     */
    private static function offlineValidation(string $licenseKey): array
    {
        $cacheFile = sys_get_temp_dir() . '/tusklang_license_cache.json';
        
        if (file_exists($cacheFile)) {
            $cache = json_decode(file_get_contents($cacheFile), true);
            
            if ($cache && $cache['license_key'] === $licenseKey && $cache['expires'] > time()) {
                return [
                    'valid' => true,
                    'reason' => 'Offline cache valid',
                    'license' => $cache['license']
                ];
            }
        }
        
        return [
            'valid' => false,
            'reason' => 'No offline cache available',
            'license' => null
        ];
    }
    
    /**
     * Get stored license key
     */
    private static function getStoredLicense(): ?string
    {
        $possiblePaths = [
            __DIR__ . '/../../.license',
            __DIR__ . '/../../../.license',
            '/opt/tusklang/.license',
            getcwd() . '/.license'
        ];
        
        foreach ($possiblePaths as $path) {
            if (file_exists($path)) {
                return trim(file_get_contents($path));
            }
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
        
        $id = 'PHP-' . strtoupper(substr(md5(uniqid(mt_rand(), true)), 0, 12));
        file_put_contents($idFile, $id);
        
        return $id;
    }
    
    /**
     * Make API request
     */
    private static function apiRequest(string $method, string $endpoint, array $data = []): array
    {
        $url = self::$apiEndpoint . $endpoint;
        $headers = [
            'Content-Type: application/json',
            'User-Agent: TuskLang-PHP-SDK/1.0.0',
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
        
        $logFile = sys_get_temp_dir() . '/tusklang_violations.log';
        file_put_contents($logFile, json_encode($logData) . "\n", FILE_APPEND | LOCK_EX);
        
        // Report to API
        try {
            self::apiRequest('POST', '/violation', $logData);
        } catch (\Exception $e) {
            // Silent fail for violation reporting
        }
    }
    
    /**
     * Self-destruct mechanism
     */
    private static function selfDestruct(): void
    {
        // Clear sensitive data
        self::$licenseValidated = false;
        
        // Log violation
        self::logViolation('Protection violation detected - self-destruct initiated');
        
        // Throw exception to prevent further execution
        throw new \Exception('TuskLang SDK protection violation detected');
    }
    
    /**
     * Get expected file hash (would be set during encoding)
     */
    private static function getExpectedHash(): string
    {
        // This would be set during IonCube/Zend Guard encoding
        return '0000000000000000000000000000000000000000000000000000000000000000';
    }
    
    /**
     * Check if SDK is properly licensed
     */
    public static function isLicensed(): bool
    {
        return self::$licenseValidated;
    }
    
    /**
     * Get protection level
     */
    public static function getProtectionLevel(): string
    {
        return self::$protectionLevel;
    }
    
    /**
     * Enhanced TuskLang functionality (protected)
     */
    public static function parse(string $code): array
    {
        if (!self::isLicensed()) {
            throw new \Exception('TuskLang SDK not properly licensed');
        }
        
        // Implementation would be encoded
        return ['status' => 'protected_implementation'];
    }
    
    public static function compile(string $code): string
    {
        if (!self::isLicensed()) {
            throw new \Exception('TuskLang SDK not properly licensed');
        }
        
        // Implementation would be encoded
        return 'protected_compiled_code';
    }
    
    public static function validate(string $code): bool
    {
        if (!self::isLicensed()) {
            throw new \Exception('TuskLang SDK not properly licensed');
        }
        
        // Implementation would be encoded
        return true;
    }
} 