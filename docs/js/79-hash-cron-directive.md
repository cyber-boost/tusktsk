# #cron Directive - Scheduled Tasks

## Overview
The `#cron` directive in TuskLang provides comprehensive scheduled task configuration capabilities, enabling you to define cron jobs, scheduled tasks, recurring operations, and automated workflows in a declarative manner.

## TuskLang Syntax

### Basic Cron Job
```tusk
#cron {
  name: "daily-backup",
  schedule: "0 2 * * *",
  handler: "backupController.createBackup",
  description: "Daily database backup at 2 AM"
}
```

### Cron with Options
```tusk
#cron {
  name: "cleanup-logs",
  schedule: "0 3 * * 0",
  handler: "maintenanceController.cleanupLogs",
  options: {
    timeout: 300,
    retries: 3,
    enabled: true
  },
  environment: "production"
}
```

### Multiple Cron Jobs
```tusk
#cron {
  jobs: {
    "hourly-check": {
      schedule: "0 * * * *",
      handler: "healthController.checkSystem",
      description: "Hourly system health check"
    },
    "weekly-report": {
      schedule: "0 9 * * 1",
      handler: "reportController.generateWeeklyReport",
      description: "Weekly report generation"
    },
    "monthly-cleanup": {
      schedule: "0 4 1 * *",
      handler: "maintenanceController.monthlyCleanup",
      description: "Monthly system cleanup"
    }
  }
}
```

## JavaScript Integration

### Node.js Cron Scheduler
```javascript
const tusklang = require('@tusklang/core');
const cron = require('node-cron');
const fs = require('fs').promises;
const path = require('path');

const config = tusklang.parse(`
cron_config: #cron {
  jobs: {
    "daily-backup": {
      schedule: "0 2 * * *",
      handler: "backupController.createBackup",
      description: "Daily database backup at 2 AM",
      options: {
        timeout: 600,
        retries: 3,
        enabled: true
      }
    },
    "cleanup-logs": {
      schedule: "0 3 * * 0",
      handler: "maintenanceController.cleanupLogs",
      description: "Weekly log cleanup",
      options: {
        timeout: 300,
        retries: 2,
        enabled: true
      }
    },
    "health-check": {
      schedule: "*/15 * * * *",
      handler: "healthController.checkSystem",
      description: "System health check every 15 minutes",
      options: {
        timeout: 60,
        retries: 1,
        enabled: true
      }
    },
    "data-sync": {
      schedule: "0 */6 * * *",
      handler: "syncController.syncData",
      description: "Data synchronization every 6 hours",
      options: {
        timeout: 1800,
        retries: 5,
        enabled: true
      }
    }
  }
}
`);

class CronScheduler {
  constructor(config) {
    this.config = config.cron_config;
    this.jobs = new Map();
    this.handlers = new Map();
    this.jobHistory = new Map();
    this.initializeScheduler();
  }

  initializeScheduler() {
    // Register handlers
    this.registerHandlers();
    
    // Create cron jobs
    this.createJobs();
    
    // Set up monitoring
    this.setupMonitoring();
  }

  registerHandlers() {
    // Backup controller
    this.handlers.set('backupController.createBackup', async (jobName) => {
      console.log(`[${new Date().toISOString()}] Starting backup job: ${jobName}`);
      
      try {
        const backupPath = await this.createDatabaseBackup();
        console.log(`Backup created successfully: ${backupPath}`);
        
        // Clean up old backups
        await this.cleanupOldBackups();
        
        this.recordJobSuccess(jobName);
      } catch (error) {
        console.error(`Backup job failed: ${error.message}`);
        this.recordJobFailure(jobName, error);
        throw error;
      }
    });

    // Maintenance controller
    this.handlers.set('maintenanceController.cleanupLogs', async (jobName) => {
      console.log(`[${new Date().toISOString()}] Starting log cleanup: ${jobName}`);
      
      try {
        const deletedFiles = await this.cleanupLogFiles();
        console.log(`Log cleanup completed. Deleted ${deletedFiles} files.`);
        
        this.recordJobSuccess(jobName);
      } catch (error) {
        console.error(`Log cleanup failed: ${error.message}`);
        this.recordJobFailure(jobName, error);
        throw error;
      }
    });

    // Health controller
    this.handlers.set('healthController.checkSystem', async (jobName) => {
      console.log(`[${new Date().toISOString()}] Starting health check: ${jobName}`);
      
      try {
        const healthStatus = await this.checkSystemHealth();
        console.log(`Health check completed. Status: ${healthStatus.status}`);
        
        if (healthStatus.status !== 'healthy') {
          await this.sendHealthAlert(healthStatus);
        }
        
        this.recordJobSuccess(jobName);
      } catch (error) {
        console.error(`Health check failed: ${error.message}`);
        this.recordJobFailure(jobName, error);
        throw error;
      }
    });

    // Sync controller
    this.handlers.set('syncController.syncData', async (jobName) => {
      console.log(`[${new Date().toISOString()}] Starting data sync: ${jobName}`);
      
      try {
        const syncResult = await this.syncDataSources();
        console.log(`Data sync completed. Synced ${syncResult.count} records.`);
        
        this.recordJobSuccess(jobName);
      } catch (error) {
        console.error(`Data sync failed: ${error.message}`);
        this.recordJobFailure(jobName, error);
        throw error;
      }
    });
  }

  createJobs() {
    if (!this.config.jobs) return;

    Object.entries(this.config.jobs).forEach(([name, jobConfig]) => {
      this.createJob(name, jobConfig);
    });
  }

  createJob(name, config) {
    if (!config.options?.enabled) {
      console.log(`Job '${name}' is disabled`);
      return;
    }

    const job = {
      name: name,
      config: config,
      task: cron.schedule(config.schedule, async () => {
        await this.executeJob(name, config);
      }, {
        scheduled: true,
        timezone: "UTC"
      }),
      lastRun: null,
      nextRun: null,
      status: 'idle'
    };

    this.jobs.set(name, job);
    
    // Calculate next run time
    job.nextRun = this.calculateNextRun(config.schedule);
    
    console.log(`Scheduled job '${name}' - Next run: ${job.nextRun}`);
  }

  async executeJob(name, config) {
    const job = this.jobs.get(name);
    if (!job) return;

    job.status = 'running';
    job.lastRun = new Date();
    
    const startTime = Date.now();
    
    try {
      const handler = this.handlers.get(config.handler);
      if (!handler) {
        throw new Error(`Handler '${config.handler}' not found`);
      }

      // Execute with timeout
      const timeout = config.options?.timeout || 300;
      await this.executeWithTimeout(handler, timeout, name);
      
      job.status = 'completed';
      console.log(`Job '${name}' completed successfully in ${Date.now() - startTime}ms`);
      
    } catch (error) {
      job.status = 'failed';
      console.error(`Job '${name}' failed: ${error.message}`);
      
      // Handle retries
      await this.handleJobRetry(name, config, error);
    }
  }

  async executeWithTimeout(handler, timeout, jobName) {
    return new Promise((resolve, reject) => {
      const timeoutId = setTimeout(() => {
        reject(new Error(`Job '${jobName}' timed out after ${timeout} seconds`));
      }, timeout * 1000);

      handler(jobName)
        .then((result) => {
          clearTimeout(timeoutId);
          resolve(result);
        })
        .catch((error) => {
          clearTimeout(timeoutId);
          reject(error);
        });
    });
  }

  async handleJobRetry(name, config, error) {
    const maxRetries = config.options?.retries || 0;
    const retryCount = this.getRetryCount(name);
    
    if (retryCount < maxRetries) {
      console.log(`Retrying job '${name}' (${retryCount + 1}/${maxRetries})`);
      
      // Exponential backoff
      const delay = Math.pow(2, retryCount) * 1000;
      await new Promise(resolve => setTimeout(resolve, delay));
      
      this.incrementRetryCount(name);
      await this.executeJob(name, config);
    } else {
      console.error(`Job '${name}' failed after ${maxRetries} retries`);
      await this.sendJobFailureAlert(name, error);
    }
  }

  getRetryCount(name) {
    const history = this.jobHistory.get(name) || {};
    return history.retryCount || 0;
  }

  incrementRetryCount(name) {
    const history = this.jobHistory.get(name) || {};
    history.retryCount = (history.retryCount || 0) + 1;
    this.jobHistory.set(name, history);
  }

  recordJobSuccess(name) {
    const history = this.jobHistory.get(name) || {};
    history.lastSuccess = new Date();
    history.successCount = (history.successCount || 0) + 1;
    history.retryCount = 0;
    this.jobHistory.set(name, history);
  }

  recordJobFailure(name, error) {
    const history = this.jobHistory.get(name) || {};
    history.lastFailure = new Date();
    history.failureCount = (history.failureCount || 0) + 1;
    history.lastError = error.message;
    this.jobHistory.set(name, history);
  }

  calculateNextRun(schedule) {
    // Simple next run calculation (in production, use a proper cron parser)
    const now = new Date();
    const next = new Date(now);
    
    // This is a simplified calculation - use a proper cron library in production
    if (schedule.includes('*/15')) {
      next.setMinutes(Math.ceil(next.getMinutes() / 15) * 15);
      next.setSeconds(0);
    } else if (schedule.includes('0 2 * * *')) {
      next.setHours(2, 0, 0, 0);
      if (next <= now) {
        next.setDate(next.getDate() + 1);
      }
    }
    
    return next;
  }

  // Mock implementation methods
  async createDatabaseBackup() {
    const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
    const backupPath = `/backups/db-backup-${timestamp}.sql`;
    
    // Simulate backup creation
    await new Promise(resolve => setTimeout(resolve, 2000));
    
    return backupPath;
  }

  async cleanupOldBackups() {
    // Simulate cleanup of backups older than 30 days
    console.log('Cleaning up old backups...');
    await new Promise(resolve => setTimeout(resolve, 1000));
  }

  async cleanupLogFiles() {
    // Simulate log file cleanup
    const logDir = '/var/log/app';
    const files = await fs.readdir(logDir);
    const oldFiles = files.filter(file => file.endsWith('.log.old'));
    
    for (const file of oldFiles) {
      await fs.unlink(path.join(logDir, file));
    }
    
    return oldFiles.length;
  }

  async checkSystemHealth() {
    // Simulate health check
    const checks = [
      { name: 'database', status: 'healthy' },
      { name: 'redis', status: 'healthy' },
      { name: 'disk', status: 'warning' }
    ];
    
    const overallStatus = checks.every(check => check.status === 'healthy') ? 'healthy' : 'warning';
    
    return {
      status: overallStatus,
      checks: checks,
      timestamp: new Date()
    };
  }

  async sendHealthAlert(healthStatus) {
    console.log(`Health alert: ${healthStatus.status}`);
    // Send notification (email, Slack, etc.)
  }

  async syncDataSources() {
    // Simulate data synchronization
    await new Promise(resolve => setTimeout(resolve, 3000));
    
    return {
      count: Math.floor(Math.random() * 1000) + 100,
      timestamp: new Date()
    };
  }

  async sendJobFailureAlert(name, error) {
    console.log(`Job failure alert for '${name}': ${error.message}`);
    // Send notification
  }

  // Management methods
  getJobStatus(name) {
    const job = this.jobs.get(name);
    if (!job) return null;
    
    return {
      name: job.name,
      status: job.status,
      lastRun: job.lastRun,
      nextRun: job.nextRun,
      history: this.jobHistory.get(name) || {}
    };
  }

  getAllJobStatus() {
    const status = {};
    for (const [name, job] of this.jobs) {
      status[name] = this.getJobStatus(name);
    }
    return status;
  }

  stopJob(name) {
    const job = this.jobs.get(name);
    if (job && job.task) {
      job.task.stop();
      job.status = 'stopped';
      console.log(`Job '${name}' stopped`);
    }
  }

  startJob(name) {
    const job = this.jobs.get(name);
    if (job && job.task) {
      job.task.start();
      job.status = 'idle';
      console.log(`Job '${name}' started`);
    }
  }

  stopAllJobs() {
    for (const [name, job] of this.jobs) {
      this.stopJob(name);
    }
  }

  startAllJobs() {
    for (const [name, job] of this.jobs) {
      this.startJob(name);
    }
  }

  setupMonitoring() {
    // Set up periodic status reporting
    setInterval(() => {
      const status = this.getAllJobStatus();
      console.log('Cron job status:', status);
    }, 60000); // Every minute
  }
}

// Usage
const cronScheduler = new CronScheduler(config);

// Graceful shutdown
process.on('SIGTERM', () => {
  console.log('Shutting down cron scheduler...');
  cronScheduler.stopAllJobs();
  process.exit(0);
});

process.on('SIGINT', () => {
  console.log('Shutting down cron scheduler...');
  cronScheduler.stopAllJobs();
  process.exit(0);
});
```

### Browser-Based Scheduler
```javascript
// Browser-based task scheduler
const browserConfig = tusklang.parse(`
browser_cron: #cron {
  jobs: {
    "data-refresh": {
      schedule: "*/5 * * * *", // Every 5 minutes
      handler: "dataController.refreshData",
      description: "Refresh data every 5 minutes",
      options: {
        enabled: true
      }
    },
    "session-check": {
      schedule: "0 * * * *", // Every hour
      handler: "authController.checkSession",
      description: "Check user session every hour",
      options: {
        enabled: true
      }
    }
  }
}
`);

class BrowserScheduler {
  constructor(config) {
    this.config = config.browser_cron;
    this.jobs = new Map();
    this.timers = new Map();
    this.handlers = new Map();
    this.initializeScheduler();
  }

  initializeScheduler() {
    this.registerHandlers();
    this.createJobs();
    this.setupVisibilityHandling();
  }

  registerHandlers() {
    this.handlers.set('dataController.refreshData', async (jobName) => {
      console.log(`[${new Date().toISOString()}] Refreshing data: ${jobName}`);
      
      try {
        // Refresh data from API
        const response = await fetch('/api/data/refresh');
        const data = await response.json();
        
        // Update UI
        this.updateUIWithNewData(data);
        
        console.log('Data refresh completed successfully');
      } catch (error) {
        console.error('Data refresh failed:', error);
      }
    });

    this.handlers.set('authController.checkSession', async (jobName) => {
      console.log(`[${new Date().toISOString()}] Checking session: ${jobName}`);
      
      try {
        const response = await fetch('/api/auth/session');
        const session = await response.json();
        
        if (!session.valid) {
          // Redirect to login
          window.location.href = '/login';
        }
        
        console.log('Session check completed');
      } catch (error) {
        console.error('Session check failed:', error);
      }
    });
  }

  createJobs() {
    if (!this.config.jobs) return;

    Object.entries(this.config.jobs).forEach(([name, jobConfig]) => {
      this.createJob(name, jobConfig);
    });
  }

  createJob(name, config) {
    if (!config.options?.enabled) {
      console.log(`Job '${name}' is disabled`);
      return;
    }

    const interval = this.parseCronToInterval(config.schedule);
    if (!interval) {
      console.error(`Invalid schedule for job '${name}': ${config.schedule}`);
      return;
    }

    const job = {
      name: name,
      config: config,
      interval: interval,
      lastRun: null,
      nextRun: null,
      status: 'idle'
    };

    this.jobs.set(name, job);
    
    // Calculate next run time
    job.nextRun = this.calculateNextRun(config.schedule);
    
    // Start the job
    this.startJob(name);
    
    console.log(`Scheduled job '${name}' - Next run: ${job.nextRun}`);
  }

  parseCronToInterval(schedule) {
    // Simple cron to interval conversion
    if (schedule.includes('*/5')) {
      return 5 * 60 * 1000; // 5 minutes
    } else if (schedule.includes('0 * * * *')) {
      return 60 * 60 * 1000; // 1 hour
    } else if (schedule.includes('0 2 * * *')) {
      return 24 * 60 * 60 * 1000; // 24 hours
    }
    
    return null;
  }

  calculateNextRun(schedule) {
    const now = new Date();
    const next = new Date(now);
    
    if (schedule.includes('*/5')) {
      next.setMinutes(Math.ceil(next.getMinutes() / 5) * 5);
      next.setSeconds(0);
    } else if (schedule.includes('0 * * * *')) {
      next.setMinutes(0);
      next.setSeconds(0);
      next.setHours(next.getHours() + 1);
    }
    
    return next;
  }

  startJob(name) {
    const job = this.jobs.get(name);
    if (!job) return;

    const timer = setInterval(async () => {
      await this.executeJob(name, job.config);
    }, job.interval);

    this.timers.set(name, timer);
    job.status = 'running';
  }

  async executeJob(name, config) {
    const job = this.jobs.get(name);
    if (!job) return;

    job.lastRun = new Date();
    job.status = 'running';
    
    try {
      const handler = this.handlers.get(config.handler);
      if (!handler) {
        throw new Error(`Handler '${config.handler}' not found`);
      }

      await handler(name);
      job.status = 'completed';
      
    } catch (error) {
      job.status = 'failed';
      console.error(`Job '${name}' failed:`, error);
    }
  }

  stopJob(name) {
    const timer = this.timers.get(name);
    if (timer) {
      clearInterval(timer);
      this.timers.delete(name);
      
      const job = this.jobs.get(name);
      if (job) {
        job.status = 'stopped';
      }
      
      console.log(`Job '${name}' stopped`);
    }
  }

  startJob(name) {
    const job = this.jobs.get(name);
    if (job && !this.timers.has(name)) {
      this.startJob(name);
    }
  }

  setupVisibilityHandling() {
    // Pause jobs when page is not visible
    document.addEventListener('visibilitychange', () => {
      if (document.hidden) {
        console.log('Page hidden, pausing jobs');
        this.pauseAllJobs();
      } else {
        console.log('Page visible, resuming jobs');
        this.resumeAllJobs();
      }
    });
  }

  pauseAllJobs() {
    for (const [name, timer] of this.timers) {
      clearInterval(timer);
    }
    this.timers.clear();
  }

  resumeAllJobs() {
    for (const [name, job] of this.jobs) {
      if (job.status === 'running') {
        this.startJob(name);
      }
    }
  }

  updateUIWithNewData(data) {
    // Update UI components with new data
    const event = new CustomEvent('dataRefreshed', { detail: data });
    window.dispatchEvent(event);
  }

  getJobStatus(name) {
    const job = this.jobs.get(name);
    if (!job) return null;
    
    return {
      name: job.name,
      status: job.status,
      lastRun: job.lastRun,
      nextRun: job.nextRun,
      interval: job.interval
    };
  }

  getAllJobStatus() {
    const status = {};
    for (const [name, job] of this.jobs) {
      status[name] = this.getJobStatus(name);
    }
    return status;
  }
}

// Usage
const browserScheduler = new BrowserScheduler(browserConfig);

// Listen for data refresh events
window.addEventListener('dataRefreshed', (event) => {
  console.log('Data refreshed:', event.detail);
  // Update UI components
});
```

## Advanced Usage Scenarios

### Distributed Cron Jobs
```tusk
#cron {
  distributed: true,
  coordinator: "redis://localhost:6379",
  jobs: {
    "distributed-backup": {
      schedule: "0 2 * * *",
      handler: "backupController.createDistributedBackup",
      options: {
        lock_timeout: 300,
        max_workers: 3
      }
    }
  }
}
```

### Conditional Cron Jobs
```tusk
#cron {
  jobs: {
    "conditional-cleanup": {
      schedule: "0 4 * * *",
      handler: "maintenanceController.conditionalCleanup",
      conditions: {
        disk_usage: ">80%",
        memory_usage: "<90%"
      }
    }
  }
}
```

### Cron Job Dependencies
```tusk
#cron {
  jobs: {
    "data-export": {
      schedule: "0 1 * * *",
      handler: "exportController.exportData",
      dependencies: ["backup"]
    },
    "backup": {
      schedule: "0 0 * * *",
      handler: "backupController.createBackup"
    }
  }
}
```

## TypeScript Implementation

### Typed Cron Scheduler
```typescript
interface CronConfig {
  jobs?: Record<string, JobConfig>;
  distributed?: boolean;
  coordinator?: string;
}

interface JobConfig {
  schedule: string;
  handler: string;
  description?: string;
  options?: JobOptions;
  conditions?: Record<string, string>;
  dependencies?: string[];
}

interface JobOptions {
  timeout?: number;
  retries?: number;
  enabled?: boolean;
  lock_timeout?: number;
  max_workers?: number;
}

class TypedCronScheduler {
  private config: CronConfig;
  private jobs: Map<string, Job> = new Map();
  private handlers: Map<string, Function> = new Map();

  constructor(config: CronConfig) {
    this.config = config;
    this.initializeScheduler();
  }

  private initializeScheduler(): void {
    this.registerHandlers();
    this.createJobs();
  }

  private registerHandlers(): void {
    // Implementation for handler registration
  }

  private createJobs(): void {
    // Implementation for job creation
  }

  async executeJob(name: string, config: JobConfig): Promise<void> {
    // Implementation for job execution
  }

  getJobStatus(name: string): JobStatus | null {
    // Implementation for status retrieval
    return null;
  }
}
```

## Real-World Examples

### Database Maintenance Cron
```javascript
// Database maintenance cron jobs
const dbConfig = tusklang.parse(`
db_cron: #cron {
  jobs: {
    "vacuum": {
      schedule: "0 3 * * 0",
      handler: "dbController.vacuum",
      description: "Weekly database vacuum"
    },
    "analyze": {
      schedule: "0 4 * * 0",
      handler: "dbController.analyze",
      description: "Weekly database analyze"
    },
    "backup": {
      schedule: "0 2 * * *",
      handler: "dbController.backup",
      description: "Daily database backup"
    }
  }
}
`);

const dbScheduler = new CronScheduler(dbConfig);

// Register database handlers
dbScheduler.registerHandler('dbController.vacuum', async (jobName) => {
  console.log('Running database vacuum...');
  // Execute VACUUM command
});

dbScheduler.registerHandler('dbController.analyze', async (jobName) => {
  console.log('Running database analyze...');
  // Execute ANALYZE command
});

dbScheduler.registerHandler('dbController.backup', async (jobName) => {
  console.log('Creating database backup...');
  // Create database backup
});
```

### Email Campaign Cron
```javascript
// Email campaign scheduling
const emailConfig = tusklang.parse(`
email_cron: #cron {
  jobs: {
    "newsletter": {
      schedule: "0 9 * * 1",
      handler: "emailController.sendNewsletter",
      description: "Weekly newsletter"
    },
    "reminders": {
      schedule: "0 10 * * *",
      handler: "emailController.sendReminders",
      description: "Daily reminder emails"
    }
  }
}
`);

const emailScheduler = new CronScheduler(emailConfig);

emailScheduler.registerHandler('emailController.sendNewsletter', async (jobName) => {
  console.log('Sending weekly newsletter...');
  // Send newsletter to subscribers
});

emailScheduler.registerHandler('emailController.sendReminders', async (jobName) => {
  console.log('Sending daily reminders...');
  // Send reminder emails
});
```

## Performance Considerations
- Use connection pooling for database operations
- Implement job queuing for long-running tasks
- Use caching for frequently accessed data
- Monitor job execution times

## Security Notes
- Validate all job inputs and parameters
- Implement proper error handling and logging
- Use secure defaults for sensitive operations
- Implement job isolation and sandboxing

## Best Practices
- Use descriptive job names and descriptions
- Implement proper logging and monitoring
- Use appropriate timeouts and retry strategies
- Test cron jobs thoroughly before deployment

## Related Topics
- [@schedule Operator](./66-schedule-operator.md) - Task scheduling
- [@event Operator](./67-event-operator.md) - Event handling
- [@trigger Operator](./68-trigger-operator.md) - Event triggers
- [Automation](./16-scripting.md) - Task automation 