# 🐘 TuskLang OAuth2/OIDC Integration Guide

## Overview

TuskLang now includes enterprise-grade OAuth2/OIDC authentication and authorization capabilities. This integration provides secure, standards-compliant authentication with support for PKCE (Proof Key for Code Exchange), role-based access control (RBAC), and comprehensive session management.

## Features

- **OAuth2/OIDC Standard Flows**: Full support for authorization code flow with PKCE
- **PKCE Security**: Enhanced security for public clients
- **Token Management**: Automatic token refresh and management
- **Role-Based Access Control**: Granular permission system
- **Session Management**: Secure session handling with configurable lifetimes
- **Enterprise Security**: Industry-standard security practices
- **CLI Integration**: Complete command-line interface for authentication management

## Quick Start

### 1. Setup OAuth2 Provider

```bash
# Configure OAuth2 provider (interactive)
tsk auth setup

# Or configure programmatically
tsk auth config
```

### 2. Start Authentication Flow

```bash
# Generate authorization URL
tsk auth login
```

### 3. Handle Callback

```bash
# Process OAuth2 callback
tsk auth callback <code> <state>
```

### 4. Check Status

```bash
# Verify authentication status
tsk auth status
```

## Configuration

### OAuth2 Settings

The OAuth2 configuration is managed through the `TuskAuth` class:

```php
use TuskPHP\Auth\TuskAuth;

TuskAuth::configure([
    'oauth2' => [
        'client_id' => 'your_client_id',
        'client_secret' => 'your_client_secret',
        'redirect_uri' => 'http://localhost:8080/auth/callback',
        'authorization_endpoint' => 'https://your-provider.com/authorize',
        'token_endpoint' => 'https://your-provider.com/oauth/token',
        'userinfo_endpoint' => 'https://your-provider.com/userinfo',
        'scopes' => ['openid', 'profile', 'email'],
        'pkce_enabled' => true,
        'state_required' => true
    ]
]);
```

### Session Configuration

```php
TuskAuth::configure([
    'session' => [
        'lifetime' => 3600, // 1 hour
        'refresh_threshold' => 300, // 5 minutes
        'secure_cookies' => true,
        'http_only' => true,
        'same_site' => 'Lax'
    ]
]);
```

### Role-Based Access Control

```php
TuskAuth::configure([
    'rbac' => [
        'roles' => [
            'admin' => ['*'],
            'developer' => ['read', 'write', 'execute'],
            'user' => ['read'],
            'guest' => ['read']
        ],
        'permissions' => [
            'read' => ['view', 'list', 'get'],
            'write' => ['create', 'update', 'delete'],
            'execute' => ['run', 'compile', 'deploy'],
            '*' => ['*']
        ]
    ]
]);
```

## API Reference

### TuskAuth Class

#### Constructor
```php
$auth = new TuskAuth();
```

#### getAuthorizationUrl()
Generates OAuth2 authorization URL with PKCE support.

```php
$result = $auth->getAuthorizationUrl();
// Returns: ['url' => 'https://...', 'state' => '...', 'code_verifier' => '...']
```

#### handleCallback($code, $state)
Processes OAuth2 callback and exchanges authorization code for tokens.

```php
$result = $auth->handleCallback($code, $state);
// Returns: ['user' => [...], 'session' => [...], 'tokens' => [...]]
```

#### validateSession()
Validates current user session and returns session data.

```php
$session = $auth->validateSession();
// Returns: session data array or null if not authenticated
```

#### hasPermission($permission, $resource = null)
Checks if current user has specific permission.

```php
if ($auth->hasPermission('write')) {
    // User can write
}
```

#### logout()
Logs out current user and destroys session.

```php
$auth->logout();
```

## CLI Commands

### Authentication Commands

```bash
# Start OAuth2 login flow
tsk auth login

# Handle OAuth2 callback
tsk auth callback <code> <state>

# Logout current user
tsk auth logout

# Show authentication status
tsk auth status

# Show OAuth2 configuration
tsk auth config [provider]

# Setup OAuth2 provider
tsk auth setup [provider]

# Test OAuth2 connection
tsk auth test
```

### User Management

```bash
# List all users
tsk auth users

# Manage user permissions
tsk auth permissions grant <user> <permission>
tsk auth permissions revoke <user> <permission>
tsk auth permissions list [user]

# Manage user roles
tsk auth roles assign <user> <role>
tsk auth roles remove <user> <role>
tsk auth roles list [user]
```

## Database Schema

The authentication system creates the following tables:

### tusk_users
```sql
CREATE TABLE tusk_users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    external_id VARCHAR(255) UNIQUE,
    email VARCHAR(255) UNIQUE NOT NULL,
    username VARCHAR(100) UNIQUE,
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    avatar_url TEXT,
    roles TEXT DEFAULT 'user',
    permissions TEXT DEFAULT 'read',
    is_active BOOLEAN DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

### tusk_sessions
```sql
CREATE TABLE tusk_sessions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER,
    session_id VARCHAR(255) UNIQUE,
    access_token TEXT,
    refresh_token TEXT,
    id_token TEXT,
    expires_at DATETIME,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES tusk_users(id)
);
```

### tusk_oauth_states
```sql
CREATE TABLE tusk_oauth_states (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    state VARCHAR(255) UNIQUE,
    code_verifier VARCHAR(255),
    redirect_uri TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    expires_at DATETIME
);
```

## Security Features

### PKCE (Proof Key for Code Exchange)
- Generates cryptographically secure code verifiers
- Uses SHA256 for code challenge generation
- Prevents authorization code interception attacks

### State Parameter
- CSRF protection for OAuth2 flows
- Unique state parameters for each authorization request
- Automatic cleanup of expired states

### Session Security
- Secure, HTTP-only cookies
- Configurable session lifetimes
- Automatic token refresh
- Session invalidation on logout

### Role-Based Access Control
- Granular permission system
- Role inheritance
- Resource-level permissions
- Admin override capabilities

## Testing

Run the comprehensive test suite:

```bash
# Run all authentication tests
./tests/auth/run-auth-tests.sh

# Or run individual test file
php tests/auth/TuskAuthTest.php
```

### Test Coverage

The test suite covers:
- OAuth2 authorization URL generation
- PKCE code verifier and challenge generation
- OAuth2 callback handling
- Session validation
- Permission checking
- Role-based access control
- Token refresh functionality
- Security features
- Database initialization

## Integration Examples

### Web Application Integration

```php
<?php
require_once 'lib/TuskAuth.php';

use TuskPHP\Auth\TuskAuth;

// Initialize authentication
$auth = new TuskAuth();

// Check if user is authenticated
$session = $auth->validateSession();
if (!$session) {
    // Redirect to login
    $authUrl = $auth->getAuthorizationUrl();
    header('Location: ' . $authUrl['url']);
    exit;
}

// Check permissions
if (!$auth->hasPermission('write')) {
    http_response_code(403);
    echo 'Access denied';
    exit;
}

// User is authenticated and authorized
echo 'Welcome, ' . $session['first_name'] . '!';
?>
```

### API Integration

```php
<?php
require_once 'lib/TuskAuth.php';

use TuskPHP\Auth\TuskAuth;

// API endpoint with authentication
function protectedApiEndpoint() {
    $auth = new TuskAuth();
    
    // Validate session
    $session = $auth->validateSession();
    if (!$session) {
        http_response_code(401);
        return ['error' => 'Unauthorized'];
    }
    
    // Check specific permission
    if (!$auth->hasPermission('read', 'api')) {
        http_response_code(403);
        return ['error' => 'Forbidden'];
    }
    
    // Process API request
    return ['data' => 'Protected data'];
}
?>
```

### CLI Integration

```php
<?php
require_once 'lib/TuskAuth.php';

use TuskPHP\Auth\TuskAuth;

// CLI command with authentication
function protectedCliCommand() {
    $auth = new TuskAuth();
    
    // Check authentication
    $session = $auth->validateSession();
    if (!$session) {
        echo "Please login first: tsk auth login\n";
        exit(1);
    }
    
    // Check permissions
    if (!$auth->hasPermission('execute')) {
        echo "Insufficient permissions\n";
        exit(1);
    }
    
    // Execute command
    echo "Command executed successfully\n";
}
?>
```

## Troubleshooting

### Common Issues

1. **"Invalid state parameter"**
   - State parameter expired or invalid
   - Check OAuth2 provider configuration
   - Verify redirect URI matches exactly

2. **"Failed to obtain access token"**
   - Check client ID and secret
   - Verify token endpoint URL
   - Ensure PKCE is properly configured

3. **"User not found"**
   - User not created during OAuth2 callback
   - Check userinfo endpoint configuration
   - Verify user data mapping

4. **Session validation fails**
   - Session expired
   - Database connection issues
   - Cookie configuration problems

### Debug Mode

Enable debug logging:

```php
TuskAuth::configure([
    'debug' => true,
    'log_level' => 'debug'
]);
```

### Logs

Check authentication logs:

```bash
# View authentication logs
tail -f /var/log/tusklang/auth.log

# Check database for issues
tsk db console
```

## Best Practices

1. **Security**
   - Always use HTTPS in production
   - Enable secure cookies
   - Implement proper session management
   - Regular security audits

2. **Configuration**
   - Store sensitive data securely
   - Use environment variables for secrets
   - Regular configuration backups
   - Test configuration changes

3. **Monitoring**
   - Monitor authentication failures
   - Track session usage
   - Log permission checks
   - Alert on security events

4. **Maintenance**
   - Regular database cleanup
   - Update OAuth2 provider settings
   - Review user permissions
   - Backup authentication data

## Support

For issues and questions:

1. Check the troubleshooting section
2. Review test logs
3. Verify configuration
4. Check OAuth2 provider documentation
5. Contact TuskLang support

## Version History

- **v1.0.0**: Initial OAuth2/OIDC integration
  - Basic OAuth2 flows
  - PKCE support
  - Role-based access control
  - CLI integration
  - Comprehensive test suite 