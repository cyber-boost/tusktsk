#!/usr/bin/env python3
"""
TuskLang Package Registry Package Signing System
Digital signatures and verification for package integrity
"""

import hashlib
import time
import base64
from typing import Dict, Optional, Tuple, List
from dataclasses import dataclass
from cryptography.hazmat.primitives import hashes, serialization
from cryptography.hazmat.primitives.asymmetric import rsa, padding, ed25519
from cryptography.hazmat.primitives.serialization import load_pem_private_key, load_pem_public_key
from cryptography.exceptions import InvalidSignature
import json
import os
from typing import Set

@dataclass
class SignatureInfo:
    """Package signature information"""
    package_id: str
    signature: str
    signed_by: str
    signed_at: float
    algorithm: str
    public_key_id: str
    signature_version: str = "1.0"

@dataclass
class KeyPair:
    """Key pair for signing"""
    key_id: str
    public_key: bytes
    private_key: bytes
    algorithm: str
    created_at: float
    expires_at: Optional[float] = None
    is_active: bool = True

class PackageSigner:
    """Package signing system with digital signatures"""
    
    def __init__(self, keys_path: str):
        self.keys_path = keys_path
        self.key_pairs: Dict[str, KeyPair] = {}
        self.package_signatures: Dict[str, SignatureInfo] = {}
        self.trusted_keys: Dict[str, bytes] = {}
        
        # Ensure keys directory exists
        os.makedirs(keys_path, exist_ok=True)
        self._load_existing_keys()
    
    def _load_existing_keys(self):
        """Load existing key pairs from storage"""
        keys_file = os.path.join(self.keys_path, 'keys.json')
        if os.path.exists(keys_file):
            try:
                with open(keys_file, 'r') as f:
                    data = json.load(f)
                    for key_id, key_data in data.items():
                        key_pair = KeyPair(
                            key_id=key_id,
                            public_key=base64.b64decode(key_data['public_key']),
                            private_key=base64.b64decode(key_data['private_key']),
                            algorithm=key_data['algorithm'],
                            created_at=key_data['created_at'],
                            expires_at=key_data.get('expires_at'),
                            is_active=key_data.get('is_active', True)
                        )
                        self.key_pairs[key_id] = key_pair
            except Exception as e:
                print(f"Error loading keys: {e}")
    
    def _save_keys(self):
        """Save key pairs to storage"""
        keys_file = os.path.join(self.keys_path, 'keys.json')
        try:
            data = {}
            for key_id, key_pair in self.key_pairs.items():
                data[key_id] = {
                    'public_key': base64.b64encode(key_pair.public_key).decode(),
                    'private_key': base64.b64encode(key_pair.private_key).decode(),
                    'algorithm': key_pair.algorithm,
                    'created_at': key_pair.created_at,
                    'expires_at': key_pair.expires_at,
                    'is_active': key_pair.is_active
                }
            
            with open(keys_file, 'w') as f:
                json.dump(data, f, indent=2)
        except Exception as e:
            print(f"Error saving keys: {e}")
    
    def generate_key_pair(self, algorithm: str = "RSA", key_size: int = 2048) -> str:
        """Generate a new key pair"""
        key_id = self._generate_key_id()
        
        if algorithm.upper() == "RSA":
            private_key = rsa.generate_private_key(
                public_exponent=65537,
                key_size=key_size
            )
            public_key = private_key.public_key()
            
            private_pem = private_key.private_bytes(
                encoding=serialization.Encoding.PEM,
                format=serialization.PrivateFormat.PKCS8,
                encryption_algorithm=serialization.NoEncryption()
            )
            
            public_pem = public_key.public_bytes(
                encoding=serialization.Encoding.PEM,
                format=serialization.PublicFormat.SubjectPublicKeyInfo
            )
            
        elif algorithm.upper() == "ED25519":
            private_key = ed25519.Ed25519PrivateKey.generate()
            public_key = private_key.public_key()
            
            private_pem = private_key.private_bytes(
                encoding=serialization.Encoding.PEM,
                format=serialization.PrivateFormat.PKCS8,
                encryption_algorithm=serialization.NoEncryption()
            )
            
            public_pem = public_key.public_bytes(
                encoding=serialization.Encoding.PEM,
                format=serialization.PublicFormat.SubjectPublicKeyInfo
            )
        
        else:
            raise ValueError(f"Unsupported algorithm: {algorithm}")
        
        key_pair = KeyPair(
            key_id=key_id,
            public_key=public_pem,
            private_key=private_pem,
            algorithm=algorithm.upper(),
            created_at=time.time()
        )
        
        self.key_pairs[key_id] = key_pair
        self._save_keys()
        
        return key_id
    
    def sign_package(self, package_id: str, package_data: bytes, 
                    key_id: str, signed_by: str) -> Optional[str]:
        """Sign a package with digital signature"""
        if key_id not in self.key_pairs:
            return None
        
        key_pair = self.key_pairs[key_id]
        if not key_pair.is_active:
            return None
        
        try:
            # Calculate package hash
            package_hash = hashlib.sha256(package_data).digest()
            
            # Sign the hash
            if key_pair.algorithm == "RSA":
                private_key = load_pem_private_key(key_pair.private_key, password=None)
                signature = private_key.sign(
                    package_hash,
                    padding.PSS(
                        mgf=padding.MGF1(hashes.SHA256()),
                        salt_length=padding.PSS.MAX_LENGTH
                    ),
                    hashes.SHA256()
                )
            elif key_pair.algorithm == "ED25519":
                private_key = load_pem_private_key(key_pair.private_key, password=None)
                signature = private_key.sign(package_hash)
            else:
                return None
            
            # Create signature info
            signature_info = SignatureInfo(
                package_id=package_id,
                signature=base64.b64encode(signature).decode(),
                signed_by=signed_by,
                signed_at=time.time(),
                algorithm=key_pair.algorithm,
                public_key_id=key_id
            )
            
            self.package_signatures[package_id] = signature_info
            return signature_info.signature
            
        except Exception as e:
            print(f"Error signing package {package_id}: {e}")
            return None
    
    def verify_package_signature(self, package_id: str, package_data: bytes) -> bool:
        """Verify package signature"""
        if package_id not in self.package_signatures:
            return False
        
        signature_info = self.package_signatures[package_id]
        
        try:
            # Get public key
            if signature_info.public_key_id in self.key_pairs:
                public_key_pem = self.key_pairs[signature_info.public_key_id].public_key
            elif signature_info.public_key_id in self.trusted_keys:
                public_key_pem = self.trusted_keys[signature_info.public_key_id]
            else:
                return False
            
            public_key = load_pem_public_key(public_key_pem)
            
            # Calculate package hash
            package_hash = hashlib.sha256(package_data).digest()
            
            # Decode signature
            signature = base64.b64decode(signature_info.signature)
            
            # Verify signature
            if signature_info.algorithm == "RSA":
                public_key.verify(
                    signature,
                    package_hash,
                    padding.PSS(
                        mgf=padding.MGF1(hashes.SHA256()),
                        salt_length=padding.PSS.MAX_LENGTH
                    ),
                    hashes.SHA256()
                )
            elif signature_info.algorithm == "ED25519":
                public_key.verify(signature, package_hash)
            else:
                return False
            
            return True
            
        except InvalidSignature:
            return False
        except Exception as e:
            print(f"Error verifying signature for package {package_id}: {e}")
            return False
    
    def add_trusted_key(self, key_id: str, public_key_pem: bytes):
        """Add a trusted public key"""
        self.trusted_keys[key_id] = public_key_pem
    
    def remove_trusted_key(self, key_id: str):
        """Remove a trusted public key"""
        if key_id in self.trusted_keys:
            del self.trusted_keys[key_id]
    
    def get_package_signature(self, package_id: str) -> Optional[SignatureInfo]:
        """Get signature information for a package"""
        return self.package_signatures.get(package_id)
    
    def revoke_key(self, key_id: str) -> bool:
        """Revoke a key pair"""
        if key_id in self.key_pairs:
            self.key_pairs[key_id].is_active = False
            self._save_keys()
            return True
        return False
    
    def _generate_key_id(self) -> str:
        """Generate unique key ID"""
        data = f"{time.time()}:{os.urandom(16).hex()}"
        return hashlib.sha256(data.encode()).hexdigest()[:16]

class SignatureTrustManager:
    """Signature trust management system"""
    
    def __init__(self):
        self.trust_levels: Dict[str, int] = {}  # key_id -> trust_level (0-100)
        self.trust_chains: Dict[str, List[str]] = {}  # key_id -> list of trusted keys
        self.revoked_keys: Set[str] = set()
    
    def set_trust_level(self, key_id: str, trust_level: int):
        """Set trust level for a key (0-100)"""
        if 0 <= trust_level <= 100:
            self.trust_levels[key_id] = trust_level
    
    def get_trust_level(self, key_id: str) -> int:
        """Get trust level for a key"""
        return self.trust_levels.get(key_id, 0)
    
    def revoke_key(self, key_id: str):
        """Revoke a key"""
        self.revoked_keys.add(key_id)
        self.trust_levels[key_id] = 0
    
    def is_key_revoked(self, key_id: str) -> bool:
        """Check if key is revoked"""
        return key_id in self.revoked_keys
    
    def add_trust_chain(self, key_id: str, trusted_keys: List[str]):
        """Add trust chain for a key"""
        self.trust_chains[key_id] = trusted_keys
    
    def verify_trust_chain(self, key_id: str) -> bool:
        """Verify trust chain for a key"""
        if key_id not in self.trust_chains:
            return False
        
        for trusted_key in self.trust_chains[key_id]:
            if trusted_key in self.revoked_keys:
                return False
        
        return True

# Global signing system instances
package_signer = PackageSigner("/var/tusklang/registry/keys")
trust_manager = SignatureTrustManager() 