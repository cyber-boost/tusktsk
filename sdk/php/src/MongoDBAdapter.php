<?php
/**
 * MongoDB Adapter for TuskLang Enhanced
 * ====================================
 * Enables @query operations with MongoDB collections
 * 
 * DEFAULT CONFIG: peanut.tsk (the bridge of language grace)
 */

namespace TuskLang;

class MongoDBAdapter
{
    private array $config;
    private $client = null;
    private $database = null;
    
    public function __construct(array $options = [])
    {
        $this->config = array_merge([
            'url' => 'mongodb://localhost:27017',
            'database' => 'tusklang',
            'connectTimeoutMS' => 10000,
            'serverSelectionTimeoutMS' => 5000
        ], $options);
        
        // Check if MongoDB extension is available
        if (!class_exists('\MongoDB\Client')) {
            throw new \Exception('MongoDB adapter requires mongodb/mongodb package. Install it with: composer require mongodb/mongodb');
        }
    }
    
    /**
     * Connect to MongoDB
     */
    public function connect(): void
    {
        if (!$this->client) {
            $this->client = new \MongoDB\Client($this->config['url'], [
                'connectTimeoutMS' => $this->config['connectTimeoutMS'],
                'serverSelectionTimeoutMS' => $this->config['serverSelectionTimeoutMS']
            ]);
            
            $this->database = $this->client->selectDatabase($this->config['database']);
        }
    }
    
    /**
     * Execute a MongoDB query
     * MongoDB uses a special query syntax for TuskLang:
     * @query("collection.find", {"active": true})
     * @query("users.countDocuments", {})
     * @query("orders.aggregate", [{"$group": {"_id": null, "total": {"$sum": "$amount"}}}])
     */
    public function query(string $operation, ...$args): mixed
    {
        $this->connect();
        
        // Parse operation (collection.method)
        if (!str_contains($operation, '.')) {
            throw new \Exception("MongoDB operation must be in format 'collection.method'");
        }
        
        [$collectionName, $method] = explode('.', $operation, 2);
        $collection = $this->database->selectCollection($collectionName);
        
        try {
            switch ($method) {
                case 'find':
                    $filter = $args[0] ?? [];
                    $options = $args[1] ?? [];
                    $cursor = $collection->find($filter, $options);
                    return $cursor->toArray();
                    
                case 'findOne':
                    $filter = $args[0] ?? [];
                    $options = $args[1] ?? [];
                    $result = $collection->findOne($filter, $options);
                    return $result ? $result->toArray() : null;
                    
                case 'countDocuments':
                    $filter = $args[0] ?? [];
                    return $collection->countDocuments($filter);
                    
                case 'estimatedDocumentCount':
                    return $collection->estimatedDocumentCount();
                    
                case 'distinct':
                    $field = $args[0] ?? '_id';
                    $filter = $args[1] ?? [];
                    return $collection->distinct($field, $filter);
                    
                case 'aggregate':
                    $pipeline = $args[0] ?? [];
                    $cursor = $collection->aggregate($pipeline);
                    return $cursor->toArray();
                    
                // TuskLang-specific helpers
                case 'count':
                    // Alias for countDocuments
                    $filter = $args[0] ?? [];
                    return $collection->countDocuments($filter);
                    
                case 'sum':
                    // Sum a specific field
                    $field = $args[0] ?? 'amount';
                    $filter = $args[1] ?? [];
                    $pipeline = [
                        ['$match' => $filter],
                        ['$group' => ['_id' => null, 'total' => ['$sum' => '$' . $field]]]
                    ];
                    $result = $collection->aggregate($pipeline)->toArray();
                    return $result[0]['total'] ?? 0;
                    
                case 'avg':
                    // Average of a specific field
                    $field = $args[0] ?? 'amount';
                    $filter = $args[1] ?? [];
                    $pipeline = [
                        ['$match' => $filter],
                        ['$group' => ['_id' => null, 'average' => ['$avg' => '$' . $field]]]
                    ];
                    $result = $collection->aggregate($pipeline)->toArray();
                    return $result[0]['average'] ?? 0;
                    
                case 'max':
                    // Maximum value of a specific field
                    $field = $args[0] ?? 'amount';
                    $filter = $args[1] ?? [];
                    $pipeline = [
                        ['$match' => $filter],
                        ['$group' => ['_id' => null, 'maximum' => ['$max' => '$' . $field]]]
                    ];
                    $result = $collection->aggregate($pipeline)->toArray();
                    return $result[0]['maximum'] ?? null;
                    
                case 'min':
                    // Minimum value of a specific field
                    $field = $args[0] ?? 'amount';
                    $filter = $args[1] ?? [];
                    $pipeline = [
                        ['$match' => $filter],
                        ['$group' => ['_id' => null, 'minimum' => ['$min' => '$' . $field]]]
                    ];
                    $result = $collection->aggregate($pipeline)->toArray();
                    return $result[0]['minimum'] ?? null;
                    
                default:
                    throw new \Exception("Unsupported MongoDB method: $method");
            }
            
        } catch (\Exception $e) {
            throw new \Exception("MongoDB query error: " . $e->getMessage());
        }
    }
    
    /**
     * Create test data for MongoDB
     */
    public function createTestData(): void
    {
        $this->connect();
        
        // Clear existing test data
        $this->database->drop();
        
        // Create users collection
        $users = $this->database->selectCollection('users');
        $users->insertMany([
            ['name' => 'John Doe', 'email' => 'john@example.com', 'active' => true, 'age' => 30],
            ['name' => 'Jane Smith', 'email' => 'jane@example.com', 'active' => true, 'age' => 25],
            ['name' => 'Bob Wilson', 'email' => 'bob@example.com', 'active' => false, 'age' => 35]
        ]);
        
        // Create orders collection
        $orders = $this->database->selectCollection('orders');
        $orders->insertMany([
            ['user_id' => 1, 'amount' => 99.99, 'status' => 'completed', 'created_at' => new \MongoDB\BSON\UTCDateTime()],
            ['user_id' => 2, 'amount' => 149.50, 'status' => 'completed', 'created_at' => new \MongoDB\BSON\UTCDateTime()],
            ['user_id' => 1, 'amount' => 75.25, 'status' => 'pending', 'created_at' => new \MongoDB\BSON\UTCDateTime()]
        ]);
        
        // Create products collection
        $products = $this->database->selectCollection('products');
        $products->insertMany([
            ['name' => 'Widget A', 'price' => 29.99, 'category' => 'electronics', 'in_stock' => true],
            ['name' => 'Widget B', 'price' => 49.99, 'category' => 'electronics', 'in_stock' => true],
            ['name' => 'Gadget C', 'price' => 19.99, 'category' => 'accessories', 'in_stock' => false]
        ]);
        
        echo "MongoDB test data created successfully\n";
    }
    
    /**
     * Check if connected
     */
    public function isConnected(): bool
    {
        try {
            $this->connect();
            $this->database->command(['ping' => 1]);
            return true;
        } catch (\Exception $e) {
            return false;
        }
    }
    
    /**
     * Load configuration from peanut.tsk
     */
    public static function loadFromPeanut(): self
    {
        $parser = new TuskLangEnhanced();
        $parser->loadPeanut();
        
        $config = [];
        
        // Look for MongoDB configuration in peanut.tsk
        if ($parser->get('database.mongodb.url')) {
            $config['url'] = $parser->get('database.mongodb.url');
        }
        if ($parser->get('database.mongodb.database')) {
            $config['database'] = $parser->get('database.mongodb.database');
        }
        if ($parser->get('database.mongodb.connectTimeoutMS')) {
            $config['connectTimeoutMS'] = (int) $parser->get('database.mongodb.connectTimeoutMS');
        }
        
        if (empty($config)) {
            throw new \Exception('No MongoDB configuration found in peanut.tsk');
        }
        
        return new self($config);
    }
}