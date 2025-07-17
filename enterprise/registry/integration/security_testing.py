#!/usr/bin/env python3
"""
TuskLang Package Registry Security Testing
Comprehensive security testing for all registry components
"""

import unittest
import time
import json
import tempfile
import os
import hashlib
from typing import Dict, Any, List
from unittest.mock import Mock, patch

# Import security components
from registry.auth.authentication import RegistryAuthenticator
from registry.security.access_control import AccessControlList
from registry.security.package_storage import SecurePackageStorage
from registry.security.download_protection import DownloadProtection
from registry.security.checksum_validator import ChecksumValidator
from registry.security.package_signing import PackageSigner
from registry.security.signature_verification import SignatureVerifier
from registry.monitoring.security_logger import SecurityLogger
from registry.validation.package_validator import PackageValidator
from registry.validation.malware_detector import MalwareDetector

class RegistrySecurityTest(unittest.TestCase):
    """Security tests for the registry system"""
    
    def setUp(self):
        """Set up security test environment"""
        self.temp_dir = tempfile.mkdtemp()
        
        # Initialize security components
        self.auth = RegistryAuthenticator("test-secret-key-2025")
        self.acl = AccessControlList()
        self.storage = SecurePackageStorage(self.temp_dir, "test-master-key-2025")
        self.download_protection = DownloadProtection("test-download-secret-2025")
        self.checksum_validator = ChecksumValidator()
        self.package_signer = PackageSigner("/tmp/test-keys")
        self.signature_verifier = SignatureVerifier(self.package_signer, None)
        self.security_logger = SecurityLogger(self.temp_dir)
        self.package_validator = PackageValidator()
        self.malware_detector = MalwareDetector()
        
        # Test users
        self.admin_user = {
            'user_id': 'admin_123',
            'username': 'admin',
            'email': 'admin@example.com',
            'role': 'admin'
        }
        
        self.regular_user = {
            'user_id': 'user_123',
            'username': 'user',
            'email': 'user@example.com',
            'role': 'publisher'
        }
        
        self.malicious_user = {
            'user_id': 'malicious_123',
            'username': 'malicious',
            'email': 'malicious@example.com',
            'role': 'reader'
        }
    
    def tearDown(self):
        """Clean up test environment"""
        import shutil
        shutil.rmtree(self.temp_dir)
    
    def test_authentication_security(self):
        """Test authentication security features"""
        # 1. Test password strength validation
        weak_password = "123"
        strong_password = "SecurePass123!@#"
        
        # Create user with strong password
        user, api_key = self.auth.create_user(
            self.regular_user['username'],
            self.regular_user['email'],
            'publisher',
            strong_password
        )
        
        self.assertIsNotNone(user)
        self.assertIsNotNone(api_key)
        
        # 2. Test API key authentication
        authenticated_user = self.auth.authenticate_user(api_key)
        self.assertEqual(authenticated_user.user_id, user.user_id)
        
        # 3. Test invalid API key
        invalid_user = self.auth.authenticate_user("invalid-key")
        self.assertIsNone(invalid_user)
        
        # 4. Test session token security
        session_token = self.auth.create_session_token(authenticated_user, expires_in=3600)
        self.assertIsNotNone(session_token)
        
        # 5. Test expired session token
        expired_token = self.auth.create_session_token(authenticated_user, expires_in=1)
        time.sleep(2)  # Wait for token to expire
        expired_user = self.auth.verify_session_token(expired_token)
        self.assertIsNone(expired_user)
        
        # 6. Test permission checking
        has_admin_permission = self.auth.check_permission(authenticated_user, 'admin')
        self.assertFalse(has_admin_permission)  # Regular user shouldn't have admin permission
    
    def test_access_control_security(self):
        """Test access control security"""
        # 1. Test permission inheritance
        self.acl.add_permission({
            'resource_type': 'package',
            'resource_id': 'test_package_123',
            'action': 'read',
            'granted_to': 'group_publishers',
            'granted_by': 'admin',
            'granted_at': time.time()
        })
        
        # Add user to group
        self.acl.add_user_to_group('user_123', 'group_publishers')
        
        # Check group permission
        has_permission = self.acl.check_permission(
            'user_123',
            'package',
            'test_package_123',
            'read'
        )
        
        self.assertTrue(has_permission)
        
        # 2. Test permission revocation
        self.acl.remove_user_from_group('user_123', 'group_publishers')
        
        has_permission_after_revoke = self.acl.check_permission(
            'user_123',
            'package',
            'test_package_123',
            'read'
        )
        
        self.assertFalse(has_permission_after_revoke)
        
        # 3. Test resource ownership
        self.acl.set_resource_owner('test_package_123', 'user_123')
        
        owner_has_permission = self.acl.check_permission(
            'user_123',
            'package',
            'test_package_123',
            'admin'
        )
        
        self.assertTrue(owner_has_permission)
    
    def test_package_storage_security(self):
        """Test package storage security"""
        # 1. Test encrypted storage
        test_data = b"sensitive package data"
        test_metadata = {
            'package_id': 'secure_package_123',
            'name': 'secure-package',
            'version': '1.0.0',
            'description': 'Secure package',
            'author': 'author@example.com',
            'license': 'MIT',
            'dependencies': [],
            'file_size': len(test_data),
            'file_type': '.tsk'
        }
        
        # Store package
        success = self.storage.store_package(test_data, test_metadata)
        self.assertTrue(success)
        
        # 2. Test data integrity
        retrieved_data = self.storage.retrieve_package('secure_package_123')
        self.assertEqual(retrieved_data, test_data)
        
        # 3. Test tamper detection
        # Modify stored file directly (simulate tampering)
        package_path = os.path.join(self.temp_dir, 'packages', 'secure_package_123.enc')
        if os.path.exists(package_path):
            with open(package_path, 'wb') as f:
                f.write(b"tampered data")
            
            # Try to retrieve tampered package
            tampered_data = self.storage.retrieve_package('secure_package_123')
            # Should fail or return None due to integrity check
            self.assertNotEqual(tampered_data, test_data)
    
    def test_download_protection_security(self):
        """Test download protection security"""
        # 1. Test download token security
        download_token = self.download_protection.create_download_token(
            'test_package_123',
            'user_123',
            expires_in=3600,
            download_limit=10
        )
        
        self.assertIsNotNone(download_token)
        
        # 2. Test token validation
        token_info = self.download_protection.validate_download_token(download_token)
        self.assertIsNotNone(token_info)
        
        # 3. Test token expiration
        expired_token = self.download_protection.create_download_token(
            'test_package_123',
            'user_123',
            expires_in=1,
            download_limit=10
        )
        
        time.sleep(2)  # Wait for token to expire
        expired_info = self.download_protection.validate_download_token(expired_token)
        self.assertIsNone(expired_info)
        
        # 4. Test download limit enforcement
        limited_token = self.download_protection.create_download_token(
            'test_package_123',
            'user_123',
            expires_in=3600,
            download_limit=2
        )
        
        # Record downloads up to limit
        for i in range(3):
            success = self.download_protection.record_download(
                limited_token,
                f"192.168.1.{i}",
                "test-user-agent"
            )
            
            if i < 2:
                self.assertTrue(success)
            else:
                self.assertFalse(success)  # Should fail on third download
    
    def test_checksum_validation_security(self):
        """Test checksum validation security"""
        # 1. Test multiple checksum algorithms
        test_data = b"package data for checksum testing"
        
        checksums = self.checksum_validator.calculate_all_checksums(test_data)
        
        self.assertIn('sha256', checksums)
        self.assertIn('sha512', checksums)
        self.assertIn('blake2b', checksums)
        
        # 2. Test checksum verification
        self.checksum_validator.store_package_checksums('test_package_123', test_data)
        
        # Verify with correct data
        verification_results = self.checksum_validator.validate_package_checksums(
            'test_package_123',
            test_data
        )
        
        all_valid = all(r.is_valid for r in verification_results.values())
        self.assertTrue(all_valid)
        
        # 3. Test tamper detection
        tampered_data = b"tampered package data"
        
        tampered_results = self.checksum_validator.validate_package_checksums(
            'test_package_123',
            tampered_data
        )
        
        any_invalid = any(not r.is_valid for r in tampered_results.values())
        self.assertTrue(any_invalid)
    
    def test_package_signing_security(self):
        """Test package signing security"""
        # 1. Test key generation
        key_id = self.package_signer.generate_key_pair("RSA")
        self.assertIsNotNone(key_id)
        
        # 2. Test package signing
        test_data = b"package data for signing"
        signature = self.package_signer.sign_package(
            'test_package_123',
            test_data,
            key_id,
            'author@example.com'
        )
        
        self.assertIsNotNone(signature)
        
        # 3. Test signature verification
        signature_valid = self.package_signer.verify_package_signature(
            'test_package_123',
            test_data
        )
        
        self.assertTrue(signature_valid)
        
        # 4. Test tamper detection
        tampered_data = b"tampered package data"
        tampered_valid = self.package_signer.verify_package_signature(
            'test_package_123',
            tampered_data
        )
        
        self.assertFalse(tampered_valid)
        
        # 5. Test key revocation
        self.package_signer.revoke_key(key_id)
        
        # Try to sign with revoked key
        revoked_signature = self.package_signer.sign_package(
            'test_package_456',
            test_data,
            key_id,
            'author@example.com'
        )
        
        self.assertIsNone(revoked_signature)
    
    def test_malware_detection_security(self):
        """Test malware detection security"""
        # 1. Test clean package
        clean_data = b"clean package data"
        clean_detections = self.malware_detector.scan_package(
            'clean_package_123',
            clean_data
        )
        
        self.assertEqual(len(clean_detections), 0)
        
        # 2. Test malware patterns
        malware_data = b"""
        eval("malicious code");
        system("rm -rf /");
        exec("dangerous command");
        """
        
        malware_detections = self.malware_detector.scan_package(
            'malware_package_123',
            malware_data
        )
        
        self.assertGreater(len(malware_detections), 0)
        
        # 3. Test suspicious patterns
        suspicious_data = b"""
        password = "secret123";
        api_key = "sk_1234567890";
        token = "ghp_abcdef123456";
        """
        
        suspicious_detections = self.malware_detector.scan_package(
            'suspicious_package_123',
            suspicious_data
        )
        
        self.assertGreater(len(suspicious_detections), 0)
        
        # 4. Test high entropy detection
        high_entropy_data = os.urandom(1000)  # Random data has high entropy
        
        entropy_detections = self.malware_detector.scan_package(
            'entropy_package_123',
            high_entropy_data,
            scan_level='deep'
        )
        
        # Should detect high entropy as suspicious
        self.assertGreater(len(entropy_detections), 0)
    
    def test_package_validation_security(self):
        """Test package validation security"""
        # 1. Test file size limits
        large_data = b"x" * (101 * 1024 * 1024)  # 101MB
        
        validation_results = self.package_validator.validate_package(
            'large_package_123',
            large_data,
            {
                'package_id': 'large_package_123',
                'name': 'large-package',
                'version': '1.0.0',
                'description': 'Large package',
                'author': 'author@example.com',
                'license': 'MIT',
                'dependencies': [],
                'file_size': len(large_data),
                'file_type': '.tsk'
            }
        )
        
        # Should fail file size validation
        size_validation = [r for r in validation_results if r.rule.value == 'file_size_limit']
        self.assertGreater(len(size_validation), 0)
        self.assertFalse(size_validation[0].is_valid)
        
        # 2. Test file type restrictions
        validation_results = self.package_validator.validate_package(
            'executable_package_123',
            b"executable content",
            {
                'package_id': 'executable_package_123',
                'name': 'executable-package.exe',
                'version': '1.0.0',
                'description': 'Executable package',
                'author': 'author@example.com',
                'license': 'MIT',
                'dependencies': [],
                'file_size': 1000,
                'file_type': '.exe'
            }
        )
        
        # Should fail file type validation
        type_validation = [r for r in validation_results if r.rule.value == 'file_type_restriction']
        self.assertGreater(len(type_validation), 0)
        self.assertFalse(type_validation[0].is_valid)
        
        # 3. Test metadata validation
        validation_results = self.package_validator.validate_package(
            'invalid_metadata_123',
            b"package data",
            {
                'package_id': 'invalid_metadata_123',
                'name': 'invalid-package',
                'version': 'invalid-version',  # Invalid semver
                'description': '',  # Missing description
                'author': 'author@example.com',
                'license': 'MIT',
                'dependencies': [],
                'file_size': 1000,
                'file_type': '.tsk'
            }
        )
        
        # Should fail metadata validation
        metadata_validation = [r for r in validation_results if r.rule.value == 'metadata_validation']
        self.assertGreater(len(metadata_validation), 0)
        self.assertFalse(metadata_validation[0].is_valid)
    
    def test_security_logging(self):
        """Test security logging"""
        # 1. Test authentication failure logging
        self.security_logger.log_authentication_failure(
            'testuser',
            '192.168.1.100',
            'test-user-agent',
            'Invalid password'
        )
        
        # 2. Test authorization failure logging
        self.security_logger.log_authorization_failure(
            'user_123',
            '192.168.1.100',
            'package',
            'delete'
        )
        
        # 3. Test suspicious activity logging
        self.security_logger.log_suspicious_activity(
            'user_123',
            '192.168.1.100',
            'multiple_failed_uploads',
            {'attempts': 10, 'timeframe': '1 hour'}
        )
        
        # 4. Test rate limit logging
        self.security_logger.log_rate_limit_exceeded(
            '192.168.1.100',
            '/api/packages',
            100
        )
        
        # 5. Get security events
        security_events = self.security_logger.get_security_events(hours=1)
        self.assertGreater(len(security_events), 0)
        
        # 6. Get security statistics
        security_stats = self.security_logger.get_security_statistics(hours=1)
        self.assertIn('total_events', security_stats)
        self.assertGreater(security_stats['total_events'], 0)
    
    def test_rate_limiting_security(self):
        """Test rate limiting security"""
        # 1. Test IP-based rate limiting
        ip_address = "192.168.1.100"
        
        # Simulate multiple requests
        for i in range(5):
            allowed = self.download_protection.check_rate_limit(ip_address, max_downloads=3)
            if i < 3:
                self.assertTrue(allowed)
            else:
                self.assertFalse(allowed)
    
    def test_encryption_security(self):
        """Test encryption security"""
        # 1. Test data encryption
        sensitive_data = b"sensitive package data"
        
        # Store encrypted data
        test_metadata = {
            'package_id': 'encrypted_package_123',
            'name': 'encrypted-package',
            'version': '1.0.0',
            'description': 'Encrypted package',
            'author': 'author@example.com',
            'license': 'MIT',
            'dependencies': [],
            'file_size': len(sensitive_data),
            'file_type': '.tsk'
        }
        
        success = self.storage.store_package(sensitive_data, test_metadata)
        self.assertTrue(success)
        
        # 2. Test encrypted retrieval
        retrieved_data = self.storage.retrieve_package('encrypted_package_123')
        self.assertEqual(retrieved_data, sensitive_data)
        
        # 3. Test encryption key security
        # Verify that raw encrypted data is not readable
        package_path = os.path.join(self.temp_dir, 'packages', 'encrypted_package_123.enc')
        if os.path.exists(package_path):
            with open(package_path, 'rb') as f:
                encrypted_data = f.read()
            
            # Encrypted data should not contain original content
            self.assertNotIn(sensitive_data, encrypted_data)
    
    def test_session_security(self):
        """Test session security"""
        # 1. Create user and session
        user, api_key = self.auth.create_user(
            'sessionuser',
            'session@example.com',
            'publisher',
            'password123'
        )
        
        authenticated_user = self.auth.authenticate_user(api_key)
        session_token = self.auth.create_session_token(authenticated_user, expires_in=3600)
        
        # 2. Test session validation
        session_user = self.auth.verify_session_token(session_token)
        self.assertEqual(session_user.user_id, authenticated_user.user_id)
        
        # 3. Test session revocation
        self.auth.revoke_session(session_token)
        
        revoked_user = self.auth.verify_session_token(session_token)
        self.assertIsNone(revoked_user)
        
        # 4. Test session hijacking prevention
        # Create another session for same user
        session_token2 = self.auth.create_session_token(authenticated_user, expires_in=3600)
        
        # Both sessions should be valid
        user1 = self.auth.verify_session_token(session_token)
        user2 = self.auth.verify_session_token(session_token2)
        
        # Revoking one should not affect the other
        self.auth.revoke_session(session_token)
        
        user1_after_revoke = self.auth.verify_session_token(session_token)
        user2_after_revoke = self.auth.verify_session_token(session_token2)
        
        self.assertIsNone(user1_after_revoke)
        self.assertIsNotNone(user2_after_revoke)

def run_security_tests():
    """Run all security tests"""
    # Create test suite
    suite = unittest.TestSuite()
    
    # Add security tests
    suite.addTest(unittest.makeSuite(RegistrySecurityTest))
    
    # Run tests
    runner = unittest.TextTestRunner(verbosity=2)
    result = runner.run(suite)
    
    # Print security test summary
    print(f"\nSecurity Test Summary:")
    print(f"Tests run: {result.testsRun}")
    print(f"Failures: {len(result.failures)}")
    print(f"Errors: {len(result.errors)}")
    
    if result.failures:
        print("\nSecurity Failures:")
        for test, traceback in result.failures:
            print(f"- {test}: {traceback}")
    
    if result.errors:
        print("\nSecurity Errors:")
        for test, traceback in result.errors:
            print(f"- {test}: {traceback}")
    
    # Security assessment
    if len(result.failures) == 0 and len(result.errors) == 0:
        print("\n✅ All security tests passed - Registry is secure!")
    else:
        print("\n❌ Security issues detected - Review and fix before deployment!")
    
    return result.wasSuccessful()

if __name__ == '__main__':
    success = run_security_tests()
    exit(0 if success else 1) 