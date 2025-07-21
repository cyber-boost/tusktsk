/**
 * Advanced Cryptography and Security Framework
 * Goal 10.1 Implementation
 */

const crypto = require('crypto');
const EventEmitter = require('events');

class CryptographyFramework extends EventEmitter {
    constructor(options = {}) {
        super();
        this.algorithms = new Map();
        this.keys = new Map();
        this.keyStore = new Map();
        this.certificates = new Map();
        this.encryptionCache = new Map();
        this.defaultAlgorithm = options.defaultAlgorithm || 'aes-256-gcm';
        this.keyRotationInterval = options.keyRotationInterval || 86400000; // 24 hours
        this.cacheTimeout = options.cacheTimeout || 300000; // 5 minutes
        
        this.registerBuiltInAlgorithms();
        this.startKeyRotation();
    }

    /**
     * Register a cryptographic algorithm
     */
    registerAlgorithm(name, algorithm) {
        if (typeof algorithm.encrypt !== 'function' || typeof algorithm.decrypt !== 'function') {
            throw new Error('Algorithm must have encrypt and decrypt methods');
        }

        this.algorithms.set(name, {
            ...algorithm,
            registeredAt: Date.now(),
            usageCount: 0
        });

        console.log(`✓ Algorithm registered: ${name}`);
        this.emit('algorithmRegistered', { name });
        
        return true;
    }

    /**
     * Generate cryptographic key
     */
    generateKey(algorithm = this.defaultAlgorithm, options = {}) {
        const keyId = this.generateKeyId();
        const keySize = options.keySize || this.getDefaultKeySize(algorithm);
        
        try {
            const key = crypto.randomBytes(keySize);
            const keyData = {
                id: keyId,
                algorithm,
                key: key,
                createdAt: Date.now(),
                expiresAt: options.expiresAt || (Date.now() + this.keyRotationInterval),
                metadata: options.metadata || {},
                usageCount: 0
            };

            this.keys.set(keyId, keyData);
            console.log(`✓ Key generated: ${keyId} (${algorithm})`);
            this.emit('keyGenerated', { keyId, algorithm });
            
            return keyId;
        } catch (error) {
            throw new Error(`Failed to generate key: ${error.message}`);
        }
    }

    /**
     * Encrypt data
     */
    async encrypt(data, keyId, options = {}) {
        const keyData = this.keys.get(keyId);
        if (!keyData) {
            throw new Error(`Key ${keyId} not found`);
        }

        const algorithm = this.algorithms.get(keyData.algorithm);
        if (!algorithm) {
            throw new Error(`Algorithm ${keyData.algorithm} not found`);
        }

        try {
            // Check cache first
            const cacheKey = this.generateCacheKey(data, keyId, options);
            if (this.encryptionCache.has(cacheKey)) {
                const cached = this.encryptionCache.get(cacheKey);
                if (Date.now() - cached.timestamp < this.cacheTimeout) {
                    return cached.result;
                }
            }

            // Perform encryption
            const result = await algorithm.encrypt(data, keyData.key, options);
            
            // Update usage count
            keyData.usageCount++;
            algorithm.usageCount++;

            // Cache result
            this.encryptionCache.set(cacheKey, {
                result,
                timestamp: Date.now()
            });

            this.emit('dataEncrypted', { keyId, dataSize: data.length, algorithm: keyData.algorithm });
            return result;
        } catch (error) {
            throw new Error(`Encryption failed: ${error.message}`);
        }
    }

    /**
     * Decrypt data
     */
    async decrypt(encryptedData, keyId, options = {}) {
        const keyData = this.keys.get(keyId);
        if (!keyData) {
            throw new Error(`Key ${keyId} not found`);
        }

        const algorithm = this.algorithms.get(keyData.algorithm);
        if (!algorithm) {
            throw new Error(`Algorithm ${keyData.algorithm} not found`);
        }

        try {
            const result = await algorithm.decrypt(encryptedData, keyData.key, options);
            
            // Update usage count
            keyData.usageCount++;
            algorithm.usageCount++;

            this.emit('dataDecrypted', { keyId, dataSize: result.length, algorithm: keyData.algorithm });
            return result;
        } catch (error) {
            throw new Error(`Decryption failed: ${error.message}`);
        }
    }

    /**
     * Create digital signature
     */
    async sign(data, keyId, options = {}) {
        const keyData = this.keys.get(keyId);
        if (!keyData) {
            throw new Error(`Key ${keyId} not found`);
        }

        try {
            const signature = crypto.createSign('SHA256')
                .update(data)
                .sign(keyData.key, 'base64');

            const signatureData = {
                signature,
                algorithm: 'SHA256',
                timestamp: Date.now(),
                keyId,
                dataHash: crypto.createHash('sha256').update(data).digest('hex')
            };

            this.emit('dataSigned', { keyId, dataSize: data.length });
            return signatureData;
        } catch (error) {
            throw new Error(`Signing failed: ${error.message}`);
        }
    }

    /**
     * Verify digital signature
     */
    async verify(data, signatureData, keyId) {
        const keyData = this.keys.get(keyId);
        if (!keyData) {
            throw new Error(`Key ${keyId} not found`);
        }

        try {
            const verifier = crypto.createVerify('SHA256')
                .update(data)
                .verify(keyData.key, signatureData.signature, 'base64');

            this.emit('signatureVerified', { keyId, verified: verifier });
            return verifier;
        } catch (error) {
            throw new Error(`Signature verification failed: ${error.message}`);
        }
    }

    /**
     * Generate certificate
     */
    generateCertificate(subject, keyId, options = {}) {
        const keyData = this.keys.get(keyId);
        if (!keyData) {
            throw new Error(`Key ${keyId} not found`);
        }

        try {
            const certId = this.generateCertificateId();
            const certificate = {
                id: certId,
                subject,
                keyId,
                algorithm: keyData.algorithm,
                issuedAt: Date.now(),
                expiresAt: options.expiresAt || (Date.now() + 365 * 24 * 60 * 60 * 1000), // 1 year
                issuer: options.issuer || 'self-signed',
                metadata: options.metadata || {}
            };

            this.certificates.set(certId, certificate);
            console.log(`✓ Certificate generated: ${certId}`);
            this.emit('certificateGenerated', { certId, subject });
            
            return certId;
        } catch (error) {
            throw new Error(`Certificate generation failed: ${error.message}`);
        }
    }

    /**
     * Store key securely
     */
    storeKey(keyId, storeName, options = {}) {
        const keyData = this.keys.get(keyId);
        if (!keyData) {
            throw new Error(`Key ${keyId} not found`);
        }

        if (!this.keyStore.has(storeName)) {
            this.keyStore.set(storeName, new Map());
        }

        const store = this.keyStore.get(storeName);
        store.set(keyId, {
            ...keyData,
            storedAt: Date.now(),
            storeName,
            metadata: { ...keyData.metadata, ...options.metadata }
        });

        console.log(`✓ Key ${keyId} stored in ${storeName}`);
        this.emit('keyStored', { keyId, storeName });
        
        return true;
    }

    /**
     * Retrieve key from store
     */
    retrieveKey(keyId, storeName) {
        const store = this.keyStore.get(storeName);
        if (!store) {
            throw new Error(`Key store ${storeName} not found`);
        }

        const keyData = store.get(keyId);
        if (!keyData) {
            throw new Error(`Key ${keyId} not found in store ${storeName}`);
        }

        console.log(`✓ Key ${keyId} retrieved from ${storeName}`);
        this.emit('keyRetrieved', { keyId, storeName });
        
        return keyData;
    }

    /**
     * Register built-in algorithms
     */
    registerBuiltInAlgorithms() {
        // AES-256-GCM
        this.registerAlgorithm('aes-256-gcm', {
            encrypt: async (data, key, options = {}) => {
                const iv = crypto.randomBytes(12);
                const cipher = crypto.createCipheriv('aes-256-gcm', key, iv);
                
                let encrypted = cipher.update(data);
                encrypted = Buffer.concat([encrypted, cipher.final()]);
                
                const authTag = cipher.getAuthTag();
                
                return {
                    encrypted: encrypted.toString('hex'),
                    iv: iv.toString('hex'),
                    authTag: authTag.toString('hex'),
                    algorithm: 'aes-256-gcm'
                };
            },
            decrypt: async (encryptedData, key, options = {}) => {
                const decipher = crypto.createDecipheriv('aes-256-gcm', key, Buffer.from(encryptedData.iv, 'hex'));
                decipher.setAuthTag(Buffer.from(encryptedData.authTag, 'hex'));
                
                let decrypted = decipher.update(Buffer.from(encryptedData.encrypted, 'hex'));
                decrypted = Buffer.concat([decrypted, decipher.final()]);
                
                return decrypted.toString();
            }
        });
        
        // ChaCha20-Poly1305
        this.registerAlgorithm('chacha20-poly1305', {
            encrypt: async (data, key, options = {}) => {
                const nonce = crypto.randomBytes(12);
                const cipher = crypto.createCipheriv('chacha20-poly1305', key, nonce);
                
                let encrypted = cipher.update(data);
                encrypted = Buffer.concat([encrypted, cipher.final()]);
                
                const authTag = cipher.getAuthTag();
                
                return {
                    encrypted: encrypted.toString('hex'),
                    nonce: nonce.toString('hex'),
                    authTag: authTag.toString('hex'),
                    algorithm: 'chacha20-poly1305'
                };
            },
            decrypt: async (encryptedData, key, options = {}) => {
                const decipher = crypto.createDecipheriv('chacha20-poly1305', key, Buffer.from(encryptedData.nonce, 'hex'));
                decipher.setAuthTag(Buffer.from(encryptedData.authTag, 'hex'));
                
                let decrypted = decipher.update(Buffer.from(encryptedData.encrypted, 'hex'));
                decrypted = Buffer.concat([decrypted, decipher.final()]);
                
                return decrypted.toString();
            }
        });
        
        // Simple for testing
        this.registerAlgorithm('simple', {
            encrypt: async (data, key, options = {}) => {
                return { encrypted: data, algorithm: 'simple' };
            },
            decrypt: async (encryptedData, key, options = {}) => {
                return encryptedData.encrypted;
            }
        });
    }

    /**
     * Get default key size for algorithm
     */
    getDefaultKeySize(algorithm) {
        const sizes = {
            'aes-256-gcm': 32,
            'chacha20-poly1305': 32,
            'rsa': 2048
        };
        return sizes[algorithm] || 32;
    }

    /**
     * Generate unique key ID
     */
    generateKeyId() {
        return `key_${Date.now()}_${crypto.randomBytes(8).toString('hex')}`;
    }

    /**
     * Generate unique certificate ID
     */
    generateCertificateId() {
        return `cert_${Date.now()}_${crypto.randomBytes(8).toString('hex')}`;
    }

    /**
     * Generate cache key
     */
    generateCacheKey(data, keyId, options) {
        const dataHash = crypto.createHash('sha256').update(data).digest('hex');
        return `${keyId}_${dataHash}_${JSON.stringify(options)}`;
    }

    /**
     * Start key rotation
     */
    startKeyRotation() {
        setInterval(() => {
            this.rotateKeys();
        }, this.keyRotationInterval);
    }

    /**
     * Rotate expired keys
     */
    rotateKeys() {
        const now = Date.now();
        let rotatedCount = 0;

        for (const [keyId, keyData] of this.keys) {
            if (keyData.expiresAt && now > keyData.expiresAt) {
                // Generate new key
                const newKeyId = this.generateKey(keyData.algorithm, {
                    keySize: keyData.key.length,
                    metadata: keyData.metadata
                });

                // Update references
                for (const [storeName, store] of this.keyStore) {
                    if (store.has(keyId)) {
                        const storedKey = store.get(keyId);
                        store.delete(keyId);
                        store.set(newKeyId, { ...storedKey, id: newKeyId });
                    }
                }

                // Remove old key
                this.keys.delete(keyId);
                rotatedCount++;
            }
        }

        if (rotatedCount > 0) {
            console.log(`✓ Rotated ${rotatedCount} keys`);
            this.emit('keysRotated', { count: rotatedCount });
        }
    }

    /**
     * Get cryptography statistics
     */
    getStats() {
        return {
            algorithms: this.algorithms.size,
            keys: this.keys.size,
            certificates: this.certificates.size,
            keyStores: this.keyStore.size,
            cacheSize: this.encryptionCache.size,
            defaultAlgorithm: this.defaultAlgorithm
        };
    }

    /**
     * Clear encryption cache
     */
    clearCache() {
        this.encryptionCache.clear();
        console.log('✓ Encryption cache cleared');
    }

    /**
     * Export key (for backup/transfer)
     */
    exportKey(keyId, format = 'base64') {
        const keyData = this.keys.get(keyId);
        if (!keyData) {
            throw new Error(`Key ${keyId} not found`);
        }

        const exportData = {
            id: keyData.id,
            algorithm: keyData.algorithm,
            key: keyData.key.toString(format),
            createdAt: keyData.createdAt,
            metadata: keyData.metadata
        };

        return exportData;
    }

    /**
     * Import key
     */
    importKey(exportData, format = 'base64') {
        const keyData = {
            id: exportData.id,
            algorithm: exportData.algorithm,
            key: Buffer.from(exportData.key, format),
            createdAt: exportData.createdAt,
            expiresAt: Date.now() + this.keyRotationInterval,
            metadata: exportData.metadata,
            usageCount: 0
        };

        this.keys.set(keyData.id, keyData);
        console.log(`✓ Key imported: ${keyData.id}`);
        this.emit('keyImported', { keyId: keyData.id });
        
        return keyData.id;
    }
}

module.exports = { CryptographyFramework }; 