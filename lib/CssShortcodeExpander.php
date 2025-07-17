<?php
/**
 * CssShortcodeExpander
 * ====================
 * Production-ready CSS shortcode expander for TuskLang and general use.
 *
 * - Generates unique shortcodes for all standard CSS properties (MDN reference)
 * - Expands shortcodes in TSK or CSS/SCSS to full CSS properties
 * - Allows optional user-defined custom mappings
 * - CLI and library compatible
 * - Follows user rules: first letter, add more letters if needed to avoid collisions
 *
 * Usage:
 *   $expanded = CssShortcodeExpander::expandShortcodes($input, $customMap = []);
 *
 * Author: Claude + TuskLang Team
 */

namespace TuskPHP\Utils;

class CssShortcodeExpander
{
    private static $cssProperties = [
        // Standard CSS properties (partial list for brevity, can be extended)
        'align-content', 'align-items', 'align-self', 'all', 'animation', 'animation-delay',
        'animation-direction', 'animation-duration', 'animation-fill-mode', 'animation-iteration-count',
        'animation-name', 'animation-play-state', 'animation-timing-function', 'backdrop-filter',
        'background', 'background-attachment', 'background-blend-mode', 'background-clip',
        'background-color', 'background-image', 'background-origin', 'background-position',
        'background-repeat', 'background-size', 'border', 'border-bottom', 'border-bottom-color',
        'border-bottom-left-radius', 'border-bottom-right-radius', 'border-bottom-style',
        'border-bottom-width', 'border-collapse', 'border-color', 'border-image', 'border-image-outset',
        'border-image-repeat', 'border-image-slice', 'border-image-source', 'border-image-width',
        'border-left', 'border-left-color', 'border-left-style', 'border-left-width', 'border-radius',
        'border-right', 'border-right-color', 'border-right-style', 'border-right-width', 'border-spacing',
        'border-style', 'border-top', 'border-top-color', 'border-top-left-radius', 'border-top-right-radius',
        'border-top-style', 'border-top-width', 'border-width', 'bottom', 'box-decoration-break',
        'box-shadow', 'box-sizing', 'break-after', 'break-before', 'break-inside', 'caption-side',
        'caret-color', 'clear', 'clip', 'clip-path', 'color', 'column-count', 'column-fill',
        'column-gap', 'column-rule', 'column-rule-color', 'column-rule-style', 'column-rule-width',
        'column-span', 'column-width', 'columns', 'content', 'counter-increment', 'counter-reset',
        'cursor', 'direction', 'display', 'empty-cells', 'filter', 'flex', 'flex-basis', 'flex-direction',
        'flex-flow', 'flex-grow', 'flex-shrink', 'flex-wrap', 'float', 'font', 'font-family',
        'font-feature-settings', 'font-kerning', 'font-language-override', 'font-size', 'font-size-adjust',
        'font-stretch', 'font-style', 'font-synthesis', 'font-variant', 'font-variant-alternates',
        'font-variant-caps', 'font-variant-east-asian', 'font-variant-ligatures', 'font-variant-numeric',
        'font-variant-position', 'font-weight', 'gap', 'grid', 'grid-area', 'grid-auto-columns',
        'grid-auto-flow', 'grid-auto-rows', 'grid-column', 'grid-column-end', 'grid-column-gap',
        'grid-column-start', 'grid-gap', 'grid-row', 'grid-row-end', 'grid-row-gap', 'grid-row-start',
        'grid-template', 'grid-template-areas', 'grid-template-columns', 'grid-template-rows', 'height',
        'hyphens', 'image-rendering', 'isolation', 'justify-content', 'justify-items', 'justify-self',
        'left', 'letter-spacing', 'line-break', 'line-height', 'list-style', 'list-style-image',
        'list-style-position', 'list-style-type', 'margin', 'margin-bottom', 'margin-left', 'margin-right',
        'margin-top', 'mask', 'mask-type', 'max-height', 'max-width', 'min-height', 'min-width', 'object-fit',
        'object-position', 'opacity', 'order', 'outline', 'outline-color', 'outline-offset', 'outline-style',
        'outline-width', 'overflow', 'overflow-wrap', 'overflow-x', 'overflow-y', 'padding', 'padding-bottom',
        'padding-left', 'padding-right', 'padding-top', 'page-break-after', 'page-break-before',
        'page-break-inside', 'perspective', 'perspective-origin', 'place-content', 'place-items',
        'place-self', 'pointer-events', 'position', 'quotes', 'resize', 'right', 'row-gap', 'scroll-behavior',
        'tab-size', 'table-layout', 'text-align', 'text-align-last', 'text-combine-upright', 'text-decoration',
        'text-decoration-color', 'text-decoration-line', 'text-decoration-style', 'text-indent',
        'text-justify', 'text-orientation', 'text-overflow', 'text-shadow', 'text-transform', 'top',
        'transform', 'transform-origin', 'transform-style', 'transition', 'transition-delay',
        'transition-duration', 'transition-property', 'transition-timing-function', 'unicode-bidi',
        'user-select', 'vertical-align', 'visibility', 'white-space', 'widows', 'width', 'word-break',
        'word-spacing', 'word-wrap', 'writing-mode', 'z-index'
    ];

    private static $shortcodeMap = null;

    /**
     * Generate unique shortcodes for all CSS properties using the naming rules.
     */
    public static function generateShortcodeMap($customMap = []) {
        if (self::$shortcodeMap !== null && empty($customMap)) {
            return self::$shortcodeMap;
        }
        $map = [];
        $used = [];
        foreach (self::$cssProperties as $prop) {
            $short = '';
            $letters = preg_replace('/[^a-z]/', '', $prop);
            for ($i = 1; $i <= strlen($letters); $i++) {
                $candidate = substr($letters, 0, $i);
                if (!isset($used[$candidate])) {
                    $short = $candidate;
                    break;
                }
            }
            // If still not unique, append numbers
            $j = 2;
            $base = $short ?: $letters;
            while (isset($used[$short])) {
                $short = $base . $j;
                $j++;
            }
            $map[$short] = $prop;
            $used[$short] = true;
        }
        // Add/override with custom mappings
        foreach ($customMap as $k => $v) {
            $map[$k] = $v;
        }
        self::$shortcodeMap = $map;
        return $map;
    }

    /**
     * Expand shortcodes in a CSS/TSK string to full CSS properties.
     * Supports both TSK/YAML and CSS/SCSS syntax.
     */
    public static function expandShortcodes($input, $customMap = []) {
        $map = self::generateShortcodeMap($customMap);
        // Build regex for all shortcodes
        $shorts = array_keys($map);
        usort($shorts, function($a, $b) { return strlen($b) - strlen($a); }); // Longest first
        $pattern = '/(?<=^|[\s;{])(' . implode('|', array_map('preg_quote', $shorts)) . ')\s*[:=]\s*/mi';
        // Replace shortcodes with full property
        $output = preg_replace_callback($pattern, function($matches) use ($map) {
            $short = $matches[1];
            $prop = $map[$short] ?? $short;
            return $prop . ': ';
        }, $input);
        return $output;
    }

    /**
     * Get the current shortcode map (for docs/UI)
     */
    public static function getShortcodeMap($customMap = []) {
        return self::generateShortcodeMap($customMap);
    }
} 