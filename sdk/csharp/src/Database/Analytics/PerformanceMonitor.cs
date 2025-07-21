using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using System.IO;

namespace TuskLang.Database
{
    /// <summary>
    /// Real-time database performance monitor for TuskTsk
    /// Provides comprehensive performance analytics and optimization
    /// </summary>
    public class PerformanceMonitor : IDisposable
    {
        private readonly ILogger<PerformanceMonitor> _logger;
        private readonly ConcurrentDictionary<string, QueryMetrics> _queryMetrics;
        private readonly ConcurrentDictionary<string, ConnectionMetrics> _connectionMetrics;
        private readonly Timer _metricsTimer;
        private readonly Timer _cleanupTimer;
        private readonly string _metricsFilePath;
        private bool _disposed = false;

        public PerformanceMonitor(ILogger<PerformanceMonitor> logger = null, string metricsFilePath = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PerformanceMonitor>.Instance;
            _queryMetrics = new ConcurrentDictionary<string, QueryMetrics>();
            _connectionMetrics = new ConcurrentDictionary<string, ConnectionMetrics>();
            _metricsFilePath = metricsFilePath ?? "database_metrics.json";
            
            // Start metrics collection timer (every 5 seconds)
            _metricsTimer = new Timer(CollectMetrics, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            
            // Start cleanup timer (every hour)
            _cleanupTimer = new Timer(CleanupOldMetrics, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
            
            _logger.LogInformation("Database performance monitor started");
        }

        #region Query Performance Tracking

        /// <summary>
        /// Track query execution
        /// </summary>
        public void TrackQuery(string queryHash, string queryText, TimeSpan duration, bool success, int rowsAffected = 0, string errorMessage = null)
        {
            try
            {
                var metrics = _queryMetrics.GetOrAdd(queryHash, _ => new QueryMetrics
                {
                    QueryHash = queryHash,
                    QueryText = queryText,
                    FirstSeen = DateTime.UtcNow
                });

                lock (metrics)
                {
                    metrics.ExecutionCount++;
                    metrics.TotalDuration += duration;
                    metrics.AverageDuration = TimeSpan.FromTicks(metrics.TotalDuration.Ticks / metrics.ExecutionCount);
                    metrics.MaxDuration = duration > metrics.MaxDuration ? duration : metrics.MaxDuration;
                    metrics.MinDuration = duration < metrics.MinDuration || metrics.MinDuration == TimeSpan.Zero ? duration : metrics.MinDuration;
                    metrics.TotalRowsAffected += rowsAffected;
                    metrics.LastExecuted = DateTime.UtcNow;
                    metrics.SuccessCount += success ? 1 : 0;
                    metrics.ErrorCount += success ? 0 : 1;
                    
                    if (!success && !string.IsNullOrEmpty(errorMessage))
                    {
                        metrics.LastError = errorMessage;
                    }
                }

                _logger.LogDebug($"Tracked query: {queryHash}, duration: {duration.TotalMilliseconds}ms, success: {success}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking query metrics");
            }
        }

        /// <summary>
        /// Get query performance metrics
        /// </summary>
        public QueryMetrics GetQueryMetrics(string queryHash)
        {
            return _queryMetrics.TryGetValue(queryHash, out var metrics) ? metrics : null;
        }

        /// <summary>
        /// Get all query metrics
        /// </summary>
        public List<QueryMetrics> GetAllQueryMetrics()
        {
            return _queryMetrics.Values.ToList();
        }

        /// <summary>
        /// Get slow queries (above threshold)
        /// </summary>
        public List<QueryMetrics> GetSlowQueries(TimeSpan threshold)
        {
            return _queryMetrics.Values
                .Where(q => q.AverageDuration > threshold)
                .OrderByDescending(q => q.AverageDuration)
                .ToList();
        }

        /// <summary>
        /// Get most executed queries
        /// </summary>
        public List<QueryMetrics> GetMostExecutedQueries(int limit = 10)
        {
            return _queryMetrics.Values
                .OrderByDescending(q => q.ExecutionCount)
                .Take(limit)
                .ToList();
        }

        #endregion

        #region Connection Performance Tracking

        /// <summary>
        /// Track connection metrics
        /// </summary>
        public void TrackConnection(string connectionId, TimeSpan duration, bool success, string errorMessage = null)
        {
            try
            {
                var metrics = _connectionMetrics.GetOrAdd(connectionId, _ => new ConnectionMetrics
                {
                    ConnectionId = connectionId,
                    FirstSeen = DateTime.UtcNow
                });

                lock (metrics)
                {
                    metrics.ConnectionCount++;
                    metrics.TotalDuration += duration;
                    metrics.AverageDuration = TimeSpan.FromTicks(metrics.TotalDuration.Ticks / metrics.ConnectionCount);
                    metrics.MaxDuration = duration > metrics.MaxDuration ? duration : metrics.MaxDuration;
                    metrics.MinDuration = duration < metrics.MinDuration || metrics.MinDuration == TimeSpan.Zero ? duration : metrics.MinDuration;
                    metrics.LastConnected = DateTime.UtcNow;
                    metrics.SuccessCount += success ? 1 : 0;
                    metrics.ErrorCount += success ? 0 : 1;
                    
                    if (!success && !string.IsNullOrEmpty(errorMessage))
                    {
                        metrics.LastError = errorMessage;
                    }
                }

                _logger.LogDebug($"Tracked connection: {connectionId}, duration: {duration.TotalMilliseconds}ms, success: {success}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking connection metrics");
            }
        }

        /// <summary>
        /// Get connection metrics
        /// </summary>
        public ConnectionMetrics GetConnectionMetrics(string connectionId)
        {
            return _connectionMetrics.TryGetValue(connectionId, out var metrics) ? metrics : null;
        }

        /// <summary>
        /// Get all connection metrics
        /// </summary>
        public List<ConnectionMetrics> GetAllConnectionMetrics()
        {
            return _connectionMetrics.Values.ToList();
        }

        #endregion

        #region Performance Analysis

        /// <summary>
        /// Get performance summary
        /// </summary>
        public PerformanceSummary GetPerformanceSummary()
        {
            var queryMetrics = _queryMetrics.Values.ToList();
            var connectionMetrics = _connectionMetrics.Values.ToList();

            return new PerformanceSummary
            {
                TotalQueries = queryMetrics.Sum(q => q.ExecutionCount),
                TotalConnections = connectionMetrics.Sum(c => c.ConnectionCount),
                AverageQueryTime = queryMetrics.Any() ? TimeSpan.FromTicks((long)queryMetrics.Average(q => q.AverageDuration.Ticks)) : TimeSpan.Zero,
                AverageConnectionTime = connectionMetrics.Any() ? TimeSpan.FromTicks((long)connectionMetrics.Average(c => c.AverageDuration.Ticks)) : TimeSpan.Zero,
                SlowQueries = queryMetrics.Count(q => q.AverageDuration > TimeSpan.FromMilliseconds(100)),
                FailedQueries = queryMetrics.Sum(q => q.ErrorCount),
                FailedConnections = connectionMetrics.Sum(c => c.ErrorCount),
                QuerySuccessRate = queryMetrics.Any() ? (double)queryMetrics.Sum(q => q.SuccessCount) / queryMetrics.Sum(q => q.ExecutionCount) : 0,
                ConnectionSuccessRate = connectionMetrics.Any() ? (double)connectionMetrics.Sum(c => c.SuccessCount) / connectionMetrics.Sum(c => c.ConnectionCount) : 0,
                GeneratedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Get performance recommendations
        /// </summary>
        public List<PerformanceRecommendation> GetPerformanceRecommendations()
        {
            var recommendations = new List<PerformanceRecommendation>();
            var queryMetrics = _queryMetrics.Values.ToList();

            // Slow query recommendations
            var slowQueries = queryMetrics.Where(q => q.AverageDuration > TimeSpan.FromMilliseconds(500)).ToList();
            foreach (var query in slowQueries)
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Type = RecommendationType.SlowQuery,
                    Severity = query.AverageDuration > TimeSpan.FromSeconds(1) ? Severity.High : Severity.Medium,
                    Message = $"Query '{query.QueryHash}' is slow (avg: {query.AverageDuration.TotalMilliseconds}ms). Consider adding indexes or optimizing the query.",
                    QueryHash = query.QueryHash,
                    Impact = $"Affects {query.ExecutionCount} executions"
                });
            }

            // High error rate recommendations
            var highErrorQueries = queryMetrics.Where(q => q.ErrorCount > 0 && (double)q.ErrorCount / q.ExecutionCount > 0.1).ToList();
            foreach (var query in highErrorQueries)
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Type = RecommendationType.HighErrorRate,
                    Severity = Severity.High,
                    Message = $"Query '{query.QueryHash}' has high error rate ({(double)query.ErrorCount / query.ExecutionCount:P1}). Check query logic and error handling.",
                    QueryHash = query.QueryHash,
                    Impact = $"{query.ErrorCount} errors out of {query.ExecutionCount} executions"
                });
            }

            // Frequently executed queries
            var frequentQueries = queryMetrics.Where(q => q.ExecutionCount > 100).OrderByDescending(q => q.ExecutionCount).Take(5).ToList();
            foreach (var query in frequentQueries)
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Type = RecommendationType.FrequentQuery,
                    Severity = Severity.Low,
                    Message = $"Query '{query.QueryHash}' is executed frequently ({query.ExecutionCount} times). Consider caching or query optimization.",
                    QueryHash = query.QueryHash,
                    Impact = $"Executed {query.ExecutionCount} times"
                });
            }

            return recommendations.OrderByDescending(r => r.Severity).ToList();
        }

        #endregion

        #region Metrics Collection

        private void CollectMetrics(object state)
        {
            try
            {
                var summary = GetPerformanceSummary();
                var recommendations = GetPerformanceRecommendations();

                // Log performance summary
                _logger.LogInformation($"Performance Summary - Queries: {summary.TotalQueries}, " +
                    $"Avg Query Time: {summary.AverageQueryTime.TotalMilliseconds:F2}ms, " +
                    $"Success Rate: {summary.QuerySuccessRate:P1}");

                // Log high severity recommendations
                var highSeverityRecommendations = recommendations.Where(r => r.Severity == Severity.High).ToList();
                foreach (var recommendation in highSeverityRecommendations)
                {
                    _logger.LogWarning($"Performance Issue: {recommendation.Message}");
                }

                // Save metrics to file
                SaveMetricsToFile();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting metrics");
            }
        }

        private void CleanupOldMetrics(object state)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddDays(-7);
                var removedQueries = 0;
                var removedConnections = 0;

                // Remove old query metrics
                var oldQueryKeys = _queryMetrics.Where(kvp => kvp.Value.LastExecuted < cutoffTime)
                    .Select(kvp => kvp.Key).ToList();
                foreach (var key in oldQueryKeys)
                {
                    _queryMetrics.TryRemove(key, out _);
                    removedQueries++;
                }

                // Remove old connection metrics
                var oldConnectionKeys = _connectionMetrics.Where(kvp => kvp.Value.LastConnected < cutoffTime)
                    .Select(kvp => kvp.Key).ToList();
                foreach (var key in oldConnectionKeys)
                {
                    _connectionMetrics.TryRemove(key, out _);
                    removedConnections++;
                }

                if (removedQueries > 0 || removedConnections > 0)
                {
                    _logger.LogInformation($"Cleaned up old metrics: {removedQueries} queries, {removedConnections} connections");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old metrics");
            }
        }

        private void SaveMetricsToFile()
        {
            try
            {
                var metricsData = new
                {
                    GeneratedAt = DateTime.UtcNow,
                    Summary = GetPerformanceSummary(),
                    Recommendations = GetPerformanceRecommendations(),
                    QueryMetrics = GetAllQueryMetrics(),
                    ConnectionMetrics = GetAllConnectionMetrics()
                };

                var json = JsonSerializer.Serialize(metricsData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_metricsFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving metrics to file");
            }
        }

        #endregion

        #region Metrics Models

        public class QueryMetrics
        {
            public string QueryHash { get; set; }
            public string QueryText { get; set; }
            public DateTime FirstSeen { get; set; }
            public DateTime LastExecuted { get; set; }
            public long ExecutionCount { get; set; }
            public TimeSpan TotalDuration { get; set; }
            public TimeSpan AverageDuration { get; set; }
            public TimeSpan MaxDuration { get; set; }
            public TimeSpan MinDuration { get; set; }
            public long TotalRowsAffected { get; set; }
            public long SuccessCount { get; set; }
            public long ErrorCount { get; set; }
            public string LastError { get; set; }
        }

        public class ConnectionMetrics
        {
            public string ConnectionId { get; set; }
            public DateTime FirstSeen { get; set; }
            public DateTime LastConnected { get; set; }
            public long ConnectionCount { get; set; }
            public TimeSpan TotalDuration { get; set; }
            public TimeSpan AverageDuration { get; set; }
            public TimeSpan MaxDuration { get; set; }
            public TimeSpan MinDuration { get; set; }
            public long SuccessCount { get; set; }
            public long ErrorCount { get; set; }
            public string LastError { get; set; }
        }

        public class PerformanceSummary
        {
            public long TotalQueries { get; set; }
            public long TotalConnections { get; set; }
            public TimeSpan AverageQueryTime { get; set; }
            public TimeSpan AverageConnectionTime { get; set; }
            public int SlowQueries { get; set; }
            public long FailedQueries { get; set; }
            public long FailedConnections { get; set; }
            public double QuerySuccessRate { get; set; }
            public double ConnectionSuccessRate { get; set; }
            public DateTime GeneratedAt { get; set; }
        }

        public class PerformanceRecommendation
        {
            public RecommendationType Type { get; set; }
            public Severity Severity { get; set; }
            public string Message { get; set; }
            public string QueryHash { get; set; }
            public string Impact { get; set; }
        }

        public enum RecommendationType
        {
            SlowQuery,
            HighErrorRate,
            FrequentQuery,
            ConnectionIssue,
            IndexOptimization
        }

        public enum Severity
        {
            Low,
            Medium,
            High,
            Critical
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            if (_disposed) return;

            _metricsTimer?.Dispose();
            _cleanupTimer?.Dispose();
            SaveMetricsToFile();
            _disposed = true;

            _logger.LogInformation("Database performance monitor stopped");
        }

        #endregion
    }
} 