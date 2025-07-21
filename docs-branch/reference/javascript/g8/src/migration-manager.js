class MigrationManager {
    constructor(options = {}) {
        this.migrations = new Map();
        this.autoMigrate = options.autoMigrate || true;
        this.backupBeforeMigration = options.backupBeforeMigration || true;
        this.registerBuiltInMigrations();
    }

    registerMigration(fromVersion, toVersion, migrationFunction, options = {}) {
        const migrationKey = `${fromVersion}_${toVersion}`;
        this.migrations.set(migrationKey, {
            fromVersion,
            toVersion,
            function: migrationFunction,
            description: options.description || `Migration from ${fromVersion} to ${toVersion}`,
            critical: options.critical || false,
            registeredAt: Date.now()
        });
        console.log(`✓ Migration registered: ${fromVersion} → ${toVersion}`);
        return true;
    }

    async migrate(config, targetVersion, options = {}) {
        const currentVersion = this.detectVersion(config);
        
        if (currentVersion === targetVersion) {
            return { config, migrated: false, version: targetVersion };
        }

        // Find migration path
        const migrationPath = this.findMigrationPath(currentVersion, targetVersion);
        if (!migrationPath) {
            throw new Error(`No migration path found from ${currentVersion} to ${targetVersion}`);
        }

        let currentConfig = { ...config };
        const migrationResults = [];

        for (const migrationStep of migrationPath) {
            const result = await this.executeMigration(currentConfig, migrationStep, options);
            currentConfig = result.config;
            migrationResults.push(result);
        }

        currentConfig._version = targetVersion;
        currentConfig._migratedAt = Date.now();

        return {
            config: currentConfig,
            migrated: true,
            version: targetVersion,
            migrationPath,
            migrationResults,
            executionTime: Date.now()
        };
    }

    executeMigration(config, migrationStep, options) {
        const migrationKey = `${migrationStep.fromVersion}_${migrationStep.toVersion}`;
        const migration = this.migrations.get(migrationKey);
        
        if (!migration) {
            throw new Error(`Migration not found: ${migrationKey}`);
        }

        const migratedConfig = migration.function(config, options);
        
        return {
            config: migratedConfig,
            fromVersion: migrationStep.fromVersion,
            toVersion: migrationStep.toVersion,
            description: migration.description,
            timestamp: Date.now(),
            critical: migration.critical
        };
    }

    findMigrationPath(fromVersion, toVersion) {
        const visited = new Set();
        const queue = [{ version: fromVersion, path: [] }];
        
        while (queue.length > 0) {
            const { version, path } = queue.shift();
            
            if (version === toVersion) {
                return path;
            }
            
            if (visited.has(version) || path.length >= 5) {
                continue;
            }
            
            visited.add(version);
            
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

    detectVersion(config) {
        if (config._version) {
            return config._version;
        }
        
        if (config.server && config.database) {
            return "1.0.0";
        } else if (config.api && config.services) {
            return "2.0.0";
        } else if (config.features && config.security) {
            return "3.0.0";
        }
        
        return "0.1.0";
    }

    registerBuiltInMigrations() {
        this.registerMigration("1.0.0", "2.0.0", (config, options) => {
            return {
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
                _migratedFrom: "1.0.0",
                _migratedAt: Date.now()
            };
        }, {
            description: "Restructure server/database to api/services format",
            critical: true
        });
    }

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

    getMigrationStats() {
        return {
            totalMigrations: this.migrations.size,
            autoMigrate: this.autoMigrate,
            backupBeforeMigration: this.backupBeforeMigration
        };
    }
}

module.exports = { MigrationManager };
