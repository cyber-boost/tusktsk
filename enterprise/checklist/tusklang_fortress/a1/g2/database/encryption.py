"""
TuskLang FORTRESS - Database Encryption System
Agent A1 - Goal G2: Database Protection Implementation

This module provides comprehensive database encryption including:
- Column-level encryption for sensitive data
- Database connection encryption (SSL/TLS)
- Key management and rotation
- Audit logging for encryption operations
"""

import os
import base64
import hashlib
import logging
from cryptography.fernet import Fernet
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
from cryptography.hazmat.backends import default_backend
import json
import time
from typing import Dict, Any, Optional, List
from datetime import datetime, timedelta

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class DatabaseEncryptionConfig:
    """Database encryption configuration"""
    
    # Encryption settings
    ENCRYPTION_ALGORITHM = "AES-256-GCM"
    KEY_DERIVATION_ITERATIONS = 100000
    SALT_SIZE = 16
    IV_SIZE = 12
    TAG_SIZE = 16
    
    # Key rotation settings
    KEY_ROTATION_DAYS = 90
    MAX_KEYS_IN_USE = 3
    
    # Sensitive columns to encrypt
    SENSITIVE_COLUMNS = [
        'password_hash',
        'api_key',
        'private_key',
        'secret_token',
        'encrypted_data',
        'personal_info',
        'financial_data'
    ]
    
    # Audit settings
    AUDIT_ENCRYPTION_OPERATIONS = True
    AUDIT_LOG_FILE = "database_encryption_audit.log"

class KeyManager:
    """Database encryption key management"""
    
    def __init__(self, master_key: Optional[str] = None):
        self.config = DatabaseEncryptionConfig()
        self.master_key = master_key or os.getenv('DB_MASTER_KEY', self._generate_master_key())
        self.keys = {}
        self.key_metadata = {}
        self._load_keys()
    
    def _generate_master_key(self) -> str:
        """Generate a new master key"""
        key = Fernet.generate_key()
        logger.info("Generated new master key for database encryption")
        return key.decode()
    
    def _load_keys(self):
        """Load encryption keys from storage"""
        try:
            # In production, load from secure key management system
            # For demo, create new keys
            self._create_new_key("current")
            self._create_new_key("previous")
            logger.info("Database encryption keys loaded successfully")
        except Exception as e:
            logger.error(f"Failed to load encryption keys: {e}")
            raise
    
    def _create_new_key(self, key_id: str) -> str:
        """Create a new encryption key"""
        salt = os.urandom(self.config.SALT_SIZE)
        kdf = PBKDF2HMAC(
            algorithm=hashes.SHA256(),
            length=32,
            salt=salt,
            iterations=self.config.KEY_DERIVATION_ITERATIONS,
            backend=default_backend()
        )
        
        key = base64.urlsafe_b64encode(kdf.derive(self.master_key.encode()))
        self.keys[key_id] = key
        self.key_metadata[key_id] = {
            'created_at': datetime.now(),
            'salt': salt,
            'iterations': self.config.KEY_DERIVATION_ITERATIONS
        }
        
        logger.info(f"Created new encryption key: {key_id}")
        return key.decode()
    
    def get_current_key(self) -> bytes:
        """Get the current encryption key"""
        return self.keys.get('current')
    
    def rotate_keys(self):
        """Rotate encryption keys"""
        try:
            # Move current to previous
            if 'current' in self.keys:
                self.keys['previous'] = self.keys['current']
                self.key_metadata['previous'] = self.key_metadata['current']
            
            # Create new current key
            self._create_new_key('current')
            
            logger.info("Database encryption keys rotated successfully")
            self._audit_operation("key_rotation", "Keys rotated successfully")
            
        except Exception as e:
            logger.error(f"Failed to rotate encryption keys: {e}")
            raise
    
    def _audit_operation(self, operation: str, details: str):
        """Audit encryption operations"""
        if not self.config.AUDIT_ENCRYPTION_OPERATIONS:
            return
        
        audit_entry = {
            'timestamp': datetime.now().isoformat(),
            'operation': operation,
            'details': details,
            'user_id': 'system'
        }
        
        try:
            with open(self.config.AUDIT_LOG_FILE, 'a') as f:
                f.write(json.dumps(audit_entry) + '\n')
        except Exception as e:
            logger.error(f"Failed to write audit log: {e}")

class DatabaseEncryption:
    """Database encryption and decryption operations"""
    
    def __init__(self, key_manager: KeyManager):
        self.key_manager = key_manager
        self.config = DatabaseEncryptionConfig()
    
    def encrypt_value(self, value: str, key_id: str = 'current') -> str:
        """Encrypt a database value"""
        try:
            if not value:
                return value
            
            key = self.key_manager.keys.get(key_id)
            if not key:
                raise ValueError(f"Encryption key not found: {key_id}")
            
            # Generate IV
            iv = os.urandom(self.config.IV_SIZE)
            
            # Create cipher
            cipher = Cipher(
                algorithms.AES(key),
                modes.GCM(iv),
                backend=default_backend()
            )
            encryptor = cipher.encryptor()
            
            # Encrypt data
            ciphertext = encryptor.update(value.encode()) + encryptor.finalize()
            
            # Combine IV, ciphertext, and tag
            encrypted_data = iv + ciphertext + encryptor.tag
            
            # Base64 encode for storage
            encrypted_b64 = base64.urlsafe_b64encode(encrypted_data).decode()
            
            self.key_manager._audit_operation("encrypt", f"Encrypted value with key: {key_id}")
            return encrypted_b64
            
        except Exception as e:
            logger.error(f"Failed to encrypt value: {e}")
            raise
    
    def decrypt_value(self, encrypted_value: str, key_id: str = 'current') -> str:
        """Decrypt a database value"""
        try:
            if not encrypted_value:
                return encrypted_value
            
            key = self.key_manager.keys.get(key_id)
            if not key:
                raise ValueError(f"Encryption key not found: {key_id}")
            
            # Base64 decode
            encrypted_data = base64.urlsafe_b64decode(encrypted_value.encode())
            
            # Extract IV, ciphertext, and tag
            iv = encrypted_data[:self.config.IV_SIZE]
            tag = encrypted_data[-self.config.TAG_SIZE:]
            ciphertext = encrypted_data[self.config.IV_SIZE:-self.config.TAG_SIZE]
            
            # Create cipher
            cipher = Cipher(
                algorithms.AES(key),
                modes.GCM(iv, tag),
                backend=default_backend()
            )
            decryptor = cipher.decryptor()
            
            # Decrypt data
            decrypted_data = decryptor.update(ciphertext) + decryptor.finalize()
            
            self.key_manager._audit_operation("decrypt", f"Decrypted value with key: {key_id}")
            return decrypted_data.decode()
            
        except Exception as e:
            logger.error(f"Failed to decrypt value: {e}")
            raise
    
    def encrypt_column(self, column_name: str, value: Any) -> Any:
        """Encrypt a database column value if it's sensitive"""
        if column_name in self.config.SENSITIVE_COLUMNS and value:
            return self.encrypt_value(str(value))
        return value
    
    def decrypt_column(self, column_name: str, value: Any) -> Any:
        """Decrypt a database column value if it's sensitive"""
        if column_name in self.config.SENSITIVE_COLUMNS and value:
            return self.decrypt_value(str(value))
        return value
    
    def bulk_encrypt(self, data: Dict[str, Any]) -> Dict[str, Any]:
        """Encrypt multiple columns in a data dictionary"""
        encrypted_data = {}
        for column, value in data.items():
            encrypted_data[column] = self.encrypt_column(column, value)
        return encrypted_data
    
    def bulk_decrypt(self, data: Dict[str, Any]) -> Dict[str, Any]:
        """Decrypt multiple columns in a data dictionary"""
        decrypted_data = {}
        for column, value in data.items():
            decrypted_data[column] = self.decrypt_column(column, value)
        return decrypted_data

class DatabaseConnectionSecurity:
    """Database connection security and SSL/TLS configuration"""
    
    def __init__(self):
        self.ssl_config = {
            'ssl': True,
            'ssl_ca': '/etc/ssl/certs/ca-certificates.crt',
            'ssl_cert': '/path/to/client-cert.pem',
            'ssl_key': '/path/to/client-key.pem',
            'ssl_verify_cert': True,
            'ssl_verify_hostname': True
        }
    
    def get_secure_connection_params(self) -> Dict[str, Any]:
        """Get secure database connection parameters"""
        return {
            'host': os.getenv('DB_HOST', 'localhost'),
            'port': int(os.getenv('DB_PORT', 5432)),
            'database': os.getenv('DB_NAME', 'tusklang_fortress'),
            'user': os.getenv('DB_USER', 'fortress_user'),
            'password': os.getenv('DB_PASSWORD', ''),
            'sslmode': 'require',
            'sslrootcert': self.ssl_config['ssl_ca'],
            'sslcert': self.ssl_config['ssl_cert'],
            'sslkey': self.ssl_config['ssl_key']
        }
    
    def validate_connection_security(self, connection) -> bool:
        """Validate that database connection is secure"""
        try:
            # Check SSL status
            ssl_status = connection.get_ssl_status()
            if not ssl_status:
                logger.warning("Database connection is not using SSL")
                return False
            
            # Check connection encryption
            if not connection.is_encrypted():
                logger.warning("Database connection is not encrypted")
                return False
            
            logger.info("Database connection security validated successfully")
            return True
            
        except Exception as e:
            logger.error(f"Failed to validate connection security: {e}")
            return False

class DatabaseAuditLogger:
    """Database audit logging system"""
    
    def __init__(self, audit_table: str = 'database_audit_log'):
        self.audit_table = audit_table
        self.config = DatabaseEncryptionConfig()
    
    def log_operation(self, operation: str, table: str, record_id: str, 
                     user_id: str, details: Dict[str, Any]):
        """Log a database operation for audit purposes"""
        try:
            audit_entry = {
                'timestamp': datetime.now().isoformat(),
                'operation': operation,
                'table_name': table,
                'record_id': record_id,
                'user_id': user_id,
                'ip_address': self._get_client_ip(),
                'session_id': self._get_session_id(),
                'details': json.dumps(details),
                'hash': self._generate_audit_hash(operation, table, record_id, details)
            }
            
            # In production, insert into database audit table
            logger.info(f"Audit log: {operation} on {table}.{record_id} by {user_id}")
            
            return audit_entry
            
        except Exception as e:
            logger.error(f"Failed to log audit entry: {e}")
            raise
    
    def _get_client_ip(self) -> str:
        """Get client IP address"""
        # In production, extract from request context
        return "127.0.0.1"
    
    def _get_session_id(self) -> str:
        """Get session ID"""
        # In production, extract from session context
        return hashlib.md5(str(time.time()).encode()).hexdigest()
    
    def _generate_audit_hash(self, operation: str, table: str, 
                           record_id: str, details: Dict[str, Any]) -> str:
        """Generate audit entry hash for integrity verification"""
        data = f"{operation}:{table}:{record_id}:{json.dumps(details, sort_keys=True)}"
        return hashlib.sha256(data.encode()).hexdigest()
    
    def verify_audit_integrity(self, audit_entry: Dict[str, Any]) -> bool:
        """Verify audit entry integrity"""
        try:
            expected_hash = self._generate_audit_hash(
                audit_entry['operation'],
                audit_entry['table_name'],
                audit_entry['record_id'],
                json.loads(audit_entry['details'])
            )
            return audit_entry['hash'] == expected_hash
        except Exception as e:
            logger.error(f"Failed to verify audit integrity: {e}")
            return False

# Initialize database encryption system
key_manager = KeyManager()
db_encryption = DatabaseEncryption(key_manager)
db_connection_security = DatabaseConnectionSecurity()
db_audit_logger = DatabaseAuditLogger() 