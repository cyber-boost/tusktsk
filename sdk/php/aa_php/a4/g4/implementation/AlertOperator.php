<?php

declare(strict_types=1);

namespace TuskLang\SDK\SystemOperations\Monitoring;

use TuskLang\SDK\Core\BaseOperator;
use TuskLang\SDK\Core\Interfaces\OperatorInterface;
use TuskLang\SDK\Core\Exceptions\AlertOperationException;
use TuskLang\SDK\SystemOperations\AI\AlertCorrelationEngine;
use TuskLang\SDK\SystemOperations\Notification\MultiChannelNotifier;
use TuskLang\SDK\SystemOperations\Escalation\EscalationPolicyManager;

/**
 * Advanced AI-Powered Alert Management & Notification Operator
 * 
 * Features:
 * - Intelligent alert correlation and deduplication
 * - Multi-channel notification routing (Email, SMS, Slack, PagerDuty)
 * - Dynamic escalation policies with time-based triggers
 * - AI-powered alert priority scoring and noise reduction
 * - Advanced alert lifecycle management and auto-resolution
 * - Real-time alert analytics and performance monitoring
 * 
 * @package TuskLang\SDK\SystemOperations\Monitoring
 * @version 1.0.0
 * @author TuskLang AI System
 */
class AlertOperator extends BaseOperator implements OperatorInterface
{
    private AlertCorrelationEngine $correlationEngine;
    private MultiChannelNotifier $notifier;
    private EscalationPolicyManager $escalationManager;
    private array $alerts = [];
    private array $rules = [];
    private array $channels = [];
    private array $policies = [];
    private int $maxAlertsInMemory = 10000;

    public function __construct(array $config = [])
    {
        parent::__construct($config);
        
        $this->correlationEngine = new AlertCorrelationEngine($config['correlation_config'] ?? []);
        $this->notifier = new MultiChannelNotifier($config['notification_config'] ?? []);
        $this->escalationManager = new EscalationPolicyManager($config['escalation_config'] ?? []);
        
        $this->maxAlertsInMemory = $config['max_alerts_memory'] ?? 10000;
        
        $this->initializeOperator();
    }

    /**
     * Trigger an alert with intelligent processing
     */
    public function triggerAlert(string $name, string $severity, string $message, array $metadata = []): string
    {
        try {
            $alertId = $this->generateAlertId();
            $timestamp = microtime(true);
            
            $alert = [
                'id' => $alertId,
                'name' => $name,
                'severity' => $this->normalizeSeverity($severity),
                'message' => $message,
                'metadata' => array_merge($metadata, [
                    'source' => 'alert_operator',
                    'host' => gethostname(),
                    'process_id' => getmypid()
                ]),
                'timestamp' => $timestamp,
                'status' => 'triggered',
                'priority' => $this->calculatePriority($name, $severity, $metadata),
                'correlations' => [],
                'notifications_sent' => [],
                'escalation_level' => 0
            ];

            // AI-powered correlation analysis
            $correlations = $this->correlationEngine->findCorrelations($alert, $this->alerts);
            if (!empty($correlations)) {
                $alert['correlations'] = $correlations;
                $alert = $this->processCorrelatedAlert($alert, $correlations);
            }

            // Store alert
            $this->alerts[$alertId] = $alert;
            
            // Trigger notifications based on rules
            $this->processAlertNotifications($alert);
            
            // Initialize escalation if needed
            $this->escalationManager->initializeEscalation($alertId, $alert);
            
            $this->log('info', "Alert triggered: {$name}", [
                'alert_id' => $alertId,
                'severity' => $severity,
                'priority' => $alert['priority']
            ]);
            
            return $alertId;
        } catch (\Exception $e) {
            throw new AlertOperationException("Failed to trigger alert: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Create alert rule with conditions and actions
     */
    public function createRule(string $name, array $conditions, array $actions, array $config = []): string
    {
        try {
            $ruleId = uniqid('rule_');
            
            $rule = [
                'id' => $ruleId,
                'name' => $name,
                'conditions' => $this->validateConditions($conditions),
                'actions' => $this->validateActions($actions),
                'config' => array_merge([
                    'enabled' => true,
                    'priority' => 'medium',
                    'cooldown' => 300, // 5 minutes
                    'max_triggers_per_hour' => 100
                ], $config),
                'stats' => [
                    'created_at' => time(),
                    'triggered_count' => 0,
                    'last_triggered' => null
                ]
            ];
            
            $this->rules[$ruleId] = $rule;
            
            $this->log('info', "Alert rule created: {$name}", ['rule_id' => $ruleId]);
            
            return $ruleId;
        } catch (\Exception $e) {
            throw new AlertOperationException("Failed to create alert rule: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Add notification channel
     */
    public function addChannel(string $name, string $type, array $config): string
    {
        try {
            $channelId = uniqid('channel_');
            
            $supportedTypes = ['email', 'sms', 'slack', 'webhook', 'pagerduty', 'discord'];
            if (!in_array($type, $supportedTypes)) {
                throw new AlertOperationException("Unsupported channel type: {$type}");
            }
            
            $channel = [
                'id' => $channelId,
                'name' => $name,
                'type' => $type,
                'config' => $config,
                'status' => 'active',
                'stats' => [
                    'created_at' => time(),
                    'messages_sent' => 0,
                    'last_used' => null,
                    'failure_count' => 0
                ]
            ];
            
            $this->channels[$channelId] = $channel;
            
            // Register with notifier
            $this->notifier->registerChannel($channelId, $type, $config);
            
            $this->log('info', "Notification channel added: {$name}", [
                'channel_id' => $channelId,
                'type' => $type
            ]);
            
            return $channelId;
        } catch (\Exception $e) {
            throw new AlertOperationException("Failed to add notification channel: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Create escalation policy
     */
    public function createEscalationPolicy(string $name, array $levels, array $config = []): string
    {
        try {
            $policyId = uniqid('policy_');
            
            $policy = [
                'id' => $policyId,
                'name' => $name,
                'levels' => $this->validateEscalationLevels($levels),
                'config' => array_merge([
                    'enabled' => true,
                    'auto_resolve_timeout' => 3600, // 1 hour
                    'max_escalations' => 5
                ], $config),
                'stats' => [
                    'created_at' => time(),
                    'escalations_triggered' => 0,
                    'auto_resolutions' => 0
                ]
            ];
            
            $this->policies[$policyId] = $policy;
            
            // Register with escalation manager
            $this->escalationManager->registerPolicy($policyId, $policy);
            
            $this->log('info', "Escalation policy created: {$name}", ['policy_id' => $policyId]);
            
            return $policyId;
        } catch (\Exception $e) {
            throw new AlertOperationException("Failed to create escalation policy: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Acknowledge alert
     */
    public function acknowledgeAlert(string $alertId, string $acknowledgedBy, string $note = ''): bool
    {
        try {
            if (!isset($this->alerts[$alertId])) {
                throw new AlertOperationException("Alert not found: {$alertId}");
            }
            
            $this->alerts[$alertId]['status'] = 'acknowledged';
            $this->alerts[$alertId]['acknowledged_at'] = time();
            $this->alerts[$alertId]['acknowledged_by'] = $acknowledgedBy;
            $this->alerts[$alertId]['acknowledgment_note'] = $note;
            
            // Stop escalation
            $this->escalationManager->stopEscalation($alertId);
            
            // Send acknowledgment notifications
            $this->notifier->sendAcknowledgment($this->alerts[$alertId], $acknowledgedBy, $note);
            
            $this->log('info', "Alert acknowledged", [
                'alert_id' => $alertId,
                'acknowledged_by' => $acknowledgedBy
            ]);
            
            return true;
        } catch (\Exception $e) {
            throw new AlertOperationException("Failed to acknowledge alert: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Resolve alert
     */
    public function resolveAlert(string $alertId, string $resolvedBy, string $resolution = ''): bool
    {
        try {
            if (!isset($this->alerts[$alertId])) {
                throw new AlertOperationException("Alert not found: {$alertId}");
            }
            
            $this->alerts[$alertId]['status'] = 'resolved';
            $this->alerts[$alertId]['resolved_at'] = time();
            $this->alerts[$alertId]['resolved_by'] = $resolvedBy;
            $this->alerts[$alertId]['resolution'] = $resolution;
            
            // Stop escalation
            $this->escalationManager->stopEscalation($alertId);
            
            // Send resolution notifications
            $this->notifier->sendResolution($this->alerts[$alertId], $resolvedBy, $resolution);
            
            // Update correlation engine
            $this->correlationEngine->markResolved($alertId);
            
            $this->log('info', "Alert resolved", [
                'alert_id' => $alertId,
                'resolved_by' => $resolvedBy
            ]);
            
            return true;
        } catch (\Exception $e) {
            throw new AlertOperationException("Failed to resolve alert: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Get alert details
     */
    public function getAlert(string $alertId): array
    {
        if (!isset($this->alerts[$alertId])) {
            throw new AlertOperationException("Alert not found: {$alertId}");
        }
        
        return $this->alerts[$alertId];
    }

    /**
     * Query alerts with filtering
     */
    public function queryAlerts(array $filters = [], int $limit = 100, int $offset = 0): array
    {
        try {
            $results = array_values($this->alerts);
            
            // Apply filters
            if (!empty($filters)) {
                $results = array_filter($results, function($alert) use ($filters) {
                    return $this->matchesFilters($alert, $filters);
                });
            }
            
            // Sort by priority and timestamp
            usort($results, function($a, $b) {
                $priorityOrder = ['critical' => 4, 'high' => 3, 'medium' => 2, 'low' => 1];
                $priorityDiff = ($priorityOrder[$b['priority']] ?? 0) - ($priorityOrder[$a['priority']] ?? 0);
                
                if ($priorityDiff === 0) {
                    return $b['timestamp'] <=> $a['timestamp'];
                }
                
                return $priorityDiff;
            });
            
            // Apply pagination
            $total = count($results);
            $results = array_slice($results, $offset, $limit);
            
            return [
                'alerts' => $results,
                'total' => $total,
                'limit' => $limit,
                'offset' => $offset
            ];
        } catch (\Exception $e) {
            throw new AlertOperationException("Failed to query alerts: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Get alert analytics and statistics
     */
    public function getAnalytics(int $timeRange = 86400): array
    {
        try {
            $now = time();
            $from = $now - $timeRange;
            
            $recentAlerts = array_filter($this->alerts, function($alert) use ($from) {
                return $alert['timestamp'] >= $from;
            });
            
            $analytics = [
                'summary' => [
                    'total_alerts' => count($recentAlerts),
                    'by_severity' => [],
                    'by_status' => [],
                    'avg_resolution_time' => 0,
                    'escalation_rate' => 0
                ],
                'trends' => $this->calculateTrends($recentAlerts),
                'top_alert_sources' => $this->getTopSources($recentAlerts),
                'channel_performance' => $this->getChannelPerformance(),
                'escalation_stats' => $this->escalationManager->getStatistics()
            ];
            
            // Calculate statistics
            foreach ($recentAlerts as $alert) {
                $severity = $alert['severity'];
                $status = $alert['status'];
                
                $analytics['summary']['by_severity'][$severity] = ($analytics['summary']['by_severity'][$severity] ?? 0) + 1;
                $analytics['summary']['by_status'][$status] = ($analytics['summary']['by_status'][$status] ?? 0) + 1;
            }
            
            // Calculate average resolution time
            $resolvedAlerts = array_filter($recentAlerts, fn($a) => $a['status'] === 'resolved');
            if (!empty($resolvedAlerts)) {
                $totalResolutionTime = array_sum(array_map(function($alert) {
                    return ($alert['resolved_at'] ?? 0) - $alert['timestamp'];
                }, $resolvedAlerts));
                
                $analytics['summary']['avg_resolution_time'] = $totalResolutionTime / count($resolvedAlerts);
            }
            
            return $analytics;
        } catch (\Exception $e) {
            throw new AlertOperationException("Failed to get analytics: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Process escalations (should be called periodically)
     */
    public function processEscalations(): int
    {
        try {
            return $this->escalationManager->processEscalations($this->alerts);
        } catch (\Exception $e) {
            throw new AlertOperationException("Failed to process escalations: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Cleanup old alerts
     */
    public function cleanup(int $retentionDays = 30): int
    {
        try {
            $cutoffTime = time() - ($retentionDays * 86400);
            $cleaned = 0;
            
            foreach ($this->alerts as $alertId => $alert) {
                if ($alert['timestamp'] < $cutoffTime && $alert['status'] === 'resolved') {
                    unset($this->alerts[$alertId]);
                    $cleaned++;
                }
            }
            
            // Maintain memory limit
            if (count($this->alerts) > $this->maxAlertsInMemory) {
                $sorted = $this->alerts;
                uasort($sorted, fn($a, $b) => $b['timestamp'] <=> $a['timestamp']);
                $this->alerts = array_slice($sorted, 0, $this->maxAlertsInMemory, true);
                $cleaned += count($sorted) - $this->maxAlertsInMemory;
            }
            
            $this->log('info', "Alert cleanup completed", ['cleaned_count' => $cleaned]);
            
            return $cleaned;
        } catch (\Exception $e) {
            throw new AlertOperationException("Failed to cleanup alerts: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Get operator statistics
     */
    public function getStatistics(): array
    {
        return [
            'total_alerts' => count($this->alerts),
            'active_alerts' => count(array_filter($this->alerts, fn($a) => $a['status'] === 'triggered')),
            'acknowledged_alerts' => count(array_filter($this->alerts, fn($a) => $a['status'] === 'acknowledged')),
            'resolved_alerts' => count(array_filter($this->alerts, fn($a) => $a['status'] === 'resolved')),
            'rules_count' => count($this->rules),
            'channels_count' => count($this->channels),
            'policies_count' => count($this->policies),
            'uptime' => time() - $this->getStartTime()
        ];
    }

    // Private helper methods
    private function generateAlertId(): string
    {
        return uniqid('alert_') . '_' . random_int(1000, 9999);
    }

    private function normalizeSeverity(string $severity): string
    {
        $severity = strtolower($severity);
        $validSeverities = ['low', 'medium', 'high', 'critical'];
        
        return in_array($severity, $validSeverities) ? $severity : 'medium';
    }

    private function calculatePriority(string $name, string $severity, array $metadata): string
    {
        // AI-powered priority calculation
        return $this->correlationEngine->calculatePriority($name, $severity, $metadata);
    }

    private function processCorrelatedAlert(array $alert, array $correlations): array
    {
        // Merge correlated alert information
        $alert['correlation_score'] = max(array_column($correlations, 'score'));
        $alert['related_alerts'] = array_column($correlations, 'alert_id');
        
        return $alert;
    }

    private function processAlertNotifications(array $alert): void
    {
        // Process notification rules
        foreach ($this->rules as $rule) {
            if ($this->evaluateRule($rule, $alert)) {
                $this->executeRuleActions($rule, $alert);
            }
        }
    }

    private function evaluateRule(array $rule, array $alert): bool
    {
        if (!$rule['config']['enabled']) {
            return false;
        }
        
        // Evaluate conditions
        foreach ($rule['conditions'] as $condition) {
            if (!$this->evaluateCondition($condition, $alert)) {
                return false;
            }
        }
        
        return true;
    }

    private function evaluateCondition(array $condition, array $alert): bool
    {
        $field = $condition['field'];
        $operator = $condition['operator'];
        $value = $condition['value'];
        
        $alertValue = $this->getNestedValue($alert, $field);
        
        switch ($operator) {
            case '==': return $alertValue == $value;
            case '!=': return $alertValue != $value;
            case '>': return $alertValue > $value;
            case '>=': return $alertValue >= $value;
            case '<': return $alertValue < $value;
            case '<=': return $alertValue <= $value;
            case 'contains': return strpos($alertValue, $value) !== false;
            case 'matches': return preg_match($value, $alertValue);
            default: return false;
        }
    }

    private function executeRuleActions(array $rule, array $alert): void
    {
        foreach ($rule['actions'] as $action) {
            switch ($action['type']) {
                case 'notify':
                    $this->notifier->send($action['channel'], $alert);
                    break;
                case 'escalate':
                    $this->escalationManager->escalate($alert['id'], $action['policy']);
                    break;
                case 'webhook':
                    $this->notifier->webhook($action['url'], $alert);
                    break;
            }
        }
    }

    private function getNestedValue(array $array, string $path)
    {
        $keys = explode('.', $path);
        $value = $array;
        
        foreach ($keys as $key) {
            if (!isset($value[$key])) {
                return null;
            }
            $value = $value[$key];
        }
        
        return $value;
    }

    private function validateConditions(array $conditions): array
    {
        foreach ($conditions as $condition) {
            if (!isset($condition['field'], $condition['operator'], $condition['value'])) {
                throw new AlertOperationException("Invalid condition format");
            }
        }
        return $conditions;
    }

    private function validateActions(array $actions): array
    {
        foreach ($actions as $action) {
            if (!isset($action['type'])) {
                throw new AlertOperationException("Invalid action format");
            }
        }
        return $actions;
    }

    private function validateEscalationLevels(array $levels): array
    {
        foreach ($levels as $level) {
            if (!isset($level['delay'], $level['actions'])) {
                throw new AlertOperationException("Invalid escalation level format");
            }
        }
        return $levels;
    }

    private function matchesFilters(array $alert, array $filters): bool
    {
        foreach ($filters as $field => $value) {
            if ($this->getNestedValue($alert, $field) !== $value) {
                return false;
            }
        }
        return true;
    }

    private function calculateTrends(array $alerts): array
    {
        // Implement trend calculation logic
        return ['hourly' => [], 'daily' => []];
    }

    private function getTopSources(array $alerts): array
    {
        $sources = [];
        foreach ($alerts as $alert) {
            $source = $alert['metadata']['source'] ?? 'unknown';
            $sources[$source] = ($sources[$source] ?? 0) + 1;
        }
        arsort($sources);
        return array_slice($sources, 0, 10, true);
    }

    private function getChannelPerformance(): array
    {
        $performance = [];
        foreach ($this->channels as $channelId => $channel) {
            $performance[$channel['name']] = [
                'messages_sent' => $channel['stats']['messages_sent'],
                'failure_count' => $channel['stats']['failure_count'],
                'success_rate' => $channel['stats']['messages_sent'] > 0 
                    ? (1 - $channel['stats']['failure_count'] / $channel['stats']['messages_sent']) * 100 
                    : 100
            ];
        }
        return $performance;
    }

    private function initializeOperator(): void
    {
        $this->log('info', 'AlertOperator initialized');
    }
} 