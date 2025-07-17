#!/usr/bin/env python3
"""
TuskLang Package Registry Checksum Validation System
Comprehensive checksum validation for package integrity
"""

import hashlib
import hmac
import time
from typing import Dict, List, Optional, Tuple
from dataclasses import dataclass
from enum import Enum
import json

class ChecksumAlgorithm(Enum):
    """Supported checksum algorithms"""
    MD5 = "md5"
    SHA1 = "sha1"
    SHA256 = "sha256"
    SHA512 = "sha512"
    BLAKE2B = "blake2b"
    BLAKE2S = "blake2s"
    SHA3_256 = "sha3_256"
    SHA3_512 = "sha3_512"

@dataclass
class ChecksumResult:
    """Result of checksum validation"""
    algorithm: ChecksumAlgorithm
    expected: str
    calculated: str
    is_valid: bool
    validation_time: float

@dataclass
class PackageChecksums:
    """Package checksums for all algorithms"""
    package_id: str
    checksums: Dict[ChecksumAlgorithm, str]
    file_size: int
    created_at: float
    verified_at: Optional[float] = None

class ChecksumValidator:
    """Comprehensive checksum validation system"""
    
    def __init__(self):
        self.package_checksums: Dict[str, PackageChecksums] = {}
        self.validation_history: List[Dict] = []
        
    def calculate_all_checksums(self, package_data: bytes) -> Dict[ChecksumAlgorithm, str]:
        """Calculate checksums using all supported algorithms"""
        checksums = {}
        
        # MD5
        checksums[ChecksumAlgorithm.MD5] = hashlib.md5(package_data).hexdigest()
        
        # SHA1
        checksums[ChecksumAlgorithm.SHA1] = hashlib.sha1(package_data).hexdigest()
        
        # SHA256
        checksums[ChecksumAlgorithm.SHA256] = hashlib.sha256(package_data).hexdigest()
        
        # SHA512
        checksums[ChecksumAlgorithm.SHA512] = hashlib.sha512(package_data).hexdigest()
        
        # BLAKE2B
        checksums[ChecksumAlgorithm.BLAKE2B] = hashlib.blake2b(package_data).hexdigest()
        
        # BLAKE2S
        checksums[ChecksumAlgorithm.BLAKE2S] = hashlib.blake2s(package_data).hexdigest()
        
        # SHA3_256
        checksums[ChecksumAlgorithm.SHA3_256] = hashlib.sha3_256(package_data).hexdigest()
        
        # SHA3_512
        checksums[ChecksumAlgorithm.SHA3_512] = hashlib.sha3_512(package_data).hexdigest()
        
        return checksums
    
    def store_package_checksums(self, package_id: str, package_data: bytes) -> bool:
        """Store checksums for a package"""
        try:
            checksums = self.calculate_all_checksums(package_data)
            
            package_checksums = PackageChecksums(
                package_id=package_id,
                checksums=checksums,
                file_size=len(package_data),
                created_at=time.time()
            )
            
            self.package_checksums[package_id] = package_checksums
            return True
            
        except Exception as e:
            print(f"Error storing checksums for package {package_id}: {e}")
            return False
    
    def validate_package_checksums(self, package_id: str, package_data: bytes) -> Dict[ChecksumAlgorithm, ChecksumResult]:
        """Validate package against stored checksums"""
        if package_id not in self.package_checksums:
            return {}
        
        stored_checksums = self.package_checksums[package_id]
        calculated_checksums = self.calculate_all_checksums(package_data)
        results = {}
        
        for algorithm in ChecksumAlgorithm:
            if algorithm in stored_checksums.checksums:
                start_time = time.time()
                expected = stored_checksums.checksums[algorithm]
                calculated = calculated_checksums[algorithm]
                is_valid = hmac.compare_digest(expected, calculated)
                validation_time = time.time() - start_time
                
                results[algorithm] = ChecksumResult(
                    algorithm=algorithm,
                    expected=expected,
                    calculated=calculated,
                    is_valid=is_valid,
                    validation_time=validation_time
                )
        
        # Update verification timestamp
        stored_checksums.verified_at = time.time()
        
        # Log validation
        self._log_validation(package_id, results)
        
        return results
    
    def validate_single_checksum(self, package_data: bytes, algorithm: ChecksumAlgorithm, 
                               expected_checksum: str) -> ChecksumResult:
        """Validate a single checksum"""
        start_time = time.time()
        calculated = self.calculate_all_checksums(package_data)[algorithm]
        is_valid = hmac.compare_digest(expected_checksum, calculated)
        validation_time = time.time() - start_time
        
        return ChecksumResult(
            algorithm=algorithm,
            expected=expected_checksum,
            calculated=calculated,
            is_valid=is_valid,
            validation_time=validation_time
        )
    
    def get_package_checksums(self, package_id: str) -> Optional[PackageChecksums]:
        """Get stored checksums for a package"""
        return self.package_checksums.get(package_id)
    
    def verify_package_integrity(self, package_id: str, package_data: bytes) -> bool:
        """Verify package integrity using SHA256 (primary algorithm)"""
        if package_id not in self.package_checksums:
            return False
        
        stored_checksums = self.package_checksums[package_id]
        
        # Check file size first
        if len(package_data) != stored_checksums.file_size:
            return False
        
        # Validate SHA256 checksum
        calculated_sha256 = hashlib.sha256(package_data).hexdigest()
        expected_sha256 = stored_checksums.checksums[ChecksumAlgorithm.SHA256]
        
        return hmac.compare_digest(calculated_sha256, expected_sha256)
    
    def create_checksum_manifest(self, package_id: str) -> Dict:
        """Create a checksum manifest for a package"""
        if package_id not in self.package_checksums:
            return {}
        
        checksums = self.package_checksums[package_id]
        
        manifest = {
            'package_id': package_id,
            'file_size': checksums.file_size,
            'created_at': checksums.created_at,
            'verified_at': checksums.verified_at,
            'checksums': {
                algorithm.value: checksum
                for algorithm, checksum in checksums.checksums.items()
            }
        }
        
        return manifest
    
    def _log_validation(self, package_id: str, results: Dict[ChecksumAlgorithm, ChecksumResult]):
        """Log validation results"""
        log_entry = {
            'timestamp': time.time(),
            'package_id': package_id,
            'results': {
                algorithm.value: {
                    'is_valid': result.is_valid,
                    'validation_time': result.validation_time
                }
                for algorithm, result in results.items()
            }
        }
        
        self.validation_history.append(log_entry)
    
    def get_validation_statistics(self) -> Dict:
        """Get validation statistics"""
        total_validations = len(self.validation_history)
        successful_validations = sum(
            1 for log in self.validation_history
            if all(result['is_valid'] for result in log['results'].values())
        )
        
        return {
            'total_validations': total_validations,
            'successful_validations': successful_validations,
            'success_rate': successful_validations / total_validations if total_validations > 0 else 0
        }

class ChecksumVerificationAPI:
    """API for checksum verification"""
    
    def __init__(self):
        self.validator = ChecksumValidator()
    
    def verify_package_download(self, package_id: str, package_data: bytes) -> Dict:
        """Verify package download integrity"""
        results = self.validator.validate_package_checksums(package_id, package_data)
        
        # Check if all checksums are valid
        all_valid = all(result.is_valid for result in results.values())
        
        return {
            'package_id': package_id,
            'integrity_verified': all_valid,
            'checksum_results': {
                algorithm.value: {
                    'is_valid': result.is_valid,
                    'validation_time': result.validation_time
                }
                for algorithm, result in results.items()
            },
            'verification_timestamp': time.time()
        }
    
    def get_package_manifest(self, package_id: str) -> Dict:
        """Get package checksum manifest"""
        return self.validator.create_checksum_manifest(package_id)

# Global checksum validator instance
checksum_validator = ChecksumValidator()
verification_api = ChecksumVerificationAPI() 