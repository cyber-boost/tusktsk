/**
 * TuskLang SDK Authentication Module
 * Enterprise-grade authentication and key management for JavaScript SDK
 */

const crypto = require('crypto');
const { promisify } = require('util');

class AuthToken {
    constructor(token, expiresAt, userId, permissions, createdAt) {
        this.token = token;
        this.expires_at = expiresAt;
        this.user_id = userId;
        this.permissions = permissions;
        this.created_at = createdAt;
    }
}

class ApiKey {
    constructor(keyId, keyHash, userId, permissions, createdAt, lastUsed = null, expiresAt = null) {
        this.key_id = keyId;
        this.key_hash = keyHash;
        this.user_id = userId;
        this.permissions = permissions;
        this.created_at = createdAt;
        this.last_used = lastUsed;
        this.expires_at = expiresAt;
    }
}

class TuskAuth {
    constructor(masterKey) {
        this.masterKey = masterKey;
        this.encryptionKey = this.deriveKey(masterKey);
        
        // Key storage
        this.apiKeys = new Map();
        this.authTokens = new Map();
        this.keyRotationSchedule = new Map();
        
        // Security settings
        this.tokenExpiry = 3600; // 1 hour
        this.keyExpiry = 86400 * 30; // 30 days
        this.maxFailedAttempts = 5;
        this.lockoutDuration = 300; // 5 minutes
        this.failedAttempts = new Map();
        
        // Generate master key pair
        this.generateMasterKeys();
    }

    deriveKey(password) {
        const salt = Buffer.from('tusklang_auth_salt', 'utf8');
        return crypto.pbkdf2Sync(password, salt, 100000, 32, 'sha256');
    }

    generateMasterKeys() {
        try {
            // In a real implementation, generate RSA key pair
            // For now, use symmetric encryption
            this.privateKey = null;
            this.publicKey = null;
        } catch (error) {
            this.privateKey = null;
            this.publicKey = null;
        }
    }

    generateApiKey(userId, permissions, expiresIn = null) {
        try {
            // Generate key ID and secret
            const keyId = this.generateUUID();
            const secret = crypto.randomBytes(32).toString('base64url');
            const fullKey = `${keyId}.${secret}`;
            
            // Hash the secret for storage
            const keyHash = crypto.createHash('sha256').update(secret).digest('hex');
            
            // Calculate expiry
            const createdAt = Date.now() / 1000;
            let expiresAt = null;
            if (expiresIn) {
                expiresAt = createdAt + expiresIn;
            }
            
            // Store API key
            const apiKey = new ApiKey(
                keyId,
                keyHash,
                userId,
                permissions,
                createdAt,
                null,
                expiresAt
            );
            
            this.apiKeys.set(keyId, apiKey);
            
            // Schedule key rotation if needed
            if (expiresAt) {
                this.keyRotationSchedule.set(keyId, expiresAt);
            }
            
            return fullKey;
        } catch (error) {
            throw new Error(`Failed to generate API key: ${error.message}`);
        }
    }

    validateApiKey(apiKey) {
        try {
            // Parse key
            if (!apiKey.includes('.')) {
                return null;
            }
            
            const [keyId, secret] = apiKey.split('.', 2);
            
            // Check if key exists
            if (!this.apiKeys.has(keyId)) {
                return null;
            }
            
            const storedKey = this.apiKeys.get(keyId);
            
            // Check if key is expired
            if (storedKey.expires_at && Date.now() / 1000 > storedKey.expires_at) {
                return null;
            }
            
            // Validate secret
            const secretHash = crypto.createHash('sha256').update(secret).digest('hex');
            if (!crypto.timingSafeEqual(
                Buffer.from(storedKey.key_hash, 'hex'),
                Buffer.from(secretHash, 'hex')
            )) {
                return null;
            }
            
            // Update last used
            storedKey.last_used = Date.now() / 1000;
            
            return {
                user_id: storedKey.user_id,
                permissions: storedKey.permissions,
                key_id: keyId
            };
        } catch (error) {
            return null;
        }
    }

    revokeApiKey(keyId) {
        try {
            if (this.apiKeys.has(keyId)) {
                this.apiKeys.delete(keyId);
                this.keyRotationSchedule.delete(keyId);
                return true;
            }
            return false;
        } catch (error) {
            return false;
        }
    }

    generateAuthToken(userId, permissions, expiresIn = null) {
        try {
            // Generate token
            const tokenData = {
                user_id: userId,
                permissions: permissions,
                created_at: Date.now() / 1000,
                nonce: crypto.randomBytes(16).toString('base64url')
            };
            
            // Set expiry
            if (expiresIn) {
                tokenData.expires_at = Date.now() / 1000 + expiresIn;
            } else {
                tokenData.expires_at = Date.now() / 1000 + this.tokenExpiry;
            }
            
            // Sign token
            const tokenJson = JSON.stringify(tokenData);
            const signature = crypto
                .createHmac('sha256', this.masterKey)
                .update(tokenJson)
                .digest('hex');
            
            // Create final token
            const tokenParts = [tokenJson, signature];
            const token = Buffer.from(tokenParts.join('.')).toString('base64url');
            
            // Store token
            const authToken = new AuthToken(
                token,
                tokenData.expires_at,
                userId,
                permissions,
                tokenData.created_at
            );
            
            this.authTokens.set(token, authToken);
            
            return token;
        } catch (error) {
            throw new Error(`Failed to generate auth token: ${error.message}`);
        }
    }

    validateAuthToken(token) {
        try {
            // Decode token
            const decoded = Buffer.from(token, 'base64url').toString();
            const [tokenJson, signature] = decoded.split('.', 2);
            
            // Verify signature
            const expectedSignature = crypto
                .createHmac('sha256', this.masterKey)
                .update(tokenJson)
                .digest('hex');
            
            if (!crypto.timingSafeEqual(
                Buffer.from(signature, 'hex'),
                Buffer.from(expectedSignature, 'hex')
            )) {
                return null;
            }
            
            // Parse token data
            const tokenData = JSON.parse(tokenJson);
            
            // Check expiry
            if (tokenData.expires_at && Date.now() / 1000 > tokenData.expires_at) {
                return null;
            }
            
            // Check if token is in storage
            if (!this.authTokens.has(token)) {
                return null;
            }
            
            return {
                user_id: tokenData.user_id,
                permissions: tokenData.permissions,
                created_at: tokenData.created_at
            };
        } catch (error) {
            return null;
        }
    }

    revokeAuthToken(token) {
        try {
            if (this.authTokens.has(token)) {
                this.authTokens.delete(token);
                return true;
            }
            return false;
        } catch (error) {
            return false;
        }
    }

    checkPermission(userId, permission, authData) {
        try {
            const permissions = authData.permissions || [];
            return permissions.includes(permission) || permissions.includes('admin');
        } catch (error) {
            return false;
        }
    }

    encryptSensitiveData(data) {
        try {
            const iv = crypto.randomBytes(16);
            const cipher = crypto.createCipher('aes-256-cbc', this.encryptionKey);
            let encrypted = cipher.update(data, 'utf8', 'hex');
            encrypted += cipher.final('hex');
            return iv.toString('hex') + ':' + encrypted;
        } catch (error) {
            return data;
        }
    }

    decryptSensitiveData(encryptedData) {
        try {
            const parts = encryptedData.split(':');
            const iv = Buffer.from(parts[0], 'hex');
            const encrypted = parts[1];
            const decipher = crypto.createDecipher('aes-256-cbc', this.encryptionKey);
            let decrypted = decipher.update(encrypted, 'hex', 'utf8');
            decrypted += decipher.final('utf8');
            return decrypted;
        } catch (error) {
            return encryptedData;
        }
    }

    rotateKeys() {
        try {
            const currentTime = Date.now() / 1000;
            const rotatedKeys = [];
            
            // Check API keys
            const keysToRemove = [];
            for (const [keyId, apiKey] of this.apiKeys) {
                if (apiKey.expires_at && currentTime > apiKey.expires_at) {
                    keysToRemove.push(keyId);
                    rotatedKeys.push(keyId);
                }
            }
            
            for (const keyId of keysToRemove) {
                this.apiKeys.delete(keyId);
                this.keyRotationSchedule.delete(keyId);
            }
            
            // Check auth tokens
            const tokensToRemove = [];
            for (const [token, authToken] of this.authTokens) {
                if (currentTime > authToken.expires_at) {
                    tokensToRemove.push(token);
                    rotatedKeys.push(token);
                }
            }
            
            for (const token of tokensToRemove) {
                this.authTokens.delete(token);
            }
            
            return rotatedKeys;
        } catch (error) {
            throw new Error(`Failed to rotate keys: ${error.message}`);
        }
    }

    getAuthStats() {
        try {
            const currentTime = Date.now() / 1000;
            
            // Count active keys and tokens
            let activeApiKeys = 0;
            let activeTokens = 0;
            let expiredApiKeys = 0;
            let expiredTokens = 0;
            
            for (const apiKey of this.apiKeys.values()) {
                if (!apiKey.expires_at || currentTime < apiKey.expires_at) {
                    activeApiKeys++;
                } else {
                    expiredApiKeys++;
                }
            }
            
            for (const authToken of this.authTokens.values()) {
                if (currentTime < authToken.expires_at) {
                    activeTokens++;
                } else {
                    expiredTokens++;
                }
            }
            
            return {
                active_api_keys: activeApiKeys,
                active_tokens: activeTokens,
                expired_api_keys: expiredApiKeys,
                expired_tokens: expiredTokens,
                total_api_keys: this.apiKeys.size,
                total_tokens: this.authTokens.size,
                failed_attempts: this.failedAttempts.size
            };
        } catch (error) {
            return { error: error.message };
        }
    }

    cleanupExpired() {
        try {
            this.rotateKeys();
            
            // Clean up failed attempts
            const currentTime = Date.now() / 1000;
            const expiredAttempts = [];
            for (const [userId, attemptData] of this.failedAttempts) {
                if (currentTime - attemptData.last_attempt > this.lockoutDuration) {
                    expiredAttempts.push(userId);
                }
            }
            
            for (const userId of expiredAttempts) {
                this.failedAttempts.delete(userId);
            }
        } catch (error) {
            throw new Error(`Failed to cleanup expired items: ${error.message}`);
        }
    }

    generateUUID() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
            const r = Math.random() * 16 | 0;
            const v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }
}

// Global auth instance
let authInstance = null;

function initializeAuth(masterKey) {
    authInstance = new TuskAuth(masterKey);
    return authInstance;
}

function getAuth() {
    if (!authInstance) {
        throw new Error('Auth not initialized. Call initializeAuth() first.');
    }
    return authInstance;
}

module.exports = {
    TuskAuth,
    AuthToken,
    ApiKey,
    initializeAuth,
    getAuth
}; 