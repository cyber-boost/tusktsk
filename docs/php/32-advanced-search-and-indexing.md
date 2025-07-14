# Advanced Search and Indexing with TuskLang

TuskLang revolutionizes search capabilities by providing configuration-driven search engines, intelligent indexing, and real-time search optimization that adapts to your application's needs.

## Overview

TuskLang's advanced search capabilities go beyond simple text matching, offering semantic search, faceted search, real-time indexing, and intelligent relevance scoring that scales from simple applications to enterprise search systems.

```php
// Advanced Search Configuration
advanced_search = {
    enabled = true
    primary_engine = "elasticsearch"
    
    search_engines = {
        elasticsearch = {
            type = "elasticsearch"
            connection = {
                hosts = [@env(ELASTICSEARCH_HOST, "localhost:9200")]
                username = @env(ELASTICSEARCH_USERNAME)
                password = @env(ELASTICSEARCH_PASSWORD)
                ssl_verify = true
            }
            options = {
                timeout = 30
                retries = 3
                compression = true
            }
        }
        
        algolia = {
            type = "algolia"
            connection = {
                app_id = @env(ALGOLIA_APP_ID)
                api_key = @env(ALGOLIA_API_KEY)
                search_key = @env(ALGOLIA_SEARCH_KEY)
            }
            options = {
                timeout = 30
                retries = 3
            }
        }
        
        meilisearch = {
            type = "meilisearch"
            connection = {
                host = @env(MEILISEARCH_HOST, "http://localhost:7700")
                api_key = @env(MEILISEARCH_API_KEY)
            }
            options = {
                timeout = 30
                batch_size = 100
            }
        }
        
        postgres_fulltext = {
            type = "postgres_fulltext"
            connection = {
                host = @env(PG_HOST, "localhost")
                port = @env(PG_PORT, 5432)
                database = @env(PG_DATABASE)
                username = @env(PG_USERNAME)
                password = @env(PG_PASSWORD)
            }
            options = {
                language = "english"
                stemming = true
                stop_words = true
            }
        }
    }
    
    index_definitions = {
        products = {
            engine = "elasticsearch"
            mapping = {
                properties = {
                    id = { type = "keyword" }
                    name = { type = "text", analyzer = "standard" }
                    description = { type = "text", analyzer = "english" }
                    category = { type = "keyword" }
                    price = { type = "float" }
                    tags = { type = "keyword" }
                    created_at = { type = "date" }
                    location = { type = "geo_point" }
                }
            }
            settings = {
                number_of_shards = 3
                number_of_replicas = 1
                refresh_interval = "1s"
            }
            indexing = {
                batch_size = 100
                auto_refresh = true
                pipeline = "product_pipeline"
            }
        }
        
        users = {
            engine = "elasticsearch"
            mapping = {
                properties = {
                    id = { type = "keyword" }
                    username = { type = "text", analyzer = "standard" }
                    email = { type = "keyword" }
                    full_name = { type = "text", analyzer = "standard" }
                    bio = { type = "text", analyzer = "english" }
                    location = { type = "geo_point" }
                    skills = { type = "keyword" }
                    created_at = { type = "date" }
                }
            }
            settings = {
                number_of_shards = 2
                number_of_replicas = 1
            }
        }
        
        articles = {
            engine = "algolia"
            attributes = {
                searchable = ["title", "content", "tags"]
                filterable = ["category", "author", "published_date"]
                sortable = ["published_date", "views", "rating"]
                faceting = ["category", "tags", "author"]
            }
            ranking = {
                criteria = ["typo", "geo", "words", "filters", "proximity", "attribute", "exact", "custom"]
                custom_ranking = ["desc(views)", "desc(rating)"]
            }
        }
    }
    
    search_configuration = {
        query_analysis = {
            enabled = true
            analyzers = {
                standard = {
                    type = "standard"
                    stopwords = ["the", "a", "an", "and", "or", "but"]
                }
                english = {
                    type = "english"
                    stemming = true
                    stopwords = true
                }
                custom = {
                    type = "custom"
                    tokenizer = "standard"
                    filters = ["lowercase", "stop", "snowball"]
                }
            }
        }
        
        relevance_tuning = {
            enabled = true
            factors = {
                text_relevance = 0.4
                recency = 0.2
                popularity = 0.2
                user_preference = 0.2
            }
            boosting = {
                title = 3.0
                category = 2.0
                tags = 1.5
                description = 1.0
            }
        }
        
        faceted_search = {
            enabled = true
            facets = {
                category = {
                    type = "terms"
                    size = 20
                    order = "count"
                }
                price_range = {
                    type = "range"
                    ranges = [
                        { from = 0, to = 50 }
                        { from = 50, to = 100 }
                        { from = 100, to = 500 }
                        { from = 500 }
                    ]
                }
                location = {
                    type = "geo_distance"
                    origin = "user_location"
                    ranges = [
                        { from = 0, to = 10 }
                        { from = 10, to = 50 }
                        { from = 50 }
                    ]
                }
            }
        }
        
        autocomplete = {
            enabled = true
            suggestions = {
                products = {
                    fields = ["name", "category"]
                    max_suggestions = 10
                    min_word_length = 2
                }
                users = {
                    fields = ["username", "full_name"]
                    max_suggestions = 5
                    min_word_length = 2
                }
            }
        }
    }
    
    real_time_indexing = {
        enabled = true
        strategies = {
            immediate = {
                delay = 0
                batch_size = 1
            }
            near_real_time = {
                delay = "1 second"
                batch_size = 10
            }
            batch = {
                delay = "5 minutes"
                batch_size = 100
            }
        }
        
        change_detection = {
            enabled = true
            methods = ["database_triggers", "event_streams", "polling"]
            polling_interval = "30 seconds"
        }
    }
    
    monitoring = {
        metrics = {
            search_volume = true
            search_latency = true
            index_size = true
            index_rate = true
            query_analysis = true
        }
        
        alerting = {
            high_latency = {
                threshold = 1000
                severity = "warning"
                notification = ["slack", "email"]
            }
            
            index_failure = {
                severity = "critical"
                notification = ["pagerduty", "slack"]
            }
        }
    }
}
```

## Core Search Features

### 1. Multi-Engine Search Management

```php
// Search Manager Implementation
class SearchManager {
    private $config;
    private $engines = [];
    private $indices = [];
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->initializeEngines();
        $this->initializeIndices();
    }
    
    public function search($indexName, $query, $options = []) {
        $index = $this->indices[$indexName];
        
        if (!$index) {
            throw new IndexNotFoundException("Index not found: {$indexName}");
        }
        
        // Analyze query
        $analyzedQuery = $this->analyzeQuery($query, $options);
        
        // Execute search
        $results = $index->search($analyzedQuery, $options);
        
        // Post-process results
        $processedResults = $this->postProcessResults($results, $options);
        
        return $processedResults;
    }
    
    public function index($indexName, $document, $options = []) {
        $index = $this->indices[$indexName];
        
        if (!$index) {
            throw new IndexNotFoundException("Index not found: {$indexName}");
        }
        
        // Pre-process document
        $processedDocument = $this->preProcessDocument($document, $options);
        
        // Index document
        $result = $index->index($processedDocument, $options);
        
        return $result;
    }
    
    public function bulkIndex($indexName, $documents, $options = []) {
        $index = $this->indices[$indexName];
        
        if (!$index) {
            throw new IndexNotFoundException("Index not found: {$indexName}");
        }
        
        // Pre-process documents
        $processedDocuments = [];
        foreach ($documents as $document) {
            $processedDocuments[] = $this->preProcessDocument($document, $options);
        }
        
        // Bulk index documents
        $result = $index->bulkIndex($processedDocuments, $options);
        
        return $result;
    }
    
    public function delete($indexName, $documentId) {
        $index = $this->indices[$indexName];
        
        if (!$index) {
            throw new IndexNotFoundException("Index not found: {$indexName}");
        }
        
        return $index->delete($documentId);
    }
    
    public function getSuggestions($indexName, $query, $options = []) {
        $index = $this->indices[$indexName];
        
        if (!$index) {
            throw new IndexNotFoundException("Index not found: {$indexName}");
        }
        
        return $index->getSuggestions($query, $options);
    }
    
    private function initializeEngines() {
        $engineConfigs = $this->config->advanced_search->search_engines;
        
        foreach ($engineConfigs as $name => $config) {
            $this->engines[$name] = $this->createEngine($name, $config);
        }
    }
    
    private function initializeIndices() {
        $indexConfigs = $this->config->advanced_search->index_definitions;
        
        foreach ($indexConfigs as $name => $config) {
            $engine = $this->engines[$config->engine];
            $this->indices[$name] = $this->createIndex($name, $config, $engine);
        }
    }
    
    private function createEngine($name, $config) {
        switch ($config->type) {
            case 'elasticsearch':
                return new ElasticsearchEngine($config->connection, $config->options);
            case 'algolia':
                return new AlgoliaEngine($config->connection, $config->options);
            case 'meilisearch':
                return new MeilisearchEngine($config->connection, $config->options);
            case 'postgres_fulltext':
                return new PostgresFulltextEngine($config->connection, $config->options);
            default:
                throw new Exception("Unknown search engine: {$config->type}");
        }
    }
    
    private function createIndex($name, $config, $engine) {
        return new SearchIndex($name, $config, $engine);
    }
    
    private function analyzeQuery($query, $options) {
        $analysisConfig = $this->config->advanced_search->search_configuration->query_analysis;
        
        if (!$analysisConfig->enabled) {
            return $query;
        }
        
        // Apply query analysis
        $analyzed = $this->applyQueryAnalysis($query, $options);
        
        return $analyzed;
    }
    
    private function preProcessDocument($document, $options) {
        // Apply document preprocessing
        $processed = $document;
        
        // Add metadata
        $processed['_indexed_at'] = time();
        $processed['_version'] = $options['version'] ?? 1;
        
        return $processed;
    }
    
    private function postProcessResults($results, $options) {
        // Apply relevance tuning
        $relevanceConfig = $this->config->advanced_search->search_configuration->relevance_tuning;
        
        if ($relevanceConfig->enabled) {
            $results = $this->applyRelevanceTuning($results, $options);
        }
        
        return $results;
    }
}

// Elasticsearch Engine Implementation
class ElasticsearchEngine {
    private $client;
    private $options;
    
    public function __construct($connection, $options) {
        $this->client = new ElasticsearchClient($connection->hosts, [
            'username' => $connection->username,
            'password' => $connection->password,
            'ssl_verify' => $connection->ssl_verify,
            'timeout' => $options->timeout,
            'retries' => $options->retries
        ]);
        $this->options = $options;
    }
    
    public function search($index, $query, $options = []) {
        $params = [
            'index' => $index,
            'body' => $this->buildSearchQuery($query, $options)
        ];
        
        if (isset($options['from'])) {
            $params['from'] = $options['from'];
        }
        
        if (isset($options['size'])) {
            $params['size'] = $options['size'];
        }
        
        $response = $this->client->search($params);
        
        return $this->formatResults($response);
    }
    
    public function index($index, $document, $options = []) {
        $params = [
            'index' => $index,
            'id' => $document['id'] ?? null,
            'body' => $document
        ];
        
        if (isset($options['pipeline'])) {
            $params['pipeline'] = $options['pipeline'];
        }
        
        return $this->client->index($params);
    }
    
    public function bulkIndex($index, $documents, $options = []) {
        $body = [];
        
        foreach ($documents as $document) {
            $body[] = [
                'index' => [
                    '_index' => $index,
                    '_id' => $document['id'] ?? null
                ]
            ];
            $body[] = $document;
        }
        
        $params = [
            'body' => $body
        ];
        
        if (isset($options['pipeline'])) {
            $params['pipeline'] = $options['pipeline'];
        }
        
        return $this->client->bulk($params);
    }
    
    private function buildSearchQuery($query, $options) {
        $searchQuery = [
            'query' => [
                'bool' => [
                    'must' => [],
                    'should' => [],
                    'filter' => []
                ]
            ]
        ];
        
        // Add text search
        if (is_string($query)) {
            $searchQuery['query']['bool']['must'][] = [
                'multi_match' => [
                    'query' => $query,
                    'fields' => $options['fields'] ?? ['*'],
                    'type' => 'best_fields'
                ]
            ];
        } elseif (is_array($query)) {
            $searchQuery['query'] = $query;
        }
        
        // Add facets
        if (isset($options['facets'])) {
            $searchQuery['aggs'] = $this->buildFacets($options['facets']);
        }
        
        // Add sorting
        if (isset($options['sort'])) {
            $searchQuery['sort'] = $options['sort'];
        }
        
        return $searchQuery;
    }
    
    private function buildFacets($facets) {
        $aggs = [];
        
        foreach ($facets as $name => $config) {
            switch ($config->type) {
                case 'terms':
                    $aggs[$name] = [
                        'terms' => [
                            'field' => $config->field,
                            'size' => $config->size ?? 10
                        ]
                    ];
                    break;
                case 'range':
                    $aggs[$name] = [
                        'range' => [
                            'field' => $config->field,
                            'ranges' => $config->ranges
                        ]
                    ];
                    break;
            }
        }
        
        return $aggs;
    }
    
    private function formatResults($response) {
        $results = [
            'total' => $response['hits']['total']['value'],
            'hits' => [],
            'facets' => []
        ];
        
        foreach ($response['hits']['hits'] as $hit) {
            $results['hits'][] = [
                'id' => $hit['_id'],
                'score' => $hit['_score'],
                'source' => $hit['_source']
            ];
        }
        
        if (isset($response['aggregations'])) {
            $results['facets'] = $response['aggregations'];
        }
        
        return $results;
    }
}
```

### 2. Intelligent Relevance Tuning

```php
// Relevance Tuner Implementation
class RelevanceTuner {
    private $config;
    private $userPreferences;
    
    public function __construct($config) {
        $this->config = $config;
        $this->userPreferences = new UserPreferenceManager();
    }
    
    public function tuneResults($results, $query, $userContext = []) {
        $relevanceConfig = $this->config->relevance_tuning;
        
        if (!$relevanceConfig->enabled) {
            return $results;
        }
        
        $tunedResults = [];
        
        foreach ($results['hits'] as $hit) {
            $score = $this->calculateRelevanceScore($hit, $query, $userContext);
            $hit['tuned_score'] = $score;
            $tunedResults[] = $hit;
        }
        
        // Sort by tuned score
        usort($tunedResults, function($a, $b) {
            return $b['tuned_score'] <=> $a['tuned_score'];
        });
        
        $results['hits'] = $tunedResults;
        
        return $results;
    }
    
    private function calculateRelevanceScore($hit, $query, $userContext) {
        $factors = $this->config->relevance_tuning->factors;
        $score = 0;
        
        // Text relevance (original score)
        $score += $hit['score'] * $factors->text_relevance;
        
        // Recency boost
        if (isset($hit['source']['created_at'])) {
            $recencyScore = $this->calculateRecencyScore($hit['source']['created_at']);
            $score += $recencyScore * $factors->recency;
        }
        
        // Popularity boost
        if (isset($hit['source']['views']) || isset($hit['source']['rating'])) {
            $popularityScore = $this->calculatePopularityScore($hit['source']);
            $score += $popularityScore * $factors->popularity;
        }
        
        // User preference boost
        if (!empty($userContext)) {
            $preferenceScore = $this->calculatePreferenceScore($hit['source'], $userContext);
            $score += $preferenceScore * $factors->user_preference;
        }
        
        return $score;
    }
    
    private function calculateRecencyScore($createdAt) {
        $age = time() - strtotime($createdAt);
        $daysOld = $age / 86400;
        
        // Exponential decay: newer content gets higher scores
        return exp(-$daysOld / 30); // 30-day half-life
    }
    
    private function calculatePopularityScore($source) {
        $score = 0;
        
        if (isset($source['views'])) {
            $score += log($source['views'] + 1) / 10;
        }
        
        if (isset($source['rating'])) {
            $score += ($source['rating'] - 2.5) / 2.5; // Normalize to -1 to 1
        }
        
        return $score;
    }
    
    private function calculatePreferenceScore($source, $userContext) {
        $score = 0;
        $userPreferences = $this->userPreferences->getUserPreferences($userContext['user_id']);
        
        // Category preference
        if (isset($source['category']) && isset($userPreferences['categories'])) {
            $categoryPreference = $userPreferences['categories'][$source['category']] ?? 0;
            $score += $categoryPreference;
        }
        
        // Tag preference
        if (isset($source['tags']) && isset($userPreferences['tags'])) {
            foreach ($source['tags'] as $tag) {
                $tagPreference = $userPreferences['tags'][$tag] ?? 0;
                $score += $tagPreference;
            }
        }
        
        return $score;
    }
}
```

### 3. Real-Time Indexing

```php
// Real-Time Indexer Implementation
class RealTimeIndexer {
    private $config;
    private $searchManager;
    private $changeDetector;
    
    public function __construct($configPath) {
        $this->config = $this->loadConfig($configPath);
        $this->searchManager = new SearchManager($configPath);
        $this->changeDetector = new ChangeDetector($this->config->advanced_search->real_time_indexing);
    }
    
    public function startIndexing() {
        $changeDetection = $this->config->advanced_search->real_time_indexing->change_detection;
        
        if ($changeDetection->enabled) {
            foreach ($changeDetection->methods as $method) {
                $this->setupChangeDetection($method);
            }
        }
    }
    
    public function indexDocument($indexName, $document, $strategy = 'immediate') {
        $strategyConfig = $this->config->advanced_search->real_time_indexing->strategies->$strategy;
        
        if ($strategyConfig->delay == 0) {
            // Immediate indexing
            return $this->searchManager->index($indexName, $document);
        } else {
            // Delayed indexing
            $this->scheduleIndexing($indexName, $document, $strategyConfig->delay);
        }
    }
    
    public function bulkIndexDocuments($indexName, $documents, $strategy = 'batch') {
        $strategyConfig = $this->config->advanced_search->real_time_indexing->strategies->$strategy;
        
        if ($strategyConfig->batch_size == 1) {
            // Individual indexing
            foreach ($documents as $document) {
                $this->indexDocument($indexName, $document, $strategy);
            }
        } else {
            // Batch indexing
            $this->scheduleBulkIndexing($indexName, $documents, $strategyConfig);
        }
    }
    
    private function setupChangeDetection($method) {
        switch ($method) {
            case 'database_triggers':
                $this->setupDatabaseTriggers();
                break;
            case 'event_streams':
                $this->setupEventStreams();
                break;
            case 'polling':
                $this->setupPolling();
                break;
        }
    }
    
    private function setupDatabaseTriggers() {
        // Setup database triggers to detect changes
        $triggers = [
            'products' => [
                'INSERT' => 'index_product',
                'UPDATE' => 'update_product',
                'DELETE' => 'delete_product'
            ],
            'users' => [
                'INSERT' => 'index_user',
                'UPDATE' => 'update_user',
                'DELETE' => 'delete_user'
            ]
        ];
        
        foreach ($triggers as $table => $operations) {
            foreach ($operations as $operation => $function) {
                $this->createDatabaseTrigger($table, $operation, $function);
            }
        }
    }
    
    private function setupEventStreams() {
        // Setup event stream listeners
        $eventStream = new EventStream();
        
        $eventStream->subscribe('product.created', function($event) {
            $this->indexDocument('products', $event->data);
        });
        
        $eventStream->subscribe('product.updated', function($event) {
            $this->indexDocument('products', $event->data);
        });
        
        $eventStream->subscribe('product.deleted', function($event) {
            $this->searchManager->delete('products', $event->data['id']);
        });
    }
    
    private function setupPolling() {
        $pollingInterval = $this->config->advanced_search->real_time_indexing->change_detection->polling_interval;
        
        // Setup periodic polling for changes
        $this->schedulePolling($pollingInterval);
    }
    
    private function scheduleIndexing($indexName, $document, $delay) {
        $job = [
            'type' => 'index_document',
            'index_name' => $indexName,
            'document' => $document,
            'scheduled_at' => time() + $this->parseDelay($delay)
        ];
        
        $this->queueJob($job);
    }
    
    private function scheduleBulkIndexing($indexName, $documents, $strategyConfig) {
        $job = [
            'type' => 'bulk_index',
            'index_name' => $indexName,
            'documents' => $documents,
            'batch_size' => $strategyConfig->batch_size,
            'scheduled_at' => time() + $this->parseDelay($strategyConfig->delay)
        ];
        
        $this->queueJob($job);
    }
    
    private function parseDelay($delay) {
        if (is_numeric($delay)) {
            return $delay;
        }
        
        // Parse delay strings like "1 second", "5 minutes"
        $units = [
            'second' => 1,
            'minute' => 60,
            'hour' => 3600
        ];
        
        preg_match('/(\d+)\s+(\w+)/', $delay, $matches);
        $value = $matches[1];
        $unit = $matches[2];
        
        return $value * $units[$unit];
    }
}

// Change Detector Implementation
class ChangeDetector {
    private $config;
    private $lastCheck = [];
    
    public function __construct($config) {
        $this->config = $config;
    }
    
    public function detectChanges($table) {
        $lastCheck = $this->lastCheck[$table] ?? 0;
        $currentTime = time();
        
        // Query for changes since last check
        $changes = $this->queryChanges($table, $lastCheck);
        
        if (!empty($changes)) {
            $this->lastCheck[$table] = $currentTime;
            return $changes;
        }
        
        return [];
    }
    
    private function queryChanges($table, $since) {
        $sql = "SELECT * FROM {$table} WHERE updated_at > ? ORDER BY updated_at ASC";
        $connection = new DatabaseConnection();
        
        return $connection->query($sql, [date('Y-m-d H:i:s', $since)]);
    }
}
```

## Integration Patterns

### 1. Database-Driven Search Configuration

```php
// Live Database Queries in Search Config
search_system_data = {
    index_definitions = @query("
        SELECT 
            index_name,
            engine_type,
            mapping_config,
            settings_config,
            is_active
        FROM search_index_definitions 
        WHERE is_active = true
        ORDER BY index_name
    ")
    
    search_analytics = @query("
        SELECT 
            query_text,
            result_count,
            avg_click_position,
            search_latency,
            user_id,
            timestamp
        FROM search_analytics 
        WHERE timestamp >= NOW() - INTERVAL 30 DAY
        ORDER BY timestamp DESC
    ")
    
    index_performance = @query("
        SELECT 
            index_name,
            document_count,
            index_size_mb,
            indexing_rate,
            query_rate,
            avg_query_time
        FROM index_performance_metrics 
        WHERE recorded_at >= NOW() - INTERVAL 24 HOUR
        ORDER BY recorded_at DESC
    ")
    
    user_search_preferences = @query("
        SELECT 
            user_id,
            preferred_categories,
            search_filters,
            result_preferences,
            last_updated
        FROM user_search_preferences 
        WHERE last_updated >= NOW() - INTERVAL 7 DAY
        ORDER BY last_updated DESC
    ")
}
```

## Best Practices

### 1. Performance Optimization

```php
// Performance Configuration
performance_config = {
    indexing = {
        batch_size = 100
        concurrency = 5
        compression = true
        async_indexing = true
    }
    
    querying = {
        query_cache = true
        result_cache = true
        cache_ttl = 300
        max_result_size = 1000
    }
    
    monitoring = {
        performance_metrics = true
        slow_query_logging = true
        resource_usage_tracking = true
    }
}
```

### 2. Relevance and Quality

```php
// Quality Configuration
quality_config = {
    relevance_tuning = {
        automated_tuning = true
        a_b_testing = true
        user_feedback = true
    }
    
    content_quality = {
        duplicate_detection = true
        content_validation = true
        spam_filtering = true
    }
    
    search_analytics = {
        query_analysis = true
        click_tracking = true
        conversion_tracking = true
    }
}
```

This comprehensive advanced search and indexing documentation demonstrates how TuskLang revolutionizes search capabilities by providing intelligent, scalable, and real-time search solutions while maintaining the rebellious spirit and technical excellence that defines the TuskLang ecosystem. 