# #api Directive - API Configuration

## Overview
The `#api` directive in TuskLang provides comprehensive API configuration capabilities, enabling you to define REST APIs, GraphQL schemas, API documentation, authentication, rate limiting, and API versioning in a declarative manner.

## TuskLang Syntax

### Basic REST API
```tusk
#api {
  version: "v1",
  base_path: "/api/v1",
  endpoints: {
    "/users": {
      GET: {
        handler: "userController.getUsers",
        description: "Get all users",
        parameters: ["page", "limit"],
        responses: {
          200: "List of users",
          400: "Bad request"
        }
      },
      POST: {
        handler: "userController.createUser",
        description: "Create a new user",
        body: "User object",
        responses: {
          201: "User created",
          400: "Validation error"
        }
      }
    }
  }
}
```

### API with Authentication
```tusk
#api {
  version: "v2",
  auth: {
    type: "jwt",
    required: true,
    roles: ["admin", "user"]
  },
  endpoints: {
    "/admin/users": {
      GET: {
        handler: "adminController.getUsers",
        auth: {
          roles: ["admin"]
        }
      }
    }
  }
}
```

### GraphQL API
```tusk
#api {
  type: "graphql",
  schema: {
    User: {
      id: "ID!",
      name: "String!",
      email: "String!",
      posts: "[Post!]!"
    },
    Post: {
      id: "ID!",
      title: "String!",
      content: "String!",
      author: "User!"
    }
  },
  resolvers: {
    Query: {
      users: "userResolver.getUsers",
      posts: "postResolver.getPosts"
    },
    Mutation: {
      createUser: "userResolver.createUser",
      createPost: "postResolver.createPost"
    }
  }
}
```

## JavaScript Integration

### Node.js REST API
```javascript
const tusklang = require('@tusklang/core');
const express = require('express');
const cors = require('cors');
const helmet = require('helmet');
const rateLimit = require('express-rate-limit');

const config = tusklang.parse(`
api_config: #api {
  version: "v1",
  base_path: "/api/v1",
  cors: {
    enabled: true,
    origin: ["http://localhost:3000"]
  },
  auth: {
    type: "jwt",
    required: true
  },
  rate_limit: {
    windowMs: 15 * 60 * 1000,
    max: 100
  },
  endpoints: {
    "/users": {
      GET: {
        handler: "userController.getUsers",
        description: "Get all users",
        parameters: ["page", "limit"],
        responses: {
          200: "List of users",
          400: "Bad request"
        }
      },
      POST: {
        handler: "userController.createUser",
        description: "Create a new user",
        body: "User object",
        responses: {
          201: "User created",
          400: "Validation error"
        }
      }
    },
    "/users/:id": {
      GET: {
        handler: "userController.getUser",
        description: "Get user by ID",
        parameters: ["id"],
        responses: {
          200: "User object",
          404: "User not found"
        }
      },
      PUT: {
        handler: "userController.updateUser",
        description: "Update user",
        body: "User object",
        responses: {
          200: "User updated",
          404: "User not found"
        }
      },
      DELETE: {
        handler: "userController.deleteUser",
        description: "Delete user",
        responses: {
          204: "User deleted",
          404: "User not found"
        }
      }
    }
  }
}
`);

class ApiServer {
  constructor(config) {
    this.config = config.api_config;
    this.app = express();
    this.controllers = new Map();
    this.middleware = new Map();
    this.initializeApi();
  }

  initializeApi() {
    // Set up basic middleware
    this.setupBasicMiddleware();
    
    // Set up API-specific middleware
    this.setupApiMiddleware();
    
    // Set up routes
    this.setupRoutes();
    
    // Set up documentation
    this.setupDocumentation();
    
    // Set up error handling
    this.setupErrorHandling();
  }

  setupBasicMiddleware() {
    this.app.use(express.json());
    this.app.use(express.urlencoded({ extended: true }));
    
    // CORS
    if (this.config.cors?.enabled) {
      this.app.use(cors({
        origin: this.config.cors.origin,
        credentials: true
      }));
    }
    
    // Security
    this.app.use(helmet());
    
    // Rate limiting
    if (this.config.rate_limit) {
      const limiter = rateLimit({
        windowMs: this.config.rate_limit.windowMs,
        max: this.config.rate_limit.max,
        message: 'Too many requests from this IP'
      });
      this.app.use(this.config.base_path, limiter);
    }
  }

  setupApiMiddleware() {
    // Authentication middleware
    if (this.config.auth?.required) {
      this.app.use(this.config.base_path, this.authMiddleware());
    }
    
    // Request logging
    this.app.use(this.config.base_path, this.requestLogger());
  }

  authMiddleware() {
    return (req, res, next) => {
      const token = req.headers.authorization?.replace('Bearer ', '');
      
      if (!token) {
        return res.status(401).json({ error: 'Authentication required' });
      }
      
      try {
        // Simple JWT verification (in production, use a proper JWT library)
        const decoded = this.verifyToken(token);
        req.user = decoded;
        next();
      } catch (error) {
        return res.status(401).json({ error: 'Invalid token' });
      }
    };
  }

  verifyToken(token) {
    // Simple token verification (replace with proper JWT verification)
    if (token === 'valid-token') {
      return { id: 1, username: 'user', role: 'user' };
    }
    throw new Error('Invalid token');
  }

  requestLogger() {
    return (req, res, next) => {
      const start = Date.now();
      
      res.on('finish', () => {
        const duration = Date.now() - start;
        console.log(`${req.method} ${req.path} ${res.statusCode} ${duration}ms`);
      });
      
      next();
    };
  }

  setupRoutes() {
    if (!this.config.endpoints) return;

    Object.entries(this.config.endpoints).forEach(([path, methods]) => {
      Object.entries(methods).forEach(([method, config]) => {
        const fullPath = this.config.base_path + path;
        const handler = this.resolveHandler(config.handler);
        const middleware = this.resolveMiddleware(config);

        this.app[method.toLowerCase()](fullPath, ...middleware, handler);
      });
    });
  }

  resolveHandler(handlerPath) {
    const handlers = {
      'userController.getUsers': async (req, res) => {
        try {
          const { page = 1, limit = 10 } = req.query;
          const users = await this.getUsers(parseInt(page), parseInt(limit));
          res.json({
            data: users,
            pagination: {
              page: parseInt(page),
              limit: parseInt(limit),
              total: users.length
            }
          });
        } catch (error) {
          res.status(500).json({ error: error.message });
        }
      },

      'userController.createUser': async (req, res) => {
        try {
          const user = await this.createUser(req.body);
          res.status(201).json({ data: user });
        } catch (error) {
          res.status(400).json({ error: error.message });
        }
      },

      'userController.getUser': async (req, res) => {
        try {
          const user = await this.getUser(req.params.id);
          if (!user) {
            return res.status(404).json({ error: 'User not found' });
          }
          res.json({ data: user });
        } catch (error) {
          res.status(500).json({ error: error.message });
        }
      },

      'userController.updateUser': async (req, res) => {
        try {
          const user = await this.updateUser(req.params.id, req.body);
          if (!user) {
            return res.status(404).json({ error: 'User not found' });
          }
          res.json({ data: user });
        } catch (error) {
          res.status(400).json({ error: error.message });
        }
      },

      'userController.deleteUser': async (req, res) => {
        try {
          const deleted = await this.deleteUser(req.params.id);
          if (!deleted) {
            return res.status(404).json({ error: 'User not found' });
          }
          res.status(204).send();
        } catch (error) {
          res.status(500).json({ error: error.message });
        }
      }
    };

    return handlers[handlerPath] || ((req, res) => {
      res.status(404).json({ error: 'Handler not found' });
    });
  }

  resolveMiddleware(config) {
    const middleware = [];

    // Validation middleware
    if (config.body) {
      middleware.push(this.bodyValidationMiddleware(config.body));
    }

    // Parameter validation
    if (config.parameters) {
      middleware.push(this.parameterValidationMiddleware(config.parameters));
    }

    return middleware;
  }

  bodyValidationMiddleware(schema) {
    return (req, res, next) => {
      // Simple validation (replace with proper validation library)
      if (schema === 'User object') {
        const { name, email } = req.body;
        if (!name || !email) {
          return res.status(400).json({ error: 'Name and email are required' });
        }
      }
      next();
    };
  }

  parameterValidationMiddleware(parameters) {
    return (req, res, next) => {
      for (const param of parameters) {
        if (param === 'id' && !req.params.id) {
          return res.status(400).json({ error: 'ID parameter is required' });
        }
      }
      next();
    };
  }

  setupDocumentation() {
    // API documentation endpoint
    this.app.get(this.config.base_path + '/docs', (req, res) => {
      res.json(this.generateApiDocs());
    });

    // OpenAPI/Swagger specification
    this.app.get(this.config.base_path + '/swagger.json', (req, res) => {
      res.json(this.generateSwaggerSpec());
    });
  }

  generateApiDocs() {
    const docs = {
      version: this.config.version,
      base_path: this.config.base_path,
      endpoints: {}
    };

    Object.entries(this.config.endpoints).forEach(([path, methods]) => {
      docs.endpoints[path] = {};
      Object.entries(methods).forEach(([method, config]) => {
        docs.endpoints[path][method] = {
          description: config.description,
          parameters: config.parameters,
          responses: config.responses
        };
      });
    });

    return docs;
  }

  generateSwaggerSpec() {
    return {
      openapi: '3.0.0',
      info: {
        title: 'API Documentation',
        version: this.config.version
      },
      paths: this.generateSwaggerPaths()
    };
  }

  generateSwaggerPaths() {
    const paths = {};

    Object.entries(this.config.endpoints).forEach(([path, methods]) => {
      paths[path] = {};
      Object.entries(methods).forEach(([method, config]) => {
        paths[path][method.toLowerCase()] = {
          summary: config.description,
          responses: this.generateSwaggerResponses(config.responses)
        };
      });
    });

    return paths;
  }

  generateSwaggerResponses(responses) {
    const swaggerResponses = {};
    Object.entries(responses).forEach(([code, description]) => {
      swaggerResponses[code] = {
        description: description,
        content: {
          'application/json': {
            schema: {
              type: 'object'
            }
          }
        }
      };
    });
    return swaggerResponses;
  }

  setupErrorHandling() {
    // 404 handler
    this.app.use(this.config.base_path + '/*', (req, res) => {
      res.status(404).json({ error: 'Endpoint not found' });
    });

    // Global error handler
    this.app.use((error, req, res, next) => {
      console.error('API Error:', error);
      res.status(500).json({ error: 'Internal server error' });
    });
  }

  // Mock data methods
  async getUsers(page, limit) {
    const users = [
      { id: 1, name: 'John Doe', email: 'john@example.com' },
      { id: 2, name: 'Jane Smith', email: 'jane@example.com' },
      { id: 3, name: 'Bob Johnson', email: 'bob@example.com' }
    ];
    
    const start = (page - 1) * limit;
    const end = start + limit;
    return users.slice(start, end);
  }

  async createUser(userData) {
    return {
      id: Date.now(),
      ...userData,
      createdAt: new Date()
    };
  }

  async getUser(id) {
    const users = await this.getUsers(1, 10);
    return users.find(user => user.id === parseInt(id));
  }

  async updateUser(id, userData) {
    const user = await this.getUser(id);
    if (!user) return null;
    
    return { ...user, ...userData, updatedAt: new Date() };
  }

  async deleteUser(id) {
    const user = await this.getUser(id);
    return !!user;
  }

  async start(port = 3000) {
    return new Promise((resolve, reject) => {
      this.server = this.app.listen(port, () => {
        console.log(`API server running on port ${port}`);
        console.log(`API docs available at http://localhost:${port}${this.config.base_path}/docs`);
        console.log(`Swagger spec available at http://localhost:${port}${this.config.base_path}/swagger.json`);
        resolve(this.server);
      });

      this.server.on('error', reject);
    });
  }

  async stop() {
    if (this.server) {
      return new Promise((resolve) => {
        this.server.close(() => {
          console.log('API server stopped');
          resolve();
        });
      });
    }
  }
}

// Usage
const apiServer = new ApiServer(config);

async function startApiServer() {
  try {
    await apiServer.start(3001);
    console.log('API server started successfully');
  } catch (error) {
    console.error('Failed to start API server:', error);
  }
}

startApiServer();
```

### GraphQL API Server
```javascript
const { ApolloServer, gql } = require('apollo-server-express');

const graphqlConfig = tusklang.parse(`
graphql_api: #api {
  type: "graphql",
  schema: {
    User: {
      id: "ID!",
      name: "String!",
      email: "String!",
      posts: "[Post!]!"
    },
    Post: {
      id: "ID!",
      title: "String!",
      content: "String!",
      author: "User!"
    }
  },
  resolvers: {
    Query: {
      users: "userResolver.getUsers",
      posts: "postResolver.getPosts"
    },
    Mutation: {
      createUser: "userResolver.createUser",
      createPost: "postResolver.createPost"
    }
  }
}
`);

class GraphQLApiServer {
  constructor(config) {
    this.config = config.graphql_api;
    this.app = express();
    this.initializeGraphQL();
  }

  initializeGraphQL() {
    // Generate GraphQL schema
    const typeDefs = this.generateTypeDefs();
    
    // Generate resolvers
    const resolvers = this.generateResolvers();
    
    // Create Apollo Server
    this.apolloServer = new ApolloServer({
      typeDefs,
      resolvers,
      context: ({ req }) => ({
        user: req.user
      })
    });
    
    // Apply middleware
    this.apolloServer.applyMiddleware({ app: this.app, path: '/graphql' });
  }

  generateTypeDefs() {
    let typeDefs = `
      type Query {
        users: [User!]!
        posts: [Post!]!
      }
      
      type Mutation {
        createUser(input: UserInput!): User!
        createPost(input: PostInput!): Post!
      }
    `;

    // Generate User type
    if (this.config.schema.User) {
      typeDefs += `
        type User {
          ${Object.entries(this.config.schema.User).map(([field, type]) => `${field}: ${type}`).join('\n          ')}
        }
        
        input UserInput {
          name: String!
          email: String!
        }
      `;
    }

    // Generate Post type
    if (this.config.schema.Post) {
      typeDefs += `
        type Post {
          ${Object.entries(this.config.schema.Post).map(([field, type]) => `${field}: ${type}`).join('\n          ')}
        }
        
        input PostInput {
          title: String!
          content: String!
          authorId: ID!
        }
      `;
    }

    return gql(typeDefs);
  }

  generateResolvers() {
    const resolvers = {
      Query: {},
      Mutation: {},
      User: {},
      Post: {}
    };

    // Generate Query resolvers
    if (this.config.resolvers.Query) {
      Object.entries(this.config.resolvers.Query).forEach(([field, handler]) => {
        resolvers.Query[field] = this.resolveHandler(handler);
      });
    }

    // Generate Mutation resolvers
    if (this.config.resolvers.Mutation) {
      Object.entries(this.config.resolvers.Mutation).forEach(([field, handler]) => {
        resolvers.Mutation[field] = this.resolveHandler(handler);
      });
    }

    // Generate field resolvers
    resolvers.User.posts = (parent) => {
      return this.getPostsByUserId(parent.id);
    };

    resolvers.Post.author = (parent) => {
      return this.getUser(parent.authorId);
    };

    return resolvers;
  }

  resolveHandler(handlerPath) {
    const handlers = {
      'userResolver.getUsers': async () => {
        return await this.getUsers();
      },
      'userResolver.createUser': async (_, { input }) => {
        return await this.createUser(input);
      },
      'postResolver.getPosts': async () => {
        return await this.getPosts();
      },
      'postResolver.createPost': async (_, { input }) => {
        return await this.createPost(input);
      }
    };

    return handlers[handlerPath] || (() => {
      throw new Error('Handler not found');
    });
  }

  // Mock data methods
  async getUsers() {
    return [
      { id: '1', name: 'John Doe', email: 'john@example.com' },
      { id: '2', name: 'Jane Smith', email: 'jane@example.com' }
    ];
  }

  async createUser(input) {
    return {
      id: Date.now().toString(),
      ...input
    };
  }

  async getPosts() {
    return [
      { id: '1', title: 'First Post', content: 'Content 1', authorId: '1' },
      { id: '2', title: 'Second Post', content: 'Content 2', authorId: '2' }
    ];
  }

  async createPost(input) {
    return {
      id: Date.now().toString(),
      ...input
    };
  }

  async getUser(id) {
    const users = await this.getUsers();
    return users.find(user => user.id === id);
  }

  async getPostsByUserId(userId) {
    const posts = await this.getPosts();
    return posts.filter(post => post.authorId === userId);
  }

  async start(port = 3002) {
    return new Promise((resolve, reject) => {
      this.server = this.app.listen(port, () => {
        console.log(`GraphQL server running on port ${port}`);
        console.log(`GraphQL playground available at http://localhost:${port}/graphql`);
        resolve(this.server);
      });

      this.server.on('error', reject);
    });
  }
}

// Usage
const graphqlServer = new GraphQLApiServer(graphqlConfig);
graphqlServer.start(3002);
```

## Advanced Usage Scenarios

### Microservices API Gateway
```tusk
#api {
  type: "gateway",
  services: {
    "user-service": {
      url: "http://user-service:3001",
      endpoints: ["/api/users/*"]
    },
    "post-service": {
      url: "http://post-service:3002",
      endpoints: ["/api/posts/*"]
    }
  },
  loadBalancer: {
    strategy: "round-robin"
  }
}
```

### API Versioning
```tusk
#api {
  versions: {
    "v1": {
      deprecated: false,
      sunset_date: null,
      endpoints: {
        "/users": {
          GET: "userController.getUsers"
        }
      }
    },
    "v2": {
      deprecated: false,
      endpoints: {
        "/users": {
          GET: "userController.getUsersV2"
        }
      }
    }
  }
}
```

### API Documentation
```tusk
#api {
  documentation: {
    title: "My API",
    version: "1.0.0",
    description: "A comprehensive API for user management",
    contact: {
      name: "API Support",
      email: "support@example.com"
    },
    servers: [
      {
        url: "https://api.example.com",
        description: "Production server"
      }
    ]
  }
}
```

## TypeScript Implementation

### Typed API Server
```typescript
interface ApiConfig {
  version: string;
  base_path: string;
  cors?: {
    enabled: boolean;
    origin: string[];
  };
  auth?: {
    type: string;
    required: boolean;
  };
  endpoints?: Record<string, Record<string, EndpointConfig>>;
}

interface EndpointConfig {
  handler: string;
  description: string;
  parameters?: string[];
  body?: string;
  responses?: Record<string, string>;
}

class TypedApiServer {
  private config: ApiConfig;
  private app: any;
  private server: any;

  constructor(config: ApiConfig) {
    this.config = config;
    this.app = express();
    this.initializeApi();
  }

  private initializeApi(): void {
    this.setupMiddleware();
    this.setupRoutes();
    this.setupDocumentation();
  }

  private setupMiddleware(): void {
    // Implementation for middleware setup
  }

  private setupRoutes(): void {
    // Implementation for route setup
  }

  private setupDocumentation(): void {
    // Implementation for documentation setup
  }

  async start(port: number = 3000): Promise<void> {
    return new Promise((resolve, reject) => {
      this.server = this.app.listen(port, () => {
        console.log(`API server running on port ${port}`);
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

### Express.js API with TuskLang
```javascript
// Express.js API with TuskLang configuration
const express = require('express');
const tusklang = require('@tusklang/core');

const config = tusklang.parse(`
api_config: #api {
  version: "v1",
  base_path: "/api/v1",
  endpoints: {
    "/health": {
      GET: {
        handler: "healthController.check",
        description: "Health check endpoint"
      }
    }
  }
}
`);

const app = express();
const apiConfig = config.api_config;

// Apply routes
Object.entries(apiConfig.endpoints).forEach(([path, methods]) => {
  Object.entries(methods).forEach(([method, config]) => {
    app[method.toLowerCase()](apiConfig.base_path + path, (req, res) => {
      res.json({ status: 'healthy', timestamp: new Date() });
    });
  });
});

app.listen(3000, () => {
  console.log('API server running on port 3000');
});
```

### React API Client
```javascript
// React API client with TuskLang configuration
import React, { useState, useEffect } from 'react';

const apiConfig = {
  baseUrl: 'https://api.example.com',
  endpoints: {
    users: '/api/v1/users',
    posts: '/api/v1/posts'
  }
};

const ApiClient = {
  async get(endpoint) {
    const response = await fetch(apiConfig.baseUrl + apiConfig.endpoints[endpoint]);
    return response.json();
  },

  async post(endpoint, data) {
    const response = await fetch(apiConfig.baseUrl + apiConfig.endpoints[endpoint], {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
    return response.json();
  }
};

const UserList = () => {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchUsers = async () => {
      try {
        const data = await ApiClient.get('users');
        setUsers(data.data);
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
- Implement API caching strategies
- Use connection pooling for database connections
- Monitor API response times
- Implement request/response compression

## Security Notes
- Always validate and sanitize API inputs
- Implement proper authentication and authorization
- Use HTTPS for all API communications
- Implement rate limiting and DDoS protection

## Best Practices
- Use consistent error response formats
- Implement comprehensive API documentation
- Use semantic HTTP status codes
- Implement proper logging and monitoring

## Related Topics
- [@http Operator](./60-http-operator.md) - HTTP requests
- [@websocket Operator](./61-websocket-operator.md) - WebSocket connections
- [Authentication](./14-security.md) - Security and authentication
- [Documentation](./18-documentation.md) - API documentation 