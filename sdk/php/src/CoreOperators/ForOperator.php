<?php
/**
 * TuskLang For Operator
 * =====================
 * Handles for loops and iteration
 */

namespace TuskLang\CoreOperators;

class ForOperator extends \TuskLang\CoreOperators\BaseOperator
{
    public function getName(): string
    {
        return 'for';
    }

    protected function executeOperator(array $config, array $context): mixed
    {
        $start = $config['start'] ?? 0;
        $end = $config['end'] ?? null;
        $step = $config['step'] ?? 1;
        $action = $config['action'] ?? null;
        $variable = $config['variable'] ?? 'i';

        if ($end === null) {
            throw new \Exception("For operator requires an 'end' parameter");
        }

        if ($action === null) {
            throw new \Exception("For operator requires an 'action' parameter");
        }

        $results = [];

        for ($i = $start; $i <= $end; $i += $step) {
            // Set the loop variable in context
            $loopContext = $context;
            $loopContext['loop_variables'] = $loopContext['loop_variables'] ?? [];
            $loopContext['loop_variables'][$variable] = $i;
            $loopContext['loop_index'] = $i;

            // Execute the action
            $result = $this->evaluateAction($action, $loopContext);
            $results[] = $result;
        }

        return $results;
    }

    private function evaluateAction($action, array $context): mixed
    {
        if (is_string($action) && str_starts_with($action, '@')) {
            // Handle nested operators
            return $this->evaluateNestedOperator($action, $context);
        }

        if (is_callable($action)) {
            return call_user_func($action, $context);
        }

        return $action;
    }

    private function evaluateNestedOperator(string $operatorString, array $context): mixed
    {
        // Parse operator string like "@operator(param1: value1, param2: value2)"
        if (preg_match('/@(\w+)\((.*)\)/', $operatorString, $matches)) {
            $operatorName = $matches[1];
            $params = $matches[2];
            
            // Parse parameters and substitute loop variables
            $config = $this->parseParametersWithSubstitution($params, $context);
            
            // Execute the nested operator
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
                
                // Remove quotes if present
                if (preg_match('/^["\'](.*)["\']$/', $value, $quoteMatches)) {
                    $value = $quoteMatches[1];
                }
                
                // Substitute loop variables
                $value = $this->substituteLoopVariables($value, $context);
                
                $config[$key] = $value;
            }
        }
        
        return $config;
    }

    private function substituteLoopVariables(string $value, array $context): string
    {
        if (isset($context['loop_variables'])) {
            foreach ($context['loop_variables'] as $varName => $varValue) {
                $value = str_replace('$' . $varName, (string)$varValue, $value);
            }
        }
        
        return $value;
    }
} 