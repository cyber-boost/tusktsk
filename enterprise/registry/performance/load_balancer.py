#!/usr/bin/env python3
"""
TuskLang Package Registry Load Balancer
Advanced load balancing and scaling system
"""

import time
import json
import random
from typing import Dict, List, Optional, Any, Tuple
from dataclasses import dataclass
from enum import Enum
import threading
import socket

class LoadBalancingAlgorithm(Enum):
    """Load balancing algorithms"""
    ROUND_ROBIN = "round_robin"
    LEAST_CONNECTIONS = "least_connections"
    WEIGHTED_ROUND_ROBIN = "weighted_round_robin"
    IP_HASH = "ip_hash"
    LEAST_RESPONSE_TIME = "least_response_time"

class ServerStatus(Enum):
    """Server status"""
    HEALTHY = "healthy"
    UNHEALTHY = "unhealthy"
    MAINTENANCE = "maintenance"
    OVERLOADED = "overloaded"

@dataclass
class ServerInfo:
    """Server information"""
    server_id: str
    host: str
    port: int
    weight: int
    max_connections: int
    current_connections: int
    response_time: float
    status: ServerStatus
    last_health_check: float
    health_check_interval: float

class HealthChecker:
    """Server health checking system"""
    
    def __init__(self, check_interval: float = 30.0):
        self.check_interval = check_interval
        self.health_checks: Dict[str, Dict] = {}
        self.check_thread = None
        self.running = False
    
    def start(self):
        """Start health checking"""
        self.running = True
        self.check_thread = threading.Thread(target=self._health_check_loop)
        self.check_thread.daemon = True
        self.check_thread.start()
    
    def stop(self):
        """Stop health checking"""
        self.running = False
        if self.check_thread:
            self.check_thread.join()
    
    def add_server(self, server_id: str, host: str, port: int, 
                   health_endpoint: str = "/health"):
        """Add server for health checking"""
        self.health_checks[server_id] = {
            'host': host,
            'port': port,
            'health_endpoint': health_endpoint,
            'last_check': 0,
            'last_status': ServerStatus.UNHEALTHY,
            'consecutive_failures': 0,
            'consecutive_successes': 0
        }
    
    def _health_check_loop(self):
        """Health check loop"""
        while self.running:
            for server_id, check_info in self.health_checks.items():
                if time.time() - check_info['last_check'] >= self.check_interval:
                    self._check_server_health(server_id, check_info)
            
            time.sleep(5)  # Check every 5 seconds
    
    def _check_server_health(self, server_id: str, check_info: Dict):
        """Check health of a specific server"""
        try:
            # Simple TCP connection check
            sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            sock.settimeout(5)
            result = sock.connect_ex((check_info['host'], check_info['port']))
            sock.close()
            
            if result == 0:
                check_info['last_status'] = ServerStatus.HEALTHY
                check_info['consecutive_successes'] += 1
                check_info['consecutive_failures'] = 0
            else:
                check_info['consecutive_failures'] += 1
                if check_info['consecutive_failures'] >= 3:
                    check_info['last_status'] = ServerStatus.UNHEALTHY
                check_info['consecutive_successes'] = 0
            
            check_info['last_check'] = time.time()
            
        except Exception as e:
            check_info['consecutive_failures'] += 1
            if check_info['consecutive_failures'] >= 3:
                check_info['last_status'] = ServerStatus.UNHEALTHY
            check_info['last_check'] = time.time()
    
    def get_server_status(self, server_id: str) -> Optional[ServerStatus]:
        """Get current status of a server"""
        if server_id in self.health_checks:
            return self.health_checks[server_id]['last_status']
        return None

class LoadBalancer:
    """Load balancer with multiple algorithms"""
    
    def __init__(self, algorithm: LoadBalancingAlgorithm = LoadBalancingAlgorithm.ROUND_ROBIN):
        self.algorithm = algorithm
        self.servers: Dict[str, ServerInfo] = {}
        self.health_checker = HealthChecker()
        self.current_index = 0
        self.server_weights: Dict[str, int] = {}
        self.connection_counts: Dict[str, int] = {}
        self.response_times: Dict[str, List[float]] = {}
        
        self.health_checker.start()
    
    def add_server(self, server_id: str, host: str, port: int, 
                   weight: int = 1, max_connections: int = 1000):
        """Add server to load balancer"""
        server = ServerInfo(
            server_id=server_id,
            host=host,
            port=port,
            weight=weight,
            max_connections=max_connections,
            current_connections=0,
            response_time=0.0,
            status=ServerStatus.HEALTHY,
            last_health_check=time.time(),
            health_check_interval=30.0
        )
        
        self.servers[server_id] = server
        self.server_weights[server_id] = weight
        self.connection_counts[server_id] = 0
        self.response_times[server_id] = []
        
        # Add to health checker
        self.health_checker.add_server(server_id, host, port)
    
    def remove_server(self, server_id: str):
        """Remove server from load balancer"""
        if server_id in self.servers:
            del self.servers[server_id]
            del self.server_weights[server_id]
            del self.connection_counts[server_id]
            del self.response_times[server_id]
    
    def get_server(self, client_ip: str = None) -> Optional[ServerInfo]:
        """Get next server based on algorithm"""
        healthy_servers = [
            server for server in self.servers.values()
            if server.status == ServerStatus.HEALTHY and 
               server.current_connections < server.max_connections
        ]
        
        if not healthy_servers:
            return None
        
        if self.algorithm == LoadBalancingAlgorithm.ROUND_ROBIN:
            return self._round_robin_select(healthy_servers)
        elif self.algorithm == LoadBalancingAlgorithm.LEAST_CONNECTIONS:
            return self._least_connections_select(healthy_servers)
        elif self.algorithm == LoadBalancingAlgorithm.WEIGHTED_ROUND_ROBIN:
            return self._weighted_round_robin_select(healthy_servers)
        elif self.algorithm == LoadBalancingAlgorithm.IP_HASH:
            return self._ip_hash_select(healthy_servers, client_ip)
        elif self.algorithm == LoadBalancingAlgorithm.LEAST_RESPONSE_TIME:
            return self._least_response_time_select(healthy_servers)
        else:
            return healthy_servers[0]  # Default to first server
    
    def _round_robin_select(self, servers: List[ServerInfo]) -> ServerInfo:
        """Round robin selection"""
        if not servers:
            return None
        
        server = servers[self.current_index % len(servers)]
        self.current_index += 1
        return server
    
    def _least_connections_select(self, servers: List[ServerInfo]) -> ServerInfo:
        """Least connections selection"""
        if not servers:
            return None
        
        return min(servers, key=lambda s: s.current_connections)
    
    def _weighted_round_robin_select(self, servers: List[ServerInfo]) -> ServerInfo:
        """Weighted round robin selection"""
        if not servers:
            return None
        
        # Calculate total weight
        total_weight = sum(server.weight for server in servers)
        
        # Generate random number
        rand = random.uniform(0, total_weight)
        
        # Select server based on weight
        current_weight = 0
        for server in servers:
            current_weight += server.weight
            if rand <= current_weight:
                return server
        
        return servers[-1]  # Fallback
    
    def _ip_hash_select(self, servers: List[ServerInfo], client_ip: str) -> ServerInfo:
        """IP hash selection"""
        if not servers or not client_ip:
            return servers[0] if servers else None
        
        # Hash client IP
        hash_value = hash(client_ip)
        index = hash_value % len(servers)
        return servers[index]
    
    def _least_response_time_select(self, servers: List[ServerInfo]) -> ServerInfo:
        """Least response time selection"""
        if not servers:
            return None
        
        return min(servers, key=lambda s: s.response_time)
    
    def record_connection(self, server_id: str):
        """Record a new connection to server"""
        if server_id in self.servers:
            self.servers[server_id].current_connections += 1
            self.connection_counts[server_id] += 1
    
    def record_disconnection(self, server_id: str):
        """Record a disconnection from server"""
        if server_id in self.servers:
            self.servers[server_id].current_connections = max(0, 
                self.servers[server_id].current_connections - 1)
    
    def record_response_time(self, server_id: str, response_time: float):
        """Record response time for server"""
        if server_id in self.servers:
            self.servers[server_id].response_time = response_time
            self.response_times[server_id].append(response_time)
            
            # Keep only last 100 response times
            if len(self.response_times[server_id]) > 100:
                self.response_times[server_id] = self.response_times[server_id][-100:]
    
    def get_server_stats(self) -> Dict[str, Any]:
        """Get load balancer statistics"""
        stats = {
            'algorithm': self.algorithm.value,
            'total_servers': len(self.servers),
            'healthy_servers': len([s for s in self.servers.values() 
                                  if s.status == ServerStatus.HEALTHY]),
            'total_connections': sum(s.current_connections for s in self.servers.values()),
            'servers': {}
        }
        
        for server_id, server in self.servers.items():
            avg_response_time = 0
            if self.response_times[server_id]:
                avg_response_time = sum(self.response_times[server_id]) / len(self.response_times[server_id])
            
            stats['servers'][server_id] = {
                'host': server.host,
                'port': server.port,
                'status': server.status.value,
                'current_connections': server.current_connections,
                'max_connections': server.max_connections,
                'weight': server.weight,
                'response_time': server.response_time,
                'avg_response_time': avg_response_time,
                'total_connections': self.connection_counts[server_id]
            }
        
        return stats

class AutoScaler:
    """Automatic scaling system"""
    
    def __init__(self, load_balancer: LoadBalancer):
        self.load_balancer = load_balancer
        self.scaling_rules: Dict[str, Dict] = {}
        self.scaling_history: List[Dict] = []
        self.scaling_enabled = True
        
        # Default scaling rules
        self.scaling_rules = {
            'cpu_threshold': 80.0,  # Scale up if CPU > 80%
            'memory_threshold': 85.0,  # Scale up if memory > 85%
            'connection_threshold': 0.9,  # Scale up if connections > 90% of max
            'response_time_threshold': 1000.0,  # Scale up if response time > 1s
            'scale_down_threshold': 0.3,  # Scale down if utilization < 30%
            'min_servers': 2,
            'max_servers': 10
        }
    
    def check_scaling_needs(self) -> Dict[str, Any]:
        """Check if scaling is needed"""
        if not self.scaling_enabled:
            return {'action': 'none', 'reason': 'scaling_disabled'}
        
        stats = self.load_balancer.get_server_stats()
        healthy_servers = stats['healthy_servers']
        total_connections = stats['total_connections']
        
        # Calculate average connections per server
        if healthy_servers > 0:
            avg_connections = total_connections / healthy_servers
        else:
            avg_connections = 0
        
        # Check for scale up
        for server in self.load_balancer.servers.values():
            if server.status == ServerStatus.HEALTHY:
                connection_ratio = server.current_connections / server.max_connections
                
                if (connection_ratio > self.scaling_rules['connection_threshold'] or
                    server.response_time > self.scaling_rules['response_time_threshold']):
                    
                    if healthy_servers < self.scaling_rules['max_servers']:
                        return {
                            'action': 'scale_up',
                            'reason': f'High load: {connection_ratio:.2%} connections, {server.response_time:.0f}ms response time',
                            'current_servers': healthy_servers,
                            'target_servers': healthy_servers + 1
                        }
        
        # Check for scale down
        if healthy_servers > self.scaling_rules['min_servers']:
            if avg_connections < self.scaling_rules['scale_down_threshold']:
                return {
                    'action': 'scale_down',
                    'reason': f'Low utilization: {avg_connections:.2%} average connections',
                    'current_servers': healthy_servers,
                    'target_servers': healthy_servers - 1
                }
        
        return {'action': 'none', 'reason': 'no_scaling_needed'}
    
    def scale_up(self) -> bool:
        """Scale up by adding a new server"""
        try:
            # This would create a new server instance in a real implementation
            new_server_id = f"server_{int(time.time())}"
            new_host = f"registry-{new_server_id}.tusklang.org"
            new_port = 8080
            
            self.load_balancer.add_server(new_server_id, new_host, new_port)
            
            # Record scaling action
            self.scaling_history.append({
                'timestamp': time.time(),
                'action': 'scale_up',
                'server_id': new_server_id,
                'reason': 'High load detected'
            })
            
            return True
            
        except Exception as e:
            print(f"Error scaling up: {e}")
            return False
    
    def scale_down(self) -> bool:
        """Scale down by removing a server"""
        try:
            # Find server with least connections
            servers = [s for s in self.load_balancer.servers.values() 
                      if s.status == ServerStatus.HEALTHY]
            
            if len(servers) <= self.scaling_rules['min_servers']:
                return False
            
            # Remove server with least connections
            server_to_remove = min(servers, key=lambda s: s.current_connections)
            
            self.load_balancer.remove_server(server_to_remove.server_id)
            
            # Record scaling action
            self.scaling_history.append({
                'timestamp': time.time(),
                'action': 'scale_down',
                'server_id': server_to_remove.server_id,
                'reason': 'Low utilization detected'
            })
            
            return True
            
        except Exception as e:
            print(f"Error scaling down: {e}")
            return False
    
    def get_scaling_history(self) -> List[Dict]:
        """Get scaling history"""
        return self.scaling_history

# Global load balancer instances
load_balancer = LoadBalancer(LoadBalancingAlgorithm.LEAST_CONNECTIONS)
auto_scaler = AutoScaler(load_balancer) 