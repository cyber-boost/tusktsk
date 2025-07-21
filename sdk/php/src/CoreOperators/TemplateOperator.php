<?php

namespace TuskLang\CoreOperators;

use TuskLang\CoreOperators\BaseOperator;

/**
 * Template Operator for template processing and rendering
 */
class TemplateOperator extends BaseOperator
{
    public function execute(array $config, array $context = []): mixed
    {
        $action = $config['action'] ?? 'render';
        $data = $config['data'] ?? '';
        $options = $config['options'] ?? [];

        try {
            switch ($action) {
                case 'render':
                    return $this->renderTemplate($data, $options, $context);
                case 'compile':
                    return $this->compileTemplate($data, $options);
                case 'validate':
                    return $this->validateTemplate($data, $options);
                case 'extract':
                    return $this->extractVariables($data, $options);
                case 'transform':
                    return $this->transformTemplate($data, $options);
                case 'merge':
                    return $this->mergeTemplates($data, $options);
                default:
                    throw new \Exception("Unknown Template action: $action");
            }
        } catch (\Exception $e) {
            error_log("Template Operator error: " . $e->getMessage());
            throw $e;
        }
    }

    /**
     * Render template with variables
     */
    private function renderTemplate(string $template, array $options, array $context): string
    {
        $variables = $options['variables'] ?? [];
        $engine = $options['engine'] ?? 'simple'; // simple, twig, blade
        $filters = $options['filters'] ?? [];
        $functions = $options['functions'] ?? [];

        // Merge context variables
        $allVariables = array_merge($context, $variables);

        switch ($engine) {
            case 'simple':
                return $this->renderSimpleTemplate($template, $allVariables, $filters, $functions);
            case 'twig':
                return $this->renderTwigTemplate($template, $allVariables, $options);
            case 'blade':
                return $this->renderBladeTemplate($template, $allVariables, $options);
            default:
                throw new \Exception("Unknown template engine: $engine");
        }
    }

    /**
     * Compile template to optimized format
     */
    private function compileTemplate(string $template, array $options): array
    {
        $engine = $options['engine'] ?? 'simple';
        $optimize = $options['optimize'] ?? true;

        $compiled = [
            'engine' => $engine,
            'template' => $template,
            'variables' => $this->extractVariables($template, $options),
            'compiled' => null,
            'optimized' => false
        ];

        if ($optimize) {
            $compiled['compiled'] = $this->optimizeTemplate($template, $options);
            $compiled['optimized'] = true;
        }

        return $compiled;
    }

    /**
     * Validate template syntax
     */
    private function validateTemplate(string $template, array $options): array
    {
        $engine = $options['engine'] ?? 'simple';
        $strict = $options['strict'] ?? false;

        $errors = [];
        $warnings = [];

        try {
            // Check for basic syntax errors
            if (strpos($template, '{{') !== false && strpos($template, '}}') === false) {
                $errors[] = "Unclosed template variable";
            }

            if (strpos($template, '{%') !== false && strpos($template, '%}') === false) {
                $errors[] = "Unclosed template tag";
            }

            // Check for balanced braces
            $openBraces = substr_count($template, '{{');
            $closeBraces = substr_count($template, '}}');
            if ($openBraces !== $closeBraces) {
                $errors[] = "Unbalanced template braces: $openBraces opening, $closeBraces closing";
            }

            // Check for valid variable names
            preg_match_all('/\{\{\s*([^}]+)\s*\}\}/', $template, $matches);
            foreach ($matches[1] as $variable) {
                $variable = trim($variable);
                if (!preg_match('/^[a-zA-Z_][a-zA-Z0-9_.]*$/', $variable)) {
                    $warnings[] = "Potentially invalid variable name: $variable";
                }
            }

            // Check for infinite loops in loops
            if (preg_match('/{%\s*for\s+.*%}.*{%\s*endfor\s*%}/s', $template)) {
                // Check for nested loops that might cause issues
                $loopCount = preg_match_all('/{%\s*for\s+/', $template);
                $endLoopCount = preg_match_all('/{%\s*endfor\s*%}/', $template);
                if ($loopCount !== $endLoopCount) {
                    $errors[] = "Unbalanced loop tags: $loopCount for, $endLoopCount endfor";
                }
            }

            // Check for undefined variables if strict mode
            if ($strict) {
                $variables = $this->extractVariables($template, $options);
                foreach ($variables as $var) {
                    if (strpos($var, '.') === false && !preg_match('/^[a-zA-Z_][a-zA-Z0-9_]*$/', $var)) {
                        $warnings[] = "Potentially undefined variable: $var";
                    }
                }
            }

        } catch (\Exception $e) {
            $errors[] = "Template validation error: " . $e->getMessage();
        }

        return [
            'valid' => empty($errors),
            'errors' => $errors,
            'warnings' => $warnings,
            'variableCount' => count($this->extractVariables($template, $options)),
            'complexity' => $this->calculateComplexity($template)
        ];
    }

    /**
     * Extract variables from template
     */
    private function extractVariables(string $template, array $options): array
    {
        $variables = [];
        
        // Extract {{ variable }} syntax
        preg_match_all('/\{\{\s*([^}]+)\s*\}\}/', $template, $matches);
        foreach ($matches[1] as $match) {
            $var = trim($match);
            // Handle filters and functions
            if (strpos($var, '|') !== false) {
                $var = trim(explode('|', $var)[0]);
            }
            if (strpos($var, '(') !== false) {
                $var = trim(explode('(', $var)[0]);
            }
            if (!empty($var) && !in_array($var, $variables)) {
                $variables[] = $var;
            }
        }

        // Extract {% for item in items %} syntax
        preg_match_all('/{%\s*for\s+([a-zA-Z_][a-zA-Z0-9_]*)\s+in\s+([a-zA-Z_][a-zA-Z0-9_.]*)\s*%}/', $template, $matches);
        foreach ($matches[2] as $match) {
            $var = trim($match);
            if (!empty($var) && !in_array($var, $variables)) {
                $variables[] = $var;
            }
        }

        // Extract {% if condition %} syntax
        preg_match_all('/{%\s*if\s+([^%]+)\s*%}/', $template, $matches);
        foreach ($matches[1] as $match) {
            $condition = trim($match);
            // Extract variables from conditions
            preg_match_all('/[a-zA-Z_][a-zA-Z0-9_.]*/', $condition, $varMatches);
            foreach ($varMatches[0] as $var) {
                if (!in_array($var, ['and', 'or', 'not', 'in', 'is', 'true', 'false', 'null']) && !in_array($var, $variables)) {
                    $variables[] = $var;
                }
            }
        }

        return array_unique($variables);
    }

    /**
     * Transform template structure
     */
    private function transformTemplate(string $template, array $options): string
    {
        $transforms = $options['transforms'] ?? [];

        foreach ($transforms as $transform) {
            $type = $transform['type'] ?? '';
            $from = $transform['from'] ?? '';
            $to = $transform['to'] ?? '';
            
            switch ($type) {
                case 'replace':
                    $template = str_replace($from, $to, $template);
                    break;
                case 'regex':
                    $template = preg_replace($from, $to, $template);
                    break;
                case 'variable_rename':
                    $template = preg_replace('/\{\{\s*' . preg_quote($from, '/') . '\s*\}\}/', '{{ ' . $to . ' }}', $template);
                    break;
                case 'tag_rename':
                    $template = preg_replace('/{%\s*' . preg_quote($from, '/') . '\s/', '{% ' . $to . ' ', $template);
                    break;
            }
        }

        return $template;
    }

    /**
     * Merge multiple templates
     */
    private function mergeTemplates(array $templates, array $options): string
    {
        $strategy = $options['strategy'] ?? 'concatenate'; // concatenate, include, extend
        $separator = $options['separator'] ?? "\n";
        $baseTemplate = $options['baseTemplate'] ?? '';

        switch ($strategy) {
            case 'concatenate':
                return implode($separator, $templates);
                
            case 'include':
                $result = '';
                foreach ($templates as $template) {
                    $result .= $template . $separator;
                }
                return $result;
                
            case 'extend':
                if (empty($baseTemplate)) {
                    throw new \Exception("Base template required for extend strategy");
                }
                
                $content = implode($separator, $templates);
                return str_replace('{{ content }}', $content, $baseTemplate);
                
            default:
                throw new \Exception("Unknown merge strategy: $strategy");
        }
    }

    /**
     * Render simple template engine
     */
    private function renderSimpleTemplate(string $template, array $variables, array $filters, array $functions): string
    {
        // Replace variables
        $template = $this->replaceVariables($template, $variables);
        
        // Apply filters
        $template = $this->applyFilters($template, $filters);
        
        // Apply functions
        $template = $this->applyFunctions($template, $functions);
        
        // Handle loops
        $template = $this->processLoops($template, $variables);
        
        // Handle conditionals
        $template = $this->processConditionals($template, $variables);
        
        return $template;
    }

    /**
     * Replace variables in template
     */
    private function replaceVariables(string $template, array $variables): string
    {
        return preg_replace_callback('/\{\{\s*([^}]+)\s*\}\}/', function($matches) use ($variables) {
            $var = trim($matches[1]);
            
            // Handle nested variables (dot notation)
            if (strpos($var, '.') !== false) {
                $parts = explode('.', $var);
                $value = $variables;
                foreach ($parts as $part) {
                    if (is_array($value) && isset($value[$part])) {
                        $value = $value[$part];
                    } else {
                        return ''; // Variable not found
                    }
                }
                return $value;
            }
            
            return $variables[$var] ?? '';
        }, $template);
    }

    /**
     * Apply filters to template
     */
    private function applyFilters(string $template, array $filters): string
    {
        return preg_replace_callback('/\{\{\s*([^|]+)\s*\|\s*([^}]+)\s*\}\}/', function($matches) use ($filters) {
            $variable = trim($matches[1]);
            $filterChain = trim($matches[2]);
            
            $filterList = explode('|', $filterChain);
            $value = $variable; // This would be the actual variable value
            
            foreach ($filterList as $filter) {
                $filter = trim($filter);
                if (isset($filters[$filter])) {
                    $value = $filters[$filter]($value);
                }
            }
            
            return $value;
        }, $template);
    }

    /**
     * Apply functions to template
     */
    private function applyFunctions(string $template, array $functions): string
    {
        return preg_replace_callback('/\{\{\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*\(([^)]*)\)\s*\}\}/', function($matches) use ($functions) {
            $functionName = $matches[1];
            $arguments = trim($matches[2]);
            
            if (isset($functions[$functionName])) {
                $args = array_map('trim', explode(',', $arguments));
                return $functions[$functionName](...$args);
            }
            
            return '';
        }, $template);
    }

    /**
     * Process loops in template
     */
    private function processLoops(string $template, array $variables): string
    {
        return preg_replace_callback('/{%\s*for\s+([a-zA-Z_][a-zA-Z0-9_]*)\s+in\s+([a-zA-Z_][a-zA-Z0-9_.]*)\s*%}(.*?){%\s*endfor\s*%}/s', function($matches) use ($variables) {
            $itemVar = $matches[1];
            $arrayVar = $matches[2];
            $loopContent = $matches[3];
            
            $array = $this->getNestedValue($variables, $arrayVar);
            if (!is_array($array)) {
                return '';
            }
            
            $result = '';
            foreach ($array as $item) {
                $loopVariables = $variables;
                $loopVariables[$itemVar] = $item;
                $result .= $this->replaceVariables($loopContent, $loopVariables);
            }
            
            return $result;
        }, $template);
    }

    /**
     * Process conditionals in template
     */
    private function processConditionals(string $template, array $variables): string
    {
        return preg_replace_callback('/{%\s*if\s+([^%]+)\s*%}(.*?){%\s*endif\s*%}/s', function($matches) use ($variables) {
            $condition = trim($matches[1]);
            $content = $matches[2];
            
            if ($this->evaluateCondition($condition, $variables)) {
                return $this->replaceVariables($content, $variables);
            }
            
            return '';
        }, $template);
    }

    /**
     * Evaluate condition
     */
    private function evaluateCondition(string $condition, array $variables): bool
    {
        // Simple condition evaluation
        $condition = $this->replaceVariables($condition, $variables);
        
        // Handle basic comparisons
        if (preg_match('/^(.+)\s+(==|!=|>|<|>=|<=)\s+(.+)$/', $condition, $matches)) {
            $left = trim($matches[1]);
            $operator = $matches[2];
            $right = trim($matches[3]);
            
            switch ($operator) {
                case '==':
                    return $left == $right;
                case '!=':
                    return $left != $right;
                case '>':
                    return $left > $right;
                case '<':
                    return $left < $right;
                case '>=':
                    return $left >= $right;
                case '<=':
                    return $left <= $right;
            }
        }
        
        // Handle simple boolean values
        return !empty($condition) && $condition !== 'false' && $condition !== '0';
    }

    /**
     * Get nested value from array
     */
    private function getNestedValue(array $array, string $path)
    {
        $keys = explode('.', $path);
        $current = $array;
        
        foreach ($keys as $key) {
            if (!isset($current[$key])) {
                return null;
            }
            $current = $current[$key];
        }
        
        return $current;
    }

    /**
     * Optimize template for better performance
     */
    private function optimizeTemplate(string $template, array $options): string
    {
        // Remove unnecessary whitespace
        $template = preg_replace('/\s+/', ' ', $template);
        
        // Optimize variable access patterns
        $template = preg_replace('/\{\{\s*([^}]+)\s*\}\}/', '{{$1}}', $template);
        
        // Optimize tag patterns
        $template = preg_replace('/{%\s*([^%]+)\s*%}/', '{%$1%}', $template);
        
        return $template;
    }

    /**
     * Calculate template complexity
     */
    private function calculateComplexity(string $template): int
    {
        $complexity = 0;
        
        // Count variables
        $complexity += preg_match_all('/\{\{[^}]+\}\}/', $template);
        
        // Count loops
        $complexity += preg_match_all('/{%\s*for[^%]*%}/', $template) * 2;
        
        // Count conditionals
        $complexity += preg_match_all('/{%\s*if[^%]*%}/', $template) * 2;
        
        // Count nested structures
        $complexity += preg_match_all('/{%\s*for[^%]*%}.*?{%\s*endfor\s*%}/s', $template) * 3;
        
        return $complexity;
    }

    /**
     * Render Twig template (placeholder)
     */
    private function renderTwigTemplate(string $template, array $variables, array $options): string
    {
        // This would integrate with Twig if available
        throw new \Exception("Twig template engine not implemented");
    }

    /**
     * Render Blade template (placeholder)
     */
    private function renderBladeTemplate(string $template, array $variables, array $options): string
    {
        // This would integrate with Blade if available
        throw new \Exception("Blade template engine not implemented");
    }
} 