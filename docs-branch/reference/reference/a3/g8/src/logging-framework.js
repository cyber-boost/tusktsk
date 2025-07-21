/**
 * Advanced Logging and Debugging Framework with Structured Output
 * Goal 8.3 Implementation
 */

const EventEmitter = require('events');
const fs = require('fs').promises;
const path = require('path');

class LoggingFramework extends EventEmitter {
    constructor(options = {}) {
        super();
        this.logLevels = {
            TRACE: 0,
            DEBUG: 1,
            INFO: 2,
            WARN: 3,
            ERROR: 4,
            FATAL: 5
        };
        
        this.currentLevel = this.logLevels[options.level || 'INFO'];
        this.logFile = options.logFile || null;
        this.maxFileSize = options.maxFileSize || 10 * 1024 * 1024; // 10MB
        this.maxFiles = options.maxFiles || 5;
        this.structured = options.structured !== false;
        this.filters = new Map();
        this.transports = new Map();
        this.logBuffer = [];
        this.bufferSize = options.bufferSize || 100;
        this.flushInterval = options.flushInterval || 5000; // 5 seconds
        
        this.setupDefaultTransports();
        this.startFlushTimer();
    }

    /**
     * Log a message with specified level
     */
    log(level, message, data = {}, context = {}) {
        const levelNum = this.logLevels[level];
        if (levelNum < this.currentLevel) {
            return;
        }

        const logEntry = this.createLogEntry(level, message, data, context);
        
        // Apply filters
        if (!this.applyFilters(logEntry)) {
            return;
        }

        // Add to buffer
        this.logBuffer.push(logEntry);
        
        // Emit event
        this.emit('log', logEntry);
        
        // Flush if buffer is full
        if (this.logBuffer.length >= this.bufferSize) {
            this.flush();
        }

        // Immediate flush for high priority levels
        if (levelNum >= this.logLevels.ERROR) {
            this.flush();
        }
    }

    /**
     * Create structured log entry
     */
    createLogEntry(level, message, data, context) {
        const entry = {
            timestamp: new Date().toISOString(),
            level: level,
            levelNum: this.logLevels[level],
            message: message,
            data: data,
            context: {
                ...context,
                pid: process.pid,
                memory: process.memoryUsage(),
                uptime: process.uptime()
            },
            id: this.generateLogId()
        };

        if (this.structured) {
            entry.structured = true;
            entry.schema = '1.0.0';
        }

        return entry;
    }

    /**
     * Apply filters to log entry
     */
    applyFilters(logEntry) {
        for (const [filterName, filter] of this.filters) {
            try {
                if (!filter(logEntry)) {
                    return false;
                }
            } catch (error) {
                console.error(`Filter '${filterName}' failed:`, error.message);
            }
        }
        return true;
    }

    /**
     * Flush log buffer to transports
     */
    async flush() {
        if (this.logBuffer.length === 0) {
            return;
        }

        const entries = [...this.logBuffer];
        this.logBuffer = [];

        for (const [transportName, transport] of this.transports) {
            try {
                await transport.write(entries);
            } catch (error) {
                console.error(`Transport '${transportName}' failed:`, error.message);
            }
        }

        this.emit('flushed', { count: entries.length });
    }

    /**
     * Setup default transports
     */
    setupDefaultTransports() {
        // Console transport
        this.addTransport('console', {
            write: async (entries) => {
                for (const entry of entries) {
                    const formatted = this.formatLogEntry(entry);
                    console.log(formatted);
                }
            }
        });

        // File transport
        if (this.logFile) {
            this.addTransport('file', {
                write: async (entries) => {
                    await this.writeToFile(entries);
                }
            });
        }
    }

    /**
     * Add custom transport
     */
    addTransport(name, transport) {
        if (typeof transport.write !== 'function') {
            throw new Error('Transport must have a write function');
        }

        this.transports.set(name, transport);
        console.log(`✓ Transport added: ${name}`);
        this.emit('transportAdded', { name });
        
        return true;
    }

    /**
     * Write logs to file
     */
    async writeToFile(entries) {
        try {
            const logDir = path.dirname(this.logFile);
            await fs.mkdir(logDir, { recursive: true });

            const logContent = entries.map(entry => 
                JSON.stringify(entry)
            ).join('\n') + '\n';

            await fs.appendFile(this.logFile, logContent);

            // Check file size and rotate if needed
            await this.rotateLogFile();
        } catch (error) {
            throw new Error(`Failed to write to log file: ${error.message}`);
        }
    }

    /**
     * Rotate log file if it exceeds size limit
     */
    async rotateLogFile() {
        try {
            const stats = await fs.stat(this.logFile);
            if (stats.size < this.maxFileSize) {
                return;
            }

            // Rotate existing files
            for (let i = this.maxFiles - 1; i > 0; i--) {
                const oldFile = `${this.logFile}.${i}`;
                const newFile = `${this.logFile}.${i + 1}`;
                
                try {
                    await fs.rename(oldFile, newFile);
                } catch (error) {
                    // File doesn't exist, continue
                }
            }

            // Move current log file
            const backupFile = `${this.logFile}.1`;
            await fs.rename(this.logFile, backupFile);

            console.log(`✓ Log file rotated: ${this.logFile} → ${backupFile}`);
        } catch (error) {
            console.error(`Log rotation failed: ${error.message}`);
        }
    }

    /**
     * Format log entry for console output
     */
    formatLogEntry(entry) {
        const timestamp = entry.timestamp;
        const level = entry.level.padEnd(5);
        const message = entry.message;
        
        let formatted = `[${timestamp}] ${level} ${message}`;
        
        if (Object.keys(entry.data).length > 0) {
            formatted += ` | ${JSON.stringify(entry.data)}`;
        }
        
        if (Object.keys(entry.context).length > 0) {
            formatted += ` | ctx: ${JSON.stringify(entry.context)}`;
        }
        
        return formatted;
    }

    /**
     * Add filter function
     */
    addFilter(name, filterFunction) {
        if (typeof filterFunction !== 'function') {
            throw new Error('Filter must be a function');
        }

        this.filters.set(name, filterFunction);
        console.log(`✓ Filter added: ${name}`);
        this.emit('filterAdded', { name });
        
        return true;
    }

    /**
     * Set log level
     */
    setLevel(level) {
        if (!(level in this.logLevels)) {
            throw new Error(`Invalid log level: ${level}`);
        }

        this.currentLevel = this.logLevels[level];
        console.log(`✓ Log level set to: ${level}`);
        this.emit('levelChanged', { level });
        
        return true;
    }

    /**
     * Convenience methods for different log levels
     */
    trace(message, data = {}, context = {}) {
        this.log('TRACE', message, data, context);
    }

    debug(message, data = {}, context = {}) {
        this.log('DEBUG', message, data, context);
    }

    info(message, data = {}, context = {}) {
        this.log('INFO', message, data, context);
    }

    warn(message, data = {}, context = {}) {
        this.log('WARN', message, data, context);
    }

    error(message, data = {}, context = {}) {
        this.log('ERROR', message, data, context);
    }

    fatal(message, data = {}, context = {}) {
        this.log('FATAL', message, data, context);
    }

    /**
     * Start flush timer
     */
    startFlushTimer() {
        this.flushTimer = setInterval(() => {
            this.flush();
        }, this.flushInterval);
    }

    /**
     * Stop flush timer
     */
    stopFlushTimer() {
        if (this.flushTimer) {
            clearInterval(this.flushTimer);
            this.flushTimer = null;
        }
    }

    /**
     * Generate unique log ID
     */
    generateLogId() {
        return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    }

    /**
     * Get log statistics
     */
    getStats() {
        return {
            currentLevel: Object.keys(this.logLevels).find(key => 
                this.logLevels[key] === this.currentLevel
            ),
            transports: this.transports.size,
            filters: this.filters.size,
            bufferSize: this.logBuffer.length,
            structured: this.structured,
            logFile: this.logFile
        };
    }

    /**
     * Create child logger with context
     */
    child(context) {
        const childLogger = new LoggingFramework({
            level: Object.keys(this.logLevels).find(key => 
                this.logLevels[key] === this.currentLevel
            ),
            logFile: this.logFile,
            structured: this.structured
        });

        // Copy filters and transports
        for (const [name, filter] of this.filters) {
            childLogger.addFilter(name, filter);
        }

        for (const [name, transport] of this.transports) {
            childLogger.addTransport(name, transport);
        }

        // Add context to all log calls
        const originalLog = childLogger.log.bind(childLogger);
        childLogger.log = (level, message, data = {}, additionalContext = {}) => {
            originalLog(level, message, data, { ...context, ...additionalContext });
        };

        return childLogger;
    }

    /**
     * Close logger and flush remaining entries
     */
    async close() {
        this.stopFlushTimer();
        await this.flush();
        this.emit('closed');
    }
}

module.exports = { LoggingFramework }; 