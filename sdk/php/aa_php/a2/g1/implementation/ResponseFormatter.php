<?php

namespace TuskLang\Communication\Http;

/**
 * Response formatter for different content types
 */
class ResponseFormatter
{
    private array $config;

    public function __construct(array $config = [])
    {
        $this->config = $config;
    }

    /**
     * Format data based on content type
     */
    public function format(mixed $data, string $contentType): string
    {
        switch (strtolower($contentType)) {
            case 'application/json':
                return $this->formatJson($data);
                
            case 'application/xml':
            case 'text/xml':
                return $this->formatXml($data);
                
            case 'text/html':
                return $this->formatHtml($data);
                
            case 'text/plain':
                return $this->formatText($data);
                
            case 'text/csv':
                return $this->formatCsv($data);
                
            case 'application/yaml':
            case 'text/yaml':
                return $this->formatYaml($data);
                
            default:
                return $this->formatJson($data);
        }
    }

    /**
     * Format as JSON
     */
    private function formatJson(mixed $data): string
    {
        $options = JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES;
        
        if ($this->config['debug'] ?? false) {
            $options |= JSON_PRETTY_PRINT;
        }
        
        return json_encode($data, $options);
    }

    /**
     * Format as XML
     */
    private function formatXml(mixed $data, string $root = 'response'): string
    {
        $xml = new \SimpleXMLElement("<?xml version=\"1.0\" encoding=\"UTF-8\"?><{$root}></{$root}>");
        $this->arrayToXml($data, $xml);
        
        $dom = dom_import_simplexml($xml)->ownerDocument;
        $dom->formatOutput = true;
        
        return $dom->saveXML();
    }

    /**
     * Format as HTML
     */
    private function formatHtml(mixed $data): string
    {
        if (is_string($data)) {
            return $data;
        }
        
        $html = '<!DOCTYPE html><html><head>';
        $html .= '<title>API Response</title>';
        $html .= '<style>body{font-family:Arial,sans-serif;margin:20px;}pre{background:#f4f4f4;padding:10px;border-radius:4px;overflow:auto;}</style>';
        $html .= '</head><body>';
        $html .= '<h1>API Response</h1>';
        $html .= '<pre>' . htmlspecialchars(json_encode($data, JSON_PRETTY_PRINT)) . '</pre>';
        $html .= '</body></html>';
        
        return $html;
    }

    /**
     * Format as plain text
     */
    private function formatText(mixed $data): string
    {
        if (is_string($data)) {
            return $data;
        }
        
        return print_r($data, true);
    }

    /**
     * Format as CSV
     */
    private function formatCsv(mixed $data): string
    {
        if (!is_array($data)) {
            return (string) $data;
        }
        
        $output = fopen('php://temp', 'r+');
        
        // Handle array of objects/arrays
        if (isset($data[0]) && (is_array($data[0]) || is_object($data[0]))) {
            $first = (array) $data[0];
            fputcsv($output, array_keys($first));
            
            foreach ($data as $row) {
                fputcsv($output, array_values((array) $row));
            }
        } else {
            // Handle single object/array
            fputcsv($output, array_keys((array) $data));
            fputcsv($output, array_values((array) $data));
        }
        
        rewind($output);
        $csv = stream_get_contents($output);
        fclose($output);
        
        return $csv;
    }

    /**
     * Format as YAML
     */
    private function formatYaml(mixed $data): string
    {
        if (function_exists('yaml_emit')) {
            return yaml_emit($data);
        }
        
        // Fallback simple YAML conversion
        return $this->arrayToYaml($data);
    }

    /**
     * Convert array to XML elements
     */
    private function arrayToXml(mixed $data, \SimpleXMLElement $xml): void
    {
        if (is_array($data)) {
            foreach ($data as $key => $value) {
                if (is_numeric($key)) {
                    $key = 'item';
                }
                
                $key = $this->sanitizeXmlName($key);
                
                if (is_array($value) || is_object($value)) {
                    $child = $xml->addChild($key);
                    $this->arrayToXml($value, $child);
                } else {
                    $xml->addChild($key, htmlspecialchars((string) $value));
                }
            }
        } elseif (is_object($data)) {
            $this->arrayToXml((array) $data, $xml);
        } else {
            $xml[0] = htmlspecialchars((string) $data);
        }
    }

    /**
     * Sanitize XML element names
     */
    private function sanitizeXmlName(string $name): string
    {
        // Remove invalid characters
        $name = preg_replace('/[^a-zA-Z0-9_-]/', '_', $name);
        
        // Ensure it doesn't start with a number
        if (preg_match('/^[0-9]/', $name)) {
            $name = 'item_' . $name;
        }
        
        return $name ?: 'item';
    }

    /**
     * Simple YAML conversion
     */
    private function arrayToYaml(mixed $data, int $indent = 0): string
    {
        $yaml = '';
        $prefix = str_repeat('  ', $indent);
        
        if (is_array($data)) {
            if (array_keys($data) === range(0, count($data) - 1)) {
                // Indexed array
                foreach ($data as $value) {
                    $yaml .= $prefix . '- ';
                    if (is_array($value) || is_object($value)) {
                        $yaml .= "\n" . $this->arrayToYaml($value, $indent + 1);
                    } else {
                        $yaml .= $this->yamlValue($value) . "\n";
                    }
                }
            } else {
                // Associative array
                foreach ($data as $key => $value) {
                    $yaml .= $prefix . $key . ': ';
                    if (is_array($value) || is_object($value)) {
                        $yaml .= "\n" . $this->arrayToYaml($value, $indent + 1);
                    } else {
                        $yaml .= $this->yamlValue($value) . "\n";
                    }
                }
            }
        } elseif (is_object($data)) {
            return $this->arrayToYaml((array) $data, $indent);
        } else {
            $yaml .= $prefix . $this->yamlValue($data) . "\n";
        }
        
        return $yaml;
    }

    /**
     * Format value for YAML
     */
    private function yamlValue(mixed $value): string
    {
        if (is_null($value)) {
            return 'null';
        }
        
        if (is_bool($value)) {
            return $value ? 'true' : 'false';
        }
        
        if (is_string($value) && (strpos($value, "\n") !== false || strpos($value, '"') !== false)) {
            return '"' . str_replace('"', '\\"', $value) . '"';
        }
        
        return (string) $value;
    }
} 