#!/usr/bin/env python3
"""
TuskLang Package Registry Download Protection System
Secure download channels with integrity verification and checksum validation
"""

import hashlib
import hmac
import time
import base64
from typing import Dict, Optional, Tuple, List
from dataclasses import dataclass
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
import os

@dataclass
class DownloadToken:
    """Secure download token for package access"""
    token_id: str
    package_id: str
    user_id: str
    expires_at: float
    download_limit: int
    downloads_used: int = 0
    created_at: float = None
    
    def __post_init__(self):
        if self.created_at is None:
            self.created_at = time.time()
    
    def is_valid(self) -> bool:
        """Check if token is still valid"""
        return (time.time() < self.expires_at and 
                self.downloads_used < self.download_limit)

@dataclass
class PackageIntegrity:
    """Package integrity information"""
    package_id: str
    sha256_checksum: str
    sha512_checksum: str
    blake2b_checksum: str
    file_size: int
    signature: str
    signed_by: str
    signed_at: float

class DownloadProtection:
    """Download protection system with integrity verification"""
    
    def __init__(self, secret_key: str):
        self.secret_key = secret_key.encode('utf-8')
        self.download_tokens: Dict[str, DownloadToken] = {}
        self.package_integrity: Dict[str, PackageIntegrity] = {}
        self.download_logs: List[Dict] = []
        
    def create_download_token(self, package_id: str, user_id: str, 
                            expires_in: int = 3600, download_limit: int = 10) -> str:
        """Create a secure download token"""
        token_id = self._generate_token_id(package_id, user_id)
        expires_at = time.time() + expires_in
        
        token = DownloadToken(
            token_id=token_id,
            package_id=package_id,
            user_id=user_id,
            expires_at=expires_at,
            download_limit=download_limit
        )
        
        self.download_tokens[token_id] = token
        
        # Create signed token
        token_data = f"{token_id}:{package_id}:{user_id}:{expires_at}:{download_limit}"
        signature = self._sign_data(token_data.encode())
        
        return base64.urlsafe_b64encode(f"{token_data}:{signature}".encode()).decode()
    
    def validate_download_token(self, token_string: str) -> Optional[DownloadToken]:
        """Validate and decode download token"""
        try:
            decoded = base64.urlsafe_b64decode(token_string.encode()).decode()
            parts = decoded.split(':')
            
            if len(parts) != 6:
                return None
            
            token_id, package_id, user_id, expires_at_str, download_limit_str, signature = parts
            
            # Verify signature
            token_data = f"{token_id}:{package_id}:{user_id}:{expires_at_str}:{download_limit_str}"
            expected_signature = self._sign_data(token_data.encode())
            
            if not hmac.compare_digest(signature, expected_signature):
                return None
            
            # Check if token exists and is valid
            if token_id not in self.download_tokens:
                return None
            
            token = self.download_tokens[token_id]
            if not token.is_valid():
                return None
            
            return token
            
        except Exception as e:
            print(f"Error validating download token: {e}")
            return None
    
    def record_download(self, token_id: str, ip_address: str, user_agent: str) -> bool:
        """Record a download attempt"""
        if token_id not in self.download_tokens:
            return False
        
        token = self.download_tokens[token_id]
        if not token.is_valid():
            return False
        
        # Increment download count
        token.downloads_used += 1
        
        # Log download
        log_entry = {
            'timestamp': time.time(),
            'token_id': token_id,
            'package_id': token.package_id,
            'user_id': token.user_id,
            'ip_address': ip_address,
            'user_agent': user_agent,
            'downloads_used': token.downloads_used
        }
        
        self.download_logs.append(log_entry)
        return True
    
    def calculate_package_checksums(self, package_data: bytes) -> Dict[str, str]:
        """Calculate multiple checksums for package integrity"""
        return {
            'sha256': hashlib.sha256(package_data).hexdigest(),
            'sha512': hashlib.sha512(package_data).hexdigest(),
            'blake2b': hashlib.blake2b(package_data).hexdigest()
        }
    
    def verify_package_integrity(self, package_id: str, package_data: bytes) -> bool:
        """Verify package integrity against stored checksums"""
        if package_id not in self.package_integrity:
            return False
        
        integrity = self.package_integrity[package_id]
        calculated_checksums = self.calculate_package_checksums(package_data)
        
        return (calculated_checksums['sha256'] == integrity.sha256_checksum and
                calculated_checksums['sha512'] == integrity.sha512_checksum and
                calculated_checksums['blake2b'] == integrity.blake2b_checksum and
                len(package_data) == integrity.file_size)
    
    def store_package_integrity(self, package_id: str, package_data: bytes, 
                               signature: str, signed_by: str) -> bool:
        """Store package integrity information"""
        checksums = self.calculate_package_checksums(package_data)
        
        integrity = PackageIntegrity(
            package_id=package_id,
            sha256_checksum=checksums['sha256'],
            sha512_checksum=checksums['sha512'],
            blake2b_checksum=checksums['blake2b'],
            file_size=len(package_data),
            signature=signature,
            signed_by=signed_by,
            signed_at=time.time()
        )
        
        self.package_integrity[package_id] = integrity
        return True
    
    def create_secure_download_url(self, package_id: str, user_id: str, 
                                 base_url: str) -> str:
        """Create a secure download URL with token"""
        token_string = self.create_download_token(package_id, user_id)
        return f"{base_url}/download/{package_id}?token={token_string}"
    
    def get_download_statistics(self, package_id: str) -> Dict:
        """Get download statistics for a package"""
        package_downloads = [log for log in self.download_logs 
                           if log['package_id'] == package_id]
        
        return {
            'total_downloads': len(package_downloads),
            'unique_users': len(set(log['user_id'] for log in package_downloads)),
            'recent_downloads': len([log for log in package_downloads 
                                   if time.time() - log['timestamp'] < 86400])
        }
    
    def _generate_token_id(self, package_id: str, user_id: str) -> str:
        """Generate unique token ID"""
        data = f"{package_id}:{user_id}:{time.time()}:{os.urandom(16).hex()}".encode()
        return hashlib.sha256(data).hexdigest()[:16]
    
    def _sign_data(self, data: bytes) -> str:
        """Sign data using HMAC"""
        return hmac.new(self.secret_key, data, hashlib.sha256).hexdigest()

class SecureDownloadChannel:
    """Secure download channel with encryption and rate limiting"""
    
    def __init__(self, encryption_key: str):
        self.encryption_key = encryption_key.encode('utf-8')
        self.download_protection = DownloadProtection(encryption_key)
        self.rate_limits: Dict[str, List[float]] = {}  # ip -> list of timestamps
        
    def encrypt_package_for_download(self, package_data: bytes, 
                                   user_id: str) -> Tuple[bytes, str]:
        """Encrypt package data for secure download"""
        # Generate unique encryption key for this download
        download_key = os.urandom(32)
        
        # Encrypt package with download key
        cipher = Cipher(algorithms.AES(download_key), modes.GCM())
        encryptor = cipher.encryptor()
        encrypted_data = encryptor.update(package_data) + encryptor.finalize()
        
        # Encrypt download key with user's key
        user_cipher = Cipher(algorithms.AES(self.encryption_key), modes.GCM())
        user_encryptor = user_cipher.encryptor()
        encrypted_key = user_encryptor.update(download_key) + user_encryptor.finalize()
        
        return encrypted_data, base64.b64encode(encrypted_key).decode()
    
    def decrypt_package_download(self, encrypted_data: bytes, 
                               encrypted_key: str) -> Optional[bytes]:
        """Decrypt package data from download"""
        try:
            # Decrypt download key
            key_data = base64.b64decode(encrypted_key)
            user_cipher = Cipher(algorithms.AES(self.encryption_key), modes.GCM())
            user_decryptor = user_cipher.decryptor()
            download_key = user_decryptor.update(key_data) + user_decryptor.finalize()
            
            # Decrypt package data
            cipher = Cipher(algorithms.AES(download_key), modes.GCM())
            decryptor = cipher.decryptor()
            package_data = decryptor.update(encrypted_data) + decryptor.finalize()
            
            return package_data
            
        except Exception as e:
            print(f"Error decrypting package download: {e}")
            return None
    
    def check_rate_limit(self, ip_address: str, max_downloads: int = 10, 
                        window_seconds: int = 3600) -> bool:
        """Check if IP address is within rate limits"""
        current_time = time.time()
        
        if ip_address not in self.rate_limits:
            self.rate_limits[ip_address] = []
        
        # Remove old timestamps
        self.rate_limits[ip_address] = [
            ts for ts in self.rate_limits[ip_address]
            if current_time - ts < window_seconds
        ]
        
        # Check if within limit
        if len(self.rate_limits[ip_address]) >= max_downloads:
            return False
        
        # Add current timestamp
        self.rate_limits[ip_address].append(current_time)
        return True

# Global download protection instance
download_protection = DownloadProtection("tusklang-download-secret-2025")
secure_channel = SecureDownloadChannel("tusklang-channel-key-2025") 