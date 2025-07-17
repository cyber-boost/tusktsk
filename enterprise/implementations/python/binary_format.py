#!/usr/bin/env python3
"""
TuskLang Binary Format - Python Reference Implementation
Version: 1.0
Author: Agent a2
"""

import struct
import zlib
import hashlib
import json
import time
from typing import Dict, List, Optional, Union, Any
from dataclasses import dataclass
from pathlib import Path
import logging

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Constants
MAGIC_BYTES = b'TUSK'
FORMAT_VERSION = 0x0100
MAX_FILE_SIZE = 1024 * 1024 * 1024  # 1GB
MAX_STRING_LENGTH = 65535

# Compression algorithms
COMPRESSION_NONE = 0
COMPRESSION_GZIP = 1
COMPRESSION_LZ4 = 2
COMPRESSION_ZSTD = 3

# Encryption algorithms
ENCRYPTION_NONE = 0
ENCRYPTION_AES256_GCM = 1
ENCRYPTION_CHACHA20_POLY1305 = 2

# Flags
FLAG_HAS_METADATA = 1 << 0
FLAG_HAS_ENCRYPTION = 1 << 1
FLAG_HAS_SIGNATURE = 1 << 2
FLAG_IS_COMPRESSED = 1 << 3
FLAG_HAS_DEPENDENCIES = 1 << 4
FLAG_HAS_KEYWORDS = 1 << 5

@dataclass
class PntMetadata:
    """Metadata for .pnt files"""
    package_name: str = ""
    version: str = ""
    author: str = ""
    description: str = ""
    license: str = ""
    repository: str = ""
    dependencies: List[Dict[str, str]] = None
    keywords: List[str] = None
    
    def __post_init__(self):
        if self.dependencies is None:
            self.dependencies = []
        if self.keywords is None:
            self.keywords = []

@dataclass
class PntHeader:
    """Header structure for .pnt files"""
    magic: bytes = MAGIC_BYTES
    version: int = FORMAT_VERSION
    flags: int = 0
    compression: int = COMPRESSION_NONE
    encryption: int = ENCRYPTION_NONE
    reserved1: int = 0
    reserved2: int = 0
    header_checksum: int = 0
    data_length: int = 0
    timestamp: int = 0

class PntReader:
    """Reader for TuskLang binary format files"""
    
    def __init__(self, file_path: Union[str, Path]):
        self.file_path = Path(file_path)
        self.header = None
        self.metadata = None
        self.data = None
        
    def read(self) -> Dict[str, Any]:
        """Read and parse a .pnt file"""
        logger.info(f"Reading .pnt file: {self.file_path}")
        
        if not self.file_path.exists():
            raise FileNotFoundError(f"File not found: {self.file_path}")
            
        if self.file_path.stat().st_size > MAX_FILE_SIZE:
            raise ValueError(f"File too large: {self.file_path.stat().st_size} bytes")
        
        with open(self.file_path, 'rb') as f:
            self._read_header(f)
            self._validate_header()
            
            if self.header.flags & FLAG_HAS_METADATA:
                self._read_metadata(f)
                
            self._read_data(f)
            
        return {
            'header': self.header,
            'metadata': self.metadata,
            'data': self.data
        }
    
    def _read_header(self, f):
        """Read the file header"""
        header_data = f.read(32)
        if len(header_data) < 32:
            raise ValueError("Invalid header: file too short")
            
        self.header = PntHeader()
        (
            self.header.magic,
            self.header.version,
            self.header.flags,
            self.header.compression,
            self.header.encryption,
            self.header.reserved1,
            self.header.reserved2,
            self.header.header_checksum,
            self.header.data_length,
            self.header.timestamp
        ) = struct.unpack('<4sHHBBBBIIQ', header_data)
        
        logger.debug(f"Header: {self.header}")
    
    def _validate_header(self):
        """Validate the file header"""
        if self.header.magic != MAGIC_BYTES:
            raise ValueError(f"Invalid magic bytes: {self.header.magic}")
            
        if self.header.version != FORMAT_VERSION:
            raise ValueError(f"Unsupported version: {self.header.version}")
            
        if self.header.reserved1 != 0 or self.header.reserved2 != 0:
            raise ValueError("Reserved fields must be zero")
            
        # Calculate and verify header checksum
        header_data = struct.pack('<4sHHBBBB', 
            self.header.magic,
            self.header.version,
            self.header.flags,
            self.header.compression,
            self.header.encryption,
            self.header.reserved1,
            self.header.reserved2
        )
        calculated_checksum = zlib.crc32(header_data)
        
        if calculated_checksum != self.header.header_checksum:
            raise ValueError(f"Header checksum mismatch: expected {self.header.header_checksum}, got {calculated_checksum}")
    
    def _read_metadata(self, f):
        """Read the metadata section"""
        metadata_length = struct.unpack('<I', f.read(4))[0]
        metadata_checksum = struct.unpack('<I', f.read(4))[0]
        
        metadata_data = f.read(metadata_length)
        if len(metadata_data) < metadata_length:
            raise ValueError("Invalid metadata: section too short")
            
        # Verify metadata checksum
        calculated_checksum = zlib.crc32(metadata_data)
        if calculated_checksum != metadata_checksum:
            raise ValueError(f"Metadata checksum mismatch: expected {metadata_checksum}, got {calculated_checksum}")
        
        # Parse metadata
        self.metadata = self._parse_metadata(metadata_data)
        logger.debug(f"Metadata: {self.metadata}")
    
    def _parse_metadata(self, data: bytes) -> PntMetadata:
        """Parse metadata from binary data"""
        offset = 0
        metadata = PntMetadata()
        
        # Read strings
        metadata.package_name = self._read_string(data, offset)
        offset += len(metadata.package_name) + 1
        
        metadata.version = self._read_string(data, offset)
        offset += len(metadata.version) + 1
        
        metadata.author = self._read_string(data, offset)
        offset += len(metadata.author) + 1
        
        metadata.description = self._read_string(data, offset)
        offset += len(metadata.description) + 1
        
        metadata.license = self._read_string(data, offset)
        offset += len(metadata.license) + 1
        
        metadata.repository = self._read_string(data, offset)
        offset += len(metadata.repository) + 1
        
        # Read dependencies
        if self.header.flags & FLAG_HAS_DEPENDENCIES:
            deps_count = struct.unpack('<I', data[offset:offset+4])[0]
            offset += 4
            
            for _ in range(deps_count):
                dep_name = self._read_string(data, offset)
                offset += len(dep_name) + 1
                
                dep_version = self._read_string(data, offset)
                offset += len(dep_version) + 1
                
                dep_type = data[offset]
                offset += 1
                
                metadata.dependencies.append({
                    'name': dep_name,
                    'version': dep_version,
                    'type': dep_type
                })
        
        # Read keywords
        if self.header.flags & FLAG_HAS_KEYWORDS:
            keywords_count = struct.unpack('<I', data[offset:offset+4])[0]
            offset += 4
            
            for _ in range(keywords_count):
                keyword = self._read_string(data, offset)
                offset += len(keyword) + 1
                metadata.keywords.append(keyword)
        
        return metadata
    
    def _read_string(self, data: bytes, offset: int) -> str:
        """Read a null-terminated string from binary data"""
        end = data.find(b'\x00', offset)
        if end == -1:
            raise ValueError("String not null-terminated")
        return data[offset:end].decode('utf-8')
    
    def _read_data(self, f):
        """Read the data section"""
        data_length = struct.unpack('<I', f.read(4))[0]
        data = f.read(data_length)
        
        if len(data) < data_length:
            raise ValueError("Invalid data: section too short")
        
        # Verify data checksum
        data_checksum = struct.unpack('<I', f.read(4))[0]
        calculated_checksum = int.from_bytes(hashlib.sha256(data).digest()[:4], 'little')
        
        if calculated_checksum != data_checksum:
            raise ValueError(f"Data checksum mismatch: expected {data_checksum}, got {calculated_checksum}")
        
        # Decompress if needed
        if self.header.flags & FLAG_IS_COMPRESSED:
            data = self._decompress_data(data)
        
        # Decrypt if needed
        if self.header.flags & FLAG_HAS_ENCRYPTION:
            data = self._decrypt_data(data)
        
        # Parse as JSON
        try:
            self.data = json.loads(data.decode('utf-8'))
        except json.JSONDecodeError as e:
            raise ValueError(f"Invalid JSON data: {e}")
        
        logger.debug(f"Data loaded: {len(data)} bytes")
    
    def _decompress_data(self, data: bytes) -> bytes:
        """Decompress data based on compression algorithm"""
        if self.header.compression == COMPRESSION_GZIP:
            return zlib.decompress(data)
        elif self.header.compression == COMPRESSION_NONE:
            return data
        else:
            raise ValueError(f"Unsupported compression: {self.header.compression}")
    
    def _decrypt_data(self, data: bytes) -> bytes:
        """Decrypt data based on encryption algorithm"""
        if self.header.encryption == ENCRYPTION_NONE:
            return data
        else:
            raise ValueError(f"Unsupported encryption: {self.header.encryption}")

class PntWriter:
    """Writer for TuskLang binary format files"""
    
    def __init__(self, file_path: Union[str, Path]):
        self.file_path = Path(file_path)
        self.header = PntHeader()
        self.metadata = PntMetadata()
        self.data = {}
    
    def set_metadata(self, metadata: PntMetadata):
        """Set metadata for the file"""
        self.metadata = metadata
        self.header.flags |= FLAG_HAS_METADATA
        
        if metadata.dependencies:
            self.header.flags |= FLAG_HAS_DEPENDENCIES
            
        if metadata.keywords:
            self.header.flags |= FLAG_HAS_KEYWORDS
    
    def set_data(self, data: Dict[str, Any]):
        """Set data for the file"""
        self.data = data
    
    def set_compression(self, compression: int):
        """Set compression algorithm"""
        self.header.compression = compression
        if compression != COMPRESSION_NONE:
            self.header.flags |= FLAG_IS_COMPRESSED
    
    def set_encryption(self, encryption: int):
        """Set encryption algorithm"""
        self.header.encryption = encryption
        if encryption != ENCRYPTION_NONE:
            self.header.flags |= FLAG_HAS_ENCRYPTION
    
    def write(self):
        """Write the .pnt file"""
        logger.info(f"Writing .pnt file: {self.file_path}")
        
        # Prepare data
        data_json = json.dumps(self.data, separators=(',', ':')).encode('utf-8')
        
        # Compress if needed
        if self.header.flags & FLAG_IS_COMPRESSED:
            data_json = self._compress_data(data_json)
        
        # Encrypt if needed
        if self.header.flags & FLAG_HAS_ENCRYPTION:
            data_json = self._encrypt_data(data_json)
        
        # Prepare metadata
        metadata_data = self._serialize_metadata()
        
        # Update header
        self.header.data_length = len(data_json)
        self.header.timestamp = int(time.time() * 1_000_000_000)  # nanoseconds
        
        # Calculate header checksum
        header_data = struct.pack('<4sHHBBBB',
            self.header.magic,
            self.header.version,
            self.header.flags,
            self.header.compression,
            self.header.encryption,
            self.header.reserved1,
            self.header.reserved2
        )
        self.header.header_checksum = zlib.crc32(header_data)
        
        # Write file
        with open(self.file_path, 'wb') as f:
            # Write header
            f.write(struct.pack('<4sHHBBBBIIQ',
                self.header.magic,
                self.header.version,
                self.header.flags,
                self.header.compression,
                self.header.encryption,
                self.header.reserved1,
                self.header.reserved2,
                self.header.header_checksum,
                self.header.data_length,
                self.header.timestamp
            ))
            
            # Write metadata
            if self.header.flags & FLAG_HAS_METADATA:
                metadata_checksum = zlib.crc32(metadata_data)
                f.write(struct.pack('<II', len(metadata_data), metadata_checksum))
                f.write(metadata_data)
            
            # Write data
            data_checksum = int.from_bytes(hashlib.sha256(data_json).digest()[:4], 'little')
            f.write(struct.pack('<I', len(data_json)))
            f.write(data_json)
            f.write(struct.pack('<I', data_checksum))
        
        logger.info(f"Successfully wrote .pnt file: {self.file_path}")
    
    def _serialize_metadata(self) -> bytes:
        """Serialize metadata to binary format"""
        data = bytearray()
        
        # Write strings
        for string in [
            self.metadata.package_name,
            self.metadata.version,
            self.metadata.author,
            self.metadata.description,
            self.metadata.license,
            self.metadata.repository
        ]:
            data.extend(string.encode('utf-8'))
            data.append(0)  # null terminator
        
        # Write dependencies
        if self.header.flags & FLAG_HAS_DEPENDENCIES:
            data.extend(struct.pack('<I', len(self.metadata.dependencies)))
            for dep in self.metadata.dependencies:
                data.extend(dep['name'].encode('utf-8'))
                data.append(0)
                data.extend(dep['version'].encode('utf-8'))
                data.append(0)
                data.append(dep.get('type', 0))
        
        # Write keywords
        if self.header.flags & FLAG_HAS_KEYWORDS:
            data.extend(struct.pack('<I', len(self.metadata.keywords)))
            for keyword in self.metadata.keywords:
                data.extend(keyword.encode('utf-8'))
                data.append(0)
        
        return bytes(data)
    
    def _compress_data(self, data: bytes) -> bytes:
        """Compress data based on compression algorithm"""
        if self.header.compression == COMPRESSION_GZIP:
            return zlib.compress(data)
        elif self.header.compression == COMPRESSION_NONE:
            return data
        else:
            raise ValueError(f"Unsupported compression: {self.header.compression}")
    
    def _encrypt_data(self, data: bytes) -> bytes:
        """Encrypt data based on encryption algorithm"""
        if self.header.encryption == ENCRYPTION_NONE:
            return data
        else:
            raise ValueError(f"Unsupported encryption: {self.header.encryption}")

def read_pnt_file(file_path: Union[str, Path]) -> Dict[str, Any]:
    """Read and parse a .pnt file"""
    reader = PntReader(file_path)
    return reader.read()

def write_pnt_file(file_path: Union[str, Path], data: Dict[str, Any], 
                   metadata: Optional[PntMetadata] = None) -> None:
    """Write a .pnt file"""
    writer = PntWriter(file_path)
    writer.set_data(data)
    
    if metadata:
        writer.set_metadata(metadata)
    
    writer.write()

def validate_pnt_file(file_path: Union[str, Path]) -> bool:
    """Validate a .pnt file"""
    try:
        reader = PntReader(file_path)
        reader.read()
        return True
    except Exception as e:
        logger.error(f"Validation failed: {e}")
        return False

def get_pnt_metadata(file_path: Union[str, Path]) -> Optional[PntMetadata]:
    """Extract metadata from a .pnt file"""
    try:
        reader = PntReader(file_path)
        result = reader.read()
        return result.get('metadata')
    except Exception as e:
        logger.error(f"Failed to extract metadata: {e}")
        return None

def convert_to_pnt(source_format: str, source_data: Union[str, Dict[str, Any]], 
                   output_path: Union[str, Path]) -> None:
    """Convert from other formats to .pnt"""
    if source_format.lower() == 'json':
        if isinstance(source_data, str):
            data = json.loads(source_data)
        else:
            data = source_data
    elif source_format.lower() == 'yaml':
        import yaml
        if isinstance(source_data, str):
            data = yaml.safe_load(source_data)
        else:
            data = source_data
    else:
        raise ValueError(f"Unsupported source format: {source_format}")
    
    write_pnt_file(output_path, data)

if __name__ == "__main__":
    # Example usage
    import tempfile
    
    # Create test data
    test_data = {
        "name": "test-package",
        "version": "1.0.0",
        "config": {
            "debug": True,
            "port": 8080,
            "hosts": ["localhost", "127.0.0.1"]
        }
    }
    
    test_metadata = PntMetadata(
        package_name="test-package",
        version="1.0.0",
        author="Agent a2",
        description="Test package for binary format",
        license="MIT",
        repository="https://github.com/test/test-package",
        dependencies=[
            {"name": "requests", "version": ">=2.25.0", "type": 1}
        ],
        keywords=["test", "binary", "format"]
    )
    
    # Write test file
    with tempfile.NamedTemporaryFile(suffix='.pnt', delete=False) as f:
        test_file = f.name
    
    try:
        write_pnt_file(test_file, test_data, test_metadata)
        print(f"Wrote test file: {test_file}")
        
        # Read and validate
        result = read_pnt_file(test_file)
        print(f"Read data: {result['data']}")
        print(f"Metadata: {result['metadata']}")
        
        # Validate
        is_valid = validate_pnt_file(test_file)
        print(f"Validation: {is_valid}")
        
        # Extract metadata
        metadata = get_pnt_metadata(test_file)
        print(f"Extracted metadata: {metadata}")
        
    finally:
        # Cleanup
        Path(test_file).unlink(missing_ok=True) 