<?php

declare(strict_types=1);

namespace TuskLang\A5\G3;

use TuskLang\CoreOperator;
use InvalidArgumentException;

/**
 * RuleEngineOperator - Complex validation rule engine with conditional logic
 * 
 * Provides advanced rule engine functionality including rule composition,
 * conditional validation chains, rule sets, and complex business logic validation.
 */
class RuleEngineOperator extends CoreOperator
{
    private array $ruleRegistry = [];
    private array $ruleGroups = [];

    public function getName(): string
    {
        return 'rule_engine';
    }

    public function getDescription(): string 
    {
        return 'Complex validation rule engine with conditional logic';
    }

    public function getSupportedActions(): array
    {
        return [
            'add_rule', 'remove_rule', 'execute_rules', 'create_rule_set',
            'validate_rule_set', 'conditional_rules', 'rule_chain', 'compose_rules',
            'rule_group', 'evaluate_expression', 'rule_priority', 'rule_dependencies'
        ];
    }

    public function execute(string $action, array $params = []): mixed
    {
        return match($action) {
            'add_rule' => $this->addRule($params['name'] ?? '', $params['rule'] ?? null, $params['config'] ?? []),
            'remove_rule' => $this->removeRule($params['name'] ?? ''),
            'execute_rules' => $this->executeRules($params['data'] ?? [], $params['rules'] ?? []),
            'create_rule_set' => $this->createRuleSet($params['name'] ?? '', $params['rules'] ?? [], $params['config'] ?? []),
            'validate_rule_set' => $this->validateRuleSet($params['data'] ?? [], $params['rule_set'] ?? ''),
            'conditional_rules' => $this->conditionalRules($params['data'] ?? [], $params['conditions'] ?? []),
            'rule_chain' => $this->ruleChain($params['data'] ?? [], $params['chain'] ?? []),
            'compose_rules' => $this->composeRules($params['rules'] ?? [], $params['operator'] ?? 'and'),
            'rule_group' => $this->ruleGroup($params['data'] ?? [], $params['groups'] ?? []),
            'evaluate_expression' => $this->evaluateExpression($params['expression'] ?? '', $params['data'] ?? []),
            'rule_priority' => $this->rulePriority($params['data'] ?? [], $params['priority_rules'] ?? []),
            'rule_dependencies' => $this->ruleDependencies($params['data'] ?? [], $params['dependency_rules'] ?? []),
            default => throw new InvalidArgumentException("Unsupported action: {$action}")
        };
    }

    /**
     * Add rule to registry
     */
    private function addRule(string $name, ?callable $rule, array $config = []): array
    {
        if (empty($name)) {
            throw new InvalidArgumentException('Rule name cannot be empty');
        }

        if ($rule === null) {
            throw new InvalidArgumentException('Rule function is required');
        }

        $this->ruleRegistry[$name] = [
            'rule' => $rule,
            'config' => $config,
            'priority' => $config['priority'] ?? 0,
            'dependencies' => $config['dependencies'] ?? [],
            'created_at' => time()
        ];

        return [
            'success' => true,
            'rule_name' => $name,
            'total_rules' => count($this->ruleRegistry)
        ];
    }

    /**
     * Remove rule from registry
     */
    private function removeRule(string $name): array
    {
        if (!isset($this->ruleRegistry[$name])) {
            throw new InvalidArgumentException("Rule '{$name}' does not exist");
        }

        unset($this->ruleRegistry[$name]);

        return [
            'success' => true,
            'removed_rule' => $name,
            'total_rules' => count($this->ruleRegistry)
        ];
    }

    /**
     * Execute multiple rules against data
     */
    private function executeRules(array $data, array $rules): array
    {
        $results = [];
        $allPassed = true;
        $errors = [];

        foreach ($rules as $ruleName => $ruleConfig) {
            if (is_int($ruleName)) {
                $ruleName = $ruleConfig;
                $ruleConfig = [];
            }

            try {
                $result = $this->executeRule($data, $ruleName, $ruleConfig);
                $results[$ruleName] = $result;

                if (!$result['passed']) {
                    $allPassed = false;
                    if (isset($result['errors'])) {
                        $errors = array_merge($errors, $result['errors']);
                    }
                }
            } catch (\Exception $e) {
                $results[$ruleName] = [
                    'passed' => false,
                    'error' => $e->getMessage(),
                    'rule_name' => $ruleName
                ];
                $allPassed = false;
                $errors[] = "Rule '{$ruleName}' failed: " . $e->getMessage();
            }
        }

        return [
            'all_passed' => $allPassed,
            'results' => $results,
            'errors' => $errors,
            'total_rules' => count($rules),
            'passed_rules' => count(array_filter($results, fn($r) => $r['passed'] ?? false)),
            'failed_rules' => count(array_filter($results, fn($r) => !($r['passed'] ?? true)))
        ];
    }

    /**
     * Create named rule set
     */
    private function createRuleSet(string $name, array $rules, array $config = []): array
    {
        if (empty($name)) {
            throw new InvalidArgumentException('Rule set name cannot be empty');
        }

        $this->ruleGroups[$name] = [
            'rules' => $rules,
            'config' => $config,
            'operator' => $config['operator'] ?? 'and', // and, or
            'stop_on_failure' => $config['stop_on_failure'] ?? false,
            'created_at' => time()
        ];

        return [
            'success' => true,
            'rule_set_name' => $name,
            'rule_count' => count($rules),
            'total_rule_sets' => count($this->ruleGroups)
        ];
    }

    /**
     * Validate data against rule set
     */
    private function validateRuleSet(array $data, string $ruleSet): array
    {
        if (!isset($this->ruleGroups[$ruleSet])) {
            throw new InvalidArgumentException("Rule set '{$ruleSet}' does not exist");
        }

        $set = $this->ruleGroups[$ruleSet];
        $operator = $set['operator'];
        $stopOnFailure = $set['stop_on_failure'];

        $results = [];
        $passed = 0;
        $failed = 0;
        $errors = [];

        foreach ($set['rules'] as $ruleName => $ruleConfig) {
            if (is_int($ruleName)) {
                $ruleName = $ruleConfig;
                $ruleConfig = [];
            }

            try {
                $result = $this->executeRule($data, $ruleName, $ruleConfig);
                $results[$ruleName] = $result;

                if ($result['passed']) {
                    $passed++;
                } else {
                    $failed++;
                    if (isset($result['errors'])) {
                        $errors = array_merge($errors, $result['errors']);
                    }

                    if ($stopOnFailure) {
                        break;
                    }
                }
            } catch (\Exception $e) {
                $results[$ruleName] = [
                    'passed' => false,
                    'error' => $e->getMessage()
                ];
                $failed++;
                $errors[] = "Rule '{$ruleName}' failed: " . $e->getMessage();

                if ($stopOnFailure) {
                    break;
                }
            }
        }

        // Determine overall result based on operator
        $overallPassed = match($operator) {
            'and' => $failed === 0,
            'or' => $passed > 0,
            'majority' => $passed > $failed,
            default => $failed === 0
        };

        return [
            'passed' => $overallPassed,
            'rule_set' => $ruleSet,
            'operator' => $operator,
            'results' => $results,
            'statistics' => [
                'total' => count($set['rules']),
                'passed' => $passed,
                'failed' => $failed,
                'success_rate' => count($set['rules']) > 0 ? ($passed / count($set['rules'])) * 100 : 0
            ],
            'errors' => $errors
        ];
    }

    /**
     * Execute conditional rules based on data state
     */
    private function conditionalRules(array $data, array $conditions): array
    {
        $results = [];

        foreach ($conditions as $condition) {
            $conditionMet = $this->evaluateCondition($data, $condition['if']);
            $rulesToExecute = $conditionMet ? ($condition['then'] ?? []) : ($condition['else'] ?? []);

            if (!empty($rulesToExecute)) {
                $result = $this->executeRules($data, $rulesToExecute);
                $results[] = [
                    'condition_met' => $conditionMet,
                    'condition' => $condition['if'],
                    'rules_executed' => $rulesToExecute,
                    'result' => $result
                ];
            }
        }

        return $results;
    }

    /**
     * Execute rules in a chain where each rule depends on the previous
     */
    private function ruleChain(array $data, array $chain): array
    {
        $results = [];
        $currentData = $data;

        foreach ($chain as $step => $ruleConfig) {
            $ruleName = $ruleConfig['rule'] ?? '';
            $transform = $ruleConfig['transform'] ?? null;

            if (empty($ruleName)) {
                throw new InvalidArgumentException("Rule name is required for step {$step}");
            }

            $result = $this->executeRule($currentData, $ruleName, $ruleConfig);
            $results[$step] = $result;

            // Stop chain if rule failed and stop_on_failure is true
            if (!$result['passed'] && ($ruleConfig['stop_on_failure'] ?? false)) {
                break;
            }

            // Apply transformation for next step
            if ($transform && is_callable($transform)) {
                $currentData = $transform($currentData, $result);
            }
        }

        return [
            'chain_passed' => !in_array(false, array_column($results, 'passed')),
            'results' => $results,
            'final_data' => $currentData
        ];
    }

    /**
     * Compose multiple rules with logical operators
     */
    private function composeRules(array $rules, string $operator = 'and'): callable
    {
        return function($data) use ($rules, $operator) {
            $results = [];
            
            foreach ($rules as $ruleName => $ruleConfig) {
                if (is_int($ruleName)) {
                    $ruleName = $ruleConfig;
                    $ruleConfig = [];
                }

                $result = $this->executeRule($data, $ruleName, $ruleConfig);
                $results[] = $result['passed'];

                // Short-circuit evaluation
                if ($operator === 'and' && !$result['passed']) {
                    return false;
                } elseif ($operator === 'or' && $result['passed']) {
                    return true;
                }
            }

            return match($operator) {
                'and' => !in_array(false, $results),
                'or' => in_array(true, $results),
                'xor' => count(array_filter($results)) === 1,
                'nand' => in_array(false, $results),
                'nor' => !in_array(true, $results),
                default => !in_array(false, $results)
            };
        };
    }

    /**
     * Execute rule groups with different strategies
     */
    private function ruleGroup(array $data, array $groups): array
    {
        $results = [];

        foreach ($groups as $groupName => $groupConfig) {
            $strategy = $groupConfig['strategy'] ?? 'all';
            $rules = $groupConfig['rules'] ?? [];

            $groupResult = $this->executeRules($data, $rules);
            
            $passed = match($strategy) {
                'all' => $groupResult['all_passed'],
                'any' => $groupResult['passed_rules'] > 0,
                'majority' => $groupResult['passed_rules'] > $groupResult['failed_rules'],
                'none' => $groupResult['passed_rules'] === 0,
                default => $groupResult['all_passed']
            };

            $results[$groupName] = [
                'passed' => $passed,
                'strategy' => $strategy,
                'group_result' => $groupResult
            ];
        }

        return $results;
    }

    /**
     * Evaluate complex expressions
     */
    private function evaluateExpression(string $expression, array $data): array
    {
        // Simple expression evaluator - extend for complex logic
        $expression = trim($expression);
        
        // Replace data placeholders
        $expression = preg_replace_callback('/\{([^}]+)\}/', function($matches) use ($data) {
            return $this->getNestedValue($data, $matches[1]) ?? 'null';
        }, $expression);

        // Basic safety check
        if (preg_match('/[^a-zA-Z0-9\s\+\-\*\/\(\)\.\<\>\=\!&\|]/', $expression)) {
            throw new InvalidArgumentException('Expression contains unsafe characters');
        }

        try {
            // This is a basic implementation - use a proper expression parser in production
            $result = eval("return {$expression};");
            
            return [
                'expression' => $expression,
                'result' => $result,
                'type' => gettype($result)
            ];
        } catch (\ParseError $e) {
            throw new InvalidArgumentException('Invalid expression syntax: ' . $e->getMessage());
        }
    }

    /**
     * Execute rules with priority ordering
     */
    private function rulePriority(array $data, array $priorityRules): array
    {
        // Sort rules by priority
        usort($priorityRules, function($a, $b) {
            return ($b['priority'] ?? 0) <=> ($a['priority'] ?? 0);
        });

        $results = [];
        foreach ($priorityRules as $ruleConfig) {
            $ruleName = $ruleConfig['rule'] ?? '';
            $result = $this->executeRule($data, $ruleName, $ruleConfig);
            $results[] = array_merge($result, [
                'priority' => $ruleConfig['priority'] ?? 0
            ]);
        }

        return [
            'results' => $results,
            'executed_in_priority_order' => true
        ];
    }

    /**
     * Handle rule dependencies
     */
    private function ruleDependencies(array $data, array $dependencyRules): array
    {
        $executed = [];
        $results = [];
        $pending = $dependencyRules;

        while (!empty($pending)) {
            $progress = false;

            foreach ($pending as $key => $ruleConfig) {
                $ruleName = $ruleConfig['rule'] ?? '';
                $dependencies = $ruleConfig['dependencies'] ?? [];

                // Check if all dependencies are satisfied
                $canExecute = true;
                foreach ($dependencies as $dep) {
                    if (!in_array($dep, $executed)) {
                        $canExecute = false;
                        break;
                    }
                }

                if ($canExecute) {
                    $result = $this->executeRule($data, $ruleName, $ruleConfig);
                    $results[$ruleName] = $result;
                    $executed[] = $ruleName;
                    unset($pending[$key]);
                    $progress = true;
                }
            }

            if (!$progress && !empty($pending)) {
                throw new InvalidArgumentException('Circular dependency detected or missing dependencies');
            }
        }

        return [
            'results' => $results,
            'execution_order' => $executed
        ];
    }

    /**
     * Execute single rule
     */
    private function executeRule(array $data, string $ruleName, array $config = []): array
    {
        if (!isset($this->ruleRegistry[$ruleName])) {
            throw new InvalidArgumentException("Rule '{$ruleName}' not found in registry");
        }

        $ruleInfo = $this->ruleRegistry[$ruleName];
        $rule = $ruleInfo['rule'];
        
        try {
            $result = $rule($data, $config);
            
            return [
                'passed' => (bool) $result,
                'rule_name' => $ruleName,
                'config' => $config,
                'result' => $result
            ];
        } catch (\Exception $e) {
            return [
                'passed' => false,
                'rule_name' => $ruleName,
                'error' => $e->getMessage(),
                'config' => $config
            ];
        }
    }

    /**
     * Evaluate condition
     */
    private function evaluateCondition(array $data, array $condition): bool
    {
        $field = $condition['field'] ?? '';
        $operator = $condition['operator'] ?? '==';
        $value = $condition['value'] ?? null;
        
        $fieldValue = $this->getNestedValue($data, $field);
        
        return match($operator) {
            '==' => $fieldValue == $value,
            '===' => $fieldValue === $value,
            '!=' => $fieldValue != $value,
            '!==' => $fieldValue !== $value,
            '>' => $fieldValue > $value,
            '>=' => $fieldValue >= $value,
            '<' => $fieldValue < $value,
            '<=' => $fieldValue <= $value,
            'in' => is_array($value) && in_array($fieldValue, $value, true),
            'not_in' => is_array($value) && !in_array($fieldValue, $value, true),
            'contains' => is_string($fieldValue) && str_contains($fieldValue, (string) $value),
            'exists' => $fieldValue !== null,
            'empty' => empty($fieldValue),
            'not_empty' => !empty($fieldValue),
            default => false
        };
    }

    /**
     * Get nested value from array using dot notation
     */
    private function getNestedValue(array $data, string $key): mixed
    {
        if (empty($key)) {
            return $data;
        }

        $keys = explode('.', $key);
        $current = $data;

        foreach ($keys as $k) {
            if (!is_array($current) || !array_key_exists($k, $current)) {
                return null;
            }
            $current = $current[$k];
        }

        return $current;
    }
} 