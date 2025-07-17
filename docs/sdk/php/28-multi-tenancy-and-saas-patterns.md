# Multi-Tenancy and SaaS Patterns with TuskLang

TuskLang revolutionizes multi-tenant SaaS development by providing configuration-driven tenant isolation, dynamic resource allocation, and intelligent tenant management that scales from startups to enterprise applications.

## Overview

TuskLang's multi-tenancy capabilities go beyond simple tenant identification, offering sophisticated isolation strategies, dynamic configuration per tenant, automated resource provisioning, and intelligent tenant lifecycle management.

```php
// Multi-Tenancy Configuration
multi_tenancy = {
    isolation_strategy = "database_per_tenant"  // database_per_tenant, schema_per_tenant, row_level_security
    
    tenant_identification = {
        methods = ["subdomain", "custom_domain", "header", "path"]
        default_method = "subdomain"
        fallback_tenant = "default"
        
        subdomain_pattern = "{tenant}.example.com"
        custom_domain_pattern = "{tenant}.custom.com"
        header_name = "X-Tenant-ID"
        path_pattern = "/tenant/{tenant}/"
    }
    
    tenant_provisioning = {
        automated = true
        provisioning_strategy = "on_demand"
        
        resources = {
            database = {
                create_schema = true
                run_migrations = true
                seed_data = true
            }
            
            storage = {
                create_bucket = true
                set_permissions = true
                quota_limit = "10GB"
            }
            
            cache = {
                create_namespace = true
                set_ttl = 3600
            }
            
            queue = {
                create_queues = true
                set_priorities = true
            }
        }
        
        templates = {
            startup = {
                features = ["basic_api", "standard_support"]
                limits = {
                    users = 100
                    storage = "1GB"
                    api_calls = 10000
                }
            }
            
            growth = {
                features = ["advanced_api", "priority_support", "analytics"]
                limits = {
                    users = 1000
                    storage = "10GB"
                    api_calls = 100000
                }
            }
            
            enterprise = {
                features = ["all_features", "dedicated_support", "sla_guarantee"]
                limits = {
                    users = -1  // unlimited
                    storage = "100GB"
                    api_calls = 1000000
                }
            }
        }
    }
    
    tenant_management = {
        lifecycle = {
            states = ["provisioning", "active", "suspended", "deprovisioned"]
            transitions = {
                "provisioning" = ["active", "failed"]
                "active" = ["suspended", "deprovisioned"]
                "suspended" = ["active", "deprovisioned"]
                "failed" = ["provisioning"]
            }
        }
        
        billing = {
            enabled = true
            provider = "stripe"
            billing_cycle = "monthly"
            usage_tracking = true
            overage_handling = "bill_and_continue"
        }
        
        monitoring = {
            per_tenant_metrics = true
            resource_usage_tracking = true
            performance_isolation = true
            alerting = true
        }
    }
    
    data_isolation = {
        database = {
            connection_pooling = true
            max_connections_per_tenant = 10
            connection_timeout = 30
            read_replicas = true
        }
        
        cache = {
            namespace_strategy = "tenant_prefix"
            isolation_level = "strict"
            cross_tenant_access = false
        }
        
        storage = {
            bucket_strategy = "tenant_specific"
            encryption = true
            backup_strategy = "daily"
        }
        
        queue = {
            queue_per_tenant = true
            priority_queues = true
            dead_letter_queues = true
        }
    }
    
    tenant_configuration = {
        dynamic_config = true
        config_sources = [
            {
                type = "database"
                table = "tenant_configs"
                cache_ttl = 300
            },
            {
                type = "file"
                path = "/etc/tenants/{tenant_id}/config.tsk"
            },
            {
                type = "consul"
                prefix = "tenants/{tenant_id}/config/"
            }
        ]
        
        feature_flags = {
            enabled = true
            per_tenant_flags = true
            gradual_rollout = true
            a_b_testing = true
        }
        
        customization = {
            themes = true
            branding = true
            workflows = true
            integrations = true
        }
    }
}
```

## Core Multi-Tenancy Features

### 1. Tenant Identification and Routing

```php
// Tenant Identification Implementation
class TenantIdentifier {
    private $config;
    private $cache;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->cache = new RedisCache();
    }
    
    public function identifyTenant($request) {
        $methods = $this->config->multi_tenancy->tenant_identification->methods;
        
        foreach ($methods as $method) {
            $tenantId = $this->identifyByMethod($request, $method);
            if ($tenantId) {
                return $this->validateTenant($tenantId);
            }
        }
        
        return $this->config->multi_tenancy->tenant_identification->fallback_tenant;
    }
    
    private function identifyByMethod($request, $method) {
        switch ($method) {
            case 'subdomain':
                return $this->identifyBySubdomain($request);
            case 'custom_domain':
                return $this->identifyByCustomDomain($request);
            case 'header':
                return $this->identifyByHeader($request);
            case 'path':
                return $this->identifyByPath($request);
            default:
                return null;
        }
    }
    
    private function identifyBySubdomain($request) {
        $host = $request->getHost();
        $pattern = $this->config->multi_tenancy->tenant_identification->subdomain_pattern;
        $pattern = str_replace('{tenant}', '([a-zA-Z0-9-]+)', $pattern);
        
        if (preg_match("/^{$pattern}$/", $host, $matches)) {
            return $matches[1];
        }
        
        return null;
    }
    
    private function identifyByCustomDomain($request) {
        $host = $request->getHost();
        
        // Check custom domain mapping
        $mapping = $this->cache->get("custom_domain:{$host}");
        if ($mapping) {
            return $mapping;
        }
        
        // Query database for custom domain
        $tenantId = $this->queryCustomDomain($host);
        if ($tenantId) {
            $this->cache->set("custom_domain:{$host}", $tenantId, 3600);
            return $tenantId;
        }
        
        return null;
    }
    
    private function identifyByHeader($request) {
        $headerName = $this->config->multi_tenancy->tenant_identification->header_name;
        return $request->getHeader($headerName);
    }
    
    private function identifyByPath($request) {
        $path = $request->getPath();
        $pattern = $this->config->multi_tenancy->tenant_identification->path_pattern;
        $pattern = str_replace('{tenant}', '([a-zA-Z0-9-]+)', $pattern);
        
        if (preg_match("/^{$pattern}/", $path, $matches)) {
            return $matches[1];
        }
        
        return null;
    }
    
    private function validateTenant($tenantId) {
        // Check if tenant exists and is active
        $tenant = $this->getTenant($tenantId);
        
        if (!$tenant || $tenant->status !== 'active') {
            throw new TenantNotFoundException("Tenant not found or inactive: {$tenantId}");
        }
        
        return $tenantId;
    }
}

// Tenant Context Middleware
class TenantContextMiddleware {
    private $tenantIdentifier;
    private $tenantContext;
    
    public function __construct($tenantIdentifier) {
        $this->tenantIdentifier = $tenantIdentifier;
        $this->tenantContext = new TenantContext();
    }
    
    public function handle($request, $next) {
        try {
            $tenantId = $this->tenantIdentifier->identifyTenant($request);
            $this->tenantContext->setCurrentTenant($tenantId);
            
            // Add tenant info to request
            $request->setTenantId($tenantId);
            
            $response = $next($request);
            
            // Add tenant headers to response
            $response->setHeader('X-Tenant-ID', $tenantId);
            
            return $response;
        } finally {
            $this->tenantContext->clear();
        }
    }
}
```

### 2. Tenant Provisioning and Management

```php
// Tenant Provisioning Implementation
class TenantProvisioner {
    private $config;
    private $resourceManagers = [];
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->initializeResourceManagers();
    }
    
    public function provisionTenant($tenantData) {
        $tenantId = $this->generateTenantId($tenantData);
        
        try {
            // Create tenant record
            $tenant = $this->createTenantRecord($tenantId, $tenantData);
            
            // Provision resources
            $this->provisionResources($tenantId, $tenantData->template);
            
            // Initialize tenant configuration
            $this->initializeTenantConfig($tenantId, $tenantData);
            
            // Activate tenant
            $this->activateTenant($tenantId);
            
            return $tenant;
        } catch (Exception $e) {
            $this->rollbackProvisioning($tenantId);
            throw $e;
        }
    }
    
    private function provisionResources($tenantId, $template) {
        $templateConfig = $this->config->multi_tenancy->tenant_provisioning->templates->$template;
        $resources = $this->config->multi_tenancy->tenant_provisioning->resources;
        
        // Provision database
        if ($resources->database->create_schema) {
            $this->resourceManagers['database']->createSchema($tenantId);
        }
        
        if ($resources->database->run_migrations) {
            $this->resourceManagers['database']->runMigrations($tenantId);
        }
        
        if ($resources->database->seed_data) {
            $this->resourceManagers['database']->seedData($tenantId, $template);
        }
        
        // Provision storage
        if ($resources->storage->create_bucket) {
            $this->resourceManagers['storage']->createBucket($tenantId, $templateConfig->limits->storage);
        }
        
        // Provision cache
        if ($resources->cache->create_namespace) {
            $this->resourceManagers['cache']->createNamespace($tenantId);
        }
        
        // Provision queues
        if ($resources->queue->create_queues) {
            $this->resourceManagers['queue']->createQueues($tenantId);
        }
    }
    
    private function initializeTenantConfig($tenantId, $tenantData) {
        $configSources = $this->config->multi_tenancy->tenant_configuration->config_sources;
        
        foreach ($configSources as $source) {
            switch ($source->type) {
                case 'database':
                    $this->initializeDatabaseConfig($tenantId, $tenantData, $source);
                    break;
                case 'file':
                    $this->initializeFileConfig($tenantId, $tenantData, $source);
                    break;
                case 'consul':
                    $this->initializeConsulConfig($tenantId, $tenantData, $source);
                    break;
            }
        }
    }
    
    public function deprovisionTenant($tenantId) {
        // Suspend tenant first
        $this->suspendTenant($tenantId);
        
        // Archive tenant data
        $this->archiveTenantData($tenantId);
        
        // Clean up resources
        $this->cleanupResources($tenantId);
        
        // Mark tenant as deprovisioned
        $this->markTenantDeprovisioned($tenantId);
    }
    
    private function cleanupResources($tenantId) {
        // Clean up database
        $this->resourceManagers['database']->dropSchema($tenantId);
        
        // Clean up storage
        $this->resourceManagers['storage']->deleteBucket($tenantId);
        
        // Clean up cache
        $this->resourceManagers['cache']->deleteNamespace($tenantId);
        
        // Clean up queues
        $this->resourceManagers['queue']->deleteQueues($tenantId);
    }
}
```

### 3. Data Isolation Strategies

```php
// Data Isolation Implementation
class DataIsolationManager {
    private $config;
    private $isolationStrategy;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->isolationStrategy = $this->createIsolationStrategy();
    }
    
    public function getDatabaseConnection($tenantId) {
        return $this->isolationStrategy->getConnection($tenantId);
    }
    
    public function getCacheNamespace($tenantId) {
        $cacheConfig = $this->config->multi_tenancy->data_isolation->cache;
        
        if ($cacheConfig->namespace_strategy === 'tenant_prefix') {
            return "tenant:{$tenantId}:";
        }
        
        return $tenantId;
    }
    
    public function getStorageBucket($tenantId) {
        $storageConfig = $this->config->multi_tenancy->data_isolation->storage;
        
        if ($storageConfig->bucket_strategy === 'tenant_specific') {
            return "tenant-{$tenantId}-bucket";
        }
        
        return "shared-bucket";
    }
    
    public function getQueueName($tenantId, $queueType) {
        $queueConfig = $this->config->multi_tenancy->data_isolation->queue;
        
        if ($queueConfig->queue_per_tenant) {
            return "tenant-{$tenantId}-{$queueType}";
        }
        
        return $queueType;
    }
    
    private function createIsolationStrategy() {
        $strategy = $this->config->multi_tenancy->isolation_strategy;
        
        switch ($strategy) {
            case 'database_per_tenant':
                return new DatabasePerTenantStrategy($this->config);
            case 'schema_per_tenant':
                return new SchemaPerTenantStrategy($this->config);
            case 'row_level_security':
                return new RowLevelSecurityStrategy($this->config);
            default:
                throw new Exception("Unknown isolation strategy: {$strategy}");
        }
    }
}

// Database Per Tenant Strategy
class DatabasePerTenantStrategy {
    private $config;
    private $connectionPool;
    
    public function __construct($config) {
        $this->config = $config;
        $this->connectionPool = new ConnectionPool($config->multi_tenancy->data_isolation->database);
    }
    
    public function getConnection($tenantId) {
        $connectionConfig = $this->config->multi_tenancy->data_isolation->database;
        
        $connection = $this->connectionPool->getConnection($tenantId);
        
        if (!$connection) {
            // Create new connection for tenant
            $connection = $this->createTenantConnection($tenantId);
            $this->connectionPool->addConnection($tenantId, $connection);
        }
        
        return $connection;
    }
    
    private function createTenantConnection($tenantId) {
        $baseConfig = $this->config->database;
        $tenantConfig = array_merge($baseConfig, [
            'database' => "tenant_{$tenantId}",
            'username' => "tenant_{$tenantId}",
            'password' => $this->generateTenantPassword($tenantId)
        ]);
        
        return new DatabaseConnection($tenantConfig);
    }
}

// Row Level Security Strategy
class RowLevelSecurityStrategy {
    private $config;
    private $connection;
    
    public function __construct($config) {
        $this->config = $config;
        $this->connection = new DatabaseConnection($config->database);
    }
    
    public function getConnection($tenantId) {
        // Set tenant context for RLS
        $this->connection->execute("SET app.tenant_id = ?", [$tenantId]);
        
        return $this->connection;
    }
    
    public function setupRLS() {
        // Enable RLS on all tables
        $tables = $this->getTables();
        
        foreach ($tables as $table) {
            $this->connection->execute("ALTER TABLE {$table} ENABLE ROW LEVEL SECURITY");
            $this->connection->execute("CREATE POLICY tenant_isolation ON {$table} FOR ALL USING (tenant_id = current_setting('app.tenant_id')::uuid)");
        }
    }
}
```

## Advanced Multi-Tenancy Features

### 1. Dynamic Tenant Configuration

```php
// Dynamic Tenant Configuration Implementation
class DynamicTenantConfig {
    private $config;
    private $cache;
    private $configSources = [];
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->cache = new RedisCache();
        $this->initializeConfigSources();
    }
    
    public function getTenantConfig($tenantId, $key = null) {
        $cacheKey = "tenant_config:{$tenantId}";
        $config = $this->cache->get($cacheKey);
        
        if (!$config) {
            $config = $this->loadTenantConfig($tenantId);
            $this->cache->set($cacheKey, $config, $this->config->multi_tenancy->tenant_configuration->config_sources[0]->cache_ttl);
        }
        
        if ($key) {
            return $config[$key] ?? null;
        }
        
        return $config;
    }
    
    public function setTenantConfig($tenantId, $key, $value) {
        $config = $this->getTenantConfig($tenantId);
        $config[$key] = $value;
        
        // Update all config sources
        foreach ($this->configSources as $source) {
            $source->setConfig($tenantId, $key, $value);
        }
        
        // Invalidate cache
        $this->cache->delete("tenant_config:{$tenantId}");
    }
    
    public function isFeatureEnabled($tenantId, $feature) {
        $featureFlags = $this->getTenantConfig($tenantId, 'feature_flags');
        
        if (!$featureFlags) {
            return false;
        }
        
        return $featureFlags[$feature] ?? false;
    }
    
    private function loadTenantConfig($tenantId) {
        $config = [];
        
        foreach ($this->configSources as $source) {
            $sourceConfig = $source->getConfig($tenantId);
            $config = array_merge($config, $sourceConfig);
        }
        
        return $config;
    }
    
    private function initializeConfigSources() {
        $configSources = $this->config->multi_tenancy->tenant_configuration->config_sources;
        
        foreach ($configSources as $source) {
            $this->configSources[] = $this->createConfigSource($source);
        }
    }
    
    private function createConfigSource($sourceConfig) {
        switch ($sourceConfig->type) {
            case 'database':
                return new DatabaseConfigSource($sourceConfig);
            case 'file':
                return new FileConfigSource($sourceConfig);
            case 'consul':
                return new ConsulConfigSource($sourceConfig);
            default:
                throw new Exception("Unknown config source: {$sourceConfig->type}");
        }
    }
}

// Database Config Source
class DatabaseConfigSource {
    private $config;
    private $connection;
    
    public function __construct($config) {
        $this->config = $config;
        $this->connection = new DatabaseConnection();
    }
    
    public function getConfig($tenantId) {
        $sql = "SELECT config_key, config_value FROM {$this->config->table} WHERE tenant_id = ?";
        $results = $this->connection->query($sql, [$tenantId]);
        
        $config = [];
        foreach ($results as $row) {
            $config[$row['config_key']] = json_decode($row['config_value'], true);
        }
        
        return $config;
    }
    
    public function setConfig($tenantId, $key, $value) {
        $sql = "INSERT INTO {$this->config->table} (tenant_id, config_key, config_value) 
                VALUES (?, ?, ?) 
                ON DUPLICATE KEY UPDATE config_value = VALUES(config_value)";
        
        $this->connection->execute($sql, [$tenantId, $key, json_encode($value)]);
    }
}
```

### 2. Tenant Billing and Usage Tracking

```php
// Tenant Billing Implementation
class TenantBilling {
    private $config;
    private $billingProvider;
    private $usageTracker;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->billingProvider = $this->createBillingProvider();
        $this->usageTracker = new UsageTracker($this->config);
    }
    
    public function trackUsage($tenantId, $metric, $value) {
        $this->usageTracker->track($tenantId, $metric, $value);
        
        // Check if usage exceeds limits
        $this->checkUsageLimits($tenantId, $metric, $value);
    }
    
    public function generateBill($tenantId, $billingPeriod) {
        $usage = $this->usageTracker->getUsage($tenantId, $billingPeriod);
        $plan = $this->getTenantPlan($tenantId);
        
        $bill = $this->calculateBill($usage, $plan);
        
        // Send to billing provider
        $this->billingProvider->createInvoice($tenantId, $bill);
        
        return $bill;
    }
    
    private function checkUsageLimits($tenantId, $metric, $value) {
        $limits = $this->getTenantLimits($tenantId);
        $currentUsage = $this->usageTracker->getCurrentUsage($tenantId, $metric);
        
        if ($currentUsage + $value > $limits[$metric]) {
            $this->handleOverage($tenantId, $metric, $currentUsage + $value, $limits[$metric]);
        }
    }
    
    private function handleOverage($tenantId, $metric, $usage, $limit) {
        $overageHandling = $this->config->multi_tenancy->tenant_management->billing->overage_handling;
        
        switch ($overageHandling) {
            case 'bill_and_continue':
                $this->billOverage($tenantId, $metric, $usage - $limit);
                break;
            case 'throttle':
                $this->throttleTenant($tenantId, $metric);
                break;
            case 'block':
                $this->blockTenant($tenantId, $metric);
                break;
        }
    }
    
    private function createBillingProvider() {
        $provider = $this->config->multi_tenancy->tenant_management->billing->provider;
        
        switch ($provider) {
            case 'stripe':
                return new StripeBillingProvider($this->config);
            case 'chargebee':
                return new ChargebeeBillingProvider($this->config);
            default:
                throw new Exception("Unknown billing provider: {$provider}");
        }
    }
}

// Usage Tracker Implementation
class UsageTracker {
    private $config;
    private $cache;
    private $connection;
    
    public function __construct($config) {
        $this->config = $config;
        $this->cache = new RedisCache();
        $this->connection = new DatabaseConnection();
    }
    
    public function track($tenantId, $metric, $value) {
        $cacheKey = "usage:{$tenantId}:{$metric}";
        $currentUsage = $this->cache->get($cacheKey) ?? 0;
        $newUsage = $currentUsage + $value;
        
        $this->cache->set($cacheKey, $newUsage, 3600);
        
        // Store in database for billing
        $this->storeUsageRecord($tenantId, $metric, $value);
    }
    
    public function getCurrentUsage($tenantId, $metric) {
        $cacheKey = "usage:{$tenantId}:{$metric}";
        return $this->cache->get($cacheKey) ?? 0;
    }
    
    public function getUsage($tenantId, $billingPeriod) {
        $sql = "SELECT metric, SUM(value) as total_usage 
                FROM tenant_usage 
                WHERE tenant_id = ? AND created_at >= ? 
                GROUP BY metric";
        
        $startDate = $this->getBillingPeriodStart($billingPeriod);
        $results = $this->connection->query($sql, [$tenantId, $startDate]);
        
        $usage = [];
        foreach ($results as $row) {
            $usage[$row['metric']] = $row['total_usage'];
        }
        
        return $usage;
    }
    
    private function storeUsageRecord($tenantId, $metric, $value) {
        $sql = "INSERT INTO tenant_usage (tenant_id, metric, value, created_at) VALUES (?, ?, ?, NOW())";
        $this->connection->execute($sql, [$tenantId, $metric, $value]);
    }
}
```

### 3. Tenant Monitoring and Analytics

```php
// Tenant Monitoring Implementation
class TenantMonitor {
    private $config;
    private $metrics;
    private $alerting;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->metrics = new MetricsCollector($configPath);
        $this->alerting = new AlertingSystem($this->config);
    }
    
    public function trackTenantMetric($tenantId, $metric, $value, $labels = []) {
        if (!$this->config->multi_tenancy->tenant_management->monitoring->per_tenant_metrics) {
            return;
        }
        
        $labels['tenant_id'] = $tenantId;
        $this->metrics->gauge("tenant.{$metric}", $value, $labels);
        
        // Check for alerts
        $this->checkTenantAlerts($tenantId, $metric, $value);
    }
    
    public function getTenantHealth($tenantId) {
        $health = [
            'tenant_id' => $tenantId,
            'status' => 'healthy',
            'metrics' => [],
            'last_updated' => date('c')
        ];
        
        // Check resource usage
        $resourceUsage = $this->getResourceUsage($tenantId);
        $health['metrics']['resource_usage'] = $resourceUsage;
        
        // Check performance
        $performance = $this->getPerformanceMetrics($tenantId);
        $health['metrics']['performance'] = $performance;
        
        // Check errors
        $errors = $this->getErrorMetrics($tenantId);
        $health['metrics']['errors'] = $errors;
        
        // Determine overall health
        $health['status'] = $this->determineHealthStatus($health['metrics']);
        
        return $health;
    }
    
    public function getTenantAnalytics($tenantId, $timeRange = '30d') {
        $analytics = [
            'usage_trends' => $this->getUsageTrends($tenantId, $timeRange),
            'performance_trends' => $this->getPerformanceTrends($tenantId, $timeRange),
            'user_activity' => $this->getUserActivity($tenantId, $timeRange),
            'feature_usage' => $this->getFeatureUsage($tenantId, $timeRange)
        ];
        
        return $analytics;
    }
    
    private function getResourceUsage($tenantId) {
        return [
            'cpu' => $this->metrics->get("tenant.cpu_usage", ['tenant_id' => $tenantId]),
            'memory' => $this->metrics->get("tenant.memory_usage", ['tenant_id' => $tenantId]),
            'storage' => $this->metrics->get("tenant.storage_usage", ['tenant_id' => $tenantId]),
            'database_connections' => $this->metrics->get("tenant.db_connections", ['tenant_id' => $tenantId])
        ];
    }
    
    private function getPerformanceMetrics($tenantId) {
        return [
            'response_time_p50' => $this->metrics->get("tenant.response_time_p50", ['tenant_id' => $tenantId]),
            'response_time_p95' => $this->metrics->get("tenant.response_time_p95", ['tenant_id' => $tenantId]),
            'throughput' => $this->metrics->get("tenant.requests_per_second", ['tenant_id' => $tenantId]),
            'error_rate' => $this->metrics->get("tenant.error_rate", ['tenant_id' => $tenantId])
        ];
    }
    
    private function determineHealthStatus($metrics) {
        $status = 'healthy';
        
        // Check resource usage
        if ($metrics['resource_usage']['cpu'] > 80 || $metrics['resource_usage']['memory'] > 80) {
            $status = 'warning';
        }
        
        // Check performance
        if ($metrics['performance']['error_rate'] > 0.05 || $metrics['performance']['response_time_p95'] > 2000) {
            $status = 'critical';
        }
        
        return $status;
    }
}
```

## Integration Patterns

### 1. Database-Driven Multi-Tenancy

```php
// Live Database Queries in Multi-Tenancy Config
multi_tenancy_data = {
    tenant_list = @query("
        SELECT 
            tenant_id,
            name,
            status,
            plan_type,
            created_at,
            last_activity
        FROM tenants 
        WHERE status IN ('active', 'suspended')
        ORDER BY last_activity DESC
    ")
    
    tenant_usage = @query("
        SELECT 
            tenant_id,
            metric,
            SUM(value) as total_usage,
            MAX(created_at) as last_usage
        FROM tenant_usage 
        WHERE created_at >= NOW() - INTERVAL 30 DAY
        GROUP BY tenant_id, metric
        ORDER BY total_usage DESC
    ")
    
    tenant_performance = @query("
        SELECT 
            tenant_id,
            AVG(response_time) as avg_response_time,
            P95(response_time) as p95_response_time,
            COUNT(*) as request_count,
            COUNT(CASE WHEN status_code >= 400 THEN 1 END) as error_count
        FROM tenant_requests 
        WHERE created_at >= NOW() - INTERVAL 24 HOUR
        GROUP BY tenant_id
        ORDER BY avg_response_time DESC
    ")
    
    tenant_configs = @query("
        SELECT 
            tenant_id,
            config_key,
            config_value,
            updated_at
        FROM tenant_configs 
        WHERE updated_at >= NOW() - INTERVAL 7 DAY
        ORDER BY tenant_id, config_key
    ")
    
    billing_data = @query("
        SELECT 
            tenant_id,
            billing_period,
            total_amount,
            usage_amount,
            overage_amount,
            status
        FROM tenant_bills 
        WHERE billing_period >= DATE_SUB(CURDATE(), INTERVAL 12 MONTH)
        ORDER BY billing_period DESC, total_amount DESC
    ")
}
```

### 2. Real-Time Tenant Management

```php
// Real-Time Tenant Management Configuration
real_time_tenant_management = {
    tenant_events = {
        tenant_created = {
            handlers = ["provision_resources", "send_welcome_email", "initialize_analytics"]
        }
        
        tenant_suspended = {
            handlers = ["suspend_resources", "notify_admin", "backup_data"]
        }
        
        tenant_reactivated = {
            handlers = ["reactivate_resources", "send_reactivation_email", "restore_data"]
        }
        
        usage_limit_exceeded = {
            handlers = ["send_usage_alert", "throttle_requests", "notify_billing"]
        }
    }
    
    real_time_monitoring = {
        enabled = true
        metrics = ["resource_usage", "performance", "errors", "billing"]
        alerting = true
        dashboard = true
    }
    
    automated_actions = {
        scale_resources = {
            enabled = true
            triggers = ["high_cpu", "high_memory", "high_storage"]
            actions = ["increase_limits", "add_resources", "notify_admin"]
        }
        
        optimize_performance = {
            enabled = true
            triggers = ["slow_response_time", "high_error_rate"]
            actions = ["analyze_bottlenecks", "optimize_queries", "adjust_cache"]
        }
    }
}

// Real-Time Tenant Manager
class RealTimeTenantManager {
    private $config;
    private $eventHandlers = [];
    private $monitor;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->monitor = new TenantMonitor($configPath);
        $this->initializeEventHandlers();
    }
    
    public function handleTenantEvent($event) {
        $eventType = $event->getType();
        $tenantId = $event->getTenantId();
        
        if (isset($this->config->real_time_tenant_management->tenant_events->$eventType)) {
            $handlers = $this->config->real_time_tenant_management->tenant_events->$eventType->handlers;
            
            foreach ($handlers as $handlerName) {
                if (isset($this->eventHandlers[$handlerName])) {
                    $this->eventHandlers[$handlerName]->handle($event);
                }
            }
        }
        
        // Update real-time metrics
        $this->updateRealTimeMetrics($tenantId, $event);
    }
    
    public function checkAutomatedActions($tenantId) {
        $automatedActions = $this->config->real_time_tenant_management->automated_actions;
        
        foreach ($automatedActions as $actionName => $actionConfig) {
            if ($actionConfig->enabled) {
                $this->checkActionTriggers($tenantId, $actionName, $actionConfig);
            }
        }
    }
    
    private function checkActionTriggers($tenantId, $actionName, $actionConfig) {
        foreach ($actionConfig->triggers as $trigger) {
            if ($this->isTriggered($tenantId, $trigger)) {
                $this->executeActions($tenantId, $actionConfig->actions);
                break;
            }
        }
    }
    
    private function isTriggered($tenantId, $trigger) {
        switch ($trigger) {
            case 'high_cpu':
                return $this->monitor->getTenantMetric($tenantId, 'cpu_usage') > 80;
            case 'high_memory':
                return $this->monitor->getTenantMetric($tenantId, 'memory_usage') > 80;
            case 'slow_response_time':
                return $this->monitor->getTenantMetric($tenantId, 'response_time_p95') > 2000;
            case 'high_error_rate':
                return $this->monitor->getTenantMetric($tenantId, 'error_rate') > 0.05;
            default:
                return false;
        }
    }
    
    private function executeActions($tenantId, $actions) {
        foreach ($actions as $action) {
            switch ($action) {
                case 'increase_limits':
                    $this->increaseTenantLimits($tenantId);
                    break;
                case 'add_resources':
                    $this->addTenantResources($tenantId);
                    break;
                case 'notify_admin':
                    $this->notifyAdmin($tenantId, $action);
                    break;
            }
        }
    }
}
```

## Best Practices

### 1. Security and Isolation

```php
// Security Configuration
security_config = {
    tenant_isolation = {
        strict_isolation = true
        cross_tenant_access = false
        data_encryption = true
        audit_logging = true
    }
    
    access_control = {
        tenant_aware_rbac = true
        resource_quota_enforcement = true
        api_rate_limiting = true
    }
    
    data_protection = {
        tenant_data_encryption = true
        backup_encryption = true
        data_retention_policies = true
        gdpr_compliance = true
    }
}
```

### 2. Performance and Scalability

```php
// Performance Configuration
performance_config = {
    connection_pooling = {
        enabled = true
        max_connections_per_tenant = 10
        connection_timeout = 30
    }
    
    caching = {
        tenant_aware_caching = true
        cache_isolation = true
        cache_warming = true
    }
    
    resource_management = {
        auto_scaling = true
        resource_quota_enforcement = true
        performance_monitoring = true
    }
}
```

### 3. Monitoring and Observability

```php
// Monitoring Configuration
monitoring_config = {
    tenant_metrics = {
        enabled = true
        collection_interval = "1 minute"
        retention_period = "90 days"
    }
    
    alerting = {
        per_tenant_alerts = true
        escalation_policies = true
        notification_channels = ["slack", "email", "pagerduty"]
    }
    
    analytics = {
        tenant_analytics = true
        usage_patterns = true
        performance_trends = true
        business_metrics = true
    }
}
```

This comprehensive multi-tenancy and SaaS patterns documentation demonstrates how TuskLang revolutionizes SaaS development by providing intelligent, scalable, and secure multi-tenant architectures while maintaining the rebellious spirit and technical excellence that defines the TuskLang ecosystem. 