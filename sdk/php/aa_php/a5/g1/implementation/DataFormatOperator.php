<?php

declare(strict_types=1);

namespace TuskLang\A5\G1;

use TuskLang\CoreOperator;
use InvalidArgumentException;
use Symfony\Component\Yaml\Yaml;
use Symfony\Component\Yaml\Exception\ParseException;

/**
 * DataFormatOperator - Universal data format conversion (JSON↔XML↔YAML↔CSV)
 * 
 * Provides comprehensive data format conversion operations with
 * support for JSON, XML, YAML, CSV, and other common data formats.
 */
class DataFormatOperator extends CoreOperator
{
    private JsonOperator $jsonOperator;
    private XmlOperator $xmlOperator;

    public function __construct()
    {
        $this->jsonOperator = new JsonOperator();
        $this->xmlOperator = new XmlOperator();
    }

    public function getName(): string
    {
        return 'format';
    }

    public function getDescription(): string 
    {
        return 'Universal data format conversion (JSON↔XML↔YAML↔CSV)';
    }

    public function getSupportedActions(): array
    {
        return [
            'convert', 'detect', 'json_to_xml', 'xml_to_json',
            'json_to_yaml', 'yaml_to_json', 'json_to_csv', 'csv_to_json',
            'xml_to_yaml', 'yaml_to_xml', 'normalize', 'validate_format'
        ];
    }

    public function execute(string $action, array $params = []): mixed
    {
        return match($action) {
            'convert' => $this->convert($params['data'] ?? '', $params['from'] ?? '', $params['to'] ?? ''),
            'detect' => $this->detectFormat($params['data'] ?? ''),
            'json_to_xml' => $this->jsonToXml($params['json'] ?? '', $params['options'] ?? []),
            'xml_to_json' => $this->xmlToJson($params['xml'] ?? '', $params['options'] ?? []),
            'json_to_yaml' => $this->jsonToYaml($params['json'] ?? '', $params['options'] ?? []),
            'yaml_to_json' => $this->yamlToJson($params['yaml'] ?? '', $params['options'] ?? []),
            'json_to_csv' => $this->jsonToCsv($params['json'] ?? '', $params['options'] ?? []),
            'csv_to_json' => $this->csvToJson($params['csv'] ?? '', $params['options'] ?? []),
            'xml_to_yaml' => $this->xmlToYaml($params['xml'] ?? '', $params['options'] ?? []),
            'yaml_to_xml' => $this->yamlToXml($params['yaml'] ?? '', $params['options'] ?? []),
            'normalize' => $this->normalizeData($params['data'] ?? '', $params['format'] ?? ''),
            'validate_format' => $this->validateFormat($params['data'] ?? '', $params['format'] ?? ''),
            default => throw new InvalidArgumentException("Unsupported action: {$action}")
        };
    }

    /**
     * Universal format converter
     */
    private function convert(string $data, string $from, string $to): string
    {
        if (empty($data)) {
            throw new InvalidArgumentException('Data cannot be empty');
        }

        $from = strtolower($from);
        $to = strtolower($to);

        if ($from === $to) {
            return $data;
        }

        // First convert to normalized array format
        $normalized = $this->parseToArray($data, $from);
        
        // Then convert from array to target format
        return $this->formatFromArray($normalized, $to);
    }

    /**
     * Auto-detect data format
     */
    private function detectFormat(string $data): array
    {
        if (empty($data)) {
            return ['format' => 'unknown', 'confidence' => 0];
        }

        $data = trim($data);
        $confidence = 0;
        $format = 'unknown';

        // JSON detection
        if ($this->isJson($data)) {
            $format = 'json';
            $confidence = 95;
        }
        // XML detection
        elseif ($this->isXml($data)) {
            $format = 'xml';
            $confidence = 90;
        }
        // YAML detection
        elseif ($this->isYaml($data)) {
            $format = 'yaml';
            $confidence = 80;
        }
        // CSV detection
        elseif ($this->isCsv($data)) {
            $format = 'csv';
            $confidence = 70;
        }
        // INI detection
        elseif ($this->isIni($data)) {
            $format = 'ini';
            $confidence = 75;
        }

        return [
            'format' => $format,
            'confidence' => $confidence,
            'characteristics' => $this->analyzeCharacteristics($data)
        ];
    }

    /**
     * Convert JSON to XML
     */
    private function jsonToXml(string $json, array $options = []): string
    {
        $data = $this->jsonOperator->execute('parse', ['json' => $json]);
        
        $rootElement = $options['root'] ?? 'root';
        return $this->xmlOperator->execute('from_array', [
            'data' => $data,
            'options' => array_merge(['root' => $rootElement], $options)
        ]);
    }

    /**
     * Convert XML to JSON
     */
    private function xmlToJson(string $xml, array $options = []): string
    {
        $data = $this->xmlOperator->execute('to_array', ['xml' => $xml]);
        
        $prettyPrint = $options['pretty'] ?? true;
        return $this->jsonOperator->execute('stringify', [
            'data' => $data,
            'options' => ['pretty' => $prettyPrint]
        ]);
    }

    /**
     * Convert JSON to YAML
     */
    private function jsonToYaml(string $json, array $options = []): string
    {
        $data = $this->jsonOperator->execute('parse', ['json' => $json]);
        
        $inline = $options['inline'] ?? 4;
        $indent = $options['indent'] ?? 2;
        
        return Yaml::dump($data, $inline, $indent);
    }

    /**
     * Convert YAML to JSON
     */
    private function yamlToJson(string $yaml, array $options = []): string
    {
        try {
            $data = Yaml::parse($yaml);
            
            $prettyPrint = $options['pretty'] ?? true;
            return $this->jsonOperator->execute('stringify', [
                'data' => $data,
                'options' => ['pretty' => $prettyPrint]
            ]);
        } catch (ParseException $e) {
            throw new InvalidArgumentException('Invalid YAML: ' . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Convert JSON to CSV
     */
    private function jsonToCsv(string $json, array $options = []): string
    {
        $data = $this->jsonOperator->execute('parse', ['json' => $json]);
        
        if (!is_array($data)) {
            throw new InvalidArgumentException('JSON must be an array for CSV conversion');
        }

        $delimiter = $options['delimiter'] ?? ',';
        $enclosure = $options['enclosure'] ?? '"';
        $escape = $options['escape'] ?? '\\';
        $includeHeaders = $options['headers'] ?? true;

        // Handle different array structures
        if (isset($data[0]) && is_array($data[0])) {
            // Array of arrays/objects
            return $this->arrayToCsv($data, $delimiter, $enclosure, $escape, $includeHeaders);
        } else {
            // Single dimensional array
            return $this->arrayToCsv([$data], $delimiter, $enclosure, $escape, $includeHeaders);
        }
    }

    /**
     * Convert CSV to JSON
     */
    private function csvToJson(string $csv, array $options = []): string
    {
        $delimiter = $options['delimiter'] ?? ',';
        $enclosure = $options['enclosure'] ?? '"';
        $escape = $options['escape'] ?? '\\';
        $hasHeaders = $options['headers'] ?? true;

        $lines = str_getcsv($csv, "\n");
        $data = [];
        $headers = [];

        foreach ($lines as $index => $line) {
            $row = str_getcsv($line, $delimiter, $enclosure, $escape);
            
            if ($index === 0 && $hasHeaders) {
                $headers = $row;
                continue;
            }

            if ($hasHeaders && !empty($headers)) {
                $assocRow = [];
                foreach ($row as $i => $value) {
                    $key = $headers[$i] ?? "column_$i";
                    $assocRow[$key] = $value;
                }
                $data[] = $assocRow;
            } else {
                $data[] = $row;
            }
        }

        $prettyPrint = $options['pretty'] ?? true;
        return $this->jsonOperator->execute('stringify', [
            'data' => $data,
            'options' => ['pretty' => $prettyPrint]
        ]);
    }

    /**
     * Convert XML to YAML
     */
    private function xmlToYaml(string $xml, array $options = []): string
    {
        $data = $this->xmlOperator->execute('to_array', ['xml' => $xml]);
        
        $inline = $options['inline'] ?? 4;
        $indent = $options['indent'] ?? 2;
        
        return Yaml::dump($data, $inline, $indent);
    }

    /**
     * Convert YAML to XML
     */
    private function yamlToXml(string $yaml, array $options = []): string
    {
        try {
            $data = Yaml::parse($yaml);
            
            $rootElement = $options['root'] ?? 'root';
            return $this->xmlOperator->execute('from_array', [
                'data' => $data,
                'options' => array_merge(['root' => $rootElement], $options)
            ]);
        } catch (ParseException $e) {
            throw new InvalidArgumentException('Invalid YAML: ' . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Normalize data format
     */
    private function normalizeData(string $data, string $format): array
    {
        $format = strtolower($format);
        return $this->parseToArray($data, $format);
    }

    /**
     * Validate data format
     */
    private function validateFormat(string $data, string $format): array
    {
        $format = strtolower($format);
        
        try {
            switch ($format) {
                case 'json':
                    return $this->jsonOperator->execute('validate', ['json' => $data]);
                
                case 'xml':
                    return $this->xmlOperator->execute('validate', ['xml' => $data]);
                
                case 'yaml':
                    try {
                        Yaml::parse($data);
                        return ['valid' => true, 'errors' => []];
                    } catch (ParseException $e) {
                        return ['valid' => false, 'errors' => [$e->getMessage()]];
                    }
                
                case 'csv':
                    return $this->validateCsv($data);
                
                default:
                    return ['valid' => false, 'errors' => ['Unsupported format']];
            }
        } catch (\Exception $e) {
            return ['valid' => false, 'errors' => [$e->getMessage()]];
        }
    }

    /**
     * Parse data to normalized array format
     */
    private function parseToArray(string $data, string $format): array
    {
        return match(strtolower($format)) {
            'json' => $this->jsonOperator->execute('parse', ['json' => $data]),
            'xml' => $this->xmlOperator->execute('to_array', ['xml' => $data]),
            'yaml' => Yaml::parse($data),
            'csv' => $this->parseCsvToArray($data),
            'ini' => parse_ini_string($data, true) ?: [],
            default => throw new InvalidArgumentException("Unsupported source format: $format")
        };
    }

    /**
     * Format array to target format
     */
    private function formatFromArray(array $data, string $format): string
    {
        return match(strtolower($format)) {
            'json' => $this->jsonOperator->execute('stringify', ['data' => $data, 'options' => ['pretty' => true]]),
            'xml' => $this->xmlOperator->execute('from_array', ['data' => $data, 'options' => ['formatted' => true]]),
            'yaml' => Yaml::dump($data, 4, 2),
            'csv' => $this->arrayToCsv(is_array($data[0] ?? null) ? $data : [$data]),
            'ini' => $this->arrayToIni($data),
            default => throw new InvalidArgumentException("Unsupported target format: $format")
        };
    }

    /**
     * Convert array to CSV
     */
    private function arrayToCsv(array $data, string $delimiter = ',', string $enclosure = '"', string $escape = '\\', bool $includeHeaders = true): string
    {
        if (empty($data)) {
            return '';
        }

        $output = fopen('php://temp', 'w');
        
        // Write headers if needed
        if ($includeHeaders && is_array($data[0])) {
            $headers = array_keys($data[0]);
            fputcsv($output, $headers, $delimiter, $enclosure, $escape);
        }

        // Write data rows
        foreach ($data as $row) {
            if (is_array($row)) {
                fputcsv($output, array_values($row), $delimiter, $enclosure, $escape);
            } else {
                fputcsv($output, [$row], $delimiter, $enclosure, $escape);
            }
        }

        rewind($output);
        $csv = stream_get_contents($output);
        fclose($output);

        return $csv;
    }

    /**
     * Parse CSV to array
     */
    private function parseCsvToArray(string $csv): array
    {
        return $this->csvToJson($csv, ['pretty' => false]);
    }

    /**
     * Convert array to INI format
     */
    private function arrayToIni(array $data): string
    {
        $ini = '';
        
        foreach ($data as $section => $values) {
            if (is_array($values)) {
                $ini .= "[$section]\n";
                foreach ($values as $key => $value) {
                    if (is_array($value)) {
                        foreach ($value as $subKey => $subValue) {
                            $ini .= "{$key}[$subKey] = " . $this->formatIniValue($subValue) . "\n";
                        }
                    } else {
                        $ini .= "$key = " . $this->formatIniValue($value) . "\n";
                    }
                }
                $ini .= "\n";
            } else {
                $ini .= "$section = " . $this->formatIniValue($values) . "\n";
            }
        }

        return rtrim($ini);
    }

    /**
     * Format value for INI file
     */
    private function formatIniValue(mixed $value): string
    {
        if (is_bool($value)) {
            return $value ? '1' : '0';
        } elseif (is_string($value) && (str_contains($value, ' ') || str_contains($value, ';'))) {
            return '"' . $value . '"';
        }
        
        return (string) $value;
    }

    /**
     * Check if string is JSON
     */
    private function isJson(string $data): bool
    {
        $validation = $this->jsonOperator->execute('validate', ['json' => $data]);
        return $validation['valid'] ?? false;
    }

    /**
     * Check if string is XML
     */
    private function isXml(string $data): bool
    {
        $validation = $this->xmlOperator->execute('validate', ['xml' => $data]);
        return $validation['valid'] ?? false;
    }

    /**
     * Check if string is YAML
     */
    private function isYaml(string $data): bool
    {
        try {
            Yaml::parse($data);
            return true;
        } catch (ParseException $e) {
            return false;
        }
    }

    /**
     * Check if string is CSV
     */
    private function isCsv(string $data): bool
    {
        $lines = explode("\n", trim($data));
        if (count($lines) < 2) return false;

        $firstLineFields = str_getcsv($lines[0]);
        $secondLineFields = str_getcsv($lines[1]);

        return count($firstLineFields) === count($secondLineFields) && count($firstLineFields) > 1;
    }

    /**
     * Check if string is INI
     */
    private function isIni(string $data): bool
    {
        return parse_ini_string($data) !== false;
    }

    /**
     * Validate CSV format
     */
    private function validateCsv(string $csv): array
    {
        try {
            $lines = explode("\n", trim($csv));
            if (empty($lines)) {
                return ['valid' => false, 'errors' => ['Empty CSV data']];
            }

            $fieldCount = null;
            foreach ($lines as $lineNumber => $line) {
                if (empty(trim($line))) continue;
                
                $fields = str_getcsv($line);
                if ($fieldCount === null) {
                    $fieldCount = count($fields);
                } elseif (count($fields) !== $fieldCount) {
                    return [
                        'valid' => false,
                        'errors' => ["Inconsistent field count at line " . ($lineNumber + 1)]
                    ];
                }
            }

            return ['valid' => true, 'errors' => []];
        } catch (\Exception $e) {
            return ['valid' => false, 'errors' => [$e->getMessage()]];
        }
    }

    /**
     * Analyze data characteristics
     */
    private function analyzeCharacteristics(string $data): array
    {
        return [
            'size' => strlen($data),
            'lines' => substr_count($data, "\n") + 1,
            'has_brackets' => str_contains($data, '{') || str_contains($data, '['),
            'has_tags' => str_contains($data, '<') && str_contains($data, '>'),
            'has_colons' => str_contains($data, ':'),
            'has_commas' => str_contains($data, ','),
            'has_quotes' => str_contains($data, '"') || str_contains($data, "'"),
            'indented' => preg_match('/^\s+/m', $data) === 1
        ];
    }
} 