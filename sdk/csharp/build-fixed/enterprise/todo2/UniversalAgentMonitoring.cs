using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Diagnostics;

namespace TuskLang.Todo2.Monitoring
{
    /// <summary>
    /// Universal agent monitoring and analytics dashboard system
    /// </summary>
    public class UniversalAgentMonitoring
    {
        private readonly ILogger<UniversalAgentMonitoring> _logger;
        private readonly IMemoryCache _cache;
        private readonly ConcurrentDictionary<string, AgentStatus> _agentStatuses;
        private readonly ConcurrentDictionary<string, PerformanceMetrics> _performanceMetrics;
        private readonly ConcurrentDictionary<string, CompletionAnalytics> _completionAnalytics;
        private readonly Timer _monitoringTimer;
        private readonly Timer _reportingTimer;
        private readonly Timer _cleanupTimer;
        private readonly string _monitoringDataPath;
        private readonly HttpClient _httpClient;

        public UniversalAgentMonitoring(ILogger<UniversalAgentMonitoring> logger, IMemoryCache cache, HttpClient httpClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _agentStatuses = new ConcurrentDictionary<string, AgentStatus>();
            _performanceMetrics = new ConcurrentDictionary<string, PerformanceMetrics>();
            _completionAnalytics = new ConcurrentDictionary<string, CompletionAnalytics>();
            _monitoringDataPath = Path.Combine(Environment.CurrentDirectory, "monitoring_data");

            // Ensure monitoring directory exists
            Directory.CreateDirectory(_monitoringDataPath);

            // Start monitoring timers
            _monitoringTimer = new Timer(CollectAgentStatus, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
            _reportingTimer = new Timer(GenerateReports, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            _cleanupTimer = new Timer(CleanupOldData, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));

            _logger.LogInformation("Universal Agent Monitoring initialized with real-time tracking");
        }

        /// <summary>
        /// Agent status information
        /// </summary>
        public class AgentStatus
        {
            public string AgentId { get; set; }
            public string Language { get; set; }
            public string Framework { get; set; }
            public string Platform { get; set; }
            public AgentState State { get; set; }
            public DateTime LastHeartbeat { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public int TasksCompleted { get; set; }
            public int TasksFailed { get; set; }
            public int TasksInProgress { get; set; }
            public double CpuUsage { get; set; }
            public double MemoryUsage { get; set; }
            public double NetworkUsage { get; set; }
            public List<string> ActiveTasks { get; set; }
            public List<string> RecentErrors { get; set; }
            public Dictionary<string, object> CustomMetrics { get; set; }

            public AgentStatus()
            {
                ActiveTasks = new List<string>();
                RecentErrors = new List<string>();
                CustomMetrics = new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Agent states
        /// </summary>
        public enum AgentState
        {
            Unknown,
            Starting,
            Running,
            Idle,
            Busy,
            Stopping,
            Stopped,
            Error,
            Offline
        }

        /// <summary>
        /// Performance metrics for agents
        /// </summary>
        public class PerformanceMetrics
        {
            public string AgentId { get; set; }
            public DateTime Timestamp { get; set; }
            public double AverageResponseTime { get; set; }
            public double Throughput { get; set; }
            public double ErrorRate { get; set; }
            public double Availability { get; set; }
            public double ResourceUtilization { get; set; }
            public List<double> ResponseTimeHistory { get; set; }
            public List<double> ThroughputHistory { get; set; }
            public Dictionary<string, double> CustomMetrics { get; set; }

            public PerformanceMetrics()
            {
                ResponseTimeHistory = new List<double>();
                ThroughputHistory = new List<double>();
                CustomMetrics = new Dictionary<string, double>();
            }
        }

        /// <summary>
        /// Completion analytics for agents
        /// </summary>
        public class CompletionAnalytics
        {
            public string AgentId { get; set; }
            public DateTime Date { get; set; }
            public int TotalTasks { get; set; }
            public int CompletedTasks { get; set; }
            public int FailedTasks { get; set; }
            public int CancelledTasks { get; set; }
            public double CompletionRate { get; set; }
            public double SuccessRate { get; set; }
            public TimeSpan AverageTaskDuration { get; set; }
            public TimeSpan TotalExecutionTime { get; set; }
            public Dictionary<string, int> TaskTypeDistribution { get; set; }
            public List<string> CommonErrors { get; set; }

            public CompletionAnalytics()
            {
                TaskTypeDistribution = new Dictionary<string, int>();
                CommonErrors = new List<string>();
            }
        }

        /// <summary>
        /// Dashboard data for real-time monitoring
        /// </summary>
        public class DashboardData
        {
            public Dictionary<string, AgentStatus> AgentStatuses { get; set; }
            public Dictionary<string, PerformanceMetrics> PerformanceMetrics { get; set; }
            public Dictionary<string, CompletionAnalytics> CompletionAnalytics { get; set; }
            public SystemHealth SystemHealth { get; set; }
            public List<Alert> ActiveAlerts { get; set; }
            public Dictionary<string, object> SummaryMetrics { get; set; }

            public DashboardData()
            {
                AgentStatuses = new Dictionary<string, AgentStatus>();
                PerformanceMetrics = new Dictionary<string, PerformanceMetrics>();
                CompletionAnalytics = new Dictionary<string, CompletionAnalytics>();
                SystemHealth = new SystemHealth();
                ActiveAlerts = new List<Alert>();
                SummaryMetrics = new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// System health information
        /// </summary>
        public class SystemHealth
        {
            public double OverallHealth { get; set; }
            public int TotalAgents { get; set; }
            public int ActiveAgents { get; set; }
            public int FailedAgents { get; set; }
            public double SystemCpuUsage { get; set; }
            public double SystemMemoryUsage { get; set; }
            public double SystemNetworkUsage { get; set; }
            public List<string> CriticalIssues { get; set; }
            public List<string> Warnings { get; set; }

            public SystemHealth()
            {
                CriticalIssues = new List<string>();
                Warnings = new List<string>();
            }
        }

        /// <summary>
        /// Alert information
        /// </summary>
        public class Alert
        {
            public string Id { get; set; }
            public string AgentId { get; set; }
            public AlertSeverity Severity { get; set; }
            public string Message { get; set; }
            public DateTime Timestamp { get; set; }
            public bool IsAcknowledged { get; set; }
            public DateTime? AcknowledgedAt { get; set; }
            public string AcknowledgedBy { get; set; }
        }

        /// <summary>
        /// Alert severity levels
        /// </summary>
        public enum AlertSeverity
        {
            Info,
            Warning,
            Error,
            Critical
        }

        /// <summary>
        /// Register agent for monitoring
        /// </summary>
        public async Task RegisterAgentAsync(string agentId, string language, string framework, string platform, CancellationToken cancellationToken = default)
        {
            try
            {
                var status = new AgentStatus
                {
                    AgentId = agentId,
                    Language = language,
                    Framework = framework,
                    Platform = platform,
                    State = AgentState.Starting,
                    StartTime = DateTime.UtcNow,
                    LastHeartbeat = DateTime.UtcNow
                };

                _agentStatuses.AddOrUpdate(agentId, status, (key, oldValue) => status);

                // Initialize performance metrics
                var metrics = new PerformanceMetrics
                {
                    AgentId = agentId,
                    Timestamp = DateTime.UtcNow
                };
                _performanceMetrics.AddOrUpdate(agentId, metrics, (key, oldValue) => metrics);

                // Initialize completion analytics
                var analytics = new CompletionAnalytics
                {
                    AgentId = agentId,
                    Date = DateTime.UtcNow.Date
                };
                _completionAnalytics.AddOrUpdate(agentId, analytics, (key, oldValue) => analytics);

                _logger.LogInformation("Agent {AgentId} registered for monitoring", agentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering agent {AgentId}", agentId);
                throw;
            }
        }

        /// <summary>
        /// Update agent status
        /// </summary>
        public async Task UpdateAgentStatusAsync(string agentId, AgentState state, Dictionary<string, object> metrics = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_agentStatuses.TryGetValue(agentId, out var status))
                {
                    status.State = state;
                    status.LastHeartbeat = DateTime.UtcNow;

                    if (metrics != null)
                    {
                        foreach (var metric in metrics)
                        {
                            status.CustomMetrics[metric.Key] = metric.Value;
                        }
                    }

                    // Update performance metrics
                    await UpdatePerformanceMetricsAsync(agentId, metrics, cancellationToken);

                    _logger.LogDebug("Updated status for agent {AgentId}: {State}", agentId, state);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for agent {AgentId}", agentId);
            }
        }

        /// <summary>
        /// Record task completion
        /// </summary>
        public async Task RecordTaskCompletionAsync(string agentId, string taskId, bool success, TimeSpan duration, string errorMessage = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_agentStatuses.TryGetValue(agentId, out var status))
                {
                    if (success)
                    {
                        status.TasksCompleted++;
                    }
                    else
                    {
                        status.TasksFailed++;
                        if (!string.IsNullOrEmpty(errorMessage))
                        {
                            status.RecentErrors.Add($"{DateTime.UtcNow:HH:mm:ss} - {errorMessage}");
                            if (status.RecentErrors.Count > 10)
                            {
                                status.RecentErrors.RemoveAt(0);
                            }
                        }
                    }
                }

                // Update completion analytics
                await UpdateCompletionAnalyticsAsync(agentId, success, duration, cancellationToken);

                _logger.LogDebug("Recorded task completion for agent {AgentId}: {TaskId} - {Success}", agentId, taskId, success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording task completion for agent {AgentId}", agentId);
            }
        }

        /// <summary>
        /// Get real-time dashboard data
        /// </summary>
        public async Task<DashboardData> GetDashboardDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var dashboardData = new DashboardData();

                // Collect agent statuses
                foreach (var kvp in _agentStatuses)
                {
                    dashboardData.AgentStatuses[kvp.Key] = kvp.Value;
                }

                // Collect performance metrics
                foreach (var kvp in _performanceMetrics)
                {
                    dashboardData.PerformanceMetrics[kvp.Key] = kvp.Value;
                }

                // Collect completion analytics
                foreach (var kvp in _completionAnalytics)
                {
                    dashboardData.CompletionAnalytics[kvp.Key] = kvp.Value;
                }

                // Calculate system health
                dashboardData.SystemHealth = await CalculateSystemHealthAsync(cancellationToken);

                // Get active alerts
                dashboardData.ActiveAlerts = await GetActiveAlertsAsync(cancellationToken);

                // Calculate summary metrics
                dashboardData.SummaryMetrics = await CalculateSummaryMetricsAsync(cancellationToken);

                return dashboardData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard data");
                return new DashboardData();
            }
        }

        /// <summary>
        /// Get agent performance report
        /// </summary>
        public async Task<Dictionary<string, object>> GetAgentPerformanceReportAsync(string agentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                var report = new Dictionary<string, object>();

                // Get agent status history
                var statusHistory = await GetAgentStatusHistoryAsync(agentId, startDate, endDate, cancellationToken);
                report["status_history"] = statusHistory;

                // Get performance metrics
                var performanceData = await GetPerformanceDataAsync(agentId, startDate, endDate, cancellationToken);
                report["performance_data"] = performanceData;

                // Get completion analytics
                var completionData = await GetCompletionDataAsync(agentId, startDate, endDate, cancellationToken);
                report["completion_data"] = completionData;

                // Calculate trends
                var trends = await CalculateTrendsAsync(agentId, startDate, endDate, cancellationToken);
                report["trends"] = trends;

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating performance report for agent {AgentId}", agentId);
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Create alert
        /// </summary>
        public async Task CreateAlertAsync(string agentId, AlertSeverity severity, string message, CancellationToken cancellationToken = default)
        {
            try
            {
                var alert = new Alert
                {
                    Id = Guid.NewGuid().ToString(),
                    AgentId = agentId,
                    Severity = severity,
                    Message = message,
                    Timestamp = DateTime.UtcNow,
                    IsAcknowledged = false
                };

                // Store alert (in a real implementation, this would be persisted)
                await StoreAlertAsync(alert, cancellationToken);

                _logger.LogWarning("Created {Severity} alert for agent {AgentId}: {Message}", severity, agentId, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating alert for agent {AgentId}", agentId);
            }
        }

        /// <summary>
        /// Acknowledge alert
        /// </summary>
        public async Task AcknowledgeAlertAsync(string alertId, string acknowledgedBy, CancellationToken cancellationToken = default)
        {
            try
            {
                // Update alert acknowledgment (in a real implementation, this would update the stored alert)
                await UpdateAlertAcknowledgmentAsync(alertId, acknowledgedBy, cancellationToken);

                _logger.LogInformation("Alert {AlertId} acknowledged by {User}", alertId, acknowledgedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acknowledging alert {AlertId}", alertId);
            }
        }

        /// <summary>
        /// Collect agent status periodically
        /// </summary>
        private async void CollectAgentStatus(object state)
        {
            try
            {
                foreach (var agentId in _agentStatuses.Keys.ToList())
                {
                    await CollectAgentMetricsAsync(agentId, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting agent status");
            }
        }

        /// <summary>
        /// Generate reports periodically
        /// </summary>
        private async void GenerateReports(object state)
        {
            try
            {
                var dashboardData = await GetDashboardDataAsync(CancellationToken.None);
                await StoreDashboardDataAsync(dashboardData, CancellationToken.None);

                // Generate automated reports
                await GenerateAutomatedReportsAsync(CancellationToken.None);

                _logger.LogInformation("Generated monitoring reports");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating reports");
            }
        }

        /// <summary>
        /// Cleanup old data periodically
        /// </summary>
        private async void CleanupOldData(object state)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-30);
                
                // Cleanup old performance metrics
                var oldMetrics = _performanceMetrics.Where(kvp => kvp.Value.Timestamp < cutoffDate).ToList();
                foreach (var kvp in oldMetrics)
                {
                    _performanceMetrics.TryRemove(kvp.Key, out _);
                }

                // Cleanup old completion analytics
                var oldAnalytics = _completionAnalytics.Where(kvp => kvp.Value.Date < cutoffDate.Date).ToList();
                foreach (var kvp in oldAnalytics)
                {
                    _completionAnalytics.TryRemove(kvp.Key, out _);
                }

                _logger.LogInformation("Cleaned up {MetricsCount} old metrics and {AnalyticsCount} old analytics", 
                    oldMetrics.Count, oldAnalytics.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old data");
            }
        }

        /// <summary>
        /// Update performance metrics for agent
        /// </summary>
        private async Task UpdatePerformanceMetricsAsync(string agentId, Dictionary<string, object> metrics, CancellationToken cancellationToken)
        {
            if (_performanceMetrics.TryGetValue(agentId, out var performanceMetrics))
            {
                performanceMetrics.Timestamp = DateTime.UtcNow;

                if (metrics != null)
                {
                    if (metrics.TryGetValue("response_time", out var responseTime))
                    {
                        performanceMetrics.AverageResponseTime = Convert.ToDouble(responseTime);
                        performanceMetrics.ResponseTimeHistory.Add(performanceMetrics.AverageResponseTime);
                        if (performanceMetrics.ResponseTimeHistory.Count > 100)
                        {
                            performanceMetrics.ResponseTimeHistory.RemoveAt(0);
                        }
                    }

                    if (metrics.TryGetValue("throughput", out var throughput))
                    {
                        performanceMetrics.Throughput = Convert.ToDouble(throughput);
                        performanceMetrics.ThroughputHistory.Add(performanceMetrics.Throughput);
                        if (performanceMetrics.ThroughputHistory.Count > 100)
                        {
                            performanceMetrics.ThroughputHistory.RemoveAt(0);
                        }
                    }

                    if (metrics.TryGetValue("error_rate", out var errorRate))
                    {
                        performanceMetrics.ErrorRate = Convert.ToDouble(errorRate);
                    }

                    foreach (var metric in metrics)
                    {
                        if (metric.Value is double doubleValue)
                        {
                            performanceMetrics.CustomMetrics[metric.Key] = doubleValue;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update completion analytics for agent
        /// </summary>
        private async Task UpdateCompletionAnalyticsAsync(string agentId, bool success, TimeSpan duration, CancellationToken cancellationToken)
        {
            if (_completionAnalytics.TryGetValue(agentId, out var analytics))
            {
                analytics.TotalTasks++;
                if (success)
                {
                    analytics.CompletedTasks++;
                }
                else
                {
                    analytics.FailedTasks++;
                }

                analytics.CompletionRate = (double)analytics.CompletedTasks / analytics.TotalTasks;
                analytics.SuccessRate = (double)analytics.CompletedTasks / (analytics.CompletedTasks + analytics.FailedTasks);

                // Update average task duration
                var totalDuration = analytics.AverageTaskDuration.TotalMilliseconds * (analytics.TotalTasks - 1) + duration.TotalMilliseconds;
                analytics.AverageTaskDuration = TimeSpan.FromMilliseconds(totalDuration / analytics.TotalTasks);

                analytics.TotalExecutionTime += duration;
            }
        }

        /// <summary>
        /// Calculate system health
        /// </summary>
        private async Task<SystemHealth> CalculateSystemHealthAsync(CancellationToken cancellationToken)
        {
            var health = new SystemHealth();

            try
            {
                health.TotalAgents = _agentStatuses.Count;
                health.ActiveAgents = _agentStatuses.Values.Count(s => s.State == AgentState.Running || s.State == AgentState.Busy);
                health.FailedAgents = _agentStatuses.Values.Count(s => s.State == AgentState.Error || s.State == AgentState.Offline);

                // Calculate overall health percentage
                if (health.TotalAgents > 0)
                {
                    health.OverallHealth = (double)health.ActiveAgents / health.TotalAgents * 100;
                }

                // Calculate system resource usage
                health.SystemCpuUsage = await GetSystemCpuUsageAsync(cancellationToken);
                health.SystemMemoryUsage = await GetSystemMemoryUsageAsync(cancellationToken);
                health.SystemNetworkUsage = await GetSystemNetworkUsageAsync(cancellationToken);

                // Identify critical issues and warnings
                foreach (var status in _agentStatuses.Values)
                {
                    if (status.State == AgentState.Error)
                    {
                        health.CriticalIssues.Add($"Agent {status.AgentId} is in error state");
                    }
                    else if (status.State == AgentState.Offline)
                    {
                        health.Warnings.Add($"Agent {status.AgentId} is offline");
                    }
                }

                return health;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating system health");
                return health;
            }
        }

        /// <summary>
        /// Get active alerts
        /// </summary>
        private async Task<List<Alert>> GetActiveAlertsAsync(CancellationToken cancellationToken)
        {
            // In a real implementation, this would retrieve alerts from storage
            return await Task.FromResult(new List<Alert>());
        }

        /// <summary>
        /// Calculate summary metrics
        /// </summary>
        private async Task<Dictionary<string, object>> CalculateSummaryMetricsAsync(CancellationToken cancellationToken)
        {
            var summary = new Dictionary<string, object>();

            try
            {
                summary["total_agents"] = _agentStatuses.Count;
                summary["active_agents"] = _agentStatuses.Values.Count(s => s.State == AgentState.Running || s.State == AgentState.Busy);
                summary["total_tasks_completed"] = _agentStatuses.Values.Sum(s => s.TasksCompleted);
                summary["total_tasks_failed"] = _agentStatuses.Values.Sum(s => s.TasksFailed);
                summary["average_completion_rate"] = _completionAnalytics.Values.Average(a => a.CompletionRate);
                summary["average_success_rate"] = _completionAnalytics.Values.Average(a => a.SuccessRate);

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating summary metrics");
                return summary;
            }
        }

        // Helper methods for data collection and storage
        private async Task CollectAgentMetricsAsync(string agentId, CancellationToken cancellationToken)
        {
            // In a real implementation, this would collect metrics from the actual agent
            await Task.Delay(1, cancellationToken);
        }

        private async Task<List<object>> GetAgentStatusHistoryAsync(string agentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
        {
            // In a real implementation, this would retrieve status history from storage
            return await Task.FromResult(new List<object>());
        }

        private async Task<Dictionary<string, object>> GetPerformanceDataAsync(string agentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
        {
            // In a real implementation, this would retrieve performance data from storage
            return await Task.FromResult(new Dictionary<string, object>());
        }

        private async Task<Dictionary<string, object>> GetCompletionDataAsync(string agentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
        {
            // In a real implementation, this would retrieve completion data from storage
            return await Task.FromResult(new Dictionary<string, object>());
        }

        private async Task<Dictionary<string, object>> CalculateTrendsAsync(string agentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
        {
            // In a real implementation, this would calculate trends from historical data
            return await Task.FromResult(new Dictionary<string, object>());
        }

        private async Task StoreAlertAsync(Alert alert, CancellationToken cancellationToken)
        {
            // In a real implementation, this would store the alert in a database
            await Task.CompletedTask;
        }

        private async Task UpdateAlertAcknowledgmentAsync(string alertId, string acknowledgedBy, CancellationToken cancellationToken)
        {
            // In a real implementation, this would update the alert in storage
            await Task.CompletedTask;
        }

        private async Task StoreDashboardDataAsync(DashboardData dashboardData, CancellationToken cancellationToken)
        {
            // In a real implementation, this would store dashboard data for historical analysis
            await Task.CompletedTask;
        }

        private async Task GenerateAutomatedReportsAsync(CancellationToken cancellationToken)
        {
            // In a real implementation, this would generate and send automated reports
            await Task.CompletedTask;
        }

        private async Task<double> GetSystemCpuUsageAsync(CancellationToken cancellationToken)
        {
            // In a real implementation, this would get actual CPU usage
            await Task.Delay(1, cancellationToken);
            return Random.Shared.NextDouble() * 100;
        }

        private async Task<double> GetSystemMemoryUsageAsync(CancellationToken cancellationToken)
        {
            // In a real implementation, this would get actual memory usage
            await Task.Delay(1, cancellationToken);
            return Random.Shared.NextDouble() * 100;
        }

        private async Task<double> GetSystemNetworkUsageAsync(CancellationToken cancellationToken)
        {
            // In a real implementation, this would get actual network usage
            await Task.Delay(1, cancellationToken);
            return Random.Shared.NextDouble() * 100;
        }

        /// <summary>
        /// Get monitoring statistics
        /// </summary>
        public async Task<Dictionary<string, object>> GetMonitoringStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var stats = new Dictionary<string, object>
            {
                ["total_agents_monitored"] = _agentStatuses.Count,
                ["active_alerts"] = await GetActiveAlertsCountAsync(cancellationToken),
                ["average_response_time"] = await CalculateAverageResponseTimeAsync(cancellationToken),
                ["system_uptime"] = await GetSystemUptimeAsync(cancellationToken),
                ["data_retention_days"] = 30
            };

            return stats;
        }

        // Additional helper methods
        private async Task<int> GetActiveAlertsCountAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            return Random.Shared.Next(0, 10);
        }

        private async Task<double> CalculateAverageResponseTimeAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            return Random.Shared.NextDouble() * 1000 + 50;
        }

        private async Task<TimeSpan> GetSystemUptimeAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            return TimeSpan.FromDays(Random.Shared.Next(1, 30));
        }

        public void Dispose()
        {
            _monitoringTimer?.Dispose();
            _reportingTimer?.Dispose();
            _cleanupTimer?.Dispose();
        }
    }
} 