class LoggingFramework {
    constructor(options = {}) {
        this.logLevels = {
            TRACE: 0,
            DEBUG: 1,
            INFO: 2,
            WARN: 3,
            ERROR: 4,
            FATAL: 5
        };
        
        this.currentLevel = this.logLevels[options.level || "INFO"];
        this.structured = options.structured !== false;
        this.transports = new Map();
        this.filters = new Map();
        this.logBuffer = [];
        this.bufferSize = options.bufferSize || 100;
        
        this.setupDefaultTransports();
    }

    log(level, message, data = {}, context = {}) {
        const levelNum = this.logLevels[level];
        if (levelNum < this.currentLevel) {
            return;
        }

        const logEntry = {
            timestamp: new Date().toISOString(),
            level: level,
            levelNum: levelNum,
            message: message,
            data: data,
            context: {
                ...context,
                pid: process.pid,
                uptime: process.uptime()
            },
            id: `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`
        };

        if (this.structured) {
            logEntry.structured = true;
            logEntry.schema = "1.0.0";
        }

        this.logBuffer.push(logEntry);
        
        // Apply filters
        if (!this.applyFilters(logEntry)) {
            return;
        }

        // Write to transports
        for (const [transportName, transport] of this.transports) {
            try {
                transport.write([logEntry]);
            } catch (error) {
                console.error(`Transport ${transportName} failed:`, error.message);
            }
        }
    }

    // Convenience methods
    trace(message, data = {}, context = {}) {
        this.log("TRACE", message, data, context);
    }

    debug(message, data = {}, context = {}) {
        this.log("DEBUG", message, data, context);
    }

    info(message, data = {}, context = {}) {
        this.log("INFO", message, data, context);
    }

    warn(message, data = {}, context = {}) {
        this.log("WARN", message, data, context);
    }

    error(message, data = {}, context = {}) {
        this.log("ERROR", message, data, context);
    }

    fatal(message, data = {}, context = {}) {
        this.log("FATAL", message, data, context);
    }

    applyFilters(logEntry) {
        for (const [filterName, filter] of this.filters) {
            try {
                if (!filter(logEntry)) {
                    return false;
                }
            } catch (error) {
                console.error(`Filter ${filterName} failed:`, error.message);
            }
        }
        return true;
    }

    setupDefaultTransports() {
        this.addTransport("console", {
            write: async (entries) => {
                for (const entry of entries) {
                    const timestamp = entry.timestamp;
                    const level = entry.level.padEnd(5);
                    const message = entry.message;
                    
                    let formatted = `[${timestamp}] ${level} ${message}`;
                    
                    if (Object.keys(entry.data).length > 0) {
                        formatted += ` | ${JSON.stringify(entry.data)}`;
                    }
                    
                    console.log(formatted);
                }
            }
        });
    }

    addTransport(name, transport) {
        if (typeof transport.write !== "function") {
            throw new Error("Transport must have a write function");
        }

        this.transports.set(name, transport);
        console.log(`✓ Transport added: ${name}`);
        return true;
    }

    addFilter(name, filterFunction) {
        if (typeof filterFunction !== "function") {
            throw new Error("Filter must be a function");
        }

        this.filters.set(name, filterFunction);
        console.log(`✓ Filter added: ${name}`);
        return true;
    }

    setLevel(level) {
        if (!(level in this.logLevels)) {
            throw new Error(`Invalid log level: ${level}`);
        }

        this.currentLevel = this.logLevels[level];
        console.log(`✓ Log level set to: ${level}`);
        return true;
    }

    createChildLogger(context) {
        const childLogger = new LoggingFramework({
            level: Object.keys(this.logLevels).find(key => 
                this.logLevels[key] === this.currentLevel
            ),
            structured: this.structured
        });

        for (const [name, filter] of this.filters) {
            childLogger.addFilter(name, filter);
        }

        for (const [name, transport] of this.transports) {
            childLogger.addTransport(name, transport);
        }

        const originalLog = childLogger.log.bind(childLogger);
        childLogger.log = (level, message, data = {}, additionalContext = {}) => {
            originalLog(level, message, data, { ...context, ...additionalContext });
        };

        return childLogger;
    }

    getStats() {
        return {
            currentLevel: Object.keys(this.logLevels).find(key => 
                this.logLevels[key] === this.currentLevel
            ),
            transports: this.transports.size,
            filters: this.filters.size,
            bufferSize: this.logBuffer.length,
            structured: this.structured
        };
    }
}

module.exports = { LoggingFramework };
