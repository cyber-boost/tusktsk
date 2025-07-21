<?php

namespace TuskLang\Communication\WebSocket;

/**
 * WebSocket Room Manager
 * 
 * Manages WebSocket rooms/channels with:
 * - Room creation and management
 * - User room membership
 * - Room broadcasting
 * - Room permissions and moderation
 */
class RoomManager
{
    private array $rooms = [];
    private array $userRooms = [];
    private array $config;

    public function __construct(array $config)
    {
        $this->config = $config;
    }

    /**
     * Join user to room
     */
    public function joinRoom(int $connectionId, string $roomName): bool
    {
        // Create room if it doesn't exist
        if (!isset($this->rooms[$roomName])) {
            $this->rooms[$roomName] = [
                'name' => $roomName,
                'created_at' => time(),
                'members' => [],
                'settings' => [
                    'max_members' => $this->config['max_room_members'] ?? 1000,
                    'public' => true,
                    'moderated' => false
                ]
            ];
        }
        
        $room = &$this->rooms[$roomName];
        
        // Check room capacity
        if (count($room['members']) >= $room['settings']['max_members']) {
            return false;
        }
        
        // Add member to room
        if (!in_array($connectionId, $room['members'])) {
            $room['members'][] = $connectionId;
        }
        
        // Track user's rooms
        if (!isset($this->userRooms[$connectionId])) {
            $this->userRooms[$connectionId] = [];
        }
        
        if (!in_array($roomName, $this->userRooms[$connectionId])) {
            $this->userRooms[$connectionId][] = $roomName;
        }
        
        return true;
    }

    /**
     * Remove user from room
     */
    public function leaveRoom(int $connectionId, string $roomName): bool
    {
        if (!isset($this->rooms[$roomName])) {
            return false;
        }
        
        $room = &$this->rooms[$roomName];
        
        // Remove member from room
        $room['members'] = array_filter($room['members'], fn($id) => $id !== $connectionId);
        
        // Remove room from user's list
        if (isset($this->userRooms[$connectionId])) {
            $this->userRooms[$connectionId] = array_filter(
                $this->userRooms[$connectionId], 
                fn($name) => $name !== $roomName
            );
            
            if (empty($this->userRooms[$connectionId])) {
                unset($this->userRooms[$connectionId]);
            }
        }
        
        // Delete room if empty
        if (empty($room['members'])) {
            unset($this->rooms[$roomName]);
        }
        
        return true;
    }

    /**
     * Remove user from all rooms
     */
    public function removeFromAllRooms(int $connectionId): void
    {
        if (!isset($this->userRooms[$connectionId])) {
            return;
        }
        
        $userRooms = $this->userRooms[$connectionId];
        
        foreach ($userRooms as $roomName) {
            $this->leaveRoom($connectionId, $roomName);
        }
        
        unset($this->userRooms[$connectionId]);
    }

    /**
     * Get room members
     */
    public function getRoomConnections(string $roomName): array
    {
        return $this->rooms[$roomName]['members'] ?? [];
    }

    /**
     * Get user's rooms
     */
    public function getUserRooms(int $connectionId): array
    {
        return $this->userRooms[$connectionId] ?? [];
    }

    /**
     * Check if user is in room
     */
    public function isInRoom(int $connectionId, string $roomName): bool
    {
        return isset($this->rooms[$roomName]) && 
               in_array($connectionId, $this->rooms[$roomName]['members']);
    }

    /**
     * Create room with settings
     */
    public function createRoom(string $roomName, array $settings = []): bool
    {
        if (isset($this->rooms[$roomName])) {
            return false; // Room already exists
        }
        
        $this->rooms[$roomName] = [
            'name' => $roomName,
            'created_at' => time(),
            'members' => [],
            'settings' => array_merge([
                'max_members' => $this->config['max_room_members'] ?? 1000,
                'public' => true,
                'moderated' => false,
                'persistent' => false,
                'password' => null
            ], $settings)
        ];
        
        return true;
    }

    /**
     * Delete room
     */
    public function deleteRoom(string $roomName): bool
    {
        if (!isset($this->rooms[$roomName])) {
            return false;
        }
        
        $room = $this->rooms[$roomName];
        
        // Remove all members from room
        foreach ($room['members'] as $connectionId) {
            $this->leaveRoom($connectionId, $roomName);
        }
        
        unset($this->rooms[$roomName]);
        return true;
    }

    /**
     * Get room information
     */
    public function getRoomInfo(string $roomName): ?array
    {
        if (!isset($this->rooms[$roomName])) {
            return null;
        }
        
        $room = $this->rooms[$roomName];
        
        return [
            'name' => $room['name'],
            'member_count' => count($room['members']),
            'created_at' => $room['created_at'],
            'settings' => $room['settings']
        ];
    }

    /**
     * List all rooms
     */
    public function listRooms(bool $includePrivate = false): array
    {
        $rooms = [];
        
        foreach ($this->rooms as $roomName => $room) {
            if (!$includePrivate && !$room['settings']['public']) {
                continue;
            }
            
            $rooms[] = [
                'name' => $roomName,
                'member_count' => count($room['members']),
                'created_at' => $room['created_at'],
                'public' => $room['settings']['public'],
                'moderated' => $room['settings']['moderated']
            ];
        }
        
        return $rooms;
    }

    /**
     * Update room settings
     */
    public function updateRoomSettings(string $roomName, array $settings): bool
    {
        if (!isset($this->rooms[$roomName])) {
            return false;
        }
        
        $this->rooms[$roomName]['settings'] = array_merge(
            $this->rooms[$roomName]['settings'],
            $settings
        );
        
        return true;
    }

    /**
     * Get room count
     */
    public function getRoomCount(): int
    {
        return count($this->rooms);
    }

    /**
     * Get room statistics
     */
    public function getStats(): array
    {
        $totalRooms = count($this->rooms);
        $totalMembers = array_sum(array_map(fn($room) => count($room['members']), $this->rooms));
        $publicRooms = count(array_filter($this->rooms, fn($room) => $room['settings']['public']));
        $privateRooms = $totalRooms - $publicRooms;
        
        return [
            'total_rooms' => $totalRooms,
            'public_rooms' => $publicRooms,
            'private_rooms' => $privateRooms,
            'total_members' => $totalMembers,
            'average_members_per_room' => $totalRooms > 0 ? $totalMembers / $totalRooms : 0
        ];
    }
} 