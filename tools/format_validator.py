#!/usr/bin/env python3
"""
TuskLang Binary Format Validator
Validates .pnt files for format compliance and integrity
"""

import sys
import os
import struct
import zlib
import hashlib
import json
import argparse
from pathlib import Path
from typing import Dict, List, Tuple, Optional
import logging

# Add implementations to path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'implementations', 'python'))

from binary_format import PntReader, PntWriter, PntMetadata, read_pnt_file, validate_pnt_file

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

class FormatValidator:
    """Comprehensive validator for TuskLang binary format files"""
    
    def __init__(self, verbose: bool = False):
        self.verbose = verbose
        self.errors = []
        self.warnings = []
        self.stats = {
            'files_checked': 0,
            'files_valid': 0,
            'files_invalid': 0,
            'total_errors': 0,
            'total_warnings': 0
        }
    
    def validate_file(self, file_path: Path) -> bool:
        """Validate a single .pnt file"""
        self.stats['files_checked'] += 1
        
        if self.verbose:
            logger.info(f"Validating: {file_path}")
        
        try:
            # Basic file checks
            if not self._check_file_basics(file_path):
                return False
            
            # Header validation
            if not self._validate_header(file_path):
                return False
            
            # Metadata validation
            if not self._validate_metadata(file_path):
                return False
            
            # Data validation
            if not self._validate_data(file_path):
                return False
            
            # Cross-reference validation
            if not self._validate_cross_references(file_path):
                return False
            
            # Performance validation
            if not self._validate_performance(file_path):
                return False
            
            self.stats['files_valid'] += 1
            if self.verbose:
                logger.info(f"✓ Valid: {file_path}")
            return True
            
        except Exception as e:
            self.stats['files_invalid'] += 1
            self.stats['total_errors'] += 1
            error_msg = f"Validation failed for {file_path}: {str(e)}"
            self.errors.append(error_msg)
            if self.verbose:
                logger.error(error_msg)
            return False
    
    def _check_file_basics(self, file_path: Path) -> bool:
        """Check basic file properties"""
        try:
            # Check file exists
            if not file_path.exists():
                self._add_error(f"File does not exist: {file_path}")
                return False
            
            # Check file size
            file_size = file_path.stat().st_size
            if file_size == 0:
                self._add_error(f"File is empty: {file_path}")
                return False
            
            if file_size > 1024 * 1024 * 1024:  # 1GB
                self._add_error(f"File too large: {file_size} bytes")
                return False
            
            # Check file extension
            if file_path.suffix.lower() != '.pnt':
                self._add_warning(f"File does not have .pnt extension: {file_path}")
            
            # Check file permissions
            if not os.access(file_path, os.R_OK):
                self._add_error(f"File not readable: {file_path}")
                return False
            
            return True
            
        except Exception as e:
            self._add_error(f"Basic file check failed: {str(e)}")
            return False
    
    def _validate_header(self, file_path: Path) -> bool:
        """Validate file header"""
        try:
            with open(file_path, 'rb') as f:
                # Read header
                header_data = f.read(32)
                if len(header_data) < 32:
                    self._add_error(f"Header too short: {len(header_data)} bytes")
                    return False
                
                # Parse header
                header = self._parse_header(header_data)
                
                # Validate magic bytes
                if header['magic'] != b'TUSK':
                    self._add_error(f"Invalid magic bytes: {header['magic']}")
                    return False
                
                # Validate version
                if header['version'] != 0x0100:
                    self._add_error(f"Unsupported version: {header['version']:04x}")
                    return False
                
                # Validate reserved fields
                if header['reserved1'] != 0 or header['reserved2'] != 0:
                    self._add_error("Reserved fields must be zero")
                    return False
                
                # Validate header checksum
                calculated_checksum = self._calculate_header_checksum(header_data)
                if calculated_checksum != header['header_checksum']:
                    self._add_error(f"Header checksum mismatch: expected {header['header_checksum']}, got {calculated_checksum}")
                    return False
                
                # Validate compression
                if header['compression'] not in [0, 1, 2, 3]:
                    self._add_error(f"Invalid compression algorithm: {header['compression']}")
                    return False
                
                # Validate encryption
                if header['encryption'] not in [0, 1, 2]:
                    self._add_error(f"Invalid encryption algorithm: {header['encryption']}")
                    return False
                
                # Store header for later validation
                self.current_header = header
                
                return True
                
        except Exception as e:
            self._add_error(f"Header validation failed: {str(e)}")
            return False
    
    def _validate_metadata(self, file_path: Path) -> bool:
        """Validate metadata section"""
        try:
            if not hasattr(self, 'current_header'):
                self._add_error("No header available for metadata validation")
                return False
            
            header = self.current_header
            
            # Check if metadata is present
            if not (header['flags'] & 0x01):  # FLAG_HAS_METADATA
                if self.verbose:
                    logger.info("No metadata section present")
                return True
            
            with open(file_path, 'rb') as f:
                # Skip header
                f.seek(32)
                
                # Read metadata length and checksum
                metadata_length = struct.unpack('<I', f.read(4))[0]
                metadata_checksum = struct.unpack('<I', f.read(4))[0]
                
                # Validate metadata length
                if metadata_length == 0:
                    self._add_error("Metadata section empty")
                    return False
                
                if metadata_length > 1024 * 1024:  # 1MB
                    self._add_error(f"Metadata section too large: {metadata_length} bytes")
                    return False
                
                # Read metadata data
                metadata_data = f.read(metadata_length)
                if len(metadata_data) < metadata_length:
                    self._add_error(f"Metadata section truncated: expected {metadata_length}, got {len(metadata_data)}")
                    return False
                
                # Validate metadata checksum
                calculated_checksum = zlib.crc32(metadata_data)
                if calculated_checksum != metadata_checksum:
                    self._add_error(f"Metadata checksum mismatch: expected {metadata_checksum}, got {calculated_checksum}")
                    return False
                
                # Parse and validate metadata content
                if not self._validate_metadata_content(metadata_data, header):
                    return False
                
                return True
                
        except Exception as e:
            self._add_error(f"Metadata validation failed: {str(e)}")
            return False
    
    def _validate_metadata_content(self, metadata_data: bytes, header: Dict) -> bool:
        """Validate metadata content"""
        try:
            offset = 0
            
            # Read and validate strings
            strings = []
            for i in range(6):  # package_name, version, author, description, license, repository
                if offset >= len(metadata_data):
                    self._add_error(f"Metadata truncated at string {i}")
                    return False
                
                # Find null terminator
                end = metadata_data.find(b'\x00', offset)
                if end == -1:
                    self._add_error(f"String {i} not null-terminated")
                    return False
                
                string_data = metadata_data[offset:end]
                try:
                    string_value = string_data.decode('utf-8')
                    strings.append(string_value)
                except UnicodeDecodeError:
                    self._add_error(f"String {i} not valid UTF-8")
                    return False
                
                offset = end + 1
            
            # Validate package name
            if strings[0] and len(strings[0]) > 100:
                self._add_warning(f"Package name very long: {len(strings[0])} characters")
            
            # Validate version
            if strings[1] and not self._is_valid_version(strings[1]):
                self._add_warning(f"Version format may be invalid: {strings[1]}")
            
            # Validate dependencies if present
            if header['flags'] & 0x10:  # FLAG_HAS_DEPENDENCIES
                if not self._validate_dependencies(metadata_data, offset):
                    return False
            
            # Validate keywords if present
            if header['flags'] & 0x20:  # FLAG_HAS_KEYWORDS
                if not self._validate_keywords(metadata_data, offset):
                    return False
            
            return True
            
        except Exception as e:
            self._add_error(f"Metadata content validation failed: {str(e)}")
            return False
    
    def _validate_dependencies(self, metadata_data: bytes, offset: int) -> bool:
        """Validate dependencies section"""
        try:
            if offset + 4 > len(metadata_data):
                self._add_error("Dependencies section truncated")
                return False
            
            deps_count = struct.unpack('<I', metadata_data[offset:offset+4])[0]
            offset += 4
            
            if deps_count > 1000:
                self._add_warning(f"Very many dependencies: {deps_count}")
            
            for i in range(deps_count):
                if offset >= len(metadata_data):
                    self._add_error(f"Dependency {i} truncated")
                    return False
                
                # Read dependency name
                end = metadata_data.find(b'\x00', offset)
                if end == -1:
                    self._add_error(f"Dependency {i} name not null-terminated")
                    return False
                
                dep_name = metadata_data[offset:end].decode('utf-8')
                offset = end + 1
                
                # Read dependency version
                end = metadata_data.find(b'\x00', offset)
                if end == -1:
                    self._add_error(f"Dependency {i} version not null-terminated")
                    return False
                
                dep_version = metadata_data[offset:end].decode('utf-8')
                offset = end + 1
                
                # Read dependency type
                if offset >= len(metadata_data):
                    self._add_error(f"Dependency {i} type missing")
                    return False
                
                dep_type = metadata_data[offset]
                offset += 1
                
                # Validate dependency
                if not dep_name:
                    self._add_error(f"Dependency {i} has empty name")
                    return False
                
                if len(dep_name) > 100:
                    self._add_warning(f"Dependency {i} name very long: {len(dep_name)} characters")
            
            return True
            
        except Exception as e:
            self._add_error(f"Dependencies validation failed: {str(e)}")
            return False
    
    def _validate_keywords(self, metadata_data: bytes, offset: int) -> bool:
        """Validate keywords section"""
        try:
            if offset + 4 > len(metadata_data):
                self._add_error("Keywords section truncated")
                return False
            
            keywords_count = struct.unpack('<I', metadata_data[offset:offset+4])[0]
            offset += 4
            
            if keywords_count > 100:
                self._add_warning(f"Very many keywords: {keywords_count}")
            
            for i in range(keywords_count):
                if offset >= len(metadata_data):
                    self._add_error(f"Keyword {i} truncated")
                    return False
                
                # Read keyword
                end = metadata_data.find(b'\x00', offset)
                if end == -1:
                    self._add_error(f"Keyword {i} not null-terminated")
                    return False
                
                keyword = metadata_data[offset:end].decode('utf-8')
                offset = end + 1
                
                # Validate keyword
                if not keyword:
                    self._add_error(f"Keyword {i} is empty")
                    return False
                
                if len(keyword) > 50:
                    self._add_warning(f"Keyword {i} very long: {len(keyword)} characters")
            
            return True
            
        except Exception as e:
            self._add_error(f"Keywords validation failed: {str(e)}")
            return False
    
    def _validate_data(self, file_path: Path) -> bool:
        """Validate data section"""
        try:
            if not hasattr(self, 'current_header'):
                self._add_error("No header available for data validation")
                return False
            
            header = self.current_header
            
            with open(file_path, 'rb') as f:
                # Skip to data section
                f.seek(32)  # Skip header
                
                # Skip metadata if present
                if header['flags'] & 0x01:  # FLAG_HAS_METADATA
                    metadata_length = struct.unpack('<I', f.read(4))[0]
                    f.seek(metadata_length + 4, 1)  # Skip metadata + checksum
                
                # Read data length
                data_length = struct.unpack('<I', f.read(4))[0]
                
                # Validate data length
                if data_length == 0:
                    self._add_error("Data section empty")
                    return False
                
                if data_length > 100 * 1024 * 1024:  # 100MB
                    self._add_warning(f"Data section very large: {data_length} bytes")
                
                # Read data
                data = f.read(data_length)
                if len(data) < data_length:
                    self._add_error(f"Data section truncated: expected {data_length}, got {len(data)}")
                    return False
                
                # Read data checksum
                data_checksum = struct.unpack('<I', f.read(4))[0]
                
                # Validate data checksum
                calculated_checksum = int.from_bytes(hashlib.sha256(data).digest()[:4], 'little')
                if calculated_checksum != data_checksum:
                    self._add_error(f"Data checksum mismatch: expected {data_checksum}, got {calculated_checksum}")
                    return False
                
                # Validate data content
                if not self._validate_data_content(data, header):
                    return False
                
                return True
                
        except Exception as e:
            self._add_error(f"Data validation failed: {str(e)}")
            return False
    
    def _validate_data_content(self, data: bytes, header: Dict) -> bool:
        """Validate data content"""
        try:
            # Decompress if needed
            if header['flags'] & 0x08:  # FLAG_IS_COMPRESSED
                try:
                    if header['compression'] == 1:  # gzip
                        data = zlib.decompress(data)
                    else:
                        self._add_warning(f"Compression algorithm {header['compression']} not validated")
                except Exception as e:
                    self._add_error(f"Data decompression failed: {str(e)}")
                    return False
            
            # Decrypt if needed
            if header['flags'] & 0x02:  # FLAG_HAS_ENCRYPTION
                self._add_warning(f"Encryption algorithm {header['encryption']} not validated")
            
            # Validate JSON
            try:
                json_data = json.loads(data.decode('utf-8'))
                if not isinstance(json_data, dict):
                    self._add_error("Data is not a JSON object")
                    return False
                
                # Validate JSON structure
                if not self._validate_json_structure(json_data):
                    return False
                
            except json.JSONDecodeError as e:
                self._add_error(f"Invalid JSON data: {str(e)}")
                return False
            except UnicodeDecodeError as e:
                self._add_error(f"Data not valid UTF-8: {str(e)}")
                return False
            
            return True
            
        except Exception as e:
            self._add_error(f"Data content validation failed: {str(e)}")
            return False
    
    def _validate_json_structure(self, json_data: Dict) -> bool:
        """Validate JSON structure"""
        try:
            # Check for common required fields
            if 'name' in json_data and not isinstance(json_data['name'], str):
                self._add_warning("Field 'name' should be a string")
            
            if 'version' in json_data and not isinstance(json_data['version'], str):
                self._add_warning("Field 'version' should be a string")
            
            # Check for very large objects
            if len(json.dumps(json_data)) > 10 * 1024 * 1024:  # 10MB
                self._add_warning("JSON data very large")
            
            return True
            
        except Exception as e:
            self._add_error(f"JSON structure validation failed: {str(e)}")
            return False
    
    def _validate_cross_references(self, file_path: Path) -> bool:
        """Validate cross-references between sections"""
        try:
            # This would validate relationships between metadata and data
            # For now, just check that the file can be read completely
            reader = PntReader(file_path)
            result = reader.read()
            
            # Validate that metadata and data are consistent
            if result.get('metadata') and result.get('data'):
                metadata = result['metadata']
                data = result['data']
                
                # Check if package name matches
                if hasattr(metadata, 'package_name') and metadata.package_name:
                    if 'name' in data and data['name'] != metadata.package_name:
                        self._add_warning("Package name mismatch between metadata and data")
                
                # Check if version matches
                if hasattr(metadata, 'version') and metadata.version:
                    if 'version' in data and data['version'] != metadata.version:
                        self._add_warning("Version mismatch between metadata and data")
            
            return True
            
        except Exception as e:
            self._add_error(f"Cross-reference validation failed: {str(e)}")
            return False
    
    def _validate_performance(self, file_path: Path) -> bool:
        """Validate performance characteristics"""
        try:
            file_size = file_path.stat().st_size
            
            # Check file size is reasonable
            if file_size > 100 * 1024 * 1024:  # 100MB
                self._add_warning(f"File very large: {file_size} bytes")
            
            # Check compression ratio if compressed
            if hasattr(self, 'current_header') and self.current_header['flags'] & 0x08:
                # This would require reading the original data size
                # For now, just check if compression is reasonable
                pass
            
            return True
            
        except Exception as e:
            self._add_error(f"Performance validation failed: {str(e)}")
            return False
    
    def _parse_header(self, header_data: bytes) -> Dict:
        """Parse header from binary data"""
        return {
            'magic': header_data[0:4],
            'version': struct.unpack('<H', header_data[4:6])[0],
            'flags': struct.unpack('<H', header_data[6:8])[0],
            'compression': header_data[8],
            'encryption': header_data[9],
            'reserved1': header_data[10],
            'reserved2': header_data[11],
            'header_checksum': struct.unpack('<I', header_data[12:16])[0],
            'data_length': struct.unpack('<Q', header_data[16:24])[0],
            'timestamp': struct.unpack('<Q', header_data[24:32])[0]
        }
    
    def _calculate_header_checksum(self, header_data: bytes) -> int:
        """Calculate header checksum"""
        return zlib.crc32(header_data[:12])
    
    def _is_valid_version(self, version: str) -> bool:
        """Check if version string is valid"""
        # Basic version validation
        if not version:
            return False
        
        # Check for common version patterns
        import re
        patterns = [
            r'^\d+\.\d+\.\d+$',  # 1.0.0
            r'^\d+\.\d+$',       # 1.0
            r'^\d+$',            # 1
            r'^\d+\.\d+\.\d+[a-zA-Z0-9.-]*$',  # 1.0.0-alpha
        ]
        
        return any(re.match(pattern, version) for pattern in patterns)
    
    def _add_error(self, message: str):
        """Add an error message"""
        self.errors.append(message)
        self.stats['total_errors'] += 1
    
    def _add_warning(self, message: str):
        """Add a warning message"""
        self.warnings.append(message)
        self.stats['total_warnings'] += 1
    
    def validate_directory(self, directory: Path) -> bool:
        """Validate all .pnt files in a directory"""
        if not directory.exists():
            logger.error(f"Directory does not exist: {directory}")
            return False
        
        if not directory.is_dir():
            logger.error(f"Path is not a directory: {directory}")
            return False
        
        pnt_files = list(directory.glob('**/*.pnt'))
        if not pnt_files:
            logger.warning(f"No .pnt files found in {directory}")
            return True
        
        logger.info(f"Found {len(pnt_files)} .pnt files to validate")
        
        all_valid = True
        for file_path in pnt_files:
            if not self.validate_file(file_path):
                all_valid = False
        
        return all_valid
    
    def print_report(self):
        """Print validation report"""
        print("\n" + "="*60)
        print("TUSKLANG BINARY FORMAT VALIDATION REPORT")
        print("="*60)
        
        print(f"\nFiles checked: {self.stats['files_checked']}")
        print(f"Files valid: {self.stats['files_valid']}")
        print(f"Files invalid: {self.stats['files_invalid']}")
        print(f"Total errors: {self.stats['total_errors']}")
        print(f"Total warnings: {self.stats['total_warnings']}")
        
        if self.errors:
            print(f"\nERRORS ({len(self.errors)}):")
            for i, error in enumerate(self.errors, 1):
                print(f"  {i}. {error}")
        
        if self.warnings:
            print(f"\nWARNINGS ({len(self.warnings)}):")
            for i, warning in enumerate(self.warnings, 1):
                print(f"  {i}. {warning}")
        
        if not self.errors and not self.warnings:
            print("\n✓ All files passed validation!")
        elif not self.errors:
            print("\n⚠ Files have warnings but no errors")
        else:
            print("\n✗ Validation failed with errors")
        
        print("="*60)

def main():
    parser = argparse.ArgumentParser(description='Validate TuskLang binary format files')
    parser.add_argument('path', help='File or directory to validate')
    parser.add_argument('-v', '--verbose', action='store_true', help='Verbose output')
    parser.add_argument('-r', '--recursive', action='store_true', help='Recursively validate directories')
    parser.add_argument('--summary', action='store_true', help='Show summary only')
    
    args = parser.parse_args()
    
    # Configure logging
    if args.verbose:
        logging.getLogger().setLevel(logging.DEBUG)
    elif args.summary:
        logging.getLogger().setLevel(logging.WARNING)
    
    path = Path(args.path)
    validator = FormatValidator(verbose=args.verbose)
    
    try:
        if path.is_file():
            success = validator.validate_file(path)
        elif path.is_dir():
            if args.recursive:
                success = validator.validate_directory(path)
            else:
                logger.error("Use -r flag to validate directories")
                return 1
        else:
            logger.error(f"Path does not exist: {path}")
            return 1
        
        validator.print_report()
        return 0 if success else 1
        
    except KeyboardInterrupt:
        logger.info("Validation interrupted by user")
        return 1
    except Exception as e:
        logger.error(f"Validation failed: {e}")
        return 1

if __name__ == '__main__':
    sys.exit(main()) 