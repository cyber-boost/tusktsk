# @trigger Operator - Reactive Task Execution

## Overview
The `@trigger` operator in TuskLang enables reactive execution of tasks and workflows in response to specific conditions, events, or state changes. It is essential for building dynamic, event-driven, and automated systems.

## TuskLang Syntax

### Basic Trigger Usage
```tusk
# Trigger a task when a file is uploaded
on_file_upload: @trigger("file_uploaded", "process_file")

# Trigger a notification when a threshold is reached
on_threshold: @trigger("metric_exceeded", {
  task: "send_alert",
  params: { level: "critical" }
})
```

### Conditional Triggers
```tusk
# Trigger only if a condition is met
conditional_trigger: @trigger("user_login", {
  task: "check_2fa",
  condition: @env("REQUIRE_2FA", true)
})
```

### Multiple Triggers
```tusk
# Multiple triggers for a single task
multi_trigger: [
  @trigger("order_placed", "start_fulfillment"),
  @trigger("payment_confirmed", "send_receipt")
]
```

### Trigger with Debounce/Throttle
```tusk
# Debounce high-frequency triggers
debounced_trigger: @trigger("search_input", {
  task: "fetch_suggestions",
  debounce: "300ms"
})

# Throttle trigger execution
throttled_trigger: @trigger("api_request", {
  task: "log_request",
  throttle: "1s"
})
```

## JavaScript Integration

### Node.js Trigger System
```javascript
const EventEmitter = require('events');
const tusklang = require('@tusklang/core');

const config = tusklang.parse(`
triggers: [
  @trigger("file_uploaded", "process_file"),
  @trigger("metric_exceeded", { task: "send_alert", params: { level: "critical" } }),
  @trigger("user_login", { task: "check_2fa", condition: @env("REQUIRE_2FA", true) })
]
`);

const eventBus = new EventEmitter();
const handlers = {
  process_file: (data) => {
    console.log('Processing file:', data.filename);
  },
  send_alert: (data) => {
    console.log('Sending alert:', data.level);
  },
  check_2fa: (data) => {
    console.log('Checking 2FA for user:', data.user);
  }
};

// Register triggers
config.triggers.forEach(trigger => {
  eventBus.on(trigger.event, (data) => {
    if (trigger.condition === undefined || trigger.condition) {
      if (handlers[trigger.task]) handlers[trigger.task](data);
    }
  });
});

// Emit events to test
// eventBus.emit('file_uploaded', { filename: 'report.pdf' });
```

### Browser Trigger System
```javascript
// Browser-based trigger system
const tusklang = require('@tusklang/core');

const config = tusklang.parse(`
browser_triggers: [
  @trigger("button_click", "show_modal"),
  @trigger("form_submit", { task: "validate_form", debounce: "200ms" })
]
`);

const handlers = {
  show_modal: () => {
    document.getElementById('modal').style.display = 'block';
  },
  validate_form: () => {
    // Form validation logic
    alert('Validating form...');
  }
};

function debounce(fn, delay) {
  let timer;
  return function(...args) {
    clearTimeout(timer);
    timer = setTimeout(() => fn.apply(this, args), delay);
  };
}

config.browser_triggers.forEach(trigger => {
  if (trigger.event === 'button_click') {
    document.getElementById('myButton').addEventListener('click', handlers[trigger.task]);
  }
  if (trigger.event === 'form_submit') {
    document.getElementById('myForm').addEventListener('submit', debounce(handlers[trigger.task], 200));
  }
});
```

## Advanced Usage Scenarios

### Workflow Automation
```tusk
# Trigger a workflow on data import
workflow_trigger: @trigger("data_imported", "start_data_pipeline")
```

### Real-Time Monitoring
```tusk
# Trigger alerts on real-time metrics
monitoring_trigger: @trigger("cpu_overload", {
  task: "scale_up",
  params: { instances: 2 }
})
```

### Chained Triggers
```tusk
# Chain triggers for multi-step processes
chained_triggers: [
  @trigger("user_registered", "send_welcome_email"),
  @trigger("welcome_email_sent", "grant_bonus")
]
```

## TypeScript Implementation

### Typed Trigger System
```typescript
interface TriggerConfig {
  event: string;
  task: string;
  params?: Record<string, any>;
  condition?: boolean;
  debounce?: string;
  throttle?: string;
}

class TriggerManager {
  private triggers: TriggerConfig[] = [];
  private handlers: Record<string, Function> = {};

  registerHandler(task: string, handler: Function) {
    this.handlers[task] = handler;
  }

  addTrigger(trigger: TriggerConfig) {
    this.triggers.push(trigger);
    // Register with event system
    // ...
  }

  emitEvent(event: string, data?: any) {
    this.triggers.forEach(trigger => {
      if (trigger.event === event && (trigger.condition === undefined || trigger.condition)) {
        if (this.handlers[trigger.task]) this.handlers[trigger.task](data);
      }
    });
  }
}
```

## Real-World Examples

### File Processing Pipeline
```javascript
// Trigger file processing on upload
eventBus.on('file_uploaded', (data) => {
  handlers.process_file(data);
});
```

### Alerting on Metrics
```javascript
// Trigger alert when metric is exceeded
eventBus.on('metric_exceeded', (data) => {
  handlers.send_alert(data);
});
```

## Performance Considerations
- Use debounce/throttle for high-frequency triggers
- Avoid blocking the main thread in trigger handlers
- Monitor trigger execution time and frequency
- Use async handlers for I/O-bound tasks

## Security Notes
- Validate trigger data before processing
- Restrict sensitive triggers to trusted sources
- Log all trigger executions for audit
- Prevent trigger handler injection or override

## Best Practices
- Use clear, descriptive event and task names
- Document all triggers and their conditions
- Handle errors gracefully in trigger handlers
- Monitor and audit trigger flows

## Related Topics
- [@event Operator](./66-event-operator.md) - Event-driven automation
- [@schedule Operator](./65-schedule-operator.md) - Flexible scheduling
- [@queue Operator](./63-queue-operator.md) - Message queue management
- [Event-Driven Architecture](./27-event-driven.md) - Event handling 