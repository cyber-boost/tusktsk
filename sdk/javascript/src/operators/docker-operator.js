/**
 * DOCKER OPERATOR - Real Container Management
 * Production-ready Docker Engine API integration with comprehensive container operations
 * 
 * @author Agent A3 - Cloud Infrastructure Specialist
 * @version 1.0.0
 * @license MIT
 */

const Docker = require('dockerode');
const tar = require('tar-fs');
const fs = require('fs');
const path = require('path');
const { Readable } = require('stream');

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
 * Docker Operator Configuration
 */
class DockerOperatorConfig {
    constructor(options = {}) {
        this.socketPath = options.socketPath || '/var/run/docker.sock';
        this.host = options.host || process.env.DOCKER_HOST;
        this.port = options.port || process.env.DOCKER_PORT || 2375;
        this.maxRetries = options.maxRetries || 3;
        this.requestTimeout = options.requestTimeout || 60000;
        this.enableMetrics = options.enableMetrics !== false;
        this.logLevel = options.logLevel || 'info';
        this.registry = options.registry || 'docker.io';
        this.username = options.username || process.env.DOCKER_USERNAME;
        this.password = options.password || process.env.DOCKER_PASSWORD;
        this.labels = options.labels || {};
    }

    validate() {
        if (!this.socketPath && !this.host) {
            throw new Error('Docker socket path or host is required');
        }
        return true;
    }
}

/**
 * Docker Service Manager
 */
class DockerServiceManager {
    constructor(config) {
        this.config = config;
        this.docker = this.createDockerClient();
    }

    createDockerClient() {
        const options = {
            timeout: this.config.requestTimeout
        };

        if (this.config.socketPath) {
            options.socketPath = this.config.socketPath;
        } else {
            options.host = this.config.host;
            options.port = this.config.port;
        }

        return new Docker(options);
    }

    getDocker() {
        return this.docker;
    }
}

/**
 * Performance Monitoring and Metrics
 */
class DockerMetrics {
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

        console.log('Docker Metrics:', {
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
class DockerCircuitBreaker {
    static async execute(operation, fallback = null) {
        if (circuitBreaker.state === 'OPEN') {
            if (Date.now() - circuitBreaker.lastFailureTime > circuitBreaker.recoveryTimeout) {
                circuitBreaker.state = 'HALF_OPEN';
            } else {
                if (fallback) return fallback();
                throw new Error('Circuit breaker is OPEN - Docker service unavailable');
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
 * Main Docker Operator Class
 */
class DockerOperator {
    constructor(config = {}) {
        this.config = new DockerOperatorConfig(config);
        this.config.validate();
        this.serviceManager = new DockerServiceManager(this.config);
        this.initialize();
    }

    async initialize() {
        try {
            const docker = this.serviceManager.getDocker();
            const info = await docker.info();
            
            console.log('Docker Operator initialized successfully');
            console.log('Docker Version:', info.Version);
            console.log('Containers:', info.Containers);
            console.log('Images:', info.Images);
        } catch (error) {
            console.error('Failed to initialize Docker Operator:', error.message);
            throw error;
        }
    }

    /**
     * Container Operations
     */
    async listContainers(options = {}) {
        const startTime = Date.now();
        try {
            const docker = this.serviceManager.getDocker();
            const containers = await DockerCircuitBreaker.execute(() =>
                docker.listContainers(options)
            );

            const containerList = containers.map(container => ({
                id: container.Id,
                names: container.Names,
                image: container.Image,
                imageId: container.ImageID,
                command: container.Command,
                created: container.Created,
                state: container.State,
                status: container.Status,
                ports: container.Ports,
                labels: container.Labels || {},
                networkSettings: container.NetworkSettings
            }));

            DockerMetrics.recordOperation('listContainers', Date.now() - startTime, true);
            return containerList;
        } catch (error) {
            DockerMetrics.recordOperation('listContainers', Date.now() - startTime, false);
            throw error;
        }
    }

    async createContainer(containerConfig) {
        const startTime = Date.now();
        try {
            const docker = this.serviceManager.getDocker();
            
            const container = await DockerCircuitBreaker.execute(() =>
                docker.createContainer({
                    Image: containerConfig.image,
                    Cmd: containerConfig.cmd || [],
                    Env: containerConfig.env || [],
                    ExposedPorts: containerConfig.exposedPorts || {},
                    HostConfig: {
                        PortBindings: containerConfig.portBindings || {},
                        Binds: containerConfig.volumeBinds || [],
                        Memory: containerConfig.memory || 0,
                        CpuShares: containerConfig.cpuShares || 0,
                        RestartPolicy: containerConfig.restartPolicy || { Name: 'no' }
                    },
                    Labels: {
                        ...this.config.labels,
                        ...containerConfig.labels
                    }
                })
            );

            DockerMetrics.recordOperation('createContainer', Date.now() - startTime, true);
            return container;
        } catch (error) {
            DockerMetrics.recordOperation('createContainer', Date.now() - startTime, false);
            throw error;
        }
    }

    async startContainer(containerId) {
        const startTime = Date.now();
        try {
            const docker = this.serviceManager.getDocker();
            const container = docker.getContainer(containerId);
            
            await DockerCircuitBreaker.execute(() => container.start());

            DockerMetrics.recordOperation('startContainer', Date.now() - startTime, true);
            return { success: true, containerId };
        } catch (error) {
            DockerMetrics.recordOperation('startContainer', Date.now() - startTime, false);
            throw error;
        }
    }

    async stopContainer(containerId, options = {}) {
        const startTime = Date.now();
        try {
            const docker = this.serviceManager.getDocker();
            const container = docker.getContainer(containerId);
            
            await DockerCircuitBreaker.execute(() => 
                container.stop({ t: options.timeout || 10 })
            );

            DockerMetrics.recordOperation('stopContainer', Date.now() - startTime, true);
            return { success: true, containerId };
        } catch (error) {
            DockerMetrics.recordOperation('stopContainer', Date.now() - startTime, false);
            throw error;
        }
    }

    async removeContainer(containerId, options = {}) {
        const startTime = Date.now();
        try {
            const docker = this.serviceManager.getDocker();
            const container = docker.getContainer(containerId);
            
            await DockerCircuitBreaker.execute(() => 
                container.remove({ 
                    force: options.force || false,
                    v: options.removeVolumes || false
                })
            );

            DockerMetrics.recordOperation('removeContainer', Date.now() - startTime, true);
            return { success: true, containerId };
        } catch (error) {
            DockerMetrics.recordOperation('removeContainer', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Image Operations
     */
    async listImages(options = {}) {
        const startTime = Date.now();
        try {
            const docker = this.serviceManager.getDocker();
            const images = await DockerCircuitBreaker.execute(() =>
                docker.listImages(options)
            );

            const imageList = images.map(image => ({
                id: image.Id,
                parentId: image.ParentId,
                repoTags: image.RepoTags || [],
                repoDigests: image.RepoDigests || [],
                created: image.Created,
                size: image.Size,
                virtualSize: image.VirtualSize,
                labels: image.Labels || {}
            }));

            DockerMetrics.recordOperation('listImages', Date.now() - startTime, true);
            return imageList;
        } catch (error) {
            DockerMetrics.recordOperation('listImages', Date.now() - startTime, false);
            throw error;
        }
    }

    async buildImage(buildConfig) {
        const startTime = Date.now();
        try {
            const docker = this.serviceManager.getDocker();
            
            const tarStream = tar.pack(buildConfig.context);
            const buildStream = await DockerCircuitBreaker.execute(() =>
                docker.buildImage(tarStream, {
                    t: buildConfig.tag,
                    dockerfile: buildConfig.dockerfile || 'Dockerfile',
                    buildargs: buildConfig.buildArgs || {},
                    labels: {
                        ...this.config.labels,
                        ...buildConfig.labels
                    }
                })
            );

            return new Promise((resolve, reject) => {
                let output = '';
                buildStream.on('data', (chunk) => {
                    const data = JSON.parse(chunk.toString());
                    if (data.stream) {
                        output += data.stream;
                    }
                });
                
                buildStream.on('end', () => {
                    DockerMetrics.recordOperation('buildImage', Date.now() - startTime, true);
                    resolve({ success: true, output });
                });
                
                buildStream.on('error', (error) => {
                    DockerMetrics.recordOperation('buildImage', Date.now() - startTime, false);
                    reject(error);
                });
            });
        } catch (error) {
            DockerMetrics.recordOperation('buildImage', Date.now() - startTime, false);
            throw error;
        }
    }

    async pullImage(imageName, options = {}) {
        const startTime = Date.now();
        try {
            const docker = this.serviceManager.getDocker();
            
            const pullStream = await DockerCircuitBreaker.execute(() =>
                docker.pull(imageName, options)
            );

            return new Promise((resolve, reject) => {
                let output = '';
                pullStream.on('data', (chunk) => {
                    const data = JSON.parse(chunk.toString());
                    if (data.status) {
                        output += data.status + '\n';
                    }
                });
                
                pullStream.on('end', () => {
                    DockerMetrics.recordOperation('pullImage', Date.now() - startTime, true);
                    resolve({ success: true, output });
                });
                
                pullStream.on('error', (error) => {
                    DockerMetrics.recordOperation('pullImage', Date.now() - startTime, false);
                    reject(error);
                });
            });
        } catch (error) {
            DockerMetrics.recordOperation('pullImage', Date.now() - startTime, false);
            throw error;
        }
    }

    async removeImage(imageId, options = {}) {
        const startTime = Date.now();
        try {
            const docker = this.serviceManager.getDocker();
            const image = docker.getImage(imageId);
            
            await DockerCircuitBreaker.execute(() => 
                image.remove({ force: options.force || false })
            );

            DockerMetrics.recordOperation('removeImage', Date.now() - startTime, true);
            return { success: true, imageId };
        } catch (error) {
            DockerMetrics.recordOperation('removeImage', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Volume Operations
     */
    async listVolumes(options = {}) {
        const startTime = Date.now();
        try {
            const docker = this.serviceManager.getDocker();
            const volumes = await DockerCircuitBreaker.execute(() =>
                docker.listVolumes(options)
            );

            const volumeList = volumes.Volumes.map(volume => ({
                name: volume.Name,
                driver: volume.Driver,
                mountpoint: volume.Mountpoint,
                labels: volume.Labels || {},
                scope: volume.Scope
            }));

            DockerMetrics.recordOperation('listVolumes', Date.now() - startTime, true);
            return volumeList;
        } catch (error) {
            DockerMetrics.recordOperation('listVolumes', Date.now() - startTime, false);
            throw error;
        }
    }

    async createVolume(volumeConfig) {
        const startTime = Date.now();
        try {
            const docker = this.serviceManager.getDocker();
            
            const volume = await DockerCircuitBreaker.execute(() =>
                docker.createVolume({
                    Name: volumeConfig.name,
                    Driver: volumeConfig.driver || 'local',
                    Labels: {
                        ...this.config.labels,
                        ...volumeConfig.labels
                    }
                })
            );

            DockerMetrics.recordOperation('createVolume', Date.now() - startTime, true);
            return volume;
        } catch (error) {
            DockerMetrics.recordOperation('createVolume', Date.now() - startTime, false);
            throw error;
        }
    }

    async removeVolume(volumeName, options = {}) {
        const startTime = Date.now();
        try {
            const docker = this.serviceManager.getDocker();
            const volume = docker.getVolume(volumeName);
            
            await DockerCircuitBreaker.execute(() => 
                volume.remove({ force: options.force || false })
            );

            DockerMetrics.recordOperation('removeVolume', Date.now() - startTime, true);
            return { success: true, volumeName };
        } catch (error) {
            DockerMetrics.recordOperation('removeVolume', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Network Operations
     */
    async listNetworks(options = {}) {
        const startTime = Date.now();
        try {
            const docker = this.serviceManager.getDocker();
            const networks = await DockerCircuitBreaker.execute(() =>
                docker.listNetworks(options)
            );

            const networkList = networks.map(network => ({
                id: network.Id,
                name: network.Name,
                driver: network.Driver,
                scope: network.Scope,
                ipam: network.IPAM,
                internal: network.Internal,
                attachable: network.Attachable,
                labels: network.Labels || {}
            }));

            DockerMetrics.recordOperation('listNetworks', Date.now() - startTime, true);
            return networkList;
        } catch (error) {
            DockerMetrics.recordOperation('listNetworks', Date.now() - startTime, false);
            throw error;
        }
    }

    async createNetwork(networkConfig) {
        const startTime = Date.now();
        try {
            const docker = this.serviceManager.getDocker();
            
            const network = await DockerCircuitBreaker.execute(() =>
                docker.createNetwork({
                    Name: networkConfig.name,
                    Driver: networkConfig.driver || 'bridge',
                    IPAM: networkConfig.ipam || {
                        Config: [{
                            Subnet: networkConfig.subnet || '172.18.0.0/16'
                        }]
                    },
                    Internal: networkConfig.internal || false,
                    Attachable: networkConfig.attachable || false,
                    Labels: {
                        ...this.config.labels,
                        ...networkConfig.labels
                    }
                })
            );

            DockerMetrics.recordOperation('createNetwork', Date.now() - startTime, true);
            return network;
        } catch (error) {
            DockerMetrics.recordOperation('createNetwork', Date.now() - startTime, false);
            throw error;
        }
    }

    async removeNetwork(networkId) {
        const startTime = Date.now();
        try {
            const docker = this.serviceManager.getDocker();
            const network = docker.getNetwork(networkId);
            
            await DockerCircuitBreaker.execute(() => network.remove());

            DockerMetrics.recordOperation('removeNetwork', Date.now() - startTime, true);
            return { success: true, networkId };
        } catch (error) {
            DockerMetrics.recordOperation('removeNetwork', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Container Logs and Exec
     */
    async getContainerLogs(containerId, options = {}) {
        const startTime = Date.now();
        try {
            const docker = this.serviceManager.getDocker();
            const container = docker.getContainer(containerId);
            
            const logStream = await DockerCircuitBreaker.execute(() =>
                container.logs({
                    stdout: options.stdout !== false,
                    stderr: options.stderr !== false,
                    tail: options.tail || 'all',
                    follow: options.follow || false,
                    timestamps: options.timestamps || false
                })
            );

            return new Promise((resolve, reject) => {
                let logs = '';
                logStream.on('data', (chunk) => {
                    logs += chunk.toString();
                });
                
                logStream.on('end', () => {
                    DockerMetrics.recordOperation('getContainerLogs', Date.now() - startTime, true);
                    resolve(logs);
                });
                
                logStream.on('error', (error) => {
                    DockerMetrics.recordOperation('getContainerLogs', Date.now() - startTime, false);
                    reject(error);
                });
            });
        } catch (error) {
            DockerMetrics.recordOperation('getContainerLogs', Date.now() - startTime, false);
            throw error;
        }
    }

    async execCommand(containerId, command, options = {}) {
        const startTime = Date.now();
        try {
            const docker = this.serviceManager.getDocker();
            const container = docker.getContainer(containerId);
            
            const exec = await DockerCircuitBreaker.execute(() =>
                container.exec({
                    AttachStdout: options.attachStdout !== false,
                    AttachStderr: options.attachStderr !== false,
                    Tty: options.tty || false,
                    Cmd: Array.isArray(command) ? command : command.split(' ')
                })
            );

            const stream = await exec.start({ hijack: true, stdin: false });

            return new Promise((resolve, reject) => {
                let output = '';
                stream.on('data', (chunk) => {
                    output += chunk.toString();
                });
                
                stream.on('end', () => {
                    DockerMetrics.recordOperation('execCommand', Date.now() - startTime, true);
                    resolve(output);
                });
                
                stream.on('error', (error) => {
                    DockerMetrics.recordOperation('execCommand', Date.now() - startTime, false);
                    reject(error);
                });
            });
        } catch (error) {
            DockerMetrics.recordOperation('execCommand', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Docker Compose Operations
     */
    async runCompose(composeConfig) {
        const startTime = Date.now();
        try {
            const docker = this.serviceManager.getDocker();
            
            // Create networks for the compose
            const networks = [];
            for (const networkName of composeConfig.networks || []) {
                try {
                    const network = await this.createNetwork({ name: networkName });
                    networks.push(network);
                } catch (error) {
                    // Network might already exist
                }
            }

            // Create and start containers
            const containers = [];
            for (const service of composeConfig.services || []) {
                const container = await this.createContainer(service);
                await this.startContainer(container.id);
                containers.push(container);
            }

            DockerMetrics.recordOperation('runCompose', Date.now() - startTime, true);
            return { networks, containers };
        } catch (error) {
            DockerMetrics.recordOperation('runCompose', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Registry Operations
     */
    async loginToRegistry(registryConfig) {
        const startTime = Date.now();
        try {
            const docker = this.serviceManager.getDocker();
            
            const result = await DockerCircuitBreaker.execute(() =>
                docker.checkAuth({
                    username: registryConfig.username || this.config.username,
                    password: registryConfig.password || this.config.password,
                    serveraddress: registryConfig.registry || this.config.registry
                })
            );

            DockerMetrics.recordOperation('loginToRegistry', Date.now() - startTime, true);
            return result;
        } catch (error) {
            DockerMetrics.recordOperation('loginToRegistry', Date.now() - startTime, false);
            throw error;
        }
    }

    async pushImage(imageName, options = {}) {
        const startTime = Date.now();
        try {
            const docker = this.serviceManager.getDocker();
            const image = docker.getImage(imageName);
            
            const pushStream = await DockerCircuitBreaker.execute(() =>
                image.push(options)
            );

            return new Promise((resolve, reject) => {
                let output = '';
                pushStream.on('data', (chunk) => {
                    const data = JSON.parse(chunk.toString());
                    if (data.status) {
                        output += data.status + '\n';
                    }
                });
                
                pushStream.on('end', () => {
                    DockerMetrics.recordOperation('pushImage', Date.now() - startTime, true);
                    resolve({ success: true, output });
                });
                
                pushStream.on('error', (error) => {
                    DockerMetrics.recordOperation('pushImage', Date.now() - startTime, false);
                    reject(error);
                });
            });
        } catch (error) {
            DockerMetrics.recordOperation('pushImage', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Utility Methods
     */
    getMetrics() {
        return DockerMetrics.getMetrics();
    }

    getCircuitBreakerStatus() {
        return { ...circuitBreaker };
    }

    async cleanup() {
        console.log('Docker Operator cleanup completed');
    }
}

/**
 * Main execution function for TuskLang integration
 */
async function executeDockerOperator(operation, params = {}) {
    const startTime = Date.now();
    
    try {
        const dockerOperator = new DockerOperator(params.config);
        
        let result;
        switch (operation) {
            case 'listContainers':
                result = await dockerOperator.listContainers(params.options);
                break;
            case 'createContainer':
                result = await dockerOperator.createContainer(params.containerConfig);
                break;
            case 'startContainer':
                result = await dockerOperator.startContainer(params.containerId);
                break;
            case 'stopContainer':
                result = await dockerOperator.stopContainer(params.containerId, params.options);
                break;
            case 'removeContainer':
                result = await dockerOperator.removeContainer(params.containerId, params.options);
                break;
            case 'listImages':
                result = await dockerOperator.listImages(params.options);
                break;
            case 'buildImage':
                result = await dockerOperator.buildImage(params.buildConfig);
                break;
            case 'pullImage':
                result = await dockerOperator.pullImage(params.imageName, params.options);
                break;
            case 'removeImage':
                result = await dockerOperator.removeImage(params.imageId, params.options);
                break;
            case 'listVolumes':
                result = await dockerOperator.listVolumes(params.options);
                break;
            case 'createVolume':
                result = await dockerOperator.createVolume(params.volumeConfig);
                break;
            case 'removeVolume':
                result = await dockerOperator.removeVolume(params.volumeName, params.options);
                break;
            case 'listNetworks':
                result = await dockerOperator.listNetworks(params.options);
                break;
            case 'createNetwork':
                result = await dockerOperator.createNetwork(params.networkConfig);
                break;
            case 'removeNetwork':
                result = await dockerOperator.removeNetwork(params.networkId);
                break;
            case 'getContainerLogs':
                result = await dockerOperator.getContainerLogs(params.containerId, params.options);
                break;
            case 'execCommand':
                result = await dockerOperator.execCommand(params.containerId, params.command, params.options);
                break;
            case 'runCompose':
                result = await dockerOperator.runCompose(params.composeConfig);
                break;
            case 'loginToRegistry':
                result = await dockerOperator.loginToRegistry(params.registryConfig);
                break;
            case 'pushImage':
                result = await dockerOperator.pushImage(params.imageName, params.options);
                break;
            case 'getMetrics':
                result = dockerOperator.getMetrics();
                break;
            case 'getCircuitBreakerStatus':
                result = dockerOperator.getCircuitBreakerStatus();
                break;
            default:
                throw new Error(`Unsupported Docker operation: ${operation}`);
        }

        const duration = Date.now() - startTime;
        console.log(`Docker Operation '${operation}' completed in ${duration}ms`);
        
        await dockerOperator.cleanup();
        return {
            success: true,
            operation,
            duration,
            result,
            timestamp: new Date().toISOString()
        };
    } catch (error) {
        const duration = Date.now() - startTime;
        console.error(`Docker Operation '${operation}' failed after ${duration}ms:`, error.message);
        
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
    DockerOperator,
    executeDockerOperator,
    DockerOperatorConfig,
    DockerMetrics,
    DockerCircuitBreaker
}; 