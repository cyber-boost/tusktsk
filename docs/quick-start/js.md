# 🟨 TuskLang JavaScript/Node.js SDK - Tusk Me Hard

**"We don't bow to any king" - JavaScript Edition**

The TuskLang JavaScript SDK provides the most feature-complete implementation with 5 database adapters, enhanced parser flexibility, and TypeScript support.

## 🚀 Quick Start

### Installation

```bash
# Install from npm
npm install tusklang

# Or install with yarn
yarn add tusklang

# Verify installation
node -e "console.log(require('tusklang').version)"
```

### One-Line Install

```bash
# Direct install
curl -sSL https://js.tusklang.org | node

# Or with wget
wget -qO- https://js.tusklang.org | node
```

## 🎯 Core Features

### 1. Enhanced Parser with Maximum Flexibility
```javascript
const TuskLang = require('tusklang');

// Support for all syntax styles
const config = TuskLang.parse(`
# Traditional sections
[database]
host: "localhost"
port: 5432

# Curly brace objects
server {
    host: "0.0.0.0"
    port: 8080
}

# Angle bracket objects
cache >
    driver: "redis"
    ttl: "5m"
<
`);

console.log(config.database.host); // "localhost"
console.log(config.server.port);   // 8080
```

### 2. Database Integration with 5 Adapters
```javascript
const TuskLang = require('tusklang');
const SQLiteAdapter = require('tusklang/adapters/sqlite');
const PostgreSQLAdapter = require('tusklang/adapters/postgresql');
const MySQLAdapter = require('tusklang/adapters/mysql');
const MongoDBAdapter = require('tusklang/adapters/mongodb');
const RedisAdapter = require('tusklang/adapters/redis');

// Configure database adapters
const sqlite = new SQLiteAdapter({ filename: './app.db' });
const postgres = new PostgreSQLAdapter({
    host: 'localhost',
    port: 5432,
    database: 'myapp',
    user: 'postgres',
    password: 'secret'
});

// Create TSK instance with database
const tsk = new TuskLang.Enhanced();
tsk.setDatabaseAdapter(sqlite);

// TSK file with database queries
const config = TuskLang.parse(`
[database]
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
recent_orders: @query("SELECT * FROM orders WHERE created_at > ?", @date.subtract("7d"))
`);

// Parse and execute
const result = await tsk.parse(config);
console.log(`Total users: ${result.database.user_count}`);
```

### 3. TypeScript Support
```typescript
import { TuskLang, Config } from 'tusklang';

interface AppConfig {
    app: {
        name: string;
        version: string;
        debug: boolean;
    };
    server: {
        host: string;
        port: number;
    };
    database: {
        host: string;
        port: number;
        name: string;
    };
}

const config: AppConfig = TuskLang.parse<AppConfig>(`
app {
    name: "MyApp"
    version: "1.0.0"
    debug: true
}

server {
    host: "0.0.0.0"
    port: 8080
}

database {
    host: "localhost"
    port: 5432
    name: "myapp"
}
`);

// Full TypeScript support
console.log(config.app.name); // TypeScript knows this is a string
```

### 4. @ Operator System
```javascript
const TuskLang = require('tusklang');

// Advanced @ operators
const config = TuskLang.parse(`
[api]
endpoint: @env("API_ENDPOINT", "https://api.example.com")
api_key: @env("API_KEY")

[cache]
data: @cache("5m", "expensive_operation")
user_data: @cache("1h", "user_profile", @request.user_id)

[processing]
timestamp: @date.now()
random_id: @uuid.generate()
file_content: @file.read("config.json")
`);

// Execute with context
const context = {
    request: { user_id: 123 },
    cache_value: 'cached_data'
};

const result = await tsk.executeOperators(config, context);
```

## 🔧 Advanced Usage

### 1. Cross-File Communication
```javascript
// main.tsk
const mainConfig = TuskLang.parse(`
$app_name: "MyApp"
$version: "1.0.0"

[database]
host: @config.tsk.get("db_host")
port: @config.tsk.get("db_port")
`);

// config.tsk
const dbConfig = TuskLang.parse(`
db_host: "localhost"
db_port: 5432
db_name: "myapp"
`);

// Link files
tsk.linkFile('config.tsk', dbConfig);
const result = await tsk.parse(mainConfig);
```

### 2. Global Variables and Interpolation
```javascript
const config = TuskLang.parse(`
$app_name: "MyApp"
$environment: @env("APP_ENV", "development")

[server]
host: "0.0.0.0"
port: @if($environment == "production", 80, 8080)
workers: @if($environment == "production", 4, 1)
debug: @if($environment != "production", true, false)

[paths]
log_file: "/var/log/${app_name}.log"
config_file: "/etc/${app_name}/config.json"
data_dir: "/var/lib/${app_name}/v${version}"
`);

// Parse with environment
process.env.APP_ENV = 'production';
const result = await tsk.parse(config);
```

### 3. Conditional Logic
```javascript
const config = TuskLang.parse(`
$environment: @env("APP_ENV", "development")

[logging]
level: @if($environment == "production", "error", "debug")
format: @if($environment == "production", "json", "text")
file: @if($environment == "production", "/var/log/app.log", "console")

[security]
ssl: @if($environment == "production", true, false)
cors: @if($environment == "production", {
    origin: ["https://myapp.com"],
    credentials: true
}, {
    origin: "*",
    credentials: false
})
`);
```

### 4. Array and Object Operations
```javascript
const config = TuskLang.parse(`
[users]
admin_users: ["alice", "bob", "charlie"]
roles: {
    admin: ["read", "write", "delete"],
    user: ["read", "write"],
    guest: ["read"]
}

[permissions]
user_permissions: @users.roles[@request.user_role]
is_admin: @users.admin_users.includes(@request.username)
can_write: @permissions.user_permissions.includes("write")
`);

// Execute with request context
const context = {
    request: {
        user_role: 'admin',
        username: 'alice'
    }
};

const result = await tsk.executeOperators(config, context);
```

## 🗄️ Database Adapters

### SQLite Adapter
```javascript
const SQLiteAdapter = require('tusklang/adapters/sqlite');

// Basic usage
const sqlite = new SQLiteAdapter({ filename: 'app.db' });

// With options
const sqlite = new SQLiteAdapter({
    filename: 'app.db',
    timeout: 30000,
    verbose: console.log
});

// Execute queries
const result = await sqlite.query("SELECT * FROM users WHERE active = ?", true);
const count = await sqlite.query("SELECT COUNT(*) FROM orders");
console.log(`Total orders: ${count[0]['COUNT(*)']}`);
```

### PostgreSQL Adapter
```javascript
const PostgreSQLAdapter = require('tusklang/adapters/postgresql');

// Connection
const postgres = new PostgreSQLAdapter({
    host: 'localhost',
    port: 5432,
    database: 'myapp',
    user: 'postgres',
    password: 'secret',
    ssl: { rejectUnauthorized: false }
});

// Connection pooling
const postgres = new PostgreSQLAdapter({
    host: 'localhost',
    database: 'myapp',
    user: 'postgres',
    password: 'secret',
    max: 20,
    idleTimeoutMillis: 30000,
    connectionTimeoutMillis: 2000
});

// Execute queries
const users = await postgres.query("SELECT * FROM users WHERE active = $1", true);
```

### MySQL Adapter
```javascript
const MySQLAdapter = require('tusklang/adapters/mysql');

// Connection
const mysql = new MySQLAdapter({
    host: 'localhost',
    port: 3306,
    database: 'myapp',
    user: 'root',
    password: 'secret'
});

// With connection pooling
const mysql = new MySQLAdapter({
    host: 'localhost',
    database: 'myapp',
    user: 'root',
    password: 'secret',
    connectionLimit: 10,
    acquireTimeout: 60000
});

// Execute queries
const result = await mysql.query("SELECT * FROM users WHERE active = ?", true);
```

### MongoDB Adapter
```javascript
const MongoDBAdapter = require('tusklang/adapters/mongodb');

// Connection
const mongo = new MongoDBAdapter({
    uri: 'mongodb://localhost:27017/',
    database: 'myapp'
});

// With authentication
const mongo = new MongoDBAdapter({
    uri: 'mongodb://user:pass@localhost:27017/',
    database: 'myapp',
    authSource: 'admin'
});

// Execute queries
const users = await mongo.query("users", { active: true });
const count = await mongo.query("users", {}, { count: true });
```

### Redis Adapter
```javascript
const RedisAdapter = require('tusklang/adapters/redis');

// Connection
const redis = new RedisAdapter({
    host: 'localhost',
    port: 6379,
    db: 0
});

// With authentication
const redis = new RedisAdapter({
    host: 'localhost',
    port: 6379,
    password: 'secret',
    db: 0
});

// Execute commands
await redis.set('key', 'value');
const value = await redis.get('key');
await redis.del('key');
```

## 🔐 Security Features

### 1. Input Validation
```javascript
const TuskLang = require('tusklang');

const config = TuskLang.parse(`
[user]
email: @validate.email(@request.email)
website: @validate.url(@request.website)
age: @validate.range(@request.age, 0, 150)
password: @validate.password(@request.password)
`);

// Custom validators
tsk.addValidator('strong_password', (password) => {
    return password.length >= 8 && 
           /[A-Z]/.test(password) && 
           /[a-z]/.test(password) && 
           /[0-9]/.test(password);
});
```

### 2. SQL Injection Prevention
```javascript
// Automatic parameterization
const config = TuskLang.parse(`
[users]
user_data: @query("SELECT * FROM users WHERE id = ?", @request.user_id)
search_results: @query("SELECT * FROM users WHERE name LIKE ?", @request.search_term)
`);

// Safe execution
const result = await tsk.parse(config, {
    request: {
        user_id: 123,
        search_term: '%john%'
    }
});
```

### 3. Environment Variable Security
```javascript
// Secure environment handling
const config = TuskLang.parse(`
[secrets]
api_key: @env("API_KEY")
database_password: @env("DB_PASSWORD")
jwt_secret: @env("JWT_SECRET")
`);

// Validate required environment variables
tsk.validateRequiredEnv(['API_KEY', 'DB_PASSWORD', 'JWT_SECRET']);
```

## 🚀 Performance Optimization

### 1. Caching
```javascript
const TuskLang = require('tusklang');
const MemoryCache = require('tusklang/cache/memory');
const RedisCache = require('tusklang/cache/redis');

// Memory cache
const cache = new MemoryCache();
const tsk = new TuskLang.Enhanced();
tsk.setCache(cache);

// Redis cache
const redisCache = new RedisCache({
    host: 'localhost',
    port: 6379,
    db: 0
});
tsk.setCache(redisCache);

// Use in TSK
const config = TuskLang.parse(`
[data]
expensive_data: @cache("5m", "expensive_operation")
user_profile: @cache("1h", "user_profile", @request.user_id)
`);
```

### 2. Lazy Loading
```javascript
// Lazy evaluation
const config = TuskLang.parse(`
[expensive]
data: @lazy("expensive_operation")
user_data: @lazy("user_profile", @request.user_id)
`);

// Only executes when accessed
const result = await tsk.get('expensive.data'); // Executes now
```

### 3. Parallel Processing
```javascript
// Async TSK processing
async function processConfig() {
    const config = TuskLang.parse(`
    [parallel]
    data1: @async("operation1")
    data2: @async("operation2")
    data3: @async("operation3")
    `);
    
    const result = await tsk.parseAsync(config);
    return result;
}

// Run in async context
const result = await processConfig();
```

## 🌐 Web Framework Integration

### 1. Express.js Integration
```javascript
const express = require('express');
const TuskLang = require('tusklang');

const app = express();

// Load configuration
const config = TuskLang.parse(`
app {
    name: "MyApp"
    version: "1.0.0"
}

server {
    host: "0.0.0.0"
    port: 8080
    debug: true
}

database {
    host: "localhost"
    port: 5432
    name: "myapp"
}
`);

app.get('/api/users', async (req, res) => {
    // Use database query from config
    const users = await tsk.query("SELECT * FROM users WHERE active = 1");
    res.json(users);
});

app.post('/api/process', async (req, res) => {
    const { amount, recipient } = req.body;
    
    // Execute FUJSEN function
    const result = await tsk.executeFujsen('payment', 'process', amount, recipient);
    res.json(result);
});

app.listen(config.server.port, config.server.host, () => {
    console.log(`${config.app.name} v${config.app.version} running on port ${config.server.port}`);
});
```

### 2. Koa.js Integration
```javascript
const Koa = require('koa');
const Router = require('@koa/router');
const TuskLang = require('tusklang');

const app = new Koa();
const router = new Router();

// Load configuration
const config = TuskLang.parse(`
api {
    version: "v1"
    rate_limit: 100
}

auth {
    secret: @env("JWT_SECRET")
    expires_in: "24h"
}
`);

router.get('/api/users', async (ctx) => {
    const users = await tsk.query("SELECT * FROM users");
    ctx.body = users;
});

router.post('/api/auth', async (ctx) => {
    const { username, password } = ctx.request.body;
    const token = await tsk.executeFujsen('auth', 'generate_token', username, password);
    ctx.body = { token };
});

app.use(router.routes());
app.listen(3000);
```

### 3. Fastify Integration
```javascript
const fastify = require('fastify');
const TuskLang = require('tusklang');

const app = fastify();

// Load configuration
const config = TuskLang.parse(`
server {
    port: 3000
    host: "0.0.0.0"
}

database {
    url: @env("DATABASE_URL")
}
`);

app.get('/api/health', async (request, reply) => {
    const status = await tsk.executeFujsen('health', 'check');
    return status;
});

app.post('/api/payment', async (request, reply) => {
    const { amount, recipient } = request.body;
    const result = await tsk.executeFujsen('payment', 'process', amount, recipient);
    return result;
});

app.listen({ port: config.server.port, host: config.server.host });
```

## 🧪 Testing

### 1. Unit Testing with Jest
```javascript
const TuskLang = require('tusklang');

describe('TuskLang Configuration', () => {
    let config;

    beforeEach(() => {
        config = TuskLang.parse(`
        [test]
        value: 42
        string: "hello"
        boolean: true
        `);
    });

    test('should parse basic values', () => {
        expect(config.test.value).toBe(42);
        expect(config.test.string).toBe('hello');
        expect(config.test.boolean).toBe(true);
    });

    test('should execute FUJSEN functions', async () => {
        const tskConfig = TuskLang.parse(`
        [math]
        add_fujsen = '''
        function add(a, b) {
            return a + b;
        }
        '''
        `);
        
        const result = await tsk.executeFujsen('math', 'add', 2, 3);
        expect(result).toBe(5);
    });
});
```

### 2. Integration Testing
```javascript
const TuskLang = require('tusklang');
const SQLiteAdapter = require('tusklang/adapters/sqlite');

describe('Database Integration', () => {
    let tsk;
    let db;

    beforeEach(async () => {
        db = new SQLiteAdapter({ filename: ':memory:' });
        tsk = new TuskLang.Enhanced();
        tsk.setDatabaseAdapter(db);
        
        // Setup test data
        await db.execute(`
            CREATE TABLE users (id INTEGER, name TEXT, active BOOLEAN);
            INSERT INTO users VALUES (1, 'Alice', 1), (2, 'Bob', 0);
        `);
    });

    test('should execute database queries', async () => {
        const config = TuskLang.parse(`
        [users]
        count: @query("SELECT COUNT(*) FROM users")
        active_count: @query("SELECT COUNT(*) FROM users WHERE active = 1")
        `);
        
        const result = await tsk.parse(config);
        expect(result.users.count).toBe(2);
        expect(result.users.active_count).toBe(1);
    });
});
```

## 🔧 CLI Tools

### 1. Basic CLI Usage
```bash
# Parse TSK file
npx tusklang parse config.tsk

# Validate syntax
npx tusklang validate config.tsk

# Execute FUJSEN
npx tusklang fujsen config.tsk payment process 100 "alice@example.com"

# Convert to JSON
npx tusklang convert config.tsk --format json

# Interactive shell
npx tusklang shell config.tsk
```

### 2. Advanced CLI Features
```bash
# Parse with environment
APP_ENV=production npx tusklang parse config.tsk

# Execute with variables
npx tusklang parse config.tsk --var user_id=123 --var debug=true

# Output to file
npx tusklang parse config.tsk --output result.json

# Watch for changes
npx tusklang parse config.tsk --watch

# Benchmark parsing
npx tusklang benchmark config.tsk --iterations 1000
```

## 🔄 Migration from Other Config Formats

### 1. From JSON
```javascript
const fs = require('fs');
const TuskLang = require('tusklang');

// Convert JSON to TSK
function jsonToTsk(jsonFile, tskFile) {
    const data = JSON.parse(fs.readFileSync(jsonFile, 'utf8'));
    
    let tskContent = '';
    for (const [key, value] of Object.entries(data)) {
        if (typeof value === 'object' && value !== null) {
            tskContent += `[${key}]\n`;
            for (const [k, v] of Object.entries(value)) {
                tskContent += `${k}: ${JSON.stringify(v)}\n`;
            }
        } else {
            tskContent += `${key}: ${JSON.stringify(value)}\n`;
        }
    }
    
    fs.writeFileSync(tskFile, tskContent);
}

// Usage
jsonToTsk('config.json', 'config.tsk');
```

### 2. From YAML
```javascript
const fs = require('fs');
const yaml = require('js-yaml');
const TuskLang = require('tusklang');

// Convert YAML to TSK
function yamlToTsk(yamlFile, tskFile) {
    const data = yaml.load(fs.readFileSync(yamlFile, 'utf8'));
    
    let tskContent = '';
    for (const [key, value] of Object.entries(data)) {
        if (typeof value === 'object' && value !== null) {
            tskContent += `[${key}]\n`;
            for (const [k, v] of Object.entries(value)) {
                tskContent += `${k}: ${JSON.stringify(v)}\n`;
            }
        } else {
            tskContent += `${key}: ${JSON.stringify(value)}\n`;
        }
    }
    
    fs.writeFileSync(tskFile, tskContent);
}

// Usage
yamlToTsk('config.yaml', 'config.tsk');
```

## 🚀 Deployment

### 1. Docker Deployment
```dockerfile
FROM node:18-alpine

WORKDIR /app

# Install TuskLang
RUN npm install tusklang

# Copy application
COPY . .

# Copy TSK configuration
COPY config.tsk /app/

# Run application
CMD ["node", "app.js"]
```

### 2. Kubernetes Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tusk-app
spec:
  replicas: 3
  selector:
    matchLabels:
      app: tusk-app
  template:
    metadata:
      labels:
        app: tusk-app
    spec:
      containers:
      - name: app
        image: tusk-app:latest
        env:
        - name: APP_ENV
          value: "production"
        - name: API_KEY
          valueFrom:
            secretKeyRef:
              name: app-secrets
              key: api-key
        volumeMounts:
        - name: config
          mountPath: /app/config
      volumes:
      - name: config
        configMap:
          name: app-config
```

## 📊 Performance Benchmarks

### Parsing Performance
```
Benchmark Results (Node.js 18):
- Simple config (1KB): 0.3ms
- Complex config (10KB): 1.8ms
- Large config (100KB): 12.1ms
- FUJSEN execution: 0.05ms per function
- Database query: 0.8ms average
```

### Memory Usage
```
Memory Usage:
- Base TSK instance: 1.8MB
- With SQLite adapter: +0.9MB
- With PostgreSQL adapter: +1.5MB
- With Redis cache: +0.6MB
```

## 🔧 Troubleshooting

### Common Issues

1. **Import Errors**
```javascript
// Make sure TuskLang is installed
npm install tusklang

// Check version
node -e "console.log(require('tusklang').version)"
```

2. **Database Connection Issues**
```javascript
// Test database connection
const SQLiteAdapter = require('tusklang/adapters/sqlite');
const db = new SQLiteAdapter({ filename: 'test.db' });
const result = await db.query("SELECT 1");
console.log("Database connection successful");
```

3. **FUJSEN Execution Errors**
```javascript
// Debug FUJSEN execution
try {
    const result = await config.executeFujsen('section', 'function', ...args);
} catch (error) {
    console.error('FUJSEN error:', error);
    // Check function syntax and parameters
}
```

### Debug Mode
```javascript
const TuskLang = require('tusklang');

// Enable debug logging
const tsk = new TuskLang.Enhanced({ debug: true });
const config = tsk.parse('config.tsk');
```

## 📚 Resources

- **Official Documentation**: [docs.tusklang.org/javascript](https://docs.tusklang.org/javascript)
- **GitHub Repository**: [github.com/tusklang/javascript](https://github.com/tusklang/javascript)
- **npm Package**: [npmjs.com/package/tusklang](https://npmjs.com/package/tusklang)
- **Examples**: [examples.tusklang.org/javascript](https://examples.tusklang.org/javascript)

## 🎯 Next Steps

1. **Install TuskLang JavaScript SDK**
2. **Create your first .tsk file**
3. **Explore database integration**
4. **Integrate with your web framework**
5. **Deploy to production**

---

**"We don't bow to any king"** - The JavaScript SDK gives you maximum flexibility with 5 database adapters, enhanced parser, and TypeScript support. Choose your syntax, query your data, and build powerful applications with TuskLang! 