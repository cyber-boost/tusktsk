/**
 * Advanced Cryptography and Security Framework
 * Goal 10.1 Implementation
 */

const crypto = require("crypto");
const EventEmitter = require("events");

class CryptographyFramework extends EventEmitter {
    constructor(options = {}) {
        super();
        this.algorithms = new Map();
        this.keys = new Map();
        this.keyStore = new Map();
        this.certificates = new Map();
        this.encryptionCache = new Map();
        this.defaultAlgorithm = options.defaultAlgorithm || "aes-256-gcm";
        this.keyRotationInterval = options.keyRotationInterval || 86400000; // 24 hours
        this.cacheTimeout = options.cacheTimeout || 300000; // 5 minutes
        
        this.registerBuiltInAlgorithms();
        this.startKeyRotation();
    }

    /**
     * Register a cryptographic algorithm
     */
    registerAlgorithm(name, algorithm) {
        if (typeof algorithm.encrypt !== "function" || typeof algorithm.decrypt !== "function") {
            throw new Error("Algorithm must have encrypt and decrypt methods");
        }

        this.algorithms.set(name, {
            ...algorithm,
            registeredAt: Date.now(),
            usageCount: 0
        });

        console.log(`✓ Algorithm registered: ${name}`);
        this.emit("algorithmRegistered", { name });
        
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
            this.emit("keyGenerated", { keyId, algorithm });
            
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
            const result = await algorithm.encrypt(data, keyData.key, options);
            
            // Update usage count
            keyData.usageCount++;
            algorithm.usageCount++;

            this.emit("dataEncrypted", { keyId, dataSize: data.length, algorithm: keyData.algorithm });
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

            this.emit("dataDecrypted", { keyId, dataSize: result.length, algorithm: keyData.algorithm });
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
            const signature = crypto.createSign("SHA256")
                .update(data)
                .sign(keyData.key, "base64");

            const signatureData = {
                signature,
                algorithm: "SHA256",
                timestamp: Date.now(),
                keyId,
                dataHash: crypto.createHash("sha256").update(data).digest("hex")
            };

            this.emit("dataSigned", { keyId, dataSize: data.length });
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
            const verifier = crypto.createVerify("SHA256")
                .update(data)
                .verify(keyData.key, signatureData.signature, "base64");

            this.emit("signatureVerified", { keyId, verified: verifier });
            return verifier;
        } catch (error) {
            throw new Error(`Signature verification failed: ${error.message}`);
        }
    }

    /**
     * Register built-in algorithms
     */
    registerBuiltInAlgorithms() {
        // AES-256-GCM
        this.registerAlgorithm("aes-256-gcm", {
            encrypt: async (data, key, options = {}) => {
                const iv = crypto.randomBytes(16);
                const cipher = crypto.createCipherGCM("aes-256-gcm", key, iv);
                
                let encrypted = cipher.update(data, "utf8", "hex");
                encrypted += cipher.final("hex");
                
                const authTag = cipher.getAuthTag();
                
                return {
                    encrypted,
                    iv: iv.toString("hex"),
                    authTag: authTag.toString("hex"),
                    algorithm: "aes-256-gcm"
                };
            },
            decrypt: async (encryptedData, key, options = {}) => {
                const decipher = crypto.createDecipherGCM("aes-256-gcm", key, Buffer.from(encryptedData.iv, "hex"));
                decipher.setAuthTag(Buffer.from(encryptedData.authTag, "hex"));
                
                let decrypted = decipher.update(encryptedData.encrypted, "hex", "utf8");
                decrypted += decipher.final("utf8");
                
                return decrypted;
            }
        });

        // Simple encryption for testing
        this.registerAlgorithm("simple", {
            encrypt: async (data, key, options = {}) => {
                return { encrypted: data, algorithm: "simple" };
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
            "aes-256-gcm": 32,
            "simple": 32
        };
        return sizes[algorithm] || 32;
    }

    /**
     * Generate unique key ID
     */
    generateKeyId() {
        return `key_${Date.now()}_${crypto.randomBytes(8).toString("hex")}`;
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
                this.keys.delete(keyId);
                rotatedCount++;
            }
        }

        if (rotatedCount > 0) {
            console.log(`✓ Rotated ${rotatedCount} keys`);
            this.emit("keysRotated", { count: rotatedCount });
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
}

module.exports = { CryptographyFramework };
