<?php

namespace TuskLang\Tests\Communication\Webhook;

use PHPUnit\Framework\TestCase;
use TuskLang\Communication\Webhook\WebhookManager;
use TuskLang\Communication\Webhook\WebhookStorage;

class WebhookManagerTest extends TestCase
{
    private WebhookManager $webhookManager;

    protected function setUp(): void
    {
        $this->webhookManager = new WebhookManager([
            'max_retries' => 3,
            'timeout' => 10,
            'enable_security' => false // Disable for testing
        ]);
    }

    public function testWebhookRegistration(): void
    {
        $webhookData = [
            'url' => 'http://example.com/webhook',
            'events' => ['user.created', 'user.updated'],
            'test' => false
        ];
        
        $webhookId = $this->webhookManager->register($webhookData);
        
        $this->assertIsString($webhookId);
        $this->assertStringStartsWith('wh_', $webhookId);
        
        $webhook = $this->webhookManager->get($webhookId);
        $this->assertEquals('http://example.com/webhook', $webhook['url']);
    }

    public function testWebhookUpdate(): void
    {
        $webhookData = [
            'url' => 'http://example.com/webhook',
            'events' => ['user.created']
        ];
        
        $webhookId = $this->webhookManager->register($webhookData);
        
        $result = $this->webhookManager->update($webhookId, [
            'events' => ['user.created', 'user.deleted']
        ]);
        
        $this->assertTrue($result);
        
        $webhook = $this->webhookManager->get($webhookId);
        $this->assertContains('user.deleted', $webhook['events']);
    }

    public function testWebhookDeletion(): void
    {
        $webhookData = [
            'url' => 'http://example.com/webhook',
            'events' => ['user.created']
        ];
        
        $webhookId = $this->webhookManager->register($webhookData);
        $result = $this->webhookManager->delete($webhookId);
        
        $this->assertTrue($result);
        
        $webhook = $this->webhookManager->get($webhookId);
        $this->assertNull($webhook);
    }

    public function testEventTriggering(): void
    {
        $webhookData = [
            'url' => 'http://example.com/webhook',
            'events' => ['user.created']
        ];
        
        $webhookId = $this->webhookManager->register($webhookData);
        
        $deliveries = $this->webhookManager->trigger('user.created', [
            'user_id' => 123,
            'username' => 'testuser'
        ]);
        
        $this->assertCount(1, $deliveries);
    }

    public function testEventSubscription(): void
    {
        $callbackExecuted = false;
        $receivedPayload = null;
        
        $this->webhookManager->subscribe('test.event', function($payload, $context) use (&$callbackExecuted, &$receivedPayload) {
            $callbackExecuted = true;
            $receivedPayload = $payload;
        });
        
        $this->webhookManager->trigger('test.event', ['test' => 'data']);
        
        $this->assertTrue($callbackExecuted);
        $this->assertEquals(['test' => 'data'], $receivedPayload);
    }

    public function testWebhookValidation(): void
    {
        $this->expectException(\TuskLang\Communication\Webhook\InvalidWebhookException::class);
        
        $this->webhookManager->register([
            'url' => '', // Invalid URL
            'events' => ['test.event']
        ]);
    }

    public function testDeliveryHistory(): void
    {
        $webhookData = [
            'url' => 'http://example.com/webhook',
            'events' => ['user.created']
        ];
        
        $webhookId = $this->webhookManager->register($webhookData);
        
        $this->webhookManager->trigger('user.created', ['user_id' => 123]);
        
        $history = $this->webhookManager->getDeliveryHistory($webhookId);
        $this->assertIsArray($history);
    }

    public function testWebhookStatistics(): void
    {
        $webhookData = [
            'url' => 'http://example.com/webhook',
            'events' => ['user.created']
        ];
        
        $this->webhookManager->register($webhookData);
        
        $stats = $this->webhookManager->getStats();
        
        $this->assertArrayHasKey('total_webhooks', $stats);
        $this->assertArrayHasKey('active_webhooks', $stats);
        $this->assertArrayHasKey('success_rate', $stats);
        
        $this->assertGreaterThanOrEqual(1, $stats['total_webhooks']);
    }

    public function testWebhookListing(): void
    {
        $webhookData1 = [
            'url' => 'http://example1.com/webhook',
            'events' => ['user.created']
        ];
        
        $webhookData2 = [
            'url' => 'http://example2.com/webhook', 
            'events' => ['user.updated']
        ];
        
        $this->webhookManager->register($webhookData1);
        $this->webhookManager->register($webhookData2);
        
        $webhooks = $this->webhookManager->list();
        $this->assertCount(2, $webhooks);
    }
} 