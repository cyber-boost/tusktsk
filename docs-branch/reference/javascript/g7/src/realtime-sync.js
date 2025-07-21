/**
 * Real-time Configuration Synchronization and Hot Reloading
 * Goal 7.2 Implementation
 */

const EventEmitter = require('events');
const fs = require('fs').promises;
const path = require('path');
const chokidar = require('chokidar');
const WebSocket = require('ws');
const crypto = require('crypto');

class RealtimeSyncManager extends EventEmitter {
    constructor(options = {}) {
        super();
        this.port = options.port || 8080;
        this.watchPaths = options.watchPaths || [];
        this.syncInterval = options.syncInterval || 1000;
        this.maxConnections = options.maxConnections || 100;
        this.encryptionKey = options.encryptionKey || null;
        
        this.connections = new Map();
        this.fileHashes = new Map();
        this.watchers = new Map();
        this.server = null;
        this.syncTimer = null;
        this.isRunning = false;
    }

    /**
     * Start the real-time synchronization system
     */
    async start() {
        try {
            console.log('🚀 Starting real-time configuration synchronization...');
            
            // Start WebSocket server
            await this.startWebSocketServer();
            
            // Initialize file watchers
            await this.initializeFileWatchers();
            
            // Start sync timer
            this.startSyncTimer();
            
            this.isRunning = true;
            console.log(`✓ Real-time sync started on port ${this.port}`);
            
            this.emit('started', { port: this.port, connections: this.connections.size });
        } catch (error) {
            throw new Error(`Failed to start real-time sync: ${error.message}`);
        }
    }

    /**
     * Stop the real-time synchronization system
     */
    async stop() {
        try {
            console.log('🛑 Stopping real-time configuration synchronization...');
            
            // Stop sync timer
            if (this.syncTimer) {
                clearInterval(this.syncTimer);
                this.syncTimer = null;
            }
            
            // Close file watchers
            for (const [path, watcher] of this.watchers) {
                await watcher.close();
            }
            this.watchers.clear();
            
            // Close WebSocket server
            if (this.server) {
                this.server.close();
                this.server = null;
            }
            
            // Close all connections
            for (const [id, connection] of this.connections) {
                connection.close();
            }
            this.connections.clear();
            
            this.isRunning = false;
            console.log('✓ Real-time sync stopped');
            
            this.emit('stopped');
        } catch (error) {
            throw new Error(`Failed to stop real-time sync: ${error.message}`);
        }
    }

    /**
     * Start WebSocket server
     */
    async startWebSocketServer() {
        return new Promise((resolve, reject) => {
            this.server = new WebSocket.Server({ port: this.port }, () => {
                console.log(`✓ WebSocket server started on port ${this.port}`);
                resolve();
            });

            this.server.on('connection', (ws, req) => {
                this.handleConnection(ws, req);
            });

            this.server.on('error', (error) => {
                reject(error);
            });
        });
    }

    /**
     * Handle new WebSocket connection
     */
    handleConnection(ws, req) {
        const connectionId = this.generateConnectionId();
        const clientInfo = {
            id: connectionId,
            ip: req.socket.remoteAddress,
            userAgent: req.headers['user-agent'],
            connectedAt: Date.now(),
            subscriptions: new Set()
        };

        this.connections.set(connectionId, { ws, info: clientInfo });
        
        console.log(`✓ New connection: ${connectionId} from ${clientInfo.ip}`);

        // Send welcome message
        this.sendMessage(connectionId, {
            type: 'welcome',
            connectionId,
            timestamp: Date.now(),
            serverInfo: {
                version: '1.0.0',
                features: ['file-sync', 'hot-reload', 'encryption']
            }
        });

        // Handle incoming messages
        ws.on('message', (data) => {
            try {
                const message = JSON.parse(data.toString());
                this.handleMessage(connectionId, message);
            } catch (error) {
                this.sendError(connectionId, 'Invalid message format');
            }
        });

        // Handle connection close
        ws.on('close', () => {
            this.connections.delete(connectionId);
            console.log(`✗ Connection closed: ${connectionId}`);
            this.emit('connectionClosed', connectionId);
        });

        // Handle connection error
        ws.on('error', (error) => {
            console.error(`✗ Connection error: ${connectionId}`, error.message);
            this.connections.delete(connectionId);
        });

        this.emit('connection', clientInfo);
    }

    /**
     * Handle incoming WebSocket message
     */
    handleMessage(connectionId, message) {
        const connection = this.connections.get(connectionId);
        if (!connection) return;

        switch (message.type) {
            case 'subscribe':
                this.handleSubscribe(connectionId, message.paths || []);
                break;
            case 'unsubscribe':
                this.handleUnsubscribe(connectionId, message.paths || []);
                break;
            case 'request-sync':
                this.handleRequestSync(connectionId, message.path);
                break;
            case 'ping':
                this.sendMessage(connectionId, { type: 'pong', timestamp: Date.now() });
                break;
            default:
                this.sendError(connectionId, `Unknown message type: ${message.type}`);
        }
    }

    /**
     * Handle file subscription
     */
    handleSubscribe(connectionId, paths) {
        const connection = this.connections.get(connectionId);
        if (!connection) return;

        for (const filePath of paths) {
            connection.info.subscriptions.add(filePath);
        }

        this.sendMessage(connectionId, {
            type: 'subscribed',
            paths,
            timestamp: Date.now()
        });

        console.log(`✓ ${connectionId} subscribed to: ${paths.join(', ')}`);
    }

    /**
     * Handle file unsubscription
     */
    handleUnsubscribe(connectionId, paths) {
        const connection = this.connections.get(connectionId);
        if (!connection) return;

        for (const filePath of paths) {
            connection.info.subscriptions.delete(filePath);
        }

        this.sendMessage(connectionId, {
            type: 'unsubscribed',
            paths,
            timestamp: Date.now()
        });

        console.log(`✓ ${connectionId} unsubscribed from: ${paths.join(', ')}`);
    }

    /**
     * Handle sync request
     */
    async handleRequestSync(connectionId, filePath) {
        try {
            const fileContent = await fs.readFile(filePath, 'utf8');
            const fileHash = this.calculateFileHash(fileContent);
            
            this.sendMessage(connectionId, {
                type: 'file-sync',
                path: filePath,
                content: fileContent,
                hash: fileHash,
                timestamp: Date.now()
            });

            console.log(`✓ Sent file sync for ${filePath} to ${connectionId}`);
        } catch (error) {
            this.sendError(connectionId, `Failed to sync file ${filePath}: ${error.message}`);
        }
    }

    /**
     * Initialize file watchers
     */
    async initializeFileWatchers() {
        for (const watchPath of this.watchPaths) {
            const watcher = chokidar.watch(watchPath, {
                persistent: true,
                ignoreInitial: true,
                awaitWriteFinish: {
                    stabilityThreshold: 100,
                    pollInterval: 100
                }
            });

            watcher.on('change', (filePath) => this.handleFileChange(filePath));
            watcher.on('add', (filePath) => this.handleFileChange(filePath));
            watcher.on('unlink', (filePath) => this.handleFileRemoved(filePath));

            this.watchers.set(watchPath, watcher);
            console.log(`✓ Watching: ${watchPath}`);
        }
    }

    /**
     * Handle file change
     */
    async handleFileChange(filePath) {
        try {
            const fileContent = await fs.readFile(filePath, 'utf8');
            const fileHash = this.calculateFileHash(fileContent);
            
            // Check if file actually changed
            const previousHash = this.fileHashes.get(filePath);
            if (previousHash === fileHash) {
                return; // No change
            }

            this.fileHashes.set(filePath, fileHash);
            
            // Notify subscribers
            this.broadcastFileChange(filePath, fileContent, fileHash);
            
            console.log(`✓ File changed: ${filePath}`);
            this.emit('fileChanged', { path: filePath, hash: fileHash });
        } catch (error) {
            console.error(`✗ Error handling file change: ${filePath}`, error.message);
        }
    }

    /**
     * Handle file removal
     */
    handleFileRemoved(filePath) {
        this.fileHashes.delete(filePath);
        this.broadcastFileRemoved(filePath);
        
        console.log(`✓ File removed: ${filePath}`);
        this.emit('fileRemoved', { path: filePath });
    }

    /**
     * Broadcast file change to subscribers
     */
    broadcastFileChange(filePath, content, hash) {
        const message = {
            type: 'file-changed',
            path: filePath,
            content: content,
            hash: hash,
            timestamp: Date.now()
        };

        for (const [connectionId, connection] of this.connections) {
            if (connection.info.subscriptions.has(filePath)) {
                this.sendMessage(connectionId, message);
            }
        }
    }

    /**
     * Broadcast file removal to subscribers
     */
    broadcastFileRemoved(filePath) {
        const message = {
            type: 'file-removed',
            path: filePath,
            timestamp: Date.now()
        };

        for (const [connectionId, connection] of this.connections) {
            if (connection.info.subscriptions.has(filePath)) {
                this.sendMessage(connectionId, message);
            }
        }
    }

    /**
     * Start sync timer for periodic checks
     */
    startSyncTimer() {
        this.syncTimer = setInterval(() => {
            this.performPeriodicSync();
        }, this.syncInterval);
    }

    /**
     * Perform periodic synchronization
     */
    async performPeriodicSync() {
        try {
            for (const watchPath of this.watchPaths) {
                const files = await this.getFilesInDirectory(watchPath);
                for (const file of files) {
                    await this.handleFileChange(file);
                }
            }
        } catch (error) {
            console.error('✗ Periodic sync error:', error.message);
        }
    }

    /**
     * Get all files in directory recursively
     */
    async getFilesInDirectory(dir) {
        const files = [];
        const items = await fs.readdir(dir, { withFileTypes: true });
        
        for (const item of items) {
            const fullPath = path.join(dir, item.name);
            if (item.isDirectory()) {
                files.push(...await this.getFilesInDirectory(fullPath));
            } else {
                files.push(fullPath);
            }
        }
        
        return files;
    }

    /**
     * Send message to specific connection
     */
    sendMessage(connectionId, message) {
        const connection = this.connections.get(connectionId);
        if (!connection || connection.ws.readyState !== WebSocket.OPEN) {
            return;
        }

        try {
            const data = JSON.stringify(message);
            connection.ws.send(data);
        } catch (error) {
            console.error(`✗ Failed to send message to ${connectionId}:`, error.message);
        }
    }

    /**
     * Send error message to connection
     */
    sendError(connectionId, error) {
        this.sendMessage(connectionId, {
            type: 'error',
            error,
            timestamp: Date.now()
        });
    }

    /**
     * Calculate file hash
     */
    calculateFileHash(content) {
        return crypto.createHash('sha256').update(content).digest('hex');
    }

    /**
     * Generate connection ID
     */
    generateConnectionId() {
        return crypto.randomBytes(8).toString('hex');
    }

    /**
     * Get system statistics
     */
    getStats() {
        return {
            isRunning: this.isRunning,
            port: this.port,
            connections: this.connections.size,
            watchedPaths: this.watchPaths.length,
            fileHashes: this.fileHashes.size,
            syncInterval: this.syncInterval
        };
    }
}

module.exports = { RealtimeSyncManager }; 