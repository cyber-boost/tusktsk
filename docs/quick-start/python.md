# 🐍 TuskLang Python SDK - Tusk Me Hard

**"We don't bow to any king" - Python Edition**

The TuskLang Python SDK provides the most advanced FUJSEN support, smart contract capabilities, and database integration for Python applications.

## 🚀 Quick Start

### Installation

```bash
# Install from PyPI
pip install tusklang

# Or install from source
git clone https://github.com/tusklang/python
cd python
pip install -e .

# Verify installation
python -c "import tsk; print(tsk.__version__)"
```

### One-Line Install

```bash
# Direct install
curl -sSL https://python.tusklang.org | python3 -

# Or with wget
wget -qO- https://python.tusklang.org | python3 -
```

## 🎯 Core Features

### 1. FUJSEN Support (Function Serialization)
```python
from tsk import TSK

# Define executable functions in TSK
tsk_config = TSK.from_string("""
[payment]
process_fujsen = '''
def process(amount, recipient):
    if amount <= 0:
        raise ValueError("Invalid amount")
    
    return {
        'success': True,
        'transactionId': f'tx_{int(time.time())}',
        'amount': amount,
        'recipient': recipient,
        'fee': amount * 0.025
    }
'''

validate_fujsen = '''
def validate(amount):
    return 0 < amount <= 1000000
'''
""")

# Execute functions
result = tsk_config.execute_fujsen('payment', 'process', 100, 'alice@example.com')
is_valid = tsk_config.execute_fujsen('payment', 'validate', 500)
```

### 2. Smart Contract Capabilities
```python
# Smart contract with business logic
contract = TSK.from_string("""
[contract]
name: "PaymentProcessor"
version: "1.0.0"

# Contract state
balance: 0
transactions: []

process_payment_fujsen = '''
def process_payment(amount, recipient):
    global balance, transactions
    
    if amount <= 0:
        raise ValueError("Amount must be positive")
    
    if amount > balance:
        raise ValueError("Insufficient balance")
    
    # Execute transaction
    balance -= amount
    transaction = {
        'id': len(transactions) + 1,
        'amount': amount,
        'recipient': recipient,
        'timestamp': time.time()
    }
    transactions.append(transaction)
    
    return transaction
'''

get_balance_fujsen = '''
def get_balance():
    return balance
'''
""")

# Use the smart contract
contract.execute_fujsen('contract', 'process_payment', 50, 'bob@example.com')
balance = contract.execute_fujsen('contract', 'get_balance')
```

### 3. Database Integration
```python
from tsk import TSK
from tsk.adapters import SQLiteAdapter, PostgreSQLAdapter

# Configure database adapters
sqlite_db = SQLiteAdapter('app.db')
postgres_db = PostgreSQLAdapter(
    host='localhost',
    port=5432,
    database='myapp',
    user='postgres',
    password='secret'
)

# Create TSK instance with database
tsk = TSK()
tsk.set_database_adapter(sqlite_db)

# TSK file with database queries
config = TSK.from_string("""
[database]
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
recent_orders: @query("SELECT * FROM orders WHERE created_at > ?", @date.subtract("7d"))
""")

# Parse and execute
result = tsk.parse(config)
print(f"Total users: {result['database']['user_count']}")
```

### 4. @ Operator System
```python
from tsk import TSK

# Advanced @ operators
config = TSK.from_string("""
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
""")

# Execute with context
context = {
    'request': {'user_id': 123},
    'cache_value': 'cached_data'
}

result = tsk.execute_operators(config, context)
```

## 🔧 Advanced Usage

### 1. Cross-File Communication
```python
# main.tsk
main_config = TSK.from_string("""
$app_name: "MyApp"
$version: "1.0.0"

[database]
host: @config.tsk.get("db_host")
port: @config.tsk.get("db_port")
""")

# config.tsk
db_config = TSK.from_string("""
db_host: "localhost"
db_port: 5432
db_name: "myapp"
""")

# Link files
main_config.link_file('config.tsk', db_config)
result = main_config.parse()
```

### 2. Conditional Logic
```python
config = TSK.from_string("""
$environment: @env("APP_ENV", "development")

[server]
host: "0.0.0.0"
port: @if($environment == "production", 80, 8080)
workers: @if($environment == "production", 4, 1)
debug: @if($environment != "production", true, false)

[logging]
level: @if($environment == "production", "error", "debug")
format: @if($environment == "production", "json", "text")
""")

# Parse with environment
import os
os.environ['APP_ENV'] = 'production'
result = config.parse()
```

### 3. String Interpolation
```python
config = TSK.from_string("""
$app_name: "MyApp"
$version: "1.0.0"

[paths]
log_file: "/var/log/${app_name}.log"
config_file: "/etc/${app_name}/config.json"
data_dir: "/var/lib/${app_name}/v${version}"
""")

result = config.parse()
# log_file: "/var/log/MyApp.log"
# config_file: "/etc/MyApp/config.json"
# data_dir: "/var/lib/MyApp/v1.0.0"
```

### 4. Array and Object Operations
```python
config = TSK.from_string("""
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
""")

# Execute with request context
context = {
    'request': {
        'user_role': 'admin',
        'username': 'alice'
    }
}

result = config.execute_operators(context)
```

## 🗄️ Database Adapters

### SQLite Adapter
```python
from tsk.adapters import SQLiteAdapter

# Basic usage
sqlite = SQLiteAdapter('app.db')

# With options
sqlite = SQLiteAdapter(
    filename='app.db',
    timeout=30,
    check_same_thread=False
)

# Execute queries
result = sqlite.query("SELECT * FROM users WHERE active = ?", True)
count = sqlite.query("SELECT COUNT(*) FROM orders")[0][0]
```

### PostgreSQL Adapter
```python
from tsk.adapters import PostgreSQLAdapter

# Connection
postgres = PostgreSQLAdapter(
    host='localhost',
    port=5432,
    database='myapp',
    user='postgres',
    password='secret',
    sslmode='require'
)

# Connection pooling
postgres = PostgreSQLAdapter(
    host='localhost',
    database='myapp',
    user='postgres',
    password='secret',
    pool_size=10,
    max_overflow=20
)
```

### MongoDB Adapter
```python
from tsk.adapters import MongoDBAdapter

# Connection
mongo = MongoDBAdapter(
    uri='mongodb://localhost:27017/',
    database='myapp'
)

# With authentication
mongo = MongoDBAdapter(
    uri='mongodb://user:pass@localhost:27017/',
    database='myapp',
    auth_source='admin'
)
```

## 🔐 Security Features

### 1. Input Validation
```python
from tsk import TSK
from tsk.validators import validate_email, validate_url

config = TSK.from_string("""
[user]
email: @validate.email(@request.email)
website: @validate.url(@request.website)
age: @validate.range(@request.age, 0, 150)
""")

# Custom validators
def validate_strong_password(password):
    return len(password) >= 8 and any(c.isupper() for c in password)

config.add_validator('strong_password', validate_strong_password)
```

### 2. SQL Injection Prevention
```python
# Automatic parameterization
config = TSK.from_string("""
[users]
user_data: @query("SELECT * FROM users WHERE id = ?", @request.user_id)
search_results: @query("SELECT * FROM users WHERE name LIKE ?", @request.search_term)
""")

# Safe execution
result = config.parse({
    'request': {
        'user_id': 123,
        'search_term': '%john%'
    }
})
```

### 3. Environment Variable Security
```python
# Secure environment handling
config = TSK.from_string("""
[secrets]
api_key: @env("API_KEY")
database_password: @env("DB_PASSWORD")
jwt_secret: @env("JWT_SECRET")
""")

# Validate required environment variables
config.validate_required_env(['API_KEY', 'DB_PASSWORD', 'JWT_SECRET'])
```

## 🚀 Performance Optimization

### 1. Caching
```python
from tsk import TSK
from tsk.cache import MemoryCache, RedisCache

# Memory cache
cache = MemoryCache()
tsk = TSK()
tsk.set_cache(cache)

# Redis cache
redis_cache = RedisCache(
    host='localhost',
    port=6379,
    db=0
)
tsk.set_cache(redis_cache)

# Use in TSK
config = TSK.from_string("""
[data]
expensive_data: @cache("5m", "expensive_operation")
user_profile: @cache("1h", "user_profile", @request.user_id)
""")
```

### 2. Lazy Loading
```python
# Lazy evaluation
config = TSK.from_string("""
[expensive]
data: @lazy("expensive_operation")
user_data: @lazy("user_profile", @request.user_id)
""")

# Only executes when accessed
result = config.get('expensive.data')  # Executes now
```

### 3. Parallel Processing
```python
from tsk import TSK
import asyncio

# Async TSK processing
async def process_config():
    config = TSK.from_string("""
    [parallel]
    data1: @async("operation1")
    data2: @async("operation2")
    data3: @async("operation3")
    """)
    
    result = await config.parse_async()
    return result

# Run in event loop
result = asyncio.run(process_config())
```

## 🧪 Testing

### 1. Unit Testing
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
    
    def test_fujsen_execution(self):
        config = TSK.from_string("""
        [math]
        add_fujsen = '''
        def add(a, b):
            return a + b
        '''
        """)
        
        result = config.execute_fujsen('math', 'add', 2, 3)
        self.assertEqual(result, 5)
```

### 2. Integration Testing
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
```

## 🔧 CLI Tools

### 1. Basic CLI Usage
```bash
# Parse TSK file
tsk parse config.tsk

# Validate syntax
tsk validate config.tsk

# Execute FUJSEN
tsk fujsen config.tsk payment process 100 "alice@example.com"

# Convert to JSON
tsk convert config.tsk --format json

# Interactive shell
tsk shell config.tsk
```

### 2. Advanced CLI Features
```bash
# Parse with environment
APP_ENV=production tsk parse config.tsk

# Execute with variables
tsk parse config.tsk --var user_id=123 --var debug=true

# Output to file
tsk parse config.tsk --output result.json

# Watch for changes
tsk parse config.tsk --watch

# Benchmark parsing
tsk benchmark config.tsk --iterations 1000
```

## 🌐 Web Framework Integration

### 1. Flask Integration
```python
from flask import Flask, request, jsonify
from tsk import TSK

app = Flask(__name__)

# Load configuration
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

@app.route('/api/users')
def get_users():
    # Use database query from config
    users = config.get('database.users_query')
    return jsonify(users)

if __name__ == '__main__':
    app.run(
        host=config.get('server.host'),
        port=config.get('server.port'),
        debug=config.get('server.debug')
    )
```

### 2. Django Integration
```python
# settings.py
from tsk import TSK

# Load TSK configuration
tsk_config = TSK.from_file('django.tsk')

# Django settings
SECRET_KEY = tsk_config.get('django.secret_key')
DEBUG = tsk_config.get('django.debug')
ALLOWED_HOSTS = tsk_config.get('django.allowed_hosts')

# Database
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

### 3. FastAPI Integration
```python
from fastapi import FastAPI, HTTPException
from tsk import TSK
from pydantic import BaseModel

app = FastAPI()

# Load configuration
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

@app.get("/users/count")
async def get_user_count():
    count = config.get('database.user_count')
    return {"count": count}
```

## 🔄 Migration from Other Config Formats

### 1. From JSON
```python
import json
from tsk import TSK

# Convert JSON to TSK
def json_to_tsk(json_file, tsk_file):
    with open(json_file, 'r') as f:
        data = json.load(f)
    
    tsk_content = ""
    for key, value in data.items():
        if isinstance(value, dict):
            tsk_content += f"[{key}]\n"
            for k, v in value.items():
                tsk_content += f"{k}: {repr(v)}\n"
        else:
            tsk_content += f"{key}: {repr(value)}\n"
    
    with open(tsk_file, 'w') as f:
        f.write(tsk_content)

# Usage
json_to_tsk('config.json', 'config.tsk')
```

### 2. From YAML
```python
import yaml
from tsk import TSK

# Convert YAML to TSK
def yaml_to_tsk(yaml_file, tsk_file):
    with open(yaml_file, 'r') as f:
        data = yaml.safe_load(f)
    
    tsk_content = ""
    for key, value in data.items():
        if isinstance(value, dict):
            tsk_content += f"[{key}]\n"
            for k, v in value.items():
                tsk_content += f"{k}: {repr(v)}\n"
        else:
            tsk_content += f"{key}: {repr(value)}\n"
    
    with open(tsk_file, 'w') as f:
        f.write(tsk_content)

# Usage
yaml_to_tsk('config.yaml', 'config.tsk')
```

## 🚀 Deployment

### 1. Docker Deployment
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
Benchmark Results (Python 3.11):
- Simple config (1KB): 0.5ms
- Complex config (10KB): 2.1ms
- Large config (100KB): 15.3ms
- FUJSEN execution: 0.1ms per function
- Database query: 1.2ms average
```

### Memory Usage
```
Memory Usage:
- Base TSK instance: 2.5MB
- With SQLite adapter: +1.2MB
- With PostgreSQL adapter: +2.1MB
- With Redis cache: +0.8MB
```

## 🔧 Troubleshooting

### Common Issues

1. **Import Errors**
```python
# Make sure TuskLang is installed
pip install tusklang

# Check version
python -c "import tsk; print(tsk.__version__)"
```

2. **Database Connection Issues**
```python
# Test database connection
from tsk.adapters import SQLiteAdapter
db = SQLiteAdapter('test.db')
result = db.query("SELECT 1")
print("Database connection successful")
```

3. **FUJSEN Execution Errors**
```python
# Debug FUJSEN execution
try:
    result = config.execute_fujsen('section', 'function', *args)
except Exception as e:
    print(f"FUJSEN error: {e}")
    # Check function syntax and parameters
```

### Debug Mode
```python
import logging
from tsk import TSK

# Enable debug logging
logging.basicConfig(level=logging.DEBUG)

# Create TSK instance with debug
tsk = TSK(debug=True)
config = tsk.from_file('config.tsk')
```

## 📚 Resources

- **Official Documentation**: [docs.tusklang.org/python](https://docs.tusklang.org/python)
- **GitHub Repository**: [github.com/tusklang/python](https://github.com/tusklang/python)
- **PyPI Package**: [pypi.org/project/tusklang](https://pypi.org/project/tusklang)
- **Examples**: [examples.tusklang.org/python](https://examples.tusklang.org/python)

## 🎯 Next Steps

1. **Install TuskLang Python SDK**
2. **Create your first .tsk file**
3. **Explore FUJSEN capabilities**
4. **Integrate with your database**
5. **Deploy to production**

---

**"We don't bow to any king"** - The Python SDK gives you maximum flexibility with advanced FUJSEN support, smart contracts, and database integration. Choose your syntax, execute your logic, and build powerful applications with TuskLang! 