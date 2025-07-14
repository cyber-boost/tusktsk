# @scope Operator - Variable Scope Management

## Overview
The `@scope` operator in TuskLang provides advanced variable scope management, enabling you to control the visibility and lifetime of variables across different parts of your application. It supports lexical scoping, block scoping, and dynamic scoping.

## TuskLang Syntax

### Basic Scope
```tusk
# Global scope
global_scope: @scope("global", {
  app_name: "MyApp",
  version: "1.0.0"
})

# Function scope
function_scope: @scope("function", {
  local_var: "function_local",
  temp_data: {}
})

# Block scope
block_scope: @scope("block", {
  loop_counter: 0,
  temp_result: null
})
```

### Nested Scopes
```tusk
# Nested scope hierarchy
nested_scopes: {
  outer: @scope("outer", {
    outer_var: "outer_value"
  }),
  
  inner: @scope("inner", {
    inner_var: "inner_value"
  }, {
    parent: "outer"
  })
}
```

### Scope with Inheritance
```tusk
# Scope inheritance
inherited_scope: @scope("child", {
  child_var: "child_value"
}, {
  inherit: ["parent_scope"],
  override: false
})
```

## JavaScript Integration

### Node.js Scope Management
```javascript
const tusklang = require('@tusklang/core');

const config = tusklang.parse(`
scope_config: {
  global: @scope("global", {
    app_name: "MyApp",
    version: "1.0.0"
  }),
  
  function: @scope("function", {
    local_var: "function_local"
  })
}
`);

class ScopeManager {
  constructor(config) {
    this.config = config.scope_config;
    this.scopes = new Map();
    this.scopeStack = [];
    this.initializeScopes();
  }

  initializeScopes() {
    Object.entries(this.config).forEach(([name, scopeConfig]) => {
      this.createScope(name, scopeConfig);
    });
  }

  createScope(name, config) {
    const scope = {
      name: name,
      variables: { ...config.initial },
      parent: config.parent || null,
      children: [],
      inherit: config.inherit || [],
      override: config.override !== false
    };

    this.scopes.set(name, scope);

    // Set up inheritance
    if (scope.parent) {
      const parentScope = this.scopes.get(scope.parent);
      if (parentScope) {
        parentScope.children.push(scope);
      }
    }
  }

  enterScope(name) {
    const scope = this.scopes.get(name);
    if (!scope) {
      throw new Error(`Scope '${name}' not found`);
    }

    this.scopeStack.push(scope);
    return scope;
  }

  exitScope() {
    if (this.scopeStack.length > 0) {
      return this.scopeStack.pop();
    }
    return null;
  }

  getCurrentScope() {
    return this.scopeStack.length > 0 ? this.scopeStack[this.scopeStack.length - 1] : null;
  }

  getVariable(name) {
    // Search current scope and parent scopes
    for (let i = this.scopeStack.length - 1; i >= 0; i--) {
      const scope = this.scopeStack[i];
      if (scope.variables.hasOwnProperty(name)) {
        return scope.variables[name];
      }
    }
    return undefined;
  }

  setVariable(name, value) {
    const currentScope = this.getCurrentScope();
    if (!currentScope) {
      throw new Error('No active scope');
    }

    currentScope.variables[name] = value;
  }

  hasVariable(name) {
    return this.getVariable(name) !== undefined;
  }

  // Function scope wrapper
  withFunctionScope(callback, variables = {}) {
    this.enterScope('function');
    
    // Set function variables
    Object.entries(variables).forEach(([name, value]) => {
      this.setVariable(name, value);
    });

    try {
      return callback();
    } finally {
      this.exitScope();
    }
  }

  // Block scope wrapper
  withBlockScope(callback, variables = {}) {
    this.enterScope('block');
    
    // Set block variables
    Object.entries(variables).forEach(([name, value]) => {
      this.setVariable(name, value);
    });

    try {
      return callback();
    } finally {
      this.exitScope();
    }
  }

  // Global scope access
  getGlobalVariable(name) {
    const globalScope = this.scopes.get('global');
    return globalScope ? globalScope.variables[name] : undefined;
  }

  setGlobalVariable(name, value) {
    const globalScope = this.scopes.get('global');
    if (globalScope) {
      globalScope.variables[name] = value;
    }
  }

  // Scope debugging
  debugScope() {
    console.log('Current scope stack:');
    this.scopeStack.forEach((scope, index) => {
      console.log(`  ${index}: ${scope.name}`, scope.variables);
    });
  }
}

// Usage
const scopeManager = new ScopeManager(config);

// Enter global scope
scopeManager.enterScope('global');

// Set global variables
scopeManager.setGlobalVariable('debug_mode', true);
scopeManager.setGlobalVariable('api_url', 'https://api.example.com');

// Function with local scope
const result = scopeManager.withFunctionScope(() => {
  scopeManager.setVariable('local_var', 'function_value');
  scopeManager.setVariable('temp_data', { id: 1, name: 'test' });
  
  console.log('Function scope variable:', scopeManager.getVariable('local_var'));
  console.log('Global variable:', scopeManager.getGlobalVariable('app_name'));
  
  return 'function_result';
}, { param1: 'value1', param2: 'value2' });

// Block scope
scopeManager.withBlockScope(() => {
  scopeManager.setVariable('loop_counter', 0);
  scopeManager.setVariable('temp_result', null);
  
  for (let i = 0; i < 5; i++) {
    scopeManager.setVariable('loop_counter', i);
    scopeManager.setVariable('temp_result', i * 2);
  }
  
  console.log('Final counter:', scopeManager.getVariable('loop_counter'));
});

scopeManager.exitScope();
```

### Browser Scope Management
```javascript
// Browser-based scope management
const browserConfig = tusklang.parse(`
browser_scope: {
  global: @scope("global", {
    theme: "light",
    language: "en"
  }),
  
  component: @scope("component", {
    state: {},
    props: {}
  })
}
`);

class BrowserScopeManager {
  constructor(config) {
    this.config = config.browser_scope;
    this.scopes = new Map();
    this.scopeStack = [];
    this.initializeScopes();
  }

  initializeScopes() {
    Object.entries(this.config).forEach(([name, scopeConfig]) => {
      this.createScope(name, scopeConfig);
    });
  }

  createScope(name, config) {
    const scope = {
      name: name,
      variables: { ...config.initial },
      parent: config.parent || null,
      children: []
    };

    this.scopes.set(name, scope);
  }

  enterScope(name) {
    const scope = this.scopes.get(name);
    if (!scope) {
      throw new Error(`Scope '${name}' not found`);
    }

    this.scopeStack.push(scope);
    return scope;
  }

  exitScope() {
    return this.scopeStack.pop();
  }

  getVariable(name) {
    for (let i = this.scopeStack.length - 1; i >= 0; i--) {
      const scope = this.scopeStack[i];
      if (scope.variables.hasOwnProperty(name)) {
        return scope.variables[name];
      }
    }
    return undefined;
  }

  setVariable(name, value) {
    const currentScope = this.getCurrentScope();
    if (!currentScope) {
      throw new Error('No active scope');
    }

    currentScope.variables[name] = value;
  }

  getCurrentScope() {
    return this.scopeStack.length > 0 ? this.scopeStack[this.scopeStack.length - 1] : null;
  }

  // Component scope wrapper
  withComponentScope(callback, props = {}) {
    this.enterScope('component');
    
    // Set component props
    this.setVariable('props', props);
    this.setVariable('state', {});

    try {
      return callback();
    } finally {
      this.exitScope();
    }
  }

  // Event handler scope
  withEventHandlerScope(callback, eventData = {}) {
    this.enterScope('event');
    
    // Set event variables
    this.setVariable('event', eventData);
    this.setVariable('timestamp', Date.now());

    try {
      return callback();
    } finally {
      this.exitScope();
    }
  }
}

// Usage
const browserScopeManager = new BrowserScopeManager(browserConfig);

// Global scope
browserScopeManager.enterScope('global');
browserScopeManager.setVariable('theme', 'dark');
browserScopeManager.setVariable('language', 'en');

// Component scope
browserScopeManager.withComponentScope(() => {
  const props = { id: 1, name: 'MyComponent' };
  browserScopeManager.setVariable('props', props);
  browserScopeManager.setVariable('state', { loading: false, data: null });
  
  console.log('Component props:', browserScopeManager.getVariable('props'));
  console.log('Global theme:', browserScopeManager.getVariable('theme'));
}, { id: 1, name: 'MyComponent' });

// Event handler scope
browserScopeManager.withEventHandlerScope(() => {
  const eventData = { type: 'click', target: 'button' };
  browserScopeManager.setVariable('event', eventData);
  
  console.log('Event type:', browserScopeManager.getVariable('event').type);
}, { type: 'click', target: 'button' });

browserScopeManager.exitScope();
```

## Advanced Usage Scenarios

### Module Scope
```tusk
# Module-level scope
module_scope: @scope("module", {
  exports: {},
  imports: {},
  private_vars: {}
}, {
  persistent: true
})
```

### Class Scope
```tusk
# Class instance scope
class_scope: @scope("class", {
  instance_vars: {},
  methods: {},
  static_vars: {}
})
```

### Async Scope
```tusk
# Async operation scope
async_scope: @scope("async", {
  promise_id: @uuid(),
  timeout: 5000,
  retry_count: 0
})
```

## TypeScript Implementation

### Typed Scope Manager
```typescript
interface ScopeConfig {
  initial: Record<string, any>;
  parent?: string;
  inherit?: string[];
  override?: boolean;
}

interface ScopeData {
  name: string;
  variables: Record<string, any>;
  parent: string | null;
  children: ScopeData[];
  inherit: string[];
  override: boolean;
}

class TypedScopeManager {
  private scopes: Map<string, ScopeData> = new Map();
  private scopeStack: ScopeData[] = [];

  createScope(name: string, config: ScopeConfig): void {
    const scope: ScopeData = {
      name,
      variables: { ...config.initial },
      parent: config.parent || null,
      children: [],
      inherit: config.inherit || [],
      override: config.override !== false
    };

    this.scopes.set(name, scope);
  }

  enterScope(name: string): ScopeData {
    const scope = this.scopes.get(name);
    if (!scope) {
      throw new Error(`Scope '${name}' not found`);
    }

    this.scopeStack.push(scope);
    return scope;
  }

  exitScope(): ScopeData | undefined {
    return this.scopeStack.pop();
  }

  getVariable<T>(name: string): T | undefined {
    for (let i = this.scopeStack.length - 1; i >= 0; i--) {
      const scope = this.scopeStack[i];
      if (scope.variables.hasOwnProperty(name)) {
        return scope.variables[name] as T;
      }
    }
    return undefined;
  }

  setVariable<T>(name: string, value: T): void {
    const currentScope = this.getCurrentScope();
    if (!currentScope) {
      throw new Error('No active scope');
    }

    currentScope.variables[name] = value;
  }

  private getCurrentScope(): ScopeData | null {
    return this.scopeStack.length > 0 ? this.scopeStack[this.scopeStack.length - 1] : null;
  }
}
```

## Real-World Examples

### Express.js Middleware Scope
```javascript
// Express.js middleware with scope
const express = require('express');
const app = express();

const scopeManager = new ScopeManager(config);

app.use((req, res, next) => {
  scopeManager.enterScope('request');
  scopeManager.setVariable('request_id', req.headers['x-request-id']);
  scopeManager.setVariable('user_id', req.headers['x-user-id']);
  
  req.scope = scopeManager;
  next();
});

app.use((req, res, next) => {
  scopeManager.withFunctionScope(() => {
    scopeManager.setVariable('processing_time', Date.now());
    next();
  });
});

app.get('/api/data', (req, res) => {
  const requestId = req.scope.getVariable('request_id');
  const processingTime = req.scope.getVariable('processing_time');
  
  res.json({
    data: 'some data',
    request_id: requestId,
    processing_time: processingTime
  });
});
```

### React Component Scope
```javascript
// React component with scope
import React, { useEffect, useState } from 'react';

const ComponentScope = ({ children }) => {
  const [scopeManager] = useState(() => new BrowserScopeManager(browserConfig));

  useEffect(() => {
    scopeManager.enterScope('component');
    return () => scopeManager.exitScope();
  }, []);

  return (
    <ScopeContext.Provider value={scopeManager}>
      {children}
    </ScopeContext.Provider>
  );
};

const useScope = () => {
  const scopeManager = useContext(ScopeContext);
  if (!scopeManager) {
    throw new Error('useScope must be used within ComponentScope');
  }
  return scopeManager;
};

const MyComponent = () => {
  const scope = useScope();
  
  useEffect(() => {
    scope.setVariable('component_mounted', true);
    scope.setVariable('mount_time', Date.now());
  }, []);

  return <div>Component with scope</div>;
};
```

## Performance Considerations
- Minimize scope nesting depth
- Clean up scopes when no longer needed
- Use scope pooling for frequently created scopes
- Monitor scope memory usage

## Security Notes
- Validate scope variable names
- Implement scope isolation for untrusted code
- Sanitize scope data before persistence
- Use scope encryption for sensitive data

## Best Practices
- Keep scope hierarchies shallow
- Use descriptive scope names
- Implement proper scope cleanup
- Monitor scope usage for debugging

## Related Topics
- [@context Operator](./71-context-operator.md) - Contextual data management
- [@state Operator](./70-state-operator.md) - State management
- [Variables](./02-syntax-basics.md) - Variable basics
- [Modules](./07-modules.md) - Module system 