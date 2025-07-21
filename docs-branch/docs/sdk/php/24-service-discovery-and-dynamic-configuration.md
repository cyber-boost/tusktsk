# Service Discovery and Dynamic Configuration with TuskLang

TuskLang empowers PHP developers to build resilient, scalable systems by providing configuration-driven service discovery and dynamic configuration management. This enables seamless scaling, zero-downtime deployments, and environment-aware applications.

## Overview

TuskLang integrates with popular service discovery backends (Consul, etcd, Eureka, Kubernetes) and supports dynamic configuration reloading, feature toggles, and environment switching—all from a single, unified config.

```php
// Service Discovery Configuration
service_discovery = {
    backend = "consul"
    address = @env(CONSUL_ADDRESS)
    datacenter = "dc1"
    token = @env(CONSUL_TOKEN)
    health_check_interval = "10s"
    retry_policy = {
        max_retries = 5
        backoff = "exponential"
    }
    
    services = {
        user_service = {
            name = "user-service"
            tags = ["v1", "php"]
            health_check = "/health"
            port = 8081
        }
        order_service = {
            name = "order-service"
            tags = ["v2", "php"]
            health_check = "/health"
            port = 8082
        }
    }
}

dynamic_config = {
    reload_interval = "30s"
    sources = [
        {
            type = "file"
            path = "/etc/tusk/config.tsk"
        },
        {
            type = "consul_kv"
            prefix = "tusk/config/"
        }
    ]
    feature_flags = {
        enable_new_checkout = @env(ENABLE_NEW_CHECKOUT, false)
        beta_mode = @consul("feature/beta_mode", false)
    }
    environment_switching = {
        default = "production"
        environments = ["development", "staging", "production"]
        current = @env(APP_ENV, "production")
    }
}
```

## Core Features

### 1. Service Registration and Discovery

```php
// Service Registration Example
class ServiceRegistrar {
    private $config;
    private $client;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->client = new ConsulClient($this->config->service_discovery->address, $this->config->service_discovery->token);
    }
    
    public function registerService($serviceName) {
        $service = $this->config->service_discovery->services->$serviceName;
        $this->client->registerService([
            'Name' => $service->name,
            'Tags' => $service->tags,
            'Port' => $service->port,
            'Check' => [
                'HTTP' => "http://localhost:{$service->port}{$service->health_check}",
                'Interval' => $this->config->service_discovery->health_check_interval
            ]
        ]);
    }
    
    public function discoverService($serviceName) {
        return $this->client->getService($serviceName);
    }
}
```

### 2. Dynamic Configuration Reloading

```php
// Dynamic Config Loader
class DynamicConfigLoader {
    private $configSources;
    private $currentConfig;
    
    public function __construct($sources) {
        $this->configSources = $sources;
        $this->reloadConfig();
    }
    
    public function reloadConfig() {
        $config = [];
        foreach ($this->configSources as $source) {
            switch ($source['type']) {
                case 'file':
                    $config = array_merge($config, $this->loadFromFile($source['path']));
                    break;
                case 'consul_kv':
                    $config = array_merge($config, $this->loadFromConsul($source['prefix']));
                    break;
            }
        }
        $this->currentConfig = $config;
    }
    
    public function get($key, $default = null) {
        return $this->currentConfig[$key] ?? $default;
    }
}
```

### 3. Feature Flags and Environment Switching

```php
// Feature Flag Example
class FeatureFlagManager {
    private $config;
    
    public function __construct($config) {
        $this->config = $config;
    }
    
    public function isEnabled($flag) {
        return $this->config['feature_flags'][$flag] ?? false;
    }
}

// Environment Switcher
class EnvironmentSwitcher {
    private $config;
    
    public function __construct($config) {
        $this->config = $config;
    }
    
    public function getCurrentEnvironment() {
        return $this->config['environment_switching']['current'];
    }
}
```

## Advanced Patterns

### 1. Zero-Downtime Deployments

- Register new service instances with a new version tag.
- Use health checks and gradual traffic shifting.
- Remove old instances only after new ones are healthy.

### 2. Multi-Cloud and Hybrid Discovery

- Use multiple backends (Consul, etcd, Kubernetes) in `service_discovery`.
- Aggregate service lists for cross-cloud failover.

### 3. Dynamic Secrets and Credentials

- Store secrets in Consul KV or Vault.
- Reference secrets in config using `@consul` or `@vault` operators.

## Best Practices

- Always use health checks for registered services.
- Use short reload intervals for dynamic config in fast-changing environments.
- Separate feature flags from core config for easier experimentation.
- Audit and log all config changes for compliance.
- Use environment switching for safe rollouts and blue/green deployments.

---

TuskLang’s service discovery and dynamic configuration features enable PHP teams to build robust, environment-aware, and highly available systems with minimal operational overhead. 