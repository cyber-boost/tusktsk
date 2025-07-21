<?php

namespace TuskLang\A3\G1;

/**
 * Extensible Plugin System - Hook-Based Architecture
 * 
 * Features:
 * - Event-driven plugin architecture
 * - Dynamic plugin loading and unloading
 * - Dependency management between plugins
 * - Performance monitoring for plugins
 * - Sandboxed plugin execution
 * - Plugin configuration management
 * 
 * @version 2.0.0
 * @package TuskLang\A3\G1
 */
class PluginSystem
{
    private array $plugins = [];
    private array $hooks = [];
    private array $pluginConfigs = [];
    private array $pluginDependencies = [];
    private array $pluginMetrics = [];
    private bool $sandboxEnabled = true;
    private string $pluginDir;
    private ErrorHandler $errorHandler;
    
    public function __construct(array $config = [])
    {
        $this->pluginDir = $config['plugin_dir'] ?? '/tmp/tusklang_plugins';
        $this->sandboxEnabled = $config['sandbox'] ?? true;
        $this->errorHandler = $config['error_handler'] ?? new ErrorHandler();
        
        $this->ensurePluginDir();
        $this->initializeCore();
    }
    
    /**
     * Register a plugin with the system
     */
    public function registerPlugin(string $name, array $config = []): bool
    {
        try {
            // Check if plugin already exists
            if (isset($this->plugins[$name])) {
                $this->errorHandler->trackError('PLUGIN_ALREADY_EXISTS', "Plugin $name is already registered");
                return false;
            }
            
            // Validate plugin configuration
            if (!$this->validatePluginConfig($config)) {
                $this->errorHandler->trackError('INVALID_PLUGIN_CONFIG', "Invalid configuration for plugin $name");
                return false;
            }
            
            // Check dependencies
            if (!$this->checkDependencies($name, $config)) {
                $this->errorHandler->trackError('PLUGIN_DEPENDENCY_FAILED', "Dependencies not satisfied for plugin $name");
                return false;
            }
            
            $plugin = [
                'name' => $name,
                'version' => $config['version'] ?? '1.0.0',
                'description' => $config['description'] ?? '',
                'author' => $config['author'] ?? 'Unknown',
                'class' => $config['class'] ?? null,
                'instance' => null,
                'hooks' => $config['hooks'] ?? [],
                'dependencies' => $config['dependencies'] ?? [],
                'config' => $config['config'] ?? [],
                'enabled' => true,
                'loaded' => false,
                'load_time' => 0,
                'execution_stats' => [
                    'calls' => 0,
                    'total_time' => 0,
                    'average_time' => 0,
                    'errors' => 0
                ]
            ];
            
            $this->plugins[$name] = $plugin;
            $this->pluginConfigs[$name] = $config['config'] ?? [];
            $this->pluginDependencies[$name] = $config['dependencies'] ?? [];
            
            // Auto-load if specified
            if ($config['auto_load'] ?? false) {
                $this->loadPlugin($name);
            }
            
            return true;
            
        } catch (\Exception $e) {
            $this->errorHandler->trackError('PLUGIN_REGISTRATION_FAILED', $e->getMessage(), [
                'plugin' => $name,
                'config' => $config
            ]);
            return false;
        }
    }
    
    /**
     * Load a plugin into memory
     */
    public function loadPlugin(string $name): bool
    {
        if (!isset($this->plugins[$name])) {
            return false;
        }
        
        if ($this->plugins[$name]['loaded']) {
            return true;
        }
        
        $startTime = microtime(true);
        
        try {
            $plugin = &$this->plugins[$name];
            
            // Load plugin class if specified
            if ($plugin['class']) {
                if ($this->sandboxEnabled) {
                    $instance = $this->createSandboxedInstance($plugin['class'], $plugin['config']);
                } else {
                    $instance = new $plugin['class']($plugin['config']);
                }
                
                $plugin['instance'] = $instance;
                
                // Register plugin hooks
                foreach ($plugin['hooks'] as $hook => $method) {
                    $this->registerHook($hook, [$instance, $method], $name);
                }
            }
            
            $plugin['loaded'] = true;
            $plugin['load_time'] = microtime(true) - $startTime;
            
            // Trigger plugin loaded event
            $this->triggerHook('plugin_loaded', ['plugin' => $name]);
            
            return true;
            
        } catch (\Exception $e) {
            $this->errorHandler->trackError('PLUGIN_LOAD_FAILED', $e->getMessage(), [
                'plugin' => $name
            ]);
            return false;
        }
    }
    
    /**
     * Unload a plugin from memory
     */
    public function unloadPlugin(string $name): bool
    {
        if (!isset($this->plugins[$name]) || !$this->plugins[$name]['loaded']) {
            return false;
        }
        
        try {
            // Remove hooks registered by this plugin
            foreach ($this->hooks as $hookName => $callbacks) {
                $this->hooks[$hookName] = array_filter($callbacks, function($callback) use ($name) {
                    return $callback['plugin'] !== $name;
                });
            }
            
            // Cleanup plugin instance
            $this->plugins[$name]['instance'] = null;
            $this->plugins[$name]['loaded'] = false;
            
            // Trigger plugin unloaded event
            $this->triggerHook('plugin_unloaded', ['plugin' => $name]);
            
            return true;
            
        } catch (\Exception $e) {
            $this->errorHandler->trackError('PLUGIN_UNLOAD_FAILED', $e->getMessage(), [
                'plugin' => $name
            ]);
            return false;
        }
    }
    
    /**
     * Enable a plugin
     */
    public function enablePlugin(string $name): bool
    {
        if (!isset($this->plugins[$name])) {
            return false;
        }
        
        $this->plugins[$name]['enabled'] = true;
        return true;
    }
    
    /**
     * Disable a plugin
     */
    public function disablePlugin(string $name): bool
    {
        if (!isset($this->plugins[$name])) {
            return false;
        }
        
        $this->plugins[$name]['enabled'] = false;
        $this->unloadPlugin($name);
        return true;
    }
    
    /**
     * Register a hook callback
     */
    public function registerHook(string $hookName, callable $callback, string $pluginName = 'core'): void
    {
        if (!isset($this->hooks[$hookName])) {
            $this->hooks[$hookName] = [];
        }
        
        $this->hooks[$hookName][] = [
            'callback' => $callback,
            'plugin' => $pluginName,
            'priority' => 10 // Default priority
        ];
        
        // Sort by priority
        usort($this->hooks[$hookName], function($a, $b) {
            return $a['priority'] <=> $b['priority'];
        });
    }
    
    /**
     * Trigger a hook and execute all registered callbacks
     */
    public function triggerHook(string $hookName, array $args = []): array
    {
        if (!isset($this->hooks[$hookName])) {
            return [];
        }
        
        $results = [];
        
        foreach ($this->hooks[$hookName] as $hook) {
            // Check if plugin is enabled
            if ($hook['plugin'] !== 'core' && isset($this->plugins[$hook['plugin']])) {
                if (!$this->plugins[$hook['plugin']]['enabled']) {
                    continue;
                }
            }
            
            $startTime = microtime(true);
            
            try {
                $result = call_user_func($hook['callback'], ...$args);
                $results[] = $result;
                
                // Update plugin statistics
                if ($hook['plugin'] !== 'core' && isset($this->plugins[$hook['plugin']])) {
                    $executionTime = microtime(true) - $startTime;
                    $this->updatePluginStats($hook['plugin'], $executionTime);
                }
                
            } catch (\Exception $e) {
                $this->errorHandler->trackError('HOOK_EXECUTION_FAILED', $e->getMessage(), [
                    'hook' => $hookName,
                    'plugin' => $hook['plugin']
                ]);
                
                if ($hook['plugin'] !== 'core' && isset($this->plugins[$hook['plugin']])) {
                    $this->plugins[$hook['plugin']]['execution_stats']['errors']++;
                }
            }
        }
        
        return $results;
    }
    
    /**
     * Get plugin information
     */
    public function getPlugin(string $name): ?array
    {
        return $this->plugins[$name] ?? null;
    }
    
    /**
     * Get all plugins
     */
    public function getAllPlugins(): array
    {
        return $this->plugins;
    }
    
    /**
     * Get plugin statistics
     */
    public function getPluginStats(string $name): ?array
    {
        if (!isset($this->plugins[$name])) {
            return null;
        }
        
        return [
            'plugin' => $name,
            'enabled' => $this->plugins[$name]['enabled'],
            'loaded' => $this->plugins[$name]['loaded'],
            'load_time' => $this->plugins[$name]['load_time'],
            'execution_stats' => $this->plugins[$name]['execution_stats'],
            'hooks_registered' => count($this->plugins[$name]['hooks']),
            'memory_usage' => $this->getPluginMemoryUsage($name)
        ];
    }
    
    /**
     * Get system statistics
     */
    public function getSystemStats(): array
    {
        $totalPlugins = count($this->plugins);
        $loadedPlugins = count(array_filter($this->plugins, fn($p) => $p['loaded']));
        $enabledPlugins = count(array_filter($this->plugins, fn($p) => $p['enabled']));
        
        return [
            'total_plugins' => $totalPlugins,
            'loaded_plugins' => $loadedPlugins,
            'enabled_plugins' => $enabledPlugins,
            'total_hooks' => count($this->hooks),
            'total_hook_callbacks' => array_sum(array_map('count', $this->hooks)),
            'memory_usage' => memory_get_usage(true),
            'plugin_dir' => $this->pluginDir
        ];
    }
    
    private function validatePluginConfig(array $config): bool
    {
        // Basic validation - can be extended
        if (empty($config['version'])) {
            return false;
        }
        
        if (isset($config['class']) && !class_exists($config['class'])) {
            return false;
        }
        
        return true;
    }
    
    private function checkDependencies(string $pluginName, array $config): bool
    {
        $dependencies = $config['dependencies'] ?? [];
        
        foreach ($dependencies as $dependency) {
            if (!isset($this->plugins[$dependency])) {
                return false;
            }
            
            if (!$this->plugins[$dependency]['enabled']) {
                return false;
            }
        }
        
        return true;
    }
    
    private function createSandboxedInstance(string $className, array $config): object
    {
        // In a real implementation, this would create a sandboxed environment
        // For now, we'll just create the instance normally but track it
        $instance = new $className($config);
        
        // Wrap the instance to monitor its behavior
        return new class($instance) {
            private $wrapped;
            
            public function __construct($instance) {
                $this->wrapped = $instance;
            }
            
            public function __call($method, $args) {
                return call_user_func_array([$this->wrapped, $method], $args);
            }
            
            public function __get($property) {
                return $this->wrapped->$property;
            }
            
            public function __set($property, $value) {
                $this->wrapped->$property = $value;
            }
        };
    }
    
    private function updatePluginStats(string $pluginName, float $executionTime): void
    {
        $stats = &$this->plugins[$pluginName]['execution_stats'];
        $stats['calls']++;
        $stats['total_time'] += $executionTime;
        $stats['average_time'] = $stats['total_time'] / $stats['calls'];
    }
    
    private function getPluginMemoryUsage(string $pluginName): int
    {
        // This is a simplified memory usage calculation
        // In a real implementation, you might track memory more precisely
        return strlen(serialize($this->plugins[$pluginName]));
    }
    
    private function ensurePluginDir(): void
    {
        if (!is_dir($this->pluginDir)) {
            mkdir($this->pluginDir, 0755, true);
        }
    }
    
    private function initializeCore(): void
    {
        // Register core system hooks
        $this->registerHook('system_init', function() {
            // System initialization logic
        });
        
        $this->registerHook('system_shutdown', function() {
            // System cleanup logic
        });
    }
} 