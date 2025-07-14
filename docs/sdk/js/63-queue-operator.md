# @queue Operator - Message Queue Management

## Overview
The `@queue` operator provides message queue functionality in TuskLang configurations, enabling asynchronous processing, job scheduling, and distributed task management.

## TuskLang Syntax

### Basic Queue Configuration
```tusk
# Simple message queue
message_queue: {
  name: @queue.create("email_queue")
  type: "fifo"
  max_size: 10000
  ttl: "24h"
}

# Priority queue for urgent tasks
priority_queue: {
  name: @queue.create("urgent_tasks")
  type: "priority"
  priorities: ["high", "medium", "low"]
  max_size: 5000
}
```

### Queue with Workers
```tusk
# Job processing queue
job_queue: {
  name: @queue.create("background_jobs")
  
  workers: {
    count: @queue.workers(5)
    concurrency: 3
    timeout: "30s"
  }
  
  processing: {
    retry_attempts: 3
    retry_delay: "5s"
    dead_letter_queue: "failed_jobs"
  }
}
```

### Queue with Scheduling
```tusk
# Scheduled task queue
scheduled_queue: {
  name: @queue.create("scheduled_tasks")
  
  scheduling: {
    cron_pattern: @queue.cron("0 */5 * * * *")  # Every 5 minutes
    timezone: "UTC"
    max_execution_time: "10m"
  }
  
  tasks: [
    @queue.schedule("cleanup_temp_files", "0 2 * * *"),
    @queue.schedule("backup_database", "0 3 * * *"),
    @queue.schedule("send_reports", "0 9 * * 1-5")
  ]
}
```

## JavaScript Integration

### Node.js Queue Implementation
```javascript
const Queue = require('bull');
const tusklang = require('@tusklang/core');

// TuskLang configuration
const config = tusklang.parse(`
queue_config: {
  name: @queue.create("email_processing")
  
  redis: {
    host: @env("REDIS_HOST", "localhost")
    port: @env("REDIS_PORT", 6379)
    password: @env("REDIS_PASSWORD")
  }
  
  workers: {
    count: @queue.workers(3)
    concurrency: 2
    timeout: "60s"
  }
  
  processing: {
    retry_attempts: 3
    retry_delay: "10s"
    backoff: "exponential"
  }
}
`);

class QueueManager {
  constructor(config) {
    this.config = config.queue_config;
    this.queues = new Map();
    this.workers = new Map();
    this.initializeQueues();
  }
  
  async initializeQueues() {
    try {
      // Create main queue
      const mainQueue = new Queue(this.config.name, {
        redis: this.config.redis,
        defaultJobOptions: {
          attempts: this.config.processing.retry_attempts,
          backoff: {
            type: this.config.processing.backoff,
            delay: this.config.processing.retry_delay * 1000
          },
          removeOnComplete: 100,
          removeOnFail: 50
        }
      });
      
      this.queues.set('main', mainQueue);
      
      // Create dead letter queue
      const deadLetterQueue = new Queue(`${this.config.name}_dead_letter`, {
        redis: this.config.redis
      });
      
      this.queues.set('dead_letter', deadLetterQueue);
      
      // Setup event handlers
      this.setupEventHandlers(mainQueue);
      
      // Start workers
      this.startWorkers();
      
    } catch (error) {
      console.error('Queue initialization error:', error);
      throw error;
    }
  }
  
  setupEventHandlers(queue) {
    queue.on('completed', (job, result) => {
      console.log(`Job ${job.id} completed successfully`);
      this.logJobCompletion(job, result);
    });
    
    queue.on('failed', (job, err) => {
      console.error(`Job ${job.id} failed:`, err.message);
      this.handleJobFailure(job, err);
    });
    
    queue.on('stalled', (job) => {
      console.warn(`Job ${job.id} stalled, retrying...`);
      job.retry();
    });
    
    queue.on('error', (error) => {
      console.error('Queue error:', error);
      this.handleQueueError(error);
    });
  }
  
  async startWorkers() {
    const mainQueue = this.queues.get('main');
    
    // Process email jobs
    mainQueue.process('send_email', this.config.workers.concurrency, async (job) => {
      return await this.processEmailJob(job);
    });
    
    // Process notification jobs
    mainQueue.process('send_notification', this.config.workers.concurrency, async (job) => {
      return await this.processNotificationJob(job);
    });
    
    // Process report generation jobs
    mainQueue.process('generate_report', this.config.workers.concurrency, async (job) => {
      return await this.processReportJob(job);
    });
  }
  
  async processEmailJob(job) {
    const { to, subject, body, template } = job.data;
    
    try {
      // Validate email data
      this.validateEmailData(job.data);
      
      // Send email
      const result = await this.sendEmail(to, subject, body, template);
      
      // Log success
      await this.logEmailSent(to, subject, result);
      
      return { success: true, messageId: result.messageId };
      
    } catch (error) {
      console.error('Email job failed:', error);
      throw error;
    }
  }
  
  async processNotificationJob(job) {
    const { userId, type, message, channels } = job.data;
    
    try {
      const results = {};
      
      // Send to different channels
      for (const channel of channels) {
        switch (channel) {
          case 'email':
            results.email = await this.sendEmailNotification(userId, message);
            break;
          case 'sms':
            results.sms = await this.sendSMSNotification(userId, message);
            break;
          case 'push':
            results.push = await this.sendPushNotification(userId, message);
            break;
        }
      }
      
      return { success: true, results };
      
    } catch (error) {
      console.error('Notification job failed:', error);
      throw error;
    }
  }
  
  async processReportJob(job) {
    const { reportType, dateRange, format } = job.data;
    
    try {
      // Generate report
      const report = await this.generateReport(reportType, dateRange);
      
      // Convert to requested format
      const formattedReport = await this.formatReport(report, format);
      
      // Store report
      const reportId = await this.storeReport(formattedReport, reportType);
      
      return { success: true, reportId, size: formattedReport.length };
      
    } catch (error) {
      console.error('Report generation failed:', error);
      throw error;
    }
  }
  
  async addJob(jobType, data, options = {}) {
    const queue = this.queues.get('main');
    
    const jobOptions = {
      priority: options.priority || 0,
      delay: options.delay || 0,
      attempts: options.attempts || this.config.processing.retry_attempts,
      ...options
    };
    
    const job = await queue.add(jobType, data, jobOptions);
    console.log(`Added job ${job.id} of type ${jobType}`);
    
    return job;
  }
  
  async addScheduledJob(jobType, data, schedule) {
    const queue = this.queues.get('main');
    
    const job = await queue.add(jobType, data, {
      repeat: {
        cron: schedule
      }
    });
    
    console.log(`Added scheduled job ${job.id} with schedule ${schedule}`);
    return job;
  }
  
  validateEmailData(data) {
    if (!data.to || !data.subject || !data.body) {
      throw new Error('Missing required email fields');
    }
    
    if (!this.isValidEmail(data.to)) {
      throw new Error('Invalid email address');
    }
  }
  
  isValidEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }
  
  async sendEmail(to, subject, body, template) {
    // Implementation for sending email
    // This would integrate with your email service (SendGrid, AWS SES, etc.)
    console.log(`Sending email to ${to}: ${subject}`);
    
    // Simulate email sending
    await new Promise(resolve => setTimeout(resolve, 1000));
    
    return { messageId: `msg_${Date.now()}` };
  }
  
  async handleJobFailure(job, error) {
    // Log failure
    await this.logJobFailure(job, error);
    
    // Move to dead letter queue if max attempts reached
    if (job.attemptsMade >= job.opts.attempts) {
      await this.moveToDeadLetter(job, error);
    }
  }
  
  async moveToDeadLetter(job, error) {
    const deadLetterQueue = this.queues.get('dead_letter');
    
    await deadLetterQueue.add('failed_job', {
      originalJob: job.data,
      error: error.message,
      attempts: job.attemptsMade,
      failedAt: new Date()
    });
    
    console.log(`Moved job ${job.id} to dead letter queue`);
  }
  
  async getQueueStats() {
    const mainQueue = this.queues.get('main');
    
    const [waiting, active, completed, failed] = await Promise.all([
      mainQueue.getWaiting(),
      mainQueue.getActive(),
      mainQueue.getCompleted(),
      mainQueue.getFailed()
    ]);
    
    return {
      waiting: waiting.length,
      active: active.length,
      completed: completed.length,
      failed: failed.length,
      total: waiting.length + active.length + completed.length + failed.length
    };
  }
  
  async pauseQueue() {
    const mainQueue = this.queues.get('main');
    await mainQueue.pause();
    console.log('Queue paused');
  }
  
  async resumeQueue() {
    const mainQueue = this.queues.get('main');
    await mainQueue.resume();
    console.log('Queue resumed');
  }
  
  async cleanQueue() {
    const mainQueue = this.queues.get('main');
    await mainQueue.clean(24 * 60 * 60 * 1000, 'completed'); // Clean completed jobs older than 24 hours
    await mainQueue.clean(7 * 24 * 60 * 60 * 1000, 'failed'); // Clean failed jobs older than 7 days
    console.log('Queue cleaned');
  }
}

// Usage
const queueManager = new QueueManager(config);

// Add jobs
await queueManager.addJob('send_email', {
  to: 'user@example.com',
  subject: 'Welcome!',
  body: 'Welcome to our platform!',
  template: 'welcome'
});

await queueManager.addScheduledJob('generate_report', {
  reportType: 'daily',
  dateRange: 'yesterday',
  format: 'pdf'
}, '0 6 * * *'); // Daily at 6 AM
```

### Browser Queue Implementation
```javascript
// Browser-based queue for offline processing
const browserConfig = tusklang.parse(`
browser_queue: {
  name: @queue.create("offline_tasks")
  
  storage: {
    type: "indexeddb"
    database: "task_queue"
    max_size: "50MB"
  }
  
  processing: {
    retry_attempts: 3
    retry_delay: "5s"
    batch_size: 10
  }
}
`);

class BrowserQueueManager {
  constructor(config) {
    this.config = config.browser_queue;
    this.db = null;
    this.isProcessing = false;
    this.initializeDatabase();
  }
  
  async initializeDatabase() {
    try {
      this.db = await this.openDatabase();
      await this.createObjectStore();
      console.log('Browser queue database initialized');
    } catch (error) {
      console.error('Database initialization failed:', error);
    }
  }
  
  openDatabase() {
    return new Promise((resolve, reject) => {
      const request = indexedDB.open(this.config.storage.database, 1);
      
      request.onerror = () => reject(request.error);
      request.onsuccess = () => resolve(request.result);
      
      request.onupgradeneeded = (event) => {
        const db = event.target.result;
        if (!db.objectStoreNames.contains('tasks')) {
          db.createObjectStore('tasks', { keyPath: 'id', autoIncrement: true });
        }
      };
    });
  }
  
  async addTask(taskType, data, options = {}) {
    const task = {
      type: taskType,
      data: data,
      status: 'pending',
      priority: options.priority || 0,
      createdAt: Date.now(),
      attempts: 0,
      maxAttempts: this.config.processing.retry_attempts,
      ...options
    };
    
    const transaction = this.db.transaction(['tasks'], 'readwrite');
    const store = transaction.objectStore('tasks');
    
    await new Promise((resolve, reject) => {
      const request = store.add(task);
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(request.error);
    });
    
    console.log(`Added task ${task.id} of type ${taskType}`);
    
    // Start processing if not already running
    if (!this.isProcessing) {
      this.startProcessing();
    }
    
    return task;
  }
  
  async startProcessing() {
    if (this.isProcessing) return;
    
    this.isProcessing = true;
    console.log('Starting task processing');
    
    while (this.isProcessing) {
      try {
        const tasks = await this.getPendingTasks(this.config.processing.batch_size);
        
        if (tasks.length === 0) {
          // No tasks to process, wait a bit
          await new Promise(resolve => setTimeout(resolve, 5000));
          continue;
        }
        
        // Process tasks in parallel
        await Promise.all(tasks.map(task => this.processTask(task)));
        
      } catch (error) {
        console.error('Task processing error:', error);
        await new Promise(resolve => setTimeout(resolve, 1000));
      }
    }
  }
  
  async getPendingTasks(limit) {
    const transaction = this.db.transaction(['tasks'], 'readonly');
    const store = transaction.objectStore('tasks');
    const index = store.index('status');
    
    return new Promise((resolve, reject) => {
      const request = index.getAll('pending', limit);
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(request.error);
    });
  }
  
  async processTask(task) {
    try {
      // Update task status
      await this.updateTaskStatus(task.id, 'processing');
      
      // Process based on task type
      let result;
      switch (task.type) {
        case 'sync_data':
          result = await this.syncData(task.data);
          break;
        case 'upload_file':
          result = await this.uploadFile(task.data);
          break;
        case 'send_analytics':
          result = await this.sendAnalytics(task.data);
          break;
        default:
          throw new Error(`Unknown task type: ${task.type}`);
      }
      
      // Mark as completed
      await this.updateTaskStatus(task.id, 'completed', result);
      
    } catch (error) {
      console.error(`Task ${task.id} failed:`, error);
      
      // Increment attempt count
      task.attempts++;
      
      if (task.attempts >= task.maxAttempts) {
        await this.updateTaskStatus(task.id, 'failed', error.message);
      } else {
        // Retry later
        await this.updateTaskStatus(task.id, 'pending');
        await this.delayTask(task.id, this.config.processing.retry_delay * 1000);
      }
    }
  }
  
  async syncData(data) {
    // Sync data when online
    if (navigator.onLine) {
      const response = await fetch('/api/sync', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
      });
      
      if (!response.ok) {
        throw new Error(`Sync failed: ${response.statusText}`);
      }
      
      return await response.json();
    } else {
      throw new Error('Offline - cannot sync data');
    }
  }
  
  async uploadFile(data) {
    if (navigator.onLine) {
      const formData = new FormData();
      formData.append('file', data.file);
      formData.append('metadata', JSON.stringify(data.metadata));
      
      const response = await fetch('/api/upload', {
        method: 'POST',
        body: formData
      });
      
      if (!response.ok) {
        throw new Error(`Upload failed: ${response.statusText}`);
      }
      
      return await response.json();
    } else {
      throw new Error('Offline - cannot upload file');
    }
  }
  
  async sendAnalytics(data) {
    if (navigator.onLine) {
      const response = await fetch('/api/analytics', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
      });
      
      if (!response.ok) {
        throw new Error(`Analytics failed: ${response.statusText}`);
      }
      
      return await response.json();
    } else {
      throw new Error('Offline - cannot send analytics');
    }
  }
  
  async updateTaskStatus(taskId, status, result = null) {
    const transaction = this.db.transaction(['tasks'], 'readwrite');
    const store = transaction.objectStore('tasks');
    
    const task = await new Promise((resolve, reject) => {
      const request = store.get(taskId);
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(request.error);
    });
    
    task.status = status;
    if (result) task.result = result;
    task.updatedAt = Date.now();
    
    await new Promise((resolve, reject) => {
      const request = store.put(task);
      request.onsuccess = () => resolve();
      request.onerror = () => reject(request.error);
    });
  }
  
  async delayTask(taskId, delay) {
    setTimeout(async () => {
      await this.updateTaskStatus(taskId, 'pending');
    }, delay);
  }
  
  async getQueueStats() {
    const transaction = this.db.transaction(['tasks'], 'readonly');
    const store = transaction.objectStore('tasks');
    
    const allTasks = await new Promise((resolve, reject) => {
      const request = store.getAll();
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(request.error);
    });
    
    const stats = {
      pending: 0,
      processing: 0,
      completed: 0,
      failed: 0,
      total: allTasks.length
    };
    
    allTasks.forEach(task => {
      stats[task.status]++;
    });
    
    return stats;
  }
  
  async clearCompletedTasks() {
    const transaction = this.db.transaction(['tasks'], 'readwrite');
    const store = transaction.objectStore('tasks');
    const index = store.index('status');
    
    const completedTasks = await new Promise((resolve, reject) => {
      const request = index.getAllKeys('completed');
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(request.error);
    });
    
    for (const key of completedTasks) {
      await new Promise((resolve, reject) => {
        const request = store.delete(key);
        request.onsuccess = () => resolve();
        request.onerror = () => reject(request.error);
      });
    }
    
    console.log(`Cleared ${completedTasks.length} completed tasks`);
  }
}

// Usage
const browserQueue = new BrowserQueueManager(browserConfig);

// Add offline tasks
await browserQueue.addTask('sync_data', {
  userId: '123',
  data: { preferences: { theme: 'dark' } }
});

await browserQueue.addTask('upload_file', {
  file: fileBlob,
  metadata: { name: 'document.pdf', size: fileBlob.size }
});
```

## Advanced Usage Scenarios

### Priority Queue System
```tusk
# Multi-priority queue system
priority_system: {
  queues: {
    critical: @queue.create("critical_tasks", {
      priority: 1,
      max_size: 100,
      workers: 2
    })
    
    high: @queue.create("high_priority", {
      priority: 2,
      max_size: 500,
      workers: 3
    })
    
    normal: @queue.create("normal_tasks", {
      priority: 3,
      max_size: 1000,
      workers: 5
    })
    
    low: @queue.create("low_priority", {
      priority: 4,
      max_size: 2000,
      workers: 2
    })
  }
  
  routing: {
    critical: ["system_alerts", "payment_processing"]
    high: ["user_notifications", "order_updates"]
    normal: ["email_sending", "report_generation"]
    low: ["analytics", "cleanup_tasks"]
  }
}
```

### Distributed Queue System
```tusk
# Distributed queue across multiple nodes
distributed_queue: {
  cluster: {
    nodes: @queue.nodes(["node1", "node2", "node3"])
    load_balancing: "round_robin"
    failover: true
  }
  
  queues: {
    processing: @queue.create("distributed_processing", {
      sharding: true,
      shard_count: 3,
      replication: 2
    })
    
    results: @queue.create("processing_results", {
      aggregation: true,
      timeout: "5m"
    })
  }
  
  monitoring: {
    node_health: @queue.metrics("node_status")
    queue_depth: @queue.metrics("queue_size")
    processing_rate: @queue.metrics("jobs_per_second")
  }
}
```

## Performance Considerations

### Queue Optimization
```javascript
// Optimized queue processing
class OptimizedQueueManager {
  constructor() {
    this.batchSize = 10;
    this.processingInterval = 100; // ms
    this.maxConcurrency = 5;
    this.activeJobs = 0;
  }
  
  async processBatch(tasks) {
    const batches = this.chunkArray(tasks, this.batchSize);
    
    for (const batch of batches) {
      await this.processBatchWithConcurrency(batch);
    }
  }
  
  async processBatchWithConcurrency(batch) {
    const promises = [];
    
    for (const task of batch) {
      if (this.activeJobs >= this.maxConcurrency) {
        await this.waitForAvailableSlot();
      }
      
      this.activeJobs++;
      const promise = this.processTask(task).finally(() => {
        this.activeJobs--;
      });
      
      promises.push(promise);
    }
    
    await Promise.all(promises);
  }
  
  async waitForAvailableSlot() {
    return new Promise(resolve => {
      const checkInterval = setInterval(() => {
        if (this.activeJobs < this.maxConcurrency) {
          clearInterval(checkInterval);
          resolve();
        }
      }, 10);
    });
  }
  
  chunkArray(array, size) {
    const chunks = [];
    for (let i = 0; i < array.length; i += size) {
      chunks.push(array.slice(i, i + size));
    }
    return chunks;
  }
}
```

## Security Notes

### Queue Security
```javascript
// Secure queue implementation
class SecureQueueManager {
  constructor() {
    this.allowedJobTypes = ['email', 'notification', 'report'];
    this.maxJobSize = 1024 * 1024; // 1MB
    this.rateLimit = 100; // jobs per minute
    this.jobCount = 0;
    this.lastReset = Date.now();
  }
  
  validateJob(jobType, data) {
    // Check job type
    if (!this.allowedJobTypes.includes(jobType)) {
      throw new Error('Job type not allowed');
    }
    
    // Check job size
    const jobSize = JSON.stringify(data).length;
    if (jobSize > this.maxJobSize) {
      throw new Error('Job too large');
    }
    
    // Check rate limit
    if (!this.checkRateLimit()) {
      throw new Error('Rate limit exceeded');
    }
    
    // Sanitize job data
    return this.sanitizeJobData(data);
  }
  
  checkRateLimit() {
    const now = Date.now();
    if (now - this.lastReset > 60000) {
      this.jobCount = 0;
      this.lastReset = now;
    }
    
    if (this.jobCount >= this.rateLimit) {
      return false;
    }
    
    this.jobCount++;
    return true;
  }
  
  sanitizeJobData(data) {
    // Remove potentially dangerous properties
    const sanitized = { ...data };
    delete sanitized.__proto__;
    delete sanitized.constructor;
    
    return sanitized;
  }
}
```

## Best Practices

### Error Handling
```javascript
// Comprehensive error handling for queues
class RobustQueueManager {
  constructor() {
    this.errorHandlers = new Map();
    this.retryStrategies = new Map();
    this.deadLetterQueue = null;
  }
  
  registerErrorHandler(jobType, handler) {
    this.errorHandlers.set(jobType, handler);
  }
  
  registerRetryStrategy(jobType, strategy) {
    this.retryStrategies.set(jobType, strategy);
  }
  
  async handleJobError(job, error) {
    const handler = this.errorHandlers.get(job.type);
    
    if (handler) {
      try {
        await handler(job, error);
      } catch (handlerError) {
        console.error('Error handler failed:', handlerError);
      }
    }
    
    // Check retry strategy
    const strategy = this.retryStrategies.get(job.type);
    if (strategy && job.attempts < strategy.maxAttempts) {
      await this.retryJob(job, strategy);
    } else {
      await this.moveToDeadLetter(job, error);
    }
  }
  
  async retryJob(job, strategy) {
    const delay = strategy.calculateDelay(job.attempts);
    
    setTimeout(async () => {
      job.attempts++;
      await this.processJob(job);
    }, delay);
  }
}
```

## Related Topics
- [@stream Operator](./62-stream-operator.md) - Data streaming
- [@cache Operator](./46-cache-operator.md) - Caching strategies
- [@metrics Operator](./47-metrics-operator.md) - Performance monitoring
- [Async Programming](./26-async-programming.md) - Asynchronous patterns
- [Event-Driven Architecture](./27-event-driven.md) - Event handling 