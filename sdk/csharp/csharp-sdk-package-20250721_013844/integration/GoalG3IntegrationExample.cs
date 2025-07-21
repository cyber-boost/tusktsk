using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace TuskLang
{
    /// <summary>
    /// Integration example demonstrating all three goal g3 implementations working together
    /// Shows ApiGatewayServiceMesh, EventStreamingMessageQueue, and MicroservicesOrchestration in action
    /// </summary>
    public class GoalG3IntegrationExample
    {
        private readonly ApiGatewayServiceMesh _gateway;
        private readonly EventStreamingMessageQueue _eventStreaming;
        private readonly MicroservicesOrchestration _orchestration;
        private readonly TSK _tsk;

        public GoalG3IntegrationExample()
        {
            _gateway = new ApiGatewayServiceMesh();
            _eventStreaming = new EventStreamingMessageQueue();
            _orchestration = new MicroservicesOrchestration();
            _tsk = new TSK();
        }

        /// <summary>
        /// Execute a comprehensive microservices operation using all three systems
        /// </summary>
        public async Task<G3IntegrationResult> ExecuteComprehensiveMicroservicesOperation(
            Dictionary<string, object> inputs,
            string operationName = "comprehensive_microservices_operation")
        {
            var result = new G3IntegrationResult();
            var startTime = DateTime.UtcNow;

            try
            {
                // Step 1: Set up services and workflows
                await SetupServicesAndWorkflows();

                // Step 2: Create event streams and message queues
                await SetupEventStreamsAndQueues();

                // Step 3: Execute gateway request
                var gatewayRequest = new GatewayRequest
                {
                    Method = "POST",
                    Path = "/api/microservices/operation",
                    Headers = new Dictionary<string, string>
                    {
                        ["Authorization"] = "Bearer token123",
                        ["Content-Type"] = "application/json"
                    },
                    Body = System.Text.Json.JsonSerializer.Serialize(inputs),
                    ClientId = "client123",
                    ServiceName = "microservices-service"
                };

                var gatewayResponse = await _gateway.RouteRequestAsync(gatewayRequest);
                result.GatewayResults = gatewayResponse;

                // Step 4: Execute workflow orchestration
                if (inputs.ContainsKey("workflow_name"))
                {
                    var workflowName = inputs["workflow_name"].ToString();
                    var workflowInput = inputs.ContainsKey("workflow_input") 
                        ? inputs["workflow_input"] as Dictionary<string, object> 
                        : new Dictionary<string, object>();

                    var workflowResult = await _orchestration.ExecuteWorkflowAsync(workflowName, workflowInput);
                    result.WorkflowResults = workflowResult;
                }

                // Step 5: Execute saga pattern
                if (inputs.ContainsKey("saga_name"))
                {
                    var sagaName = inputs["saga_name"].ToString();
                    var sagaInput = inputs.ContainsKey("saga_input") 
                        ? inputs["saga_input"] as Dictionary<string, object> 
                        : new Dictionary<string, object>();

                    var sagaResult = await _orchestration.StartSagaAsync(sagaName, sagaInput);
                    result.SagaResults = sagaResult;
                }

                // Step 6: Publish events and send messages
                await PublishEventsAndMessages(inputs);

                // Step 7: Execute FUJSEN with microservices context
                if (inputs.ContainsKey("fujsen_code"))
                {
                    var fujsenResult = await ExecuteFujsenWithMicroservices(
                        inputs["fujsen_code"].ToString(),
                        inputs);
                    result.FujsenResults = fujsenResult;
                }

                result.Success = true;
                result.ExecutionTime = DateTime.UtcNow - startTime;

                // Step 8: Collect metrics from all systems
                result.Metrics = new G3IntegrationMetrics
                {
                    GatewayMetrics = _gateway.GetMetrics(),
                    EventMetrics = _eventStreaming.GetMetrics(),
                    OrchestrationMetrics = _orchestration.GetMetrics()
                };

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Microservices operation failed: {ex.Message}");
                result.ExecutionTime = DateTime.UtcNow - startTime;

                return result;
            }
        }

        /// <summary>
        /// Set up services and workflows for the orchestration system
        /// </summary>
        private async Task SetupServicesAndWorkflows()
        {
            // Register microservices
            var userService = new Microservice
            {
                Name = "user-service",
                Endpoint = "http://localhost:5001",
                Operations = new Dictionary<string, string>
                {
                    ["create"] = "POST /users",
                    ["get"] = "GET /users/{id}",
                    ["update"] = "PUT /users/{id}",
                    ["delete"] = "DELETE /users/{id}"
                }
            };

            var orderService = new Microservice
            {
                Name = "order-service",
                Endpoint = "http://localhost:5002",
                Operations = new Dictionary<string, string>
                {
                    ["create"] = "POST /orders",
                    ["get"] = "GET /orders/{id}",
                    ["update"] = "PUT /orders/{id}",
                    ["cancel"] = "POST /orders/{id}/cancel"
                }
            };

            var paymentService = new Microservice
            {
                Name = "payment-service",
                Endpoint = "http://localhost:5003",
                Operations = new Dictionary<string, string>
                {
                    ["process"] = "POST /payments",
                    ["refund"] = "POST /payments/{id}/refund",
                    ["status"] = "GET /payments/{id}"
                }
            };

            _orchestration.RegisterService("user-service", userService);
            _orchestration.RegisterService("order-service", orderService);
            _orchestration.RegisterService("payment-service", paymentService);

            // Register services with gateway
            _gateway.RegisterService("user-service", new ServiceEndpoint
            {
                ServiceName = "user-service",
                BaseUrl = "http://localhost:5001",
                Instances = new List<string> { "http://localhost:5001", "http://localhost:5001-backup" },
                HealthCheckUrl = "http://localhost:5001/health"
            });

            _gateway.RegisterService("order-service", new ServiceEndpoint
            {
                ServiceName = "order-service",
                BaseUrl = "http://localhost:5002",
                Instances = new List<string> { "http://localhost:5002" },
                HealthCheckUrl = "http://localhost:5002/health"
            });

            _gateway.RegisterService("payment-service", new ServiceEndpoint
            {
                ServiceName = "payment-service",
                BaseUrl = "http://localhost:5003",
                Instances = new List<string> { "http://localhost:5003" },
                HealthCheckUrl = "http://localhost:5003/health"
            });

            // Create workflows
            var orderWorkflow = new WorkflowDefinition
            {
                Type = WorkflowType.Sequential,
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Id = "step1",
                        Name = "Create User",
                        ServiceName = "user-service",
                        Operation = "create"
                    },
                    new WorkflowStep
                    {
                        Id = "step2",
                        Name = "Create Order",
                        ServiceName = "order-service",
                        Operation = "create"
                    },
                    new WorkflowStep
                    {
                        Id = "step3",
                        Name = "Process Payment",
                        ServiceName = "payment-service",
                        Operation = "process"
                    }
                }
            };

            _orchestration.CreateWorkflow("order-processing", orderWorkflow);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Set up event streams and message queues
        /// </summary>
        private async Task SetupEventStreamsAndQueues()
        {
            // Create event streams
            _eventStreaming.CreateEventStream("user-events", new EventStreamConfig
            {
                MaxEvents = 1000,
                RetentionPeriod = TimeSpan.FromDays(30)
            });

            _eventStreaming.CreateEventStream("order-events", new EventStreamConfig
            {
                MaxEvents = 1000,
                RetentionPeriod = TimeSpan.FromDays(30)
            });

            _eventStreaming.CreateEventStream("payment-events", new EventStreamConfig
            {
                MaxEvents = 1000,
                RetentionPeriod = TimeSpan.FromDays(30)
            });

            // Create message queues
            _eventStreaming.CreateMessageQueue("order-queue", new MessageQueueConfig
            {
                MaxConcurrentConsumers = 5,
                MaxQueueSize = 10000
            });

            _eventStreaming.CreateMessageQueue("payment-queue", new MessageQueueConfig
            {
                MaxConcurrentConsumers = 3,
                MaxQueueSize = 5000
            });

            // Subscribe to event streams
            await _eventStreaming.SubscribeToStreamAsync("user-events", async (eventData) =>
            {
                Console.WriteLine($"User event received: {eventData.EventType}");
                // Process user event
            });

            await _eventStreaming.SubscribeToStreamAsync("order-events", async (eventData) =>
            {
                Console.WriteLine($"Order event received: {eventData.EventType}");
                // Process order event
            });

            await Task.CompletedTask;
        }

        /// <summary>
        /// Publish events and send messages
        /// </summary>
        private async Task PublishEventsAndMessages(Dictionary<string, object> inputs)
        {
            // Publish user event
            var userEvent = new EventData
            {
                EventType = "user.created",
                Data = new { userId = "123", email = "user@example.com" }
            };

            await _eventStreaming.PublishEventAsync("user-events", userEvent);

            // Publish order event
            var orderEvent = new EventData
            {
                EventType = "order.created",
                Data = new { orderId = "456", userId = "123", amount = 99.99 }
            };

            await _eventStreaming.PublishEventAsync("order-events", orderEvent);

            // Send order message
            var orderMessage = new MessageData
            {
                MessageType = "order.process",
                Data = new { orderId = "456", action = "process" }
            };

            await _eventStreaming.SendMessageAsync("order-queue", orderMessage);

            // Send payment message
            var paymentMessage = new MessageData
            {
                MessageType = "payment.process",
                Data = new { orderId = "456", amount = 99.99 }
            };

            await _eventStreaming.SendMessageAsync("payment-queue", paymentMessage);
        }

        /// <summary>
        /// Execute FUJSEN with microservices context
        /// </summary>
        private async Task<FujsenOperationResult> ExecuteFujsenWithMicroservices(
            string fujsenCode,
            Dictionary<string, object> context)
        {
            try
            {
                // Set up TSK with the FUJSEN code and microservices context
                _tsk.SetSection("microservices_section", new Dictionary<string, object>
                {
                    ["fujsen"] = fujsenCode,
                    ["gateway"] = _gateway,
                    ["event_streaming"] = _eventStreaming,
                    ["orchestration"] = _orchestration
                });

                // Execute with context injection
                var result = await _tsk.ExecuteFujsenWithContext("microservices_section", "fujsen", context);

                return new FujsenOperationResult
                {
                    Success = true,
                    Result = result,
                    ExecutionTime = TimeSpan.Zero
                };
            }
            catch (Exception ex)
            {
                return new FujsenOperationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Get comprehensive microservices health report
        /// </summary>
        public async Task<G3SystemHealthReport> GetMicroservicesHealthReport()
        {
            var gatewayHealth = await _gateway.GetServiceHealthAsync();
            var orchestrationHealth = await _orchestration.GetServiceHealthAsync();

            return new G3SystemHealthReport
            {
                Timestamp = DateTime.UtcNow,
                GatewayHealth = gatewayHealth,
                OrchestrationHealth = orchestrationHealth,
                EventStreams = _eventStreaming.GetStreamNames(),
                MessageQueues = _eventStreaming.GetQueueNames(),
                OverallHealth = CalculateG3OverallHealth(gatewayHealth, orchestrationHealth)
            };
        }

        /// <summary>
        /// Execute batch microservices operations
        /// </summary>
        public async Task<List<G3IntegrationResult>> ExecuteBatchMicroservicesOperations(
            List<Dictionary<string, object>> inputsList)
        {
            var tasks = inputsList.Select(inputs => ExecuteComprehensiveMicroservicesOperation(inputs)).ToList();
            return await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Get service registry summary
        /// </summary>
        public async Task<ServiceRegistrySummary> GetServiceRegistrySummary()
        {
            return new ServiceRegistrySummary
            {
                ServiceNames = _orchestration.GetServiceNames(),
                WorkflowNames = _orchestration.GetWorkflowNames(),
                StreamNames = _eventStreaming.GetStreamNames(),
                QueueNames = _eventStreaming.GetQueueNames()
            };
        }

        private G3SystemHealth CalculateG3OverallHealth(
            Dictionary<string, ServiceHealth> gatewayHealth,
            Dictionary<string, ServiceHealthStatus> orchestrationHealth)
        {
            // Calculate health based on various metrics
            var gatewayHealthScore = gatewayHealth.Values.Count(h => h.IsHealthy) / (double)gatewayHealth.Count;
            var orchestrationHealthScore = orchestrationHealth.Values.Count(h => h.IsHealthy) / (double)orchestrationHealth.Count;

            var overallHealth = (gatewayHealthScore + orchestrationHealthScore) / 2.0;

            if (overallHealth > 0.9)
                return G3SystemHealth.Excellent;
            else if (overallHealth > 0.7)
                return G3SystemHealth.Good;
            else if (overallHealth > 0.5)
                return G3SystemHealth.Fair;
            else
                return G3SystemHealth.Poor;
        }
    }

    public class G3IntegrationResult
    {
        public bool Success { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public TimeSpan ExecutionTime { get; set; }
        public GatewayResponse GatewayResults { get; set; }
        public WorkflowExecutionResult WorkflowResults { get; set; }
        public SagaResult SagaResults { get; set; }
        public FujsenOperationResult FujsenResults { get; set; }
        public G3IntegrationMetrics Metrics { get; set; }
    }

    public class FujsenOperationResult
    {
        public bool Success { get; set; }
        public object Result { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class G3IntegrationMetrics
    {
        public GatewayMetrics GatewayMetrics { get; set; }
        public EventMetrics EventMetrics { get; set; }
        public OrchestrationMetrics OrchestrationMetrics { get; set; }
    }

    public class G3SystemHealthReport
    {
        public DateTime Timestamp { get; set; }
        public Dictionary<string, ServiceHealth> GatewayHealth { get; set; }
        public Dictionary<string, ServiceHealthStatus> OrchestrationHealth { get; set; }
        public List<string> EventStreams { get; set; }
        public List<string> MessageQueues { get; set; }
        public G3SystemHealth OverallHealth { get; set; }
    }

    public class ServiceRegistrySummary
    {
        public List<string> ServiceNames { get; set; }
        public List<string> WorkflowNames { get; set; }
        public List<string> StreamNames { get; set; }
        public List<string> QueueNames { get; set; }
    }

    public enum G3SystemHealth
    {
        Poor,
        Fair,
        Good,
        Excellent
    }
} 