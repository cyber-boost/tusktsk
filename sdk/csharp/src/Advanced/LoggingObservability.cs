using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.IO;

namespace TuskLang
{
    /// <summary>
    /// Advanced logging and observability system for TuskLang C# SDK
    /// Provides structured logging, metrics collection, tracing, and monitoring capabilities
    /// </summary>
    public class LoggingObservability
    {
        private readonly List<ILogSink> _logSinks;
        private readonly List<IMetricsCollector> _metricsCollectors;
        private readonly List<ITraceExporter> _traceExporters;
        private readonly LoggingMetrics _metrics;
        private readonly ConcurrentQueue<LogEntry> _logBuffer;
        private readonly Timer _flushTimer;
        private readonly object _lock = new object();

        public LoggingObservability()
        {
            _logSinks = new List<ILogSink>();
            _metricsCollectors = new List<IMetricsCollector>();
            _traceExporters = new List<ITraceExporter>();
            _metrics = new LoggingMetrics();
            _logBuffer = new ConcurrentQueue<LogEntry>();
            
            // Flush logs every 5 seconds
            _flushTimer = new Timer(FlushLogs, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

            // Register default sinks
            RegisterLogSink(new ConsoleLogSink());
            RegisterLogSink(new FileLogSink());
            
            // Register default metrics collectors
            RegisterMetricsCollector(new PrometheusMetricsCollector());
            RegisterMetricsCollector(new InMemoryMetricsCollector());
            
            // Register default trace exporters
            RegisterTraceExporter(new JaegerTraceExporter());
            RegisterTraceExporter(new ZipkinTraceExporter());
        }

        /// <summary>
        /// Log a message with structured data
        /// </summary>
        public void Log(LogLevel level, string message, Dictionary<string, object> context = null, Exception exception = null)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Message = message,
                Context = context ?? new Dictionary<string, object>(),
                Exception = exception,
                TraceId = Activity.Current?.Id ?? Guid.NewGuid().ToString(),
                SpanId = Activity.Current?.SpanId.ToString() ?? Guid.NewGuid().ToString()
            };

            _logBuffer.Enqueue(entry);
            _metrics.RecordLog(level);

            // Flush immediately for high priority logs
            if (level >= LogLevel.Error)
            {
                FlushLogs();
            }
        }

        /// <summary>
        /// Log with specific log level methods
        /// </summary>
        public void LogDebug(string message, Dictionary<string, object> context = null) => Log(LogLevel.Debug, message, context);
        public void LogInfo(string message, Dictionary<string, object> context = null) => Log(LogLevel.Info, message, context);
        public void LogWarning(string message, Dictionary<string, object> context = null) => Log(LogLevel.Warning, message, context);
        public void LogError(string message, Dictionary<string, object> context = null, Exception exception = null) => Log(LogLevel.Error, message, context, exception);
        public void LogCritical(string message, Dictionary<string, object> context = null, Exception exception = null) => Log(LogLevel.Critical, message, context, exception);

        /// <summary>
        /// Record a metric
        /// </summary>
        public void RecordMetric(string name, double value, Dictionary<string, string> labels = null)
        {
            var metric = new Metric
            {
                Name = name,
                Value = value,
                Labels = labels ?? new Dictionary<string, string>(),
                Timestamp = DateTime.UtcNow
            };

            foreach (var collector in _metricsCollectors)
            {
                collector.RecordMetric(metric);
            }

            _metrics.RecordMetric(name, value);
        }

        /// <summary>
        /// Increment a counter metric
        /// </summary>
        public void IncrementCounter(string name, Dictionary<string, string> labels = null)
        {
            RecordMetric(name, 1, labels);
        }

        /// <summary>
        /// Record a histogram metric
        /// </summary>
        public void RecordHistogram(string name, double value, Dictionary<string, string> labels = null)
        {
            RecordMetric($"{name}_histogram", value, labels);
        }

        /// <summary>
        /// Start a trace span
        /// </summary>
        public IDisposable StartSpan(string operationName, Dictionary<string, object> attributes = null)
        {
            var span = new TraceSpan(operationName, attributes, this);
            return span;
        }

        /// <summary>
        /// Register a log sink
        /// </summary>
        public void RegisterLogSink(ILogSink sink)
        {
            lock (_lock)
            {
                _logSinks.Add(sink);
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
        /// Register a trace exporter
        /// </summary>
        public void RegisterTraceExporter(ITraceExporter exporter)
        {
            lock (_lock)
            {
                _traceExporters.Add(exporter);
            }
        }

        /// <summary>
        /// Get logging metrics
        /// </summary>
        public LoggingMetrics GetMetrics()
        {
            return _metrics;
        }

        /// <summary>
        /// Flush all buffered logs
        /// </summary>
        public async Task FlushAsync()
        {
            FlushLogs();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            _flushTimer?.Dispose();
            FlushLogs();
        }

        private void FlushLogs(object state = null)
        {
            var logsToFlush = new List<LogEntry>();
            
            while (_logBuffer.TryDequeue(out var entry))
            {
                logsToFlush.Add(entry);
            }

            if (logsToFlush.Count > 0)
            {
                lock (_lock)
                {
                    foreach (var sink in _logSinks)
                    {
                        try
                        {
                            sink.WriteLogs(logsToFlush);
                        }
                        catch (Exception ex)
                        {
                            // Log sink failure - use console as fallback
                            Console.WriteLine($"Log sink {sink.GetType().Name} failed: {ex.Message}");
                        }
                    }
                }
            }
        }

        internal void ExportSpan(TraceSpan span)
        {
            lock (_lock)
            {
                foreach (var exporter in _traceExporters)
                {
                    try
                    {
                        exporter.ExportSpan(span);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Trace exporter {exporter.GetType().Name} failed: {ex.Message}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Log entry with structured data
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; }
        public Dictionary<string, object> Context { get; set; }
        public Exception Exception { get; set; }
        public string TraceId { get; set; }
        public string SpanId { get; set; }
        public string ServiceName { get; set; } = "TuskLang";
        public string Environment { get; set; } = "Development";
    }

    /// <summary>
    /// Metric data structure
    /// </summary>
    public class Metric
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public Dictionary<string, string> Labels { get; set; }
        public DateTime Timestamp { get; set; }
        public MetricType Type { get; set; } = MetricType.Counter;
    }

    /// <summary>
    /// Trace span for distributed tracing
    /// </summary>
    public class TraceSpan : IDisposable
    {
        private readonly string _operationName;
        private readonly Dictionary<string, object> _attributes;
        private readonly LoggingObservability _logger;
        private readonly DateTime _startTime;
        private readonly string _spanId;
        private readonly string _traceId;
        private readonly string _parentSpanId;

        public TraceSpan(string operationName, Dictionary<string, object> attributes, LoggingObservability logger)
        {
            _operationName = operationName;
            _attributes = attributes ?? new Dictionary<string, object>();
            _logger = logger;
            _startTime = DateTime.UtcNow;
            _spanId = Guid.NewGuid().ToString();
            _traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            _parentSpanId = Activity.Current?.SpanId.ToString();

            // Log span start
            _logger.LogInfo($"Span started: {operationName}", new Dictionary<string, object>
            {
                ["span_id"] = _spanId,
                ["trace_id"] = _traceId,
                ["parent_span_id"] = _parentSpanId,
                ["operation"] = operationName
            });
        }

        public void Dispose()
        {
            var duration = DateTime.UtcNow - _startTime;
            
            // Log span end
            _logger.LogInfo($"Span ended: {_operationName}", new Dictionary<string, object>
            {
                ["span_id"] = _spanId,
                ["trace_id"] = _traceId,
                ["duration_ms"] = duration.TotalMilliseconds,
                ["operation"] = _operationName
            });

            // Export span
            _logger.ExportSpan(this);
        }

        public void AddAttribute(string key, object value)
        {
            _attributes[key] = value;
        }

        public void SetStatus(SpanStatus status, string description = null)
        {
            _attributes["status"] = status.ToString();
            if (!string.IsNullOrEmpty(description))
            {
                _attributes["status_description"] = description;
            }
        }
    }

    /// <summary>
    /// Log sink interface
    /// </summary>
    public interface ILogSink
    {
        void WriteLogs(List<LogEntry> logs);
    }

    /// <summary>
    /// Console log sink
    /// </summary>
    public class ConsoleLogSink : ILogSink
    {
        public void WriteLogs(List<LogEntry> logs)
        {
            foreach (var log in logs)
            {
                var color = GetConsoleColor(log.Level);
                var originalColor = Console.ForegroundColor;
                
                Console.ForegroundColor = color;
                Console.WriteLine(FormatLogEntry(log));
                Console.ForegroundColor = originalColor;
            }
        }

        private ConsoleColor GetConsoleColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };
        }

        private string FormatLogEntry(LogEntry entry)
        {
            var timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var level = entry.Level.ToString().ToUpper().PadRight(8);
            var message = entry.Message;
            
            var result = $"[{timestamp}] {level} {message}";
            
            if (entry.Context.Count > 0)
            {
                result += $" | Context: {JsonSerializer.Serialize(entry.Context)}";
            }
            
            if (entry.Exception != null)
            {
                result += $" | Exception: {entry.Exception.Message}";
            }
            
            return result;
        }
    }

    /// <summary>
    /// File log sink
    /// </summary>
    public class FileLogSink : ILogSink
    {
        private readonly string _logDirectory;
        private readonly object _lock = new object();

        public FileLogSink(string logDirectory = "logs")
        {
            _logDirectory = logDirectory;
            Directory.CreateDirectory(_logDirectory);
        }

        public void WriteLogs(List<LogEntry> logs)
        {
            var fileName = $"tusklang_{DateTime.UtcNow:yyyy-MM-dd}.log";
            var filePath = Path.Combine(_logDirectory, fileName);

            lock (_lock)
            {
                using var writer = new StreamWriter(filePath, true);
                foreach (var log in logs)
                {
                    writer.WriteLine(JsonSerializer.Serialize(log));
                }
            }
        }
    }

    /// <summary>
    /// Metrics collector interface
    /// </summary>
    public interface IMetricsCollector
    {
        void RecordMetric(Metric metric);
    }

    /// <summary>
    /// Prometheus metrics collector
    /// </summary>
    public class PrometheusMetricsCollector : IMetricsCollector
    {
        private readonly Dictionary<string, double> _counters = new Dictionary<string, double>();
        private readonly Dictionary<string, List<double>> _histograms = new Dictionary<string, List<double>>();

        public void RecordMetric(Metric metric)
        {
            if (metric.Type == MetricType.Counter)
            {
                var key = GetMetricKey(metric);
                _counters[key] = _counters.GetValueOrDefault(key, 0) + metric.Value;
            }
            else if (metric.Type == MetricType.Histogram)
            {
                var key = GetMetricKey(metric);
                if (!_histograms.ContainsKey(key))
                {
                    _histograms[key] = new List<double>();
                }
                _histograms[key].Add(metric.Value);
            }
        }

        private string GetMetricKey(Metric metric)
        {
            var labels = string.Join(",", metric.Labels.Select(kvp => $"{kvp.Key}=\"{kvp.Value}\""));
            return $"{metric.Name}{{{labels}}}";
        }

        public string GetPrometheusFormat()
        {
            var result = new List<string>();
            
            foreach (var counter in _counters)
            {
                result.Add($"{counter.Key} {counter.Value}");
            }
            
            foreach (var histogram in _histograms)
            {
                var values = histogram.Value;
                result.Add($"{histogram.Key}_count {values.Count}");
                result.Add($"{histogram.Key}_sum {values.Sum()}");
                result.Add($"{histogram.Key}_avg {values.Average()}");
            }
            
            return string.Join("\n", result);
        }
    }

    /// <summary>
    /// In-memory metrics collector
    /// </summary>
    public class InMemoryMetricsCollector : IMetricsCollector
    {
        private readonly List<Metric> _metrics = new List<Metric>();

        public void RecordMetric(Metric metric)
        {
            _metrics.Add(metric);
            
            // Keep only last 10000 metrics
            if (_metrics.Count > 10000)
            {
                _metrics.RemoveRange(0, _metrics.Count - 10000);
            }
        }

        public List<Metric> GetMetrics()
        {
            return new List<Metric>(_metrics);
        }
    }

    /// <summary>
    /// Trace exporter interface
    /// </summary>
    public interface ITraceExporter
    {
        void ExportSpan(TraceSpan span);
    }

    /// <summary>
    /// Jaeger trace exporter
    /// </summary>
    public class JaegerTraceExporter : ITraceExporter
    {
        public void ExportSpan(TraceSpan span)
        {
            // In a real implementation, this would send spans to Jaeger
            // For now, just log the span
            Console.WriteLine($"Jaeger: Exporting span {span.GetType().Name}");
        }
    }

    /// <summary>
    /// Zipkin trace exporter
    /// </summary>
    public class ZipkinTraceExporter : ITraceExporter
    {
        public void ExportSpan(TraceSpan span)
        {
            // In a real implementation, this would send spans to Zipkin
            // For now, just log the span
            Console.WriteLine($"Zipkin: Exporting span {span.GetType().Name}");
        }
    }

    /// <summary>
    /// Logging metrics collection
    /// </summary>
    public class LoggingMetrics
    {
        private readonly Dictionary<LogLevel, int> _logCounts = new Dictionary<LogLevel, int>();
        private readonly Dictionary<string, double> _metricValues = new Dictionary<string, double>();
        private readonly object _lock = new object();

        public void RecordLog(LogLevel level)
        {
            lock (_lock)
            {
                _logCounts[level] = _logCounts.GetValueOrDefault(level, 0) + 1;
            }
        }

        public void RecordMetric(string name, double value)
        {
            lock (_lock)
            {
                _metricValues[name] = value;
            }
        }

        public Dictionary<LogLevel, int> GetLogCounts()
        {
            lock (_lock)
            {
                return new Dictionary<LogLevel, int>(_logCounts);
            }
        }

        public Dictionary<string, double> GetMetrics()
        {
            lock (_lock)
            {
                return new Dictionary<string, double>(_metricValues);
            }
        }
    }

    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }

    public enum MetricType
    {
        Counter,
        Gauge,
        Histogram,
        Summary
    }

    public enum SpanStatus
    {
        Ok,
        Error,
        Unset
    }
} 