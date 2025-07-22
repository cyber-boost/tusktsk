/**
 * TuskLang Advanced Core - Goals 2.1, 2.2, 2.3 Integration
 * Integrates Database Integration, Event Streaming, and Security Framework
 */

const { DatabaseManager, PostgreSQLAdapter, MySQLAdapter, MongoDBAdapter } = require('./database-integration');
const { EventStream, WebSocketManager, EventProcessor } = require('./event-streaming');
const { SecurityManager, EncryptionManager, RateLimiter } = require('./security-framework');

class TuskAdvancedCore {
  constructor(options = {}) {
    // Initialize core components
    this.databaseManager = new DatabaseManager();
    this.eventStream = new EventStream(options.eventStream || {});
    this.webSocketManager = new WebSocketManager({
      ...options.webSocket,
      eventStream: this.eventStream
    });
    this.securityManager = new SecurityManager(options.security || {});
    this.encryptionManager = new EncryptionManager(options.encryption || {});
    this.rateLimiter = new RateLimiter(options.rateLimit || {});
    this.eventProcessor = new EventProcessor();
    
    // Core state
    this.isInitialized = false;
    this.config = options;
    
    // Register database adapters
    this.initializeDatabaseAdapters();
    
    // Setup event processing pipeline
    this.setupEventPipeline();
    
    // Setup security policies
    this.setupSecurityPolicies();
  }

  /**
   * Initialize the advanced core
   */
  async initialize() {
    try {
      // Start WebSocket server if enabled
      if (this.config.enableWebSocket !== false) {
        await this.webSocketManager.start();
      }
      
      this.isInitialized = true;
      console.log('TuskLang Advanced Core initialized successfully');
      
      return true;
    } catch (error) {
      console.error('Failed to initialize TuskLang Advanced Core:', error);
      throw error;
    }
  }

  /**
   * Initialize database adapters
   */
  initializeDatabaseAdapters() {
    this.databaseManager.registerAdapter('postgresql', PostgreSQLAdapter);
    this.databaseManager.registerAdapter('mysql', MySQLAdapter);
    this.databaseManager.registerAdapter('mongodb', MongoDBAdapter);
  }

  /**
   * Setup event processing pipeline
   */
  setupEventPipeline() {
    // Add security validation step
    this.eventProcessor.addPipelineStep(async (event) => {
      const violations = this.securityManager.validateSecurityPolicies('event_publish', event);
      if (violations.length > 0) {
        throw new Error(`Security policy violations: ${violations.map(v => v.reason).join(', ')}`);
      }
      return event;
    });

    // Add rate limiting step
    this.eventProcessor.addPipelineStep(async (event) => {
      const identifier = event.metadata?.source || 'unknown';
      if (!this.rateLimiter.isAllowed(identifier)) {
        throw new Error('Rate limit exceeded');
      }
      return event;
    });
  }

  /**
   * Setup security policies
   */
  setupSecurityPolicies() {
    // Event size policy
    this.securityManager.addSecurityPolicy('event_size', {
      appliesTo: ['event_publish'],
      validate: (data) => {
        const eventSize = JSON.stringify(data).length;
        return {
          valid: eventSize <= 1024 * 1024, // 1MB limit
          reason: eventSize > 1024 * 1024 ? 'Event size exceeds 1MB limit' : null
        };
      }
    });

    // Authentication policy (only for secure operations)
    this.securityManager.addSecurityPolicy('authentication', {
      appliesTo: ['secure_database_query', 'secure_event_publish'],
      validate: (data) => {
        return {
          valid: data.token && this.securityManager.validateSession(data.token),
          reason: !data.token ? 'Authentication token required' : 'Invalid authentication token'
        };
      }
    });
  }

  /**
   * Database operations with security
   */
  async executeSecureQuery(connectionName, query, params = [], token = null) {
    // Validate authentication
    if (token && !this.securityManager.validateSession(token)) {
      throw new Error('Invalid authentication token');
    }

    // Rate limiting
    const identifier = token || 'anonymous';
    if (!this.rateLimiter.isAllowed(identifier)) {
      throw new Error('Rate limit exceeded');
    }

    // Execute query
    const result = await this.databaseManager.executeQuery(connectionName, query, params);
    
    // Publish event
    this.eventStream.publish('database_query_executed', {
      connection: connectionName,
      query,
      resultCount: result.rowCount,
      executionTime: result.executionTime
    }, { source: identifier });

    return result;
  }

  /**
   * Secure event publishing
   */
  async publishSecureEvent(event, data, token = null, metadata = {}) {
    // Validate authentication
    if (token && !this.securityManager.validateSession(token)) {
      throw new Error('Invalid authentication token');
    }

    // Process event through pipeline
    const processedEvent = await this.eventProcessor.processEvent({
      event,
      data,
      metadata: { ...metadata, token }
    });

    // Publish event
    const eventId = this.eventStream.publish(event, data, metadata);
    
    return { eventId, processed: true };
  }

  /**
   * User authentication
   */
  async authenticateUser(username, password, metadata = {}) {
    try {
      const session = await this.securityManager.authenticateUser(username, password, metadata);
      
      // Publish authentication event
      this.eventStream.publish('user_authenticated', {
        username,
        sessionId: session.sessionId
      }, { source: 'system' });

      return session;
    } catch (error) {
      // Publish failed authentication event
      this.eventStream.publish('authentication_failed', {
        username,
        reason: error.message
      }, { source: 'system' });

      throw error;
    }
  }

  /**
   * Create database connection with security
   */
  async createSecureConnection(config, token = null) {
    // Validate authentication
    if (token && !this.securityManager.validateSession(token)) {
      throw new Error('Invalid authentication token');
    }

    const connection = await this.databaseManager.createConnection(config);
    
    // Publish connection event
    this.eventStream.publish('database_connected', {
      connectionName: config.name || 'default',
      type: config.type
    }, { source: token || 'system' });

    return connection;
  }

  /**
   * WebSocket connection with authentication
   */
  handleSecureConnection(connection, token = null) {
    // Validate token if provided
    if (token) {
      const session = this.securityManager.validateSession(token);
      if (!session) {
        connection.send(JSON.stringify({
          type: 'error',
          message: 'Invalid authentication token'
        }));
        connection.close();
        return null;
      }
    }

    const connectionId = this.webSocketManager.handleConnection(connection);
    
    // Publish connection event
    this.eventStream.publish('websocket_connected', {
      connectionId,
      authenticated: !!token
    }, { source: 'system' });

    return connectionId;
  }

  /**
   * Encrypt sensitive data
   */
  encryptData(data, key = null) {
    const encryptionKey = key || this.encryptionManager.generateKey();
    const encrypted = this.encryptionManager.encrypt(JSON.stringify(data), encryptionKey);
    
    return {
      encrypted: encrypted.encrypted,
      iv: encrypted.iv,
      algorithm: encrypted.algorithm,
      key: encryptionKey
    };
  }

  /**
   * Decrypt sensitive data
   */
  decryptData(encryptedData, key, iv) {
    const decrypted = this.encryptionManager.decrypt(encryptedData, key, iv);
    return JSON.parse(decrypted);
  }

  /**
   * Get comprehensive system status
   */
  getSystemStatus() {
    return {
      isInitialized: this.isInitialized,
      database: {
        connections: this.databaseManager.getConnectionStats(),
        adapters: this.databaseManager.adapters.size
      },
      events: {
        stream: this.eventStream.getStats(),
        websocket: this.webSocketManager.getStats()
      },
      security: {
        users: this.securityManager.getSecurityStats(),
        rateLimit: {
          totalRequests: this.rateLimiter.requests.size
        }
      },
      encryption: {
        algorithm: this.encryptionManager.options.algorithm,
        keyLength: this.encryptionManager.options.keyLength
      }
    };
  }

  /**
   * Cleanup and shutdown
   */
  async shutdown() {
    try {
      // Stop WebSocket server
      if (this.webSocketManager.isRunning) {
        await this.webSocketManager.stop();
      }

      // Close database connections
      await this.databaseManager.closeAllConnections();

      // Cleanup expired sessions
      this.securityManager.cleanupExpiredSessions();

      console.log('TuskLang Advanced Core shutdown completed');
      return true;
    } catch (error) {
      console.error('Error during shutdown:', error);
      throw error;
    }
  }

  /**
   * Get audit information
   */
  getAuditInfo() {
    return {
      security: this.securityManager.getAuditLog(),
      events: this.eventStream.getEventBuffer(),
      connections: this.webSocketManager.isRunning ? this.webSocketManager.getActiveSessions() : []
    };
  }
}

module.exports = {
  TuskAdvancedCore,
  DatabaseManager,
  EventStream,
  WebSocketManager,
  SecurityManager,
  EncryptionManager,
  RateLimiter
}; 