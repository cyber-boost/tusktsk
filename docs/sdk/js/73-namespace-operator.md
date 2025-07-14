# @namespace Operator - Namespace Organization

## Overview
The `@namespace` operator in TuskLang provides namespace organization capabilities, enabling you to group related functionality, avoid naming conflicts, and create modular, well-structured applications.

## TuskLang Syntax

### Basic Namespace
```tusk
# Simple namespace
app_namespace: @namespace("app", {
  version: "1.0.0",
  config: {
    api_url: "https://api.example.com",
    timeout: 5000
  }
})

# Nested namespace
nested_namespace: @namespace("app.utils", {
  format_date: "function",
  validate_email: "function"
})
```

### Namespace with Imports
```tusk
# Namespace with imports
imported_namespace: @namespace("app.services", {
  user_service: @import("user_service"),
  auth_service: @import("auth_service")
}, {
  imports: ["utils", "models"]
})
```

### Namespace with Exports
```tusk
# Namespace with exports
exported_namespace: @namespace("app.api", {
  endpoints: {
    users: "/api/users",
    posts: "/api/posts"
  }
}, {
  exports: ["endpoints", "config"]
})
```

## JavaScript Integration

### Node.js Namespace Management
```javascript
const tusklang = require('@tusklang/core');

const config = tusklang.parse(`
namespace_config: {
  app: @namespace("app", {
    version: "1.0.0",
    config: {
      api_url: "https://api.example.com",
      timeout: 5000
    }
  }),
  
  utils: @namespace("app.utils", {
    format_date: "function",
    validate_email: "function"
  }),
  
  services: @namespace("app.services", {
    user_service: @import("user_service"),
    auth_service: @import("auth_service")
  })
}
`);

class NamespaceManager {
  constructor(config) {
    this.config = config.namespace_config;
    this.namespaces = new Map();
    this.imports = new Map();
    this.exports = new Map();
    this.initializeNamespaces();
  }

  initializeNamespaces() {
    Object.entries(this.config).forEach(([name, namespaceConfig]) => {
      this.createNamespace(name, namespaceConfig);
    });
  }

  createNamespace(name, config) {
    const namespace = {
      name: name,
      path: name.split('.'),
      data: { ...config.initial },
      imports: config.imports || [],
      exports: config.exports || [],
      children: new Map(),
      parent: null
    };

    this.namespaces.set(name, namespace);

    // Set up parent-child relationships
    if (namespace.path.length > 1) {
      const parentName = namespace.path.slice(0, -1).join('.');
      const parent = this.namespaces.get(parentName);
      if (parent) {
        parent.children.set(namespace.path[namespace.path.length - 1], namespace);
        namespace.parent = parent;
      }
    }
  }

  getNamespace(name) {
    return this.namespaces.get(name);
  }

  getNamespaceValue(name, path) {
    const namespace = this.namespaces.get(name);
    if (!namespace) {
      return undefined;
    }

    const pathParts = path.split('.');
    let current = namespace.data;

    for (const part of pathParts) {
      if (current && typeof current === 'object' && current.hasOwnProperty(part)) {
        current = current[part];
      } else {
        return undefined;
      }
    }

    return current;
  }

  setNamespaceValue(name, path, value) {
    const namespace = this.namespaces.get(name);
    if (!namespace) {
      throw new Error(`Namespace '${name}' not found`);
    }

    const pathParts = path.split('.');
    let current = namespace.data;

    // Navigate to the parent of the target property
    for (let i = 0; i < pathParts.length - 1; i++) {
      const part = pathParts[i];
      if (!current[part] || typeof current[part] !== 'object') {
        current[part] = {};
      }
      current = current[part];
    }

    // Set the value
    current[pathParts[pathParts.length - 1]] = value;
  }

  importNamespace(name, importPath) {
    const namespace = this.namespaces.get(name);
    if (!namespace) {
      throw new Error(`Namespace '${name}' not found`);
    }

    // Simulate importing from a module
    const importedModule = this.loadModule(importPath);
    if (importedModule) {
      Object.assign(namespace.data, importedModule);
    }
  }

  loadModule(path) {
    // Simulate module loading
    const modules = {
      'user_service': {
        getUsers: () => ['user1', 'user2'],
        createUser: (user) => ({ id: 1, ...user })
      },
      'auth_service': {
        login: (credentials) => ({ token: 'jwt_token' }),
        logout: () => ({ success: true })
      },
      'utils': {
        formatDate: (date) => date.toISOString(),
        validateEmail: (email) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)
      }
    };

    return modules[path];
  }

  exportNamespace(name, exports) {
    const namespace = this.namespaces.get(name);
    if (!namespace) {
      throw new Error(`Namespace '${name}' not found`);
    }

    const exportedData = {};
    exports.forEach(exportName => {
      if (namespace.data.hasOwnProperty(exportName)) {
        exportedData[exportName] = namespace.data[exportName];
      }
    });

    this.exports.set(name, exportedData);
    return exportedData;
  }

  // Namespace resolution
  resolveNamespace(path) {
    const pathParts = path.split('.');
    let current = this.namespaces.get(pathParts[0]);

    if (!current) {
      return null;
    }

    for (let i = 1; i < pathParts.length; i++) {
      current = current.children.get(pathParts[i]);
      if (!current) {
        return null;
      }
    }

    return current;
  }

  // Namespace traversal
  traverseNamespaces(callback, namespace = null) {
    if (!namespace) {
      // Start from root namespaces
      this.namespaces.forEach(ns => {
        if (ns.path.length === 1) {
          this.traverseNamespaces(callback, ns);
        }
      });
      return;
    }

    callback(namespace);

    // Traverse children
    namespace.children.forEach(child => {
      this.traverseNamespaces(callback, child);
    });
  }

  // Namespace merging
  mergeNamespaces(targetName, sourceName) {
    const target = this.namespaces.get(targetName);
    const source = this.namespaces.get(sourceName);

    if (!target || !source) {
      throw new Error('One or both namespaces not found');
    }

    Object.assign(target.data, source.data);
    return target;
  }
}

// Usage
const namespaceManager = new NamespaceManager(config);

// Access namespace data
const appVersion = namespaceManager.getNamespaceValue('app', 'version');
const apiUrl = namespaceManager.getNamespaceValue('app', 'config.api_url');

console.log('App version:', appVersion);
console.log('API URL:', apiUrl);

// Set namespace values
namespaceManager.setNamespaceValue('app', 'config.debug', true);
namespaceManager.setNamespaceValue('app.utils', 'format_date', (date) => date.toISOString());

// Import modules
namespaceManager.importNamespace('app.services', 'user_service');
namespaceManager.importNamespace('app.services', 'auth_service');

// Export namespace
const exportedData = namespaceManager.exportNamespace('app', ['version', 'config']);

// Traverse all namespaces
namespaceManager.traverseNamespaces(namespace => {
  console.log(`Namespace: ${namespace.name}`);
});
```

### Browser Namespace Management
```javascript
// Browser-based namespace management
const browserConfig = tusklang.parse(`
browser_namespace: {
  app: @namespace("app", {
    version: "1.0.0",
    config: {
      theme: "light",
      language: "en"
    }
  }),
  
  components: @namespace("app.components", {
    Button: "component",
    Modal: "component"
  }),
  
  utils: @namespace("app.utils", {
    formatDate: "function",
    validateEmail: "function"
  })
}
`);

class BrowserNamespaceManager {
  constructor(config) {
    this.config = config.browser_namespace;
    this.namespaces = new Map();
    this.globalNamespace = window;
    this.initializeNamespaces();
  }

  initializeNamespaces() {
    Object.entries(this.config).forEach(([name, namespaceConfig]) => {
      this.createNamespace(name, namespaceConfig);
    });
  }

  createNamespace(name, config) {
    const namespace = {
      name: name,
      path: name.split('.'),
      data: { ...config.initial },
      exports: config.exports || [],
      children: new Map()
    };

    this.namespaces.set(name, namespace);

    // Create global namespace object
    this.createGlobalNamespace(name, namespace);
  }

  createGlobalNamespace(name, namespace) {
    const pathParts = name.split('.');
    let current = this.globalNamespace;

    // Create namespace path in global object
    for (const part of pathParts) {
      if (!current[part]) {
        current[part] = {};
      }
      current = current[part];
    }

    // Copy namespace data to global object
    Object.assign(current, namespace.data);
  }

  getNamespace(name) {
    return this.namespaces.get(name);
  }

  getNamespaceValue(name, path) {
    const namespace = this.namespaces.get(name);
    if (!namespace) {
      return undefined;
    }

    const pathParts = path.split('.');
    let current = namespace.data;

    for (const part of pathParts) {
      if (current && typeof current === 'object' && current.hasOwnProperty(part)) {
        current = current[part];
      } else {
        return undefined;
      }
    }

    return current;
  }

  setNamespaceValue(name, path, value) {
    const namespace = this.namespaces.get(name);
    if (!namespace) {
      throw new Error(`Namespace '${name}' not found`);
    }

    const pathParts = path.split('.');
    let current = namespace.data;

    // Navigate to the parent of the target property
    for (let i = 0; i < pathParts.length - 1; i++) {
      const part = pathParts[i];
      if (!current[part] || typeof current[part] !== 'object') {
        current[part] = {};
      }
      current = current[part];
    }

    // Set the value
    current[pathParts[pathParts.length - 1]] = value;

    // Update global namespace
    this.updateGlobalNamespace(name, path, value);
  }

  updateGlobalNamespace(name, path, value) {
    const pathParts = name.split('.');
    let current = this.globalNamespace;

    // Navigate to namespace in global object
    for (const part of pathParts) {
      current = current[part];
    }

    // Set the value in global namespace
    const valuePath = path.split('.');
    for (let i = 0; i < valuePath.length - 1; i++) {
      const part = valuePath[i];
      if (!current[part] || typeof current[part] !== 'object') {
        current[part] = {};
      }
      current = current[part];
    }

    current[valuePath[valuePath.length - 1]] = value;
  }

  // Component registration
  registerComponent(namespace, name, component) {
    this.setNamespaceValue(namespace, name, component);
  }

  // Utility function registration
  registerUtility(namespace, name, utility) {
    this.setNamespaceValue(namespace, name, utility);
  }

  // Namespace access from global scope
  getGlobalNamespace(name) {
    const pathParts = name.split('.');
    let current = this.globalNamespace;

    for (const part of pathParts) {
      if (!current[part]) {
        return undefined;
      }
      current = current[part];
    }

    return current;
  }
}

// Usage
const browserNamespaceManager = new BrowserNamespaceManager(browserConfig);

// Access namespace data
const appVersion = browserNamespaceManager.getNamespaceValue('app', 'version');
const theme = browserNamespaceManager.getNamespaceValue('app', 'config.theme');

console.log('App version:', appVersion);
console.log('Theme:', theme);

// Register components
browserNamespaceManager.registerComponent('app.components', 'Button', {
  render: (props) => `<button>${props.text}</button>`
});

browserNamespaceManager.registerComponent('app.components', 'Modal', {
  render: (props) => `<div class="modal">${props.content}</div>`
});

// Register utilities
browserNamespaceManager.registerUtility('app.utils', 'formatDate', (date) => {
  return date.toLocaleDateString();
});

browserNamespaceManager.registerUtility('app.utils', 'validateEmail', (email) => {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
});

// Access from global scope
const Button = window.app.components.Button;
const formatDate = window.app.utils.formatDate;

console.log('Button component:', Button);
console.log('Format date utility:', formatDate);
```

## Advanced Usage Scenarios

### Plugin Namespace
```tusk
# Plugin namespace system
plugin_namespace: @namespace("app.plugins", {
  registry: {},
  hooks: {},
  extensions: {}
}, {
  dynamic: true
})
```

### API Namespace
```tusk
# API namespace organization
api_namespace: @namespace("app.api", {
  v1: {
    endpoints: {},
    middleware: {}
  },
  v2: {
    endpoints: {},
    middleware: {}
  }
})
```

### Database Namespace
```tusk
# Database namespace
db_namespace: @namespace("app.database", {
  models: {},
  migrations: {},
  seeds: {}
})
```

## TypeScript Implementation

### Typed Namespace Manager
```typescript
interface NamespaceConfig {
  initial: Record<string, any>;
  imports?: string[];
  exports?: string[];
  dynamic?: boolean;
}

interface NamespaceData {
  name: string;
  path: string[];
  data: Record<string, any>;
  imports: string[];
  exports: string[];
  children: Map<string, NamespaceData>;
  parent: NamespaceData | null;
  dynamic: boolean;
}

class TypedNamespaceManager {
  private namespaces: Map<string, NamespaceData> = new Map();

  createNamespace(name: string, config: NamespaceConfig): void {
    const namespace: NamespaceData = {
      name,
      path: name.split('.'),
      data: { ...config.initial },
      imports: config.imports || [],
      exports: config.exports || [],
      children: new Map(),
      parent: null,
      dynamic: config.dynamic || false
    };

    this.namespaces.set(name, namespace);
  }

  getNamespace(name: string): NamespaceData | undefined {
    return this.namespaces.get(name);
  }

  getNamespaceValue<T>(name: string, path: string): T | undefined {
    const namespace = this.namespaces.get(name);
    if (!namespace) {
      return undefined;
    }

    const pathParts = path.split('.');
    let current: any = namespace.data;

    for (const part of pathParts) {
      if (current && typeof current === 'object' && current.hasOwnProperty(part)) {
        current = current[part];
      } else {
        return undefined;
      }
    }

    return current as T;
  }

  setNamespaceValue<T>(name: string, path: string, value: T): void {
    const namespace = this.namespaces.get(name);
    if (!namespace) {
      throw new Error(`Namespace '${name}' not found`);
    }

    const pathParts = path.split('.');
    let current: any = namespace.data;

    for (let i = 0; i < pathParts.length - 1; i++) {
      const part = pathParts[i];
      if (!current[part] || typeof current[part] !== 'object') {
        current[part] = {};
      }
      current = current[part];
    }

    current[pathParts[pathParts.length - 1]] = value;
  }
}
```

## Real-World Examples

### Express.js API Namespace
```javascript
// Express.js API with namespaces
const express = require('express');
const app = express();

const namespaceManager = new NamespaceManager(config);

// API v1 namespace
namespaceManager.setNamespaceValue('app.api.v1', 'endpoints.users', '/api/v1/users');
namespaceManager.setNamespaceValue('app.api.v1', 'endpoints.posts', '/api/v1/posts');

// API v2 namespace
namespaceManager.setNamespaceValue('app.api.v2', 'endpoints.users', '/api/v2/users');
namespaceManager.setNamespaceValue('app.api.v2', 'endpoints.posts', '/api/v2/posts');

// Route registration
app.get(namespaceManager.getNamespaceValue('app.api.v1', 'endpoints.users'), (req, res) => {
  res.json({ version: 'v1', endpoint: 'users' });
});

app.get(namespaceManager.getNamespaceValue('app.api.v2', 'endpoints.users'), (req, res) => {
  res.json({ version: 'v2', endpoint: 'users' });
});
```

### React Component Namespace
```javascript
// React components with namespace
import React from 'react';

const ComponentNamespace = {
  Button: ({ children, ...props }) => (
    <button className="btn" {...props}>{children}</button>
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
    <input type={type} className="input" {...props} />
  )
};

// Usage
const MyForm = () => {
  const [isModalOpen, setIsModalOpen] = useState(false);
  
  return (
    <div>
      <ComponentNamespace.Input placeholder="Enter text" />
      <ComponentNamespace.Button onClick={() => setIsModalOpen(true)}>
        Open Modal
      </ComponentNamespace.Button>
      <ComponentNamespace.Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)}>
        <h2>Modal Content</h2>
        <p>This is modal content.</p>
      </ComponentNamespace.Modal>
    </div>
  );
};
```

## Performance Considerations
- Use namespace caching for frequently accessed data
- Implement lazy loading for large namespaces
- Monitor namespace memory usage
- Clean up unused namespaces

## Security Notes
- Validate namespace names and paths
- Implement namespace access controls
- Sanitize namespace data
- Use namespace isolation for untrusted code

## Best Practices
- Use descriptive namespace names
- Keep namespace hierarchies shallow
- Implement proper namespace cleanup
- Document namespace structure

## Related Topics
- [@scope Operator](./72-scope-operator.md) - Variable scope management
- [@context Operator](./71-context-operator.md) - Contextual data management
- [Modules](./07-modules.md) - Module system
- [Data Structures](./05-data-structures.md) - Data organization 