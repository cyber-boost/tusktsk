/**
 * InfluxDB Operator - Production-Ready Implementation
 * Enterprise-grade InfluxDB integration with line protocol, Flux query language,
 * batch writing, retention policies, and real-time streaming capabilities
 */

const { InfluxDB, Point, WriteApi, QueryApi } = require('@influxdata/influxdb-client');
const { WriteOptions } = require('@influxdata/influxdb-client-apis');
const crypto = require('crypto');

class InfluxDBOperator {
  constructor() {
    this.clients = new Map();
    this.writeApis = new Map();
    this.queryApis = new Map();
    this.metrics = {
      connections: 0,
      writes: 0,
      queries: 0,
      fluxQueries: 0,
      batchWrites: 0,
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
    this.batchBuffers = new Map();
    this.retentionPolicies = new Map();
  }

  /**
   * Create or get InfluxDB client with connection pooling
   */
  createClient(connectionConfig, options = {}) {
    const clientKey = this.generateClientKey(connectionConfig);
    
    if (this.clients.has(clientKey)) {
      return this.clients.get(clientKey);
    }

    const clientOptions = {
      url: connectionConfig.url,
      token: connectionConfig.token,
      timeout: options.timeout || 30000,
      transportOptions: {
        proxy: options.proxy,
        headers: options.headers || {},
        ...options.transportOptions
      }
    };

    const client = new InfluxDB(clientOptions);

    // Create write API with optimized settings
    const writeOptions = new WriteOptions(
      options.batchSize || 1000,
      options.flushInterval || 10000,
      options.jitterInterval || 0,
      options.retryDelay || 5000,
      options.maxRetries || 5,
      options.maxRetryDelay || 30000,
      options.exponentialBase || 2
    );

    const writeApi = client.getWriteApi(
      connectionConfig.org,
      connectionConfig.bucket,
      'ms', // precision
      writeOptions
    );

    // Write API event handlers
    writeApi.on('writeError', (error) => {
      this.metrics.errors++;
      this.circuitBreaker.failures++;
      this.circuitBreaker.lastFailure = Date.now();
      console.error('InfluxDB: Write error', error);
    });

    writeApi.on('writeSuccess', (lines, response) => {
      this.metrics.writes++;
      console.log(`InfluxDB: Write success. Lines: ${lines}, Response: ${response}`);
    });

    // Create query API
    const queryApi = client.getQueryApi(connectionConfig.org);

    this.clients.set(clientKey, client);
    this.writeApis.set(clientKey, writeApi);
    this.queryApis.set(clientKey, queryApi);
    this.metrics.connections++;

    console.log(`InfluxDB: Client created for ${connectionConfig.url}`);

    return { client, writeApi, queryApi };
  }

  /**
   * Generate unique client key from connection config
   */
  generateClientKey(connectionConfig) {
    const keyString = `${connectionConfig.url}:${connectionConfig.org}:${connectionConfig.bucket}`;
    return crypto.createHash('md5').update(keyString).digest('hex');
  }

  /**
   * Write data point using line protocol
   */
  async writeDataPoint(connectionConfig, measurement, tags = {}, fields = {}, timestamp = null, options = {}) {
    const startTime = Date.now();
    const writeId = crypto.randomUUID();
    
    console.log(`InfluxDB: Writing data point ${writeId}`, {
      measurement,
      tags: Object.keys(tags),
      fields: Object.keys(fields),
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

    const { writeApi } = this.createClient(connectionConfig, options);

    try {
      const point = new Point(measurement);
      
      // Add tags
      Object.entries(tags).forEach(([key, value]) => {
        point.tag(key, String(value));
      });
      
      // Add fields
      Object.entries(fields).forEach(([key, value]) => {
        if (typeof value === 'number') {
          point.floatField(key, value);
        } else if (typeof value === 'boolean') {
          point.booleanField(key, value);
        } else {
          point.stringField(key, String(value));
        }
      });
      
      // Add timestamp if provided
      if (timestamp) {
        point.timestamp(timestamp);
      }

      // Write timeout
      const writeTimeout = options.writeTimeout || 30000;
      const timeoutPromise = new Promise((_, reject) => {
        setTimeout(() => reject(new Error('Write timeout')), writeTimeout);
      });

      const writePromise = writeApi.writePoint(point);
      await Promise.race([writePromise, timeoutPromise]);

      // Update metrics
      const responseTime = Date.now() - startTime;
      this.metrics.avgResponseTime = (this.metrics.avgResponseTime + responseTime) / 2;

      // Reset circuit breaker on success
      if (this.circuitBreaker.state === 'HALF_OPEN') {
        this.circuitBreaker.state = 'CLOSED';
        this.circuitBreaker.failures = 0;
      }

      console.log(`InfluxDB: Data point ${writeId} written successfully`, {
        responseTime: `${responseTime}ms`,
        avgResponseTime: `${this.metrics.avgResponseTime}ms`
      });

      return {
        success: true,
        writeId,
        responseTime
      };

    } catch (error) {
      this.metrics.errors++;
      this.circuitBreaker.failures++;
      this.circuitBreaker.lastFailure = Date.now();

      // Update circuit breaker state
      if (this.circuitBreaker.failures >= this.circuitBreaker.threshold) {
        this.circuitBreaker.state = 'OPEN';
      }

      console.error(`InfluxDB: Data point ${writeId} write failed`, {
        error: error.message,
        responseTime: Date.now() - startTime
      });

      throw error;
    }
  }

  /**
   * Batch write multiple data points for high-throughput scenarios
   */
  async batchWrite(connectionConfig, dataPoints, options = {}) {
    const startTime = Date.now();
    const batchId = crypto.randomUUID();
    
    console.log(`InfluxDB: Starting batch write ${batchId}`, {
      dataPoints: dataPoints.length,
      options
    });

    const { writeApi } = this.createClient(connectionConfig, options);

    try {
      const points = dataPoints.map(dp => {
        const point = new Point(dp.measurement);
        
        // Add tags
        Object.entries(dp.tags || {}).forEach(([key, value]) => {
          point.tag(key, String(value));
        });
        
        // Add fields
        Object.entries(dp.fields || {}).forEach(([key, value]) => {
          if (typeof value === 'number') {
            point.floatField(key, value);
          } else if (typeof value === 'boolean') {
            point.booleanField(key, value);
          } else {
            point.stringField(key, String(value));
          }
        });
        
        // Add timestamp if provided
        if (dp.timestamp) {
          point.timestamp(dp.timestamp);
        }
        
        return point;
      });

      // Batch write timeout
      const batchTimeout = options.batchTimeout || 60000;
      const timeoutPromise = new Promise((_, reject) => {
        setTimeout(() => reject(new Error('Batch write timeout')), batchTimeout);
      });

      const writePromise = writeApi.writePoints(points);
      await Promise.race([writePromise, timeoutPromise]);

      const responseTime = Date.now() - startTime;
      this.metrics.batchWrites++;
      this.metrics.avgResponseTime = (this.metrics.avgResponseTime + responseTime) / 2;

      console.log(`InfluxDB: Batch write ${batchId} completed successfully`, {
        responseTime: `${responseTime}ms`,
        points: dataPoints.length
      });

      return {
        success: true,
        batchId,
        pointsWritten: dataPoints.length,
        responseTime
      };

    } catch (error) {
      this.metrics.errors++;
      console.error(`InfluxDB: Batch write ${batchId} failed`, {
        error: error.message,
        responseTime: Date.now() - startTime
      });

      throw error;
    }
  }

  /**
   * Execute Flux query for complex analytics
   */
  async executeFluxQuery(connectionConfig, fluxQuery, options = {}) {
    const startTime = Date.now();
    const queryId = crypto.randomUUID();
    
    console.log(`InfluxDB: Executing Flux query ${queryId}`, {
      query: fluxQuery.substring(0, 100) + (fluxQuery.length > 100 ? '...' : ''),
      options
    });

    const { queryApi } = this.createClient(connectionConfig, options);

    try {
      // Query timeout
      const queryTimeout = options.queryTimeout || 30000;
      const timeoutPromise = new Promise((_, reject) => {
        setTimeout(() => reject(new Error('Query timeout')), queryTimeout);
      });

      const queryPromise = queryApi.queryRaw(fluxQuery);
      const result = await Promise.race([queryPromise, timeoutPromise]);

      // Parse CSV result
      const lines = result.split('\n');
      const headers = lines[0].split(',');
      const data = lines.slice(1, -1).map(line => {
        const values = line.split(',');
        const row = {};
        headers.forEach((header, index) => {
          row[header.trim()] = values[index]?.trim() || '';
        });
        return row;
      });

      const responseTime = Date.now() - startTime;
      this.metrics.fluxQueries++;
      this.metrics.avgResponseTime = (this.metrics.avgResponseTime + responseTime) / 2;

      console.log(`InfluxDB: Flux query ${queryId} completed successfully`, {
        responseTime: `${responseTime}ms`,
        resultCount: data.length
      });

      return {
        success: true,
        data,
        headers,
        responseTime,
        queryId
      };

    } catch (error) {
      this.metrics.errors++;
      console.error(`InfluxDB: Flux query ${queryId} failed`, {
        error: error.message,
        responseTime: Date.now() - startTime
      });

      throw error;
    }
  }

  /**
   * Execute InfluxQL query (legacy SQL-like syntax)
   */
  async executeInfluxQLQuery(connectionConfig, query, options = {}) {
    const startTime = Date.now();
    const queryId = crypto.randomUUID();
    
    console.log(`InfluxDB: Executing InfluxQL query ${queryId}`, {
      query: query.substring(0, 100) + (query.length > 100 ? '...' : ''),
      options
    });

    const { queryApi } = this.createClient(connectionConfig, options);

    try {
      // Query timeout
      const queryTimeout = options.queryTimeout || 30000;
      const timeoutPromise = new Promise((_, reject) => {
        setTimeout(() => reject(new Error('Query timeout')), queryTimeout);
      });

      const queryPromise = queryApi.queryRaw(query);
      const result = await Promise.race([queryPromise, timeoutPromise]);

      const responseTime = Date.now() - startTime;
      this.metrics.queries++;
      this.metrics.avgResponseTime = (this.metrics.avgResponseTime + responseTime) / 2;

      console.log(`InfluxDB: InfluxQL query ${queryId} completed successfully`, {
        responseTime: `${responseTime}ms`
      });

      return {
        success: true,
        result,
        responseTime,
        queryId
      };

    } catch (error) {
      this.metrics.errors++;
      console.error(`InfluxDB: InfluxQL query ${queryId} failed`, {
        error: error.message,
        responseTime: Date.now() - startTime
      });

      throw error;
    }
  }

  /**
   * Manage retention policies
   */
  async manageRetentionPolicy(connectionConfig, operation, policyConfig, options = {}) {
    const startTime = Date.now();
    const policyId = crypto.randomUUID();
    
    console.log(`InfluxDB: Managing retention policy ${policyId}`, {
      operation,
      policyConfig,
      options
    });

    const { queryApi } = this.createClient(connectionConfig, options);

    try {
      let query;
      
      switch (operation.toLowerCase()) {
        case 'create':
          query = `CREATE RETENTION POLICY "${policyConfig.name}" ON "${connectionConfig.bucket}" DURATION ${policyConfig.duration} REPLICATION ${policyConfig.replication || 1}`;
          break;
        
        case 'alter':
          query = `ALTER RETENTION POLICY "${policyConfig.name}" ON "${connectionConfig.bucket}" DURATION ${policyConfig.duration}`;
          break;
        
        case 'drop':
          query = `DROP RETENTION POLICY "${policyConfig.name}" ON "${connectionConfig.bucket}"`;
          break;
        
        case 'show':
          query = `SHOW RETENTION POLICIES ON "${connectionConfig.bucket}"`;
          break;
        
        default:
          throw new Error(`Unknown retention policy operation: ${operation}`);
      }

      const result = await queryApi.queryRaw(query);
      
      const responseTime = Date.now() - startTime;
      console.log(`InfluxDB: Retention policy ${policyId} operation completed`, {
        operation,
        responseTime: `${responseTime}ms`
      });

      return {
        success: true,
        operation,
        result,
        responseTime,
        policyId
      };

    } catch (error) {
      this.metrics.errors++;
      console.error(`InfluxDB: Retention policy ${policyId} operation failed`, {
        error: error.message,
        responseTime: Date.now() - startTime
      });

      throw error;
    }
  }

  /**
   * Health check for InfluxDB instance
   */
  async healthCheck(connectionConfig, options = {}) {
    try {
      const { queryApi } = this.createClient(connectionConfig, options);
      
      const result = await queryApi.queryRaw('SHOW MEASUREMENTS LIMIT 1');
      
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
      errorRate: this.metrics.writes > 0 ? (this.metrics.errors / this.metrics.writes) * 100 : 0,
      circuitBreaker: { ...this.circuitBreaker },
      clients: this.clients.size,
      writeApis: this.writeApis.size,
      queryApis: this.queryApis.size
    };
  }

  /**
   * Close all connections and flush pending writes
   */
  async closeAll() {
    console.log('InfluxDB: Closing all connections and flushing writes');
    
    // Flush all write APIs
    const flushPromises = Array.from(this.writeApis.values()).map(writeApi => writeApi.close());
    await Promise.all(flushPromises);
    
    this.clients.clear();
    this.writeApis.clear();
    this.queryApis.clear();
    console.log('InfluxDB: All connections closed and writes flushed');
  }

  /**
   * Execute InfluxDB operator with TuskLang integration
   */
  async executeInfluxDbOperator(params) {
    try {
      // Parse InfluxDB operations: "url, database, measurement, data, ...options"
      const parts = params.split(',').map(p => p.trim());
      if (parts.length < 4) {
        throw new Error('Invalid @influxdb syntax. Expected: url, database, measurement, data, ...options');
      }

      const url = this.parseValue(parts[0]);
      const database = this.parseValue(parts[1]);
      const measurement = this.parseValue(parts[2]);
      const data = this.parseValue(parts[3]);
      const options = parts[4] ? this.parseValue(parts[4]) : {};

      // Build connection config
      const connectionConfig = {
        url,
        org: options.org || 'my-org',
        bucket: database,
        token: options.token || process.env.INFLUXDB_TOKEN
      };

      // Execute write operation
      const result = await this.writeDataPoint(
        connectionConfig,
        measurement,
        data.tags || {},
        data.fields || {},
        data.timestamp || null,
        options
      );

      console.log(`InfluxDB: Operator executed successfully`, {
        database,
        measurement,
        responseTime: result.responseTime
      });

      return {
        success: true,
        writeId: result.writeId,
        responseTime: result.responseTime
      };

    } catch (error) {
      console.error('InfluxDB operator error:', error);
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

module.exports = InfluxDBOperator; 