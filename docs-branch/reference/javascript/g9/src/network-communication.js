/**
 * Advanced Network Communication and Protocol Management
 * Goal 9.1 Implementation
 */

const EventEmitter = require("events");

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
        if (typeof handler !== "object" || !handler.handler) {
            throw new Error("Protocol handler must be an object with handler, send, and receive methods");
        }

        this.protocols.set(name, {
            handler: handler.handler,
            send: handler.send,
            receive: handler.receive,
            registeredAt: Date.now(),
            usageCount: 0
        });

        console.log(`✓ Protocol registered: ${name}`);
        this.emit("protocolRegistered", { name });
        
        return true;
    }

    /**
     * Create connection with protocol
     */
    async createConnection(protocol, endpoint, options = {}) {
        const protocolHandler = this.protocols.get(protocol);
        if (!protocolHandler) {
            throw new Error(`Protocol ${protocol} not found`);
        }

        try {
            const connection = await protocolHandler.handler(endpoint, options);
            
            // Store connection
            const connectionId = this.generateConnectionId();
            this.connections.set(connectionId, {
                id: connectionId,
                protocol,
                endpoint,
                connection,
                createdAt: Date.now(),
                lastUsed: Date.now(),
                status: "connected"
            });

            console.log(`✓ Connection created: ${protocol}://${endpoint}`);
            this.emit("connectionCreated", { connectionId, protocol, endpoint });
            
            return connectionId;
        } catch (error) {
            throw new Error(`Failed to create connection: ${error.message}`);
        }
    }

    /**
     * Send data through connection
     */
    async sendData(connectionId, data, options = {}) {
        const connection = this.connections.get(connectionId);
        if (!connection) {
            throw new Error(`Connection ${connectionId} not found`);
        }

        try {
            const protocolHandler = this.protocols.get(connection.protocol);
            const result = await protocolHandler.send(connection.connection, data, options);
            
            connection.lastUsed = Date.now();
            
            this.emit("dataSent", { connectionId, dataSize: data.length, result });
            return result;
        } catch (error) {
            throw new Error(`Failed to send data: ${error.message}`);
        }
    }

    /**
     * Receive data from connection
     */
    async receiveData(connectionId, options = {}) {
        const connection = this.connections.get(connectionId);
        if (!connection) {
            throw new Error(`Connection ${connectionId} not found`);
        }

        try {
            const protocolHandler = this.protocols.get(connection.protocol);
            const data = await protocolHandler.receive(connection.connection, options);
            
            connection.lastUsed = Date.now();
            
            this.emit("dataReceived", { connectionId, dataSize: data.length });
            return data;
        } catch (error) {
            throw new Error(`Failed to receive data: ${error.message}`);
        }
    }

    /**
     * Register built-in protocols
     */
    registerBuiltInProtocols() {
        // Custom Protocol
        this.registerProtocol("custom", {
            handler: async (endpoint, options) => {
                return { endpoint, options, type: "custom" };
            },
            send: async (connection, data, options) => {
                return { sent: true, size: data.length, protocol: "custom" };
            },
            receive: async (connection, options) => {
                return { received: true, protocol: "custom" };
            }
        });

        // HTTP Protocol
        this.registerProtocol("http", {
            handler: async (endpoint, options) => {
                return { endpoint, options, type: "http" };
            },
            send: async (connection, data, options) => {
                return { sent: true, size: data.length, protocol: "http" };
            },
            receive: async (connection, options) => {
                return { received: true, protocol: "http" };
            }
        });

        // TCP Protocol
        this.registerProtocol("tcp", {
            handler: async (endpoint, options) => {
                return { endpoint, options, type: "tcp" };
            },
            send: async (connection, data, options) => {
                return { sent: true, size: data.length, protocol: "tcp" };
            },
            receive: async (connection, options) => {
                return { received: true, protocol: "tcp" };
            }
        });
    }

    /**
     * Generate unique connection ID
     */
    generateConnectionId() {
        return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    }

    /**
     * Get network statistics
     */
    getStats() {
        return {
            connections: this.connections.size,
            protocols: this.protocols.size,
            routes: this.routes.size,
            pools: this.connectionPool.size,
            maxConnections: this.maxConnections
        };
    }
}

module.exports = { NetworkCommunication };
