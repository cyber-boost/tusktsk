/**
 * TuskLang Security Core - Goals 5.1, 5.2, 5.3 Integration
 * Integrates Security, Database, and Network Systems
 */

const { SecurityManager, AuthorizationManager, EncryptionManager } = require('./security-system');
const { DatabaseManager } = require('./database-system');
const { NetworkManager } = require('./network-system');

class TuskSecurityCore {
  constructor(options = {}) {
    // Initialize core components
    this.securityManager = new SecurityManager(options.security || {});
    this.authorizationManager = new AuthorizationManager();
    this.encryptionManager = new EncryptionManager(options.encryption || {});
    this.databaseManager = new DatabaseManager(options.database || {});
    this.networkManager = new NetworkManager(options.network || {});
    
    // Core state
    this.isInitialized = false;
    this.config = options;
    
    // Setup integrations
    this.setupIntegrations();
    
    // Setup built-in security features
    this.setupSecurityFeatures();
    
    // Setup built-in network features
    this.setupNetworkFeatures();
  }

  /**
   * Initialize the security core
   */
  async initialize() {
    try {
      // Initialize database manager
      await this.databaseManager.initialize();
      
      // Initialize network manager
      await this.networkManager.initialize();
      
      // Setup default roles and permissions
      this.setupDefaultRoles();
      
      this.isInitialized = true;
      console.log('TuskLang Security Core initialized successfully');
      
      return true;
    } catch (error) {
      console.error('Failed to initialize TuskLang Security Core:', error);
      throw error;
    }
  }

  /**
   * Setup integrations between components
   */
  setupIntegrations() {
    // Connect security events to database logging
    this.securityManager.eventEmitter.on('userAuthenticated', (data) => {
      this.logSecurityEvent('user_authenticated', data);
    });

    this.securityManager.eventEmitter.on('failedLoginAttempt', (data) => {
      this.logSecurityEvent('failed_login_attempt', data);
    });

    // Connect network events to security monitoring
    this.networkManager.eventEmitter.on('requestHandled', (data) => {
      this.monitorNetworkActivity(data);
    });

    this.networkManager.eventEmitter.on('websocketConnected', (data) => {
      this.monitorWebSocketConnection(data);
    });
  }

  /**
   * Setup built-in security features
   */
  setupSecurityFeatures() {
    // Add authentication middleware
    this.networkManager.addMiddleware(async (options) => {
      // Add authentication headers if available
      if (this.currentSession) {
        options.headers = options.headers || {};
        options.headers['Authorization'] = `Bearer ${this.currentSession.id}`;
      }
      return options;
    });

    // Add rate limiting middleware
    this.networkManager.addMiddleware(async (options) => {
      // Simple rate limiting (can be enhanced)
      const clientIP = options.clientIP;
      if (clientIP && this.isRateLimited(clientIP)) {
        throw new Error('Rate limit exceeded');
      }
      return options;
    });
  }

  /**
   * Setup built-in network features
   */
  setupNetworkFeatures() {
    // Register default endpoints
    this.networkManager.registerEndpoint('GET', '/health', async (request) => {
      return {
        statusCode: 200,
        data: JSON.stringify({ status: 'healthy', timestamp: Date.now() }),
        headers: { 'Content-Type': 'application/json' }
      };
    });

    this.networkManager.registerEndpoint('POST', '/auth/login', async (request) => {
      try {
        const { username, password } = request.body;
        const result = await this.securityManager.authenticateUser(username, password, request.ip);
        
        return {
          statusCode: 200,
          data: JSON.stringify(result),
          headers: { 'Content-Type': 'application/json' }
        };
      } catch (error) {
        return {
          statusCode: 401,
          data: JSON.stringify({ error: error.message }),
          headers: { 'Content-Type': 'application/json' }
        };
      }
    });

    this.networkManager.registerEndpoint('POST', '/auth/register', async (request) => {
      try {
        const { username, password, userData } = request.body;
        const result = await this.securityManager.registerUser(username, password, userData);
        
        return {
          statusCode: 201,
          data: JSON.stringify(result),
          headers: { 'Content-Type': 'application/json' }
        };
      } catch (error) {
        return {
          statusCode: 400,
          data: JSON.stringify({ error: error.message }),
          headers: { 'Content-Type': 'application/json' }
        };
      }
    });
  }

  /**
   * Setup default roles and permissions
   */
  setupDefaultRoles() {
    // Define permissions
    this.authorizationManager.definePermission('read', 'Read access to resources');
    this.authorizationManager.definePermission('write', 'Write access to resources');
    this.authorizationManager.definePermission('delete', 'Delete access to resources');
    this.authorizationManager.definePermission('admin', 'Administrative access');

    // Define roles
    this.authorizationManager.defineRole('user', ['read']);
    this.authorizationManager.defineRole('editor', ['read', 'write']);
    this.authorizationManager.defineRole('admin', ['read', 'write', 'delete', 'admin']);
  }

  /**
   * Secure database operation with authentication
   */
  async secureDatabaseOperation(sessionId, operation, ...args) {
    // Validate session
    const session = this.securityManager.validateSession(sessionId);
    if (!session) {
      throw new Error('Invalid or expired session');
    }

    // Get user
    const user = this.securityManager.users.get(session.username);
    if (!user) {
      throw new Error('User not found');
    }

    // Check permissions
    if (!this.authorizationManager.hasPermission(user, 'write')) {
      throw new Error('Insufficient permissions');
    }

    // Execute operation
    const result = await operation(...args);
    
    // Log operation
    this.logSecurityEvent('database_operation', {
      username: session.username,
      operation: operation.name,
      timestamp: Date.now()
    });

    return result;
  }

  /**
   * Secure network request with encryption
   */
  async secureNetworkRequest(requestOptions, data = null, sessionId = null) {
    // Validate session if provided
    if (sessionId) {
      const session = this.securityManager.validateSession(sessionId);
      if (!session) {
        throw new Error('Invalid or expired session');
      }
    }

    // Encrypt sensitive data
    let encryptedData = data;
    if (data && requestOptions.encrypt) {
      const keyId = this.encryptionManager.generateKey();
      encryptedData = this.encryptionManager.encrypt(JSON.stringify(data), keyId);
      requestOptions.headers = requestOptions.headers || {};
      requestOptions.headers['X-Encryption-Key'] = keyId;
    }

    // Make request
    const response = await this.networkManager.makeRequest(requestOptions, encryptedData);
    
    // Decrypt response if needed
    if (response.headers['x-encrypted'] && requestOptions.encrypt) {
      const keyId = response.headers['x-encryption-key'];
      response.data = this.encryptionManager.decrypt(response.data, keyId);
    }

    return response;
  }

  /**
   * Create secure database with encryption
   */
  async createSecureDatabase(name, schema = {}, encryptionKey = null) {
    // Generate encryption key if not provided
    const keyId = encryptionKey || this.encryptionManager.generateKey();
    
    // Create database
    const database = await this.databaseManager.createDatabase(name, schema);
    
    // Store encryption key securely
    await this.storeEncryptionKey(name, keyId);
    
    return { database, keyId };
  }

  /**
   * Store encryption key securely
   */
  async storeEncryptionKey(databaseName, keyId) {
    try {
      // Create key store database if it doesn't exist
      if (!this.databaseManager.databases.has('key_store')) {
        await this.databaseManager.createDatabase('key_store', {
          databaseName: { type: 'string', required: true },
          keyId: { type: 'string', required: true },
          createdAt: { type: 'number', required: true }
        });
      }

      // Create keys table if it doesn't exist
      const keyStoreDb = this.databaseManager.databases.get('key_store');
      if (!keyStoreDb.tables.has('keys')) {
        await this.databaseManager.createTable('key_store', 'keys', {
          databaseName: { type: 'string', required: true },
          keyId: { type: 'string', required: true },
          createdAt: { type: 'number', required: true }
        });
      }

      await this.databaseManager.insertRecord('key_store', 'keys', {
        databaseName,
        keyId,
        createdAt: Date.now()
      });
    } catch (error) {
      console.error('Failed to store encryption key:', error);
    }
  }

  /**
   * Log security event
   */
  async logSecurityEvent(eventType, data) {
    try {
      // Create security log database if it doesn't exist
      if (!this.databaseManager.databases.has('security_logs')) {
        await this.databaseManager.createDatabase('security_logs', {
          eventType: { type: 'string', required: true },
          data: { type: 'object', required: true },
          timestamp: { type: 'number', required: true },
          ipAddress: { type: 'string' }
        });
      }

      // Create events table if it doesn't exist
      const securityLogsDb = this.databaseManager.databases.get('security_logs');
      if (!securityLogsDb.tables.has('events')) {
        await this.databaseManager.createTable('security_logs', 'events', {
          eventType: { type: 'string', required: true },
          data: { type: 'object', required: true },
          timestamp: { type: 'number', required: true },
          ipAddress: { type: 'string' }
        });
      }

      await this.databaseManager.insertRecord('security_logs', 'events', {
        eventType,
        data,
        timestamp: Date.now(),
        ipAddress: data.ipAddress || null
      });
    } catch (error) {
      console.error('Failed to log security event:', error);
    }
  }

  /**
   * Monitor network activity
   */
  async monitorNetworkActivity(data) {
    // Log suspicious activity
    if (data.statusCode >= 400) {
      await this.logSecurityEvent('network_error', data);
    }

    // Check for potential attacks
    if (data.duration > 10000) { // Requests taking more than 10 seconds
      await this.logSecurityEvent('slow_request', data);
    }
  }

  /**
   * Monitor WebSocket connection
   */
  async monitorWebSocketConnection(data) {
    // Log new WebSocket connections
    await this.logSecurityEvent('websocket_connected', data);
  }

  /**
   * Rate limiting check
   */
  isRateLimited(clientIP) {
    // Simple rate limiting implementation
    const now = Date.now();
    const window = 60000; // 1 minute window
    
    if (!this.rateLimitMap) {
      this.rateLimitMap = new Map();
    }

    const clientData = this.rateLimitMap.get(clientIP) || { count: 0, resetTime: now + window };
    
    if (now > clientData.resetTime) {
      clientData.count = 0;
      clientData.resetTime = now + window;
    }

    clientData.count++;
    this.rateLimitMap.set(clientIP, clientData);

    return clientData.count > 100; // Max 100 requests per minute
  }

  /**
   * Create secure HTTP server
   */
  createSecureServer(port, options = {}) {
    const server = this.networkManager.createServer(options);
    
    // Add security headers middleware
    server.on('request', (req, res) => {
      res.setHeader('X-Content-Type-Options', 'nosniff');
      res.setHeader('X-Frame-Options', 'DENY');
      res.setHeader('X-XSS-Protection', '1; mode=block');
      res.setHeader('Strict-Transport-Security', 'max-age=31536000; includeSubDomains');
    });

    return server;
  }

  /**
   * Create secure WebSocket server
   */
  createSecureWebSocketServer(server, options = {}) {
    const wss = this.networkManager.createWebSocketServer(server, options);
    
    // Add authentication to WebSocket connections
    wss.on('connection', (ws, req) => {
      // Extract token from query string or headers
      const token = req.url.split('token=')[1] || req.headers['authorization'];
      
      if (token) {
        try {
          // Verify token (simplified)
          const session = this.securityManager.validateSession(token);
          if (!session) {
            ws.close(1008, 'Invalid token');
            return;
          }
        } catch (error) {
          ws.close(1008, 'Authentication failed');
          return;
        }
      }
    });

    return wss;
  }

  /**
   * Get comprehensive security statistics
   */
  getSecurityStats() {
    return {
      isInitialized: this.isInitialized,
      security: this.securityManager.getSecurityStats(),
      network: this.networkManager.getNetworkStats(),
      database: {
        databases: this.databaseManager.databases.size,
        connections: this.databaseManager.connections.size
      },
      encryption: {
        keys: this.encryptionManager.keys.size
      },
      authorization: {
        roles: this.authorizationManager.roles.size,
        permissions: this.authorizationManager.permissions.size
      }
    };
  }

  /**
   * Get security audit log
   */
  async getSecurityAuditLog(limit = 100) {
    try {
      const events = await this.databaseManager.findRecords('security_logs', 'events', {}, {
        sort: { timestamp: 'desc' },
        limit
      });
      return events;
    } catch (error) {
      return [];
    }
  }

  /**
   * Perform security scan
   */
  async performSecurityScan() {
    const scanResults = {
      timestamp: Date.now(),
      checks: []
    };

    // Check for locked accounts
    const lockedAccounts = Array.from(this.securityManager.failedAttempts.entries())
      .filter(([_, attempts]) => attempts.length >= this.securityManager.options.maxLoginAttempts);
    
    scanResults.checks.push({
      type: 'locked_accounts',
      count: lockedAccounts.length,
      status: lockedAccounts.length > 0 ? 'warning' : 'ok'
    });

    // Check for blacklisted IPs
    scanResults.checks.push({
      type: 'blacklisted_ips',
      count: this.securityManager.blacklistedIPs.size,
      status: this.securityManager.blacklistedIPs.size > 0 ? 'warning' : 'ok'
    });

    // Check for expired sessions
    const expiredSessions = Array.from(this.securityManager.sessions.values())
      .filter(session => Date.now() > session.expiresAt);
    
    scanResults.checks.push({
      type: 'expired_sessions',
      count: expiredSessions.length,
      status: expiredSessions.length > 0 ? 'warning' : 'ok'
    });

    // Check for active connections
    scanResults.checks.push({
      type: 'active_connections',
      count: this.networkManager.connections.size,
      status: 'ok'
    });

    return scanResults;
  }

  /**
   * Cleanup and shutdown
   */
  async shutdown() {
    try {
      // Shutdown database manager
      await this.databaseManager.shutdown();
      
      // Shutdown network manager
      await this.networkManager.shutdown();
      
      console.log('TuskLang Security Core shutdown completed');
      return true;
    } catch (error) {
      console.error('Error during shutdown:', error);
      throw error;
    }
  }
}

module.exports = {
  TuskSecurityCore,
  SecurityManager,
  AuthorizationManager,
  EncryptionManager,
  DatabaseManager,
  NetworkManager
}; 