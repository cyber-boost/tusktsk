"""
TuskLang FORTRESS - Authentication API
Agent A1 - Goal G1: Backend Security Implementation

This module provides authentication endpoints including:
- User login and logout
- JWT token generation and validation
- API key management
- Password reset functionality
"""

from flask import Blueprint, request, jsonify, g
from werkzeug.security import generate_password_hash, check_password_hash
import jwt
import time
import secrets
import hashlib
from typing import Dict, Any, Optional
import logging

from ..security.middleware import security_middleware, require_auth, require_role
from ...config.security import security_config

# Configure logging
logger = logging.getLogger(__name__)

# Create authentication blueprint
auth_bp = Blueprint('auth', __name__, url_prefix='/api/v1/auth')

# In-memory storage for demo (replace with database in production)
users_db = {}
api_keys_db = {}
refresh_tokens_db = {}

class AuthService:
    """Authentication service class"""
    
    def __init__(self):
        self.config = security_config
    
    def create_user(self, username: str, email: str, password: str, role: str = "user") -> Dict[str, Any]:
        """Create a new user"""
        if username in users_db:
            raise ValueError("Username already exists")
        
        if email in [user['email'] for user in users_db.values()]:
            raise ValueError("Email already exists")
        
        user_id = secrets.token_hex(16)
        user = {
            'id': user_id,
            'username': username,
            'email': email,
            'password_hash': generate_password_hash(password),
            'role': role,
            'created_at': time.time(),
            'last_login': None,
            'is_active': True
        }
        
        users_db[username] = user
        logger.info(f"User created: {username}")
        return user
    
    def authenticate_user(self, username: str, password: str) -> Optional[Dict[str, Any]]:
        """Authenticate user with username and password"""
        user = users_db.get(username)
        if not user or not user['is_active']:
            return None
        
        if not check_password_hash(user['password_hash'], password):
            return None
        
        # Update last login
        user['last_login'] = time.time()
        users_db[username] = user
        
        logger.info(f"User authenticated: {username}")
        return user
    
    def generate_tokens(self, user: Dict[str, Any]) -> Dict[str, str]:
        """Generate access and refresh tokens"""
        access_token = security_middleware.jwt_auth.generate_token(
            user['id'], user['role']
        )
        
        # Generate refresh token
        refresh_token = secrets.token_hex(32)
        refresh_tokens_db[refresh_token] = {
            'user_id': user['id'],
            'expires_at': time.time() + self.config.JWT_REFRESH_EXPIRATION
        }
        
        return {
            'access_token': access_token,
            'refresh_token': refresh_token,
            'token_type': 'Bearer',
            'expires_in': self.config.JWT_EXPIRATION
        }
    
    def refresh_access_token(self, refresh_token: str) -> Optional[str]:
        """Refresh access token using refresh token"""
        token_data = refresh_tokens_db.get(refresh_token)
        if not token_data or token_data['expires_at'] < time.time():
            return None
        
        # Find user by ID
        user = None
        for u in users_db.values():
            if u['id'] == token_data['user_id']:
                user = u
                break
        
        if not user:
            return None
        
        return security_middleware.jwt_auth.generate_token(
            user['id'], user['role']
        )
    
    def create_api_key(self, user_id: str, name: str) -> str:
        """Create API key for user"""
        api_key = secrets.token_hex(self.config.API_KEY_LENGTH // 2)
        key_hash = hashlib.sha256(api_key.encode()).hexdigest()
        
        api_keys_db[key_hash] = {
            'user_id': user_id,
            'name': name,
            'created_at': time.time(),
            'last_used': None,
            'is_active': True
        }
        
        logger.info(f"API key created for user: {user_id}")
        return api_key
    
    def validate_api_key(self, api_key: str) -> Optional[Dict[str, Any]]:
        """Validate API key and return user info"""
        key_hash = hashlib.sha256(api_key.encode()).hexdigest()
        key_data = api_keys_db.get(key_hash)
        
        if not key_data or not key_data['is_active']:
            return None
        
        # Update last used
        key_data['last_used'] = time.time()
        api_keys_db[key_hash] = key_data
        
        # Find user
        for user in users_db.values():
            if user['id'] == key_data['user_id']:
                return user
        
        return None

# Initialize auth service
auth_service = AuthService()

@auth_bp.route('/register', methods=['POST'])
def register():
    """Register a new user"""
    try:
        data = request.get_json()
        
        if not data:
            return jsonify({'error': 'Invalid request data'}), 400
        
        username = data.get('username')
        email = data.get('email')
        password = data.get('password')
        role = data.get('role', 'user')
        
        # Validate input
        if not all([username, email, password]):
            return jsonify({'error': 'Missing required fields'}), 400
        
        if len(password) < 8:
            return jsonify({'error': 'Password must be at least 8 characters'}), 400
        
        # Create user
        user = auth_service.create_user(username, email, password, role)
        
        # Generate tokens
        tokens = auth_service.generate_tokens(user)
        
        return jsonify({
            'message': 'User registered successfully',
            'user': {
                'id': user['id'],
                'username': user['username'],
                'email': user['email'],
                'role': user['role']
            },
            'tokens': tokens
        }), 201
        
    except ValueError as e:
        return jsonify({'error': str(e)}), 400
    except Exception as e:
        logger.error(f"Registration error: {e}")
        return jsonify({'error': 'Internal server error'}), 500

@auth_bp.route('/login', methods=['POST'])
def login():
    """Authenticate user and return tokens"""
    try:
        data = request.get_json()
        
        if not data:
            return jsonify({'error': 'Invalid request data'}), 400
        
        username = data.get('username')
        password = data.get('password')
        
        if not all([username, password]):
            return jsonify({'error': 'Missing username or password'}), 400
        
        # Authenticate user
        user = auth_service.authenticate_user(username, password)
        if not user:
            return jsonify({'error': 'Invalid credentials'}), 401
        
        # Generate tokens
        tokens = auth_service.generate_tokens(user)
        
        return jsonify({
            'message': 'Login successful',
            'user': {
                'id': user['id'],
                'username': user['username'],
                'email': user['email'],
                'role': user['role']
            },
            'tokens': tokens
        }), 200
        
    except Exception as e:
        logger.error(f"Login error: {e}")
        return jsonify({'error': 'Internal server error'}), 500

@auth_bp.route('/refresh', methods=['POST'])
def refresh_token():
    """Refresh access token"""
    try:
        data = request.get_json()
        
        if not data:
            return jsonify({'error': 'Invalid request data'}), 400
        
        refresh_token = data.get('refresh_token')
        if not refresh_token:
            return jsonify({'error': 'Refresh token required'}), 400
        
        # Refresh access token
        access_token = auth_service.refresh_access_token(refresh_token)
        if not access_token:
            return jsonify({'error': 'Invalid or expired refresh token'}), 401
        
        return jsonify({
            'access_token': access_token,
            'token_type': 'Bearer',
            'expires_in': security_config.JWT_EXPIRATION
        }), 200
        
    except Exception as e:
        logger.error(f"Token refresh error: {e}")
        return jsonify({'error': 'Internal server error'}), 500

@auth_bp.route('/logout', methods=['POST'])
@require_auth
def logout():
    """Logout user and invalidate tokens"""
    try:
        data = request.get_json()
        refresh_token = data.get('refresh_token') if data else None
        
        if refresh_token and refresh_token in refresh_tokens_db:
            del refresh_tokens_db[refresh_token]
        
        return jsonify({'message': 'Logout successful'}), 200
        
    except Exception as e:
        logger.error(f"Logout error: {e}")
        return jsonify({'error': 'Internal server error'}), 500

@auth_bp.route('/api-keys', methods=['POST'])
@require_auth
def create_api_key():
    """Create new API key for authenticated user"""
    try:
        data = request.get_json()
        name = data.get('name', 'Default API Key') if data else 'Default API Key'
        
        api_key = auth_service.create_api_key(g.user_id, name)
        
        return jsonify({
            'message': 'API key created successfully',
            'api_key': api_key,
            'name': name
        }), 201
        
    except Exception as e:
        logger.error(f"API key creation error: {e}")
        return jsonify({'error': 'Internal server error'}), 500

@auth_bp.route('/api-keys', methods=['GET'])
@require_auth
def list_api_keys():
    """List API keys for authenticated user"""
    try:
        user_keys = []
        for key_hash, key_data in api_keys_db.items():
            if key_data['user_id'] == g.user_id:
                user_keys.append({
                    'name': key_data['name'],
                    'created_at': key_data['created_at'],
                    'last_used': key_data['last_used'],
                    'is_active': key_data['is_active']
                })
        
        return jsonify({'api_keys': user_keys}), 200
        
    except Exception as e:
        logger.error(f"API key listing error: {e}")
        return jsonify({'error': 'Internal server error'}), 500

@auth_bp.route('/profile', methods=['GET'])
@require_auth
def get_profile():
    """Get current user profile"""
    try:
        # Find user by ID
        user = None
        for u in users_db.values():
            if u['id'] == g.user_id:
                user = u
                break
        
        if not user:
            return jsonify({'error': 'User not found'}), 404
        
        return jsonify({
            'user': {
                'id': user['id'],
                'username': user['username'],
                'email': user['email'],
                'role': user['role'],
                'created_at': user['created_at'],
                'last_login': user['last_login']
            }
        }), 200
        
    except Exception as e:
        logger.error(f"Profile retrieval error: {e}")
        return jsonify({'error': 'Internal server error'}), 500

@auth_bp.route('/security', methods=['GET'])
@require_auth
def get_security_info():
    """Get security information for current user"""
    try:
        return jsonify({
            'rate_limiting': security_config.get_rate_limit_config(),
            'auth_enabled': security_config.AUTH_ENABLED,
            'api_key_enabled': security_config.API_KEY_ENABLED,
            'request_signing_enabled': security_config.REQUEST_SIGNING_ENABLED
        }), 200
        
    except Exception as e:
        logger.error(f"Security info retrieval error: {e}")
        return jsonify({'error': 'Internal server error'}), 500 