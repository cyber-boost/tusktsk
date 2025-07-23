package cache

import (
	"encoding/json"
	"fmt"
	"sync"
	"time"
)

// L1Cache provides ultra-fast in-memory caching
type L1Cache struct {
	mu           sync.RWMutex
	data         map[string]*CacheEntry
	maxSize      int
	currentSize  int
	stats        *CacheStats
	evictionPolicy EvictionPolicy
	ttl          time.Duration
	cleanupTicker *time.Ticker
	stopCleanup  chan bool
}

// CacheEntry represents a cached item
type CacheEntry struct {
	Key       string
	Value     interface{}
	Created   time.Time
	Accessed  time.Time
	Hits      int64
	Size      int
	ExpiresAt time.Time
}

// CacheStats tracks cache performance
type CacheStats struct {
	Hits        int64
	Misses      int64
	Evictions   int64
	Sets        int64
	Gets        int64
	Size        int
	MaxSize     int
	HitRate     float64
	MemoryUsage uint64
}

// EvictionPolicy defines how items are evicted from cache
type EvictionPolicy string

const (
	LRU  EvictionPolicy = "lru"  // Least Recently Used
	LFU  EvictionPolicy = "lfu"  // Least Frequently Used
	TTL  EvictionPolicy = "ttl"  // Time To Live
	RAND EvictionPolicy = "rand" // Random
)

// NewL1Cache creates a new L1 cache instance
func NewL1Cache(maxSize int, policy EvictionPolicy, ttl time.Duration) *L1Cache {
	cache := &L1Cache{
		data:           make(map[string]*CacheEntry),
		maxSize:        maxSize,
		evictionPolicy: policy,
		ttl:            ttl,
		stats:          &CacheStats{MaxSize: maxSize},
		stopCleanup:    make(chan bool),
	}
	
	// Start cleanup goroutine
	go cache.startCleanup()
	
	return cache
}

// Set stores a value in the cache
func (c *L1Cache) Set(key string, value interface{}) error {
	c.mu.Lock()
	defer c.mu.Unlock()
	
	// Serialize value to calculate size
	data, err := json.Marshal(value)
	if err != nil {
		return fmt.Errorf("failed to serialize value: %v", err)
	}
	
	size := len(data)
	
	// Check if we need to evict items
	if c.currentSize+size > c.maxSize {
		c.evict(size)
	}
	
	// Create cache entry
	entry := &CacheEntry{
		Key:       key,
		Value:     value,
		Created:   time.Now(),
		Accessed:  time.Now(),
		Hits:      0,
		Size:      size,
		ExpiresAt: time.Now().Add(c.ttl),
	}
	
	// Store in cache
	c.data[key] = entry
	c.currentSize += size
	c.stats.Sets++
	c.stats.Size = c.currentSize
	
	return nil
}

// Get retrieves a value from the cache
func (c *L1Cache) Get(key string) (interface{}, bool) {
	c.mu.Lock()
	defer c.mu.Unlock()
	
	entry, exists := c.data[key]
	if !exists {
		c.stats.Misses++
		return nil, false
	}
	
	// Check if entry has expired
	if time.Now().After(entry.ExpiresAt) {
		delete(c.data, key)
		c.currentSize -= entry.Size
		c.stats.Misses++
		return nil, false
	}
	
	// Update access statistics
	entry.Accessed = time.Now()
	entry.Hits++
	c.stats.Hits++
	
	// Update hit rate
	c.updateHitRate()
	
	return entry.Value, true
}

// Delete removes an item from the cache
func (c *L1Cache) Delete(key string) bool {
	c.mu.Lock()
	defer c.mu.Unlock()
	
	entry, exists := c.data[key]
	if !exists {
		return false
	}
	
	delete(c.data, key)
	c.currentSize -= entry.Size
	c.stats.Size = c.currentSize
	
	return true
}

// Clear removes all items from the cache
func (c *L1Cache) Clear() {
	c.mu.Lock()
	defer c.mu.Unlock()
	
	c.data = make(map[string]*CacheEntry)
	c.currentSize = 0
	c.stats.Size = 0
}

// GetStats returns cache statistics
func (c *L1Cache) GetStats() *CacheStats {
	c.mu.RLock()
	defer c.mu.RUnlock()
	
	stats := *c.stats
	return &stats
}

// GetKeys returns all cache keys
func (c *L1Cache) GetKeys() []string {
	c.mu.RLock()
	defer c.mu.RUnlock()
	
	keys := make([]string, 0, len(c.data))
	for key := range c.data {
		keys = append(keys, key)
	}
	
	return keys
}

// GetSize returns the current cache size
func (c *L1Cache) GetSize() int {
	c.mu.RLock()
	defer c.mu.RUnlock()
	
	return c.currentSize
}

// GetMaxSize returns the maximum cache size
func (c *L1Cache) GetMaxSize() int {
	return c.maxSize
}

// SetMaxSize updates the maximum cache size
func (c *L1Cache) SetMaxSize(maxSize int) {
	c.mu.Lock()
	defer c.mu.Unlock()
	
	c.maxSize = maxSize
	c.stats.MaxSize = maxSize
	
	// Evict items if necessary
	if c.currentSize > maxSize {
		c.evict(c.currentSize - maxSize)
	}
}

// evict removes items from cache based on eviction policy
func (c *L1Cache) evict(neededSpace int) {
	switch c.evictionPolicy {
	case LRU:
		c.evictLRU(neededSpace)
	case LFU:
		c.evictLFU(neededSpace)
	case TTL:
		c.evictTTL(neededSpace)
	case RAND:
		c.evictRandom(neededSpace)
	default:
		c.evictLRU(neededSpace)
	}
}

// evictLRU removes least recently used items
func (c *L1Cache) evictLRU(neededSpace int) {
	// Create sorted list by access time
	type entryWithTime struct {
		key   string
		entry *CacheEntry
	}
	
	var entries []entryWithTime
	for key, entry := range c.data {
		entries = append(entries, entryWithTime{key, entry})
	}
	
	// Sort by access time (oldest first)
	for i := 0; i < len(entries)-1; i++ {
		for j := i + 1; j < len(entries); j++ {
			if entries[i].entry.Accessed.After(entries[j].entry.Accessed) {
				entries[i], entries[j] = entries[j], entries[i]
			}
		}
	}
	
	// Evict oldest entries
	freedSpace := 0
	for _, item := range entries {
		if freedSpace >= neededSpace {
			break
		}
		
		delete(c.data, item.key)
		freedSpace += item.entry.Size
		c.stats.Evictions++
	}
	
	c.currentSize -= freedSpace
	c.stats.Size = c.currentSize
}

// evictLFU removes least frequently used items
func (c *L1Cache) evictLFU(neededSpace int) {
	// Create sorted list by hit count
	type entryWithHits struct {
		key   string
		entry *CacheEntry
	}
	
	var entries []entryWithHits
	for key, entry := range c.data {
		entries = append(entries, entryWithHits{key, entry})
	}
	
	// Sort by hit count (lowest first)
	for i := 0; i < len(entries)-1; i++ {
		for j := i + 1; j < len(entries); j++ {
			if entries[i].entry.Hits > entries[j].entry.Hits {
				entries[i], entries[j] = entries[j], entries[i]
			}
		}
	}
	
	// Evict least frequently used entries
	freedSpace := 0
	for _, item := range entries {
		if freedSpace >= neededSpace {
			break
		}
		
		delete(c.data, item.key)
		freedSpace += item.entry.Size
		c.stats.Evictions++
	}
	
	c.currentSize -= freedSpace
	c.stats.Size = c.currentSize
}

// evictTTL removes expired items
func (c *L1Cache) evictTTL(neededSpace int) {
	now := time.Now()
	freedSpace := 0
	
	for key, entry := range c.data {
		if now.After(entry.ExpiresAt) {
			delete(c.data, key)
			freedSpace += entry.Size
			c.stats.Evictions++
		}
	}
	
	c.currentSize -= freedSpace
	c.stats.Size = c.currentSize
}

// evictRandom removes random items
func (c *L1Cache) evictRandom(neededSpace int) {
	// Simple random eviction - in practice, you'd use a proper random generator
	freedSpace := 0
	for key, entry := range c.data {
		if freedSpace >= neededSpace {
			break
		}
		
		delete(c.data, key)
		freedSpace += entry.Size
		c.stats.Evictions++
	}
	
	c.currentSize -= freedSpace
	c.stats.Size = c.currentSize
}

// updateHitRate calculates the current hit rate
func (c *L1Cache) updateHitRate() {
	total := c.stats.Hits + c.stats.Misses
	if total > 0 {
		c.stats.HitRate = float64(c.stats.Hits) / float64(total)
	}
}

// startCleanup starts the cleanup goroutine
func (c *L1Cache) startCleanup() {
	c.cleanupTicker = time.NewTicker(time.Minute)
	
	for {
		select {
		case <-c.cleanupTicker.C:
			c.cleanup()
		case <-c.stopCleanup:
			c.cleanupTicker.Stop()
			return
		}
	}
}

// cleanup removes expired items
func (c *L1Cache) cleanup() {
	c.mu.Lock()
	defer c.mu.Unlock()
	
	now := time.Now()
	freedSpace := 0
	
	for key, entry := range c.data {
		if now.After(entry.ExpiresAt) {
			delete(c.data, key)
			freedSpace += entry.Size
			c.stats.Evictions++
		}
	}
	
	c.currentSize -= freedSpace
	c.stats.Size = c.currentSize
}

// Stop stops the cache and cleanup goroutine
func (c *L1Cache) Stop() {
	close(c.stopCleanup)
}

// WarmUp preloads the cache with frequently accessed data
func (c *L1Cache) WarmUp(data map[string]interface{}) {
	for key, value := range data {
		c.Set(key, value)
	}
}

// GetMemoryUsage returns estimated memory usage
func (c *L1Cache) GetMemoryUsage() uint64 {
	c.mu.RLock()
	defer c.mu.RUnlock()
	
	// Estimate memory usage (simplified)
	usage := uint64(c.currentSize)
	
	// Add overhead for map structure
	usage += uint64(len(c.data) * 64) // Approximate overhead per entry
	
	return usage
} 