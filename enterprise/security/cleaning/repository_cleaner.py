#!/usr/bin/env python3
"""
TuskLang FORTRESS - Repository Cleaning System
Agent A4 - Security & Compliance Expert

This module provides comprehensive repository cleaning to remove sensitive data and hardcoded secrets.
"""

import os
import sys
import json
import re
import shutil
import logging
from datetime import datetime
from typing import Dict, List, Any, Optional, Tuple
from pathlib import Path

class RepositoryCleaner:
    """Comprehensive repository cleaning system."""
    
    def __init__(self, project_root: str = "."):
        self.project_root = Path(project_root)
        self.cleaning_results = {
            "timestamp": datetime.now().isoformat(),
            "cleaner": "Agent A4 - Security & Compliance Expert",
            "project": "TuskLang FORTRESS",
            "cleaning_operations": [],
            "files_processed": 0,
            "files_cleaned": 0,
            "secrets_removed": 0,
            "sensitive_files_removed": 0,
            "backup_created": True,
            "summary": {
                "total_operations": 0,
                "successful_operations": 0,
                "failed_operations": 0
            }
        }
        
        # Patterns for sensitive data
        self.sensitive_patterns = [
            # API Keys and Tokens
            r'api_key\s*=\s*["\'][^"\']+["\']',
            r'api_token\s*=\s*["\'][^"\']+["\']',
            r'access_token\s*=\s*["\'][^"\']+["\']',
            r'secret_key\s*=\s*["\'][^"\']+["\']',
            r'private_key\s*=\s*["\'][^"\']+["\']',
            
            # Passwords and Credentials
            r'password\s*=\s*["\'][^"\']+["\']',
            r'passwd\s*=\s*["\'][^"\']+["\']',
            r'credential\s*=\s*["\'][^"\']+["\']',
            r'username\s*=\s*["\'][^"\']+["\']',
            r'user\s*=\s*["\'][^"\']+["\']',
            
            # Database Connections
            r'database_url\s*=\s*["\'][^"\']+["\']',
            r'db_url\s*=\s*["\'][^"\']+["\']',
            r'connection_string\s*=\s*["\'][^"\']+["\']',
            
            # Private Keys and Certificates
            r'-----BEGIN PRIVATE KEY-----',
            r'-----BEGIN RSA PRIVATE KEY-----',
            r'-----BEGIN DSA PRIVATE KEY-----',
            r'-----BEGIN EC PRIVATE KEY-----',
            r'-----BEGIN CERTIFICATE-----',
            
            # OAuth and Social Media
            r'oauth_token\s*=\s*["\'][^"\']+["\']',
            r'client_secret\s*=\s*["\'][^"\']+["\']',
            r'client_id\s*=\s*["\'][^"\']+["\']',
            
            # AWS and Cloud Services
            r'aws_access_key\s*=\s*["\'][^"\']+["\']',
            r'aws_secret_key\s*=\s*["\'][^"\']+["\']',
            r'aws_session_token\s*=\s*["\'][^"\']+["\']',
            
            # Email and SMTP
            r'smtp_password\s*=\s*["\'][^"\']+["\']',
            r'email_password\s*=\s*["\'][^"\']+["\']',
            
            # JWT and Session
            r'jwt_secret\s*=\s*["\'][^"\']+["\']',
            r'session_secret\s*=\s*["\'][^"\']+["\']',
            
            # Encryption Keys
            r'encryption_key\s*=\s*["\'][^"\']+["\']',
            r'crypto_key\s*=\s*["\'][^"\']+["\']',
            r'hash_salt\s*=\s*["\'][^"\']+["\']'
        ]
        
        # Sensitive file patterns
        self.sensitive_files = [
            '.env', '.env.local', '.env.production', '.env.development',
            '.pem', '.key', '.crt', '.p12', '.pfx', '.p8',
            'secrets.json', 'credentials.json', 'config.json',
            'private.key', 'public.key', 'certificate.crt',
            'id_rsa', 'id_dsa', 'id_ecdsa', 'id_ed25519',
            '.aws', '.azure', '.gcp', '.kube',
            'password.txt', 'secrets.txt', 'keys.txt'
        ]
        
        # File extensions to scan
        self.scan_extensions = [
            '.py', '.js', '.ts', '.php', '.java', '.go', '.rs',
            '.sh', '.bash', '.zsh', '.fish',
            '.json', '.yaml', '.yml', '.toml', '.ini', '.cfg',
            '.md', '.txt', '.log', '.conf', '.config'
        ]
        
        # Directories to exclude
        self.exclude_dirs = [
            '.git', '.svn', '.hg', '__pycache__', 'node_modules',
            '.venv', 'venv', 'env', '.env', 'build', 'dist',
            'target', 'bin', 'obj', '.vs', '.idea', '.vscode'
        ]
    
    def clean_repository(self) -> Dict[str, Any]:
        """Perform comprehensive repository cleaning."""
        logging.info("Starting comprehensive repository cleaning")
        
        # Create backup
        self._create_backup()
        
        # Clean sensitive data
        self._clean_sensitive_data()
        
        # Remove sensitive files
        self._remove_sensitive_files()
        
        # Setup secure configuration management
        self._setup_secure_configuration()
        
        # Generate cleaning report
        self._generate_cleaning_report()
        
        logging.info("Repository cleaning completed")
        return self.cleaning_results
    
    def _create_backup(self):
        """Create backup before cleaning operations."""
        logging.info("Creating backup before cleaning")
        
        backup_dir = self.project_root / "backup" / f"pre_cleaning_{datetime.now().strftime('%Y%m%d_%H%M%S')}"
        backup_dir.mkdir(parents=True, exist_ok=True)
        
        try:
            # Copy project files to backup
            for item in self.project_root.iterdir():
                if item.name not in ['.git', 'backup']:
                    if item.is_file():
                        shutil.copy2(item, backup_dir / item.name)
                    elif item.is_dir():
                        shutil.copytree(item, backup_dir / item.name, ignore=shutil.ignore_patterns('__pycache__', '*.pyc'))
            
            self.cleaning_results["backup_path"] = str(backup_dir)
            logging.info(f"Backup created at {backup_dir}")
            
        except Exception as e:
            logging.error(f"Backup creation failed: {e}")
            self.cleaning_results["backup_created"] = False
    
    def _clean_sensitive_data(self):
        """Clean sensitive data from code files."""
        logging.info("Cleaning sensitive data from code files")
        
        for file_path in self.project_root.rglob("*"):
            if self._should_scan_file(file_path):
                self._clean_file_content(file_path)
    
    def _should_scan_file(self, file_path: Path) -> bool:
        """Determine if a file should be scanned for sensitive data."""
        if not file_path.is_file():
            return False
        
        # Check if file extension should be scanned
        if file_path.suffix not in self.scan_extensions:
            return False
        
        # Check if file is in excluded directory
        for exclude_dir in self.exclude_dirs:
            if exclude_dir in file_path.parts:
                return False
        
        return True
    
    def _clean_file_content(self, file_path: Path):
        """Clean sensitive content from a file."""
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            original_content = content
            cleaned_content = content
            secrets_found = []
            
            # Check for sensitive patterns
            for pattern in self.sensitive_patterns:
                matches = re.finditer(pattern, content, re.IGNORECASE)
                for match in matches:
                    secret_value = match.group(0)
                    secrets_found.append({
                        "pattern": pattern,
                        "value": secret_value,
                        "line": content[:match.start()].count('\n') + 1
                    })
                    
                    # Replace with placeholder
                    placeholder = self._generate_placeholder(secret_value)
                    cleaned_content = cleaned_content.replace(secret_value, placeholder)
            
            # Write cleaned content if changes were made
            if cleaned_content != original_content:
                with open(file_path, 'w', encoding='utf-8') as f:
                    f.write(cleaned_content)
                
                self.cleaning_results["files_cleaned"] += 1
                self.cleaning_results["secrets_removed"] += len(secrets_found)
                
                self.cleaning_results["cleaning_operations"].append({
                    "file": str(file_path),
                    "operation": "sensitive_data_removal",
                    "secrets_found": len(secrets_found),
                    "status": "success"
                })
                
                logging.info(f"Cleaned {len(secrets_found)} secrets from {file_path}")
            
            self.cleaning_results["files_processed"] += 1
            
        except Exception as e:
            logging.error(f"Error cleaning file {file_path}: {e}")
            self.cleaning_results["cleaning_operations"].append({
                "file": str(file_path),
                "operation": "sensitive_data_removal",
                "status": "failed",
                "error": str(e)
            })
    
    def _generate_placeholder(self, secret_value: str) -> str:
        """Generate a placeholder for sensitive data."""
        if '=' in secret_value:
            # For assignments like "api_key = 'secret'"
            parts = secret_value.split('=', 1)
            return f"{parts[0]}= 'REMOVED_SECRET'"
        else:
            # For other patterns
            return "REMOVED_SECRET"
    
    def _remove_sensitive_files(self):
        """Remove sensitive files from repository."""
        logging.info("Removing sensitive files")
        
        for file_path in self.project_root.rglob("*"):
            if file_path.is_file():
                file_name = file_path.name.lower()
                
                # Check if file matches sensitive patterns
                for sensitive_pattern in self.sensitive_files:
                    if sensitive_pattern.lower() in file_name:
                        try:
                            # Move to backup instead of deleting
                            backup_dir = self.project_root / "backup" / "sensitive_files"
                            backup_dir.mkdir(parents=True, exist_ok=True)
                            
                            backup_path = backup_dir / f"{file_path.name}_{datetime.now().strftime('%Y%m%d_%H%M%S')}"
                            shutil.move(str(file_path), str(backup_path))
                            
                            self.cleaning_results["sensitive_files_removed"] += 1
                            self.cleaning_results["cleaning_operations"].append({
                                "file": str(file_path),
                                "operation": "sensitive_file_removal",
                                "backup_location": str(backup_path),
                                "status": "success"
                            })
                            
                            logging.info(f"Removed sensitive file {file_path} to {backup_path}")
                            
                        except Exception as e:
                            logging.error(f"Error removing sensitive file {file_path}: {e}")
                            self.cleaning_results["cleaning_operations"].append({
                                "file": str(file_path),
                                "operation": "sensitive_file_removal",
                                "status": "failed",
                                "error": str(e)
                            })
    
    def _setup_secure_configuration(self):
        """Setup secure configuration management."""
        logging.info("Setting up secure configuration management")
        
        # Create secure configuration template
        config_template = '''# Secure Configuration Template
# Replace placeholder values with actual configuration
# DO NOT commit actual secrets to version control

# Database Configuration
DATABASE_URL = "postgresql://user:password@localhost:5432/database"

# API Configuration
API_KEY = "your_api_key_here"
API_SECRET = "your_api_secret_here"

# Security Configuration
SECRET_KEY = "your_secret_key_here"
JWT_SECRET = "your_jwt_secret_here"

# Email Configuration
SMTP_HOST = "smtp.example.com"
SMTP_PORT = 587
SMTP_USERNAME = "your_email@example.com"
SMTP_PASSWORD = "your_email_password_here"

# AWS Configuration
AWS_ACCESS_KEY_ID = "your_aws_access_key"
AWS_SECRET_ACCESS_KEY = "your_aws_secret_key"
AWS_REGION = "us-east-1"

# OAuth Configuration
OAUTH_CLIENT_ID = "your_oauth_client_id"
OAUTH_CLIENT_SECRET = "your_oauth_client_secret"

# Monitoring Configuration
LOG_LEVEL = "INFO"
MONITORING_ENABLED = true
'''
        
        # Create configuration management system
        config_manager_content = '''#!/usr/bin/env python3
"""
TuskLang FORTRESS - Secure Configuration Management
Agent A4 - Security & Compliance Expert

This module provides secure configuration management for the TuskLang FORTRESS system.
"""

import os
import json
import logging
from pathlib import Path
from typing import Dict, Any, Optional

class SecureConfigManager:
    """Secure configuration management system."""
    
    def __init__(self, config_dir: str = "config"):
        self.config_dir = Path(config_dir)
        self.config_dir.mkdir(exist_ok=True)
        self.logger = logging.getLogger(__name__)
    
    def load_config(self, environment: str = "development") -> Dict[str, Any]:
        """Load configuration for specified environment."""
        config_file = self.config_dir / f"config_{environment}.json"
        
        if config_file.exists():
            with open(config_file, 'r') as f:
                return json.load(f)
        else:
            self.logger.warning(f"Configuration file {config_file} not found")
            return {}
    
    def save_config(self, config: Dict[str, Any], environment: str = "development"):
        """Save configuration for specified environment."""
        config_file = self.config_dir / f"config_{environment}.json"
        
        with open(config_file, 'w') as f:
            json.dump(config, f, indent=2)
        
        self.logger.info(f"Configuration saved to {config_file}")
    
    def validate_config(self, config: Dict[str, Any]) -> bool:
        """Validate configuration for required fields."""
        required_fields = [
            "DATABASE_URL",
            "SECRET_KEY",
            "API_KEY"
        ]
        
        for field in required_fields:
            if field not in config or not config[field]:
                self.logger.error(f"Missing required configuration field: {field}")
                return False
        
        return True

def main():
    """Main function for secure configuration management."""
    manager = SecureConfigManager()
    
    # Example configuration
    config = {
        "DATABASE_URL": "postgresql://user:password@localhost:5432/database",
        "SECRET_KEY": "your_secret_key_here",
        "API_KEY": "your_api_key_here"
    }
    
    if manager.validate_config(config):
        manager.save_config(config, "development")
        print("Configuration saved successfully")
    else:
        print("Configuration validation failed")
    
    return config

if __name__ == "__main__":
    main()
'''
        
        # Create configuration files
        config_dir = self.project_root / "config"
        config_dir.mkdir(exist_ok=True)
        
        # Save template
        template_file = config_dir / "config_template.env"
        with open(template_file, 'w') as f:
            f.write(config_template)
        
        # Save configuration manager
        manager_file = config_dir / "config_manager.py"
        with open(manager_file, 'w') as f:
            f.write(config_manager_content)
        
        # Update .gitignore to exclude sensitive files
        gitignore_content = '''
# Sensitive files
.env
.env.local
.env.production
.env.development
*.pem
*.key
*.crt
*.p12
*.pfx
*.p8
secrets.json
credentials.json
config.json
private.key
public.key
certificate.crt
id_rsa
id_dsa
id_ecdsa
id_ed25519
.aws
.azure
.gcp
.kube
password.txt
secrets.txt
keys.txt

# Configuration files with secrets
config/config_*.json
config/*.env

# Backup directories
backup/
'''
        
        gitignore_file = self.project_root / ".gitignore"
        if gitignore_file.exists():
            with open(gitignore_file, 'a') as f:
                f.write(gitignore_content)
        else:
            with open(gitignore_file, 'w') as f:
                f.write(gitignore_content)
        
        self.cleaning_results["cleaning_operations"].append({
            "operation": "secure_configuration_setup",
            "files_created": [
                str(template_file),
                str(manager_file)
            ],
            "status": "success"
        })
        
        logging.info("Secure configuration management setup completed")
    
    def _generate_cleaning_report(self):
        """Generate comprehensive cleaning report."""
        # Update summary
        self.cleaning_results["summary"]["total_operations"] = len(self.cleaning_results["cleaning_operations"])
        self.cleaning_results["summary"]["successful_operations"] = len([
            op for op in self.cleaning_results["cleaning_operations"] 
            if op.get("status") == "success"
        ])
        self.cleaning_results["summary"]["failed_operations"] = len([
            op for op in self.cleaning_results["cleaning_operations"] 
            if op.get("status") == "failed"
        ])
        
        # Save report
        output_file = self.project_root / "security" / "cleaning" / "cleaning_report.json"
        output_file.parent.mkdir(parents=True, exist_ok=True)
        
        with open(output_file, 'w') as f:
            json.dump(self.cleaning_results, f, indent=2)
        
        logging.info(f"Cleaning report saved to {output_file}")

def main():
    """Main function to run repository cleaning."""
    cleaner = RepositoryCleaner()
    results = cleaner.clean_repository()
    
    print("=== TuskLang FORTRESS Repository Cleaning Results ===")
    print(f"Files Processed: {results['files_processed']}")
    print(f"Files Cleaned: {results['files_cleaned']}")
    print(f"Secrets Removed: {results['secrets_removed']}")
    print(f"Sensitive Files Removed: {results['sensitive_files_removed']}")
    print(f"Backup Created: {results['backup_created']}")
    print(f"Total Operations: {results['summary']['total_operations']}")
    print(f"Successful Operations: {results['summary']['successful_operations']}")
    print(f"Failed Operations: {results['summary']['failed_operations']}")
    
    return results

if __name__ == "__main__":
    main() 