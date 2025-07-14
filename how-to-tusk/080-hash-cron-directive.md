# #cron - Scheduled Task Directive

The `#cron` directive creates scheduled tasks that run automatically at specified intervals, perfect for maintenance, cleanup, notifications, and recurring jobs.

## Basic Syntax

```tusk
# Run every minute
#cron "* * * * *" {
    @log("Task runs every minute")
}

# Run daily at midnight
#cron "0 0 * * *" {
    @cleanup_old_logs()
}

# Named cron job
#cron "0 2 * * *" name: "daily_backup" {
    @backup_database()
    @notify_admin("Backup completed")
}
```

## Cron Expression Format

```tusk
# Cron expression: "minute hour day month weekday"
# ┌───────────── minute (0-59)
# │ ┌───────────── hour (0-23)
# │ │ ┌───────────── day of month (1-31)
# │ │ │ ┌───────────── month (1-12)
# │ │ │ │ ┌───────────── day of week (0-7, Sunday=0 or 7)
# │ │ │ │ │
# * * * * *

# Examples:
#cron "0 0 * * *"      # Daily at midnight
#cron "*/5 * * * *"    # Every 5 minutes
#cron "0 */2 * * *"    # Every 2 hours
#cron "0 9 * * 1-5"    # Weekdays at 9 AM
#cron "0 0 1 * *"      # First day of month
#cron "0 0 * * 0"      # Every Sunday
```

## Common Schedules

```tusk
# Every minute
#cron "* * * * *" {
    @process_queue()
}

# Every 5 minutes
#cron "*/5 * * * *" {
    @check_health()
}

# Every hour on the hour
#cron "0 * * * *" {
    @generate_hourly_report()
}

# Every day at 3:30 AM
#cron "30 3 * * *" {
    @run_daily_maintenance()
}

# Every Monday at 9 AM
#cron "0 9 * * 1" {
    @send_weekly_newsletter()
}

# First day of month at noon
#cron "0 12 1 * *" {
    @generate_monthly_invoices()
}

# Every weekday at 6 PM
#cron "0 18 * * 1-5" {
    @send_daily_summary()
}

# Multiple times per day
#cron "0 9,12,15,18 * * *" {
    @sync_data()  # Runs at 9 AM, noon, 3 PM, and 6 PM
}
```

## Task Context

```tusk
# Access cron context
#cron "0 * * * *" name: "hourly_stats" {
    # Available context variables
    current_run: @cron.current_run      # Current execution time
    last_run: @cron.last_run           # Previous execution time
    next_run: @cron.next_run           # Next scheduled time
    job_name: @cron.name               # "hourly_stats"
    schedule: @cron.schedule           # "0 * * * *"
    
    @log("Job " + job_name + " started at " + current_run)
    
    # Calculate time since last run
    if (last_run) {
        duration: current_run - last_run
        @log("Time since last run: " + duration + " seconds")
    }
    
    # Do work
    @calculate_hourly_statistics()
}
```

## Preventing Overlaps

```tusk
# Single instance lock
#cron "*/5 * * * *" name: "data_sync" {
    # Acquire lock
    if (!@cron.lock()) {
        @log("Previous instance still running, skipping")
        return
    }
    
    try {
        # Long-running task
        @sync_large_dataset()
    } finally {
        # Always release lock
        @cron.unlock()
    }
}

# With timeout
#cron "0 * * * *" name: "hourly_process" {
    # Try to acquire lock with 5-minute timeout
    if (!@cron.lock(300)) {
        @alert("Hourly process stuck, could not acquire lock")
        return
    }
    
    try {
        @process_hourly_data()
    } finally {
        @cron.unlock()
    }
}

# Using Redis for distributed locking
#cron "*/10 * * * *" {
    lock_key: "cron:import_data"
    lock_ttl: 600  # 10 minutes
    
    if (!@redis.set(lock_key, @server_id, "NX", "EX", lock_ttl)) {
        @log("Another server is running this job")
        return
    }
    
    try {
        @import_external_data()
    } finally {
        @redis.del(lock_key)
    }
}
```

## Error Handling

```tusk
# Basic error handling
#cron "0 2 * * *" name: "daily_cleanup" {
    try {
        @cleanup_old_files()
        @optimize_database()
        @clear_cache()
        
        @log("Daily cleanup completed successfully")
        
    } catch (Exception e) {
        @log.error("Daily cleanup failed", {
            error: e.message,
            trace: e.trace
        })
        
        @notify_admin("Cron job failed: daily_cleanup", e)
    }
}

# Retry logic
#cron "*/30 * * * *" name: "api_sync" {
    max_retries: 3
    retry_count: 0
    
    while (retry_count < max_retries) {
        try {
            @sync_with_external_api()
            @log("API sync successful")
            break
            
        } catch (NetworkException e) {
            retry_count++
            @log.warning("API sync failed, attempt " + retry_count)
            
            if (retry_count < max_retries) {
                @sleep(60 * retry_count)  # Exponential backoff
            } else {
                @alert("API sync failed after " + max_retries + " attempts")
            }
        }
    }
}
```

## Database Maintenance

```tusk
# Daily database optimization
#cron "0 3 * * *" name: "db_maintenance" {
    @log("Starting database maintenance")
    
    # Clean old records
    deleted: @query("
        DELETE FROM logs 
        WHERE created_at < DATE_SUB(NOW(), INTERVAL 90 DAY)
    ").affected_rows()
    
    @log("Deleted " + deleted + " old log entries")
    
    # Optimize tables
    tables: ["users", "posts", "comments", "logs"]
    for (table in tables) {
        @query("OPTIMIZE TABLE " + table)
        @log("Optimized table: " + table)
    }
    
    # Update statistics
    @query("ANALYZE TABLE " + tables.join(", "))
    
    # Clean orphaned records
    @clean_orphaned_records()
    
    @log("Database maintenance completed")
}

# Backup database
#cron "0 4 * * *" name: "db_backup" {
    backup_file: "backup_" + @date("Y-m-d") + ".sql"
    
    # Create backup
    success: @exec("mysqldump -u {user} -p{pass} {db} > {file}", {
        user: @env.DB_USERNAME,
        pass: @env.DB_PASSWORD,
        db: @env.DB_DATABASE,
        file: @storage_path("backups/" + backup_file)
    })
    
    if (success) {
        # Compress backup
        @exec("gzip " + @storage_path("backups/" + backup_file))
        
        # Upload to S3
        @s3.upload(
            @storage_path("backups/" + backup_file + ".gz"),
            "backups/" + backup_file + ".gz"
        )
        
        # Keep only last 7 local backups
        @cleanup_old_backups(7)
        
        @log("Backup completed: " + backup_file)
    } else {
        @alert("Database backup failed!")
    }
}
```

## Reports and Notifications

```tusk
# Daily report generation
#cron "0 8 * * *" name: "daily_report" {
    yesterday: @date("Y-m-d", @strtotime("-1 day"))
    
    # Gather metrics
    metrics: {
        new_users: @User.whereDate("created_at", yesterday).count(),
        orders: @Order.whereDate("created_at", yesterday).count(),
        revenue: @Order.whereDate("created_at", yesterday).sum("total"),
        page_views: @Analytics.getPageViews(yesterday),
        bounce_rate: @Analytics.getBounceRate(yesterday)
    }
    
    # Generate report
    report_html: @render("emails/daily_report.tusk", {
        date: yesterday,
        metrics: metrics
    })
    
    # Send to admins
    admins: @User.where("role", "admin").get()
    for (admin in admins) {
        @email.send({
            to: admin.email,
            subject: "Daily Report - " + yesterday,
            html: report_html
        })
    }
    
    @log("Daily report sent to " + admins.length + " admins")
}

# Weekly digest
#cron "0 9 * * 1" name: "weekly_digest" {
    # Every Monday at 9 AM
    users: @User.where("receive_digest", true).get()
    
    for (user in users) {
        digest: @generate_user_digest(user, "week")
        
        @email.queue({
            to: user.email,
            subject: "Your Weekly Digest",
            template: "digest",
            data: digest
        })
    }
    
    @log("Queued " + users.length + " weekly digests")
}
```

## Cache Management

```tusk
# Clear expired cache
#cron "*/15 * * * *" name: "cache_cleanup" {
    # Clear expired cache entries
    expired: @cache.clear_expired()
    @log("Cleared " + expired + " expired cache entries")
    
    # Warm critical cache
    @cache.warm([
        "homepage_data",
        "navigation_menu",
        "featured_products"
    ])
}

# Rebuild cache
#cron "0 5 * * *" name: "cache_rebuild" {
    @log("Starting cache rebuild")
    
    # Clear all cache
    @cache.flush()
    
    # Rebuild essential caches
    caches: [
        {key: "categories", builder: @build_category_cache},
        {key: "products:featured", builder: @build_featured_products},
        {key: "stats:global", builder: @calculate_global_stats}
    ]
    
    for (cache in caches) {
        data: cache.builder()
        @cache.forever(cache.key, data)
        @log("Rebuilt cache: " + cache.key)
    }
}
```

## Queue Processing

```tusk
# Process failed jobs
#cron "0 * * * *" name: "retry_failed_jobs" {
    failed_jobs: @queue.failed()
    
    for (job in failed_jobs) {
        if (job.attempts < 3 && @should_retry(job)) {
            @queue.retry(job)
            @log("Retrying failed job: " + job.id)
        } else if (job.failed_at < @strtotime("-7 days")) {
            @queue.forget(job)
            @log("Removed old failed job: " + job.id)
        }
    }
}

# Monitor queue health
#cron "*/5 * * * *" name: "queue_monitor" {
    queues: ["default", "emails", "notifications"]
    
    for (queue_name in queues) {
        size: @queue.size(queue_name)
        
        if (size > 1000) {
            @alert("Queue " + queue_name + " has " + size + " pending jobs!")
        }
        
        # Start more workers if needed
        if (size > 500) {
            current_workers: @queue.workers(queue_name)
            if (current_workers < 5) {
                @scale_queue_workers(queue_name, 5)
            }
        }
    }
}
```

## System Monitoring

```tusk
# Health check
#cron "*/5 * * * *" name: "health_check" {
    checks: [
        {name: "Database", check: @check_database_connection},
        {name: "Redis", check: @check_redis_connection},
        {name: "Storage", check: @check_storage_space},
        {name: "Memory", check: @check_memory_usage},
        {name: "API", check: @check_external_api}
    ]
    
    failures: []
    
    for (service in checks) {
        if (!service.check()) {
            failures[] = service.name
            @log.error(service.name + " health check failed")
        }
    }
    
    if (failures.length > 0) {
        @alert("Health check failures: " + failures.join(", "))
        
        # Update status page
        @update_status_page(failures)
    }
}

# Disk space monitoring
#cron "0 * * * *" name: "disk_monitor" {
    threshold: 90  # Alert at 90% usage
    
    partitions: @get_disk_partitions()
    
    for (partition in partitions) {
        usage: @get_disk_usage(partition)
        
        if (usage.percent > threshold) {
            @alert("Disk space critical on " + partition, {
                used: usage.used_human,
                free: usage.free_human,
                percent: usage.percent
            })
            
            # Try to free space
            @cleanup_temp_files()
            @rotate_logs()
        }
    }
}
```

## Conditional Execution

```tusk
# Environment-specific crons
#cron "0 1 * * *" if: @env.APP_ENV == "production" {
    # Only runs in production
    @generate_analytics_report()
}

# Feature flag controlled
#cron "*/10 * * * *" if: @feature_enabled("auto_import") {
    @import_external_data()
}

# Dynamic scheduling
#cron "0 0 * * *" {
    # Check if should run today
    if (@is_holiday()) {
        @log("Skipping - today is a holiday")
        return
    }
    
    if (@is_weekend() && !@env.RUN_WEEKENDS) {
        @log("Skipping - weekend processing disabled")
        return
    }
    
    @process_daily_batch()
}
```

## Best Practices

1. **Use descriptive names** - Make cron jobs identifiable
2. **Add logging** - Track execution and results
3. **Prevent overlaps** - Use locking for long-running tasks
4. **Handle errors gracefully** - Don't let crons fail silently
5. **Monitor execution** - Track if crons are running as expected
6. **Set appropriate schedules** - Don't run more often than needed
7. **Consider timezones** - Crons run in server timezone
8. **Test thoroughly** - Cron errors can go unnoticed

## Related Topics

- `hash-cli-directive` - Command-line interface
- `queue-jobs` - Background job processing
- `task-scheduling` - Advanced scheduling
- `monitoring` - System monitoring
- `notifications` - Alert systems