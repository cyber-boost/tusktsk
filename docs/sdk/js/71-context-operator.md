# @context Operator - Contextual Data Management

## Overview
The `@context` operator in TuskLang provides contextual data management, enabling you to pass and manage context information across different parts of your application. It supports request context, user context, and application context with automatic propagation.

## TuskLang Syntax

### Basic Context
```tusk
# Request context
request_context: @context("request", {
  user_id: @env("USER_ID"),
  session_id: @env("SESSION_ID"),
  request_id: @uuid()
})

# User context
user_context: @context("user", {
  id: null,
  permissions: [],
  preferences: {}
})
```

### Context with Inheritance
```tusk
# Context inheritance
inherited_context: @context("app", {
  environment: @env("NODE_ENV"),
  version: "1.0.0"
}, {
  inherit: ["request", "user"]
})
```

### Context with Validation
```tusk
# Validated context
validated_context: @context("api", {
  api_key: @env("API_KEY"),
  rate_limit: 1000
}, {
  validate: {
    api_key: "required",
    rate_limit: "number|min:1"
  }
})
```

## JavaScript Integration

### Node.js Context Management
```javascript
const tusklang = require('@tusklang/core');

const config = tusklang.parse(`
context_config: {
  request: @context("request", {
    user_id: @env("USER_ID"),
    session_id: @env("SESSION_ID"),
    request_id: @uuid()
  }),
  
  user: @context("user", {
    id: null,
    permissions: [],
    preferences: {}
  })
}
`);

class ContextManager {
  constructor(config) {
    this.config = config.context_config;
    this.contexts = new Map();
    this.currentContext = null;
    this.initializeContexts();
  }

  initializeContexts() {
    Object.entries(this.config).forEach(([name, contextConfig]) => {
      this.createContext(name, contextConfig);
    });
  }

  createContext(name, config) {
    const context = {
      data: { ...config.initial },
      validators: config.validate || {},
      inherit: config.inherit || [],
      parent: null
    };

    this.contexts.set(name, context);
  }

  getContext(name) {
    return this.contexts.get(name);
  }

  setContext(name, data) {
    const context = this.contexts.get(name);
    if (!context) {
      throw new Error(`Context '${name}' not found`);
    }

    // Validate data if validators exist
    if (context.validators) {
      this.validateContextData(data, context.validators);
    }

    Object.assign(context.data, data);
  }

  validateContextData(data, validators) {
    Object.entries(validators).forEach(([field, rules]) => {
      const value = data[field];
      if (!this.validateValue(value, rules)) {
        throw new Error(`Validation failed for ${field}`);
      }
    });
  }

  validateValue(value, rules) {
    const ruleList = rules.split('|');
    
    for (const rule of ruleList) {
      if (!this.applyValidationRule(value, rule)) {
        return false;
      }
    }
    
    return true;
  }

  applyValidationRule(value, rule) {
    switch (rule) {
      case 'required':
        return value !== null && value !== undefined && value !== '';
      case 'number':
        return typeof value === 'number' && !isNaN(value);
      default:
        if (rule.startsWith('min:')) {
          const min = parseInt(rule.split(':')[1]);
          return value >= min;
        }
        return true;
    }
  }

  // Request context middleware
  requestContextMiddleware() {
    return (req, res, next) => {
      const requestContext = this.getContext('request');
      
      // Set request-specific data
      this.setContext('request', {
        user_id: req.headers['user-id'],
        session_id: req.headers['session-id'],
        request_id: this.generateRequestId(),
        method: req.method,
        url: req.url,
        timestamp: new Date()
      });

      // Set current context
      this.currentContext = 'request';
      
      // Add context to request object
      req.context = requestContext.data;
      
      next();
    };
  }

  // User context middleware
  userContextMiddleware() {
    return (req, res, next) => {
      if (req.user) {
        const userContext = this.getContext('user');
        
        this.setContext('user', {
          id: req.user.id,
          permissions: req.user.permissions || [],
          preferences: req.user.preferences || {}
        });

        // Merge with request context
        req.context = {
          ...req.context,
          ...userContext.data
        };
      }
      
      next();
    };
  }

  generateRequestId() {
    return `req_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  // Get current context data
  getCurrentContext() {
    if (!this.currentContext) {
      return {};
    }

    const context = this.contexts.get(this.currentContext);
    return context ? context.data : {};
  }

  // Clear context
  clearContext(name) {
    const context = this.contexts.get(name);
    if (context) {
      context.data = {};
    }
  }
}

// Usage
const contextManager = new ContextManager(config);

// Express middleware setup
const express = require('express');
const app = express();

app.use(contextManager.requestContextMiddleware());
app.use(contextManager.userContextMiddleware());

// Route using context
app.get('/api/data', (req, res) => {
  const context = contextManager.getCurrentContext();
  console.log('Request context:', context);
  
  res.json({
    data: 'some data',
    context: context
  });
});
```

### Browser Context Management
```javascript
// Browser-based context management
const browserConfig = tusklang.parse(`
browser_context: {
  app: @context("app", {
    theme: "light",
    language: "en",
    user_id: null
  }),
  
  session: @context("session", {
    token: null,
    permissions: []
  })
}
`);

class BrowserContextManager {
  constructor(config) {
    this.config = config.browser_context;
    this.contexts = new Map();
    this.initializeContexts();
  }

  initializeContexts() {
    Object.entries(this.config).forEach(([name, contextConfig]) => {
      this.createContext(name, contextConfig);
    });
  }

  createContext(name, config) {
    const context = {
      data: { ...config.initial },
      watchers: [],
      persist: config.persist || false
    };

    this.contexts.set(name, context);

    // Load from localStorage if persistent
    if (context.persist) {
      this.loadPersistedContext(name);
    }
  }

  getContext(name) {
    const context = this.contexts.get(name);
    return context ? context.data : null;
  }

  setContext(name, data) {
    const context = this.contexts.get(name);
    if (!context) {
      throw new Error(`Context '${name}' not found`);
    }

    const oldData = { ...context.data };
    Object.assign(context.data, data);

    // Notify watchers
    this.notifyWatchers(name, context.data, oldData);

    // Persist if needed
    if (context.persist) {
      this.persistContext(name, context.data);
    }
  }

  watch(name, callback) {
    const context = this.contexts.get(name);
    if (context) {
      context.watchers.push(callback);
    }
  }

  notifyWatchers(name, newData, oldData) {
    const context = this.contexts.get(name);
    if (context) {
      context.watchers.forEach(callback => {
        try {
          callback(newData, oldData);
        } catch (error) {
          console.error('Context watcher error:', error);
        }
      });
    }
  }

  persistContext(name, data) {
    try {
      localStorage.setItem(`context_${name}`, JSON.stringify(data));
    } catch (error) {
      console.error('Failed to persist context:', error);
    }
  }

  loadPersistedContext(name) {
    try {
      const data = localStorage.getItem(`context_${name}`);
      if (data) {
        const context = this.contexts.get(name);
        if (context) {
          Object.assign(context.data, JSON.parse(data));
        }
      }
    } catch (error) {
      console.error('Failed to load persisted context:', error);
    }
  }

  // API request with context
  async apiRequest(url, options = {}) {
    const appContext = this.getContext('app');
    const sessionContext = this.getContext('session');

    const headers = {
      'Content-Type': 'application/json',
      'X-User-ID': appContext.user_id,
      'X-Theme': appContext.theme,
      'X-Language': appContext.language,
      ...options.headers
    };

    if (sessionContext.token) {
      headers['Authorization'] = `Bearer ${sessionContext.token}`;
    }

    const response = await fetch(url, {
      ...options,
      headers
    });

    return response;
  }
}

// Usage
const browserContextManager = new BrowserContextManager(browserConfig);

// Set context
browserContextManager.setContext('app', {
  theme: 'dark',
  user_id: 'user123'
});

browserContextManager.setContext('session', {
  token: 'jwt_token_here',
  permissions: ['read', 'write']
});

// Watch for changes
browserContextManager.watch('app', (newData, oldData) => {
  console.log('App context changed:', { newData, oldData });
});

// API request with context
browserContextManager.apiRequest('/api/data')
  .then(response => response.json())
  .then(data => console.log('API response:', data));
```

## Advanced Usage Scenarios

### Multi-Tenant Context
```tusk
# Multi-tenant application context
tenant_context: @context("tenant", {
  tenant_id: @env("TENANT_ID"),
  database: @env("TENANT_DB"),
  settings: {}
}, {
  validate: {
    tenant_id: "required",
    database: "required"
  }
})
```

### Transaction Context
```tusk
# Database transaction context
transaction_context: @context("transaction", {
  transaction_id: @uuid(),
  start_time: @date.now(),
  isolation_level: "READ_COMMITTED"
})
```

### Security Context
```tusk
# Security context for authentication
security_context: @context("security", {
  user_id: null,
  roles: [],
  permissions: [],
  session_expiry: null
}, {
  encrypt: true
})
```

## TypeScript Implementation

### Typed Context Manager
```typescript
interface ContextConfig {
  initial: Record<string, any>;
  validate?: Record<string, string>;
  inherit?: string[];
  persist?: boolean;
}

interface ContextData {
  data: Record<string, any>;
  validators: Record<string, string>;
  inherit: string[];
  watchers: Function[];
  persist: boolean;
}

class TypedContextManager {
  private contexts: Map<string, ContextData> = new Map();

  createContext<T extends Record<string, any>>(
    name: string,
    config: ContextConfig
  ): T {
    const context: ContextData = {
      data: { ...config.initial },
      validators: config.validate || {},
      inherit: config.inherit || [],
      watchers: [],
      persist: !!config.persist
    };

    this.contexts.set(name, context);
    return context.data as T;
  }

  getContext<T>(name: string): T | null {
    const context = this.contexts.get(name);
    return context ? context.data as T : null;
  }

  setContext<T>(name: string, data: Partial<T>): void {
    const context = this.contexts.get(name);
    if (!context) {
      throw new Error(`Context '${name}' not found`);
    }

    Object.assign(context.data, data);
  }

  watch<T>(name: string, callback: (newData: T, oldData: T) => void): void {
    const context = this.contexts.get(name);
    if (context) {
      context.watchers.push(callback as Function);
    }
  }
}
```

## Real-World Examples

### Express.js Middleware
```javascript
// Express.js context middleware
const express = require('express');
const app = express();

const contextManager = new ContextManager(config);

// Request context middleware
app.use((req, res, next) => {
  contextManager.setContext('request', {
    id: req.headers['x-request-id'],
    user_id: req.headers['x-user-id'],
    timestamp: new Date()
  });
  
  req.context = contextManager.getCurrentContext();
  next();
});

// Route using context
app.get('/api/user', (req, res) => {
  const context = req.context;
  console.log('Processing request for user:', context.user_id);
  
  res.json({ user_id: context.user_id });
});
```

### React Context Provider
```javascript
// React context provider
import React, { createContext, useContext, useState } from 'react';

const AppContext = createContext();

export const AppProvider = ({ children }) => {
  const [context, setContext] = useState({
    theme: 'light',
    user: null,
    settings: {}
  });

  const updateContext = (updates) => {
    setContext(prev => ({ ...prev, ...updates }));
  };

  return (
    <AppContext.Provider value={{ context, updateContext }}>
      {children}
    </AppContext.Provider>
  );
};

export const useAppContext = () => {
  const context = useContext(AppContext);
  if (!context) {
    throw new Error('useAppContext must be used within AppProvider');
  }
  return context;
};
```

## Performance Considerations
- Use context sparingly to avoid memory leaks
- Implement context cleanup for long-running applications
- Monitor context size and complexity
- Use selective watching to avoid unnecessary updates

## Security Notes
- Validate all context data
- Encrypt sensitive context information
- Implement proper access controls for context data
- Sanitize context data before persistence

## Best Practices
- Keep context data minimal and focused
- Use descriptive context names
- Implement proper error handling for context operations
- Monitor context usage for debugging

## Related Topics
- [@state Operator](./70-state-operator.md) - State management
- [@env Operator](./41-json-operator.md) - Environment variables
- [@workflow Operator](./69-workflow-operator.md) - Business process automation
- [Security](./14-security.md) - Security best practices 