"""
TuskLang FORTRESS - Security Configuration
Agent A1 - Goal G1: Backend Security Implementation

This module contains security configuration settings for the TuskLang FORTRESS system.
"""

import os
from typing import Dict, List, Any

class SecuritySettings:
    """Security configuration settings"""
    
    # API Security
    API_VERSION = "v1"
    API_PREFIX = "/api/v1"
    
    # Rate Limiting
    RATE_LIMIT_ENABLED = True
    RATE_LIMIT_WINDOW = 60  # seconds
    RATE_LIMIT_MAX_REQUESTS = 100
    RATE_LIMIT_BURST = 20
    
    # Authentication
    AUTH_ENABLED = True
    JWT_SECRET_KEY = os.getenv("JWT_SECRET_KEY", "tusklang_fortress_secret_key_2025")
    JWT_ALGORITHM = "HS256"
    JWT_EXPIRATION = 3600  # 1 hour
    JWT_REFRESH_EXPIRATION = 86400  # 24 hours
    
    # API Keys
    API_KEY_ENABLED = True
    API_KEY_HEADER = "X-API-Key"
    API_KEY_LENGTH = 32
    
    # Request Signing
    REQUEST_SIGNING_ENABLED = True
    SIGNATURE_HEADER = "X-Request-Signature"
    SIGNATURE_ALGORITHM = "sha256"
    
    # Security Headers
    SECURITY_HEADERS = {
        'X-Content-Type-Options': 'nosniff',
        'X-Frame-Options': 'DENY',
        'X-XSS-Protection': '1; mode=block',
        'Strict-Transport-Security': 'max-age=31536000; includeSubDomains',
        'Content-Security-Policy': "default-src 'self'; script-src 'self' 'unsafe-inline'",
        'Referrer-Policy': 'strict-origin-when-cross-origin',
        'Permissions-Policy': 'geolocation=(), microphone=(), camera=()'
    }
    
    # CORS Configuration
    CORS_ENABLED = True
    CORS_ORIGINS = [
        'https://tusklang.org',
        'https://fortress.tusklang.org',
        'https://api.tusklang.org'
    ]
    CORS_METHODS = ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS']
    CORS_HEADERS = [
        'Content-Type',
        'Authorization',
        'X-API-Key',
        'X-Request-Signature'
    ]
    CORS_EXPOSE_HEADERS = ['X-Rate-Limit', 'X-Rate-Limit-Remaining']
    
    # Input Validation
    MAX_REQUEST_SIZE = 10 * 1024 * 1024  # 10MB
    MAX_JSON_DEPTH = 10
    ALLOWED_FILE_TYPES = ['.json', '.txt', '.md', '.tsk']
    MAX_FILE_SIZE = 5 * 1024 * 1024  # 5MB
    
    # DDoS Protection
    DDOS_PROTECTION_ENABLED = True
    DDOS_MAX_CONNECTIONS_PER_IP = 100
    DDOS_BLOCK_DURATION = 300  # 5 minutes
    
    # Logging
    SECURITY_LOGGING_ENABLED = True
    LOG_LEVEL = "INFO"
    LOG_FORMAT = "%(asctime)s - %(name)s - %(levelname)s - %(message)s"
    
    # Monitoring
    MONITORING_ENABLED = True
    METRICS_ENDPOINT = "/metrics"
    HEALTH_CHECK_ENDPOINT = "/health"
    
    # Database Security
    DB_CONNECTION_POOL_SIZE = 10
    DB_CONNECTION_TIMEOUT = 30
    DB_QUERY_TIMEOUT = 60
    
    # Cache Security
    CACHE_ENABLED = True
    CACHE_TTL = 300  # 5 minutes
    CACHE_MAX_SIZE = 1000
    
    @classmethod
    def get_rate_limit_config(cls) -> Dict[str, Any]:
        """Get rate limiting configuration"""
        return {
            'enabled': cls.RATE_LIMIT_ENABLED,
            'window': cls.RATE_LIMIT_WINDOW,
            'max_requests': cls.RATE_LIMIT_MAX_REQUESTS,
            'burst': cls.RATE_LIMIT_BURST
        }
    
    @classmethod
    def get_auth_config(cls) -> Dict[str, Any]:
        """Get authentication configuration"""
        return {
            'enabled': cls.AUTH_ENABLED,
            'jwt_secret': cls.JWT_SECRET_KEY,
            'jwt_algorithm': cls.JWT_ALGORITHM,
            'jwt_expiration': cls.JWT_EXPIRATION,
            'jwt_refresh_expiration': cls.JWT_REFRESH_EXPIRATION
        }
    
    @classmethod
    def get_cors_config(cls) -> Dict[str, Any]:
        """Get CORS configuration"""
        return {
            'enabled': cls.CORS_ENABLED,
            'origins': cls.CORS_ORIGINS,
            'methods': cls.CORS_METHODS,
            'headers': cls.CORS_HEADERS,
            'expose_headers': cls.CORS_EXPOSE_HEADERS
        }
    
    @classmethod
    def get_security_headers(cls) -> Dict[str, str]:
        """Get security headers configuration"""
        return cls.SECURITY_HEADERS.copy()
    
    @classmethod
    def validate_config(cls) -> bool:
        """Validate security configuration"""
        try:
            # Validate JWT secret key
            if not cls.JWT_SECRET_KEY or len(cls.JWT_SECRET_KEY) < 16:
                raise ValueError("JWT secret key must be at least 16 characters")
            
            # Validate rate limiting settings
            if cls.RATE_LIMIT_WINDOW <= 0 or cls.RATE_LIMIT_MAX_REQUESTS <= 0:
                raise ValueError("Invalid rate limiting configuration")
            
            # Validate CORS origins
            if not cls.CORS_ORIGINS:
                raise ValueError("CORS origins must be specified")
            
            return True
        except Exception as e:
            print(f"Security configuration validation failed: {e}")
            return False

# Environment-specific configurations
class DevelopmentSecuritySettings(SecuritySettings):
    """Development environment security settings"""
    RATE_LIMIT_MAX_REQUESTS = 1000
    CORS_ORIGINS = ['http://localhost:3000', 'http://localhost:8080']
    LOG_LEVEL = "DEBUG"

class ProductionSecuritySettings(SecuritySettings):
    """Production environment security settings"""
    RATE_LIMIT_MAX_REQUESTS = 50
    JWT_EXPIRATION = 1800  # 30 minutes
    LOG_LEVEL = "WARNING"

class TestingSecuritySettings(SecuritySettings):
    """Testing environment security settings"""
    RATE_LIMIT_ENABLED = False
    AUTH_ENABLED = False
    CORS_ENABLED = False
    LOG_LEVEL = "ERROR"

# Configuration factory
def get_security_config(environment: str = "development") -> SecuritySettings:
    """Get security configuration for specified environment"""
    configs = {
        "development": DevelopmentSecuritySettings,
        "production": ProductionSecuritySettings,
        "testing": TestingSecuritySettings
    }
    
    config_class = configs.get(environment, SecuritySettings)
    return config_class()

# Initialize default configuration
security_config = get_security_config(os.getenv("ENVIRONMENT", "development")) 