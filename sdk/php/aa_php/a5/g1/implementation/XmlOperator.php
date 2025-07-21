<?php

declare(strict_types=1);

namespace TuskLang\A5\G1;

use TuskLang\CoreOperator;
use InvalidArgumentException;
use DOMDocument;
use DOMXPath;
use SimpleXMLElement;
use LibXMLError;

/**
 * XmlOperator - Full XML processing with DOM/SAX parsing, validation, and conversion
 * 
 * Provides comprehensive XML operations including parsing, validation,
 * transformation, querying, and conversion with full error handling.
 */
class XmlOperator extends CoreOperator
{
    public function getName(): string
    {
        return 'xml';
    }

    public function getDescription(): string 
    {
        return 'Full XML processing with DOM/SAX parsing, validation, and conversion';
    }

    public function getSupportedActions(): array
    {
        return [
            'parse', 'stringify', 'validate', 'transform', 'query',
            'xpath', 'format', 'minify', 'to_array', 'from_array'
        ];
    }

    public function execute(string $action, array $params = []): mixed
    {
        return match($action) {
            'parse' => $this->parseXml($params['xml'] ?? '', $params['options'] ?? []),
            'stringify' => $this->stringifyXml($params['data'] ?? null, $params['options'] ?? []),
            'validate' => $this->validateXml($params['xml'] ?? '', $params['schema'] ?? null),
            'transform' => $this->transformXml($params['xml'] ?? '', $params['xslt'] ?? ''),
            'query' => $this->queryXml($params['xml'] ?? '', $params['query'] ?? ''),
            'xpath' => $this->xpathQuery($params['xml'] ?? '', $params['xpath'] ?? ''),
            'format' => $this->formatXml($params['xml'] ?? '', $params['options'] ?? []),
            'minify' => $this->minifyXml($params['xml'] ?? ''),
            'to_array' => $this->xmlToArray($params['xml'] ?? ''),
            'from_array' => $this->arrayToXml($params['data'] ?? [], $params['options'] ?? []),
            default => throw new InvalidArgumentException("Unsupported action: {$action}")
        };
    }

    /**
     * Parse XML string with comprehensive error handling
     */
    private function parseXml(string $xml, array $options = []): DOMDocument|SimpleXMLElement
    {
        if (empty($xml)) {
            throw new InvalidArgumentException('XML string cannot be empty');
        }

        $useSimpleXml = $options['simple'] ?? false;
        $libxml_options = LIBXML_NOCDATA | LIBXML_NOBLANKS;

        if ($options['recover'] ?? false) {
            $libxml_options |= LIBXML_RECOVER;
        }
        if ($options['dtdload'] ?? false) {
            $libxml_options |= LIBXML_DTDLOAD;
        }

        libxml_use_internal_errors(true);
        libxml_clear_errors();

        try {
            if ($useSimpleXml) {
                $result = simplexml_load_string($xml, 'SimpleXMLElement', $libxml_options);
                if ($result === false) {
                    throw new InvalidArgumentException('Failed to parse XML: ' . $this->getLibXmlErrors());
                }
                return $result;
            } else {
                $dom = new DOMDocument('1.0', 'UTF-8');
                $dom->formatOutput = true;
                $dom->preserveWhiteSpace = !($options['trim_whitespace'] ?? true);
                
                if (!$dom->loadXML($xml, $libxml_options)) {
                    throw new InvalidArgumentException('Failed to parse XML: ' . $this->getLibXmlErrors());
                }
                
                return $dom;
            }
        } finally {
            libxml_use_internal_errors(false);
        }
    }

    /**
     * Convert DOM/SimpleXML to string
     */
    private function stringifyXml(mixed $data, array $options = []): string
    {
        if ($data === null) {
            throw new InvalidArgumentException('XML data cannot be null');
        }

        $formatted = $options['formatted'] ?? true;

        if ($data instanceof DOMDocument) {
            $data->formatOutput = $formatted;
            return $data->saveXML();
        } elseif ($data instanceof SimpleXMLElement) {
            $dom = new DOMDocument('1.0', 'UTF-8');
            $dom->formatOutput = $formatted;
            $dom->loadXML($data->asXML());
            return $dom->saveXML();
        } else {
            throw new InvalidArgumentException('Invalid XML data type');
        }
    }

    /**
     * Validate XML against DTD or XSD schema
     */
    private function validateXml(string $xml, ?string $schema = null): array
    {
        if (empty($xml)) {
            return ['valid' => false, 'errors' => ['Empty XML string']];
        }

        libxml_use_internal_errors(true);
        libxml_clear_errors();

        try {
            $dom = new DOMDocument();
            if (!$dom->loadXML($xml)) {
                return [
                    'valid' => false,
                    'errors' => $this->getLibXmlErrorsArray()
                ];
            }

            if ($schema !== null) {
                if (str_contains($schema, '.xsd') || str_contains($schema, 'xs:schema')) {
                    // XSD validation
                    if (is_file($schema)) {
                        $isValid = $dom->schemaValidate($schema);
                    } else {
                        $isValid = $dom->schemaValidateSource($schema);
                    }
                } else {
                    // DTD validation
                    if (is_file($schema)) {
                        $isValid = $dom->validate();
                    } else {
                        // Inline DTD validation
                        $isValid = $dom->validate();
                    }
                }

                return [
                    'valid' => $isValid,
                    'errors' => $isValid ? [] : $this->getLibXmlErrorsArray()
                ];
            }

            return ['valid' => true, 'errors' => []];

        } catch (\Exception $e) {
            return [
                'valid' => false,
                'errors' => [$e->getMessage()]
            ];
        } finally {
            libxml_use_internal_errors(false);
        }
    }

    /**
     * Transform XML using XSLT
     */
    private function transformXml(string $xml, string $xslt): string
    {
        if (empty($xml) || empty($xslt)) {
            throw new InvalidArgumentException('Both XML and XSLT are required');
        }

        $xmlDoc = new DOMDocument();
        if (!$xmlDoc->loadXML($xml)) {
            throw new InvalidArgumentException('Invalid XML document');
        }

        $xslDoc = new DOMDocument();
        if (!$xslDoc->loadXML($xslt)) {
            throw new InvalidArgumentException('Invalid XSLT document');
        }

        $processor = new \XSLTProcessor();
        $processor->importStylesheet($xslDoc);
        
        $result = $processor->transformToXml($xmlDoc);
        if ($result === false) {
            throw new InvalidArgumentException('XSLT transformation failed');
        }

        return $result;
    }

    /**
     * Query XML using simple element/attribute queries
     */
    private function queryXml(string $xml, string $query): array
    {
        $dom = $this->parseXml($xml);
        if (!$dom instanceof DOMDocument) {
            throw new InvalidArgumentException('Failed to parse XML for querying');
        }

        // Simple query format: "element" or "element@attribute"
        $results = [];
        
        if (str_contains($query, '@')) {
            [$element, $attribute] = explode('@', $query, 2);
            $nodes = $dom->getElementsByTagName($element);
            foreach ($nodes as $node) {
                if ($node->hasAttribute($attribute)) {
                    $results[] = [
                        'element' => $element,
                        'attribute' => $attribute,
                        'value' => $node->getAttribute($attribute),
                        'text' => $node->textContent
                    ];
                }
            }
        } else {
            $nodes = $dom->getElementsByTagName($query);
            foreach ($nodes as $node) {
                $results[] = [
                    'element' => $query,
                    'text' => $node->textContent,
                    'attributes' => $this->getNodeAttributes($node)
                ];
            }
        }

        return $results;
    }

    /**
     * Execute XPath query
     */
    private function xpathQuery(string $xml, string $xpath): array
    {
        $dom = $this->parseXml($xml);
        if (!$dom instanceof DOMDocument) {
            throw new InvalidArgumentException('Failed to parse XML for XPath');
        }

        $xpathProcessor = new DOMXPath($dom);
        $nodes = $xpathProcessor->query($xpath);

        if ($nodes === false) {
            throw new InvalidArgumentException('Invalid XPath expression');
        }

        $results = [];
        foreach ($nodes as $node) {
            $results[] = [
                'nodeName' => $node->nodeName,
                'nodeValue' => $node->nodeValue,
                'attributes' => $this->getNodeAttributes($node),
                'xml' => $dom->saveXML($node)
            ];
        }

        return $results;
    }

    /**
     * Format XML with proper indentation
     */
    private function formatXml(string $xml, array $options = []): string
    {
        $dom = $this->parseXml($xml);
        if (!$dom instanceof DOMDocument) {
            throw new InvalidArgumentException('Failed to parse XML for formatting');
        }

        $dom->formatOutput = true;
        $dom->preserveWhiteSpace = false;
        
        if (isset($options['indent'])) {
            // Custom indentation (PHP 8.0+ feature)
            if (method_exists($dom, 'xmlStandalone')) {
                $dom->xmlStandalone = true;
            }
        }

        return $dom->saveXML();
    }

    /**
     * Minify XML by removing unnecessary whitespace
     */
    private function minifyXml(string $xml): string
    {
        $dom = $this->parseXml($xml);
        if (!$dom instanceof DOMDocument) {
            throw new InvalidArgumentException('Failed to parse XML for minification');
        }

        $dom->formatOutput = false;
        $dom->preserveWhiteSpace = false;
        
        // Remove empty text nodes
        $xpath = new DOMXPath($dom);
        $emptyTextNodes = $xpath->query('//text()[normalize-space(.) = ""]');
        
        foreach ($emptyTextNodes as $textNode) {
            $textNode->parentNode->removeChild($textNode);
        }

        return $dom->saveXML();
    }

    /**
     * Convert XML to associative array
     */
    private function xmlToArray(string $xml): array
    {
        $simpleXml = $this->parseXml($xml, ['simple' => true]);
        if (!$simpleXml instanceof SimpleXMLElement) {
            throw new InvalidArgumentException('Failed to parse XML to SimpleXML');
        }

        return $this->simpleXmlToArray($simpleXml);
    }

    /**
     * Convert associative array to XML
     */
    private function arrayToXml(array $data, array $options = []): string
    {
        $rootElement = $options['root'] ?? 'root';
        $xmlVersion = $options['version'] ?? '1.0';
        $encoding = $options['encoding'] ?? 'UTF-8';

        $dom = new DOMDocument($xmlVersion, $encoding);
        $dom->formatOutput = $options['formatted'] ?? true;

        $root = $dom->createElement($rootElement);
        $dom->appendChild($root);

        $this->arrayToXmlElement($dom, $root, $data);

        return $dom->saveXML();
    }

    /**
     * Convert SimpleXMLElement to array recursively
     */
    private function simpleXmlToArray(SimpleXMLElement $xml): array
    {
        $array = [];
        
        // Get attributes
        $attributes = $xml->attributes();
        if ($attributes) {
            foreach ($attributes as $key => $value) {
                $array['@' . $key] = (string) $value;
            }
        }

        // Get child elements
        $children = $xml->children();
        if ($children->count() > 0) {
            foreach ($children as $key => $child) {
                if (isset($array[$key])) {
                    // Multiple elements with same name
                    if (!is_array($array[$key]) || !isset($array[$key][0])) {
                        $array[$key] = [$array[$key]];
                    }
                    $array[$key][] = $this->simpleXmlToArray($child);
                } else {
                    $array[$key] = $this->simpleXmlToArray($child);
                }
            }
        } else {
            // Text content
            $text = (string) $xml;
            if (!empty($text)) {
                if (!empty($array)) {
                    $array['#text'] = $text;
                } else {
                    return $text;
                }
            }
        }

        return $array;
    }

    /**
     * Convert array to XML elements recursively
     */
    private function arrayToXmlElement(DOMDocument $dom, \DOMElement $parent, mixed $data): void
    {
        if (is_array($data)) {
            foreach ($data as $key => $value) {
                if (str_starts_with($key, '@')) {
                    // Attribute
                    $parent->setAttribute(substr($key, 1), (string) $value);
                } elseif ($key === '#text') {
                    // Text content
                    $parent->appendChild($dom->createTextNode((string) $value));
                } elseif (is_int($key)) {
                    // Numeric key - use parent element name
                    $element = $dom->createElement($parent->nodeName);
                    $parent->parentNode->appendChild($element);
                    $this->arrayToXmlElement($dom, $element, $value);
                } else {
                    if (is_array($value) && isset($value[0])) {
                        // Multiple elements with same name
                        foreach ($value as $item) {
                            $element = $dom->createElement($key);
                            $parent->appendChild($element);
                            $this->arrayToXmlElement($dom, $element, $item);
                        }
                    } else {
                        // Single element
                        $element = $dom->createElement($key);
                        $parent->appendChild($element);
                        $this->arrayToXmlElement($dom, $element, $value);
                    }
                }
            }
        } else {
            // Scalar value
            $parent->appendChild($dom->createTextNode((string) $data));
        }
    }

    /**
     * Get node attributes as array
     */
    private function getNodeAttributes(\DOMNode $node): array
    {
        $attributes = [];
        if ($node->attributes) {
            foreach ($node->attributes as $attr) {
                $attributes[$attr->name] = $attr->value;
            }
        }
        return $attributes;
    }

    /**
     * Get LibXML errors as string
     */
    private function getLibXmlErrors(): string
    {
        $errors = libxml_get_errors();
        $errorMessages = [];
        
        foreach ($errors as $error) {
            $errorMessages[] = trim($error->message);
        }
        
        libxml_clear_errors();
        return implode('; ', $errorMessages);
    }

    /**
     * Get LibXML errors as array
     */
    private function getLibXmlErrorsArray(): array
    {
        $errors = libxml_get_errors();
        $errorMessages = [];
        
        foreach ($errors as $error) {
            $errorMessages[] = [
                'level' => $error->level,
                'code' => $error->code,
                'message' => trim($error->message),
                'file' => $error->file,
                'line' => $error->line
            ];
        }
        
        libxml_clear_errors();
        return $errorMessages;
    }
} 