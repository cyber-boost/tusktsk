/**
 * Elasticsearch Operator - Production-Ready Implementation
 * Enterprise-grade Elasticsearch integration with full-text search, index management,
 * bulk operations, aggregations framework, and comprehensive cluster monitoring
 */

const { Client } = require('@elastic/elasticsearch');
const crypto = require('crypto');

class ElasticsearchOperator {
  constructor() {
    this.clients = new Map();
    this.metrics = {
      connections: 0,
      searches: 0,
      indexings: 0,
      bulkOperations: 0,
      aggregations: 0,
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
    this.indexTemplates = new Map();
    this.bulkBuffers = new Map();
  }

  /**
   * Create or get Elasticsearch client with connection pooling
   */
  createClient(connectionConfig, options = {}) {
    const clientKey = this.generateClientKey(connectionConfig);
    
    if (this.clients.has(clientKey)) {
      return this.clients.get(clientKey);
    }

    const clientOptions = {
      node: connectionConfig.node || connectionConfig.url,
      auth: connectionConfig.auth ? {
        username: connectionConfig.auth.username,
        password: connectionConfig.auth.password
      } : undefined,
      apiKey: connectionConfig.apiKey,
      ssl: connectionConfig.ssl !== false ? {
        rejectUnauthorized: connectionConfig.ssl?.rejectUnauthorized !== false
      } : false,
      maxRetries: options.maxRetries || 3,
      requestTimeout: options.requestTimeout || 30000,
      sniffOnStart: options.sniffOnStart || false,
      sniffInterval: options.sniffInterval || false,
      sniffOnConnectionFault: options.sniffOnConnectionFault || false,
      compression: options.compression !== false,
      ...options
    };

    const client = new Client(clientOptions);

    // Client event handlers
    client.on('response', (err, result) => {
      if (err) {
        this.metrics.errors++;
        this.circuitBreaker.failures++;
        this.circuitBreaker.lastFailure = Date.now();
        console.error('Elasticsearch: Response error', err);
      } else {
        console.log(`Elasticsearch: Response received. Status: ${result.statusCode}`);
      }
    });

    this.clients.set(clientKey, client);
    this.metrics.connections++;

    console.log(`Elasticsearch: Client created for ${connectionConfig.node || connectionConfig.url}`);

    return client;
  }

  /**
   * Generate unique client key from connection config
   */
  generateClientKey(connectionConfig) {
    const keyString = `${connectionConfig.node || connectionConfig.url}:${connectionConfig.auth?.username || 'anonymous'}`;
    return crypto.createHash('md5').update(keyString).digest('hex');
  }

  /**
   * Execute full-text search with relevance scoring and faceting
   */
  async executeSearch(connectionConfig, index, searchQuery, options = {}) {
    const startTime = Date.now();
    const searchId = crypto.randomUUID();
    
    console.log(`Elasticsearch: Executing search ${searchId}`, {
      index,
      queryType: typeof searchQuery,
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

    const client = this.createClient(connectionConfig, options);

    try {
      // Search timeout
      const searchTimeout = options.searchTimeout || 30000;
      const timeoutPromise = new Promise((_, reject) => {
        setTimeout(() => reject(new Error('Search timeout')), searchTimeout);
      });

      const searchPromise = client.search({
        index,
        body: searchQuery,
        size: options.size || 10,
        from: options.from || 0,
        sort: options.sort,
        aggs: options.aggregations,
        highlight: options.highlight,
        suggest: options.suggest,
        timeout: `${searchTimeout}ms`
      });

      const result = await Promise.race([searchPromise, timeoutPromise]);

      // Update metrics
      const responseTime = Date.now() - startTime;
      this.metrics.searches++;
      this.metrics.avgResponseTime = (this.metrics.avgResponseTime + responseTime) / 2;

      // Reset circuit breaker on success
      if (this.circuitBreaker.state === 'HALF_OPEN') {
        this.circuitBreaker.state = 'CLOSED';
        this.circuitBreaker.failures = 0;
      }

      console.log(`Elasticsearch: Search ${searchId} completed successfully`, {
        responseTime: `${responseTime}ms`,
        hits: result.body.hits.total.value,
        avgResponseTime: `${this.metrics.avgResponseTime}ms`
      });

      return {
        success: true,
        hits: result.body.hits.hits,
        total: result.body.hits.total,
        aggregations: result.body.aggregations,
        suggest: result.body.suggest,
        responseTime,
        searchId
      };

    } catch (error) {
      this.metrics.errors++;
      this.circuitBreaker.failures++;
      this.circuitBreaker.lastFailure = Date.now();

      // Update circuit breaker state
      if (this.circuitBreaker.failures >= this.circuitBreaker.threshold) {
        this.circuitBreaker.state = 'OPEN';
      }

      console.error(`Elasticsearch: Search ${searchId} failed`, {
        error: error.message,
        responseTime: Date.now() - startTime
      });

      throw error;
    }
  }

  /**
   * Index document with mapping and settings
   */
  async indexDocument(connectionConfig, index, document, options = {}) {
    const startTime = Date.now();
    const indexId = crypto.randomUUID();
    
    console.log(`Elasticsearch: Indexing document ${indexId}`, {
      index,
      documentId: options.id,
      options
    });

    const client = this.createClient(connectionConfig, options);

    try {
      const indexParams = {
        index,
        body: document,
        id: options.id,
        refresh: options.refresh || 'wait_for',
        timeout: options.timeout || '30s'
      };

      const result = await client.index(indexParams);

      const responseTime = Date.now() - startTime;
      this.metrics.indexings++;
      this.metrics.avgResponseTime = (this.metrics.avgResponseTime + responseTime) / 2;

      console.log(`Elasticsearch: Document ${indexId} indexed successfully`, {
        responseTime: `${responseTime}ms`,
        documentId: result.body._id
      });

      return {
        success: true,
        documentId: result.body._id,
        responseTime,
        indexId
      };

    } catch (error) {
      this.metrics.errors++;
      console.error(`Elasticsearch: Document ${indexId} indexing failed`, {
        error: error.message,
        responseTime: Date.now() - startTime
      });

      throw error;
    }
  }

  /**
   * Execute bulk operations for high-performance indexing
   */
  async executeBulkOperation(connectionConfig, operations, options = {}) {
    const startTime = Date.now();
    const bulkId = crypto.randomUUID();
    
    console.log(`Elasticsearch: Executing bulk operation ${bulkId}`, {
      operations: operations.length,
      options
    });

    const client = this.createClient(connectionConfig, options);

    try {
      const body = [];
      operations.forEach(operation => {
        body.push(operation.action);
        if (operation.document) {
          body.push(operation.document);
        }
      });

      const result = await client.bulk({
        body,
        refresh: options.refresh || 'wait_for',
        timeout: options.timeout || '30s'
      });

      const responseTime = Date.now() - startTime;
      this.metrics.bulkOperations++;
      this.metrics.avgResponseTime = (this.metrics.avgResponseTime + responseTime) / 2;

      // Check for errors in bulk response
      const errors = result.body.items.filter(item => item.index?.error || item.update?.error || item.delete?.error);
      
      if (errors.length > 0) {
        console.warn(`Elasticsearch: Bulk operation ${bulkId} completed with ${errors.length} errors`);
      }

      console.log(`Elasticsearch: Bulk operation ${bulkId} completed successfully`, {
        responseTime: `${responseTime}ms`,
        operations: operations.length,
        errors: errors.length
      });

      return {
        success: true,
        result: result.body,
        errors,
        responseTime,
        bulkId
      };

    } catch (error) {
      this.metrics.errors++;
      console.error(`Elasticsearch: Bulk operation ${bulkId} failed`, {
        error: error.message,
        responseTime: Date.now() - startTime
      });

      throw error;
    }
  }

  /**
   * Execute aggregations for complex analytics
   */
  async executeAggregation(connectionConfig, index, aggregationQuery, options = {}) {
    const startTime = Date.now();
    const aggregationId = crypto.randomUUID();
    
    console.log(`Elasticsearch: Executing aggregation ${aggregationId}`, {
      index,
      aggregationType: Object.keys(aggregationQuery),
      options
    });

    const client = this.createClient(connectionConfig, options);

    try {
      const result = await client.search({
        index,
        body: {
          size: 0,
          aggs: aggregationQuery
        },
        timeout: options.timeout || '30s'
      });

      const responseTime = Date.now() - startTime;
      this.metrics.aggregations++;
      this.metrics.avgResponseTime = (this.metrics.avgResponseTime + responseTime) / 2;

      console.log(`Elasticsearch: Aggregation ${aggregationId} completed successfully`, {
        responseTime: `${responseTime}ms`
      });

      return {
        success: true,
        aggregations: result.body.aggregations,
        responseTime,
        aggregationId
      };

    } catch (error) {
      this.metrics.errors++;
      console.error(`Elasticsearch: Aggregation ${aggregationId} failed`, {
        error: error.message,
        responseTime: Date.now() - startTime
      });

      throw error;
    }
  }

  /**
   * Manage index with mapping templates and settings
   */
  async manageIndex(connectionConfig, operation, indexConfig, options = {}) {
    const startTime = Date.now();
    const indexOpId = crypto.randomUUID();
    
    console.log(`Elasticsearch: Managing index ${indexOpId}`, {
      operation,
      index: indexConfig.index,
      options
    });

    const client = this.createClient(connectionConfig, options);

    try {
      let result;
      
      switch (operation.toLowerCase()) {
        case 'create':
          result = await client.indices.create({
            index: indexConfig.index,
            body: {
              settings: indexConfig.settings || {},
              mappings: indexConfig.mappings || {}
            }
          });
          break;
        
        case 'delete':
          result = await client.indices.delete({
            index: indexConfig.index
          });
          break;
        
        case 'exists':
          result = await client.indices.exists({
            index: indexConfig.index
          });
          break;
        
        case 'get':
          result = await client.indices.get({
            index: indexConfig.index
          });
          break;
        
        case 'put_mapping':
          result = await client.indices.putMapping({
            index: indexConfig.index,
            body: indexConfig.mapping
          });
          break;
        
        case 'put_settings':
          result = await client.indices.putSettings({
            index: indexConfig.index,
            body: indexConfig.settings
          });
          break;
        
        default:
          throw new Error(`Unknown index operation: ${operation}`);
      }

      const responseTime = Date.now() - startTime;
      console.log(`Elasticsearch: Index operation ${indexOpId} completed successfully`, {
        operation,
        responseTime: `${responseTime}ms`
      });

      return {
        success: true,
        operation,
        result: result.body,
        responseTime,
        indexOpId
      };

    } catch (error) {
      this.metrics.errors++;
      console.error(`Elasticsearch: Index operation ${indexOpId} failed`, {
        error: error.message,
        responseTime: Date.now() - startTime
      });

      throw error;
    }
  }

  /**
   * Monitor cluster health and shard management
   */
  async getClusterHealth(connectionConfig, options = {}) {
    const startTime = Date.now();
    const healthId = crypto.randomUUID();
    
    console.log(`Elasticsearch: Getting cluster health ${healthId}`);

    const client = this.createClient(connectionConfig, options);

    try {
      const [healthResult, statsResult, nodesResult] = await Promise.all([
        client.cluster.health(),
        client.cluster.stats(),
        client.nodes.stats()
      ]);

      const responseTime = Date.now() - startTime;

      console.log(`Elasticsearch: Cluster health ${healthId} retrieved successfully`, {
        responseTime: `${responseTime}ms`,
        status: healthResult.body.status
      });

      return {
        success: true,
        health: healthResult.body,
        stats: statsResult.body,
        nodes: nodesResult.body,
        responseTime,
        healthId
      };

    } catch (error) {
      this.metrics.errors++;
      console.error(`Elasticsearch: Cluster health ${healthId} failed`, {
        error: error.message,
        responseTime: Date.now() - startTime
      });

      throw error;
    }
  }

  /**
   * Health check for Elasticsearch instance
   */
  async healthCheck(connectionConfig, options = {}) {
    try {
      const client = this.createClient(connectionConfig, options);
      
      const result = await client.ping();
      
      return {
        healthy: true,
        responseTime: Date.now(),
        timestamp: new Date().toISOString()
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
      errorRate: this.metrics.searches > 0 ? (this.metrics.errors / this.metrics.searches) * 100 : 0,
      circuitBreaker: { ...this.circuitBreaker },
      clients: this.clients.size,
      indexTemplates: this.indexTemplates.size
    };
  }

  /**
   * Close all connections
   */
  async closeAll() {
    console.log('Elasticsearch: Closing all connections');
    
    // Close all clients
    const closePromises = Array.from(this.clients.values()).map(client => client.close());
    await Promise.all(closePromises);
    
    this.clients.clear();
    console.log('Elasticsearch: All connections closed');
  }

  /**
   * Execute Elasticsearch operator with TuskLang integration
   */
  async executeElasticsearchOperator(params) {
    try {
      // Parse Elasticsearch operations: "url, index, document, ...options"
      const parts = params.split(',').map(p => p.trim());
      if (parts.length < 3) {
        throw new Error('Invalid @elasticsearch syntax. Expected: url, index, document, ...options');
      }

      const url = this.parseValue(parts[0]);
      const index = this.parseValue(parts[1]);
      const document = this.parseValue(parts[2]);
      const options = parts[3] ? this.parseValue(parts[3]) : {};

      // Build connection config
      const connectionConfig = {
        node: url,
        auth: options.auth,
        apiKey: options.apiKey,
        ssl: options.ssl
      };

      // Execute indexing operation
      const result = await this.indexDocument(connectionConfig, index, document, options);

      console.log(`Elasticsearch: Operator executed successfully`, {
        index,
        documentId: result.documentId,
        responseTime: result.responseTime
      });

      return {
        success: true,
        indexed: true,
        id: result.documentId,
        index: index,
        responseTime: result.responseTime
      };

    } catch (error) {
      console.error('Elasticsearch operator error:', error);
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

module.exports = ElasticsearchOperator; 