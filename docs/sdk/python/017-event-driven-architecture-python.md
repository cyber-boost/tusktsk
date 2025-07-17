# ⚡ Event-Driven Architecture - Python

**"We don't bow to any king" - Event-Driven Edition**

TuskLang empowers you to build scalable, decoupled event-driven systems with robust event publishing, subscription, and processing capabilities.

## 🚀 Event-Driven Concepts

### What is Event-Driven Architecture?
- **Event Producers**: Components that generate events (e.g., user actions, system changes).
- **Event Consumers**: Components that react to events (e.g., services, workflows).
- **Event Bus/Broker**: Middleware for delivering events (e.g., Redis, RabbitMQ, Kafka).
- **Event Types**: Domain events, integration events, system events.

## 🏗️ Event Publishing & Subscription

### Basic Event Publishing

```python
from tsk import TSK
import redis
import json
import time

# Event configuration
event_config = TSK.from_string("""
[event_system]
# Event bus configuration
event_bus: @env("EVENT_BUS", "redis://localhost:6379/0")
event_channel: "tusk_events"

# Publish event
publish_event_fujsen = '''
def publish_event(event_type, payload, metadata=None):
    r = redis.from_url(event_bus)
    event = {
        'type': event_type,
        'payload': payload,
        'metadata': metadata or {},
        'timestamp': time.time()
    }
    r.publish(event_channel, json.dumps(event))
    return event
'''

# Subscribe to events
subscribe_events_fujsen = '''
def subscribe_events(handler, event_types=None):
    r = redis.from_url(event_bus)
    pubsub = r.pubsub()
    pubsub.subscribe(event_channel)
    for message in pubsub.listen():
        if message['type'] == 'message':
            event = json.loads(message['data'])
            if event_types and event['type'] not in event_types:
                continue
            handler(event)
'''
""")

# Example event handler
def handle_event(event):
    print(f"Received event: {event['type']} at {event['timestamp']}")
    # Process event payload

# Publishing an event
event_config.execute_fujsen('event_system', 'publish_event', 'user_registered', {'user_id': 123})

# Subscribing to events (in a background thread or process)
# event_config.execute_fujsen('event_system', 'subscribe_events', handle_event, ['user_registered'])
```

## 🧩 Event Processing Patterns

### Synchronous vs. Asynchronous Processing
- **Synchronous**: Immediate processing, suitable for simple workflows.
- **Asynchronous**: Decoupled, scalable, use for heavy or distributed workloads.

### Event Sourcing

```python
# Event sourcing configuration
event_sourcing = TSK.from_string("""
[event_sourcing]
# Store event
store_event_fujsen = '''
def store_event(event_type, payload):
    execute("""
        INSERT INTO event_store (event_type, payload, timestamp)
        VALUES (?, ?, datetime('now'))
    """, event_type, json.dumps(payload))
    return True
'''

# Replay events
replay_events_fujsen = '''
def replay_events(event_type=None, since=None):
    query_str = "SELECT event_type, payload, timestamp FROM event_store"
    params = []
    if event_type:
        query_str += " WHERE event_type = ?"
        params.append(event_type)
    if since:
        query_str += " AND timestamp > ?" if event_type else " WHERE timestamp > ?"
        params.append(since)
    events = query(query_str, *params)
    return [{
        'event_type': row[0],
        'payload': json.loads(row[1]),
        'timestamp': row[2]
    } for row in events]
'''
""")
```

## 🔄 Event-Driven Workflows

### Choreography vs. Orchestration
- **Choreography**: Services react to events independently (loose coupling).
- **Orchestration**: Central coordinator directs event flow (more control).

### Example: Choreographed Workflow

```python
# Service A: Publishes event when user registers
# Service B: Subscribes to 'user_registered' and sends welcome email
# Service C: Subscribes to 'user_registered' and creates user profile

# In Service B:
def handle_user_registered(event):
    user_id = event['payload']['user_id']
    send_welcome_email(user_id)

# In Service C:
def handle_user_registered(event):
    user_id = event['payload']['user_id']
    create_user_profile(user_id)
```

## 🚦 Event Reliability & Delivery

### At-Least-Once vs. Exactly-Once
- **At-Least-Once**: Most common, may require idempotency.
- **Exactly-Once**: Harder, requires deduplication and transactional guarantees.

### Dead Letter Queues

```python
# Dead letter queue configuration
dlq_config = TSK.from_string("""
[dead_letter_queue]
dlq_channel: "tusk_dlq"

# Publish to DLQ
publish_dlq_fujsen = '''
def publish_dlq(event, reason):
    r = redis.from_url(event_bus)
    dlq_event = {
        'original_event': event,
        'reason': reason,
        'timestamp': time.time()
    }
    r.publish(dlq_channel, json.dumps(dlq_event))
    return dlq_event
'''
""")
```

## 📊 Event Monitoring & Tracing

### Event Metrics
- Event throughput, processing latency, error rates, DLQ counts.

### Distributed Tracing for Events
- Correlate event flows across services using trace IDs.

## 🎯 Event-Driven Best Practices

- Use clear event naming conventions
- Ensure idempotency in event handlers
- Monitor event delivery and failures
- Use dead letter queues for failed events
- Document event contracts and schemas
- Prefer asynchronous processing for scalability

## 🚀 Next Steps

1. **Define your event types and contracts**
2. **Implement event publishing and subscription**
3. **Set up event storage and replay**
4. **Monitor event flows and failures**
5. **Document your event-driven architecture**

---

**"We don't bow to any king"** - TuskLang empowers you to build scalable, decoupled event-driven systems. Publish, subscribe, process, and monitor events with confidence! 