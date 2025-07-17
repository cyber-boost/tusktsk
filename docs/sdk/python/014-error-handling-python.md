# 🚨 Error Handling - Python

**"We don't bow to any king" - Error Handling Edition**

TuskLang provides robust error handling capabilities to ensure your applications gracefully handle failures and provide meaningful feedback.

## 🛡️ Basic Error Handling

### Exception Handling in FUJSEN

```python
from tsk import TSK
import traceback

# Error handling configuration
error_config = TSK.from_string("""
[error_handling]
# Error handling functions
safe_operation_fujsen = '''
def safe_operation(operation_func, *args, **kwargs):
    try:
        result = operation_func(*args, **kwargs)
        return {
            'success': True,
            'result': result,
            'error': None
        }
    except Exception as e:
        return {
            'success': False,
            'result': None,
            'error': {
                'type': type(e).__name__,
                'message': str(e),
                'traceback': traceback.format_exc()
            }
        }
'''

validate_input_fujsen = '''
def validate_input(data, required_fields=None, field_types=None):
    errors = []
    
    # Check required fields
    if required_fields:
        for field in required_fields:
            if field not in data:
                errors.append(f"Missing required field: {field}")
            elif data[field] is None or data[field] == "":
                errors.append(f"Field {field} cannot be empty")
    
    # Check field types
    if field_types:
        for field, expected_type in field_types.items():
            if field in data and not isinstance(data[field], expected_type):
                errors.append(f"Field {field} must be {expected_type.__name__}, got {type(data[field]).__name__}")
    
    if errors:
        raise ValueError("Validation errors: " + "; ".join(errors))
    
    return True
'''

handle_database_error_fujsen = '''
def handle_database_error(operation_func, *args, **kwargs):
    try:
        return operation_func(*args, **kwargs)
    except Exception as e:
        error_type = type(e).__name__
        
        if 'connection' in str(e).lower():
            # Connection error
            log_error("Database connection error", e, {'operation': operation_func.__name__})
            raise ConnectionError("Database connection failed. Please try again later.")
        
        elif 'timeout' in str(e).lower():
            # Timeout error
            log_error("Database timeout error", e, {'operation': operation_func.__name__})
            raise TimeoutError("Database operation timed out. Please try again.")
        
        elif 'constraint' in str(e).lower():
            # Constraint violation
            log_error("Database constraint violation", e, {'operation': operation_func.__name__})
            raise ValueError("Data validation failed. Please check your input.")
        
        else:
            # Generic database error
            log_error("Database error", e, {'operation': operation_func.__name__})
            raise RuntimeError("A database error occurred. Please try again later.")
'''
""")

# Test error handling
def test_error_handling():
    # Test safe operation
    def risky_function(x, y):
        return x / y
    
    result = error_config.execute_fujsen('error_handling', 'safe_operation', risky_function, 10, 2)
    print(f"Safe operation (success): {result['success']}")
    
    result = error_config.execute_fujsen('error_handling', 'safe_operation', risky_function, 10, 0)
    print(f"Safe operation (error): {result['success']}")
    print(f"Error: {result['error']['message']}")
    
    # Test input validation
    try:
        error_config.execute_fujsen('error_handling', 'validate_input', 
            {'name': 'John', 'age': 30}, 
            ['name', 'age'], 
            {'age': int}
        )
        print("Validation passed")
    except ValueError as e:
        print(f"Validation failed: {e}")
```

## 🔍 Custom Exception Classes

### Domain-Specific Exceptions

```python
# Custom exceptions configuration
custom_exceptions = TSK.from_string("""
[custom_exceptions]
# Custom exception classes
TuskLangError_fujsen = '''
class TuskLangError(Exception):
    """Base exception for TuskLang applications"""
    def __init__(self, message, error_code=None, context=None):
        super().__init__(message)
        self.message = message
        self.error_code = error_code
        self.context = context or {}
    
    def to_dict(self):
        return {
            'error': True,
            'message': self.message,
            'error_code': self.error_code,
            'context': self.context,
            'type': self.__class__.__name__
        }
'''

ValidationError_fujsen = '''
class ValidationError(TuskLangError):
    """Raised when data validation fails"""
    def __init__(self, message, field=None, value=None):
        super().__init__(message, 'VALIDATION_ERROR', {'field': field, 'value': value})
        self.field = field
        self.value = value
'''

AuthenticationError_fujsen = '''
class AuthenticationError(TuskLangError):
    """Raised when authentication fails"""
    def __init__(self, message, user_id=None):
        super().__init__(message, 'AUTHENTICATION_ERROR', {'user_id': user_id})
        self.user_id = user_id
'''

AuthorizationError_fujsen = '''
class AuthorizationError(TuskLangError):
    """Raised when authorization fails"""
    def __init__(self, message, user_id=None, resource=None, action=None):
        super().__init__(message, 'AUTHORIZATION_ERROR', {
            'user_id': user_id,
            'resource': resource,
            'action': action
        })
        self.user_id = user_id
        self.resource = resource
        self.action = action
'''

DatabaseError_fujsen = '''
class DatabaseError(TuskLangError):
    """Raised when database operations fail"""
    def __init__(self, message, operation=None, table=None):
        super().__init__(message, 'DATABASE_ERROR', {
            'operation': operation,
            'table': table
        })
        self.operation = operation
        self.table = table
'''

ExternalServiceError_fujsen = '''
class ExternalServiceError(TuskLangError):
    """Raised when external service calls fail"""
    def __init__(self, message, service=None, endpoint=None, status_code=None):
        super().__init__(message, 'EXTERNAL_SERVICE_ERROR', {
            'service': service,
            'endpoint': endpoint,
            'status_code': status_code
        })
        self.service = service
        self.endpoint = endpoint
        self.status_code = status_code
'''
""")
```

## 🔄 Error Recovery Strategies

### Retry Mechanisms

```python
# Retry configuration
retry_config = TSK.from_string("""
[retry_mechanisms]
# Retry with exponential backoff
retry_with_backoff_fujsen = '''
def retry_with_backoff(operation_func, max_retries=3, base_delay=1, max_delay=60):
    import time
    import random
    
    for attempt in range(max_retries + 1):
        try:
            return operation_func()
        except Exception as e:
            if attempt == max_retries:
                # Last attempt failed, re-raise
                raise e
            
            # Calculate delay with exponential backoff and jitter
            delay = min(base_delay * (2 ** attempt) + random.uniform(0, 1), max_delay)
            
            # Log retry attempt
            log_warning(f"Retry attempt {attempt + 1}/{max_retries + 1} failed: {str(e)}")
            
            # Wait before retry
            time.sleep(delay)
'''

# Retry specific operations
retry_database_operation_fujsen = '''
def retry_database_operation(operation_func, *args, **kwargs):
    def wrapped_operation():
        return operation_func(*args, **kwargs)
    
    return retry_with_backoff(wrapped_operation, max_retries=3, base_delay=0.5)
'''

retry_external_api_fujsen = '''
def retry_external_api(api_func, *args, **kwargs):
    def wrapped_api_call():
        return api_func(*args, **kwargs)
    
    return retry_with_backoff(wrapped_api_call, max_retries=5, base_delay=1)
'''

# Circuit breaker pattern
circuit_breaker_fujsen = '''
class CircuitBreaker:
    def __init__(self, failure_threshold=5, recovery_timeout=60):
        self.failure_threshold = failure_threshold
        self.recovery_timeout = recovery_timeout
        self.failure_count = 0
        self.last_failure_time = None
        self.state = 'CLOSED'  # CLOSED, OPEN, HALF_OPEN
    
    def call(self, operation_func, *args, **kwargs):
        if self.state == 'OPEN':
            if time.time() - self.last_failure_time > self.recovery_timeout:
                self.state = 'HALF_OPEN'
            else:
                raise Exception("Circuit breaker is OPEN")
        
        try:
            result = operation_func(*args, **kwargs)
            self.on_success()
            return result
        except Exception as e:
            self.on_failure()
            raise e
    
    def on_success(self):
        self.failure_count = 0
        self.state = 'CLOSED'
    
    def on_failure(self):
        self.failure_count += 1
        self.last_failure_time = time.time()
        
        if self.failure_count >= self.failure_threshold:
            self.state = 'OPEN'
'''

# Create circuit breaker instances
database_circuit_breaker_fujsen = '''
def get_database_circuit_breaker():
    # Get or create database circuit breaker
    if not hasattr(get_database_circuit_breaker, '_instance'):
        get_database_circuit_breaker._instance = CircuitBreaker(failure_threshold=3, recovery_timeout=30)
    return get_database_circuit_breaker._instance
'''

api_circuit_breaker_fujsen = '''
def get_api_circuit_breaker():
    # Get or create API circuit breaker
    if not hasattr(get_api_circuit_breaker, '_instance'):
        get_api_circuit_breaker._instance = CircuitBreaker(failure_threshold=5, recovery_timeout=60)
    return get_api_circuit_breaker._instance
'''
""")
```

## 📝 Error Logging & Reporting

### Comprehensive Error Logging

```python
# Error logging configuration
error_logging = TSK.from_string("""
[error_logging]
# Error logging functions
log_error_with_context_fujsen = '''
def log_error_with_context(error, context=None, severity='error'):
    import json
    import traceback
    
    error_data = {
        'timestamp': time.time(),
        'severity': severity,
        'error_type': type(error).__name__,
        'error_message': str(error),
        'traceback': traceback.format_exc(),
        'context': context or {},
        'user_id': context.get('user_id') if context else None,
        'request_id': context.get('request_id') if context else None,
        'session_id': context.get('session_id') if context else None
    }
    
    # Log to database
    execute("""
        INSERT INTO error_logs (
            timestamp, severity, error_type, error_message, 
            traceback, context, user_id, request_id, session_id
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
    """, 
        time.strftime('%Y-%m-%d %H:%M:%S'), severity, error_data['error_type'],
        error_data['error_message'], error_data['traceback'], 
        json.dumps(error_data['context']), error_data['user_id'],
        error_data['request_id'], error_data['session_id']
    )
    
    # Log to file if critical
    if severity == 'critical':
        write_critical_error_to_file(error_data)
    
    # Send alert if needed
    if severity in ['critical', 'high']:
        send_error_alert(error_data)
    
    return error_data
'''

write_critical_error_to_file_fujsen = '''
def write_critical_error_to_file(error_data):
    import json
    
    log_entry = {
        'timestamp': time.strftime('%Y-%m-%d %H:%M:%S'),
        'severity': 'CRITICAL',
        'error': error_data
    }
    
    with open('/app/logs/critical_errors.log', 'a') as f:
        f.write(json.dumps(log_entry) + '\\n')
'''

send_error_alert_fujsen = '''
def send_error_alert(error_data):
    # Send error alert via email/Slack/etc.
    alert_message = f"""
    CRITICAL ERROR ALERT
    
    Time: {time.strftime('%Y-%m-%d %H:%M:%S')}
    Error Type: {error_data['error_type']}
    Error Message: {error_data['error_message']}
    User ID: {error_data['user_id']}
    Request ID: {error_data['request_id']}
    
    Context: {json.dumps(error_data['context'], indent=2)}
    """
    
    # Send via email
    send_email_alert(alert_message)
    
    # Send via Slack
    send_slack_alert(alert_message)
'''

# Error aggregation
aggregate_errors_fujsen = '''
def aggregate_errors(time_range='1h'):
    # Aggregate errors by type and severity
    if time_range == '1h':
        time_filter = "timestamp > datetime('now', '-1 hour')"
    elif time_range == '24h':
        time_filter = "timestamp > datetime('now', '-1 day')"
    else:
        time_filter = "1=1"
    
    # Get error counts by type
    error_counts = query(f"""
        SELECT error_type, severity, COUNT(*) as count
        FROM error_logs
        WHERE {time_filter}
        GROUP BY error_type, severity
        ORDER BY count DESC
    """)
    
    # Get recent errors
    recent_errors = query(f"""
        SELECT error_type, error_message, timestamp, user_id
        FROM error_logs
        WHERE {time_filter}
        ORDER BY timestamp DESC
        LIMIT 10
    """)
    
    return {
        'error_counts': [{
            'error_type': row[0],
            'severity': row[1],
            'count': row[2]
        } for row in error_counts],
        'recent_errors': [{
            'error_type': row[0],
            'error_message': row[1],
            'timestamp': row[2],
            'user_id': row[3]
        } for row in recent_errors]
    }
'''
""")
```

## 🛠️ Error Recovery & Fallbacks

### Graceful Degradation

```python
# Error recovery configuration
error_recovery = TSK.from_string("""
[error_recovery]
# Fallback mechanisms
fallback_data_source_fujsen = '''
def fallback_data_source(primary_func, fallback_func, *args, **kwargs):
    try:
        # Try primary data source
        return primary_func(*args, **kwargs)
    except Exception as e:
        log_warning(f"Primary data source failed: {str(e)}, using fallback")
        
        try:
            # Try fallback data source
            return fallback_func(*args, **kwargs)
        except Exception as fallback_error:
            log_error("Both primary and fallback data sources failed", fallback_error)
            raise fallback_error
'''

# Cache fallback
cache_fallback_fujsen = '''
def cache_fallback(operation_func, cache_key, ttl=300, *args, **kwargs):
    # Try to get from cache first
    cached_result = cache.get(cache_key)
    if cached_result:
        return cached_result
    
    try:
        # Execute operation
        result = operation_func(*args, **kwargs)
        
        # Cache the result
        cache.set(cache_key, result, ttl)
        
        return result
    except Exception as e:
        # If operation fails, try to return cached data even if expired
        expired_result = cache.get(cache_key, include_expired=True)
        if expired_result:
            log_warning(f"Operation failed, returning expired cache for {cache_key}")
            return expired_result
        
        # No cache available, re-raise error
        raise e
'''

# Default value fallback
default_value_fallback_fujsen = '''
def default_value_fallback(operation_func, default_value, *args, **kwargs):
    try:
        return operation_func(*args, **kwargs)
    except Exception as e:
        log_warning(f"Operation failed, using default value: {str(e)}")
        return default_value
'''

# Service degradation
degrade_service_fujsen = '''
def degrade_service(operation_func, degraded_func, *args, **kwargs):
    try:
        return operation_func(*args, **kwargs)
    except Exception as e:
        log_warning(f"Full service failed, using degraded mode: {str(e)}")
        
        # Set service status to degraded
        set_service_status('degraded')
        
        # Use degraded functionality
        return degraded_func(*args, **kwargs)
'''

# Set service status
set_service_status_fujsen = '''
def set_service_status(status):
    # Update service status
    execute("""
        INSERT OR REPLACE INTO service_status (service_name, status, updated_at)
        VALUES (?, ?, datetime('now'))
    """, 'main_service', status)
    
    # Log status change
    log_info(f"Service status changed to: {status}")
    
    return status
'''
""")
```

## 🔧 Error Prevention

### Input Validation & Sanitization

```python
# Error prevention configuration
error_prevention = TSK.from_string("""
[error_prevention]
# Comprehensive input validation
validate_user_input_fujsen = '''
def validate_user_input(data, schema):
    errors = []
    
    for field, rules in schema.items():
        if field not in data:
            if rules.get('required', False):
                errors.append(f"Missing required field: {field}")
            continue
        
        value = data[field]
        
        # Type validation
        if 'type' in rules:
            expected_type = rules['type']
            if not isinstance(value, expected_type):
                errors.append(f"Field {field} must be {expected_type.__name__}")
                continue
        
        # Length validation
        if 'min_length' in rules and len(str(value)) < rules['min_length']:
            errors.append(f"Field {field} must be at least {rules['min_length']} characters")
        
        if 'max_length' in rules and len(str(value)) > rules['max_length']:
            errors.append(f"Field {field} must be at most {rules['max_length']} characters")
        
        # Range validation
        if 'min' in rules and value < rules['min']:
            errors.append(f"Field {field} must be at least {rules['min']}")
        
        if 'max' in rules and value > rules['max']:
            errors.append(f"Field {field} must be at most {rules['max']}")
        
        # Pattern validation
        if 'pattern' in rules:
            import re
            if not re.match(rules['pattern'], str(value)):
                errors.append(f"Field {field} does not match required pattern")
        
        # Custom validation
        if 'validator' in rules:
            try:
                if not rules['validator'](value):
                    errors.append(f"Field {field} failed custom validation")
            except Exception as e:
                errors.append(f"Field {field} validation error: {str(e)}")
    
    if errors:
        raise ValidationError("Input validation failed: " + "; ".join(errors))
    
    return True
'''

# SQL injection prevention
safe_sql_query_fujsen = '''
def safe_sql_query(query_template, *params):
    # Validate query template
    if not isinstance(query_template, str):
        raise ValueError("Query template must be a string")
    
    # Check for suspicious patterns
    suspicious_patterns = [
        'DROP TABLE', 'DELETE FROM', 'TRUNCATE', 'ALTER TABLE',
        'CREATE TABLE', 'INSERT INTO', 'UPDATE', 'EXEC', 'EXECUTE'
    ]
    
    query_upper = query_template.upper()
    for pattern in suspicious_patterns:
        if pattern in query_upper:
            raise ValueError(f"Suspicious SQL pattern detected: {pattern}")
    
    # Execute safe query
    try:
        return query(query_template, *params)
    except Exception as e:
        log_error("SQL query error", e, {'query': query_template, 'params': params})
        raise DatabaseError(f"Database query failed: {str(e)}")
'''

# XSS prevention
sanitize_html_fujsen = '''
def sanitize_html(html_content):
    import re
    
    # Remove potentially dangerous HTML tags
    dangerous_tags = ['script', 'iframe', 'object', 'embed', 'form']
    for tag in dangerous_tags:
        pattern = f'<{tag}[^>]*>.*?</{tag}>'
        html_content = re.sub(pattern, '', html_content, flags=re.IGNORECASE | re.DOTALL)
    
    # Remove dangerous attributes
    dangerous_attrs = ['onclick', 'onload', 'onerror', 'javascript:']
    for attr in dangerous_attrs:
        pattern = f'{attr}=["\'][^"\']*["\']'
        html_content = re.sub(pattern, '', html_content, flags=re.IGNORECASE)
    
    return html_content
'''

# Rate limiting
rate_limit_check_fujsen = '''
def rate_limit_check(identifier, limit, window):
    current_time = time.time()
    window_start = current_time - window
    
    # Count recent requests
    count = query("""
        SELECT COUNT(*) FROM rate_limits 
        WHERE identifier = ? AND timestamp > ?
    """, identifier, window_start)
    
    if count[0][0] >= limit:
        raise RateLimitError(f"Rate limit exceeded: {limit} requests per {window} seconds")
    
    # Record this request
    execute("""
        INSERT INTO rate_limits (identifier, timestamp)
        VALUES (?, ?)
    """, identifier, current_time)
    
    return True
'''
""")
```

## 🎯 Error Handling Best Practices

### 1. Exception Hierarchy
- Create custom exception classes for different error types
- Use appropriate exception types for different scenarios
- Include context information in exceptions
- Implement proper exception chaining

### 2. Error Recovery
- Implement retry mechanisms with exponential backoff
- Use circuit breakers for external service calls
- Provide fallback mechanisms for critical operations
- Implement graceful degradation

### 3. Error Logging
- Log errors with sufficient context
- Use appropriate log levels
- Include correlation IDs for request tracing
- Implement error aggregation and reporting

### 4. Error Prevention
- Validate all input data
- Use parameterized queries to prevent SQL injection
- Sanitize user-generated content
- Implement rate limiting and access controls

### 5. User Experience
- Provide meaningful error messages
- Don't expose sensitive information in error messages
- Implement proper error pages
- Use appropriate HTTP status codes

## 🚀 Next Steps

1. **Implement custom exception classes** for your domain
2. **Set up comprehensive error logging** with context
3. **Add retry mechanisms** for external service calls
4. **Implement fallback strategies** for critical operations
5. **Create error monitoring** and alerting systems

---

**"We don't bow to any king"** - TuskLang provides robust error handling capabilities to ensure your applications gracefully handle failures. Implement proper error handling, logging, and recovery strategies to build resilient applications! 