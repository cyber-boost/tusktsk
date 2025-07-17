#!/usr/bin/env python3
"""
TuskLang Package Registry Signature Verification System
Advanced signature verification and trust validation
"""

import hashlib
import time
from typing import Dict, List, Optional, Tuple
from dataclasses import dataclass
from enum import Enum
import json

class VerificationStatus(Enum):
    """Signature verification status"""
    VALID = "valid"
    INVALID = "invalid"
    UNTRUSTED = "untrusted"
    REVOKED = "revoked"
    EXPIRED = "expired"
    UNKNOWN = "unknown"

@dataclass
class VerificationResult:
    """Result of signature verification"""
    package_id: str
    status: VerificationStatus
    signature_info: Optional[Dict] = None
    trust_level: int = 0
    verification_time: float = 0
    error_message: Optional[str] = None

class SignatureVerifier:
    """Advanced signature verification system"""
    
    def __init__(self, package_signer, trust_manager):
        self.package_signer = package_signer
        self.trust_manager = trust_manager
        self.verification_cache: Dict[str, VerificationResult] = {}
        self.verification_history: List[Dict] = []
    
    def verify_package_signature(self, package_id: str, package_data: bytes) -> VerificationResult:
        """Verify package signature with comprehensive checks"""
        start_time = time.time()
        
        # Check cache first
        cache_key = f"{package_id}:{hashlib.sha256(package_data).hexdigest()}"
        if cache_key in self.verification_cache:
            return self.verification_cache[cache_key]
        
        # Get signature info
        signature_info = self.package_signer.get_package_signature(package_id)
        if not signature_info:
            result = VerificationResult(
                package_id=package_id,
                status=VerificationStatus.UNKNOWN,
                error_message="No signature found for package"
            )
            self._cache_result(cache_key, result)
            return result
        
        # Check if key is revoked
        if self.trust_manager.is_key_revoked(signature_info.public_key_id):
            result = VerificationResult(
                package_id=package_id,
                status=VerificationStatus.REVOKED,
                signature_info=signature_info.__dict__,
                error_message="Signing key has been revoked"
            )
            self._cache_result(cache_key, result)
            return result
        
        # Verify signature
        if not self.package_signer.verify_package_signature(package_id, package_data):
            result = VerificationResult(
                package_id=package_id,
                status=VerificationStatus.INVALID,
                signature_info=signature_info.__dict__,
                error_message="Signature verification failed"
            )
            self._cache_result(cache_key, result)
            return result
        
        # Check trust level
        trust_level = self.trust_manager.get_trust_level(signature_info.public_key_id)
        
        # Determine final status
        if trust_level >= 80:
            status = VerificationStatus.VALID
        elif trust_level >= 50:
            status = VerificationStatus.VALID
        else:
            status = VerificationStatus.UNTRUSTED
        
        verification_time = time.time() - start_time
        
        result = VerificationResult(
            package_id=package_id,
            status=status,
            signature_info=signature_info.__dict__,
            trust_level=trust_level,
            verification_time=verification_time
        )
        
        self._cache_result(cache_key, result)
        self._log_verification(result)
        
        return result
    
    def verify_multiple_packages(self, packages: List[Tuple[str, bytes]]) -> Dict[str, VerificationResult]:
        """Verify multiple packages efficiently"""
        results = {}
        
        for package_id, package_data in packages:
            results[package_id] = self.verify_package_signature(package_id, package_data)
        
        return results
    
    def get_verification_statistics(self) -> Dict:
        """Get verification statistics"""
        total_verifications = len(self.verification_history)
        
        status_counts = {}
        for status in VerificationStatus:
            status_counts[status.value] = 0
        
        for log in self.verification_history:
            status = log['status']
            status_counts[status] = status_counts.get(status, 0) + 1
        
        return {
            'total_verifications': total_verifications,
            'status_counts': status_counts,
            'cache_hit_rate': len(self.verification_cache) / max(total_verifications, 1)
        }
    
    def _cache_result(self, cache_key: str, result: VerificationResult):
        """Cache verification result"""
        self.verification_cache[cache_key] = result
        
        # Limit cache size
        if len(self.verification_cache) > 1000:
            # Remove oldest entries
            oldest_keys = sorted(self.verification_cache.keys())[:100]
            for key in oldest_keys:
                del self.verification_cache[key]
    
    def _log_verification(self, result: VerificationResult):
        """Log verification result"""
        log_entry = {
            'timestamp': time.time(),
            'package_id': result.package_id,
            'status': result.status.value,
            'trust_level': result.trust_level,
            'verification_time': result.verification_time,
            'error_message': result.error_message
        }
        
        self.verification_history.append(log_entry)
        
        # Limit history size
        if len(self.verification_history) > 10000:
            self.verification_history = self.verification_history[-5000:]

class TrustValidator:
    """Trust validation system"""
    
    def __init__(self, trust_manager):
        self.trust_manager = trust_manager
        self.trust_chains: Dict[str, List[str]] = {}
    
    def validate_trust_chain(self, key_id: str) -> bool:
        """Validate trust chain for a key"""
        return self.trust_manager.verify_trust_chain(key_id)
    
    def calculate_trust_score(self, key_id: str) -> float:
        """Calculate trust score for a key"""
        base_trust = self.trust_manager.get_trust_level(key_id)
        
        # Apply trust chain multiplier
        if self.validate_trust_chain(key_id):
            chain_multiplier = 1.2
        else:
            chain_multiplier = 0.8
        
        return min(100.0, base_trust * chain_multiplier)
    
    def get_trusted_keys(self, min_trust_level: int = 50) -> List[str]:
        """Get list of trusted keys above minimum level"""
        trusted_keys = []
        
        for key_id in self.trust_manager.trust_levels:
            if (self.trust_manager.get_trust_level(key_id) >= min_trust_level and
                not self.trust_manager.is_key_revoked(key_id)):
                trusted_keys.append(key_id)
        
        return trusted_keys

# Global verification system instances
signature_verifier = SignatureVerifier(package_signer, trust_manager)
trust_validator = TrustValidator(trust_manager) 