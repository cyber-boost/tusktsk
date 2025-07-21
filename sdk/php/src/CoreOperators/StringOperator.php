<?php
/**
 * TuskLang String Operator
 * ========================
 * Handles string manipulation and processing
 */

namespace TuskLang\CoreOperators;

class StringOperator extends \TuskLang\CoreOperators\BaseOperator
{
    public function getName(): string
    {
        return 'string';
    }

    protected function executeOperator(array $config, array $context): mixed
    {
        $operation = $config['operation'] ?? null;
        $value = $config['value'] ?? null;
        $target = $config['target'] ?? null;

        if ($operation === null) {
            throw new \Exception("String operator requires an 'operation' parameter");
        }

        if ($value === null) {
            throw new \Exception("String operator requires a 'value' parameter");
        }

        switch ($operation) {
            case 'uppercase':
                return strtoupper($value);

            case 'lowercase':
                return strtolower($value);

            case 'capitalize':
                return ucfirst($value);

            case 'title':
                return ucwords($value);

            case 'trim':
                return trim($value);

            case 'ltrim':
                return ltrim($value);

            case 'rtrim':
                return rtrim($value);

            case 'length':
                return strlen($value);

            case 'substring':
                $start = $config['start'] ?? 0;
                $length = $config['length'] ?? null;
                if ($length !== null) {
                    return substr($value, $start, $length);
                }
                return substr($value, $start);

            case 'replace':
                if ($target === null) {
                    throw new \Exception("String replace operation requires 'target' parameter");
                }
                $replacement = $config['replacement'] ?? '';
                return str_replace($target, $replacement, $value);

            case 'split':
                $delimiter = $config['delimiter'] ?? ' ';
                return explode($delimiter, $value);

            case 'join':
                if (!is_array($value)) {
                    throw new \Exception("String join operation requires array value");
                }
                $delimiter = $config['delimiter'] ?? ' ';
                return implode($delimiter, $value);

            case 'contains':
                if ($target === null) {
                    throw new \Exception("String contains operation requires 'target' parameter");
                }
                return strpos($value, $target) !== false;

            case 'starts_with':
                if ($target === null) {
                    throw new \Exception("String starts_with operation requires 'target' parameter");
                }
                return str_starts_with($value, $target);

            case 'ends_with':
                if ($target === null) {
                    throw new \Exception("String ends_with operation requires 'target' parameter");
                }
                return str_ends_with($value, $target);

            case 'reverse':
                return strrev($value);

            case 'repeat':
                $times = $config['times'] ?? 1;
                return str_repeat($value, $times);

            case 'pad':
                $length = $config['length'] ?? 0;
                $padString = $config['pad_string'] ?? ' ';
                $padType = $config['pad_type'] ?? STR_PAD_RIGHT;
                return str_pad($value, $length, $padString, $padType);

            case 'word_count':
                return str_word_count($value);

            case 'slug':
                $value = strtolower($value);
                $value = preg_replace('/[^a-z0-9\s-]/', '', $value);
                $value = preg_replace('/[\s-]+/', '-', $value);
                return trim($value, '-');

            default:
                throw new \Exception("Unknown string operation: $operation");
        }
    }
} 