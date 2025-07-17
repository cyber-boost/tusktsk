#!/usr/bin/env python3
"""
TuskLang Package Registry Secure Storage System
Encrypted and secure storage for package files and metadata
"""

import os
import json
import hashlib
import base64
from cryptography.fernet import Fernet
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC
from typing import Dict, Optional, Tuple, List
from dataclasses import dataclass, asdict
import time

@dataclass
class PackageMetadata:
    """Package metadata with security information"""
    package_id: str
    name: str
    version: str
    author: str
    description: str
    dependencies: List[str]
    checksum: str
    signature: str
    uploaded_at: float
    uploaded_by: str
    file_size: int
    encryption_key_id: str
    access_level: str
    is_verified: bool = False
    verification_date: Optional[float] = None

class SecurePackageStorage:
    """Secure storage system for package files and metadata"""
    
    def __init__(self, storage_path: str, master_key: str):
        self.storage_path = storage_path
        self.master_key = master_key.encode('utf-8')
        self.encryption_keys: Dict[str, bytes] = {}
        self.package_metadata: Dict[str, PackageMetadata] = {}
        
        # Ensure storage directories exist
        os.makedirs(storage_path, exist_ok=True)
        os.makedirs(os.path.join(storage_path, 'packages'), exist_ok=True)
        os.makedirs(os.path.join(storage_path, 'metadata'), exist_ok=True)
        os.makedirs(os.path.join(storage_path, 'keys'), exist_ok=True)
        
        self._load_existing_data()
    
    def _load_existing_data(self):
        """Load existing package metadata and keys"""
        metadata_path = os.path.join(self.storage_path, 'metadata')
        keys_path = os.path.join(self.storage_path, 'keys')
        
        # Load metadata
        for filename in os.listdir(metadata_path):
            if filename.endswith('.json'):
                filepath = os.path.join(metadata_path, filename)
                try:
                    with open(filepath, 'r') as f:
                        data = json.load(f)
                        metadata = PackageMetadata(**data)
                        self.package_metadata[metadata.package_id] = metadata
                except Exception as e:
                    print(f"Error loading metadata {filename}: {e}")
        
        # Load encryption keys
        for filename in os.listdir(keys_path):
            if filename.endswith('.key'):
                key_id = filename[:-4]
                filepath = os.path.join(keys_path, filename)
                try:
                    with open(filepath, 'rb') as f:
                        encrypted_key = f.read()
                        key = self._decrypt_key(encrypted_key)
                        self.encryption_keys[key_id] = key
                except Exception as e:
                    print(f"Error loading key {filename}: {e}")
    
    def _generate_encryption_key(self) -> Tuple[str, bytes]:
        """Generate a new encryption key"""
        key_id = hashlib.sha256(f"{time.time()}:{os.urandom(16)}".encode()).hexdigest()[:16]
        key = Fernet.generate_key()
        return key_id, key
    
    def _encrypt_key(self, key: bytes) -> bytes:
        """Encrypt a key using the master key"""
        salt = os.urandom(16)
        kdf = PBKDF2HMAC(
            algorithm=hashes.SHA256(),
            length=32,
            salt=salt,
            iterations=100000,
        )
        derived_key = base64.urlsafe_b64encode(kdf.derive(self.master_key))
        fernet = Fernet(derived_key)
        return salt + fernet.encrypt(key)
    
    def _decrypt_key(self, encrypted_key_data: bytes) -> bytes:
        """Decrypt a key using the master key"""
        salt = encrypted_key_data[:16]
        encrypted_key = encrypted_key_data[16:]
        
        kdf = PBKDF2HMAC(
            algorithm=hashes.SHA256(),
            length=32,
            salt=salt,
            iterations=100000,
        )
        derived_key = base64.urlsafe_b64encode(kdf.derive(self.master_key))
        fernet = Fernet(derived_key)
        return fernet.decrypt(encrypted_key)
    
    def store_package(self, package_data: bytes, metadata: PackageMetadata) -> bool:
        """Store package file with encryption"""
        try:
            # Generate encryption key for this package
            key_id, key = self._generate_encryption_key()
            
            # Encrypt package data
            fernet = Fernet(key)
            encrypted_data = fernet.encrypt(package_data)
            
            # Store encrypted package
            package_path = os.path.join(self.storage_path, 'packages', f"{metadata.package_id}.enc")
            with open(package_path, 'wb') as f:
                f.write(encrypted_data)
            
            # Store encryption key
            encrypted_key = self._encrypt_key(key)
            key_path = os.path.join(self.storage_path, 'keys', f"{key_id}.key")
            with open(key_path, 'wb') as f:
                f.write(encrypted_key)
            
            # Update metadata
            metadata.encryption_key_id = key_id
            metadata.uploaded_at = time.time()
            
            # Store metadata
            self.package_metadata[metadata.package_id] = metadata
            metadata_path = os.path.join(self.storage_path, 'metadata', f"{metadata.package_id}.json")
            with open(metadata_path, 'w') as f:
                json.dump(asdict(metadata), f, indent=2)
            
            # Store key in memory
            self.encryption_keys[key_id] = key
            
            return True
            
        except Exception as e:
            print(f"Error storing package {metadata.package_id}: {e}")
            return False
    
    def retrieve_package(self, package_id: str) -> Optional[bytes]:
        """Retrieve and decrypt package file"""
        try:
            if package_id not in self.package_metadata:
                return None
            
            metadata = self.package_metadata[package_id]
            
            # Load encrypted package
            package_path = os.path.join(self.storage_path, 'packages', f"{package_id}.enc")
            with open(package_path, 'rb') as f:
                encrypted_data = f.read()
            
            # Get encryption key
            if metadata.encryption_key_id not in self.encryption_keys:
                # Load key from disk
                key_path = os.path.join(self.storage_path, 'keys', f"{metadata.encryption_key_id}.key")
                with open(key_path, 'rb') as f:
                    encrypted_key = f.read()
                key = self._decrypt_key(encrypted_key)
                self.encryption_keys[metadata.encryption_key_id] = key
            
            key = self.encryption_keys[metadata.encryption_key_id]
            
            # Decrypt package
            fernet = Fernet(key)
            package_data = fernet.decrypt(encrypted_data)
            
            # Verify checksum
            calculated_checksum = hashlib.sha256(package_data).hexdigest()
            if calculated_checksum != metadata.checksum:
                raise ValueError("Package checksum verification failed")
            
            return package_data
            
        except Exception as e:
            print(f"Error retrieving package {package_id}: {e}")
            return None
    
    def get_package_metadata(self, package_id: str) -> Optional[PackageMetadata]:
        """Get package metadata"""
        return self.package_metadata.get(package_id)
    
    def list_packages(self) -> List[str]:
        """List all package IDs"""
        return list(self.package_metadata.keys())
    
    def delete_package(self, package_id: str) -> bool:
        """Delete package and all associated data"""
        try:
            if package_id not in self.package_metadata:
                return False
            
            metadata = self.package_metadata[package_id]
            
            # Delete encrypted package file
            package_path = os.path.join(self.storage_path, 'packages', f"{package_id}.enc")
            if os.path.exists(package_path):
                os.remove(package_path)
            
            # Delete encryption key
            key_path = os.path.join(self.storage_path, 'keys', f"{metadata.encryption_key_id}.key")
            if os.path.exists(key_path):
                os.remove(key_path)
            
            # Delete metadata
            metadata_path = os.path.join(self.storage_path, 'metadata', f"{package_id}.json")
            if os.path.exists(metadata_path):
                os.remove(metadata_path)
            
            # Remove from memory
            del self.package_metadata[package_id]
            if metadata.encryption_key_id in self.encryption_keys:
                del self.encryption_keys[metadata.encryption_key_id]
            
            return True
            
        except Exception as e:
            print(f"Error deleting package {package_id}: {e}")
            return False
    
    def verify_package_integrity(self, package_id: str) -> bool:
        """Verify package integrity and update verification status"""
        try:
            package_data = self.retrieve_package(package_id)
            if package_data is None:
                return False
            
            metadata = self.package_metadata[package_id]
            calculated_checksum = hashlib.sha256(package_data).hexdigest()
            
            if calculated_checksum == metadata.checksum:
                metadata.is_verified = True
                metadata.verification_date = time.time()
                
                # Update metadata file
                metadata_path = os.path.join(self.storage_path, 'metadata', f"{package_id}.json")
                with open(metadata_path, 'w') as f:
                    json.dump(asdict(metadata), f, indent=2)
                
                return True
            else:
                metadata.is_verified = False
                return False
                
        except Exception as e:
            print(f"Error verifying package {package_id}: {e}")
            return False

# Global secure storage instance
secure_storage = SecurePackageStorage("/var/tusklang/registry", "tusklang-master-key-2025") 