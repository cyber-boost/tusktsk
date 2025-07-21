#!/usr/bin/env python3
"""
Simplified Operator Test Suite
==============================
Tests the actual functionality of operators without complex mocking
"""

import unittest
import os
import tempfile
from tsk_enhanced import TuskLangEnhanced

class TestSimpleOperators(unittest.TestCase):
    """Test basic operator functionality"""
    
    def setUp(self):
        """Set up test environment"""
        self.parser = TuskLangEnhanced()
        # Set up test environment variables
        os.environ['TEST_VAR'] = 'test_value'
        
    def test_001_variable_operator(self):
        """Test @variable operator"""
        result = self.parser.execute_operator('variable', '"test_var"')
        self.assertIsInstance(result, str)
        
    def test_002_env_operator(self):
        """Test @env operator"""
        result = self.parser.execute_operator('env', '"TEST_VAR"')
        self.assertEqual(result, 'test_value')
        
    def test_003_date_operator(self):
        """Test @date operator"""
        result = self.parser.execute_operator('date', '"%Y"')
        self.assertIsInstance(result, str)
        self.assertTrue(len(result) == 4)  # Year should be 4 digits
        
    def test_004_file_operator(self):
        """Test @file operator"""
        # Create a temporary file
        with tempfile.NamedTemporaryFile(mode='w', delete=False) as f:
            f.write('test content')
            temp_file = f.name
        
        try:
            # Test read operation
            result = self.parser.execute_operator('file', f'"read", "{temp_file}"')
            self.assertEqual(result, 'test content')
            
            # Test exists operation
            result = self.parser.execute_operator('file', f'"exists", "{temp_file}"')
            self.assertTrue(result)
            
        finally:
            # Clean up
            os.unlink(temp_file)
            
    def test_005_json_operator(self):
        """Test @json operator"""
        # Test parse operation
        result = self.parser.execute_operator('json', '"parse", \'{"key": "value"}\'')
        self.assertIsInstance(result, dict)
        self.assertEqual(result['key'], 'value')
        
        # Test stringify operation
        result = self.parser.execute_operator('json', '"stringify", \'{"key": "value"}\'')
        self.assertIsInstance(result, str)
        self.assertIn('"key"', result)
        
    def test_006_query_operator(self):
        """Test @query operator"""
        result = self.parser.execute_operator('query', '"SELECT * FROM test"')
        self.assertIsInstance(result, dict)
        
    def test_007_cache_operator(self):
        """Test @cache operator"""
        result = self.parser.execute_operator('cache', '30, "cached_value"')
        self.assertEqual(result, 'cached_value')
        
    def test_008_string_operator(self):
        """Test @string operator"""
        result = self.parser.execute_operator('string', '"uppercase", "hello"')
        self.assertEqual(result, 'HELLO')
        
        result = self.parser.execute_operator('string', '"lowercase", "WORLD"')
        self.assertEqual(result, 'world')
        
    def test_009_regex_operator(self):
        """Test @regex operator"""
        result = self.parser.execute_operator('regex', '"match", "hello world", "h.*o"')
        self.assertIsInstance(result, list)
        
    def test_010_hash_operator(self):
        """Test @hash operator"""
        result = self.parser.execute_operator('hash', '"md5", "test"')
        self.assertIsInstance(result, str)
        self.assertEqual(len(result), 32)  # MD5 hash is 32 characters
        
    def test_011_base64_operator(self):
        """Test @base64 operator"""
        result = self.parser.execute_operator('base64', '"encode", "test"')
        self.assertIsInstance(result, str)
        
        # Test decode
        result = self.parser.execute_operator('base64', '"decode", "dGVzdA=="')
        self.assertEqual(result, 'test')
        
    def test_012_if_operator(self):
        """Test @if operator"""
        result = self.parser.execute_operator('if', 'true, "yes", "no"')
        self.assertEqual(result, 'yes')
        
        result = self.parser.execute_operator('if', 'false, "yes", "no"')
        self.assertEqual(result, 'no')
        
    def test_013_switch_operator(self):
        """Test @switch operator"""
        result = self.parser.execute_operator('switch', '"test", "case1:result1;case2:result2", "default"')
        self.assertEqual(result, 'default')
        
        result = self.parser.execute_operator('switch', '"case1", "case1:result1;case2:result2", "default"')
        self.assertEqual(result, 'result1')
        
    def test_014_for_operator(self):
        """Test @for operator"""
        result = self.parser.execute_operator('for', '1, 3, "$i"')
        self.assertIsInstance(result, list)
        self.assertEqual(len(result), 3)
        
    def test_015_while_operator(self):
        """Test @while operator"""
        result = self.parser.execute_operator('while', 'false, "test"')
        self.assertIsInstance(result, list)
        
    def test_016_each_operator(self):
        """Test @each operator"""
        result = self.parser.execute_operator('each', '["a", "b"], "$item"')
        self.assertIsInstance(result, list)
        self.assertEqual(len(result), 2)
        
    def test_017_filter_operator(self):
        """Test @filter operator"""
        result = self.parser.execute_operator('filter', '["a", "b", "c"], "$item == \\"a\\""')
        self.assertIsInstance(result, list)
        self.assertEqual(len(result), 1)
        self.assertEqual(result[0], 'a')
        
    def test_018_encrypt_operator(self):
        """Test @encrypt operator"""
        result = self.parser.execute_operator('encrypt', '"aes", "test data"')
        self.assertIsInstance(result, str)
        self.assertTrue(result.startswith('encrypted_'))
        
    def test_019_decrypt_operator(self):
        """Test @decrypt operator"""
        result = self.parser.execute_operator('decrypt', '"aes", "encrypted_test data"')
        self.assertEqual(result, 'test data')
        
    def test_020_jwt_operator(self):
        """Test @jwt operator"""
        result = self.parser.execute_operator('jwt', '"encode", {"user": "test"}, "secret"')
        self.assertIsInstance(result, str)
        self.assertTrue(result.startswith('jwt_token_'))
        
    def test_021_email_operator(self):
        """Test @email operator"""
        result = self.parser.execute_operator('email', '"send", "test@example.com", "Hello"')
        self.assertIsInstance(result, str)
        self.assertIn('test@example.com', result)
        
    def test_022_sms_operator(self):
        """Test @sms operator"""
        result = self.parser.execute_operator('sms', '"send", "+1234567890", "Hello"')
        self.assertIsInstance(result, str)
        self.assertIn('+1234567890', result)
        
    def test_023_webhook_operator(self):
        """Test @webhook operator"""
        result = self.parser.execute_operator('webhook', '"post", "http://example.com/webhook", {"data": "test"}')
        self.assertIsInstance(result, str)
        self.assertIn('http://example.com/webhook', result)
        
    def test_024_etcd_operator(self):
        """Test @etcd operator"""
        result = self.parser.execute_operator('etcd', '"get", "test_key"')
        self.assertIsInstance(result, dict)
        self.assertIn('operation', result)
        
    def test_025_elasticsearch_operator(self):
        """Test @elasticsearch operator"""
        result = self.parser.execute_operator('elasticsearch', '"search", "test_index", "{}"')
        self.assertIsInstance(result, dict)
        self.assertIn('operation', result)
        
    def test_026_vault_operator(self):
        """Test @vault operator"""
        result = self.parser.execute_operator('vault', '"secret/path", "key"')
        self.assertIsInstance(result, dict)
        self.assertIn('secret_path', result)
        
    def test_027_temporal_operator(self):
        """Test @temporal operator"""
        result = self.parser.execute_operator('temporal', '"workflow", "operation"')
        self.assertIsInstance(result, dict)
        self.assertIn('workflow', result)
        
    def test_028_compliance_operator(self):
        """Test @compliance operator"""
        result = self.parser.execute_operator('compliance', '"gdpr", "compliant"')
        self.assertIsInstance(result, str)
        self.assertIn('gdpr', result)
        
    def test_029_governance_operator(self):
        """Test @governance operator"""
        result = self.parser.execute_operator('governance', '"data_retention", "30_days"')
        self.assertIsInstance(result, str)
        self.assertIn('data_retention', result)
        
    def test_030_policy_operator(self):
        """Test @policy operator"""
        result = self.parser.execute_operator('policy', '"access_control", "strict"')
        self.assertIsInstance(result, str)
        self.assertIn('access_control', result)
        
    def test_031_workflow_operator(self):
        """Test @workflow operator"""
        result = self.parser.execute_operator('workflow', '"approval", "step1"')
        self.assertIsInstance(result, str)
        self.assertIn('approval', result)
        
    def test_032_ai_operator(self):
        """Test @ai operator"""
        result = self.parser.execute_operator('ai', '"gpt", "Hello world"')
        self.assertIsInstance(result, str)
        self.assertIn('gpt', result)
        
    def test_033_blockchain_operator(self):
        """Test @blockchain operator"""
        result = self.parser.execute_operator('blockchain', '"ethereum", "transfer"')
        self.assertIsInstance(result, str)
        self.assertIn('ethereum', result)
        
    def test_034_iot_operator(self):
        """Test @iot operator"""
        result = self.parser.execute_operator('iot', '"sensor1", "read"')
        self.assertIsInstance(result, str)
        self.assertIn('sensor1', result)
        
    def test_035_edge_operator(self):
        """Test @edge operator"""
        result = self.parser.execute_operator('edge', '"node1", "process"')
        self.assertIsInstance(result, str)
        self.assertIn('node1', result)
        
    def test_036_quantum_operator(self):
        """Test @quantum operator"""
        result = self.parser.execute_operator('quantum', '"circuit1", "execute"')
        self.assertIsInstance(result, str)
        self.assertIn('circuit1', result)
        
    def test_037_neural_operator(self):
        """Test @neural operator"""
        result = self.parser.execute_operator('neural', '"network1", "train"')
        self.assertIsInstance(result, str)
        self.assertIn('network1', result)

def run_simple_tests():
    """Run the simplified test suite"""
    print("🧪 RUNNING SIMPLIFIED OPERATOR TESTS")
    print("=" * 50)
    
    # Create test suite
    suite = unittest.TestLoader().loadTestsFromTestCase(TestSimpleOperators)
    
    # Run tests
    runner = unittest.TextTestRunner(verbosity=2)
    result = runner.run(suite)
    
    print("=" * 50)
    print(f"📊 SIMPLIFIED TEST RESULTS:")
    print(f"✅ Tests run: {result.testsRun}")
    print(f"❌ Failures: {len(result.failures)}")
    print(f"⚠️  Errors: {len(result.errors)}")
    print(f"📈 Success rate: {((result.testsRun - len(result.failures) - len(result.errors)) / result.testsRun * 100):.1f}%")
    
    if result.wasSuccessful():
        print("\n🎉 ALL SIMPLIFIED TESTS PASSED!")
        return True
    else:
        print("\n⚠️  SOME TESTS FAILED")
        return False

if __name__ == '__main__':
    success = run_simple_tests()
    exit(0 if success else 1) 