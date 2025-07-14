# 🟨 TuskLang JavaScript Framework Integration Guide

**"We don't bow to any king" - JavaScript Edition**

Integrate TuskLang seamlessly with your favorite JavaScript frameworks. From Express.js to Next.js, React to Vue.js, learn how to leverage TuskLang's power in your framework of choice.

## 🚀 Express.js Integration

### Basic Express.js Setup

```javascript
const express = require('express');
const TuskLang = require('tusklang');
const PostgreSQLAdapter = require('tusklang/adapters/postgresql');

// Create Express app
const app = express();
app.use(express.json());

// Initialize TuskLang with database
const postgres = new PostgreSQLAdapter({
    host: 'localhost',
    port: 5432,
    database: 'myapp',
    user: 'postgres',
    password: process.env.DB_PASSWORD
});

const tsk = new TuskLang.Enhanced();
tsk.setDatabaseAdapter(postgres);

// Load configuration
const config = TuskLang.parse(`
app {
    name: "MyExpressApp"
    version: "1.0.0"
    port: @env("PORT", 3000)
}

server {
    host: "0.0.0.0"
    workers: @env("WORKERS", 1)
    timeout: 30000
}

database {
    host: "localhost"
    port: 5432
    name: "myapp"
    
    # Database queries for API endpoints
    user_count: @query("SELECT COUNT(*) FROM users")
    active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
}

api {
    rate_limit: @if(@env("NODE_ENV") == "production", 100, 1000)
    cors_origin: @if(@env("NODE_ENV") == "production", ["https://myapp.com"], ["*"])
    auth_required: @if(@env("NODE_ENV") == "production", true, false)
}

logging {
    level: @if(@env("NODE_ENV") == "production", "error", "debug")
    format: @if(@env("NODE_ENV") == "production", "json", "text")
}
`);

// Middleware for TuskLang configuration
app.use(async (req, res, next) => {
    try {
        // Add request context to TuskLang
        req.tsk = tsk;
        req.config = await tsk.parse(config, { request: req });
        next();
    } catch (error) {
        console.error('Configuration error:', error);
        res.status(500).json({ error: 'Configuration error' });
    }
});

// API Routes using TuskLang
app.get('/api/stats', async (req, res) => {
    try {
        const stats = await req.tsk.parse(TuskLang.parse(`
            stats {
                total_users: @query("SELECT COUNT(*) FROM users")
                active_users: @query("SELECT COUNT(*) FROM users WHERE active = true")
                recent_posts: @query("SELECT COUNT(*) FROM posts WHERE created_at > ?", @date.subtract("7d"))
                total_revenue: @query("SELECT SUM(amount) FROM orders WHERE status = 'completed'")
            }
        `), { request: req });
        
        res.json(stats);
    } catch (error) {
        res.status(500).json({ error: error.message });
    }
});

app.get('/api/users/:id', async (req, res) => {
    try {
        const userData = await req.tsk.parse(TuskLang.parse(`
            user {
                profile: @query("SELECT * FROM users WHERE id = ?", @request.params.id)
                posts: @query("SELECT * FROM posts WHERE user_id = ? ORDER BY created_at DESC LIMIT 10", @request.params.id)
                orders: @query("SELECT * FROM orders WHERE user_id = ? ORDER BY created_at DESC LIMIT 5", @request.params.id)
            }
        `), { request: req });
        
        if (!userData.user.profile) {
            return res.status(404).json({ error: 'User not found' });
        }
        
        res.json(userData);
    } catch (error) {
        res.status(500).json({ error: error.message });
    }
});

// Start server
const PORT = config.app.port;
app.listen(PORT, () => {
    console.log(`🚀 ${config.app.name} v${config.app.version} running on port ${PORT}`);
    console.log(`📊 Total users: ${config.database.user_count}`);
    console.log(`👥 Active users: ${config.database.active_users}`);
});
```

### Advanced Express.js Integration

```javascript
// Advanced Express.js with middleware and error handling
const express = require('express');
const helmet = require('helmet');
const cors = require('cors');
const rateLimit = require('express-rate-limit');
const TuskLang = require('tusklang');

const app = express();

// Load configuration with security settings
const config = TuskLang.parse(`
app {
    name: "SecureExpressApp"
    version: "1.0.0"
    port: @env("PORT", 3000)
}

security {
    # Rate limiting configuration
    rate_limit: @if(@env("NODE_ENV") == "production", 100, 1000)
    rate_limit_window: @if(@env("NODE_ENV") == "production", "15m", "1h")
    
    # CORS configuration
    cors_origin: @if(@env("NODE_ENV") == "production", 
        ["https://myapp.com", "https://www.myapp.com"], 
        ["http://localhost:3000", "http://localhost:3001"]
    )
    
    # Authentication
    jwt_secret: @env.secure("JWT_SECRET")
    session_secret: @env.secure("SESSION_SECRET")
    
    # SSL/TLS
    force_https: @if(@env("NODE_ENV") == "production", true, false)
    hsts_max_age: @if(@env("NODE_ENV") == "production", 31536000, 0)
}

database {
    host: @env("DB_HOST", "localhost")
    port: @env("DB_PORT", "5432")
    name: @env("DB_NAME", "myapp")
    user: @env("DB_USER", "postgres")
    password: @env.secure("DB_PASSWORD")
    
    # Connection pool settings
    pool_size: @if(@env("NODE_ENV") == "production", 20, 5)
    connection_timeout: 5000
    idle_timeout: 30000
}

logging {
    level: @if(@env("NODE_ENV") == "production", "error", "debug")
    format: @if(@env("NODE_ENV") == "production", "json", "text")
    file: @if(@env("NODE_ENV") == "production", "/var/log/app.log", "console")
}
`);

// Security middleware
app.use(helmet({
    contentSecurityPolicy: {
        directives: {
            defaultSrc: ["'self'"],
            styleSrc: ["'self'", "'unsafe-inline'"],
            scriptSrc: ["'self'"],
            imgSrc: ["'self'", "data:", "https:"],
        },
    },
    hsts: {
        maxAge: config.security.hsts_max_age,
        includeSubDomains: true,
        preload: true
    }
}));

// CORS configuration
app.use(cors({
    origin: config.security.cors_origin,
    credentials: true
}));

// Rate limiting
const limiter = rateLimit({
    windowMs: config.security.rate_limit_window,
    max: config.security.rate_limit,
    message: {
        error: 'Too many requests, please try again later.'
    }
});
app.use('/api/', limiter);

// TuskLang middleware with error handling
app.use(async (req, res, next) => {
    try {
        // Initialize TuskLang with request context
        const tsk = new TuskLang.Enhanced();
        
        // Add request data to context
        const context = {
            request: {
                method: req.method,
                url: req.url,
                headers: req.headers,
                body: req.body,
                params: req.params,
                query: req.query,
                user: req.user // From auth middleware
            },
            config: config
        };
        
        req.tsk = tsk;
        req.tskContext = context;
        
        next();
    } catch (error) {
        console.error('TuskLang middleware error:', error);
        res.status(500).json({ error: 'Configuration error' });
    }
});

// Authentication middleware
const authenticateToken = async (req, res, next) => {
    const authHeader = req.headers['authorization'];
    const token = authHeader && authHeader.split(' ')[1];
    
    if (!token) {
        return res.status(401).json({ error: 'Access token required' });
    }
    
    try {
        // Verify JWT token
        const jwt = require('jsonwebtoken');
        const user = jwt.verify(token, config.security.jwt_secret);
        req.user = user;
        next();
    } catch (error) {
        return res.status(403).json({ error: 'Invalid token' });
    }
};

// Protected API routes
app.get('/api/protected/stats', authenticateToken, async (req, res) => {
    try {
        const stats = await req.tsk.parse(TuskLang.parse(`
            user_stats {
                user_id: @request.user.id
                profile: @query("SELECT * FROM users WHERE id = ?", @request.user.id)
                post_count: @query("SELECT COUNT(*) FROM posts WHERE user_id = ?", @request.user.id)
                recent_activity: @query("""
                    SELECT 'post' as type, title as content, created_at 
                    FROM posts WHERE user_id = ? 
                    UNION ALL
                    SELECT 'order' as type, CONCAT('Order #', id) as content, created_at 
                    FROM orders WHERE user_id = ?
                    ORDER BY created_at DESC LIMIT 10
                """, @request.user.id, @request.user.id)
            }
        `), req.tskContext);
        
        res.json(stats);
    } catch (error) {
        res.status(500).json({ error: error.message });
    }
});

// Error handling middleware
app.use((error, req, res, next) => {
    console.error('Express error:', error);
    
    if (config.logging.level === 'debug') {
        res.status(500).json({ 
            error: error.message,
            stack: error.stack 
        });
    } else {
        res.status(500).json({ error: 'Internal server error' });
    }
});

// Start server
const PORT = config.app.port;
app.listen(PORT, () => {
    console.log(`🚀 ${config.app.name} v${config.app.version} running on port ${PORT}`);
    console.log(`🔒 Security: Rate limit ${config.security.rate_limit} requests per ${config.security.rate_limit_window}`);
    console.log(`🌍 CORS origins: ${config.security.cors_origin.join(', ')}`);
});
```

## ⚛️ Next.js Integration

### Next.js API Routes with TuskLang

```javascript
// pages/api/users.js
import TuskLang from 'tusklang';
import { PostgreSQLAdapter } from 'tusklang/adapters/postgresql';

// Initialize TuskLang
const postgres = new PostgreSQLAdapter({
    host: process.env.DB_HOST || 'localhost',
    port: process.env.DB_PORT || 5432,
    database: process.env.DB_NAME || 'myapp',
    user: process.env.DB_USER || 'postgres',
    password: process.env.DB_PASSWORD
});

const tsk = new TuskLang.Enhanced();
tsk.setDatabaseAdapter(postgres);

// Configuration for Next.js
const config = TuskLang.parse(`
app {
    name: "NextJSApp"
    version: "1.0.0"
}

api {
    rate_limit: @if(@env("NODE_ENV") == "production", 100, 1000)
    cache_ttl: @if(@env("NODE_ENV") == "production", "5m", "1m")
}

database {
    host: @env("DB_HOST", "localhost")
    port: @env("DB_PORT", "5432")
    name: @env("DB_NAME", "myapp")
    
    # Cached queries for better performance
    user_count: @cache("5m", @query("SELECT COUNT(*) FROM users"))
    active_users: @cache("1m", @query("SELECT COUNT(*) FROM users WHERE active = true"))
}
`);

export default async function handler(req, res) {
    try {
        const { method } = req;
        
        // Add request context
        const context = {
            request: {
                method: method,
                query: req.query,
                body: req.body,
                headers: req.headers
            }
        };
        
        switch (method) {
            case 'GET':
                const users = await tsk.parse(TuskLang.parse(`
                    users {
                        count: @query("SELECT COUNT(*) FROM users")
                        active: @query("SELECT COUNT(*) FROM users WHERE active = true")
                        list: @query("""
                            SELECT id, name, email, created_at 
                            FROM users 
                            ORDER BY created_at DESC 
                            LIMIT ?
                        """, @request.query.limit || 10)
                    }
                `), context);
                
                res.status(200).json(users);
                break;
                
            case 'POST':
                const newUser = await tsk.parse(TuskLang.parse(`
                    result {
                        user_id: @query("""
                            INSERT INTO users (name, email, created_at) 
                            VALUES (?, ?, NOW()) 
                            RETURNING id
                        """, @request.body.name, @request.body.email)
                        
                        # Invalidate cache
                        cache_invalidated: @cache.invalidate("user_count")
                    }
                `), context);
                
                res.status(201).json(newUser);
                break;
                
            default:
                res.setHeader('Allow', ['GET', 'POST']);
                res.status(405).end(`Method ${method} Not Allowed`);
        }
    } catch (error) {
        console.error('API error:', error);
        res.status(500).json({ error: error.message });
    }
}
```

### Next.js with Server-Side Rendering

```javascript
// pages/index.js
import { GetServerSideProps } from 'next';
import TuskLang from 'tusklang';

export default function Home({ stats, recentPosts }) {
    return (
        <div>
            <h1>Welcome to MyApp</h1>
            <div>
                <h2>Statistics</h2>
                <p>Total Users: {stats.user_count}</p>
                <p>Active Users: {stats.active_users}</p>
            </div>
            <div>
                <h2>Recent Posts</h2>
                {recentPosts.map(post => (
                    <div key={post.id}>
                        <h3>{post.title}</h3>
                        <p>{post.content}</p>
                    </div>
                ))}
            </div>
        </div>
    );
}

export const getServerSideProps = async () => {
    try {
        const tsk = new TuskLang.Enhanced();
        
        const data = await tsk.parse(TuskLang.parse(`
            page_data {
                stats {
                    user_count: @cache("5m", @query("SELECT COUNT(*) FROM users"))
                    active_users: @cache("1m", @query("SELECT COUNT(*) FROM users WHERE active = true"))
                    total_posts: @cache("5m", @query("SELECT COUNT(*) FROM posts"))
                }
                
                recent_posts: @cache("2m", @query("""
                    SELECT p.*, u.name as author_name
                    FROM posts p
                    JOIN users u ON p.user_id = u.id
                    ORDER BY p.created_at DESC
                    LIMIT 10
                """))
            }
        `));
        
        return {
            props: {
                stats: data.page_data.stats,
                recentPosts: data.page_data.recent_posts
            }
        };
    } catch (error) {
        console.error('SSR error:', error);
        return {
            props: {
                stats: { user_count: 0, active_users: 0, total_posts: 0 },
                recentPosts: []
            }
        };
    }
};
```

## ⚛️ React Integration

### React Hook for TuskLang

```javascript
// hooks/useTuskLang.js
import { useState, useEffect, useContext, createContext } from 'react';
import TuskLang from 'tusklang';

const TuskLangContext = createContext();

export function TuskLangProvider({ children, config }) {
    const [tsk, setTsk] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    useEffect(() => {
        async function initializeTuskLang() {
            try {
                const tuskInstance = new TuskLang.Enhanced();
                
                // Parse configuration
                const parsedConfig = await tuskInstance.parse(config);
                
                setTsk({ instance: tuskInstance, config: parsedConfig });
                setLoading(false);
            } catch (err) {
                setError(err);
                setLoading(false);
            }
        }

        initializeTuskLang();
    }, [config]);

    return (
        <TuskLangContext.Provider value={{ tsk, loading, error }}>
            {children}
        </TuskLangContext.Provider>
    );
}

export function useTuskLang() {
    const context = useContext(TuskLangContext);
    if (!context) {
        throw new Error('useTuskLang must be used within a TuskLangProvider');
    }
    return context;
}

// Custom hook for TuskLang queries
export function useTuskQuery(query, dependencies = []) {
    const { tsk } = useTuskLang();
    const [data, setData] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    useEffect(() => {
        async function executeQuery() {
            if (!tsk) return;
            
            try {
                setLoading(true);
                const result = await tsk.instance.parse(query);
                setData(result);
                setError(null);
            } catch (err) {
                setError(err);
            } finally {
                setLoading(false);
            }
        }

        executeQuery();
    }, [tsk, query, ...dependencies]);

    return { data, loading, error };
}
```

### React Component with TuskLang

```javascript
// components/UserDashboard.js
import React from 'react';
import { useTuskQuery } from '../hooks/useTuskLang';

export default function UserDashboard({ userId }) {
    const userStatsQuery = `
        user_stats {
            profile: @query("SELECT * FROM users WHERE id = ?", ${userId})
            posts: @query("SELECT * FROM posts WHERE user_id = ? ORDER BY created_at DESC LIMIT 5", ${userId})
            orders: @query("SELECT * FROM orders WHERE user_id = ? ORDER BY created_at DESC LIMIT 5", ${userId})
            total_spent: @query("SELECT SUM(amount) FROM orders WHERE user_id = ? AND status = 'completed'", ${userId})
        }
    `;

    const { data, loading, error } = useTuskQuery(userStatsQuery, [userId]);

    if (loading) return <div>Loading...</div>;
    if (error) return <div>Error: {error.message}</div>;
    if (!data) return <div>No data available</div>;

    const { user_stats } = data;

    return (
        <div className="user-dashboard">
            <h2>Welcome, {user_stats.profile.name}!</h2>
            
            <div className="stats-grid">
                <div className="stat-card">
                    <h3>Posts</h3>
                    <p>{user_stats.posts.length}</p>
                </div>
                
                <div className="stat-card">
                    <h3>Orders</h3>
                    <p>{user_stats.orders.length}</p>
                </div>
                
                <div className="stat-card">
                    <h3>Total Spent</h3>
                    <p>${user_stats.total_spent || 0}</p>
                </div>
            </div>
            
            <div className="recent-activity">
                <h3>Recent Posts</h3>
                {user_stats.posts.map(post => (
                    <div key={post.id} className="post-item">
                        <h4>{post.title}</h4>
                        <p>{post.content}</p>
                        <small>{new Date(post.created_at).toLocaleDateString()}</small>
                    </div>
                ))}
            </div>
        </div>
    );
}
```

## 🟢 Vue.js Integration

### Vue.js Plugin for TuskLang

```javascript
// plugins/tusklang.js
import TuskLang from 'tusklang';

export default {
    install(app, options) {
        const tsk = new TuskLang.Enhanced();
        
        // Parse configuration
        const config = TuskLang.parse(`
            app {
                name: "VueApp"
                version: "1.0.0"
            }
            
            api {
                base_url: @env("API_BASE_URL", "http://localhost:3000")
                timeout: 5000
            }
            
            database {
                host: @env("DB_HOST", "localhost")
                port: @env("DB_PORT", "5432")
                name: @env("DB_NAME", "myapp")
            }
        `);
        
        // Make TuskLang available globally
        app.config.globalProperties.$tsk = tsk;
        app.config.globalProperties.$config = config;
        
        // Provide TuskLang to components
        app.provide('tsk', tsk);
        app.provide('config', config);
    }
};
```

### Vue.js Component with TuskLang

```javascript
// components/UserProfile.vue
<template>
    <div class="user-profile">
        <div v-if="loading">Loading...</div>
        <div v-else-if="error">Error: {{ error }}</div>
        <div v-else>
            <h2>{{ user.name }}</h2>
            <p>{{ user.email }}</p>
            
            <div class="stats">
                <div class="stat">
                    <h3>Posts</h3>
                    <p>{{ stats.post_count }}</p>
                </div>
                <div class="stat">
                    <h3>Orders</h3>
                    <p>{{ stats.order_count }}</p>
                </div>
                <div class="stat">
                    <h3>Total Spent</h3>
                    <p>${{ stats.total_spent }}</p>
                </div>
            </div>
            
            <div class="recent-posts">
                <h3>Recent Posts</h3>
                <div v-for="post in recentPosts" :key="post.id" class="post">
                    <h4>{{ post.title }}</h4>
                    <p>{{ post.content }}</p>
                </div>
            </div>
        </div>
    </div>
</template>

<script>
import { ref, onMounted, inject } from 'vue';

export default {
    name: 'UserProfile',
    props: {
        userId: {
            type: Number,
            required: true
        }
    },
    setup(props) {
        const tsk = inject('tsk');
        const user = ref(null);
        const stats = ref({});
        const recentPosts = ref([]);
        const loading = ref(true);
        const error = ref(null);

        const loadUserData = async () => {
            try {
                const query = `
                    user_data {
                        user: @query("SELECT * FROM users WHERE id = ?", ${props.userId})
                        stats {
                            post_count: @query("SELECT COUNT(*) FROM posts WHERE user_id = ?", ${props.userId})
                            order_count: @query("SELECT COUNT(*) FROM orders WHERE user_id = ?", ${props.userId})
                            total_spent: @query("SELECT SUM(amount) FROM orders WHERE user_id = ? AND status = 'completed'", ${props.userId})
                        }
                        recent_posts: @query("SELECT * FROM posts WHERE user_id = ? ORDER BY created_at DESC LIMIT 5", ${props.userId})
                    }
                `;

                const result = await tsk.parse(query);
                
                user.value = result.user_data.user;
                stats.value = result.user_data.stats;
                recentPosts.value = result.user_data.recent_posts;
                
            } catch (err) {
                error.value = err.message;
            } finally {
                loading.value = false;
            }
        };

        onMounted(loadUserData);

        return {
            user,
            stats,
            recentPosts,
            loading,
            error
        };
    }
};
</script>
```

## 🔧 Nuxt.js Integration

### Nuxt.js Module for TuskLang

```javascript
// modules/tusklang.js
import TuskLang from 'tusklang';

export default function (moduleOptions) {
    const { nuxt } = this;
    
    // Initialize TuskLang
    const tsk = new TuskLang.Enhanced();
    
    // Parse configuration
    const config = TuskLang.parse(`
        app {
            name: "NuxtApp"
            version: "1.0.0"
        }
        
        api {
            base_url: @env("API_BASE_URL", "http://localhost:3000")
            timeout: 5000
        }
        
        database {
            host: @env("DB_HOST", "localhost")
            port: @env("DB_PORT", "5432")
            name: @env("DB_NAME", "myapp")
        }
    `);
    
    // Add to Nuxt context
    nuxt.options.tusklang = { tsk, config };
    
    // Add plugin to inject TuskLang
    this.addPlugin({
        src: require.resolve('./plugin.js'),
        fileName: 'tusklang.js'
    });
}
```

### Nuxt.js Plugin

```javascript
// plugins/tusklang.js
export default ({ app }, inject) => {
    const { tsk, config } = app.$nuxt.options.tusklang;
    
    // Inject TuskLang into Vue context
    inject('tsk', tsk);
    inject('config', config);
    
    // Add to app context
    app.$tsk = tsk;
    app.$config = config;
};
```

## 📚 Next Steps

1. **[Production Deployment](007-production-deployment-javascript.md)** - Deploy with confidence
2. **[Performance Optimization](008-performance-optimization-javascript.md)** - Optimize your applications
3. **[Security Best Practices](009-security-best-practices-javascript.md)** - Secure your applications
4. **[Testing Strategies](010-testing-strategies-javascript.md)** - Test your TuskLang integrations

## 🎉 Framework Integration Complete!

You now understand how to integrate TuskLang with:
- ✅ **Express.js** - API routes and middleware
- ✅ **Next.js** - API routes and SSR
- ✅ **React** - Hooks and components
- ✅ **Vue.js** - Plugins and components
- ✅ **Nuxt.js** - Modules and plugins

**Ready to deploy your TuskLang-powered applications to production!** 