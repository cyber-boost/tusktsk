<?php
/**
 * 🐘 TuskLang Offline Grace Period Manager
 * =======================================
 * Manages offline usage support with grace period functionality
 * Allows SDKs to function when API is unavailable
 */

namespace TuskPHP\License;

class OfflineGracePeriod
{
    private static $gracePeriod = 3600; // 1 hour default
    private static $maxGracePeriod = 86400; // 24 hours max
    private static $cacheDir;
    private static $licenseCache = [];
    
    /**
     * Initialize grace period system
     */
    public static function init(int $gracePeriod = 3600): void
    {
        self::$gracePeriod = min($gracePeriod, self::$maxGracePeriod);
        self::$cacheDir = sys_get_temp_dir() . '/tusklang_grace_period';
        
        if (!is_dir(self::$cacheDir)) {
            mkdir(self::$cacheDir, 0700, true);
        }
    }
    
    /**
     * Check if offline usage is allowed
     */
    public static function isOfflineAllowed(string $licenseKey, string $operation = 'general'): bool
    {
        $cacheFile = self::getCacheFile($licenseKey);
        
        if (!file_exists($cacheFile)) {
            return false;
        }
        
        $cache = self::loadCache($licenseKey);
        if (!$cache) {
            return false;
        }
        
        // Check if grace period is still valid
        if ($cache['expires'] < time()) {
            self::clearCache($licenseKey);
            return false;
        }
        
        // Check if operation is allowed offline
        if (isset($cache['allowed_operations']) && !in_array($operation, $cache['allowed_operations'])) {
            return false;
        }
        
        return true;
    }
    
    /**
     * Store license cache for offline use
     */
    public static function storeCache(string $licenseKey, array $licenseData, array $allowedOperations = []): bool
    {
        $cache = [
            'license_key' => $licenseKey,
            'license_data' => $licenseData,
            'allowed_operations' => $allowedOperations ?: ['general', 'parse', 'compile', 'validate'],
            'created' => time(),
            'expires' => time() + self::$gracePeriod,
            'installation_id' => self::getInstallationId()
        ];
        
        $cacheFile = self::getCacheFile($licenseKey);
        $result = file_put_contents($cacheFile, json_encode($cache));
        
        if ($result !== false) {
            chmod($cacheFile, 0600);
            self::$licenseCache[$licenseKey] = $cache;
            return true;
        }
        
        return false;
    }
    
    /**
     * Load license cache
     */
    public static function loadCache(string $licenseKey): ?array
    {
        if (isset(self::$licenseCache[$licenseKey])) {
            return self::$licenseCache[$licenseKey];
        }
        
        $cacheFile = self::getCacheFile($licenseKey);
        if (!file_exists($cacheFile)) {
            return null;
        }
        
        $content = file_get_contents($cacheFile);
        if ($content === false) {
            return null;
        }
        
        $cache = json_decode($content, true);
        if (!$cache) {
            return null;
        }
        
        self::$licenseCache[$licenseKey] = $cache;
        return $cache;
    }
    
    /**
     * Clear license cache
     */
    public static function clearCache(string $licenseKey): bool
    {
        $cacheFile = self::getCacheFile($licenseKey);
        if (file_exists($cacheFile)) {
            unlink($cacheFile);
        }
        
        unset(self::$licenseCache[$licenseKey]);
        return true;
    }
    
    /**
     * Get remaining grace period time
     */
    public static function getRemainingTime(string $licenseKey): int
    {
        $cache = self::loadCache($licenseKey);
        if (!$cache) {
            return 0;
        }
        
        $remaining = $cache['expires'] - time();
        return max(0, $remaining);
    }
    
    /**
     * Extend grace period
     */
    public static function extendGracePeriod(string $licenseKey, int $additionalTime = 3600): bool
    {
        $cache = self::loadCache($licenseKey);
        if (!$cache) {
            return false;
        }
        
        $cache['expires'] = min($cache['expires'] + $additionalTime, time() + self::$maxGracePeriod);
        
        return self::storeCache($licenseKey, $cache['license_data'], $cache['allowed_operations']);
    }
    
    /**
     * Validate offline license
     */
    public static function validateOffline(string $licenseKey, string $operation = 'general'): array
    {
        if (!self::isOfflineAllowed($licenseKey, $operation)) {
            return [
                'valid' => false,
                'reason' => 'Offline usage not allowed or grace period expired',
                'license' => null
            ];
        }
        
        $cache = self::loadCache($licenseKey);
        if (!$cache) {
            return [
                'valid' => false,
                'reason' => 'No offline cache available',
                'license' => null
            ];
        }
        
        return [
            'valid' => true,
            'reason' => 'Offline grace period active',
            'license' => $cache['license_data'],
            'remaining_time' => self::getRemainingTime($licenseKey)
        ];
    }
    
    /**
     * Get grace period statistics
     */
    public static function getStats(): array
    {
        $stats = [
            'total_caches' => 0,
            'active_caches' => 0,
            'expired_caches' => 0,
            'grace_period' => self::$gracePeriod,
            'max_grace_period' => self::$maxGracePeriod
        ];
        
        if (!is_dir(self::$cacheDir)) {
            return $stats;
        }
        
        $files = glob(self::$cacheDir . '/*.json');
        $stats['total_caches'] = count($files);
        
        foreach ($files as $file) {
            $content = file_get_contents($file);
            if ($content) {
                $cache = json_decode($content, true);
                if ($cache && isset($cache['expires'])) {
                    if ($cache['expires'] > time()) {
                        $stats['active_caches']++;
                    } else {
                        $stats['expired_caches']++;
                    }
                }
            }
        }
        
        return $stats;
    }
    
    /**
     * Clean up expired caches
     */
    public static function cleanup(): int
    {
        $cleaned = 0;
        
        if (!is_dir(self::$cacheDir)) {
            return $cleaned;
        }
        
        $files = glob(self::$cacheDir . '/*.json');
        foreach ($files as $file) {
            $content = file_get_contents($file);
            if ($content) {
                $cache = json_decode($content, true);
                if ($cache && isset($cache['expires']) && $cache['expires'] < time()) {
                    unlink($file);
                    $cleaned++;
                }
            }
        }
        
        return $cleaned;
    }
    
    /**
     * Get cache file path
     */
    private static function getCacheFile(string $licenseKey): string
    {
        $hash = hash('sha256', $licenseKey);
        return self::$cacheDir . "/license_{$hash}.json";
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
        
        $id = 'OFFLINE-' . strtoupper(substr(md5(uniqid(mt_rand(), true)), 0, 12));
        file_put_contents($idFile, $id);
        
        return $id;
    }
    
    /**
     * Set grace period duration
     */
    public static function setGracePeriod(int $seconds): void
    {
        self::$gracePeriod = min($seconds, self::$maxGracePeriod);
    }
    
    /**
     * Get current grace period duration
     */
    public static function getGracePeriod(): int
    {
        return self::$gracePeriod;
    }
} 