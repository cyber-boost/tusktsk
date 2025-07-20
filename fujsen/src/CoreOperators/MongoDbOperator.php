<?php

namespace Fujsen\CoreOperators;

use MongoDB\Client;
use MongoDB\Collection;
use MongoDB\Database;
use MongoDB\BSON\ObjectId;
use MongoDB\BSON\UTCDateTime;
use MongoDB\Operation\FindOneAndUpdate;
use MongoDB\Operation\FindOneAndReplace;
use MongoDB\Operation\FindOneAndDelete;
use MongoDB\Driver\Exception\Exception as MongoException;

/**
 * @mongodb Operator for MongoDB Database Operations
 * 
 * Provides comprehensive MongoDB integration including:
 * - Document CRUD operations
 * - Aggregation pipelines
 * - Index management
 * - Connection pooling
 * - GridFS support
 * - Change streams
 * - Transactions
 * 
 * Usage:
 * users: @mongodb("find", "users", {"active": true})
 * user_count: @mongodb("count", "users", {"status": "active"})
 * aggregated_data: @mongodb("aggregate", "orders", [{"$group": {"_id": "$status", "count": {"$sum": 1}}}])
 */
class MongoDbOperator extends BaseOperator
{
    private Client $client;
    private Database $database;
    private array $connections = [];
    private array $collections = [];
    
    public function __construct()
    {
        parent::__construct();
        $this->operatorName = 'mongodb';
        $this->supportedOperations = [
            'connect', 'disconnect', 'find', 'findOne', 'insertOne', 'insertMany',
            'updateOne', 'updateMany', 'deleteOne', 'deleteMany', 'count',
            'aggregate', 'distinct', 'createIndex', 'dropIndex', 'listIndexes',
            'createCollection', 'dropCollection', 'listCollections',
            'gridfs_upload', 'gridfs_download', 'gridfs_delete',
            'watch', 'transaction', 'bulkWrite', 'replaceOne'
        ];
    }
    
    /**
     * Execute MongoDB operation
     */
    public function execute(string $operation, array $params = []): mixed
    {
        try {
            $this->validateOperation($operation);
            
            return match($operation) {
                'connect' => $this->connect($params),
                'disconnect' => $this->disconnect($params),
                'find' => $this->find($params),
                'findOne' => $this->findOne($params),
                'insertOne' => $this->insertOne($params),
                'insertMany' => $this->insertMany($params),
                'updateOne' => $this->updateOne($params),
                'updateMany' => $this->updateMany($params),
                'deleteOne' => $this->deleteOne($params),
                'deleteMany' => $this->deleteMany($params),
                'count' => $this->count($params),
                'aggregate' => $this->aggregate($params),
                'distinct' => $this->distinct($params),
                'createIndex' => $this->createIndex($params),
                'dropIndex' => $this->dropIndex($params),
                'listIndexes' => $this->listIndexes($params),
                'createCollection' => $this->createCollection($params),
                'dropCollection' => $this->dropCollection($params),
                'listCollections' => $this->listCollections($params),
                'gridfs_upload' => $this->gridfsUpload($params),
                'gridfs_download' => $this->gridfsDownload($params),
                'gridfs_delete' => $this->gridfsDelete($params),
                'watch' => $this->watch($params),
                'transaction' => $this->transaction($params),
                'bulkWrite' => $this->bulkWrite($params),
                'replaceOne' => $this->replaceOne($params),
                default => throw new \InvalidArgumentException("Unsupported operation: $operation")
            };
        } catch (MongoException $e) {
            $this->logError("MongoDB operation failed: " . $e->getMessage(), [
                'operation' => $operation,
                'params' => $params,
                'error' => $e->getMessage()
            ]);
            throw $e;
        }
    }
    
    /**
     * Connect to MongoDB
     */
    private function connect(array $params): bool
    {
        $uri = $params['uri'] ?? 'mongodb://localhost:27017';
        $database = $params['database'] ?? 'test';
        $options = $params['options'] ?? [];
        
        $this->client = new Client($uri, $options);
        $this->database = $this->client->selectDatabase($database);
        
        // Test connection
        $this->client->listDatabases();
        
        $this->logInfo("Connected to MongoDB", [
            'uri' => $uri,
            'database' => $database
        ]);
        
        return true;
    }
    
    /**
     * Disconnect from MongoDB
     */
    private function disconnect(array $params = []): bool
    {
        if (isset($this->client)) {
            $this->client = null;
            $this->database = null;
            $this->collections = [];
        }
        
        $this->logInfo("Disconnected from MongoDB");
        return true;
    }
    
    /**
     * Find documents
     */
    private function find(array $params): array
    {
        $collection = $this->getCollection($params['collection']);
        $filter = $params['filter'] ?? [];
        $options = $params['options'] ?? [];
        
        $cursor = $collection->find($filter, $options);
        $documents = iterator_to_array($cursor);
        
        // Convert ObjectIds to strings for JSON serialization
        return $this->convertObjectIds($documents);
    }
    
    /**
     * Find single document
     */
    private function findOne(array $params): ?array
    {
        $collection = $this->getCollection($params['collection']);
        $filter = $params['filter'] ?? [];
        $options = $params['options'] ?? [];
        
        $document = $collection->findOne($filter, $options);
        
        if ($document === null) {
            return null;
        }
        
        return $this->convertObjectIds([$document])[0];
    }
    
    /**
     * Insert single document
     */
    private function insertOne(array $params): array
    {
        $collection = $this->getCollection($params['collection']);
        $document = $params['document'];
        
        $result = $collection->insertOne($document);
        
        return [
            'insertedId' => (string) $result->getInsertedId(),
            'acknowledged' => $result->isAcknowledged()
        ];
    }
    
    /**
     * Insert multiple documents
     */
    private function insertMany(array $params): array
    {
        $collection = $this->getCollection($params['collection']);
        $documents = $params['documents'];
        
        $result = $collection->insertMany($documents);
        
        return [
            'insertedIds' => array_map(fn($id) => (string) $id, $result->getInsertedIds()),
            'acknowledged' => $result->isAcknowledged()
        ];
    }
    
    /**
     * Update single document
     */
    private function updateOne(array $params): array
    {
        $collection = $this->getCollection($params['collection']);
        $filter = $params['filter'];
        $update = $params['update'];
        $options = $params['options'] ?? [];
        
        $result = $collection->updateOne($filter, $update, $options);
        
        return [
            'matchedCount' => $result->getMatchedCount(),
            'modifiedCount' => $result->getModifiedCount(),
            'upsertedId' => $result->getUpsertedId() ? (string) $result->getUpsertedId() : null,
            'acknowledged' => $result->isAcknowledged()
        ];
    }
    
    /**
     * Update multiple documents
     */
    private function updateMany(array $params): array
    {
        $collection = $this->getCollection($params['collection']);
        $filter = $params['filter'];
        $update = $params['update'];
        $options = $params['options'] ?? [];
        
        $result = $collection->updateMany($filter, $update, $options);
        
        return [
            'matchedCount' => $result->getMatchedCount(),
            'modifiedCount' => $result->getModifiedCount(),
            'upsertedId' => $result->getUpsertedId() ? (string) $result->getUpsertedId() : null,
            'acknowledged' => $result->isAcknowledged()
        ];
    }
    
    /**
     * Delete single document
     */
    private function deleteOne(array $params): array
    {
        $collection = $this->getCollection($params['collection']);
        $filter = $params['filter'];
        $options = $params['options'] ?? [];
        
        $result = $collection->deleteOne($filter, $options);
        
        return [
            'deletedCount' => $result->getDeletedCount(),
            'acknowledged' => $result->isAcknowledged()
        ];
    }
    
    /**
     * Delete multiple documents
     */
    private function deleteMany(array $params): array
    {
        $collection = $this->getCollection($params['collection']);
        $filter = $params['filter'];
        $options = $params['options'] ?? [];
        
        $result = $collection->deleteMany($filter, $options);
        
        return [
            'deletedCount' => $result->getDeletedCount(),
            'acknowledged' => $result->isAcknowledged()
        ];
    }
    
    /**
     * Count documents
     */
    private function count(array $params): int
    {
        $collection = $this->getCollection($params['collection']);
        $filter = $params['filter'] ?? [];
        $options = $params['options'] ?? [];
        
        return $collection->countDocuments($filter, $options);
    }
    
    /**
     * Aggregate documents
     */
    private function aggregate(array $params): array
    {
        $collection = $this->getCollection($params['collection']);
        $pipeline = $params['pipeline'];
        $options = $params['options'] ?? [];
        
        $cursor = $collection->aggregate($pipeline, $options);
        $results = iterator_to_array($cursor);
        
        return $this->convertObjectIds($results);
    }
    
    /**
     * Get distinct values
     */
    private function distinct(array $params): array
    {
        $collection = $this->getCollection($params['collection']);
        $fieldName = $params['fieldName'];
        $filter = $params['filter'] ?? [];
        $options = $params['options'] ?? [];
        
        return $collection->distinct($fieldName, $filter, $options);
    }
    
    /**
     * Create index
     */
    private function createIndex(array $params): string
    {
        $collection = $this->getCollection($params['collection']);
        $key = $params['key'];
        $options = $params['options'] ?? [];
        
        return $collection->createIndex($key, $options);
    }
    
    /**
     * Drop index
     */
    private function dropIndex(array $params): array
    {
        $collection = $this->getCollection($params['collection']);
        $indexName = $params['indexName'];
        
        $result = $collection->dropIndex($indexName);
        
        return [
            'ok' => $result['ok'],
            'nIndexesWas' => $result['nIndexesWas']
        ];
    }
    
    /**
     * List indexes
     */
    private function listIndexes(array $params): array
    {
        $collection = $this->getCollection($params['collection']);
        $cursor = $collection->listIndexes();
        
        return iterator_to_array($cursor);
    }
    
    /**
     * Create collection
     */
    private function createCollection(array $params): array
    {
        $name = $params['name'];
        $options = $params['options'] ?? [];
        
        $result = $this->database->createCollection($name, $options);
        
        return [
            'ok' => $result['ok']
        ];
    }
    
    /**
     * Drop collection
     */
    private function dropCollection(array $params): array
    {
        $name = $params['name'];
        
        $result = $this->database->dropCollection($name);
        
        return [
            'ok' => $result['ok'],
            'nIndexesWas' => $result['nIndexesWas']
        ];
    }
    
    /**
     * List collections
     */
    private function listCollections(array $params): array
    {
        $options = $params['options'] ?? [];
        $cursor = $this->database->listCollections($options);
        
        return iterator_to_array($cursor);
    }
    
    /**
     * Upload file to GridFS
     */
    private function gridfsUpload(array $params): array
    {
        $bucket = $this->database->selectGridFSBucket();
        $filename = $params['filename'];
        $data = $params['data'];
        $options = $params['options'] ?? [];
        
        $stream = fopen('php://temp', 'r+');
        fwrite($stream, $data);
        rewind($stream);
        
        $fileId = $bucket->uploadFromStream($filename, $stream, $options);
        fclose($stream);
        
        return [
            'fileId' => (string) $fileId
        ];
    }
    
    /**
     * Download file from GridFS
     */
    private function gridfsDownload(array $params): string
    {
        $bucket = $this->database->selectGridFSBucket();
        $fileId = $params['fileId'];
        
        $stream = $bucket->openDownloadStream(new ObjectId($fileId));
        $data = stream_get_contents($stream);
        fclose($stream);
        
        return $data;
    }
    
    /**
     * Delete file from GridFS
     */
    private function gridfsDelete(array $params): bool
    {
        $bucket = $this->database->selectGridFSBucket();
        $fileId = $params['fileId'];
        
        $bucket->delete(new ObjectId($fileId));
        
        return true;
    }
    
    /**
     * Watch collection for changes
     */
    private function watch(array $params): array
    {
        $collection = $this->getCollection($params['collection']);
        $pipeline = $params['pipeline'] ?? [];
        $options = $params['options'] ?? [];
        
        $cursor = $collection->watch($pipeline, $options);
        $changes = iterator_to_array($cursor);
        
        return $this->convertObjectIds($changes);
    }
    
    /**
     * Execute transaction
     */
    private function transaction(array $params): mixed
    {
        $callback = $params['callback'];
        $options = $params['options'] ?? [];
        
        $session = $this->client->startSession();
        
        try {
            $result = $session->withTransaction($callback, $options);
            return $result;
        } finally {
            $session->endSession();
        }
    }
    
    /**
     * Bulk write operations
     */
    private function bulkWrite(array $params): array
    {
        $collection = $this->getCollection($params['collection']);
        $operations = $params['operations'];
        $options = $params['options'] ?? [];
        
        $result = $collection->bulkWrite($operations, $options);
        
        return [
            'insertedCount' => $result->getInsertedCount(),
            'matchedCount' => $result->getMatchedCount(),
            'modifiedCount' => $result->getModifiedCount(),
            'deletedCount' => $result->getDeletedCount(),
            'upsertedCount' => $result->getUpsertedCount(),
            'insertedIds' => array_map(fn($id) => (string) $id, $result->getInsertedIds()),
            'upsertedIds' => array_map(fn($id) => (string) $id, $result->getUpsertedIds())
        ];
    }
    
    /**
     * Replace single document
     */
    private function replaceOne(array $params): array
    {
        $collection = $this->getCollection($params['collection']);
        $filter = $params['filter'];
        $replacement = $params['replacement'];
        $options = $params['options'] ?? [];
        
        $result = $collection->replaceOne($filter, $replacement, $options);
        
        return [
            'matchedCount' => $result->getMatchedCount(),
            'modifiedCount' => $result->getModifiedCount(),
            'upsertedId' => $result->getUpsertedId() ? (string) $result->getUpsertedId() : null,
            'acknowledged' => $result->isAcknowledged()
        ];
    }
    
    /**
     * Get collection with caching
     */
    private function getCollection(string $name): Collection
    {
        if (!isset($this->collections[$name])) {
            $this->collections[$name] = $this->database->selectCollection($name);
        }
        
        return $this->collections[$name];
    }
    
    /**
     * Convert ObjectIds to strings for JSON serialization
     */
    private function convertObjectIds(array $data): array
    {
        $json = json_encode($data);
        $decoded = json_decode($json, true);
        
        return $decoded;
    }
    
    /**
     * Get operator metadata
     */
    public function getMetadata(): array
    {
        return [
            'name' => $this->operatorName,
            'description' => 'MongoDB database operations with document CRUD, aggregation, and GridFS support',
            'version' => '1.0.0',
            'supported_operations' => $this->supportedOperations,
            'examples' => [
                'users: @mongodb("find", "users", {"active": true})',
                'user_count: @mongodb("count", "users", {"status": "active"})',
                'aggregated_data: @mongodb("aggregate", "orders", [{"$group": {"_id": "$status", "count": {"$sum": 1}}}])',
                'new_user: @mongodb("insertOne", "users", {"name": "John", "email": "john@example.com"})',
                'updated_user: @mongodb("updateOne", "users", {"_id": "user_id"}, {"$set": {"status": "active"}})'
            ]
        ];
    }
} 