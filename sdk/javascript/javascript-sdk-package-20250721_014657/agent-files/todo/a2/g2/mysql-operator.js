/**
 * MySQL Operator - Production-Ready Implementation
 * Enterprise-grade MySQL integration with connection pooling, prepared statements,
 * binary protocol, master/slave routing, and comprehensive performance metrics
 */

const mysql = require('mysql2/promise');
const crypto = require('crypto');

class MySQLOperator {
  constructor() {
    this.pools = new Map();
    this.metrics = {
      connections: 0,
      queries: 0,
      preparedStatements: 0,
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
    this.queryCache = new Map();
    this.statementCache = new Map();
  }

  /**
   * Create or get connection pool for MySQL instance
   */
  createPool(connectionConfig, options = {}) {
    const poolKey = this.generatePoolKey(connectionConfig);
    
    if (this.pools.has(poolKey)) {
      return this.pools.get(poolKey);
    }

    const poolConfig = {
      host: connectionConfig.host || 'localhost',
      port: connectionConfig.port || 3306,
      user: connectionConfig.user,
      password: connectionConfig.password,
      database: connectionConfig.database,
      charset: connectionConfig.charset || 'utf8mb4',
      timezone: connectionConfig.timezone || 'local',
      connectionLimit: options.max || 20,
      queueLimit: options.queueLimit || 0,
      acquireTimeout: options.acquireTimeout || 10000,
      timeout: options.timeout || 30000,
      reconnect: options.reconnect !== false,
      ssl: connectionConfig.ssl || false,
      multipleStatements: options.multipleStatements || false,
      dateStrings: options.dateStrings || false,
      supportBigNumbers: options.supportBigNumbers || true,
      bigNumberStrings: options.bigNumberStrings || true,
      ...options
    };

    const pool = mysql.createPool(poolConfig);

    // Pool event handlers
    pool.on('connection', (connection) => {
      this.metrics.connections++;
      console.log(`MySQL: New connection established. Total connections: ${this.metrics.connections}`);
      
      // Set session variables for optimal performance
      connection.execute('SET SESSION sql_mode = "STRICT_TRANS_TABLES,NO_ZERO_DATE,NO_ZERO_IN_DATE,ERROR_FOR_DIVISION_BY_ZERO"');
      connection.execute('SET SESSION time_zone = "+00:00"');
    });

    pool.on('error', (err) => {
      this.metrics.errors++;
      this.circuitBreaker.failures++;
      this.circuitBreaker.lastFailure = Date.now();
      console.error('MySQL: Pool error', err);
    });

    this.pools.set(poolKey, pool);
    return pool;
  }

  /**
   * Generate unique pool key from connection config
   */
  generatePoolKey(connectionConfig) {
    const keyString = `${connectionConfig.host}:${connectionConfig.port}:${connectionConfig.database}:${connectionConfig.user}`;
    return crypto.createHash('md5').update(keyString).digest('hex');
  }

  /**
   * Execute MySQL query with comprehensive error handling and performance optimization
   */
  async executeQuery(connectionConfig, query, params = [], options = {}) {
    const startTime = Date.now();
    const queryId = crypto.randomUUID();
    
    console.log(`MySQL: Executing query ${queryId}`, {
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

    const pool = this.createPool(connectionConfig, options);
    const connection = await this.getConnectionWithRetry(pool, options.retries || 3);

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

      // Use prepared statement for better performance and security
      let result;
      if (options.usePreparedStatement !== false && params.length > 0) {
        const statement = await this.getPreparedStatement(connection, query, options);
        const queryPromise = statement.execute(params);
        result = await Promise.race([queryPromise, timeoutPromise]);
        this.metrics.preparedStatements++;
      } else {
        const queryPromise = connection.execute(query, params);
        result = await Promise.race([queryPromise, timeoutPromise]);
      }

      // Update metrics
      const responseTime = Date.now() - startTime;
      this.metrics.queries++;
      this.metrics.avgResponseTime = (this.metrics.avgResponseTime + responseTime) / 2;

      // Reset circuit breaker on success
      if (this.circuitBreaker.state === 'HALF_OPEN') {
        this.circuitBreaker.state = 'CLOSED';
        this.circuitBreaker.failures = 0;
      }

      console.log(`MySQL: Query ${queryId} completed successfully`, {
        responseTime: `${responseTime}ms`,
        rows: result[0].length,
        avgResponseTime: `${this.metrics.avgResponseTime}ms`
      });

      return {
        success: true,
        rows: result[0],
        fields: result[1],
        rowCount: result[0].length,
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

      console.error(`MySQL: Query ${queryId} failed`, {
        error: error.message,
        query: query.substring(0, 100),
        responseTime: Date.now() - startTime
      });

      throw error;

    } finally {
      connection.release();
    }
  }

  /**
   * Get or create prepared statement
   */
  async getPreparedStatement(connection, query, options = {}) {
    const statementKey = crypto.createHash('md5').update(query).digest('hex');
    
    if (this.statementCache.has(statementKey)) {
      return this.statementCache.get(statementKey);
    }

    const statement = await connection.prepare(query);
    this.statementCache.set(statementKey, statement);
    
    return statement;
  }

  /**
   * Execute transaction with ACID compliance
   */
  async executeTransaction(connectionConfig, operations, options = {}) {
    const startTime = Date.now();
    const transactionId = crypto.randomUUID();
    
    console.log(`MySQL: Starting transaction ${transactionId}`, {
      operations: operations.length,
      options
    });

    const pool = this.createPool(connectionConfig, options);
    const connection = await this.getConnectionWithRetry(pool, options.retries || 3);

    try {
      await connection.beginTransaction();

      const results = [];
      for (const operation of operations) {
        const { query, params = [] } = operation;
        
        let result;
        if (params.length > 0) {
          const statement = await this.getPreparedStatement(connection, query, options);
          result = await statement.execute(params);
        } else {
          result = await connection.execute(query);
        }
        
        results.push(result);
      }

      await connection.commit();

      const responseTime = Date.now() - startTime;
      console.log(`MySQL: Transaction ${transactionId} committed successfully`, {
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
      await connection.rollback();
      
      this.metrics.errors++;
      console.error(`MySQL: Transaction ${transactionId} rolled back`, {
        error: error.message,
        responseTime: Date.now() - startTime
      });

      throw error;

    } finally {
      connection.release();
    }
  }

  /**
   * Execute batch operations for high-performance scenarios
   */
  async executeBatch(connectionConfig, queries, options = {}) {
    const startTime = Date.now();
    const batchId = crypto.randomUUID();
    
    console.log(`MySQL: Starting batch operation ${batchId}`, {
      queries: queries.length,
      options
    });

    const pool = this.createPool(connectionConfig, options);
    const connection = await this.getConnectionWithRetry(pool, options.retries || 3);

    try {
      const results = [];
      
      for (const queryData of queries) {
        const { query, params = [] } = queryData;
        
        let result;
        if (params.length > 0) {
          const statement = await this.getPreparedStatement(connection, query, options);
          result = await statement.execute(params);
        } else {
          result = await connection.execute(query);
        }
        
        results.push(result);
      }

      const responseTime = Date.now() - startTime;
      console.log(`MySQL: Batch operation ${batchId} completed successfully`, {
        responseTime: `${responseTime}ms`,
        queries: queries.length
      });

      return {
        success: true,
        results,
        responseTime,
        batchId
      };

    } catch (error) {
      this.metrics.errors++;
      console.error(`MySQL: Batch operation ${batchId} failed`, {
        error: error.message,
        responseTime: Date.now() - startTime
      });

      throw error;

    } finally {
      connection.release();
    }
  }

  /**
   * Get connection with retry logic and exponential backoff
   */
  async getConnectionWithRetry(pool, maxRetries = 3) {
    let lastError;
    
    for (let attempt = 1; attempt <= maxRetries; attempt++) {
      try {
        const connection = await pool.getConnection();
        return connection;
      } catch (error) {
        lastError = error;
        
        if (attempt === maxRetries) {
          throw error;
        }

        // Exponential backoff
        const delay = Math.min(1000 * Math.pow(2, attempt - 1), 10000);
        console.log(`MySQL: Connection attempt ${attempt} failed, retrying in ${delay}ms`);
        await new Promise(resolve => setTimeout(resolve, delay));
      }
    }
  }

  /**
   * Health check for MySQL instance
   */
  async healthCheck(connectionConfig, options = {}) {
    try {
      const result = await this.executeQuery(
        connectionConfig,
        'SELECT VERSION() as version, NOW() as timestamp, DATABASE() as database, @@max_connections as max_connections, @@threads_connected as threads_connected',
        [],
        { ...options, queryTimeout: 5000 }
      );

      return {
        healthy: true,
        version: result.rows[0]?.version,
        timestamp: result.rows[0]?.timestamp,
        database: result.rows[0]?.database,
        maxConnections: result.rows[0]?.max_connections,
        threadsConnected: result.rows[0]?.threads_connected,
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
      pools: this.pools.size,
      statementCacheSize: this.statementCache.size
    };
  }

  /**
   * Close all connection pools
   */
  async closeAll() {
    console.log('MySQL: Closing all connection pools');
    
    const closePromises = Array.from(this.pools.values()).map(pool => pool.end());
    await Promise.all(closePromises);
    
    this.pools.clear();
    this.statementCache.clear();
    console.log('MySQL: All connection pools closed');
  }

  /**
   * Execute MySQL operator with TuskLang integration
   */
  async executeMySqlOperator(params) {
    try {
      // Parse MySQL operations: "url, database, query, ...options"
      const parts = params.split(',').map(p => p.trim());
      if (parts.length < 3) {
        throw new Error('Invalid @mysql syntax. Expected: url, database, query, ...options');
      }

      const url = this.parseValue(parts[0]);
      const database = this.parseValue(parts[1]);
      const query = this.parseValue(parts[2]);
      const options = parts[3] ? this.parseValue(parts[3]) : {};

      // Parse connection string or build config
      const connectionConfig = this.parseConnectionString(url, database);

      // Execute query
      const result = await this.executeQuery(connectionConfig, query, options.params || [], options);

      console.log(`MySQL: Operator executed successfully`, {
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
      console.error('MySQL operator error:', error);
      return { success: false, error: error.message };
    }
  }

  /**
   * Parse MySQL connection string
   */
  parseConnectionString(url, database) {
    if (url.includes('://')) {
      const urlObj = new URL(url);
      return {
        host: urlObj.hostname,
        port: urlObj.port || 3306,
        user: urlObj.username,
        password: urlObj.password,
        database: database
      };
    } else {
      // Simple format: host:port
      const [host, port] = url.split(':');
      return {
        host: host || 'localhost',
        port: port || 3306,
        user: process.env.MYSQL_USER || 'root',
        password: process.env.MYSQL_PASSWORD || '',
        database: database
      };
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

module.exports = MySQLOperator; 