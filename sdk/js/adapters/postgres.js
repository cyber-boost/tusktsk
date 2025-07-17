/**
 * PostgreSQL Adapter for TuskLang
 * ================================
 * Enables @query operations with PostgreSQL databases
 */

class PostgreSQLAdapter {
  constructor(options = {}) {
    this.config = {
      host: options.host || 'localhost',
      port: options.port || 5432,
      database: options.database || options.name,
      user: options.user || 'postgres',
      password: options.password || '',
      ssl: options.ssl || false,
      max: options.max || 10,
      idleTimeoutMillis: options.idleTimeoutMillis || 30000,
      connectionTimeoutMillis: options.connectionTimeoutMillis || 2000,
    };
    
    this.pool = null;
    this.client = null;
    
    // Lazy load pg
    try {
      const { Pool } = require('pg');
      this.pool = new Pool(this.config);
      
      // Handle pool errors
      this.pool.on('error', (err, client) => {
        console.error('Unexpected PostgreSQL pool error', err);
      });
    } catch (error) {
      throw new Error('PostgreSQL adapter requires pg. Install it with: npm install pg');
    }
  }

  /**
   * Execute a query
   * @param {string} sql - SQL query
   * @param {array} params - Query parameters
   * @returns {any} Query result
   */
  async query(sql, params = []) {
    try {
      const normalized = sql.trim().toLowerCase();
      
      // For single connection mode
      if (this.client) {
        const result = await this.client.query(sql, params);
        return this.processResult(result, normalized);
      }
      
      // For pool mode
      const result = await this.pool.query(sql, params);
      return this.processResult(result, normalized);
      
    } catch (error) {
      console.error('PostgreSQL query error:', error);
      throw error;
    }
  }

  /**
   * Process query result based on query type
   */
  processResult(result, normalizedSql) {
    // SELECT queries
    if (normalizedSql.startsWith('select')) {
      // Aggregate functions - return single value
      if (normalizedSql.includes('count(') || 
          normalizedSql.includes('sum(') || 
          normalizedSql.includes('avg(') || 
          normalizedSql.includes('max(') || 
          normalizedSql.includes('min(')) {
        
        if (result.rows.length > 0 && result.fields.length === 1) {
          const firstRow = result.rows[0];
          const firstCol = Object.keys(firstRow)[0];
          return firstRow[firstCol];
        }
      }
      
      // LIMIT 1 - return single row
      if (normalizedSql.includes('limit 1')) {
        return result.rows[0] || null;
      }
      
      // Return all rows
      return result.rows;
    }
    
    // INSERT with RETURNING
    if (normalizedSql.startsWith('insert') && normalizedSql.includes('returning')) {
      return result.rows[0] || null;
    }
    
    // INSERT, UPDATE, DELETE - return affected rows
    if (normalizedSql.startsWith('insert') || 
        normalizedSql.startsWith('update') || 
        normalizedSql.startsWith('delete')) {
      return result.rowCount;
    }
    
    // Other queries
    return true;
  }

  /**
   * Get a client for transaction support
   */
  async getClient() {
    this.client = await this.pool.connect();
    return this.client;
  }

  /**
   * Release the client back to pool
   */
  releaseClient() {
    if (this.client) {
      this.client.release();
      this.client = null;
    }
  }

  /**
   * Begin a transaction
   */
  async beginTransaction() {
    if (!this.client) {
      await this.getClient();
    }
    await this.client.query('BEGIN');
  }

  /**
   * Commit a transaction
   */
  async commit() {
    if (this.client) {
      await this.client.query('COMMIT');
      this.releaseClient();
    }
  }

  /**
   * Rollback a transaction
   */
  async rollback() {
    if (this.client) {
      await this.client.query('ROLLBACK');
      this.releaseClient();
    }
  }

  /**
   * Close all connections
   */
  async close() {
    if (this.client) {
      this.releaseClient();
    }
    if (this.pool) {
      await this.pool.end();
    }
  }

  /**
   * Create test database and tables
   */
  async createTestData() {
    try {
      // Create tables
      await this.query(`
        CREATE TABLE IF NOT EXISTS users (
          id SERIAL PRIMARY KEY,
          name VARCHAR(255) NOT NULL,
          email VARCHAR(255) UNIQUE,
          active BOOLEAN DEFAULT true,
          last_seen TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
          created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
      `);
      
      await this.query(`
        CREATE TABLE IF NOT EXISTS plans (
          id SERIAL PRIMARY KEY,
          name VARCHAR(100) NOT NULL,
          rate_limit INTEGER DEFAULT 1000,
          price DECIMAL(10,2)
        )
      `);
      
      await this.query(`
        CREATE TABLE IF NOT EXISTS feature_flags (
          id SERIAL PRIMARY KEY,
          name VARCHAR(100) NOT NULL,
          enabled BOOLEAN DEFAULT false,
          description TEXT
        )
      `);
      
      await this.query(`
        CREATE TABLE IF NOT EXISTS settings (
          key VARCHAR(100) PRIMARY KEY,
          value TEXT,
          type VARCHAR(50) DEFAULT 'string'
        )
      `);
      
      // Insert sample data
      await this.query(`
        INSERT INTO users (name, email) VALUES 
        ($1, $2), ($3, $4), ($5, $6)
        ON CONFLICT (email) DO NOTHING
      `, ['John Doe', 'john@example.com', 'Jane Smith', 'jane@example.com', 'Bob Wilson', 'bob@example.com']);
      
      await this.query(`
        INSERT INTO plans (name, rate_limit, price) VALUES 
        ($1, $2, $3), ($4, $5, $6), ($7, $8, $9)
        ON CONFLICT DO NOTHING
      `, ['basic', 100, 9.99, 'premium', 1000, 29.99, 'enterprise', 10000, 99.99]);
      
      await this.query(`
        INSERT INTO feature_flags (name, enabled, description) VALUES 
        ($1, $2, $3), ($4, $5, $6), ($7, $8, $9)
        ON CONFLICT DO NOTHING
      `, [
        'new_dashboard', true, 'New dashboard UI',
        'api_v2', false, 'API version 2',
        'dark_mode', true, 'Dark mode theme'
      ]);
      
      await this.query(`
        INSERT INTO settings (key, value, type) VALUES 
        ($1, $2, $3), ($4, $5, $6), ($7, $8, $9)
        ON CONFLICT (key) DO UPDATE SET value = EXCLUDED.value
      `, [
        'max_users', '1000', 'integer',
        'app_name', 'TuskLang Demo', 'string',
        'maintenance_mode', 'false', 'boolean'
      ]);
      
      console.log('PostgreSQL test data created successfully');
      
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
      await this.query('SELECT 1');
      return true;
    } catch {
      return false;
    }
  }
}

module.exports = PostgreSQLAdapter;