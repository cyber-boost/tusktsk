# TuskLang Binary Format Specification (.pnt)

## Version 1.0.0

This document defines the binary format specification for TuskLang configuration files (.pnt). This format provides efficient storage, fast parsing, and cross-platform compatibility across all TuskLang SDKs.

## File Structure Overview

```
┌─────────────────┬─────────────────┬─────────────────┬─────────────────┐
│     Header      │   Data Section  │   Index Table   │   Footer        │
│   (64 bytes)    │   (variable)    │   (variable)    │   (16 bytes)    │
└─────────────────┴─────────────────┴─────────────────┴─────────────────┘
```

## Header Structure (64 bytes)

| Offset | Size | Field | Description | Value |
|--------|------|-------|-------------|-------|
| 0x00   | 4    | Magic | File identifier | `0x50 0x4E 0x54 0x00` ("PNT\0") |
| 0x04   | 1    | Major | Major version | `0x01` |
| 0x05   | 1    | Minor | Minor version | `0x00` |
| 0x06   | 1    | Patch | Patch version | `0x00` |
| 0x07   | 1    | Flags | Feature flags | Bitmap (see below) |
| 0x08   | 8    | DataOffset | Offset to data section | Little-endian uint64 |
| 0x10   | 8    | DataSize | Size of data section | Little-endian uint64 |
| 0x18   | 8    | IndexOffset | Offset to index table | Little-endian uint64 |
| 0x20   | 8    | IndexSize | Size of index table | Little-endian uint64 |
| 0x28   | 8    | Checksum | CRC32 of header | Little-endian uint32 + padding |
| 0x30   | 8    | Reserved | Reserved for future use | `0x00` |
| 0x38   | 8    | Reserved | Reserved for future use | `0x00` |
| 0x40   | 8    | Reserved | Reserved for future use | `0x00` |
| 0x48   | 8    | Reserved | Reserved for future use | `0x00` |
| 0x50   | 8    | Reserved | Reserved for future use | `0x00` |
| 0x58   | 8    | Reserved | Reserved for future use | `0x00` |

### Feature Flags (byte 0x07)

| Bit | Flag | Description |
|-----|------|-------------|
| 0   | COMPRESSED | Data section is compressed |
| 1   | ENCRYPTED | Data section is encrypted |
| 2   | INDEXED | Index table is present |
| 3   | VALIDATED | File has been validated |
| 4   | STREAMING | Supports streaming access |
| 5   | RESERVED | Reserved for future use |
| 6   | RESERVED | Reserved for future use |
| 7   | RESERVED | Reserved for future use |

## Data Section

The data section contains the serialized configuration data in a structured format. All values are stored in little-endian byte order.

### Type Identifiers

| ID | Type | Size | Description |
|----|------|------|-------------|
| 0x00 | NULL | 0 | Null value |
| 0x01 | BOOL | 1 | Boolean (0x00=false, 0x01=true) |
| 0x02 | INT8 | 1 | 8-bit signed integer |
| 0x03 | UINT8 | 1 | 8-bit unsigned integer |
| 0x04 | INT16 | 2 | 16-bit signed integer |
| 0x05 | UINT16 | 2 | 16-bit unsigned integer |
| 0x06 | INT32 | 4 | 32-bit signed integer |
| 0x07 | UINT32 | 4 | 32-bit unsigned integer |
| 0x08 | INT64 | 8 | 64-bit signed integer |
| 0x09 | UINT64 | 8 | 64-bit unsigned integer |
| 0x0A | FLOAT32 | 4 | 32-bit IEEE 754 float |
| 0x0B | FLOAT64 | 8 | 64-bit IEEE 754 double |
| 0x0C | STRING | variable | UTF-8 string with length prefix |
| 0x0D | BYTES | variable | Raw bytes with length prefix |
| 0x0E | ARRAY | variable | Array with length prefix |
| 0x0F | OBJECT | variable | Object with key-value pairs |
| 0x10 | REFERENCE | 8 | Reference to another value (offset) |
| 0x11 | TIMESTAMP | 8 | Unix timestamp (seconds since epoch) |
| 0x12 | DURATION | 8 | Duration in nanoseconds |
| 0x13 | DECIMAL | 16 | Decimal number (128-bit) |

### Length Encoding

Variable-length fields use a compact length encoding:

- **1-127 bytes**: Single byte with value 0x01-0x7F
- **128-16383 bytes**: Two bytes, first byte has bit 7 set (0x80-0xFF), second byte is 0x00-0x7F
- **16384+ bytes**: Four bytes, first byte is 0xFF, followed by 3-byte length

### String Encoding

Strings are stored as:
1. Length (compact encoding)
2. UTF-8 bytes (no null terminator)

### Array Encoding

Arrays are stored as:
1. Length (compact encoding)
2. Type identifier for all elements
3. Elements (if primitive types)
4. Element offsets (if complex types)

### Object Encoding

Objects are stored as:
1. Number of key-value pairs (compact encoding)
2. For each pair:
   - Key string (length + UTF-8 bytes)
   - Value type identifier
   - Value data or offset

## Index Table

The index table provides fast access to specific configuration keys without parsing the entire data section.

### Index Entry Structure

| Offset | Size | Field | Description |
|--------|------|-------|-------------|
| 0x00   | 2    | KeyLength | Length of key string |
| 0x02   | variable | Key | Key string (UTF-8) |
| 0x02+n | 8    | ValueOffset | Offset to value in data section |
| 0x0A+n | 1    | ValueType | Type identifier of value |
| 0x0B+n | 4    | ValueSize | Size of value in bytes |

## Footer Structure (16 bytes)

| Offset | Size | Field | Description |
|--------|------|-------|-------------|
| 0x00   | 4    | Magic | Footer identifier | `0x00 0x54 0x4E 0x50` ("\0TNP") |
| 0x04   | 4    | FileSize | Total file size | Little-endian uint32 |
| 0x08   | 4    | Checksum | CRC32 of entire file | Little-endian uint32 |
| 0x0C   | 4    | Reserved | Reserved for future use | `0x00` |

## Compression

When the COMPRESSED flag is set, the data section is compressed using zstd:

1. Original data size (8 bytes, little-endian)
2. Compressed data size (8 bytes, little-endian)
3. zstd compressed data

## Encryption

When the ENCRYPTED flag is set, the data section is encrypted using AES-256-GCM:

1. Nonce (12 bytes)
2. Encrypted data size (8 bytes, little-endian)
3. Encrypted data
4. Authentication tag (16 bytes)

## Version Compatibility

### Forward Compatibility
- Readers must ignore unknown fields
- Unknown type identifiers should be skipped
- Reserved fields must be preserved

### Backward Compatibility
- Writers must maintain field order
- Deprecated fields should be set to zero
- Version field determines supported features

### Migration Strategy
- Version 1.0.x: Basic format support
- Version 1.1.x: Compression support
- Version 1.2.x: Encryption support
- Version 2.0.x: Breaking changes (new magic bytes)

## Error Handling

### Common Error Conditions
- Invalid magic bytes
- Unsupported version
- Corrupted checksum
- Invalid type identifier
- Malformed length encoding
- Truncated file

### Recovery Strategies
- Attempt to read partial data
- Use backup files if available
- Fall back to text format
- Report detailed error with byte offset

## Performance Considerations

### Optimization Techniques
- Memory-mapped file access
- Lazy loading of sections
- Streaming parsing for large files
- Index table for fast key lookup
- Buffer pooling for memory efficiency

### Benchmarks
- Parse time: <1ms for 1KB files
- Memory usage: <2x file size
- Compression ratio: 60-80% for typical configs
- Encryption overhead: <5% performance impact

## Implementation Guidelines

### Required Features
- Header validation
- Checksum verification
- Type safety
- Error reporting
- Memory management

### Optional Features
- Compression
- Encryption
- Indexing
- Streaming
- Validation

### Testing Requirements
- Unit tests for each type
- Property-based testing
- Fuzz testing
- Performance benchmarks
- Cross-platform compatibility

## Example File

```
00000000: 504E 5400 0100 0000 0000 0000 0000 0040  PNT.............@
00000010: 0000 0000 0000 0000 0000 0000 0000 0080  ................
00000020: 0000 0000 0000 0000 1234 5678 0000 0000  .........4Vx....
00000030: 0000 0000 0000 0000 0000 0000 0000 0000  ................
00000040: 0000 0000 0000 0000 0000 0000 0000 0000  ................
00000050: 0000 0000 0000 0000 0000 0000 0000 0000  ................
00000060: 0F01 0000 0000 0000 0000 0000 0000 0000  ................
00000070: 0C03 6462 5F68 6F73 740C 0F6C 6F63 616C  ..db_host..local
00000080: 686F 7374 0C03 6462 5F70 6F72 7406 0000  host..db_port...
00000090: 0000 0000 0000 0000 0000 0000 0000 0000  ................
000000A0: 0054 4E50 0000 0000 0000 0000 0000 0000  .TNP............
```

This example shows a simple configuration with `db_host = "localhost"` and `db_port = 5432`.

## Security Considerations

### Encryption
- Use strong random number generation
- Implement proper key management
- Validate authentication tags
- Protect against timing attacks

### Validation
- Verify all offsets are within file bounds
- Check for integer overflow
- Validate UTF-8 encoding
- Prevent circular references

### Best Practices
- Use secure random for nonces
- Implement rate limiting for decryption
- Log security events
- Regular security audits 