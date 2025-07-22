/**
 * TensorFlow.js Core Integration for TuskTsk
 * Provides comprehensive AI/ML capabilities with TensorFlow.js
 * 
 * @author AI-Cloud-Native-Engineer
 * @version 2.0.3
 * @license BBL
 */

const tf = require('@tensorflow/tfjs-node');
const fs = require('fs').promises;
const path = require('path');

class TensorFlowCore {
    constructor() {
        this.models = new Map();
        this.trainingHistory = new Map();
        this.modelRegistry = new Map();
        this.isInitialized = false;
        this.gpuAvailable = false;
        this.memoryUsage = {
            allocated: 0,
            peak: 0
        };
    }

    /**
     * Initialize TensorFlow.js with optimal settings
     */
    async initialize() {
        try {
            // Check for GPU availability
            this.gpuAvailable = await tf.getBackend() === 'tensorflow';
            
            // Configure memory management
            await tf.setBackend('tensorflow');
            await tf.ready();
            
            // Set memory growth to prevent OOM
            if (this.gpuAvailable) {
                const gpu = tf.backend().gpu;
                if (gpu && gpu.setMemoryGrowth) {
                    gpu.setMemoryGrowth(true);
                }
            }
            
            this.isInitialized = true;
            console.log(`✅ TensorFlow.js initialized. GPU: ${this.gpuAvailable ? 'Available' : 'CPU Only'}`);
            
            return {
                success: true,
                gpu: this.gpuAvailable,
                backend: await tf.getBackend(),
                version: tf.version.tfjs
            };
        } catch (error) {
            console.error('❌ TensorFlow.js initialization failed:', error);
            throw new Error(`TensorFlow.js initialization failed: ${error.message}`);
        }
    }

    /**
     * Create a neural network model
     */
    createModel(config) {
        if (!this.isInitialized) {
            throw new Error('TensorFlow.js not initialized. Call initialize() first.');
        }

        const {
            name,
            layers,
            optimizer = 'adam',
            loss = 'categoricalCrossentropy',
            metrics = ['accuracy']
        } = config;

        try {
            const model = tf.sequential();
            
            // Add layers
            layers.forEach(layerConfig => {
                const layer = this.createLayer(layerConfig);
                model.add(layer);
            });

            // Compile model
            model.compile({
                optimizer: this.createOptimizer(optimizer),
                loss: loss,
                metrics: metrics
            });

            // Store model
            this.models.set(name, {
                model: model,
                config: config,
                created: new Date(),
                version: '1.0.0'
            });

            console.log(`✅ Model '${name}' created successfully`);
            return {
                success: true,
                modelName: name,
                layers: model.layers.length,
                parameters: model.countParams()
            };
        } catch (error) {
            console.error(`❌ Model creation failed: ${error.message}`);
            throw new Error(`Model creation failed: ${error.message}`);
        }
    }

    /**
     * Create a layer based on configuration
     */
    createLayer(layerConfig) {
        const { type, ...params } = layerConfig;
        
        switch (type.toLowerCase()) {
            case 'dense':
                return tf.layers.dense(params);
            case 'conv2d':
                return tf.layers.conv2d(params);
            case 'maxpooling2d':
                return tf.layers.maxPooling2d(params);
            case 'dropout':
                return tf.layers.dropout(params);
            case 'flatten':
                return tf.layers.flatten(params);
            case 'lstm':
                return tf.layers.lstm(params);
            case 'gru':
                return tf.layers.gru(params);
            case 'embedding':
                return tf.layers.embedding(params);
            default:
                throw new Error(`Unknown layer type: ${type}`);
        }
    }

    /**
     * Create optimizer based on configuration
     */
    createOptimizer(optimizerConfig) {
        if (typeof optimizerConfig === 'string') {
            return optimizerConfig;
        }

        const { type, ...params } = optimizerConfig;
        
        switch (type.toLowerCase()) {
            case 'adam':
                return tf.train.adam(params.learningRate || 0.001);
            case 'sgd':
                return tf.train.sgd(params.learningRate || 0.01);
            case 'rmsprop':
                return tf.train.rmsprop(params.learningRate || 0.001);
            case 'adagrad':
                return tf.train.adagrad(params.learningRate || 0.01);
            default:
                return tf.train.adam(0.001);
        }
    }

    /**
     * Train a model
     */
    async trainModel(modelName, trainingData, options = {}) {
        if (!this.models.has(modelName)) {
            throw new Error(`Model '${modelName}' not found`);
        }

        const modelInfo = this.models.get(modelName);
        const model = modelInfo.model;

        const {
            epochs = 10,
            batchSize = 32,
            validationSplit = 0.2,
            callbacks = [],
            verbose = 1
        } = options;

        try {
            // Prepare training data
            const { xs, ys } = this.prepareTrainingData(trainingData);
            
            // Add default callbacks
            const defaultCallbacks = [
                tf.callbacks.earlyStopping({ patience: 5 }),
                tf.callbacks.modelCheckpoint(`./models/${modelName}_checkpoint`),
                this.createMemoryCallback()
            ];

            const allCallbacks = [...defaultCallbacks, ...callbacks];

            // Train the model
            const history = await model.fit(xs, ys, {
                epochs: epochs,
                batchSize: batchSize,
                validationSplit: validationSplit,
                callbacks: allCallbacks,
                verbose: verbose
            });

            // Store training history
            this.trainingHistory.set(modelName, {
                history: history,
                timestamp: new Date(),
                options: options
            });

            // Update memory usage
            this.updateMemoryUsage();

            console.log(`✅ Model '${modelName}' training completed`);
            return {
                success: true,
                modelName: modelName,
                epochs: epochs,
                finalLoss: history.history.loss[history.history.loss.length - 1],
                finalAccuracy: history.history.acc ? history.history.acc[history.history.acc.length - 1] : null,
                memoryUsage: this.memoryUsage
            };
        } catch (error) {
            console.error(`❌ Model training failed: ${error.message}`);
            throw new Error(`Model training failed: ${error.message}`);
        }
    }

    /**
     * Prepare training data for TensorFlow.js
     */
    prepareTrainingData(data) {
        if (Array.isArray(data)) {
            // Assume data is in format [{input: [...], output: [...]}]
            const inputs = data.map(item => item.input);
            const outputs = data.map(item => item.output);
            
            return {
                xs: tf.tensor2d(inputs),
                ys: tf.tensor2d(outputs)
            };
        } else if (data.xs && data.ys) {
            // Data already in TensorFlow format
            return {
                xs: tf.tensor(data.xs),
                ys: tf.tensor(data.ys)
            };
        } else {
            throw new Error('Invalid training data format');
        }
    }

    /**
     * Create memory monitoring callback
     */
    createMemoryCallback() {
        return {
            onBatchEnd: async (batch, logs) => {
                this.updateMemoryUsage();
            }
        };
    }

    /**
     * Update memory usage statistics
     */
    updateMemoryUsage() {
        const info = tf.memory();
        this.memoryUsage.allocated = info.numBytes;
        this.memoryUsage.peak = Math.max(this.memoryUsage.peak, info.numBytes);
    }

    /**
     * Make predictions with a trained model
     */
    async predict(modelName, inputData) {
        if (!this.models.has(modelName)) {
            throw new Error(`Model '${modelName}' not found`);
        }

        const model = this.models.get(modelName).model;

        try {
            // Prepare input data
            const input = this.prepareInputData(inputData);
            
            // Make prediction
            const startTime = Date.now();
            const prediction = await model.predict(input);
            const inferenceTime = Date.now() - startTime;

            // Convert to regular array
            const result = await prediction.array();

            // Clean up tensors
            input.dispose();
            prediction.dispose();

            return {
                success: true,
                prediction: result,
                inferenceTime: inferenceTime,
                modelName: modelName
            };
        } catch (error) {
            console.error(`❌ Prediction failed: ${error.message}`);
            throw new Error(`Prediction failed: ${error.message}`);
        }
    }

    /**
     * Prepare input data for prediction
     */
    prepareInputData(data) {
        if (Array.isArray(data)) {
            return tf.tensor(data);
        } else if (data instanceof tf.Tensor) {
            return data;
        } else {
            throw new Error('Invalid input data format');
        }
    }

    /**
     * Save a model to disk
     */
    async saveModel(modelName, filepath) {
        if (!this.models.has(modelName)) {
            throw new Error(`Model '${modelName}' not found`);
        }

        try {
            const model = this.models.get(modelName).model;
            await model.save(`file://${filepath}`);
            
            console.log(`✅ Model '${modelName}' saved to ${filepath}`);
            return {
                success: true,
                modelName: modelName,
                filepath: filepath
            };
        } catch (error) {
            console.error(`❌ Model save failed: ${error.message}`);
            throw new Error(`Model save failed: ${error.message}`);
        }
    }

    /**
     * Load a model from disk
     */
    async loadModel(modelName, filepath) {
        try {
            const model = await tf.loadLayersModel(`file://${filepath}/model.json`);
            
            this.models.set(modelName, {
                model: model,
                config: { name: modelName },
                created: new Date(),
                version: '1.0.0'
            });

            console.log(`✅ Model '${modelName}' loaded from ${filepath}`);
            return {
                success: true,
                modelName: modelName,
                filepath: filepath
            };
        } catch (error) {
            console.error(`❌ Model load failed: ${error.message}`);
            throw new Error(`Model load failed: ${error.message}`);
        }
    }

    /**
     * Get model information
     */
    getModelInfo(modelName) {
        if (!this.models.has(modelName)) {
            throw new Error(`Model '${modelName}' not found`);
        }

        const modelInfo = this.models.get(modelName);
        const model = modelInfo.model;

        return {
            name: modelName,
            layers: model.layers.length,
            parameters: model.countParams(),
            created: modelInfo.created,
            version: modelInfo.version,
            config: modelInfo.config
        };
    }

    /**
     * List all available models
     */
    listModels() {
        return Array.from(this.models.keys()).map(name => this.getModelInfo(name));
    }

    /**
     * Delete a model
     */
    deleteModel(modelName) {
        if (!this.models.has(modelName)) {
            throw new Error(`Model '${modelName}' not found`);
        }

        const modelInfo = this.models.get(modelName);
        modelInfo.model.dispose();
        this.models.delete(modelName);
        this.trainingHistory.delete(modelName);

        console.log(`✅ Model '${modelName}' deleted`);
        return {
            success: true,
            modelName: modelName
        };
    }

    /**
     * Get system information
     */
    getSystemInfo() {
        return {
            initialized: this.isInitialized,
            gpuAvailable: this.gpuAvailable,
            backend: tf.getBackend(),
            version: tf.version.tfjs,
            memoryUsage: this.memoryUsage,
            modelCount: this.models.size
        };
    }

    /**
     * Clean up resources
     */
    async cleanup() {
        // Dispose all models
        for (const [name, modelInfo] of this.models) {
            modelInfo.model.dispose();
        }
        
        this.models.clear();
        this.trainingHistory.clear();
        
        // Clear TensorFlow memory
        tf.disposeVariables();
        
        console.log('✅ TensorFlow.js resources cleaned up');
    }
}

// Export the class
module.exports = TensorFlowCore;

// Create a singleton instance
const tensorFlowCore = new TensorFlowCore();

// Export the singleton instance
module.exports.instance = tensorFlowCore; 