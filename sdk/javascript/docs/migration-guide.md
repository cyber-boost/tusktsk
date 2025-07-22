# Migration Guide: Python SDK to JavaScript SDK

**Complete guide for migrating from TuskLang Python SDK to JavaScript SDK**

## Table of Contents

- [Overview](#overview)
- [Key Differences](#key-differences)
- [Installation Migration](#installation-migration)
- [Configuration Migration](#configuration-migration)
- [CLI Commands Migration](#cli-commands-migration)
- [API Migration](#api-migration)
- [Database Integration Migration](#database-integration-migration)
- [Testing Migration](#testing-migration)
- [Deployment Migration](#deployment-migration)
- [Troubleshooting](#troubleshooting)

## Overview

This guide helps you migrate from the TuskLang Python SDK to the JavaScript SDK. The JavaScript SDK provides the same powerful features as the Python SDK with native JavaScript performance and Node.js ecosystem integration.

### Migration Benefits

- **Better Performance**: Native JavaScript execution with V8 engine
- **Rich Ecosystem**: Access to npm packages and Node.js tools
- **Web Integration**: Direct browser compatibility
- **Modern Development**: ES6+ features and modern JavaScript patterns
- **Microservices**: Lightweight deployment with Node.js

## Key Differences

| Feature | Python SDK | JavaScript SDK |
|---------|------------|----------------|
| **Installation** | `pip install tusklang` | `npm install tusklang` |
| **CLI Command** | `tusk` | `tsk` |
| **Import** | `import tusklang` | `const TuskLang = require('tusklang')` |
| **Async/Await** | `async def` | `async function` |
| **Package Manager** | pip/poetry | npm/yarn |
| **Runtime** | Python | Node.js |
| **File Extensions** | `.py` | `.js` |

## Installation Migration

### From Python to JavaScript

**Before (Python):**
```bash
# Install Python SDK
pip install tusklang

# Or with poetry
poetry add tusklang
```

**After (JavaScript):**
```bash
# Install JavaScript SDK
npm install tusklang

# Or with yarn
yarn add tusklang

# Install globally for CLI access
npm install -g tusklang
```

### Development Dependencies

**Before (Python):**
```python
# requirements.txt
tusklang==2.0.0
pytest==7.0.0
black==22.0.0
```

**After (JavaScript):**
```json
// package.json
{
  "dependencies": {
    "tusklang": "^2.0.0"
  },
  "devDependencies": {
    "jest": "^29.0.0",
    "eslint": "^8.0.0",
    "prettier": "^2.0.0"
  }
}
```

## Configuration Migration

### Basic Configuration

**Before (Python):**
```python
# config.py
import tusklang

tusk = tusklang.TuskLang()
config = tusk.parse_file('config.tsk')
```

**After (JavaScript):**
```javascript
// config.js
const TuskLang = require('tusklang');

const tusk = new TuskLang();
const config = tusk.parseFile('./config.tsk');
```

### Enhanced Configuration

**Before (Python):**
```python
# config.py
from tusklang import TuskLangEnhanced

tusk = TuskLangEnhanced()
tusk.set_database_adapter(pg_adapter)
config = await tusk.parse_file('config.tsk')
```

**After (JavaScript):**
```javascript
// config.js
const TuskLangEnhanced = require('./tsk-enhanced.js');

const tusk = new TuskLangEnhanced();
tusk.setDatabaseAdapter(pgAdapter);
const config = await tusk.parseFile('./config.tsk');
```

### Environment Variables

**Before (Python):**
```python
# config.py
import os

database_url = os.getenv('DATABASE_URL', 'default_url')
```

**After (JavaScript):**
```tsk
# config.tsk
database_url: @env("DATABASE_URL", "default_url")
```

## CLI Commands Migration

### Basic Commands

| Python Command | JavaScript Command | Notes |
|----------------|-------------------|-------|
| `tusk --help` | `tsk --help` | Same help information |
| `tusk --version` | `tsk --version` | Same version display |
| `tusk parse config.tsk` | `tsk parse config.tsk` | Same parsing functionality |
| `tusk validate config.tsk` | `tsk validate config.tsk` | Same validation |

### Database Commands

**Before (Python):**
```bash
# Check database status
tusk db status

# Run migration
tusk db migrate schema.sql

# Backup database
tusk db backup backup.sql
```

**After (JavaScript):**
```bash
# Check database status
tsk db status

# Run migration
tsk db migrate schema.sql

# Backup database
tsk db backup backup.sql
```

### Binary Commands

**Before (Python):**
```bash
# Compile to binary
tusk binary compile config.tsk

# Execute binary
tusk binary execute config.pnt

# Benchmark
tusk binary benchmark config.tsk
```

**After (JavaScript):**
```bash
# Compile to binary
tsk binary compile config.tsk

# Execute binary
tsk binary execute config.pnt

# Benchmark
tsk binary benchmark config.tsk
```

## API Migration

### Basic Parsing

**Before (Python):**
```python
import tusklang

# Create parser
tusk = tusklang.TuskLang()

# Parse string
config = tusk.parse("""
name: "MyApp"
version: "1.0.0"
""")

# Parse file
config = tusk.parse_file('config.tsk')

# Access values
print(config['name'])
print(config['version'])
```

**After (JavaScript):**
```javascript
const TuskLang = require('tusklang');

// Create parser
const tusk = new TuskLang();

// Parse string
const config = tusk.parse(`
name: "MyApp"
version: "1.0.0"
`);

// Parse file
const config = tusk.parseFile('./config.tsk');

// Access values
console.log(config.name);
console.log(config.version);
```

### Enhanced Features

**Before (Python):**
```python
from tusklang import TuskLangEnhanced

tusk = TuskLangEnhanced()

# Set variables
tusk.set_variable('env', 'production')
tusk.set_variable('port', 8080)

# Parse with variables
config = tusk.parse("""
debug: $env == "development" ? true : false
server_port: $port
""")
```

**After (JavaScript):**
```javascript
const TuskLangEnhanced = require('./tsk-enhanced.js');

const tusk = new TuskLangEnhanced();

// Set variables
tusk.setVariable('env', 'production');
tusk.setVariable('port', 8080);

// Parse with variables
const config = tusk.parse(`
debug: $env == "development" ? true : false
server_port: $port
`);
```

### Database Integration

**Before (Python):**
```python
import asyncpg
from tusklang import TuskLangEnhanced

# Create database adapter
async def pg_adapter(sql, params=None):
    conn = await asyncpg.connect(
        host='localhost',
        port=5432,
        database='myapp',
        user='postgres',
        password='password'
    )
    try:
        result = await conn.fetch(sql, *params or [])
        return [dict(row) for row in result]
    finally:
        await conn.close()

# Use with parser
tusk = TuskLangEnhanced()
tusk.set_database_adapter(pg_adapter)

config = await tusk.parse("""
[database]
user_count: @query("SELECT COUNT(*) FROM users")
""")
```

**After (JavaScript):**
```javascript
const { Pool } = require('pg');
const TuskLangEnhanced = require('./tsk-enhanced.js');

// Create database adapter
const pool = new Pool({
  host: 'localhost',
  port: 5432,
  database: 'myapp',
  user: 'postgres',
  password: 'password'
});

const pgAdapter = {
  async query(sql, params = []) {
    const client = await pool.connect();
    try {
      const result = await client.query(sql, params);
      return result.rows;
    } finally {
      client.release();
    }
  }
};

// Use with parser
const tusk = new TuskLangEnhanced();
tusk.setDatabaseAdapter(pgAdapter);

const config = await tusk.parse(`
[database]
user_count: @query("SELECT COUNT(*) FROM users")
`);
```

## Database Integration Migration

### PostgreSQL

**Before (Python):**
```python
import asyncpg

async def setup_database():
    conn = await asyncpg.connect(
        host='localhost',
        port=5432,
        database='myapp',
        user='postgres',
        password='password'
    )
    return conn

async def query_database(conn, sql, params=None):
    result = await conn.fetch(sql, *params or [])
    return [dict(row) for row in result]
```

**After (JavaScript):**
```javascript
const { Pool } = require('pg');

const pool = new Pool({
  host: 'localhost',
  port: 5432,
  database: 'myapp',
  user: 'postgres',
  password: 'password'
});

async function queryDatabase(sql, params = []) {
  const client = await pool.connect();
  try {
    const result = await client.query(sql, params);
    return result.rows;
  } finally {
    client.release();
  }
}
```

### MySQL

**Before (Python):**
```python
import aiomysql

async def setup_mysql():
    conn = await aiomysql.connect(
        host='localhost',
        port=3306,
        database='myapp',
        user='root',
        password='password'
    )
    return conn

async def query_mysql(conn, sql, params=None):
    async with conn.cursor() as cursor:
        await cursor.execute(sql, params or [])
        result = await cursor.fetchall()
        return result
```

**After (JavaScript):**
```javascript
const mysql = require('mysql2/promise');

const connection = await mysql.createConnection({
  host: 'localhost',
  port: 3306,
  database: 'myapp',
  user: 'root',
  password: 'password'
});

async function queryMySQL(sql, params = []) {
  const [rows] = await connection.execute(sql, params);
  return rows;
}
```

## Testing Migration

### Unit Tests

**Before (Python):**
```python
# test_config.py
import pytest
import tusklang

def test_basic_parsing():
    tusk = tusklang.TuskLang()
    config = tusk.parse("""
    name: "TestApp"
    version: "1.0.0"
    """)
    
    assert config['name'] == 'TestApp'
    assert config['version'] == '1.0.0'

def test_database_integration():
    tusk = tusklang.TuskLangEnhanced()
    # Test database integration
    pass
```

**After (JavaScript):**
```javascript
// test-config.js
const TuskLang = require('tusklang');

describe('Configuration Parsing', () => {
  test('should parse basic configuration', () => {
    const tusk = new TuskLang();
    const config = tusk.parse(`
      name: "TestApp"
      version: "1.0.0"
    `);
    
    expect(config.name).toBe('TestApp');
    expect(config.version).toBe('1.0.0');
  });

  test('should handle database integration', () => {
    const tusk = new TuskLangEnhanced();
    // Test database integration
  });
});
```

### Integration Tests

**Before (Python):**
```python
# test_integration.py
import pytest
import asyncio
from tusklang import TuskLangEnhanced

@pytest.mark.asyncio
async def test_database_queries():
    tusk = TuskLangEnhanced()
    # Setup database adapter
    # Test queries
    pass
```

**After (JavaScript):**
```javascript
// test-integration.js
const TuskLangEnhanced = require('./tsk-enhanced.js');

describe('Database Integration', () => {
  test('should execute database queries', async () => {
    const tusk = new TuskLangEnhanced();
    // Setup database adapter
    // Test queries
  });
});
```

## Deployment Migration

### Docker

**Before (Python):**
```dockerfile
# Dockerfile
FROM python:3.11-slim

WORKDIR /app

COPY requirements.txt .
RUN pip install -r requirements.txt

COPY . .

CMD ["python", "app.py"]
```

**After (JavaScript):**
```dockerfile
# Dockerfile
FROM node:18-alpine

WORKDIR /app

COPY package*.json ./
RUN npm ci --only=production

COPY . .

CMD ["node", "app.js"]
```

### Docker Compose

**Before (Python):**
```yaml
# docker-compose.yml
version: '3.8'
services:
  app:
    build: .
    ports:
      - "8080:8080"
    environment:
      - DATABASE_URL=postgresql://postgres:password@db:5432/myapp
    depends_on:
      - db
  
  db:
    image: postgres:15
    environment:
      - POSTGRES_DB=myapp
      - POSTGRES_PASSWORD=password
```

**After (JavaScript):**
```yaml
# docker-compose.yml
version: '3.8'
services:
  app:
    build: .
    ports:
      - "8080:8080"
    environment:
      - DATABASE_URL=postgresql://postgres:password@db:5432/myapp
    depends_on:
      - db
  
  db:
    image: postgres:15
    environment:
      - POSTGRES_DB=myapp
      - POSTGRES_PASSWORD=password
```

### Kubernetes

**Before (Python):**
```yaml
# deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tusklang-app
spec:
  replicas: 3
  selector:
    matchLabels:
      app: tusklang
  template:
    metadata:
      labels:
        app: tusklang
    spec:
      containers:
      - name: app
        image: tusklang-app:latest
        ports:
        - containerPort: 8080
        env:
        - name: DATABASE_URL
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: url
```

**After (JavaScript):**
```yaml
# deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tusklang-app
spec:
  replicas: 3
  selector:
    matchLabels:
      app: tusklang
  template:
    metadata:
      labels:
        app: tusklang
    spec:
      containers:
      - name: app
        image: tusklang-app:latest
        ports:
        - containerPort: 8080
        env:
        - name: DATABASE_URL
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: url
```

## Complete Migration Example

### Python Application

**Before (Python):**
```python
# app.py
import asyncio
import asyncpg
from tusklang import TuskLangEnhanced

async def main():
    # Setup database
    conn = await asyncpg.connect(
        host='localhost',
        port=5432,
        database='myapp',
        user='postgres',
        password='password'
    )
    
    # Create database adapter
    async def db_adapter(sql, params=None):
        result = await conn.fetch(sql, *params or [])
        return [dict(row) for row in result]
    
    # Setup parser
    tusk = TuskLangEnhanced()
    tusk.set_database_adapter(db_adapter)
    
    # Parse configuration
    config = await tusk.parse_file('config.tsk')
    
    print(f"App: {config['name']}")
    print(f"Version: {config['version']}")
    print(f"Database users: {config['database']['user_count']}")
    
    await conn.close()

if __name__ == '__main__':
    asyncio.run(main())
```

**After (JavaScript):**
```javascript
// app.js
const { Pool } = require('pg');
const TuskLangEnhanced = require('./tsk-enhanced.js');

async function main() {
  // Setup database
  const pool = new Pool({
    host: 'localhost',
    port: 5432,
    database: 'myapp',
    user: 'postgres',
    password: 'password'
  });
  
  // Create database adapter
  const dbAdapter = {
    async query(sql, params = []) {
      const client = await pool.connect();
      try {
        const result = await client.query(sql, params);
        return result.rows;
      } finally {
        client.release();
      }
    }
  };
  
  // Setup parser
  const tusk = new TuskLangEnhanced();
  tusk.setDatabaseAdapter(dbAdapter);
  
  // Parse configuration
  const config = await tusk.parseFile('./config.tsk');
  
  console.log(`App: ${config.name}`);
  console.log(`Version: ${config.version}`);
  console.log(`Database users: ${config.database.user_count}`);
  
  await pool.end();
}

main().catch(console.error);
```

### Configuration File

**Before (Python):**
```tsk
# config.tsk
name: "MyApp"
version: "1.0.0"

[database]
host: "localhost"
port: 5432
database: "myapp"
user_count: @query("SELECT COUNT(*) FROM users")
```

**After (JavaScript):**
```tsk
# config.tsk
name: "MyApp"
version: "1.0.0"

[database]
host: "localhost"
port: 5432
database: "myapp"
user_count: @query("SELECT COUNT(*) FROM users")
```

*Note: The configuration file format remains the same!*

## Troubleshooting

### Common Issues

#### 1. Import Errors

**Problem:** `Cannot find module 'tusklang'`

**Solution:**
```bash
# Install the package
npm install tusklang

# Check if it's installed
npm list tusklang
```

#### 2. Async/Await Issues

**Problem:** `SyntaxError: await is only valid in async functions`

**Solution:**
```javascript
// Wrap in async function
async function main() {
  const config = await tusk.parseFile('./config.tsk');
}

main().catch(console.error);
```

#### 3. Database Connection Issues

**Problem:** Database adapter not working

**Solution:**
```javascript
// Ensure proper async/await usage
const dbAdapter = {
  async query(sql, params = []) {
    const client = await pool.connect();
    try {
      const result = await client.query(sql, params);
      return result.rows;
    } finally {
      client.release();
    }
  }
};
```

#### 4. CLI Command Issues

**Problem:** `tsk: command not found`

**Solution:**
```bash
# Install globally
npm install -g tusklang

# Or use npx
npx tusklang --help
```

### Performance Optimization

#### 1. Use Binary Compilation

```bash
# Compile to binary for production
tsk binary compile config.tsk

# Use binary in production
tsk binary execute config.pnt
```

#### 2. Implement Caching

```javascript
const cache = new Map();

function getConfig(filePath) {
  if (cache.has(filePath)) {
    return cache.get(filePath);
  }
  
  const config = tusk.parseFile(filePath);
  cache.set(filePath, config);
  return config;
}
```

#### 3. Connection Pooling

```javascript
const pool = new Pool({
  max: 20,
  min: 2,
  idleTimeoutMillis: 30000,
  connectionTimeoutMillis: 2000
});
```

### Migration Checklist

- [ ] Install Node.js and npm
- [ ] Install TuskLang JavaScript SDK
- [ ] Update package.json with dependencies
- [ ] Convert Python imports to JavaScript requires
- [ ] Update async/await syntax
- [ ] Migrate database adapters
- [ ] Update test files
- [ ] Update deployment configurations
- [ ] Test all functionality
- [ ] Update documentation
- [ ] Deploy to production

### Support

If you encounter issues during migration:

1. **Check the documentation**: [TuskLang JavaScript SDK Docs](https://tuskt.sk/docs)
2. **Review examples**: See the examples directory
3. **Run tests**: Ensure all tests pass
4. **Check compatibility**: Verify Node.js version requirements
5. **Community support**: Join the TuskLang community

---

**Happy migrating! The JavaScript SDK provides the same powerful features with better performance and modern development experience.** 