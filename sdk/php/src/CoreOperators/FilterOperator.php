<?php
/**
 * TuskLang Filter Operator
 * ========================
 * Handles array filtering and conditional selection
 */

namespace TuskLang\CoreOperators;

class FilterOperator extends \TuskLang\CoreOperators\BaseOperator
{
    public function getName(): string
    {
        return 'filter';
    }

    protected function executeOperator(array $config, array $context): mixed
    {
        $array = $config['array'] ?? null;
        $condition = $config['condition'] ?? null;
        $keyVariable = $config['key_variable'] ?? 'key';
        $valueVariable = $config['value_variable'] ?? 'value';

        if ($array === null) {
            throw new \Exception("Filter operator requires an 'array' parameter");
        }

        if ($condition === null) {
            throw new \Exception("Filter operator requires a 'condition' parameter");
        }

        if (!is_array($array)) {
            throw new \Exception("Filter operator requires an array parameter");
        }

        $filtered = [];

        foreach ($array as $key => $value) {
            // Set iteration variables in context
            $filterContext = $context;
            $filterContext['iteration_variables'] = $filterContext['iteration_variables'] ?? [];
            $filterContext['iteration_variables'][$keyVariable] = $key;
            $filterContext['iteration_variables'][$valueVariable] = $value;
            $filterContext['current_key'] = $key;
            $filterContext['current_value'] = $value;

            // Evaluate the condition
            if ($this->evaluateCondition($condition, $filterContext)) {
                $filtered[$key] = $value;
            }
        }

        return $filtered;
    }

    private function evaluateCondition($condition, array $context): bool
    {
        if (is_bool($condition)) {
            return $condition;
        }

        if (is_string($condition) && str_starts_with($condition, '@')) {
            $result = $this->evaluateNestedOperator($condition, $context);
            return (bool)$result;
        }

        if (is_callable($condition)) {
            return (bool)call_user_func($condition, $context);
        }

        return (bool)$condition;
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
                
                $value = $this->substituteIterationVariables($value, $context);
                $config[$key] = $value;
            }
        }
        
        return $config;
    }

    private function substituteIterationVariables(string $value, array $context): string
    {
        if (isset($context['iteration_variables'])) {
            foreach ($context['iteration_variables'] as $varName => $varValue) {
                $value = str_replace('$' . $varName, (string)$varValue, $value);
            }
        }
        
        if (isset($context['current_key'])) {
            $value = str_replace('$current_key', (string)$context['current_key'], $value);
        }
        
        if (isset($context['current_value'])) {
            $value = str_replace('$current_value', (string)$context['current_value'], $value);
        }
        
        return $value;
    }
} 