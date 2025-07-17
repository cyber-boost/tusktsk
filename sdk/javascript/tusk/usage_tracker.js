/**
 * TuskLang SDK Usage Tracking Module
 * Enterprise-grade silent usage tracking for JavaScript SDK
 */

const crypto = require('crypto');
const https = require('https');
const http = require('http');

class UsageEvent {
    constructor(timestamp, sessionId, eventType, eventData, userId = null, licenseKeyHash = null) {
        this.timestamp = timestamp;
        this.session_id = sessionId;
        this.event_type = eventType;
        this.event_data = eventData;
        this.user_id = userId;
        this.license_key_hash = licenseKeyHash;
    }
}

class UsageMetrics {
    constructor() {
        this.total_events = 0;
        this.events_by_type = {};
        this.unique_users = 0;
        this.session_duration = 0;
        this.api_calls = 0;
        this.errors = 0;
        this.last_activity = Date.now();
    }
}

class TuskUsageTracker {
    constructor(apiKey, endpoint = 'https://api.tusklang.org/v1/usage') {
        this.apiKey = apiKey;
        this.endpoint = endpoint;
        this.sessionId = this.generateUUID();
        this.startTime = Date.now();
        
        // Event storage
        this.events = [];
        this.eventQueue = [];
        this.metrics = new UsageMetrics();
        
        // User tracking
        this.users = new Set();
        this.userSessions = new Map();
        
        // Configuration
        this.batchSize = 50;
        this.flushInterval = 300000; // 5 minutes
        this.maxRetries = 3;
        this.enabled = true;
        
        // Start background worker
        this.startWorker();
    }

    generateUUID() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
            const r = Math.random() * 16 | 0;
            const v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }

    startWorker() {
        setInterval(() => {
            this.processBatch();
        }, this.flushInterval);
    }

    async processBatch() {
        if (this.eventQueue.length === 0) return;

        const batch = this.eventQueue.splice(0, this.batchSize);
        await this.sendBatch(batch);
    }

    async sendBatch(events) {
        try {
            const batchData = {
                session_id: this.sessionId,
                timestamp: Date.now(),
                events: events,
                metrics: this.metrics
            };

            const signature = crypto
                .createHmac('sha256', this.apiKey)
                .update(JSON.stringify(batchData))
                .digest('hex');
            batchData.signature = signature;

            const url = new URL(this.endpoint);
            const options = {
                hostname: url.hostname,
                port: url.port || (url.protocol === 'https:' ? 443 : 80),
                path: url.pathname,
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${this.apiKey}`,
                    'Content-Type': 'application/json',
                    'Content-Length': Buffer.byteLength(JSON.stringify(batchData))
                }
            };

            return new Promise((resolve, reject) => {
                const client = url.protocol === 'https:' ? https : http;
                const req = client.request(options, (res) => {
                    let responseData = '';
                    res.on('data', (chunk) => {
                        responseData += chunk;
                    });
                    res.on('end', () => {
                        if (res.statusCode === 200) {
                            this.logEvent('batch_sent', { count: events.length, status: 'success' });
                            resolve(responseData);
                        } else {
                            this.logError(`Failed to send batch: ${res.statusCode}`);
                            reject(new Error(`HTTP ${res.statusCode}`));
                        }
                    });
                });

                req.on('error', (error) => {
                    this.logError(`Error sending batch: ${error.message}`);
                    reject(error);
                });

                req.setTimeout(10000, () => {
                    req.destroy();
                    reject(new Error('Request timeout'));
                });

                req.write(JSON.stringify(batchData));
                req.end();
            });
        } catch (error) {
            this.logError(`Error sending batch: ${error.message}`);
        }
    }

    trackEvent(eventType, eventData, userId = null) {
        if (!this.enabled) return;

        try {
            const event = new UsageEvent(
                Date.now(),
                this.sessionId,
                eventType,
                eventData,
                userId,
                this.hashLicenseKey()
            );

            // Add to storage
            this.events.push(event);
            this.eventQueue.push(event);

            // Update metrics
            this.metrics.total_events++;
            this.metrics.events_by_type[eventType] = (this.metrics.events_by_type[eventType] || 0) + 1;
            this.metrics.last_activity = Date.now();

            // Track user
            if (userId) {
                this.users.add(userId);
                if (!this.userSessions.has(userId)) {
                    this.userSessions.set(userId, []);
                }
                this.userSessions.get(userId).push(event);
                this.metrics.unique_users = this.users.size;
            }

            // Update session duration
            this.metrics.session_duration = Date.now() - this.startTime;

        } catch (error) {
            this.logError(`Error tracking event: ${error.message}`);
        }
    }

    trackApiCall(endpoint, method, statusCode, duration) {
        this.trackEvent('api_call', {
            endpoint: endpoint,
            method: method,
            status_code: statusCode,
            duration: duration
        });

        this.metrics.api_calls++;
        if (statusCode >= 400) {
            this.metrics.errors++;
        }
    }

    trackError(errorType, errorMessage, stackTrace = null) {
        this.trackEvent('error', {
            error_type: errorType,
            error_message: errorMessage,
            stack_trace: stackTrace
        });

        this.metrics.errors++;
    }

    trackFeatureUsage(feature, success, metadata = null) {
        const eventData = {
            feature: feature,
            success: success
        };
        if (metadata) {
            Object.assign(eventData, metadata);
        }

        this.trackEvent('feature_usage', eventData);
    }

    trackPerformance(operation, duration, memoryUsage = null) {
        const eventData = {
            operation: operation,
            duration: duration
        };
        if (memoryUsage) {
            eventData.memory_usage = memoryUsage;
        }

        this.trackEvent('performance', eventData);
    }

    trackSecurityEvent(eventType, severity, details) {
        this.trackEvent('security', {
            security_type: eventType,
            severity: severity,
            details: details
        });
    }

    getUsageSummary() {
        return {
            session_id: this.sessionId,
            start_time: this.startTime,
            current_time: Date.now(),
            session_duration: this.metrics.session_duration,
            total_events: this.metrics.total_events,
            events_by_type: this.metrics.events_by_type,
            unique_users: this.metrics.unique_users,
            api_calls: this.metrics.api_calls,
            errors: this.metrics.errors,
            last_activity: this.metrics.last_activity,
            queue_size: this.eventQueue.length,
            enabled: this.enabled
        };
    }

    getEventsByType(eventType, limit = 100) {
        const events = this.events.filter(event => event.event_type === eventType);
        return events.slice(-limit);
    }

    getUserEvents(userId, limit = 100) {
        const userEvents = this.userSessions.get(userId) || [];
        return userEvents.slice(-limit);
    }

    async flushEvents() {
        try {
            const batch = this.eventQueue.splice(0, this.batchSize);
            if (batch.length > 0) {
                await this.sendBatch(batch);
            }
        } catch (error) {
            this.logError(`Error flushing events: ${error.message}`);
        }
    }

    setEnabled(enabled) {
        this.enabled = enabled;
        this.trackEvent('tracking_toggle', { enabled: enabled });
    }

    hashLicenseKey() {
        // In real implementation, get license key from protection system
        return null;
    }

    logError(message) {
        // In production, use proper logging
        console.error(`TuskUsageTracker Error: ${message}`);
    }

    logEvent(eventType, data) {
        // For debugging purposes
    }

    shutdown() {
        this.enabled = false;
        this.flushEvents();
    }
}

// Global usage tracker instance
let usageTrackerInstance = null;

function initializeUsageTracker(apiKey, endpoint = null) {
    usageTrackerInstance = new TuskUsageTracker(apiKey, endpoint);
    return usageTrackerInstance;
}

function getUsageTracker() {
    if (!usageTrackerInstance) {
        throw new Error('Usage tracker not initialized. Call initializeUsageTracker() first.');
    }
    return usageTrackerInstance;
}

// Convenience functions
function trackEvent(eventType, eventData, userId = null) {
    try {
        const tracker = getUsageTracker();
        tracker.trackEvent(eventType, eventData, userId);
    } catch (error) {
        // Silently fail if tracker not available
    }
}

function trackApiCall(endpoint, method, statusCode, duration) {
    try {
        const tracker = getUsageTracker();
        tracker.trackApiCall(endpoint, method, statusCode, duration);
    } catch (error) {
        // Silently fail if tracker not available
    }
}

function trackError(errorType, errorMessage, stackTrace = null) {
    try {
        const tracker = getUsageTracker();
        tracker.trackError(errorType, errorMessage, stackTrace);
    } catch (error) {
        // Silently fail if tracker not available
    }
}

module.exports = {
    TuskUsageTracker,
    UsageEvent,
    UsageMetrics,
    initializeUsageTracker,
    getUsageTracker,
    trackEvent,
    trackApiCall,
    trackError
}; 