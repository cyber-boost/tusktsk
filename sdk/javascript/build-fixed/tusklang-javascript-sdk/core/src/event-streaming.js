/**
 * TuskLang Real-time Event Streaming and WebSocket Integration
 * Provides comprehensive real-time communication capabilities
 */

const { EventEmitter } = require('events');

class EventStream {
  constructor(options = {}) {
    this.options = {
      maxListeners: options.maxListeners || 100,
      bufferSize: options.bufferSize || 1000,
      retryAttempts: options.retryAttempts || 3,
      retryDelay: options.retryDelay || 1000,
      ...options
    };
    
    this.eventEmitter = new EventEmitter();
    this.eventBuffer = [];
    this.subscribers = new Map();
    this.connections = new Map();
    this.messageQueue = [];
    this.isRunning = false;
    
    this.eventEmitter.setMaxListeners(this.options.maxListeners);
  }

  /**
   * Publish an event to all subscribers
   */
  publish(event, data, metadata = {}) {
    const eventMessage = {
      id: this.generateEventId(),
      event,
      data,
      metadata: {
        timestamp: new Date().toISOString(),
        source: metadata.source || 'system',
        ...metadata
      }
    };

    // Add to buffer
    this.addToBuffer(eventMessage);

    // Emit to local listeners
    this.eventEmitter.emit(event, eventMessage);

    // Send to all connections
    this.broadcastToConnections(eventMessage);

    return eventMessage.id;
  }

  /**
   * Subscribe to an event
   */
  subscribe(event, callback, options = {}) {
    const subscription = {
      id: this.generateSubscriptionId(),
      event,
      callback,
      options: {
        once: options.once || false,
        priority: options.priority || 100,
        filter: options.filter || null,
        ...options
      }
    };

    this.subscribers.set(subscription.id, subscription);
    this.eventEmitter.on(event, callback);

    return subscription.id;
  }

  /**
   * Unsubscribe from an event
   */
  unsubscribe(subscriptionId) {
    const subscription = this.subscribers.get(subscriptionId);
    if (subscription) {
      this.eventEmitter.off(subscription.event, subscription.callback);
      this.subscribers.delete(subscriptionId);
      return true;
    }
    return false;
  }

  /**
   * Add connection for real-time streaming
   */
  addConnection(connectionId, connection) {
    this.connections.set(connectionId, {
      connection,
      subscribedEvents: new Set(),
      lastActivity: Date.now(),
      metadata: {}
    });
  }

  /**
   * Remove connection
   */
  removeConnection(connectionId) {
    this.connections.delete(connectionId);
  }

  /**
   * Subscribe connection to specific events
   */
  subscribeConnection(connectionId, events) {
    const connection = this.connections.get(connectionId);
    if (connection) {
      events.forEach(event => connection.subscribedEvents.add(event));
      connection.lastActivity = Date.now();
    }
  }

  /**
   * Broadcast message to all connections
   */
  broadcastToConnections(message) {
    for (const [connectionId, connectionData] of this.connections) {
      if (connectionData.subscribedEvents.has(message.event)) {
        try {
          connectionData.connection.send(JSON.stringify(message));
          connectionData.lastActivity = Date.now();
        } catch (error) {
          console.error(`Error sending to connection ${connectionId}:`, error);
          this.removeConnection(connectionId);
        }
      }
    }
  }

  /**
   * Get event buffer
   */
  getEventBuffer(limit = 100) {
    return this.eventBuffer.slice(-limit);
  }

  /**
   * Clear event buffer
   */
  clearEventBuffer() {
    this.eventBuffer = [];
  }

  /**
   * Get statistics
   */
  getStats() {
    return {
      subscribers: this.subscribers.size,
      connections: this.connections.size,
      bufferSize: this.eventBuffer.length,
      maxBufferSize: this.options.bufferSize
    };
  }

  addToBuffer(eventMessage) {
    this.eventBuffer.push(eventMessage);
    
    // Maintain buffer size
    if (this.eventBuffer.length > this.options.bufferSize) {
      this.eventBuffer.shift();
    }
  }

  generateEventId() {
    return `evt_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  generateSubscriptionId() {
    return `sub_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }
}

class WebSocketManager {
  constructor(options = {}) {
    this.options = {
      port: options.port || 8080,
      path: options.path || '/ws',
      heartbeatInterval: options.heartbeatInterval || 30000,
      maxConnections: options.maxConnections || 1000,
      ...options
    };
    
    this.connections = new Map();
    this.eventStream = new EventStream(options.eventStream);
    this.isRunning = false;
    this.server = null;
  }

  /**
   * Start WebSocket server
   */
  async start() {
    if (this.isRunning) {
      throw new Error('WebSocket server is already running');
    }

    try {
      // Simulate WebSocket server creation
      this.server = {
        port: this.options.port,
        path: this.options.path,
        connections: 0
      };

      this.isRunning = true;
      this.startHeartbeat();
      
      console.log(`WebSocket server started on port ${this.options.port}`);
      return this.server;
    } catch (error) {
      throw new Error(`Failed to start WebSocket server: ${error.message}`);
    }
  }

  /**
   * Stop WebSocket server
   */
  async stop() {
    if (!this.isRunning) {
      return;
    }

    this.isRunning = false;
    
    // Close all connections
    for (const [connectionId, connection] of this.connections) {
      await this.closeConnection(connectionId);
    }

    this.connections.clear();
    console.log('WebSocket server stopped');
  }

  /**
   * Handle new connection
   */
  handleConnection(connection) {
    const connectionId = this.generateConnectionId();
    
    connection.id = connectionId;
    connection.isAlive = true;
    connection.subscribedEvents = new Set();
    connection.lastActivity = Date.now();

    this.connections.set(connectionId, connection);
    this.eventStream.addConnection(connectionId, connection);

    // Send welcome message
    this.sendToConnection(connectionId, {
      type: 'welcome',
      connectionId,
      timestamp: new Date().toISOString()
    });

    return connectionId;
  }

  /**
   * Handle connection close
   */
  async closeConnection(connectionId) {
    const connection = this.connections.get(connectionId);
    if (connection) {
      connection.isAlive = false;
      this.connections.delete(connectionId);
      this.eventStream.removeConnection(connectionId);
    }
  }

  /**
   * Send message to specific connection
   */
  sendToConnection(connectionId, message) {
    const connection = this.connections.get(connectionId);
    if (connection && connection.isAlive) {
      try {
        connection.send(JSON.stringify(message));
        connection.lastActivity = Date.now();
        return true;
      } catch (error) {
        console.error(`Error sending to connection ${connectionId}:`, error);
        this.closeConnection(connectionId);
        return false;
      }
    }
    return false;
  }

  /**
   * Broadcast message to all connections
   */
  broadcast(message, filter = null) {
    let sentCount = 0;
    
    for (const [connectionId, connection] of this.connections) {
      if (connection.isAlive && (!filter || filter(connection))) {
        if (this.sendToConnection(connectionId, message)) {
          sentCount++;
        }
      }
    }
    
    return sentCount;
  }

  /**
   * Handle incoming message
   */
  handleMessage(connectionId, message) {
    try {
      const parsedMessage = JSON.parse(message);
      const connection = this.connections.get(connectionId);
      
      if (!connection) {
        return;
      }

      connection.lastActivity = Date.now();

      switch (parsedMessage.type) {
        case 'subscribe':
          this.handleSubscribe(connectionId, parsedMessage);
          break;
        case 'unsubscribe':
          this.handleUnsubscribe(connectionId, parsedMessage);
          break;
        case 'publish':
          this.handlePublish(connectionId, parsedMessage);
          break;
        case 'ping':
          this.sendToConnection(connectionId, { type: 'pong' });
          break;
        default:
          console.warn(`Unknown message type: ${parsedMessage.type}`);
      }
    } catch (error) {
      console.error(`Error handling message from ${connectionId}:`, error);
    }
  }

  /**
   * Handle subscribe message
   */
  handleSubscribe(connectionId, message) {
    const events = Array.isArray(message.events) ? message.events : [message.events];
    this.eventStream.subscribeConnection(connectionId, events);
    
    this.sendToConnection(connectionId, {
      type: 'subscribed',
      events,
      timestamp: new Date().toISOString()
    });
  }

  /**
   * Handle unsubscribe message
   */
  handleUnsubscribe(connectionId, message) {
    const connection = this.connections.get(connectionId);
    if (connection) {
      const events = Array.isArray(message.events) ? message.events : [message.events];
      events.forEach(event => connection.subscribedEvents.delete(event));
    }
    
    this.sendToConnection(connectionId, {
      type: 'unsubscribed',
      events: message.events,
      timestamp: new Date().toISOString()
    });
  }

  /**
   * Handle publish message
   */
  handlePublish(connectionId, message) {
    const eventId = this.eventStream.publish(
      message.event,
      message.data,
      { source: connectionId, ...message.metadata }
    );
    
    this.sendToConnection(connectionId, {
      type: 'published',
      eventId,
      timestamp: new Date().toISOString()
    });
  }

  /**
   * Start heartbeat to keep connections alive
   */
  startHeartbeat() {
    setInterval(() => {
      if (!this.isRunning) return;
      
      const now = Date.now();
      const deadConnections = [];
      
      for (const [connectionId, connection] of this.connections) {
        if (now - connection.lastActivity > this.options.heartbeatInterval * 2) {
          deadConnections.push(connectionId);
        } else {
          this.sendToConnection(connectionId, { type: 'ping' });
        }
      }
      
      // Remove dead connections
      deadConnections.forEach(connectionId => {
        this.closeConnection(connectionId);
      });
    }, this.options.heartbeatInterval);
  }

  /**
   * Get connection statistics
   */
  getStats() {
    return {
      isRunning: this.isRunning,
      totalConnections: this.connections.size,
      maxConnections: this.options.maxConnections,
      eventStreamStats: this.eventStream.getStats()
    };
  }

  /**
   * Get active sessions
   */
  getActiveSessions() {
    const activeSessions = [];
    for (const [connectionId, connection] of this.connections) {
      if (connection.isAlive) {
        activeSessions.push({
          connectionId,
          lastActivity: connection.lastActivity,
          subscribedEvents: Array.from(connection.subscribedEvents)
        });
      }
    }
    return activeSessions;
  }

  generateConnectionId() {
    return `conn_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }
}

class EventProcessor {
  constructor() {
    this.processors = new Map();
    this.pipeline = [];
  }

  /**
   * Register event processor
   */
  registerProcessor(eventType, processor) {
    this.processors.set(eventType, processor);
  }

  /**
   * Add pipeline step
   */
  addPipelineStep(step) {
    this.pipeline.push(step);
  }

  /**
   * Process event through pipeline
   */
  async processEvent(event) {
    let processedEvent = event;
    
    for (const step of this.pipeline) {
      try {
        processedEvent = await step(processedEvent);
      } catch (error) {
        console.error('Pipeline step error:', error);
        break;
      }
    }
    
    return processedEvent;
  }

  /**
   * Get processor for event type
   */
  getProcessor(eventType) {
    return this.processors.get(eventType);
  }
}

module.exports = {
  EventStream,
  WebSocketManager,
  EventProcessor
}; 