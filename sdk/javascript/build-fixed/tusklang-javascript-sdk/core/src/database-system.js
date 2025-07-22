/**
 * TuskLang Advanced Database and Storage Management System
 * Provides comprehensive database management, storage, and data persistence capabilities
 */

const { EventEmitter } = require('events');
const fs = require('fs').promises;
const path = require('path');

class DatabaseManager {
  constructor(options = {}) {
    this.options = {
      dataDir: options.dataDir || './data',
      maxConnections: options.maxConnections || 10,
      connectionTimeout: options.connectionTimeout || 30000,
      autoBackup: options.autoBackup !== false,
      backupInterval: options.backupInterval || 3600000, // 1 hour
      compression: options.compression !== false,
      ...options
    };
    
    this.databases = new Map();
    this.connections = new Map();
    this.transactions = new Map();
    this.backups = new Map();
    this.eventEmitter = new EventEmitter();
    this.isRunning = false;
    this.backupTimer = null;
  }

  /**
   * Initialize database manager
   */
  async initialize() {
    try {
      // Create data directory if it doesn't exist
      await fs.mkdir(this.options.dataDir, { recursive: true });
      
      // Load existing databases
      await this.loadDatabases();
      
      // Start backup timer
      if (this.options.autoBackup) {
        this.startBackupTimer();
      }
      
      this.isRunning = true;
      this.eventEmitter.emit('initialized');
      
      return true;
    } catch (error) {
      this.eventEmitter.emit('initializationError', error);
      throw error;
    }
  }

  /**
   * Create a new database
   */
  async createDatabase(name, schema = {}) {
    if (this.databases.has(name)) {
      throw new Error(`Database '${name}' already exists`);
    }

    const dbPath = path.join(this.options.dataDir, `${name}.json`);
    
    const database = {
      name,
      schema,
      tables: new Map(),
      indexes: new Map(),
      createdAt: Date.now(),
      lastModified: Date.now(),
      size: 0,
      recordCount: 0
    };

    // Create database file
    await fs.writeFile(dbPath, JSON.stringify({
      metadata: {
        name: database.name,
        schema: database.schema,
        createdAt: database.createdAt,
        lastModified: database.lastModified
      },
      tables: {},
      indexes: {}
    }, null, 2));

    this.databases.set(name, database);
    this.eventEmitter.emit('databaseCreated', { name, schema });
    
    return database;
  }

  /**
   * Open database connection
   */
  async openConnection(databaseName, options = {}) {
    const database = this.databases.get(databaseName);
    if (!database) {
      throw new Error(`Database '${databaseName}' not found`);
    }

    if (this.connections.size >= this.options.maxConnections) {
      throw new Error('Maximum connections reached');
    }

    const connectionId = this.generateConnectionId();
    const connection = {
      id: connectionId,
      databaseName,
      createdAt: Date.now(),
      lastActivity: Date.now(),
      isActive: true,
      options
    };

    this.connections.set(connectionId, connection);
    this.eventEmitter.emit('connectionOpened', { connectionId, databaseName });
    
    return connectionId;
  }

  /**
   * Close database connection
   */
  closeConnection(connectionId) {
    const connection = this.connections.get(connectionId);
    if (!connection) {
      return false;
    }

    connection.isActive = false;
    this.connections.delete(connectionId);
    this.eventEmitter.emit('connectionClosed', { connectionId });
    
    return true;
  }

  /**
   * Create a table
   */
  async createTable(databaseName, tableName, schema) {
    const database = this.databases.get(databaseName);
    if (!database) {
      throw new Error(`Database '${databaseName}' not found`);
    }

    if (database.tables.has(tableName)) {
      throw new Error(`Table '${tableName}' already exists`);
    }

    const table = {
      name: tableName,
      schema,
      records: new Map(),
      indexes: new Map(),
      createdAt: Date.now(),
      lastModified: Date.now(),
      recordCount: 0
    };

    database.tables.set(tableName, table);
    database.lastModified = Date.now();
    
    await this.saveDatabase(databaseName);
    
    this.eventEmitter.emit('tableCreated', { databaseName, tableName, schema });
    return table;
  }

  /**
   * Insert record into table
   */
  async insertRecord(databaseName, tableName, record) {
    const database = this.databases.get(databaseName);
    if (!database) {
      throw new Error(`Database '${databaseName}' not found`);
    }

    const table = database.tables.get(tableName);
    if (!table) {
      throw new Error(`Table '${tableName}' not found`);
    }

    // Validate record against schema
    this.validateRecord(record, table.schema);

    // Generate ID if not provided
    if (!record.id) {
      record.id = this.generateRecordId();
    }

    // Add metadata
    record._createdAt = Date.now();
    record._updatedAt = Date.now();

    // Insert record
    table.records.set(record.id, record);
    table.recordCount++;
    table.lastModified = Date.now();
    database.lastModified = Date.now();

    // Update indexes
    this.updateIndexes(table, record, 'insert');

    await this.saveDatabase(databaseName);
    
    this.eventEmitter.emit('recordInserted', { databaseName, tableName, recordId: record.id });
    return record;
  }

  /**
   * Find records in table
   */
  async findRecords(databaseName, tableName, query = {}, options = {}) {
    const database = this.databases.get(databaseName);
    if (!database) {
      throw new Error(`Database '${databaseName}' not found`);
    }

    const table = database.tables.get(tableName);
    if (!table) {
      throw new Error(`Table '${tableName}' not found`);
    }

    let results = Array.from(table.records.values());

    // Apply query filters
    if (Object.keys(query).length > 0) {
      results = results.filter(record => this.matchesQuery(record, query));
    }

    // Apply sorting
    if (options.sort) {
      results = this.sortRecords(results, options.sort);
    }

    // Apply pagination
    if (options.limit) {
      const offset = options.offset || 0;
      results = results.slice(offset, offset + options.limit);
    }

    return results;
  }

  /**
   * Update record in table
   */
  async updateRecord(databaseName, tableName, recordId, updates) {
    const database = this.databases.get(databaseName);
    if (!database) {
      throw new Error(`Database '${databaseName}' not found`);
    }

    const table = database.tables.get(tableName);
    if (!table) {
      throw new Error(`Table '${tableName}' not found`);
    }

    const record = table.records.get(recordId);
    if (!record) {
      throw new Error(`Record '${recordId}' not found`);
    }

    // Update indexes (remove old values)
    this.updateIndexes(table, record, 'delete');

    // Apply updates
    const updatedRecord = { ...record, ...updates };
    updatedRecord._updatedAt = Date.now();

    // Validate updated record
    this.validateRecord(updatedRecord, table.schema);

    // Update record
    table.records.set(recordId, updatedRecord);
    table.lastModified = Date.now();
    database.lastModified = Date.now();

    // Update indexes (add new values)
    this.updateIndexes(table, updatedRecord, 'insert');

    await this.saveDatabase(databaseName);
    
    this.eventEmitter.emit('recordUpdated', { databaseName, tableName, recordId });
    return updatedRecord;
  }

  /**
   * Delete record from table
   */
  async deleteRecord(databaseName, tableName, recordId) {
    const database = this.databases.get(databaseName);
    if (!database) {
      throw new Error(`Database '${databaseName}' not found`);
    }

    const table = database.tables.get(tableName);
    if (!table) {
      throw new Error(`Table '${tableName}' not found`);
    }

    const record = table.records.get(recordId);
    if (!record) {
      throw new Error(`Record '${recordId}' not found`);
    }

    // Update indexes
    this.updateIndexes(table, record, 'delete');

    // Delete record
    table.records.delete(recordId);
    table.recordCount--;
    table.lastModified = Date.now();
    database.lastModified = Date.now();

    await this.saveDatabase(databaseName);
    
    this.eventEmitter.emit('recordDeleted', { databaseName, tableName, recordId });
    return true;
  }

  /**
   * Create index on table
   */
  async createIndex(databaseName, tableName, fieldName, options = {}) {
    const database = this.databases.get(databaseName);
    if (!database) {
      throw new Error(`Database '${databaseName}' not found`);
    }

    const table = database.tables.get(tableName);
    if (!table) {
      throw new Error(`Table '${tableName}' not found`);
    }

    if (table.indexes.has(fieldName)) {
      throw new Error(`Index on field '${fieldName}' already exists`);
    }

    const index = {
      fieldName,
      values: new Map(),
      type: options.type || 'btree',
      unique: options.unique || false,
      createdAt: Date.now()
    };

    // Build index from existing records
    for (const [recordId, record] of table.records) {
      const value = record[fieldName];
      if (value !== undefined) {
        if (!index.values.has(value)) {
          index.values.set(value, new Set());
        }
        index.values.get(value).add(recordId);
      }
    }

    table.indexes.set(fieldName, index);
    await this.saveDatabase(databaseName);
    
    this.eventEmitter.emit('indexCreated', { databaseName, tableName, fieldName });
    return index;
  }

  /**
   * Start transaction
   */
  async startTransaction(databaseName) {
    const transactionId = this.generateTransactionId();
    const transaction = {
      id: transactionId,
      databaseName,
      operations: [],
      startedAt: Date.now(),
      isActive: true
    };

    this.transactions.set(transactionId, transaction);
    this.eventEmitter.emit('transactionStarted', { transactionId, databaseName });
    
    return transactionId;
  }

  /**
   * Commit transaction
   */
  async commitTransaction(transactionId) {
    const transaction = this.transactions.get(transactionId);
    if (!transaction || !transaction.isActive) {
      throw new Error('Invalid or inactive transaction');
    }

    // Apply all operations
    for (const operation of transaction.operations) {
      await this.applyOperation(operation);
    }

    transaction.isActive = false;
    this.transactions.delete(transactionId);
    
    this.eventEmitter.emit('transactionCommitted', { transactionId });
    return true;
  }

  /**
   * Rollback transaction
   */
  async rollbackTransaction(transactionId) {
    const transaction = this.transactions.get(transactionId);
    if (!transaction || !transaction.isActive) {
      throw new Error('Invalid or inactive transaction');
    }

    transaction.isActive = false;
    this.transactions.delete(transactionId);
    
    this.eventEmitter.emit('transactionRolledBack', { transactionId });
    return true;
  }

  /**
   * Create backup
   */
  async createBackup(databaseName) {
    const database = this.databases.get(databaseName);
    if (!database) {
      throw new Error(`Database '${databaseName}' not found`);
    }

    const backupId = this.generateBackupId();
    const backupPath = path.join(this.options.dataDir, `backup_${databaseName}_${backupId}.json`);
    
    const backup = {
      id: backupId,
      databaseName,
      path: backupPath,
      createdAt: Date.now(),
      size: 0
    };

    // Create backup file
    const backupData = {
      metadata: {
        backupId: backup.id,
        databaseName: backup.databaseName,
        createdAt: backup.createdAt
      },
      database: await this.serializeDatabase(databaseName)
    };

    const backupContent = this.options.compression 
      ? JSON.stringify(backupData) // Could add compression here
      : JSON.stringify(backupData, null, 2);

    await fs.writeFile(backupPath, backupContent);
    
    const stats = await fs.stat(backupPath);
    backup.size = stats.size;

    this.backups.set(backupId, backup);
    this.eventEmitter.emit('backupCreated', { backupId, databaseName, size: backup.size });
    
    return backup;
  }

  /**
   * Restore from backup
   */
  async restoreFromBackup(backupId) {
    const backup = this.backups.get(backupId);
    if (!backup) {
      throw new Error(`Backup '${backupId}' not found`);
    }

    const backupData = JSON.parse(await fs.readFile(backup.path, 'utf8'));
    const database = backupData.database;

    // Restore database
    this.databases.set(database.name, database);
    await this.saveDatabase(database.name);
    
    this.eventEmitter.emit('backupRestored', { backupId, databaseName: database.name });
    return true;
  }

  /**
   * Get database statistics
   */
  getDatabaseStats(databaseName) {
    const database = this.databases.get(databaseName);
    if (!database) {
      return null;
    }

    let totalRecords = 0;
    let totalSize = 0;

    for (const table of database.tables.values()) {
      totalRecords += table.recordCount;
      totalSize += table.records.size;
    }

    return {
      name: database.name,
      tables: database.tables.size,
      totalRecords,
      totalSize,
      createdAt: database.createdAt,
      lastModified: database.lastModified
    };
  }

  /**
   * Validate record against schema
   */
  validateRecord(record, schema) {
    for (const [fieldName, fieldSchema] of Object.entries(schema)) {
      if (fieldSchema.required && record[fieldName] === undefined) {
        throw new Error(`Required field '${fieldName}' is missing`);
      }

      if (record[fieldName] !== undefined) {
        const value = record[fieldName];
        const expectedType = fieldSchema.type;

        if (expectedType && typeof value !== expectedType) {
          throw new Error(`Field '${fieldName}' must be of type '${expectedType}'`);
        }
      }
    }
  }

  /**
   * Check if record matches query
   */
  matchesQuery(record, query) {
    for (const [field, value] of Object.entries(query)) {
      if (record[field] !== value) {
        return false;
      }
    }
    return true;
  }

  /**
   * Sort records
   */
  sortRecords(records, sort) {
    return records.sort((a, b) => {
      for (const [field, direction] of Object.entries(sort)) {
        const aVal = a[field];
        const bVal = b[field];
        
        if (aVal < bVal) return direction === 'desc' ? 1 : -1;
        if (aVal > bVal) return direction === 'desc' ? -1 : 1;
      }
      return 0;
    });
  }

  /**
   * Update indexes
   */
  updateIndexes(table, record, operation) {
    for (const [fieldName, index] of table.indexes) {
      const value = record[fieldName];
      if (value !== undefined) {
        if (operation === 'insert') {
          if (!index.values.has(value)) {
            index.values.set(value, new Set());
          }
          index.values.get(value).add(record.id);
        } else if (operation === 'delete') {
          const valueSet = index.values.get(value);
          if (valueSet) {
            valueSet.delete(record.id);
            if (valueSet.size === 0) {
              index.values.delete(value);
            }
          }
        }
      }
    }
  }

  /**
   * Save database to file
   */
  async saveDatabase(databaseName) {
    const database = this.databases.get(databaseName);
    if (!database) return;

    const dbPath = path.join(this.options.dataDir, `${databaseName}.json`);
    const data = await this.serializeDatabase(databaseName);
    
    await fs.writeFile(dbPath, JSON.stringify(data, null, 2));
  }

  /**
   * Serialize database
   */
  async serializeDatabase(databaseName) {
    const database = this.databases.get(databaseName);
    if (!database) return null;

    return {
      metadata: {
        name: database.name,
        schema: database.schema,
        createdAt: database.createdAt,
        lastModified: database.lastModified
      },
      tables: Object.fromEntries(
        Array.from(database.tables.entries()).map(([tableName, table]) => [
          tableName,
          {
            name: table.name,
            schema: table.schema,
            records: Object.fromEntries(table.records),
            indexes: Object.fromEntries(
              Array.from(table.indexes.entries()).map(([fieldName, index]) => [
                fieldName,
                {
                  fieldName: index.fieldName,
                  values: Object.fromEntries(
                    Array.from(index.values.entries()).map(([value, recordIds]) => [
                      value,
                      Array.from(recordIds)
                    ])
                  ),
                  type: index.type,
                  unique: index.unique,
                  createdAt: index.createdAt
                }
              ])
            ),
            createdAt: table.createdAt,
            lastModified: table.lastModified,
            recordCount: table.recordCount
          }
        ])
      )
    };
  }

  /**
   * Load databases from files
   */
  async loadDatabases() {
    try {
      const files = await fs.readdir(this.options.dataDir);
      
      for (const file of files) {
        if (file.endsWith('.json') && !file.startsWith('backup_')) {
          const databaseName = file.replace('.json', '');
          const dbPath = path.join(this.options.dataDir, file);
          const data = JSON.parse(await fs.readFile(dbPath, 'utf8'));
          
          await this.loadDatabase(databaseName, data);
        }
      }
    } catch (error) {
      console.warn('Error loading databases:', error.message);
    }
  }

  /**
   * Load database from data
   */
  async loadDatabase(databaseName, data) {
    const database = {
      name: data.metadata.name,
      schema: data.metadata.schema,
      tables: new Map(),
      indexes: new Map(),
      createdAt: data.metadata.createdAt,
      lastModified: data.metadata.lastModified,
      size: 0,
      recordCount: 0
    };

    // Load tables
    for (const [tableName, tableData] of Object.entries(data.tables)) {
      const table = {
        name: tableData.name,
        schema: tableData.schema,
        records: new Map(Object.entries(tableData.records)),
        indexes: new Map(),
        createdAt: tableData.createdAt,
        lastModified: tableData.lastModified,
        recordCount: tableData.recordCount
      };

      // Load indexes
      for (const [fieldName, indexData] of Object.entries(tableData.indexes)) {
        const index = {
          fieldName: indexData.fieldName,
          values: new Map(
            Object.entries(indexData.values).map(([value, recordIds]) => [
              value,
              new Set(recordIds)
            ])
          ),
          type: indexData.type,
          unique: indexData.unique,
          createdAt: indexData.createdAt
        };

        table.indexes.set(fieldName, index);
      }

      database.tables.set(tableName, table);
      database.recordCount += table.recordCount;
    }

    this.databases.set(databaseName, database);
  }

  /**
   * Start backup timer
   */
  startBackupTimer() {
    this.backupTimer = setInterval(async () => {
      for (const databaseName of this.databases.keys()) {
        try {
          await this.createBackup(databaseName);
        } catch (error) {
          console.error(`Backup failed for database ${databaseName}:`, error.message);
        }
      }
    }, this.options.backupInterval);
  }

  /**
   * Generate IDs
   */
  generateConnectionId() {
    return `conn_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  generateTransactionId() {
    return `txn_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  generateRecordId() {
    return `rec_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  generateBackupId() {
    return `backup_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Apply operation (for transactions)
   */
  async applyOperation(operation) {
    switch (operation.type) {
      case 'insert':
        await this.insertRecord(operation.databaseName, operation.tableName, operation.record);
        break;
      case 'update':
        await this.updateRecord(operation.databaseName, operation.tableName, operation.recordId, operation.updates);
        break;
      case 'delete':
        await this.deleteRecord(operation.databaseName, operation.tableName, operation.recordId);
        break;
    }
  }

  /**
   * Shutdown database manager
   */
  async shutdown() {
    this.isRunning = false;
    
    if (this.backupTimer) {
      clearInterval(this.backupTimer);
      this.backupTimer = null;
    }

    // Close all connections
    for (const connectionId of this.connections.keys()) {
      this.closeConnection(connectionId);
    }

    // Rollback all active transactions
    for (const transactionId of this.transactions.keys()) {
      await this.rollbackTransaction(transactionId);
    }

    this.eventEmitter.emit('shutdown');
  }
}

module.exports = {
  DatabaseManager
}; 