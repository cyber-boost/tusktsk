# Performance Optimization in TuskLang with Rust

## ⚡ Performance Foundation

Performance optimization with TuskLang and Rust provides powerful tools for building high-performance applications. This guide covers profiling, benchmarking, memory optimization, and advanced optimization techniques.

## 🏗️ Performance Architecture

### Optimization Strategy

```rust
[performance_architecture]
profiling: true
benchmarking: true
memory_optimization: true
cpu_optimization: true

[optimization_components]
criterion: "benchmarking_framework"
perf: "profiling_tools"
heim: "system_metrics"
rayon: "parallel_processing"
```

### Performance Configuration

```rust
[performance_configuration]
profiling_enabled: true
benchmarking_enabled: true
metrics_collection: true
optimization_level: "release"

[performance_implementation]
use std::time::{Duration, Instant};
use std::collections::HashMap;
use tokio::sync::RwLock;

// Performance monitor
pub struct PerformanceMonitor {
    metrics: Arc<RwLock<HashMap<String, MetricData>>>,
    config: PerformanceConfig,
}

#[derive(Debug, Clone)]
pub struct PerformanceConfig {
    pub enable_profiling: bool,
    pub enable_benchmarking: bool,
    pub metrics_retention_days: u32,
    pub performance_threshold_ms: u64,
}

#[derive(Debug, Clone)]
pub struct MetricData {
    pub count: u64,
    pub total_time: Duration,
    pub min_time: Duration,
    pub max_time: Duration,
    pub avg_time: Duration,
    pub last_updated: Instant,
}

impl PerformanceMonitor {
    pub fn new(config: PerformanceConfig) -> Self {
        Self {
            metrics: Arc::new(RwLock::new(HashMap::new())),
            config,
        }
    }
    
    pub async fn record_metric(&self, name: &str, duration: Duration) {
        if !self.config.enable_profiling {
            return;
        }
        
        let mut metrics = self.metrics.write().await;
        let metric = metrics.entry(name.to_string()).or_insert_with(|| MetricData {
            count: 0,
            total_time: Duration::ZERO,
            min_time: Duration::MAX,
            max_time: Duration::ZERO,
            avg_time: Duration::ZERO,
            last_updated: Instant::now(),
        });
        
        metric.count += 1;
        metric.total_time += duration;
        metric.min_time = metric.min_time.min(duration);
        metric.max_time = metric.max_time.max(duration);
        metric.avg_time = metric.total_time / metric.count;
        metric.last_updated = Instant::now();
    }
    
    pub async fn get_metrics(&self) -> HashMap<String, MetricData> {
        self.metrics.read().await.clone()
    }
    
    pub async fn get_metric(&self, name: &str) -> Option<MetricData> {
        self.metrics.read().await.get(name).cloned()
    }
    
    pub async fn clear_metrics(&self) {
        self.metrics.write().await.clear();
    }
}

// Performance profiler
pub struct PerformanceProfiler {
    monitor: Arc<PerformanceMonitor>,
}

impl PerformanceProfiler {
    pub fn new(monitor: Arc<PerformanceMonitor>) -> Self {
        Self { monitor }
    }
    
    pub async fn profile<F, T>(&self, name: &str, f: F) -> T
    where
        F: FnOnce() -> T,
    {
        let start = Instant::now();
        let result = f();
        let duration = start.elapsed();
        
        self.monitor.record_metric(name, duration).await;
        
        result
    }
    
    pub async fn profile_async<F, Fut, T>(&self, name: &str, f: F) -> T
    where
        F: FnOnce() -> Fut,
        Fut: Future<Output = T>,
    {
        let start = Instant::now();
        let result = f().await;
        let duration = start.elapsed();
        
        self.monitor.record_metric(name, duration).await;
        
        result
    }
}
```

## 📊 Profiling and Benchmarking

### Criterion Benchmarking

```rust
[profiling_benchmarking]
criterion_benchmarks: true
custom_benchmarks: true
performance_tests: true

[benchmarking_implementation]
use criterion::{black_box, criterion_group, criterion_main, Criterion};

// Benchmark functions
pub fn fibonacci_recursive(n: u64) -> u64 {
    match n {
        0 => 0,
        1 => 1,
        _ => fibonacci_recursive(n - 1) + fibonacci_recursive(n - 2),
    }
}

pub fn fibonacci_iterative(n: u64) -> u64 {
    if n <= 1 {
        return n;
    }
    
    let mut a = 0;
    let mut b = 1;
    
    for _ in 2..=n {
        let temp = a + b;
        a = b;
        b = temp;
    }
    
    b
}

pub fn fibonacci_memoized(n: u64) -> u64 {
    use std::collections::HashMap;
    use std::sync::Mutex;
    use once_cell::sync::Lazy;
    
    static CACHE: Lazy<Mutex<HashMap<u64, u64>>> = Lazy::new(|| Mutex::new(HashMap::new()));
    
    if n <= 1 {
        return n;
    }
    
    {
        let cache = CACHE.lock().unwrap();
        if let Some(&result) = cache.get(&n) {
            return result;
        }
    }
    
    let result = fibonacci_memoized(n - 1) + fibonacci_memoized(n - 2);
    
    {
        let mut cache = CACHE.lock().unwrap();
        cache.insert(n, result);
    }
    
    result
}

// Criterion benchmarks
pub fn fibonacci_benchmarks(c: &mut Criterion) {
    let mut group = c.benchmark_group("fibonacci");
    
    group.bench_function("recursive_10", |b| {
        b.iter(|| fibonacci_recursive(black_box(10)))
    });
    
    group.bench_function("iterative_10", |b| {
        b.iter(|| fibonacci_iterative(black_box(10)))
    });
    
    group.bench_function("memoized_10", |b| {
        b.iter(|| fibonacci_memoized(black_box(10)))
    });
    
    group.bench_function("recursive_20", |b| {
        b.iter(|| fibonacci_recursive(black_box(20)))
    });
    
    group.bench_function("iterative_20", |b| {
        b.iter(|| fibonacci_iterative(black_box(20)))
    });
    
    group.bench_function("memoized_20", |b| {
        b.iter(|| fibonacci_memoized(black_box(20)))
    });
    
    group.finish();
}

// Custom benchmarking framework
pub struct CustomBenchmark {
    name: String,
    iterations: usize,
    warmup_iterations: usize,
}

impl CustomBenchmark {
    pub fn new(name: String) -> Self {
        Self {
            name,
            iterations: 1000,
            warmup_iterations: 100,
        }
    }
    
    pub fn iterations(mut self, iterations: usize) -> Self {
        self.iterations = iterations;
        self
    }
    
    pub fn warmup_iterations(mut self, warmup: usize) -> Self {
        self.warmup_iterations = warmup;
        self
    }
    
    pub fn run<F>(&self, f: F) -> BenchmarkResult
    where
        F: Fn() + Copy,
    {
        // Warmup
        for _ in 0..self.warmup_iterations {
            f();
        }
        
        // Actual benchmark
        let mut times = Vec::with_capacity(self.iterations);
        
        for _ in 0..self.iterations {
            let start = Instant::now();
            f();
            times.push(start.elapsed());
        }
        
        // Calculate statistics
        let total_time: Duration = times.iter().sum();
        let avg_time = total_time / self.iterations as u32;
        let min_time = times.iter().min().unwrap_or(&Duration::ZERO);
        let max_time = times.iter().max().unwrap_or(&Duration::ZERO);
        
        BenchmarkResult {
            name: self.name.clone(),
            iterations: self.iterations,
            total_time,
            avg_time,
            min_time: *min_time,
            max_time: *max_time,
            times,
        }
    }
}

#[derive(Debug)]
pub struct BenchmarkResult {
    pub name: String,
    pub iterations: usize,
    pub total_time: Duration,
    pub avg_time: Duration,
    pub min_time: Duration,
    pub max_time: Duration,
    pub times: Vec<Duration>,
}

impl BenchmarkResult {
    pub fn print_summary(&self) {
        println!("Benchmark: {}", self.name);
        println!("  Iterations: {}", self.iterations);
        println!("  Total time: {:?}", self.total_time);
        println!("  Average time: {:?}", self.avg_time);
        println!("  Min time: {:?}", self.min_time);
        println!("  Max time: {:?}", self.max_time);
        println!("  Throughput: {:.2} ops/sec", 
                self.iterations as f64 / self.total_time.as_secs_f64());
    }
}
```

### Performance Testing

```rust
[performance_testing]
load_testing: true
stress_testing: true
endurance_testing: true

[testing_implementation]
use tokio::time::sleep;

// Load test framework
pub struct LoadTest {
    name: String,
    concurrency: usize,
    duration: Duration,
    requests_per_second: usize,
}

impl LoadTest {
    pub fn new(name: String) -> Self {
        Self {
            name,
            concurrency: 10,
            duration: Duration::from_secs(60),
            requests_per_second: 100,
        }
    }
    
    pub fn concurrency(mut self, concurrency: usize) -> Self {
        self.concurrency = concurrency;
        self
    }
    
    pub fn duration(mut self, duration: Duration) -> Self {
        self.duration = duration;
        self
    }
    
    pub fn requests_per_second(mut self, rps: usize) -> Self {
        self.requests_per_second = rps;
        self
    }
    
    pub async fn run<F, Fut>(&self, test_function: F) -> LoadTestResult
    where
        F: Fn() -> Fut + Send + Sync + 'static,
        Fut: Future<Output = Result<(), String>> + Send,
    {
        let start_time = Instant::now();
        let mut results = Vec::new();
        let mut handles = Vec::new();
        
        // Spawn worker tasks
        for worker_id in 0..self.concurrency {
            let test_function = test_function.clone();
            let worker_duration = self.duration;
            let requests_per_worker = self.requests_per_second / self.concurrency;
            
            let handle = tokio::spawn(async move {
                let mut worker_results = Vec::new();
                let interval = Duration::from_secs(1) / requests_per_worker as u32;
                
                while start_time.elapsed() < worker_duration {
                    let request_start = Instant::now();
                    let result = test_function().await;
                    let request_duration = request_start.elapsed();
                    
                    worker_results.push(WorkerResult {
                        worker_id,
                        success: result.is_ok(),
                        duration: request_duration,
                        error: result.err(),
                    });
                    
                    sleep(interval).await;
                }
                
                worker_results
            });
            
            handles.push(handle);
        }
        
        // Collect results
        for handle in handles {
            let worker_results = handle.await.unwrap();
            results.extend(worker_results);
        }
        
        LoadTestResult {
            name: self.name.clone(),
            total_requests: results.len(),
            successful_requests: results.iter().filter(|r| r.success).count(),
            failed_requests: results.iter().filter(|r| !r.success).count(),
            total_duration: start_time.elapsed(),
            avg_response_time: results.iter().map(|r| r.duration).sum::<Duration>() / results.len() as u32,
            min_response_time: results.iter().map(|r| r.duration).min().unwrap_or(Duration::ZERO),
            max_response_time: results.iter().map(|r| r.duration).max().unwrap_or(Duration::ZERO),
            results,
        }
    }
}

#[derive(Debug)]
pub struct WorkerResult {
    pub worker_id: usize,
    pub success: bool,
    pub duration: Duration,
    pub error: Option<String>,
}

#[derive(Debug)]
pub struct LoadTestResult {
    pub name: String,
    pub total_requests: usize,
    pub successful_requests: usize,
    pub failed_requests: usize,
    pub total_duration: Duration,
    pub avg_response_time: Duration,
    pub min_response_time: Duration,
    pub max_response_time: Duration,
    pub results: Vec<WorkerResult>,
}

impl LoadTestResult {
    pub fn print_summary(&self) {
        println!("Load Test: {}", self.name);
        println!("  Total requests: {}", self.total_requests);
        println!("  Successful: {}", self.successful_requests);
        println!("  Failed: {}", self.failed_requests);
        println!("  Success rate: {:.2}%", 
                (self.successful_requests as f64 / self.total_requests as f64) * 100.0);
        println!("  Total duration: {:?}", self.total_duration);
        println!("  Average response time: {:?}", self.avg_response_time);
        println!("  Min response time: {:?}", self.min_response_time);
        println!("  Max response time: {:?}", self.max_response_time);
        println!("  Throughput: {:.2} req/sec", 
                self.total_requests as f64 / self.total_duration.as_secs_f64());
    }
}
```

## 💾 Memory Optimization

### Memory Management

```rust
[memory_optimization]
allocation_optimization: true
memory_pools: true
garbage_collection: true

[memory_implementation]
use std::alloc::{alloc, dealloc, Layout};
use std::ptr;

// Memory pool
pub struct MemoryPool {
    block_size: usize,
    block_count: usize,
    free_blocks: Vec<*mut u8>,
    allocated_blocks: std::collections::HashSet<*mut u8>,
    layout: Layout,
}

impl MemoryPool {
    pub fn new(block_size: usize, block_count: usize) -> Result<Self, std::alloc::AllocError> {
        let layout = Layout::from_size_align(block_size, std::mem::align_of::<usize>())?;
        
        let mut pool = Self {
            block_size,
            block_count,
            free_blocks: Vec::new(),
            allocated_blocks: std::collections::HashSet::new(),
            layout,
        };
        
        // Pre-allocate blocks
        for _ in 0..block_count {
            let block = unsafe { alloc(layout) };
            if block.is_null() {
                return Err(std::alloc::AllocError);
            }
            pool.free_blocks.push(block);
        }
        
        Ok(pool)
    }
    
    pub fn allocate(&mut self) -> Option<*mut u8> {
        if let Some(block) = self.free_blocks.pop() {
            self.allocated_blocks.insert(block);
            Some(block)
        } else {
            None
        }
    }
    
    pub fn deallocate(&mut self, block: *mut u8) {
        if self.allocated_blocks.remove(&block) {
            self.free_blocks.push(block);
        }
    }
    
    pub fn available_blocks(&self) -> usize {
        self.free_blocks.len()
    }
    
    pub fn allocated_blocks(&self) -> usize {
        self.allocated_blocks.len()
    }
}

impl Drop for MemoryPool {
    fn drop(&mut self) {
        // Deallocate all blocks
        for block in self.free_blocks.drain(..) {
            unsafe {
                dealloc(block, self.layout);
            }
        }
        
        for block in self.allocated_blocks.drain() {
            unsafe {
                dealloc(block, self.layout);
            }
        }
    }
}

// Arena allocator
pub struct Arena {
    chunks: Vec<Vec<u8>>,
    current_chunk: usize,
    current_offset: usize,
    chunk_size: usize,
}

impl Arena {
    pub fn new(chunk_size: usize) -> Self {
        Self {
            chunks: vec![Vec::with_capacity(chunk_size)],
            current_chunk: 0,
            current_offset: 0,
            chunk_size,
        }
    }
    
    pub fn allocate<T>(&mut self, value: T) -> &mut T {
        let size = std::mem::size_of::<T>();
        let align = std::mem::align_of::<T>();
        
        // Ensure alignment
        let aligned_offset = (self.current_offset + align - 1) & !(align - 1);
        
        // Check if we need a new chunk
        if aligned_offset + size > self.chunks[self.current_chunk].capacity() {
            self.current_chunk += 1;
            self.current_offset = 0;
            
            if self.current_chunk >= self.chunks.len() {
                self.chunks.push(Vec::with_capacity(self.chunk_size));
            }
        }
        
        // Allocate in current chunk
        let chunk = &mut self.chunks[self.current_chunk];
        chunk.resize(aligned_offset + size, 0);
        
        let ptr = &mut chunk[aligned_offset] as *mut u8 as *mut T;
        unsafe {
            ptr.write(value);
            &mut *ptr
        }
    }
    
    pub fn clear(&mut self) {
        self.chunks.clear();
        self.chunks.push(Vec::with_capacity(self.chunk_size));
        self.current_chunk = 0;
        self.current_offset = 0;
    }
    
    pub fn memory_usage(&self) -> usize {
        self.chunks.iter().map(|chunk| chunk.capacity()).sum()
    }
}

// Smart pointer optimization
pub struct OptimizedVec<T> {
    data: Vec<T>,
    capacity_hint: usize,
}

impl<T> OptimizedVec<T> {
    pub fn new() -> Self {
        Self {
            data: Vec::new(),
            capacity_hint: 0,
        }
    }
    
    pub fn with_capacity(capacity: usize) -> Self {
        Self {
            data: Vec::with_capacity(capacity),
            capacity_hint: capacity,
        }
    }
    
    pub fn push(&mut self, item: T) {
        // Use capacity hint to avoid frequent reallocations
        if self.data.len() == self.data.capacity() && self.capacity_hint > 0 {
            let new_capacity = (self.data.capacity() * 2).max(self.capacity_hint);
            self.data.reserve(new_capacity - self.data.capacity());
        }
        
        self.data.push(item);
    }
    
    pub fn shrink_to_fit(&mut self) {
        self.data.shrink_to_fit();
    }
    
    pub fn clear(&mut self) {
        self.data.clear();
    }
    
    pub fn len(&self) -> usize {
        self.data.len()
    }
    
    pub fn capacity(&self) -> usize {
        self.data.capacity()
    }
    
    pub fn is_empty(&self) -> bool {
        self.data.is_empty()
    }
}

impl<T> std::ops::Deref for OptimizedVec<T> {
    type Target = [T];
    
    fn deref(&self) -> &Self::Target {
        &self.data
    }
}

impl<T> std::ops::DerefMut for OptimizedVec<T> {
    fn deref_mut(&mut self) -> &mut Self::Target {
        &mut self.data
    }
}
```

### Cache Optimization

```rust
[cache_optimization]
cache_alignment: true
cache_friendly_data: true
prefetching: true

[cache_implementation]
use std::collections::HashMap;

// Cache-friendly data structure
#[repr(C)]
pub struct CacheFriendlyStruct {
    pub id: u64,
    pub value: f64,
    pub timestamp: u64,
    pub flags: u32,
    // Padding to ensure cache line alignment
    _padding: [u8; 32],
}

impl CacheFriendlyStruct {
    pub fn new(id: u64, value: f64) -> Self {
        Self {
            id,
            value,
            timestamp: std::time::SystemTime::now()
                .duration_since(std::time::UNIX_EPOCH)
                .unwrap()
                .as_secs(),
            flags: 0,
            _padding: [0; 32],
        }
    }
}

// Cache-aligned vector
pub struct CacheAlignedVec<T> {
    data: Vec<T>,
    cache_line_size: usize,
}

impl<T> CacheAlignedVec<T> {
    pub fn new() -> Self {
        Self {
            data: Vec::new(),
            cache_line_size: 64, // Typical cache line size
        }
    }
    
    pub fn with_capacity(capacity: usize) -> Self {
        Self {
            data: Vec::with_capacity(capacity),
            cache_line_size: 64,
        }
    }
    
    pub fn push(&mut self, item: T) {
        self.data.push(item);
    }
    
    pub fn iter(&self) -> std::slice::Iter<T> {
        self.data.iter()
    }
    
    pub fn iter_mut(&mut self) -> std::slice::IterMut<T> {
        self.data.iter_mut()
    }
    
    pub fn len(&self) -> usize {
        self.data.len()
    }
    
    pub fn is_empty(&self) -> bool {
        self.data.is_empty()
    }
}

// LRU Cache
pub struct LRUCache<K, V> {
    capacity: usize,
    cache: HashMap<K, (V, usize)>, // (value, access_time)
    access_counter: usize,
}

impl<K, V> LRUCache<K, V>
where
    K: std::hash::Hash + Eq + Clone,
{
    pub fn new(capacity: usize) -> Self {
        Self {
            capacity,
            cache: HashMap::new(),
            access_counter: 0,
        }
    }
    
    pub fn get(&mut self, key: &K) -> Option<&V> {
        if let Some((value, _)) = self.cache.get_mut(key) {
            self.access_counter += 1;
            self.cache.get_mut(key).unwrap().1 = self.access_counter;
            Some(value)
        } else {
            None
        }
    }
    
    pub fn put(&mut self, key: K, value: V) {
        self.access_counter += 1;
        
        if self.cache.len() >= self.capacity && !self.cache.contains_key(&key) {
            // Remove least recently used item
            let lru_key = self.cache
                .iter()
                .min_by_key(|(_, (_, access_time))| access_time)
                .map(|(key, _)| key.clone())
                .unwrap();
            
            self.cache.remove(&lru_key);
        }
        
        self.cache.insert(key, (value, self.access_counter));
    }
    
    pub fn len(&self) -> usize {
        self.cache.len()
    }
    
    pub fn is_empty(&self) -> bool {
        self.cache.is_empty()
    }
    
    pub fn clear(&mut self) {
        self.cache.clear();
        self.access_counter = 0;
    }
}
```

## 🔄 CPU Optimization

### Parallel Processing

```rust
[cpu_optimization]
parallel_processing: true
vectorization: true
cpu_affinity: true

[cpu_implementation]
use rayon::prelude::*;
use std::sync::atomic::{AtomicUsize, Ordering};

// Parallel processing utilities
pub struct ParallelProcessor {
    thread_count: usize,
    chunk_size: usize,
}

impl ParallelProcessor {
    pub fn new() -> Self {
        Self {
            thread_count: num_cpus::get(),
            chunk_size: 1000,
        }
    }
    
    pub fn thread_count(mut self, count: usize) -> Self {
        self.thread_count = count;
        self
    }
    
    pub fn chunk_size(mut self, size: usize) -> Self {
        self.chunk_size = size;
        self
    }
    
    pub fn parallel_map<T, R, F>(&self, data: Vec<T>, f: F) -> Vec<R>
    where
        T: Send + Sync,
        R: Send,
        F: Fn(T) -> R + Send + Sync,
    {
        data.into_par_iter()
            .map(f)
            .collect()
    }
    
    pub fn parallel_filter<T, F>(&self, data: Vec<T>, predicate: F) -> Vec<T>
    where
        T: Send + Sync,
        F: Fn(&T) -> bool + Send + Sync,
    {
        data.into_par_iter()
            .filter(predicate)
            .collect()
    }
    
    pub fn parallel_reduce<T, F>(&self, data: Vec<T>, identity: T, op: F) -> T
    where
        T: Send + Sync + Clone,
        F: Fn(T, T) -> T + Send + Sync,
    {
        data.into_par_iter()
            .reduce(|| identity.clone(), op)
    }
    
    pub fn parallel_sort<T>(&self, mut data: Vec<T>) -> Vec<T>
    where
        T: Send + Sync + Ord,
    {
        data.par_sort();
        data
    }
}

// SIMD operations
pub struct SIMDProcessor;

impl SIMDProcessor {
    pub fn vector_add(a: &[f32], b: &[f32]) -> Vec<f32> {
        assert_eq!(a.len(), b.len());
        
        let mut result = Vec::with_capacity(a.len());
        
        // Use SIMD when possible
        if a.len() >= 4 {
            let chunks = a.len() / 4;
            
            for i in 0..chunks {
                let idx = i * 4;
                result.push(a[idx] + b[idx]);
                result.push(a[idx + 1] + b[idx + 1]);
                result.push(a[idx + 2] + b[idx + 2]);
                result.push(a[idx + 3] + b[idx + 3]);
            }
            
            // Handle remaining elements
            for i in chunks * 4..a.len() {
                result.push(a[i] + b[i]);
            }
        } else {
            // Fallback for small arrays
            for (a_val, b_val) in a.iter().zip(b.iter()) {
                result.push(a_val + b_val);
            }
        }
        
        result
    }
    
    pub fn vector_multiply(a: &[f32], b: &[f32]) -> Vec<f32> {
        assert_eq!(a.len(), b.len());
        
        let mut result = Vec::with_capacity(a.len());
        
        // Use SIMD when possible
        if a.len() >= 4 {
            let chunks = a.len() / 4;
            
            for i in 0..chunks {
                let idx = i * 4;
                result.push(a[idx] * b[idx]);
                result.push(a[idx + 1] * b[idx + 1]);
                result.push(a[idx + 2] * b[idx + 2]);
                result.push(a[idx + 3] * b[idx + 3]);
            }
            
            // Handle remaining elements
            for i in chunks * 4..a.len() {
                result.push(a[i] * b[i]);
            }
        } else {
            // Fallback for small arrays
            for (a_val, b_val) in a.iter().zip(b.iter()) {
                result.push(a_val * b_val);
            }
        }
        
        result
    }
    
    pub fn vector_sum(data: &[f32]) -> f32 {
        let mut sum = 0.0f32;
        
        // Use SIMD when possible
        if data.len() >= 4 {
            let chunks = data.len() / 4;
            
            for i in 0..chunks {
                let idx = i * 4;
                sum += data[idx] + data[idx + 1] + data[idx + 2] + data[idx + 3];
            }
            
            // Handle remaining elements
            for i in chunks * 4..data.len() {
                sum += data[i];
            }
        } else {
            // Fallback for small arrays
            for &val in data {
                sum += val;
            }
        }
        
        sum
    }
}

// CPU affinity
pub struct CPUAffinity;

impl CPUAffinity {
    pub fn set_thread_affinity(cpu_id: usize) -> Result<(), String> {
        #[cfg(target_os = "linux")]
        {
            use std::os::unix::thread::JoinHandleExt;
            
            let mut cpu_set = unsafe { std::mem::zeroed() };
            unsafe {
                libc::CPU_ZERO(&mut cpu_set);
                libc::CPU_SET(cpu_id, &mut cpu_set);
                
                let thread_id = libc::pthread_self();
                if libc::pthread_setaffinity_np(thread_id, std::mem::size_of_val(&cpu_set), &cpu_set) != 0 {
                    return Err("Failed to set CPU affinity".to_string());
                }
            }
        }
        
        #[cfg(not(target_os = "linux"))]
        {
            return Err("CPU affinity not supported on this platform".to_string());
        }
        
        Ok(())
    }
    
    pub fn get_optimal_cpu_count() -> usize {
        num_cpus::get()
    }
    
    pub fn get_physical_cpu_count() -> usize {
        num_cpus::get_physical()
    }
}
```

## 🎯 Best Practices

### 1. **Profiling and Measurement**
- Profile before optimizing
- Use appropriate benchmarking tools
- Measure in production-like environments
- Focus on bottlenecks identified by profiling

### 2. **Memory Optimization**
- Minimize allocations in hot paths
- Use appropriate data structures
- Implement memory pools for frequent allocations
- Consider cache-friendly data layouts

### 3. **CPU Optimization**
- Use parallel processing for CPU-bound tasks
- Leverage SIMD instructions when possible
- Optimize for cache locality
- Use appropriate thread counts

### 4. **Algorithm Optimization**
- Choose appropriate algorithms for data size
- Use specialized data structures
- Implement early termination
- Consider trade-offs between time and space

### 5. **TuskLang Integration**
- Use TuskLang configuration for performance parameters
- Implement performance monitoring with TuskLang
- Configure optimization levels through TuskLang
- Use TuskLang for performance testing

Performance optimization with TuskLang and Rust provides powerful tools for building high-performance applications with systematic measurement and optimization approaches. 