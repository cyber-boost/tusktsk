# 🟨 TuskLang JavaScript Quick Start Guide

**"We don't bow to any king" - JavaScript Edition**

Get up and running with TuskLang in JavaScript in under 5 minutes. This guide will show you how to create your first configuration file, connect to a database, and start using @ operators.

## ⚡ 5-Minute Setup

### Step 1: Install TuskLang

```bash
# Install the JavaScript SDK
npm install tusklang

# Verify installation
node -e "console.log(require('tusklang').version)"
```

### Step 2: Create Your First Configuration

Create a file called `app.tsk`:

```javascript
// app.tsk
const config = `
app {
    name: "MyAwesomeApp"
    version: "1.0.0"
    debug: true
}

server {
    host: "0.0.0.0"
    port: 3000
    workers: 4
}

database {
    host: "localhost"
    port: 5432
    name: "myapp"
    user: "postgres"
    password: @env("DB_PASSWORD", "default_password")
}

api {
    endpoint: @env("API_ENDPOINT", "https://api.example.com")
    timeout: 5000
    retries: 3
}
`;
```

### Step 3: Parse and Use Configuration

Create `index.js`:

```javascript
const TuskLang = require('tusklang');

// Parse the configuration
const config = TuskLang.parse(`
app {
    name: "MyAwesomeApp"
    version: "1.0.0"
    debug: true
}

server {
    host: "0.0.0.0"
    port: 3000
    workers: 4
}

database {
    host: "localhost"
    port: 5432
    name: "myapp"
    user: "postgres"
    password: @env("DB_PASSWORD", "default_password")
}

api {
    endpoint: @env("API_ENDPOINT", "https://api.example.com")
    timeout: 5000
    retries: 3
}
`);

// Use the configuration
console.log(`Starting ${config.app.name} v${config.app.version}`);
console.log(`Server: ${config.server.host}:${config.server.port}`);
console.log(`Database: ${config.database.host}:${config.database.port}/${config.database.name}`);
```

### Step 4: Run Your Application

```bash
# Set environment variables
export DB_PASSWORD="your_secure_password"
export API_ENDPOINT="https://your-api.com"

# Run the application
node index.js
```

## 🗃️ Database Integration (2 Minutes)

### Step 5: Add Database Queries

Update your `app.tsk`:

```javascript
const config = `
app {
    name: "MyAwesomeApp"
    version: "1.0.0"
    debug: true
}

server {
    host: "0.0.0.0"
    port: 3000
    workers: 4
}

database {
    host: "localhost"
    port: 5432
    name: "myapp"
    user: "postgres"
    password: @env("DB_PASSWORD", "default_password")
    
    # Database queries in configuration!
    user_count: @query("SELECT COUNT(*) FROM users")
    active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
    recent_orders: @query("SELECT * FROM orders WHERE created_at > ?", @date.subtract("7d"))
}

api {
    endpoint: @env("API_ENDPOINT", "https://api.example.com")
    timeout: 5000
    retries: 3
}
`;
```

### Step 6: Set Up Database Connection

Create `database.js`:

```javascript
const TuskLang = require('tusklang');
const PostgreSQLAdapter = require('tusklang/adapters/postgresql');

// Create database adapter
const postgres = new PostgreSQLAdapter({
    host: 'localhost',
    port: 5432,
    database: 'myapp',
    user: 'postgres',
    password: process.env.DB_PASSWORD
});

// Create enhanced TuskLang instance
const tsk = new TuskLang.Enhanced();
tsk.setDatabaseAdapter(postgres);

// Parse configuration with database queries
const config = TuskLang.parse(`
app {
    name: "MyAwesomeApp"
    version: "1.0.0"
    debug: true
}

database {
    host: "localhost"
    port: 5432
    name: "myapp"
    user: "postgres"
    password: @env("DB_PASSWORD", "default_password")
    
    # Database queries in configuration!
    user_count: @query("SELECT COUNT(*) FROM users")
    active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
    recent_orders: @query("SELECT * FROM orders WHERE created_at > ?", @date.subtract("7d"))
}
`);

// Execute and get results
async function startApp() {
    try {
        const result = await tsk.parse(config);
        
        console.log(`✅ ${result.app.name} v${result.app.version} started!`);
        console.log(`📊 Total users: ${result.database.user_count}`);
        console.log(`👥 Active users: ${result.database.active_users}`);
        console.log(`📦 Recent orders: ${result.database.recent_orders.length}`);
        
    } catch (error) {
        console.error('❌ Error starting app:', error.message);
    }
}

startApp();
```

## 🚀 Advanced Features (3 Minutes)

### Step 7: Add @ Operators and Cross-File Communication

Create `peanu.tsk` (global configuration):

```javascript
// peanu.tsk - Global configuration
const globalConfig = `
$app_name: "MyAwesomeApp"
$environment: @env("NODE_ENV", "development")
$version: "1.0.0"

[paths]
log_dir: "/var/log/${app_name}"
data_dir: "/var/lib/${app_name}"
config_dir: "/etc/${app_name}"

[features]
debug: @if($environment != "production", true, false)
logging: @if($environment == "production", "json", "text")
cache_ttl: @if($environment == "production", "1h", "5m")
`;
```

Create `app.tsk` (application-specific):

```javascript
// app.tsk - Application configuration
const appConfig = `
# Reference global configuration
$app_name: @peanu.tsk.get("app_name")
$environment: @peanu.tsk.get("environment")

app {
    name: $app_name
    version: @peanu.tsk.get("version")
    debug: @peanu.tsk.get("features.debug")
}

server {
    host: "0.0.0.0"
    port: @if($environment == "production", 80, 3000)
    workers: @if($environment == "production", 4, 1)
}

database {
    host: "localhost"
    port: 5432
    name: "myapp"
    user: "postgres"
    password: @env.secure("DB_PASSWORD")
    
    # Advanced database queries
    user_count: @query("SELECT COUNT(*) FROM users")
    active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
    recent_orders: @query("SELECT * FROM orders WHERE created_at > ?", @date.subtract("7d"))
    
    # Cached queries
    popular_products: @cache("1h", @query("SELECT * FROM products ORDER BY sales DESC LIMIT 10"))
}

api {
    endpoint: @env("API_ENDPOINT", "https://api.example.com")
    timeout: 5000
    retries: 3
    
    # Rate limiting
    rate_limit: @if($environment == "production", 100, 1000)
    
    # Authentication
    auth_required: @if($environment == "production", true, false)
}

logging {
    level: @if($environment == "production", "error", "debug")
    format: @peanu.tsk.get("features.logging")
    file: @peanu.tsk.get("paths.log_dir") + "/app.log"
    
    # Metrics
    metrics_enabled: @if($environment == "production", true, false)
    metrics_interval: "5m"
}

cache {
    driver: "redis"
    host: "localhost"
    port: 6379
    ttl: @peanu.tsk.get("features.cache_ttl")
    
    # Cache warming
    warm_on_startup: @if($environment == "production", true, false)
}
`;
```

### Step 8: Create Enhanced Application

Create `enhanced-app.js`:

```javascript
const TuskLang = require('tusklang');
const PostgreSQLAdapter = require('tusklang/adapters/postgresql');
const RedisAdapter = require('tusklang/adapters/redis');

// Set up database adapters
const postgres = new PostgreSQLAdapter({
    host: 'localhost',
    port: 5432,
    database: 'myapp',
    user: 'postgres',
    password: process.env.DB_PASSWORD
});

const redis = new RedisAdapter({
    host: 'localhost',
    port: 6379
});

// Create enhanced TuskLang instance
const tsk = new TuskLang.Enhanced();
tsk.setDatabaseAdapter(postgres);
tsk.setCacheAdapter(redis);

// Load global configuration
const globalConfig = TuskLang.parse(`
$app_name: "MyAwesomeApp"
$environment: @env("NODE_ENV", "development")
$version: "1.0.0"

[paths]
log_dir: "/var/log/${app_name}"
data_dir: "/var/lib/${app_name}"
config_dir: "/etc/${app_name}"

[features]
debug: @if($environment != "production", true, false)
logging: @if($environment == "production", "json", "text")
cache_ttl: @if($environment == "production", "1h", "5m")
`);

// Link global configuration
tsk.linkFile('peanu.tsk', globalConfig);

// Parse application configuration
const appConfig = TuskLang.parse(`
$app_name: @peanu.tsk.get("app_name")
$environment: @peanu.tsk.get("environment")

app {
    name: $app_name
    version: @peanu.tsk.get("version")
    debug: @peanu.tsk.get("features.debug")
}

server {
    host: "0.0.0.0"
    port: @if($environment == "production", 80, 3000)
    workers: @if($environment == "production", 4, 1)
}

database {
    host: "localhost"
    port: 5432
    name: "myapp"
    user: "postgres"
    password: @env.secure("DB_PASSWORD")
    
    user_count: @query("SELECT COUNT(*) FROM users")
    active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
    recent_orders: @query("SELECT * FROM orders WHERE created_at > ?", @date.subtract("7d"))
    popular_products: @cache("1h", @query("SELECT * FROM products ORDER BY sales DESC LIMIT 10"))
}

api {
    endpoint: @env("API_ENDPOINT", "https://api.example.com")
    timeout: 5000
    retries: 3
    rate_limit: @if($environment == "production", 100, 1000)
    auth_required: @if($environment == "production", true, false)
}

logging {
    level: @if($environment == "production", "error", "debug")
    format: @peanu.tsk.get("features.logging")
    file: @peanu.tsk.get("paths.log_dir") + "/app.log"
    metrics_enabled: @if($environment == "production", true, false)
    metrics_interval: "5m"
}

cache {
    driver: "redis"
    host: "localhost"
    port: 6379
    ttl: @peanu.tsk.get("features.cache_ttl")
    warm_on_startup: @if($environment == "production", true, false)
}
`);

// Start enhanced application
async function startEnhancedApp() {
    try {
        const result = await tsk.parse(appConfig);
        
        console.log(`🚀 ${result.app.name} v${result.app.version} started!`);
        console.log(`🌍 Environment: ${process.env.NODE_ENV || 'development'}`);
        console.log(`🔧 Debug mode: ${result.app.debug}`);
        console.log(`🌐 Server: ${result.server.host}:${result.server.port}`);
        console.log(`👥 Workers: ${result.server.workers}`);
        console.log(`📊 Total users: ${result.database.user_count}`);
        console.log(`👤 Active users: ${result.database.active_users}`);
        console.log(`📦 Recent orders: ${result.database.recent_orders.length}`);
        console.log(`🔥 Popular products: ${result.database.popular_products.length}`);
        console.log(`🔗 API endpoint: ${result.api.endpoint}`);
        console.log(`⚡ Rate limit: ${result.api.rate_limit}/min`);
        console.log(`🔐 Auth required: ${result.api.auth_required}`);
        console.log(`📝 Log level: ${result.logging.level}`);
        console.log(`💾 Cache TTL: ${result.cache.ttl}`);
        
    } catch (error) {
        console.error('❌ Error starting enhanced app:', error.message);
    }
}

startEnhancedApp();
```

## 🎯 What You've Accomplished

In just 8 steps, you've created a production-ready JavaScript application with:

✅ **Basic Configuration Parsing** - TuskLang syntax with multiple styles  
✅ **Environment Variable Integration** - Secure configuration management  
✅ **Database Queries in Config** - Direct SQL in configuration files  
✅ **Cross-File Communication** - Global `peanu.tsk` configuration  
✅ **@ Operator System** - Environment variables, caching, conditional logic  
✅ **Type Safety** - Full TypeScript support  
✅ **Production Features** - Rate limiting, caching, metrics, logging  

## 📚 Next Steps

1. **[Basic Syntax](003-basic-syntax-javascript.md)** - Master TuskLang syntax patterns
2. **[Database Integration](004-database-integration-javascript.md)** - Deep dive into database features
3. **[Advanced Features](005-advanced-features-javascript.md)** - @ operators, FUJSEN, and more
4. **[Framework Integration](006-framework-integration-javascript.md)** - Express.js, Next.js, and more

## 🎉 Congratulations!

You've successfully set up TuskLang for JavaScript! You now have a powerful configuration system that can:

- **Query databases directly** from configuration files
- **Execute JavaScript functions** embedded in config
- **Share variables** across multiple files
- **Adapt to your syntax preferences** (INI, JSON, XML styles)
- **Scale from development to production** seamlessly

**Ready to revolutionize your JavaScript configuration? Let's dive deeper!** 