/**
 * Database Commands for TuskLang CLI
 * ===================================
 * Implements all database operations from the CLI specification
 */

const { Command } = require('commander');
const fs = require('fs').promises;
const path = require('path');
const TuskLang = require('../../index.js');

// Database adapters
const adapters = {
  sqlite: require('../../adapters/sqlite.js'),
  postgres: require('../../adapters/postgres.js'),
  mysql: require('../../adapters/mysql.js'),
  mongodb: require('../../adapters/mongodb.js'),
  redis: require('../../adapters/redis.js')
};

// Helper function to get database adapter
async function getDatabaseAdapter() {
  try {
    // Try to load from peanut.tsk
    const peanutConfig = new (require('../../peanut-config.js'))();
    const config = peanutConfig.load(process.cwd());
    
    if (config.database) {
      const { type, ...options } = config.database;
      if (adapters[type]) {
        return new adapters[type](options);
      }
    }
    
    // Fallback to SQLite
    return new adapters.sqlite({ filename: ':memory:' });
  } catch (error) {
    console.warn('⚠️ No database configuration found, using SQLite in-memory');
    return new adapters.sqlite({ filename: ':memory:' });
  }
}

// Database status command
const status = new Command('status')
  .description('Check database connection status')
  .action(async () => {
    try {
      console.log('🔄 Checking database connection...');
      const adapter = await getDatabaseAdapter();
      const isConnected = await adapter.isConnected();
      
      if (isConnected) {
        console.log('✅ Database connected successfully');
        return { status: 'connected', adapter: adapter.constructor.name };
      } else {
        console.log('❌ Database connection failed');
        return { status: 'disconnected', adapter: adapter.constructor.name };
      }
    } catch (error) {
      console.error('❌ Database status check failed:', error.message);
      return { status: 'error', error: error.message };
    }
  });

// Database migrate command
const migrate = new Command('migrate')
  .description('Run migration file')
  .argument('<file>', 'Migration file to run')
  .action(async (file) => {
    try {
      console.log(`🔄 Running migration: ${file}`);
      
      // Check if file exists
      await fs.access(file);
      
      // Read migration file
      const migrationContent = await fs.readFile(file, 'utf8');
      
      // Get database adapter
      const adapter = await getDatabaseAdapter();
      
      // Execute migration
      const result = await adapter.query(migrationContent);
      
      console.log('✅ Migration completed successfully');
      return { success: true, file, result };
    } catch (error) {
      console.error('❌ Migration failed:', error.message);
      return { success: false, file, error: error.message };
    }
  });

// Database console command
const console = new Command('console')
  .description('Open interactive database console')
  .action(async () => {
    try {
      console.log('🔄 Opening database console...');
      const adapter = await getDatabaseAdapter();
      
      console.log(`📍 Connected to ${adapter.constructor.name}`);
      console.log('Type SQL queries, "exit" to quit\n');
      
      const readline = require('readline');
      const rl = readline.createInterface({
        input: process.stdin,
        output: process.stdout,
        prompt: 'db> '
      });

      rl.prompt();

      rl.on('line', async (line) => {
        const input = line.trim();
        
        if (input === 'exit' || input === 'quit') {
          rl.close();
          return;
        }
        
        if (input) {
          try {
            const result = await adapter.query(input);
            console.log('Result:', result);
          } catch (error) {
            console.error('❌ Query error:', error.message);
          }
        }
        
        rl.prompt();
      });

      rl.on('close', () => {
        console.log('Database console closed');
        process.exit(0);
      });
      
    } catch (error) {
      console.error('❌ Failed to open database console:', error.message);
      return { success: false, error: error.message };
    }
  });

// Database backup command
const backup = new Command('backup')
  .description('Backup database')
  .argument('[file]', 'Backup file name', `tusklang_backup_${Date.now()}.sql`)
  .action(async (file) => {
    try {
      console.log(`🔄 Creating database backup: ${file}`);
      const adapter = await getDatabaseAdapter();
      
      // For SQLite, copy the file
      if (adapter.constructor.name === 'SQLiteAdapter') {
        if (adapter.filename !== ':memory:') {
          await fs.copyFile(adapter.filename, file);
          console.log('✅ SQLite database backed up successfully');
          return { success: true, file };
        } else {
          throw new Error('Cannot backup in-memory SQLite database');
        }
      }
      
      // For other databases, export schema and data
      const schema = await adapter.query("SELECT sql FROM sqlite_master WHERE type='table'");
      const backupContent = schema.map(row => row.sql).join(';\n\n');
      
      await fs.writeFile(file, backupContent);
      console.log('✅ Database backup created successfully');
      return { success: true, file };
    } catch (error) {
      console.error('❌ Backup failed:', error.message);
      return { success: false, file, error: error.message };
    }
  });

// Database restore command
const restore = new Command('restore')
  .description('Restore from backup file')
  .argument('<file>', 'Backup file to restore from')
  .action(async (file) => {
    try {
      console.log(`🔄 Restoring database from: ${file}`);
      
      // Check if backup file exists
      await fs.access(file);
      
      // Read backup file
      const backupContent = await fs.readFile(file, 'utf8');
      
      // Get database adapter
      const adapter = await getDatabaseAdapter();
      
      // Execute restore
      const result = await adapter.query(backupContent);
      
      console.log('✅ Database restored successfully');
      return { success: true, file, result };
    } catch (error) {
      console.error('❌ Restore failed:', error.message);
      return { success: false, file, error: error.message };
    }
  });

// Database init command
const init = new Command('init')
  .description('Initialize SQLite database')
  .action(async () => {
    try {
      console.log('🔄 Initializing SQLite database...');
      
      const dbPath = path.join(process.cwd(), 'tusklang.db');
      const adapter = new adapters.sqlite({ filename: dbPath });
      
      // Create basic tables
      await adapter.query(`
        CREATE TABLE IF NOT EXISTS users (
          id INTEGER PRIMARY KEY,
          name TEXT NOT NULL,
          email TEXT UNIQUE,
          created_at DATETIME DEFAULT CURRENT_TIMESTAMP
        )
      `);
      
      await adapter.query(`
        CREATE TABLE IF NOT EXISTS settings (
          key TEXT PRIMARY KEY,
          value TEXT,
          type TEXT DEFAULT 'string'
        )
      `);
      
      await adapter.query(`
        CREATE TABLE IF NOT EXISTS migrations (
          id INTEGER PRIMARY KEY,
          name TEXT NOT NULL,
          executed_at DATETIME DEFAULT CURRENT_TIMESTAMP
        )
      `);
      
      console.log('✅ SQLite database initialized successfully');
      console.log(`📍 Database file: ${dbPath}`);
      
      return { success: true, database: dbPath };
    } catch (error) {
      console.error('❌ Database initialization failed:', error.message);
      return { success: false, error: error.message };
    }
  });

module.exports = {
  status,
  migrate,
  console,
  backup,
  restore,
  init
}; 