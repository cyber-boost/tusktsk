<?php

declare(strict_types=1);

namespace TuskLang\A5\G6;

use TuskLang\CoreOperator;
use InvalidArgumentException;

/**
 * MathOperator - Comprehensive mathematical operations and calculations
 * 
 * Provides advanced mathematical operations including basic arithmetic,
 * statistical calculations, trigonometry, number theory, and financial math.
 */
class MathOperator extends CoreOperator
{
    private const PI = M_PI;
    private const E = M_E;

    public function getName(): string
    {
        return 'math';
    }

    public function getDescription(): string 
    {
        return 'Comprehensive mathematical operations and calculations';
    }

    public function getSupportedActions(): array
    {
        return [
            'basic', 'statistics', 'trigonometry', 'logarithm', 'power', 'root',
            'factorial', 'fibonacci', 'prime', 'gcd', 'lcm', 'random',
            'percentage', 'financial', 'geometry', 'convert_base'
        ];
    }

    public function execute(string $action, array $params = []): mixed
    {
        return match($action) {
            'basic' => $this->basicMath($params['operation'] ?? '', $params['operands'] ?? []),
            'statistics' => $this->statistics($params['data'] ?? []),
            'trigonometry' => $this->trigonometry($params['function'] ?? '', $params['angle'] ?? 0, $params['unit'] ?? 'radians'),
            'logarithm' => $this->logarithm($params['number'] ?? 1, $params['base'] ?? M_E),
            'power' => $this->power($params['base'] ?? 0, $params['exponent'] ?? 0),
            'root' => $this->root($params['number'] ?? 0, $params['n'] ?? 2),
            'factorial' => $this->factorial($params['number'] ?? 0),
            'fibonacci' => $this->fibonacci($params['n'] ?? 0),
            'prime' => $this->primeOperations($params['operation'] ?? 'check', $params['number'] ?? 2),
            'gcd' => $this->gcd($params['numbers'] ?? []),
            'lcm' => $this->lcm($params['numbers'] ?? []),
            'random' => $this->randomNumber($params['min'] ?? 0, $params['max'] ?? 100, $params['type'] ?? 'integer'),
            'percentage' => $this->percentage($params['operation'] ?? '', $params['value'] ?? 0, $params['percent'] ?? 0),
            'financial' => $this->financialCalculation($params['type'] ?? '', $params),
            'geometry' => $this->geometry($params['shape'] ?? '', $params),
            'convert_base' => $this->convertBase($params['number'] ?? '0', $params['from_base'] ?? 10, $params['to_base'] ?? 10),
            default => throw new InvalidArgumentException("Unsupported action: {$action}")
        };
    }

    /**
     * Basic mathematical operations
     */
    private function basicMath(string $operation, array $operands): array
    {
        if (empty($operands)) {
            throw new InvalidArgumentException('Operands cannot be empty');
        }

        // Convert to numbers
        $numbers = array_map('floatval', $operands);
        
        $result = match($operation) {
            'add', '+' => array_sum($numbers),
            'subtract', '-' => array_reduce(array_slice($numbers, 1), fn($carry, $num) => $carry - $num, $numbers[0]),
            'multiply', '*' => array_product($numbers),
            'divide', '/' => $this->divide($numbers),
            'modulo', '%' => $this->modulo($numbers),
            'average', 'mean' => array_sum($numbers) / count($numbers),
            'min' => min($numbers),
            'max' => max($numbers),
            'abs' => array_map('abs', $numbers),
            'round' => array_map(fn($n) => round($n, $operands[1] ?? 0), [$numbers[0]]),
            'ceil' => array_map('ceil', $numbers),
            'floor' => array_map('floor', $numbers),
            default => throw new InvalidArgumentException("Unknown operation: {$operation}")
        };

        return [
            'operation' => $operation,
            'operands' => $operands,
            'result' => $result,
            'type' => gettype($result)
        ];
    }

    /**
     * Statistical calculations
     */
    private function statistics(array $data): array
    {
        if (empty($data)) {
            throw new InvalidArgumentException('Data cannot be empty for statistics');
        }

        $numbers = array_map('floatval', $data);
        sort($numbers);
        
        $count = count($numbers);
        $sum = array_sum($numbers);
        $mean = $sum / $count;
        
        // Median
        $median = $count % 2 === 0 
            ? ($numbers[intval($count / 2) - 1] + $numbers[intval($count / 2)]) / 2
            : $numbers[intval($count / 2)];
            
        // Mode
        $frequency = array_count_values($numbers);
        arsort($frequency);
        $mode = array_key_first($frequency);
        $modeCount = reset($frequency);
        
        // Variance and Standard Deviation
        $variance = array_sum(array_map(fn($x) => pow($x - $mean, 2), $numbers)) / $count;
        $stdDev = sqrt($variance);
        
        // Range
        $range = max($numbers) - min($numbers);
        
        // Quartiles
        $q1 = $this->percentile($numbers, 25);
        $q3 = $this->percentile($numbers, 75);
        $iqr = $q3 - $q1;

        return [
            'count' => $count,
            'sum' => $sum,
            'mean' => round($mean, 6),
            'median' => $median,
            'mode' => $mode,
            'mode_frequency' => $modeCount,
            'min' => min($numbers),
            'max' => max($numbers),
            'range' => $range,
            'variance' => round($variance, 6),
            'standard_deviation' => round($stdDev, 6),
            'quartiles' => [
                'q1' => $q1,
                'q2_median' => $median,
                'q3' => $q3,
                'iqr' => $iqr
            ]
        ];
    }

    /**
     * Trigonometric functions
     */
    private function trigonometry(string $function, float $angle, string $unit = 'radians'): array
    {
        // Convert to radians if necessary
        $radians = $unit === 'degrees' ? deg2rad($angle) : $angle;
        
        $result = match($function) {
            'sin' => sin($radians),
            'cos' => cos($radians),
            'tan' => tan($radians),
            'asin', 'arcsin' => asin($radians),
            'acos', 'arccos' => acos($radians),
            'atan', 'arctan' => atan($radians),
            'sinh' => sinh($radians),
            'cosh' => cosh($radians),
            'tanh' => tanh($radians),
            'csc' => 1 / sin($radians),
            'sec' => 1 / cos($radians),
            'cot' => 1 / tan($radians),
            default => throw new InvalidArgumentException("Unknown trigonometric function: {$function}")
        };

        return [
            'function' => $function,
            'angle' => $angle,
            'unit' => $unit,
            'radians' => $radians,
            'result' => round($result, 10)
        ];
    }

    /**
     * Logarithmic functions
     */
    private function logarithm(float $number, float $base = M_E): array
    {
        if ($number <= 0) {
            throw new InvalidArgumentException('Number must be positive for logarithm');
        }

        if ($base <= 0 || $base === 1) {
            throw new InvalidArgumentException('Base must be positive and not equal to 1');
        }

        $result = $base === M_E ? log($number) : log($number) / log($base);
        
        return [
            'number' => $number,
            'base' => $base,
            'result' => $result,
            'natural_log' => log($number),
            'log10' => log10($number)
        ];
    }

    /**
     * Power and exponentiation
     */
    private function power(float $base, float $exponent): array
    {
        $result = pow($base, $exponent);
        
        return [
            'base' => $base,
            'exponent' => $exponent,
            'result' => $result,
            'is_integer_result' => is_int($result),
            'scientific_notation' => sprintf('%e', $result)
        ];
    }

    /**
     * Root calculations
     */
    private function root(float $number, int $n = 2): array
    {
        if ($n === 0) {
            throw new InvalidArgumentException('Root degree cannot be zero');
        }

        if ($n % 2 === 0 && $number < 0) {
            throw new InvalidArgumentException('Cannot calculate even root of negative number');
        }

        $result = $n === 2 ? sqrt($number) : pow($number, 1 / $n);
        
        return [
            'number' => $number,
            'degree' => $n,
            'result' => $result,
            'square_root' => sqrt(abs($number)),
            'cube_root' => pow($number, 1/3)
        ];
    }

    /**
     * Factorial calculation
     */
    private function factorial(int $number): array
    {
        if ($number < 0) {
            throw new InvalidArgumentException('Factorial is not defined for negative numbers');
        }

        if ($number > 170) {
            throw new InvalidArgumentException('Factorial too large (would overflow)');
        }

        $result = 1;
        $steps = [];
        
        for ($i = 1; $i <= $number; $i++) {
            $result *= $i;
            $steps[] = $i;
        }

        return [
            'number' => $number,
            'result' => $result,
            'steps' => $steps,
            'approximation_stirling' => $this->stirlingApproximation($number)
        ];
    }

    /**
     * Fibonacci sequence
     */
    private function fibonacci(int $n): array
    {
        if ($n < 0) {
            throw new InvalidArgumentException('n must be non-negative');
        }

        $sequence = [];
        $a = 0;
        $b = 1;
        
        for ($i = 0; $i <= $n; $i++) {
            if ($i === 0) {
                $sequence[] = $a;
            } elseif ($i === 1) {
                $sequence[] = $b;
            } else {
                $temp = $a + $b;
                $sequence[] = $temp;
                $a = $b;
                $b = $temp;
            }
        }

        return [
            'n' => $n,
            'value' => $sequence[$n] ?? 0,
            'sequence' => array_slice($sequence, 0, min(20, count($sequence))), // Limit display
            'golden_ratio_approximation' => $n > 1 ? $sequence[$n] / $sequence[$n - 1] : 0
        ];
    }

    /**
     * Prime number operations
     */
    private function primeOperations(string $operation, int $number): array
    {
        return match($operation) {
            'check' => $this->isPrime($number),
            'next' => $this->nextPrime($number),
            'factors' => $this->primeFactors($number),
            'list' => $this->primeList($number),
            default => throw new InvalidArgumentException("Unknown prime operation: {$operation}")
        };
    }

    /**
     * Greatest Common Divisor
     */
    private function gcd(array $numbers): array
    {
        if (count($numbers) < 2) {
            throw new InvalidArgumentException('At least 2 numbers required for GCD');
        }

        $result = intval($numbers[0]);
        for ($i = 1; $i < count($numbers); $i++) {
            $result = $this->euclideanGcd($result, intval($numbers[$i]));
        }

        return [
            'numbers' => $numbers,
            'gcd' => $result,
            'coprime' => $result === 1
        ];
    }

    /**
     * Least Common Multiple
     */
    private function lcm(array $numbers): array
    {
        if (count($numbers) < 2) {
            throw new InvalidArgumentException('At least 2 numbers required for LCM');
        }

        $result = intval($numbers[0]);
        for ($i = 1; $i < count($numbers); $i++) {
            $gcd = $this->euclideanGcd($result, intval($numbers[$i]));
            $result = ($result * intval($numbers[$i])) / $gcd;
        }

        return [
            'numbers' => $numbers,
            'lcm' => $result
        ];
    }

    /**
     * Generate random numbers
     */
    private function randomNumber(float $min, float $max, string $type = 'integer'): array
    {
        if ($min > $max) {
            throw new InvalidArgumentException('Min cannot be greater than max');
        }

        $result = match($type) {
            'integer' => random_int(intval($min), intval($max)),
            'float' => $min + mt_rand() / mt_getrandmax() * ($max - $min),
            'gaussian' => $this->gaussianRandom($min, $max),
            default => throw new InvalidArgumentException("Unknown random type: {$type}")
        };

        return [
            'result' => $result,
            'min' => $min,
            'max' => $max,
            'type' => $type,
            'seed_used' => mt_getrandmax()
        ];
    }

    /**
     * Percentage calculations
     */
    private function percentage(string $operation, float $value, float $percent): array
    {
        $result = match($operation) {
            'of' => ($percent / 100) * $value, // percent of value
            'is' => ($value / $percent) * 100, // value is what percent of percent
            'increase' => $value + (($percent / 100) * $value),
            'decrease' => $value - (($percent / 100) * $value),
            'change' => (($percent - $value) / $value) * 100, // percent change
            default => throw new InvalidArgumentException("Unknown percentage operation: {$operation}")
        };

        return [
            'operation' => $operation,
            'value' => $value,
            'percent' => $percent,
            'result' => round($result, 2)
        ];
    }

    /**
     * Financial calculations
     */
    private function financialCalculation(string $type, array $params): array
    {
        return match($type) {
            'compound_interest' => $this->compoundInterest($params),
            'simple_interest' => $this->simpleInterest($params),
            'loan_payment' => $this->loanPayment($params),
            'present_value' => $this->presentValue($params),
            'future_value' => $this->futureValue($params),
            default => throw new InvalidArgumentException("Unknown financial calculation: {$type}")
        };
    }

    /**
     * Geometric calculations
     */
    private function geometry(string $shape, array $params): array
    {
        return match($shape) {
            'circle' => $this->circleCalculations($params),
            'triangle' => $this->triangleCalculations($params),
            'rectangle' => $this->rectangleCalculations($params),
            'sphere' => $this->sphereCalculations($params),
            'cylinder' => $this->cylinderCalculations($params),
            default => throw new InvalidArgumentException("Unknown geometric shape: {$shape}")
        };
    }

    /**
     * Convert between number bases
     */
    private function convertBase(string $number, int $fromBase, int $toBase): array
    {
        if ($fromBase < 2 || $fromBase > 36 || $toBase < 2 || $toBase > 36) {
            throw new InvalidArgumentException('Base must be between 2 and 36');
        }

        // Convert to decimal first
        $decimal = base_convert($number, $fromBase, 10);
        
        // Convert to target base
        $result = base_convert($decimal, 10, $toBase);

        return [
            'original' => $number,
            'from_base' => $fromBase,
            'to_base' => $toBase,
            'result' => $result,
            'decimal_value' => $decimal
        ];
    }

    // Helper methods

    private function divide(array $numbers): float
    {
        $result = $numbers[0];
        for ($i = 1; $i < count($numbers); $i++) {
            if ($numbers[$i] == 0) {
                throw new InvalidArgumentException('Division by zero');
            }
            $result /= $numbers[$i];
        }
        return $result;
    }

    private function modulo(array $numbers): float
    {
        if (count($numbers) !== 2) {
            throw new InvalidArgumentException('Modulo requires exactly 2 operands');
        }
        
        if ($numbers[1] == 0) {
            throw new InvalidArgumentException('Modulo by zero');
        }
        
        return fmod($numbers[0], $numbers[1]);
    }

    private function percentile(array $sortedData, float $percentile): float
    {
        $index = ($percentile / 100) * (count($sortedData) - 1);
        $lower = floor($index);
        $upper = ceil($index);
        
        if ($lower == $upper) {
            return $sortedData[$lower];
        }
        
        return $sortedData[$lower] + ($index - $lower) * ($sortedData[$upper] - $sortedData[$lower]);
    }

    private function stirlingApproximation(int $n): float
    {
        if ($n === 0) return 1;
        return sqrt(2 * M_PI * $n) * pow($n / M_E, $n);
    }

    private function isPrime(int $number): array
    {
        if ($number < 2) {
            return ['number' => $number, 'is_prime' => false, 'reason' => 'Less than 2'];
        }

        if ($number === 2) {
            return ['number' => $number, 'is_prime' => true, 'reason' => 'Is 2'];
        }

        if ($number % 2 === 0) {
            return ['number' => $number, 'is_prime' => false, 'reason' => 'Even number'];
        }

        $sqrt = intval(sqrt($number));
        for ($i = 3; $i <= $sqrt; $i += 2) {
            if ($number % $i === 0) {
                return [
                    'number' => $number, 
                    'is_prime' => false, 
                    'reason' => "Divisible by {$i}",
                    'factor' => $i
                ];
            }
        }

        return ['number' => $number, 'is_prime' => true, 'reason' => 'No divisors found'];
    }

    private function nextPrime(int $number): array
    {
        $candidate = $number + 1;
        while (!$this->isPrime($candidate)['is_prime']) {
            $candidate++;
        }
        
        return [
            'original' => $number,
            'next_prime' => $candidate,
            'gap' => $candidate - $number
        ];
    }

    private function primeFactors(int $number): array
    {
        if ($number < 2) {
            return ['number' => $number, 'factors' => []];
        }

        $factors = [];
        $d = 2;
        
        while ($d * $d <= $number) {
            while ($number % $d === 0) {
                $factors[] = $d;
                $number /= $d;
            }
            $d++;
        }
        
        if ($number > 1) {
            $factors[] = $number;
        }

        return [
            'number' => $number,
            'factors' => $factors,
            'unique_factors' => array_unique($factors),
            'factorization' => array_count_values($factors)
        ];
    }

    private function primeList(int $limit): array
    {
        if ($limit < 2) {
            return ['limit' => $limit, 'primes' => []];
        }

        $sieve = array_fill(2, $limit - 1, true);
        
        for ($i = 2; $i * $i <= $limit; $i++) {
            if ($sieve[$i]) {
                for ($j = $i * $i; $j <= $limit; $j += $i) {
                    $sieve[$j] = false;
                }
            }
        }

        $primes = array_keys(array_filter($sieve));
        
        return [
            'limit' => $limit,
            'primes' => $primes,
            'count' => count($primes)
        ];
    }

    private function euclideanGcd(int $a, int $b): int
    {
        while ($b !== 0) {
            $temp = $b;
            $b = $a % $b;
            $a = $temp;
        }
        return abs($a);
    }

    private function gaussianRandom(float $min, float $max): float
    {
        $mean = ($min + $max) / 2;
        $stddev = ($max - $min) / 6; // 99.7% within range
        
        // Box-Muller transform
        static $haveSpare = false;
        static $spare;
        
        if ($haveSpare) {
            $haveSpare = false;
            return $spare * $stddev + $mean;
        }
        
        $haveSpare = true;
        $u1 = mt_rand() / mt_getrandmax();
        $u2 = mt_rand() / mt_getrandmax();
        
        $mag = $stddev * sqrt(-2.0 * log($u1));
        $spare = $mag * cos(2.0 * M_PI * $u2);
        
        return $mag * sin(2.0 * M_PI * $u2) + $mean;
    }

    private function compoundInterest(array $params): array
    {
        $principal = $params['principal'] ?? 0;
        $rate = $params['rate'] ?? 0;
        $time = $params['time'] ?? 0;
        $compound = $params['compound_frequency'] ?? 1;
        
        $amount = $principal * pow(1 + $rate / $compound, $compound * $time);
        $interest = $amount - $principal;
        
        return [
            'principal' => $principal,
            'rate' => $rate,
            'time' => $time,
            'compound_frequency' => $compound,
            'final_amount' => round($amount, 2),
            'interest_earned' => round($interest, 2)
        ];
    }

    private function simpleInterest(array $params): array
    {
        $principal = $params['principal'] ?? 0;
        $rate = $params['rate'] ?? 0;
        $time = $params['time'] ?? 0;
        
        $interest = $principal * $rate * $time;
        $amount = $principal + $interest;
        
        return [
            'principal' => $principal,
            'rate' => $rate,
            'time' => $time,
            'interest' => round($interest, 2),
            'final_amount' => round($amount, 2)
        ];
    }

    private function loanPayment(array $params): array
    {
        $principal = $params['principal'] ?? 0;
        $rate = $params['rate'] ?? 0;
        $periods = $params['periods'] ?? 0;
        
        if ($rate === 0) {
            $payment = $principal / $periods;
        } else {
            $payment = $principal * ($rate * pow(1 + $rate, $periods)) / (pow(1 + $rate, $periods) - 1);
        }
        
        return [
            'principal' => $principal,
            'rate' => $rate,
            'periods' => $periods,
            'payment' => round($payment, 2),
            'total_paid' => round($payment * $periods, 2),
            'total_interest' => round(($payment * $periods) - $principal, 2)
        ];
    }

    private function presentValue(array $params): array
    {
        $futureValue = $params['future_value'] ?? 0;
        $rate = $params['rate'] ?? 0;
        $periods = $params['periods'] ?? 0;
        
        $pv = $futureValue / pow(1 + $rate, $periods);
        
        return [
            'future_value' => $futureValue,
            'rate' => $rate,
            'periods' => $periods,
            'present_value' => round($pv, 2)
        ];
    }

    private function futureValue(array $params): array
    {
        $presentValue = $params['present_value'] ?? 0;
        $rate = $params['rate'] ?? 0;
        $periods = $params['periods'] ?? 0;
        
        $fv = $presentValue * pow(1 + $rate, $periods);
        
        return [
            'present_value' => $presentValue,
            'rate' => $rate,
            'periods' => $periods,
            'future_value' => round($fv, 2)
        ];
    }

    private function circleCalculations(array $params): array
    {
        $radius = $params['radius'] ?? 0;
        
        return [
            'radius' => $radius,
            'diameter' => $radius * 2,
            'circumference' => round(2 * M_PI * $radius, 4),
            'area' => round(M_PI * $radius * $radius, 4)
        ];
    }

    private function triangleCalculations(array $params): array
    {
        $base = $params['base'] ?? 0;
        $height = $params['height'] ?? 0;
        $side_a = $params['side_a'] ?? $base;
        $side_b = $params['side_b'] ?? $height;
        $side_c = $params['side_c'] ?? 0;
        
        $area = 0.5 * $base * $height;
        $perimeter = $side_a + $side_b + $side_c;
        
        return [
            'base' => $base,
            'height' => $height,
            'area' => $area,
            'perimeter' => $perimeter
        ];
    }

    private function rectangleCalculations(array $params): array
    {
        $length = $params['length'] ?? 0;
        $width = $params['width'] ?? 0;
        
        return [
            'length' => $length,
            'width' => $width,
            'area' => $length * $width,
            'perimeter' => 2 * ($length + $width),
            'diagonal' => round(sqrt($length * $length + $width * $width), 4)
        ];
    }

    private function sphereCalculations(array $params): array
    {
        $radius = $params['radius'] ?? 0;
        
        return [
            'radius' => $radius,
            'diameter' => $radius * 2,
            'surface_area' => round(4 * M_PI * $radius * $radius, 4),
            'volume' => round((4/3) * M_PI * $radius * $radius * $radius, 4)
        ];
    }

    private function cylinderCalculations(array $params): array
    {
        $radius = $params['radius'] ?? 0;
        $height = $params['height'] ?? 0;
        
        return [
            'radius' => $radius,
            'height' => $height,
            'base_area' => round(M_PI * $radius * $radius, 4),
            'lateral_area' => round(2 * M_PI * $radius * $height, 4),
            'total_area' => round(2 * M_PI * $radius * ($radius + $height), 4),
            'volume' => round(M_PI * $radius * $radius * $height, 4)
        ];
    }
} 