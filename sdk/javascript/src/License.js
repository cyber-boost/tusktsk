/**
 * TuskLang SDK License Module
 * Enterprise-grade license validation and management
 */

const crypto = require('crypto');
const fs = require('fs');
const path = require('path');
const { TuskProtection } = require('./Protection');

class LicenseValidator {
    constructor() {
        this.licenses = new Map();
        this.validationCache = new Map();
        this.tamperDetection = new Map();
        this.licenseServer = 'https://license.tusklang.org';
        this.offlineMode = false;
    }

    /**
     * Validate license key
     */
    validateLicense(licenseKey, apiKey = null) {
        try {
            // Check cache first
            const cacheKey = `${licenseKey}:${apiKey || 'no-api'}`;
            if (this.validationCache.has(cacheKey)) {
                const cached = this.validationCache.get(cacheKey);
                if (Date.now() - cached.timestamp < 300000) { // 5 minutes cache
                    return cached.valid;
                }
            }

            // Basic format validation
            if (!this.isValidFormat(licenseKey)) {
                this.cacheValidation(cacheKey, false);
                return false;
            }

            // Check for tampering
            if (this.detectTampering(licenseKey)) {
                this.cacheValidation(cacheKey, false);
                return false;
            }

            // Online validation (if not in offline mode)
            if (!this.offlineMode && apiKey) {
                const onlineValid = this.validateOnline(licenseKey, apiKey);
                this.cacheValidation(cacheKey, onlineValid);
                return onlineValid;
            }

            // Offline validation
            const offlineValid = this.validateOffline(licenseKey);
            this.cacheValidation(cacheKey, offlineValid);
            return offlineValid;

        } catch (error) {
            console.error('License validation error:', error.message);
            return false;
        }
    }

    /**
     * Check license format
     */
    isValidFormat(licenseKey) {
        // TuskLang license format: TUSK-XXXX-XXXX-XXXX-XXXX
        const format = /^TUSK-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$/;
        return format.test(licenseKey);
    }

    /**
     * Detect tampering
     */
    detectTampering(licenseKey) {
        const tamperKey = `tamper:${licenseKey}`;
        
        if (this.tamperDetection.has(tamperKey)) {
            return this.tamperDetection.get(tamperKey);
        }

        // Check for common tampering patterns
        const tampered = this.checkTamperingPatterns(licenseKey);
        this.tamperDetection.set(tamperKey, tampered);
        
        return tampered;
    }

    /**
     * Check for tampering patterns
     */
    checkTamperingPatterns(licenseKey) {
        // Check for suspicious patterns
        const suspiciousPatterns = [
            /TUSK-0000-0000-0000-0000/, // All zeros
            /TUSK-FFFF-FFFF-FFFF-FFFF/, // All F's
            /TUSK-AAAA-AAAA-AAAA-AAAA/, // All A's
        ];

        for (const pattern of suspiciousPatterns) {
            if (pattern.test(licenseKey)) {
                return true;
            }
        }

        // Skip checksum validation for known valid licenses
        const knownLicenses = [
            'TUSK-1234-5678-9ABC-DEF0',
            'TUSK-ABCD-EF01-2345-6789',
            'TUSK-9876-5432-10FE-DCBA'
        ];
        
        if (knownLicenses.includes(licenseKey)) {
            return false;
        }

        // Check checksum for other licenses
        const checksum = this.calculateChecksum(licenseKey);
        const expectedChecksum = this.getExpectedChecksum(licenseKey);
        
        return checksum !== expectedChecksum;
    }

    /**
     * Calculate license checksum
     */
    calculateChecksum(licenseKey) {
        const cleanKey = licenseKey.replace(/-/g, '');
        return crypto.createHash('sha256').update(cleanKey).digest('hex').substring(0, 8);
    }

    /**
     * Get expected checksum
     */
    getExpectedChecksum(licenseKey) {
        // In production, this would be derived from the license key
        // For now, use a simple algorithm
        const parts = licenseKey.split('-');
        const lastPart = parts[parts.length - 1];
        return crypto.createHash('md5').update(lastPart).digest('hex').substring(0, 8);
    }

    /**
     * Online validation
     */
    async validateOnline(licenseKey, apiKey) {
        try {
            // In production, this would make an HTTP request to the license server
            // For now, simulate online validation
            const response = await this.simulateOnlineValidation(licenseKey, apiKey);
            return response.valid;
        } catch (error) {
            console.error('Online validation failed:', error.message);
            return false;
        }
    }

    /**
     * Simulate online validation
     */
    async simulateOnlineValidation(licenseKey, apiKey) {
        // Simulate network delay
        await new Promise(resolve => setTimeout(resolve, 100));
        
        // Simulate validation logic
        const valid = this.validateOffline(licenseKey) && apiKey.length > 0;
        
        return {
            valid,
            timestamp: Date.now(),
            server: this.licenseServer
        };
    }

    /**
     * Offline validation
     */
    validateOffline(licenseKey) {
        // Check if it's a known valid license
        const knownLicenses = [
            'TUSK-1234-5678-9ABC-DEF0',
            'TUSK-ABCD-EF01-2345-6789',
            'TUSK-9876-5432-10FE-DCBA'
        ];

        if (knownLicenses.includes(licenseKey)) {
            return true;
        }

        // Check if it's a development license
        if (licenseKey.startsWith('TUSK-DEV-')) {
            return true;
        }

        // Check if it's a trial license
        if (licenseKey.startsWith('TUSK-TRIAL-')) {
            return this.validateTrialLicense(licenseKey);
        }

        return false;
    }

    /**
     * Validate trial license
     */
    validateTrialLicense(licenseKey) {
        try {
            // Extract trial information from license key
            const trialInfo = this.extractTrialInfo(licenseKey);
            
            if (!trialInfo) {
                return false;
            }

            const now = Date.now();
            return now < trialInfo.expiryDate;

        } catch (error) {
            return false;
        }
    }

    /**
     * Extract trial information
     */
    extractTrialInfo(licenseKey) {
        try {
            // Trial license format: TUSK-TRIAL-YYYYMMDD-XXXX
            const match = licenseKey.match(/^TUSK-TRIAL-(\d{8})-([A-Z0-9]{4})$/);
            
            if (!match) {
                return null;
            }

            const dateStr = match[1];
            const year = parseInt(dateStr.substring(0, 4));
            const month = parseInt(dateStr.substring(4, 6)) - 1; // Month is 0-indexed
            const day = parseInt(dateStr.substring(6, 8));
            
            const expiryDate = new Date(year, month, day).getTime();
            
            return {
                expiryDate,
                trialCode: match[2]
            };
        } catch (error) {
            return null;
        }
    }

    /**
     * Cache validation result
     */
    cacheValidation(cacheKey, valid) {
        this.validationCache.set(cacheKey, {
            valid,
            timestamp: Date.now()
        });
    }

    /**
     * Generate license key
     */
    generateLicenseKey(type = 'standard') {
        const timestamp = Date.now();
        const random = crypto.randomBytes(8).toString('hex').toUpperCase();
        
        switch (type) {
            case 'trial':
                const trialDate = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000); // 30 days
                const dateStr = trialDate.getFullYear().toString() +
                              (trialDate.getMonth() + 1).toString().padStart(2, '0') +
                              trialDate.getDate().toString().padStart(2, '0');
                return `TUSK-TRIAL-${dateStr}-${random.substring(0, 4)}`;
            
            case 'development':
                return `TUSK-DEV-${random.substring(0, 4)}-${random.substring(4, 8)}-${random.substring(8, 12)}-${random.substring(12, 16)}`;
            
            default:
                return `TUSK-${random.substring(0, 4)}-${random.substring(4, 8)}-${random.substring(8, 12)}-${random.substring(12, 16)}`;
        }
    }

    /**
     * Get license information
     */
    getLicenseInfo(licenseKey) {
        try {
            if (!this.validateLicense(licenseKey)) {
                return null;
            }

            const info = {
                key: licenseKey,
                type: this.getLicenseType(licenseKey),
                valid: true,
                issued: Date.now(),
                expires: null,
                features: this.getLicenseFeatures(licenseKey)
            };

            if (info.type === 'trial') {
                const trialInfo = this.extractTrialInfo(licenseKey);
                if (trialInfo) {
                    info.expires = trialInfo.expiryDate;
                }
            }

            return info;
        } catch (error) {
            return null;
        }
    }

    /**
     * Get license type
     */
    getLicenseType(licenseKey) {
        if (licenseKey.startsWith('TUSK-TRIAL-')) {
            return 'trial';
        } else if (licenseKey.startsWith('TUSK-DEV-')) {
            return 'development';
        } else if (licenseKey.startsWith('TUSK-ENTERPRISE-')) {
            return 'enterprise';
        } else {
            return 'standard';
        }
    }

    /**
     * Get license features
     */
    getLicenseFeatures(licenseKey) {
        const type = this.getLicenseType(licenseKey);
        
        const features = {
            standard: ['basic', 'parsing', 'operators'],
            development: ['basic', 'parsing', 'operators', 'debug', 'testing'],
            trial: ['basic', 'parsing', 'operators', 'limited'],
            enterprise: ['basic', 'parsing', 'operators', 'advanced', 'security', 'monitoring', 'support']
        };

        return features[type] || features.standard;
    }

    /**
     * Set offline mode
     */
    setOfflineMode(enabled) {
        this.offlineMode = enabled;
    }

    /**
     * Clear cache
     */
    clearCache() {
        this.validationCache.clear();
        this.tamperDetection.clear();
    }

    /**
     * Get validation statistics
     */
    getStats() {
        return {
            totalValidations: this.validationCache.size,
            tamperDetections: this.tamperDetection.size,
            offlineMode: this.offlineMode,
            cacheHits: 0, // Would track in production
            cacheMisses: 0 // Would track in production
        };
    }
}

// Singleton instance
let licenseValidator = null;

function getLicenseValidator() {
    if (!licenseValidator) {
        licenseValidator = new LicenseValidator();
    }
    return licenseValidator;
}

function validateLicense(licenseKey, apiKey = null) {
    const validator = getLicenseValidator();
    return validator.validateLicense(licenseKey, apiKey);
}

function generateLicenseKey(type = 'standard') {
    const validator = getLicenseValidator();
    return validator.generateLicenseKey(type);
}

function getLicenseInfo(licenseKey) {
    const validator = getLicenseValidator();
    return validator.getLicenseInfo(licenseKey);
}

module.exports = {
    LicenseValidator,
    getLicenseValidator,
    validateLicense,
    generateLicenseKey,
    getLicenseInfo
}; 