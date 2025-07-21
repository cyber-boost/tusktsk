<?php

namespace TuskLang\Communication\Webhook;

use TuskLang\Communication\Http\HttpClient;
use TuskLang\Communication\Http\ApiRequest;

/**
 * Advanced Webhook Management System
 * 
 * Features:
 * - Webhook registration and management
 * - Event subscription and filtering
 * - Reliable delivery with retries
 * - Signature verification and security
 * - Rate limiting and throttling
 * - Dead letter queue handling
 * - Webhook testing and validation
 * - Analytics and monitoring
 */
class WebhookManager
{
    private array $config;
    private WebhookStorage $storage;
    private HttpClient $httpClient;
    private EventDispatcher $eventDispatcher;
    private SecurityManager $securityManager;
    private DeliveryQueue $deliveryQueue;
    private array $subscribers = [];

    public function __construct(array $config = [])
    {
        $this->config = array_merge([
            'max_retries' => 3,
            'retry_delay' => 1000, // milliseconds
            'timeout' => 30,
            'signature_header' => 'X-Webhook-Signature',
            'timestamp_header' => 'X-Webhook-Timestamp',
            'signature_algorithm' => 'sha256',
            'max_payload_size' => 1048576, // 1MB
            'delivery_timeout' => 300, // 5 minutes
            'rate_limit_per_minute' => 60,
            'enable_security' => true,
            'enable_analytics' => true,
            'dead_letter_queue' => true,
            'webhook_table' => 'webhooks',
            'delivery_table' => 'webhook_deliveries'
        ], $config);

        $this->initializeComponents();
    }

    /**
     * Register new webhook
     */
    public function register(array $webhookData): string
    {
        $webhook = array_merge([
            'id' => $this->generateWebhookId(),
            'url' => '',
            'events' => [],
            'secret' => $this->generateSecret(),
            'active' => true,
            'created_at' => date('Y-m-d H:i:s'),
            'updated_at' => date('Y-m-d H:i:s'),
            'headers' => [],
            'timeout' => $this->config['timeout'],
            'max_retries' => $this->config['max_retries'],
            'retry_delay' => $this->config['retry_delay']
        ], $webhookData);

        // Validate webhook
        $this->validateWebhook($webhook);

        // Store webhook
        $this->storage->store($webhook);

        // Test webhook if requested
        if ($webhookData['test'] ?? false) {
            $this->testWebhook($webhook['id']);
        }

        return $webhook['id'];
    }

    /**
     * Update existing webhook
     */
    public function update(string $webhookId, array $updateData): bool
    {
        $webhook = $this->storage->find($webhookId);
        if (!$webhook) {
            throw new WebhookNotFoundException("Webhook not found: {$webhookId}");
        }

        $updatedWebhook = array_merge($webhook, $updateData, [
            'updated_at' => date('Y-m-d H:i:s')
        ]);

        $this->validateWebhook($updatedWebhook);
        return $this->storage->update($webhookId, $updatedWebhook);
    }

    /**
     * Delete webhook
     */
    public function delete(string $webhookId): bool
    {
        return $this->storage->delete($webhookId);
    }

    /**
     * Get webhook by ID
     */
    public function get(string $webhookId): ?array
    {
        return $this->storage->find($webhookId);
    }

    /**
     * List webhooks with filtering
     */
    public function list(array $filters = []): array
    {
        return $this->storage->findAll($filters);
    }

    /**
     * Subscribe to webhook events
     */
    public function subscribe(string $event, callable $callback): self
    {
        if (!isset($this->subscribers[$event])) {
            $this->subscribers[$event] = [];
        }
        $this->subscribers[$event][] = $callback;
        return $this;
    }

    /**
     * Trigger webhook event
     */
    public function trigger(string $event, array $payload, array $context = []): array
    {
        // Get subscribers for this event
        $webhooks = $this->storage->findByEvent($event);
        
        if (empty($webhooks)) {
            return [];
        }

        $deliveries = [];
        
        foreach ($webhooks as $webhook) {
            if (!$webhook['active']) {
                continue;
            }

            // Rate limiting check
            if (!$this->checkRateLimit($webhook['id'])) {
                continue;
            }

            // Prepare delivery
            $delivery = $this->prepareDelivery($webhook, $event, $payload, $context);
            
            // Queue for delivery
            $this->deliveryQueue->enqueue($delivery);
            
            $deliveries[] = $delivery['id'];
        }

        // Notify internal subscribers
        $this->notifySubscribers($event, $payload, $context);

        return $deliveries;
    }

    /**
     * Process delivery queue
     */
    public function processDeliveries(): void
    {
        $deliveries = $this->deliveryQueue->getReadyDeliveries();
        
        foreach ($deliveries as $delivery) {
            $this->executeDelivery($delivery);
        }
    }

    /**
     * Test webhook by sending test payload
     */
    public function testWebhook(string $webhookId): array
    {
        $webhook = $this->get($webhookId);
        if (!$webhook) {
            throw new WebhookNotFoundException("Webhook not found: {$webhookId}");
        }

        $testPayload = [
            'event' => 'webhook.test',
            'data' => [
                'test' => true,
                'webhook_id' => $webhookId,
                'timestamp' => time()
            ]
        ];

        $delivery = $this->prepareDelivery($webhook, 'webhook.test', $testPayload);
        return $this->executeDelivery($delivery);
    }

    /**
     * Get delivery status and history
     */
    public function getDeliveryHistory(string $webhookId, int $limit = 50): array
    {
        return $this->storage->getDeliveries($webhookId, $limit);
    }

    /**
     * Retry failed delivery
     */
    public function retryDelivery(string $deliveryId): array
    {
        $delivery = $this->storage->findDelivery($deliveryId);
        if (!$delivery) {
            throw new DeliveryNotFoundException("Delivery not found: {$deliveryId}");
        }

        // Reset delivery for retry
        $delivery['attempt_count'] = 0;
        $delivery['status'] = 'pending';
        $delivery['next_retry'] = date('Y-m-d H:i:s');

        $this->storage->updateDelivery($deliveryId, $delivery);
        $this->deliveryQueue->enqueue($delivery);

        return $this->executeDelivery($delivery);
    }

    /**
     * Validate webhook data
     */
    private function validateWebhook(array $webhook): void
    {
        if (empty($webhook['url'])) {
            throw new InvalidWebhookException('Webhook URL is required');
        }

        if (!filter_var($webhook['url'], FILTER_VALIDATE_URL)) {
            throw new InvalidWebhookException('Invalid webhook URL');
        }

        if (empty($webhook['events'])) {
            throw new InvalidWebhookException('At least one event must be specified');
        }

        // Check URL accessibility
        if (!$this->isUrlAccessible($webhook['url'])) {
            throw new InvalidWebhookException('Webhook URL is not accessible');
        }
    }

    /**
     * Check if URL is accessible
     */
    private function isUrlAccessible(string $url): bool
    {
        try {
            $ch = curl_init();
            curl_setopt_array($ch, [
                CURLOPT_URL => $url,
                CURLOPT_NOBODY => true,
                CURLOPT_RETURNTRANSFER => true,
                CURLOPT_TIMEOUT => 10,
                CURLOPT_FOLLOWLOCATION => true,
                CURLOPT_SSL_VERIFYPEER => false
            ]);
            
            curl_exec($ch);
            $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
            curl_close($ch);
            
            return $httpCode < 400;
            
        } catch (\Exception $e) {
            return false;
        }
    }

    /**
     * Prepare delivery data
     */
    private function prepareDelivery(array $webhook, string $event, array $payload, array $context = []): array
    {
        $delivery = [
            'id' => $this->generateDeliveryId(),
            'webhook_id' => $webhook['id'],
            'event' => $event,
            'payload' => $payload,
            'url' => $webhook['url'],
            'headers' => array_merge($webhook['headers'] ?? [], [
                'Content-Type' => 'application/json',
                'User-Agent' => 'TuskLang-Webhook/1.0'
            ]),
            'secret' => $webhook['secret'],
            'timeout' => $webhook['timeout'] ?? $this->config['timeout'],
            'max_retries' => $webhook['max_retries'] ?? $this->config['max_retries'],
            'retry_delay' => $webhook['retry_delay'] ?? $this->config['retry_delay'],
            'attempt_count' => 0,
            'status' => 'pending',
            'created_at' => date('Y-m-d H:i:s'),
            'next_retry' => date('Y-m-d H:i:s'),
            'context' => $context
        ];

        // Add security headers
        if ($this->config['enable_security']) {
            $delivery['headers'] = $this->securityManager->addSecurityHeaders(
                $delivery['headers'], 
                $payload, 
                $webhook['secret']
            );
        }

        // Store delivery
        $this->storage->storeDelivery($delivery);

        return $delivery;
    }

    /**
     * Execute webhook delivery
     */
    private function executeDelivery(array $delivery): array
    {
        $startTime = microtime(true);
        $result = [
            'delivery_id' => $delivery['id'],
            'webhook_id' => $delivery['webhook_id'],
            'status' => 'failed',
            'response_code' => null,
            'response_body' => null,
            'error' => null,
            'duration' => 0,
            'attempt' => $delivery['attempt_count'] + 1
        ];

        try {
            // Increment attempt count
            $delivery['attempt_count']++;
            $this->storage->updateDelivery($delivery['id'], [
                'attempt_count' => $delivery['attempt_count'],
                'status' => 'delivering'
            ]);

            // Prepare request
            $request = new ApiRequest('POST', $delivery['url']);
            $request->setHeaders($delivery['headers']);
            $request->setData($delivery['payload']);

            // Execute request
            $response = $this->httpClient->execute($request, [
                'timeout' => $delivery['timeout'],
                'verify_ssl' => true
            ]);

            $result['status'] = $response->getStatusCode() >= 200 && $response->getStatusCode() < 300 ? 'success' : 'failed';
            $result['response_code'] = $response->getStatusCode();
            $result['response_body'] = $response->getBody();

        } catch (\Exception $e) {
            $result['error'] = $e->getMessage();
        }

        $result['duration'] = microtime(true) - $startTime;

        // Update delivery status
        $updateData = [
            'status' => $result['status'],
            'response_code' => $result['response_code'],
            'response_body' => is_string($result['response_body']) ? substr($result['response_body'], 0, 1000) : json_encode($result['response_body']),
            'error_message' => $result['error'],
            'duration' => $result['duration'],
            'delivered_at' => date('Y-m-d H:i:s')
        ];

        // Schedule retry if failed and retries remaining
        if ($result['status'] === 'failed' && $delivery['attempt_count'] < $delivery['max_retries']) {
            $nextRetry = time() + ($delivery['retry_delay'] * pow(2, $delivery['attempt_count'] - 1)) / 1000;
            $updateData['next_retry'] = date('Y-m-d H:i:s', $nextRetry);
            $updateData['status'] = 'pending';
            
            // Re-queue for retry
            $this->deliveryQueue->enqueue($delivery, $nextRetry);
        } elseif ($result['status'] === 'failed') {
            // Move to dead letter queue
            if ($this->config['dead_letter_queue']) {
                $this->moveToDeadLetterQueue($delivery, $result);
            }
        }

        $this->storage->updateDelivery($delivery['id'], $updateData);

        // Record analytics
        if ($this->config['enable_analytics']) {
            $this->recordAnalytics($delivery, $result);
        }

        return $result;
    }

    /**
     * Check rate limiting for webhook
     */
    private function checkRateLimit(string $webhookId): bool
    {
        $key = "webhook_rate_limit:{$webhookId}";
        $window = 60; // 1 minute
        $limit = $this->config['rate_limit_per_minute'];
        
        // Simple in-memory rate limiting (use Redis in production)
        static $rateLimits = [];
        
        $now = time();
        $windowStart = $now - $window;
        
        if (!isset($rateLimits[$key])) {
            $rateLimits[$key] = [];
        }
        
        // Remove old entries
        $rateLimits[$key] = array_filter($rateLimits[$key], function($timestamp) use ($windowStart) {
            return $timestamp > $windowStart;
        });
        
        if (count($rateLimits[$key]) >= $limit) {
            return false;
        }
        
        $rateLimits[$key][] = $now;
        return true;
    }

    /**
     * Notify internal subscribers
     */
    private function notifySubscribers(string $event, array $payload, array $context): void
    {
        if (isset($this->subscribers[$event])) {
            foreach ($this->subscribers[$event] as $callback) {
                try {
                    $callback($payload, $context);
                } catch (\Exception $e) {
                    error_log("Webhook subscriber error: " . $e->getMessage());
                }
            }
        }
    }

    /**
     * Move failed delivery to dead letter queue
     */
    private function moveToDeadLetterQueue(array $delivery, array $result): void
    {
        $deadLetter = [
            'original_delivery' => $delivery,
            'failure_result' => $result,
            'created_at' => date('Y-m-d H:i:s')
        ];
        
        $this->storage->storeDeadLetter($deadLetter);
    }

    /**
     * Record analytics data
     */
    private function recordAnalytics(array $delivery, array $result): void
    {
        $analytics = [
            'webhook_id' => $delivery['webhook_id'],
            'event' => $delivery['event'],
            'status' => $result['status'],
            'response_code' => $result['response_code'],
            'duration' => $result['duration'],
            'attempt' => $result['attempt'],
            'timestamp' => time()
        ];
        
        $this->storage->storeAnalytics($analytics);
    }

    /**
     * Generate webhook ID
     */
    private function generateWebhookId(): string
    {
        return 'wh_' . bin2hex(random_bytes(16));
    }

    /**
     * Generate delivery ID
     */
    private function generateDeliveryId(): string
    {
        return 'whd_' . bin2hex(random_bytes(16));
    }

    /**
     * Generate webhook secret
     */
    private function generateSecret(): string
    {
        return bin2hex(random_bytes(32));
    }

    /**
     * Initialize components
     */
    private function initializeComponents(): void
    {
        $this->storage = new WebhookStorage($this->config);
        $this->httpClient = new HttpClient();
        $this->eventDispatcher = new EventDispatcher($this->config);
        $this->securityManager = new SecurityManager($this->config);
        $this->deliveryQueue = new DeliveryQueue($this->config);
    }

    /**
     * Get webhook statistics
     */
    public function getStats(): array
    {
        return [
            'total_webhooks' => $this->storage->getWebhookCount(),
            'active_webhooks' => $this->storage->getActiveWebhookCount(),
            'pending_deliveries' => $this->deliveryQueue->getPendingCount(),
            'failed_deliveries' => $this->storage->getFailedDeliveryCount(),
            'success_rate' => $this->storage->getSuccessRate(),
            'average_response_time' => $this->storage->getAverageResponseTime(),
            'configuration' => $this->config
        ];
    }
}

/**
 * Webhook exceptions
 */
class WebhookNotFoundException extends \Exception {}
class InvalidWebhookException extends \Exception {}
class DeliveryNotFoundException extends \Exception {} 