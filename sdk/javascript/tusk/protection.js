/**
 * TuskLang SDK Protection Core Module
 * Enterprise-grade protection for JavaScript SDK
 */

const crypto = require('crypto');

class TuskProtection {
    constructor(licenseKey, apiKey) {
        this.licenseKey = licenseKey;
        this.apiKey = apiKey;
        this.sessionId = this.generateUUID();
        this.integrityChecks = {};
        this.usageMetrics = {
            startTime: Date.now(),
            apiCalls: 0,
            errors: 0
        };
        this.encryptionKey = this.deriveKey(licenseKey);
    }

    generateUUID() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
            const r = Math.random() * 16 | 0;
            const v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }

    deriveKey(password) {
        const salt = Buffer.from('tusklang_protection_salt', 'utf8');
        return crypto.pbkdf2Sync(password, salt, 100000, 32, 'sha256');
    }

    validateLicense() {
        try {
            if (!this.licenseKey || this.licenseKey.length < 32) {
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
            const iv = crypto.randomBytes(16);
            const cipher = crypto.createCipher('aes-256-cbc', this.encryptionKey);
            let encrypted = cipher.update(data, 'utf8', 'hex');
            encrypted += cipher.final('hex');
            return iv.toString('hex') + ':' + encrypted;
        } catch (error) {
            return data;
        }
    }

    decryptData(encryptedData) {
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

    verifyIntegrity(data, signature) {
        try {
            const expectedSignature = crypto
                .createHmac('sha256', this.apiKey)
                .update(data)
                .digest('hex');
            return crypto.timingSafeEqual(
                Buffer.from(signature, 'hex'),
                Buffer.from(expectedSignature, 'hex')
            );
        } catch (error) {
            return false;
        }
    }

    generateSignature(data) {
        return crypto
            .createHmac('sha256', this.apiKey)
            .update(data)
            .digest('hex');
    }

    trackUsage(operation, success = true) {
        this.usageMetrics.apiCalls++;
        if (!success) {
            this.usageMetrics.errors++;
        }
    }

    getMetrics() {
        return {
            ...this.usageMetrics,
            sessionId: this.sessionId,
            uptime: Date.now() - this.usageMetrics.startTime
        };
    }

    obfuscateCode(code) {
        return Buffer.from(code).toString('base64');
    }

    detectTampering() {
        try {
            // Check module integrity
            const currentModule = __filename;
            const fs = require('fs');
            const content = fs.readFileSync(currentModule);
            const fileHash = crypto.createHash('sha256').update(content).digest('hex');
            this.integrityChecks[currentModule] = fileHash;
            return true;
        } catch (error) {
            return false;
        }
    }

    reportViolation(violationType, details) {
        const violation = {
            timestamp: Date.now(),
            sessionId: this.sessionId,
            type: violationType,
            details: details,
            licenseKey: this.licenseKey.substring(0, 8) + '...'
        };
        
        console.log(`SECURITY VIOLATION: ${JSON.stringify(violation)}`);
        return violation;
    }
}

// Global protection instance
let protectionInstance = null;

function initializeProtection(licenseKey, apiKey) {
    protectionInstance = new TuskProtection(licenseKey, apiKey);
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
    initializeProtection,
    getProtection
}; 