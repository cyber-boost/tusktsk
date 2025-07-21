<?php

namespace TuskLang\CoreOperators;

use TuskLang\CoreOperators\BaseOperator;

/**
 * XML Operator for parsing, generating, and manipulating XML data
 */
class XmlOperator extends BaseOperator
{
    public function execute(array $config, array $context = []): mixed
    {
        $action = $config['action'] ?? 'parse';
        $data = $config['data'] ?? '';
        $options = $config['options'] ?? [];

        try {
            switch ($action) {
                case 'parse':
                    return $this->parseXml($data, $options);
                case 'generate':
                    return $this->generateXml($data, $options);
                case 'validate':
                    return $this->validateXml($data, $options);
                case 'transform':
                    return $this->transformXml($data, $options);
                case 'query':
                    return $this->queryXml($data, $options);
                case 'merge':
                    return $this->mergeXml($data, $options);
                default:
                    throw new \Exception("Unknown XML action: $action");
            }
        } catch (\Exception $e) {
            error_log("XML Operator error: " . $e->getMessage());
            throw $e;
        }
    }

    /**
     * Parse XML string to array
     */
    private function parseXml(string $xmlString, array $options): array
    {
        $flags = $options['flags'] ?? LIBXML_NOCDATA | LIBXML_NOERROR;
        $encoding = $options['encoding'] ?? 'UTF-8';
        
        libxml_use_internal_errors(true);
        $xml = simplexml_load_string($xmlString, 'SimpleXMLElement', $flags);
        
        if ($xml === false) {
            $errors = libxml_get_errors();
            libxml_clear_errors();
            throw new \Exception("XML parsing failed: " . implode(', ', array_map(fn($e) => $e->message, $errors)));
        }

        return $this->xmlToArray($xml);
    }

    /**
     * Generate XML from array
     */
    private function generateXml(array $data, array $options): string
    {
        $rootElement = $options['root'] ?? 'root';
        $version = $options['version'] ?? '1.0';
        $encoding = $options['encoding'] ?? 'UTF-8';
        $indent = $options['indent'] ?? true;

        $xml = new \XMLWriter();
        $xml->openMemory();
        $xml->setIndent($indent);
        $xml->startDocument($version, $encoding);
        $xml->startElement($rootElement);

        $this->arrayToXml($xml, $data);
        
        $xml->endElement();
        return $xml->outputMemory();
    }

    /**
     * Validate XML against schema
     */
    private function validateXml(string $xmlString, array $options): array
    {
        $schema = $options['schema'] ?? null;
        $dtd = $options['dtd'] ?? null;

        libxml_use_internal_errors(true);
        
        if ($schema) {
            $dom = new \DOMDocument();
            $dom->loadXML($xmlString);
            $valid = $dom->schemaValidate($schema);
        } elseif ($dtd) {
            $dom = new \DOMDocument();
            $dom->loadXML($xmlString);
            $valid = $dom->validate();
        } else {
            // Basic well-formedness check
            $valid = simplexml_load_string($xmlString) !== false;
        }

        $errors = libxml_get_errors();
        libxml_clear_errors();

        return [
            'valid' => $valid,
            'errors' => array_map(fn($e) => $e->message, $errors)
        ];
    }

    /**
     * Transform XML using XSLT
     */
    private function transformXml(string $xmlString, array $options): string
    {
        $xslt = $options['xslt'] ?? '';
        if (empty($xslt)) {
            throw new \Exception("XSLT stylesheet required for transformation");
        }

        $xmlDoc = new \DOMDocument();
        $xmlDoc->loadXML($xmlString);

        $xsltDoc = new \DOMDocument();
        $xsltDoc->loadXML($xslt);

        $processor = new \XSLTProcessor();
        $processor->importStylesheet($xsltDoc);

        return $processor->transformToXml($xmlDoc);
    }

    /**
     * Query XML using XPath
     */
    private function queryXml(string $xmlString, array $options): array
    {
        $xpath = $options['xpath'] ?? '';
        if (empty($xpath)) {
            throw new \Exception("XPath expression required for querying");
        }

        $dom = new \DOMDocument();
        $dom->loadXML($xmlString);
        
        $xpathObj = new \DOMXPath($dom);
        $nodes = $xpathObj->query($xpath);

        $results = [];
        foreach ($nodes as $node) {
            $results[] = $this->nodeToArray($node);
        }

        return $results;
    }

    /**
     * Merge multiple XML documents
     */
    private function mergeXml(array $xmlStrings, array $options): string
    {
        $rootElement = $options['root'] ?? 'merged';
        $strategy = $options['strategy'] ?? 'append'; // append, merge, replace

        $xml = new \XMLWriter();
        $xml->openMemory();
        $xml->setIndent(true);
        $xml->startDocument('1.0', 'UTF-8');
        $xml->startElement($rootElement);

        foreach ($xmlStrings as $xmlString) {
            $dom = new \DOMDocument();
            $dom->loadXML($xmlString);
            
            $this->importNode($xml, $dom->documentElement, $strategy);
        }

        $xml->endElement();
        return $xml->outputMemory();
    }

    /**
     * Convert SimpleXMLElement to array
     */
    private function xmlToArray(\SimpleXMLElement $xml): array
    {
        $array = [];
        
        foreach ($xml->children() as $child) {
            $name = $child->getName();
            $value = (string)$child;
            
            if ($child->count() > 0) {
                $array[$name] = $this->xmlToArray($child);
            } else {
                $array[$name] = $value;
            }
        }

        return $array;
    }

    /**
     * Convert array to XML
     */
    private function arrayToXml(\XMLWriter $xml, array $data): void
    {
        foreach ($data as $key => $value) {
            if (is_array($value)) {
                $xml->startElement($key);
                $this->arrayToXml($xml, $value);
                $xml->endElement();
            } else {
                $xml->writeElement($key, $value);
            }
        }
    }

    /**
     * Convert DOMNode to array
     */
    private function nodeToArray(\DOMNode $node): array
    {
        $array = [];
        
        if ($node->nodeType === XML_ELEMENT_NODE) {
            $array['name'] = $node->nodeName;
            $array['value'] = $node->textContent;
            
            if ($node->hasAttributes()) {
                $array['attributes'] = [];
                foreach ($node->attributes as $attr) {
                    $array['attributes'][$attr->name] = $attr->value;
                }
            }
            
            if ($node->hasChildNodes()) {
                $array['children'] = [];
                foreach ($node->childNodes as $child) {
                    if ($child->nodeType === XML_ELEMENT_NODE) {
                        $array['children'][] = $this->nodeToArray($child);
                    }
                }
            }
        }

        return $array;
    }

    /**
     * Import DOMNode into XMLWriter
     */
    private function importNode(\XMLWriter $xml, \DOMNode $node, string $strategy): void
    {
        if ($node->nodeType === XML_ELEMENT_NODE) {
            $xml->startElement($node->nodeName);
            
            if ($node->hasAttributes()) {
                foreach ($node->attributes as $attr) {
                    $xml->writeAttribute($attr->name, $attr->value);
                }
            }
            
            if ($node->hasChildNodes()) {
                foreach ($node->childNodes as $child) {
                    if ($child->nodeType === XML_ELEMENT_NODE) {
                        $this->importNode($xml, $child, $strategy);
                    } elseif ($child->nodeType === XML_TEXT_NODE) {
                        $xml->text($child->textContent);
                    }
                }
            }
            
            $xml->endElement();
        }
    }
} 