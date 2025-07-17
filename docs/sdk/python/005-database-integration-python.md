# 🗄️ Database Integration for Python

**"We don't bow to any king" - Query Your Database in Config**

TuskLang's revolutionary database integration allows you to write database queries directly in your configuration files. This brings real-time data into your configuration, making it truly dynamic and responsive.

## 🎯 Why Database Integration is Revolutionary

1. **Real-time Data in Config** - Configuration that adapts to live data
2. **Dynamic Configuration** - Values that change based on database state
3. **Cross-Environment Consistency** - Same config works across environments
4. **Performance Optimization** - Cache expensive queries automatically
5. **Type Safety** - Automatic type conversion and validation

## 🚀 Quick Start

### Basic Database Setup

```python
from tsk import TSK
from tsk.adapters import SQLiteAdapter

# Set up database adapter
db = SQLiteAdapter('app.db')
tsk = TSK()
tsk.set_database_adapter(db)

# Create test data
db.execute("""
CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY,
    name TEXT,
    email TEXT,
    active BOOLEAN,
    created_at TIMESTAMP
)
""")

db.execute("""
INSERT OR REPLACE INTO users VALUES 
(1, 'Alice', 'alice@example.com', 1, '2024-01-01 10:00:00'),
(2, 'Bob', 'bob@example.com', 1, '2024-01-02 11:00:00'),
(3, 'Charlie', 'charlie@example.com', 0, '2024-01-03 12:00:00')
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
print(f"Active user list: {result['users']['user_list']}")
```

## 🔌 Database Adapters

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

# Execute statements
sqlite.execute("INSERT INTO users (name, email) VALUES (?, ?)", "Alice", "alice@example.com")
sqlite.execute("UPDATE users SET active = ? WHERE id = ?", False, 1)
```

### PostgreSQL Adapter

```python
from tsk.adapters import PostgreSQLAdapter

# Basic connection
postgres = PostgreSQLAdapter(
    host='localhost',
    port=5432,
    database='myapp',
    user='postgres',
    password='secret'
)

# With SSL
postgres = PostgreSQLAdapter(
    host='localhost',
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

# Execute queries
result = postgres.query("SELECT * FROM users WHERE active = %s", True)
count = postgres.query("SELECT COUNT(*) FROM orders")[0][0]
```

### MySQL Adapter

```python
from tsk.adapters import MySQLAdapter

# Basic connection
mysql = MySQLAdapter(
    host='localhost',
    port=3306,
    database='myapp',
    user='root',
    password='secret'
)

# With SSL
mysql = MySQLAdapter(
    host='localhost',
    database='myapp',
    user='root',
    password='secret',
    ssl_ca='/path/to/ca.pem',
    ssl_cert='/path/to/cert.pem',
    ssl_key='/path/to/key.pem'
)

# Connection pooling
mysql = MySQLAdapter(
    host='localhost',
    database='myapp',
    user='root',
    password='secret',
    pool_size=10,
    pool_recycle=3600
)

# Execute queries
result = mysql.query("SELECT * FROM users WHERE active = %s", True)
count = mysql.query("SELECT COUNT(*) FROM orders")[0][0]
```

### MongoDB Adapter

```python
from tsk.adapters import MongoDBAdapter

# Basic connection
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

# With SSL
mongo = MongoDBAdapter(
    uri='mongodb://localhost:27017/',
    database='myapp',
    ssl=True,
    ssl_cert_reqs='CERT_REQUIRED'
)

# Execute queries
result = mongo.query("users", {"active": True})
count = mongo.query("users", {}).count()
```

### Redis Adapter

```python
from tsk.adapters import RedisAdapter

# Basic connection
redis = RedisAdapter(
    host='localhost',
    port=6379,
    db=0
)

# With authentication
redis = RedisAdapter(
    host='localhost',
    port=6379,
    password='secret',
    db=0
)

# With SSL
redis = RedisAdapter(
    host='localhost',
    port=6380,
    ssl=True,
    ssl_cert_reqs='CERT_REQUIRED'
)

# Execute queries
result = redis.query("GET", "user:123")
redis.execute("SET", "user:123", "alice@example.com")
```

## 📊 Advanced Query Features

### Parameterized Queries

```python
config = TSK.from_string("""
[users]
user_by_id: @query("SELECT * FROM users WHERE id = ?", @request.user_id)
users_by_status: @query("SELECT * FROM users WHERE active = ?", @request.active)
search_users: @query("SELECT * FROM users WHERE name LIKE ?", @request.search_term)
""")

# Execute with context
context = {
    'request': {
        'user_id': 1,
        'active': True,
        'search_term': '%alice%'
    }
}

result = tsk.parse(config, context)
```

### Complex Queries with Joins

```python
# Set up test data
db.execute("""
CREATE TABLE IF NOT EXISTS orders (
    id INTEGER PRIMARY KEY,
    user_id INTEGER,
    amount DECIMAL(10,2),
    status TEXT,
    created_at TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users (id)
)
""")

db.execute("""
INSERT OR REPLACE INTO orders VALUES 
(1, 1, 100.50, 'completed', '2024-01-01 10:00:00'),
(2, 1, 250.75, 'pending', '2024-01-02 11:00:00'),
(3, 2, 75.25, 'completed', '2024-01-03 12:00:00')
""")

config = TSK.from_string("""
[analytics]
user_orders: @query("""
    SELECT u.name, u.email, COUNT(o.id) as order_count, SUM(o.amount) as total_amount
    FROM users u
    LEFT JOIN orders o ON u.id = o.user_id
    WHERE u.active = 1
    GROUP BY u.id, u.name, u.email
    ORDER BY total_amount DESC
""")

recent_orders: @query("""
    SELECT o.id, o.amount, o.status, u.name as user_name
    FROM orders o
    JOIN users u ON o.user_id = u.id
    WHERE o.created_at > ?
    ORDER BY o.created_at DESC
""", @date.subtract("7d"))
""")

result = tsk.parse(config)
print(f"User orders: {result['analytics']['user_orders']}")
print(f"Recent orders: {result['analytics']['recent_orders']}")
```

### Aggregation Queries

```python
config = TSK.from_string("""
[statistics]
total_revenue: @query("SELECT SUM(amount) FROM orders WHERE status = 'completed'")
average_order: @query("SELECT AVG(amount) FROM orders WHERE status = 'completed'")
order_count: @query("SELECT COUNT(*) FROM orders WHERE status = 'completed'")
max_order: @query("SELECT MAX(amount) FROM orders WHERE status = 'completed'")
min_order: @query("SELECT MIN(amount) FROM orders WHERE status = 'completed'")

revenue_by_status: @query("""
    SELECT status, COUNT(*) as count, SUM(amount) as total
    FROM orders
    GROUP BY status
    ORDER BY total DESC
""")

daily_revenue: @query("""
    SELECT DATE(created_at) as date, SUM(amount) as revenue
    FROM orders
    WHERE status = 'completed'
    GROUP BY DATE(created_at)
    ORDER BY date DESC
    LIMIT 30
""")
""")

result = tsk.parse(config)
print(f"Total revenue: ${result['statistics']['total_revenue']}")
print(f"Average order: ${result['statistics']['average_order']}")
print(f"Revenue by status: {result['statistics']['revenue_by_status']}")
```

## 🔄 Real-time Data Integration

### Dynamic Configuration Based on Database State

```python
config = TSK.from_string("""
$environment: @env("APP_ENV", "development")

[server]
host: "0.0.0.0"
port: @if($environment == "production", 80, 8080)
workers: @query("SELECT value FROM settings WHERE key = 'server_workers'")
max_connections: @query("SELECT value FROM settings WHERE key = 'max_connections'")

[database]
host: @query("SELECT value FROM settings WHERE key = 'db_host'")
port: @query("SELECT value FROM settings WHERE key = 'db_port'")
name: @query("SELECT value FROM settings WHERE key = 'db_name'")

[features]
maintenance_mode: @query("SELECT value FROM settings WHERE key = 'maintenance_mode'")
debug_mode: @query("SELECT value FROM settings WHERE key = 'debug_mode'")
api_rate_limit: @query("SELECT value FROM settings WHERE key = 'api_rate_limit'")
""")

# Set up settings table
db.execute("""
CREATE TABLE IF NOT EXISTS settings (
    key TEXT PRIMARY KEY,
    value TEXT,
    updated_at TIMESTAMP
)
""")

db.execute("""
INSERT OR REPLACE INTO settings VALUES 
('server_workers', '4', '2024-01-01 10:00:00'),
('max_connections', '1000', '2024-01-01 10:00:00'),
('db_host', 'localhost', '2024-01-01 10:00:00'),
('db_port', '5432', '2024-01-01 10:00:00'),
('db_name', 'myapp', '2024-01-01 10:00:00'),
('maintenance_mode', 'false', '2024-01-01 10:00:00'),
('debug_mode', 'true', '2024-01-01 10:00:00'),
('api_rate_limit', '1000', '2024-01-01 10:00:00')
""")

result = tsk.parse(config)
print(f"Server workers: {result['server']['workers']}")
print(f"Database host: {result['database']['host']}")
print(f"Maintenance mode: {result['features']['maintenance_mode']}")
```

### User-Specific Configuration

```python
config = TSK.from_string("""
[user]
profile: @query("SELECT * FROM users WHERE id = ?", @request.user_id)
preferences: @query("SELECT * FROM user_preferences WHERE user_id = ?", @request.user_id)
permissions: @query("SELECT permission FROM user_permissions WHERE user_id = ?", @request.user_id)

[content]
personalized_feed: @query("""
    SELECT p.* FROM posts p
    JOIN user_interests ui ON p.category = ui.category
    WHERE ui.user_id = ?
    ORDER BY p.created_at DESC
    LIMIT 10
""", @request.user_id)

recommended_items: @query("""
    SELECT i.* FROM items i
    JOIN user_purchases up ON i.category = up.category
    WHERE up.user_id = ? AND i.id NOT IN (
        SELECT item_id FROM user_purchases WHERE user_id = ?
    )
    ORDER BY i.rating DESC
    LIMIT 5
""", @request.user_id, @request.user_id)
""")

# Execute with user context
context = {
    'request': {
        'user_id': 1
    }
}

result = tsk.parse(config, context)
print(f"User profile: {result['user']['profile']}")
print(f"Personalized feed: {len(result['content']['personalized_feed'])} items")
```

## 🗃️ Database Schema Management

### Migration Support

```python
config = TSK.from_string("""
[migrations]
create_users_table: @migration("""
    CREATE TABLE IF NOT EXISTS users (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        name TEXT NOT NULL,
        email TEXT UNIQUE NOT NULL,
        password_hash TEXT NOT NULL,
        active BOOLEAN DEFAULT 1,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    )
""")

create_orders_table: @migration("""
    CREATE TABLE IF NOT EXISTS orders (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        user_id INTEGER NOT NULL,
        amount DECIMAL(10,2) NOT NULL,
        status TEXT DEFAULT 'pending',
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        FOREIGN KEY (user_id) REFERENCES users (id)
    )
""")

add_user_indexes: @migration("""
    CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
    CREATE INDEX IF NOT EXISTS idx_users_active ON users(active);
    CREATE INDEX IF NOT EXISTS idx_orders_user_id ON orders(user_id);
    CREATE INDEX IF NOT EXISTS idx_orders_status ON orders(status);
""")
""")

# Run migrations
tsk.run_migrations(config)
```

### Schema Validation

```python
config = TSK.from_string("""
[validation]
validate_schema: @validate_schema({
    "users": {
        "required_tables": ["id", "name", "email"],
        "types": {
            "id": "INTEGER",
            "name": "TEXT",
            "email": "TEXT",
            "active": "BOOLEAN"
        },
        "constraints": {
            "email": "UNIQUE",
            "id": "PRIMARY KEY"
        }
    },
    "orders": {
        "required_tables": ["id", "user_id", "amount"],
        "foreign_keys": {
            "user_id": "users(id)"
        }
    }
})
""")

# Validate schema
validation_result = tsk.validate_schema(config)
print(f"Schema validation: {validation_result}")
```

## 🔒 Security Features

### SQL Injection Prevention

```python
config = TSK.from_string("""
[users]
# Safe parameterized queries
user_by_id: @query("SELECT * FROM users WHERE id = ?", @request.user_id)
user_by_email: @query("SELECT * FROM users WHERE email = ?", @request.email)
search_users: @query("SELECT * FROM users WHERE name LIKE ?", @request.search_term)

# Safe dynamic queries with validation
filtered_users: @query("""
    SELECT * FROM users 
    WHERE active = ? 
    AND created_at > ?
    ORDER BY name
    LIMIT ?
""", @request.active, @request.since_date, @request.limit)
""")

# Execute with safe parameters
context = {
    'request': {
        'user_id': 1,
        'email': 'alice@example.com',
        'search_term': '%alice%',
        'active': True,
        'since_date': '2024-01-01',
        'limit': 10
    }
}

result = tsk.parse(config, context)
```

### Access Control

```python
config = TSK.from_string("""
[security]
user_permissions: @query("""
    SELECT permission FROM user_permissions 
    WHERE user_id = ? AND active = 1
""", @request.user_id)

can_access_resource: @query("""
    SELECT COUNT(*) > 0 as has_access
    FROM user_permissions up
    JOIN resource_permissions rp ON up.permission = rp.permission
    WHERE up.user_id = ? AND rp.resource = ? AND up.active = 1
""", @request.user_id, @request.resource)

audit_log: @query("""
    INSERT INTO audit_log (user_id, action, resource, timestamp)
    VALUES (?, ?, ?, CURRENT_TIMESTAMP)
""", @request.user_id, @request.action, @request.resource)
""")
```

## 📈 Performance Optimization

### Query Caching

```python
config = TSK.from_string("""
[performance]
# Cache expensive queries
user_count: @cache("5m", @query("SELECT COUNT(*) FROM users"))
active_user_count: @cache("1m", @query("SELECT COUNT(*) FROM users WHERE active = 1"))

# Cache with parameters
user_profile: @cache("10m", @query("SELECT * FROM users WHERE id = ?", @request.user_id))
user_orders: @cache("2m", @query("""
    SELECT * FROM orders 
    WHERE user_id = ? 
    ORDER BY created_at DESC 
    LIMIT 10
""", @request.user_id))

# Cache complex aggregations
revenue_stats: @cache("15m", @query("""
    SELECT 
        SUM(amount) as total_revenue,
        AVG(amount) as avg_order,
        COUNT(*) as order_count
    FROM orders 
    WHERE status = 'completed' 
    AND created_at > DATE('now', '-30 days')
"""))
""")
```

### Connection Pooling

```python
from tsk.adapters import PostgreSQLAdapter

# Configure connection pooling
postgres = PostgreSQLAdapter(
    host='localhost',
    database='myapp',
    user='postgres',
    password='secret',
    pool_size=20,
    max_overflow=30,
    pool_timeout=30,
    pool_recycle=3600
)

tsk = TSK()
tsk.set_database_adapter(postgres)

# Use with high concurrency
config = TSK.from_string("""
[concurrent]
user_data: @query("SELECT * FROM users WHERE id = ?", @request.user_id)
user_orders: @query("SELECT * FROM orders WHERE user_id = ?", @request.user_id)
user_preferences: @query("SELECT * FROM user_preferences WHERE user_id = ?", @request.user_id)
""")
```

## 🔄 Transaction Support

### Database Transactions

```python
config = TSK.from_string("""
[transactions]
create_user_with_profile: @transaction("""
    INSERT INTO users (name, email, password_hash) VALUES (?, ?, ?);
    INSERT INTO user_profiles (user_id, bio, avatar) VALUES (LAST_INSERT_ID(), ?, ?);
""", @request.name, @request.email, @request.password_hash, @request.bio, @request.avatar)

process_order: @transaction("""
    INSERT INTO orders (user_id, amount, status) VALUES (?, ?, 'pending');
    UPDATE user_balances SET balance = balance - ? WHERE user_id = ?;
    INSERT INTO order_history (order_id, action, timestamp) VALUES (LAST_INSERT_ID(), 'created', CURRENT_TIMESTAMP);
""", @request.user_id, @request.amount, @request.amount, @request.user_id)
""")

# Execute transactions
try:
    result = tsk.execute_transaction(config, 'transactions', 'create_user_with_profile')
    print(f"User created: {result}")
except Exception as e:
    print(f"Transaction failed: {e}")
```

## 🚀 Advanced Features

### Cross-Database Queries

```python
# Set up multiple databases
sqlite_db = SQLiteAdapter('app.db')
postgres_db = PostgreSQLAdapter(host='localhost', database='analytics')

# Configure TSK with multiple adapters
tsk = TSK()
tsk.set_database_adapter('main', sqlite_db)
tsk.set_database_adapter('analytics', postgres_db)

config = TSK.from_string("""
[analytics]
# Query from main database
user_data: @query.main("SELECT * FROM users WHERE id = ?", @request.user_id)

# Query from analytics database
user_metrics: @query.analytics("""
    SELECT 
        user_id,
        COUNT(*) as page_views,
        AVG(session_duration) as avg_session
    FROM user_analytics 
    WHERE user_id = ? 
    AND date > CURRENT_DATE - INTERVAL '30 days'
    GROUP BY user_id
""", @request.user_id)

# Combine data from both databases
combined_data: @combine(@query.main("SELECT * FROM users WHERE id = ?", @request.user_id),
                       @query.analytics("SELECT * FROM user_analytics WHERE user_id = ?", @request.user_id))
""")
```

### Real-time Data Streaming

```python
config = TSK.from_string("""
[streaming]
live_orders: @stream("""
    SELECT * FROM orders 
    WHERE created_at > ? 
    ORDER BY created_at DESC
""", @date.subtract("1h"))

user_activity: @stream("""
    SELECT user_id, action, timestamp 
    FROM user_activity 
    WHERE timestamp > ? 
    ORDER BY timestamp DESC
""", @date.subtract("5m"))
""")

# Set up streaming
def handle_order_update(order_data):
    print(f"New order: {order_data}")

def handle_user_activity(activity_data):
    print(f"User activity: {activity_data}")

# Start streaming
tsk.start_streaming(config, 'streaming', 'live_orders', handle_order_update)
tsk.start_streaming(config, 'streaming', 'user_activity', handle_user_activity)
```

## 🛠️ Troubleshooting

### Common Database Issues

```python
# Test database connection
def test_connection():
    try:
        result = db.query("SELECT 1")
        print("Database connection successful")
        return True
    except Exception as e:
        print(f"Database connection failed: {e}")
        return False

# Check query performance
def analyze_query(query, params=None):
    import time
    start = time.time()
    result = db.query(query, params)
    end = time.time()
    print(f"Query took {(end-start)*1000:.2f}ms")
    return result

# Monitor slow queries
config = TSK.from_string("""
[monitoring]
slow_queries: @query("""
    SELECT query, execution_time, timestamp
    FROM query_log
    WHERE execution_time > 1000
    ORDER BY execution_time DESC
    LIMIT 10
""")

connection_stats: @query("""
    SELECT 
        COUNT(*) as active_connections,
        MAX(created_at) as last_connection
    FROM connection_log
    WHERE active = 1
""")
""")
```

## 🚀 Next Steps

Now that you understand database integration:

1. **Master @ Operators** - [006-at-operators-python.md](006-at-operators-python.md)
2. **Explore Advanced Features** - [007-advanced-features-python.md](007-advanced-features-python.md)
3. **Learn FUJSEN Functions** - [004-fujsen-python.md](004-fujsen-python.md)

## 💡 Best Practices

1. **Use parameterized queries** - Always use `?` placeholders for user input
2. **Cache expensive queries** - Use `@cache` for frequently accessed data
3. **Monitor performance** - Track query execution times
4. **Handle errors gracefully** - Always include error handling
5. **Use transactions** - Group related operations in transactions
6. **Validate data** - Check data types and constraints
7. **Optimize indexes** - Create appropriate database indexes

---

**"We don't bow to any king"** - TuskLang's database integration brings real-time data into your configuration, making it truly dynamic and responsive! 