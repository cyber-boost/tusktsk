/**
 * AZURE OPERATOR - Real Microsoft Azure Integration
 * Production-ready Azure SDK for JavaScript integration with comprehensive service support
 * 
 * @author Agent A3 - Cloud Infrastructure Specialist
 * @version 1.0.0
 * @license MIT
 */

const { DefaultAzureCredential, ClientSecretCredential } = require('@azure/identity');
const { ComputeManagementClient } = require('@azure/arm-compute');
const { StorageManagementClient } = require('@azure/arm-storage');
const { SqlManagementClient } = require('@azure/arm-sql');
const { WebSiteManagementClient } = require('@azure/arm-appservice');
const { MonitorClient } = require('@azure/arm-monitor');
const { ResourceManagementClient } = require('@azure/arm-resources');
const { NetworkManagementClient } = require('@azure/arm-network');
const { ContainerServiceClient } = require('@azure/arm-containerservice');
const { KeyVaultManagementClient } = require('@azure/arm-keyvault');
const { BlobServiceClient } = require('@azure/storage-blob');
const { TableServiceClient } = require('@azure/data-tables');
const { QueueServiceClient } = require('@azure/storage-queue');

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
 * Azure Operator Configuration
 */
class AzureOperatorConfig {
    constructor(options = {}) {
        this.subscriptionId = options.subscriptionId || process.env.AZURE_SUBSCRIPTION_ID;
        this.tenantId = options.tenantId || process.env.AZURE_TENANT_ID;
        this.clientId = options.clientId || process.env.AZURE_CLIENT_ID;
        this.clientSecret = options.clientSecret || process.env.AZURE_CLIENT_SECRET;
        this.resourceGroup = options.resourceGroup || process.env.AZURE_RESOURCE_GROUP;
        this.location = options.location || process.env.AZURE_LOCATION || 'eastus';
        this.maxRetries = options.maxRetries || 3;
        this.requestTimeout = options.requestTimeout || 60000;
        this.enableMetrics = options.enableMetrics !== false;
        this.logLevel = options.logLevel || 'info';
        this.useManagedIdentity = options.useManagedIdentity !== false;
        this.tags = options.tags || {};
    }

    validate() {
        if (!this.subscriptionId) {
            throw new Error('Azure subscription ID is required');
        }
        if (!this.useManagedIdentity && (!this.tenantId || !this.clientId || !this.clientSecret)) {
            throw new Error('Azure credentials are required when not using managed identity');
        }
        if (!this.resourceGroup) {
            throw new Error('Azure resource group is required');
        }
        return true;
    }
}

/**
 * Azure Service Client Manager
 */
class AzureServiceManager {
    constructor(config) {
        this.config = config;
        this.clients = new Map();
        this.credential = this.createCredential();
        this.initializeClients();
    }

    createCredential() {
        if (this.config.useManagedIdentity) {
            return new DefaultAzureCredential();
        } else {
            return new ClientSecretCredential(
                this.config.tenantId,
                this.config.clientId,
                this.config.clientSecret
            );
        }
    }

    initializeClients() {
        const clientOptions = {
            credential: this.credential,
            subscriptionId: this.config.subscriptionId,
            requestOptions: {
                timeout: this.config.requestTimeout
            }
        };

        this.clients.set('compute', new ComputeManagementClient(clientOptions));
        this.clients.set('storage', new StorageManagementClient(clientOptions));
        this.clients.set('sql', new SqlManagementClient(clientOptions));
        this.clients.set('web', new WebSiteManagementClient(clientOptions));
        this.clients.set('monitor', new MonitorClient(clientOptions));
        this.clients.set('resources', new ResourceManagementClient(clientOptions));
        this.clients.set('network', new NetworkManagementClient(clientOptions));
        this.clients.set('containerservice', new ContainerServiceClient(clientOptions));
        this.clients.set('keyvault', new KeyVaultManagementClient(clientOptions));
    }

    getClient(service) {
        const client = this.clients.get(service);
        if (!client) {
            throw new Error(`Unsupported Azure service: ${service}`);
        }
        return client;
    }
}

/**
 * Performance Monitoring and Metrics
 */
class AzureMetrics {
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

        console.log('Azure Metrics:', {
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
class AzureCircuitBreaker {
    static async execute(operation, fallback = null) {
        if (circuitBreaker.state === 'OPEN') {
            if (Date.now() - circuitBreaker.lastFailureTime > circuitBreaker.recoveryTimeout) {
                circuitBreaker.state = 'HALF_OPEN';
            } else {
                if (fallback) return fallback();
                throw new Error('Circuit breaker is OPEN - Azure service unavailable');
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
 * Main Azure Operator Class
 */
class AzureOperator {
    constructor(config = {}) {
        this.config = new AzureOperatorConfig(config);
        this.config.validate();
        this.serviceManager = new AzureServiceManager(this.config);
        this.initialize();
    }

    async initialize() {
        try {
            const resourcesClient = this.serviceManager.getClient('resources');
            const resourceGroup = await resourcesClient.resourceGroups.get(this.config.resourceGroup);
            
            console.log('Azure Operator initialized successfully');
            console.log('Subscription ID:', this.config.subscriptionId);
            console.log('Resource Group:', this.config.resourceGroup);
            console.log('Location:', this.config.location);
            console.log('Resource Group Location:', resourceGroup.location);
        } catch (error) {
            console.error('Failed to initialize Azure Operator:', error.message);
            throw error;
        }
    }

    /**
     * Virtual Machine Operations
     */
    async listVirtualMachines() {
        const startTime = Date.now();
        try {
            const computeClient = this.serviceManager.getClient('compute');
            const vms = [];
            
            const vmList = await AzureCircuitBreaker.execute(() => 
                computeClient.virtualMachines.list(this.config.resourceGroup)
            );

            for await (const vm of vmList) {
                vms.push({
                    name: vm.name,
                    id: vm.id,
                    location: vm.location,
                    size: vm.hardwareProfile?.vmSize,
                    osType: vm.storageProfile?.osDisk?.osType,
                    status: vm.provisioningState,
                    publicIp: vm.publicIPAddresses?.[0]?.ipAddress,
                    privateIp: vm.privateIPAddresses?.[0],
                    tags: vm.tags || {}
                });
            }

            AzureMetrics.recordOperation('listVirtualMachines', Date.now() - startTime, true);
            return vms;
        } catch (error) {
            AzureMetrics.recordOperation('listVirtualMachines', Date.now() - startTime, false);
            throw error;
        }
    }

    async createVirtualMachine(vmConfig) {
        const startTime = Date.now();
        try {
            const computeClient = this.serviceManager.getClient('compute');
            const networkClient = this.serviceManager.getClient('network');

            // Create network interface if not provided
            let networkInterfaceId = vmConfig.networkInterfaceId;
            if (!networkInterfaceId) {
                const nic = await this.createNetworkInterface(vmConfig);
                networkInterfaceId = nic.id;
            }

            const vmParameters = {
                location: this.config.location,
                hardwareProfile: {
                    vmSize: vmConfig.size || 'Standard_B1s'
                },
                osProfile: {
                    computerName: vmConfig.name,
                    adminUsername: vmConfig.adminUsername,
                    adminPassword: vmConfig.adminPassword
                },
                networkProfile: {
                    networkInterfaces: [{
                        id: networkInterfaceId
                    }]
                },
                storageProfile: {
                    imageReference: {
                        publisher: vmConfig.imagePublisher || 'Canonical',
                        offer: vmConfig.imageOffer || 'UbuntuServer',
                        sku: vmConfig.imageSku || '18.04-LTS',
                        version: vmConfig.imageVersion || 'latest'
                    },
                    osDisk: {
                        name: `${vmConfig.name}-osdisk`,
                        createOption: 'FromImage',
                        managedDisk: {
                            storageAccountType: 'Standard_LRS'
                        }
                    }
                },
                tags: {
                    ...this.config.tags,
                    Name: vmConfig.name,
                    Environment: vmConfig.environment || 'development'
                }
            };

            const result = await AzureCircuitBreaker.execute(() =>
                computeClient.virtualMachines.beginCreateOrUpdate(
                    this.config.resourceGroup,
                    vmConfig.name,
                    vmParameters
                )
            );

            AzureMetrics.recordOperation('createVirtualMachine', Date.now() - startTime, true);
            return result;
        } catch (error) {
            AzureMetrics.recordOperation('createVirtualMachine', Date.now() - startTime, false);
            throw error;
        }
    }

    async deleteVirtualMachine(vmName) {
        const startTime = Date.now();
        try {
            const computeClient = this.serviceManager.getClient('compute');
            
            const result = await AzureCircuitBreaker.execute(() =>
                computeClient.virtualMachines.beginDelete(
                    this.config.resourceGroup,
                    vmName
                )
            );

            AzureMetrics.recordOperation('deleteVirtualMachine', Date.now() - startTime, true);
            return result;
        } catch (error) {
            AzureMetrics.recordOperation('deleteVirtualMachine', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Storage Account Operations
     */
    async listStorageAccounts() {
        const startTime = Date.now();
        try {
            const storageClient = this.serviceManager.getClient('storage');
            const accounts = [];
            
            const accountList = await AzureCircuitBreaker.execute(() =>
                storageClient.storageAccounts.listByResourceGroup(this.config.resourceGroup)
            );

            for await (const account of accountList) {
                accounts.push({
                    name: account.name,
                    id: account.id,
                    location: account.location,
                    sku: account.sku?.name,
                    kind: account.kind,
                    status: account.statusOfPrimary,
                    primaryEndpoints: account.primaryEndpoints,
                    tags: account.tags || {}
                });
            }

            AzureMetrics.recordOperation('listStorageAccounts', Date.now() - startTime, true);
            return accounts;
        } catch (error) {
            AzureMetrics.recordOperation('listStorageAccounts', Date.now() - startTime, false);
            throw error;
        }
    }

    async createStorageAccount(accountConfig) {
        const startTime = Date.now();
        try {
            const storageClient = this.serviceManager.getClient('storage');
            
            const accountParameters = {
                location: this.config.location,
                sku: {
                    name: accountConfig.sku || 'Standard_LRS'
                },
                kind: accountConfig.kind || 'StorageV2',
                enableHttpsTrafficOnly: true,
                minimumTlsVersion: 'TLS1_2',
                allowBlobPublicAccess: false,
                tags: {
                    ...this.config.tags,
                    Name: accountConfig.name,
                    Environment: accountConfig.environment || 'development'
                }
            };

            const result = await AzureCircuitBreaker.execute(() =>
                storageClient.storageAccounts.beginCreate(
                    this.config.resourceGroup,
                    accountConfig.name,
                    accountParameters
                )
            );

            AzureMetrics.recordOperation('createStorageAccount', Date.now() - startTime, true);
            return result;
        } catch (error) {
            AzureMetrics.recordOperation('createStorageAccount', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Blob Storage Operations
     */
    async uploadBlob(storageAccountName, containerName, blobName, data, options = {}) {
        const startTime = Date.now();
        try {
            const storageClient = this.serviceManager.getClient('storage');
            
            // Get storage account keys
            const keys = await AzureCircuitBreaker.execute(() =>
                storageClient.storageAccounts.listKeys(this.config.resourceGroup, storageAccountName)
            );

            const connectionString = `DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${keys.keys[0].value};EndpointSuffix=core.windows.net`;
            const blobServiceClient = BlobServiceClient.fromConnectionString(connectionString);
            const containerClient = blobServiceClient.getContainerClient(containerName);
            const blockBlobClient = containerClient.getBlockBlobClient(blobName);

            const uploadResult = await AzureCircuitBreaker.execute(() =>
                blockBlobClient.upload(data, data.length, {
                    blobHTTPHeaders: {
                        blobContentType: options.contentType || 'application/octet-stream'
                    },
                    metadata: options.metadata || {}
                })
            );

            AzureMetrics.recordOperation('uploadBlob', Date.now() - startTime, true);
            return {
                etag: uploadResult.etag,
                lastModified: uploadResult.lastModified,
                requestId: uploadResult.requestId
            };
        } catch (error) {
            AzureMetrics.recordOperation('uploadBlob', Date.now() - startTime, false);
            throw error;
        }
    }

    async downloadBlob(storageAccountName, containerName, blobName) {
        const startTime = Date.now();
        try {
            const storageClient = this.serviceManager.getClient('storage');
            
            // Get storage account keys
            const keys = await AzureCircuitBreaker.execute(() =>
                storageClient.storageAccounts.listKeys(this.config.resourceGroup, storageAccountName)
            );

            const connectionString = `DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${keys.keys[0].value};EndpointSuffix=core.windows.net`;
            const blobServiceClient = BlobServiceClient.fromConnectionString(connectionString);
            const containerClient = blobServiceClient.getContainerClient(containerName);
            const blockBlobClient = containerClient.getBlockBlobClient(blobName);

            const downloadResult = await AzureCircuitBreaker.execute(() =>
                blockBlobClient.download()
            );

            const chunks = [];
            for await (const chunk of downloadResult.readableStreamBody) {
                chunks.push(chunk);
            }

            const data = Buffer.concat(chunks);

            AzureMetrics.recordOperation('downloadBlob', Date.now() - startTime, true);
            return data;
        } catch (error) {
            AzureMetrics.recordOperation('downloadBlob', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * SQL Database Operations
     */
    async listSqlServers() {
        const startTime = Date.now();
        try {
            const sqlClient = this.serviceManager.getClient('sql');
            const servers = [];
            
            const serverList = await AzureCircuitBreaker.execute(() =>
                sqlClient.servers.listByResourceGroup(this.config.resourceGroup)
            );

            for await (const server of serverList) {
                servers.push({
                    name: server.name,
                    id: server.id,
                    location: server.location,
                    version: server.version,
                    administratorLogin: server.administratorLogin,
                    fullyQualifiedDomainName: server.fullyQualifiedDomainName,
                    tags: server.tags || {}
                });
            }

            AzureMetrics.recordOperation('listSqlServers', Date.now() - startTime, true);
            return servers;
        } catch (error) {
            AzureMetrics.recordOperation('listSqlServers', Date.now() - startTime, false);
            throw error;
        }
    }

    async createSqlServer(serverConfig) {
        const startTime = Date.now();
        try {
            const sqlClient = this.serviceManager.getClient('sql');
            
            const serverParameters = {
                location: this.config.location,
                administratorLogin: serverConfig.adminUsername,
                administratorLoginPassword: serverConfig.adminPassword,
                version: serverConfig.version || '12.0',
                tags: {
                    ...this.config.tags,
                    Name: serverConfig.name,
                    Environment: serverConfig.environment || 'development'
                }
            };

            const result = await AzureCircuitBreaker.execute(() =>
                sqlClient.servers.beginCreateOrUpdate(
                    this.config.resourceGroup,
                    serverConfig.name,
                    serverParameters
                )
            );

            AzureMetrics.recordOperation('createSqlServer', Date.now() - startTime, true);
            return result;
        } catch (error) {
            AzureMetrics.recordOperation('createSqlServer', Date.now() - startTime, false);
            throw error;
        }
    }

    async createSqlDatabase(serverName, databaseConfig) {
        const startTime = Date.now();
        try {
            const sqlClient = this.serviceManager.getClient('sql');
            
            const databaseParameters = {
                location: this.config.location,
                sku: {
                    name: databaseConfig.sku || 'Basic',
                    tier: databaseConfig.tier || 'Basic',
                    capacity: databaseConfig.capacity || 5
                },
                tags: {
                    ...this.config.tags,
                    Name: databaseConfig.name,
                    Environment: databaseConfig.environment || 'development'
                }
            };

            const result = await AzureCircuitBreaker.execute(() =>
                sqlClient.databases.beginCreateOrUpdate(
                    this.config.resourceGroup,
                    serverName,
                    databaseConfig.name,
                    databaseParameters
                )
            );

            AzureMetrics.recordOperation('createSqlDatabase', Date.now() - startTime, true);
            return result;
        } catch (error) {
            AzureMetrics.recordOperation('createSqlDatabase', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * App Service Operations
     */
    async listWebApps() {
        const startTime = Date.now();
        try {
            const webClient = this.serviceManager.getClient('web');
            const apps = [];
            
            const appList = await AzureCircuitBreaker.execute(() =>
                webClient.webApps.listByResourceGroup(this.config.resourceGroup)
            );

            for await (const app of appList) {
                apps.push({
                    name: app.name,
                    id: app.id,
                    location: app.location,
                    kind: app.kind,
                    state: app.state,
                    defaultHostName: app.defaultHostName,
                    hostNames: app.hostNames,
                    tags: app.tags || {}
                });
            }

            AzureMetrics.recordOperation('listWebApps', Date.now() - startTime, true);
            return apps;
        } catch (error) {
            AzureMetrics.recordOperation('listWebApps', Date.now() - startTime, false);
            throw error;
        }
    }

    async createWebApp(appConfig) {
        const startTime = Date.now();
        try {
            const webClient = this.serviceManager.getClient('web');
            
            const appParameters = {
                location: this.config.location,
                kind: appConfig.kind || 'app',
                reserved: appConfig.reserved || false,
                sku: {
                    name: appConfig.sku || 'F1',
                    tier: appConfig.tier || 'Free'
                },
                siteConfig: {
                    linuxFxVersion: appConfig.linuxFxVersion || 'NODE|18-lts',
                    appSettings: appConfig.appSettings || []
                },
                tags: {
                    ...this.config.tags,
                    Name: appConfig.name,
                    Environment: appConfig.environment || 'development'
                }
            };

            const result = await AzureCircuitBreaker.execute(() =>
                webClient.webApps.beginCreateOrUpdate(
                    this.config.resourceGroup,
                    appConfig.name,
                    appParameters
                )
            );

            AzureMetrics.recordOperation('createWebApp', Date.now() - startTime, true);
            return result;
        } catch (error) {
            AzureMetrics.recordOperation('createWebApp', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Monitor Operations
     */
    async putMetric(metricData) {
        const startTime = Date.now();
        try {
            const monitorClient = this.serviceManager.getClient('monitor');
            
            const metricDataPoints = [{
                timeStamp: new Date(),
                value: metricData.value
            }];

            const result = await AzureCircuitBreaker.execute(() =>
                monitorClient.metrics.create(
                    metricData.resourceUri,
                    {
                        timespan: `${new Date(Date.now() - 60000).toISOString()}/${new Date().toISOString()}`,
                        interval: 'PT1M',
                        metricNames: [metricData.name],
                        aggregation: metricData.aggregation || 'Total'
                    }
                )
            );

            AzureMetrics.recordOperation('putMetric', Date.now() - startTime, true);
            return result;
        } catch (error) {
            AzureMetrics.recordOperation('putMetric', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Resource Group Operations
     */
    async listResourceGroups() {
        const startTime = Date.now();
        try {
            const resourcesClient = this.serviceManager.getClient('resources');
            const groups = [];
            
            const groupList = await AzureCircuitBreaker.execute(() =>
                resourcesClient.resourceGroups.list()
            );

            for await (const group of groupList) {
                groups.push({
                    name: group.name,
                    id: group.id,
                    location: group.location,
                    properties: group.properties,
                    tags: group.tags || {}
                });
            }

            AzureMetrics.recordOperation('listResourceGroups', Date.now() - startTime, true);
            return groups;
        } catch (error) {
            AzureMetrics.recordOperation('listResourceGroups', Date.now() - startTime, false);
            throw error;
        }
    }

    async createResourceGroup(groupConfig) {
        const startTime = Date.now();
        try {
            const resourcesClient = this.serviceManager.getClient('resources');
            
            const groupParameters = {
                location: groupConfig.location || this.config.location,
                tags: {
                    ...this.config.tags,
                    Name: groupConfig.name,
                    Environment: groupConfig.environment || 'development'
                }
            };

            const result = await AzureCircuitBreaker.execute(() =>
                resourcesClient.resourceGroups.createOrUpdate(
                    groupConfig.name,
                    groupParameters
                )
            );

            AzureMetrics.recordOperation('createResourceGroup', Date.now() - startTime, true);
            return result;
        } catch (error) {
            AzureMetrics.recordOperation('createResourceGroup', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Network Operations
     */
    async createNetworkInterface(nicConfig) {
        const startTime = Date.now();
        try {
            const networkClient = this.serviceManager.getClient('network');
            
            // Create virtual network if not exists
            let subnetId = nicConfig.subnetId;
            if (!subnetId) {
                const vnet = await this.createVirtualNetwork(nicConfig.vnetConfig);
                subnetId = vnet.subnets[0].id;
            }

            const nicParameters = {
                location: this.config.location,
                ipConfigurations: [{
                    name: 'ipconfig1',
                    subnet: {
                        id: subnetId
                    }
                }],
                tags: {
                    ...this.config.tags,
                    Name: nicConfig.name,
                    Environment: nicConfig.environment || 'development'
                }
            };

            const result = await AzureCircuitBreaker.execute(() =>
                networkClient.networkInterfaces.beginCreateOrUpdate(
                    this.config.resourceGroup,
                    nicConfig.name,
                    nicParameters
                )
            );

            AzureMetrics.recordOperation('createNetworkInterface', Date.now() - startTime, true);
            return result;
        } catch (error) {
            AzureMetrics.recordOperation('createNetworkInterface', Date.now() - startTime, false);
            throw error;
        }
    }

    async createVirtualNetwork(vnetConfig) {
        const startTime = Date.now();
        try {
            const networkClient = this.serviceManager.getClient('network');
            
            const vnetParameters = {
                location: this.config.location,
                addressSpace: {
                    addressPrefixes: [vnetConfig.addressPrefix || '10.0.0.0/16']
                },
                subnets: [{
                    name: 'default',
                    addressPrefix: vnetConfig.subnetPrefix || '10.0.0.0/24'
                }],
                tags: {
                    ...this.config.tags,
                    Name: vnetConfig.name,
                    Environment: vnetConfig.environment || 'development'
                }
            };

            const result = await AzureCircuitBreaker.execute(() =>
                networkClient.virtualNetworks.beginCreateOrUpdate(
                    this.config.resourceGroup,
                    vnetConfig.name,
                    vnetParameters
                )
            );

            AzureMetrics.recordOperation('createVirtualNetwork', Date.now() - startTime, true);
            return result;
        } catch (error) {
            AzureMetrics.recordOperation('createVirtualNetwork', Date.now() - startTime, false);
            throw error;
        }
    }

    /**
     * Utility Methods
     */
    getMetrics() {
        return AzureMetrics.getMetrics();
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
        console.log('Azure Operator cleanup completed');
    }
}

/**
 * Main execution function for TuskLang integration
 */
async function executeAzureOperator(operation, params = {}) {
    const startTime = Date.now();
    
    try {
        const azureOperator = new AzureOperator(params.config);
        
        let result;
        switch (operation) {
            case 'listVirtualMachines':
                result = await azureOperator.listVirtualMachines();
                break;
            case 'createVirtualMachine':
                result = await azureOperator.createVirtualMachine(params.vmConfig);
                break;
            case 'deleteVirtualMachine':
                result = await azureOperator.deleteVirtualMachine(params.vmName);
                break;
            case 'listStorageAccounts':
                result = await azureOperator.listStorageAccounts();
                break;
            case 'createStorageAccount':
                result = await azureOperator.createStorageAccount(params.accountConfig);
                break;
            case 'uploadBlob':
                result = await azureOperator.uploadBlob(params.storageAccountName, params.containerName, params.blobName, params.data, params.options);
                break;
            case 'downloadBlob':
                result = await azureOperator.downloadBlob(params.storageAccountName, params.containerName, params.blobName);
                break;
            case 'listSqlServers':
                result = await azureOperator.listSqlServers();
                break;
            case 'createSqlServer':
                result = await azureOperator.createSqlServer(params.serverConfig);
                break;
            case 'createSqlDatabase':
                result = await azureOperator.createSqlDatabase(params.serverName, params.databaseConfig);
                break;
            case 'listWebApps':
                result = await azureOperator.listWebApps();
                break;
            case 'createWebApp':
                result = await azureOperator.createWebApp(params.appConfig);
                break;
            case 'putMetric':
                result = await azureOperator.putMetric(params.metricData);
                break;
            case 'listResourceGroups':
                result = await azureOperator.listResourceGroups();
                break;
            case 'createResourceGroup':
                result = await azureOperator.createResourceGroup(params.groupConfig);
                break;
            case 'getMetrics':
                result = azureOperator.getMetrics();
                break;
            case 'getCircuitBreakerStatus':
                result = azureOperator.getCircuitBreakerStatus();
                break;
            default:
                throw new Error(`Unsupported Azure operation: ${operation}`);
        }

        const duration = Date.now() - startTime;
        console.log(`Azure Operation '${operation}' completed in ${duration}ms`);
        
        await azureOperator.cleanup();
        return {
            success: true,
            operation,
            duration,
            result,
            timestamp: new Date().toISOString()
        };
    } catch (error) {
        const duration = Date.now() - startTime;
        console.error(`Azure Operation '${operation}' failed after ${duration}ms:`, error.message);
        
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
    AzureOperator,
    executeAzureOperator,
    AzureOperatorConfig,
    AzureMetrics,
    AzureCircuitBreaker
}; 