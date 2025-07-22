# Web Application Example

**Complete web application using TuskLang JavaScript SDK**

This example demonstrates how to build a production-ready web application using TuskLang for configuration management.

## Project Structure

```
web-app/
├── config/
│   ├── app.tsk
│   ├── database.tsk
│   └── production.tsk
├── src/
│   ├── app.js
│   ├── server.js
│   ├── routes/
│   │   ├── api.js
│   │   └── web.js
│   └── middleware/
│       ├── auth.js
│       └── logging.js
├── package.json
└── README.md
```

## Configuration Files

### Main Application Configuration (`config/app.tsk`)

```tsk
# Global variables
$app_name: "TuskLang Web App"
$version: "1.0.0"
$env: @env("NODE_ENV", "development")

# Application settings
name: $app_name
version: $version
debug: $env == "development" ? true : false

# Server configuration
[server]
host: "0.0.0.0"
port: @env("PORT", "8080")
workers: $env == "production" ? 8 : 2
timeout: 30000

# SSL configuration (production only)
ssl: {
  enabled: $env == "production" ? true : false
  cert: @env("SSL_CERT", "/path/to/cert")
  key: @env("SSL_KEY", "/path/to/key")
}

# Security settings
[security]
jwt_secret: @env("JWT_SECRET", "your-secret-key")
bcrypt_rounds: 12
session_timeout: 3600
cors_origin: $env == "production" ? "https://myapp.com" : "*"

# Rate limiting
[rate_limit]
enabled: true
window_ms: 900000  # 15 minutes
max_requests: $env == "production" ? 100 : 1000
skip_successful_requests: false

# Logging
[logging]
level: $env == "production" ? "info" : "debug"
format: "json"
file: $env == "production" ? "/var/log/app.log" : null
console: true

# Monitoring
[monitoring]
enabled: true
metrics: {
  uptime: @date("U")
  memory_usage: @system("memory")
  cpu_usage: @system("cpu")
}
```

### Database Configuration (`config/database.tsk`)

```tsk
# Database configuration
[database]
type: "postgres"
host: @env("DB_HOST", "localhost")
port: @env("DB_PORT", "5432")
database: @env("DB_NAME", "tusklang_webapp")
username: @env("DB_USER", "postgres")
password: @env("DB_PASSWORD")

# Connection pool settings
pool: {
  min: 2
  max: $env == "production" ? 20 : 10
  acquire_timeout: 60000
  idle_timeout: 30000
}

# Migration settings
migrations: {
  directory: "./migrations"
  table_name: "schema_migrations"
  timeout: 60000
}

# Query logging (development only)
query_logging: $env == "development" ? true : false

# Statistics
stats: {
  total_users: @query("SELECT COUNT(*) FROM users")
  active_users: @query("SELECT COUNT(*) FROM users WHERE last_login > NOW() - INTERVAL '7 days'")
  total_posts: @query("SELECT COUNT(*) FROM posts")
}
```

### Production Configuration (`config/production.tsk`)

```tsk
# Production-specific overrides
$env: "production"

[server]
workers: 8
timeout: 60000

[security]
session_timeout: 7200
cors_origin: "https://myapp.com"

[logging]
level: "warn"
file: "/var/log/app.log"

[monitoring]
enabled: true
alerts: {
  memory_threshold: 80
  cpu_threshold: 70
  disk_threshold: 85
}
```

## Application Code

### Main Application (`src/app.js`)

```javascript
const express = require('express');
const cors = require('cors');
const helmet = require('helmet');
const rateLimit = require('express-rate-limit');
const TuskLangEnhanced = require('../../tsk-enhanced.js');
const { Pool } = require('pg');

class WebApplication {
  constructor() {
    this.app = express();
    this.config = null;
    this.dbPool = null;
  }

  async initialize() {
    console.log('🚀 Initializing TuskLang Web Application...');
    
    // Load configuration
    await this.loadConfiguration();
    
    // Initialize database
    await this.initializeDatabase();
    
    // Setup middleware
    this.setupMiddleware();
    
    // Setup routes
    this.setupRoutes();
    
    // Setup error handling
    this.setupErrorHandling();
    
    console.log('✅ Application initialized successfully');
  }

  async loadConfiguration() {
    console.log('📋 Loading configuration...');
    
    // Set up database adapter for configuration queries
    const configDbPool = new Pool({
      host: process.env.DB_HOST || 'localhost',
      port: process.env.DB_PORT || 5432,
      database: process.env.DB_NAME || 'tusklang_webapp',
      user: process.env.DB_USER || 'postgres',
      password: process.env.DB_PASSWORD
    });

    const dbAdapter = {
      async query(sql, params = []) {
        const client = await configDbPool.connect();
        try {
          const result = await client.query(sql, params);
          return result.rows;
        } finally {
          client.release();
        }
      }
    };

    // Create parser
    const tusk = new TuskLangEnhanced({
      debug: process.env.NODE_ENV === 'development'
    });

    tusk.setDatabaseAdapter(dbAdapter);

    // Load main configuration
    this.config = await tusk.parseFile('./config/app.tsk');
    
    // Load database configuration
    const dbConfig = await tusk.parseFile('./config/database.tsk');
    this.config.database = dbConfig.database;

    // Load environment-specific overrides
    if (process.env.NODE_ENV === 'production') {
      const prodConfig = await tusk.parseFile('./config/production.tsk');
      this.config = { ...this.config, ...prodConfig };
    }

    console.log(`✅ Configuration loaded: ${this.config.name} v${this.config.version}`);
    console.log(`🌍 Environment: ${process.env.NODE_ENV || 'development'}`);
    console.log(`🔧 Debug mode: ${this.config.debug}`);
  }

  async initializeDatabase() {
    console.log('🗄️ Initializing database connection...');
    
    this.dbPool = new Pool({
      host: this.config.database.host,
      port: this.config.database.port,
      database: this.config.database.database,
      user: this.config.database.username,
      password: this.config.database.password,
      ...this.config.database.pool
    });

    // Test connection
    try {
      const client = await this.dbPool.connect();
      await client.query('SELECT NOW()');
      client.release();
      console.log('✅ Database connection established');
    } catch (error) {
      console.error('❌ Database connection failed:', error.message);
      throw error;
    }
  }

  setupMiddleware() {
    console.log('🔧 Setting up middleware...');

    // Security middleware
    this.app.use(helmet());

    // CORS
    this.app.use(cors({
      origin: this.config.security.cors_origin,
      credentials: true
    }));

    // Rate limiting
    if (this.config.rate_limit.enabled) {
      const limiter = rateLimit({
        windowMs: this.config.rate_limit.window_ms,
        max: this.config.rate_limit.max_requests,
        skipSuccessfulRequests: this.config.rate_limit.skip_successful_requests,
        message: {
          error: 'Too many requests, please try again later.'
        }
      });
      this.app.use('/api/', limiter);
    }

    // Body parsing
    this.app.use(express.json({ limit: '10mb' }));
    this.app.use(express.urlencoded({ extended: true }));

    // Logging middleware
    this.app.use((req, res, next) => {
      if (this.config.logging.console) {
        console.log(`${new Date().toISOString()} - ${req.method} ${req.path}`);
      }
      next();
    });
  }

  setupRoutes() {
    console.log('🛣️ Setting up routes...');

    // Health check
    this.app.get('/health', (req, res) => {
      res.json({
        status: 'healthy',
        name: this.config.name,
        version: this.config.version,
        uptime: process.uptime(),
        timestamp: new Date().toISOString()
      });
    });

    // API routes
    this.app.use('/api', require('./routes/api')(this.config, this.dbPool));
    
    // Web routes
    this.app.use('/', require('./routes/web')(this.config));

    // 404 handler
    this.app.use('*', (req, res) => {
      res.status(404).json({
        error: 'Not Found',
        message: `Route ${req.originalUrl} not found`
      });
    });
  }

  setupErrorHandling() {
    // Global error handler
    this.app.use((error, req, res, next) => {
      console.error('Application error:', error);
      
      res.status(error.status || 500).json({
        error: 'Internal Server Error',
        message: this.config.debug ? error.message : 'Something went wrong'
      });
    });
  }

  async start() {
    const port = this.config.server.port;
    
    if (this.config.server.ssl.enabled) {
      const https = require('https');
      const fs = require('fs');
      
      const options = {
        key: fs.readFileSync(this.config.server.ssl.key),
        cert: fs.readFileSync(this.config.server.ssl.cert)
      };

      https.createServer(options, this.app).listen(port, () => {
        console.log(`🚀 HTTPS Server running on port ${port}`);
      });
    } else {
      this.app.listen(port, this.config.server.host, () => {
        console.log(`🚀 HTTP Server running on ${this.config.server.host}:${port}`);
      });
    }
  }

  async shutdown() {
    console.log('🛑 Shutting down application...');
    
    if (this.dbPool) {
      await this.dbPool.end();
      console.log('✅ Database connections closed');
    }
    
    process.exit(0);
  }
}

// Start application
async function main() {
  const app = new WebApplication();
  
  try {
    await app.initialize();
    await app.start();
    
    // Graceful shutdown
    process.on('SIGTERM', () => app.shutdown());
    process.on('SIGINT', () => app.shutdown());
    
  } catch (error) {
    console.error('❌ Failed to start application:', error);
    process.exit(1);
  }
}

if (require.main === module) {
  main();
}

module.exports = WebApplication;
```

### API Routes (`src/routes/api.js`)

```javascript
const express = require('express');
const bcrypt = require('bcrypt');
const jwt = require('jsonwebtoken');

module.exports = (config, dbPool) => {
  const router = express.Router();

  // Authentication middleware
  const authenticateToken = async (req, res, next) => {
    const authHeader = req.headers['authorization'];
    const token = authHeader && authHeader.split(' ')[1];

    if (!token) {
      return res.status(401).json({ error: 'Access token required' });
    }

    try {
      const user = jwt.verify(token, config.security.jwt_secret);
      req.user = user;
      next();
    } catch (error) {
      return res.status(403).json({ error: 'Invalid token' });
    }
  };

  // User registration
  router.post('/register', async (req, res) => {
    try {
      const { username, email, password } = req.body;

      // Validate input
      if (!username || !email || !password) {
        return res.status(400).json({
          error: 'Missing required fields'
        });
      }

      // Check if user exists
      const existingUser = await dbPool.query(
        'SELECT id FROM users WHERE email = $1',
        [email]
      );

      if (existingUser.rows.length > 0) {
        return res.status(409).json({
          error: 'User already exists'
        });
      }

      // Hash password
      const hashedPassword = await bcrypt.hash(
        password, 
        config.security.bcrypt_rounds
      );

      // Create user
      const result = await dbPool.query(
        'INSERT INTO users (username, email, password_hash) VALUES ($1, $2, $3) RETURNING id, username, email',
        [username, email, hashedPassword]
      );

      const user = result.rows[0];

      // Generate JWT token
      const token = jwt.sign(
        { userId: user.id, username: user.username },
        config.security.jwt_secret,
        { expiresIn: config.security.session_timeout }
      );

      res.status(201).json({
        message: 'User created successfully',
        user: {
          id: user.id,
          username: user.username,
          email: user.email
        },
        token
      });

    } catch (error) {
      console.error('Registration error:', error);
      res.status(500).json({
        error: 'Internal server error'
      });
    }
  });

  // User login
  router.post('/login', async (req, res) => {
    try {
      const { email, password } = req.body;

      // Validate input
      if (!email || !password) {
        return res.status(400).json({
          error: 'Email and password required'
        });
      }

      // Find user
      const result = await dbPool.query(
        'SELECT id, username, email, password_hash FROM users WHERE email = $1',
        [email]
      );

      if (result.rows.length === 0) {
        return res.status(401).json({
          error: 'Invalid credentials'
        });
      }

      const user = result.rows[0];

      // Verify password
      const validPassword = await bcrypt.compare(password, user.password_hash);
      if (!validPassword) {
        return res.status(401).json({
          error: 'Invalid credentials'
        });
      }

      // Update last login
      await dbPool.query(
        'UPDATE users SET last_login = NOW() WHERE id = $1',
        [user.id]
      );

      // Generate JWT token
      const token = jwt.sign(
        { userId: user.id, username: user.username },
        config.security.jwt_secret,
        { expiresIn: config.security.session_timeout }
      );

      res.json({
        message: 'Login successful',
        user: {
          id: user.id,
          username: user.username,
          email: user.email
        },
        token
      });

    } catch (error) {
      console.error('Login error:', error);
      res.status(500).json({
        error: 'Internal server error'
      });
    }
  });

  // Get user profile (protected route)
  router.get('/profile', authenticateToken, async (req, res) => {
    try {
      const result = await dbPool.query(
        'SELECT id, username, email, created_at, last_login FROM users WHERE id = $1',
        [req.user.userId]
      );

      if (result.rows.length === 0) {
        return res.status(404).json({
          error: 'User not found'
        });
      }

      res.json({
        user: result.rows[0]
      });

    } catch (error) {
      console.error('Profile error:', error);
      res.status(500).json({
        error: 'Internal server error'
      });
    }
  });

  // Get application statistics
  router.get('/stats', async (req, res) => {
    try {
      const stats = {
        total_users: config.database.stats.total_users,
        active_users: config.database.stats.active_users,
        total_posts: config.database.stats.total_posts,
        uptime: config.monitoring.metrics.uptime,
        memory_usage: config.monitoring.metrics.memory_usage,
        cpu_usage: config.monitoring.metrics.cpu_usage
      };

      res.json(stats);

    } catch (error) {
      console.error('Stats error:', error);
      res.status(500).json({
        error: 'Internal server error'
      });
    }
  });

  return router;
};
```

### Web Routes (`src/routes/web.js`)

```javascript
const express = require('express');

module.exports = (config) => {
  const router = express.Router();

  // Home page
  router.get('/', (req, res) => {
    res.json({
      message: 'Welcome to TuskLang Web Application',
      name: config.name,
      version: config.version,
      environment: process.env.NODE_ENV || 'development',
      features: [
        'TuskLang Configuration Management',
        'Database Integration',
        'JWT Authentication',
        'Rate Limiting',
        'Security Headers',
        'Monitoring & Metrics'
      ]
    });
  });

  // API documentation
  router.get('/docs', (req, res) => {
    res.json({
      api_endpoints: {
        'POST /api/register': 'Register a new user',
        'POST /api/login': 'Login user',
        'GET /api/profile': 'Get user profile (authenticated)',
        'GET /api/stats': 'Get application statistics',
        'GET /health': 'Health check'
      },
      authentication: 'JWT Bearer token required for protected routes',
      rate_limiting: `${config.rate_limit.max_requests} requests per ${config.rate_limit.window_ms / 1000 / 60} minutes`
    });
  });

  return router;
};
```

## Package Configuration

### `package.json`

```json
{
  "name": "tusklang-web-app",
  "version": "1.0.0",
  "description": "Web application using TuskLang configuration",
  "main": "src/app.js",
  "scripts": {
    "start": "node src/app.js",
    "dev": "NODE_ENV=development nodemon src/app.js",
    "test": "jest",
    "lint": "eslint src/",
    "migrate": "tsk db migrate migrations/schema.sql"
  },
  "dependencies": {
    "express": "^4.18.2",
    "cors": "^2.8.5",
    "helmet": "^7.0.0",
    "express-rate-limit": "^6.7.0",
    "bcrypt": "^5.1.0",
    "jsonwebtoken": "^9.0.0",
    "pg": "^8.10.0",
    "tusklang": "^2.0.2"
  },
  "devDependencies": {
    "nodemon": "^2.0.22",
    "jest": "^29.5.0",
    "eslint": "^8.40.0"
  },
  "engines": {
    "node": ">=12.0.0"
  }
}
```

## Database Schema

### `migrations/schema.sql`

```sql
-- Create users table
CREATE TABLE IF NOT EXISTS users (
  id SERIAL PRIMARY KEY,
  username VARCHAR(50) UNIQUE NOT NULL,
  email VARCHAR(100) UNIQUE NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  last_login TIMESTAMP,
  status VARCHAR(20) DEFAULT 'active'
);

-- Create posts table
CREATE TABLE IF NOT EXISTS posts (
  id SERIAL PRIMARY KEY,
  user_id INTEGER REFERENCES users(id),
  title VARCHAR(200) NOT NULL,
  content TEXT,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_status ON users(status);
CREATE INDEX IF NOT EXISTS idx_posts_user_id ON posts(user_id);
CREATE INDEX IF NOT EXISTS idx_posts_created_at ON posts(created_at);

-- Insert sample data
INSERT INTO users (username, email, password_hash) VALUES
  ('admin', 'admin@example.com', '$2b$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj4J/HS.iK8.'),
  ('user1', 'user1@example.com', '$2b$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj4J/HS.iK8.')
ON CONFLICT (email) DO NOTHING;

INSERT INTO posts (user_id, title, content) VALUES
  (1, 'Welcome to TuskLang', 'This is the first post using TuskLang configuration!'),
  (2, 'Getting Started', 'How to use TuskLang in your applications.')
ON CONFLICT DO NOTHING;
```

## Environment Variables

### `.env`

```bash
# Application
NODE_ENV=development
PORT=8080

# Database
DB_HOST=localhost
DB_PORT=5432
DB_NAME=tusklang_webapp
DB_USER=postgres
DB_PASSWORD=password

# Security
JWT_SECRET=your-super-secret-jwt-key-change-in-production

# SSL (production only)
SSL_CERT=/path/to/cert.pem
SSL_KEY=/path/to/key.pem
```

## Running the Application

### 1. Install Dependencies

```bash
npm install
```

### 2. Set Up Database

```bash
# Create database
createdb tusklang_webapp

# Run migrations
npm run migrate
```

### 3. Start the Application

```bash
# Development mode
npm run dev

# Production mode
npm start
```

### 4. Test the Application

```bash
# Health check
curl http://localhost:8080/health

# Register a user
curl -X POST http://localhost:8080/api/register \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","email":"test@example.com","password":"password123"}'

# Login
curl -X POST http://localhost:8080/api/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"password123"}'

# Get profile (use token from login response)
curl -H "Authorization: Bearer YOUR_TOKEN" \
  http://localhost:8080/api/profile

# Get statistics
curl http://localhost:8080/api/stats
```

## Key Features Demonstrated

1. **TuskLang Configuration Management** - Multiple configuration files with environment-specific overrides
2. **Database Integration** - Direct SQL queries in configuration files
3. **Security** - JWT authentication, rate limiting, CORS, helmet
4. **Monitoring** - Health checks, metrics, uptime tracking
5. **Production Ready** - SSL support, graceful shutdown, error handling
6. **Development Friendly** - Debug logging, hot reloading

This example shows how TuskLang can be used to build a complete, production-ready web application with comprehensive configuration management. 