/**
 * TuskLang SDK Protection Core Module
 * Enterprise-grade protection for JavaScript SDK
 */

const crypto = require('crypto');
const fs = require('fs');
const path = require('path');

class UsageMetrics {
    constructor() {
        this.startTime = Date.now();
        this.apiCalls = 0;
        this.errors = 0;
    }

    incrementApiCalls() {
        this.apiCalls++;
    }

    incrementErrors() {
        this.errors++;
    }

    getStartTime() {
        return this.startTime;
    }

    getApiCalls() {
        return this.apiCalls;
    }

    getErrors() {
        return this.errors;
    }
}

class Violation {
    constructor(timestamp, sessionId, violationType, details, licenseKeyPartial) {
        this.timestamp = timestamp;
        this.sessionId = sessionId;
        this.violationType = violationType;
        this.details = details;
        this.licenseKeyPartial = licenseKeyPartial;
    }

    toString() {
        return `VIOLATION [${new Date(this.timestamp).toISOString()}] ${this.violationType}: ${this.details} (Session: ${this.sessionId}, License: ${this.licenseKeyPartial})`;
    }
}

class TuskProtection {
    constructor(licenseKey, apiKey) {
        this.licenseKey = licenseKey;
        this.apiKey = apiKey;
        this.sessionId = this.generateUUID();
        this.encryptionKey = this.deriveKey(licenseKey);
        this.integrityChecks = [];
        this.usageMetrics = new UsageMetrics();
    }

    generateUUID() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
            const r = Math.random() * 16 | 0;
            const v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }

    deriveKey(password) {
        const salt = 'tusklang_protection_salt';
        return crypto.pbkdf2Sync(password, salt, 100000, 32, 'sha256');
    }

    validateLicense() {
        try {
            if (this.licenseKey.length < 32) {
                return false;
            }

            const checksum = crypto.createHash('sha256').update(this.licenseKey).digest('hex');
            return checksum.startsWith('tusk');
        } catch (error) {
            return false;
        }
    }

    encryptData(data) {
        try {
            const iv = crypto.randomBytes(12);
            const cipher = crypto.createCipherGCM('aes-256-gcm', this.encryptionKey);
            
            let encrypted = cipher.update(data, 'utf8', 'hex');
            encrypted += cipher.final('hex');
            
            const tag = cipher.getAuthTag();
            
            const result = Buffer.concat([iv, tag, Buffer.from(encrypted, 'hex')]);
            return result.toString('base64');
        } catch (error) {
            return data;
        }
    }

    decryptData(encryptedData) {
        try {
            const decoded = Buffer.from(encryptedData, 'base64');
            if (decoded.length < 28) {
                return encryptedData;
            }

            const iv = decoded.slice(0, 12);
            const tag = decoded.slice(12, 28);
            const encrypted = decoded.slice(28);

            const decipher = crypto.createDecipherGCM('aes-256-gcm', this.encryptionKey);
            decipher.setAuthTag(tag);
            
            let decrypted = decipher.update(encrypted, null, 'utf8');
            decrypted += decipher.final('utf8');
            
            return decrypted;
        } catch (error) {
            return encryptedData;
        }
    }

    verifyIntegrity(data, signature) {
        try {
            const expectedSignature = this.generateSignature(data);
            return crypto.timingSafeEqual(Buffer.from(signature), Buffer.from(expectedSignature));
        } catch (error) {
            return false;
        }
    }

    generateSignature(data) {
        return crypto.createHmac('sha256', this.apiKey).update(data).digest('hex');
    }

    trackUsage(operation, success = true) {
        this.usageMetrics.incrementApiCalls();
        if (!success) {
            this.usageMetrics.incrementErrors();
        }
    }

    getMetrics() {
        return {
            start_time: this.usageMetrics.getStartTime(),
            api_calls: this.usageMetrics.getApiCalls(),
            errors: this.usageMetrics.getErrors(),
            session_id: this.sessionId,
            uptime: Date.now() - this.usageMetrics.getStartTime()
        };
    }

    obfuscateCode(code) {
        return Buffer.from(code).toString('base64');
    }

    detectTampering() {
        try {
            // In production, implement file integrity checks
            // For now, return true as placeholder
            return true;
        } catch (error) {
            return false;
        }
    }

    reportViolation(violationType, details) {
        const violation = new Violation(
            Date.now(),
            this.sessionId,
            violationType,
            details,
            this.licenseKey.substring(0, 8) + '...'
        );

        console.error("SECURITY VIOLATION: " + violation.toString());
        return violation;
    }

    // Advanced security features
    encryptFile(filePath) {
        try {
            const content = fs.readFileSync(filePath, 'utf8');
            const encrypted = this.encryptData(content);
            const encryptedPath = filePath + '.enc';
            fs.writeFileSync(encryptedPath, encrypted);
            return encryptedPath;
        } catch (error) {
            throw new Error(`Failed to encrypt file: ${error.message}`);
        }
    }

    decryptFile(encryptedFilePath) {
        try {
            const encrypted = fs.readFileSync(encryptedFilePath, 'utf8');
            const decrypted = this.decryptData(encrypted);
            const decryptedPath = encryptedFilePath.replace('.enc', '');
            fs.writeFileSync(decryptedPath, decrypted);
            return decryptedPath;
        } catch (error) {
            throw new Error(`Failed to decrypt file: ${error.message}`);
        }
    }

    generateSecureToken() {
        return crypto.randomBytes(32).toString('hex');
    }

    hashPassword(password) {
        const salt = crypto.randomBytes(16).toString('hex');
        const hash = crypto.pbkdf2Sync(password, salt, 100000, 64, 'sha512').toString('hex');
        return `${salt}:${hash}`;
    }

    verifyPassword(password, hashedPassword) {
        const [salt, hash] = hashedPassword.split(':');
        const verifyHash = crypto.pbkdf2Sync(password, salt, 100000, 64, 'sha512').toString('hex');
        return crypto.timingSafeEqual(Buffer.from(hash), Buffer.from(verifyHash));
    }

    // Self-destruct mechanism
    selfDestruct() {
        try {
            // Clear sensitive data
            this.encryptionKey = null;
            this.licenseKey = null;
            this.apiKey = null;
            
            // Report violation
            this.reportViolation('SELF_DESTRUCT', 'Protection system activated self-destruct');
            
            // In production, this could trigger additional cleanup
            return true;
        } catch (error) {
            return false;
        }
    }
}

// Singleton pattern
let protectionInstance = null;

function initializeProtection(licenseKey, apiKey) {
    if (!protectionInstance) {
        protectionInstance = new TuskProtection(licenseKey, apiKey);
    }
    return protectionInstance;
}

function getProtection() {
    if (!protectionInstance) {
        throw new Error('Protection not initialized. Call initializeProtection() first.');
    }
    return protectionInstance;
}

module.exports = {
    TuskProtection,
    UsageMetrics,
    Violation,
    initializeProtection,
    getProtection
}; 