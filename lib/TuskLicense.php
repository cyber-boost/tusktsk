<?php
/**
 * 🐘 TuskLang License System
 * =========================
 * "Every elephant needs its registration"
 * 
 * License key generation and validation system for TuskLang
 * Uses cryptographic signatures to ensure authenticity
 */

namespace TuskPHP\License;

class TuskLicense
{
    // License types
    const TYPE_COMMUNITY = 'community';
    const TYPE_PROFESSIONAL = 'professional';
    const TYPE_ENTERPRISE = 'enterprise';
    
    // Installation tracking database
    private static $trackingDb = '/var/lib/tusklang/installations.db';
    private static $licenseDb = '/var/lib/tusklang/licenses.db';
    private static $secretKey = null;
    
    /**
     * Generate a new license key
     */
    public static function generate(array $options = []): array
    {
        $defaults = [
            'type' => self::TYPE_COMMUNITY,
            'email' => '',
            'organization' => '',
            'expires' => null,
            'features' => [],
            'max_installations' => 1
        ];
        
        $license = array_merge($defaults, $options);
        $license['id'] = self::generateUniqueId();
        $license['created'] = date('Y-m-d H:i:s');
        $license['version'] = '1.0.0';
        
        // Generate the license key
        $key = self::generateLicenseKey($license);
        $license['key'] = $key;
        
        // Store license in database
        self::storeLicense($license);
        
        return $license;
    }
    
    /**
     * Generate unique license ID
     */
    private static function generateUniqueId(): string
    {
        return 'TSK-' . strtoupper(substr(md5(uniqid(mt_rand(), true)), 0, 8));
    }
    
    /**
     * Generate cryptographic license key
     */
    private static function generateLicenseKey(array $license): string
    {
        // Create payload
        $payload = [
            'id' => $license['id'],
            'type' => $license['type'],
            'created' => $license['created'],
            'expires' => $license['expires']
        ];
        
        // Encode payload
        $encodedPayload = base64_encode(json_encode($payload));
        
        // Create signature
        $signature = self::createSignature($encodedPayload);
        
        // Format: XXXX-XXXX-XXXX-XXXX
        $key = self::formatLicenseKey($license['id'] . '-' . substr($signature, 0, 12));
        
        return $key;
    }
    
    /**
     * Format license key into readable chunks
     */
    private static function formatLicenseKey(string $raw): string
    {
        $raw = preg_replace('/[^A-Z0-9]/', '', strtoupper($raw));
        $chunks = str_split($raw, 4);
        return implode('-', array_slice($chunks, 0, 4));
    }
    
    /**
     * Create cryptographic signature
     */
    private static function createSignature(string $data): string
    {
        $key = self::getSecretKey();
        return hash_hmac('sha256', $data, $key);
    }
    
    /**
     * Get or generate secret key
     */
    private static function getSecretKey(): string
    {
        if (self::$secretKey === null) {
            $keyFile = '/var/lib/tusklang/.secret_key';
            
            if (file_exists($keyFile)) {
                self::$secretKey = trim(file_get_contents($keyFile));
            } else {
                // Generate new secret key
                self::$secretKey = bin2hex(random_bytes(32));
                
                // Ensure directory exists
                $dir = dirname($keyFile);
                if (!is_dir($dir)) {
                    mkdir($dir, 0700, true);
                }
                
                // Save secret key
                file_put_contents($keyFile, self::$secretKey);
                chmod($keyFile, 0600);
            }
        }
        
        return self::$secretKey;
    }
    
    /**
     * Validate a license key
     */
    public static function validate(string $licenseKey): array
    {
        $result = [
            'valid' => false,
            'reason' => '',
            'license' => null
        ];
        
        // Normalize key format
        $licenseKey = strtoupper(preg_replace('/[^A-Z0-9]/', '', $licenseKey));
        
        // Look up license in database
        $license = self::findLicense($licenseKey);
        
        if (!$license) {
            $result['reason'] = 'License key not found';
            return $result;
        }
        
        // Check expiration
        if ($license['expires'] && strtotime($license['expires']) < time()) {
            $result['reason'] = 'License has expired';
            return $result;
        }
        
        // Check installation count
        $installations = self::getInstallations($license['id']);
        if (count($installations) >= $license['max_installations']) {
            $result['reason'] = 'Maximum installations reached';
            return $result;
        }
        
        $result['valid'] = true;
        $result['license'] = $license;
        
        return $result;
    }
    
    /**
     * Track a new installation
     */
    public static function trackInstallation(array $info): bool
    {
        $installation = [
            'id' => uniqid('INST-'),
            'license_id' => $info['license_id'] ?? 'COMMUNITY',
            'hostname' => gethostname(),
            'ip' => $_SERVER['SERVER_ADDR'] ?? 'unknown',
            'path' => $info['path'] ?? getcwd(),
            'php_version' => PHP_VERSION,
            'os' => PHP_OS,
            'created' => date('Y-m-d H:i:s'),
            'last_seen' => date('Y-m-d H:i:s'),
            'metadata' => [
                'user' => get_current_user(),
                'tusklang_version' => $info['version'] ?? '1.0.0',
                'fujsen_enabled' => $info['fujsen'] ?? false,
                'sdks' => $info['sdks'] ?? []
            ]
        ];
        
        // Initialize SQLite database
        $db = self::getTrackingDb();
        
        // Create installations table if not exists
        $db->exec('CREATE TABLE IF NOT EXISTS installations (
            id TEXT PRIMARY KEY,
            license_id TEXT,
            hostname TEXT,
            ip TEXT,
            path TEXT,
            php_version TEXT,
            os TEXT,
            created DATETIME,
            last_seen DATETIME,
            metadata TEXT
        )');
        
        // Insert installation record
        $stmt = $db->prepare('INSERT INTO installations 
            (id, license_id, hostname, ip, path, php_version, os, created, last_seen, metadata)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)');
        
        $stmt->execute([
            $installation['id'],
            $installation['license_id'],
            $installation['hostname'],
            $installation['ip'],
            $installation['path'],
            $installation['php_version'],
            $installation['os'],
            $installation['created'],
            $installation['last_seen'],
            json_encode($installation['metadata'])
        ]);
        
        // Also track in local installation file
        $localTracking = $info['path'] . '/.tusk-installation';
        file_put_contents($localTracking, json_encode([
            'installation_id' => $installation['id'],
            'license_id' => $installation['license_id'],
            'installed' => $installation['created'],
            'path' => $installation['path']
        ], JSON_PRETTY_PRINT));
        
        return true;
    }
    
    /**
     * Get all installations for a license
     */
    public static function getInstallations(string $licenseId): array
    {
        $db = self::getTrackingDb();
        
        $stmt = $db->prepare('SELECT * FROM installations WHERE license_id = ? ORDER BY created DESC');
        $stmt->execute([$licenseId]);
        
        $installations = [];
        while ($row = $stmt->fetch(\PDO::FETCH_ASSOC)) {
            $row['metadata'] = json_decode($row['metadata'], true);
            $installations[] = $row;
        }
        
        return $installations;
    }
    
    /**
     * Get installation statistics
     */
    public static function getStats(): array
    {
        $db = self::getTrackingDb();
        
        // Total installations
        $total = $db->query('SELECT COUNT(*) FROM installations')->fetchColumn();
        
        // By OS
        $byOs = [];
        $stmt = $db->query('SELECT os, COUNT(*) as count FROM installations GROUP BY os');
        while ($row = $stmt->fetch(\PDO::FETCH_ASSOC)) {
            $byOs[$row['os']] = $row['count'];
        }
        
        // By PHP version
        $byPhp = [];
        $stmt = $db->query('SELECT php_version, COUNT(*) as count FROM installations GROUP BY php_version');
        while ($row = $stmt->fetch(\PDO::FETCH_ASSOC)) {
            $byPhp[$row['php_version']] = $row['count'];
        }
        
        // Recent installations
        $recent = $db->query('SELECT COUNT(*) FROM installations WHERE created > datetime("now", "-30 days")')->fetchColumn();
        
        return [
            'total' => $total,
            'by_os' => $byOs,
            'by_php' => $byPhp,
            'recent_30_days' => $recent,
            'generated' => date('Y-m-d H:i:s')
        ];
    }
    
    /**
     * Phone home - update last seen for installation
     */
    public static function phoneHome(string $installationId): bool
    {
        $db = self::getTrackingDb();
        
        $stmt = $db->prepare('UPDATE installations SET last_seen = ? WHERE id = ?');
        return $stmt->execute([date('Y-m-d H:i:s'), $installationId]);
    }
    
    /**
     * Store license in database
     */
    private static function storeLicense(array $license): void
    {
        $db = self::getLicenseDb();
        
        // Create licenses table if not exists
        $db->exec('CREATE TABLE IF NOT EXISTS licenses (
            id TEXT PRIMARY KEY,
            key TEXT UNIQUE,
            type TEXT,
            email TEXT,
            organization TEXT,
            expires DATETIME,
            features TEXT,
            max_installations INTEGER,
            created DATETIME,
            metadata TEXT
        )');
        
        $stmt = $db->prepare('INSERT INTO licenses 
            (id, key, type, email, organization, expires, features, max_installations, created, metadata)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)');
        
        $stmt->execute([
            $license['id'],
            preg_replace('/[^A-Z0-9]/', '', strtoupper($license['key'])),
            $license['type'],
            $license['email'],
            $license['organization'],
            $license['expires'],
            json_encode($license['features']),
            $license['max_installations'],
            $license['created'],
            json_encode(['version' => $license['version']])
        ]);
    }
    
    /**
     * Find license by key
     */
    private static function findLicense(string $key): ?array
    {
        $db = self::getLicenseDb();
        
        $stmt = $db->prepare('SELECT * FROM licenses WHERE key = ?');
        $stmt->execute([$key]);
        
        $license = $stmt->fetch(\PDO::FETCH_ASSOC);
        if ($license) {
            $license['features'] = json_decode($license['features'], true);
            return $license;
        }
        
        return null;
    }
    
    /**
     * Get tracking database connection
     */
    private static function getTrackingDb(): \PDO
    {
        // Ensure directory exists
        $dir = dirname(self::$trackingDb);
        if (!is_dir($dir)) {
            mkdir($dir, 0755, true);
        }
        
        $db = new \PDO('sqlite:' . self::$trackingDb);
        $db->setAttribute(\PDO::ATTR_ERRMODE, \PDO::ERRMODE_EXCEPTION);
        
        return $db;
    }
    
    /**
     * Get license database connection
     */
    private static function getLicenseDb(): \PDO
    {
        // Ensure directory exists
        $dir = dirname(self::$licenseDb);
        if (!is_dir($dir)) {
            mkdir($dir, 0755, true);
        }
        
        $db = new \PDO('sqlite:' . self::$licenseDb);
        $db->setAttribute(\PDO::ATTR_ERRMODE, \PDO::ERRMODE_EXCEPTION);
        
        return $db;
    }
    
    /**
     * API Integration with lic.tusklang.org
     */
    private static $apiBaseUrl = 'https://lic.tusklang.org/api/v1';
    private static $apiCache = [];
    private static $cacheExpiry = 300; // 5 minutes
    
    /**
     * Validate license via API
     */
    public static function validateViaApi(string $licenseKey): array
    {
        $cacheKey = 'validate_' . $licenseKey;
        
        // Check cache first
        if (isset(self::$apiCache[$cacheKey]) && self::$apiCache[$cacheKey]['expires'] > time()) {
            return self::$apiCache[$cacheKey]['data'];
        }
        
        try {
            $response = self::apiRequest('POST', '/validate', [
                'license_key' => $licenseKey,
                'installation_id' => self::getInstallationId(),
                'hostname' => gethostname(),
                'timestamp' => time()
            ]);
            
            $result = [
                'valid' => $response['valid'] ?? false,
                'reason' => $response['reason'] ?? 'Unknown error',
                'license' => $response['license'] ?? null
            ];
            
            // Cache result
            self::$apiCache[$cacheKey] = [
                'data' => $result,
                'expires' => time() + self::$cacheExpiry
            ];
            
            return $result;
            
        } catch (\Exception $e) {
            return [
                'valid' => false,
                'reason' => 'API communication failed: ' . $e->getMessage(),
                'license' => null
            ];
        }
    }
    
    /**
     * Renew license via API
     */
    public static function renew(string $licenseKey): array
    {
        try {
            $response = self::apiRequest('POST', '/renew', [
                'license_key' => $licenseKey,
                'installation_id' => self::getInstallationId(),
                'timestamp' => time()
            ]);
            
            return [
                'success' => $response['success'] ?? false,
                'reason' => $response['reason'] ?? 'Unknown error',
                'expires' => $response['expires'] ?? null
            ];
            
        } catch (\Exception $e) {
            return [
                'success' => false,
                'reason' => 'API communication failed: ' . $e->getMessage(),
                'expires' => null
            ];
        }
    }
    
    /**
     * Revoke license via API
     */
    public static function revoke(string $licenseKey): array
    {
        try {
            $response = self::apiRequest('POST', '/revoke', [
                'license_key' => $licenseKey,
                'installation_id' => self::getInstallationId(),
                'timestamp' => time()
            ]);
            
            return [
                'success' => $response['success'] ?? false,
                'reason' => $response['reason'] ?? 'Unknown error'
            ];
            
        } catch (\Exception $e) {
            return [
                'success' => false,
                'reason' => 'API communication failed: ' . $e->getMessage()
            ];
        }
    }
    
    /**
     * Get license information via API
     */
    public static function getInfo(string $licenseKey): array
    {
        $cacheKey = 'info_' . $licenseKey;
        
        // Check cache first
        if (isset(self::$apiCache[$cacheKey]) && self::$apiCache[$cacheKey]['expires'] > time()) {
            return self::$apiCache[$cacheKey]['data'];
        }
        
        try {
            $response = self::apiRequest('GET', '/info/' . urlencode($licenseKey));
            
            $result = [
                'success' => $response['success'] ?? false,
                'reason' => $response['reason'] ?? 'Unknown error',
                'license' => $response['license'] ?? null
            ];
            
            // Cache result
            self::$apiCache[$cacheKey] = [
                'data' => $result,
                'expires' => time() + self::$cacheExpiry
            ];
            
            return $result;
            
        } catch (\Exception $e) {
            return [
                'success' => false,
                'reason' => 'API communication failed: ' . $e->getMessage(),
                'license' => null
            ];
        }
    }
    
    /**
     * Clear API cache
     */
    public static function clearCache(): void
    {
        self::$apiCache = [];
    }
    
    /**
     * Get cache statistics
     */
    public static function getCacheStats(): array
    {
        $totalEntries = count(self::$apiCache);
        $memoryUsage = strlen(serialize(self::$apiCache));
        
        return [
            'total_entries' => $totalEntries,
            'memory_usage' => $memoryUsage,
            'cache_hits' => 0, // Would need to track this
            'cache_misses' => 0 // Would need to track this
        ];
    }
    
    /**
     * Make API request to lic.tusklang.org
     */
    private static function apiRequest(string $method, string $endpoint, array $data = []): array
    {
        $url = self::$apiBaseUrl . $endpoint;
        $headers = [
            'Content-Type: application/json',
            'User-Agent: TuskLang-License/1.0.0',
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
     * Get unique installation ID
     */
    private static function getInstallationId(): string
    {
        $idFile = '/var/lib/tusklang/.installation_id';
        
        if (file_exists($idFile)) {
            return trim(file_get_contents($idFile));
        }
        
        // Generate new installation ID
        $id = 'INST-' . strtoupper(substr(md5(uniqid(mt_rand(), true)), 0, 12));
        
        // Ensure directory exists
        $dir = dirname($idFile);
        if (!is_dir($dir)) {
            mkdir($dir, 0700, true);
        }
        
        // Save installation ID
        file_put_contents($idFile, $id);
        chmod($idFile, 0600);
        
        return $id;
    }
}