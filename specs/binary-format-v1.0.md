# TuskLang Binary Format Specification v1.0

## Overview
The TuskLang Binary Format (.pnt) is a unified, efficient, and secure format for storing configuration data across all TuskLang SDKs. This specification defines the complete binary structure, encoding rules, and implementation requirements.

## Version Information
- **Format Version**: 1.0
- **Magic Bytes**: "TUSK"
- **Endianness**: Little-endian (network byte order)
- **Alignment**: 8-byte aligned
- **Maximum File Size**: 1GB

## File Structure

### Header Section (32 bytes)
```
Offset  Size    Type     Description
0       4       char[4]  Magic bytes: "TUSK"
4       2       uint16   Format version (1.0 = 0x0100)
6       2       uint16   Flags (bitmap)
8       1       uint8    Compression algorithm
9       1       uint8    Encryption algorithm
10      1       uint8    Reserved (must be 0)
11      1       uint8    Reserved (must be 0)
12      4       uint32   Header checksum (CRC32)
16      8       uint64   Data section length
24      8       uint64   Timestamp (Unix epoch, nanoseconds)
```

### Flags Bitmap
```
Bit 0: Has metadata section
Bit 1: Has encryption
Bit 2: Has digital signature
Bit 3: Is compressed
Bit 4: Has dependencies
Bit 5: Has keywords
Bit 6: Reserved
Bit 7: Reserved
Bit 8-15: Reserved
```

### Compression Algorithms
```
0: None
1: gzip (RFC 1952)
2: lz4 (LZ4 frame format)
3: zstd (Zstandard)
4-255: Reserved
```

### Encryption Algorithms
```
0: None
1: AES-256-GCM
2: ChaCha20-Poly1305
3-255: Reserved
```

### Metadata Section (Variable length)
```
Offset  Size    Type     Description
0       4       uint32   Metadata length
4       4       uint32   Metadata checksum (CRC32)
8       var     string   Package name (UTF-8, null-terminated)
var     var     string   Version (UTF-8, null-terminated)
var     var     string   Author (UTF-8, null-terminated)
var     var     string   Description (UTF-8, null-terminated)
var     var     string   License (UTF-8, null-terminated)
var     var     string   Repository (UTF-8, null-terminated)
var     4       uint32   Dependencies count
var     var     array    Dependencies array
var     4       uint32   Keywords count
var     var     array    Keywords array
```

### Dependencies Array
```
Offset  Size    Type     Description
0       4       uint32   Dependency name length
4       var     string   Dependency name (UTF-8)
var     4       uint32   Version constraint length
var     var     string   Version constraint (UTF-8)
var     1       uint8    Dependency type
```

### Keywords Array
```
Offset  Size    Type     Description
0       4       uint32   Keyword length
4       var     string   Keyword (UTF-8)
```

### Data Section (Variable length)
```
Offset  Size    Type     Description
0       4       uint32   Data length
4       var     bytes    Configuration data (compressed/encrypted)
var     4       uint32   Data checksum (SHA-256 truncated to 32 bits)
```

### Digital Signature Section (Optional, 64 bytes)
```
Offset  Size    Type     Description
0       32      bytes    Ed25519 signature
32      32      bytes    Public key
```

## Encoding Rules

### String Encoding
- All strings use UTF-8 encoding
- Strings are null-terminated
- Maximum string length: 65,535 bytes
- Empty strings are represented as single null byte

### Integer Encoding
- All integers use little-endian byte order
- uint8: 1 byte
- uint16: 2 bytes
- uint32: 4 bytes
- uint64: 8 bytes

### Array Encoding
- Arrays are prefixed with 4-byte count
- Maximum array size: 16,777,215 elements
- Array elements are stored sequentially

### Checksum Calculation
- Header checksum: CRC32 of bytes 0-11 (excluding checksum field)
- Metadata checksum: CRC32 of entire metadata section
- Data checksum: First 4 bytes of SHA-256 hash

## Security Features

### Encryption
When encryption is enabled:
1. Generate random 32-byte key
2. Generate random 12-byte nonce (AES-GCM) or 12-byte nonce (ChaCha20-Poly1305)
3. Encrypt data section with authenticated encryption
4. Store key encrypted with master key (if available)

### Digital Signatures
When digital signatures are enabled:
1. Calculate SHA-256 hash of header + metadata + data sections
2. Sign hash with Ed25519 private key
3. Store signature and public key in signature section

### Validation
1. Verify magic bytes
2. Validate header checksum
3. Check format version compatibility
4. Verify metadata checksum
5. Validate data checksum
6. Verify digital signature (if present)

## Performance Requirements

### Load Time Targets
- Small files (<1KB): <10ms
- Medium files (1KB-1MB): <50ms
- Large files (1MB-10MB): <200ms
- Very large files (10MB-100MB): <1s

### Memory Usage
- Streaming read: <1MB memory regardless of file size
- Full load: <10MB for 100MB files
- Peak memory: <50MB for any file size

### Compression Ratios
- Text configurations: >70% compression
- Binary configurations: >50% compression
- Already compressed data: >10% additional compression

## Error Handling

### Validation Errors
- Invalid magic bytes: "Invalid TuskLang binary file"
- Unsupported version: "Unsupported format version"
- Corrupted header: "Header checksum validation failed"
- Corrupted metadata: "Metadata checksum validation failed"
- Corrupted data: "Data checksum validation failed"
- Invalid signature: "Digital signature validation failed"

### Recovery Strategies
1. Attempt to read without compression
2. Attempt to read without encryption
3. Attempt to read without metadata
4. Provide detailed error information
5. Suggest recovery tools

## Compatibility Matrix

### Version Compatibility
```
Format Version  Backward Compatible  Forward Compatible
1.0             N/A                  Yes (with limitations)
1.1             Yes                  Yes (with limitations)
2.0             No                   N/A
```

### SDK Compatibility
- Python SDK: 1.0+
- Go SDK: 1.0+
- Rust SDK: 1.0+
- Java SDK: 1.0+
- JavaScript SDK: 1.0+

## Implementation Guidelines

### Required Functions
All SDK implementations must provide:
1. `read_pnt_file(path)` - Read and parse .pnt file
2. `write_pnt_file(path, data, metadata)` - Write .pnt file
3. `validate_pnt_file(path)` - Validate file integrity
4. `get_pnt_metadata(path)` - Extract metadata only
5. `convert_to_pnt(source_format, source_data)` - Convert from other formats

### Error Codes
```
0: Success
1: File not found
2: Invalid format
3: Corrupted data
4: Unsupported version
5: Encryption error
6: Signature error
7: Compression error
8: Memory error
9: I/O error
10: Validation error
```

### Logging Requirements
- Log all file operations with timestamps
- Log validation failures with details
- Log performance metrics for large files
- Log security events (signature verification, etc.)

## Migration from Text Formats

### YAML to .pnt
1. Parse YAML structure
2. Extract metadata from YAML front matter
3. Compress YAML content
4. Add TuskLang header
5. Write binary file

### JSON to .pnt
1. Parse JSON structure
2. Extract metadata from JSON schema
3. Compress JSON content
4. Add TuskLang header
5. Write binary file

### TOML to .pnt
1. Parse TOML structure
2. Extract metadata from TOML headers
3. Compress TOML content
4. Add TuskLang header
5. Write binary file

## Testing Requirements

### Unit Tests
- Header validation
- Metadata parsing
- Data compression/decompression
- Encryption/decryption
- Signature verification
- Error handling

### Integration Tests
- Cross-SDK compatibility
- Format conversion
- Performance benchmarks
- Security validation
- Large file handling

### Stress Tests
- Maximum file sizes
- Concurrent access
- Memory pressure
- Network conditions
- Corrupted data recovery

## Future Extensions

### Version 1.1 Features
- Streaming read/write support
- Incremental updates
- Delta compression
- Multi-part files
- External references

### Version 2.0 Features
- Schema validation
- Type safety
- Versioned schemas
- Plugin system
- Advanced compression

## References

### Standards
- RFC 1952: GZIP file format
- RFC 8439: ChaCha20 and Poly1305
- RFC 8032: Ed25519 signatures
- ISO/IEC 14496-12: MP4 file format (inspiration)

### Libraries
- Python: struct, zlib, cryptography
- Go: encoding/binary, compress/gzip, crypto/aes
- Rust: byteorder, flate2, ring
- Java: java.nio, java.util.zip, javax.crypto
- JavaScript: Buffer, zlib, crypto

## Appendix

### Example File Structure
```
00000000: 5455 534b 0100 0000 0100 0000 0000 0000  TUSK............
00000010: a1b2c3d4 00000000 00000000 00000000 00000000 00000000
00000020: 00000000 00000000 00000000 00000000 00000000 00000000
00000030: 00000000 00000000 00000000 00000000 00000000 00000000
...
```

### Checksum Examples
- CRC32 of "TUSK": 0x12345678
- SHA-256 of empty data: 0xe3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 