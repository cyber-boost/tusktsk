"""
TuskLang FORTRESS - Security Tests
Agent A1 - Goal G1: Backend Security Implementation

This module contains comprehensive tests for the security system.
"""

import unittest
import time
import jwt
import hashlib
from unittest.mock import Mock, patch
import sys
import os

# Add parent directory to path for imports
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from api.security.middleware import SecurityMiddleware, JWTAuth, RateLimiter, SecurityConfig
from config.security import SecuritySettings, get_security_config
from api.v1.auth import AuthService
from api.v1.security import SecurityMonitor

class TestSecurityConfig(unittest.TestCase):
    """Test security configuration"""
    
    def setUp(self):
        self.config = SecurityConfig()
    
    def test_rate_limit_config(self):
        """Test rate limiting configuration"""
        self.assertTrue(self.config.RATE_LIMIT_ENABLED)
        self.assertGreater(self.config.RATE_LIMIT_WINDOW, 0)
        self.assertGreater(self.config.RATE_LIMIT_MAX_REQUESTS, 0)
    
    def test_jwt_config(self):
        """Test JWT configuration"""
        self.assertIsNotNone(self.config.JWT_SECRET_KEY)
        self.assertEqual(self.config.JWT_ALGORITHM, "HS256")
        self.assertGreater(self.config.JWT_EXPIRATION, 0)
    
    def test_security_headers(self):
        """Test security headers configuration"""
        headers = self.config.SECURITY_HEADERS
        self.assertIn('X-Content-Type-Options', headers)
        self.assertIn('X-Frame-Options', headers)
        self.assertIn('Strict-Transport-Security', headers)

class TestJWTAuth(unittest.TestCase):
    """Test JWT authentication"""
    
    def setUp(self):
        self.jwt_auth = JWTAuth()
        self.test_user_id = "test_user_123"
        self.test_role = "user"
    
    def test_generate_token(self):
        """Test JWT token generation"""
        token = self.jwt_auth.generate_token(self.test_user_id, self.test_role)
        self.assertIsInstance(token, str)
        self.assertGreater(len(token), 0)
    
    def test_validate_token(self):
        """Test JWT token validation"""
        token = self.jwt_auth.generate_token(self.test_user_id, self.test_role)
        payload = self.jwt_auth.validate_token(token)
        
        self.assertIsNotNone(payload)
        self.assertEqual(payload['user_id'], self.test_user_id)
        self.assertEqual(payload['role'], self.test_role)
    
    def test_validate_expired_token(self):
        """Test expired token validation"""
        # Create token with short expiration
        payload = {
            'user_id': self.test_user_id,
            'role': self.test_role,
            'exp': time.time() - 1,  # Expired 1 second ago
            'iat': time.time() - 2
        }
        token = jwt.encode(payload, SecurityConfig.JWT_SECRET_KEY, algorithm=SecurityConfig.JWT_ALGORITHM)
        
        result = self.jwt_auth.validate_token(token)
        self.assertIsNone(result)
    
    def test_extract_token(self):
        """Test token extraction from headers"""
        mock_request = Mock()
        mock_request.headers = {'Authorization': 'Bearer test_token_123'}
        
        token = self.jwt_auth.extract_token(mock_request)
        self.assertEqual(token, 'test_token_123')
    
    def test_extract_token_no_header(self):
        """Test token extraction with no authorization header"""
        mock_request = Mock()
        mock_request.headers = {}
        
        token = self.jwt_auth.extract_token(mock_request)
        self.assertIsNone(token)

class TestRateLimiter(unittest.TestCase):
    """Test rate limiting functionality"""
    
    def setUp(self):
        self.mock_redis = Mock()
        self.rate_limiter = RateLimiter(self.mock_redis)
        self.test_client_id = "test_client_123"
    
    def test_get_client_id(self):
        """Test client ID generation"""
        mock_request = Mock()
        mock_request.remote_addr = "192.168.1.1"
        mock_request.headers = {}
        
        client_id = self.rate_limiter.get_client_id(mock_request)
        self.assertIsInstance(client_id, str)
        self.assertEqual(len(client_id), 64)  # SHA256 hash length
    
    def test_get_client_id_with_api_key(self):
        """Test client ID generation with API key"""
        mock_request = Mock()
        mock_request.remote_addr = "192.168.1.1"
        mock_request.headers = {'X-API-Key': 'test_api_key'}
        
        client_id = self.rate_limiter.get_client_id(mock_request)
        self.assertIsInstance(client_id, str)
        self.assertEqual(len(client_id), 64)
    
    @patch('time.time')
    def test_is_rate_limited_first_request(self, mock_time):
        """Test rate limiting for first request"""
        mock_time.return_value = 1000
        self.mock_redis.get.return_value = None
        
        result = self.rate_limiter.is_rate_limited(self.test_client_id)
        self.assertFalse(result)
        self.mock_redis.setex.assert_called_once()
    
    @patch('time.time')
    def test_is_rate_limited_under_limit(self, mock_time):
        """Test rate limiting under the limit"""
        mock_time.return_value = 1000
        self.mock_redis.get.return_value = "50"  # Under limit
        
        result = self.rate_limiter.is_rate_limited(self.test_client_id)
        self.assertFalse(result)
        self.mock_redis.incr.assert_called_once()
    
    @patch('time.time')
    def test_is_rate_limited_over_limit(self, mock_time):
        """Test rate limiting over the limit"""
        mock_time.return_value = 1000
        self.mock_redis.get.return_value = "150"  # Over limit
        
        result = self.rate_limiter.is_rate_limited(self.test_client_id)
        self.assertTrue(result)

class TestAuthService(unittest.TestCase):
    """Test authentication service"""
    
    def setUp(self):
        self.auth_service = AuthService()
        self.test_username = "testuser"
        self.test_email = "test@example.com"
        self.test_password = "testpassword123"
    
    def test_create_user(self):
        """Test user creation"""
        user = self.auth_service.create_user(
            self.test_username, 
            self.test_email, 
            self.test_password
        )
        
        self.assertIsNotNone(user)
        self.assertEqual(user['username'], self.test_username)
        self.assertEqual(user['email'], self.test_email)
        self.assertIn('password_hash', user)
        self.assertEqual(user['role'], 'user')
    
    def test_create_user_duplicate_username(self):
        """Test user creation with duplicate username"""
        # Create first user
        self.auth_service.create_user(
            self.test_username, 
            self.test_email, 
            self.test_password
        )
        
        # Try to create duplicate
        with self.assertRaises(ValueError):
            self.auth_service.create_user(
                self.test_username, 
                "another@example.com", 
                "anotherpassword"
            )
    
    def test_authenticate_user(self):
        """Test user authentication"""
        # Create user first
        self.auth_service.create_user(
            self.test_username, 
            self.test_email, 
            self.test_password
        )
        
        # Authenticate
        user = self.auth_service.authenticate_user(self.test_username, self.test_password)
        self.assertIsNotNone(user)
        self.assertEqual(user['username'], self.test_username)
    
    def test_authenticate_user_invalid_password(self):
        """Test authentication with invalid password"""
        # Create user first
        self.auth_service.create_user(
            self.test_username, 
            self.test_email, 
            self.test_password
        )
        
        # Try to authenticate with wrong password
        user = self.auth_service.authenticate_user(self.test_username, "wrongpassword")
        self.assertIsNone(user)
    
    def test_generate_tokens(self):
        """Test token generation"""
        user = self.auth_service.create_user(
            self.test_username, 
            self.test_email, 
            self.test_password
        )
        
        tokens = self.auth_service.generate_tokens(user)
        
        self.assertIn('access_token', tokens)
        self.assertIn('refresh_token', tokens)
        self.assertIn('token_type', tokens)
        self.assertIn('expires_in', tokens)
        self.assertEqual(tokens['token_type'], 'Bearer')

class TestSecurityMonitor(unittest.TestCase):
    """Test security monitoring"""
    
    def setUp(self):
        self.monitor = SecurityMonitor()
    
    def test_increment_metric(self):
        """Test metric increment"""
        initial_value = self.monitor.metrics['requests_total']
        self.monitor.increment_metric('requests_total')
        self.assertEqual(self.monitor.metrics['requests_total'], initial_value + 1)
    
    def test_get_metrics(self):
        """Test metrics retrieval"""
        metrics = self.monitor.get_metrics()
        
        self.assertIn('uptime_seconds', metrics)
        self.assertIn('requests_total', metrics)
        self.assertIn('requests_blocked', metrics)
        self.assertIn('block_rate', metrics)
        self.assertGreaterEqual(metrics['uptime_seconds'], 0)
    
    def test_get_rate_limit_status(self):
        """Test rate limit status"""
        status = self.monitor.get_rate_limit_status()
        
        self.assertIn('enabled', status)
        self.assertIn('window_seconds', status)
        self.assertIn('max_requests', status)
        self.assertIn('current_hits', status)

class TestSecuritySettings(unittest.TestCase):
    """Test security settings configuration"""
    
    def test_development_config(self):
        """Test development configuration"""
        config = get_security_config("development")
        self.assertEqual(config.RATE_LIMIT_MAX_REQUESTS, 1000)
        self.assertIn('http://localhost:3000', config.CORS_ORIGINS)
        self.assertEqual(config.LOG_LEVEL, "DEBUG")
    
    def test_production_config(self):
        """Test production configuration"""
        config = get_security_config("production")
        self.assertEqual(config.RATE_LIMIT_MAX_REQUESTS, 50)
        self.assertEqual(config.JWT_EXPIRATION, 1800)
        self.assertEqual(config.LOG_LEVEL, "WARNING")
    
    def test_testing_config(self):
        """Test testing configuration"""
        config = get_security_config("testing")
        self.assertFalse(config.RATE_LIMIT_ENABLED)
        self.assertFalse(config.AUTH_ENABLED)
        self.assertFalse(config.CORS_ENABLED)
        self.assertEqual(config.LOG_LEVEL, "ERROR")
    
    def test_config_validation(self):
        """Test configuration validation"""
        config = SecuritySettings()
        self.assertTrue(config.validate_config())

if __name__ == '__main__':
    # Run tests
    unittest.main(verbosity=2) 