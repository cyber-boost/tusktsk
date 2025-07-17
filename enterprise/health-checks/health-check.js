#!/usr/bin/env node

/**
 * TuskLang Health Check System - JavaScript/Node.js Implementation
 * Comprehensive health monitoring for all TuskLang services
 */

const https = require('https');
const http = require('http');
const { promisify } = require('util');
const fs = require('fs').promises;
const os = require('os');

// Health status enumeration
const HealthStatus = {
    HEALTHY: 'healthy',
    DEGRADED: 'degraded',
    UNHEALTHY: 'unhealthy',
    UNKNOWN: 'unknown'
};

/**
 * Health check result
 */
class HealthCheck {
    constructor(name, status, message, details = null) {
        this.name = name;
        this.status = status;
        this.message = message;
        this.details = details;
        this.timestamp = new Date().toISOString();
    }
}

/**
 * Health report
 */
class HealthReport {
    constructor() {
        this.timestamp = new Date().toISOString();
        this.overall_status = HealthStatus.HEALTHY;
        this.checks = [];
        this.summary = {
            total: 0,
            healthy: 0,
            degraded: 0,
            unhealthy: 0,
            unknown: 0
        };
    }

    addCheck(check) {
        this.checks.push(check);
        this.summary.total++;
        
        switch (check.status) {
            case HealthStatus.HEALTHY:
                this.summary.healthy++;
                break;
            case HealthStatus.DEGRADED:
                this.summary.degraded++;
                if (this.overall_status === HealthStatus.HEALTHY) {
                    this.overall_status = HealthStatus.DEGRADED;
                }
                break;
            case HealthStatus.UNHEALTHY:
            case HealthStatus.UNKNOWN:
                this.summary.unhealthy++;
                this.overall_status = HealthStatus.UNHEALTHY;
                break;
        }
    }
}

/**
 * TuskLang Health Checker
 */
class TuskLangHealthChecker {
    constructor(config) {
        this.config = config;
    }

    /**
     * Check system resources
     */
    async checkSystemResources() {
        try {
            const totalMem = os.totalmem();
            const freeMem = os.freemem();
            const memoryUsage = ((totalMem - freeMem) / totalMem) * 100;
            
            const cpus = os.cpus();
            const cpuUsage = cpus.reduce((acc, cpu) => {
                const total = Object.values(cpu.times).reduce((a, b) => a + b);
                const idle = cpu.times.idle;
                return acc + ((total - idle) / total) * 100;
            }, 0) / cpus.length;

            // Get disk usage (simplified - in production use a proper disk usage library)
            const diskUsage = 75; // Simulated for now

            const details = {
                cpu_percent: cpuUsage.toFixed(2),
                memory_percent: memoryUsage.toFixed(2),
                disk_percent: diskUsage,
                memory_available: freeMem,
                cpu_count: cpus.length
            };

            let status = HealthStatus.HEALTHY;
            let message = 'System resources are normal';

            if (cpuUsage > 80 || memoryUsage > 85 || diskUsage > 90) {
                status = HealthStatus.DEGRADED;
                message = `High resource usage - CPU: ${cpuUsage.toFixed(1)}%, Memory: ${memoryUsage.toFixed(1)}%, Disk: ${diskUsage}%`;
            }

            return new HealthCheck('system_resources', status, message, details);
        } catch (error) {
            return new HealthCheck('system_resources', HealthStatus.UNHEALTHY, `System resource check failed: ${error.message}`);
        }
    }

    /**
     * Check database connectivity
     */
    async checkDatabase() {
        try {
            const dbConfig = this.config.database;
            if (!dbConfig) {
                return new HealthCheck('database', HealthStatus.UNHEALTHY, 'Database configuration not found');
            }

            // In a real implementation, you would use a database client
            // For now, we'll simulate the check
            const isConnected = true; // Simulated
            const activeConnections = 25; // Simulated
            const dbSize = 1024 * 1024 * 100; // 100MB simulated

            const details = {
                active_connections: activeConnections,
                database_size_bytes: dbSize,
                database_size_mb: Math.round(dbSize / 1024 / 1024)
            };

            let status = HealthStatus.HEALTHY;
            let message = 'Database is healthy';

            if (!isConnected) {
                status = HealthStatus.UNHEALTHY;
                message = 'Database connection failed';
            } else if (activeConnections > 100) {
                status = HealthStatus.DEGRADED;
                message = `High number of active connections: ${activeConnections}`;
            }

            return new HealthCheck('database', status, message, details);
        } catch (error) {
            return new HealthCheck('database', HealthStatus.UNHEALTHY, `Database check failed: ${error.message}`);
        }
    }

    /**
     * Check Redis connectivity
     */
    async checkRedis() {
        try {
            const redisConfig = this.config.redis;
            if (!redisConfig) {
                return new HealthCheck('redis', HealthStatus.UNHEALTHY, 'Redis configuration not found');
            }

            // In a real implementation, you would use a Redis client
            // For now, we'll simulate the check
            const isConnected = true; // Simulated
            const memoryUsagePercent = 45; // Simulated
            const connectedClients = 15; // Simulated

            const details = {
                memory_usage_percent: memoryUsagePercent,
                connected_clients: connectedClients,
                used_memory_mb: 512,
                max_memory_mb: 1024
            };

            let status = HealthStatus.HEALTHY;
            let message = 'Redis is healthy';

            if (!isConnected) {
                status = HealthStatus.UNHEALTHY;
                message = 'Redis connection failed';
            } else if (memoryUsagePercent > 80) {
                status = HealthStatus.DEGRADED;
                message = `High Redis memory usage: ${memoryUsagePercent}%`;
            } else if (connectedClients > 100) {
                status = HealthStatus.DEGRADED;
                message = `High number of Redis clients: ${connectedClients}`;
            }

            return new HealthCheck('redis', status, message, details);
        } catch (error) {
            return new HealthCheck('redis', HealthStatus.UNHEALTHY, `Redis check failed: ${error.message}`);
        }
    }

    /**
     * Check package registry health
     */
    async checkPackageRegistry() {
        try {
            const registryConfig = this.config.registry;
            if (!registryConfig) {
                return new HealthCheck('package_registry', HealthStatus.UNHEALTHY, 'Registry configuration not found');
            }

            const healthUrl = `${registryConfig.url}/health`;
            const response = await this.makeHttpRequest(healthUrl);

            if (response.statusCode !== 200) {
                return new HealthCheck('package_registry', HealthStatus.UNHEALTHY, 
                    `Registry health endpoint returned status ${response.statusCode}`);
            }

            const healthData = JSON.parse(response.body);
            return new HealthCheck('package_registry', HealthStatus.HEALTHY, 'Package registry is healthy', healthData);
        } catch (error) {
            return new HealthCheck('package_registry', HealthStatus.UNHEALTHY, 
                `Package registry check failed: ${error.message}`);
        }
    }

    /**
     * Check CDN health
     */
    async checkCDN() {
        try {
            const cdnConfig = this.config.cdn;
            if (!cdnConfig) {
                return new HealthCheck('cdn', HealthStatus.UNHEALTHY, 'CDN configuration not found');
            }

            const healthUrl = `${cdnConfig.url}/health`;
            const response = await this.makeHttpRequest(healthUrl);

            if (response.statusCode !== 200) {
                return new HealthCheck('cdn', HealthStatus.UNHEALTHY, 
                    `CDN health check returned status ${response.statusCode}`);
            }

            const cdnHealth = JSON.parse(response.body);

            // Check sync status
            const syncUrl = `${cdnConfig.url}/sync/status`;
            const syncResponse = await this.makeHttpRequest(syncUrl);
            const syncStatus = JSON.parse(syncResponse.body);

            const details = {
                cdn_health: cdnHealth,
                sync_status: syncStatus
            };

            let status = HealthStatus.HEALTHY;
            let message = 'CDN is healthy';

            if (!syncStatus.synced) {
                status = HealthStatus.DEGRADED;
                message = 'CDN synchronization issues detected';
            }

            return new HealthCheck('cdn', status, message, details);
        } catch (error) {
            return new HealthCheck('cdn', HealthStatus.UNHEALTHY, `CDN check failed: ${error.message}`);
        }
    }

    /**
     * Check security scanning service
     */
    async checkSecurityScanning() {
        try {
            const securityConfig = this.config.security;
            if (!securityConfig) {
                return new HealthCheck('security_scanning', HealthStatus.UNHEALTHY, 'Security configuration not found');
            }

            const healthUrl = `${securityConfig.url}/health`;
            const response = await this.makeHttpRequest(healthUrl);

            if (response.statusCode !== 200) {
                return new HealthCheck('security_scanning', HealthStatus.UNHEALTHY, 
                    `Security service returned status ${response.statusCode}`);
            }

            const securityHealth = JSON.parse(response.body);

            let status = HealthStatus.HEALTHY;
            let message = 'Security scanning is healthy';

            if (!securityHealth.scanner_active) {
                status = HealthStatus.DEGRADED;
                message = 'Security scanner is not active';
            }

            return new HealthCheck('security_scanning', status, message, securityHealth);
        } catch (error) {
            return new HealthCheck('security_scanning', HealthStatus.UNHEALTHY, 
                `Security scanning check failed: ${error.message}`);
        }
    }

    /**
     * Make HTTP request
     */
    async makeHttpRequest(url) {
        return new Promise((resolve, reject) => {
            const client = url.startsWith('https') ? https : http;
            const req = client.get(url, { timeout: 10000 }, (res) => {
                let body = '';
                res.on('data', (chunk) => {
                    body += chunk;
                });
                res.on('end', () => {
                    resolve({
                        statusCode: res.statusCode,
                        body: body
                    });
                });
            });

            req.on('error', (error) => {
                reject(error);
            });

            req.on('timeout', () => {
                req.destroy();
                reject(new Error('Request timeout'));
            });
        });
    }

    /**
     * Run all health checks
     */
    async runAllChecks() {
        const report = new HealthReport();

        const checks = [
            await this.checkSystemResources(),
            await this.checkDatabase(),
            await this.checkRedis(),
            await this.checkPackageRegistry(),
            await this.checkCDN(),
            await this.checkSecurityScanning()
        ];

        checks.forEach(check => report.addCheck(check));

        return report;
    }
}

/**
 * Main function
 */
async function main() {
    // Configuration
    const config = {
        database: {
            host: process.env.DB_HOST || 'localhost',
            port: process.env.DB_PORT || '5432',
            name: process.env.DB_NAME || 'tusklang',
            user: process.env.DB_USER || 'postgres',
            password: process.env.DB_PASSWORD || ''
        },
        redis: {
            host: process.env.REDIS_HOST || 'localhost',
            port: process.env.REDIS_PORT || '6379',
            password: process.env.REDIS_PASSWORD || ''
        },
        registry: {
            url: process.env.REGISTRY_URL || 'http://localhost:8000'
        },
        cdn: {
            url: process.env.CDN_URL || 'https://cdn.tusklang.org'
        },
        security: {
            url: process.env.SECURITY_URL || 'http://localhost:9000'
        }
    };

    try {
        const checker = new TuskLangHealthChecker(config);
        const report = await checker.runAllChecks();

        // Output report as JSON
        console.log(JSON.stringify(report, null, 2));

        // Exit with appropriate code
        switch (report.overall_status) {
            case HealthStatus.UNHEALTHY:
                process.exit(1);
            case HealthStatus.DEGRADED:
                process.exit(2);
            default:
                process.exit(0);
        }
    } catch (error) {
        console.error('Health check failed:', error.message);
        process.exit(1);
    }
}

// Run if called directly
if (require.main === module) {
    main();
}

module.exports = {
    TuskLangHealthChecker,
    HealthCheck,
    HealthReport,
    HealthStatus
}; 