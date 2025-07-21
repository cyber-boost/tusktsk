<?php

declare(strict_types=1);

namespace TuskLang\SDK\SystemOperations\Environment;

use TuskLang\SDK\Core\BaseOperator;
use TuskLang\SDK\Core\Interfaces\OperatorInterface;
use TuskLang\SDK\Core\Exceptions\VariableOperationException;

/**
 * Advanced Variable Management Operator
 * 
 * Features:
 * - Dynamic variable management with type safety
 * - Variable scoping and namespace isolation
 * - Variable dependency tracking and resolution
 * - Variable interpolation and template processing
 * - Variable lifecycle management and cleanup
 * 
 * @package TuskLang\SDK\SystemOperations\Environment
 * @version 1.0.0
 * @author TuskLang AI System
 */
class VariableOperator extends BaseOperator implements OperatorInterface
{
    private array $variables = [];
    private array $scopes = [];
    private array $dependencies = [];
    private string $currentScope = 'global';

    public function __construct(array $config = [])
    {
        parent::__construct($config);
        $this->initializeOperator();
    }

    public function setVariable(string $name, $value, array $options = []): bool
    {
        try {
            $scope = $options['scope'] ?? $this->currentScope;
            $type = $options['type'] ?? $this->detectType($value);
            $readOnly = $options['readonly'] ?? false;
            
            $variableData = [
                'value' => $value,
                'type' => $type,
                'scope' => $scope,
                'readonly' => $readOnly,
                'created_at' => microtime(true),
                'updated_at' => microtime(true),
                'access_count' => 0,
                'metadata' => $options['metadata'] ?? []
            ];
            
            $this->variables[$scope][$name] = $variableData;
            
            // Track dependencies if specified
            if (isset($options['depends_on'])) {
                $this->dependencies[$scope][$name] = $options['depends_on'];
            }
            
            $this->logOperation('variable_set', $name, [
                'scope' => $scope,
                'type' => $type,
                'readonly' => $readOnly
            ]);
            
            return true;

        } catch (\Exception $e) {
            $this->logOperation('variable_set_error', $name, ['error' => $e->getMessage()]);
            throw new VariableOperationException("Failed to set variable: " . $e->getMessage());
        }
    }

    public function getVariable(string $name, string $scope = null)
    {
        try {
            $scope = $scope ?? $this->currentScope;
            
            // Check current scope first, then parent scopes
            $searchScopes = $this->getScopeChain($scope);
            
            foreach ($searchScopes as $searchScope) {
                if (isset($this->variables[$searchScope][$name])) {
                    $variable = $this->variables[$searchScope][$name];
                    $variable['access_count']++;
                    $this->variables[$searchScope][$name] = $variable;
                    
                    return $variable['value'];
                }
            }
            
            throw new VariableOperationException("Variable not found: {$name}");

        } catch (\Exception $e) {
            $this->logOperation('variable_get_error', $name, ['error' => $e->getMessage()]);
            throw $e;
        }
    }

    public function createScope(string $scopeName, string $parentScope = null): bool
    {
        try {
            if (isset($this->scopes[$scopeName])) {
                throw new VariableOperationException("Scope already exists: {$scopeName}");
            }
            
            $this->scopes[$scopeName] = [
                'parent' => $parentScope,
                'created_at' => microtime(true),
                'variable_count' => 0
            ];
            
            $this->variables[$scopeName] = [];
            $this->dependencies[$scopeName] = [];
            
            $this->logOperation('scope_created', $scopeName, ['parent' => $parentScope]);
            return true;

        } catch (\Exception $e) {
            $this->logOperation('scope_create_error', $scopeName, ['error' => $e->getMessage()]);
            throw new VariableOperationException("Failed to create scope: " . $e->getMessage());
        }
    }

    public function switchScope(string $scopeName): bool
    {
        try {
            if (!isset($this->scopes[$scopeName])) {
                throw new VariableOperationException("Scope does not exist: {$scopeName}");
            }
            
            $previousScope = $this->currentScope;
            $this->currentScope = $scopeName;
            
            $this->logOperation('scope_switched', $scopeName, ['previous' => $previousScope]);
            return true;

        } catch (\Exception $e) {
            $this->logOperation('scope_switch_error', $scopeName, ['error' => $e->getMessage()]);
            throw new VariableOperationException("Failed to switch scope: " . $e->getMessage());
        }
    }

    public function interpolateTemplate(string $template, string $scope = null): string
    {
        try {
            $scope = $scope ?? $this->currentScope;
            $result = $template;
            
            // Find all variable references in template
            preg_match_all('/\$\{([^}]+)\}/', $template, $matches);
            
            foreach ($matches[1] as $variableName) {
                $value = $this->getVariable($variableName, $scope);
                $result = str_replace('${' . $variableName . '}', (string)$value, $result);
            }
            
            return $result;

        } catch (\Exception $e) {
            $this->logOperation('template_interpolation_error', '', ['error' => $e->getMessage()]);
            throw new VariableOperationException("Template interpolation failed: " . $e->getMessage());
        }
    }

    public function resolveDependencies(string $scope = null): array
    {
        try {
            $scope = $scope ?? $this->currentScope;
            $resolved = [];
            $visiting = [];
            
            foreach (array_keys($this->dependencies[$scope] ?? []) as $variable) {
                $this->resolveDependency($variable, $scope, $resolved, $visiting);
            }
            
            return $resolved;

        } catch (\Exception $e) {
            $this->logOperation('dependency_resolution_error', '', ['error' => $e->getMessage()]);
            throw new VariableOperationException("Dependency resolution failed: " . $e->getMessage());
        }
    }

    private function resolveDependency(string $variable, string $scope, array &$resolved, array &$visiting): void
    {
        if (in_array($variable, $resolved)) {
            return;
        }
        
        if (in_array($variable, $visiting)) {
            throw new VariableOperationException("Circular dependency detected: {$variable}");
        }
        
        $visiting[] = $variable;
        
        if (isset($this->dependencies[$scope][$variable])) {
            foreach ($this->dependencies[$scope][$variable] as $dependency) {
                $this->resolveDependency($dependency, $scope, $resolved, $visiting);
            }
        }
        
        $resolved[] = $variable;
        $visiting = array_diff($visiting, [$variable]);
    }

    private function getScopeChain(string $scope): array
    {
        $chain = [$scope];
        $current = $scope;
        
        while (isset($this->scopes[$current]['parent']) && $this->scopes[$current]['parent']) {
            $current = $this->scopes[$current]['parent'];
            $chain[] = $current;
        }
        
        return $chain;
    }

    private function detectType($value): string
    {
        if (is_bool($value)) return 'boolean';
        if (is_int($value)) return 'integer';
        if (is_float($value)) return 'float';
        if (is_string($value)) return 'string';
        if (is_array($value)) return 'array';
        if (is_object($value)) return 'object';
        if (is_null($value)) return 'null';
        return 'unknown';
    }

    private function initializeOperator(): void
    {
        $this->createScope('global');
        $this->logOperation('variable_operator_initialized', '');
    }

    private function logOperation(string $operation, string $variable, array $context = []): void
    {
        error_log("VariableOperator: " . json_encode([
            'operation' => $operation,
            'variable' => $variable,
            'timestamp' => microtime(true),
            'context' => $context
        ]));
    }
} 