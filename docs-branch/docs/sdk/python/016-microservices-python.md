# 🏗️ Microservices - Python

**"We don't bow to any king" - Microservices Edition**

TuskLang provides powerful tools for building and orchestrating microservices architectures with seamless communication and service discovery.

## 🚀 Service Architecture

### Basic Microservice Structure

```python
from tsk import TSK
from flask import Flask, request, jsonify
import requests
import json

app = Flask(__name__)

# Microservice configuration
service_config = TSK.from_string("""
[microservice]
# Service metadata
name: "user-service"
version: "1.0.0"
port: 8001
environment: @env("SERVICE_ENV", "development")

# Service dependencies
dependencies: {
    auth_service: "http://auth-service:8002",
    notification_service: "http://notification-service:8003",
    database_service: "http://database-service:8004"
}

# Service discovery
service_registry: @env("SERVICE_REGISTRY", "http://registry:8000")

# Health check configuration
health_check: {
    endpoint: "/health",
    interval: 30,
    timeout: 5
}

# Service communication
service_client_fujsen = '''
def service_client(service_name, endpoint, method='GET', data=None, headers=None):
    # Get service URL from registry or dependencies
    service_url = get_service_url(service_name)
    
    if not service_url:
        raise ServiceUnavailableError(f"Service {service_name} not available")
    
    # Make service call
    url = f"{service_url}{endpoint}"
    
    try:
        if method.upper() == 'GET':
            response = requests.get(url, headers=headers, timeout=10)
        elif method.upper() == 'POST':
            response = requests.post(url, json=data, headers=headers, timeout=10)
        elif method.upper() == 'PUT':
            response = requests.put(url, json=data, headers=headers, timeout=10)
        elif method.upper() == 'DELETE':
            response = requests.delete(url, headers=headers, timeout=10)
        else:
            raise ValueError(f"Unsupported HTTP method: {method}")
        
        response.raise_for_status()
        return response.json()
    
    except requests.exceptions.RequestException as e:
        log_error(f"Service call failed: {service_name}", e)
        raise ServiceCommunicationError(f"Failed to communicate with {service_name}: {str(e)}")
'''

# Service discovery
get_service_url_fujsen = '''
def get_service_url(service_name):
    # First check dependencies
    if service_name in dependencies:
        return dependencies[service_name]
    
    # Then check service registry
    try:
        response = requests.get(f"{service_registry}/services/{service_name}")
        if response.status_code == 200:
            service_info = response.json()
            return service_info['url']
    except:
        pass
    
    return None
'''

# Circuit breaker for service calls
circuit_breaker_fujsen = '''
class ServiceCircuitBreaker:
    def __init__(self, service_name, failure_threshold=5, recovery_timeout=60):
        self.service_name = service_name
        self.failure_threshold = failure_threshold
        self.recovery_timeout = recovery_timeout
        self.failure_count = 0
        self.last_failure_time = None
        self.state = 'CLOSED'
    
    def call(self, service_func, *args, **kwargs):
        if self.state == 'OPEN':
            if time.time() - self.last_failure_time > self.recovery_timeout:
                self.state = 'HALF_OPEN'
            else:
                raise ServiceUnavailableError(f"Circuit breaker for {self.service_name} is OPEN")
        
        try:
            result = service_func(*args, **kwargs)
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

# Service health check
health_check_fujsen = '''
def health_check():
    health_status = {
        'service': name,
        'version': version,
        'status': 'healthy',
        'timestamp': time.time(),
        'dependencies': {}
    }
    
    # Check dependencies
    for dep_name, dep_url in dependencies.items():
        try:
            response = requests.get(f"{dep_url}/health", timeout=5)
            health_status['dependencies'][dep_name] = {
                'status': 'healthy' if response.status_code == 200 else 'unhealthy',
                'response_time': response.elapsed.total_seconds()
            }
        except Exception as e:
            health_status['dependencies'][dep_name] = {
                'status': 'unhealthy',
                'error': str(e)
            }
    
    # Determine overall status
    all_healthy = all(dep['status'] == 'healthy' for dep in health_status['dependencies'].values())
    health_status['status'] = 'healthy' if all_healthy else 'degraded'
    
    return health_status
'''
""")

# User service endpoints
@app.route('/users', methods=['GET'])
def get_users():
    try:
        # Get users from database service
        users = service_config.execute_fujsen('microservice', 'service_client', 
            'database_service', '/users', 'GET')
        
        return jsonify({
            'status': 'success',
            'data': users,
            'service': service_config.get('microservice.name')
        })
    
    except Exception as e:
        return jsonify({
            'status': 'error',
            'message': str(e),
            'service': service_config.get('microservice.name')
        }), 500

@app.route('/users', methods=['POST'])
def create_user():
    try:
        data = request.get_json()
        
        # Validate user data
        if not data.get('username') or not data.get('email'):
            return jsonify({'status': 'error', 'message': 'Missing required fields'}), 400
        
        # Create user in database service
        user = service_config.execute_fujsen('microservice', 'service_client',
            'database_service', '/users', 'POST', data)
        
        # Send notification
        try:
            service_config.execute_fujsen('microservice', 'service_client',
                'notification_service', '/notifications', 'POST', {
                    'type': 'user_created',
                    'user_id': user['id'],
                    'email': user['email']
                })
        except Exception as e:
            # Log notification failure but don't fail the request
            print(f"Notification failed: {e}")
        
        return jsonify({
            'status': 'success',
            'data': user,
            'service': service_config.get('microservice.name')
        }), 201
    
    except Exception as e:
        return jsonify({
            'status': 'error',
            'message': str(e),
            'service': service_config.get('microservice.name')
        }), 500

@app.route('/health', methods=['GET'])
def health():
    """Health check endpoint"""
    health_status = service_config.execute_fujsen('microservice', 'health_check')
    status_code = 200 if health_status['status'] == 'healthy' else 503
    return jsonify(health_status), status_code
```

## 🔄 Service Communication

### Inter-Service Communication

```python
# Service communication configuration
communication_config = TSK.from_string("""
[service_communication]
# Message queue configuration
message_queue: @env("MESSAGE_QUEUE", "redis://localhost:6379")
queue_name: "service_events"

# Event-driven communication
publish_event_fujsen = '''
def publish_event(event_type, event_data, target_service=None):
    import redis
    
    # Connect to Redis
    r = redis.from_url(message_queue)
    
    # Create event message
    event = {
        'id': generate_event_id(),
        'type': event_type,
        'data': event_data,
        'source_service': name,
        'target_service': target_service,
        'timestamp': time.time()
    }
    
    # Publish to queue
    r.lpush(queue_name, json.dumps(event))
    
    return event
'''

subscribe_events_fujsen = '''
def subscribe_events(event_types=None):
    import redis
    
    # Connect to Redis
    r = redis.from_url(message_queue)
    
    while True:
        # Pop event from queue
        event_data = r.brpop(queue_name, timeout=1)
        
        if event_data:
            event = json.loads(event_data[1])
            
            # Filter by event type if specified
            if event_types and event['type'] not in event_types:
                continue
            
            # Process event
            process_event(event)
'''

process_event_fujsen = '''
def process_event(event):
    # Route event to appropriate handler
    handlers = {
        'user_created': handle_user_created,
        'user_updated': handle_user_updated,
        'user_deleted': handle_user_deleted,
        'order_created': handle_order_created
    }
    
    handler = handlers.get(event['type'])
    if handler:
        try:
            handler(event['data'])
        except Exception as e:
            log_error(f"Event processing failed: {event['type']}", e)
    else:
        log_warning(f"No handler for event type: {event['type']}")
'''

# gRPC communication
grpc_client_fujsen = '''
def grpc_client(service_name, method, request_data):
    import grpc
    
    # Get service address
    service_address = get_service_url(service_name)
    
    # Create gRPC channel
    channel = grpc.insecure_channel(service_address)
    
    try:
        # Import service stub
        if service_name == 'user_service':
            from user_service_pb2_grpc import UserServiceStub
            from user_service_pb2 import UserRequest
            
            stub = UserServiceStub(channel)
            request = UserRequest(**request_data)
            
            if method == 'get_user':
                response = stub.GetUser(request)
            elif method == 'create_user':
                response = stub.CreateUser(request)
            else:
                raise ValueError(f"Unknown method: {method}")
        
        return response
        
    except grpc.RpcError as e:
        log_error(f"gRPC call failed: {service_name}.{method}", e)
        raise ServiceCommunicationError(f"gRPC call failed: {str(e)}")
    finally:
        channel.close()
'''

# Service mesh integration
service_mesh_fujsen = '''
def service_mesh_request(service_name, endpoint, method='GET', data=None):
    # Add service mesh headers
    headers = {
        'X-Request-ID': generate_request_id(),
        'X-Service-Name': name,
        'X-Service-Version': version
    }
    
    # Make request through service mesh
    service_url = get_service_url(service_name)
    
    try:
        if method.upper() == 'GET':
            response = requests.get(f"{service_url}{endpoint}", headers=headers)
        elif method.upper() == 'POST':
            response = requests.post(f"{service_url}{endpoint}", json=data, headers=headers)
        else:
            raise ValueError(f"Unsupported method: {method}")
        
        response.raise_for_status()
        return response.json()
        
    except requests.exceptions.RequestException as e:
        log_error(f"Service mesh request failed: {service_name}", e)
        raise ServiceCommunicationError(f"Service mesh request failed: {str(e)}")
'''
""")
```

## 🔍 Service Discovery & Registry

### Service Registry Implementation

```python
# Service registry configuration
registry_config = TSK.from_string("""
[service_registry]
# Registry storage
registry_db: @env("REGISTRY_DB", "sqlite:///registry.db")

# Service registration
register_service_fujsen = '''
def register_service(service_info):
    # Validate service info
    required_fields = ['name', 'version', 'url', 'health_endpoint']
    for field in required_fields:
        if field not in service_info:
            raise ValueError(f"Missing required field: {field}")
    
    # Check if service is healthy
    try:
        response = requests.get(f"{service_info['url']}{service_info['health_endpoint']}", timeout=5)
        if response.status_code != 200:
            raise ValueError("Service health check failed")
    except Exception as e:
        raise ValueError(f"Service health check failed: {str(e)}")
    
    # Register service
    service_id = f"{service_info['name']}-{service_info['version']}"
    
    execute("""
        INSERT OR REPLACE INTO services (
            service_id, name, version, url, health_endpoint, 
            metadata, registered_at, last_health_check
        ) VALUES (?, ?, ?, ?, ?, ?, datetime('now'), datetime('now'))
    """, service_id, service_info['name'], service_info['version'],
        service_info['url'], service_info['health_endpoint'],
        json.dumps(service_info.get('metadata', {})))
    
    return {'service_id': service_id, 'status': 'registered'}
'''

# Service discovery
discover_service_fujsen = '''
def discover_service(service_name, version=None):
    if version:
        service_id = f"{service_name}-{version}"
        service = query("""
            SELECT * FROM services 
            WHERE service_id = ? AND active = 1
            ORDER BY last_health_check DESC
        """, service_id)
    else:
        service = query("""
            SELECT * FROM services 
            WHERE name = ? AND active = 1
            ORDER BY last_health_check DESC, version DESC
        """, service_name)
    
    if not service:
        return None
    
    return {
        'service_id': service[0][0],
        'name': service[0][1],
        'version': service[0][2],
        'url': service[0][3],
        'health_endpoint': service[0][4],
        'metadata': json.loads(service[0][5]),
        'registered_at': service[0][6],
        'last_health_check': service[0][7]
    }
'''

# Health check monitoring
monitor_services_fujsen = '''
def monitor_services():
    # Get all registered services
    services = query("SELECT * FROM services WHERE active = 1")
    
    for service in services:
        service_url = service[3]
        health_endpoint = service[4]
        
        try:
            # Check service health
            response = requests.get(f"{service_url}{health_endpoint}", timeout=5)
            
            if response.status_code == 200:
                # Service is healthy
                execute("""
                    UPDATE services 
                    SET last_health_check = datetime('now'), health_status = 'healthy'
                    WHERE service_id = ?
                """, service[0])
            else:
                # Service is unhealthy
                execute("""
                    UPDATE services 
                    SET last_health_check = datetime('now'), health_status = 'unhealthy'
                    WHERE service_id = ?
                """, service[0])
                
        except Exception as e:
            # Service is unreachable
            execute("""
                UPDATE services 
                SET last_health_check = datetime('now'), health_status = 'unreachable'
                WHERE service_id = ?
            """, service[0])
            
            log_error(f"Service health check failed: {service[1]}", e)
'''

# Load balancing
load_balance_fujsen = '''
def load_balance(service_name, strategy='round_robin'):
    # Get all healthy instances of the service
    services = query("""
        SELECT * FROM services 
        WHERE name = ? AND health_status = 'healthy' AND active = 1
        ORDER BY last_health_check DESC
    """, service_name)
    
    if not services:
        return None
    
    if strategy == 'round_robin':
        # Simple round-robin
        return services[0][3]  # Return URL of first service
    
    elif strategy == 'least_connections':
        # Return service with least active connections
        # This would require tracking connection counts
        return services[0][3]
    
    elif strategy == 'random':
        import random
        return random.choice(services)[3]
    
    else:
        return services[0][3]
'''
""")
```

## 📊 Service Monitoring & Observability

### Distributed Tracing

```python
# Service monitoring configuration
monitoring_config = TSK.from_string("""
[service_monitoring]
# Distributed tracing
trace_request_fujsen = '''
def trace_request(request_id, service_name, operation, duration, status='success'):
    # Create trace span
    span = {
        'trace_id': request_id,
        'service_name': service_name,
        'operation': operation,
        'start_time': time.time() - duration,
        'end_time': time.time(),
        'duration': duration,
        'status': status,
        'metadata': {
            'request_id': request_id,
            'service_version': version
        }
    }
    
    # Store trace
    execute("""
        INSERT INTO traces (
            trace_id, service_name, operation, start_time, 
            end_time, duration, status, metadata
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
    """, span['trace_id'], span['service_name'], span['operation'],
        span['start_time'], span['end_time'], span['duration'],
        span['status'], json.dumps(span['metadata']))
    
    return span
'''

# Service metrics
collect_metrics_fujsen = '''
def collect_metrics():
    # Collect service metrics
    metrics = {
        'requests_total': get_request_count(),
        'requests_per_second': calculate_rps(),
        'average_response_time': get_avg_response_time(),
        'error_rate': get_error_rate(),
        'active_connections': get_active_connections(),
        'memory_usage': get_memory_usage(),
        'cpu_usage': get_cpu_usage()
    }
    
    # Store metrics
    for metric_name, value in metrics.items():
        record_metric(metric_name, value, {'service': name})
    
    return metrics
'''

# Service alerts
check_alerts_fujsen = '''
def check_alerts():
    alerts = []
    
    # Check error rate
    error_rate = get_error_rate()
    if error_rate > 5:  # 5% error rate threshold
        alerts.append({
            'type': 'high_error_rate',
            'severity': 'warning',
            'message': f'Error rate is {error_rate}%',
            'value': error_rate,
            'threshold': 5
        })
    
    # Check response time
    avg_response_time = get_avg_response_time()
    if avg_response_time > 2:  # 2 second threshold
        alerts.append({
            'type': 'high_response_time',
            'severity': 'warning',
            'message': f'Average response time is {avg_response_time}s',
            'value': avg_response_time,
            'threshold': 2
        })
    
    # Check memory usage
    memory_usage = get_memory_usage()
    if memory_usage > 80:  # 80% memory threshold
        alerts.append({
            'type': 'high_memory_usage',
            'severity': 'critical',
            'message': f'Memory usage is {memory_usage}%',
            'value': memory_usage,
            'threshold': 80
        })
    
    return alerts
'''

# Service dashboard
generate_dashboard_fujsen = '''
def generate_dashboard():
    # Generate service dashboard data
    dashboard_data = {
        'service_info': {
            'name': name,
            'version': version,
            'status': 'healthy',
            'uptime': get_uptime()
        },
        'metrics': collect_metrics(),
        'recent_requests': get_recent_requests(),
        'error_breakdown': get_error_breakdown(),
        'dependencies': get_dependency_status()
    }
    
    return dashboard_data
'''
""")
```

## 🔧 Service Configuration Management

### Configuration Management

```python
# Configuration management
config_management = TSK.from_string("""
[configuration_management]
# Configuration store
config_store: @env("CONFIG_STORE", "http://config-service:8005")

# Load configuration
load_config_fujsen = '''
def load_config(service_name, environment=None):
    if not environment:
        environment = @env("SERVICE_ENV", "development")
    
    try:
        # Load from config service
        response = requests.get(f"{config_store}/config/{service_name}/{environment}")
        if response.status_code == 200:
            return response.json()
    except:
        pass
    
    # Fallback to local config
    return load_local_config(service_name, environment)
'''

# Configuration validation
validate_config_fujsen = '''
def validate_config(config, schema):
    errors = []
    
    for field, rules in schema.items():
        if field not in config:
            if rules.get('required', False):
                errors.append(f"Missing required config: {field}")
            continue
        
        value = config[field]
        
        # Type validation
        if 'type' in rules:
            if not isinstance(value, rules['type']):
                errors.append(f"Config {field} must be {rules['type'].__name__}")
        
        # Value validation
        if 'min' in rules and value < rules['min']:
            errors.append(f"Config {field} must be at least {rules['min']}")
        
        if 'max' in rules and value > rules['max']:
            errors.append(f"Config {field} must be at most {rules['max']}")
    
    if errors:
        raise ValueError("Configuration validation failed: " + "; ".join(errors))
    
    return True
'''

# Hot reload configuration
hot_reload_config_fujsen = '''
def hot_reload_config():
    # Watch for configuration changes
    import time
    
    last_config_hash = None
    
    while True:
        try:
            # Get current config
            current_config = load_config(name)
            current_hash = hash(json.dumps(current_config, sort_keys=True))
            
            if current_hash != last_config_hash:
                # Configuration changed
                validate_and_apply_config(current_config)
                last_config_hash = current_hash
                
                log_info("Configuration reloaded")
        
        except Exception as e:
            log_error("Configuration reload failed", e)
        
        time.sleep(30)  # Check every 30 seconds
'''

# Apply configuration
apply_config_fujsen = '''
def apply_config(config):
    # Apply configuration changes
    for key, value in config.items():
        if key == 'database':
            update_database_config(value)
        elif key == 'cache':
            update_cache_config(value)
        elif key == 'logging':
            update_logging_config(value)
        elif key == 'security':
            update_security_config(value)
    
    log_info("Configuration applied successfully")
'''
""")
```

## 🎯 Microservices Best Practices

### 1. Service Design
- Design services around business capabilities
- Keep services small and focused
- Use appropriate communication patterns
- Implement proper error handling

### 2. Service Communication
- Use asynchronous communication when possible
- Implement circuit breakers for resilience
- Use service mesh for complex routing
- Monitor service dependencies

### 3. Data Management
- Use database per service pattern
- Implement eventual consistency
- Use event sourcing for data synchronization
- Handle distributed transactions carefully

### 4. Deployment & Operations
- Use containerization for consistency
- Implement proper service discovery
- Use health checks and monitoring
- Implement proper logging and tracing

### 5. Security
- Implement service-to-service authentication
- Use secure communication protocols
- Implement proper access controls
- Monitor for security threats

## 🚀 Next Steps

1. **Design your service architecture** with clear boundaries
2. **Implement service discovery** and registration
3. **Set up inter-service communication** with proper error handling
4. **Add monitoring and tracing** for observability
5. **Implement configuration management** for flexibility

---

**"We don't bow to any king"** - TuskLang provides powerful tools for building and orchestrating microservices architectures. Design with clear boundaries, implement proper communication, and build scalable, resilient systems! 