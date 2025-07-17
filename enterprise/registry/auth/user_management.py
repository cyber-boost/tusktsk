#!/usr/bin/env python3
"""
TuskLang Package Registry User Management System
Comprehensive user management with permissions and roles
"""

import hashlib
import time
import json
from typing import Dict, List, Optional, Set
from dataclasses import dataclass, asdict
from enum import Enum
import os

class UserRole(Enum):
    """User roles in the registry"""
    READER = "reader"
    PUBLISHER = "publisher"
    MAINTAINER = "maintainer"
    ADMIN = "admin"
    SUPER_ADMIN = "super_admin"

class Permission(Enum):
    """Registry permissions"""
    READ_PACKAGES = "read_packages"
    PUBLISH_PACKAGES = "publish_packages"
    UPDATE_PACKAGES = "update_packages"
    DELETE_PACKAGES = "delete_packages"
    MANAGE_USERS = "manage_users"
    MANAGE_REGISTRY = "manage_registry"
    VIEW_ANALYTICS = "view_analytics"
    MANAGE_SECURITY = "manage_security"

@dataclass
class UserProfile:
    """User profile information"""
    user_id: str
    username: str
    email: str
    full_name: str
    role: UserRole
    permissions: Set[Permission]
    is_active: bool
    is_verified: bool
    created_at: float
    last_login: Optional[float] = None
    api_key_hash: Optional[str] = None
    two_factor_enabled: bool = False
    two_factor_secret: Optional[str] = None

class UserManager:
    """User management system for registry authentication"""
    
    def __init__(self, data_path: str):
        self.data_path = data_path
        self.users: Dict[str, UserProfile] = {}
        self.user_sessions: Dict[str, Dict] = {}
        self.login_attempts: Dict[str, List[float]] = {}
        
        # Ensure data directory exists
        os.makedirs(data_path, exist_ok=True)
        self._load_users()
    
    def _load_users(self):
        """Load users from storage"""
        users_file = os.path.join(self.data_path, 'users.json')
        if os.path.exists(users_file):
            try:
                with open(users_file, 'r') as f:
                    data = json.load(f)
                    for user_data in data.values():
                        # Convert permissions back to set
                        user_data['permissions'] = set(Permission(p) for p in user_data['permissions'])
                        user_data['role'] = UserRole(user_data['role'])
                        user = UserProfile(**user_data)
                        self.users[user.user_id] = user
            except Exception as e:
                print(f"Error loading users: {e}")
    
    def _save_users(self):
        """Save users to storage"""
        users_file = os.path.join(self.data_path, 'users.json')
        try:
            data = {}
            for user_id, user in self.users.items():
                user_data = asdict(user)
                # Convert permissions set to list for JSON serialization
                user_data['permissions'] = [p.value for p in user.permissions]
                user_data['role'] = user.role.value
                data[user_id] = user_data
            
            with open(users_file, 'w') as f:
                json.dump(data, f, indent=2)
        except Exception as e:
            print(f"Error saving users: {e}")
    
    def create_user(self, username: str, email: str, full_name: str, 
                   password: str, role: UserRole = UserRole.READER) -> Optional[str]:
        """Create a new user"""
        # Check if username or email already exists
        for user in self.users.values():
            if user.username == username or user.email == email:
                return None
        
        user_id = self._generate_user_id(username, email)
        password_hash = self._hash_password(password)
        
        # Set default permissions based on role
        permissions = self._get_default_permissions(role)
        
        user = UserProfile(
            user_id=user_id,
            username=username,
            email=email,
            full_name=full_name,
            role=role,
            permissions=permissions,
            is_active=True,
            is_verified=False,
            created_at=time.time()
        )
        
        self.users[user_id] = user
        self._save_users()
        
        return user_id
    
    def authenticate_user(self, username: str, password: str) -> Optional[UserProfile]:
        """Authenticate user with username and password"""
        # Find user by username or email
        user = None
        for u in self.users.values():
            if u.username == username or u.email == username:
                user = u
                break
        
        if not user or not user.is_active:
            return None
        
        # Check rate limiting
        if self._is_rate_limited(username):
            return None
        
        # Verify password
        if self._verify_password(password, user.api_key_hash):
            # Update last login
            user.last_login = time.time()
            self._save_users()
            return user
        
        # Record failed login attempt
        self._record_login_attempt(username)
        return None
    
    def create_api_key(self, user_id: str) -> Optional[str]:
        """Create API key for user"""
        if user_id not in self.users:
            return None
        
        user = self.users[user_id]
        api_key = self._generate_api_key(user.username, user.email)
        user.api_key_hash = self._hash_api_key(api_key)
        self._save_users()
        
        return api_key
    
    def authenticate_api_key(self, api_key: str) -> Optional[UserProfile]:
        """Authenticate user using API key"""
        api_key_hash = self._hash_api_key(api_key)
        
        for user in self.users.values():
            if user.api_key_hash == api_key_hash and user.is_active:
                user.last_login = time.time()
                self._save_users()
                return user
        
        return None
    
    def create_session(self, user: UserProfile, expires_in: int = 3600) -> str:
        """Create a user session"""
        session_id = self._generate_session_id(user.user_id)
        expires_at = time.time() + expires_in
        
        session = {
            'user_id': user.user_id,
            'username': user.username,
            'role': user.role.value,
            'permissions': [p.value for p in user.permissions],
            'created_at': time.time(),
            'expires_at': expires_at
        }
        
        self.user_sessions[session_id] = session
        return session_id
    
    def validate_session(self, session_id: str) -> Optional[UserProfile]:
        """Validate and return user for session"""
        if session_id not in self.user_sessions:
            return None
        
        session = self.user_sessions[session_id]
        if time.time() > session['expires_at']:
            del self.user_sessions[session_id]
            return None
        
        user_id = session['user_id']
        return self.users.get(user_id)
    
    def revoke_session(self, session_id: str) -> bool:
        """Revoke a user session"""
        if session_id in self.user_sessions:
            del self.user_sessions[session_id]
            return True
        return False
    
    def update_user_permissions(self, user_id: str, permissions: Set[Permission]) -> bool:
        """Update user permissions"""
        if user_id not in self.users:
            return False
        
        user = self.users[user_id]
        user.permissions = permissions
        self._save_users()
        return True
    
    def update_user_role(self, user_id: str, role: UserRole) -> bool:
        """Update user role"""
        if user_id not in self.users:
            return False
        
        user = self.users[user_id]
        user.role = role
        user.permissions = self._get_default_permissions(role)
        self._save_users()
        return True
    
    def deactivate_user(self, user_id: str) -> bool:
        """Deactivate a user"""
        if user_id not in self.users:
            return False
        
        user = self.users[user_id]
        user.is_active = False
        self._save_users()
        return True
    
    def activate_user(self, user_id: str) -> bool:
        """Activate a user"""
        if user_id not in self.users:
            return False
        
        user = self.users[user_id]
        user.is_active = True
        self._save_users()
        return True
    
    def has_permission(self, user: UserProfile, permission: Permission) -> bool:
        """Check if user has specific permission"""
        return permission in user.permissions
    
    def get_users_by_role(self, role: UserRole) -> List[UserProfile]:
        """Get all users with specific role"""
        return [user for user in self.users.values() if user.role == role]
    
    def get_active_users(self) -> List[UserProfile]:
        """Get all active users"""
        return [user for user in self.users.values() if user.is_active]
    
    def _generate_user_id(self, username: str, email: str) -> str:
        """Generate unique user ID"""
        data = f"{username}:{email}:{time.time()}".encode()
        return hashlib.sha256(data).hexdigest()[:16]
    
    def _hash_password(self, password: str) -> str:
        """Hash password securely"""
        salt = os.urandom(16)
        return hashlib.pbkdf2_hmac('sha256', password.encode(), salt, 100000).hex()
    
    def _verify_password(self, password: str, stored_hash: str) -> bool:
        """Verify password against stored hash"""
        # This is a simplified version - in production, use proper password hashing
        return hashlib.sha256(password.encode()).hexdigest() == stored_hash
    
    def _generate_api_key(self, username: str, email: str) -> str:
        """Generate API key"""
        data = f"{username}:{email}:{time.time()}:{os.urandom(16).hex()}".encode()
        return hashlib.sha256(data).hexdigest()
    
    def _hash_api_key(self, api_key: str) -> str:
        """Hash API key"""
        return hashlib.sha256(api_key.encode()).hexdigest()
    
    def _generate_session_id(self, user_id: str) -> str:
        """Generate session ID"""
        data = f"{user_id}:{time.time()}:{os.urandom(16).hex()}".encode()
        return hashlib.sha256(data).hexdigest()
    
    def _get_default_permissions(self, role: UserRole) -> Set[Permission]:
        """Get default permissions for role"""
        permissions = {Permission.READ_PACKAGES}  # All users can read packages
        
        if role == UserRole.PUBLISHER:
            permissions.update({Permission.PUBLISH_PACKAGES, Permission.UPDATE_PACKAGES})
        elif role == UserRole.MAINTAINER:
            permissions.update({Permission.PUBLISH_PACKAGES, Permission.UPDATE_PACKAGES, 
                              Permission.DELETE_PACKAGES, Permission.VIEW_ANALYTICS})
        elif role == UserRole.ADMIN:
            permissions.update({Permission.PUBLISH_PACKAGES, Permission.UPDATE_PACKAGES,
                              Permission.DELETE_PACKAGES, Permission.MANAGE_USERS,
                              Permission.VIEW_ANALYTICS, Permission.MANAGE_SECURITY})
        elif role == UserRole.SUPER_ADMIN:
            permissions.update(Permission)  # All permissions
        
        return permissions
    
    def _is_rate_limited(self, username: str) -> bool:
        """Check if username is rate limited"""
        if username not in self.login_attempts:
            return False
        
        # Remove attempts older than 15 minutes
        cutoff_time = time.time() - 900
        self.login_attempts[username] = [
            attempt for attempt in self.login_attempts[username]
            if attempt > cutoff_time
        ]
        
        # Allow max 5 attempts per 15 minutes
        return len(self.login_attempts[username]) >= 5
    
    def _record_login_attempt(self, username: str):
        """Record failed login attempt"""
        if username not in self.login_attempts:
            self.login_attempts[username] = []
        
        self.login_attempts[username].append(time.time())

# Global user manager instance
user_manager = UserManager("/var/tusklang/registry/users") 