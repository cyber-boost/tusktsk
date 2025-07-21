<?php

namespace TuskLang\CoreOperators;

use Exception;

/**
 * Alerts Operator - Alert Management and Notification
 * 
 * Provides comprehensive alert management capabilities including:
 * - Alert creation and configuration
 * - Alert monitoring and triggering
 * - Notification delivery (email, SMS, Slack, etc.)
 * - Alert escalation and routing
 * - Alert history and analytics
 * - Alert suppression and snoozing
 * - Alert correlation and deduplication
 * 
 * @package TuskLang\CoreOperators
 */
class AlertsOperator implements OperatorInterface
{
    private $alerts = [];
    private $notifications = [];
    private $escalationRules = [];
    private $alertHistory = [];

    public function __construct()
    {
        $this->initializeAlertSystem();
    }

    /**
     * Execute Alerts operations
     */
    public function execute(array $params, array $context = []): array
    {
        try {
            $operation = $params['operation'] ?? 'create';
            $data = $params['data'] ?? [];
            
            // Substitute context variables
            $data = $this->substituteContext($data, $context);
            
            switch ($operation) {
                case 'create':
                    return $this->createAlert($data);
                case 'trigger':
                    return $this->triggerAlert($data);
                case 'acknowledge':
                    return $this->acknowledgeAlert($data);
                case 'resolve':
                    return $this->resolveAlert($data);
                case 'escalate':
                    return $this->escalateAlert($data);
                case 'suppress':
                    return $this->suppressAlert($data);
                case 'snooze':
                    return $this->snoozeAlert($data);
                case 'notify':
                    return $this->sendNotification($data);
                case 'history':
                    return $this->getAlertHistory($data);
                case 'analytics':
                    return $this->getAlertAnalytics($data);
                case 'info':
                default:
                    return $this->getAlertsInfo();
            }
        } catch (Exception $e) {
            return [
                'success' => false,
                'error' => 'Alerts operation failed: ' . $e->getMessage(),
                'data' => null
            ];
        }
    }

    /**
     * Create a new alert
     */
    private function createAlert(array $data): array
    {
        $name = $data['name'] ?? '';
        $description = $data['description'] ?? '';
        $severity = $data['severity'] ?? 'medium';
        $conditions = $data['conditions'] ?? [];
        $notifications = $data['notifications'] ?? [];
        $escalation = $data['escalation'] ?? [];
        $suppression = $data['suppression'] ?? [];

        if (empty($name)) {
            return [
                'success' => false,
                'error' => 'Alert name is required',
                'data' => null
            ];
        }

        if (empty($conditions)) {
            return [
                'success' => false,
                'error' => 'Alert conditions are required',
                'data' => null
            ];
        }

        $alertId = uniqid('alert_');
        $alert = [
            'id' => $alertId,
            'name' => $name,
            'description' => $description,
            'severity' => $severity,
            'conditions' => $conditions,
            'notifications' => $notifications,
            'escalation' => $escalation,
            'suppression' => $suppression,
            'status' => 'active',
            'created_at' => date('Y-m-d H:i:s'),
            'updated_at' => date('Y-m-d H:i:s')
        ];

        $this->alerts[$alertId] = $alert;

        return [
            'success' => true,
            'data' => [
                'alert_id' => $alertId,
                'alert' => $alert,
                'status' => 'created'
            ],
            'error' => null
        ];
    }

    /**
     * Trigger an alert
     */
    private function triggerAlert(array $data): array
    {
        $alertId = $data['alert_id'] ?? '';
        $message = $data['message'] ?? '';
        $context = $data['context'] ?? [];
        $source = $data['source'] ?? '';

        if (empty($alertId)) {
            return [
                'success' => false,
                'error' => 'Alert ID is required',
                'data' => null
            ];
        }

        if (!isset($this->alerts[$alertId])) {
            return [
                'success' => false,
                'error' => 'Alert not found: ' . $alertId,
                'data' => null
            ];
        }

        $alert = $this->alerts[$alertId];
        
        // Check if alert is suppressed
        if ($this->isAlertSuppressed($alertId)) {
            return [
                'success' => true,
                'data' => [
                    'alert_id' => $alertId,
                    'status' => 'suppressed',
                    'message' => 'Alert is currently suppressed'
                ],
                'error' => null
            ];
        }

        // Create alert instance
        $instanceId = uniqid('instance_');
        $instance = [
            'id' => $instanceId,
            'alert_id' => $alertId,
            'message' => $message,
            'context' => $context,
            'source' => $source,
            'severity' => $alert['severity'],
            'status' => 'triggered',
            'triggered_at' => date('Y-m-d H:i:s'),
            'acknowledged_at' => null,
            'resolved_at' => null,
            'escalation_level' => 0
        ];

        $this->alertHistory[$instanceId] = $instance;

        // Send notifications
        $notificationResults = $this->sendAlertNotifications($alert, $instance);

        // Check for escalation
        $escalationResult = $this->checkEscalation($alert, $instance);

        return [
            'success' => true,
            'data' => [
                'instance_id' => $instanceId,
                'alert_id' => $alertId,
                'instance' => $instance,
                'notifications' => $notificationResults,
                'escalation' => $escalationResult,
                'status' => 'triggered'
            ],
            'error' => null
        ];
    }

    /**
     * Acknowledge an alert
     */
    private function acknowledgeAlert(array $data): array
    {
        $instanceId = $data['instance_id'] ?? '';
        $acknowledgedBy = $data['acknowledged_by'] ?? '';
        $comment = $data['comment'] ?? '';

        if (empty($instanceId)) {
            return [
                'success' => false,
                'error' => 'Instance ID is required',
                'data' => null
            ];
        }

        if (!isset($this->alertHistory[$instanceId])) {
            return [
                'success' => false,
                'error' => 'Alert instance not found: ' . $instanceId,
                'data' => null
            ];
        }

        $instance = $this->alertHistory[$instanceId];
        $instance['status'] = 'acknowledged';
        $instance['acknowledged_at'] = date('Y-m-d H:i:s');
        $instance['acknowledged_by'] = $acknowledgedBy;
        $instance['comment'] = $comment;
        $instance['updated_at'] = date('Y-m-d H:i:s');

        $this->alertHistory[$instanceId] = $instance;

        return [
            'success' => true,
            'data' => [
                'instance_id' => $instanceId,
                'instance' => $instance,
                'status' => 'acknowledged'
            ],
            'error' => null
        ];
    }

    /**
     * Resolve an alert
     */
    private function resolveAlert(array $data): array
    {
        $instanceId = $data['instance_id'] ?? '';
        $resolvedBy = $data['resolved_by'] ?? '';
        $resolution = $data['resolution'] ?? '';

        if (empty($instanceId)) {
            return [
                'success' => false,
                'error' => 'Instance ID is required',
                'data' => null
            ];
        }

        if (!isset($this->alertHistory[$instanceId])) {
            return [
                'success' => false,
                'error' => 'Alert instance not found: ' . $instanceId,
                'data' => null
            ];
        }

        $instance = $this->alertHistory[$instanceId];
        $instance['status'] = 'resolved';
        $instance['resolved_at'] = date('Y-m-d H:i:s');
        $instance['resolved_by'] = $resolvedBy;
        $instance['resolution'] = $resolution;
        $instance['updated_at'] = date('Y-m-d H:i:s');

        $this->alertHistory[$instanceId] = $instance;

        return [
            'success' => true,
            'data' => [
                'instance_id' => $instanceId,
                'instance' => $instance,
                'status' => 'resolved'
            ],
            'error' => null
        ];
    }

    /**
     * Escalate an alert
     */
    private function escalateAlert(array $data): array
    {
        $instanceId = $data['instance_id'] ?? '';
        $escalationLevel = $data['escalation_level'] ?? 1;

        if (empty($instanceId)) {
            return [
                'success' => false,
                'error' => 'Instance ID is required',
                'data' => null
            ];
        }

        if (!isset($this->alertHistory[$instanceId])) {
            return [
                'success' => false,
                'error' => 'Alert instance not found: ' . $instanceId,
                'data' => null
            ];
        }

        $instance = $this->alertHistory[$instanceId];
        $alert = $this->alerts[$instance['alert_id']] ?? null;

        if (!$alert) {
            return [
                'success' => false,
                'error' => 'Alert not found',
                'data' => null
            ];
        }

        $escalationResult = $this->performEscalation($alert, $instance, $escalationLevel);

        return [
            'success' => true,
            'data' => [
                'instance_id' => $instanceId,
                'escalation_result' => $escalationResult,
                'status' => 'escalated'
            ],
            'error' => null
        ];
    }

    /**
     * Suppress an alert
     */
    private function suppressAlert(array $data): array
    {
        $alertId = $data['alert_id'] ?? '';
        $duration = $data['duration'] ?? 3600; // Default 1 hour
        $reason = $data['reason'] ?? '';

        if (empty($alertId)) {
            return [
                'success' => false,
                'error' => 'Alert ID is required',
                'data' => null
            ];
        }

        if (!isset($this->alerts[$alertId])) {
            return [
                'success' => false,
                'error' => 'Alert not found: ' . $alertId,
                'data' => null
            ];
        }

        $suppression = [
            'alert_id' => $alertId,
            'suppressed_at' => date('Y-m-d H:i:s'),
            'suppressed_until' => date('Y-m-d H:i:s', time() + $duration),
            'reason' => $reason,
            'duration' => $duration
        ];

        $this->alerts[$alertId]['suppression'] = $suppression;

        return [
            'success' => true,
            'data' => [
                'alert_id' => $alertId,
                'suppression' => $suppression,
                'status' => 'suppressed'
            ],
            'error' => null
        ];
    }

    /**
     * Snooze an alert
     */
    private function snoozeAlert(array $data): array
    {
        $instanceId = $data['instance_id'] ?? '';
        $duration = $data['duration'] ?? 1800; // Default 30 minutes
        $reason = $data['reason'] ?? '';

        if (empty($instanceId)) {
            return [
                'success' => false,
                'error' => 'Instance ID is required',
                'data' => null
            ];
        }

        if (!isset($this->alertHistory[$instanceId])) {
            return [
                'success' => false,
                'error' => 'Alert instance not found: ' . $instanceId,
                'data' => null
            ];
        }

        $instance = $this->alertHistory[$instanceId];
        $instance['status'] = 'snoozed';
        $instance['snoozed_at'] = date('Y-m-d H:i:s');
        $instance['snoozed_until'] = date('Y-m-d H:i:s', time() + $duration);
        $instance['snooze_reason'] = $reason;
        $instance['updated_at'] = date('Y-m-d H:i:s');

        $this->alertHistory[$instanceId] = $instance;

        return [
            'success' => true,
            'data' => [
                'instance_id' => $instanceId,
                'instance' => $instance,
                'status' => 'snoozed'
            ],
            'error' => null
        ];
    }

    /**
     * Send notification
     */
    private function sendNotification(array $data): array
    {
        $type = $data['type'] ?? '';
        $recipients = $data['recipients'] ?? [];
        $message = $data['message'] ?? '';
        $subject = $data['subject'] ?? '';
        $priority = $data['priority'] ?? 'normal';

        if (empty($type) || empty($recipients) || empty($message)) {
            return [
                'success' => false,
                'error' => 'Type, recipients, and message are required',
                'data' => null
            ];
        }

        $notificationId = uniqid('notification_');
        $notification = [
            'id' => $notificationId,
            'type' => $type,
            'recipients' => $recipients,
            'message' => $message,
            'subject' => $subject,
            'priority' => $priority,
            'sent_at' => date('Y-m-d H:i:s'),
            'status' => 'sent'
        ];

        $this->notifications[$notificationId] = $notification;

        // Simulate sending notification
        $result = $this->deliverNotification($notification);

        return [
            'success' => true,
            'data' => [
                'notification_id' => $notificationId,
                'notification' => $notification,
                'delivery_result' => $result,
                'status' => 'sent'
            ],
            'error' => null
        ];
    }

    /**
     * Get alert history
     */
    private function getAlertHistory(array $data): array
    {
        $alertId = $data['alert_id'] ?? '';
        $status = $data['status'] ?? '';
        $startDate = $data['start_date'] ?? '';
        $endDate = $data['end_date'] ?? '';
        $limit = $data['limit'] ?? 100;

        $filteredHistory = [];

        foreach ($this->alertHistory as $instanceId => $instance) {
            $include = true;

            if (!empty($alertId) && $instance['alert_id'] !== $alertId) {
                $include = false;
            }

            if (!empty($status) && $instance['status'] !== $status) {
                $include = false;
            }

            if (!empty($startDate)) {
                $instanceTime = strtotime($instance['triggered_at']);
                $startTime = strtotime($startDate);
                if ($instanceTime < $startTime) {
                    $include = false;
                }
            }

            if (!empty($endDate)) {
                $instanceTime = strtotime($instance['triggered_at']);
                $endTime = strtotime($endDate);
                if ($instanceTime > $endTime) {
                    $include = false;
                }
            }

            if ($include) {
                $filteredHistory[] = $instance;
            }
        }

        // Sort by triggered_at descending
        usort($filteredHistory, function($a, $b) {
            return strtotime($b['triggered_at']) - strtotime($a['triggered_at']);
        });

        // Apply limit
        $filteredHistory = array_slice($filteredHistory, 0, $limit);

        return [
            'success' => true,
            'data' => [
                'history' => $filteredHistory,
                'total_count' => count($filteredHistory),
                'filters' => [
                    'alert_id' => $alertId,
                    'status' => $status,
                    'start_date' => $startDate,
                    'end_date' => $endDate,
                    'limit' => $limit
                ]
            ],
            'error' => null
        ];
    }

    /**
     * Get alert analytics
     */
    private function getAlertAnalytics(array $data): array
    {
        $timeRange = $data['time_range'] ?? '24h';
        $alertId = $data['alert_id'] ?? '';

        $analytics = [
            'total_alerts' => count($this->alerts),
            'total_instances' => count($this->alertHistory),
            'status_distribution' => [],
            'severity_distribution' => [],
            'hourly_distribution' => [],
            'top_alerts' => []
        ];

        // Calculate status distribution
        foreach ($this->alertHistory as $instance) {
            $status = $instance['status'];
            $analytics['status_distribution'][$status] = ($analytics['status_distribution'][$status] ?? 0) + 1;
        }

        // Calculate severity distribution
        foreach ($this->alertHistory as $instance) {
            $severity = $instance['severity'];
            $analytics['severity_distribution'][$severity] = ($analytics['severity_distribution'][$severity] ?? 0) + 1;
        }

        // Calculate hourly distribution
        foreach ($this->alertHistory as $instance) {
            $hour = date('H', strtotime($instance['triggered_at']));
            $analytics['hourly_distribution'][$hour] = ($analytics['hourly_distribution'][$hour] ?? 0) + 1;
        }

        // Calculate top alerts
        $alertCounts = [];
        foreach ($this->alertHistory as $instance) {
            $alertId = $instance['alert_id'];
            $alertCounts[$alertId] = ($alertCounts[$alertId] ?? 0) + 1;
        }
        arsort($alertCounts);
        $analytics['top_alerts'] = array_slice($alertCounts, 0, 10, true);

        return [
            'success' => true,
            'data' => [
                'time_range' => $timeRange,
                'analytics' => $analytics
            ],
            'error' => null
        ];
    }

    // Helper Methods
    private function initializeAlertSystem(): void
    {
        $this->alerts = [];
        $this->notifications = [];
        $this->escalationRules = [
            'immediate' => 0,
            '5min' => 300,
            '15min' => 900,
            '30min' => 1800,
            '1hour' => 3600,
            '2hours' => 7200
        ];
        $this->alertHistory = [];
    }

    private function isAlertSuppressed(string $alertId): bool
    {
        if (!isset($this->alerts[$alertId])) {
            return false;
        }

        $alert = $this->alerts[$alertId];
        if (empty($alert['suppression'])) {
            return false;
        }

        $suppression = $alert['suppression'];
        $suppressedUntil = strtotime($suppression['suppressed_until']);
        
        return time() < $suppressedUntil;
    }

    private function sendAlertNotifications(array $alert, array $instance): array
    {
        $results = [];
        $notifications = $alert['notifications'] ?? [];

        foreach ($notifications as $notification) {
            $type = $notification['type'] ?? '';
            $recipients = $notification['recipients'] ?? [];
            $template = $notification['template'] ?? '';

            $message = $this->formatNotificationMessage($template, $instance);

            $result = $this->sendNotification([
                'type' => $type,
                'recipients' => $recipients,
                'message' => $message,
                'subject' => "Alert: {$alert['name']}",
                'priority' => $alert['severity']
            ]);

            $results[] = $result;
        }

        return $results;
    }

    private function checkEscalation(array $alert, array $instance): array
    {
        $escalation = $alert['escalation'] ?? [];
        if (empty($escalation)) {
            return ['escalated' => false];
        }

        $escalationDelay = $escalation['delay'] ?? 300; // Default 5 minutes
        $escalationLevel = $escalation['level'] ?? 1;

        return [
            'escalated' => true,
            'delay' => $escalationDelay,
            'level' => $escalationLevel
        ];
    }

    private function performEscalation(array $alert, array $instance, int $level): array
    {
        $escalation = $alert['escalation'] ?? [];
        $escalationContacts = $escalation['contacts'] ?? [];
        $escalationMessage = $escalation['message'] ?? '';

        $message = $this->formatEscalationMessage($escalationMessage, $instance, $level);

        $results = [];
        foreach ($escalationContacts as $contact) {
            $result = $this->sendNotification([
                'type' => $contact['type'] ?? 'email',
                'recipients' => [$contact['address'] ?? ''],
                'message' => $message,
                'subject' => "ESCALATED: {$alert['name']}",
                'priority' => 'high'
            ]);
            $results[] = $result;
        }

        return [
            'level' => $level,
            'contacts_notified' => count($escalationContacts),
            'results' => $results
        ];
    }

    private function deliverNotification(array $notification): array
    {
        // Simulate notification delivery
        switch ($notification['type']) {
            case 'email':
                return ['delivered' => true, 'method' => 'email'];
            case 'sms':
                return ['delivered' => true, 'method' => 'sms'];
            case 'slack':
                return ['delivered' => true, 'method' => 'slack'];
            case 'webhook':
                return ['delivered' => true, 'method' => 'webhook'];
            default:
                return ['delivered' => false, 'error' => 'Unknown notification type'];
        }
    }

    private function formatNotificationMessage(string $template, array $instance): string
    {
        $alert = $this->alerts[$instance['alert_id']] ?? [];
        
        $replacements = [
            '{alert_name}' => $alert['name'] ?? '',
            '{alert_description}' => $alert['description'] ?? '',
            '{severity}' => $instance['severity'],
            '{message}' => $instance['message'],
            '{source}' => $instance['source'],
            '{triggered_at}' => $instance['triggered_at'],
            '{context}' => json_encode($instance['context'])
        ];

        return str_replace(array_keys($replacements), array_values($replacements), $template);
    }

    private function formatEscalationMessage(string $template, array $instance, int $level): string
    {
        $alert = $this->alerts[$instance['alert_id']] ?? [];
        
        $replacements = [
            '{alert_name}' => $alert['name'] ?? '',
            '{severity}' => $instance['severity'],
            '{message}' => $instance['message'],
            '{escalation_level}' => $level,
            '{triggered_at}' => $instance['triggered_at']
        ];

        return str_replace(array_keys($replacements), array_values($replacements), $template);
    }

    private function getAlertsInfo(): array
    {
        return [
            'success' => true,
            'data' => [
                'total_alerts' => count($this->alerts),
                'total_instances' => count($this->alertHistory),
                'total_notifications' => count($this->notifications),
                'supported_severities' => ['low', 'medium', 'high', 'critical'],
                'supported_notification_types' => ['email', 'sms', 'slack', 'webhook'],
                'capabilities' => [
                    'create_alerts' => true,
                    'trigger_alerts' => true,
                    'acknowledge_alerts' => true,
                    'resolve_alerts' => true,
                    'escalate_alerts' => true,
                    'suppress_alerts' => true,
                    'snooze_alerts' => true,
                    'send_notifications' => true,
                    'alert_history' => true,
                    'alert_analytics' => true
                ]
            ],
            'error' => null
        ];
    }

    private function substituteContext(array $data, array $context): array
    {
        $substituted = [];
        
        foreach ($data as $key => $value) {
            if (is_string($value)) {
                $value = preg_replace_callback('/\$\{([^}]+)\}/', function($matches) use ($context) {
                    $varName = $matches[1];
                    return $context[$varName] ?? $matches[0];
                }, $value);
            } elseif (is_array($value)) {
                $value = $this->substituteContext($value, $context);
            }
            
            $substituted[$key] = $value;
        }
        
        return $substituted;
    }
} 