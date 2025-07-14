# @schedule Operator - Flexible Task Scheduling

## Overview
The `@schedule` operator in TuskLang provides advanced, flexible scheduling for tasks, jobs, and workflows. It supports cron, interval, and event-based triggers, enabling robust automation directly from your configuration files.

## TuskLang Syntax

### Basic Interval Scheduling
```tusk
# Run a task every 10 minutes
interval_task: @schedule("interval", "10m", "refresh_cache")

# Run a task every hour
hourly_task: @schedule("interval", "1h", "sync_data")
```

### Event-Based Scheduling
```tusk
# Trigger a task on a specific event
event_task: @schedule("event", "user_signup", "send_welcome_email")

# Schedule a task after another completes
chained_task: @schedule("after", "generate_report", "send_report_email")
```

### Combined Scheduling
```tusk
# Multiple schedules for a single task
multi_schedule: [
  @schedule("interval", "5m", "poll_api"),
  @schedule("event", "data_update", "process_update")
]
```

### Schedule with Parameters
```tusk
# Pass parameters to scheduled tasks
parameterized_schedule: @schedule("interval", "30m", {
  task: "send_digest",
  params: {
    type: "summary",
    recipients: ["admin@example.com", "ops@example.com"]
  }
})
```

## JavaScript Integration

### Node.js Scheduling Implementation
```javascript
const schedule = require('node-schedule');
const tusklang = require('@tusklang/core');

// TuskLang configuration
const config = tusklang.parse(`
schedules: [
  @schedule("interval", "10m", "refresh_cache"),
  @schedule("event", "user_signup", "send_welcome_email"),
  @schedule("after", "generate_report", "send_report_email")
]
`);

// Task handlers
const handlers = {
  refresh_cache: () => {
    // Cache refresh logic
    console.log('Refreshing cache...');
  },
  send_welcome_email: () => {
    // Welcome email logic
    console.log('Sending welcome email...');
  },
  send_report_email: () => {
    // Report email logic
    console.log('Sending report email...');
  }
};

// Register interval schedules
config.schedules.forEach(job => {
  if (job.type === 'interval') {
    const intervalMs = parseDuration(job.value);
    setInterval(() => {
      if (handlers[job.task]) handlers[job.task]();
    }, intervalMs);
  }
});

function parseDuration(duration) {
  // Simple duration parser: "10m" => 600000 ms
  const match = duration.match(/(\d+)([smhd])/);
  if (!match) return 0;
  const value = parseInt(match[1], 10);
  switch (match[2]) {
    case 's': return value * 1000;
    case 'm': return value * 60 * 1000;
    case 'h': return value * 60 * 60 * 1000;
    case 'd': return value * 24 * 60 * 60 * 1000;
    default: return 0;
  }
}
```

### Event-Based Scheduling in Node.js
```javascript
// Simulate event-based scheduling
const EventEmitter = require('events');
const eventBus = new EventEmitter();

// Register event-based schedules
config.schedules.forEach(job => {
  if (job.type === 'event') {
    eventBus.on(job.value, () => {
      if (handlers[job.task]) handlers[job.task]();
    });
  }
});

// Emit event to trigger task
// eventBus.emit('user_signup');
```

### Chained Scheduling
```javascript
// Chained scheduling: run a task after another completes
function runChainedTasks(jobs) {
  const afterMap = new Map();
  jobs.forEach(job => {
    if (job.type === 'after') {
      afterMap.set(job.value, job.task);
    }
  });

  function completeTask(taskName) {
    if (afterMap.has(taskName)) {
      const nextTask = afterMap.get(taskName);
      if (handlers[nextTask]) handlers[nextTask]();
    }
  }

  // Example: after 'generate_report' completes
  // completeTask('generate_report');
}
```

## Advanced Usage Scenarios

### Multi-Trigger Scheduling
```tusk
# Task triggered by both interval and event
multi_trigger_task: [
  @schedule("interval", "15m", "sync_status"),
  @schedule("event", "manual_sync", "sync_status")
]
```

### Conditional Scheduling
```tusk
# Schedule a task only if a condition is met
conditional_schedule: @schedule("interval", "1h", {
  task: "archive_logs",
  condition: @env("ENABLE_ARCHIVE", true)
})
```

## TypeScript Implementation

### Typed Schedule Manager
```typescript
interface ScheduleConfig {
  type: 'interval' | 'event' | 'after';
  value: string;
  task: string;
  params?: Record<string, any>;
  condition?: boolean;
}

class ScheduleManager {
  private schedules: ScheduleConfig[] = [];
  private handlers: Record<string, Function> = {};

  registerHandler(task: string, handler: Function) {
    this.handlers[task] = handler;
  }

  addSchedule(schedule: ScheduleConfig) {
    this.schedules.push(schedule);
    // Register with scheduler
    // ...
  }

  triggerEvent(event: string) {
    this.schedules.forEach(schedule => {
      if (schedule.type === 'event' && schedule.value === event) {
        if (this.handlers[schedule.task]) this.handlers[schedule.task]();
      }
    });
  }
}
```

## Real-World Examples

### Automated Data Sync
```javascript
// Sync data every hour
setInterval(() => {
  // Data sync logic
  console.log('Syncing data...');
}, 60 * 60 * 1000);
```

### User Notification on Event
```javascript
// Notify user on signup event
eventBus.on('user_signup', () => {
  // Notification logic
  console.log('User signed up! Sending notification...');
});
```

## Performance Considerations
- Use lightweight handlers for frequent schedules
- Avoid overlapping executions for long-running tasks
- Monitor schedule drift and missed triggers
- Use event debouncing for high-frequency events

## Security Notes
- Validate schedule parameters to prevent abuse
- Restrict sensitive tasks to trusted triggers
- Log all scheduled executions for audit
- Use secure storage for credentials in scheduled tasks

## Best Practices
- Use clear, descriptive task names
- Document all schedules and triggers
- Handle errors and retries gracefully
- Monitor and alert on failed or missed schedules

## Related Topics
- [@cron Operator](./64-cron-operator.md) - Cron-based scheduling
- [@queue Operator](./63-queue-operator.md) - Message queue management
- [@stream Operator](./62-stream-operator.md) - Data streaming
- [Event-Driven Architecture](./27-event-driven.md) - Event handling 