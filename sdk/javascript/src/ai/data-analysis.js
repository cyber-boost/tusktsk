/**
 * Data Analysis and Visualization Engine for TuskTsk
 * Provides comprehensive data analysis capabilities with statistical analysis and visualization
 * 
 * @author AI-Cloud-Native-Engineer
 * @version 2.0.3
 * @license BBL
 */

const tf = require('@tensorflow/tfjs-node');
const ss = require('simple-statistics');
const Chart = require('chart.js/auto');
const { createCanvas } = require('canvas');
const fs = require('fs').promises;
const path = require('path');

class DataAnalysisEngine {
    constructor() {
        this.datasets = new Map();
        this.analyses = new Map();
        this.charts = new Map();
        this.isInitialized = false;
        this.supportedFormats = ['json', 'csv', 'tsv', 'xlsx'];
        this.chartTypes = ['line', 'bar', 'scatter', 'pie', 'doughnut', 'radar', 'polarArea'];
    }

    /**
     * Initialize data analysis engine
     */
    async initialize() {
        try {
            // Initialize TensorFlow.js for numerical computations
            await tf.ready();
            
            // Create output directory for charts
            await this.ensureDirectoryExists('./charts');
            
            this.isInitialized = true;
            console.log('✅ Data Analysis Engine initialized successfully');
            
            return {
                success: true,
                supportedFormats: this.supportedFormats,
                chartTypes: this.chartTypes,
                tfVersion: tf.version.tfjs
            };
        } catch (error) {
            console.error('❌ Data Analysis Engine initialization failed:', error);
            throw new Error(`Data Analysis Engine initialization failed: ${error.message}`);
        }
    }

    /**
     * Load dataset from various formats
     */
    async loadDataset(name, data, format = 'json') {
        if (!this.isInitialized) {
            throw new Error('Data Analysis Engine not initialized. Call initialize() first.');
        }

        try {
            let dataset = null;
            
            switch (format.toLowerCase()) {
                case 'json':
                    dataset = typeof data === 'string' ? JSON.parse(data) : data;
                    break;
                case 'csv':
                    dataset = this.parseCSV(data);
                    break;
                case 'tsv':
                    dataset = this.parseTSV(data);
                    break;
                default:
                    throw new Error(`Format '${format}' not supported`);
            }
            
            // Store dataset
            this.datasets.set(name, {
                data: dataset,
                format: format,
                loaded: new Date(),
                size: Array.isArray(dataset) ? dataset.length : Object.keys(dataset).length
            });
            
            console.log(`✅ Dataset '${name}' loaded successfully`);
            return {
                success: true,
                name: name,
                size: this.datasets.get(name).size,
                format: format
            };
        } catch (error) {
            console.error(`❌ Dataset loading failed: ${error.message}`);
            throw new Error(`Dataset loading failed: ${error.message}`);
        }
    }

    /**
     * Parse CSV data
     */
    parseCSV(csvText) {
        const lines = csvText.trim().split('\n');
        const headers = lines[0].split(',').map(h => h.trim());
        const data = [];
        
        for (let i = 1; i < lines.length; i++) {
            const values = lines[i].split(',').map(v => v.trim());
            const row = {};
            headers.forEach((header, index) => {
                row[header] = values[index] || '';
            });
            data.push(row);
        }
        
        return data;
    }

    /**
     * Parse TSV data
     */
    parseTSV(tsvText) {
        const lines = tsvText.trim().split('\n');
        const headers = lines[0].split('\t').map(h => h.trim());
        const data = [];
        
        for (let i = 1; i < lines.length; i++) {
            const values = lines[i].split('\t').map(v => v.trim());
            const row = {};
            headers.forEach((header, index) => {
                row[header] = values[index] || '';
            });
            data.push(row);
        }
        
        return data;
    }

    /**
     * Perform statistical analysis on dataset
     */
    analyzeDataset(datasetName, columns = null) {
        if (!this.datasets.has(datasetName)) {
            throw new Error(`Dataset '${datasetName}' not found`);
        }

        try {
            const dataset = this.datasets.get(datasetName).data;
            const analysis = {
                dataset: datasetName,
                timestamp: new Date(),
                summary: {},
                statistics: {}
            };

            // Determine columns to analyze
            const cols = columns || Object.keys(dataset[0] || {});
            
            cols.forEach(column => {
                const values = dataset.map(row => {
                    const val = row[column];
                    return typeof val === 'number' ? val : parseFloat(val);
                }).filter(val => !isNaN(val));
                
                if (values.length > 0) {
                    analysis.statistics[column] = {
                        count: values.length,
                        mean: ss.mean(values),
                        median: ss.median(values),
                        mode: ss.mode(values),
                        standardDeviation: ss.standardDeviation(values),
                        variance: ss.variance(values),
                        min: ss.min(values),
                        max: ss.max(values),
                        range: ss.max(values) - ss.min(values),
                        quartiles: ss.quantile(values, [0.25, 0.5, 0.75]),
                        skewness: ss.skewness(values),
                        kurtosis: ss.kurtosis(values)
                    };
                }
            });

            // Overall dataset summary
            analysis.summary = {
                totalRows: dataset.length,
                totalColumns: cols.length,
                numericColumns: Object.keys(analysis.statistics).length,
                missingValues: this.countMissingValues(dataset, cols)
            };

            // Store analysis
            this.analyses.set(datasetName, analysis);
            
            return {
                success: true,
                analysis: analysis
            };
        } catch (error) {
            console.error(`❌ Dataset analysis failed: ${error.message}`);
            throw new Error(`Dataset analysis failed: ${error.message}`);
        }
    }

    /**
     * Count missing values in dataset
     */
    countMissingValues(dataset, columns) {
        const missing = {};
        columns.forEach(col => {
            missing[col] = dataset.filter(row => 
                row[col] === null || row[col] === undefined || row[col] === ''
            ).length;
        });
        return missing;
    }

    /**
     * Create correlation matrix
     */
    createCorrelationMatrix(datasetName, columns = null) {
        if (!this.datasets.has(datasetName)) {
            throw new Error(`Dataset '${datasetName}' not found`);
        }

        try {
            const dataset = this.datasets.get(datasetName).data;
            const cols = columns || Object.keys(dataset[0] || {});
            const numericCols = cols.filter(col => {
                const values = dataset.map(row => parseFloat(row[col]));
                return values.some(val => !isNaN(val));
            });

            const correlationMatrix = {};
            
            numericCols.forEach(col1 => {
                correlationMatrix[col1] = {};
                numericCols.forEach(col2 => {
                    const values1 = dataset.map(row => parseFloat(row[col1])).filter(val => !isNaN(val));
                    const values2 = dataset.map(row => parseFloat(row[col2])).filter(val => !isNaN(val));
                    
                    if (values1.length === values2.length && values1.length > 1) {
                        correlationMatrix[col1][col2] = ss.correlation(values1, values2);
                    } else {
                        correlationMatrix[col1][col2] = null;
                    }
                });
            });

            return {
                success: true,
                dataset: datasetName,
                correlationMatrix: correlationMatrix,
                columns: numericCols
            };
        } catch (error) {
            console.error(`❌ Correlation matrix creation failed: ${error.message}`);
            throw new Error(`Correlation matrix creation failed: ${error.message}`);
        }
    }

    /**
     * Perform outlier detection
     */
    detectOutliers(datasetName, column, method = 'iqr') {
        if (!this.datasets.has(datasetName)) {
            throw new Error(`Dataset '${datasetName}' not found`);
        }

        try {
            const dataset = this.datasets.get(datasetName).data;
            const values = dataset.map(row => parseFloat(row[column])).filter(val => !isNaN(val));
            
            let outliers = [];
            
            switch (method.toLowerCase()) {
                case 'iqr':
                    outliers = this.detectOutliersIQR(values);
                    break;
                case 'zscore':
                    outliers = this.detectOutliersZScore(values);
                    break;
                case 'modified_zscore':
                    outliers = this.detectOutliersModifiedZScore(values);
                    break;
                default:
                    throw new Error(`Outlier detection method '${method}' not supported`);
            }

            return {
                success: true,
                dataset: datasetName,
                column: column,
                method: method,
                outliers: outliers,
                outlierCount: outliers.length,
                totalValues: values.length
            };
        } catch (error) {
            console.error(`❌ Outlier detection failed: ${error.message}`);
            throw new Error(`Outlier detection failed: ${error.message}`);
        }
    }

    /**
     * Detect outliers using IQR method
     */
    detectOutliersIQR(values) {
        const q1 = ss.quantile(values, 0.25);
        const q3 = ss.quantile(values, 0.75);
        const iqr = q3 - q1;
        const lowerBound = q1 - 1.5 * iqr;
        const upperBound = q3 + 1.5 * iqr;
        
        return values.filter(val => val < lowerBound || val > upperBound);
    }

    /**
     * Detect outliers using Z-score method
     */
    detectOutliersZScore(values, threshold = 3) {
        const mean = ss.mean(values);
        const std = ss.standardDeviation(values);
        
        return values.filter(val => Math.abs((val - mean) / std) > threshold);
    }

    /**
     * Detect outliers using modified Z-score method
     */
    detectOutliersModifiedZScore(values, threshold = 3.5) {
        const median = ss.median(values);
        const mad = ss.medianAbsoluteDeviation(values);
        
        return values.filter(val => Math.abs(0.6745 * (val - median) / mad) > threshold);
    }

    /**
     * Create chart from dataset
     */
    async createChart(datasetName, config) {
        if (!this.datasets.has(datasetName)) {
            throw new Error(`Dataset '${datasetName}' not found`);
        }

        const {
            type = 'line',
            xColumn,
            yColumn,
            title = 'Chart',
            width = 800,
            height = 600,
            outputPath = null
        } = config;

        try {
            const dataset = this.datasets.get(datasetName).data;
            
            // Prepare chart data
            const chartData = this.prepareChartData(dataset, xColumn, yColumn, type);
            
            // Create canvas
            const canvas = createCanvas(width, height);
            const ctx = canvas.getContext('2d');
            
            // Create chart
            const chart = new Chart(ctx, {
                type: type,
                data: chartData,
                options: {
                    responsive: false,
                    plugins: {
                        title: {
                            display: true,
                            text: title
                        }
                    }
                }
            });
            
            // Generate chart
            await chart.render();
            
            // Save chart
            const filename = outputPath || `./charts/${datasetName}_${type}_${Date.now()}.png`;
            const buffer = canvas.toBuffer('image/png');
            await fs.writeFile(filename, buffer);
            
            // Store chart info
            this.charts.set(filename, {
                dataset: datasetName,
                type: type,
                config: config,
                created: new Date()
            });
            
            console.log(`✅ Chart created: ${filename}`);
            return {
                success: true,
                chartPath: filename,
                type: type,
                dataset: datasetName
            };
        } catch (error) {
            console.error(`❌ Chart creation failed: ${error.message}`);
            throw new Error(`Chart creation failed: ${error.message}`);
        }
    }

    /**
     * Prepare chart data
     */
    prepareChartData(dataset, xColumn, yColumn, type) {
        const labels = dataset.map(row => row[xColumn]);
        const data = dataset.map(row => parseFloat(row[yColumn])).filter(val => !isNaN(val));
        
        switch (type.toLowerCase()) {
            case 'line':
            case 'bar':
                return {
                    labels: labels,
                    datasets: [{
                        label: yColumn,
                        data: data,
                        borderColor: 'rgb(75, 192, 192)',
                        backgroundColor: 'rgba(75, 192, 192, 0.2)'
                    }]
                };
            case 'scatter':
                return {
                    datasets: [{
                        label: yColumn,
                        data: labels.map((label, index) => ({
                            x: label,
                            y: data[index]
                        })),
                        backgroundColor: 'rgba(75, 192, 192, 0.6)'
                    }]
                };
            case 'pie':
            case 'doughnut':
                return {
                    labels: labels,
                    datasets: [{
                        data: data,
                        backgroundColor: this.generateColors(data.length)
                    }]
                };
            default:
                return {
                    labels: labels,
                    datasets: [{
                        label: yColumn,
                        data: data
                    }]
                };
        }
    }

    /**
     * Generate colors for charts
     */
    generateColors(count) {
        const colors = [
            'rgba(255, 99, 132, 0.6)',
            'rgba(54, 162, 235, 0.6)',
            'rgba(255, 206, 86, 0.6)',
            'rgba(75, 192, 192, 0.6)',
            'rgba(153, 102, 255, 0.6)',
            'rgba(255, 159, 64, 0.6)',
            'rgba(199, 199, 199, 0.6)',
            'rgba(83, 102, 255, 0.6)',
            'rgba(255, 99, 132, 0.6)',
            'rgba(54, 162, 235, 0.6)'
        ];
        
        const result = [];
        for (let i = 0; i < count; i++) {
            result.push(colors[i % colors.length]);
        }
        return result;
    }

    /**
     * Perform data clustering
     */
    async performClustering(datasetName, columns, k = 3, method = 'kmeans') {
        if (!this.datasets.has(datasetName)) {
            throw new Error(`Dataset '${datasetName}' not found`);
        }

        try {
            const dataset = this.datasets.get(datasetName).data;
            
            // Prepare data for clustering
            const data = dataset.map(row => 
                columns.map(col => parseFloat(row[col])).filter(val => !isNaN(val))
            ).filter(row => row.length === columns.length);
            
            if (data.length === 0) {
                throw new Error('No valid numeric data for clustering');
            }
            
            // Convert to TensorFlow tensor
            const tensor = tf.tensor2d(data);
            
            let clusters = null;
            
            switch (method.toLowerCase()) {
                case 'kmeans':
                    clusters = await this.kMeansClustering(tensor, k);
                    break;
                default:
                    throw new Error(`Clustering method '${method}' not supported`);
            }
            
            // Add cluster assignments to original data
            const clusteredData = dataset.map((row, index) => ({
                ...row,
                cluster: clusters[index] || -1
            }));
            
            return {
                success: true,
                dataset: datasetName,
                method: method,
                k: k,
                clusters: clusters,
                clusteredData: clusteredData,
                clusterSizes: this.getClusterSizes(clusters, k)
            };
        } catch (error) {
            console.error(`❌ Clustering failed: ${error.message}`);
            throw new Error(`Clustering failed: ${error.message}`);
        }
    }

    /**
     * Perform K-means clustering
     */
    async kMeansClustering(data, k, maxIterations = 100) {
        const numSamples = data.shape[0];
        const numFeatures = data.shape[1];
        
        // Initialize centroids randomly
        let centroids = tf.randomUniform([k, numFeatures]);
        
        for (let iteration = 0; iteration < maxIterations; iteration++) {
            // Assign points to nearest centroid
            const distances = tf.matMul(data, centroids.transpose());
            const assignments = tf.argMax(distances, 1);
            
            // Update centroids
            const newCentroids = tf.zeros([k, numFeatures]);
            
            for (let i = 0; i < k; i++) {
                const mask = tf.equal(assignments, tf.scalar(i));
                const clusterPoints = tf.booleanMask(data, mask);
                
                if (clusterPoints.shape[0] > 0) {
                    const centroid = tf.mean(clusterPoints, 0);
                    newCentroids.slice([i, 0], [1, numFeatures]).assign(centroid);
                }
            }
            
            // Check convergence
            const centroidDiff = tf.sum(tf.square(tf.sub(centroids, newCentroids)));
            if (centroidDiff.dataSync()[0] < 1e-6) {
                break;
            }
            
            centroids = newCentroids;
        }
        
        // Final assignment
        const distances = tf.matMul(data, centroids.transpose());
        const assignments = tf.argMax(distances, 1);
        
        return await assignments.array();
    }

    /**
     * Get cluster sizes
     */
    getClusterSizes(clusters, k) {
        const sizes = new Array(k).fill(0);
        clusters.forEach(cluster => {
            if (cluster >= 0 && cluster < k) {
                sizes[cluster]++;
            }
        });
        return sizes;
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
     * Get dataset information
     */
    getDatasetInfo(datasetName) {
        if (!this.datasets.has(datasetName)) {
            throw new Error(`Dataset '${datasetName}' not found`);
        }

        const info = this.datasets.get(datasetName);
        return {
            name: datasetName,
            size: info.size,
            format: info.format,
            loaded: info.loaded
        };
    }

    /**
     * List all datasets
     */
    listDatasets() {
        return Array.from(this.datasets.keys()).map(name => this.getDatasetInfo(name));
    }

    /**
     * Delete dataset
     */
    deleteDataset(datasetName) {
        if (!this.datasets.has(datasetName)) {
            throw new Error(`Dataset '${datasetName}' not found`);
        }

        this.datasets.delete(datasetName);
        this.analyses.delete(datasetName);
        
        console.log(`✅ Dataset '${datasetName}' deleted`);
        return {
            success: true,
            name: datasetName
        };
    }

    /**
     * Get engine information
     */
    getInfo() {
        return {
            initialized: this.isInitialized,
            datasets: this.datasets.size,
            analyses: this.analyses.size,
            charts: this.charts.size,
            supportedFormats: this.supportedFormats,
            chartTypes: this.chartTypes
        };
    }

    /**
     * Clean up resources
     */
    async cleanup() {
        this.datasets.clear();
        this.analyses.clear();
        this.charts.clear();
        
        console.log('✅ Data Analysis Engine resources cleaned up');
    }
}

// Export the class
module.exports = DataAnalysisEngine;

// Create a singleton instance
const dataAnalysisEngine = new DataAnalysisEngine();

// Export the singleton instance
module.exports.instance = dataAnalysisEngine; 