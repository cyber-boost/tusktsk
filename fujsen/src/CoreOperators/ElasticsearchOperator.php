<?php

namespace Fujsen\CoreOperators;

use Fujsen\BaseOperator;
use Fujsen\Exceptions\OperatorException;

/**
 * Elasticsearch Operator for TuskLang
 * 
 * Provides Elasticsearch functionality with support for:
 * - Document indexing and search
 * - Aggregations and analytics
 * - Index management and mapping
 * - Bulk operations
 * - Cluster health and monitoring
 * - Scroll and search after
 * - Multi-index operations
 * 
 * Usage:
 * ```php
 * // Search documents
 * $results = @elasticsearch({
 *   action: "search",
 *   index: "users",
 *   query: { match: { name: "john" } },
 *   size: 10
 * })
 * 
 * // Index document
 * $result = @elasticsearch({
 *   action: "index",
 *   index: "products",
 *   id: "123",
 *   body: { name: "Laptop", price: 999.99 }
 * })
 * ```
 */
class ElasticsearchOperator extends BaseOperator
{
    private array $clients = [];
    private array $indices = [];
    private array $config;
    
    public function __construct(array $config = [])
    {
        parent::__construct('elasticsearch');
        $this->config = array_merge([
            'default_url' => 'http://localhost:9200',
            'timeout' => 30,
            'retry_attempts' => 3,
            'retry_delay' => 1000,
            'enable_tls' => false,
            'tls_verify' => true,
            'tls_cert' => '',
            'tls_key' => '',
            'tls_ca' => '',
            'username' => '',
            'password' => '',
            'api_key' => ''
        ], $config);
    }

    /**
     * Execute Elasticsearch operation
     */
    public function execute(array $params, array $context = []): mixed
    {
        $this->validateParams($params);
        
        $action = $params['action'] ?? '';
        $index = $params['index'] ?? '';
        $id = $params['id'] ?? '';
        $body = $params['body'] ?? [];
        $query = $params['query'] ?? [];
        $url = $params['url'] ?? $this->config['default_url'];
        $size = $params['size'] ?? 10;
        $from = $params['from'] ?? 0;
        $sort = $params['sort'] ?? [];
        $aggs = $params['aggs'] ?? [];
        $scroll = $params['scroll'] ?? '';
        $searchAfter = $params['search_after'] ?? [];
        
        try {
            $client = $this->getClient($url);
            
            switch ($action) {
                case 'index':
                    return $this->indexDocument($client, $index, $id, $body);
                case 'get':
                    return $this->getDocument($client, $index, $id);
                case 'update':
                    return $this->updateDocument($client, $index, $id, $body);
                case 'delete':
                    return $this->deleteDocument($client, $index, $id);
                case 'search':
                    return $this->searchDocuments($client, $index, $query, $size, $from, $sort, $aggs, $searchAfter);
                case 'scroll':
                    return $this->scrollSearch($client, $scroll);
                case 'bulk':
                    return $this->bulkOperation($client, $body);
                case 'create_index':
                    return $this->createIndex($client, $index, $body);
                case 'delete_index':
                    return $this->deleteIndex($client, $index);
                case 'mapping':
                    return $this->getMapping($client, $index);
                case 'health':
                    return $this->getClusterHealth($client);
                case 'stats':
                    return $this->getIndexStats($client, $index);
                default:
                    throw new OperatorException("Unsupported action: $action");
            }
        } catch (\Exception $e) {
            throw new OperatorException("Elasticsearch operation failed: " . $e->getMessage(), 0, $e);
        }
    }

    /**
     * Index document
     */
    private function indexDocument($client, string $index, string $id, array $body): array
    {
        $url = "/$index/_doc";
        if ($id) {
            $url = "/$index/_doc/$id";
        }
        
        $response = $client->post($url, ['json' => $body]);
        
        if ($response->getStatusCode() !== 200 && $response->getStatusCode() !== 201) {
            throw new OperatorException("Failed to index document: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'indexed',
            'index' => $index,
            'id' => $data['_id'],
            'result' => $data['result'],
            'version' => $data['_version']
        ];
    }

    /**
     * Get document
     */
    private function getDocument($client, string $index, string $id): array
    {
        $response = $client->get("/$index/_doc/$id");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get document: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'index' => $data['_index'],
            'id' => $data['_id'],
            'version' => $data['_version'],
            'source' => $data['_source']
        ];
    }

    /**
     * Update document
     */
    private function updateDocument($client, string $index, string $id, array $body): array
    {
        $response = $client->post("/$index/_update/$id", ['json' => $body]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to update document: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'updated',
            'index' => $index,
            'id' => $id,
            'result' => $data['result'],
            'version' => $data['_version']
        ];
    }

    /**
     * Delete document
     */
    private function deleteDocument($client, string $index, string $id): array
    {
        $response = $client->delete("/$index/_doc/$id");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to delete document: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'deleted',
            'index' => $index,
            'id' => $id,
            'result' => $data['result']
        ];
    }

    /**
     * Search documents
     */
    private function searchDocuments($client, string $index, array $query, int $size, int $from, array $sort, array $aggs, array $searchAfter): array
    {
        $body = [];
        
        if (!empty($query)) {
            $body['query'] = $query;
        }
        
        if ($size > 0) {
            $body['size'] = $size;
        }
        
        if ($from > 0) {
            $body['from'] = $from;
        }
        
        if (!empty($sort)) {
            $body['sort'] = $sort;
        }
        
        if (!empty($aggs)) {
            $body['aggs'] = $aggs;
        }
        
        if (!empty($searchAfter)) {
            $body['search_after'] = $searchAfter;
        }
        
        $response = $client->post("/$index/_search", ['json' => $body]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to search documents: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        $hits = array_map(function($hit) {
            return [
                'index' => $hit['_index'],
                'id' => $hit['_id'],
                'score' => $hit['_score'],
                'source' => $hit['_source']
            ];
        }, $data['hits']['hits'] ?? []);
        
        return [
            'status' => 'searched',
            'index' => $index,
            'total' => $data['hits']['total']['value'] ?? 0,
            'max_score' => $data['hits']['max_score'] ?? 0,
            'hits' => $hits,
            'aggregations' => $data['aggregations'] ?? [],
            'scroll_id' => $data['_scroll_id'] ?? null
        ];
    }

    /**
     * Scroll search
     */
    private function scrollSearch($client, string $scrollId): array
    {
        $response = $client->post('/_search/scroll', [
            'json' => [
                'scroll_id' => $scrollId,
                'scroll' => '1m'
            ]
        ]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to scroll search: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        $hits = array_map(function($hit) {
            return [
                'index' => $hit['_index'],
                'id' => $hit['_id'],
                'score' => $hit['_score'],
                'source' => $hit['_source']
            ];
        }, $data['hits']['hits'] ?? []);
        
        return [
            'status' => 'scrolled',
            'total' => $data['hits']['total']['value'] ?? 0,
            'hits' => $hits,
            'scroll_id' => $data['_scroll_id'] ?? null
        ];
    }

    /**
     * Bulk operation
     */
    private function bulkOperation($client, array $operations): array
    {
        $body = '';
        
        foreach ($operations as $operation) {
            $action = $operation['action'] ?? '';
            $index = $operation['index'] ?? '';
            $id = $operation['id'] ?? '';
            $data = $operation['data'] ?? [];
            
            $header = ['index' => ['_index' => $index]];
            if ($id) {
                $header['index']['_id'] = $id;
            }
            
            $body .= json_encode($header) . "\n";
            $body .= json_encode($data) . "\n";
        }
        
        $response = $client->post('/_bulk', [
            'body' => $body,
            'headers' => ['Content-Type' => 'application/x-ndjson']
        ]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to execute bulk operation: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'bulk_executed',
            'took' => $data['took'],
            'errors' => $data['errors'],
            'items' => $data['items'] ?? []
        ];
    }

    /**
     * Create index
     */
    private function createIndex($client, string $index, array $body): array
    {
        $response = $client->put("/$index", ['json' => $body]);
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to create index: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'created',
            'index' => $index,
            'acknowledged' => $data['acknowledged'],
            'shards_acknowledged' => $data['shards_acknowledged'] ?? false
        ];
    }

    /**
     * Delete index
     */
    private function deleteIndex($client, string $index): array
    {
        $response = $client->delete("/$index");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to delete index: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'deleted',
            'index' => $index,
            'acknowledged' => $data['acknowledged']
        ];
    }

    /**
     * Get mapping
     */
    private function getMapping($client, string $index): array
    {
        $response = $client->get("/$index/_mapping");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get mapping: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'index' => $index,
            'mapping' => $data[$index]['mappings'] ?? []
        ];
    }

    /**
     * Get cluster health
     */
    private function getClusterHealth($client): array
    {
        $response = $client->get('/_cluster/health');
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get cluster health: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'cluster_name' => $data['cluster_name'],
            'status' => $data['status'],
            'number_of_nodes' => $data['number_of_nodes'],
            'active_primary_shards' => $data['active_primary_shards'],
            'active_shards' => $data['active_shards'],
            'relocating_shards' => $data['relocating_shards'],
            'initializing_shards' => $data['initializing_shards'],
            'unassigned_shards' => $data['unassigned_shards']
        ];
    }

    /**
     * Get index stats
     */
    private function getIndexStats($client, string $index): array
    {
        $response = $client->get("/$index/_stats");
        
        if ($response->getStatusCode() !== 200) {
            throw new OperatorException("Failed to get index stats: " . $response->getBody());
        }
        
        $data = json_decode($response->getBody(), true);
        
        return [
            'status' => 'retrieved',
            'index' => $index,
            'stats' => $data['indices'][$index] ?? []
        ];
    }

    /**
     * Get or create Elasticsearch client
     */
    private function getClient(string $url): object
    {
        if (!isset($this->clients[$url])) {
            $this->clients[$url] = $this->createClient($url);
        }
        
        return $this->clients[$url];
    }

    /**
     * Create HTTP client for Elasticsearch
     */
    private function createClient(string $url): object
    {
        $config = [
            'base_uri' => $url,
            'timeout' => $this->config['timeout'],
            'headers' => [
                'Content-Type' => 'application/json'
            ]
        ];
        
        if ($this->config['username'] && $this->config['password']) {
            $config['auth'] = [$this->config['username'], $this->config['password']];
        }
        
        if ($this->config['api_key']) {
            $config['headers']['Authorization'] = 'ApiKey ' . $this->config['api_key'];
        }
        
        if ($this->config['enable_tls']) {
            $config['verify'] = $this->config['tls_verify'];
            if ($this->config['tls_cert'] && $this->config['tls_key']) {
                $config['cert'] = [$this->config['tls_cert'], $this->config['tls_key']];
            }
            if ($this->config['tls_ca']) {
                $config['verify'] = $this->config['tls_ca'];
            }
        }
        
        return new \GuzzleHttp\Client($config);
    }

    /**
     * Validate operator parameters
     */
    private function validateParams(array $params): void
    {
        if (!isset($params['action'])) {
            throw new OperatorException("Action is required");
        }
        
        $validActions = ['index', 'get', 'update', 'delete', 'search', 'scroll', 'bulk', 'create_index', 'delete_index', 'mapping', 'health', 'stats'];
        if (!in_array($params['action'], $validActions)) {
            throw new OperatorException("Invalid action: " . $params['action']);
        }
        
        if (in_array($params['action'], ['index', 'get', 'update', 'delete', 'mapping', 'stats']) && !isset($params['index'])) {
            throw new OperatorException("Index is required for " . $params['action'] . " action");
        }
        
        if (in_array($params['action'], ['get', 'update', 'delete']) && !isset($params['id'])) {
            throw new OperatorException("ID is required for " . $params['action'] . " action");
        }
        
        if (in_array($params['action'], ['index', 'update']) && !isset($params['body'])) {
            throw new OperatorException("Body is required for " . $params['action'] . " action");
        }
        
        if ($params['action'] === 'search' && !isset($params['index'])) {
            throw new OperatorException("Index is required for search action");
        }
        
        if ($params['action'] === 'scroll' && !isset($params['scroll'])) {
            throw new OperatorException("Scroll ID is required for scroll action");
        }
    }

    /**
     * Cleanup resources
     */
    public function cleanup(): void
    {
        $this->clients = [];
        $this->indices = [];
    }

    /**
     * Get operator schema
     */
    public function getSchema(): array
    {
        return [
            'type' => 'object',
            'properties' => [
                'action' => [
                    'type' => 'string',
                    'enum' => ['index', 'get', 'update', 'delete', 'search', 'scroll', 'bulk', 'create_index', 'delete_index', 'mapping', 'health', 'stats'],
                    'description' => 'Elasticsearch action'
                ],
                'index' => ['type' => 'string', 'description' => 'Index name'],
                'id' => ['type' => 'string', 'description' => 'Document ID'],
                'body' => ['type' => 'object', 'description' => 'Document body'],
                'query' => ['type' => 'object', 'description' => 'Search query'],
                'url' => ['type' => 'string', 'description' => 'Elasticsearch URL'],
                'size' => ['type' => 'integer', 'description' => 'Result size'],
                'from' => ['type' => 'integer', 'description' => 'Result offset'],
                'sort' => ['type' => 'array', 'description' => 'Sort criteria'],
                'aggs' => ['type' => 'object', 'description' => 'Aggregations'],
                'scroll' => ['type' => 'string', 'description' => 'Scroll ID'],
                'search_after' => ['type' => 'array', 'description' => 'Search after values']
            ],
            'required' => ['action']
        ];
    }
} 