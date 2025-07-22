/**
 * TuskLang Request Validation Middleware
 * ======================================
 * Comprehensive request validation and sanitization
 */

const { validationResult } = require('express-validator');

/**
 * Request validation middleware
 */
function requestValidator(req, res, next) {
    // Generate unique request ID
    req.id = generateRequestId();
    
    // Add request timestamp
    req.timestamp = new Date().toISOString();
    
    // Validate request size
    const contentLength = parseInt(req.get('Content-Length') || '0');
    const maxSize = 10 * 1024 * 1024; // 10MB
    
    if (contentLength > maxSize) {
        return res.status(413).json({
            error: 'Request too large',
            message: 'Request body exceeds maximum allowed size',
            maxSize: maxSize,
            receivedSize: contentLength,
            timestamp: new Date().toISOString()
        });
    }
    
    // Validate content type for POST/PUT requests
    if (['POST', 'PUT', 'PATCH'].includes(req.method)) {
        const contentType = req.get('Content-Type');
        
        if (req.body && Object.keys(req.body).length > 0) {
            if (!contentType || !contentType.includes('application/json')) {
                return res.status(400).json({
                    error: 'Invalid content type',
                    message: 'Content-Type must be application/json',
                    receivedType: contentType,
                    timestamp: new Date().toISOString()
                });
            }
        }
    }
    
    // Sanitize request body
    if (req.body) {
        req.body = sanitizeObject(req.body);
    }
    
    // Sanitize query parameters
    if (req.query) {
        req.query = sanitizeObject(req.query);
    }
    
    // Sanitize URL parameters
    if (req.params) {
        req.params = sanitizeObject(req.params);
    }
    
    next();
}

/**
 * Validation result handler
 */
function handleValidationErrors(req, res, next) {
    const errors = validationResult(req);
    
    if (!errors.isEmpty()) {
        return res.status(400).json({
            error: 'Validation failed',
            details: errors.array(),
            timestamp: new Date().toISOString()
        });
    }
    
    next();
}

/**
 * Generate unique request ID
 */
function generateRequestId() {
    return 'req_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
}

/**
 * Sanitize object recursively
 */
function sanitizeObject(obj) {
    if (typeof obj !== 'object' || obj === null) {
        return sanitizeValue(obj);
    }
    
    if (Array.isArray(obj)) {
        return obj.map(item => sanitizeObject(item));
    }
    
    const sanitized = {};
    for (const [key, value] of Object.entries(obj)) {
        const sanitizedKey = sanitizeKey(key);
        if (sanitizedKey) {
            sanitized[sanitizedKey] = sanitizeObject(value);
        }
    }
    
    return sanitized;
}

/**
 * Sanitize individual value
 */
function sanitizeValue(value) {
    if (typeof value === 'string') {
        return sanitizeString(value);
    }
    
    if (typeof value === 'number') {
        return isFinite(value) ? value : 0;
    }
    
    if (typeof value === 'boolean') {
        return Boolean(value);
    }
    
    return value;
}

/**
 * Sanitize string value
 */
function sanitizeString(str) {
    if (typeof str !== 'string') {
        return '';
    }
    
    // Remove null bytes
    str = str.replace(/\0/g, '');
    
    // Remove control characters except newlines and tabs
    str = str.replace(/[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]/g, '');
    
    // Trim whitespace
    str = str.trim();
    
    // Limit length
    const maxLength = 10000;
    if (str.length > maxLength) {
        str = str.substring(0, maxLength);
    }
    
    return str;
}

/**
 * Sanitize object key
 */
function sanitizeKey(key) {
    if (typeof key !== 'string') {
        return null;
    }
    
    // Remove dangerous characters
    key = key.replace(/[^a-zA-Z0-9_-]/g, '');
    
    // Limit length
    const maxLength = 100;
    if (key.length > maxLength) {
        key = key.substring(0, maxLength);
    }
    
    return key || null;
}

/**
 * Custom validation rules
 */
const customValidators = {
    /**
     * Validate TSK content
     */
    isTSKContent: (value) => {
        if (typeof value !== 'string') {
            return false;
        }
        
        // Basic TSK syntax validation
        const tskPattern = /^[\w\s\.\-\+\*\/\(\)\[\]\{\}\=\:\,\;\|\&\|\!\<\>\?\@\#\$\%\^\&\*\(\)\_\+\-\=\[\]\{\}\\\|\;\'\:\"\,\.\/\<\>\?]+$/;
        return tskPattern.test(value) && value.length > 0 && value.length <= 100000;
    },
    
    /**
     * Validate configuration key
     */
    isConfigKey: (value) => {
        if (typeof value !== 'string') {
            return false;
        }
        
        // Configuration key pattern
        const keyPattern = /^[a-zA-Z][a-zA-Z0-9_\.]*$/;
        return keyPattern.test(value) && value.length <= 200;
    },
    
    /**
     * Validate database query
     */
    isDatabaseQuery: (value) => {
        if (typeof value !== 'string') {
            return false;
        }
        
        // Basic SQL injection prevention
        const dangerousPatterns = [
            /(\b(union|select|insert|update|delete|drop|create|alter|exec|execute|script)\b)/i,
            /(\b(union|select|insert|update|delete|drop|create|alter|exec|execute|script)\s+all\b)/i,
            /(\b(union|select|insert|update|delete|drop|create|alter|exec|execute|script)\s+distinct\b)/i,
            /(\b(union|select|insert|update|delete|drop|create|alter|exec|execute|script)\s+top\b)/i,
            /(\b(union|select|insert|update|delete|drop|create|alter|exec|execute|script)\s+limit\b)/i,
            /(\b(union|select|insert|update|delete|drop|create|alter|exec|execute|script)\s+offset\b)/i,
            /(\b(union|select|insert|update|delete|drop|create|alter|exec|execute|script)\s+fetch\b)/i,
            /(\b(union|select|insert|update|delete|drop|create|alter|exec|execute|script)\s+next\b)/i,
            /(\b(union|select|insert|update|delete|drop|create|alter|exec|execute|script)\s+first\b)/i,
            /(\b(union|select|insert|update|delete|drop|create|alter|exec|execute|script)\s+last\b)/i
        ];
        
        for (const pattern of dangerousPatterns) {
            if (pattern.test(value)) {
                return false;
            }
        }
        
        return value.length > 0 && value.length <= 10000;
    },
    
    /**
     * Validate email address
     */
    isEmail: (value) => {
        if (typeof value !== 'string') {
            return false;
        }
        
        const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailPattern.test(value) && value.length <= 254;
    },
    
    /**
     * Validate password strength
     */
    isStrongPassword: (value) => {
        if (typeof value !== 'string') {
            return false;
        }
        
        // At least 8 characters, 1 uppercase, 1 lowercase, 1 number, 1 special character
        const passwordPattern = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/;
        return passwordPattern.test(value) && value.length <= 128;
    },
    
    /**
     * Validate username
     */
    isUsername: (value) => {
        if (typeof value !== 'string') {
            return false;
        }
        
        const usernamePattern = /^[a-zA-Z0-9_-]{3,50}$/;
        return usernamePattern.test(value);
    }
};

/**
 * Rate limiting validation
 */
function validateRateLimit(req, res, next) {
    // This would integrate with the rate limiter
    // For now, just pass through
    next();
}

/**
 * Content type validation
 */
function validateContentType(allowedTypes = ['application/json']) {
    return (req, res, next) => {
        const contentType = req.get('Content-Type');
        
        if (!contentType) {
            return res.status(400).json({
                error: 'Content-Type header required',
                allowedTypes,
                timestamp: new Date().toISOString()
            });
        }
        
        const isValidType = allowedTypes.some(type => 
            contentType.includes(type)
        );
        
        if (!isValidType) {
            return res.status(415).json({
                error: 'Unsupported media type',
                receivedType: contentType,
                allowedTypes,
                timestamp: new Date().toISOString()
            });
        }
        
        next();
    };
}

module.exports = {
    requestValidator,
    handleValidationErrors,
    customValidators,
    validateRateLimit,
    validateContentType,
    sanitizeObject,
    sanitizeString,
    sanitizeKey
}; 