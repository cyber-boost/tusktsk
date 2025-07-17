# 🎯 CQRS with TuskLang Java

**"We don't bow to any king" - CQRS Java Edition**

TuskLang Java enables building CQRS (Command Query Responsibility Segregation) applications that separate read and write operations for optimal performance, scalability, and maintainability.

## 🎯 CQRS Architecture Overview

### CQRS Configuration
```java
// cqrs-app.tsk
[cqrs_system]
name: "CQRS TuskLang App"
version: "2.0.0"
paradigm: "cqrs"
separation: "command_query"

[command_side]
name: "command-side"
database: "write_database"
event_store: "event_store"
command_handlers: {
    user_commands: {
        package: "com.tusklang.cqrs.commands.user"
        handlers: [
            "CreateUserCommandHandler",
            "UpdateUserCommandHandler",
            "DeleteUserCommandHandler",
            "ActivateUserCommandHandler",
            "DeactivateUserCommandHandler"
        ]
    }
    order_commands: {
        package: "com.tusklang.cqrs.commands.order"
        handlers: [
            "CreateOrderCommandHandler",
            "AddOrderItemCommandHandler",
            "RemoveOrderItemCommandHandler",
            "UpdateOrderStatusCommandHandler",
            "CancelOrderCommandHandler"
        ]
    }
    product_commands: {
        package: "com.tusklang.cqrs.commands.product"
        handlers: [
            "CreateProductCommandHandler",
            "UpdateProductCommandHandler",
            "UpdateProductPriceCommandHandler",
            "UpdateProductStockCommandHandler",
            "DiscontinueProductCommandHandler"
        ]
    }
}

[query_side]
name: "query-side"
database: "read_database"
projections: {
    user_projections: {
        package: "com.tusklang.cqrs.projections.user"
        handlers: [
            "UserSummaryProjection",
            "UserDetailsProjection",
            "UserSearchProjection"
        ]
        read_models: [
            "UserSummary",
            "UserDetails",
            "UserSearch"
        ]
    }
    order_projections: {
        package: "com.tusklang.cqrs.projections.order"
        handlers: [
            "OrderSummaryProjection",
            "OrderDetailsProjection",
            "OrderHistoryProjection"
        ]
        read_models: [
            "OrderSummary",
            "OrderDetails",
            "OrderHistory"
        ]
    }
    product_projections: {
        package: "com.tusklang.cqrs.projections.product"
        handlers: [
            "ProductSummaryProjection",
            "ProductDetailsProjection",
            "ProductInventoryProjection"
        ]
        read_models: [
            "ProductSummary",
            "ProductDetails",
            "ProductInventory"
        ]
    }
}

[databases]
write_database: {
    type: "postgresql"
    host: @env("WRITE_DB_HOST", "localhost")
    port: @env("WRITE_DB_PORT", "5432")
    database: @env("WRITE_DB_NAME", "write_db")
    username: @env("WRITE_DB_USER", "write_user")
    password: @env.secure("WRITE_DB_PASSWORD")
    pool_size: 20
    connection_timeout: "30s"
    purpose: "command_processing"
}

read_database: {
    type: "postgresql"
    host: @env("READ_DB_HOST", "localhost")
    port: @env("READ_DB_PORT", "5432")
    database: @env("READ_DB_NAME", "read_db")
    username: @env("READ_DB_USER", "read_user")
    password: @env.secure("READ_DB_PASSWORD")
    pool_size: 50
    connection_timeout: "30s"
    purpose: "query_processing"
    read_replicas: [
        {
            host: @env("READ_REPLICA_1", "read-replica-1")
            port: @env("READ_DB_PORT", "5432")
            weight: 0.5
        },
        {
            host: @env("READ_REPLICA_2", "read-replica-2")
            port: @env("READ_DB_PORT", "5432")
            weight: 0.5
        }
    ]
}

event_store: {
    type: "postgresql"
    host: @env("EVENT_STORE_HOST", "localhost")
    port: @env("EVENT_STORE_PORT", "5432")
    database: @env("EVENT_STORE_DB", "event_store")
    username: @env("EVENT_STORE_USER", "event_store_user")
    password: @env.secure("EVENT_STORE_PASSWORD")
    pool_size: 15
    connection_timeout: "30s"
    purpose: "event_storage"
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

[projection_processing]
mode: "asynchronous"
batch_size: 100
processing_interval: "1s"
error_handling: {
    retry_attempts: 3
    retry_backoff: "exponential"
    dead_letter_queue: "projection-dlq"
}

[consistency]
eventual_consistency: {
    enabled: true
    max_lag: "5s"
    monitoring: true
}

strong_consistency: {
    enabled: false
    use_cases: ["critical_operations"]
}

[monitoring]
command_metrics: {
    enabled: true
    metrics: [
        "command_processing_time",
        "command_success_rate",
        "command_errors",
        "aggregate_versions"
    ]
}

query_metrics: {
    enabled: true
    metrics: [
        "query_response_time",
        "query_cache_hit_rate",
        "query_errors",
        "read_model_freshness"
    ]
}

projection_metrics: {
    enabled: true
    metrics: [
        "projection_processing_time",
        "projection_lag",
        "projection_errors",
        "event_processing_rate"
    ]
}
```

## ⚡ Command Side Implementation

### Command Configuration
```java
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.tusklang.java.annotations.TuskConfig;
import java.util.Map;

@Configuration
@TuskConfig
public class CQRSConfiguration {
    
    private final CQRSConfig cqrsConfig;
    
    public CQRSConfiguration(CQRSConfig cqrsConfig) {
        this.cqrsConfig = cqrsConfig;
    }
    
    @Bean
    public CommandBus commandBus() {
        return new SimpleCommandBus();
    }
    
    @Bean
    public QueryBus queryBus() {
        return new SimpleQueryBus();
    }
    
    @Bean
    public EventBus eventBus() {
        EventBusConfig config = cqrsConfig.getEventBus();
        return new KafkaEventBus(config);
    }
    
    @Bean
    public CommandGateway commandGateway(CommandBus commandBus) {
        return new DefaultCommandGateway(commandBus);
    }
    
    @Bean
    public QueryGateway queryGateway(QueryBus queryBus) {
        return new DefaultQueryGateway(queryBus);
    }
}

@TuskConfig
public class CQRSConfig {
    
    private String name;
    private String version;
    private String paradigm;
    private String separation;
    private CommandSideConfig commandSide;
    private QuerySideConfig querySide;
    private Map<String, DatabaseConfig> databases;
    private EventBusConfig eventBus;
    private ProjectionProcessingConfig projectionProcessing;
    private ConsistencyConfig consistency;
    private MonitoringConfig monitoring;
    
    // Getters and setters
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    
    public String getVersion() { return version; }
    public void setVersion(String version) { this.version = version; }
    
    public String getParadigm() { return paradigm; }
    public void setParadigm(String paradigm) { this.paradigm = paradigm; }
    
    public String getSeparation() { return separation; }
    public void setSeparation(String separation) { this.separation = separation; }
    
    public CommandSideConfig getCommandSide() { return commandSide; }
    public void setCommandSide(CommandSideConfig commandSide) { this.commandSide = commandSide; }
    
    public QuerySideConfig getQuerySide() { return querySide; }
    public void setQuerySide(QuerySideConfig querySide) { this.querySide = querySide; }
    
    public Map<String, DatabaseConfig> getDatabases() { return databases; }
    public void setDatabases(Map<String, DatabaseConfig> databases) { this.databases = databases; }
    
    public EventBusConfig getEventBus() { return eventBus; }
    public void setEventBus(EventBusConfig eventBus) { this.eventBus = eventBus; }
    
    public ProjectionProcessingConfig getProjectionProcessing() { return projectionProcessing; }
    public void setProjectionProcessing(ProjectionProcessingConfig projectionProcessing) { this.projectionProcessing = projectionProcessing; }
    
    public ConsistencyConfig getConsistency() { return consistency; }
    public void setConsistency(ConsistencyConfig consistency) { this.consistency = consistency; }
    
    public MonitoringConfig getMonitoring() { return monitoring; }
    public void setMonitoring(MonitoringConfig monitoring) { this.monitoring = monitoring; }
}

@TuskConfig
public class CommandSideConfig {
    
    private String name;
    private String database;
    private String eventStore;
    private Map<String, CommandHandlersConfig> commandHandlers;
    
    // Getters and setters
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    
    public String getDatabase() { return database; }
    public void setDatabase(String database) { this.database = database; }
    
    public String getEventStore() { return eventStore; }
    public void setEventStore(String eventStore) { this.eventStore = eventStore; }
    
    public Map<String, CommandHandlersConfig> getCommandHandlers() { return commandHandlers; }
    public void setCommandHandlers(Map<String, CommandHandlersConfig> commandHandlers) { this.commandHandlers = commandHandlers; }
}
```

### Command Implementation
```java
import java.util.UUID;
import java.time.Instant;

// Base Command interface
public interface Command<T> {
    String getCommandId();
    String getAggregateId();
    Instant getTimestamp();
    Class<T> getResultType();
}

// User Commands
public class CreateUserCommand implements Command<String> {
    private final String commandId;
    private final String email;
    private final String firstName;
    private final String lastName;
    private final Instant timestamp;
    
    public CreateUserCommand(String email, String firstName, String lastName) {
        this.commandId = UUID.randomUUID().toString();
        this.email = email;
        this.firstName = firstName;
        this.lastName = lastName;
        this.timestamp = Instant.now();
    }
    
    @Override
    public String getCommandId() { return commandId; }
    
    @Override
    public String getAggregateId() { return commandId; }
    
    @Override
    public Instant getTimestamp() { return timestamp; }
    
    @Override
    public Class<String> getResultType() { return String.class; }
    
    // Getters
    public String getEmail() { return email; }
    public String getFirstName() { return firstName; }
    public String getLastName() { return lastName; }
}

public class UpdateUserCommand implements Command<Void> {
    private final String commandId;
    private final String userId;
    private final String firstName;
    private final String lastName;
    private final Instant timestamp;
    
    public UpdateUserCommand(String userId, String firstName, String lastName) {
        this.commandId = UUID.randomUUID().toString();
        this.userId = userId;
        this.firstName = firstName;
        this.lastName = lastName;
        this.timestamp = Instant.now();
    }
    
    @Override
    public String getCommandId() { return commandId; }
    
    @Override
    public String getAggregateId() { return userId; }
    
    @Override
    public Instant getTimestamp() { return timestamp; }
    
    @Override
    public Class<Void> getResultType() { return Void.class; }
    
    // Getters
    public String getUserId() { return userId; }
    public String getFirstName() { return firstName; }
    public String getLastName() { return lastName; }
}

public class DeleteUserCommand implements Command<Void> {
    private final String commandId;
    private final String userId;
    private final Instant timestamp;
    
    public DeleteUserCommand(String userId) {
        this.commandId = UUID.randomUUID().toString();
        this.userId = userId;
        this.timestamp = Instant.now();
    }
    
    @Override
    public String getCommandId() { return commandId; }
    
    @Override
    public String getAggregateId() { return userId; }
    
    @Override
    public Instant getTimestamp() { return timestamp; }
    
    @Override
    public Class<Void> getResultType() { return Void.class; }
    
    // Getters
    public String getUserId() { return userId; }
}

// Order Commands
public class CreateOrderCommand implements Command<String> {
    private final String commandId;
    private final String userId;
    private final List<OrderItem> items;
    private final Instant timestamp;
    
    public CreateOrderCommand(String userId, List<OrderItem> items) {
        this.commandId = UUID.randomUUID().toString();
        this.userId = userId;
        this.items = items;
        this.timestamp = Instant.now();
    }
    
    @Override
    public String getCommandId() { return commandId; }
    
    @Override
    public String getAggregateId() { return commandId; }
    
    @Override
    public Instant getTimestamp() { return timestamp; }
    
    @Override
    public Class<String> getResultType() { return String.class; }
    
    // Getters
    public String getUserId() { return userId; }
    public List<OrderItem> getItems() { return items; }
}

public class AddOrderItemCommand implements Command<Void> {
    private final String commandId;
    private final String orderId;
    private final OrderItem item;
    private final Instant timestamp;
    
    public AddOrderItemCommand(String orderId, OrderItem item) {
        this.commandId = UUID.randomUUID().toString();
        this.orderId = orderId;
        this.item = item;
        this.timestamp = Instant.now();
    }
    
    @Override
    public String getCommandId() { return commandId; }
    
    @Override
    public String getAggregateId() { return orderId; }
    
    @Override
    public Instant getTimestamp() { return timestamp; }
    
    @Override
    public Class<Void> getResultType() { return Void.class; }
    
    // Getters
    public String getOrderId() { return orderId; }
    public OrderItem getItem() { return item; }
}
```

### Command Handlers
```java
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;
import java.util.concurrent.CompletableFuture;

@Component
public class CreateUserCommandHandler implements CommandHandler<CreateUserCommand, String> {
    
    private final UserRepository userRepository;
    private final EventBus eventBus;
    private final CommandMetrics commandMetrics;
    
    public CreateUserCommandHandler(UserRepository userRepository, 
                                   EventBus eventBus,
                                   CommandMetrics commandMetrics) {
        this.userRepository = userRepository;
        this.eventBus = eventBus;
        this.commandMetrics = commandMetrics;
    }
    
    @Override
    @Transactional
    public CompletableFuture<String> handle(CreateUserCommand command) {
        long startTime = System.currentTimeMillis();
        
        try {
            // Validate command
            validateCreateUserCommand(command);
            
            // Create user aggregate
            UserAggregate user = new UserAggregate(
                command.getEmail(),
                command.getFirstName(),
                command.getLastName()
            );
            
            // Save to write database
            userRepository.save(user);
            
            // Publish events
            for (Event event : user.getUncommittedEvents()) {
                eventBus.publish(event);
            }
            
            // Mark events as committed
            user.markEventsAsCommitted();
            
            // Record metrics
            commandMetrics.recordCommandSuccess("CreateUserCommand", 
                System.currentTimeMillis() - startTime);
            
            return CompletableFuture.completedFuture(user.getId());
            
        } catch (Exception e) {
            // Record metrics
            commandMetrics.recordCommandError("CreateUserCommand", e);
            throw new CommandExecutionException("Failed to create user", e);
        }
    }
    
    private void validateCreateUserCommand(CreateUserCommand command) {
        if (command.getEmail() == null || command.getEmail().trim().isEmpty()) {
            throw new IllegalArgumentException("Email is required");
        }
        if (command.getFirstName() == null || command.getFirstName().trim().isEmpty()) {
            throw new IllegalArgumentException("First name is required");
        }
        if (command.getLastName() == null || command.getLastName().trim().isEmpty()) {
            throw new IllegalArgumentException("Last name is required");
        }
        
        // Check if user already exists
        if (userRepository.existsByEmail(command.getEmail())) {
            throw new UserAlreadyExistsException("User with email " + command.getEmail() + " already exists");
        }
    }
}

@Component
public class UpdateUserCommandHandler implements CommandHandler<UpdateUserCommand, Void> {
    
    private final UserRepository userRepository;
    private final EventBus eventBus;
    private final CommandMetrics commandMetrics;
    
    public UpdateUserCommandHandler(UserRepository userRepository,
                                   EventBus eventBus,
                                   CommandMetrics commandMetrics) {
        this.userRepository = userRepository;
        this.eventBus = eventBus;
        this.commandMetrics = commandMetrics;
    }
    
    @Override
    @Transactional
    public CompletableFuture<Void> handle(UpdateUserCommand command) {
        long startTime = System.currentTimeMillis();
        
        try {
            // Load user aggregate
            UserAggregate user = userRepository.findById(command.getUserId())
                .orElseThrow(() -> new UserNotFoundException("User not found: " + command.getUserId()));
            
            // Update user
            user.updateUser(command.getFirstName(), command.getLastName());
            
            // Save to write database
            userRepository.save(user);
            
            // Publish events
            for (Event event : user.getUncommittedEvents()) {
                eventBus.publish(event);
            }
            
            // Mark events as committed
            user.markEventsAsCommitted();
            
            // Record metrics
            commandMetrics.recordCommandSuccess("UpdateUserCommand", 
                System.currentTimeMillis() - startTime);
            
            return CompletableFuture.completedFuture(null);
            
        } catch (Exception e) {
            // Record metrics
            commandMetrics.recordCommandError("UpdateUserCommand", e);
            throw new CommandExecutionException("Failed to update user", e);
        }
    }
}

@Component
public class CreateOrderCommandHandler implements CommandHandler<CreateOrderCommand, String> {
    
    private final OrderRepository orderRepository;
    private final ProductRepository productRepository;
    private final EventBus eventBus;
    private final CommandMetrics commandMetrics;
    
    public CreateOrderCommandHandler(OrderRepository orderRepository,
                                    ProductRepository productRepository,
                                    EventBus eventBus,
                                    CommandMetrics commandMetrics) {
        this.orderRepository = orderRepository;
        this.productRepository = productRepository;
        this.eventBus = eventBus;
        this.commandMetrics = commandMetrics;
    }
    
    @Override
    @Transactional
    public CompletableFuture<String> handle(CreateOrderCommand command) {
        long startTime = System.currentTimeMillis();
        
        try {
            // Validate command
            validateCreateOrderCommand(command);
            
            // Create order aggregate
            OrderAggregate order = new OrderAggregate(
                command.getUserId(),
                command.getItems()
            );
            
            // Save to write database
            orderRepository.save(order);
            
            // Publish events
            for (Event event : order.getUncommittedEvents()) {
                eventBus.publish(event);
            }
            
            // Mark events as committed
            order.markEventsAsCommitted();
            
            // Record metrics
            commandMetrics.recordCommandSuccess("CreateOrderCommand", 
                System.currentTimeMillis() - startTime);
            
            return CompletableFuture.completedFuture(order.getId());
            
        } catch (Exception e) {
            // Record metrics
            commandMetrics.recordCommandError("CreateOrderCommand", e);
            throw new CommandExecutionException("Failed to create order", e);
        }
    }
    
    private void validateCreateOrderCommand(CreateOrderCommand command) {
        if (command.getUserId() == null || command.getUserId().trim().isEmpty()) {
            throw new IllegalArgumentException("User ID is required");
        }
        if (command.getItems() == null || command.getItems().isEmpty()) {
            throw new IllegalArgumentException("Order items are required");
        }
        
        // Validate each item
        for (OrderItem item : command.getItems()) {
            if (item.getProductId() == null || item.getProductId().trim().isEmpty()) {
                throw new IllegalArgumentException("Product ID is required for all items");
            }
            if (item.getQuantity() <= 0) {
                throw new IllegalArgumentException("Quantity must be greater than 0");
            }
            
            // Check product availability
            ProductAggregate product = productRepository.findById(item.getProductId())
                .orElseThrow(() -> new ProductNotFoundException("Product not found: " + item.getProductId()));
            
            if (product.getStockQuantity() < item.getQuantity()) {
                throw new InsufficientStockException("Insufficient stock for product: " + item.getProductId());
            }
        }
    }
}
```

## 🔍 Query Side Implementation

### Query Implementation
```java
import java.util.List;
import java.util.Optional;

// Base Query interface
public interface Query<T> {
    Class<T> getResultType();
}

// User Queries
public class GetUserByIdQuery implements Query<UserSummary> {
    private final String userId;
    
    public GetUserByIdQuery(String userId) {
        this.userId = userId;
    }
    
    @Override
    public Class<UserSummary> getResultType() { return UserSummary.class; }
    
    public String getUserId() { return userId; }
}

public class GetUsersQuery implements Query<List<UserSummary>> {
    private final int page;
    private final int size;
    private final String search;
    
    public GetUsersQuery(int page, int size, String search) {
        this.page = page;
        this.size = size;
        this.search = search;
    }
    
    @Override
    public Class<List<UserSummary>> getResultType() { return (Class<List<UserSummary>>) (Class<?>) List.class; }
    
    // Getters
    public int getPage() { return page; }
    public int getSize() { return size; }
    public String getSearch() { return search; }
}

public class GetUserByEmailQuery implements Query<Optional<UserSummary>> {
    private final String email;
    
    public GetUserByEmailQuery(String email) {
        this.email = email;
    }
    
    @Override
    public Class<Optional<UserSummary>> getResultType() { return (Class<Optional<UserSummary>>) (Class<?>) Optional.class; }
    
    public String getEmail() { return email; }
}

// Order Queries
public class GetOrderByIdQuery implements Query<OrderDetails> {
    private final String orderId;
    
    public GetOrderByIdQuery(String orderId) {
        this.orderId = orderId;
    }
    
    @Override
    public Class<OrderDetails> getResultType() { return OrderDetails.class; }
    
    public String getOrderId() { return orderId; }
}

public class GetOrdersByUserIdQuery implements Query<List<OrderSummary>> {
    private final String userId;
    private final int page;
    private final int size;
    
    public GetOrdersByUserIdQuery(String userId, int page, int size) {
        this.userId = userId;
        this.page = page;
        this.size = size;
    }
    
    @Override
    public Class<List<OrderSummary>> getResultType() { return (Class<List<OrderSummary>>) (Class<?>) List.class; }
    
    // Getters
    public String getUserId() { return userId; }
    public int getPage() { return page; }
    public int getSize() { return size; }
}

// Product Queries
public class GetProductByIdQuery implements Query<ProductDetails> {
    private final String productId;
    
    public GetProductByIdQuery(String productId) {
        this.productId = productId;
    }
    
    @Override
    public Class<ProductDetails> getResultType() { return ProductDetails.class; }
    
    public String getProductId() { return productId; }
}

public class SearchProductsQuery implements Query<List<ProductSummary>> {
    private final String search;
    private final String category;
    private final BigDecimal minPrice;
    private final BigDecimal maxPrice;
    private final int page;
    private final int size;
    
    public SearchProductsQuery(String search, String category, BigDecimal minPrice, 
                              BigDecimal maxPrice, int page, int size) {
        this.search = search;
        this.category = category;
        this.minPrice = minPrice;
        this.maxPrice = maxPrice;
        this.page = page;
        this.size = size;
    }
    
    @Override
    public Class<List<ProductSummary>> getResultType() { return (Class<List<ProductSummary>>) (Class<?>) List.class; }
    
    // Getters
    public String getSearch() { return search; }
    public String getCategory() { return category; }
    public BigDecimal getMinPrice() { return minPrice; }
    public BigDecimal getMaxPrice() { return maxPrice; }
    public int getPage() { return page; }
    public int getSize() { return size; }
}
```

### Query Handlers
```java
import org.springframework.stereotype.Component;
import org.springframework.cache.annotation.Cacheable;
import java.util.concurrent.CompletableFuture;

@Component
public class GetUserByIdQueryHandler implements QueryHandler<GetUserByIdQuery, UserSummary> {
    
    private final UserReadRepository userReadRepository;
    private final QueryMetrics queryMetrics;
    
    public GetUserByIdQueryHandler(UserReadRepository userReadRepository,
                                  QueryMetrics queryMetrics) {
        this.userReadRepository = userReadRepository;
        this.queryMetrics = queryMetrics;
    }
    
    @Override
    @Cacheable(value = "users", key = "#query.userId")
    public CompletableFuture<UserSummary> handle(GetUserByIdQuery query) {
        long startTime = System.currentTimeMillis();
        
        try {
            UserSummary user = userReadRepository.findById(query.getUserId())
                .orElseThrow(() -> new UserNotFoundException("User not found: " + query.getUserId()));
            
            // Record metrics
            queryMetrics.recordQuerySuccess("GetUserByIdQuery", 
                System.currentTimeMillis() - startTime);
            
            return CompletableFuture.completedFuture(user);
            
        } catch (Exception e) {
            // Record metrics
            queryMetrics.recordQueryError("GetUserByIdQuery", e);
            throw new QueryExecutionException("Failed to get user by ID", e);
        }
    }
}

@Component
public class GetUsersQueryHandler implements QueryHandler<GetUsersQuery, List<UserSummary>> {
    
    private final UserReadRepository userReadRepository;
    private final QueryMetrics queryMetrics;
    
    public GetUsersQueryHandler(UserReadRepository userReadRepository,
                               QueryMetrics queryMetrics) {
        this.userReadRepository = userReadRepository;
        this.queryMetrics = queryMetrics;
    }
    
    @Override
    public CompletableFuture<List<UserSummary>> handle(GetUsersQuery query) {
        long startTime = System.currentTimeMillis();
        
        try {
            List<UserSummary> users = userReadRepository.findUsers(
                query.getPage(), 
                query.getSize(), 
                query.getSearch()
            );
            
            // Record metrics
            queryMetrics.recordQuerySuccess("GetUsersQuery", 
                System.currentTimeMillis() - startTime);
            
            return CompletableFuture.completedFuture(users);
            
        } catch (Exception e) {
            // Record metrics
            queryMetrics.recordQueryError("GetUsersQuery", e);
            throw new QueryExecutionException("Failed to get users", e);
        }
    }
}

@Component
public class GetOrderByIdQueryHandler implements QueryHandler<GetOrderByIdQuery, OrderDetails> {
    
    private final OrderReadRepository orderReadRepository;
    private final QueryMetrics queryMetrics;
    
    public GetOrderByIdQueryHandler(OrderReadRepository orderReadRepository,
                                   QueryMetrics queryMetrics) {
        this.orderReadRepository = orderReadRepository;
        this.queryMetrics = queryMetrics;
    }
    
    @Override
    @Cacheable(value = "orders", key = "#query.orderId")
    public CompletableFuture<OrderDetails> handle(GetOrderByIdQuery query) {
        long startTime = System.currentTimeMillis();
        
        try {
            OrderDetails order = orderReadRepository.findById(query.getOrderId())
                .orElseThrow(() -> new OrderNotFoundException("Order not found: " + query.getOrderId()));
            
            // Record metrics
            queryMetrics.recordQuerySuccess("GetOrderByIdQuery", 
                System.currentTimeMillis() - startTime);
            
            return CompletableFuture.completedFuture(order);
            
        } catch (Exception e) {
            // Record metrics
            queryMetrics.recordQueryError("GetOrderByIdQuery", e);
            throw new QueryExecutionException("Failed to get order by ID", e);
        }
    }
}

@Component
public class SearchProductsQueryHandler implements QueryHandler<SearchProductsQuery, List<ProductSummary>> {
    
    private final ProductReadRepository productReadRepository;
    private final QueryMetrics queryMetrics;
    
    public SearchProductsQueryHandler(ProductReadRepository productReadRepository,
                                     QueryMetrics queryMetrics) {
        this.productReadRepository = productReadRepository;
        this.queryMetrics = queryMetrics;
    }
    
    @Override
    public CompletableFuture<List<ProductSummary>> handle(SearchProductsQuery query) {
        long startTime = System.currentTimeMillis();
        
        try {
            List<ProductSummary> products = productReadRepository.searchProducts(
                query.getSearch(),
                query.getCategory(),
                query.getMinPrice(),
                query.getMaxPrice(),
                query.getPage(),
                query.getSize()
            );
            
            // Record metrics
            queryMetrics.recordQuerySuccess("SearchProductsQuery", 
                System.currentTimeMillis() - startTime);
            
            return CompletableFuture.completedFuture(products);
            
        } catch (Exception e) {
            // Record metrics
            queryMetrics.recordQueryError("SearchProductsQuery", e);
            throw new QueryExecutionException("Failed to search products", e);
        }
    }
}
```

## 🔧 Best Practices

### 1. Command Design
- Make commands immutable and serializable
- Include all necessary data for command execution
- Validate commands before processing
- Use proper error handling and rollback

### 2. Query Design
- Design queries for specific use cases
- Use denormalized data for read performance
- Implement proper caching strategies
- Monitor query performance and optimize

### 3. Event Handling
- Use events for eventual consistency
- Implement proper event versioning
- Handle event processing failures
- Monitor event processing lag

### 4. Database Design
- Separate read and write databases
- Use appropriate indexes for queries
- Implement proper connection pooling
- Monitor database performance

### 5. Consistency Management
- Use eventual consistency for most operations
- Implement strong consistency for critical operations
- Monitor consistency lag
- Handle consistency violations

## 🎯 Summary

TuskLang Java CQRS provides:

- **Command Side**: Write operations with event sourcing
- **Query Side**: Read operations with optimized read models
- **Event Bus**: Asynchronous event processing
- **Separation of Concerns**: Clear separation between commands and queries
- **Performance Optimization**: Optimized read and write operations

The combination of TuskLang's executable configuration with Java's CQRS capabilities creates a powerful platform for building scalable, maintainable, and high-performance applications.

**"We don't bow to any king" - Build CQRS applications that scale with confidence!** 