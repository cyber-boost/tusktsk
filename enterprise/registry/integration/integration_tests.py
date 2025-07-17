#!/usr/bin/env python3
"""
TuskLang Package Registry Integration Tests
Comprehensive integration testing for all registry components
"""

import unittest
import time
import json
import tempfile
import os
from typing import Dict, Any
import requests
from unittest.mock import Mock, patch

# Import registry components
from registry.auth.authentication import RegistryAuthenticator
from registry.security.access_control import AccessControlList
from registry.security.package_storage import SecurePackageStorage
from registry.security.download_protection import DownloadProtection
from registry.security.checksum_validator import ChecksumValidator
from registry.security.package_signing import PackageSigner
from registry.security.signature_verification import SignatureVerifier
from registry.monitoring.usage_monitor import RegistryUsageMonitor
from registry.monitoring.security_logger import SecurityLogger
from registry.validation.package_validator import PackageValidator
from registry.validation.malware_detector import MalwareDetector
from registry.backup.backup_manager import BackupManager
from registry.backup.recovery_manager import DisasterRecoveryManager
from registry.performance.cache_manager import RegistryCacheManager
from registry.performance.load_balancer import LoadBalancer

class RegistryIntegrationTest(unittest.TestCase):
    """Integration tests for the complete registry system"""
    
    def setUp(self):
        """Set up test environment"""
        self.temp_dir = tempfile.mkdtemp()
        
        # Initialize all components
        self.auth = RegistryAuthenticator("test-secret-key")
        self.acl = AccessControlList()
        self.storage = SecurePackageStorage(self.temp_dir, "test-master-key")
        self.download_protection = DownloadProtection("test-download-secret")
        self.checksum_validator = ChecksumValidator()
        self.package_signer = PackageSigner("/tmp/test-keys")
        self.signature_verifier = SignatureVerifier(self.package_signer, None)
        self.usage_monitor = RegistryUsageMonitor(self.temp_dir)
        self.security_logger = SecurityLogger(self.temp_dir)
        self.package_validator = PackageValidator()
        self.malware_detector = MalwareDetector()
        self.backup_manager = BackupManager(self.temp_dir, self.temp_dir)
        self.recovery_manager = DisasterRecoveryManager(self.backup_manager, self.temp_dir)
        self.cache_manager = RegistryCacheManager(self.temp_dir)
        self.load_balancer = LoadBalancer()
        
        # Test data
        self.test_user = {
            'user_id': 'test_user_123',
            'username': 'testuser',
            'email': 'test@example.com',
            'auth_level': 'publisher'
        }
        
        self.test_package_data = b"test package content"
        self.test_package_metadata = {
            'package_id': 'test_package_123',
            'name': 'test-package',
            'version': '1.0.0',
            'description': 'Test package',
            'author': 'test@example.com',
            'license': 'MIT',
            'dependencies': [],
            'file_size': len(self.test_package_data),
            'file_type': '.tsk'
        }
    
    def tearDown(self):
        """Clean up test environment"""
        import shutil
        shutil.rmtree(self.temp_dir)
    
    def test_complete_package_lifecycle(self):
        """Test complete package lifecycle from upload to download"""
        # 1. Create user and authenticate
        user, api_key = self.auth.create_user(
            self.test_user['username'],
            self.test_user['email'],
            'publisher',
            'password123'
        )
        
        self.assertIsNotNone(user)
        self.assertIsNotNone(api_key)
        
        # 2. Authenticate user
        authenticated_user = self.auth.authenticate_user(api_key)
        self.assertEqual(authenticated_user.user_id, user.user_id)
        
        # 3. Create session token
        session_token = self.auth.create_session_token(authenticated_user)
        self.assertIsNotNone(session_token)
        
        # 4. Validate package
        validation_results = self.package_validator.validate_package(
            self.test_package_metadata['package_id'],
            self.test_package_data,
            self.test_package_metadata
        )
        
        # Check that validation passed
        passed_validations = [r for r in validation_results if r.status.value == 'passed']
        self.assertGreater(len(passed_validations), 0)
        
        # 5. Scan for malware
        malware_detections = self.malware_detector.scan_package(
            self.test_package_metadata['package_id'],
            self.test_package_data
        )
        
        # Should not detect malware in test data
        self.assertEqual(len(malware_detections), 0)
        
        # 6. Calculate checksums
        checksums = self.checksum_validator.calculate_all_checksums(self.test_package_data)
        self.assertIn('sha256', checksums)
        self.assertIn('sha512', checksums)
        
        # 7. Store package integrity
        self.checksum_validator.store_package_integrity(
            self.test_package_metadata['package_id'],
            self.test_package_data,
            "test-signature",
            self.test_user['email']
        )
        
        # 8. Sign package
        key_id = self.package_signer.generate_key_pair("RSA")
        signature = self.package_signer.sign_package(
            self.test_package_metadata['package_id'],
            self.test_package_data,
            key_id,
            self.test_user['email']
        )
        
        self.assertIsNotNone(signature)
        
        # 9. Store package securely
        success = self.storage.store_package(
            self.test_package_data,
            self.test_package_metadata
        )
        
        self.assertTrue(success)
        
        # 10. Create download token
        download_token = self.download_protection.create_download_token(
            self.test_package_metadata['package_id'],
            user.user_id
        )
        
        self.assertIsNotNone(download_token)
        
        # 11. Validate download token
        token_info = self.download_protection.validate_download_token(download_token)
        self.assertIsNotNone(token_info)
        self.assertEqual(token_info.package_id, self.test_package_metadata['package_id'])
        
        # 12. Record download
        download_success = self.download_protection.record_download(
            token_info.token_id,
            "192.168.1.100",
            "test-user-agent"
        )
        
        self.assertTrue(download_success)
        
        # 13. Retrieve package
        retrieved_data = self.storage.retrieve_package(self.test_package_metadata['package_id'])
        self.assertEqual(retrieved_data, self.test_package_data)
        
        # 14. Verify package integrity
        integrity_valid = self.checksum_validator.verify_package_integrity(
            self.test_package_metadata['package_id'],
            retrieved_data
        )
        
        self.assertTrue(integrity_valid)
        
        # 15. Verify signature
        signature_valid = self.package_signer.verify_package_signature(
            self.test_package_metadata['package_id'],
            retrieved_data
        )
        
        self.assertTrue(signature_valid)
        
        # 16. Cache package
        self.cache_manager.set_package_data(
            self.test_package_metadata['package_id'],
            retrieved_data
        )
        
        # 17. Retrieve from cache
        cached_data = self.cache_manager.get_package_data(self.test_package_metadata['package_id'])
        self.assertEqual(cached_data, retrieved_data)
        
        # 18. Record usage events
        self.usage_monitor.record_event(
            'package_upload',
            user_id=user.user_id,
            package_id=self.test_package_metadata['package_id'],
            details={'size': len(self.test_package_data)}
        )
        
        self.usage_monitor.record_event(
            'package_download',
            user_id=user.user_id,
            package_id=self.test_package_metadata['package_id']
        )
        
        # 19. Create backup
        backup_id = self.backup_manager.create_full_backup("Integration test backup")
        self.assertIsNotNone(backup_id)
        
        # 20. Verify backup
        backup_info = self.backup_manager.get_backup_info(backup_id)
        self.assertEqual(backup_info.status.value, 'completed')
    
    def test_security_integration(self):
        """Test security features integration"""
        # 1. Test access control
        self.acl.add_permission({
            'resource_type': 'package',
            'resource_id': 'test_package_123',
            'action': 'read',
            'granted_to': 'test_user_123',
            'granted_by': 'admin',
            'granted_at': time.time()
        })
        
        # 2. Test permission checking
        has_permission = self.acl.check_permission(
            'test_user_123',
            'package',
            'test_package_123',
            'read'
        )
        
        self.assertTrue(has_permission)
        
        # 3. Test security logging
        self.security_logger.log_authentication_failure(
            'testuser',
            '192.168.1.100',
            'test-user-agent',
            'Invalid password'
        )
        
        # 4. Test rate limiting
        self.security_logger.log_rate_limit_exceeded(
            '192.168.1.100',
            '/api/packages',
            100
        )
        
        # 5. Get security events
        security_events = self.security_logger.get_security_events(hours=1)
        self.assertGreater(len(security_events), 0)
    
    def test_performance_integration(self):
        """Test performance features integration"""
        # 1. Test load balancer
        self.load_balancer.add_server('server1', '192.168.1.10', 8080)
        self.load_balancer.add_server('server2', '192.168.1.11', 8080)
        
        # 2. Get server
        server = self.load_balancer.get_server()
        self.assertIsNotNone(server)
        
        # 3. Record connection
        self.load_balancer.record_connection(server.server_id)
        
        # 4. Record response time
        self.load_balancer.record_response_time(server.server_id, 150.0)
        
        # 5. Get stats
        stats = self.load_balancer.get_server_stats()
        self.assertIn('total_servers', stats)
        self.assertEqual(stats['total_servers'], 2)
    
    def test_monitoring_integration(self):
        """Test monitoring features integration"""
        # 1. Record various events
        self.usage_monitor.record_event('user_login', user_id='user1')
        self.usage_monitor.record_event('package_upload', user_id='user1', package_id='pkg1')
        self.usage_monitor.record_event('package_download', user_id='user2', package_id='pkg1')
        self.usage_monitor.record_event('api_call', user_id='user1')
        
        # 2. Get usage metrics
        metrics = self.usage_monitor.get_usage_metrics(
            time.time() - 3600,
            time.time()
        )
        
        self.assertGreater(metrics.total_events, 0)
        self.assertGreater(metrics.unique_users, 0)
        
        # 3. Get real-time stats
        stats = self.usage_monitor.get_real_time_stats()
        self.assertIn('total_events', stats)
    
    def test_backup_recovery_integration(self):
        """Test backup and recovery integration"""
        # 1. Create test data
        test_data = b"test backup data"
        
        # 2. Create backup
        backup_id = self.backup_manager.create_full_backup("Test backup")
        
        # 3. Verify backup exists
        backup_info = self.backup_manager.get_backup_info(backup_id)
        self.assertIsNotNone(backup_info)
        self.assertEqual(backup_info.status.value, 'completed')
        
        # 4. Test recovery plan execution
        recovery_plans = self.recovery_manager.get_recovery_plans()
        self.assertGreater(len(recovery_plans), 0)
        
        # 5. Execute recovery plan
        execution_id = self.recovery_manager.execute_recovery_plan(
            'data_recovery',
            backup_id
        )
        
        self.assertIsNotNone(execution_id)
        
        # 6. Check execution status
        execution = self.recovery_manager.get_execution_status(execution_id)
        self.assertIsNotNone(execution)
    
    def test_cache_integration(self):
        """Test caching system integration"""
        # 1. Test metadata caching
        metadata = {'name': 'test-package', 'version': '1.0.0'}
        self.cache_manager.set_package_metadata('test_pkg_123', metadata)
        
        cached_metadata = self.cache_manager.get_package_metadata('test_pkg_123')
        self.assertEqual(cached_metadata, metadata)
        
        # 2. Test data caching
        test_data = b"test package data"
        self.cache_manager.set_package_data('test_pkg_123', test_data)
        
        cached_data = self.cache_manager.get_package_data('test_pkg_123')
        self.assertEqual(cached_data, test_data)
        
        # 3. Test cache invalidation
        self.cache_manager.invalidate_package_cache('test_pkg_123')
        
        invalidated_metadata = self.cache_manager.get_package_metadata('test_pkg_123')
        self.assertIsNone(invalidated_metadata)
        
        # 4. Get cache stats
        stats = self.cache_manager.get_cache_stats()
        self.assertIn('memory_cache', stats)
        self.assertIn('disk_cache', stats)
    
    def test_error_handling(self):
        """Test error handling and edge cases"""
        # 1. Test invalid authentication
        invalid_user = self.auth.authenticate_user("invalid-key")
        self.assertIsNone(invalid_user)
        
        # 2. Test invalid package retrieval
        invalid_data = self.storage.retrieve_package("non-existent-package")
        self.assertIsNone(invalid_data)
        
        # 3. Test invalid download token
        invalid_token = self.download_protection.validate_download_token("invalid-token")
        self.assertIsNone(invalid_token)
        
        # 4. Test cache miss
        cached_data = self.cache_manager.get_package_data("non-existent-package")
        self.assertIsNone(cached_data)
        
        # 5. Test backup of non-existent data
        backup_id = self.backup_manager.create_full_backup("Empty backup")
        self.assertIsNotNone(backup_id)

class RegistryAPIIntegrationTest(unittest.TestCase):
    """API integration tests"""
    
    def setUp(self):
        """Set up API test environment"""
        self.base_url = "http://localhost:8000/api/v1"
        self.test_user = {
            'username': 'testuser',
            'password': 'testpass123'
        }
        self.auth_token = None
    
    def test_api_authentication(self):
        """Test API authentication flow"""
        # 1. Get authentication token
        response = requests.post(
            f"{self.base_url}/auth/token",
            json=self.test_user
        )
        
        if response.status_code == 200:
            data = response.json()
            self.auth_token = data['access_token']
            self.assertIsNotNone(self.auth_token)
        else:
            self.skipTest("API server not available")
    
    def test_api_package_operations(self):
        """Test API package operations"""
        if not self.auth_token:
            self.skipTest("Authentication required")
        
        headers = {'Authorization': f'Bearer {self.auth_token}'}
        
        # 1. List packages
        response = requests.get(f"{self.base_url}/packages", headers=headers)
        if response.status_code == 200:
            data = response.json()
            self.assertIn('packages', data)
        
        # 2. Upload package (mock)
        with patch('requests.post') as mock_post:
            mock_post.return_value.status_code = 200
            mock_post.return_value.json.return_value = {'id': 'test_pkg_123'}
            
            response = requests.post(
                f"{self.base_url}/packages",
                headers=headers,
                files={'package': ('test.tsk', b'test data')}
            )
            
            if response.status_code == 200:
                data = response.json()
                self.assertIn('id', data)
    
    def test_api_security_endpoints(self):
        """Test API security endpoints"""
        if not self.auth_token:
            self.skipTest("Authentication required")
        
        headers = {'Authorization': f'Bearer {self.auth_token}'}
        
        # 1. Get security events
        response = requests.get(f"{self.base_url}/security/events", headers=headers)
        if response.status_code == 200:
            data = response.json()
            self.assertIn('events', data)
        
        # 2. Get security statistics
        response = requests.get(f"{self.base_url}/security/stats", headers=headers)
        if response.status_code == 200:
            data = response.json()
            self.assertIn('total_events', data)
    
    def test_api_monitoring_endpoints(self):
        """Test API monitoring endpoints"""
        if not self.auth_token:
            self.skipTest("Authentication required")
        
        headers = {'Authorization': f'Bearer {self.auth_token}'}
        
        # 1. Get usage statistics
        response = requests.get(f"{self.base_url}/monitoring/stats", headers=headers)
        if response.status_code == 200:
            data = response.json()
            self.assertIn('total_events', data)
        
        # 2. Get top packages
        response = requests.get(f"{self.base_url}/monitoring/top-packages", headers=headers)
        if response.status_code == 200:
            data = response.json()
            self.assertIn('packages', data)

def run_integration_tests():
    """Run all integration tests"""
    # Create test suite
    suite = unittest.TestSuite()
    
    # Add integration tests
    suite.addTest(unittest.makeSuite(RegistryIntegrationTest))
    suite.addTest(unittest.makeSuite(RegistryAPIIntegrationTest))
    
    # Run tests
    runner = unittest.TextTestRunner(verbosity=2)
    result = runner.run(suite)
    
    # Print summary
    print(f"\nIntegration Test Summary:")
    print(f"Tests run: {result.testsRun}")
    print(f"Failures: {len(result.failures)}")
    print(f"Errors: {len(result.errors)}")
    
    if result.failures:
        print("\nFailures:")
        for test, traceback in result.failures:
            print(f"- {test}: {traceback}")
    
    if result.errors:
        print("\nErrors:")
        for test, traceback in result.errors:
            print(f"- {test}: {traceback}")
    
    return result.wasSuccessful()

if __name__ == '__main__':
    success = run_integration_tests()
    exit(0 if success else 1) 