#!/usr/bin/env python3
"""
License Module for TuskLang Python SDK
======================================
Implements license validation, verification, and permission checking
"""

import os
import hashlib
import hmac
import base64
import json
import time
import logging
import requests
from typing import Any, Dict, List, Optional, Union
from datetime import datetime, timedelta
import re

logger = logging.getLogger(__name__)


class License:
    """License validation class for TuskLang"""
    
    def __init__(self):
        self.license_key = None
        self.license_data = {}
        self.license_server = "https://api.tusklang.org/v1/license"
        self.offline_mode = False
        self.cache_duration = 3600  # 1 hour
        self.last_verification = 0
        
        # License features and permissions
        self.features = {
            "core": ["basic_parsing", "file_operations", "database_queries"],
            "advanced": ["graphql", "websocket", "messaging", "monitoring"],
            "enterprise": ["multi_tenancy", "rbac", "oauth2", "audit"],
            "security": ["encryption", "signing", "protection"],
            "performance": ["binary_compilation", "optimization"]
        }
        
        # License types and their feature sets
        self.license_types = {
            "community": ["core"],
            "professional": ["core", "advanced"],
            "enterprise": ["core", "advanced", "enterprise", "security"],
            "ultimate": ["core", "advanced", "enterprise", "security", "performance"]
        }
    
    def validate_license_key(self, license_key: str = None) -> bool:
        """Validate license key format and checksum"""
        try:
            if not license_key:
                license_key = self.license_key or os.environ.get('TUSKLANG_LICENSE_KEY')
            
            if not license_key:
                logger.warning("No license key provided")
                return False
            
            # License key format: TUSK-XXXX-XXXX-XXXX-XXXX
            if not re.match(r'^TUSK-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$', license_key):
                logger.error("Invalid license key format")
                return False
            
            # Extract parts
            parts = license_key.split('-')
            if len(parts) != 5 or parts[0] != 'TUSK':
                logger.error("Invalid license key structure")
                return False
            
            # Validate checksum
            data_parts = parts[1:4]
            checksum_part = parts[4]
            
            # Calculate expected checksum
            data_string = ''.join(data_parts)
            expected_checksum = self._calculate_checksum(data_string)
            
            if checksum_part != expected_checksum:
                logger.error("License key checksum validation failed")
                return False
            
            self.license_key = license_key
            logger.info("License key format validation passed")
            return True
            
        except Exception as e:
            logger.error(f"License key validation error: {str(e)}")
            return False
    
    def verify_license_server(self, server_url: str = None) -> Dict[str, Any]:
        """Verify license with online server"""
        try:
            if not self.license_key:
                return {"valid": False, "error": "No license key available"}
            
            if self.offline_mode:
                return {"valid": True, "mode": "offline", "message": "Offline mode enabled"}
            
            # Check cache
            if time.time() - self.last_verification < self.cache_duration:
                return {"valid": True, "mode": "cached", "data": self.license_data}
            
            server_url = server_url or self.license_server
            
            # Prepare verification request
            payload = {
                "license_key": self.license_key,
                "timestamp": int(time.time()),
                "client_id": self._get_client_id(),
                "version": "1.0.0"
            }
            
            # Add signature
            signature = self._generate_request_signature(payload)
            payload["signature"] = signature
            
            # Make request
            headers = {
                "Content-Type": "application/json",
                "User-Agent": "TuskLang-Python-SDK/1.0.0"
            }
            
            response = requests.post(
                server_url,
                json=payload,
                headers=headers,
                timeout=30
            )
            
            if response.status_code == 200:
                result = response.json()
                
                if result.get("valid", False):
                    self.license_data = result.get("data", {})
                    self.last_verification = time.time()
                    
                    return {
                        "valid": True,
                        "mode": "online",
                        "data": self.license_data,
                        "expires": result.get("expires"),
                        "features": result.get("features", [])
                    }
                else:
                    return {
                        "valid": False,
                        "error": result.get("error", "License verification failed"),
                        "code": result.get("code")
                    }
            else:
                return {
                    "valid": False,
                    "error": f"Server error: {response.status_code}",
                    "fallback": "offline"
                }
                
        except requests.exceptions.RequestException as e:
            logger.warning(f"Online verification failed: {str(e)}")
            return {
                "valid": False,
                "error": f"Network error: {str(e)}",
                "fallback": "offline"
            }
        except Exception as e:
            logger.error(f"License verification error: {str(e)}")
            return {
                "valid": False,
                "error": f"Verification error: {str(e)}"
            }
    
    def check_license_expiration(self) -> Dict[str, Any]:
        """Check license expiration date"""
        try:
            if not self.license_data:
                return {"expired": True, "error": "No license data available"}
            
            expiration_date = self.license_data.get("expires")
            if not expiration_date:
                return {"expired": False, "message": "No expiration date set"}
            
            # Parse expiration date
            if isinstance(expiration_date, str):
                try:
                    exp_date = datetime.fromisoformat(expiration_date.replace('Z', '+00:00'))
                except:
                    exp_date = datetime.strptime(expiration_date, "%Y-%m-%d")
            else:
                exp_date = datetime.fromtimestamp(expiration_date)
            
            now = datetime.now()
            
            if exp_date.tzinfo is None:
                exp_date = exp_date.replace(tzinfo=now.tzinfo)
            
            if now.tzinfo is None:
                now = now.replace(tzinfo=exp_date.tzinfo)
            
            is_expired = now > exp_date
            days_remaining = (exp_date - now).days if not is_expired else 0
            
            return {
                "expired": is_expired,
                "expiration_date": exp_date.isoformat(),
                "days_remaining": days_remaining,
                "warning": days_remaining <= 30 if not is_expired else False
            }
            
        except Exception as e:
            logger.error(f"License expiration check error: {str(e)}")
            return {"expired": True, "error": str(e)}
    
    def validate_license_permissions(self, feature_name: str) -> bool:
        """Validate if license allows specific feature"""
        try:
            if not self.license_data:
                return False
            
            license_type = self.license_data.get("type", "community")
            allowed_features = self.license_types.get(license_type, [])
            
            # Check if feature is in allowed feature sets
            for feature_set in allowed_features:
                if feature_name in self.features.get(feature_set, []):
                    return True
            
            # Check explicit feature permissions
            explicit_features = self.license_data.get("features", [])
            if feature_name in explicit_features:
                return True
            
            # Check if it's a core feature (always allowed)
            if feature_name in self.features.get("core", []):
                return True
            
            logger.warning(f"Feature '{feature_name}' not allowed by license type '{license_type}'")
            return False
            
        except Exception as e:
            logger.error(f"Permission validation error: {str(e)}")
            return False
    
    def get_license_info(self) -> Dict[str, Any]:
        """Get comprehensive license information"""
        try:
            if not self.license_key:
                return {"error": "No license key available"}
            
            info = {
                "license_key": self.license_key,
                "valid": self.validate_license_key(),
                "data": self.license_data.copy() if self.license_data else {},
                "expiration": self.check_license_expiration(),
                "features": self._get_available_features(),
                "type": self.license_data.get("type", "unknown"),
                "customer": self.license_data.get("customer", "unknown"),
                "issued": self.license_data.get("issued"),
                "last_verification": self.last_verification
            }
            
            return info
            
        except Exception as e:
            logger.error(f"License info error: {str(e)}")
            return {"error": str(e)}
    
    def _calculate_checksum(self, data: str) -> str:
        """Calculate license key checksum"""
        try:
            # Use HMAC-SHA256 with a secret key
            secret_key = "TuskLang-License-Secret-2024"
            signature = hmac.new(
                secret_key.encode('utf-8'),
                data.encode('utf-8'),
                hashlib.sha256
            ).digest()
            
            # Take first 4 characters of base32 encoding
            checksum = base64.b32encode(signature[:3]).decode('utf-8')
            return checksum[:4]
            
        except Exception as e:
            logger.error(f"Checksum calculation error: {str(e)}")
            return "0000"
    
    def _get_client_id(self) -> str:
        """Generate client ID for license verification"""
        try:
            # Use machine-specific information
            import platform
            import uuid
            
            machine_info = {
                "platform": platform.system(),
                "machine": platform.machine(),
                "processor": platform.processor(),
                "hostname": platform.node()
            }
            
            # Create deterministic client ID
            client_data = json.dumps(machine_info, sort_keys=True)
            client_hash = hashlib.sha256(client_data.encode('utf-8')).hexdigest()
            
            return client_hash[:16]
            
        except Exception as e:
            logger.error(f"Client ID generation error: {str(e)}")
            return "unknown"
    
    def _generate_request_signature(self, payload: Dict[str, Any]) -> str:
        """Generate signature for license verification request"""
        try:
            # Create signature data
            signature_data = {
                "license_key": payload["license_key"],
                "timestamp": payload["timestamp"],
                "client_id": payload["client_id"]
            }
            
            # Sort keys for deterministic signature
            signature_string = json.dumps(signature_data, sort_keys=True)
            
            # Generate HMAC signature
            secret_key = "TuskLang-API-Secret-2024"
            signature = hmac.new(
                secret_key.encode('utf-8'),
                signature_string.encode('utf-8'),
                hashlib.sha256
            ).digest()
            
            return base64.b64encode(signature).decode('utf-8')
            
        except Exception as e:
            logger.error(f"Request signature error: {str(e)}")
            return ""
    
    def _get_available_features(self) -> List[str]:
        """Get list of available features based on license"""
        try:
            if not self.license_data:
                return self.features.get("core", [])
            
            license_type = self.license_data.get("type", "community")
            allowed_features = self.license_types.get(license_type, [])
            
            available = []
            for feature_set in allowed_features:
                available.extend(self.features.get(feature_set, []))
            
            # Add explicit features
            explicit_features = self.license_data.get("features", [])
            available.extend(explicit_features)
            
            return list(set(available))  # Remove duplicates
            
        except Exception as e:
            logger.error(f"Feature list error: {str(e)}")
            return self.features.get("core", [])
    
    def set_offline_mode(self, enabled: bool = True):
        """Enable or disable offline mode"""
        self.offline_mode = enabled
        logger.info(f"Offline mode {'enabled' if enabled else 'disabled'}")
    
    def load_license_from_file(self, file_path: str) -> bool:
        """Load license from file"""
        try:
            if not os.path.exists(file_path):
                logger.error(f"License file not found: {file_path}")
                return False
            
            with open(file_path, 'r') as f:
                license_data = json.load(f)
            
            if "license_key" in license_data:
                self.license_key = license_data["license_key"]
                self.license_data = license_data.get("data", {})
                self.last_verification = time.time()
                
                logger.info(f"License loaded from file: {file_path}")
                return True
            else:
                logger.error("Invalid license file format")
                return False
                
        except Exception as e:
            logger.error(f"License file loading error: {str(e)}")
            return False
    
    def save_license_to_file(self, file_path: str) -> bool:
        """Save license to file"""
        try:
            if not self.license_key:
                logger.error("No license key to save")
                return False
            
            license_data = {
                "license_key": self.license_key,
                "data": self.license_data,
                "last_verification": self.last_verification,
                "saved_at": time.time()
            }
            
            with open(file_path, 'w') as f:
                json.dump(license_data, f, indent=2)
            
            logger.info(f"License saved to file: {file_path}")
            return True
            
        except Exception as e:
            logger.error(f"License file saving error: {str(e)}")
            return False


# Global license instance
_license = License()


# Operator functions for TuskLang
def validate_license_key(license_key: str = None) -> bool:
    """Execute @license.validate operator"""
    try:
        return _license.validate_license_key(license_key)
    except Exception as e:
        logger.error(f"License validation error: {str(e)}")
        return False


def verify_license_server(server_url: str = None) -> Dict[str, Any]:
    """Execute @license.verify operator"""
    try:
        return _license.verify_license_server(server_url)
    except Exception as e:
        logger.error(f"License verification error: {str(e)}")
        return {"valid": False, "error": str(e)}


def check_license_expiration() -> Dict[str, Any]:
    """Execute @license.check operator"""
    try:
        return _license.check_license_expiration()
    except Exception as e:
        logger.error(f"License expiration check error: {str(e)}")
        return {"expired": True, "error": str(e)}


def validate_license_permissions(feature_name: str) -> bool:
    """Execute @license.permissions operator"""
    try:
        return _license.validate_license_permissions(feature_name)
    except Exception as e:
        logger.error(f"License permissions error: {str(e)}")
        return False


# Convenience functions for direct use
def check_license(feature: str = None) -> bool:
    """Check if license is valid and allows specific feature"""
    try:
        if not _license.validate_license_key():
            return False
        
        if feature:
            return _license.validate_license_permissions(feature)
        else:
            return True
            
    except Exception as e:
        logger.error(f"License check error: {str(e)}")
        return False


def get_license_status() -> Dict[str, Any]:
    """Get current license status"""
    try:
        return _license.get_license_info()
    except Exception as e:
        logger.error(f"License status error: {str(e)}")
        return {"error": str(e)}


def require_license(feature: str):
    """Decorator to require license for function execution"""
    def decorator(func):
        def wrapper(*args, **kwargs):
            if not check_license(feature):
                raise Exception(f"License required for feature: {feature}")
            return func(*args, **kwargs)
        return wrapper
    return decorator


# Test functions
def test_license():
    """Test license functionality"""
    print("Testing TuskLang License Module...")
    
    # Test license key validation
    print("\n1. Testing license key validation...")
    test_key = "TUSK-1234-5678-9ABC-DEF0"  # This will fail checksum
    valid = validate_license_key(test_key)
    print(f"Test key valid: {valid}")
    
    # Test license info
    print("\n2. Testing license info...")
    info = get_license_status()
    print(f"License info: {json.dumps(info, indent=2)}")
    
    # Test feature permissions
    print("\n3. Testing feature permissions...")
    features = ["basic_parsing", "graphql", "encryption", "multi_tenancy"]
    for feature in features:
        allowed = validate_license_permissions(feature)
        print(f"Feature '{feature}': {'Allowed' if allowed else 'Denied'}")
    
    # Test expiration check
    print("\n4. Testing expiration check...")
    expiration = check_license_expiration()
    print(f"Expiration: {json.dumps(expiration, indent=2)}")
    
    print("\nLicense module tests completed!")


if __name__ == '__main__':
    test_license() 