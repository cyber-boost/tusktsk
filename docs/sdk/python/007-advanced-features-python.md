# 🚀 Advanced Features for Python

**"We don't bow to any king" - Enterprise-Grade Configuration**

TuskLang's advanced features bring enterprise-grade capabilities to configuration management. From performance optimization to security hardening, these features enable you to build production-ready applications.

## 🎯 Advanced Configuration Patterns

### Hierarchical Configuration

```python
from tsk import TSK

# Base configuration
base_config = TSK.from_string("""
$app_name: "MyApp"
$version: "1.0.0"

[server]
host: "0.0.0.0"
port: 8080
debug: false

[database]
host: "localhost"
port: 5432
name: "myapp"
""")

# Environment-specific overrides
dev_config = TSK.from_string("""
$environment: "development"

[server]
port: 3000
debug: true

[database]
name: "myapp_dev"
""")

prod_config = TSK.from_string("""
$environment: "production"

[server]
port: 80
debug: false

[database]
host: @env("DB_HOST")
port: @env("DB_PORT")
name: @env("DB_NAME")
user: @env("DB_USER")
password: @env("DB_PASSWORD")
""")

# Merge configurations
def load_config(environment):
    config = base_config.copy()
    
    if environment == "development":
        config.merge(dev_config)
    elif environment == "production":
        config.merge(prod_config)
    
    return config.parse()

# Usage
dev_settings = load_config("development")
prod_settings = load_config("production")
```

### Configuration Inheritance

```python
config = TSK.from_string("""
[base]
timeout: 30
retries: 3
cache_ttl: "5m"

[api:base]
endpoint: "https://api.example.com"
timeout: 60  # Override base timeout

[api.users:api]
endpoint: "https://api.example.com/users"
cache_ttl: "1m"  # Override base cache_ttl

[api.orders:api]
endpoint: "https://api.example.com/orders"
timeout: 120  # Override api timeout
""")

result = config.parse()
print(f"Users API timeout: {result['api']['users']['timeout']}")  # 60 (inherited from api)
print(f"Orders API timeout: {result['api']['orders']['timeout']}")  # 120 (overridden)
```

## 🔄 Advanced Cross-File Communication

### Dynamic File Loading

```python
config = TSK.from_string("""
$environment: @env("APP_ENV", "development")

# Dynamic file loading based on environment
config_file: @if($environment == "production", "config/prod.tsk", "config/dev.tsk")
secrets_file: @if($environment == "production", "secrets/prod.tsk", "secrets/dev.tsk")

[database]
host: @config_file.get("database.host")
port: @config_file.get("database.port")
name: @config_file.get("database.name")
password: @secrets_file.get("database.password")
""")

# Load referenced files
config.link_file('config/prod.tsk', TSK.from_file('config/prod.tsk'))
config.link_file('config/dev.tsk', TSK.from_file('config/dev.tsk'))
config.link_file('secrets/prod.tsk', TSK.from_file('secrets/prod.tsk'))
config.link_file('secrets/dev.tsk', TSK.from_file('secrets/dev.tsk'))

result = config.parse()
```

### Configuration Composition

```python
# Core configuration
core_config = TSK.from_string("""
[core]
app_name: "MyApp"
version: "1.0.0"
""")

# Feature configurations
auth_config = TSK.from_string("""
[auth]
provider: "jwt"
secret: @env("JWT_SECRET")
expires_in: "24h"
""")

cache_config = TSK.from_string("""
[cache]
provider: "redis"
host: @env("REDIS_HOST")
port: @env("REDIS_PORT")
ttl: "1h"
""")

# Compose configurations
def compose_config(*configs):
    result = TSK()
    for config in configs:
        result.merge(config)
    return result

# Usage
app_config = compose_config(core_config, auth_config, cache_config)
result = app_config.parse()
```

## ⚡ Performance Optimization

### Lazy Loading

```python
config = TSK.from_string("""
[expensive]
# Lazy evaluation - only executes when accessed
user_data: @lazy(@query("SELECT * FROM users WHERE id = ?", @request.user_id))
weather_data: @lazy(@http("GET", "https://api.weatherapi.com/v1/current.json?key=YOUR_KEY&q=London"))
complex_calculation: @lazy(@expensive_operation())
""")

# Parse without executing expensive operations
result = config.parse()

# Execute only when needed
user_data = config.get('expensive.user_data')  # Executes now
weather_data = config.get('expensive.weather_data')  # Executes now
```

### Parallel Processing

```python
import asyncio
from tsk import TSK

# Async TSK processing
async def process_config():
    config = TSK.from_string("""
    [parallel]
    # These will execute in parallel
    user_count: @async(@query("SELECT COUNT(*) FROM users"))
    order_count: @async(@query("SELECT COUNT(*) FROM orders"))
    revenue_total: @async(@query("SELECT SUM(amount) FROM orders"))
    weather_data: @async(@http("GET", "https://api.weatherapi.com/v1/current.json?key=YOUR_KEY&q=London"))
    """)
    
    result = await config.parse_async()
    return result

# Run in event loop
result = asyncio.run(process_config())
```

### Intelligent Caching

```python
config = TSK.from_string("""
[cache_strategies]
# Cache with intelligent invalidation
user_profile: @cache.smart("10m", @query("SELECT * FROM users WHERE id = ?", @request.user_id), {
    "invalidate_on": ["user_update", "user_delete"],
    "stale_while_revalidate": true,
    "background_refresh": true
})

# Cache with dependencies
user_orders: @cache.dependent("5m", @query("SELECT * FROM orders WHERE user_id = ?", @request.user_id), {
    "dependencies": ["user_profile"],
    "invalidate_on_dependency_change": true
})

# Cache with adaptive TTL
popular_content: @cache.adaptive("1h", @query("SELECT * FROM content WHERE views > 1000"), {
    "base_ttl": "1h",
    "max_ttl": "24h",
    "access_multiplier": 1.5
})
""")
```

## 🔒 Security Hardening

### Configuration Encryption

```python
config = TSK.from_string("""
[security]
# Encrypt sensitive configuration
database_password: @encrypt(@env("DB_PASSWORD"), "AES-256-GCM")
api_key: @encrypt(@env("API_KEY"), "AES-256-GCM")
jwt_secret: @encrypt(@env("JWT_SECRET"), "AES-256-GCM")

# Encrypt entire sections
secrets: @encrypt.section({
    "internal_api_key": @env("INTERNAL_API_KEY"),
    "admin_password": @env("ADMIN_PASSWORD"),
    "ssl_private_key": @file.read("ssl/private.key")
}, "AES-256-GCM")
""")
```

### Access Control

```python
config = TSK.from_string("""
[access_control]
# Role-based configuration access
admin_config: @access.role("admin", {
    "database_admin": true,
    "system_settings": true,
    "user_management": true
})

user_config: @access.role("user", {
    "profile_settings": true,
    "preferences": true
})

# Environment-based access
production_only: @access.environment("production", {
    "monitoring": true,
    "logging": "verbose",
    "backup_enabled": true
})
""")
```

### Configuration Validation

```python
config = TSK.from_string("""
[validation]
# Schema validation
user_schema: @validate.schema({
    "name": {"type": "string", "required": True, "min_length": 2},
    "email": {"type": "email", "required": True},
    "age": {"type": "integer", "min": 0, "max": 150}
}, @request.user_data)

# Business rule validation
order_validation: @validate.business_rules({
    "amount_positive": @request.amount > 0,
    "user_exists": @query.exists("SELECT 1 FROM users WHERE id = ?", @request.user_id),
    "sufficient_balance": @query("SELECT balance FROM users WHERE id = ?", @request.user_id) >= @request.amount
})
""")
```

## 🧪 Testing and Validation

### Configuration Testing

```python
import unittest
from tsk import TSK

class TestTSKConfiguration(unittest.TestCase):
    def setUp(self):
        self.config = TSK.from_string("""
        [test]
        value: 42
        string: "hello"
        boolean: true
        
        [test.functions]
        add_fujsen = '''
        def add(a, b):
            return a + b
        '''
        """)
    
    def test_basic_parsing(self):
        result = self.config.parse()
        self.assertEqual(result['test']['value'], 42)
        self.assertEqual(result['test']['string'], "hello")
        self.assertTrue(result['test']['boolean'])
    
    def test_fujsen_execution(self):
        result = self.config.execute_fujsen('test.functions', 'add', 2, 3)
        self.assertEqual(result, 5)
    
    def test_operator_execution(self):
        config = TSK.from_string("""
        [test]
        env_value: @env("TEST_VAR", "default")
        """)
        
        result = config.parse()
        self.assertEqual(result['test']['env_value'], "default")

if __name__ == '__main__':
    unittest.main()
```

### Integration Testing

```python
import pytest
from tsk import TSK
from tsk.adapters import SQLiteAdapter

@pytest.fixture
def test_db():
    return SQLiteAdapter(':memory:')

@pytest.fixture
def tsk_instance(test_db):
    tsk = TSK()
    tsk.set_database_adapter(test_db)
    return tsk

def test_database_integration(tsk_instance, test_db):
    # Setup test data
    test_db.execute("""
        CREATE TABLE users (id INTEGER, name TEXT, active BOOLEAN);
        INSERT INTO users VALUES (1, 'Alice', 1), (2, 'Bob', 0);
    """)
    
    config = TSK.from_string("""
    [users]
    count: @query("SELECT COUNT(*) FROM users")
    active_count: @query("SELECT COUNT(*) FROM users WHERE active = 1")
    """)
    
    result = tsk_instance.parse(config)
    assert result['users']['count'] == 2
    assert result['users']['active_count'] == 1

def test_http_integration(tsk_instance, requests_mock):
    # Mock HTTP requests
    requests_mock.get('https://api.example.com/data', json={'status': 'ok'})
    
    config = TSK.from_string("""
    [api]
    data: @http("GET", "https://api.example.com/data")
    """)
    
    result = tsk_instance.parse(config)
    assert result['api']['data']['status'] == 'ok'
```

## 🔄 Advanced Data Processing

### Data Transformation Pipelines

```python
config = TSK.from_string("""
[data_pipeline]
# Multi-step data transformation
raw_data: @query("SELECT * FROM raw_events")

processed_data: @pipeline(@raw_data, [
    @transform.filter("event_type", "user_action"),
    @transform.map("timestamp", @date.parse),
    @transform.aggregate("user_id", "count", "event_count"),
    @transform.sort("event_count", "desc")
])

enriched_data: @pipeline(@processed_data, [
    @transform.join("user_id", @query("SELECT id, name FROM users"), "user_name"),
    @transform.calculate("engagement_score", @formula("event_count * 0.5 + user_age * 0.1"))
])
""")
```

### Real-time Data Streaming

```python
config = TSK.from_string("""
[streaming]
# Real-time data streams
live_events: @stream("""
    SELECT * FROM events 
    WHERE created_at > ? 
    ORDER BY created_at DESC
""", @date.subtract("1h"))

user_activity: @stream("""
    SELECT user_id, action, timestamp 
    FROM user_activity 
    WHERE timestamp > ? 
    ORDER BY timestamp DESC
""", @date.subtract("5m"))

# Stream processing
processed_stream: @stream.process(@live_events, {
    "filter": @lambda("event => event.type == 'user_action'"),
    "transform": @lambda("event => ({...event, processed: true})"),
    "aggregate": @lambda("events => events.reduce((acc, e) => acc + 1, 0)")
})
""")

# Set up stream handlers
def handle_event(event):
    print(f"New event: {event}")

def handle_activity(activity):
    print(f"User activity: {activity}")

# Start streaming
tsk.start_streaming(config, 'streaming', 'live_events', handle_event)
tsk.start_streaming(config, 'streaming', 'user_activity', handle_activity)
```

## 🚀 Enterprise Features

### Configuration Management

```python
config = TSK.from_string("""
[management]
# Version control
version: "1.2.3"
change_log: @file.read("CHANGELOG.md")

# Rollback support
previous_version: @config.history.get("previous")
rollback_config: @config.rollback("1.2.2")

# Configuration diff
changes: @config.diff(@previous_version, @current_version)

# Configuration validation
validation_status: @config.validate({
    "required_sections": ["server", "database", "api"],
    "required_env_vars": ["DB_PASSWORD", "API_KEY"],
    "schema_validation": true
})
""")
```

### Monitoring and Observability

```python
config = TSK.from_string("""
[monitoring]
# Configuration health checks
health_status: @health.check({
    "database": @query("SELECT 1"),
    "api": @http("GET", "https://api.example.com/health"),
    "cache": @cache.ping()
})

# Performance metrics
config_load_time: @metrics.timer("config_load_ms")
query_execution_time: @metrics.timer("query_execution_ms")
cache_hit_rate: @metrics.ratio("cache_hits", "cache_total")

# Alerting
alerts: @alerts.check({
    "database_down": @health.database.status == "down",
    "high_latency": @metrics.response_time > 1000,
    "cache_miss_rate": @metrics.cache_hit_rate < 0.8
})
""")
```

### Multi-Environment Deployment

```python
config = TSK.from_string("""
[deployment]
# Environment-specific configurations
environments: {
    "development": {
        "server": {"port": 3000, "debug": true},
        "database": {"name": "dev_db"},
        "logging": {"level": "debug"}
    },
    "staging": {
        "server": {"port": 8080, "debug": false},
        "database": {"name": "staging_db"},
        "logging": {"level": "info"}
    },
    "production": {
        "server": {"port": 80, "debug": false},
        "database": {"name": "prod_db"},
        "logging": {"level": "error"}
    }
}

# Dynamic environment selection
current_env: @env("APP_ENV", "development")
env_config: @environments[@current_env]

# Blue-green deployment support
deployment_type: @env("DEPLOYMENT_TYPE", "blue")
active_config: @if(@deployment_type == "blue", @blue_config, @green_config)
""")
```

## 🔧 Advanced CLI Features

### Interactive Configuration

```python
# Interactive configuration builder
config = TSK.from_string("""
[interactive]
# Interactive prompts
app_name: @prompt("Enter application name", "MyApp")
database_host: @prompt("Enter database host", "localhost")
api_key: @prompt.secret("Enter API key")

# Confirmation prompts
confirm_production: @prompt.confirm("Deploy to production?")
confirm_destructive: @prompt.confirm("This will delete all data. Continue?", default=false)

# Choice prompts
database_type: @prompt.choice("Select database type", ["postgresql", "mysql", "sqlite"])
deployment_region: @prompt.choice("Select deployment region", ["us-east-1", "us-west-2", "eu-west-1"])
""")

# Run interactive configuration
result = config.parse_interactive()
```

### Configuration Validation CLI

```bash
# Validate configuration
tsk validate config.tsk

# Validate with schema
tsk validate config.tsk --schema schema.json

# Validate environment variables
tsk validate config.tsk --check-env

# Validate database connectivity
tsk validate config.tsk --check-db

# Generate configuration report
tsk report config.tsk --output report.html
```

## 🚀 Next Steps

Now that you understand advanced features:

1. **Master Basic Syntax** - [003-basic-syntax-python.md](003-basic-syntax-python.md)
2. **Learn FUJSEN Functions** - [004-fujsen-python.md](004-fujsen-python.md)
3. **Integrate Databases** - [005-database-integration-python.md](005-database-integration-python.md)
4. **Master @ Operators** - [006-at-operators-python.md](006-at-operators-python.md)

## 💡 Best Practices

1. **Use hierarchical configuration** - Organize configs by environment and feature
2. **Implement lazy loading** - Defer expensive operations until needed
3. **Cache intelligently** - Use adaptive caching for dynamic data
4. **Validate thoroughly** - Implement comprehensive validation
5. **Monitor performance** - Track configuration load times and resource usage
6. **Secure sensitive data** - Encrypt secrets and implement access control
7. **Test configurations** - Write tests for critical configuration logic
8. **Document changes** - Maintain change logs and version history

---

**"We don't bow to any king"** - TuskLang's advanced features give you enterprise-grade configuration management with the power and flexibility you need for production applications! 