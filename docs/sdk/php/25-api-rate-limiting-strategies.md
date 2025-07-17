# API Rate Limiting Strategies with TuskLang

TuskLang revolutionizes API rate limiting by providing configuration-driven strategies that adapt to your application's needs, from simple token buckets to sophisticated adaptive rate limiting with machine learning.

## Overview

TuskLang's rate limiting capabilities go beyond simple counters, offering intelligent, adaptive, and context-aware rate limiting that scales with your application and protects against abuse while maintaining excellent user experience.

```php
// Rate Limiting Configuration
rate_limiting = {
    strategies = {
        token_bucket = {
            algorithm = "token_bucket"
            capacity = 100
            refill_rate = 10
            refill_interval = "1 second"
            burst_protection = true
        }
        
        sliding_window = {
            algorithm = "sliding_window"
            window_size = "1 minute"
            max_requests = 100
            precision = "millisecond"
        }
        
        leaky_bucket = {
            algorithm = "leaky_bucket"
            capacity = 100
            leak_rate = 10
            leak_interval = "1 second"
        }
        
        adaptive = {
            algorithm = "adaptive"
            base_rate = 100
            learning_rate = 0.1
            max_rate = 1000
            min_rate = 10
            factors = ["user_behavior", "system_load", "time_of_day"]
        }
        
        distributed = {
            algorithm = "distributed"
            storage = "redis"
            sync_interval = "1 second"
            consistency = "eventual"
            partition_strategy = "consistent_hashing"
        }
    }
    
    scopes = {
        global = {
            strategy = "token_bucket"
            key_template = "global:{endpoint}"
        }
        
        per_user = {
            strategy = "sliding_window"
            key_template = "user:{user_id}:{endpoint}"
            user_identification = "jwt_token"
        }
        
        per_ip = {
            strategy = "leaky_bucket"
            key_template = "ip:{ip_address}:{endpoint}"
            ip_extraction = "x_forwarded_for"
        }
        
        per_api_key = {
            strategy = "adaptive"
            key_template = "apikey:{api_key}:{endpoint}"
            tier_based = {
                free = { base_rate = 100, max_rate = 200 }
                premium = { base_rate = 1000, max_rate = 2000 }
                enterprise = { base_rate = 10000, max_rate = 20000 }
            }
        }
    }
    
    storage = {
        redis = {
            host = @env(REDIS_HOST, "localhost")
            port = @env(REDIS_PORT, 6379)
            database = 1
            key_prefix = "rate_limit:"
            ttl = 3600
        }
        
        memory = {
            max_entries = 10000
            eviction_policy = "lru"
            cleanup_interval = "5 minutes"
        }
    }
    
    response_headers = {
        enabled = true
        limit_header = "X-RateLimit-Limit"
        remaining_header = "X-RateLimit-Remaining"
        reset_header = "X-RateLimit-Reset"
        retry_after_header = "Retry-After"
    }
    
    retry_strategies = {
        exponential_backoff = {
            base_delay = 1
            max_delay = 60
            multiplier = 2
            jitter = true
        }
        
        fixed_delay = {
            delay = 5
            max_retries = 3
        }
    }
}
```

## Core Rate Limiting Strategies

### 1. Token Bucket Algorithm

```php
// Token Bucket Implementation
class TokenBucketRateLimiter {
    private $config;
    private $storage;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->storage = new RedisStorage($this->config->rate_limiting->storage->redis);
    }
    
    public function isAllowed($key, $tokens = 1) {
        $bucketConfig = $this->config->rate_limiting->strategies->token_bucket;
        $current = $this->storage->get($key);
        $now = microtime(true);
        
        if ($current === null) {
            $current = [
                'tokens' => $bucketConfig->capacity,
                'last_refill' => $now
            ];
        }
        
        // Refill tokens
        $timePassed = $now - $current['last_refill'];
        $tokensToAdd = floor($timePassed / $bucketConfig->refill_interval) * $bucketConfig->refill_rate;
        
        $current['tokens'] = min($bucketConfig->capacity, $current['tokens'] + $tokensToAdd);
        $current['last_refill'] = $now;
        
        // Check if request can be processed
        if ($current['tokens'] >= $tokens) {
            $current['tokens'] -= $tokens;
            $this->storage->set($key, $current, $this->config->rate_limiting->storage->redis->ttl);
            
            return [
                'allowed' => true,
                'remaining' => $current['tokens'],
                'reset_time' => $now + $bucketConfig->refill_interval,
                'limit' => $bucketConfig->capacity
            ];
        }
        
        return [
            'allowed' => false,
            'remaining' => 0,
            'reset_time' => $current['last_refill'] + $bucketConfig->refill_interval,
            'limit' => $bucketConfig->capacity,
            'retry_after' => $this->calculateRetryAfter($current['tokens'], $bucketConfig)
        ];
    }
    
    private function calculateRetryAfter($currentTokens, $config) {
        $tokensNeeded = 1 - $currentTokens;
        $refillTime = $tokensNeeded / $config->refill_rate * $config->refill_interval;
        return ceil($refillTime);
    }
}
```

### 2. Sliding Window Algorithm

```php
// Sliding Window Implementation
class SlidingWindowRateLimiter {
    private $config;
    private $storage;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->storage = new RedisStorage($this->config->rate_limiting->storage->redis);
    }
    
    public function isAllowed($key) {
        $windowConfig = $this->config->rate_limiting->strategies->sliding_window;
        $now = microtime(true);
        $windowStart = $now - $this->parseTime($windowConfig->window_size);
        
        // Get requests in current window
        $requests = $this->storage->zrangebyscore($key, $windowStart, '+inf');
        
        if (count($requests) >= $windowConfig->max_requests) {
            $oldestRequest = $this->storage->zrange($key, 0, 0, true);
            $resetTime = $oldestRequest[0] + $this->parseTime($windowConfig->window_size);
            
            return [
                'allowed' => false,
                'remaining' => 0,
                'reset_time' => $resetTime,
                'limit' => $windowConfig->max_requests
            ];
        }
        
        // Add current request
        $this->storage->zadd($key, $now, uniqid());
        $this->storage->expire($key, $this->parseTime($windowConfig->window_size));
        
        return [
            'allowed' => true,
            'remaining' => $windowConfig->max_requests - count($requests) - 1,
            'reset_time' => $now + $this->parseTime($windowConfig->window_size),
            'limit' => $windowConfig->max_requests
        ];
    }
    
    private function parseTime($timeString) {
        $unit = substr($timeString, -1);
        $value = (int) substr($timeString, 0, -1);
        
        switch ($unit) {
            case 's': return $value;
            case 'm': return $value * 60;
            case 'h': return $value * 3600;
            case 'd': return $value * 86400;
            default: return $value;
        }
    }
}
```

### 3. Adaptive Rate Limiting

```php
// Adaptive Rate Limiting Implementation
class AdaptiveRateLimiter {
    private $config;
    private $storage;
    private $mlModel;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->storage = new RedisStorage($this->config->rate_limiting->storage->redis);
        $this->mlModel = new AdaptiveRateModel($this->config->rate_limiting->strategies->adaptive);
    }
    
    public function isAllowed($key, $context = []) {
        $adaptiveConfig = $this->config->rate_limiting->strategies->adaptive;
        
        // Get current rate limit
        $currentLimit = $this->getCurrentLimit($key);
        
        // Analyze context factors
        $factors = $this->analyzeContext($context);
        
        // Predict optimal rate limit
        $predictedLimit = $this->mlModel->predict($factors);
        
        // Apply constraints
        $newLimit = max($adaptiveConfig->min_rate, 
                       min($adaptiveConfig->max_rate, $predictedLimit));
        
        // Update rate limit gradually
        $updatedLimit = $currentLimit + 
                       ($adaptiveConfig->learning_rate * ($newLimit - $currentLimit));
        
        $this->updateLimit($key, $updatedLimit);
        
        // Check if request is allowed
        $currentUsage = $this->getCurrentUsage($key);
        
        if ($currentUsage < $updatedLimit) {
            $this->incrementUsage($key);
            
            return [
                'allowed' => true,
                'remaining' => $updatedLimit - $currentUsage - 1,
                'limit' => $updatedLimit,
                'adaptive_factors' => $factors
            ];
        }
        
        return [
            'allowed' => false,
            'remaining' => 0,
            'limit' => $updatedLimit,
            'adaptive_factors' => $factors
        ];
    }
    
    private function analyzeContext($context) {
        $factors = [];
        $adaptiveConfig = $this->config->rate_limiting->strategies->adaptive;
        
        foreach ($adaptiveConfig->factors as $factor) {
            switch ($factor) {
                case 'user_behavior':
                    $factors['user_behavior'] = $this->analyzeUserBehavior($context['user_id']);
                    break;
                case 'system_load':
                    $factors['system_load'] = $this->getSystemLoad();
                    break;
                case 'time_of_day':
                    $factors['time_of_day'] = $this->getTimeOfDayFactor();
                    break;
            }
        }
        
        return $factors;
    }
    
    private function analyzeUserBehavior($userId) {
        // Analyze user's request patterns, success rates, etc.
        $recentRequests = $this->storage->get("user_behavior:{$userId}");
        $successRate = $recentRequests['success_rate'] ?? 0.95;
        $requestFrequency = $recentRequests['frequency'] ?? 1.0;
        
        return [
            'success_rate' => $successRate,
            'frequency' => $requestFrequency,
            'trust_score' => $successRate * $requestFrequency
        ];
    }
}
```

## Advanced Rate Limiting Features

### 1. Distributed Rate Limiting

```php
// Distributed Rate Limiting Configuration
distributed_rate_limiting = {
    coordination = {
        strategy = "redis_cluster"
        nodes = [
            { host = "redis-1.example.com", port = 6379 }
            { host = "redis-2.example.com", port = 6379 }
            { host = "redis-3.example.com", port = 6379 }
        ]
        replication_factor = 2
        consistency_level = "strong"
    }
    
    partitioning = {
        strategy = "consistent_hashing"
        virtual_nodes = 150
        hash_function = "crc32"
    }
    
    synchronization = {
        sync_interval = "1 second"
        conflict_resolution = "last_write_wins"
        clock_sync = true
    }
}

// Distributed Rate Limiter
class DistributedRateLimiter {
    private $config;
    private $cluster;
    private $partitionManager;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->cluster = new RedisCluster($this->config->distributed_rate_limiting->coordination);
        $this->partitionManager = new ConsistentHashPartitioner($this->config->distributed_rate_limiting->partitioning);
    }
    
    public function isAllowed($key, $tokens = 1) {
        // Determine which node should handle this key
        $node = $this->partitionManager->getNode($key);
        
        // Use distributed locking to ensure consistency
        $lockKey = "lock:{$key}";
        $lock = $this->cluster->lock($lockKey, 1000); // 1 second timeout
        
        if (!$lock) {
            throw new RateLimitException("Unable to acquire lock for rate limiting");
        }
        
        try {
            // Perform rate limiting check
            $result = $this->performRateLimitCheck($node, $key, $tokens);
            
            // Synchronize with other nodes if needed
            $this->synchronizeAcrossNodes($key, $result);
            
            return $result;
        } finally {
            $lock->release();
        }
    }
    
    private function synchronizeAcrossNodes($key, $result) {
        $syncConfig = $this->config->distributed_rate_limiting->synchronization;
        
        if ($syncConfig->sync_interval === "immediate") {
            $this->broadcastUpdate($key, $result);
        }
    }
}
```

### 2. Tier-Based Rate Limiting

```php
// Tier-Based Rate Limiting Configuration
tier_based_limiting = {
    tiers = {
        free = {
            rate_limit = 100
            burst_limit = 20
            features = ["basic_api", "standard_support"]
            cost = 0
        }
        
        premium = {
            rate_limit = 1000
            burst_limit = 100
            features = ["advanced_api", "priority_support", "analytics"]
            cost = 29.99
        }
        
        enterprise = {
            rate_limit = 10000
            burst_limit = 1000
            features = ["all_features", "dedicated_support", "sla_guarantee"]
            cost = 299.99
        }
    }
    
    upgrade_prompts = {
        enabled = true
        threshold = 0.8  // 80% of limit
        message_template = "Upgrade to {tier} for higher limits"
    }
}

// Tier-Based Rate Limiter
class TierBasedRateLimiter {
    private $config;
    private $storage;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->storage = new RedisStorage($this->config->rate_limiting->storage->redis);
    }
    
    public function isAllowed($apiKey, $endpoint) {
        // Get user's tier
        $tier = $this->getUserTier($apiKey);
        $tierConfig = $this->config->tier_based_limiting->tiers->$tier;
        
        // Check rate limit
        $key = "tier:{$tier}:{$apiKey}:{$endpoint}";
        $result = $this->checkRateLimit($key, $tierConfig->rate_limit);
        
        // Check burst limit
        $burstKey = "burst:{$tier}:{$apiKey}:{$endpoint}";
        $burstResult = $this->checkBurstLimit($burstKey, $tierConfig->burst_limit);
        
        if (!$result['allowed'] || !$burstResult['allowed']) {
            // Suggest upgrade if approaching limit
            if ($this->shouldSuggestUpgrade($result, $tierConfig)) {
                $result['upgrade_suggestion'] = $this->generateUpgradeSuggestion($tier);
            }
            
            return $result;
        }
        
        return $result;
    }
    
    private function shouldSuggestUpgrade($result, $tierConfig) {
        $upgradeConfig = $this->config->tier_based_limiting->upgrade_prompts;
        $usageRatio = ($tierConfig->rate_limit - $result['remaining']) / $tierConfig->rate_limit;
        
        return $usageRatio >= $upgradeConfig->threshold;
    }
    
    private function generateUpgradeSuggestion($currentTier) {
        $tiers = $this->config->tier_based_limiting->tiers;
        $upgradeOptions = [];
        
        foreach ($tiers as $tier => $config) {
            if ($tier !== $currentTier) {
                $upgradeOptions[] = [
                    'tier' => $tier,
                    'rate_limit' => $config->rate_limit,
                    'cost' => $config->cost,
                    'message' => str_replace('{tier}', $tier, $this->config->tier_based_limiting->upgrade_prompts->message_template)
                ];
            }
        }
        
        return $upgradeOptions;
    }
}
```

### 3. Intelligent Rate Limiting with ML

```php
// ML-Enhanced Rate Limiting Configuration
ml_rate_limiting = {
    models = {
        abuse_detection = {
            type = "anomaly_detection"
            algorithm = "isolation_forest"
            features = ["request_pattern", "ip_reputation", "user_behavior"]
            training_data = @query("SELECT * FROM request_logs WHERE created_at >= NOW() - INTERVAL 30 DAY")
        }
        
        load_prediction = {
            type = "time_series"
            algorithm = "lstm"
            features = ["hour", "day_of_week", "special_events"]
            prediction_horizon = "1 hour"
        }
        
        user_classification = {
            type = "classification"
            algorithm = "random_forest"
            features = ["request_frequency", "success_rate", "account_age"]
            classes = ["normal", "power_user", "potential_abuser"]
        }
    }
    
    adaptive_limits = {
        enabled = true
        learning_rate = 0.01
        update_frequency = "5 minutes"
        min_change_threshold = 0.05
    }
}

// ML-Enhanced Rate Limiter
class MLRateLimiter {
    private $config;
    private $models;
    private $storage;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->storage = new RedisStorage($this->config->rate_limiting->storage->redis);
        $this->initializeModels();
    }
    
    public function isAllowed($request) {
        // Extract features
        $features = $this->extractFeatures($request);
        
        // Run ML models
        $abuseScore = $this->models['abuse_detection']->predict($features);
        $loadPrediction = $this->models['load_prediction']->predict($features);
        $userClass = $this->models['user_classification']->predict($features);
        
        // Calculate adaptive rate limit
        $baseLimit = $this->getBaseLimit($request);
        $adaptiveLimit = $this->calculateAdaptiveLimit($baseLimit, $abuseScore, $loadPrediction, $userClass);
        
        // Apply rate limiting
        $key = $this->generateKey($request);
        $result = $this->applyRateLimit($key, $adaptiveLimit);
        
        // Log for model training
        $this->logForTraining($request, $features, $result);
        
        return array_merge($result, [
            'ml_insights' => [
                'abuse_score' => $abuseScore,
                'load_prediction' => $loadPrediction,
                'user_class' => $userClass,
                'adaptive_limit' => $adaptiveLimit
            ]
        ]);
    }
    
    private function extractFeatures($request) {
        return [
            'request_pattern' => $this->analyzeRequestPattern($request),
            'ip_reputation' => $this->getIPReputation($request->getClientIP()),
            'user_behavior' => $this->analyzeUserBehavior($request->getUserId()),
            'hour' => date('H'),
            'day_of_week' => date('N'),
            'request_frequency' => $this->getRequestFrequency($request->getUserId()),
            'success_rate' => $this->getSuccessRate($request->getUserId()),
            'account_age' => $this->getAccountAge($request->getUserId())
        ];
    }
    
    private function calculateAdaptiveLimit($baseLimit, $abuseScore, $loadPrediction, $userClass) {
        $adaptiveConfig = $this->config->ml_rate_limiting->adaptive_limits;
        
        // Adjust based on abuse score (lower limit for suspicious users)
        $abuseMultiplier = 1 - ($abuseScore * 0.5);
        
        // Adjust based on predicted load (higher limit during low load)
        $loadMultiplier = 1 + (1 - $loadPrediction) * 0.3;
        
        // Adjust based on user class
        $classMultiplier = $this->getClassMultiplier($userClass);
        
        $newLimit = $baseLimit * $abuseMultiplier * $loadMultiplier * $classMultiplier;
        
        // Apply constraints
        $newLimit = max($baseLimit * 0.1, min($baseLimit * 2, $newLimit));
        
        return round($newLimit);
    }
}
```

## Integration Patterns

### 1. Database-Driven Rate Limiting

```php
// Live Database Queries in Rate Limiting Config
rate_limiting_data = {
    user_limits = @query("
        SELECT 
            user_id,
            tier,
            rate_limit,
            burst_limit,
            custom_rules
        FROM user_rate_limits 
        WHERE is_active = true
    ")
    
    ip_blacklist = @query("
        SELECT ip_address, reason, expires_at
        FROM ip_blacklist 
        WHERE expires_at > NOW()
    ")
    
    rate_limit_history = @query("
        SELECT 
            user_id,
            endpoint,
            request_count,
            success_count,
            avg_response_time,
            created_at
        FROM rate_limit_metrics 
        WHERE created_at >= NOW() - INTERVAL 24 HOUR
        ORDER BY created_at DESC
    ")
    
    adaptive_limits = @query("
        SELECT 
            user_id,
            current_limit,
            suggested_limit,
            confidence_score,
            factors
        FROM adaptive_rate_limits 
        WHERE updated_at >= NOW() - INTERVAL 1 HOUR
    ")
}
```

### 2. Real-Time Rate Limit Monitoring

```php
// Rate Limit Monitoring Configuration
rate_limit_monitoring = {
    metrics = {
        request_count = {
            type = "counter"
            labels = ["user_id", "endpoint", "status"]
        }
        
        rate_limit_hits = {
            type = "counter"
            labels = ["user_id", "endpoint", "limit_type"]
        }
        
        response_time = {
            type = "histogram"
            buckets = [0.1, 0.5, 1, 2, 5]
            labels = ["user_id", "endpoint"]
        }
    }
    
    alerts = {
        high_rate_limit_hits = {
            condition = "rate_limit_hits > 100 per minute"
            severity = "warning"
            notification = ["slack", "email"]
        }
        
        suspicious_activity = {
            condition = "abuse_score > 0.8"
            severity = "critical"
            notification = ["pagerduty", "slack"]
        }
    }
    
    dashboards = {
        real_time_monitoring = {
            refresh_interval = "5 seconds"
            widgets = ["request_volume", "rate_limit_hits", "top_users", "suspicious_ips"]
        }
    }
}

// Rate Limit Monitor
class RateLimitMonitor {
    private $config;
    private $metrics;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->metrics = new PrometheusMetrics();
    }
    
    public function recordRequest($request, $result) {
        // Record basic metrics
        $this->metrics->increment('request_count', [
            'user_id' => $request->getUserId(),
            'endpoint' => $request->getPath(),
            'status' => $result['allowed'] ? 'allowed' : 'blocked'
        ]);
        
        // Record rate limit hits
        if (!$result['allowed']) {
            $this->metrics->increment('rate_limit_hits', [
                'user_id' => $request->getUserId(),
                'endpoint' => $request->getPath(),
                'limit_type' => $result['limit_type'] ?? 'default'
            ]);
        }
        
        // Record response time
        $this->metrics->histogram('response_time', $result['processing_time'], [
            'user_id' => $request->getUserId(),
            'endpoint' => $request->getPath()
        ]);
        
        // Check for alerts
        $this->checkAlerts($request, $result);
    }
    
    private function checkAlerts($request, $result) {
        foreach ($this->config->rate_limit_monitoring->alerts as $alertName => $alertConfig) {
            if ($this->evaluateAlertCondition($alertConfig->condition, $request, $result)) {
                $this->triggerAlert($alertName, $alertConfig, $request, $result);
            }
        }
    }
}
```

## Best Practices

### 1. Performance Optimization

```php
// Performance Configuration
performance_config = {
    caching = {
        enabled = true
        cache_ttl = 300
        cache_size = "100MB"
        eviction_policy = "lru"
    }
    
    batching = {
        enabled = true
        batch_size = 100
        flush_interval = "1 second"
    }
    
    connection_pooling = {
        enabled = true
        max_connections = 50
        min_connections = 10
        connection_timeout = 30
    }
    
    async_processing = {
        enabled = true
        worker_pool_size = 10
        queue_size = 1000
    }
}
```

### 2. Security Considerations

```php
// Security Configuration
security_config = {
    key_rotation = {
        enabled = true
        rotation_interval = "30 days"
        grace_period = "7 days"
    }
    
    encryption = {
        enabled = true
        algorithm = "AES-256-GCM"
        key_derivation = "pbkdf2"
    }
    
    audit_logging = {
        enabled = true
        log_level = "info"
        sensitive_fields = ["api_key", "password"]
        retention_period = "1 year"
    }
    
    ddos_protection = {
        enabled = true
        max_requests_per_second = 1000
        burst_protection = true
        ip_whitelist = ["trusted_ips"]
    }
}
```

### 3. Monitoring and Observability

```php
// Monitoring Configuration
monitoring_config = {
    health_checks = {
        enabled = true
        check_interval = "30 seconds"
        timeout = 5
        failure_threshold = 3
    }
    
    distributed_tracing = {
        enabled = true
        sampling_rate = 0.1
        propagation = "w3c"
    }
    
    logging = {
        level = "info"
        format = "json"
        structured_logging = true
        correlation_ids = true
    }
}
```

This comprehensive rate limiting documentation demonstrates how TuskLang revolutionizes API protection by providing intelligent, adaptive, and scalable rate limiting strategies while maintaining the rebellious spirit and technical excellence that defines the TuskLang ecosystem. 