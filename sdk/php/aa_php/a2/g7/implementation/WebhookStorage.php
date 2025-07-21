<?php

namespace TuskLang\Communication\Webhook;

/**
 * Webhook Storage Interface Implementation
 */
class WebhookStorage
{
    private array $config;
    private array $webhooks = [];
    private array $deliveries = [];
    private array $analytics = [];

    public function __construct(array $config)
    {
        $this->config = $config;
    }

    public function store(array $webhook): void
    {
        $this->webhooks[$webhook['id']] = $webhook;
    }

    public function find(string $id): ?array
    {
        return $this->webhooks[$id] ?? null;
    }

    public function update(string $id, array $webhook): bool
    {
        if (isset($this->webhooks[$id])) {
            $this->webhooks[$id] = $webhook;
            return true;
        }
        return false;
    }

    public function delete(string $id): bool
    {
        if (isset($this->webhooks[$id])) {
            unset($this->webhooks[$id]);
            return true;
        }
        return false;
    }

    public function findAll(array $filters = []): array
    {
        return array_values($this->webhooks);
    }

    public function findByEvent(string $event): array
    {
        return array_filter($this->webhooks, function($webhook) use ($event) {
            return in_array($event, $webhook['events'] ?? []);
        });
    }

    public function storeDelivery(array $delivery): void
    {
        $this->deliveries[$delivery['id']] = $delivery;
    }

    public function updateDelivery(string $id, array $updates): bool
    {
        if (isset($this->deliveries[$id])) {
            $this->deliveries[$id] = array_merge($this->deliveries[$id], $updates);
            return true;
        }
        return false;
    }

    public function findDelivery(string $id): ?array
    {
        return $this->deliveries[$id] ?? null;
    }

    public function getDeliveries(string $webhookId, int $limit = 50): array
    {
        $webhookDeliveries = array_filter($this->deliveries, function($delivery) use ($webhookId) {
            return $delivery['webhook_id'] === $webhookId;
        });
        
        return array_slice($webhookDeliveries, 0, $limit);
    }

    public function storeDeadLetter(array $deadLetter): void
    {
        // In a real implementation, store in dead letter table
        error_log("Dead letter: " . json_encode($deadLetter));
    }

    public function storeAnalytics(array $analytics): void
    {
        $this->analytics[] = $analytics;
    }

    public function getWebhookCount(): int
    {
        return count($this->webhooks);
    }

    public function getActiveWebhookCount(): int
    {
        return count(array_filter($this->webhooks, fn($w) => $w['active'] ?? true));
    }

    public function getFailedDeliveryCount(): int
    {
        return count(array_filter($this->deliveries, fn($d) => $d['status'] === 'failed'));
    }

    public function getSuccessRate(): float
    {
        $total = count($this->deliveries);
        if ($total === 0) return 100.0;
        
        $successful = count(array_filter($this->deliveries, fn($d) => $d['status'] === 'success'));
        return ($successful / $total) * 100;
    }

    public function getAverageResponseTime(): float
    {
        $deliveries = array_filter($this->deliveries, fn($d) => isset($d['duration']));
        if (empty($deliveries)) return 0.0;
        
        $totalTime = array_sum(array_column($deliveries, 'duration'));
        return $totalTime / count($deliveries);
    }
} 