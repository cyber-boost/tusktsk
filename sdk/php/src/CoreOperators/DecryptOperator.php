<?php
/**
 * TuskLang Decrypt Operator
 * =========================
 * Handles data decryption operations
 */

namespace TuskLang\CoreOperators;

class DecryptOperator extends \TuskLang\CoreOperators\BaseOperator
{
    public function getName(): string
    {
        return 'decrypt';
    }

    protected function executeOperator(array $config, array $context): mixed
    {
        $algorithm = $config['algorithm'] ?? 'AES-256-CBC';
        $data = $config['data'] ?? null;
        $key = $config['key'] ?? null;
        $options = $config['options'] ?? [];

        if ($data === null) {
            throw new \Exception("Decrypt operator requires a 'data' parameter");
        }

        if ($key === null) {
            throw new \Exception("Decrypt operator requires a 'key' parameter");
        }

        switch ($algorithm) {
            case 'AES-256-CBC':
            case 'AES-192-CBC':
            case 'AES-128-CBC':
                return $this->decryptAES($data, $key, $algorithm, $options);

            case 'ChaCha20-Poly1305':
                return $this->decryptChaCha20($data, $key, $options);

            case 'XChaCha20-Poly1305':
                return $this->decryptXChaCha20($data, $key, $options);

            case 'AES-256-GCM':
            case 'AES-192-GCM':
            case 'AES-128-GCM':
                return $this->decryptAESGCM($data, $key, $algorithm, $options);

            case 'simple':
                return $this->simpleDecrypt($data, $key);

            default:
                throw new \Exception("Unknown decryption algorithm: $algorithm");
        }
    }

    private function decryptAES(string $data, string $key, string $algorithm, array $options): string
    {
        $method = strtolower(str_replace('-', '-', $algorithm));
        $ivLength = openssl_cipher_iv_length($method);
        
        $decoded = base64_decode($data);
        if ($decoded === false) {
            throw new \Exception("Invalid base64 data");
        }
        
        $iv = substr($decoded, 0, $ivLength);
        $encrypted = substr($decoded, $ivLength);
        
        $decrypted = openssl_decrypt($encrypted, $method, $key, OPENSSL_RAW_DATA, $iv);
        if ($decrypted === false) {
            throw new \Exception("Decryption failed");
        }
        
        return $decrypted;
    }

    private function decryptChaCha20(string $data, string $key, array $options): string
    {
        if (!function_exists('sodium_crypto_aead_chacha20poly1305_decrypt')) {
            throw new \Exception("ChaCha20-Poly1305 not available (requires sodium extension)");
        }
        
        $decoded = base64_decode($data);
        if ($decoded === false) {
            throw new \Exception("Invalid base64 data");
        }
        
        $nonce = substr($decoded, 0, 12);
        $encrypted = substr($decoded, 12);
        $additionalData = $options['additional_data'] ?? '';
        
        $decrypted = sodium_crypto_aead_chacha20poly1305_decrypt($encrypted, $additionalData, $nonce, $key);
        if ($decrypted === false) {
            throw new \Exception("Decryption failed");
        }
        
        return $decrypted;
    }

    private function decryptXChaCha20(string $data, string $key, array $options): string
    {
        if (!function_exists('sodium_crypto_aead_xchacha20poly1305_ietf_decrypt')) {
            throw new \Exception("XChaCha20-Poly1305 not available (requires sodium extension)");
        }
        
        $decoded = base64_decode($data);
        if ($decoded === false) {
            throw new \Exception("Invalid base64 data");
        }
        
        $nonce = substr($decoded, 0, 24);
        $encrypted = substr($decoded, 24);
        $additionalData = $options['additional_data'] ?? '';
        
        $decrypted = sodium_crypto_aead_xchacha20poly1305_ietf_decrypt($encrypted, $additionalData, $nonce, $key);
        if ($decrypted === false) {
            throw new \Exception("Decryption failed");
        }
        
        return $decrypted;
    }

    private function decryptAESGCM(string $data, string $key, string $algorithm, array $options): string
    {
        $method = strtolower(str_replace('-', '-', $algorithm));
        $ivLength = openssl_cipher_iv_length($method);
        $tagLength = 16; // GCM tag is always 16 bytes
        
        $decoded = base64_decode($data);
        if ($decoded === false) {
            throw new \Exception("Invalid base64 data");
        }
        
        $iv = substr($decoded, 0, $ivLength);
        $tag = substr($decoded, $ivLength, $tagLength);
        $encrypted = substr($decoded, $ivLength + $tagLength);
        
        $decrypted = openssl_decrypt($encrypted, $method, $key, OPENSSL_RAW_DATA, $iv, $tag);
        if ($decrypted === false) {
            throw new \Exception("Decryption failed");
        }
        
        return $decrypted;
    }

    private function simpleDecrypt(string $data, string $key): string
    {
        $key = hash('sha256', $key, true);
        $ivLength = openssl_cipher_iv_length('AES-256-CBC');
        
        $decoded = base64_decode($data);
        if ($decoded === false) {
            throw new \Exception("Invalid base64 data");
        }
        
        $iv = substr($decoded, 0, $ivLength);
        $encrypted = substr($decoded, $ivLength);
        
        $decrypted = openssl_decrypt($encrypted, 'AES-256-CBC', $key, OPENSSL_RAW_DATA, $iv);
        if ($decrypted === false) {
            throw new \Exception("Decryption failed");
        }
        
        return $decrypted;
    }
} 