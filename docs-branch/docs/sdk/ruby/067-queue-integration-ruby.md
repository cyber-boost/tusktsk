# Queue Integration with TuskLang and Ruby

This guide covers integrating job queues with TuskLang and Ruby applications for background processing and task management.

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Basic Setup](#basic-setup)
4. [Job Implementation](#job-implementation)
5. [Queue Management](#queue-management)
6. [Advanced Features](#advanced-features)
7. [Performance Optimization](#performance-optimization)
8. [Testing](#testing)
9. [Deployment](#deployment)

## Overview

Job queues enable background processing and task management in applications. This guide shows how to integrate various queue systems with TuskLang and Ruby applications.

### Key Features

- **Multiple queue backends** (Sidekiq, Delayed Job, Resque)
- **Job scheduling** and retry mechanisms
- **Queue prioritization** and routing
- **Job monitoring** and metrics
- **Distributed processing** support
- **Error handling** and recovery

## Installation

### Dependencies

```ruby
# Gemfile
gem 'sidekiq'
gem 'redis'
gem 'connection_pool'
gem 'activejob'
```

### TuskLang Configuration

```tusk
# config/queue.tusk
queue:
  backend: "sidekiq"  # sidekiq, delayed_job, resque
  
  sidekiq:
    redis_url: "redis://localhost:6379/3"
    concurrency: 10
    queues:
      default: 1
      high: 5
      low: 1
      critical: 10
  
  delayed_job:
    max_attempts: 3
    delay_jobs: true
    sleep_delay: 5
  
  resque:
    redis_url: "redis://localhost:6379/4"
    namespace: "resque:tusk"
  
  jobs:
    retry_attempts: 3
    retry_delay: 60
    timeout: 300
    batch_size: 100
  
  monitoring:
    enabled: true
    metrics_port: 9090
    health_check_interval: 30
```

## Basic Setup

### Queue Manager

```ruby
# app/queue/queue_manager.rb
class QueueManager
  include Singleton

  def initialize
    @config = Rails.application.config.queue
    @backend = create_backend
  end

  def enqueue(job_class, *args, options = {})
    @backend.enqueue(job_class, *args, options)
  end

  def enqueue_at(timestamp, job_class, *args, options = {})
    @backend.enqueue_at(timestamp, job_class, *args, options)
  end

  def perform_async(job_class, *args, options = {})
    @backend.perform_async(job_class, *args, options)
  end

  def perform_in(delay, job_class, *args, options = {})
    @backend.perform_in(delay, job_class, *args, options)
  end

  def queue_size(queue_name)
    @backend.queue_size(queue_name)
  end

  def clear_queue(queue_name)
    @backend.clear_queue(queue_name)
  end

  def health_check
    @backend.health_check
  end

  private

  def create_backend
    case @config[:backend]
    when 'sidekiq'
      SidekiqBackend.new(@config[:sidekiq])
    when 'delayed_job'
      DelayedJobBackend.new(@config[:delayed_job])
    when 'resque'
      ResqueBackend.new(@config[:resque])
    else
      raise "Unsupported queue backend: #{@config[:backend]}"
    end
  end
end
```

### Base Job

```ruby
# app/jobs/base_job.rb
class BaseJob
  include ActiveJob::Base

  def self.perform_async(*args)
    QueueManager.instance.perform_async(self, *args)
  end

  def self.perform_in(delay, *args)
    QueueManager.instance.perform_in(delay, self, *args)
  end

  def self.perform_at(timestamp, *args)
    QueueManager.instance.enqueue_at(timestamp, self, *args)
  end

  protected

  def log_job_start
    Rails.logger.info "Starting job: #{self.class.name} with args: #{arguments}"
  end

  def log_job_complete
    Rails.logger.info "Completed job: #{self.class.name}"
  end

  def log_job_error(error)
    Rails.logger.error "Job error: #{self.class.name} - #{error.message}"
    Rails.logger.error error.backtrace.join("\n")
  end

  def track_job_metrics(action, duration = nil)
    return unless Rails.application.config.queue[:monitoring][:enabled]
    
    # Implementation would send metrics to monitoring system
    Rails.logger.debug "Job metric: #{self.class.name} - #{action} - #{duration}"
  end
end
```

## Job Implementation

### User Jobs

```ruby
# app/jobs/user_jobs/welcome_email_job.rb
class WelcomeEmailJob < BaseJob
  queue_as :high

  def perform(user_id)
    log_job_start
    start_time = Time.current

    begin
      user = User.find(user_id)
      UserMailer.welcome_email(user).deliver_now
      
      log_job_complete
      track_job_metrics(:completed, Time.current - start_time)
    rescue => e
      log_job_error(e)
      track_job_metrics(:error, Time.current - start_time)
      raise e
    end
  end
end

# app/jobs/user_jobs/password_reset_job.rb
class PasswordResetJob < BaseJob
  queue_as :high

  def perform(user_id, token)
    log_job_start
    start_time = Time.current

    begin
      user = User.find(user_id)
      UserMailer.password_reset_email(user, token).deliver_now
      
      log_job_complete
      track_job_metrics(:completed, Time.current - start_time)
    rescue => e
      log_job_error(e)
      track_job_metrics(:error, Time.current - start_time)
      raise e
    end
  end
end

# app/jobs/user_jobs/user_statistics_job.rb
class UserStatisticsJob < BaseJob
  queue_as :low

  def perform(user_id)
    log_job_start
    start_time = Time.current

    begin
      user = User.find(user_id)
      
      # Calculate user statistics
      statistics = {
        posts_count: user.posts.count,
        comments_count: user.comments.count,
        likes_count: user.likes.count,
        followers_count: user.followers.count,
        following_count: user.following.count
      }
      
      # Update user statistics
      user.update!(statistics: statistics)
      
      # Cache the statistics
      CacheService.instance.set_user_statistics(user, statistics)
      
      log_job_complete
      track_job_metrics(:completed, Time.current - start_time)
    rescue => e
      log_job_error(e)
      track_job_metrics(:error, Time.current - start_time)
      raise e
    end
  end
end
```

### Post Jobs

```ruby
# app/jobs/post_jobs/post_notification_job.rb
class PostNotificationJob < BaseJob
  queue_as :high

  def perform(post_id)
    log_job_start
    start_time = Time.current

    begin
      post = Post.find(post_id)
      
      # Notify followers
      post.user.followers.find_each do |follower|
        Notification.create!(
          user: follower,
          notifiable: post,
          action: 'new_post',
          data: {
            user_name: post.user.name,
            post_title: post.title
          }
        )
      end
      
      # Send email notifications
      post.user.followers.find_each do |follower|
        UserMailer.post_notification_email(follower, post).deliver_later
      end
      
      log_job_complete
      track_job_metrics(:completed, Time.current - start_time)
    rescue => e
      log_job_error(e)
      track_job_metrics(:error, Time.current - start_time)
      raise e
    end
  end
end

# app/jobs/post_jobs/post_indexing_job.rb
class PostIndexingJob < BaseJob
  queue_as :low

  def perform(post_id)
    log_job_start
    start_time = Time.current

    begin
      post = Post.find(post_id)
      
      # Index post for search
      SearchService.instance.index_post(post)
      
      # Update search cache
      CacheService.instance.invalidate_post_cache(post.id)
      
      log_job_complete
      track_job_metrics(:completed, Time.current - start_time)
    rescue => e
      log_job_error(e)
      track_job_metrics(:error, Time.current - start_time)
      raise e
    end
  end
end

# app/jobs/post_jobs/post_analytics_job.rb
class PostAnalyticsJob < BaseJob
  queue_as :low

  def perform(post_id)
    log_job_start
    start_time = Time.current

    begin
      post = Post.find(post_id)
      
      # Calculate post analytics
      analytics = {
        views_count: post.views.count,
        likes_count: post.likes.count,
        comments_count: post.comments.count,
        shares_count: post.shares.count,
        engagement_rate: calculate_engagement_rate(post)
      }
      
      # Update post analytics
      post.update!(analytics: analytics)
      
      # Cache analytics
      CacheService.instance.set_post_analytics(post, analytics)
      
      log_job_complete
      track_job_metrics(:completed, Time.current - start_time)
    rescue => e
      log_job_error(e)
      track_job_metrics(:error, Time.current - start_time)
      raise e
    end
  end

  private

  def calculate_engagement_rate(post)
    total_interactions = post.likes.count + post.comments.count + post.shares.count
    total_views = post.views.count
    
    return 0.0 if total_views == 0
    
    (total_interactions.to_f / total_views * 100).round(2)
  end
end
```

### Email Jobs

```ruby
# app/jobs/email_jobs/bulk_email_job.rb
class BulkEmailJob < BaseJob
  queue_as :low

  def perform(user_ids, template, data)
    log_job_start
    start_time = Time.current

    begin
      users = User.where(id: user_ids)
      
      users.find_each do |user|
        begin
          case template
          when 'newsletter'
            UserMailer.newsletter_email(user, data).deliver_now
          when 'announcement'
            UserMailer.announcement_email(user, data).deliver_now
          when 'promotion'
            UserMailer.promotion_email(user, data).deliver_now
          else
            Rails.logger.warn "Unknown email template: #{template}"
          end
        rescue => e
          Rails.logger.error "Failed to send email to user #{user.id}: #{e.message}"
        end
      end
      
      log_job_complete
      track_job_metrics(:completed, Time.current - start_time)
    rescue => e
      log_job_error(e)
      track_job_metrics(:error, Time.current - start_time)
      raise e
    end
  end
end

# app/jobs/email_jobs/email_cleanup_job.rb
class EmailCleanupJob < BaseJob
  queue_as :low

  def perform
    log_job_start
    start_time = Time.current

    begin
      # Clean up old email logs
      EmailLog.where('created_at < ?', 30.days.ago).delete_all
      
      # Clean up failed email attempts
      FailedEmail.where('created_at < ?', 7.days.ago).delete_all
      
      # Update email statistics
      update_email_statistics
      
      log_job_complete
      track_job_metrics(:completed, Time.current - start_time)
    rescue => e
      log_job_error(e)
      track_job_metrics(:error, Time.current - start_time)
      raise e
    end
  end

  private

  def update_email_statistics
    today = Date.current
    
    stats = {
      total_sent: EmailLog.where('DATE(created_at) = ?', today).count,
      total_failed: FailedEmail.where('DATE(created_at) = ?', today).count,
      delivery_rate: calculate_delivery_rate(today)
    }
    
    Rails.cache.write("email:stats:#{today}", stats, expires_in: 1.day)
  end

  def calculate_delivery_rate(date)
    total_sent = EmailLog.where('DATE(created_at) = ?', date).count
    total_failed = FailedEmail.where('DATE(created_at) = ?', date).count
    total_attempts = total_sent + total_failed
    
    return 100.0 if total_attempts == 0
    
    ((total_sent.to_f / total_attempts) * 100).round(2)
  end
end
```

### Data Processing Jobs

```ruby
# app/jobs/data_jobs/data_export_job.rb
class DataExportJob < BaseJob
  queue_as :low

  def perform(user_id, format, filters = {})
    log_job_start
    start_time = Time.current

    begin
      user = User.find(user_id)
      
      case format
      when 'csv'
        export_to_csv(user, filters)
      when 'json'
        export_to_json(user, filters)
      when 'pdf'
        export_to_pdf(user, filters)
      else
        raise "Unsupported export format: #{format}"
      end
      
      # Notify user when export is complete
      Notification.create!(
        user: user,
        action: 'data_export_complete',
        data: { format: format }
      )
      
      log_job_complete
      track_job_metrics(:completed, Time.current - start_time)
    rescue => e
      log_job_error(e)
      track_job_metrics(:error, Time.current - start_time)
      raise e
    end
  end

  private

  def export_to_csv(user, filters)
    filename = "export_#{user.id}_#{Time.current.to_i}.csv"
    file_path = Rails.root.join('tmp', 'exports', filename)
    
    FileUtils.mkdir_p(File.dirname(file_path))
    
    CSV.open(file_path, 'w') do |csv|
      csv << ['ID', 'Email', 'Name', 'Created At']
      
      user_data = get_user_data(user, filters)
      user_data.each do |data|
        csv << [data[:id], data[:email], data[:name], data[:created_at]]
      end
    end
    
    save_export_file(user, file_path, 'csv')
  end

  def export_to_json(user, filters)
    filename = "export_#{user.id}_#{Time.current.to_i}.json"
    file_path = Rails.root.join('tmp', 'exports', filename)
    
    FileUtils.mkdir_p(File.dirname(file_path))
    
    user_data = get_user_data(user, filters)
    File.write(file_path, user_data.to_json)
    
    save_export_file(user, file_path, 'json')
  end

  def export_to_pdf(user, filters)
    filename = "export_#{user.id}_#{Time.current.to_i}.pdf"
    file_path = Rails.root.join('tmp', 'exports', filename)
    
    FileUtils.mkdir_p(File.dirname(file_path))
    
    # Implementation would use a PDF generation library
    user_data = get_user_data(user, filters)
    generate_pdf(user_data, file_path)
    
    save_export_file(user, file_path, 'pdf')
  end

  def get_user_data(user, filters)
    # Get user data based on filters
    {
      id: user.id,
      email: user.email,
      name: user.name,
      created_at: user.created_at,
      posts: user.posts.limit(filters[:posts_limit] || 100).map do |post|
        {
          id: post.id,
          title: post.title,
          content: post.content,
          created_at: post.created_at
        }
      end
    }
  end

  def save_export_file(user, file_path, format)
    DataExport.create!(
      user: user,
      file_path: file_path.to_s,
      format: format,
      completed_at: Time.current
    )
  end

  def generate_pdf(data, file_path)
    # Implementation would use a PDF generation library like Prawn
    File.write(file_path, "PDF data for user #{data[:id]}")
  end
end

# app/jobs/data_jobs/data_cleanup_job.rb
class DataCleanupJob < BaseJob
  queue_as :low

  def perform
    log_job_start
    start_time = Time.current

    begin
      # Clean up old data exports
      cleanup_old_exports
      
      # Clean up old notifications
      cleanup_old_notifications
      
      # Clean up old logs
      cleanup_old_logs
      
      # Clean up old cache entries
      cleanup_old_cache
      
      log_job_complete
      track_job_metrics(:completed, Time.current - start_time)
    rescue => e
      log_job_error(e)
      track_job_metrics(:error, Time.current - start_time)
      raise e
    end
  end

  private

  def cleanup_old_exports
    # Delete exports older than 7 days
    DataExport.where('created_at < ?', 7.days.ago).find_each do |export|
      File.delete(export.file_path) if File.exist?(export.file_path)
      export.destroy
    end
  end

  def cleanup_old_notifications
    # Delete notifications older than 30 days
    Notification.where('created_at < ?', 30.days.ago).delete_all
  end

  def cleanup_old_logs
    # Delete logs older than 90 days
    LogEntry.where('created_at < ?', 90.days.ago).delete_all
  end

  def cleanup_old_cache
    # Clear old cache entries
    CacheManager.instance.clear("temp:*")
  end
end
```

## Queue Management

### Sidekiq Backend

```ruby
# app/queue/backends/sidekiq_backend.rb
class SidekiqBackend
  def initialize(config)
    @config = config
    Sidekiq.configure_server do |config|
      config.redis = { url: config[:redis_url] }
      config.concurrency = config[:concurrency]
    end
  end

  def enqueue(job_class, *args, options = {})
    queue = options[:queue] || 'default'
    Sidekiq::Client.enqueue_to(queue, job_class, *args)
  end

  def enqueue_at(timestamp, job_class, *args, options = {})
    Sidekiq::Client.enqueue_to_in(
      options[:queue] || 'default',
      timestamp.to_i - Time.current.to_i,
      job_class,
      *args
    )
  end

  def perform_async(job_class, *args, options = {})
    queue = options[:queue] || 'default'
    Sidekiq::Client.enqueue_to(queue, job_class, *args)
  end

  def perform_in(delay, job_class, *args, options = {})
    Sidekiq::Client.enqueue_to_in(
      options[:queue] || 'default',
      delay,
      job_class,
      *args
    )
  end

  def queue_size(queue_name)
    Sidekiq::Queue.new(queue_name).size
  end

  def clear_queue(queue_name)
    Sidekiq::Queue.new(queue_name).clear
  end

  def health_check
    begin
      Sidekiq.redis { |conn| conn.ping }
      { status: 'healthy' }
    rescue => e
      { status: 'error', error: e.message }
    end
  end
end
```

### Queue Monitoring

```ruby
# app/queue/monitoring/queue_monitor.rb
class QueueMonitor
  include Singleton

  def initialize
    @queue_manager = QueueManager.instance
    @config = Rails.application.config.queue
  end

  def monitor_queues
    queues = get_queue_names
    
    queues.each do |queue_name|
      size = @queue_manager.queue_size(queue_name)
      track_queue_metrics(queue_name, size)
    end
  end

  def get_queue_stats
    queues = get_queue_names
    
    queues.map do |queue_name|
      {
        name: queue_name,
        size: @queue_manager.queue_size(queue_name),
        status: queue_healthy?(queue_name) ? 'healthy' : 'unhealthy'
      }
    end
  end

  def health_check
    {
      status: 'healthy',
      queues: get_queue_stats,
      timestamp: Time.current.iso8601
    }
  end

  private

  def get_queue_names
    case @config[:backend]
    when 'sidekiq'
      @config[:sidekiq][:queues].keys
    when 'delayed_job'
      ['default']
    when 'resque'
      ['default', 'high', 'low']
    else
      ['default']
    end
  end

  def queue_healthy?(queue_name)
    size = @queue_manager.queue_size(queue_name)
    size < 1000 # Threshold for healthy queue
  end

  def track_queue_metrics(queue_name, size)
    return unless @config[:monitoring][:enabled]
    
    # Implementation would send metrics to monitoring system
    Rails.logger.debug "Queue metric: #{queue_name} - #{size} jobs"
  end
end
```

## Advanced Features

### Job Scheduling

```ruby
# app/queue/scheduling/job_scheduler.rb
class JobScheduler
  include Singleton

  def initialize
    @queue_manager = QueueManager.instance
  end

  def schedule_daily_tasks
    # Schedule daily cleanup
    schedule_job(DataCleanupJob, 1.day.from_now)
    
    # Schedule daily analytics
    schedule_job(DailyAnalyticsJob, 1.day.from_now)
    
    # Schedule daily email cleanup
    schedule_job(EmailCleanupJob, 1.day.from_now)
  end

  def schedule_weekly_tasks
    # Schedule weekly reports
    schedule_job(WeeklyReportJob, 1.week.from_now)
    
    # Schedule weekly backups
    schedule_job(WeeklyBackupJob, 1.week.from_now)
  end

  def schedule_monthly_tasks
    # Schedule monthly analytics
    schedule_job(MonthlyAnalyticsJob, 1.month.from_now)
    
    # Schedule monthly cleanup
    schedule_job(MonthlyCleanupJob, 1.month.from_now)
  end

  def schedule_user_tasks(user)
    # Schedule user statistics update
    schedule_job(UserStatisticsJob, 1.hour.from_now, user.id)
    
    # Schedule user analytics
    schedule_job(UserAnalyticsJob, 1.day.from_now, user.id)
  end

  def schedule_post_tasks(post)
    # Schedule post indexing
    schedule_job(PostIndexingJob, 5.minutes.from_now, post.id)
    
    # Schedule post analytics
    schedule_job(PostAnalyticsJob, 1.hour.from_now, post.id)
  end

  private

  def schedule_job(job_class, time, *args)
    @queue_manager.enqueue_at(time, job_class, *args)
  end
end
```

### Job Retry Handler

```ruby
# app/queue/retry/job_retry_handler.rb
class JobRetryHandler
  include Singleton

  def initialize
    @config = Rails.application.config.queue
  end

  def handle_retry(job_class, args, error, attempt)
    if attempt >= @config[:jobs][:retry_attempts]
      handle_final_failure(job_class, args, error)
    else
      schedule_retry(job_class, args, attempt)
    end
  end

  private

  def handle_final_failure(job_class, args, error)
    # Log final failure
    Rails.logger.error "Job failed permanently: #{job_class} - #{error.message}"
    
    # Store in failed jobs table
    FailedJob.create!(
      job_class: job_class.name,
      arguments: args,
      error_message: error.message,
      backtrace: error.backtrace.join("\n"),
      failed_at: Time.current
    )
    
    # Send alert
    send_failure_alert(job_class, args, error)
  end

  def schedule_retry(job_class, args, attempt)
    delay = calculate_retry_delay(attempt)
    
    Rails.logger.info "Scheduling retry for #{job_class} in #{delay} seconds (attempt #{attempt + 1})"
    
    QueueManager.instance.perform_in(delay, job_class, *args)
  end

  def calculate_retry_delay(attempt)
    base_delay = @config[:jobs][:retry_delay]
    base_delay * (2 ** attempt) # Exponential backoff
  end

  def send_failure_alert(job_class, args, error)
    # Implementation would send alert to monitoring system
    Rails.logger.warn "Job failure alert: #{job_class} - #{error.message}"
  end
end
```

## Performance Optimization

### Job Batching

```ruby
# app/queue/batching/job_batcher.rb
class JobBatcher
  include Singleton

  def initialize
    @queue_manager = QueueManager.instance
    @config = Rails.application.config.queue
  end

  def batch_enqueue(job_class, items, options = {})
    batch_size = options[:batch_size] || @config[:jobs][:batch_size]
    
    items.each_slice(batch_size) do |batch|
      @queue_manager.enqueue(job_class, batch, options)
    end
  end

  def batch_process(items, options = {}, &block)
    batch_size = options[:batch_size] || @config[:jobs][:batch_size]
    
    items.each_slice(batch_size) do |batch|
      batch.each do |item|
        begin
          block.call(item)
        rescue => e
          Rails.logger.error "Batch processing error: #{e.message}"
        end
      end
    end
  end
end
```

## Testing

### Queue Test Helper

```ruby
# spec/support/queue_helper.rb
module QueueHelper
  def clear_all_queues
    QueueManager.instance.clear_queue('default')
    QueueManager.instance.clear_queue('high')
    QueueManager.instance.clear_queue('low')
  end

  def expect_job_enqueued(job_class, queue = 'default')
    expect(QueueManager.instance.queue_size(queue)).to be > 0
  end

  def perform_enqueued_jobs
    # Implementation would perform all enqueued jobs
    # This is a simplified version
  end
end

RSpec.configure do |config|
  config.include QueueHelper, type: :queue
  
  config.before(:each, type: :queue) do
    clear_all_queues
  end
end
```

### Queue Tests

```ruby
# spec/queue/jobs/welcome_email_job_spec.rb
RSpec.describe WelcomeEmailJob, type: :queue do
  let(:user) { create(:user) }

  describe '#perform' do
    it 'sends welcome email' do
      expect {
        WelcomeEmailJob.perform_async(user.id)
        perform_enqueued_jobs
      }.to change { ActionMailer::Base.deliveries.count }.by(1)
    end
  end
end
```

## Deployment

### Production Configuration

```ruby
# config/environments/production.rb
Rails.application.configure do
  # Queue configuration
  config.queue = {
    backend: ENV['QUEUE_BACKEND'] || 'sidekiq',
    sidekiq: {
      redis_url: ENV['SIDEKIQ_REDIS_URL'] || 'redis://localhost:6379/3',
      concurrency: ENV['SIDEKIQ_CONCURRENCY'] || 10,
      queues: {
        default: ENV['SIDEKIQ_DEFAULT_QUEUE_WEIGHT'] || 1,
        high: ENV['SIDEKIQ_HIGH_QUEUE_WEIGHT'] || 5,
        low: ENV['SIDEKIQ_LOW_QUEUE_WEIGHT'] || 1,
        critical: ENV['SIDEKIQ_CRITICAL_QUEUE_WEIGHT'] || 10
      }
    },
    delayed_job: {
      max_attempts: ENV['DELAYED_JOB_MAX_ATTEMPTS'] || 3,
      delay_jobs: ENV['DELAYED_JOB_DELAY_JOBS'] != 'false',
      sleep_delay: ENV['DELAYED_JOB_SLEEP_DELAY'] || 5
    },
    resque: {
      redis_url: ENV['RESQUE_REDIS_URL'] || 'redis://localhost:6379/4',
      namespace: ENV['RESQUE_NAMESPACE'] || 'resque:tusk'
    },
    jobs: {
      retry_attempts: ENV['JOB_RETRY_ATTEMPTS'] || 3,
      retry_delay: ENV['JOB_RETRY_DELAY'] || 60,
      timeout: ENV['JOB_TIMEOUT'] || 300,
      batch_size: ENV['JOB_BATCH_SIZE'] || 100
    },
    monitoring: {
      enabled: ENV['QUEUE_MONITORING_ENABLED'] != 'false',
      metrics_port: ENV['QUEUE_METRICS_PORT'] || 9090,
      health_check_interval: ENV['QUEUE_HEALTH_CHECK_INTERVAL'] || 30
    }
  }
end
```

### Systemd Service

```ini
# /etc/systemd/system/sidekiq.service
[Unit]
Description=Sidekiq Worker
After=network.target

[Service]
Type=simple
User=deploy
WorkingDirectory=/var/www/tsk/sdk
Environment=RAILS_ENV=production
ExecStart=/usr/bin/bundle exec sidekiq
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
```

### Docker Configuration

```dockerfile
# Dockerfile.queue
FROM ruby:3.2-alpine

RUN apk add --no-cache \
    build-base \
    redis

WORKDIR /app

COPY Gemfile Gemfile.lock ./
RUN bundle install --jobs 4 --retry 3

COPY . .

CMD ["bundle", "exec", "sidekiq"]
```

```yaml
# docker-compose.queue.yml
version: '3.8'

services:
  sidekiq:
    build:
      context: .
      dockerfile: Dockerfile.queue
    environment:
      - RAILS_ENV=production
      - REDIS_URL=redis://redis:6379/3
      - SIDEKIQ_REDIS_URL=redis://redis:6379/3
    depends_on:
      - redis

  redis:
    image: redis:7-alpine
    volumes:
      - redis_data:/data

volumes:
  redis_data:
```

This comprehensive queue integration guide provides everything needed to build robust background job processing systems with TuskLang and Ruby, including multiple backend support, job scheduling, retry mechanisms, monitoring, testing, and deployment strategies. 