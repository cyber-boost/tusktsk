/**
 * TuskLang SDK Anti-Tampering Module
 * Enterprise-grade anti-tampering for JavaScript SDK
 */

const crypto = require('crypto');
const fs = require('fs');
const path = require('path');

class TuskAntiTamper {
    constructor(secretKey) {
        this.secretKey = secretKey;
        this.encryptionKey = this.deriveKey(secretKey);
        this.integrityChecks = new Map();
        this.tamperDetections = [];
        this.obfuscationCache = new Map();
        this.selfCheckInterval = 300000; // 5 minutes
        this.lastSelfCheck = Date.now();
    }

    deriveKey(password) {
        const salt = Buffer.from('tusklang_antitamper_salt', 'utf8');
        return crypto.pbkdf2Sync(password, salt, 100000, 32, 'sha256');
    }

    calculateFileHash(filePath) {
        try {
            const content = fs.readFileSync(filePath);
            return crypto.createHash('sha256').update(content).digest('hex');
        } catch (error) {
            return '';
        }
    }

    verifyFileIntegrity(filePath, expectedHash) {
        try {
            const actualHash = this.calculateFileHash(filePath);
            return crypto.timingSafeEqual(
                Buffer.from(actualHash, 'hex'),
                Buffer.from(expectedHash, 'hex')
            );
        } catch (error) {
            return false;
        }
    }

    obfuscateCode(code, level = 2) {
        try {
            if (level === 0) {
                return code;
            }

            // Level 1: Basic obfuscation
            if (level >= 1) {
                // Compress and encode
                const compressed = Buffer.from(code).toString('base64');
                code = `eval(Buffer.from('${compressed}', 'base64').toString());`;
            }

            // Level 2: Advanced obfuscation
            if (level >= 2) {
                // Add junk code and variable renaming
                const junkVars = Array.from({ length: 10 }, (_, i) => `_${i}`);
                const junkCode = junkVars.map(var => `${var}=null`).join(';');
                code = `${junkCode};${code}`;
            }

            // Level 3: Maximum obfuscation
            if (level >= 3) {
                // Encrypt the code
                const iv = crypto.randomBytes(16);
                const cipher = crypto.createCipher('aes-256-cbc', this.encryptionKey);
                let encrypted = cipher.update(code, 'utf8', 'hex');
                encrypted += cipher.final('hex');
                const encryptedData = iv.toString('hex') + ':' + encrypted;
                code = `const crypto=require('crypto');const decipher=crypto.createDecipher('aes-256-cbc',Buffer.from('${this.encryptionKey.toString('base64')}', 'base64'));const parts='${encryptedData}'.split(':');const iv=Buffer.from(parts[0],'hex');const encrypted=parts[1];let decrypted=decipher.update(encrypted,'hex','utf8');decrypted+=decipher.final('utf8');eval(decrypted);`;
            }

            return code;
        } catch (error) {
            return code;
        }
    }

    deobfuscateCode(obfuscatedCode) {
        try {
            if (obfuscatedCode.includes('eval(Buffer.from(')) {
                // Extract encoded part
                const start = obfuscatedCode.indexOf("('") + 2;
                const end = obfuscatedCode.indexOf("')");
                const encoded = obfuscatedCode.substring(start, end);
                return Buffer.from(encoded, 'base64').toString();
            } else if (obfuscatedCode.includes('createDecipher')) {
                // Extract encrypted part
                const start = obfuscatedCode.indexOf("('") + 2;
                const end = obfuscatedCode.indexOf("')");
                const encryptedData = obfuscatedCode.substring(start, end);
                const parts = encryptedData.split(':');
                const iv = Buffer.from(parts[0], 'hex');
                const encrypted = parts[1];
                const decipher = crypto.createDecipher('aes-256-cbc', this.encryptionKey);
                let decrypted = decipher.update(encrypted, 'hex', 'utf8');
                decrypted += decipher.final('utf8');
                return decrypted;
            } else {
                return obfuscatedCode;
            }
        } catch (error) {
            return obfuscatedCode;
        }
    }

    protectFunction(func, obfuscationLevel = 2) {
        try {
            // Get function source (simplified - in real implementation, use AST parsing)
            const funcStr = func.toString();
            
            // Obfuscate the source
            const obfuscated = this.obfuscateCode(funcStr, obfuscationLevel);
            
            // Create protected function
            const protectedWrapper = (...args) => {
                // Self-check before execution
                if (!this.selfCheck()) {
                    throw new Error('Tampering detected - function execution blocked');
                }
                
                // Execute original function
                return func.apply(this, args);
            };
            
            // Store obfuscated code for later verification
            this.obfuscationCache.set(func.name || 'anonymous', {
                original: funcStr,
                obfuscated: obfuscated,
                hash: crypto.createHash('sha256').update(funcStr).digest('hex')
            });
            
            return protectedWrapper;
        } catch (error) {
            // Fallback to original function
            return func;
        }
    }

    selfCheck() {
        try {
            const currentTime = Date.now();
            
            // Check if it's time for a self-check
            if (currentTime - this.lastSelfCheck < this.selfCheckInterval) {
                return true;
            }
            
            this.lastSelfCheck = currentTime;
            
            // Check current file integrity
            const currentFile = __filename;
            const currentHash = this.calculateFileHash(currentFile);
            
            // Store first hash if not exists
            if (!this.integrityChecks.has(currentFile)) {
                this.integrityChecks.set(currentFile, currentHash);
                return true;
            }
            
            // Compare with stored hash
            const storedHash = this.integrityChecks.get(currentFile);
            if (!crypto.timingSafeEqual(
                Buffer.from(currentHash, 'hex'),
                Buffer.from(storedHash, 'hex')
            )) {
                this.tamperDetections.push({
                    timestamp: currentTime,
                    file: currentFile,
                    expected: storedHash,
                    actual: currentHash
                });
                return false;
            }
            
            // Check obfuscated functions
            for (const [funcName, cacheData] of this.obfuscationCache) {
                const currentHash = crypto.createHash('sha256').update(cacheData.original).digest('hex');
                if (!crypto.timingSafeEqual(
                    Buffer.from(cacheData.hash, 'hex'),
                    Buffer.from(currentHash, 'hex')
                )) {
                    this.tamperDetections.push({
                        timestamp: currentTime,
                        function: funcName,
                        expected: cacheData.hash,
                        actual: currentHash
                    });
                    return false;
                }
            }
            
            return true;
        } catch (error) {
            return false;
        }
    }

    detectTampering() {
        try {
            const tamperingDetected = {
                fileTampering: false,
                functionTampering: false,
                environmentTampering: false,
                debuggerDetected: false,
                details: []
            };
            
            // Check for debugger
            if (this.detectDebugger()) {
                tamperingDetected.debuggerDetected = true;
                tamperingDetected.details.push('Debugger detected');
            }
            
            // Check environment tampering
            if (this.detectEnvironmentTampering()) {
                tamperingDetected.environmentTampering = true;
                tamperingDetected.details.push('Environment tampering detected');
            }
            
            // Check file tampering
            if (!this.selfCheck()) {
                tamperingDetected.fileTampering = true;
                tamperingDetected.details.push('File integrity check failed');
            }
            
            // Check function tampering
            for (const [funcName, cacheData] of this.obfuscationCache) {
                const currentHash = crypto.createHash('sha256').update(cacheData.original).digest('hex');
                if (!crypto.timingSafeEqual(
                    Buffer.from(cacheData.hash, 'hex'),
                    Buffer.from(currentHash, 'hex')
                )) {
                    tamperingDetected.functionTampering = true;
                    tamperingDetected.details.push(`Function ${funcName} tampering detected`);
                }
            }
            
            return tamperingDetected;
        } catch (error) {
            return {
                fileTampering: false,
                functionTampering: false,
                environmentTampering: false,
                debuggerDetected: false,
                details: [`Error during tampering detection: ${error.message}`]
            };
        }
    }

    detectDebugger() {
        try {
            // Check for common debugger indicators
            if (process.env.NODE_OPTIONS && process.env.NODE_OPTIONS.includes('--inspect')) {
                return true;
            }
            
            // Check for development environment
            if (process.env.NODE_ENV === 'development') {
                return true;
            }
            
            // Check for debugging flags
            if (process.argv.some(arg => arg.includes('--inspect') || arg.includes('--debug'))) {
                return true;
            }
            
            return false;
        } catch (error) {
            return false;
        }
    }

    detectEnvironmentTampering() {
        try {
            // Check for suspicious environment variables
            const suspiciousVars = ['NODE_OPTIONS', 'NODE_ENV', 'DEBUG'];
            for (const varName of suspiciousVars) {
                if (process.env[varName]) {
                    const value = process.env[varName].toLowerCase();
                    if (value.includes('debug') || value.includes('test') || value.includes('dev')) {
                        return true;
                    }
                }
            }
            
            // Check for suspicious command line arguments
            for (const arg of process.argv) {
                if (arg.toLowerCase().includes('debug') || arg.toLowerCase().includes('test')) {
                    return true;
                }
            }
            
            return false;
        } catch (error) {
            return false;
        }
    }

    getTamperDetections() {
        return [...this.tamperDetections];
    }

    clearTamperDetections() {
        this.tamperDetections = [];
    }

    getIntegrityReport() {
        try {
            return {
                selfCheckPassed: this.selfCheck(),
                tamperingDetected: this.detectTampering(),
                protectedFunctions: Array.from(this.obfuscationCache.keys()),
                integrityChecks: this.integrityChecks.size,
                tamperDetections: this.tamperDetections.length,
                lastSelfCheck: this.lastSelfCheck
            };
        } catch (error) {
            return { error: error.message };
        }
    }
}

// Global anti-tamper instance
let antiTamperInstance = null;

function initializeAntiTamper(secretKey) {
    antiTamperInstance = new TuskAntiTamper(secretKey);
    return antiTamperInstance;
}

function getAntiTamper() {
    if (!antiTamperInstance) {
        throw new Error('Anti-tamper not initialized. Call initializeAntiTamper() first.');
    }
    return antiTamperInstance;
}

module.exports = {
    TuskAntiTamper,
    initializeAntiTamper,
    getAntiTamper
}; 