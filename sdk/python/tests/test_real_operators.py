#!/usr/bin/env python3
"""
Test Real TuskLang Operators
============================
Test the actual functional implementations
"""

import unittest
import os
import tempfile
from unittest.mock import patch, MagicMock
from datetime import datetime

# Add the parent directory to the path
import sys
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '.'))

from real_operators import (
    RealSlackOperator, RealTeamsOperator, RealDiscordOperator,
    RealRBACOperator, RealAuditOperator, RealMonitoringOperator,
    get_real_operator, real_slack_send, real_teams_send, real_rbac_check, real_audit_log
)


class TestRealOperators(unittest.TestCase):
    """Test real operator implementations"""
    
    def setUp(self):
        """Set up test environment"""
        self.temp_dir = tempfile.mkdtemp()
        self.audit_log_file = os.path.join(self.temp_dir, "test_audit.log")
        
    def tearDown(self):
        """Clean up test environment"""
        import shutil
        shutil.rmtree(self.temp_dir, ignore_errors=True)
    
    def test_real_slack_operator(self):
        """Test real Slack operator"""
        print("\n=== Testing Real Slack Operator ===")
        
        slack = RealSlackOperator()
        
        # Test without token (should return error)
        result = slack.send_message("#test", "Hello from TuskLang")
        self.assertIn("error", result)
        self.assertIn("token not configured", result["error"])
        print(f"✓ Slack without token: {result['error']}")
        
        # Test with mock token
        with patch.dict(os.environ, {'SLACK_BOT_TOKEN': 'xoxb-test-token'}):
            slack_with_token = RealSlackOperator()
            
            with patch('requests.post') as mock_post:
                mock_response = MagicMock()
                mock_response.json.return_value = {"ok": True, "channel": "C123456", "ts": "1234567890.123456"}
                mock_post.return_value = mock_response
                
                result = slack_with_token.send_message("#test", "Hello from TuskLang")
                self.assertTrue(result["ok"])
                print(f"✓ Slack send message: {result}")
    
    def test_real_teams_operator(self):
        """Test real Teams operator"""
        print("\n=== Testing Real Teams Operator ===")
        
        teams = RealTeamsOperator()
        
        # Test without webhook (should return error)
        result = teams.send_message("Test Title", "Test Message")
        self.assertIn("error", result)
        self.assertIn("webhook URL not configured", result["error"])
        print(f"✓ Teams without webhook: {result['error']}")
        
        # Test with mock webhook
        with patch.dict(os.environ, {'TEAMS_WEBHOOK_URL': 'https://hooks.office.com/webhook/test'}):
            teams_with_webhook = RealTeamsOperator()
            
            with patch('requests.post') as mock_post:
                mock_response = MagicMock()
                mock_response.status_code = 200
                mock_response.text = "1"
                mock_post.return_value = mock_response
                
                result = teams_with_webhook.send_message("Test Title", "Test Message")
                self.assertEqual(result["status"], 200)
                print(f"✓ Teams send message: {result}")
    
    def test_real_discord_operator(self):
        """Test real Discord operator"""
        print("\n=== Testing Real Discord Operator ===")
        
        discord = RealDiscordOperator()
        
        # Test without webhook (should return error)
        result = discord.send_message("Test Message")
        self.assertIn("error", result)
        self.assertIn("webhook URL not configured", result["error"])
        print(f"✓ Discord without webhook: {result['error']}")
        
        # Test with mock webhook
        with patch.dict(os.environ, {'DISCORD_WEBHOOK_URL': 'https://discord.com/api/webhooks/test'}):
            discord_with_webhook = RealDiscordOperator()
            
            with patch('requests.post') as mock_post:
                mock_response = MagicMock()
                mock_response.status_code = 204
                mock_response.text = ""
                mock_post.return_value = mock_response
                
                result = discord_with_webhook.send_message("Test Message", "Test Bot")
                self.assertEqual(result["status"], 204)
                print(f"✓ Discord send message: {result}")
    
    def test_real_rbac_operator(self):
        """Test real RBAC operator"""
        print("\n=== Testing Real RBAC Operator ===")
        
        rbac = RealRBACOperator()
        
        # Test adding role
        result = rbac.add_role("admin", ["read", "write", "delete"])
        self.assertEqual(result["status"], "success")
        self.assertEqual(result["role"], "admin")
        self.assertEqual(result["permissions"], ["read", "write", "delete"])
        print(f"✓ RBAC add role: {result}")
        
        # Test assigning role
        result = rbac.assign_role("user123", "admin")
        self.assertEqual(result["status"], "success")
        self.assertEqual(result["user"], "user123")
        self.assertEqual(result["role"], "admin")
        print(f"✓ RBAC assign role: {result}")
        
        # Test checking permission (should have permission)
        result = rbac.check_permission("user123", "read")
        self.assertTrue(result["has_permission"])
        self.assertEqual(result["user"], "user123")
        self.assertEqual(result["role"], "admin")
        print(f"✓ RBAC check permission (allowed): {result}")
        
        # Test checking permission (should not have permission)
        result = rbac.check_permission("user123", "superuser")
        self.assertFalse(result["has_permission"])
        print(f"✓ RBAC check permission (denied): {result}")
        
        # Test user without role
        result = rbac.check_permission("user456", "read")
        self.assertFalse(result["has_permission"])
        self.assertIn("no role", result["reason"])
        print(f"✓ RBAC check permission (no role): {result}")
    
    def test_real_audit_operator(self):
        """Test real audit operator"""
        print("\n=== Testing Real Audit Operator ===")
        
        audit = RealAuditOperator(self.audit_log_file)
        
        # Test logging event
        result = audit.log_event("test_event", "user123", "test_action", {"key": "value"})
        self.assertEqual(result["status"], "logged")
        self.assertIsInstance(result["audit_id"], int)
        print(f"✓ Audit log event: {result}")
        
        # Test getting audit log
        result = audit.get_audit_log()
        self.assertIsInstance(result, list)
        self.assertGreater(len(result), 0)
        
        event = result[0]
        self.assertEqual(event["event_type"], "test_event")
        self.assertEqual(event["user_id"], "user123")
        self.assertEqual(event["action"], "test_action")
        self.assertEqual(event["details"], {"key": "value"})
        print(f"✓ Audit get log: {len(result)} events")
        
        # Test getting audit log for specific user
        result = audit.get_audit_log("user123")
        self.assertIsInstance(result, list)
        self.assertGreater(len(result), 0)
        for event in result:
            self.assertEqual(event["user_id"], "user123")
        print(f"✓ Audit get log for user: {len(result)} events")
    
    def test_real_monitoring_operator(self):
        """Test real monitoring operator"""
        print("\n=== Testing Real Monitoring Operator ===")
        
        monitoring = RealMonitoringOperator()
        
        # Test recording metric
        result = monitoring.record_metric("test_metric", 42.5, {"tag1": "value1"})
        self.assertEqual(result["status"], "recorded")
        self.assertEqual(result["metric"], "test_metric")
        self.assertEqual(result["value"], 42.5)
        print(f"✓ Monitoring record metric: {result}")
        
        # Test getting metric
        result = monitoring.get_metric("test_metric", 60)
        self.assertEqual(result["metric"], "test_metric")
        self.assertEqual(result["count"], 1)
        self.assertEqual(result["min"], 42.5)
        self.assertEqual(result["max"], 42.5)
        self.assertEqual(result["avg"], 42.5)
        self.assertEqual(result["latest"], 42.5)
        print(f"✓ Monitoring get metric: {result}")
        
        # Test adding health check
        def test_health_check():
            return {"status": "healthy", "timestamp": datetime.now().isoformat()}
        
        result = monitoring.add_health_check("test_service", test_health_check)
        self.assertEqual(result["status"], "added")
        self.assertEqual(result["health_check"], "test_service")
        print(f"✓ Monitoring add health check: {result}")
        
        # Test running health checks
        result = monitoring.run_health_checks()
        self.assertIn("test_service", result)
        self.assertEqual(result["test_service"]["status"], "healthy")
        print(f"✓ Monitoring run health checks: {result}")
        
        # Test creating alert
        result = monitoring.create_alert("test_alert", "Test alert message", "warning")
        self.assertEqual(result["status"], "created")
        self.assertEqual(result["alert"]["type"], "test_alert")
        self.assertEqual(result["alert"]["message"], "Test alert message")
        self.assertEqual(result["alert"]["severity"], "warning")
        self.assertFalse(result["alert"]["acknowledged"])
        print(f"✓ Monitoring create alert: {result}")
    
    def test_factory_function(self):
        """Test factory function"""
        print("\n=== Testing Factory Function ===")
        
        # Test getting Slack operator
        slack = get_real_operator("slack")
        self.assertIsInstance(slack, RealSlackOperator)
        print(f"✓ Factory Slack operator: {type(slack)}")
        
        # Test getting Teams operator
        teams = get_real_operator("teams")
        self.assertIsInstance(teams, RealTeamsOperator)
        print(f"✓ Factory Teams operator: {type(teams)}")
        
        # Test getting RBAC operator
        rbac = get_real_operator("rbac")
        self.assertIsInstance(rbac, RealRBACOperator)
        print(f"✓ Factory RBAC operator: {type(rbac)}")
        
        # Test getting audit operator
        audit = get_real_operator("audit", log_file=self.audit_log_file)
        self.assertIsInstance(audit, RealAuditOperator)
        print(f"✓ Factory audit operator: {type(audit)}")
        
        # Test getting monitoring operator
        monitoring = get_real_operator("monitoring")
        self.assertIsInstance(monitoring, RealMonitoringOperator)
        print(f"✓ Factory monitoring operator: {type(monitoring)}")
        
        # Test unknown operator
        with self.assertRaises(ValueError):
            get_real_operator("unknown")
        print(f"✓ Factory unknown operator: ValueError raised")
    
    def test_convenience_functions(self):
        """Test convenience functions"""
        print("\n=== Testing Convenience Functions ===")
        
        # Test real_slack_send (will fail without token, but should not crash)
        result = real_slack_send("#test", "Test message")
        self.assertIn("error", result)
        print(f"✓ Convenience slack send: {result['error']}")
        
        # Test real_teams_send (will fail without webhook, but should not crash)
        result = real_teams_send("Test Title", "Test Message")
        self.assertIn("error", result)
        print(f"✓ Convenience teams send: {result['error']}")
        
        # Test real_rbac_check
        result = real_rbac_check("user123", "read")
        self.assertFalse(result["has_permission"])
        self.assertIn("no role", result["reason"])
        print(f"✓ Convenience RBAC check: {result}")
        
        # Test real_audit_log
        result = real_audit_log("test_event", "user123", "test_action", {"key": "value"})
        self.assertEqual(result["status"], "logged")
        print(f"✓ Convenience audit log: {result}")


if __name__ == '__main__':
    unittest.main(verbosity=2) 