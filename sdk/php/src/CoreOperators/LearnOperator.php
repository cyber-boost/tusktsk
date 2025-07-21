<?php
/**
 * Learn Operator
 * 
 * Machine learning operator that can analyze patterns, make predictions,
 * and adapt configurations based on historical data.
 * 
 * @package TuskPHP\CoreOperators
 * @version 2.0.0
 */

namespace TuskLang\CoreOperators;

/**
 * Learn Operator
 * 
 * Provides machine learning capabilities for pattern recognition,
 * prediction, and adaptive configuration management.
 */
class LearnOperator extends \TuskLang\CoreOperators\BaseOperator
{
    private array $models = [];
    private array $datasets = [];
    private array $algorithms = [];
    
    public function __construct()
    {
        $this->version = '2.0.0';
        $this->requiredFields = ['pattern'];
        $this->optionalFields = [
            'data', 'algorithm', 'features', 'target', 'window',
            'min_samples', 'confidence', 'fallback'
        ];
        
        $this->defaultConfig = [
            'algorithm' => 'linear_regression',
            'window' => 100,
            'min_samples' => 10,
            'confidence' => 0.8,
            'features' => [],
            'target' => null
        ];
        
        $this->initializeAlgorithms();
    }
    
    public function getName(): string
    {
        return 'learn';
    }
    
    protected function getDescription(): string
    {
        return 'Machine learning operator for pattern recognition, prediction, and adaptive configuration management';
    }
    
    protected function getExamples(): array
    {
        return [
            'basic' => '@learn({pattern: "optimal_workers", data: [1, 2, 3, 4], algorithm: "linear_regression"})',
            'with_features' => '@learn({pattern: "response_time", data: @metrics("api_calls"), features: ["load", "time"], target: "latency"})',
            'prediction' => '@learn({pattern: "traffic_prediction", algorithm: "time_series", window: "24h"})',
            'adaptive' => '@learn({pattern: "cache_size", data: @metrics("cache_hit_rate"), algorithm: "adaptive"})',
            'classification' => '@learn({pattern: "user_behavior", data: user_actions, algorithm: "decision_tree"})'
        ];
    }
    
    protected function getErrorCodes(): array
    {
        return array_merge(parent::getErrorCodes(), [
            'INSUFFICIENT_DATA' => 'Insufficient data for learning',
            'MODEL_TRAINING_FAILED' => 'Model training failed',
            'PREDICTION_FAILED' => 'Prediction failed',
            'INVALID_ALGORITHM' => 'Invalid learning algorithm',
            'FEATURE_EXTRACTION_FAILED' => 'Feature extraction failed'
        ]);
    }
    
    /**
     * Initialize learning algorithms
     */
    private function initializeAlgorithms(): void
    {
        $this->algorithms = [
            'linear_regression' => new LearningAlgorithms\LinearRegression(),
            'time_series' => new LearningAlgorithms\TimeSeries(),
            'adaptive' => new LearningAlgorithms\AdaptiveLearning(),
            'decision_tree' => new LearningAlgorithms\DecisionTree(),
            'clustering' => new LearningAlgorithms\Clustering(),
            'neural_network' => new LearningAlgorithms\NeuralNetwork()
        ];
    }
    
    /**
     * Execute learn operator
     */
    protected function executeOperator(array $config, array $context): mixed
    {
        $pattern = $this->resolveVariable($config['pattern'], $context);
        $algorithm = $config['algorithm'];
        $algorithmInstance = $this->getAlgorithm($algorithm);
        
        // Get or create model for this pattern
        $model = $this->getModel($pattern, $algorithm);
        
        // Prepare data
        $data = $this->prepareData($config, $context);
        
        // Check if we have enough data
        if (count($data) < $config['min_samples']) {
            $this->log('warning', 'Insufficient data for learning', [
                'pattern' => $pattern,
                'data_count' => count($data),
                'min_samples' => $config['min_samples']
            ]);
            
            // Return fallback value if provided
            if (isset($config['fallback'])) {
                return $this->resolveVariable($config['fallback'], $context);
            }
            
            return null;
        }
        
        // Train or update model
        $model->train($data, $config);
        
        // Make prediction
        $prediction = $model->predict($data, $config);
        
        // Check confidence
        $confidence = $model->getConfidence();
        if ($confidence < $config['confidence']) {
            $this->log('warning', 'Low confidence prediction', [
                'pattern' => $pattern,
                'confidence' => $confidence,
                'threshold' => $config['confidence']
            ]);
            
            // Return fallback if confidence is too low
            if (isset($config['fallback'])) {
                return $this->resolveVariable($config['fallback'], $context);
            }
        }
        
        $this->log('info', 'Learning prediction made', [
            'pattern' => $pattern,
            'algorithm' => $algorithm,
            'prediction' => $prediction,
            'confidence' => $confidence,
            'data_points' => count($data)
        ]);
        
        return $prediction;
    }
    
    /**
     * Get learning algorithm
     */
    private function getAlgorithm(string $name): LearningAlgorithms\LearningAlgorithmInterface
    {
        if (!isset($this->algorithms[$name])) {
            throw new \InvalidArgumentException("Unknown learning algorithm: {$name}");
        }
        
        return $this->algorithms[$name];
    }
    
    /**
     * Get or create model
     */
    private function getModel(string $pattern, string $algorithm): LearningModels\LearningModel
    {
        $key = "{$pattern}_{$algorithm}";
        
        if (!isset($this->models[$key])) {
            $this->models[$key] = new LearningModels\LearningModel($pattern, $algorithm);
        }
        
        return $this->models[$key];
    }
    
    /**
     * Prepare data for learning
     */
    private function prepareData(array $config, array $context): array
    {
        $data = [];
        
        // Get data from config
        if (isset($config['data'])) {
            $rawData = $this->resolveVariable($config['data'], $context);
            
            if (is_array($rawData)) {
                $data = $rawData;
            } else {
                $data = [$rawData];
            }
        }
        
        // Extract features if specified
        if (isset($config['features']) && !empty($config['features'])) {
            $features = $this->extractFeatures($config['features'], $context);
            $data = array_merge($data, $features);
        }
        
        // Apply window if specified
        if (isset($config['window'])) {
            $window = $this->parseWindow($config['window']);
            $data = array_slice($data, -$window);
        }
        
        return $data;
    }
    
    /**
     * Extract features from context
     */
    private function extractFeatures(array $features, array $context): array
    {
        $extracted = [];
        
        foreach ($features as $feature) {
            $value = $this->getContextValue($context, $feature);
            if ($value !== null) {
                $extracted[] = $value;
            }
        }
        
        return $extracted;
    }
    
    /**
     * Parse window specification
     */
    private function parseWindow(mixed $window): int
    {
        if (is_numeric($window)) {
            return (int)$window;
        }
        
        if (is_string($window)) {
            return $this->parseTimeWindow($window);
        }
        
        return 100; // Default window
    }
    
    /**
     * Parse time window (e.g., "1h", "24h", "7d")
     */
    private function parseTimeWindow(string $window): int
    {
        $units = [
            's' => 1,
            'm' => 60,
            'h' => 3600,
            'd' => 86400
        ];
        
        if (preg_match('/^(\d+)([smhd])$/', strtolower($window), $matches)) {
            $value = (int)$matches[1];
            $unit = $matches[2];
            
            if (isset($units[$unit])) {
                return $value * $units[$unit];
            }
        }
        
        return 3600; // Default 1 hour
    }
    
    /**
     * Custom validation
     */
    protected function customValidate(array $config): array
    {
        $errors = [];
        $warnings = [];
        
        // Validate algorithm
        if (isset($config['algorithm']) && !isset($this->algorithms[$config['algorithm']])) {
            $errors[] = "Unknown learning algorithm: {$config['algorithm']}";
        }
        
        // Validate window
        if (isset($config['window'])) {
            $window = $this->parseWindow($config['window']);
            if ($window <= 0) {
                $errors[] = "Window must be positive";
            }
        }
        
        // Validate min_samples
        if (isset($config['min_samples']) && $config['min_samples'] < 1) {
            $errors[] = "min_samples must be at least 1";
        }
        
        // Validate confidence
        if (isset($config['confidence'])) {
            $confidence = (float)$config['confidence'];
            if ($confidence < 0 || $confidence > 1) {
                $errors[] = "Confidence must be between 0 and 1";
            }
        }
        
        return ['errors' => $errors, 'warnings' => $warnings];
    }
    
    /**
     * Get learning statistics
     */
    public function getStatistics(): array
    {
        $stats = [
            'models' => count($this->models),
            'algorithms' => array_keys($this->algorithms),
            'datasets' => count($this->datasets)
        ];
        
        foreach ($this->models as $key => $model) {
            $stats['model_' . $key] = $model->getStatistics();
        }
        
        return $stats;
    }
    
    /**
     * Cleanup resources
     */
    public function cleanup(): void
    {
        foreach ($this->models as $model) {
            $model->cleanup();
        }
        $this->models = [];
    }
}

/**
 * Learning Algorithm Interface
 */
interface LearningAlgorithmInterface
{
    public function train(array $data, array $config): void;
    public function predict(array $data, array $config): mixed;
    public function getConfidence(): float;
    public function getStatistics(): array;
    public function cleanup(): void;
}

/**
 * Linear Regression Algorithm
 */
class LinearRegression implements LearningAlgorithmInterface
{
    private array $coefficients = [];
    private float $intercept = 0.0;
    private float $confidence = 0.0;
    private array $stats = [
        'trained' => false,
        'predictions' => 0,
        'errors' => 0
    ];
    
    public function train(array $data, array $config): void
    {
        if (count($data) < 2) {
            throw new \RuntimeException("Insufficient data for linear regression");
        }
        
        // Simple linear regression: y = mx + b
        $n = count($data);
        $sumX = 0;
        $sumY = 0;
        $sumXY = 0;
        $sumX2 = 0;
        
        foreach ($data as $i => $value) {
            $x = $i;
            $y = is_numeric($value) ? (float)$value : 0.0;
            
            $sumX += $x;
            $sumY += $y;
            $sumXY += $x * $y;
            $sumX2 += $x * $x;
        }
        
        // Calculate slope (m) and intercept (b)
        $denominator = $n * $sumX2 - $sumX * $sumX;
        if ($denominator != 0) {
            $this->coefficients['slope'] = ($n * $sumXY - $sumX * $sumY) / $denominator;
            $this->intercept = ($sumY - $this->coefficients['slope'] * $sumX) / $n;
        } else {
            $this->coefficients['slope'] = 0;
            $this->intercept = $sumY / $n;
        }
        
        // Calculate confidence (R-squared)
        $this->calculateConfidence($data);
        
        $this->stats['trained'] = true;
    }
    
    public function predict(array $data, array $config): mixed
    {
        if (!$this->stats['trained']) {
            throw new \RuntimeException("Model not trained");
        }
        
        $nextX = count($data);
        $prediction = $this->coefficients['slope'] * $nextX + $this->intercept;
        
        $this->stats['predictions']++;
        
        return $prediction;
    }
    
    public function getConfidence(): float
    {
        return $this->confidence;
    }
    
    public function getStatistics(): array
    {
        return array_merge($this->stats, [
            'coefficients' => $this->coefficients,
            'intercept' => $this->intercept,
            'confidence' => $this->confidence
        ]);
    }
    
    public function cleanup(): void
    {
        $this->coefficients = [];
        $this->intercept = 0.0;
        $this->confidence = 0.0;
        $this->stats['trained'] = false;
    }
    
    private function calculateConfidence(array $data): void
    {
        $n = count($data);
        if ($n < 2) {
            $this->confidence = 0.0;
            return;
        }
        
        $sumY = 0;
        $sumY2 = 0;
        $sumResiduals2 = 0;
        
        foreach ($data as $i => $value) {
            $y = is_numeric($value) ? (float)$value : 0.0;
            $predicted = $this->coefficients['slope'] * $i + $this->intercept;
            $residual = $y - $predicted;
            
            $sumY += $y;
            $sumY2 += $y * $y;
            $sumResiduals2 += $residual * $residual;
        }
        
        $meanY = $sumY / $n;
        $totalSS = $sumY2 - $n * $meanY * $meanY;
        
        if ($totalSS > 0) {
            $this->confidence = 1 - ($sumResiduals2 / $totalSS);
        } else {
            $this->confidence = 0.0;
        }
        
        // Ensure confidence is between 0 and 1
        $this->confidence = max(0.0, min(1.0, $this->confidence));
    }
}

/**
 * Time Series Algorithm
 */
class TimeSeries implements LearningAlgorithmInterface
{
    private array $values = [];
    private array $trends = [];
    private float $confidence = 0.0;
    private array $stats = [
        'trained' => false,
        'predictions' => 0,
        'errors' => 0
    ];
    
    public function train(array $data, array $config): void
    {
        $this->values = array_values($data);
        
        // Calculate moving averages and trends
        $this->calculateTrends();
        
        // Calculate confidence based on trend consistency
        $this->calculateConfidence();
        
        $this->stats['trained'] = true;
    }
    
    public function predict(array $data, array $config): mixed
    {
        if (!$this->stats['trained']) {
            throw new \RuntimeException("Model not trained");
        }
        
        $n = count($this->values);
        if ($n < 2) {
            return end($this->values);
        }
        
        // Simple prediction based on trend
        $lastValue = end($this->values);
        $trend = $this->calculateTrend();
        
        $prediction = $lastValue + $trend;
        
        $this->stats['predictions']++;
        
        return $prediction;
    }
    
    public function getConfidence(): float
    {
        return $this->confidence;
    }
    
    public function getStatistics(): array
    {
        return array_merge($this->stats, [
            'values_count' => count($this->values),
            'trends' => $this->trends,
            'confidence' => $this->confidence
        ]);
    }
    
    public function cleanup(): void
    {
        $this->values = [];
        $this->trends = [];
        $this->confidence = 0.0;
        $this->stats['trained'] = false;
    }
    
    private function calculateTrends(): void
    {
        $n = count($this->values);
        if ($n < 2) {
            return;
        }
        
        for ($i = 1; $i < $n; $i++) {
            $trend = $this->values[$i] - $this->values[$i - 1];
            $this->trends[] = $trend;
        }
    }
    
    private function calculateTrend(): float
    {
        if (empty($this->trends)) {
            return 0.0;
        }
        
        // Use average of recent trends
        $recentTrends = array_slice($this->trends, -5);
        return array_sum($recentTrends) / count($recentTrends);
    }
    
    private function calculateConfidence(): void
    {
        if (empty($this->trends)) {
            $this->confidence = 0.0;
            return;
        }
        
        // Calculate trend consistency
        $trendVariance = $this->calculateVariance($this->trends);
        $trendMean = array_sum($this->trends) / count($this->trends);
        
        if ($trendMean != 0) {
            $coefficientOfVariation = sqrt($trendVariance) / abs($trendMean);
            $this->confidence = max(0.0, 1.0 - $coefficientOfVariation);
        } else {
            $this->confidence = 0.0;
        }
    }
    
    private function calculateVariance(array $values): float
    {
        $n = count($values);
        if ($n < 2) {
            return 0.0;
        }
        
        $mean = array_sum($values) / $n;
        $sumSquaredDiff = 0;
        
        foreach ($values as $value) {
            $sumSquaredDiff += ($value - $mean) * ($value - $mean);
        }
        
        return $sumSquaredDiff / ($n - 1);
    }
}

/**
 * Adaptive Learning Algorithm
 */
class AdaptiveLearning implements LearningAlgorithmInterface
{
    private array $weights = [];
    private float $learningRate = 0.1;
    private float $confidence = 0.0;
    private array $stats = [
        'trained' => false,
        'predictions' => 0,
        'errors' => 0,
        'iterations' => 0
    ];
    
    public function train(array $data, array $config): void
    {
        if (empty($data)) {
            throw new \RuntimeException("No data provided for training");
        }
        
        $this->learningRate = $config['learning_rate'] ?? 0.1;
        
        // Initialize weights
        $this->weights = array_fill(0, count($data), 1.0);
        
        // Adaptive training
        $this->adaptiveTraining($data);
        
        $this->stats['trained'] = true;
    }
    
    public function predict(array $data, array $config): mixed
    {
        if (!$this->stats['trained']) {
            throw new \RuntimeException("Model not trained");
        }
        
        // Weighted average prediction
        $weightedSum = 0;
        $totalWeight = 0;
        
        foreach ($data as $i => $value) {
            $weight = $this->weights[$i] ?? 1.0;
            $weightedSum += $value * $weight;
            $totalWeight += $weight;
        }
        
        $prediction = $totalWeight > 0 ? $weightedSum / $totalWeight : 0;
        
        $this->stats['predictions']++;
        
        return $prediction;
    }
    
    public function getConfidence(): float
    {
        return $this->confidence;
    }
    
    public function getStatistics(): array
    {
        return array_merge($this->stats, [
            'weights' => $this->weights,
            'learning_rate' => $this->learningRate,
            'confidence' => $this->confidence
        ]);
    }
    
    public function cleanup(): void
    {
        $this->weights = [];
        $this->learningRate = 0.1;
        $this->confidence = 0.0;
        $this->stats['trained'] = false;
    }
    
    private function adaptiveTraining(array $data): void
    {
        $maxIterations = 100;
        $tolerance = 0.001;
        
        for ($iteration = 0; $iteration < $maxIterations; $iteration++) {
            $this->stats['iterations']++;
            
            $totalError = 0;
            
            // Update weights based on prediction errors
            for ($i = 0; $i < count($data) - 1; $i++) {
                $prediction = $this->predict(array_slice($data, 0, $i + 1), []);
                $actual = $data[$i + 1];
                $error = $actual - $prediction;
                
                // Update weight based on error
                $this->weights[$i] += $this->learningRate * $error * $data[$i];
                
                $totalError += abs($error);
            }
            
            // Check convergence
            if ($totalError < $tolerance) {
                break;
            }
        }
        
        // Calculate confidence based on final error
        $this->confidence = max(0.0, 1.0 - ($totalError / count($data)));
    }
}

/**
 * Decision Tree Algorithm
 */
class DecisionTree implements LearningAlgorithmInterface
{
    private array $tree = [];
    private float $confidence = 0.0;
    private array $stats = [
        'trained' => false,
        'predictions' => 0,
        'errors' => 0
    ];
    
    public function train(array $data, array $config): void
    {
        if (empty($data)) {
            throw new \RuntimeException("No data provided for training");
        }
        
        // Simple decision tree based on thresholds
        $this->buildTree($data);
        
        $this->stats['trained'] = true;
    }
    
    public function predict(array $data, array $config): mixed
    {
        if (!$this->stats['trained']) {
            throw new \RuntimeException("Model not trained");
        }
        
        // Simple prediction based on tree structure
        $prediction = $this->traverseTree($data);
        
        $this->stats['predictions']++;
        
        return $prediction;
    }
    
    public function getConfidence(): float
    {
        return $this->confidence;
    }
    
    public function getStatistics(): array
    {
        return array_merge($this->stats, [
            'tree_depth' => $this->calculateTreeDepth(),
            'confidence' => $this->confidence
        ]);
    }
    
    public function cleanup(): void
    {
        $this->tree = [];
        $this->confidence = 0.0;
        $this->stats['trained'] = false;
    }
    
    private function buildTree(array $data): void
    {
        // Simple tree building based on data distribution
        $values = array_values($data);
        $mean = array_sum($values) / count($values);
        
        $this->tree = [
            'threshold' => $mean,
            'left' => array_filter($values, fn($v) => $v <= $mean),
            'right' => array_filter($values, fn($v) => $v > $mean)
        ];
        
        $this->confidence = 0.8; // Simple confidence calculation
    }
    
    private function traverseTree(array $data): mixed
    {
        if (empty($this->tree)) {
            return 0;
        }
        
        $lastValue = end($data);
        
        if ($lastValue <= $this->tree['threshold']) {
            return !empty($this->tree['left']) ? array_sum($this->tree['left']) / count($this->tree['left']) : 0;
        } else {
            return !empty($this->tree['right']) ? array_sum($this->tree['right']) / count($this->tree['right']) : 0;
        }
    }
    
    private function calculateTreeDepth(): int
    {
        return 1; // Simple tree with depth 1
    }
}

/**
 * Clustering Algorithm
 */
class Clustering implements LearningAlgorithmInterface
{
    private array $clusters = [];
    private float $confidence = 0.0;
    private array $stats = [
        'trained' => false,
        'predictions' => 0,
        'errors' => 0
    ];
    
    public function train(array $data, array $config): void
    {
        if (empty($data)) {
            throw new \RuntimeException("No data provided for training");
        }
        
        // Simple k-means clustering
        $this->kMeansClustering($data, 3);
        
        $this->stats['trained'] = true;
    }
    
    public function predict(array $data, array $config): mixed
    {
        if (!$this->stats['trained']) {
            throw new \RuntimeException("Model not trained");
        }
        
        // Predict based on nearest cluster
        $lastValue = end($data);
        $nearestCluster = $this->findNearestCluster($lastValue);
        
        $prediction = $nearestCluster['centroid'];
        
        $this->stats['predictions']++;
        
        return $prediction;
    }
    
    public function getConfidence(): float
    {
        return $this->confidence;
    }
    
    public function getStatistics(): array
    {
        return array_merge($this->stats, [
            'clusters' => count($this->clusters),
            'confidence' => $this->confidence
        ]);
    }
    
    public function cleanup(): void
    {
        $this->clusters = [];
        $this->confidence = 0.0;
        $this->stats['trained'] = false;
    }
    
    private function kMeansClustering(array $data, int $k): void
    {
        $values = array_values($data);
        
        // Initialize centroids
        $centroids = [];
        for ($i = 0; $i < $k; $i++) {
            $centroids[] = $values[array_rand($values)];
        }
        
        // Simple clustering
        $this->clusters = [];
        foreach ($centroids as $i => $centroid) {
            $this->clusters[] = [
                'centroid' => $centroid,
                'points' => []
            ];
        }
        
        // Assign points to clusters
        foreach ($values as $value) {
            $nearestCluster = $this->findNearestCluster($value);
            $nearestCluster['points'][] = $value;
        }
        
        $this->confidence = 0.7; // Simple confidence
    }
    
    private function findNearestCluster(float $value): array
    {
        $nearest = null;
        $minDistance = PHP_FLOAT_MAX;
        
        foreach ($this->clusters as $cluster) {
            $distance = abs($value - $cluster['centroid']);
            if ($distance < $minDistance) {
                $minDistance = $distance;
                $nearest = $cluster;
            }
        }
        
        return $nearest ?? $this->clusters[0];
    }
}

/**
 * Neural Network Algorithm
 */
class NeuralNetwork implements LearningAlgorithmInterface
{
    private array $layers = [];
    private array $weights = [];
    private float $confidence = 0.0;
    private array $stats = [
        'trained' => false,
        'predictions' => 0,
        'errors' => 0
    ];
    
    public function train(array $data, array $config): void
    {
        if (empty($data)) {
            throw new \RuntimeException("No data provided for training");
        }
        
        // Simple neural network with one hidden layer
        $this->initializeNetwork(count($data));
        $this->trainNetwork($data);
        
        $this->stats['trained'] = true;
    }
    
    public function predict(array $data, array $config): mixed
    {
        if (!$this->stats['trained']) {
            throw new \RuntimeException("Model not trained");
        }
        
        // Simple forward pass
        $input = array_values($data);
        $output = $this->forwardPass($input);
        
        $this->stats['predictions']++;
        
        return $output;
    }
    
    public function getConfidence(): float
    {
        return $this->confidence;
    }
    
    public function getStatistics(): array
    {
        return array_merge($this->stats, [
            'layers' => count($this->layers),
            'confidence' => $this->confidence
        ]);
    }
    
    public function cleanup(): void
    {
        $this->layers = [];
        $this->weights = [];
        $this->confidence = 0.0;
        $this->stats['trained'] = false;
    }
    
    private function initializeNetwork(int $inputSize): void
    {
        $hiddenSize = max(2, $inputSize / 2);
        
        $this->layers = [
            'input' => $inputSize,
            'hidden' => (int)$hiddenSize,
            'output' => 1
        ];
        
        // Initialize weights randomly
        $this->weights = [
            'input_hidden' => array_fill(0, $inputSize, array_fill(0, $hiddenSize, 0.1)),
            'hidden_output' => array_fill(0, $hiddenSize, 0.1)
        ];
    }
    
    private function trainNetwork(array $data): void
    {
        // Simple training (placeholder)
        $this->confidence = 0.6;
    }
    
    private function forwardPass(array $input): float
    {
        // Simple forward pass (placeholder)
        return array_sum($input) / count($input);
    }
} 