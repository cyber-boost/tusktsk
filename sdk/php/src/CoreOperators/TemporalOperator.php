<?php

namespace TuskLang\CoreOperators;

use Fujsen\BaseOperator;
use Fujsen\Exceptions\OperatorException;

/**
 * Temporal Operator for TuskLang
 * 
 * Provides Temporal workflow orchestration functionality with support for:
 * - Workflow execution and management
 * - Activity execution and scheduling
 * - Task queue management
 * - Workflow history and state
 * - Signal handling and queries
 * - Child workflow execution
 * - Timer and cron scheduling
 * 
 * Usage:
 * ```php
 * // Start workflow
 * $workflow = @temporal({
 *   action: "start",
 *   workflow: "OrderProcessing",
 *   task_queue: "orders",
 *   input: { order_id: "12345", items: @variable("cart_items") }
 * })
 * 
 * // Execute activity
 * $result = @temporal({
 *   action: "activity",
 *   name: "ProcessPayment",
 *   task_queue: "payments",
 *   input: { amount: 99.99, method: "credit_card" }
 * })
 * ```
 */
class TemporalOperator extends \TuskLang\CoreOperators\BaseOperator
{
    private array $clients = [];
    private array $workflows = [];
    private array $activities = [];
    private array $config;
    
    public function __construct(array $config = [])
    {
        parent::__construct('temporal');
        $this->config = array_merge([
            'default_url' => 'localhost:7233',
            'namespace' => 'default',
            'timeout' => 30,
            'retry_attempts' => 3,
            'retry_delay' => 1000,
            'enable_tls' => false,
            'tls_cert' => '',
            'tls_key' => '',
            'tls_ca' => '',
            'identity' => 'tusklang-operator'
        ], $config);
    }

    /**
     * Execute Temporal operation
     */
    public function execute(array $params, array $context = []): mixed
    {
        $this->validateParams($params);
        
        $action = $params['action'] ?? '';
        $workflow = $params['workflow'] ?? '';
        $activity = $params['activity'] ?? '';
        $taskQueue = $params['task_queue'] ?? '';
        $input = $params['input'] ?? [];
        $workflowId = $params['workflow_id'] ?? '';
        $url = $params['url'] ?? $this->config['default_url'];
        $namespace = $params['namespace'] ?? $this->config['namespace'];
        $signal = $params['signal'] ?? '';
        $query = $params['query'] ?? '';
        $timer = $params['timer'] ?? '';
        $cron = $params['cron'] ?? '';
        
        try {
            $client = $this->getClient($url, $namespace);
            
            switch ($action) {
                case 'start':
                    return $this->startWorkflow($client, $workflow, $taskQueue, $input, $workflowId, $cron);
                case 'signal':
                    return $this->signalWorkflow($client, $workflowId, $signal, $input);
                case 'query':
                    return $this->queryWorkflow($client, $workflowId, $query, $input);
                case 'cancel':
                    return $this->cancelWorkflow($client, $workflowId);
                case 'terminate':
                    return $this->terminateWorkflow($client, $workflowId, $input);
                case 'activity':
                    return $this->executeActivity($client, $activity, $taskQueue, $input);
                case 'timer':
                    return $this->scheduleTimer($client, $timer, $input);
                case 'describe':
                    return $this->describeWorkflow($client, $workflowId);
                case 'list':
                    return $this->listWorkflows($client, $workflow, $taskQueue);
                default:
                    throw new OperatorException("Unsupported action: $action");
            }
        } catch (\Exception $e) {
            throw new OperatorException("Temporal operation failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Start workflow execution
     */
    private function startWorkflow($client, string $workflow, string $taskQueue, array $input, string $workflowId, string $cron): array
    {
        $request = [
            'namespace' => $this->config['namespace'],
            'workflowId' => $workflowId ?: $this->generateWorkflowId($workflow),
            'workflowType' => [
                'name' => $workflow
            ],
            'taskQueue' => [
                'name' => $taskQueue
            ],
            'input' => $this->serializeInput($input),
            'workflowExecutionTimeout' => 'PT1H',
            'workflowRunTimeout' => 'PT30M',
            'workflowTaskTimeout' => 'PT10S'
        ];
        
        if ($cron) {
            $request['cronSchedule'] = $cron;
        }
        
        $response = $client->post('/api/v1/namespaces/' . $this->config['namespace'] . '/workflows', [
            'json' => $request
        ]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to start workflow: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        $this->workflows[$data['workflowId']] = [
            'id' => $data['workflowId'],
            'runId' => $data['runId'],
            'workflow' => $workflow,
            'taskQueue' => $taskQueue,
            'status' => 'started',
            'startTime' => time()
        ];
        
        return [
            'status' => 'started',
            'workflow_id' => $data['workflowId'],
            'run_id' => $data['runId'],
            'workflow' => $workflow,
            'task_queue' => $taskQueue
        ];
    }

    /**
     * Signal workflow
     */
    private function signalWorkflow($client, string $workflowId, string $signal, array $input): array
    {
        $request = [
            'signalName' => $signal,
            'input' => $this->serializeInput($input)
        ];
        
        $response = $client->post('/api/v1/namespaces/' . $this->config['namespace'] . '/workflows/' . $workflowId . '/signal', [
            'json' => $request
        ]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to signal workflow: " . $response->getBody());
        }
        
        return [
            'status' => 'signaled',
            'workflow_id' => $workflowId,
            'signal' => $signal,
            'timestamp' => time()
        ];
    }

    /**
     * Query workflow
     */
    private function queryWorkflow($client, string $workflowId, string $query, array $input): array
    {
        $request = [
            'queryType' => $query,
            'queryArgs' => $this->serializeInput($input)
        ];
        
        $response = $client->post('/api/v1/namespaces/' . $this->config['namespace'] . '/workflows/' . $workflowId . '/query', [
            'json' => $request
        ]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to query workflow: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'queried',
            'workflow_id' => $workflowId,
            'query' => $query,
            'result' => $this->deserializeOutput($data['queryResult']),
            'timestamp' => time()
        ];
    }

    /**
     * Cancel workflow
     */
    private function cancelWorkflow($client, string $workflowId): array
    {
        $response = $client->post('/api/v1/namespaces/' . $this->config['namespace'] . '/workflows/' . $workflowId . '/cancel');
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to cancel workflow: " . $response->getBody());
        }
        
        if (isset($this->workflows[$workflowId])) {
            $this->workflows[$workflowId]['status'] = 'cancelled';
        }
        
        return [
            'status' => 'cancelled',
            'workflow_id' => $workflowId,
            'timestamp' => time()
        ];
    }

    /**
     * Terminate workflow
     */
    private function terminateWorkflow($client, string $workflowId, array $input): array
    {
        $request = [
            'reason' => $input['reason'] ?? 'Terminated by operator',
            'details' => $this->serializeInput($input['details'] ?? [])
        ];
        
        $response = $client->post('/api/v1/namespaces/' . $this->config['namespace'] . '/workflows/' . $workflowId . '/terminate', [
            'json' => $request
        ]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to terminate workflow: " . $response->getBody());
        }
        
        if (isset($this->workflows[$workflowId])) {
            $this->workflows[$workflowId]['status'] = 'terminated';
        }
        
        return [
            'status' => 'terminated',
            'workflow_id' => $workflowId,
            'timestamp' => time()
        ];
    }

    /**
     * Execute activity
     */
    private function executeActivity($client, string $activity, string $taskQueue, array $input): array
    {
        $request = [
            'namespace' => $this->config['namespace'],
            'activityType' => [
                'name' => $activity
            ],
            'taskQueue' => [
                'name' => $taskQueue
            ],
            'input' => $this->serializeInput($input),
            'scheduleToCloseTimeout' => 'PT5M',
            'startToCloseTimeout' => 'PT4M',
            'heartbeatTimeout' => 'PT30S'
        ];
        
        $response = $client->post('/api/v1/namespaces/' . $this->config['namespace'] . '/activities', [
            'json' => $request
        ]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to execute activity: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        $this->activities[$data['activityId']] = [
            'id' => $data['activityId'],
            'name' => $activity,
            'task_queue' => $taskQueue,
            'status' => 'scheduled',
            'startTime' => time()
        ];
        
        return [
            'status' => 'scheduled',
            'activity_id' => $data['activityId'],
            'activity' => $activity,
            'task_queue' => $taskQueue
        ];
    }

    /**
     * Schedule timer
     */
    private function scheduleTimer($client, string $timer, array $input): array
    {
        $duration = $this->parseDuration($timer);
        
        $request = [
            'namespace' => $this->config['namespace'],
            'startToFireTimeout' => $duration,
            'input' => $this->serializeInput($input)
        ];
        
        $response = $client->post('/api/v1/namespaces/' . $this->config['namespace'] . '/timers', [
            'json' => $request
        ]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to schedule timer: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'scheduled',
            'timer_id' => $data['timerId'],
            'duration' => $duration,
            'expires_at' => time() + $this->durationToSeconds($duration)
        ];
    }

    /**
     * Describe workflow
     */
    private function describeWorkflow($client, string $workflowId): array
    {
        $response = $client->get('/api/v1/namespaces/' . $this->config['namespace'] . '/workflows/' . $workflowId . '/describe');
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to describe workflow: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'described',
            'workflow_id' => $workflowId,
            'execution_config' => $data['executionConfig'],
            'workflow_execution_info' => $data['workflowExecutionInfo'],
            'pending_activities' => $data['pendingActivities'] ?? [],
            'pending_children' => $data['pendingChildren'] ?? []
        ];
    }

    /**
     * List workflows
     */
    private function listWorkflows($client, string $workflow, string $taskQueue): array
    {
        $params = [];
        
        if ($workflow) {
            $params['query'] = "WorkflowType='$workflow'";
        }
        
        if ($taskQueue) {
            $params['query'] = ($params['query'] ?? '') . " AND TaskQueue='$taskQueue'";
        }
        
        $response = $client->get('/api/v1/namespaces/' . $this->config['namespace'] . '/workflows', [
            'query' => $params
        ]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to list workflows: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'listed',
            'workflows' => $data['executions'] ?? [],
            'count' => count($data['executions'] ?? [])
        ];
    }

    /**
     * Generate workflow ID
     */
    private function generateWorkflowId(string $workflow): string
    {
        return $workflow . '-' . uniqid() . '-' . time();
    }

    /**
     * Serialize input for Temporal
     */
    private function serializeInput($input): array
    {
        if (is_array($input)) {
            return array_map(function($value) {
                if (is_string($value)) {
                    return ['data' => base64_encode($value)];
                }
                return ['data' => base64_encode(json_encode($value))];
            }, $input);
        }
        
        return ['data' => base64_encode(json_encode($input))];
    }

    /**
     * Deserialize output from Temporal
     */
    private function deserializeOutput($output): mixed
    {
        if (is_array($output) && isset($output['data'])) {
            $decoded = base64_decode($output['data']);
            $json = json_decode($decoded, true);
            return $json !== null ? $json : $decoded;
        }
        
        return $output;
    }

    /**
     * Parse duration string
     */
    private function parseDuration(string $duration): string
    {
        // Convert simple formats to ISO 8601
        if (preg_match('/^(\d+)s$/', $duration, $matches)) {
            return 'PT' . $matches[1] . 'S';
        }
        
        if (preg_match('/^(\d+)m$/', $duration, $matches)) {
            return 'PT' . $matches[1] . 'M';
        }
        
        if (preg_match('/^(\d+)h$/', $duration, $matches)) {
            return 'PT' . $matches[1] . 'H';
        }
        
        return $duration; // Assume already in ISO 8601 format
    }

    /**
     * Convert duration to seconds
     */
    private function durationToSeconds(string $duration): int
    {
        if (preg_match('/^PT(\d+)S$/', $duration, $matches)) {
            return (int)$matches[1];
        }
        
        if (preg_match('/^PT(\d+)M$/', $duration, $matches)) {
            return (int)$matches[1] * 60;
        }
        
        if (preg_match('/^PT(\d+)H$/', $duration, $matches)) {
            return (int)$matches[1] * 3600;
        }
        
        return 0;
    }

    /**
     * Get or create Temporal client
     */
    private function getClient(string $url, string $namespace): object
    {
        $clientKey = $url . $namespace;
        
        if (!isset($this->clients[$clientKey])) {
            $this->clients[$clientKey] = $this->createClient($url);
        }
        
        return $this->clients[$clientKey];
    }

    /**
     * Create HTTP client for Temporal
     */
    private function createClient(string $url): object
    {
        $config = [
            'base_uri' => 'http://' . $url,
            'timeout' => $this->config['timeout'],
            'headers' => [
                'Content-Type' => 'application/json',
                'X-Temporal-Namespace' => $this->config['namespace'],
                'X-Temporal-Identity' => $this->config['identity']
            ]
        ];
        
        if ($this->config['enable_tls']) {
            $config['base_uri'] = 'https://' . $url;
            $config['verify'] = true;
            if ($this->config['tls_cert'] && $this->config['tls_key']) {
                $config['cert'] = [$this->config['tls_cert'], $this->config['tls_key']];
            }
            if ($this->config['tls_ca']) {
                $config['verify'] = $this->config['tls_ca'];
            }
        }
        
        return new \GuzzleHttp\Client($config);
    }

    /**
     * Validate operator parameters
     */
    private function validateParams(array $params): void
    {
        if (!isset($params['action'])) {
            throw new OperatorException("Action is required");
        }
        
        $validActions = ['start', 'signal', 'query', 'cancel', 'terminate', 'activity', 'timer', 'describe', 'list'];
        if (!in_array($params['action'], $validActions)) {
            throw new OperatorException("Invalid action: " . $params['action']);
        }
        
        if (in_array($params['action'], ['start']) && !isset($params['workflow'])) {
            throw new OperatorException("Workflow is required for start action");
        }
        
        if (in_array($params['action'], ['activity']) && !isset($params['activity'])) {
            throw new OperatorException("Activity is required for activity action");
        }
        
        if (in_array($params['action'], ['signal', 'query', 'cancel', 'terminate', 'describe']) && !isset($params['workflow_id'])) {
            throw new OperatorException("Workflow ID is required for " . $params['action'] . " action");
        }
    }

    /**
     * Cleanup resources
     */
    public function cleanup(): void
    {
        $this->clients = [];
        $this->workflows = [];
        $this->activities = [];
    }

    /**
     * Get operator schema
     */
    public function getSchema(): array
    {
        return [
            'type' => 'object',
            'properties' => [
                'action' => [
                    'type' => 'string',
                    'enum' => ['start', 'signal', 'query', 'cancel', 'terminate', 'activity', 'timer', 'describe', 'list'],
                    'description' => 'Temporal action'
                ],
                'workflow' => ['type' => 'string', 'description' => 'Workflow name'],
                'activity' => ['type' => 'string', 'description' => 'Activity name'],
                'task_queue' => ['type' => 'string', 'description' => 'Task queue'],
                'input' => ['type' => 'object', 'description' => 'Input data'],
                'workflow_id' => ['type' => 'string', 'description' => 'Workflow ID'],
                'url' => ['type' => 'string', 'description' => 'Temporal URL'],
                'namespace' => ['type' => 'string', 'description' => 'Namespace'],
                'signal' => ['type' => 'string', 'description' => 'Signal name'],
                'query' => ['type' => 'string', 'description' => 'Query type'],
                'timer' => ['type' => 'string', 'description' => 'Timer duration'],
                'cron' => ['type' => 'string', 'description' => 'Cron schedule']
            ],
            'required' => ['action']
        ];
    }
} 