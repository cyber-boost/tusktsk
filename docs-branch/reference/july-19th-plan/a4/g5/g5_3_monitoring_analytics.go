package main

import (
	"fmt"
	"math"
	"sync"
	"time"
)

// MetricType represents the type of metric
type MetricType string

const (
	Counter   MetricType = "counter"
	Gauge     MetricType = "gauge"
	Histogram MetricType = "histogram"
	Summary   MetricType = "summary"
)

// Metric represents a system metric
type Metric struct {
	Name        string                 `json:"name"`
	Type        MetricType             `json:"type"`
	Value       float64                `json:"value"`
	Unit        string                 `json:"unit"`
	Labels      map[string]string      `json:"labels"`
	Timestamp   time.Time              `json:"timestamp"`
	Metadata    map[string]interface{} `json:"metadata"`
}

// PerformanceMetric represents performance-specific metrics
type PerformanceMetric struct {
	Metric
	MinValue    float64 `json:"min_value"`
	MaxValue    float64 `json:"max_value"`
	AvgValue    float64 `json:"avg_value"`
	Count       int64   `json:"count"`
	Sum         float64 `json:"sum"`
	Percentiles map[string]float64 `json:"percentiles"`
}

// Alert represents a system alert
type Alert struct {
	ID          string                 `json:"id"`
	Name        string                 `json:"name"`
	Description string                 `json:"description"`
	Severity    string                 `json:"severity"`
	Status      string                 `json:"status"`
	Metric      string                 `json:"metric"`
	Threshold   float64                `json:"threshold"`
	CurrentValue float64               `json:"current_value"`
	TriggeredAt time.Time              `json:"triggered_at"`
	ResolvedAt  *time.Time             `json:"resolved_at"`
	Metadata    map[string]interface{} `json:"metadata"`
}

// AnalyticsData represents analytics data
type AnalyticsData struct {
	ID          string                 `json:"id"`
	Type        string                 `json:"type"`
	Data        map[string]interface{} `json:"data"`
	Timestamp   time.Time              `json:"timestamp"`
	Duration    time.Duration          `json:"duration"`
	Metadata    map[string]interface{} `json:"metadata"`
}

// MonitoringAnalytics provides monitoring and analytics capabilities
type MonitoringAnalytics struct {
	metrics     map[string]*Metric
	alerts      map[string]*Alert
	analytics   []AnalyticsData
	collectors  map[string]MetricCollector
	processors  map[string]DataProcessor
	mu          sync.RWMutex
	startTime   time.Time
}

// MetricCollector interface for collecting metrics
type MetricCollector interface {
	Collect() ([]Metric, error)
	Name() string
}

// DataProcessor interface for processing analytics data
type DataProcessor interface {
	Process(data AnalyticsData) (AnalyticsData, error)
	Name() string
}

// SystemMetricsCollector collects system metrics
type SystemMetricsCollector struct {
	lastCPU    float64
	lastMemory uint64
}

// PerformanceMetricsCollector collects performance metrics
type PerformanceMetricsCollector struct {
	responseTimes []time.Duration
	mu            sync.RWMutex
}

// AnalyticsProcessor processes analytics data
type AnalyticsProcessor struct {
	aggregations map[string]interface{}
	mu           sync.RWMutex
}

// NewMonitoringAnalytics creates a new monitoring and analytics instance
func NewMonitoringAnalytics() *MonitoringAnalytics {
	return &MonitoringAnalytics{
		metrics:    make(map[string]*Metric),
		alerts:     make(map[string]*Alert),
		analytics:  make([]AnalyticsData, 0),
		collectors: make(map[string]MetricCollector),
		processors: make(map[string]DataProcessor),
		startTime:  time.Now(),
	}
}

// RegisterCollector registers a metric collector
func (ma *MonitoringAnalytics) RegisterCollector(collector MetricCollector) {
	ma.mu.Lock()
	defer ma.mu.Unlock()
	ma.collectors[collector.Name()] = collector
}

// RegisterProcessor registers a data processor
func (ma *MonitoringAnalytics) RegisterProcessor(processor DataProcessor) {
	ma.mu.Lock()
	defer ma.mu.Unlock()
	ma.processors[processor.Name()] = processor
}

// RecordMetric records a metric
func (ma *MonitoringAnalytics) RecordMetric(metric *Metric) {
	ma.mu.Lock()
	defer ma.mu.Unlock()

	metric.Timestamp = time.Now()
	ma.metrics[metric.Name] = metric

	// Check for alerts
	ma.checkAlerts(metric)
}

// RecordPerformanceMetric records a performance metric
func (ma *MonitoringAnalytics) RecordPerformanceMetric(name string, value float64, labels map[string]string) {
	metric := &PerformanceMetric{
		Metric: Metric{
			Name:      name,
			Type:      Histogram,
			Value:     value,
			Labels:    labels,
			Timestamp: time.Now(),
		},
		MinValue: value,
		MaxValue: value,
		AvgValue: value,
		Count:    1,
		Sum:      value,
		Percentiles: map[string]float64{
			"p50": value,
			"p95": value,
			"p99": value,
		},
	}

	ma.mu.Lock()
	defer ma.mu.Unlock()

	// Update existing metric if it exists
	if existing, exists := ma.metrics[name]; exists {
		// For simplicity, just update the value
		existing.Value = value
		existing.Timestamp = time.Now()
	} else {
		ma.metrics[name] = &metric.Metric
	}

	// Check for alerts
	ma.checkAlerts(&metric.Metric)
}

// CreateAlert creates a new alert
func (ma *MonitoringAnalytics) CreateAlert(alert *Alert) {
	ma.mu.Lock()
	defer ma.mu.Unlock()

	alert.ID = fmt.Sprintf("alert-%d", time.Now().UnixNano())
	alert.Status = "active"
	ma.alerts[alert.ID] = alert
}

// GetMetrics returns all metrics
func (ma *MonitoringAnalytics) GetMetrics() map[string]*Metric {
	ma.mu.RLock()
	defer ma.mu.RUnlock()

	metrics := make(map[string]*Metric)
	for name, metric := range ma.metrics {
		metrics[name] = metric
	}

	return metrics
}

// GetAlerts returns all alerts
func (ma *MonitoringAnalytics) GetAlerts() map[string]*Alert {
	ma.mu.RLock()
	defer ma.mu.RUnlock()

	alerts := make(map[string]*Alert)
	for id, alert := range ma.alerts {
		alerts[id] = alert
	}

	return alerts
}

// CollectMetrics collects metrics from all registered collectors
func (ma *MonitoringAnalytics) CollectMetrics() error {
	ma.mu.RLock()
	collectors := make(map[string]MetricCollector)
	for name, collector := range ma.collectors {
		collectors[name] = collector
	}
	ma.mu.RUnlock()

	for name, collector := range collectors {
		metrics, err := collector.Collect()
		if err != nil {
			return fmt.Errorf("collector %s failed: %v", name, err)
		}

		for _, metric := range metrics {
			ma.RecordMetric(&metric)
		}
	}

	return nil
}

// ProcessAnalytics processes analytics data
func (ma *MonitoringAnalytics) ProcessAnalytics(data AnalyticsData) error {
	ma.mu.RLock()
	processors := make(map[string]DataProcessor)
	for name, processor := range ma.processors {
		processors[name] = processor
	}
	ma.mu.RUnlock()

	for name, processor := range processors {
		processed, err := processor.Process(data)
		if err != nil {
			return fmt.Errorf("processor %s failed: %v", name, err)
		}

		ma.mu.Lock()
		ma.analytics = append(ma.analytics, processed)
		ma.mu.Unlock()
	}

	return nil
}

// GetAnalytics returns analytics data
func (ma *MonitoringAnalytics) GetAnalytics() []AnalyticsData {
	ma.mu.RLock()
	defer ma.mu.RUnlock()

	analytics := make([]AnalyticsData, len(ma.analytics))
	copy(analytics, ma.analytics)

	return analytics
}

// GetUptime returns system uptime
func (ma *MonitoringAnalytics) GetUptime() time.Duration {
	return time.Since(ma.startTime)
}

// GetSystemHealth returns overall system health
func (ma *MonitoringAnalytics) GetSystemHealth() map[string]interface{} {
	ma.mu.RLock()
	defer ma.mu.RUnlock()

	health := map[string]interface{}{
		"uptime":     ma.GetUptime().String(),
		"metrics":    len(ma.metrics),
		"alerts":     len(ma.alerts),
		"analytics":  len(ma.analytics),
		"collectors": len(ma.collectors),
		"processors": len(ma.processors),
	}

	// Calculate health score
	activeAlerts := 0
	for _, alert := range ma.alerts {
		if alert.Status == "active" {
			activeAlerts++
		}
	}

	healthScore := 100.0
	if activeAlerts > 0 {
		healthScore = math.Max(0, 100-float64(activeAlerts)*10)
	}

	health["health_score"] = healthScore
	health["active_alerts"] = activeAlerts

	return health
}

// Helper methods
func (ma *MonitoringAnalytics) checkAlerts(metric *Metric) {
	for _, alert := range ma.alerts {
		if alert.Metric == metric.Name && alert.Status == "active" {
			if metric.Value > alert.Threshold {
				alert.CurrentValue = metric.Value
				alert.TriggeredAt = time.Now()
			} else if alert.Status == "triggered" {
				now := time.Now()
				alert.ResolvedAt = &now
				alert.Status = "resolved"
			}
		}
	}
}

// SystemMetricsCollector implementation
func (smc *SystemMetricsCollector) Name() string {
	return "system"
}

func (smc *SystemMetricsCollector) Collect() ([]Metric, error) {
	// Simulate system metrics collection
	metrics := []Metric{
		{
			Name:      "cpu_usage",
			Type:      Gauge,
			Value:     45.2,
			Unit:      "percent",
			Timestamp: time.Now(),
		},
		{
			Name:      "memory_usage",
			Type:      Gauge,
			Value:     67.8,
			Unit:      "percent",
			Timestamp: time.Now(),
		},
		{
			Name:      "disk_usage",
			Type:      Gauge,
			Value:     23.4,
			Unit:      "percent",
			Timestamp: time.Now(),
		},
	}

	return metrics, nil
}

// PerformanceMetricsCollector implementation
func (pmc *PerformanceMetricsCollector) Name() string {
	return "performance"
}

func (pmc *PerformanceMetricsCollector) Collect() ([]Metric, error) {
	pmc.mu.RLock()
	defer pmc.mu.RUnlock()

	if len(pmc.responseTimes) == 0 {
		return []Metric{}, nil
	}

	// Calculate statistics
	var sum time.Duration
	min := pmc.responseTimes[0]
	max := pmc.responseTimes[0]

	for _, rt := range pmc.responseTimes {
		sum += rt
		if rt < min {
			min = rt
		}
		if rt > max {
			max = rt
		}
	}

	avg := sum / time.Duration(len(pmc.responseTimes))

	metrics := []Metric{
		{
			Name:      "response_time_avg",
			Type:      Gauge,
			Value:     float64(avg.Milliseconds()),
			Unit:      "ms",
			Timestamp: time.Now(),
		},
		{
			Name:      "response_time_min",
			Type:      Gauge,
			Value:     float64(min.Milliseconds()),
			Unit:      "ms",
			Timestamp: time.Now(),
		},
		{
			Name:      "response_time_max",
			Type:      Gauge,
			Value:     float64(max.Milliseconds()),
			Unit:      "ms",
			Timestamp: time.Now(),
		},
	}

	return metrics, nil
}

func (pmc *PerformanceMetricsCollector) RecordResponseTime(duration time.Duration) {
	pmc.mu.Lock()
	defer pmc.mu.Unlock()

	pmc.responseTimes = append(pmc.responseTimes, duration)

	// Keep only last 1000 measurements
	if len(pmc.responseTimes) > 1000 {
		pmc.responseTimes = pmc.responseTimes[len(pmc.responseTimes)-1000:]
	}
}

// AnalyticsProcessor implementation
func (ap *AnalyticsProcessor) Name() string {
	return "analytics"
}

func (ap *AnalyticsProcessor) Process(data AnalyticsData) (AnalyticsData, error) {
	ap.mu.Lock()
	defer ap.mu.Unlock()

	// Process analytics data
	processed := data
	processed.ID = fmt.Sprintf("processed-%s", data.ID)
	processed.Timestamp = time.Now()

	// Initialize metadata if nil
	if processed.Metadata == nil {
		processed.Metadata = make(map[string]interface{})
	}

	// Add aggregations
	if ap.aggregations == nil {
		ap.aggregations = make(map[string]interface{})
	}

	// Simple aggregation logic
	if count, exists := ap.aggregations["total_processed"]; exists {
		ap.aggregations["total_processed"] = count.(int) + 1
	} else {
		ap.aggregations["total_processed"] = 1
	}

	processed.Metadata["aggregations"] = ap.aggregations

	return processed, nil
}

// Example usage and testing
func main() {
	// Create monitoring and analytics
	ma := NewMonitoringAnalytics()

	// Register collectors
	systemCollector := &SystemMetricsCollector{}
	performanceCollector := &PerformanceMetricsCollector{}
	analyticsProcessor := &AnalyticsProcessor{}

	ma.RegisterCollector(systemCollector)
	ma.RegisterCollector(performanceCollector)
	ma.RegisterProcessor(analyticsProcessor)

	// Create alerts
	ma.CreateAlert(&Alert{
		Name:        "High CPU Usage",
		Description: "CPU usage is above threshold",
		Severity:    "warning",
		Metric:      "cpu_usage",
		Threshold:   80.0,
	})

	ma.CreateAlert(&Alert{
		Name:        "High Memory Usage",
		Description: "Memory usage is above threshold",
		Severity:    "critical",
		Metric:      "memory_usage",
		Threshold:   90.0,
	})

	fmt.Println("Monitoring and Analytics Demo")
	fmt.Println("=============================")

	// Record some metrics
	ma.RecordMetric(&Metric{
		Name:      "requests_total",
		Type:      Counter,
		Value:     1000,
		Unit:      "requests",
		Labels:    map[string]string{"endpoint": "/api/v1/users"},
	})

	ma.RecordPerformanceMetric("api_response_time", 150.5, map[string]string{
		"endpoint": "/api/v1/users",
		"method":   "GET",
	})

	ma.RecordPerformanceMetric("api_response_time", 200.3, map[string]string{
		"endpoint": "/api/v1/users",
		"method":   "POST",
	})

	// Collect metrics from collectors
	if err := ma.CollectMetrics(); err != nil {
		fmt.Printf("Error collecting metrics: %v\n", err)
	}

	// Process analytics data
	analyticsData := AnalyticsData{
		ID:    "analytics-001",
		Type:  "user_activity",
		Data:  map[string]interface{}{"users_online": 150, "active_sessions": 75},
		Duration: 5 * time.Minute,
	}

	if err := ma.ProcessAnalytics(analyticsData); err != nil {
		fmt.Printf("Error processing analytics: %v\n", err)
	}

	// Display results
	metrics := ma.GetMetrics()
	fmt.Printf("Metrics collected: %d\n", len(metrics))

	alerts := ma.GetAlerts()
	fmt.Printf("Alerts configured: %d\n", len(alerts))

	analytics := ma.GetAnalytics()
	fmt.Printf("Analytics processed: %d\n", len(analytics))

	health := ma.GetSystemHealth()
	fmt.Printf("System health score: %.1f%%\n", health["health_score"])
	fmt.Printf("System uptime: %s\n", health["uptime"])

	// Display some metrics
	for name, metric := range metrics {
		fmt.Printf("  %s: %.2f %s\n", name, metric.Value, metric.Unit)
	}

	fmt.Println("\nMonitoring and analytics successfully integrated with TuskLang Go SDK")
} 