/**
 * TuskLang SDK License Validation Module
 * Enterprise-grade license validation for JavaScript SDK
 */

const crypto = require('crypto');
const https = require('https');
const http = require('http');
const fs = require('fs').promises;
const path = require('path');
const os = require('os');

class TuskLicense {
    constructor(licenseKey, apiKey, cacheDir = null) {
        this.licenseKey = licenseKey;
        this.apiKey = apiKey;
        this.sessionId = this.generateUUID();
        this.licenseCache = new Map();
        this.validationHistory = [];
        this.expirationWarnings = [];
        
        // Set up offline cache directory
        if (cacheDir) {
            this.cacheDir = cacheDir;
        } else {
            this.cacheDir = path.join(os.homedir(), '.tusk', 'license_cache');
        }
        this.cacheFile = path.join(this.cacheDir, `${crypto.createHash('md5').update(licenseKey).digest('hex')}.cache`);
        
        // Load offline cache if exists
        this.offlineCache = null;
        this._loadOfflineCache();
    }

    generateUUID() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
            const r = Math.random() * 16 | 0;
            const v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }

    validateLicenseKey() {
        try {
            if (!this.licenseKey || this.licenseKey.length < 32) {
                return { valid: false, error: 'Invalid license key format' };
            }

            if (!this.licenseKey.startsWith('TUSK-')) {
                return { valid: false, error: 'Invalid license key prefix' };
            }

            const checksum = crypto.createHash('sha256').update(this.licenseKey).digest('hex');
            if (!checksum.startsWith('tusk')) {
                return { valid: false, error: 'Invalid license key checksum' };
            }

            return { valid: true, checksum };
        } catch (error) {
            return { valid: false, error: error.message };
        }
    }

    async verifyLicenseServer(serverUrl = 'https://api.tusklang.org/v1/license') {
        return new Promise((resolve) => {
            try {
                const data = {
                    license_key: this.licenseKey,
                    session_id: this.sessionId,
                    timestamp: Math.floor(Date.now() / 1000)
                };

                const signature = crypto
                    .createHmac('sha256', this.apiKey)
                    .update(JSON.stringify(data))
                    .digest('hex');
                data.signature = signature;

                const postData = JSON.stringify(data);
                const url = new URL(serverUrl);
                const options = {
                    hostname: url.hostname,
                    port: url.port || (url.protocol === 'https:' ? 443 : 80),
                    path: url.pathname,
                    method: 'POST',
                    headers: {
                        'Authorization': `Bearer ${this.apiKey}`,
                        'Content-Type': 'application/json',
                        'Content-Length': Buffer.byteLength(postData)
                    }
                };

                const client = url.protocol === 'https:' ? https : http;
                const req = client.request(options, (res) => {
                    let responseData = '';
                    res.on('data', (chunk) => {
                        responseData += chunk;
                    });
                    res.on('end', async () => {
                        if (res.statusCode === 200) {
                            try {
                                const result = JSON.parse(responseData);
                                this.licenseCache.set(this.licenseKey, {
                                    data: result,
                                    timestamp: Date.now(),
                                    expires: Date.now() + 3600000 // 1 hour
                                });
                                // Save to offline cache
                                await this._saveOfflineCache(result);
                                resolve(result);
                            } catch (error) {
                                console.warn('Failed to parse server response:', error);
                                resolve(await this._fallbackToOfflineCache('Invalid response format'));
                            }
                        } else {
                            console.warn(`Server returned error: ${res.statusCode}`);
                            resolve(await this._fallbackToOfflineCache(`Server error: ${res.statusCode}`));
                        }
                    });
                });

                req.on('error', async (error) => {
                    console.warn('Network error during license validation:', error.message);
                    resolve(await this._fallbackToOfflineCache(`Network error: ${error.message}`));
                });

                req.setTimeout(10000, async () => {
                    req.destroy();
                    console.warn('License validation request timeout');
                    resolve(await this._fallbackToOfflineCache('Request timeout'));
                });

                req.write(postData);
                req.end();
            } catch (error) {
                console.error('Unexpected error during license validation:', error);
                resolve(await this._fallbackToOfflineCache(error.message));
            }
        });
    }
    
    async _loadOfflineCache() {
        try {
            await fs.mkdir(this.cacheDir, { recursive: true });
            const cacheData = await fs.readFile(this.cacheFile, 'utf8');
            const cached = JSON.parse(cacheData);
            
            // Verify the cache is for the correct license key
            const keyHash = crypto.createHash('sha256').update(this.licenseKey).digest('hex');
            if (cached.licenseKeyHash === keyHash) {
                this.offlineCache = cached;
                console.info('Loaded offline license cache');
            } else {
                console.warn('Offline cache key mismatch');
                this.offlineCache = null;
            }
        } catch (error) {
            // Cache file doesn't exist or is invalid
            this.offlineCache = null;
        }
    }
    
    async _saveOfflineCache(licenseData) {
        try {
            const cacheData = {
                licenseKeyHash: crypto.createHash('sha256').update(this.licenseKey).digest('hex'),
                licenseData: licenseData,
                timestamp: Date.now(),
                expiration: this.checkLicenseExpiration()
            };
            
            await fs.mkdir(this.cacheDir, { recursive: true });
            await fs.writeFile(this.cacheFile, JSON.stringify(cacheData, null, 2));
            this.offlineCache = cacheData;
            console.info('Saved license data to offline cache');
        } catch (error) {
            console.error('Failed to save offline cache:', error);
        }
    }
    
    async _fallbackToOfflineCache(errorMsg) {
        if (this.offlineCache && this.offlineCache.licenseData) {
            const cacheAge = Date.now() - this.offlineCache.timestamp;
            const cacheAgeDays = cacheAge / 86400000;
            
            // Check if cached license is not expired
            const expiration = this.offlineCache.expiration || {};
            if (!expiration.expired) {
                console.warn(`Using offline license cache (age: ${cacheAgeDays.toFixed(1)} days)`);
                return {
                    ...this.offlineCache.licenseData,
                    offline_mode: true,
                    cache_age_days: cacheAgeDays,
                    warning: `Operating in offline mode due to: ${errorMsg}`
                };
            } else {
                return {
                    valid: false,
                    error: `License expired and server unreachable: ${errorMsg}`,
                    offline_cache_expired: true
                };
            }
        } else {
            return {
                valid: false,
                error: `No offline cache available: ${errorMsg}`,
                offline_cache_missing: true
            };
        }
    }

    checkLicenseExpiration() {
        try {
            const parts = this.licenseKey.split('-');
            if (parts.length < 4) {
                return { expired: true, error: 'Invalid license key format' };
            }

            const expirationStr = parts[parts.length - 1];
            const expirationTimestamp = parseInt(expirationStr, 16);
            const expirationDate = new Date(expirationTimestamp * 1000);
            const currentDate = new Date();

            if (expirationDate < currentDate) {
                return {
                    expired: true,
                    expiration_date: expirationDate.toISOString(),
                    days_overdue: Math.floor((currentDate - expirationDate) / (1000 * 60 * 60 * 24))
                };
            }

            const daysUntilExpiration = Math.floor((expirationDate - currentDate) / (1000 * 60 * 60 * 24));

            if (daysUntilExpiration <= 30) {
                this.expirationWarnings.push({
                    timestamp: Date.now(),
                    days_remaining: daysUntilExpiration
                });
            }

            return {
                expired: false,
                expiration_date: expirationDate.toISOString(),
                days_remaining: daysUntilExpiration,
                warning: daysUntilExpiration <= 30
            };
        } catch (error) {
            return { expired: true, error: error.message };
        }
    }

    validateLicensePermissions(feature) {
        try {
            if (this.licenseCache.has(this.licenseKey)) {
                const cacheData = this.licenseCache.get(this.licenseKey);
                if (Date.now() < cacheData.expires) {
                    const licenseData = cacheData.data;
                    if (licenseData.features) {
                        const allowedFeatures = licenseData.features;
                        if (allowedFeatures.includes(feature)) {
                            return { allowed: true, feature };
                        } else {
                            return { allowed: false, feature, error: 'Feature not licensed' };
                        }
                    }
                }
            }

            if (['basic', 'core', 'standard'].includes(feature)) {
                return { allowed: true, feature };
            } else if (['premium', 'enterprise'].includes(feature)) {
                if (this.licenseKey.toUpperCase().includes('PREMIUM') || 
                    this.licenseKey.toUpperCase().includes('ENTERPRISE')) {
                    return { allowed: true, feature };
                } else {
                    return { allowed: false, feature, error: 'Premium license required' };
                }
            } else {
                return { allowed: false, feature, error: 'Unknown feature' };
            }
        } catch (error) {
            return { allowed: false, feature, error: error.message };
        }
    }

    getLicenseInfo() {
        try {
            const validationResult = this.validateLicenseKey();
            const expirationResult = this.checkLicenseExpiration();

            const info = {
                license_key: this.licenseKey.substring(0, 8) + '...' + this.licenseKey.substring(this.licenseKey.length - 4),
                session_id: this.sessionId,
                validation: validationResult,
                expiration: expirationResult,
                cache_status: this.licenseCache.has(this.licenseKey) ? 'cached' : 'not_cached',
                validation_count: this.validationHistory.length,
                warnings: this.expirationWarnings.length
            };

            if (this.licenseCache.has(this.licenseKey)) {
                const cacheData = this.licenseCache.get(this.licenseKey);
                info.cached_data = cacheData.data;
                info.cache_age = Date.now() - cacheData.timestamp;
            }

            return info;
        } catch (error) {
            return { error: error.message };
        }
    }

    refreshLicenseCache() {
        return this.verifyLicenseServer();
    }

    logValidationAttempt(success, details = '') {
        this.validationHistory.push({
            timestamp: Date.now(),
            success,
            details,
            session_id: this.sessionId
        });
    }

    getValidationHistory() {
        return this.validationHistory;
    }

    clearValidationHistory() {
        this.validationHistory = [];
    }
}

// Global license instance
let licenseInstance = null;

function initializeLicense(licenseKey, apiKey, cacheDir = null) {
    licenseInstance = new TuskLicense(licenseKey, apiKey, cacheDir);
    return licenseInstance;
}

function getLicense() {
    if (!licenseInstance) {
        throw new Error('License not initialized. Call initializeLicense() first.');
    }
    return licenseInstance;
}

module.exports = {
    TuskLicense,
    initializeLicense,
    getLicense
}; 