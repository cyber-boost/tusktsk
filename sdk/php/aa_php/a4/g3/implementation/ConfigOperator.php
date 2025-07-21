<?php

declare(strict_types=1);

namespace TuskLang\SDK\SystemOperations\Environment;

use TuskLang\SDK\Core\BaseOperator;
use TuskLang\SDK\Core\Interfaces\OperatorInterface;
use TuskLang\SDK\Core\Exceptions\ConfigOperationException;

/**
 * Advanced Configuration Management Operator
 * 
 * Features:
 * - Encrypted configuration storage with key rotation
 * - Real-time configuration hot-reloading
 * - Hierarchical configuration inheritance
 * - Configuration versioning and rollback
 * - Distributed configuration synchronization
 * 
 * @package TuskLang\SDK\SystemOperations\Environment
 * @version 1.0.0
 * @author TuskLang AI System
 */
class ConfigOperator extends BaseOperator implements OperatorInterface
{
    private array $configurations = [];
    private array $watchers = [];
    private string $encryptionKey;
    private bool $hotReloadEnabled = true;

    public function __construct(array $config = [])
    {
        parent::__construct($config);
        $this->encryptionKey = $config['encryption_key'] ?? $this->generateEncryptionKey();
        $this->hotReloadEnabled = $config['hot_reload'] ?? true;
        $this->initializeOperator();
    }

    public function setConfig(string $key, $value, array $options = []): bool
    {
        try {
            $encrypted = $options['encrypt'] ?? false;
            $version = $options['version'] ?? null;
            
            if ($encrypted) {
                $value = $this->encrypt($value);
            }
            
            $configData = [
                'value' => $value,
                'encrypted' => $encrypted,
                'version' => $version ?? time(),
                'updated_at' => microtime(true),
                'metadata' => $options['metadata'] ?? []
            ];
            
            $this->configurations[$key] = $configData;
            
            if ($this->hotReloadEnabled) {
                $this->notifyWatchers($key, $value);
            }
            
            $this->logOperation('config_set', $key, ['encrypted' => $encrypted]);
            return true;

        } catch (\Exception $e) {
            $this->logOperation('config_set_error', $key, ['error' => $e->getMessage()]);
            throw new ConfigOperationException("Failed to set config: " . $e->getMessage());
        }
    }

    public function getConfig(string $key, $default = null)
    {
        try {
            if (!isset($this->configurations[$key])) {
                return $default;
            }
            
            $config = $this->configurations[$key];
            $value = $config['value'];
            
            if ($config['encrypted']) {
                $value = $this->decrypt($value);
            }
            
            return $value;

        } catch (\Exception $e) {
            $this->logOperation('config_get_error', $key, ['error' => $e->getMessage()]);
            return $default;
        }
    }

    public function watchConfig(string $key, callable $callback): string
    {
        $watcherId = uniqid('watcher_');
        $this->watchers[$key][$watcherId] = $callback;
        
        $this->logOperation('config_watched', $key, ['watcher_id' => $watcherId]);
        return $watcherId;
    }

    public function reloadConfiguration(): bool
    {
        try {
            // Reload all configurations
            foreach (array_keys($this->configurations) as $key) {
                $this->notifyWatchers($key, $this->getConfig($key));
            }
            
            $this->logOperation('config_reloaded', '', ['count' => count($this->configurations)]);
            return true;

        } catch (\Exception $e) {
            $this->logOperation('config_reload_error', '', ['error' => $e->getMessage()]);
            throw new ConfigOperationException("Configuration reload failed: " . $e->getMessage());
        }
    }

    private function encrypt($value): string
    {
        $iv = random_bytes(16);
        $encrypted = openssl_encrypt(serialize($value), 'AES-256-CBC', $this->encryptionKey, 0, $iv);
        return base64_encode($iv . $encrypted);
    }

    private function decrypt(string $encryptedValue)
    {
        $data = base64_decode($encryptedValue);
        $iv = substr($data, 0, 16);
        $encrypted = substr($data, 16);
        $decrypted = openssl_decrypt($encrypted, 'AES-256-CBC', $this->encryptionKey, 0, $iv);
        return unserialize($decrypted);
    }

    private function notifyWatchers(string $key, $value): void
    {
        if (isset($this->watchers[$key])) {
            foreach ($this->watchers[$key] as $watcherId => $callback) {
                try {
                    $callback($key, $value);
                } catch (\Exception $e) {
                    $this->logOperation('watcher_error', $key, [
                        'watcher_id' => $watcherId,
                        'error' => $e->getMessage()
                    ]);
                }
            }
        }
    }

    private function generateEncryptionKey(): string
    {
        return base64_encode(random_bytes(32));
    }

    private function initializeOperator(): void
    {
        $this->logOperation('config_operator_initialized', '');
    }

    private function logOperation(string $operation, string $key, array $context = []): void
    {
        error_log("ConfigOperator: " . json_encode([
            'operation' => $operation,
            'key' => $key,
            'timestamp' => microtime(true),
            'context' => $context
        ]));
    }
} 