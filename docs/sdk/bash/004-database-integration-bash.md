# 🗃️ TuskLang Bash Database Integration Guide

**"We don't bow to any king" - Database queries in configuration files**

Welcome to the revolutionary world where your configuration files can directly query databases! TuskLang's database integration brings real-time data into your configuration, making your shell scripts dynamic and data-driven. Say goodbye to static configs and hello to configuration with a heartbeat.

## 🚀 Quick Start

### Basic Database Setup

```bash
#!/bin/bash
source tusk-bash.sh

# Set up database connection
tusk_set_db_adapter "sqlite" "/tmp/test.db"

# Create a simple configuration with database queries
cat > db-config.tsk << 'EOF'
[users]
total_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
user_list: @query("SELECT name, email FROM users LIMIT 10")

[system]
last_backup: @query("SELECT MAX(created_at) FROM backups")
backup_count: @query("SELECT COUNT(*) FROM backups WHERE created_at > ?", @date.subtract("7d"))
EOF

# Parse and use the configuration
config=$(tusk_parse db-config.tsk)

echo "Total Users: $(tusk_get "$config" users.total_count)"
echo "Active Users: $(tusk_get "$config" users.active_users)"
echo "Last Backup: $(tusk_get "$config" system.last_backup)"
```

## 🗄️ Supported Databases

### 1. SQLite

```bash
#!/bin/bash
source tusk-bash.sh

# Set up SQLite connection
tusk_set_db_adapter "sqlite" "/path/to/database.db"

# Create test database
sqlite3 /path/to/database.db << 'EOF'
CREATE TABLE users (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    email TEXT UNIQUE,
    active INTEGER DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO users (name, email) VALUES 
    ('Alice', 'alice@example.com'),
    ('Bob', 'bob@example.com'),
    ('Charlie', 'charlie@example.com');
EOF

cat > sqlite-config.tsk << 'EOF'
[users]
count: @query("SELECT COUNT(*) FROM users")
active_count: @query("SELECT COUNT(*) FROM users WHERE active = 1")
recent_users: @query("SELECT name FROM users WHERE created_at > ?", @date.subtract("30d"))
EOF

config=$(tusk_parse sqlite-config.tsk)
echo "Total Users: $(tusk_get "$config" users.count)"
echo "Active Users: $(tusk_get "$config" users.active_count)"
```

### 2. PostgreSQL

```bash
#!/bin/bash
source tusk-bash.sh

# Set up PostgreSQL connection
tusk_set_db_adapter "postgresql" "host=localhost port=5432 dbname=myapp user=postgres password=secret"

# Create test database
psql -h localhost -U postgres -d myapp << 'EOF'
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(255) UNIQUE,
    active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO users (name, email) VALUES 
    ('Alice', 'alice@example.com'),
    ('Bob', 'bob@example.com'),
    ('Charlie', 'charlie@example.com');
EOF

cat > postgres-config.tsk << 'EOF'
[users]
count: @query("SELECT COUNT(*) FROM users")
active_count: @query("SELECT COUNT(*) FROM users WHERE active = true")
recent_users: @query("SELECT name FROM users WHERE created_at > NOW() - INTERVAL '30 days'")
EOF

config=$(tusk_parse postgres-config.tsk)
echo "Total Users: $(tusk_get "$config" users.count)"
echo "Active Users: $(tusk_get "$config" users.active_count)"
```

### 3. MySQL

```bash
#!/bin/bash
source tusk-bash.sh

# Set up MySQL connection
tusk_set_db_adapter "mysql" "host=localhost port=3306 dbname=myapp user=root password=secret"

# Create test database
mysql -h localhost -u root -psecret myapp << 'EOF'
CREATE TABLE users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(255) UNIQUE,
    active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO users (name, email) VALUES 
    ('Alice', 'alice@example.com'),
    ('Bob', 'bob@example.com'),
    ('Charlie', 'charlie@example.com');
EOF

cat > mysql-config.tsk << 'EOF'
[users]
count: @query("SELECT COUNT(*) FROM users")
active_count: @query("SELECT COUNT(*) FROM users WHERE active = 1")
recent_users: @query("SELECT name FROM users WHERE created_at > DATE_SUB(NOW(), INTERVAL 30 DAY)")
EOF

config=$(tusk_parse mysql-config.tsk)
echo "Total Users: $(tusk_get "$config" users.count)"
echo "Active Users: $(tusk_get "$config" users.active_count)"
```

### 4. MongoDB

```bash
#!/bin/bash
source tusk-bash.sh

# Set up MongoDB connection
tusk_set_db_adapter "mongodb" "mongodb://localhost:27017/myapp"

# Create test data
mongo myapp << 'EOF'
db.users.insertMany([
    { name: "Alice", email: "alice@example.com", active: true, created_at: new Date() },
    { name: "Bob", email: "bob@example.com", active: true, created_at: new Date() },
    { name: "Charlie", email: "charlie@example.com", active: false, created_at: new Date() }
]);
EOF

cat > mongo-config.tsk << 'EOF'
[users]
count: @query("db.users.count()")
active_count: @query("db.users.count({active: true})")
recent_users: @query("db.users.find({created_at: {$gt: new Date(Date.now() - 30*24*60*60*1000)}}).toArray()")
EOF

config=$(tusk_parse mongo-config.tsk)
echo "Total Users: $(tusk_get "$config" users.count)"
echo "Active Users: $(tusk_get "$config" users.active_count)"
```

### 5. Redis

```bash
#!/bin/bash
source tusk-bash.sh

# Set up Redis connection
tusk_set_db_adapter "redis" "localhost:6379"

# Create test data
redis-cli << 'EOF'
SET user:1:name "Alice"
SET user:1:email "alice@example.com"
SET user:1:active "1"
SET user:2:name "Bob"
SET user:2:email "bob@example.com"
SET user:2:active "1"
SET user:3:name "Charlie"
SET user:3:email "charlie@example.com"
SET user:3:active "0"
EOF

cat > redis-config.tsk << 'EOF'
[users]
count: @query("KEYS user:*:name")
active_count: @query("KEYS user:*:active")
user_names: @query("MGET user:1:name user:2:name user:3:name")
EOF

config=$(tusk_parse redis-config.tsk)
echo "Total Users: $(tusk_get "$config" users.count)"
echo "Active Users: $(tusk_get "$config" users.active_count)"
```

## 🔍 Query Types

### 1. Simple Queries

```bash
#!/bin/bash
source tusk-bash.sh

tusk_set_db_adapter "sqlite" "/tmp/test.db"

cat > simple-queries.tsk << 'EOF'
[simple]
# Count records
user_count: @query("SELECT COUNT(*) FROM users")

# Get single value
latest_user: @query("SELECT name FROM users ORDER BY created_at DESC LIMIT 1")

# Get multiple values
all_emails: @query("SELECT email FROM users")

# Get with conditions
active_users: @query("SELECT name FROM users WHERE active = 1")
EOF

config=$(tusk_parse simple-queries.tsk)
echo "User Count: $(tusk_get "$config" simple.user_count)"
echo "Latest User: $(tusk_get "$config" simple.latest_user)"
echo "Active Users: $(tusk_get "$config" simple.active_users)"
```

### 2. Parameterized Queries

```bash
#!/bin/bash
source tusk-bash.sh

tusk_set_db_adapter "sqlite" "/tmp/test.db"

cat > parameterized-queries.tsk << 'EOF'
[parameterized]
# Single parameter
user_by_id: @query("SELECT name FROM users WHERE id = ?", 1)

# Multiple parameters
user_by_email: @query("SELECT name FROM users WHERE email = ? AND active = ?", "alice@example.com", 1)

# Date parameters
recent_users: @query("SELECT name FROM users WHERE created_at > ?", @date.subtract("7d"))

# Dynamic parameters
$user_id: @env("USER_ID", "1")
current_user: @query("SELECT name FROM users WHERE id = ?", $user_id)
EOF

config=$(tusk_parse parameterized-queries.tsk)
echo "User by ID: $(tusk_get "$config" parameterized.user_by_id)"
echo "User by Email: $(tusk_get "$config" parameterized.user_by_email)"
echo "Recent Users: $(tusk_get "$config" parameterized.recent_users)"
```

### 3. Complex Queries

```bash
#!/bin/bash
source tusk-bash.sh

tusk_set_db_adapter "sqlite" "/tmp/test.db"

cat > complex-queries.tsk << 'EOF'
[complex]
# Joins
user_orders: @query("""
    SELECT u.name, COUNT(o.id) as order_count 
    FROM users u 
    LEFT JOIN orders o ON u.id = o.user_id 
    GROUP BY u.id, u.name
""")

# Subqueries
users_with_orders: @query("""
    SELECT name FROM users 
    WHERE id IN (SELECT DISTINCT user_id FROM orders)
""")

# Aggregations
stats: @query("""
    SELECT 
        COUNT(*) as total_users,
        SUM(CASE WHEN active = 1 THEN 1 ELSE 0 END) as active_users,
        AVG(CASE WHEN active = 1 THEN 1 ELSE 0 END) as active_ratio
    FROM users
""")

# Window functions (PostgreSQL)
user_rankings: @query("""
    SELECT name, 
           ROW_NUMBER() OVER (ORDER BY created_at) as rank
    FROM users
""")
EOF

config=$(tusk_parse complex-queries.tsk)
echo "User Orders: $(tusk_get "$config" complex.user_orders)"
echo "Users with Orders: $(tusk_get "$config" complex.users_with_orders)"
echo "Stats: $(tusk_get "$config" complex.stats)"
```

## 🔄 Transactions

### Database Transactions

```bash
#!/bin/bash
source tusk-bash.sh

tusk_set_db_adapter "sqlite" "/tmp/test.db"

cat > transactions.tsk << 'EOF'
[transactions]
# Begin transaction
@transaction.begin()

# Multiple operations
create_user: @query("INSERT INTO users (name, email) VALUES (?, ?)", "David", "david@example.com")
update_count: @query("UPDATE user_stats SET count = count + 1")

# Commit transaction
@transaction.commit()

# Rollback on error
@transaction.rollback_on_error()
EOF

# Execute transaction
execute_transaction() {
    config=$(tusk_parse transactions.tsk)
    
    if [[ $? -eq 0 ]]; then
        echo "Transaction completed successfully"
        echo "New user created: $(tusk_get "$config" transactions.create_user)"
    else
        echo "Transaction failed, rolled back"
    fi
}

execute_transaction
```

## 📊 Real-World Examples

### 1. System Monitoring Dashboard

```bash
#!/bin/bash
source tusk-bash.sh

tusk_set_db_adapter "sqlite" "/var/log/system.db"

cat > monitoring-dashboard.tsk << 'EOF'
[system_metrics]
# Current system stats
cpu_usage: @shell("top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1")
memory_usage: @shell("free | grep Mem | awk '{printf \"%.2f\", $3/$2 * 100.0}'")
disk_usage: @shell("df / | tail -1 | awk '{print $5}' | sed 's/%//'")

# Historical data
avg_cpu_24h: @query("SELECT AVG(cpu_usage) FROM system_metrics WHERE timestamp > datetime('now', '-24 hours')")
avg_memory_24h: @query("SELECT AVG(memory_usage) FROM system_metrics WHERE timestamp > datetime('now', '-24 hours')")
peak_cpu: @query("SELECT MAX(cpu_usage) FROM system_metrics WHERE timestamp > datetime('now', '-24 hours')")

# Alerts
high_cpu_alert: @query("SELECT COUNT(*) FROM system_metrics WHERE cpu_usage > 80 AND timestamp > datetime('now', '-1 hour')")
high_memory_alert: @query("SELECT COUNT(*) FROM system_metrics WHERE memory_usage > 85 AND timestamp > datetime('now', '-1 hour')")

[actions]
# Store current metrics
@query("INSERT INTO system_metrics (cpu_usage, memory_usage, disk_usage, timestamp) VALUES (?, ?, ?, datetime('now'))", 
       @shell("top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | cut -d'%' -f1"),
       @shell("free | grep Mem | awk '{printf \"%.2f\", $3/$2 * 100.0}'"),
       @shell("df / | tail -1 | awk '{print $5}' | sed 's/%//'"))
EOF

# Create monitoring database
sqlite3 /var/log/system.db << 'EOF'
CREATE TABLE IF NOT EXISTS system_metrics (
    id INTEGER PRIMARY KEY,
    cpu_usage REAL,
    memory_usage REAL,
    disk_usage REAL,
    timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
);
EOF

# Run monitoring
monitor_system() {
    config=$(tusk_parse monitoring-dashboard.tsk)
    
    echo "=== System Dashboard ==="
    echo "Current CPU: $(tusk_get "$config" system_metrics.cpu_usage)%"
    echo "Current Memory: $(tusk_get "$config" system_metrics.memory_usage)%"
    echo "Current Disk: $(tusk_get "$config" system_metrics.disk_usage)%"
    echo ""
    echo "24h Average CPU: $(tusk_get "$config" system_metrics.avg_cpu_24h)%"
    echo "24h Average Memory: $(tusk_get "$config" system_metrics.avg_memory_24h)%"
    echo "24h Peak CPU: $(tusk_get "$config" system_metrics.peak_cpu)%"
    echo ""
    echo "High CPU Alerts (1h): $(tusk_get "$config" system_metrics.high_cpu_alert)"
    echo "High Memory Alerts (1h): $(tusk_get "$config" system_metrics.high_memory_alert)"
}

monitor_system
```

### 2. User Management System

```bash
#!/bin/bash
source tusk-bash.sh

tusk_set_db_adapter "sqlite" "/var/lib/users.db"

cat > user-management.tsk << 'EOF'
[user_management]
# User statistics
total_users: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
new_users_today: @query("SELECT COUNT(*) FROM users WHERE DATE(created_at) = DATE('now')")
new_users_week: @query("SELECT COUNT(*) FROM users WHERE created_at > datetime('now', '-7 days')")

# User lists
recent_users: @query("SELECT name, email, created_at FROM users ORDER BY created_at DESC LIMIT 10")
inactive_users: @query("SELECT name, email, last_login FROM users WHERE active = 0 ORDER BY last_login DESC")

# User actions
$new_user_name: @env("NEW_USER_NAME", "")
$new_user_email: @env("NEW_USER_EMAIL", "")

create_user: @if($new_user_name != "" && $new_user_email != "", 
    @query("INSERT INTO users (name, email, active, created_at) VALUES (?, ?, 1, datetime('now'))", 
           $new_user_name, $new_user_email), 
    null)

# User validation
user_exists: @query("SELECT COUNT(*) FROM users WHERE email = ?", $new_user_email)
EOF

# Create user database
sqlite3 /var/lib/users.db << 'EOF'
CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    email TEXT UNIQUE NOT NULL,
    active INTEGER DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    last_login DATETIME
);

INSERT OR IGNORE INTO users (name, email) VALUES 
    ('Alice', 'alice@example.com'),
    ('Bob', 'bob@example.com'),
    ('Charlie', 'charlie@example.com');
EOF

# User management functions
manage_users() {
    config=$(tusk_parse user-management.tsk)
    
    echo "=== User Management Dashboard ==="
    echo "Total Users: $(tusk_get "$config" user_management.total_users)"
    echo "Active Users: $(tusk_get "$config" user_management.active_users)"
    echo "New Users Today: $(tusk_get "$config" user_management.new_users_today)"
    echo "New Users This Week: $(tusk_get "$config" user_management.new_users_week)"
    echo ""
    echo "Recent Users:"
    echo "$(tusk_get "$config" user_management.recent_users)"
}

# Create new user
create_new_user() {
    export NEW_USER_NAME="$1"
    export NEW_USER_EMAIL="$2"
    
    config=$(tusk_parse user-management.tsk)
    
    if [[ $(tusk_get "$config" user_management.user_exists) -gt 0 ]]; then
        echo "User with email $NEW_USER_EMAIL already exists"
        return 1
    fi
    
    if [[ $(tusk_get "$config" user_management.create_user) != "null" ]]; then
        echo "User $NEW_USER_NAME created successfully"
    else
        echo "Failed to create user"
        return 1
    fi
}

manage_users
create_new_user "David" "david@example.com"
```

### 3. E-commerce Analytics

```bash
#!/bin/bash
source tusk-bash.sh

tusk_set_db_adapter "sqlite" "/var/lib/ecommerce.db"

cat > ecommerce-analytics.tsk << 'EOF'
[ecommerce]
# Sales metrics
total_sales: @query("SELECT SUM(amount) FROM orders WHERE status = 'completed'")
today_sales: @query("SELECT SUM(amount) FROM orders WHERE status = 'completed' AND DATE(created_at) = DATE('now')")
monthly_sales: @query("SELECT SUM(amount) FROM orders WHERE status = 'completed' AND created_at > datetime('now', '-30 days')")

# Order metrics
total_orders: @query("SELECT COUNT(*) FROM orders")
pending_orders: @query("SELECT COUNT(*) FROM orders WHERE status = 'pending'")
completed_orders: @query("SELECT COUNT(*) FROM orders WHERE status = 'completed'")

# Customer metrics
total_customers: @query("SELECT COUNT(DISTINCT user_id) FROM orders")
repeat_customers: @query("SELECT COUNT(*) FROM (SELECT user_id FROM orders GROUP BY user_id HAVING COUNT(*) > 1)")

# Product metrics
top_products: @query("""
    SELECT p.name, COUNT(o.id) as order_count, SUM(oi.quantity) as total_quantity
    FROM products p
    JOIN order_items oi ON p.id = oi.product_id
    JOIN orders o ON oi.order_id = o.id
    WHERE o.status = 'completed'
    GROUP BY p.id, p.name
    ORDER BY order_count DESC
    LIMIT 5
""")

# Revenue by category
category_revenue: @query("""
    SELECT c.name, SUM(oi.quantity * oi.price) as revenue
    FROM categories c
    JOIN products p ON c.id = p.category_id
    JOIN order_items oi ON p.id = oi.product_id
    JOIN orders o ON oi.order_id = o.id
    WHERE o.status = 'completed'
    GROUP BY c.id, c.name
    ORDER BY revenue DESC
""")
EOF

# Create e-commerce database
sqlite3 /var/lib/ecommerce.db << 'EOF'
CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    email TEXT UNIQUE NOT NULL
);

CREATE TABLE IF NOT EXISTS categories (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS products (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    price REAL NOT NULL,
    category_id INTEGER,
    FOREIGN KEY (category_id) REFERENCES categories (id)
);

CREATE TABLE IF NOT EXISTS orders (
    id INTEGER PRIMARY KEY,
    user_id INTEGER,
    amount REAL NOT NULL,
    status TEXT DEFAULT 'pending',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users (id)
);

CREATE TABLE IF NOT EXISTS order_items (
    id INTEGER PRIMARY KEY,
    order_id INTEGER,
    product_id INTEGER,
    quantity INTEGER NOT NULL,
    price REAL NOT NULL,
    FOREIGN KEY (order_id) REFERENCES orders (id),
    FOREIGN KEY (product_id) REFERENCES products (id)
);

-- Insert sample data
INSERT OR IGNORE INTO users (name, email) VALUES 
    ('Alice', 'alice@example.com'),
    ('Bob', 'bob@example.com');

INSERT OR IGNORE INTO categories (name) VALUES 
    ('Electronics'),
    ('Clothing'),
    ('Books');

INSERT OR IGNORE INTO products (name, price, category_id) VALUES 
    ('Laptop', 999.99, 1),
    ('T-Shirt', 29.99, 2),
    ('Programming Book', 49.99, 3);

INSERT OR IGNORE INTO orders (user_id, amount, status) VALUES 
    (1, 999.99, 'completed'),
    (2, 79.98, 'completed'),
    (1, 49.99, 'pending');
EOF

# Analytics dashboard
show_analytics() {
    config=$(tusk_parse ecommerce-analytics.tsk)
    
    echo "=== E-commerce Analytics Dashboard ==="
    echo "Sales Metrics:"
    echo "  Total Sales: $$(tusk_get "$config" ecommerce.total_sales)"
    echo "  Today's Sales: $$(tusk_get "$config" ecommerce.today_sales)"
    echo "  Monthly Sales: $$(tusk_get "$config" ecommerce.monthly_sales)"
    echo ""
    echo "Order Metrics:"
    echo "  Total Orders: $(tusk_get "$config" ecommerce.total_orders)"
    echo "  Pending Orders: $(tusk_get "$config" ecommerce.pending_orders)"
    echo "  Completed Orders: $(tusk_get "$config" ecommerce.completed_orders)"
    echo ""
    echo "Customer Metrics:"
    echo "  Total Customers: $(tusk_get "$config" ecommerce.total_customers)"
    echo "  Repeat Customers: $(tusk_get "$config" ecommerce.repeat_customers)"
    echo ""
    echo "Top Products:"
    echo "$(tusk_get "$config" ecommerce.top_products)"
    echo ""
    echo "Revenue by Category:"
    echo "$(tusk_get "$config" ecommerce.category_revenue)"
}

show_analytics
```

## 🔒 Security Best Practices

### Secure Database Configuration

```bash
#!/bin/bash
source tusk-bash.sh

cat > secure-db-config.tsk << 'EOF'
[security]
# Use environment variables for credentials
db_host: @env("DB_HOST", "localhost")
db_port: @env("DB_PORT", "5432")
db_name: @env("DB_NAME", "myapp")
db_user: @env("DB_USER", "postgres")
db_password: @env("DB_PASSWORD")

# Validate required environment variables
@validate.required(["DB_PASSWORD"])

# Encrypt sensitive data
encrypted_password: @encrypt(@env("DB_PASSWORD"), "AES-256-GCM")

# Connection string with validation
connection_string: "host=${db_host} port=${db_port} dbname=${db_name} user=${db_user} password=${db_password}"

# Set up secure connection
@db.connect(connection_string)
EOF

# Set up secure environment
export DB_HOST="localhost"
export DB_PORT="5432"
export DB_NAME="myapp"
export DB_USER="postgres"
export DB_PASSWORD="your-secure-password"

config=$(tusk_parse secure-db-config.tsk)

# Use encrypted password when needed
decrypted_password=$(tusk_decrypt "$(tusk_get "$config" security.encrypted_password)")
echo "Database connected securely"
```

## ⚡ Performance Optimization

### Query Optimization

```bash
#!/bin/bash
source tusk-bash.sh

tusk_set_db_adapter "sqlite" "/tmp/optimized.db"

cat > optimized-queries.tsk << 'EOF'
[optimization]
# Cache expensive queries
expensive_stats: @cache("5m", @query("""
    SELECT 
        COUNT(*) as total_users,
        AVG(CASE WHEN active = 1 THEN 1 ELSE 0 END) as active_ratio,
        MAX(created_at) as latest_user
    FROM users
"""))

# Use indexes for better performance
indexed_query: @query("SELECT name FROM users WHERE email = ?", "alice@example.com")

# Pagination for large datasets
paginated_users: @query("SELECT name, email FROM users ORDER BY created_at DESC LIMIT 10 OFFSET 0")

# Batch operations
batch_update: @query("UPDATE users SET last_login = datetime('now') WHERE id IN (1, 2, 3)")
EOF

# Create optimized database with indexes
sqlite3 /tmp/optimized.db << 'EOF'
CREATE TABLE users (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    email TEXT UNIQUE NOT NULL,
    active INTEGER DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    last_login DATETIME
);

-- Create indexes for better performance
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_active ON users(active);
CREATE INDEX idx_users_created_at ON users(created_at);

INSERT INTO users (name, email) VALUES 
    ('Alice', 'alice@example.com'),
    ('Bob', 'bob@example.com'),
    ('Charlie', 'charlie@example.com');
EOF

config=$(tusk_parse optimized-queries.tsk)
echo "Optimized Stats: $(tusk_get "$config" optimization.expensive_stats)"
echo "Indexed Query: $(tusk_get "$config" optimization.indexed_query)"
```

## 🎯 What You've Learned

In this database integration guide, you've mastered:

✅ **Multiple database support** - SQLite, PostgreSQL, MySQL, MongoDB, Redis  
✅ **Query types** - Simple, parameterized, and complex queries  
✅ **Transactions** - ACID-compliant database operations  
✅ **Real-world applications** - Monitoring, user management, e-commerce analytics  
✅ **Security best practices** - Encrypted credentials and validation  
✅ **Performance optimization** - Caching, indexing, and query optimization  
✅ **Dynamic configuration** - Real-time data in configuration files  

## 🚀 Next Steps

Ready to explore advanced features and build sophisticated applications?

1. **Advanced Features** → [005-advanced-features-bash.md](005-advanced-features-bash.md)

## 💡 Pro Tips

- **Use parameterized queries** - Always use `?` placeholders to prevent SQL injection
- **Cache expensive queries** - Use `@cache()` for frequently accessed data
- **Validate environment variables** - Use `@validate.required()` for database credentials
- **Encrypt sensitive data** - Use `@encrypt()` for passwords and API keys
- **Use transactions** - Wrap related operations in transactions for data consistency
- **Optimize queries** - Create indexes and use pagination for large datasets

---

**You're now a TuskLang database integration expert! 🗃️** 