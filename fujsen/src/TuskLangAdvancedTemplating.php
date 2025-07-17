<?php
/**
 * 🎨 TuskLangAdvancedTemplating - Advanced Template Engine
 * ========================================================
 * "Templates that think, loop, include, and expand like magic"
 * 
 * Features:
 * - Conditional compilation (@if, @unless, @switch)
 * - Loop constructs (@foreach, @while, @for)
 * - Include/import system (@include, @import)
 * - Macro definitions and expansion (@macro, @call)
 * - Integration with peanu.tsk and plugin system
 * - Performance optimized with caching
 * 
 * Strong. Secure. Scalable. 🐘🎨
 */

namespace TuskLang;

use TuskPHP\Utils\PeanuConfig;
use TuskPHP\Utils\TuskLang;

class TuskLangAdvancedTemplating
{
    private static $instance = null;
    private $macros = [];
    private $includes = [];
    private $templateCache = [];
    private $configManager;
    private $context = [];
    
    // Template directives
    const DIRECTIVES = [
        'if', 'unless', 'switch', 'case', 'default',
        'foreach', 'while', 'for', 'break', 'continue',
        'include', 'import', 'macro', 'call', 'endmacro'
    ];
    
    // Cache settings
    const CACHE_TTL = 300; // 5 minutes
    const MAX_RECURSION = 10;
    
    private function __construct()
    {
        $this->configManager = PeanuConfig::getInstance();
        $this->loadBuiltinMacros();
    }
    
    public static function getInstance(): self
    {
        if (self::$instance === null) {
            self::$instance = new self();
        }
        return self::$instance;
    }
    
    /**
     * Process advanced template with all directives
     */
    public function processTemplate(string $content, array $context = [], int $recursionLevel = 0): string
    {
        if ($recursionLevel > self::MAX_RECURSION) {
            throw new \Exception("Template recursion limit exceeded");
        }
        
        $this->context = array_merge($this->context, $context);
        
        // Process in order: includes, macros, conditionals, loops
        $content = $this->processIncludes($content, $recursionLevel);
        $content = $this->processMacros($content);
        $content = $this->processConditionals($content);
        $content = $this->processLoops($content);
        
        return $content;
    }
    
    /**
     * Conditional Compilation: @if, @unless, @switch
     */
    private function processConditionals(string $content): string
    {
        // Process @if directives
        $content = $this->processIfDirectives($content);
        
        // Process @unless directives
        $content = $this->processUnlessDirectives($content);
        
        // Process @switch directives
        $content = $this->processSwitchDirectives($content);
        
        return $content;
    }
    
    /**
     * Process @if directives
     */
    private function processIfDirectives(string $content): string
    {
        $pattern = '/@if\s*\(\s*(.+?)\s*\)\s*\n(.*?)(?=@elseif|@else|@endif|$)/s';
        
        return preg_replace_callback($pattern, function($matches) {
            $condition = trim($matches[1]);
            $trueContent = $matches[2];
            
            if ($this->evaluateCondition($condition)) {
                return $trueContent;
            }
            
            return '';
        }, $content);
    }
    
    /**
     * Process @unless directives (opposite of @if)
     */
    private function processUnlessDirectives(string $content): string
    {
        $pattern = '/@unless\s*\(\s*(.+?)\s*\)\s*\n(.*?)(?=@endunless|$)/s';
        
        return preg_replace_callback($pattern, function($matches) {
            $condition = trim($matches[1]);
            $content = $matches[2];
            
            if (!$this->evaluateCondition($condition)) {
                return $content;
            }
            
            return '';
        }, $content);
    }
    
    /**
     * Process @switch directives
     */
    private function processSwitchDirectives(string $content): string
    {
        $pattern = '/@switch\s*\(\s*(.+?)\s*\)\s*\n(.*?)(?=@endswitch|$)/s';
        
        return preg_replace_callback($pattern, function($matches) {
            $switchValue = $this->evaluateExpression(trim($matches[1]));
            $switchContent = $matches[2];
            
            // Extract @case and @default blocks
            $casePattern = '/@case\s*\(\s*(.+?)\s*\)\s*\n(.*?)(?=@case|@default|@endswitch|$)/s';
            $defaultPattern = '/@default\s*\n(.*?)(?=@endswitch|$)/s';
            
            // Find matching case
            if (preg_match_all($casePattern, $switchContent, $caseMatches, PREG_SET_ORDER)) {
                foreach ($caseMatches as $caseMatch) {
                    $caseValue = $this->evaluateExpression(trim($caseMatch[1]));
                    if ($caseValue == $switchValue) {
                        return $caseMatch[2];
                    }
                }
            }
            
            // Find default case
            if (preg_match($defaultPattern, $switchContent, $defaultMatch)) {
                return $defaultMatch[1];
            }
            
            return '';
        }, $content);
    }
    
    /**
     * Loop Constructs: @foreach, @while, @for
     */
    private function processLoops(string $content): string
    {
        // Process @foreach loops
        $content = $this->processForeachLoops($content);
        
        // Process @while loops
        $content = $this->processWhileLoops($content);
        
        // Process @for loops
        $content = $this->processForLoops($content);
        
        return $content;
    }
    
    /**
     * Process @foreach loops
     */
    private function processForeachLoops(string $content): string
    {
        $pattern = '/@foreach\s*\(\s*(.+?)\s+as\s+(.+?)(?:\s*=>\s*(.+?))?\s*\)\s*\n(.*?)(?=@endforeach|$)/s';
        
        return preg_replace_callback($pattern, function($matches) {
            $arrayExpr = trim($matches[1]);
            $valueVar = trim($matches[2]);
            $keyVar = isset($matches[3]) ? trim($matches[3]) : null;
            $loopContent = $matches[4];
            
            $array = $this->evaluateExpression($arrayExpr);
            
            if (!is_array($array)) {
                return '';
            }
            
            $result = '';
            foreach ($array as $key => $value) {
                $loopContext = $this->context;
                $loopContext[$valueVar] = $value;
                if ($keyVar) {
                    $loopContext[$keyVar] = $key;
                }
                
                // Process loop content with context
                $processedContent = $this->processTemplate($loopContent, $loopContext);
                $result .= $processedContent;
            }
            
            return $result;
        }, $content);
    }
    
    /**
     * Process @while loops
     */
    private function processWhileLoops(string $content): string
    {
        $pattern = '/@while\s*\(\s*(.+?)\s*\)\s*\n(.*?)(?=@endwhile|$)/s';
        
        return preg_replace_callback($pattern, function($matches) {
            $condition = trim($matches[1]);
            $loopContent = $matches[2];
            
            $result = '';
            $maxIterations = 1000; // Safety limit
            $iterations = 0;
            
            while ($this->evaluateCondition($condition) && $iterations < $maxIterations) {
                $processedContent = $this->processTemplate($loopContent, $this->context);
                $result .= $processedContent;
                $iterations++;
            }
            
            return $result;
        }, $content);
    }
    
    /**
     * Process @for loops
     */
    private function processForLoops(string $content): string
    {
        $pattern = '/@for\s*\(\s*(.+?)\s*;\s*(.+?)\s*;\s*(.+?)\s*\)\s*\n(.*?)(?=@endfor|$)/s';
        
        return preg_replace_callback($pattern, function($matches) {
            $init = trim($matches[1]);
            $condition = trim($matches[2]);
            $increment = trim($matches[3]);
            $loopContent = $matches[4];
            
            // Execute initialization
            $this->evaluateExpression($init);
            
            $result = '';
            $maxIterations = 1000; // Safety limit
            $iterations = 0;
            
            while ($this->evaluateCondition($condition) && $iterations < $maxIterations) {
                $processedContent = $this->processTemplate($loopContent, $this->context);
                $result .= $processedContent;
                
                // Execute increment
                $this->evaluateExpression($increment);
                $iterations++;
            }
            
            return $result;
        }, $content);
    }
    
    /**
     * Include/Import System: @include, @import
     */
    private function processIncludes(string $content, int $recursionLevel): string
    {
        // Process @include directives
        $content = $this->processIncludeDirectives($content, $recursionLevel);
        
        // Process @import directives
        $content = $this->processImportDirectives($content, $recursionLevel);
        
        return $content;
    }
    
    /**
     * Process @include directives
     */
    private function processIncludeDirectives(string $content, int $recursionLevel): string
    {
        $pattern = '/@include\s*\(\s*"([^"]+)"(?:\s*,\s*(.+?))?\s*\)/';
        
        return preg_replace_callback($pattern, function($matches) use ($recursionLevel) {
            $filePath = $matches[1];
            $includeContext = isset($matches[2]) ? $this->evaluateExpression(trim($matches[2])) : [];
            
            // Resolve file path
            $resolvedPath = $this->resolveIncludePath($filePath);
            
            if (!file_exists($resolvedPath)) {
                throw new \Exception("Include file not found: {$filePath}");
            }
            
            // Read and process included file
            $includedContent = file_get_contents($resolvedPath);
            $mergedContext = array_merge($this->context, $includeContext);
            
            return $this->processTemplate($includedContent, $mergedContext, $recursionLevel + 1);
        }, $content);
    }
    
    /**
     * Process @import directives (for plugin imports)
     */
    private function processImportDirectives(string $content, int $recursionLevel): string
    {
        $pattern = '/@import\s*\(\s*"([^"]+)"(?:\s*,\s*(.+?))?\s*\)/';
        
        return preg_replace_callback($pattern, function($matches) use ($recursionLevel) {
            $pluginPath = $matches[1];
            $importContext = isset($matches[2]) ? $this->evaluateExpression(trim($matches[2])) : [];
            
            // Resolve plugin path
            $resolvedPath = $this->resolvePluginPath($pluginPath);
            
            if (!file_exists($resolvedPath)) {
                throw new \Exception("Import file not found: {$pluginPath}");
            }
            
            // Read and process imported file
            $importedContent = file_get_contents($resolvedPath);
            $mergedContext = array_merge($this->context, $importContext);
            
            return $this->processTemplate($importedContent, $mergedContext, $recursionLevel + 1);
        }, $content);
    }
    
    /**
     * Macro Definitions and Expansion: @macro, @call
     */
    private function processMacros(string $content): string
    {
        // First, extract macro definitions
        $content = $this->extractMacroDefinitions($content);
        
        // Then, expand macro calls
        $content = $this->expandMacroCalls($content);
        
        return $content;
    }
    
    /**
     * Extract macro definitions
     */
    private function extractMacroDefinitions(string $content): string
    {
        $pattern = '/@macro\s+(\w+)\s*\(\s*(.*?)\s*\)\s*\n(.*?)(?=@endmacro|$)/s';
        
        return preg_replace_callback($pattern, function($matches) {
            $macroName = $matches[1];
            $params = $this->parseMacroParameters($matches[2]);
            $macroContent = $matches[3];
            
            $this->macros[$macroName] = [
                'params' => $params,
                'content' => $macroContent
            ];
            
            // Remove macro definition from output
            return '';
        }, $content);
    }
    
    /**
     * Expand macro calls
     */
    private function expandMacroCalls(string $content): string
    {
        $pattern = '/@call\s+(\w+)\s*\(\s*(.*?)\s*\)/';
        
        return preg_replace_callback($pattern, function($matches) {
            $macroName = $matches[1];
            $arguments = $this->parseMacroArguments($matches[2]);
            
            if (!isset($this->macros[$macroName])) {
                throw new \Exception("Macro not found: {$macroName}");
            }
            
            $macro = $this->macros[$macroName];
            $macroContent = $macro['content'];
            
            // Replace parameters with arguments
            foreach ($macro['params'] as $index => $param) {
                $value = isset($arguments[$index]) ? $arguments[$index] : '';
                $macroContent = str_replace('${' . $param . '}', $value, $macroContent);
            }
            
            return $macroContent;
        }, $content);
    }
    
    /**
     * Evaluate conditions for @if, @unless, @while
     */
    private function evaluateCondition(string $condition): bool
    {
        $condition = trim($condition);
        
        // Handle common conditions
        if ($condition === 'true' || $condition === '1') {
            return true;
        }
        
        if ($condition === 'false' || $condition === '0' || $condition === '') {
            return false;
        }
        
        // Handle variable references
        if (preg_match('/^@(\w+)$/', $condition, $matches)) {
            $varName = $matches[1];
            return !empty($this->context[$varName]);
        }
        
        // Handle comparisons
        if (preg_match('/^(.+?)\s*(==|!=|<=|>=|<|>)\s*(.+)$/', $condition, $matches)) {
            $left = $this->evaluateExpression(trim($matches[1]));
            $operator = $matches[2];
            $right = $this->evaluateExpression(trim($matches[3]));
            
            switch ($operator) {
                case '==': return $left == $right;
                case '!=': return $left != $right;
                case '<=': return $left <= $right;
                case '>=': return $left >= $right;
                case '<': return $left < $right;
                case '>': return $left > $right;
            }
        }
        
        // Handle function calls
        if (preg_match('/^(\w+)\((.+?)\)$/', $condition, $matches)) {
            $function = $matches[1];
            $args = $this->evaluateExpression(trim($matches[2]));
            
            switch ($function) {
                case 'empty': return empty($args);
                case 'isset': return isset($this->context[$args]);
                case 'count': return is_array($args) ? count($args) : 0;
            }
        }
        
        // Default: evaluate as expression
        return (bool)$this->evaluateExpression($condition);
    }
    
    /**
     * Evaluate expressions
     */
    private function evaluateExpression(string $expression)
    {
        $expression = trim($expression);
        
        // Handle string literals
        if (preg_match('/^"([^"]*)"$/', $expression, $matches)) {
            return $matches[1];
        }
        
        if (preg_match("/^'([^']*)'$/", $expression, $matches)) {
            return $matches[1];
        }
        
        // Handle variable references
        if (preg_match('/^@(\w+)$/', $expression, $matches)) {
            $varName = $matches[1];
            return $this->context[$varName] ?? null;
        }
        
        // Handle array access
        if (preg_match('/^@(\w+)\[(\d+)\]$/', $expression, $matches)) {
            $varName = $matches[1];
            $index = (int)$matches[2];
            $array = $this->context[$varName] ?? [];
            return is_array($array) && isset($array[$index]) ? $array[$index] : null;
        }
        
        // Handle numeric literals
        if (is_numeric($expression)) {
            return (float)$expression;
        }
        
        // Handle boolean literals
        if ($expression === 'true') return true;
        if ($expression === 'false') return false;
        
        // Handle arrays
        if (preg_match('/^\[(.*)\]$/', $expression, $matches)) {
            if (empty($matches[1])) {
                return [];
            }
            $items = explode(',', $matches[1]);
            return array_map(function($item) {
                return $this->evaluateExpression(trim($item));
            }, $items);
        }
        
        // Default: return as string
        return $expression;
    }
    
    /**
     * Parse macro parameters
     */
    private function parseMacroParameters(string $paramString): array
    {
        if (empty($paramString)) {
            return [];
        }
        
        return array_map('trim', explode(',', $paramString));
    }
    
    /**
     * Parse macro arguments
     */
    private function parseMacroArguments(string $argString): array
    {
        if (empty($argString)) {
            return [];
        }
        
        return array_map(function($arg) {
            return $this->evaluateExpression(trim($arg));
        }, explode(',', $argString));
    }
    
    /**
     * Resolve include file path
     */
    private function resolveIncludePath(string $filePath): string
    {
        // Check if it's an absolute path
        if (file_exists($filePath)) {
            return $filePath;
        }
        
        // Check relative to current directory
        $currentDir = getcwd();
        $relativePath = $currentDir . '/' . $filePath;
        if (file_exists($relativePath)) {
            return $relativePath;
        }
        
        // Check in templates directory
        $templatePath = $currentDir . '/templates/' . $filePath;
        if (file_exists($templatePath)) {
            return $templatePath;
        }
        
        return $filePath; // Return original if not found
    }
    
    /**
     * Resolve plugin file path
     */
    private function resolvePluginPath(string $pluginPath): string
    {
        // Check if it's an absolute path
        if (file_exists($pluginPath)) {
            return $pluginPath;
        }
        
        // Check in plugins directory
        $currentDir = getcwd();
        $pluginFilePath = $currentDir . '/plugins/installed/' . $pluginPath;
        if (file_exists($pluginFilePath)) {
            return $pluginFilePath;
        }
        
        return $pluginPath; // Return original if not found
    }
    
    /**
     * Load built-in macros
     */
    private function loadBuiltinMacros(): void
    {
        // HTML form macro
        $this->macros['form'] = [
            'params' => ['action', 'method', 'class'],
            'content' => '<form action="${action}" method="${method}" class="${class}">'
        ];
        
        // Button macro
        $this->macros['button'] = [
            'params' => ['text', 'type', 'class'],
            'content' => '<button type="${type}" class="${class}">${text}</button>'
        ];
        
        // Card macro
        $this->macros['card'] = [
            'params' => ['title', 'content', 'class'],
            'content' => '<div class="card ${class}"><h3>${title}</h3><div class="card-body">${content}</div></div>'
        ];
    }
    
    /**
     * Get all available macros
     */
    public function getMacros(): array
    {
        return array_keys($this->macros);
    }
    
    /**
     * Add custom macro
     */
    public function addMacro(string $name, array $params, string $content): void
    {
        $this->macros[$name] = [
            'params' => $params,
            'content' => $content
        ];
    }
    
    /**
     * Clear template cache
     */
    public function clearCache(): void
    {
        $this->templateCache = [];
    }
} 