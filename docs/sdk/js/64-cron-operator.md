# @cron Operator - Scheduled Task Automation

## Overview
The `@cron` operator in TuskLang enables powerful, flexible scheduling of tasks using cron expressions. It supports time-based automation for jobs, maintenance, notifications, and more, directly from your configuration files.

## TuskLang Syntax

### Basic Cron Scheduling
```tusk
# Run a cleanup task every day at midnight
cleanup_schedule: @cron("0 0 * * *", "cleanup_temp_files")

# Schedule a report every Monday at 6 AM
weekly_report: @cron("0 6 * * 1", "generate_weekly_report")
```

### Multiple Cron Jobs
```tusk
# Multiple scheduled jobs
task_schedules: [
  @cron("0 2 * * *", "backup_database"),
  @cron("0 3 * * 0", "send_weekly_summary"),
  @cron("*/15 * * * *", "check_system_health")
]
```

### Cron with Parameters
```tusk
# Pass parameters to scheduled tasks
parameterized_cron: @cron("0 5 * * *", {
  task: "send_reminders",
  params: {
    type: "email",
    priority: "high"
  }
})
```

### Cron with Timezone
```tusk
# Schedule with timezone support
cron_with_timezone: @cron("0 9 * * *", "morning_sync", {
  timezone: "America/New_York"
})
```

## JavaScript Integration

### Node.js Cron Implementation
```javascript
const cron = require('node-cron');
const tusklang = require('@tusklang/core');

// TuskLang configuration
const config = tusklang.parse(`
cron_jobs: [
  @cron("0 0 * * *", "cleanup_temp_files"),
  @cron("0 6 * * 1", "generate_weekly_report"),
  @cron("*/10 * * * *", {
    task: "send_notifications",
    params: { type: "push" }
  })
]
`);

// Task handlers
const handlers = {
  cleanup_temp_files: () => {
    // Cleanup logic
    console.log('Cleaning up temp files...');
  },
  generate_weekly_report: () => {
    // Report generation logic
    console.log('Generating weekly report...');
  },
  send_notifications: (params) => {
    // Notification logic
    console.log('Sending notifications:', params);
  }
};

// Register cron jobs
config.cron_jobs.forEach(job => {
  if (typeof job === 'string') return;
  const { schedule, task, params } = job;
  cron.schedule(schedule, () => {
    if (handlers[task]) {
      handlers[task](params);
    }
  });
});
```

### Browser Cron Simulation
```javascript
// Simulate cron jobs in the browser using setInterval
const tusklang = require('@tusklang/core');

const config = tusklang.parse(`
browser_cron: [
  @cron("*/5 * * * *", "refresh_data"),
  @cron("0 12 * * *", "show_daily_tip")
]
`);

const handlers = {
  refresh_data: () => {
    // Fetch new data
    console.log('Refreshing data...');
  },
  show_daily_tip: () => {
    // Show a tip to the user
    alert('Here is your daily tip!');
  }
};

function cronToInterval(cronExpr) {
  // Simple conversion for demo: only supports minute intervals
  const match = cronExpr.match(/^\*\/(\d+) \* \* \* \*$/);
  if (match) {
    return parseInt(match[1], 10) * 60 * 1000;
  }
  return null;
}

config.browser_cron.forEach(job => {
  if (typeof job === 'string') return;
  const { schedule, task } = job;
  const interval = cronToInterval(schedule);
  if (interval && handlers[task]) {
    setInterval(handlers[task], interval);
  }
});
```

## Advanced Usage Scenarios

### Distributed Cron Jobs
```tusk
# Distributed cron jobs across multiple nodes
distributed_cron: {
  jobs: [
    @cron("0 1 * * *", "sync_cluster", { node: "node1" }),
    @cron("0 2 * * *", "sync_cluster", { node: "node2" })
  ]
  failover: true
  monitoring: @metrics("cron_job_status")
}
```

### Dynamic Cron Scheduling
```tusk
# Dynamically add or remove cron jobs
cron_manager: {
  add_job: @cron.add("0 4 * * *", "dynamic_task"),
  remove_job: @cron.remove("dynamic_task")
}
```

## TypeScript Implementation

### Typed Cron Manager
```typescript
interface CronJobConfig {
  schedule: string;
  task: string;
  params?: Record<string, any>;
  timezone?: string;
}

class CronManager {
  private jobs: CronJobConfig[] = [];
  private handlers: Record<string, Function> = {};

  registerHandler(task: string, handler: Function) {
    this.handlers[task] = handler;
  }

  addJob(job: CronJobConfig) {
    this.jobs.push(job);
    // Register with node-cron or similar
    // ...
  }

  removeJob(task: string) {
    this.jobs = this.jobs.filter(job => job.task !== task);
    // Remove from scheduler
    // ...
  }

  startAll() {
    this.jobs.forEach(job => {
      // Register with scheduler
      // ...
    });
  }
}
```

## Real-World Examples

### Automated Backups
```javascript
// Schedule daily backups
const cron = require('node-cron');
cron.schedule('0 2 * * *', () => {
  // Backup logic
  console.log('Performing daily backup...');
});
```

### Email Reminders
```javascript
// Send email reminders every Monday
cron.schedule('0 9 * * 1', () => {
  // Email logic
  console.log('Sending Monday reminders...');
});
```

## Performance Considerations
- Use distributed cron managers for high-availability
- Avoid overlapping jobs by using job locks
- Monitor job execution time and failures
- Use lightweight handlers for frequent jobs

## Security Notes
- Validate cron expressions to prevent misfires
- Restrict sensitive tasks to trusted schedules
- Log all scheduled executions for audit
- Use secure storage for credentials in scheduled jobs

## Best Practices
- Use descriptive task names
- Document all scheduled jobs
- Handle errors and retries gracefully
- Monitor and alert on failed jobs

## Related Topics
- [@queue Operator](./63-queue-operator.md) - Message queue management
- [@stream Operator](./62-stream-operator.md) - Data streaming
- [@metrics Operator](./47-metrics-operator.md) - Performance monitoring
- [Async Programming](./26-async-programming.md) - Asynchronous patterns
- [Event-Driven Architecture](./27-event-driven.md) - Event handling 