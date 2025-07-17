# 🗄️ TuskLang Ruby Database Integration Guide

**"We don't bow to any king" - Ruby Edition**

Revolutionize your configuration with direct database queries. TuskLang allows you to embed database queries directly in your configuration files, making them dynamic and intelligent.

## 🚀 Database Queries in Configuration

### Basic Database Queries
```ruby
# config/database.tsk
$app_name: "MyApp"

[database]
host: "localhost"
port: 5432
name: "myapp"

[analytics]
total_users: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
premium_users: @query("SELECT COUNT(*) FROM users WHERE subscription_type = 'premium'")
recent_orders: @query("SELECT COUNT(*) FROM orders WHERE created_at > ?", @date.subtract("7d"))
```

### Parameterized Queries
```ruby
# config/dynamic.tsk
$days_ago: 30

[user_stats]
users_created_today: @query("SELECT COUNT(*) FROM users WHERE DATE(created_at) = ?", @date.today())
users_created_this_week: @query("SELECT COUNT(*) FROM users WHERE created_at > ?", @date.subtract("7d"))
users_created_this_month: @query("SELECT COUNT(*) FROM users WHERE created_at > ?", @date.subtract("30d"))
users_created_custom: @query("SELECT COUNT(*) FROM users WHERE created_at > ?", @date.subtract("#{days_ago}d"))

[order_stats]
orders_today: @query("SELECT COUNT(*) FROM orders WHERE DATE(created_at) = ?", @date.today())
revenue_today: @query("SELECT SUM(amount) FROM orders WHERE DATE(created_at) = ?", @date.today())
avg_order_value: @query("SELECT AVG(amount) FROM orders WHERE created_at > ?", @date.subtract("30d"))
```

## 🔌 Database Adapters

### 1. SQLite Adapter
```ruby
# config/sqlite.tsk
[database]
type: "sqlite"
file: "app.db"

[queries]
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
```

```ruby
# Ruby integration
require 'tusklang'
require 'tusklang/adapters'

# Basic SQLite connection
sqlite = TuskLang::Adapters::SQLiteAdapter.new('app.db')

# With options
sqlite_with_options = TuskLang::Adapters::SQLiteAdapter.new(
  filename: 'app.db',
  timeout: 30000,
  verbose: true,
  journal_mode: 'WAL'
)

# Create TuskLang parser with SQLite
parser = TuskLang.new
parser.database_adapter = sqlite

# Parse configuration with database queries
config = parser.parse_file('config/sqlite.tsk')
puts "Total users: #{config['queries']['user_count']}"
```

### 2. PostgreSQL Adapter
```ruby
# config/postgresql.tsk
[database]
type: "postgresql"
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: @env("DATABASE_PASSWORD")

[queries]
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
premium_users: @query("SELECT COUNT(*) FROM users WHERE subscription_type = 'premium'")
recent_orders: @query("SELECT COUNT(*) FROM orders WHERE created_at > NOW() - INTERVAL '7 days'")
```

```ruby
# Ruby integration
require 'tusklang'
require 'tusklang/adapters'

# Basic PostgreSQL connection
postgres = TuskLang::Adapters::PostgreSQLAdapter.new(
  host: 'localhost',
  port: 5432,
  database: 'myapp',
  user: 'postgres',
  password: 'secret'
)

# With connection pooling
postgres_with_pool = TuskLang::Adapters::PostgreSQLAdapter.new(
  host: 'localhost',
  database: 'myapp',
  user: 'postgres',
  password: 'secret'
).with_pool(
  max_open_conns: 20,
  max_idle_conns: 10,
  conn_max_lifetime: 30000
)

# With SSL
postgres_with_ssl = TuskLang::Adapters::PostgreSQLAdapter.new(
  host: 'localhost',
  database: 'myapp',
  user: 'postgres',
  password: 'secret',
  ssl_mode: 'require',
  ssl_cert: '/path/to/client-cert.pem',
  ssl_key: '/path/to/client-key.pem',
  ssl_root_cert: '/path/to/ca-cert.pem'
)

# Create TuskLang parser with PostgreSQL
parser = TuskLang.new
parser.database_adapter = postgres

# Parse configuration
config = parser.parse_file('config/postgresql.tsk')
puts "Total users: #{config['queries']['user_count']}"
```

### 3. MySQL Adapter
```ruby
# config/mysql.tsk
[database]
type: "mysql"
host: "localhost"
port: 3306
name: "myapp"
user: "root"
password: @env("DATABASE_PASSWORD")

[queries]
user_count: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
premium_users: @query("SELECT COUNT(*) FROM users WHERE subscription_type = 'premium'")
recent_orders: @query("SELECT COUNT(*) FROM orders WHERE created_at > DATE_SUB(NOW(), INTERVAL 7 DAY)")
```

```ruby
# Ruby integration
require 'tusklang'
require 'tusklang/adapters'

# Basic MySQL connection
mysql = TuskLang::Adapters::MySQLAdapter.new(
  host: 'localhost',
  port: 3306,
  database: 'myapp',
  user: 'root',
  password: 'secret'
)

# With connection pooling
mysql_with_pool = TuskLang::Adapters::MySQLAdapter.new(
  host: 'localhost',
  database: 'myapp',
  user: 'root',
  password: 'secret'
).with_pool(
  max_open_conns: 10,
  max_idle_conns: 5,
  conn_max_lifetime: 60000
)

# With SSL
mysql_with_ssl = TuskLang::Adapters::MySQLAdapter.new(
  host: 'localhost',
  database: 'myapp',
  user: 'root',
  password: 'secret',
  ssl: {
    ca: '/path/to/ca-cert.pem',
    cert: '/path/to/client-cert.pem',
    key: '/path/to/client-key.pem'
  }
)

# Create TuskLang parser with MySQL
parser = TuskLang.new
parser.database_adapter = mysql

# Parse configuration
config = parser.parse_file('config/mysql.tsk')
puts "Total users: #{config['queries']['user_count']}"
```

### 4. MongoDB Adapter
```ruby
# config/mongodb.tsk
[database]
type: "mongodb"
uri: "mongodb://localhost:27017/"
name: "myapp"

[queries]
user_count: @query("users", {}, { count: true })
active_users: @query("users", { active: true }, { count: true })
premium_users: @query("users", { subscription_type: "premium" }, { count: true })
recent_orders: @query("orders", { created_at: { $gt: @date.subtract("7d") } }, { count: true })
```

```ruby
# Ruby integration
require 'tusklang'
require 'tusklang/adapters'

# Basic MongoDB connection
mongo = TuskLang::Adapters::MongoDBAdapter.new(
  uri: 'mongodb://localhost:27017/',
  database: 'myapp'
)

# With authentication
mongo_with_auth = TuskLang::Adapters::MongoDBAdapter.new(
  uri: 'mongodb://user:pass@localhost:27017/',
  database: 'myapp',
  auth_source: 'admin'
)

# With connection pooling
mongo_with_pool = TuskLang::Adapters::MongoDBAdapter.new(
  uri: 'mongodb://localhost:27017/',
  database: 'myapp'
).with_pool(
  max_pool_size: 10,
  min_pool_size: 5,
  max_idle_time: 30000
)

# Create TuskLang parser with MongoDB
parser = TuskLang.new
parser.database_adapter = mongo

# Parse configuration
config = parser.parse_file('config/mongodb.tsk')
puts "Total users: #{config['queries']['user_count']}"
```

### 5. Redis Adapter
```ruby
# config/redis.tsk
[database]
type: "redis"
host: "localhost"
port: 6379
db: 0

[cache]
user_count: @query("GET", "stats:user_count")
active_users: @query("GET", "stats:active_users")
premium_users: @query("GET", "stats:premium_users")
session_count: @query("SCARD", "active_sessions")
```

```ruby
# Ruby integration
require 'tusklang'
require 'tusklang/adapters'

# Basic Redis connection
redis = TuskLang::Adapters::RedisAdapter.new(
  host: 'localhost',
  port: 6379,
  db: 0
)

# With authentication
redis_with_auth = TuskLang::Adapters::RedisAdapter.new(
  host: 'localhost',
  port: 6379,
  password: 'secret',
  db: 0
)

# With connection pooling
redis_with_pool = TuskLang::Adapters::RedisAdapter.new(
  host: 'localhost',
  port: 6379,
  db: 0
).with_pool(
  max_connections: 10,
  timeout: 5000
)

# Create TuskLang parser with Redis
parser = TuskLang.new
parser.database_adapter = redis

# Parse configuration
config = parser.parse_file('config/redis.tsk')
puts "User count: #{config['cache']['user_count']}"
```

## 🔄 Rails Integration

### 1. Rails Database Configuration
```ruby
# config/database.tsk
$environment: @env("RAILS_ENV", "development")

[database]
host: @env("DATABASE_HOST", "localhost")
port: @env("DATABASE_PORT", 5432)
name: @env("DATABASE_NAME", "myapp_#{environment}")
user: @env("DATABASE_USER", "postgres")
password: @env("DATABASE_PASSWORD", "")

[analytics]
total_users: @query("SELECT COUNT(*) FROM users")
active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
premium_users: @query("SELECT COUNT(*) FROM users WHERE subscription_type = 'premium'")
recent_orders: @query("SELECT COUNT(*) FROM orders WHERE created_at > ?", @date.subtract("7d"))
```

### 2. Rails Initializer
```ruby
# config/initializers/tusk.rb
Rails.application.config.after_initialize do
  # Configure TuskLang with Rails database
  TuskLang.configure do |config|
    case Rails.env
    when 'development'
      config.database_adapter = TuskLang::Adapters::SQLiteAdapter.new('db/development.sqlite3')
    when 'production'
      config.database_adapter = TuskLang::Adapters::PostgreSQLAdapter.new(
        host: ENV['DATABASE_HOST'],
        port: ENV['DATABASE_PORT'],
        database: ENV['DATABASE_NAME'],
        user: ENV['DATABASE_USER'],
        password: ENV['DATABASE_PASSWORD']
      )
    end
    
    config.logger = Rails.logger
  end
end
```

### 3. Rails Model Integration
```ruby
# app/models/analytics.rb
class Analytics
  def self.config
    TuskLang.config
  end
  
  def self.total_users
    config['analytics']['total_users']
  end
  
  def self.active_users
    config['analytics']['active_users']
  end
  
  def self.premium_users
    config['analytics']['premium_users']
  end
  
  def self.recent_orders
    config['analytics']['recent_orders']
  end
  
  def self.refresh_stats
    # Force refresh of cached queries
    TuskLang.config.database_adapter.clear_cache
  end
end
```

### 4. Rails Controller Usage
```ruby
# app/controllers/api/v1/analytics_controller.rb
class Api::V1::AnalyticsController < ApplicationController
  def dashboard
    analytics = {
      total_users: Analytics.total_users,
      active_users: Analytics.active_users,
      premium_users: Analytics.premium_users,
      recent_orders: Analytics.recent_orders
    }
    
    render json: {
      analytics: analytics,
      last_updated: Time.current
    }
  end
  
  def refresh
    Analytics.refresh_stats
    
    render json: {
      message: "Analytics refreshed successfully",
      timestamp: Time.current
    }
  end
end
```

## 🔧 Advanced Database Features

### 1. Multiple Database Connections
```ruby
# config/multi_db.tsk
$environment: @env("RAILS_ENV", "development")

[primary_db]
type: "postgresql"
host: @env("PRIMARY_DB_HOST", "localhost")
port: @env("PRIMARY_DB_PORT", 5432)
name: @env("PRIMARY_DB_NAME", "myapp_#{environment}")

[analytics_db]
type: "postgresql"
host: @env("ANALYTICS_DB_HOST", "localhost")
port: @env("ANALYTICS_DB_PORT", 5432)
name: @env("ANALYTICS_DB_NAME", "analytics_#{environment}")

[cache_db]
type: "redis"
host: @env("REDIS_HOST", "localhost")
port: @env("REDIS_PORT", 6379)

[queries]
# Primary database queries
user_count: @query.primary("SELECT COUNT(*) FROM users")
active_users: @query.primary("SELECT COUNT(*) FROM users WHERE active = true")

# Analytics database queries
daily_revenue: @query.analytics("SELECT SUM(amount) FROM transactions WHERE DATE(created_at) = ?", @date.today())
monthly_growth: @query.analytics("SELECT growth_rate FROM monthly_stats WHERE month = ?", @date.format("Y-m"))

# Cache queries
session_count: @query.cache("SCARD", "active_sessions")
cache_hits: @query.cache("GET", "stats:cache_hits")
```

### 2. Complex Queries with Joins
```ruby
# config/complex_queries.tsk
[analytics]
# User engagement metrics
active_users_with_orders: @query("""
    SELECT COUNT(DISTINCT u.id) 
    FROM users u 
    JOIN orders o ON u.id = o.user_id 
    WHERE u.active = true 
    AND o.created_at > ?
""", @date.subtract("30d"))

# Revenue by user type
revenue_by_user_type: @query("""
    SELECT 
        u.subscription_type,
        COUNT(DISTINCT u.id) as user_count,
        SUM(o.amount) as total_revenue,
        AVG(o.amount) as avg_order_value
    FROM users u
    JOIN orders o ON u.id = o.user_id
    WHERE o.created_at > ?
    GROUP BY u.subscription_type
    ORDER BY total_revenue DESC
""", @date.subtract("30d"))

# Top performing products
top_products: @query("""
    SELECT 
        p.name,
        COUNT(oi.id) as order_count,
        SUM(oi.quantity * oi.price) as total_revenue
    FROM products p
    JOIN order_items oi ON p.id = oi.product_id
    JOIN orders o ON oi.order_id = o.id
    WHERE o.created_at > ?
    GROUP BY p.id, p.name
    ORDER BY total_revenue DESC
    LIMIT 10
""", @date.subtract("30d"))
```

### 3. Caching Database Queries
```ruby
# config/cached_queries.tsk
[analytics]
# Cached queries with TTL
total_users: @cache("5m", @query("SELECT COUNT(*) FROM users"))
active_users: @cache("5m", @query("SELECT COUNT(*) FROM users WHERE active = true"))
premium_users: @cache("10m", @query("SELECT COUNT(*) FROM users WHERE subscription_type = 'premium'"))

# Aggregated statistics
daily_stats: @cache("1h", @query("""
    SELECT 
        DATE(created_at) as date,
        COUNT(*) as new_users,
        SUM(CASE WHEN subscription_type = 'premium' THEN 1 ELSE 0 END) as premium_signups
    FROM users 
    WHERE created_at > ?
    GROUP BY DATE(created_at)
    ORDER BY date DESC
    LIMIT 30
""", @date.subtract("30d")))

# Real-time metrics (no cache)
current_sessions: @query("SELECT COUNT(*) FROM sessions WHERE expires_at > NOW()")
pending_orders: @query("SELECT COUNT(*) FROM orders WHERE status = 'pending'")
```

## 🔒 Security Best Practices

### 1. Parameterized Queries
```ruby
# config/secure_queries.tsk
[user_queries]
# ✅ Secure - Parameterized
user_by_id: @query("SELECT * FROM users WHERE id = ?", @request.user_id)
users_by_status: @query("SELECT * FROM users WHERE status = ?", @request.status)

# ❌ Insecure - String concatenation (don't do this)
# user_by_id: @query("SELECT * FROM users WHERE id = #{@request.user_id}")
```

### 2. Environment Variables for Credentials
```ruby
# config/secure_config.tsk
[database]
host: @env("DATABASE_HOST", "localhost")
port: @env("DATABASE_PORT", 5432)
name: @env("DATABASE_NAME", "myapp")
user: @env("DATABASE_USER", "postgres")
password: @env("DATABASE_PASSWORD")  # No default for passwords
ssl_mode: @env("DATABASE_SSL_MODE", "prefer")
```

### 3. Connection Pooling
```ruby
# config/connection_pool.tsk
[database]
host: "localhost"
port: 5432
name: "myapp"
user: "postgres"
password: @env("DATABASE_PASSWORD")

[pool]
max_open_conns: @env("DB_MAX_OPEN_CONNS", 20)
max_idle_conns: @env("DB_MAX_IDLE_CONNS", 10)
conn_max_lifetime: @env("DB_CONN_MAX_LIFETIME", 30000)
```

## 🚀 Performance Optimization

### 1. Query Optimization
```ruby
# config/optimized_queries.tsk
[analytics]
# Use indexes effectively
active_users: @query("SELECT COUNT(*) FROM users WHERE active = true AND last_login > ?", @date.subtract("30d"))

# Limit result sets
recent_orders: @query("SELECT * FROM orders WHERE created_at > ? ORDER BY created_at DESC LIMIT 100", @date.subtract("7d"))

# Use specific columns instead of *
user_summary: @query("SELECT id, email, created_at, subscription_type FROM users WHERE active = true")
```

### 2. Caching Strategy
```ruby
# config/caching_strategy.tsk
[analytics]
# Frequently accessed data - short cache
total_users: @cache("1m", @query("SELECT COUNT(*) FROM users"))
active_users: @cache("1m", @query("SELECT COUNT(*) FROM users WHERE active = true"))

# Less frequently accessed - longer cache
monthly_stats: @cache("1h", @query("SELECT * FROM monthly_analytics WHERE month = ?", @date.format("Y-m")))
yearly_revenue: @cache("6h", @query("SELECT SUM(amount) FROM orders WHERE YEAR(created_at) = ?", @date.year))

# Rarely accessed - very long cache
historical_data: @cache("24h", @query("SELECT * FROM historical_analytics WHERE year < ?", @date.year))
```

## 🎯 Next Steps

Now that you understand database integration, explore:

1. **@ Operators Guide** - Master all the powerful built-in functions
2. **Advanced Features** - Caching, monitoring, and performance optimization
3. **Security Features** - Validation, encryption, and security best practices
4. **Rails Integration** - Deep integration with Rails applications
5. **Deployment Guide** - Production deployment and scaling

**Ready to revolutionize your database configuration? Let's Tusk! 🚀** 