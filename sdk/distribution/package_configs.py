"""
TuskLang SDK Package Configuration System
Secure package configuration and distribution management
"""

import json
import hashlib
import hmac
import time
import os
import tempfile
import shutil
from typing import Dict, Any, List, Optional, Tuple
from dataclasses import dataclass, asdict
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.asymmetric import rsa, padding
from cryptography.hazmat.primitives import serialization

@dataclass
class PackageConfig:
    """Package configuration for SDK distribution"""
    name: str
    version: str
    language: str
    platform: str
    architecture: str
    dependencies: List[str]
    features: List[str]
    security_level: int
    checksum: str
    signature: str
    created_at: float
    expires_at: Optional[float]
    metadata: Dict[str, Any]

@dataclass
class DistributionConfig:
    """Distribution configuration"""
    registry_url: str
    signing_key_path: str
    verification_key_path: str
    backup_urls: List[str]
    monitoring_endpoints: List[str]
    security_policies: Dict[str, Any]

class TuskPackageConfig:
    """Package configuration management system"""
    
    def __init__(self, config_path: str = None):
        self.config_path = config_path or "tusk_package_config.json"
        self.packages: Dict[str, PackageConfig] = {}
        self.distribution_config = self._load_distribution_config()
        self.signing_key = self._load_signing_key()
        self.verification_key = self._load_verification_key()
        
    def _load_distribution_config(self) -> DistributionConfig:
        """Load distribution configuration"""
        default_config = {
            "registry_url": "https://registry.tusklang.org",
            "signing_key_path": "keys/signing_key.pem",
            "verification_key_path": "keys/verification_key.pub",
            "backup_urls": [
                "https://backup1.tusklang.org",
                "https://backup2.tusklang.org"
            ],
            "monitoring_endpoints": [
                "https://monitor1.tusklang.org",
                "https://monitor2.tusklang.org"
            ],
            "security_policies": {
                "require_signature": True,
                "require_checksum": True,
                "expiration_days": 365,
                "max_package_size": 100 * 1024 * 1024,  # 100MB
                "allowed_languages": ["python", "javascript", "rust", "go", "java", "csharp", "ruby", "php", "bash"]
            }
        }
        
        if os.path.exists(self.config_path):
            with open(self.config_path, 'r') as f:
                config_data = json.load(f)
                default_config.update(config_data)
        
        return DistributionConfig(**default_config)
    
    def _load_signing_key(self) -> Optional[rsa.RSAPrivateKey]:
        """Load signing private key"""
        try:
            if os.path.exists(self.distribution_config.signing_key_path):
                with open(self.distribution_config.signing_key_path, 'rb') as f:
                    return serialization.load_pem_private_key(
                        f.read(),
                        password=None
                    )
        except Exception as e:
            print(f"Warning: Could not load signing key: {e}")
        return None
    
    def _load_verification_key(self) -> Optional[rsa.RSAPublicKey]:
        """Load verification public key"""
        try:
            if os.path.exists(self.distribution_config.verification_key_path):
                with open(self.distribution_config.verification_key_path, 'rb') as f:
                    return serialization.load_pem_public_key(f.read())
        except Exception as e:
            print(f"Warning: Could not load verification key: {e}")
        return None
    
    def create_package_config(self, name: str, version: str, language: str, 
                            platform: str, architecture: str, dependencies: List[str],
                            features: List[str], security_level: int = 2,
                            metadata: Dict[str, Any] = None) -> PackageConfig:
        """Create a new package configuration"""
        
        # Validate inputs
        if language not in self.distribution_config.security_policies["allowed_languages"]:
            raise ValueError(f"Language {language} not allowed")
        
        if security_level < 1 or security_level > 3:
            raise ValueError("Security level must be between 1 and 3")
        
        # Generate package ID
        package_id = f"{name}-{version}-{language}-{platform}-{architecture}"
        
        # Create package config
        config = PackageConfig(
            name=name,
            version=version,
            language=language,
            platform=platform,
            architecture=architecture,
            dependencies=dependencies,
            features=features,
            security_level=security_level,
            checksum="",  # Will be set when package is built
            signature="",  # Will be set when package is signed
            created_at=time.time(),
            expires_at=time.time() + (self.distribution_config.security_policies["expiration_days"] * 86400),
            metadata=metadata or {}
        )
        
        self.packages[package_id] = config
        return config
    
    def build_package(self, package_id: str, source_path: str, output_path: str) -> str:
        """Build a package from source"""
        if package_id not in self.packages:
            raise ValueError(f"Package {package_id} not found")
        
        config = self.packages[package_id]
        
        # Validate source path
        if not os.path.exists(source_path):
            raise FileNotFoundError(f"Source path {source_path} not found")
        
        # Create output directory
        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        
        # Copy source to output
        if os.path.isdir(source_path):
            shutil.copytree(source_path, output_path, dirs_exist_ok=True)
        else:
            shutil.copy2(source_path, output_path)
        
        # Calculate checksum
        checksum = self._calculate_checksum(output_path)
        config.checksum = checksum
        
        # Sign package
        if self.signing_key and self.distribution_config.security_policies["require_signature"]:
            signature = self._sign_package(output_path)
            config.signature = signature
        
        # Save updated config
        self._save_packages()
        
        return output_path
    
    def _calculate_checksum(self, file_path: str) -> str:
        """Calculate SHA256 checksum of file or directory"""
        hasher = hashlib.sha256()
        
        if os.path.isfile(file_path):
            with open(file_path, 'rb') as f:
                for chunk in iter(lambda: f.read(4096), b""):
                    hasher.update(chunk)
        else:
            # For directories, hash all files
            for root, dirs, files in os.walk(file_path):
                for file in sorted(files):
                    file_path_full = os.path.join(root, file)
                    with open(file_path_full, 'rb') as f:
                        for chunk in iter(lambda: f.read(4096), b""):
                            hasher.update(chunk)
        
        return hasher.hexdigest()
    
    def _sign_package(self, file_path: str) -> str:
        """Sign package with private key"""
        if not self.signing_key:
            raise RuntimeError("No signing key available")
        
        # Calculate checksum
        checksum = self._calculate_checksum(file_path)
        
        # Sign checksum
        signature = self.signing_key.sign(
            checksum.encode(),
            padding.PSS(
                mgf=padding.MGF1(hashes.SHA256()),
                salt_length=padding.PSS.MAX_LENGTH
            ),
            hashes.SHA256()
        )
        
        return signature.hex()
    
    def verify_package(self, package_path: str, package_id: str) -> bool:
        """Verify package integrity and signature"""
        if package_id not in self.packages:
            return False
        
        config = self.packages[package_id]
        
        # Check expiration
        if config.expires_at and time.time() > config.expires_at:
            print(f"Package {package_id} has expired")
            return False
        
        # Verify checksum
        if self.distribution_config.security_policies["require_checksum"]:
            current_checksum = self._calculate_checksum(package_path)
            if current_checksum != config.checksum:
                print(f"Checksum mismatch for package {package_id}")
                return False
        
        # Verify signature
        if (self.verification_key and 
            self.distribution_config.security_policies["require_signature"] and
            config.signature):
            
            try:
                signature_bytes = bytes.fromhex(config.signature)
                checksum = self._calculate_checksum(package_path)
                
                self.verification_key.verify(
                    signature_bytes,
                    checksum.encode(),
                    padding.PSS(
                        mgf=padding.MGF1(hashes.SHA256()),
                        salt_length=padding.PSS.MAX_LENGTH
                    ),
                    hashes.SHA256()
                )
            except Exception as e:
                print(f"Signature verification failed for package {package_id}: {e}")
                return False
        
        return True
    
    def get_package_info(self, package_id: str) -> Optional[Dict[str, Any]]:
        """Get package information"""
        if package_id not in self.packages:
            return None
        
        config = self.packages[package_id]
        info = asdict(config)
        
        # Add verification status
        info["verified"] = True  # Would be set based on actual verification
        info["expired"] = config.expires_at and time.time() > config.expires_at
        
        return info
    
    def list_packages(self, language: str = None, platform: str = None) -> List[Dict[str, Any]]:
        """List packages with optional filtering"""
        packages = []
        
        for package_id, config in self.packages.items():
            if language and config.language != language:
                continue
            if platform and config.platform != platform:
                continue
            
            packages.append({
                "package_id": package_id,
                "name": config.name,
                "version": config.version,
                "language": config.language,
                "platform": config.platform,
                "architecture": config.architecture,
                "security_level": config.security_level,
                "created_at": config.created_at,
                "expires_at": config.expires_at,
                "expired": config.expires_at and time.time() > config.expires_at
            })
        
        return packages
    
    def update_package(self, package_id: str, updates: Dict[str, Any]) -> bool:
        """Update package configuration"""
        if package_id not in self.packages:
            return False
        
        config = self.packages[package_id]
        
        # Update allowed fields
        allowed_fields = ["dependencies", "features", "security_level", "metadata"]
        for field, value in updates.items():
            if field in allowed_fields:
                setattr(config, field, value)
        
        # Update timestamp
        config.created_at = time.time()
        
        self._save_packages()
        return True
    
    def delete_package(self, package_id: str) -> bool:
        """Delete package configuration"""
        if package_id not in self.packages:
            return False
        
        del self.packages[package_id]
        self._save_packages()
        return True
    
    def _save_packages(self):
        """Save package configurations to file"""
        data = {
            "distribution_config": asdict(self.distribution_config),
            "packages": {pid: asdict(config) for pid, config in self.packages.items()}
        }
        
        with open(self.config_path, 'w') as f:
            json.dump(data, f, indent=2)
    
    def generate_distribution_report(self) -> Dict[str, Any]:
        """Generate distribution report"""
        total_packages = len(self.packages)
        expired_packages = sum(1 for config in self.packages.values() 
                             if config.expires_at and time.time() > config.expires_at)
        
        languages = {}
        platforms = {}
        security_levels = {}
        
        for config in self.packages.values():
            languages[config.language] = languages.get(config.language, 0) + 1
            platforms[config.platform] = platforms.get(config.platform, 0) + 1
            security_levels[config.security_level] = security_levels.get(config.security_level, 0) + 1
        
        return {
            "total_packages": total_packages,
            "expired_packages": expired_packages,
            "active_packages": total_packages - expired_packages,
            "languages": languages,
            "platforms": platforms,
            "security_levels": security_levels,
            "distribution_config": asdict(self.distribution_config),
            "generated_at": time.time()
        }

# Global package config instance
_package_config_instance: Optional[TuskPackageConfig] = None

def initialize_package_config(config_path: str = None) -> TuskPackageConfig:
    """Initialize global package config instance"""
    global _package_config_instance
    _package_config_instance = TuskPackageConfig(config_path)
    return _package_config_instance

def get_package_config() -> TuskPackageConfig:
    """Get global package config instance"""
    if _package_config_instance is None:
        raise RuntimeError("Package config not initialized. Call initialize_package_config() first.")
    return _package_config_instance 