# @dependency Operator - Dependency Management

## Overview
The `@dependency` operator in TuskLang provides comprehensive dependency management capabilities, enabling you to declare, resolve, and manage dependencies between modules, services, and components with version control and conflict resolution.

## TuskLang Syntax

### Basic Dependency
```tusk
# Simple dependency declaration
app_dependencies: @dependency("app", {
  database: "2.0.0",
  cache: "1.5.0",
  auth: "3.1.0"
})

# Dependency with constraints
constrained_dependencies: @dependency("api", {
  express: ">=4.0.0 <5.0.0",
  mongoose: "^6.0.0",
  redis: "~4.0.0"
})
```

### Dependency with Options
```tusk
# Dependency with specific options
optional_dependencies: @dependency("service", {
  required: {
    database: "2.0.0",
    logger: "1.0.0"
  },
  optional: {
    cache: "1.5.0",
    monitoring: "2.0.0"
  },
  peer: {
    react: ">=16.0.0"
  }
})
```

### Dependency Resolution
```tusk
# Dependency resolution configuration
dependency_resolution: @dependency.resolve({
  registry: "https://registry.npmjs.org",
  cache: true,
  offline: false,
  resolution_strategy: "highest"
})
```

## JavaScript Integration

### Node.js Dependency Management
```javascript
const tusklang = require('@tusklang/core');

const config = tusklang.parse(`
dependency_config: {
  app: @dependency("app", {
    express: "4.18.0",
    mongoose: "^6.0.0",
    redis: "~4.0.0",
    jwt: ">=8.0.0"
  }),
  
  api: @dependency("api", {
    required: {
      express: ">=4.0.0",
      cors: "2.8.5"
    },
    optional: {
      compression: "1.7.4",
      helmet: "5.0.0"
    }
  })
}
`);

class DependencyManager {
  constructor(config) {
    this.config = config.dependency_config;
    this.dependencies = new Map();
    this.resolvedDependencies = new Map();
    this.dependencyGraph = new Map();
    this.initializeDependencies();
  }

  initializeDependencies() {
    Object.entries(this.config).forEach(([name, depConfig]) => {
      this.createDependencySet(name, depConfig);
    });
  }

  createDependencySet(name, config) {
    const dependencySet = {
      name: name,
      required: config.required || {},
      optional: config.optional || {},
      peer: config.peer || {},
      resolved: new Map(),
      conflicts: [],
      missing: []
    };

    this.dependencies.set(name, dependencySet);
    this.buildDependencyGraph(name, dependencySet);
  }

  buildDependencyGraph(name, dependencySet) {
    const graph = new Map();
    
    // Add required dependencies
    Object.entries(dependencySet.required).forEach(([dep, version]) => {
      graph.set(dep, { version, type: 'required' });
    });

    // Add optional dependencies
    Object.entries(dependencySet.optional).forEach(([dep, version]) => {
      graph.set(dep, { version, type: 'optional' });
    });

    // Add peer dependencies
    Object.entries(dependencySet.peer).forEach(([dep, version]) => {
      graph.set(dep, { version, type: 'peer' });
    });

    this.dependencyGraph.set(name, graph);
  }

  async resolveDependencies(name) {
    const dependencySet = this.dependencies.get(name);
    if (!dependencySet) {
      throw new Error(`Dependency set '${name}' not found`);
    }

    const graph = this.dependencyGraph.get(name);
    const resolved = new Map();

    // Resolve required dependencies
    for (const [dep, info] of graph.entries()) {
      if (info.type === 'required') {
        try {
          const resolvedVersion = await this.resolveDependency(dep, info.version);
          resolved.set(dep, { version: resolvedVersion, type: 'required' });
        } catch (error) {
          dependencySet.missing.push({ dep, version: info.version, error: error.message });
        }
      }
    }

    // Resolve optional dependencies
    for (const [dep, info] of graph.entries()) {
      if (info.type === 'optional') {
        try {
          const resolvedVersion = await this.resolveDependency(dep, info.version);
          resolved.set(dep, { version: resolvedVersion, type: 'optional' });
        } catch (error) {
          console.warn(`Optional dependency '${dep}' could not be resolved:`, error.message);
        }
      }
    }

    // Check peer dependencies
    for (const [dep, info] of graph.entries()) {
      if (info.type === 'peer') {
        const peerVersion = await this.checkPeerDependency(dep, info.version);
        if (peerVersion) {
          resolved.set(dep, { version: peerVersion, type: 'peer' });
        } else {
          console.warn(`Peer dependency '${dep}' not found`);
        }
      }
    }

    dependencySet.resolved = resolved;
    this.resolvedDependencies.set(name, resolved);

    return resolved;
  }

  async resolveDependency(name, versionConstraint) {
    // Simulate dependency resolution
    const availableVersions = await this.getAvailableVersions(name);
    const resolvedVersion = this.satisfyVersionConstraint(availableVersions, versionConstraint);
    
    if (!resolvedVersion) {
      throw new Error(`No version of '${name}' satisfies constraint '${versionConstraint}'`);
    }

    return resolvedVersion;
  }

  async getAvailableVersions(name) {
    // Simulate fetching available versions from registry
    const mockVersions = {
      'express': ['4.17.1', '4.18.0', '4.18.1', '4.18.2'],
      'mongoose': ['6.0.0', '6.1.0', '6.2.0', '6.3.0'],
      'redis': ['4.0.0', '4.1.0', '4.2.0', '4.3.0'],
      'jwt': ['8.0.0', '8.1.0', '8.2.0', '8.3.0'],
      'cors': ['2.8.5', '2.8.6'],
      'compression': ['1.7.4'],
      'helmet': ['5.0.0', '5.1.0']
    };

    return mockVersions[name] || [];
  }

  satisfyVersionConstraint(versions, constraint) {
    // Simple version constraint satisfaction
    if (constraint.startsWith('^')) {
      const majorVersion = constraint.slice(1);
      return versions.find(v => v.startsWith(majorVersion + '.'));
    } else if (constraint.startsWith('~')) {
      const minorVersion = constraint.slice(1);
      return versions.find(v => v.startsWith(minorVersion));
    } else if (constraint.startsWith('>=')) {
      const minVersion = constraint.slice(2);
      return versions.find(v => this.compareVersions(v, minVersion) >= 0);
    } else if (constraint.startsWith('<')) {
      const maxVersion = constraint.slice(1);
      return versions.find(v => this.compareVersions(v, maxVersion) < 0);
    } else {
      return versions.find(v => v === constraint);
    }
  }

  compareVersions(v1, v2) {
    const parts1 = v1.split('.').map(Number);
    const parts2 = v2.split('.').map(Number);
    
    for (let i = 0; i < Math.max(parts1.length, parts2.length); i++) {
      const part1 = parts1[i] || 0;
      const part2 = parts2[i] || 0;
      
      if (part1 > part2) return 1;
      if (part1 < part2) return -1;
    }
    
    return 0;
  }

  async checkPeerDependency(name, versionConstraint) {
    // Check if peer dependency is already installed
    try {
      const peerModule = require(name);
      const peerVersion = peerModule.version || 'unknown';
      
      if (this.satisfyVersionConstraint([peerVersion], versionConstraint)) {
        return peerVersion;
      }
    } catch (error) {
      // Peer dependency not found
    }
    
    return null;
  }

  // Dependency installation
  async installDependencies(name) {
    const resolved = this.resolvedDependencies.get(name);
    if (!resolved) {
      throw new Error(`Dependencies for '${name}' not resolved`);
    }

    const installPromises = [];
    
    for (const [dep, info] of resolved.entries()) {
      if (info.type === 'required' || info.type === 'optional') {
        installPromises.push(this.installDependency(dep, info.version));
      }
    }

    await Promise.all(installPromises);
    console.log(`Dependencies for '${name}' installed successfully`);
  }

  async installDependency(name, version) {
    // Simulate dependency installation
    console.log(`Installing ${name}@${version}...`);
    await new Promise(resolve => setTimeout(resolve, 100));
    console.log(`${name}@${version} installed successfully`);
  }

  // Dependency validation
  validateDependencies(name) {
    const dependencySet = this.dependencies.get(name);
    if (!dependencySet) {
      throw new Error(`Dependency set '${name}' not found`);
    }

    const issues = [];

    // Check for missing required dependencies
    if (dependencySet.missing.length > 0) {
      issues.push({
        type: 'missing',
        dependencies: dependencySet.missing
      });
    }

    // Check for conflicts
    if (dependencySet.conflicts.length > 0) {
      issues.push({
        type: 'conflicts',
        dependencies: dependencySet.conflicts
      });
    }

    return {
      valid: issues.length === 0,
      issues: issues
    };
  }

  // Dependency tree
  getDependencyTree(name) {
    const graph = this.dependencyGraph.get(name);
    if (!graph) {
      return null;
    }

    const tree = {
      name: name,
      dependencies: {}
    };

    for (const [dep, info] of graph.entries()) {
      tree.dependencies[dep] = {
        version: info.version,
        type: info.type,
        resolved: this.resolvedDependencies.get(name)?.get(dep)?.version || null
      };
    }

    return tree;
  }

  // Update dependencies
  async updateDependencies(name, updates) {
    const dependencySet = this.dependencies.get(name);
    if (!dependencySet) {
      throw new Error(`Dependency set '${name}' not found`);
    }

    // Update version constraints
    Object.entries(updates).forEach(([dep, version]) => {
      if (dependencySet.required[dep]) {
        dependencySet.required[dep] = version;
      } else if (dependencySet.optional[dep]) {
        dependencySet.optional[dep] = version;
      }
    });

    // Rebuild dependency graph
    this.buildDependencyGraph(name, dependencySet);

    // Re-resolve dependencies
    await this.resolveDependencies(name);
  }
}

// Usage
const dependencyManager = new DependencyManager(config);

// Resolve and install dependencies
async function setupDependencies() {
  try {
    // Resolve app dependencies
    const appDeps = await dependencyManager.resolveDependencies('app');
    console.log('App dependencies resolved:', appDeps);

    // Validate dependencies
    const validation = dependencyManager.validateDependencies('app');
    console.log('Dependency validation:', validation);

    if (validation.valid) {
      // Install dependencies
      await dependencyManager.installDependencies('app');
    } else {
      console.error('Dependency validation failed:', validation.issues);
    }

    // Get dependency tree
    const tree = dependencyManager.getDependencyTree('app');
    console.log('Dependency tree:', tree);

  } catch (error) {
    console.error('Dependency setup error:', error);
  }
}

setupDependencies();
```

### Browser Dependency Management
```javascript
// Browser-based dependency management
const browserConfig = tusklang.parse(`
browser_dependencies: {
  app: @dependency("app", {
    required: {
      lodash: "4.17.21",
      axios: "^0.27.0"
    },
    optional: {
      moment: "2.29.0",
      chartjs: "3.0.0"
    }
  }),
  
  components: @dependency("components", {
    required: {
      react: ">=16.0.0",
      reactdom: ">=16.0.0"
    },
    peer: {
      react: ">=16.0.0"
    }
  })
}
`);

class BrowserDependencyManager {
  constructor(config) {
    this.config = config.browser_dependencies;
    this.dependencies = new Map();
    this.loadedDependencies = new Map();
    this.initializeDependencies();
  }

  initializeDependencies() {
    Object.entries(this.config).forEach(([name, depConfig]) => {
      this.createDependencySet(name, depConfig);
    });
  }

  createDependencySet(name, config) {
    const dependencySet = {
      name: name,
      required: config.required || {},
      optional: config.optional || {},
      peer: config.peer || {},
      loaded: new Map(),
      missing: []
    };

    this.dependencies.set(name, dependencySet);
  }

  async loadDependencies(name) {
    const dependencySet = this.dependencies.get(name);
    if (!dependencySet) {
      throw new Error(`Dependency set '${name}' not found`);
    }

    const loadPromises = [];

    // Load required dependencies
    for (const [dep, version] of Object.entries(dependencySet.required)) {
      loadPromises.push(this.loadDependency(dep, version, 'required'));
    }

    // Load optional dependencies
    for (const [dep, version] of Object.entries(dependencySet.optional)) {
      loadPromises.push(this.loadDependency(dep, version, 'optional'));
    }

    await Promise.all(loadPromises);

    return dependencySet.loaded;
  }

  async loadDependency(name, version, type) {
    try {
      // Simulate loading dependency from CDN
      const module = await this.loadFromCDN(name, version);
      this.loadedDependencies.set(name, { module, version, type });
      
      const dependencySet = this.dependencies.get(name);
      if (dependencySet) {
        dependencySet.loaded.set(name, { module, version, type });
      }

      console.log(`Loaded ${name}@${version} (${type})`);
      return module;
    } catch (error) {
      if (type === 'required') {
        throw new Error(`Failed to load required dependency ${name}: ${error.message}`);
      } else {
        console.warn(`Failed to load optional dependency ${name}: ${error.message}`);
      }
    }
  }

  async loadFromCDN(name, version) {
    // Simulate loading from CDN
    const cdnUrls = {
      'lodash': `https://cdn.jsdelivr.net/npm/lodash@${version}/lodash.min.js`,
      'axios': `https://cdn.jsdelivr.net/npm/axios@${version}/dist/axios.min.js`,
      'moment': `https://cdn.jsdelivr.net/npm/moment@${version}/moment.min.js`,
      'chartjs': `https://cdn.jsdelivr.net/npm/chart.js@${version}/dist/chart.min.js`
    };

    const url = cdnUrls[name];
    if (!url) {
      throw new Error(`No CDN URL found for ${name}`);
    }

    // Simulate script loading
    return new Promise((resolve, reject) => {
      const script = document.createElement('script');
      script.src = url;
      script.onload = () => {
        // Return the global variable that the script creates
        const globalVar = this.getGlobalVariable(name);
        resolve(globalVar);
      };
      script.onerror = () => reject(new Error(`Failed to load ${name} from CDN`));
      document.head.appendChild(script);
    });
  }

  getGlobalVariable(name) {
    // Map module names to their global variables
    const globalMap = {
      'lodash': window._,
      'axios': window.axios,
      'moment': window.moment,
      'chartjs': window.Chart
    };

    return globalMap[name];
  }

  // Check if dependency is available
  isDependencyAvailable(name) {
    return this.loadedDependencies.has(name);
  }

  // Get loaded dependency
  getDependency(name) {
    return this.loadedDependencies.get(name);
  }

  // Validate peer dependencies
  validatePeerDependencies(name) {
    const dependencySet = this.dependencies.get(name);
    if (!dependencySet) {
      return { valid: false, issues: ['Dependency set not found'] };
    }

    const issues = [];

    for (const [dep, version] of Object.entries(dependencySet.peer)) {
      const loaded = this.loadedDependencies.get(dep);
      if (!loaded) {
        issues.push(`Peer dependency ${dep} not found`);
      } else if (!this.satisfyVersionConstraint([loaded.version], version)) {
        issues.push(`Peer dependency ${dep} version ${loaded.version} does not satisfy constraint ${version}`);
      }
    }

    return {
      valid: issues.length === 0,
      issues: issues
    };
  }

  satisfyVersionConstraint(versions, constraint) {
    // Simple version constraint satisfaction
    if (constraint.startsWith('>=')) {
      const minVersion = constraint.slice(2);
      return versions.some(v => this.compareVersions(v, minVersion) >= 0);
    }
    return versions.includes(constraint);
  }

  compareVersions(v1, v2) {
    const parts1 = v1.split('.').map(Number);
    const parts2 = v2.split('.').map(Number);
    
    for (let i = 0; i < Math.max(parts1.length, parts2.length); i++) {
      const part1 = parts1[i] || 0;
      const part2 = parts2[i] || 0;
      
      if (part1 > part2) return 1;
      if (part1 < part2) return -1;
    }
    
    return 0;
  }
}

// Usage
const browserDependencyManager = new BrowserDependencyManager(browserConfig);

// Load dependencies
async function loadAppDependencies() {
  try {
    const appDeps = await browserDependencyManager.loadDependencies('app');
    console.log('App dependencies loaded:', appDeps);

    // Use loaded dependencies
    if (browserDependencyManager.isDependencyAvailable('lodash')) {
      const _ = browserDependencyManager.getDependency('lodash').module;
      console.log('Lodash available:', _.version);
    }

    if (browserDependencyManager.isDependencyAvailable('axios')) {
      const axios = browserDependencyManager.getDependency('axios').module;
      console.log('Axios available:', axios.VERSION);
    }

  } catch (error) {
    console.error('Failed to load dependencies:', error);
  }
}

loadAppDependencies();
```

## Advanced Usage Scenarios

### Monorepo Dependencies
```tusk
# Monorepo dependency management
monorepo_dependencies: @dependency("monorepo", {
  workspaces: {
    "packages/app": {
      dependencies: ["packages/shared", "packages/ui"]
    },
    "packages/api": {
      dependencies: ["packages/shared", "packages/database"]
    }
  }
})
```

### Plugin Dependencies
```tusk
# Plugin dependency system
plugin_dependencies: @dependency("plugin", {
  core: {
    required: ["plugin-api", "plugin-loader"]
  },
  plugins: {
    "auth-plugin": {
      dependencies: ["plugin-api", "database"]
    },
    "ui-plugin": {
      dependencies: ["plugin-api", "react"]
    }
  }
})
```

### Microservice Dependencies
```tusk
# Microservice dependencies
microservice_dependencies: @dependency("microservice", {
  services: {
    "user-service": {
      dependencies: ["database", "redis", "auth-service"]
    },
    "order-service": {
      dependencies: ["database", "user-service", "payment-service"]
    }
  }
})
```

## TypeScript Implementation

### Typed Dependency Manager
```typescript
interface DependencyConfig {
  required?: Record<string, string>;
  optional?: Record<string, string>;
  peer?: Record<string, string>;
}

interface DependencyInfo {
  version: string;
  type: 'required' | 'optional' | 'peer';
  resolved?: string;
}

interface DependencySet {
  name: string;
  required: Record<string, string>;
  optional: Record<string, string>;
  peer: Record<string, string>;
  resolved: Map<string, DependencyInfo>;
  missing: Array<{ dep: string; version: string; error: string }>;
}

class TypedDependencyManager {
  private dependencies: Map<string, DependencySet> = new Map();
  private resolvedDependencies: Map<string, Map<string, DependencyInfo>> = new Map();

  createDependencySet(name: string, config: DependencyConfig): void {
    const dependencySet: DependencySet = {
      name,
      required: config.required || {},
      optional: config.optional || {},
      peer: config.peer || {},
      resolved: new Map(),
      missing: []
    };

    this.dependencies.set(name, dependencySet);
  }

  async resolveDependencies(name: string): Promise<Map<string, DependencyInfo>> {
    const dependencySet = this.dependencies.get(name);
    if (!dependencySet) {
      throw new Error(`Dependency set '${name}' not found`);
    }

    const resolved = new Map<string, DependencyInfo>();

    // Resolve required dependencies
    for (const [dep, version] of Object.entries(dependencySet.required)) {
      try {
        const resolvedVersion = await this.resolveDependency(dep, version);
        resolved.set(dep, { version, type: 'required', resolved: resolvedVersion });
      } catch (error) {
        dependencySet.missing.push({ dep, version, error: error.message });
      }
    }

    // Resolve optional dependencies
    for (const [dep, version] of Object.entries(dependencySet.optional)) {
      try {
        const resolvedVersion = await this.resolveDependency(dep, version);
        resolved.set(dep, { version, type: 'optional', resolved: resolvedVersion });
      } catch (error) {
        console.warn(`Optional dependency '${dep}' could not be resolved:`, error.message);
      }
    }

    dependencySet.resolved = resolved;
    this.resolvedDependencies.set(name, resolved);

    return resolved;
  }

  private async resolveDependency(name: string, versionConstraint: string): Promise<string> {
    // Implementation for dependency resolution
    return '1.0.0';
  }

  getDependencySet(name: string): DependencySet | undefined {
    return this.dependencies.get(name);
  }

  validateDependencies(name: string): { valid: boolean; issues: any[] } {
    const dependencySet = this.dependencies.get(name);
    if (!dependencySet) {
      return { valid: false, issues: ['Dependency set not found'] };
    }

    const issues = dependencySet.missing.map(missing => ({
      type: 'missing',
      dependency: missing.dep,
      version: missing.version,
      error: missing.error
    }));

    return {
      valid: issues.length === 0,
      issues: issues
    };
  }
}
```

## Real-World Examples

### Package.json Dependencies
```javascript
// Package.json dependency management
const packageJson = {
  name: "my-app",
  version: "1.0.0",
  dependencies: {
    "express": "^4.18.0",
    "mongoose": "^6.0.0",
    "redis": "~4.0.0"
  },
  devDependencies: {
    "jest": "^27.0.0",
    "eslint": "^8.0.0"
  },
  peerDependencies: {
    "react": ">=16.0.0"
  }
};

const dependencyManager = new DependencyManager({
  app: {
    required: packageJson.dependencies,
    optional: packageJson.devDependencies,
    peer: packageJson.peerDependencies
  }
});

// Resolve and install
dependencyManager.resolveDependencies('app')
  .then(() => dependencyManager.installDependencies('app'))
  .then(() => console.log('Dependencies installed successfully'));
```

### React Component Dependencies
```javascript
// React component with dependencies
import React from 'react';

const ComponentWithDependencies = () => {
  const [dependencies, setDependencies] = useState({});

  useEffect(() => {
    const loadDeps = async () => {
      const browserDeps = new BrowserDependencyManager(browserConfig);
      const loaded = await browserDeps.loadDependencies('components');
      setDependencies(loaded);
    };

    loadDeps();
  }, []);

  if (!dependencies.size) {
    return <div>Loading dependencies...</div>;
  }

  return (
    <div>
      <h2>Dependencies Loaded:</h2>
      {Array.from(dependencies.entries()).map(([name, info]) => (
        <div key={name}>
          {name}: {info.version} ({info.type})
        </div>
      ))}
    </div>
  );
};
```

## Performance Considerations
- Cache resolved dependencies to avoid repeated resolution
- Use lazy loading for optional dependencies
- Implement dependency tree optimization
- Monitor dependency resolution time

## Security Notes
- Validate dependency sources and signatures
- Implement dependency scanning for vulnerabilities
- Use secure dependency registries
- Implement dependency isolation for untrusted packages

## Best Practices
- Pin critical dependency versions
- Use semantic versioning for dependencies
- Implement dependency lock files
- Regular dependency updates and security audits

## Related Topics
- [@module Operator](./74-module-operator.md) - Module system
- [@namespace Operator](./73-namespace-operator.md) - Namespace organization
- [Modules](./07-modules.md) - Module system basics
- [Package Management](./17-deployment.md) - Deployment and packaging 