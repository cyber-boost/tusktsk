/**
 * Advanced Network Communication and Protocol Management
 * Goal 9.1 Implementation
 */

const EventEmitter = require('events');
const net = require('net');
const http = require('http');
const https = require('https');
const { URL } = require('url');

class NetworkCommunication extends EventEmitter {
    constructor(options = {}) {
        super();
        this.connections = new Map();
        this.connectionPool = new Map();
        this.protocols = new Map();
        this.routes = new Map();
        this.maxConnections = options.maxConnections || 100;
        this.connectionTimeout = options.connectionTimeout || 30000;
        this.retryAttempts = options.retryAttempts || 3;
        this.retryDelay = options.retryDelay || 1000;
        
        this.registerBuiltInProtocols();
    }

    /**
     * Register a protocol handler
     */
    registerProtocol(name, handler) {
        if (typeof handler.connect !== 'function' || typeof handler.send !== 'function') {
            throw new Error('Protocol handler must have connect and send methods');
        }

        this.protocols.set(name, {
            handler,
            registeredAt: Date.now(),
            usageCount: 0
        });

        console.log(`✓ Protocol registered: ${name}`);
        this.emit('protocolRegistered', { name });
        
        return true;
    }

    /**
     * Create connection with protocol
     */
    async connect(endpoint, protocol = 'http', options = {}) {
        const protocolHandler = this.protocols.get(protocol);
        if (!protocolHandler) {
            throw new Error(`Protocol '${protocol}' not supported`);
        }

        const connectionId = this.generateConnectionId(endpoint, protocol);
        
        // Check connection pool first
        if (this.connectionPool.has(connectionId)) {
            const pooledConnection = this.connectionPool.get(connectionId);
            if (this.isConnectionValid(pooledConnection)) {
                console.log(`✓ Reusing pooled connection: ${connectionId}`);
                return pooledConnection;
            } else {
                this.connectionPool.delete(connectionId);
            }
        }

        try {
            console.log(`🔌 Connecting to ${endpoint} via ${protocol}...`);
            
            const connection = await this.createConnection(endpoint, protocol, options);
            
            // Store connection
            this.connections.set(connectionId, {
                ...connection,
                endpoint,
                protocol,
                createdAt: Date.now(),
                lastUsed: Date.now(),
                usageCount: 0
            });

            // Add to pool if pooling is enabled
            if (options.pool !== false) {
                this.addToConnectionPool(connectionId, connection);
            }

            console.log(`✓ Connected to ${endpoint} via ${protocol}`);
            this.emit('connectionEstablished', { connectionId, endpoint, protocol });
            
            return connection;
        } catch (error) {
            throw new Error(`Failed to connect to ${endpoint}: ${error.message}`);
        }
    }

    /**
     * Send data through connection
     */
    async send(connectionId, data, options = {}) {
        const connection = this.connections.get(connectionId);
        if (!connection) {
            throw new Error(`Connection '${connectionId}' not found`);
        }

        try {
            // Update usage statistics
            connection.lastUsed = Date.now();
            connection.usageCount++;
            this.connections.set(connectionId, connection);

            const protocolHandler = this.protocols.get(connection.protocol);
            const result = await protocolHandler.handler.send(connection, data, options);

            console.log(`✓ Data sent via ${connectionId}`);
            this.emit('dataSent', { connectionId, dataSize: data.length, result });
            
            return result;
        } catch (error) {
            this.emit('sendError', { connectionId, error: error.message });
            throw new Error(`Failed to send data via ${connectionId}: ${error.message}`);
        }
    }

    /**
     * Create connection with retry logic
     */
    async createConnection(endpoint, protocol, options) {
        let lastError;
        
        for (let attempt = 1; attempt <= this.retryAttempts; attempt++) {
            try {
                const protocolHandler = this.protocols.get(protocol);
                const connection = await protocolHandler.handler.connect(endpoint, options);
                
                return {
                    id: this.generateConnectionId(endpoint, protocol),
                    endpoint,
                    protocol,
                    connection,
                    status: 'connected'
                };
            } catch (error) {
                lastError = error;
                console.log(`⚠️ Connection attempt ${attempt} failed: ${error.message}`);
                
                if (attempt < this.retryAttempts) {
                    await this.delay(this.retryDelay * attempt);
                }
            }
        }
        
        throw lastError;
    }

    /**
     * Add connection to pool
     */
    addToConnectionPool(connectionId, connection) {
        if (this.connectionPool.size >= this.maxConnections) {
            // Remove oldest connection
            const oldestKey = this.connectionPool.keys().next().value;
            this.connectionPool.delete(oldestKey);
        }

        this.connectionPool.set(connectionId, {
            ...connection,
            pooledAt: Date.now()
        });
    }

    /**
     * Check if connection is still valid
     */
    isConnectionValid(connection) {
        const age = Date.now() - connection.pooledAt;
        return age < this.connectionTimeout && connection.status === 'connected';
    }

    /**
     * Register built-in protocols
     */
    registerBuiltInProtocols() {
        // HTTP Protocol
        this.registerProtocol('http', {
            connect: async (endpoint, options) => {
                const url = new URL(endpoint.startsWith('http') ? endpoint : `http://${endpoint}`);
                
                return new Promise((resolve, reject) => {
                    const req = http.request({
                        hostname: url.hostname,
                        port: url.port || 80,
                        path: url.pathname + url.search,
                        method: 'GET',
                        timeout: options.timeout || this.connectionTimeout
                    }, (res) => {
                        resolve({
                            statusCode: res.statusCode,
                            headers: res.headers,
                            readable: res
                        });
                    });

                    req.on('error', reject);
                    req.on('timeout', () => reject(new Error('Connection timeout')));
                    req.end();
                });
            },
            send: async (connection, data, options) => {
                const url = new URL(connection.endpoint.startsWith('http') ? connection.endpoint : `http://${connection.endpoint}`);
                
                return new Promise((resolve, reject) => {
                    const req = http.request({
                        hostname: url.hostname,
                        port: url.port || 80,
                        path: url.pathname + url.search,
                        method: options.method || 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Content-Length': Buffer.byteLength(data),
                            ...options.headers
                        },
                        timeout: options.timeout || this.connectionTimeout
                    }, (res) => {
                        let responseData = '';
                        res.on('data', chunk => responseData += chunk);
                        res.on('end', () => {
                            resolve({
                                statusCode: res.statusCode,
                                headers: res.headers,
                                data: responseData
                            });
                        });
                    });

                    req.on('error', reject);
                    req.on('timeout', () => reject(new Error('Request timeout')));
                    req.write(data);
                    req.end();
                });
            }
        });

        // HTTPS Protocol
        this.registerProtocol('https', {
            connect: async (endpoint, options) => {
                const url = new URL(endpoint.startsWith('https') ? endpoint : `https://${endpoint}`);
                
                return new Promise((resolve, reject) => {
                    const req = https.request({
                        hostname: url.hostname,
                        port: url.port || 443,
                        path: url.pathname + url.search,
                        method: 'GET',
                        timeout: options.timeout || this.connectionTimeout,
                        rejectUnauthorized: options.rejectUnauthorized !== false
                    }, (res) => {
                        resolve({
                            statusCode: res.statusCode,
                            headers: res.headers,
                            readable: res
                        });
                    });

                    req.on('error', reject);
                    req.on('timeout', () => reject(new Error('Connection timeout')));
                    req.end();
                });
            },
            send: async (connection, data, options) => {
                const url = new URL(connection.endpoint.startsWith('https') ? connection.endpoint : `https://${connection.endpoint}`);
                
                return new Promise((resolve, reject) => {
                    const req = https.request({
                        hostname: url.hostname,
                        port: url.port || 443,
                        path: url.pathname + url.search,
                        method: options.method || 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Content-Length': Buffer.byteLength(data),
                            ...options.headers
                        },
                        timeout: options.timeout || this.connectionTimeout,
                        rejectUnauthorized: options.rejectUnauthorized !== false
                    }, (res) => {
                        let responseData = '';
                        res.on('data', chunk => responseData += chunk);
                        res.on('end', () => {
                            resolve({
                                statusCode: res.statusCode,
                                headers: res.headers,
                                data: responseData
                            });
                        });
                    });

                    req.on('error', reject);
                    req.on('timeout', () => reject(new Error('Request timeout')));
                    req.write(data);
                    req.end();
                });
            }
        });

        // TCP Protocol
        this.registerProtocol('tcp', {
            connect: async (endpoint, options) => {
                const [host, port] = endpoint.split(':');
                
                return new Promise((resolve, reject) => {
                    const socket = net.createConnection({
                        host,
                        port: parseInt(port),
                        timeout: options.timeout || this.connectionTimeout
                    }, () => {
                        resolve({
                            socket,
                            remoteAddress: socket.remoteAddress,
                            remotePort: socket.remotePort
                        });
                    });

                    socket.on('error', reject);
                    socket.on('timeout', () => reject(new Error('Connection timeout')));
                });
            },
            send: async (connection, data, options) => {
                return new Promise((resolve, reject) => {
                    const socket = connection.connection.socket;
                    
                    socket.write(data, (error) => {
                        if (error) {
                            reject(error);
                        } else {
                            resolve({
                                bytesWritten: data.length,
                                timestamp: Date.now()
                            });
                        }
                    });
                });
            }
        });
    }

    /**
     * Add routing rule
     */
    addRoute(pattern, handler, options = {}) {
        this.routes.set(pattern, {
            handler,
            priority: options.priority || 0,
            registeredAt: Date.now()
        });

        console.log(`✓ Route registered: ${pattern}`);
        this.emit('routeRegistered', { pattern });
        
        return true;
    }

    /**
     * Route request through appropriate handler
     */
    async routeRequest(endpoint, data, options = {}) {
        // Find matching route
        let bestMatch = null;
        let bestPriority = -1;

        for (const [pattern, route] of this.routes) {
            if (this.matchesPattern(endpoint, pattern) && route.priority > bestPriority) {
                bestMatch = route;
                bestPriority = route.priority;
            }
        }

        if (!bestMatch) {
            throw new Error(`No route found for endpoint: ${endpoint}`);
        }

        return await bestMatch.handler(endpoint, data, options);
    }

    /**
     * Check if endpoint matches pattern
     */
    matchesPattern(endpoint, pattern) {
        if (pattern instanceof RegExp) {
            return pattern.test(endpoint);
        }
        
        if (typeof pattern === 'string') {
            return endpoint.includes(pattern);
        }
        
        return false;
    }

    /**
     * Close connection
     */
    async closeConnection(connectionId) {
        const connection = this.connections.get(connectionId);
        if (!connection) {
            return false;
        }

        try {
            if (connection.connection.socket) {
                connection.connection.socket.destroy();
            }
            
            this.connections.delete(connectionId);
            this.connectionPool.delete(connectionId);
            
            console.log(`✓ Connection closed: ${connectionId}`);
            this.emit('connectionClosed', { connectionId });
            
            return true;
        } catch (error) {
            console.error(`Failed to close connection ${connectionId}:`, error.message);
            return false;
        }
    }

    /**
     * Close all connections
     */
    async closeAllConnections() {
        const connectionIds = Array.from(this.connections.keys());
        const results = await Promise.allSettled(
            connectionIds.map(id => this.closeConnection(id))
        );
        
        const closed = results.filter(r => r.status === 'fulfilled' && r.value).length;
        console.log(`✓ Closed ${closed}/${connectionIds.length} connections`);
        
        return closed;
    }

    /**
     * Generate unique connection ID
     */
    generateConnectionId(endpoint, protocol) {
        return `${protocol}_${endpoint.replace(/[^a-zA-Z0-9]/g, '_')}_${Date.now()}`;
    }

    /**
     * Delay utility
     */
    delay(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    /**
     * Get network statistics
     */
    getStats() {
        return {
            activeConnections: this.connections.size,
            pooledConnections: this.connectionPool.size,
            registeredProtocols: this.protocols.size,
            registeredRoutes: this.routes.size,
            maxConnections: this.maxConnections,
            connectionTimeout: this.connectionTimeout
        };
    }
}

module.exports = { NetworkCommunication }; 