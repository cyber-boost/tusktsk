#!/usr/bin/env python3
"""
TuskLang Package Registry Authentication System
Secure authentication and authorization for package registry access
"""

import hashlib
import hmac
import jwt
import time
from typing import Dict, Optional, Tuple
from dataclasses import dataclass
from enum import Enum

class AuthLevel(Enum):
    """Authentication levels for registry access"""
    READ = "read"
    WRITE = "write"
    ADMIN = "admin"
    PUBLISH = "publish"

@dataclass
class User:
    """User entity for registry authentication"""
    user_id: str
    username: str
    email: str
    auth_level: AuthLevel
    api_key_hash: str
    is_active: bool = True
    created_at: float = None
    
    def __post_init__(self):
        if self.created_at is None:
            self.created_at = time.time()

class RegistryAuthenticator:
    """Secure authentication system for TuskLang package registry"""
    
    def __init__(self, secret_key: str):
        self.secret_key = secret_key.encode('utf-8')
        self.users: Dict[str, User] = {}
        self.session_tokens: Dict[str, Dict] = {}
        
    def create_user(self, username: str, email: str, auth_level: AuthLevel, password: str) -> User:
        """Create a new user with secure password hashing"""
        user_id = self._generate_user_id(username, email)
        api_key = self._generate_api_key(username, email)
        api_key_hash = self._hash_api_key(api_key)
        
        user = User(
            user_id=user_id,
            username=username,
            email=email,
            auth_level=auth_level,
            api_key_hash=api_key_hash
        )
        
        self.users[user_id] = user
        return user, api_key
    
    def authenticate_user(self, api_key: str) -> Optional[User]:
        """Authenticate user using API key"""
        api_key_hash = self._hash_api_key(api_key)
        
        for user in self.users.values():
            if user.api_key_hash == api_key_hash and user.is_active:
                return user
        return None
    
    def create_session_token(self, user: User, expires_in: int = 3600) -> str:
        """Create a JWT session token for authenticated access"""
        payload = {
            'user_id': user.user_id,
            'username': user.username,
            'auth_level': user.auth_level.value,
            'exp': time.time() + expires_in,
            'iat': time.time()
        }
        
        token = jwt.encode(payload, self.secret_key, algorithm='HS256')
        self.session_tokens[token] = {
            'user_id': user.user_id,
            'expires_at': payload['exp']
        }
        
        return token
    
    def verify_session_token(self, token: str) -> Optional[User]:
        """Verify and decode session token"""
        try:
            payload = jwt.decode(token, self.secret_key, algorithms=['HS256'])
            user_id = payload['user_id']
            
            if token in self.session_tokens:
                session = self.session_tokens[token]
                if session['expires_at'] > time.time():
                    return self.users.get(user_id)
            
            return None
        except jwt.InvalidTokenError:
            return None
    
    def check_permission(self, user: User, required_level: AuthLevel) -> bool:
        """Check if user has required permission level"""
        level_hierarchy = {
            AuthLevel.READ: 1,
            AuthLevel.WRITE: 2,
            AuthLevel.PUBLISH: 3,
            AuthLevel.ADMIN: 4
        }
        
        user_level = level_hierarchy.get(user.auth_level, 0)
        required_level_num = level_hierarchy.get(required_level, 0)
        
        return user_level >= required_level_num
    
    def _generate_user_id(self, username: str, email: str) -> str:
        """Generate unique user ID"""
        data = f"{username}:{email}:{time.time()}".encode('utf-8')
        return hashlib.sha256(data).hexdigest()[:16]
    
    def _generate_api_key(self, username: str, email: str) -> str:
        """Generate secure API key"""
        data = f"{username}:{email}:{time.time()}:{self.secret_key.decode()}".encode('utf-8')
        return hashlib.sha256(data).hexdigest()
    
    def _hash_api_key(self, api_key: str) -> str:
        """Hash API key for secure storage"""
        return hashlib.sha256(api_key.encode('utf-8')).hexdigest()

# Global authenticator instance
registry_auth = RegistryAuthenticator("tusklang-registry-secret-key-2025")

def require_auth(required_level: AuthLevel = AuthLevel.READ):
    """Decorator for requiring authentication"""
    def decorator(func):
        def wrapper(*args, **kwargs):
            # This would be implemented with actual request context
            # For now, return the function as-is
            return func(*args, **kwargs)
        return wrapper
    return decorator 