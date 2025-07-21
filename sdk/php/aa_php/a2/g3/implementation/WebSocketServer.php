<?php

namespace TuskLang\Communication\WebSocket;

use Ratchet\MessageComponentInterface;
use Ratchet\ConnectionInterface;
use Ratchet\Server\IoServer;
use Ratchet\Http\HttpServer;
use Ratchet\WebSocket\WsServer;
use React\Socket\Server as SocketServer;

/**
 * Advanced WebSocket Server
 * 
 * Features:
 * - RFC 6455 compliance
 * - Connection lifecycle management
 * - Room/channel broadcasting
 * - User presence tracking
 * - Message acknowledgment
 * - Rate limiting
 */
class WebSocketServer implements MessageComponentInterface
{
    private array $connections = [];
    private array $rooms = [];
    private array $userPresence = [];
    private array $config;
    private ConnectionManager $connectionManager;
    private MessageBroker $messageBroker;
    private RoomManager $roomManager;

    public function __construct(array $config = [])
    {
        $this->config = array_merge([
            'port' => 8080,
            'host' => '0.0.0.0',
            'max_connections' => 1000,
            'heartbeat_interval' => 30,
            'message_rate_limit' => 100,
            'enable_compression' => true,
            'ssl_enabled' => false,
            'ssl_cert' => null,
            'ssl_key' => null
        ], $config);

        $this->connectionManager = new ConnectionManager($this->config);
        $this->messageBroker = new MessageBroker($this->config);
        $this->roomManager = new RoomManager($this->config);
    }

    /**
     * Start WebSocket server
     */
    public function start(): void
    {
        $wsServer = new WsServer($this);
        $httpServer = new HttpServer($wsServer);
        
        if ($this->config['ssl_enabled']) {
            $context = [
                'ssl' => [
                    'local_cert' => $this->config['ssl_cert'],
                    'local_pk' => $this->config['ssl_key'],
                    'verify_peer' => false
                ]
            ];
            $socket = new SocketServer($this->config['host'] . ':' . $this->config['port'], [], $context);
        } else {
            $socket = new SocketServer($this->config['host'] . ':' . $this->config['port']);
        }
        
        $server = new IoServer($httpServer, $socket);
        
        echo "WebSocket server started on {$this->config['host']}:{$this->config['port']}\n";
        $server->run();
    }

    /**
     * New connection established
     */
    public function onOpen(ConnectionInterface $conn): void
    {
        try {
            $this->connectionManager->addConnection($conn);
            $this->connections[$conn->resourceId] = $conn;
            
            echo "New connection: {$conn->resourceId}\n";
            
            // Send welcome message
            $this->send($conn, [
                'type' => 'welcome',
                'connectionId' => $conn->resourceId,
                'timestamp' => time()
            ]);
            
        } catch (\Exception $e) {
            echo "Error on connection open: " . $e->getMessage() . "\n";
            $conn->close();
        }
    }

    /**
     * Message received from client
     */
    public function onMessage(ConnectionInterface $from, $msg): void
    {
        try {
            // Rate limiting
            if (!$this->connectionManager->checkRateLimit($from)) {
                $this->send($from, [
                    'type' => 'error',
                    'message' => 'Rate limit exceeded'
                ]);
                return;
            }

            $data = json_decode($msg, true);
            if (!$data) {
                throw new \InvalidArgumentException('Invalid JSON message');
            }

            $this->handleMessage($from, $data);
            
        } catch (\Exception $e) {
            echo "Error handling message: " . $e->getMessage() . "\n";
            $this->send($from, [
                'type' => 'error',
                'message' => 'Invalid message format'
            ]);
        }
    }

    /**
     * Connection closed
     */
    public function onClose(ConnectionInterface $conn): void
    {
        $this->connectionManager->removeConnection($conn);
        $this->roomManager->removeFromAllRooms($conn->resourceId);
        
        unset($this->connections[$conn->resourceId]);
        
        // Update user presence
        $userId = $this->connectionManager->getUserId($conn->resourceId);
        if ($userId) {
            $this->updateUserPresence($userId, 'offline');
            unset($this->userPresence[$userId]);
        }
        
        echo "Connection {$conn->resourceId} closed\n";
    }

    /**
     * Connection error
     */
    public function onError(ConnectionInterface $conn, \Exception $e): void
    {
        echo "Connection error: " . $e->getMessage() . "\n";
        $conn->close();
    }

    /**
     * Handle incoming message
     */
    private function handleMessage(ConnectionInterface $from, array $data): void
    {
        $type = $data['type'] ?? '';
        
        switch ($type) {
            case 'auth':
                $this->handleAuth($from, $data);
                break;
                
            case 'join_room':
                $this->handleJoinRoom($from, $data);
                break;
                
            case 'leave_room':
                $this->handleLeaveRoom($from, $data);
                break;
                
            case 'message':
                $this->handleMessage($from, $data);
                break;
                
            case 'broadcast':
                $this->handleBroadcast($from, $data);
                break;
                
            case 'private_message':
                $this->handlePrivateMessage($from, $data);
                break;
                
            case 'ping':
                $this->handlePing($from, $data);
                break;
                
            case 'presence':
                $this->handlePresence($from, $data);
                break;
                
            default:
                $this->send($from, [
                    'type' => 'error',
                    'message' => 'Unknown message type: ' . $type
                ]);
        }
    }

    /**
     * Handle authentication
     */
    private function handleAuth(ConnectionInterface $conn, array $data): void
    {
        $token = $data['token'] ?? '';
        
        // Validate JWT token (integrate with your auth system)
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

    /**
     * Handle join room request
     */
    private function handleJoinRoom(ConnectionInterface $conn, array $data): void
    {
        $room = $data['room'] ?? '';
        
        if (!$room) {
            $this->send($conn, [
                'type' => 'error',
                'message' => 'Room name required'
            ]);
            return;
        }
        
        $this->roomManager->joinRoom($conn->resourceId, $room);
        
        // Notify user and room members
        $this->send($conn, [
            'type' => 'room_joined',
            'room' => $room
        ]);
        
        $this->broadcastToRoom($room, [
            'type' => 'user_joined',
            'room' => $room,
            'userId' => $this->connectionManager->getUserId($conn->resourceId)
        ], $conn->resourceId);
    }

    /**
     * Handle leave room request
     */
    private function handleLeaveRoom(ConnectionInterface $conn, array $data): void
    {
        $room = $data['room'] ?? '';
        
        if ($this->roomManager->leaveRoom($conn->resourceId, $room)) {
            $this->send($conn, [
                'type' => 'room_left',
                'room' => $room
            ]);
            
            $this->broadcastToRoom($room, [
                'type' => 'user_left',
                'room' => $room,
                'userId' => $this->connectionManager->getUserId($conn->resourceId)
            ]);
        }
    }

    /**
     * Handle broadcast to room
     */
    private function handleBroadcast(ConnectionInterface $from, array $data): void
    {
        $room = $data['room'] ?? '';
        $message = $data['message'] ?? '';
        
        if (!$room || !$message) {
            $this->send($from, [
                'type' => 'error',
                'message' => 'Room and message required'
            ]);
            return;
        }
        
        $broadcastData = [
            'type' => 'room_message',
            'room' => $room,
            'message' => $message,
            'from' => $this->connectionManager->getUserId($from->resourceId),
            'timestamp' => time()
        ];
        
        $this->broadcastToRoom($room, $broadcastData);
    }

    /**
     * Handle private message
     */
    private function handlePrivateMessage(ConnectionInterface $from, array $data): void
    {
        $targetUserId = $data['to'] ?? '';
        $message = $data['message'] ?? '';
        
        if (!$targetUserId || !$message) {
            $this->send($from, [
                'type' => 'error',
                'message' => 'Target user ID and message required'
            ]);
            return;
        }
        
        $targetConnection = $this->connectionManager->getConnectionByUserId($targetUserId);
        
        if ($targetConnection) {
            $this->send($targetConnection, [
                'type' => 'private_message',
                'message' => $message,
                'from' => $this->connectionManager->getUserId($from->resourceId),
                'timestamp' => time()
            ]);
            
            // Send confirmation to sender
            $this->send($from, [
                'type' => 'message_sent',
                'to' => $targetUserId
            ]);
        } else {
            $this->send($from, [
                'type' => 'error',
                'message' => 'User not online'
            ]);
        }
    }

    /**
     * Handle ping/pong for heartbeat
     */
    private function handlePing(ConnectionInterface $conn, array $data): void
    {
        $this->send($conn, [
            'type' => 'pong',
            'timestamp' => time()
        ]);
    }

    /**
     * Handle presence updates
     */
    private function handlePresence(ConnectionInterface $conn, array $data): void
    {
        $status = $data['status'] ?? 'online';
        $userId = $this->connectionManager->getUserId($conn->resourceId);
        
        if ($userId) {
            $this->updateUserPresence($userId, $status);
        }
    }

    /**
     * Send message to specific connection
     */
    private function send(ConnectionInterface $conn, array $data): void
    {
        try {
            $conn->send(json_encode($data));
        } catch (\Exception $e) {
            echo "Error sending message: " . $e->getMessage() . "\n";
        }
    }

    /**
     * Broadcast message to room
     */
    public function broadcastToRoom(string $room, array $data, ?int $excludeConnectionId = null): void
    {
        $roomConnections = $this->roomManager->getRoomConnections($room);
        
        foreach ($roomConnections as $connectionId) {
            if ($excludeConnectionId && $connectionId === $excludeConnectionId) {
                continue;
            }
            
            if (isset($this->connections[$connectionId])) {
                $this->send($this->connections[$connectionId], $data);
            }
        }
    }

    /**
     * Broadcast to all connections
     */
    public function broadcastToAll(array $data, ?int $excludeConnectionId = null): void
    {
        foreach ($this->connections as $connectionId => $conn) {
            if ($excludeConnectionId && $connectionId === $excludeConnectionId) {
                continue;
            }
            
            $this->send($conn, $data);
        }
    }

    /**
     * Update user presence
     */
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

    /**
     * Validate authentication token
     */
    private function validateToken(string $token): ?array
    {
        // Integrate with your JWT/auth system
        // This is a mock implementation
        if ($token === 'valid_token') {
            return [
                'id' => 'user123',
                'username' => 'testuser',
                'email' => 'test@example.com'
            ];
        }
        
        return null;
    }

    /**
     * Get server statistics
     */
    public function getStats(): array
    {
        return [
            'total_connections' => count($this->connections),
            'total_rooms' => $this->roomManager->getRoomCount(),
            'online_users' => count($this->userPresence),
            'memory_usage' => memory_get_usage(true),
            'uptime' => time() - $_SERVER['REQUEST_TIME']
        ];
    }
} 