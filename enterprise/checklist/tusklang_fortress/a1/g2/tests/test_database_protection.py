"""
TuskLang FORTRESS - Database Protection Tests
Agent A1 - Goal G2: Database Protection Implementation

This module contains comprehensive tests for the database protection system.
"""

import unittest
import os
import tempfile
import json
import time
from unittest.mock import Mock, patch, MagicMock
import sys

# Add parent directory to path for imports
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from database.encryption import (
    DatabaseEncryptionConfig, KeyManager, DatabaseEncryption,
    DatabaseConnectionSecurity, DatabaseAuditLogger
)
from database.connection_pool import (
    DatabasePoolConfig, ConnectionHealthMonitor, SecureDatabasePool,
    DatabaseAccessControl, SecurityError
)

class TestDatabaseEncryptionConfig(unittest.TestCase):
    """Test database encryption configuration"""
    
    def setUp(self):
        self.config = DatabaseEncryptionConfig()
    
    def test_encryption_settings(self):
        """Test encryption algorithm and settings"""
        self.assertEqual(self.config.ENCRYPTION_ALGORITHM, "AES-256-GCM")
        self.assertGreater(self.config.KEY_DERIVATION_ITERATIONS, 0)
        self.assertGreater(self.config.SALT_SIZE, 0)
        self.assertGreater(self.config.IV_SIZE, 0)
    
    def test_sensitive_columns(self):
        """Test sensitive columns configuration"""
        sensitive_cols = self.config.SENSITIVE_COLUMNS
        self.assertIn('password_hash', sensitive_cols)
        self.assertIn('api_key', sensitive_cols)
        self.assertIn('private_key', sensitive_cols)
    
    def test_key_rotation_settings(self):
        """Test key rotation configuration"""
        self.assertGreater(self.config.KEY_ROTATION_DAYS, 0)
        self.assertGreater(self.config.MAX_KEYS_IN_USE, 0)

class TestKeyManager(unittest.TestCase):
    """Test encryption key management"""
    
    def setUp(self):
        self.key_manager = KeyManager()
    
    def test_master_key_generation(self):
        """Test master key generation"""
        master_key = self.key_manager._generate_master_key()
        self.assertIsInstance(master_key, str)
        self.assertGreater(len(master_key), 0)
    
    def test_key_creation(self):
        """Test encryption key creation"""
        key_id = "test_key"
        key = self.key_manager._create_new_key(key_id)
        
        self.assertIsInstance(key, str)
        self.assertIn(key_id, self.key_manager.keys)
        self.assertIn(key_id, self.key_manager.key_metadata)
    
    def test_get_current_key(self):
        """Test getting current encryption key"""
        current_key = self.key_manager.get_current_key()
        self.assertIsInstance(current_key, bytes)
        self.assertGreater(len(current_key), 0)
    
    def test_key_rotation(self):
        """Test encryption key rotation"""
        # Store initial keys
        initial_current = self.key_manager.keys.get('current')
        initial_previous = self.key_manager.keys.get('previous')
        
        # Rotate keys
        self.key_manager.rotate_keys()
        
        # Verify rotation
        new_current = self.key_manager.keys.get('current')
        new_previous = self.key_manager.keys.get('previous')
        
        self.assertNotEqual(initial_current, new_current)
        self.assertEqual(initial_current, new_previous)
        self.assertNotEqual(initial_previous, new_previous)

class TestDatabaseEncryption(unittest.TestCase):
    """Test database encryption operations"""
    
    def setUp(self):
        self.key_manager = KeyManager()
        self.encryption = DatabaseEncryption(self.key_manager)
        self.test_value = "sensitive_data_123"
    
    def test_encrypt_decrypt_value(self):
        """Test value encryption and decryption"""
        # Encrypt value
        encrypted = self.encryption.encrypt_value(self.test_value)
        self.assertIsInstance(encrypted, str)
        self.assertNotEqual(encrypted, self.test_value)
        
        # Decrypt value
        decrypted = self.encryption.decrypt_value(encrypted)
        self.assertEqual(decrypted, self.test_value)
    
    def test_encrypt_empty_value(self):
        """Test encryption of empty value"""
        encrypted = self.encryption.encrypt_value("")
        self.assertEqual(encrypted, "")
    
    def test_encrypt_column_sensitive(self):
        """Test encryption of sensitive column"""
        encrypted = self.encryption.encrypt_column('password_hash', self.test_value)
        self.assertNotEqual(encrypted, self.test_value)
        self.assertIsInstance(encrypted, str)
    
    def test_encrypt_column_non_sensitive(self):
        """Test encryption of non-sensitive column"""
        value = "public_data"
        encrypted = self.encryption.encrypt_column('username', value)
        self.assertEqual(encrypted, value)
    
    def test_bulk_encrypt(self):
        """Test bulk encryption of multiple columns"""
        data = {
            'username': 'testuser',
            'password_hash': 'secret123',
            'email': 'test@example.com',
            'api_key': 'key123'
        }
        
        encrypted_data = self.encryption.bulk_encrypt(data)
        
        # Check that sensitive columns are encrypted
        self.assertEqual(encrypted_data['username'], 'testuser')  # Not sensitive
        self.assertNotEqual(encrypted_data['password_hash'], 'secret123')  # Sensitive
        self.assertEqual(encrypted_data['email'], 'test@example.com')  # Not sensitive
        self.assertNotEqual(encrypted_data['api_key'], 'key123')  # Sensitive
    
    def test_bulk_decrypt(self):
        """Test bulk decryption of multiple columns"""
        # First encrypt
        data = {
            'username': 'testuser',
            'password_hash': 'secret123',
            'api_key': 'key123'
        }
        encrypted_data = self.encryption.bulk_encrypt(data)
        
        # Then decrypt
        decrypted_data = self.encryption.bulk_decrypt(encrypted_data)
        
        # Verify decryption
        self.assertEqual(decrypted_data['username'], 'testuser')
        self.assertEqual(decrypted_data['password_hash'], 'secret123')
        self.assertEqual(decrypted_data['api_key'], 'key123')

class TestDatabaseConnectionSecurity(unittest.TestCase):
    """Test database connection security"""
    
    def setUp(self):
        self.connection_security = DatabaseConnectionSecurity()
    
    def test_ssl_config(self):
        """Test SSL configuration"""
        ssl_config = self.connection_security.ssl_config
        self.assertTrue(ssl_config['ssl'])
        self.assertTrue(ssl_config['ssl_verify_cert'])
        self.assertTrue(ssl_config['ssl_verify_hostname'])
    
    def test_secure_connection_params(self):
        """Test secure connection parameters"""
        params = self.connection_security.get_secure_connection_params()
        
        required_keys = ['host', 'port', 'database', 'user', 'password', 'sslmode']
        for key in required_keys:
            self.assertIn(key, params)
        
        self.assertEqual(params['sslmode'], 'require')
    
    @patch('os.getenv')
    def test_connection_params_with_env_vars(self, mock_getenv):
        """Test connection parameters with environment variables"""
        mock_getenv.side_effect = lambda key, default=None: {
            'DB_HOST': 'testhost',
            'DB_PORT': '5433',
            'DB_NAME': 'testdb',
            'DB_USER': 'testuser',
            'DB_PASSWORD': 'testpass'
        }.get(key, default)
        
        params = self.connection_security.get_secure_connection_params()
        
        self.assertEqual(params['host'], 'testhost')
        self.assertEqual(params['port'], 5433)
        self.assertEqual(params['database'], 'testdb')
        self.assertEqual(params['user'], 'testuser')
        self.assertEqual(params['password'], 'testpass')

class TestDatabaseAuditLogger(unittest.TestCase):
    """Test database audit logging"""
    
    def setUp(self):
        self.audit_logger = DatabaseAuditLogger()
    
    def test_log_operation(self):
        """Test audit operation logging"""
        operation = "SELECT"
        table = "users"
        record_id = "123"
        user_id = "user456"
        details = {"query": "SELECT * FROM users WHERE id = 123"}
        
        audit_entry = self.audit_logger.log_operation(
            operation, table, record_id, user_id, details
        )
        
        self.assertIsInstance(audit_entry, dict)
        self.assertEqual(audit_entry['operation'], operation)
        self.assertEqual(audit_entry['table_name'], table)
        self.assertEqual(audit_entry['record_id'], record_id)
        self.assertEqual(audit_entry['user_id'], user_id)
        self.assertIn('timestamp', audit_entry)
        self.assertIn('hash', audit_entry)
    
    def test_audit_hash_generation(self):
        """Test audit hash generation"""
        operation = "INSERT"
        table = "users"
        record_id = "789"
        details = {"data": {"name": "test", "email": "test@example.com"}}
        
        hash1 = self.audit_logger._generate_audit_hash(operation, table, record_id, details)
        hash2 = self.audit_logger._generate_audit_hash(operation, table, record_id, details)
        
        # Same inputs should produce same hash
        self.assertEqual(hash1, hash2)
        
        # Different details should produce different hash
        details2 = {"data": {"name": "test2", "email": "test2@example.com"}}
        hash3 = self.audit_logger._generate_audit_hash(operation, table, record_id, details2)
        self.assertNotEqual(hash1, hash3)
    
    def test_audit_integrity_verification(self):
        """Test audit entry integrity verification"""
        operation = "UPDATE"
        table = "users"
        record_id = "456"
        details = {"changes": {"status": "active"}}
        
        audit_entry = self.audit_logger.log_operation(
            operation, table, record_id, "user123", details
        )
        
        # Verify integrity
        is_valid = self.audit_logger.verify_audit_integrity(audit_entry)
        self.assertTrue(is_valid)
        
        # Tamper with entry
        audit_entry['details'] = '{"tampered": true}'
        is_valid = self.audit_logger.verify_audit_integrity(audit_entry)
        self.assertFalse(is_valid)

class TestDatabasePoolConfig(unittest.TestCase):
    """Test database pool configuration"""
    
    def setUp(self):
        self.config = DatabasePoolConfig()
    
    def test_pool_settings(self):
        """Test pool configuration settings"""
        self.assertGreater(self.config.MIN_CONNECTIONS, 0)
        self.assertGreater(self.config.MAX_CONNECTIONS, self.config.MIN_CONNECTIONS)
        self.assertGreater(self.config.CONNECTION_TIMEOUT, 0)
        self.assertGreater(self.config.IDLE_TIMEOUT, 0)
        self.assertGreater(self.config.MAX_LIFETIME, 0)
    
    def test_health_check_settings(self):
        """Test health check configuration"""
        self.assertGreater(self.config.HEALTH_CHECK_INTERVAL, 0)
        self.assertGreater(self.config.HEALTH_CHECK_TIMEOUT, 0)
        self.assertGreater(self.config.MAX_FAILED_HEALTH_CHECKS, 0)
    
    def test_security_settings(self):
        """Test security configuration"""
        self.assertTrue(self.config.REQUIRE_SSL)
        self.assertTrue(self.config.VERIFY_CERTIFICATE)
        self.assertTrue(self.config.CONNECTION_ENCRYPTION)

class TestConnectionHealthMonitor(unittest.TestCase):
    """Test connection health monitoring"""
    
    def setUp(self):
        self.config = DatabasePoolConfig()
        self.monitor = ConnectionHealthMonitor(self.config)
    
    def test_health_monitor_initialization(self):
        """Test health monitor initialization"""
        self.assertEqual(self.monitor.config, self.config)
        self.assertIsInstance(self.monitor.health_status, dict)
        self.assertIsInstance(self.monitor.failed_checks, dict)
    
    def test_mark_connection_failed(self):
        """Test marking connection as failed"""
        connection_id = "conn123"
        
        # Mark connection failed
        self.monitor.mark_connection_failed(connection_id)
        self.assertEqual(self.monitor.failed_checks[connection_id], 1)
        
        # Mark failed again
        self.monitor.mark_connection_failed(connection_id)
        self.assertEqual(self.monitor.failed_checks[connection_id], 2)
    
    def test_reset_connection_failures(self):
        """Test resetting connection failures"""
        connection_id = "conn456"
        
        # Mark failed
        self.monitor.mark_connection_failed(connection_id)
        self.assertIn(connection_id, self.monitor.failed_checks)
        
        # Reset failures
        self.monitor.reset_connection_failures(connection_id)
        self.assertNotIn(connection_id, self.monitor.failed_checks)

class TestDatabaseAccessControl(unittest.TestCase):
    """Test database access control"""
    
    def setUp(self):
        self.access_control = DatabaseAccessControl()
    
    def test_role_permissions(self):
        """Test role-based permissions"""
        # Admin should have all permissions
        self.assertTrue(self.access_control.check_permission('admin', 'SELECT'))
        self.assertTrue(self.access_control.check_permission('admin', 'INSERT'))
        self.assertTrue(self.access_control.check_permission('admin', 'UPDATE'))
        self.assertTrue(self.access_control.check_permission('admin', 'DELETE'))
        
        # User should have limited permissions
        self.assertTrue(self.access_control.check_permission('user', 'SELECT'))
        self.assertTrue(self.access_control.check_permission('user', 'INSERT'))
        self.assertTrue(self.access_control.check_permission('user', 'UPDATE'))
        self.assertFalse(self.access_control.check_permission('user', 'DELETE'))
        
        # Readonly should only have SELECT
        self.assertTrue(self.access_control.check_permission('readonly', 'SELECT'))
        self.assertFalse(self.access_control.check_permission('readonly', 'INSERT'))
        self.assertFalse(self.access_control.check_permission('readonly', 'UPDATE'))
        self.assertFalse(self.access_control.check_permission('readonly', 'DELETE'))
    
    def test_table_permissions(self):
        """Test table-specific permissions"""
        # Admin can do everything on users table
        self.assertTrue(self.access_control.check_permission('admin', 'SELECT', 'users'))
        self.assertTrue(self.access_control.check_permission('admin', 'DELETE', 'users'))
        
        # User can only SELECT and UPDATE on users table
        self.assertTrue(self.access_control.check_permission('user', 'SELECT', 'users'))
        self.assertTrue(self.access_control.check_permission('user', 'UPDATE', 'users'))
        self.assertFalse(self.access_control.check_permission('user', 'DELETE', 'users'))
        
        # Readonly can only SELECT on users table
        self.assertTrue(self.access_control.check_permission('readonly', 'SELECT', 'users'))
        self.assertFalse(self.access_control.check_permission('readonly', 'UPDATE', 'users'))
    
    def test_invalid_role(self):
        """Test permissions for invalid role"""
        self.assertFalse(self.access_control.check_permission('invalid_role', 'SELECT'))
        self.assertFalse(self.access_control.check_permission('invalid_role', 'INSERT', 'users'))
    
    def test_invalid_operation(self):
        """Test permissions for invalid operation"""
        self.assertFalse(self.access_control.check_permission('user', 'INVALID_OP'))
        self.assertFalse(self.access_control.check_permission('admin', 'INVALID_OP', 'users'))

class TestSecureDatabasePool(unittest.TestCase):
    """Test secure database pool"""
    
    def setUp(self):
        self.config = DatabasePoolConfig()
        # Use smaller pool for testing
        self.config.MIN_CONNECTIONS = 1
        self.config.MAX_CONNECTIONS = 3
        self.config.ENABLE_MONITORING = False  # Disable monitoring for tests
    
    @patch('psycopg2.pool.ThreadedConnectionPool')
    def test_pool_initialization(self, mock_pool):
        """Test pool initialization"""
        mock_pool_instance = Mock()
        mock_pool.return_value = mock_pool_instance
        
        pool = SecureDatabasePool(self.config)
        
        self.assertIsNotNone(pool.pool)
        mock_pool.assert_called_once()
    
    def test_pool_stats(self):
        """Test pool statistics"""
        with patch('psycopg2.pool.ThreadedConnectionPool'):
            pool = SecureDatabasePool(self.config)
            
            stats = pool.get_pool_stats()
            
            self.assertIn('pool_size', stats)
            self.assertIn('available_connections', stats)
            self.assertIn('in_use_connections', stats)
            self.assertIn('connection_stats', stats)
            self.assertIn('health_status', stats)
            self.assertIn('failed_checks', stats)

if __name__ == '__main__':
    # Run tests
    unittest.main(verbosity=2) 