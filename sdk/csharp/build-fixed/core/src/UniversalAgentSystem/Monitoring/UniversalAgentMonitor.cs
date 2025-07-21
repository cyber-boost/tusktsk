using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Collections.Concurrent;

namespace TODO2.UniversalAgentSystem.Monitoring
{
    /// <summary>
    /// Universal Agent Monitoring & Analytics Dashboard
    /// Real-time monitoring system for all deployed agents
    /// </summary>
    public class UniversalAgentMonitor
    {
        private readonly ILogger<UniversalAgentMonitor> _logger;
        private readonly ConcurrentDictionary<string, AgentStatus> _agentStatuses;
        private readonly ConcurrentDictionary<string, AgentMetrics> _agentMetrics;
        private readonly Timer _metricsCollectionTimer;
        private readonly Timer _healthCheckTimer;
        private readonly IAgentMetricsCollector _metricsCollector;
        private readonly IAgentHealthChecker _healthChecker;

        public UniversalAgentMonitor(
            ILogger<UniversalAgentMonitor> logger,
            IAgentMetricsCollector metricsCollector,
            IAgentHealthChecker healthChecker)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _agentStatuses = new ConcurrentDictionary<string, AgentStatus>();
            _agentMetrics = new ConcurrentDictionary<string, AgentMetrics>();
            _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
            _healthChecker = healthChecker ?? throw new ArgumentNullException(nameof(healthChecker));

            // Start monitoring timers
            _metricsCollectionTimer = new Timer(CollectMetricsAsync, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
            _healthCheckTimer = new Timer(PerformHealthChecksAsync, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Register an agent for monitoring
        /// </summary>
        public async Task RegisterAgentAsync(string agentId, AgentConfiguration config)
        {
            try
            {
                var status = new AgentStatus
                {
                    AgentId = agentId,
                    Configuration = config,
                    Status = AgentState.Registered,
                    RegisteredAt = DateTime.UtcNow,
                    LastHeartbeat = DateTime.UtcNow
                };

                _agentStatuses[agentId] = status;

                var metrics = new AgentMetrics
                {
                    AgentId = agentId,
                    GoalsCompleted = 0,
                    GoalsTotal = config.TotalGoals,
                    ResponseTimeAverage = 0,
                    MemoryUsageAverage = 0,
                    UptimePercentage = 100.0,
                    ErrorCount = 0,
                    LastUpdated = DateTime.UtcNow
                };

                _agentMetrics[agentId] = metrics;

                _logger.LogInformation("Agent {AgentId} registered for monitoring", agentId);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register agent {AgentId} for monitoring", agentId);
                throw;
            }
        }

        /// <summary>
        /// Update agent status
        /// </summary>
        public async Task UpdateAgentStatusAsync(string agentId, AgentState status, string message = "")
        {
            if (_agentStatuses.TryGetValue(agentId, out var agentStatus))
            {
                agentStatus.Status = status;
                agentStatus.LastUpdated = DateTime.UtcNow;
                agentStatus.Message = message;

                if (status == AgentState.Completed)
                {
                    agentStatus.CompletedAt = DateTime.UtcNow;
                }

                _logger.LogInformation("Agent {AgentId} status updated to {Status}: {Message}", agentId, status, message);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Update agent metrics
        /// </summary>
        public async Task UpdateAgentMetricsAsync(string agentId, AgentMetricsUpdate update)
        {
            if (_agentMetrics.TryGetValue(agentId, out var metrics))
            {
                metrics.GoalsCompleted = update.GoalsCompleted;
                metrics.ResponseTimeAverage = update.ResponseTimeAverage;
                metrics.MemoryUsageAverage = update.MemoryUsageAverage;
                metrics.ErrorCount = update.ErrorCount;
                metrics.LastUpdated = DateTime.UtcNow;

                // Calculate uptime percentage
                if (metrics.GoalsTotal > 0)
                {
                    metrics.UptimePercentage = (double)metrics.GoalsCompleted / metrics.GoalsTotal * 100.0;
                }

                _logger.LogDebug("Agent {AgentId} metrics updated: {GoalsCompleted}/{GoalsTotal} goals completed", 
                    agentId, metrics.GoalsCompleted, metrics.GoalsTotal);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Get real-time dashboard data
        /// </summary>
        public async Task<DashboardData> GetDashboardDataAsync()
        {
            var agents = new List<AgentDashboardInfo>();

            foreach (var kvp in _agentStatuses)
            {
                var agentId = kvp.Key;
                var status = kvp.Value;
                var metrics = _agentMetrics.TryGetValue(agentId, out var m) ? m : new AgentMetrics();

                agents.Add(new AgentDashboardInfo
                {
                    AgentId = agentId,
                    Status = status.Status,
                    Specialty = status.Configuration.AgentSpecialty,
                    Language = status.Configuration.Language,
                    GoalsCompleted = metrics.GoalsCompleted,
                    GoalsTotal = metrics.GoalsTotal,
                    CompletionPercentage = metrics.GoalsTotal > 0 ? (double)metrics.GoalsCompleted / metrics.GoalsTotal * 100.0 : 0.0,
                    ResponseTimeAverage = metrics.ResponseTimeAverage,
                    MemoryUsageAverage = metrics.MemoryUsageAverage,
                    UptimePercentage = metrics.UptimePercentage,
                    ErrorCount = metrics.ErrorCount,
                    LastHeartbeat = status.LastHeartbeat,
                    RegisteredAt = status.RegisteredAt,
                    LastUpdated = status.LastUpdated
                });
            }

            var systemMetrics = await CalculateSystemMetricsAsync(agents);

            return new DashboardData
            {
                Agents = agents,
                SystemMetrics = systemMetrics,
                LastUpdated = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Get agent-specific analytics
        /// </summary>
        public async Task<AgentAnalytics> GetAgentAnalyticsAsync(string agentId)
        {
            if (!_agentStatuses.TryGetValue(agentId, out var status) || 
                !_agentMetrics.TryGetValue(agentId, out var metrics))
            {
                throw new ArgumentException($"Agent {agentId} not found in monitoring system");
            }

            var performanceHistory = await _metricsCollector.GetPerformanceHistoryAsync(agentId);
            var errorHistory = await _metricsCollector.GetErrorHistoryAsync(agentId);
            var goalHistory = await _metricsCollector.GetGoalHistoryAsync(agentId);

            return new AgentAnalytics
            {
                AgentId = agentId,
                Configuration = status.Configuration,
                CurrentMetrics = metrics,
                PerformanceHistory = performanceHistory,
                ErrorHistory = errorHistory,
                GoalHistory = goalHistory,
                GeneratedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Generate automated reports
        /// </summary>
        public async Task<AutomatedReport> GenerateAutomatedReportAsync(ReportType reportType)
        {
            var dashboardData = await GetDashboardDataAsync();
            var report = new AutomatedReport
            {
                ReportType = reportType,
                GeneratedAt = DateTime.UtcNow,
                Summary = await GenerateReportSummaryAsync(dashboardData, reportType),
                Details = await GenerateReportDetailsAsync(dashboardData, reportType),
                Recommendations = await GenerateRecommendationsAsync(dashboardData, reportType)
            };

            _logger.LogInformation("Generated {ReportType} report with {AgentCount} agents", reportType, dashboardData.Agents.Count);
            return report;
        }

        /// <summary>
        /// Collect metrics from all agents
        /// </summary>
        private async void CollectMetricsAsync(object state)
        {
            try
            {
                foreach (var agentId in _agentStatuses.Keys)
                {
                    var metrics = await _metricsCollector.CollectMetricsAsync(agentId);
                    if (metrics != null)
                    {
                        await UpdateAgentMetricsAsync(agentId, metrics);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting metrics from agents");
            }
        }

        /// <summary>
        /// Perform health checks on all agents
        /// </summary>
        private async void PerformHealthChecksAsync(object state)
        {
            try
            {
                foreach (var agentId in _agentStatuses.Keys)
                {
                    var healthStatus = await _healthChecker.CheckHealthAsync(agentId);
                    
                    if (healthStatus.IsHealthy)
                    {
                        if (_agentStatuses.TryGetValue(agentId, out var status))
                        {
                            status.LastHeartbeat = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        await UpdateAgentStatusAsync(agentId, AgentState.Unhealthy, healthStatus.Message);
                        _logger.LogWarning("Agent {AgentId} health check failed: {Message}", agentId, healthStatus.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing health checks on agents");
            }
        }

        /// <summary>
        /// Calculate system-wide metrics
        /// </summary>
        private async Task<SystemMetrics> CalculateSystemMetricsAsync(List<AgentDashboardInfo> agents)
        {
            if (agents.Count == 0)
            {
                return new SystemMetrics();
            }

            var totalAgents = agents.Count;
            var activeAgents = agents.Count(a => a.Status == AgentState.Active || a.Status == AgentState.Running);
            var completedAgents = agents.Count(a => a.Status == AgentState.Completed);
            var failedAgents = agents.Count(a => a.Status == AgentState.Failed || a.Status == AgentState.Unhealthy);

            var totalGoals = agents.Sum(a => a.GoalsTotal);
            var completedGoals = agents.Sum(a => a.GoalsCompleted);
            var overallCompletion = totalGoals > 0 ? (double)completedGoals / totalGoals * 100.0 : 0.0;

            var averageResponseTime = agents.Average(a => a.ResponseTimeAverage);
            var averageMemoryUsage = agents.Average(a => a.MemoryUsageAverage);
            var totalErrors = agents.Sum(a => a.ErrorCount);

            return new SystemMetrics
            {
                TotalAgents = totalAgents,
                ActiveAgents = activeAgents,
                CompletedAgents = completedAgents,
                FailedAgents = failedAgents,
                OverallCompletionPercentage = overallCompletion,
                AverageResponseTime = averageResponseTime,
                AverageMemoryUsage = averageMemoryUsage,
                TotalErrors = totalErrors,
                SystemUptime = 100.0 - (failedAgents > 0 ? (double)failedAgents / totalAgents * 100.0 : 0.0)
            };
        }

        /// <summary>
        /// Generate report summary
        /// </summary>
        private async Task<string> GenerateReportSummaryAsync(DashboardData dashboardData, ReportType reportType)
        {
            var systemMetrics = dashboardData.SystemMetrics;
            
            return reportType switch
            {
                ReportType.Daily => $"Daily Report: {systemMetrics.TotalAgents} agents monitored, {systemMetrics.CompletedAgents} completed, {systemMetrics.OverallCompletionPercentage:F1}% overall completion",
                ReportType.Weekly => $"Weekly Report: {systemMetrics.TotalAgents} agents active, {systemMetrics.AverageResponseTime:F0}ms avg response time, {systemMetrics.TotalErrors} total errors",
                ReportType.Monthly => $"Monthly Report: {systemMetrics.TotalAgents} agents deployed, {systemMetrics.SystemUptime:F1}% system uptime, {systemMetrics.AverageMemoryUsage:F0}MB avg memory usage",
                _ => $"Report: {systemMetrics.TotalAgents} agents, {systemMetrics.OverallCompletionPercentage:F1}% completion"
            };
        }

        /// <summary>
        /// Generate report details
        /// </summary>
        private async Task<string> GenerateReportDetailsAsync(DashboardData dashboardData, ReportType reportType)
        {
            var details = new List<string>();

            foreach (var agent in dashboardData.Agents)
            {
                details.Add($"Agent {agent.AgentId}: {agent.Status}, {agent.GoalsCompleted}/{agent.GoalsTotal} goals, {agent.CompletionPercentage:F1}% completion");
            }

            return string.Join("\n", details);
        }

        /// <summary>
        /// Generate recommendations
        /// </summary>
        private async Task<List<string>> GenerateRecommendationsAsync(DashboardData dashboardData, ReportType reportType)
        {
            var recommendations = new List<string>();
            var systemMetrics = dashboardData.SystemMetrics;

            if (systemMetrics.FailedAgents > 0)
            {
                recommendations.Add($"Investigate {systemMetrics.FailedAgents} failed agents for potential issues");
            }

            if (systemMetrics.AverageResponseTime > 100)
            {
                recommendations.Add("Consider performance optimization for agents with high response times");
            }

            if (systemMetrics.TotalErrors > 0)
            {
                recommendations.Add($"Review error logs for {systemMetrics.TotalErrors} total errors across all agents");
            }

            if (systemMetrics.OverallCompletionPercentage < 80)
            {
                recommendations.Add("Focus on completing pending goals to improve overall completion rate");
            }

            return recommendations;
        }

        public void Dispose()
        {
            _metricsCollectionTimer?.Dispose();
            _healthCheckTimer?.Dispose();
        }
    }

    // Data models
    public class AgentStatus
    {
        public string AgentId { get; set; } = string.Empty;
        public AgentConfiguration Configuration { get; set; } = new();
        public AgentState Status { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class AgentMetrics
    {
        public string AgentId { get; set; } = string.Empty;
        public int GoalsCompleted { get; set; }
        public int GoalsTotal { get; set; }
        public double ResponseTimeAverage { get; set; }
        public double MemoryUsageAverage { get; set; }
        public double UptimePercentage { get; set; }
        public int ErrorCount { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class AgentMetricsUpdate
    {
        public int GoalsCompleted { get; set; }
        public double ResponseTimeAverage { get; set; }
        public double MemoryUsageAverage { get; set; }
        public int ErrorCount { get; set; }
    }

    public class DashboardData
    {
        public List<AgentDashboardInfo> Agents { get; set; } = new();
        public SystemMetrics SystemMetrics { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    public class AgentDashboardInfo
    {
        public string AgentId { get; set; } = string.Empty;
        public AgentState Status { get; set; }
        public string Specialty { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public int GoalsCompleted { get; set; }
        public int GoalsTotal { get; set; }
        public double CompletionPercentage { get; set; }
        public double ResponseTimeAverage { get; set; }
        public double MemoryUsageAverage { get; set; }
        public double UptimePercentage { get; set; }
        public int ErrorCount { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class SystemMetrics
    {
        public int TotalAgents { get; set; }
        public int ActiveAgents { get; set; }
        public int CompletedAgents { get; set; }
        public int FailedAgents { get; set; }
        public double OverallCompletionPercentage { get; set; }
        public double AverageResponseTime { get; set; }
        public double AverageMemoryUsage { get; set; }
        public int TotalErrors { get; set; }
        public double SystemUptime { get; set; }
    }

    public class AgentAnalytics
    {
        public string AgentId { get; set; } = string.Empty;
        public AgentConfiguration Configuration { get; set; } = new();
        public AgentMetrics CurrentMetrics { get; set; } = new();
        public List<PerformanceDataPoint> PerformanceHistory { get; set; } = new();
        public List<ErrorDataPoint> ErrorHistory { get; set; } = new();
        public List<GoalDataPoint> GoalHistory { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class AutomatedReport
    {
        public ReportType ReportType { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public List<string> Recommendations { get; set; } = new();
    }

    public class PerformanceDataPoint
    {
        public DateTime Timestamp { get; set; }
        public double ResponseTime { get; set; }
        public double MemoryUsage { get; set; }
        public double CpuUsage { get; set; }
    }

    public class ErrorDataPoint
    {
        public DateTime Timestamp { get; set; }
        public string ErrorType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
    }

    public class GoalDataPoint
    {
        public DateTime Timestamp { get; set; }
        public int GoalNumber { get; set; }
        public string GoalTitle { get; set; } = string.Empty;
        public GoalState State { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public enum AgentState
    {
        Registered,
        Active,
        Running,
        Completed,
        Failed,
        Unhealthy,
        Stopped
    }

    public enum GoalState
    {
        Pending,
        Running,
        Completed,
        Failed,
        Skipped
    }

    public enum ReportType
    {
        Daily,
        Weekly,
        Monthly,
        Custom
    }

    // Interfaces for extensibility
    public interface IAgentMetricsCollector
    {
        Task<AgentMetricsUpdate?> CollectMetricsAsync(string agentId);
        Task<List<PerformanceDataPoint>> GetPerformanceHistoryAsync(string agentId);
        Task<List<ErrorDataPoint>> GetErrorHistoryAsync(string agentId);
        Task<List<GoalDataPoint>> GetGoalHistoryAsync(string agentId);
    }

    public interface IAgentHealthChecker
    {
        Task<HealthStatus> CheckHealthAsync(string agentId);
    }

    public class HealthStatus
    {
        public bool IsHealthy { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CheckedAt { get; set; }
    }

    // Default implementations
    public class DefaultMetricsCollector : IAgentMetricsCollector
    {
        public async Task<AgentMetricsUpdate?> CollectMetricsAsync(string agentId)
        {
            // Default implementation - would be replaced with real metrics collection
            await Task.Delay(10);
            return new AgentMetricsUpdate
            {
                GoalsCompleted = 0,
                ResponseTimeAverage = 50.0,
                MemoryUsageAverage = 100.0,
                ErrorCount = 0
            };
        }

        public async Task<List<PerformanceDataPoint>> GetPerformanceHistoryAsync(string agentId)
        {
            await Task.CompletedTask;
            return new List<PerformanceDataPoint>();
        }

        public async Task<List<ErrorDataPoint>> GetErrorHistoryAsync(string agentId)
        {
            await Task.CompletedTask;
            return new List<ErrorDataPoint>();
        }

        public async Task<List<GoalDataPoint>> GetGoalHistoryAsync(string agentId)
        {
            await Task.CompletedTask;
            return new List<GoalDataPoint>();
        }
    }

    public class DefaultHealthChecker : IAgentHealthChecker
    {
        public async Task<HealthStatus> CheckHealthAsync(string agentId)
        {
            await Task.Delay(10);
            return new HealthStatus
            {
                IsHealthy = true,
                Message = "Agent is healthy",
                CheckedAt = DateTime.UtcNow
            };
        }
    }
} 