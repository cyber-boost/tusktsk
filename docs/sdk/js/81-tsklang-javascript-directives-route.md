# TuskLang JavaScript Documentation: #route Directive

## Overview

The `#route` directive in TuskLang defines routing patterns and handlers for web applications, enabling declarative route configuration with JavaScript/Node.js integration.

## TuskLang Syntax

```tsk
#route /api/users
  method: GET
  handler: getUsers
  middleware: [auth, rateLimit]
  cache: 300
  cors: true

#route /api/users/:id
  method: PUT
  handler: updateUser
  middleware: [auth, validateUser]
  rate_limit: 100/hour
  validation:
    id: number
    name: string
    email: email

#route /api/posts/*/comments
  method: POST
  handler: addComment
  middleware: [auth, validatePost]
  websocket: true
  realtime: true
```

## JavaScript Integration

### Express.js Integration

```javascript
// route-handler.js
const express = require('express');
const { parseTuskRoutes } = require('@tusklang/route-parser');

class RouteHandler {
  constructor() {
    this.app = express();
    this.routes = new Map();
  }

  async loadRoutes(tuskConfig) {
    const routes = await parseTuskRoutes(tuskConfig);
    
    routes.forEach(route => {
      this.registerRoute(route);
    });
  }

  registerRoute(route) {
    const { path, method, handler, middleware, options } = route;
    
    const routeMiddleware = this.buildMiddleware(middleware);
    const routeHandler = this.buildHandler(handler, options);
    
    this.app[method.toLowerCase()](path, routeMiddleware, routeHandler);
  }

  buildMiddleware(middlewareNames) {
    return middlewareNames.map(name => {
      switch (name) {
        case 'auth':
          return this.authMiddleware;
        case 'rateLimit':
          return this.rateLimitMiddleware;
        case 'validateUser':
          return this.validateUserMiddleware;
        default:
          return (req, res, next) => next();
      }
    });
  }

  buildHandler(handlerName, options) {
    return async (req, res) => {
      try {
        // Apply route-specific options
        if (options.cache) {
          res.set('Cache-Control', `public, max-age=${options.cache}`);
        }
        
        if (options.cors) {
          res.set('Access-Control-Allow-Origin', '*');
        }

        // Execute handler
        const handler = this.getHandler(handlerName);
        const result = await handler(req, res);
        
        res.json(result);
      } catch (error) {
        res.status(500).json({ error: error.message });
      }
    };
  }

  getHandler(name) {
    const handlers = {
      getUsers: async (req, res) => {
        return { users: await User.findAll() };
      },
      updateUser: async (req, res) => {
        const { id } = req.params;
        const updates = req.body;
        return await User.update(id, updates);
      },
      addComment: async (req, res) => {
        const { postId } = req.params;
        const comment = req.body;
        return await Comment.create({ ...comment, postId });
      }
    };
    
    return handlers[name] || (() => ({ error: 'Handler not found' }));
  }

  // Middleware implementations
  authMiddleware(req, res, next) {
    const token = req.headers.authorization;
    if (!token) {
      return res.status(401).json({ error: 'Unauthorized' });
    }
    next();
  }

  rateLimitMiddleware(req, res, next) {
    // Rate limiting logic
    next();
  }

  validateUserMiddleware(req, res, next) {
    const { id } = req.params;
    if (!id || isNaN(id)) {
      return res.status(400).json({ error: 'Invalid user ID' });
    }
    next();
  }
}

module.exports = RouteHandler;
```

### Fastify Integration

```javascript
// fastify-route-handler.js
const fastify = require('fastify');
const { parseTuskRoutes } = require('@tusklang/route-parser');

class FastifyRouteHandler {
  constructor() {
    this.app = fastify();
    this.routes = new Map();
  }

  async loadRoutes(tuskConfig) {
    const routes = await parseTuskRoutes(tuskConfig);
    
    for (const route of routes) {
      await this.registerRoute(route);
    }
  }

  async registerRoute(route) {
    const { path, method, handler, middleware, options } = route;
    
    const schema = this.buildSchema(options.validation);
    const preHandler = this.buildPreHandler(middleware);
    
    this.app[method.toLowerCase()]({
      url: path,
      schema,
      preHandler,
      handler: this.buildHandler(handler, options)
    });
  }

  buildSchema(validation) {
    if (!validation) return {};
    
    const properties = {};
    const required = [];
    
    Object.entries(validation).forEach(([key, type]) => {
      properties[key] = this.getSchemaType(type);
      required.push(key);
    });
    
    return {
      body: {
        type: 'object',
        properties,
        required
      }
    };
  }

  getSchemaType(type) {
    const types = {
      string: { type: 'string' },
      number: { type: 'number' },
      email: { type: 'string', format: 'email' },
      boolean: { type: 'boolean' }
    };
    
    return types[type] || { type: 'string' };
  }

  buildPreHandler(middlewareNames) {
    return middlewareNames.map(name => {
      switch (name) {
        case 'auth':
          return this.authPreHandler;
        case 'rateLimit':
          return this.rateLimitPreHandler;
        default:
          return async (request, reply) => {};
      }
    });
  }

  buildHandler(handlerName, options) {
    return async (request, reply) => {
      try {
        // Apply route-specific options
        if (options.cache) {
          reply.header('Cache-Control', `public, max-age=${options.cache}`);
        }
        
        if (options.cors) {
          reply.header('Access-Control-Allow-Origin', '*');
        }

        // Execute handler
        const handler = this.getHandler(handlerName);
        const result = await handler(request, reply);
        
        return result;
      } catch (error) {
        reply.status(500).send({ error: error.message });
      }
    };
  }

  getHandler(name) {
    const handlers = {
      getUsers: async (request, reply) => {
        return { users: await User.findAll() };
      },
      updateUser: async (request, reply) => {
        const { id } = request.params;
        const updates = request.body;
        return await User.update(id, updates);
      },
      addComment: async (request, reply) => {
        const { postId } = request.params;
        const comment = request.body;
        return await Comment.create({ ...comment, postId });
      }
    };
    
    return handlers[name] || (() => ({ error: 'Handler not found' }));
  }

  // Pre-handler implementations
  async authPreHandler(request, reply) {
    const token = request.headers.authorization;
    if (!token) {
      reply.status(401).send({ error: 'Unauthorized' });
    }
  }

  async rateLimitPreHandler(request, reply) {
    // Rate limiting logic
  }
}

module.exports = FastifyRouteHandler;
```

## TypeScript Implementation

```typescript
// route-handler.types.ts
export interface TuskRoute {
  path: string;
  method: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH';
  handler: string;
  middleware?: string[];
  cache?: number;
  cors?: boolean;
  rate_limit?: string;
  validation?: Record<string, string>;
  websocket?: boolean;
  realtime?: boolean;
}

export interface RouteOptions {
  cache?: number;
  cors?: boolean;
  rate_limit?: string;
  validation?: Record<string, string>;
  websocket?: boolean;
  realtime?: boolean;
}

export interface RouteHandler {
  (req: any, res: any): Promise<any>;
}

export interface Middleware {
  (req: any, res: any, next: () => void): void | Promise<void>;
}

// route-handler.ts
import { TuskRoute, RouteOptions, RouteHandler, Middleware } from './route-handler.types';

export class TypeScriptRouteHandler {
  private app: any;
  private routes: Map<string, TuskRoute> = new Map();
  private handlers: Map<string, RouteHandler> = new Map();
  private middleware: Map<string, Middleware> = new Map();

  constructor(app: any) {
    this.app = app;
    this.initializeMiddleware();
  }

  private initializeMiddleware(): void {
    this.middleware.set('auth', this.authMiddleware.bind(this));
    this.middleware.set('rateLimit', this.rateLimitMiddleware.bind(this));
    this.middleware.set('validateUser', this.validateUserMiddleware.bind(this));
  }

  async loadRoutes(tuskConfig: string): Promise<void> {
    const routes = await this.parseTuskRoutes(tuskConfig);
    
    routes.forEach(route => {
      this.registerRoute(route);
    });
  }

  private async parseTuskRoutes(config: string): Promise<TuskRoute[]> {
    // Implementation of TuskLang route parsing
    const routes: TuskRoute[] = [];
    
    const lines = config.split('\n');
    let currentRoute: Partial<TuskRoute> = {};
    
    for (const line of lines) {
      if (line.startsWith('#route')) {
        if (currentRoute.path) {
          routes.push(currentRoute as TuskRoute);
        }
        currentRoute = { path: line.split(' ')[1] };
      } else if (line.trim() && currentRoute.path) {
        const [key, value] = line.split(':').map(s => s.trim());
        this.parseRouteProperty(currentRoute, key, value);
      }
    }
    
    if (currentRoute.path) {
      routes.push(currentRoute as TuskRoute);
    }
    
    return routes;
  }

  private parseRouteProperty(route: Partial<TuskRoute>, key: string, value: string): void {
    switch (key) {
      case 'method':
        route.method = value as any;
        break;
      case 'handler':
        route.handler = value;
        break;
      case 'middleware':
        route.middleware = value.replace(/[\[\]]/g, '').split(',').map(s => s.trim());
        break;
      case 'cache':
        route.cache = parseInt(value);
        break;
      case 'cors':
        route.cors = value === 'true';
        break;
      case 'rate_limit':
        route.rate_limit = value;
        break;
      case 'validation':
        route.validation = this.parseValidation(value);
        break;
      case 'websocket':
        route.websocket = value === 'true';
        break;
      case 'realtime':
        route.realtime = value === 'true';
        break;
    }
  }

  private parseValidation(value: string): Record<string, string> {
    const validation: Record<string, string> = {};
    const pairs = value.split(',').map(s => s.trim());
    
    pairs.forEach(pair => {
      const [key, type] = pair.split(':').map(s => s.trim());
      if (key && type) {
        validation[key] = type;
      }
    });
    
    return validation;
  }

  private registerRoute(route: TuskRoute): void {
    const { path, method, handler, middleware = [], options } = route;
    
    const routeMiddleware = this.buildMiddleware(middleware);
    const routeHandler = this.buildHandler(handler, route);
    
    this.app[method.toLowerCase()](path, routeMiddleware, routeHandler);
  }

  private buildMiddleware(middlewareNames: string[]): Middleware[] {
    return middlewareNames.map(name => {
      return this.middleware.get(name) || ((req, res, next) => next());
    });
  }

  private buildHandler(handlerName: string, route: TuskRoute): RouteHandler {
    return async (req: any, res: any) => {
      try {
        // Apply route-specific options
        if (route.cache) {
          res.set('Cache-Control', `public, max-age=${route.cache}`);
        }
        
        if (route.cors) {
          res.set('Access-Control-Allow-Origin', '*');
        }

        // Execute handler
        const handler = this.getHandler(handlerName);
        const result = await handler(req, res);
        
        res.json(result);
      } catch (error) {
        res.status(500).json({ error: (error as Error).message });
      }
    };
  }

  private getHandler(name: string): RouteHandler {
    const handlers: Record<string, RouteHandler> = {
      getUsers: async (req, res) => {
        return { users: await User.findAll() };
      },
      updateUser: async (req, res) => {
        const { id } = req.params;
        const updates = req.body;
        return await User.update(id, updates);
      },
      addComment: async (req, res) => {
        const { postId } = req.params;
        const comment = req.body;
        return await Comment.create({ ...comment, postId });
      }
    };
    
    return handlers[name] || (() => ({ error: 'Handler not found' }));
  }

  // Middleware implementations
  private authMiddleware(req: any, res: any, next: () => void): void {
    const token = req.headers.authorization;
    if (!token) {
      res.status(401).json({ error: 'Unauthorized' });
      return;
    }
    next();
  }

  private rateLimitMiddleware(req: any, res: any, next: () => void): void {
    // Rate limiting logic
    next();
  }

  private validateUserMiddleware(req: any, res: any, next: () => void): void {
    const { id } = req.params;
    if (!id || isNaN(Number(id))) {
      res.status(400).json({ error: 'Invalid user ID' });
      return;
    }
    next();
  }
}
```

## Advanced Usage Scenarios

### Dynamic Route Generation

```javascript
// dynamic-routes.js
class DynamicRouteGenerator {
  constructor() {
    this.routeTemplates = new Map();
  }

  registerTemplate(name, template) {
    this.routeTemplates.set(name, template);
  }

  generateRoutes(config) {
    const routes = [];
    
    config.forEach(item => {
      const template = this.routeTemplates.get(item.type);
      if (template) {
        routes.push(this.applyTemplate(template, item));
      }
    });
    
    return routes;
  }

  applyTemplate(template, data) {
    return {
      path: template.path.replace(/\{(\w+)\}/g, (_, key) => data[key]),
      method: template.method,
      handler: template.handler,
      middleware: template.middleware,
      options: { ...template.options, ...data.options }
    };
  }
}

// Usage
const generator = new DynamicRouteGenerator();

generator.registerTemplate('crud', {
  path: '/api/{resource}',
  method: 'GET',
  handler: 'get{Resource}',
  middleware: ['auth'],
  options: { cache: 300 }
});

const config = [
  { type: 'crud', resource: 'users', options: { cache: 600 } },
  { type: 'crud', resource: 'posts', options: { cache: 300 } }
];

const routes = generator.generateRoutes(config);
```

### Route Composition

```javascript
// route-composition.js
class RouteComposer {
  constructor() {
    this.composers = new Map();
  }

  registerComposer(name, composer) {
    this.composers.set(name, composer);
  }

  composeRoutes(baseRoute, composers) {
    let route = { ...baseRoute };
    
    composers.forEach(composerName => {
      const composer = this.composers.get(composerName);
      if (composer) {
        route = composer(route);
      }
    });
    
    return route;
  }
}

// Usage
const composer = new RouteComposer();

composer.registerComposer('withAuth', (route) => ({
  ...route,
  middleware: [...(route.middleware || []), 'auth']
}));

composer.registerComposer('withCache', (route) => ({
  ...route,
  options: { ...route.options, cache: 300 }
}));

const baseRoute = {
  path: '/api/users',
  method: 'GET',
  handler: 'getUsers'
};

const enhancedRoute = composer.composeRoutes(baseRoute, ['withAuth', 'withCache']);
```

## Real-World Examples

### RESTful API Routes

```tsk
#route /api/v1/users
  method: GET
  handler: listUsers
  middleware: [auth, pagination]
  cache: 300
  cors: true

#route /api/v1/users/:id
  method: GET
  handler: getUser
  middleware: [auth, validateUser]
  cache: 600

#route /api/v1/users
  method: POST
  handler: createUser
  middleware: [auth, validateUserData]
  rate_limit: 10/minute
  validation:
    name: string
    email: email
    role: string

#route /api/v1/users/:id
  method: PUT
  handler: updateUser
  middleware: [auth, validateUser, validateUserData]
  validation:
    name: string
    email: email

#route /api/v1/users/:id
  method: DELETE
  handler: deleteUser
  middleware: [auth, validateUser, adminOnly]
```

### WebSocket Routes

```tsk
#route /ws/chat/:roomId
  method: GET
  handler: chatHandler
  middleware: [auth, validateRoom]
  websocket: true
  realtime: true

#route /ws/notifications
  method: GET
  handler: notificationHandler
  middleware: [auth]
  websocket: true
  realtime: true
```

### File Upload Routes

```tsk
#route /api/upload/images
  method: POST
  handler: uploadImage
  middleware: [auth, fileValidation]
  rate_limit: 5/minute
  validation:
    file: file
    description: string

#route /api/upload/documents
  method: POST
  handler: uploadDocument
  middleware: [auth, fileValidation, virusScan]
  rate_limit: 2/minute
  validation:
    file: file
    category: string
```

## Performance Considerations

### Route Caching

```javascript
// route-cache.js
class RouteCache {
  constructor() {
    this.cache = new Map();
    this.stats = { hits: 0, misses: 0 };
  }

  get(key) {
    const cached = this.cache.get(key);
    if (cached) {
      this.stats.hits++;
      return cached;
    }
    this.stats.misses++;
    return null;
  }

  set(key, value, ttl = 300000) {
    this.cache.set(key, {
      value,
      expires: Date.now() + ttl
    });
  }

  cleanup() {
    const now = Date.now();
    for (const [key, entry] of this.cache.entries()) {
      if (entry.expires < now) {
        this.cache.delete(key);
      }
    }
  }
}
```

### Route Optimization

```javascript
// route-optimizer.js
class RouteOptimizer {
  optimizeRoutes(routes) {
    return routes
      .sort((a, b) => a.path.length - b.path.length) // Shorter paths first
      .map(route => ({
        ...route,
        middleware: this.optimizeMiddleware(route.middleware),
        options: this.optimizeOptions(route.options)
      }));
  }

  optimizeMiddleware(middleware) {
    // Remove duplicate middleware
    return [...new Set(middleware)];
  }

  optimizeOptions(options) {
    // Set sensible defaults
    return {
      cache: 300,
      cors: false,
      rate_limit: '1000/hour',
      ...options
    };
  }
}
```

## Security Notes

### Route Security

```javascript
// route-security.js
class RouteSecurity {
  validateRoute(route) {
    const issues = [];
    
    // Check for sensitive paths
    if (route.path.includes('/admin') && !route.middleware.includes('adminAuth')) {
      issues.push('Admin routes must have adminAuth middleware');
    }
    
    // Check for missing validation
    if (route.method === 'POST' && !route.validation) {
      issues.push('POST routes should have validation');
    }
    
    // Check for missing rate limiting
    if (!route.options?.rate_limit) {
      issues.push('Routes should have rate limiting');
    }
    
    return issues;
  }

  sanitizePath(path) {
    // Remove potential path traversal
    return path.replace(/\.\./g, '');
  }

  validateMethod(method) {
    const allowedMethods = ['GET', 'POST', 'PUT', 'DELETE', 'PATCH'];
    return allowedMethods.includes(method.toUpperCase());
  }
}
```

## Best Practices

### Route Organization

```javascript
// route-organizer.js
class RouteOrganizer {
  organizeByPrefix(routes) {
    const organized = {};
    
    routes.forEach(route => {
      const prefix = this.getPrefix(route.path);
      if (!organized[prefix]) {
        organized[prefix] = [];
      }
      organized[prefix].push(route);
    });
    
    return organized;
  }

  getPrefix(path) {
    const parts = path.split('/');
    return parts.length > 2 ? `/${parts[1]}` : '/';
  }

  validateRouteStructure(route) {
    const required = ['path', 'method', 'handler'];
    const missing = required.filter(field => !route[field]);
    
    if (missing.length > 0) {
      throw new Error(`Missing required fields: ${missing.join(', ')}`);
    }
  }
}
```

### Error Handling

```javascript
// route-error-handler.js
class RouteErrorHandler {
  handleRouteError(error, req, res) {
    console.error('Route error:', error);
    
    if (error.name === 'ValidationError') {
      return res.status(400).json({
        error: 'Validation failed',
        details: error.details
      });
    }
    
    if (error.name === 'AuthError') {
      return res.status(401).json({
        error: 'Authentication required'
      });
    }
    
    return res.status(500).json({
      error: 'Internal server error'
    });
  }
}
```

## Related Topics

- [@http Operator](./40-tsklang-javascript-operator-http.md)
- [@websocket Operator](./61-tsklang-javascript-operator-websocket.md)
- [@middleware Directive](./80-tsklang-javascript-directives-middleware.md)
- [@api Directive](./76-tsklang-javascript-directives-api.md)
- [@web Directive](./75-tsklang-javascript-directives-web.md) 