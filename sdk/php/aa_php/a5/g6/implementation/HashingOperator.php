<?php

declare(strict_types=1);

namespace TuskLang\A5\G6;

use TuskLang\CoreOperator;
use InvalidArgumentException;

/**
 * HashingOperator - Cryptographic hashing and security operations
 * 
 * Provides comprehensive cryptographic hashing operations including various hash algorithms,
 * password hashing, HMAC, key derivation, digital signatures, and security utilities.
 */
class HashingOperator extends CoreOperator
{
    public function getName(): string
    {
        return 'hashing';
    }

    public function getDescription(): string 
    {
        return 'Cryptographic hashing and security operations';
    }

    public function getSupportedActions(): array
    {
        return [
            'hash', 'verify', 'hmac', 'password_hash', 'password_verify',
            'pbkdf2', 'scrypt', 'argon2', 'checksum', 'compare_hash',
            'available_algorithms', 'benchmark', 'secure_compare', 'random_salt'
        ];
    }

    public function execute(string $action, array $params = []): mixed
    {
        return match($action) {
            'hash' => $this->hash($params['data'] ?? '', $params['algorithm'] ?? 'sha256', $params['options'] ?? []),
            'verify' => $this->verifyHash($params['data'] ?? '', $params['hash'] ?? '', $params['algorithm'] ?? 'sha256'),
            'hmac' => $this->hmac($params['data'] ?? '', $params['key'] ?? '', $params['algorithm'] ?? 'sha256'),
            'password_hash' => $this->passwordHash($params['password'] ?? '', $params['algorithm'] ?? 'argon2id', $params['options'] ?? []),
            'password_verify' => $this->passwordVerify($params['password'] ?? '', $params['hash'] ?? ''),
            'pbkdf2' => $this->pbkdf2($params['password'] ?? '', $params['salt'] ?? '', $params['iterations'] ?? 10000, $params['algorithm'] ?? 'sha256'),
            'scrypt' => $this->scrypt($params['password'] ?? '', $params['salt'] ?? '', $params['options'] ?? []),
            'argon2' => $this->argon2($params['password'] ?? '', $params['salt'] ?? '', $params['options'] ?? []),
            'checksum' => $this->checksum($params['data'] ?? '', $params['algorithm'] ?? 'crc32'),
            'compare_hash' => $this->compareHashes($params['hash1'] ?? '', $params['hash2'] ?? ''),
            'available_algorithms' => $this->getAvailableAlgorithms(),
            'benchmark' => $this->benchmarkAlgorithms($params['data'] ?? 'benchmark test'),
            'secure_compare' => $this->secureCompare($params['string1'] ?? '', $params['string2'] ?? ''),
            'random_salt' => $this->generateRandomSalt($params['length'] ?? 32),
            default => throw new InvalidArgumentException("Unsupported action: {$action}")
        };
    }

    /**
     * Generate hash using specified algorithm
     */
    private function hash(string $data, string $algorithm, array $options = []): array
    {
        if (empty($data)) {
            throw new InvalidArgumentException('Data cannot be empty');
        }

        // Validate algorithm
        if (!in_array($algorithm, hash_algos())) {
            throw new InvalidArgumentException("Unsupported hash algorithm: {$algorithm}");
        }

        $binary = $options['binary'] ?? false;
        $key = $options['key'] ?? null;
        $salt = $options['salt'] ?? '';
        
        // Add salt if provided
        $saltedData = $salt . $data;
        
        // Generate hash
        if ($key !== null) {
            // Use HMAC if key provided
            $hash = hash_hmac($algorithm, $saltedData, $key, $binary);
        } else {
            $hash = hash($algorithm, $saltedData, $binary);
        }

        return [
            'algorithm' => $algorithm,
            'data_length' => strlen($data),
            'hash' => $hash,
            'salt' => $salt,
            'binary' => $binary,
            'hash_length' => strlen($hash),
            'hex_length' => $binary ? strlen(bin2hex($hash)) : strlen($hash)
        ];
    }

    /**
     * Verify data against hash
     */
    private function verifyHash(string $data, string $expectedHash, string $algorithm): array
    {
        if (empty($data) || empty($expectedHash)) {
            throw new InvalidArgumentException('Data and hash cannot be empty');
        }

        $computedHash = hash($algorithm, $data);
        $isValid = hash_equals($expectedHash, $computedHash);

        return [
            'valid' => $isValid,
            'algorithm' => $algorithm,
            'expected_hash' => $expectedHash,
            'computed_hash' => $computedHash,
            'data_length' => strlen($data),
            'timing_safe' => true
        ];
    }

    /**
     * Generate HMAC
     */
    private function hmac(string $data, string $key, string $algorithm = 'sha256'): array
    {
        if (empty($data) || empty($key)) {
            throw new InvalidArgumentException('Data and key cannot be empty');
        }

        if (!in_array($algorithm, hash_hmac_algos())) {
            throw new InvalidArgumentException("Algorithm not supported for HMAC: {$algorithm}");
        }

        $hmac = hash_hmac($algorithm, $data, $key);
        
        return [
            'hmac' => $hmac,
            'algorithm' => $algorithm,
            'data_length' => strlen($data),
            'key_length' => strlen($key),
            'hmac_length' => strlen($hmac),
            'base64' => base64_encode(hash_hmac($algorithm, $data, $key, true))
        ];
    }

    /**
     * Hash password using modern algorithms
     */
    private function passwordHash(string $password, string $algorithm = 'argon2id', array $options = []): array
    {
        if (empty($password)) {
            throw new InvalidArgumentException('Password cannot be empty');
        }

        $phpAlgorithm = match($algorithm) {
            'bcrypt' => PASSWORD_BCRYPT,
            'argon2i' => PASSWORD_ARGON2I,
            'argon2id' => PASSWORD_ARGON2ID,
            'default' => PASSWORD_DEFAULT,
            default => throw new InvalidArgumentException("Unsupported password algorithm: {$algorithm}")
        };

        // Set default options based on algorithm
        $defaultOptions = match($algorithm) {
            'bcrypt' => ['cost' => 12],
            'argon2i', 'argon2id' => [
                'memory_cost' => 65536, // 64 MB
                'time_cost' => 4,
                'threads' => 3
            ],
            default => []
        };

        $finalOptions = array_merge($defaultOptions, $options);
        $hash = password_hash($password, $phpAlgorithm, $finalOptions);

        if ($hash === false) {
            throw new InvalidArgumentException('Password hashing failed');
        }

        return [
            'hash' => $hash,
            'algorithm' => $algorithm,
            'options' => $finalOptions,
            'hash_length' => strlen($hash),
            'info' => password_get_info($hash)
        ];
    }

    /**
     * Verify password against hash
     */
    private function passwordVerify(string $password, string $hash): array
    {
        if (empty($password) || empty($hash)) {
            throw new InvalidArgumentException('Password and hash cannot be empty');
        }

        $isValid = password_verify($password, $hash);
        $needsRehash = password_needs_rehash($hash, PASSWORD_DEFAULT);
        $info = password_get_info($hash);

        return [
            'valid' => $isValid,
            'needs_rehash' => $needsRehash,
            'algorithm_name' => $info['algoName'],
            'algorithm_options' => $info['options'],
            'timing_safe' => true
        ];
    }

    /**
     * PBKDF2 key derivation
     */
    private function pbkdf2(string $password, string $salt, int $iterations = 10000, string $algorithm = 'sha256'): array
    {
        if (empty($password)) {
            throw new InvalidArgumentException('Password cannot be empty');
        }

        if ($iterations < 1000) {
            throw new InvalidArgumentException('Iterations should be at least 1000 for security');
        }

        $keyLength = match($algorithm) {
            'md5' => 16,
            'sha1' => 20,
            'sha256' => 32,
            'sha512' => 64,
            default => 32
        };

        // Generate salt if not provided
        if (empty($salt)) {
            $salt = random_bytes(16);
        }

        $derivedKey = hash_pbkdf2($algorithm, $password, $salt, $iterations, $keyLength, false);

        return [
            'derived_key' => $derivedKey,
            'algorithm' => $algorithm,
            'iterations' => $iterations,
            'salt' => base64_encode($salt),
            'key_length' => $keyLength,
            'hex_length' => strlen($derivedKey)
        ];
    }

    /**
     * Scrypt key derivation
     */
    private function scrypt(string $password, string $salt, array $options = []): array
    {
        if (!extension_loaded('sodium')) {
            throw new InvalidArgumentException('Sodium extension required for scrypt');
        }

        if (empty($password)) {
            throw new InvalidArgumentException('Password cannot be empty');
        }

        $defaultOptions = [
            'N' => 32768, // CPU/memory cost
            'r' => 8,     // Block size
            'p' => 1,     // Parallelization
            'length' => 32
        ];

        $finalOptions = array_merge($defaultOptions, $options);

        // Generate salt if not provided
        if (empty($salt)) {
            $salt = random_bytes(32);
        }

        try {
            $derivedKey = sodium_crypto_pwhash_scryptsalsa208sha256(
                $finalOptions['length'],
                $password,
                $salt,
                $finalOptions['N'],
                $finalOptions['r'] * 128 * $finalOptions['p']
            );

            return [
                'derived_key' => bin2hex($derivedKey),
                'algorithm' => 'scrypt',
                'salt' => base64_encode($salt),
                'options' => $finalOptions,
                'key_length' => strlen($derivedKey)
            ];
        } catch (\Exception $e) {
            throw new InvalidArgumentException('Scrypt derivation failed: ' . $e->getMessage());
        }
    }

    /**
     * Argon2 key derivation
     */
    private function argon2(string $password, string $salt, array $options = []): array
    {
        if (empty($password)) {
            throw new InvalidArgumentException('Password cannot be empty');
        }

        $defaultOptions = [
            'memory_cost' => 65536, // 64 MB
            'time_cost' => 4,
            'threads' => 3,
            'length' => 32
        ];

        $finalOptions = array_merge($defaultOptions, $options);

        // Generate salt if not provided
        if (empty($salt)) {
            $salt = random_bytes(16);
        }

        $hash = password_hash($password, PASSWORD_ARGON2ID, [
            'memory_cost' => $finalOptions['memory_cost'],
            'time_cost' => $finalOptions['time_cost'],
            'threads' => $finalOptions['threads']
        ]);

        if ($hash === false) {
            throw new InvalidArgumentException('Argon2 hashing failed');
        }

        return [
            'hash' => $hash,
            'algorithm' => 'argon2id',
            'salt' => base64_encode($salt),
            'options' => $finalOptions,
            'info' => password_get_info($hash)
        ];
    }

    /**
     * Calculate checksum
     */
    private function checksum(string $data, string $algorithm = 'crc32'): array
    {
        if (empty($data)) {
            throw new InvalidArgumentException('Data cannot be empty');
        }

        $result = match($algorithm) {
            'crc32' => sprintf('%08x', crc32($data)),
            'crc32b' => hash('crc32b', $data),
            'adler32' => hash('adler32', $data),
            'md5' => md5($data),
            'sha1' => sha1($data),
            default => in_array($algorithm, hash_algos()) ? hash($algorithm, $data) : 
                      throw new InvalidArgumentException("Unsupported checksum algorithm: {$algorithm}")
        };

        return [
            'checksum' => $result,
            'algorithm' => $algorithm,
            'data_length' => strlen($data),
            'checksum_length' => strlen($result)
        ];
    }

    /**
     * Compare two hashes securely
     */
    private function compareHashes(string $hash1, string $hash2): array
    {
        $equal = hash_equals($hash1, $hash2);
        
        return [
            'equal' => $equal,
            'hash1_length' => strlen($hash1),
            'hash2_length' => strlen($hash2),
            'timing_safe' => true,
            'length_match' => strlen($hash1) === strlen($hash2)
        ];
    }

    /**
     * Get available hash algorithms
     */
    private function getAvailableAlgorithms(): array
    {
        $hashAlgorithms = hash_algos();
        $hmacAlgorithms = hash_hmac_algos();
        
        $categorized = [
            'cryptographic' => [],
            'checksums' => [],
            'legacy' => [],
            'hmac_supported' => $hmacAlgorithms
        ];

        foreach ($hashAlgorithms as $algo) {
            if (in_array($algo, ['sha224', 'sha256', 'sha384', 'sha512', 'sha3-224', 'sha3-256', 'sha3-384', 'sha3-512'])) {
                $categorized['cryptographic'][] = $algo;
            } elseif (in_array($algo, ['crc32', 'crc32b', 'adler32'])) {
                $categorized['checksums'][] = $algo;
            } elseif (in_array($algo, ['md4', 'md5', 'sha1'])) {
                $categorized['legacy'][] = $algo;
            }
        }

        return [
            'all_algorithms' => $hashAlgorithms,
            'categorized' => $categorized,
            'total_count' => count($hashAlgorithms),
            'recommended' => ['sha256', 'sha512', 'sha3-256', 'sha3-512']
        ];
    }

    /**
     * Benchmark hash algorithms
     */
    private function benchmarkAlgorithms(string $data): array
    {
        $algorithms = ['md5', 'sha1', 'sha256', 'sha512'];
        $results = [];
        
        foreach ($algorithms as $algorithm) {
            $start = microtime(true);
            
            // Run multiple iterations for better timing
            for ($i = 0; $i < 1000; $i++) {
                hash($algorithm, $data);
            }
            
            $end = microtime(true);
            $totalTime = ($end - $start) * 1000; // Convert to milliseconds
            
            $results[$algorithm] = [
                'total_time_ms' => round($totalTime, 4),
                'avg_time_ms' => round($totalTime / 1000, 6),
                'hashes_per_second' => round(1000 / $totalTime * 1000, 0)
            ];
        }

        // Sort by speed (fastest first)
        uasort($results, fn($a, $b) => $a['avg_time_ms'] <=> $b['avg_time_ms']);

        return [
            'data_length' => strlen($data),
            'iterations' => 1000,
            'results' => $results,
            'fastest' => array_key_first($results),
            'slowest' => array_key_last($results)
        ];
    }

    /**
     * Timing-safe string comparison
     */
    private function secureCompare(string $string1, string $string2): array
    {
        $equal = hash_equals($string1, $string2);
        
        return [
            'equal' => $equal,
            'string1_length' => strlen($string1),
            'string2_length' => strlen($string2),
            'timing_safe' => true,
            'constant_time' => true
        ];
    }

    /**
     * Generate cryptographically secure random salt
     */
    private function generateRandomSalt(int $length = 32): array
    {
        if ($length < 8 || $length > 1024) {
            throw new InvalidArgumentException('Salt length must be between 8 and 1024 bytes');
        }

        $salt = random_bytes($length);
        
        return [
            'salt' => $salt,
            'salt_hex' => bin2hex($salt),
            'salt_base64' => base64_encode($salt),
            'length' => $length,
            'entropy_bits' => $length * 8
        ];
    }

    /**
     * Generate multiple hash formats
     */
    public function multiHash(string $data, array $algorithms = ['md5', 'sha1', 'sha256']): array
    {
        if (empty($data)) {
            throw new InvalidArgumentException('Data cannot be empty');
        }

        $hashes = [];
        
        foreach ($algorithms as $algorithm) {
            if (in_array($algorithm, hash_algos())) {
                $hashes[$algorithm] = hash($algorithm, $data);
            }
        }

        return [
            'data_length' => strlen($data),
            'algorithms' => $algorithms,
            'hashes' => $hashes,
            'count' => count($hashes)
        ];
    }

    /**
     * File integrity verification
     */
    public function fileIntegrityHash(string $filePath, string $algorithm = 'sha256'): array
    {
        if (!file_exists($filePath)) {
            throw new InvalidArgumentException('File does not exist: ' . $filePath);
        }

        if (!is_readable($filePath)) {
            throw new InvalidArgumentException('File is not readable: ' . $filePath);
        }

        $start = microtime(true);
        $hash = hash_file($algorithm, $filePath);
        $end = microtime(true);

        if ($hash === false) {
            throw new InvalidArgumentException('Failed to hash file');
        }

        return [
            'file_path' => $filePath,
            'file_size' => filesize($filePath),
            'algorithm' => $algorithm,
            'hash' => $hash,
            'processing_time_ms' => round(($end - $start) * 1000, 4),
            'hash_rate_mbps' => round((filesize($filePath) / 1048576) / ($end - $start), 2)
        ];
    }

    /**
     * Password strength analysis
     */
    public function analyzePasswordStrength(string $password): array
    {
        $length = strlen($password);
        $hasLower = preg_match('/[a-z]/', $password);
        $hasUpper = preg_match('/[A-Z]/', $password);
        $hasDigits = preg_match('/\d/', $password);
        $hasSpecial = preg_match('/[^a-zA-Z\d]/', $password);
        $hasSpaces = str_contains($password, ' ');
        
        $score = 0;
        $recommendations = [];
        
        if ($length >= 8) $score += 1; else $recommendations[] = 'Use at least 8 characters';
        if ($length >= 12) $score += 1;
        if ($hasLower) $score += 1; else $recommendations[] = 'Include lowercase letters';
        if ($hasUpper) $score += 1; else $recommendations[] = 'Include uppercase letters';
        if ($hasDigits) $score += 1; else $recommendations[] = 'Include numbers';
        if ($hasSpecial) $score += 1; else $recommendations[] = 'Include special characters';
        if ($length >= 16) $score += 1;
        
        $strength = match(true) {
            $score >= 7 => 'very_strong',
            $score >= 5 => 'strong',
            $score >= 3 => 'moderate',
            $score >= 1 => 'weak',
            default => 'very_weak'
        };

        $entropy = $this->calculatePasswordEntropy($password);

        return [
            'password_length' => $length,
            'strength' => $strength,
            'score' => $score,
            'max_score' => 7,
            'entropy_bits' => round($entropy, 2),
            'characteristics' => [
                'has_lowercase' => (bool) $hasLower,
                'has_uppercase' => (bool) $hasUpper,
                'has_digits' => (bool) $hasDigits,
                'has_special' => (bool) $hasSpecial,
                'has_spaces' => $hasSpaces
            ],
            'recommendations' => $recommendations,
            'estimated_crack_time' => $this->estimateCrackTime($entropy)
        ];
    }

    /**
     * Calculate password entropy
     */
    private function calculatePasswordEntropy(string $password): float
    {
        $charsets = [
            'lowercase' => 26,
            'uppercase' => 26,
            'digits' => 10,
            'special' => 32, // Common special characters
            'space' => 1
        ];
        
        $charsetSize = 0;
        
        if (preg_match('/[a-z]/', $password)) $charsetSize += $charsets['lowercase'];
        if (preg_match('/[A-Z]/', $password)) $charsetSize += $charsets['uppercase'];
        if (preg_match('/\d/', $password)) $charsetSize += $charsets['digits'];
        if (preg_match('/[^a-zA-Z\d ]/', $password)) $charsetSize += $charsets['special'];
        if (str_contains($password, ' ')) $charsetSize += $charsets['space'];
        
        return strlen($password) * log($charsetSize, 2);
    }

    /**
     * Estimate password crack time
     */
    private function estimateCrackTime(float $entropy): array
    {
        // Assume 1 billion guesses per second
        $guessesPerSecond = 1e9;
        $totalCombinations = pow(2, $entropy);
        $avgCrackTime = $totalCombinations / (2 * $guessesPerSecond);
        
        $timeUnits = [
            ['years', 365 * 24 * 3600],
            ['months', 30 * 24 * 3600],
            ['days', 24 * 3600],
            ['hours', 3600],
            ['minutes', 60],
            ['seconds', 1]
        ];
        
        foreach ($timeUnits as [$unit, $seconds]) {
            if ($avgCrackTime >= $seconds) {
                $value = $avgCrackTime / $seconds;
                return [
                    'value' => round($value, 2),
                    'unit' => $unit,
                    'description' => round($value, 2) . ' ' . $unit
                ];
            }
        }
        
        return ['value' => 0, 'unit' => 'instant', 'description' => 'Instant'];
    }

    /**
     * Generate secure token
     */
    public function generateSecureToken(int $length = 32, string $encoding = 'hex'): array
    {
        if ($length < 16 || $length > 1024) {
            throw new InvalidArgumentException('Token length must be between 16 and 1024');
        }

        $bytes = random_bytes($length);
        
        $token = match($encoding) {
            'hex' => bin2hex($bytes),
            'base64' => base64_encode($bytes),
            'base64url' => rtrim(strtr(base64_encode($bytes), '+/', '-_'), '='),
            'raw' => $bytes,
            default => throw new InvalidArgumentException("Unsupported encoding: {$encoding}")
        };

        return [
            'token' => $token,
            'length' => $length,
            'encoding' => $encoding,
            'entropy_bits' => $length * 8,
            'url_safe' => $encoding === 'base64url'
        ];
    }
} 