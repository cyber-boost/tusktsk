/**
 * FUJSEN - Function Serialization for JavaScript
 * Complete function serialization and deserialization system
 */

const crypto = require('crypto');
const vm = require('vm');

class FUJSEN {
    constructor() {
        this.serializedFunctions = new Map();
        this.contextCache = new Map();
        this.serializationVersion = '1.0.0';
    }

    /**
     * Serialize a function to a portable format
     */
    serializeFunction(func, options = {}) {
        try {
            const functionString = func.toString();
            const functionHash = this.generateFunctionHash(functionString);
            
            const serialized = {
                version: this.serializationVersion,
                hash: functionHash,
                source: functionString,
                name: func.name || 'anonymous',
                length: func.length,
                prototype: this.serializePrototype(func),
                metadata: {
                    timestamp: Date.now(),
                    environment: this.getEnvironmentInfo(),
                    dependencies: this.extractDependencies(functionString),
                    ...options.metadata
                }
            };

            // Store in cache
            this.serializedFunctions.set(functionHash, serialized);
            
            return {
                hash: functionHash,
                serialized: JSON.stringify(serialized),
                size: JSON.stringify(serialized).length
            };
        } catch (error) {
            throw new Error(`Function serialization failed: ${error.message}`);
        }
    }

    /**
     * Deserialize a function from serialized format
     */
    deserializeFunction(serializedData, context = {}) {
        try {
            const data = typeof serializedData === 'string' ? JSON.parse(serializedData) : serializedData;
            
            // Validate version
            if (data.version !== this.serializationVersion) {
                throw new Error(`Version mismatch: expected ${this.serializationVersion}, got ${data.version}`);
            }

            // Validate hash
            const expectedHash = this.generateFunctionHash(data.source);
            if (data.hash !== expectedHash) {
                throw new Error('Function hash validation failed - function may have been tampered with');
            }

            // Create execution context
            const executionContext = this.createExecutionContext(context);
            
            // Deserialize function
            const deserializedFunction = this.createFunctionFromSource(data.source, executionContext);
            
            // Restore prototype if available
            if (data.prototype) {
                this.restorePrototype(deserializedFunction, data.prototype);
            }

            return deserializedFunction;
        } catch (error) {
            throw new Error(`Function deserialization failed: ${error.message}`);
        }
    }

    /**
     * Generate hash for function source
     */
    generateFunctionHash(source) {
        return crypto.createHash('sha256').update(source).digest('hex');
    }

    /**
     * Serialize function prototype
     */
    serializePrototype(func) {
        try {
            const prototype = Object.getPrototypeOf(func);
            if (prototype === Function.prototype) {
                return null;
            }

            const prototypeData = {};
            for (const key of Object.getOwnPropertyNames(prototype)) {
                const descriptor = Object.getOwnPropertyDescriptor(prototype, key);
                if (descriptor && typeof descriptor.value === 'function') {
                    prototypeData[key] = this.serializeFunction(descriptor.value);
                }
            }

            return Object.keys(prototypeData).length > 0 ? prototypeData : null;
        } catch (error) {
            return null;
        }
    }

    /**
     * Restore function prototype
     */
    restorePrototype(func, prototypeData) {
        try {
            if (!prototypeData) return;

            const prototype = {};
            for (const [key, serializedFunc] of Object.entries(prototypeData)) {
                const deserializedFunc = this.deserializeFunction(serializedFunc);
                prototype[key] = deserializedFunc;
            }

            Object.setPrototypeOf(func, Object.create(Function.prototype, prototype));
        } catch (error) {
            console.warn('Failed to restore prototype:', error.message);
        }
    }

    /**
     * Create execution context
     */
    createExecutionContext(context = {}) {
        const baseContext = {
            console: console,
            Buffer: Buffer,
            process: process,
            global: global,
            ...context
        };

        // Create a sandboxed context
        const sandbox = vm.createContext(baseContext);
        return sandbox;
    }

    /**
     * Create function from source code
     */
    createFunctionFromSource(source, context) {
        try {
            // Use vm.runInContext for safer execution
            const script = new vm.Script(source);
            const result = script.runInContext(context, { timeout: 5000 });
            
            // If the result is a function, return it
            if (typeof result === 'function') {
                return result;
            }

            // Otherwise, try to create a function from the source
            return vm.runInContext(`(${source})`, context, { timeout: 5000 });
        } catch (error) {
            // Fallback to eval (less secure but more compatible)
            return eval(`(${source})`);
        }
    }

    /**
     * Extract dependencies from function source
     */
    extractDependencies(source) {
        const dependencies = new Set();
        
        // Extract require statements
        const requireMatches = source.match(/require\(['"`]([^'"`]+)['"`]\)/g);
        if (requireMatches) {
            requireMatches.forEach(match => {
                const module = match.match(/require\(['"`]([^'"`]+)['"`]\)/)[1];
                dependencies.add(module);
            });
        }

        // Extract import statements
        const importMatches = source.match(/import\s+.*?from\s+['"`]([^'"`]+)['"`]/g);
        if (importMatches) {
            importMatches.forEach(match => {
                const module = match.match(/from\s+['"`]([^'"`]+)['"`]/)[1];
                dependencies.add(module);
            });
        }

        return Array.from(dependencies);
    }

    /**
     * Get environment information
     */
    getEnvironmentInfo() {
        return {
            nodeVersion: process.version,
            platform: process.platform,
            arch: process.arch,
            timestamp: Date.now()
        };
    }

    /**
     * Serialize multiple functions
     */
    serializeFunctions(functions, options = {}) {
        const results = {};
        
        for (const [name, func] of Object.entries(functions)) {
            try {
                results[name] = this.serializeFunction(func, options);
            } catch (error) {
                results[name] = { error: error.message };
            }
        }
        
        return results;
    }

    /**
     * Deserialize multiple functions
     */
    deserializeFunctions(serializedFunctions, context = {}) {
        const results = {};
        
        for (const [name, serialized] of Object.entries(serializedFunctions)) {
            try {
                if (serialized.error) {
                    results[name] = null;
                } else {
                    results[name] = this.deserializeFunction(serialized, context);
                }
            } catch (error) {
                results[name] = null;
                console.error(`Failed to deserialize function ${name}:`, error.message);
            }
        }
        
        return results;
    }

    /**
     * Validate serialized function
     */
    validateSerializedFunction(serializedData) {
        try {
            const data = typeof serializedData === 'string' ? JSON.parse(serializedData) : serializedData;
            
            // Check required fields
            const requiredFields = ['version', 'hash', 'source', 'name', 'length', 'metadata'];
            for (const field of requiredFields) {
                if (!(field in data)) {
                    return { valid: false, error: `Missing required field: ${field}` };
                }
            }

            // Validate hash
            const expectedHash = this.generateFunctionHash(data.source);
            if (data.hash !== expectedHash) {
                return { valid: false, error: 'Hash validation failed' };
            }

            // Validate source code
            if (typeof data.source !== 'string' || data.source.trim() === '') {
                return { valid: false, error: 'Invalid source code' };
            }

            return { valid: true };
        } catch (error) {
            return { valid: false, error: error.message };
        }
    }

    /**
     * Compress serialized function
     */
    compressSerializedFunction(serializedData) {
        try {
            const data = typeof serializedData === 'string' ? serializedData : JSON.stringify(serializedData);
            
            // Simple compression (in production, use zlib)
            const compressed = Buffer.from(data).toString('base64');
            
            return {
                compressed,
                originalSize: data.length,
                compressedSize: compressed.length,
                compressionRatio: (compressed.length / data.length * 100).toFixed(2) + '%'
            };
        } catch (error) {
            throw new Error(`Compression failed: ${error.message}`);
        }
    }

    /**
     * Decompress serialized function
     */
    decompressSerializedFunction(compressedData) {
        try {
            const decompressed = Buffer.from(compressedData, 'base64').toString('utf8');
            return JSON.parse(decompressed);
        } catch (error) {
            throw new Error(`Decompression failed: ${error.message}`);
        }
    }

    /**
     * Create function bundle
     */
    createBundle(functions, options = {}) {
        const bundle = {
            version: this.serializationVersion,
            timestamp: Date.now(),
            functions: {},
            metadata: {
                totalFunctions: Object.keys(functions).length,
                environment: this.getEnvironmentInfo(),
                ...options.metadata
            }
        };

        for (const [name, func] of Object.entries(functions)) {
            try {
                bundle.functions[name] = this.serializeFunction(func, options);
            } catch (error) {
                bundle.functions[name] = { error: error.message };
            }
        }

        return bundle;
    }

    /**
     * Load function bundle
     */
    loadBundle(bundle, context = {}) {
        if (typeof bundle === 'string') {
            bundle = JSON.parse(bundle);
        }

        if (bundle.version !== this.serializationVersion) {
            throw new Error(`Bundle version mismatch: expected ${this.serializationVersion}, got ${bundle.version}`);
        }

        return this.deserializeFunctions(bundle.functions, context);
    }

    /**
     * Get serialization statistics
     */
    getStats() {
        return {
            totalSerialized: this.serializedFunctions.size,
            version: this.serializationVersion,
            cacheSize: this.contextCache.size
        };
    }

    /**
     * Clear cache
     */
    clearCache() {
        this.serializedFunctions.clear();
        this.contextCache.clear();
    }

    /**
     * Export cache
     */
    exportCache() {
        return {
            functions: Object.fromEntries(this.serializedFunctions),
            contexts: Object.fromEntries(this.contextCache),
            stats: this.getStats()
        };
    }

    /**
     * Import cache
     */
    importCache(cacheData) {
        if (cacheData.functions) {
            this.serializedFunctions = new Map(Object.entries(cacheData.functions));
        }
        if (cacheData.contexts) {
            this.contextCache = new Map(Object.entries(cacheData.contexts));
        }
    }
}

// Convenience functions
function serializeFunction(func, options = {}) {
    const fujsen = new FUJSEN();
    return fujsen.serializeFunction(func, options);
}

function deserializeFunction(serializedData, context = {}) {
    const fujsen = new FUJSEN();
    return fujsen.deserializeFunction(serializedData, context);
}

function createBundle(functions, options = {}) {
    const fujsen = new FUJSEN();
    return fujsen.createBundle(functions, options);
}

function loadBundle(bundle, context = {}) {
    const fujsen = new FUJSEN();
    return fujsen.loadBundle(bundle, context);
}

module.exports = {
    FUJSEN,
    serializeFunction,
    deserializeFunction,
    createBundle,
    loadBundle
}; 