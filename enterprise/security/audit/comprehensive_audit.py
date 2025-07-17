#!/usr/bin/env python3
"""
TuskLang FORTRESS - Comprehensive Security Audit System
Agent A4 - Security & Compliance Expert

This module provides comprehensive security auditing capabilities for the TuskLang FORTRESS system.
"""

import os
import sys
import json
import hashlib
import subprocess
import logging
from datetime import datetime
from typing import Dict, List, Any, Optional
from pathlib import Path

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('security/audit/audit.log'),
        logging.StreamHandler()
    ]
)

class SecurityAuditor:
    """Comprehensive security auditor for TuskLang FORTRESS system."""
    
    def __init__(self, project_root: str = "."):
        self.project_root = Path(project_root)
        self.audit_results = {
            "timestamp": datetime.now().isoformat(),
            "auditor": "Agent A4 - Security & Compliance Expert",
            "project": "TuskLang FORTRESS",
            "version": "1.0.0",
            "findings": [],
            "risk_levels": {
                "critical": [],
                "high": [],
                "medium": [],
                "low": [],
                "info": []
            },
            "compliance": {
                "gdpr": {"status": "pending", "findings": []},
                "soc2": {"status": "pending", "findings": []},
                "iso27001": {"status": "pending", "findings": []},
                "pci_dss": {"status": "pending", "findings": []}
            },
            "summary": {
                "total_findings": 0,
                "critical_count": 0,
                "high_count": 0,
                "medium_count": 0,
                "low_count": 0,
                "info_count": 0
            }
        }
        
    def run_comprehensive_audit(self) -> Dict[str, Any]:
        """Execute comprehensive security audit."""
        logging.info("Starting comprehensive security audit for TuskLang FORTRESS")
        
        # Core security checks
        self._audit_file_permissions()
        self._audit_secrets_and_keys()
        self._audit_dependencies()
        self._audit_code_security()
        self._audit_network_security()
        self._audit_database_security()
        self._audit_authentication_systems()
        self._audit_authorization_controls()
        self._audit_data_protection()
        self._audit_logging_and_monitoring()
        self._audit_incident_response()
        self._audit_compliance_frameworks()
        
        # Generate summary
        self._generate_audit_summary()
        
        # Save results
        self._save_audit_results()
        
        logging.info("Comprehensive security audit completed")
        return self.audit_results
    
    def _audit_file_permissions(self):
        """Audit file and directory permissions."""
        logging.info("Auditing file permissions")
        
        critical_files = [
            ".env", ".pem", ".key", ".crt", ".p12", ".pfx",
            "config.json", "secrets.json", "credentials.json"
        ]
        
        for file_path in self.project_root.rglob("*"):
            if file_path.is_file():
                # Check for sensitive files
                if any(sensitive in file_path.name.lower() for sensitive in critical_files):
                    self._add_finding(
                        "critical",
                        "Sensitive file found",
                        f"Sensitive file detected: {file_path}",
                        "Remove or secure sensitive files",
                        "File permissions"
                    )
                
                # Check file permissions
                stat = file_path.stat()
                if stat.st_mode & 0o777 == 0o777:
                    self._add_finding(
                        "high",
                        "Overly permissive file",
                        f"File has 777 permissions: {file_path}",
                        "Restrict file permissions to minimum required",
                        "File permissions"
                    )
    
    def _audit_secrets_and_keys(self):
        """Audit for hardcoded secrets and keys."""
        logging.info("Auditing for hardcoded secrets and keys")
        
        secret_patterns = [
            r'password\s*=\s*["\'][^"\']+["\']',
            r'secret\s*=\s*["\'][^"\']+["\']',
            r'key\s*=\s*["\'][^"\']+["\']',
            r'token\s*=\s*["\'][^"\']+["\']',
            r'api_key\s*=\s*["\'][^"\']+["\']',
            r'private_key\s*=\s*["\'][^"\']+["\']',
            r'-----BEGIN PRIVATE KEY-----',
            r'-----BEGIN RSA PRIVATE KEY-----',
            r'-----BEGIN DSA PRIVATE KEY-----',
            r'-----BEGIN EC PRIVATE KEY-----'
        ]
        
        code_extensions = ['.py', '.js', '.ts', '.php', '.java', '.go', '.rs', '.sh', '.bash']
        
        for file_path in self.project_root.rglob("*"):
            if file_path.is_file() and file_path.suffix in code_extensions:
                try:
                    with open(file_path, 'r', encoding='utf-8') as f:
                        content = f.read()
                        for pattern in secret_patterns:
                            if pattern in content:
                                self._add_finding(
                                    "critical",
                                    "Hardcoded secret detected",
                                    f"Potential secret found in {file_path}",
                                    "Move secrets to secure configuration management",
                                    "Secrets management"
                                )
                except Exception as e:
                    logging.warning(f"Could not read {file_path}: {e}")
    
    def _audit_dependencies(self):
        """Audit dependencies for known vulnerabilities."""
        logging.info("Auditing dependencies for vulnerabilities")
        
        # Check for dependency files
        dependency_files = ['requirements.txt', 'package.json', 'Cargo.toml', 'go.mod', 'pom.xml']
        
        for dep_file in dependency_files:
            file_path = self.project_root / dep_file
            if file_path.exists():
                self._add_finding(
                    "medium",
                    "Dependency audit required",
                    f"Dependency file found: {dep_file}",
                    "Run automated dependency vulnerability scanning",
                    "Dependency security"
                )
    
    def _audit_code_security(self):
        """Audit code for security vulnerabilities."""
        logging.info("Auditing code for security vulnerabilities")
        
        # Common security anti-patterns
        security_anti_patterns = [
            (r'eval\s*\(', "Use of eval() function"),
            (r'exec\s*\(', "Use of exec() function"),
            (r'os\.system\s*\(', "Use of os.system()"),
            (r'subprocess\.call\s*\(', "Use of subprocess.call()"),
            (r'innerHTML\s*=', "Direct innerHTML assignment"),
            (r'document\.write\s*\(', "Use of document.write()"),
            (r'SQL.*\+.*user_input', "SQL injection vulnerability"),
            (r'password.*==.*["\']', "Hardcoded password comparison")
        ]
        
        code_extensions = ['.py', '.js', '.ts', '.php', '.java', '.go', '.rs']
        
        for file_path in self.project_root.rglob("*"):
            if file_path.is_file() and file_path.suffix in code_extensions:
                try:
                    with open(file_path, 'r', encoding='utf-8') as f:
                        content = f.read()
                        for pattern, description in security_anti_patterns:
                            if pattern in content:
                                self._add_finding(
                                    "high",
                                    "Security anti-pattern detected",
                                    f"{description} in {file_path}",
                                    "Review and refactor to use secure alternatives",
                                    "Code security"
                                )
                except Exception as e:
                    logging.warning(f"Could not read {file_path}: {e}")
    
    def _audit_network_security(self):
        """Audit network security configurations."""
        logging.info("Auditing network security")
        
        # Check for network configuration files
        network_files = ['nginx.conf', 'apache.conf', '.htaccess', 'firewall.conf']
        
        for net_file in network_files:
            for file_path in self.project_root.rglob(net_file):
                self._add_finding(
                    "medium",
                    "Network configuration found",
                    f"Network config file: {file_path}",
                    "Review network security configurations",
                    "Network security"
                )
    
    def _audit_database_security(self):
        """Audit database security configurations."""
        logging.info("Auditing database security")
        
        # Check for database files and configurations
        db_patterns = [
            r'database\.py',
            r'db_config',
            r'connection\.py',
            r'\.sql$',
            r'\.db$'
        ]
        
        for file_path in self.project_root.rglob("*"):
            if file_path.is_file():
                for pattern in db_patterns:
                    if pattern in str(file_path):
                        self._add_finding(
                            "medium",
                            "Database configuration found",
                            f"Database file: {file_path}",
                            "Review database security and access controls",
                            "Database security"
                        )
    
    def _audit_authentication_systems(self):
        """Audit authentication systems."""
        logging.info("Auditing authentication systems")
        
        auth_patterns = [
            r'auth\.py',
            r'login\.py',
            r'authentication',
            r'session\.py',
            r'jwt',
            r'oauth'
        ]
        
        for file_path in self.project_root.rglob("*"):
            if file_path.is_file():
                for pattern in auth_patterns:
                    if pattern in str(file_path):
                        self._add_finding(
                            "high",
                            "Authentication system found",
                            f"Auth file: {file_path}",
                            "Review authentication security and session management",
                            "Authentication security"
                        )
    
    def _audit_authorization_controls(self):
        """Audit authorization and access controls."""
        logging.info("Auditing authorization controls")
        
        authz_patterns = [
            r'permission',
            r'authorization',
            r'access_control',
            r'rbac',
            r'role'
        ]
        
        for file_path in self.project_root.rglob("*"):
            if file_path.is_file():
                for pattern in authz_patterns:
                    if pattern in str(file_path):
                        self._add_finding(
                            "medium",
                            "Authorization system found",
                            f"Authz file: {file_path}",
                            "Review authorization controls and access policies",
                            "Authorization controls"
                        )
    
    def _audit_data_protection(self):
        """Audit data protection measures."""
        logging.info("Auditing data protection")
        
        # Check for data protection implementations
        protection_patterns = [
            r'encryption',
            r'hash',
            r'bcrypt',
            r'pbkdf2',
            r'argon2',
            r'gpg',
            r'pki'
        ]
        
        for file_path in self.project_root.rglob("*"):
            if file_path.is_file():
                for pattern in protection_patterns:
                    if pattern in str(file_path):
                        self._add_finding(
                            "info",
                            "Data protection mechanism found",
                            f"Protection file: {file_path}",
                            "Verify data protection implementation",
                            "Data protection"
                        )
    
    def _audit_logging_and_monitoring(self):
        """Audit logging and monitoring systems."""
        logging.info("Auditing logging and monitoring")
        
        logging_patterns = [
            r'log',
            r'logger',
            r'logging',
            r'monitor',
            r'audit'
        ]
        
        for file_path in self.project_root.rglob("*"):
            if file_path.is_file():
                for pattern in logging_patterns:
                    if pattern in str(file_path):
                        self._add_finding(
                            "info",
                            "Logging/monitoring found",
                            f"Logging file: {file_path}",
                            "Review logging and monitoring implementation",
                            "Logging and monitoring"
                        )
    
    def _audit_incident_response(self):
        """Audit incident response capabilities."""
        logging.info("Auditing incident response")
        
        incident_patterns = [
            r'incident',
            r'response',
            r'alert',
            r'notification',
            r'emergency'
        ]
        
        for file_path in self.project_root.rglob("*"):
            if file_path.is_file():
                for pattern in incident_patterns:
                    if pattern in str(file_path):
                        self._add_finding(
                            "info",
                            "Incident response found",
                            f"Incident file: {file_path}",
                            "Review incident response procedures",
                            "Incident response"
                        )
    
    def _audit_compliance_frameworks(self):
        """Audit compliance with security frameworks."""
        logging.info("Auditing compliance frameworks")
        
        # GDPR compliance checks
        gdpr_patterns = [
            r'gdpr',
            r'privacy',
            r'consent',
            r'data_protection',
            r'personal_data'
        ]
        
        for file_path in self.project_root.rglob("*"):
            if file_path.is_file():
                for pattern in gdpr_patterns:
                    if pattern in str(file_path):
                        self.audit_results["compliance"]["gdpr"]["findings"].append({
                            "file": str(file_path),
                            "pattern": pattern,
                            "status": "found"
                        })
        
        # SOC2 compliance checks
        soc2_patterns = [
            r'access_control',
            r'change_management',
            r'risk_assessment',
            r'security_policy'
        ]
        
        for file_path in self.project_root.rglob("*"):
            if file_path.is_file():
                for pattern in soc2_patterns:
                    if pattern in str(file_path):
                        self.audit_results["compliance"]["soc2"]["findings"].append({
                            "file": str(file_path),
                            "pattern": pattern,
                            "status": "found"
                        })
    
    def _add_finding(self, risk_level: str, title: str, description: str, recommendation: str, category: str):
        """Add a security finding to the audit results."""
        finding = {
            "risk_level": risk_level,
            "title": title,
            "description": description,
            "recommendation": recommendation,
            "category": category,
            "timestamp": datetime.now().isoformat()
        }
        
        self.audit_results["findings"].append(finding)
        self.audit_results["risk_levels"][risk_level].append(finding)
    
    def _generate_audit_summary(self):
        """Generate audit summary statistics."""
        self.audit_results["summary"]["total_findings"] = len(self.audit_results["findings"])
        self.audit_results["summary"]["critical_count"] = len(self.audit_results["risk_levels"]["critical"])
        self.audit_results["summary"]["high_count"] = len(self.audit_results["risk_levels"]["high"])
        self.audit_results["summary"]["medium_count"] = len(self.audit_results["risk_levels"]["medium"])
        self.audit_results["summary"]["low_count"] = len(self.audit_results["risk_levels"]["low"])
        self.audit_results["summary"]["info_count"] = len(self.audit_results["risk_levels"]["info"])
    
    def _save_audit_results(self):
        """Save audit results to file."""
        output_file = self.project_root / "security" / "audit" / "comprehensive_audit_report.json"
        output_file.parent.mkdir(parents=True, exist_ok=True)
        
        with open(output_file, 'w') as f:
            json.dump(self.audit_results, f, indent=2)
        
        logging.info(f"Audit results saved to {output_file}")

def main():
    """Main function to run the comprehensive security audit."""
    auditor = SecurityAuditor()
    results = auditor.run_comprehensive_audit()
    
    print("=== TuskLang FORTRESS Security Audit Results ===")
    print(f"Total Findings: {results['summary']['total_findings']}")
    print(f"Critical: {results['summary']['critical_count']}")
    print(f"High: {results['summary']['high_count']}")
    print(f"Medium: {results['summary']['medium_count']}")
    print(f"Low: {results['summary']['low_count']}")
    print(f"Info: {results['summary']['info_count']}")
    
    return results

if __name__ == "__main__":
    main() 