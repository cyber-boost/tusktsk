<?php

declare(strict_types=1);

namespace TuskLang\A5\G2;

use TuskLang\CoreOperator;
use InvalidArgumentException;

/**
 * ArrayTransformOperator - Advanced array manipulation, filtering, mapping, and restructuring
 * 
 * Provides comprehensive array transformation operations including mapping,
 * filtering, grouping, sorting, and complex restructuring with functional patterns.
 */
class ArrayTransformOperator extends CoreOperator
{
    public function getName(): string
    {
        return 'array_transform';
    }

    public function getDescription(): string 
    {
        return 'Advanced array manipulation, filtering, mapping, and restructuring operations';
    }

    public function getSupportedActions(): array
    {
        return [
            'map', 'filter', 'reduce', 'group_by', 'sort_by', 'flatten',
            'chunk', 'zip', 'unique', 'diff', 'intersect', 'merge',
            'pluck', 'pivot', 'transpose', 'reshape'
        ];
    }

    public function execute(string $action, array $params = []): mixed
    {
        return match($action) {
            'map' => $this->mapArray($params['array'] ?? [], $params['callback'] ?? null),
            'filter' => $this->filterArray($params['array'] ?? [], $params['callback'] ?? null),
            'reduce' => $this->reduceArray($params['array'] ?? [], $params['callback'] ?? null, $params['initial'] ?? null),
            'group_by' => $this->groupBy($params['array'] ?? [], $params['key'] ?? ''),
            'sort_by' => $this->sortBy($params['array'] ?? [], $params['key'] ?? '', $params['direction'] ?? 'asc'),
            'flatten' => $this->flattenArray($params['array'] ?? [], $params['depth'] ?? null),
            'chunk' => $this->chunkArray($params['array'] ?? [], $params['size'] ?? 1),
            'zip' => $this->zipArrays($params['arrays'] ?? []),
            'unique' => $this->uniqueArray($params['array'] ?? [], $params['key'] ?? null),
            'diff' => $this->diffArrays($params['array1'] ?? [], $params['array2'] ?? [], $params['key'] ?? null),
            'intersect' => $this->intersectArrays($params['array1'] ?? [], $params['array2'] ?? [], $params['key'] ?? null),
            'merge' => $this->mergeArrays($params['arrays'] ?? [], $params['strategy'] ?? 'replace'),
            'pluck' => $this->pluckValues($params['array'] ?? [], $params['key'] ?? ''),
            'pivot' => $this->pivotArray($params['array'] ?? [], $params['row_key'] ?? '', $params['col_key'] ?? '', $params['value_key'] ?? ''),
            'transpose' => $this->transposeArray($params['array'] ?? []),
            'reshape' => $this->reshapeArray($params['array'] ?? [], $params['shape'] ?? []),
            default => throw new InvalidArgumentException("Unsupported action: {$action}")
        };
    }

    /**
     * Map array elements with callback function
     */
    private function mapArray(array $array, ?callable $callback): array
    {
        if ($callback === null) {
            throw new InvalidArgumentException('Callback function is required for map operation');
        }

        $result = [];
        foreach ($array as $key => $value) {
            $result[$key] = $callback($value, $key);
        }

        return $result;
    }

    /**
     * Filter array elements with callback function
     */
    private function filterArray(array $array, ?callable $callback): array
    {
        if ($callback === null) {
            return array_filter($array);
        }

        $result = [];
        foreach ($array as $key => $value) {
            if ($callback($value, $key)) {
                $result[$key] = $value;
            }
        }

        return $result;
    }

    /**
     * Reduce array to single value using callback
     */
    private function reduceArray(array $array, ?callable $callback, mixed $initial = null): mixed
    {
        if ($callback === null) {
            throw new InvalidArgumentException('Callback function is required for reduce operation');
        }

        $accumulator = $initial;
        foreach ($array as $key => $value) {
            $accumulator = $callback($accumulator, $value, $key);
        }

        return $accumulator;
    }

    /**
     * Group array elements by key or callback result
     */
    private function groupBy(array $array, string|callable $key): array
    {
        if (empty($key)) {
            throw new InvalidArgumentException('Grouping key or callback is required');
        }

        $groups = [];
        
        foreach ($array as $item) {
            if (is_callable($key)) {
                $groupKey = $key($item);
            } else {
                $groupKey = $this->getNestedValue($item, $key);
            }
            
            $groupKey = (string) $groupKey;
            if (!isset($groups[$groupKey])) {
                $groups[$groupKey] = [];
            }
            $groups[$groupKey][] = $item;
        }

        return $groups;
    }

    /**
     * Sort array by key or callback result
     */
    private function sortBy(array $array, string|callable $key, string $direction = 'asc'): array
    {
        if (empty($key)) {
            sort($array);
            return $direction === 'desc' ? array_reverse($array) : $array;
        }

        uasort($array, function($a, $b) use ($key, $direction) {
            if (is_callable($key)) {
                $valueA = $key($a);
                $valueB = $key($b);
            } else {
                $valueA = $this->getNestedValue($a, $key);
                $valueB = $this->getNestedValue($b, $key);
            }

            $comparison = $valueA <=> $valueB;
            return $direction === 'desc' ? -$comparison : $comparison;
        });

        return $array;
    }

    /**
     * Flatten multi-dimensional array
     */
    private function flattenArray(array $array, ?int $depth = null): array
    {
        $result = [];
        $currentDepth = 0;

        $flatten = function($arr, $currentDepth) use (&$flatten, &$result, $depth) {
            foreach ($arr as $item) {
                if (is_array($item) && ($depth === null || $currentDepth < $depth)) {
                    $flatten($item, $currentDepth + 1);
                } else {
                    $result[] = $item;
                }
            }
        };

        $flatten($array, $currentDepth);
        return $result;
    }

    /**
     * Split array into chunks of specified size
     */
    private function chunkArray(array $array, int $size): array
    {
        if ($size <= 0) {
            throw new InvalidArgumentException('Chunk size must be greater than 0');
        }

        return array_chunk($array, $size, true);
    }

    /**
     * Zip multiple arrays together
     */
    private function zipArrays(array $arrays): array
    {
        if (empty($arrays)) {
            return [];
        }

        // Get the length of the shortest array
        $minLength = min(array_map('count', $arrays));
        $result = [];

        for ($i = 0; $i < $minLength; $i++) {
            $zipped = [];
            foreach ($arrays as $array) {
                $zipped[] = array_values($array)[$i];
            }
            $result[] = $zipped;
        }

        return $result;
    }

    /**
     * Remove duplicate values from array
     */
    private function uniqueArray(array $array, ?string $key = null): array
    {
        if ($key === null) {
            return array_unique($array, SORT_REGULAR);
        }

        $seen = [];
        $result = [];

        foreach ($array as $index => $item) {
            $value = $this->getNestedValue($item, $key);
            $hash = serialize($value);
            
            if (!isset($seen[$hash])) {
                $seen[$hash] = true;
                $result[$index] = $item;
            }
        }

        return $result;
    }

    /**
     * Find difference between two arrays
     */
    private function diffArrays(array $array1, array $array2, ?string $key = null): array
    {
        if ($key === null) {
            return array_diff($array1, $array2);
        }

        $values2 = array_map(fn($item) => $this->getNestedValue($item, $key), $array2);
        
        return array_filter($array1, function($item) use ($values2, $key) {
            $value = $this->getNestedValue($item, $key);
            return !in_array($value, $values2, true);
        });
    }

    /**
     * Find intersection between two arrays
     */
    private function intersectArrays(array $array1, array $array2, ?string $key = null): array
    {
        if ($key === null) {
            return array_intersect($array1, $array2);
        }

        $values2 = array_map(fn($item) => $this->getNestedValue($item, $key), $array2);
        
        return array_filter($array1, function($item) use ($values2, $key) {
            $value = $this->getNestedValue($item, $key);
            return in_array($value, $values2, true);
        });
    }

    /**
     * Merge multiple arrays with different strategies
     */
    private function mergeArrays(array $arrays, string $strategy = 'replace'): array
    {
        if (empty($arrays)) {
            return [];
        }

        $result = array_shift($arrays);

        foreach ($arrays as $array) {
            $result = $this->mergeTwo($result, $array, $strategy);
        }

        return $result;
    }

    /**
     * Merge two arrays with strategy
     */
    private function mergeTwo(array $array1, array $array2, string $strategy): array
    {
        return match($strategy) {
            'replace' => array_merge($array1, $array2),
            'preserve' => $array2 + $array1,
            'recursive' => array_merge_recursive($array1, $array2),
            'numeric_keys' => array_values(array_merge($array1, $array2)),
            default => throw new InvalidArgumentException("Unsupported merge strategy: {$strategy}")
        };
    }

    /**
     * Extract specific field values from array of objects/arrays
     */
    private function pluckValues(array $array, string $key): array
    {
        if (empty($key)) {
            throw new InvalidArgumentException('Key is required for pluck operation');
        }

        $result = [];
        foreach ($array as $item) {
            $value = $this->getNestedValue($item, $key);
            if ($value !== null) {
                $result[] = $value;
            }
        }

        return $result;
    }

    /**
     * Create pivot table from array data
     */
    private function pivotArray(array $array, string $rowKey, string $colKey, string $valueKey): array
    {
        if (empty($rowKey) || empty($colKey) || empty($valueKey)) {
            throw new InvalidArgumentException('Row key, column key, and value key are required for pivot');
        }

        $pivot = [];

        foreach ($array as $item) {
            $row = $this->getNestedValue($item, $rowKey);
            $col = $this->getNestedValue($item, $colKey);
            $value = $this->getNestedValue($item, $valueKey);

            if (!isset($pivot[$row])) {
                $pivot[$row] = [];
            }

            $pivot[$row][$col] = $value;
        }

        return $pivot;
    }

    /**
     * Transpose 2D array (swap rows and columns)
     */
    private function transposeArray(array $array): array
    {
        if (empty($array)) {
            return [];
        }

        // Ensure all rows have the same length
        $maxCols = max(array_map('count', $array));
        $result = [];

        for ($col = 0; $col < $maxCols; $col++) {
            $result[$col] = [];
            foreach ($array as $row => $data) {
                $result[$col][$row] = $data[$col] ?? null;
            }
        }

        return $result;
    }

    /**
     * Reshape array to specified dimensions
     */
    private function reshapeArray(array $array, array $shape): array
    {
        if (empty($shape)) {
            throw new InvalidArgumentException('Shape specification is required');
        }

        $flatArray = $this->flattenArray($array);
        $totalElements = count($flatArray);
        
        // Calculate total elements needed
        $requiredElements = array_product($shape);
        
        if ($totalElements !== $requiredElements) {
            throw new InvalidArgumentException(
                "Cannot reshape array: {$totalElements} elements to shape requiring {$requiredElements} elements"
            );
        }

        return $this->buildMultiDimensionalArray($flatArray, $shape);
    }

    /**
     * Build multi-dimensional array from flat array and shape
     */
    private function buildMultiDimensionalArray(array $flatArray, array $shape): array
    {
        if (count($shape) === 1) {
            return array_slice($flatArray, 0, $shape[0]);
        }

        $result = [];
        $firstDim = array_shift($shape);
        $elementsPerGroup = array_product($shape);

        for ($i = 0; $i < $firstDim; $i++) {
            $slice = array_slice($flatArray, $i * $elementsPerGroup, $elementsPerGroup);
            $result[] = $this->buildMultiDimensionalArray($slice, $shape);
        }

        return $result;
    }

    /**
     * Get nested value from array/object using dot notation
     */
    private function getNestedValue(mixed $data, string $key): mixed
    {
        if (empty($key)) {
            return $data;
        }

        $keys = explode('.', $key);
        $current = $data;

        foreach ($keys as $k) {
            if (is_array($current) && array_key_exists($k, $current)) {
                $current = $current[$k];
            } elseif (is_object($current) && property_exists($current, $k)) {
                $current = $current->$k;
            } else {
                return null;
            }
        }

        return $current;
    }
} 