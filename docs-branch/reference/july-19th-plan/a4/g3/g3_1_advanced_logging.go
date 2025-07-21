package main

import (
	"encoding/json"
	"fmt"
	"io"
	"log"
	"os"
	"sync"
	"time"
)

// LogLevel represents the severity level of a log message
type LogLevel int

const (
	DEBUG LogLevel = iota
	INFO
	WARN
	ERROR
	FATAL
)

// String returns the string representation of LogLevel
func (l LogLevel) String() string {
	switch l {
	case DEBUG:
		return "DEBUG"
	case INFO:
		return "INFO"
	case WARN:
		return "WARN"
	case ERROR:
		return "ERROR"
	case FATAL:
		return "FATAL"
	default:
		return "UNKNOWN"
	}
}

// LogEntry represents a structured log entry
type LogEntry struct {
	Timestamp   time.Time              `json:"timestamp"`
	Level       LogLevel               `json:"level"`
	Message     string                 `json:"message"`
	Component   string                 `json:"component"`
	TraceID     string                 `json:"trace_id,omitempty"`
	UserID      string                 `json:"user_id,omitempty"`
	Metadata    map[string]interface{} `json:"metadata,omitempty"`
	Performance *PerformanceMetrics    `json:"performance,omitempty"`
}

// PerformanceMetrics tracks performance data
type PerformanceMetrics struct {
	Duration    time.Duration `json:"duration"`
	MemoryUsage int64         `json:"memory_usage_bytes"`
	CPUUsage    float64       `json:"cpu_usage_percent"`
}

// AdvancedLogger provides structured logging capabilities
type AdvancedLogger struct {
	mu          sync.RWMutex
	output      io.Writer
	level       LogLevel
	component   string
	performance bool
	metrics     map[string]*PerformanceMetrics
}

// NewAdvancedLogger creates a new advanced logger instance
func NewAdvancedLogger(component string, level LogLevel, output io.Writer) *AdvancedLogger {
	if output == nil {
		output = os.Stdout
	}
	
	return &AdvancedLogger{
		output:      output,
		level:       level,
		component:   component,
		performance: false,
		metrics:     make(map[string]*PerformanceMetrics),
	}
}

// EnablePerformanceTracking enables performance metrics collection
func (l *AdvancedLogger) EnablePerformanceTracking() {
	l.mu.Lock()
	defer l.mu.Unlock()
	l.performance = true
}

// Log creates a log entry with the specified level and message
func (l *AdvancedLogger) Log(level LogLevel, message string, metadata map[string]interface{}) {
	if level < l.level {
		return
	}

	entry := LogEntry{
		Timestamp: time.Now(),
		Level:     level,
		Message:   message,
		Component: l.component,
		Metadata:  metadata,
	}

	l.writeEntry(entry)
}

// Debug logs a debug message
func (l *AdvancedLogger) Debug(message string, metadata map[string]interface{}) {
	l.Log(DEBUG, message, metadata)
}

// Info logs an info message
func (l *AdvancedLogger) Info(message string, metadata map[string]interface{}) {
	l.Log(INFO, message, metadata)
}

// Warn logs a warning message
func (l *AdvancedLogger) Warn(message string, metadata map[string]interface{}) {
	l.Log(WARN, message, metadata)
}

// Error logs an error message
func (l *AdvancedLogger) Error(message string, err error, metadata map[string]interface{}) {
	if metadata == nil {
		metadata = make(map[string]interface{})
	}
	if err != nil {
		metadata["error"] = err.Error()
	}
	l.Log(ERROR, message, metadata)
}

// Fatal logs a fatal message and exits
func (l *AdvancedLogger) Fatal(message string, metadata map[string]interface{}) {
	l.Log(FATAL, message, metadata)
	os.Exit(1)
}

// StartTimer starts performance tracking for an operation
func (l *AdvancedLogger) StartTimer(operation string) func() {
	if !l.performance {
		return func() {}
	}

	start := time.Now()
	return func() {
		duration := time.Since(start)
		l.mu.Lock()
		l.metrics[operation] = &PerformanceMetrics{
			Duration: duration,
		}
		l.mu.Unlock()
		
		l.Info("Operation completed", map[string]interface{}{
			"operation": operation,
			"duration":  duration.String(),
		})
	}
}

// writeEntry writes a log entry to the output
func (l *AdvancedLogger) writeEntry(entry LogEntry) {
	l.mu.RLock()
	defer l.mu.RUnlock()

	data, err := json.Marshal(entry)
	if err != nil {
		log.Printf("Failed to marshal log entry: %v", err)
		return
	}

	fmt.Fprintln(l.output, string(data))
}

// GetMetrics returns current performance metrics
func (l *AdvancedLogger) GetMetrics() map[string]*PerformanceMetrics {
	l.mu.RLock()
	defer l.mu.RUnlock()
	
	metrics := make(map[string]*PerformanceMetrics)
	for k, v := range l.metrics {
		metrics[k] = v
	}
	return metrics
}

// Example usage and testing
func main() {
	// Create a new logger for the parser component
	logger := NewAdvancedLogger("parser", INFO, os.Stdout)
	logger.EnablePerformanceTracking()

	// Log various messages
	logger.Info("Parser initialized", map[string]interface{}{
		"version": "1.0.0",
		"config":  "default",
	})

	logger.Debug("Parsing configuration", map[string]interface{}{
		"file": "config.pnt",
	})

	// Simulate an operation with timing
	done := logger.StartTimer("parse_file")
	time.Sleep(100 * time.Millisecond) // Simulate work
	done()

	logger.Warn("Deprecated feature used", map[string]interface{}{
		"feature": "old_syntax",
		"line":    42,
	})

	logger.Error("Failed to parse expression", fmt.Errorf("invalid syntax"), map[string]interface{}{
		"expression": "1 + + 2",
		"position":   10,
	})

	// Display metrics
	fmt.Println("\nPerformance Metrics:")
	for operation, metrics := range logger.GetMetrics() {
		fmt.Printf("  %s: %v\n", operation, metrics.Duration)
	}
} 