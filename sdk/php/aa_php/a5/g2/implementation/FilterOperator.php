<?php

declare(strict_types=1);

namespace TuskLang\A5\G2;

use TuskLang\CoreOperator;
use InvalidArgumentException;

/**
 * FilterOperator - Advanced data filtering with complex conditions and predicates
 * 
 * Provides comprehensive filtering operations including complex predicates,
 * multi-condition filtering, and advanced query-like filtering capabilities.
 */
class FilterOperator extends CoreOperator
{
    public function getName(): string
    {
        return 'filter';
    }

    public function getDescription(): string 
    {
        return 'Advanced data filtering with complex conditions and predicates';
    }

    public function getSupportedActions(): array
    {
        return [
            'where', 'where_in', 'where_not_in', 'where_between', 'where_like',
            'where_regex', 'where_null', 'where_not_null', 'where_exists',
            'and_where', 'or_where', 'complex_filter', 'multi_filter',
            'date_filter', 'range_filter', 'fuzzy_filter', 'exclude'
        ];
    }

    public function execute(string $action, array $params = []): mixed
    {
        return match($action) {
            'where' => $this->where($params['data'] ?? [], $params['field'] ?? '', $params['operator'] ?? '==', $params['value'] ?? null),
            'where_in' => $this->whereIn($params['data'] ?? [], $params['field'] ?? '', $params['values'] ?? []),
            'where_not_in' => $this->whereNotIn($params['data'] ?? [], $params['field'] ?? '', $params['values'] ?? []),
            'where_between' => $this->whereBetween($params['data'] ?? [], $params['field'] ?? '', $params['min'] ?? null, $params['max'] ?? null),
            'where_like' => $this->whereLike($params['data'] ?? [], $params['field'] ?? '', $params['pattern'] ?? ''),
            'where_regex' => $this->whereRegex($params['data'] ?? [], $params['field'] ?? '', $params['pattern'] ?? ''),
            'where_null' => $this->whereNull($params['data'] ?? [], $params['field'] ?? ''),
            'where_not_null' => $this->whereNotNull($params['data'] ?? [], $params['field'] ?? ''),
            'where_exists' => $this->whereExists($params['data'] ?? [], $params['field'] ?? ''),
            'and_where' => $this->andWhere($params['data'] ?? [], $params['conditions'] ?? []),
            'or_where' => $this->orWhere($params['data'] ?? [], $params['conditions'] ?? []),
            'complex_filter' => $this->complexFilter($params['data'] ?? [], $params['filter'] ?? []),
            'multi_filter' => $this->multiFilter($params['data'] ?? [], $params['filters'] ?? []),
            'date_filter' => $this->dateFilter($params['data'] ?? [], $params['field'] ?? '', $params['date_condition'] ?? []),
            'range_filter' => $this->rangeFilter($params['data'] ?? [], $params['ranges'] ?? []),
            'fuzzy_filter' => $this->fuzzyFilter($params['data'] ?? [], $params['field'] ?? '', $params['search'] ?? '', $params['threshold'] ?? 0.6),
            'exclude' => $this->exclude($params['data'] ?? [], $params['conditions'] ?? []),
            default => throw new InvalidArgumentException("Unsupported action: {$action}")
        };
    }

    /**
     * Basic where filter with operator support
     */
    private function where(array $data, string $field, string $operator, mixed $value): array
    {
        if (empty($field)) {
            throw new InvalidArgumentException('Field name cannot be empty');
        }

        return array_filter($data, function($item) use ($field, $operator, $value) {
            $fieldValue = $this->getFieldValue($item, $field);
            return $this->evaluateCondition($fieldValue, $operator, $value);
        });
    }

    /**
     * Filter where field value is in array
     */
    private function whereIn(array $data, string $field, array $values): array
    {
        if (empty($field)) {
            throw new InvalidArgumentException('Field name cannot be empty');
        }

        if (empty($values)) {
            return [];
        }

        return array_filter($data, function($item) use ($field, $values) {
            $fieldValue = $this->getFieldValue($item, $field);
            return in_array($fieldValue, $values, true);
        });
    }

    /**
     * Filter where field value is not in array
     */
    private function whereNotIn(array $data, string $field, array $values): array
    {
        if (empty($field)) {
            throw new InvalidArgumentException('Field name cannot be empty');
        }

        return array_filter($data, function($item) use ($field, $values) {
            $fieldValue = $this->getFieldValue($item, $field);
            return !in_array($fieldValue, $values, true);
        });
    }

    /**
     * Filter where field value is between two values
     */
    private function whereBetween(array $data, string $field, mixed $min, mixed $max): array
    {
        if (empty($field)) {
            throw new InvalidArgumentException('Field name cannot be empty');
        }

        if ($min === null || $max === null) {
            throw new InvalidArgumentException('Min and max values must be provided');
        }

        return array_filter($data, function($item) use ($field, $min, $max) {
            $fieldValue = $this->getFieldValue($item, $field);
            return $fieldValue >= $min && $fieldValue <= $max;
        });
    }

    /**
     * Filter where field value matches pattern (LIKE)
     */
    private function whereLike(array $data, string $field, string $pattern): array
    {
        if (empty($field)) {
            throw new InvalidArgumentException('Field name cannot be empty');
        }

        return array_filter($data, function($item) use ($field, $pattern) {
            $fieldValue = (string) $this->getFieldValue($item, $field);
            return $this->matchLikePattern($fieldValue, $pattern);
        });
    }

    /**
     * Filter where field value matches regex pattern
     */
    private function whereRegex(array $data, string $field, string $pattern): array
    {
        if (empty($field)) {
            throw new InvalidArgumentException('Field name cannot be empty');
        }

        if (empty($pattern)) {
            throw new InvalidArgumentException('Regex pattern cannot be empty');
        }

        return array_filter($data, function($item) use ($field, $pattern) {
            $fieldValue = (string) $this->getFieldValue($item, $field);
            return preg_match($pattern, $fieldValue) === 1;
        });
    }

    /**
     * Filter where field value is null
     */
    private function whereNull(array $data, string $field): array
    {
        if (empty($field)) {
            throw new InvalidArgumentException('Field name cannot be empty');
        }

        return array_filter($data, function($item) use ($field) {
            $fieldValue = $this->getFieldValue($item, $field);
            return $fieldValue === null;
        });
    }

    /**
     * Filter where field value is not null
     */
    private function whereNotNull(array $data, string $field): array
    {
        if (empty($field)) {
            throw new InvalidArgumentException('Field name cannot be empty');
        }

        return array_filter($data, function($item) use ($field) {
            $fieldValue = $this->getFieldValue($item, $field);
            return $fieldValue !== null;
        });
    }

    /**
     * Filter where field exists (key exists)
     */
    private function whereExists(array $data, string $field): array
    {
        if (empty($field)) {
            throw new InvalidArgumentException('Field name cannot be empty');
        }

        return array_filter($data, function($item) use ($field) {
            return $this->fieldExists($item, $field);
        });
    }

    /**
     * Apply multiple AND conditions
     */
    private function andWhere(array $data, array $conditions): array
    {
        if (empty($conditions)) {
            return $data;
        }

        return array_filter($data, function($item) use ($conditions) {
            foreach ($conditions as $condition) {
                if (!$this->evaluateFilterCondition($item, $condition)) {
                    return false;
                }
            }
            return true;
        });
    }

    /**
     * Apply multiple OR conditions
     */
    private function orWhere(array $data, array $conditions): array
    {
        if (empty($conditions)) {
            return $data;
        }

        return array_filter($data, function($item) use ($conditions) {
            foreach ($conditions as $condition) {
                if ($this->evaluateFilterCondition($item, $condition)) {
                    return true;
                }
            }
            return false;
        });
    }

    /**
     * Apply complex nested filter conditions
     */
    private function complexFilter(array $data, array $filter): array
    {
        if (empty($filter)) {
            return $data;
        }

        return array_filter($data, function($item) use ($filter) {
            return $this->evaluateComplexFilter($item, $filter);
        });
    }

    /**
     * Apply multiple filters in sequence
     */
    private function multiFilter(array $data, array $filters): array
    {
        $result = $data;

        foreach ($filters as $filter) {
            $result = $this->complexFilter($result, $filter);
        }

        return $result;
    }

    /**
     * Filter by date conditions
     */
    private function dateFilter(array $data, string $field, array $dateCondition): array
    {
        if (empty($field)) {
            throw new InvalidArgumentException('Field name cannot be empty');
        }

        return array_filter($data, function($item) use ($field, $dateCondition) {
            $fieldValue = $this->getFieldValue($item, $field);
            $date = $this->parseDate($fieldValue);
            
            if (!$date) {
                return false;
            }

            return $this->evaluateDateCondition($date, $dateCondition);
        });
    }

    /**
     * Filter by multiple range conditions
     */
    private function rangeFilter(array $data, array $ranges): array
    {
        return array_filter($data, function($item) use ($ranges) {
            foreach ($ranges as $field => $range) {
                $fieldValue = $this->getFieldValue($item, $field);
                
                if (!is_numeric($fieldValue)) {
                    return false;
                }

                $min = $range['min'] ?? null;
                $max = $range['max'] ?? null;

                if ($min !== null && $fieldValue < $min) {
                    return false;
                }

                if ($max !== null && $fieldValue > $max) {
                    return false;
                }
            }
            
            return true;
        });
    }

    /**
     * Fuzzy text matching filter
     */
    private function fuzzyFilter(array $data, string $field, string $search, float $threshold = 0.6): array
    {
        if (empty($field) || empty($search)) {
            throw new InvalidArgumentException('Field and search term cannot be empty');
        }

        return array_filter($data, function($item) use ($field, $search, $threshold) {
            $fieldValue = (string) $this->getFieldValue($item, $field);
            return $this->calculateSimilarity($fieldValue, $search) >= $threshold;
        });
    }

    /**
     * Exclude items matching conditions
     */
    private function exclude(array $data, array $conditions): array
    {
        return array_filter($data, function($item) use ($conditions) {
            foreach ($conditions as $condition) {
                if ($this->evaluateFilterCondition($item, $condition)) {
                    return false; // Exclude this item
                }
            }
            return true; // Keep this item
        });
    }

    /**
     * Get field value with dot notation support
     */
    private function getFieldValue(array|object $item, string $field): mixed
    {
        if (empty($field)) {
            return $item;
        }

        $keys = explode('.', $field);
        $current = $item;

        foreach ($keys as $key) {
            if (is_array($current) && array_key_exists($key, $current)) {
                $current = $current[$key];
            } elseif (is_object($current) && property_exists($current, $key)) {
                $current = $current->$key;
            } else {
                return null;
            }
        }

        return $current;
    }

    /**
     * Check if field exists
     */
    private function fieldExists(array|object $item, string $field): bool
    {
        if (empty($field)) {
            return true;
        }

        $keys = explode('.', $field);
        $current = $item;

        foreach ($keys as $key) {
            if (is_array($current)) {
                if (!array_key_exists($key, $current)) {
                    return false;
                }
                $current = $current[$key];
            } elseif (is_object($current)) {
                if (!property_exists($current, $key)) {
                    return false;
                }
                $current = $current->$key;
            } else {
                return false;
            }
        }

        return true;
    }

    /**
     * Evaluate condition with operator
     */
    private function evaluateCondition(mixed $fieldValue, string $operator, mixed $value): bool
    {
        return match($operator) {
            '==', '=' => $fieldValue == $value,
            '===' => $fieldValue === $value,
            '!=', '<>' => $fieldValue != $value,
            '!==' => $fieldValue !== $value,
            '>' => $fieldValue > $value,
            '>=' => $fieldValue >= $value,
            '<' => $fieldValue < $value,
            '<=' => $fieldValue <= $value,
            'like' => $this->matchLikePattern((string) $fieldValue, (string) $value),
            'not_like' => !$this->matchLikePattern((string) $fieldValue, (string) $value),
            'regex' => preg_match((string) $value, (string) $fieldValue) === 1,
            'contains' => is_string($fieldValue) && str_contains($fieldValue, (string) $value),
            'starts_with' => is_string($fieldValue) && str_starts_with($fieldValue, (string) $value),
            'ends_with' => is_string($fieldValue) && str_ends_with($fieldValue, (string) $value),
            'in' => is_array($value) && in_array($fieldValue, $value, true),
            'not_in' => is_array($value) && !in_array($fieldValue, $value, true),
            'between' => is_array($value) && count($value) >= 2 && $fieldValue >= $value[0] && $fieldValue <= $value[1],
            'is_null' => $fieldValue === null,
            'is_not_null' => $fieldValue !== null,
            default => throw new InvalidArgumentException("Unsupported operator: {$operator}")
        };
    }

    /**
     * Evaluate filter condition array
     */
    private function evaluateFilterCondition(array|object $item, array $condition): bool
    {
        $field = $condition['field'] ?? '';
        $operator = $condition['operator'] ?? '==';
        $value = $condition['value'] ?? null;

        $fieldValue = $this->getFieldValue($item, $field);
        return $this->evaluateCondition($fieldValue, $operator, $value);
    }

    /**
     * Evaluate complex nested filter
     */
    private function evaluateComplexFilter(array|object $item, array $filter): bool
    {
        if (isset($filter['and'])) {
            foreach ($filter['and'] as $condition) {
                if (!$this->evaluateComplexFilter($item, $condition)) {
                    return false;
                }
            }
            return true;
        }

        if (isset($filter['or'])) {
            foreach ($filter['or'] as $condition) {
                if ($this->evaluateComplexFilter($item, $condition)) {
                    return true;
                }
            }
            return false;
        }

        if (isset($filter['not'])) {
            return !$this->evaluateComplexFilter($item, $filter['not']);
        }

        // Simple condition
        return $this->evaluateFilterCondition($item, $filter);
    }

    /**
     * Match LIKE pattern (% wildcards)
     */
    private function matchLikePattern(string $text, string $pattern): bool
    {
        // Convert LIKE pattern to regex
        $regexPattern = '/^' . str_replace(
            ['%', '_'],
            ['.*', '.'],
            preg_quote($pattern, '/')
        ) . '$/i';

        return preg_match($regexPattern, $text) === 1;
    }

    /**
     * Parse date string to timestamp
     */
    private function parseDate(mixed $date): ?int
    {
        if (is_numeric($date)) {
            return (int) $date;
        }

        if (is_string($date)) {
            $timestamp = strtotime($date);
            return $timestamp !== false ? $timestamp : null;
        }

        return null;
    }

    /**
     * Evaluate date condition
     */
    private function evaluateDateCondition(int $timestamp, array $condition): bool
    {
        $operator = $condition['operator'] ?? '==';
        $value = $condition['value'] ?? null;

        if (is_string($value)) {
            $value = strtotime($value);
        }

        if (!is_numeric($value)) {
            return false;
        }

        return $this->evaluateCondition($timestamp, $operator, (int) $value);
    }

    /**
     * Calculate string similarity for fuzzy matching
     */
    private function calculateSimilarity(string $str1, string $str2): float
    {
        $str1 = strtolower($str1);
        $str2 = strtolower($str2);

        if ($str1 === $str2) {
            return 1.0;
        }

        // Use Levenshtein distance for similarity
        $maxLength = max(strlen($str1), strlen($str2));
        if ($maxLength === 0) {
            return 1.0;
        }

        $distance = levenshtein($str1, $str2);
        return 1.0 - ($distance / $maxLength);
    }
} 