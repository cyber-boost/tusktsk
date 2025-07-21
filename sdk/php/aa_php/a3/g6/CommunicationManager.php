<?php

namespace TuskLang\A3\G6;

/**
 * Real-time Communication Manager
 * 
 * Features:
 * - Real-time WebSocket connection management
 * - Room-based message broadcasting
 * - Connection tracking and monitoring
 * - Message queuing and delivery
 * 
 * @version 2.0.0
 * @package TuskLang\A3\G6
 */
class CommunicationManager
{
    private array $connections = [];
    private array $rooms = [];
    private array $messageQueue = [];
    private array $statistics = [];
    private int $nextConnectionId = 1;
    
    public function __construct()
    {
        $this->initializeStats();
    }
    
    /**
     * Register a new connection
     */
    public function registerConnection(mixed $socket, array $metadata = []): int
    {
        $connectionId = $this->nextConnectionId++;
        
        $this->connections[$connectionId] = [
            'id' => $connectionId,
            'socket' => $socket,
            'metadata' => $metadata,
            'rooms' => [],
            'created_at' => time(),
            'last_activity' => time(),
            'messages_sent' => 0,
            'messages_received' => 0,
            'status' => 'active'
        ];
        
        $this->updateStats('connections_registered');
        
        return $connectionId;
    }
    
    /**
     * Remove a connection
     */
    public function removeConnection(int $connectionId): bool
    {
        if (!isset($this->connections[$connectionId])) {
            return false;
        }
        
        // Remove from all rooms
        $connection = $this->connections[$connectionId];
        foreach ($connection['rooms'] as $roomId) {
            $this->leaveRoom($connectionId, $roomId);
        }
        
        unset($this->connections[$connectionId]);
        $this->updateStats('connections_removed');
        
        return true;
    }
    
    /**
     * Join a room
     */
    public function joinRoom(int $connectionId, string $roomId): bool
    {
        if (!isset($this->connections[$connectionId])) {
            return false;
        }
        
        if (!isset($this->rooms[$roomId])) {
            $this->rooms[$roomId] = [
                'id' => $roomId,
                'connections' => [],
                'created_at' => time(),
                'message_count' => 0
            ];
        }
        
        // Add connection to room
        $this->rooms[$roomId]['connections'][] = $connectionId;
        $this->connections[$connectionId]['rooms'][] = $roomId;
        
        $this->updateStats('room_joins');
        
        // Broadcast join event
        $this->broadcastToRoom($roomId, [
            'type' => 'user_joined',
            'connection_id' => $connectionId,
            'room_id' => $roomId
        ], $connectionId);
        
        return true;
    }
    
    /**
     * Leave a room
     */
    public function leaveRoom(int $connectionId, string $roomId): bool
    {
        if (!isset($this->connections[$connectionId]) || !isset($this->rooms[$roomId])) {
            return false;
        }
        
        // Remove connection from room
        $this->rooms[$roomId]['connections'] = array_filter(
            $this->rooms[$roomId]['connections'],
            fn($id) => $id !== $connectionId
        );
        
        // Remove room from connection
        $this->connections[$connectionId]['rooms'] = array_filter(
            $this->connections[$connectionId]['rooms'],
            fn($id) => $id !== $roomId
        );
        
        $this->updateStats('room_leaves');
        
        // Broadcast leave event
        $this->broadcastToRoom($roomId, [
            'type' => 'user_left',
            'connection_id' => $connectionId,
            'room_id' => $roomId
        ], $connectionId);
        
        // Clean up empty rooms
        if (empty($this->rooms[$roomId]['connections'])) {
            unset($this->rooms[$roomId]);
        }
        
        return true;
    }
    
    /**
     * Send message to specific connection
     */
    public function sendToConnection(int $connectionId, array $message): bool
    {
        if (!isset($this->connections[$connectionId])) {
            return false;
        }
        
        $connection = $this->connections[$connectionId];
        
        if ($connection['status'] !== 'active') {
            // Queue message for later delivery
            $this->queueMessage($connectionId, $message);
            return true;
        }
        
        try {
            // In a real implementation, this would send through the actual socket
            $this->simulateSend($connection['socket'], $message);
            
            $this->connections[$connectionId]['messages_sent']++;
            $this->connections[$connectionId]['last_activity'] = time();
            $this->updateStats('messages_sent');
            
            return true;
            
        } catch (\Exception $e) {
            $this->queueMessage($connectionId, $message);
            return false;
        }
    }
    
    /**
     * Broadcast message to room
     */
    public function broadcastToRoom(string $roomId, array $message, int $excludeConnectionId = null): int
    {
        if (!isset($this->rooms[$roomId])) {
            return 0;
        }
        
        $sent = 0;
        $connections = $this->rooms[$roomId]['connections'];
        
        foreach ($connections as $connectionId) {
            if ($excludeConnectionId && $connectionId === $excludeConnectionId) {
                continue;
            }
            
            if ($this->sendToConnection($connectionId, $message)) {
                $sent++;
            }
        }
        
        $this->rooms[$roomId]['message_count']++;
        $this->updateStats('broadcasts_sent');
        
        return $sent;
    }
    
    /**
     * Broadcast to all connections
     */
    public function broadcastToAll(array $message, int $excludeConnectionId = null): int
    {
        $sent = 0;
        
        foreach ($this->connections as $connectionId => $connection) {
            if ($excludeConnectionId && $connectionId === $excludeConnectionId) {
                continue;
            }
            
            if ($this->sendToConnection($connectionId, $message)) {
                $sent++;
            }
        }
        
        $this->updateStats('global_broadcasts');
        
        return $sent;
    }
    
    /**
     * Process message queue for offline connections
     */
    public function processMessageQueue(): int
    {
        $processed = 0;
        
        foreach ($this->messageQueue as $index => $queuedMessage) {
            $connectionId = $queuedMessage['connection_id'];
            
            if (isset($this->connections[$connectionId]) && 
                $this->connections[$connectionId]['status'] === 'active') {
                
                if ($this->sendToConnection($connectionId, $queuedMessage['message'])) {
                    unset($this->messageQueue[$index]);
                    $processed++;
                }
            } else {
                // Remove expired messages
                if (time() - $queuedMessage['queued_at'] > 3600) { // 1 hour
                    unset($this->messageQueue[$index]);
                }
            }
        }
        
        // Reindex array
        $this->messageQueue = array_values($this->messageQueue);
        
        return $processed;
    }
    
    /**
     * Get connection information
     */
    public function getConnection(int $connectionId): ?array
    {
        return $this->connections[$connectionId] ?? null;
    }
    
    /**
     * Get room information
     */
    public function getRoom(string $roomId): ?array
    {
        return $this->rooms[$roomId] ?? null;
    }
    
    /**
     * Get system statistics
     */
    public function getStatistics(): array
    {
        return [
            'connections' => [
                'total' => count($this->connections),
                'active' => count(array_filter($this->connections, fn($c) => $c['status'] === 'active'))
            ],
            'rooms' => [
                'total' => count($this->rooms),
                'average_size' => count($this->rooms) > 0 ? 
                    array_sum(array_map(fn($r) => count($r['connections']), $this->rooms)) / count($this->rooms) : 0
            ],
            'messages' => [
                'queued' => count($this->messageQueue),
                'total_sent' => $this->statistics['messages_sent'],
                'broadcasts_sent' => $this->statistics['broadcasts_sent']
            ],
            'activity' => $this->statistics
        ];
    }
    
    /**
     * Clean up inactive connections
     */
    public function cleanupInactiveConnections(int $timeoutSeconds = 300): int
    {
        $cleaned = 0;
        $cutoffTime = time() - $timeoutSeconds;
        
        foreach ($this->connections as $connectionId => $connection) {
            if ($connection['last_activity'] < $cutoffTime) {
                $this->removeConnection($connectionId);
                $cleaned++;
            }
        }
        
        return $cleaned;
    }
    
    private function queueMessage(int $connectionId, array $message): void
    {
        $this->messageQueue[] = [
            'connection_id' => $connectionId,
            'message' => $message,
            'queued_at' => time()
        ];
        
        $this->updateStats('messages_queued');
    }
    
    private function simulateSend(mixed $socket, array $message): void
    {
        // In a real implementation, this would use actual WebSocket sending
        // For simulation purposes, we just record the send
        $encoded = json_encode($message);
        
        if (strlen($encoded) > 1024 * 1024) { // 1MB limit
            throw new \Exception("Message too large");
        }
        
        // Simulate network delay
        usleep(rand(1000, 5000)); // 1-5ms delay
    }
    
    private function initializeStats(): void
    {
        $this->statistics = [
            'connections_registered' => 0,
            'connections_removed' => 0,
            'room_joins' => 0,
            'room_leaves' => 0,
            'messages_sent' => 0,
            'messages_queued' => 0,
            'broadcasts_sent' => 0,
            'global_broadcasts' => 0
        ];
    }
    
    private function updateStats(string $stat): void
    {
        if (isset($this->statistics[$stat])) {
            $this->statistics[$stat]++;
        }
    }
} 