<?php

namespace TuskLang\Communication\GraphQL;

/**
 * DataLoader Registry for preventing N+1 query problems
 * 
 * Features:
 * - Batch loading with deferred execution
 * - Caching and memoization
 * - Promise-based async loading
 * - Multiple loader registration
 */
class DataLoaderRegistry
{
    private array $loaders = [];
    private array $cache = [];
    private array $batchQueue = [];
    private bool $dispatching = false;

    /**
     * Register a data loader
     */
    public function register(string $name, callable $batchLoader): DataLoader
    {
        $loader = new DataLoader($batchLoader, [
            'cache' => true,
            'maxBatchSize' => 100,
            'cacheMap' => new ArrayCache()
        ]);
        
        $this->loaders[$name] = $loader;
        return $loader;
    }

    /**
     * Get registered loader
     */
    public function get(string $name): ?DataLoader
    {
        return $this->loaders[$name] ?? null;
    }

    /**
     * Load single item through registered loader
     */
    public function load(string $loaderName, $key)
    {
        $loader = $this->get($loaderName);
        if (!$loader) {
            throw new \InvalidArgumentException("Loader '{$loaderName}' not found");
        }
        
        return $loader->load($key);
    }

    /**
     * Load multiple items through registered loader
     */
    public function loadMany(string $loaderName, array $keys): array
    {
        $loader = $this->get($loaderName);
        if (!$loader) {
            throw new \InvalidArgumentException("Loader '{$loaderName}' not found");
        }
        
        return $loader->loadMany($keys);
    }

    /**
     * Prime cache with known values
     */
    public function prime(string $loaderName, $key, $value): self
    {
        $loader = $this->get($loaderName);
        if ($loader) {
            $loader->prime($key, $value);
        }
        return $this;
    }

    /**
     * Clear cache for specific loader
     */
    public function clearCache(string $loaderName, $key = null): self
    {
        $loader = $this->get($loaderName);
        if ($loader) {
            if ($key !== null) {
                $loader->clear($key);
            } else {
                $loader->clearAll();
            }
        }
        return $this;
    }

    /**
     * Clear all caches
     */
    public function clearAllCaches(): self
    {
        foreach ($this->loaders as $loader) {
            $loader->clearAll();
        }
        return $this;
    }

    /**
     * Get all registered loaders
     */
    public function getRegisteredLoaders(): array
    {
        return array_keys($this->loaders);
    }

    /**
     * Execute all pending batches
     */
    public function dispatch(): void
    {
        if ($this->dispatching) {
            return;
        }
        
        $this->dispatching = true;
        
        try {
            foreach ($this->loaders as $loader) {
                $loader->dispatch();
            }
        } finally {
            $this->dispatching = false;
        }
    }
}

/**
 * Individual DataLoader implementation
 */
class DataLoader
{
    private $batchLoader;
    private array $options;
    private array $promiseCache;
    private array $batch = [];
    private bool $dispatched = false;

    public function __construct(callable $batchLoader, array $options = [])
    {
        $this->batchLoader = $batchLoader;
        $this->options = array_merge([
            'cache' => true,
            'maxBatchSize' => 100,
            'cacheMap' => new ArrayCache()
        ], $options);
        $this->promiseCache = [];
    }

    /**
     * Load single value
     */
    public function load($key)
    {
        if ($this->options['cache'] && isset($this->promiseCache[$key])) {
            return $this->promiseCache[$key];
        }

        $this->batch[] = $key;
        
        $promise = new Promise(function() use ($key) {
            if (!$this->dispatched) {
                $this->dispatch();
            }
        });

        if ($this->options['cache']) {
            $this->promiseCache[$key] = $promise;
        }

        return $promise;
    }

    /**
     * Load multiple values
     */
    public function loadMany(array $keys): array
    {
        $promises = [];
        foreach ($keys as $key) {
            $promises[] = $this->load($key);
        }
        return $promises;
    }

    /**
     * Prime cache with known value
     */
    public function prime($key, $value): self
    {
        if ($this->options['cache']) {
            $this->promiseCache[$key] = Promise::resolve($value);
        }
        return $this;
    }

    /**
     * Clear specific key from cache
     */
    public function clear($key): self
    {
        unset($this->promiseCache[$key]);
        return $this;
    }

    /**
     * Clear entire cache
     */
    public function clearAll(): self
    {
        $this->promiseCache = [];
        return $this;
    }

    /**
     * Execute batch load
     */
    public function dispatch(): void
    {
        if ($this->dispatched || empty($this->batch)) {
            return;
        }

        $this->dispatched = true;
        $batch = $this->batch;
        $this->batch = [];

        try {
            $results = ($this->batchLoader)($batch);
            
            if (!is_array($results) || count($results) !== count($batch)) {
                throw new \RuntimeException('Batch loader must return array with same length as input');
            }

            foreach ($batch as $index => $key) {
                $promise = $this->promiseCache[$key] ?? null;
                if ($promise) {
                    $promise->resolveValue($results[$index]);
                }
            }
        } catch (\Exception $error) {
            foreach ($batch as $key) {
                $promise = $this->promiseCache[$key] ?? null;
                if ($promise) {
                    $promise->rejectWith($error);
                }
            }
        } finally {
            $this->dispatched = false;
        }
    }
}

/**
 * Simple Promise implementation
 */
class Promise
{
    private $state = 'pending'; // pending, resolved, rejected
    private $value = null;
    private $reason = null;
    private array $handlers = [];

    public function __construct(callable $executor = null)
    {
        if ($executor) {
            try {
                $executor();
            } catch (\Exception $e) {
                                 $this->rejectWith($e);
            }
        }
    }

    public static function resolve($value): self
    {
        $promise = new self();
        $promise->resolveValue($value);
        return $promise;
    }

    public static function reject($reason): self
    {
        $promise = new self();
        $promise->rejectWith($reason);
        return $promise;
    }

    public function resolveValue($value): void
    {
        if ($this->state !== 'pending') {
            return;
        }

        $this->state = 'resolved';
        $this->value = $value;
        $this->executeHandlers();
    }

    public function rejectWith($reason): void
    {
        if ($this->state !== 'pending') {
            return;
        }

        $this->state = 'rejected';
        $this->reason = $reason;
        $this->executeHandlers();
    }

    public function then(callable $onResolved = null, callable $onRejected = null): self
    {
        $promise = new self();
        
        $this->handlers[] = [
            'promise' => $promise,
            'onResolved' => $onResolved,
            'onRejected' => $onRejected
        ];

        if ($this->state !== 'pending') {
            $this->executeHandlers();
        }

        return $promise;
    }

    private function executeHandlers(): void
    {
        foreach ($this->handlers as $handler) {
            $this->executeHandler($handler);
        }
        $this->handlers = [];
    }

    private function executeHandler(array $handler): void
    {
        if ($this->state === 'resolved' && $handler['onResolved']) {
            try {
                                 $result = $handler['onResolved']($this->value);
                 $handler['promise']->resolveValue($result);
            } catch (\Exception $e) {
                $handler['promise']->reject($e);
            }
        } elseif ($this->state === 'rejected' && $handler['onRejected']) {
            try {
                                 $result = $handler['onRejected']($this->reason);
                 $handler['promise']->resolveValue($result);
            } catch (\Exception $e) {
                $handler['promise']->reject($e);
            }
        } elseif ($this->state === 'resolved') {
            $handler['promise']->resolveValue($this->value);
        } elseif ($this->state === 'rejected') {
                         $handler['promise']->rejectWith($this->reason);
        }
    }

    public function getValue()
    {
        return $this->value;
    }

    public function getReason()
    {
        return $this->reason;
    }

    public function getState(): string
    {
        return $this->state;
    }
}

/**
 * Simple array-based cache
 */
class ArrayCache
{
    private array $cache = [];

    public function get($key)
    {
        return $this->cache[$key] ?? null;
    }

    public function set($key, $value): void
    {
        $this->cache[$key] = $value;
    }

    public function delete($key): void
    {
        unset($this->cache[$key]);
    }

    public function clear(): void
    {
        $this->cache = [];
    }
} 