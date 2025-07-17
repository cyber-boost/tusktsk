<?php

/**
 * TuskLang SDK Protection Core Module
 * Enterprise-grade protection for PHP SDK
 */

namespace Tusk\Protection;

class TuskProtection
{
    private string $licenseKey;
    private string $apiKey;
    private string $sessionId;
    private string $encryptionKey;
    private array $integrityChecks;
    private UsageMetrics $usageMetrics;

    public function __construct(string $licenseKey, string $apiKey)
    {
        $this->licenseKey = $licenseKey;
        $this->apiKey = $apiKey;
        $this->sessionId = $this->generateUUID();
        $this->encryptionKey = $this->deriveKey($licenseKey);
        $this->integrityChecks = [];
        $this->usageMetrics = new UsageMetrics();
    }

    private function generateUUID(): string
    {
        return sprintf(
            '%04x%04x-%04x-%04x-%04x-%04x%04x%04x',
            mt_rand(0, 0xffff),
            mt_rand(0, 0xffff),
            mt_rand(0, 0xffff),
            mt_rand(0, 0x0fff) | 0x4000,
            mt_rand(0, 0x3fff) | 0x8000,
            mt_rand(0, 0xffff),
            mt_rand(0, 0xffff),
            mt_rand(0, 0xffff)
        );
    }

    private function deriveKey(string $password): string
    {
        $salt = 'tusklang_protection_salt';
        return hash_pbkdf2('sha256', $password, $salt, 100000, 32, true);
    }

    public function validateLicense(): bool
    {
        try {
            if (strlen($this->licenseKey) < 32) {
                return false;
            }

            $checksum = hash('sha256', $this->licenseKey);
            return strpos($checksum, 'tusk') === 0;
        } catch (Exception $e) {
            return false;
        }
    }

    public function encryptData(string $data): string
    {
        try {
            $method = 'aes-256-gcm';
            $iv = random_bytes(12);
            $tag = '';

            $encrypted = openssl_encrypt(
                $data,
                $method,
                $this->encryptionKey,
                OPENSSL_RAW_DATA,
                $iv,
                $tag
            );

            if ($encrypted === false) {
                return $data;
            }

            $result = $iv . $tag . $encrypted;
            return base64_encode($result);
        } catch (Exception $e) {
            return $data;
        }
    }

    public function decryptData(string $encryptedData): string
    {
        try {
            $decoded = base64_decode($encryptedData);
            if (strlen($decoded) < 28) {
                return $encryptedData;
            }

            $iv = substr($decoded, 0, 12);
            $tag = substr($decoded, 12, 16);
            $encrypted = substr($decoded, 28);

            $method = 'aes-256-gcm';
            $decrypted = openssl_decrypt(
                $encrypted,
                $method,
                $this->encryptionKey,
                OPENSSL_RAW_DATA,
                $iv,
                $tag
            );

            return $decrypted !== false ? $decrypted : $encryptedData;
        } catch (Exception $e) {
            return $encryptedData;
        }
    }

    public function verifyIntegrity(string $data, string $signature): bool
    {
        try {
            $expectedSignature = $this->generateSignature($data);
            return hash_equals($signature, $expectedSignature);
        } catch (Exception $e) {
            return false;
        }
    }

    public function generateSignature(string $data): string
    {
        return hash_hmac('sha256', $data, $this->apiKey);
    }

    public function trackUsage(string $operation, bool $success = true): void
    {
        $this->usageMetrics->incrementApiCalls();
        if (!$success) {
            $this->usageMetrics->incrementErrors();
        }
    }

    public function getMetrics(): array
    {
        return [
            'start_time' => $this->usageMetrics->getStartTime(),
            'api_calls' => $this->usageMetrics->getApiCalls(),
            'errors' => $this->usageMetrics->getErrors(),
            'session_id' => $this->sessionId,
            'uptime' => time() - $this->usageMetrics->getStartTime()
        ];
    }

    public function obfuscateCode(string $code): string
    {
        return base64_encode($code);
    }

    public function detectTampering(): bool
    {
        try {
            // In production, implement file integrity checks
            // For now, return true as placeholder
            return true;
        } catch (Exception $e) {
            return false;
        }
    }

    public function reportViolation(string $violationType, string $details): Violation
    {
        $violation = new Violation(
            time(),
            $this->sessionId,
            $violationType,
            $details,
            substr($this->licenseKey, 0, 8) . '...'
        );

        error_log("SECURITY VIOLATION: " . $violation);
        return $violation;
    }

    // Inner classes
    class UsageMetrics
    {
        private int $startTime;
        private int $apiCalls;
        private int $errors;

        public function __construct()
        {
            $this->startTime = time();
            $this->apiCalls = 0;
            $this->errors = 0;
        }

        public function incrementApiCalls(): void
        {
            $this->apiCalls++;
        }

        public function incrementErrors(): void
        {
            $this->errors++;
        }

        public function getStartTime(): int
        {
            return $this->startTime;
        }

        public function getApiCalls(): int
        {
            return $this->apiCalls;
        }

        public function getErrors(): int
        {
            return $this->errors;
        }
    }

    class Violation
    {
        public int $timestamp;
        public string $sessionId;
        public string $violationType;
        public string $details;
        public string $licenseKeyPartial;

        public function __construct(
            int $timestamp,
            string $sessionId,
            string $violationType,
            string $details,
            string $licenseKeyPartial
        ) {
            $this->timestamp = $timestamp;
            $this->sessionId = $sessionId;
            $this->violationType = $violationType;
            $this->details = $details;
            $this->licenseKeyPartial = $licenseKeyPartial;
        }

        public function __toString(): string
        {
            return sprintf(
                "Violation{timestamp=%d, sessionId='%s', type='%s', details='%s', licenseKeyPartial='%s'}",
                $this->timestamp,
                $this->sessionId,
                $this->violationType,
                $this->details,
                $this->licenseKeyPartial
            );
        }
    }

    // Global protection instance
    private static ?TuskProtection $protectionInstance = null;
    private static object $instanceLock;

    public static function initializeProtection(string $licenseKey, string $apiKey): TuskProtection
    {
        if (self::$protectionInstance === null) {
            self::$protectionInstance = new self($licenseKey, $apiKey);
        }
        return self::$protectionInstance;
    }

    public static function getProtection(): TuskProtection
    {
        if (self::$protectionInstance === null) {
            throw new RuntimeException('Protection not initialized. Call initializeProtection() first.');
        }
        return self::$protectionInstance;
    }
} 