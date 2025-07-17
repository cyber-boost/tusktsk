# 🌐 Distributed Systems with TuskLang Java

**"We don't bow to any king" - Distributed Java Edition**

TuskLang Java enables building robust distributed systems with microservices, event-driven architecture, and distributed patterns that scale across multiple nodes and services.

## 🎯 Distributed Architecture Overview

### Microservices Configuration
```java
// distributed-app.tsk
[distributed_system]
name: "Enterprise Distributed App"
version: "3.0.0"
deployment_mode: "microservices"

[service_mesh]
type: "istio"
enabled: true
tracing: true
metrics: true
security: {
    mTLS: true
    authorization: true
}

[services]
user_service: {
    name: "user-service"
    port: 8081
    replicas: 3
    health_check: "/health"
    dependencies: ["database", "cache"]
    api_version: "v1"
    rate_limit: {
        requests_per_minute: 1000
        burst_size: 100
    }
}

order_service: {
    name: "order-service"
    port: 8082
    replicas: 5
    health_check: "/health"
    dependencies: ["database", "payment_service", "inventory_service"]
    api_version: "v1"
    circuit_breaker: {
        failure_threshold: 5
        recovery_timeout: "30s"
        half_open_state: true
    }
}

payment_service: {
    name: "payment-service"
    port: 8083
    replicas: 2
    health_check: "/health"
    dependencies: ["database", "external_payment_gateway"]
    api_version: "v1"
    retry_policy: {
        max_attempts: 3
        backoff: "exponential"
        initial_delay: "1s"
    }
}

inventory_service: {
    name: "inventory-service"
    port: 8084
    replicas: 3
    health_check: "/health"
    dependencies: ["database", "cache"]
    api_version: "v1"
    cache_strategy: {
        ttl: "5m"
        invalidation: "write_through"
    }
}

[databases]
primary: {
    type: "postgresql"
    host: @env("DB_HOST", "localhost")
    port: @env("DB_PORT", "5432")
    name: @env("DB_NAME", "distributed_app")
    user: @env("DB_USER", "app_user")
    password: @env.secure("DB_PASSWORD")
    pool_size: 50
    read_replicas: [
        {
            host: @env("DB_READ_REPLICA_1", "read-replica-1")
            port: @env("DB_PORT", "5432")
            weight: 0.5
        },
        {
            host: @env("DB_READ_REPLICA_2", "read-replica-2")
            port: @env("DB_PORT", "5432")
            weight: 0.5
        }
    ]
}

cache: {
    type: "redis"
    mode: "cluster"
    nodes: [
        {
            host: @env("REDIS_NODE_1", "redis-node-1")
            port: @env("REDIS_PORT", "6379")
        },
        {
            host: @env("REDIS_NODE_2", "redis-node-2")
            port: @env("REDIS_PORT", "6379")
        },
        {
            host: @env("REDIS_NODE_3", "redis-node-3")
            port: @env("REDIS_PORT", "6379")
        }
    ]
    password: @env.secure("REDIS_PASSWORD")
    ttl: "1h"
    max_memory: "2gb"
}

[message_broker]
type: "kafka"
bootstrap_servers: [
    @env("KAFKA_BROKER_1", "kafka-1:9092"),
    @env("KAFKA_BROKER_2", "kafka-2:9092"),
    @env("KAFKA_BROKER_3", "kafka-3:9092")
]
security: {
    sasl_mechanism: "PLAIN"
    username: @env.secure("KAFKA_USERNAME")
    password: @env.secure("KAFKA_PASSWORD")
}
topics: {
    user_events: {
        partitions: 10
        replication_factor: 3
        retention: "7d"
    }
    order_events: {
        partitions: 20
        replication_factor: 3
        retention: "30d"
    }
    payment_events: {
        partitions: 15
        replication_factor: 3
        retention: "90d"
    }
}

[distributed_tracing]
enabled: true
sampling_rate: 0.1
exporter: "jaeger"
endpoint: @env("JAEGER_ENDPOINT", "http://jaeger:14268/api/traces")
correlation_id_header: "X-Correlation-ID"
```

## 🔄 Event-Driven Architecture

### Event Configuration
```java
// events.tsk
[event_schema]
version: "1.0"
namespace: "com.enterprise.events"

[events]
user_created: {
    type: "user.created"
    version: "1.0"
    schema: {
        user_id: "string"
        email: "string"
        first_name: "string"
        last_name: "string"
        created_at: "timestamp"
        metadata: "object"
    }
    producers: ["user_service"]
    consumers: ["notification_service", "analytics_service", "audit_service"]
    routing_key: "user.created"
    dead_letter_queue: "user_events_dlq"
}

user_updated: {
    type: "user.updated"
    version: "1.0"
    schema: {
        user_id: "string"
        email: "string"
        first_name: "string"
        last_name: "string"
        updated_at: "timestamp"
        changes: "array"
        metadata: "object"
    }
    producers: ["user_service"]
    consumers: ["notification_service", "analytics_service", "audit_service"]
    routing_key: "user.updated"
    dead_letter_queue: "user_events_dlq"
}

order_created: {
    type: "order.created"
    version: "1.0"
    schema: {
        order_id: "string"
        user_id: "string"
        items: "array"
        total_amount: "decimal"
        currency: "string"
        status: "string"
        created_at: "timestamp"
        metadata: "object"
    }
    producers: ["order_service"]
    consumers: ["payment_service", "inventory_service", "notification_service", "analytics_service"]
    routing_key: "order.created"
    dead_letter_queue: "order_events_dlq"
}

order_status_changed: {
    type: "order.status.changed"
    version: "1.0"
    schema: {
        order_id: "string"
        user_id: "string"
        old_status: "string"
        new_status: "string"
        changed_at: "timestamp"
        reason: "string"
        metadata: "object"
    }
    producers: ["order_service", "payment_service", "inventory_service"]
    consumers: ["notification_service", "analytics_service", "audit_service"]
    routing_key: "order.status.changed"
    dead_letter_queue: "order_events_dlq"
}

payment_processed: {
    type: "payment.processed"
    version: "1.0"
    schema: {
        payment_id: "string"
        order_id: "string"
        user_id: "string"
        amount: "decimal"
        currency: "string"
        status: "string"
        gateway: "string"
        processed_at: "timestamp"
        metadata: "object"
    }
    producers: ["payment_service"]
    consumers: ["order_service", "notification_service", "analytics_service", "audit_service"]
    routing_key: "payment.processed"
    dead_letter_queue: "payment_events_dlq"
}

[event_handlers]
user_service: {
    handlers: [
        {
            event_type: "order.created"
            handler: "handleOrderCreated"
            async: true
            retry_policy: {
                max_attempts: 3
                backoff: "exponential"
            }
        },
        {
            event_type: "payment.processed"
            handler: "handlePaymentProcessed"
            async: true
            retry_policy: {
                max_attempts: 3
                backoff: "exponential"
            }
        }
    ]
}

order_service: {
    handlers: [
        {
            event_type: "payment.processed"
            handler: "handlePaymentProcessed"
            async: false
            retry_policy: {
                max_attempts: 5
                backoff: "exponential"
            }
        },
        {
            event_type: "inventory.updated"
            handler: "handleInventoryUpdated"
            async: true
            retry_policy: {
                max_attempts: 3
                backoff: "exponential"
            }
        }
    ]
}
```

### Event-Driven Service Implementation
```java
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Bean;
import org.springframework.kafka.annotation.KafkaListener;
import org.springframework.kafka.core.KafkaTemplate;
import org.springframework.stereotype.Service;
import org.tusklang.java.annotations.TuskConfig;
import com.fasterxml.jackson.databind.ObjectMapper;
import java.util.Map;

@Configuration
@TuskConfig
public class EventDrivenConfig {
    
    @Bean
    public ObjectMapper eventObjectMapper() {
        ObjectMapper mapper = new ObjectMapper();
        mapper.registerModule(new JavaTimeModule());
        return mapper;
    }
    
    @Bean
    public EventPublisher eventPublisher(KafkaTemplate<String, String> kafkaTemplate,
                                       ObjectMapper objectMapper) {
        return new EventPublisher(kafkaTemplate, objectMapper);
    }
    
    @Bean
    public EventHandlerRegistry eventHandlerRegistry() {
        return new EventHandlerRegistry();
    }
}

@Service
public class EventPublisher {
    
    private final KafkaTemplate<String, String> kafkaTemplate;
    private final ObjectMapper objectMapper;
    private final Map<String, EventConfig> eventConfigs;
    
    public EventPublisher(KafkaTemplate<String, String> kafkaTemplate,
                         ObjectMapper objectMapper,
                         Map<String, EventConfig> eventConfigs) {
        this.kafkaTemplate = kafkaTemplate;
        this.objectMapper = objectMapper;
        this.eventConfigs = eventConfigs;
    }
    
    public <T> void publishEvent(String eventType, T event) {
        try {
            EventConfig config = eventConfigs.get(eventType);
            if (config == null) {
                throw new EventConfigurationException("No configuration found for event: " + eventType);
            }
            
            String eventJson = objectMapper.writeValueAsString(event);
            String topic = config.getTopic();
            String routingKey = config.getRoutingKey();
            
            kafkaTemplate.send(topic, routingKey, eventJson)
                .addCallback(
                    result -> log.info("Event published successfully: {}", eventType),
                    ex -> log.error("Failed to publish event: {}", eventType, ex)
                );
                
        } catch (Exception e) {
            log.error("Error publishing event: {}", eventType, e);
            throw new EventPublishException("Failed to publish event: " + eventType, e);
        }
    }
    
    public <T> void publishEventWithCorrelation(String eventType, T event, String correlationId) {
        try {
            EventConfig config = eventConfigs.get(eventType);
            if (config == null) {
                throw new EventConfigurationException("No configuration found for event: " + eventType);
            }
            
            // Add correlation ID to event metadata
            if (event instanceof BaseEvent) {
                ((BaseEvent) event).setCorrelationId(correlationId);
            }
            
            String eventJson = objectMapper.writeValueAsString(event);
            String topic = config.getTopic();
            String routingKey = config.getRoutingKey();
            
            kafkaTemplate.send(topic, routingKey, eventJson)
                .addCallback(
                    result -> log.info("Event published successfully: {} with correlation: {}", 
                                     eventType, correlationId),
                    ex -> log.error("Failed to publish event: {} with correlation: {}", 
                                  eventType, correlationId, ex)
                );
                
        } catch (Exception e) {
            log.error("Error publishing event: {} with correlation: {}", eventType, correlationId, e);
            throw new EventPublishException("Failed to publish event: " + eventType, e);
        }
    }
}

@Service
public class UserEventHandlers {
    
    private final EventPublisher eventPublisher;
    private final UserService userService;
    
    public UserEventHandlers(EventPublisher eventPublisher, UserService userService) {
        this.eventPublisher = eventPublisher;
        this.userService = userService;
    }
    
    @KafkaListener(topics = "order_events", groupId = "user_service")
    public void handleOrderCreated(String eventJson) {
        try {
            OrderCreatedEvent event = objectMapper.readValue(eventJson, OrderCreatedEvent.class);
            
            // Update user's order history
            userService.addOrderToHistory(event.getUserId(), event.getOrderId());
            
            // Publish user updated event
            UserUpdatedEvent userEvent = new UserUpdatedEvent();
            userEvent.setUserId(event.getUserId());
            userEvent.setUpdatedAt(Instant.now());
            userEvent.setChanges(Arrays.asList("order_history"));
            
            eventPublisher.publishEvent("user.updated", userEvent);
            
        } catch (Exception e) {
            log.error("Error handling order created event", e);
            // Send to dead letter queue
            sendToDeadLetterQueue("order_events_dlq", eventJson, e);
        }
    }
    
    @KafkaListener(topics = "payment_events", groupId = "user_service")
    public void handlePaymentProcessed(String eventJson) {
        try {
            PaymentProcessedEvent event = objectMapper.readValue(eventJson, PaymentProcessedEvent.class);
            
            // Update user's payment history
            userService.addPaymentToHistory(event.getUserId(), event.getPaymentId());
            
            // Publish user updated event
            UserUpdatedEvent userEvent = new UserUpdatedEvent();
            userEvent.setUserId(event.getUserId());
            userEvent.setUpdatedAt(Instant.now());
            userEvent.setChanges(Arrays.asList("payment_history"));
            
            eventPublisher.publishEvent("user.updated", userEvent);
            
        } catch (Exception e) {
            log.error("Error handling payment processed event", e);
            // Send to dead letter queue
            sendToDeadLetterQueue("payment_events_dlq", eventJson, e);
        }
    }
    
    private void sendToDeadLetterQueue(String dlqTopic, String eventJson, Exception error) {
        DeadLetterEvent dlqEvent = new DeadLetterEvent();
        dlqEvent.setOriginalEvent(eventJson);
        dlqEvent.setError(error.getMessage());
        dlqEvent.setTimestamp(Instant.now());
        
        kafkaTemplate.send(dlqTopic, eventJson)
            .addCallback(
                result -> log.info("Event sent to DLQ: {}", dlqTopic),
                ex -> log.error("Failed to send event to DLQ: {}", dlqTopic, ex)
            );
    }
}
```

## 🔗 Service Communication Patterns

### Circuit Breaker Configuration
```java
// circuit-breaker.tsk
[circuit_breaker]
default_config: {
    failure_threshold: 5
    recovery_timeout: "30s"
    half_open_state: true
    monitoring: true
}

[services]
payment_service: {
    failure_threshold: 3
    recovery_timeout: "60s"
    half_open_state: true
    exceptions: ["PaymentGatewayException", "NetworkException"]
}

inventory_service: {
    failure_threshold: 10
    recovery_timeout: "15s"
    half_open_state: false
    exceptions: ["DatabaseException"]
}

external_api: {
    failure_threshold: 2
    recovery_timeout: "120s"
    half_open_state: true
    exceptions: ["HttpClientException", "TimeoutException"]
}

[monitoring]
metrics: {
    enabled: true
    circuit_state_changes: true
    failure_counts: true
    success_counts: true
    response_times: true
}

alerts: {
    circuit_open: {
        enabled: true
        threshold: 1
        notification: {
            email: ["devops@company.com"]
            slack: "#alerts"
        }
    }
}
```

### Circuit Breaker Implementation
```java
import io.github.resilience4j.circuitbreaker.CircuitBreaker;
import io.github.resilience4j.circuitbreaker.CircuitBreakerConfig;
import io.github.resilience4j.circuitbreaker.CircuitBreakerRegistry;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.tusklang.java.annotations.TuskConfig;
import java.time.Duration;

@Configuration
@TuskConfig
public class CircuitBreakerConfiguration {
    
    private final Map<String, CircuitBreakerConfig> circuitBreakerConfigs;
    
    public CircuitBreakerConfiguration(Map<String, CircuitBreakerConfig> circuitBreakerConfigs) {
        this.circuitBreakerConfigs = circuitBreakerConfigs;
    }
    
    @Bean
    public CircuitBreakerRegistry circuitBreakerRegistry() {
        CircuitBreakerRegistry registry = CircuitBreakerRegistry.ofDefaults();
        
        // Register circuit breakers for each service
        circuitBreakerConfigs.forEach((serviceName, config) -> {
            CircuitBreaker circuitBreaker = CircuitBreaker.of(serviceName, config);
            registry.addConfiguration(serviceName, config);
        });
        
        return registry;
    }
    
    @Bean
    public CircuitBreakerService circuitBreakerService(CircuitBreakerRegistry registry) {
        return new CircuitBreakerService(registry);
    }
}

@Service
public class CircuitBreakerService {
    
    private final CircuitBreakerRegistry registry;
    private final Map<String, CircuitBreakerConfig> configs;
    
    public CircuitBreakerService(CircuitBreakerRegistry registry,
                                Map<String, CircuitBreakerConfig> configs) {
        this.registry = registry;
        this.configs = configs;
    }
    
    public <T> T executeWithCircuitBreaker(String serviceName, Supplier<T> supplier) {
        CircuitBreaker circuitBreaker = registry.circuitBreaker(serviceName);
        return circuitBreaker.executeSupplier(supplier);
    }
    
    public void executeWithCircuitBreaker(String serviceName, Runnable runnable) {
        CircuitBreaker circuitBreaker = registry.circuitBreaker(serviceName);
        circuitBreaker.executeRunnable(runnable);
    }
    
    public CircuitBreaker.State getCircuitState(String serviceName) {
        CircuitBreaker circuitBreaker = registry.circuitBreaker(serviceName);
        return circuitBreaker.getState();
    }
    
    public CircuitBreaker.Metrics getCircuitMetrics(String serviceName) {
        CircuitBreaker circuitBreaker = registry.circuitBreaker(serviceName);
        return circuitBreaker.getMetrics();
    }
}

@Service
public class PaymentServiceClient {
    
    private final CircuitBreakerService circuitBreakerService;
    private final RestTemplate restTemplate;
    private final String paymentServiceUrl;
    
    public PaymentServiceClient(CircuitBreakerService circuitBreakerService,
                               RestTemplate restTemplate,
                               @Value("${payment.service.url}") String paymentServiceUrl) {
        this.circuitBreakerService = circuitBreakerService;
        this.restTemplate = restTemplate;
        this.paymentServiceUrl = paymentServiceUrl;
    }
    
    public PaymentResponse processPayment(PaymentRequest request) {
        return circuitBreakerService.executeWithCircuitBreaker("payment_service", () -> {
            try {
                ResponseEntity<PaymentResponse> response = restTemplate.postForEntity(
                    paymentServiceUrl + "/payments",
                    request,
                    PaymentResponse.class
                );
                
                if (response.getStatusCode().is2xxSuccessful()) {
                    return response.getBody();
                } else {
                    throw new PaymentServiceException("Payment service returned error: " + 
                                                    response.getStatusCode());
                }
                
            } catch (Exception e) {
                log.error("Error calling payment service", e);
                throw new PaymentServiceException("Failed to process payment", e);
            }
        });
    }
    
    public PaymentStatus getPaymentStatus(String paymentId) {
        return circuitBreakerService.executeWithCircuitBreaker("payment_service", () -> {
            try {
                ResponseEntity<PaymentStatus> response = restTemplate.getForEntity(
                    paymentServiceUrl + "/payments/" + paymentId + "/status",
                    PaymentStatus.class
                );
                
                if (response.getStatusCode().is2xxSuccessful()) {
                    return response.getBody();
                } else {
                    throw new PaymentServiceException("Payment service returned error: " + 
                                                    response.getStatusCode());
                }
                
            } catch (Exception e) {
                log.error("Error getting payment status", e);
                throw new PaymentServiceException("Failed to get payment status", e);
            }
        });
    }
}
```

## 🔄 Distributed Caching

### Cache Configuration
```java
// cache.tsk
[distributed_cache]
type: "redis"
mode: "cluster"
nodes: [
    {
        host: @env("REDIS_NODE_1", "redis-node-1")
        port: @env("REDIS_PORT", "6379")
        weight: 1
    },
    {
        host: @env("REDIS_NODE_2", "redis-node-2")
        port: @env("REDIS_PORT", "6379")
        weight: 1
    },
    {
        host: @env("REDIS_NODE_3", "redis-node-3")
        port: @env("REDIS_PORT", "6379")
        weight: 1
    }
]
password: @env.secure("REDIS_PASSWORD")
max_memory: "4gb"
eviction_policy: "allkeys-lru"

[cache_strategies]
user_cache: {
    ttl: "1h"
    max_size: 10000
    invalidation: "write_through"
    compression: true
    serialization: "json"
}

order_cache: {
    ttl: "30m"
    max_size: 5000
    invalidation: "write_behind"
    compression: true
    serialization: "json"
}

product_cache: {
    ttl: "24h"
    max_size: 20000
    invalidation: "cache_aside"
    compression: true
    serialization: "json"
}

session_cache: {
    ttl: "2h"
    max_size: 1000
    invalidation: "write_through"
    compression: false
    serialization: "binary"
}

[cache_events]
user_updated: {
    cache_key: "user:{user_id}"
    invalidation: true
    propagation: true
}

order_created: {
    cache_key: "user_orders:{user_id}"
    invalidation: false
    propagation: true
}

product_updated: {
    cache_key: "product:{product_id}"
    invalidation: true
    propagation: true
}
```

### Distributed Cache Implementation
```java
import org.springframework.cache.annotation.Cacheable;
import org.springframework.cache.annotation.CacheEvict;
import org.springframework.cache.annotation.CachePut;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.stereotype.Service;
import org.tusklang.java.annotations.TuskConfig;
import java.util.concurrent.TimeUnit;

@Configuration
@TuskConfig
public class CacheConfiguration {
    
    @Bean
    public RedisTemplate<String, Object> redisTemplate(RedisConnectionFactory connectionFactory) {
        RedisTemplate<String, Object> template = new RedisTemplate<>();
        template.setConnectionFactory(connectionFactory);
        
        // Configure serialization
        template.setKeySerializer(new StringRedisSerializer());
        template.setValueSerializer(new GenericJackson2JsonRedisSerializer());
        template.setHashKeySerializer(new StringRedisSerializer());
        template.setHashValueSerializer(new GenericJackson2JsonRedisSerializer());
        
        return template;
    }
    
    @Bean
    public CacheManager cacheManager(RedisTemplate<String, Object> redisTemplate,
                                   Map<String, CacheStrategy> cacheStrategies) {
        RedisCacheManager cacheManager = RedisCacheManager.builder(redisTemplate.getConnectionFactory())
            .cacheDefaults(RedisCacheConfiguration.defaultCacheConfig()
                .entryTtl(Duration.ofHours(1))
                .serializeKeysWith(RedisSerializationContext.SerializationPair
                    .fromSerializer(new StringRedisSerializer()))
                .serializeValuesWith(RedisSerializationContext.SerializationPair
                    .fromSerializer(new GenericJackson2JsonRedisSerializer())))
            .build();
        
        // Configure custom cache settings
        cacheStrategies.forEach((cacheName, strategy) -> {
            RedisCacheConfiguration config = RedisCacheConfiguration.defaultCacheConfig()
                .entryTtl(Duration.parse(strategy.getTtl()))
                .serializeKeysWith(RedisSerializationContext.SerializationPair
                    .fromSerializer(new StringRedisSerializer()))
                .serializeValuesWith(RedisSerializationContext.SerializationPair
                    .fromSerializer(new GenericJackson2JsonRedisSerializer()));
            
            cacheManager.setCacheConfiguration(cacheName, config);
        });
        
        return cacheManager;
    }
}

@Service
public class DistributedCacheService {
    
    private final RedisTemplate<String, Object> redisTemplate;
    private final Map<String, CacheStrategy> cacheStrategies;
    private final EventPublisher eventPublisher;
    
    public DistributedCacheService(RedisTemplate<String, Object> redisTemplate,
                                  Map<String, CacheStrategy> cacheStrategies,
                                  EventPublisher eventPublisher) {
        this.redisTemplate = redisTemplate;
        this.cacheStrategies = cacheStrategies;
        this.eventPublisher = eventPublisher;
    }
    
    public <T> T get(String cacheName, String key, Class<T> type) {
        String fullKey = buildCacheKey(cacheName, key);
        Object value = redisTemplate.opsForValue().get(fullKey);
        
        if (value != null) {
            log.debug("Cache hit for key: {}", fullKey);
            return type.cast(value);
        }
        
        log.debug("Cache miss for key: {}", fullKey);
        return null;
    }
    
    public void put(String cacheName, String key, Object value) {
        String fullKey = buildCacheKey(cacheName, key);
        CacheStrategy strategy = cacheStrategies.get(cacheName);
        
        if (strategy != null) {
            redisTemplate.opsForValue().set(fullKey, value, 
                Duration.parse(strategy.getTtl()));
        } else {
            redisTemplate.opsForValue().set(fullKey, value);
        }
        
        log.debug("Cached value for key: {}", fullKey);
        
        // Publish cache event if configured
        publishCacheEvent(cacheName, key, "PUT");
    }
    
    public void evict(String cacheName, String key) {
        String fullKey = buildCacheKey(cacheName, key);
        redisTemplate.delete(fullKey);
        
        log.debug("Evicted cache for key: {}", fullKey);
        
        // Publish cache event if configured
        publishCacheEvent(cacheName, key, "EVICT");
    }
    
    public void evictPattern(String cacheName, String pattern) {
        String fullPattern = buildCacheKey(cacheName, pattern);
        Set<String> keys = redisTemplate.keys(fullPattern);
        
        if (keys != null && !keys.isEmpty()) {
            redisTemplate.delete(keys);
            log.debug("Evicted {} cache entries for pattern: {}", keys.size(), fullPattern);
        }
    }
    
    private String buildCacheKey(String cacheName, String key) {
        return String.format("%s:%s", cacheName, key);
    }
    
    private void publishCacheEvent(String cacheName, String key, String operation) {
        CacheEvent event = new CacheEvent();
        event.setCacheName(cacheName);
        event.setKey(key);
        event.setOperation(operation);
        event.setTimestamp(Instant.now());
        
        eventPublisher.publishEvent("cache.operation", event);
    }
}

@Service
public class UserService {
    
    private final DistributedCacheService cacheService;
    private final UserRepository userRepository;
    
    public UserService(DistributedCacheService cacheService, UserRepository userRepository) {
        this.cacheService = cacheService;
        this.userRepository = userRepository;
    }
    
    @Cacheable(value = "user_cache", key = "#userId")
    public User getUserById(String userId) {
        log.debug("Fetching user from database: {}", userId);
        return userRepository.findById(userId)
            .orElseThrow(() -> new UserNotFoundException("User not found: " + userId));
    }
    
    @CachePut(value = "user_cache", key = "#user.id")
    public User createUser(User user) {
        log.debug("Creating new user: {}", user.getId());
        User savedUser = userRepository.save(user);
        
        // Publish user created event
        UserCreatedEvent event = new UserCreatedEvent();
        event.setUserId(savedUser.getId());
        event.setEmail(savedUser.getEmail());
        event.setCreatedAt(Instant.now());
        
        eventPublisher.publishEvent("user.created", event);
        
        return savedUser;
    }
    
    @CachePut(value = "user_cache", key = "#user.id")
    public User updateUser(User user) {
        log.debug("Updating user: {}", user.getId());
        User updatedUser = userRepository.save(user);
        
        // Publish user updated event
        UserUpdatedEvent event = new UserUpdatedEvent();
        event.setUserId(updatedUser.getId());
        event.setUpdatedAt(Instant.now());
        event.setChanges(Arrays.asList("profile", "preferences"));
        
        eventPublisher.publishEvent("user.updated", event);
        
        return updatedUser;
    }
    
    @CacheEvict(value = "user_cache", key = "#userId")
    public void deleteUser(String userId) {
        log.debug("Deleting user: {}", userId);
        userRepository.deleteById(userId);
        
        // Publish user deleted event
        UserDeletedEvent event = new UserDeletedEvent();
        event.setUserId(userId);
        event.setDeletedAt(Instant.now());
        
        eventPublisher.publishEvent("user.deleted", event);
    }
}
```

## 🔧 Best Practices

### 1. Service Communication
- Use circuit breakers for resilience
- Implement retry policies with exponential backoff
- Use health checks for service discovery
- Implement proper timeout handling

### 2. Event-Driven Architecture
- Use event sourcing for audit trails
- Implement CQRS for read/write separation
- Use saga pattern for distributed transactions
- Implement dead letter queues for failed events

### 3. Distributed Caching
- Use cache-aside pattern for read-heavy workloads
- Implement write-through for consistency
- Use cache invalidation strategies
- Monitor cache hit rates and performance

### 4. Monitoring and Observability
- Use distributed tracing for request tracking
- Implement health checks for all services
- Use structured logging with correlation IDs
- Monitor circuit breaker states and metrics

### 5. Security
- Use mTLS for service-to-service communication
- Implement proper authentication and authorization
- Use secure configuration management
- Monitor for security events

## 🎯 Summary

TuskLang Java distributed systems provide:

- **Microservices Architecture**: Service discovery, load balancing, and health checks
- **Event-Driven Communication**: Kafka integration with event schemas and handlers
- **Circuit Breakers**: Resilience patterns with monitoring and alerts
- **Distributed Caching**: Redis cluster with multiple cache strategies
- **Service Mesh Integration**: Istio support for advanced traffic management

The combination of TuskLang's executable configuration with Java's distributed systems capabilities creates a powerful platform for building scalable, resilient, and maintainable microservices.

**"We don't bow to any king" - Build distributed systems that scale with confidence!** 