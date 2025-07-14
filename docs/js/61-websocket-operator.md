# @websocket Operator - Real-Time Communication

## Overview
The `@websocket` operator enables real-time bidirectional communication in TuskLang configurations, allowing dynamic data exchange between clients and servers.

## TuskLang Syntax

### Basic WebSocket Connection
```tusk
# WebSocket server configuration
websocket_server: {
  port: @websocket.server(8080)
  path: "/ws"
  max_connections: 100
}

# WebSocket client connection
client_connection: @websocket.connect("ws://localhost:8080/ws")
```

### WebSocket with Event Handlers
```tusk
# Real-time chat configuration
chat_system: {
  server: @websocket.server(3000, {
    on_connect: "handle_connection"
    on_message: "process_message"
    on_disconnect: "cleanup_connection"
  })
  
  broadcast: @websocket.broadcast("chat_room", "message")
  send_private: @websocket.send("user_id", "private_message")
}
```

### WebSocket with Authentication
```tusk
# Secure WebSocket with JWT
secure_websocket: {
  server: @websocket.server(8443, {
    auth: @websocket.auth.jwt("secret_key")
    ssl: true
    cert: @file.read("cert.pem")
    key: @file.read("key.pem")
  })
}
```

## JavaScript Integration

### Node.js WebSocket Server
```javascript
const WebSocket = require('ws');
const tusklang = require('@tusklang/core');

// TuskLang configuration
const config = tusklang.parse(`
websocket_config: {
  port: @websocket.server(8080)
  path: "/realtime"
  handlers: {
    on_connect: "handleNewConnection"
    on_message: "processMessage"
    on_disconnect: "handleDisconnect"
  }
}
`);

// WebSocket server implementation
class WebSocketManager {
  constructor(config) {
    this.wss = new WebSocket.Server({
      port: config.websocket_config.port,
      path: config.websocket_config.path
    });
    
    this.setupEventHandlers();
  }
  
  setupEventHandlers() {
    this.wss.on('connection', (ws, req) => {
      console.log('New client connected');
      
      ws.on('message', (data) => {
        this.processMessage(ws, data);
      });
      
      ws.on('close', () => {
        this.handleDisconnect(ws);
      });
    });
  }
  
  processMessage(ws, data) {
    try {
      const message = JSON.parse(data);
      // Process message based on TuskLang configuration
      this.broadcast(message);
    } catch (error) {
      console.error('Message processing error:', error);
    }
  }
  
  broadcast(message) {
    this.wss.clients.forEach(client => {
      if (client.readyState === WebSocket.OPEN) {
        client.send(JSON.stringify(message));
      }
    });
  }
  
  handleDisconnect(ws) {
    console.log('Client disconnected');
  }
}

// Initialize WebSocket server
const wsManager = new WebSocketManager(config);
```

### Browser WebSocket Client
```javascript
// TuskLang configuration for client
const clientConfig = tusklang.parse(`
client_websocket: {
  url: @websocket.connect("ws://localhost:8080/realtime")
  reconnect: true
  max_reconnect_attempts: 5
  reconnect_interval: 3000
}
`);

class WebSocketClient {
  constructor(config) {
    this.config = config.client_websocket;
    this.ws = null;
    this.reconnectAttempts = 0;
    this.connect();
  }
  
  connect() {
    try {
      this.ws = new WebSocket(this.config.url);
      this.setupEventHandlers();
    } catch (error) {
      console.error('WebSocket connection failed:', error);
      this.handleReconnect();
    }
  }
  
  setupEventHandlers() {
    this.ws.onopen = () => {
      console.log('WebSocket connected');
      this.reconnectAttempts = 0;
    };
    
    this.ws.onmessage = (event) => {
      this.handleMessage(event.data);
    };
    
    this.ws.onclose = () => {
      console.log('WebSocket disconnected');
      if (this.config.reconnect) {
        this.handleReconnect();
      }
    };
    
    this.ws.onerror = (error) => {
      console.error('WebSocket error:', error);
    };
  }
  
  handleMessage(data) {
    try {
      const message = JSON.parse(data);
      // Process incoming message
      this.processMessage(message);
    } catch (error) {
      console.error('Message parsing error:', error);
    }
  }
  
  processMessage(message) {
    // Handle different message types
    switch (message.type) {
      case 'chat':
        this.displayChatMessage(message);
        break;
      case 'notification':
        this.showNotification(message);
        break;
      case 'update':
        this.updateUI(message);
        break;
      default:
        console.log('Unknown message type:', message.type);
    }
  }
  
  send(message) {
    if (this.ws && this.ws.readyState === WebSocket.OPEN) {
      this.ws.send(JSON.stringify(message));
    }
  }
  
  handleReconnect() {
    if (this.reconnectAttempts < this.config.max_reconnect_attempts) {
      this.reconnectAttempts++;
      setTimeout(() => {
        console.log(`Reconnecting... Attempt ${this.reconnectAttempts}`);
        this.connect();
      }, this.config.reconnect_interval);
    }
  }
  
  displayChatMessage(message) {
    const chatContainer = document.getElementById('chat-messages');
    const messageElement = document.createElement('div');
    messageElement.className = 'chat-message';
    messageElement.innerHTML = `
      <strong>${message.user}:</strong> ${message.content}
      <small>${new Date(message.timestamp).toLocaleTimeString()}</small>
    `;
    chatContainer.appendChild(messageElement);
    chatContainer.scrollTop = chatContainer.scrollHeight;
  }
  
  showNotification(message) {
    if ('Notification' in window && Notification.permission === 'granted') {
      new Notification(message.title, {
        body: message.body,
        icon: message.icon
      });
    }
  }
  
  updateUI(message) {
    // Update UI elements based on message
    Object.keys(message.data).forEach(key => {
      const element = document.getElementById(key);
      if (element) {
        element.textContent = message.data[key];
      }
    });
  }
}

// Initialize WebSocket client
const wsClient = new WebSocketClient(clientConfig);
```

## Advanced Usage Scenarios

### Real-Time Dashboard
```tusk
# Real-time monitoring dashboard
dashboard_websocket: {
  server: @websocket.server(9000, {
    rooms: ["metrics", "alerts", "logs"]
    compression: true
    max_payload: "1MB"
  })
  
  metrics_room: @websocket.room("metrics", {
    broadcast_interval: "5s"
    data_format: "json"
  })
  
  alert_room: @websocket.room("alerts", {
    priority: "high"
    notification: true
  })
}
```

### Multi-Room Chat System
```tusk
# Advanced chat system with rooms
chat_system: {
  server: @websocket.server(8080)
  
  rooms: {
    general: @websocket.room("general", {
      max_users: 100
      moderators: ["admin1", "admin2"]
    })
    
    tech: @websocket.room("tech", {
      max_users: 50
      topic: "Technology Discussion"
    })
    
    private: @websocket.room("private", {
      password_protected: true
      invite_only: true
    })
  }
  
  features: {
    typing_indicators: true
    read_receipts: true
    file_sharing: true
    voice_messages: true
  }
}
```

### Gaming Real-Time Updates
```tusk
# Real-time gaming configuration
gaming_websocket: {
  server: @websocket.server(7777, {
    game_rooms: true
    state_sync: "60fps"
    lag_compensation: true
  })
  
  game_state: @websocket.broadcast("game_room", {
    player_positions: true
    game_events: true
    score_updates: true
  })
  
  matchmaking: @websocket.queue("players", {
    max_wait_time: "30s"
    skill_based: true
  })
}
```

## TypeScript Implementation

### WebSocket Manager with TypeScript
```typescript
interface WebSocketConfig {
  port: number;
  path: string;
  handlers: {
    on_connect: string;
    on_message: string;
    on_disconnect: string;
  };
}

interface WebSocketMessage {
  type: string;
  data: any;
  timestamp: number;
  userId?: string;
}

interface ChatMessage extends WebSocketMessage {
  type: 'chat';
  data: {
    user: string;
    content: string;
    room: string;
  };
}

interface NotificationMessage extends WebSocketMessage {
  type: 'notification';
  data: {
    title: string;
    body: string;
    icon?: string;
  };
}

class TypedWebSocketManager {
  private wss: WebSocket.Server;
  private config: WebSocketConfig;
  private rooms: Map<string, Set<WebSocket>> = new Map();
  
  constructor(config: WebSocketConfig) {
    this.config = config;
    this.wss = new WebSocket.Server({
      port: config.port,
      path: config.path
    });
    
    this.setupEventHandlers();
  }
  
  private setupEventHandlers(): void {
    this.wss.on('connection', (ws: WebSocket, req: any) => {
      this.handleConnection(ws, req);
    });
  }
  
  private handleConnection(ws: WebSocket, req: any): void {
    console.log('New client connected');
    
    ws.on('message', (data: WebSocket.Data) => {
      this.processMessage(ws, data);
    });
    
    ws.on('close', () => {
      this.handleDisconnect(ws);
    });
  }
  
  private processMessage(ws: WebSocket, data: WebSocket.Data): void {
    try {
      const message: WebSocketMessage = JSON.parse(data.toString());
      this.routeMessage(ws, message);
    } catch (error) {
      console.error('Message processing error:', error);
    }
  }
  
  private routeMessage(ws: WebSocket, message: WebSocketMessage): void {
    switch (message.type) {
      case 'join_room':
        this.joinRoom(ws, message.data.room);
        break;
      case 'leave_room':
        this.leaveRoom(ws, message.data.room);
        break;
      case 'chat':
        this.broadcastToRoom(message.data.room, message);
        break;
      case 'private_message':
        this.sendPrivateMessage(message.data.userId, message);
        break;
      default:
        console.log('Unknown message type:', message.type);
    }
  }
  
  private joinRoom(ws: WebSocket, room: string): void {
    if (!this.rooms.has(room)) {
      this.rooms.set(room, new Set());
    }
    this.rooms.get(room)!.add(ws);
    
    // Send confirmation
    ws.send(JSON.stringify({
      type: 'room_joined',
      data: { room },
      timestamp: Date.now()
    }));
  }
  
  private leaveRoom(ws: WebSocket, room: string): void {
    const roomSet = this.rooms.get(room);
    if (roomSet) {
      roomSet.delete(ws);
      if (roomSet.size === 0) {
        this.rooms.delete(room);
      }
    }
  }
  
  private broadcastToRoom(room: string, message: WebSocketMessage): void {
    const roomSet = this.rooms.get(room);
    if (roomSet) {
      roomSet.forEach(client => {
        if (client.readyState === WebSocket.OPEN) {
          client.send(JSON.stringify(message));
        }
      });
    }
  }
  
  private sendPrivateMessage(userId: string, message: WebSocketMessage): void {
    // Implementation for private messaging
    // Would need user-to-connection mapping
  }
  
  private handleDisconnect(ws: WebSocket): void {
    console.log('Client disconnected');
    // Remove from all rooms
    this.rooms.forEach((roomSet, room) => {
      roomSet.delete(ws);
    });
  }
  
  public broadcast(message: WebSocketMessage): void {
    this.wss.clients.forEach(client => {
      if (client.readyState === WebSocket.OPEN) {
        client.send(JSON.stringify(message));
      }
    });
  }
  
  public getRoomStats(): Record<string, number> {
    const stats: Record<string, number> = {};
    this.rooms.forEach((roomSet, room) => {
      stats[room] = roomSet.size;
    });
    return stats;
  }
}

// Usage
const config: WebSocketConfig = {
  port: 8080,
  path: '/ws',
  handlers: {
    on_connect: 'handleConnection',
    on_message: 'processMessage',
    on_disconnect: 'handleDisconnect'
  }
};

const wsManager = new TypedWebSocketManager(config);
```

## Real-World Examples

### Live Trading Platform
```javascript
// Real-time trading data via WebSocket
const tradingConfig = tusklang.parse(`
trading_websocket: {
  server: @websocket.connect("wss://api.tradingplatform.com/stream")
  subscriptions: ["BTC/USD", "ETH/USD", "AAPL"]
  update_frequency: "100ms"
  data_types: ["price", "volume", "trades"]
}
`);

class TradingWebSocket {
  constructor(config) {
    this.config = config.trading_websocket;
    this.ws = null;
    this.subscriptions = new Set();
    this.priceData = new Map();
    this.connect();
  }
  
  connect() {
    this.ws = new WebSocket(this.config.server);
    this.ws.onopen = () => {
      this.subscribeToSymbols();
    };
    this.ws.onmessage = (event) => {
      this.handleTradingData(JSON.parse(event.data));
    };
  }
  
  subscribeToSymbols() {
    this.config.subscriptions.forEach(symbol => {
      this.ws.send(JSON.stringify({
        action: 'subscribe',
        symbol: symbol
      }));
    });
  }
  
  handleTradingData(data) {
    // Update price displays
    this.updatePriceDisplay(data.symbol, data.price);
    
    // Update charts
    this.updateChart(data.symbol, data);
    
    // Trigger alerts
    this.checkAlerts(data);
  }
  
  updatePriceDisplay(symbol, price) {
    const element = document.getElementById(`price-${symbol}`);
    if (element) {
      const oldPrice = parseFloat(element.textContent);
      element.textContent = price.toFixed(2);
      
      // Add visual feedback
      if (price > oldPrice) {
        element.classList.add('price-up');
      } else if (price < oldPrice) {
        element.classList.add('price-down');
      }
      
      setTimeout(() => {
        element.classList.remove('price-up', 'price-down');
      }, 1000);
    }
  }
}
```

### Collaborative Editor
```javascript
// Real-time collaborative editing
const editorConfig = tusklang.parse(`
collaborative_editor: {
  server: @websocket.connect("ws://localhost:3000/editor")
  document_id: @env("DOCUMENT_ID")
  user_id: @env("USER_ID")
  sync_interval: "100ms"
  conflict_resolution: "operational_transform"
}
`);

class CollaborativeEditor {
  constructor(config) {
    this.config = config.collaborative_editor;
    this.ws = null;
    this.editor = null;
    this.operations = [];
    this.version = 0;
    this.connect();
  }
  
  connect() {
    this.ws = new WebSocket(this.config.server);
    this.ws.onopen = () => {
      this.joinDocument();
    };
    this.ws.onmessage = (event) => {
      this.handleEditorMessage(JSON.parse(event.data));
    };
  }
  
  joinDocument() {
    this.ws.send(JSON.stringify({
      action: 'join_document',
      documentId: this.config.document_id,
      userId: this.config.user_id
    }));
  }
  
  handleEditorMessage(message) {
    switch (message.type) {
      case 'operation':
        this.applyRemoteOperation(message.operation);
        break;
      case 'cursor_update':
        this.updateRemoteCursor(message.userId, message.position);
        break;
      case 'user_joined':
        this.addUserIndicator(message.userId, message.name);
        break;
      case 'user_left':
        this.removeUserIndicator(message.userId);
        break;
    }
  }
  
  applyRemoteOperation(operation) {
    // Apply operational transformation
    const transformedOp = this.transformOperation(operation);
    this.editor.applyOperation(transformedOp);
  }
  
  sendLocalOperation(operation) {
    this.ws.send(JSON.stringify({
      type: 'operation',
      operation: operation,
      version: this.version++
    }));
  }
}
```

## Performance Considerations

### Connection Management
```javascript
// Efficient WebSocket connection pooling
class WebSocketPool {
  constructor(maxConnections = 100) {
    this.maxConnections = maxConnections;
    this.connections = new Map();
    this.connectionQueue = [];
  }
  
  getConnection(url) {
    if (this.connections.has(url)) {
      const connection = this.connections.get(url);
      if (connection.readyState === WebSocket.OPEN) {
        return connection;
      } else {
        this.connections.delete(url);
      }
    }
    
    if (this.connections.size >= this.maxConnections) {
      // Remove oldest connection
      const oldestUrl = this.connections.keys().next().value;
      this.connections.delete(oldestUrl);
    }
    
    const ws = new WebSocket(url);
    this.connections.set(url, ws);
    return ws;
  }
}
```

### Message Batching
```javascript
// Batch WebSocket messages for performance
class MessageBatcher {
  constructor(batchSize = 10, batchTimeout = 100) {
    this.batchSize = batchSize;
    this.batchTimeout = batchTimeout;
    this.messages = [];
    this.timer = null;
  }
  
  addMessage(message) {
    this.messages.push(message);
    
    if (this.messages.length >= this.batchSize) {
      this.flush();
    } else if (!this.timer) {
      this.timer = setTimeout(() => this.flush(), this.batchTimeout);
    }
  }
  
  flush() {
    if (this.timer) {
      clearTimeout(this.timer);
      this.timer = null;
    }
    
    if (this.messages.length > 0) {
      this.sendBatch(this.messages);
      this.messages = [];
    }
  }
  
  sendBatch(messages) {
    // Send batched messages
    this.ws.send(JSON.stringify({
      type: 'batch',
      messages: messages
    }));
  }
}
```

## Security Notes

### WebSocket Security Best Practices
```javascript
// Secure WebSocket implementation
class SecureWebSocket {
  constructor(url, options = {}) {
    this.url = url;
    this.options = {
      token: null,
      encryption: false,
      rateLimit: 100, // messages per minute
      ...options
    };
    
    this.messageCount = 0;
    this.lastReset = Date.now();
    this.connect();
  }
  
  connect() {
    const headers = {};
    if (this.options.token) {
      headers['Authorization'] = `Bearer ${this.options.token}`;
    }
    
    this.ws = new WebSocket(this.url, {
      headers: headers
    });
    
    this.setupSecurityHandlers();
  }
  
  setupSecurityHandlers() {
    this.ws.onmessage = (event) => {
      // Validate incoming messages
      if (!this.validateMessage(event.data)) {
        console.warn('Invalid message received');
        return;
      }
      
      this.handleMessage(event.data);
    };
  }
  
  validateMessage(data) {
    try {
      const message = JSON.parse(data);
      
      // Check rate limiting
      if (!this.checkRateLimit()) {
        return false;
      }
      
      // Validate message structure
      if (!message.type || !message.timestamp) {
        return false;
      }
      
      // Check for suspicious content
      if (this.containsSuspiciousContent(message)) {
        return false;
      }
      
      return true;
    } catch (error) {
      return false;
    }
  }
  
  checkRateLimit() {
    const now = Date.now();
    if (now - this.lastReset > 60000) {
      this.messageCount = 0;
      this.lastReset = now;
    }
    
    if (this.messageCount >= this.options.rateLimit) {
      return false;
    }
    
    this.messageCount++;
    return true;
  }
  
  containsSuspiciousContent(message) {
    const suspicious = ['script', 'eval', 'function', 'alert'];
    const content = JSON.stringify(message).toLowerCase();
    
    return suspicious.some(term => content.includes(term));
  }
}
```

## Best Practices

### Error Handling
```javascript
// Comprehensive WebSocket error handling
class RobustWebSocket {
  constructor(url, options = {}) {
    this.url = url;
    this.options = {
      maxRetries: 5,
      retryDelay: 1000,
      heartbeatInterval: 30000,
      ...options
    };
    
    this.retryCount = 0;
    this.heartbeatTimer = null;
    this.connect();
  }
  
  connect() {
    try {
      this.ws = new WebSocket(this.url);
      this.setupEventHandlers();
    } catch (error) {
      this.handleConnectionError(error);
    }
  }
  
  setupEventHandlers() {
    this.ws.onopen = () => {
      console.log('WebSocket connected');
      this.retryCount = 0;
      this.startHeartbeat();
    };
    
    this.ws.onclose = (event) => {
      console.log('WebSocket closed:', event.code, event.reason);
      this.stopHeartbeat();
      
      if (!event.wasClean && this.retryCount < this.options.maxRetries) {
        this.retry();
      }
    };
    
    this.ws.onerror = (error) => {
      console.error('WebSocket error:', error);
      this.handleError(error);
    };
  }
  
  retry() {
    this.retryCount++;
    const delay = this.options.retryDelay * Math.pow(2, this.retryCount - 1);
    
    setTimeout(() => {
      console.log(`Retrying connection (${this.retryCount}/${this.options.maxRetries})`);
      this.connect();
    }, delay);
  }
  
  startHeartbeat() {
    this.heartbeatTimer = setInterval(() => {
      if (this.ws.readyState === WebSocket.OPEN) {
        this.ws.send(JSON.stringify({ type: 'ping', timestamp: Date.now() }));
      }
    }, this.options.heartbeatInterval);
  }
  
  stopHeartbeat() {
    if (this.heartbeatTimer) {
      clearInterval(this.heartbeatTimer);
      this.heartbeatTimer = null;
    }
  }
  
  handleConnectionError(error) {
    console.error('Connection error:', error);
    // Implement fallback or user notification
  }
  
  handleError(error) {
    console.error('WebSocket error:', error);
    // Log error and potentially reconnect
  }
}
```

## Related Topics
- [@http Operator](./50-http-operator.md) - HTTP communication
- [@cache Operator](./46-cache-operator.md) - Caching strategies
- [@metrics Operator](./47-metrics-operator.md) - Performance monitoring
- [Real-Time Applications](./29-websockets.md) - WebSocket patterns
- [Event-Driven Architecture](./27-event-driven.md) - Event handling 