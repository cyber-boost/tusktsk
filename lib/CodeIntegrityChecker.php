<?php
/**
 * 🐘 TuskLang Code Integrity Checker
 * ==================================
 * Anti-tampering measures and code integrity verification
 * Ensures SDK files haven't been modified or compromised
 */

namespace TuskPHP\License;

class CodeIntegrityChecker
{
    private static $integrityFile = '/var/lib/tusklang/integrity.json';
    private static $hashAlgorithm = 'sha256';
    private static $criticalFiles = [
        'lib/TuskLicense.php',
        'lib/RuntimeLicenseValidator.php',
        'lib/OfflineGracePeriod.php',
        'lib/CodeIntegrityChecker.php',
        'bin/tsk',
        'bin/tusk-license'
    ];
    
    /**
     * Initialize integrity checker
     */
    public static function init(): void
    {
        if (!file_exists(self::$integrityFile)) {
            self::generateIntegrityFile();
        }
    }
    
    /**
     * Check integrity of all critical files
     */
    public static function checkIntegrity(): array
    {
        $results = [
            'valid' => true,
            'violations' => [],
            'checked_files' => 0,
            'valid_files' => 0,
            'invalid_files' => 0
        ];
        
        $integrityData = self::loadIntegrityData();
        if (!$integrityData) {
            $results['valid'] = false;
            $results['violations'][] = 'Integrity file not found or invalid';
            return $results;
        }
        
        foreach (self::$criticalFiles as $file) {
            $results['checked_files']++;
            
            if (!self::checkFileIntegrity($file, $integrityData)) {
                $results['valid'] = false;
                $results['invalid_files']++;
                $results['violations'][] = "File integrity violation: {$file}";
            } else {
                $results['valid_files']++;
            }
        }
        
        return $results;
    }
    
    /**
     * Check integrity of specific file
     */
    public static function checkFileIntegrity(string $file, array $integrityData = null): bool
    {
        $filePath = self::getProjectRoot() . '/' . $file;
        
        if (!file_exists($filePath)) {
            return false;
        }
        
        if ($integrityData === null) {
            $integrityData = self::loadIntegrityData();
        }
        
        if (!$integrityData || !isset($integrityData['files'][$file])) {
            return false;
        }
        
        $expectedHash = $integrityData['files'][$file]['hash'];
        $currentHash = hash_file(self::$hashAlgorithm, $filePath);
        
        return $expectedHash === $currentHash;
    }
    
    /**
     * Generate integrity file with current file hashes
     */
    public static function generateIntegrityFile(): bool
    {
        $integrityData = [
            'generated' => date('Y-m-d H:i:s'),
            'algorithm' => self::$hashAlgorithm,
            'files' => []
        ];
        
        foreach (self::$criticalFiles as $file) {
            $filePath = self::getProjectRoot() . '/' . $file;
            
            if (file_exists($filePath)) {
                $integrityData['files'][$file] = [
                    'hash' => hash_file(self::$hashAlgorithm, $filePath),
                    'size' => filesize($filePath),
                    'modified' => date('Y-m-d H:i:s', filemtime($filePath))
                ];
            }
        }
        
        // Ensure directory exists
        $dir = dirname(self::$integrityFile);
        if (!is_dir($dir)) {
            mkdir($dir, 0700, true);
        }
        
        $result = file_put_contents(self::$integrityFile, json_encode($integrityData, JSON_PRETTY_PRINT));
        
        if ($result !== false) {
            chmod(self::$integrityFile, 0600);
            return true;
        }
        
        return false;
    }
    
    /**
     * Update integrity file for specific file
     */
    public static function updateFileIntegrity(string $file): bool
    {
        $filePath = self::getProjectRoot() . '/' . $file;
        
        if (!file_exists($filePath)) {
            return false;
        }
        
        $integrityData = self::loadIntegrityData();
        if (!$integrityData) {
            $integrityData = [
                'generated' => date('Y-m-d H:i:s'),
                'algorithm' => self::$hashAlgorithm,
                'files' => []
            ];
        }
        
        $integrityData['files'][$file] = [
            'hash' => hash_file(self::$hashAlgorithm, $filePath),
            'size' => filesize($filePath),
            'modified' => date('Y-m-d H:i:s', filemtime($filePath))
        ];
        
        $result = file_put_contents(self::$integrityFile, json_encode($integrityData, JSON_PRETTY_PRINT));
        
        if ($result !== false) {
            chmod(self::$integrityFile, 0600);
            return true;
        }
        
        return false;
    }
    
    /**
     * Verify file signature
     */
    public static function verifySignature(string $file, string $signature): bool
    {
        $filePath = self::getProjectRoot() . '/' . $file;
        
        if (!file_exists($filePath)) {
            return false;
        }
        
        $content = file_get_contents($filePath);
        $expectedSignature = hash_hmac(self::$hashAlgorithm, $content, self::getSecretKey());
        
        return hash_equals($expectedSignature, $signature);
    }
    
    /**
     * Generate file signature
     */
    public static function generateSignature(string $file): string
    {
        $filePath = self::getProjectRoot() . '/' . $file;
        
        if (!file_exists($filePath)) {
            throw new \Exception("File not found: {$file}");
        }
        
        $content = file_get_contents($filePath);
        return hash_hmac(self::$hashAlgorithm, $content, self::getSecretKey());
    }
    
    /**
     * Check for suspicious modifications
     */
    public static function checkForSuspiciousModifications(): array
    {
        $suspicious = [];
        
        foreach (self::$criticalFiles as $file) {
            $filePath = self::getProjectRoot() . '/' . $file;
            
            if (!file_exists($filePath)) {
                $suspicious[] = [
                    'file' => $file,
                    'issue' => 'File missing',
                    'severity' => 'high'
                ];
                continue;
            }
            
            $content = file_get_contents($filePath);
            
            // Check for suspicious patterns
            $suspiciousPatterns = [
                '/eval\s*\(/i' => 'eval() function detected',
                '/base64_decode\s*\(/i' => 'base64_decode() function detected',
                '/system\s*\(/i' => 'system() function detected',
                '/shell_exec\s*\(/i' => 'shell_exec() function detected',
                '/file_get_contents\s*\(\s*[\'"]https?:\/\//i' => 'Remote file inclusion detected'
            ];
            
            foreach ($suspiciousPatterns as $pattern => $description) {
                if (preg_match($pattern, $content)) {
                    $suspicious[] = [
                        'file' => $file,
                        'issue' => $description,
                        'severity' => 'high'
                    ];
                }
            }
            
            // Check file permissions
            $perms = fileperms($filePath);
            if (($perms & 0x0177) !== 0) {
                $suspicious[] = [
                    'file' => $file,
                    'issue' => 'Insecure file permissions',
                    'severity' => 'medium'
                ];
            }
        }
        
        return $suspicious;
    }
    
    /**
     * Load integrity data
     */
    private static function loadIntegrityData(): ?array
    {
        if (!file_exists(self::$integrityFile)) {
            return null;
        }
        
        $content = file_get_contents(self::$integrityFile);
        if ($content === false) {
            return null;
        }
        
        $data = json_decode($content, true);
        if (!$data) {
            return null;
        }
        
        return $data;
    }
    
    /**
     * Get project root directory
     */
    private static function getProjectRoot(): string
    {
        return dirname(__DIR__, 2);
    }
    
    /**
     * Get secret key for signatures
     */
    private static function getSecretKey(): string
    {
        $keyFile = '/var/lib/tusklang/.integrity_key';
        
        if (file_exists($keyFile)) {
            return trim(file_get_contents($keyFile));
        }
        
        // Generate new secret key
        $key = bin2hex(random_bytes(32));
        
        // Ensure directory exists
        $dir = dirname($keyFile);
        if (!is_dir($dir)) {
            mkdir($dir, 0700, true);
        }
        
        // Save secret key
        file_put_contents($keyFile, $key);
        chmod($keyFile, 0600);
        
        return $key;
    }
    
    /**
     * Get integrity statistics
     */
    public static function getStats(): array
    {
        $integrityData = self::loadIntegrityData();
        
        return [
            'total_files' => count(self::$criticalFiles),
            'monitored_files' => $integrityData ? count($integrityData['files']) : 0,
            'integrity_file_exists' => file_exists(self::$integrityFile),
            'last_generated' => $integrityData['generated'] ?? 'Never',
            'algorithm' => self::$hashAlgorithm
        ];
    }
    
    /**
     * Validate all files and log violations
     */
    public static function validateAndLog(): bool
    {
        $results = self::checkIntegrity();
        
        if (!$results['valid']) {
            self::logViolations($results['violations']);
        }
        
        return $results['valid'];
    }
    
    /**
     * Log integrity violations
     */
    private static function logViolations(array $violations): void
    {
        $logData = [
            'timestamp' => date('Y-m-d H:i:s'),
            'violations' => $violations,
            'hostname' => gethostname(),
            'installation_id' => self::getInstallationId()
        ];
        
        $logFile = sys_get_temp_dir() . '/tusklang_integrity_violations.log';
        file_put_contents($logFile, json_encode($logData) . "\n", FILE_APPEND | LOCK_EX);
        
        // Report to API
        try {
            $client = new \GuzzleHttp\Client();
            $client->post('https://lic.tusklang.org/api/v1/integrity-violation', [
                'json' => $logData
            ]);
        } catch (\Exception $e) {
            // Silent fail for violation reporting
        }
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
        
        $id = 'INTEGRITY-' . strtoupper(substr(md5(uniqid(mt_rand(), true)), 0, 12));
        file_put_contents($idFile, $id);
        
        return $id;
    }
} 