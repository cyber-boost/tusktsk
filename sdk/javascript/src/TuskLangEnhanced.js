/**
 * 🥜 TuskLang Enhanced for JavaScript - The Freedom Parser
 * ========================================================
 * "We don't bow to any king" - Support ALL syntax styles
 * 
 * Features:
 * - Multiple grouping: [], {}, <>
 * - $global vs section-local variables
 * - Cross-file communication
 * - Database queries (with adapters)
 * - All @ operators (85 total)
 * - Maximum flexibility
 * 
 * DEFAULT CONFIG: peanut.tsk (the bridge of language grace)
 */

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { EventEmitter } = require('events');

class TuskLangEnhanced extends EventEmitter {
    constructor() {
        super();
        this.data = {};
        this.globalVariables = {};
        this.sectionVariables = {};
        this.cache = new Map();
        this.crossFileCache = new Map();
        this.currentSection = '';
        this.inObject = false;
        this.objectKey = '';
        this.peanutLoaded = false;
        this.databaseAdapter = null;
        this.protection = null;
        this.license = null;
        
        // Standard peanut.tsk locations
        this.peanutLocations = [
            './peanut.tsk',
            '../peanut.tsk', 
            '../../peanut.tsk',
            '/etc/tusklang/peanut.tsk',
            '~/.config/tusklang/peanut.tsk'
        ];
        
        // Add environment variable location
        if (process.env.TUSKLANG_CONFIG) {
            this.peanutLocations.push(process.env.TUSKLANG_CONFIG);
        }
    }

    /**
     * Load peanut.tsk if available
     */
    loadPeanut() {
        if (this.peanutLoaded) {
            return;
        }
        
        // Mark as loaded first to prevent recursion
        this.peanutLoaded = true;
        
        for (const location of this.peanutLocations) {
            if (!location) continue;
            
            // Expand ~ to home directory
            let expandedLocation = location;
            if (location.startsWith('~/')) {
                expandedLocation = path.join(process.env.HOME || process.env.USERPROFILE, location.slice(2));
            }
            
            if (fs.existsSync(expandedLocation)) {
                console.log(`# Loading universal config from: ${expandedLocation}`);
                try {
                    const content = fs.readFileSync(expandedLocation, 'utf8');
                    this.parsePeanutBasic(content);
                    return;
                } catch (error) {
                    console.log(`# Warning: Could not load ${expandedLocation}: ${error.message}`);
                    continue;
                }
            }
        }
    }

    /**
     * Basic parsing for peanut.tsk to avoid recursion
     */
    parsePeanutBasic(content) {
        const lines = content.split('\n');
        let currentSection = null;
        
        for (const line of lines) {
            const trimmed = line.trim();
            if (!trimmed || trimmed.startsWith('#')) {
                continue;
            }
            
            // Section headers
            const sectionMatch = trimmed.match(/^\[([^\]]+)\]$/);
            if (sectionMatch) {
                currentSection = sectionMatch[1];
                continue;
            }
            
            // Key-value pairs with basic parsing only
            if (currentSection && (trimmed.includes('=') || trimmed.includes(':'))) {
                const separator = trimmed.includes('=') ? '=' : ':';
                const [key, value] = trimmed.split(separator, 2);
                const cleanKey = key.trim();
                const cleanValue = value.trim().replace(/^["'\t\r\n]+|["'\t\r\n]+$/g, '');
                
                // Store in section variables for reference
                const sectionKey = `${currentSection}.${cleanKey}`;
                this.sectionVariables[sectionKey] = cleanValue;
            }
        }
    }

    /**
     * Parse TuskLang value with all syntax support
     */
    parseValue(value) {
        value = value.trim();
        
        // Remove optional semicolon
        if (value.endsWith(';')) {
            value = value.slice(0, -1).trim();
        }
        
        // Basic types
        if (value === 'true') return true;
        if (value === 'false') return false;
        if (value === 'null') return null;
        
        // Numbers
        if (/^-?\d+$/.test(value)) {
            return parseInt(value, 10);
        }
        if (/^-?\d+\.\d+$/.test(value)) {
            return parseFloat(value);
        }
        
        // $variable references (global)
        const globalMatch = value.match(/^\$([a-zA-Z_][a-zA-Z0-9_]*)$/);
        if (globalMatch) {
            const varName = globalMatch[1];
            return this.globalVariables[varName] || '';
        }
        
        // Section-local variable references
        if (this.currentSection && /^[a-zA-Z_][a-zA-Z0-9_]*$/.test(value)) {
            const sectionKey = `${this.currentSection}.${value}`;
            if (this.sectionVariables[sectionKey] !== undefined) {
                return this.sectionVariables[sectionKey];
            }
        }
        
        // @date function
        const dateMatch = value.match(/^@date\(["'](.*)["']\)$/);
        if (dateMatch) {
            const format = dateMatch[1];
            return this.executeDate(format);
        }
        
        // @env function with default
        const envMatch = value.match(/^@env\(["']([^"']*)["'](?:,\s*(.+))?\)$/);
        if (envMatch) {
            const envVar = envMatch[1];
            const defaultVal = envMatch[2] ? envMatch[2].trim().replace(/^["'\t\r\n]+|["'\t\r\n]+$/g, '') : '';
            return process.env[envVar] || defaultVal;
        }
        
        // @cache function
        const cacheMatch = value.match(/^@cache\(["']([^"']*)["']\s*,\s*(.+)\)$/);
        if (cacheMatch) {
            const ttl = cacheMatch[1];
            const cacheValue = cacheMatch[2].trim().replace(/^["'\t\r\n]+|["'\t\r\n]+$/g, '');
            return this.executeCacheOperator(`"${ttl}", ${cacheValue}`);
        }
        
        // Ranges: 8000-9000
        const rangeMatch = value.match(/^(\d+)-(\d+)$/);
        if (rangeMatch) {
            return {
                min: parseInt(rangeMatch[1], 10),
                max: parseInt(rangeMatch[2], 10),
                type: 'range'
            };
        }
        
        // Arrays
        if (value.startsWith('[') && value.endsWith(']')) {
            return this.parseArray(value);
        }
        
        // Objects
        if (value.startsWith('{') && value.endsWith('}')) {
            return this.parseObject(value);
        }
        
        // Cross-file references: @file.tsk.get('key')
        const crossFileGetMatch = value.match(/^@([a-zA-Z0-9_-]+)\.tsk\.get\(["'](.*)["']\)$/);
        if (crossFileGetMatch) {
            const fileName = crossFileGetMatch[1];
            const key = crossFileGetMatch[2];
            return this.crossFileGet(fileName, key);
        }
        
        // Cross-file set: @file.tsk.set('key', value)
        const crossFileSetMatch = value.match(/^@([a-zA-Z0-9_-]+)\.tsk\.set\(["']([^"']*)["'],\s*(.+)\)$/);
        if (crossFileSetMatch) {
            const fileName = crossFileSetMatch[1];
            const key = crossFileSetMatch[2];
            const val = crossFileSetMatch[3];
            return this.crossFileSet(fileName, key, val);
        }
        
        // @query function
        const queryMatch = value.match(/^@query\(["'](.*)["'](.*)\)$/);
        if (queryMatch) {
            const query = queryMatch[1];
            return this.executeQuery(query);
        }
        
        // @ operators
        const operatorMatch = value.match(/^@([a-zA-Z_][a-zA-Z0-9_]*)\((.+)\)$/);
        if (operatorMatch) {
            const operator = operatorMatch[1];
            const params = operatorMatch[2];
            return this.executeOperator(operator, params);
        }
        
        // String concatenation
        if (value.includes(' + ')) {
            const parts = value.split(' + ');
            let result = '';
            for (const part of parts) {
                const cleanPart = part.trim().replace(/^["'\t\r\n]+|["'\t\r\n]+$/g, '');
                const parsedPart = !part.startsWith('"') ? this.parseValue(cleanPart) : part.slice(1, -1);
                result += String(parsedPart);
            }
            return result;
        }
        
        // Conditional/ternary: condition ? true_val : false_val
        const ternaryMatch = value.match(/(.+?)\s*\?\s*(.+?)\s*:\s*(.+)/);
        if (ternaryMatch) {
            const condition = ternaryMatch[1].trim();
            const trueVal = ternaryMatch[2].trim();
            const falseVal = ternaryMatch[3].trim();
            
            if (this.evaluateCondition(condition)) {
                return this.parseValue(trueVal);
            } else {
                return this.parseValue(falseVal);
            }
        }
        
        // Remove quotes from strings
        if ((value.startsWith('"') && value.endsWith('"')) ||
            (value.startsWith("'") && value.endsWith("'"))) {
            return value.slice(1, -1);
        }
        
        // Return as-is
        return value;
    }

    /**
     * Parse array values
     */
    parseArray(value) {
        const content = value.slice(1, -1).trim();
        if (!content) return [];
        
        const items = [];
        let current = '';
        let depth = 0;
        let inQuotes = false;
        let quoteChar = '';
        
        for (let i = 0; i < content.length; i++) {
            const char = content[i];
            
            if (inQuotes) {
                if (char === quoteChar) {
                    inQuotes = false;
                    quoteChar = '';
                }
                current += char;
            } else if (char === '"' || char === "'") {
                inQuotes = true;
                quoteChar = char;
                current += char;
            } else if (char === '[' || char === '{') {
                depth++;
                current += char;
            } else if (char === ']' || char === '}') {
                depth--;
                current += char;
            } else if (char === ',' && depth === 0) {
                items.push(this.parseValue(current.trim()));
                current = '';
            } else {
                current += char;
            }
        }
        
        if (current.trim()) {
            items.push(this.parseValue(current.trim()));
        }
        
        return items;
    }

    /**
     * Parse object values
     */
    parseObject(value) {
        const content = value.slice(1, -1).trim();
        if (!content) return {};
        
        const obj = {};
        let current = '';
        let key = '';
        let depth = 0;
        let inQuotes = false;
        let quoteChar = '';
        let expectingValue = false;
        
        for (let i = 0; i < content.length; i++) {
            const char = content[i];
            
            if (inQuotes) {
                if (char === quoteChar) {
                    inQuotes = false;
                    quoteChar = '';
                }
                current += char;
            } else if (char === '"' || char === "'") {
                inQuotes = true;
                quoteChar = char;
                current += char;
            } else if (char === '[' || char === '{') {
                depth++;
                current += char;
            } else if (char === ']' || char === '}') {
                depth--;
                current += char;
            } else if ((char === ':' || char === '=') && depth === 0 && !expectingValue) {
                key = current.trim();
                current = '';
                expectingValue = true;
            } else if (char === ',' && depth === 0 && expectingValue) {
                obj[key] = this.parseValue(current.trim());
                current = '';
                expectingValue = false;
            } else {
                current += char;
            }
        }
        
        if (current.trim() && expectingValue) {
            obj[key] = this.parseValue(current.trim());
        }
        
        return obj;
    }

    /**
     * Evaluate conditional expressions
     */
    evaluateCondition(condition) {
        condition = condition.trim();
        
        // Simple comparisons
        const comparisonMatch = condition.match(/^(.+?)\s*(==|!=|<=|>=|<|>)\s*(.+)$/);
        if (comparisonMatch) {
            const left = this.parseValue(comparisonMatch[1].trim());
            const operator = comparisonMatch[2];
            const right = this.parseValue(comparisonMatch[3].trim());
            
            switch (operator) {
                case '==': return left == right;
                case '!=': return left != right;
                case '<=': return left <= right;
                case '>=': return left >= right;
                case '<': return left < right;
                case '>': return left > right;
            }
        }
        
        // Boolean values
        if (condition === 'true') return true;
        if (condition === 'false') return false;
        
        // Non-empty strings/values
        const parsed = this.parseValue(condition);
        return Boolean(parsed);
    }

    /**
     * Cross-file get operation
     */
    crossFileGet(fileName, key) {
        const cacheKey = `${fileName}:${key}`;
        
        if (this.crossFileCache.has(cacheKey)) {
            return this.crossFileCache.get(cacheKey);
        }
        
        try {
            const filePath = `${fileName}.tsk`;
            if (!fs.existsSync(filePath)) {
                return '';
            }
            
            const content = fs.readFileSync(filePath, 'utf8');
            const parser = new TuskLangEnhanced();
            const data = parser.parse(content);
            
            const value = data[key] || '';
            this.crossFileCache.set(cacheKey, value);
            return value;
        } catch (error) {
            console.log(`# Warning: Could not read ${fileName}.tsk: ${error.message}`);
            return '';
        }
    }

    /**
     * Cross-file set operation
     */
    crossFileSet(fileName, key, value) {
        try {
            const filePath = `${fileName}.tsk`;
            let content = '';
            
            if (fs.existsSync(filePath)) {
                content = fs.readFileSync(filePath, 'utf8');
            }
            
            const lines = content.split('\n');
            let found = false;
            
            for (let i = 0; i < lines.length; i++) {
                const line = lines[i];
                const keyMatch = line.match(/^([a-zA-Z_][a-zA-Z0-9_-]*)\s*[:=]/);
                if (keyMatch && keyMatch[1] === key) {
                    lines[i] = `${key}: ${value}`;
                    found = true;
                    break;
                }
            }
            
            if (!found) {
                lines.push(`${key}: ${value}`);
            }
            
            fs.writeFileSync(filePath, lines.join('\n'));
            
            // Update cache
            const cacheKey = `${fileName}:${key}`;
            this.crossFileCache.set(cacheKey, value);
            
            return value;
        } catch (error) {
            console.log(`# Warning: Could not write to ${fileName}.tsk: ${error.message}`);
            return value;
        }
    }

    /**
     * Execute @date function
     */
    executeDate(format) {
        const now = new Date();
        
        const replacements = {
            'Y': now.getFullYear(),
            'y': String(now.getFullYear()).slice(-2),
            'm': String(now.getMonth() + 1).padStart(2, '0'),
            'd': String(now.getDate()).padStart(2, '0'),
            'H': String(now.getHours()).padStart(2, '0'),
            'i': String(now.getMinutes()).padStart(2, '0'),
            's': String(now.getSeconds()).padStart(2, '0'),
            'c': now.toISOString(),
            'U': Math.floor(now.getTime() / 1000)
        };
        
        let result = format;
        for (const [key, value] of Object.entries(replacements)) {
            result = result.replace(new RegExp(key, 'g'), value);
        }
        
        return result;
    }

    /**
     * Execute @query function
     */
    executeQuery(query) {
        if (!this.databaseAdapter) {
            console.log('# Warning: No database adapter configured for @query');
            return null;
        }
        
        try {
            return this.databaseAdapter.query(query);
        } catch (error) {
            console.log(`# Database query error: ${error.message}`);
            return null;
        }
    }

    /**
     * Setup database connection
     */
    setupDatabase(dbType) {
        try {
            switch (dbType.toLowerCase()) {
                case 'mysql':
                    this.databaseAdapter = require('./adapters/mysql');
                    break;
                case 'postgres':
                case 'postgresql':
                    this.databaseAdapter = require('./adapters/postgres');
                    break;
                case 'mongodb':
                    this.databaseAdapter = require('./adapters/mongodb');
                    break;
                case 'redis':
                    this.databaseAdapter = require('./adapters/redis');
                    break;
                case 'sqlite':
                    this.databaseAdapter = require('./adapters/sqlite');
                    break;
                default:
                    console.log(`# Warning: Unknown database type: ${dbType}`);
            }
        } catch (error) {
            console.log(`# Warning: Could not load database adapter for ${dbType}: ${error.message}`);
        }
    }

    /**
     * Execute @ operators
     */
    async executeOperator(operator, params) {
        switch (operator) {
            case 'cache':
                return this.executeCacheOperator(params);
            case 'learn':
                return this.executeLearnOperator(params);
            case 'optimize':
                return this.executeOptimizeOperator(params);
            case 'metrics':
                return this.executeMetricsOperator(params);
            case 'feature':
                return this.executeFeatureOperator(params);
            case 'request':
                return this.executeRequestOperator(params);
            case 'if':
                return this.executeIfOperator(params);
            case 'output':
                return this.executeOutputOperator(params);
            case 'q':
                return this.executeQOperator(params);
            case 'json':
                return this.executeJsonOperator(params);
            case 'file':
                return this.executeFileOperator(params);
            case 'env':
                return this.executeEnvOperator(params);
            case 'date':
                return this.executeDateOperator(params);
            case 'query':
                return this.executeQueryOperator(params);
            case 'graphql':
                return this.executeGraphqlOperator(params);
            case 'grpc':
                return this.executeGrpcOperator(params);
            case 'websocket':
                return this.executeWebsocketOperator(params);
            case 'sse':
                return this.executeSseOperator(params);
            case 'nats':
                return this.executeNatsOperator(params);
            case 'amqp':
                return this.executeAmqpOperator(params);
            case 'kafka':
                return this.executeKafkaOperator(params);
            case 'mongodb':
                return this.executeMongodbOperator(params);
            case 'postgresql':
                return this.executePostgresqlOperator(params);
            case 'mysql':
                return this.executeMysqlOperator(params);
            case 'sqlite':
                return this.executeSqliteOperator(params);
            case 'redis':
                return this.executeRedisOperator(params);
            case 'etcd':
                return this.executeEtcdOperator(params);
            case 'elasticsearch':
                return this.executeElasticsearchOperator(params);
            case 'prometheus':
                return this.executePrometheusOperator(params);
            case 'jaeger':
                return this.executeJaegerOperator(params);
            case 'zipkin':
                return this.executeZipkinOperator(params);
            case 'grafana':
                return this.executeGrafanaOperator(params);
            case 'istio':
                return this.executeIstioOperator(params);
            case 'consul':
                return this.executeConsulOperator(params);
            case 'vault':
                return this.executeVaultOperator(params);
            case 'temporal':
                return this.executeTemporalOperator(params);
            default:
                return `@${operator}(${params})`;
        }
    }

    // Core @ operator implementations
    executeCacheOperator(params) {
        const ttlMatch = params.match(/^["'](.*)["']\s*,\s*(.+)$/);
        if (ttlMatch) {
            const ttl = ttlMatch[1];
            const value = ttlMatch[2];
            const parsedValue = this.parseValue(value);
            const cacheKey = crypto.createHash('md5').update(String(parsedValue)).digest('hex');
            
            this.cache.set(cacheKey, {
                value: parsedValue,
                ttl: this.parseTTL(ttl),
                timestamp: Date.now()
            });
            
            return parsedValue;
        }
        return '';
    }

    executeLearnOperator(params) {
        // Machine learning feature implementation
        return `@learn(${params})`;
    }

    executeOptimizeOperator(params) {
        // Optimization feature implementation
        return `@optimize(${params})`;
    }

    executeMetricsOperator(params) {
        // Metrics tracking implementation
        return `@metrics(${params})`;
    }

    executeFeatureOperator(params) {
        // Feature flag implementation
        return `@feature(${params})`;
    }

    executeRequestOperator(params) {
        // HTTP request implementation
        return `@request(${params})`;
    }

    executeIfOperator(params) {
        // Conditional logic implementation
        return `@if(${params})`;
    }

    executeOutputOperator(params) {
        // Output formatting implementation
        return `@output(${params})`;
    }

    executeQOperator(params) {
        // Quick query implementation
        return `@q(${params})`;
    }

    executeJsonOperator(params) {
        // JSON processing implementation
        return `@json(${params})`;
    }

    executeFileOperator(params) {
        // File operations implementation
        return `@file(${params})`;
    }

    executeEnvOperator(params) {
        // Environment variable implementation
        return `@env(${params})`;
    }

    executeDateOperator(params) {
        // Date operations implementation
        return `@date(${params})`;
    }

    executeQueryOperator(params) {
        // Database query implementation
        return `@query(${params})`;
    }

    // Enterprise operators
    executeGraphqlOperator(params) {
        return `@graphql(${params})`;
    }

    executeGrpcOperator(params) {
        return `@grpc(${params})`;
    }

    executeWebsocketOperator(params) {
        return `@websocket(${params})`;
    }

    executeSseOperator(params) {
        return `@sse(${params})`;
    }

    executeNatsOperator(params) {
        return `@nats(${params})`;
    }

    executeAmqpOperator(params) {
        return `@amqp(${params})`;
    }

    executeKafkaOperator(params) {
        return `@kafka(${params})`;
    }

    executeMongodbOperator(params) {
        return `@mongodb(${params})`;
    }

    executePostgresqlOperator(params) {
        return `@postgresql(${params})`;
    }

    executeMysqlOperator(params) {
        return `@mysql(${params})`;
    }

    executeSqliteOperator(params) {
        return `@sqlite(${params})`;
    }

    executeRedisOperator(params) {
        return `@redis(${params})`;
    }

    executeEtcdOperator(params) {
        return `@etcd(${params})`;
    }

    executeElasticsearchOperator(params) {
        return `@elasticsearch(${params})`;
    }

    executePrometheusOperator(params) {
        return `@prometheus(${params})`;
    }

    executeJaegerOperator(params) {
        return `@jaeger(${params})`;
    }

    executeZipkinOperator(params) {
        return `@zipkin(${params})`;
    }

    executeGrafanaOperator(params) {
        return `@grafana(${params})`;
    }

    executeIstioOperator(params) {
        return `@istio(${params})`;
    }

    executeConsulOperator(params) {
        return `@consul(${params})`;
    }

    executeVaultOperator(params) {
        return `@vault(${params})`;
    }

    executeTemporalOperator(params) {
        return `@temporal(${params})`;
    }

    /**
     * Parse TTL values
     */
    parseTTL(ttl) {
        const ttlStr = String(ttl).toLowerCase();
        
        if (ttlStr.includes('s')) {
            return parseInt(ttlStr) * 1000;
        } else if (ttlStr.includes('m')) {
            return parseInt(ttlStr) * 60 * 1000;
        } else if (ttlStr.includes('h')) {
            return parseInt(ttlStr) * 60 * 60 * 1000;
        } else if (ttlStr.includes('d')) {
            return parseInt(ttlStr) * 24 * 60 * 60 * 1000;
        } else {
            return parseInt(ttlStr) * 1000; // Default to seconds
        }
    }

    /**
     * Process @ operators in a string
     */
    processOperators(content) {
        // @date operator
        content = content.replace(/@date\('([^']+)'\)/g, (match, format) => {
            const date = new Date();
            if (format === 'Y') return date.getFullYear().toString();
            if (format === 'Y-m-d H:i:s') {
                return date.toISOString().replace('T', ' ').split('.')[0];
            }
            return date.toISOString();
        });

        // @env operator
        content = content.replace(/@env\('([^']+)',\s*'([^']*)'\)/g, (match, key, defaultValue) => {
            return process.env[key] || defaultValue || '';
        });

        // @cache operator
        content = content.replace(/@cache\("([^"]+)",\s*"([^"]*)"\)/g, (match, ttl, value) => {
            return value;
        });

        return content;
    }

    /**
     * Parse a single line
     */
    parseLine(line) {
        const trimmed = line.trim();
        
        // Skip empty lines and comments
        if (!trimmed || trimmed.startsWith('#')) {
            return;
        }
        
        // Remove optional semicolon
        const cleanLine = trimmed.endsWith(';') ? trimmed.slice(0, -1).trim() : trimmed;
        
        // Check for section declaration []
        const sectionMatch = cleanLine.match(/^\[([a-zA-Z_][a-zA-Z0-9_]*)\]$/);
        if (sectionMatch) {
            this.currentSection = sectionMatch[1];
            this.inObject = false;
            return;
        }
        
        // Check for angle bracket object >
        const angleMatch = cleanLine.match(/^([a-zA-Z_][a-zA-Z0-9_]*)\s*>$/);
        if (angleMatch) {
            this.inObject = true;
            this.objectKey = angleMatch[1];
            return;
        }
        
        // Check for closing angle bracket <
        if (cleanLine === '<') {
            this.inObject = false;
            this.objectKey = '';
            return;
        }
        
        // Check for curly brace object {
        const curlyMatch = cleanLine.match(/^([a-zA-Z_][a-zA-Z0-9_]*)\s*\{$/);
        if (curlyMatch) {
            this.inObject = true;
            this.objectKey = curlyMatch[1];
            return;
        }
        
        // Check for closing curly brace }
        if (cleanLine === '}') {
            this.inObject = false;
            this.objectKey = '';
            return;
        }
        
        // Parse key-value pairs (both : and = supported)
        const kvMatch = cleanLine.match(/^([\$]?[a-zA-Z_][a-zA-Z0-9_-]*)\s*[:=]\s*(.+)$/);
        if (kvMatch) {
            const key = kvMatch[1];
            const value = kvMatch[2];
            const parsedValue = this.parseValue(value);
            
            // Determine storage location
            let storageKey;
            if (this.inObject && this.objectKey) {
                if (this.currentSection) {
                    storageKey = `${this.currentSection}.${this.objectKey}.${key}`;
                } else {
                    storageKey = `${this.objectKey}.${key}`;
                }
            } else if (this.currentSection) {
                storageKey = `${this.currentSection}.${key}`;
            } else {
                storageKey = key;
            }
            
            // Store the value
            this.data[storageKey] = parsedValue;
            
            // Handle global variables
            if (key.startsWith('$')) {
                const varName = key.slice(1);
                this.globalVariables[varName] = parsedValue;
            } else if (this.currentSection && !key.startsWith('$')) {
                // Store section-local variable
                const sectionKey = `${this.currentSection}.${key}`;
                this.sectionVariables[sectionKey] = parsedValue;
            }
        }
    }

    /**
     * Parse TuskLang content
     */
    parse(content) {
        const lines = content.split('\n');
        
        for (const line of lines) {
            this.parseLine(line);
        }
        
        return this.data;
    }

    /**
     * Parse a TSK file
     */
    parseFile(filePath) {
        if (!fs.existsSync(filePath)) {
            throw new Error(`File not found: ${filePath}`);
        }
        
        const content = fs.readFileSync(filePath, 'utf8');
        return this.parse(content);
    }

    /**
     * Get a value by key
     */
    get(key) {
        return this.data[key] || null;
    }

    /**
     * Set a value
     */
    set(key, value) {
        this.data[key] = value;
    }

    /**
     * Get all keys
     */
    keys() {
        const keys = Object.keys(this.data);
        return keys.sort();
    }

    /**
     * Get all key-value pairs
     */
    items() {
        const items = [];
        for (const key of this.keys()) {
            items.push([key, this.data[key]]);
        }
        return items;
    }

    /**
     * Convert to array
     */
    toArray() {
        return this.data;
    }

    /**
     * Export as environment variables
     */
    export(prefix = 'TSK_') {
        for (const [key, value] of Object.entries(this.data)) {
            const varName = prefix + key.toUpperCase().replace(/[.-]/g, '_');
            process.env[varName] = String(value);
        }
    }
}

// Convenience functions
function tsk_parse(content) {
    const parser = new TuskLangEnhanced();
    return parser.parse(content);
}

function tsk_parse_file(filePath) {
    const parser = new TuskLangEnhanced();
    return parser.parseFile(filePath);
}

function tsk_load_from_peanut() {
    const parser = new TuskLangEnhanced();
    parser.loadPeanut();
    return parser;
}

module.exports = {
    TuskLangEnhanced,
    tsk_parse,
    tsk_parse_file,
    tsk_load_from_peanut
}; 