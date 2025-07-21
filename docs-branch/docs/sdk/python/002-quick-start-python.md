# 🚀 TuskLang Quick Start for Python

**"We don't bow to any king" - Get Started in 5 Minutes**

Welcome to TuskLang for Python! This guide will have you writing revolutionary configuration with a heartbeat in under 5 minutes.

## ⚡ Lightning Fast Start

### 1. Install TuskLang

```bash
# One-line install
curl -sSL https://python.tusklang.org | python3 -

# Verify installation
python3 -c "import tsk; print('TuskLang ready!')"
```

### 2. Create Your First TSK File

```python
# Create app.tsk
from tsk import TSK

# Your first TuskLang configuration
config_content = """
$app_name: "MyFirstTuskApp"
$version: "1.0.0"

[server]
host: "0.0.0.0"
port: 8080
debug: true

[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: @env("DB_PASSWORD", "secret")

[api]
endpoint: "https://api.example.com"
timeout: 30
retries: 3
"""

# Parse the configuration
config = TSK.from_string(config_content)
result = config.parse()

print(f"App: {result['app_name']}")
print(f"Server: {result['server']['host']}:{result['server']['port']}")
print(f"Database: {result['database']['name']}")
```

### 3. Run Your First TuskLang Code

```bash
# Save the above code as quick_start.py
python quick_start.py
```

**Output:**
```
App: MyFirstTuskApp
Server: 0.0.0.0:8080
Database: myapp
```

## 🎯 Core Concepts in 60 Seconds

### What Makes TuskLang Revolutionary?

1. **Database Queries in Config** - Query your database directly in configuration
2. **Executable Functions** - Write Python functions in your config files
3. **@ Operator System** - Powerful operators for dynamic configuration
4. **Cross-File Communication** - Link and reference other TSK files
5. **Environment Integration** - Seamless environment variable handling

## 🔥 Your First Real Application

### Step 1: Create a Web Application Config

```python
# web_app.py
from tsk import TSK
from flask import Flask, jsonify

# TuskLang configuration with database queries
config_content = """
$app_name: "TuskWebApp"
$version: "1.0.0"

[server]
host: "0.0.0.0"
port: 5000
debug: @env("DEBUG", "true")

[database]
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", "5432")
name: @env("DB_NAME", "tuskapp")
user: @env("DB_USER", "postgres")
password: @env("DB_PASSWORD", "secret")

[api]
# Database query directly in config!
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")

# Executable function in config
process_user_fujsen = '''
def process_user(user_data):
    if user_data.get('age', 0) < 18:
        return {"status": "rejected", "reason": "underage"}
    return {"status": "approved", "user_id": user_data.get('id')}
'''
"""

# Parse configuration
config = TSK.from_string(config_content)
app_config = config.parse()

# Create Flask app
app = Flask(__name__)

@app.route('/')
def home():
    return jsonify({
        "app": app_config['app_name'],
        "version": app_config['version'],
        "status": "running"
    })

@app.route('/stats')
def stats():
    # Use database queries from config
    user_count = config.get('api.user_count')
    active_users = config.get('api.active_users')
    
    return jsonify({
        "total_users": user_count,
        "active_users": active_users
    })

@app.route('/process', methods=['POST'])
def process_user():
    from flask import request
    
    # Execute FUJSEN function from config
    result = config.execute_fujsen(
        'api', 
        'process_user', 
        request.json
    )
    
    return jsonify(result)

if __name__ == '__main__':
    app.run(
        host=app_config['server']['host'],
        port=app_config['server']['port'],
        debug=app_config['server']['debug']
    )
```

### Step 2: Set Up Environment Variables

```bash
# Set environment variables
export DEBUG=true
export DB_HOST=localhost
export DB_PORT=5432
export DB_NAME=tuskapp
export DB_USER=postgres
export DB_PASSWORD=your_password
```

### Step 3: Run Your Application

```bash
# Install Flask if needed
pip install flask

# Run the application
python web_app.py
```

**Visit:**
- `http://localhost:5000/` - App info
- `http://localhost:5000/stats` - Database stats
- `http://localhost:5000/process` - Process users (POST)

## 🎨 Multiple Syntax Styles

TuskLang supports multiple syntax styles. Choose what feels right for you:

### Traditional INI Style
```python
config_content = """
[server]
host = 0.0.0.0
port = 8080
debug = true
"""
```

### JSON-Like Style
```python
config_content = """
{
    "server": {
        "host": "0.0.0.0",
        "port": 8080,
        "debug": true
    }
}
"""
```

### XML-Inspired Style
```python
config_content = """
<server>
    <host>0.0.0.0</host>
    <port>8080</port>
    <debug>true</debug>
</server>
"""
```

## 🔗 Cross-File Communication

### Main Configuration
```python
# main.tsk
main_config = TSK.from_string("""
$app_name: "CrossFileApp"

[database]
host: @config.tsk.get("db_host")
port: @config.tsk.get("db_port")
name: @config.tsk.get("db_name")
""")
```

### Database Configuration
```python
# config.tsk
db_config = TSK.from_string("""
db_host: "localhost"
db_port: 5432
db_name: "myapp"
db_user: "postgres"
db_password: @env("DB_PASSWORD")
""")
```

### Link Files Together
```python
# Link the files
main_config.link_file('config.tsk', db_config)
result = main_config.parse()

print(f"Database: {result['database']['host']}:{result['database']['port']}")
```

## ⚡ @ Operator Examples

### Environment Variables
```python
config = TSK.from_string("""
[api]
endpoint: @env("API_ENDPOINT", "https://api.example.com")
api_key: @env("API_KEY")
debug: @env("DEBUG", "false")
""")
```

### Date and Time
```python
config = TSK.from_string("""
[timestamps]
current_time: @date.now()
formatted_date: @date("Y-m-d H:i:s")
yesterday: @date.subtract("1d")
next_week: @date.add("7d")
""")
```

### File Operations
```python
config = TSK.from_string("""
[files]
config_json: @file.read("config.json")
log_content: @file.read("app.log")
file_exists: @file.exists("important.txt")
""")
```

### HTTP Requests
```python
config = TSK.from_string("""
[external]
weather_data: @http("GET", "https://api.weatherapi.com/v1/current.json?key=YOUR_KEY&q=London")
user_info: @http("POST", "https://api.example.com/users", {"id": 123})
""")
```

## 🗄️ Database Integration

### SQLite Example
```python
from tsk import TSK
from tsk.adapters import SQLiteAdapter

# Set up database adapter
db = SQLiteAdapter('app.db')
tsk = TSK()
tsk.set_database_adapter(db)

# Create test table
db.execute("""
CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY,
    name TEXT,
    email TEXT,
    active BOOLEAN
)
""")

db.execute("""
INSERT OR REPLACE INTO users VALUES 
(1, 'Alice', 'alice@example.com', 1),
(2, 'Bob', 'bob@example.com', 0)
""")

# TSK with database queries
config = TSK.from_string("""
[users]
total_count: @query("SELECT COUNT(*) FROM users")
active_count: @query("SELECT COUNT(*) FROM users WHERE active = 1")
user_list: @query("SELECT * FROM users WHERE active = 1")
""")

result = tsk.parse(config)
print(f"Total users: {result['users']['total_count']}")
print(f"Active users: {result['users']['active_count']}")
```

## 🔧 CLI Quick Commands

```bash
# Parse a TSK file
tsk parse config.tsk

# Validate syntax
tsk validate config.tsk

# Execute FUJSEN function
tsk fujsen config.tsk api process_user '{"name": "John", "age": 25}'

# Convert to JSON
tsk convert config.tsk --format json

# Interactive shell
tsk shell config.tsk
```

## 🚀 Advanced Quick Start

### Smart Contract Example
```python
# smart_contract.py
from tsk import TSK

contract = TSK.from_string("""
[contract]
name: "PaymentProcessor"
version: "1.0.0"

# Contract state
balance: 1000
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

get_transactions_fujsen = '''
def get_transactions():
    return transactions
'''
""")

# Use the smart contract
try:
    # Process a payment
    transaction = contract.execute_fujsen('contract', 'process_payment', 50, 'alice@example.com')
    print(f"Payment processed: {transaction}")
    
    # Check balance
    balance = contract.execute_fujsen('contract', 'get_balance')
    print(f"Current balance: {balance}")
    
    # Get transaction history
    transactions = contract.execute_fujsen('contract', 'get_transactions')
    print(f"Transaction count: {len(transactions)}")
    
except ValueError as e:
    print(f"Error: {e}")
```

## 🎯 What You've Learned

In this quick start, you've discovered:

1. **Basic TSK syntax** - Multiple styles to choose from
2. **@ Operator system** - Dynamic configuration with environment variables, dates, files, and HTTP
3. **Database integration** - Query databases directly in configuration
4. **FUJSEN functions** - Write executable Python code in your config
5. **Cross-file communication** - Link and reference other TSK files
6. **Web framework integration** - Use TuskLang with Flask, Django, FastAPI
7. **Smart contracts** - Business logic embedded in configuration

## 🚀 Next Steps

Ready to dive deeper? Explore these guides:

1. **Basic Syntax** - [003-basic-syntax-python.md](003-basic-syntax-python.md)
2. **FUJSEN Functions** - [004-fujsen-python.md](004-fujsen-python.md)
3. **Database Integration** - [005-database-integration-python.md](005-database-integration-python.md)
4. **@ Operators** - [006-at-operators-python.md](006-at-operators-python.md)
5. **Advanced Features** - [007-advanced-features-python.md](007-advanced-features-python.md)

## 💡 Pro Tips

- **Start simple** - Begin with basic key-value pairs
- **Use @ operators** - Leverage environment variables and dynamic content
- **Try FUJSEN** - Add executable logic to your configuration
- **Link files** - Break complex configs into manageable pieces
- **Query databases** - Integrate real-time data into your configuration

---

**"We don't bow to any king"** - You now have the power to write configuration with a heartbeat. Go forth and build revolutionary applications with TuskLang! 