/**
 * AI/ML Core Integration for TuskTsk
 * Provides unified interface for all AI/ML capabilities
 * 
 * @author AI-Cloud-Native-Engineer
 * @version 2.0.3
 * @license BBL
 */

const TensorFlowCore = require('./tensorflow-core');
const NLPEngine = require('./nlp-engine');
const DataAnalysisEngine = require('./data-analysis');
const ModelManager = require('./model-manager');

class AICore {
    constructor() {
        this.tensorflow = TensorFlowCore.instance;
        this.nlp = NLPEngine.instance;
        this.dataAnalysis = DataAnalysisEngine.instance;
        this.modelManager = ModelManager.instance;
        this.isInitialized = false;
        this.capabilities = new Map();
        this.activeModels = new Map();
        this.performanceMetrics = new Map();
    }

    /**
     * Initialize AI/ML core with all components
     */
    async initialize(config = {}) {
        try {
            console.log('🚀 Initializing AI/ML Core...');

            // Initialize TensorFlow.js
            console.log('📊 Initializing TensorFlow.js...');
            await this.tensorflow.initialize();

            // Initialize NLP Engine
            console.log('🗣️ Initializing NLP Engine...');
            await this.nlp.initialize();

            // Initialize Data Analysis Engine
            console.log('📈 Initializing Data Analysis Engine...');
            await this.dataAnalysis.initialize();

            // Initialize Model Manager
            console.log('🏗️ Initializing Model Manager...');
            await this.modelManager.initialize();

            // Register capabilities
            this.registerCapabilities();

            this.isInitialized = true;
            console.log('✅ AI/ML Core initialized successfully');
            
            return {
                success: true,
                tensorflow: await this.tensorflow.getSystemInfo(),
                nlp: this.nlp.getInfo(),
                dataAnalysis: this.dataAnalysis.getInfo(),
                modelManager: this.modelManager.getInfo(),
                capabilities: Array.from(this.capabilities.keys())
            };
        } catch (error) {
            console.error('❌ AI/ML Core initialization failed:', error);
            throw new Error(`AI/ML Core initialization failed: ${error.message}`);
        }
    }

    /**
     * Register available AI/ML capabilities
     */
    registerCapabilities() {
        // TensorFlow.js capabilities
        this.capabilities.set('neural_networks', {
            name: 'Neural Networks',
            description: 'Create, train, and deploy neural networks',
            provider: 'tensorflow',
            methods: ['createModel', 'trainModel', 'predict', 'saveModel', 'loadModel']
        });

        this.capabilities.set('natural_language_processing', {
            name: 'Natural Language Processing',
            description: 'Text analysis, sentiment analysis, entity extraction',
            provider: 'nlp',
            methods: ['tokenize', 'analyzeSentiment', 'extractEntities', 'classifyText', 'analyzeText']
        });

        this.capabilities.set('data_analysis', {
            name: 'Data Analysis & Visualization',
            description: 'Statistical analysis, data visualization, clustering',
            provider: 'dataAnalysis',
            methods: ['analyzeDataset', 'createChart', 'detectOutliers', 'performClustering']
        });

        this.capabilities.set('model_management', {
            name: 'Model Management',
            description: 'Model versioning, deployment, and lifecycle management',
            provider: 'modelManager',
            methods: ['registerModel', 'createVersion', 'deployVersion', 'loadVersion']
        });

        this.capabilities.set('machine_learning', {
            name: 'Machine Learning',
            description: 'Comprehensive ML workflows and pipelines',
            provider: 'integrated',
            methods: ['trainPipeline', 'evaluateModel', 'optimizeHyperparameters', 'crossValidate']
        });
    }

    /**
     * Create and train a complete ML pipeline
     */
    async createMLPipeline(pipelineConfig) {
        if (!this.isInitialized) {
            throw new Error('AI/ML Core not initialized. Call initialize() first.');
        }

        const {
            name,
            type,
            data,
            preprocessing = {},
            modelConfig = {},
            trainingConfig = {},
            evaluationConfig = {}
        } = pipelineConfig;

        try {
            console.log(`🔧 Creating ML pipeline: ${name}`);

            // Step 1: Data preprocessing
            const processedData = await this.preprocessData(data, preprocessing);

            // Step 2: Create model
            const modelResult = await this.tensorflow.createModel({
                name: name,
                ...modelConfig
            });

            // Step 3: Train model
            const trainingResult = await this.tensorflow.trainModel(
                name,
                processedData,
                trainingConfig
            );

            // Step 4: Evaluate model
            const evaluationResult = await this.evaluateModel(name, processedData, evaluationConfig);

            // Step 5: Register model in model manager
            const modelInfo = await this.modelManager.registerModel({
                name: name,
                type: type,
                description: `ML pipeline: ${name}`,
                author: 'AI-Cloud-Native-Engineer',
                tags: ['ml-pipeline', type]
            });

            // Step 6: Create version
            const versionResult = await this.modelManager.createVersion(modelInfo.modelId, {
                version: '1.0.0',
                modelPath: `./models/${name}`,
                description: 'Initial version',
                performance: evaluationResult.metrics
            });

            // Store active model
            this.activeModels.set(name, {
                modelId: modelInfo.modelId,
                versionId: versionResult.versionId,
                type: type,
                created: new Date(),
                performance: evaluationResult.metrics
            });

            console.log(`✅ ML pipeline '${name}' created successfully`);
            return {
                success: true,
                name: name,
                modelId: modelInfo.modelId,
                versionId: versionResult.versionId,
                training: trainingResult,
                evaluation: evaluationResult,
                performance: evaluationResult.metrics
            };
        } catch (error) {
            console.error(`❌ ML pipeline creation failed: ${error.message}`);
            throw new Error(`ML pipeline creation failed: ${error.message}`);
        }
    }

    /**
     * Preprocess data for ML training
     */
    async preprocessData(data, preprocessing) {
        const {
            normalize = false,
            scale = false,
            encode = false,
            clean = false,
            split = { train: 0.8, validation: 0.1, test: 0.1 }
        } = preprocessing;

        try {
            let processedData = data;

            // Data cleaning
            if (clean) {
                processedData = this.cleanData(processedData);
            }

            // Data normalization
            if (normalize) {
                processedData = this.normalizeData(processedData);
            }

            // Data scaling
            if (scale) {
                processedData = this.scaleData(processedData);
            }

            // Data encoding
            if (encode) {
                processedData = this.encodeData(processedData);
            }

            // Data splitting
            const splitData = this.splitData(processedData, split);

            return splitData;
        } catch (error) {
            console.error(`❌ Data preprocessing failed: ${error.message}`);
            throw new Error(`Data preprocessing failed: ${error.message}`);
        }
    }

    /**
     * Clean data
     */
    cleanData(data) {
        // Remove null/undefined values
        return data.filter(item => item !== null && item !== undefined);
    }

    /**
     * Normalize data
     */
    normalizeData(data) {
        // Simple min-max normalization
        const min = Math.min(...data);
        const max = Math.max(...data);
        return data.map(value => (value - min) / (max - min));
    }

    /**
     * Scale data
     */
    scaleData(data) {
        // Z-score standardization
        const mean = data.reduce((sum, val) => sum + val, 0) / data.length;
        const std = Math.sqrt(data.reduce((sum, val) => sum + Math.pow(val - mean, 2), 0) / data.length);
        return data.map(value => (value - mean) / std);
    }

    /**
     * Encode data
     */
    encodeData(data) {
        // Simple one-hot encoding for categorical data
        const uniqueValues = [...new Set(data)];
        return data.map(value => {
            const encoding = new Array(uniqueValues.length).fill(0);
            const index = uniqueValues.indexOf(value);
            if (index !== -1) {
                encoding[index] = 1;
            }
            return encoding;
        });
    }

    /**
     * Split data into train/validation/test sets
     */
    splitData(data, split) {
        const shuffled = [...data].sort(() => Math.random() - 0.5);
        const trainSize = Math.floor(data.length * split.train);
        const validationSize = Math.floor(data.length * split.validation);

        return {
            train: shuffled.slice(0, trainSize),
            validation: shuffled.slice(trainSize, trainSize + validationSize),
            test: shuffled.slice(trainSize + validationSize)
        };
    }

    /**
     * Evaluate model performance
     */
    async evaluateModel(modelName, data, evaluationConfig) {
        const {
            metrics = ['accuracy', 'precision', 'recall', 'f1'],
            crossValidation = false,
            folds = 5
        } = evaluationConfig;

        try {
            let results = {};

            if (crossValidation) {
                results = await this.crossValidate(modelName, data, folds, metrics);
            } else {
                results = await this.singleEvaluation(modelName, data, metrics);
            }

            return {
                success: true,
                modelName: modelName,
                metrics: results,
                evaluationType: crossValidation ? 'cross_validation' : 'single_evaluation'
            };
        } catch (error) {
            console.error(`❌ Model evaluation failed: ${error.message}`);
            throw new Error(`Model evaluation failed: ${error.message}`);
        }
    }

    /**
     * Single evaluation
     */
    async singleEvaluation(modelName, data, metrics) {
        const predictions = [];
        const actuals = [];

        // Make predictions
        for (const item of data.test) {
            const prediction = await this.tensorflow.predict(modelName, item.input);
            predictions.push(prediction.prediction[0]);
            actuals.push(item.output[0]);
        }

        // Calculate metrics
        return this.calculateMetrics(predictions, actuals, metrics);
    }

    /**
     * Cross-validation
     */
    async crossValidate(modelName, data, folds, metrics) {
        const foldResults = [];

        for (let i = 0; i < folds; i++) {
            // Split data for this fold
            const foldData = this.createFold(data, i, folds);
            
            // Train model on this fold
            await this.tensorflow.trainModel(modelName, foldData.train, { epochs: 5 });
            
            // Evaluate on validation set
            const foldResult = await this.singleEvaluation(modelName, foldData, metrics);
            foldResults.push(foldResult.metrics);
        }

        // Average results across folds
        return this.averageMetrics(foldResults);
    }

    /**
     * Create fold for cross-validation
     */
    createFold(data, foldIndex, totalFolds) {
        const foldSize = Math.floor(data.length / totalFolds);
        const start = foldIndex * foldSize;
        const end = start + foldSize;

        const validation = data.slice(start, end);
        const train = [...data.slice(0, start), ...data.slice(end)];

        return { train, validation, test: validation };
    }

    /**
     * Calculate metrics
     */
    calculateMetrics(predictions, actuals, metrics) {
        const results = {};

        if (metrics.includes('accuracy')) {
            results.accuracy = this.calculateAccuracy(predictions, actuals);
        }

        if (metrics.includes('precision')) {
            results.precision = this.calculatePrecision(predictions, actuals);
        }

        if (metrics.includes('recall')) {
            results.recall = this.calculateRecall(predictions, actuals);
        }

        if (metrics.includes('f1')) {
            results.f1 = this.calculateF1Score(results.precision, results.recall);
        }

        return results;
    }

    /**
     * Calculate accuracy
     */
    calculateAccuracy(predictions, actuals) {
        const correct = predictions.filter((pred, i) => pred === actuals[i]).length;
        return correct / predictions.length;
    }

    /**
     * Calculate precision
     */
    calculatePrecision(predictions, actuals) {
        const truePositives = predictions.filter((pred, i) => pred === 1 && actuals[i] === 1).length;
        const falsePositives = predictions.filter((pred, i) => pred === 1 && actuals[i] === 0).length;
        return truePositives / (truePositives + falsePositives) || 0;
    }

    /**
     * Calculate recall
     */
    calculateRecall(predictions, actuals) {
        const truePositives = predictions.filter((pred, i) => pred === 1 && actuals[i] === 1).length;
        const falseNegatives = predictions.filter((pred, i) => pred === 0 && actuals[i] === 1).length;
        return truePositives / (truePositives + falseNegatives) || 0;
    }

    /**
     * Calculate F1 score
     */
    calculateF1Score(precision, recall) {
        return 2 * (precision * recall) / (precision + recall) || 0;
    }

    /**
     * Average metrics across folds
     */
    averageMetrics(foldResults) {
        const averaged = {};
        const metricNames = Object.keys(foldResults[0]);

        for (const metric of metricNames) {
            const sum = foldResults.reduce((acc, result) => acc + result[metric], 0);
            averaged[metric] = sum / foldResults.length;
        }

        return averaged;
    }

    /**
     * Perform comprehensive text analysis
     */
    async analyzeText(text, options = {}) {
        if (!this.isInitialized) {
            throw new Error('AI/ML Core not initialized. Call initialize() first.');
        }

        try {
            const analysis = await this.nlp.analyzeText(text, options);
            
            // Add AI insights
            analysis.analysis.aiInsights = await this.generateAIInsights(analysis.analysis);
            
            return analysis;
        } catch (error) {
            console.error(`❌ Text analysis failed: ${error.message}`);
            throw new Error(`Text analysis failed: ${error.message}`);
        }
    }

    /**
     * Generate AI insights from analysis
     */
    async generateAIInsights(analysis) {
        const insights = [];

        // Sentiment insights
        if (analysis.sentiment) {
            const sentiment = analysis.sentiment.sentiment;
            if (sentiment.score > 0.5) {
                insights.push('Positive sentiment detected');
            } else if (sentiment.score < -0.5) {
                insights.push('Negative sentiment detected');
            } else {
                insights.push('Neutral sentiment detected');
            }
        }

        // Entity insights
        if (analysis.entities) {
            const entities = analysis.entities.entities;
            if (entities.people.length > 0) {
                insights.push(`Mentions ${entities.people.length} people`);
            }
            if (entities.organizations.length > 0) {
                insights.push(`Mentions ${entities.organizations.length} organizations`);
            }
        }

        // Key phrase insights
        if (analysis.keyPhrases) {
            insights.push(`Key topics: ${analysis.keyPhrases.keyPhrases.slice(0, 3).map(p => p.term).join(', ')}`);
        }

        return insights;
    }

    /**
     * Get AI/ML capabilities
     */
    getCapabilities() {
        return Array.from(this.capabilities.values());
    }

    /**
     * Get active models
     */
    getActiveModels() {
        return Array.from(this.activeModels.entries()).map(([name, info]) => ({
            name: name,
            modelId: info.modelId,
            versionId: info.versionId,
            type: info.type,
            created: info.created,
            performance: info.performance
        }));
    }

    /**
     * Get performance metrics
     */
    getPerformanceMetrics() {
        return Object.fromEntries(this.performanceMetrics);
    }

    /**
     * Get core information
     */
    getInfo() {
        return {
            initialized: this.isInitialized,
            capabilities: this.capabilities.size,
            activeModels: this.activeModels.size,
            tensorflow: this.tensorflow.getSystemInfo(),
            nlp: this.nlp.getInfo(),
            dataAnalysis: this.dataAnalysis.getInfo(),
            modelManager: this.modelManager.getInfo()
        };
    }

    /**
     * Clean up resources
     */
    async cleanup() {
        await this.tensorflow.cleanup();
        this.nlp.cleanup();
        await this.dataAnalysis.cleanup();
        await this.modelManager.cleanup();
        
        this.capabilities.clear();
        this.activeModels.clear();
        this.performanceMetrics.clear();
        
        console.log('✅ AI/ML Core resources cleaned up');
    }
}

// Export the class
module.exports = AICore;

// Create a singleton instance
const aiCore = new AICore();

// Export the singleton instance
module.exports.instance = aiCore; 