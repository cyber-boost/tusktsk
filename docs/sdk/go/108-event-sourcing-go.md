# ⚡ Event Sourcing with TuskLang & Go

## Introduction
Event sourcing is the backbone of modern, auditable systems. TuskLang and Go let you build event-driven architectures with config-driven event stores, CQRS, and real-time projections—no more brittle state.

## Key Features
- **Event store management**
- **Aggregates and CQRS**
- **Projections and event replay**
- **Versioning and schema evolution**
- **Event-driven integration**
- **Saga patterns**

## Example: Event Store Config
```ini
[event_store]
driver: postgres
uri: @env("EVENT_STORE_URI")
append: @go("events.Append")
replay: @go("events.Replay")
```

## Go: Event Store Example
```go
package events
import (
  "database/sql"
  _ "github.com/lib/pq"
)
func Append(event Event) error {
  // Insert event into DB
}
func Replay(aggregateID string) []Event {
  // Query and replay events
}
```

## CQRS & Aggregates
- Use TuskLang config to separate read/write models
- Go implements command/query handlers

## Projections
```ini
[projection]
source: @go("events.Replay")
project: @go("projections.UpdateView")
```

## Versioning
- Store event schema version in each event
- Use Go for migration logic

## Event-Driven Integration
- Use @http, @file.write, or @go for side effects

## Saga Patterns
- Orchestrate long-running workflows with Go routines and TuskLang config

## Best Practices
- Store all state as events
- Use @metrics for event throughput
- Secure event store with @env.secure

## Troubleshooting
- Use Go logs for replay errors
- Monitor event lag with @metrics

## Conclusion
TuskLang + Go = event sourcing that’s robust, auditable, and ready for anything. 