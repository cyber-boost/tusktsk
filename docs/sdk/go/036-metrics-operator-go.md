# @metrics Operator in TuskLang - Go Guide

## 📊 **Metrics Mastery: @metrics Operator Unleashed**

TuskLang's `@metrics` operator is your observability superpower. We don't bow to any king—especially not to blind spots in production. Here's how to use `@metrics` in Go projects to collect, monitor, and alert on your system's performance.

## 📋 **Table of Contents**
- [What is @metrics?](#what-is-metrics)
- [Basic Usage](#basic-usage)
- [Metric Types](#metric-types)
- [Prometheus Integration](#prometheus-integration)
- [Go Integration](#go-integration)
- [Best Practices](#best-practices)

## 📈 **What is @metrics?**

The `@metrics` operator collects and exposes metrics directly from your config. No more scattered monitoring—just pure, centralized observability.

## 🛠️ **Basic Usage**

```go
[monitoring]
startup_time: @metrics("startup_time_ms", 123)
user_count: @metrics("user_count", @query("SELECT COUNT(*) FROM users"))
response_time: @metrics("api_response_time_ms", 45)
```

## 📊 **Metric Types**

### **Counters**
```go
[counters]
requests_total: @metrics("requests_total", 1000)
errors_total: @metrics("errors_total", 5)
```

### **Gauges**
```go
[gauges]
active_connections: @metrics("active_connections", 42)
memory_usage: @metrics("memory_usage_bytes", 1073741824)
```

### **Histograms**
```go
[histograms]
request_duration: @metrics("request_duration_seconds", 0.123)
```

## 🔗 **Prometheus Integration**

```go
// Go - Prometheus metrics
import "github.com/prometheus/client_golang/prometheus"

var (
    requestsTotal = prometheus.NewCounter(prometheus.CounterOpts{
        Name: "requests_total",
        Help: "Total number of requests",
    })
    
    activeConnections = prometheus.NewGauge(prometheus.GaugeOpts{
        Name: "active_connections",
        Help: "Number of active connections",
    })
)

func init() {
    prometheus.MustRegister(requestsTotal)
    prometheus.MustRegister(activeConnections)
}
```

## 🔗 **Go Integration**

```go
// Record metrics from config
startupTime := config.GetInt("startup_time")
userCount := config.GetInt("user_count")

// Update Prometheus metrics
activeConnections.Set(float64(userCount))
```

## 🥇 **Best Practices**
- Use descriptive metric names
- Include units in metric names
- Set up alerts for critical metrics
- Monitor all key performance indicators

---

**TuskLang: See everything with @metrics.** 