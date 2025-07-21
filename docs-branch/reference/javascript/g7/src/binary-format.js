/**
 * Advanced Binary Format Support and Serialization
 * Goal 7.1 Implementation
 */

const crypto = require('crypto');
const zlib = require('zlib');
const { promisify } = require('util');

const gzip = promisify(zlib.gzip);
const gunzip = promisify(zlib.gunzip);

class BinaryFormatManager {
    constructor(options = {}) {
        this.version = options.version || 1;
        this.compressionLevel = options.compressionLevel || 6;
        this.encryptionKey = options.encryptionKey || null;
        this.magicHeader = Buffer.from('TUSK', 'utf8');
        this.maxSize = options.maxSize || 100 * 1024 * 1024; // 100MB
    }

    /**
     * Serialize TuskLang configuration to binary format
     */
    async serialize(config, options = {}) {
        try {
            const startTime = Date.now();
            
            // Validate input
            if (!config || typeof config !== 'object') {
                throw new Error('Invalid configuration object');
            }

            // Create metadata
            const metadata = {
                version: this.version,
                timestamp: Date.now(),
                checksum: null,
                compressed: options.compress !== false,
                encrypted: options.encrypt === true && this.encryptionKey,
                size: 0
            };

            // Convert to JSON string
            const jsonString = JSON.stringify(config, null, 0);
            
            // Compress if enabled
            let data = Buffer.from(jsonString, 'utf8');
            if (metadata.compressed) {
                data = await gzip(data, { level: this.compressionLevel });
            }

            // Encrypt if enabled
            if (metadata.encrypted) {
                data = this.encryptData(data);
            }

            // Calculate checksum
            metadata.checksum = crypto.createHash('sha256').update(data).digest('hex');
            metadata.size = data.length;

            // Validate size
            if (metadata.size > this.maxSize) {
                throw new Error(`Serialized data exceeds maximum size: ${metadata.size} > ${this.maxSize}`);
            }

            // Create binary structure
            const binaryData = this.createBinaryStructure(metadata, data);
            
            const executionTime = Date.now() - startTime;
            console.log(`✓ Binary serialization completed in ${executionTime}ms (${metadata.size} bytes)`);
            
            return binaryData;
        } catch (error) {
            throw new Error(`Binary serialization failed: ${error.message}`);
        }
    }

    /**
     * Deserialize binary format to TuskLang configuration
     */
    async deserialize(binaryData, options = {}) {
        try {
            const startTime = Date.now();
            
            // Validate input
            if (!Buffer.isBuffer(binaryData)) {
                throw new Error('Invalid binary data');
            }

            // Parse binary structure
            const { metadata, data } = this.parseBinaryStructure(binaryData);
            
            // Validate checksum
            const calculatedChecksum = crypto.createHash('sha256').update(data).digest('hex');
            if (calculatedChecksum !== metadata.checksum) {
                throw new Error('Checksum validation failed - data corruption detected');
            }

            // Decrypt if encrypted
            let decryptedData = data;
            if (metadata.encrypted) {
                decryptedData = this.decryptData(data);
            }

            // Decompress if compressed
            let jsonString;
            if (metadata.compressed) {
                const decompressed = await gunzip(decryptedData);
                jsonString = decompressed.toString('utf8');
            } else {
                jsonString = decryptedData.toString('utf8');
            }

            // Parse JSON
            const config = JSON.parse(jsonString);
            
            const executionTime = Date.now() - startTime;
            console.log(`✓ Binary deserialization completed in ${executionTime}ms`);
            
            return {
                config,
                metadata,
                executionTime
            };
        } catch (error) {
            throw new Error(`Binary deserialization failed: ${error.message}`);
        }
    }

    /**
     * Create binary structure with metadata and data
     */
    createBinaryStructure(metadata, data) {
        const metadataBuffer = Buffer.from(JSON.stringify(metadata), 'utf8');
        const metadataLength = Buffer.alloc(4);
        metadataLength.writeUInt32BE(metadataBuffer.length, 0);
        
        const dataLength = Buffer.alloc(8);
        dataLength.writeBigUInt64BE(BigInt(data.length), 0);
        
        return Buffer.concat([
            this.magicHeader,
            metadataLength,
            metadataBuffer,
            dataLength,
            data
        ]);
    }

    /**
     * Parse binary structure to extract metadata and data
     */
    parseBinaryStructure(binaryData) {
        let offset = 0;
        
        // Check magic header
        const magic = binaryData.slice(offset, offset + 4);
        if (!magic.equals(this.magicHeader)) {
            throw new Error('Invalid binary format - magic header mismatch');
        }
        offset += 4;
        
        // Read metadata length
        const metadataLength = binaryData.readUInt32BE(offset);
        offset += 4;
        
        // Read metadata
        const metadataBuffer = binaryData.slice(offset, offset + metadataLength);
        const metadata = JSON.parse(metadataBuffer.toString('utf8'));
        offset += metadataLength;
        
        // Read data length
        const dataLength = binaryData.readBigUInt64BE(offset);
        offset += 8;
        
        // Read data
        const data = binaryData.slice(offset, offset + Number(dataLength));
        
        return { metadata, data };
    }

    /**
     * Encrypt data using AES-256-GCM
     */
    encryptData(data) {
        if (!this.encryptionKey) {
            throw new Error('Encryption key not provided');
        }
        
        const iv = crypto.randomBytes(16);
        const cipher = crypto.createCipher('aes-256-gcm', this.encryptionKey);
        cipher.setAAD(Buffer.from('TuskLang', 'utf8'));
        
        const encrypted = Buffer.concat([
            cipher.update(data),
            cipher.final()
        ]);
        
        const authTag = cipher.getAuthTag();
        
        return Buffer.concat([iv, authTag, encrypted]);
    }

    /**
     * Decrypt data using AES-256-GCM
     */
    decryptData(encryptedData) {
        if (!this.encryptionKey) {
            throw new Error('Encryption key not provided');
        }
        
        const iv = encryptedData.slice(0, 16);
        const authTag = encryptedData.slice(16, 32);
        const encrypted = encryptedData.slice(32);
        
        const decipher = crypto.createDecipher('aes-256-gcm', this.encryptionKey);
        decipher.setAAD(Buffer.from('TuskLang', 'utf8'));
        decipher.setAuthTag(authTag);
        
        const decrypted = Buffer.concat([
            decipher.update(encrypted),
            decipher.final()
        ]);
        
        return decrypted;
    }

    /**
     * Get format statistics
     */
    getStats() {
        return {
            version: this.version,
            compressionLevel: this.compressionLevel,
            encryptionEnabled: !!this.encryptionKey,
            maxSize: this.maxSize,
            magicHeader: this.magicHeader.toString('hex')
        };
    }
}

module.exports = { BinaryFormatManager }; 