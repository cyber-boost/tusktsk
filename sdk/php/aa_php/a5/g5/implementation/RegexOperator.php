<?php

declare(strict_types=1);

namespace TuskLang\A5\G5;

use TuskLang\CoreOperator;
use InvalidArgumentException;

/**
 * RegexOperator - Powerful regex pattern matching and replacement operations
 * 
 * Provides comprehensive regular expression operations including pattern matching,
 * replacement, validation, extraction, and advanced regex utilities with full Unicode support.
 */
class RegexOperator extends CoreOperator
{
    private array $commonPatterns;

    public function __construct()
    {
        $this->initializeCommonPatterns();
    }

    public function getName(): string
    {
        return 'regex';
    }

    public function getDescription(): string 
    {
        return 'Powerful regex pattern matching and replacement operations';
    }

    public function getSupportedActions(): array
    {
        return [
            'match', 'match_all', 'replace', 'split', 'validate_pattern',
            'extract', 'find_positions', 'count_matches', 'test_pattern',
            'escape', 'build_pattern', 'named_groups', 'common_patterns'
        ];
    }

    public function execute(string $action, array $params = []): mixed
    {
        return match($action) {
            'match' => $this->match($params['pattern'] ?? '', $params['subject'] ?? '', $params['flags'] ?? []),
            'match_all' => $this->matchAll($params['pattern'] ?? '', $params['subject'] ?? '', $params['flags'] ?? []),
            'replace' => $this->replace($params['pattern'] ?? '', $params['replacement'] ?? '', $params['subject'] ?? '', $params['options'] ?? []),
            'split' => $this->split($params['pattern'] ?? '', $params['subject'] ?? '', $params['limit'] ?? -1),
            'validate_pattern' => $this->validatePattern($params['pattern'] ?? ''),
            'extract' => $this->extract($params['pattern'] ?? '', $params['subject'] ?? '', $params['group'] ?? 0),
            'find_positions' => $this->findPositions($params['pattern'] ?? '', $params['subject'] ?? ''),
            'count_matches' => $this->countMatches($params['pattern'] ?? '', $params['subject'] ?? ''),
            'test_pattern' => $this->testPattern($params['pattern'] ?? '', $params['test_strings'] ?? []),
            'escape' => $this->escapeString($params['string'] ?? ''),
            'build_pattern' => $this->buildPattern($params['components'] ?? [], $params['options'] ?? []),
            'named_groups' => $this->namedGroups($params['pattern'] ?? '', $params['subject'] ?? ''),
            'common_patterns' => $this->getCommonPatterns($params['type'] ?? null),
            default => throw new InvalidArgumentException("Unsupported action: {$action}")
        };
    }

    /**
     * Perform regex match
     */
    private function match(string $pattern, string $subject, array $flags = []): array
    {
        if (empty($pattern)) {
            throw new InvalidArgumentException('Pattern cannot be empty');
        }

        $pattern = $this->normalizePattern($pattern);
        $phpFlags = $this->convertFlags($flags);

        $matches = [];
        $result = preg_match($pattern, $subject, $matches, $phpFlags);

        if ($result === false) {
            throw new InvalidArgumentException('Invalid regex pattern: ' . preg_last_error_msg());
        }

        return [
            'matched' => $result === 1,
            'matches' => $matches,
            'full_match' => $matches[0] ?? null,
            'groups' => array_slice($matches, 1),
            'pattern' => $pattern,
            'subject' => $subject,
            'flags' => $flags
        ];
    }

    /**
     * Perform global regex match
     */
    private function matchAll(string $pattern, string $subject, array $flags = []): array
    {
        if (empty($pattern)) {
            throw new InvalidArgumentException('Pattern cannot be empty');
        }

        $pattern = $this->normalizePattern($pattern);
        $phpFlags = $this->convertFlags($flags) | PREG_PATTERN_ORDER;

        $matches = [];
        $result = preg_match_all($pattern, $subject, $matches, $phpFlags);

        if ($result === false) {
            throw new InvalidArgumentException('Invalid regex pattern: ' . preg_last_error_msg());
        }

        return [
            'match_count' => $result,
            'matches' => $matches,
            'all_matches' => $matches[0] ?? [],
            'groups' => array_slice($matches, 1),
            'pattern' => $pattern,
            'subject' => $subject,
            'flags' => $flags
        ];
    }

    /**
     * Perform regex replacement
     */
    private function replace(string $pattern, string $replacement, string $subject, array $options = []): array
    {
        if (empty($pattern)) {
            throw new InvalidArgumentException('Pattern cannot be empty');
        }

        $pattern = $this->normalizePattern($pattern);
        $limit = $options['limit'] ?? -1;
        $count = 0;

        if ($options['callback'] ?? false) {
            if (!is_callable($replacement)) {
                throw new InvalidArgumentException('Replacement must be callable when callback option is true');
            }
            $result = preg_replace_callback($pattern, $replacement, $subject, $limit, $count);
        } else {
            $result = preg_replace($pattern, $replacement, $subject, $limit, $count);
        }

        if ($result === null) {
            throw new InvalidArgumentException('Regex replacement failed: ' . preg_last_error_msg());
        }

        return [
            'result' => $result,
            'original' => $subject,
            'pattern' => $pattern,
            'replacement' => is_callable($replacement) ? '[callback]' : $replacement,
            'replacements_made' => $count,
            'changed' => $result !== $subject
        ];
    }

    /**
     * Split string using regex
     */
    private function split(string $pattern, string $subject, int $limit = -1): array
    {
        if (empty($pattern)) {
            throw new InvalidArgumentException('Pattern cannot be empty');
        }

        $pattern = $this->normalizePattern($pattern);
        $flags = PREG_SPLIT_NO_EMPTY;

        $result = preg_split($pattern, $subject, $limit, $flags);

        if ($result === false) {
            throw new InvalidArgumentException('Regex split failed: ' . preg_last_error_msg());
        }

        return [
            'parts' => $result,
            'count' => count($result),
            'pattern' => $pattern,
            'subject' => $subject,
            'limit' => $limit
        ];
    }

    /**
     * Validate regex pattern
     */
    private function validatePattern(string $pattern): array
    {
        if (empty($pattern)) {
            return ['valid' => false, 'error' => 'Pattern cannot be empty'];
        }

        $pattern = $this->normalizePattern($pattern);
        
        // Test pattern validity
        $result = preg_match($pattern, '');
        
        if ($result === false) {
            return [
                'valid' => false,
                'error' => preg_last_error_msg(),
                'error_code' => preg_last_error(),
                'pattern' => $pattern
            ];
        }

        return [
            'valid' => true,
            'pattern' => $pattern,
            'features' => $this->analyzePattern($pattern)
        ];
    }

    /**
     * Extract specific groups from matches
     */
    private function extract(string $pattern, string $subject, int|string $group = 0): array
    {
        $matchResult = $this->matchAll($pattern, $subject);
        
        if (!$matchResult['match_count']) {
            return [
                'extracted' => [],
                'count' => 0,
                'group' => $group
            ];
        }

        $extracted = [];
        
        if (is_int($group)) {
            $extracted = $matchResult['matches'][$group] ?? [];
        } else {
            // Named group
            $extracted = $matchResult['matches'][$group] ?? [];
        }

        return [
            'extracted' => $extracted,
            'count' => count($extracted),
            'group' => $group,
            'total_matches' => $matchResult['match_count']
        ];
    }

    /**
     * Find positions of all matches
     */
    private function findPositions(string $pattern, string $subject): array
    {
        if (empty($pattern)) {
            throw new InvalidArgumentException('Pattern cannot be empty');
        }

        $pattern = $this->normalizePattern($pattern);
        $matches = [];
        $result = preg_match_all($pattern, $subject, $matches, PREG_OFFSET_CAPTURE);

        if ($result === false) {
            throw new InvalidArgumentException('Invalid regex pattern: ' . preg_last_error_msg());
        }

        $positions = [];
        foreach ($matches[0] as $match) {
            $positions[] = [
                'match' => $match[0],
                'offset' => $match[1],
                'length' => strlen($match[0]),
                'end_offset' => $match[1] + strlen($match[0])
            ];
        }

        return [
            'positions' => $positions,
            'count' => count($positions),
            'pattern' => $pattern,
            'subject_length' => strlen($subject)
        ];
    }

    /**
     * Count regex matches
     */
    private function countMatches(string $pattern, string $subject): array
    {
        if (empty($pattern)) {
            throw new InvalidArgumentException('Pattern cannot be empty');
        }

        $pattern = $this->normalizePattern($pattern);
        $result = preg_match_all($pattern, $subject);

        if ($result === false) {
            throw new InvalidArgumentException('Invalid regex pattern: ' . preg_last_error_msg());
        }

        return [
            'count' => $result,
            'pattern' => $pattern,
            'subject_length' => strlen($subject),
            'match_density' => strlen($subject) > 0 ? round($result / strlen($subject), 6) : 0
        ];
    }

    /**
     * Test pattern against multiple strings
     */
    private function testPattern(string $pattern, array $testStrings): array
    {
        if (empty($pattern)) {
            throw new InvalidArgumentException('Pattern cannot be empty');
        }

        $pattern = $this->normalizePattern($pattern);
        $results = [];
        $totalMatches = 0;
        $matchedStrings = 0;

        foreach ($testStrings as $index => $testString) {
            $matchResult = $this->match($pattern, $testString);
            $results[$index] = [
                'string' => $testString,
                'matched' => $matchResult['matched'],
                'full_match' => $matchResult['full_match'],
                'groups' => $matchResult['groups']
            ];
            
            if ($matchResult['matched']) {
                $totalMatches++;
                $matchedStrings++;
            }
        }

        return [
            'pattern' => $pattern,
            'results' => $results,
            'statistics' => [
                'total_tests' => count($testStrings),
                'matched_strings' => $matchedStrings,
                'match_rate' => count($testStrings) > 0 ? round(($matchedStrings / count($testStrings)) * 100, 2) : 0
            ]
        ];
    }

    /**
     * Escape special regex characters
     */
    private function escapeString(string $string): array
    {
        $escaped = preg_quote($string, '/');
        
        return [
            'original' => $string,
            'escaped' => $escaped,
            'changed' => $escaped !== $string,
            'special_chars_found' => $this->findSpecialChars($string)
        ];
    }

    /**
     * Build regex pattern from components
     */
    private function buildPattern(array $components, array $options = []): array
    {
        $pattern = '';
        $delimiter = $options['delimiter'] ?? '/';
        $flags = $options['flags'] ?? '';
        
        foreach ($components as $component) {
            if (is_array($component)) {
                $type = $component['type'] ?? 'literal';
                $value = $component['value'] ?? '';
                
                switch ($type) {
                    case 'literal':
                        $pattern .= preg_quote($value, $delimiter);
                        break;
                    case 'group':
                        $pattern .= '(' . $value . ')';
                        break;
                    case 'optional':
                        $pattern .= '(' . $value . ')?';
                        break;
                    case 'character_class':
                        $pattern .= '[' . $value . ']';
                        break;
                    case 'negated_class':
                        $pattern .= '[^' . $value . ']';
                        break;
                    case 'quantifier':
                        $pattern .= $value;
                        break;
                    default:
                        $pattern .= $value;
                }
            } else {
                $pattern .= preg_quote($component, $delimiter);
            }
        }

        $fullPattern = $delimiter . $pattern . $delimiter . $flags;
        
        $validation = $this->validatePattern($fullPattern);

        return [
            'pattern' => $fullPattern,
            'components' => $components,
            'valid' => $validation['valid'],
            'error' => $validation['error'] ?? null
        ];
    }

    /**
     * Extract named groups from pattern and subject
     */
    private function namedGroups(string $pattern, string $subject): array
    {
        if (empty($pattern)) {
            throw new InvalidArgumentException('Pattern cannot be empty');
        }

        $pattern = $this->normalizePattern($pattern);
        $matches = [];
        $result = preg_match($pattern, $subject, $matches);

        if ($result === false) {
            throw new InvalidArgumentException('Invalid regex pattern: ' . preg_last_error_msg());
        }

        // Extract named groups
        $namedGroups = [];
        foreach ($matches as $key => $value) {
            if (is_string($key)) {
                $namedGroups[$key] = $value;
            }
        }

        return [
            'matched' => $result === 1,
            'named_groups' => $namedGroups,
            'all_matches' => $matches,
            'group_names' => array_keys($namedGroups),
            'pattern' => $pattern
        ];
    }

    /**
     * Get common regex patterns
     */
    private function getCommonPatterns(?string $type = null): array
    {
        if ($type && isset($this->commonPatterns[$type])) {
            return $this->commonPatterns[$type];
        }

        return $type ? [] : $this->commonPatterns;
    }

    // Helper methods

    /**
     * Initialize common regex patterns
     */
    private function initializeCommonPatterns(): void
    {
        $this->commonPatterns = [
            'email' => [
                'basic' => '/^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/',
                'strict' => '/^[a-zA-Z0-9.!#$%&\'*+\/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$/'
            ],
            'url' => [
                'basic' => '/^https?:\/\/[^\s]+$/',
                'strict' => '/^https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)$/'
            ],
            'phone' => [
                'us' => '/^(\+1[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}$/',
                'international' => '/^\+?[1-9]\d{1,14}$/'
            ],
            'date' => [
                'iso' => '/^\d{4}-\d{2}-\d{2}$/',
                'us' => '/^(0?[1-9]|1[0-2])\/(0?[1-9]|[12]\d|3[01])\/\d{4}$/',
                'eu' => '/^(0?[1-9]|[12]\d|3[01])\/(0?[1-9]|1[0-2])\/\d{4}$/'
            ],
            'ip' => [
                'ipv4' => '/^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$/',
                'ipv6' => '/^([0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}$/'
            ],
            'password' => [
                'simple' => '/^.{8,}$/',
                'medium' => '/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d]{8,}$/',
                'strong' => '/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/'
            ],
            'text' => [
                'alphanumeric' => '/^[a-zA-Z0-9]+$/',
                'alpha_only' => '/^[a-zA-Z]+$/',
                'numeric_only' => '/^\d+$/',
                'whitespace' => '/\s+/',
                'word_boundaries' => '/\b\w+\b/'
            ],
            'html' => [
                'tags' => '/<\/?[a-z][\s\S]*>/i',
                'attributes' => '/\s*(\w+)\s*=\s*["\']([^"\']*)["\']/',
                'comments' => '/<!--[\s\S]*?-->/'
            ],
            'css' => [
                'selector' => '/^[a-zA-Z][\w\-]*$/',
                'property' => '/^[a-zA-Z\-]+$/',
                'color_hex' => '/^#([a-fA-F0-9]{6}|[a-fA-F0-9]{3})$/'
            ]
        ];
    }

    /**
     * Normalize pattern (add delimiters if missing)
     */
    private function normalizePattern(string $pattern): string
    {
        // If pattern doesn't start with delimiter, add default delimiters
        if (!preg_match('/^[\/~#@!]/', $pattern)) {
            return '/' . $pattern . '/';
        }
        
        return $pattern;
    }

    /**
     * Convert flag array to PHP preg flags
     */
    private function convertFlags(array $flags): int
    {
        $phpFlags = 0;
        
        foreach ($flags as $flag) {
            $phpFlags |= match(strtolower($flag)) {
                'i', 'ignorecase' => PREG_PATTERN_ORDER,
                'm', 'multiline' => PREG_SET_ORDER,
                's', 'dotall' => PREG_OFFSET_CAPTURE,
                'x', 'extended' => PREG_UNMATCHED_AS_NULL,
                'u', 'unicode' => PREG_UNMATCHED_AS_NULL,
                default => 0
            };
        }
        
        return $phpFlags;
    }

    /**
     * Analyze regex pattern features
     */
    private function analyzePattern(string $pattern): array
    {
        $features = [
            'has_groups' => (bool) preg_match('/\([^?]/', $pattern),
            'has_named_groups' => (bool) preg_match('/\(\?P<\w+>/', $pattern),
            'has_quantifiers' => (bool) preg_match('/[*+?{]/', $pattern),
            'has_anchors' => (bool) preg_match('/[\^$]/', $pattern),
            'has_character_classes' => (bool) preg_match('/\[.*?\]/', $pattern),
            'has_word_boundaries' => (bool) preg_match('/\\b/', $pattern),
            'has_lookahead' => (bool) preg_match('/\(\?=/', $pattern),
            'has_lookbehind' => (bool) preg_match('/\(\?<=/', $pattern),
            'has_alternatives' => (bool) preg_match('/\|/', $pattern)
        ];

        return $features;
    }

    /**
     * Find special regex characters in string
     */
    private function findSpecialChars(string $string): array
    {
        $specialChars = ['.', '^', '$', '*', '+', '?', '{', '}', '[', ']', '(', ')', '|', '\\'];
        $found = [];
        
        foreach ($specialChars as $char) {
            if (str_contains($string, $char)) {
                $found[] = $char;
            }
        }
        
        return $found;
    }

    /**
     * Generate regex for common validation tasks
     */
    public function generateValidationPattern(string $type, array $options = []): array
    {
        $patterns = [
            'email' => $this->generateEmailPattern($options),
            'phone' => $this->generatePhonePattern($options),
            'password' => $this->generatePasswordPattern($options),
            'url' => $this->generateUrlPattern($options),
            'credit_card' => $this->generateCreditCardPattern($options)
        ];

        if (!isset($patterns[$type])) {
            throw new InvalidArgumentException("Unknown validation type: {$type}");
        }

        return $patterns[$type];
    }

    private function generateEmailPattern(array $options): array
    {
        $strictMode = $options['strict'] ?? false;
        
        $pattern = $strictMode 
            ? '/^[a-zA-Z0-9.!#$%&\'*+\/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$/'
            : '/^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/';

        return [
            'pattern' => $pattern,
            'description' => 'Email address validation',
            'strict_mode' => $strictMode
        ];
    }

    private function generatePhonePattern(array $options): array
    {
        $country = $options['country'] ?? 'us';
        
        $patterns = [
            'us' => '/^(\+1[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}$/',
            'international' => '/^\+?[1-9]\d{1,14}$/',
            'any' => '/^[\+]?[1-9][\d\s\-\(\)]{7,15}$/'
        ];

        $pattern = $patterns[$country] ?? $patterns['us'];

        return [
            'pattern' => $pattern,
            'description' => ucfirst($country) . ' phone number validation',
            'country' => $country
        ];
    }

    private function generatePasswordPattern(array $options): array
    {
        $minLength = $options['min_length'] ?? 8;
        $requireUpper = $options['require_uppercase'] ?? false;
        $requireLower = $options['require_lowercase'] ?? false;
        $requireDigit = $options['require_digit'] ?? false;
        $requireSpecial = $options['require_special'] ?? false;

        $pattern = '/^';
        
        if ($requireUpper) $pattern .= '(?=.*[A-Z])';
        if ($requireLower) $pattern .= '(?=.*[a-z])';
        if ($requireDigit) $pattern .= '(?=.*\d)';
        if ($requireSpecial) $pattern .= '(?=.*[@$!%*?&])';
        
        $pattern .= '.{' . $minLength . ',}$/';

        return [
            'pattern' => $pattern,
            'description' => 'Password validation',
            'requirements' => [
                'min_length' => $minLength,
                'uppercase' => $requireUpper,
                'lowercase' => $requireLower,
                'digit' => $requireDigit,
                'special' => $requireSpecial
            ]
        ];
    }

    private function generateUrlPattern(array $options): array
    {
        $protocol = $options['protocol'] ?? 'https?';
        $requireWww = $options['require_www'] ?? false;
        
        $wwwPart = $requireWww ? 'www\.' : '(www\.)?';
        $pattern = '/^' . $protocol . ':\/\/' . $wwwPart . '[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)$/';

        return [
            'pattern' => $pattern,
            'description' => 'URL validation',
            'protocol' => $protocol,
            'require_www' => $requireWww
        ];
    }

    private function generateCreditCardPattern(array $options): array
    {
        $type = $options['type'] ?? 'any';
        
        $patterns = [
            'visa' => '/^4[0-9]{12}(?:[0-9]{3})?$/',
            'mastercard' => '/^5[1-5][0-9]{14}$/',
            'amex' => '/^3[47][0-9]{13}$/',
            'discover' => '/^6(?:011|5[0-9]{2})[0-9]{12}$/',
            'any' => '/^(?:4[0-9]{12}(?:[0-9]{3})?|5[1-5][0-9]{14}|3[47][0-9]{13}|6(?:011|5[0-9]{2})[0-9]{12})$/'
        ];

        $pattern = $patterns[$type] ?? $patterns['any'];

        return [
            'pattern' => $pattern,
            'description' => ucfirst($type) . ' credit card validation',
            'type' => $type
        ];
    }
} 