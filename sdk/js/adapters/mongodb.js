/**
 * MongoDB Adapter for TuskLang
 * ============================
 * Enables @query operations with MongoDB databases
 * 
 * DEFAULT CONFIG: peanut.tsk (the bridge of language grace)
 */

class MongoDBAdapter {
  constructor(options = {}) {
    this.config = {
      url: options.url || options.uri || 'mongodb://localhost:27017',
      database: options.database || options.name || 'tusklang',
      connectTimeoutMS: options.connectTimeoutMS || 10000,
      serverSelectionTimeoutMS: options.serverSelectionTimeoutMS || 5000,
      ...options.options
    };
    
    this.client = null;
    this.db = null;
    
    // Lazy load mongodb
    try {
      const { MongoClient } = require('mongodb');
      this.MongoClient = MongoClient;
    } catch (error) {
      throw new Error('MongoDB adapter requires mongodb. Install it with: npm install mongodb');
    }
  }

  /**
   * Connect to MongoDB
   */
  async connect() {
    if (!this.client) {
      this.client = new this.MongoClient(this.config.url, {
        connectTimeoutMS: this.config.connectTimeoutMS,
        serverSelectionTimeoutMS: this.config.serverSelectionTimeoutMS
      });
      
      await this.client.connect();
      this.db = this.client.db(this.config.database);
    }
    return this.db;
  }

  /**
   * Execute a query
   * MongoDB uses a special query syntax for TuskLang:
   * @query("collection.method", params)
   * 
   * Examples:
   * @query("users.count", {active: true})
   * @query("users.findOne", {email: "john@example.com"})
   * @query("users.find", {active: true}, {limit: 10})
   */
  async query(queryString, ...params) {
    try {
      await this.connect();
      
      // Parse MongoDB-style query
      const [collection, method] = queryString.split('.');
      
      if (!collection || !method) {
        throw new Error('MongoDB query must be in format: collection.method');
      }
      
      const coll = this.db.collection(collection);
      
      // Handle different MongoDB methods
      switch (method.toLowerCase()) {
        case 'count':
        case 'countdocuments':
          return await coll.countDocuments(params[0] || {});
          
        case 'findone':
          return await coll.findOne(params[0] || {}, params[1] || {});
          
        case 'find':
          const filter = params[0] || {};
          const options = params[1] || {};
          
          let cursor = coll.find(filter);
          
          if (options.limit) cursor = cursor.limit(options.limit);
          if (options.skip) cursor = cursor.skip(options.skip);
          if (options.sort) cursor = cursor.sort(options.sort);
          if (options.projection) cursor = cursor.project(options.projection);
          
          return await cursor.toArray();
          
        case 'aggregate':
          return await coll.aggregate(params[0] || []).toArray();
          
        case 'distinct':
          return await coll.distinct(params[0], params[1] || {});
          
        case 'insertone':
          const insertResult = await coll.insertOne(params[0]);
          return insertResult.insertedId;
          
        case 'insertmany':
          const insertManyResult = await coll.insertMany(params[0]);
          return insertManyResult.insertedCount;
          
        case 'updateone':
          const updateResult = await coll.updateOne(params[0], params[1], params[2] || {});
          return updateResult.modifiedCount;
          
        case 'updatemany':
          const updateManyResult = await coll.updateMany(params[0], params[1], params[2] || {});
          return updateManyResult.modifiedCount;
          
        case 'deleteone':
          const deleteResult = await coll.deleteOne(params[0]);
          return deleteResult.deletedCount;
          
        case 'deletemany':
          const deleteManyResult = await coll.deleteMany(params[0]);
          return deleteManyResult.deletedCount;
          
        // Special aggregation helpers
        case 'sum':
          const sumResult = await coll.aggregate([
            { $match: params[0] || {} },
            { $group: { _id: null, total: { $sum: `$${params[1]}` } } }
          ]).toArray();
          return sumResult[0]?.total || 0;
          
        case 'avg':
          const avgResult = await coll.aggregate([
            { $match: params[0] || {} },
            { $group: { _id: null, average: { $avg: `$${params[1]}` } } }
          ]).toArray();
          return avgResult[0]?.average || 0;
          
        case 'max':
          const maxResult = await coll.aggregate([
            { $match: params[0] || {} },
            { $group: { _id: null, maximum: { $max: `$${params[1]}` } } }
          ]).toArray();
          return maxResult[0]?.maximum || null;
          
        case 'min':
          const minResult = await coll.aggregate([
            { $match: params[0] || {} },
            { $group: { _id: null, minimum: { $min: `$${params[1]}` } } }
          ]).toArray();
          return minResult[0]?.minimum || null;
          
        default:
          throw new Error(`Unknown MongoDB method: ${method}`);
      }
      
    } catch (error) {
      console.error('MongoDB query error:', error);
      throw error;
    }
  }

  /**
   * Begin a transaction (MongoDB 4.0+)
   */
  async beginTransaction() {
    const session = this.client.startSession();
    session.startTransaction();
    return session;
  }

  /**
   * Commit a transaction
   */
  async commit(session) {
    if (session) {
      await session.commitTransaction();
      session.endSession();
    }
  }

  /**
   * Rollback a transaction
   */
  async rollback(session) {
    if (session) {
      await session.abortTransaction();
      session.endSession();
    }
  }

  /**
   * Close the connection
   */
  async close() {
    if (this.client) {
      await this.client.close();
      this.client = null;
      this.db = null;
    }
  }

  /**
   * Create test data
   */
  async createTestData() {
    try {
      await this.connect();
      
      // Users collection
      const users = this.db.collection('users');
      await users.createIndex({ email: 1 }, { unique: true });
      
      await users.insertMany([
        { name: 'John Doe', email: 'john@example.com', active: true, created_at: new Date() },
        { name: 'Jane Smith', email: 'jane@example.com', active: true, created_at: new Date() },
        { name: 'Bob Wilson', email: 'bob@example.com', active: false, created_at: new Date() }
      ]).catch(() => {}); // Ignore duplicate errors
      
      // Plans collection
      const plans = this.db.collection('plans');
      await plans.insertMany([
        { name: 'basic', rate_limit: 100, price: 9.99 },
        { name: 'premium', rate_limit: 1000, price: 29.99 },
        { name: 'enterprise', rate_limit: 10000, price: 99.99 }
      ]).catch(() => {});
      
      // Feature flags collection
      const features = this.db.collection('feature_flags');
      await features.insertMany([
        { name: 'new_dashboard', enabled: true, description: 'New dashboard UI' },
        { name: 'api_v2', enabled: false, description: 'API version 2' },
        { name: 'dark_mode', enabled: true, description: 'Dark mode theme' }
      ]).catch(() => {});
      
      // Settings collection
      const settings = this.db.collection('settings');
      await settings.createIndex({ key: 1 }, { unique: true });
      
      await settings.replaceOne(
        { key: 'max_users' },
        { key: 'max_users', value: '1000', type: 'integer' },
        { upsert: true }
      );
      
      await settings.replaceOne(
        { key: 'app_name' },
        { key: 'app_name', value: 'TuskLang Demo', type: 'string' },
        { upsert: true }
      );
      
      await settings.replaceOne(
        { key: 'maintenance_mode' },
        { key: 'maintenance_mode', value: 'false', type: 'boolean' },
        { upsert: true }
      );
      
      console.log('MongoDB test data created successfully');
      
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
      await this.db.admin().ping();
      return true;
    } catch {
      return false;
    }
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
          
          if (config.database?.mongodb) {
            return new MongoDBAdapter(config.database.mongodb);
          }
        } catch (e) {
          // Continue to next location
        }
      }
      
      throw new Error('No peanut.tsk found with MongoDB configuration');
      
    } catch (error) {
      throw new Error(`Failed to load MongoDB config from peanut.tsk: ${error.message}`);
    }
  }
}

module.exports = MongoDBAdapter;