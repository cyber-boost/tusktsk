# 🐍 TuskLang for Python

**"We don't bow to any king" - Python Edition**

Welcome to the comprehensive TuskLang documentation for Python developers. This guide will transform how you think about configuration management by introducing you to the revolutionary concept of **configuration with a heartbeat**.

## 🎯 What is TuskLang?

TuskLang is a revolutionary configuration language that brings **executable configuration** to Python applications. Unlike traditional static configuration files, TuskLang allows you to:

- **Query databases directly in config** - Real-time data integration
- **Execute Python functions** - Dynamic logic with FUJSEN
- **Use powerful @ operators** - Environment variables, HTTP requests, file operations
- **Cross-file communication** - Link and reference other TSK files
- **Multiple syntax styles** - Choose INI, JSON, or XML-inspired syntax

## 🚀 Quick Start

### Installation

```bash
# One-line install
curl -sSL https://python.tuskt.sk | python3 -

# Or via pip
pip install tusklang

# Verify installation
python3 -c "import tsk; print('TuskLang ready!')"
```

### Your First TSK File

```python
from tsk import TSK

config = TSK.from_string("""
$app_name: "MyFirstTuskApp"
$version: "1.0.0"

[server]
host: "0.0.0.0"
port: 8080
debug: @env("DEBUG", "true")

[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", "5432")
name: @env("DB_NAME", "myapp")
user: @env("DB_USER", "postgres")
password: @env("DB_PASSWORD", "secret")

[api]
# Database query directly in config!
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")

# Executable function in config
process_user_fujsen = '''
def process_user(user_data):
    if user_data.get('age', 0) < 18:
        return {"status": "rejected", "reason": "underage"}
    return {"status": "approved", "user_id": user_data.get('id')}
'''
""")

result = config.parse()
print(f"App: {result['app_name']}")
print(f"Server: {result['server']['host']}:{result['server']['port']}")
print(f"Total users: {result['api']['user_count']}")
```

## 📚 Documentation Guide

### 🎯 Getting Started

1. **[Installation Guide](001-installation-python.md)** - Complete installation instructions for all platforms
   - One-line installation
   - PyPI and source installation
   - Virtual environment setup
   - Docker and cloud deployment
   - Troubleshooting common issues

2. **[Quick Start Guide](002-quick-start-python.md)** - Get up and running in 5 minutes
   - Lightning-fast setup
   - Your first TSK file
   - Core concepts in 60 seconds
   - Real application examples
   - Multiple syntax styles

### 🔧 Core Concepts

3. **[Basic Syntax](003-basic-syntax-python.md)** - Master TuskLang syntax
   - Multiple syntax styles (INI, JSON, XML)
   - Data types and structures
   - Global variables and sections
   - String interpolation
   - Comments and formatting

4. **[FUJSEN Functions](004-fujsen-python.md)** - Executable configuration
   - Function serialization concepts
   - Basic and advanced functions
   - Database integration
   - Security and validation
   - Smart contracts

5. **[Database Integration](005-database-integration-python.md)** - Query databases in config
   - Supported databases (SQLite, PostgreSQL, MySQL, MongoDB, Redis)
   - Basic and complex queries
   - Real-time data integration
   - Performance optimization
   - Security features

6. **[@ Operators](006-at-operators-python.md)** - Dynamic configuration power
   - Environment variables
   - Date and time operations
   - File operations
   - HTTP requests
   - Caching and ML operations
   - Custom operators

### 🚀 Advanced Features

7. **[Advanced Features](007-advanced-features-python.md)** - Enterprise-grade capabilities
   - Hierarchical configuration
   - Performance optimization
   - Security hardening
   - Testing and validation
   - Enterprise features

## 🎨 Multiple Syntax Styles

TuskLang supports three syntax styles. Choose what feels natural to you:

### Traditional INI Style
```python
config = TSK.from_string("""
[server]
host = 0.0.0.0
port = 8080
debug = true
""")
```

### JSON-Like Style
```python
config = TSK.from_string("""
{
    "server": {
        "host": "0.0.0.0",
        "port": 8080,
        "debug": true
    }
}
""")
```

### XML-Inspired Style
```python
config = TSK.from_string("""
<server>
    <host>0.0.0.0</host>
    <port>8080</port>
    <debug>true</debug>
</server>
""")
```

## ⚡ Revolutionary Features

### Database Queries in Config
```python
config = TSK.from_string("""
[analytics]
user_count: @query("SELECT COUNT(*) FROM users")
recent_orders: @query("""
    SELECT * FROM orders 
    WHERE created_at > ? 
    ORDER BY created_at DESC
    LIMIT 10
""", @date.subtract("7d"))
""")
```

### Executable Functions (FUJSEN)
```python
config = TSK.from_string("""
[payment]
process_payment_fujsen = '''
def process_payment(amount, recipient):
    if amount <= 0:
        raise ValueError("Amount must be positive")
    
    return {
        'success': True,
        'transactionId': f'tx_{int(time.time())}',
        'amount': amount,
        'recipient': recipient,
        'fee': amount * 0.025
    }
'''
""")

# Execute the function
result = config.execute_fujsen('payment', 'process_payment', 100, 'alice@example.com')
```

### Powerful @ Operators
```python
config = TSK.from_string("""
[environment]
api_key: @env("API_KEY", "default_key")
current_time: @date.now()
weather_data: @http("GET", "https://api.weatherapi.com/v1/current.json?key=YOUR_KEY&q=London")
config_content: @file.read("config.json")
cached_data: @cache("5m", @expensive_operation())
""")
```

## 🔗 Cross-File Communication

```python
# main.tsk
main_config = TSK.from_string("""
$app_name: "MyApp"

[database]
host: @config.tsk.get("db_host")
port: @config.tsk.get("db_port")
name: @config.tsk.get("db_name")
""")

# config.tsk
db_config = TSK.from_string("""
db_host: "localhost"
db_port: 5432
db_name: "myapp"
db_user: "postgres"
db_password: @env("DB_PASSWORD")
""")

# Link files
main_config.link_file('config.tsk', db_config)
result = main_config.parse()
```

## 🗄️ Database Adapters

TuskLang supports multiple database systems:

### SQLite
```python
from tsk.adapters import SQLiteAdapter
db = SQLiteAdapter('app.db')
```

### PostgreSQL
```python
from tsk.adapters import PostgreSQLAdapter
db = PostgreSQLAdapter(
    host='localhost',
    port=5432,
    database='myapp',
    user='postgres',
    password='secret'
)
```

### MySQL
```python
from tsk.adapters import MySQLAdapter
db = MySQLAdapter(
    host='localhost',
    port=3306,
    database='myapp',
    user='root',
    password='secret'
)
```

### MongoDB
```python
from tsk.adapters import MongoDBAdapter
db = MongoDBAdapter(
    uri='mongodb://localhost:27017/',
    database='myapp'
)
```

### Redis
```python
from tsk.adapters import RedisAdapter
db = RedisAdapter(
    host='localhost',
    port=6379,
    db=0
)
```

## 🌐 Web Framework Integration

### Flask Integration
```python
from flask import Flask, request, jsonify
from tsk import TSK

app = Flask(__name__)
config = TSK.from_file('app.tsk')

@app.route('/api/process', methods=['POST'])
def process_data():
    data = request.json
    
    # Execute FUJSEN function
    result = config.execute_fujsen(
        'api', 
        'process_data', 
        data['amount'], 
        data['recipient']
    )
    
    return jsonify(result)
```

### Django Integration
```python
# settings.py
from tsk import TSK

tsk_config = TSK.from_file('django.tsk')

SECRET_KEY = tsk_config.get('django.secret_key')
DEBUG = tsk_config.get('django.debug')
ALLOWED_HOSTS = tsk_config.get('django.allowed_hosts')

DATABASES = {
    'default': {
        'ENGINE': 'django.db.backends.postgresql',
        'NAME': tsk_config.get('database.name'),
        'USER': tsk_config.get('database.user'),
        'PASSWORD': tsk_config.get('database.password'),
        'HOST': tsk_config.get('database.host'),
        'PORT': tsk_config.get('database.port'),
    }
}
```

### FastAPI Integration
```python
from fastapi import FastAPI, HTTPException
from tsk import TSK
from pydantic import BaseModel

app = FastAPI()
config = TSK.from_file('api.tsk')

class PaymentRequest(BaseModel):
    amount: float
    recipient: str

@app.post("/payment/process")
async def process_payment(payment: PaymentRequest):
    try:
        result = config.execute_fujsen(
            'payment',
            'process',
            payment.amount,
            payment.recipient
        )
        return result
    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e))
```

## 🔧 CLI Tools

```bash
# Parse a TSK file
tsk parse config.tsk

# Validate syntax
tsk validate config.tsk

# Execute FUJSEN function
tsk fujsen config.tsk payment process 100 "alice@example.com"

# Convert to JSON
tsk convert config.tsk --format json

# Interactive shell
tsk shell config.tsk

# Parse with environment
APP_ENV=production tsk parse config.tsk

# Execute with variables
tsk parse config.tsk --var user_id=123 --var debug=true
```

## 🚀 Performance Features

### Caching
```python
config = TSK.from_string("""
[cache]
# Cache expensive operations
user_count: @cache("5m", @query("SELECT COUNT(*) FROM users"))
weather_data: @cache("1h", @http("GET", "https://api.weatherapi.com/v1/current.json?key=YOUR_KEY&q=London"))

# Cache with parameters
user_profile: @cache("10m", @query("SELECT * FROM users WHERE id = ?", @request.user_id))
""")
```

### Lazy Loading
```python
config = TSK.from_string("""
[expensive]
# Lazy evaluation - only executes when accessed
user_data: @lazy(@query("SELECT * FROM users WHERE id = ?", @request.user_id))
weather_data: @lazy(@http("GET", "https://api.weatherapi.com/v1/current.json?key=YOUR_KEY&q=London"))
""")
```

### Parallel Processing
```python
import asyncio

async def process_config():
    config = TSK.from_string("""
    [parallel]
    # These will execute in parallel
    user_count: @async(@query("SELECT COUNT(*) FROM users"))
    order_count: @async(@query("SELECT COUNT(*) FROM orders"))
    revenue_total: @async(@query("SELECT SUM(amount) FROM orders"))
    """)
    
    result = await config.parse_async()
    return result

result = asyncio.run(process_config())
```

## 🔒 Security Features

### Input Validation
```python
config = TSK.from_string("""
[validation]
# Validate email format
admin_email: @env.validate("ADMIN_EMAIL", "email")

# Validate URL format
api_url: @env.validate("API_URL", "url")

# Validate numeric range
port_number: @env.validate("PORT", "int", min=1, max=65535)
""")
```

### Encryption
```python
config = TSK.from_string("""
[security]
# Encrypt sensitive data
database_password: @encrypt(@env("DB_PASSWORD"), "AES-256-GCM")
api_key: @encrypt(@env("API_KEY"), "AES-256-GCM")
""")
```

### SQL Injection Prevention
```python
config = TSK.from_string("""
[users]
# Automatic parameterization
user_data: @query("SELECT * FROM users WHERE id = ?", @request.user_id)
search_results: @query("SELECT * FROM users WHERE name LIKE ?", @request.search_term)
""")
```

## 🧪 Testing

### Unit Testing
```python
import unittest
from tsk import TSK

class TestTSKConfig(unittest.TestCase):
    def setUp(self):
        self.config = TSK.from_string("""
        [test]
        value: 42
        string: "hello"
        boolean: true
        """)
    
    def test_basic_parsing(self):
        result = self.config.parse()
        self.assertEqual(result['test']['value'], 42)
        self.assertEqual(result['test']['string'], "hello")
        self.assertTrue(result['test']['boolean'])
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
```

## 📊 Performance Benchmarks

```
Benchmark Results (Python 3.11):
- Simple config (1KB): 0.5ms
- Complex config (10KB): 2.1ms
- Large config (100KB): 15.3ms
- FUJSEN execution: 0.1ms per function
- Database query: 1.2ms average
- HTTP request: 50ms average
- File read: 0.3ms average

Memory Usage:
- Base TSK instance: 2.5MB
- With SQLite adapter: +1.2MB
- With PostgreSQL adapter: +2.1MB
- With Redis cache: +0.8MB
```

## 🚀 Deployment

### Docker Deployment
```dockerfile
FROM python:3.11-slim

WORKDIR /app

# Install TuskLang
RUN pip install tusklang

# Copy application
COPY . .

# Copy TSK configuration
COPY config.tsk /app/

# Run application
CMD ["python", "app.py"]
```

### Kubernetes Deployment
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

## 📚 Additional Resources

- **Official Documentation**: [docs.tuskt.sk/python](https://docs.tuskt.sk/python)
- **GitHub Repository**: [github.com/cyber-boost/tusktsk](https://github.com/cyber-boost/tusktsk)
- **PyPI Package**: [pypi.org/project/tusklang](https://pypi.org/project/tusklang)
- **Examples**: [examples.tuskt.sk/python](https://examples.tuskt.sk/python)
- **Community Support**: [community.tuskt.sk](https://community.tuskt.sk)

## 🎯 Next Steps

1. **Start with Installation** - [001-installation-python.md](001-installation-python.md)
2. **Quick Start Guide** - [002-quick-start-python.md](002-quick-start-python.md)
3. **Learn Basic Syntax** - [003-basic-syntax-python.md](003-basic-syntax-python.md)
4. **Explore FUJSEN Functions** - [004-fujsen-python.md](004-fujsen-python.md)
5. **Integrate Databases** - [005-database-integration-python.md](005-database-integration-python.md)
6. **Master @ Operators** - [006-at-operators-python.md](006-at-operators-python.md)
7. **Advanced Features** - [007-advanced-features-python.md](007-advanced-features-python.md)

## 💡 Why TuskLang?

### Traditional Configuration Problems
- **Static and rigid** - No dynamic behavior
- **Environment-specific files** - Multiple config files to maintain
- **No validation** - Errors discovered at runtime
- **Limited integration** - Can't access external data
- **No logic** - Can't make decisions based on data

### TuskLang Solutions
- **Dynamic and intelligent** - Configuration that adapts
- **Environment-aware** - Single config works everywhere
- **Built-in validation** - Catch errors early
- **Real-time data** - Query databases, make HTTP requests
- **Executable logic** - FUJSEN functions for complex decisions

## 🚀 Ready to Revolutionize Your Configuration?

TuskLang transforms static configuration into dynamic, intelligent systems. Start your journey today and experience the power of **configuration with a heartbeat**.

---

**"We don't bow to any king"** - TuskLang gives you the power to write configuration that thinks, adapts, and responds. Choose your syntax, execute your logic, and build revolutionary applications! 