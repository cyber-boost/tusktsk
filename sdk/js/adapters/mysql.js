/**
 * MySQL Adapter for TuskLang
 * ==========================
 * Enables @query operations with MySQL/MariaDB databases
 * 
 * DEFAULT CONFIG: peanut.tsk (the bridge of language grace)
 */

class MySQLAdapter {
  constructor(options = {}) {
    this.config = {
      host: options.host || 'localhost',
      port: options.port || 3306,
      database: options.database || options.name,
      user: options.user || 'root',
      password: options.password || '',
      waitForConnections: options.waitForConnections ?? true,
      connectionLimit: options.connectionLimit || 10,
      queueLimit: options.queueLimit || 0,
      enableKeepAlive: options.enableKeepAlive ?? true,
      keepAliveInitialDelay: options.keepAliveInitialDelay || 0,
      timezone: options.timezone || 'local',
      charset: options.charset || 'utf8mb4'
    };
    
    this.pool = null;
    this.connection = null;
    
    // Lazy load mysql2
    try {
      const mysql = require('mysql2/promise');
      this.pool = mysql.createPool(this.config);
    } catch (error) {
      throw new Error('MySQL adapter requires mysql2. Install it with: npm install mysql2');
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
      
      // Use connection if in transaction, otherwise use pool
      const executor = this.connection || this.pool;
      const [rows, fields] = await executor.execute(sql, params);
      
      return this.processResult(rows, fields, normalized);
      
    } catch (error) {
      console.error('MySQL query error:', error);
      throw error;
    }
  }

  /**
   * Process query result based on query type
   */
  processResult(rows, fields, normalizedSql) {
    // SELECT queries
    if (normalizedSql.startsWith('select')) {
      // Aggregate functions - return single value
      if (normalizedSql.includes('count(') || 
          normalizedSql.includes('sum(') || 
          normalizedSql.includes('avg(') || 
          normalizedSql.includes('max(') || 
          normalizedSql.includes('min(')) {
        
        if (rows.length > 0 && Object.keys(rows[0]).length === 1) {
          const firstCol = Object.keys(rows[0])[0];
          return rows[0][firstCol];
        }
      }
      
      // LIMIT 1 - return single row
      if (normalizedSql.includes('limit 1')) {
        return rows[0] || null;
      }
      
      // Return all rows
      return rows;
    }
    
    // INSERT, UPDATE, DELETE - return affected rows
    if (normalizedSql.startsWith('insert') || 
        normalizedSql.startsWith('update') || 
        normalizedSql.startsWith('delete')) {
      return rows.affectedRows;
    }
    
    // INSERT with auto-increment ID
    if (normalizedSql.startsWith('insert') && rows.insertId) {
      return rows.insertId;
    }
    
    // Other queries
    return true;
  }

  /**
   * Get a connection for transaction support
   */
  async getConnection() {
    this.connection = await this.pool.getConnection();
    return this.connection;
  }

  /**
   * Release the connection back to pool
   */
  releaseConnection() {
    if (this.connection) {
      this.connection.release();
      this.connection = null;
    }
  }

  /**
   * Begin a transaction
   */
  async beginTransaction() {
    if (!this.connection) {
      await this.getConnection();
    }
    await this.connection.beginTransaction();
  }

  /**
   * Commit a transaction
   */
  async commit() {
    if (this.connection) {
      await this.connection.commit();
      this.releaseConnection();
    }
  }

  /**
   * Rollback a transaction
   */
  async rollback() {
    if (this.connection) {
      await this.connection.rollback();
      this.releaseConnection();
    }
  }

  /**
   * Close all connections
   */
  async close() {
    if (this.connection) {
      this.releaseConnection();
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
          id INT AUTO_INCREMENT PRIMARY KEY,
          name VARCHAR(255) NOT NULL,
          email VARCHAR(255) UNIQUE,
          active BOOLEAN DEFAULT true,
          last_seen TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
          created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
      `);
      
      await this.query(`
        CREATE TABLE IF NOT EXISTS plans (
          id INT AUTO_INCREMENT PRIMARY KEY,
          name VARCHAR(100) NOT NULL,
          rate_limit INT DEFAULT 1000,
          price DECIMAL(10,2)
        )
      `);
      
      await this.query(`
        CREATE TABLE IF NOT EXISTS feature_flags (
          id INT AUTO_INCREMENT PRIMARY KEY,
          name VARCHAR(100) NOT NULL,
          enabled BOOLEAN DEFAULT false,
          description TEXT
        )
      `);
      
      await this.query(`
        CREATE TABLE IF NOT EXISTS settings (
          \`key\` VARCHAR(100) PRIMARY KEY,
          value TEXT,
          type VARCHAR(50) DEFAULT 'string'
        )
      `);
      
      // Insert sample data with INSERT IGNORE
      await this.query(`
        INSERT IGNORE INTO users (name, email) VALUES 
        (?, ?), (?, ?), (?, ?)
      `, ['John Doe', 'john@example.com', 'Jane Smith', 'jane@example.com', 'Bob Wilson', 'bob@example.com']);
      
      await this.query(`
        INSERT IGNORE INTO plans (name, rate_limit, price) VALUES 
        (?, ?, ?), (?, ?, ?), (?, ?, ?)
      `, ['basic', 100, 9.99, 'premium', 1000, 29.99, 'enterprise', 10000, 99.99]);
      
      await this.query(`
        INSERT IGNORE INTO feature_flags (name, enabled, description) VALUES 
        (?, ?, ?), (?, ?, ?), (?, ?, ?)
      `, [
        'new_dashboard', true, 'New dashboard UI',
        'api_v2', false, 'API version 2',
        'dark_mode', true, 'Dark mode theme'
      ]);
      
      await this.query(`
        INSERT INTO settings (\`key\`, value, type) VALUES 
        (?, ?, ?), (?, ?, ?), (?, ?, ?)
        ON DUPLICATE KEY UPDATE value = VALUES(value)
      `, [
        'max_users', '1000', 'integer',
        'app_name', 'TuskLang Demo', 'string',
        'maintenance_mode', 'false', 'boolean'
      ]);
      
      console.log('MySQL test data created successfully');
      
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
          
          if (config.database?.mysql) {
            return new MySQLAdapter(config.database.mysql);
          }
        } catch (e) {
          // Continue to next location
        }
      }
      
      throw new Error('No peanut.tsk found with MySQL configuration');
      
    } catch (error) {
      throw new Error(`Failed to load MySQL config from peanut.tsk: ${error.message}`);
    }
  }
}

module.exports = MySQLAdapter;