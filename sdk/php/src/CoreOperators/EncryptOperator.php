<?php
/**
 * TuskLang Encrypt Operator
 * =========================
 * Handles data encryption operations
 */

namespace TuskLang\CoreOperators;

class EncryptOperator extends \TuskLang\CoreOperators\BaseOperator
{
    public function getName(): string
    {
        return 'encrypt';
    }

    protected function executeOperator(array $config, array $context): mixed
    {
        $algorithm = $config['algorithm'] ?? 'AES-256-CBC';
        $data = $config['data'] ?? null;
        $key = $config['key'] ?? null;
        $options = $config['options'] ?? [];

        if ($data === null) {
            throw new \Exception("Encrypt operator requires a 'data' parameter");
        }

        if ($key === null) {
            throw new \Exception("Encrypt operator requires a 'key' parameter");
        }

        switch ($algorithm) {
            case 'AES-256-CBC':
            case 'AES-192-CBC':
            case 'AES-128-CBC':
                return $this->encryptAES($data, $key, $algorithm, $options);

            case 'ChaCha20-Poly1305':
                return $this->encryptChaCha20($data, $key, $options);

            case 'XChaCha20-Poly1305':
                return $this->encryptXChaCha20($data, $key, $options);

            case 'AES-256-GCM':
            case 'AES-192-GCM':
            case 'AES-128-GCM':
                return $this->encryptAESGCM($data, $key, $algorithm, $options);

            case 'simple':
                return $this->simpleEncrypt($data, $key);

            default:
                throw new \Exception("Unknown encryption algorithm: $algorithm");
        }
    }

    private function encryptAES(string $data, string $key, string $algorithm, array $options): string
    {
        $method = strtolower(str_replace('-', '-', $algorithm));
        $ivLength = openssl_cipher_iv_length($method);
        $iv = $options['iv'] ?? openssl_random_pseudo_bytes($ivLength);
        
        $encrypted = openssl_encrypt($data, $method, $key, OPENSSL_RAW_DATA, $iv);
        if ($encrypted === false) {
            throw new \Exception("Encryption failed");
        }
        
        return base64_encode($iv . $encrypted);
    }

    private function encryptChaCha20(string $data, string $key, array $options): string
    {
        if (!function_exists('sodium_crypto_aead_chacha20poly1305_encrypt')) {
            throw new \Exception("ChaCha20-Poly1305 not available (requires sodium extension)");
        }
        
        $nonce = $options['nonce'] ?? random_bytes(12);
        $additionalData = $options['additional_data'] ?? '';
        
        $encrypted = sodium_crypto_aead_chacha20poly1305_encrypt($data, $additionalData, $nonce, $key);
        return base64_encode($nonce . $encrypted);
    }

    private function encryptXChaCha20(string $data, string $key, array $options): string
    {
        if (!function_exists('sodium_crypto_aead_xchacha20poly1305_ietf_encrypt')) {
            throw new \Exception("XChaCha20-Poly1305 not available (requires sodium extension)");
        }
        
        $nonce = $options['nonce'] ?? random_bytes(24);
        $additionalData = $options['additional_data'] ?? '';
        
        $encrypted = sodium_crypto_aead_xchacha20poly1305_ietf_encrypt($data, $additionalData, $nonce, $key);
        return base64_encode($nonce . $encrypted);
    }

    private function encryptAESGCM(string $data, string $key, string $algorithm, array $options): string
    {
        $method = strtolower(str_replace('-', '-', $algorithm));
        $ivLength = openssl_cipher_iv_length($method);
        $iv = $options['iv'] ?? openssl_random_pseudo_bytes($ivLength);
        $tag = '';
        
        $encrypted = openssl_encrypt($data, $method, $key, OPENSSL_RAW_DATA, $iv, $tag);
        if ($encrypted === false) {
            throw new \Exception("Encryption failed");
        }
        
        return base64_encode($iv . $tag . $encrypted);
    }

    private function simpleEncrypt(string $data, string $key): string
    {
        $key = hash('sha256', $key, true);
        $ivLength = openssl_cipher_iv_length('AES-256-CBC');
        $iv = openssl_random_pseudo_bytes($ivLength);
        
        $encrypted = openssl_encrypt($data, 'AES-256-CBC', $key, OPENSSL_RAW_DATA, $iv);
        if ($encrypted === false) {
            throw new \Exception("Encryption failed");
        }
        
        return base64_encode($iv . $encrypted);
    }
} 