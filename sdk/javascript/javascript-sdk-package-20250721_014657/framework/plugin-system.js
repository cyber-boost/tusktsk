/**
 * TuskLang Plugin System
 * Provides extensibility and plugin management for the JavaScript SDK
 */

const fs = require('fs').promises;
const path = require('path');

class Plugin {
  constructor(name, version, description = '') {
    this.name = name;
    this.version = version;
    this.description = description;
    this.enabled = false;
    this.hooks = new Map();
    this.config = {};
    this.metadata = {};
  }

  /**
   * Register a hook
   */
  registerHook(event, callback, priority = 100) {
    if (!this.hooks.has(event)) {
      this.hooks.set(event, []);
    }
    
    this.hooks.get(event).push({
      callback,
      priority,
      plugin: this.name
    });

    // Sort by priority (lower numbers = higher priority)
    this.hooks.get(event).sort((a, b) => a.priority - b.priority);
  }

  /**
   * Execute a hook
   */
  async executeHook(event, data, context = {}) {
    const hooks = this.hooks.get(event) || [];
    let result = data;

    for (const hook of hooks) {
      try {
        result = await hook.callback(result, context);
      } catch (error) {
        console.error(`Error executing hook ${event} in plugin ${this.name}:`, error);
      }
    }

    return result;
  }

  /**
   * Enable the plugin
   */
  enable() {
    this.enabled = true;
  }

  /**
   * Disable the plugin
   */
  disable() {
    this.enabled = false;
  }

  /**
   * Set plugin configuration
   */
  setConfig(config) {
    this.config = { ...this.config, ...config };
  }

  /**
   * Get plugin configuration
   */
  getConfig(key = null) {
    if (key === null) {
      return this.config;
    }
    return this.config[key];
  }

  /**
   * Set plugin metadata
   */
  setMetadata(metadata) {
    this.metadata = { ...this.metadata, ...metadata };
  }

  /**
   * Get plugin metadata
   */
  getMetadata(key = null) {
    if (key === null) {
      return this.metadata;
    }
    return this.metadata[key];
  }
}

class PluginManager {
  constructor(options = {}) {
    this.plugins = new Map();
    this.hooks = new Map();
    this.pluginDir = options.pluginDir || './plugins';
    this.autoLoad = options.autoLoad !== false;
    this.hookRegistry = new Map();
    this.eventEmitter = new Map();
    this.pluginConfigs = new Map();
  }

  /**
   * Register a plugin
   */
  registerPlugin(plugin) {
    if (this.plugins.has(plugin.name)) {
      throw new Error(`Plugin ${plugin.name} is already registered`);
    }

    this.plugins.set(plugin.name, plugin);
    
    // Register all hooks from the plugin
    for (const [event, hooks] of plugin.hooks) {
      this.registerHook(event, hooks);
    }

    return plugin;
  }

  /**
   * Unregister a plugin
   */
  unregisterPlugin(pluginName) {
    const plugin = this.plugins.get(pluginName);
    if (!plugin) {
      throw new Error(`Plugin ${pluginName} is not registered`);
    }

    // Remove all hooks from this plugin
    for (const [event, hooks] of this.hooks) {
      this.hooks.set(event, hooks.filter(hook => hook.plugin !== pluginName));
    }

    this.plugins.delete(pluginName);
    return plugin;
  }

  /**
   * Register a hook
   */
  registerHook(event, hooks) {
    if (!this.hooks.has(event)) {
      this.hooks.set(event, []);
    }

    this.hooks.get(event).push(...hooks);
    this.hooks.get(event).sort((a, b) => a.priority - b.priority);
  }

  /**
   * Execute hooks for an event
   */
  async executeHooks(event, data, context = {}) {
    const hooks = this.hooks.get(event) || [];
    let result = data;

    for (const hook of hooks) {
      try {
        // Check if the plugin is enabled
        const plugin = this.plugins.get(hook.plugin);
        if (plugin && plugin.enabled) {
          result = await hook.callback(result, context);
        }
      } catch (error) {
        console.error(`Error executing hook ${event} in plugin ${hook.plugin}:`, error);
      }
    }

    return result;
  }

  /**
   * Enable a plugin
   */
  enablePlugin(pluginName) {
    const plugin = this.plugins.get(pluginName);
    if (!plugin) {
      throw new Error(`Plugin ${pluginName} is not registered`);
    }

    plugin.enable();
    return plugin;
  }

  /**
   * Disable a plugin
   */
  disablePlugin(pluginName) {
    const plugin = this.plugins.get(pluginName);
    if (!plugin) {
      throw new Error(`Plugin ${pluginName} is not registered`);
    }

    plugin.disable();
    return plugin;
  }

  /**
   * Get a plugin
   */
  getPlugin(pluginName) {
    return this.plugins.get(pluginName);
  }

  /**
   * Get all plugins
   */
  getAllPlugins() {
    return Array.from(this.plugins.values());
  }

  /**
   * Get enabled plugins
   */
  getEnabledPlugins() {
    return Array.from(this.plugins.values()).filter(plugin => plugin.enabled);
  }

  /**
   * Load plugins from directory
   */
  async loadPluginsFromDirectory(directory = this.pluginDir) {
    try {
      const files = await fs.readdir(directory);
      
      for (const file of files) {
        if (file.endsWith('.js')) {
          const pluginPath = path.join(directory, file);
          await this.loadPluginFromFile(pluginPath);
        }
      }
    } catch (error) {
      if (error.code !== 'ENOENT') {
        console.error('Error loading plugins from directory:', error);
      }
    }
  }

  /**
   * Load plugin from file
   */
  async loadPluginFromFile(filePath) {
    try {
      const pluginModule = require(filePath);
      
      if (typeof pluginModule === 'function') {
        const plugin = pluginModule(this);
        if (plugin instanceof Plugin) {
          this.registerPlugin(plugin);
          return plugin;
        }
      } else if (pluginModule.default && typeof pluginModule.default === 'function') {
        const plugin = pluginModule.default(this);
        if (plugin instanceof Plugin) {
          this.registerPlugin(plugin);
          return plugin;
        }
      }
    } catch (error) {
      console.error(`Error loading plugin from ${filePath}:`, error);
    }
  }

  /**
   * Create a plugin builder
   */
  createPlugin(name, version, description = '') {
    return new Plugin(name, version, description);
  }

  /**
   * Set plugin configuration
   */
  setPluginConfig(pluginName, config) {
    const plugin = this.plugins.get(pluginName);
    if (!plugin) {
      throw new Error(`Plugin ${pluginName} is not registered`);
    }

    plugin.setConfig(config);
    this.pluginConfigs.set(pluginName, config);
  }

  /**
   * Get plugin configuration
   */
  getPluginConfig(pluginName, key = null) {
    const plugin = this.plugins.get(pluginName);
    if (!plugin) {
      throw new Error(`Plugin ${pluginName} is not registered`);
    }

    return plugin.getConfig(key);
  }

  /**
   * Get plugin statistics
   */
  getPluginStats() {
    const stats = {
      total: this.plugins.size,
      enabled: this.getEnabledPlugins().length,
      disabled: this.plugins.size - this.getEnabledPlugins().length,
      hooks: this.hooks.size,
      plugins: []
    };

    for (const plugin of this.plugins.values()) {
      stats.plugins.push({
        name: plugin.name,
        version: plugin.version,
        enabled: plugin.enabled,
        hooks: plugin.hooks.size,
        description: plugin.description
      });
    }

    return stats;
  }

  /**
   * Validate plugin dependencies
   */
  validatePluginDependencies(plugin, dependencies = []) {
    for (const dep of dependencies) {
      if (!this.plugins.has(dep)) {
        throw new Error(`Plugin ${plugin.name} requires dependency ${dep} which is not loaded`);
      }
    }
  }

  /**
   * Get plugin dependency tree
   */
  getPluginDependencyTree() {
    const tree = {};
    
    for (const plugin of this.plugins.values()) {
      tree[plugin.name] = {
        dependencies: plugin.metadata.dependencies || [],
        dependents: []
      };
    }

    // Find dependents
    for (const [name, info] of Object.entries(tree)) {
      for (const dep of info.dependencies) {
        if (tree[dep]) {
          tree[dep].dependents.push(name);
        }
      }
    }

    return tree;
  }

  /**
   * Enable plugins in dependency order
   */
  async enablePluginsInOrder(pluginNames) {
    const tree = this.getPluginDependencyTree();
    const enabled = new Set();
    const toEnable = [...pluginNames];

    while (toEnable.length > 0) {
      const pluginName = toEnable.shift();
      
      if (enabled.has(pluginName)) {
        continue;
      }

      const plugin = this.plugins.get(pluginName);
      if (!plugin) {
        throw new Error(`Plugin ${pluginName} is not registered`);
      }

      // Check dependencies
      const dependencies = tree[pluginName]?.dependencies || [];
      const unmetDeps = dependencies.filter(dep => !enabled.has(dep));
      
      if (unmetDeps.length > 0) {
        // Add dependencies to the front of the queue
        toEnable.unshift(...unmetDeps);
        continue;
      }

      // Enable the plugin
      plugin.enable();
      enabled.add(pluginName);
    }

    return Array.from(enabled);
  }

  /**
   * Create a plugin manifest
   */
  createPluginManifest() {
    const manifest = {
      version: '1.0.0',
      plugins: [],
      hooks: Array.from(this.hooks.keys()),
      timestamp: new Date().toISOString()
    };

    for (const plugin of this.plugins.values()) {
      manifest.plugins.push({
        name: plugin.name,
        version: plugin.version,
        description: plugin.description,
        enabled: plugin.enabled,
        hooks: Array.from(plugin.hooks.keys()),
        config: plugin.config,
        metadata: plugin.metadata
      });
    }

    return manifest;
  }

  /**
   * Save plugin manifest to file
   */
  async savePluginManifest(filePath) {
    const manifest = this.createPluginManifest();
    await fs.writeFile(filePath, JSON.stringify(manifest, null, 2));
  }

  /**
   * Load plugin manifest from file
   */
  async loadPluginManifest(filePath) {
    try {
      const content = await fs.readFile(filePath, 'utf8');
      const manifest = JSON.parse(content);
      
      // Restore plugin states
      for (const pluginInfo of manifest.plugins) {
        const plugin = this.plugins.get(pluginInfo.name);
        if (plugin) {
          plugin.setConfig(pluginInfo.config);
          plugin.setMetadata(pluginInfo.metadata);
          if (pluginInfo.enabled) {
            plugin.enable();
          } else {
            plugin.disable();
          }
        }
      }

      return manifest;
    } catch (error) {
      console.error('Error loading plugin manifest:', error);
      return null;
    }
  }
}

// Built-in plugins
class BuiltInPlugins {
  /**
   * Logger plugin
   */
  static createLoggerPlugin(manager) {
    const plugin = manager.createPlugin('logger', '1.0.0', 'Built-in logging plugin');
    
    plugin.registerHook('parse.start', (data, context) => {
      console.log(`[TuskLang] Starting to parse: ${context.filename || 'unknown'}`);
      return data;
    });

    plugin.registerHook('parse.end', (data, context) => {
      console.log(`[TuskLang] Finished parsing: ${context.filename || 'unknown'}`);
      return data;
    });

    plugin.registerHook('operator.execute', (data, context) => {
      console.log(`[TuskLang] Executing operator: ${context.operator}`);
      return data;
    });

    return plugin;
  }

  /**
   * Metrics plugin
   */
  static createMetricsPlugin(manager) {
    const plugin = manager.createPlugin('metrics', '1.0.0', 'Built-in metrics plugin');
    const metrics = {
      parseCount: 0,
      operatorCount: 0,
      errorCount: 0,
      startTime: Date.now()
    };

    plugin.registerHook('parse.start', (data, context) => {
      metrics.parseCount++;
      return data;
    });

    plugin.registerHook('operator.execute', (data, context) => {
      metrics.operatorCount++;
      return data;
    });

    plugin.registerHook('error', (data, context) => {
      metrics.errorCount++;
      return data;
    });

    plugin.setMetadata({ metrics });
    return plugin;
  }

  /**
   * Validation plugin
   */
  static createValidationPlugin(manager) {
    const plugin = manager.createPlugin('validation', '1.0.0', 'Built-in validation plugin');
    
    plugin.registerHook('parse.before', (data, context) => {
      // Add validation logic here
      return data;
    });

    plugin.registerHook('parse.after', (data, context) => {
      // Add post-parse validation here
      return data;
    });

    return plugin;
  }
}

module.exports = {
  Plugin,
  PluginManager,
  BuiltInPlugins
}; 