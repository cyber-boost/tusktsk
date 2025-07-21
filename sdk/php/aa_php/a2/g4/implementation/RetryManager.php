<?php

namespace TuskLang\Communication\MessageQueue;

/**
 * Retry Manager for Message Queue Operations
 * 
 * Handles:
 * - Failed message retry logic
 * - Exponential backoff
 * - Dead letter queue management
 * - Retry attempt tracking
 */
class RetryManager
{
    private array $config;
    private array $retryTracking = [];

    public function __construct(array $config)
    {
        $this->config = array_merge([
            'retry_attempts' => 3,
            'retry_delay' => 1000, // milliseconds
            'max_retry_delay' => 30000, // 30 seconds
            'backoff_multiplier' => 2,
            'dead_letter_enabled' => true,
            'retry_tracking_ttl' => 3600 // 1 hour
        ], $config);
    }

    /**
     * Handle failed message
     */
    public function handleFailedMessage($message, \Exception $error): void
    {
        $messageId = $this->getMessageId($message);
        
        if (!isset($this->retryTracking[$messageId])) {
            $this->retryTracking[$messageId] = [
                'attempts' => 0,
                'first_failure' => time(),
                'last_error' => null
            ];
        }
        
        $tracking = &$this->retryTracking[$messageId];
        $tracking['attempts']++;
        $tracking['last_error'] = $error->getMessage();
        $tracking['last_attempt'] = time();
        
        if ($tracking['attempts'] < $this->config['retry_attempts']) {
            $this->scheduleRetry($message, $tracking['attempts']);
        } else {
            $this->sendToDeadLetterQueue($message, $tracking);
            unset($this->retryTracking[$messageId]);
        }
    }

    /**
     * Schedule message retry
     */
    private function scheduleRetry($message, int $attempt): void
    {
        $delay = $this->calculateDelay($attempt);
        
        // In a real implementation, this would queue the message for retry
        // after the calculated delay. For now, we log the retry.
        echo "Scheduling retry #{$attempt} for message after {$delay}ms\n";
        
        // You would implement this with:
        // - Redis delayed queue
        // - Database scheduled jobs
        // - Message queue with delay/TTL
        // - Cron-based retry processor
    }

    /**
     * Calculate retry delay with exponential backoff
     */
    private function calculateDelay(int $attempt): int
    {
        $delay = $this->config['retry_delay'] * pow($this->config['backoff_multiplier'], $attempt - 1);
        return min($delay, $this->config['max_retry_delay']);
    }

    /**
     * Send message to dead letter queue
     */
    private function sendToDeadLetterQueue($message, array $tracking): void
    {
        if (!$this->config['dead_letter_enabled']) {
            echo "Message permanently failed but DLQ disabled\n";
            return;
        }
        
        $deadLetterData = [
            'original_message' => $this->serializeMessage($message),
            'failure_info' => $tracking,
            'dlq_timestamp' => time(),
            'final_error' => $tracking['last_error'],
            'total_attempts' => $tracking['attempts']
        ];
        
        echo "Sending message to dead letter queue after {$tracking['attempts']} attempts\n";
        
        // In a real implementation, send to DLQ
        // $this->deadLetterQueue->send($deadLetterData);
    }

    /**
     * Get message ID for tracking
     */
    private function getMessageId($message): string
    {
        if (is_object($message) && method_exists($message, 'getMessageId')) {
            return $message->getMessageId();
        }
        
        if (is_array($message) && isset($message['id'])) {
            return $message['id'];
        }
        
        // Fallback to hash
        return md5(serialize($message));
    }

    /**
     * Serialize message for storage
     */
    private function serializeMessage($message): array
    {
        if (is_object($message) && method_exists($message, 'toArray')) {
            return $message->toArray();
        }
        
        if (is_array($message)) {
            return $message;
        }
        
        return ['data' => $message];
    }

    /**
     * Clean up old retry tracking
     */
    public function cleanup(): void
    {
        $now = time();
        $ttl = $this->config['retry_tracking_ttl'];
        
        foreach ($this->retryTracking as $messageId => $tracking) {
            if (($now - $tracking['first_failure']) > $ttl) {
                unset($this->retryTracking[$messageId]);
            }
        }
    }

    /**
     * Get retry statistics
     */
    public function getStats(): array
    {
        $totalTracked = count($this->retryTracking);
        $attemptCounts = array_count_values(array_column($this->retryTracking, 'attempts'));
        
        return [
            'messages_being_retried' => $totalTracked,
            'attempt_distribution' => $attemptCounts,
            'oldest_retry' => $totalTracked > 0 ? min(array_column($this->retryTracking, 'first_failure')) : null
        ];
    }
} 