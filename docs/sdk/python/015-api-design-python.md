# 🌐 API Design - Python

**"We don't bow to any king" - API Design Edition**

TuskLang provides powerful tools for designing and implementing robust, scalable APIs with comprehensive documentation and testing capabilities.

## 🚀 RESTful API Design

### Basic API Structure

```python
from tsk import TSK
from flask import Flask, request, jsonify
import json

app = Flask(__name__)

# API configuration
api_config = TSK.from_string("""
[api]
# API metadata
name: "TuskLang API"
version: "1.0.0"
base_url: "/api/v1"
description: "Comprehensive API for TuskLang applications"

# API endpoints
endpoints: {
    users: "/users",
    posts: "/posts",
    comments: "/comments",
    auth: "/auth"
}

# Response formats
response_formats: {
    success: {
        status: "success",
        data: null,
        message: null
    },
    error: {
        status: "error",
        error: {
            code: null,
            message: null,
            details: null
        }
    }
}

# API validation
validate_request_fujsen = '''
def validate_request(data, schema):
    errors = []
    
    for field, rules in schema.items():
        if field not in data:
            if rules.get('required', False):
                errors.append(f"Missing required field: {field}")
            continue
        
        value = data[field]
        
        # Type validation
        if 'type' in rules:
            if not isinstance(value, rules['type']):
                errors.append(f"Field {field} must be {rules['type'].__name__}")
        
        # Length validation
        if 'min_length' in rules and len(str(value)) < rules['min_length']:
            errors.append(f"Field {field} must be at least {rules['min_length']} characters")
        
        if 'max_length' in rules and len(str(value)) > rules['max_length']:
            errors.append(f"Field {field} must be at most {rules['max_length']} characters")
    
    if errors:
        raise ValueError("Validation errors: " + "; ".join(errors))
    
    return True
'''

# Standardize API response
format_response_fujsen = '''
def format_response(data=None, message=None, status_code=200):
    response = {
        'status': 'success' if status_code < 400 else 'error',
        'data': data,
        'message': message,
        'timestamp': time.time()
    }
    
    if status_code >= 400:
        response['error'] = {
            'code': status_code,
            'message': message,
            'details': data
        }
        del response['data']
    
    return response, status_code
'''

# Handle API errors
handle_api_error_fujsen = '''
def handle_api_error(error, status_code=500):
    error_response = {
        'status': 'error',
        'error': {
            'code': status_code,
            'message': str(error),
            'type': type(error).__name__
        },
        'timestamp': time.time()
    }
    
    # Log error
    log_error("API Error", error, {
        'status_code': status_code,
        'endpoint': request.endpoint,
        'method': request.method
    })
    
    return error_response, status_code
'''
""")

# User API endpoints
@app.route('/api/v1/users', methods=['GET'])
def get_users():
    try:
        # Get query parameters
        page = int(request.args.get('page', 1))
        limit = int(request.args.get('limit', 10))
        search = request.args.get('search', '')
        
        # Validate parameters
        api_config.execute_fujsen('api', 'validate_request', {
            'page': page,
            'limit': limit
        }, {
            'page': {'type': int, 'min': 1},
            'limit': {'type': int, 'min': 1, 'max': 100}
        })
        
        # Get users from database
        users = get_users_from_db(page, limit, search)
        
        # Format response
        response_data = {
            'users': users,
            'pagination': {
                'page': page,
                'limit': limit,
                'total': get_total_users(search)
            }
        }
        
        return api_config.execute_fujsen('api', 'format_response', response_data, 'Users retrieved successfully')
    
    except Exception as e:
        return api_config.execute_fujsen('api', 'handle_api_error', e, 400)

@app.route('/api/v1/users', methods=['POST'])
def create_user():
    try:
        data = request.get_json()
        
        # Validate user data
        user_schema = {
            'username': {'type': str, 'required': True, 'min_length': 3, 'max_length': 50},
            'email': {'type': str, 'required': True, 'min_length': 5, 'max_length': 100},
            'password': {'type': str, 'required': True, 'min_length': 8}
        }
        
        api_config.execute_fujsen('api', 'validate_request', data, user_schema)
        
        # Create user
        user = create_user_in_db(data)
        
        return api_config.execute_fujsen('api', 'format_response', user, 'User created successfully', 201)
    
    except Exception as e:
        return api_config.execute_fujsen('api', 'handle_api_error', e, 400)

@app.route('/api/v1/users/<int:user_id>', methods=['GET'])
def get_user(user_id):
    try:
        user = get_user_from_db(user_id)
        
        if not user:
            return api_config.execute_fujsen('api', 'handle_api_error', 
                Exception("User not found"), 404)
        
        return api_config.execute_fujsen('api', 'format_response', user, 'User retrieved successfully')
    
    except Exception as e:
        return api_config.execute_fujsen('api', 'handle_api_error', e, 400)
```

## 🔐 Authentication & Authorization

### JWT Authentication

```python
# Authentication configuration
auth_config = TSK.from_string("""
[authentication]
# JWT configuration
jwt_secret: @env("JWT_SECRET")
jwt_expiry: @env("JWT_EXPIRY", 3600)
jwt_algorithm: @env("JWT_ALGORITHM", "HS256")

# Authentication functions
generate_token_fujsen = '''
def generate_token(user_id, username, roles=None):
    import jwt
    import time
    
    payload = {
        'user_id': user_id,
        'username': username,
        'roles': roles or [],
        'exp': time.time() + jwt_expiry,
        'iat': time.time()
    }
    
    token = jwt.encode(payload, jwt_secret, algorithm=jwt_algorithm)
    
    return {
        'token': token,
        'expires_in': jwt_expiry,
        'token_type': 'Bearer'
    }
'''

verify_token_fujsen = '''
def verify_token(token):
    import jwt
    
    try:
        payload = jwt.decode(token, jwt_secret, algorithms=[jwt_algorithm])
        
        # Check if user still exists and is active
        user = query("SELECT id, username, active FROM users WHERE id = ?", payload['user_id'])
        
        if not user or not user[0][2]:
            raise jwt.InvalidTokenError("User not found or inactive")
        
        return {
            'user_id': payload['user_id'],
            'username': payload['username'],
            'roles': payload.get('roles', [])
        }
    
    except jwt.ExpiredSignatureError:
        raise jwt.ExpiredSignatureError("Token has expired")
    except jwt.InvalidTokenError as e:
        raise jwt.InvalidTokenError(f"Invalid token: {str(e)}")
'''

# Authorization functions
check_permission_fujsen = '''
def check_permission(user_roles, required_roles, resource=None, action=None):
    # Check if user has any of the required roles
    if not any(role in user_roles for role in required_roles):
        raise AuthorizationError(
            f"Insufficient permissions for {action} on {resource}",
            required_roles=required_roles,
            user_roles=user_roles
        )
    
    return True
'''

require_auth_fujsen = '''
def require_auth(f):
    def decorated_function(*args, **kwargs):
        auth_header = request.headers.get('Authorization')
        
        if not auth_header or not auth_header.startswith('Bearer '):
            raise AuthenticationError("Missing or invalid authorization header")
        
        token = auth_header.split(' ')[1]
        user_data = verify_token(token)
        
        # Add user data to request context
        request.user = user_data
        
        return f(*args, **kwargs)
    
    return decorated_function
'''

require_role_fujsen = '''
def require_role(required_roles):
    def decorator(f):
        def decorated_function(*args, **kwargs):
            if not hasattr(request, 'user'):
                raise AuthenticationError("Authentication required")
            
            check_permission(request.user['roles'], required_roles)
            return f(*args, **kwargs)
        
        return decorated_function
    return decorator
'''
""")

# Protected endpoints
@app.route('/api/v1/users/<int:user_id>', methods=['PUT'])
@auth_config.execute_fujsen('authentication', 'require_auth')
@auth_config.execute_fujsen('authentication', 'require_role', ['admin', 'user'])
def update_user(user_id):
    try:
        # Check if user can update this profile
        if request.user['user_id'] != user_id and 'admin' not in request.user['roles']:
            return api_config.execute_fujsen('api', 'handle_api_error', 
                Exception("Insufficient permissions"), 403)
        
        data = request.get_json()
        
        # Validate update data
        update_schema = {
            'username': {'type': str, 'min_length': 3, 'max_length': 50},
            'email': {'type': str, 'min_length': 5, 'max_length': 100}
        }
        
        api_config.execute_fujsen('api', 'validate_request', data, update_schema)
        
        # Update user
        user = update_user_in_db(user_id, data)
        
        return api_config.execute_fujsen('api', 'format_response', user, 'User updated successfully')
    
    except Exception as e:
        return api_config.execute_fujsen('api', 'handle_api_error', e, 400)

@app.route('/api/v1/auth/login', methods=['POST'])
def login():
    try:
        data = request.get_json()
        
        # Validate login data
        login_schema = {
            'username': {'type': str, 'required': True},
            'password': {'type': str, 'required': True}
        }
        
        api_config.execute_fujsen('api', 'validate_request', data, login_schema)
        
        # Authenticate user
        user = authenticate_user(data['username'], data['password'])
        
        if not user:
            return api_config.execute_fujsen('api', 'handle_api_error', 
                Exception("Invalid credentials"), 401)
        
        # Generate token
        token_data = auth_config.execute_fujsen('authentication', 'generate_token', 
            user['id'], user['username'], user['roles'])
        
        return api_config.execute_fujsen('api', 'format_response', token_data, 'Login successful')
    
    except Exception as e:
        return api_config.execute_fujsen('api', 'handle_api_error', e, 400)
```

## 📊 API Documentation

### OpenAPI/Swagger Integration

```python
# API documentation configuration
docs_config = TSK.from_string("""
[api_documentation]
# OpenAPI specification
openapi_spec_fujsen = '''
def generate_openapi_spec():
    spec = {
        'openapi': '3.0.0',
        'info': {
            'title': 'TuskLang API',
            'version': '1.0.0',
            'description': 'Comprehensive API for TuskLang applications'
        },
        'servers': [
            {
                'url': 'https://api.example.com/api/v1',
                'description': 'Production server'
            },
            {
                'url': 'http://localhost:8000/api/v1',
                'description': 'Development server'
            }
        ],
        'paths': {
            '/users': {
                'get': {
                    'summary': 'Get users',
                    'parameters': [
                        {
                            'name': 'page',
                            'in': 'query',
                            'schema': {'type': 'integer', 'default': 1}
                        },
                        {
                            'name': 'limit',
                            'in': 'query',
                            'schema': {'type': 'integer', 'default': 10}
                        }
                    ],
                    'responses': {
                        '200': {
                            'description': 'Successful response',
                            'content': {
                                'application/json': {
                                    'schema': {
                                        'type': 'object',
                                        'properties': {
                                            'status': {'type': 'string'},
                                            'data': {'type': 'object'},
                                            'message': {'type': 'string'}
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                'post': {
                    'summary': 'Create user',
                    'requestBody': {
                        'required': True,
                        'content': {
                            'application/json': {
                                'schema': {
                                    'type': 'object',
                                    'properties': {
                                        'username': {'type': 'string'},
                                        'email': {'type': 'string'},
                                        'password': {'type': 'string'}
                                    },
                                    'required': ['username', 'email', 'password']
                                }
                            }
                        }
                    },
                    'responses': {
                        '201': {
                            'description': 'User created successfully'
                        }
                    }
                }
            }
        },
        'components': {
            'securitySchemes': {
                'bearerAuth': {
                    'type': 'http',
                    'scheme': 'bearer',
                    'bearerFormat': 'JWT'
                }
            }
        }
    }
    
    return spec
'''

# Generate API documentation
generate_docs_fujsen = '''
def generate_docs():
    # Generate OpenAPI spec
    openapi_spec = generate_openapi_spec()
    
    # Save to file
    with open('/app/docs/openapi.json', 'w') as f:
        json.dump(openapi_spec, f, indent=2)
    
    # Generate HTML documentation
    generate_html_docs(openapi_spec)
    
    return {
        'openapi_spec': openapi_spec,
        'docs_url': '/docs',
        'redoc_url': '/redoc'
    }
'''

# API versioning
version_api_fujsen = '''
def version_api(version):
    # API versioning logic
    if version == 'v1':
        return {
            'base_url': '/api/v1',
            'features': ['basic_crud', 'authentication', 'pagination']
        }
    elif version == 'v2':
        return {
            'base_url': '/api/v2',
            'features': ['basic_crud', 'authentication', 'pagination', 'graphql', 'websockets']
        }
    else:
        raise ValueError(f"Unsupported API version: {version}")
'''
""")

# Documentation endpoints
@app.route('/api/docs')
def api_docs():
    """Serve API documentation"""
    return docs_config.execute_fujsen('api_documentation', 'generate_docs')

@app.route('/api/openapi.json')
def openapi_spec():
    """Serve OpenAPI specification"""
    spec = docs_config.execute_fujsen('api_documentation', 'generate_openapi_spec')
    return jsonify(spec)
```

## 🔄 API Versioning & Migration

### Version Management

```python
# API versioning configuration
versioning_config = TSK.from_string("""
[api_versioning]
# Version management
manage_versions_fujsen = '''
def manage_versions():
    versions = {
        'v1': {
            'status': 'stable',
            'deprecated': False,
            'sunset_date': None,
            'features': ['basic_crud', 'authentication', 'pagination']
        },
        'v2': {
            'status': 'beta',
            'deprecated': False,
            'sunset_date': None,
            'features': ['basic_crud', 'authentication', 'pagination', 'graphql', 'websockets']
        }
    }
    
    return versions
'''

# Version migration
migrate_data_fujsen = '''
def migrate_data(from_version, to_version, data):
    if from_version == 'v1' and to_version == 'v2':
        # Migrate from v1 to v2
        migrated_data = {}
        
        # Map old fields to new fields
        field_mapping = {
            'user_id': 'id',
            'user_name': 'username',
            'user_email': 'email'
        }
        
        for old_field, new_field in field_mapping.items():
            if old_field in data:
                migrated_data[new_field] = data[old_field]
        
        # Add new fields with defaults
        migrated_data['created_at'] = data.get('created_at', time.time())
        migrated_data['updated_at'] = time.time()
        
        return migrated_data
    
    else:
        raise ValueError(f"Migration from {from_version} to {to_version} not supported")
'''

# Backward compatibility
ensure_backward_compatibility_fujsen = '''
def ensure_backward_compatibility(version, data):
    if version == 'v1':
        # Ensure v1 compatibility
        compatible_data = {}
        
        # Map new fields to old fields
        field_mapping = {
            'id': 'user_id',
            'username': 'user_name',
            'email': 'user_email'
        }
        
        for new_field, old_field in field_mapping.items():
            if new_field in data:
                compatible_data[old_field] = data[new_field]
        
        return compatible_data
    
    return data
'''
""")
```

## 📈 API Analytics & Monitoring

### Performance Monitoring

```python
# API analytics configuration
analytics_config = TSK.from_string("""
[api_analytics]
# Request tracking
track_request_fujsen = '''
def track_request(request_id, method, path, status_code, duration, user_id=None):
    # Track API request
    request_data = {
        'request_id': request_id,
        'method': method,
        'path': path,
        'status_code': status_code,
        'duration': duration,
        'user_id': user_id,
        'timestamp': time.time(),
        'ip_address': request.remote_addr,
        'user_agent': request.headers.get('User-Agent')
    }
    
    # Store in database
    execute("""
        INSERT INTO api_requests (
            request_id, method, path, status_code, duration, 
            user_id, timestamp, ip_address, user_agent
        ) VALUES (?, ?, ?, ?, ?, ?, datetime('now'), ?, ?)
    """, request_id, method, path, status_code, duration, 
        user_id, request.remote_addr, request.headers.get('User-Agent'))
    
    # Update metrics
    increment_metric('api_requests_total', 1, {'method': method, 'status_code': status_code})
    record_metric('api_response_time', duration, {'path': path})
    
    return request_data
'''

# API analytics
get_api_analytics_fujsen = '''
def get_api_analytics(time_range='24h'):
    if time_range == '24h':
        time_filter = "timestamp > datetime('now', '-1 day')"
    elif time_range == '7d':
        time_filter = "timestamp > datetime('now', '-7 days')"
    else:
        time_filter = "1=1"
    
    # Get request statistics
    stats = query(f"""
        SELECT 
            COUNT(*) as total_requests,
            AVG(duration) as avg_response_time,
            COUNT(CASE WHEN status_code >= 400 THEN 1 END) as error_count,
            COUNT(DISTINCT user_id) as unique_users
        FROM api_requests 
        WHERE {time_filter}
    """)[0]
    
    # Get top endpoints
    top_endpoints = query(f"""
        SELECT path, COUNT(*) as request_count, AVG(duration) as avg_duration
        FROM api_requests 
        WHERE {time_filter}
        GROUP BY path
        ORDER BY request_count DESC
        LIMIT 10
    """)
    
    # Get error breakdown
    error_breakdown = query(f"""
        SELECT status_code, COUNT(*) as count
        FROM api_requests 
        WHERE status_code >= 400 AND {time_filter}
        GROUP BY status_code
        ORDER BY count DESC
    """)
    
    return {
        'total_requests': stats[0],
        'avg_response_time': stats[1],
        'error_count': stats[2],
        'unique_users': stats[3],
        'error_rate': (stats[2] / stats[0] * 100) if stats[0] > 0 else 0,
        'top_endpoints': [{
            'path': row[0],
            'request_count': row[1],
            'avg_duration': row[2]
        } for row in top_endpoints],
        'error_breakdown': [{
            'status_code': row[0],
            'count': row[1]
        } for row in error_breakdown]
    }
'''

# Rate limiting analytics
analyze_rate_limits_fujsen = '''
def analyze_rate_limits(time_range='1h'):
    if time_range == '1h':
        time_filter = "timestamp > datetime('now', '-1 hour')"
    else:
        time_filter = "1=1"
    
    # Get rate limit violations
    violations = query(f"""
        SELECT ip_address, COUNT(*) as violation_count
        FROM rate_limit_violations
        WHERE {time_filter}
        GROUP BY ip_address
        ORDER BY violation_count DESC
        LIMIT 10
    """)
    
    return [{
        'ip_address': row[0],
        'violation_count': row[1]
    } for row in violations]
'''
""")
```

## 🎯 API Design Best Practices

### 1. RESTful Design
- Use appropriate HTTP methods (GET, POST, PUT, DELETE)
- Implement proper status codes
- Use consistent URL patterns
- Include proper headers

### 2. Authentication & Authorization
- Implement secure authentication (JWT, OAuth)
- Use role-based access control
- Validate permissions for each endpoint
- Implement proper token management

### 3. Input Validation
- Validate all input data
- Use consistent validation schemas
- Provide meaningful error messages
- Implement rate limiting

### 4. Documentation
- Generate comprehensive API documentation
- Include examples and use cases
- Maintain up-to-date specifications
- Provide interactive documentation

### 5. Versioning & Migration
- Implement proper API versioning
- Maintain backward compatibility
- Plan migration strategies
- Communicate deprecation timelines

## 🚀 Next Steps

1. **Design your API structure** with proper endpoints
2. **Implement authentication** and authorization
3. **Add comprehensive validation** for all inputs
4. **Generate API documentation** with OpenAPI
5. **Set up monitoring** and analytics

---

**"We don't bow to any king"** - TuskLang provides powerful tools for designing and implementing robust, scalable APIs. Follow best practices, implement proper security, and create comprehensive documentation to build world-class APIs! 