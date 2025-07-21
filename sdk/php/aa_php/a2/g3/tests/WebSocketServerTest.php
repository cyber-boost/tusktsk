<?php

namespace TuskLang\Tests\Communication\WebSocket;

use PHPUnit\Framework\TestCase;
use TuskLang\Communication\WebSocket\WebSocketServer;
use TuskLang\Communication\WebSocket\ConnectionManager;
use TuskLang\Communication\WebSocket\RoomManager;
use Ratchet\ConnectionInterface;

/**
 * Test suite for WebSocket Server
 */
class WebSocketServerTest extends TestCase
{
    private WebSocketServer $server;
    private ConnectionManager $connectionManager;
    private RoomManager $roomManager;

    protected function setUp(): void
    {
        $this->server = new WebSocketServer([
            'port' => 8080,
            'host' => '127.0.0.1',
            'max_connections' => 100,
            'message_rate_limit' => 10
        ]);
        
        $this->connectionManager = new ConnectionManager([
            'message_rate_limit' => 10
        ]);
        
        $this->roomManager = new RoomManager([
            'max_room_members' => 50
        ]);
    }

    public function testConnectionLifecycle(): void
    {
        $mockConnection = $this->createMockConnection(1);
        
        // Test connection open
        $this->server->onOpen($mockConnection);
        $stats = $this->server->getStats();
        $this->assertEquals(1, $stats['total_connections']);
        
        // Test connection close
        $this->server->onClose($mockConnection);
        $stats = $this->server->getStats();
        $this->assertEquals(0, $stats['total_connections']);
    }

    public function testRoomManagement(): void
    {
        $connectionId = 1;
        
        // Test joining room
        $result = $this->roomManager->joinRoom($connectionId, 'test-room');
        $this->assertTrue($result);
        
        // Test room info
        $roomInfo = $this->roomManager->getRoomInfo('test-room');
        $this->assertNotNull($roomInfo);
        $this->assertEquals(1, $roomInfo['member_count']);
        
        // Test leaving room
        $result = $this->roomManager->leaveRoom($connectionId, 'test-room');
        $this->assertTrue($result);
        
        // Room should be deleted when empty
        $roomInfo = $this->roomManager->getRoomInfo('test-room');
        $this->assertNull($roomInfo);
    }

    public function testRateLimiting(): void
    {
        $mockConnection = $this->createMockConnection(1);
        $this->connectionManager->addConnection($mockConnection);
        
        // Should allow messages under limit
        for ($i = 0; $i < 5; $i++) {
            $result = $this->connectionManager->checkRateLimit($mockConnection);
            $this->assertTrue($result);
        }
        
        // Should deny messages over limit
        for ($i = 0; $i < 10; $i++) {
            $this->connectionManager->checkRateLimit($mockConnection);
        }
        
        $result = $this->connectionManager->checkRateLimit($mockConnection);
        $this->assertFalse($result);
    }

    public function testUserAuthentication(): void
    {
        $mockConnection = $this->createMockConnection(1);
        $this->connectionManager->addConnection($mockConnection);
        
        // Test setting user
        $user = ['id' => 'user123', 'username' => 'testuser'];
        $this->connectionManager->setUser(1, $user);
        
        $userId = $this->connectionManager->getUserId(1);
        $this->assertEquals('user123', $userId);
        
        // Test getting connection by user ID
        $connection = $this->connectionManager->getConnectionByUserId('user123');
        $this->assertSame($mockConnection, $connection);
    }

    public function testMessageHandling(): void
    {
        $mockConnection = $this->createMockConnection(1);
        
        // Test ping message
        $pingMessage = json_encode(['type' => 'ping']);
        
        // Should not throw exception
        $this->expectNotToPerformAssertions();
        $this->server->onMessage($mockConnection, $pingMessage);
    }

    public function testRoomBroadcasting(): void
    {
        $connectionId1 = 1;
        $connectionId2 = 2;
        
        // Add connections to room
        $this->roomManager->joinRoom($connectionId1, 'broadcast-room');
        $this->roomManager->joinRoom($connectionId2, 'broadcast-room');
        
        $connections = $this->roomManager->getRoomConnections('broadcast-room');
        $this->assertCount(2, $connections);
        $this->assertContains($connectionId1, $connections);
        $this->assertContains($connectionId2, $connections);
    }

    public function testMultipleRoomMembership(): void
    {
        $connectionId = 1;
        
        // Join multiple rooms
        $this->roomManager->joinRoom($connectionId, 'room1');
        $this->roomManager->joinRoom($connectionId, 'room2');
        $this->roomManager->joinRoom($connectionId, 'room3');
        
        $userRooms = $this->roomManager->getUserRooms($connectionId);
        $this->assertCount(3, $userRooms);
        $this->assertContains('room1', $userRooms);
        $this->assertContains('room2', $userRooms);
        $this->assertContains('room3', $userRooms);
    }

    public function testRoomCapacityLimits(): void
    {
        $roomManager = new RoomManager(['max_room_members' => 2]);
        
        // Fill room to capacity
        $roomManager->joinRoom(1, 'limited-room');
        $roomManager->joinRoom(2, 'limited-room');
        
        // Should reject additional members
        $result = $roomManager->joinRoom(3, 'limited-room');
        $this->assertFalse($result);
        
        $roomInfo = $roomManager->getRoomInfo('limited-room');
        $this->assertEquals(2, $roomInfo['member_count']);
    }

    public function testConnectionStatistics(): void
    {
        $mockConnection1 = $this->createMockConnection(1);
        $mockConnection2 = $this->createMockConnection(2);
        
        $this->connectionManager->addConnection($mockConnection1);
        $this->connectionManager->addConnection($mockConnection2);
        
        // Set one user as authenticated
        $this->connectionManager->setUser(1, ['id' => 'user1']);
        
        $stats = $this->connectionManager->getStats();
        $this->assertEquals(2, $stats['total_connections']);
        $this->assertEquals(1, $stats['authenticated_connections']);
        $this->assertEquals(1, $stats['anonymous_connections']);
        $this->assertEquals(1, $stats['unique_users']);
    }

    public function testRoomStatistics(): void
    {
        // Create rooms with different member counts
        $this->roomManager->joinRoom(1, 'room1');
        $this->roomManager->joinRoom(2, 'room1');
        $this->roomManager->joinRoom(3, 'room2');
        
        $stats = $this->roomManager->getStats();
        $this->assertEquals(2, $stats['total_rooms']);
        $this->assertEquals(3, $stats['total_members']);
        $this->assertEquals(1.5, $stats['average_members_per_room']);
    }

    public function testInvalidMessageHandling(): void
    {
        $mockConnection = $this->createMockConnection(1);
        
        // Test invalid JSON
        $invalidMessage = 'invalid json';
        
        $this->expectNotToPerformAssertions();
        $this->server->onMessage($mockConnection, $invalidMessage);
    }

    public function testConnectionCleanup(): void
    {
        $mockConnection = $this->createMockConnection(1);
        $this->connectionManager->addConnection($mockConnection);
        
        // Join rooms
        $this->roomManager->joinRoom(1, 'cleanup-room1');
        $this->roomManager->joinRoom(1, 'cleanup-room2');
        
        // Remove connection
        $this->connectionManager->removeConnection($mockConnection);
        $this->roomManager->removeFromAllRooms(1);
        
        // Rooms should be empty/deleted
        $this->assertNull($this->roomManager->getRoomInfo('cleanup-room1'));
        $this->assertNull($this->roomManager->getRoomInfo('cleanup-room2'));
    }

    /**
     * Create mock connection
     */
    private function createMockConnection(int $resourceId): ConnectionInterface
    {
        $mock = $this->createMock(ConnectionInterface::class);
        $mock->resourceId = $resourceId;
        $mock->method('send')->willReturn(null);
        $mock->method('close')->willReturn(null);
        
        return $mock;
    }
} 