<?php

declare(strict_types=1);

namespace TuskLang\A5\G5;

use TuskLang\CoreOperator;
use InvalidArgumentException;

/**
 * StringOperator - Comprehensive string manipulation, transformation, and analysis
 * 
 * Provides comprehensive string operations including manipulation, transformation,
 * analysis, formatting, and advanced string processing with UTF-8 support.
 */
class StringOperator extends CoreOperator
{
    public function getName(): string
    {
        return 'string';
    }

    public function getDescription(): string 
    {
        return 'Comprehensive string manipulation, transformation, and analysis';
    }

    public function getSupportedActions(): array
    {
        return [
            'length', 'substring', 'replace', 'split', 'join', 'trim', 'pad',
            'case_transform', 'reverse', 'contains', 'starts_with', 'ends_with',
            'repeat', 'truncate', 'word_count', 'char_count', 'slug', 'random',
            'similarity', 'distance', 'normalize', 'encode', 'decode', 'format'
        ];
    }

    public function execute(string $action, array $params = []): mixed
    {
        return match($action) {
            'length' => $this->getLength($params['string'] ?? ''),
            'substring' => $this->substring($params['string'] ?? '', $params['start'] ?? 0, $params['length'] ?? null),
            'replace' => $this->replace($params['string'] ?? '', $params['search'] ?? '', $params['replace'] ?? '', $params['options'] ?? []),
            'split' => $this->split($params['string'] ?? '', $params['delimiter'] ?? '', $params['limit'] ?? null),
            'join' => $this->join($params['array'] ?? [], $params['glue'] ?? ''),
            'trim' => $this->trim($params['string'] ?? '', $params['characters'] ?? null),
            'pad' => $this->pad($params['string'] ?? '', $params['length'] ?? 0, $params['pad_string'] ?? ' ', $params['type'] ?? 'right'),
            'case_transform' => $this->caseTransform($params['string'] ?? '', $params['type'] ?? 'lower'),
            'reverse' => $this->reverse($params['string'] ?? ''),
            'contains' => $this->contains($params['string'] ?? '', $params['needle'] ?? '', $params['case_sensitive'] ?? true),
            'starts_with' => $this->startsWith($params['string'] ?? '', $params['prefix'] ?? '', $params['case_sensitive'] ?? true),
            'ends_with' => $this->endsWith($params['string'] ?? '', $params['suffix'] ?? '', $params['case_sensitive'] ?? true),
            'repeat' => $this->repeat($params['string'] ?? '', $params['times'] ?? 1),
            'truncate' => $this->truncate($params['string'] ?? '', $params['length'] ?? 100, $params['suffix'] ?? '...'),
            'word_count' => $this->wordCount($params['string'] ?? ''),
            'char_count' => $this->charCount($params['string'] ?? '', $params['char'] ?? ''),
            'slug' => $this->slug($params['string'] ?? '', $params['separator'] ?? '-'),
            'random' => $this->random($params['length'] ?? 10, $params['charset'] ?? 'alphanumeric'),
            'similarity' => $this->similarity($params['string1'] ?? '', $params['string2'] ?? ''),
            'distance' => $this->distance($params['string1'] ?? '', $params['string2'] ?? ''),
            'normalize' => $this->normalize($params['string'] ?? '', $params['form'] ?? 'NFC'),
            'encode' => $this->encode($params['string'] ?? '', $params['encoding'] ?? 'base64'),
            'decode' => $this->decode($params['string'] ?? '', $params['encoding'] ?? 'base64'),
            'format' => $this->format($params['template'] ?? '', $params['values'] ?? []),
            default => throw new InvalidArgumentException("Unsupported action: {$action}")
        };
    }

    /**
     * Get string length (UTF-8 safe)
     */
    private function getLength(string $string): int
    {
        return mb_strlen($string, 'UTF-8');
    }

    /**
     * Get substring (UTF-8 safe)
     */
    private function substring(string $string, int $start, ?int $length = null): string
    {
        if ($length === null) {
            return mb_substr($string, $start, null, 'UTF-8');
        }
        
        return mb_substr($string, $start, $length, 'UTF-8');
    }

    /**
     * Replace substring with options
     */
    private function replace(string $string, string $search, string $replace, array $options = []): array
    {
        $caseSensitive = $options['case_sensitive'] ?? true;
        $limit = $options['limit'] ?? -1;
        $count = 0;
        
        if ($caseSensitive) {
            $result = $limit === -1 
                ? str_replace($search, $replace, $string, $count)
                : $this->strReplaceLimit($search, $replace, $string, $limit, $count);
        } else {
            $result = $limit === -1
                ? str_ireplace($search, $replace, $string, $count)
                : $this->strIreplaceLimit($search, $replace, $string, $limit, $count);
        }

        return [
            'result' => $result,
            'original' => $string,
            'replacements' => $count,
            'search' => $search,
            'replace' => $replace
        ];
    }

    /**
     * Split string into array
     */
    private function split(string $string, string $delimiter, ?int $limit = null): array
    {
        if (empty($delimiter)) {
            return str_split($string);
        }

        if ($limit === null) {
            return explode($delimiter, $string);
        }

        return explode($delimiter, $string, $limit);
    }

    /**
     * Join array elements with glue
     */
    private function join(array $array, string $glue): string
    {
        return implode($glue, $array);
    }

    /**
     * Trim whitespace or custom characters
     */
    private function trim(string $string, ?string $characters = null): array
    {
        $original = $string;
        
        if ($characters === null) {
            $trimmed = trim($string);
            $leftTrimmed = ltrim($string);
            $rightTrimmed = rtrim($string);
        } else {
            $trimmed = trim($string, $characters);
            $leftTrimmed = ltrim($string, $characters);
            $rightTrimmed = rtrim($string, $characters);
        }

        return [
            'trimmed' => $trimmed,
            'left_trimmed' => $leftTrimmed,
            'right_trimmed' => $rightTrimmed,
            'original' => $original,
            'characters_removed' => mb_strlen($original) - mb_strlen($trimmed)
        ];
    }

    /**
     * Pad string to specified length
     */
    private function pad(string $string, int $length, string $padString = ' ', string $type = 'right'): string
    {
        $currentLength = mb_strlen($string, 'UTF-8');
        
        if ($currentLength >= $length) {
            return $string;
        }

        $padLength = $length - $currentLength;
        
        return match($type) {
            'left' => str_repeat($padString, ceil($padLength / mb_strlen($padString))) . $string,
            'right' => $string . str_repeat($padString, ceil($padLength / mb_strlen($padString))),
            'both' => $this->padBoth($string, $length, $padString),
            default => $string . str_repeat($padString, ceil($padLength / mb_strlen($padString)))
        };
    }

    /**
     * Transform case
     */
    private function caseTransform(string $string, string $type): array
    {
        $result = match($type) {
            'lower', 'lowercase' => mb_strtolower($string, 'UTF-8'),
            'upper', 'uppercase' => mb_strtoupper($string, 'UTF-8'),
            'title', 'titlecase' => mb_convert_case($string, MB_CASE_TITLE, 'UTF-8'),
            'sentence' => $this->sentenceCase($string),
            'camel', 'camelcase' => $this->camelCase($string),
            'pascal', 'pascalcase' => $this->pascalCase($string),
            'snake', 'snakecase' => $this->snakeCase($string),
            'kebab', 'kebabcase' => $this->kebabCase($string),
            'swap', 'swapcase' => $this->swapCase($string),
            default => $string
        };

        return [
            'result' => $result,
            'original' => $string,
            'type' => $type
        ];
    }

    /**
     * Reverse string (UTF-8 safe)
     */
    private function reverse(string $string): string
    {
        $chars = mb_str_split($string, 1, 'UTF-8');
        return implode('', array_reverse($chars));
    }

    /**
     * Check if string contains needle
     */
    private function contains(string $string, string $needle, bool $caseSensitive = true): array
    {
        if (empty($needle)) {
            return ['contains' => true, 'position' => 0];
        }

        $position = $caseSensitive 
            ? mb_strpos($string, $needle, 0, 'UTF-8')
            : mb_stripos($string, $needle, 0, 'UTF-8');

        return [
            'contains' => $position !== false,
            'position' => $position !== false ? $position : -1,
            'case_sensitive' => $caseSensitive,
            'needle' => $needle,
            'haystack_length' => mb_strlen($string)
        ];
    }

    /**
     * Check if string starts with prefix
     */
    private function startsWith(string $string, string $prefix, bool $caseSensitive = true): bool
    {
        if (empty($prefix)) {
            return true;
        }

        $stringStart = mb_substr($string, 0, mb_strlen($prefix), 'UTF-8');
        
        return $caseSensitive 
            ? $stringStart === $prefix
            : mb_strtolower($stringStart, 'UTF-8') === mb_strtolower($prefix, 'UTF-8');
    }

    /**
     * Check if string ends with suffix
     */
    private function endsWith(string $string, string $suffix, bool $caseSensitive = true): bool
    {
        if (empty($suffix)) {
            return true;
        }

        $stringEnd = mb_substr($string, -mb_strlen($suffix), null, 'UTF-8');
        
        return $caseSensitive
            ? $stringEnd === $suffix
            : mb_strtolower($stringEnd, 'UTF-8') === mb_strtolower($suffix, 'UTF-8');
    }

    /**
     * Repeat string multiple times
     */
    private function repeat(string $string, int $times): array
    {
        if ($times < 0) {
            throw new InvalidArgumentException('Repeat times must be non-negative');
        }

        $result = str_repeat($string, $times);
        
        return [
            'result' => $result,
            'original' => $string,
            'times' => $times,
            'length' => mb_strlen($result)
        ];
    }

    /**
     * Truncate string to specified length
     */
    private function truncate(string $string, int $length, string $suffix = '...'): array
    {
        if ($length < 0) {
            throw new InvalidArgumentException('Truncate length must be non-negative');
        }

        $originalLength = mb_strlen($string, 'UTF-8');
        
        if ($originalLength <= $length) {
            return [
                'result' => $string,
                'truncated' => false,
                'original_length' => $originalLength,
                'target_length' => $length
            ];
        }

        $suffixLength = mb_strlen($suffix, 'UTF-8');
        $truncateLength = max(0, $length - $suffixLength);
        
        $result = mb_substr($string, 0, $truncateLength, 'UTF-8') . $suffix;

        return [
            'result' => $result,
            'truncated' => true,
            'original_length' => $originalLength,
            'result_length' => mb_strlen($result),
            'characters_removed' => $originalLength - $truncateLength,
            'suffix' => $suffix
        ];
    }

    /**
     * Count words in string
     */
    private function wordCount(string $string): array
    {
        $string = trim($string);
        
        if (empty($string)) {
            return [
                'words' => 0,
                'characters' => 0,
                'characters_no_spaces' => 0,
                'sentences' => 0,
                'paragraphs' => 0
            ];
        }

        $words = str_word_count($string, 0);
        $characters = mb_strlen($string, 'UTF-8');
        $charactersNoSpaces = mb_strlen(str_replace(' ', '', $string), 'UTF-8');
        $sentences = preg_match_all('/[.!?]+/', $string);
        $paragraphs = count(array_filter(explode("\n\n", $string)));

        return [
            'words' => $words,
            'characters' => $characters,
            'characters_no_spaces' => $charactersNoSpaces,
            'sentences' => $sentences,
            'paragraphs' => max(1, $paragraphs),
            'average_word_length' => $words > 0 ? round($charactersNoSpaces / $words, 2) : 0
        ];
    }

    /**
     * Count occurrences of character
     */
    private function charCount(string $string, string $char): array
    {
        if (mb_strlen($char, 'UTF-8') !== 1) {
            throw new InvalidArgumentException('Character must be exactly one character');
        }

        $count = mb_substr_count($string, $char, 'UTF-8');
        $positions = [];
        
        $offset = 0;
        while (($pos = mb_strpos($string, $char, $offset, 'UTF-8')) !== false) {
            $positions[] = $pos;
            $offset = $pos + 1;
        }

        return [
            'count' => $count,
            'character' => $char,
            'positions' => $positions,
            'string_length' => mb_strlen($string),
            'percentage' => mb_strlen($string) > 0 ? round(($count / mb_strlen($string)) * 100, 2) : 0
        ];
    }

    /**
     * Create URL-friendly slug
     */
    private function slug(string $string, string $separator = '-'): array
    {
        $original = $string;
        
        // Convert to lowercase
        $slug = mb_strtolower($string, 'UTF-8');
        
        // Replace accented characters
        $slug = $this->removeAccents($slug);
        
        // Remove special characters
        $slug = preg_replace('/[^a-z0-9\s-]/', '', $slug);
        
        // Replace whitespace and multiple separators
        $slug = preg_replace('/[\s-]+/', $separator, $slug);
        
        // Trim separators from ends
        $slug = trim($slug, $separator);

        return [
            'slug' => $slug,
            'original' => $original,
            'separator' => $separator,
            'length' => mb_strlen($slug)
        ];
    }

    /**
     * Generate random string
     */
    private function random(int $length, string $charset = 'alphanumeric'): array
    {
        if ($length <= 0) {
            throw new InvalidArgumentException('Length must be positive');
        }

        $chars = match($charset) {
            'numeric' => '0123456789',
            'alpha' => 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ',
            'alphanumeric' => 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789',
            'lower' => 'abcdefghijklmnopqrstuvwxyz',
            'upper' => 'ABCDEFGHIJKLMNOPQRSTUVWXYZ',
            'hex' => '0123456789abcdef',
            'safe' => 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_',
            default => $charset
        };

        $result = '';
        $charLength = strlen($chars);
        
        for ($i = 0; $i < $length; $i++) {
            $result .= $chars[random_int(0, $charLength - 1)];
        }

        return [
            'result' => $result,
            'length' => $length,
            'charset' => $charset,
            'charset_size' => $charLength
        ];
    }

    /**
     * Calculate string similarity
     */
    private function similarity(string $string1, string $string2): array
    {
        similar_text($string1, $string2, $percent);
        
        $levenshtein = levenshtein($string1, $string2);
        $jaro = $this->jaroSimilarity($string1, $string2);
        
        return [
            'similar_text_percent' => round($percent, 2),
            'levenshtein_distance' => $levenshtein,
            'jaro_similarity' => round($jaro, 4),
            'max_length' => max(mb_strlen($string1), mb_strlen($string2)),
            'min_length' => min(mb_strlen($string1), mb_strlen($string2))
        ];
    }

    /**
     * Calculate edit distance
     */
    private function distance(string $string1, string $string2): array
    {
        $levenshtein = levenshtein($string1, $string2);
        $hamming = $this->hammingDistance($string1, $string2);
        
        return [
            'levenshtein' => $levenshtein,
            'hamming' => $hamming,
            'string1_length' => mb_strlen($string1),
            'string2_length' => mb_strlen($string2)
        ];
    }

    /**
     * Normalize Unicode string
     */
    private function normalize(string $string, string $form = 'NFC'): array
    {
        if (!class_exists('Normalizer')) {
            return [
                'result' => $string,
                'normalized' => false,
                'error' => 'Normalizer class not available'
            ];
        }

        $form = match(strtoupper($form)) {
            'NFC' => \Normalizer::FORM_C,
            'NFD' => \Normalizer::FORM_D,
            'NFKC' => \Normalizer::FORM_KC,
            'NFKD' => \Normalizer::FORM_KD,
            default => \Normalizer::FORM_C
        };

        $result = \Normalizer::normalize($string, $form);
        
        return [
            'result' => $result,
            'original' => $string,
            'form' => $form,
            'normalized' => $result !== $string
        ];
    }

    /**
     * Encode string
     */
    private function encode(string $string, string $encoding): array
    {
        $result = match(strtolower($encoding)) {
            'base64' => base64_encode($string),
            'url' => urlencode($string),
            'html' => htmlentities($string, ENT_QUOTES, 'UTF-8'),
            'json' => json_encode($string),
            'hex' => bin2hex($string),
            'rot13' => str_rot13($string),
            default => throw new InvalidArgumentException("Unsupported encoding: {$encoding}")
        };

        return [
            'result' => $result,
            'original' => $string,
            'encoding' => $encoding,
            'original_length' => mb_strlen($string),
            'encoded_length' => mb_strlen($result)
        ];
    }

    /**
     * Decode string
     */
    private function decode(string $string, string $encoding): array
    {
        try {
            $result = match(strtolower($encoding)) {
                'base64' => base64_decode($string, true),
                'url' => urldecode($string),
                'html' => html_entity_decode($string, ENT_QUOTES, 'UTF-8'),
                'json' => json_decode($string, true),
                'hex' => hex2bin($string),
                'rot13' => str_rot13($string),
                default => throw new InvalidArgumentException("Unsupported encoding: {$encoding}")
            };

            if ($result === false) {
                throw new InvalidArgumentException("Failed to decode string");
            }

            return [
                'result' => $result,
                'original' => $string,
                'encoding' => $encoding,
                'success' => true
            ];
        } catch (\Exception $e) {
            return [
                'result' => null,
                'original' => $string,
                'encoding' => $encoding,
                'success' => false,
                'error' => $e->getMessage()
            ];
        }
    }

    /**
     * Format string template
     */
    private function format(string $template, array $values): array
    {
        $original = $template;
        $result = $template;
        $replacements = 0;

        // Simple placeholder replacement {key}
        foreach ($values as $key => $value) {
            $placeholder = '{' . $key . '}';
            if (str_contains($result, $placeholder)) {
                $result = str_replace($placeholder, (string) $value, $result);
                $replacements++;
            }
        }

        // sprintf style formatting
        if (str_contains($template, '%')) {
            $result = vsprintf($template, array_values($values));
        }

        return [
            'result' => $result,
            'template' => $original,
            'values' => $values,
            'replacements' => $replacements
        ];
    }

    // Helper methods

    private function padBoth(string $string, int $length, string $padString): string
    {
        $currentLength = mb_strlen($string, 'UTF-8');
        $totalPad = $length - $currentLength;
        
        $leftPad = intval($totalPad / 2);
        $rightPad = $totalPad - $leftPad;
        
        return str_repeat($padString, $leftPad) . $string . str_repeat($padString, $rightPad);
    }

    private function sentenceCase(string $string): string
    {
        return mb_strtoupper(mb_substr($string, 0, 1, 'UTF-8'), 'UTF-8') . 
               mb_strtolower(mb_substr($string, 1, null, 'UTF-8'), 'UTF-8');
    }

    private function camelCase(string $string): string
    {
        $words = preg_split('/[\s_-]+/', mb_strtolower($string, 'UTF-8'));
        $result = array_shift($words);
        
        foreach ($words as $word) {
            $result .= mb_strtoupper(mb_substr($word, 0, 1, 'UTF-8'), 'UTF-8') . 
                      mb_substr($word, 1, null, 'UTF-8');
        }
        
        return $result;
    }

    private function pascalCase(string $string): string
    {
        $camel = $this->camelCase($string);
        return mb_strtoupper(mb_substr($camel, 0, 1, 'UTF-8'), 'UTF-8') . 
               mb_substr($camel, 1, null, 'UTF-8');
    }

    private function snakeCase(string $string): string
    {
        $string = preg_replace('/([a-z])([A-Z])/', '$1_$2', $string);
        return mb_strtolower(preg_replace('/[\s-]+/', '_', $string), 'UTF-8');
    }

    private function kebabCase(string $string): string
    {
        $string = preg_replace('/([a-z])([A-Z])/', '$1-$2', $string);
        return mb_strtolower(preg_replace('/[\s_]+/', '-', $string), 'UTF-8');
    }

    private function swapCase(string $string): string
    {
        $result = '';
        $chars = mb_str_split($string, 1, 'UTF-8');
        
        foreach ($chars as $char) {
            if (mb_strtolower($char, 'UTF-8') === $char) {
                $result .= mb_strtoupper($char, 'UTF-8');
            } else {
                $result .= mb_strtolower($char, 'UTF-8');
            }
        }
        
        return $result;
    }

    private function removeAccents(string $string): string
    {
        $accents = [
            'à' => 'a', 'á' => 'a', 'â' => 'a', 'ã' => 'a', 'ä' => 'a', 'å' => 'a',
            'è' => 'e', 'é' => 'e', 'ê' => 'e', 'ë' => 'e',
            'ì' => 'i', 'í' => 'i', 'î' => 'i', 'ï' => 'i',
            'ò' => 'o', 'ó' => 'o', 'ô' => 'o', 'õ' => 'o', 'ö' => 'o',
            'ù' => 'u', 'ú' => 'u', 'û' => 'u', 'ü' => 'u',
            'ñ' => 'n', 'ç' => 'c'
        ];
        
        return strtr($string, $accents);
    }

    private function strReplaceLimit(string $search, string $replace, string $subject, int $limit, &$count): string
    {
        $count = 0;
        $pos = 0;
        
        while ($count < $limit && ($pos = strpos($subject, $search, $pos)) !== false) {
            $subject = substr_replace($subject, $replace, $pos, strlen($search));
            $pos += strlen($replace);
            $count++;
        }
        
        return $subject;
    }

    private function strIreplaceLimit(string $search, string $replace, string $subject, int $limit, &$count): string
    {
        $count = 0;
        $pos = 0;
        
        while ($count < $limit && ($pos = stripos($subject, $search, $pos)) !== false) {
            $subject = substr_replace($subject, $replace, $pos, strlen($search));
            $pos += strlen($replace);
            $count++;
        }
        
        return $subject;
    }

    private function jaroSimilarity(string $s1, string $s2): float
    {
        $len1 = mb_strlen($s1);
        $len2 = mb_strlen($s2);
        
        if ($len1 === 0 && $len2 === 0) return 1.0;
        if ($len1 === 0 || $len2 === 0) return 0.0;
        
        $matchWindow = max($len1, $len2) / 2 - 1;
        $matchWindow = max(0, $matchWindow);
        
        $s1Matches = array_fill(0, $len1, false);
        $s2Matches = array_fill(0, $len2, false);
        
        $matches = 0;
        $transpositions = 0;
        
        // Identify matches
        for ($i = 0; $i < $len1; $i++) {
            $start = max(0, $i - $matchWindow);
            $end = min($i + $matchWindow + 1, $len2);
            
            for ($j = $start; $j < $end; $j++) {
                if ($s2Matches[$j] || mb_substr($s1, $i, 1) !== mb_substr($s2, $j, 1)) {
                    continue;
                }
                
                $s1Matches[$i] = true;
                $s2Matches[$j] = true;
                $matches++;
                break;
            }
        }
        
        if ($matches === 0) return 0.0;
        
        // Count transpositions
        $k = 0;
        for ($i = 0; $i < $len1; $i++) {
            if (!$s1Matches[$i]) continue;
            
            while (!$s2Matches[$k]) $k++;
            
            if (mb_substr($s1, $i, 1) !== mb_substr($s2, $k, 1)) {
                $transpositions++;
            }
            $k++;
        }
        
        return ($matches / $len1 + $matches / $len2 + ($matches - $transpositions / 2) / $matches) / 3;
    }

    private function hammingDistance(string $s1, string $s2): ?int
    {
        $len1 = mb_strlen($s1);
        $len2 = mb_strlen($s2);
        
        if ($len1 !== $len2) {
            return null; // Hamming distance requires equal length strings
        }
        
        $distance = 0;
        for ($i = 0; $i < $len1; $i++) {
            if (mb_substr($s1, $i, 1) !== mb_substr($s2, $i, 1)) {
                $distance++;
            }
        }
        
        return $distance;
    }
} 