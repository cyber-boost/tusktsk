# @state Operator - State Management

## Overview
The `@state` operator in TuskLang provides powerful state management capabilities, enabling persistent, reactive, and distributed state across your applications. It supports both local and global state with automatic synchronization and conflict resolution.

## TuskLang Syntax

### Basic State Management
```tusk
# Simple state variable
user_state: @state("user", {
  name: "John Doe",
  email: "john@example.com",
  preferences: { theme: "dark" }
})

# Reactive state with watchers
reactive_state: @state("app", {
  count: 0,
  items: []
}, {
  watch: ["count", "items"],
  persist: true
})
```

### Global State
```tusk
# Global application state
global_state: @state.global("app", {
  user: null,
  settings: {},
  notifications: []
})

# Shared state across components
shared_state: @state.shared("session", {
  token: null,
  permissions: []
})
```

### State with Validation
```tusk
# State with validation rules
validated_state: @state("user_profile", {
  name: "",
  age: 0,
  email: ""
}, {
  validate: {
    name: "required|min:2",
    age: "number|min:0|max:120",
    email: "email"
  }
})
```

### State with Persistence
```tusk
# Persistent state with storage options
persistent_state: @state("user_preferences", {
  theme: "light",
  language: "en",
  notifications: true
}, {
  persist: {
    storage: "localStorage",
    key: "user_prefs"
  }
})
```

## JavaScript Integration

### Node.js State Management
```javascript
const tusklang = require('@tusklang/core');

const config = tusklang.parse(`
state_config: {
  user: @state("user", {
    id: null,
    name: "",
    email: "",
    preferences: {}
  }),
  
  app: @state.global("app", {
    theme: "light",
    language: "en",
    notifications: []
  })
}
`);

class StateManager {
  constructor(config) {
    this.config = config.state_config;
    this.states = new Map();
    this.watchers = new Map();
    this.initializeStates();
  }

  initializeStates() {
    Object.entries(this.config).forEach(([name, stateConfig]) => {
      this.createState(name, stateConfig);
    });
  }

  createState(name, config) {
    const state = {
      data: { ...config.initial },
      watchers: [],
      validators: config.validate || {},
      persist: config.persist || false
    };

    this.states.set(name, state);

    // Load persisted state if available
    if (state.persist) {
      this.loadPersistedState(name);
    }

    // Create proxy for reactivity
    this.createReactiveProxy(name, state);
  }

  createReactiveProxy(name, state) {
    const proxy = new Proxy(state.data, {
      get: (target, property) => {
        return target[property];
      },
      set: (target, property, value) => {
        const oldValue = target[property];
        target[property] = value;

        // Validate if validator exists
        if (state.validators[property]) {
          const isValid = this.validateValue(value, state.validators[property]);
          if (!isValid) {
            target[property] = oldValue;
            throw new Error(`Validation failed for ${property}`);
          }
        }

        // Notify watchers
        this.notifyWatchers(name, property, value, oldValue);

        // Persist if needed
        if (state.persist) {
          this.persistState(name);
        }

        return true;
      }
    });

    state.proxy = proxy;
  }

  getState(name) {
    const state = this.states.get(name);
    return state ? state.proxy : null;
  }

  setState(name, updates) {
    const state = this.states.get(name);
    if (!state) {
      throw new Error(`State '${name}' not found`);
    }

    Object.entries(updates).forEach(([key, value]) => {
      state.proxy[key] = value;
    });
  }

  watch(name, callback) {
    if (!this.watchers.has(name)) {
      this.watchers.set(name, []);
    }
    this.watchers.get(name).push(callback);
  }

  notifyWatchers(name, property, newValue, oldValue) {
    const watchers = this.watchers.get(name) || [];
    watchers.forEach(callback => {
      try {
        callback(property, newValue, oldValue);
      } catch (error) {
        console.error('State watcher error:', error);
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
      case 'email':
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
      case 'number':
        return typeof value === 'number' && !isNaN(value);
      default:
        if (rule.startsWith('min:')) {
          const min = parseInt(rule.split(':')[1]);
          return value >= min;
        }
        if (rule.startsWith('max:')) {
          const max = parseInt(rule.split(':')[1]);
          return value <= max;
        }
        return true;
    }
  }

  async persistState(name) {
    const state = this.states.get(name);
    if (!state || !state.persist) return;

    try {
      const fs = require('fs').promises;
      const data = JSON.stringify(state.data);
      await fs.writeFile(`${name}_state.json`, data);
    } catch (error) {
      console.error('Failed to persist state:', error);
    }
  }

  async loadPersistedState(name) {
    try {
      const fs = require('fs').promises;
      const data = await fs.readFile(`${name}_state.json`, 'utf8');
      const persistedData = JSON.parse(data);
      
      const state = this.states.get(name);
      if (state) {
        Object.assign(state.data, persistedData);
      }
    } catch (error) {
      // State file doesn't exist or is invalid, use defaults
      console.log(`No persisted state found for ${name}, using defaults`);
    }
  }

  // Global state methods
  getGlobalState(name) {
    return this.getState(name);
  }

  setGlobalState(name, updates) {
    this.setState(name, updates);
    // Notify all connected clients in distributed setup
    this.broadcastStateChange(name, updates);
  }

  broadcastStateChange(name, updates) {
    // Implementation for distributed state synchronization
    console.log(`Broadcasting state change for ${name}:`, updates);
  }
}

// Usage
const stateManager = new StateManager(config);

// Get and set state
const userState = stateManager.getState('user');
userState.name = 'Jane Doe';
userState.email = 'jane@example.com';

// Watch for changes
stateManager.watch('user', (property, newValue, oldValue) => {
  console.log(`User ${property} changed from ${oldValue} to ${newValue}`);
});

// Global state
const appState = stateManager.getGlobalState('app');
appState.theme = 'dark';
```

### Browser State Management
```javascript
// Browser-based state management with localStorage
const browserConfig = tusklang.parse(`
browser_state: {
  user: @state("user", {
    name: "",
    email: "",
    preferences: {}
  }, {
    persist: { storage: "localStorage" }
  }),
  
  app: @state("app", {
    theme: "light",
    language: "en"
  }, {
    persist: { storage: "sessionStorage" }
  })
}
`);

class BrowserStateManager {
  constructor(config) {
    this.config = config.browser_state;
    this.states = new Map();
    this.watchers = new Map();
    this.initializeStates();
  }

  initializeStates() {
    Object.entries(this.config).forEach(([name, stateConfig]) => {
      this.createState(name, stateConfig);
    });
  }

  createState(name, config) {
    const state = {
      data: { ...config.initial },
      watchers: [],
      persist: config.persist || false,
      storage: config.persist?.storage || 'localStorage'
    };

    this.states.set(name, state);

    // Load persisted state
    if (state.persist) {
      this.loadPersistedState(name, state);
    }

    // Create reactive proxy
    this.createReactiveProxy(name, state);
  }

  createReactiveProxy(name, state) {
    const proxy = new Proxy(state.data, {
      get: (target, property) => target[property],
      set: (target, property, value) => {
        const oldValue = target[property];
        target[property] = value;

        // Notify watchers
        this.notifyWatchers(name, property, value, oldValue);

        // Persist if needed
        if (state.persist) {
          this.persistState(name, state);
        }

        return true;
      }
    });

    state.proxy = proxy;
  }

  getState(name) {
    const state = this.states.get(name);
    return state ? state.proxy : null;
  }

  setState(name, updates) {
    const state = this.states.get(name);
    if (!state) {
      throw new Error(`State '${name}' not found`);
    }

    Object.entries(updates).forEach(([key, value]) => {
      state.proxy[key] = value;
    });
  }

  watch(name, callback) {
    if (!this.watchers.has(name)) {
      this.watchers.set(name, []);
    }
    this.watchers.get(name).push(callback);
  }

  notifyWatchers(name, property, newValue, oldValue) {
    const watchers = this.watchers.get(name) || [];
    watchers.forEach(callback => {
      try {
        callback(property, newValue, oldValue);
      } catch (error) {
        console.error('State watcher error:', error);
      }
    });
  }

  persistState(name, state) {
    try {
      const storage = window[state.storage];
      const data = JSON.stringify(state.data);
      storage.setItem(`${name}_state`, data);
    } catch (error) {
      console.error('Failed to persist state:', error);
    }
  }

  loadPersistedState(name, state) {
    try {
      const storage = window[state.storage];
      const data = storage.getItem(`${name}_state`);
      if (data) {
        const persistedData = JSON.parse(data);
        Object.assign(state.data, persistedData);
      }
    } catch (error) {
      console.error('Failed to load persisted state:', error);
    }
  }

  // React-like hooks for components
  useState(name, initialValue) {
    const state = this.getState(name);
    if (!state) {
      this.createState(name, { initial: initialValue });
    }
    
    return [
      this.getState(name),
      (updates) => this.setState(name, updates)
    ];
  }

  useEffect(name, callback, dependencies = []) {
    this.watch(name, callback);
    
    // Cleanup function
    return () => {
      const watchers = this.watchers.get(name) || [];
      const index = watchers.indexOf(callback);
      if (index > -1) {
        watchers.splice(index, 1);
      }
    };
  }
}

// Usage
const browserStateManager = new BrowserStateManager(browserConfig);

// React-like usage
const [user, setUser] = browserStateManager.useState('user', {
  name: '',
  email: ''
});

const [app, setApp] = browserStateManager.useState('app', {
  theme: 'light',
  language: 'en'
});

// Update state
setUser({ name: 'John Doe', email: 'john@example.com' });
setApp({ theme: 'dark' });

// Watch for changes
browserStateManager.useEffect('user', (property, newValue, oldValue) => {
  console.log(`User ${property} changed: ${oldValue} -> ${newValue}`);
});
```

## Advanced Usage Scenarios

### Distributed State Management
```tusk
# Distributed state across multiple nodes
distributed_state: @state.distributed("session", {
  user_id: null,
  session_data: {},
  last_activity: null
}, {
  sync: "websocket",
  conflict_resolution: "last_write_wins"
})
```

### State with Time-to-Live
```tusk
# State with automatic expiration
temporary_state: @state("cache", {
  data: {},
  timestamp: null
}, {
  ttl: "1h",
  auto_cleanup: true
})
```

### State with Encryption
```tusk
# Encrypted state for sensitive data
secure_state: @state("credentials", {
  token: null,
  refresh_token: null
}, {
  encrypt: true,
  algorithm: "AES-256-GCM"
})
```

## TypeScript Implementation

### Typed State Manager
```typescript
interface StateConfig {
  initial: Record<string, any>;
  validate?: Record<string, string>;
  persist?: boolean | { storage: string; key?: string };
  watch?: string[];
}

interface StateData {
  data: Record<string, any>;
  proxy: ProxyHandler<any>;
  watchers: Function[];
  validators: Record<string, string>;
  persist: boolean;
}

class TypedStateManager {
  private states: Map<string, StateData> = new Map();
  private watchers: Map<string, Function[]> = new Map();

  createState<T extends Record<string, any>>(
    name: string, 
    config: StateConfig
  ): T {
    const state: StateData = {
      data: { ...config.initial },
      proxy: {} as ProxyHandler<any>,
      watchers: [],
      validators: config.validate || {},
      persist: !!config.persist
    };

    this.states.set(name, state);
    this.createReactiveProxy(name, state);
    
    return state.proxy as T;
  }

  getState<T>(name: string): T | null {
    const state = this.states.get(name);
    return state ? state.proxy as T : null;
  }

  setState<T>(name: string, updates: Partial<T>): void {
    const state = this.states.get(name);
    if (!state) {
      throw new Error(`State '${name}' not found`);
    }

    Object.entries(updates).forEach(([key, value]) => {
      state.proxy[key] = value;
    });
  }

  watch<T>(name: string, callback: (property: keyof T, newValue: any, oldValue: any) => void): void {
    if (!this.watchers.has(name)) {
      this.watchers.set(name, []);
    }
    this.watchers.get(name)!.push(callback as Function);
  }

  private createReactiveProxy(name: string, state: StateData): void {
    // Proxy implementation
  }
}
```

## Real-World Examples

### User Session Management
```javascript
// User session state
const sessionState = stateManager.createState('session', {
  initial: {
    user: null,
    token: null,
    permissions: []
  },
  persist: { storage: 'localStorage' },
  validate: {
    token: 'required'
  }
});

// Watch for authentication changes
stateManager.watch('session', (property, newValue, oldValue) => {
  if (property === 'user' && newValue) {
    console.log('User logged in:', newValue);
  } else if (property === 'user' && !newValue) {
    console.log('User logged out');
  }
});
```

### Application Settings
```javascript
// App settings state
const settingsState = stateManager.createState('settings', {
  initial: {
    theme: 'light',
    language: 'en',
    notifications: true,
    autoSave: true
  },
  persist: { storage: 'localStorage' }
});

// Apply theme changes
settingsState.theme = 'dark';
document.body.className = `theme-${settingsState.theme}`;
```

## Performance Considerations
- Use selective watching to avoid unnecessary updates
- Implement state batching for multiple updates
- Use immutable updates for large state objects
- Monitor state size and cleanup unused state

## Security Notes
- Validate all state updates
- Encrypt sensitive state data
- Implement proper access controls for global state
- Sanitize state data before persistence

## Best Practices
- Keep state normalized and flat
- Use descriptive state names
- Implement proper error handling for state operations
- Monitor state changes for debugging

## Related Topics
- [@workflow Operator](./69-workflow-operator.md) - Business process automation
- [@event Operator](./66-event-operator.md) - Event-driven automation
- [@cache Operator](./46-cache-operator.md) - Caching strategies
- [Data Structures](./05-data-structures.md) - Data manipulation 