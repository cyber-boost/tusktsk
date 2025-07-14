# TuskLang JavaScript Documentation: Advanced Database Integration

## Overview

Advanced database integration in TuskLang enables sophisticated data operations, real-time synchronization, and complex query patterns with JavaScript/Node.js applications.

## TuskLang Syntax

```tsk
#database advanced
  connections:
    primary:
      type: postgres
      host: localhost
      database: main_db
      pool_size: 20
    read_replica:
      type: postgres
      host: replica.example.com
      database: main_db
      pool_size: 10
    cache:
      type: redis
      host: localhost
      port: 6379

  migrations:
    auto: true
    directory: migrations
    table: schema_migrations

  replication:
    strategy: master_slave
    sync_interval: 5s
    conflict_resolution: last_write_wins

  sharding:
    enabled: true
    strategy: hash
    shards:
      - host: shard1.example.com
      - host: shard2.example.com
      - host: shard3.example.com
```

## JavaScript Integration

### Advanced Database Manager

```javascript
// advanced-database-manager.js
const { Pool } = require('pg');
const Redis = require('ioredis');
const EventEmitter = require('events');

class AdvancedDatabaseManager extends EventEmitter {
  constructor(config) {
    super();
    this.config = config;
    this.connections = new Map();
    this.migrationManager = null;
    this.replicationManager = null;
    this.shardingManager = null;
    this.queryCache = new Map();
  }

  async initialize() {
    await this.setupConnections();
    await this.setupMigrationManager();
    await this.setupReplicationManager();
    await this.setupShardingManager();
    
    console.log('Advanced database manager initialized');
  }

  async setupConnections() {
    for (const [name, config] of Object.entries(this.config.connections)) {
      const connection = await this.createConnection(config);
      this.connections.set(name, connection);
    }
  }

  async createConnection(config) {
    switch (config.type) {
      case 'postgres':
        return new Pool({
          host: config.host,
          port: config.port || 5432,
          database: config.database,
          user: config.user,
          password: config.password,
          max: config.pool_size || 10,
          idleTimeoutMillis: 30000,
          connectionTimeoutMillis: 2000
        });
        
      case 'redis':
        return new Redis({
          host: config.host,
          port: config.port || 6379,
          password: config.password,
          db: config.db || 0,
          retryDelayOnFailover: 100,
          maxRetriesPerRequest: 3
        });
        
      default:
        throw new Error(`Unsupported database type: ${config.type}`);
    }
  }

  async setupMigrationManager() {
    if (this.config.migrations?.auto) {
      this.migrationManager = new MigrationManager(this.config.migrations);
      await this.migrationManager.initialize();
    }
  }

  async setupReplicationManager() {
    if (this.config.replication) {
      this.replicationManager = new ReplicationManager(
        this.connections,
        this.config.replication
      );
      await this.replicationManager.initialize();
    }
  }

  async setupShardingManager() {
    if (this.config.sharding?.enabled) {
      this.shardingManager = new ShardingManager(
        this.connections,
        this.config.sharding
      );
      await this.shardingManager.initialize();
    }
  }

  async query(sql, params = [], options = {}) {
    const connectionName = options.connection || 'primary';
    const connection = this.connections.get(connectionName);
    
    if (!connection) {
      throw new Error(`Connection '${connectionName}' not found`);
    }

    // Check cache if enabled
    if (options.cache && this.queryCache.has(sql)) {
      const cached = this.queryCache.get(sql);
      if (Date.now() - cached.timestamp < options.cache * 1000) {
        return cached.data;
      }
    }

    const start = Date.now();
    
    try {
      const result = await connection.query(sql, params);
      const duration = Date.now() - start;
      
      // Cache result if enabled
      if (options.cache) {
        this.queryCache.set(sql, {
          data: result.rows,
          timestamp: Date.now()
        });
      }
      
      // Emit query event
      this.emit('query', {
        sql,
        params,
        duration,
        rows: result.rowCount
      });
      
      return result.rows;
    } catch (error) {
      this.emit('error', { sql, params, error });
      throw error;
    }
  }

  async transaction(callback, options = {}) {
    const connectionName = options.connection || 'primary';
    const connection = this.connections.get(connectionName);
    
    if (!connection) {
      throw new Error(`Connection '${connectionName}' not found`);
    }

    const client = await connection.connect();
    
    try {
      await client.query('BEGIN');
      const result = await callback(client);
      await client.query('COMMIT');
      return result;
    } catch (error) {
      await client.query('ROLLBACK');
      throw error;
    } finally {
      client.release();
    }
  }

  async getConnection(name) {
    return this.connections.get(name);
  }

  async close() {
    for (const [name, connection] of this.connections.entries()) {
      if (connection.end) {
        await connection.end();
      } else if (connection.quit) {
        await connection.quit();
      }
    }
    
    this.connections.clear();
  }
}

module.exports = AdvancedDatabaseManager;
```

### Migration Manager

```javascript
// migration-manager.js
const fs = require('fs').promises;
const path = require('path');

class MigrationManager {
  constructor(config) {
    this.config = config;
    this.migrations = new Map();
    this.migrationTable = config.table || 'schema_migrations';
  }

  async initialize() {
    await this.createMigrationTable();
    await this.loadMigrations();
  }

  async createMigrationTable() {
    const sql = `
      CREATE TABLE IF NOT EXISTS ${this.migrationTable} (
        id SERIAL PRIMARY KEY,
        version VARCHAR(255) UNIQUE NOT NULL,
        name VARCHAR(255) NOT NULL,
        applied_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
      )
    `;
    
    // This would use the database connection
    console.log('Migration table created');
  }

  async loadMigrations() {
    const migrationsDir = this.config.directory || 'migrations';
    
    try {
      const files = await fs.readdir(migrationsDir);
      const migrationFiles = files.filter(file => file.endsWith('.sql'));
      
      for (const file of migrationFiles) {
        const version = file.split('_')[0];
        const name = file.replace('.sql', '').replace(`${version}_`, '');
        const content = await fs.readFile(path.join(migrationsDir, file), 'utf8');
        
        this.migrations.set(version, {
          version,
          name,
          content,
          file
        });
      }
    } catch (error) {
      console.warn('No migrations directory found');
    }
  }

  async getAppliedMigrations() {
    const sql = `SELECT version FROM ${this.migrationTable} ORDER BY version`;
    // This would execute the query
    return [];
  }

  async migrate(targetVersion = null) {
    const applied = await this.getAppliedMigrations();
    const versions = Array.from(this.migrations.keys()).sort();
    
    if (targetVersion) {
      const targetIndex = versions.indexOf(targetVersion);
      if (targetIndex === -1) {
        throw new Error(`Target version '${targetVersion}' not found`);
      }
      
      const currentIndex = versions.indexOf(applied[applied.length - 1] || '');
      
      if (targetIndex > currentIndex) {
        // Migrate up
        for (let i = currentIndex + 1; i <= targetIndex; i++) {
          await this.runMigration(versions[i], 'up');
        }
      } else if (targetIndex < currentIndex) {
        // Migrate down
        for (let i = currentIndex; i > targetIndex; i--) {
          await this.runMigration(versions[i], 'down');
        }
      }
    } else {
      // Migrate to latest
      for (const version of versions) {
        if (!applied.includes(version)) {
          await this.runMigration(version, 'up');
        }
      }
    }
  }

  async runMigration(version, direction) {
    const migration = this.migrations.get(version);
    if (!migration) {
      throw new Error(`Migration '${version}' not found`);
    }

    console.log(`Running migration ${version} ${direction}`);
    
    // This would execute the migration
    // For now, just log the action
    if (direction === 'up') {
      console.log(`Applying: ${migration.content}`);
    } else {
      console.log(`Rolling back: ${migration.content}`);
    }
  }

  async createMigration(name) {
    const timestamp = Date.now();
    const version = `${timestamp}_${name}`;
    const filename = `${version}.sql`;
    const migrationsDir = this.config.directory || 'migrations';
    
    const content = `-- Migration: ${name}
-- Created: ${new Date().toISOString()}

-- Add your migration SQL here
-- Example:
-- CREATE TABLE example (
--   id SERIAL PRIMARY KEY,
--   name VARCHAR(255) NOT NULL,
--   created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
-- );

-- Rollback:
-- DROP TABLE example;
`;

    await fs.writeFile(path.join(migrationsDir, filename), content);
    console.log(`Created migration: ${filename}`);
    
    return filename;
  }
}

module.exports = MigrationManager;
```

### Replication Manager

```javascript
// replication-manager.js
const EventEmitter = require('events');

class ReplicationManager extends EventEmitter {
  constructor(connections, config) {
    super();
    this.connections = connections;
    this.config = config;
    this.syncInterval = null;
    this.replicationLog = [];
  }

  async initialize() {
    if (this.config.strategy === 'master_slave') {
      await this.setupMasterSlaveReplication();
    }
    
    if (this.config.sync_interval) {
      this.startSyncInterval();
    }
  }

  async setupMasterSlaveReplication() {
    const master = this.connections.get('primary');
    const slave = this.connections.get('read_replica');
    
    if (!master || !slave) {
      throw new Error('Master and slave connections required for replication');
    }
    
    console.log('Master-slave replication configured');
  }

  startSyncInterval() {
    const interval = this.parseInterval(this.config.sync_interval);
    
    this.syncInterval = setInterval(async () => {
      await this.syncData();
    }, interval);
    
    console.log(`Replication sync started with ${this.config.sync_interval} interval`);
  }

  parseInterval(intervalStr) {
    const match = intervalStr.match(/^(\d+)([smhd])$/);
    if (!match) return 5000; // Default 5 seconds

    const [, value, unit] = match;
    const multipliers = {
      s: 1000,
      m: 60 * 1000,
      h: 60 * 60 * 1000,
      d: 24 * 60 * 60 * 1000
    };

    return parseInt(value) * (multipliers[unit] || 1000);
  }

  async syncData() {
    try {
      const master = this.connections.get('primary');
      const slave = this.connections.get('read_replica');
      
      // Get changes from master
      const changes = await this.getChangesFromMaster(master);
      
      // Apply changes to slave
      await this.applyChangesToSlave(slave, changes);
      
      this.replicationLog.push({
        timestamp: Date.now(),
        changes: changes.length,
        status: 'success'
      });
      
      this.emit('sync', { changes: changes.length, status: 'success' });
    } catch (error) {
      this.replicationLog.push({
        timestamp: Date.now(),
        error: error.message,
        status: 'error'
      });
      
      this.emit('error', error);
    }
  }

  async getChangesFromMaster(master) {
    // This would implement change data capture
    // For now, return empty array
    return [];
  }

  async applyChangesToSlave(slave, changes) {
    // This would apply changes to slave
    // For now, just log the action
    console.log(`Applying ${changes.length} changes to slave`);
  }

  async resolveConflict(conflict) {
    switch (this.config.conflict_resolution) {
      case 'last_write_wins':
        return conflict.changes.sort((a, b) => 
          new Date(b.timestamp) - new Date(a.timestamp)
        )[0];
        
      case 'master_wins':
        return conflict.master;
        
      case 'manual':
        this.emit('conflict', conflict);
        return null;
        
      default:
        throw new Error(`Unknown conflict resolution strategy: ${this.config.conflict_resolution}`);
    }
  }

  getReplicationStatus() {
    return {
      strategy: this.config.strategy,
      syncInterval: this.config.sync_interval,
      lastSync: this.replicationLog[this.replicationLog.length - 1],
      logCount: this.replicationLog.length
    };
  }

  stop() {
    if (this.syncInterval) {
      clearInterval(this.syncInterval);
      this.syncInterval = null;
    }
  }
}

module.exports = ReplicationManager;
```

### Sharding Manager

```javascript
// sharding-manager.js
const crypto = require('crypto');

class ShardingManager {
  constructor(connections, config) {
    this.connections = connections;
    this.config = config;
    this.shards = config.shards || [];
    this.strategy = config.strategy || 'hash';
  }

  async initialize() {
    await this.setupShards();
    console.log(`Sharding initialized with ${this.shards.length} shards`);
  }

  async setupShards() {
    for (let i = 0; i < this.shards.length; i++) {
      const shard = this.shards[i];
      const connection = await this.createShardConnection(shard);
      this.shards[i] = { ...shard, connection, index: i };
    }
  }

  async createShardConnection(shardConfig) {
    // This would create a connection to the shard
    // For now, return a mock connection
    return {
      query: async (sql, params) => {
        console.log(`Shard query: ${sql}`);
        return { rows: [] };
      }
    };
  }

  getShardForKey(key) {
    switch (this.strategy) {
      case 'hash':
        return this.getShardByHash(key);
        
      case 'range':
        return this.getShardByRange(key);
        
      case 'round_robin':
        return this.getShardByRoundRobin(key);
        
      default:
        throw new Error(`Unknown sharding strategy: ${this.strategy}`);
    }
  }

  getShardByHash(key) {
    const hash = crypto.createHash('md5').update(String(key)).digest('hex');
    const shardIndex = parseInt(hash.substring(0, 8), 16) % this.shards.length;
    return this.shards[shardIndex];
  }

  getShardByRange(key) {
    // This would implement range-based sharding
    // For now, use simple modulo
    const shardIndex = key % this.shards.length;
    return this.shards[shardIndex];
  }

  getShardByRoundRobin(key) {
    // This would implement round-robin sharding
    // For now, use simple modulo
    const shardIndex = key % this.shards.length;
    return this.shards[shardIndex];
  }

  async query(sql, params = [], shardKey = null) {
    if (shardKey) {
      // Query specific shard
      const shard = this.getShardForKey(shardKey);
      return await shard.connection.query(sql, params);
    } else {
      // Query all shards
      const results = await Promise.all(
        this.shards.map(shard => shard.connection.query(sql, params))
      );
      
      return results.reduce((acc, result) => {
        acc.rows.push(...result.rows);
        acc.rowCount += result.rowCount;
        return acc;
      }, { rows: [], rowCount: 0 });
    }
  }

  async insert(table, data, shardKey) {
    const shard = this.getShardForKey(shardKey);
    const columns = Object.keys(data);
    const values = Object.values(data);
    const placeholders = values.map((_, i) => `$${i + 1}`).join(', ');
    
    const sql = `INSERT INTO ${table} (${columns.join(', ')}) VALUES (${placeholders}) RETURNING *`;
    return await shard.connection.query(sql, values);
  }

  async getShardStatus() {
    const status = [];
    
    for (const shard of this.shards) {
      try {
        const result = await shard.connection.query('SELECT 1');
        status.push({
          index: shard.index,
          host: shard.host,
          status: 'healthy',
          responseTime: Date.now()
        });
      } catch (error) {
        status.push({
          index: shard.index,
          host: shard.host,
          status: 'unhealthy',
          error: error.message
        });
      }
    }
    
    return status;
  }
}

module.exports = ShardingManager;
```

## TypeScript Implementation

```typescript
// advanced-database.types.ts
export interface DatabaseConfig {
  connections: Record<string, ConnectionConfig>;
  migrations?: MigrationConfig;
  replication?: ReplicationConfig;
  sharding?: ShardingConfig;
}

export interface ConnectionConfig {
  type: 'postgres' | 'redis' | 'mysql' | 'mongodb';
  host: string;
  port?: number;
  database?: string;
  user?: string;
  password?: string;
  pool_size?: number;
  db?: number;
}

export interface MigrationConfig {
  auto?: boolean;
  directory?: string;
  table?: string;
}

export interface ReplicationConfig {
  strategy: 'master_slave' | 'multi_master';
  sync_interval?: string;
  conflict_resolution: 'last_write_wins' | 'master_wins' | 'manual';
}

export interface ShardingConfig {
  enabled: boolean;
  strategy: 'hash' | 'range' | 'round_robin';
  shards: Array<{ host: string; port?: number }>;
}

export interface QueryOptions {
  connection?: string;
  cache?: number;
  timeout?: number;
}

export interface DatabaseManager {
  query(sql: string, params?: any[], options?: QueryOptions): Promise<any[]>;
  transaction(callback: (client: any) => Promise<any>, options?: QueryOptions): Promise<any>;
  getConnection(name: string): Promise<any>;
  close(): Promise<void>;
}

// advanced-database.ts
import { DatabaseConfig, DatabaseManager, QueryOptions } from './advanced-database.types';

export class TypeScriptAdvancedDatabaseManager implements DatabaseManager {
  private config: DatabaseConfig;
  private connections: Map<string, any> = new Map();

  constructor(config: DatabaseConfig) {
    this.config = config;
  }

  async query(sql: string, params: any[] = [], options: QueryOptions = {}): Promise<any[]> {
    const connectionName = options.connection || 'primary';
    const connection = this.connections.get(connectionName);
    
    if (!connection) {
      throw new Error(`Connection '${connectionName}' not found`);
    }

    const result = await connection.query(sql, params);
    return result.rows;
  }

  async transaction(callback: (client: any) => Promise<any>, options: QueryOptions = {}): Promise<any> {
    const connectionName = options.connection || 'primary';
    const connection = this.connections.get(connectionName);
    
    if (!connection) {
      throw new Error(`Connection '${connectionName}' not found`);
    }

    const client = await connection.connect();
    
    try {
      await client.query('BEGIN');
      const result = await callback(client);
      await client.query('COMMIT');
      return result;
    } catch (error) {
      await client.query('ROLLBACK');
      throw error;
    } finally {
      client.release();
    }
  }

  async getConnection(name: string): Promise<any> {
    return this.connections.get(name);
  }

  async close(): Promise<void> {
    for (const connection of this.connections.values()) {
      if (connection.end) {
        await connection.end();
      } else if (connection.quit) {
        await connection.quit();
      }
    }
    
    this.connections.clear();
  }
}
```

## Advanced Usage Scenarios

### Multi-Database Operations

```javascript
// multi-database-operations.js
class MultiDatabaseOperations {
  constructor(databaseManager) {
    this.db = databaseManager;
  }

  async crossDatabaseQuery() {
    const results = await Promise.all([
      this.db.query('SELECT * FROM users', [], { connection: 'primary' }),
      this.db.query('SELECT * FROM analytics', [], { connection: 'read_replica' }),
      this.db.query('GET user_sessions', [], { connection: 'cache' })
    ]);

    return {
      users: results[0],
      analytics: results[1],
      sessions: results[2]
    };
  }

  async distributedTransaction() {
    return await this.db.transaction(async (client) => {
      // Update primary database
      await client.query('UPDATE users SET last_login = NOW() WHERE id = $1', [userId]);
      
      // Update analytics
      await client.query('INSERT INTO login_events (user_id, timestamp) VALUES ($1, NOW())', [userId]);
      
      // Update cache
      await client.query('SET user_session_${userId} ${sessionData}');
    });
  }
}
```

### Real-Time Data Synchronization

```javascript
// real-time-sync.js
class RealTimeDataSync {
  constructor(databaseManager) {
    this.db = databaseManager;
    this.syncQueue = [];
    this.isSyncing = false;
  }

  async syncData(data) {
    this.syncQueue.push(data);
    
    if (!this.isSyncing) {
      await this.processSyncQueue();
    }
  }

  async processSyncQueue() {
    this.isSyncing = true;
    
    while (this.syncQueue.length > 0) {
      const data = this.syncQueue.shift();
      await this.syncToAllDatabases(data);
    }
    
    this.isSyncing = false;
  }

  async syncToAllDatabases(data) {
    const connections = ['primary', 'read_replica', 'cache'];
    
    await Promise.all(connections.map(async (connection) => {
      try {
        await this.db.query(
          'INSERT INTO sync_data (data, timestamp) VALUES ($1, NOW())',
          [JSON.stringify(data)],
          { connection }
        );
      } catch (error) {
        console.error(`Sync failed for ${connection}:`, error);
      }
    }));
  }
}
```

## Real-World Examples

### E-commerce Database Integration

```javascript
// ecommerce-database.js
class EcommerceDatabase {
  constructor(databaseManager) {
    this.db = databaseManager;
  }

  async createOrder(userId, items) {
    return await this.db.transaction(async (client) => {
      // Create order
      const orderResult = await client.query(
        'INSERT INTO orders (user_id, total_amount, status) VALUES ($1, $2, $3) RETURNING id',
        [userId, this.calculateTotal(items), 'pending']
      );
      
      const orderId = orderResult.rows[0].id;
      
      // Add order items
      for (const item of items) {
        await client.query(
          'INSERT INTO order_items (order_id, product_id, quantity, price) VALUES ($1, $2, $3, $4)',
          [orderId, item.productId, item.quantity, item.price]
        );
        
        // Update inventory
        await client.query(
          'UPDATE products SET stock_quantity = stock_quantity - $1 WHERE id = $2',
          [item.quantity, item.productId]
        );
      }
      
      // Update user analytics
      await client.query(
        'INSERT INTO user_analytics (user_id, order_count, total_spent) VALUES ($1, 1, $2) ON CONFLICT (user_id) DO UPDATE SET order_count = user_analytics.order_count + 1, total_spent = user_analytics.total_spent + $2',
        [userId, this.calculateTotal(items)]
      );
      
      return orderId;
    });
  }

  async getProductWithInventory(productId) {
    const [product, inventory] = await Promise.all([
      this.db.query('SELECT * FROM products WHERE id = $1', [productId]),
      this.db.query('SELECT * FROM inventory WHERE product_id = $1', [productId], { connection: 'read_replica' })
    ]);
    
    return {
      ...product[0],
      inventory: inventory[0]
    };
  }

  calculateTotal(items) {
    return items.reduce((total, item) => total + (item.price * item.quantity), 0);
  }
}
```

### Analytics Database Integration

```javascript
// analytics-database.js
class AnalyticsDatabase {
  constructor(databaseManager) {
    this.db = databaseManager;
  }

  async trackEvent(eventType, userId, data) {
    const timestamp = new Date();
    
    // Store in primary database
    await this.db.query(
      'INSERT INTO events (event_type, user_id, data, timestamp) VALUES ($1, $2, $3, $4)',
      [eventType, userId, JSON.stringify(data), timestamp]
    );
    
    // Store in analytics database
    await this.db.query(
      'INSERT INTO analytics_events (event_type, user_id, data, timestamp) VALUES ($1, $2, $3, $4)',
      [eventType, userId, JSON.stringify(data), timestamp],
      { connection: 'analytics' }
    );
    
    // Cache recent events
    await this.db.query(
      'LPUSH recent_events ${JSON.stringify({ eventType, userId, data, timestamp })}',
      [],
      { connection: 'cache' }
    );
  }

  async getAnalytics(timeRange) {
    const [events, cache] = await Promise.all([
      this.db.query(
        'SELECT event_type, COUNT(*) as count FROM analytics_events WHERE timestamp >= $1 GROUP BY event_type',
        [timeRange.start],
        { connection: 'analytics' }
      ),
      this.db.query('LRANGE recent_events 0 99', [], { connection: 'cache' })
    ]);
    
    return {
      events: events,
      recentEvents: cache
    };
  }
}
```

## Performance Considerations

### Query Optimization

```javascript
// query-optimizer.js
class QueryOptimizer {
  constructor(databaseManager) {
    this.db = databaseManager;
    this.queryCache = new Map();
    this.slowQueries = [];
  }

  async optimizedQuery(sql, params = [], options = {}) {
    const cacheKey = this.generateCacheKey(sql, params);
    
    // Check cache
    if (options.cache && this.queryCache.has(cacheKey)) {
      const cached = this.queryCache.get(cacheKey);
      if (Date.now() - cached.timestamp < options.cache * 1000) {
        return cached.data;
      }
    }
    
    const start = Date.now();
    const result = await this.db.query(sql, params, options);
    const duration = Date.now() - start;
    
    // Cache result
    if (options.cache) {
      this.queryCache.set(cacheKey, {
        data: result,
        timestamp: Date.now()
      });
    }
    
    // Track slow queries
    if (duration > 1000) {
      this.slowQueries.push({
        sql,
        params,
        duration,
        timestamp: Date.now()
      });
    }
    
    return result;
  }

  generateCacheKey(sql, params) {
    return `${sql}:${JSON.stringify(params)}`;
  }

  getSlowQueries() {
    return this.slowQueries;
  }
}
```

### Connection Pooling

```javascript
// connection-pool-manager.js
class ConnectionPoolManager {
  constructor() {
    this.pools = new Map();
    this.stats = new Map();
  }

  async getConnection(poolName) {
    const pool = this.pools.get(poolName);
    if (!pool) {
      throw new Error(`Pool '${poolName}' not found`);
    }
    
    const stats = this.stats.get(poolName);
    stats.requests++;
    
    return pool.connect();
  }

  async releaseConnection(poolName, connection) {
    const pool = this.pools.get(poolName);
    if (pool) {
      connection.release();
    }
  }

  getPoolStats() {
    const stats = {};
    for (const [name, stat] of this.stats.entries()) {
      stats[name] = { ...stat };
    }
    return stats;
  }
}
```

## Security Notes

### Database Security

```javascript
// database-security.js
class DatabaseSecurity {
  constructor() {
    this.allowedQueries = new Set();
    this.blockedPatterns = [
      /DROP\s+TABLE/i,
      /DELETE\s+FROM/i,
      /TRUNCATE/i
    ];
  }

  validateQuery(sql) {
    // Check for dangerous patterns
    for (const pattern of this.blockedPatterns) {
      if (pattern.test(sql)) {
        throw new Error('Dangerous query detected');
      }
    }
    
    return true;
  }

  sanitizeInput(input) {
    if (typeof input === 'string') {
      return input.replace(/['";\\]/g, '');
    }
    return input;
  }

  encryptSensitiveData(data) {
    const crypto = require('crypto');
    const algorithm = 'aes-256-gcm';
    const key = crypto.randomBytes(32);
    const iv = crypto.randomBytes(16);
    
    const cipher = crypto.createCipher(algorithm, key);
    let encrypted = cipher.update(JSON.stringify(data), 'utf8', 'hex');
    encrypted += cipher.final('hex');
    
    return {
      encrypted,
      key: key.toString('hex'),
      iv: iv.toString('hex')
    };
  }
}
```

## Best Practices

### Database Configuration Management

```javascript
// database-config-manager.js
class DatabaseConfigManager {
  constructor() {
    this.configs = new Map();
  }

  setConfig(environment, config) {
    this.configs.set(environment, this.validateConfig(config));
  }

  getConfig(environment) {
    const config = this.configs.get(environment);
    if (!config) {
      throw new Error(`No configuration found for environment: ${environment}`);
    }
    return config;
  }

  validateConfig(config) {
    if (!config.connections || Object.keys(config.connections).length === 0) {
      throw new Error('At least one database connection is required');
    }
    
    return config;
  }

  getCurrentConfig() {
    const environment = process.env.NODE_ENV || 'development';
    return this.getConfig(environment);
  }
}
```

### Database Health Monitoring

```javascript
// database-health-monitor.js
class DatabaseHealthMonitor {
  constructor(databaseManager) {
    this.db = databaseManager;
    this.metrics = {
      queries: 0,
      errors: 0,
      avgResponseTime: 0
    };
  }

  async checkHealth() {
    try {
      const start = Date.now();
      await this.db.query('SELECT 1');
      const responseTime = Date.now() - start;
      
      this.metrics.queries++;
      this.metrics.avgResponseTime = 
        (this.metrics.avgResponseTime * (this.metrics.queries - 1) + responseTime) / this.metrics.queries;
      
      return {
        status: 'healthy',
        responseTime,
        metrics: this.metrics
      };
    } catch (error) {
      this.metrics.errors++;
      return {
        status: 'unhealthy',
        error: error.message,
        metrics: this.metrics
      };
    }
  }

  getMetrics() {
    return this.metrics;
  }
}
```

## Related Topics

- [@database Operator](./25-tsklang-javascript-operator-database.md)
- [@query Operator](./44-tsklang-javascript-operator-query.md)
- [@cache Operator](./48-tsklang-javascript-operator-cache.md)
- [@metrics Operator](./49-tsklang-javascript-operator-metrics.md)
- [@database Directive](./83-tsklang-javascript-directives-database.md) 