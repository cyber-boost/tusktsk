<?php

namespace TuskLang\CoreOperators;

use TuskLang\CoreOperators\BaseOperator;

/**
 * CSV Operator for parsing, generating, and manipulating CSV data
 */
class CsvOperator extends BaseOperator
{
    public function execute(array $config, array $context = []): mixed
    {
        $action = $config['action'] ?? 'parse';
        $data = $config['data'] ?? '';
        $options = $config['options'] ?? [];

        try {
            switch ($action) {
                case 'parse':
                    return $this->parseCsv($data, $options);
                case 'generate':
                    return $this->generateCsv($data, $options);
                case 'validate':
                    return $this->validateCsv($data, $options);
                case 'filter':
                    return $this->filterCsv($data, $options);
                case 'sort':
                    return $this->sortCsv($data, $options);
                case 'merge':
                    return $this->mergeCsv($data, $options);
                case 'transform':
                    return $this->transformCsv($data, $options);
                case 'stats':
                    return $this->getCsvStats($data, $options);
                default:
                    throw new \Exception("Unknown CSV action: $action");
            }
        } catch (\Exception $e) {
            error_log("CSV Operator error: " . $e->getMessage());
            throw $e;
        }
    }

    /**
     * Parse CSV string to array
     */
    private function parseCsv(string $csvString, array $options): array
    {
        $delimiter = $options['delimiter'] ?? ',';
        $enclosure = $options['enclosure'] ?? '"';
        $escape = $options['escape'] ?? '\\';
        $hasHeader = $options['hasHeader'] ?? true;
        $encoding = $options['encoding'] ?? 'UTF-8';

        // Handle encoding
        if ($encoding !== 'UTF-8') {
            $csvString = mb_convert_encoding($csvString, 'UTF-8', $encoding);
        }

        $lines = explode("\n", $csvString);
        $result = [];
        $headers = null;

        foreach ($lines as $lineNum => $line) {
            $line = trim($line);
            if (empty($line)) {
                continue;
            }

            // Parse CSV line
            $row = str_getcsv($line, $delimiter, $enclosure, $escape);
            
            if ($hasHeader && $headers === null) {
                $headers = $row;
                continue;
            }

            if ($headers !== null) {
                // Associate with headers
                $assocRow = [];
                foreach ($headers as $i => $header) {
                    $assocRow[$header] = $row[$i] ?? null;
                }
                $result[] = $assocRow;
            } else {
                // Numeric keys
                $result[] = $row;
            }
        }

        return $result;
    }

    /**
     * Generate CSV from array
     */
    private function generateCsv(array $data, array $options): string
    {
        $delimiter = $options['delimiter'] ?? ',';
        $enclosure = $options['enclosure'] ?? '"';
        $escape = $options['escape'] ?? '\\';
        $includeHeader = $options['includeHeader'] ?? true;
        $encoding = $options['encoding'] ?? 'UTF-8';

        if (empty($data)) {
            return '';
        }

        $output = '';
        
        // Add header if requested
        if ($includeHeader && !empty($data)) {
            $headers = array_keys($data[0]);
            $output .= $this->arrayToCsvLine($headers, $delimiter, $enclosure, $escape) . "\n";
        }

        // Add data rows
        foreach ($data as $row) {
            $output .= $this->arrayToCsvLine($row, $delimiter, $enclosure, $escape) . "\n";
        }

        // Handle encoding
        if ($encoding !== 'UTF-8') {
            $output = mb_convert_encoding($output, $encoding, 'UTF-8');
        }

        return $output;
    }

    /**
     * Validate CSV structure
     */
    private function validateCsv(string $csvString, array $options): array
    {
        $delimiter = $options['delimiter'] ?? ',';
        $enclosure = $options['enclosure'] ?? '"';
        $escape = $options['escape'] ?? '\\';
        $requiredColumns = $options['requiredColumns'] ?? [];
        $columnTypes = $options['columnTypes'] ?? [];
        $maxRows = $options['maxRows'] ?? null;

        try {
            $parsed = $this->parseCsv($csvString, $options);
            
            $errors = [];
            $warnings = [];
            
            if (empty($parsed)) {
                $errors[] = "CSV is empty or contains no valid data";
                return [
                    'valid' => false,
                    'errors' => $errors,
                    'warnings' => $warnings,
                    'rowCount' => 0,
                    'columnCount' => 0
                ];
            }

            $rowCount = count($parsed);
            $columnCount = count($parsed[0]);
            
            // Check row limit
            if ($maxRows !== null && $rowCount > $maxRows) {
                $warnings[] = "CSV exceeds maximum row limit ($maxRows), found $rowCount rows";
            }

            // Check required columns
            if (!empty($requiredColumns)) {
                $headers = array_keys($parsed[0]);
                foreach ($requiredColumns as $requiredCol) {
                    if (!in_array($requiredCol, $headers)) {
                        $errors[] = "Required column '$requiredCol' is missing";
                    }
                }
            }

            // Check column types
            if (!empty($columnTypes)) {
                foreach ($parsed as $rowNum => $row) {
                    foreach ($columnTypes as $column => $expectedType) {
                        if (isset($row[$column])) {
                            $value = $row[$column];
                            $actualType = $this->getValueType($value);
                            
                            if ($actualType !== $expectedType) {
                                $errors[] = "Row " . ($rowNum + 1) . ", Column '$column': expected $expectedType, got $actualType";
                            }
                        }
                    }
                }
            }

            // Check for empty rows
            $emptyRows = 0;
            foreach ($parsed as $row) {
                if (empty(array_filter($row, fn($v) => $v !== null && $v !== ''))) {
                    $emptyRows++;
                }
            }
            
            if ($emptyRows > 0) {
                $warnings[] = "Found $emptyRows empty rows";
            }

            return [
                'valid' => empty($errors),
                'errors' => $errors,
                'warnings' => $warnings,
                'rowCount' => $rowCount,
                'columnCount' => $columnCount,
                'emptyRows' => $emptyRows
            ];
        } catch (\Exception $e) {
            return [
                'valid' => false,
                'errors' => [$e->getMessage()],
                'warnings' => [],
                'rowCount' => 0,
                'columnCount' => 0
            ];
        }
    }

    /**
     * Filter CSV data
     */
    private function filterCsv(string $csvString, array $options): string
    {
        $parsed = $this->parseCsv($csvString, $options);
        $filters = $options['filters'] ?? [];
        $columns = $options['columns'] ?? null;

        $filtered = [];
        
        foreach ($parsed as $row) {
            $include = true;
            
            // Apply filters
            foreach ($filters as $filter) {
                $column = $filter['column'] ?? '';
                $operator = $filter['operator'] ?? 'equals';
                $value = $filter['value'] ?? null;
                
                if (!isset($row[$column])) {
                    $include = false;
                    break;
                }
                
                $cellValue = $row[$column];
                
                switch ($operator) {
                    case 'equals':
                        if ($cellValue != $value) $include = false;
                        break;
                    case 'not_equals':
                        if ($cellValue == $value) $include = false;
                        break;
                    case 'contains':
                        if (strpos($cellValue, $value) === false) $include = false;
                        break;
                    case 'not_contains':
                        if (strpos($cellValue, $value) !== false) $include = false;
                        break;
                    case 'greater_than':
                        if (!is_numeric($cellValue) || $cellValue <= $value) $include = false;
                        break;
                    case 'less_than':
                        if (!is_numeric($cellValue) || $cellValue >= $value) $include = false;
                        break;
                    case 'regex':
                        if (!preg_match($value, $cellValue)) $include = false;
                        break;
                }
                
                if (!$include) break;
            }
            
            if ($include) {
                if ($columns !== null) {
                    // Select specific columns
                    $filteredRow = [];
                    foreach ($columns as $col) {
                        $filteredRow[$col] = $row[$col] ?? null;
                    }
                    $filtered[] = $filteredRow;
                } else {
                    $filtered[] = $row;
                }
            }
        }

        return $this->generateCsv($filtered, $options);
    }

    /**
     * Sort CSV data
     */
    private function sortCsv(string $csvString, array $options): string
    {
        $parsed = $this->parseCsv($csvString, $options);
        $sortBy = $options['sortBy'] ?? [];
        $direction = $options['direction'] ?? 'asc';

        if (empty($sortBy)) {
            return $csvString; // No sorting specified
        }

        usort($parsed, function($a, $b) use ($sortBy, $direction) {
            foreach ($sortBy as $column) {
                $aVal = $a[$column] ?? '';
                $bVal = $b[$column] ?? '';
                
                $comparison = $this->compareValues($aVal, $bVal);
                if ($comparison !== 0) {
                    return $direction === 'desc' ? -$comparison : $comparison;
                }
            }
            return 0;
        });

        return $this->generateCsv($parsed, $options);
    }

    /**
     * Merge multiple CSV files
     */
    private function mergeCsv(array $csvStrings, array $options): string
    {
        $strategy = $options['strategy'] ?? 'append'; // append, union, intersection
        $keyColumn = $options['keyColumn'] ?? null;
        $includeSource = $options['includeSource'] ?? false;

        if (empty($csvStrings)) {
            return '';
        }

        $merged = [];
        $sourceIndex = 0;

        foreach ($csvStrings as $csvString) {
            $parsed = $this->parseCsv($csvString, $options);
            
            if ($strategy === 'append') {
                // Simple append
                foreach ($parsed as $row) {
                    if ($includeSource) {
                        $row['_source'] = $sourceIndex;
                    }
                    $merged[] = $row;
                }
            } elseif ($strategy === 'union' && $keyColumn !== null) {
                // Union by key column
                foreach ($parsed as $row) {
                    $key = $row[$keyColumn] ?? null;
                    if ($key !== null) {
                        if ($includeSource) {
                            $row['_source'] = $sourceIndex;
                        }
                        $merged[$key] = $row;
                    }
                }
            } elseif ($strategy === 'intersection' && $keyColumn !== null) {
                // Intersection by key column
                if ($sourceIndex === 0) {
                    foreach ($parsed as $row) {
                        $key = $row[$keyColumn] ?? null;
                        if ($key !== null) {
                            $merged[$key] = $row;
                        }
                    }
                } else {
                    $intersection = [];
                    foreach ($parsed as $row) {
                        $key = $row[$keyColumn] ?? null;
                        if ($key !== null && isset($merged[$key])) {
                            $intersection[$key] = $row;
                        }
                    }
                    $merged = $intersection;
                }
            }
            
            $sourceIndex++;
        }

        if ($strategy === 'union' || $strategy === 'intersection') {
            $merged = array_values($merged);
        }

        return $this->generateCsv($merged, $options);
    }

    /**
     * Transform CSV data
     */
    private function transformCsv(string $csvString, array $options): string
    {
        $parsed = $this->parseCsv($csvString, $options);
        $transforms = $options['transforms'] ?? [];

        foreach ($transforms as $transform) {
            $type = $transform['type'] ?? '';
            $column = $transform['column'] ?? '';
            $value = $transform['value'] ?? null;
            
            switch ($type) {
                case 'rename':
                    $newName = $transform['newName'] ?? '';
                    if (!empty($newName)) {
                        foreach ($parsed as &$row) {
                            if (isset($row[$column])) {
                                $row[$newName] = $row[$column];
                                unset($row[$column]);
                            }
                        }
                    }
                    break;
                    
                case 'set':
                    foreach ($parsed as &$row) {
                        $row[$column] = $value;
                    }
                    break;
                    
                case 'calculate':
                    $formula = $transform['formula'] ?? '';
                    if (!empty($formula)) {
                        foreach ($parsed as &$row) {
                            $row[$column] = $this->evaluateFormula($formula, $row);
                        }
                    }
                    break;
                    
                case 'format':
                    $format = $transform['format'] ?? '';
                    if (!empty($format)) {
                        foreach ($parsed as &$row) {
                            if (isset($row[$column])) {
                                $row[$column] = $this->formatValue($row[$column], $format);
                            }
                        }
                    }
                    break;
            }
        }

        return $this->generateCsv($parsed, $options);
    }

    /**
     * Get CSV statistics
     */
    private function getCsvStats(string $csvString, array $options): array
    {
        $parsed = $this->parseCsv($csvString, $options);
        
        if (empty($parsed)) {
            return [
                'rowCount' => 0,
                'columnCount' => 0,
                'columns' => [],
                'stats' => []
            ];
        }

        $rowCount = count($parsed);
        $columnCount = count($parsed[0]);
        $columns = array_keys($parsed[0]);
        $stats = [];

        foreach ($columns as $column) {
            $values = array_column($parsed, $column);
            $nonEmptyValues = array_filter($values, fn($v) => $v !== null && $v !== '');
            
            $stats[$column] = [
                'total' => count($values),
                'nonEmpty' => count($nonEmptyValues),
                'empty' => count($values) - count($nonEmptyValues),
                'unique' => count(array_unique($nonEmptyValues)),
                'type' => $this->getColumnType($nonEmptyValues)
            ];

            // Numeric statistics
            $numericValues = array_filter($nonEmptyValues, 'is_numeric');
            if (!empty($numericValues)) {
                $stats[$column]['numeric'] = [
                    'min' => min($numericValues),
                    'max' => max($numericValues),
                    'sum' => array_sum($numericValues),
                    'avg' => array_sum($numericValues) / count($numericValues),
                    'count' => count($numericValues)
                ];
            }
        }

        return [
            'rowCount' => $rowCount,
            'columnCount' => $columnCount,
            'columns' => $columns,
            'stats' => $stats
        ];
    }

    /**
     * Convert array to CSV line
     */
    private function arrayToCsvLine(array $row, string $delimiter, string $enclosure, string $escape): string
    {
        $line = '';
        foreach ($row as $value) {
            if ($line !== '') {
                $line .= $delimiter;
            }
            
            if (strpos($value, $delimiter) !== false || strpos($value, $enclosure) !== false || strpos($value, "\n") !== false) {
                $value = $enclosure . str_replace($enclosure, $escape . $enclosure, $value) . $enclosure;
            }
            
            $line .= $value;
        }
        
        return $line;
    }

    /**
     * Get value type
     */
    private function getValueType($value): string
    {
        if (is_numeric($value)) {
            return is_float($value) ? 'float' : 'integer';
        } elseif (is_bool($value)) {
            return 'boolean';
        } elseif (is_null($value)) {
            return 'null';
        } else {
            return 'string';
        }
    }

    /**
     * Get column type based on values
     */
    private function getColumnType(array $values): string
    {
        if (empty($values)) {
            return 'unknown';
        }

        $types = array_map([$this, 'getValueType'], $values);
        $typeCounts = array_count_values($types);
        
        // Return the most common type
        return array_keys($typeCounts, max($typeCounts))[0];
    }

    /**
     * Compare values for sorting
     */
    private function compareValues($a, $b): int
    {
        // Handle numeric comparison
        if (is_numeric($a) && is_numeric($b)) {
            return $a <=> $b;
        }
        
        // String comparison
        return strcasecmp($a, $b);
    }

    /**
     * Evaluate formula with row context
     */
    private function evaluateFormula(string $formula, array $row): mixed
    {
        // Simple formula evaluation - replace column names with values
        foreach ($row as $column => $value) {
            $formula = str_replace('{' . $column . '}', $value, $formula);
        }
        
        // Basic arithmetic evaluation (be careful with eval in production)
        try {
            return eval("return $formula;");
        } catch (\Exception $e) {
            return null;
        }
    }

    /**
     * Format value according to format string
     */
    private function formatValue($value, string $format): string
    {
        switch ($format) {
            case 'uppercase':
                return strtoupper($value);
            case 'lowercase':
                return strtolower($value);
            case 'capitalize':
                return ucfirst(strtolower($value));
            case 'title':
                return ucwords(strtolower($value));
            case 'trim':
                return trim($value);
            default:
                return $value;
        }
    }
} 