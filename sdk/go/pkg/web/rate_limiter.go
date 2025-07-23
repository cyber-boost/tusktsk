package web

import (
	"sync"
	"time"
)

// RateLimiter implements token bucket rate limiting
type RateLimiter struct {
	mu       sync.RWMutex
	buckets  map[string]*TokenBucket
	limit    int
	window   time.Duration
	cleanup  *time.Ticker
	stopChan chan struct{}
}

// TokenBucket represents a token bucket for rate limiting
type TokenBucket struct {
	tokens    int
	lastRefill time.Time
	limit     int
	window    time.Duration
}

// NewRateLimiter creates a new rate limiter
func NewRateLimiter(limit int, window time.Duration) *RateLimiter {
	limiter := &RateLimiter{
		buckets:  make(map[string]*TokenBucket),
		limit:    limit,
		window:   window,
		cleanup:  time.NewTicker(5 * time.Minute), // Cleanup every 5 minutes
		stopChan: make(chan struct{}),
	}

	// Start cleanup goroutine
	go limiter.cleanupExpired()

	return limiter
}

// Allow checks if a request is allowed
func (r *RateLimiter) Allow(key string) bool {
	r.mu.Lock()
	defer r.mu.Unlock()

	bucket, exists := r.buckets[key]
	if !exists {
		bucket = &TokenBucket{
			tokens:     r.limit,
			lastRefill: time.Now(),
			limit:      r.limit,
			window:     r.window,
		}
		r.buckets[key] = bucket
	}

	// Refill tokens
	now := time.Now()
	timePassed := now.Sub(bucket.lastRefill)
	tokensToAdd := int(timePassed / bucket.window) * bucket.limit

	if tokensToAdd > 0 {
		bucket.tokens = min(bucket.limit, bucket.tokens+tokensToAdd)
		bucket.lastRefill = now
	}

	// Check if we have tokens
	if bucket.tokens > 0 {
		bucket.tokens--
		return true
	}

	return false
}

// Remaining returns the number of remaining tokens for a key
func (r *RateLimiter) Remaining(key string) int {
	r.mu.RLock()
	defer r.mu.RUnlock()

	bucket, exists := r.buckets[key]
	if !exists {
		return r.limit
	}

	// Calculate current tokens
	now := time.Now()
	timePassed := now.Sub(bucket.lastRefill)
	tokensToAdd := int(timePassed / bucket.window) * bucket.limit

	currentTokens := min(bucket.limit, bucket.tokens+tokensToAdd)
	return max(0, currentTokens)
}

// Reset resets the rate limiter for a specific key
func (r *RateLimiter) Reset(key string) {
	r.mu.Lock()
	defer r.mu.Unlock()

	delete(r.buckets, key)
}

// GetStats returns rate limiter statistics
func (r *RateLimiter) GetStats() map[string]interface{} {
	r.mu.RLock()
	defer r.mu.RUnlock()

	stats := make(map[string]interface{})
	for key, bucket := range r.buckets {
		stats[key] = map[string]interface{}{
			"tokens":     bucket.tokens,
			"limit":      bucket.limit,
			"last_refill": bucket.lastRefill,
		}
	}

	return stats
}

// cleanupExpired removes expired buckets
func (r *RateLimiter) cleanupExpired() {
	for {
		select {
		case <-r.cleanup.C:
			r.mu.Lock()
			now := time.Now()
			for key, bucket := range r.buckets {
				// Remove buckets that haven't been used in 1 hour
				if now.Sub(bucket.lastRefill) > time.Hour {
					delete(r.buckets, key)
				}
			}
			r.mu.Unlock()
		case <-r.stopChan:
			r.cleanup.Stop()
			return
		}
	}
}

// Stop stops the rate limiter cleanup
func (r *RateLimiter) Stop() {
	close(r.stopChan)
}

// MultiRateLimiter provides multiple rate limiters for different types
type MultiRateLimiter struct {
	ipLimiter    *RateLimiter
	userLimiter  *RateLimiter
	apiLimiter   *RateLimiter
	globalLimiter *RateLimiter
}

// NewMultiRateLimiter creates a new multi-rate limiter
func NewMultiRateLimiter() *MultiRateLimiter {
	return &MultiRateLimiter{
		ipLimiter:     NewRateLimiter(100, time.Minute),      // 100 requests per minute per IP
		userLimiter:   NewRateLimiter(1000, time.Minute),     // 1000 requests per minute per user
		apiLimiter:    NewRateLimiter(10000, time.Minute),    // 10000 requests per minute per API key
		globalLimiter: NewRateLimiter(100000, time.Minute),   // 100000 requests per minute globally
	}
}

// Allow checks if a request is allowed across all limiters
func (m *MultiRateLimiter) Allow(ip, userID, apiKey string) bool {
	// Check global limiter first
	if !m.globalLimiter.Allow("global") {
		return false
	}

	// Check IP limiter
	if !m.ipLimiter.Allow(ip) {
		return false
	}

	// Check user limiter if userID is provided
	if userID != "" && !m.userLimiter.Allow(userID) {
		return false
	}

	// Check API key limiter if apiKey is provided
	if apiKey != "" && !m.apiLimiter.Allow(apiKey) {
		return false
	}

	return true
}

// GetRemaining returns remaining tokens for all limiters
func (m *MultiRateLimiter) GetRemaining(ip, userID, apiKey string) map[string]int {
	return map[string]int{
		"global": m.globalLimiter.Remaining("global"),
		"ip":     m.ipLimiter.Remaining(ip),
		"user":   m.userLimiter.Remaining(userID),
		"api":    m.apiLimiter.Remaining(apiKey),
	}
}

// Reset resets all limiters for given keys
func (m *MultiRateLimiter) Reset(ip, userID, apiKey string) {
	m.globalLimiter.Reset("global")
	m.ipLimiter.Reset(ip)
	m.userLimiter.Reset(userID)
	m.apiLimiter.Reset(apiKey)
}

// GetStats returns statistics for all limiters
func (m *MultiRateLimiter) GetStats() map[string]interface{} {
	return map[string]interface{}{
		"global": m.globalLimiter.GetStats(),
		"ip":     m.ipLimiter.GetStats(),
		"user":   m.userLimiter.GetStats(),
		"api":    m.apiLimiter.GetStats(),
	}
}

// Stop stops all limiters
func (m *MultiRateLimiter) Stop() {
	m.globalLimiter.Stop()
	m.ipLimiter.Stop()
	m.userLimiter.Stop()
	m.apiLimiter.Stop()
}

// AdaptiveRateLimiter provides adaptive rate limiting based on user behavior
type AdaptiveRateLimiter struct {
	mu           sync.RWMutex
	limiters     map[string]*RateLimiter
	userScores   map[string]float64
	baseLimit    int
	baseWindow   time.Duration
	scoreDecay   float64
	scoreIncrement float64
}

// NewAdaptiveRateLimiter creates a new adaptive rate limiter
func NewAdaptiveRateLimiter(baseLimit int, baseWindow time.Duration) *AdaptiveRateLimiter {
	return &AdaptiveRateLimiter{
		limiters:       make(map[string]*RateLimiter),
		userScores:     make(map[string]float64),
		baseLimit:      baseLimit,
		baseWindow:     baseWindow,
		scoreDecay:     0.95, // Decay score by 5% per minute
		scoreIncrement: 0.1,  // Increment score by 10% per successful request
	}
}

// Allow checks if a request is allowed with adaptive limits
func (a *AdaptiveRateLimiter) Allow(key string) bool {
	a.mu.Lock()
	defer a.mu.Unlock()

	// Get or create limiter for this key
	limiter, exists := a.limiters[key]
	if !exists {
		score := a.userScores[key]
		adaptiveLimit := int(float64(a.baseLimit) * (1.0 + score))
		limiter = NewRateLimiter(adaptiveLimit, a.baseWindow)
		a.limiters[key] = limiter
	}

	allowed := limiter.Allow(key)

	// Update user score based on behavior
	if allowed {
		// Increment score for good behavior
		a.userScores[key] = min(1.0, a.userScores[key]+a.scoreIncrement)
	} else {
		// Decrement score for bad behavior
		a.userScores[key] = max(-1.0, a.userScores[key]-a.scoreIncrement)
	}

	return allowed
}

// GetScore returns the current score for a key
func (a *AdaptiveRateLimiter) GetScore(key string) float64 {
	a.mu.RLock()
	defer a.mu.RUnlock()

	return a.userScores[key]
}

// SetScore sets the score for a key
func (a *AdaptiveRateLimiter) SetScore(key string, score float64) {
	a.mu.Lock()
	defer a.mu.Unlock()

	a.userScores[key] = max(-1.0, min(1.0, score))
}

// GetStats returns adaptive rate limiter statistics
func (a *AdaptiveRateLimiter) GetStats() map[string]interface{} {
	a.mu.RLock()
	defer a.mu.RUnlock()

	stats := make(map[string]interface{})
	for key, limiter := range a.limiters {
		stats[key] = map[string]interface{}{
			"limiter": limiter.GetStats(),
			"score":   a.userScores[key],
		}
	}

	return stats
}

// Helper functions
func min(a, b int) int {
	if a < b {
		return a
	}
	return b
}

func max(a, b int) int {
	if a > b {
		return a
	}
	return b
}

func minFloat(a, b float64) float64 {
	if a < b {
		return a
	}
	return b
}

func maxFloat(a, b float64) float64 {
	if a > b {
		return a
	}
	return b
} 