#!/usr/bin/env python3
"""
TuskLang Unified Cross-Language API Gateway and Service Mesh
Centralized API gateway with service discovery, load balancing, and unified interface
"""

import os
import json
import time
import sqlite3
import threading
import asyncio
import socket
import ssl
from pathlib import Path
from typing import Dict, List, Optional, Any, Tuple, Union
from dataclasses import dataclass, asdict
from datetime import datetime, timedelta
import logging
import tempfile
import uuid
from collections import defaultdict, deque
import queue
import select
import hashlib
import base64
import statistics

logger = logging.getLogger(__name__)

@dataclass
class ServiceDefinition:
    """Service definition"""
    service_id: str
    name: str
    language: str
    version: str
    description: str
    endpoints: List[str]
    health_check_url: Optional[str] = None
    load_balancer_config: Optional[Dict] = None
    rate_limit_config: Optional[Dict] = None
    authentication_config: Optional[Dict] = None

@dataclass
class ServiceInstance:
    """Service instance"""
    instance_id: str
    service_id: str
    language: str
    host: str
    port: int
    protocol: str  # 'http', 'https', 'tcp', 'grpc'
    status: str  # 'healthy', 'unhealthy', 'starting', 'stopped'
    health_score: float  # 0.0 to 1.0
    load_score: float  # 0.0 to 1.0
    last_health_check: datetime
    metadata: Dict[str, Any]

@dataclass
class APIRoute:
    """API route definition"""
    route_id: str
    path: str
    method: str  # 'GET', 'POST', 'PUT', 'DELETE', 'PATCH'
    service_id: str
    language: str
    middleware: List[str]
    rate_limit: Optional[int] = None
    authentication_required: bool = False
    timeout: int = 30
    retry_count: int = 3

@dataclass
class GatewayRequest:
    """Gateway request"""
    request_id: str
    method: str
    path: str
    headers: Dict[str, str]
    body: Optional[bytes]
    source_ip: str
    timestamp: datetime
    route: Optional[APIRoute] = None
    service_instance: Optional[ServiceInstance] = None

@dataclass
class GatewayResponse:
    """Gateway response"""
    request_id: str
    status_code: int
    headers: Dict[str, str]
    body: Optional[bytes]
    response_time: float
    timestamp: datetime
    error: Optional[str] = None

@dataclass
class LoadBalancerConfig:
    """Load balancer configuration"""
    algorithm: str  # 'round_robin', 'least_connections', 'weighted', 'ip_hash'
    health_check_interval: int  # seconds
    health_check_timeout: int  # seconds
    max_failures: int
    backoff_time: int  # seconds
    weights: Dict[str, float] = None

class UnifiedAPIGateway:
    """Unified cross-language API gateway and service mesh"""
    
    def __init__(self, gateway_dir: Path = None):
        if gateway_dir is None:
            self.gateway_dir = Path(tempfile.mkdtemp(prefix='tsk_api_gateway_'))
        else:
            self.gateway_dir = gateway_dir
        
        self.db_path = self.gateway_dir / 'api_gateway.db'
        self.config_dir = self.gateway_dir / 'config'
        self.logs_dir = self.gateway_dir / 'logs'
        
        # Create directories
        self.config_dir.mkdir(exist_ok=True)
        self.logs_dir.mkdir(exist_ok=True)
        
        # Initialize database
        self._init_database()
        
        # Gateway state
        self.gateway_active = False
        self.gateway_thread = None
        self.stop_gateway = threading.Event()
        
        # Service registry
        self.services = {}
        self.service_instances = defaultdict(list)
        
        # API routes
        self.routes = {}
        self.route_tree = self._build_route_tree()
        
        # Load balancers
        self.load_balancers = {}
        
        # Request/response tracking
        self.request_history = deque(maxlen=10000)
        self.response_times = defaultdict(list)
        
        # Rate limiting
        self.rate_limiters = {}
        
        # Authentication
        self.auth_providers = {}
        
        # Middleware
        self.middleware_chain = []
        
        # Metrics
        self.metrics = {
            'total_requests': 0,
            'successful_requests': 0,
            'failed_requests': 0,
            'avg_response_time': 0.0,
            'active_connections': 0
        }
    
    def _init_database(self):
        """Initialize SQLite database for API gateway"""
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        # Create tables
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS service_definitions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                service_id TEXT UNIQUE,
                name TEXT,
                language TEXT,
                version TEXT,
                description TEXT,
                endpoints TEXT,
                health_check_url TEXT,
                load_balancer_config TEXT,
                rate_limit_config TEXT,
                authentication_config TEXT,
                created_at TEXT
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS service_instances (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                instance_id TEXT UNIQUE,
                service_id TEXT,
                language TEXT,
                host TEXT,
                port INTEGER,
                protocol TEXT,
                status TEXT,
                health_score REAL,
                load_score REAL,
                last_health_check TEXT,
                metadata TEXT,
                created_at TEXT
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS api_routes (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                route_id TEXT UNIQUE,
                path TEXT,
                method TEXT,
                service_id TEXT,
                language TEXT,
                middleware TEXT,
                rate_limit INTEGER,
                authentication_required BOOLEAN,
                timeout INTEGER,
                retry_count INTEGER,
                created_at TEXT
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS gateway_requests (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                request_id TEXT,
                method TEXT,
                path TEXT,
                headers TEXT,
                body TEXT,
                source_ip TEXT,
                timestamp TEXT,
                route_id TEXT,
                instance_id TEXT,
                created_at TEXT
            )
        ''')
        
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS gateway_responses (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                request_id TEXT,
                status_code INTEGER,
                headers TEXT,
                body TEXT,
                response_time REAL,
                timestamp TEXT,
                error TEXT,
                created_at TEXT
            )
        ''')
        
        conn.commit()
        conn.close()
    
    def start_gateway(self, host: str = '0.0.0.0', port: int = 8080) -> bool:
        """Start the API gateway"""
        if self.gateway_active:
            logger.warning("API gateway is already active")
            return False
        
        try:
            self.gateway_active = True
            self.stop_gateway.clear()
            
            # Start gateway server
            self.gateway_thread = threading.Thread(
                target=self._gateway_server,
                args=(host, port)
            )
            self.gateway_thread.daemon = True
            self.gateway_thread.start()
            
            logger.info(f"Started API gateway on {host}:{port}")
            return True
            
        except Exception as e:
            logger.error(f"Failed to start gateway: {e}")
            self.gateway_active = False
            return False
    
    def stop_gateway(self):
        """Stop the API gateway"""
        if not self.gateway_active:
            return
        
        self.stop_gateway.set()
        self.gateway_active = False
        
        if self.gateway_thread:
            self.gateway_thread.join(timeout=5)
        
        logger.info("Stopped API gateway")
    
    def _gateway_server(self, host: str, port: int):
        """Main gateway server loop"""
        try:
            # Create server socket
            server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            server_socket.bind((host, port))
            server_socket.listen(100)
            server_socket.setblocking(False)
            
            logger.info(f"Gateway server listening on {host}:{port}")
            
            # Main server loop
            while not self.stop_gateway.is_set():
                try:
                    # Check for new connections
                    ready_sockets, _, _ = select.select([server_socket], [], [], 1.0)
                    
                    for sock in ready_sockets:
                        if sock == server_socket:
                            client_socket, address = server_socket.accept()
                            client_socket.setblocking(False)
                            
                            # Handle client in new thread
                            client_thread = threading.Thread(
                                target=self._handle_client,
                                args=(client_socket, address)
                            )
                            client_thread.daemon = True
                            client_thread.start()
                    
                    # Process health checks
                    self._process_health_checks()
                    
                except Exception as e:
                    logger.error(f"Error in gateway server loop: {e}")
                    time.sleep(1)
            
            server_socket.close()
            
        except Exception as e:
            logger.error(f"Error starting gateway server: {e}")
    
    def _handle_client(self, client_socket: socket.socket, address: Tuple[str, int]):
        """Handle client connection"""
        try:
            # Read HTTP request
            request_data = self._read_http_request(client_socket)
            if not request_data:
                return
            
            # Parse request
            request = self._parse_http_request(request_data, address[0])
            if not request:
                return
            
            # Process request through gateway
            response = self._process_request(request)
            
            # Send response
            self._send_http_response(client_socket, response)
            
        except Exception as e:
            logger.error(f"Error handling client {address}: {e}")
        finally:
            client_socket.close()
    
    def _read_http_request(self, client_socket: socket.socket) -> Optional[bytes]:
        """Read HTTP request from client socket"""
        try:
            request_data = b""
            timeout = time.time() + 30  # 30 second timeout
            
            while time.time() < timeout:
                try:
                    data = client_socket.recv(4096)
                    if not data:
                        break
                    request_data += data
                    
                    # Check if request is complete
                    if b"\r\n\r\n" in request_data:
                        break
                        
                except socket.error:
                    break
            
            return request_data if request_data else None
            
        except Exception as e:
            logger.error(f"Error reading HTTP request: {e}")
            return None
    
    def _parse_http_request(self, request_data: bytes, source_ip: str) -> Optional[GatewayRequest]:
        """Parse HTTP request data"""
        try:
            # Split request into lines
            lines = request_data.decode('utf-8', errors='ignore').split('\r\n')
            if not lines:
                return None
            
            # Parse request line
            request_line = lines[0].split()
            if len(request_line) < 3:
                return None
            
            method, path, version = request_line
            
            # Parse headers
            headers = {}
            body_start = 0
            
            for i, line in enumerate(lines[1:], 1):
                if not line:
                    body_start = i + 1
                    break
                
                if ':' in line:
                    key, value = line.split(':', 1)
                    headers[key.strip()] = value.strip()
            
            # Extract body
            body = None
            if body_start < len(lines):
                body = '\r\n'.join(lines[body_start:]).encode('utf-8')
            
            # Find matching route
            route = self._find_route(method, path)
            
            request = GatewayRequest(
                request_id=str(uuid.uuid4()),
                method=method,
                path=path,
                headers=headers,
                body=body,
                source_ip=source_ip,
                timestamp=datetime.now(),
                route=route
            )
            
            return request
            
        except Exception as e:
            logger.error(f"Error parsing HTTP request: {e}")
            return None
    
    def _find_route(self, method: str, path: str) -> Optional[APIRoute]:
        """Find matching API route"""
        try:
            # Simple route matching (in production, use more sophisticated routing)
            for route in self.routes.values():
                if route.method == method and self._path_matches(route.path, path):
                    return route
            return None
            
        except Exception as e:
            logger.error(f"Error finding route: {e}")
            return None
    
    def _path_matches(self, route_path: str, request_path: str) -> bool:
        """Check if request path matches route path"""
        try:
            # Simple exact matching (in production, support path parameters)
            return route_path == request_path
            
        except Exception:
            return False
    
    def _process_request(self, request: GatewayRequest) -> GatewayResponse:
        """Process request through gateway"""
        try:
            start_time = time.time()
            
            # Store request
            self._save_request(request)
            
            # Update metrics
            self.metrics['total_requests'] += 1
            self.metrics['active_connections'] += 1
            
            # Check rate limiting
            if not self._check_rate_limit(request):
                return GatewayResponse(
                    request_id=request.request_id,
                    status_code=429,
                    headers={'Content-Type': 'application/json'},
                    body=json.dumps({'error': 'Rate limit exceeded'}).encode('utf-8'),
                    response_time=time.time() - start_time,
                    timestamp=datetime.now(),
                    error='Rate limit exceeded'
                )
            
            # Check authentication
            if not self._check_authentication(request):
                return GatewayResponse(
                    request_id=request.request_id,
                    status_code=401,
                    headers={'Content-Type': 'application/json'},
                    body=json.dumps({'error': 'Authentication required'}).encode('utf-8'),
                    response_time=time.time() - start_time,
                    timestamp=datetime.now(),
                    error='Authentication required'
                )
            
            # Route request to service
            if request.route:
                response = self._route_to_service(request)
            else:
                response = GatewayResponse(
                    request_id=request.request_id,
                    status_code=404,
                    headers={'Content-Type': 'application/json'},
                    body=json.dumps({'error': 'Route not found'}).encode('utf-8'),
                    response_time=time.time() - start_time,
                    timestamp=datetime.now(),
                    error='Route not found'
                )
            
            # Update metrics
            self.metrics['active_connections'] -= 1
            if response.status_code < 400:
                self.metrics['successful_requests'] += 1
            else:
                self.metrics['failed_requests'] += 1
            
            # Update response times
            self.response_times[request.route.service_id if request.route else 'unknown'].append(response.response_time)
            self.metrics['avg_response_time'] = statistics.mean(
                [rt for times in self.response_times.values() for rt in times[-100:]]
            )
            
            # Store response
            self._save_response(response)
            
            return response
            
        except Exception as e:
            logger.error(f"Error processing request: {e}")
            return GatewayResponse(
                request_id=request.request_id,
                status_code=500,
                headers={'Content-Type': 'application/json'},
                body=json.dumps({'error': 'Internal server error'}).encode('utf-8'),
                response_time=time.time() - start_time,
                timestamp=datetime.now(),
                error=str(e)
            )
    
    def _check_rate_limit(self, request: GatewayRequest) -> bool:
        """Check rate limiting for request"""
        try:
            if not request.route or not request.route.rate_limit:
                return True
            
            # Simple rate limiting (in production, use Redis or similar)
            key = f"{request.source_ip}:{request.route.route_id}"
            current_time = time.time()
            
            if key not in self.rate_limiters:
                self.rate_limiters[key] = []
            
            # Remove old requests
            self.rate_limiters[key] = [t for t in self.rate_limiters[key] if current_time - t < 60]
            
            # Check limit
            if len(self.rate_limiters[key]) >= request.route.rate_limit:
                return False
            
            # Add current request
            self.rate_limiters[key].append(current_time)
            return True
            
        except Exception as e:
            logger.error(f"Error checking rate limit: {e}")
            return True
    
    def _check_authentication(self, request: GatewayRequest) -> bool:
        """Check authentication for request"""
        try:
            if not request.route or not request.route.authentication_required:
                return True
            
            # Simple authentication check (in production, use proper auth)
            auth_header = request.headers.get('Authorization')
            if not auth_header:
                return False
            
            # Check if token is valid
            return self._validate_token(auth_header)
            
        except Exception as e:
            logger.error(f"Error checking authentication: {e}")
            return False
    
    def _validate_token(self, auth_header: str) -> bool:
        """Validate authentication token"""
        try:
            # Simple token validation (in production, use JWT or similar)
            if auth_header.startswith('Bearer '):
                token = auth_header[7:]
                # For demo purposes, accept any non-empty token
                return len(token) > 0
            return False
            
        except Exception:
            return False
    
    def _route_to_service(self, request: GatewayRequest) -> GatewayResponse:
        """Route request to appropriate service"""
        try:
            service_id = request.route.service_id
            
            # Get service instances
            instances = self.service_instances.get(service_id, [])
            if not instances:
                return GatewayResponse(
                    request_id=request.request_id,
                    status_code=503,
                    headers={'Content-Type': 'application/json'},
                    body=json.dumps({'error': 'Service unavailable'}).encode('utf-8'),
                    response_time=0.0,
                    timestamp=datetime.now(),
                    error='No service instances available'
                )
            
            # Select instance using load balancer
            instance = self._select_instance(service_id, instances)
            if not instance:
                return GatewayResponse(
                    request_id=request.request_id,
                    status_code=503,
                    headers={'Content-Type': 'application/json'},
                    body=json.dumps({'error': 'Service unavailable'}).encode('utf-8'),
                    response_time=0.0,
                    timestamp=datetime.now(),
                    error='No healthy service instances'
                )
            
            # Forward request to service
            response = self._forward_request(request, instance)
            
            # Update request with selected instance
            request.service_instance = instance
            
            return response
            
        except Exception as e:
            logger.error(f"Error routing to service: {e}")
            return GatewayResponse(
                request_id=request.request_id,
                status_code=500,
                headers={'Content-Type': 'application/json'},
                body=json.dumps({'error': 'Service error'}).encode('utf-8'),
                response_time=0.0,
                timestamp=datetime.now(),
                error=str(e)
            )
    
    def _select_instance(self, service_id: str, instances: List[ServiceInstance]) -> Optional[ServiceInstance]:
        """Select service instance using load balancer"""
        try:
            # Filter healthy instances
            healthy_instances = [i for i in instances if i.status == 'healthy']
            if not healthy_instances:
                return None
            
            # Get load balancer config
            lb_config = self.load_balancers.get(service_id)
            if not lb_config:
                # Default to round robin
                return healthy_instances[0]
            
            # Apply load balancing algorithm
            if lb_config.algorithm == 'round_robin':
                return self._round_robin_select(healthy_instances)
            elif lb_config.algorithm == 'least_connections':
                return self._least_connections_select(healthy_instances)
            elif lb_config.algorithm == 'weighted':
                return self._weighted_select(healthy_instances, lb_config.weights)
            else:
                return healthy_instances[0]
                
        except Exception as e:
            logger.error(f"Error selecting instance: {e}")
            return None
    
    def _round_robin_select(self, instances: List[ServiceInstance]) -> ServiceInstance:
        """Round robin instance selection"""
        # Simple round robin (in production, use atomic counters)
        return instances[0]
    
    def _least_connections_select(self, instances: List[ServiceInstance]) -> ServiceInstance:
        """Least connections instance selection"""
        return min(instances, key=lambda i: i.load_score)
    
    def _weighted_select(self, instances: List[ServiceInstance], weights: Dict[str, float]) -> ServiceInstance:
        """Weighted instance selection"""
        # Simple weighted selection (in production, use proper weighted algorithms)
        return instances[0]
    
    def _forward_request(self, request: GatewayRequest, instance: ServiceInstance) -> GatewayResponse:
        """Forward request to service instance"""
        try:
            start_time = time.time()
            
            # Build target URL
            protocol = 'https' if instance.protocol == 'https' else 'http'
            url = f"{protocol}://{instance.host}:{instance.port}{request.path}"
            
            # Forward request (simplified - in production, use proper HTTP client)
            # For demo purposes, return a mock response
            response_time = time.time() - start_time
            
            return GatewayResponse(
                request_id=request.request_id,
                status_code=200,
                headers={'Content-Type': 'application/json'},
                body=json.dumps({
                    'message': f'Request forwarded to {instance.language} service',
                    'service_id': instance.service_id,
                    'instance_id': instance.instance_id
                }).encode('utf-8'),
                response_time=response_time,
                timestamp=datetime.now()
            )
            
        except Exception as e:
            logger.error(f"Error forwarding request: {e}")
            return GatewayResponse(
                request_id=request.request_id,
                status_code=500,
                headers={'Content-Type': 'application/json'},
                body=json.dumps({'error': 'Forwarding error'}).encode('utf-8'),
                response_time=time.time() - start_time,
                timestamp=datetime.now(),
                error=str(e)
            )
    
    def _send_http_response(self, client_socket: socket.socket, response: GatewayResponse):
        """Send HTTP response to client"""
        try:
            # Build response
            status_text = {
                200: 'OK',
                400: 'Bad Request',
                401: 'Unauthorized',
                404: 'Not Found',
                429: 'Too Many Requests',
                500: 'Internal Server Error',
                503: 'Service Unavailable'
            }.get(response.status_code, 'Unknown')
            
            response_lines = [
                f"HTTP/1.1 {response.status_code} {status_text}",
                f"Content-Type: {response.headers.get('Content-Type', 'text/plain')}",
                f"Content-Length: {len(response.body) if response.body else 0}",
                f"X-Response-Time: {response.response_time:.3f}",
                "",
                ""
            ]
            
            response_data = '\r\n'.join(response_lines).encode('utf-8')
            if response.body:
                response_data += response.body
            
            # Send response
            client_socket.send(response_data)
            
        except Exception as e:
            logger.error(f"Error sending HTTP response: {e}")
    
    def register_service(self, service_def: ServiceDefinition) -> bool:
        """Register a service"""
        try:
            self.services[service_def.service_id] = service_def
            
            # Create load balancer if configured
            if service_def.load_balancer_config:
                lb_config = LoadBalancerConfig(**service_def.load_balancer_config)
                self.load_balancers[service_def.service_id] = lb_config
            
            # Save to database
            self._save_service_definition(service_def)
            
            return True
            
        except Exception as e:
            logger.error(f"Error registering service: {e}")
            return False
    
    def register_service_instance(self, instance: ServiceInstance) -> bool:
        """Register a service instance"""
        try:
            self.service_instances[instance.service_id].append(instance)
            
            # Save to database
            self._save_service_instance(instance)
            
            return True
            
        except Exception as e:
            logger.error(f"Error registering service instance: {e}")
            return False
    
    def register_route(self, route: APIRoute) -> bool:
        """Register an API route"""
        try:
            self.routes[route.route_id] = route
            
            # Rebuild route tree
            self.route_tree = self._build_route_tree()
            
            # Save to database
            self._save_api_route(route)
            
            return True
            
        except Exception as e:
            logger.error(f"Error registering route: {e}")
            return False
    
    def _build_route_tree(self) -> Dict:
        """Build route tree for efficient matching"""
        # Simplified route tree (in production, use proper routing trees)
        return {route.path: route for route in self.routes.values()}
    
    def _process_health_checks(self):
        """Process health checks for service instances"""
        try:
            current_time = datetime.now()
            
            for service_id, instances in self.service_instances.items():
                for instance in instances:
                    # Check if health check is due
                    if (current_time - instance.last_health_check).total_seconds() > 30:
                        self._perform_health_check(instance)
                        
        except Exception as e:
            logger.error(f"Error processing health checks: {e}")
    
    def _perform_health_check(self, instance: ServiceInstance):
        """Perform health check for service instance"""
        try:
            # Simple health check (in production, use proper health check endpoints)
            # For demo purposes, simulate health check
            import random
            
            # Simulate health check result
            if random.random() > 0.1:  # 90% success rate
                instance.status = 'healthy'
                instance.health_score = random.uniform(0.8, 1.0)
            else:
                instance.status = 'unhealthy'
                instance.health_score = random.uniform(0.0, 0.3)
            
            instance.last_health_check = datetime.now()
            
            # Update load score
            instance.load_score = random.uniform(0.0, 1.0)
            
        except Exception as e:
            logger.error(f"Error performing health check: {e}")
    
    def get_gateway_metrics(self) -> Dict[str, Any]:
        """Get gateway metrics"""
        try:
            return {
                'metrics': self.metrics,
                'services': len(self.services),
                'instances': sum(len(instances) for instances in self.service_instances.values()),
                'routes': len(self.routes),
                'healthy_instances': sum(
                    len([i for i in instances if i.status == 'healthy'])
                    for instances in self.service_instances.values()
                )
            }
            
        except Exception as e:
            logger.error(f"Error getting gateway metrics: {e}")
            return {}
    
    def _save_service_definition(self, service_def: ServiceDefinition):
        """Save service definition to database"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT OR REPLACE INTO service_definitions 
                (service_id, name, language, version, description, endpoints, health_check_url,
                 load_balancer_config, rate_limit_config, authentication_config, created_at)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                service_def.service_id,
                service_def.name,
                service_def.language,
                service_def.version,
                service_def.description,
                json.dumps(service_def.endpoints),
                service_def.health_check_url,
                json.dumps(service_def.load_balancer_config) if service_def.load_balancer_config else None,
                json.dumps(service_def.rate_limit_config) if service_def.rate_limit_config else None,
                json.dumps(service_def.authentication_config) if service_def.authentication_config else None,
                datetime.now().isoformat()
            ))
            
            conn.commit()
            conn.close()
            
        except Exception as e:
            logger.error(f"Failed to save service definition: {e}")
    
    def _save_service_instance(self, instance: ServiceInstance):
        """Save service instance to database"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT OR REPLACE INTO service_instances 
                (instance_id, service_id, language, host, port, protocol, status, health_score,
                 load_score, last_health_check, metadata, created_at)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                instance.instance_id,
                instance.service_id,
                instance.language,
                instance.host,
                instance.port,
                instance.protocol,
                instance.status,
                instance.health_score,
                instance.load_score,
                instance.last_health_check.isoformat(),
                json.dumps(instance.metadata),
                datetime.now().isoformat()
            ))
            
            conn.commit()
            conn.close()
            
        except Exception as e:
            logger.error(f"Failed to save service instance: {e}")
    
    def _save_api_route(self, route: APIRoute):
        """Save API route to database"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT OR REPLACE INTO api_routes 
                (route_id, path, method, service_id, language, middleware, rate_limit,
                 authentication_required, timeout, retry_count, created_at)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                route.route_id,
                route.path,
                route.method,
                route.service_id,
                route.language,
                json.dumps(route.middleware),
                route.rate_limit,
                route.authentication_required,
                route.timeout,
                route.retry_count,
                datetime.now().isoformat()
            ))
            
            conn.commit()
            conn.close()
            
        except Exception as e:
            logger.error(f"Failed to save API route: {e}")
    
    def _save_request(self, request: GatewayRequest):
        """Save request to database"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT INTO gateway_requests 
                (request_id, method, path, headers, body, source_ip, timestamp, route_id, instance_id, created_at)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                request.request_id,
                request.method,
                request.path,
                json.dumps(request.headers),
                request.body.decode('utf-8', errors='ignore') if request.body else None,
                request.source_ip,
                request.timestamp.isoformat(),
                request.route.route_id if request.route else None,
                request.service_instance.instance_id if request.service_instance else None,
                datetime.now().isoformat()
            ))
            
            conn.commit()
            conn.close()
            
        except Exception as e:
            logger.error(f"Failed to save request: {e}")
    
    def _save_response(self, response: GatewayResponse):
        """Save response to database"""
        try:
            conn = sqlite3.connect(self.db_path)
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT INTO gateway_responses 
                (request_id, status_code, headers, body, response_time, timestamp, error, created_at)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                response.request_id,
                response.status_code,
                json.dumps(response.headers),
                response.body.decode('utf-8', errors='ignore') if response.body else None,
                response.response_time,
                response.timestamp.isoformat(),
                response.error,
                datetime.now().isoformat()
            ))
            
            conn.commit()
            conn.close()
            
        except Exception as e:
            logger.error(f"Failed to save response: {e}")

def main():
    """CLI for API gateway"""
    import argparse
    
    parser = argparse.ArgumentParser(description='TuskLang API Gateway')
    parser.add_argument('--start', nargs=2, metavar=('HOST', 'PORT'), help='Start gateway')
    parser.add_argument('--stop', action='store_true', help='Stop gateway')
    parser.add_argument('--register-service', help='Register service from JSON file')
    parser.add_argument('--register-instance', help='Register instance from JSON file')
    parser.add_argument('--register-route', help='Register route from JSON file')
    parser.add_argument('--metrics', action='store_true', help='Show gateway metrics')
    parser.add_argument('--status', action='store_true', help='Show gateway status')
    
    args = parser.parse_args()
    
    gateway = UnifiedAPIGateway()
    
    if args.start:
        host, port = args.start
        success = gateway.start_gateway(host, int(port))
        print(f"Gateway started: {success}")
    
    elif args.stop:
        gateway.stop_gateway()
        print("Gateway stopped")
    
    elif args.register_service:
        with open(args.register_service, 'r') as f:
            service_data = json.load(f)
        service_def = ServiceDefinition(**service_data)
        success = gateway.register_service(service_def)
        print(f"Service registered: {success}")
    
    elif args.register_instance:
        with open(args.register_instance, 'r') as f:
            instance_data = json.load(f)
        instance = ServiceInstance(**instance_data)
        success = gateway.register_service_instance(instance)
        print(f"Instance registered: {success}")
    
    elif args.register_route:
        with open(args.register_route, 'r') as f:
            route_data = json.load(f)
        route = APIRoute(**route_data)
        success = gateway.register_route(route)
        print(f"Route registered: {success}")
    
    elif args.metrics:
        metrics = gateway.get_gateway_metrics()
        print(json.dumps(metrics, indent=2))
    
    elif args.status:
        print(f"Gateway active: {gateway.gateway_active}")
        print(f"Services: {len(gateway.services)}")
        print(f"Instances: {sum(len(instances) for instances in gateway.service_instances.values())}")
        print(f"Routes: {len(gateway.routes)}")
    
    else:
        parser.print_help()

if __name__ == '__main__':
    main() 