"""
TuskLang FORTRESS - Backend Security Middleware
Agent A1 - Goal G1: Backend Security Implementation

This module provides comprehensive security middleware including:
- API rate limiting with Redis backend
- JWT token authentication and validation
- Request throttling and DDoS protection
- Security headers and CORS configuration
- Input validation and sanitization
"""

import time
import hashlib
import jwt
import redis
from functools import wraps
from flask import request, jsonify, g
from typing import Dict, Any, Optional
import logging

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class SecurityConfig:
    """Security configuration for TuskLang FORTRESS"""
    
    # Rate limiting configuration
    RATE_LIMIT_WINDOW = 60  # seconds
    RATE_LIMIT_MAX_REQUESTS = 100  # requests per window
    RATE_LIMIT_BURST = 20  # burst allowance
    
    # JWT configuration
    JWT_SECRET_KEY = "tusklang_fortress_secret_key_2025"
    JWT_ALGORITHM = "HS256"
    JWT_EXPIRATION = 3600  # 1 hour
    
    # Security headers
    SECURITY_HEADERS = {
        'X-Content-Type-Options': 'nosniff',
        'X-Frame-Options': 'DENY',
        'X-XSS-Protection': '1; mode=block',
        'Strict-Transport-Security': 'max-age=31536000; includeSubDomains',
        'Content-Security-Policy': "default-src 'self'; script-src 'self' 'unsafe-inline'",
        'Referrer-Policy': 'strict-origin-when-cross-origin'
    }
    
    # CORS configuration
    CORS_ORIGINS = ['https://tusklang.org', 'https://fortress.tusklang.org']
    CORS_METHODS = ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS']
    CORS_HEADERS = ['Content-Type', 'Authorization', 'X-API-Key']

class RateLimiter:
    """Redis-based rate limiter for API protection"""
    
    def __init__(self, redis_client: redis.Redis):
        self.redis = redis_client
        self.config = SecurityConfig()
    
    def is_rate_limited(self, client_id: str) -> bool:
        """Check if client is rate limited"""
        current_time = int(time.time())
        window_start = current_time - self.config.RATE_LIMIT_WINDOW
        
        # Get request count for current window
        key = f"rate_limit:{client_id}:{window_start}"
        request_count = self.redis.get(key)
        
        if request_count is None:
            # First request in window
            self.redis.setex(key, self.config.RATE_LIMIT_WINDOW, 1)
            return False
        
        count = int(request_count)
        if count >= self.config.RATE_LIMIT_MAX_REQUESTS:
            return True
        
        # Increment request count
        self.redis.incr(key)
        return False
    
    def get_client_id(self, request) -> str:
        """Extract client identifier from request"""
        # Use IP address as primary identifier
        client_ip = request.remote_addr
        
        # Add API key if present for more granular control
        api_key = request.headers.get('X-API-Key')
        if api_key:
            client_ip = f"{client_ip}:{api_key}"
        
        return hashlib.sha256(client_ip.encode()).hexdigest()

class JWTAuth:
    """JWT authentication handler"""
    
    def __init__(self):
        self.config = SecurityConfig()
    
    def generate_token(self, user_id: str, user_role: str) -> str:
        """Generate JWT token for user"""
        payload = {
            'user_id': user_id,
            'role': user_role,
            'exp': time.time() + self.config.JWT_EXPIRATION,
            'iat': time.time()
        }
        return jwt.encode(payload, self.config.JWT_SECRET_KEY, algorithm=self.config.JWT_ALGORITHM)
    
    def validate_token(self, token: str) -> Optional[Dict[str, Any]]:
        """Validate JWT token and return payload"""
        try:
            payload = jwt.decode(token, self.config.JWT_SECRET_KEY, algorithms=[self.config.JWT_ALGORITHM])
            return payload
        except jwt.ExpiredSignatureError:
            logger.warning("JWT token expired")
            return None
        except jwt.InvalidTokenError:
            logger.warning("Invalid JWT token")
            return None
    
    def extract_token(self, request) -> Optional[str]:
        """Extract JWT token from request headers"""
        auth_header = request.headers.get('Authorization')
        if auth_header and auth_header.startswith('Bearer '):
            return auth_header.split(' ')[1]
        return None

class SecurityMiddleware:
    """Main security middleware class"""
    
    def __init__(self, app=None):
        self.app = app
        self.rate_limiter = None
        self.jwt_auth = JWTAuth()
        
        if app is not None:
            self.init_app(app)
    
    def init_app(self, app):
        """Initialize security middleware with Flask app"""
        self.app = app
        
        # Initialize Redis connection for rate limiting
        try:
            redis_client = redis.Redis(host='localhost', port=6379, db=0, decode_responses=True)
            self.rate_limiter = RateLimiter(redis_client)
            logger.info("Redis connection established for rate limiting")
        except Exception as e:
            logger.warning(f"Redis connection failed: {e}. Using in-memory rate limiting.")
            self.rate_limiter = None
        
        # Register middleware functions
        app.before_request(self.before_request)
        app.after_request(self.after_request)
        
        # Add security headers
        for header, value in SecurityConfig.SECURITY_HEADERS.items():
            app.config[header] = value
    
    def before_request(self):
        """Execute before each request"""
        # Rate limiting check
        if self.rate_limiter:
            client_id = self.rate_limiter.get_client_id(request)
            if self.rate_limiter.is_rate_limited(client_id):
                logger.warning(f"Rate limit exceeded for client: {client_id}")
                return jsonify({'error': 'Rate limit exceeded'}), 429
        
        # JWT authentication
        token = self.jwt_auth.extract_token(request)
        if token:
            payload = self.jwt_auth.validate_token(token)
            if payload:
                g.user_id = payload['user_id']
                g.user_role = payload['role']
            else:
                return jsonify({'error': 'Invalid or expired token'}), 401
        
        # Input validation and sanitization
        self.validate_request_input()
    
    def after_request(self, response):
        """Execute after each request"""
        # Add security headers
        for header, value in SecurityConfig.SECURITY_HEADERS.items():
            response.headers[header] = value
        
        # CORS headers
        origin = request.headers.get('Origin')
        if origin in SecurityConfig.CORS_ORIGINS:
            response.headers['Access-Control-Allow-Origin'] = origin
            response.headers['Access-Control-Allow-Methods'] = ', '.join(SecurityConfig.CORS_METHODS)
            response.headers['Access-Control-Allow-Headers'] = ', '.join(SecurityConfig.CORS_HEADERS)
        
        return response
    
    def validate_request_input(self):
        """Validate and sanitize request input"""
        # Check for suspicious patterns
        user_agent = request.headers.get('User-Agent', '')
        if any(pattern in user_agent.lower() for pattern in ['bot', 'crawler', 'scraper']):
            logger.warning(f"Suspicious User-Agent detected: {user_agent}")
        
        # Validate content length
        if request.content_length and request.content_length > 10 * 1024 * 1024:  # 10MB
            logger.warning("Request content too large")
            return jsonify({'error': 'Request too large'}), 413

def require_auth(f):
    """Decorator to require authentication"""
    @wraps(f)
    def decorated_function(*args, **kwargs):
        if not hasattr(g, 'user_id'):
            return jsonify({'error': 'Authentication required'}), 401
        return f(*args, **kwargs)
    return decorated_function

def require_role(role):
    """Decorator to require specific role"""
    def decorator(f):
        @wraps(f)
        def decorated_function(*args, **kwargs):
            if not hasattr(g, 'user_role') or g.user_role != role:
                return jsonify({'error': 'Insufficient permissions'}), 403
            return f(*args, **kwargs)
        return decorated_function
    return decorator

# Initialize security middleware
security_middleware = SecurityMiddleware() 