<?php

namespace TuskLang\Communication\WebSocket;

use Ratchet\ConnectionInterface;

/**
 * WebSocket Connection Manager
 * 
 * Manages WebSocket connections with:
 * - Connection tracking and user mapping
 * - Rate limiting per connection
 * - Connection state management
 * - User authentication tracking
 */
class ConnectionManager
{
    private array $connections = [];
    private array $userConnections = [];
    private array $rateLimits = [];
    private array $config;

    public function __construct(array $config)
    {
        $this->config = $config;
    }

    /**
     * Add new connection
     */
    public function addConnection(ConnectionInterface $conn): void
    {
        $this->connections[$conn->resourceId] = [
            'connection' => $conn,
            'user_id' => null,
            'connected_at' => time(),
            'last_activity' => time(),
            'messages_sent' => 0,
            'rate_limit_tokens' => $this->config['message_rate_limit'] ?? 100
        ];
    }

    /**
     * Remove connection
     */
    public function removeConnection(ConnectionInterface $conn): void
    {
        $resourceId = $conn->resourceId;
        
        if (isset($this->connections[$resourceId])) {
            $userId = $this->connections[$resourceId]['user_id'];
            
            if ($userId && isset($this->userConnections[$userId])) {
                $this->userConnections[$userId] = array_filter(
                    $this->userConnections[$userId],
                    fn($id) => $id !== $resourceId
                );
                
                if (empty($this->userConnections[$userId])) {
                    unset($this->userConnections[$userId]);
                }
            }
            
            unset($this->connections[$resourceId]);
            unset($this->rateLimits[$resourceId]);
        }
    }

    /**
     * Set user for connection
     */
    public function setUser(int $resourceId, array $user): void
    {
        if (isset($this->connections[$resourceId])) {
            $userId = $user['id'];
            $this->connections[$resourceId]['user_id'] = $userId;
            
            if (!isset($this->userConnections[$userId])) {
                $this->userConnections[$userId] = [];
            }
            
            $this->userConnections[$userId][] = $resourceId;
        }
    }

    /**
     * Get user ID for connection
     */
    public function getUserId(int $resourceId): ?string
    {
        return $this->connections[$resourceId]['user_id'] ?? null;
    }

    /**
     * Get connection by user ID
     */
    public function getConnectionByUserId(string $userId): ?ConnectionInterface
    {
        if (isset($this->userConnections[$userId]) && !empty($this->userConnections[$userId])) {
            $resourceId = $this->userConnections[$userId][0];
            return $this->connections[$resourceId]['connection'] ?? null;
        }
        
        return null;
    }

    /**
     * Get all connections for user
     */
    public function getUserConnections(string $userId): array
    {
        if (!isset($this->userConnections[$userId])) {
            return [];
        }
        
        $connections = [];
        foreach ($this->userConnections[$userId] as $resourceId) {
            if (isset($this->connections[$resourceId])) {
                $connections[] = $this->connections[$resourceId]['connection'];
            }
        }
        
        return $connections;
    }

    /**
     * Check rate limit for connection
     */
    public function checkRateLimit(ConnectionInterface $conn): bool
    {
        $resourceId = $conn->resourceId;
        
        if (!isset($this->connections[$resourceId])) {
            return false;
        }
        
        $now = time();
        $window = 60; // 1 minute window
        $limit = $this->config['message_rate_limit'] ?? 100;
        
        if (!isset($this->rateLimits[$resourceId])) {
            $this->rateLimits[$resourceId] = [];
        }
        
        // Clean old entries
        $this->rateLimits[$resourceId] = array_filter(
            $this->rateLimits[$resourceId],
            fn($timestamp) => $timestamp > ($now - $window)
        );
        
        if (count($this->rateLimits[$resourceId]) >= $limit) {
            return false;
        }
        
        $this->rateLimits[$resourceId][] = $now;
        $this->connections[$resourceId]['last_activity'] = $now;
        $this->connections[$resourceId]['messages_sent']++;
        
        return true;
    }

    /**
     * Update connection activity
     */
    public function updateActivity(int $resourceId): void
    {
        if (isset($this->connections[$resourceId])) {
            $this->connections[$resourceId]['last_activity'] = time();
        }
    }

    /**
     * Get connection info
     */
    public function getConnectionInfo(int $resourceId): ?array
    {
        return $this->connections[$resourceId] ?? null;
    }

    /**
     * Get all connections
     */
    public function getAllConnections(): array
    {
        return array_map(fn($conn) => $conn['connection'], $this->connections);
    }

    /**
     * Get connection statistics
     */
    public function getStats(): array
    {
        $totalConnections = count($this->connections);
        $authenticatedConnections = count(array_filter($this->connections, fn($conn) => $conn['user_id'] !== null));
        $uniqueUsers = count($this->userConnections);
        
        return [
            'total_connections' => $totalConnections,
            'authenticated_connections' => $authenticatedConnections,
            'anonymous_connections' => $totalConnections - $authenticatedConnections,
            'unique_users' => $uniqueUsers,
            'average_messages_per_connection' => $totalConnections > 0 ? 
                array_sum(array_column($this->connections, 'messages_sent')) / $totalConnections : 0
        ];
    }
} 