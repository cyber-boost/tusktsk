# 📊 Event Sourcing with TuskLang Java

**"We don't bow to any king" - Event Sourcing Java Edition**

TuskLang Java enables building event-sourced applications with event stores, CQRS patterns, and event-driven architectures that provide complete audit trails and temporal data analysis.

## 🎯 Event Sourcing Architecture Overview

### Event Sourcing Configuration
```java
// event-sourcing-app.tsk
[event_sourcing]
name: "Event Sourced TuskLang App"
version: "2.0.0"
paradigm: "event_sourcing"
event_store: "postgresql"

[event_store]
type: "postgresql"
host: @env("EVENT_STORE_HOST", "localhost")
port: @env("EVENT_STORE_PORT", "5432")
database: @env("EVENT_STORE_DB", "event_store")
username: @env("EVENT_STORE_USER", "event_store_user")
password: @env.secure("EVENT_STORE_PASSWORD")
pool_size: 20
connection_timeout: "30s"

tables: {
    events: {
        name: "events"
        columns: [
            "id",
            "aggregate_id",
            "aggregate_type",
            "event_type",
            "event_data",
            "event_metadata",
            "version",
            "timestamp",
            "correlation_id",
            "causation_id"
        ]
        indexes: [
            "idx_events_aggregate_id",
            "idx_events_aggregate_type",
            "idx_events_timestamp",
            "idx_events_correlation_id"
        ]
    }
    
    snapshots: {
        name: "snapshots"
        columns: [
            "aggregate_id",
            "aggregate_type",
            "snapshot_data",
            "version",
            "timestamp"
        ]
        indexes: [
            "idx_snapshots_aggregate_id",
            "idx_snapshots_aggregate_type"
        ]
    }
    
    projections: {
        name: "projections"
        columns: [
            "projection_name",
            "projection_key",
            "projection_data",
            "version",
            "last_processed_event_id",
            "timestamp"
        ]
        indexes: [
            "idx_projections_name_key",
            "idx_projections_last_processed"
        ]
    }
}

[aggregates]
user: {
    name: "user"
    event_types: [
        "UserCreated",
        "UserUpdated",
        "UserDeleted",
        "UserActivated",
        "UserDeactivated"
    ]
    snapshot_frequency: 10
    max_events_per_aggregate: 1000
}

order: {
    name: "order"
    event_types: [
        "OrderCreated",
        "OrderItemAdded",
        "OrderItemRemoved",
        "OrderStatusChanged",
        "OrderCancelled",
        "OrderCompleted"
    ]
    snapshot_frequency: 5
    max_events_per_aggregate: 500
}

product: {
    name: "product"
    event_types: [
        "ProductCreated",
        "ProductUpdated",
        "ProductPriceChanged",
        "ProductStockUpdated",
        "ProductDiscontinued"
    ]
    snapshot_frequency: 20
    max_events_per_aggregate: 2000
}

[projections]
user_summary: {
    name: "user_summary"
    aggregate_type: "user"
    event_types: ["UserCreated", "UserUpdated", "UserDeleted"]
    projection_type: "read_model"
    storage: "postgresql"
    table_name: "user_summaries"
    columns: [
        "user_id",
        "email",
        "first_name",
        "last_name",
        "status",
        "created_at",
        "updated_at",
        "version"
    ]
}

order_summary: {
    name: "order_summary"
    aggregate_type: "order"
    event_types: ["OrderCreated", "OrderItemAdded", "OrderItemRemoved", "OrderStatusChanged", "OrderCancelled", "OrderCompleted"]
    projection_type: "read_model"
    storage: "postgresql"
    table_name: "order_summaries"
    columns: [
        "order_id",
        "user_id",
        "total_amount",
        "status",
        "item_count",
        "created_at",
        "updated_at",
        "version"
    ]
}

product_inventory: {
    name: "product_inventory"
    aggregate_type: "product"
    event_types: ["ProductCreated", "ProductStockUpdated", "ProductDiscontinued"]
    projection_type: "read_model"
    storage: "postgresql"
    table_name: "product_inventories"
    columns: [
        "product_id",
        "name",
        "price",
        "stock_quantity",
        "status",
        "created_at",
        "updated_at",
        "version"
    ]
}

[event_handlers]
user_created_handler: {
    name: "UserCreatedHandler"
    event_type: "UserCreated"
    handler_type: "projection"
    target_projection: "user_summary"
    async: true
    retry_policy: {
        max_attempts: 3
        backoff: "exponential"
        initial_delay: "1s"
    }
}

order_status_changed_handler: {
    name: "OrderStatusChangedHandler"
    event_type: "OrderStatusChanged"
    handler_type: "notification"
    target_service: "notification_service"
    async: true
    retry_policy: {
        max_attempts: 5
        backoff: "exponential"
        initial_delay: "2s"
    }
}

product_stock_updated_handler: {
    name: "ProductStockUpdatedHandler"
    event_type: "ProductStockUpdated"
    handler_type: "projection"
    target_projection: "product_inventory"
    async: true
    retry_policy: {
        max_attempts: 3
        backoff: "exponential"
        initial_delay: "1s"
    }
}

[read_models]
user_read_model: {
    name: "user_read_model"
    storage: "postgresql"
    table_name: "users"
    columns: [
        "id",
        "email",
        "first_name",
        "last_name",
        "status",
        "created_at",
        "updated_at"
    ]
    indexes: [
        "idx_users_email",
        "idx_users_status",
        "idx_users_created_at"
    ]
}

order_read_model: {
    name: "order_read_model"
    storage: "postgresql"
    table_name: "orders"
    columns: [
        "id",
        "user_id",
        "total_amount",
        "status",
        "created_at",
        "updated_at"
    ]
    indexes: [
        "idx_orders_user_id",
        "idx_orders_status",
        "idx_orders_created_at"
    ]
}

product_read_model: {
    name: "product_read_model"
    storage: "postgresql"
    table_name: "products"
    columns: [
        "id",
        "name",
        "description",
        "price",
        "stock_quantity",
        "status",
        "created_at",
        "updated_at"
    ]
    indexes: [
        "idx_products_name",
        "idx_products_status",
        "idx_products_price"
    ]
}

[event_bus]
type: "kafka"
bootstrap_servers: [
    @env("KAFKA_BROKER_1", "localhost:9092"),
    @env("KAFKA_BROKER_2", "localhost:9093")
]
topics: {
    user_events: {
        name: "user-events"
        partitions: 10
        replication_factor: 3
        retention: "7d"
    }
    order_events: {
        name: "order-events"
        partitions: 20
        replication_factor: 3
        retention: "30d"
    }
    product_events: {
        name: "product-events"
        partitions: 15
        replication_factor: 3
        retention: "7d"
    }
}

[monitoring]
event_store_metrics: {
    enabled: true
    metrics: [
        "events_per_second",
        "aggregate_versions",
        "projection_lag",
        "snapshot_frequency"
    ]
}

projection_metrics: {
    enabled: true
    metrics: [
        "projection_processing_time",
        "projection_errors",
        "projection_lag",
        "read_model_freshness"
    ]
}
```

## 📊 Event Store Implementation

### Event Store Configuration
```java
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;
import org.tusklang.java.annotations.TuskConfig;
import java.util.UUID;

@Configuration
@TuskConfig
public class EventStoreConfiguration {
    
    private final EventSourcingConfig eventSourcingConfig;
    
    public EventStoreConfiguration(EventSourcingConfig eventSourcingConfig) {
        this.eventSourcingConfig = eventSourcingConfig;
    }
    
    @Bean
    public EventStore eventStore(JdbcTemplate jdbcTemplate) {
        EventStoreConfig config = eventSourcingConfig.getEventStore();
        return new PostgresEventStore(jdbcTemplate, config);
    }
    
    @Bean
    public EventPublisher eventPublisher() {
        return new KafkaEventPublisher(eventSourcingConfig.getEventBus());
    }
    
    @Bean
    public SnapshotStore snapshotStore(JdbcTemplate jdbcTemplate) {
        return new PostgresSnapshotStore(jdbcTemplate);
    }
    
    @Bean
    public ProjectionStore projectionStore(JdbcTemplate jdbcTemplate) {
        return new PostgresProjectionStore(jdbcTemplate);
    }
}

@TuskConfig
public class EventSourcingConfig {
    
    private String name;
    private String version;
    private String paradigm;
    private String eventStore;
    private EventStoreConfig eventStoreConfig;
    private Map<String, AggregateConfig> aggregates;
    private Map<String, ProjectionConfig> projections;
    private Map<String, EventHandlerConfig> eventHandlers;
    private Map<String, ReadModelConfig> readModels;
    private EventBusConfig eventBus;
    private MonitoringConfig monitoring;
    
    // Getters and setters
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    
    public String getVersion() { return version; }
    public void setVersion(String version) { this.version = version; }
    
    public String getParadigm() { return paradigm; }
    public void setParadigm(String paradigm) { this.paradigm = paradigm; }
    
    public String getEventStore() { return eventStore; }
    public void setEventStore(String eventStore) { this.eventStore = eventStore; }
    
    public EventStoreConfig getEventStoreConfig() { return eventStoreConfig; }
    public void setEventStoreConfig(EventStoreConfig eventStoreConfig) { this.eventStoreConfig = eventStoreConfig; }
    
    public Map<String, AggregateConfig> getAggregates() { return aggregates; }
    public void setAggregates(Map<String, AggregateConfig> aggregates) { this.aggregates = aggregates; }
    
    public Map<String, ProjectionConfig> getProjections() { return projections; }
    public void setProjections(Map<String, ProjectionConfig> projections) { this.projections = projections; }
    
    public Map<String, EventHandlerConfig> getEventHandlers() { return eventHandlers; }
    public void setEventHandlers(Map<String, EventHandlerConfig> eventHandlers) { this.eventHandlers = eventHandlers; }
    
    public Map<String, ReadModelConfig> getReadModels() { return readModels; }
    public void setReadModels(Map<String, ReadModelConfig> readModels) { this.readModels = readModels; }
    
    public EventBusConfig getEventBus() { return eventBus; }
    public void setEventBus(EventBusConfig eventBus) { this.eventBus = eventBus; }
    
    public MonitoringConfig getMonitoring() { return monitoring; }
    public void setMonitoring(MonitoringConfig monitoring) { this.monitoring = monitoring; }
}

@TuskConfig
public class EventStoreConfig {
    
    private String type;
    private String host;
    private int port;
    private String database;
    private String username;
    private String password;
    private int poolSize;
    private String connectionTimeout;
    private Map<String, TableConfig> tables;
    
    // Getters and setters
    public String getType() { return type; }
    public void setType(String type) { this.type = type; }
    
    public String getHost() { return host; }
    public void setHost(String host) { this.host = host; }
    
    public int getPort() { return port; }
    public void setPort(int port) { this.port = port; }
    
    public String getDatabase() { return database; }
    public void setDatabase(String database) { this.database = database; }
    
    public String getUsername() { return username; }
    public void setUsername(String username) { this.username = username; }
    
    public String getPassword() { return password; }
    public void setPassword(String password) { this.password = password; }
    
    public int getPoolSize() { return poolSize; }
    public void setPoolSize(int poolSize) { this.poolSize = poolSize; }
    
    public String getConnectionTimeout() { return connectionTimeout; }
    public void setConnectionTimeout(String connectionTimeout) { this.connectionTimeout = connectionTimeout; }
    
    public Map<String, TableConfig> getTables() { return tables; }
    public void setTables(Map<String, TableConfig> tables) { this.tables = tables; }
}
```

### Event Store Implementation
```java
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Repository;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

public interface EventStore {
    void appendEvents(String aggregateId, String aggregateType, List<Event> events, long expectedVersion);
    List<Event> getEvents(String aggregateId, long fromVersion);
    List<Event> getEventsByType(String eventType, long fromTimestamp);
    Optional<Snapshot> getLatestSnapshot(String aggregateId);
    void saveSnapshot(Snapshot snapshot);
}

@Repository
public class PostgresEventStore implements EventStore {
    
    private final JdbcTemplate jdbcTemplate;
    private final EventStoreConfig config;
    private final ObjectMapper objectMapper;
    
    public PostgresEventStore(JdbcTemplate jdbcTemplate, EventStoreConfig config) {
        this.jdbcTemplate = jdbcTemplate;
        this.config = config;
        this.objectMapper = new ObjectMapper();
        objectMapper.registerModule(new JavaTimeModule());
    }
    
    @Override
    public void appendEvents(String aggregateId, String aggregateType, 
                           List<Event> events, long expectedVersion) {
        String sql = """
            INSERT INTO events (id, aggregate_id, aggregate_type, event_type, 
                              event_data, event_metadata, version, timestamp, 
                              correlation_id, causation_id)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """;
        
        for (Event event : events) {
            try {
                String eventData = objectMapper.writeValueAsString(event.getData());
                String eventMetadata = objectMapper.writeValueAsString(event.getMetadata());
                
                jdbcTemplate.update(sql,
                    UUID.randomUUID().toString(),
                    aggregateId,
                    aggregateType,
                    event.getType(),
                    eventData,
                    eventMetadata,
                    expectedVersion + events.indexOf(event) + 1,
                    event.getTimestamp(),
                    event.getCorrelationId(),
                    event.getCausationId()
                );
                
            } catch (Exception e) {
                log.error("Error appending event: {}", event.getType(), e);
                throw new EventStoreException("Failed to append event", e);
            }
        }
    }
    
    @Override
    public List<Event> getEvents(String aggregateId, long fromVersion) {
        String sql = """
            SELECT id, aggregate_id, aggregate_type, event_type, event_data, 
                   event_metadata, version, timestamp, correlation_id, causation_id
            FROM events
            WHERE aggregate_id = ? AND version > ?
            ORDER BY version ASC
            """;
        
        return jdbcTemplate.query(sql, new Object[]{aggregateId, fromVersion}, (rs, rowNum) -> {
            try {
                Event event = new Event();
                event.setId(rs.getString("id"));
                event.setAggregateId(rs.getString("aggregate_id"));
                event.setAggregateType(rs.getString("aggregate_type"));
                event.setType(rs.getString("event_type"));
                event.setData(objectMapper.readValue(rs.getString("event_data"), Object.class));
                event.setMetadata(objectMapper.readValue(rs.getString("event_metadata"), Map.class));
                event.setVersion(rs.getLong("version"));
                event.setTimestamp(rs.getTimestamp("timestamp").toInstant());
                event.setCorrelationId(rs.getString("correlation_id"));
                event.setCausationId(rs.getString("causation_id"));
                return event;
            } catch (Exception e) {
                log.error("Error deserializing event", e);
                throw new EventStoreException("Failed to deserialize event", e);
            }
        });
    }
    
    @Override
    public List<Event> getEventsByType(String eventType, long fromTimestamp) {
        String sql = """
            SELECT id, aggregate_id, aggregate_type, event_type, event_data, 
                   event_metadata, version, timestamp, correlation_id, causation_id
            FROM events
            WHERE event_type = ? AND timestamp >= ?
            ORDER BY timestamp ASC
            """;
        
        return jdbcTemplate.query(sql, new Object[]{eventType, fromTimestamp}, (rs, rowNum) -> {
            try {
                Event event = new Event();
                event.setId(rs.getString("id"));
                event.setAggregateId(rs.getString("aggregate_id"));
                event.setAggregateType(rs.getString("aggregate_type"));
                event.setType(rs.getString("event_type"));
                event.setData(objectMapper.readValue(rs.getString("event_data"), Object.class));
                event.setMetadata(objectMapper.readValue(rs.getString("event_metadata"), Map.class));
                event.setVersion(rs.getLong("version"));
                event.setTimestamp(rs.getTimestamp("timestamp").toInstant());
                event.setCorrelationId(rs.getString("correlation_id"));
                event.setCausationId(rs.getString("causation_id"));
                return event;
            } catch (Exception e) {
                log.error("Error deserializing event", e);
                throw new EventStoreException("Failed to deserialize event", e);
            }
        });
    }
    
    @Override
    public Optional<Snapshot> getLatestSnapshot(String aggregateId) {
        String sql = """
            SELECT aggregate_id, aggregate_type, snapshot_data, version, timestamp
            FROM snapshots
            WHERE aggregate_id = ?
            ORDER BY version DESC
            LIMIT 1
            """;
        
        try {
            Snapshot snapshot = jdbcTemplate.queryForObject(sql, new Object[]{aggregateId}, (rs, rowNum) -> {
                try {
                    Snapshot snap = new Snapshot();
                    snap.setAggregateId(rs.getString("aggregate_id"));
                    snap.setAggregateType(rs.getString("aggregate_type"));
                    snap.setData(objectMapper.readValue(rs.getString("snapshot_data"), Object.class));
                    snap.setVersion(rs.getLong("version"));
                    snap.setTimestamp(rs.getTimestamp("timestamp").toInstant());
                    return snap;
                } catch (Exception e) {
                    log.error("Error deserializing snapshot", e);
                    throw new EventStoreException("Failed to deserialize snapshot", e);
                }
            });
            return Optional.ofNullable(snapshot);
        } catch (EmptyResultDataAccessException e) {
            return Optional.empty();
        }
    }
    
    @Override
    public void saveSnapshot(Snapshot snapshot) {
        String sql = """
            INSERT INTO snapshots (aggregate_id, aggregate_type, snapshot_data, version, timestamp)
            VALUES (?, ?, ?, ?, ?)
            """;
        
        try {
            String snapshotData = objectMapper.writeValueAsString(snapshot.getData());
            
            jdbcTemplate.update(sql,
                snapshot.getAggregateId(),
                snapshot.getAggregateType(),
                snapshotData,
                snapshot.getVersion(),
                snapshot.getTimestamp()
            );
            
        } catch (Exception e) {
            log.error("Error saving snapshot", e);
            throw new EventStoreException("Failed to save snapshot", e);
        }
    }
}
```

## 🔄 Aggregate Implementation

### User Aggregate
```java
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

public abstract class Aggregate {
    
    private String id;
    private long version;
    private List<Event> uncommittedEvents = new ArrayList<>();
    
    public String getId() { return id; }
    public void setId(String id) { this.id = id; }
    
    public long getVersion() { return version; }
    public void setVersion(long version) { this.version = version; }
    
    public List<Event> getUncommittedEvents() { return uncommittedEvents; }
    
    protected void apply(Event event) {
        uncommittedEvents.add(event);
        handle(event);
    }
    
    protected abstract void handle(Event event);
    
    public void markEventsAsCommitted() {
        uncommittedEvents.clear();
    }
    
    public void loadFromHistory(List<Event> events) {
        for (Event event : events) {
            handle(event);
            version = event.getVersion();
        }
    }
}

public class UserAggregate extends Aggregate {
    
    private String email;
    private String firstName;
    private String lastName;
    private UserStatus status;
    private Instant createdAt;
    private Instant updatedAt;
    
    public UserAggregate() {}
    
    public UserAggregate(String email, String firstName, String lastName) {
        this.id = UUID.randomUUID().toString();
        this.email = email;
        this.firstName = firstName;
        this.lastName = lastName;
        this.status = UserStatus.ACTIVE;
        this.createdAt = Instant.now();
        this.updatedAt = Instant.now();
        
        apply(new UserCreatedEvent(this.id, email, firstName, lastName, status, createdAt));
    }
    
    public void updateUser(String firstName, String lastName) {
        if (status == UserStatus.DELETED) {
            throw new IllegalStateException("Cannot update deleted user");
        }
        
        this.firstName = firstName;
        this.lastName = lastName;
        this.updatedAt = Instant.now();
        
        apply(new UserUpdatedEvent(this.id, firstName, lastName, updatedAt));
    }
    
    public void deleteUser() {
        if (status == UserStatus.DELETED) {
            throw new IllegalStateException("User is already deleted");
        }
        
        this.status = UserStatus.DELETED;
        this.updatedAt = Instant.now();
        
        apply(new UserDeletedEvent(this.id, updatedAt));
    }
    
    public void activateUser() {
        if (status == UserStatus.ACTIVE) {
            throw new IllegalStateException("User is already active");
        }
        
        this.status = UserStatus.ACTIVE;
        this.updatedAt = Instant.now();
        
        apply(new UserActivatedEvent(this.id, updatedAt));
    }
    
    public void deactivateUser() {
        if (status == UserStatus.INACTIVE) {
            throw new IllegalStateException("User is already inactive");
        }
        
        this.status = UserStatus.INACTIVE;
        this.updatedAt = Instant.now();
        
        apply(new UserDeactivatedEvent(this.id, updatedAt));
    }
    
    @Override
    protected void handle(Event event) {
        switch (event.getType()) {
            case "UserCreated":
                handleUserCreated((UserCreatedEvent) event.getData());
                break;
            case "UserUpdated":
                handleUserUpdated((UserUpdatedEvent) event.getData());
                break;
            case "UserDeleted":
                handleUserDeleted((UserDeletedEvent) event.getData());
                break;
            case "UserActivated":
                handleUserActivated((UserActivatedEvent) event.getData());
                break;
            case "UserDeactivated":
                handleUserDeactivated((UserDeactivatedEvent) event.getData());
                break;
            default:
                throw new UnsupportedEventException("Unsupported event type: " + event.getType());
        }
    }
    
    private void handleUserCreated(UserCreatedEvent event) {
        this.id = event.getUserId();
        this.email = event.getEmail();
        this.firstName = event.getFirstName();
        this.lastName = event.getLastName();
        this.status = event.getStatus();
        this.createdAt = event.getCreatedAt();
        this.updatedAt = event.getCreatedAt();
    }
    
    private void handleUserUpdated(UserUpdatedEvent event) {
        this.firstName = event.getFirstName();
        this.lastName = event.getLastName();
        this.updatedAt = event.getUpdatedAt();
    }
    
    private void handleUserDeleted(UserDeletedEvent event) {
        this.status = UserStatus.DELETED;
        this.updatedAt = event.getDeletedAt();
    }
    
    private void handleUserActivated(UserActivatedEvent event) {
        this.status = UserStatus.ACTIVE;
        this.updatedAt = event.getActivatedAt();
    }
    
    private void handleUserDeactivated(UserDeactivatedEvent event) {
        this.status = UserStatus.INACTIVE;
        this.updatedAt = event.getDeactivatedAt();
    }
    
    // Getters
    public String getEmail() { return email; }
    public String getFirstName() { return firstName; }
    public String getLastName() { return lastName; }
    public UserStatus getStatus() { return status; }
    public Instant getCreatedAt() { return createdAt; }
    public Instant getUpdatedAt() { return updatedAt; }
}

public enum UserStatus {
    ACTIVE, INACTIVE, DELETED
}
```

## 📊 Projection Implementation

### User Summary Projection
```java
import org.springframework.stereotype.Component;
import org.springframework.jdbc.core.JdbcTemplate;
import java.util.List;

@Component
public class UserSummaryProjection implements EventHandler {
    
    private final JdbcTemplate jdbcTemplate;
    private final ObjectMapper objectMapper;
    
    public UserSummaryProjection(JdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
        this.objectMapper = new ObjectMapper();
    }
    
    @Override
    public void handle(Event event) {
        switch (event.getType()) {
            case "UserCreated":
                handleUserCreated((UserCreatedEvent) event.getData());
                break;
            case "UserUpdated":
                handleUserUpdated((UserUpdatedEvent) event.getData());
                break;
            case "UserDeleted":
                handleUserDeleted((UserDeletedEvent) event.getData());
                break;
            case "UserActivated":
                handleUserActivated((UserActivatedEvent) event.getData());
                break;
            case "UserDeactivated":
                handleUserDeactivated((UserDeactivatedEvent) event.getData());
                break;
        }
    }
    
    private void handleUserCreated(UserCreatedEvent event) {
        String sql = """
            INSERT INTO user_summaries (user_id, email, first_name, last_name, status, created_at, updated_at, version)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?)
            """;
        
        jdbcTemplate.update(sql,
            event.getUserId(),
            event.getEmail(),
            event.getFirstName(),
            event.getLastName(),
            event.getStatus().name(),
            event.getCreatedAt(),
            event.getCreatedAt(),
            1L
        );
    }
    
    private void handleUserUpdated(UserUpdatedEvent event) {
        String sql = """
            UPDATE user_summaries
            SET first_name = ?, last_name = ?, updated_at = ?, version = version + 1
            WHERE user_id = ?
            """;
        
        jdbcTemplate.update(sql,
            event.getFirstName(),
            event.getLastName(),
            event.getUpdatedAt(),
            event.getUserId()
        );
    }
    
    private void handleUserDeleted(UserDeletedEvent event) {
        String sql = """
            UPDATE user_summaries
            SET status = 'DELETED', updated_at = ?, version = version + 1
            WHERE user_id = ?
            """;
        
        jdbcTemplate.update(sql,
            event.getDeletedAt(),
            event.getUserId()
        );
    }
    
    private void handleUserActivated(UserActivatedEvent event) {
        String sql = """
            UPDATE user_summaries
            SET status = 'ACTIVE', updated_at = ?, version = version + 1
            WHERE user_id = ?
            """;
        
        jdbcTemplate.update(sql,
            event.getActivatedAt(),
            event.getUserId()
        );
    }
    
    private void handleUserDeactivated(UserDeactivatedEvent event) {
        String sql = """
            UPDATE user_summaries
            SET status = 'INACTIVE', updated_at = ?, version = version + 1
            WHERE user_id = ?
            """;
        
        jdbcTemplate.update(sql,
            event.getDeactivatedAt(),
            event.getUserId()
        );
    }
    
    public List<UserSummary> getAllUsers() {
        String sql = "SELECT * FROM user_summaries ORDER BY created_at DESC";
        
        return jdbcTemplate.query(sql, (rs, rowNum) -> {
            UserSummary summary = new UserSummary();
            summary.setUserId(rs.getString("user_id"));
            summary.setEmail(rs.getString("email"));
            summary.setFirstName(rs.getString("first_name"));
            summary.setLastName(rs.getString("last_name"));
            summary.setStatus(UserStatus.valueOf(rs.getString("status")));
            summary.setCreatedAt(rs.getTimestamp("created_at").toInstant());
            summary.setUpdatedAt(rs.getTimestamp("updated_at").toInstant());
            summary.setVersion(rs.getLong("version"));
            return summary;
        });
    }
    
    public Optional<UserSummary> getUserById(String userId) {
        String sql = "SELECT * FROM user_summaries WHERE user_id = ?";
        
        try {
            UserSummary summary = jdbcTemplate.queryForObject(sql, new Object[]{userId}, (rs, rowNum) -> {
                UserSummary userSummary = new UserSummary();
                userSummary.setUserId(rs.getString("user_id"));
                userSummary.setEmail(rs.getString("email"));
                userSummary.setFirstName(rs.getString("first_name"));
                userSummary.setLastName(rs.getString("last_name"));
                userSummary.setStatus(UserStatus.valueOf(rs.getString("status")));
                userSummary.setCreatedAt(rs.getTimestamp("created_at").toInstant());
                userSummary.setUpdatedAt(rs.getTimestamp("updated_at").toInstant());
                userSummary.setVersion(rs.getLong("version"));
                return userSummary;
            });
            return Optional.ofNullable(summary);
        } catch (EmptyResultDataAccessException e) {
            return Optional.empty();
        }
    }
}
```

## 🔧 Best Practices

### 1. Event Design
- Make events immutable and serializable
- Use descriptive event names
- Include all necessary data in events
- Version events for backward compatibility

### 2. Aggregate Design
- Keep aggregates small and focused
- Use business rules to validate state changes
- Implement proper error handling
- Use snapshots for performance optimization

### 3. Projection Design
- Design projections for specific use cases
- Use denormalized data for read performance
- Implement proper error handling and retry logic
- Monitor projection lag and performance

### 4. Event Store
- Use proper indexing for performance
- Implement event versioning
- Use snapshots for large aggregates
- Monitor event store performance

### 5. CQRS Implementation
- Separate read and write models
- Use appropriate storage for read models
- Implement eventual consistency
- Monitor read model freshness

## 🎯 Summary

TuskLang Java event sourcing provides:

- **Event Store**: PostgreSQL-based event storage with proper indexing
- **Aggregate Pattern**: Domain-driven aggregates with event handling
- **Projections**: Read models for efficient querying
- **CQRS**: Command and Query Responsibility Segregation
- **Event-Driven Architecture**: Asynchronous event processing

The combination of TuskLang's executable configuration with Java's event sourcing capabilities creates a powerful platform for building audit trails, temporal analysis, and event-driven applications.

**"We don't bow to any king" - Build event-sourced applications with complete audit trails!** 