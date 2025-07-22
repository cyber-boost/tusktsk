/**
 * TuskLang JWT Manager
 * ====================
 * Comprehensive JWT token management with security features
 */

const jwt = require('jsonwebtoken');
const crypto = require('crypto');

class JWTManager {
    constructor(options = {}) {
        this.options = {
            secret: options.secret || process.env.JWT_SECRET || 'tusk-jwt-secret-key',
            expiresIn: options.expiresIn || '24h',
            algorithm: options.algorithm || 'HS256',
            issuer: options.issuer || 'tusk-web-server',
            audience: options.audience || 'tusk-users',
            clockTolerance: options.clockTolerance || 30, // 30 seconds
            ...options
        };

        this.blacklistedTokens = new Set();
        this.tokenMetadata = new Map();
        this.refreshTokens = new Map();
    }

    /**
     * Generate JWT token
     */
    generate(payload, options = {}) {
        const tokenOptions = {
            expiresIn: options.expiresIn || this.options.expiresIn,
            algorithm: options.algorithm || this.options.algorithm,
            issuer: options.issuer || this.options.issuer,
            audience: options.audience || this.options.audience,
            jwtid: this.generateTokenId(),
            ...options
        };

        const token = jwt.sign(payload, this.options.secret, tokenOptions);
        
        // Store token metadata
        this.tokenMetadata.set(token, {
            payload,
            issuedAt: Date.now(),
            expiresAt: this.getExpirationTime(tokenOptions.expiresIn),
            metadata: options.metadata || {}
        });

        return token;
    }

    /**
     * Verify JWT token
     */
    verify(token, options = {}) {
        try {
            // Check if token is blacklisted
            if (this.blacklistedTokens.has(token)) {
                throw new Error('Token has been blacklisted');
            }

            const verifyOptions = {
                algorithms: [options.algorithm || this.options.algorithm],
                issuer: options.issuer || this.options.issuer,
                audience: options.audience || this.options.audience,
                clockTolerance: options.clockTolerance || this.options.clockTolerance,
                ...options
            };

            const decoded = jwt.verify(token, this.options.secret, verifyOptions);
            
            // Update token metadata
            const metadata = this.tokenMetadata.get(token);
            if (metadata) {
                metadata.lastUsed = Date.now();
                metadata.useCount = (metadata.useCount || 0) + 1;
            }

            return decoded;
        } catch (error) {
            // Log verification failures
            this.logTokenEvent('verification_failed', {
                token: this.maskToken(token),
                error: error.message
            });
            throw error;
        }
    }

    /**
     * Decode JWT token without verification
     */
    decode(token) {
        try {
            return jwt.decode(token, { complete: true });
        } catch (error) {
            throw new Error('Invalid token format');
        }
    }

    /**
     * Refresh JWT token
     */
    refresh(token, options = {}) {
        try {
            const decoded = this.verify(token);
            
            // Create new token with same payload but new expiration
            const newToken = this.generate(decoded, options);
            
            // Blacklist old token
            this.blacklistToken(token);
            
            // Store refresh relationship
            this.refreshTokens.set(newToken, {
                originalToken: token,
                refreshedAt: Date.now()
            });

            this.logTokenEvent('token_refreshed', {
                originalToken: this.maskToken(token),
                newToken: this.maskToken(newToken)
            });

            return newToken;
        } catch (error) {
            throw new Error(`Token refresh failed: ${error.message}`);
        }
    }

    /**
     * Blacklist token
     */
    blacklistToken(token) {
        this.blacklistedTokens.add(token);
        
        // Remove from metadata
        this.tokenMetadata.delete(token);
        
        this.logTokenEvent('token_blacklisted', {
            token: this.maskToken(token)
        });
    }

    /**
     * Check if token is blacklisted
     */
    isBlacklisted(token) {
        return this.blacklistedTokens.has(token);
    }

    /**
     * Generate refresh token
     */
    generateRefreshToken(userId, options = {}) {
        const refreshToken = crypto.randomBytes(32).toString('hex');
        
        const refreshTokenData = {
            userId,
            token: refreshToken,
            createdAt: Date.now(),
            expiresAt: Date.now() + (options.expiresIn || 30 * 24 * 60 * 60 * 1000), // 30 days default
            metadata: options.metadata || {}
        };

        this.refreshTokens.set(refreshToken, refreshTokenData);
        
        return refreshToken;
    }

    /**
     * Validate refresh token
     */
    validateRefreshToken(refreshToken) {
        const tokenData = this.refreshTokens.get(refreshToken);
        
        if (!tokenData) {
            return null;
        }

        if (tokenData.expiresAt < Date.now()) {
            this.refreshTokens.delete(refreshToken);
            return null;
        }

        return tokenData;
    }

    /**
     * Revoke refresh token
     */
    revokeRefreshToken(refreshToken) {
        const tokenData = this.refreshTokens.get(refreshToken);
        if (tokenData) {
            this.refreshTokens.delete(refreshToken);
            this.logTokenEvent('refresh_token_revoked', {
                refreshToken: this.maskToken(refreshToken),
                userId: tokenData.userId
            });
        }
    }

    /**
     * Generate token ID
     */
    generateTokenId() {
        return crypto.randomBytes(16).toString('hex');
    }

    /**
     * Get expiration time from expiresIn string
     */
    getExpirationTime(expiresIn) {
        const now = Date.now();
        
        if (typeof expiresIn === 'number') {
            return now + expiresIn * 1000;
        }
        
        if (typeof expiresIn === 'string') {
            const match = expiresIn.match(/^(\d+)([smhd])$/);
            if (match) {
                const value = parseInt(match[1]);
                const unit = match[2];
                
                const multipliers = {
                    's': 1000,
                    'm': 60 * 1000,
                    'h': 60 * 60 * 1000,
                    'd': 24 * 60 * 60 * 1000
                };
                
                return now + value * multipliers[unit];
            }
        }
        
        // Default to 24 hours
        return now + 24 * 60 * 60 * 1000;
    }

    /**
     * Get token metadata
     */
    getTokenMetadata(token) {
        return this.tokenMetadata.get(token);
    }

    /**
     * Get all active tokens for a user
     */
    getUserTokens(userId) {
        const userTokens = [];
        
        for (const [token, metadata] of this.tokenMetadata) {
            if (metadata.payload.userId === userId) {
                userTokens.push({
                    token: this.maskToken(token),
                    issuedAt: metadata.issuedAt,
                    expiresAt: metadata.expiresAt,
                    lastUsed: metadata.lastUsed,
                    useCount: metadata.useCount,
                    metadata: metadata.metadata
                });
            }
        }
        
        return userTokens;
    }

    /**
     * Revoke all tokens for a user
     */
    revokeUserTokens(userId) {
        const revokedTokens = [];
        
        for (const [token, metadata] of this.tokenMetadata) {
            if (metadata.payload.userId === userId) {
                this.blacklistToken(token);
                revokedTokens.push(this.maskToken(token));
            }
        }
        
        this.logTokenEvent('user_tokens_revoked', {
            userId,
            revokedCount: revokedTokens.length
        });
        
        return revokedTokens;
    }

    /**
     * Clean up expired tokens
     */
    cleanupExpiredTokens() {
        const now = Date.now();
        const expiredTokens = [];
        
        // Clean up expired tokens from metadata
        for (const [token, metadata] of this.tokenMetadata) {
            if (metadata.expiresAt < now) {
                expiredTokens.push(token);
                this.tokenMetadata.delete(token);
            }
        }
        
        // Clean up expired refresh tokens
        for (const [refreshToken, tokenData] of this.refreshTokens) {
            if (tokenData.expiresAt < now) {
                this.refreshTokens.delete(refreshToken);
            }
        }
        
        return expiredTokens.length;
    }

    /**
     * Get JWT statistics
     */
    getStats() {
        const now = Date.now();
        let activeTokens = 0;
        let expiredTokens = 0;
        
        for (const metadata of this.tokenMetadata.values()) {
            if (metadata.expiresAt > now) {
                activeTokens++;
            } else {
                expiredTokens++;
            }
        }
        
        return {
            totalTokens: this.tokenMetadata.size,
            activeTokens,
            expiredTokens,
            blacklistedTokens: this.blacklistedTokens.size,
            refreshTokens: this.refreshTokens.size
        };
    }

    /**
     * Mask token for logging (show only first and last 4 characters)
     */
    maskToken(token) {
        if (!token || token.length < 8) {
            return '***';
        }
        return token.substring(0, 4) + '***' + token.substring(token.length - 4);
    }

    /**
     * Log token events
     */
    logTokenEvent(event, data) {
        const logEntry = {
            event,
            timestamp: new Date().toISOString(),
            data
        };
        
        // Log to console in development
        if (process.env.NODE_ENV === 'development') {
            console.log('JWT Event:', logEntry);
        }
        
        // In production, you might want to send this to a logging service
    }

    /**
     * Validate token format
     */
    validateTokenFormat(token) {
        if (!token || typeof token !== 'string') {
            return false;
        }
        
        // Check if token has the correct format (header.payload.signature)
        const parts = token.split('.');
        return parts.length === 3;
    }

    /**
     * Get token expiration time
     */
    getTokenExpiration(token) {
        try {
            const decoded = this.decode(token);
            if (decoded && decoded.payload.exp) {
                return new Date(decoded.payload.exp * 1000);
            }
        } catch (error) {
            // Token is invalid
        }
        return null;
    }

    /**
     * Check if token is expired
     */
    isTokenExpired(token) {
        const expiration = this.getTokenExpiration(token);
        if (!expiration) {
            return true; // Consider invalid tokens as expired
        }
        return expiration < new Date();
    }

    /**
     * Get token payload without verification
     */
    getTokenPayload(token) {
        try {
            const decoded = this.decode(token);
            return decoded ? decoded.payload : null;
        } catch (error) {
            return null;
        }
    }

    /**
     * Clear all data (for testing)
     */
    clear() {
        this.blacklistedTokens.clear();
        this.tokenMetadata.clear();
        this.refreshTokens.clear();
    }
}

module.exports = JWTManager; 