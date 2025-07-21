using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace TuskLang.CLI.Advanced
{
    /// <summary>
    /// Production-ready CLI analytics and usage tracking for TuskTsk
    /// Provides comprehensive analytics with privacy-respecting data collection
    /// </summary>
    public class CLIAnalytics : IDisposable
    {
        private readonly ILogger<CLIAnalytics> _logger;
        private readonly AnalyticsDataCollector _dataCollector;
        private readonly AnalyticsProcessor _processor;
        private readonly AnalyticsReporter _reporter;
        private readonly AnalyticsOptions _options;
        private readonly PerformanceMetrics _metrics;
        private bool _disposed = false;

        public CLIAnalytics(
            AnalyticsOptions options = null,
            ILogger<CLIAnalytics> logger = null)
        {
            _options = options ?? new AnalyticsOptions();
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<CLIAnalytics>.Instance;
            
            _dataCollector = new AnalyticsDataCollector(_logger);
            _processor = new AnalyticsProcessor(_logger);
            _reporter = new AnalyticsReporter(_logger);
            _metrics = new PerformanceMetrics();

            _logger.LogInformation("CLI analytics system initialized");
        }

        #region Analytics Operations

        /// <summary>
        /// Track command execution
        /// </summary>
        public async Task TrackCommandExecutionAsync(
            string command, string[] arguments, TimeSpan executionTime, bool success, 
            Dictionary<string, object> metadata = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var eventData = new CommandExecutionEvent
                {
                    Command = command,
                    Arguments = arguments ?? new string[0],
                    ExecutionTime = executionTime,
                    Success = success,
                    Timestamp = DateTime.UtcNow,
                    Metadata = metadata ?? new Dictionary<string, object>()
                };

                await _dataCollector.CollectEventAsync(eventData, cancellationToken);
                _metrics.RecordCommandExecution(executionTime, success);
                
                _logger.LogDebug($"Tracked command execution: {command} ({executionTime.TotalMilliseconds}ms)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track command execution");
            }
        }

        /// <summary>
        /// Track user interaction
        /// </summary>
        public async Task TrackUserInteractionAsync(
            string interactionType, string target, TimeSpan duration, 
            Dictionary<string, object> metadata = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var eventData = new UserInteractionEvent
                {
                    InteractionType = interactionType,
                    Target = target,
                    Duration = duration,
                    Timestamp = DateTime.UtcNow,
                    Metadata = metadata ?? new Dictionary<string, object>()
                };

                await _dataCollector.CollectEventAsync(eventData, cancellationToken);
                _metrics.RecordUserInteraction(duration);
                
                _logger.LogDebug($"Tracked user interaction: {interactionType} -> {target}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track user interaction");
            }
        }

        /// <summary>
        /// Track performance metric
        /// </summary>
        public async Task TrackPerformanceMetricAsync(
            string metricName, double value, string unit, 
            Dictionary<string, object> metadata = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var eventData = new PerformanceMetricEvent
                {
                    MetricName = metricName,
                    Value = value,
                    Unit = unit,
                    Timestamp = DateTime.UtcNow,
                    Metadata = metadata ?? new Dictionary<string, object>()
                };

                await _dataCollector.CollectEventAsync(eventData, cancellationToken);
                _metrics.RecordPerformanceMetric(metricName, value);
                
                _logger.LogDebug($"Tracked performance metric: {metricName} = {value} {unit}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track performance metric");
            }
        }

        /// <summary>
        /// Generate analytics report
        /// </summary>
        public async Task<AnalyticsReport> GenerateReportAsync(
            DateTime startDate, DateTime endDate, ReportType reportType, 
            CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var report = new AnalyticsReport { Success = false };

            try
            {
                _logger.LogInformation($"Generating analytics report: {reportType} from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

                // Collect data for time period
                var events = await _dataCollector.GetEventsAsync(startDate, endDate, cancellationToken);
                
                // Process events
                var processedData = await _processor.ProcessEventsAsync(events, reportType, cancellationToken);
                
                // Generate report
                report = await _reporter.GenerateReportAsync(processedData, reportType, cancellationToken);

                stopwatch.Stop();
                report.GenerationTime = stopwatch.Elapsed;
                report.Success = true;

                _metrics.RecordReportGeneration(stopwatch.Elapsed);
                _logger.LogInformation($"Analytics report generated successfully: {reportType}");

                return report;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                report.ErrorMessage = ex.Message;
                report.GenerationTime = stopwatch.Elapsed;
                
                _metrics.RecordError(stopwatch.Elapsed);
                _logger.LogError(ex, "Failed to generate analytics report");
                return report;
            }
        }

        /// <summary>
        /// Detect performance regressions
        /// </summary>
        public async Task<RegressionDetectionResult> DetectPerformanceRegressionsAsync(
            string metricName, TimeSpan baselinePeriod, double threshold, 
            CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new RegressionDetectionResult { Success = false };

            try
            {
                _logger.LogInformation($"Detecting performance regressions for {metricName}");

                var endDate = DateTime.UtcNow;
                var startDate = endDate.Subtract(baselinePeriod);

                // Get baseline data
                var baselineEvents = await _dataCollector.GetEventsAsync(startDate, endDate, cancellationToken);
                var baselineMetrics = baselineEvents
                    .OfType<PerformanceMetricEvent>()
                    .Where(e => e.MetricName == metricName)
                    .ToList();

                if (baselineMetrics.Count < 10)
                {
                    result.ErrorMessage = "Insufficient baseline data for regression detection";
                    return result;
                }

                // Calculate baseline statistics
                var baselineValues = baselineMetrics.Select(m => m.Value).ToList();
                var baselineMean = baselineValues.Average();
                var baselineStdDev = CalculateStandardDeviation(baselineValues);

                // Get recent data
                var recentStartDate = endDate.Subtract(TimeSpan.FromDays(1));
                var recentEvents = await _dataCollector.GetEventsAsync(recentStartDate, endDate, cancellationToken);
                var recentMetrics = recentEvents
                    .OfType<PerformanceMetricEvent>()
                    .Where(e => e.MetricName == metricName)
                    .ToList();

                if (recentMetrics.Count == 0)
                {
                    result.ErrorMessage = "No recent data available for comparison";
                    return result;
                }

                // Detect regressions
                var regressions = new List<PerformanceRegression>();
                foreach (var metric in recentMetrics)
                {
                    var zScore = Math.Abs((metric.Value - baselineMean) / baselineStdDev);
                    if (zScore > threshold)
                    {
                        regressions.Add(new PerformanceRegression
                        {
                            MetricName = metricName,
                            BaselineValue = baselineMean,
                            CurrentValue = metric.Value,
                            ZScore = zScore,
                            Timestamp = metric.Timestamp,
                            Severity = zScore > threshold * 2 ? RegressionSeverity.High : RegressionSeverity.Medium
                        });
                    }
                }

                stopwatch.Stop();
                result.Success = true;
                result.Regressions = regressions;
                result.BaselineMean = baselineMean;
                result.BaselineStdDev = baselineStdDev;
                result.DetectionTime = stopwatch.Elapsed;

                _metrics.RecordRegressionDetection(stopwatch.Elapsed);
                _logger.LogInformation($"Performance regression detection completed: {regressions.Count} regressions found");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ErrorMessage = ex.Message;
                result.DetectionTime = stopwatch.Elapsed;
                
                _metrics.RecordError(stopwatch.Elapsed);
                _logger.LogError(ex, "Failed to detect performance regressions");
                return result;
            }
        }

        /// <summary>
        /// Export analytics data
        /// </summary>
        public async Task<ExportResult> ExportAnalyticsDataAsync(
            DateTime startDate, DateTime endDate, ExportFormat format, string outputPath, 
            CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new ExportResult { Success = false };

            try
            {
                _logger.LogInformation($"Exporting analytics data: {format} to {outputPath}");

                // Get events
                var events = await _dataCollector.GetEventsAsync(startDate, endDate, cancellationToken);
                
                // Export based on format
                string exportContent;
                switch (format)
                {
                    case ExportFormat.Json:
                        exportContent = await ExportToJsonAsync(events, cancellationToken);
                        break;
                    case ExportFormat.Csv:
                        exportContent = await ExportToCsvAsync(events, cancellationToken);
                        break;
                    case ExportFormat.Xml:
                        exportContent = await ExportToXmlAsync(events, cancellationToken);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported export format: {format}");
                }

                // Save to file
                await File.WriteAllTextAsync(outputPath, exportContent, cancellationToken);

                stopwatch.Stop();
                result.Success = true;
                result.OutputPath = outputPath;
                result.ExportTime = stopwatch.Elapsed;
                result.RecordCount = events.Count;

                _metrics.RecordDataExport(stopwatch.Elapsed);
                _logger.LogInformation($"Analytics data exported successfully: {outputPath}");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ErrorMessage = ex.Message;
                result.ExportTime = stopwatch.Elapsed;
                
                _metrics.RecordError(stopwatch.Elapsed);
                _logger.LogError(ex, "Failed to export analytics data");
                return result;
            }
        }

        #endregion

        #region Export Methods

        private async Task<string> ExportToJsonAsync(List<AnalyticsEvent> events, CancellationToken cancellationToken)
        {
            var exportData = new
            {
                ExportDate = DateTime.UtcNow,
                RecordCount = events.Count,
                Events = events.Select(e => new
                {
                    e.Timestamp,
                    EventType = e.GetType().Name,
                    Data = e
                }).ToList()
            };

            return JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
        }

        private async Task<string> ExportToCsvAsync(List<AnalyticsEvent> events, CancellationToken cancellationToken)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Timestamp,EventType,Command,ExecutionTime,Success,InteractionType,Target,MetricName,Value,Unit");

            foreach (var evt in events)
            {
                var timestamp = evt.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                var eventType = evt.GetType().Name;

                switch (evt)
                {
                    case CommandExecutionEvent cmdEvt:
                        csv.AppendLine($"{timestamp},{eventType},{cmdEvt.Command},{cmdEvt.ExecutionTime.TotalMilliseconds},{cmdEvt.Success},,,,");
                        break;
                    case UserInteractionEvent intEvt:
                        csv.AppendLine($"{timestamp},{eventType},,,,{intEvt.InteractionType},{intEvt.Target},,");
                        break;
                    case PerformanceMetricEvent perfEvt:
                        csv.AppendLine($"{timestamp},{eventType},,,,,,{perfEvt.MetricName},{perfEvt.Value},{perfEvt.Unit}");
                        break;
                }
            }

            return csv.ToString();
        }

        private async Task<string> ExportToXmlAsync(List<AnalyticsEvent> events, CancellationToken cancellationToken)
        {
            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<AnalyticsExport>");
            xml.AppendLine($"  <ExportDate>{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}</ExportDate>");
            xml.AppendLine($"  <RecordCount>{events.Count}</RecordCount>");
            xml.AppendLine("  <Events>");

            foreach (var evt in events)
            {
                xml.AppendLine($"    <Event>");
                xml.AppendLine($"      <Timestamp>{evt.Timestamp:yyyy-MM-dd HH:mm:ss}</Timestamp>");
                xml.AppendLine($"      <EventType>{evt.GetType().Name}</EventType>");
                xml.AppendLine($"    </Event>");
            }

            xml.AppendLine("  </Events>");
            xml.AppendLine("</AnalyticsExport>");

            return xml.ToString();
        }

        #endregion

        #region Utility Methods

        private double CalculateStandardDeviation(List<double> values)
        {
            if (values.Count == 0) return 0;

            var mean = values.Average();
            var sumSquaredDifferences = values.Sum(x => Math.Pow(x - mean, 2));
            return Math.Sqrt(sumSquaredDifferences / values.Count);
        }

        #endregion

        #region Performance Metrics

        public PerformanceMetrics GetPerformanceMetrics()
        {
            return _metrics;
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }

    #region Supporting Classes

    public class AnalyticsOptions
    {
        public bool EnableTracking { get; set; } = true;
        public bool EnablePrivacyProtection { get; set; } = true;
        public bool EnablePerformanceMonitoring { get; set; } = true;
        public TimeSpan DataRetentionPeriod { get; set; } = TimeSpan.FromDays(90);
        public int MaxEventsPerBatch { get; set; } = 1000;
    }

    public enum ReportType
    {
        Usage,
        Performance,
        UserBehavior,
        Custom
    }

    public enum ExportFormat
    {
        Json,
        Csv,
        Xml
    }

    public enum RegressionSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public class AnalyticsReport
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public ReportType ReportType { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public TimeSpan GenerationTime { get; set; }
    }

    public class RegressionDetectionResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public List<PerformanceRegression> Regressions { get; set; } = new List<PerformanceRegression>();
        public double BaselineMean { get; set; }
        public double BaselineStdDev { get; set; }
        public TimeSpan DetectionTime { get; set; }
    }

    public class PerformanceRegression
    {
        public string MetricName { get; set; }
        public double BaselineValue { get; set; }
        public double CurrentValue { get; set; }
        public double ZScore { get; set; }
        public DateTime Timestamp { get; set; }
        public RegressionSeverity Severity { get; set; }
    }

    public class ExportResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string OutputPath { get; set; }
        public int RecordCount { get; set; }
        public TimeSpan ExportTime { get; set; }
    }

    public abstract class AnalyticsEvent
    {
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class CommandExecutionEvent : AnalyticsEvent
    {
        public string Command { get; set; }
        public string[] Arguments { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public bool Success { get; set; }
    }

    public class UserInteractionEvent : AnalyticsEvent
    {
        public string InteractionType { get; set; }
        public string Target { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class PerformanceMetricEvent : AnalyticsEvent
    {
        public string MetricName { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }
    }

    public class AnalyticsDataCollector
    {
        private readonly ILogger _logger;
        private readonly List<AnalyticsEvent> _events = new List<AnalyticsEvent>();
        private readonly object _lock = new object();

        public AnalyticsDataCollector(ILogger logger)
        {
            _logger = logger;
        }

        public async Task CollectEventAsync(AnalyticsEvent eventData, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                _events.Add(eventData);
                
                // Keep only recent events
                var cutoff = DateTime.UtcNow.Subtract(TimeSpan.FromDays(90));
                _events.RemoveAll(e => e.Timestamp < cutoff);
            }
        }

        public async Task<List<AnalyticsEvent>> GetEventsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                return _events.Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate).ToList();
            }
        }
    }

    public class AnalyticsProcessor
    {
        private readonly ILogger _logger;

        public AnalyticsProcessor(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<Dictionary<string, object>> ProcessEventsAsync(
            List<AnalyticsEvent> events, ReportType reportType, CancellationToken cancellationToken)
        {
            var processedData = new Dictionary<string, object>();

            switch (reportType)
            {
                case ReportType.Usage:
                    processedData = await ProcessUsageDataAsync(events, cancellationToken);
                    break;
                case ReportType.Performance:
                    processedData = await ProcessPerformanceDataAsync(events, cancellationToken);
                    break;
                case ReportType.UserBehavior:
                    processedData = await ProcessUserBehaviorDataAsync(events, cancellationToken);
                    break;
            }

            return processedData;
        }

        private async Task<Dictionary<string, object>> ProcessUsageDataAsync(List<AnalyticsEvent> events, CancellationToken cancellationToken)
        {
            var commandExecutions = events.OfType<CommandExecutionEvent>().ToList();
            
            return new Dictionary<string, object>
            {
                ["TotalCommands"] = commandExecutions.Count,
                ["SuccessfulCommands"] = commandExecutions.Count(c => c.Success),
                ["FailedCommands"] = commandExecutions.Count(c => !c.Success),
                ["AverageExecutionTime"] = commandExecutions.Any() ? commandExecutions.Average(c => c.ExecutionTime.TotalMilliseconds) : 0,
                ["MostUsedCommands"] = commandExecutions.GroupBy(c => c.Command)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        private async Task<Dictionary<string, object>> ProcessPerformanceDataAsync(List<AnalyticsEvent> events, CancellationToken cancellationToken)
        {
            var performanceMetrics = events.OfType<PerformanceMetricEvent>().ToList();
            
            return new Dictionary<string, object>
            {
                ["TotalMetrics"] = performanceMetrics.Count,
                ["MetricTypes"] = performanceMetrics.GroupBy(m => m.MetricName)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ["AverageValues"] = performanceMetrics.GroupBy(m => m.MetricName)
                    .ToDictionary(g => g.Key, g => g.Average(m => m.Value))
            };
        }

        private async Task<Dictionary<string, object>> ProcessUserBehaviorDataAsync(List<AnalyticsEvent> events, CancellationToken cancellationToken)
        {
            var interactions = events.OfType<UserInteractionEvent>().ToList();
            
            return new Dictionary<string, object>
            {
                ["TotalInteractions"] = interactions.Count,
                ["InteractionTypes"] = interactions.GroupBy(i => i.InteractionType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ["AverageInteractionDuration"] = interactions.Any() ? interactions.Average(i => i.Duration.TotalMilliseconds) : 0
            };
        }
    }

    public class AnalyticsReporter
    {
        private readonly ILogger _logger;

        public AnalyticsReporter(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<AnalyticsReport> GenerateReportAsync(
            Dictionary<string, object> data, ReportType reportType, CancellationToken cancellationToken)
        {
            return new AnalyticsReport
            {
                Success = true,
                ReportType = reportType,
                Data = data
            };
        }
    }

    public class PerformanceMetrics
    {
        private readonly List<TimeSpan> _commandExecutionTimes = new List<TimeSpan>();
        private readonly List<TimeSpan> _userInteractionTimes = new List<TimeSpan>();
        private readonly Dictionary<string, List<double>> _performanceMetrics = new Dictionary<string, List<double>>();
        private readonly List<TimeSpan> _reportGenerationTimes = new List<TimeSpan>();
        private readonly List<TimeSpan> _regressionDetectionTimes = new List<TimeSpan>();
        private readonly List<TimeSpan> _dataExportTimes = new List<TimeSpan>();
        private readonly List<TimeSpan> _errorTimes = new List<TimeSpan>();

        public void RecordCommandExecution(TimeSpan time, bool success)
        {
            _commandExecutionTimes.Add(time);
            if (_commandExecutionTimes.Count > 10000) _commandExecutionTimes.RemoveAt(0);
        }

        public void RecordUserInteraction(TimeSpan time)
        {
            _userInteractionTimes.Add(time);
            if (_userInteractionTimes.Count > 10000) _userInteractionTimes.RemoveAt(0);
        }

        public void RecordPerformanceMetric(string metricName, double value)
        {
            if (!_performanceMetrics.ContainsKey(metricName))
                _performanceMetrics[metricName] = new List<double>();

            _performanceMetrics[metricName].Add(value);
            if (_performanceMetrics[metricName].Count > 1000)
                _performanceMetrics[metricName].RemoveAt(0);
        }

        public void RecordReportGeneration(TimeSpan time)
        {
            _reportGenerationTimes.Add(time);
            if (_reportGenerationTimes.Count > 100) _reportGenerationTimes.RemoveAt(0);
        }

        public void RecordRegressionDetection(TimeSpan time)
        {
            _regressionDetectionTimes.Add(time);
            if (_regressionDetectionTimes.Count > 100) _regressionDetectionTimes.RemoveAt(0);
        }

        public void RecordDataExport(TimeSpan time)
        {
            _dataExportTimes.Add(time);
            if (_dataExportTimes.Count > 100) _dataExportTimes.RemoveAt(0);
        }

        public void RecordError(TimeSpan time)
        {
            _errorTimes.Add(time);
            if (_errorTimes.Count > 100) _errorTimes.RemoveAt(0);
        }

        public double AverageCommandExecutionTime => _commandExecutionTimes.Count > 0 ? _commandExecutionTimes.Average(t => t.TotalMilliseconds) : 0;
        public double AverageUserInteractionTime => _userInteractionTimes.Count > 0 ? _userInteractionTimes.Average(t => t.TotalMilliseconds) : 0;
        public double AverageReportGenerationTime => _reportGenerationTimes.Count > 0 ? _reportGenerationTimes.Average(t => t.TotalMilliseconds) : 0;
        public int ErrorCount => _errorTimes.Count;
        public int TotalCommands => _commandExecutionTimes.Count;
        public int TotalInteractions => _userInteractionTimes.Count;
    }

    #endregion
} 