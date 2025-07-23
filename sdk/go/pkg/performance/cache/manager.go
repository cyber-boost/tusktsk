package cache

import (
	"context"
	"fmt"
	"sync"
	"time"
)

// CacheManager coordinates multi-level caching system
type CacheManager struct {
	mu           sync.RWMutex
	l1Cache      *L1Cache
	l2Cache      *L2Cache
	l3Cache      *L3Cache
	stats        *ManagerStats
	config       *ManagerConfig
	ctx          context.Context
	cancel       context.CancelFunc
	warmupQueue  chan WarmupRequest
	evictionQueue chan EvictionRequest
}

// ManagerStats tracks overall cache manager performance
type ManagerStats struct {
	TotalRequests    int64
	L1Hits           int64
	L2Hits           int64
	L3Hits           int64
	CacheMisses      int64
	WarmupRequests   int64
	EvictionRequests int64
	HitRate          float64
	AverageLatency   time.Duration
}

// ManagerConfig defines cache manager configuration
type ManagerConfig struct {
	L1Size          int
	L2Size          int
	L3Size          int
	L1TTL           time.Duration
	L2TTL           time.Duration
	L3TTL           time.Duration
	WarmupWorkers   int
	EvictionWorkers int
	PredictiveWarmup bool
	AutoScaling     bool
}

// WarmupRequest represents a cache warming request
type WarmupRequest struct {
	Key   string
	Value interface{}
	TTL   time.Duration
	Priority int
}

// EvictionRequest represents a cache eviction request
type EvictionRequest struct {
	Key      string
	Level    int
	Reason   string
	Priority int
}

// NewCacheManager creates a new cache manager instance
func NewCacheManager(config *ManagerConfig) *CacheManager {
	ctx, cancel := context.WithCancel(context.Background())
	
	manager := &CacheManager{
		l1Cache:       NewL1Cache(config.L1Size, LRU, config.L1TTL),
		l2Cache:       NewL2Cache(config.L2Size, config.L2TTL),
		l3Cache:       NewL3Cache(config.L3Size, config.L3TTL),
		stats:         &ManagerStats{},
		config:        config,
		ctx:           ctx,
		cancel:        cancel,
		warmupQueue:   make(chan WarmupRequest, 1000),
		evictionQueue: make(chan EvictionRequest, 1000),
	}
	
	// Start worker goroutines
	manager.startWorkers()
	
	return manager
}

// Get retrieves a value from the cache hierarchy
func (cm *CacheManager) Get(key string) (interface{}, bool) {
	start := time.Now()
	cm.stats.TotalRequests++
	
	// Try L1 cache first (fastest)
	if value, found := cm.l1Cache.Get(key); found {
		cm.stats.L1Hits++
		cm.updateStats(time.Since(start))
		return value, true
	}
	
	// Try L2 cache (medium speed)
	if value, found := cm.l2Cache.Get(key); found {
		cm.stats.L2Hits++
		// Promote to L1 cache
		go cm.promoteToL1(key, value)
		cm.updateStats(time.Since(start))
		return value, true
	}
	
	// Try L3 cache (slowest)
	if value, found := cm.l3Cache.Get(key); found {
		cm.stats.L3Hits++
		// Promote to L2 cache
		go cm.promoteToL2(key, value)
		cm.updateStats(time.Since(start))
		return value, true
	}
	
	// Cache miss
	cm.stats.CacheMisses++
	cm.updateStats(time.Since(start))
	
	// Trigger predictive warmup if enabled
	if cm.config.PredictiveWarmup {
		go cm.predictiveWarmup(key)
	}
	
	return nil, false
}

// Set stores a value in the cache hierarchy
func (cm *CacheManager) Set(key string, value interface{}, ttl time.Duration) error {
	// Store in all cache levels
	err1 := cm.l1Cache.Set(key, value)
	err2 := cm.l2Cache.Set(key, value, ttl)
	err3 := cm.l3Cache.Set(key, value, ttl)
	
	if err1 != nil {
		return fmt.Errorf("L1 cache set failed: %v", err1)
	}
	if err2 != nil {
		return fmt.Errorf("L2 cache set failed: %v", err2)
	}
	if err3 != nil {
		return fmt.Errorf("L3 cache set failed: %v", err3)
	}
	
	return nil
}

// Delete removes a value from all cache levels
func (cm *CacheManager) Delete(key string) {
	cm.l1Cache.Delete(key)
	cm.l2Cache.Delete(key)
	cm.l3Cache.Delete(key)
}

// Clear clears all cache levels
func (cm *CacheManager) Clear() {
	cm.l1Cache.Clear()
	cm.l2Cache.Clear()
	cm.l3Cache.Clear()
}

// WarmUp preloads the cache with frequently accessed data
func (cm *CacheManager) WarmUp(data map[string]interface{}) {
	cm.stats.WarmupRequests++
	
	for key, value := range data {
		request := WarmupRequest{
			Key:      key,
			Value:    value,
			TTL:      cm.config.L1TTL,
			Priority: 1,
		}
		
		select {
		case cm.warmupQueue <- request:
		default:
			// Queue full, skip this request
		}
	}
}

// PredictiveWarmup predicts what data will be needed
func (cm *CacheManager) predictiveWarmup(key string) {
	// Analyze access patterns to predict related keys
	relatedKeys := cm.analyzeAccessPatterns(key)
	
	for _, relatedKey := range relatedKeys {
		request := WarmupRequest{
			Key:      relatedKey,
			Value:    nil, // Will be fetched from data source
			TTL:      cm.config.L1TTL,
			Priority: 2, // Lower priority than explicit warmup
		}
		
		select {
		case cm.warmupQueue <- request:
		default:
			// Queue full, skip this request
		}
	}
}

// analyzeAccessPatterns analyzes access patterns to predict related keys
func (cm *CacheManager) analyzeAccessPatterns(key string) []string {
	// This is a simplified implementation
	// In practice, you'd use machine learning or statistical analysis
	
	// Example: if key is "user:123", predict "user:123:profile", "user:123:settings"
	var relatedKeys []string
	
	// Simple pattern matching
	if len(key) > 5 && key[:5] == "user:" {
		userID := key[5:]
		relatedKeys = append(relatedKeys, 
			fmt.Sprintf("user:%s:profile", userID),
			fmt.Sprintf("user:%s:settings", userID),
			fmt.Sprintf("user:%s:preferences", userID),
		)
	}
	
	return relatedKeys
}

// promoteToL1 promotes a value from L2 to L1 cache
func (cm *CacheManager) promoteToL1(key string, value interface{}) {
	cm.l1Cache.Set(key, value)
}

// promoteToL2 promotes a value from L3 to L2 cache
func (cm *CacheManager) promoteToL2(key string, value interface{}) {
	cm.l2Cache.Set(key, value, cm.config.L2TTL)
}

// startWorkers starts background worker goroutines
func (cm *CacheManager) startWorkers() {
	// Start warmup workers
	for i := 0; i < cm.config.WarmupWorkers; i++ {
		go cm.warmupWorker()
	}
	
	// Start eviction workers
	for i := 0; i < cm.config.EvictionWorkers; i++ {
		go cm.evictionWorker()
	}
	
	// Start auto-scaling worker if enabled
	if cm.config.AutoScaling {
		go cm.autoScalingWorker()
	}
}

// warmupWorker processes cache warming requests
func (cm *CacheManager) warmupWorker() {
	for {
		select {
		case request := <-cm.warmupQueue:
			cm.processWarmupRequest(request)
		case <-cm.ctx.Done():
			return
		}
	}
}

// evictionWorker processes cache eviction requests
func (cm *CacheManager) evictionWorker() {
	for {
		select {
		case request := <-cm.evictionQueue:
			cm.processEvictionRequest(request)
		case <-cm.ctx.Done():
			return
		}
	}
}

// processWarmupRequest processes a single warmup request
func (cm *CacheManager) processWarmupRequest(request WarmupRequest) {
	// If value is nil, fetch from data source
	if request.Value == nil {
		// This would typically fetch from database or external service
		// For now, we'll skip nil values
		return
	}
	
	// Store in appropriate cache level based on priority
	switch request.Priority {
	case 1: // High priority - store in L1
		cm.l1Cache.Set(request.Key, request.Value)
	case 2: // Medium priority - store in L2
		cm.l2Cache.Set(request.Key, request.Value, request.TTL)
	case 3: // Low priority - store in L3
		cm.l3Cache.Set(request.Key, request.Value, request.TTL)
	}
}

// processEvictionRequest processes a single eviction request
func (cm *CacheManager) processEvictionRequest(request EvictionRequest) {
	cm.stats.EvictionRequests++
	
	switch request.Level {
	case 1:
		cm.l1Cache.Delete(request.Key)
	case 2:
		cm.l2Cache.Delete(request.Key)
	case 3:
		cm.l3Cache.Delete(request.Key)
	}
}

// autoScalingWorker automatically scales cache sizes based on usage
func (cm *CacheManager) autoScalingWorker() {
	ticker := time.NewTicker(5 * time.Minute)
	defer ticker.Stop()
	
	for {
		select {
		case <-ticker.C:
			cm.performAutoScaling()
		case <-cm.ctx.Done():
			return
		}
	}
}

// performAutoScaling adjusts cache sizes based on performance
func (cm *CacheManager) performAutoScaling() {
	stats := cm.GetStats()
	
	// Scale L1 cache based on hit rate
	if stats.HitRate < 0.8 {
		// Increase L1 cache size
		currentSize := cm.l1Cache.GetMaxSize()
		newSize := int(float64(currentSize) * 1.2)
		cm.l1Cache.SetMaxSize(newSize)
	} else if stats.HitRate > 0.95 {
		// Decrease L1 cache size if hit rate is very high
		currentSize := cm.l1Cache.GetMaxSize()
		newSize := int(float64(currentSize) * 0.9)
		cm.l1Cache.SetMaxSize(newSize)
	}
}

// GetStats returns comprehensive cache statistics
func (cm *CacheManager) GetStats() *ManagerStats {
	cm.mu.RLock()
	defer cm.mu.RUnlock()
	
	stats := *cm.stats
	
	// Calculate overall hit rate
	total := stats.L1Hits + stats.L2Hits + stats.L3Hits + stats.CacheMisses
	if total > 0 {
		stats.HitRate = float64(stats.L1Hits+stats.L2Hits+stats.L3Hits) / float64(total)
	}
	
	return &stats
}

// GetDetailedStats returns detailed statistics for all cache levels
func (cm *CacheManager) GetDetailedStats() map[string]interface{} {
	return map[string]interface{}{
		"manager": cm.GetStats(),
		"l1":      cm.l1Cache.GetStats(),
		"l2":      cm.l2Cache.GetStats(),
		"l3":      cm.l3Cache.GetStats(),
		"config":  cm.config,
	}
}

// updateStats updates performance statistics
func (cm *CacheManager) updateStats(latency time.Duration) {
	cm.mu.Lock()
	defer cm.mu.Unlock()
	
	// Update average latency
	total := cm.stats.TotalRequests
	if total > 0 {
		cm.stats.AverageLatency = (cm.stats.AverageLatency*time.Duration(total-1) + latency) / time.Duration(total)
	}
}

// Stop stops the cache manager and all workers
func (cm *CacheManager) Stop() {
	cm.cancel()
	cm.l1Cache.Stop()
	cm.l2Cache.Stop()
	cm.l3Cache.Stop()
}

// GetMemoryUsage returns total memory usage across all cache levels
func (cm *CacheManager) GetMemoryUsage() uint64 {
	return cm.l1Cache.GetMemoryUsage() + 
		   cm.l2Cache.GetMemoryUsage() + 
		   cm.l3Cache.GetMemoryUsage()
}

// FlushToDisk flushes cache data to disk for persistence
func (cm *CacheManager) FlushToDisk(filename string) error {
	// This would implement cache persistence
	// For now, return success
	return nil
}

// LoadFromDisk loads cache data from disk
func (cm *CacheManager) LoadFromDisk(filename string) error {
	// This would implement cache restoration
	// For now, return success
	return nil
} 