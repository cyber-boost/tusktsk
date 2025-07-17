#!/usr/bin/env python3
"""
TuskLang Package Registry Package Validation System
Comprehensive package validation rules and content scanning
"""

import hashlib
import json
import time
from typing import Dict, List, Optional, Any, Tuple
from dataclasses import dataclass
from enum import Enum
import re
import os

class ValidationRule(Enum):
    """Package validation rules"""
    FILE_SIZE_LIMIT = "file_size_limit"
    FILE_TYPE_RESTRICTION = "file_type_restriction"
    CONTENT_SCANNING = "content_scanning"
    METADATA_VALIDATION = "metadata_validation"
    DEPENDENCY_CHECK = "dependency_check"
    LICENSE_COMPLIANCE = "license_compliance"
    SECURITY_SCAN = "security_scan"

class ValidationStatus(Enum):
    """Validation status"""
    PASSED = "passed"
    FAILED = "failed"
    WARNING = "warning"
    ERROR = "error"

@dataclass
class ValidationResult:
    """Package validation result"""
    package_id: str
    status: ValidationStatus
    rule: ValidationRule
    message: str
    details: Dict[str, Any]
    timestamp: float

@dataclass
class PackageMetadata:
    """Package metadata for validation"""
    package_id: str
    name: str
    version: str
    description: str
    author: str
    license: str
    dependencies: List[str]
    file_size: int
    file_type: str

class PackageValidator:
    """Package validation system"""
    
    def __init__(self):
        self.validation_rules: Dict[ValidationRule, Dict] = {
            ValidationRule.FILE_SIZE_LIMIT: {
                'max_size_mb': 100,
                'enabled': True
            },
            ValidationRule.FILE_TYPE_RESTRICTION: {
                'allowed_types': ['.tsk', '.tar.gz', '.zip', '.json'],
                'blocked_types': ['.exe', '.bat', '.sh', '.pyc'],
                'enabled': True
            },
            ValidationRule.CONTENT_SCANNING: {
                'scan_for_malware': True,
                'scan_for_suspicious_patterns': True,
                'enabled': True
            },
            ValidationRule.METADATA_VALIDATION: {
                'require_metadata': True,
                'validate_semver': True,
                'enabled': True
            },
            ValidationRule.DEPENDENCY_CHECK: {
                'check_dependency_versions': True,
                'block_known_vulnerable_deps': True,
                'enabled': True
            },
            ValidationRule.LICENSE_COMPLIANCE: {
                'allowed_licenses': ['MIT', 'Apache-2.0', 'GPL-3.0', 'BSD-3-Clause'],
                'block_proprietary': True,
                'enabled': True
            },
            ValidationRule.SECURITY_SCAN: {
                'scan_for_secrets': True,
                'scan_for_vulnerabilities': True,
                'enabled': True
            }
        }
        self.validation_results: List[ValidationResult] = []
        self.malware_patterns: List[str] = [
            r'eval\s*\(',
            r'exec\s*\(',
            r'system\s*\(',
            r'shell_exec\s*\(',
            r'base64_decode\s*\(',
            r'gzinflate\s*\(',
            r'str_rot13\s*\('
        ]
        self.suspicious_patterns: List[str] = [
            r'password\s*=',
            r'secret\s*=',
            r'api_key\s*=',
            r'token\s*=',
            r'private_key\s*='
        ]
    
    def validate_package(self, package_id: str, package_data: bytes, 
                        metadata: PackageMetadata) -> List[ValidationResult]:
        """Validate a package using all enabled rules"""
        results = []
        
        for rule, config in self.validation_rules.items():
            if config.get('enabled', True):
                result = self._apply_validation_rule(rule, package_id, package_data, metadata)
                if result:
                    results.append(result)
        
        self.validation_results.extend(results)
        return results
    
    def _apply_validation_rule(self, rule: ValidationRule, package_id: str,
                              package_data: bytes, metadata: PackageMetadata) -> Optional[ValidationResult]:
        """Apply a specific validation rule"""
        if rule == ValidationRule.FILE_SIZE_LIMIT:
            return self._validate_file_size(package_id, package_data, metadata)
        elif rule == ValidationRule.FILE_TYPE_RESTRICTION:
            return self._validate_file_type(package_id, metadata)
        elif rule == ValidationRule.CONTENT_SCANNING:
            return self._validate_content(package_id, package_data)
        elif rule == ValidationRule.METADATA_VALIDATION:
            return self._validate_metadata(package_id, metadata)
        elif rule == ValidationRule.DEPENDENCY_CHECK:
            return self._validate_dependencies(package_id, metadata)
        elif rule == ValidationRule.LICENSE_COMPLIANCE:
            return self._validate_license(package_id, metadata)
        elif rule == ValidationRule.SECURITY_SCAN:
            return self._validate_security(package_id, package_data, metadata)
        
        return None
    
    def _validate_file_size(self, package_id: str, package_data: bytes, 
                           metadata: PackageMetadata) -> ValidationResult:
        """Validate file size limits"""
        max_size = self.validation_rules[ValidationRule.FILE_SIZE_LIMIT]['max_size_mb'] * 1024 * 1024
        actual_size = len(package_data)
        
        if actual_size > max_size:
            return ValidationResult(
                package_id=package_id,
                status=ValidationStatus.FAILED,
                rule=ValidationRule.FILE_SIZE_LIMIT,
                message=f"Package size {actual_size} exceeds limit of {max_size} bytes",
                details={'actual_size': actual_size, 'max_size': max_size},
                timestamp=time.time()
            )
        
        return ValidationResult(
            package_id=package_id,
            status=ValidationStatus.PASSED,
            rule=ValidationRule.FILE_SIZE_LIMIT,
            message="File size validation passed",
            details={'actual_size': actual_size, 'max_size': max_size},
            timestamp=time.time()
        )
    
    def _validate_file_type(self, package_id: str, metadata: PackageMetadata) -> ValidationResult:
        """Validate file type restrictions"""
        config = self.validation_rules[ValidationRule.FILE_TYPE_RESTRICTION]
        allowed_types = config['allowed_types']
        blocked_types = config['blocked_types']
        
        file_extension = os.path.splitext(metadata.name)[1].lower()
        
        if file_extension in blocked_types:
            return ValidationResult(
                package_id=package_id,
                status=ValidationStatus.FAILED,
                rule=ValidationRule.FILE_TYPE_RESTRICTION,
                message=f"File type {file_extension} is not allowed",
                details={'file_type': file_extension, 'blocked_types': blocked_types},
                timestamp=time.time()
            )
        
        if allowed_types and file_extension not in allowed_types:
            return ValidationResult(
                package_id=package_id,
                status=ValidationStatus.WARNING,
                rule=ValidationRule.FILE_TYPE_RESTRICTION,
                message=f"File type {file_extension} is not in allowed list",
                details={'file_type': file_extension, 'allowed_types': allowed_types},
                timestamp=time.time()
            )
        
        return ValidationResult(
            package_id=package_id,
            status=ValidationStatus.PASSED,
            rule=ValidationRule.FILE_TYPE_RESTRICTION,
            message="File type validation passed",
            details={'file_type': file_extension},
            timestamp=time.time()
        )
    
    def _validate_content(self, package_id: str, package_data: bytes) -> ValidationResult:
        """Validate package content for suspicious patterns"""
        content_str = package_data.decode('utf-8', errors='ignore')
        found_patterns = []
        
        # Check for malware patterns
        for pattern in self.malware_patterns:
            if re.search(pattern, content_str, re.IGNORECASE):
                found_patterns.append(f"malware_pattern: {pattern}")
        
        # Check for suspicious patterns
        for pattern in self.suspicious_patterns:
            if re.search(pattern, content_str, re.IGNORECASE):
                found_patterns.append(f"suspicious_pattern: {pattern}")
        
        if found_patterns:
            return ValidationResult(
                package_id=package_id,
                status=ValidationStatus.FAILED,
                rule=ValidationRule.CONTENT_SCANNING,
                message=f"Found {len(found_patterns)} suspicious patterns",
                details={'found_patterns': found_patterns},
                timestamp=time.time()
            )
        
        return ValidationResult(
            package_id=package_id,
            status=ValidationStatus.PASSED,
            rule=ValidationRule.CONTENT_SCANNING,
            message="Content scanning passed",
            details={'scanned_patterns': len(self.malware_patterns) + len(self.suspicious_patterns)},
            timestamp=time.time()
        )
    
    def _validate_metadata(self, package_id: str, metadata: PackageMetadata) -> ValidationResult:
        """Validate package metadata"""
        config = self.validation_rules[ValidationRule.METADATA_VALIDATION]
        errors = []
        
        # Check required fields
        if config.get('require_metadata', True):
            required_fields = ['name', 'version', 'description', 'author']
            for field in required_fields:
                if not getattr(metadata, field, None):
                    errors.append(f"Missing required field: {field}")
        
        # Validate semantic versioning
        if config.get('validate_semver', True):
            if not self._is_valid_semver(metadata.version):
                errors.append(f"Invalid semantic version: {metadata.version}")
        
        if errors:
            return ValidationResult(
                package_id=package_id,
                status=ValidationStatus.FAILED,
                rule=ValidationRule.METADATA_VALIDATION,
                message=f"Metadata validation failed: {', '.join(errors)}",
                details={'errors': errors},
                timestamp=time.time()
            )
        
        return ValidationResult(
            package_id=package_id,
            status=ValidationStatus.PASSED,
            rule=ValidationRule.METADATA_VALIDATION,
            message="Metadata validation passed",
            details={},
            timestamp=time.time()
        )
    
    def _validate_dependencies(self, package_id: str, metadata: PackageMetadata) -> ValidationResult:
        """Validate package dependencies"""
        config = self.validation_rules[ValidationRule.DEPENDENCY_CHECK]
        warnings = []
        
        # Check for known vulnerable dependencies
        if config.get('block_known_vulnerable_deps', True):
            vulnerable_deps = self._get_vulnerable_dependencies()
            for dep in metadata.dependencies:
                if dep in vulnerable_deps:
                    warnings.append(f"Known vulnerable dependency: {dep}")
        
        if warnings:
            return ValidationResult(
                package_id=package_id,
                status=ValidationStatus.WARNING,
                rule=ValidationRule.DEPENDENCY_CHECK,
                message=f"Dependency validation warnings: {', '.join(warnings)}",
                details={'warnings': warnings},
                timestamp=time.time()
            )
        
        return ValidationResult(
            package_id=package_id,
            status=ValidationStatus.PASSED,
            rule=ValidationRule.DEPENDENCY_CHECK,
            message="Dependency validation passed",
            details={'dependency_count': len(metadata.dependencies)},
            timestamp=time.time()
        )
    
    def _validate_license(self, package_id: str, metadata: PackageMetadata) -> ValidationResult:
        """Validate package license"""
        config = self.validation_rules[ValidationRule.LICENSE_COMPLIANCE]
        allowed_licenses = config.get('allowed_licenses', [])
        block_proprietary = config.get('block_proprietary', True)
        
        license_lower = metadata.license.lower()
        
        if block_proprietary and 'proprietary' in license_lower:
            return ValidationResult(
                package_id=package_id,
                status=ValidationStatus.FAILED,
                rule=ValidationRule.LICENSE_COMPLIANCE,
                message="Proprietary licenses are not allowed",
                details={'license': metadata.license},
                timestamp=time.time()
            )
        
        if allowed_licenses and metadata.license not in allowed_licenses:
            return ValidationResult(
                package_id=package_id,
                status=ValidationStatus.WARNING,
                rule=ValidationRule.LICENSE_COMPLIANCE,
                message=f"License {metadata.license} is not in allowed list",
                details={'license': metadata.license, 'allowed_licenses': allowed_licenses},
                timestamp=time.time()
            )
        
        return ValidationResult(
            package_id=package_id,
            status=ValidationStatus.PASSED,
            rule=ValidationRule.LICENSE_COMPLIANCE,
            message="License validation passed",
            details={'license': metadata.license},
            timestamp=time.time()
        )
    
    def _validate_security(self, package_id: str, package_data: bytes, 
                          metadata: PackageMetadata) -> ValidationResult:
        """Validate package security"""
        config = self.validation_rules[ValidationRule.SECURITY_SCAN]
        issues = []
        
        content_str = package_data.decode('utf-8', errors='ignore')
        
        # Scan for secrets
        if config.get('scan_for_secrets', True):
            secret_patterns = [
                r'[a-zA-Z0-9]{32,}',  # Long alphanumeric strings
                r'sk_[a-zA-Z0-9]{24}',  # Stripe secret keys
                r'AKIA[0-9A-Z]{16}',  # AWS access keys
                r'ghp_[a-zA-Z0-9]{36}',  # GitHub personal access tokens
            ]
            
            for pattern in secret_patterns:
                if re.search(pattern, content_str):
                    issues.append(f"Potential secret found: {pattern}")
        
        if issues:
            return ValidationResult(
                package_id=package_id,
                status=ValidationStatus.FAILED,
                rule=ValidationRule.SECURITY_SCAN,
                message=f"Security scan found {len(issues)} issues",
                details={'issues': issues},
                timestamp=time.time()
            )
        
        return ValidationResult(
            package_id=package_id,
            status=ValidationStatus.PASSED,
            rule=ValidationRule.SECURITY_SCAN,
            message="Security scan passed",
            details={},
            timestamp=time.time()
        )
    
    def _is_valid_semver(self, version: str) -> bool:
        """Check if version follows semantic versioning"""
        pattern = r'^\d+\.\d+\.\d+(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?(\+[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$'
        return bool(re.match(pattern, version))
    
    def _get_vulnerable_dependencies(self) -> List[str]:
        """Get list of known vulnerable dependencies"""
        # This would typically query a vulnerability database
        return [
            "vulnerable-package@1.0.0",
            "deprecated-lib@2.1.0"
        ]
    
    def get_validation_summary(self, package_id: str) -> Dict[str, Any]:
        """Get validation summary for a package"""
        package_results = [r for r in self.validation_results if r.package_id == package_id]
        
        if not package_results:
            return {'status': 'not_validated'}
        
        passed = len([r for r in package_results if r.status == ValidationStatus.PASSED])
        failed = len([r for r in package_results if r.status == ValidationStatus.FAILED])
        warnings = len([r for r in package_results if r.status == ValidationStatus.WARNING])
        
        overall_status = ValidationStatus.PASSED if failed == 0 else ValidationStatus.FAILED
        
        return {
            'package_id': package_id,
            'overall_status': overall_status.value,
            'passed_rules': passed,
            'failed_rules': failed,
            'warnings': warnings,
            'total_rules': len(package_results),
            'results': [asdict(r) for r in package_results]
        }

# Global package validator instance
package_validator = PackageValidator() 