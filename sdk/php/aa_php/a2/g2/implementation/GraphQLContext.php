<?php

namespace TuskLang\Communication\GraphQL;

use TuskLang\Communication\Http\ApiRequest;

/**
 * GraphQL execution context
 * 
 * Provides access to:
 * - HTTP request information
 * - Authentication data
 * - Data loaders
 * - Configuration
 */
class GraphQLContext
{
    private array $data;

    public function __construct(array $data = [])
    {
        $this->data = $data;
    }

    /**
     * Get HTTP request
     */
    public function getRequest(): ?ApiRequest
    {
        return $this->data['request'] ?? null;
    }

    /**
     * Get authenticated user
     */
    public function getUser(): ?array
    {
        return $this->data['user'] ?? null;
    }

    /**
     * Get user permissions
     */
    public function getPermissions(): array
    {
        return $this->data['permissions'] ?? [];
    }

    /**
     * Get data loader registry
     */
    public function getDataLoaders(): ?DataLoaderRegistry
    {
        return $this->data['dataLoaders'] ?? null;
    }

    /**
     * Get configuration
     */
    public function getConfig(): array
    {
        return $this->data['config'] ?? [];
    }

    /**
     * Set context data
     */
    public function set(string $key, $value): self
    {
        $this->data[$key] = $value;
        return $this;
    }

    /**
     * Get context data
     */
    public function get(string $key, $default = null)
    {
        return $this->data[$key] ?? $default;
    }

    /**
     * Check if user has permission
     */
    public function hasPermission(string $permission): bool
    {
        return in_array($permission, $this->getPermissions());
    }

    /**
     * Check if user is authenticated
     */
    public function isAuthenticated(): bool
    {
        return $this->getUser() !== null;
    }

    /**
     * Get user ID
     */
    public function getUserId(): ?int
    {
        $user = $this->getUser();
        return $user['id'] ?? null;
    }

    /**
     * Get all context data
     */
    public function toArray(): array
    {
        return $this->data;
    }
} 