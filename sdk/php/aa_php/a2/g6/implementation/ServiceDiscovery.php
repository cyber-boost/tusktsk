<?php

namespace TuskLang\Communication\Gateway;

/**
 * Service Discovery for API Gateway
 */
class ServiceDiscovery
{
    private array $services = [];
    private array $routes = [];
    private array $config;

    public function __construct(array $config)
    {
        $this->config = $config;
    }

    public function registerService(string $name, array $endpoints, array $options = []): void
    {
        $this->services[$name] = [
            'name' => $name,
            'endpoints' => $endpoints,
            'options' => $options,
            'registered_at' => time()
        ];
    }

    public function registerRoute(string $path, array $route): void
    {
        $this->routes[$path] = $route;
    }

    public function resolveRoute(string $path, string $method): ?array
    {
        return $this->routes[$path] ?? null;
    }

    public function getServiceEndpoints(string $serviceName): array
    {
        return $this->services[$serviceName]['endpoints'] ?? [];
    }

    public function getServiceCount(): int
    {
        return count($this->services);
    }

    public function getRouteCount(): int
    {
        return count($this->routes);
    }
} 