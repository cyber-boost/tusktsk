# @module Operator - Module System

## Overview
The `@module` operator in TuskLang provides a comprehensive module system, enabling you to create, import, export, and manage modular code with dependency resolution, lazy loading, and circular dependency handling.

## TuskLang Syntax

### Basic Module
```tusk
# Simple module definition
user_module: @module("user", {
  name: "User Module",
  version: "1.0.0",
  exports: ["User", "UserService"],
  dependencies: ["database", "auth"]
})

# Module with configuration
config_module: @module("config", {
  name: "Configuration Module",
  exports: ["getConfig", "setConfig"],
  imports: ["@env", "@file"]
})
```

### Module with Exports
```tusk
# Module with specific exports
api_module: @module("api", {
  name: "API Module",
  exports: {
    endpoints: ["users", "posts", "comments"],
    services: ["UserService", "PostService"],
    middleware: ["auth", "validation"]
  }
})
```

### Module with Dependencies
```tusk
# Module with dependency management
service_module: @module("service", {
  name: "Service Module",
  dependencies: [
    @module.dependency("database", "2.0.0"),
    @module.dependency("cache", "1.5.0")
  ],
  exports: ["ServiceManager"]
})
```

## JavaScript Integration

### Node.js Module System
```javascript
const tusklang = require('@tusklang/core');

const config = tusklang.parse(`
module_config: {
  user: @module("user", {
    name: "User Module",
    version: "1.0.0",
    exports: ["User", "UserService"],
    dependencies: ["database", "auth"]
  }),
  
  api: @module("api", {
    name: "API Module",
    exports: ["endpoints", "services"],
    dependencies: ["user", "auth"]
  }),
  
  auth: @module("auth", {
    name: "Auth Module",
    exports: ["AuthService", "JWT"],
    dependencies: ["database"]
  })
}
`);

class ModuleManager {
  constructor(config) {
    this.config = config.module_config;
    this.modules = new Map();
    this.loadedModules = new Map();
    this.dependencyGraph = new Map();
    this.initializeModules();
  }

  initializeModules() {
    Object.entries(this.config).forEach(([name, moduleConfig]) => {
      this.createModule(name, moduleConfig);
    });
  }

  createModule(name, config) {
    const module = {
      name: name,
      config: config,
      exports: {},
      dependencies: config.dependencies || [],
      loaded: false,
      loading: false,
      error: null
    };

    this.modules.set(name, module);
    this.dependencyGraph.set(name, config.dependencies || []);
  }

  async loadModule(name) {
    const module = this.modules.get(name);
    if (!module) {
      throw new Error(`Module '${name}' not found`);
    }

    if (module.loaded) {
      return module.exports;
    }

    if (module.loading) {
      // Handle circular dependencies
      return new Promise(resolve => {
        const checkLoaded = () => {
          if (module.loaded) {
            resolve(module.exports);
          } else {
            setTimeout(checkLoaded, 10);
          }
        };
        checkLoaded();
      });
    }

    module.loading = true;

    try {
      // Load dependencies first
      await this.loadDependencies(module.dependencies);

      // Load the actual module
      const moduleExports = await this.loadModuleImplementation(name, module.config);
      module.exports = moduleExports;
      module.loaded = true;

      return module.exports;
    } catch (error) {
      module.error = error;
      module.loading = false;
      throw error;
    }
  }

  async loadDependencies(dependencies) {
    const loadPromises = dependencies.map(dep => this.loadModule(dep));
    await Promise.all(loadPromises);
  }

  async loadModuleImplementation(name, config) {
    // Simulate loading different module types
    switch (name) {
      case 'user':
        return this.loadUserModule(config);
      case 'api':
        return this.loadApiModule(config);
      case 'auth':
        return this.loadAuthModule(config);
      case 'database':
        return this.loadDatabaseModule(config);
      default:
        throw new Error(`Unknown module: ${name}`);
    }
  }

  loadUserModule(config) {
    return {
      User: class User {
        constructor(data) {
          this.id = data.id;
          this.name = data.name;
          this.email = data.email;
        }

        save() {
          console.log('Saving user:', this.name);
          return Promise.resolve({ success: true });
        }
      },

      UserService: class UserService {
        constructor() {
          this.users = [];
        }

        async createUser(userData) {
          const user = new this.User(userData);
          await user.save();
          this.users.push(user);
          return user;
        }

        async getUser(id) {
          return this.users.find(user => user.id === id);
        }
      }
    };
  }

  loadApiModule(config) {
    return {
      endpoints: {
        users: '/api/users',
        posts: '/api/posts',
        comments: '/api/comments'
      },

      services: {
        UserService: class UserService {
          async getUsers() {
            return ['user1', 'user2', 'user3'];
          }
        },

        PostService: class PostService {
          async getPosts() {
            return ['post1', 'post2'];
          }
        }
      },

      middleware: {
        auth: (req, res, next) => {
          console.log('Auth middleware');
          next();
        },

        validation: (req, res, next) => {
          console.log('Validation middleware');
          next();
        }
      }
    };
  }

  loadAuthModule(config) {
    return {
      AuthService: class AuthService {
        constructor() {
          this.secret = 'your-secret-key';
        }

        async login(credentials) {
          console.log('Logging in user:', credentials.username);
          return { token: 'jwt_token_here' };
        }

        async verifyToken(token) {
          console.log('Verifying token:', token);
          return { valid: true, user: { id: 1, username: 'user' } };
        }
      },

      JWT: {
        sign: (payload, secret) => {
          return 'signed_jwt_token';
        },

        verify: (token, secret) => {
          return { valid: true, payload: {} };
        }
      }
    };
  }

  loadDatabaseModule(config) {
    return {
      connect: () => {
        console.log('Connecting to database...');
        return Promise.resolve({ connected: true });
      },

      query: (sql, params) => {
        console.log('Executing query:', sql);
        return Promise.resolve([]);
      },

      close: () => {
        console.log('Closing database connection...');
        return Promise.resolve();
      }
    };
  }

  // Module resolution
  resolveModule(name) {
    return this.modules.get(name);
  }

  // Get module exports
  getModuleExports(name) {
    const module = this.modules.get(name);
    return module && module.loaded ? module.exports : null;
  }

  // Check if module is loaded
  isModuleLoaded(name) {
    const module = this.modules.get(name);
    return module ? module.loaded : false;
  }

  // Get dependency graph
  getDependencyGraph() {
    return this.dependencyGraph;
  }

  // Check for circular dependencies
  detectCircularDependencies() {
    const visited = new Set();
    const recursionStack = new Set();

    const hasCycle = (node) => {
      if (recursionStack.has(node)) {
        return true;
      }

      if (visited.has(node)) {
        return false;
      }

      visited.add(node);
      recursionStack.add(node);

      const dependencies = this.dependencyGraph.get(node) || [];
      for (const dep of dependencies) {
        if (hasCycle(dep)) {
          return true;
        }
      }

      recursionStack.delete(node);
      return false;
    };

    const cycles = [];
    for (const node of this.dependencyGraph.keys()) {
      if (!visited.has(node)) {
        if (hasCycle(node)) {
          cycles.push(node);
        }
      }
    }

    return cycles;
  }

  // Hot reload module
  async hotReloadModule(name) {
    const module = this.modules.get(name);
    if (!module) {
      throw new Error(`Module '${name}' not found`);
    }

    // Mark as not loaded
    module.loaded = false;
    module.exports = {};

    // Reload the module
    return await this.loadModule(name);
  }
}

// Usage
const moduleManager = new ModuleManager(config);

// Load modules
async function initializeApp() {
  try {
    // Load all modules
    const userModule = await moduleManager.loadModule('user');
    const apiModule = await moduleManager.loadModule('api');
    const authModule = await moduleManager.loadModule('auth');

    console.log('User module loaded:', userModule);
    console.log('API module loaded:', apiModule);
    console.log('Auth module loaded:', authModule);

    // Use module exports
    const User = userModule.User;
    const userService = new userModule.UserService();

    const newUser = new User({ id: 1, name: 'John Doe', email: 'john@example.com' });
    await userService.createUser(newUser);

  } catch (error) {
    console.error('Module loading error:', error);
  }
}

initializeApp();
```

### Browser Module System
```javascript
// Browser-based module system
const browserConfig = tusklang.parse(`
browser_modules: {
  utils: @module("utils", {
    name: "Utilities Module",
    exports: ["formatDate", "validateEmail", "debounce"]
  }),
  
  components: @module("components", {
    name: "Components Module",
    exports: ["Button", "Modal", "Input"],
    dependencies: ["utils"]
  }),
  
  services: @module("services", {
    name: "Services Module",
    exports: ["ApiService", "StorageService"],
    dependencies: ["utils"]
  })
}
`);

class BrowserModuleManager {
  constructor(config) {
    this.config = config.browser_modules;
    this.modules = new Map();
    this.loadedModules = new Map();
    this.initializeModules();
  }

  initializeModules() {
    Object.entries(this.config).forEach(([name, moduleConfig]) => {
      this.createModule(name, moduleConfig);
    });
  }

  createModule(name, config) {
    const module = {
      name: name,
      config: config,
      exports: {},
      dependencies: config.dependencies || [],
      loaded: false
    };

    this.modules.set(name, module);
  }

  async loadModule(name) {
    const module = this.modules.get(name);
    if (!module) {
      throw new Error(`Module '${name}' not found`);
    }

    if (module.loaded) {
      return module.exports;
    }

    // Load dependencies first
    await this.loadDependencies(module.dependencies);

    // Load the actual module
    const moduleExports = await this.loadModuleImplementation(name, module.config);
    module.exports = moduleExports;
    module.loaded = true;

    return module.exports;
  }

  async loadDependencies(dependencies) {
    const loadPromises = dependencies.map(dep => this.loadModule(dep));
    await Promise.all(loadPromises);
  }

  async loadModuleImplementation(name, config) {
    switch (name) {
      case 'utils':
        return this.loadUtilsModule(config);
      case 'components':
        return this.loadComponentsModule(config);
      case 'services':
        return this.loadServicesModule(config);
      default:
        throw new Error(`Unknown module: ${name}`);
    }
  }

  loadUtilsModule(config) {
    return {
      formatDate: (date) => {
        return date.toLocaleDateString();
      },

      validateEmail: (email) => {
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
      },

      debounce: (func, delay) => {
        let timeoutId;
        return function (...args) {
          clearTimeout(timeoutId);
          timeoutId = setTimeout(() => func.apply(this, args), delay);
        };
      }
    };
  }

  loadComponentsModule(config) {
    return {
      Button: class Button {
        constructor(text, onClick) {
          this.text = text;
          this.onClick = onClick;
        }

        render() {
          const button = document.createElement('button');
          button.textContent = this.text;
          button.addEventListener('click', this.onClick);
          return button;
        }
      },

      Modal: class Modal {
        constructor(content) {
          this.content = content;
        }

        render() {
          const modal = document.createElement('div');
          modal.className = 'modal';
          modal.innerHTML = `
            <div class="modal-content">
              ${this.content}
              <button class="close-btn">Close</button>
            </div>
          `;
          return modal;
        }
      },

      Input: class Input {
        constructor(type = 'text', placeholder = '') {
          this.type = type;
          this.placeholder = placeholder;
        }

        render() {
          const input = document.createElement('input');
          input.type = this.type;
          input.placeholder = this.placeholder;
          return input;
        }
      }
    };
  }

  loadServicesModule(config) {
    return {
      ApiService: class ApiService {
        constructor() {
          this.baseUrl = 'https://api.example.com';
        }

        async get(endpoint) {
          const response = await fetch(`${this.baseUrl}${endpoint}`);
          return response.json();
        }

        async post(endpoint, data) {
          const response = await fetch(`${this.baseUrl}${endpoint}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
          });
          return response.json();
        }
      },

      StorageService: class StorageService {
        constructor() {
          this.storage = localStorage;
        }

        set(key, value) {
          this.storage.setItem(key, JSON.stringify(value));
        }

        get(key) {
          const value = this.storage.getItem(key);
          return value ? JSON.parse(value) : null;
        }

        remove(key) {
          this.storage.removeItem(key);
        }

        clear() {
          this.storage.clear();
        }
      }
    };
  }

  // Get module exports
  getModuleExports(name) {
    const module = this.modules.get(name);
    return module && module.loaded ? module.exports : null;
  }

  // Check if module is loaded
  isModuleLoaded(name) {
    const module = this.modules.get(name);
    return module ? module.loaded : false;
  }

  // Lazy load module
  async lazyLoadModule(name) {
    if (!this.isModuleLoaded(name)) {
      return await this.loadModule(name);
    }
    return this.getModuleExports(name);
  }
}

// Usage
const browserModuleManager = new BrowserModuleManager(browserConfig);

// Load and use modules
async function initializeApp() {
  try {
    const utils = await browserModuleManager.loadModule('utils');
    const components = await browserModuleManager.loadModule('components');
    const services = await browserModuleManager.loadModule('services');

    // Use utilities
    const formattedDate = utils.formatDate(new Date());
    const isValidEmail = utils.validateEmail('test@example.com');

    // Use components
    const button = new components.Button('Click me', () => alert('Clicked!'));
    const modal = new components.Modal('<h2>Hello World</h2>');
    const input = new components.Input('text', 'Enter text...');

    // Use services
    const apiService = new services.ApiService();
    const storageService = new services.StorageService();

    // Add to DOM
    document.body.appendChild(button.render());
    document.body.appendChild(modal.render());
    document.body.appendChild(input.render());

    console.log('App initialized with modules');

  } catch (error) {
    console.error('Module loading error:', error);
  }
}

initializeApp();
```

## Advanced Usage Scenarios

### Plugin Module System
```tusk
# Plugin module system
plugin_module: @module("plugin", {
  name: "Plugin System",
  exports: ["PluginManager", "PluginInterface"],
  dependencies: ["events", "config"]
})
```

### Microservice Module
```tusk
# Microservice module
microservice_module: @module("microservice", {
  name: "Microservice Module",
  exports: ["ServiceRegistry", "ServiceDiscovery"],
  dependencies: ["network", "serialization"]
})
```

### Testing Module
```tusk
# Testing module
testing_module: @module("testing", {
  name: "Testing Module",
  exports: ["TestRunner", "MockFactory", "Assertions"],
  dependencies: ["utils"]
})
```

## TypeScript Implementation

### Typed Module Manager
```typescript
interface ModuleConfig {
  name: string;
  version?: string;
  exports: string[] | Record<string, string[]>;
  dependencies?: string[];
}

interface ModuleData {
  name: string;
  config: ModuleConfig;
  exports: Record<string, any>;
  dependencies: string[];
  loaded: boolean;
  loading: boolean;
  error: Error | null;
}

class TypedModuleManager {
  private modules: Map<string, ModuleData> = new Map();
  private dependencyGraph: Map<string, string[]> = new Map();

  createModule(name: string, config: ModuleConfig): void {
    const module: ModuleData = {
      name,
      config,
      exports: {},
      dependencies: config.dependencies || [],
      loaded: false,
      loading: false,
      error: null
    };

    this.modules.set(name, module);
    this.dependencyGraph.set(name, config.dependencies || []);
  }

  async loadModule(name: string): Promise<Record<string, any>> {
    const module = this.modules.get(name);
    if (!module) {
      throw new Error(`Module '${name}' not found`);
    }

    if (module.loaded) {
      return module.exports;
    }

    // Load dependencies and module implementation
    await this.loadDependencies(module.dependencies);
    const exports = await this.loadModuleImplementation(name, module.config);
    
    module.exports = exports;
    module.loaded = true;
    
    return exports;
  }

  private async loadDependencies(dependencies: string[]): Promise<void> {
    const loadPromises = dependencies.map(dep => this.loadModule(dep));
    await Promise.all(loadPromises);
  }

  private async loadModuleImplementation(name: string, config: ModuleConfig): Promise<Record<string, any>> {
    // Implementation for loading specific modules
    return {};
  }

  getModuleExports<T>(name: string): T | null {
    const module = this.modules.get(name);
    return module && module.loaded ? module.exports as T : null;
  }

  isModuleLoaded(name: string): boolean {
    const module = this.modules.get(name);
    return module ? module.loaded : false;
  }
}
```

## Real-World Examples

### Express.js Module System
```javascript
// Express.js with module system
const express = require('express');
const app = express();

const moduleManager = new ModuleManager(config);

// Load modules and set up routes
async function setupApp() {
  const apiModule = await moduleManager.loadModule('api');
  const authModule = await moduleManager.loadModule('auth');

  // Use API endpoints
  app.get(apiModule.endpoints.users, authModule.middleware.auth, (req, res) => {
    res.json({ users: ['user1', 'user2'] });
  });

  app.get(apiModule.endpoints.posts, authModule.middleware.auth, (req, res) => {
    res.json({ posts: ['post1', 'post2'] });
  });
}

setupApp();
```

### React Component Module
```javascript
// React components with module system
import React from 'react';

const ComponentModule = {
  Button: ({ children, onClick, ...props }) => (
    <button onClick={onClick} {...props}>{children}</button>
  ),

  Modal: ({ isOpen, children, onClose }) => (
    isOpen ? (
      <div className="modal">
        <div className="modal-content">
          {children}
          <button onClick={onClose}>Close</button>
        </div>
      </div>
    ) : null
  ),

  Input: ({ type = 'text', ...props }) => (
    <input type={type} {...props} />
  )
};

// Usage in React component
const MyForm = () => {
  const [isModalOpen, setIsModalOpen] = useState(false);

  return (
    <div>
      <ComponentModule.Input placeholder="Enter text" />
      <ComponentModule.Button onClick={() => setIsModalOpen(true)}>
        Open Modal
      </ComponentModule.Button>
      <ComponentModule.Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)}>
        <h2>Modal Content</h2>
      </ComponentModule.Modal>
    </div>
  );
};
```

## Performance Considerations
- Implement module caching for frequently used modules
- Use lazy loading for large modules
- Monitor module memory usage
- Implement module cleanup for unused modules

## Security Notes
- Validate module names and paths
- Implement module sandboxing for untrusted code
- Sanitize module exports
- Use module signing for critical modules

## Best Practices
- Keep modules focused and single-purpose
- Use descriptive module names
- Implement proper error handling for module loading
- Document module dependencies and exports

## Related Topics
- [@namespace Operator](./73-namespace-operator.md) - Namespace organization
- [@scope Operator](./72-scope-operator.md) - Variable scope management
- [Modules](./07-modules.md) - Module system basics
- [Dependencies](./07-modules.md) - Dependency management 