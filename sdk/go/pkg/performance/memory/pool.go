package memory

import (
	"encoding/binary"
	"fmt"
	"runtime"
	"sync"
	"time"
	"unsafe"
)

// Pool provides object pooling for memory optimization
type Pool struct {
	mu           sync.RWMutex
	pools        map[int]*ObjectPool
	stats        *PoolStats
	config       *PoolConfig
	cleanupTicker *time.Ticker
	stopCleanup  chan bool
}

// ObjectPool manages objects of a specific size
type ObjectPool struct {
	mu           sync.Mutex
	objects      []interface{}
	size         int
	maxSize      int
	created      int64
	reused       int64
	allocated    int64
	freed        int64
	lastAccess   time.Time
}

// PoolStats tracks pool performance
type PoolStats struct {
	TotalPools     int64
	TotalObjects   int64
	TotalCreated   int64
	TotalReused    int64
	TotalAllocated int64
	TotalFreed     int64
	MemoryUsage    uint64
	HitRate        float64
	Efficiency     float64
}

// PoolConfig defines pool configuration
type PoolConfig struct {
	MaxPoolSize    int
	CleanupInterval time.Duration
	EnableGC       bool
	PreAllocate    bool
	AutoScale      bool
}

// NewPool creates a new memory pool instance
func NewPool(config *PoolConfig) *Pool {
	pool := &Pool{
		pools:        make(map[int]*ObjectPool),
		stats:        &PoolStats{},
		config:       config,
		stopCleanup:  make(chan bool),
	}
	
	// Start cleanup goroutine
	go pool.startCleanup()
	
	return pool
}

// Get retrieves an object from the pool
func (p *Pool) Get(size int) interface{} {
	p.mu.RLock()
	objectPool, exists := p.pools[size]
	p.mu.RUnlock()
	
	if !exists {
		// Create new pool for this size
		p.mu.Lock()
		objectPool = p.createPool(size)
		p.pools[size] = objectPool
		p.stats.TotalPools++
		p.mu.Unlock()
	}
	
	return objectPool.Get()
}

// Put returns an object to the pool
func (p *Pool) Put(size int, obj interface{}) {
	p.mu.RLock()
	objectPool, exists := p.pools[size]
	p.mu.RUnlock()
	
	if !exists {
		// Create new pool for this size
		p.mu.Lock()
		objectPool = p.createPool(size)
		p.pools[size] = objectPool
		p.stats.TotalPools++
		p.mu.Unlock()
	}
	
	objectPool.Put(obj)
}

// GetBytes retrieves a byte slice from the pool
func (p *Pool) GetBytes(size int) []byte {
	obj := p.Get(size)
	if obj == nil {
		return make([]byte, size)
	}
	
	// Type assertion for byte slice
	if bytes, ok := obj.([]byte); ok {
		return bytes[:size]
	}
	
	// Fallback to new allocation
	return make([]byte, size)
}

// PutBytes returns a byte slice to the pool
func (p *Pool) PutBytes(bytes []byte) {
	p.Put(cap(bytes), bytes)
}

// GetString retrieves a string buffer from the pool
func (p *Pool) GetString(size int) *StringBuffer {
	obj := p.Get(size)
	if obj == nil {
		return NewStringBuffer(size)
	}
	
	// Type assertion for string buffer
	if buffer, ok := obj.(*StringBuffer); ok {
		buffer.Reset()
		return buffer
	}
	
	// Fallback to new allocation
	return NewStringBuffer(size)
}

// PutString returns a string buffer to the pool
func (p *Pool) PutString(buffer *StringBuffer) {
	p.Put(buffer.Capacity(), buffer)
}

// createPool creates a new object pool for the specified size
func (p *Pool) createPool(size int) *ObjectPool {
	pool := &ObjectPool{
		objects:    make([]interface{}, 0, p.config.MaxPoolSize),
		size:       size,
		maxSize:    p.config.MaxPoolSize,
		lastAccess: time.Now(),
	}
	
	// Pre-allocate objects if enabled
	if p.config.PreAllocate {
		for i := 0; i < p.config.MaxPoolSize/2; i++ {
			pool.objects = append(pool.objects, p.createObject(size))
			pool.created++
		}
	}
	
	return pool
}

// createObject creates a new object of the specified size
func (p *Pool) createObject(size int) interface{} {
	switch size {
	case 8:
		return make([]byte, 8)
	case 16:
		return make([]byte, 16)
	case 32:
		return make([]byte, 32)
	case 64:
		return make([]byte, 64)
	case 128:
		return make([]byte, 128)
	case 256:
		return make([]byte, 256)
	case 512:
		return make([]byte, 512)
	case 1024:
		return make([]byte, 1024)
	case 2048:
		return make([]byte, 2048)
	case 4096:
		return make([]byte, 4096)
	default:
		return make([]byte, size)
	}
}

// Get retrieves an object from the pool
func (op *ObjectPool) Get() interface{} {
	op.mu.Lock()
	defer op.mu.Unlock()
	
	op.lastAccess = time.Now()
	op.allocated++
	
	if len(op.objects) > 0 {
		// Reuse existing object
		obj := op.objects[len(op.objects)-1]
		op.objects = op.objects[:len(op.objects)-1]
		op.reused++
		return obj
	}
	
	// Create new object
	obj := make([]byte, op.size)
	op.created++
	return obj
}

// Put returns an object to the pool
func (op *ObjectPool) Put(obj interface{}) {
	op.mu.Lock()
	defer op.mu.Unlock()
	
	op.lastAccess = time.Now()
	op.freed++
	
	// Reset object if it's a byte slice
	if bytes, ok := obj.([]byte); ok {
		// Clear the slice
		for i := range bytes {
			bytes[i] = 0
		}
	}
	
	// Add to pool if not full
	if len(op.objects) < op.maxSize {
		op.objects = append(op.objects, obj)
	}
}

// GetStats returns pool statistics
func (p *Pool) GetStats() *PoolStats {
	p.mu.RLock()
	defer p.mu.RUnlock()
	
	stats := *p.stats
	
	// Calculate totals from all pools
	for _, pool := range p.pools {
		pool.mu.Lock()
		stats.TotalObjects += int64(len(pool.objects))
		stats.TotalCreated += pool.created
		stats.TotalReused += pool.reused
		stats.TotalAllocated += pool.allocated
		stats.TotalFreed += pool.freed
		pool.mu.Unlock()
	}
	
	// Calculate hit rate
	total := stats.TotalAllocated
	if total > 0 {
		stats.HitRate = float64(stats.TotalReused) / float64(total)
	}
	
	// Calculate efficiency
	totalObjects := stats.TotalCreated + stats.TotalReused
	if totalObjects > 0 {
		stats.Efficiency = float64(stats.TotalReused) / float64(totalObjects)
	}
	
	// Calculate memory usage
	stats.MemoryUsage = p.calculateMemoryUsage()
	
	return &stats
}

// calculateMemoryUsage calculates total memory usage
func (p *Pool) calculateMemoryUsage() uint64 {
	var usage uint64
	
	for size, pool := range p.pools {
		pool.mu.Lock()
		objectCount := len(pool.objects)
		pool.mu.Unlock()
		
		usage += uint64(objectCount * size)
	}
	
	return usage
}

// startCleanup starts the cleanup goroutine
func (p *Pool) startCleanup() {
	p.cleanupTicker = time.NewTicker(p.config.CleanupInterval)
	
	for {
		select {
		case <-p.cleanupTicker.C:
			p.cleanup()
		case <-p.stopCleanup:
			p.cleanupTicker.Stop()
			return
		}
	}
}

// cleanup removes unused objects from pools
func (p *Pool) cleanup() {
	p.mu.Lock()
	defer p.mu.Unlock()
	
	now := time.Now()
	
	for size, pool := range p.pools {
		pool.mu.Lock()
		
		// Remove objects that haven't been accessed recently
		if now.Sub(pool.lastAccess) > 10*time.Minute {
			// Keep only 25% of objects
			keepCount := len(pool.objects) / 4
			if keepCount < len(pool.objects) {
				pool.objects = pool.objects[:keepCount]
			}
		}
		
		pool.mu.Unlock()
	}
	
	// Trigger garbage collection if enabled
	if p.config.EnableGC {
		runtime.GC()
	}
}

// Stop stops the pool and cleanup goroutine
func (p *Pool) Stop() {
	close(p.stopCleanup)
}

// StringBuffer provides a reusable string buffer
type StringBuffer struct {
	buffer []byte
	length int
}

// NewStringBuffer creates a new string buffer
func NewStringBuffer(capacity int) *StringBuffer {
	return &StringBuffer{
		buffer: make([]byte, 0, capacity),
		length: 0,
	}
}

// Write appends data to the buffer
func (sb *StringBuffer) Write(data []byte) (int, error) {
	sb.buffer = append(sb.buffer, data...)
	sb.length = len(sb.buffer)
	return len(data), nil
}

// WriteString appends a string to the buffer
func (sb *StringBuffer) WriteString(s string) (int, error) {
	sb.buffer = append(sb.buffer, s...)
	sb.length = len(sb.buffer)
	return len(s), nil
}

// String returns the buffer as a string
func (sb *StringBuffer) String() string {
	return string(sb.buffer)
}

// Bytes returns the buffer as a byte slice
func (sb *StringBuffer) Bytes() []byte {
	return sb.buffer
}

// Reset clears the buffer
func (sb *StringBuffer) Reset() {
	sb.buffer = sb.buffer[:0]
	sb.length = 0
}

// Capacity returns the buffer capacity
func (sb *StringBuffer) Capacity() int {
	return cap(sb.buffer)
}

// Length returns the current buffer length
func (sb *StringBuffer) Length() int {
	return sb.length
}

// Grow ensures the buffer has at least n bytes of capacity
func (sb *StringBuffer) Grow(n int) {
	if cap(sb.buffer) < n {
		newBuffer := make([]byte, len(sb.buffer), n)
		copy(newBuffer, sb.buffer)
		sb.buffer = newBuffer
	}
}

// Truncate truncates the buffer to n bytes
func (sb *StringBuffer) Truncate(n int) {
	if n < len(sb.buffer) {
		sb.buffer = sb.buffer[:n]
		sb.length = n
	}
}

// MemoryOptimizer provides advanced memory optimization
type MemoryOptimizer struct {
	pool    *Pool
	stats   *OptimizerStats
	enabled bool
}

// OptimizerStats tracks optimization performance
type OptimizerStats struct {
	Optimizations int64
	MemorySaved   uint64
	GCTriggered   int64
	Compactions   int64
}

// NewMemoryOptimizer creates a new memory optimizer
func NewMemoryOptimizer(pool *Pool) *MemoryOptimizer {
	return &MemoryOptimizer{
		pool:    pool,
		stats:   &OptimizerStats{},
		enabled: true,
	}
}

// Optimize performs memory optimization
func (mo *MemoryOptimizer) Optimize() {
	if !mo.enabled {
		return
	}
	
	mo.stats.Optimizations++
	
	// Trigger garbage collection
	runtime.GC()
	mo.stats.GCTriggered++
	
	// Compact memory
	mo.compactMemory()
	
	// Update statistics
	mo.updateStats()
}

// compactMemory compacts memory usage
func (mo *MemoryOptimizer) compactMemory() {
	// This would implement memory compaction
	// For now, just trigger GC
	runtime.GC()
	mo.stats.Compactions++
}

// updateStats updates optimization statistics
func (mo *MemoryOptimizer) updateStats() {
	// Calculate memory saved
	var m runtime.MemStats
	runtime.ReadMemStats(&m)
	
	// This is a simplified calculation
	mo.stats.MemorySaved = m.TotalAlloc - m.HeapAlloc
}

// GetStats returns optimizer statistics
func (mo *MemoryOptimizer) GetStats() *OptimizerStats {
	return mo.stats
}

// Enable enables memory optimization
func (mo *MemoryOptimizer) Enable() {
	mo.enabled = true
}

// Disable disables memory optimization
func (mo *MemoryOptimizer) Disable() {
	mo.enabled = false
} 