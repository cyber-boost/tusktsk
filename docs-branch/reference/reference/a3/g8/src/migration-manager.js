/**
 * Intelligent Configuration Migration and Version Management
 * Goal 8.2 Implementation
 */

const EventEmitter = require('events');
const crypto = require('crypto');

class MigrationManager extends EventEmitter {
    constructor(options = {}) {
        super();
        this.migrations = new Map();
        this.versionHistory = new Map();
        this.migrationCache = new Map();
        this.autoMigrate = options.autoMigrate || true;
        this.backupBeforeMigration = options.backupBeforeMigration || true;
        this.maxMigrationSteps = options.maxMigrationSteps || 10;
        
        this.registerBuiltInMigrations();
    }

    /**
     * Register a migration for a specific version
     */
    registerMigration(fromVersion, toVersion, migrationFunction, options = {}) {
        const migrationKey = `${fromVersion}_${toVersion}`;
        
        if (typeof migrationFunction !== 'function') {
            throw new Error('Migration function must be a function');
        }

        this.migrations.set(migrationKey, {
            fromVersion,
            toVersion,
            function: migrationFunction,
            description: options.description || `Migration from ${fromVersion} to ${toVersion}`,
            critical: options.critical || false,
            rollback: options.rollback || null,
            registeredAt: Date.now()
        });

        console.log(`✓ Migration registered: ${fromVersion} → ${toVersion}`);
        this.emit('migrationRegistered', { fromVersion, toVersion, description: options.description });
        
        return true;
    }

    /**
     * Migrate configuration to target version
     */
    async migrate(config, targetVersion, options = {}) {
        const startTime = Date.now();
        const currentVersion = this.detectVersion(config);
        
        if (currentVersion === targetVersion) {
            console.log(`✓ Configuration already at target version: ${targetVersion}`);
            return { config, migrated: false, version: targetVersion };
        }

        try {
            console.log(`🚀 Starting migration: ${currentVersion} → ${targetVersion}`);
            
            // Create backup if enabled
            let backup = null;
            if (this.backupBeforeMigration && options.backup !== false) {
                backup = this.createBackup(config, currentVersion);
            }

            // Find migration path
            const migrationPath = this.findMigrationPath(currentVersion, targetVersion);
            if (!migrationPath) {
                throw new Error(`No migration path found from ${currentVersion} to ${targetVersion}`);
            }

            // Execute migrations
            let currentConfig = { ...config };
            const migrationResults = [];

            for (const migrationStep of migrationPath) {
                const result = await this.executeMigration(currentConfig, migrationStep, options);
                currentConfig = result.config;
                migrationResults.push(result);
                
                console.log(`✓ Migration step completed: ${migrationStep.fromVersion} → ${migrationStep.toVersion}`);
            }

            // Update version in config
            currentConfig._version = targetVersion;
            currentConfig._migratedAt = Date.now();
            currentConfig._migrationHistory = migrationResults.map(r => ({
                from: r.fromVersion,
                to: r.toVersion,
                timestamp: r.timestamp,
                description: r.description
            }));

            const executionTime = Date.now() - startTime;
            console.log(`✓ Migration completed in ${executionTime}ms`);

            const result = {
                config: currentConfig,
                migrated: true,
                version: targetVersion,
                migrationPath,
                migrationResults,
                backup,
                executionTime
            };

            this.emit('migrationCompleted', result);
            return result;

        } catch (error) {
            console.error(`✗ Migration failed: ${error.message}`);
            this.emit('migrationFailed', { error: error.message, fromVersion: currentVersion, toVersion: targetVersion });
            throw new Error(`Migration failed: ${error.message}`);
        }
    }

    /**
     * Execute a single migration step
     */
    async executeMigration(config, migrationStep, options) {
        const migrationKey = `${migrationStep.fromVersion}_${migrationStep.toVersion}`;
        const migration = this.migrations.get(migrationKey);
        
        if (!migration) {
            throw new Error(`Migration not found: ${migrationKey}`);
        }

        const startTime = Date.now();
        
        try {
            // Execute migration function
            const migratedConfig = await migration.function(config, options);
            
            // Validate migrated config
            this.validateMigratedConfig(migratedConfig, migrationStep.toVersion);
            
            const executionTime = Date.now() - startTime;
            
            const result = {
                config: migratedConfig,
                fromVersion: migrationStep.fromVersion,
                toVersion: migrationStep.toVersion,
                description: migration.description,
                timestamp: Date.now(),
                executionTime,
                critical: migration.critical
            };

            // Cache migration result
            this.migrationCache.set(migrationKey, {
                result,
                timestamp: Date.now()
            });

            return result;

        } catch (error) {
            throw new Error(`Migration step failed (${migrationKey}): ${error.message}`);
        }
    }

    /**
     * Find optimal migration path
     */
    findMigrationPath(fromVersion, toVersion) {
        const visited = new Set();
        const queue = [{ version: fromVersion, path: [] }];
        
        while (queue.length > 0) {
            const { version, path } = queue.shift();
            
            if (version === toVersion) {
                return path;
            }
            
            if (visited.has(version) || path.length >= this.maxMigrationSteps) {
                continue;
            }
            
            visited.add(version);
            
            // Find all possible next migrations
            for (const [migrationKey, migration] of this.migrations) {
                if (migration.fromVersion === version) {
                    queue.push({
                        version: migration.toVersion,
                        path: [...path, migration]
                    });
                }
            }
        }
        
        return null;
    }

    /**
     * Detect configuration version
     */
    detectVersion(config) {
        if (config._version) {
            return config._version;
        }
        
        // Version detection based on structure
        if (config.server && config.database) {
            return '1.0.0';
        } else if (config.api && config.services) {
            return '2.0.0';
        } else if (config.features && config.security) {
            return '3.0.0';
        }
        
        return '0.1.0'; // Default version
    }

    /**
     * Validate migrated configuration
     */
    validateMigratedConfig(config, expectedVersion) {
        if (!config || typeof config !== 'object') {
            throw new Error('Migrated config must be an object');
        }
        
        // Basic structure validation
        if (expectedVersion.startsWith('1.')) {
            if (!config.server || !config.database) {
                throw new Error('Version 1.x config must have server and database sections');
            }
        } else if (expectedVersion.startsWith('2.')) {
            if (!config.api || !config.services) {
                throw new Error('Version 2.x config must have api and services sections');
            }
        } else if (expectedVersion.startsWith('3.')) {
            if (!config.features || !config.security) {
                throw new Error('Version 3.x config must have features and security sections');
            }
        }
    }

    /**
     * Create backup of configuration
     */
    createBackup(config, version) {
        const backup = {
            config: JSON.parse(JSON.stringify(config)),
            version,
            timestamp: Date.now(),
            checksum: this.calculateChecksum(config)
        };
        
        console.log(`✓ Backup created for version ${version}`);
        return backup;
    }

    /**
     * Restore configuration from backup
     */
    restoreFromBackup(backup) {
        if (!backup || !backup.config) {
            throw new Error('Invalid backup data');
        }
        
        const currentChecksum = this.calculateChecksum(backup.config);
        if (currentChecksum !== backup.checksum) {
            throw new Error('Backup checksum validation failed');
        }
        
        console.log(`✓ Configuration restored from backup (version ${backup.version})`);
        return backup.config;
    }

    /**
     * Register built-in migrations
     */
    registerBuiltInMigrations() {
        // Migration from 1.0.0 to 2.0.0
        this.registerMigration('1.0.0', '2.0.0', (config, options) => {
            const migrated = {
                api: {
                    host: config.server.host,
                    port: config.server.port,
                    timeout: config.server.timeout || 30000
                },
                services: {
                    database: {
                        type: config.database.type,
                        host: config.database.host,
                        port: config.database.port,
                        name: config.database.name
                    }
                },
                _migratedFrom: '1.0.0',
                _migratedAt: Date.now()
            };
            
            console.log('✓ Migrated from 1.0.0 to 2.0.0 structure');
            return migrated;
        }, {
            description: 'Restructure server/database to api/services format',
            critical: true
        });

        // Migration from 2.0.0 to 3.0.0
        this.registerMigration('2.0.0', '3.0.0', (config, options) => {
            const migrated = {
                features: {
                    api: config.api,
                    services: config.services
                },
                security: {
                    enabled: true,
                    encryption: 'aes-256-gcm',
                    timeout: config.api.timeout
                },
                _migratedFrom: '2.0.0',
                _migratedAt: Date.now()
            };
            
            console.log('✓ Migrated from 2.0.0 to 3.0.0 structure');
            return migrated;
        }, {
            description: 'Add security layer and reorganize to features/security format',
            critical: true
        });

        // Migration from 3.0.0 to 3.1.0
        this.registerMigration('3.0.0', '3.1.0', (config, options) => {
            const migrated = {
                ...config,
                features: {
                    ...config.features,
                    monitoring: {
                        enabled: true,
                        interval: 5000
                    }
                },
                _migratedFrom: '3.0.0',
                _migratedAt: Date.now()
            };
            
            console.log('✓ Migrated from 3.0.0 to 3.1.0 (added monitoring)');
            return migrated;
        }, {
            description: 'Add monitoring features',
            critical: false
        });
    }

    /**
     * Get available migrations
     */
    getAvailableMigrations() {
        const migrations = [];
        for (const [key, migration] of this.migrations) {
            migrations.push({
                key,
                fromVersion: migration.fromVersion,
                toVersion: migration.toVersion,
                description: migration.description,
                critical: migration.critical,
                registeredAt: migration.registeredAt
            });
        }
        return migrations.sort((a, b) => a.fromVersion.localeCompare(b.fromVersion));
    }

    /**
     * Get migration statistics
     */
    getMigrationStats() {
        return {
            totalMigrations: this.migrations.size,
            cacheSize: this.migrationCache.size,
            autoMigrate: this.autoMigrate,
            backupBeforeMigration: this.backupBeforeMigration,
            maxMigrationSteps: this.maxMigrationSteps
        };
    }

    /**
     * Calculate configuration checksum
     */
    calculateChecksum(config) {
        const configString = JSON.stringify(config, Object.keys(config).sort());
        return crypto.createHash('sha256').update(configString).digest('hex');
    }

    /**
     * Clear migration cache
     */
    clearCache() {
        this.migrationCache.clear();
        console.log('✓ Migration cache cleared');
    }

    /**
     * Get version compatibility matrix
     */
    getCompatibilityMatrix() {
        const matrix = {};
        const versions = new Set();
        
        for (const migration of this.migrations.values()) {
            versions.add(migration.fromVersion);
            versions.add(migration.toVersion);
        }
        
        const sortedVersions = Array.from(versions).sort();
        
        for (const fromVersion of sortedVersions) {
            matrix[fromVersion] = {};
            for (const toVersion of sortedVersions) {
                const migrationKey = `${fromVersion}_${toVersion}`;
                matrix[fromVersion][toVersion] = this.migrations.has(migrationKey);
            }
        }
        
        return matrix;
    }
}

module.exports = { MigrationManager }; 