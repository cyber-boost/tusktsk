# Cron Directives - Go

## 🎯 What Are Cron Directives?

Cron directives (`#cron`) in TuskLang allow you to define scheduled tasks, job execution timing, error handling, and logging configuration directly in your configuration files. They transform static config into executable scheduled job logic.

```go
// Cron directives define your entire scheduled task system
type CronConfig struct {
    Jobs        map[string]string `tsk:"#cron_jobs"`
    Schedules   map[string]string `tsk:"#cron_schedules"`
    ErrorHandling map[string]string `tsk:"#cron_error_handling"`
    Logging     map[string]string `tsk:"#cron_logging"`
}
```

## 🚀 Why Cron Directives Matter

### Traditional Cron Development
```go
// Old way - scattered across multiple files
func main() {
    c := cron.New()
    
    // Jobs defined in code
    c.AddFunc("0 0 * * *", cleanupOldData)      // Daily at midnight
    c.AddFunc("0 */6 * * *", backupDatabase)    // Every 6 hours
    c.AddFunc("*/15 * * * *", checkHealth)      // Every 15 minutes
    
    // Error handling scattered
    c.AddFunc("0 2 * * *", func() {
        if err := generateReports(); err != nil {
            log.Printf("Report generation failed: %v", err)
            sendAlert("Report generation failed")
        }
    })
    
    c.Start()
    select {}
}
```

### TuskLang Cron Directives - Declarative & Dynamic
```tsk
# cron.tsk - Complete cron job definition
cron_jobs: #cron("""
    cleanup_old_data -> Clean up old data files
        schedule: "0 0 * * *"
        handler: handlers.CleanupOldData
        timeout: 30m
        retries: 3
    
    backup_database -> Create database backup
        schedule: "0 */6 * * *"
        handler: handlers.BackupDatabase
        timeout: 1h
        retries: 2
    
    check_health -> Check system health
        schedule: "*/15 * * * *"
        handler: handlers.CheckHealth
        timeout: 5m
        retries: 1
    
    generate_reports -> Generate daily reports
        schedule: "0 2 * * *"
        handler: handlers.GenerateReports
        timeout: 2h
        retries: 3
        error_handler: handlers.ReportErrorHandler
""")

cron_error_handling: #cron("""
    max_retries -> 3
    retry_delay -> 5m
    alert_on_failure -> true
    alert_recipients -> admin@example.com
""")

cron_logging: #cron("""
    log_level -> info
    log_file -> /var/log/cron.log
    log_format -> json
    log_rotation -> daily
""")
```

## 📋 Cron Directive Types

### 1. **Job Directives** (`#cron_jobs`)
- Job definitions and schedules
- Handler function mappings
- Timeout and retry configuration
- Error handling strategies

### 2. **Schedule Directives** (`#cron_schedules`)
- Cron expression definitions
- Timezone configuration
- Execution frequency
- Conditional scheduling

### 3. **Error Handling Directives** (`#cron_error_handling`)
- Retry strategies
- Failure notifications
- Error recovery
- Alert configuration

### 4. **Logging Directives** (`#cron_logging`)
- Log level configuration
- Log file management
- Log rotation
- Log formatting

## 🔧 Basic Cron Directive Syntax

### Simple Job Definition
```tsk
# Basic job with schedule
cleanup_job: #cron("0 0 * * * -> handlers.CleanupData")
```

### Job with Configuration
```tsk
# Job with timeout and retries
backup_job: #cron("""
    schedule: "0 */6 * * *"
    handler: handlers.BackupDatabase
    timeout: 1h
    retries: 3
""")
```

### Multiple Jobs
```tsk
# Multiple jobs in one directive
system_jobs: #cron("""
    cleanup -> "0 0 * * *" -> handlers.CleanupData
    backup -> "0 */6 * * *" -> handlers.BackupDatabase
    health -> "*/15 * * * *" -> handlers.CheckHealth
    reports -> "0 2 * * *" -> handlers.GenerateReports
""")
```

## 🎯 Go Integration Patterns

### Struct Tags for Cron Directives
```go
type CronConfig struct {
    // Job definitions
    Jobs string `tsk:"#cron_jobs"`
    
    // Schedule definitions
    Schedules string `tsk:"#cron_schedules"`
    
    // Error handling configuration
    ErrorHandling string `tsk:"#cron_error_handling"`
    
    // Logging configuration
    Logging string `tsk:"#cron_logging"`
}
```

### Cron Application Setup
```go
package main

import (
    "github.com/tusklang/go-sdk"
    "github.com/robfig/cron/v3"
    "log"
)

func main() {
    // Load cron configuration
    config := tusk.LoadConfig("cron.tsk")
    
    var cronConfig CronConfig
    config.Unmarshal(&cronConfig)
    
    // Create cron scheduler from directives
    scheduler := tusk.NewCronScheduler(cronConfig)
    
    // Start the cron scheduler
    scheduler.Start()
    
    // Keep the application running
    select {}
}
```

### Job Handler Implementation
```go
package handlers

import (
    "context"
    "log"
    "time"
)

// Job handlers
func CleanupOldData(ctx context.Context) error {
    log.Println("Starting cleanup of old data...")
    
    // Set timeout from context
    timeout := 30 * time.Minute
    if deadline, ok := ctx.Deadline(); ok {
        timeout = time.Until(deadline)
    }
    
    // Create timeout context
    timeoutCtx, cancel := context.WithTimeout(ctx, timeout)
    defer cancel()
    
    // Cleanup logic
    if err := cleanupFiles(timeoutCtx); err != nil {
        return fmt.Errorf("failed to cleanup files: %v", err)
    }
    
    if err := cleanupDatabase(timeoutCtx); err != nil {
        return fmt.Errorf("failed to cleanup database: %v", err)
    }
    
    log.Println("Cleanup completed successfully")
    return nil
}

func BackupDatabase(ctx context.Context) error {
    log.Println("Starting database backup...")
    
    // Backup logic
    backupPath := fmt.Sprintf("/backups/db_%s.sql", time.Now().Format("2006-01-02_15-04-05"))
    
    if err := createDatabaseBackup(ctx, backupPath); err != nil {
        return fmt.Errorf("failed to create backup: %v", err)
    }
    
    // Compress backup
    if err := compressBackup(ctx, backupPath); err != nil {
        return fmt.Errorf("failed to compress backup: %v", err)
    }
    
    // Upload to cloud storage
    if err := uploadBackup(ctx, backupPath+".gz"); err != nil {
        return fmt.Errorf("failed to upload backup: %v", err)
    }
    
    log.Printf("Database backup completed: %s", backupPath)
    return nil
}

func CheckHealth(ctx context.Context) error {
    log.Println("Checking system health...")
    
    // Health checks
    checks := []HealthCheck{
        {Name: "Database", Check: checkDatabaseHealth},
        {Name: "Redis", Check: checkRedisHealth},
        {Name: "API", Check: checkAPIHealth},
        {Name: "Disk Space", Check: checkDiskSpace},
    }
    
    var failures []string
    
    for _, check := range checks {
        if err := check.Check(ctx); err != nil {
            failures = append(failures, fmt.Sprintf("%s: %v", check.Name, err))
        }
    }
    
    if len(failures) > 0 {
        return fmt.Errorf("health check failures: %s", strings.Join(failures, "; "))
    }
    
    log.Println("All health checks passed")
    return nil
}

func GenerateReports(ctx context.Context) error {
    log.Println("Generating daily reports...")
    
    // Report generation logic
    reports := []Report{
        {Name: "User Activity", Generator: generateUserActivityReport},
        {Name: "Sales Summary", Generator: generateSalesReport},
        {Name: "System Metrics", Generator: generateSystemMetricsReport},
    }
    
    for _, report := range reports {
        if err := report.Generator(ctx); err != nil {
            return fmt.Errorf("failed to generate %s report: %v", report.Name, err)
        }
    }
    
    log.Println("All reports generated successfully")
    return nil
}
```

## 🔄 Advanced Scheduling

### Conditional Scheduling
```tsk
# Conditional scheduling based on environment
production_jobs: #if(
    #env("ENVIRONMENT") == "production",
    #cron("""
        backup -> "0 */4 * * *" -> handlers.BackupDatabase
        monitoring -> "*/5 * * * *" -> handlers.MonitorSystem
        cleanup -> "0 1 * * *" -> handlers.CleanupData
    """),
    #cron("""
        backup -> "0 */12 * * *" -> handlers.BackupDatabase
        monitoring -> "*/15 * * * *" -> handlers.MonitorSystem
    """)
)
```

### Dynamic Scheduling
```tsk
# Dynamic scheduling based on load
adaptive_jobs: #cron("""
    health_check -> #if(
        #metrics("system_load") > 80,
        "*/30 * * * *",
        "*/5 * * * *"
    ) -> handlers.CheckHealth
    
    backup -> #if(
        #env("ENVIRONMENT") == "production",
        "0 */4 * * *",
        "0 */12 * * *"
    ) -> handlers.BackupDatabase
""")
```

### Timezone-Aware Scheduling
```tsk
# Timezone-aware scheduling
timezone_jobs: #cron("""
    daily_report -> "0 9 * * *" -> handlers.GenerateDailyReport
        timezone: "America/New_York"
    
    weekly_backup -> "0 2 * * 0" -> handlers.WeeklyBackup
        timezone: "UTC"
    
    maintenance -> "0 3 * * 1" -> handlers.Maintenance
        timezone: "Europe/London"
""")
```

## 🛡️ Error Handling

### Error Handling Configuration
```tsk
# Error handling configuration
cron_error_handling: #cron("""
    max_retries -> 3
    retry_delay -> 5m
    exponential_backoff -> true
    alert_on_failure -> true
    alert_recipients -> admin@example.com, ops@example.com
    alert_channels -> email, slack
    failure_threshold -> 3
    circuit_breaker -> true
""")
```

### Go Error Handler Implementation
```go
package handlers

import (
    "context"
    "fmt"
    "log"
    "time"
)

// Error handler for report generation
func ReportErrorHandler(ctx context.Context, err error) error {
    log.Printf("Report generation failed: %v", err)
    
    // Send alert
    alert := Alert{
        Type:    "cron_failure",
        Job:     "generate_reports",
        Error:   err.Error(),
        Time:    time.Now(),
    }
    
    if err := sendAlert(alert); err != nil {
        log.Printf("Failed to send alert: %v", err)
    }
    
    // Try alternative report generation
    if err := generateAlternativeReports(ctx); err != nil {
        return fmt.Errorf("alternative report generation also failed: %v", err)
    }
    
    return nil
}

// Retry handler with exponential backoff
func RetryHandler(ctx context.Context, job func(context.Context) error, maxRetries int) error {
    var lastErr error
    
    for attempt := 0; attempt <= maxRetries; attempt++ {
        if err := job(ctx); err == nil {
            return nil
        } else {
            lastErr = err
            log.Printf("Job failed (attempt %d/%d): %v", attempt+1, maxRetries+1, err)
            
            if attempt < maxRetries {
                // Exponential backoff
                delay := time.Duration(attempt+1) * 5 * time.Minute
                log.Printf("Retrying in %v...", delay)
                
                select {
                case <-time.After(delay):
                    continue
                case <-ctx.Done():
                    return ctx.Err()
                }
            }
        }
    }
    
    return fmt.Errorf("job failed after %d attempts: %v", maxRetries+1, lastErr)
}
```

## ⚡ Performance Optimization

### Job Optimization Configuration
```tsk
# Job optimization configuration
cron_optimization: #cron("""
    parallel_execution -> true
    max_concurrent_jobs -> 5
    job_timeout -> 1h
    memory_limit -> 512MB
    cpu_limit -> 50%
    resource_monitoring -> true
""")
```

### Go Performance Implementation
```go
package optimization

import (
    "context"
    "runtime"
    "sync"
    "time"
)

// Job executor with resource limits
type JobExecutor struct {
    maxConcurrent int
    semaphore     chan struct{}
    mu            sync.Mutex
    activeJobs    map[string]context.CancelFunc
}

func NewJobExecutor(maxConcurrent int) *JobExecutor {
    return &JobExecutor{
        maxConcurrent: maxConcurrent,
        semaphore:     make(chan struct{}, maxConcurrent),
        activeJobs:    make(map[string]context.CancelFunc),
    }
}

func (je *JobExecutor) ExecuteJob(ctx context.Context, jobName string, job func(context.Context) error) error {
    // Acquire semaphore
    select {
    case je.semaphore <- struct{}{}:
    case <-ctx.Done():
        return ctx.Err()
    }
    defer func() { <-je.semaphore }()
    
    // Create job context with timeout
    jobCtx, cancel := context.WithTimeout(ctx, 1*time.Hour)
    defer cancel()
    
    // Register job
    je.mu.Lock()
    je.activeJobs[jobName] = cancel
    je.mu.Unlock()
    
    defer func() {
        je.mu.Lock()
        delete(je.activeJobs, jobName)
        je.mu.Unlock()
    }()
    
    // Monitor resources
    go je.monitorResources(jobCtx, jobName)
    
    // Execute job
    return job(jobCtx)
}

func (je *JobExecutor) monitorResources(ctx context.Context, jobName string) {
    ticker := time.NewTicker(30 * time.Second)
    defer ticker.Stop()
    
    for {
        select {
        case <-ctx.Done():
            return
        case <-ticker.C:
            var m runtime.MemStats
            runtime.ReadMemStats(&m)
            
            // Check memory usage
            if m.Alloc > 500*1024*1024 { // 500MB
                log.Printf("Job %s using too much memory: %d MB", jobName, m.Alloc/1024/1024)
            }
            
            // Check CPU usage
            if runtime.NumGoroutine() > 100 {
                log.Printf("Job %s creating too many goroutines: %d", jobName, runtime.NumGoroutine())
            }
        }
    }
}
```

## 🔧 Logging and Monitoring

### Logging Configuration
```tsk
# Logging configuration
cron_logging: #cron("""
    log_level -> info
    log_file -> /var/log/cron.log
    log_format -> json
    log_rotation -> daily
    log_retention -> 30d
    metrics_enabled -> true
    metrics_endpoint -> /metrics
    alerting_enabled -> true
""")
```

### Go Logging Implementation
```go
package logging

import (
    "encoding/json"
    "log"
    "os"
    "time"
)

// Structured log entry
type LogEntry struct {
    Timestamp time.Time              `json:"timestamp"`
    Level     string                 `json:"level"`
    Job       string                 `json:"job"`
    Message   string                 `json:"message"`
    Duration  time.Duration          `json:"duration,omitempty"`
    Error     string                 `json:"error,omitempty"`
    Metadata  map[string]interface{} `json:"metadata,omitempty"`
}

// Job logger
type JobLogger struct {
    file   *os.File
    logger *log.Logger
}

func NewJobLogger(logFile string) (*JobLogger, error) {
    file, err := os.OpenFile(logFile, os.O_APPEND|os.O_CREATE|os.O_WRONLY, 0644)
    if err != nil {
        return nil, err
    }
    
    return &JobLogger{
        file:   file,
        logger: log.New(file, "", 0),
    }, nil
}

func (jl *JobLogger) LogJob(jobName string, startTime time.Time, err error) {
    entry := LogEntry{
        Timestamp: time.Now(),
        Level:     "info",
        Job:       jobName,
        Duration:  time.Since(startTime),
    }
    
    if err != nil {
        entry.Level = "error"
        entry.Error = err.Error()
        entry.Message = "Job failed"
    } else {
        entry.Message = "Job completed successfully"
    }
    
    // Write JSON log entry
    if data, err := json.Marshal(entry); err == nil {
        jl.logger.Println(string(data))
    }
}

func (jl *JobLogger) Close() error {
    return jl.file.Close()
}
```

## 🎯 Real-World Example

### Complete Cron Configuration
```tsk
# cron-config.tsk - Complete cron configuration

# Environment configuration
environment: #env("ENVIRONMENT", "development")
timezone: #env("TIMEZONE", "UTC")

# Database configuration
database_url: #env("DATABASE_URL", "sqlite://cron.db")

# Job definitions
jobs: #cron("""
    # Data maintenance
    cleanup_old_data -> Clean up old data files
        schedule: "0 0 * * *"
        handler: handlers.CleanupOldData
        timeout: 30m
        retries: 3
        error_handler: handlers.CleanupErrorHandler
    
    # Database operations
    backup_database -> Create database backup
        schedule: "0 */6 * * *"
        handler: handlers.BackupDatabase
        timeout: 1h
        retries: 2
        error_handler: handlers.BackupErrorHandler
    
    # System monitoring
    check_health -> Check system health
        schedule: "*/15 * * * *"
        handler: handlers.CheckHealth
        timeout: 5m
        retries: 1
        error_handler: handlers.HealthErrorHandler
    
    # Report generation
    generate_reports -> Generate daily reports
        schedule: "0 2 * * *"
        handler: handlers.GenerateReports
        timeout: 2h
        retries: 3
        error_handler: handlers.ReportErrorHandler
    
    # Data processing
    process_analytics -> Process analytics data
        schedule: "0 3 * * *"
        handler: handlers.ProcessAnalytics
        timeout: 3h
        retries: 2
        error_handler: handlers.AnalyticsErrorHandler
    
    # Maintenance tasks
    system_maintenance -> System maintenance
        schedule: "0 4 * * 0"
        handler: handlers.SystemMaintenance
        timeout: 4h
        retries: 1
        error_handler: handlers.MaintenanceErrorHandler
""")

# Error handling configuration
error_handling: #cron("""
    max_retries -> 3
    retry_delay -> 5m
    exponential_backoff -> true
    alert_on_failure -> true
    alert_recipients -> admin@example.com, ops@example.com
    alert_channels -> email, slack
    failure_threshold -> 3
    circuit_breaker -> true
    graceful_shutdown -> true
""")

# Logging configuration
logging: #cron("""
    log_level -> info
    log_file -> /var/log/cron.log
    log_format -> json
    log_rotation -> daily
    log_retention -> 30d
    metrics_enabled -> true
    metrics_endpoint -> /metrics
    alerting_enabled -> true
    performance_monitoring -> true
""")

# Performance configuration
performance: #cron("""
    parallel_execution -> true
    max_concurrent_jobs -> 5
    job_timeout -> 1h
    memory_limit -> 512MB
    cpu_limit -> 50%
    resource_monitoring -> true
    load_balancing -> true
""")
```

### Go Cron Application Implementation
```go
package main

import (
    "fmt"
    "log"
    "os"
    "os/signal"
    "syscall"
    "github.com/tusklang/go-sdk"
)

type CronConfig struct {
    Environment   string `tsk:"environment"`
    Timezone      string `tsk:"timezone"`
    DatabaseURL   string `tsk:"database_url"`
    Jobs          string `tsk:"jobs"`
    ErrorHandling string `tsk:"error_handling"`
    Logging       string `tsk:"logging"`
    Performance   string `tsk:"performance"`
}

func main() {
    // Load cron configuration
    config := tusk.LoadConfig("cron-config.tsk")
    
    var cronConfig CronConfig
    if err := config.Unmarshal(&cronConfig); err != nil {
        log.Fatal("Failed to load cron config:", err)
    }
    
    // Create cron scheduler from directives
    scheduler := tusk.NewCronScheduler(cronConfig)
    
    // Setup database connection
    db := tusk.ConnectDatabase(cronConfig.DatabaseURL)
    
    // Register handlers with database context
    handlers.SetDatabase(db)
    
    // Setup logging
    logger, err := logging.NewJobLogger("/var/log/cron.log")
    if err != nil {
        log.Fatal("Failed to setup logging:", err)
    }
    defer logger.Close()
    
    // Start the cron scheduler
    scheduler.Start()
    
    log.Printf("Cron scheduler started in %s environment", cronConfig.Environment)
    
    // Wait for shutdown signal
    sigChan := make(chan os.Signal, 1)
    signal.Notify(sigChan, syscall.SIGINT, syscall.SIGTERM)
    
    <-sigChan
    log.Println("Shutting down cron scheduler...")
    
    // Graceful shutdown
    scheduler.Stop()
    log.Println("Cron scheduler stopped")
}
```

## 🎯 Best Practices

### 1. **Use Descriptive Job Names**
```tsk
# Good - descriptive job names
jobs: #cron("""
    daily_data_cleanup -> Clean up old data files
    hourly_database_backup -> Create database backup
    system_health_monitoring -> Check system health
""")

# Bad - unclear job names
jobs: #cron("""
    job1 -> Clean up old data files
    job2 -> Create database backup
    job3 -> Check system health
""")
```

### 2. **Implement Proper Error Handling**
```go
// Comprehensive error handling for jobs
func handleJobError(jobName string, err error) {
    log.Printf("Job %s failed: %v", jobName, err)
    
    // Send alert
    alert := Alert{
        Type:    "cron_failure",
        Job:     jobName,
        Error:   err.Error(),
        Time:    time.Now(),
    }
    
    if err := sendAlert(alert); err != nil {
        log.Printf("Failed to send alert: %v", err)
    }
    
    // Log to monitoring system
    if err := logToMonitoring(jobName, err); err != nil {
        log.Printf("Failed to log to monitoring: %v", err)
    }
}
```

### 3. **Use Environment-Specific Scheduling**
```tsk
# Different schedules for different environments
job_schedules: #if(
    #env("ENVIRONMENT") == "production",
    #cron("""
        backup -> "0 */4 * * *" -> handlers.BackupDatabase
        monitoring -> "*/5 * * * *" -> handlers.MonitorSystem
    """),
    #cron("""
        backup -> "0 */12 * * *" -> handlers.BackupDatabase
        monitoring -> "*/15 * * * *" -> handlers.MonitorSystem
    """)
)
```

### 4. **Monitor Job Performance**
```go
// Job performance monitoring
func monitorJobPerformance(jobName string, startTime time.Time) {
    duration := time.Since(startTime)
    
    // Log performance metrics
    metrics := map[string]interface{}{
        "job_name":  jobName,
        "duration":  duration.Seconds(),
        "timestamp": time.Now(),
    }
    
    if err := logMetrics(metrics); err != nil {
        log.Printf("Failed to log metrics: %v", err)
    }
    
    // Alert on slow jobs
    if duration > 30*time.Minute {
        sendAlert(Alert{
            Type:     "slow_job",
            Job:      jobName,
            Duration: duration,
            Time:     time.Now(),
        })
    }
}
```

## 🎯 Summary

Cron directives in TuskLang provide a powerful, declarative way to define scheduled tasks. They enable:

- **Declarative job definitions** that are easy to understand and maintain
- **Flexible scheduling** with conditional and dynamic timing
- **Built-in error handling** with retry strategies and alerts
- **Comprehensive logging** and monitoring capabilities
- **Performance optimization** with resource limits and parallel execution

The Go SDK seamlessly integrates cron directives with existing Go cron libraries, making them feel like native Go features while providing the power and flexibility of TuskLang's directive system.

**Next**: Explore middleware directives, auth directives, and other specialized directive types in the following guides. 