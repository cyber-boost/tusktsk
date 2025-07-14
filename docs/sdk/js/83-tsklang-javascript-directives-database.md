# TuskLang JavaScript Documentation: #database Directive

## Overview

The `#database` directive in TuskLang defines database connections, schemas, and configurations, enabling declarative database management with JavaScript/Node.js integration.

## TuskLang Syntax

```tsk
#database postgres
  host: localhost
  port: 5432
  database: myapp
  username: ${DB_USER}
  password: ${DB_PASSWORD}
  ssl: true
  pool:
    min: 5
    max: 20
    acquire: 30000
    idle: 10000

#database mysql
  host: localhost
  port: 3306
  database: myapp
  username: ${DB_USER}
  password: ${DB_PASSWORD}
  charset: utf8mb4
  timezone: +00:00
  pool:
    min: 2
    max: 10

#database mongodb
  uri: mongodb://localhost:27017/myapp
  options:
    useNewUrlParser: true
    useUnifiedTopology: true
    maxPoolSize: 10
    serverSelectionTimeoutMS: 5000

#database redis
  host: localhost
  port: 6379
  password: ${REDIS_PASSWORD}
  db: 0
  retryDelayOnFailover: 100
  maxRetriesPerRequest: 3
```

## JavaScript Integration

### PostgreSQL Database Handler

```javascript
// postgres-db-handler.js
const { Pool } = require('pg');

class PostgresDBHandler {
  constructor(config) {
    this.config = config;
    this.pool = null;
    this.connections = new Map();
  }

  async connect() {
    const poolConfig = {
      host: this.config.host,
      port: this.config.port,
      database: this.config.database,
      user: this.config.username,
      password: this.config.password,
      ssl: this.config.ssl ? { rejectUnauthorized: false } : false,
      max: this.config.pool?.max || 20,
      min: this.config.pool?.min || 5,
      acquire: this.config.pool?.acquire || 30000,
      idle: this.config.pool?.idle || 10000
    };

    this.pool = new Pool(poolConfig);

    // Test connection
    const client = await this.pool.connect();
    await client.query('SELECT NOW()');
    client.release();

    console.log('PostgreSQL connected successfully');
  }

  async query(sql, params = []) {
    if (!this.pool) {
      throw new Error('Database not connected');
    }

    const client = await this.pool.connect();
    try {
      const result = await client.query(sql, params);
      return result.rows;
    } finally {
      client.release();
    }
  }

  async transaction(callback) {
    if (!this.pool) {
      throw new Error('Database not connected');
    }

    const client = await this.pool.connect();
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

  async createTable(tableName, schema) {
    const columns = Object.entries(schema)
      .map(([name, type]) => `${name} ${type}`)
      .join(', ');

    const sql = `CREATE TABLE IF NOT EXISTS ${tableName} (${columns})`;
    await this.query(sql);
  }

  async insert(tableName, data) {
    const columns = Object.keys(data);
    const values = Object.values(data);
    const placeholders = values.map((_, i) => `$${i + 1}`).join(', ');

    const sql = `INSERT INTO ${tableName} (${columns.join(', ')}) VALUES (${placeholders}) RETURNING *`;
    const result = await this.query(sql, values);
    return result[0];
  }

  async update(tableName, data, where) {
    const setColumns = Object.keys(data).map((key, i) => `${key} = $${i + 1}`);
    const whereColumns = Object.keys(where).map((key, i) => `${key} = $${i + Object.keys(data).length + 1}`);
    
    const values = [...Object.values(data), ...Object.values(where)];
    
    const sql = `UPDATE ${tableName} SET ${setColumns.join(', ')} WHERE ${whereColumns.join(' AND ')} RETURNING *`;
    const result = await this.query(sql, values);
    return result[0];
  }

  async delete(tableName, where) {
    const whereColumns = Object.keys(where).map((key, i) => `${key} = $${i + 1}`);
    const values = Object.values(where);
    
    const sql = `DELETE FROM ${tableName} WHERE ${whereColumns.join(' AND ')} RETURNING *`;
    const result = await this.query(sql, values);
    return result[0];
  }

  async findOne(tableName, where) {
    const whereColumns = Object.keys(where).map((key, i) => `${key} = $${i + 1}`);
    const values = Object.values(where);
    
    const sql = `SELECT * FROM ${tableName} WHERE ${whereColumns.join(' AND ')} LIMIT 1`;
    const result = await this.query(sql, values);
    return result[0] || null;
  }

  async findMany(tableName, where = {}, options = {}) {
    let sql = `SELECT * FROM ${tableName}`;
    const values = [];
    
    if (Object.keys(where).length > 0) {
      const whereColumns = Object.keys(where).map((key, i) => `${key} = $${i + 1}`);
      values.push(...Object.values(where));
      sql += ` WHERE ${whereColumns.join(' AND ')}`;
    }
    
    if (options.orderBy) {
      sql += ` ORDER BY ${options.orderBy}`;
    }
    
    if (options.limit) {
      sql += ` LIMIT ${options.limit}`;
    }
    
    if (options.offset) {
      sql += ` OFFSET ${options.offset}`;
    }
    
    return await this.query(sql, values);
  }

  async disconnect() {
    if (this.pool) {
      await this.pool.end();
      this.pool = null;
    }
  }
}

module.exports = PostgresDBHandler;
```

### MySQL Database Handler

```javascript
// mysql-db-handler.js
const mysql = require('mysql2/promise');

class MySQLDBHandler {
  constructor(config) {
    this.config = config;
    this.pool = null;
  }

  async connect() {
    const poolConfig = {
      host: this.config.host,
      port: this.config.port,
      database: this.config.database,
      user: this.config.username,
      password: this.config.password,
      charset: this.config.charset || 'utf8mb4',
      timezone: this.config.timezone || '+00:00',
      connectionLimit: this.config.pool?.max || 10,
      acquireTimeout: this.config.pool?.acquire || 30000,
      timeout: this.config.pool?.idle || 10000
    };

    this.pool = mysql.createPool(poolConfig);

    // Test connection
    const connection = await this.pool.getConnection();
    await connection.query('SELECT NOW()');
    connection.release();

    console.log('MySQL connected successfully');
  }

  async query(sql, params = []) {
    if (!this.pool) {
      throw new Error('Database not connected');
    }

    const [rows] = await this.pool.execute(sql, params);
    return rows;
  }

  async transaction(callback) {
    if (!this.pool) {
      throw new Error('Database not connected');
    }

    const connection = await this.pool.getConnection();
    try {
      await connection.beginTransaction();
      const result = await callback(connection);
      await connection.commit();
      return result;
    } catch (error) {
      await connection.rollback();
      throw error;
    } finally {
      connection.release();
    }
  }

  async createTable(tableName, schema) {
    const columns = Object.entries(schema)
      .map(([name, type]) => `${name} ${type}`)
      .join(', ');

    const sql = `CREATE TABLE IF NOT EXISTS ${tableName} (${columns})`;
    await this.query(sql);
  }

  async insert(tableName, data) {
    const columns = Object.keys(data);
    const values = Object.values(data);
    const placeholders = values.map(() => '?').join(', ');

    const sql = `INSERT INTO ${tableName} (${columns.join(', ')}) VALUES (${placeholders})`;
    const result = await this.query(sql, values);
    return { id: result.insertId, ...data };
  }

  async update(tableName, data, where) {
    const setColumns = Object.keys(data).map(key => `${key} = ?`);
    const whereColumns = Object.keys(where).map(key => `${key} = ?`);
    
    const values = [...Object.values(data), ...Object.values(where)];
    
    const sql = `UPDATE ${tableName} SET ${setColumns.join(', ')} WHERE ${whereColumns.join(' AND ')}`;
    const result = await this.query(sql, values);
    return { affectedRows: result.affectedRows };
  }

  async delete(tableName, where) {
    const whereColumns = Object.keys(where).map(key => `${key} = ?`);
    const values = Object.values(where);
    
    const sql = `DELETE FROM ${tableName} WHERE ${whereColumns.join(' AND ')}`;
    const result = await this.query(sql, values);
    return { affectedRows: result.affectedRows };
  }

  async findOne(tableName, where) {
    const whereColumns = Object.keys(where).map(key => `${key} = ?`);
    const values = Object.values(where);
    
    const sql = `SELECT * FROM ${tableName} WHERE ${whereColumns.join(' AND ')} LIMIT 1`;
    const result = await this.query(sql, values);
    return result[0] || null;
  }

  async findMany(tableName, where = {}, options = {}) {
    let sql = `SELECT * FROM ${tableName}`;
    const values = [];
    
    if (Object.keys(where).length > 0) {
      const whereColumns = Object.keys(where).map(key => `${key} = ?`);
      values.push(...Object.values(where));
      sql += ` WHERE ${whereColumns.join(' AND ')}`;
    }
    
    if (options.orderBy) {
      sql += ` ORDER BY ${options.orderBy}`;
    }
    
    if (options.limit) {
      sql += ` LIMIT ${options.limit}`;
    }
    
    if (options.offset) {
      sql += ` OFFSET ${options.offset}`;
    }
    
    return await this.query(sql, values);
  }

  async disconnect() {
    if (this.pool) {
      await this.pool.end();
      this.pool = null;
    }
  }
}

module.exports = MySQLDBHandler;
```

### MongoDB Database Handler

```javascript
// mongodb-db-handler.js
const { MongoClient } = require('mongodb');

class MongoDBHandler {
  constructor(config) {
    this.config = config;
    this.client = null;
    this.db = null;
  }

  async connect() {
    const options = {
      useNewUrlParser: true,
      useUnifiedTopology: true,
      maxPoolSize: this.config.options?.maxPoolSize || 10,
      serverSelectionTimeoutMS: this.config.options?.serverSelectionTimeoutMS || 5000,
      ...this.config.options
    };

    this.client = new MongoClient(this.config.uri, options);
    await this.client.connect();
    
    const dbName = this.config.uri.split('/').pop().split('?')[0];
    this.db = this.client.db(dbName);

    console.log('MongoDB connected successfully');
  }

  async query(collectionName, operation, ...args) {
    if (!this.db) {
      throw new Error('Database not connected');
    }

    const collection = this.db.collection(collectionName);
    return await collection[operation](...args);
  }

  async transaction(callback) {
    if (!this.client) {
      throw new Error('Database not connected');
    }

    const session = this.client.startSession();
    try {
      await session.withTransaction(async () => {
        await callback(session);
      });
    } finally {
      await session.endSession();
    }
  }

  async createCollection(collectionName, options = {}) {
    return await this.db.createCollection(collectionName, options);
  }

  async insertOne(collectionName, document) {
    const result = await this.query(collectionName, 'insertOne', document);
    return { id: result.insertedId, ...document };
  }

  async insertMany(collectionName, documents) {
    const result = await this.query(collectionName, 'insertMany', documents);
    return result.insertedIds;
  }

  async updateOne(collectionName, filter, update, options = {}) {
    const result = await this.query(collectionName, 'updateOne', filter, update, options);
    return { modifiedCount: result.modifiedCount };
  }

  async updateMany(collectionName, filter, update, options = {}) {
    const result = await this.query(collectionName, 'updateMany', filter, update, options);
    return { modifiedCount: result.modifiedCount };
  }

  async deleteOne(collectionName, filter) {
    const result = await this.query(collectionName, 'deleteOne', filter);
    return { deletedCount: result.deletedCount };
  }

  async deleteMany(collectionName, filter) {
    const result = await this.query(collectionName, 'deleteMany', filter);
    return { deletedCount: result.deletedCount };
  }

  async findOne(collectionName, filter, options = {}) {
    return await this.query(collectionName, 'findOne', filter, options);
  }

  async findMany(collectionName, filter = {}, options = {}) {
    const cursor = await this.query(collectionName, 'find', filter, options);
    return await cursor.toArray();
  }

  async aggregate(collectionName, pipeline) {
    const cursor = await this.query(collectionName, 'aggregate', pipeline);
    return await cursor.toArray();
  }

  async createIndex(collectionName, indexSpec, options = {}) {
    return await this.query(collectionName, 'createIndex', indexSpec, options);
  }

  async disconnect() {
    if (this.client) {
      await this.client.close();
      this.client = null;
      this.db = null;
    }
  }
}

module.exports = MongoDBHandler;
```

### Redis Database Handler

```javascript
// redis-db-handler.js
const Redis = require('ioredis');

class RedisDBHandler {
  constructor(config) {
    this.config = config;
    this.client = null;
  }

  async connect() {
    const redisConfig = {
      host: this.config.host,
      port: this.config.port,
      password: this.config.password,
      db: this.config.db || 0,
      retryDelayOnFailover: this.config.retryDelayOnFailover || 100,
      maxRetriesPerRequest: this.config.maxRetriesPerRequest || 3,
      lazyConnect: true
    };

    this.client = new Redis(redisConfig);
    
    // Test connection
    await this.client.ping();
    
    console.log('Redis connected successfully');
  }

  async get(key) {
    if (!this.client) {
      throw new Error('Database not connected');
    }

    const value = await this.client.get(key);
    return value ? JSON.parse(value) : null;
  }

  async set(key, value, ttl = null) {
    if (!this.client) {
      throw new Error('Database not connected');
    }

    const serialized = JSON.stringify(value);
    
    if (ttl) {
      return await this.client.setex(key, ttl, serialized);
    } else {
      return await this.client.set(key, serialized);
    }
  }

  async del(key) {
    if (!this.client) {
      throw new Error('Database not connected');
    }

    return await this.client.del(key);
  }

  async exists(key) {
    if (!this.client) {
      throw new Error('Database not connected');
    }

    return await this.client.exists(key);
  }

  async expire(key, ttl) {
    if (!this.client) {
      throw new Error('Database not connected');
    }

    return await this.client.expire(key, ttl);
  }

  async ttl(key) {
    if (!this.client) {
      throw new Error('Database not connected');
    }

    return await this.client.ttl(key);
  }

  async hget(key, field) {
    if (!this.client) {
      throw new Error('Database not connected');
    }

    const value = await this.client.hget(key, field);
    return value ? JSON.parse(value) : null;
  }

  async hset(key, field, value) {
    if (!this.client) {
      throw new Error('Database not connected');
    }

    const serialized = JSON.stringify(value);
    return await this.client.hset(key, field, serialized);
  }

  async hgetall(key) {
    if (!this.client) {
      throw new Error('Database not connected');
    }

    const hash = await this.client.hgetall(key);
    const result = {};
    
    for (const [field, value] of Object.entries(hash)) {
      result[field] = JSON.parse(value);
    }
    
    return result;
  }

  async lpush(key, value) {
    if (!this.client) {
      throw new Error('Database not connected');
    }

    const serialized = JSON.stringify(value);
    return await this.client.lpush(key, serialized);
  }

  async rpop(key) {
    if (!this.client) {
      throw new Error('Database not connected');
    }

    const value = await this.client.rpop(key);
    return value ? JSON.parse(value) : null;
  }

  async lrange(key, start, stop) {
    if (!this.client) {
      throw new Error('Database not connected');
    }

    const values = await this.client.lrange(key, start, stop);
    return values.map(value => JSON.parse(value));
  }

  async sadd(key, member) {
    if (!this.client) {
      throw new Error('Database not connected');
    }

    const serialized = JSON.stringify(member);
    return await this.client.sadd(key, serialized);
  }

  async smembers(key) {
    if (!this.client) {
      throw new Error('Database not connected');
    }

    const members = await this.client.smembers(key);
    return members.map(member => JSON.parse(member));
  }

  async publish(channel, message) {
    if (!this.client) {
      throw new Error('Database not connected');
    }

    const serialized = JSON.stringify(message);
    return await this.client.publish(channel, serialized);
  }

  async subscribe(channel, callback) {
    if (!this.client) {
      throw new Error('Database not connected');
    }

    const subscriber = this.client.duplicate();
    await subscriber.subscribe(channel);
    
    subscriber.on('message', (ch, message) => {
      const parsed = JSON.parse(message);
      callback(ch, parsed);
    });
    
    return subscriber;
  }

  async disconnect() {
    if (this.client) {
      await this.client.quit();
      this.client = null;
    }
  }
}

module.exports = RedisDBHandler;
```

## TypeScript Implementation

```typescript
// database-handler.types.ts
export interface DatabaseConfig {
  host?: string;
  port?: number;
  database?: string;
  username?: string;
  password?: string;
  uri?: string;
  ssl?: boolean;
  charset?: string;
  timezone?: string;
  pool?: {
    min?: number;
    max?: number;
    acquire?: number;
    idle?: number;
  };
  options?: Record<string, any>;
  retryDelayOnFailover?: number;
  maxRetriesPerRequest?: number;
  db?: number;
}

export interface QueryResult {
  rows?: any[];
  rowCount?: number;
  insertId?: number;
  affectedRows?: number;
}

export interface DatabaseHandler {
  connect(): Promise<void>;
  query(sql: string, params?: any[]): Promise<any>;
  transaction(callback: (connection: any) => Promise<any>): Promise<any>;
  disconnect(): Promise<void>;
}

export interface Document {
  _id?: string;
  [key: string]: any;
}

// database-handler.ts
import { DatabaseConfig, DatabaseHandler, QueryResult, Document } from './database-handler.types';

export class TypeScriptDatabaseHandler implements DatabaseHandler {
  private config: DatabaseConfig;
  private connection: any;

  constructor(config: DatabaseConfig) {
    this.config = config;
  }

  async connect(): Promise<void> {
    // Implementation depends on database type
    throw new Error('Method not implemented');
  }

  async query(sql: string, params: any[] = []): Promise<any> {
    if (!this.connection) {
      throw new Error('Database not connected');
    }
    // Implementation depends on database type
    throw new Error('Method not implemented');
  }

  async transaction(callback: (connection: any) => Promise<any>): Promise<any> {
    if (!this.connection) {
      throw new Error('Database not connected');
    }
    // Implementation depends on database type
    throw new Error('Method not implemented');
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      // Implementation depends on database type
      this.connection = null;
    }
  }
}

export class TypeScriptPostgresHandler extends TypeScriptDatabaseHandler {
  private pool: any;

  async connect(): Promise<void> {
    const { Pool } = require('pg');
    
    this.pool = new Pool({
      host: this.config.host,
      port: this.config.port,
      database: this.config.database,
      user: this.config.username,
      password: this.config.password,
      ssl: this.config.ssl ? { rejectUnauthorized: false } : false,
      max: this.config.pool?.max || 20,
      min: this.config.pool?.min || 5
    });

    const client = await this.pool.connect();
    await client.query('SELECT NOW()');
    client.release();
  }

  async query(sql: string, params: any[] = []): Promise<any> {
    const client = await this.pool.connect();
    try {
      const result = await client.query(sql, params);
      return result.rows;
    } finally {
      client.release();
    }
  }

  async transaction(callback: (connection: any) => Promise<any>): Promise<any> {
    const client = await this.pool.connect();
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

  async disconnect(): Promise<void> {
    if (this.pool) {
      await this.pool.end();
      this.pool = null;
    }
  }
}

export class TypeScriptMongoHandler extends TypeScriptDatabaseHandler {
  private client: any;
  private db: any;

  async connect(): Promise<void> {
    const { MongoClient } = require('mongodb');
    
    this.client = new MongoClient(this.config.uri!, {
      useNewUrlParser: true,
      useUnifiedTopology: true
    });
    
    await this.client.connect();
    const dbName = this.config.uri!.split('/').pop()!.split('?')[0];
    this.db = this.client.db(dbName);
  }

  async query(collectionName: string, operation: string, ...args: any[]): Promise<any> {
    const collection = this.db.collection(collectionName);
    return await collection[operation](...args);
  }

  async transaction(callback: (session: any) => Promise<any>): Promise<any> {
    const session = this.client.startSession();
    try {
      return await session.withTransaction(async () => {
        return await callback(session);
      });
    } finally {
      await session.endSession();
    }
  }

  async disconnect(): Promise<void> {
    if (this.client) {
      await this.client.close();
      this.client = null;
      this.db = null;
    }
  }
}
```

## Advanced Usage Scenarios

### Database Connection Pool Management

```javascript
// connection-pool-manager.js
class ConnectionPoolManager {
  constructor() {
    this.pools = new Map();
    this.stats = new Map();
  }

  async createPool(name, config) {
    const pool = await this.createDatabasePool(config);
    this.pools.set(name, pool);
    this.stats.set(name, {
      created: Date.now(),
      connections: 0,
      queries: 0
    });
    
    return pool;
  }

  async getPool(name) {
    const pool = this.pools.get(name);
    if (!pool) {
      throw new Error(`Pool '${name}' not found`);
    }
    return pool;
  }

  async executeQuery(poolName, query, params = []) {
    const pool = await this.getPool(poolName);
    const stats = this.stats.get(poolName);
    
    stats.queries++;
    
    const start = Date.now();
    try {
      const result = await pool.query(query, params);
      const duration = Date.now() - start;
      
      console.log(`Query executed in ${duration}ms on pool '${poolName}'`);
      return result;
    } catch (error) {
      console.error(`Query failed on pool '${poolName}':`, error);
      throw error;
    }
  }

  async closePool(name) {
    const pool = this.pools.get(name);
    if (pool) {
      await pool.end();
      this.pools.delete(name);
      this.stats.delete(name);
    }
  }

  getPoolStats() {
    const stats = {};
    for (const [name, stat] of this.stats.entries()) {
      stats[name] = {
        ...stat,
        uptime: Date.now() - stat.created
      };
    }
    return stats;
  }
}
```

### Database Migration System

```javascript
// migration-system.js
class MigrationSystem {
  constructor(dbHandler) {
    this.dbHandler = dbHandler;
    this.migrations = new Map();
  }

  registerMigration(version, up, down) {
    this.migrations.set(version, { up, down });
  }

  async createMigrationsTable() {
    const sql = `
      CREATE TABLE IF NOT EXISTS migrations (
        id SERIAL PRIMARY KEY,
        version VARCHAR(255) UNIQUE NOT NULL,
        applied_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
      )
    `;
    await this.dbHandler.query(sql);
  }

  async getAppliedMigrations() {
    const sql = 'SELECT version FROM migrations ORDER BY version';
    const result = await this.dbHandler.query(sql);
    return result.map(row => row.version);
  }

  async migrate(targetVersion = null) {
    await this.createMigrationsTable();
    
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

    await this.dbHandler.transaction(async (connection) => {
      if (direction === 'up') {
        await migration.up(connection);
        await connection.query(
          'INSERT INTO migrations (version) VALUES ($1)',
          [version]
        );
      } else {
        await migration.down(connection);
        await connection.query(
          'DELETE FROM migrations WHERE version = $1',
          [version]
        );
      }
    });

    console.log(`Migration ${version} ${direction} completed`);
  }
}

// Usage
const migrationSystem = new MigrationSystem(postgresHandler);

migrationSystem.registerMigration('001_create_users', 
  async (connection) => {
    await connection.query(`
      CREATE TABLE users (
        id SERIAL PRIMARY KEY,
        email VARCHAR(255) UNIQUE NOT NULL,
        password_hash VARCHAR(255) NOT NULL,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
      )
    `);
  },
  async (connection) => {
    await connection.query('DROP TABLE users');
  }
);

await migrationSystem.migrate();
```

## Real-World Examples

### E-commerce Database Schema

```javascript
// ecommerce-schema.js
class EcommerceSchema {
  constructor(dbHandler) {
    this.db = dbHandler;
  }

  async createTables() {
    await this.createUsersTable();
    await this.createProductsTable();
    await this.createOrdersTable();
    await this.createOrderItemsTable();
    await this.createCategoriesTable();
  }

  async createUsersTable() {
    const sql = `
      CREATE TABLE IF NOT EXISTS users (
        id SERIAL PRIMARY KEY,
        email VARCHAR(255) UNIQUE NOT NULL,
        password_hash VARCHAR(255) NOT NULL,
        first_name VARCHAR(100),
        last_name VARCHAR(100),
        phone VARCHAR(20),
        address TEXT,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
      )
    `;
    await this.db.query(sql);
  }

  async createProductsTable() {
    const sql = `
      CREATE TABLE IF NOT EXISTS products (
        id SERIAL PRIMARY KEY,
        name VARCHAR(255) NOT NULL,
        description TEXT,
        price DECIMAL(10,2) NOT NULL,
        stock_quantity INTEGER DEFAULT 0,
        category_id INTEGER REFERENCES categories(id),
        image_url VARCHAR(500),
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
      )
    `;
    await this.db.query(sql);
  }

  async createOrdersTable() {
    const sql = `
      CREATE TABLE IF NOT EXISTS orders (
        id SERIAL PRIMARY KEY,
        user_id INTEGER REFERENCES users(id),
        status VARCHAR(50) DEFAULT 'pending',
        total_amount DECIMAL(10,2) NOT NULL,
        shipping_address TEXT,
        billing_address TEXT,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
      )
    `;
    await this.db.query(sql);
  }

  async createOrderItemsTable() {
    const sql = `
      CREATE TABLE IF NOT EXISTS order_items (
        id SERIAL PRIMARY KEY,
        order_id INTEGER REFERENCES orders(id),
        product_id INTEGER REFERENCES products(id),
        quantity INTEGER NOT NULL,
        unit_price DECIMAL(10,2) NOT NULL,
        total_price DECIMAL(10,2) NOT NULL
      )
    `;
    await this.db.query(sql);
  }

  async createCategoriesTable() {
    const sql = `
      CREATE TABLE IF NOT EXISTS categories (
        id SERIAL PRIMARY KEY,
        name VARCHAR(100) NOT NULL,
        description TEXT,
        parent_id INTEGER REFERENCES categories(id),
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
      )
    `;
    await this.db.query(sql);
  }
}
```

### Blog Database Schema

```javascript
// blog-schema.js
class BlogSchema {
  constructor(dbHandler) {
    this.db = dbHandler;
  }

  async createTables() {
    await this.createPostsTable();
    await this.createCommentsTable();
    await this.createTagsTable();
    await this.createPostTagsTable();
  }

  async createPostsTable() {
    const sql = `
      CREATE TABLE IF NOT EXISTS posts (
        id SERIAL PRIMARY KEY,
        title VARCHAR(255) NOT NULL,
        slug VARCHAR(255) UNIQUE NOT NULL,
        content TEXT NOT NULL,
        excerpt TEXT,
        author_id INTEGER REFERENCES users(id),
        status VARCHAR(20) DEFAULT 'draft',
        published_at TIMESTAMP,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
      )
    `;
    await this.db.query(sql);
  }

  async createCommentsTable() {
    const sql = `
      CREATE TABLE IF NOT EXISTS comments (
        id SERIAL PRIMARY KEY,
        post_id INTEGER REFERENCES posts(id),
        author_name VARCHAR(100),
        author_email VARCHAR(255),
        content TEXT NOT NULL,
        status VARCHAR(20) DEFAULT 'pending',
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
      )
    `;
    await this.db.query(sql);
  }

  async createTagsTable() {
    const sql = `
      CREATE TABLE IF NOT EXISTS tags (
        id SERIAL PRIMARY KEY,
        name VARCHAR(50) UNIQUE NOT NULL,
        slug VARCHAR(50) UNIQUE NOT NULL,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
      )
    `;
    await this.db.query(sql);
  }

  async createPostTagsTable() {
    const sql = `
      CREATE TABLE IF NOT EXISTS post_tags (
        post_id INTEGER REFERENCES posts(id),
        tag_id INTEGER REFERENCES tags(id),
        PRIMARY KEY (post_id, tag_id)
      )
    `;
    await this.db.query(sql);
  }
}
```

## Performance Considerations

### Query Optimization

```javascript
// query-optimizer.js
class QueryOptimizer {
  constructor(dbHandler) {
    this.db = dbHandler;
    this.queryCache = new Map();
  }

  async optimizeQuery(sql, params = []) {
    // Check cache first
    const cacheKey = this.generateCacheKey(sql, params);
    if (this.queryCache.has(cacheKey)) {
      return this.queryCache.get(cacheKey);
    }

    // Analyze query
    const analysis = this.analyzeQuery(sql);
    
    // Apply optimizations
    const optimizedSql = this.applyOptimizations(sql, analysis);
    
    // Execute query
    const result = await this.db.query(optimizedSql, params);
    
    // Cache result
    this.queryCache.set(cacheKey, result);
    
    return result;
  }

  analyzeQuery(sql) {
    const analysis = {
      type: this.getQueryType(sql),
      tables: this.extractTables(sql),
      joins: this.countJoins(sql),
      whereClauses: this.extractWhereClauses(sql)
    };
    
    return analysis;
  }

  getQueryType(sql) {
    const upper = sql.trim().toUpperCase();
    if (upper.startsWith('SELECT')) return 'SELECT';
    if (upper.startsWith('INSERT')) return 'INSERT';
    if (upper.startsWith('UPDATE')) return 'UPDATE';
    if (upper.startsWith('DELETE')) return 'DELETE';
    return 'UNKNOWN';
  }

  extractTables(sql) {
    const fromMatch = sql.match(/FROM\s+(\w+)/i);
    const joinMatches = sql.match(/JOIN\s+(\w+)/gi);
    
    const tables = [];
    if (fromMatch) tables.push(fromMatch[1]);
    if (joinMatches) {
      joinMatches.forEach(match => {
        const table = match.replace(/JOIN\s+/i, '');
        tables.push(table);
      });
    }
    
    return tables;
  }

  countJoins(sql) {
    const joinMatches = sql.match(/JOIN/gi);
    return joinMatches ? joinMatches.length : 0;
  }

  extractWhereClauses(sql) {
    const whereMatch = sql.match(/WHERE\s+(.+?)(?:ORDER BY|GROUP BY|LIMIT|$)/i);
    return whereMatch ? whereMatch[1].split('AND').length : 0;
  }

  applyOptimizations(sql, analysis) {
    let optimized = sql;
    
    // Add LIMIT if missing for SELECT queries
    if (analysis.type === 'SELECT' && !sql.includes('LIMIT')) {
      optimized += ' LIMIT 1000';
    }
    
    // Add ORDER BY if missing
    if (analysis.type === 'SELECT' && !sql.includes('ORDER BY')) {
      optimized += ' ORDER BY id DESC';
    }
    
    return optimized;
  }

  generateCacheKey(sql, params) {
    return `${sql}:${JSON.stringify(params)}`;
  }

  clearCache() {
    this.queryCache.clear();
  }
}
```

### Connection Pooling

```javascript
// connection-pool.js
class ConnectionPool {
  constructor(config) {
    this.config = config;
    this.pool = [];
    this.inUse = new Set();
    this.waiting = [];
  }

  async getConnection() {
    // Try to get an available connection
    const available = this.pool.find(conn => !this.inUse.has(conn));
    
    if (available) {
      this.inUse.add(available);
      return available;
    }

    // Create new connection if pool not full
    if (this.pool.length < this.config.max) {
      const connection = await this.createConnection();
      this.pool.push(connection);
      this.inUse.add(connection);
      return connection;
    }

    // Wait for available connection
    return new Promise((resolve) => {
      this.waiting.push(resolve);
    });
  }

  async releaseConnection(connection) {
    this.inUse.delete(connection);
    
    // Notify waiting requests
    if (this.waiting.length > 0) {
      const resolve = this.waiting.shift();
      this.inUse.add(connection);
      resolve(connection);
    }
  }

  async createConnection() {
    // Implementation depends on database type
    throw new Error('Method not implemented');
  }

  async close() {
    for (const connection of this.pool) {
      await connection.close();
    }
    this.pool = [];
    this.inUse.clear();
  }
}
```

## Security Notes

### SQL Injection Prevention

```javascript
// sql-injection-prevention.js
class SQLInjectionPrevention {
  static sanitizeInput(input) {
    if (typeof input === 'string') {
      // Remove dangerous characters
      return input.replace(/['";\\]/g, '');
    }
    return input;
  }

  static validateQuery(sql) {
    const dangerousPatterns = [
      /DROP\s+TABLE/i,
      /DELETE\s+FROM/i,
      /UPDATE\s+.+\s+SET/i,
      /INSERT\s+INTO/i,
      /CREATE\s+TABLE/i,
      /ALTER\s+TABLE/i
    ];

    for (const pattern of dangerousPatterns) {
      if (pattern.test(sql)) {
        throw new Error('Potentially dangerous SQL operation detected');
      }
    }
  }

  static useParameterizedQueries(sql, params) {
    // Ensure all user input is parameterized
    const paramCount = (sql.match(/\?/g) || []).length;
    if (paramCount !== params.length) {
      throw new Error('Parameter count mismatch');
    }
    
    return { sql, params };
  }
}
```

### Database Access Control

```javascript
// database-access-control.js
class DatabaseAccessControl {
  constructor() {
    this.permissions = new Map();
    this.roles = new Map();
  }

  defineRole(role, permissions) {
    this.roles.set(role, permissions);
  }

  definePermission(permission, tables, operations) {
    this.permissions.set(permission, { tables, operations });
  }

  checkPermission(user, table, operation) {
    const userRole = user.role;
    const rolePermissions = this.roles.get(userRole);
    
    if (!rolePermissions) {
      return false;
    }

    for (const permission of rolePermissions) {
      const perm = this.permissions.get(permission);
      if (perm && perm.tables.includes(table) && perm.operations.includes(operation)) {
        return true;
      }
    }

    return false;
  }

  validateQuery(user, sql) {
    const tables = this.extractTables(sql);
    const operation = this.getOperation(sql);
    
    for (const table of tables) {
      if (!this.checkPermission(user, table, operation)) {
        throw new Error(`Access denied to ${operation} on ${table}`);
      }
    }
  }

  extractTables(sql) {
    // Implementation to extract table names from SQL
    const fromMatch = sql.match(/FROM\s+(\w+)/i);
    const joinMatches = sql.match(/JOIN\s+(\w+)/gi);
    
    const tables = [];
    if (fromMatch) tables.push(fromMatch[1]);
    if (joinMatches) {
      joinMatches.forEach(match => {
        const table = match.replace(/JOIN\s+/i, '');
        tables.push(table);
      });
    }
    
    return tables;
  }

  getOperation(sql) {
    const upper = sql.trim().toUpperCase();
    if (upper.startsWith('SELECT')) return 'SELECT';
    if (upper.startsWith('INSERT')) return 'INSERT';
    if (upper.startsWith('UPDATE')) return 'UPDATE';
    if (upper.startsWith('DELETE')) return 'DELETE';
    return 'UNKNOWN';
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
    this.environments = ['development', 'staging', 'production'];
  }

  setConfig(environment, config) {
    if (!this.environments.includes(environment)) {
      throw new Error(`Invalid environment: ${environment}`);
    }
    
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
    const required = ['host', 'port', 'database', 'username', 'password'];
    const missing = required.filter(field => !config[field]);
    
    if (missing.length > 0) {
      throw new Error(`Missing required fields: ${missing.join(', ')}`);
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
  constructor(dbHandler) {
    this.db = dbHandler;
    this.metrics = {
      queries: 0,
      errors: 0,
      slowQueries: 0,
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
      
      if (responseTime > 1000) {
        this.metrics.slowQueries++;
      }
      
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

  resetMetrics() {
    this.metrics = {
      queries: 0,
      errors: 0,
      slowQueries: 0,
      avgResponseTime: 0
    };
  }
}
```

## Related Topics

- [@query Operator](./44-tsklang-javascript-operator-query.md)
- [@database Operator](./25-tsklang-javascript-operator-database.md)
- [@cache Operator](./48-tsklang-javascript-operator-cache.md)
- [@metrics Operator](./49-tsklang-javascript-operator-metrics.md)
- [@optimize Operator](./50-tsklang-javascript-operator-optimize.md) 