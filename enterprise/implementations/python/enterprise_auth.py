#!/usr/bin/env python3
"""
TuskLang Enterprise Authentication
SAML/OAuth2 support for enterprise environments
"""

import os
import json
import time
import base64
import hashlib
import hmac
import urllib.parse
from typing import Dict, List, Optional, Any, Tuple
from dataclasses import dataclass
from pathlib import Path
import logging
import requests
from cryptography.hazmat.primitives import hashes, serialization
from cryptography.hazmat.primitives.asymmetric import rsa, padding
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
from cryptography.hazmat.backends import default_backend
import xml.etree.ElementTree as ET

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

@dataclass
class AuthConfig:
    """Authentication configuration"""
    auth_type: str  # 'saml', 'oauth2', 'jwt', 'api_key'
    client_id: Optional[str] = None
    client_secret: Optional[str] = None
    redirect_uri: Optional[str] = None
    auth_url: Optional[str] = None
    token_url: Optional[str] = None
    userinfo_url: Optional[str] = None
    issuer: Optional[str] = None
    audience: Optional[str] = None
    cert_path: Optional[str] = None
    key_path: Optional[str] = None
    api_key: Optional[str] = None
    scopes: List[str] = None
    
    def __post_init__(self):
        if self.scopes is None:
            self.scopes = []

@dataclass
class UserInfo:
    """User information from authentication"""
    user_id: str
    email: str
    name: str
    groups: List[str] = None
    permissions: List[str] = None
    metadata: Dict[str, Any] = None
    
    def __post_init__(self):
        if self.groups is None:
            self.groups = []
        if self.permissions is None:
            self.permissions = []
        if self.metadata is None:
            self.metadata = {}

@dataclass
class AuthToken:
    """Authentication token"""
    access_token: str
    token_type: str = 'Bearer'
    expires_in: Optional[int] = None
    refresh_token: Optional[str] = None
    scope: Optional[str] = None
    created_at: float = None
    
    def __post_init__(self):
        if self.created_at is None:
            self.created_at = time.time()
    
    @property
    def is_expired(self) -> bool:
        """Check if token is expired"""
        if self.expires_in is None:
            return False
        return time.time() > (self.created_at + self.expires_in)

class EnterpriseAuth:
    """Enterprise authentication manager"""
    
    def __init__(self, config: AuthConfig):
        self.config = config
        self.session = requests.Session()
        self._private_key = None
        self._public_key = None
        self._load_keys()
    
    def _load_keys(self):
        """Load cryptographic keys"""
        try:
            if self.config.key_path and os.path.exists(self.config.key_path):
                with open(self.config.key_path, 'rb') as f:
                    self._private_key = serialization.load_pem_private_key(
                        f.read(), password=None, backend=default_backend()
                    )
            
            if self.config.cert_path and os.path.exists(self.config.cert_path):
                with open(self.config.cert_path, 'rb') as f:
                    self._public_key = serialization.load_pem_x509_certificate(
                        f.read(), backend=default_backend()
                    ).public_key()
        except Exception as e:
            logger.warning(f"Failed to load keys: {e}")
    
    def authenticate(self, credentials: Dict[str, Any]) -> Tuple[AuthToken, UserInfo]:
        """Authenticate user based on configuration"""
        if self.config.auth_type == 'oauth2':
            return self._authenticate_oauth2(credentials)
        elif self.config.auth_type == 'saml':
            return self._authenticate_saml(credentials)
        elif self.config.auth_type == 'jwt':
            return self._authenticate_jwt(credentials)
        elif self.config.auth_type == 'api_key':
            return self._authenticate_api_key(credentials)
        else:
            raise ValueError(f"Unsupported authentication type: {self.config.auth_type}")
    
    def _authenticate_oauth2(self, credentials: Dict[str, Any]) -> Tuple[AuthToken, UserInfo]:
        """OAuth2 authentication flow"""
        if 'code' in credentials:
            # Authorization code flow
            return self._oauth2_code_flow(credentials['code'])
        elif 'username' in credentials and 'password' in credentials:
            # Resource owner password credentials flow
            return self._oauth2_password_flow(credentials['username'], credentials['password'])
        elif 'client_credentials' in credentials:
            # Client credentials flow
            return self._oauth2_client_credentials_flow()
        else:
            raise ValueError("Invalid OAuth2 credentials")
    
    def _oauth2_code_flow(self, code: str) -> Tuple[AuthToken, UserInfo]:
        """OAuth2 authorization code flow"""
        # Exchange code for token
        token_data = {
            'grant_type': 'authorization_code',
            'code': code,
            'client_id': self.config.client_id,
            'client_secret': self.config.client_secret,
            'redirect_uri': self.config.redirect_uri
        }
        
        response = self.session.post(self.config.token_url, data=token_data)
        response.raise_for_status()
        
        token_response = response.json()
        auth_token = AuthToken(
            access_token=token_response['access_token'],
            token_type=token_response.get('token_type', 'Bearer'),
            expires_in=token_response.get('expires_in'),
            refresh_token=token_response.get('refresh_token'),
            scope=token_response.get('scope')
        )
        
        # Get user info
        user_info = self._get_oauth2_userinfo(auth_token)
        
        return auth_token, user_info
    
    def _oauth2_password_flow(self, username: str, password: str) -> Tuple[AuthToken, UserInfo]:
        """OAuth2 resource owner password credentials flow"""
        token_data = {
            'grant_type': 'password',
            'username': username,
            'password': password,
            'client_id': self.config.client_id,
            'client_secret': self.config.client_secret,
            'scope': ' '.join(self.config.scopes)
        }
        
        response = self.session.post(self.config.token_url, data=token_data)
        response.raise_for_status()
        
        token_response = response.json()
        auth_token = AuthToken(
            access_token=token_response['access_token'],
            token_type=token_response.get('token_type', 'Bearer'),
            expires_in=token_response.get('expires_in'),
            refresh_token=token_response.get('refresh_token'),
            scope=token_response.get('scope')
        )
        
        # Get user info
        user_info = self._get_oauth2_userinfo(auth_token)
        
        return auth_token, user_info
    
    def _oauth2_client_credentials_flow(self) -> Tuple[AuthToken, UserInfo]:
        """OAuth2 client credentials flow"""
        token_data = {
            'grant_type': 'client_credentials',
            'client_id': self.config.client_id,
            'client_secret': self.config.client_secret,
            'scope': ' '.join(self.config.scopes)
        }
        
        response = self.session.post(self.config.token_url, data=token_data)
        response.raise_for_status()
        
        token_response = response.json()
        auth_token = AuthToken(
            access_token=token_response['access_token'],
            token_type=token_response.get('token_type', 'Bearer'),
            expires_in=token_response.get('expires_in'),
            scope=token_response.get('scope')
        )
        
        # For client credentials, create a service account user info
        user_info = UserInfo(
            user_id=f"service_{self.config.client_id}",
            email=f"{self.config.client_id}@service.local",
            name=f"Service Account: {self.config.client_id}",
            groups=['service_accounts'],
            permissions=self.config.scopes
        )
        
        return auth_token, user_info
    
    def _get_oauth2_userinfo(self, auth_token: AuthToken) -> UserInfo:
        """Get user information from OAuth2 provider"""
        if not self.config.userinfo_url:
            # Try to decode JWT token if no userinfo endpoint
            return self._decode_jwt_userinfo(auth_token.access_token)
        
        headers = {'Authorization': f"{auth_token.token_type} {auth_token.access_token}"}
        response = self.session.get(self.config.userinfo_url, headers=headers)
        response.raise_for_status()
        
        user_data = response.json()
        
        return UserInfo(
            user_id=user_data.get('sub', user_data.get('id')),
            email=user_data.get('email'),
            name=user_data.get('name', user_data.get('preferred_username')),
            groups=user_data.get('groups', []),
            permissions=user_data.get('permissions', []),
            metadata=user_data
        )
    
    def _authenticate_saml(self, credentials: Dict[str, Any]) -> Tuple[AuthToken, UserInfo]:
        """SAML authentication"""
        if 'saml_response' not in credentials:
            raise ValueError("SAML response required")
        
        saml_response = credentials['saml_response']
        
        # Decode SAML response
        if saml_response.startswith('<?xml'):
            # XML format
            user_info = self._parse_saml_xml(saml_response)
        else:
            # Base64 encoded
            decoded = base64.b64decode(saml_response)
            user_info = self._parse_saml_xml(decoded.decode('utf-8'))
        
        # Create JWT token for internal use
        auth_token = self._create_jwt_token(user_info)
        
        return auth_token, user_info
    
    def _parse_saml_xml(self, saml_xml: str) -> UserInfo:
        """Parse SAML XML response"""
        try:
            root = ET.fromstring(saml_xml)
            
            # Extract user information from SAML attributes
            ns = {'saml': 'urn:oasis:names:tc:SAML:2.0:assertion'}
            
            # Find assertion
            assertion = root.find('.//saml:Assertion', ns)
            if assertion is None:
                raise ValueError("No SAML assertion found")
            
            # Extract subject
            subject = assertion.find('.//saml:Subject', ns)
            user_id = None
            if subject is not None:
                name_id = subject.find('.//saml:NameID', ns)
                if name_id is not None:
                    user_id = name_id.text
            
            # Extract attributes
            attributes = {}
            attr_stmt = assertion.find('.//saml:AttributeStatement', ns)
            if attr_stmt is not None:
                for attr in attr_stmt.findall('.//saml:Attribute', ns):
                    name = attr.get('Name')
                    values = []
                    for value in attr.findall('.//saml:AttributeValue', ns):
                        if value.text:
                            values.append(value.text)
                    attributes[name] = values[0] if len(values) == 1 else values
            
            return UserInfo(
                user_id=user_id or attributes.get('uid', 'unknown'),
                email=attributes.get('email', attributes.get('mail')),
                name=attributes.get('displayName', attributes.get('cn')),
                groups=attributes.get('groups', []),
                permissions=attributes.get('permissions', []),
                metadata=attributes
            )
            
        except ET.ParseError as e:
            raise ValueError(f"Invalid SAML XML: {e}")
    
    def _authenticate_jwt(self, credentials: Dict[str, Any]) -> Tuple[AuthToken, UserInfo]:
        """JWT authentication"""
        if 'token' not in credentials:
            raise ValueError("JWT token required")
        
        token = credentials['token']
        
        # Verify and decode JWT
        user_info = self._verify_jwt_token(token)
        
        # Create internal token
        auth_token = AuthToken(
            access_token=token,
            token_type='Bearer',
            expires_in=3600  # Assume 1 hour
        )
        
        return auth_token, user_info
    
    def _verify_jwt_token(self, token: str) -> UserInfo:
        """Verify and decode JWT token"""
        try:
            # Split token
            parts = token.split('.')
            if len(parts) != 3:
                raise ValueError("Invalid JWT format")
            
            # Decode payload
            payload_b64 = parts[1]
            # Add padding if needed
            payload_b64 += '=' * (4 - len(payload_b64) % 4)
            payload = json.loads(base64.b64decode(payload_b64))
            
            # Verify signature if public key available
            if self._public_key:
                signature = base64.urlsafe_b64decode(parts[2] + '=')
                message = f"{parts[0]}.{parts[1]}".encode('utf-8')
                
                try:
                    self._public_key.verify(
                        signature,
                        message,
                        padding.PKCS1v15(),
                        hashes.SHA256()
                    )
                except Exception as e:
                    raise ValueError(f"JWT signature verification failed: {e}")
            
            # Check expiration
            if 'exp' in payload and payload['exp'] < time.time():
                raise ValueError("JWT token expired")
            
            # Check audience
            if self.config.audience and payload.get('aud') != self.config.audience:
                raise ValueError("JWT audience mismatch")
            
            # Check issuer
            if self.config.issuer and payload.get('iss') != self.config.issuer:
                raise ValueError("JWT issuer mismatch")
            
            return UserInfo(
                user_id=payload.get('sub', payload.get('user_id')),
                email=payload.get('email'),
                name=payload.get('name', payload.get('preferred_username')),
                groups=payload.get('groups', []),
                permissions=payload.get('permissions', []),
                metadata=payload
            )
            
        except Exception as e:
            raise ValueError(f"JWT verification failed: {e}")
    
    def _authenticate_api_key(self, credentials: Dict[str, Any]) -> Tuple[AuthToken, UserInfo]:
        """API key authentication"""
        if 'api_key' not in credentials:
            raise ValueError("API key required")
        
        api_key = credentials['api_key']
        
        # Verify API key
        if api_key != self.config.api_key:
            raise ValueError("Invalid API key")
        
        # Create service account user info
        user_info = UserInfo(
            user_id="api_user",
            email="api@service.local",
            name="API User",
            groups=['api_users'],
            permissions=['read', 'write']
        )
        
        # Create token
        auth_token = AuthToken(
            access_token=api_key,
            token_type='API-Key',
            expires_in=None  # API keys don't expire
        )
        
        return auth_token, user_info
    
    def _decode_jwt_userinfo(self, token: str) -> UserInfo:
        """Decode user info from JWT token"""
        try:
            parts = token.split('.')
            if len(parts) != 3:
                raise ValueError("Invalid JWT format")
            
            payload_b64 = parts[1]
            payload_b64 += '=' * (4 - len(payload_b64) % 4)
            payload = json.loads(base64.b64decode(payload_b64))
            
            return UserInfo(
                user_id=payload.get('sub', payload.get('user_id')),
                email=payload.get('email'),
                name=payload.get('name', payload.get('preferred_username')),
                groups=payload.get('groups', []),
                permissions=payload.get('permissions', []),
                metadata=payload
            )
        except Exception as e:
            logger.warning(f"Failed to decode JWT userinfo: {e}")
            return UserInfo(
                user_id="unknown",
                email="unknown@local",
                name="Unknown User"
            )
    
    def _create_jwt_token(self, user_info: UserInfo) -> AuthToken:
        """Create JWT token for internal use"""
        if not self._private_key:
            raise ValueError("Private key required for JWT creation")
        
        # Create payload
        payload = {
            'sub': user_info.user_id,
            'email': user_info.email,
            'name': user_info.name,
            'groups': user_info.groups,
            'permissions': user_info.permissions,
            'iat': int(time.time()),
            'exp': int(time.time()) + 3600,  # 1 hour
            'iss': self.config.issuer or 'tusklang',
            'aud': self.config.audience or 'tusklang'
        }
        
        # Create JWT
        header = {'alg': 'RS256', 'typ': 'JWT'}
        header_b64 = base64.urlsafe_b64encode(json.dumps(header).encode()).rstrip(b'=')
        payload_b64 = base64.urlsafe_b64encode(json.dumps(payload).encode()).rstrip(b'=')
        
        message = header_b64 + b'.' + payload_b64
        signature = self._private_key.sign(
            message,
            padding.PKCS1v15(),
            hashes.SHA256()
        )
        signature_b64 = base64.urlsafe_b64encode(signature).rstrip(b'=')
        
        token = message.decode() + '.' + signature_b64.decode()
        
        return AuthToken(
            access_token=token,
            token_type='Bearer',
            expires_in=3600
        )
    
    def refresh_token(self, auth_token: AuthToken) -> AuthToken:
        """Refresh OAuth2 token"""
        if not auth_token.refresh_token:
            raise ValueError("No refresh token available")
        
        token_data = {
            'grant_type': 'refresh_token',
            'refresh_token': auth_token.refresh_token,
            'client_id': self.config.client_id,
            'client_secret': self.config.client_secret
        }
        
        response = self.session.post(self.config.token_url, data=token_data)
        response.raise_for_status()
        
        token_response = response.json()
        return AuthToken(
            access_token=token_response['access_token'],
            token_type=token_response.get('token_type', 'Bearer'),
            expires_in=token_response.get('expires_in'),
            refresh_token=token_response.get('refresh_token', auth_token.refresh_token),
            scope=token_response.get('scope')
        )
    
    def revoke_token(self, auth_token: AuthToken):
        """Revoke OAuth2 token"""
        if not hasattr(self, 'revoke_url'):
            logger.warning("No revoke URL configured")
            return
        
        data = {
            'token': auth_token.access_token,
            'client_id': self.config.client_id,
            'client_secret': self.config.client_secret
        }
        
        response = self.session.post(self.revoke_url, data=data)
        response.raise_for_status()
    
    def get_authorization_url(self, state: str = None) -> str:
        """Get OAuth2 authorization URL"""
        if not self.config.auth_url:
            raise ValueError("Authorization URL not configured")
        
        params = {
            'response_type': 'code',
            'client_id': self.config.client_id,
            'redirect_uri': self.config.redirect_uri,
            'scope': ' '.join(self.config.scopes),
            'state': state or self._generate_state()
        }
        
        return f"{self.config.auth_url}?{urllib.parse.urlencode(params)}"
    
    def _generate_state(self) -> str:
        """Generate random state parameter"""
        return base64.urlsafe_b64encode(os.urandom(32)).decode().rstrip('=')

class AuthManager:
    """Authentication manager for TuskLang applications"""
    
    def __init__(self, config_path: str = None):
        self.configs = {}
        self.active_sessions = {}
        
        if config_path:
            self.load_config(config_path)
    
    def load_config(self, config_path: str):
        """Load authentication configuration"""
        config_file = Path(config_path)
        if not config_file.exists():
            raise FileNotFoundError(f"Config file not found: {config_path}")
        
        with open(config_file, 'r') as f:
            config_data = json.load(f)
        
        for auth_name, auth_config in config_data.items():
            self.configs[auth_name] = AuthConfig(**auth_config)
    
    def authenticate(self, auth_name: str, credentials: Dict[str, Any]) -> Tuple[AuthToken, UserInfo]:
        """Authenticate using specified configuration"""
        if auth_name not in self.configs:
            raise ValueError(f"Authentication configuration not found: {auth_name}")
        
        config = self.configs[auth_name]
        auth = EnterpriseAuth(config)
        
        auth_token, user_info = auth.authenticate(credentials)
        
        # Store session
        session_id = self._generate_session_id()
        self.active_sessions[session_id] = {
            'auth_token': auth_token,
            'user_info': user_info,
            'auth_name': auth_name,
            'created_at': time.time()
        }
        
        return auth_token, user_info
    
    def validate_session(self, session_id: str) -> Optional[UserInfo]:
        """Validate session and return user info"""
        if session_id not in self.active_sessions:
            return None
        
        session = self.active_sessions[session_id]
        auth_token = session['auth_token']
        
        # Check if token is expired
        if auth_token.is_expired:
            # Try to refresh token
            try:
                config = self.configs[session['auth_name']]
                auth = EnterpriseAuth(config)
                new_token = auth.refresh_token(auth_token)
                session['auth_token'] = new_token
            except Exception as e:
                logger.warning(f"Failed to refresh token: {e}")
                del self.active_sessions[session_id]
                return None
        
        return session['user_info']
    
    def logout(self, session_id: str):
        """Logout and invalidate session"""
        if session_id in self.active_sessions:
            session = self.active_sessions[session_id]
            
            # Revoke token if possible
            try:
                config = self.configs[session['auth_name']]
                auth = EnterpriseAuth(config)
                auth.revoke_token(session['auth_token'])
            except Exception as e:
                logger.warning(f"Failed to revoke token: {e}")
            
            del self.active_sessions[session_id]
    
    def _generate_session_id(self) -> str:
        """Generate unique session ID"""
        return base64.urlsafe_b64encode(os.urandom(32)).decode().rstrip('=')

# Example usage and configuration
def create_oauth2_config():
    """Create OAuth2 configuration example"""
    return AuthConfig(
        auth_type='oauth2',
        client_id='your_client_id',
        client_secret='your_client_secret',
        redirect_uri='http://localhost:8080/callback',
        auth_url='https://accounts.google.com/o/oauth2/auth',
        token_url='https://oauth2.googleapis.com/token',
        userinfo_url='https://www.googleapis.com/oauth2/v2/userinfo',
        scopes=['openid', 'email', 'profile']
    )

def create_saml_config():
    """Create SAML configuration example"""
    return AuthConfig(
        auth_type='saml',
        issuer='https://your-app.com',
        audience='https://your-saml-provider.com',
        cert_path='/path/to/cert.pem',
        key_path='/path/to/key.pem'
    )

def create_jwt_config():
    """Create JWT configuration example"""
    return AuthConfig(
        auth_type='jwt',
        issuer='https://your-jwt-issuer.com',
        audience='your-app',
        cert_path='/path/to/public-key.pem'
    )

def create_api_key_config():
    """Create API key configuration example"""
    return AuthConfig(
        auth_type='api_key',
        api_key='your-api-key'
    )

if __name__ == '__main__':
    # Example usage
    auth_manager = AuthManager()
    
    # Add OAuth2 configuration
    oauth2_config = create_oauth2_config()
    auth_manager.configs['google'] = oauth2_config
    
    # Add SAML configuration
    saml_config = create_saml_config()
    auth_manager.configs['saml'] = saml_config
    
    # Example authentication
    try:
        # OAuth2 authentication
        credentials = {'code': 'authorization_code_here'}
        auth_token, user_info = auth_manager.authenticate('google', credentials)
        print(f"Authenticated user: {user_info.name} ({user_info.email})")
        
        # JWT authentication
        jwt_credentials = {'token': 'jwt_token_here'}
        auth_token, user_info = auth_manager.authenticate('jwt', jwt_credentials)
        print(f"JWT authenticated user: {user_info.name}")
        
    except Exception as e:
        print(f"Authentication failed: {e}") 