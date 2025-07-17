/**
 * Redis Adapter for TuskLang
 * ==========================
 * Enables @query operations with Redis key-value store
 * 
 * DEFAULT CONFIG: peanut.tsk (the bridge of language grace)
 */

class RedisAdapter {
  constructor(options = {}) {
    this.config = {
      host: options.host || 'localhost',
      port: options.port || 6379,
      password: options.password || null,
      db: options.db || 0,
      username: options.username || null,
      family: options.family || 4,
      connectTimeout: options.connectTimeout || 10000,
      keepAlive: options.keepAlive ?? 30000,
      lazyConnect: options.lazyConnect ?? false,
      ...options
    };
    
    this.client = null;
    
    // Lazy load redis
    try {
      const redis = require('redis');
      this.redis = redis;
    } catch (error) {
      throw new Error('Redis adapter requires redis. Install it with: npm install redis');
    }
  }

  /**
   * Connect to Redis
   */
  async connect() {
    if (!this.client) {
      this.client = this.redis.createClient({
        socket: {
          host: this.config.host,
          port: this.config.port,
          family: this.config.family,
          connectTimeout: this.config.connectTimeout,
          keepAlive: this.config.keepAlive
        },
        username: this.config.username,
        password: this.config.password,
        database: this.config.db,
        lazyConnect: this.config.lazyConnect
      });
      
      this.client.on('error', (err) => {
        console.error('Redis Client Error', err);
      });
      
      await this.client.connect();
    }
    return this.client;
  }

  /**
   * Execute a Redis query
   * Redis uses a special query syntax for TuskLang:
   * @query("command", ...args)
   * 
   * Examples:
   * @query("get", "user:count")
   * @query("hget", "settings", "max_users")
   * @query("smembers", "features:enabled")
   * @query("zrange", "leaderboard", 0, 10)
   */
  async query(command, ...args) {
    try {
      await this.connect();
      
      const cmd = command.toLowerCase();
      
      // Handle special TuskLang commands
      switch (cmd) {
        // Count operations
        case 'count':
          if (args[0]) {
            const keys = await this.client.keys(args[0]);
            return keys.length;
          }
          return 0;
          
        // Sum numeric values
        case 'sum':
          const sumKeys = await this.client.keys(args[0] || '*');
          let sum = 0;
          for (const key of sumKeys) {
            const val = await this.client.get(key);
            const num = parseFloat(val);
            if (!isNaN(num)) sum += num;
          }
          return sum;
          
        // Get all matching keys and values
        case 'getall':
          const pattern = args[0] || '*';
          const allKeys = await this.client.keys(pattern);
          const result = {};
          for (const key of allKeys) {
            result[key] = await this.client.get(key);
          }
          return result;
          
        // Check if key exists
        case 'exists':
          const exists = await this.client.exists(args[0]);
          return exists === 1;
          
        // Standard Redis commands
        default:
          // Direct command execution
          if (typeof this.client[cmd] === 'function') {
            const result = await this.client[cmd](...args);
            
            // Convert Redis responses to appropriate types
            if (result === null) return null;
            if (result === 'true') return true;
            if (result === 'false') return false;
            if (!isNaN(result) && typeof result === 'string') {
              return parseFloat(result);
            }
            
            return result;
          }
          
          throw new Error(`Unknown Redis command: ${command}`);
      }
      
    } catch (error) {
      console.error('Redis query error:', error);
      throw error;
    }
  }

  /**
   * Multi/Pipeline support
   */
  multi() {
    return this.client.multi();
  }

  /**
   * Execute multi/pipeline
   */
  async exec(multi) {
    return await multi.exec();
  }

  /**
   * Close the connection
   */
  async close() {
    if (this.client) {
      await this.client.quit();
      this.client = null;
    }
  }

  /**
   * Create test data
   */
  async createTestData() {
    try {
      await this.connect();
      
      // Clear existing test data
      const testKeys = await this.client.keys('test:*');
      if (testKeys.length > 0) {
        await this.client.del(testKeys);
      }
      
      // Settings (strings)
      await this.client.set('settings:max_users', '1000');
      await this.client.set('settings:app_name', 'TuskLang Demo');
      await this.client.set('settings:maintenance_mode', 'false');
      
      // User counters
      await this.client.set('stats:user_count', '3');
      await this.client.set('stats:active_users', '2');
      
      // Feature flags (set)
      await this.client.sAdd('features:enabled', ['new_dashboard', 'dark_mode']);
      await this.client.sAdd('features:disabled', 'api_v2');
      
      // Plans (hash)
      await this.client.hSet('plan:basic', {
        rate_limit: '100',
        price: '9.99'
      });
      
      await this.client.hSet('plan:premium', {
        rate_limit: '1000', 
        price: '29.99'
      });
      
      await this.client.hSet('plan:enterprise', {
        rate_limit: '10000',
        price: '99.99'
      });
      
      // Users (hash)
      await this.client.hSet('user:1', {
        name: 'John Doe',
        email: 'john@example.com',
        active: 'true'
      });
      
      await this.client.hSet('user:2', {
        name: 'Jane Smith',
        email: 'jane@example.com',
        active: 'true'
      });
      
      await this.client.hSet('user:3', {
        name: 'Bob Wilson',
        email: 'bob@example.com',
        active: 'false'
      });
      
      // Leaderboard (sorted set)
      await this.client.zAdd('leaderboard', [
        { score: 100, value: 'John' },
        { score: 85, value: 'Jane' },
        { score: 92, value: 'Bob' }
      ]);
      
      // Lists
      await this.client.lPush('queue:tasks', ['task3', 'task2', 'task1']);
      await this.client.lPush('recent:activities', ['login', 'view_dashboard', 'update_profile']);
      
      // Expiring keys
      await this.client.set('cache:homepage', 'cached_content', {
        EX: 300 // 5 minutes
      });
      
      console.log('Redis test data created successfully');
      
    } catch (error) {
      console.error('Error creating test data:', error);
      throw error;
    }
  }

  /**
   * Check if connected
   */
  async isConnected() {
    try {
      await this.connect();
      const pong = await this.client.ping();
      return pong === 'PONG';
    } catch {
      return false;
    }
  }

  /**
   * TuskLang specific helpers
   */
  async getSettings() {
    const keys = await this.client.keys('settings:*');
    const settings = {};
    
    for (const key of keys) {
      const name = key.replace('settings:', '');
      settings[name] = await this.client.get(key);
    }
    
    return settings;
  }

  async getFeatures() {
    const enabled = await this.client.sMembers('features:enabled');
    const disabled = await this.client.sMembers('features:disabled');
    
    return {
      enabled,
      disabled,
      all: [...enabled, ...disabled]
    };
  }

  /**
   * Load configuration from peanut.tsk
   */
  static async loadFromPeanut() {
    const fs = require('fs').promises;
    const TuskLang = require('../index.js');
    
    try {
      // Look for peanut.tsk in standard locations
      const locations = [
        './peanut.tsk',
        '../peanut.tsk',
        '../../peanut.tsk',
        '/etc/tusklang/peanut.tsk',
        process.env.TUSKLANG_CONFIG || ''
      ].filter(Boolean);
      
      for (const location of locations) {
        try {
          const content = await fs.readFile(location, 'utf8');
          const tusk = new TuskLang();
          const config = tusk.parse(content);
          
          if (config.database?.redis || config.cache?.redis) {
            return new RedisAdapter(config.database?.redis || config.cache?.redis);
          }
        } catch (e) {
          // Continue to next location
        }
      }
      
      throw new Error('No peanut.tsk found with Redis configuration');
      
    } catch (error) {
      throw new Error(`Failed to load Redis config from peanut.tsk: ${error.message}`);
    }
  }
}

module.exports = RedisAdapter;