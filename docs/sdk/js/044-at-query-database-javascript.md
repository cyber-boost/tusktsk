# @query Database - JavaScript

## Overview

The `@query` function in TuskLang enables direct database queries within your configuration files. This revolutionary feature allows JavaScript applications to fetch real-time data from databases and use it directly in configuration, eliminating the need for separate database connection logic.

## Basic Syntax

```tsk
# Simple database queries
user_count: @query("SELECT COUNT(*) as count FROM users")
active_users: @query("SELECT COUNT(*) as count FROM users WHERE status = 'active'")

# Query with parameters
user_by_id: @query("SELECT * FROM users WHERE id = ?", [123])
users_by_role: @query("SELECT * FROM users WHERE role = ?", ["admin"])
```

## JavaScript Integration

### Node.js Database Integration

```javascript
const tusk = require('tusklang');

// Load configuration with database queries
const config = tusk.load('database.tsk');

// Access query results directly
console.log(config.user_count); // { count: 150 }
console.log(config.active_users); // { count: 120 }

// Use in application logic
const totalUsers = config.user_count.count;
const activeUserCount = config.active_users.count;

// Dynamic queries with parameters
const userData = await tusk.query("SELECT * FROM users WHERE id = ?", [userId]);
const adminUsers = await tusk.query("SELECT * FROM users WHERE role = ?", ["admin"]);
```

### Browser Database Integration

```javascript
// Load TuskLang configuration
const config = await tusk.load('database.tsk');

// Use query results in frontend
class UserDashboard {
  constructor() {
    this.userCount = config.user_count.count;
    this.activeUsers = config.active_users.count;
  }
  
  updateStats() {
    document.getElementById('total-users').textContent = this.userCount;
    document.getElementById('active-users').textContent = this.activeUsers;
  }
  
  async getUserProfile(userId) {
    const userData = await tusk.query("SELECT * FROM users WHERE id = ?", [userId]);
    return userData[0];
  }
}
```

## Advanced Usage

### Complex Queries

```tsk
# Complex queries with joins
user_posts: @query("""
  SELECT u.name, p.title, p.created_at 
  FROM users u 
  JOIN posts p ON u.id = p.user_id 
  WHERE u.status = 'active' 
  ORDER BY p.created_at DESC 
  LIMIT 10
""")

# Aggregation queries
user_stats: @query("""
  SELECT 
    COUNT(*) as total_users,
    COUNT(CASE WHEN status = 'active' THEN 1 END) as active_users,
    COUNT(CASE WHEN created_at >= DATE_SUB(NOW(), INTERVAL 30 DAY) THEN 1 END) as new_users
  FROM users
""")
```

### Parameterized Queries

```tsk
# Queries with dynamic parameters
user_profile: @query("SELECT * FROM users WHERE id = ? AND status = ?", [$user_id, "active"])
user_posts_by_category: @query("""
  SELECT p.*, c.name as category_name 
  FROM posts p 
  JOIN categories c ON p.category_id = c.id 
  WHERE p.user_id = ? AND c.name = ?
""", [$user_id, $category_name])
```

### Transaction Queries

```tsk
# Transaction-based queries
user_transaction: @query("""
  BEGIN;
  UPDATE users SET last_login = NOW() WHERE id = ?;
  INSERT INTO login_logs (user_id, login_time) VALUES (?, NOW());
  COMMIT;
""", [$user_id, $user_id])
```

## JavaScript Implementation

### Database Connection Manager

```javascript
class TuskDatabaseManager {
  constructor() {
    this.connections = new Map();
    this.queryCache = new Map();
  }
  
  async connect(databaseType, connectionString) {
    const connectionKey = `${databaseType}_${connectionString}`;
    
    if (!this.connections.has(connectionKey)) {
      let connection;
      
      switch (databaseType) {
        case 'mysql':
          connection = await mysql.createConnection(connectionString);
          break;
        case 'postgresql':
          connection = await pg.connect(connectionString);
          break;
        case 'sqlite':
          connection = await sqlite3.open(connectionString);
          break;
        default:
          throw new Error(`Unsupported database type: ${databaseType}`);
      }
      
      this.connections.set(connectionKey, connection);
    }
    
    return this.connections.get(connectionKey);
  }
  
  async query(sql, params = []) {
    const cacheKey = `${sql}_${JSON.stringify(params)}`;
    
    // Check cache first
    if (this.queryCache.has(cacheKey)) {
      const cached = this.queryCache.get(cacheKey);
      if (Date.now() - cached.timestamp < 300000) { // 5 minutes
        return cached.result;
      }
    }
    
    // Execute query
    const connection = await this.getDefaultConnection();
    const result = await connection.query(sql, params);
    
    // Cache result
    this.queryCache.set(cacheKey, {
      result: result.rows || result,
      timestamp: Date.now()
    });
    
    return result.rows || result;
  }
  
  async getDefaultConnection() {
    // Get default connection from configuration
    const config = await tusk.load('database.tsk');
    return await this.connect(config.database_type, config.connection_string);
  }
}
```

### TypeScript Support

```typescript
interface DatabaseConfig {
  database_type: 'mysql' | 'postgresql' | 'sqlite';
  connection_string: string;
  user_count: { count: number };
  active_users: { count: number };
}

interface User {
  id: number;
  name: string;
  email: string;
  status: string;
  created_at: Date;
}

class TypedDatabaseManager {
  private dbManager: TuskDatabaseManager;
  
  constructor() {
    this.dbManager = new TuskDatabaseManager();
  }
  
  async getUserById(id: number): Promise<User | null> {
    const result = await this.dbManager.query(
      "SELECT * FROM users WHERE id = ?",
      [id]
    );
    return result[0] || null;
  }
  
  async getUsersByRole(role: string): Promise<User[]> {
    return await this.dbManager.query(
      "SELECT * FROM users WHERE role = ?",
      [role]
    );
  }
  
  async getConfig(): Promise<DatabaseConfig> {
    return await tusk.load('database.tsk');
  }
}
```

## Real-World Examples

### E-commerce Application

```tsk
# Product catalog queries
featured_products: @query("""
  SELECT p.*, c.name as category_name, AVG(r.rating) as avg_rating
  FROM products p
  JOIN categories c ON p.category_id = c.id
  LEFT JOIN reviews r ON p.id = r.product_id
  WHERE p.featured = true AND p.stock > 0
  GROUP BY p.id
  ORDER BY avg_rating DESC
  LIMIT 10
""")

# User order history
user_orders: @query("""
  SELECT o.*, p.name as product_name, p.price
  FROM orders o
  JOIN order_items oi ON o.id = oi.order_id
  JOIN products p ON oi.product_id = p.id
  WHERE o.user_id = ?
  ORDER BY o.created_at DESC
""", [$user_id])
```

### Content Management System

```tsk
# Article queries
recent_articles: @query("""
  SELECT a.*, u.name as author_name, c.name as category_name
  FROM articles a
  JOIN users u ON a.author_id = u.id
  JOIN categories c ON a.category_id = c.id
  WHERE a.status = 'published'
  ORDER BY a.published_at DESC
  LIMIT 20
""")

# Article statistics
article_stats: @query("""
  SELECT 
    COUNT(*) as total_articles,
    COUNT(CASE WHEN status = 'published' THEN 1 END) as published_articles,
    COUNT(CASE WHEN status = 'draft' THEN 1 END) as draft_articles,
    AVG(view_count) as avg_views
  FROM articles
""")
```

### Analytics Dashboard

```tsk
# User analytics
user_analytics: @query("""
  SELECT 
    DATE(created_at) as date,
    COUNT(*) as new_users,
    COUNT(CASE WHEN status = 'active' THEN 1 END) as active_users
  FROM users
  WHERE created_at >= DATE_SUB(NOW(), INTERVAL 30 DAY)
  GROUP BY DATE(created_at)
  ORDER BY date DESC
""")

# Revenue analytics
revenue_analytics: @query("""
  SELECT 
    DATE(created_at) as date,
    SUM(total_amount) as daily_revenue,
    COUNT(*) as order_count,
    AVG(total_amount) as avg_order_value
  FROM orders
  WHERE status = 'completed'
  AND created_at >= DATE_SUB(NOW(), INTERVAL 30 DAY)
  GROUP BY DATE(created_at)
  ORDER BY date DESC
""")
```

## Performance Considerations

### Query Caching

```tsk
# Cache expensive queries
cached_user_count: @cache("10m", @query("SELECT COUNT(*) as count FROM users"))
cached_analytics: @cache("1h", @query("""
  SELECT 
    COUNT(*) as total_users,
    COUNT(CASE WHEN status = 'active' THEN 1 END) as active_users
  FROM users
"""))
```

### Connection Pooling

```javascript
// Implement connection pooling for better performance
class ConnectionPool {
  constructor(maxConnections = 10) {
    this.pool = [];
    this.maxConnections = maxConnections;
    this.activeConnections = 0;
  }
  
  async getConnection() {
    if (this.pool.length > 0) {
      return this.pool.pop();
    }
    
    if (this.activeConnections < this.maxConnections) {
      this.activeConnections++;
      return await this.createConnection();
    }
    
    // Wait for available connection
    return new Promise(resolve => {
      const checkPool = () => {
        if (this.pool.length > 0) {
          resolve(this.pool.pop());
        } else {
          setTimeout(checkPool, 100);
        }
      };
      checkPool();
    });
  }
  
  releaseConnection(connection) {
    this.pool.push(connection);
  }
}
```

## Security Notes

- **SQL Injection**: Always use parameterized queries
- **Connection Security**: Use encrypted connections
- **Query Limits**: Implement query timeouts and limits
- **Access Control**: Restrict database access permissions

```javascript
// Secure query execution
class SecureQueryExecutor {
  constructor() {
    this.maxQueryTime = 30000; // 30 seconds
    this.maxResults = 1000;
  }
  
  async executeQuery(sql, params = []) {
    // Validate SQL
    if (!this.isValidQuery(sql)) {
      throw new Error('Invalid query detected');
    }
    
    // Set timeout
    const timeoutPromise = new Promise((_, reject) => {
      setTimeout(() => reject(new Error('Query timeout')), this.maxQueryTime);
    });
    
    // Execute query with timeout
    const queryPromise = this.dbManager.query(sql, params);
    
    try {
      const result = await Promise.race([queryPromise, timeoutPromise]);
      
      // Limit results
      if (Array.isArray(result) && result.length > this.maxResults) {
        return result.slice(0, this.maxResults);
      }
      
      return result;
    } catch (error) {
      console.error('Query execution error:', error);
      throw error;
    }
  }
  
  isValidQuery(sql) {
    // Basic SQL validation
    const dangerousKeywords = ['DROP', 'DELETE', 'TRUNCATE', 'ALTER', 'CREATE'];
    const upperSql = sql.toUpperCase();
    
    return !dangerousKeywords.some(keyword => upperSql.includes(keyword));
  }
}
```

## Best Practices

1. **Parameterized Queries**: Always use parameters to prevent SQL injection
2. **Connection Management**: Implement proper connection pooling
3. **Query Optimization**: Use indexes and optimize complex queries
4. **Error Handling**: Implement comprehensive error handling
5. **Caching**: Cache frequently accessed data
6. **Monitoring**: Monitor query performance and database health

## Next Steps

- Learn about [@tusk object](./045-at-tusk-object-javascript.md) for advanced operations
- Explore [@cache function](./046-at-cache-function-javascript.md) for caching strategies
- Master [@metrics function](./047-at-metrics-function-javascript.md) for performance monitoring 