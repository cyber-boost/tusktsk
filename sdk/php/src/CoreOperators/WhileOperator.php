<?php
/**
 * TuskLang While Operator
 * =======================
 * Handles while loops and conditional iteration
 */

namespace TuskLang\CoreOperators;

class WhileOperator extends \TuskLang\CoreOperators\BaseOperator
{
    public function getName(): string
    {
        return 'while';
    }

    protected function executeOperator(array $config, array $context): mixed
    {
        $condition = $config['condition'] ?? null;
        $action = $config['action'] ?? null;
        $maxIterations = $config['max_iterations'] ?? 1000;

        if ($condition === null) {
            throw new \Exception("While operator requires a 'condition' parameter");
        }

        if ($action === null) {
            throw new \Exception("While operator requires an 'action' parameter");
        }

        $results = [];
        $iteration = 0;

        while ($this->evaluateCondition($condition, $context) && $iteration < $maxIterations) {
            // Execute the action
            $result = $this->evaluateAction($action, $context);
            $results[] = $result;
            
            $iteration++;
            
            // Update context with iteration count
            $context['iteration'] = $iteration;
        }

        if ($iteration >= $maxIterations) {
            error_log("While operator reached maximum iterations ($maxIterations)");
        }

        return $results;
    }

    private function evaluateCondition($condition, array $context): bool
    {
        if (is_bool($condition)) {
            return $condition;
        }

        if (is_string($condition) && str_starts_with($condition, '@')) {
            // Handle nested operators that return boolean
            $result = $this->evaluateNestedOperator($condition, $context);
            return (bool)$result;
        }

        if (is_callable($condition)) {
            return (bool)call_user_func($condition, $context);
        }

        return (bool)$condition;
    }

    private function evaluateAction($action, array $context): mixed
    {
        if (is_string($action) && str_starts_with($action, '@')) {
            return $this->evaluateNestedOperator($action, $context);
        }

        if (is_callable($action)) {
            return call_user_func($action, $context);
        }

        return $action;
    }

    private function evaluateNestedOperator(string $operatorString, array $context): mixed
    {
        if (preg_match('/@(\w+)\((.*)\)/', $operatorString, $matches)) {
            $operatorName = $matches[1];
            $params = $matches[2];
            
            $config = $this->parseParametersWithSubstitution($params, $context);
            
            $registry = \TuskLang\Enhanced\OperatorRegistry::getInstance();
            return $registry->executeOperator($operatorName, $config, $context);
        }

        return $operatorString;
    }

    private function parseParametersWithSubstitution(string $params, array $context): array
    {
        $config = [];
        
        if (preg_match_all('/(\w+)\s*:\s*([^,\s]+)/', $params, $paramMatches, PREG_SET_ORDER)) {
            foreach ($paramMatches as $match) {
                $key = $match[1];
                $value = $match[2];
                
                if (preg_match('/^["\'](.*)["\']$/', $value, $quoteMatches)) {
                    $value = $quoteMatches[1];
                }
                
                $value = $this->substituteContextVariables($value, $context);
                $config[$key] = $value;
            }
        }
        
        return $config;
    }

    private function substituteContextVariables(string $value, array $context): string
    {
        if (isset($context['iteration'])) {
            $value = str_replace('$iteration', (string)$context['iteration'], $value);
        }
        
        if (isset($context['global_variables'])) {
            foreach ($context['global_variables'] as $varName => $varValue) {
                $value = str_replace('$' . $varName, (string)$varValue, $value);
            }
        }
        
        return $value;
    }
} 