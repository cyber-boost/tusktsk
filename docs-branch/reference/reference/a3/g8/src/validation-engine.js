/**
 * Advanced Configuration Validation and Schema Management
 * Goal 8.1 Implementation
 */

const EventEmitter = require('events');

class ValidationEngine extends EventEmitter {
    constructor(options = {}) {
        super();
        this.schemas = new Map();
        this.customValidators = new Map();
        this.validationCache = new Map();
        this.strictMode = options.strictMode || false;
        this.autoFix = options.autoFix || false;
        this.maxErrors = options.maxErrors || 100;
        
        this.registerBuiltInValidators();
    }

    /**
     * Register a JSON schema for validation
     */
    registerSchema(name, schema) {
        try {
            // Basic schema validation
            this.validateSchemaStructure(schema);
            
            this.schemas.set(name, {
                schema,
                registeredAt: Date.now(),
                usageCount: 0
            });
            
            console.log(`✓ Schema registered: ${name}`);
            this.emit('schemaRegistered', { name, schema });
            
            return true;
        } catch (error) {
            throw new Error(`Failed to register schema '${name}': ${error.message}`);
        }
    }

    /**
     * Validate configuration against schema
     */
    validate(config, schemaName, options = {}) {
        const startTime = Date.now();
        const schema = this.schemas.get(schemaName);
        
        if (!schema) {
            throw new Error(`Schema '${schemaName}' not found`);
        }

        try {
            // Update usage count
            schema.usageCount++;
            
            // Check cache first
            const cacheKey = this.generateCacheKey(config, schemaName, options);
            if (this.validationCache.has(cacheKey)) {
                const cached = this.validationCache.get(cacheKey);
                if (Date.now() - cached.timestamp < 300000) { // 5 minutes cache
                    return cached.result;
                }
            }

            // Perform validation
            const result = this.performValidation(config, schema.schema, options);
            
            // Cache result
            this.validationCache.set(cacheKey, {
                result,
                timestamp: Date.now()
            });

            const executionTime = Date.now() - startTime;
            console.log(`✓ Validation completed in ${executionTime}ms (${result.errors.length} errors)`);
            
            this.emit('validationCompleted', { schemaName, result, executionTime });
            
            return result;
        } catch (error) {
            throw new Error(`Validation failed: ${error.message}`);
        }
    }

    /**
     * Perform actual validation logic
     */
    performValidation(config, schema, options) {
        const errors = [];
        const warnings = [];
        const fixes = [];

        // Type validation
        this.validateTypes(config, schema, errors, warnings, options);
        
        // Required field validation
        this.validateRequired(config, schema, errors, options);
        
        // Custom validator execution
        this.executeCustomValidators(config, schema, errors, warnings, options);
        
        // Range and constraint validation
        this.validateConstraints(config, schema, errors, warnings, options);
        
        // Auto-fix if enabled
        if (this.autoFix && options.autoFix !== false) {
            this.autoFixIssues(config, errors, fixes);
        }

        return {
            valid: errors.length === 0,
            errors: errors.slice(0, this.maxErrors),
            warnings: warnings.slice(0, this.maxErrors),
            fixes: fixes,
            schema: schema,
            timestamp: Date.now()
        };
    }

    /**
     * Validate data types
     */
    validateTypes(data, schema, errors, warnings, options) {
        if (!schema.type) return;

        const expectedType = schema.type;
        const actualType = this.getType(data);

        if (expectedType !== actualType) {
            const error = {
                type: 'TYPE_MISMATCH',
                path: options.path || 'root',
                expected: expectedType,
                actual: actualType,
                value: data,
                message: `Expected ${expectedType}, got ${actualType}`
            };
            
            if (this.strictMode) {
                errors.push(error);
            } else {
                warnings.push(error);
            }
        }
    }

    /**
     * Validate required fields
     */
    validateRequired(data, schema, errors, options) {
        if (!schema.required || !Array.isArray(schema.required)) return;
        if (typeof data !== 'object' || data === null) return;

        for (const field of schema.required) {
            if (!(field in data)) {
                errors.push({
                    type: 'MISSING_REQUIRED_FIELD',
                    path: options.path ? `${options.path}.${field}` : field,
                    field: field,
                    message: `Required field '${field}' is missing`
                });
            }
        }
    }

    /**
     * Execute custom validators
     */
    executeCustomValidators(data, schema, errors, warnings, options) {
        if (!schema.validators) return;

        for (const [validatorName, validatorConfig] of Object.entries(schema.validators)) {
            const validator = this.customValidators.get(validatorName);
            if (!validator) {
                warnings.push({
                    type: 'UNKNOWN_VALIDATOR',
                    validator: validatorName,
                    message: `Unknown validator: ${validatorName}`
                });
                continue;
            }

            try {
                const result = validator(data, validatorConfig, options);
                if (result && !result.valid) {
                    if (result.severity === 'error') {
                        errors.push(result);
                    } else {
                        warnings.push(result);
                    }
                }
            } catch (error) {
                errors.push({
                    type: 'VALIDATOR_ERROR',
                    validator: validatorName,
                    error: error.message,
                    message: `Validator '${validatorName}' failed: ${error.message}`
                });
            }
        }
    }

    /**
     * Validate constraints (min, max, pattern, etc.)
     */
    validateConstraints(data, schema, errors, warnings, options) {
        if (typeof data === 'number') {
            this.validateNumberConstraints(data, schema, errors, warnings, options);
        } else if (typeof data === 'string') {
            this.validateStringConstraints(data, schema, errors, warnings, options);
        } else if (Array.isArray(data)) {
            this.validateArrayConstraints(data, schema, errors, warnings, options);
        }
    }

    /**
     * Validate number constraints
     */
    validateNumberConstraints(value, schema, errors, warnings, options) {
        if (schema.minimum !== undefined && value < schema.minimum) {
            errors.push({
                type: 'VALUE_TOO_SMALL',
                path: options.path || 'root',
                value: value,
                minimum: schema.minimum,
                message: `Value ${value} is less than minimum ${schema.minimum}`
            });
        }

        if (schema.maximum !== undefined && value > schema.maximum) {
            errors.push({
                type: 'VALUE_TOO_LARGE',
                path: options.path || 'root',
                value: value,
                maximum: schema.maximum,
                message: `Value ${value} is greater than maximum ${schema.maximum}`
            });
        }
    }

    /**
     * Validate string constraints
     */
    validateStringConstraints(value, schema, errors, warnings, options) {
        if (schema.minLength !== undefined && value.length < schema.minLength) {
            errors.push({
                type: 'STRING_TOO_SHORT',
                path: options.path || 'root',
                value: value,
                minLength: schema.minLength,
                actualLength: value.length,
                message: `String length ${value.length} is less than minimum ${schema.minLength}`
            });
        }

        if (schema.maxLength !== undefined && value.length > schema.maxLength) {
            errors.push({
                type: 'STRING_TOO_LONG',
                path: options.path || 'root',
                value: value,
                maxLength: schema.maxLength,
                actualLength: value.length,
                message: `String length ${value.length} is greater than maximum ${schema.maxLength}`
            });
        }

        if (schema.pattern && !new RegExp(schema.pattern).test(value)) {
            errors.push({
                type: 'PATTERN_MISMATCH',
                path: options.path || 'root',
                value: value,
                pattern: schema.pattern,
                message: `Value does not match pattern: ${schema.pattern}`
            });
        }
    }

    /**
     * Validate array constraints
     */
    validateArrayConstraints(value, schema, errors, warnings, options) {
        if (schema.minItems !== undefined && value.length < schema.minItems) {
            errors.push({
                type: 'ARRAY_TOO_SHORT',
                path: options.path || 'root',
                value: value,
                minItems: schema.minItems,
                actualLength: value.length,
                message: `Array length ${value.length} is less than minimum ${schema.minItems}`
            });
        }

        if (schema.maxItems !== undefined && value.length > schema.maxItems) {
            errors.push({
                type: 'ARRAY_TOO_LONG',
                path: options.path || 'root',
                value: value,
                maxItems: schema.maxItems,
                actualLength: value.length,
                message: `Array length ${value.length} is greater than maximum ${schema.maxItems}`
            });
        }
    }

    /**
     * Auto-fix common issues
     */
    autoFixIssues(config, errors, fixes) {
        for (const error of errors) {
            switch (error.type) {
                case 'TYPE_MISMATCH':
                    const fix = this.suggestTypeFix(error);
                    if (fix) fixes.push(fix);
                    break;
                case 'MISSING_REQUIRED_FIELD':
                    fixes.push({
                        type: 'ADD_DEFAULT_VALUE',
                        path: error.path,
                        field: error.field,
                        suggestedValue: this.getDefaultValue(error.field)
                    });
                    break;
            }
        }
    }

    /**
     * Register custom validator
     */
    registerValidator(name, validator) {
        if (typeof validator !== 'function') {
            throw new Error('Validator must be a function');
        }

        this.customValidators.set(name, validator);
        console.log(`✓ Custom validator registered: ${name}`);
        this.emit('validatorRegistered', { name });
        
        return true;
    }

    /**
     * Register built-in validators
     */
    registerBuiltInValidators() {
        // Email validator
        this.registerValidator('email', (value, config) => {
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (!emailRegex.test(value)) {
                return {
                    type: 'INVALID_EMAIL',
                    valid: false,
                    severity: 'error',
                    message: `Invalid email format: ${value}`
                };
            }
            return { valid: true };
        });

        // URL validator
        this.registerValidator('url', (value, config) => {
            try {
                new URL(value);
                return { valid: true };
            } catch {
                return {
                    type: 'INVALID_URL',
                    valid: false,
                    severity: 'error',
                    message: `Invalid URL format: ${value}`
                };
            }
        });

        // Port number validator
        this.registerValidator('port', (value, config) => {
            const port = parseInt(value);
            if (isNaN(port) || port < 1 || port > 65535) {
                return {
                    type: 'INVALID_PORT',
                    valid: false,
                    severity: 'error',
                    message: `Invalid port number: ${value} (must be 1-65535)`
                };
            }
            return { valid: true };
        });
    }

    /**
     * Get data type
     */
    getType(data) {
        if (data === null) return 'null';
        if (Array.isArray(data)) return 'array';
        return typeof data;
    }

    /**
     * Generate cache key
     */
    generateCacheKey(config, schemaName, options) {
        return `${schemaName}_${JSON.stringify(config)}_${JSON.stringify(options)}`;
    }

    /**
     * Validate schema structure
     */
    validateSchemaStructure(schema) {
        if (!schema || typeof schema !== 'object') {
            throw new Error('Schema must be an object');
        }
        
        if (schema.type && !['string', 'number', 'boolean', 'object', 'array', 'null'].includes(schema.type)) {
            throw new Error(`Invalid schema type: ${schema.type}`);
        }
    }

    /**
     * Suggest type fix
     */
    suggestTypeFix(error) {
        switch (error.expected) {
            case 'number':
                const num = parseFloat(error.value);
                if (!isNaN(num)) {
                    return {
                        type: 'CONVERT_TO_NUMBER',
                        path: error.path,
                        originalValue: error.value,
                        suggestedValue: num
                    };
                }
                break;
            case 'string':
                return {
                    type: 'CONVERT_TO_STRING',
                    path: error.path,
                    originalValue: error.value,
                    suggestedValue: String(error.value)
                };
        }
        return null;
    }

    /**
     * Get default value for field
     */
    getDefaultValue(fieldName) {
        const defaults = {
            port: 8080,
            host: 'localhost',
            debug: false,
            timeout: 30000,
            retries: 3
        };
        return defaults[fieldName] || null;
    }

    /**
     * Clear validation cache
     */
    clearCache() {
        this.validationCache.clear();
        console.log('✓ Validation cache cleared');
    }

    /**
     * Get validation statistics
     */
    getStats() {
        return {
            schemas: this.schemas.size,
            validators: this.customValidators.size,
            cacheSize: this.validationCache.size,
            strictMode: this.strictMode,
            autoFix: this.autoFix
        };
    }
}

module.exports = { ValidationEngine }; 