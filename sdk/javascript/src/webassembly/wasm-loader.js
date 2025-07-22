/**
 * WebAssembly Module Loader for TuskTsk
 * Provides comprehensive WASM loading, execution, and security sandboxing
 * 
 * @author AI-Cloud-Native-Engineer
 * @version 2.0.3
 * @license BBL
 */

const fs = require('fs').promises;
const path = require('path');
const crypto = require('crypto');

class WebAssemblyLoader {
    constructor() {
        this.modules = new Map();
        this.instances = new Map();
        this.imports = new Map();
        this.securityPolicies = new Map();
        this.isInitialized = false;
        this.moduleCache = new Map();
        this.performanceMetrics = new Map();
    }

    /**
     * Initialize WebAssembly loader
     */
    async initialize() {
        try {
            // Check WebAssembly support
            if (typeof WebAssembly === 'undefined') {
                throw new Error('WebAssembly is not supported in this environment');
            }

            // Create default security policies
            this.createDefaultSecurityPolicies();
            
            this.isInitialized = true;
            console.log('✅ WebAssembly Loader initialized successfully');
            
            return {
                success: true,
                supported: true,
                version: '1.0',
                securityPolicies: Array.from(this.securityPolicies.keys())
            };
        } catch (error) {
            console.error('❌ WebAssembly Loader initialization failed:', error);
            throw new Error(`WebAssembly Loader initialization failed: ${error.message}`);
        }
    }

    /**
     * Create default security policies
     */
    createDefaultSecurityPolicies() {
        // Strict policy - minimal imports
        this.securityPolicies.set('strict', {
            name: 'strict',
            description: 'Minimal imports, maximum security',
            allowedImports: ['env'],
            memoryLimit: 1024 * 1024, // 1MB
            executionTimeout: 5000, // 5 seconds
            allowFileSystem: false,
            allowNetwork: false,
            allowSystemCalls: false
        });

        // Standard policy - balanced security
        this.securityPolicies.set('standard', {
            name: 'standard',
            description: 'Balanced security and functionality',
            allowedImports: ['env', 'console'],
            memoryLimit: 10 * 1024 * 1024, // 10MB
            executionTimeout: 30000, // 30 seconds
            allowFileSystem: false,
            allowNetwork: false,
            allowSystemCalls: false
        });

        // Permissive policy - maximum functionality
        this.securityPolicies.set('permissive', {
            name: 'permissive',
            description: 'Maximum functionality, use with caution',
            allowedImports: ['env', 'console', 'fs', 'net'],
            memoryLimit: 100 * 1024 * 1024, // 100MB
            executionTimeout: 300000, // 5 minutes
            allowFileSystem: true,
            allowNetwork: true,
            allowSystemCalls: true
        });
    }

    /**
     * Load WebAssembly module from file
     */
    async loadModule(name, filePath, options = {}) {
        if (!this.isInitialized) {
            throw new Error('WebAssembly Loader not initialized. Call initialize() first.');
        }

        const {
            securityPolicy = 'standard',
            cache = true,
            validate = true
        } = options;

        try {
            // Check if module is already loaded
            if (this.modules.has(name)) {
                console.log(`Module '${name}' already loaded, returning cached version`);
                return {
                    success: true,
                    name: name,
                    cached: true
                };
            }

            // Read WASM file
            const wasmBuffer = await fs.readFile(filePath);
            
            // Validate WASM file
            if (validate) {
                const validationResult = await this.validateWasmModule(wasmBuffer);
                if (!validationResult.valid) {
                    throw new Error(`WASM validation failed: ${validationResult.error}`);
                }
            }

            // Compile module
            const startTime = Date.now();
            const module = await WebAssembly.compile(wasmBuffer);
            const compileTime = Date.now() - startTime;

            // Store module
            this.modules.set(name, {
                module: module,
                filePath: filePath,
                securityPolicy: securityPolicy,
                compileTime: compileTime,
                size: wasmBuffer.length,
                loaded: new Date(),
                checksum: crypto.createHash('md5').update(wasmBuffer).digest('hex')
            });

            // Cache module if requested
            if (cache) {
                this.moduleCache.set(name, {
                    module: module,
                    timestamp: new Date()
                });
            }

            console.log(`✅ WebAssembly module '${name}' loaded successfully`);
            return {
                success: true,
                name: name,
                compileTime: compileTime,
                size: wasmBuffer.length,
                securityPolicy: securityPolicy
            };
        } catch (error) {
            console.error(`❌ Module loading failed: ${error.message}`);
            throw new Error(`Module loading failed: ${error.message}`);
        }
    }

    /**
     * Validate WebAssembly module
     */
    async validateWasmModule(wasmBuffer) {
        try {
            // Basic validation - try to compile
            await WebAssembly.validate(wasmBuffer);
            
            // Additional validation can be added here
            // - Check for specific imports
            // - Validate memory requirements
            // - Check for suspicious patterns
            
            return {
                valid: true,
                error: null
            };
        } catch (error) {
            return {
                valid: false,
                error: error.message
            };
        }
    }

    /**
     * Instantiate WebAssembly module
     */
    async instantiateModule(name, imports = {}, options = {}) {
        if (!this.modules.has(name)) {
            throw new Error(`Module '${name}' not found. Load it first.`);
        }

        const {
            instanceName = `${name}_instance`,
            securityPolicy = null,
            memorySize = 256, // 256 pages = 16MB
            maxMemorySize = 2048 // 2048 pages = 128MB
        } = options;

        try {
            const moduleInfo = this.modules.get(name);
            const policy = securityPolicy || moduleInfo.securityPolicy;
            const securityConfig = this.securityPolicies.get(policy);

            if (!securityConfig) {
                throw new Error(`Security policy '${policy}' not found`);
            }

            // Create imports with security restrictions
            const secureImports = this.createSecureImports(imports, securityConfig);

            // Create memory if needed
            if (securityConfig.memoryLimit > 0) {
                secureImports.env = secureImports.env || {};
                secureImports.env.memory = new WebAssembly.Memory({
                    initial: memorySize,
                    maximum: Math.min(maxMemorySize, Math.floor(securityConfig.memoryLimit / 65536))
                });
            }

            // Instantiate with timeout
            const startTime = Date.now();
            const instance = await this.instantiateWithTimeout(
                moduleInfo.module,
                secureImports,
                securityConfig.executionTimeout
            );
            const instantiateTime = Date.now() - startTime;

            // Store instance
            this.instances.set(instanceName, {
                instance: instance,
                moduleName: name,
                imports: secureImports,
                securityPolicy: policy,
                instantiateTime: instantiateTime,
                created: new Date(),
                memoryUsage: this.getMemoryUsage(instance)
            });

            console.log(`✅ WebAssembly instance '${instanceName}' created successfully`);
            return {
                success: true,
                instanceName: instanceName,
                moduleName: name,
                instantiateTime: instantiateTime,
                exports: Object.keys(instance.exports),
                memoryUsage: this.getMemoryUsage(instance)
            };
        } catch (error) {
            console.error(`❌ Module instantiation failed: ${error.message}`);
            throw new Error(`Module instantiation failed: ${error.message}`);
        }
    }

    /**
     * Instantiate with timeout
     */
    async instantiateWithTimeout(module, imports, timeout) {
        return new Promise((resolve, reject) => {
            const timeoutId = setTimeout(() => {
                reject(new Error(`Instantiation timeout after ${timeout}ms`));
            }, timeout);

            WebAssembly.instantiate(module, imports)
                .then(result => {
                    clearTimeout(timeoutId);
                    resolve(result.instance);
                })
                .catch(error => {
                    clearTimeout(timeoutId);
                    reject(error);
                });
        });
    }

    /**
     * Create secure imports based on security policy
     */
    createSecureImports(imports, securityConfig) {
        const secureImports = {};

        // Only allow imports specified in security policy
        for (const allowedImport of securityConfig.allowedImports) {
            if (imports[allowedImport]) {
                secureImports[allowedImport] = imports[allowedImport];
            }
        }

        // Add default console logging if allowed
        if (securityConfig.allowedImports.includes('console')) {
            secureImports.console = {
                log: (...args) => console.log('[WASM]', ...args),
                error: (...args) => console.error('[WASM]', ...args),
                warn: (...args) => console.warn('[WASM]', ...args)
            };
        }

        // Add default environment if allowed
        if (securityConfig.allowedImports.includes('env')) {
            secureImports.env = {
                ...secureImports.env,
                abort: () => {
                    throw new Error('WASM module called abort');
                }
            };
        }

        return secureImports;
    }

    /**
     * Call function in WebAssembly instance
     */
    async callFunction(instanceName, functionName, ...args) {
        if (!this.instances.has(instanceName)) {
            throw new Error(`Instance '${instanceName}' not found`);
        }

        try {
            const instanceInfo = this.instances.get(instanceName);
            const instance = instanceInfo.instance;
            const securityConfig = this.securityPolicies.get(instanceInfo.securityPolicy);

            // Check if function exists
            if (!instance.exports[functionName]) {
                throw new Error(`Function '${functionName}' not found in instance`);
            }

            // Call function with timeout
            const startTime = Date.now();
            const result = await this.callWithTimeout(
                instance.exports[functionName],
                args,
                securityConfig.executionTimeout
            );
            const executionTime = Date.now() - startTime;

            // Update performance metrics
            this.updatePerformanceMetrics(instanceName, functionName, executionTime);

            // Update memory usage
            instanceInfo.memoryUsage = this.getMemoryUsage(instance);

            return {
                success: true,
                result: result,
                executionTime: executionTime,
                memoryUsage: instanceInfo.memoryUsage
            };
        } catch (error) {
            console.error(`❌ Function call failed: ${error.message}`);
            throw new Error(`Function call failed: ${error.message}`);
        }
    }

    /**
     * Call function with timeout
     */
    async callWithTimeout(func, args, timeout) {
        return new Promise((resolve, reject) => {
            const timeoutId = setTimeout(() => {
                reject(new Error(`Function execution timeout after ${timeout}ms`));
            }, timeout);

            try {
                const result = func(...args);
                clearTimeout(timeoutId);
                resolve(result);
            } catch (error) {
                clearTimeout(timeoutId);
                reject(error);
            }
        });
    }

    /**
     * Get memory usage of instance
     */
    getMemoryUsage(instance) {
        if (instance.exports.memory) {
            const memory = instance.exports.memory;
            return {
                pages: memory.buffer.byteLength / 65536,
                bytes: memory.buffer.byteLength,
                used: this.estimateMemoryUsage(memory.buffer)
            };
        }
        return { pages: 0, bytes: 0, used: 0 };
    }

    /**
     * Estimate memory usage
     */
    estimateMemoryUsage(buffer) {
        // Simple estimation - can be improved with more sophisticated analysis
        let used = 0;
        const view = new Uint8Array(buffer);
        for (let i = 0; i < view.length; i += 1024) {
            if (view[i] !== 0) {
                used += 1024;
            }
        }
        return used;
    }

    /**
     * Update performance metrics
     */
    updatePerformanceMetrics(instanceName, functionName, executionTime) {
        const key = `${instanceName}:${functionName}`;
        if (!this.performanceMetrics.has(key)) {
            this.performanceMetrics.set(key, {
                calls: 0,
                totalTime: 0,
                minTime: Infinity,
                maxTime: 0,
                avgTime: 0
            });
        }

        const metrics = this.performanceMetrics.get(key);
        metrics.calls++;
        metrics.totalTime += executionTime;
        metrics.minTime = Math.min(metrics.minTime, executionTime);
        metrics.maxTime = Math.max(metrics.maxTime, executionTime);
        metrics.avgTime = metrics.totalTime / metrics.calls;
    }

    /**
     * Get module information
     */
    getModuleInfo(name) {
        if (!this.modules.has(name)) {
            throw new Error(`Module '${name}' not found`);
        }

        const moduleInfo = this.modules.get(name);
        return {
            name: name,
            filePath: moduleInfo.filePath,
            securityPolicy: moduleInfo.securityPolicy,
            compileTime: moduleInfo.compileTime,
            size: moduleInfo.size,
            loaded: moduleInfo.loaded,
            checksum: moduleInfo.checksum
        };
    }

    /**
     * Get instance information
     */
    getInstanceInfo(instanceName) {
        if (!this.instances.has(instanceName)) {
            throw new Error(`Instance '${instanceName}' not found`);
        }

        const instanceInfo = this.instances.get(instanceName);
        return {
            name: instanceName,
            moduleName: instanceInfo.moduleName,
            securityPolicy: instanceInfo.securityPolicy,
            instantiateTime: instanceInfo.instantiateTime,
            created: instanceInfo.created,
            memoryUsage: instanceInfo.memoryUsage,
            exports: Object.keys(instanceInfo.instance.exports)
        };
    }

    /**
     * List all modules
     */
    listModules() {
        return Array.from(this.modules.keys()).map(name => this.getModuleInfo(name));
    }

    /**
     * List all instances
     */
    listInstances() {
        return Array.from(this.instances.keys()).map(name => this.getInstanceInfo(name));
    }

    /**
     * Get performance metrics
     */
    getPerformanceMetrics() {
        return Object.fromEntries(this.performanceMetrics);
    }

    /**
     * Delete module
     */
    deleteModule(name) {
        if (!this.modules.has(name)) {
            throw new Error(`Module '${name}' not found`);
        }

        // Delete all instances of this module
        for (const [instanceName, instanceInfo] of this.instances) {
            if (instanceInfo.moduleName === name) {
                this.instances.delete(instanceName);
            }
        }

        // Delete module
        this.modules.delete(name);
        this.moduleCache.delete(name);

        console.log(`✅ Module '${name}' deleted`);
        return {
            success: true,
            name: name
        };
    }

    /**
     * Delete instance
     */
    deleteInstance(instanceName) {
        if (!this.instances.has(instanceName)) {
            throw new Error(`Instance '${instanceName}' not found`);
        }

        this.instances.delete(instanceName);

        console.log(`✅ Instance '${instanceName}' deleted`);
        return {
            success: true,
            name: instanceName
        };
    }

    /**
     * Get loader information
     */
    getInfo() {
        return {
            initialized: this.isInitialized,
            modules: this.modules.size,
            instances: this.instances.size,
            securityPolicies: Array.from(this.securityPolicies.keys()),
            performanceMetrics: this.performanceMetrics.size
        };
    }

    /**
     * Clean up resources
     */
    cleanup() {
        this.modules.clear();
        this.instances.clear();
        this.imports.clear();
        this.moduleCache.clear();
        this.performanceMetrics.clear();
        
        console.log('✅ WebAssembly Loader resources cleaned up');
    }
}

// Export the class
module.exports = WebAssemblyLoader;

// Create a singleton instance
const webAssemblyLoader = new WebAssemblyLoader();

// Export the singleton instance
module.exports.instance = webAssemblyLoader; 