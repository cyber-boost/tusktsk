/**
 * TuskLang Advanced Network and Communication System
 * Provides comprehensive networking, communication, and protocol handling capabilities
 */

const { EventEmitter } = require('events');
const http = require('http');
const https = require('https');
const url = require('url');

class NetworkManager {
  constructor(options = {}) {
    this.options = {
      timeout: options.timeout || 30000,
      maxRetries: options.maxRetries || 3,
      retryDelay: options.retryDelay || 1000,
      maxConnections: options.maxConnections || 100,
      keepAlive: options.keepAlive !== false,
      ...options
    };
    
    this.connections = new Map();
    this.requests = new Map();
    this.endpoints = new Map();
    this.middleware = [];
    this.eventEmitter = new EventEmitter();
    this.isRunning = false;
  }

  /**
   * Initialize network manager
   */
  async initialize() {
    try {
      this.isRunning = true;
      this.eventEmitter.emit('initialized');
      return true;
    } catch (error) {
      this.eventEmitter.emit('initializationError', error);
      throw error;
    }
  }

  /**
   * Make HTTP request
   */
  async makeRequest(requestOptions, data = null) {
    const requestId = this.generateRequestId();
    const startTime = Date.now();

    const request = {
      id: requestId,
      options: { ...requestOptions },
      data,
      startTime,
      status: 'pending',
      attempts: 0,
      response: null,
      error: null
    };

    this.requests.set(requestId, request);

    try {
      const response = await this.executeRequest(request);
      request.status = 'completed';
      request.response = response;
      
      this.eventEmitter.emit('requestCompleted', { 
        requestId, 
        duration: Date.now() - startTime,
        statusCode: response.statusCode 
      });
      
      return response;
    } catch (error) {
      request.status = 'failed';
      request.error = error;
      
      this.eventEmitter.emit('requestFailed', { 
        requestId, 
        error: error.message,
        duration: Date.now() - startTime 
      });
      
      throw error;
    } finally {
      this.requests.delete(requestId);
    }
  }

  /**
   * Execute request with retry logic
   */
  async executeRequest(request) {
    let lastError;

    for (let attempt = 1; attempt <= this.options.maxRetries; attempt++) {
      try {
        request.attempts = attempt;
        
        // Apply middleware
        const processedOptions = await this.applyMiddleware(request.options);
        
        const response = await this.performRequest(processedOptions, request.data);
        return response;
      } catch (error) {
        lastError = error;
        
        if (attempt < this.options.maxRetries) {
          await this.delay(this.options.retryDelay * attempt);
        }
      }
    }

    throw lastError;
  }

  /**
   * Perform actual HTTP request
   */
  async performRequest(options, data) {
    return new Promise((resolve, reject) => {
      const urlObj = url.parse(options.url);
      const isHttps = urlObj.protocol === 'https:';
      const client = isHttps ? https : http;

      const requestOptions = {
        hostname: urlObj.hostname,
        port: urlObj.port || (isHttps ? 443 : 80),
        path: urlObj.path,
        method: options.method || 'GET',
        headers: options.headers || {},
        timeout: this.options.timeout,
        keepAlive: this.options.keepAlive
      };

      const req = client.request(requestOptions, (res) => {
        let responseData = '';

        res.on('data', (chunk) => {
          responseData += chunk;
        });

        res.on('end', () => {
          const response = {
            statusCode: res.statusCode,
            headers: res.headers,
            data: responseData,
            url: options.url
          };

          resolve(response);
        });
      });

      req.on('error', (error) => {
        reject(error);
      });

      req.on('timeout', () => {
        req.destroy();
        reject(new Error('Request timeout'));
      });

      if (data) {
        req.write(typeof data === 'string' ? data : JSON.stringify(data));
      }

      req.end();
    });
  }

  /**
   * Add middleware
   */
  addMiddleware(middleware) {
    this.middleware.push(middleware);
  }

  /**
   * Apply middleware to request
   */
  async applyMiddleware(options) {
    let processedOptions = { ...options };

    for (const middleware of this.middleware) {
      processedOptions = await middleware(processedOptions);
    }

    return processedOptions;
  }

  /**
   * Create HTTP server
   */
  createServer(options = {}) {
    const server = http.createServer(async (req, res) => {
      const requestId = this.generateRequestId();
      const startTime = Date.now();

      try {
        // Parse request
        const parsedUrl = url.parse(req.url, true);
        const request = {
          id: requestId,
          method: req.method,
          url: req.url,
          path: parsedUrl.pathname,
          query: parsedUrl.query,
          headers: req.headers,
          body: null
        };

        // Read request body
        if (req.method !== 'GET' && req.method !== 'HEAD') {
          request.body = await this.readRequestBody(req);
        }

        // Handle request
        const response = await this.handleRequest(request);
        
        // Send response
        res.writeHead(response.statusCode || 200, response.headers || {});
        res.end(response.data || '');

        this.eventEmitter.emit('requestHandled', {
          requestId,
          method: req.method,
          path: parsedUrl.pathname,
          statusCode: response.statusCode || 200,
          duration: Date.now() - startTime
        });

      } catch (error) {
        res.writeHead(500, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({ error: error.message }));

        this.eventEmitter.emit('requestError', {
          requestId,
          error: error.message,
          duration: Date.now() - startTime
        });
      }
    });

    return server;
  }

  /**
   * Register endpoint handler
   */
  registerEndpoint(method, path, handler) {
    const key = `${method.toUpperCase()}:${path}`;
    this.endpoints.set(key, handler);
  }

  /**
   * Handle incoming request
   */
  async handleRequest(request) {
    const key = `${request.method.toUpperCase()}:${request.path}`;
    const handler = this.endpoints.get(key);

    if (handler) {
      return await handler(request);
    }

    // Default 404 response
    return {
      statusCode: 404,
      data: JSON.stringify({ error: 'Endpoint not found' }),
      headers: { 'Content-Type': 'application/json' }
    };
  }

  /**
   * Read request body
   */
  async readRequestBody(req) {
    return new Promise((resolve, reject) => {
      let body = '';
      
      req.on('data', (chunk) => {
        body += chunk.toString();
      });

      req.on('end', () => {
        try {
          // Try to parse as JSON
          const contentType = req.headers['content-type'] || '';
          if (contentType.includes('application/json')) {
            resolve(JSON.parse(body));
          } else {
            resolve(body);
          }
        } catch (error) {
          resolve(body);
        }
      });

      req.on('error', reject);
    });
  }

  /**
   * Create WebSocket server
   */
  createWebSocketServer(server, options = {}) {
    let WebSocket;
    try {
      WebSocket = require('ws');
    } catch (error) {
      console.warn('WebSocket module not available. Install with: npm install ws');
      return null;
    }

    const wss = new WebSocket.Server({ server, ...options });

    wss.on('connection', (ws, req) => {
      const connectionId = this.generateConnectionId();
      
      const connection = {
        id: connectionId,
        ws,
        ip: req.socket.remoteAddress,
        userAgent: req.headers['user-agent'],
        createdAt: Date.now(),
        isActive: true
      };

      this.connections.set(connectionId, connection);

      ws.on('message', (message) => {
        this.handleWebSocketMessage(connectionId, message);
      });

      ws.on('close', () => {
        this.handleWebSocketClose(connectionId);
      });

      ws.on('error', (error) => {
        this.handleWebSocketError(connectionId, error);
      });

      this.eventEmitter.emit('websocketConnected', { connectionId, ip: connection.ip });
    });

    return wss;
  }

  /**
   * Handle WebSocket message
   */
  handleWebSocketMessage(connectionId, message) {
    const connection = this.connections.get(connectionId);
    if (!connection) return;

    try {
      const data = JSON.parse(message);
      this.eventEmitter.emit('websocketMessage', { connectionId, data });
    } catch (error) {
      this.eventEmitter.emit('websocketMessageError', { connectionId, error: error.message });
    }
  }

  /**
   * Handle WebSocket close
   */
  handleWebSocketClose(connectionId) {
    const connection = this.connections.get(connectionId);
    if (connection) {
      connection.isActive = false;
      this.connections.delete(connectionId);
      this.eventEmitter.emit('websocketDisconnected', { connectionId });
    }
  }

  /**
   * Handle WebSocket error
   */
  handleWebSocketError(connectionId, error) {
    this.eventEmitter.emit('websocketError', { connectionId, error: error.message });
  }

  /**
   * Send message to WebSocket connection
   */
  sendWebSocketMessage(connectionId, data) {
    const connection = this.connections.get(connectionId);
    if (!connection || !connection.isActive) {
      throw new Error('Connection not found or inactive');
    }

    const message = typeof data === 'string' ? data : JSON.stringify(data);
    connection.ws.send(message);
  }

  /**
   * Broadcast message to all WebSocket connections
   */
  broadcastWebSocketMessage(data, filter = null) {
    const message = typeof data === 'string' ? data : JSON.stringify(data);
    let sentCount = 0;

    for (const [connectionId, connection] of this.connections) {
      if (connection.isActive && (!filter || filter(connection))) {
        connection.ws.send(message);
        sentCount++;
      }
    }

    return sentCount;
  }

  /**
   * Create TCP server
   */
  createTCPServer(port, host = 'localhost') {
    const net = require('net');
    
    const server = net.createServer((socket) => {
      const connectionId = this.generateConnectionId();
      
      const connection = {
        id: connectionId,
        socket,
        ip: socket.remoteAddress,
        port: socket.remotePort,
        createdAt: Date.now(),
        isActive: true
      };

      this.connections.set(connectionId, connection);

      socket.on('data', (data) => {
        this.handleTCPMessage(connectionId, data);
      });

      socket.on('close', () => {
        this.handleTCPClose(connectionId);
      });

      socket.on('error', (error) => {
        this.handleTCPError(connectionId, error);
      });

      this.eventEmitter.emit('tcpConnected', { connectionId, ip: connection.ip });
    });

    server.listen(port, host, () => {
      this.eventEmitter.emit('tcpServerStarted', { port, host });
    });

    return server;
  }

  /**
   * Handle TCP message
   */
  handleTCPMessage(connectionId, data) {
    const connection = this.connections.get(connectionId);
    if (!connection) return;

    this.eventEmitter.emit('tcpMessage', { connectionId, data: data.toString() });
  }

  /**
   * Handle TCP close
   */
  handleTCPClose(connectionId) {
    const connection = this.connections.get(connectionId);
    if (connection) {
      connection.isActive = false;
      this.connections.delete(connectionId);
      this.eventEmitter.emit('tcpDisconnected', { connectionId });
    }
  }

  /**
   * Handle TCP error
   */
  handleTCPError(connectionId, error) {
    this.eventEmitter.emit('tcpError', { connectionId, error: error.message });
  }

  /**
   * Send message to TCP connection
   */
  sendTCPMessage(connectionId, data) {
    const connection = this.connections.get(connectionId);
    if (!connection || !connection.isActive) {
      throw new Error('Connection not found or inactive');
    }

    const message = typeof data === 'string' ? data : JSON.stringify(data);
    connection.socket.write(message);
  }

  /**
   * Get network statistics
   */
  getNetworkStats() {
    return {
      activeConnections: Array.from(this.connections.values()).filter(c => c.isActive).length,
      totalConnections: this.connections.size,
      activeRequests: Array.from(this.requests.values()).filter(r => r.status === 'pending').length,
      registeredEndpoints: this.endpoints.size,
      middlewareCount: this.middleware.length
    };
  }

  /**
   * Close all connections
   */
  closeAllConnections() {
    for (const [connectionId, connection] of this.connections) {
      if (connection.ws) {
        connection.ws.close();
      } else if (connection.socket) {
        connection.socket.destroy();
      }
    }
    this.connections.clear();
  }

  /**
   * Generate IDs
   */
  generateRequestId() {
    return `req_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  generateConnectionId() {
    return `conn_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Delay utility
   */
  delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  /**
   * Shutdown network manager
   */
  async shutdown() {
    this.isRunning = false;
    this.closeAllConnections();
    this.eventEmitter.emit('shutdown');
  }
}

module.exports = {
  NetworkManager
}; 