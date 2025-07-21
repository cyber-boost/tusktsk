using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Linq;
using System.Threading;
using System.Diagnostics;

namespace TuskLang
{
    /// <summary>
    /// Advanced monitoring and alerting system for TuskLang C# SDK
    /// Provides real-time monitoring, metrics collection, alerting, and health checks
    /// </summary>
    public class MonitoringAlerting
    {
        private readonly Dictionary<string, IMonitor> _monitors;
        private readonly List<IAlertChannel> _alertChannels;
        private readonly List<IMetricsCollector> _metricsCollectors;
        private readonly MonitoringMetrics _metrics;
        private readonly AlertManager _alertManager;
        private readonly HealthChecker _healthChecker;
        private readonly object _lock = new object();

        public MonitoringAlerting()
        {
            _monitors = new Dictionary<string, IMonitor>();
            _alertChannels = new List<IAlertChannel>();
            _metricsCollectors = new List<IMetricsCollector>();
            _metrics = new MonitoringMetrics();
            _alertManager = new AlertManager();
            _healthChecker = new HealthChecker();

            // Register default alert channels
            RegisterAlertChannel(new EmailAlertChannel());
            RegisterAlertChannel(new SlackAlertChannel());
            RegisterAlertChannel(new WebhookAlertChannel());
            
            // Register default metrics collectors
            RegisterMetricsCollector(new SystemMetricsCollector());
            RegisterMetricsCollector(new ApplicationMetricsCollector());
            RegisterMetricsCollector(new BusinessMetricsCollector());
        }

        /// <summary>
        /// Register a monitor
        /// </summary>
        public void RegisterMonitor(string monitorName, IMonitor monitor)
        {
            lock (_lock)
            {
                _monitors[monitorName] = monitor;
            }
        }

        /// <summary>
        /// Start monitoring
        /// </summary>
        public async Task<MonitoringResult> StartMonitoringAsync(MonitoringConfig config = null)
        {
            var startTime = DateTime.UtcNow;
            var monitoringTasks = new List<Task<MonitorResult>>();

            try
            {
                foreach (var monitor in _monitors)
                {
                    var task = monitor.Value.StartMonitoringAsync(config ?? new MonitoringConfig());
                    monitoringTasks.Add(task);
                }

                var results = await Task.WhenAll(monitoringTasks);
                var success = results.All(r => r.Success);

                _metrics.RecordMonitoringSession(success, DateTime.UtcNow - startTime);

                return new MonitoringResult
                {
                    Success = success,
                    MonitorResults = results.ToList(),
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                _metrics.RecordMonitoringSession(false, DateTime.UtcNow - startTime);
                return new MonitoringResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// Send an alert
        /// </summary>
        public async Task<AlertResult> SendAlertAsync(Alert alert)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var result = await _alertManager.SendAlertAsync(alert, _alertChannels);
                
                _metrics.RecordAlertSent(alert.Severity, result.Success);
                
                return result;
            }
            catch (Exception ex)
            {
                _metrics.RecordAlertSent(alert.Severity, false);
                return new AlertResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// Collect metrics
        /// </summary>
        public async Task<MetricsCollectionResult> CollectMetricsAsync()
        {
            var startTime = DateTime.UtcNow;
            var collectionTasks = new List<Task<MetricsData>>();

            try
            {
                foreach (var collector in _metricsCollectors)
                {
                    var task = collector.CollectMetricsAsync();
                    collectionTasks.Add(task);
                }

                var metricsData = await Task.WhenAll(collectionTasks);

                return new MetricsCollectionResult
                {
                    Success = true,
                    MetricsData = metricsData.ToList(),
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new MetricsCollectionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// Perform health check
        /// </summary>
        public async Task<HealthCheckResult> PerformHealthCheckAsync(string serviceName = null)
        {
            return await _healthChecker.CheckHealthAsync(serviceName);
        }

        /// <summary>
        /// Register an alert channel
        /// </summary>
        public void RegisterAlertChannel(IAlertChannel channel)
        {
            lock (_lock)
            {
                _alertChannels.Add(channel);
            }
        }

        /// <summary>
        /// Register a metrics collector
        /// </summary>
        public void RegisterMetricsCollector(IMetricsCollector collector)
        {
            lock (_lock)
            {
                _metricsCollectors.Add(collector);
            }
        }

        /// <summary>
        /// Get monitoring metrics
        /// </summary>
        public MonitoringMetrics GetMetrics()
        {
            return _metrics;
        }

        /// <summary>
        /// Get all monitor names
        /// </summary>
        public List<string> GetMonitorNames()
        {
            lock (_lock)
            {
                return _monitors.Keys.ToList();
            }
        }
    }

    /// <summary>
    /// Monitor interface
    /// </summary>
    public interface IMonitor
    {
        string Name { get; }
        Task<MonitorResult> StartMonitoringAsync(MonitoringConfig config);
    }

    /// <summary>
    /// Alert channel interface
    /// </summary>
    public interface IAlertChannel
    {
        string Name { get; }
        Task<bool> SendAlertAsync(Alert alert);
    }

    /// <summary>
    /// Metrics collector interface
    /// </summary>
    public interface IMetricsCollector
    {
        string Name { get; }
        Task<MetricsData> CollectMetricsAsync();
    }

    /// <summary>
    /// System monitor
    /// </summary>
    public class SystemMonitor : IMonitor
    {
        public string Name => "System Monitor";

        public async Task<MonitorResult> StartMonitoringAsync(MonitoringConfig config)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Monitor system resources
                var cpuUsage = GetCpuUsage();
                var memoryUsage = GetMemoryUsage();
                var diskUsage = GetDiskUsage();

                var isHealthy = cpuUsage < config.CpuThreshold && 
                               memoryUsage < config.MemoryThreshold && 
                               diskUsage < config.DiskThreshold;

                if (!isHealthy)
                {
                    // Send alert for unhealthy system
                    var alert = new Alert
                    {
                        Title = "System Health Alert",
                        Message = $"System resources exceeded thresholds: CPU={cpuUsage}%, Memory={memoryUsage}%, Disk={diskUsage}%",
                        Severity = AlertSeverity.Warning,
                        Timestamp = DateTime.UtcNow
                    };

                    // In a real implementation, this would send the alert
                }

                return new MonitorResult
                {
                    MonitorName = Name,
                    Success = true,
                    IsHealthy = isHealthy,
                    Metrics = new Dictionary<string, object>
                    {
                        ["cpu_usage"] = cpuUsage,
                        ["memory_usage"] = memoryUsage,
                        ["disk_usage"] = diskUsage
                    },
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new MonitorResult
                {
                    MonitorName = Name,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        private double GetCpuUsage()
        {
            // In a real implementation, this would get actual CPU usage
            return new Random().Next(10, 90);
        }

        private double GetMemoryUsage()
        {
            // In a real implementation, this would get actual memory usage
            return new Random().Next(20, 80);
        }

        private double GetDiskUsage()
        {
            // In a real implementation, this would get actual disk usage
            return new Random().Next(30, 70);
        }
    }

    /// <summary>
    /// Application monitor
    /// </summary>
    public class ApplicationMonitor : IMonitor
    {
        public string Name => "Application Monitor";

        public async Task<MonitorResult> StartMonitoringAsync(MonitoringConfig config)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Monitor application health
                var responseTime = GetResponseTime();
                var errorRate = GetErrorRate();
                var throughput = GetThroughput();

                var isHealthy = responseTime < config.ResponseTimeThreshold && 
                               errorRate < config.ErrorRateThreshold;

                return new MonitorResult
                {
                    MonitorName = Name,
                    Success = true,
                    IsHealthy = isHealthy,
                    Metrics = new Dictionary<string, object>
                    {
                        ["response_time"] = responseTime,
                        ["error_rate"] = errorRate,
                        ["throughput"] = throughput
                    },
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new MonitorResult
                {
                    MonitorName = Name,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        private double GetResponseTime()
        {
            // In a real implementation, this would get actual response time
            return new Random().Next(50, 500);
        }

        private double GetErrorRate()
        {
            // In a real implementation, this would get actual error rate
            return new Random().Next(0, 5);
        }

        private int GetThroughput()
        {
            // In a real implementation, this would get actual throughput
            return new Random().Next(100, 1000);
        }
    }

    /// <summary>
    /// Email alert channel
    /// </summary>
    public class EmailAlertChannel : IAlertChannel
    {
        public string Name => "Email";

        public async Task<bool> SendAlertAsync(Alert alert)
        {
            // In a real implementation, this would send an email
            await Task.Delay(100);
            
            Console.WriteLine($"Email Alert: {alert.Title} - {alert.Message}");
            return true;
        }
    }

    /// <summary>
    /// Slack alert channel
    /// </summary>
    public class SlackAlertChannel : IAlertChannel
    {
        public string Name => "Slack";

        public async Task<bool> SendAlertAsync(Alert alert)
        {
            // In a real implementation, this would send a Slack message
            await Task.Delay(50);
            
            Console.WriteLine($"Slack Alert: {alert.Title} - {alert.Message}");
            return true;
        }
    }

    /// <summary>
    /// Webhook alert channel
    /// </summary>
    public class WebhookAlertChannel : IAlertChannel
    {
        public string Name => "Webhook";

        public async Task<bool> SendAlertAsync(Alert alert)
        {
            // In a real implementation, this would send a webhook
            await Task.Delay(75);
            
            Console.WriteLine($"Webhook Alert: {alert.Title} - {alert.Message}");
            return true;
        }
    }

    /// <summary>
    /// System metrics collector
    /// </summary>
    public class SystemMetricsCollector : IMetricsCollector
    {
        public string Name => "System Metrics";

        public async Task<MetricsData> CollectMetricsAsync()
        {
            await Task.Delay(50);

            return new MetricsData
            {
                CollectorName = Name,
                Timestamp = DateTime.UtcNow,
                Metrics = new Dictionary<string, object>
                {
                    ["cpu_usage"] = new Random().Next(10, 90),
                    ["memory_usage"] = new Random().Next(20, 80),
                    ["disk_usage"] = new Random().Next(30, 70),
                    ["network_io"] = new Random().Next(100, 1000)
                }
            };
        }
    }

    /// <summary>
    /// Application metrics collector
    /// </summary>
    public class ApplicationMetricsCollector : IMetricsCollector
    {
        public string Name => "Application Metrics";

        public async Task<MetricsData> CollectMetricsAsync()
        {
            await Task.Delay(30);

            return new MetricsData
            {
                CollectorName = Name,
                Timestamp = DateTime.UtcNow,
                Metrics = new Dictionary<string, object>
                {
                    ["request_count"] = new Random().Next(100, 1000),
                    ["response_time"] = new Random().Next(50, 500),
                    ["error_count"] = new Random().Next(0, 10),
                    ["active_connections"] = new Random().Next(10, 100)
                }
            };
        }
    }

    /// <summary>
    /// Business metrics collector
    /// </summary>
    public class BusinessMetricsCollector : IMetricsCollector
    {
        public string Name => "Business Metrics";

        public async Task<MetricsData> CollectMetricsAsync()
        {
            await Task.Delay(25);

            return new MetricsData
            {
                CollectorName = Name,
                Timestamp = DateTime.UtcNow,
                Metrics = new Dictionary<string, object>
                {
                    ["user_count"] = new Random().Next(1000, 10000),
                    ["transaction_count"] = new Random().Next(100, 1000),
                    ["revenue"] = new Random().Next(10000, 100000),
                    ["conversion_rate"] = new Random().Next(1, 10)
                }
            };
        }
    }

    /// <summary>
    /// Alert manager
    /// </summary>
    public class AlertManager
    {
        public async Task<AlertResult> SendAlertAsync(Alert alert, List<IAlertChannel> channels)
        {
            var startTime = DateTime.UtcNow;
            var results = new List<bool>();

            foreach (var channel in channels)
            {
                try
                {
                    var result = await channel.SendAlertAsync(alert);
                    results.Add(result);
                }
                catch
                {
                    results.Add(false);
                }
            }

            return new AlertResult
            {
                Success = results.Any(r => r),
                ChannelsUsed = channels.Count,
                SuccessfulChannels = results.Count(r => r),
                ExecutionTime = DateTime.UtcNow - startTime
            };
        }
    }

    /// <summary>
    /// Health checker
    /// </summary>
    public class HealthChecker
    {
        public async Task<HealthCheckResult> CheckHealthAsync(string serviceName)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // In a real implementation, this would check actual service health
                await Task.Delay(100);

                var isHealthy = new Random().Next(0, 10) > 2; // 80% healthy

                return new HealthCheckResult
                {
                    ServiceName = serviceName ?? "default",
                    IsHealthy = isHealthy,
                    ResponseTime = TimeSpan.FromMilliseconds(100),
                    Timestamp = DateTime.UtcNow,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new HealthCheckResult
                {
                    ServiceName = serviceName ?? "default",
                    IsHealthy = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }
    }

    // Data transfer objects
    public class MonitoringResult
    {
        public bool Success { get; set; }
        public List<MonitorResult> MonitorResults { get; set; } = new List<MonitorResult>();
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class MonitorResult
    {
        public string MonitorName { get; set; }
        public bool Success { get; set; }
        public bool IsHealthy { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class Alert
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public AlertSeverity Severity { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class AlertResult
    {
        public bool Success { get; set; }
        public int ChannelsUsed { get; set; }
        public int SuccessfulChannels { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class MetricsData
    {
        public string CollectorName { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
    }

    public class MetricsCollectionResult
    {
        public bool Success { get; set; }
        public List<MetricsData> MetricsData { get; set; } = new List<MetricsData>();
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class HealthCheckResult
    {
        public string ServiceName { get; set; }
        public bool IsHealthy { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public DateTime Timestamp { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    // Configuration classes
    public class MonitoringConfig
    {
        public double CpuThreshold { get; set; } = 80.0;
        public double MemoryThreshold { get; set; } = 85.0;
        public double DiskThreshold { get; set; } = 90.0;
        public TimeSpan ResponseTimeThreshold { get; set; } = TimeSpan.FromSeconds(1);
        public double ErrorRateThreshold { get; set; } = 5.0;
        public TimeSpan MonitoringInterval { get; set; } = TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Monitoring metrics collection
    /// </summary>
    public class MonitoringMetrics
    {
        private readonly Dictionary<AlertSeverity, int> _alertsSent = new Dictionary<AlertSeverity, int>();
        private readonly Dictionary<string, int> _monitoringSessions = new Dictionary<string, int>();
        private readonly object _lock = new object();

        public void RecordAlertSent(AlertSeverity severity, bool success)
        {
            lock (_lock)
            {
                if (success)
                {
                    _alertsSent[severity] = _alertsSent.GetValueOrDefault(severity, 0) + 1;
                }
            }
        }

        public void RecordMonitoringSession(bool success, TimeSpan duration)
        {
            lock (_lock)
            {
                var key = success ? "successful" : "failed";
                _monitoringSessions[key] = _monitoringSessions.GetValueOrDefault(key, 0) + 1;
            }
        }

        public Dictionary<AlertSeverity, int> GetAlertsSent() => new Dictionary<AlertSeverity, int>(_alertsSent);
        public Dictionary<string, int> GetMonitoringSessions() => new Dictionary<string, int>(_monitoringSessions);
    }

    public enum AlertSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
} 