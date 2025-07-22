/**
 * AI Model Management System for TuskTsk
 * Provides comprehensive model lifecycle management with versioning, storage, and deployment
 * 
 * @author AI-Cloud-Native-Engineer
 * @version 2.0.3
 * @license BBL
 */

const tf = require('@tensorflow/tfjs-node');
const fs = require('fs').promises;
const path = require('path');
const crypto = require('crypto');

class ModelManager {
    constructor() {
        this.models = new Map();
        this.versions = new Map();
        this.deployments = new Map();
        this.registry = new Map();
        this.isInitialized = false;
        this.modelStoragePath = './models';
        this.versionStoragePath = './model-versions';
        this.deploymentStoragePath = './deployments';
    }

    /**
     * Initialize model management system
     */
    async initialize() {
        try {
            // Create storage directories
            await this.ensureDirectoryExists(this.modelStoragePath);
            await this.ensureDirectoryExists(this.versionStoragePath);
            await this.ensureDirectoryExists(this.deploymentStoragePath);
            
            // Load existing models from storage
            await this.loadModelsFromStorage();
            
            this.isInitialized = true;
            console.log('✅ Model Manager initialized successfully');
            
            return {
                success: true,
                modelCount: this.models.size,
                versionCount: this.versions.size,
                deploymentCount: this.deployments.size
            };
        } catch (error) {
            console.error('❌ Model Manager initialization failed:', error);
            throw new Error(`Model Manager initialization failed: ${error.message}`);
        }
    }

    /**
     * Register a new model
     */
    async registerModel(modelInfo) {
        if (!this.isInitialized) {
            throw new Error('Model Manager not initialized. Call initialize() first.');
        }

        const {
            name,
            type,
            description,
            author,
            tags = [],
            metadata = {}
        } = modelInfo;

        try {
            // Generate model ID
            const modelId = this.generateModelId(name);
            
            // Create model record
            const model = {
                id: modelId,
                name: name,
                type: type,
                description: description,
                author: author,
                tags: tags,
                metadata: metadata,
                versions: [],
                created: new Date(),
                updated: new Date(),
                status: 'registered'
            };
            
            // Store model
            this.models.set(modelId, model);
            this.registry.set(name, modelId);
            
            // Save to storage
            await this.saveModelToStorage(model);
            
            console.log(`✅ Model '${name}' registered successfully`);
            return {
                success: true,
                modelId: modelId,
                name: name,
                status: model.status
            };
        } catch (error) {
            console.error(`❌ Model registration failed: ${error.message}`);
            throw new Error(`Model registration failed: ${error.message}`);
        }
    }

    /**
     * Create a new model version
     */
    async createVersion(modelId, versionInfo) {
        if (!this.models.has(modelId)) {
            throw new Error(`Model '${modelId}' not found`);
        }

        const {
            version,
            modelPath,
            description,
            performance = {},
            dependencies = {},
            config = {}
        } = versionInfo;

        try {
            // Generate version ID
            const versionId = this.generateVersionId(modelId, version);
            
            // Create version record
            const modelVersion = {
                id: versionId,
                modelId: modelId,
                version: version,
                description: description,
                modelPath: modelPath,
                performance: performance,
                dependencies: dependencies,
                config: config,
                created: new Date(),
                status: 'created',
                checksum: await this.calculateChecksum(modelPath)
            };
            
            // Store version
            this.versions.set(versionId, modelVersion);
            
            // Update model
            const model = this.models.get(modelId);
            model.versions.push(versionId);
            model.updated = new Date();
            
            // Save to storage
            await this.saveVersionToStorage(modelVersion);
            await this.saveModelToStorage(model);
            
            console.log(`✅ Version '${version}' created for model '${model.name}'`);
            return {
                success: true,
                versionId: versionId,
                modelId: modelId,
                version: version,
                status: modelVersion.status
            };
        } catch (error) {
            console.error(`❌ Version creation failed: ${error.message}`);
            throw new Error(`Version creation failed: ${error.message}`);
        }
    }

    /**
     * Deploy a model version
     */
    async deployVersion(versionId, deploymentConfig) {
        if (!this.versions.has(versionId)) {
            throw new Error(`Version '${versionId}' not found`);
        }

        const {
            environment = 'production',
            replicas = 1,
            resources = {},
            scaling = {},
            monitoring = {}
        } = deploymentConfig;

        try {
            const version = this.versions.get(versionId);
            const model = this.models.get(version.modelId);
            
            // Generate deployment ID
            const deploymentId = this.generateDeploymentId(versionId, environment);
            
            // Create deployment record
            const deployment = {
                id: deploymentId,
                versionId: versionId,
                modelId: version.modelId,
                modelName: model.name,
                version: version.version,
                environment: environment,
                replicas: replicas,
                resources: resources,
                scaling: scaling,
                monitoring: monitoring,
                status: 'deploying',
                created: new Date(),
                updated: new Date()
            };
            
            // Store deployment
            this.deployments.set(deploymentId, deployment);
            
            // Update version status
            version.status = 'deployed';
            version.deploymentId = deploymentId;
            
            // Save to storage
            await this.saveDeploymentToStorage(deployment);
            await this.saveVersionToStorage(version);
            
            console.log(`✅ Version '${version.version}' deployed to ${environment}`);
            return {
                success: true,
                deploymentId: deploymentId,
                versionId: versionId,
                environment: environment,
                status: deployment.status
            };
        } catch (error) {
            console.error(`❌ Version deployment failed: ${error.message}`);
            throw new Error(`Version deployment failed: ${error.message}`);
        }
    }

    /**
     * Load a model version
     */
    async loadVersion(versionId) {
        if (!this.versions.has(versionId)) {
            throw new Error(`Version '${versionId}' not found`);
        }

        try {
            const version = this.versions.get(versionId);
            const modelPath = version.modelPath;
            
            // Load TensorFlow.js model
            const model = await tf.loadLayersModel(`file://${modelPath}/model.json`);
            
            console.log(`✅ Version '${version.version}' loaded successfully`);
            return {
                success: true,
                versionId: versionId,
                model: model,
                config: version.config
            };
        } catch (error) {
            console.error(`❌ Version loading failed: ${error.message}`);
            throw new Error(`Version loading failed: ${error.message}`);
        }
    }

    /**
     * Update model performance metrics
     */
    async updatePerformance(versionId, performanceMetrics) {
        if (!this.versions.has(versionId)) {
            throw new Error(`Version '${versionId}' not found`);
        }

        try {
            const version = this.versions.get(versionId);
            version.performance = {
                ...version.performance,
                ...performanceMetrics,
                lastUpdated: new Date()
            };
            
            // Save to storage
            await this.saveVersionToStorage(version);
            
            console.log(`✅ Performance metrics updated for version '${version.version}'`);
            return {
                success: true,
                versionId: versionId,
                performance: version.performance
            };
        } catch (error) {
            console.error(`❌ Performance update failed: ${error.message}`);
            throw new Error(`Performance update failed: ${error.message}`);
        }
    }

    /**
     * Rollback to previous version
     */
    async rollbackVersion(modelId, targetVersion) {
        if (!this.models.has(modelId)) {
            throw new Error(`Model '${modelId}' not found`);
        }

        try {
            const model = this.models.get(modelId);
            const targetVersionId = model.versions.find(vId => {
                const version = this.versions.get(vId);
                return version.version === targetVersion;
            });
            
            if (!targetVersionId) {
                throw new Error(`Version '${targetVersion}' not found for model '${model.name}'`);
            }
            
            // Find current deployment
            const currentDeployment = Array.from(this.deployments.values())
                .find(d => d.modelId === modelId && d.status === 'active');
            
            if (currentDeployment) {
                // Update current deployment to inactive
                currentDeployment.status = 'inactive';
                currentDeployment.updated = new Date();
                await this.saveDeploymentToStorage(currentDeployment);
            }
            
            // Deploy target version
            const deploymentResult = await this.deployVersion(targetVersionId, {
                environment: currentDeployment?.environment || 'production',
                replicas: currentDeployment?.replicas || 1
            });
            
            console.log(`✅ Rolled back to version '${targetVersion}'`);
            return {
                success: true,
                targetVersion: targetVersion,
                deploymentId: deploymentResult.deploymentId
            };
        } catch (error) {
            console.error(`❌ Version rollback failed: ${error.message}`);
            throw new Error(`Version rollback failed: ${error.message}`);
        }
    }

    /**
     * Get model information
     */
    getModelInfo(modelId) {
        if (!this.models.has(modelId)) {
            throw new Error(`Model '${modelId}' not found`);
        }

        const model = this.models.get(modelId);
        const versions = model.versions.map(vId => {
            const version = this.versions.get(vId);
            return {
                id: vId,
                version: version.version,
                description: version.description,
                status: version.status,
                created: version.created,
                performance: version.performance
            };
        });
        
        return {
            ...model,
            versions: versions
        };
    }

    /**
     * List all models
     */
    listModels() {
        return Array.from(this.models.values()).map(model => ({
            id: model.id,
            name: model.name,
            type: model.type,
            description: model.description,
            status: model.status,
            versionCount: model.versions.length,
            created: model.created,
            updated: model.updated
        }));
    }

    /**
     * List all versions of a model
     */
    listVersions(modelId) {
        if (!this.models.has(modelId)) {
            throw new Error(`Model '${modelId}' not found`);
        }

        const model = this.models.get(modelId);
        return model.versions.map(vId => {
            const version = this.versions.get(vId);
            return {
                id: vId,
                version: version.version,
                description: version.description,
                status: version.status,
                created: version.created,
                performance: version.performance
            };
        });
    }

    /**
     * List all deployments
     */
    listDeployments() {
        return Array.from(this.deployments.values()).map(deployment => ({
            id: deployment.id,
            modelName: deployment.modelName,
            version: deployment.version,
            environment: deployment.environment,
            status: deployment.status,
            replicas: deployment.replicas,
            created: deployment.created,
            updated: deployment.updated
        }));
    }

    /**
     * Delete a model
     */
    async deleteModel(modelId) {
        if (!this.models.has(modelId)) {
            throw new Error(`Model '${modelId}' not found`);
        }

        try {
            const model = this.models.get(modelId);
            
            // Delete all versions
            for (const versionId of model.versions) {
                await this.deleteVersion(versionId);
            }
            
            // Delete model
            this.models.delete(modelId);
            this.registry.delete(model.name);
            
            // Remove from storage
            await this.removeModelFromStorage(modelId);
            
            console.log(`✅ Model '${model.name}' deleted`);
            return {
                success: true,
                modelId: modelId,
                name: model.name
            };
        } catch (error) {
            console.error(`❌ Model deletion failed: ${error.message}`);
            throw new Error(`Model deletion failed: ${error.message}`);
        }
    }

    /**
     * Delete a version
     */
    async deleteVersion(versionId) {
        if (!this.versions.has(versionId)) {
            throw new Error(`Version '${versionId}' not found`);
        }

        try {
            const version = this.versions.get(versionId);
            
            // Remove from model
            const model = this.models.get(version.modelId);
            model.versions = model.versions.filter(vId => vId !== versionId);
            model.updated = new Date();
            
            // Delete version
            this.versions.delete(versionId);
            
            // Remove from storage
            await this.removeVersionFromStorage(versionId);
            await this.saveModelToStorage(model);
            
            console.log(`✅ Version '${version.version}' deleted`);
            return {
                success: true,
                versionId: versionId,
                version: version.version
            };
        } catch (error) {
            console.error(`❌ Version deletion failed: ${error.message}`);
            throw new Error(`Version deletion failed: ${error.message}`);
        }
    }

    /**
     * Generate model ID
     */
    generateModelId(name) {
        const timestamp = Date.now();
        const hash = crypto.createHash('md5').update(`${name}-${timestamp}`).digest('hex');
        return `model_${hash.substring(0, 8)}`;
    }

    /**
     * Generate version ID
     */
    generateVersionId(modelId, version) {
        const hash = crypto.createHash('md5').update(`${modelId}-${version}`).digest('hex');
        return `version_${hash.substring(0, 8)}`;
    }

    /**
     * Generate deployment ID
     */
    generateDeploymentId(versionId, environment) {
        const timestamp = Date.now();
        const hash = crypto.createHash('md5').update(`${versionId}-${environment}-${timestamp}`).digest('hex');
        return `deployment_${hash.substring(0, 8)}`;
    }

    /**
     * Calculate file checksum
     */
    async calculateChecksum(filePath) {
        try {
            const data = await fs.readFile(filePath);
            return crypto.createHash('md5').update(data).digest('hex');
        } catch (error) {
            console.warn(`Could not calculate checksum for ${filePath}: ${error.message}`);
            return null;
        }
    }

    /**
     * Ensure directory exists
     */
    async ensureDirectoryExists(dirPath) {
        try {
            await fs.access(dirPath);
        } catch {
            await fs.mkdir(dirPath, { recursive: true });
        }
    }

    /**
     * Save model to storage
     */
    async saveModelToStorage(model) {
        const filePath = path.join(this.modelStoragePath, `${model.id}.json`);
        await fs.writeFile(filePath, JSON.stringify(model, null, 2));
    }

    /**
     * Save version to storage
     */
    async saveVersionToStorage(version) {
        const filePath = path.join(this.versionStoragePath, `${version.id}.json`);
        await fs.writeFile(filePath, JSON.stringify(version, null, 2));
    }

    /**
     * Save deployment to storage
     */
    async saveDeploymentToStorage(deployment) {
        const filePath = path.join(this.deploymentStoragePath, `${deployment.id}.json`);
        await fs.writeFile(filePath, JSON.stringify(deployment, null, 2));
    }

    /**
     * Load models from storage
     */
    async loadModelsFromStorage() {
        try {
            const files = await fs.readdir(this.modelStoragePath);
            for (const file of files) {
                if (file.endsWith('.json')) {
                    const filePath = path.join(this.modelStoragePath, file);
                    const data = await fs.readFile(filePath, 'utf8');
                    const model = JSON.parse(data);
                    this.models.set(model.id, model);
                    this.registry.set(model.name, model.id);
                }
            }
        } catch (error) {
            console.warn('Could not load models from storage:', error.message);
        }
    }

    /**
     * Remove model from storage
     */
    async removeModelFromStorage(modelId) {
        try {
            const filePath = path.join(this.modelStoragePath, `${modelId}.json`);
            await fs.unlink(filePath);
        } catch (error) {
            console.warn(`Could not remove model file: ${error.message}`);
        }
    }

    /**
     * Remove version from storage
     */
    async removeVersionFromStorage(versionId) {
        try {
            const filePath = path.join(this.versionStoragePath, `${versionId}.json`);
            await fs.unlink(filePath);
        } catch (error) {
            console.warn(`Could not remove version file: ${error.message}`);
        }
    }

    /**
     * Get manager information
     */
    getInfo() {
        return {
            initialized: this.isInitialized,
            models: this.models.size,
            versions: this.versions.size,
            deployments: this.deployments.size,
            storagePaths: {
                models: this.modelStoragePath,
                versions: this.versionStoragePath,
                deployments: this.deploymentStoragePath
            }
        };
    }

    /**
     * Clean up resources
     */
    async cleanup() {
        this.models.clear();
        this.versions.clear();
        this.deployments.clear();
        this.registry.clear();
        
        console.log('✅ Model Manager resources cleaned up');
    }
}

// Export the class
module.exports = ModelManager;

// Create a singleton instance
const modelManager = new ModelManager();

// Export the singleton instance
module.exports.instance = modelManager; 