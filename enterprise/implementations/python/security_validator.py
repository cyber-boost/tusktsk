#!/usr/bin/env python3
"""
TuskLang Security Validation
Digital signatures, encryption, and comprehensive security validation
"""

import os
import json
import time
import base64
import hashlib
import hmac
import secrets
from typing import Dict, List, Optional, Any, Tuple, Union
from dataclasses import dataclass
from pathlib import Path
import logging
from cryptography.hazmat.primitives import hashes, serialization
from cryptography.hazmat.primitives.asymmetric import rsa, ed25519, padding
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC
from cryptography.hazmat.primitives.kdf.hkdf import HKDF
from cryptography.hazmat.backends import default_backend
from cryptography.exceptions import InvalidKey, InvalidSignature
import struct

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

@dataclass
class SecurityConfig:
    """Security configuration"""
    signature_algorithm: str = 'ed25519'  # 'ed25519', 'rsa', 'hmac'
    encryption_algorithm: str = 'aes256_gcm'  # 'aes256_gcm', 'chacha20_poly1305'
    key_derivation: str = 'pbkdf2'  # 'pbkdf2', 'hkdf'
    hash_algorithm: str = 'sha256'  # 'sha256', 'sha384', 'sha512'
    key_size: int = 256
    salt_size: int = 32
    iv_size: int = 12
    tag_size: int = 16
    iterations: int = 100000
    max_key_age: int = 86400 * 365  # 1 year
    require_signature: bool = True
    require_encryption: bool = False
    allow_weak_algorithms: bool = False

@dataclass
class SecurityMetadata:
    """Security metadata for files"""
    signature: str
    signature_algorithm: str
    public_key_id: str
    timestamp: float
    key_version: int
    encryption_info: Optional[Dict[str, Any]] = None
    integrity_hash: Optional[str] = None
    metadata_hash: Optional[str] = None

class SecurityValidator:
    """Comprehensive security validation system"""
    
    def __init__(self, config: SecurityConfig):
        self.config = config
        self.keys = {}
        self.key_cache = {}
        self._load_keys()
    
    def _load_keys(self):
        """Load cryptographic keys"""
        try:
            # Load private key
            private_key_path = Path('keys/private_key.pem')
            if private_key_path.exists():
                with open(private_key_path, 'rb') as f:
                    if self.config.signature_algorithm == 'ed25519':
                        self.private_key = serialization.load_pem_private_key(
                            f.read(), password=None, backend=default_backend()
                        )
                    elif self.config.signature_algorithm == 'rsa':
                        self.private_key = serialization.load_pem_private_key(
                            f.read(), password=None, backend=default_backend()
                        )
            
            # Load public key
            public_key_path = Path('keys/public_key.pem')
            if public_key_path.exists():
                with open(public_key_path, 'rb') as f:
                    if self.config.signature_algorithm == 'ed25519':
                        self.public_key = serialization.load_pem_public_key(
                            f.read(), backend=default_backend()
                        )
                    elif self.config.signature_algorithm == 'rsa':
                        self.public_key = serialization.load_pem_public_key(
                            f.read(), backend=default_backend()
                        )
            
            # Load HMAC key
            hmac_key_path = Path('keys/hmac_key.bin')
            if hmac_key_path.exists():
                with open(hmac_key_path, 'rb') as f:
                    self.hmac_key = f.read()
            
        except Exception as e:
            logger.warning(f"Failed to load keys: {e}")
            self._generate_keys()
    
    def _generate_keys(self):
        """Generate new cryptographic keys"""
        keys_dir = Path('keys')
        keys_dir.mkdir(exist_ok=True)
        
        if self.config.signature_algorithm == 'ed25519':
            # Generate Ed25519 key pair
            self.private_key = ed25519.Ed25519PrivateKey.generate()
            self.public_key = self.private_key.public_key()
            
            # Save private key
            with open(keys_dir / 'private_key.pem', 'wb') as f:
                f.write(self.private_key.private_bytes(
                    encoding=serialization.Encoding.PEM,
                    format=serialization.PrivateFormat.PKCS8,
                    encryption_algorithm=serialization.NoEncryption()
                ))
            
            # Save public key
            with open(keys_dir / 'public_key.pem', 'wb') as f:
                f.write(self.public_key.public_bytes(
                    encoding=serialization.Encoding.PEM,
                    format=serialization.PublicFormat.SubjectPublicKeyInfo
                ))
        
        elif self.config.signature_algorithm == 'rsa':
            # Generate RSA key pair
            self.private_key = rsa.generate_private_key(
                public_exponent=65537,
                key_size=2048,
                backend=default_backend()
            )
            self.public_key = self.private_key.public_key()
            
            # Save private key
            with open(keys_dir / 'private_key.pem', 'wb') as f:
                f.write(self.private_key.private_bytes(
                    encoding=serialization.Encoding.PEM,
                    format=serialization.PrivateFormat.PKCS8,
                    encryption_algorithm=serialization.NoEncryption()
                ))
            
            # Save public key
            with open(keys_dir / 'public_key.pem', 'wb') as f:
                f.write(self.public_key.public_bytes(
                    encoding=serialization.Encoding.PEM,
                    format=serialization.PublicFormat.SubjectPublicKeyInfo
                ))
        
        # Generate HMAC key
        self.hmac_key = secrets.token_bytes(32)
        with open(keys_dir / 'hmac_key.bin', 'wb') as f:
            f.write(self.hmac_key)
        
        logger.info("Generated new cryptographic keys")
    
    def sign_data(self, data: bytes, metadata: Dict[str, Any] = None) -> SecurityMetadata:
        """Sign data and return security metadata"""
        if not self.config.require_signature:
            raise ValueError("Signatures are required but no signature algorithm configured")
        
        # Create metadata
        security_metadata = SecurityMetadata(
            signature="",
            signature_algorithm=self.config.signature_algorithm,
            public_key_id=self._get_public_key_id(),
            timestamp=time.time(),
            key_version=1,
            metadata_hash=self._hash_metadata(metadata) if metadata else None
        )
        
        # Create signature payload
        payload = self._create_signature_payload(data, security_metadata)
        
        # Sign payload
        if self.config.signature_algorithm == 'ed25519':
            signature = self.private_key.sign(payload)
        elif self.config.signature_algorithm == 'rsa':
            signature = self.private_key.sign(
                payload,
                padding.PSS(
                    mgf=padding.MGF1(hashes.SHA256()),
                    salt_length=padding.PSS.MAX_LENGTH
                ),
                hashes.SHA256()
            )
        elif self.config.signature_algorithm == 'hmac':
            signature = hmac.new(self.hmac_key, payload, hashlib.sha256).digest()
        else:
            raise ValueError(f"Unsupported signature algorithm: {self.config.signature_algorithm}")
        
        security_metadata.signature = base64.b64encode(signature).decode()
        
        return security_metadata
    
    def verify_signature(self, data: bytes, security_metadata: SecurityMetadata) -> bool:
        """Verify data signature"""
        try:
            # Create signature payload
            payload = self._create_signature_payload(data, security_metadata)
            
            # Decode signature
            signature = base64.b64decode(security_metadata.signature)
            
            # Verify signature
            if security_metadata.signature_algorithm == 'ed25519':
                self.public_key.verify(signature, payload)
            elif security_metadata.signature_algorithm == 'rsa':
                self.public_key.verify(
                    signature,
                    payload,
                    padding.PSS(
                        mgf=padding.MGF1(hashes.SHA256()),
                        salt_length=padding.PSS.MAX_LENGTH
                    ),
                    hashes.SHA256()
                )
            elif security_metadata.signature_algorithm == 'hmac':
                expected_signature = hmac.new(self.hmac_key, payload, hashlib.sha256).digest()
                if not hmac.compare_digest(signature, expected_signature):
                    raise InvalidSignature("HMAC signature verification failed")
            else:
                raise ValueError(f"Unsupported signature algorithm: {security_metadata.signature_algorithm}")
            
            # Verify timestamp
            if time.time() - security_metadata.timestamp > self.config.max_key_age:
                logger.warning("Signature timestamp is too old")
                return False
            
            return True
            
        except Exception as e:
            logger.error(f"Signature verification failed: {e}")
            return False
    
    def encrypt_data(self, data: bytes, key: Optional[bytes] = None) -> Tuple[bytes, Dict[str, Any]]:
        """Encrypt data"""
        if not self.config.require_encryption:
            return data, {}
        
        # Generate encryption key if not provided
        if key is None:
            key = secrets.token_bytes(self.config.key_size // 8)
        
        # Generate salt and IV
        salt = secrets.token_bytes(self.config.salt_size)
        iv = secrets.token_bytes(self.config.iv_size)
        
        # Derive encryption key
        if self.config.key_derivation == 'pbkdf2':
            kdf = PBKDF2HMAC(
                algorithm=hashes.SHA256(),
                length=self.config.key_size // 8,
                salt=salt,
                iterations=self.config.iterations,
                backend=default_backend()
            )
            derived_key = kdf.derive(key)
        elif self.config.key_derivation == 'hkdf':
            kdf = HKDF(
                algorithm=hashes.SHA256(),
                length=self.config.key_size // 8,
                salt=salt,
                info=b'tusklang_encryption',
                backend=default_backend()
            )
            derived_key = kdf.derive(key)
        else:
            raise ValueError(f"Unsupported key derivation: {self.config.key_derivation}")
        
        # Encrypt data
        if self.config.encryption_algorithm == 'aes256_gcm':
            cipher = Cipher(
                algorithms.AES(derived_key),
                modes.GCM(iv),
                backend=default_backend()
            )
            encryptor = cipher.encryptor()
            ciphertext = encryptor.update(data) + encryptor.finalize()
            tag = encryptor.tag
        elif self.config.encryption_algorithm == 'chacha20_poly1305':
            cipher = Cipher(
                algorithms.ChaCha20(derived_key, iv),
                modes.Poly1305(),
                backend=default_backend()
            )
            encryptor = cipher.encryptor()
            ciphertext = encryptor.update(data) + encryptor.finalize()
            tag = encryptor.tag
        else:
            raise ValueError(f"Unsupported encryption algorithm: {self.config.encryption_algorithm}")
        
        # Create encryption info
        encryption_info = {
            'algorithm': self.config.encryption_algorithm,
            'key_derivation': self.config.key_derivation,
            'salt': base64.b64encode(salt).decode(),
            'iv': base64.b64encode(iv).decode(),
            'tag': base64.b64encode(tag).decode(),
            'iterations': self.config.iterations
        }
        
        return ciphertext, encryption_info
    
    def decrypt_data(self, encrypted_data: bytes, encryption_info: Dict[str, Any], key: bytes) -> bytes:
        """Decrypt data"""
        try:
            # Extract parameters
            salt = base64.b64decode(encryption_info['salt'])
            iv = base64.b64decode(encryption_info['iv'])
            tag = base64.b64decode(encryption_info['tag'])
            
            # Derive decryption key
            if encryption_info['key_derivation'] == 'pbkdf2':
                kdf = PBKDF2HMAC(
                    algorithm=hashes.SHA256(),
                    length=self.config.key_size // 8,
                    salt=salt,
                    iterations=encryption_info['iterations'],
                    backend=default_backend()
                )
                derived_key = kdf.derive(key)
            elif encryption_info['key_derivation'] == 'hkdf':
                kdf = HKDF(
                    algorithm=hashes.SHA256(),
                    length=self.config.key_size // 8,
                    salt=salt,
                    info=b'tusklang_encryption',
                    backend=default_backend()
                )
                derived_key = kdf.derive(key)
            else:
                raise ValueError(f"Unsupported key derivation: {encryption_info['key_derivation']}")
            
            # Decrypt data
            if encryption_info['algorithm'] == 'aes256_gcm':
                cipher = Cipher(
                    algorithms.AES(derived_key),
                    modes.GCM(iv, tag),
                    backend=default_backend()
                )
                decryptor = cipher.decryptor()
                plaintext = decryptor.update(encrypted_data) + decryptor.finalize()
            elif encryption_info['algorithm'] == 'chacha20_poly1305':
                cipher = Cipher(
                    algorithms.ChaCha20(derived_key, iv),
                    modes.Poly1305(tag),
                    backend=default_backend()
                )
                decryptor = cipher.decryptor()
                plaintext = decryptor.update(encrypted_data) + decryptor.finalize()
            else:
                raise ValueError(f"Unsupported encryption algorithm: {encryption_info['algorithm']}")
            
            return plaintext
            
        except Exception as e:
            logger.error(f"Decryption failed: {e}")
            raise
    
    def validate_file_integrity(self, file_path: Path) -> Dict[str, Any]:
        """Validate file integrity and security"""
        results = {
            'file_path': str(file_path),
            'valid': False,
            'checksum_valid': False,
            'signature_valid': False,
            'encryption_valid': False,
            'errors': [],
            'warnings': []
        }
        
        try:
            if not file_path.exists():
                results['errors'].append("File does not exist")
                return results
            
            # Read file
            with open(file_path, 'rb') as f:
                file_data = f.read()
            
            # Check file size
            if len(file_data) == 0:
                results['errors'].append("File is empty")
                return results
            
            # Calculate checksum
            expected_checksum = hashlib.sha256(file_data).hexdigest()
            results['checksum'] = expected_checksum
            results['checksum_valid'] = True
            
            # Check for security metadata
            metadata_file = file_path.with_suffix(file_path.suffix + '.meta')
            if metadata_file.exists():
                with open(metadata_file, 'r') as f:
                    security_metadata = SecurityMetadata(**json.load(f))
                
                # Verify signature
                if self.config.require_signature:
                    signature_valid = self.verify_signature(file_data, security_metadata)
                    results['signature_valid'] = signature_valid
                    if not signature_valid:
                        results['errors'].append("Signature verification failed")
                
                # Check encryption info
                if security_metadata.encryption_info:
                    results['encryption_info'] = security_metadata.encryption_info
                    results['encryption_valid'] = True
                
                # Check timestamp
                age = time.time() - security_metadata.timestamp
                if age > self.config.max_key_age:
                    results['warnings'].append(f"File is {age/86400:.1f} days old")
                
                results['security_metadata'] = security_metadata
            else:
                if self.config.require_signature:
                    results['errors'].append("No security metadata found")
                else:
                    results['warnings'].append("No security metadata found")
            
            # Overall validation
            results['valid'] = (
                results['checksum_valid'] and
                (not self.config.require_signature or results['signature_valid'])
            )
            
        except Exception as e:
            results['errors'].append(f"Validation failed: {str(e)}")
        
        return results
    
    def secure_file_write(self, file_path: Path, data: bytes, metadata: Dict[str, Any] = None,
                         encrypt: bool = False, key: Optional[bytes] = None) -> bool:
        """Securely write file with signature and optional encryption"""
        try:
            # Encrypt if requested
            if encrypt:
                encrypted_data, encryption_info = self.encrypt_data(data, key)
                write_data = encrypted_data
            else:
                encryption_info = None
                write_data = data
            
            # Sign data
            security_metadata = self.sign_data(write_data, metadata)
            if encryption_info:
                security_metadata.encryption_info = encryption_info
            
            # Write file
            with open(file_path, 'wb') as f:
                f.write(write_data)
            
            # Write security metadata
            metadata_file = file_path.with_suffix(file_path.suffix + '.meta')
            with open(metadata_file, 'w') as f:
                json.dump(asdict(security_metadata), f, indent=2)
            
            logger.info(f"Securely wrote file: {file_path}")
            return True
            
        except Exception as e:
            logger.error(f"Failed to securely write file: {e}")
            return False
    
    def secure_file_read(self, file_path: Path, key: Optional[bytes] = None) -> Tuple[bytes, Dict[str, Any]]:
        """Securely read file with signature verification and decryption"""
        try:
            # Read file
            with open(file_path, 'rb') as f:
                file_data = f.read()
            
            # Read security metadata
            metadata_file = file_path.with_suffix(file_path.suffix + '.meta')
            if not metadata_file.exists():
                raise ValueError("No security metadata found")
            
            with open(metadata_file, 'r') as f:
                security_metadata = SecurityMetadata(**json.load(f))
            
            # Verify signature
            if not self.verify_signature(file_data, security_metadata):
                raise ValueError("Signature verification failed")
            
            # Decrypt if needed
            if security_metadata.encryption_info:
                if key is None:
                    raise ValueError("Encryption key required for encrypted file")
                decrypted_data = self.decrypt_data(file_data, security_metadata.encryption_info, key)
                return decrypted_data, asdict(security_metadata)
            else:
                return file_data, asdict(security_metadata)
            
        except Exception as e:
            logger.error(f"Failed to securely read file: {e}")
            raise
    
    def _create_signature_payload(self, data: bytes, metadata: SecurityMetadata) -> bytes:
        """Create payload for signature"""
        # Include data hash, timestamp, and metadata hash
        data_hash = hashlib.sha256(data).digest()
        timestamp_bytes = struct.pack('>Q', int(metadata.timestamp))
        
        payload = data_hash + timestamp_bytes
        
        if metadata.metadata_hash:
            payload += base64.b64decode(metadata.metadata_hash)
        
        return payload
    
    def _hash_metadata(self, metadata: Dict[str, Any]) -> str:
        """Hash metadata for signature"""
        metadata_json = json.dumps(metadata, sort_keys=True, separators=(',', ':'))
        return base64.b64encode(hashlib.sha256(metadata_json.encode()).digest()).decode()
    
    def _get_public_key_id(self) -> str:
        """Get public key identifier"""
        if hasattr(self, 'public_key'):
            public_key_bytes = self.public_key.public_bytes(
                encoding=serialization.Encoding.DER,
                format=serialization.PublicFormat.SubjectPublicKeyInfo
            )
            return hashlib.sha256(public_key_bytes).hexdigest()[:16]
        return "unknown"
    
    def validate_configuration(self) -> Dict[str, Any]:
        """Validate security configuration"""
        results = {
            'valid': True,
            'errors': [],
            'warnings': [],
            'recommendations': []
        }
        
        # Check signature algorithm
        if self.config.signature_algorithm not in ['ed25519', 'rsa', 'hmac']:
            results['errors'].append(f"Unsupported signature algorithm: {self.config.signature_algorithm}")
            results['valid'] = False
        
        # Check encryption algorithm
        if self.config.encryption_algorithm not in ['aes256_gcm', 'chacha20_poly1305']:
            results['errors'].append(f"Unsupported encryption algorithm: {self.config.encryption_algorithm}")
            results['valid'] = False
        
        # Check key derivation
        if self.config.key_derivation not in ['pbkdf2', 'hkdf']:
            results['errors'].append(f"Unsupported key derivation: {self.config.key_derivation}")
            results['valid'] = False
        
        # Check hash algorithm
        if self.config.hash_algorithm not in ['sha256', 'sha384', 'sha512']:
            results['errors'].append(f"Unsupported hash algorithm: {self.config.hash_algorithm}")
            results['valid'] = False
        
        # Check key sizes
        if self.config.key_size < 256:
            results['warnings'].append("Key size should be at least 256 bits")
        
        if self.config.iterations < 100000:
            results['warnings'].append("PBKDF2 iterations should be at least 100,000")
        
        # Check for weak algorithms
        if not self.config.allow_weak_algorithms:
            if self.config.signature_algorithm == 'hmac':
                results['warnings'].append("HMAC signatures are less secure than Ed25519 or RSA")
            
            if self.config.hash_algorithm == 'sha256':
                results['recommendations'].append("Consider using SHA-384 or SHA-512 for better security")
        
        # Check key availability
        if self.config.require_signature:
            if not hasattr(self, 'private_key') or not hasattr(self, 'public_key'):
                results['errors'].append("Signature keys not available")
                results['valid'] = False
        
        return results
    
    def generate_security_report(self, directory: Path) -> Dict[str, Any]:
        """Generate comprehensive security report for directory"""
        report = {
            'directory': str(directory),
            'scan_time': time.time(),
            'total_files': 0,
            'valid_files': 0,
            'invalid_files': 0,
            'encrypted_files': 0,
            'signed_files': 0,
            'files': [],
            'summary': {},
            'recommendations': []
        }
        
        try:
            # Scan directory
            for file_path in directory.rglob('*'):
                if file_path.is_file() and not file_path.name.endswith('.meta'):
                    file_result = self.validate_file_integrity(file_path)
                    report['files'].append(file_result)
                    report['total_files'] += 1
                    
                    if file_result['valid']:
                        report['valid_files'] += 1
                    else:
                        report['invalid_files'] += 1
                    
                    if file_result.get('encryption_valid'):
                        report['encrypted_files'] += 1
                    
                    if file_result.get('signature_valid'):
                        report['signed_files'] += 1
            
            # Generate summary
            if report['total_files'] > 0:
                report['summary'] = {
                    'valid_percentage': (report['valid_files'] / report['total_files']) * 100,
                    'encrypted_percentage': (report['encrypted_files'] / report['total_files']) * 100,
                    'signed_percentage': (report['signed_files'] / report['total_files']) * 100
                }
            
            # Generate recommendations
            if report['invalid_files'] > 0:
                report['recommendations'].append(f"Fix {report['invalid_files']} invalid files")
            
            if report['encrypted_files'] == 0 and self.config.require_encryption:
                report['recommendations'].append("Enable encryption for sensitive files")
            
            if report['signed_files'] == 0 and self.config.require_signature:
                report['recommendations'].append("Enable signatures for all files")
            
        except Exception as e:
            report['error'] = str(e)
        
        return report

# Example usage
if __name__ == '__main__':
    # Create security configuration
    config = SecurityConfig(
        signature_algorithm='ed25519',
        encryption_algorithm='aes256_gcm',
        key_derivation='pbkdf2',
        hash_algorithm='sha256',
        require_signature=True,
        require_encryption=False
    )
    
    # Initialize security validator
    validator = SecurityValidator(config)
    
    # Validate configuration
    config_validation = validator.validate_configuration()
    print(f"Configuration valid: {config_validation['valid']}")
    
    if config_validation['errors']:
        print("Errors:", config_validation['errors'])
    
    if config_validation['warnings']:
        print("Warnings:", config_validation['warnings'])
    
    # Example: Secure file operations
    test_data = b"This is sensitive data that needs to be secured"
    test_file = Path('test_secure_file.bin')
    
    # Write file securely
    success = validator.secure_file_write(test_file, test_data, {'description': 'Test file'})
    print(f"Secure write: {success}")
    
    # Read file securely
    try:
        read_data, metadata = validator.secure_file_read(test_file)
        print(f"Secure read: {read_data == test_data}")
        print(f"Metadata: {metadata}")
    except Exception as e:
        print(f"Secure read failed: {e}")
    
    # Validate file integrity
    validation = validator.validate_file_integrity(test_file)
    print(f"File validation: {validation['valid']}")
    
    # Generate security report
    report = validator.generate_security_report(Path('.'))
    print(f"Security report: {report['valid_files']}/{report['total_files']} files valid") 