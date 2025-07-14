# @event Operator - Event-Driven Automation

## Overview
The `@event` operator in TuskLang enables event-driven automation, allowing you to define, emit, and handle custom events within your configuration. This supports reactive workflows, decoupled logic, and real-time integrations.

## TuskLang Syntax

### Basic Event Definition
```tusk
# Define a custom event
user_registered: @event("user_registered")

# Emit an event when a task completes
on_task_complete: @event.emit("task_completed", { task_id: 123 })
```

### Event Handlers
```tusk
# Handle an event with a specific action
handle_signup: @event.on("user_registered", "send_welcome_email")

# Multiple handlers for the same event
multi_handlers: [
  @event.on("user_registered", "send_welcome_email"),
  @event.on("user_registered", "log_signup")
]
```

### Event Chaining
```tusk
# Chain events for complex workflows
workflow: [
  @event.on("order_placed", "process_payment"),
  @event.on("payment_processed", "send_receipt"),
  @event.on("receipt_sent", "update_order_status")
]
```

### Event with Conditions
```tusk
# Conditional event handling
conditional_event: @event.on("user_login", {
  action: "check_2fa",
  condition: @env("REQUIRE_2FA", true)
})
```

## JavaScript Integration

### Node.js Event Emitter
```javascript
const EventEmitter = require('events');
const tusklang = require('@tusklang/core');

const config = tusklang.parse(`
events: [
  @event.on("user_registered", "send_welcome_email"),
  @event.on("user_registered", "log_signup"),
  @event.on("order_placed", "process_payment")
]
`);

const eventBus = new EventEmitter();

const handlers = {
  send_welcome_email: (data) => {
    console.log('Sending welcome email to', data.user);
  },
  log_signup: (data) => {
    console.log('User signed up:', data.user);
  },
  process_payment: (data) => {
    console.log('Processing payment for order', data.orderId);
  }
};

// Register event handlers
config.events.forEach(eventConfig => {
  if (eventConfig.type === 'on') {
    eventBus.on(eventConfig.event, (data) => {
      if (handlers[eventConfig.action]) {
        handlers[eventConfig.action](data);
      }
    });
  }
});

// Emit events
// eventBus.emit('user_registered', { user: 'alice' });
```

### Browser Event Handling
```javascript
// Custom event system in the browser
const tusklang = require('@tusklang/core');

const config = tusklang.parse(`
browser_events: [
  @event.on("file_uploaded", "show_upload_success"),
  @event.on("user_logout", "clear_session")
]
`);

const handlers = {
  show_upload_success: (data) => {
    alert('Upload successful: ' + data.filename);
  },
  clear_session: () => {
    // Clear session data
    localStorage.clear();
    alert('Session cleared.');
  }
};

function registerBrowserEvents(events) {
  events.forEach(eventConfig => {
    if (eventConfig.type === 'on') {
      document.addEventListener(eventConfig.event, (e) => {
        if (handlers[eventConfig.action]) {
          handlers[eventConfig.action](e.detail);
        }
      });
    }
  });
}

registerBrowserEvents(config.browser_events);

// To emit an event:
// document.dispatchEvent(new CustomEvent('file_uploaded', { detail: { filename: 'report.pdf' } }));
```

## Advanced Usage Scenarios

### Real-Time Notification System
```tusk
# Notify users in real-time on specific events
notifications: [
  @event.on("message_received", "push_notification"),
  @event.on("system_alert", "send_sms_alert")
]
```

### Event-Driven Microservices
```tusk
# Microservice event bus
microservice_events: [
  @event.on("order_created", "inventory_update"),
  @event.on("inventory_updated", "shipping_schedule")
]
```

### Event Logging and Auditing
```tusk
# Log all critical events
event_logging: [
  @event.on("user_deleted", "log_deletion"),
  @event.on("permission_changed", "audit_permission_change")
]
```

## TypeScript Implementation

### Typed Event Bus
```typescript
interface EventConfig {
  type: 'on' | 'emit';
  event: string;
  action?: string;
  condition?: boolean;
}

class TypedEventBus {
  private handlers: Record<string, Function[]> = {};

  on(event: string, handler: Function) {
    if (!this.handlers[event]) this.handlers[event] = [];
    this.handlers[event].push(handler);
  }

  emit(event: string, data?: any) {
    (this.handlers[event] || []).forEach(handler => handler(data));
  }
}

// Usage
const bus = new TypedEventBus();
bus.on('user_registered', (data) => {
  // Handle user registration
});
bus.emit('user_registered', { user: 'alice' });
```

## Real-World Examples

### User Signup Workflow
```javascript
// User signup triggers multiple actions
eventBus.on('user_registered', (data) => {
  handlers.send_welcome_email(data);
  handlers.log_signup(data);
});
```

### File Upload Notification
```javascript
// Notify user after file upload
document.addEventListener('file_uploaded', (e) => {
  handlers.show_upload_success(e.detail);
});
```

## Performance Considerations
- Use asynchronous handlers for long-running tasks
- Avoid blocking the event loop in event handlers
- Debounce high-frequency events to prevent overload
- Monitor event handler execution time

## Security Notes
- Validate event data before processing
- Restrict sensitive actions to trusted events
- Log all critical event emissions and handlers
- Prevent event handler injection or override

## Best Practices
- Use descriptive event names
- Document all custom events and handlers
- Handle errors gracefully in event handlers
- Monitor and audit event flows

## Related Topics
- [@schedule Operator](./65-schedule-operator.md) - Flexible scheduling
- [@queue Operator](./63-queue-operator.md) - Message queue management
- [@stream Operator](./62-stream-operator.md) - Data streaming
- [Event-Driven Architecture](./27-event-driven.md) - Event handling 