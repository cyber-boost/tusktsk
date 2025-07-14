# #middleware Directive - Middleware Configuration

## Overview
The `#middleware` directive in TuskLang provides comprehensive middleware configuration capabilities, enabling you to define request/response middleware, authentication, logging, error handling, and custom processing pipelines in a declarative manner.

## TuskLang Syntax

### Basic Middleware
```tusk
#middleware {
  name: "auth",
  handler: "authMiddleware.verifyToken",
  order: 1,
  enabled: true
}
```

### Middleware with Options
```tusk
#middleware {
  name: "rate-limit",
  handler: "rateLimitMiddleware.checkLimit",
  order: 2,
  options: {
    windowMs: 15 * 60 * 1000,
    max: 100,
    message: "Too many requests"
  },
  enabled: true
}
```

### Middleware Pipeline
```tusk
#middleware {
  pipeline: [
    {
      name: "cors",
      handler: "corsMiddleware.handleCors",
      order: 1
    },
    {
      name: "helmet",
      handler: "helmetMiddleware.secureHeaders",
      order: 2
    },
    {
      name: "auth",
      handler: "authMiddleware.verifyToken",
      order: 3,
      skip: ["/public/*", "/health"]
    },
    {
      name: "rate-limit",
      handler: "rateLimitMiddleware.checkLimit",
      order: 4
    }
  ]
}
```

## JavaScript Integration

### Node.js Middleware System
```javascript
const tusklang = require('@tusklang/core');
const express = require('express');
const cors = require('cors');
const helmet = require('helmet');
const rateLimit = require('express-rate-limit');

const config = tusklang.parse(`
middleware_config: #middleware {
  pipeline: [
    {
      name: "cors",
      handler: "corsMiddleware.handleCors",
      order: 1,
      options: {
        origin: ["http://localhost:3000", "https://example.com"],
        credentials: true
      }
    },
    {
      name: "helmet",
      handler: "helmetMiddleware.secureHeaders",
      order: 2,
      options: {
        contentSecurityPolicy: {
          directives: {
            defaultSrc: ["'self'"],
            styleSrc: ["'self'", "'unsafe-inline'"],
            scriptSrc: ["'self'"]
          }
        }
      }
    },
    {
      name: "request-logger",
      handler: "loggingMiddleware.logRequest",
      order: 3,
      options: {
        format: "combined",
        level: "info"
      }
    },
    {
      name: "auth",
      handler: "authMiddleware.verifyToken",
      order: 4,
      skip: ["/public/*", "/health", "/api/auth/*"],
      options: {
        secret: @env("JWT_SECRET"),
        algorithms: ["HS256"]
      }
    },
    {
      name: "rate-limit",
      handler: "rateLimitMiddleware.checkLimit",
      order: 5,
      options: {
        windowMs: 15 * 60 * 1000,
        max: 100,
        message: "Too many requests from this IP"
      }
    },
    {
      name: "validation",
      handler: "validationMiddleware.validateRequest",
      order: 6,
      options: {
        schemas: {
          user: "userSchema",
          post: "postSchema"
        }
      }
    }
  ]
}
`);

class MiddlewareManager {
  constructor(config) {
    this.config = config.middleware_config;
    this.middleware = new Map();
    this.handlers = new Map();
    this.pipeline = [];
    this.initializeMiddleware();
  }

  initializeMiddleware() {
    // Register middleware handlers
    this.registerHandlers();
    
    // Create middleware pipeline
    this.createPipeline();
    
    // Set up error handling
    this.setupErrorHandling();
  }

  registerHandlers() {
    // CORS middleware
    this.handlers.set('corsMiddleware.handleCors', (options) => {
      return cors({
        origin: options.origin || true,
        credentials: options.credentials || false,
        methods: options.methods || ['GET', 'POST', 'PUT', 'DELETE'],
        allowedHeaders: options.allowedHeaders || ['Content-Type', 'Authorization']
      });
    });

    // Helmet middleware
    this.handlers.set('helmetMiddleware.secureHeaders', (options) => {
      return helmet({
        contentSecurityPolicy: options.contentSecurityPolicy,
        hsts: options.hsts || { maxAge: 31536000, includeSubDomains: true },
        noSniff: true,
        xssFilter: true
      });
    });

    // Request logging middleware
    this.handlers.set('loggingMiddleware.logRequest', (options) => {
      return (req, res, next) => {
        const start = Date.now();
        
        res.on('finish', () => {
          const duration = Date.now() - start;
          const logLevel = options.level || 'info';
          
          console.log(`[${logLevel.toUpperCase()}] ${req.method} ${req.path} ${res.statusCode} ${duration}ms`);
          
          if (options.format === 'detailed') {
            console.log(`  User-Agent: ${req.get('User-Agent')}`);
            console.log(`  IP: ${req.ip}`);
            console.log(`  User: ${req.user?.id || 'anonymous'}`);
          }
        });
        
        next();
      };
    });

    // Authentication middleware
    this.handlers.set('authMiddleware.verifyToken', (options) => {
      return (req, res, next) => {
        const token = req.headers.authorization?.replace('Bearer ', '');
        
        if (!token) {
          return res.status(401).json({ error: 'Authentication required' });
        }
        
        try {
          // Simple JWT verification (use proper JWT library in production)
          const decoded = this.verifyJWT(token, options.secret);
          req.user = decoded;
          next();
        } catch (error) {
          return res.status(401).json({ error: 'Invalid token' });
        }
      };
    });

    // Rate limiting middleware
    this.handlers.set('rateLimitMiddleware.checkLimit', (options) => {
      return rateLimit({
        windowMs: options.windowMs || 15 * 60 * 1000,
        max: options.max || 100,
        message: options.message || 'Too many requests from this IP',
        standardHeaders: true,
        legacyHeaders: false
      });
    });

    // Validation middleware
    this.handlers.set('validationMiddleware.validateRequest', (options) => {
      return (req, res, next) => {
        const schema = this.getValidationSchema(req.path, options.schemas);
        
        if (schema) {
          const validation = this.validateRequest(req, schema);
          if (!validation.isValid) {
            return res.status(400).json({ 
              error: 'Validation failed', 
              details: validation.errors 
            });
          }
        }
        
        next();
      };
    });
  }

  createPipeline() {
    if (!this.config.pipeline) return;

    // Sort middleware by order
    const sortedMiddleware = this.config.pipeline.sort((a, b) => a.order - b.order);
    
    sortedMiddleware.forEach(middlewareConfig => {
      this.createMiddleware(middlewareConfig);
    });
  }

  createMiddleware(config) {
    if (!config.enabled) {
      console.log(`Middleware '${config.name}' is disabled`);
      return;
    }

    const handler = this.handlers.get(config.handler);
    if (!handler) {
      console.error(`Handler '${config.handler}' not found for middleware '${config.name}'`);
      return;
    }

    const middleware = {
      name: config.name,
      handler: handler(config.options || {}),
      skip: config.skip || [],
      order: config.order
    };

    this.middleware.set(config.name, middleware);
    this.pipeline.push(middleware);
    
    console.log(`Registered middleware '${config.name}' (order: ${config.order})`);
  }

  applyMiddleware(app) {
    this.pipeline.forEach(middleware => {
      if (middleware.skip && middleware.skip.length > 0) {
        // Apply middleware with skip conditions
        app.use((req, res, next) => {
          if (this.shouldSkipMiddleware(req.path, middleware.skip)) {
            return next();
          }
          middleware.handler(req, res, next);
        });
      } else {
        // Apply middleware normally
        app.use(middleware.handler);
      }
    });
  }

  shouldSkipMiddleware(path, skipPatterns) {
    return skipPatterns.some(pattern => {
      if (pattern.endsWith('*')) {
        const prefix = pattern.slice(0, -1);
        return path.startsWith(prefix);
      }
      return path === pattern;
    });
  }

  setupErrorHandling() {
    // Global error handling middleware
    this.errorHandler = (error, req, res, next) => {
      console.error('Error:', error);
      
      // Log error details
      console.error(`  Method: ${req.method}`);
      console.error(`  Path: ${req.path}`);
      console.error(`  User: ${req.user?.id || 'anonymous'}`);
      console.error(`  IP: ${req.ip}`);
      
      // Send appropriate error response
      if (error.name === 'ValidationError') {
        return res.status(400).json({ error: 'Validation failed', details: error.details });
      }
      
      if (error.name === 'UnauthorizedError') {
        return res.status(401).json({ error: 'Unauthorized' });
      }
      
      if (error.name === 'RateLimitError') {
        return res.status(429).json({ error: 'Rate limit exceeded' });
      }
      
      // Default error response
      res.status(500).json({ error: 'Internal server error' });
    };
  }

  // Utility methods
  verifyJWT(token, secret) {
    // Simple JWT verification (use proper JWT library in production)
    if (token === 'valid-token') {
      return { id: 1, username: 'user', role: 'user' };
    }
    throw new Error('Invalid token');
  }

  getValidationSchema(path, schemas) {
    if (path.startsWith('/api/users') && schemas.user) {
      return schemas.user;
    }
    if (path.startsWith('/api/posts') && schemas.post) {
      return schemas.post;
    }
    return null;
  }

  validateRequest(req, schema) {
    // Simple validation (use proper validation library in production)
    const errors = [];
    
    if (schema === 'userSchema') {
      const { name, email } = req.body;
      if (!name) errors.push('Name is required');
      if (!email) errors.push('Email is required');
      if (email && !email.includes('@')) errors.push('Invalid email format');
    }
    
    if (schema === 'postSchema') {
      const { title, content } = req.body;
      if (!title) errors.push('Title is required');
      if (!content) errors.push('Content is required');
    }
    
    return {
      isValid: errors.length === 0,
      errors: errors
    };
  }

  // Middleware management
  addMiddleware(name, handler, options = {}) {
    const middleware = {
      name: name,
      handler: handler,
      skip: options.skip || [],
      order: options.order || this.pipeline.length + 1
    };

    this.middleware.set(name, middleware);
    this.pipeline.push(middleware);
    
    // Re-sort pipeline by order
    this.pipeline.sort((a, b) => a.order - b.order);
    
    console.log(`Added custom middleware '${name}'`);
  }

  removeMiddleware(name) {
    const middleware = this.middleware.get(name);
    if (middleware) {
      this.middleware.delete(name);
      this.pipeline = this.pipeline.filter(m => m.name !== name);
      console.log(`Removed middleware '${name}'`);
    }
  }

  getMiddleware(name) {
    return this.middleware.get(name);
  }

  getAllMiddleware() {
    return Array.from(this.middleware.values());
  }

  enableMiddleware(name) {
    const middleware = this.middleware.get(name);
    if (middleware) {
      middleware.enabled = true;
      console.log(`Enabled middleware '${name}'`);
    }
  }

  disableMiddleware(name) {
    const middleware = this.middleware.get(name);
    if (middleware) {
      middleware.enabled = false;
      console.log(`Disabled middleware '${name}'`);
    }
  }
}

// Usage
const middlewareManager = new MiddlewareManager(config);

// Create Express app
const app = express();

// Apply middleware pipeline
middlewareManager.applyMiddleware(app);

// Add error handling middleware
app.use(middlewareManager.errorHandler);

// Example routes
app.get('/health', (req, res) => {
  res.json({ status: 'healthy', timestamp: new Date() });
});

app.get('/api/users', (req, res) => {
  res.json([
    { id: 1, name: 'John Doe', email: 'john@example.com' },
    { id: 2, name: 'Jane Smith', email: 'jane@example.com' }
  ]);
});

app.post('/api/users', (req, res) => {
  const user = {
    id: Date.now(),
    ...req.body,
    createdAt: new Date()
  };
  res.status(201).json(user);
});

// Start server
const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
  console.log(`Server running on port ${PORT}`);
  console.log('Middleware pipeline:', middlewareManager.getAllMiddleware().map(m => m.name));
});
```

### Browser Middleware System
```javascript
// Browser-based middleware system
const browserConfig = tusklang.parse(`
browser_middleware: #middleware {
  pipeline: [
    {
      name: "request-interceptor",
      handler: "requestMiddleware.intercept",
      order: 1,
      options: {
        baseUrl: "https://api.example.com",
        timeout: 5000
      }
    },
    {
      name: "auth-interceptor",
      handler: "authMiddleware.addToken",
      order: 2,
      options: {
        tokenKey: "auth_token"
      }
    },
    {
      name: "response-interceptor",
      handler: "responseMiddleware.handleResponse",
      order: 3,
      options: {
        handleErrors: true,
        retryOnFailure: true
      }
    }
  ]
}
`);

class BrowserMiddlewareManager {
  constructor(config) {
    this.config = config.browser_middleware;
    this.middleware = new Map();
    this.pipeline = [];
    this.initializeMiddleware();
  }

  initializeMiddleware() {
    this.registerHandlers();
    this.createPipeline();
  }

  registerHandlers() {
    // Request interceptor middleware
    this.handlers.set('requestMiddleware.intercept', (options) => {
      return (request) => {
        // Add base URL if not present
        if (!request.url.startsWith('http')) {
          request.url = options.baseUrl + request.url;
        }
        
        // Add default timeout
        if (!request.timeout) {
          request.timeout = options.timeout || 5000;
        }
        
        // Add default headers
        request.headers = {
          'Content-Type': 'application/json',
          ...request.headers
        };
        
        console.log(`[REQUEST] ${request.method} ${request.url}`);
        return request;
      };
    });

    // Auth interceptor middleware
    this.handlers.set('authMiddleware.addToken', (options) => {
      return (request) => {
        const token = localStorage.getItem(options.tokenKey);
        
        if (token) {
          request.headers = {
            ...request.headers,
            'Authorization': `Bearer ${token}`
          };
        }
        
        return request;
      };
    });

    // Response interceptor middleware
    this.handlers.set('responseMiddleware.handleResponse', (options) => {
      return (response, request) => {
        console.log(`[RESPONSE] ${request.method} ${request.url} ${response.status}`);
        
        if (options.handleErrors && !response.ok) {
          this.handleError(response, request, options);
        }
        
        return response;
      };
    });
  }

  createPipeline() {
    if (!this.config.pipeline) return;

    const sortedMiddleware = this.config.pipeline.sort((a, b) => a.order - b.order);
    
    sortedMiddleware.forEach(middlewareConfig => {
      this.createMiddleware(middlewareConfig);
    });
  }

  createMiddleware(config) {
    const handler = this.handlers.get(config.handler);
    if (!handler) {
      console.error(`Handler '${config.handler}' not found`);
      return;
    }

    const middleware = {
      name: config.name,
      handler: handler(config.options || {}),
      order: config.order
    };

    this.middleware.set(config.name, middleware);
    this.pipeline.push(middleware);
  }

  async executePipeline(request) {
    let currentRequest = { ...request };
    
    // Execute request middleware
    for (const middleware of this.pipeline) {
      currentRequest = middleware.handler(currentRequest);
    }
    
    // Make the actual request
    const response = await this.makeRequest(currentRequest);
    
    // Execute response middleware
    let currentResponse = response;
    for (const middleware of this.pipeline) {
      currentResponse = middleware.handler(currentResponse, currentRequest);
    }
    
    return currentResponse;
  }

  async makeRequest(request) {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), request.timeout);
    
    try {
      const response = await fetch(request.url, {
        method: request.method,
        headers: request.headers,
        body: request.body ? JSON.stringify(request.body) : undefined,
        signal: controller.signal
      });
      
      clearTimeout(timeoutId);
      return response;
    } catch (error) {
      clearTimeout(timeoutId);
      throw error;
    }
  }

  handleError(response, request, options) {
    if (response.status === 401) {
      // Handle unauthorized
      localStorage.removeItem('auth_token');
      window.location.href = '/login';
    } else if (response.status === 403) {
      // Handle forbidden
      console.error('Access forbidden');
    } else if (response.status >= 500) {
      // Handle server errors
      console.error('Server error occurred');
    }
  }

  // API client with middleware
  async get(url, options = {}) {
    return this.executePipeline({
      method: 'GET',
      url: url,
      ...options
    });
  }

  async post(url, data, options = {}) {
    return this.executePipeline({
      method: 'POST',
      url: url,
      body: data,
      ...options
    });
  }

  async put(url, data, options = {}) {
    return this.executePipeline({
      method: 'PUT',
      url: url,
      body: data,
      ...options
    });
  }

  async delete(url, options = {}) {
    return this.executePipeline({
      method: 'DELETE',
      url: url,
      ...options
    });
  }
}

// Usage
const browserMiddleware = new BrowserMiddlewareManager(browserConfig);

// Example API calls
async function fetchUsers() {
  try {
    const response = await browserMiddleware.get('/api/users');
    const users = await response.json();
    console.log('Users:', users);
  } catch (error) {
    console.error('Failed to fetch users:', error);
  }
}

async function createUser(userData) {
  try {
    const response = await browserMiddleware.post('/api/users', userData);
    const user = await response.json();
    console.log('Created user:', user);
  } catch (error) {
    console.error('Failed to create user:', error);
  }
}

// Use the API client
fetchUsers();
createUser({ name: 'John Doe', email: 'john@example.com' });
```

## Advanced Usage Scenarios

### Custom Middleware
```tusk
#middleware {
  pipeline: [
    {
      name: "custom-logger",
      handler: "customMiddleware.logRequest",
      order: 1,
      options: {
        logLevel: "debug",
        includeHeaders: true
      }
    },
    {
      name: "custom-auth",
      handler: "customMiddleware.customAuth",
      order: 2,
      options: {
        authType: "api-key",
        keyHeader: "X-API-Key"
      }
    }
  ]
}
```

### Conditional Middleware
```tusk
#middleware {
  pipeline: [
    {
      name: "conditional-auth",
      handler: "authMiddleware.conditionalAuth",
      order: 1,
      conditions: {
        method: ["POST", "PUT", "DELETE"],
        path: "/api/*"
      }
    }
  ]
}
```

### Middleware Composition
```tusk
#middleware {
  pipeline: [
    {
      name: "composed-middleware",
      handler: "composedMiddleware.execute",
      order: 1,
      composition: [
        "loggingMiddleware.logRequest",
        "authMiddleware.verifyToken",
        "validationMiddleware.validateRequest"
      ]
    }
  ]
}
```

## TypeScript Implementation

### Typed Middleware Manager
```typescript
interface MiddlewareConfig {
  pipeline?: MiddlewareItem[];
}

interface MiddlewareItem {
  name: string;
  handler: string;
  order: number;
  options?: any;
  skip?: string[];
  enabled?: boolean;
}

interface Middleware {
  name: string;
  handler: Function;
  skip: string[];
  order: number;
}

class TypedMiddlewareManager {
  private config: MiddlewareConfig;
  private middleware: Map<string, Middleware> = new Map();
  private handlers: Map<string, Function> = new Map();
  private pipeline: Middleware[] = [];

  constructor(config: MiddlewareConfig) {
    this.config = config;
    this.initializeMiddleware();
  }

  private initializeMiddleware(): void {
    this.registerHandlers();
    this.createPipeline();
  }

  private registerHandlers(): void {
    // Implementation for handler registration
  }

  private createPipeline(): void {
    // Implementation for pipeline creation
  }

  applyMiddleware(app: any): void {
    // Implementation for applying middleware
  }

  addMiddleware(name: string, handler: Function, options: any = {}): void {
    // Implementation for adding middleware
  }

  removeMiddleware(name: string): void {
    // Implementation for removing middleware
  }
}
```

## Real-World Examples

### Express.js with Custom Middleware
```javascript
// Express.js with custom middleware
const express = require('express');
const tusklang = require('@tusklang/core');

const config = tusklang.parse(`
app_middleware: #middleware {
  pipeline: [
    {
      name: "custom-logger",
      handler: "customMiddleware.logRequest",
      order: 1
    },
    {
      name: "custom-auth",
      handler: "customMiddleware.customAuth",
      order: 2
    }
  ]
}
`);

const app = express();
const middlewareManager = new MiddlewareManager(config);

// Register custom handlers
middlewareManager.handlers.set('customMiddleware.logRequest', () => {
  return (req, res, next) => {
    console.log(`[CUSTOM] ${req.method} ${req.path} at ${new Date()}`);
    next();
  };
});

middlewareManager.handlers.set('customMiddleware.customAuth', () => {
  return (req, res, next) => {
    const apiKey = req.headers['x-api-key'];
    if (!apiKey || apiKey !== 'valid-key') {
      return res.status(401).json({ error: 'Invalid API key' });
    }
    next();
  };
});

middlewareManager.applyMiddleware(app);

app.get('/api/data', (req, res) => {
  res.json({ data: 'protected data' });
});

app.listen(3000, () => {
  console.log('Server running with custom middleware');
});
```

### React with API Middleware
```javascript
// React with API middleware
import React, { useState, useEffect } from 'react';

const apiMiddleware = new BrowserMiddlewareManager(browserConfig);

const UserList = () => {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchUsers = async () => {
      try {
        const response = await apiMiddleware.get('/api/users');
        const data = await response.json();
        setUsers(data);
      } catch (error) {
        console.error('Failed to fetch users:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchUsers();
  }, []);

  if (loading) return <div>Loading...</div>;

  return (
    <div>
      <h2>Users</h2>
      {users.map(user => (
        <div key={user.id}>
          <h3>{user.name}</h3>
          <p>{user.email}</p>
        </div>
      ))}
    </div>
  );
};
```

## Performance Considerations
- Use middleware sparingly to avoid performance overhead
- Implement middleware caching for frequently used operations
- Use async middleware for I/O operations
- Monitor middleware execution times

## Security Notes
- Always validate and sanitize inputs in middleware
- Implement proper authentication and authorization
- Use secure defaults for sensitive operations
- Implement proper error handling

## Best Practices
- Keep middleware focused and single-purpose
- Use descriptive middleware names
- Implement proper logging and monitoring
- Test middleware thoroughly

## Related Topics
- [@web Directive](./76-hash-web-directive.md) - Web application configuration
- [@api Directive](./77-hash-api-directive.md) - API configuration
- [Authentication](./14-security.md) - Security and authentication
- [Error Handling](./13-error-handling.md) - Error handling patterns 