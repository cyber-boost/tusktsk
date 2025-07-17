package main

import (
	"context"
	"database/sql"
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"os"
	"time"

	_ "github.com/lib/pq"
	"github.com/go-redis/redis/v8"
)

// HealthStatus represents the health status of a service
type HealthStatus string

const (
	StatusHealthy   HealthStatus = "healthy"
	StatusDegraded  HealthStatus = "degraded"
	StatusUnhealthy HealthStatus = "unhealthy"
	StatusUnknown   HealthStatus = "unknown"
)

// HealthCheck represents a single health check result
type HealthCheck struct {
	Name      string                 `json:"name"`
	Status    HealthStatus           `json:"status"`
	Message   string                 `json:"message"`
	Details   map[string]interface{} `json:"details,omitempty"`
	Timestamp time.Time              `json:"timestamp"`
}

// HealthReport represents the overall health report
type HealthReport struct {
	Timestamp     time.Time     `json:"timestamp"`
	OverallStatus HealthStatus  `json:"overall_status"`
	Checks        []HealthCheck `json:"checks"`
	Summary       struct {
		Total     int `json:"total"`
		Healthy   int `json:"healthy"`
		Degraded  int `json:"degraded"`
		Unhealthy int `json:"unhealthy"`
		Unknown   int `json:"unknown"`
	} `json:"summary"`
}

// HealthChecker manages health checks
type HealthChecker struct {
	config map[string]interface{}
}

// NewHealthChecker creates a new health checker
func NewHealthChecker(config map[string]interface{}) *HealthChecker {
	return &HealthChecker{
		config: config,
	}
}

// CheckSystemResources checks system resource usage
func (hc *HealthChecker) CheckSystemResources() HealthCheck {
	check := HealthCheck{
		Name:      "system_resources",
		Timestamp: time.Now(),
	}

	// In a real implementation, you would use system calls to get actual metrics
	// For now, we'll simulate the check
	cpuUsage := 45.0 // Simulated CPU usage
	memoryUsage := 60.0 // Simulated memory usage
	diskUsage := 75.0 // Simulated disk usage

	details := map[string]interface{}{
		"cpu_percent":    cpuUsage,
		"memory_percent": memoryUsage,
		"disk_percent":   diskUsage,
	}

	check.Details = details

	if cpuUsage > 80 || memoryUsage > 85 || diskUsage > 90 {
		check.Status = StatusDegraded
		check.Message = fmt.Sprintf("High resource usage - CPU: %.1f%%, Memory: %.1f%%, Disk: %.1f%%", cpuUsage, memoryUsage, diskUsage)
	} else {
		check.Status = StatusHealthy
		check.Message = "System resources are normal"
	}

	return check
}

// CheckDatabase checks database connectivity and performance
func (hc *HealthChecker) CheckDatabase() HealthCheck {
	check := HealthCheck{
		Name:      "database",
		Timestamp: time.Now(),
	}

	dbConfig, ok := hc.config["database"].(map[string]interface{})
	if !ok {
		check.Status = StatusUnhealthy
		check.Message = "Database configuration not found"
		return check
	}

	dsn := fmt.Sprintf("host=%s port=%s user=%s password=%s dbname=%s sslmode=disable",
		dbConfig["host"], dbConfig["port"], dbConfig["user"], dbConfig["password"], dbConfig["name"])

	db, err := sql.Open("postgres", dsn)
	if err != nil {
		check.Status = StatusUnhealthy
		check.Message = fmt.Sprintf("Database connection failed: %v", err)
		return check
	}
	defer db.Close()

	// Set connection timeout
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	// Test basic connectivity
	var result int
	err = db.QueryRowContext(ctx, "SELECT 1").Scan(&result)
	if err != nil {
		check.Status = StatusUnhealthy
		check.Message = fmt.Sprintf("Database query failed: %v", err)
		return check
	}

	// Check active connections
	var activeConnections int
	err = db.QueryRowContext(ctx, "SELECT count(*) FROM pg_stat_activity").Scan(&activeConnections)
	if err != nil {
		check.Status = StatusDegraded
		check.Message = fmt.Sprintf("Failed to get connection count: %v", err)
		return check
	}

	// Check database size
	var dbSize int64
	err = db.QueryRowContext(ctx, "SELECT pg_database_size(current_database())").Scan(&dbSize)
	if err != nil {
		check.Status = StatusDegraded
		check.Message = fmt.Sprintf("Failed to get database size: %v", err)
		return check
	}

	details := map[string]interface{}{
		"active_connections": activeConnections,
		"database_size_bytes": dbSize,
		"database_size_mb":   dbSize / 1024 / 1024,
	}

	check.Details = details

	if activeConnections > 100 {
		check.Status = StatusDegraded
		check.Message = fmt.Sprintf("High number of active connections: %d", activeConnections)
	} else {
		check.Status = StatusHealthy
		check.Message = "Database is healthy"
	}

	return check
}

// CheckRedis checks Redis connectivity and performance
func (hc *HealthChecker) CheckRedis() HealthCheck {
	check := HealthCheck{
		Name:      "redis",
		Timestamp: time.Now(),
	}

	redisConfig, ok := hc.config["redis"].(map[string]interface{})
	if !ok {
		check.Status = StatusUnhealthy
		check.Message = "Redis configuration not found"
		return check
	}

	rdb := redis.NewClient(&redis.Options{
		Addr:     fmt.Sprintf("%s:%s", redisConfig["host"], redisConfig["port"]),
		Password: redisConfig["password"].(string),
		DB:       0,
	})
	defer rdb.Close()

	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	// Test basic connectivity
	_, err := rdb.Ping(ctx).Result()
	if err != nil {
		check.Status = StatusUnhealthy
		check.Message = fmt.Sprintf("Redis connection failed: %v", err)
		return check
	}

	// Get Redis info
	info, err := rdb.Info(ctx).Result()
	if err != nil {
		check.Status = StatusDegraded
		check.Message = fmt.Sprintf("Failed to get Redis info: %v", err)
		return check
	}

	// Parse info for key metrics
	var usedMemory, maxMemory, connectedClients int64
	fmt.Sscanf(info, "used_memory:%d\nmaxmemory:%d\nconnected_clients:%d", &usedMemory, &maxMemory, &connectedClients)

	memoryUsagePercent := float64(0)
	if maxMemory > 0 {
		memoryUsagePercent = float64(usedMemory) / float64(maxMemory) * 100
	}

	details := map[string]interface{}{
		"memory_usage_percent": memoryUsagePercent,
		"connected_clients":    connectedClients,
		"used_memory_mb":       usedMemory / 1024 / 1024,
		"max_memory_mb":        maxMemory / 1024 / 1024,
	}

	check.Details = details

	if memoryUsagePercent > 80 {
		check.Status = StatusDegraded
		check.Message = fmt.Sprintf("High Redis memory usage: %.1f%%", memoryUsagePercent)
	} else if connectedClients > 100 {
		check.Status = StatusDegraded
		check.Message = fmt.Sprintf("High number of Redis clients: %d", connectedClients)
	} else {
		check.Status = StatusHealthy
		check.Message = "Redis is healthy"
	}

	return check
}

// CheckPackageRegistry checks package registry service health
func (hc *HealthChecker) CheckPackageRegistry() HealthCheck {
	check := HealthCheck{
		Name:      "package_registry",
		Timestamp: time.Now(),
	}

	registryConfig, ok := hc.config["registry"].(map[string]interface{})
	if !ok {
		check.Status = StatusUnhealthy
		check.Message = "Registry configuration not found"
		return check
	}

	registryURL := registryConfig["url"].(string)

	// Check health endpoint
	client := &http.Client{Timeout: 10 * time.Second}
	resp, err := client.Get(registryURL + "/health")
	if err != nil {
		check.Status = StatusUnhealthy
		check.Message = fmt.Sprintf("Registry health check failed: %v", err)
		return check
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		check.Status = StatusUnhealthy
		check.Message = fmt.Sprintf("Registry health endpoint returned status %d", resp.StatusCode)
		return check
	}

	// Parse health response
	var healthData map[string]interface{}
	if err := json.NewDecoder(resp.Body).Decode(&healthData); err != nil {
		check.Status = StatusDegraded
		check.Message = fmt.Sprintf("Failed to parse health response: %v", err)
		return check
	}

	check.Details = healthData
	check.Status = StatusHealthy
	check.Message = "Package registry is healthy"

	return check
}

// CheckCDN checks CDN health and synchronization
func (hc *HealthChecker) CheckCDN() HealthCheck {
	check := HealthCheck{
		Name:      "cdn",
		Timestamp: time.Now(),
	}

	cdnConfig, ok := hc.config["cdn"].(map[string]interface{})
	if !ok {
		check.Status = StatusUnhealthy
		check.Message = "CDN configuration not found"
		return check
	}

	cdnURL := cdnConfig["url"].(string)

	// Check CDN health
	client := &http.Client{Timeout: 10 * time.Second}
	resp, err := client.Get(cdnURL + "/health")
	if err != nil {
		check.Status = StatusUnhealthy
		check.Message = fmt.Sprintf("CDN health check failed: %v", err)
		return check
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		check.Status = StatusUnhealthy
		check.Message = fmt.Sprintf("CDN health check returned status %d", resp.StatusCode)
		return check
	}

	// Parse CDN health response
	var cdnHealth map[string]interface{}
	if err := json.NewDecoder(resp.Body).Decode(&cdnHealth); err != nil {
		check.Status = StatusDegraded
		check.Message = fmt.Sprintf("Failed to parse CDN health response: %v", err)
		return check
	}

	// Check sync status
	resp, err = client.Get(cdnURL + "/sync/status")
	if err != nil {
		check.Status = StatusDegraded
		check.Message = fmt.Sprintf("CDN sync status check failed: %v", err)
		return check
	}
	defer resp.Body.Close()

	var syncStatus map[string]interface{}
	if err := json.NewDecoder(resp.Body).Decode(&syncStatus); err != nil {
		check.Status = StatusDegraded
		check.Message = fmt.Sprintf("Failed to parse sync status: %v", err)
		return check
	}

	details := map[string]interface{}{
		"cdn_health":  cdnHealth,
		"sync_status": syncStatus,
	}

	check.Details = details

	if synced, ok := syncStatus["synced"].(bool); !ok || !synced {
		check.Status = StatusDegraded
		check.Message = "CDN synchronization issues detected"
	} else {
		check.Status = StatusHealthy
		check.Message = "CDN is healthy"
	}

	return check
}

// CheckSecurityScanning checks security scanning service health
func (hc *HealthChecker) CheckSecurityScanning() HealthCheck {
	check := HealthCheck{
		Name:      "security_scanning",
		Timestamp: time.Now(),
	}

	securityConfig, ok := hc.config["security"].(map[string]interface{})
	if !ok {
		check.Status = StatusUnhealthy
		check.Message = "Security configuration not found"
		return check
	}

	securityURL := securityConfig["url"].(string)

	// Check security service health
	client := &http.Client{Timeout: 10 * time.Second}
	resp, err := client.Get(securityURL + "/health")
	if err != nil {
		check.Status = StatusUnhealthy
		check.Message = fmt.Sprintf("Security service health check failed: %v", err)
		return check
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		check.Status = StatusUnhealthy
		check.Message = fmt.Sprintf("Security service returned status %d", resp.StatusCode)
		return check
	}

	// Parse security health response
	var securityHealth map[string]interface{}
	if err := json.NewDecoder(resp.Body).Decode(&securityHealth); err != nil {
		check.Status = StatusDegraded
		check.Message = fmt.Sprintf("Failed to parse security health response: %v", err)
		return check
	}

	check.Details = securityHealth

	if scannerActive, ok := securityHealth["scanner_active"].(bool); !ok || !scannerActive {
		check.Status = StatusDegraded
		check.Message = "Security scanner is not active"
	} else {
		check.Status = StatusHealthy
		check.Message = "Security scanning is healthy"
	}

	return check
}

// RunAllChecks runs all health checks and returns a comprehensive report
func (hc *HealthChecker) RunAllChecks() HealthReport {
	checks := []HealthCheck{
		hc.CheckSystemResources(),
		hc.CheckDatabase(),
		hc.CheckRedis(),
		hc.CheckPackageRegistry(),
		hc.CheckCDN(),
		hc.CheckSecurityScanning(),
	}

	// Calculate overall status
	overallStatus := StatusHealthy
	summary := struct {
		Total     int `json:"total"`
		Healthy   int `json:"healthy"`
		Degraded  int `json:"degraded"`
		Unhealthy int `json:"unhealthy"`
		Unknown   int `json:"unknown"`
	}{
		Total: len(checks),
	}

	for _, check := range checks {
		switch check.Status {
		case StatusHealthy:
			summary.Healthy++
		case StatusDegraded:
			summary.Degraded++
			if overallStatus == StatusHealthy {
				overallStatus = StatusDegraded
			}
		case StatusUnhealthy:
			summary.Unhealthy++
			overallStatus = StatusUnhealthy
		case StatusUnknown:
			summary.Unknown++
			overallStatus = StatusUnhealthy
		}
	}

	report := HealthReport{
		Timestamp:     time.Now(),
		OverallStatus: overallStatus,
		Checks:        checks,
		Summary:       summary,
	}

	return report
}

func main() {
	// Configuration (in production, load from environment or config file)
	config := map[string]interface{}{
		"database": map[string]interface{}{
			"host":     "localhost",
			"port":     "5432",
			"name":     "tusklang",
			"user":     "postgres",
			"password": "",
		},
		"redis": map[string]interface{}{
			"host":     "localhost",
			"port":     "6379",
			"password": "",
		},
		"registry": map[string]interface{}{
			"url": "http://localhost:8000",
		},
		"cdn": map[string]interface{}{
			"url": "https://cdn.tusklang.org",
		},
		"security": map[string]interface{}{
			"url": "http://localhost:9000",
		},
	}

	checker := NewHealthChecker(config)
	report := checker.RunAllChecks()

	// Output report as JSON
	jsonData, err := json.MarshalIndent(report, "", "  ")
	if err != nil {
		log.Fatal("Failed to marshal report:", err)
	}

	fmt.Println(string(jsonData))

	// Exit with appropriate code
	switch report.OverallStatus {
	case StatusUnhealthy:
		os.Exit(1)
	case StatusDegraded:
		os.Exit(2)
	default:
		os.Exit(0)
	}
} 