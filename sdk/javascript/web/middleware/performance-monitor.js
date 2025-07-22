/**
 * TuskLang Performance Monitoring Middleware
 * ==========================================
 * Comprehensive performance monitoring and metrics
 */

/**
 * Performance monitoring middleware
 */
function performanceMonitor(req, res, next) {
    const startTime = process.hrtime.bigint();
    const startMemory = process.memoryUsage();
    
    // Add performance tracking to request object
    req.performance = {
        startTime,
        startMemory,
        requestId: req.id || generateRequestId()
    };
    
    // Override res.end to capture response metrics
    const originalEnd = res.end;
    res.end = function(chunk, encoding) {
        const endTime = process.hrtime.bigint();
        const endMemory = process.memoryUsage();
        
        // Calculate performance metrics
        const duration = Number(endTime - startTime) / 1000000; // Convert to milliseconds
        const memoryDelta = {
            rss: endMemory.rss - startMemory.rss,
            heapUsed: endMemory.heapUsed - startMemory.heapUsed,
            heapTotal: endMemory.heapTotal - startMemory.heapTotal,
            external: endMemory.external - startMemory.external
        };
        
        // Log performance metrics
        logPerformanceMetrics({
            requestId: req.performance.requestId,
            method: req.method,
            url: req.url,
            statusCode: res.statusCode,
            duration,
            memoryDelta,
            timestamp: new Date().toISOString(),
            userAgent: req.get('User-Agent'),
            ip: req.ip || req.connection.remoteAddress
        });
        
        // Call original end method
        originalEnd.call(this, chunk, encoding);
    };
    
    next();
}

/**
 * Generate request ID for performance tracking
 */
function generateRequestId() {
    return 'perf_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
}

/**
 * Log performance metrics
 */
function logPerformanceMetrics(metrics) {
    // Log to console in development
    if (process.env.NODE_ENV === 'development') {
        console.log('Performance Metrics:', {
            requestId: metrics.requestId,
            method: metrics.method,
            url: metrics.url,
            statusCode: metrics.statusCode,
            duration: `${metrics.duration.toFixed(2)}ms`,
            memoryDelta: {
                rss: `${(metrics.memoryDelta.rss / 1024 / 1024).toFixed(2)}MB`,
                heapUsed: `${(metrics.memoryDelta.heapUsed / 1024 / 1024).toFixed(2)}MB`
            }
        });
    }
    
    // Store metrics for aggregation
    storePerformanceMetrics(metrics);
}

/**
 * Store performance metrics for aggregation
 */
const performanceMetrics = {
    requests: [],
    slowRequests: [],
    errors: [],
    memoryUsage: [],
    startTime: Date.now()
};

function storePerformanceMetrics(metrics) {
    // Add to requests array
    performanceMetrics.requests.push(metrics);
    
    // Keep only last 1000 requests
    if (performanceMetrics.requests.length > 1000) {
        performanceMetrics.requests.shift();
    }
    
    // Track slow requests (> 1000ms)
    if (metrics.duration > 1000) {
        performanceMetrics.slowRequests.push(metrics);
        
        // Keep only last 100 slow requests
        if (performanceMetrics.slowRequests.length > 100) {
            performanceMetrics.slowRequests.shift();
        }
    }
    
    // Track errors (4xx, 5xx status codes)
    if (metrics.statusCode >= 400) {
        performanceMetrics.errors.push(metrics);
        
        // Keep only last 100 errors
        if (performanceMetrics.errors.length > 100) {
            performanceMetrics.errors.shift();
        }
    }
    
    // Track memory usage
    performanceMetrics.memoryUsage.push({
        timestamp: metrics.timestamp,
        rss: process.memoryUsage().rss,
        heapUsed: process.memoryUsage().heapUsed,
        heapTotal: process.memoryUsage().heapTotal
    });
    
    // Keep only last 100 memory readings
    if (performanceMetrics.memoryUsage.length > 100) {
        performanceMetrics.memoryUsage.shift();
    }
}

/**
 * Get performance statistics
 */
function getPerformanceStats() {
    const now = Date.now();
    const uptime = now - performanceMetrics.startTime;
    
    if (performanceMetrics.requests.length === 0) {
        return {
            uptime,
            totalRequests: 0,
            averageResponseTime: 0,
            slowRequests: 0,
            errors: 0,
            memoryUsage: process.memoryUsage()
        };
    }
    
    const durations = performanceMetrics.requests.map(r => r.duration);
    const averageResponseTime = durations.reduce((a, b) => a + b, 0) / durations.length;
    const maxResponseTime = Math.max(...durations);
    const minResponseTime = Math.min(...durations);
    
    const statusCodes = performanceMetrics.requests.map(r => r.statusCode);
    const statusCodeCounts = statusCodes.reduce((acc, code) => {
        acc[code] = (acc[code] || 0) + 1;
        return acc;
    }, {});
    
    const recentRequests = performanceMetrics.requests.filter(r => 
        now - new Date(r.timestamp).getTime() < 60000 // Last minute
    );
    
    const requestsPerMinute = recentRequests.length;
    
    return {
        uptime,
        totalRequests: performanceMetrics.requests.length,
        averageResponseTime: Math.round(averageResponseTime * 100) / 100,
        maxResponseTime: Math.round(maxResponseTime * 100) / 100,
        minResponseTime: Math.round(minResponseTime * 100) / 100,
        slowRequests: performanceMetrics.slowRequests.length,
        errors: performanceMetrics.errors.length,
        statusCodeCounts,
        requestsPerMinute,
        memoryUsage: process.memoryUsage(),
        recentMemoryUsage: performanceMetrics.memoryUsage.slice(-10)
    };
}

/**
 * Get slow requests
 */
function getSlowRequests(limit = 10) {
    return performanceMetrics.slowRequests
        .sort((a, b) => b.duration - a.duration)
        .slice(0, limit);
}

/**
 * Get recent errors
 */
function getRecentErrors(limit = 10) {
    return performanceMetrics.errors
        .sort((a, b) => new Date(b.timestamp) - new Date(a.timestamp))
        .slice(0, limit);
}

/**
 * Clear performance metrics
 */
function clearPerformanceMetrics() {
    performanceMetrics.requests = [];
    performanceMetrics.slowRequests = [];
    performanceMetrics.errors = [];
    performanceMetrics.memoryUsage = [];
    performanceMetrics.startTime = Date.now();
}

/**
 * Performance alert middleware
 */
function performanceAlert(threshold = 1000) {
    return (req, res, next) => {
        const originalEnd = res.end;
        
        res.end = function(chunk, encoding) {
            const endTime = process.hrtime.bigint();
            const startTime = req.performance?.startTime || endTime;
            const duration = Number(endTime - startTime) / 1000000;
            
            if (duration > threshold) {
                console.warn(`⚠️  Slow request detected: ${req.method} ${req.url} took ${duration.toFixed(2)}ms`);
            }
            
            originalEnd.call(this, chunk, encoding);
        };
        
        next();
    };
}

/**
 * Memory monitoring middleware
 */
function memoryMonitor(req, res, next) {
    const memoryUsage = process.memoryUsage();
    const memoryThreshold = 500 * 1024 * 1024; // 500MB
    
    if (memoryUsage.heapUsed > memoryThreshold) {
        console.warn(`⚠️  High memory usage detected: ${(memoryUsage.heapUsed / 1024 / 1024).toFixed(2)}MB`);
    }
    
    next();
}

/**
 * CPU monitoring middleware
 */
function cpuMonitor(req, res, next) {
    const startUsage = process.cpuUsage();
    
    res.on('finish', () => {
        const endUsage = process.cpuUsage(startUsage);
        const cpuTime = (endUsage.user + endUsage.system) / 1000000; // Convert to seconds
        
        if (cpuTime > 1) { // More than 1 second of CPU time
            console.warn(`⚠️  High CPU usage detected: ${cpuTime.toFixed(2)}s for ${req.method} ${req.url}`);
        }
    });
    
    next();
}

/**
 * Response time header middleware
 */
function responseTimeHeader(req, res, next) {
    const startTime = process.hrtime.bigint();
    
    res.on('finish', () => {
        const endTime = process.hrtime.bigint();
        const duration = Number(endTime - startTime) / 1000000;
        res.setHeader('X-Response-Time', `${duration.toFixed(2)}ms`);
    });
    
    next();
}

module.exports = {
    performanceMonitor,
    getPerformanceStats,
    getSlowRequests,
    getRecentErrors,
    clearPerformanceMetrics,
    performanceAlert,
    memoryMonitor,
    cpuMonitor,
    responseTimeHeader
}; 