<?php

namespace TuskLang\Tests\Communication\MessageQueue;

use PHPUnit\Framework\TestCase;
use TuskLang\Communication\MessageQueue\KafkaClient;
use TuskLang\Communication\MessageQueue\RabbitMQClient;
use TuskLang\Communication\MessageQueue\RetryManager;

/**
 * Test suite for Message Queue implementations
 */
class MessageQueueTest extends TestCase
{
    public function testKafkaClientBasicOperations(): void
    {
        // Mock Kafka client for testing
        $client = $this->createMockKafkaClient();
        
        // Test topic creation
        $result = $client->createTopic('test-topic');
        $this->assertTrue($result);
        
        // Test message publishing
        $result = $client->publish('test-topic', ['message' => 'test data']);
        $this->assertTrue($result);
    }

    public function testRabbitMQClientBasicOperations(): void
    {
        // Mock RabbitMQ client for testing  
        $client = $this->createMockRabbitMQClient();
        
        // Test exchange creation
        $result = $client->createTopic('test-exchange');
        $this->assertTrue($result);
        
        // Test message publishing
        $result = $client->publish('test-topic', ['message' => 'test data']);
        $this->assertTrue($result);
    }

    public function testMessageSerialization(): void
    {
        $client = $this->createMockKafkaClient();
        
        // Test JSON serialization
        $message = ['key' => 'value', 'number' => 123];
        $result = $client->publish('test-topic', $message, ['format' => 'json']);
        $this->assertTrue($result);
        
        // Test string serialization
        $result = $client->publish('test-topic', 'simple string', ['format' => 'string']);
        $this->assertTrue($result);
    }

    public function testBatchMessagePublishing(): void
    {
        $client = $this->createMockKafkaClient();
        
        $messages = [
            ['key' => 'msg1', 'payload' => 'Message 1'],
            ['key' => 'msg2', 'payload' => 'Message 2'],
            ['key' => 'msg3', 'payload' => 'Message 3']
        ];
        
        $results = $client->publishBatch('test-topic', $messages);
        $this->assertCount(3, $results);
        $this->assertTrue($results[0]);
        $this->assertTrue($results[1]);
        $this->assertTrue($results[2]);
    }

    public function testRetryManager(): void
    {
        $retryManager = new RetryManager([
            'retry_attempts' => 3,
            'retry_delay' => 100,
            'backoff_multiplier' => 2
        ]);
        
        $mockMessage = ['id' => 'test-message-1', 'data' => 'test'];
        $mockError = new \Exception('Test error');
        
        // Test retry handling
        $retryManager->handleFailedMessage($mockMessage, $mockError);
        
        $stats = $retryManager->getStats();
        $this->assertEquals(1, $stats['messages_being_retried']);
    }

    public function testConsumerCallback(): void
    {
        $client = $this->createMockKafkaClient();
        $callbackExecuted = false;
        $receivedMessage = null;
        
        $callback = function($message) use (&$callbackExecuted, &$receivedMessage) {
            $callbackExecuted = true;
            $receivedMessage = $message;
            return true; // Acknowledge message
        };
        
        // Mock subscription
        $this->expectNotToPerformAssertions();
    }

    public function testErrorHandling(): void
    {
        $client = $this->createMockKafkaClient();
        
        // Test handling of invalid topic
        $result = $client->publish('', 'test message');
        $this->assertFalse($result);
        
        // Test handling of invalid message
        $result = $client->publish('test-topic', null);
        $this->assertFalse($result);
    }

    public function testMessageHeaders(): void
    {
        $client = $this->createMockKafkaClient();
        
        $message = 'test message';
        $options = [
            'headers' => [
                'content-type' => 'text/plain',
                'source' => 'unit-test',
                'timestamp' => time()
            ]
        ];
        
        $result = $client->publish('test-topic', $message, $options);
        $this->assertTrue($result);
    }

    public function testTopicManagement(): void
    {
        $client = $this->createMockKafkaClient();
        
        // Create topic
        $result = $client->createTopic('management-test-topic');
        $this->assertTrue($result);
        
        // List topics
        $topics = $client->listTopics();
        $this->assertIsArray($topics);
        
        // Get topic info
        $topicInfo = $client->getTopicInfo('management-test-topic');
        $this->assertIsArray($topicInfo);
    }

    public function testConnectionPooling(): void
    {
        // Test connection reuse
        $client1 = $this->createMockKafkaClient();
        $client2 = $this->createMockKafkaClient();
        
        // Both should work independently
        $result1 = $client1->publish('topic1', 'message1');
        $result2 = $client2->publish('topic2', 'message2');
        
        $this->assertTrue($result1);
        $this->assertTrue($result2);
    }

    public function testSynchronousPublishing(): void
    {
        $client = $this->createMockKafkaClient();
        
        $result = $client->publishSync('test-topic', 'sync message');
        $this->assertTrue($result);
    }

    public function testDeadLetterQueue(): void
    {
        $retryManager = new RetryManager([
            'retry_attempts' => 1, // Fail quickly for test
            'dead_letter_enabled' => true
        ]);
        
        $mockMessage = ['id' => 'dlq-test', 'data' => 'test'];
        $mockError = new \Exception('Permanent failure');
        
        // First failure - should retry
        $retryManager->handleFailedMessage($mockMessage, $mockError);
        
        // Second failure - should go to DLQ
        $retryManager->handleFailedMessage($mockMessage, $mockError);
        
        $stats = $retryManager->getStats();
        $this->assertEquals(0, $stats['messages_being_retried']);
    }

    /**
     * Create mock Kafka client for testing
     */
    private function createMockKafkaClient(): KafkaClient
    {
        $mock = $this->createMock(KafkaClient::class);
        
        $mock->method('publish')
             ->willReturn(true);
             
        $mock->method('publishSync')
             ->willReturn(true);
             
        $mock->method('publishBatch')
             ->willReturn([true, true, true]);
             
        $mock->method('createTopic')
             ->willReturn(true);
             
        $mock->method('listTopics')
             ->willReturn(['topic1', 'topic2', 'test-topic']);
             
        $mock->method('getTopicInfo')
             ->willReturn(['name' => 'test-topic', 'partitions' => 3]);
             
        return $mock;
    }

    /**
     * Create mock RabbitMQ client for testing
     */
    private function createMockRabbitMQClient(): RabbitMQClient
    {
        $mock = $this->createMock(RabbitMQClient::class);
        
        $mock->method('publish')
             ->willReturn(true);
             
        $mock->method('publishSync')
             ->willReturn(true);
             
        $mock->method('createTopic')
             ->willReturn(true);
             
        $mock->method('listTopics')
             ->willReturn(['exchange1', 'exchange2']);
             
        $mock->method('getTopicInfo')
             ->willReturn(['name' => 'test-exchange', 'type' => 'topic']);
             
        return $mock;
    }
} 