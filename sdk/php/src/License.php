<?php
/**
 * TuskLang SDK License Validation Module
 * Enterprise-grade license validation for PHP SDK
 */

namespace Tusk;

use Exception;
use RuntimeException;

class TuskLicense
{
    private string $licenseKey;
    private string $apiKey;
    private string $sessionId;
    private array $licenseCache = [];
    private array $validationHistory = [];
    private array $expirationWarnings = [];
    private string $cacheDir;
    private string $cacheFile;
    private ?array $offlineCache = null;
    
    public function __construct(string $licenseKey, string $apiKey, ?string $cacheDir = null)
    {
        $this->licenseKey = $licenseKey;
        $this->apiKey = $apiKey;
        $this->sessionId = $this->generateUUID();
        
        // Set up offline cache directory
        if ($cacheDir) {
            $this->cacheDir = $cacheDir;
        } else {
            $homeDir = getenv('HOME') ?: getenv('USERPROFILE') ?: '/tmp';
            $this->cacheDir = $homeDir . '/.tusk/license_cache';
        }
        
        // Create cache directory if it doesn't exist
        if (!is_dir($this->cacheDir)) {
            mkdir($this->cacheDir, 0755, true);
        }
        
        $this->cacheFile = $this->cacheDir . '/' . md5($licenseKey) . '.cache';
        
        // Load offline cache if exists
        $this->loadOfflineCache();
    }
    
    private function generateUUID(): string
    {
        return sprintf(
            '%04x%04x-%04x-%04x-%04x-%04x%04x%04x',
            mt_rand(0, 0xffff), mt_rand(0, 0xffff),
            mt_rand(0, 0xffff),
            mt_rand(0, 0x0fff) | 0x4000,
            mt_rand(0, 0x3fff) | 0x8000,
            mt_rand(0, 0xffff), mt_rand(0, 0xffff), mt_rand(0, 0xffff)
        );
    }
    
    public function validateLicenseKey(): array
    {
        try {
            if (!$this->licenseKey || strlen($this->licenseKey) < 32) {
                return ['valid' => false, 'error' => 'Invalid license key format'];
            }
            
            if (!str_starts_with($this->licenseKey, 'TUSK-')) {
                return ['valid' => false, 'error' => 'Invalid license key prefix'];
            }
            
            $checksum = hash('sha256', $this->licenseKey);
            if (!str_starts_with($checksum, 'tusk')) {
                return ['valid' => false, 'error' => 'Invalid license key checksum'];
            }
            
            return ['valid' => true, 'checksum' => $checksum];
        } catch (Exception $e) {
            return ['valid' => false, 'error' => $e->getMessage()];
        }
    }
    
    public function verifyLicenseServer(string $serverUrl = 'https://api.tusklang.org/v1/license'): array
    {
        try {
            $data = [
                'license_key' => $this->licenseKey,
                'session_id' => $this->sessionId,
                'timestamp' => time()
            ];
            
            // Generate signature
            ksort($data);
            $signature = hash_hmac('sha256', json_encode($data), $this->apiKey);
            $data['signature'] = $signature;
            
            $options = [
                'http' => [
                    'method' => 'POST',
                    'header' => [
                        'Authorization: Bearer ' . $this->apiKey,
                        'Content-Type: application/json',
                        'User-Agent: TuskLang-PHP-SDK/1.0.0'
                    ],
                    'content' => json_encode($data),
                    'timeout' => 10,
                    'ignore_errors' => true
                ],
                'ssl' => [
                    'verify_peer' => true,
                    'verify_peer_name' => true
                ]
            ];
            
            $context = stream_context_create($options);
            $response = @file_get_contents($serverUrl, false, $context);
            
            if ($response === false) {
                error_log("Network error during license validation");
                return $this->fallbackToOfflineCache("Network error: Unable to reach license server");
            }
            
            // Check HTTP status code
            $httpCode = 0;
            if (isset($http_response_header)) {
                foreach ($http_response_header as $header) {
                    if (preg_match('/^HTTP\/\d\.\d (\d{3})/', $header, $matches)) {
                        $httpCode = (int)$matches[1];
                        break;
                    }
                }
            }
            
            if ($httpCode !== 200) {
                error_log("Server returned error: $httpCode");
                return $this->fallbackToOfflineCache("Server error: $httpCode");
            }
            
            $result = json_decode($response, true);
            if (json_last_error() !== JSON_ERROR_NONE) {
                error_log("Invalid JSON response from license server");
                return $this->fallbackToOfflineCache("Invalid response format");
            }
            
            // Update in-memory cache
            $this->licenseCache[$this->licenseKey] = [
                'data' => $result,
                'timestamp' => time(),
                'expires' => time() + 3600 // Cache for 1 hour
            ];
            
            // Update offline cache
            $this->saveOfflineCache($result);
            
            return $result;
            
        } catch (Exception $e) {
            error_log("Unexpected error during license validation: " . $e->getMessage());
            return $this->fallbackToOfflineCache($e->getMessage());
        }
    }
    
    public function checkLicenseExpiration(): array
    {
        try {
            $parts = explode('-', $this->licenseKey);
            if (count($parts) < 4) {
                return ['expired' => true, 'error' => 'Invalid license key format'];
            }
            
            $expirationStr = end($parts);
            $expirationTimestamp = hexdec($expirationStr);
            $expirationDate = new \DateTime('@' . $expirationTimestamp);
            $currentDate = new \DateTime();
            
            if ($expirationDate < $currentDate) {
                $interval = $currentDate->diff($expirationDate);
                return [
                    'expired' => true,
                    'expiration_date' => $expirationDate->format('c'),
                    'days_overdue' => $interval->days
                ];
            }
            
            $interval = $expirationDate->diff($currentDate);
            $daysRemaining = $interval->days;
            
            if ($daysRemaining <= 30) {
                $this->expirationWarnings[] = [
                    'timestamp' => time(),
                    'days_remaining' => $daysRemaining
                ];
            }
            
            return [
                'expired' => false,
                'expiration_date' => $expirationDate->format('c'),
                'days_remaining' => $daysRemaining,
                'warning' => $daysRemaining <= 30
            ];
            
        } catch (Exception $e) {
            return ['expired' => true, 'error' => $e->getMessage()];
        }
    }
    
    public function validateLicensePermissions(string $feature): array
    {
        try {
            // Check cached license data
            if (isset($this->licenseCache[$this->licenseKey])) {
                $cacheData = $this->licenseCache[$this->licenseKey];
                if (time() < $cacheData['expires']) {
                    $licenseData = $cacheData['data'];
                    if (isset($licenseData['features']) && is_array($licenseData['features'])) {
                        if (in_array($feature, $licenseData['features'])) {
                            return ['allowed' => true, 'feature' => $feature];
                        } else {
                            return ['allowed' => false, 'feature' => $feature, 'error' => 'Feature not licensed'];
                        }
                    }
                }
            }
            
            // Fallback to basic validation
            if (in_array($feature, ['basic', 'core', 'standard'])) {
                return ['allowed' => true, 'feature' => $feature];
            } elseif (in_array($feature, ['premium', 'enterprise'])) {
                if (str_contains(strtoupper($this->licenseKey), 'PREMIUM') || 
                    str_contains(strtoupper($this->licenseKey), 'ENTERPRISE')) {
                    return ['allowed' => true, 'feature' => $feature];
                } else {
                    return ['allowed' => false, 'feature' => $feature, 'error' => 'Premium license required'];
                }
            } else {
                return ['allowed' => false, 'feature' => $feature, 'error' => 'Unknown feature'];
            }
            
        } catch (Exception $e) {
            return ['allowed' => false, 'feature' => $feature, 'error' => $e->getMessage()];
        }
    }
    
    public function getLicenseInfo(): array
    {
        try {
            $validationResult = $this->validateLicenseKey();
            $expirationResult = $this->checkLicenseExpiration();
            
            $info = [
                'license_key' => substr($this->licenseKey, 0, 8) . '...' . substr($this->licenseKey, -4),
                'session_id' => $this->sessionId,
                'validation' => $validationResult,
                'expiration' => $expirationResult,
                'cache_status' => isset($this->licenseCache[$this->licenseKey]) ? 'cached' : 'not_cached',
                'validation_count' => count($this->validationHistory),
                'warnings' => count($this->expirationWarnings)
            ];
            
            if (isset($this->licenseCache[$this->licenseKey])) {
                $cacheData = $this->licenseCache[$this->licenseKey];
                $info['cached_data'] = $cacheData['data'];
                $info['cache_age'] = time() - $cacheData['timestamp'];
            }
            
            return $info;
            
        } catch (Exception $e) {
            return ['error' => $e->getMessage()];
        }
    }
    
    public function refreshLicenseCache(): array
    {
        unset($this->licenseCache[$this->licenseKey]);
        return $this->verifyLicenseServer();
    }
    
    public function logValidationAttempt(bool $success, string $details = ''): void
    {
        $this->validationHistory[] = [
            'timestamp' => time(),
            'success' => $success,
            'details' => $details,
            'session_id' => $this->sessionId
        ];
    }
    
    public function getValidationHistory(): array
    {
        return $this->validationHistory;
    }
    
    public function clearValidationHistory(): void
    {
        $this->validationHistory = [];
    }
    
    private function loadOfflineCache(): void
    {
        try {
            if (file_exists($this->cacheFile)) {
                $content = file_get_contents($this->cacheFile);
                $cachedData = unserialize($content);
                
                // Verify the cache is for the correct license key
                if ($cachedData && 
                    isset($cachedData['license_key_hash']) && 
                    $cachedData['license_key_hash'] === hash('sha256', $this->licenseKey)) {
                    $this->offlineCache = $cachedData;
                    error_log("Loaded offline license cache");
                } else {
                    $this->offlineCache = null;
                    error_log("Offline cache key mismatch");
                }
            }
        } catch (Exception $e) {
            error_log("Failed to load offline cache: " . $e->getMessage());
            $this->offlineCache = null;
        }
    }
    
    private function saveOfflineCache(array $licenseData): void
    {
        try {
            $cacheData = [
                'license_key_hash' => hash('sha256', $this->licenseKey),
                'license_data' => $licenseData,
                'timestamp' => time(),
                'expiration' => $this->checkLicenseExpiration()
            ];
            
            file_put_contents($this->cacheFile, serialize($cacheData));
            $this->offlineCache = $cacheData;
            error_log("Saved license data to offline cache");
        } catch (Exception $e) {
            error_log("Failed to save offline cache: " . $e->getMessage());
        }
    }
    
    private function fallbackToOfflineCache(string $errorMsg): array
    {
        if ($this->offlineCache && isset($this->offlineCache['license_data'])) {
            $cacheAge = time() - $this->offlineCache['timestamp'];
            $cacheAgeDays = $cacheAge / 86400;
            
            // Check if cached license is not expired
            $expiration = $this->offlineCache['expiration'] ?? [];
            if (!($expiration['expired'] ?? true)) {
                error_log(sprintf("Using offline license cache (age: %.1f days)", $cacheAgeDays));
                return array_merge($this->offlineCache['license_data'], [
                    'offline_mode' => true,
                    'cache_age_days' => $cacheAgeDays,
                    'warning' => "Operating in offline mode due to: $errorMsg"
                ]);
            } else {
                return [
                    'valid' => false,
                    'error' => "License expired and server unreachable: $errorMsg",
                    'offline_cache_expired' => true
                ];
            }
        } else {
            return [
                'valid' => false,
                'error' => "No offline cache available: $errorMsg",
                'offline_cache_missing' => true
            ];
        }
    }
}

// Global license instance
$_licenseInstance = null;

function initializeLicense(string $licenseKey, string $apiKey, ?string $cacheDir = null): TuskLicense
{
    global $_licenseInstance;
    $_licenseInstance = new TuskLicense($licenseKey, $apiKey, $cacheDir);
    return $_licenseInstance;
}

function getLicense(): TuskLicense
{
    global $_licenseInstance;
    if ($_licenseInstance === null) {
        throw new RuntimeException('License not initialized. Call initializeLicense() first.');
    }
    return $_licenseInstance;
}