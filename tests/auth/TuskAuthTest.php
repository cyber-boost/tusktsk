<?php
/**
 * 🐘 TuskAuth Unit Tests
 * ======================
 * Comprehensive test suite for OAuth2/OIDC authentication system
 */

require_once __DIR__ . '/../../lib/TuskAuth.php';
require_once __DIR__ . '/../../lib/TuskDb.php';

use TuskPHP\Auth\TuskAuth;
use PHPUnit\Framework\TestCase;

class TuskAuthTest extends TestCase
{
    private $auth;
    private $testDbPath;

    protected function setUp(): void
    {
        // Create test database
        $this->testDbPath = __DIR__ . '/test_auth.db';
        if (file_exists($this->testDbPath)) {
            unlink($this->testDbPath);
        }
        
        // Configure test OAuth2 settings
        TuskAuth::configure([
            'oauth2' => [
                'client_id' => 'test_client_id',
                'client_secret' => 'test_client_secret',
                'redirect_uri' => 'http://localhost:8080/auth/callback',
                'authorization_endpoint' => 'https://test.auth0.com/authorize',
                'token_endpoint' => 'https://test.auth0.com/oauth/token',
                'userinfo_endpoint' => 'https://test.auth0.com/userinfo',
                'scopes' => ['openid', 'profile', 'email'],
                'pkce_enabled' => true,
                'state_required' => true
            ],
            'session' => [
                'lifetime' => 3600,
                'refresh_threshold' => 300,
                'secure_cookies' => false, // Disable for testing
                'http_only' => true,
                'same_site' => 'Lax'
            ]
        ]);
        
        $this->auth = new TuskAuth();
    }

    protected function tearDown(): void
    {
        // Clean up test database
        if (file_exists($this->testDbPath)) {
            unlink($this->testDbPath);
        }
    }

    /**
     * Test OAuth2 authorization URL generation with PKCE
     */
    public function testGetAuthorizationUrl(): void
    {
        $result = $this->auth->getAuthorizationUrl();
        
        $this->assertIsArray($result);
        $this->assertArrayHasKey('url', $result);
        $this->assertArrayHasKey('state', $result);
        $this->assertArrayHasKey('code_verifier', $result);
        
        // Verify URL contains required parameters
        $this->assertStringContainsString('response_type=code', $result['url']);
        $this->assertStringContainsString('client_id=test_client_id', $result['url']);
        $this->assertStringContainsString('code_challenge=', $result['url']);
        $this->assertStringContainsString('code_challenge_method=S256', $result['url']);
        $this->assertStringContainsString('state=' . $result['state'], $result['url']);
        
        // Verify state and code verifier are valid
        $this->assertMatchesRegularExpression('/^[a-f0-9]{32}$/', $result['state']);
        $this->assertMatchesRegularExpression('/^[A-Za-z0-9\-_]{43,128}$/', $result['code_verifier']);
    }

    /**
     * Test PKCE code verifier and challenge generation
     */
    public function testPkceGeneration(): void
    {
        $reflection = new ReflectionClass($this->auth);
        $generateCodeVerifier = $reflection->getMethod('generateCodeVerifier');
        $generateCodeChallenge = $reflection->getMethod('generateCodeChallenge');
        
        $generateCodeVerifier->setAccessible(true);
        $generateCodeChallenge->setAccessible(true);
        
        $codeVerifier = $generateCodeVerifier->invoke($this->auth);
        $codeChallenge = $generateCodeChallenge->invoke($this->auth, $codeVerifier);
        
        // Verify code verifier format (RFC 7636)
        $this->assertMatchesRegularExpression('/^[A-Za-z0-9\-_]{43,128}$/', $codeVerifier);
        
        // Verify code challenge format
        $this->assertMatchesRegularExpression('/^[A-Za-z0-9\-_]{43}$/', $codeChallenge);
        
        // Verify challenge is derived from verifier
        $expectedChallenge = rtrim(strtr(base64_encode(hash('sha256', $codeVerifier, true)), '+/', '-_'), '=');
        $this->assertEquals($expectedChallenge, $codeChallenge);
    }

    /**
     * Test OAuth2 callback handling with mock data
     */
    public function testHandleCallback(): void
    {
        // First generate authorization URL to get state and code verifier
        $authResult = $this->auth->getAuthorizationUrl();
        
        // Mock OAuth2 callback data
        $code = 'test_authorization_code';
        $state = $authResult['state'];
        
        // Mock successful token exchange response
        $this->mockHttpResponse([
            'access_token' => 'test_access_token',
            'refresh_token' => 'test_refresh_token',
            'id_token' => 'test_id_token',
            'token_type' => 'Bearer',
            'expires_in' => 3600
        ]);
        
        // Mock successful user info response
        $this->mockHttpResponse([
            'sub' => 'test_user_123',
            'email' => 'test@example.com',
            'preferred_username' => 'testuser',
            'given_name' => 'Test',
            'family_name' => 'User',
            'picture' => 'https://example.com/avatar.jpg'
        ]);
        
        $result = $this->auth->handleCallback($code, $state);
        
        $this->assertIsArray($result);
        $this->assertArrayHasKey('user', $result);
        $this->assertArrayHasKey('session', $result);
        $this->assertArrayHasKey('tokens', $result);
        
        // Verify user data
        $this->assertEquals('test_user_123', $result['user']['external_id']);
        $this->assertEquals('test@example.com', $result['user']['email']);
        $this->assertEquals('testuser', $result['user']['username']);
        
        // Verify session data
        $this->assertArrayHasKey('session_id', $result['session']);
        $this->assertArrayHasKey('expires_at', $result['session']);
        
        // Verify tokens
        $this->assertEquals('test_access_token', $result['tokens']['access_token']);
        $this->assertEquals('test_refresh_token', $result['tokens']['refresh_token']);
    }

    /**
     * Test session validation
     */
    public function testSessionValidation(): void
    {
        // Create a test user and session
        $this->createTestUser();
        $session = $this->createTestSession();
        
        // Mock session cookie
        $_COOKIE['tusk_session'] = $session['session_id'];
        
        $validSession = $this->auth->validateSession();
        
        $this->assertNotNull($validSession);
        $this->assertEquals($session['session_id'], $validSession['session_id']);
        $this->assertEquals('test@example.com', $validSession['email']);
    }

    /**
     * Test session validation with expired session
     */
    public function testExpiredSessionValidation(): void
    {
        // Create expired session
        $this->createTestUser();
        $session = $this->createTestSession(true); // expired
        
        $_COOKIE['tusk_session'] = $session['session_id'];
        
        $validSession = $this->auth->validateSession();
        
        $this->assertNull($validSession);
    }

    /**
     * Test permission checking
     */
    public function testPermissionChecking(): void
    {
        // Create admin user
        $this->createTestUser(['roles' => json_encode(['admin'])]);
        $session = $this->createTestSession();
        
        $_COOKIE['tusk_session'] = $session['session_id'];
        
        // Admin should have all permissions
        $this->assertTrue($this->auth->hasPermission('read'));
        $this->assertTrue($this->auth->hasPermission('write'));
        $this->assertTrue($this->auth->hasPermission('execute'));
        $this->assertTrue($this->auth->hasPermission('admin'));
    }

    /**
     * Test permission checking for regular user
     */
    public function testRegularUserPermissions(): void
    {
        // Create regular user
        $this->createTestUser(['roles' => json_encode(['user'])]);
        $session = $this->createTestSession();
        
        $_COOKIE['tusk_session'] = $session['session_id'];
        
        // Regular user should only have read permission
        $this->assertTrue($this->auth->hasPermission('read'));
        $this->assertFalse($this->auth->hasPermission('write'));
        $this->assertFalse($this->auth->hasPermission('execute'));
    }

    /**
     * Test permission checking without session
     */
    public function testPermissionWithoutSession(): void
    {
        // No session cookie set
        unset($_COOKIE['tusk_session']);
        
        $this->assertFalse($this->auth->hasPermission('read'));
        $this->assertFalse($this->auth->hasPermission('write'));
    }

    /**
     * Test logout functionality
     */
    public function testLogout(): void
    {
        // Create test user and session
        $this->createTestUser();
        $session = $this->createTestSession();
        
        $_COOKIE['tusk_session'] = $session['session_id'];
        
        // Verify session exists
        $this->assertNotNull($this->auth->validateSession());
        
        // Logout
        $this->auth->logout();
        
        // Verify session is destroyed
        $this->assertNull($this->auth->validateSession());
    }

    /**
     * Test token refresh functionality
     */
    public function testTokenRefresh(): void
    {
        // Create test user and session with expiring token
        $this->createTestUser();
        $session = $this->createTestSession(false, true); // expiring soon
        
        $_COOKIE['tusk_session'] = $session['session_id'];
        
        // Mock successful token refresh response
        $this->mockHttpResponse([
            'access_token' => 'new_access_token',
            'refresh_token' => 'new_refresh_token',
            'token_type' => 'Bearer',
            'expires_in' => 3600
        ]);
        
        // Validate session (should trigger refresh)
        $validSession = $this->auth->validateSession();
        
        $this->assertNotNull($validSession);
        $this->assertEquals('new_access_token', $validSession['access_token']);
    }

    /**
     * Test invalid state parameter handling
     */
    public function testInvalidStateParameter(): void
    {
        $this->expectException(\Exception::class);
        $this->expectExceptionMessage('Invalid state parameter');
        
        $this->auth->handleCallback('test_code', 'invalid_state');
    }

    /**
     * Test database initialization
     */
    public function testDatabaseInitialization(): void
    {
        // Verify tables were created
        $tables = ['tusk_users', 'tusk_sessions', 'tusk_oauth_states', 'tusk_permissions', 'tusk_user_permissions'];
        
        foreach ($tables as $table) {
            $result = $this->auth->getDb()->query("SELECT name FROM sqlite_master WHERE type='table' AND name=?", [$table])->fetch();
            $this->assertNotNull($result, "Table $table should exist");
        }
    }

    /**
     * Test security features
     */
    public function testSecurityFeatures(): void
    {
        // Test state parameter uniqueness
        $states = [];
        for ($i = 0; $i < 100; $i++) {
            $authResult = $this->auth->getAuthorizationUrl();
            $this->assertNotContains($authResult['state'], $states);
            $states[] = $authResult['state'];
        }
        
        // Test code verifier uniqueness
        $verifiers = [];
        for ($i = 0; $i < 100; $i++) {
            $authResult = $this->auth->getAuthorizationUrl();
            $this->assertNotContains($authResult['code_verifier'], $verifiers);
            $verifiers[] = $authResult['code_verifier'];
        }
        
        // Test session ID uniqueness
        $sessionIds = [];
        for ($i = 0; $i < 100; $i++) {
            $this->createTestUser();
            $session = $this->createTestSession();
            $this->assertNotContains($session['session_id'], $sessionIds);
            $sessionIds[] = $session['session_id'];
        }
    }

    /**
     * Helper method to create test user
     */
    private function createTestUser(array $overrides = []): array
    {
        $defaults = [
            'external_id' => 'test_user_123',
            'email' => 'test@example.com',
            'username' => 'testuser',
            'first_name' => 'Test',
            'last_name' => 'User',
            'roles' => json_encode(['user']),
            'permissions' => json_encode(['read'])
        ];
        
        $data = array_merge($defaults, $overrides);
        
        $sql = "INSERT INTO tusk_users (external_id, email, username, first_name, last_name, roles, permissions) 
                VALUES (?, ?, ?, ?, ?, ?, ?)";
        
        $this->auth->getDb()->execute($sql, [
            $data['external_id'],
            $data['email'],
            $data['username'],
            $data['first_name'],
            $data['last_name'],
            $data['roles'],
            $data['permissions']
        ]);
        
        return $this->auth->getDb()->query("SELECT * FROM tusk_users WHERE external_id = ?", [$data['external_id']])->fetch();
    }

    /**
     * Helper method to create test session
     */
    private function createTestSession(bool $expired = false, bool $expiringSoon = false): array
    {
        $user = $this->auth->getDb()->query("SELECT id FROM tusk_users LIMIT 1")->fetch();
        
        $sessionId = bin2hex(random_bytes(32));
        $expiresAt = $expired 
            ? date('Y-m-d H:i:s', time() - 3600) 
            : ($expiringSoon 
                ? date('Y-m-d H:i:s', time() + 100) 
                : date('Y-m-d H:i:s', time() + 3600));
        
        $sql = "INSERT INTO tusk_sessions (user_id, session_id, access_token, refresh_token, expires_at) 
                VALUES (?, ?, ?, ?, ?)";
        
        $this->auth->getDb()->execute($sql, [
            $user['id'],
            $sessionId,
            'test_access_token',
            'test_refresh_token',
            $expiresAt
        ]);
        
        return [
            'session_id' => $sessionId,
            'expires_at' => $expiresAt
        ];
    }

    /**
     * Helper method to mock HTTP responses
     */
    private function mockHttpResponse(array $response): void
    {
        // This would be implemented with a proper HTTP mocking library
        // For now, we'll use a simple approach with curl_mock or similar
        // In a real implementation, you'd use PHPUnit's HTTP mocking capabilities
    }

    /**
     * Helper method to get database instance for testing
     */
    private function getDb()
    {
        $reflection = new ReflectionClass($this->auth);
        $dbProperty = $reflection->getProperty('db');
        $dbProperty->setAccessible(true);
        return $dbProperty->getValue($this->auth);
    }
}

// Run tests if executed directly
if (php_sapi_name() === 'cli') {
    echo "Running TuskAuth Unit Tests...\n";
    
    $test = new TuskAuthTest();
    $test->setUp();
    
    $methods = get_class_methods($test);
    $testMethods = array_filter($methods, function($method) {
        return strpos($method, 'test') === 0;
    });
    
    $passed = 0;
    $failed = 0;
    
    foreach ($testMethods as $method) {
        try {
            $test->setUp();
            $test->$method();
            echo "✓ $method passed\n";
            $passed++;
        } catch (Exception $e) {
            echo "✗ $method failed: " . $e->getMessage() . "\n";
            $failed++;
        }
    }
    
    echo "\nTest Results: $passed passed, $failed failed\n";
    exit($failed > 0 ? 1 : 0);
} 