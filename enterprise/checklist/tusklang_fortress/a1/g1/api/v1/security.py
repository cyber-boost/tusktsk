"""
TuskLang FORTRESS - Security API
Agent A1 - Goal G1: Backend Security Implementation

This module provides security endpoints including:
- Rate limiting status and management
- Security monitoring and metrics
- DDoS protection status
- Security configuration management
"""

from flask import Blueprint, request, jsonify, g
import time
import psutil
import threading
from typing import Dict, Any, List
import logging

from ..security.middleware import security_middleware, require_auth, require_role
from ...config.security import security_config

# Configure logging
logger = logging.getLogger(__name__)

# Create security blueprint
security_bp = Blueprint('security', __name__, url_prefix='/api/v1/security')

# Security metrics storage
security_metrics = {
    'requests_total': 0,
    'requests_blocked': 0,
    'rate_limit_hits': 0,
    'auth_failures': 0,
    'ddos_attacks': 0,
    'start_time': time.time()
}

class SecurityMonitor:
    """Security monitoring and metrics collection"""
    
    def __init__(self):
        self.metrics = security_metrics
        self.lock = threading.Lock()
    
    def increment_metric(self, metric_name: str, value: int = 1):
        """Thread-safe metric increment"""
        with self.lock:
            self.metrics[metric_name] = self.metrics.get(metric_name, 0) + value
    
    def get_metrics(self) -> Dict[str, Any]:
        """Get current security metrics"""
        with self.lock:
            uptime = time.time() - self.metrics['start_time']
            
            return {
                'uptime_seconds': uptime,
                'requests_total': self.metrics['requests_total'],
                'requests_blocked': self.metrics['requests_blocked'],
                'rate_limit_hits': self.metrics['rate_limit_hits'],
                'auth_failures': self.metrics['auth_failures'],
                'ddos_attacks': self.metrics['ddos_attacks'],
                'block_rate': (self.metrics['requests_blocked'] / max(self.metrics['requests_total'], 1)) * 100,
                'rate_limit_rate': (self.metrics['rate_limit_hits'] / max(self.metrics['requests_total'], 1)) * 100
            }
    
    def get_system_metrics(self) -> Dict[str, Any]:
        """Get system resource metrics"""
        try:
            cpu_percent = psutil.cpu_percent(interval=1)
            memory = psutil.virtual_memory()
            disk = psutil.disk_usage('/')
            
            return {
                'cpu_percent': cpu_percent,
                'memory_percent': memory.percent,
                'memory_available_gb': memory.available / (1024**3),
                'disk_percent': disk.percent,
                'disk_free_gb': disk.free / (1024**3)
            }
        except Exception as e:
            logger.error(f"System metrics error: {e}")
            return {}
    
    def get_rate_limit_status(self) -> Dict[str, Any]:
        """Get rate limiting status"""
        if not security_middleware.rate_limiter:
            return {'enabled': False, 'message': 'Rate limiting not available'}
        
        config = security_config.get_rate_limit_config()
        return {
            'enabled': config['enabled'],
            'window_seconds': config['window'],
            'max_requests': config['max_requests'],
            'burst_allowance': config['burst'],
            'current_hits': self.metrics['rate_limit_hits']
        }

# Initialize security monitor
security_monitor = SecurityMonitor()

@security_bp.route('/metrics', methods=['GET'])
@require_auth
@require_role('admin')
def get_security_metrics():
    """Get security metrics (admin only)"""
    try:
        metrics = security_monitor.get_metrics()
        system_metrics = security_monitor.get_system_metrics()
        
        return jsonify({
            'security_metrics': metrics,
            'system_metrics': system_metrics,
            'timestamp': time.time()
        }), 200
        
    except Exception as e:
        logger.error(f"Metrics retrieval error: {e}")
        return jsonify({'error': 'Internal server error'}), 500

@security_bp.route('/rate-limit/status', methods=['GET'])
@require_auth
def get_rate_limit_status():
    """Get rate limiting status"""
    try:
        status = security_monitor.get_rate_limit_status()
        return jsonify(status), 200
        
    except Exception as e:
        logger.error(f"Rate limit status error: {e}")
        return jsonify({'error': 'Internal server error'}), 500

@security_bp.route('/rate-limit/config', methods=['GET', 'PUT'])
@require_auth
@require_role('admin')
def manage_rate_limit_config():
    """Get or update rate limiting configuration (admin only)"""
    try:
        if request.method == 'GET':
            config = security_config.get_rate_limit_config()
            return jsonify(config), 200
        
        elif request.method == 'PUT':
            data = request.get_json()
            if not data:
                return jsonify({'error': 'Invalid request data'}), 400
            
            # Update configuration (in production, this would persist to database)
            if 'enabled' in data:
                security_config.RATE_LIMIT_ENABLED = bool(data['enabled'])
            if 'window' in data:
                security_config.RATE_LIMIT_WINDOW = int(data['window'])
            if 'max_requests' in data:
                security_config.RATE_LIMIT_MAX_REQUESTS = int(data['max_requests'])
            if 'burst' in data:
                security_config.RATE_LIMIT_BURST = int(data['burst'])
            
            logger.info(f"Rate limit configuration updated: {data}")
            return jsonify({'message': 'Configuration updated successfully'}), 200
        
    except Exception as e:
        logger.error(f"Rate limit config error: {e}")
        return jsonify({'error': 'Internal server error'}), 500

@security_bp.route('/ddos/status', methods=['GET'])
@require_auth
@require_role('admin')
def get_ddos_status():
    """Get DDoS protection status (admin only)"""
    try:
        return jsonify({
            'enabled': security_config.DDOS_PROTECTION_ENABLED,
            'max_connections_per_ip': security_config.DDOS_MAX_CONNECTIONS_PER_IP,
            'block_duration_seconds': security_config.DDOS_BLOCK_DURATION,
            'attacks_detected': security_metrics['ddos_attacks']
        }), 200
        
    except Exception as e:
        logger.error(f"DDoS status error: {e}")
        return jsonify({'error': 'Internal server error'}), 500

@security_bp.route('/headers', methods=['GET'])
@require_auth
def get_security_headers():
    """Get current security headers configuration"""
    try:
        headers = security_config.get_security_headers()
        return jsonify({
            'security_headers': headers,
            'cors_enabled': security_config.CORS_ENABLED,
            'cors_origins': security_config.CORS_ORIGINS
        }), 200
        
    except Exception as e:
        logger.error(f"Security headers error: {e}")
        return jsonify({'error': 'Internal server error'}), 500

@security_bp.route('/validation', methods=['POST'])
@require_auth
def validate_request():
    """Validate request security parameters"""
    try:
        validation_results = {
            'timestamp': time.time(),
            'checks': {}
        }
        
        # Check request size
        content_length = request.content_length or 0
        validation_results['checks']['request_size'] = {
            'valid': content_length <= security_config.MAX_REQUEST_SIZE,
            'size_bytes': content_length,
            'max_allowed': security_config.MAX_REQUEST_SIZE
        }
        
        # Check content type
        content_type = request.headers.get('Content-Type', '')
        validation_results['checks']['content_type'] = {
            'valid': 'application/json' in content_type or 'multipart/form-data' in content_type,
            'content_type': content_type
        }
        
        # Check user agent
        user_agent = request.headers.get('User-Agent', '')
        suspicious_patterns = ['bot', 'crawler', 'scraper', 'spider']
        is_suspicious = any(pattern in user_agent.lower() for pattern in suspicious_patterns)
        validation_results['checks']['user_agent'] = {
            'valid': not is_suspicious,
            'user_agent': user_agent,
            'suspicious': is_suspicious
        }
        
        # Check authentication
        validation_results['checks']['authentication'] = {
            'valid': hasattr(g, 'user_id'),
            'user_id': getattr(g, 'user_id', None),
            'user_role': getattr(g, 'user_role', None)
        }
        
        # Overall validation
        all_valid = all(check['valid'] for check in validation_results['checks'].values())
        validation_results['overall_valid'] = all_valid
        
        return jsonify(validation_results), 200
        
    except Exception as e:
        logger.error(f"Request validation error: {e}")
        return jsonify({'error': 'Internal server error'}), 500

@security_bp.route('/health', methods=['GET'])
def health_check():
    """Security system health check"""
    try:
        health_status = {
            'status': 'healthy',
            'timestamp': time.time(),
            'components': {}
        }
        
        # Check rate limiting
        if security_middleware.rate_limiter:
            health_status['components']['rate_limiting'] = 'healthy'
        else:
            health_status['components']['rate_limiting'] = 'unavailable'
        
        # Check authentication
        if security_middleware.jwt_auth:
            health_status['components']['authentication'] = 'healthy'
        else:
            health_status['components']['authentication'] = 'unavailable'
        
        # Check configuration
        if security_config.validate_config():
            health_status['components']['configuration'] = 'healthy'
        else:
            health_status['components']['configuration'] = 'invalid'
            health_status['status'] = 'degraded'
        
        # Check system resources
        system_metrics = security_monitor.get_system_metrics()
        if system_metrics:
            cpu_ok = system_metrics.get('cpu_percent', 0) < 90
            memory_ok = system_metrics.get('memory_percent', 0) < 90
            
            health_status['components']['system_resources'] = 'healthy' if (cpu_ok and memory_ok) else 'degraded'
            
            if not (cpu_ok and memory_ok):
                health_status['status'] = 'degraded'
        else:
            health_status['components']['system_resources'] = 'unavailable'
        
        return jsonify(health_status), 200
        
    except Exception as e:
        logger.error(f"Health check error: {e}")
        return jsonify({
            'status': 'unhealthy',
            'error': str(e),
            'timestamp': time.time()
        }), 500

@security_bp.route('/logs', methods=['GET'])
@require_auth
@require_role('admin')
def get_security_logs():
    """Get recent security logs (admin only)"""
    try:
        # In production, this would query actual log files or database
        # For demo purposes, return recent metrics
        logs = [
            {
                'timestamp': time.time() - 300,  # 5 minutes ago
                'level': 'INFO',
                'message': f"Security metrics: {security_monitor.get_metrics()}"
            },
            {
                'timestamp': time.time() - 600,  # 10 minutes ago
                'level': 'INFO',
                'message': f"System metrics: {security_monitor.get_system_metrics()}"
            }
        ]
        
        return jsonify({
            'logs': logs,
            'total_logs': len(logs)
        }), 200
        
    except Exception as e:
        logger.error(f"Security logs error: {e}")
        return jsonify({'error': 'Internal server error'}), 500 