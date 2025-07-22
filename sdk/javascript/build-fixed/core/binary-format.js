/**
 * TuskLang Binary Format Implementation for JavaScript
 * 
 * This module provides a complete implementation of the TuskLang binary format (.pnt)
 * that is compatible with the Go reference implementation and supports both Node.js
 * and browser environments.
 */

// Type identifiers
const TypeIdentifier = {
    NULL: 0x00,
    BOOL: 0x01,
    INT8: 0x02,
    UINT8: 0x03,
    INT16: 0x04,
    UINT16: 0x05,
    INT32: 0x06,
    UINT32: 0x07,
    INT64: 0x08,
    UINT64: 0x09,
    FLOAT32: 0x0A,
    FLOAT64: 0x0B,
    STRING: 0x0C,
    BYTES: 0x0D,
    ARRAY: 0x0E,
    OBJECT: 0x0F,
    REFERENCE: 0x10,
    TIMESTAMP: 0x11,
    DURATION: 0x12,
    DECIMAL: 0x13
};

// Feature flags
const FeatureFlags = {
    COMPRESSED: 1 << 0,
    ENCRYPTED: 1 << 1,
    INDEXED: 1 << 2,
    VALIDATED: 1 << 3,
    STREAMING: 1 << 4
};

// Magic bytes
const MAGIC_HEADER = new Uint8Array([0x50, 0x4E, 0x54, 0x00]); // "PNT\0"
const MAGIC_FOOTER = new Uint8Array([0x00, 0x54, 0x4E, 0x50]); // "\0TNP"

// Version constants
const VERSION_MAJOR = 1;
const VERSION_MINOR = 0;
const VERSION_PATCH = 0;

/**
 * Binary format error class
 */
class BinaryFormatError extends Error {
    constructor(message, offset = 0, errorType = "unknown") {
        super(`binary format error at offset ${offset} (${errorType}): ${message}`);
        this.name = "BinaryFormatError";
        this.message = message;
        this.offset = offset;
        this.errorType = errorType;
    }
}

/**
 * Header structure
 */
class Header {
    constructor() {
        this.magic = new Uint8Array(MAGIC_HEADER);
        this.major = VERSION_MAJOR;
        this.minor = VERSION_MINOR;
        this.patch = VERSION_PATCH;
        this.flags = 0;
        this.dataOffset = 64;
        this.dataSize = 0;
        this.indexOffset = 0;
        this.indexSize = 0;
        this.checksum = 0;
        this.reserved = new Uint8Array(40);
    }
}

/**
 * Footer structure
 */
class Footer {
    constructor() {
        this.magic = new Uint8Array(MAGIC_FOOTER);
        this.fileSize = 0;
        this.checksum = 0;
        this.reserved = new Uint8Array(4);
    }
}

/**
 * Index entry structure
 */
class IndexEntry {
    constructor() {
        this.keyLength = 0;
        this.key = "";
        this.valueOffset = 0;
        this.valueType = 0;
        this.valueSize = 0;
    }
}

/**
 * Value structure
 */
class Value {
    constructor(typeId, data) {
        this.typeId = typeId;
        this.data = data;
        this.size = 0;
        this.offset = 0;
    }
}

/**
 * Object structure
 */
class Object {
    constructor(entries = {}) {
        this.entries = entries;
        this.size = 0;
        this.offset = 0;
    }
}

/**
 * Array structure
 */
class Array {
    constructor(elements = []) {
        this.elements = elements;
        this.size = 0;
        this.offset = 0;
    }
}

/**
 * Encode length using compact encoding
 */
function encodeLength(length) {
    if (length <= 127) {
        return new Uint8Array([length]);
    } else if (length <= 16383) {
        const buffer = new ArrayBuffer(2);
        const view = new DataView(buffer);
        view.setUint16(0, length | 0x8000, true); // little-endian
        return new Uint8Array(buffer);
    } else {
        const buffer = new ArrayBuffer(4);
        const view = new DataView(buffer);
        view.setUint8(0, 0xFF);
        view.setUint32(1, length, true); // little-endian
        return new Uint8Array(buffer);
    }
}

/**
 * Decode length from compact encoding
 */
function decodeLength(dataView, offset) {
    const firstByte = dataView.getUint8(offset);
    
    if (firstByte <= 127) {
        return { length: firstByte, bytesRead: 1 };
    } else if (firstByte <= 0xFF) {
        const secondByte = dataView.getUint8(offset + 1);
        const length = ((firstByte & 0x7F) << 8) | secondByte;
        return { length, bytesRead: 2 };
    } else {
        const length = dataView.getUint32(offset + 1, true); // little-endian
        return { length, bytesRead: 4 };
    }
}

/**
 * Calculate CRC32 checksum
 */
function calculateCRC32(data) {
    let crc = 0xFFFFFFFF;
    const table = new Uint32Array(256);
    
    // Generate CRC32 table
    for (let i = 0; i < 256; i++) {
        let c = i;
        for (let j = 0; j < 8; j++) {
            c = (c & 1) ? (0xEDB88320 ^ (c >>> 1)) : (c >>> 1);
        }
        table[i] = c;
    }
    
    // Calculate CRC32
    for (let i = 0; i < data.length; i++) {
        crc = table[(crc ^ data[i]) & 0xFF] ^ (crc >>> 8);
    }
    
    return (crc ^ 0xFFFFFFFF) >>> 0;
}

/**
 * Binary reader for .pnt files
 */
class BinaryReader {
    constructor(filename) {
        this.filename = filename;
        this.file = null;
        this.offset = 0;
        this.buffer = null;
        this.dataView = null;
    }
    
    /**
     * Open file for reading
     */
    async open() {
        if (typeof window !== 'undefined') {
            // Browser environment - use File API
            const response = await fetch(this.filename);
            const arrayBuffer = await response.arrayBuffer();
            this.buffer = new Uint8Array(arrayBuffer);
            this.dataView = new DataView(arrayBuffer);
        } else {
            // Node.js environment
            const fs = require('fs');
            const buffer = fs.readFileSync(this.filename);
            this.buffer = new Uint8Array(buffer);
            this.dataView = new DataView(buffer.buffer);
        }
    }
    
    /**
     * Read and validate file header
     */
    readHeader() {
        if (!this.dataView) {
            throw new BinaryFormatError("file not open", 0, "file_not_open");
        }
        
        const header = new Header();
        
        // Read magic bytes
        header.magic = new Uint8Array(this.buffer.slice(0, 4));
        
        // Read version and flags
        header.major = this.dataView.getUint8(4);
        header.minor = this.dataView.getUint8(5);
        header.patch = this.dataView.getUint8(6);
        header.flags = this.dataView.getUint8(7);
        
        // Read offsets and sizes
        header.dataOffset = this.dataView.getBigUint64(8, true);
        header.dataSize = this.dataView.getBigUint64(16, true);
        header.indexOffset = this.dataView.getBigUint64(24, true);
        header.indexSize = this.dataView.getBigUint64(32, true);
        
        // Read checksum
        header.checksum = this.dataView.getUint32(40, true);
        
        // Read reserved bytes
        header.reserved = new Uint8Array(this.buffer.slice(44, 64));
        
        // Validate magic bytes
        if (!header.magic.every((byte, i) => byte === MAGIC_HEADER[i])) {
            throw new BinaryFormatError(
                `invalid magic bytes: expected ${MAGIC_HEADER}, got ${header.magic}`,
                this.offset, "magic_validation"
            );
        }
        
        // Validate version
        if (header.major !== VERSION_MAJOR) {
            throw new BinaryFormatError(
                `unsupported major version: ${header.major}`,
                this.offset, "version_validation"
            );
        }
        
        // Validate checksum
        const checksumData = new Uint8Array([
            ...this.buffer.slice(0, 40),
            ...this.buffer.slice(44, 64)
        ]);
        const calculatedChecksum = calculateCRC32(checksumData);
        
        if (header.checksum !== calculatedChecksum) {
            throw new BinaryFormatError(
                `header checksum mismatch: expected ${calculatedChecksum.toString(16)}, got ${header.checksum.toString(16)}`,
                this.offset, "checksum_validation"
            );
        }
        
        this.offset = 64;
        return header;
    }
    
    /**
     * Read a single value from current position
     */
    readValue() {
        if (!this.dataView) {
            throw new BinaryFormatError("file not open", this.offset, "file_not_open");
        }
        
        // Read type identifier
        const typeId = this.dataView.getUint8(this.offset);
        this.offset++;
        
        const value = new Value(typeId);
        value.offset = this.offset;
        
        // Read value data based on type
        switch (typeId) {
            case TypeIdentifier.NULL:
                value.data = null;
                value.size = 0;
                break;
                
            case TypeIdentifier.BOOL:
                value.data = this.dataView.getUint8(this.offset) !== 0;
                value.size = 1;
                this.offset++;
                break;
                
            case TypeIdentifier.INT8:
                value.data = this.dataView.getInt8(this.offset);
                value.size = 1;
                this.offset++;
                break;
                
            case TypeIdentifier.UINT8:
                value.data = this.dataView.getUint8(this.offset);
                value.size = 1;
                this.offset++;
                break;
                
            case TypeIdentifier.INT16:
                value.data = this.dataView.getInt16(this.offset, true);
                value.size = 2;
                this.offset += 2;
                break;
                
            case TypeIdentifier.UINT16:
                value.data = this.dataView.getUint16(this.offset, true);
                value.size = 2;
                this.offset += 2;
                break;
                
            case TypeIdentifier.INT32:
                value.data = this.dataView.getInt32(this.offset, true);
                value.size = 4;
                this.offset += 4;
                break;
                
            case TypeIdentifier.UINT32:
                value.data = this.dataView.getUint32(this.offset, true);
                value.size = 4;
                this.offset += 4;
                break;
                
            case TypeIdentifier.INT64:
                value.data = Number(this.dataView.getBigInt64(this.offset, true));
                value.size = 8;
                this.offset += 8;
                break;
                
            case TypeIdentifier.UINT64:
                value.data = Number(this.dataView.getBigUint64(this.offset, true));
                value.size = 8;
                this.offset += 8;
                break;
                
            case TypeIdentifier.FLOAT32:
                value.data = this.dataView.getFloat32(this.offset, true);
                value.size = 4;
                this.offset += 4;
                break;
                
            case TypeIdentifier.FLOAT64:
                value.data = this.dataView.getFloat64(this.offset, true);
                value.size = 8;
                this.offset += 8;
                break;
                
            case TypeIdentifier.STRING:
                value.data = this.readString();
                value.size = new TextEncoder().encode(value.data).length;
                break;
                
            case TypeIdentifier.BYTES:
                value.data = this.readBytes();
                value.size = value.data.length;
                break;
                
            case TypeIdentifier.TIMESTAMP:
                const timestamp = Number(this.dataView.getBigUint64(this.offset, true));
                value.data = new Date(timestamp * 1000); // Convert to milliseconds
                value.size = 8;
                this.offset += 8;
                break;
                
            case TypeIdentifier.DURATION:
                const nanoseconds = Number(this.dataView.getBigUint64(this.offset, true));
                value.data = nanoseconds / 1e9; // Convert to seconds
                value.size = 8;
                this.offset += 8;
                break;
                
            case TypeIdentifier.REFERENCE:
                value.data = Number(this.dataView.getBigUint64(this.offset, true));
                value.size = 8;
                this.offset += 8;
                break;
                
            case TypeIdentifier.ARRAY:
                value.data = this.readArray();
                value.size = value.data.size;
                break;
                
            case TypeIdentifier.OBJECT:
                value.data = this.readObject();
                value.size = value.data.size;
                break;
                
            default:
                throw new BinaryFormatError(`unknown type identifier: ${typeId}`, this.offset, "unknown_type");
        }
        
        return value;
    }
    
    /**
     * Read a string with length prefix
     */
    readString() {
        const { length, bytesRead } = decodeLength(this.dataView, this.offset);
        this.offset += bytesRead;
        
        if (length > 0) {
            const stringBytes = this.buffer.slice(this.offset, this.offset + length);
            this.offset += length;
            return new TextDecoder().decode(stringBytes);
        }
        
        return "";
    }
    
    /**
     * Read a byte array with length prefix
     */
    readBytes() {
        const { length, bytesRead } = decodeLength(this.dataView, this.offset);
        this.offset += bytesRead;
        
        if (length > 0) {
            const bytes = this.buffer.slice(this.offset, this.offset + length);
            this.offset += length;
            return bytes;
        }
        
        return new Uint8Array(0);
    }
    
    /**
     * Read an array value
     */
    readArray() {
        const { length, bytesRead } = decodeLength(this.dataView, this.offset);
        this.offset += bytesRead;
        
        const array = new Array();
        array.offset = this.offset;
        
        // Read array elements
        for (let i = 0; i < length; i++) {
            const element = this.readValue();
            array.elements.push(element);
        }
        
        // Calculate total size
        array.size = this.offset - array.offset;
        return array;
    }
    
    /**
     * Read an object value
     */
    readObject() {
        const { length, bytesRead } = decodeLength(this.dataView, this.offset);
        this.offset += bytesRead;
        
        const object = new Object();
        object.offset = this.offset;
        
        // Read object entries
        for (let i = 0; i < length; i++) {
            // Read key
            const key = this.readString();
            
            // Read value
            const value = this.readValue();
            
            object.entries[key] = value;
        }
        
        // Calculate total size
        object.size = this.offset - object.offset;
        return object;
    }
    
    /**
     * Read and validate file footer
     */
    readFooter() {
        if (!this.dataView) {
            throw new BinaryFormatError("file not open", this.offset, "file_not_open");
        }
        
        const footer = new Footer();
        
        // Read magic bytes
        footer.magic = new Uint8Array(this.buffer.slice(this.offset, this.offset + 4));
        
        // Read file size and checksum
        footer.fileSize = this.dataView.getUint32(this.offset + 4, true);
        footer.checksum = this.dataView.getUint32(this.offset + 8, true);
        
        // Read reserved bytes
        footer.reserved = new Uint8Array(this.buffer.slice(this.offset + 12, this.offset + 16));
        
        // Validate magic bytes
        if (!footer.magic.every((byte, i) => byte === MAGIC_FOOTER[i])) {
            throw new BinaryFormatError(
                `invalid footer magic bytes: expected ${MAGIC_FOOTER}, got ${footer.magic}`,
                this.offset, "footer_magic"
            );
        }
        
        return footer;
    }
    
    /**
     * Close the reader
     */
    close() {
        this.buffer = null;
        this.dataView = null;
    }
}

/**
 * Binary writer for .pnt files
 */
class BinaryWriter {
    constructor(filename) {
        this.filename = filename;
        this.file = null;
        this.offset = 0;
        this.buffer = [];
        this.totalSize = 0;
    }
    
    /**
     * Open file for writing
     */
    async open() {
        if (typeof window !== 'undefined') {
            // Browser environment - use Blob API
            this.file = new Blob([], { type: 'application/octet-stream' });
        } else {
            // Node.js environment
            const fs = require('fs');
            this.file = fs.createWriteStream(this.filename);
        }
    }
    
    /**
     * Write data to buffer
     */
    write(data) {
        if (Array.isArray(data)) {
            this.buffer.push(...data);
        } else {
            this.buffer.push(data);
        }
        this.totalSize += data.length || 1;
    }
    
    /**
     * Write file header
     */
    writeHeader(header) {
        // Set magic bytes
        header.magic = new Uint8Array(MAGIC_HEADER);
        
        // Set version
        header.major = VERSION_MAJOR;
        header.minor = VERSION_MINOR;
        header.patch = VERSION_PATCH;
        
        // Set data offset
        header.dataOffset = 64;
        
        // Calculate checksum (we'll update this after writing)
        header.checksum = 0;
        
        // Write header
        const headerData = new Uint8Array(64);
        const view = new DataView(headerData.buffer);
        
        // Magic bytes
        headerData.set(header.magic, 0);
        
        // Version and flags
        view.setUint8(4, header.major);
        view.setUint8(5, header.minor);
        view.setUint8(6, header.patch);
        view.setUint8(7, header.flags);
        
        // Offsets and sizes
        view.setBigUint64(8, BigInt(header.dataOffset), true);
        view.setBigUint64(16, BigInt(header.dataSize), true);
        view.setBigUint64(24, BigInt(header.indexOffset), true);
        view.setBigUint64(32, BigInt(header.indexSize), true);
        
        // Checksum
        view.setUint32(40, header.checksum, true);
        
        // Reserved bytes
        headerData.set(header.reserved, 44);
        
        this.write(headerData);
        this.offset = 64;
    }
    
    /**
     * Write a single value
     */
    writeValue(value) {
        // Write type identifier
        this.write(new Uint8Array([value.typeId]));
        this.offset++;
        
        // Write value data based on type
        switch (value.typeId) {
            case TypeIdentifier.NULL:
                value.size = 0;
                break;
                
            case TypeIdentifier.BOOL:
                this.write(new Uint8Array([value.data ? 1 : 0]));
                value.size = 1;
                this.offset++;
                break;
                
            case TypeIdentifier.INT8:
                const int8Buffer = new ArrayBuffer(1);
                new DataView(int8Buffer).setInt8(0, value.data);
                this.write(new Uint8Array(int8Buffer));
                value.size = 1;
                this.offset++;
                break;
                
            case TypeIdentifier.UINT8:
                this.write(new Uint8Array([value.data]));
                value.size = 1;
                this.offset++;
                break;
                
            case TypeIdentifier.INT16:
                const int16Buffer = new ArrayBuffer(2);
                new DataView(int16Buffer).setInt16(0, value.data, true);
                this.write(new Uint8Array(int16Buffer));
                value.size = 2;
                this.offset += 2;
                break;
                
            case TypeIdentifier.UINT16:
                const uint16Buffer = new ArrayBuffer(2);
                new DataView(uint16Buffer).setUint16(0, value.data, true);
                this.write(new Uint8Array(uint16Buffer));
                value.size = 2;
                this.offset += 2;
                break;
                
            case TypeIdentifier.INT32:
                const int32Buffer = new ArrayBuffer(4);
                new DataView(int32Buffer).setInt32(0, value.data, true);
                this.write(new Uint8Array(int32Buffer));
                value.size = 4;
                this.offset += 4;
                break;
                
            case TypeIdentifier.UINT32:
                const uint32Buffer = new ArrayBuffer(4);
                new DataView(uint32Buffer).setUint32(0, value.data, true);
                this.write(new Uint8Array(uint32Buffer));
                value.size = 4;
                this.offset += 4;
                break;
                
            case TypeIdentifier.INT64:
                const int64Buffer = new ArrayBuffer(8);
                new DataView(int64Buffer).setBigInt64(0, BigInt(value.data), true);
                this.write(new Uint8Array(int64Buffer));
                value.size = 8;
                this.offset += 8;
                break;
                
            case TypeIdentifier.UINT64:
                const uint64Buffer = new ArrayBuffer(8);
                new DataView(uint64Buffer).setBigUint64(0, BigInt(value.data), true);
                this.write(new Uint8Array(uint64Buffer));
                value.size = 8;
                this.offset += 8;
                break;
                
            case TypeIdentifier.FLOAT32:
                const float32Buffer = new ArrayBuffer(4);
                new DataView(float32Buffer).setFloat32(0, value.data, true);
                this.write(new Uint8Array(float32Buffer));
                value.size = 4;
                this.offset += 4;
                break;
                
            case TypeIdentifier.FLOAT64:
                const float64Buffer = new ArrayBuffer(8);
                new DataView(float64Buffer).setFloat64(0, value.data, true);
                this.write(new Uint8Array(float64Buffer));
                value.size = 8;
                this.offset += 8;
                break;
                
            case TypeIdentifier.STRING:
                this.writeString(value.data);
                value.size = new TextEncoder().encode(value.data).length;
                break;
                
            case TypeIdentifier.BYTES:
                this.writeBytes(value.data);
                value.size = value.data.length;
                break;
                
            case TypeIdentifier.TIMESTAMP:
                const timestamp = Math.floor(value.data.getTime() / 1000); // Convert to seconds
                const timestampBuffer = new ArrayBuffer(8);
                new DataView(timestampBuffer).setBigUint64(0, BigInt(timestamp), true);
                this.write(new Uint8Array(timestampBuffer));
                value.size = 8;
                this.offset += 8;
                break;
                
            case TypeIdentifier.DURATION:
                const nanoseconds = Math.floor(value.data * 1e9); // Convert to nanoseconds
                const durationBuffer = new ArrayBuffer(8);
                new DataView(durationBuffer).setBigUint64(0, BigInt(nanoseconds), true);
                this.write(new Uint8Array(durationBuffer));
                value.size = 8;
                this.offset += 8;
                break;
                
            case TypeIdentifier.REFERENCE:
                const refBuffer = new ArrayBuffer(8);
                new DataView(refBuffer).setBigUint64(0, BigInt(value.data), true);
                this.write(new Uint8Array(refBuffer));
                value.size = 8;
                this.offset += 8;
                break;
                
            case TypeIdentifier.ARRAY:
                this.writeArray(value.data);
                value.size = value.data.size;
                break;
                
            case TypeIdentifier.OBJECT:
                this.writeObject(value.data);
                value.size = value.data.size;
                break;
                
            default:
                throw new BinaryFormatError(`unknown type identifier: ${value.typeId}`, this.offset, "unknown_type");
        }
    }
    
    /**
     * Write a string with length prefix
     */
    writeString(stringData) {
        const stringBytes = new TextEncoder().encode(stringData);
        const lengthBytes = encodeLength(stringBytes.length);
        
        this.write(lengthBytes);
        this.offset += lengthBytes.length;
        
        if (stringBytes.length > 0) {
            this.write(stringBytes);
            this.offset += stringBytes.length;
        }
    }
    
    /**
     * Write a byte array with length prefix
     */
    writeBytes(bytesData) {
        const lengthBytes = encodeLength(bytesData.length);
        
        this.write(lengthBytes);
        this.offset += lengthBytes.length;
        
        if (bytesData.length > 0) {
            this.write(bytesData);
            this.offset += bytesData.length;
        }
    }
    
    /**
     * Write an array value
     */
    writeArray(array) {
        const lengthBytes = encodeLength(array.elements.length);
        
        this.write(lengthBytes);
        this.offset += lengthBytes.length;
        
        array.offset = this.offset;
        
        // Write array elements
        for (const element of array.elements) {
            this.writeValue(element);
        }
        
        // Calculate total size
        array.size = this.offset - array.offset;
    }
    
    /**
     * Write an object value
     */
    writeObject(object) {
        const lengthBytes = encodeLength(Object.keys(object.entries).length);
        
        this.write(lengthBytes);
        this.offset += lengthBytes.length;
        
        object.offset = this.offset;
        
        // Write object entries
        for (const [key, value] of Object.entries(object.entries)) {
            // Write key
            this.writeString(key);
            
            // Write value
            this.writeValue(value);
        }
        
        // Calculate total size
        object.size = this.offset - object.offset;
    }
    
    /**
     * Write file footer
     */
    writeFooter(footer) {
        // Set magic bytes
        footer.magic = new Uint8Array(MAGIC_FOOTER);
        
        // Set file size
        footer.fileSize = this.offset + 16; // +16 for footer itself
        
        // Calculate checksum of entire file
        const fileData = new Uint8Array(this.buffer);
        footer.checksum = calculateCRC32(fileData);
        
        // Write footer
        const footerData = new Uint8Array(16);
        const view = new DataView(footerData.buffer);
        
        // Magic bytes
        footerData.set(footer.magic, 0);
        
        // File size and checksum
        view.setUint32(4, footer.fileSize, true);
        view.setUint32(8, footer.checksum, true);
        
        // Reserved bytes
        footerData.set(footer.reserved, 12);
        
        this.write(footerData);
        this.offset += 16;
    }
    
    /**
     * Flush data to file
     */
    async flush() {
        if (typeof window !== 'undefined') {
            // Browser environment - create download link
            const blob = new Blob([new Uint8Array(this.buffer)], { type: 'application/octet-stream' });
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = this.filename;
            a.click();
            URL.revokeObjectURL(url);
        } else {
            // Node.js environment
            const fs = require('fs');
            fs.writeFileSync(this.filename, Buffer.from(this.buffer));
        }
    }
    
    /**
     * Close the writer
     */
    close() {
        this.buffer = null;
    }
}

// Convenience functions for easy usage
async function readPntFile(filename) {
    const reader = new BinaryReader(filename);
    await reader.open();
    
    try {
        const header = reader.readHeader();
        const object = reader.readObject();
        const footer = reader.readFooter();
        
        // Convert to dictionary
        const result = {};
        for (const [key, value] of Object.entries(object.entries)) {
            result[key] = value.data;
        }
        
        return result;
    } finally {
        reader.close();
    }
}

async function writePntFile(filename, config) {
    const writer = new BinaryWriter(filename);
    await writer.open();
    
    try {
        // Convert dictionary to object
        const entries = {};
        for (const [key, value] of Object.entries(config)) {
            let typeId;
            
            if (value === null || value === undefined) {
                typeId = TypeIdentifier.NULL;
            } else if (typeof value === 'boolean') {
                typeId = TypeIdentifier.BOOL;
            } else if (typeof value === 'number') {
                if (Number.isInteger(value)) {
                    if (value >= -128 && value <= 127) {
                        typeId = TypeIdentifier.INT8;
                    } else if (value >= 0 && value <= 255) {
                        typeId = TypeIdentifier.UINT8;
                    } else if (value >= -32768 && value <= 32767) {
                        typeId = TypeIdentifier.INT16;
                    } else if (value >= 0 && value <= 65535) {
                        typeId = TypeIdentifier.UINT16;
                    } else if (value >= -2147483648 && value <= 2147483647) {
                        typeId = TypeIdentifier.INT32;
                    } else if (value >= 0 && value <= 4294967295) {
                        typeId = TypeIdentifier.UINT32;
                    } else {
                        typeId = TypeIdentifier.INT64;
                    }
                } else {
                    typeId = TypeIdentifier.FLOAT64;
                }
            } else if (typeof value === 'string') {
                typeId = TypeIdentifier.STRING;
            } else if (value instanceof Uint8Array) {
                typeId = TypeIdentifier.BYTES;
            } else {
                throw new Error(`unsupported type for key '${key}': ${typeof value}`);
            }
            
            entries[key] = new Value(typeId, value);
        }
        
        const object = new Object(entries);
        
        // Write file
        const header = new Header();
        writer.writeHeader(header);
        writer.writeObject(object);
        const footer = new Footer();
        writer.writeFooter(footer);
        
        await writer.flush();
    } finally {
        writer.close();
    }
}

// Export for different module systems
if (typeof module !== 'undefined' && module.exports) {
    // Node.js
    module.exports = {
        TypeIdentifier,
        FeatureFlags,
        BinaryFormatError,
        Header,
        Footer,
        IndexEntry,
        Value,
        Object,
        Array,
        BinaryReader,
        BinaryWriter,
        readPntFile,
        writePntFile
    };
} else if (typeof window !== 'undefined') {
    // Browser
    window.TuskLangBinary = {
        TypeIdentifier,
        FeatureFlags,
        BinaryFormatError,
        Header,
        Footer,
        IndexEntry,
        Value,
        Object,
        Array,
        BinaryReader,
        BinaryWriter,
        readPntFile,
        writePntFile
    };
} 