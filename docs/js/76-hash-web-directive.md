# #web Directive - Web Application Configuration

## Overview
The `#web` directive in TuskLang provides comprehensive web application configuration capabilities, enabling you to define web servers, routes, middleware, static file serving, and web-specific settings in a declarative manner.

## TuskLang Syntax

### Basic Web Server
```tusk
#web {
  port: 3000,
  host: "localhost",
  protocol: "http",
  cors: {
    enabled: true,
    origin: ["http://localhost:3000", "https://example.com"]
  }
}
```

### Web Server with Routes
```tusk
#web {
  port: 8080,
  routes: {
    "/api/users": {
      method: "GET",
      handler: "userController.getUsers",
      middleware: ["auth", "rateLimit"]
    },
    "/api/posts": {
      method: "POST",
      handler: "postController.createPost",
      middleware: ["auth", "validation"]
    }
  },
  static: {
    "/public": "./public",
    "/assets": "./assets"
  }
}
```

### Advanced Web Configuration
```tusk
#web {
  port: @env("PORT", 3000),
  host: @env("HOST", "0.0.0.0"),
  ssl: {
    enabled: true,
    cert: @file.read("ssl/cert.pem"),
    key: @file.read("ssl/key.pem")
  },
  middleware: ["cors", "helmet", "compression"],
  rateLimit: {
    windowMs: 15 * 60 * 1000,
    max: 100
  }
}
```

## JavaScript Integration

### Node.js Web Server
```javascript
const tusklang = require('@tusklang/core');
const express = require('express');
const cors = require('cors');
const helmet = require('helmet');
const compression = require('compression');
const rateLimit = require('express-rate-limit');

const config = tusklang.parse(`
web_config: #web {
  port: 3000,
  host: "localhost",
  protocol: "http",
  cors: {
    enabled: true,
    origin: ["http://localhost:3000", "https://example.com"]
  },
  routes: {
    "/api/users": {
      method: "GET",
      handler: "userController.getUsers",
      middleware: ["auth", "rateLimit"]
    },
    "/api/posts": {
      method: "POST",
      handler: "postController.createPost",
      middleware: ["auth", "validation"]
    }
  },
  static: {
    "/public": "./public",
    "/assets": "./assets"
  },
  middleware: ["cors", "helmet", "compression"],
  rateLimit: {
    windowMs: 15 * 60 * 1000,
    max: 100
  }
}
`);

class WebServer {
  constructor(config) {
    this.config = config.web_config;
    this.app = express();
    this.server = null;
    this.controllers = new Map();
    this.middleware = new Map();
    this.initializeServer();
  }

  initializeServer() {
    // Set up basic middleware
    this.setupBasicMiddleware();
    
    // Set up custom middleware
    this.setupCustomMiddleware();
    
    // Set up routes
    this.setupRoutes();
    
    // Set up static files
    this.setupStaticFiles();
    
    // Set up error handling
    this.setupErrorHandling();
  }

  setupBasicMiddleware() {
    // Body parsing middleware
    this.app.use(express.json());
    this.app.use(express.urlencoded({ extended: true }));
  }

  setupCustomMiddleware() {
    // CORS middleware
    if (this.config.cors?.enabled) {
      this.app.use(cors({
        origin: this.config.cors.origin,
        credentials: true
      }));
    }

    // Security middleware
    if (this.config.middleware?.includes('helmet')) {
      this.app.use(helmet());
    }

    // Compression middleware
    if (this.config.middleware?.includes('compression')) {
      this.app.use(compression());
    }

    // Rate limiting
    if (this.config.rateLimit) {
      const limiter = rateLimit({
        windowMs: this.config.rateLimit.windowMs,
        max: this.config.rateLimit.max,
        message: 'Too many requests from this IP'
      });
      this.app.use(limiter);
    }
  }

  setupRoutes() {
    if (!this.config.routes) return;

    Object.entries(this.config.routes).forEach(([path, routeConfig]) => {
      const method = routeConfig.method.toLowerCase();
      const handler = this.resolveHandler(routeConfig.handler);
      const middleware = this.resolveMiddleware(routeConfig.middleware || []);

      this.app[method](path, ...middleware, handler);
    });
  }

  resolveHandler(handlerPath) {
    // Simple handler resolution
    const handlers = {
      'userController.getUsers': (req, res) => {
        res.json([
          { id: 1, name: 'John Doe', email: 'john@example.com' },
          { id: 2, name: 'Jane Smith', email: 'jane@example.com' }
        ]);
      },
      'postController.createPost': (req, res) => {
        const { title, content } = req.body;
        res.json({
          id: Date.now(),
          title,
          content,
          createdAt: new Date()
        });
      }
    };

    return handlers[handlerPath] || ((req, res) => {
      res.status(404).json({ error: 'Handler not found' });
    });
  }

  resolveMiddleware(middlewareNames) {
    const middlewareMap = {
      'auth': (req, res, next) => {
        const token = req.headers.authorization;
        if (!token) {
          return res.status(401).json({ error: 'Authentication required' });
        }
        // Simple token validation
        if (token !== 'Bearer valid-token') {
          return res.status(401).json({ error: 'Invalid token' });
        }
        next();
      },
      'rateLimit': (req, res, next) => {
        // Custom rate limiting logic
        const clientId = req.ip;
        const now = Date.now();
        
        if (!this.rateLimitStore) {
          this.rateLimitStore = new Map();
        }
        
        const clientData = this.rateLimitStore.get(clientId) || { count: 0, resetTime: now + 60000 };
        
        if (now > clientData.resetTime) {
          clientData.count = 0;
          clientData.resetTime = now + 60000;
        }
        
        clientData.count++;
        this.rateLimitStore.set(clientId, clientData);
        
        if (clientData.count > 10) {
          return res.status(429).json({ error: 'Rate limit exceeded' });
        }
        
        next();
      },
      'validation': (req, res, next) => {
        const { title, content } = req.body;
        if (!title || !content) {
          return res.status(400).json({ error: 'Title and content are required' });
        }
        next();
      }
    };

    return middlewareNames.map(name => middlewareMap[name] || ((req, res, next) => next()));
  }

  setupStaticFiles() {
    if (!this.config.static) return;

    Object.entries(this.config.static).forEach(([route, path]) => {
      this.app.use(route, express.static(path));
    });
  }

  setupErrorHandling() {
    // 404 handler
    this.app.use((req, res) => {
      res.status(404).json({ error: 'Route not found' });
    });

    // Global error handler
    this.app.use((error, req, res, next) => {
      console.error('Error:', error);
      res.status(500).json({ error: 'Internal server error' });
    });
  }

  async start() {
    const port = this.config.port || 3000;
    const host = this.config.host || 'localhost';

    return new Promise((resolve, reject) => {
      this.server = this.app.listen(port, host, () => {
        console.log(`Server running at http://${host}:${port}`);
        resolve(this.server);
      });

      this.server.on('error', (error) => {
        console.error('Server error:', error);
        reject(error);
      });
    });
  }

  async stop() {
    if (this.server) {
      return new Promise((resolve) => {
        this.server.close(() => {
          console.log('Server stopped');
          resolve();
        });
      });
    }
  }

  // Health check endpoint
  addHealthCheck() {
    this.app.get('/health', (req, res) => {
      res.json({
        status: 'healthy',
        timestamp: new Date().toISOString(),
        uptime: process.uptime()
      });
    });
  }

  // Metrics endpoint
  addMetricsEndpoint() {
    this.app.get('/metrics', (req, res) => {
      res.json({
        requests: this.getRequestMetrics(),
        memory: process.memoryUsage(),
        cpu: process.cpuUsage()
      });
    });
  }

  getRequestMetrics() {
    // Simple request metrics
    return {
      total: this.requestCount || 0,
      active: this.activeRequests || 0
    };
  }
}

// Usage
const webServer = new WebServer(config);

// Start the server
async function startServer() {
  try {
    await webServer.start();
    
    // Add health check
    webServer.addHealthCheck();
    
    // Add metrics endpoint
    webServer.addMetricsEndpoint();
    
    console.log('Web server started successfully');
  } catch (error) {
    console.error('Failed to start server:', error);
  }
}

startServer();

// Graceful shutdown
process.on('SIGTERM', async () => {
  console.log('SIGTERM received, shutting down gracefully');
  await webServer.stop();
  process.exit(0);
});
```

### Browser Web Application
```javascript
// Browser-based web application
const browserConfig = tusklang.parse(`
browser_web: #web {
  baseUrl: "https://api.example.com",
  routes: {
    "/": {
      component: "HomePage",
      title: "Home"
    },
    "/users": {
      component: "UserList",
      title: "Users",
      requiresAuth: true
    },
    "/posts": {
      component: "PostList",
      title: "Posts"
    }
  },
  api: {
    endpoints: {
      users: "/api/users",
      posts: "/api/posts",
      auth: "/api/auth"
    }
  },
  auth: {
    enabled: true,
    tokenKey: "auth_token",
    loginUrl: "/login"
  }
}
`);

class BrowserWebApp {
  constructor(config) {
    this.config = config.browser_web;
    this.currentRoute = null;
    this.components = new Map();
    this.api = new ApiClient(this.config.api);
    this.auth = new AuthManager(this.config.auth);
    this.initializeApp();
  }

  initializeApp() {
    // Set up routing
    this.setupRouting();
    
    // Set up authentication
    this.setupAuthentication();
    
    // Set up components
    this.setupComponents();
    
    // Handle initial route
    this.handleRoute(window.location.pathname);
  }

  setupRouting() {
    // Handle browser navigation
    window.addEventListener('popstate', (event) => {
      this.handleRoute(window.location.pathname);
    });

    // Intercept link clicks
    document.addEventListener('click', (event) => {
      if (event.target.tagName === 'A' && event.target.href.startsWith(window.location.origin)) {
        event.preventDefault();
        const path = new URL(event.target.href).pathname;
        this.navigateTo(path);
      }
    });
  }

  setupAuthentication() {
    if (this.config.auth?.enabled) {
      // Check authentication on app start
      if (!this.auth.isAuthenticated()) {
        this.navigateTo(this.config.auth.loginUrl);
      }
    }
  }

  setupComponents() {
    // Register components
    this.components.set('HomePage', this.createHomePage());
    this.components.set('UserList', this.createUserList());
    this.components.set('PostList', this.createPostList());
    this.components.set('LoginPage', this.createLoginPage());
  }

  createHomePage() {
    return {
      render: () => {
        return `
          <div class="home-page">
            <h1>Welcome to Our App</h1>
            <p>This is the home page of our web application.</p>
            <nav>
              <a href="/users">Users</a>
              <a href="/posts">Posts</a>
            </nav>
          </div>
        `;
      }
    };
  }

  createUserList() {
    return {
      render: async () => {
        try {
          const users = await this.api.get('users');
          return `
            <div class="user-list">
              <h2>Users</h2>
              <div class="users">
                ${users.map(user => `
                  <div class="user-card">
                    <h3>${user.name}</h3>
                    <p>${user.email}</p>
                  </div>
                `).join('')}
              </div>
            </div>
          `;
        } catch (error) {
          return `<div class="error">Failed to load users: ${error.message}</div>`;
        }
      }
    };
  }

  createPostList() {
    return {
      render: async () => {
        try {
          const posts = await this.api.get('posts');
          return `
            <div class="post-list">
              <h2>Posts</h2>
              <div class="posts">
                ${posts.map(post => `
                  <div class="post-card">
                    <h3>${post.title}</h3>
                    <p>${post.content}</p>
                  </div>
                `).join('')}
              </div>
            </div>
          `;
        } catch (error) {
          return `<div class="error">Failed to load posts: ${error.message}</div>`;
        }
      }
    };
  }

  createLoginPage() {
    return {
      render: () => {
        return `
          <div class="login-page">
            <h2>Login</h2>
            <form id="login-form">
              <input type="email" name="email" placeholder="Email" required>
              <input type="password" name="password" placeholder="Password" required>
              <button type="submit">Login</button>
            </form>
          </div>
        `;
      },
      afterRender: () => {
        const form = document.getElementById('login-form');
        form.addEventListener('submit', async (event) => {
          event.preventDefault();
          const formData = new FormData(form);
          const email = formData.get('email');
          const password = formData.get('password');
          
          try {
            await this.auth.login(email, password);
            this.navigateTo('/');
          } catch (error) {
            alert('Login failed: ' + error.message);
          }
        });
      }
    };
  }

  async handleRoute(pathname) {
    const route = this.config.routes[pathname];
    
    if (!route) {
      this.showError('Page not found');
      return;
    }

    // Check authentication
    if (route.requiresAuth && !this.auth.isAuthenticated()) {
      this.navigateTo(this.config.auth.loginUrl);
      return;
    }

    // Update current route
    this.currentRoute = route;

    // Update page title
    document.title = route.title || 'Web App';

    // Render component
    const component = this.components.get(route.component);
    if (component) {
      const content = await component.render();
      this.renderContent(content);
      
      // Call afterRender if available
      if (component.afterRender) {
        component.afterRender();
      }
    } else {
      this.showError('Component not found');
    }
  }

  renderContent(content) {
    const app = document.getElementById('app');
    if (app) {
      app.innerHTML = content;
    }
  }

  navigateTo(path) {
    window.history.pushState({}, '', path);
    this.handleRoute(path);
  }

  showError(message) {
    this.renderContent(`
      <div class="error-page">
        <h2>Error</h2>
        <p>${message}</p>
        <a href="/">Go Home</a>
      </div>
    `);
  }
}

class ApiClient {
  constructor(config) {
    this.baseUrl = config.baseUrl || '';
    this.endpoints = config.endpoints || {};
  }

  async get(endpoint) {
    const url = this.baseUrl + this.endpoints[endpoint];
    const response = await fetch(url);
    
    if (!response.ok) {
      throw new Error(`API request failed: ${response.statusText}`);
    }
    
    return response.json();
  }

  async post(endpoint, data) {
    const url = this.baseUrl + this.endpoints[endpoint];
    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(data)
    });
    
    if (!response.ok) {
      throw new Error(`API request failed: ${response.statusText}`);
    }
    
    return response.json();
  }
}

class AuthManager {
  constructor(config) {
    this.config = config;
    this.tokenKey = config.tokenKey || 'auth_token';
  }

  isAuthenticated() {
    return !!localStorage.getItem(this.tokenKey);
  }

  async login(email, password) {
    // Simulate login API call
    const response = await fetch('/api/auth/login', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ email, password })
    });

    if (!response.ok) {
      throw new Error('Login failed');
    }

    const data = await response.json();
    localStorage.setItem(this.tokenKey, data.token);
  }

  logout() {
    localStorage.removeItem(this.tokenKey);
  }

  getToken() {
    return localStorage.getItem(this.tokenKey);
  }
}

// Usage
const webApp = new BrowserWebApp(browserConfig);
```

## Advanced Usage Scenarios

### Microservices Web Gateway
```tusk
#web {
  port: 8080,
  gateway: {
    services: {
      "user-service": {
        url: "http://user-service:3001",
        routes: ["/api/users/*"]
      },
      "post-service": {
        url: "http://post-service:3002",
        routes: ["/api/posts/*"]
      }
    }
  },
  loadBalancer: {
    strategy: "round-robin",
    healthCheck: true
  }
}
```

### WebSocket Server
```tusk
#web {
  port: 3000,
  websocket: {
    enabled: true,
    path: "/ws",
    events: {
      "connection": "handleConnection",
      "message": "handleMessage",
      "disconnect": "handleDisconnect"
    }
  }
}
```

### Static Site Generator
```tusk
#web {
  static: {
    "/": "./dist",
    "/assets": "./assets"
  },
  spa: {
    enabled: true,
    fallback: "index.html"
  },
  compression: {
    enabled: true,
    level: 6
  }
}
```

## TypeScript Implementation

### Typed Web Server
```typescript
interface WebConfig {
  port?: number;
  host?: string;
  protocol?: 'http' | 'https';
  cors?: {
    enabled: boolean;
    origin: string[];
  };
  routes?: Record<string, RouteConfig>;
  static?: Record<string, string>;
  middleware?: string[];
  rateLimit?: {
    windowMs: number;
    max: number;
  };
}

interface RouteConfig {
  method: string;
  handler: string;
  middleware?: string[];
}

class TypedWebServer {
  private config: WebConfig;
  private app: any;
  private server: any;

  constructor(config: WebConfig) {
    this.config = config;
    this.app = express();
    this.initializeServer();
  }

  private initializeServer(): void {
    this.setupMiddleware();
    this.setupRoutes();
    this.setupStaticFiles();
  }

  private setupMiddleware(): void {
    // Implementation for middleware setup
  }

  private setupRoutes(): void {
    // Implementation for route setup
  }

  private setupStaticFiles(): void {
    // Implementation for static file setup
  }

  async start(): Promise<void> {
    const port = this.config.port || 3000;
    const host = this.config.host || 'localhost';

    return new Promise((resolve, reject) => {
      this.server = this.app.listen(port, host, () => {
        console.log(`Server running at http://${host}:${port}`);
        resolve();
      });

      this.server.on('error', reject);
    });
  }

  async stop(): Promise<void> {
    if (this.server) {
      return new Promise((resolve) => {
        this.server.close(() => resolve());
      });
    }
  }
}
```

## Real-World Examples

### Express.js with TuskLang Config
```javascript
// Express.js application with TuskLang web configuration
const express = require('express');
const tusklang = require('@tusklang/core');

const config = tusklang.parse(`
app_config: #web {
  port: @env("PORT", 3000),
  cors: {
    enabled: true,
    origin: ["http://localhost:3000"]
  },
  routes: {
    "/api/health": {
      method: "GET",
      handler: "healthController.check"
    }
  }
}
`);

const app = express();
const webConfig = config.app_config;

// Apply CORS if enabled
if (webConfig.cors?.enabled) {
  app.use(cors({ origin: webConfig.cors.origin }));
}

// Apply routes
Object.entries(webConfig.routes).forEach(([path, route]) => {
  app[route.method.toLowerCase()](path, (req, res) => {
    res.json({ status: 'healthy', timestamp: new Date() });
  });
});

app.listen(webConfig.port, () => {
  console.log(`Server running on port ${webConfig.port}`);
});
```

### React App with Web Config
```javascript
// React application with TuskLang web configuration
import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';

const webConfig = {
  routes: {
    "/": { component: "HomePage", title: "Home" },
    "/users": { component: "UserList", title: "Users" },
    "/posts": { component: "PostList", title: "Posts" }
  }
};

const App = () => {
  return (
    <BrowserRouter>
      <Routes>
        {Object.entries(webConfig.routes).map(([path, route]) => (
          <Route
            key={path}
            path={path}
            element={<route.component />}
          />
        ))}
      </Routes>
    </BrowserRouter>
  );
};
```

## Performance Considerations
- Use compression middleware for large responses
- Implement caching for static assets
- Use connection pooling for database connections
- Monitor request/response times

## Security Notes
- Always use HTTPS in production
- Implement proper CORS policies
- Use security middleware (helmet, etc.)
- Validate and sanitize all inputs

## Best Practices
- Use environment variables for configuration
- Implement proper error handling
- Add health check endpoints
- Use logging for debugging

## Related Topics
- [@http Operator](./60-http-operator.md) - HTTP requests
- [@websocket Operator](./61-websocket-operator.md) - WebSocket connections
- [Middleware](./15-middleware.md) - Middleware patterns
- [Security](./14-security.md) - Security best practices 