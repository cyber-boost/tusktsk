<?php

namespace TuskLang\Communication\MessageQueue;

/**
 * Message Queue Interface
 * 
 * Defines the contract for message queue implementations
 */
interface MessageQueueInterface
{
    /**
     * Publish message to topic/queue
     */
    public function publish(string $topic, $message, array $options = []): bool;

    /**
     * Subscribe to topics and process messages
     */
    public function subscribe(array $topics, callable $callback, array $options = []): void;

    /**
     * Create topic/queue with configuration
     */
    public function createTopic(string $topicName, array $config = []): bool;

    /**
     * List available topics/queues
     */
    public function listTopics(): array;

    /**
     * Get topic/queue metadata
     */
    public function getTopicInfo(string $topic): array;
} 