# 🔄 TuskLang Ruby Advanced Background Jobs Guide

**"We don't bow to any king" - Ruby Edition**

Master background job integration with TuskLang in Ruby. Learn job queues, scheduling, monitoring, and advanced patterns with Sidekiq, Resque, and Delayed Job.

## 🚀 Job Queue Configuration

### 1. Sidekiq Integration
```ruby
# config/sidekiq.tsk
[sidekiq]
enabled: true
concurrency: @env("SIDEKIQ_CONCURRENCY", 10)
queues: ["default", "high", "low", "critical"]
timeout: "30s"

[sidekiq_redis]
host: @env("REDIS_HOST", "localhost")
port: @env("REDIS_PORT", 6379)
db: @env("REDIS_DB", 0)
password: @env.secure("REDIS_PASSWORD")

[sidekiq_monitoring]
enabled: true
web_ui: true
web_ui_port: 8080
metrics: true

[job_queues]
default: {
    priority: 1
    concurrency: 5
    timeout: "30s"
}

high: {
    priority: 2
    concurrency: 3
    timeout: "60s"
}

low: {
    priority: 0
    concurrency: 2
    timeout: "300s"
}

critical: {
    priority: 3
    concurrency: 1
    timeout: "10s"
}
```

### 2. Resque Integration
```ruby
# config/resque.tsk
[resque]
enabled: true
concurrency: @env("RESQUE_CONCURRENCY", 5)
queues: ["default", "high", "low"]

[resque_redis]
host: @env("REDIS_HOST", "localhost")
port: @env("REDIS_PORT", 6379)
db: @env("REDIS_DB", 1)
password: @env.secure("REDIS_PASSWORD")

[resque_monitoring]
enabled: true
web_ui: true
web_ui_port: 8081
```

### 3. Delayed Job Integration
```ruby
# config/delayed_job.tsk
[delayed_job]
enabled: true
workers: @env("DELAYED_JOB_WORKERS", 2)
max_attempts: 3
max_run_time: "30s"

[delayed_job_database]
adapter: "postgresql"
host: @env("DATABASE_HOST", "localhost")
port: @env("DATABASE_PORT", 5432)
database: @env("DATABASE_NAME", "myapp")
username: @env("DATABASE_USER", "postgres")
password: @env.secure("DATABASE_PASSWORD")
```

## 📅 Job Scheduling

### 1. Cron-Style Scheduling
```ruby
# config/job_scheduling.tsk
[job_scheduling]
enabled: true

[scheduled_jobs]
daily_cleanup: {
    schedule: "0 2 * * *"
    job_class: "DailyCleanupJob"
    queue: "low"
    enabled: true
}

hourly_analytics: {
    schedule: "0 * * * *"
    job_class: "HourlyAnalyticsJob"
    queue: "high"
    enabled: true
}

weekly_report: {
    schedule: "0 9 * * 1"
    job_class: "WeeklyReportJob"
    queue: "default"
    enabled: true
}

monthly_backup: {
    schedule: "0 3 1 * *"
    job_class: "MonthlyBackupJob"
    queue: "low"
    enabled: true
}
```

### 2. Event-Driven Scheduling
```ruby
# config/event_scheduling.tsk
[event_scheduling]
enabled: true

[event_triggers]
user_registered: {
    jobs: ["WelcomeEmailJob", "AnalyticsJob"]
    delay: "5m"
    queue: "high"
}

order_created: {
    jobs: ["OrderConfirmationJob", "InventoryUpdateJob", "AnalyticsJob"]
    delay: "1m"
    queue: "default"
}

payment_processed: {
    jobs: ["PaymentConfirmationJob", "RevenueAnalyticsJob"]
    delay: "2m"
    queue: "high"
}

user_inactive: {
    jobs: ["ReminderEmailJob", "AccountCleanupJob"]
    delay: "7d"
    queue: "low"
}
```

## 📊 Job Monitoring

### 1. Job Metrics
```ruby
# config/job_metrics.tsk
[job_metrics]
enabled: true

[metrics]
job_count: @metrics("job_count", @job.count)
job_rate: @metrics("job_rate", @job.rate)
job_duration: @metrics("job_duration_ms", @job.duration)
job_failure_rate: @metrics("job_failure_rate", @job.failure_rate)
queue_length: @metrics("queue_length", @job.queue_length)

[queue_metrics]
default_queue_length: @metrics("default_queue_length", @job.queue_length("default"))
high_queue_length: @metrics("high_queue_length", @job.queue_length("high"))
low_queue_length: @metrics("low_queue_length", @job.queue_length("low"))
critical_queue_length: @metrics("critical_queue_length", @job.queue_length("critical"))

[job_patterns]
email_jobs: @metrics("email_jobs", @job.count_by_pattern(".*EmailJob"))
analytics_jobs: @metrics("analytics_jobs", @job.count_by_pattern(".*AnalyticsJob"))
cleanup_jobs: @metrics("cleanup_jobs", @job.count_by_pattern(".*CleanupJob"))
```

### 2. Job Alerts
```ruby
# config/job_alerts.tsk
[job_alerts]
enabled: true

[alerts]
high_failure_rate: @alert("job_failure_rate > 0.1", {
    severity: "critical",
    message: "Job failure rate is above 10%",
    notification: ["slack", "pagerduty"]
})

queue_backlog: @alert("default_queue_length > 1000 OR high_queue_length > 500", {
    severity: "warning",
    message: "Job queue backlog detected",
    notification: ["slack"]
})

job_timeout: @alert("job_duration_ms > 300000", {
    severity: "warning",
    message: "Jobs are timing out",
    notification: ["slack"]
})

worker_down: @alert("worker_count < 1", {
    severity: "critical",
    message: "No workers are running",
    notification: ["slack", "pagerduty"]
})
```

## 🛠️ Ruby Integration Example

### 1. Sidekiq Jobs
```ruby
# app/jobs/sidekiq_jobs.rb
class EmailJob
  include Sidekiq::Worker
  sidekiq_options queue: 'high', retry: 3

  def perform(user_id, email_type)
    config = TuskLang.config
    user = User.find(user_id)
    
    case email_type
    when 'welcome'
      UserMailer.welcome_email(user).deliver_now
    when 'reminder'
      UserMailer.reminder_email(user).deliver_now
    end
    
    # Log job completion
    AdvancedLoggingService.log_structured_event('email_sent', {
      user_id: user_id,
      email_type: email_type,
      job_id: jid
    })
  end
end

class AnalyticsJob
  include Sidekiq::Worker
  sidekiq_options queue: 'low', retry: 2

  def perform(data)
    config = TuskLang.config
    
    # Process analytics data
    AnalyticsService.process_data(data)
    
    # Update metrics
    MonitoringService.record_metric('analytics_processed', 1)
  end
end

class CleanupJob
  include Sidekiq::Worker
  sidekiq_options queue: 'low', retry: 1

  def perform
    config = TuskLang.config
    
    # Clean up old data
    User.where('last_login < ?', 30.days.ago).delete_all
    Order.where('created_at < ?', 1.year.ago).delete_all
    
    # Log cleanup
    AdvancedLoggingService.log_structured_event('cleanup_completed', {
      deleted_users: User.count,
      deleted_orders: Order.count
    })
  end
end
```

### 2. Job Service
```ruby
# app/services/job_service.rb
require 'tusklang'

class JobService
  def self.load_job_config
    parser = TuskLang.new
    parser.parse_file('config/background_jobs.tsk')
  end

  def self.enqueue_job(job_class, args = [], options = {})
    config = load_job_config
    
    if config['sidekiq']['enabled']
      queue = options[:queue] || 'default'
      queue_config = config['job_queues'][queue]
      
      if queue_config
        Sidekiq::Client.enqueue_to(queue, job_class, *args)
      else
        Sidekiq::Client.enqueue(job_class, *args)
      end
    end
  end

  def self.schedule_job(job_class, args = [], schedule_time = nil)
    config = load_job_config
    
    if config['sidekiq']['enabled']
      if schedule_time
        Sidekiq::Client.enqueue_in(schedule_time, job_class, *args)
      else
        Sidekiq::Client.enqueue(job_class, *args)
      end
    end
  end

  def self.trigger_event_jobs(event_name, data = {})
    config = load_job_config
    
    if config['event_scheduling']['enabled']
      event_config = config['event_scheduling']['event_triggers'][event_name]
      
      if event_config
        event_config['jobs'].each do |job_class|
          delay = event_config['delay']
          queue = event_config['queue']
          
          if delay
            schedule_job(job_class, [data], delay)
          else
            enqueue_job(job_class, [data], queue: queue)
          end
        end
      end
    end
  end

  def self.monitor_jobs
    config = load_job_config
    
    if config['job_metrics']['enabled']
      metrics = config['job_metrics']['metrics']
      
      metrics.each do |metric_name, metric_config|
        value = calculate_job_metric(metric_name, metric_config)
        MonitoringService.record_metric(metric_name, value)
      end
    end
  end

  def self.check_job_alerts
    config = load_job_config
    
    if config['job_alerts']['enabled']
      alerts = config['job_alerts']['alerts']
      
      alerts.each do |alert_name, alert_config|
        if alert_config['condition'].evaluate
          send_job_alert(alert_name, alert_config)
        end
      end
    end
  end

  private

  def self.calculate_job_metric(metric_name, metric_config)
    case metric_name
    when 'job_count'
      Sidekiq::Stats.new.processed
    when 'job_rate'
      Sidekiq::Stats.new.processed_per_second
    when 'job_failure_rate'
      stats = Sidekiq::Stats.new
      stats.failed.to_f / stats.processed
    when 'queue_length'
      Sidekiq::Queue.new.size
    end
  end

  def self.send_job_alert(alert_name, alert_config)
    message = alert_config['message']
    severity = alert_config['severity']
    notifications = alert_config['notification']
    
    notifications.each do |notification|
      case notification
      when 'slack'
        SlackNotifier.send(message, severity)
      when 'pagerduty'
        PagerDutyNotifier.send(message, severity)
      end
    end
  end
end

# Usage
config = JobService.load_job_config

# Enqueue jobs
JobService.enqueue_job(EmailJob, [123, 'welcome'], queue: 'high')
JobService.schedule_job(AnalyticsJob, [data], 5.minutes)

# Trigger event jobs
JobService.trigger_event_jobs('user_registered', { user_id: 123 })
JobService.trigger_event_jobs('order_created', { order_id: 456 })

# Monitor jobs
JobService.monitor_jobs
JobService.check_job_alerts
```

## 🛡️ Best Practices
- Use appropriate job queues for different priorities.
- Implement job monitoring and alerting.
- Use event-driven job scheduling for complex workflows.
- Monitor job performance and failure rates.
- Implement job retry and error handling.

**Ready to process jobs like a pro? Let's Tusk! 🚀** 