/**
 * TERRAFORM OPERATOR - Real Infrastructure as Code
 * Production-ready Terraform execution with comprehensive IaC operations
 * 
 * @author Agent A3 - Cloud Infrastructure Specialist
 * @version 1.0.0
 * @license MIT
 */

const { spawn, exec } = require('child_process');
const fs = require('fs');
const path = require('path');
const yaml = require('js-yaml');

// Performance and monitoring
const performanceMetrics = {
    operationCount: 0,
    totalLatency: 0,
    errorCount: 0,
    lastOperation: null,
    startTime: Date.now()
};

// Circuit breaker configuration
const circuitBreaker = {
    failureThreshold: 5,
    recoveryTimeout: 60000,
    failureCount: 0,
    lastFailureTime: null,
    state: 'CLOSED'
};

/**
 * Terraform Operator Configuration
 */
class TerraformOperatorConfig {
    constructor(options = {}) {
        this.terraformPath = options.terraformPath || process.env.TERRAFORM_PATH || 'terraform';
        this.workingDirectory = options.workingDirectory || process.env.TF_WORKING_DIR || '.';
        this.backendConfig = options.backendConfig || {};
        this.variables = options.variables || {};
        this.environment = options.environment || {};
        this.maxRetries = options.maxRetries || 3;
        this.requestTimeout = options.requestTimeout || 300000; // 5 minutes for Terraform
        this.enableMetrics = options.enableMetrics !== false;
        this.logLevel = options.logLevel || 'info';
        this.autoApprove = options.autoApprove || false;
        this.parallelism = options.parallelism || 10;
        this.refresh = options.refresh !== false;
    }

    validate() {
        if (!fs.existsSync(this.workingDirectory)) {
            throw new Error(`Terraform working directory not found: ${this.workingDirectory}`);
        }
        return true;
    }
}

/**
 * Terraform Service Manager
 */
class TerraformServiceManager {
    constructor(config) {
        this.config = config;
        this.terraformPath = config.terraformPath;
        this.workingDirectory = config.workingDirectory;
    }

    async executeCommand(command, args = [], options = {}) {
        return new Promise((resolve, reject) => {
            const timeout = options.timeout || this.config.requestTimeout;
            const env = { ...process.env, ...this.config.environment, ...options.env };
            
            const child = spawn(this.terraformPath, args, {
                cwd: this.workingDirectory,
                env,
                stdio: ['pipe', 'pipe', 'pipe']
            });

            let stdout = '';
            let stderr = '';

            child.stdout.on('data', (data) => {
                stdout += data.toString();
            });

            child.stderr.on('data', (data) => {
                stderr += data.toString();
            });

            const timeoutId = setTimeout(() => {
                child.kill('SIGTERM');
                reject(new Error(`Terraform command timed out after ${timeout}ms`));
            }, timeout);

            child.on('close', (code) => {
                clearTimeout(timeoutId);
                if (code === 0) {
                    resolve({ stdout, stderr, code });
                } else {
                    reject(new Error(`Terraform command failed with code ${code}: ${stderr}`));
                }
            });

            child.on('error', (error) => {
                clearTimeout(timeoutId);
                reject(error);
            });
        });
    }

    getTerraformPath() {
        return this.terraformPath;
    }

    getWorkingDirectory() {
        return this.workingDirectory;
    }
}

/**
 * Performance Monitoring and Metrics
 */
class TerraformMetrics {
    static recordOperation(operation, duration, success = true) {
        performanceMetrics.operationCount++;
        performanceMetrics.totalLatency += duration;
        performanceMetrics.lastOperation = {
            operation,
            duration,
            success,
            timestamp: Date.now()
        };

        if (!success) {
            performanceMetrics.errorCount++;
        }

        if (performanceMetrics.operationCount % 100 === 0) {
            this.logMetrics();
        }
    }

    static logMetrics() {
        const avgLatency = performanceMetrics.totalLatency / performanceMetrics.operationCount;
        const errorRate = (performanceMetrics.errorCount / performanceMetrics.operationCount) * 100;
        const uptime = Date.now() - performanceMetrics.startTime;

        console.log('Terraform Metrics:', {
            operationCount: performanceMetrics.operationCount,
            averageLatency: `${avgLatency.toFixed(2)}ms`,
            errorRate: `${errorRate.toFixed(2)}%`,
            uptime: `${(uptime / 1000 / 60).toFixed(2)} minutes`
        });
    }

    static getMetrics() {
        return { ...performanceMetrics };
    }
}

/**
 * Circuit Breaker Implementation
 */
class TerraformCircuitBreaker {
    static async execute(operation, fallback = null) {
        if (circuitBreaker.state === 'OPEN') {
            if (Date.now() - circuitBreaker.lastFailureTime > circuitBreaker.recoveryTimeout) {
                circuitBreaker.state = 'HALF_OPEN';
            } else {
                if (fallback) return fallback();
                throw new Error('Circuit breaker is OPEN - Terraform service unavailable');
            }
        }

        try {
            const result = await operation();
            if (circuitBreaker.state === 'HALF_OPEN') {
                circuitBreaker.state = 'CLOSED';
                circuitBreaker.failureCount = 0;
            }
            return result;
        } catch (error) {
            circuitBreaker.failureCount++;
            circuitBreaker.lastFailureTime = Date.now();

            if (circuitBreaker.failureCount >= circuitBreaker.failureThreshold) {
                circuitBreaker.state = 'OPEN';
            }

            throw error;
        }
    }
}

/**
 * Main Terraform Operator Class
 */
class TerraformOperator {
    constructor(config = {}) {
        this.config = new TerraformOperatorConfig(config);
        this.config.validate();
        this.serviceManager = new TerraformServiceManager(this.config);
        this.initialize();
    }

    async initialize() {
        try {
            const result = await this.serviceManager.executeCommand('version');
            const versionMatch = result.stdout.match(/Terraform v(\d+\.\d+\.\d+)/);
            const version = versionMatch ? versionMatch[1] : 'unknown';
            
            console.log('Terraform Operator initialized successfully');
            console.log('Terraform Version:', version);
            console.log('Working Directory:', this.config.workingDirectory);
        } catch (error) {
            console.error('Failed to initialize Terraform Operator:', error.message);
            throw error;
        }
    }

    /**
     * Terraform Init Operations
     */
    async init(options = {}) {
        const startTime = Date.now();
        try {
            const args = ['init'];
            
            if (options.backendConfig) {
                Object.entries(options.backendConfig).forEach(([key, value]) => {
                    args.push('-backend-config', `${key}=${value}`);
                });
            }

            if (options.reconfigure) {
                args.push('-reconfigure');
            }

            if (options.upgrade) {
                args.push('-upgrade');
            }

            const result = await TerraformCircuitBreaker.execute(() =>
                this.serviceManager.executeCommand('init', args, options)
            );

            TerraformMetrics.recordOperation('init', Date.now() - startTime, true);
            return result;
        } catch (error) {
            TerraformMetrics.recordOperation('init', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Terraform Plan Operations
     */
    async plan(options = {}) {
        const startTime = Date.now();
        try {
            const args = ['plan'];
            
            // Add variables
            Object.entries({ ...this.config.variables, ...options.variables }).forEach(([key, value]) => {
                args.push('-var', `${key}=${value}`);
            });

            // Add var-files
            if (options.varFiles) {
                options.varFiles.forEach(file => {
                    args.push('-var-file', file);
                });
            }

            // Add target resources
            if (options.targets) {
                options.targets.forEach(target => {
                    args.push('-target', target);
                });
            }

            // Add parallelism
            if (options.parallelism || this.config.parallelism) {
                args.push('-parallelism', options.parallelism || this.config.parallelism);
            }

            // Add refresh option
            if (options.refresh !== undefined ? options.refresh : this.config.refresh) {
                args.push('-refresh=true');
            } else {
                args.push('-refresh=false');
            }

            // Add output format
            if (options.out) {
                args.push('-out', options.out);
            }

            const result = await TerraformCircuitBreaker.execute(() =>
                this.serviceManager.executeCommand('plan', args, options)
            );

            TerraformMetrics.recordOperation('plan', Date.now() - startTime, true);
            return result;
        } catch (error) {
            TerraformMetrics.recordOperation('plan', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Terraform Apply Operations
     */
    async apply(options = {}) {
        const startTime = Date.now();
        try {
            const args = ['apply'];
            
            // Add auto-approve
            if (options.autoApprove !== undefined ? options.autoApprove : this.config.autoApprove) {
                args.push('-auto-approve');
            }

            // Add variables
            Object.entries({ ...this.config.variables, ...options.variables }).forEach(([key, value]) => {
                args.push('-var', `${key}=${value}`);
            });

            // Add var-files
            if (options.varFiles) {
                options.varFiles.forEach(file => {
                    args.push('-var-file', file);
                });
            }

            // Add target resources
            if (options.targets) {
                options.targets.forEach(target => {
                    args.push('-target', target);
                });
            }

            // Add parallelism
            if (options.parallelism || this.config.parallelism) {
                args.push('-parallelism', options.parallelism || this.config.parallelism);
            }

            // Add plan file
            if (options.planFile) {
                args.push(options.planFile);
            }

            const result = await TerraformCircuitBreaker.execute(() =>
                this.serviceManager.executeCommand('apply', args, options)
            );

            TerraformMetrics.recordOperation('apply', Date.now() - startTime, true);
            return result;
        } catch (error) {
            TerraformMetrics.recordOperation('apply', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Terraform Destroy Operations
     */
    async destroy(options = {}) {
        const startTime = Date.now();
        try {
            const args = ['destroy'];
            
            // Add auto-approve
            if (options.autoApprove !== undefined ? options.autoApprove : this.config.autoApprove) {
                args.push('-auto-approve');
            }

            // Add variables
            Object.entries({ ...this.config.variables, ...options.variables }).forEach(([key, value]) => {
                args.push('-var', `${key}=${value}`);
            });

            // Add var-files
            if (options.varFiles) {
                options.varFiles.forEach(file => {
                    args.push('-var-file', file);
                });
            }

            // Add target resources
            if (options.targets) {
                options.targets.forEach(target => {
                    args.push('-target', target);
                });
            }

            // Add parallelism
            if (options.parallelism || this.config.parallelism) {
                args.push('-parallelism', options.parallelism || this.config.parallelism);
            }

            const result = await TerraformCircuitBreaker.execute(() =>
                this.serviceManager.executeCommand('destroy', args, options)
            );

            TerraformMetrics.recordOperation('destroy', Date.now() - startTime, true);
            return result;
        } catch (error) {
            TerraformMetrics.recordOperation('destroy', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Terraform State Operations
     */
    async listState() {
        const startTime = Date.now();
        try {
            const result = await TerraformCircuitBreaker.execute(() =>
                this.serviceManager.executeCommand('state', ['list'])
            );

            const resources = result.stdout.split('\n')
                .filter(line => line.trim())
                .map(line => ({
                    address: line.trim(),
                    type: line.split('.')[0],
                    name: line.split('.')[1] || ''
                }));

            TerraformMetrics.recordOperation('listState', Date.now() - startTime, true);
            return { resources, raw: result.stdout };
        } catch (error) {
            TerraformMetrics.recordOperation('listState', Date.now() - startTime, false);
            throw error;
        }
    }

    async showState(resourceAddress) {
        const startTime = Date.now();
        try {
            const args = ['state', 'show', resourceAddress];
            const result = await TerraformCircuitBreaker.execute(() =>
                this.serviceManager.executeCommand('show', args)
            );

            TerraformMetrics.recordOperation('showState', Date.now() - startTime, true);
            return result;
        } catch (error) {
            TerraformMetrics.recordOperation('showState', Date.now() - startTime, false);
            throw error;
        }
    }

    async removeFromState(resourceAddress) {
        const startTime = Date.now();
        try {
            const args = ['state', 'rm', resourceAddress];
            const result = await TerraformCircuitBreaker.execute(() =>
                this.serviceManager.executeCommand('state', args)
            );

            TerraformMetrics.recordOperation('removeFromState', Date.now() - startTime, true);
            return result;
        } catch (error) {
            TerraformMetrics.recordOperation('removeFromState', Date.now() - startTime, false);
            throw error;
        }
    }

    async importResource(resourceAddress, resourceId) {
        const startTime = Date.now();
        try {
            const args = ['import', resourceAddress, resourceId];
            const result = await TerraformCircuitBreaker.execute(() =>
                this.serviceManager.executeCommand('import', args)
            );

            TerraformMetrics.recordOperation('importResource', Date.now() - startTime, true);
            return result;
        } catch (error) {
            TerraformMetrics.recordOperation('importResource', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Terraform Output Operations
     */
    async getOutputs(options = {}) {
        const startTime = Date.now();
        try {
            const args = ['output', '-json'];
            
            if (options.name) {
                args.push(options.name);
            }

            const result = await TerraformCircuitBreaker.execute(() =>
                this.serviceManager.executeCommand('output', args)
            );

            const outputs = JSON.parse(result.stdout);

            TerraformMetrics.recordOperation('getOutputs', Date.now() - startTime, true);
            return outputs;
        } catch (error) {
            TerraformMetrics.recordOperation('getOutputs', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Terraform Workspace Operations
     */
    async listWorkspaces() {
        const startTime = Date.now();
        try {
            const result = await TerraformCircuitBreaker.execute(() =>
                this.serviceManager.executeCommand('workspace', ['list'])
            );

            const workspaces = result.stdout.split('\n')
                .filter(line => line.trim())
                .map(line => ({
                    name: line.replace('*', '').trim(),
                    current: line.includes('*')
                }));

            TerraformMetrics.recordOperation('listWorkspaces', Date.now() - startTime, true);
            return workspaces;
        } catch (error) {
            TerraformMetrics.recordOperation('listWorkspaces', Date.now() - startTime, false);
            throw error;
        }
    }

    async createWorkspace(workspaceName) {
        const startTime = Date.now();
        try {
            const args = ['workspace', 'new', workspaceName];
            const result = await TerraformCircuitBreaker.execute(() =>
                this.serviceManager.executeCommand('workspace', args)
            );

            TerraformMetrics.recordOperation('createWorkspace', Date.now() - startTime, true);
            return result;
        } catch (error) {
            TerraformMetrics.recordOperation('createWorkspace', Date.now() - startTime, false);
            throw error;
        }
    }

    async selectWorkspace(workspaceName) {
        const startTime = Date.now();
        try {
            const args = ['workspace', 'select', workspaceName];
            const result = await TerraformCircuitBreaker.execute(() =>
                this.serviceManager.executeCommand('workspace', args)
            );

            TerraformMetrics.recordOperation('selectWorkspace', Date.now() - startTime, true);
            return result;
        } catch (error) {
            TerraformMetrics.recordOperation('selectWorkspace', Date.now() - startTime, false);
            throw error;
        }
    }

    async deleteWorkspace(workspaceName) {
        const startTime = Date.now();
        try {
            const args = ['workspace', 'delete', workspaceName];
            const result = await TerraformCircuitBreaker.execute(() =>
                this.serviceManager.executeCommand('workspace', args)
            );

            TerraformMetrics.recordOperation('deleteWorkspace', Date.now() - startTime, true);
            return result;
        } catch (error) {
            TerraformMetrics.recordOperation('deleteWorkspace', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Terraform Validate Operations
     */
    async validate(options = {}) {
        const startTime = Date.now();
        try {
            const args = ['validate'];
            
            if (options.json) {
                args.push('-json');
            }

            const result = await TerraformCircuitBreaker.execute(() =>
                this.serviceManager.executeCommand('validate', args, options)
            );

            TerraformMetrics.recordOperation('validate', Date.now() - startTime, true);
            return result;
        } catch (error) {
            TerraformMetrics.recordOperation('validate', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Terraform Format Operations
     */
    async format(options = {}) {
        const startTime = Date.now();
        try {
            const args = ['fmt'];
            
            if (options.check) {
                args.push('-check');
            }

            if (options.diff) {
                args.push('-diff');
            }

            if (options.recursive) {
                args.push('-recursive');
            }

            if (options.write) {
                args.push('-write');
            }

            const result = await TerraformCircuitBreaker.execute(() =>
                this.serviceManager.executeCommand('fmt', args, options)
            );

            TerraformMetrics.recordOperation('format', Date.now() - startTime, true);
            return result;
        } catch (error) {
            TerraformMetrics.recordOperation('format', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Terraform Refresh Operations
     */
    async refresh(options = {}) {
        const startTime = Date.now();
        try {
            const args = ['refresh'];
            
            // Add variables
            Object.entries({ ...this.config.variables, ...options.variables }).forEach(([key, value]) => {
                args.push('-var', `${key}=${value}`);
            });

            // Add var-files
            if (options.varFiles) {
                options.varFiles.forEach(file => {
                    args.push('-var-file', file);
                });
            }

            // Add target resources
            if (options.targets) {
                options.targets.forEach(target => {
                    args.push('-target', target);
                });
            }

            const result = await TerraformCircuitBreaker.execute(() =>
                this.serviceManager.executeCommand('refresh', args, options)
            );

            TerraformMetrics.recordOperation('refresh', Date.now() - startTime, true);
            return result;
        } catch (error) {
            TerraformMetrics.recordOperation('refresh', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Terraform Taint Operations
     */
    async taint(resourceAddress) {
        const startTime = Date.now();
        try {
            const args = ['taint', resourceAddress];
            const result = await TerraformCircuitBreaker.execute(() =>
                this.serviceManager.executeCommand('taint', args)
            );

            TerraformMetrics.recordOperation('taint', Date.now() - startTime, true);
            return result;
        } catch (error) {
            TerraformMetrics.recordOperation('taint', Date.now() - startTime, false);
            throw error;
        }
    }

    async untaint(resourceAddress) {
        const startTime = Date.now();
        try {
            const args = ['untaint', resourceAddress];
            const result = await TerraformCircuitBreaker.execute(() =>
                this.serviceManager.executeCommand('untaint', args)
            );

            TerraformMetrics.recordOperation('untaint', Date.now() - startTime, true);
            return result;
        } catch (error) {
            TerraformMetrics.recordOperation('untaint', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Configuration Management
     */
    async createTerraformConfig(configData) {
        const startTime = Date.now();
        try {
            const configPath = path.join(this.config.workingDirectory, 'main.tf');
            let configContent = '';

            // Generate provider configuration
            if (configData.providers) {
                Object.entries(configData.providers).forEach(([name, provider]) => {
                    configContent += `provider "${name}" {\n`;
                    Object.entries(provider).forEach(([key, value]) => {
                        configContent += `  ${key} = "${value}"\n`;
                    });
                    configContent += '}\n\n';
                });
            }

            // Generate resource configurations
            if (configData.resources) {
                Object.entries(configData.resources).forEach(([type, resources]) => {
                    Object.entries(resources).forEach(([name, resource]) => {
                        configContent += `resource "${type}" "${name}" {\n`;
                        Object.entries(resource).forEach(([key, value) => {
                            if (typeof value === 'object') {
                                configContent += `  ${key} {\n`;
                                Object.entries(value).forEach(([subKey, subValue]) => {
                                    configContent += `    ${subKey} = "${subValue}"\n`;
                                });
                                configContent += `  }\n`;
                            } else {
                                configContent += `  ${key} = "${value}"\n`;
                            }
                        });
                        configContent += '}\n\n';
                    });
                });
            }

            // Generate output configurations
            if (configData.outputs) {
                Object.entries(configData.outputs).forEach(([name, output]) => {
                    configContent += `output "${name}" {\n`;
                    configContent += `  value = ${output.value}\n`;
                    if (output.description) {
                        configContent += `  description = "${output.description}"\n`;
                    }
                    configContent += '}\n\n';
                });
            }

            fs.writeFileSync(configPath, configContent);

            TerraformMetrics.recordOperation('createTerraformConfig', Date.now() - startTime, true);
            return { configPath, configContent };
        } catch (error) {
            TerraformMetrics.recordOperation('createTerraformConfig', Date.now() - startTime, false);
            throw error;
        }
    }

    async createVariablesFile(variables) {
        const startTime = Date.now();
        try {
            const variablesPath = path.join(this.config.workingDirectory, 'variables.tf');
            let variablesContent = '';

            Object.entries(variables).forEach(([name, variable]) => {
                variablesContent += `variable "${name}" {\n`;
                if (variable.description) {
                    variablesContent += `  description = "${variable.description}"\n`;
                }
                if (variable.type) {
                    variablesContent += `  type = ${variable.type}\n`;
                }
                if (variable.default !== undefined) {
                    variablesContent += `  default = ${JSON.stringify(variable.default)}\n`;
                }
                variablesContent += '}\n\n';
            });

            fs.writeFileSync(variablesPath, variablesContent);

            TerraformMetrics.recordOperation('createVariablesFile', Date.now() - startTime, true);
            return { variablesPath, variablesContent };
        } catch (error) {
            TerraformMetrics.recordOperation('createVariablesFile', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Drift Detection
     */
    async detectDrift(options = {}) {
        const startTime = Date.now();
        try {
            // Run plan to detect drift
            const planResult = await this.plan({
                ...options,
                detailedExitCode: true
            });

            // Parse plan output for drift detection
            const driftAnalysis = this.analyzePlanOutput(planResult.stdout);

            TerraformMetrics.recordOperation('detectDrift', Date.now() - startTime, true);
            return driftAnalysis;
        } catch (error) {
            TerraformMetrics.recordOperation('detectDrift', Date.now() - startTime, false);
            throw error;
        }
    }

    analyzePlanOutput(planOutput) {
        const analysis = {
            resourcesToAdd: 0,
            resourcesToChange: 0,
            resourcesToDestroy: 0,
            changes: []
        };

        const lines = planOutput.split('\n');
        let currentResource = null;

        for (const line of lines) {
            if (line.includes('+ create')) {
                analysis.resourcesToAdd++;
                currentResource = { action: 'create', resource: line.trim() };
            } else if (line.includes('~ update')) {
                analysis.resourcesToChange++;
                currentResource = { action: 'update', resource: line.trim() };
            } else if (line.includes('- destroy')) {
                analysis.resourcesToDestroy++;
                currentResource = { action: 'destroy', resource: line.trim() };
            } else if (currentResource && line.includes('+') || line.includes('-') || line.includes('~')) {
                currentResource.changes = currentResource.changes || [];
                currentResource.changes.push(line.trim());
            }

            if (currentResource && !line.includes('+') && !line.includes('-') && !line.includes('~')) {
                if (currentResource.changes) {
                    analysis.changes.push(currentResource);
                }
                currentResource = null;
            }
        }

        return analysis;
    }

    /**
     * Utility Methods
     */
    getMetrics() {
        return TerraformMetrics.getMetrics();
    }

    getCircuitBreakerStatus() {
        return { ...circuitBreaker };
    }

    async cleanup() {
        console.log('Terraform Operator cleanup completed');
    }
}

/**
 * Main execution function for TuskLang integration
 */
async function executeTerraformOperator(operation, params = {}) {
    const startTime = Date.now();
    
    try {
        const terraformOperator = new TerraformOperator(params.config);
        
        let result;
        switch (operation) {
            case 'init':
                result = await terraformOperator.init(params.options);
                break;
            case 'plan':
                result = await terraformOperator.plan(params.options);
                break;
            case 'apply':
                result = await terraformOperator.apply(params.options);
                break;
            case 'destroy':
                result = await terraformOperator.destroy(params.options);
                break;
            case 'listState':
                result = await terraformOperator.listState();
                break;
            case 'showState':
                result = await terraformOperator.showState(params.resourceAddress);
                break;
            case 'removeFromState':
                result = await terraformOperator.removeFromState(params.resourceAddress);
                break;
            case 'importResource':
                result = await terraformOperator.importResource(params.resourceAddress, params.resourceId);
                break;
            case 'getOutputs':
                result = await terraformOperator.getOutputs(params.options);
                break;
            case 'listWorkspaces':
                result = await terraformOperator.listWorkspaces();
                break;
            case 'createWorkspace':
                result = await terraformOperator.createWorkspace(params.workspaceName);
                break;
            case 'selectWorkspace':
                result = await terraformOperator.selectWorkspace(params.workspaceName);
                break;
            case 'deleteWorkspace':
                result = await terraformOperator.deleteWorkspace(params.workspaceName);
                break;
            case 'validate':
                result = await terraformOperator.validate(params.options);
                break;
            case 'format':
                result = await terraformOperator.format(params.options);
                break;
            case 'refresh':
                result = await terraformOperator.refresh(params.options);
                break;
            case 'taint':
                result = await terraformOperator.taint(params.resourceAddress);
                break;
            case 'untaint':
                result = await terraformOperator.untaint(params.resourceAddress);
                break;
            case 'createTerraformConfig':
                result = await terraformOperator.createTerraformConfig(params.configData);
                break;
            case 'createVariablesFile':
                result = await terraformOperator.createVariablesFile(params.variables);
                break;
            case 'detectDrift':
                result = await terraformOperator.detectDrift(params.options);
                break;
            case 'getMetrics':
                result = terraformOperator.getMetrics();
                break;
            case 'getCircuitBreakerStatus':
                result = terraformOperator.getCircuitBreakerStatus();
                break;
            default:
                throw new Error(`Unsupported Terraform operation: ${operation}`);
        }

        const duration = Date.now() - startTime;
        console.log(`Terraform Operation '${operation}' completed in ${duration}ms`);
        
        await terraformOperator.cleanup();
        return {
            success: true,
            operation,
            duration,
            result,
            timestamp: new Date().toISOString()
        };
    } catch (error) {
        const duration = Date.now() - startTime;
        console.error(`Terraform Operation '${operation}' failed after ${duration}ms:`, error.message);
        
        return {
            success: false,
            operation,
            duration,
            error: error.message,
            timestamp: new Date().toISOString()
        };
    }
}

module.exports = {
    TerraformOperator,
    executeTerraformOperator,
    TerraformOperatorConfig,
    TerraformMetrics,
    TerraformCircuitBreaker
}; 