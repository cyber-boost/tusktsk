<?php
/**
 * TuskLang Operator Registry
 * 
 * Central registry for all @ operators with plugin architecture,
 * validation, and execution capabilities.
 * 
 * @package TuskPHP\Core
 * @version 2.0.0
 */

namespace TuskPHP\Core;

use TuskPHP\Utils\TuskLangQueryBridge;
use TuskPHP\Utils\TuskLangCache;
use TuskPHP\Utils\TuskLangMetrics;

/**
 * Operator Plugin Interface
 */
interface OperatorPlugin
{
    public function getName(): string;
    public function getVersion(): string;
    public function getSchema(): array;
    public function validate(array $config): array;
    public function execute(array $config, array $context): mixed;
    public function cleanup(): void;
}

/**
 * Operator Registry
 * 
 * Manages all @ operators with plugin architecture, validation,
 * and execution capabilities.
 */
class TuskLangOperatorRegistry
{
    private static ?self $instance = null;
    private array $operators = [];
    private array $operatorSchemas = [];
    private array $executionContext = [];
    private bool $debugMode = false;
    
    // Core operators that are always available
    private array $coreOperators = [
        'cache', 'metrics', 'learn', 'optimize', 'env', 'file', 'json',
        'request', 'query', 'date', 'hash', 'blockchain', 'feature'
    ];
    
    // Communication operators
    private array $communicationOperators = [
        'graphql', 'grpc', 'websocket', 'sse'
    ];
    
    // Message queue operators
    private array $messageQueueOperators = [
        'nats', 'amqp', 'kafka'
    ];
    
    // Service discovery operators
    private array $serviceDiscoveryOperators = [
        'vault', 'consul', 'etcd'
    ];
    
    // Workflow operators
    private array $workflowOperators = [
        'temporal', 'step', 'circuit'
    ];
    
    public static function getInstance(): self
    {
        if (self::$instance === null) {
            self::$instance = new self();
        }
        return self::$instance;
    }
    
    private function __construct()
    {
        $this->initializeCoreOperators();
        $this->loadOperatorPlugins();
    }
    
    /**
     * Initialize core operators
     */
    private function initializeCoreOperators(): void
    {
        // Core caching and optimization operators
        $this->registerOperator('cache', new CoreOperators\CacheOperator());
        $this->registerOperator('metrics', new CoreOperators\MetricsOperator());
        $this->registerOperator('learn', new CoreOperators\LearnOperator());
        $this->registerOperator('optimize', new CoreOperators\OptimizeOperator());
        
        // Environment and file operators
        $this->registerOperator('env', new CoreOperators\EnvOperator());
        $this->registerOperator('file', new CoreOperators\FileOperator());
        $this->registerOperator('json', new CoreOperators\JsonOperator());
        
        // HTTP and database operators
        $this->registerOperator('request', new CoreOperators\RequestOperator());
        $this->registerOperator('query', new CoreOperators\QueryOperator());
        
        // Utility operators
        $this->registerOperator('date', new CoreOperators\DateOperator());
        $this->registerOperator('hash', new CoreOperators\HashOperator());
        $this->registerOperator('blockchain', new CoreOperators\BlockchainOperator());
        $this->registerOperator('feature', new CoreOperators\FeatureOperator());
    }
    
    /**
     * Load operator plugins from plugins directory
     */
    private function loadOperatorPlugins(): void
    {
        $pluginsDir = __DIR__ . '/../../plugins/operators/';
        
        if (!is_dir($pluginsDir)) {
            return;
        }
        
        $pluginFiles = glob($pluginsDir . '*.php');
        
        foreach ($pluginFiles as $pluginFile) {
            try {
                require_once $pluginFile;
                
                $className = pathinfo($pluginFile, PATHINFO_FILENAME);
                $fullClassName = "TuskPHP\\Plugins\\Operators\\{$className}";
                
                if (class_exists($fullClassName)) {
                    $plugin = new $fullClassName();
                    if ($plugin instanceof OperatorPlugin) {
                        $this->registerOperator($plugin->getName(), $plugin);
                    }
                }
            } catch (\Exception $e) {
                error_log("Failed to load operator plugin {$pluginFile}: " . $e->getMessage());
            }
        }
    }
    
    /**
     * Register an operator
     */
    public function registerOperator(string $name, OperatorPlugin $plugin): void
    {
        $this->operators[$name] = $plugin;
        $this->operatorSchemas[$name] = $plugin->getSchema();
        
        if ($this->debugMode) {
            error_log("Registered operator: {$name} v{$plugin->getVersion()}");
        }
    }
    
    /**
     * Execute an operator
     */
    public function executeOperator(string $name, array $config, array $context = []): mixed
    {
        if (!isset($this->operators[$name])) {
            throw new \InvalidArgumentException("Unknown operator: @{$name}");
        }
        
        $operator = $this->operators[$name];
        
        // Validate configuration
        $validation = $operator->validate($config);
        if (!empty($validation['errors'])) {
            throw new \InvalidArgumentException("Invalid @{$name} configuration: " . implode(', ', $validation['errors']));
        }
        
        // Execute operator
        try {
            $result = $operator->execute($config, array_merge($this->executionContext, $context));
            
            if ($this->debugMode) {
                error_log("Executed @{$name}: " . json_encode($result));
            }
            
            return $result;
        } catch (\Exception $e) {
            error_log("Operator @{$name} execution failed: " . $e->getMessage());
            throw $e;
        }
    }
    
    /**
     * Parse operator expression
     */
    public function parseOperatorExpression(string $expression): array
    {
        // Match @operator(...) pattern
        if (preg_match('/^@([a-zA-Z_][a-zA-Z0-9_]*)\s*\((.*)\)$/', $expression, $matches)) {
            $operatorName = $matches[1];
            $configString = $matches[2];
            
            // Parse configuration
            $config = $this->parseConfigString($configString);
            
            return [
                'operator' => $operatorName,
                'config' => $config
            ];
        }
        
        throw new \InvalidArgumentException("Invalid operator expression: {$expression}");
    }
    
    /**
     * Parse configuration string
     */
    private function parseConfigString(string $configString): array
    {
        $config = [];
        
        // Handle simple string parameters
        if (preg_match('/^"([^"]+)"$/', trim($configString), $matches)) {
            return $matches[1];
        }
        
        // Handle key-value pairs
        if (strpos($configString, ':') !== false) {
            $pairs = explode(',', $configString);
            
            foreach ($pairs as $pair) {
                $pair = trim($pair);
                if (preg_match('/^([a-zA-Z_][a-zA-Z0-9_]*)\s*:\s*(.+)$/', $pair, $matches)) {
                    $key = $matches[1];
                    $value = $this->parseValue($matches[2]);
                    $config[$key] = $value;
                }
            }
        } else {
            // Handle array-like values
            $config = $this->parseValue($configString);
        }
        
        return $config;
    }
    
    /**
     * Parse a single value
     */
    private function parseValue(string $value): mixed
    {
        $value = trim($value);
        
        // String literals
        if (preg_match('/^"([^"]*)"$/', $value, $matches)) {
            return $matches[1];
        }
        
        // Numbers
        if (is_numeric($value)) {
            return strpos($value, '.') !== false ? (float)$value : (int)$value;
        }
        
        // Booleans
        if (in_array(strtolower($value), ['true', 'false'])) {
            return strtolower($value) === 'true';
        }
        
        // Arrays
        if (preg_match('/^\[(.*)\]$/', $value, $matches)) {
            $items = explode(',', $matches[1]);
            $array = [];
            foreach ($items as $item) {
                $array[] = $this->parseValue(trim($item));
            }
            return $array;
        }
        
        // Objects
        if (preg_match('/^\{(.*)\}$/', $value, $matches)) {
            return $this->parseConfigString($matches[1]);
        }
        
        // Variables (prefixed with @)
        if (preg_match('/^@([a-zA-Z_][a-zA-Z0-9_]*)$/', $value, $matches)) {
            return ['variable' => $matches[1]];
        }
        
        // Default to string
        return $value;
    }
    
    /**
     * Get all registered operators
     */
    public function getOperators(): array
    {
        return array_keys($this->operators);
    }
    
    /**
     * Get operator schema
     */
    public function getOperatorSchema(string $name): ?array
    {
        return $this->operatorSchemas[$name] ?? null;
    }
    
    /**
     * Check if operator exists
     */
    public function hasOperator(string $name): bool
    {
        return isset($this->operators[$name]);
    }
    
    /**
     * Set execution context
     */
    public function setExecutionContext(array $context): void
    {
        $this->executionContext = $context;
    }
    
    /**
     * Enable debug mode
     */
    public function setDebugMode(bool $enabled): void
    {
        $this->debugMode = $enabled;
    }
    
    /**
     * Get operator statistics
     */
    public function getStatistics(): array
    {
        $stats = [
            'total_operators' => count($this->operators),
            'core_operators' => count(array_intersect($this->getOperators(), $this->coreOperators)),
            'communication_operators' => count(array_intersect($this->getOperators(), $this->communicationOperators)),
            'message_queue_operators' => count(array_intersect($this->getOperators(), $this->messageQueueOperators)),
            'service_discovery_operators' => count(array_intersect($this->getOperators(), $this->serviceDiscoveryOperators)),
            'workflow_operators' => count(array_intersect($this->getOperators(), $this->workflowOperators)),
            'operators' => $this->getOperators()
        ];
        
        return $stats;
    }
    
    /**
     * Cleanup all operators
     */
    public function cleanup(): void
    {
        foreach ($this->operators as $operator) {
            try {
                $operator->cleanup();
            } catch (\Exception $e) {
                error_log("Operator cleanup failed: " . $e->getMessage());
            }
        }
    }
} 