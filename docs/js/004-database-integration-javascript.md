# 🟨 TuskLang JavaScript Database Integration Guide

**"We don't bow to any king" - JavaScript Edition**

Connect to databases directly from your configuration files. TuskLang supports 5 database adapters with full @ operator integration, making database queries as simple as writing configuration.

## 🗃️ Supported Databases

TuskLang JavaScript SDK supports 5 database adapters:

- **SQLite** - Lightweight, file-based database
- **PostgreSQL** - Powerful, open-source relational database
- **MySQL** - Popular relational database
- **MongoDB** - NoSQL document database
- **Redis** - In-memory key-value store

## 🚀 Quick Database Setup

### Step 1: Install Database Drivers

```bash
# Install all database drivers
npm install sqlite3 pg mysql2 mongodb redis

# Or install specific drivers
npm install sqlite3    # SQLite
npm install pg         # PostgreSQL
npm install mysql2     # MySQL
npm install mongodb    # MongoDB
npm install redis      # Redis
```

### Step 2: Create Database Configuration

```javascript
const TuskLang = require('tusklang');
const SQLiteAdapter = require('tusklang/adapters/sqlite');
const PostgreSQLAdapter = require('tusklang/adapters/postgresql');
const MySQLAdapter = require('tusklang/adapters/mysql');
const MongoDBAdapter = require('tusklang/adapters/mongodb');
const RedisAdapter = require('tusklang/adapters/redis');

// Create database adapters
const sqlite = new SQLiteAdapter({ filename: './app.db' });
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

// Configuration with database queries
const config = TuskLang.parse(`
app {
    name: "MyApp"
    version: "1.0.0"
}

database {
    host: "localhost"
    port: 5432
    name: "myapp"
    
    # Database queries in configuration!
    user_count: @query("SELECT COUNT(*) FROM users")
    active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
    recent_orders: @query("SELECT * FROM orders WHERE created_at > ?", @date.subtract("7d"))
    popular_products: @query("SELECT * FROM products ORDER BY sales DESC LIMIT 10")
}
`);

// Execute and get results
async function startApp() {
    try {
        const result = await tsk.parse(config);
        
        console.log(`📊 Total users: ${result.database.user_count}`);
        console.log(`👥 Active users: ${result.database.active_users}`);
        console.log(`📦 Recent orders: ${result.database.recent_orders.length}`);
        console.log(`🔥 Popular products: ${result.database.popular_products.length}`);
        
    } catch (error) {
        console.error('❌ Database error:', error.message);
    }
}

startApp();
```

## 📊 SQLite Integration

### Basic SQLite Setup

```javascript
const TuskLang = require('tusklang');
const SQLiteAdapter = require('tusklang/adapters/sqlite');

// Create SQLite adapter
const sqlite = new SQLiteAdapter({ 
    filename: './app.db',
    verbose: console.log  // Optional: enable SQL logging
});

// Create enhanced instance
const tsk = new TuskLang.Enhanced();
tsk.setDatabaseAdapter(sqlite);

// Configuration with SQLite queries
const config = TuskLang.parse(`
app {
    name: "MyApp"
    version: "1.0.0"
}

database {
    type: "sqlite"
    file: "./app.db"
    
    # SQLite queries
    user_count: @query("SELECT COUNT(*) FROM users")
    active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
    recent_posts: @query("SELECT * FROM posts WHERE created_at > datetime('now', '-7 days')")
    
    # Complex queries with joins
    user_posts: @query("""
        SELECT u.name, COUNT(p.id) as post_count 
        FROM users u 
        LEFT JOIN posts p ON u.id = p.user_id 
        GROUP BY u.id, u.name
    """)
    
    # Parameterized queries
    user_by_id: @query("SELECT * FROM users WHERE id = ?", @request.user_id)
    posts_by_user: @query("SELECT * FROM posts WHERE user_id = ? ORDER BY created_at DESC", @request.user_id)
}
`);

// Execute with request context
async function loadUserData(userId) {
    const context = { request: { user_id: userId } };
    const result = await tsk.parse(config, context);
    
    console.log(`User: ${result.database.user_by_id.name}`);
    console.log(`Posts: ${result.database.posts_by_user.length}`);
}
```

### SQLite Schema Creation

```javascript
// Create database schema
const schema = `
CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    email TEXT UNIQUE NOT NULL,
    active INTEGER DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS posts (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL,
    title TEXT NOT NULL,
    content TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users (id)
);

CREATE TABLE IF NOT EXISTS products (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    price REAL NOT NULL,
    sales INTEGER DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);
`;

// Initialize database
async function initDatabase() {
    await sqlite.exec(schema);
    
    // Insert sample data
    await sqlite.exec(`
        INSERT OR IGNORE INTO users (name, email) VALUES 
        ('John Doe', 'john@example.com'),
        ('Jane Smith', 'jane@example.com'),
        ('Bob Johnson', 'bob@example.com')
    `);
    
    await sqlite.exec(`
        INSERT OR IGNORE INTO posts (user_id, title, content) VALUES 
        (1, 'First Post', 'Hello World!'),
        (1, 'Second Post', 'Another post'),
        (2, 'Jane Post', 'Jane says hello')
    `);
    
    await sqlite.exec(`
        INSERT OR IGNORE INTO products (name, price, sales) VALUES 
        ('Product A', 29.99, 150),
        ('Product B', 49.99, 75),
        ('Product C', 19.99, 300)
    `);
}

initDatabase();
```

## 🐘 PostgreSQL Integration

### PostgreSQL Setup

```javascript
const TuskLang = require('tusklang');
const PostgreSQLAdapter = require('tusklang/adapters/postgresql');

// Create PostgreSQL adapter
const postgres = new PostgreSQLAdapter({
    host: 'localhost',
    port: 5432,
    database: 'myapp',
    user: 'postgres',
    password: process.env.DB_PASSWORD,
    ssl: process.env.NODE_ENV === 'production' ? { rejectUnauthorized: false } : false,
    max: 20, // Connection pool size
    idleTimeoutMillis: 30000,
    connectionTimeoutMillis: 2000
});

// Create enhanced instance
const tsk = new TuskLang.Enhanced();
tsk.setDatabaseAdapter(postgres);

// Configuration with PostgreSQL queries
const config = TuskLang.parse(`
app {
    name: "MyApp"
    version: "1.0.0"
}

database {
    type: "postgresql"
    host: "localhost"
    port: 5432
    name: "myapp"
    
    # PostgreSQL queries with advanced features
    user_count: @query("SELECT COUNT(*) FROM users")
    active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
    recent_orders: @query("SELECT * FROM orders WHERE created_at > NOW() - INTERVAL '7 days'")
    
    # JSON queries (PostgreSQL specific)
    user_preferences: @query("SELECT name, preferences FROM users WHERE preferences->>'theme' = 'dark'")
    
    # Full-text search
    search_posts: @query("""
        SELECT title, content, ts_rank(to_tsvector('english', content), plainto_tsquery('english', ?)) as rank
        FROM posts 
        WHERE to_tsvector('english', content) @@ plainto_tsquery('english', ?)
        ORDER BY rank DESC
    """, @request.search_term, @request.search_term)
    
    # Window functions
    top_users: @query("""
        SELECT name, post_count, 
               ROW_NUMBER() OVER (ORDER BY post_count DESC) as rank
        FROM (
            SELECT u.name, COUNT(p.id) as post_count
            FROM users u
            LEFT JOIN posts p ON u.id = p.user_id
            GROUP BY u.id, u.name
        ) user_stats
        LIMIT 10
    """)
    
    # Complex aggregations
    monthly_stats: @query("""
        SELECT 
            DATE_TRUNC('month', created_at) as month,
            COUNT(*) as total_posts,
            COUNT(DISTINCT user_id) as active_users
        FROM posts
        WHERE created_at >= NOW() - INTERVAL '12 months'
        GROUP BY DATE_TRUNC('month', created_at)
        ORDER BY month DESC
    """)
}
`);

// Execute with search context
async function searchPosts(searchTerm) {
    const context = { request: { search_term: searchTerm } };
    const result = await tsk.parse(config, context);
    
    console.log(`Search results for "${searchTerm}":`);
    result.database.search_posts.forEach(post => {
        console.log(`- ${post.title} (rank: ${post.rank})`);
    });
}

searchPosts('hello world');
```

### PostgreSQL Schema

```sql
-- Create tables with PostgreSQL features
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    active BOOLEAN DEFAULT true,
    preferences JSONB DEFAULT '{}',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE posts (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id),
    title VARCHAR(255) NOT NULL,
    content TEXT,
    tags TEXT[],
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE orders (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id),
    amount DECIMAL(10,2) NOT NULL,
    status VARCHAR(50) DEFAULT 'pending',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for performance
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_posts_user_id ON posts(user_id);
CREATE INDEX idx_posts_created_at ON posts(created_at);
CREATE INDEX idx_orders_user_id ON orders(user_id);
CREATE INDEX idx_orders_created_at ON orders(created_at);

-- Full-text search index
CREATE INDEX idx_posts_content_fts ON posts USING GIN(to_tsvector('english', content));
```

## 🐬 MySQL Integration

### MySQL Setup

```javascript
const TuskLang = require('tusklang');
const MySQLAdapter = require('tusklang/adapters/mysql');

// Create MySQL adapter
const mysql = new MySQLAdapter({
    host: 'localhost',
    port: 3306,
    database: 'myapp',
    user: 'root',
    password: process.env.DB_PASSWORD,
    connectionLimit: 10,
    acquireTimeout: 60000,
    timeout: 60000,
    charset: 'utf8mb4'
});

// Create enhanced instance
const tsk = new TuskLang.Enhanced();
tsk.setDatabaseAdapter(mysql);

// Configuration with MySQL queries
const config = TuskLang.parse(`
app {
    name: "MyApp"
    version: "1.0.0"
}

database {
    type: "mysql"
    host: "localhost"
    port: 3306
    name: "myapp"
    
    # MySQL queries
    user_count: @query("SELECT COUNT(*) FROM users")
    active_users: @query("SELECT COUNT(*) FROM users WHERE active = 1")
    recent_orders: @query("SELECT * FROM orders WHERE created_at > DATE_SUB(NOW(), INTERVAL 7 DAY)")
    
    # MySQL-specific features
    user_stats: @query("""
        SELECT 
            u.name,
            COUNT(p.id) as post_count,
            SUM(CASE WHEN p.created_at > DATE_SUB(NOW(), INTERVAL 30 DAY) THEN 1 ELSE 0 END) as recent_posts
        FROM users u
        LEFT JOIN posts p ON u.id = p.user_id
        GROUP BY u.id, u.name
        HAVING recent_posts > 0
        ORDER BY post_count DESC
    """)
    
    # Full-text search (MySQL)
    search_posts: @query("""
        SELECT title, content, MATCH(content) AGAINST(? IN BOOLEAN MODE) as relevance
        FROM posts 
        WHERE MATCH(content) AGAINST(? IN BOOLEAN MODE)
        ORDER BY relevance DESC
    """, @request.search_term, @request.search_term)
    
    # Date functions
    daily_stats: @query("""
        SELECT 
            DATE(created_at) as date,
            COUNT(*) as total_posts,
            COUNT(DISTINCT user_id) as active_users
        FROM posts
        WHERE created_at >= DATE_SUB(NOW(), INTERVAL 30 DAY)
        GROUP BY DATE(created_at)
        ORDER BY date DESC
    """)
}
`);

// Execute with search context
async function searchPosts(searchTerm) {
    const context = { request: { search_term: searchTerm } };
    const result = await tsk.parse(config, context);
    
    console.log(`MySQL search results for "${searchTerm}":`);
    result.database.search_posts.forEach(post => {
        console.log(`- ${post.title} (relevance: ${post.relevance})`);
    });
}
```

## 🍃 MongoDB Integration

### MongoDB Setup

```javascript
const TuskLang = require('tusklang');
const MongoDBAdapter = require('tusklang/adapters/mongodb');

// Create MongoDB adapter
const mongodb = new MongoDBAdapter({
    url: 'mongodb://localhost:27017',
    database: 'myapp',
    options: {
        useNewUrlParser: true,
        useUnifiedTopology: true,
        maxPoolSize: 10
    }
});

// Create enhanced instance
const tsk = new TuskLang.Enhanced();
tsk.setDatabaseAdapter(mongodb);

// Configuration with MongoDB queries
const config = TuskLang.parse(`
app {
    name: "MyApp"
    version: "1.0.0"
}

database {
    type: "mongodb"
    url: "mongodb://localhost:27017"
    name: "myapp"
    
    # MongoDB aggregation queries
    user_count: @query("db.users.countDocuments()")
    active_users: @query("db.users.countDocuments({active: true})")
    recent_posts: @query("db.posts.find({created_at: {$gte: new Date(Date.now() - 7*24*60*60*1000)}}).toArray()")
    
    # Complex aggregations
    user_stats: @query("""
        db.posts.aggregate([
            {
                $lookup: {
                    from: "users",
                    localField: "user_id",
                    foreignField: "_id",
                    as: "user"
                }
            },
            {
                $unwind: "$user"
            },
            {
                $group: {
                    _id: "$user._id",
                    name: {$first: "$user.name"},
                    post_count: {$sum: 1},
                    recent_posts: {
                        $sum: {
                            $cond: [
                                {$gte: ["$created_at", new Date(Date.now() - 30*24*60*60*1000)]},
                                1,
                                0
                            ]
                        }
                    }
                }
            },
            {
                $match: {recent_posts: {$gt: 0}}
            },
            {
                $sort: {post_count: -1}
            }
        ]).toArray()
    """)
    
    # Text search
    search_posts: @query("""
        db.posts.find({
            $text: {$search: ?}
        }, {
            score: {$meta: "textScore"}
        }).sort({
            score: {$meta: "textScore"}
        }).toArray()
    """, @request.search_term)
    
    # Geospatial queries
    nearby_users: @query("""
        db.users.find({
            location: {
                $near: {
                    $geometry: {
                        type: "Point",
                        coordinates: [?, ?]
                    },
                    $maxDistance: 10000
                }
            }
        }).toArray()
    """, @request.longitude, @request.latitude)
}
`);

// Execute with location context
async function findNearbyUsers(longitude, latitude) {
    const context = { 
        request: { 
            longitude: longitude, 
            latitude: latitude 
        } 
    };
    const result = await tsk.parse(config, context);
    
    console.log(`Nearby users: ${result.database.nearby_users.length}`);
    result.database.nearby_users.forEach(user => {
        console.log(`- ${user.name} (${user.location.coordinates.join(', ')})`);
    });
}
```

## 🔴 Redis Integration

### Redis Setup

```javascript
const TuskLang = require('tusklang');
const RedisAdapter = require('tusklang/adapters/redis');

// Create Redis adapter
const redis = new RedisAdapter({
    host: 'localhost',
    port: 6379,
    password: process.env.REDIS_PASSWORD,
    db: 0,
    retryDelayOnFailover: 100,
    enableReadyCheck: false,
    maxRetriesPerRequest: null
});

// Create enhanced instance
const tsk = new TuskLang.Enhanced();
tsk.setCacheAdapter(redis);

// Configuration with Redis operations
const config = TuskLang.parse(`
app {
    name: "MyApp"
    version: "1.0.0"
}

cache {
    type: "redis"
    host: "localhost"
    port: 6379
    
    # Redis operations
    user_count: @redis.get("user_count")
    active_users: @redis.get("active_users")
    session_data: @redis.hgetall("session:${session_id}")
    
    # Cached database queries
    cached_user_count: @cache("5m", @query("SELECT COUNT(*) FROM users"))
    cached_popular_posts: @cache("1h", @query("SELECT * FROM posts ORDER BY views DESC LIMIT 10"))
    
    # Redis-specific operations
    online_users: @redis.scard("online_users")
    user_sessions: @redis.keys("session:*")
    
    # Hash operations
    user_profile: @redis.hgetall("user:${user_id}")
    user_settings: @redis.hget("user:${user_id}", "settings")
    
    # List operations
    recent_activities: @redis.lrange("activities", 0, 9)
    queue_length: @redis.llen("task_queue")
    
    # Set operations
    user_followers: @redis.smembers("followers:${user_id}")
    user_following: @redis.smembers("following:${user_id}")
    
    # Sorted set operations
    top_users: @redis.zrevrange("user_scores", 0, 9, "WITHSCORES")
    user_rank: @redis.zrevrank("user_scores", "${user_id}")
}
`);

// Execute with user context
async function loadUserData(userId, sessionId) {
    const context = { 
        user_id: userId, 
        session_id: sessionId 
    };
    const result = await tsk.parse(config, context);
    
    console.log(`User profile:`, result.cache.user_profile);
    console.log(`Online users: ${result.cache.online_users}`);
    console.log(`User rank: ${result.cache.user_rank}`);
}
```

## 🔄 Multi-Database Setup

### Using Multiple Databases

```javascript
const TuskLang = require('tusklang');
const PostgreSQLAdapter = require('tusklang/adapters/postgresql');
const RedisAdapter = require('tusklang/adapters/redis');

// Create multiple adapters
const postgres = new PostgreSQLAdapter({
    host: 'localhost',
    port: 5432,
    database: 'myapp',
    user: 'postgres',
    password: process.env.DB_PASSWORD
});

const redis = new RedisAdapter({
    host: 'localhost',
    port: 6379,
    password: process.env.REDIS_PASSWORD
});

// Create enhanced instance with multiple adapters
const tsk = new TuskLang.Enhanced();
tsk.setDatabaseAdapter(postgres);
tsk.setCacheAdapter(redis);

// Configuration using multiple databases
const config = TuskLang.parse(`
app {
    name: "MyApp"
    version: "1.0.0"
}

# PostgreSQL for persistent data
database {
    type: "postgresql"
    host: "localhost"
    port: 5432
    
    # Complex queries
    user_count: @query("SELECT COUNT(*) FROM users")
    active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
    recent_orders: @query("SELECT * FROM orders WHERE created_at > NOW() - INTERVAL '7 days'")
    
    # Cached queries
    cached_user_stats: @cache("5m", @query("""
        SELECT 
            u.name,
            COUNT(p.id) as post_count,
            SUM(o.amount) as total_spent
        FROM users u
        LEFT JOIN posts p ON u.id = p.user_id
        LEFT JOIN orders o ON u.id = o.user_id
        GROUP BY u.id, u.name
        ORDER BY total_spent DESC
        LIMIT 10
    """))
}

# Redis for caching and sessions
cache {
    type: "redis"
    host: "localhost"
    port: 6379
    
    # Session data
    session: @redis.hgetall("session:${session_id}")
    user_sessions: @redis.keys("session:*")
    
    # Real-time data
    online_users: @redis.scard("online_users")
    active_sessions: @redis.scard("active_sessions")
    
    # Cached database results
    cached_user_count: @cache("1m", @query("SELECT COUNT(*) FROM users"))
    cached_popular_posts: @cache("30m", @query("SELECT * FROM posts ORDER BY views DESC LIMIT 20"))
    
    # User activity
    user_activities: @redis.lrange("activities:${user_id}", 0, 19)
    recent_notifications: @redis.lrange("notifications:${user_id}", 0, 9)
}

# Combined operations
analytics {
    # Database + Cache combination
    total_users: @query("SELECT COUNT(*) FROM users")
    online_users: @redis.scard("online_users")
    active_sessions: @redis.scard("active_sessions")
    
    # Cached analytics
    user_engagement: @cache("15m", @query("""
        SELECT 
            DATE_TRUNC('hour', created_at) as hour,
            COUNT(*) as posts,
            COUNT(DISTINCT user_id) as active_users
        FROM posts
        WHERE created_at >= NOW() - INTERVAL '24 hours'
        GROUP BY DATE_TRUNC('hour', created_at)
        ORDER BY hour DESC
    """))
    
    # Real-time metrics
    current_load: @redis.get("system:current_load")
    error_rate: @redis.get("system:error_rate")
    response_time: @redis.get("system:avg_response_time")
}
`);

// Execute with session context
async function loadDashboard(sessionId, userId) {
    const context = { 
        session_id: sessionId, 
        user_id: userId 
    };
    
    try {
        const result = await tsk.parse(config, context);
        
        console.log(`📊 Dashboard for ${result.cache.session.username}:`);
        console.log(`👥 Total users: ${result.database.user_count}`);
        console.log(`🟢 Online users: ${result.cache.online_users}`);
        console.log(`📈 Active sessions: ${result.cache.active_sessions}`);
        console.log(`💳 Recent orders: ${result.database.recent_orders.length}`);
        console.log(`📝 Recent activities: ${result.cache.user_activities.length}`);
        console.log(`🔔 Notifications: ${result.cache.recent_notifications.length}`);
        console.log(`⚡ System load: ${result.analytics.current_load}`);
        
    } catch (error) {
        console.error('❌ Dashboard error:', error.message);
    }
}
```

## 🎯 @ Operator Database Features

### Advanced @ Operators

```javascript
const config = TuskLang.parse(`
app {
    name: "MyApp"
    version: "1.0.0"
}

database {
    # Basic queries
    user_count: @query("SELECT COUNT(*) FROM users")
    active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
    
    # Parameterized queries
    user_by_id: @query("SELECT * FROM users WHERE id = ?", @request.user_id)
    posts_by_user: @query("SELECT * FROM posts WHERE user_id = ? ORDER BY created_at DESC", @request.user_id)
    
    # Date-based queries
    recent_posts: @query("SELECT * FROM posts WHERE created_at > ?", @date.subtract("7d"))
    today_orders: @query("SELECT * FROM orders WHERE DATE(created_at) = ?", @date.today())
    
    # Cached queries
    cached_user_stats: @cache("5m", @query("SELECT COUNT(*) FROM users"))
    cached_popular_posts: @cache("1h", @query("SELECT * FROM posts ORDER BY views DESC LIMIT 10"))
    
    # Conditional queries
    user_data: @if(@request.user_id, 
        @query("SELECT * FROM users WHERE id = ?", @request.user_id),
        null
    )
    
    # Complex queries with multiple parameters
    search_posts: @query("""
        SELECT p.*, u.name as author_name
        FROM posts p
        JOIN users u ON p.user_id = u.id
        WHERE p.title LIKE ? OR p.content LIKE ?
        AND p.created_at > ?
        ORDER BY p.created_at DESC
        LIMIT ?
    """, 
        "%" + @request.search_term + "%",
        "%" + @request.search_term + "%",
        @date.subtract("30d"),
        @request.limit || 20
    )
    
    # Aggregation queries
    monthly_stats: @query("""
        SELECT 
            DATE_FORMAT(created_at, '%Y-%m') as month,
            COUNT(*) as total_posts,
            COUNT(DISTINCT user_id) as active_users,
            AVG(LENGTH(content)) as avg_content_length
        FROM posts
        WHERE created_at >= DATE_SUB(NOW(), INTERVAL 12 MONTH)
        GROUP BY DATE_FORMAT(created_at, '%Y-%m')
        ORDER BY month DESC
    """)
    
    # Transaction-like operations
    user_creation: @query("""
        INSERT INTO users (name, email, created_at) 
        VALUES (?, ?, NOW())
    """, @request.user_name, @request.user_email)
    
    # Update operations
    update_user_status: @query("""
        UPDATE users 
        SET active = ?, updated_at = NOW() 
        WHERE id = ?
    """, @request.active, @request.user_id)
}

# Redis operations with @ operators
cache {
    # Basic operations
    user_session: @redis.hgetall("session:${session_id}")
    user_preferences: @redis.hget("user:${user_id}", "preferences")
    
    # List operations
    user_activities: @redis.lrange("activities:${user_id}", 0, @request.limit || 10)
    
    # Set operations
    user_followers: @redis.smembers("followers:${user_id}")
    is_following: @redis.sismember("following:${user_id}", @request.target_user_id)
    
    # Sorted set operations
    user_rank: @redis.zrevrank("user_scores", "${user_id}")
    top_users: @redis.zrevrange("user_scores", 0, @request.limit || 10, "WITHSCORES")
    
    # Conditional cache operations
    cached_data: @if(@redis.exists("cache:${cache_key}"),
        @redis.get("cache:${cache_key}"),
        @cache("5m", @query("SELECT * FROM data WHERE key = ?", @request.data_key))
    )
}
`);
```

## 🔒 Security Best Practices

### Secure Database Configuration

```javascript
const config = TuskLang.parse(`
database {
    # Use environment variables for sensitive data
    host: @env("DB_HOST", "localhost")
    port: @env("DB_PORT", "5432")
    name: @env("DB_NAME", "myapp")
    user: @env("DB_USER", "postgres")
    password: @env.secure("DB_PASSWORD")  # Secure environment variable
    
    # Use parameterized queries to prevent SQL injection
    user_by_id: @query("SELECT * FROM users WHERE id = ?", @request.user_id)
    search_posts: @query("SELECT * FROM posts WHERE title LIKE ?", "%" + @request.search_term + "%")
    
    # Validate input with @ operators
    safe_user_id: @validate.integer(@request.user_id, 1, 1000000)
    safe_search_term: @validate.string(@request.search_term, 1, 100)
    
    # Use connection pooling
    pool_size: @if(@env("NODE_ENV") == "production", 20, 5)
    connection_timeout: 5000
    idle_timeout: 30000
}

security {
    # Rate limiting for database queries
    max_queries_per_minute: @if(@env("NODE_ENV") == "production", 1000, 10000)
    
    # Query timeout
    query_timeout: @if(@env("NODE_ENV") == "production", 5000, 30000)
    
    # SSL for production
    ssl: @if(@env("NODE_ENV") == "production", true, false)
    
    # Audit logging
    audit_queries: @if(@env("NODE_ENV") == "production", true, false)
}
`);
```

## 📚 Next Steps

1. **[Advanced Features](005-advanced-features-javascript.md)** - Master @ operators and FUJSEN
2. **[Framework Integration](006-framework-integration-javascript.md)** - Use with Express.js, Next.js, and more
3. **[Production Deployment](007-production-deployment-javascript.md)** - Deploy with confidence
4. **[Performance Optimization](008-performance-optimization-javascript.md)** - Optimize database queries

## 🎉 You've Mastered Database Integration!

You now understand:
- ✅ **5 Database Adapters** - SQLite, PostgreSQL, MySQL, MongoDB, Redis
- ✅ **@ Query Operators** - Direct database queries in configuration
- ✅ **Parameterized Queries** - Secure, injection-proof database access
- ✅ **Caching Integration** - Redis caching with database queries
- ✅ **Multi-Database Setup** - Use multiple databases simultaneously
- ✅ **Security Best Practices** - Secure database configuration
- ✅ **Complex Queries** - Aggregations, joins, and advanced features

**Ready to master @ operators and FUJSEN? Let's dive into advanced features!** 