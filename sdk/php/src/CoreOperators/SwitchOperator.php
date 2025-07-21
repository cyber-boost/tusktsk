<?php
/**
 * TuskLang Switch Operator
 * ========================
 * Handles switch statements and conditional branching
 */

namespace TuskLang\CoreOperators;

class SwitchOperator extends \TuskLang\CoreOperators\BaseOperator
{
    public function getName(): string
    {
        return 'switch';
    }

    protected function executeOperator(array $config, array $context): mixed
    {
        $value = $config['value'] ?? null;
        $cases = $config['cases'] ?? [];
        $default = $config['default'] ?? null;

        if ($value === null) {
            throw new \Exception("Switch operator requires a 'value' parameter");
        }

        // Check each case
        foreach ($cases as $caseValue => $caseResult) {
            if ($value == $caseValue) {
                return $this->evaluateResult($caseResult, $context);
            }
        }

        // Return default if no match found
        if ($default !== null) {
            return $this->evaluateResult($default, $context);
        }

        return null;
    }

    private function evaluateResult($result, array $context): mixed
    {
        if (is_string($result) && str_starts_with($result, '@')) {
            // Handle nested operators
            return $this->evaluateNestedOperator($result, $context);
        }

        return $result;
    }

    private function evaluateNestedOperator(string $operatorString, array $context): mixed
    {
        // Parse operator string like "@operator(param1: value1, param2: value2)"
        if (preg_match('/@(\w+)\((.*)\)/', $operatorString, $matches)) {
            $operatorName = $matches[1];
            $params = $matches[2];
            
            // Parse parameters
            $config = [];
            if (preg_match_all('/(\w+)\s*:\s*([^,\s]+)/', $params, $paramMatches, PREG_SET_ORDER)) {
                foreach ($paramMatches as $match) {
                    $key = $match[1];
                    $value = $match[2];
                    
                    // Remove quotes if present
                    if (preg_match('/^["\'](.*)["\']$/', $value, $quoteMatches)) {
                        $value = $quoteMatches[1];
                    }
                    
                    $config[$key] = $value;
                }
            }
            
            // Execute the nested operator
            $registry = \TuskLang\Enhanced\OperatorRegistry::getInstance();
            return $registry->executeOperator($operatorName, $config, $context);
        }

        return $operatorString;
    }
} 