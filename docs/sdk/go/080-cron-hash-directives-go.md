# Cron Hash Directives in TuskLang for Go

## Overview

Cron hash directives in TuskLang provide powerful scheduling capabilities directly in your configuration files. These directives enable you to define complex cron jobs, scheduled tasks, and automated workflows with Go integration for reliable task execution.

## Basic Cron Directive Syntax

```go
// TuskLang cron directive
#cron {
    jobs: {
        backup_database: {
            schedule: "0 2 * * *"
            command: "backup_command"
            description: "Daily database backup at 2 AM"
            enabled: true
        }
        
        cleanup_logs: {
            schedule: "0 3 * * 0"
            command: "cleanup_command"
            description: "Weekly log cleanup on Sundays"
            enabled: true
        }
        
        health_check: {
            schedule: "*/5 * * * *"
            command: "health_check_command"
            description: "Health check every 5 minutes"
            enabled: true
        }
    }
}
```

## Go Integration

```go
package main

import (
    "fmt"
    "log"
    "time"
    "github.com/robfig/cron/v3"
    "github.com/tusklang/go-sdk"
)

type CronDirective struct {
    Jobs map[string]Job `tsk:"jobs"`
}

type Job struct {
    Schedule    string `tsk:"schedule"`
    Command     string `tsk:"command"`
    Description string `tsk:"description"`
    Enabled     bool   `tsk:"enabled"`
    Timeout     int    `tsk:"timeout"`
    Retries     int    `tsk:"retries"`
}

type CronScheduler struct {
    cron    *cron.Cron
    jobs    map[string]Job
    handlers map[string]JobHandler
}

type JobHandler func() error

func main() {
    // Load cron configuration
    config, err := tusk.LoadFile("cron-config.tsk")
    if err != nil {
        log.Fatalf("Error loading cron config: %v", err)
    }
    
    var cronDirective CronDirective
    if err := config.Get("#cron", &cronDirective); err != nil {
        log.Fatalf("Error parsing cron directive: %v", err)
    }
    
    // Initialize cron scheduler
    scheduler := NewCronScheduler(cronDirective)
    
    // Register job handlers
    scheduler.RegisterHandler("backup_command", backupDatabase)
    scheduler.RegisterHandler("cleanup_command", cleanupLogs)
    scheduler.RegisterHandler("health_check_command", healthCheck)
    
    // Start scheduler
    scheduler.Start()
    
    // Keep the application running
    select {}
}

func NewCronScheduler(directive CronDirective) *CronScheduler {
    return &CronScheduler{
        cron:     cron.New(cron.WithSeconds()),
        jobs:     directive.Jobs,
        handlers: make(map[string]JobHandler),
    }
}

func (s *CronScheduler) RegisterHandler(command string, handler JobHandler) {
    s.handlers[command] = handler
}

func (s *CronScheduler) Start() {
    for jobName, job := range s.jobs {
        if !job.Enabled {
            continue
        }
        
        handler, exists := s.handlers[job.Command]
        if !exists {
            log.Printf("Warning: No handler registered for command %s", job.Command)
            continue
        }
        
        // Create job function with retry logic
        jobFunc := s.createJobFunction(jobName, job, handler)
        
        // Schedule the job
        entryID, err := s.cron.AddFunc(job.Schedule, jobFunc)
        if err != nil {
            log.Printf("Error scheduling job %s: %v", jobName, err)
            continue
        }
        
        log.Printf("Scheduled job %s (ID: %d) with schedule: %s", jobName, entryID, job.Schedule)
    }
    
    s.cron.Start()
    log.Println("Cron scheduler started")
}

func (s *CronScheduler) createJobFunction(jobName string, job Job, handler JobHandler) func() {
    return func() {
        log.Printf("Starting job: %s", jobName)
        startTime := time.Now()
        
        var lastErr error
        for attempt := 1; attempt <= job.Retries+1; attempt++ {
            if attempt > 1 {
                log.Printf("Retry attempt %d for job %s", attempt-1, jobName)
                time.Sleep(time.Duration(attempt) * time.Second)
            }
            
            // Execute job with timeout
            done := make(chan error, 1)
            go func() {
                done <- handler()
            }()
            
            select {
            case err := <-done:
                if err == nil {
                    duration := time.Since(startTime)
                    log.Printf("Job %s completed successfully in %v", jobName, duration)
                    return
                }
                lastErr = err
                log.Printf("Job %s failed (attempt %d): %v", jobName, attempt, err)
                
            case <-time.After(time.Duration(job.Timeout) * time.Second):
                lastErr = fmt.Errorf("job timed out after %d seconds", job.Timeout)
                log.Printf("Job %s timed out (attempt %d)", jobName, attempt)
            }
        }
        
        log.Printf("Job %s failed after %d attempts: %v", jobName, job.Retries+1, lastErr)
    }
}

// Job handler implementations
func backupDatabase() error {
    log.Println("Performing database backup...")
    // Implement backup logic here
    time.Sleep(2 * time.Second) // Simulate work
    return nil
}

func cleanupLogs() error {
    log.Println("Cleaning up old log files...")
    // Implement cleanup logic here
    time.Sleep(1 * time.Second) // Simulate work
    return nil
}

func healthCheck() error {
    log.Println("Performing health check...")
    // Implement health check logic here
    time.Sleep(500 * time.Millisecond) // Simulate work
    return nil
}
```

## Advanced Cron Features

### Complex Scheduling Patterns

```go
// TuskLang configuration with complex schedules
#cron {
    jobs: {
        // Business hours only (9 AM - 5 PM, Monday-Friday)
        business_metrics: {
            schedule: "0 */30 9-17 * * 1-5"
            command: "collect_business_metrics"
            description: "Collect business metrics every 30 minutes during business hours"
            enabled: true
        }
        
        // Weekend maintenance
        weekend_maintenance: {
            schedule: "0 2 * * 6,0"
            command: "weekend_maintenance"
            description: "Weekend maintenance at 2 AM"
            enabled: true
        }
        
        // Quarterly reports
        quarterly_report: {
            schedule: "0 9 1 1,4,7,10 *"
            command: "generate_quarterly_report"
            description: "Generate quarterly reports on first day of quarter"
            enabled: true
        }
        
        // End of month processing
        month_end: {
            schedule: "0 23 L * *"
            command: "month_end_processing"
            description: "Month-end processing on last day of month"
            enabled: true
        }
    }
}
```

### Conditional Job Execution

```go
// TuskLang configuration with conditional execution
#cron {
    jobs: {
        conditional_backup: {
            schedule: "0 2 * * *"
            command: "conditional_backup"
            description: "Conditional backup based on system state"
            enabled: true
            conditions: {
                disk_space: ">10GB"
                system_load: "<80%"
                last_backup: ">24h"
            }
        }
        
        smart_cleanup: {
            schedule: "0 3 * * *"
            command: "smart_cleanup"
            description: "Smart cleanup based on usage patterns"
            enabled: true
            conditions: {
                log_size: ">100MB"
                error_rate: "<5%"
            }
        }
    }
}
```

### Job Dependencies and Chaining

```go
// TuskLang configuration with job dependencies
#cron {
    jobs: {
        data_extraction: {
            schedule: "0 1 * * *"
            command: "extract_data"
            description: "Extract data from external sources"
            enabled: true
        }
        
        data_processing: {
            schedule: "0 2 * * *"
            command: "process_data"
            description: "Process extracted data"
            enabled: true
            depends_on: ["data_extraction"]
        }
        
        data_analysis: {
            schedule: "0 3 * * *"
            command: "analyze_data"
            description: "Analyze processed data"
            enabled: true
            depends_on: ["data_processing"]
        }
        
        report_generation: {
            schedule: "0 4 * * *"
            command: "generate_report"
            description: "Generate daily report"
            enabled: true
            depends_on: ["data_analysis"]
        }
    }
}
```

## Error Handling and Monitoring

```go
type JobResult struct {
    JobName     string
    StartTime   time.Time
    EndTime     time.Time
    Duration    time.Duration
    Success     bool
    Error       error
    Attempts    int
    Retries     int
}

type CronScheduler struct {
    cron        *cron.Cron
    jobs        map[string]Job
    handlers    map[string]JobHandler
    results     chan JobResult
    metrics     *JobMetrics
}

type JobMetrics struct {
    TotalJobs     int64
    SuccessfulJobs int64
    FailedJobs    int64
    TotalDuration time.Duration
}

func (s *CronScheduler) createJobFunction(jobName string, job Job, handler JobHandler) func() {
    return func() {
        result := JobResult{
            JobName:   jobName,
            StartTime: time.Now(),
        }
        
        defer func() {
            result.EndTime = time.Now()
            result.Duration = result.EndTime.Sub(result.StartTime)
            s.results <- result
            s.updateMetrics(result)
        }()
        
        // Execute with retry logic
        var lastErr error
        for attempt := 1; attempt <= job.Retries+1; attempt++ {
            result.Attempts = attempt
            
            if err := s.executeWithTimeout(handler, job.Timeout); err != nil {
                lastErr = err
                result.Retries++
                
                if attempt <= job.Retries {
                    time.Sleep(time.Duration(attempt) * time.Second)
                    continue
                }
            } else {
                result.Success = true
                return
            }
        }
        
        result.Error = lastErr
    }
}

func (s *CronScheduler) executeWithTimeout(handler JobHandler, timeout int) error {
    done := make(chan error, 1)
    go func() {
        done <- handler()
    }()
    
    select {
    case err := <-done:
        return err
    case <-time.After(time.Duration(timeout) * time.Second):
        return fmt.Errorf("job timed out after %d seconds", timeout)
    }
}

func (s *CronScheduler) updateMetrics(result JobResult) {
    atomic.AddInt64(&s.metrics.TotalJobs, 1)
    
    if result.Success {
        atomic.AddInt64(&s.metrics.SuccessfulJobs, 1)
    } else {
        atomic.AddInt64(&s.metrics.FailedJobs, 1)
    }
    
    // Update total duration (simplified)
    s.metrics.TotalDuration += result.Duration
}

func (s *CronScheduler) StartMonitoring() {
    go func() {
        for result := range s.results {
            if !result.Success {
                log.Printf("Job %s failed after %d attempts: %v", 
                    result.JobName, result.Attempts, result.Error)
                
                // Send alert or notification
                s.sendAlert(result)
            } else {
                log.Printf("Job %s completed successfully in %v", 
                    result.JobName, result.Duration)
            }
        }
    }()
}

func (s *CronScheduler) sendAlert(result JobResult) {
    // Implement alert logic (email, Slack, etc.)
    log.Printf("ALERT: Job %s failed - %v", result.JobName, result.Error)
}
```

## Performance Considerations

- **Concurrent Execution**: Use goroutines for job execution to avoid blocking
- **Resource Management**: Implement proper cleanup and resource limits
- **Memory Optimization**: Avoid memory leaks in long-running jobs
- **Database Connections**: Use connection pooling for database operations
- **File Handling**: Implement proper file cleanup and error handling

## Security Notes

- **Job Isolation**: Run jobs in isolated environments when possible
- **Permission Management**: Use minimal required permissions for job execution
- **Input Validation**: Validate all job inputs and parameters
- **Audit Logging**: Log all job executions and results for security auditing
- **Secret Management**: Use secure methods for handling sensitive job data

## Best Practices

1. **Idempotent Jobs**: Design jobs to be safely re-runnable
2. **Error Recovery**: Implement proper error handling and recovery mechanisms
3. **Monitoring**: Set up comprehensive monitoring and alerting
4. **Documentation**: Document job purposes, schedules, and dependencies
5. **Testing**: Test job logic thoroughly before deployment
6. **Backup Strategies**: Implement backup and recovery for critical jobs

## Integration Examples

### With Prometheus Metrics

```go
import (
    "github.com/prometheus/client_golang/prometheus"
    "github.com/prometheus/client_golang/prometheus/promauto"
)

var (
    jobDuration = promauto.NewHistogramVec(prometheus.HistogramOpts{
        Name: "cron_job_duration_seconds",
        Help: "Duration of cron job execution",
    }, []string{"job_name"})
    
    jobSuccess = promauto.NewCounterVec(prometheus.CounterOpts{
        Name: "cron_job_success_total",
        Help: "Total number of successful cron job executions",
    }, []string{"job_name"})
    
    jobFailures = promauto.NewCounterVec(prometheus.CounterOpts{
        Name: "cron_job_failures_total",
        Help: "Total number of failed cron job executions",
    }, []string{"job_name"})
)

func (s *CronScheduler) recordMetrics(result JobResult) {
    jobDuration.WithLabelValues(result.JobName).Observe(result.Duration.Seconds())
    
    if result.Success {
        jobSuccess.WithLabelValues(result.JobName).Inc()
    } else {
        jobFailures.WithLabelValues(result.JobName).Inc()
    }
}
```

### With Database Job Queue

```go
type DatabaseJobQueue struct {
    db *sql.DB
}

func (q *DatabaseJobQueue) EnqueueJob(job Job) error {
    query := `
        INSERT INTO job_queue (command, schedule, description, created_at)
        VALUES (?, ?, ?, NOW())
    `
    _, err := q.db.Exec(query, job.Command, job.Schedule, job.Description)
    return err
}

func (q *DatabaseJobQueue) GetPendingJobs() ([]Job, error) {
    query := `
        SELECT command, schedule, description 
        FROM job_queue 
        WHERE status = 'pending' 
        AND next_run <= NOW()
    `
    rows, err := q.db.Query(query)
    if err != nil {
        return nil, err
    }
    defer rows.Close()
    
    var jobs []Job
    for rows.Next() {
        var job Job
        err := rows.Scan(&job.Command, &job.Schedule, &job.Description)
        if err != nil {
            return nil, err
        }
        jobs = append(jobs, job)
    }
    
    return jobs, nil
}
```

This comprehensive cron hash directives documentation provides Go developers with everything they need to build sophisticated scheduling systems using TuskLang's powerful directive system. 