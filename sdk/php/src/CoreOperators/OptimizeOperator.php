<?php
/**
 * Optimize Operator
 * 
 * Automatic optimization operator that can tune configurations
 * based on performance metrics and learning algorithms.
 * 
 * @package TuskPHP\CoreOperators
 * @version 2.0.0
 */

namespace TuskLang\CoreOperators;

/**
 * Optimize Operator
 * 
 * Provides automatic configuration optimization based on
 * performance metrics, constraints, and learning algorithms.
 */
class OptimizeOperator extends \TuskLang\CoreOperators\BaseOperator
{
    private array $optimizers = [];
    private array $constraints = [];
    private array $history = [];
    
    public function __construct()
    {
        $this->version = '2.0.0';
        $this->requiredFields = ['parameter'];
        $this->optionalFields = [
            'current', 'target', 'constraints', 'algorithm', 'metrics',
            'step_size', 'max_iterations', 'tolerance', 'fallback'
        ];
        
        $this->defaultConfig = [
            'algorithm' => 'gradient_descent',
            'step_size' => 0.1,
            'max_iterations' => 100,
            'tolerance' => 0.001,
            'constraints' => []
        ];
        
        $this->initializeOptimizers();
    }
    
    public function getName(): string
    {
        return 'optimize';
    }
    
    protected function getDescription(): string
    {
        return 'Automatic configuration optimization based on performance metrics and learning algorithms';
    }
    
    protected function getExamples(): array
    {
        return [
            'basic' => '@optimize({parameter: "cache_size", current: 100, target: 0.9})',
            'with_constraints' => '@optimize({parameter: "workers", current: 4, target: "min_latency", constraints: {min: 1, max: 16}})',
            'adaptive' => '@optimize({parameter: "timeout", current: 30, algorithm: "adaptive", metrics: ["response_time", "error_rate"]})',
            'multi_objective' => '@optimize({parameter: "pool_size", current: 10, target: ["throughput", "memory_usage"], constraints: {memory_limit: "1GB"}})',
            'bayesian' => '@optimize({parameter: "batch_size", current: 100, algorithm: "bayesian", max_iterations: 50})'
        ];
    }
    
    protected function getErrorCodes(): array
    {
        return array_merge(parent::getErrorCodes(), [
            'OPTIMIZATION_FAILED' => 'Optimization algorithm failed to converge',
            'CONSTRAINT_VIOLATION' => 'Optimization violated constraints',
            'INSUFFICIENT_DATA' => 'Insufficient data for optimization',
            'INVALID_TARGET' => 'Invalid optimization target',
            'ALGORITHM_ERROR' => 'Optimization algorithm error'
        ]);
    }
    
    /**
     * Initialize optimization algorithms
     */
    private function initializeOptimizers(): void
    {
        $this->optimizers = [
            'gradient_descent' => new OptimizationAlgorithms\GradientDescent(),
            'adaptive' => new OptimizationAlgorithms\AdaptiveOptimization(),
            'bayesian' => new OptimizationAlgorithms\BayesianOptimization(),
            'genetic' => new OptimizationAlgorithms\GeneticAlgorithm(),
            'simulated_annealing' => new OptimizationAlgorithms\SimulatedAnnealing()
        ];
    }
    
    /**
     * Execute optimize operator
     */
    protected function executeOperator(array $config, array $context): mixed
    {
        $parameter = $this->resolveVariable($config['parameter'], $context);
        $algorithm = $config['algorithm'];
        $optimizer = $this->getOptimizer($algorithm);
        
        // Get current value
        $current = $this->getCurrentValue($config, $context);
        
        // Get target value
        $target = $this->getTargetValue($config, $context);
        
        // Get constraints
        $constraints = $this->resolveConstraints($config['constraints'] ?? [], $context);
        
        // Get performance metrics
        $metrics = $this->getPerformanceMetrics($config, $context);
        
        // Check if we have enough data
        if (empty($metrics)) {
            $this->log('warning', 'No performance metrics available for optimization', [
                'parameter' => $parameter
            ]);
            
            if (isset($config['fallback'])) {
                return $this->resolveVariable($config['fallback'], $context);
            }
            
            return $current;
        }
        
        // Create optimization problem
        $problem = [
            'parameter' => $parameter,
            'current' => $current,
            'target' => $target,
            'constraints' => $constraints,
            'metrics' => $metrics,
            'config' => $config
        ];
        
        // Run optimization
        try {
            $result = $optimizer->optimize($problem);
            
            // Validate result against constraints
            if (!$this->validateConstraints($result, $constraints)) {
                $this->log('warning', 'Optimization result violates constraints', [
                    'parameter' => $parameter,
                    'result' => $result,
                    'constraints' => $constraints
                ]);
                
                // Return constrained result
                $result = $this->applyConstraints($result, $constraints);
            }
            
            // Store optimization history
            $this->storeOptimizationHistory($parameter, $current, $result, $metrics);
            
            $this->log('info', 'Optimization completed', [
                'parameter' => $parameter,
                'current' => $current,
                'optimized' => $result,
                'algorithm' => $algorithm,
                'iterations' => $optimizer->getIterations()
            ]);
            
            return $result;
            
        } catch (\Exception $e) {
            $this->log('error', 'Optimization failed', [
                'parameter' => $parameter,
                'error' => $e->getMessage()
            ]);
            
            if (isset($config['fallback'])) {
                return $this->resolveVariable($config['fallback'], $context);
            }
            
            return $current;
        }
    }
    
    /**
     * Get optimization algorithm
     */
    private function getOptimizer(string $name): OptimizationAlgorithms\OptimizationAlgorithmInterface
    {
        if (!isset($this->optimizers[$name])) {
            throw new \InvalidArgumentException("Unknown optimization algorithm: {$name}");
        }
        
        return $this->optimizers[$name];
    }
    
    /**
     * Get current parameter value
     */
    private function getCurrentValue(array $config, array $context): mixed
    {
        if (isset($config['current'])) {
            return $this->resolveVariable($config['current'], $context);
        }
        
        // Try to get from context
        $parameter = $config['parameter'];
        return $this->getContextValue($context, $parameter, 0);
    }
    
    /**
     * Get target value
     */
    private function getTargetValue(array $config, array $context): mixed
    {
        if (isset($config['target'])) {
            return $this->resolveVariable($config['target'], $context);
        }
        
        // Default target based on parameter type
        $parameter = $config['parameter'];
        if (strpos($parameter, 'latency') !== false || strpos($parameter, 'timeout') !== false) {
            return 'minimize';
        } elseif (strpos($parameter, 'throughput') !== false || strpos($parameter, 'rate') !== false) {
            return 'maximize';
        } else {
            return 'optimize';
        }
    }
    
    /**
     * Resolve constraints
     */
    private function resolveConstraints(array $constraints, array $context): array
    {
        $resolved = [];
        
        foreach ($constraints as $key => $value) {
            $resolved[$key] = $this->resolveVariable($value, $context);
        }
        
        return $resolved;
    }
    
    /**
     * Get performance metrics
     */
    private function getPerformanceMetrics(array $config, array $context): array
    {
        $metrics = [];
        
        if (isset($config['metrics'])) {
            $metricNames = is_array($config['metrics']) ? $config['metrics'] : [$config['metrics']];
            
            foreach ($metricNames as $metricName) {
                $value = $this->getContextValue($context, $metricName);
                if ($value !== null) {
                    $metrics[$metricName] = $value;
                }
            }
        }
        
        return $metrics;
    }
    
    /**
     * Validate constraints
     */
    private function validateConstraints(mixed $value, array $constraints): bool
    {
        if (isset($constraints['min']) && $value < $constraints['min']) {
            return false;
        }
        
        if (isset($constraints['max']) && $value > $constraints['max']) {
            return false;
        }
        
        if (isset($constraints['range']) && ($value < $constraints['range'][0] || $value > $constraints['range'][1])) {
            return false;
        }
        
        return true;
    }
    
    /**
     * Apply constraints to value
     */
    private function applyConstraints(mixed $value, array $constraints): mixed
    {
        if (isset($constraints['min']) && $value < $constraints['min']) {
            $value = $constraints['min'];
        }
        
        if (isset($constraints['max']) && $value > $constraints['max']) {
            $value = $constraints['max'];
        }
        
        if (isset($constraints['range'])) {
            $value = max($constraints['range'][0], min($constraints['range'][1], $value));
        }
        
        return $value;
    }
    
    /**
     * Store optimization history
     */
    private function storeOptimizationHistory(string $parameter, mixed $current, mixed $optimized, array $metrics): void
    {
        $this->history[] = [
            'parameter' => $parameter,
            'current' => $current,
            'optimized' => $optimized,
            'metrics' => $metrics,
            'timestamp' => time()
        ];
        
        // Keep only last 1000 entries
        if (count($this->history) > 1000) {
            array_shift($this->history);
        }
    }
    
    /**
     * Custom validation
     */
    protected function customValidate(array $config): array
    {
        $errors = [];
        $warnings = [];
        
        // Validate algorithm
        if (isset($config['algorithm']) && !isset($this->optimizers[$config['algorithm']])) {
            $errors[] = "Unknown optimization algorithm: {$config['algorithm']}";
        }
        
        // Validate step size
        if (isset($config['step_size']) && $config['step_size'] <= 0) {
            $errors[] = "Step size must be positive";
        }
        
        // Validate max iterations
        if (isset($config['max_iterations']) && $config['max_iterations'] < 1) {
            $errors[] = "Max iterations must be at least 1";
        }
        
        // Validate tolerance
        if (isset($config['tolerance']) && $config['tolerance'] <= 0) {
            $errors[] = "Tolerance must be positive";
        }
        
        return ['errors' => $errors, 'warnings' => $warnings];
    }
    
    /**
     * Get optimization statistics
     */
    public function getStatistics(): array
    {
        return [
            'optimizers' => array_keys($this->optimizers),
            'history_count' => count($this->history),
            'recent_optimizations' => array_slice($this->history, -10)
        ];
    }
    
    /**
     * Cleanup resources
     */
    public function cleanup(): void
    {
        foreach ($this->optimizers as $optimizer) {
            $optimizer->cleanup();
        }
        $this->history = [];
    }
}

/**
 * Optimization Algorithm Interface
 */
interface OptimizationAlgorithmInterface
{
    public function optimize(array $problem): mixed;
    public function getIterations(): int;
    public function getStatistics(): array;
    public function cleanup(): void;
}

/**
 * Gradient Descent Optimizer
 */
class GradientDescent implements OptimizationAlgorithmInterface
{
    private int $iterations = 0;
    private array $stats = [
        'converged' => false,
        'final_error' => 0.0
    ];
    
    public function optimize(array $problem): mixed
    {
        $current = $problem['current'];
        $target = $problem['target'];
        $stepSize = $problem['config']['step_size'] ?? 0.1;
        $maxIterations = $problem['config']['max_iterations'] ?? 100;
        $tolerance = $problem['config']['tolerance'] ?? 0.001;
        
        $this->iterations = 0;
        
        for ($i = 0; $i < $maxIterations; $i++) {
            $this->iterations++;
            
            // Calculate gradient (simplified)
            $gradient = $this->calculateGradient($current, $target, $problem['metrics']);
            
            // Update parameter
            $newValue = $current - $stepSize * $gradient;
            
            // Check convergence
            if (abs($newValue - $current) < $tolerance) {
                $this->stats['converged'] = true;
                break;
            }
            
            $current = $newValue;
        }
        
        $this->stats['final_error'] = abs($current - $target);
        
        return $current;
    }
    
    public function getIterations(): int
    {
        return $this->iterations;
    }
    
    public function getStatistics(): array
    {
        return $this->stats;
    }
    
    public function cleanup(): void
    {
        $this->iterations = 0;
        $this->stats = ['converged' => false, 'final_error' => 0.0];
    }
    
    private function calculateGradient(mixed $current, mixed $target, array $metrics): float
    {
        // Simplified gradient calculation
        if (is_numeric($target)) {
            return $current - $target;
        }
        
        // For minimize/maximize targets, use metrics
        if ($target === 'minimize') {
            return 1.0; // Move towards smaller values
        } elseif ($target === 'maximize') {
            return -1.0; // Move towards larger values
        }
        
        return 0.0;
    }
}

/**
 * Adaptive Optimization
 */
class AdaptiveOptimization implements OptimizationAlgorithmInterface
{
    private int $iterations = 0;
    private array $stats = [
        'converged' => false,
        'adaptations' => 0
    ];
    
    public function optimize(array $problem): mixed
    {
        $current = $problem['current'];
        $target = $problem['target'];
        $maxIterations = $problem['config']['max_iterations'] ?? 100;
        
        $this->iterations = 0;
        $this->stats['adaptations'] = 0;
        
        for ($i = 0; $i < $maxIterations; $i++) {
            $this->iterations++;
            
            // Adaptive step size based on performance
            $stepSize = $this->calculateAdaptiveStepSize($current, $problem['metrics']);
            
            // Calculate direction
            $direction = $this->calculateDirection($current, $target, $problem['metrics']);
            
            // Update parameter
            $newValue = $current + $stepSize * $direction;
            
            // Check if improvement
            if ($this->isImprovement($newValue, $current, $target, $problem['metrics'])) {
                $current = $newValue;
                $this->stats['adaptations']++;
            } else {
                // Reduce step size
                $stepSize *= 0.5;
            }
            
            // Check convergence
            if ($stepSize < 0.001) {
                $this->stats['converged'] = true;
                break;
            }
        }
        
        return $current;
    }
    
    public function getIterations(): int
    {
        return $this->iterations;
    }
    
    public function getStatistics(): array
    {
        return $this->stats;
    }
    
    public function cleanup(): void
    {
        $this->iterations = 0;
        $this->stats = ['converged' => false, 'adaptations' => 0];
    }
    
    private function calculateAdaptiveStepSize(mixed $current, array $metrics): float
    {
        // Adaptive step size based on metric variance
        if (empty($metrics)) {
            return 0.1;
        }
        
        $values = array_values($metrics);
        $variance = $this->calculateVariance($values);
        
        return max(0.01, min(1.0, 1.0 / (1.0 + $variance)));
    }
    
    private function calculateDirection(mixed $current, mixed $target, array $metrics): float
    {
        if ($target === 'minimize') {
            return -1.0;
        } elseif ($target === 'maximize') {
            return 1.0;
        }
        
        return 0.0;
    }
    
    private function isImprovement(mixed $newValue, mixed $current, mixed $target, array $metrics): bool
    {
        if ($target === 'minimize') {
            return $newValue < $current;
        } elseif ($target === 'maximize') {
            return $newValue > $current;
        }
        
        return true;
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
 * Bayesian Optimization
 */
class BayesianOptimization implements OptimizationAlgorithmInterface
{
    private int $iterations = 0;
    private array $observations = [];
    private array $stats = [
        'converged' => false,
        'acquisitions' => 0
    ];
    
    public function optimize(array $problem): mixed
    {
        $current = $problem['current'];
        $target = $problem['target'];
        $maxIterations = $problem['config']['max_iterations'] ?? 50;
        
        $this->iterations = 0;
        $this->observations = [];
        
        // Add current observation
        $this->addObservation($current, $this->evaluateObjective($current, $target, $problem['metrics']));
        
        for ($i = 0; $i < $maxIterations; $i++) {
            $this->iterations++;
            
            // Find next point to evaluate using acquisition function
            $nextPoint = $this->acquisitionFunction($problem);
            
            // Evaluate objective
            $objective = $this->evaluateObjective($nextPoint, $target, $problem['metrics']);
            
            // Add observation
            $this->addObservation($nextPoint, $objective);
            
            $this->stats['acquisitions']++;
            
            // Check convergence
            if ($this->checkConvergence()) {
                $this->stats['converged'] = true;
                break;
            }
        }
        
        // Return best observed point
        return $this->getBestPoint($target);
    }
    
    public function getIterations(): int
    {
        return $this->iterations;
    }
    
    public function getStatistics(): array
    {
        return $this->stats;
    }
    
    public function cleanup(): void
    {
        $this->iterations = 0;
        $this->observations = [];
        $this->stats = ['converged' => false, 'acquisitions' => 0];
    }
    
    private function addObservation(mixed $point, float $objective): void
    {
        $this->observations[] = [
            'point' => $point,
            'objective' => $objective
        ];
    }
    
    private function evaluateObjective(mixed $point, mixed $target, array $metrics): float
    {
        // Simple objective function
        if ($target === 'minimize') {
            return (float)$point;
        } elseif ($target === 'maximize') {
            return -(float)$point;
        }
        
        return 0.0;
    }
    
    private function acquisitionFunction(array $problem): mixed
    {
        // Simple acquisition function (random sampling)
        $constraints = $problem['constraints'];
        $min = $constraints['min'] ?? 0;
        $max = $constraints['max'] ?? 100;
        
        return $min + (($max - $min) * (mt_rand() / mt_getrandmax()));
    }
    
    private function checkConvergence(): bool
    {
        if (count($this->observations) < 5) {
            return false;
        }
        
        // Check if last few observations are similar
        $recent = array_slice($this->observations, -5);
        $objectives = array_column($recent, 'objective');
        
        $variance = $this->calculateVariance($objectives);
        return $variance < 0.01;
    }
    
    private function getBestPoint(mixed $target): mixed
    {
        if (empty($this->observations)) {
            return 0;
        }
        
        $best = $this->observations[0];
        
        foreach ($this->observations as $observation) {
            if ($target === 'minimize' && $observation['objective'] < $best['objective']) {
                $best = $observation;
            } elseif ($target === 'maximize' && $observation['objective'] > $best['objective']) {
                $best = $observation;
            }
        }
        
        return $best['point'];
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
 * Genetic Algorithm Optimizer
 */
class GeneticAlgorithm implements OptimizationAlgorithmInterface
{
    private int $iterations = 0;
    private array $population = [];
    private array $stats = [
        'converged' => false,
        'generations' => 0
    ];
    
    public function optimize(array $problem): mixed
    {
        $current = $problem['current'];
        $target = $problem['target'];
        $maxIterations = $problem['config']['max_iterations'] ?? 50;
        $constraints = $problem['constraints'];
        
        $this->iterations = 0;
        $this->initializePopulation($current, $constraints);
        
        for ($i = 0; $i < $maxIterations; $i++) {
            $this->iterations++;
            
            // Evaluate fitness
            $this->evaluateFitness($target, $problem['metrics']);
            
            // Selection
            $this->selection();
            
            // Crossover
            $this->crossover();
            
            // Mutation
            $this->mutation();
            
            // Check convergence
            if ($this->checkConvergence()) {
                $this->stats['converged'] = true;
                break;
            }
        }
        
        // Return best individual
        return $this->getBestIndividual();
    }
    
    public function getIterations(): int
    {
        return $this->iterations;
    }
    
    public function getStatistics(): array
    {
        return $this->stats;
    }
    
    public function cleanup(): void
    {
        $this->iterations = 0;
        $this->population = [];
        $this->stats = ['converged' => false, 'generations' => 0];
    }
    
    private function initializePopulation(mixed $current, array $constraints): void
    {
        $populationSize = 20;
        $min = $constraints['min'] ?? 0;
        $max = $constraints['max'] ?? 100;
        
        $this->population = [];
        for ($i = 0; $i < $populationSize; $i++) {
            $this->population[] = [
                'value' => $min + (($max - $min) * (mt_rand() / mt_getrandmax())),
                'fitness' => 0.0
            ];
        }
        
        // Add current value to population
        $this->population[] = [
            'value' => $current,
            'fitness' => 0.0
        ];
    }
    
    private function evaluateFitness(mixed $target, array $metrics): void
    {
        foreach ($this->population as &$individual) {
            $individual['fitness'] = $this->calculateFitness($individual['value'], $target, $metrics);
        }
    }
    
    private function calculateFitness(mixed $value, mixed $target, array $metrics): float
    {
        if ($target === 'minimize') {
            return 1.0 / (1.0 + (float)$value);
        } elseif ($target === 'maximize') {
            return (float)$value;
        }
        
        return 1.0;
    }
    
    private function selection(): void
    {
        // Sort by fitness
        usort($this->population, fn($a, $b) => $b['fitness'] <=> $a['fitness']);
        
        // Keep top 50%
        $this->population = array_slice($this->population, 0, ceil(count($this->population) / 2));
    }
    
    private function crossover(): void
    {
        $newPopulation = $this->population;
        
        while (count($newPopulation) < 20) {
            $parent1 = $this->population[array_rand($this->population)];
            $parent2 = $this->population[array_rand($this->population)];
            
            $child = [
                'value' => ($parent1['value'] + $parent2['value']) / 2,
                'fitness' => 0.0
            ];
            
            $newPopulation[] = $child;
        }
        
        $this->population = $newPopulation;
    }
    
    private function mutation(): void
    {
        foreach ($this->population as &$individual) {
            if (mt_rand() / mt_getrandmax() < 0.1) { // 10% mutation rate
                $individual['value'] *= (0.9 + (0.2 * (mt_rand() / mt_getrandmax())));
            }
        }
    }
    
    private function checkConvergence(): bool
    {
        if (count($this->population) < 2) {
            return true;
        }
        
        $fitnesses = array_column($this->population, 'fitness');
        $variance = $this->calculateVariance($fitnesses);
        
        return $variance < 0.01;
    }
    
    private function getBestIndividual(): mixed
    {
        if (empty($this->population)) {
            return 0;
        }
        
        $best = $this->population[0];
        foreach ($this->population as $individual) {
            if ($individual['fitness'] > $best['fitness']) {
                $best = $individual;
            }
        }
        
        return $best['value'];
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
 * Simulated Annealing Optimizer
 */
class SimulatedAnnealing implements OptimizationAlgorithmInterface
{
    private int $iterations = 0;
    private array $stats = [
        'converged' => false,
        'acceptances' => 0
    ];
    
    public function optimize(array $problem): mixed
    {
        $current = $problem['current'];
        $target = $problem['target'];
        $maxIterations = $problem['config']['max_iterations'] ?? 100;
        $constraints = $problem['constraints'];
        
        $this->iterations = 0;
        $this->stats['acceptances'] = 0;
        
        $temperature = 100.0;
        $coolingRate = 0.95;
        
        $bestValue = $current;
        $bestObjective = $this->evaluateObjective($current, $target, $problem['metrics']);
        
        for ($i = 0; $i < $maxIterations; $i++) {
            $this->iterations++;
            
            // Generate neighbor
            $neighbor = $this->generateNeighbor($current, $constraints);
            $neighborObjective = $this->evaluateObjective($neighbor, $target, $problem['metrics']);
            
            // Calculate acceptance probability
            $delta = $neighborObjective - $bestObjective;
            $probability = exp(-$delta / $temperature);
            
            // Accept or reject
            if ($delta < 0 || (mt_rand() / mt_getrandmax()) < $probability) {
                $current = $neighbor;
                $this->stats['acceptances']++;
                
                if ($neighborObjective < $bestObjective) {
                    $bestValue = $neighbor;
                    $bestObjective = $neighborObjective;
                }
            }
            
            // Cool down
            $temperature *= $coolingRate;
            
            // Check convergence
            if ($temperature < 0.1) {
                $this->stats['converged'] = true;
                break;
            }
        }
        
        return $bestValue;
    }
    
    public function getIterations(): int
    {
        return $this->iterations;
    }
    
    public function getStatistics(): array
    {
        return $this->stats;
    }
    
    public function cleanup(): void
    {
        $this->iterations = 0;
        $this->stats = ['converged' => false, 'acceptances' => 0];
    }
    
    private function generateNeighbor(mixed $current, array $constraints): mixed
    {
        $min = $constraints['min'] ?? 0;
        $max = $constraints['max'] ?? 100;
        
        $range = $max - $min;
        $step = $range * 0.1;
        
        $neighbor = $current + (($step * 2) * (mt_rand() / mt_getrandmax()) - $step);
        
        return max($min, min($max, $neighbor));
    }
    
    private function evaluateObjective(mixed $value, mixed $target, array $metrics): float
    {
        if ($target === 'minimize') {
            return (float)$value;
        } elseif ($target === 'maximize') {
            return -(float)$value;
        }
        
        return 0.0;
    }
} 