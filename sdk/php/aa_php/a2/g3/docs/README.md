# WebSocket & Real-time Communication (A2-G3)

## Overview

The WebSocket & Real-time Communication module provides a complete RFC 6455 compliant WebSocket server with advanced features for building real-time applications with rooms, presence tracking, and sophisticated connection management.

## Features

### WebSocket Server
- RFC 6455 compliant WebSocket implementation
- SSL/TLS support for secure connections
- Connection lifecycle management
- Rate limiting and throttling
- Authentication and authorization
- Health monitoring and metrics

### Real-time Communication
- Room/channel management with permissions
- User presence tracking and status updates
- Broadcasting and targeted messaging
- Message acknowledgments and delivery confirmation
- Connection recovery and reconnection handling
- Event-driven architecture

### Advanced Features
- Middleware pipeline for request processing
- Circuit breaker pattern for resilience
- Load balancing and clustering support
- Message queuing and persistence
- Comprehensive logging and monitoring
- Performance optimization and caching

## Quick Start

### Basic WebSocket Server

```php
use TuskLang\Communication\WebSocket\WebSocketServer;

// Create WebSocket server
$server = new WebSocketServer([
    'host' => '0.0.0.0',
    'port' => 8080,
    'ssl_enabled' => false,
    'max_connections' => 1000,
    'message_rate_limit' => 100,
    'heartbeat_interval' => 30
]);

// Start server
$server->start();
```

### SSL/TLS Configuration

```php
$server = new WebSocketServer([
    'host' => '0.0.0.0',
    'port' => 8443,
    'ssl_enabled' => true,
    'ssl_cert' => '/path/to/certificate.pem',
    'ssl_key' => '/path/to/private-key.pem'
]);
```

## Client-Server Communication

### Connection Flow

```javascript
// Client-side JavaScript
const ws = new WebSocket('ws://localhost:8080');

ws.onopen = function() {
    console.log('Connected to WebSocket server');
    
    // Authenticate user
    ws.send(JSON.stringify({
        type: 'auth',
        token: 'your-jwt-token'
    }));
};

ws.onmessage = function(event) {
    const data = JSON.parse(event.data);
    console.log('Received:', data);
};
```

### Message Types

The server handles various message types:

```php
// Authentication
{
    "type": "auth",
    "token": "jwt-token-here"
}

// Join room
{
    "type": "join_room",
    "room": "general"
}

// Send message to room
{
    "type": "broadcast",
    "room": "general",
    "message": "Hello everyone!"
}

// Private message
{
    "type": "private_message",
    "to": "user123",
    "message": "Hello there!"
}

// Update presence
{
    "type": "presence",
    "status": "online|away|busy|offline"
}

// Heartbeat
{
    "type": "ping"
}
```

## Room Management

### Creating and Managing Rooms

```php
use TuskLang\Communication\WebSocket\RoomManager;

$roomManager = new RoomManager([
    'max_room_members' => 1000
]);

// Create room with settings
$roomManager->createRoom('gaming-lobby', [
    'max_members' => 50,
    'public' => true,
    'moderated' => false,
    'password' => null
]);

// Join user to room
$roomManager->joinRoom($connectionId, 'gaming-lobby');

// Get room information
$roomInfo = $roomManager->getRoomInfo('gaming-lobby');
```

### Room Broadcasting

```php
// Broadcast to all members in room
$server->broadcastToRoom('general', [
    'type' => 'room_message',
    'message' => 'Welcome to the room!',
    'from' => 'admin',
    'timestamp' => time()
]);

// Broadcast to all connections except sender
$server->broadcastToRoom('general', $message, $excludeConnectionId);
```

## Connection Management

### User Authentication

```php
use TuskLang\Communication\WebSocket\ConnectionManager;

$connectionManager = new ConnectionManager([
    'message_rate_limit' => 100
]);

// Handle authentication
private function handleAuth(ConnectionInterface $conn, array $data): void
{
    $token = $data['token'] ?? '';
    
    // Validate JWT token
    $user = $this->validateToken($token);
    
    if ($user) {
        $this->connectionManager->setUser($conn->resourceId, $user);
        $this->updateUserPresence($user['id'], 'online');
        
        $this->send($conn, [
            'type' => 'auth_success',
            'user' => $user
        ]);
    } else {
        $this->send($conn, [
            'type' => 'auth_failed',
            'message' => 'Invalid token'
        ]);
    }
}
```

### Rate Limiting

```php
// Check rate limit before processing message
if (!$this->connectionManager->checkRateLimit($conn)) {
    $this->send($conn, [
        'type' => 'error',
        'message' => 'Rate limit exceeded'
    ]);
    return;
}
```

## User Presence Tracking

### Presence Management

```php
// Update user presence
private function updateUserPresence(string $userId, string $status): void
{
    $this->userPresence[$userId] = [
        'status' => $status,
        'last_seen' => time()
    ];
    
    // Broadcast presence update
    $this->broadcastToAll([
        'type' => 'presence_update',
        'userId' => $userId,
        'status' => $status,
        'timestamp' => time()
    ]);
}

// Handle presence update from client
private function handlePresence(ConnectionInterface $conn, array $data): void
{
    $status = $data['status'] ?? 'online';
    $userId = $this->connectionManager->getUserId($conn->resourceId);
    
    if ($userId) {
        $this->updateUserPresence($userId, $status);
    }
}
```

### Presence States

- `online` - User is actively connected
- `away` - User is connected but inactive
- `busy` - User is connected but busy
- `offline` - User is disconnected

## Advanced Features

### Custom Message Handlers

```php
class CustomWebSocketServer extends WebSocketServer
{
    protected function handleMessage(ConnectionInterface $from, array $data): void
    {
        $type = $data['type'] ?? '';
        
        switch ($type) {
            case 'custom_action':
                $this->handleCustomAction($from, $data);
                break;
                
            default:
                parent::handleMessage($from, $data);
        }
    }
    
    private function handleCustomAction(ConnectionInterface $conn, array $data): void
    {
        // Custom message handling logic
        $this->send($conn, [
            'type' => 'custom_response',
            'data' => 'Action processed'
        ]);
    }
}
```

### Middleware Integration

```php
use TuskLang\Communication\WebSocket\Middleware\AuthenticationMiddleware;
use TuskLang\Communication\WebSocket\Middleware\LoggingMiddleware;

$server = new WebSocketServer($config);

// Add middleware
$server->addMiddleware(new AuthenticationMiddleware());
$server->addMiddleware(new LoggingMiddleware());
```

### Connection Recovery

```javascript
// Client-side reconnection logic
class WebSocketClient {
    constructor(url) {
        this.url = url;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.reconnectDelay = 1000;
        this.connect();
    }
    
    connect() {
        this.ws = new WebSocket(this.url);
        
        this.ws.onopen = () => {
            console.log('Connected');
            this.reconnectAttempts = 0;
            this.heartbeat();
        };
        
        this.ws.onclose = () => {
            if (this.reconnectAttempts < this.maxReconnectAttempts) {
                setTimeout(() => {
                    this.reconnectAttempts++;
                    this.connect();
                }, this.reconnectDelay * this.reconnectAttempts);
            }
        };
        
        this.ws.onmessage = (event) => {
            const data = JSON.parse(event.data);
            if (data.type === 'pong') {
                this.schedulePing();
            }
        };
    }
    
    heartbeat() {
        this.send({type: 'ping'});
        this.schedulePing();
    }
    
    schedulePing() {
        setTimeout(() => this.heartbeat(), 30000);
    }
    
    send(data) {
        if (this.ws.readyState === WebSocket.OPEN) {
            this.ws.send(JSON.stringify(data));
        }
    }
}
```

## Performance Optimization

### Connection Pooling

```php
// Configure connection limits
$server = new WebSocketServer([
    'max_connections' => 10000,
    'max_connections_per_ip' => 100,
    'connection_timeout' => 300,
    'idle_timeout' => 60
]);
```

### Message Batching

```php
// Batch messages for efficiency
private function batchBroadcast(array $messages, array $connections): void
{
    $batchedMessage = [
        'type' => 'batch',
        'messages' => $messages,
        'timestamp' => time()
    ];
    
    foreach ($connections as $conn) {
        $this->send($conn, $batchedMessage);
    }
}
```

### Memory Management

```php
// Monitor memory usage
public function getStats(): array
{
    return [
        'total_connections' => count($this->connections),
        'total_rooms' => $this->roomManager->getRoomCount(),
        'online_users' => count($this->userPresence),
        'memory_usage' => memory_get_usage(true),
        'peak_memory' => memory_get_peak_usage(true),
        'uptime' => time() - $_SERVER['REQUEST_TIME']
    ];
}
```

## Security Best Practices

### Authentication & Authorization

```php
// Validate JWT tokens
private function validateToken(string $token): ?array
{
    try {
        $decoded = JWT::decode($token, new Key($this->secretKey, 'HS256'));
        return [
            'id' => $decoded->sub,
            'username' => $decoded->username,
            'roles' => $decoded->roles ?? []
        ];
    } catch (\Exception $e) {
        return null;
    }
}

// Check permissions
private function hasPermission(array $user, string $action, string $room = null): bool
{
    // Implement your permission logic
    return in_array('admin', $user['roles']) || 
           in_array($action, $user['permissions'] ?? []);
}
```

### Input Validation

```php
private function validateMessage(array $data): bool
{
    // Validate message structure
    if (!isset($data['type'])) {
        return false;
    }
    
    // Sanitize content
    if (isset($data['message'])) {
        $data['message'] = htmlspecialchars($data['message'], ENT_QUOTES, 'UTF-8');
    }
    
    // Check message length
    if (isset($data['message']) && strlen($data['message']) > 1000) {
        return false;
    }
    
    return true;
}
```

### Rate Limiting

```php
// Implement sophisticated rate limiting
class RateLimiter
{
    private array $buckets = [];
    
    public function isAllowed(string $identifier, int $limit, int $window): bool
    {
        $now = time();
        $key = $identifier . ':' . floor($now / $window);
        
        if (!isset($this->buckets[$key])) {
            $this->buckets[$key] = 0;
        }
        
        if ($this->buckets[$key] >= $limit) {
            return false;
        }
        
        $this->buckets[$key]++;
        return true;
    }
}
```

## Monitoring and Logging

### Health Checks

```php
// Health check endpoint
public function getHealthStatus(): array
{
    $stats = $this->getStats();
    
    return [
        'status' => 'healthy',
        'connections' => $stats['total_connections'],
        'memory_usage_mb' => round($stats['memory_usage'] / 1024 / 1024, 2),
        'uptime_seconds' => $stats['uptime'],
        'last_check' => time()
    ];
}
```

### Logging

```php
use Psr\Log\LoggerInterface;

class WebSocketServer
{
    private LoggerInterface $logger;
    
    private function logMessage(string $level, string $message, array $context = []): void
    {
        $this->logger->log($level, $message, array_merge($context, [
            'component' => 'websocket',
            'timestamp' => time(),
            'memory_usage' => memory_get_usage(true)
        ]));
    }
}
```

## Testing

### Unit Testing

```php
class WebSocketServerTest extends TestCase
{
    public function testConnectionHandling()
    {
        $server = new WebSocketServer(['port' => 8081]);
        $mockConnection = $this->createMockConnection();
        
        $server->onOpen($mockConnection);
        $stats = $server->getStats();
        
        $this->assertEquals(1, $stats['total_connections']);
    }
    
    public function testRoomManagement()
    {
        $roomManager = new RoomManager(['max_room_members' => 10]);
        
        $result = $roomManager->joinRoom(1, 'test-room');
        $this->assertTrue($result);
        
        $roomInfo = $roomManager->getRoomInfo('test-room');
        $this->assertEquals(1, $roomInfo['member_count']);
    }
}
```

### Integration Testing

```php
class WebSocketIntegrationTest extends TestCase
{
    public function testFullCommunicationFlow()
    {
        // Test complete message flow from client to server and back
        $client = new WebSocketTestClient('ws://localhost:8080');
        
        $client->connect();
        $client->authenticate('test-token');
        $client->joinRoom('test-room');
        $client->sendMessage('Hello World!');
        
        $response = $client->waitForMessage();
        $this->assertEquals('room_message', $response['type']);
    }
}
```

## Related Modules

- **A2-G1**: HTTP/REST integration for WebSocket handshake
- **A2-G2**: GraphQL subscriptions over WebSocket
- **A2-G4**: Message queue integration for scalability
- **A3-G2**: JWT/OAuth authentication
- **A5-G1**: Real-time analytics and monitoring

## Configuration Reference

```php
$config = [
    // Server settings
    'host' => '0.0.0.0',
    'port' => 8080,
    'max_connections' => 1000,
    'heartbeat_interval' => 30,
    'message_rate_limit' => 100,
    
    // SSL/TLS settings
    'ssl_enabled' => false,
    'ssl_cert' => null,
    'ssl_key' => null,
    
    // Room settings
    'max_room_members' => 1000,
    'max_rooms_per_user' => 50,
    
    // Performance settings
    'enable_compression' => true,
    'buffer_size' => 8192,
    'keep_alive_timeout' => 300,
    
    // Security settings
    'enable_origin_check' => true,
    'allowed_origins' => ['https://yourdomain.com'],
    'enable_rate_limiting' => true,
    'max_message_size' => 1024
];
``` 