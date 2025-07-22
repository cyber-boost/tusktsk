/**
 * TuskLang Advanced Configuration Management and Environment Handling
 * Provides comprehensive configuration management and environment handling
 */

const fs = require('fs').promises;
const path = require('path');

class ConfigurationManager {
  constructor(options = {}) {
    this.options = {
      configPath: options.configPath || './config',
      environment: options.environment || process.env.NODE_ENV || 'development',
      hotReload: options.hotReload !== false,
      encryption: options.encryption || false,
      ...options
    };
    
    this.configs = new Map();
    this.environments = new Map();
    this.secrets = new Map();
    this.watchers = new Map();
    this.eventEmitter = new (require('events'))();
    this.isInitialized = false;
  }

  /**
   * Initialize configuration manager
   */
  async initialize() {
    try {
      // Load environment configurations
      await this.loadEnvironmentConfigs();
      
      // Load secrets
      await this.loadSecrets();
      
      // Setup file watchers if hot reload is enabled
      if (this.options.hotReload) {
        await this.setupFileWatchers();
      }
      
      this.isInitialized = true;
      this.eventEmitter.emit('initialized');
      
      return true;
    } catch (error) {
      this.eventEmitter.emit('error', error);
      throw error;
    }
  }

  /**
   * Load configuration from file
   */
  async loadConfig(name, filePath) {
    try {
      const fullPath = path.resolve(filePath);
      const content = await fs.readFile(fullPath, 'utf8');
      
      let config;
      if (filePath.endsWith('.json')) {
        config = JSON.parse(content);
      } else if (filePath.endsWith('.yaml') || filePath.endsWith('.yml')) {
        // In real implementation, use yaml parser
        config = JSON.parse(content); // Simplified for demo
      } else {
        throw new Error(`Unsupported config file format: ${filePath}`);
      }

      // Apply environment overrides
      config = this.applyEnvironmentOverrides(config);
      
      // Decrypt if needed
      if (this.options.encryption) {
        config = await this.decryptConfig(config);
      }

      this.configs.set(name, {
        data: config,
        filePath: fullPath,
        lastModified: Date.now()
      });

      this.eventEmitter.emit('configLoaded', { name, config });
      return config;
    } catch (error) {
      this.eventEmitter.emit('error', error);
      throw error;
    }
  }

  /**
   * Get configuration value
   */
  getConfig(name, key = null, defaultValue = null) {
    const config = this.configs.get(name);
    if (!config) {
      return defaultValue;
    }

    if (key === null) {
      return config.data;
    }

    const keys = key.split('.');
    let value = config.data;

    for (const k of keys) {
      if (value && typeof value === 'object' && k in value) {
        value = value[k];
      } else {
        return defaultValue;
      }
    }

    return value;
  }

  /**
   * Set configuration value
   */
  setConfig(name, key, value) {
    const config = this.configs.get(name);
    if (!config) {
      throw new Error(`Configuration not found: ${name}`);
    }

    const keys = key.split('.');
    let current = config.data;

    for (let i = 0; i < keys.length - 1; i++) {
      const k = keys[i];
      if (!(k in current) || typeof current[k] !== 'object') {
        current[k] = {};
      }
      current = current[k];
    }

    current[keys[keys.length - 1]] = value;
    config.lastModified = Date.now();

    this.eventEmitter.emit('configChanged', { name, key, value });
    return value;
  }

  /**
   * Save configuration to file
   */
  async saveConfig(name) {
    const config = this.configs.get(name);
    if (!config) {
      throw new Error(`Configuration not found: ${name}`);
    }

    try {
      let dataToSave = config.data;
      
      // Encrypt if needed
      if (this.options.encryption) {
        dataToSave = await this.encryptConfig(dataToSave);
      }

      const content = JSON.stringify(dataToSave, null, 2);
      await fs.writeFile(config.filePath, content, 'utf8');

      this.eventEmitter.emit('configSaved', { name });
      return true;
    } catch (error) {
      this.eventEmitter.emit('error', error);
      throw error;
    }
  }

  /**
   * Load environment-specific configurations
   */
  async loadEnvironmentConfigs() {
    const envConfigs = {
      development: {
        debug: true,
        logLevel: 'debug',
        database: {
          host: 'localhost',
          port: 5432
        }
      },
      staging: {
        debug: false,
        logLevel: 'info',
        database: {
          host: 'staging-db.example.com',
          port: 5432
        }
      },
      production: {
        debug: false,
        logLevel: 'warn',
        database: {
          host: 'prod-db.example.com',
          port: 5432
        }
      }
    };

    this.environments.set('development', envConfigs.development);
    this.environments.set('staging', envConfigs.staging);
    this.environments.set('production', envConfigs.production);
  }

  /**
   * Apply environment overrides to configuration
   */
  applyEnvironmentOverrides(config) {
    const envConfig = this.environments.get(this.options.environment);
    if (!envConfig) {
      return config;
    }

    return this.mergeConfigs(config, envConfig);
  }

  /**
   * Merge configurations
   */
  mergeConfigs(base, override) {
    const result = { ...base };

    for (const [key, value] of Object.entries(override)) {
      if (value && typeof value === 'object' && !Array.isArray(value)) {
        result[key] = this.mergeConfigs(result[key] || {}, value);
      } else {
        result[key] = value;
      }
    }

    return result;
  }

  /**
   * Load secrets from environment or file
   */
  async loadSecrets() {
    // Load from environment variables
    for (const [key, value] of Object.entries(process.env)) {
      if (key.startsWith('SECRET_')) {
        const secretName = key.replace('SECRET_', '').toLowerCase();
        this.secrets.set(secretName, value);
      }
    }

    // Load from secrets file if exists
    try {
      const secretsPath = path.join(this.options.configPath, 'secrets.json');
      const content = await fs.readFile(secretsPath, 'utf8');
      const secrets = JSON.parse(content);

      for (const [key, value] of Object.entries(secrets)) {
        this.secrets.set(key, value);
      }
    } catch (error) {
      // Secrets file doesn't exist, which is fine
    }
  }

  /**
   * Get secret value
   */
  getSecret(name, defaultValue = null) {
    return this.secrets.get(name) || defaultValue;
  }

  /**
   * Set secret value
   */
  setSecret(name, value) {
    this.secrets.set(name, value);
    this.eventEmitter.emit('secretChanged', { name });
    return value;
  }

  /**
   * Save secrets to file
   */
  async saveSecrets() {
    try {
      const secretsPath = path.join(this.options.configPath, 'secrets.json');
      const secrets = Object.fromEntries(this.secrets);
      const content = JSON.stringify(secrets, null, 2);
      
      await fs.mkdir(path.dirname(secretsPath), { recursive: true });
      await fs.writeFile(secretsPath, content, 'utf8');

      this.eventEmitter.emit('secretsSaved');
      return true;
    } catch (error) {
      this.eventEmitter.emit('error', error);
      throw error;
    }
  }

  /**
   * Setup file watchers for hot reload
   */
  async setupFileWatchers() {
    // In a real implementation, use fs.watch or chokidar
    // For demo purposes, we'll simulate file watching
    console.log('File watchers setup for hot reload');
  }

  /**
   * Encrypt configuration
   */
  async encryptConfig(config) {
    // In real implementation, use proper encryption
    // For demo purposes, return as-is
    return config;
  }

  /**
   * Decrypt configuration
   */
  async decryptConfig(config) {
    // In real implementation, use proper decryption
    // For demo purposes, return as-is
    return config;
  }

  /**
   * Validate configuration
   */
  validateConfig(name, schema) {
    const config = this.configs.get(name);
    if (!config) {
      throw new Error(`Configuration not found: ${name}`);
    }

    const errors = [];
    
    for (const [key, rules] of Object.entries(schema)) {
      const value = this.getConfig(name, key);
      
      if (rules.required && value === undefined) {
        errors.push(`Required field missing: ${key}`);
      }
      
      if (value !== undefined && rules.type && typeof value !== rules.type) {
        errors.push(`Invalid type for ${key}: expected ${rules.type}, got ${typeof value}`);
      }
      
      if (value !== undefined && rules.enum && !rules.enum.includes(value)) {
        errors.push(`Invalid value for ${key}: ${value}, expected one of [${rules.enum.join(', ')}]`);
      }
    }

    if (errors.length > 0) {
      throw new Error(`Configuration validation failed: ${errors.join(', ')}`);
    }

    return true;
  }

  /**
   * Get configuration statistics
   */
  getStats() {
    return {
      isInitialized: this.isInitialized,
      configs: this.configs.size,
      environments: this.environments.size,
      secrets: this.secrets.size,
      currentEnvironment: this.options.environment,
      hotReload: this.options.hotReload
    };
  }

  /**
   * Export configuration
   */
  exportConfig(name, format = 'json') {
    const config = this.configs.get(name);
    if (!config) {
      throw new Error(`Configuration not found: ${name}`);
    }

    switch (format.toLowerCase()) {
      case 'json':
        return JSON.stringify(config.data, null, 2);
      case 'yaml':
        // In real implementation, use yaml serializer
        return JSON.stringify(config.data, null, 2);
      default:
        throw new Error(`Unsupported export format: ${format}`);
    }
  }

  /**
   * Import configuration
   */
  async importConfig(name, content, format = 'json') {
    let config;
    
    switch (format.toLowerCase()) {
      case 'json':
        config = JSON.parse(content);
        break;
      case 'yaml':
        // In real implementation, use yaml parser
        config = JSON.parse(content);
        break;
      default:
        throw new Error(`Unsupported import format: ${format}`);
    }

    this.configs.set(name, {
      data: config,
      filePath: null,
      lastModified: Date.now()
    });

    this.eventEmitter.emit('configImported', { name, config });
    return config;
  }
}

class EnvironmentManager {
  constructor() {
    this.environments = new Map();
    this.currentEnvironment = process.env.NODE_ENV || 'development';
  }

  /**
   * Register environment
   */
  registerEnvironment(name, config) {
    this.environments.set(name, {
      name,
      config,
      variables: new Map(),
      lastModified: Date.now()
    });
  }

  /**
   * Set environment variable
   */
  setEnvironmentVariable(envName, key, value) {
    const env = this.environments.get(envName);
    if (!env) {
      throw new Error(`Environment not found: ${envName}`);
    }

    env.variables.set(key, value);
    env.lastModified = Date.now();
    return value;
  }

  /**
   * Get environment variable
   */
  getEnvironmentVariable(envName, key, defaultValue = null) {
    const env = this.environments.get(envName);
    if (!env) {
      return defaultValue;
    }

    return env.variables.get(key) || defaultValue;
  }

  /**
   * Switch environment
   */
  switchEnvironment(name) {
    if (!this.environments.has(name)) {
      throw new Error(`Environment not found: ${name}`);
    }

    this.currentEnvironment = name;
    process.env.NODE_ENV = name;
    return name;
  }

  /**
   * Get current environment
   */
  getCurrentEnvironment() {
    return this.currentEnvironment;
  }

  /**
   * Get environment configuration
   */
  getEnvironmentConfig(name) {
    const env = this.environments.get(name);
    return env ? env.config : null;
  }
}

module.exports = {
  ConfigurationManager,
  EnvironmentManager
}; 