<?php
/**
 * TuskLang Each Operator
 * ======================
 * Handles array iteration and collection processing
 */

namespace TuskLang\CoreOperators;

class EachOperator extends \TuskLang\CoreOperators\BaseOperator
{
    public function getName(): string
    {
        return 'each';
    }

    protected function executeOperator(array $config, array $context): mixed
    {
        $array = $config['array'] ?? null;
        $action = $config['action'] ?? null;
        $keyVariable = $config['key_variable'] ?? 'key';
        $valueVariable = $config['value_variable'] ?? 'value';

        if ($array === null) {
            throw new \Exception("Each operator requires an 'array' parameter");
        }

        if ($action === null) {
            throw new \Exception("Each operator requires an 'action' parameter");
        }

        if (!is_array($array)) {
            throw new \Exception("Each operator requires an array parameter");
        }

        $results = [];

        foreach ($array as $key => $value) {
            // Set iteration variables in context
            $iterationContext = $context;
            $iterationContext['iteration_variables'] = $iterationContext['iteration_variables'] ?? [];
            $iterationContext['iteration_variables'][$keyVariable] = $key;
            $iterationContext['iteration_variables'][$valueVariable] = $value;
            $iterationContext['current_key'] = $key;
            $iterationContext['current_value'] = $value;

            // Execute the action
            $result = $this->evaluateAction($action, $iterationContext);
            $results[] = $result;
        }

        return $results;
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