<?php

namespace TuskLang\Communication\Gateway;

use TuskLang\Communication\Http\ApiRequest;

/**
 * Load Balancer for API Gateway
 */
class LoadBalancer
{
    private array $config;
    private array $endpointStats = [];

    public function __construct(array $config)
    {
        $this->config = $config;
    }

    public function selectEndpoint(string $serviceName, array $endpoints, ApiRequest $request): ?string
    {
        if (empty($endpoints)) {
            return null;
        }

        $algorithm = $this->config['load_balance_algorithm'] ?? 'round_robin';

        switch ($algorithm) {
            case 'round_robin':
                return $this->roundRobin($serviceName, $endpoints);
            case 'weighted':
                return $this->weighted($serviceName, $endpoints);
            case 'least_connections':
                return $this->leastConnections($serviceName, $endpoints);
            default:
                return $endpoints[0]['url'] ?? null;
        }
    }

    private function roundRobin(string $serviceName, array $endpoints): ?string
    {
        if (!isset($this->endpointStats[$serviceName])) {
            $this->endpointStats[$serviceName] = ['current' => 0];
        }

        $current = $this->endpointStats[$serviceName]['current'];
        $endpoint = $endpoints[$current % count($endpoints)];
        
        $this->endpointStats[$serviceName]['current'] = ($current + 1) % count($endpoints);
        
        return $endpoint['url'] ?? null;
    }

    private function weighted(string $serviceName, array $endpoints): ?string
    {
        $totalWeight = array_sum(array_column($endpoints, 'weight'));
        $random = mt_rand(1, $totalWeight);
        
        $currentWeight = 0;
        foreach ($endpoints as $endpoint) {
            $currentWeight += $endpoint['weight'] ?? 1;
            if ($random <= $currentWeight) {
                return $endpoint['url'];
            }
        }
        
        return $endpoints[0]['url'] ?? null;
    }

    private function leastConnections(string $serviceName, array $endpoints): ?string
    {
        $minConnections = PHP_INT_MAX;
        $selectedEndpoint = null;
        
        foreach ($endpoints as $endpoint) {
            $connections = $endpoint['active_connections'] ?? 0;
            if ($connections < $minConnections) {
                $minConnections = $connections;
                $selectedEndpoint = $endpoint['url'];
            }
        }
        
        return $selectedEndpoint;
    }
} 