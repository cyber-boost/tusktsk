#!/usr/bin/env python3
"""
Protection Module for TuskLang Python SDK
=========================================
Implements security features: encryption, signing, integrity verification
"""

import os
import hmac
import hashlib
import base64
import json
import logging
import secrets
import time
from typing import Any, Dict, Optional, Tuple
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.backends import default_backend
from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC
from cryptography.hazmat.primitives import serialization
from cryptography.hazmat.primitives.asymmetric import rsa, padding
import struct

logger = logging.getLogger(__name__)


class Protection:
    """Security protection class for TuskLang"""
    
    def __init__(self, master_key: str = None):
        self.master_key = master_key or os.environ.get('TUSKLANG_MASTER_KEY', 'default-master-key-change-me')
        self.algorithm = 'AES-256-GCM'
        self.key_length = 32  # 256 bits
        self.iv_length = 12   # 96 bits for GCM
        self.tag_length = 16  # 128 bits for GCM
        self.salt_length = 32
        self.iterations = 100000
        
        # Generate or load encryption key
        self.encryption_key = self._derive_key(self.master_key, b'encryption')
        self.signing_key = self._derive_key(self.master_key, b'signing')
        
        # Security violations tracking
        self.violations = []
        self.max_violations = 10
        self.lockout_duration = 3600  # 1 hour
    
    def _derive_key(self, password: str, purpose: bytes) -> bytes:
        """Derive encryption key from master key using PBKDF2"""
        salt = hashlib.sha256(purpose + self.master_key.encode()).digest()
        kdf = PBKDF2HMAC(
            algorithm=hashes.SHA256(),
            length=self.key_length,
            salt=salt,
            iterations=self.iterations,
            backend=default_backend()
        )
        return kdf.derive(password.encode())
    
    def encrypt_data(self, data: str, additional_data: str = None) -> str:
        """Encrypt data using AES-256-GCM"""
        try:
            # Convert data to bytes
            if isinstance(data, str):
                data_bytes = data.encode('utf-8')
            else:
                data_bytes = str(data).encode('utf-8')
            
            # Generate random IV
            iv = os.urandom(self.iv_length)
            
            # Create cipher
            cipher = Cipher(
                algorithms.AES(self.encryption_key),
                modes.GCM(iv),
                backend=default_backend()
            )
            encryptor = cipher.encryptor()
            
            # Add additional authenticated data if provided
            if additional_data:
                encryptor.authenticate_additional_data(additional_data.encode('utf-8'))
            
            # Encrypt data
            ciphertext = encryptor.update(data_bytes) + encryptor.finalize()
            
            # Get authentication tag
            tag = encryptor.tag
            
            # Combine IV, ciphertext, and tag
            encrypted_data = iv + ciphertext + tag
            
            # Encode as base64
            return base64.b64encode(encrypted_data).decode('utf-8')
            
        except Exception as e:
            logger.error(f"Encryption error: {str(e)}")
            raise Exception(f"Encryption failed: {str(e)}")
    
    def decrypt_data(self, encrypted_data: str, additional_data: str = None) -> str:
        """Decrypt data using AES-256-GCM"""
        try:
            # Decode from base64
            encrypted_bytes = base64.b64decode(encrypted_data.encode('utf-8'))
            
            # Extract IV, ciphertext, and tag
            iv = encrypted_bytes[:self.iv_length]
            tag = encrypted_bytes[-self.tag_length:]
            ciphertext = encrypted_bytes[self.iv_length:-self.tag_length]
            
            # Create cipher
            cipher = Cipher(
                algorithms.AES(self.encryption_key),
                modes.GCM(iv, tag),
                backend=default_backend()
            )
            decryptor = cipher.decryptor()
            
            # Add additional authenticated data if provided
            if additional_data:
                decryptor.authenticate_additional_data(additional_data.encode('utf-8'))
            
            # Decrypt data
            decrypted_data = decryptor.update(ciphertext) + decryptor.finalize()
            
            return decrypted_data.decode('utf-8')
            
        except Exception as e:
            logger.error(f"Decryption error: {str(e)}")
            raise Exception(f"Decryption failed: {str(e)}")
    
    def generate_signature(self, data: str) -> str:
        """Generate HMAC-SHA256 signature"""
        try:
            if isinstance(data, str):
                data_bytes = data.encode('utf-8')
            else:
                data_bytes = str(data).encode('utf-8')
            
            signature = hmac.new(
                self.signing_key,
                data_bytes,
                hashlib.sha256
            ).digest()
            
            return base64.b64encode(signature).decode('utf-8')
            
        except Exception as e:
            logger.error(f"Signature generation error: {str(e)}")
            raise Exception(f"Signature generation failed: {str(e)}")
    
    def verify_integrity(self, data: str, signature: str) -> bool:
        """Verify HMAC-SHA256 signature"""
        try:
            expected_signature = self.generate_signature(data)
            return hmac.compare_digest(signature, expected_signature)
            
        except Exception as e:
            logger.error(f"Integrity verification error: {str(e)}")
            return False
    
    def obfuscate_code(self, source_code: str) -> str:
        """Obfuscate source code using base64 encoding"""
        try:
            # Compress and encode
            import gzip
            compressed = gzip.compress(source_code.encode('utf-8'))
            encoded = base64.b64encode(compressed).decode('utf-8')
            
            # Add header for identification
            return f"# OBFUSCATED_CODE\n{encoded}"
            
        except Exception as e:
            logger.error(f"Code obfuscation error: {str(e)}")
            raise Exception(f"Code obfuscation failed: {str(e)}")
    
    def deobfuscate_code(self, obfuscated_code: str) -> str:
        """Deobfuscate source code"""
        try:
            # Remove header
            if obfuscated_code.startswith("# OBFUSCATED_CODE\n"):
                encoded = obfuscated_code[len("# OBFUSCATED_CODE\n"):]
            else:
                encoded = obfuscated_code
            
            # Decode and decompress
            import gzip
            compressed = base64.b64decode(encoded.encode('utf-8'))
            source_code = gzip.decompress(compressed).decode('utf-8')
            
            return source_code
            
        except Exception as e:
            logger.error(f"Code deobfuscation error: {str(e)}")
            raise Exception(f"Code deobfuscation failed: {str(e)}")
    
    def detect_tampering(self) -> Dict[str, Any]:
        """Detect file tampering (placeholder implementation)"""
        try:
            # This would typically check file hashes, signatures, etc.
            # For now, return basic integrity check
            return {
                "tampering_detected": False,
                "integrity_check": "passed",
                "timestamp": time.time(),
                "checksum": hashlib.sha256(self.master_key.encode()).hexdigest()
            }
            
        except Exception as e:
            logger.error(f"Tampering detection error: {str(e)}")
            return {
                "tampering_detected": True,
                "integrity_check": "failed",
                "error": str(e),
                "timestamp": time.time()
            }
    
    def report_violation(self, violation_type: str, details: str) -> bool:
        """Report security violation"""
        try:
            violation = {
                "type": violation_type,
                "details": details,
                "timestamp": time.time(),
                "datetime": time.strftime("%Y-%m-%d %H:%M:%S"),
                "ip_address": self._get_client_ip(),
                "user_agent": self._get_user_agent()
            }
            
            self.violations.append(violation)
            
            # Check if we should trigger lockout
            recent_violations = [
                v for v in self.violations 
                if time.time() - v["timestamp"] < self.lockout_duration
            ]
            
            if len(recent_violations) >= self.max_violations:
                logger.warning(f"Security lockout triggered after {len(recent_violations)} violations")
                return False
            
            logger.warning(f"Security violation reported: {violation_type} - {details}")
            return True
            
        except Exception as e:
            logger.error(f"Violation reporting error: {str(e)}")
            return False
    
    def _get_client_ip(self) -> str:
        """Get client IP address (placeholder)"""
        # In a web context, this would get the actual client IP
        return "127.0.0.1"
    
    def _get_user_agent(self) -> str:
        """Get user agent (placeholder)"""
        # In a web context, this would get the actual user agent
        return "TuskLang-Python-SDK"
    
    def generate_secure_token(self, length: int = 32) -> str:
        """Generate cryptographically secure token"""
        try:
            return secrets.token_urlsafe(length)
        except Exception as e:
            logger.error(f"Token generation error: {str(e)}")
            raise Exception(f"Token generation failed: {str(e)}")
    
    def hash_password(self, password: str) -> str:
        """Hash password using bcrypt"""
        try:
            import bcrypt
            salt = bcrypt.gensalt()
            hashed = bcrypt.hashpw(password.encode('utf-8'), salt)
            return hashed.decode('utf-8')
        except Exception as e:
            logger.error(f"Password hashing error: {str(e)}")
            raise Exception(f"Password hashing failed: {str(e)}")
    
    def verify_password(self, password: str, hashed_password: str) -> bool:
        """Verify password against hash"""
        try:
            import bcrypt
            return bcrypt.checkpw(password.encode('utf-8'), hashed_password.encode('utf-8'))
        except Exception as e:
            logger.error(f"Password verification error: {str(e)}")
            return False
    
    def generate_key_pair(self) -> Tuple[str, str]:
        """Generate RSA key pair"""
        try:
            # Generate private key
            private_key = rsa.generate_private_key(
                public_exponent=65537,
                key_size=2048,
                backend=default_backend()
            )
            
            # Get public key
            public_key = private_key.public_key()
            
            # Serialize keys
            private_pem = private_key.private_bytes(
                encoding=serialization.Encoding.PEM,
                format=serialization.PrivateFormat.PKCS8,
                encryption_algorithm=serialization.NoEncryption()
            )
            
            public_pem = public_key.public_bytes(
                encoding=serialization.Encoding.PEM,
                format=serialization.PublicFormat.SubjectPublicKeyInfo
            )
            
            return private_pem.decode('utf-8'), public_pem.decode('utf-8')
            
        except Exception as e:
            logger.error(f"Key pair generation error: {str(e)}")
            raise Exception(f"Key pair generation failed: {str(e)}")
    
    def sign_data_rsa(self, data: str, private_key_pem: str) -> str:
        """Sign data using RSA private key"""
        try:
            # Load private key
            private_key = serialization.load_pem_private_key(
                private_key_pem.encode('utf-8'),
                password=None,
                backend=default_backend()
            )
            
            # Sign data
            signature = private_key.sign(
                data.encode('utf-8'),
                padding.PSS(
                    mgf=padding.MGF1(hashes.SHA256()),
                    salt_length=padding.PSS.MAX_LENGTH
                ),
                hashes.SHA256()
            )
            
            return base64.b64encode(signature).decode('utf-8')
            
        except Exception as e:
            logger.error(f"RSA signing error: {str(e)}")
            raise Exception(f"RSA signing failed: {str(e)}")
    
    def verify_signature_rsa(self, data: str, signature: str, public_key_pem: str) -> bool:
        """Verify RSA signature"""
        try:
            # Load public key
            public_key = serialization.load_pem_public_key(
                public_key_pem.encode('utf-8'),
                backend=default_backend()
            )
            
            # Verify signature
            signature_bytes = base64.b64decode(signature.encode('utf-8'))
            public_key.verify(
                signature_bytes,
                data.encode('utf-8'),
                padding.PSS(
                    mgf=padding.MGF1(hashes.SHA256()),
                    salt_length=padding.PSS.MAX_LENGTH
                ),
                hashes.SHA256()
            )
            
            return True
            
        except Exception as e:
            logger.error(f"RSA verification error: {str(e)}")
            return False


# Global protection instance
_protection = Protection()


# Operator functions for TuskLang
def encrypt_data(data: str, additional_data: str = None) -> str:
    """Execute @protection.encrypt operator"""
    try:
        return _protection.encrypt_data(data, additional_data)
    except Exception as e:
        logger.error(f"Protection encrypt error: {str(e)}")
        return f"@protection.encrypt({data}) - Error: {str(e)}"


def decrypt_data(encrypted_data: str, additional_data: str = None) -> str:
    """Execute @protection.decrypt operator"""
    try:
        return _protection.decrypt_data(encrypted_data, additional_data)
    except Exception as e:
        logger.error(f"Protection decrypt error: {str(e)}")
        return f"@protection.decrypt({encrypted_data}) - Error: {str(e)}"


def verify_integrity(data: str, signature: str) -> bool:
    """Execute @protection.verify operator"""
    try:
        return _protection.verify_integrity(data, signature)
    except Exception as e:
        logger.error(f"Protection verify error: {str(e)}")
        return False


def generate_signature(data: str) -> str:
    """Execute @protection.sign operator"""
    try:
        return _protection.generate_signature(data)
    except Exception as e:
        logger.error(f"Protection sign error: {str(e)}")
        return f"@protection.sign({data}) - Error: {str(e)}"


def obfuscate_code(source_code: str) -> str:
    """Execute @protection.obfuscate operator"""
    try:
        return _protection.obfuscate_code(source_code)
    except Exception as e:
        logger.error(f"Protection obfuscate error: {str(e)}")
        return f"@protection.obfuscate({source_code}) - Error: {str(e)}"


def detect_tampering() -> Dict[str, Any]:
    """Execute @protection.detect operator"""
    try:
        return _protection.detect_tampering()
    except Exception as e:
        logger.error(f"Protection detect error: {str(e)}")
        return {"error": str(e)}


def report_violation(violation_type: str, details: str) -> bool:
    """Execute @protection.report operator"""
    try:
        return _protection.report_violation(violation_type, details)
    except Exception as e:
        logger.error(f"Protection report error: {str(e)}")
        return False


# Convenience functions for direct use
def protect_string(data: str, operation: str = "encrypt") -> str:
    """Protect string data with specified operation"""
    if operation == "encrypt":
        return encrypt_data(data)
    elif operation == "sign":
        return generate_signature(data)
    elif operation == "obfuscate":
        return obfuscate_code(data)
    else:
        raise ValueError(f"Unknown protection operation: {operation}")


def verify_protected_data(data: str, signature: str = None, operation: str = "verify") -> bool:
    """Verify protected data"""
    if operation == "verify" and signature:
        return verify_integrity(data, signature)
    elif operation == "detect":
        result = detect_tampering()
        return not result.get("tampering_detected", True)
    else:
        raise ValueError(f"Unknown verification operation: {operation}")


# Test functions
def test_protection():
    """Test protection functionality"""
    print("Testing TuskLang Protection Module...")
    
    # Test data
    test_data = "Hello, TuskLang! This is a test message."
    
    # Test encryption/decryption
    print("\n1. Testing AES-256-GCM encryption/decryption...")
    encrypted = encrypt_data(test_data)
    print(f"Encrypted: {encrypted[:50]}...")
    decrypted = decrypt_data(encrypted)
    print(f"Decrypted: {decrypted}")
    print(f"Success: {test_data == decrypted}")
    
    # Test signing/verification
    print("\n2. Testing HMAC-SHA256 signing/verification...")
    signature = generate_signature(test_data)
    print(f"Signature: {signature[:50]}...")
    verified = verify_integrity(test_data, signature)
    print(f"Verified: {verified}")
    
    # Test code obfuscation
    print("\n3. Testing code obfuscation...")
    test_code = 'print("Hello, World!")\nreturn 42'
    obfuscated = obfuscate_code(test_code)
    print(f"Obfuscated: {obfuscated[:100]}...")
    deobfuscated = _protection.deobfuscate_code(obfuscated)
    print(f"Deobfuscated: {deobfuscated}")
    print(f"Success: {test_code == deobfuscated}")
    
    # Test tampering detection
    print("\n4. Testing tampering detection...")
    tampering_result = detect_tampering()
    print(f"Tampering result: {tampering_result}")
    
    # Test violation reporting
    print("\n5. Testing violation reporting...")
    violation_reported = report_violation("test_violation", "Test security violation")
    print(f"Violation reported: {violation_reported}")
    
    print("\nProtection module tests completed!")


if __name__ == '__main__':
    test_protection() 