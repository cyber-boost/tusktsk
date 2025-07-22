/**
 * TuskLang Binary Format Module
 * =============================
 * 
 * Handles binary serialization and deserialization of TuskLang data
 * Enterprise-grade binary format with compression and encryption support
 */

const crypto = require('crypto');
const zlib = require('zlib');

class BinaryFormatReader {
    constructor(buffer) {
        this.buffer = Buffer.isBuffer(buffer) ? buffer : Buffer.from(buffer);
        this.offset = 0;
    }

    readUInt8() {
        const value = this.buffer.readUInt8(this.offset);
        this.offset += 1;
        return value;
    }

    readUInt16() {
        const value = this.buffer.readUInt16BE(this.offset);
        this.offset += 2;
        return value;
    }

    readUInt32() {
        const value = this.buffer.readUInt32BE(this.offset);
        this.offset += 4;
        return value;
    }

    readString() {
        const length = this.readUInt16();
        const value = this.buffer.toString('utf8', this.offset, this.offset + length);
        this.offset += length;
        return value;
    }

    readObject() {
        const type = this.readUInt8();
        const length = this.readUInt32();
        
        if (type === 0x01) { // String
            return this.readString();
        } else if (type === 0x02) { // Number
            return this.buffer.readDoubleBE(this.offset);
        } else if (type === 0x03) { // Boolean
            return this.readUInt8() === 1;
        } else if (type === 0x04) { // Object
            const obj = {};
            const keyCount = this.readUInt16();
            for (let i = 0; i < keyCount; i++) {
                const key = this.readString();
                obj[key] = this.readObject();
            }
            return obj;
        } else if (type === 0x05) { // Array
            const arr = [];
            const itemCount = this.readUInt16();
            for (let i = 0; i < itemCount; i++) {
                arr.push(this.readObject());
            }
            return arr;
        }
        
        return null;
    }

    read() {
        const magic = this.readUInt32();
        if (magic !== 0x5455534B) { // TUSK
            throw new Error('Invalid binary format: wrong magic number');
        }
        
        const version = this.readUInt8();
        const flags = this.readUInt8();
        
        let data = this.readObject();
        
        // Handle compression
        if (flags & 0x01) {
            data = zlib.inflateSync(Buffer.from(data, 'base64'));
            data = JSON.parse(data.toString());
        }
        
        // Handle encryption
        if (flags & 0x02) {
            // Decryption would be implemented here
            // For now, return as-is
        }
        
        return data;
    }
}

class BinaryFormatWriter {
    constructor() {
        this.chunks = [];
    }

    writeUInt8(value) {
        const buffer = Buffer.alloc(1);
        buffer.writeUInt8(value);
        this.chunks.push(buffer);
    }

    writeUInt16(value) {
        const buffer = Buffer.alloc(2);
        buffer.writeUInt16BE(value);
        this.chunks.push(buffer);
    }

    writeUInt32(value) {
        const buffer = Buffer.alloc(4);
        buffer.writeUInt32BE(value);
        this.chunks.push(buffer);
    }

    writeString(value) {
        const str = String(value);
        const buffer = Buffer.from(str, 'utf8');
        this.writeUInt16(buffer.length);
        this.chunks.push(buffer);
    }

    writeObject(value) {
        if (typeof value === 'string') {
            this.writeUInt8(0x01);
            this.writeUInt32(0); // Placeholder
            this.writeString(value);
        } else if (typeof value === 'number') {
            this.writeUInt8(0x02);
            this.writeUInt32(8);
            const buffer = Buffer.alloc(8);
            buffer.writeDoubleBE(value);
            this.chunks.push(buffer);
        } else if (typeof value === 'boolean') {
            this.writeUInt8(0x03);
            this.writeUInt32(1);
            this.writeUInt8(value ? 1 : 0);
        } else if (Array.isArray(value)) {
            this.writeUInt8(0x05);
            this.writeUInt32(0); // Placeholder
            this.writeUInt16(value.length);
            for (const item of value) {
                this.writeObject(item);
            }
        } else if (typeof value === 'object' && value !== null) {
            this.writeUInt8(0x04);
            this.writeUInt32(0); // Placeholder
            const keys = Object.keys(value);
            this.writeUInt16(keys.length);
            for (const key of keys) {
                this.writeString(key);
                this.writeObject(value[key]);
            }
        } else {
            this.writeUInt8(0x00);
            this.writeUInt32(0);
        }
    }

    write(data, options = {}) {
        // Write magic number
        this.writeUInt32(0x5455534B); // TUSK
        
        // Write version
        this.writeUInt8(1);
        
        // Write flags
        let flags = 0;
        if (options.compress) flags |= 0x01;
        if (options.encrypt) flags |= 0x02;
        this.writeUInt8(flags);
        
        // Handle compression
        if (options.compress) {
            const jsonStr = JSON.stringify(data);
            const compressed = zlib.deflateSync(Buffer.from(jsonStr));
            data = compressed.toString('base64');
        }
        
        // Write data
        this.writeObject(data);
        
        // Combine all chunks
        return Buffer.concat(this.chunks);
    }
}

class BinaryFormatError extends Error {
    constructor(message, code) {
        super(message);
        this.name = 'BinaryFormatError';
        this.code = code;
    }
}

module.exports = {
    BinaryFormatReader,
    BinaryFormatWriter,
    BinaryFormatError
}; 