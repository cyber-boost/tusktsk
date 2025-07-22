/**
 * GCP OPERATOR - Real Google Cloud Platform Integration
 * Production-ready Google Cloud client libraries integration with comprehensive service support
 * 
 * @author Agent A3 - Cloud Infrastructure Specialist
 * @version 1.0.0
 * @license MIT
 */

const { Compute } = require('@google-cloud/compute');
const { Storage } = require('@google-cloud/storage');
const { BigQuery } = require('@google-cloud/bigquery');
const { CloudFunctionsServiceClient } = require('@google-cloud/functions');
const { Monitoring } = require('@google-cloud/monitoring');
const { ResourceManager } = require('@google-cloud/resource-manager');
const { IAM } = require('@google-cloud/iam');
const { ContainerAnalysis } = require('@google-cloud/containeranalysis');
const { ContainerRegistry } = require('@google-cloud/containerregistry');
const { CloudBuild } = require('@google-cloud/cloudbuild');
const { PubSub } = require('@google-cloud/pubsub');
const { Datastore } = require('@google-cloud/datastore');
const { Firestore } = require('@google-cloud/firestore');

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
    state: 'CLOSED' // CLOSED, OPEN, HALF_OPEN
};

// Connection pooling
const connectionPool = new Map();
const MAX_CONNECTIONS = 50;
const CONNECTION_TIMEOUT = 30000;

/**
 * GCP Operator Configuration
 */
class GcpOperatorConfig {
    constructor(options = {}) {
        this.projectId = options.projectId || process.env.GOOGLE_CLOUD_PROJECT;
        this.credentials = options.credentials || process.env.GOOGLE_APPLICATION_CREDENTIALS;
        this.zone = options.zone || process.env.GOOGLE_CLOUD_ZONE || 'us-central1-a';
        this.region = options.region || process.env.GOOGLE_CLOUD_REGION || 'us-central1';
        this.maxRetries = options.maxRetries || 3;
        this.requestTimeout = options.requestTimeout || 60000;
        this.enableMetrics = options.enableMetrics !== false;
        this.logLevel = options.logLevel || 'info';
        this.useDefaultCredentials = options.useDefaultCredentials !== false;
        this.tags = options.tags || {};
    }

    validate() {
        if (!this.projectId) {
            throw new Error('GCP project ID is required');
        }
        if (!this.useDefaultCredentials && !this.credentials) {
            throw new Error('GCP credentials are required when not using default credentials');
        }
        return true;
    }
}

/**
 * GCP Service Client Manager
 */
class GcpServiceManager {
    constructor(config) {
        this.config = config;
        this.clients = new Map();
        this.initializeClients();
    }

    initializeClients() {
        const clientOptions = {
            projectId: this.config.projectId,
            keyFilename: this.config.useDefaultCredentials ? undefined : this.config.credentials,
            timeout: this.config.requestTimeout
        };

        this.clients.set('compute', new Compute(clientOptions));
        this.clients.set('storage', new Storage(clientOptions));
        this.clients.set('bigquery', new BigQuery(clientOptions));
        this.clients.set('functions', new CloudFunctionsServiceClient(clientOptions));
        this.clients.set('monitoring', new Monitoring.MetricServiceClient(clientOptions));
        this.clients.set('resourceManager', new ResourceManager(clientOptions));
        this.clients.set('iam', new IAM.IAMClient(clientOptions));
        this.clients.set('containerAnalysis', new ContainerAnalysis(clientOptions));
        this.clients.set('containerRegistry', new ContainerRegistry(clientOptions));
        this.clients.set('cloudBuild', new CloudBuild(clientOptions));
        this.clients.set('pubsub', new PubSub(clientOptions));
        this.clients.set('datastore', new Datastore(clientOptions));
        this.clients.set('firestore', new Firestore(clientOptions));
    }

    getClient(service) {
        const client = this.clients.get(service);
        if (!client) {
            throw new Error(`Unsupported GCP service: ${service}`);
        }
        return client;
    }
}

/**
 * Performance Monitoring and Metrics
 */
class GcpMetrics {
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

        console.log('GCP Metrics:', {
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
class GcpCircuitBreaker {
    static async execute(operation, fallback = null) {
        if (circuitBreaker.state === 'OPEN') {
            if (Date.now() - circuitBreaker.lastFailureTime > circuitBreaker.recoveryTimeout) {
                circuitBreaker.state = 'HALF_OPEN';
            } else {
                if (fallback) return fallback();
                throw new Error('Circuit breaker is OPEN - GCP service unavailable');
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
 * Main GCP Operator Class
 */
class GcpOperator {
    constructor(config = {}) {
        this.config = new GcpOperatorConfig(config);
        this.config.validate();
        this.serviceManager = new GcpServiceManager(this.config);
        this.initialize();
    }

    async initialize() {
        try {
            const resourceManager = this.serviceManager.getClient('resourceManager');
            const project = await resourceManager.getProject(this.config.projectId);
            
            console.log('GCP Operator initialized successfully');
            console.log('Project ID:', this.config.projectId);
            console.log('Project Name:', project.metadata.name);
            console.log('Zone:', this.config.zone);
            console.log('Region:', this.config.region);
        } catch (error) {
            console.error('Failed to initialize GCP Operator:', error.message);
            throw error;
        }
    }

    /**
     * Compute Engine Operations
     */
    async listInstances() {
        const startTime = Date.now();
        try {
            const compute = this.serviceManager.getClient('compute');
            const [instances] = await GcpCircuitBreaker.execute(() =>
                compute.getInstances({ zone: this.config.zone })
            );

            const instanceList = instances.map(instance => ({
                name: instance.name,
                id: instance.id,
                zone: instance.zone.split('/').pop(),
                machineType: instance.machineType.split('/').pop(),
                status: instance.status,
                internalIp: instance.networkInterfaces?.[0]?.networkIP,
                externalIp: instance.networkInterfaces?.[0]?.accessConfigs?.[0]?.natIP,
                creationTimestamp: instance.creationTimestamp,
                labels: instance.labels || {}
            }));

            GcpMetrics.recordOperation('listInstances', Date.now() - startTime, true);
            return instanceList;
        } catch (error) {
            GcpMetrics.recordOperation('listInstances', Date.now() - startTime, false);
            throw error;
        }
    }

    async createInstance(instanceConfig) {
        const startTime = Date.now();
        try {
            const compute = this.serviceManager.getClient('compute');
            
            const instance = {
                name: instanceConfig.name,
                machineType: `zones/${this.config.zone}/machineTypes/${instanceConfig.machineType || 'e2-micro'}`,
                disks: [{
                    boot: true,
                    autoDelete: true,
                    initializeParams: {
                        sourceImage: instanceConfig.image || 'projects/debian-cloud/global/images/family/debian-11'
                    }
                }],
                networkInterfaces: [{
                    network: 'global/networks/default',
                    accessConfigs: [{
                        name: 'External NAT',
                        type: 'ONE_TO_ONE_NAT'
                    }]
                }],
                labels: {
                    ...this.config.tags,
                    name: instanceConfig.name,
                    environment: instanceConfig.environment || 'development'
                }
            };

            const [operation] = await GcpCircuitBreaker.execute(() =>
                compute.createInstance({
                    zone: this.config.zone,
                    resource: instance
                })
            );

            GcpMetrics.recordOperation('createInstance', Date.now() - startTime, true);
            return operation;
        } catch (error) {
            GcpMetrics.recordOperation('createInstance', Date.now() - startTime, false);
            throw error;
        }
    }

    async deleteInstance(instanceName) {
        const startTime = Date.now();
        try {
            const compute = this.serviceManager.getClient('compute');
            
            const [operation] = await GcpCircuitBreaker.execute(() =>
                compute.deleteInstance({
                    zone: this.config.zone,
                    instance: instanceName
                })
            );

            GcpMetrics.recordOperation('deleteInstance', Date.now() - startTime, true);
            return operation;
        } catch (error) {
            GcpMetrics.recordOperation('deleteInstance', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Cloud Storage Operations
     */
    async listBuckets() {
        const startTime = Date.now();
        try {
            const storage = this.serviceManager.getClient('storage');
            const [buckets] = await GcpCircuitBreaker.execute(() =>
                storage.getBuckets()
            );

            const bucketList = buckets.map(bucket => ({
                name: bucket.name,
                location: bucket.metadata.location,
                storageClass: bucket.metadata.storageClass,
                timeCreated: bucket.metadata.timeCreated,
                updated: bucket.metadata.updated,
                labels: bucket.metadata.labels || {}
            }));

            GcpMetrics.recordOperation('listBuckets', Date.now() - startTime, true);
            return bucketList;
        } catch (error) {
            GcpMetrics.recordOperation('listBuckets', Date.now() - startTime, false);
            throw error;
        }
    }

    async createBucket(bucketConfig) {
        const startTime = Date.now();
        try {
            const storage = this.serviceManager.getClient('storage');
            
            const [bucket] = await GcpCircuitBreaker.execute(() =>
                storage.createBucket(bucketConfig.name, {
                    location: bucketConfig.location || this.config.region,
                    storageClass: bucketConfig.storageClass || 'STANDARD',
                    labels: {
                        ...this.config.tags,
                        name: bucketConfig.name,
                        environment: bucketConfig.environment || 'development'
                    }
                })
            );

            GcpMetrics.recordOperation('createBucket', Date.now() - startTime, true);
            return bucket;
        } catch (error) {
            GcpMetrics.recordOperation('createBucket', Date.now() - startTime, false);
            throw error;
        }
    }

    async uploadFile(bucketName, fileName, data, options = {}) {
        const startTime = Date.now();
        try {
            const storage = this.serviceManager.getClient('storage');
            const bucket = storage.bucket(bucketName);
            const file = bucket.file(fileName);

            const [result] = await GcpCircuitBreaker.execute(() =>
                file.save(data, {
                    metadata: {
                        contentType: options.contentType || 'application/octet-stream',
                        metadata: options.metadata || {}
                    }
                })
            );

            GcpMetrics.recordOperation('uploadFile', Date.now() - startTime, true);
            return result;
        } catch (error) {
            GcpMetrics.recordOperation('uploadFile', Date.now() - startTime, false);
            throw error;
        }
    }

    async downloadFile(bucketName, fileName) {
        const startTime = Date.now();
        try {
            const storage = this.serviceManager.getClient('storage');
            const bucket = storage.bucket(bucketName);
            const file = bucket.file(fileName);

            const [data] = await GcpCircuitBreaker.execute(() =>
                file.download()
            );

            GcpMetrics.recordOperation('downloadFile', Date.now() - startTime, true);
            return data;
        } catch (error) {
            GcpMetrics.recordOperation('downloadFile', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * BigQuery Operations
     */
    async listDatasets() {
        const startTime = Date.now();
        try {
            const bigquery = this.serviceManager.getClient('bigquery');
            const [datasets] = await GcpCircuitBreaker.execute(() =>
                bigquery.getDatasets()
            );

            const datasetList = datasets.map(dataset => ({
                id: dataset.id,
                datasetId: dataset.id.split(':').pop(),
                friendlyName: dataset.metadata.friendlyName,
                description: dataset.metadata.description,
                location: dataset.metadata.location,
                labels: dataset.metadata.labels || {}
            }));

            GcpMetrics.recordOperation('listDatasets', Date.now() - startTime, true);
            return datasetList;
        } catch (error) {
            GcpMetrics.recordOperation('listDatasets', Date.now() - startTime, false);
            throw error;
        }
    }

    async createDataset(datasetConfig) {
        const startTime = Date.now();
        try {
            const bigquery = this.serviceManager.getClient('bigquery');
            
            const [dataset] = await GcpCircuitBreaker.execute(() =>
                bigquery.createDataset(datasetConfig.datasetId, {
                    location: datasetConfig.location || this.config.region,
                    description: datasetConfig.description,
                    labels: {
                        ...this.config.tags,
                        name: datasetConfig.datasetId,
                        environment: datasetConfig.environment || 'development'
                    }
                })
            );

            GcpMetrics.recordOperation('createDataset', Date.now() - startTime, true);
            return dataset;
        } catch (error) {
            GcpMetrics.recordOperation('createDataset', Date.now() - startTime, false);
            throw error;
        }
    }

    async executeQuery(query) {
        const startTime = Date.now();
        try {
            const bigquery = this.serviceManager.getClient('bigquery');
            
            const [rows] = await GcpCircuitBreaker.execute(() =>
                bigquery.query({ query })
            );

            GcpMetrics.recordOperation('executeQuery', Date.now() - startTime, true);
            return rows;
        } catch (error) {
            GcpMetrics.recordOperation('executeQuery', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Cloud Functions Operations
     */
    async listFunctions() {
        const startTime = Date.now();
        try {
            const functions = this.serviceManager.getClient('functions');
            const [functionList] = await GcpCircuitBreaker.execute(() =>
                functions.listFunctions({
                    parent: `projects/${this.config.projectId}/locations/${this.config.region}`
                })
            );

            const functionsList = functionList.map(func => ({
                name: func.name,
                displayName: func.displayName,
                description: func.description,
                status: func.state,
                runtime: func.runtime,
                entryPoint: func.entryPoint,
                availableMemoryMb: func.availableMemoryMb,
                timeout: func.timeout,
                labels: func.labels || {}
            }));

            GcpMetrics.recordOperation('listFunctions', Date.now() - startTime, true);
            return functionsList;
        } catch (error) {
            GcpMetrics.recordOperation('listFunctions', Date.now() - startTime, false);
            throw error;
        }
    }

    async createFunction(functionConfig) {
        const startTime = Date.now();
        try {
            const functions = this.serviceManager.getClient('functions');
            
            const [operation] = await GcpCircuitBreaker.execute(() =>
                functions.createFunction({
                    parent: `projects/${this.config.projectId}/locations/${this.config.region}`,
                    functionId: functionConfig.name,
                    function: {
                        name: `projects/${this.config.projectId}/locations/${this.config.region}/functions/${functionConfig.name}`,
                        description: functionConfig.description,
                        sourceArchiveUrl: functionConfig.sourceUrl,
                        entryPoint: functionConfig.entryPoint || 'helloWorld',
                        runtime: functionConfig.runtime || 'nodejs18',
                        availableMemoryMb: functionConfig.memory || 256,
                        timeout: functionConfig.timeout || '60s',
                        labels: {
                            ...this.config.tags,
                            name: functionConfig.name,
                            environment: functionConfig.environment || 'development'
                        }
                    }
                })
            );

            GcpMetrics.recordOperation('createFunction', Date.now() - startTime, true);
            return operation;
        } catch (error) {
            GcpMetrics.recordOperation('createFunction', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Cloud Monitoring Operations
     */
    async putMetric(metricData) {
        const startTime = Date.now();
        try {
            const monitoring = this.serviceManager.getClient('monitoring');
            
            const timeSeriesData = {
                metric: {
                    type: `custom.googleapis.com/${metricData.name}`,
                    labels: metricData.labels || {}
                },
                resource: {
                    type: 'global',
                    labels: {
                        project_id: this.config.projectId
                    }
                },
                points: [{
                    interval: {
                        endTime: {
                            seconds: Math.floor(Date.now() / 1000)
                        }
                    },
                    value: {
                        doubleValue: metricData.value
                    }
                }]
            };

            const [result] = await GcpCircuitBreaker.execute(() =>
                monitoring.createTimeSeries({
                    name: `projects/${this.config.projectId}`,
                    timeSeries: [timeSeriesData]
                })
            );

            GcpMetrics.recordOperation('putMetric', Date.now() - startTime, true);
            return result;
        } catch (error) {
            GcpMetrics.recordOperation('putMetric', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * IAM Operations
     */
    async listServiceAccounts() {
        const startTime = Date.now();
        try {
            const iam = this.serviceManager.getClient('iam');
            const [accounts] = await GcpCircuitBreaker.execute(() =>
                iam.listServiceAccounts({
                    name: `projects/${this.config.projectId}`
                })
            );

            const accountList = accounts.accounts.map(account => ({
                name: account.name,
                email: account.email,
                displayName: account.displayName,
                description: account.description,
                disabled: account.disabled,
                oauth2ClientId: account.oauth2ClientId
            }));

            GcpMetrics.recordOperation('listServiceAccounts', Date.now() - startTime, true);
            return accountList;
        } catch (error) {
            GcpMetrics.recordOperation('listServiceAccounts', Date.now() - startTime, false);
            throw error;
        }
    }

    async createServiceAccount(accountConfig) {
        const startTime = Date.now();
        try {
            const iam = this.serviceManager.getClient('iam');
            
            const [account] = await GcpCircuitBreaker.execute(() =>
                iam.createServiceAccount({
                    name: `projects/${this.config.projectId}`,
                    accountId: accountConfig.accountId,
                    serviceAccount: {
                        displayName: accountConfig.displayName,
                        description: accountConfig.description
                    }
                })
            );

            GcpMetrics.recordOperation('createServiceAccount', Date.now() - startTime, true);
            return account;
        } catch (error) {
            GcpMetrics.recordOperation('createServiceAccount', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Pub/Sub Operations
     */
    async listTopics() {
        const startTime = Date.now();
        try {
            const pubsub = this.serviceManager.getClient('pubsub');
            const [topics] = await GcpCircuitBreaker.execute(() =>
                pubsub.getTopics()
            );

            const topicList = topics.map(topic => ({
                name: topic.name,
                kmsKeyName: topic.metadata.kmsKeyName,
                labels: topic.metadata.labels || {}
            }));

            GcpMetrics.recordOperation('listTopics', Date.now() - startTime, true);
            return topicList;
        } catch (error) {
            GcpMetrics.recordOperation('listTopics', Date.now() - startTime, false);
            throw error;
        }
    }

    async createTopic(topicConfig) {
        const startTime = Date.now();
        try {
            const pubsub = this.serviceManager.getClient('pubsub');
            
            const [topic] = await GcpCircuitBreaker.execute(() =>
                pubsub.createTopic(topicConfig.name, {
                    labels: {
                        ...this.config.tags,
                        name: topicConfig.name,
                        environment: topicConfig.environment || 'development'
                    }
                })
            );

            GcpMetrics.recordOperation('createTopic', Date.now() - startTime, true);
            return topic;
        } catch (error) {
            GcpMetrics.recordOperation('createTopic', Date.now() - startTime, false);
            throw error;
        }
    }

    async publishMessage(topicName, message, attributes = {}) {
        const startTime = Date.now();
        try {
            const pubsub = this.serviceManager.getClient('pubsub');
            const topic = pubsub.topic(topicName);
            
            const [messageId] = await GcpCircuitBreaker.execute(() =>
                topic.publish(Buffer.from(message), attributes)
            );

            GcpMetrics.recordOperation('publishMessage', Date.now() - startTime, true);
            return { messageId };
        } catch (error) {
            GcpMetrics.recordOperation('publishMessage', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Cloud Build Operations
     */
    async listBuilds() {
        const startTime = Date.now();
        try {
            const cloudBuild = this.serviceManager.getClient('cloudBuild');
            const [builds] = await GcpCircuitBreaker.execute(() =>
                cloudBuild.listBuilds({
                    projectId: this.config.projectId
                })
            );

            const buildList = builds.builds.map(build => ({
                id: build.id,
                status: build.status,
                source: build.source,
                steps: build.steps,
                createTime: build.createTime,
                startTime: build.startTime,
                finishTime: build.finishTime,
                logsUrl: build.logsUrl
            }));

            GcpMetrics.recordOperation('listBuilds', Date.now() - startTime, true);
            return buildList;
        } catch (error) {
            GcpMetrics.recordOperation('listBuilds', Date.now() - startTime, false);
            throw error;
        }
    }

    async createBuild(buildConfig) {
        const startTime = Date.now();
        try {
            const cloudBuild = this.serviceManager.getClient('cloudBuild');
            
            const [operation] = await GcpCircuitBreaker.execute(() =>
                cloudBuild.createBuild({
                    projectId: this.config.projectId,
                    build: {
                        source: {
                            storageSource: {
                                bucket: buildConfig.sourceBucket,
                                object: buildConfig.sourceObject
                            }
                        },
                        steps: buildConfig.steps || [],
                        options: buildConfig.options || {},
                        tags: [
                            ...Object.entries(this.config.tags).map(([k, v]) => `${k}=${v}`),
                            `name=${buildConfig.name}`,
                            `environment=${buildConfig.environment || 'development'}`
                        ]
                    }
                })
            );

            GcpMetrics.recordOperation('createBuild', Date.now() - startTime, true);
            return operation;
        } catch (error) {
            GcpMetrics.recordOperation('createBuild', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Utility Methods
     */
    getMetrics() {
        return GcpMetrics.getMetrics();
    }

    getCircuitBreakerStatus() {
        return { ...circuitBreaker };
    }

    async cleanup() {
        // Cleanup connections and resources
        for (const [service, client] of this.serviceManager.clients) {
            if (client.close) {
                await client.close();
            }
        }
        console.log('GCP Operator cleanup completed');
    }
}

/**
 * Main execution function for TuskLang integration
 */
async function executeGcpOperator(operation, params = {}) {
    const startTime = Date.now();
    
    try {
        const gcpOperator = new GcpOperator(params.config);
        
        let result;
        switch (operation) {
            case 'listInstances':
                result = await gcpOperator.listInstances();
                break;
            case 'createInstance':
                result = await gcpOperator.createInstance(params.instanceConfig);
                break;
            case 'deleteInstance':
                result = await gcpOperator.deleteInstance(params.instanceName);
                break;
            case 'listBuckets':
                result = await gcpOperator.listBuckets();
                break;
            case 'createBucket':
                result = await gcpOperator.createBucket(params.bucketConfig);
                break;
            case 'uploadFile':
                result = await gcpOperator.uploadFile(params.bucketName, params.fileName, params.data, params.options);
                break;
            case 'downloadFile':
                result = await gcpOperator.downloadFile(params.bucketName, params.fileName);
                break;
            case 'listDatasets':
                result = await gcpOperator.listDatasets();
                break;
            case 'createDataset':
                result = await gcpOperator.createDataset(params.datasetConfig);
                break;
            case 'executeQuery':
                result = await gcpOperator.executeQuery(params.query);
                break;
            case 'listFunctions':
                result = await gcpOperator.listFunctions();
                break;
            case 'createFunction':
                result = await gcpOperator.createFunction(params.functionConfig);
                break;
            case 'putMetric':
                result = await gcpOperator.putMetric(params.metricData);
                break;
            case 'listServiceAccounts':
                result = await gcpOperator.listServiceAccounts();
                break;
            case 'createServiceAccount':
                result = await gcpOperator.createServiceAccount(params.accountConfig);
                break;
            case 'listTopics':
                result = await gcpOperator.listTopics();
                break;
            case 'createTopic':
                result = await gcpOperator.createTopic(params.topicConfig);
                break;
            case 'publishMessage':
                result = await gcpOperator.publishMessage(params.topicName, params.message, params.attributes);
                break;
            case 'listBuilds':
                result = await gcpOperator.listBuilds();
                break;
            case 'createBuild':
                result = await gcpOperator.createBuild(params.buildConfig);
                break;
            case 'getMetrics':
                result = gcpOperator.getMetrics();
                break;
            case 'getCircuitBreakerStatus':
                result = gcpOperator.getCircuitBreakerStatus();
                break;
            default:
                throw new Error(`Unsupported GCP operation: ${operation}`);
        }

        const duration = Date.now() - startTime;
        console.log(`GCP Operation '${operation}' completed in ${duration}ms`);
        
        await gcpOperator.cleanup();
        return {
            success: true,
            operation,
            duration,
            result,
            timestamp: new Date().toISOString()
        };
    } catch (error) {
        const duration = Date.now() - startTime;
        console.error(`GCP Operation '${operation}' failed after ${duration}ms:`, error.message);
        
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
    GcpOperator,
    executeGcpOperator,
    GcpOperatorConfig,
    GcpMetrics,
    GcpCircuitBreaker
}; 