package performance

import (
	"context"
	"fmt"
	"runtime"
	"sync"
	"time"

	"github.com/cyber-boost/tusktsk/pkg/performance/cache"
	"github.com/cyber-boost/tusktsk/pkg/performance/jit"
	"github.com/cyber-boost/tusktsk/pkg/performance/memory"
)

// Framework provides unified performance optimization
type Framework struct {
	mu           sync.RWMutex
	jitCompiler  *jit.JITCompiler
	cacheManager *cache.CacheManager
	memoryPool   *memory.Pool
	optimizer    *memory.MemoryOptimizer
	profiler     *jit.Profiler
	config       *FrameworkConfig
	stats        *FrameworkStats
	ctx          context.Context
	cancel       context.CancelFunc
	enabled      bool
}

// FrameworkConfig defines framework configuration
type FrameworkConfig struct {
	JITEnabled       bool
	CacheEnabled     bool
	MemoryEnabled    bool
	ProfilingEnabled bool
	AutoOptimize     bool
	OptimizationInterval time.Duration
	MaxConcurrency   int
	PerformanceTarget float64
}

// FrameworkStats tracks overall framework performance
type FrameworkStats struct {
	TotalRequests    int64
	JITCompilations  int64
	CacheHits        int64
	MemoryOptimizations int64
	PerformanceGain  float64
	MemoryUsage      uint64
	CPUUsage         float64
	Uptime           time.Duration
	StartTime        time.Time
}

// NewFramework creates a new performance framework instance
func NewFramework(config *FrameworkConfig) *Framework {
	ctx, cancel := context.WithCancel(context.Background())
	
	framework := &Framework{
		config:  config,
		stats:   &FrameworkStats{StartTime: time.Now()},
		ctx:     ctx,
		cancel:  cancel,
		enabled: true,
	}
	
	// Initialize components
	framework.initializeComponents()
	
	// Start optimization goroutine
	if config.AutoOptimize {
		go framework.optimizationWorker()
	}
	
	return framework
}

// initializeComponents initializes all performance components
func (f *Framework) initializeComponents() {
	// Initialize JIT compiler
	if f.config.JITEnabled {
		f.jitCompiler = jit.NewJITCompiler()
	}
	
	// Initialize cache manager
	if f.config.CacheEnabled {
		cacheConfig := &cache.ManagerConfig{
			L1Size:           1000000, // 1MB
			L2Size:           10000000, // 10MB
			L3Size:           100000000, // 100MB
			L1TTL:            5 * time.Minute,
			L2TTL:            30 * time.Minute,
			L3TTL:            2 * time.Hour,
			WarmupWorkers:    4,
			EvictionWorkers:  2,
			PredictiveWarmup: true,
			AutoScaling:      true,
		}
		f.cacheManager = cache.NewCacheManager(cacheConfig)
	}
	
	// Initialize memory pool
	if f.config.MemoryEnabled {
		poolConfig := &memory.PoolConfig{
			MaxPoolSize:      1000,
			CleanupInterval:  5 * time.Minute,
			EnableGC:         true,
			PreAllocate:      true,
			AutoScale:        true,
		}
		f.memoryPool = memory.NewPool(poolConfig)
		f.optimizer = memory.NewMemoryOptimizer(f.memoryPool)
	}
	
	// Initialize profiler
	if f.config.ProfilingEnabled {
		f.profiler = jit.NewProfiler()
	}
}

// Execute runs code with full performance optimization
func (f *Framework) Execute(signature string, fn func() interface{}) interface{} {
	if !f.enabled {
		return fn()
	}
	
	f.stats.TotalRequests++
	start := time.Now()
	
	// Use JIT compilation if enabled
	if f.jitCompiler != nil {
		result := f.jitCompiler.Execute(signature, fn)
		f.stats.JITCompilations++
		f.updatePerformanceMetrics(time.Since(start))
		return result
	}
	
	// Fallback to direct execution
	result := fn()
	f.updatePerformanceMetrics(time.Since(start))
	return result
}

// Get retrieves data with caching optimization
func (f *Framework) Get(key string) (interface{}, bool) {
	if !f.enabled || f.cacheManager == nil {
		return nil, false
	}
	
	value, found := f.cacheManager.Get(key)
	if found {
		f.stats.CacheHits++
	}
	return value, found
}

// Set stores data with caching optimization
func (f *Framework) Set(key string, value interface{}, ttl time.Duration) error {
	if !f.enabled || f.cacheManager == nil {
		return fmt.Errorf("cache not enabled")
	}
	
	return f.cacheManager.Set(key, value, ttl)
}

// GetBytes retrieves optimized byte slice
func (f *Framework) GetBytes(size int) []byte {
	if !f.enabled || f.memoryPool == nil {
		return make([]byte, size)
	}
	
	return f.memoryPool.GetBytes(size)
}

// PutBytes returns byte slice to pool
func (f *Framework) PutBytes(bytes []byte) {
	if !f.enabled || f.memoryPool == nil {
		return
	}
	
	f.memoryPool.PutBytes(bytes)
}

// GetString retrieves optimized string buffer
func (f *Framework) GetString(size int) *memory.StringBuffer {
	if !f.enabled || f.memoryPool == nil {
		return memory.NewStringBuffer(size)
	}
	
	return f.memoryPool.GetString(size)
}

// PutString returns string buffer to pool
func (f *Framework) PutString(buffer *memory.StringBuffer) {
	if !f.enabled || f.memoryPool == nil {
		return
	}
	
	f.memoryPool.PutString(buffer)
}

// ProfileFunction profiles a function execution
func (f *Framework) ProfileFunction(id, name string, fn func() interface{}) interface{} {
	if !f.enabled || f.profiler == nil {
		return fn()
	}
	
	return f.profiler.ProfileFunction(id, name, fn)
}

// StartProfile begins profiling session
func (f *Framework) StartProfile(id, name string) *jit.ProfileSession {
	if !f.enabled || f.profiler == nil {
		return &jit.ProfileSession{Enabled: false}
	}
	
	return f.profiler.StartProfile(id, name)
}

// Optimize performs comprehensive optimization
func (f *Framework) Optimize() {
	if !f.enabled {
		return
	}
	
	f.stats.MemoryOptimizations++
	
	// Memory optimization
	if f.optimizer != nil {
		f.optimizer.Optimize()
	}
	
	// Cache optimization
	if f.cacheManager != nil {
		// Trigger cache cleanup
		// This would be handled by the cache manager internally
	}
	
	// JIT optimization
	if f.jitCompiler != nil {
		// JIT optimizations are handled automatically
	}
	
	// Update statistics
	f.updateStats()
}

// optimizationWorker runs continuous optimization
func (f *Framework) optimizationWorker() {
	ticker := time.NewTicker(f.config.OptimizationInterval)
	defer ticker.Stop()
	
	for {
		select {
		case <-ticker.C:
			f.Optimize()
		case <-f.ctx.Done():
			return
		}
	}
}

// updatePerformanceMetrics updates performance tracking
func (f *Framework) updatePerformanceMetrics(latency time.Duration) {
	// Calculate performance gain
	baselineLatency := 100 * time.Microsecond
	if latency < baselineLatency {
		gain := float64(baselineLatency) / float64(latency)
		f.stats.PerformanceGain = (f.stats.PerformanceGain + gain) / 2
	}
}

// updateStats updates framework statistics
func (f *Framework) updateStats() {
	f.stats.Uptime = time.Since(f.stats.StartTime)
	
	// Update memory usage
	var m runtime.MemStats
	runtime.ReadMemStats(&m)
	f.stats.MemoryUsage = m.Alloc
	
	// Update CPU usage (simplified)
	f.stats.CPUUsage = float64(runtime.NumGoroutine()) / float64(runtime.NumCPU())
}

// GetStats returns comprehensive framework statistics
func (f *Framework) GetStats() *FrameworkStats {
	f.mu.RLock()
	defer f.mu.RUnlock()
	
	stats := *f.stats
	stats.Uptime = time.Since(stats.StartTime)
	
	return &stats
}

// GetDetailedStats returns detailed statistics from all components
func (f *Framework) GetDetailedStats() map[string]interface{} {
	stats := map[string]interface{}{
		"framework": f.GetStats(),
	}
	
	if f.jitCompiler != nil {
		stats["jit"] = f.jitCompiler.GetStats()
		stats["hot_paths"] = f.jitCompiler.GetHotPaths()
	}
	
	if f.cacheManager != nil {
		stats["cache"] = f.cacheManager.GetDetailedStats()
	}
	
	if f.memoryPool != nil {
		stats["memory"] = f.memoryPool.GetStats()
	}
	
	if f.optimizer != nil {
		stats["optimizer"] = f.optimizer.GetStats()
	}
	
	if f.profiler != nil {
		stats["profiler"] = f.profiler.GetStats()
		stats["profiles"] = f.profiler.GetAllProfiles()
	}
	
	return stats
}

// Enable enables the performance framework
func (f *Framework) Enable() {
	f.mu.Lock()
	defer f.mu.Unlock()
	f.enabled = true
}

// Disable disables the performance framework
func (f *Framework) Disable() {
	f.mu.Lock()
	defer f.mu.Unlock()
	f.enabled = false
}

// Stop stops the framework and all components
func (f *Framework) Stop() {
	f.cancel()
	
	if f.cacheManager != nil {
		f.cacheManager.Stop()
	}
	
	if f.memoryPool != nil {
		f.memoryPool.Stop()
	}
	
	if f.profiler != nil {
		f.profiler.Disable()
	}
}

// Benchmark runs performance benchmarks
func (f *Framework) Benchmark(iterations int) *BenchmarkResults {
	results := &BenchmarkResults{
		Iterations: iterations,
		StartTime:  time.Now(),
	}
	
	// Benchmark JIT compilation
	if f.jitCompiler != nil {
		results.JITResults = f.benchmarkJIT(iterations)
	}
	
	// Benchmark caching
	if f.cacheManager != nil {
		results.CacheResults = f.benchmarkCache(iterations)
	}
	
	// Benchmark memory operations
	if f.memoryPool != nil {
		results.MemoryResults = f.benchmarkMemory(iterations)
	}
	
	results.EndTime = time.Now()
	results.Duration = results.EndTime.Sub(results.StartTime)
	
	return results
}

// BenchmarkResults contains benchmark results
type BenchmarkResults struct {
	Iterations   int
	StartTime    time.Time
	EndTime      time.Time
	Duration     time.Duration
	JITResults   *JITBenchmarkResults
	CacheResults *CacheBenchmarkResults
	MemoryResults *MemoryBenchmarkResults
}

// JITBenchmarkResults contains JIT benchmark results
type JITBenchmarkResults struct {
	CompilationTime time.Duration
	ExecutionTime   time.Duration
	Optimizations   int64
	PerformanceGain float64
}

// CacheBenchmarkResults contains cache benchmark results
type CacheBenchmarkResults struct {
	GetTime       time.Duration
	SetTime       time.Duration
	HitRate       float64
	MemoryUsage   uint64
}

// MemoryBenchmarkResults contains memory benchmark results
type MemoryBenchmarkResults struct {
	AllocationTime time.Duration
	DeallocationTime time.Duration
	PoolHitRate    float64
	MemorySaved    uint64
}

// benchmarkJIT benchmarks JIT compilation
func (f *Framework) benchmarkJIT(iterations int) *JITBenchmarkResults {
	results := &JITBenchmarkResults{}
	
	// Benchmark compilation
	start := time.Now()
	for i := 0; i < iterations; i++ {
		signature := fmt.Sprintf("benchmark_function_%d", i)
		f.jitCompiler.Execute(signature, func() interface{} {
			return i * i
		})
	}
	results.CompilationTime = time.Since(start)
	
	// Get JIT stats
	stats := f.jitCompiler.GetStats()
	results.Optimizations = stats.TotalCompilations
	results.PerformanceGain = stats.PerformanceGain
	
	return results
}

// benchmarkCache benchmarks cache operations
func (f *Framework) benchmarkCache(iterations int) *CacheBenchmarkResults {
	results := &CacheBenchmarkResults{}
	
	// Benchmark gets
	start := time.Now()
	for i := 0; i < iterations; i++ {
		key := fmt.Sprintf("benchmark_key_%d", i)
		f.cacheManager.Get(key)
	}
	results.GetTime = time.Since(start)
	
	// Benchmark sets
	start = time.Now()
	for i := 0; i < iterations; i++ {
		key := fmt.Sprintf("benchmark_key_%d", i)
		value := fmt.Sprintf("benchmark_value_%d", i)
		f.cacheManager.Set(key, value, time.Minute)
	}
	results.SetTime = time.Since(start)
	
	// Get cache stats
	stats := f.cacheManager.GetStats()
	results.HitRate = stats.HitRate
	results.MemoryUsage = f.cacheManager.GetMemoryUsage()
	
	return results
}

// benchmarkMemory benchmarks memory operations
func (f *Framework) benchmarkMemory(iterations int) *MemoryBenchmarkResults {
	results := &MemoryBenchmarkResults{}
	
	// Benchmark allocations
	start := time.Now()
	var buffers [][]byte
	for i := 0; i < iterations; i++ {
		buffer := f.memoryPool.GetBytes(1024)
		buffers = append(buffers, buffer)
	}
	results.AllocationTime = time.Since(start)
	
	// Benchmark deallocations
	start = time.Now()
	for _, buffer := range buffers {
		f.memoryPool.PutBytes(buffer)
	}
	results.DeallocationTime = time.Since(start)
	
	// Get memory stats
	stats := f.memoryPool.GetStats()
	results.PoolHitRate = stats.HitRate
	results.MemorySaved = stats.MemoryUsage
	
	return results
}

// GetPerformanceReport generates a comprehensive performance report
func (f *Framework) GetPerformanceReport() *PerformanceReport {
	report := &PerformanceReport{
		Timestamp: time.Now(),
		Framework: f.GetStats(),
		Components: f.GetDetailedStats(),
		Benchmarks: f.Benchmark(1000),
	}
	
	// Calculate overall performance score
	report.PerformanceScore = f.calculatePerformanceScore(report)
	
	return report
}

// PerformanceReport contains comprehensive performance information
type PerformanceReport struct {
	Timestamp        time.Time
	Framework        *FrameworkStats
	Components       map[string]interface{}
	Benchmarks       *BenchmarkResults
	PerformanceScore float64
}

// calculatePerformanceScore calculates overall performance score
func (f *Framework) calculatePerformanceScore(report *PerformanceReport) float64 {
	score := 0.0
	
	// Framework performance (40%)
	if report.Framework.PerformanceGain > 0 {
		score += report.Framework.PerformanceGain * 0.4
	}
	
	// Cache performance (30%)
	if cacheStats, ok := report.Components["cache"].(map[string]interface{}); ok {
		if manager, ok := cacheStats["manager"].(*cache.ManagerStats); ok {
			score += manager.HitRate * 0.3
		}
	}
	
	// Memory efficiency (20%)
	if memoryStats, ok := report.Components["memory"].(*memory.PoolStats); ok {
		score += memoryStats.Efficiency * 0.2
	}
	
	// JIT optimization (10%)
	if jitStats, ok := report.Components["jit"].(*jit.CompilationStats); ok {
		if jitStats.PerformanceGain > 0 {
			score += jitStats.PerformanceGain * 0.1
		}
	}
	
	return score
} 