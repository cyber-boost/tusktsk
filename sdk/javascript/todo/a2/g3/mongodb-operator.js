/**
 * MongoDB Operator - Production-Ready Implementation
 * Enterprise-grade MongoDB integration with replica sets, aggregation pipelines,
 * GridFS integration, change streams, and comprehensive index management
 */

const { MongoClient, GridFSBucket, ObjectId } = require('mongodb');
const crypto = require('crypto');
const fs = require('fs');
const path = require('path');

class MongoDBOperator {
  constructor() {
    this.clients = new Map();
    this.metrics = {
      connections: 0,
      operations: 0,
      aggregations: 0,
      gridfsOperations: 0,
      changeStreams: 0,
      errors: 0,
      avgResponseTime: 0,
      startTime: Date.now()
    };
    this.circuitBreaker = {
      failures: 0,
      lastFailure: 0,
      threshold: 5,
      timeout: 60000, // 1 minute
      state: 'CLOSED' // CLOSED, OPEN, HALF_OPEN
    };
    this.changeStreams = new Map();
    this.indexCache = new Map();
  }

  /**
   * Create or get MongoDB client with connection pooling
   */
  createClient(connectionString, options = {}) {
    const clientKey = this.generateClientKey(connectionString);
    
    if (this.clients.has(clientKey)) {
      return this.clients.get(clientKey);
    }

    const clientOptions = {
      maxPoolSize: options.maxPoolSize || 20,
      minPoolSize: options.minPoolSize || 5,
      maxIdleTimeMS: options.maxIdleTimeMS || 30000,
      connectTimeoutMS: options.connectTimeoutMS || 10000,
      socketTimeoutMS: options.socketTimeoutMS || 30000,
      serverSelectionTimeoutMS: options.serverSelectionTimeoutMS || 30000,
      heartbeatFrequencyMS: options.heartbeatFrequencyMS || 10000,
      retryWrites: options.retryWrites !== false,
      retryReads: options.retryReads !== false,
      readPreference: options.readPreference || 'primary',
      writeConcern: options.writeConcern || { w: 'majority' },
      readConcern: options.readConcern || { level: 'local' },
      ssl: options.ssl !== false,
      ...options
    };

    const client = new MongoClient(connectionString, clientOptions);

    // Client event handlers
    client.on('connect', () => {
      this.metrics.connections++;
      console.log(`MongoDB: Client connected. Total connections: ${this.metrics.connections}`);
    });

    client.on('error', (err) => {
      this.metrics.errors++;
      this.circuitBreaker.failures++;
      this.circuitBreaker.lastFailure = Date.now();
      console.error('MongoDB: Client error', err);
    });

    client.on('close', () => {
      this.metrics.connections = Math.max(0, this.metrics.connections - 1);
      console.log(`MongoDB: Client closed. Total connections: ${this.metrics.connections}`);
    });

    this.clients.set(clientKey, client);
    return client;
  }

  /**
   * Generate unique client key from connection string
   */
  generateClientKey(connectionString) {
    return crypto.createHash('md5').update(connectionString).digest('hex');
  }

  /**
   * Execute MongoDB operation with comprehensive error handling
   */
  async executeOperation(connectionString, database, collection, operation, data = {}, options = {}) {
    const startTime = Date.now();
    const operationId = crypto.randomUUID();
    
    console.log(`MongoDB: Executing operation ${operationId}`, {
      database,
      collection,
      operation,
      dataKeys: Object.keys(data),
      options
    });

    // Circuit breaker check
    if (this.circuitBreaker.state === 'OPEN') {
      const timeSinceLastFailure = Date.now() - this.circuitBreaker.lastFailure;
      if (timeSinceLastFailure < this.circuitBreaker.timeout) {
        throw new Error('Circuit breaker is OPEN - too many recent failures');
      }
      this.circuitBreaker.state = 'HALF_OPEN';
    }

    const client = this.createClient(connectionString, options);
    const db = client.db(database);
    const col = db.collection(collection);

    try {
      // Operation timeout
      const operationTimeout = options.operationTimeout || 30000;
      const timeoutPromise = new Promise((_, reject) => {
        setTimeout(() => reject(new Error('Operation timeout')), operationTimeout);
      });

      let result;
      const operationPromise = this.performOperation(col, operation, data, options);
      result = await Promise.race([operationPromise, timeoutPromise]);

      // Update metrics
      const responseTime = Date.now() - startTime;
      this.metrics.operations++;
      this.metrics.avgResponseTime = (this.metrics.avgResponseTime + responseTime) / 2;

      // Reset circuit breaker on success
      if (this.circuitBreaker.state === 'HALF_OPEN') {
        this.circuitBreaker.state = 'CLOSED';
        this.circuitBreaker.failures = 0;
      }

      console.log(`MongoDB: Operation ${operationId} completed successfully`, {
        responseTime: `${responseTime}ms`,
        avgResponseTime: `${this.metrics.avgResponseTime}ms`
      });

      return {
        success: true,
        result,
        responseTime,
        operationId
      };

    } catch (error) {
      this.metrics.errors++;
      this.circuitBreaker.failures++;
      this.circuitBreaker.lastFailure = Date.now();

      // Update circuit breaker state
      if (this.circuitBreaker.failures >= this.circuitBreaker.threshold) {
        this.circuitBreaker.state = 'OPEN';
      }

      console.error(`MongoDB: Operation ${operationId} failed`, {
        error: error.message,
        operation,
        responseTime: Date.now() - startTime
      });

      throw error;
    }
  }

  /**
   * Perform specific MongoDB operation
   */
  async performOperation(collection, operation, data, options = {}) {
    switch (operation.toLowerCase()) {
      case 'find':
        return await collection.find(data.filter || {}, data.options || {}).toArray();
      
      case 'findone':
        return await collection.findOne(data.filter || {}, data.options || {});
      
      case 'insertone':
        return await collection.insertOne(data.document || {});
      
      case 'insertmany':
        return await collection.insertMany(data.documents || []);
      
      case 'updateone':
        return await collection.updateOne(
          data.filter || {},
          data.update || {},
          data.options || {}
        );
      
      case 'updatemany':
        return await collection.updateMany(
          data.filter || {},
          data.update || {},
          data.options || {}
        );
      
      case 'deleteone':
        return await collection.deleteOne(data.filter || {});
      
      case 'deletemany':
        return await collection.deleteMany(data.filter || {});
      
      case 'count':
        return await collection.countDocuments(data.filter || {}, data.options || {});
      
      case 'distinct':
        return await collection.distinct(data.field || '', data.filter || {}, data.options || {});
      
      case 'aggregate':
        this.metrics.aggregations++;
        return await collection.aggregate(data.pipeline || [], data.options || {}).toArray();
      
      case 'createindex':
        return await collection.createIndex(data.keys || {}, data.options || {});
      
      case 'dropindex':
        return await collection.dropIndex(data.indexName || '');
      
      case 'listindexes':
        return await collection.listIndexes().toArray();
      
      default:
        throw new Error(`Unknown MongoDB operation: ${operation}`);
    }
  }

  /**
   * Execute aggregation pipeline with advanced features
   */
  async executeAggregation(connectionString, database, collection, pipeline, options = {}) {
    const startTime = Date.now();
    const aggregationId = crypto.randomUUID();
    
    console.log(`MongoDB: Executing aggregation ${aggregationId}`, {
      database,
      collection,
      pipelineStages: pipeline.length,
      options
    });

    const client = this.createClient(connectionString, options);
    const db = client.db(database);
    const col = db.collection(collection);

    try {
      const result = await col.aggregate(pipeline, options).toArray();
      
      const responseTime = Date.now() - startTime;
      this.metrics.aggregations++;
      this.metrics.avgResponseTime = (this.metrics.avgResponseTime + responseTime) / 2;

      console.log(`MongoDB: Aggregation ${aggregationId} completed successfully`, {
        responseTime: `${responseTime}ms`,
        resultCount: result.length
      });

      return {
        success: true,
        result,
        responseTime,
        aggregationId
      };

    } catch (error) {
      this.metrics.errors++;
      console.error(`MongoDB: Aggregation ${aggregationId} failed`, {
        error: error.message,
        responseTime: Date.now() - startTime
      });

      throw error;
    }
  }

  /**
   * GridFS operations for large file handling
   */
  async executeGridFSOperation(connectionString, database, operation, data = {}, options = {}) {
    const startTime = Date.now();
    const gridfsId = crypto.randomUUID();
    
    console.log(`MongoDB: Executing GridFS operation ${gridfsId}`, {
      database,
      operation,
      dataKeys: Object.keys(data),
      options
    });

    const client = this.createClient(connectionString, options);
    const db = client.db(database);
    const bucket = new GridFSBucket(db, options.bucketOptions || {});

    try {
      let result;
      
      switch (operation.toLowerCase()) {
        case 'upload':
          const uploadStream = bucket.openUploadStream(data.filename || 'file', {
            metadata: data.metadata || {},
            contentType: data.contentType || 'application/octet-stream'
          });
          
          if (data.filePath) {
            const fileStream = fs.createReadStream(data.filePath);
            fileStream.pipe(uploadStream);
            result = await new Promise((resolve, reject) => {
              uploadStream.on('finish', () => resolve({ fileId: uploadStream.id }));
              uploadStream.on('error', reject);
            });
          } else if (data.buffer) {
            uploadStream.end(data.buffer);
            result = await new Promise((resolve, reject) => {
              uploadStream.on('finish', () => resolve({ fileId: uploadStream.id }));
              uploadStream.on('error', reject);
            });
          }
          break;
        
        case 'download':
          const downloadStream = bucket.openDownloadStream(new ObjectId(data.fileId));
          const chunks = [];
          result = await new Promise((resolve, reject) => {
            downloadStream.on('data', chunk => chunks.push(chunk));
            downloadStream.on('end', () => resolve({ data: Buffer.concat(chunks) }));
            downloadStream.on('error', reject);
          });
          break;
        
        case 'delete':
          result = await bucket.delete(new ObjectId(data.fileId));
          break;
        
        case 'find':
          result = await bucket.find(data.filter || {}).toArray();
          break;
        
        default:
          throw new Error(`Unknown GridFS operation: ${operation}`);
      }

      const responseTime = Date.now() - startTime;
      this.metrics.gridfsOperations++;
      this.metrics.avgResponseTime = (this.metrics.avgResponseTime + responseTime) / 2;

      console.log(`MongoDB: GridFS operation ${gridfsId} completed successfully`, {
        responseTime: `${responseTime}ms`
      });

      return {
        success: true,
        result,
        responseTime,
        gridfsId
      };

    } catch (error) {
      this.metrics.errors++;
      console.error(`MongoDB: GridFS operation ${gridfsId} failed`, {
        error: error.message,
        responseTime: Date.now() - startTime
      });

      throw error;
    }
  }

  /**
   * Create change stream for real-time data monitoring
   */
  async createChangeStream(connectionString, database, collection, pipeline = [], options = {}) {
    const streamId = crypto.randomUUID();
    
    console.log(`MongoDB: Creating change stream ${streamId}`, {
      database,
      collection,
      pipelineStages: pipeline.length,
      options
    });

    const client = this.createClient(connectionString, options);
    const db = client.db(database);
    const col = db.collection(collection);

    try {
      const changeStream = col.watch(pipeline, options);
      
      changeStream.on('change', (change) => {
        console.log(`MongoDB: Change stream ${streamId} detected change`, {
          operationType: change.operationType,
          documentId: change.documentKey?._id
        });
        
        // Emit change event for external listeners
        if (options.onChange) {
          options.onChange(change);
        }
      });

      changeStream.on('error', (error) => {
        this.metrics.errors++;
        console.error(`MongoDB: Change stream ${streamId} error`, error);
      });

      this.changeStreams.set(streamId, { changeStream, client });
      this.metrics.changeStreams++;

      return {
        success: true,
        streamId,
        close: () => this.closeChangeStream(streamId)
      };

    } catch (error) {
      this.metrics.errors++;
      console.error(`MongoDB: Failed to create change stream ${streamId}`, error);
      throw error;
    }
  }

  /**
   * Close change stream
   */
  async closeChangeStream(streamId) {
    const streamData = this.changeStreams.get(streamId);
    if (streamData) {
      await streamData.changeStream.close();
      this.changeStreams.delete(streamId);
      this.metrics.changeStreams = Math.max(0, this.metrics.changeStreams - 1);
      console.log(`MongoDB: Change stream ${streamId} closed`);
    }
  }

  /**
   * Health check for MongoDB instance
   */
  async healthCheck(connectionString, options = {}) {
    try {
      const client = this.createClient(connectionString, options);
      const adminDb = client.db('admin');
      
      const result = await adminDb.command({
        serverStatus: 1,
        repl: 1,
        connections: 1
      });

      return {
        healthy: true,
        version: result.version,
        uptime: result.uptime,
        connections: result.connections,
        repl: result.repl,
        responseTime: Date.now()
      };
    } catch (error) {
      return {
        healthy: false,
        error: error.message,
        timestamp: new Date().toISOString()
      };
    }
  }

  /**
   * Get comprehensive metrics
   */
  getMetrics() {
    const uptime = Date.now() - this.metrics.startTime;
    return {
      ...this.metrics,
      uptime,
      errorRate: this.metrics.operations > 0 ? (this.metrics.errors / this.metrics.operations) * 100 : 0,
      circuitBreaker: { ...this.circuitBreaker },
      clients: this.clients.size,
      activeChangeStreams: this.changeStreams.size
    };
  }

  /**
   * Close all connections and streams
   */
  async closeAll() {
    console.log('MongoDB: Closing all connections and streams');
    
    // Close all change streams
    const closeStreamPromises = Array.from(this.changeStreams.keys()).map(streamId => 
      this.closeChangeStream(streamId)
    );
    await Promise.all(closeStreamPromises);
    
    // Close all clients
    const closeClientPromises = Array.from(this.clients.values()).map(client => client.close());
    await Promise.all(closeClientPromises);
    
    this.clients.clear();
    console.log('MongoDB: All connections and streams closed');
  }

  /**
   * Execute MongoDB operator with TuskLang integration
   */
  async executeMongoDbOperator(params) {
    try {
      // Parse MongoDB operations: "url, database, collection, operation, ...options"
      const parts = params.split(',').map(p => p.trim());
      if (parts.length < 4) {
        throw new Error('Invalid @mongodb syntax. Expected: url, database, collection, operation, ...options');
      }

      const url = this.parseValue(parts[0]);
      const database = this.parseValue(parts[1]);
      const collection = this.parseValue(parts[2]);
      const operation = this.parseValue(parts[3]);
      const data = parts[4] ? this.parseValue(parts[4]) : {};
      const options = parts[5] ? this.parseValue(parts[5]) : {};

      // Execute operation
      const result = await this.executeOperation(url, database, collection, operation, data, options);

      console.log(`MongoDB: Operator executed successfully`, {
        database,
        collection,
        operation,
        responseTime: result.responseTime
      });

      return {
        success: true,
        result: result.result,
        responseTime: result.responseTime,
        operationId: result.operationId
      };

    } catch (error) {
      console.error('MongoDB operator error:', error);
      return { success: false, error: error.message };
    }
  }

  /**
   * Parse value from TuskLang format
   */
  parseValue(value) {
    if (typeof value === 'string') {
      // Remove quotes if present
      if ((value.startsWith('"') && value.endsWith('"')) || 
          (value.startsWith("'") && value.endsWith("'"))) {
        return value.slice(1, -1);
      }
      
      // Try to parse JSON
      try {
        return JSON.parse(value);
      } catch {
        return value;
      }
    }
    return value;
  }
}

module.exports = MongoDBOperator; 