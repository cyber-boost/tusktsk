# 🟨 TuskLang JavaScript Performance Optimization Guide

**"We don't bow to any king" - JavaScript Edition**

Optimize your TuskLang-powered JavaScript applications for maximum performance. Learn caching strategies, query optimization, memory management, and scaling techniques.

## ⚡ Caching Strategies

### Multi-Level Caching

```javascript
// config/caching.tsk
const cachingConfig = `
caching {
    # L1: Memory cache (fastest)
    memory {
        enabled: true
        max_size: "100MB"
        ttl: "5m"
        eviction_policy: "lru"
        
        # Cache frequently accessed data
        user_profiles: @cache.memory("10m", @query("SELECT * FROM users WHERE id = ?", @request.user.id))
        app_config: @cache.memory("1h", @file.read("config/app.json"))
        api_keys: @cache.memory("30m", @env.secure("API_KEY"))
    }
    
    # L2: Redis cache (distributed)
    redis {
        enabled: true
        host: @env("REDIS_HOST", "localhost")
        port: @env("REDIS_PORT", "6379")
        password: @env.secure("REDIS_PASSWORD")
        
        # Cache settings
        default_ttl: "30m"
        max_memory: "1GB"
        max_memory_policy: "allkeys-lru"
        
        # Cache categories
        user_sessions: @cache.redis("2h", @session.data)
        query_results: @cache.redis("5m", @query("SELECT * FROM posts WHERE user_id = ?", @request.user.id))
        api_responses: @cache.redis("1m", @http.get("https://api.example.com/data"))
    }
    
    # L3: Database query cache
    database {
        enabled: true
        query_cache_ttl: "10m"
        max_cached_queries: 1000
        
        # Cache expensive queries
        user_stats: @cache.query("30m", @query("""
            SELECT 
                u.id,
                COUNT(p.id) as post_count,
                COUNT(o.id) as order_count,
                SUM(o.amount) as total_spent
            FROM users u
            LEFT JOIN posts p ON u.id = p.user_id
            LEFT JOIN orders o ON u.id = o.user_id
            WHERE u.id = ?
            GROUP BY u.id
        """, @request.user.id))
    }
    
    # Cache invalidation strategies
    invalidation {
        # Automatic invalidation on data changes
        on_user_update: @cache.invalidate("user_profiles", @request.user.id)
        on_post_create: @cache.invalidate("user_stats", @request.user.id)
        on_order_complete: @cache.invalidate("user_stats", @request.user.id)
        
        # Scheduled cache warming
        warmup_schedule: [
            @cache.warm("user_profiles", "0 */6 * * *"),  # Every 6 hours
            @cache.warm("app_config", "0 0 * * *"),       # Daily at midnight
            @cache.warm("api_keys", "0 */2 * * *")        # Every 2 hours
        ]
    }
}
`;

// Advanced caching implementation
class TuskLangCache {
    constructor() {
        this.memoryCache = new Map();
        this.redisClient = require('redis').createClient({
            host: process.env.REDIS_HOST,
            port: process.env.REDIS_PORT,
            password: process.env.REDIS_PASSWORD
        });
    }

    async get(key, level = 'memory') {
        try {
            // L1: Memory cache
            if (level === 'memory' && this.memoryCache.has(key)) {
                const cached = this.memoryCache.get(key);
                if (cached.expires > Date.now()) {
                    return cached.value;
                }
                this.memoryCache.delete(key);
            }

            // L2: Redis cache
            if (level === 'redis') {
                const cached = await this.redisClient.get(key);
                if (cached) {
                    const parsed = JSON.parse(cached);
                    if (parsed.expires > Date.now()) {
                        // Update memory cache
                        this.memoryCache.set(key, parsed);
                        return parsed.value;
                    }
                }
            }

            return null;
        } catch (error) {
            console.error('Cache get error:', error);
            return null;
        }
    }

    async set(key, value, ttl = '5m', level = 'memory') {
        try {
            const expires = Date.now() + this.parseTTL(ttl);
            const cacheData = { value, expires };

            // L1: Memory cache
            if (level === 'memory') {
                this.memoryCache.set(key, cacheData);
                
                // Implement LRU eviction
                if (this.memoryCache.size > 1000) {
                    const firstKey = this.memoryCache.keys().next().value;
                    this.memoryCache.delete(firstKey);
                }
            }

            // L2: Redis cache
            if (level === 'redis') {
                await this.redisClient.setex(key, this.parseTTL(ttl) / 1000, JSON.stringify(cacheData));
            }

            return true;
        } catch (error) {
            console.error('Cache set error:', error);
            return false;
        }
    }

    parseTTL(ttl) {
        const units = {
            's': 1000,
            'm': 60 * 1000,
            'h': 60 * 60 * 1000,
            'd': 24 * 60 * 60 * 1000
        };
        
        const match = ttl.match(/^(\d+)([smhd])$/);
        if (match) {
            return parseInt(match[1]) * units[match[2]];
        }
        return 5 * 60 * 1000; // Default 5 minutes
    }
}
```

### Intelligent Cache Warming

```javascript
// cache-warming.js
const TuskLang = require('tusklang');

class CacheWarmer {
    constructor(tsk) {
        this.tsk = tsk;
        this.cache = new TuskLangCache();
    }

    async warmUserProfiles() {
        try {
            const activeUsers = await this.tsk.parse(TuskLang.parse(`
                active_users: @query("""
                    SELECT id FROM users 
                    WHERE last_active > NOW() - INTERVAL '1 day'
                    ORDER BY last_active DESC
                    LIMIT 100
                """)
            `));

            for (const user of activeUsers.active_users) {
                const userProfile = await this.tsk.parse(TuskLang.parse(`
                    user_profile {
                        profile: @query("SELECT * FROM users WHERE id = ?", ${user.id})
                        stats: @query("""
                            SELECT 
                                COUNT(p.id) as post_count,
                                COUNT(o.id) as order_count,
                                SUM(o.amount) as total_spent
                            FROM users u
                            LEFT JOIN posts p ON u.id = p.user_id
                            LEFT JOIN orders o ON u.id = o.user_id
                            WHERE u.id = ?
                            GROUP BY u.id
                        """, ${user.id})
                    }
                `));

                await this.cache.set(`user_profile_${user.id}`, userProfile, '30m', 'redis');
            }

            console.log(`Warmed ${activeUsers.active_users.length} user profiles`);
        } catch (error) {
            console.error('Cache warming error:', error);
        }
    }

    async warmAppConfig() {
        try {
            const config = await this.tsk.parse(TuskLang.parse(`
                app_config {
                    settings: @file.read("config/app.json")
                    features: @file.read("config/features.json")
                    api_keys: @env.secure("API_KEY")
                }
            `));

            await this.cache.set('app_config', config, '1h', 'redis');
            console.log('App config warmed');
        } catch (error) {
            console.error('Config warming error:', error);
        }
    }

    async warmFrequentlyAccessedData() {
        try {
            const frequentData = await this.tsk.parse(TuskLang.parse(`
                frequent_data {
                    # Most popular posts
                    popular_posts: @query("""
                        SELECT p.*, u.name as author_name, COUNT(l.id) as like_count
                        FROM posts p
                        JOIN users u ON p.user_id = u.id
                        LEFT JOIN likes l ON p.id = l.post_id
                        WHERE p.created_at > NOW() - INTERVAL '7 days'
                        GROUP BY p.id, u.name
                        ORDER BY like_count DESC
                        LIMIT 20
                    """)
                    
                    # Recent activity
                    recent_activity: @query("""
                        SELECT 'post' as type, p.title as content, p.created_at, u.name as user_name
                        FROM posts p
                        JOIN users u ON p.user_id = u.id
                        WHERE p.created_at > NOW() - INTERVAL '1 day'
                        UNION ALL
                        SELECT 'order' as type, CONCAT('Order #', o.id) as content, o.created_at, u.name as user_name
                        FROM orders o
                        JOIN users u ON o.user_id = u.id
                        WHERE o.created_at > NOW() - INTERVAL '1 day'
                        ORDER BY created_at DESC
                        LIMIT 50
                    """)
                    
                    # System statistics
                    system_stats: @query("""
                        SELECT 
                            COUNT(*) as total_users,
                            COUNT(CASE WHEN active = true THEN 1 END) as active_users,
                            COUNT(CASE WHEN created_at > NOW() - INTERVAL '7 days' THEN 1 END) as new_users_week
                        FROM users
                    """)
                }
            `));

            await this.cache.set('frequent_data', frequentData, '5m', 'redis');
            console.log('Frequent data warmed');
        } catch (error) {
            console.error('Frequent data warming error:', error);
        }
    }
}
```

## 🔍 Query Optimization

### Database Query Optimization

```javascript
// query-optimization.js
const TuskLang = require('tusklang');

class QueryOptimizer {
    constructor(tsk) {
        this.tsk = tsk;
    }

    // Optimized user dashboard query
    async getOptimizedUserDashboard(userId) {
        return await this.tsk.parse(TuskLang.parse(`
            user_dashboard {
                # Use indexed columns and limit results
                profile: @query("""
                    SELECT id, name, email, created_at, last_active
                    FROM users 
                    WHERE id = ?
                    LIMIT 1
                """, ${userId})
                
                # Optimize with proper joins and indexing
                recent_posts: @query("""
                    SELECT p.id, p.title, p.content, p.created_at, COUNT(l.id) as like_count
                    FROM posts p
                    LEFT JOIN likes l ON p.id = l.post_id
                    WHERE p.user_id = ?
                    GROUP BY p.id, p.title, p.content, p.created_at
                    ORDER BY p.created_at DESC
                    LIMIT 10
                """, ${userId})
                
                # Use aggregation for statistics
                stats: @query("""
                    SELECT 
                        COUNT(p.id) as post_count,
                        COUNT(o.id) as order_count,
                        COALESCE(SUM(o.amount), 0) as total_spent,
                        MAX(p.created_at) as last_post,
                        MAX(o.created_at) as last_order
                    FROM users u
                    LEFT JOIN posts p ON u.id = p.user_id
                    LEFT JOIN orders o ON u.id = o.user_id AND o.status = 'completed'
                    WHERE u.id = ?
                    GROUP BY u.id
                """, ${userId})
                
                # Paginated activity feed
                activity_feed: @query("""
                    SELECT 
                        'post' as type,
                        p.id,
                        p.title as content,
                        p.created_at,
                        'Post created' as action
                    FROM posts p
                    WHERE p.user_id = ?
                    
                    UNION ALL
                    
                    SELECT 
                        'order' as type,
                        o.id,
                        CONCAT('Order #', o.id) as content,
                        o.created_at,
                        'Order placed' as action
                    FROM orders o
                    WHERE o.user_id = ?
                    
                    ORDER BY created_at DESC
                    LIMIT 20
                """, ${userId}, ${userId})
            }
        `));
    }

    // Optimized search with full-text search
    async getOptimizedSearch(query, page = 1, limit = 20) {
        const offset = (page - 1) * limit;
        
        return await this.tsk.parse(TuskLang.parse(`
            search_results {
                # Use full-text search for better performance
                posts: @query("""
                    SELECT 
                        p.id,
                        p.title,
                        p.content,
                        p.created_at,
                        u.name as author_name,
                        ts_rank(p.search_vector, plainto_tsquery('english', ?)) as rank
                    FROM posts p
                    JOIN users u ON p.user_id = u.id
                    WHERE p.search_vector @@ plainto_tsquery('english', ?)
                    ORDER BY rank DESC, p.created_at DESC
                    LIMIT ? OFFSET ?
                """, "${query}", "${query}", ${limit}, ${offset})
                
                # Total count for pagination
                total_count: @query("""
                    SELECT COUNT(*) as count
                    FROM posts p
                    WHERE p.search_vector @@ plainto_tsquery('english', ?)
                """, "${query}")
                
                # Search suggestions
                suggestions: @query("""
                    SELECT DISTINCT word
                    FROM ts_stat('SELECT search_vector FROM posts')
                    WHERE word ILIKE ?
                    ORDER BY nentry DESC
                    LIMIT 5
                """, "${query}%")
            }
        `));
    }

    // Optimized analytics queries
    async getOptimizedAnalytics(startDate, endDate) {
        return await this.tsk.parse(TuskLang.parse(`
            analytics {
                # Daily user registrations
                daily_registrations: @query("""
                    SELECT 
                        DATE(created_at) as date,
                        COUNT(*) as registrations
                    FROM users
                    WHERE created_at BETWEEN ? AND ?
                    GROUP BY DATE(created_at)
                    ORDER BY date
                """, "${startDate}", "${endDate}")
                
                # Revenue by day
                daily_revenue: @query("""
                    SELECT 
                        DATE(created_at) as date,
                        SUM(amount) as revenue,
                        COUNT(*) as orders
                    FROM orders
                    WHERE status = 'completed' 
                    AND created_at BETWEEN ? AND ?
                    GROUP BY DATE(created_at)
                    ORDER BY date
                """, "${startDate}", "${endDate}")
                
                # Top performing posts
                top_posts: @query("""
                    SELECT 
                        p.id,
                        p.title,
                        u.name as author_name,
                        COUNT(l.id) as likes,
                        COUNT(c.id) as comments,
                        p.created_at
                    FROM posts p
                    JOIN users u ON p.user_id = u.id
                    LEFT JOIN likes l ON p.id = l.post_id
                    LEFT JOIN comments c ON p.id = c.post_id
                    WHERE p.created_at BETWEEN ? AND ?
                    GROUP BY p.id, p.title, u.name, p.created_at
                    ORDER BY likes DESC, comments DESC
                    LIMIT 10
                """, "${startDate}", "${endDate}")
                
                # User engagement metrics
                engagement_metrics: @query("""
                    SELECT 
                        COUNT(DISTINCT u.id) as total_users,
                        COUNT(DISTINCT CASE WHEN p.id IS NOT NULL THEN u.id END) as users_with_posts,
                        COUNT(DISTINCT CASE WHEN o.id IS NOT NULL THEN u.id END) as users_with_orders,
                        AVG(posts_per_user.post_count) as avg_posts_per_user,
                        AVG(orders_per_user.order_count) as avg_orders_per_user
                    FROM users u
                    LEFT JOIN posts p ON u.id = p.user_id
                    LEFT JOIN orders o ON u.id = o.user_id
                    LEFT JOIN (
                        SELECT user_id, COUNT(*) as post_count
                        FROM posts
                        GROUP BY user_id
                    ) posts_per_user ON u.id = posts_per_user.user_id
                    LEFT JOIN (
                        SELECT user_id, COUNT(*) as order_count
                        FROM orders
                        GROUP BY user_id
                    ) orders_per_user ON u.id = orders_per_user.user_id
                    WHERE u.created_at BETWEEN ? AND ?
                """, "${startDate}", "${endDate}")
            }
        `));
    }
}
```

### Connection Pooling

```javascript
// connection-pooling.js
const { Pool } = require('pg');
const Redis = require('ioredis');

class ConnectionManager {
    constructor() {
        // PostgreSQL connection pool
        this.postgresPool = new Pool({
            host: process.env.DB_HOST,
            port: process.env.DB_PORT,
            database: process.env.DB_NAME,
            user: process.env.DB_USER,
            password: process.env.DB_PASSWORD,
            
            // Pool configuration
            max: 20, // Maximum number of clients
            min: 5,  // Minimum number of clients
            idle: 10000, // Close idle clients after 10 seconds
            connectionTimeoutMillis: 5000,
            idleTimeoutMillis: 30000,
            
            // SSL configuration
            ssl: process.env.NODE_ENV === 'production' ? {
                rejectUnauthorized: false
            } : false
        });

        // Redis connection pool
        this.redisPool = new Redis({
            host: process.env.REDIS_HOST,
            port: process.env.REDIS_PORT,
            password: process.env.REDIS_PASSWORD,
            
            // Connection pool settings
            maxRetriesPerRequest: 3,
            retryDelayOnFailover: 100,
            enableReadyCheck: true,
            maxLoadingTimeout: 5000,
            
            // Cluster configuration
            cluster: process.env.REDIS_CLUSTER === 'true',
            clusterNodes: process.env.REDIS_CLUSTER_NODES ? 
                process.env.REDIS_CLUSTER_NODES.split(',') : undefined
        });

        // Monitor connection health
        this.monitorConnections();
    }

    async getPostgresConnection() {
        try {
            const client = await this.postgresPool.connect();
            return client;
        } catch (error) {
            console.error('PostgreSQL connection error:', error);
            throw error;
        }
    }

    async getRedisConnection() {
        try {
            if (this.redisPool.status === 'ready') {
                return this.redisPool;
            }
            throw new Error('Redis not ready');
        } catch (error) {
            console.error('Redis connection error:', error);
            throw error;
        }
    }

    monitorConnections() {
        // Monitor PostgreSQL pool
        this.postgresPool.on('connect', (client) => {
            console.log('PostgreSQL client connected');
        });

        this.postgresPool.on('error', (err, client) => {
            console.error('PostgreSQL pool error:', err);
        });

        // Monitor Redis connections
        this.redisPool.on('connect', () => {
            console.log('Redis connected');
        });

        this.redisPool.on('error', (error) => {
            console.error('Redis error:', error);
        });

        // Health check interval
        setInterval(async () => {
            try {
                await this.postgresPool.query('SELECT 1');
                await this.redisPool.ping();
            } catch (error) {
                console.error('Health check failed:', error);
            }
        }, 30000); // Every 30 seconds
    }

    async close() {
        await this.postgresPool.end();
        await this.redisPool.quit();
    }
}
```

## 🧠 Memory Management

### Memory Optimization

```javascript
// memory-optimization.js
const TuskLang = require('tusklang');

class MemoryOptimizer {
    constructor() {
        this.memoryUsage = new Map();
        this.gcThreshold = 100 * 1024 * 1024; // 100MB
    }

    // Optimize TuskLang configuration parsing
    async parseOptimizedConfig(configString) {
        try {
            // Use streaming parser for large configs
            const config = await TuskLang.parse(configString, {
                streaming: true,
                maxMemory: 50 * 1024 * 1024, // 50MB limit
                cleanup: true
            });

            // Clear memory after parsing
            this.clearMemory();
            
            return config;
        } catch (error) {
            console.error('Config parsing error:', error);
            throw error;
        }
    }

    // Optimize large query results
    async executeOptimizedQuery(query, params = []) {
        try {
            // Use cursor for large result sets
            const result = await this.tsk.parse(TuskLang.parse(`
                query_result {
                    data: @query.cursor("${query}", ${JSON.stringify(params)})
                    metadata: @query.metadata("${query}")
                }
            `), {
                cursor: true,
                batchSize: 1000,
                maxRows: 10000
            });

            return result;
        } catch (error) {
            console.error('Query execution error:', error);
            throw error;
        }
    }

    // Memory cleanup
    clearMemory() {
        // Clear unused variables
        this.memoryUsage.clear();
        
        // Force garbage collection if available
        if (global.gc) {
            global.gc();
        }
    }

    // Monitor memory usage
    monitorMemory() {
        const usage = process.memoryUsage();
        
        this.memoryUsage.set(Date.now(), {
            rss: usage.rss,
            heapUsed: usage.heapUsed,
            heapTotal: usage.heapTotal,
            external: usage.external
        });

        // Clean old entries
        const oneHourAgo = Date.now() - (60 * 60 * 1000);
        for (const [timestamp] of this.memoryUsage) {
            if (timestamp < oneHourAgo) {
                this.memoryUsage.delete(timestamp);
            }
        }

        // Alert if memory usage is high
        if (usage.heapUsed > this.gcThreshold) {
            console.warn('High memory usage detected:', usage.heapUsed);
            this.clearMemory();
        }
    }
}
```

## 📈 Performance Monitoring

### Performance Metrics

```javascript
// performance-monitoring.js
const TuskLang = require('tusklang');
const { performance } = require('perf_hooks');

class PerformanceMonitor {
    constructor(tsk) {
        this.tsk = tsk;
        this.metrics = new Map();
        this.startTime = Date.now();
    }

    // Monitor query performance
    async monitorQuery(query, params = []) {
        const start = performance.now();
        
        try {
            const result = await this.tsk.parse(TuskLang.parse(query), { request: { params } });
            const duration = performance.now() - start;
            
            this.recordMetric('query_duration', duration);
            this.recordMetric('query_success', 1);
            
            return result;
        } catch (error) {
            const duration = performance.now() - start;
            this.recordMetric('query_duration', duration);
            this.recordMetric('query_errors', 1);
            
            throw error;
        }
    }

    // Monitor cache performance
    async monitorCache(key, operation, callback) {
        const start = performance.now();
        
        try {
            const result = await callback();
            const duration = performance.now() - start;
            
            this.recordMetric(`cache_${operation}_duration`, duration);
            this.recordMetric(`cache_${operation}_success`, 1);
            
            return result;
        } catch (error) {
            const duration = performance.now() - start;
            this.recordMetric(`cache_${operation}_duration`, duration);
            this.recordMetric(`cache_${operation}_errors`, 1);
            
            throw error;
        }
    }

    // Record performance metrics
    recordMetric(name, value) {
        if (!this.metrics.has(name)) {
            this.metrics.set(name, []);
        }
        
        this.metrics.get(name).push({
            value,
            timestamp: Date.now()
        });

        // Keep only last 1000 measurements
        if (this.metrics.get(name).length > 1000) {
            this.metrics.get(name).shift();
        }
    }

    // Get performance statistics
    getPerformanceStats() {
        const stats = {};
        
        for (const [name, values] of this.metrics) {
            const numericValues = values.map(v => v.value).filter(v => typeof v === 'number');
            
            if (numericValues.length > 0) {
                stats[name] = {
                    count: numericValues.length,
                    min: Math.min(...numericValues),
                    max: Math.max(...numericValues),
                    avg: numericValues.reduce((a, b) => a + b, 0) / numericValues.length,
                    p95: this.percentile(numericValues, 95),
                    p99: this.percentile(numericValues, 99)
                };
            }
        }
        
        return stats;
    }

    // Calculate percentile
    percentile(values, p) {
        const sorted = values.sort((a, b) => a - b);
        const index = Math.ceil((p / 100) * sorted.length) - 1;
        return sorted[index];
    }

    // Generate performance report
    generateReport() {
        const stats = this.getPerformanceStats();
        const uptime = Date.now() - this.startTime;
        
        return {
            uptime: uptime,
            uptime_formatted: this.formatUptime(uptime),
            metrics: stats,
            summary: {
                total_queries: stats.query_success?.count || 0,
                avg_query_time: stats.query_duration?.avg || 0,
                cache_hit_rate: this.calculateCacheHitRate(),
                error_rate: this.calculateErrorRate()
            }
        };
    }

    formatUptime(ms) {
        const seconds = Math.floor(ms / 1000);
        const minutes = Math.floor(seconds / 60);
        const hours = Math.floor(minutes / 60);
        const days = Math.floor(hours / 24);
        
        return `${days}d ${hours % 24}h ${minutes % 60}m ${seconds % 60}s`;
    }

    calculateCacheHitRate() {
        const hits = this.metrics.get('cache_get_success')?.length || 0;
        const misses = this.metrics.get('cache_get_errors')?.length || 0;
        const total = hits + misses;
        
        return total > 0 ? (hits / total) * 100 : 0;
    }

    calculateErrorRate() {
        const successes = this.metrics.get('query_success')?.length || 0;
        const errors = this.metrics.get('query_errors')?.length || 0;
        const total = successes + errors;
        
        return total > 0 ? (errors / total) * 100 : 0;
    }
}
```

## 📚 Next Steps

1. **[Security Best Practices](009-security-best-practices-javascript.md)** - Secure your applications
2. **[Testing Strategies](010-testing-strategies-javascript.md)** - Test your optimizations
3. **[Scaling Applications](011-scaling-applications-javascript.md)** - Scale your applications
4. **[Debugging Tools](012-debugging-tools-javascript.md)** - Debug performance issues

## 🎉 Performance Optimization Complete!

You now understand how to optimize TuskLang applications for:
- ✅ **Caching** - Multi-level caching strategies
- ✅ **Query Optimization** - Database query optimization
- ✅ **Connection Pooling** - Efficient connection management
- ✅ **Memory Management** - Memory optimization techniques
- ✅ **Performance Monitoring** - Real-time performance tracking

**Ready to build lightning-fast TuskLang applications!** 