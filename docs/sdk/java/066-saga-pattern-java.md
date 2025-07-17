# 🔄 Saga Pattern with TuskLang Java

**"We don't bow to any king" - Saga Pattern Java Edition**

TuskLang Java enables building saga patterns for distributed transactions with compensation logic, ensuring data consistency across multiple microservices and handling failures gracefully.

## 🎯 Saga Pattern Architecture Overview

### Saga Configuration
```java
// saga-app.tsk
[saga_system]
name: "Saga Pattern TuskLang App"
version: "2.0.0"
paradigm: "saga"
orchestration: "choreography"

[saga_orchestrator]
name: "saga-orchestrator"
type: "centralized"
database: "saga_database"
timeout: "5m"
retry_policy: {
    max_attempts: 3
    backoff: "exponential"
    initial_delay: "1s"
}

[sagas]
order_processing_saga: {
    name: "OrderProcessingSaga"
    description: "Process order from creation to completion"
    steps: [
        {
            name: "validate_order"
            service: "order_service"
            action: "validateOrder"
            compensation: "cancelOrder"
            timeout: "30s"
            retry_policy: {
                max_attempts: 3
                backoff: "exponential"
                initial_delay: "1s"
            }
        },
        {
            name: "reserve_inventory"
            service: "inventory_service"
            action: "reserveInventory"
            compensation: "releaseInventory"
            timeout: "30s"
            retry_policy: {
                max_attempts: 3
                backoff: "exponential"
                initial_delay: "1s"
            }
        },
        {
            name: "process_payment"
            service: "payment_service"
            action: "processPayment"
            compensation: "refundPayment"
            timeout: "60s"
            retry_policy: {
                max_attempts: 3
                backoff: "exponential"
                initial_delay: "2s"
            }
        },
        {
            name: "confirm_order"
            service: "order_service"
            action: "confirmOrder"
            compensation: "cancelOrder"
            timeout: "30s"
            retry_policy: {
                max_attempts: 3
                backoff: "exponential"
                initial_delay: "1s"
            }
        },
        {
            name: "send_notification"
            service: "notification_service"
            action: "sendOrderConfirmation"
            compensation: "sendOrderCancellation"
            timeout: "30s"
            retry_policy: {
                max_attempts: 3
                backoff: "exponential"
                initial_delay: "1s"
            }
        }
    ]
    compensation_strategy: "backward"
    timeout: "5m"
    monitoring: {
        enabled: true
        metrics: [
            "saga_duration",
            "saga_success_rate",
            "compensation_rate",
            "step_failure_rate"
        ]
    }
}

user_registration_saga: {
    name: "UserRegistrationSaga"
    description: "Register user with all required services"
    steps: [
        {
            name: "create_user"
            service: "user_service"
            action: "createUser"
            compensation: "deleteUser"
            timeout: "30s"
            retry_policy: {
                max_attempts: 3
                backoff: "exponential"
                initial_delay: "1s"
            }
        },
        {
            name: "create_profile"
            service: "profile_service"
            action: "createProfile"
            compensation: "deleteProfile"
            timeout: "30s"
            retry_policy: {
                max_attempts: 3
                backoff: "exponential"
                initial_delay: "1s"
            }
        },
        {
            name: "send_welcome_email"
            service: "notification_service"
            action: "sendWelcomeEmail"
            compensation: "sendCancellationEmail"
            timeout: "30s"
            retry_policy: {
                max_attempts: 3
                backoff: "exponential"
                initial_delay: "1s"
            }
        },
        {
            name: "activate_account"
            service: "user_service"
            action: "activateAccount"
            compensation: "deactivateAccount"
            timeout: "30s"
            retry_policy: {
                max_attempts: 3
                backoff: "exponential"
                initial_delay: "1s"
            }
        }
    ]
    compensation_strategy: "backward"
    timeout: "3m"
    monitoring: {
        enabled: true
        metrics: [
            "saga_duration",
            "saga_success_rate",
            "compensation_rate",
            "step_failure_rate"
        ]
    }
}

payment_processing_saga: {
    name: "PaymentProcessingSaga"
    description: "Process payment with fraud detection"
    steps: [
        {
            name: "validate_payment"
            service: "payment_service"
            action: "validatePayment"
            compensation: "cancelPayment"
            timeout: "30s"
            retry_policy: {
                max_attempts: 3
                backoff: "exponential"
                initial_delay: "1s"
            }
        },
        {
            name: "fraud_check"
            service: "fraud_service"
            action: "checkFraud"
            compensation: "clearFraudFlag"
            timeout: "60s"
            retry_policy: {
                max_attempts: 3
                backoff: "exponential"
                initial_delay: "2s"
            }
        },
        {
            name: "process_transaction"
            service: "payment_service"
            action: "processTransaction"
            compensation: "reverseTransaction"
            timeout: "60s"
            retry_policy: {
                max_attempts: 3
                backoff: "exponential"
                initial_delay: "2s"
            }
        },
        {
            name: "update_balance"
            service: "account_service"
            action: "updateBalance"
            compensation: "revertBalance"
            timeout: "30s"
            retry_policy: {
                max_attempts: 3
                backoff: "exponential"
                initial_delay: "1s"
            }
        },
        {
            name: "send_receipt"
            service: "notification_service"
            action: "sendReceipt"
            compensation: "sendCancellationReceipt"
            timeout: "30s"
            retry_policy: {
                max_attempts: 3
                backoff: "exponential"
                initial_delay: "1s"
            }
        }
    ]
    compensation_strategy: "backward"
    timeout: "5m"
    monitoring: {
        enabled: true
        metrics: [
            "saga_duration",
            "saga_success_rate",
            "compensation_rate",
            "step_failure_rate"
        ]
    }
}

[databases]
saga_database: {
    type: "postgresql"
    host: @env("SAGA_DB_HOST", "localhost")
    port: @env("SAGA_DB_PORT", "5432")
    database: @env("SAGA_DB_NAME", "saga_db")
    username: @env("SAGA_DB_USER", "saga_user")
    password: @env.secure("SAGA_DB_PASSWORD")
    pool_size: 20
    connection_timeout: "30s"
    tables: {
        saga_instances: {
            name: "saga_instances"
            columns: [
                "id",
                "saga_type",
                "status",
                "current_step",
                "data",
                "created_at",
                "updated_at",
                "timeout_at"
            ]
        }
        saga_steps: {
            name: "saga_steps"
            columns: [
                "id",
                "saga_instance_id",
                "step_name",
                "service",
                "action",
                "compensation",
                "status",
                "data",
                "result",
                "error",
                "attempts",
                "created_at",
                "updated_at"
            ]
        }
        saga_compensations: {
            name: "saga_compensations"
            columns: [
                "id",
                "saga_instance_id",
                "step_name",
                "service",
                "compensation_action",
                "status",
                "data",
                "result",
                "error",
                "attempts",
                "created_at",
                "updated_at"
            ]
        }
    }
}

[event_bus]
type: "kafka"
bootstrap_servers: [
    @env("KAFKA_BROKER_1", "localhost:9092"),
    @env("KAFKA_BROKER_2", "localhost:9093")
]
topics: {
    saga_events: {
        name: "saga-events"
        partitions: 10
        replication_factor: 3
        retention: "7d"
    }
    saga_compensations: {
        name: "saga-compensations"
        partitions: 10
        replication_factor: 3
        retention: "7d"
    }
}

[monitoring]
saga_metrics: {
    enabled: true
    metrics: [
        "saga_duration",
        "saga_success_rate",
        "compensation_rate",
        "step_failure_rate",
        "saga_timeout_rate"
    ]
}

distributed_tracing: {
    enabled: true
    sampling_rate: 0.1
    exporter: "jaeger"
    endpoint: @env("JAEGER_ENDPOINT", "http://jaeger:14268/api/traces")
    correlation_id_header: "X-Saga-Correlation-ID"
}

[alerts]
saga_failure: {
    enabled: true
    threshold: 0.05
    period: "5m"
    evaluation_periods: 2
    actions: [
        {
            type: "email"
            recipients: ["devops@company.com"]
        },
        {
            type: "slack"
            channel: "#alerts"
        }
    ]
}

compensation_failure: {
    enabled: true
    threshold: 0.1
    period: "5m"
    evaluation_periods: 2
    actions: [
        {
            type: "email"
            recipients: ["devops@company.com"]
        },
        {
            type: "slack"
            channel: "#alerts"
        }
    ]
}
```

## 🔄 Saga Orchestrator Implementation

### Saga Configuration
```java
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.tusklang.java.annotations.TuskConfig;
import java.util.Map;

@Configuration
@TuskConfig
public class SagaConfiguration {
    
    private final SagaConfig sagaConfig;
    
    public SagaConfiguration(SagaConfig sagaConfig) {
        this.sagaConfig = sagaConfig;
    }
    
    @Bean
    public SagaOrchestrator sagaOrchestrator() {
        SagaOrchestratorConfig config = sagaConfig.getSagaOrchestrator();
        return new SagaOrchestrator(config);
    }
    
    @Bean
    public SagaRepository sagaRepository() {
        return new PostgresSagaRepository(sagaConfig.getDatabases().get("saga_database"));
    }
    
    @Bean
    public SagaEventPublisher sagaEventPublisher() {
        return new KafkaSagaEventPublisher(sagaConfig.getEventBus());
    }
    
    @Bean
    public SagaMetrics sagaMetrics() {
        return new SagaMetrics(sagaConfig.getMonitoring());
    }
}

@TuskConfig
public class SagaConfig {
    
    private String name;
    private String version;
    private String paradigm;
    private String orchestration;
    private SagaOrchestratorConfig sagaOrchestrator;
    private Map<String, SagaDefinition> sagas;
    private Map<String, DatabaseConfig> databases;
    private EventBusConfig eventBus;
    private MonitoringConfig monitoring;
    private Map<String, AlertConfig> alerts;
    
    // Getters and setters
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    
    public String getVersion() { return version; }
    public void setVersion(String version) { this.version = version; }
    
    public String getParadigm() { return paradigm; }
    public void setParadigm(String paradigm) { this.paradigm = paradigm; }
    
    public String getOrchestration() { return orchestration; }
    public void setOrchestration(String orchestration) { this.orchestration = orchestration; }
    
    public SagaOrchestratorConfig getSagaOrchestrator() { return sagaOrchestrator; }
    public void setSagaOrchestrator(SagaOrchestratorConfig sagaOrchestrator) { this.sagaOrchestrator = sagaOrchestrator; }
    
    public Map<String, SagaDefinition> getSagas() { return sagas; }
    public void setSagas(Map<String, SagaDefinition> sagas) { this.sagas = sagas; }
    
    public Map<String, DatabaseConfig> getDatabases() { return databases; }
    public void setDatabases(Map<String, DatabaseConfig> databases) { this.databases = databases; }
    
    public EventBusConfig getEventBus() { return eventBus; }
    public void setEventBus(EventBusConfig eventBus) { this.eventBus = eventBus; }
    
    public MonitoringConfig getMonitoring() { return monitoring; }
    public void setMonitoring(MonitoringConfig monitoring) { this.monitoring = monitoring; }
    
    public Map<String, AlertConfig> getAlerts() { return alerts; }
    public void setAlerts(Map<String, AlertConfig> alerts) { this.alerts = alerts; }
}

@TuskConfig
public class SagaDefinition {
    
    private String name;
    private String description;
    private List<SagaStep> steps;
    private String compensationStrategy;
    private String timeout;
    private MonitoringConfig monitoring;
    
    // Getters and setters
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    
    public String getDescription() { return description; }
    public void setDescription(String description) { this.description = description; }
    
    public List<SagaStep> getSteps() { return steps; }
    public void setSteps(List<SagaStep> steps) { this.steps = steps; }
    
    public String getCompensationStrategy() { return compensationStrategy; }
    public void setCompensationStrategy(String compensationStrategy) { this.compensationStrategy = compensationStrategy; }
    
    public String getTimeout() { return timeout; }
    public void setTimeout(String timeout) { this.timeout = timeout; }
    
    public MonitoringConfig getMonitoring() { return monitoring; }
    public void setMonitoring(MonitoringConfig monitoring) { this.monitoring = monitoring; }
}

@TuskConfig
public class SagaStep {
    
    private String name;
    private String service;
    private String action;
    private String compensation;
    private String timeout;
    private RetryPolicyConfig retryPolicy;
    
    // Getters and setters
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    
    public String getService() { return service; }
    public void setService(String service) { this.service = service; }
    
    public String getAction() { return action; }
    public void setAction(String action) { this.action = action; }
    
    public String getCompensation() { return compensation; }
    public void setCompensation(String compensation) { this.compensation = compensation; }
    
    public String getTimeout() { return timeout; }
    public void setTimeout(String timeout) { this.timeout = timeout; }
    
    public RetryPolicyConfig getRetryPolicy() { return retryPolicy; }
    public void setRetryPolicy(RetryPolicyConfig retryPolicy) { this.retryPolicy = retryPolicy; }
}
```

### Saga Orchestrator
```java
import org.springframework.stereotype.Service;
import java.util.List;
import java.util.Map;
import java.util.concurrent.CompletableFuture;
import java.time.Instant;

@Service
public class SagaOrchestrator {
    
    private final SagaOrchestratorConfig config;
    private final SagaRepository sagaRepository;
    private final SagaEventPublisher eventPublisher;
    private final SagaMetrics metrics;
    private final Map<String, SagaDefinition> sagaDefinitions;
    
    public SagaOrchestrator(SagaOrchestratorConfig config,
                           SagaRepository sagaRepository,
                           SagaEventPublisher eventPublisher,
                           SagaMetrics metrics,
                           Map<String, SagaDefinition> sagaDefinitions) {
        this.config = config;
        this.sagaRepository = sagaRepository;
        this.eventPublisher = eventPublisher;
        this.metrics = metrics;
        this.sagaDefinitions = sagaDefinitions;
    }
    
    public CompletableFuture<SagaResult> executeSaga(String sagaType, Map<String, Object> data) {
        SagaDefinition definition = sagaDefinitions.get(sagaType);
        if (definition == null) {
            throw new SagaNotFoundException("Saga definition not found: " + sagaType);
        }
        
        // Create saga instance
        SagaInstance instance = new SagaInstance();
        instance.setId(UUID.randomUUID().toString());
        instance.setSagaType(sagaType);
        instance.setStatus(SagaStatus.RUNNING);
        instance.setCurrentStep(0);
        instance.setData(data);
        instance.setCreatedAt(Instant.now());
        instance.setTimeoutAt(Instant.now().plus(Duration.parse(definition.getTimeout())));
        
        // Save saga instance
        sagaRepository.saveSagaInstance(instance);
        
        // Publish saga started event
        eventPublisher.publishSagaStarted(instance);
        
        // Start saga execution
        return executeSagaSteps(instance, definition);
    }
    
    private CompletableFuture<SagaResult> executeSagaSteps(SagaInstance instance, SagaDefinition definition) {
        long startTime = System.currentTimeMillis();
        
        return CompletableFuture.supplyAsync(() -> {
            try {
                List<SagaStep> steps = definition.getSteps();
                
                for (int i = 0; i < steps.size(); i++) {
                    SagaStep step = steps.get(i);
                    
                    // Update current step
                    instance.setCurrentStep(i);
                    sagaRepository.updateSagaInstance(instance);
                    
                    // Execute step
                    SagaStepResult stepResult = executeStep(instance, step);
                    
                    if (!stepResult.isSuccess()) {
                        // Step failed, start compensation
                        return compensateSaga(instance, definition, i);
                    }
                    
                    // Save step result
                    saveSagaStep(instance, step, stepResult);
                }
                
                // Saga completed successfully
                instance.setStatus(SagaStatus.COMPLETED);
                sagaRepository.updateSagaInstance(instance);
                
                // Publish saga completed event
                eventPublisher.publishSagaCompleted(instance);
                
                // Record metrics
                metrics.recordSagaSuccess(instance.getSagaType(), 
                    System.currentTimeMillis() - startTime);
                
                return new SagaResult(true, "Saga completed successfully", instance.getData());
                
            } catch (Exception e) {
                log.error("Error executing saga: {}", instance.getId(), e);
                
                // Mark saga as failed
                instance.setStatus(SagaStatus.FAILED);
                sagaRepository.updateSagaInstance(instance);
                
                // Publish saga failed event
                eventPublisher.publishSagaFailed(instance, e);
                
                // Record metrics
                metrics.recordSagaFailure(instance.getSagaType(), e);
                
                return new SagaResult(false, "Saga failed: " + e.getMessage(), null);
            }
        });
    }
    
    private SagaStepResult executeStep(SagaInstance instance, SagaStep step) {
        int attempts = 0;
        int maxAttempts = step.getRetryPolicy().getMaxAttempts();
        
        while (attempts < maxAttempts) {
            try {
                // Create step record
                SagaStepRecord stepRecord = new SagaStepRecord();
                stepRecord.setId(UUID.randomUUID().toString());
                stepRecord.setSagaInstanceId(instance.getId());
                stepRecord.setStepName(step.getName());
                stepRecord.setService(step.getService());
                stepRecord.setAction(step.getAction());
                stepRecord.setCompensation(step.getCompensation());
                stepRecord.setStatus(SagaStepStatus.RUNNING);
                stepRecord.setData(instance.getData());
                stepRecord.setAttempts(attempts + 1);
                stepRecord.setCreatedAt(Instant.now());
                
                sagaRepository.saveSagaStep(stepRecord);
                
                // Execute step action
                Object result = executeStepAction(step, instance.getData());
                
                // Mark step as completed
                stepRecord.setStatus(SagaStepStatus.COMPLETED);
                stepRecord.setResult(result);
                stepRecord.setUpdatedAt(Instant.now());
                sagaRepository.updateSagaStep(stepRecord);
                
                return new SagaStepResult(true, result, null);
                
            } catch (Exception e) {
                attempts++;
                log.error("Step execution failed: {} (attempt {}/{})", step.getName(), attempts, maxAttempts, e);
                
                // Update step record with error
                stepRecord.setStatus(SagaStepStatus.FAILED);
                stepRecord.setError(e.getMessage());
                stepRecord.setUpdatedAt(Instant.now());
                sagaRepository.updateSagaStep(stepRecord);
                
                if (attempts >= maxAttempts) {
                    return new SagaStepResult(false, null, e);
                }
                
                // Wait before retry
                try {
                    Thread.sleep(calculateBackoffDelay(attempts, step.getRetryPolicy()));
                } catch (InterruptedException ie) {
                    Thread.currentThread().interrupt();
                    return new SagaStepResult(false, null, ie);
                }
            }
        }
        
        return new SagaStepResult(false, null, new RuntimeException("Max attempts exceeded"));
    }
    
    private SagaResult compensateSaga(SagaInstance instance, SagaDefinition definition, int failedStepIndex) {
        log.info("Starting compensation for saga: {} at step: {}", instance.getId(), failedStepIndex);
        
        List<SagaStep> steps = definition.getSteps();
        List<SagaStep> stepsToCompensate = steps.subList(0, failedStepIndex);
        
        // Reverse the steps for compensation
        for (int i = stepsToCompensate.size() - 1; i >= 0; i--) {
            SagaStep step = stepsToCompensate.get(i);
            
            try {
                // Execute compensation
                SagaCompensationResult compensationResult = executeCompensation(instance, step);
                
                if (!compensationResult.isSuccess()) {
                    log.error("Compensation failed for step: {}", step.getName(), compensationResult.getError());
                    // Continue with other compensations even if one fails
                }
                
                // Save compensation record
                saveSagaCompensation(instance, step, compensationResult);
                
            } catch (Exception e) {
                log.error("Error during compensation for step: {}", step.getName(), e);
                // Continue with other compensations
            }
        }
        
        // Mark saga as compensated
        instance.setStatus(SagaStatus.COMPENSATED);
        sagaRepository.updateSagaInstance(instance);
        
        // Publish saga compensated event
        eventPublisher.publishSagaCompensated(instance);
        
        // Record metrics
        metrics.recordSagaCompensation(instance.getSagaType());
        
        return new SagaResult(false, "Saga compensated", null);
    }
    
    private SagaCompensationResult executeCompensation(SagaInstance instance, SagaStep step) {
        try {
            // Create compensation record
            SagaCompensationRecord compensationRecord = new SagaCompensationRecord();
            compensationRecord.setId(UUID.randomUUID().toString());
            compensationRecord.setSagaInstanceId(instance.getId());
            compensationRecord.setStepName(step.getName());
            compensationRecord.setService(step.getService());
            compensationRecord.setCompensationAction(step.getCompensation());
            compensationRecord.setStatus(SagaCompensationStatus.RUNNING);
            compensationRecord.setData(instance.getData());
            compensationRecord.setCreatedAt(Instant.now());
            
            sagaRepository.saveSagaCompensation(compensationRecord);
            
            // Execute compensation action
            Object result = executeCompensationAction(step, instance.getData());
            
            // Mark compensation as completed
            compensationRecord.setStatus(SagaCompensationStatus.COMPLETED);
            compensationRecord.setResult(result);
            compensationRecord.setUpdatedAt(Instant.now());
            sagaRepository.updateSagaCompensation(compensationRecord);
            
            return new SagaCompensationResult(true, result, null);
            
        } catch (Exception e) {
            log.error("Compensation failed for step: {}", step.getName(), e);
            
            // Update compensation record with error
            compensationRecord.setStatus(SagaCompensationStatus.FAILED);
            compensationRecord.setError(e.getMessage());
            compensationRecord.setUpdatedAt(Instant.now());
            sagaRepository.updateSagaCompensation(compensationRecord);
            
            return new SagaCompensationResult(false, null, e);
        }
    }
    
    private Object executeStepAction(SagaStep step, Map<String, Object> data) {
        // This would typically involve calling the appropriate service
        // For now, we'll simulate the action
        switch (step.getService()) {
            case "order_service":
                return executeOrderServiceAction(step.getAction(), data);
            case "inventory_service":
                return executeInventoryServiceAction(step.getAction(), data);
            case "payment_service":
                return executePaymentServiceAction(step.getAction(), data);
            case "notification_service":
                return executeNotificationServiceAction(step.getAction(), data);
            default:
                throw new UnsupportedServiceException("Unsupported service: " + step.getService());
        }
    }
    
    private Object executeCompensationAction(SagaStep step, Map<String, Object> data) {
        // This would typically involve calling the appropriate compensation action
        // For now, we'll simulate the compensation
        switch (step.getService()) {
            case "order_service":
                return executeOrderServiceCompensation(step.getCompensation(), data);
            case "inventory_service":
                return executeInventoryServiceCompensation(step.getCompensation(), data);
            case "payment_service":
                return executePaymentServiceCompensation(step.getCompensation(), data);
            case "notification_service":
                return executeNotificationServiceCompensation(step.getCompensation(), data);
            default:
                throw new UnsupportedServiceException("Unsupported service: " + step.getService());
        }
    }
    
    private long calculateBackoffDelay(int attempt, RetryPolicyConfig retryPolicy) {
        if ("exponential".equals(retryPolicy.getBackoff())) {
            return (long) (Duration.parse(retryPolicy.getInitialDelay()).toMillis() * Math.pow(2, attempt - 1));
        } else {
            return Duration.parse(retryPolicy.getInitialDelay()).toMillis();
        }
    }
    
    // Service action implementations (simplified)
    private Object executeOrderServiceAction(String action, Map<String, Object> data) {
        // Implementation would call actual order service
        return Map.of("status", "success", "action", action);
    }
    
    private Object executeInventoryServiceAction(String action, Map<String, Object> data) {
        // Implementation would call actual inventory service
        return Map.of("status", "success", "action", action);
    }
    
    private Object executePaymentServiceAction(String action, Map<String, Object> data) {
        // Implementation would call actual payment service
        return Map.of("status", "success", "action", action);
    }
    
    private Object executeNotificationServiceAction(String action, Map<String, Object> data) {
        // Implementation would call actual notification service
        return Map.of("status", "success", "action", action);
    }
    
    // Compensation implementations (simplified)
    private Object executeOrderServiceCompensation(String compensation, Map<String, Object> data) {
        // Implementation would call actual order service compensation
        return Map.of("status", "compensated", "compensation", compensation);
    }
    
    private Object executeInventoryServiceCompensation(String compensation, Map<String, Object> data) {
        // Implementation would call actual inventory service compensation
        return Map.of("status", "compensated", "compensation", compensation);
    }
    
    private Object executePaymentServiceCompensation(String compensation, Map<String, Object> data) {
        // Implementation would call actual payment service compensation
        return Map.of("status", "compensated", "compensation", compensation);
    }
    
    private Object executeNotificationServiceCompensation(String compensation, Map<String, Object> data) {
        // Implementation would call actual notification service compensation
        return Map.of("status", "compensated", "compensation", compensation);
    }
}
```

## 🔄 Saga Service Implementation

### Order Processing Saga Service
```java
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestTemplate;
import java.util.Map;
import java.util.concurrent.CompletableFuture;

@Service
public class OrderProcessingSagaService {
    
    private final SagaOrchestrator sagaOrchestrator;
    private final RestTemplate restTemplate;
    private final SagaMetrics metrics;
    
    public OrderProcessingSagaService(SagaOrchestrator sagaOrchestrator,
                                     RestTemplate restTemplate,
                                     SagaMetrics metrics) {
        this.sagaOrchestrator = sagaOrchestrator;
        this.restTemplate = restTemplate;
        this.metrics = metrics;
    }
    
    public CompletableFuture<OrderResult> processOrder(CreateOrderRequest request) {
        // Prepare saga data
        Map<String, Object> sagaData = Map.of(
            "orderId", request.getOrderId(),
            "userId", request.getUserId(),
            "items", request.getItems(),
            "totalAmount", request.getTotalAmount(),
            "paymentMethod", request.getPaymentMethod()
        );
        
        // Execute saga
        return sagaOrchestrator.executeSaga("order_processing_saga", sagaData)
            .thenApply(sagaResult -> {
                if (sagaResult.isSuccess()) {
                    return new OrderResult(true, "Order processed successfully", 
                        sagaResult.getData().get("orderId").toString());
                } else {
                    return new OrderResult(false, sagaResult.getMessage(), null);
                }
            });
    }
    
    // Individual step implementations
    public Map<String, Object> validateOrder(Map<String, Object> data) {
        String orderId = (String) data.get("orderId");
        String userId = (String) data.get("userId");
        
        // Call order service to validate order
        OrderValidationRequest validationRequest = new OrderValidationRequest(orderId, userId);
        OrderValidationResponse response = restTemplate.postForObject(
            "http://order-service/api/orders/validate",
            validationRequest,
            OrderValidationResponse.class
        );
        
        if (response == null || !response.isValid()) {
            throw new OrderValidationException("Order validation failed: " + orderId);
        }
        
        return Map.of("validated", true, "orderId", orderId);
    }
    
    public Map<String, Object> reserveInventory(Map<String, Object> data) {
        String orderId = (String) data.get("orderId");
        List<OrderItem> items = (List<OrderItem>) data.get("items");
        
        // Call inventory service to reserve inventory
        InventoryReservationRequest reservationRequest = new InventoryReservationRequest(orderId, items);
        InventoryReservationResponse response = restTemplate.postForObject(
            "http://inventory-service/api/inventory/reserve",
            reservationRequest,
            InventoryReservationResponse.class
        );
        
        if (response == null || !response.isReserved()) {
            throw new InventoryReservationException("Inventory reservation failed: " + orderId);
        }
        
        return Map.of("reserved", true, "orderId", orderId, "reservationId", response.getReservationId());
    }
    
    public Map<String, Object> processPayment(Map<String, Object> data) {
        String orderId = (String) data.get("orderId");
        BigDecimal totalAmount = (BigDecimal) data.get("totalAmount");
        String paymentMethod = (String) data.get("paymentMethod");
        
        // Call payment service to process payment
        PaymentRequest paymentRequest = new PaymentRequest(orderId, totalAmount, paymentMethod);
        PaymentResponse response = restTemplate.postForObject(
            "http://payment-service/api/payments/process",
            paymentRequest,
            PaymentResponse.class
        );
        
        if (response == null || !response.isSuccessful()) {
            throw new PaymentProcessingException("Payment processing failed: " + orderId);
        }
        
        return Map.of("paid", true, "orderId", orderId, "transactionId", response.getTransactionId());
    }
    
    public Map<String, Object> confirmOrder(Map<String, Object> data) {
        String orderId = (String) data.get("orderId");
        
        // Call order service to confirm order
        OrderConfirmationRequest confirmationRequest = new OrderConfirmationRequest(orderId);
        OrderConfirmationResponse response = restTemplate.postForObject(
            "http://order-service/api/orders/confirm",
            confirmationRequest,
            OrderConfirmationResponse.class
        );
        
        if (response == null || !response.isConfirmed()) {
            throw new OrderConfirmationException("Order confirmation failed: " + orderId);
        }
        
        return Map.of("confirmed", true, "orderId", orderId);
    }
    
    public Map<String, Object> sendOrderConfirmation(Map<String, Object> data) {
        String orderId = (String) data.get("orderId");
        String userId = (String) data.get("userId");
        
        // Call notification service to send confirmation
        NotificationRequest notificationRequest = new NotificationRequest(
            userId, "ORDER_CONFIRMATION", Map.of("orderId", orderId)
        );
        NotificationResponse response = restTemplate.postForObject(
            "http://notification-service/api/notifications/send",
            notificationRequest,
            NotificationResponse.class
        );
        
        if (response == null || !response.isSent()) {
            throw new NotificationException("Order confirmation notification failed: " + orderId);
        }
        
        return Map.of("notified", true, "orderId", orderId);
    }
    
    // Compensation implementations
    public Map<String, Object> cancelOrder(Map<String, Object> data) {
        String orderId = (String) data.get("orderId");
        
        // Call order service to cancel order
        OrderCancellationRequest cancellationRequest = new OrderCancellationRequest(orderId);
        OrderCancellationResponse response = restTemplate.postForObject(
            "http://order-service/api/orders/cancel",
            cancellationRequest,
            OrderCancellationResponse.class
        );
        
        return Map.of("cancelled", true, "orderId", orderId);
    }
    
    public Map<String, Object> releaseInventory(Map<String, Object> data) {
        String orderId = (String) data.get("orderId");
        String reservationId = (String) data.get("reservationId");
        
        // Call inventory service to release inventory
        InventoryReleaseRequest releaseRequest = new InventoryReleaseRequest(orderId, reservationId);
        InventoryReleaseResponse response = restTemplate.postForObject(
            "http://inventory-service/api/inventory/release",
            releaseRequest,
            InventoryReleaseResponse.class
        );
        
        return Map.of("released", true, "orderId", orderId);
    }
    
    public Map<String, Object> refundPayment(Map<String, Object> data) {
        String orderId = (String) data.get("orderId");
        String transactionId = (String) data.get("transactionId");
        
        // Call payment service to refund payment
        PaymentRefundRequest refundRequest = new PaymentRefundRequest(orderId, transactionId);
        PaymentRefundResponse response = restTemplate.postForObject(
            "http://payment-service/api/payments/refund",
            refundRequest,
            PaymentRefundResponse.class
        );
        
        return Map.of("refunded", true, "orderId", orderId);
    }
    
    public Map<String, Object> sendOrderCancellation(Map<String, Object> data) {
        String orderId = (String) data.get("orderId");
        String userId = (String) data.get("userId");
        
        // Call notification service to send cancellation
        NotificationRequest notificationRequest = new NotificationRequest(
            userId, "ORDER_CANCELLATION", Map.of("orderId", orderId)
        );
        NotificationResponse response = restTemplate.postForObject(
            "http://notification-service/api/notifications/send",
            notificationRequest,
            NotificationResponse.class
        );
        
        return Map.of("cancellation_notified", true, "orderId", orderId);
    }
}
```

## 🔧 Best Practices

### 1. Saga Design
- Keep sagas small and focused
- Design compensation actions carefully
- Use backward compensation strategy
- Implement proper timeout handling

### 2. Error Handling
- Implement retry policies with exponential backoff
- Handle partial failures gracefully
- Log all saga events for debugging
- Monitor compensation success rates

### 3. Monitoring
- Track saga duration and success rates
- Monitor compensation rates
- Set up alerts for saga failures
- Use distributed tracing for debugging

### 4. Performance
- Use async processing for saga steps
- Implement proper connection pooling
- Cache frequently accessed data
- Optimize database queries

### 5. Testing
- Test saga success scenarios
- Test compensation scenarios
- Test timeout scenarios
- Use integration tests for end-to-end validation

## 🎯 Summary

TuskLang Java saga pattern provides:

- **Saga Orchestrator**: Centralized saga management
- **Compensation Logic**: Automatic rollback on failures
- **Event Publishing**: Asynchronous event processing
- **Monitoring**: Comprehensive saga metrics
- **Error Handling**: Robust retry and compensation strategies

The combination of TuskLang's executable configuration with Java's saga pattern capabilities creates a powerful platform for building reliable distributed transactions.

**"We don't bow to any king" - Build reliable distributed transactions with saga patterns!** 