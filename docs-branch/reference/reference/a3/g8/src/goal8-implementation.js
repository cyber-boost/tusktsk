/**
 * Goal 8 Implementation - Advanced Configuration Management
 * Combines Validation Engine, Migration Manager, and Logging Framework
 */

const { ValidationEngine } = require('./validation-engine');
const { MigrationManager } = require('./migration-manager');
const { LoggingFramework } = require('./logging-framework');

class Goal8Implementation {
    constructor(options = {}) {
        this.validationEngine = new ValidationEngine(options.validation || {});
        this.migrationManager = new MigrationManager(options.migration || {});
        this.loggingFramework = new LoggingFramework(options.logging || {});
        
        this.isInitialized = false;
        this.stats = {
            validations: 0,
            migrations: 0,
            logEntries: 0
        };
    }

    /**
     * Initialize all components
     */
    async initialize() {
        try {
            console.log('🚀 Initializing Goal 8 Implementation...');
            
            // Initialize validation engine
            console.log('✓ Validation engine initialized');
            
            // Initialize migration manager
            console.log('✓ Migration manager initialized');
            
            // Initialize logging framework
            console.log('✓ Logging framework initialized');
            
            // Set up event handlers
            this.setupEventHandlers();
            
            // Register default schemas
            this.registerDefaultSchemas();
            
            this.isInitialized = true;
            console.log('✓ Goal 8 implementation initialized successfully');
            
            return true;
        } catch (error) {
            throw new Error(`Failed to initialize Goal 8: ${error.message}`);
        }
    }

    /**
     * Set up event handlers for integration
     */
    setupEventHandlers() {
        // Validation engine events
        this.validationEngine.on('validationCompleted', (data) => {
            this.stats.validations++;
            this.loggingFramework.info('Validation completed', {
                schema: data.schemaName,
                errors: data.result.errors.length,
                warnings: data.result.warnings.length,
                executionTime: data.executionTime
            });
        });

        // Migration manager events
        this.migrationManager.on('migrationCompleted', (data) => {
            this.stats.migrations++;
            this.loggingFramework.info('Migration completed', {
                fromVersion: data.migrationPath[0]?.fromVersion,
                toVersion: data.version,
                steps: data.migrationPath.length,
                executionTime: data.executionTime
            });
        });

        // Logging framework events
        this.loggingFramework.on('log', (entry) => {
            this.stats.logEntries++;
        });
    }

    /**
     * Register default schemas
     */
    registerDefaultSchemas() {
        // Server configuration schema
        this.validationEngine.registerSchema('server-config', {
            type: 'object',
            required: ['host', 'port'],
            properties: {
                host: {
                    type: 'string',
                    validators: {
                        url: {}
                    }
                },
                port: {
                    type: 'number',
                    minimum: 1,
                    maximum: 65535,
                    validators: {
                        port: {}
                    }
                },
                timeout: {
                    type: 'number',
                    minimum: 1000,
                    maximum: 300000
                },
                ssl: {
                    type: 'boolean'
                }
            }
        });

        // Database configuration schema
        this.validationEngine.registerSchema('database-config', {
            type: 'object',
            required: ['type', 'host', 'port', 'name'],
            properties: {
                type: {
                    type: 'string',
                    enum: ['postgresql', 'mysql', 'sqlite', 'mongodb']
                },
                host: {
                    type: 'string'
                },
                port: {
                    type: 'number',
                    minimum: 1,
                    maximum: 65535
                },
                name: {
                    type: 'string',
                    minLength: 1
                },
                username: {
                    type: 'string'
                },
                password: {
                    type: 'string'
                }
            }
        });

        // Application configuration schema
        this.validationEngine.registerSchema('app-config', {
            type: 'object',
            required: ['name', 'version'],
            properties: {
                name: {
                    type: 'string',
                    minLength: 1,
                    maxLength: 100
                },
                version: {
                    type: 'string',
                    pattern: '^\\d+\\.\\d+\\.\\d+$'
                },
                debug: {
                    type: 'boolean'
                },
                features: {
                    type: 'array',
                    items: {
                        type: 'string'
                    }
                }
            }
        });
    }

    /**
     * Validate configuration
     */
    async validateConfig(config, schemaName, options = {}) {
        if (!this.isInitialized) {
            throw new Error('Goal 8 not initialized');
        }

        try {
            const result = await this.validationEngine.validate(config, schemaName, options);
            return result;
        } catch (error) {
            this.loggingFramework.error('Configuration validation failed', {
                schema: schemaName,
                error: error.message
            });
            throw error;
        }
    }

    /**
     * Migrate configuration
     */
    async migrateConfig(config, targetVersion, options = {}) {
        if (!this.isInitialized) {
            throw new Error('Goal 8 not initialized');
        }

        try {
            const result = await this.migrationManager.migrate(config, targetVersion, options);
            return result;
        } catch (error) {
            this.loggingFramework.error('Configuration migration failed', {
                targetVersion,
                error: error.message
            });
            throw error;
        }
    }

    /**
     * Log message with context
     */
    log(level, message, data = {}, context = {}) {
        if (!this.isInitialized) {
            throw new Error('Goal 8 not initialized');
        }

        this.loggingFramework.log(level, message, data, context);
    }

    /**
     * Create child logger with context
     */
    createChildLogger(context) {
        return this.loggingFramework.child(context);
    }

    /**
     * Register custom validator
     */
    registerValidator(name, validator) {
        return this.validationEngine.registerValidator(name, validator);
    }

    /**
     * Register custom migration
     */
    registerMigration(fromVersion, toVersion, migrationFunction, options = {}) {
        return this.migrationManager.registerMigration(fromVersion, toVersion, migrationFunction, options);
    }

    /**
     * Add log filter
     */
    addLogFilter(name, filterFunction) {
        return this.loggingFramework.addFilter(name, filterFunction);
    }

    /**
     * Add log transport
     */
    addLogTransport(name, transport) {
        return this.loggingFramework.addTransport(name, transport);
    }

    /**
     * Get comprehensive system status
     */
    getSystemStatus() {
        return {
            initialized: this.isInitialized,
            validation: this.validationEngine.getStats(),
            migration: this.migrationManager.getMigrationStats(),
            logging: this.loggingFramework.getStats(),
            stats: this.stats
        };
    }

    /**
     * Run comprehensive test suite
     */
    async runTests() {
        console.log('🧪 Running Goal 8 test suite...');
        
        const results = {
            validation: { passed: 0, total: 0, tests: [] },
            migration: { passed: 0, total: 0, tests: [] },
            logging: { passed: 0, total: 0, tests: [] },
            integration: { passed: 0, total: 0, tests: [] }
        };

        // Test validation engine
        await this.testValidationEngine(results.validation);
        
        // Test migration manager
        await this.testMigrationManager(results.migration);
        
        // Test logging framework
        await this.testLoggingFramework(results.logging);
        
        // Test integration
        await this.testIntegration(results.integration);

        return results;
    }

    /**
     * Test validation engine functionality
     */
    async testValidationEngine(results) {
        const testConfig = {
            host: 'localhost',
            port: 8080,
            timeout: 30000
        };

        try {
            // Test validation
            const result = await this.validateConfig(testConfig, 'server-config');
            const isValid = result.valid;
            results.tests.push({ name: 'Server config validation', passed: isValid });
            if (isValid) results.passed++;
        } catch (error) {
            results.tests.push({ name: 'Server config validation', passed: false, error: error.message });
        }
        results.total++;

        try {
            // Test invalid config
            const invalidConfig = { host: 'localhost' }; // Missing required port
            const result = await this.validateConfig(invalidConfig, 'server-config');
            const hasErrors = result.errors.length > 0;
            results.tests.push({ name: 'Invalid config validation', passed: hasErrors });
            if (hasErrors) results.passed++;
        } catch (error) {
            results.tests.push({ name: 'Invalid config validation', passed: false, error: error.message });
        }
        results.total++;

        try {
            // Test custom validator
            const customValidator = (value) => ({ valid: value.length > 0 });
            this.registerValidator('custom', customValidator);
            const hasValidator = this.validationEngine.customValidators.has('custom');
            results.tests.push({ name: 'Custom validator registration', passed: hasValidator });
            if (hasValidator) results.passed++;
        } catch (error) {
            results.tests.push({ name: 'Custom validator registration', passed: false, error: error.message });
        }
        results.total++;
    }

    /**
     * Test migration manager functionality
     */
    async testMigrationManager(results) {
        const testConfig = {
            server: { host: 'localhost', port: 8080 },
            database: { type: 'postgresql', host: 'db.example.com', port: 5432, name: 'testdb' }
        };

        try {
            // Test migration
            const result = await this.migrateConfig(testConfig, '2.0.0');
            const migrated = result.migrated && result.version === '2.0.0';
            results.tests.push({ name: 'Config migration', passed: migrated });
            if (migrated) results.passed++;
        } catch (error) {
            results.tests.push({ name: 'Config migration', passed: false, error: error.message });
        }
        results.total++;

        try {
            // Test migration path finding
            const migrations = this.migrationManager.getAvailableMigrations();
            const hasMigrations = migrations.length > 0;
            results.tests.push({ name: 'Migration path finding', passed: hasMigrations });
            if (hasMigrations) results.passed++;
        } catch (error) {
            results.tests.push({ name: 'Migration path finding', passed: false, error: error.message });
        }
        results.total++;

        try {
            // Test compatibility matrix
            const matrix = this.migrationManager.getCompatibilityMatrix();
            const hasMatrix = Object.keys(matrix).length > 0;
            results.tests.push({ name: 'Compatibility matrix', passed: hasMatrix });
            if (hasMatrix) results.passed++;
        } catch (error) {
            results.tests.push({ name: 'Compatibility matrix', passed: false, error: error.message });
        }
        results.total++;
    }

    /**
     * Test logging framework functionality
     */
    async testLoggingFramework(results) {
        try {
            // Test basic logging
            this.log('INFO', 'Test message', { test: true });
            const stats = this.loggingFramework.getStats();
            const hasTransports = stats.transports > 0;
            results.tests.push({ name: 'Basic logging', passed: hasTransports });
            if (hasTransports) results.passed++;
        } catch (error) {
            results.tests.push({ name: 'Basic logging', passed: false, error: error.message });
        }
        results.total++;

        try {
            // Test log level setting
            this.loggingFramework.setLevel('DEBUG');
            const stats = this.loggingFramework.getStats();
            const levelSet = stats.currentLevel === 'DEBUG';
            results.tests.push({ name: 'Log level setting', passed: levelSet });
            if (levelSet) results.passed++;
        } catch (error) {
            results.tests.push({ name: 'Log level setting', passed: false, error: error.message });
        }
        results.total++;

        try {
            // Test child logger
            const childLogger = this.createChildLogger({ component: 'test' });
            const isChildLogger = childLogger && typeof childLogger.log === 'function';
            results.tests.push({ name: 'Child logger creation', passed: isChildLogger });
            if (isChildLogger) results.passed++;
        } catch (error) {
            results.tests.push({ name: 'Child logger creation', passed: false, error: error.message });
        }
        results.total++;
    }

    /**
     * Test integration functionality
     */
    async testIntegration(results) {
        try {
            // Test system status
            const status = this.getSystemStatus();
            const hasAllComponents = status.validation && status.migration && status.logging && status.stats;
            results.tests.push({ name: 'System status integration', passed: hasAllComponents });
            if (hasAllComponents) results.passed++;
        } catch (error) {
            results.tests.push({ name: 'System status integration', passed: false, error: error.message });
        }
        results.total++;

        try {
            // Test end-to-end workflow
            const config = { host: 'localhost', port: 8080 };
            const validation = await this.validateConfig(config, 'server-config');
            const migration = await this.migrateConfig(config, '2.0.0');
            this.log('INFO', 'End-to-end test completed', { validation: validation.valid, migration: migration.migrated });
            
            const workflowSuccess = validation.valid && migration.migrated;
            results.tests.push({ name: 'End-to-end workflow', passed: workflowSuccess });
            if (workflowSuccess) results.passed++;
        } catch (error) {
            results.tests.push({ name: 'End-to-end workflow', passed: false, error: error.message });
        }
        results.total++;
    }
}

module.exports = { Goal8Implementation }; 