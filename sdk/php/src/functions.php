<?php
/**
 * TuskLang Enhanced - Global Helper Functions
 * ===========================================
 * Convenience functions for working with TuskLang
 */

if (!function_exists('tsk_parse')) {
    /**
     * Parse TuskLang content
     */
    function tsk_parse(string $content): array
    {
        $parser = new \TuskLang\Enhanced\TuskLangEnhanced();
        return $parser->parse($content);
    }
}

if (!function_exists('tsk_parse_file')) {
    /**
     * Parse TuskLang file
     */
    function tsk_parse_file(string $filePath): array
    {
        $parser = new \TuskLang\Enhanced\TuskLangEnhanced();
        return $parser->parseFile($filePath);
    }
}

if (!function_exists('tsk_load_from_peanut')) {
    /**
     * Load configuration from peanut.tsk
     */
    function tsk_load_from_peanut(): \TuskLang\Enhanced\TuskLangEnhanced
    {
        $parser = new \TuskLang\Enhanced\TuskLangEnhanced();
        $parser->loadPeanut();
        return $parser;
    }
}

if (!function_exists('tsk_env')) {
    /**
     * Get environment variable with default
     */
    function tsk_env(string $key, mixed $default = null): mixed
    {
        return $_ENV[$key] ?? $default;
    }
}

if (!function_exists('tsk_date')) {
    /**
     * Format date with TuskLang date format
     */
    function tsk_date(string $format = 'Y-m-d H:i:s'): string
    {
        $formatMap = [
            'Y' => 'Y',
            'Y-m-d' => 'Y-m-d',
            'Y-m-d H:i:s' => 'Y-m-d H:i:s',
            'c' => 'c'
        ];
        
        $phpFormat = $formatMap[$format] ?? 'Y-m-d H:i:s';
        return date($phpFormat);
    }
}