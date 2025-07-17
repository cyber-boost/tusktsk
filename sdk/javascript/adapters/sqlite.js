/**
 * SQLite Adapter for TuskLang
 * ===========================
 * Enables @query operations with SQLite databases
 */

class SQLiteAdapter {
  constructor(options = {}) {
    this.filename = options.filename || ':memory:';
    this.db = null;
    
    // Lazy load better-sqlite3
    try {
      const Database = require('better-sqlite3');
      this.db = new Database(this.filename, {
        readonly: options.readonly || false,
        fileMustExist: options.fileMustExist || false,
        timeout: options.timeout || 5000,
        verbose: options.verbose || null
      });
    } catch (error) {
      throw new Error('SQLite adapter requires better-sqlite3. Install it with: npm install better-sqlite3');
    }
  }

  /**
   * Execute a query
   * @param {string} sql - SQL query
   * @param {array} params - Query parameters
   * @returns {any} Query result
   */
  query(sql, params = []) {
    try {
      const normalized = sql.trim().toLowerCase();
      
      // SELECT queries
      if (normalized.startsWith('select')) {
        // Check if it's a single value query
        if (normalized.includes('count(') || 
            normalized.includes('sum(') || 
            normalized.includes('avg(') || 
            normalized.includes('max(') || 
            normalized.includes('min(')) {
          // Return single value
          const stmt = this.db.prepare(sql);
          const row = stmt.get(...params);
          return row ? Object.values(row)[0] : null;
        }
        
        // Check if LIMIT 1
        if (normalized.includes('limit 1')) {
          const stmt = this.db.prepare(sql);
          return stmt.get(...params);
        }
        
        // Return all rows
        const stmt = this.db.prepare(sql);
        return stmt.all(...params);
      }
      
      // INSERT, UPDATE, DELETE queries
      if (normalized.startsWith('insert') || 
          normalized.startsWith('update') || 
          normalized.startsWith('delete')) {
        const stmt = this.db.prepare(sql);
        const result = stmt.run(...params);
        return result.changes;
      }
      
      // Other queries (CREATE, etc)
      this.db.exec(sql);
      return true;
      
    } catch (error) {
      console.error('SQLite query error:', error);
      throw error;
    }
  }

  /**
   * Execute a query asynchronously
   * @param {string} sql - SQL query
   * @param {array} params - Query parameters
   * @returns {Promise<any>} Query result
   */
  async queryAsync(sql, params = []) {
    return new Promise((resolve, reject) => {
      try {
        const result = this.query(sql, params);
        resolve(result);
      } catch (error) {
        reject(error);
      }
    });
  }

  /**
   * Begin a transaction
   */
  beginTransaction() {
    this.db.exec('BEGIN');
  }

  /**
   * Commit a transaction
   */
  commit() {
    this.db.exec('COMMIT');
  }

  /**
   * Rollback a transaction
   */
  rollback() {
    this.db.exec('ROLLBACK');
  }

  /**
   * Close the database connection
   */
  close() {
    if (this.db) {
      this.db.close();
    }
  }

  /**
   * Create a test database with sample data
   */
  createTestData() {
    // Create tables
    this.db.exec(`
      CREATE TABLE IF NOT EXISTS users (
        id INTEGER PRIMARY KEY,
        name TEXT NOT NULL,
        email TEXT UNIQUE,
        active BOOLEAN DEFAULT 1,
        created_at DATETIME DEFAULT CURRENT_TIMESTAMP
      );
      
      CREATE TABLE IF NOT EXISTS plans (
        id INTEGER PRIMARY KEY,
        name TEXT NOT NULL,
        rate_limit INTEGER DEFAULT 1000,
        price DECIMAL(10,2)
      );
      
      CREATE TABLE IF NOT EXISTS feature_flags (
        id INTEGER PRIMARY KEY,
        name TEXT NOT NULL,
        enabled BOOLEAN DEFAULT 0,
        description TEXT
      );
    `);
    
    // Insert sample data
    const insertUser = this.db.prepare('INSERT OR IGNORE INTO users (name, email) VALUES (?, ?)');
    insertUser.run('John Doe', 'john@example.com');
    insertUser.run('Jane Smith', 'jane@example.com');
    
    const insertPlan = this.db.prepare('INSERT OR IGNORE INTO plans (name, rate_limit, price) VALUES (?, ?, ?)');
    insertPlan.run('basic', 100, 9.99);
    insertPlan.run('premium', 1000, 29.99);
    insertPlan.run('enterprise', 10000, 99.99);
    
    const insertFeature = this.db.prepare('INSERT OR IGNORE INTO feature_flags (name, enabled, description) VALUES (?, ?, ?)');
    insertFeature.run('new_dashboard', 1, 'New dashboard UI');
    insertFeature.run('api_v2', 0, 'API version 2');
    insertFeature.run('dark_mode', 1, 'Dark mode theme');
  }
}

module.exports = SQLiteAdapter;