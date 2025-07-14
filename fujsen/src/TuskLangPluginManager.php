<?php
/**
 * 🥜 TuskLangPluginManager - Enhanced Plugin Management with Peanu.tsk Integration
 * ==============================================================================
 * "Plugins that inherit like CSS, compile like peanuts, and scale like elephants"
 * 
 * Features:
 * - Plugin-specific peanu.tsk configuration files
 * - CSS-like inheritance from project configuration
 * - Automatic binary compilation to .peanuts format
 * - Configuration chain resolution and validation
 * - Integration with existing PeanuConfig system
 * - Marketplace with configuration templates
 * 
 * Strong. Secure. Scalable. 🐘🥜
 */

namespace TuskLang;

use TuskPHP\Utils\PeanuConfig;
use TuskPHP\Utils\TuskLang;
use TuskPHP\Utils\PeanutsBinary;

class TuskLangPluginManager
{
    private static $instance = null;
    private $plugins = [];
    private $marketplace = [];
    private $configManager;
    private $pluginConfigs = [];
    
    // Plugin configuration constants
    const PLUGIN_CONFIG_FILE = 'peanu.tsk';
    const PLUGIN_BINARY_FILE = '.peanuts';
    const PLUGIN_MANIFEST_FILE = 'manifest.tsk';
    const PLUGIN_CONFIG_CACHE_TTL = 300; // 5 minutes
    
    private function __construct()
    {
        $this->configManager = PeanuConfig::getInstance();
        $this->loadInstalledPlugins();
        $this->loadMarketplace();
    }
    
    public static function getInstance(): self
    {
        if (self::$instance === null) {
            self::$instance = new self();
        }
        return self::$instance;
    }
    
    /**
     * Install a plugin with peanu.tsk configuration support
     */
    public function installPlugin(string $pluginName, string $source = 'official'): array
    {
        $startTime = microtime(true);
        
        try {
            // Get plugin information
            $pluginInfo = $this->getPluginInfo($pluginName, $source);
            if (!$pluginInfo) {
                throw new \Exception("Plugin '{$pluginName}' not found in {$source} marketplace");
            }
            
            // Create plugin directory
            $pluginDir = $this->getPluginDirectory($pluginName);
            if (!is_dir($pluginDir)) {
                mkdir($pluginDir, 0755, true);
            }
            
            // Download and extract plugin
            $this->downloadPlugin($pluginInfo, $pluginDir);
            
            // Generate plugin-specific peanu.tsk configuration
            $this->generatePluginConfiguration($pluginName, $pluginInfo, $pluginDir);
            
            // Compile configuration to binary .peanuts format
            $this->compilePluginConfiguration($pluginDir);
            
            // Install dependencies
            $this->installPluginDependencies($pluginInfo, $pluginDir);
            
            // Register plugin
            $this->registerPlugin($pluginName, $pluginInfo, $pluginDir);
            
            // Validate configuration inheritance
            $this->validatePluginConfiguration($pluginDir);
            
            $installTime = microtime(true) - $startTime;
            
            return [
                'success' => true,
                'plugin' => $pluginName,
                'version' => $pluginInfo['version'],
                'install_time' => round($installTime, 3),
                'config_file' => $pluginDir . '/' . self::PLUGIN_CONFIG_FILE,
                'binary_file' => $pluginDir . '/' . self::PLUGIN_BINARY_FILE,
                'dependencies' => $pluginInfo['dependencies'] ?? [],
                'features' => $pluginInfo['features'] ?? []
            ];
            
        } catch (\Exception $e) {
            return [
                'success' => false,
                'error' => $e->getMessage(),
                'plugin' => $pluginName
            ];
        }
    }
    
    /**
     * Generate plugin-specific peanu.tsk configuration with inheritance
     */
    private function generatePluginConfiguration(string $pluginName, array $pluginInfo, string $pluginDir): void
    {
        $configFile = $pluginDir . '/' . self::PLUGIN_CONFIG_FILE;
        
        // Get project configuration for inheritance
        $projectConfig = $this->configManager->getConfig(getcwd());
        
        // Create plugin-specific configuration
        $pluginConfig = $this->buildPluginConfiguration($pluginName, $pluginInfo, $projectConfig);
        
        // Write configuration file
        file_put_contents($configFile, $pluginConfig);
        
        echo "🥜 Generated plugin configuration: {$configFile}\n";
    }
    
    /**
     * Build plugin configuration with inheritance from project config
     */
    private function buildPluginConfiguration(string $pluginName, array $pluginInfo, array $projectConfig): string
    {
        $config = "# 🥜 Plugin Configuration: {$pluginName}\n";
        $config .= "# ============================================\n";
        $config .= "# Inherits from project peanu.tsk configuration\n";
        $config .= "# Plugin-specific overrides and additions\n\n";
        
        // Plugin metadata
        $config .= "# Plugin Information\n";
        $config .= "plugin:\n";
        $config .= "    name: \"{$pluginName}\"\n";
        $config .= "    version: \"{$pluginInfo['version']}\"\n";
        $config .= "    author: \"{$pluginInfo['author']}\"\n";
        $config .= "    description: \"{$pluginInfo['description']}\"\n";
        $config .= "    installed_at: \"" . date('Y-m-d H:i:s') . "\"\n\n";
        
        // Inherit database configuration from project
        if (isset($projectConfig['database'])) {
            $config .= "# Inherited Database Configuration\n";
            $config .= "database:\n";
            foreach ($projectConfig['database'] as $key => $value) {
                if (is_string($value)) {
                    $config .= "    {$key}: \"{$value}\"\n";
                } else {
                    $config .= "    {$key}: {$value}\n";
                }
            }
            $config .= "\n";
        }
        
        // Inherit server configuration
        if (isset($projectConfig['server'])) {
            $config .= "# Inherited Server Configuration\n";
            $config .= "server:\n";
            foreach ($projectConfig['server'] as $key => $value) {
                if (is_string($value)) {
                    $config .= "    {$key}: \"{$value}\"\n";
                } else {
                    $config .= "    {$key}: {$value}\n";
                }
            }
            $config .= "\n";
        }
        
        // Plugin-specific configuration
        $config .= "# Plugin-Specific Configuration\n";
        $config .= "plugin_config:\n";
        $config .= "    enabled: true\n";
        $config .= "    auto_load: true\n";
        $config .= "    cache_enabled: true\n";
        $config .= "    debug_mode: false\n\n";
        
        // Plugin features
        if (isset($pluginInfo['features'])) {
            $config .= "# Plugin Features\n";
            $config .= "features:\n";
            foreach ($pluginInfo['features'] as $feature => $enabled) {
                $config .= "    {$feature}: " . ($enabled ? 'true' : 'false') . "\n";
            }
            $config .= "\n";
        }
        
        // Plugin dependencies
        if (isset($pluginInfo['dependencies'])) {
            $config .= "# Plugin Dependencies\n";
            $config .= "dependencies:\n";
            foreach ($pluginInfo['dependencies'] as $dep => $version) {
                $config .= "    {$dep}: \"{$version}\"\n";
            }
            $config .= "\n";
        }
        
        // Plugin-specific overrides
        $config .= "# Plugin-Specific Overrides\n";
        $config .= "overrides:\n";
        $config .= "    # Override project settings for this plugin\n";
        $config .= "    cache_ttl: 1800  # 30 minutes for plugin cache\n";
        $config .= "    log_level: \"info\"  # Plugin-specific logging\n";
        $config .= "    max_connections: 50  # Plugin connection limit\n\n";
        
        // Integration with project features
        $config .= "# Integration Configuration\n";
        $config .= "integration:\n";
        $config .= "    inherit_security: true\n";
        $config .= "    inherit_database: true\n";
        $config .= "    inherit_cache: true\n";
        $config .= "    inherit_logging: true\n";
        $config .= "    custom_routes: true\n";
        $config .= "    api_endpoints: true\n\n";
        
        // Performance settings
        $config .= "# Performance Configuration\n";
        $config .= "performance:\n";
        $config .= "    binary_compilation: true\n";
        $config .= "    cache_warming: true\n";
        $config .= "    lazy_loading: true\n";
        $config .= "    memory_optimization: true\n\n";
        
        // Meta information
        $config .= "# Meta Information\n";
        $config .= "meta:\n";
        $config .= "    config_type: \"plugin\"\n";
        $config .= "    parent_config: \"project peanu.tsk\"\n";
        $config .= "    inheritance_chain: [\"project\", \"plugin\"]\n";
        $config .= "    compiled_at: \"" . date('Y-m-d H:i:s') . "\"\n";
        $config .= "    tusk_version: \"1.0.0\"\n";
        
        return $config;
    }
    
    /**
     * Compile plugin configuration to binary .peanuts format
     */
    private function compilePluginConfiguration(string $pluginDir): void
    {
        $configFile = $pluginDir . '/' . self::PLUGIN_CONFIG_FILE;
        $binaryFile = $pluginDir . '/' . self::PLUGIN_BINARY_FILE;
        
        if (file_exists($configFile)) {
            // Use PeanutsBinary for compilation
            $binary = PeanutsBinary::getInstance();
            $success = $binary->compile($configFile, $binaryFile);
            
            if ($success) {
                echo "📦 Compiled plugin configuration to binary format: {$binaryFile}\n";
            } else {
                echo "⚠️  Warning: Failed to compile plugin configuration to binary\n";
            }
        }
    }
    
    /**
     * Validate plugin configuration inheritance
     */
    private function validatePluginConfiguration(string $pluginDir): array
    {
        $validation = [
            'success' => true,
            'errors' => [],
            'warnings' => [],
            'inheritance_chain' => []
        ];
        
        try {
            // Get configuration chain
            $configChain = $this->configManager->getConfigChain($pluginDir);
            $validation['inheritance_chain'] = $configChain;
            
            // Validate inheritance
            foreach ($configChain as $chainItem) {
                $config = $chainItem['config'];
                
                // Check for required fields
                if (!isset($config['plugin']['name'])) {
                    $validation['warnings'][] = "Missing plugin name in: {$chainItem['file']}";
                }
                
                // Check for conflicts
                if (isset($config['overrides'])) {
                    foreach ($config['overrides'] as $key => $value) {
                        // Validate override values
                        if (is_numeric($value) && $value < 0) {
                            $validation['errors'][] = "Invalid override value for {$key}: {$value}";
                        }
                    }
                }
            }
            
            if (!empty($validation['errors'])) {
                $validation['success'] = false;
            }
            
        } catch (\Exception $e) {
            $validation['success'] = false;
            $validation['errors'][] = "Configuration validation failed: " . $e->getMessage();
        }
        
        return $validation;
    }
    
    /**
     * Get plugin configuration with inheritance
     */
    public function getPluginConfig(string $pluginName): array
    {
        $pluginDir = $this->getPluginDirectory($pluginName);
        
        if (!is_dir($pluginDir)) {
            throw new \Exception("Plugin '{$pluginName}' not installed");
        }
        
        // Use PeanuConfig for hierarchical configuration resolution
        return $this->configManager->getConfig($pluginDir);
    }
    
    /**
     * Get plugin configuration value with inheritance
     */
    public function getPluginConfigValue(string $pluginName, string $keyPath, $default = null)
    {
        $pluginDir = $this->getPluginDirectory($pluginName);
        
        if (!is_dir($pluginDir)) {
            return $default;
        }
        
        // Use PeanuConfig for hierarchical key resolution
        return $this->configManager->get($pluginDir, $keyPath, $default);
    }
    
    /**
     * Update plugin configuration
     */
    public function updatePluginConfig(string $pluginName, array $updates): array
    {
        $pluginDir = $this->getPluginDirectory($pluginName);
        $configFile = $pluginDir . '/' . self::PLUGIN_CONFIG_FILE;
        
        if (!file_exists($configFile)) {
            throw new \Exception("Plugin configuration not found: {$configFile}");
        }
        
        // Load current configuration
        $currentConfig = TuskLang::parse(file_get_contents($configFile));
        
        // Apply updates
        $updatedConfig = $this->mergeConfigurations($currentConfig, $updates);
        
        // Convert back to TuskLang format
        $configContent = $this->arrayToTuskLang($updatedConfig);
        
        // Write updated configuration
        file_put_contents($configFile, $configContent);
        
        // Recompile to binary
        $this->compilePluginConfiguration($pluginDir);
        
        // Clear cache
        $this->configManager->clearCache($pluginDir);
        
        return [
            'success' => true,
            'plugin' => $pluginName,
            'updated_keys' => array_keys($updates),
            'config_file' => $configFile
        ];
    }
    
    /**
     * List all plugin configurations with inheritance info
     */
    public function listPluginConfigs(): array
    {
        $configs = [];
        
        foreach ($this->plugins as $pluginName => $pluginInfo) {
            $pluginDir = $this->getPluginDirectory($pluginName);
            
            try {
                $config = $this->getPluginConfig($pluginName);
                $configChain = $this->configManager->getConfigChain($pluginDir);
                
                $configs[$pluginName] = [
                    'name' => $pluginName,
                    'version' => $pluginInfo['version'],
                    'config_file' => $pluginDir . '/' . self::PLUGIN_CONFIG_FILE,
                    'binary_file' => $pluginDir . '/' . self::PLUGIN_BINARY_FILE,
                    'inheritance_chain' => array_map(function($item) {
                        return basename($item['directory']);
                    }, $configChain),
                    'config_keys' => array_keys($config),
                    'enabled' => $config['plugin_config']['enabled'] ?? true
                ];
                
            } catch (\Exception $e) {
                $configs[$pluginName] = [
                    'name' => $pluginName,
                    'error' => $e->getMessage()
                ];
            }
        }
        
        return $configs;
    }
    
    /**
     * Create plugin configuration template
     */
    public function createPluginConfigTemplate(string $pluginName, array $features = []): string
    {
        $template = "# 🥜 Plugin Configuration Template: {$pluginName}\n";
        $template .= "# ===============================================\n";
        $template .= "# This template will be customized during plugin installation\n\n";
        
        $template .= "# Plugin Information\n";
        $template .= "plugin:\n";
        $template .= "    name: \"{$pluginName}\"\n";
        $template .= "    version: \"1.0.0\"\n";
        $template .= "    author: \"Plugin Author\"\n";
        $template .= "    description: \"Plugin description\"\n\n";
        
        $template .= "# Plugin Configuration\n";
        $template .= "plugin_config:\n";
        $template .= "    enabled: true\n";
        $template .= "    auto_load: true\n";
        $template .= "    cache_enabled: true\n";
        $template .= "    debug_mode: false\n\n";
        
        if (!empty($features)) {
            $template .= "# Plugin Features\n";
            $template .= "features:\n";
            foreach ($features as $feature) {
                $template .= "    {$feature}: true\n";
            }
            $template .= "\n";
        }
        
        $template .= "# Integration Settings\n";
        $template .= "integration:\n";
        $template .= "    inherit_security: true\n";
        $template .= "    inherit_database: true\n";
        $template .= "    inherit_cache: true\n";
        $template .= "    custom_routes: true\n";
        $template .= "    api_endpoints: true\n\n";
        
        $template .= "# Performance Settings\n";
        $template .= "performance:\n";
        $template .= "    binary_compilation: true\n";
        $template .= "    cache_warming: true\n";
        $template .= "    lazy_loading: true\n";
        $template .= "    memory_optimization: true\n";
        
        return $template;
    }
    
    /**
     * Helper methods
     */
    
    private function applyFilters(array $plugin, array $filters): bool
    {
        if (empty($filters)) {
            return true;
        }
        
        foreach ($filters as $key => $value) {
            if (!isset($plugin[$key]) || $plugin[$key] !== $value) {
                return false;
            }
        }
        
        return true;
    }
    
    private function isUpdateAvailable(string $pluginId, string $newVersion): bool
    {
        if (!isset($this->installedPlugins[$pluginId])) {
            return false;
        }
        
        $currentVersion = $this->installedPlugins[$pluginId]['version'];
        return version_compare($newVersion, $currentVersion, '>');
    }
    
    private function validateDependencies(array $dependencies): void
    {
        foreach ($dependencies as $dependency) {
            if (!isset($this->installedPlugins[$dependency])) {
                throw new \Exception("Required dependency '$dependency' is not installed");
            }
        }
    }
    
    private function downloadPlugin(array $pluginInfo, string $pluginDir): void
    {
        $tempDir = __DIR__ . '/../../plugins/temp';
        $downloadPath = $tempDir . '/' . $pluginInfo['name'] . '.zip';
        
        $content = file_get_contents($pluginInfo['download_url']);
        if ($content === false) {
            throw new \Exception("Failed to download plugin from '{$pluginInfo['download_url']}'");
        }
        
        file_put_contents($downloadPath, $content);
        
        $zip = new \ZipArchive();
        if ($zip->open($downloadPath) !== true) {
            throw new \Exception("Failed to open plugin archive");
        }
        
        $zip->extractTo($pluginDir);
        $zip->close();
        
        unlink($downloadPath);
    }
    
    private function extractAndInstallPlugin(string $zipPath, string $pluginId): string
    {
        $installPath = __DIR__ . '/../../plugins/installed/' . $pluginId;
        
        $zip = new \ZipArchive();
        if ($zip->open($zipPath) !== true) {
            throw new \Exception("Failed to open plugin archive");
        }
        
        $zip->extractTo($installPath);
        $zip->close();
        
        return $installPath;
    }
    
    private function registerPlugin(string $pluginName, array $pluginInfo, string $pluginDir): void
    {
        $this->plugins[$pluginName] = $pluginInfo;
        $this->pluginConfigs[$pluginName] = [
            'name' => $pluginName,
            'version' => $pluginInfo['version'],
            'config_file' => $pluginDir . '/' . self::PLUGIN_CONFIG_FILE,
            'binary_file' => $pluginDir . '/' . self::PLUGIN_BINARY_FILE,
            'install_path' => $pluginDir,
            'state' => 'installed' // Assuming 'installed' is the state for a plugin that has been installed
        ];
        
        // Save plugin registry (if it were a separate file)
        // $this->savePluginRegistry(); 
    }
    
    private function createPluginBackup(string $pluginId): void
    {
        $pluginPath = __DIR__ . '/../../plugins/installed/' . $pluginId;
        $backupPath = __DIR__ . '/../../plugins/backups/' . $pluginId . '_' . date('Y-m-d_H-i-s');
        
        if (is_dir($pluginPath)) {
            $this->copyDirectory($pluginPath, $backupPath);
        }
    }
    
    private function loadPluginRegistry(): void
    {
        $registryFile = __DIR__ . '/../../plugins/registry.json';
        
        if (file_exists($registryFile)) {
            $this->pluginRegistry = json_decode(file_get_contents($registryFile), true) ?? [];
        }
    }
    
    private function savePluginRegistry(): void
    {
        $registryFile = __DIR__ . '/../../plugins/registry.json';
        file_put_contents($registryFile, json_encode($this->installedPlugins, JSON_PRETTY_PRINT));
    }
    
    private function parsePluginManifest(string $file): array
    {
        $content = file_get_contents($file);
        
        // Simple TuskLang-like parser for manifest
        $manifest = [];
        $lines = explode("\n", $content);
        
        foreach ($lines as $line) {
            $line = trim($line);
            if (empty($line) || strpos($line, '#') === 0) {
                continue;
            }
            
            if (strpos($line, ':') !== false) {
                list($key, $value) = explode(':', $line, 2);
                $key = trim($key);
                $value = trim($value);
                
                // Remove quotes if present
                if (preg_match('/^"(.*)"$/', $value, $matches)) {
                    $value = $matches[1];
                }
                
                $manifest[$key] = $value;
            }
        }
        
        return $manifest;
    }
    
    private function parseCustomMarketplace(string $file): array
    {
        return $this->parsePluginManifest($file);
    }
    
    private function removeDirectory(string $path): void
    {
        if (!is_dir($path)) {
            return;
        }
        
        $iterator = new \RecursiveIteratorIterator(
            new \RecursiveDirectoryIterator($path, \RecursiveDirectoryIterator::SKIP_DOTS),
            \RecursiveIteratorIterator::CHILD_FIRST
        );
        
        foreach ($iterator as $file) {
            if ($file->isDir()) {
                rmdir($file->getPathname());
            } else {
                unlink($file->getPathname());
            }
        }
        
        rmdir($path);
    }
    
    private function copyDirectory(string $source, string $destination): void
    {
        if (!is_dir($source)) {
            return;
        }
        
        if (!is_dir($destination)) {
            mkdir($destination, 0755, true);
        }
        
        $iterator = new \RecursiveIteratorIterator(
            new \RecursiveDirectoryIterator($source, \RecursiveDirectoryIterator::SKIP_DOTS),
            \RecursiveIteratorIterator::SELF_FIRST
        );
        
        foreach ($iterator as $file) {
            $target = $destination . '/' . $iterator->getSubPathName();
            
            if ($file->isDir()) {
                mkdir($target, 0755, true);
            } else {
                copy($file->getPathname(), $target);
            }
        }
    }

    private function getPluginDirectory(string $pluginName): string
    {
        return __DIR__ . '/../../plugins/installed/' . $pluginName;
    }

    private function getPluginInfo(string $pluginName, string $source): ?array
    {
        if (isset($this->marketplace[$source][$pluginName])) {
            return $this->marketplace[$source][$pluginName];
        }
        return null;
    }

    private function loadMarketplace(): void
    {
        // Load official marketplace
        $this->loadOfficialMarketplace();
        
        // Load community marketplace
        $this->loadCommunityMarketplace();
        
        // Load custom marketplace sources
        $this->loadCustomMarketplaces();
    }

    private function loadOfficialMarketplace(): void
    {
        $officialPlugins = [
            'database-optimizer' => [
                'name' => 'Database Optimizer',
                'description' => 'Advanced database optimization and query analysis',
                'version' => '1.0.0',
                'author' => 'TuskLang Team',
                'category' => 'performance',
                'tags' => ['database', 'optimization', 'performance'],
                'dependencies' => ['tuskdb'],
                'download_url' => 'https://marketplace.tusklang.org/official/database-optimizer-1.0.0.zip',
                'downloads' => 15420,
                'rating' => 4.8,
                'reviews' => 342,
                'price' => 0,
                'license' => 'MIT'
            ],
            'api-generator' => [
                'name' => 'API Generator',
                'description' => 'Generate REST APIs from TuskLang configurations',
                'version' => '1.2.0',
                'author' => 'TuskLang Team',
                'category' => 'development',
                'tags' => ['api', 'rest', 'generator'],
                'dependencies' => ['fujsen'],
                'download_url' => 'https://marketplace.tusklang.org/official/api-generator-1.2.0.zip',
                'downloads' => 8920,
                'rating' => 4.9,
                'reviews' => 156,
                'price' => 0,
                'license' => 'MIT'
            ],
            'cloud-deployer' => [
                'name' => 'Cloud Deployer',
                'description' => 'Deploy TuskLang applications to cloud platforms',
                'version' => '1.1.0',
                'author' => 'TuskLang Team',
                'category' => 'deployment',
                'tags' => ['cloud', 'deployment', 'aws', 'azure'],
                'dependencies' => ['fujsen-services'],
                'download_url' => 'https://marketplace.tusklang.org/official/cloud-deployer-1.1.0.zip',
                'downloads' => 5670,
                'rating' => 4.7,
                'reviews' => 89,
                'price' => 29.99,
                'license' => 'Commercial'
            ],
            'security-scanner' => [
                'name' => 'Security Scanner',
                'description' => 'Scan TuskLang configurations for security vulnerabilities',
                'version' => '1.0.0',
                'author' => 'TuskLang Team',
                'category' => 'security',
                'tags' => ['security', 'vulnerability', 'scanning'],
                'dependencies' => [],
                'download_url' => 'https://marketplace.tusklang.org/official/security-scanner-1.0.0.zip',
                'downloads' => 12340,
                'rating' => 4.9,
                'reviews' => 234,
                'price' => 0,
                'license' => 'MIT'
            ],
            'monitoring-dashboard' => [
                'name' => 'Monitoring Dashboard',
                'description' => 'Real-time monitoring and analytics dashboard',
                'version' => '1.3.0',
                'author' => 'TuskLang Team',
                'category' => 'monitoring',
                'tags' => ['monitoring', 'analytics', 'dashboard'],
                'dependencies' => ['cache-cleaner'],
                'download_url' => 'https://marketplace.tusklang.org/official/monitoring-dashboard-1.3.0.zip',
                'downloads' => 7890,
                'rating' => 4.8,
                'reviews' => 167,
                'price' => 19.99,
                'license' => 'Commercial'
            ]
        ];
        
        $this->marketplace[self::MARKETPLACE_OFFICIAL] = $officialPlugins;
    }

    private function loadCommunityMarketplace(): void
    {
        $communityPlugins = [
            'theme-manager' => [
                'name' => 'Theme Manager',
                'description' => 'Manage and apply themes to TuskLang applications',
                'version' => '0.9.0',
                'author' => 'Community Developer',
                'category' => 'ui',
                'tags' => ['theme', 'ui', 'styling'],
                'dependencies' => [],
                'download_url' => 'https://marketplace.tusklang.org/community/theme-manager-0.9.0.zip',
                'downloads' => 2340,
                'rating' => 4.5,
                'reviews' => 45,
                'price' => 0,
                'license' => 'MIT'
            ],
            'data-migrator' => [
                'name' => 'Data Migrator',
                'description' => 'Migrate data between different TuskLang configurations',
                'version' => '0.8.0',
                'author' => 'Community Developer',
                'category' => 'data',
                'tags' => ['migration', 'data', 'import'],
                'dependencies' => ['tuskdb'],
                'download_url' => 'https://marketplace.tusklang.org/community/data-migrator-0.8.0.zip',
                'downloads' => 1560,
                'rating' => 4.3,
                'reviews' => 23,
                'price' => 0,
                'license' => 'MIT'
            ]
        ];
        
        $this->marketplace[self::MARKETPLACE_COMMUNITY] = $communityPlugins;
    }

    private function loadCustomMarketplaces(): void
    {
        $customMarketplaceFile = __DIR__ . '/../../config/custom-marketplace.tsk';
        
        if (file_exists($customMarketplaceFile)) {
            $customPlugins = $this->parseCustomMarketplace($customMarketplaceFile);
            $this->marketplace[self::MARKETPLACE_CUSTOM] = $customPlugins;
        }
    }

    private function mergeConfigurations(array $baseConfig, array $updates): array
    {
        $merged = $baseConfig;
        foreach ($updates as $key => $value) {
            $this->setConfigValue($merged, $key, $value);
        }
        return $merged;
    }

    private function setConfigValue(array &$config, string $keyPath, $value): void
    {
        $keys = explode('.', $keyPath);
        $current = &$config;
        foreach ($keys as $key) {
            if (!isset($current[$key])) {
                $current[$key] = [];
            }
            $current = &$current[$key];
        }
        $current = $value;
    }

    private function arrayToTuskLang(array $array): string
    {
        $tuskLang = new TuskLang();
        return $tuskLang->stringify($array);
    }
}

/**
 * Plugin Security Validator
 */
class PluginSecurityValidator
{
    public function validatePlugin(string $pluginPath): void
    {
        // Check for malicious code patterns
        $this->checkForMaliciousCode($pluginPath);
        
        // Validate plugin structure
        $this->validatePluginStructure($pluginPath);
        
        // Check file permissions
        $this->checkFilePermissions($pluginPath);
    }
    
    private function checkForMaliciousCode(string $pluginPath): void
    {
        $dangerousPatterns = [
            'eval\s*\(',
            'exec\s*\(',
            'system\s*\(',
            'shell_exec\s*\(',
            'passthru\s*\(',
            'file_get_contents\s*\(\s*[\'"]https?://',
            'curl_exec\s*\(',
            'fopen\s*\(\s*[\'"]https?://'
        ];
        
        $iterator = new \RecursiveIteratorIterator(
            new \RecursiveDirectoryIterator($pluginPath)
        );
        
        foreach ($iterator as $file) {
            if ($file->isFile() && pathinfo($file->getPathname(), PATHINFO_EXTENSION) === 'php') {
                $content = file_get_contents($file->getPathname());
                
                foreach ($dangerousPatterns as $pattern) {
                    if (preg_match('/' . $pattern . '/i', $content)) {
                        throw new \Exception("Potentially malicious code detected in plugin");
                    }
                }
            }
        }
    }
    
    private function validatePluginStructure(string $pluginPath): void
    {
        $requiredFiles = ['manifest.tsk', 'bootstrap.php'];
        
        foreach ($requiredFiles as $file) {
            if (!file_exists($pluginPath . '/' . $file)) {
                throw new \Exception("Required file '$file' not found in plugin");
            }
        }
    }
    
    private function checkFilePermissions(string $pluginPath): void
    {
        $iterator = new \RecursiveIteratorIterator(
            new \RecursiveDirectoryIterator($pluginPath)
        );
        
        foreach ($iterator as $file) {
            if ($file->isFile()) {
                $perms = fileperms($file->getPathname());
                if (($perms & 0x0200) || ($perms & 0x0100)) {
                    throw new \Exception("Plugin contains files with write permissions");
                }
            }
        }
    }
}

/**
 * Plugin Dependency Resolver
 */
class PluginDependencyResolver
{
    public function resolveDependencies(array $dependencies): array
    {
        $resolved = [];
        
        foreach ($dependencies as $dependency) {
            $resolved[] = $this->resolveDependency($dependency);
        }
        
        return $resolved;
    }
    
    private function resolveDependency(string $dependency): array
    {
        // Simple dependency resolution
        return [
            'name' => $dependency,
            'version' => '1.0.0',
            'status' => 'resolved'
        ];
    }
} 