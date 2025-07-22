/**
 * PostgreSQL Operator - Production-Ready Implementation
 * Enterprise-grade PostgreSQL integration with connection pooling, transactions,
 * parameterized queries, retry logic, and comprehensive monitoring
 */

const { Pool } = require('pg');
const crypto = require('crypto');

class PostgreSQLOperator {
  constructor() {
    this.pools = new Map();
    this.metrics = {
      connections: 0,
      queries: 0,
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
  }

  /**
   * Create or get connection pool for PostgreSQL instance
   */
  createPool(connectionString, options = {}) {
    const poolKey = this.generatePoolKey(connectionString);
    
    if (this.pools.has(poolKey)) {
      return this.pools.get(poolKey);
    }

    const poolConfig = {
      connectionString,
      max: options.max || 20,
      min: options.min || 5,
      idleTimeoutMillis: options.idleTimeout || 30000,
      connectionTimeoutMillis: options.connectionTimeout || 10000,
      query_timeout: options.queryTimeout || 30000,
      statement_timeout: options.statementTimeout || 30000,
      ssl: options.ssl !== false ? { rejectUnauthorized: false } : false,
      ...options
    };

    const pool = new Pool(poolConfig);

    // Pool event handlers
    pool.on('connect', (client) => {
      this.metrics.connections++;
      console.log(`PostgreSQL: New connection established. Total connections: ${this.metrics.connections}`);
    });

    pool.on('error', (err, client) => {
      this.metrics.errors++;
      this.circuitBreaker.failures++;
      this.circuitBreaker.lastFailure = Date.now();
      console.error('PostgreSQL: Unexpected error on idle client', err);
    });

    pool.on('remove', (client) => {
      this.metrics.connections = Math.max(0, this.metrics.connections - 1);
      console.log(`PostgreSQL: Client removed. Total connections: ${this.metrics.connections}`);
    });

    this.pools.set(poolKey, pool);
    return pool;
  }

  /**
   * Generate unique pool key from connection string
   */
  generatePoolKey(connectionString) {
    return crypto.createHash('md5').update(connectionString).digest('hex');
  }

  /**
   * Execute PostgreSQL query with comprehensive error handling and retry logic
   */
  async executeQuery(connectionString, query, params = [], options = {}) {
    const startTime = Date.now();
    const queryId = crypto.randomUUID();
    
    console.log(`PostgreSQL: Executing query ${queryId}`, {
      query: query.substring(0, 100) + (query.length > 100 ? '...' : ''),
      params: params.length,
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

    const pool = this.createPool(connectionString, options);
    const client = await this.getClientWithRetry(pool, options.retries || 3);

    try {
      // Parameter validation
      if (!Array.isArray(params)) {
        throw new Error('Query parameters must be an array');
      }

      // Query timeout
      const queryTimeout = options.queryTimeout || 30000;
      const timeoutPromise = new Promise((_, reject) => {
        setTimeout(() => reject(new Error('Query timeout')), queryTimeout);
      });

      // Execute query with timeout
      const queryPromise = client.query(query, params);
      const result = await Promise.race([queryPromise, timeoutPromise]);

      // Update metrics
      const responseTime = Date.now() - startTime;
      this.metrics.queries++;
      this.metrics.avgResponseTime = (this.metrics.avgResponseTime + responseTime) / 2;

      // Reset circuit breaker on success
      if (this.circuitBreaker.state === 'HALF_OPEN') {
        this.circuitBreaker.state = 'CLOSED';
        this.circuitBreaker.failures = 0;
      }

      console.log(`PostgreSQL: Query ${queryId} completed successfully`, {
        responseTime: `${responseTime}ms`,
        rows: result.rowCount,
        avgResponseTime: `${this.metrics.avgResponseTime}ms`
      });

      return {
        success: true,
        rows: result.rows,
        rowCount: result.rowCount,
        fields: result.fields,
        responseTime,
        queryId
      };

    } catch (error) {
      this.metrics.errors++;
      this.circuitBreaker.failures++;
      this.circuitBreaker.lastFailure = Date.now();

      // Update circuit breaker state
      if (this.circuitBreaker.failures >= this.circuitBreaker.threshold) {
        this.circuitBreaker.state = 'OPEN';
      }

      console.error(`PostgreSQL: Query ${queryId} failed`, {
        error: error.message,
        query: query.substring(0, 100),
        responseTime: Date.now() - startTime
      });

      throw error;

    } finally {
      client.release();
    }
  }

  /**
   * Execute transaction with ACID compliance
   */
  async executeTransaction(connectionString, operations, options = {}) {
    const startTime = Date.now();
    const transactionId = crypto.randomUUID();
    
    console.log(`PostgreSQL: Starting transaction ${transactionId}`, {
      operations: operations.length,
      options
    });

    const pool = this.createPool(connectionString, options);
    const client = await this.getClientWithRetry(pool, options.retries || 3);

    try {
      await client.query('BEGIN');

      const results = [];
      for (const operation of operations) {
        const { query, params = [] } = operation;
        const result = await client.query(query, params);
        results.push(result);
      }

      await client.query('COMMIT');

      const responseTime = Date.now() - startTime;
      console.log(`PostgreSQL: Transaction ${transactionId} committed successfully`, {
        responseTime: `${responseTime}ms`,
        operations: operations.length
      });

      return {
        success: true,
        results,
        responseTime,
        transactionId
      };

    } catch (error) {
      await client.query('ROLLBACK');
      
      this.metrics.errors++;
      console.error(`PostgreSQL: Transaction ${transactionId} rolled back`, {
        error: error.message,
        responseTime: Date.now() - startTime
      });

      throw error;

    } finally {
      client.release();
    }
  }

  /**
   * Get client with retry logic and exponential backoff
   */
  async getClientWithRetry(pool, maxRetries = 3) {
    let lastError;
    
    for (let attempt = 1; attempt <= maxRetries; attempt++) {
      try {
        const client = await pool.connect();
        return client;
      } catch (error) {
        lastError = error;
        
        if (attempt === maxRetries) {
          throw error;
        }

        // Exponential backoff
        const delay = Math.min(1000 * Math.pow(2, attempt - 1), 10000);
        console.log(`PostgreSQL: Connection attempt ${attempt} failed, retrying in ${delay}ms`);
        await new Promise(resolve => setTimeout(resolve, delay));
      }
    }
  }

  /**
   * Health check for PostgreSQL instance
   */
  async healthCheck(connectionString, options = {}) {
    try {
      const result = await this.executeQuery(
        connectionString,
        'SELECT version(), current_timestamp, current_database()',
        [],
        { ...options, queryTimeout: 5000 }
      );

      return {
        healthy: true,
        version: result.rows[0]?.version,
        timestamp: result.rows[0]?.current_timestamp,
        database: result.rows[0]?.current_database,
        responseTime: result.responseTime
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
      errorRate: this.metrics.queries > 0 ? (this.metrics.errors / this.metrics.queries) * 100 : 0,
      circuitBreaker: { ...this.circuitBreaker },
      pools: this.pools.size
    };
  }

  /**
   * Close all connection pools
   */
  async closeAll() {
    console.log('PostgreSQL: Closing all connection pools');
    
    const closePromises = Array.from(this.pools.values()).map(pool => pool.end());
    await Promise.all(closePromises);
    
    this.pools.clear();
    console.log('PostgreSQL: All connection pools closed');
  }

  /**
   * Execute PostgreSQL operator with TuskLang integration
   */
  async executePostgreSqlOperator(params) {
    try {
      // Parse PostgreSQL operations: "url, database, query, ...options"
      const parts = params.split(',').map(p => p.trim());
      if (parts.length < 3) {
        throw new Error('Invalid @postgresql syntax. Expected: url, database, query, ...options');
      }

      const url = this.parseValue(parts[0]);
      const database = this.parseValue(parts[1]);
      const query = this.parseValue(parts[2]);
      const options = parts[3] ? this.parseValue(parts[3]) : {};

      // Build connection string if needed
      let connectionString = url;
      if (!url.includes('://')) {
        connectionString = `postgresql://${url}/${database}`;
      }

      // Execute query
      const result = await this.executeQuery(connectionString, query, options.params || [], options);

      console.log(`PostgreSQL: Operator executed successfully`, {
        database,
        query: query.substring(0, 100) + (query.length > 100 ? '...' : ''),
        rowCount: result.rowCount,
        responseTime: result.responseTime
      });

      return {
        success: true,
        rows: result.rows,
        rowCount: result.rowCount,
        responseTime: result.responseTime,
        queryId: result.queryId
      };

    } catch (error) {
      console.error('PostgreSQL operator error:', error);
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

module.exports = PostgreSQLOperator; 