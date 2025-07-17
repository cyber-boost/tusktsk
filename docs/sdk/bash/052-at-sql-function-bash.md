# 🗄️ TuskLang Bash @sql Function Guide

**"We don't bow to any king" – SQL is your configuration's database.**

The @sql function in TuskLang is your database powerhouse, enabling dynamic SQL query execution, database management, and data manipulation directly within your configuration files. Whether you're querying databases, managing schemas, or performing complex data operations, @sql provides the power and flexibility to work with databases seamlessly.

## 🎯 What is @sql?
The @sql function provides SQL database operations in TuskLang. It offers:
- **SQL query execution** - Execute SQL queries and return results
- **Database management** - Create, modify, and manage databases
- **Schema operations** - Create and modify database schemas
- **Data manipulation** - Insert, update, delete, and query data
- **Transaction management** - Handle database transactions

## 📝 Basic @sql Syntax

### Simple SQL Queries
```ini
[simple_queries]
# Basic SELECT query
user_count: @sql("SELECT COUNT(*) FROM users")
active_users: @sql("SELECT COUNT(*) FROM users WHERE status = 'active'")
recent_orders: @sql("SELECT * FROM orders WHERE created_at >= DATE_SUB(NOW(), INTERVAL 7 DAY)")

# Query with parameters
$user_id: 123
user_info: @sql("SELECT * FROM users WHERE id = ?", $user_id)
```

### Database Operations
```ini
[database_operations]
# Create database
create_db: @sql("CREATE DATABASE IF NOT EXISTS tusklang_app")

# Create table
create_table: @sql("""
CREATE TABLE IF NOT EXISTS users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
)
""")

# Insert data
insert_user: @sql("INSERT INTO users (name, email) VALUES (?, ?)", "John Doe", "john@example.com")
```

### Complex Queries
```ini
[complex_queries]
# Join query
user_orders: @sql("""
SELECT u.name, COUNT(o.id) as order_count, SUM(o.amount) as total_spent
FROM users u
LEFT JOIN orders o ON u.id = o.user_id
WHERE u.created_at >= DATE_SUB(NOW(), INTERVAL 30 DAY)
GROUP BY u.id
HAVING total_spent > 100
ORDER BY total_spent DESC
""")

# Subquery
top_customers: @sql("""
SELECT name, email, total_spent
FROM (
    SELECT u.name, u.email, SUM(o.amount) as total_spent
    FROM users u
    JOIN orders o ON u.id = o.user_id
    GROUP BY u.id
) as customer_totals
WHERE total_spent > (SELECT AVG(total_spent) FROM customer_totals)
""")
```

## 🚀 Quick Start Example

```bash
#!/bin/bash
source tusk-bash.sh

cat > sql-quickstart.tsk << 'EOF'
[database_setup]
# Create database and tables
create_database: @sql("CREATE DATABASE IF NOT EXISTS tusklang_demo")
use_database: @sql("USE tusklang_demo")

create_users_table: @sql("""
CREATE TABLE IF NOT EXISTS users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    status ENUM('active', 'inactive') DEFAULT 'active',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
)
""")

create_orders_table: @sql("""
CREATE TABLE IF NOT EXISTS orders (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT,
    amount DECIMAL(10,2) NOT NULL,
    status ENUM('pending', 'completed', 'cancelled') DEFAULT 'pending',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id)
)
""")

[data_operations]
# Insert sample data
insert_users: @sql("""
INSERT INTO users (name, email) VALUES 
('Alice Johnson', 'alice@example.com'),
('Bob Smith', 'bob@example.com'),
('Charlie Brown', 'charlie@example.com')
""")

insert_orders: @sql("""
INSERT INTO orders (user_id, amount, status) VALUES 
(1, 150.00, 'completed'),
(1, 75.50, 'completed'),
(2, 200.00, 'pending'),
(3, 125.25, 'completed')
""")

[query_operations]
# Query data
total_users: @sql("SELECT COUNT(*) FROM users")
active_users: @sql("SELECT COUNT(*) FROM users WHERE status = 'active'")
total_orders: @sql("SELECT COUNT(*) FROM orders")
total_revenue: @sql("SELECT SUM(amount) FROM orders WHERE status = 'completed'")

# Complex query
user_summary: @sql("""
SELECT 
    u.name,
    COUNT(o.id) as order_count,
    SUM(o.amount) as total_spent,
    AVG(o.amount) as avg_order
FROM users u
LEFT JOIN orders o ON u.id = o.user_id
GROUP BY u.id, u.name
ORDER BY total_spent DESC
""")
EOF

config=$(tusk_parse sql-quickstart.tsk)

echo "=== Database Setup ==="
echo "Database created: $(tusk_get "$config" database_setup.create_database)"
echo "Tables created: $(tusk_get "$config" database_setup.create_users_table)"

echo ""
echo "=== Data Operations ==="
echo "Users inserted: $(tusk_get "$config" data_operations.insert_users)"
echo "Orders inserted: $(tusk_get "$config" data_operations.insert_orders)"

echo ""
echo "=== Query Results ==="
echo "Total Users: $(tusk_get "$config" query_operations.total_users)"
echo "Active Users: $(tusk_get "$config" query_operations.active_users)"
echo "Total Orders: $(tusk_get "$config" query_operations.total_orders)"
echo "Total Revenue: $(tusk_get "$config" query_operations.total_revenue)"
echo "User Summary: $(tusk_get "$config" query_operations.user_summary)"
```

## 🔗 Real-World Use Cases

### 1. E-commerce Analytics
```ini
[ecommerce_analytics]
# Comprehensive e-commerce analytics
$sales_analytics: {
    "daily_sales": @sql("""
        SELECT 
            DATE(created_at) as sale_date,
            COUNT(*) as order_count,
            SUM(amount) as total_revenue,
            AVG(amount) as avg_order_value
        FROM orders 
        WHERE status = 'completed'
        AND created_at >= DATE_SUB(NOW(), INTERVAL 30 DAY)
        GROUP BY DATE(created_at)
        ORDER BY sale_date DESC
    """),
    
    "product_performance": @sql("""
        SELECT 
            p.name as product_name,
            COUNT(oi.id) as units_sold,
            SUM(oi.quantity * oi.price) as revenue,
            AVG(oi.price) as avg_price
        FROM products p
        JOIN order_items oi ON p.id = oi.product_id
        JOIN orders o ON oi.order_id = o.id
        WHERE o.status = 'completed'
        GROUP BY p.id, p.name
        ORDER BY revenue DESC
        LIMIT 10
    """),
    
    "customer_segments": @sql("""
        SELECT 
            CASE 
                WHEN total_spent >= 1000 THEN 'VIP'
                WHEN total_spent >= 500 THEN 'Premium'
                WHEN total_spent >= 100 THEN 'Regular'
                ELSE 'New'
            END as segment,
            COUNT(*) as customer_count,
            AVG(total_spent) as avg_spent
        FROM (
            SELECT u.id, SUM(o.amount) as total_spent
            FROM users u
            JOIN orders o ON u.id = o.user_id
            WHERE o.status = 'completed'
            GROUP BY u.id
        ) as customer_totals
        GROUP BY segment
    """)
}

# Generate analytics report
$analytics_report: {
    "report_date": @date("Y-m-d H:i:s"),
    "total_customers": @sql("SELECT COUNT(*) FROM users"),
    "total_orders": @sql("SELECT COUNT(*) FROM orders"),
    "total_revenue": @sql("SELECT SUM(amount) FROM orders WHERE status = 'completed'"),
    "avg_order_value": @sql("SELECT AVG(amount) FROM orders WHERE status = 'completed'"),
    "daily_sales": $sales_analytics.daily_sales,
    "top_products": $sales_analytics.product_performance,
    "customer_segments": $sales_analytics.customer_segments
}
```

### 2. User Management System
```ini
[user_management]
# User management operations
$user_operations: {
    "create_user": @sql("""
        INSERT INTO users (name, email, password_hash, role, status)
        VALUES (?, ?, ?, ?, ?)
    """, "New User", "newuser@example.com", @encrypt.hash("password123", "bcrypt"), "user", "active"),
    
    "update_user": @sql("""
        UPDATE users 
        SET name = ?, email = ?, role = ?, updated_at = NOW()
        WHERE id = ?
    """, "Updated Name", "updated@example.com", "admin", 1),
    
    "deactivate_user": @sql("""
        UPDATE users 
        SET status = 'inactive', deactivated_at = NOW()
        WHERE id = ?
    """, 2),
    
    "get_user_permissions": @sql("""
        SELECT p.name as permission_name, p.description
        FROM user_roles ur
        JOIN role_permissions rp ON ur.role_id = rp.role_id
        JOIN permissions p ON rp.permission_id = p.id
        WHERE ur.user_id = ?
    """, 1)
}

# User analytics
$user_analytics: {
    "active_users": @sql("SELECT COUNT(*) FROM users WHERE status = 'active'"),
    "users_by_role": @sql("""
        SELECT role, COUNT(*) as count
        FROM users 
        WHERE status = 'active'
        GROUP BY role
    """),
    "recent_registrations": @sql("""
        SELECT name, email, created_at
        FROM users
        WHERE created_at >= DATE_SUB(NOW(), INTERVAL 7 DAY)
        ORDER BY created_at DESC
    """),
    "inactive_users": @sql("""
        SELECT name, email, last_login
        FROM users
        WHERE status = 'inactive'
        AND last_login < DATE_SUB(NOW(), INTERVAL 30 DAY)
    """)
}
```

### 3. Inventory Management
```ini
[inventory_management]
# Inventory tracking and management
$inventory_operations: {
    "check_stock": @sql("""
        SELECT 
            p.name as product_name,
            p.sku,
            i.quantity as current_stock,
            i.reorder_level,
            CASE 
                WHEN i.quantity <= i.reorder_level THEN 'Low Stock'
                WHEN i.quantity = 0 THEN 'Out of Stock'
                ELSE 'In Stock'
            END as stock_status
        FROM products p
        JOIN inventory i ON p.id = i.product_id
        WHERE i.quantity <= i.reorder_level
        ORDER BY i.quantity ASC
    """),
    
    "update_stock": @sql("""
        UPDATE inventory 
        SET quantity = quantity + ?, updated_at = NOW()
        WHERE product_id = ?
    """, 50, 1),
    
    "stock_movement": @sql("""
        SELECT 
            p.name as product_name,
            sm.type as movement_type,
            sm.quantity,
            sm.reason,
            sm.created_at
        FROM stock_movements sm
        JOIN products p ON sm.product_id = p.id
        WHERE sm.created_at >= DATE_SUB(NOW(), INTERVAL 7 DAY)
        ORDER BY sm.created_at DESC
    """),
    
    "low_stock_alert": @sql("""
        SELECT 
            p.name as product_name,
            p.sku,
            i.quantity as current_stock,
            i.reorder_level,
            (i.reorder_level - i.quantity) as units_needed
        FROM products p
        JOIN inventory i ON p.id = i.product_id
        WHERE i.quantity <= i.reorder_level
        ORDER BY (i.reorder_level - i.quantity) DESC
    """)
}

# Inventory analytics
$inventory_analytics: {
    "total_products": @sql("SELECT COUNT(*) FROM products"),
    "low_stock_count": @sql("""
        SELECT COUNT(*) 
        FROM inventory 
        WHERE quantity <= reorder_level
    """),
    "out_of_stock_count": @sql("""
        SELECT COUNT(*) 
        FROM inventory 
        WHERE quantity = 0
    """),
    "total_inventory_value": @sql("""
        SELECT SUM(i.quantity * p.price) 
        FROM inventory i
        JOIN products p ON i.product_id = p.id
    """)
}
```

### 4. Financial Reporting
```ini
[financial_reporting]
# Financial reporting and analysis
$financial_operations: {
    "monthly_revenue": @sql("""
        SELECT 
            YEAR(created_at) as year,
            MONTH(created_at) as month,
            SUM(amount) as revenue,
            COUNT(*) as order_count,
            AVG(amount) as avg_order
        FROM orders
        WHERE status = 'completed'
        AND created_at >= DATE_SUB(NOW(), INTERVAL 12 MONTH)
        GROUP BY YEAR(created_at), MONTH(created_at)
        ORDER BY year DESC, month DESC
    """),
    
    "customer_lifetime_value": @sql("""
        SELECT 
            u.name,
            u.email,
            COUNT(o.id) as total_orders,
            SUM(o.amount) as total_spent,
            AVG(o.amount) as avg_order_value,
            MAX(o.created_at) as last_order_date
        FROM users u
        JOIN orders o ON u.id = o.user_id
        WHERE o.status = 'completed'
        GROUP BY u.id, u.name, u.email
        ORDER BY total_spent DESC
    """),
    
    "refund_analysis": @sql("""
        SELECT 
            DATE(created_at) as refund_date,
            COUNT(*) as refund_count,
            SUM(amount) as refund_amount,
            AVG(amount) as avg_refund
        FROM refunds
        WHERE created_at >= DATE_SUB(NOW(), INTERVAL 30 DAY)
        GROUP BY DATE(created_at)
        ORDER BY refund_date DESC
    """),
    
    "profit_margin": @sql("""
        SELECT 
            p.name as product_name,
            p.price as selling_price,
            p.cost as product_cost,
            (p.price - p.cost) as profit,
            ((p.price - p.cost) / p.price * 100) as margin_percentage
        FROM products p
        ORDER BY margin_percentage DESC
    """)
}

# Financial summary
$financial_summary: {
    "total_revenue": @sql("SELECT SUM(amount) FROM orders WHERE status = 'completed'"),
    "total_refunds": @sql("SELECT SUM(amount) FROM refunds"),
    "net_revenue": @sql("""
        SELECT 
            (SELECT SUM(amount) FROM orders WHERE status = 'completed') -
            (SELECT COALESCE(SUM(amount), 0) FROM refunds)
    """),
    "avg_order_value": @sql("SELECT AVG(amount) FROM orders WHERE status = 'completed'"),
    "top_customers": $financial_operations.customer_lifetime_value
}
```

## 🧠 Advanced @sql Patterns

### Transaction Management
```ini
[transaction_management]
# Handle database transactions
$transaction_operations: {
    "transfer_funds": @sql.transaction("""
        BEGIN;
        
        -- Deduct from source account
        UPDATE accounts 
        SET balance = balance - ? 
        WHERE id = ? AND balance >= ?;
        
        -- Add to destination account
        UPDATE accounts 
        SET balance = balance + ? 
        WHERE id = ?;
        
        -- Record transaction
        INSERT INTO transactions (from_account, to_account, amount, type)
        VALUES (?, ?, ?, 'transfer');
        
        COMMIT;
    """, 100.00, 1, 100.00, 100.00, 2, 1, 2, 100.00),
    
    "batch_insert": @sql.transaction("""
        BEGIN;
        
        INSERT INTO users (name, email) VALUES 
        ('User1', 'user1@example.com'),
        ('User2', 'user2@example.com'),
        ('User3', 'user3@example.com');
        
        INSERT INTO user_profiles (user_id, bio) VALUES 
        (LAST_INSERT_ID()-2, 'Bio for User1'),
        (LAST_INSERT_ID()-1, 'Bio for User2'),
        (LAST_INSERT_ID(), 'Bio for User3');
        
        COMMIT;
    """)
}
```

### Dynamic Query Building
```ini
[dynamic_queries]
# Build dynamic SQL queries
$query_builder: {
    "build_user_query": @sql.dynamic("""
        SELECT id, name, email, status, created_at
        FROM users
        WHERE 1=1
        {{#if name_filter}}
        AND name LIKE ?
        {{/if}}
        {{#if status_filter}}
        AND status = ?
        {{/if}}
        {{#if date_filter}}
        AND created_at >= ?
        {{/if}}
        ORDER BY created_at DESC
        LIMIT ?
    """, {
        "name_filter": @env("NAME_FILTER"),
        "status_filter": @env("STATUS_FILTER"),
        "date_filter": @env("DATE_FILTER"),
        "limit": @env("QUERY_LIMIT", "100")
    }),
    
    "build_search_query": @sql.dynamic("""
        SELECT p.*, c.name as category_name
        FROM products p
        JOIN categories c ON p.category_id = c.id
        WHERE 1=1
        {{#if search_term}}
        AND (p.name LIKE ? OR p.description LIKE ?)
        {{/if}}
        {{#if category_id}}
        AND p.category_id = ?
        {{/if}}
        {{#if min_price}}
        AND p.price >= ?
        {{/if}}
        {{#if max_price}}
        AND p.price <= ?
        {{/if}}
        ORDER BY p.created_at DESC
    """, {
        "search_term": @env("SEARCH_TERM"),
        "category_id": @env("CATEGORY_ID"),
        "min_price": @env("MIN_PRICE"),
        "max_price": @env("MAX_PRICE")
    })
}
```

### Performance Optimization
```ini
[performance_optimization]
# Optimize SQL queries for performance
$optimized_queries: {
    "indexed_query": @sql("""
        SELECT u.name, COUNT(o.id) as order_count
        FROM users u
        USE INDEX (idx_users_status_created)
        LEFT JOIN orders o USE INDEX (idx_orders_user_status)
        ON u.id = o.user_id AND o.status = 'completed'
        WHERE u.status = 'active'
        AND u.created_at >= DATE_SUB(NOW(), INTERVAL 30 DAY)
        GROUP BY u.id, u.name
        HAVING order_count > 0
    """),
    
    "partitioned_query": @sql("""
        SELECT 
            DATE(created_at) as order_date,
            COUNT(*) as order_count,
            SUM(amount) as daily_revenue
        FROM orders PARTITION (p2024)
        WHERE created_at >= '2024-01-01'
        AND created_at < '2025-01-01'
        GROUP BY DATE(created_at)
        ORDER BY order_date DESC
    """),
    
    "cached_query": @sql.cached("""
        SELECT 
            category_id,
            COUNT(*) as product_count,
            AVG(price) as avg_price
        FROM products
        WHERE status = 'active'
        GROUP BY category_id
    """, "5m")
}
```

## 🛡️ Security & Performance Notes
- **SQL injection prevention:** Use parameterized queries and input validation
- **Query optimization:** Use indexes and optimize complex queries
- **Transaction safety:** Use transactions for data consistency
- **Connection pooling:** Implement proper connection management
- **Query timeout:** Set appropriate timeouts for long-running queries
- **Access control:** Implement proper database permissions

## 🐞 Troubleshooting
- **Connection issues:** Check database connectivity and credentials
- **Query performance:** Monitor slow queries and optimize indexes
- **Transaction deadlocks:** Implement proper transaction ordering
- **Memory usage:** Monitor query result sizes and implement pagination
- **Data consistency:** Use transactions for multi-step operations

## 💡 Best Practices
- **Use parameterized queries:** Prevent SQL injection attacks
- **Optimize queries:** Use indexes and avoid SELECT *
- **Handle transactions:** Use transactions for data consistency
- **Monitor performance:** Track query execution times
- **Implement caching:** Cache frequently accessed data
- **Validate input:** Validate all user inputs before queries

## 🔗 Cross-References
- [@ Operator Introduction](024-at-operator-intro-bash.md)
- [@query Function](027-at-query-function-bash.md)
- [Database Integration](076-database-integration-bash.md)
- [Performance Optimization](095-performance-optimization-bash.md)
- [Security Best Practices](099-security-best-practices-bash.md)

---

**Master @sql in TuskLang and wield the power of databases in your configurations. 🗄️** 