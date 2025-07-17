# Database Optimization in TuskLang with Rust

## 🗄️ Database Optimization Foundation

Database optimization with TuskLang and Rust provides powerful tools for building high-performance database applications. This guide covers connection pooling, query optimization, caching strategies, and advanced database techniques.

## 🏗️ Database Architecture

### Database Stack

```rust
[database_architecture]
connection_pooling: true
query_optimization: true
caching_strategies: true
transaction_management: true

[database_components]
sqlx: "async_sql"
diesel: "orm_framework"
redis: "caching_layer"
postgres: "primary_database"
```

### Database Configuration

```rust
[database_configuration]
connection_pool_size: 20
max_connections: 100
query_timeout: 30
enable_caching: true

[database_implementation]
use sqlx::{Pool, Postgres, postgres::PgPoolOptions};
use std::collections::HashMap;
use tokio::sync::RwLock;
use std::time::{Duration, Instant};

// Database manager
pub struct DatabaseManager {
    pool: Pool<Postgres>,
    config: DatabaseConfig,
    metrics: Arc<RwLock<DatabaseMetrics>>,
    query_cache: Arc<RwLock<HashMap<String, CachedQuery>>>,
}

#[derive(Debug, Clone)]
pub struct DatabaseConfig {
    pub connection_string: String,
    pub max_connections: u32,
    pub min_connections: u32,
    pub connection_timeout: Duration,
    pub query_timeout: Duration,
    pub enable_query_cache: bool,
    pub cache_ttl: Duration,
    pub enable_metrics: bool,
}

#[derive(Debug, Clone)]
pub struct DatabaseMetrics {
    pub total_queries: u64,
    pub successful_queries: u64,
    pub failed_queries: u64,
    pub avg_query_time: Duration,
    pub cache_hits: u64,
    pub cache_misses: u64,
    pub active_connections: u32,
    pub idle_connections: u32,
}

#[derive(Debug, Clone)]
pub struct CachedQuery {
    pub result: String,
    pub created_at: Instant,
    pub ttl: Duration,
}

impl DatabaseManager {
    pub async fn new(config: DatabaseConfig) -> Result<Self, sqlx::Error> {
        let pool = PgPoolOptions::new()
            .max_connections(config.max_connections)
            .min_connections(config.min_connections)
            .connect_timeout(config.connection_timeout)
            .acquire_timeout(config.connection_timeout)
            .idle_timeout(config.connection_timeout)
            .max_lifetime(config.connection_timeout * 2)
            .connect(&config.connection_string)
            .await?;
        
        Ok(Self {
            pool,
            config,
            metrics: Arc::new(RwLock::new(DatabaseMetrics {
                total_queries: 0,
                successful_queries: 0,
                failed_queries: 0,
                avg_query_time: Duration::ZERO,
                cache_hits: 0,
                cache_misses: 0,
                active_connections: 0,
                idle_connections: 0,
            })),
            query_cache: Arc::new(RwLock::new(HashMap::new())),
        })
    }
    
    pub async fn execute_query(&self, query: &str, params: &[&(dyn sqlx::Encode<'_, Postgres> + Send + Sync)]) -> Result<sqlx::postgres::PgRow, DatabaseError> {
        let start_time = Instant::now();
        
        // Check cache first
        if self.config.enable_query_cache {
            let cache_key = self.generate_cache_key(query, params);
            if let Some(cached_result) = self.get_cached_query(&cache_key).await {
                self.record_cache_hit().await;
                return Ok(cached_result);
            }
        }
        
        // Execute query
        let result = sqlx::query(query)
            .bind_all(params)
            .fetch_one(&self.pool)
            .await;
        
        let query_time = start_time.elapsed();
        self.record_query_metrics(result.is_ok(), query_time).await;
        
        match result {
            Ok(row) => {
                // Cache result if enabled
                if self.config.enable_query_cache {
                    let cache_key = self.generate_cache_key(query, params);
                    self.cache_query(&cache_key, &row).await;
                }
                Ok(row)
            }
            Err(e) => Err(DatabaseError::QueryFailed { message: e.to_string() }),
        }
    }
    
    pub async fn execute_transaction<F, T>(&self, f: F) -> Result<T, DatabaseError>
    where
        F: for<'a> FnOnce(&'a mut sqlx::Transaction<'a, Postgres>) -> std::pin::Pin<Box<dyn std::future::Future<Output = Result<T, DatabaseError>> + Send + 'a>>,
    {
        let mut transaction = self.pool.begin().await
            .map_err(|e| DatabaseError::TransactionFailed { message: e.to_string() })?;
        
        let result = f(&mut transaction).await;
        
        match result {
            Ok(value) => {
                transaction.commit().await
                    .map_err(|e| DatabaseError::TransactionFailed { message: e.to_string() })?;
                Ok(value)
            }
            Err(e) => {
                transaction.rollback().await
                    .map_err(|_| DatabaseError::TransactionFailed { message: "Rollback failed".to_string() })?;
                Err(e)
            }
        }
    }
    
    async fn generate_cache_key(&self, query: &str, params: &[&(dyn sqlx::Encode<'_, Postgres> + Send + Sync)]) -> String {
        // Simple cache key generation - in production, use a proper hash
        format!("{}:{}", query, params.len())
    }
    
    async fn get_cached_query(&self, cache_key: &str) -> Option<sqlx::postgres::PgRow> {
        let cache = self.query_cache.read().await;
        if let Some(cached) = cache.get(cache_key) {
            if cached.created_at.elapsed() < cached.ttl {
                return Some(cached.result.clone());
            }
        }
        None
    }
    
    async fn cache_query(&self, cache_key: &str, result: &sqlx::postgres::PgRow) {
        let mut cache = self.query_cache.write().await;
        cache.insert(cache_key.to_string(), CachedQuery {
            result: result.to_string(),
            created_at: Instant::now(),
            ttl: self.config.cache_ttl,
        });
    }
    
    async fn record_cache_hit(&self) {
        let mut metrics = self.metrics.write().await;
        metrics.cache_hits += 1;
    }
    
    async fn record_query_metrics(&self, success: bool, query_time: Duration) {
        let mut metrics = self.metrics.write().await;
        metrics.total_queries += 1;
        
        if success {
            metrics.successful_queries += 1;
        } else {
            metrics.failed_queries += 1;
        }
        
        // Update average query time
        let total_time = metrics.avg_query_time * (metrics.total_queries - 1) + query_time;
        metrics.avg_query_time = total_time / metrics.total_queries;
    }
    
    pub async fn get_metrics(&self) -> DatabaseMetrics {
        self.metrics.read().await.clone()
    }
    
    pub async fn clear_cache(&self) {
        self.query_cache.write().await.clear();
    }
}

#[derive(Debug, thiserror::Error)]
pub enum DatabaseError {
    #[error("Query failed: {message}")]
    QueryFailed { message: String },
    #[error("Transaction failed: {message}")]
    TransactionFailed { message: String },
    #[error("Connection failed: {message}")]
    ConnectionFailed { message: String },
    #[error("Cache error: {message}")]
    CacheError { message: String },
}
```

## 🔗 Connection Pooling

### Advanced Connection Pool

```rust
[connection_pooling]
pool_management: true
connection_monitoring: true
load_balancing: true

[pool_implementation]
use std::sync::atomic::{AtomicU32, Ordering};
use tokio::time::interval;

// Advanced connection pool
pub struct AdvancedConnectionPool {
    pools: Vec<Pool<Postgres>>,
    current_pool: AtomicU32,
    config: PoolConfig,
    health_checker: Arc<RwLock<HealthChecker>>,
}

#[derive(Debug, Clone)]
pub struct PoolConfig {
    pub primary_connection_string: String,
    pub replica_connection_strings: Vec<String>,
    pub max_connections_per_pool: u32,
    pub min_connections_per_pool: u32,
    pub connection_timeout: Duration,
    pub health_check_interval: Duration,
    pub load_balancing_strategy: LoadBalancingStrategy,
}

#[derive(Debug, Clone)]
pub enum LoadBalancingStrategy {
    RoundRobin,
    LeastConnections,
    Weighted,
}

pub struct HealthChecker {
    pool_health: HashMap<usize, bool>,
    last_check: HashMap<usize, Instant>,
}

impl AdvancedConnectionPool {
    pub async fn new(config: PoolConfig) -> Result<Self, sqlx::Error> {
        let mut pools = Vec::new();
        
        // Create primary pool
        let primary_pool = PgPoolOptions::new()
            .max_connections(config.max_connections_per_pool)
            .min_connections(config.min_connections_per_pool)
            .connect_timeout(config.connection_timeout)
            .connect(&config.primary_connection_string)
            .await?;
        
        pools.push(primary_pool);
        
        // Create replica pools
        for connection_string in &config.replica_connection_strings {
            let pool = PgPoolOptions::new()
                .max_connections(config.max_connections_per_pool)
                .min_connections(config.min_connections_per_pool)
                .connect_timeout(config.connection_timeout)
                .connect(connection_string)
                .await?;
            
            pools.push(pool);
        }
        
        let health_checker = Arc::new(RwLock::new(HealthChecker {
            pool_health: HashMap::new(),
            last_check: HashMap::new(),
        }));
        
        let pool = Self {
            pools,
            current_pool: AtomicU32::new(0),
            config,
            health_checker: Arc::clone(&health_checker),
        };
        
        // Start health checking
        pool.start_health_checker().await;
        
        Ok(pool)
    }
    
    pub async fn get_connection(&self) -> Result<Pool<Postgres>, DatabaseError> {
        match self.config.load_balancing_strategy {
            LoadBalancingStrategy::RoundRobin => self.get_round_robin_connection().await,
            LoadBalancingStrategy::LeastConnections => self.get_least_connections_pool().await,
            LoadBalancingStrategy::Weighted => self.get_weighted_connection().await,
        }
    }
    
    async fn get_round_robin_connection(&self) -> Result<Pool<Postgres>, DatabaseError> {
        let mut attempts = 0;
        let max_attempts = self.pools.len();
        
        while attempts < max_attempts {
            let pool_index = self.current_pool.fetch_add(1, Ordering::Relaxed) as usize % self.pools.len();
            
            if self.is_pool_healthy(pool_index).await {
                return Ok(self.pools[pool_index].clone());
            }
            
            attempts += 1;
        }
        
        Err(DatabaseError::ConnectionFailed { message: "No healthy pools available".to_string() })
    }
    
    async fn get_least_connections_pool(&self) -> Result<Pool<Postgres>, DatabaseError> {
        let mut best_pool = None;
        let mut min_connections = u32::MAX;
        
        for (index, pool) in self.pools.iter().enumerate() {
            if self.is_pool_healthy(index).await {
                let size = pool.size();
                if size < min_connections {
                    min_connections = size;
                    best_pool = Some(pool.clone());
                }
            }
        }
        
        best_pool.ok_or_else(|| DatabaseError::ConnectionFailed { message: "No healthy pools available".to_string() })
    }
    
    async fn get_weighted_connection(&self) -> Result<Pool<Postgres>, DatabaseError> {
        // Simple weighted strategy - primary gets higher weight
        if self.is_pool_healthy(0).await {
            return Ok(self.pools[0].clone());
        }
        
        // Fallback to round-robin for replicas
        for index in 1..self.pools.len() {
            if self.is_pool_healthy(index).await {
                return Ok(self.pools[index].clone());
            }
        }
        
        Err(DatabaseError::ConnectionFailed { message: "No healthy pools available".to_string() })
    }
    
    async fn is_pool_healthy(&self, pool_index: usize) -> bool {
        let health_checker = self.health_checker.read().await;
        health_checker.pool_health.get(&pool_index).copied().unwrap_or(false)
    }
    
    async fn start_health_checker(&self) {
        let health_checker = Arc::clone(&self.health_checker);
        let pools = self.pools.clone();
        let interval_duration = self.config.health_check_interval;
        
        tokio::spawn(async move {
            let mut interval = interval(interval_duration);
            
            loop {
                interval.tick().await;
                
                let mut checker = health_checker.write().await;
                let now = Instant::now();
                
                for (index, pool) in pools.iter().enumerate() {
                    // Check if we need to perform health check
                    let last_check = checker.last_check.get(&index).copied().unwrap_or(Instant::now() - interval_duration);
                    if now.duration_since(last_check) >= interval_duration {
                        let is_healthy = Self::check_pool_health(pool).await;
                        checker.pool_health.insert(index, is_healthy);
                        checker.last_check.insert(index, now);
                    }
                }
            }
        });
    }
    
    async fn check_pool_health(pool: &Pool<Postgres>) -> bool {
        match sqlx::query("SELECT 1").fetch_one(pool).await {
            Ok(_) => true,
            Err(_) => false,
        }
    }
}
```

## 🔍 Query Optimization

### Query Optimizer

```rust
[query_optimization]
query_analysis: true
index_optimization: true
query_rewriting: true

[optimization_implementation]
use std::collections::HashMap;

// Query optimizer
pub struct QueryOptimizer {
    query_stats: Arc<RwLock<HashMap<String, QueryStats>>>,
    index_recommendations: Arc<RwLock<Vec<IndexRecommendation>>>,
    slow_query_threshold: Duration,
}

#[derive(Debug, Clone)]
pub struct QueryStats {
    pub query_hash: String,
    pub execution_count: u64,
    pub total_execution_time: Duration,
    pub avg_execution_time: Duration,
    pub min_execution_time: Duration,
    pub max_execution_time: Duration,
    pub last_executed: Instant,
}

#[derive(Debug, Clone)]
pub struct IndexRecommendation {
    pub table_name: String,
    pub column_name: String,
    pub index_type: IndexType,
    pub estimated_improvement: f64,
    pub reason: String,
}

#[derive(Debug, Clone)]
pub enum IndexType {
    BTree,
    Hash,
    GIN,
    GIST,
}

impl QueryOptimizer {
    pub fn new(slow_query_threshold: Duration) -> Self {
        Self {
            query_stats: Arc::new(RwLock::new(HashMap::new())),
            index_recommendations: Arc::new(RwLock::new(Vec::new())),
            slow_query_threshold,
        }
    }
    
    pub async fn analyze_query(&self, query: &str, execution_time: Duration) -> QueryAnalysis {
        let query_hash = self.hash_query(query);
        
        // Update query statistics
        {
            let mut stats = self.query_stats.write().await;
            let stat = stats.entry(query_hash.clone()).or_insert_with(|| QueryStats {
                query_hash: query_hash.clone(),
                execution_count: 0,
                total_execution_time: Duration::ZERO,
                avg_execution_time: Duration::ZERO,
                min_execution_time: Duration::MAX,
                max_execution_time: Duration::ZERO,
                last_executed: Instant::now(),
            });
            
            stat.execution_count += 1;
            stat.total_execution_time += execution_time;
            stat.avg_execution_time = stat.total_execution_time / stat.execution_count;
            stat.min_execution_time = stat.min_execution_time.min(execution_time);
            stat.max_execution_time = stat.max_execution_time.max(execution_time);
            stat.last_executed = Instant::now();
        }
        
        // Analyze query performance
        let is_slow = execution_time > self.slow_query_threshold;
        let recommendations = if is_slow {
            self.generate_recommendations(query).await
        } else {
            Vec::new()
        };
        
        QueryAnalysis {
            query_hash,
            execution_time,
            is_slow,
            recommendations,
        }
    }
    
    pub async fn optimize_query(&self, query: &str) -> String {
        let mut optimized_query = query.to_string();
        
        // Apply query optimizations
        optimized_query = self.rewrite_select_star(&optimized_query);
        optimized_query = self.add_limit_if_missing(&optimized_query);
        optimized_query = self.optimize_joins(&optimized_query);
        optimized_query = self.optimize_where_clause(&optimized_query);
        
        optimized_query
    }
    
    fn hash_query(&self, query: &str) -> String {
        // Simple hash - in production, use a proper cryptographic hash
        use std::collections::hash_map::DefaultHasher;
        use std::hash::{Hash, Hasher};
        
        let mut hasher = DefaultHasher::new();
        query.hash(&mut hasher);
        format!("{:x}", hasher.finish())
    }
    
    async fn generate_recommendations(&self, query: &str) -> Vec<IndexRecommendation> {
        let mut recommendations = Vec::new();
        
        // Analyze WHERE clauses for potential indexes
        if let Some(where_clause) = self.extract_where_clause(query) {
            for column in self.extract_columns_from_where(&where_clause) {
                recommendations.push(IndexRecommendation {
                    table_name: "unknown".to_string(), // Would need to parse table name
                    column_name: column,
                    index_type: IndexType::BTree,
                    estimated_improvement: 0.8,
                    reason: "Frequent filtering on this column".to_string(),
                });
            }
        }
        
        // Analyze JOIN clauses
        if let Some(join_clause) = self.extract_join_clause(query) {
            for column in self.extract_columns_from_join(&join_clause) {
                recommendations.push(IndexRecommendation {
                    table_name: "unknown".to_string(),
                    column_name: column,
                    index_type: IndexType::BTree,
                    estimated_improvement: 0.9,
                    reason: "JOIN condition on this column".to_string(),
                });
            }
        }
        
        recommendations
    }
    
    fn rewrite_select_star(&self, query: &str) -> String {
        // Replace SELECT * with specific columns when possible
        if query.to_lowercase().contains("select *") {
            // This would require schema information to be truly effective
            query.replace("SELECT *", "SELECT id, name, created_at")
        } else {
            query.to_string()
        }
    }
    
    fn add_limit_if_missing(&self, query: &str) -> String {
        // Add LIMIT clause to prevent large result sets
        if !query.to_lowercase().contains("limit") && query.to_lowercase().contains("select") {
            format!("{} LIMIT 1000", query)
        } else {
            query.to_string()
        }
    }
    
    fn optimize_joins(&self, query: &str) -> String {
        // Optimize JOIN order (simplified)
        query.to_string()
    }
    
    fn optimize_where_clause(&self, query: &str) -> String {
        // Optimize WHERE clause order (simplified)
        query.to_string()
    }
    
    fn extract_where_clause(&self, query: &str) -> Option<&str> {
        let query_lower = query.to_lowercase();
        if let Some(where_pos) = query_lower.find("where") {
            if let Some(order_pos) = query_lower.find("order by") {
                Some(&query[where_pos + 5..order_pos])
            } else if let Some(limit_pos) = query_lower.find("limit") {
                Some(&query[where_pos + 5..limit_pos])
            } else {
                Some(&query[where_pos + 5..])
            }
        } else {
            None
        }
    }
    
    fn extract_join_clause(&self, query: &str) -> Option<&str> {
        let query_lower = query.to_lowercase();
        if let Some(join_pos) = query_lower.find("join") {
            if let Some(where_pos) = query_lower.find("where") {
                Some(&query[join_pos..where_pos])
            } else {
                Some(&query[join_pos..])
            }
        } else {
            None
        }
    }
    
    fn extract_columns_from_where(&self, where_clause: &str) -> Vec<String> {
        // Simple column extraction - would need proper SQL parsing in production
        where_clause
            .split_whitespace()
            .filter(|word| word.contains('.') || word.chars().all(|c| c.is_alphanumeric() || c == '_'))
            .map(|s| s.to_string())
            .collect()
    }
    
    fn extract_columns_from_join(&self, join_clause: &str) -> Vec<String> {
        // Simple column extraction for JOIN clauses
        join_clause
            .split_whitespace()
            .filter(|word| word.contains('.') || word.chars().all(|c| c.is_alphanumeric() || c == '_'))
            .map(|s| s.to_string())
            .collect()
    }
    
    pub async fn get_slow_queries(&self) -> Vec<QueryStats> {
        let stats = self.query_stats.read().await;
        stats.values()
            .filter(|stat| stat.avg_execution_time > self.slow_query_threshold)
            .cloned()
            .collect()
    }
    
    pub async fn get_index_recommendations(&self) -> Vec<IndexRecommendation> {
        self.index_recommendations.read().await.clone()
    }
}

#[derive(Debug)]
pub struct QueryAnalysis {
    pub query_hash: String,
    pub execution_time: Duration,
    pub is_slow: bool,
    pub recommendations: Vec<IndexRecommendation>,
}
```

## 💾 Caching Strategies

### Multi-Level Caching

```rust
[caching_strategies]
l1_cache: true
l2_cache: true
distributed_cache: true

[caching_implementation]
use redis::{Client, Connection, Commands};
use std::collections::HashMap;

// Multi-level cache
pub struct MultiLevelCache {
    l1_cache: Arc<RwLock<HashMap<String, CacheEntry>>>,
    l2_cache: Arc<RwLock<RedisCache>>,
    config: CacheConfig,
}

#[derive(Debug, Clone)]
pub struct CacheConfig {
    pub l1_max_size: usize,
    pub l1_ttl: Duration,
    pub l2_ttl: Duration,
    pub enable_l1: bool,
    pub enable_l2: bool,
    pub redis_url: String,
}

#[derive(Debug, Clone)]
pub struct CacheEntry {
    pub value: String,
    pub created_at: Instant,
    pub ttl: Duration,
    pub access_count: u64,
}

pub struct RedisCache {
    client: Client,
}

impl RedisCache {
    pub fn new(redis_url: &str) -> Result<Self, redis::RedisError> {
        let client = Client::open(redis_url)?;
        Ok(Self { client })
    }
    
    pub async fn get(&self, key: &str) -> Result<Option<String>, redis::RedisError> {
        let mut conn = self.client.get_async_connection().await?;
        let result: Option<String> = conn.get(key).await?;
        Ok(result)
    }
    
    pub async fn set(&self, key: &str, value: &str, ttl: Duration) -> Result<(), redis::RedisError> {
        let mut conn = self.client.get_async_connection().await?;
        let ttl_secs = ttl.as_secs() as usize;
        conn.set_ex(key, value, ttl_secs).await?;
        Ok(())
    }
    
    pub async fn delete(&self, key: &str) -> Result<(), redis::RedisError> {
        let mut conn = self.client.get_async_connection().await?;
        conn.del(key).await?;
        Ok(())
    }
}

impl MultiLevelCache {
    pub async fn new(config: CacheConfig) -> Result<Self, redis::RedisError> {
        let l2_cache = if config.enable_l2 {
            Arc::new(RwLock::new(RedisCache::new(&config.redis_url)?))
        } else {
            Arc::new(RwLock::new(RedisCache::new("redis://localhost:6379")?))
        };
        
        Ok(Self {
            l1_cache: Arc::new(RwLock::new(HashMap::new())),
            l2_cache,
            config,
        })
    }
    
    pub async fn get(&self, key: &str) -> Option<String> {
        // Try L1 cache first
        if self.config.enable_l1 {
            if let Some(value) = self.get_from_l1(key).await {
                return Some(value);
            }
        }
        
        // Try L2 cache
        if self.config.enable_l2 {
            if let Some(value) = self.get_from_l2(key).await {
                // Store in L1 cache for future access
                if self.config.enable_l1 {
                    self.store_in_l1(key, &value).await;
                }
                return Some(value);
            }
        }
        
        None
    }
    
    pub async fn set(&self, key: &str, value: &str) -> Result<(), CacheError> {
        // Store in L1 cache
        if self.config.enable_l1 {
            self.store_in_l1(key, value).await;
        }
        
        // Store in L2 cache
        if self.config.enable_l2 {
            self.store_in_l2(key, value).await?;
        }
        
        Ok(())
    }
    
    pub async fn delete(&self, key: &str) -> Result<(), CacheError> {
        // Delete from L1 cache
        if self.config.enable_l1 {
            self.delete_from_l1(key).await;
        }
        
        // Delete from L2 cache
        if self.config.enable_l2 {
            self.delete_from_l2(key).await?;
        }
        
        Ok(())
    }
    
    async fn get_from_l1(&self, key: &str) -> Option<String> {
        let mut cache = self.l1_cache.write().await;
        
        if let Some(entry) = cache.get_mut(key) {
            // Check if entry is expired
            if entry.created_at.elapsed() > entry.ttl {
                cache.remove(key);
                return None;
            }
            
            // Update access count
            entry.access_count += 1;
            
            // Implement LRU eviction if cache is full
            if cache.len() >= self.config.l1_max_size {
                self.evict_lru_entry(&mut cache).await;
            }
            
            Some(entry.value.clone())
        } else {
            None
        }
    }
    
    async fn store_in_l1(&self, key: &str, value: &str) {
        let mut cache = self.l1_cache.write().await;
        
        // Evict if cache is full
        if cache.len() >= self.config.l1_max_size {
            self.evict_lru_entry(&mut cache).await;
        }
        
        cache.insert(key.to_string(), CacheEntry {
            value: value.to_string(),
            created_at: Instant::now(),
            ttl: self.config.l1_ttl,
            access_count: 1,
        });
    }
    
    async fn delete_from_l1(&self, key: &str) {
        self.l1_cache.write().await.remove(key);
    }
    
    async fn get_from_l2(&self, key: &str) -> Option<String> {
        let l2_cache = self.l2_cache.read().await;
        match l2_cache.get(key).await {
            Ok(value) => value,
            Err(_) => None,
        }
    }
    
    async fn store_in_l2(&self, key: &str, value: &str) -> Result<(), CacheError> {
        let l2_cache = self.l2_cache.read().await;
        l2_cache.set(key, value, self.config.l2_ttl).await
            .map_err(|e| CacheError::RedisError { message: e.to_string() })
    }
    
    async fn delete_from_l2(&self, key: &str) -> Result<(), CacheError> {
        let l2_cache = self.l2_cache.read().await;
        l2_cache.delete(key).await
            .map_err(|e| CacheError::RedisError { message: e.to_string() })
    }
    
    async fn evict_lru_entry(&self, cache: &mut HashMap<String, CacheEntry>) {
        // Find least recently used entry
        let lru_key = cache.iter()
            .min_by_key(|(_, entry)| entry.access_count)
            .map(|(key, _)| key.clone());
        
        if let Some(key) = lru_key {
            cache.remove(&key);
        }
    }
    
    pub async fn get_cache_stats(&self) -> CacheStats {
        let l1_size = self.l1_cache.read().await.len();
        let l2_size = 0; // Would need to implement size tracking for Redis
        
        CacheStats {
            l1_size,
            l2_size,
            l1_hit_rate: 0.0, // Would need to track hits/misses
            l2_hit_rate: 0.0,
        }
    }
}

#[derive(Debug, thiserror::Error)]
pub enum CacheError {
    #[error("Redis error: {message}")]
    RedisError { message: String },
    #[error("Serialization error: {message}")]
    SerializationError { message: String },
}

#[derive(Debug)]
pub struct CacheStats {
    pub l1_size: usize,
    pub l2_size: usize,
    pub l1_hit_rate: f64,
    pub l2_hit_rate: f64,
}
```

## 🎯 Best Practices

### 1. **Connection Management**
- Use connection pooling for high-throughput applications
- Monitor connection health and implement automatic failover
- Set appropriate connection timeouts and limits
- Implement connection retry logic

### 2. **Query Optimization**
- Analyze slow queries and optimize them
- Use appropriate indexes for frequently accessed columns
- Implement query caching for expensive operations
- Monitor query performance metrics

### 3. **Caching Strategy**
- Implement multi-level caching (L1, L2, distributed)
- Use appropriate cache TTL values
- Implement cache invalidation strategies
- Monitor cache hit rates and performance

### 4. **Transaction Management**
- Use transactions for data consistency
- Implement proper error handling and rollback
- Avoid long-running transactions
- Use appropriate isolation levels

### 5. **TuskLang Integration**
- Use TuskLang configuration for database parameters
- Implement database monitoring with TuskLang
- Configure caching strategies through TuskLang
- Use TuskLang for database schema management

Database optimization with TuskLang and Rust provides powerful tools for building high-performance, scalable database applications with comprehensive monitoring and optimization capabilities. 