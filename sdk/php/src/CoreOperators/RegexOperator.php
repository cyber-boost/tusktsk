<?php
/**
 * TuskLang Regex Operator
 * =======================
 * Handles regular expressions and pattern matching
 */

namespace TuskLang\CoreOperators;

class RegexOperator extends \TuskLang\CoreOperators\BaseOperator
{
    public function getName(): string
    {
        return 'regex';
    }

    protected function executeOperator(array $config, array $context): mixed
    {
        $operation = $config['operation'] ?? null;
        $pattern = $config['pattern'] ?? null;
        $subject = $config['subject'] ?? null;
        $replacement = $config['replacement'] ?? '';

        if ($operation === null) {
            throw new \Exception("Regex operator requires an 'operation' parameter");
        }

        if ($pattern === null) {
            throw new \Exception("Regex operator requires a 'pattern' parameter");
        }

        if ($subject === null) {
            throw new \Exception("Regex operator requires a 'subject' parameter");
        }

        switch ($operation) {
            case 'match':
                $matches = [];
                if (preg_match($pattern, $subject, $matches)) {
                    return $matches;
                }
                return false;

            case 'match_all':
                $matches = [];
                $flags = $config['flags'] ?? 0;
                if (preg_match_all($pattern, $subject, $matches, $flags)) {
                    return $matches;
                }
                return false;

            case 'replace':
                return preg_replace($pattern, $replacement, $subject);

            case 'replace_callback':
                $callback = $config['callback'] ?? null;
                if ($callback === null) {
                    throw new \Exception("Regex replace_callback operation requires 'callback' parameter");
                }
                return preg_replace_callback($pattern, $callback, $subject);

            case 'split':
                $limit = $config['limit'] ?? -1;
                return preg_split($pattern, $subject, $limit);

            case 'test':
                return preg_match($pattern, $subject) === 1;

            case 'extract':
                $matches = [];
                if (preg_match($pattern, $subject, $matches)) {
                    return $matches[1] ?? $matches[0];
                }
                return null;

            case 'extract_all':
                $matches = [];
                if (preg_match_all($pattern, $subject, $matches)) {
                    return $matches[1] ?? $matches[0];
                }
                return [];

            case 'validate':
                return preg_match($pattern, $subject) === 1;

            case 'escape':
                return preg_quote($subject, $config['delimiter'] ?? '/');

            default:
                throw new \Exception("Unknown regex operation: $operation");
        }
    }
} 