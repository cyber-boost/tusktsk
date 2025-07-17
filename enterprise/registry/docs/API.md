# TuskLang Package Registry API Documentation

## Base URL

```
https://registry.tusklang.org/api/v1
```

## Authentication

All API endpoints require authentication using JWT tokens unless specified otherwise.

### Getting a Token

```http
POST /auth/token
Content-Type: application/json

{
  "username": "your_username",
  "password": "your_password"
}
```

Response:
```json
{
  "access_token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9...",
  "token_type": "bearer",
  "expires_in": 3600,
  "user": {
    "id": "user_123",
    "username": "your_username",
    "role": "publisher",
    "permissions": ["read_packages", "publish_packages"]
  }
}
```

### Using the Token

Include the token in the Authorization header:

```http
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9...
```

## Package Management

### List Packages

```http
GET /packages
Authorization: Bearer <token>
```

Query Parameters:
- `page` (integer): Page number (default: 1)
- `limit` (integer): Items per page (default: 20, max: 100)
- `sort` (string): Sort field (name, version, created_at, downloads)
- `order` (string): Sort order (asc, desc)
- `search` (string): Search query
- `author` (string): Filter by author
- `license` (string): Filter by license

Response:
```json
{
  "packages": [
    {
      "id": "package_123",
      "name": "my-package",
      "version": "1.0.0",
      "description": "A sample package",
      "author": "author@example.com",
      "license": "MIT",
      "downloads": 1500,
      "created_at": "2025-01-15T10:30:00Z",
      "updated_at": "2025-01-15T10:30:00Z",
      "signature": "verified",
      "size": 1024000
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 20,
    "total": 150,
    "pages": 8
  }
}
```

### Get Package Details

```http
GET /packages/{package_id}
Authorization: Bearer <token>
```

Response:
```json
{
  "id": "package_123",
  "name": "my-package",
  "version": "1.0.0",
  "description": "A sample package",
  "author": "author@example.com",
  "license": "MIT",
  "dependencies": [
    {
      "name": "dependency-package",
      "version": "^1.0.0"
    }
  ],
  "downloads": 1500,
  "created_at": "2025-01-15T10:30:00Z",
  "updated_at": "2025-01-15T10:30:00Z",
  "signature": {
    "verified": true,
    "signed_by": "author@example.com",
    "signed_at": "2025-01-15T10:30:00Z",
    "algorithm": "RSA"
  },
  "size": 1024000,
  "checksums": {
    "sha256": "a1b2c3d4...",
    "sha512": "e5f6g7h8..."
  },
  "metadata": {
    "tags": ["web", "utility"],
    "repository": "https://github.com/author/my-package"
  }
}
```

### Upload Package

```http
POST /packages
Authorization: Bearer <token>
Content-Type: multipart/form-data

Form Data:
- package: Package file (.tsk, .tar.gz, .zip)
- metadata: Package metadata (JSON)
- signature: Digital signature (optional)
```

Metadata JSON:
```json
{
  "name": "my-package",
  "version": "1.0.0",
  "description": "A sample package",
  "license": "MIT",
  "dependencies": [
    {
      "name": "dependency-package",
      "version": "^1.0.0"
    }
  ],
  "tags": ["web", "utility"],
  "repository": "https://github.com/author/my-package"
}
```

Response:
```json
{
  "id": "package_123",
  "status": "uploaded",
  "message": "Package uploaded successfully",
  "validation": {
    "passed": true,
    "warnings": [],
    "errors": []
  },
  "signature": {
    "verified": true,
    "signed_by": "author@example.com"
  }
}
```

### Download Package

```http
GET /packages/{package_id}/download
Authorization: Bearer <token>
```

Response:
- File download with appropriate headers
- Content-Type: application/octet-stream
- Content-Disposition: attachment; filename="package-name.tsk"

### Update Package

```http
PUT /packages/{package_id}
Authorization: Bearer <token>
Content-Type: application/json

{
  "description": "Updated description",
  "license": "Apache-2.0",
  "tags": ["web", "utility", "updated"]
}
```

### Delete Package

```http
DELETE /packages/{package_id}
Authorization: Bearer <token>
```

## User Management

### List Users

```http
GET /users
Authorization: Bearer <admin_token>
```

Query Parameters:
- `page` (integer): Page number
- `limit` (integer): Items per page
- `role` (string): Filter by role
- `active` (boolean): Filter by active status

Response:
```json
{
  "users": [
    {
      "id": "user_123",
      "username": "john_doe",
      "email": "john@example.com",
      "role": "publisher",
      "active": true,
      "created_at": "2025-01-01T00:00:00Z",
      "last_login": "2025-01-15T10:30:00Z",
      "packages_count": 5
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 20,
    "total": 50,
    "pages": 3
  }
}
```

### Create User

```http
POST /users
Authorization: Bearer <admin_token>
Content-Type: application/json

{
  "username": "new_user",
  "email": "newuser@example.com",
  "password": "secure_password",
  "role": "publisher",
  "permissions": ["read_packages", "publish_packages"]
}
```

### Update User

```http
PUT /users/{user_id}
Authorization: Bearer <admin_token>
Content-Type: application/json

{
  "role": "maintainer",
  "permissions": ["read_packages", "publish_packages", "delete_packages"],
  "active": true
}
```

### Delete User

```http
DELETE /users/{user_id}
Authorization: Bearer <admin_token>
```

## Security Management

### List Security Events

```http
GET /security/events
Authorization: Bearer <admin_token>
```

Query Parameters:
- `hours` (integer): Hours to look back (default: 24)
- `level` (string): Security level (low, medium, high, critical)
- `type` (string): Event type

Response:
```json
{
  "events": [
    {
      "id": "event_123",
      "type": "authentication_failure",
      "level": "medium",
      "timestamp": "2025-01-15T10:30:00Z",
      "user_id": "user_123",
      "ip_address": "192.168.1.100",
      "details": {
        "reason": "Invalid password",
        "username": "john_doe"
      }
    }
  ]
}
```

### Get Security Statistics

```http
GET /security/stats
Authorization: Bearer <admin_token>
```

Response:
```json
{
  "total_events": 150,
  "event_type_counts": {
    "authentication_failure": 50,
    "authorization_failure": 20,
    "suspicious_activity": 30,
    "rate_limit_exceeded": 25,
    "invalid_signature": 15,
    "malware_detected": 10
  },
  "security_level_counts": {
    "low": 20,
    "medium": 80,
    "high": 40,
    "critical": 10
  },
  "resolved_events": 120,
  "unresolved_events": 30
}
```

## Monitoring and Analytics

### Get Usage Statistics

```http
GET /monitoring/stats
Authorization: Bearer <admin_token>
```

Query Parameters:
- `hours` (integer): Hours to look back (default: 24)

Response:
```json
{
  "total_events": 5000,
  "unique_users": 150,
  "unique_packages": 300,
  "total_downloads": 2500,
  "total_uploads": 50,
  "api_calls": 2000,
  "security_events": 25,
  "error_events": 10,
  "cache_hit_rate": 0.85,
  "average_response_time": 150
}
```

### Get Top Packages

```http
GET /monitoring/top-packages
Authorization: Bearer <admin_token>
```

Query Parameters:
- `limit` (integer): Number of packages (default: 10)
- `days` (integer): Days to look back (default: 7)

Response:
```json
{
  "packages": [
    {
      "package_id": "package_123",
      "name": "popular-package",
      "downloads": 5000,
      "trend": "up"
    }
  ]
}
```

### Get Download Trends

```http
GET /monitoring/download-trends/{package_id}
Authorization: Bearer <token>
```

Query Parameters:
- `days` (integer): Days to look back (default: 7)

Response:
```json
{
  "package_id": "package_123",
  "total_downloads": 5000,
  "daily_downloads": {
    "2025-01-09": 100,
    "2025-01-10": 150,
    "2025-01-11": 200
  },
  "average_daily": 166.67
}
```

## Backup and Recovery

### List Backups

```http
GET /backup
Authorization: Bearer <admin_token>
```

Response:
```json
{
  "backups": [
    {
      "id": "backup_123",
      "type": "full",
      "status": "completed",
      "created_at": "2025-01-15T02:00:00Z",
      "completed_at": "2025-01-15T02:15:00Z",
      "size_bytes": 1073741824,
      "checksum": "a1b2c3d4...",
      "metadata": {
        "description": "Daily backup"
      }
    }
  ]
}
```

### Create Backup

```http
POST /backup
Authorization: Bearer <admin_token>
Content-Type: application/json

{
  "type": "full",
  "description": "Manual backup"
}
```

### Restore Backup

```http
POST /backup/{backup_id}/restore
Authorization: Bearer <admin_token>
Content-Type: application/json

{
  "target_path": "/var/registry/restore",
  "verify_only": false
}
```

## Error Responses

All endpoints may return the following error responses:

### 400 Bad Request

```json
{
  "error": "validation_error",
  "message": "Invalid package metadata",
  "details": {
    "field": "version",
    "issue": "Invalid semantic version format"
  }
}
```

### 401 Unauthorized

```json
{
  "error": "authentication_required",
  "message": "Valid authentication token required"
}
```

### 403 Forbidden

```json
{
  "error": "insufficient_permissions",
  "message": "User does not have required permissions",
  "required_permissions": ["publish_packages"]
}
```

### 404 Not Found

```json
{
  "error": "not_found",
  "message": "Package not found",
  "resource": "package",
  "id": "package_123"
}
```

### 429 Too Many Requests

```json
{
  "error": "rate_limit_exceeded",
  "message": "Rate limit exceeded",
  "retry_after": 60
}
```

### 500 Internal Server Error

```json
{
  "error": "internal_error",
  "message": "An internal error occurred",
  "request_id": "req_123"
}
```

## Rate Limiting

API requests are rate limited to prevent abuse:

- **Authenticated users**: 1000 requests per hour
- **Anonymous users**: 100 requests per hour
- **Package uploads**: 10 per hour per user
- **Package downloads**: 1000 per hour per user

Rate limit headers are included in responses:

```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1642233600
```

## Pagination

List endpoints support pagination with the following parameters:

- `page`: Page number (1-based)
- `limit`: Items per page (max 100)

Pagination information is included in responses:

```json
{
  "data": [...],
  "pagination": {
    "page": 1,
    "limit": 20,
    "total": 150,
    "pages": 8,
    "has_next": true,
    "has_prev": false
  }
}
```

## Webhooks

The registry supports webhooks for real-time notifications:

### Register Webhook

```http
POST /webhooks
Authorization: Bearer <token>
Content-Type: application/json

{
  "url": "https://your-app.com/webhook",
  "events": ["package.uploaded", "package.downloaded"],
  "secret": "webhook_secret"
}
```

### Webhook Events

- `package.uploaded`: Package uploaded successfully
- `package.downloaded`: Package downloaded
- `package.deleted`: Package deleted
- `user.created`: User created
- `security.event`: Security event detected

### Webhook Payload

```json
{
  "event": "package.uploaded",
  "timestamp": "2025-01-15T10:30:00Z",
  "data": {
    "package_id": "package_123",
    "name": "my-package",
    "version": "1.0.0",
    "author": "author@example.com"
  }
}
``` 