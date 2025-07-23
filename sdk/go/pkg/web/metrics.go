package web

import (
	"runtime"
	"sync"
	"time"

	"github.com/prometheus/client_golang/prometheus"
	"github.com/prometheus/client_golang/prometheus/promauto"
)

// Metrics holds all Prometheus metrics
type Metrics struct {
	mu sync.RWMutex

	// HTTP metrics
	RequestsTotal   prometheus.Counter
	RequestsDuration prometheus.Histogram
	RequestsInFlight prometheus.Gauge
	ResponseSize     prometheus.Histogram

	// WebSocket metrics
	WebSocketConnections prometheus.Gauge
	WebSocketMessages    prometheus.Counter
	WebSocketErrors      prometheus.Counter

	// System metrics
	MemoryAlloc     prometheus.Gauge
	MemoryTotalAlloc prometheus.Gauge
	MemorySys       prometheus.Gauge
	Goroutines      prometheus.Gauge
	GCPause         prometheus.Histogram

	// Custom metrics
	ActiveConnections prometheus.Gauge
	ErrorRate        prometheus.Gauge
	Uptime           prometheus.Gauge

	// Rate limiting metrics
	RateLimitHits   prometheus.Counter
	RateLimitBlocks prometheus.Counter

	// Authentication metrics
	AuthSuccess prometheus.Counter
	AuthFailure prometheus.Counter
	AuthAttempts prometheus.Counter

	// Database metrics (placeholder for future use)
	DatabaseConnections prometheus.Gauge
	DatabaseQueries     prometheus.Counter
	DatabaseErrors      prometheus.Counter
	DatabaseDuration    prometheus.Histogram

	// Cache metrics (placeholder for future use)
	CacheHits   prometheus.Counter
	CacheMisses prometheus.Counter
	CacheSize   prometheus.Gauge

	// Business metrics
	APIEndpointCalls map[string]prometheus.Counter
	UserSessions     prometheus.Gauge
	ActiveUsers      prometheus.Gauge
}

// NewMetrics creates a new metrics instance
func NewMetrics() *Metrics {
	metrics := &Metrics{
		// HTTP metrics
		RequestsTotal: promauto.NewCounter(prometheus.CounterOpts{
			Name: "tusktsk_http_requests_total",
			Help: "Total number of HTTP requests",
		}),
		RequestsDuration: promauto.NewHistogram(prometheus.HistogramOpts{
			Name:    "tusktsk_http_request_duration_seconds",
			Help:    "HTTP request duration in seconds",
			Buckets: prometheus.DefBuckets,
		}),
		RequestsInFlight: promauto.NewGauge(prometheus.GaugeOpts{
			Name: "tusktsk_http_requests_in_flight",
			Help: "Number of HTTP requests currently being processed",
		}),
		ResponseSize: promauto.NewHistogram(prometheus.HistogramOpts{
			Name:    "tusktsk_http_response_size_bytes",
			Help:    "HTTP response size in bytes",
			Buckets: prometheus.ExponentialBuckets(100, 10, 8),
		}),

		// WebSocket metrics
		WebSocketConnections: promauto.NewGauge(prometheus.GaugeOpts{
			Name: "tusktsk_websocket_connections",
			Help: "Number of active WebSocket connections",
		}),
		WebSocketMessages: promauto.NewCounter(prometheus.CounterOpts{
			Name: "tusktsk_websocket_messages_total",
			Help: "Total number of WebSocket messages",
		}),
		WebSocketErrors: promauto.NewCounter(prometheus.CounterOpts{
			Name: "tusktsk_websocket_errors_total",
			Help: "Total number of WebSocket errors",
		}),

		// System metrics
		MemoryAlloc: promauto.NewGauge(prometheus.GaugeOpts{
			Name: "tusktsk_memory_alloc_bytes",
			Help: "Current memory allocation in bytes",
		}),
		MemoryTotalAlloc: promauto.NewGauge(prometheus.GaugeOpts{
			Name: "tusktsk_memory_total_alloc_bytes",
			Help: "Total memory allocation in bytes",
		}),
		MemorySys: promauto.NewGauge(prometheus.GaugeOpts{
			Name: "tusktsk_memory_sys_bytes",
			Help: "System memory usage in bytes",
		}),
		Goroutines: promauto.NewGauge(prometheus.GaugeOpts{
			Name: "tusktsk_goroutines",
			Help: "Number of active goroutines",
		}),
		GCPause: promauto.NewHistogram(prometheus.HistogramOpts{
			Name:    "tusktsk_gc_pause_seconds",
			Help:    "GC pause duration in seconds",
			Buckets: prometheus.ExponentialBuckets(0.001, 2, 10),
		}),

		// Custom metrics
		ActiveConnections: promauto.NewGauge(prometheus.GaugeOpts{
			Name: "tusktsk_active_connections",
			Help: "Number of active connections",
		}),
		ErrorRate: promauto.NewGauge(prometheus.GaugeOpts{
			Name: "tusktsk_error_rate",
			Help: "Error rate percentage",
		}),
		Uptime: promauto.NewGauge(prometheus.GaugeOpts{
			Name: "tusktsk_uptime_seconds",
			Help: "Application uptime in seconds",
		}),

		// Rate limiting metrics
		RateLimitHits: promauto.NewCounter(prometheus.CounterOpts{
			Name: "tusktsk_rate_limit_hits_total",
			Help: "Total number of rate limit hits",
		}),
		RateLimitBlocks: promauto.NewCounter(prometheus.CounterOpts{
			Name: "tusktsk_rate_limit_blocks_total",
			Help: "Total number of rate limit blocks",
		}),

		// Authentication metrics
		AuthSuccess: promauto.NewCounter(prometheus.CounterOpts{
			Name: "tusktsk_auth_success_total",
			Help: "Total number of successful authentications",
		}),
		AuthFailure: promauto.NewCounter(prometheus.CounterOpts{
			Name: "tusktsk_auth_failure_total",
			Help: "Total number of failed authentications",
		}),
		AuthAttempts: promauto.NewCounter(prometheus.CounterOpts{
			Name: "tusktsk_auth_attempts_total",
			Help: "Total number of authentication attempts",
		}),

		// Database metrics
		DatabaseConnections: promauto.NewGauge(prometheus.GaugeOpts{
			Name: "tusktsk_database_connections",
			Help: "Number of active database connections",
		}),
		DatabaseQueries: promauto.NewCounter(prometheus.CounterOpts{
			Name: "tusktsk_database_queries_total",
			Help: "Total number of database queries",
		}),
		DatabaseErrors: promauto.NewCounter(prometheus.CounterOpts{
			Name: "tusktsk_database_errors_total",
			Help: "Total number of database errors",
		}),
		DatabaseDuration: promauto.NewHistogram(prometheus.HistogramOpts{
			Name:    "tusktsk_database_query_duration_seconds",
			Help:    "Database query duration in seconds",
			Buckets: prometheus.DefBuckets,
		}),

		// Cache metrics
		CacheHits: promauto.NewCounter(prometheus.CounterOpts{
			Name: "tusktsk_cache_hits_total",
			Help: "Total number of cache hits",
		}),
		CacheMisses: promauto.NewCounter(prometheus.CounterOpts{
			Name: "tusktsk_cache_misses_total",
			Help: "Total number of cache misses",
		}),
		CacheSize: promauto.NewGauge(prometheus.GaugeOpts{
			Name: "tusktsk_cache_size",
			Help: "Current cache size",
		}),

		// Business metrics
		APIEndpointCalls: make(map[string]prometheus.Counter),
		UserSessions: promauto.NewGauge(prometheus.GaugeOpts{
			Name: "tusktsk_user_sessions",
			Help: "Number of active user sessions",
		}),
		ActiveUsers: promauto.NewGauge(prometheus.GaugeOpts{
			Name: "tusktsk_active_users",
			Help: "Number of active users",
		}),
	}

	// Initialize API endpoint counters
	endpoints := []string{"health", "status", "info", "echo", "websocket", "graphql", "metrics"}
	for _, endpoint := range endpoints {
		metrics.APIEndpointCalls[endpoint] = promauto.NewCounter(prometheus.CounterOpts{
			Name: "tusktsk_api_endpoint_calls_total",
			Help: "Total number of API endpoint calls",
			ConstLabels: prometheus.Labels{"endpoint": endpoint},
		})
	}

	return metrics
}

// RecordRequest records an HTTP request
func (m *Metrics) RecordRequest(method, path string, statusCode int, duration time.Duration, size int) {
	m.mu.Lock()
	defer m.mu.Unlock()

	m.RequestsTotal.Inc()
	m.RequestsDuration.Observe(duration.Seconds())
	m.ResponseSize.Observe(float64(size))

	// Record endpoint-specific metrics
	if counter, exists := m.APIEndpointCalls[path]; exists {
		counter.Inc()
	}
}

// RecordWebSocketConnection records a WebSocket connection
func (m *Metrics) RecordWebSocketConnection() {
	m.mu.Lock()
	defer m.mu.Unlock()

	m.WebSocketConnections.Inc()
	m.ActiveConnections.Inc()
}

// RecordWebSocketDisconnection records a WebSocket disconnection
func (m *Metrics) RecordWebSocketDisconnection() {
	m.mu.Lock()
	defer m.mu.Unlock()

	m.WebSocketConnections.Dec()
	m.ActiveConnections.Dec()
}

// RecordWebSocketMessage records a WebSocket message
func (m *Metrics) RecordWebSocketMessage() {
	m.mu.Lock()
	defer m.mu.Unlock()

	m.WebSocketMessages.Inc()
}

// RecordWebSocketError records a WebSocket error
func (m *Metrics) RecordWebSocketError() {
	m.mu.Lock()
	defer m.mu.Unlock()

	m.WebSocketErrors.Inc()
}

// RecordAuthSuccess records a successful authentication
func (m *Metrics) RecordAuthSuccess() {
	m.mu.Lock()
	defer m.mu.Unlock()

	m.AuthSuccess.Inc()
	m.AuthAttempts.Inc()
}

// RecordAuthFailure records a failed authentication
func (m *Metrics) RecordAuthFailure() {
	m.mu.Lock()
	defer m.mu.Unlock()

	m.AuthFailure.Inc()
	m.AuthAttempts.Inc()
}

// RecordRateLimitHit records a rate limit hit
func (m *Metrics) RecordRateLimitHit() {
	m.mu.Lock()
	defer m.mu.Unlock()

	m.RateLimitHits.Inc()
}

// RecordRateLimitBlock records a rate limit block
func (m *Metrics) RecordRateLimitBlock() {
	m.mu.Lock()
	defer m.mu.Unlock()

	m.RateLimitBlocks.Inc()
}

// UpdateSystemMetrics updates system-related metrics
func (m *Metrics) UpdateSystemMetrics() {
	m.mu.Lock()
	defer m.mu.Unlock()

	var memStats runtime.MemStats
	runtime.ReadMemStats(&memStats)

	m.MemoryAlloc.Set(float64(memStats.Alloc))
	m.MemoryTotalAlloc.Set(float64(memStats.TotalAlloc))
	m.MemorySys.Set(float64(memStats.Sys))
	m.Goroutines.Set(float64(runtime.NumGoroutine()))

	// Record GC pause time
	if memStats.NumGC > 0 {
		m.GCPause.Observe(float64(memStats.PauseNs[(memStats.NumGC-1)%256]) / 1e9)
	}
}

// UpdateUptime updates the uptime metric
func (m *Metrics) UpdateUptime(uptime time.Duration) {
	m.mu.Lock()
	defer m.mu.Unlock()

	m.Uptime.Set(uptime.Seconds())
}

// UpdateErrorRate updates the error rate metric
func (m *Metrics) UpdateErrorRate(errorRate float64) {
	m.mu.Lock()
	defer m.mu.Unlock()

	m.ErrorRate.Set(errorRate)
}

// RecordDatabaseQuery records a database query
func (m *Metrics) RecordDatabaseQuery(duration time.Duration) {
	m.mu.Lock()
	defer m.mu.Unlock()

	m.DatabaseQueries.Inc()
	m.DatabaseDuration.Observe(duration.Seconds())
}

// RecordDatabaseError records a database error
func (m *Metrics) RecordDatabaseError() {
	m.mu.Lock()
	defer m.mu.Unlock()

	m.DatabaseErrors.Inc()
}

// RecordCacheHit records a cache hit
func (m *Metrics) RecordCacheHit() {
	m.mu.Lock()
	defer m.mu.Unlock()

	m.CacheHits.Inc()
}

// RecordCacheMiss records a cache miss
func (m *Metrics) RecordCacheMiss() {
	m.mu.Lock()
	defer m.mu.Unlock()

	m.CacheMisses.Inc()
}

// UpdateCacheSize updates the cache size metric
func (m *Metrics) UpdateCacheSize(size int) {
	m.mu.Lock()
	defer m.mu.Unlock()

	m.CacheSize.Set(float64(size))
}

// UpdateUserSessions updates the user sessions metric
func (m *Metrics) UpdateUserSessions(sessions int) {
	m.mu.Lock()
	defer m.mu.Unlock()

	m.UserSessions.Set(float64(sessions))
}

// UpdateActiveUsers updates the active users metric
func (m *Metrics) UpdateActiveUsers(users int) {
	m.mu.Lock()
	defer m.mu.Unlock()

	m.ActiveUsers.Set(float64(users))
}

// GetMetricsSnapshot returns a snapshot of current metrics
func (m *Metrics) GetMetricsSnapshot() map[string]interface{} {
	m.mu.RLock()
	defer m.mu.RUnlock()

	var memStats runtime.MemStats
	runtime.ReadMemStats(&memStats)

	return map[string]interface{}{
		"requests_total":        m.RequestsTotal,
		"websocket_connections": m.WebSocketConnections,
		"memory_alloc":          memStats.Alloc,
		"memory_total_alloc":    memStats.TotalAlloc,
		"memory_sys":            memStats.Sys,
		"goroutines":            runtime.NumGoroutine(),
		"uptime_seconds":        m.Uptime,
	}
} 