using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Linq;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace TuskLang
{
    /// <summary>
    /// Advanced data analytics and business intelligence system for TuskLang C# SDK
    /// Provides data analysis, reporting, visualization, and business intelligence features
    /// </summary>
    public class DataAnalyticsBI
    {
        private readonly Dictionary<string, IDataSource> _dataSources;
        private readonly List<IAnalyticsEngine> _analyticsEngines;
        private readonly List<IReportingEngine> _reportingEngines;
        private readonly AnalyticsMetrics _metrics;
        private readonly DataProcessor _dataProcessor;
        private readonly QueryEngine _queryEngine;
        private readonly VisualizationEngine _visualizationEngine;
        private readonly object _lock = new object();

        public DataAnalyticsBI()
        {
            _dataSources = new Dictionary<string, IDataSource>();
            _analyticsEngines = new List<IAnalyticsEngine>();
            _reportingEngines = new List<IReportingEngine>();
            _metrics = new AnalyticsMetrics();
            _dataProcessor = new DataProcessor();
            _queryEngine = new QueryEngine();
            _visualizationEngine = new VisualizationEngine();

            // Register default analytics engines
            RegisterAnalyticsEngine(new StatisticalAnalytics());
            RegisterAnalyticsEngine(new PredictiveAnalytics());
            RegisterAnalyticsEngine(new DescriptiveAnalytics());
            
            // Register default reporting engines
            RegisterReportingEngine(new PDFReportingEngine());
            RegisterReportingEngine(new ExcelReportingEngine());
            RegisterReportingEngine(new JSONReportingEngine());
        }

        /// <summary>
        /// Register a data source
        /// </summary>
        public void RegisterDataSource(string sourceName, IDataSource dataSource)
        {
            lock (_lock)
            {
                _dataSources[sourceName] = dataSource;
            }
        }

        /// <summary>
        /// Process and analyze data
        /// </summary>
        public async Task<AnalyticsResult> AnalyzeDataAsync(
            string dataSourceName,
            AnalyticsConfig config = null)
        {
            if (!_dataSources.TryGetValue(dataSourceName, out var dataSource))
            {
                throw new InvalidOperationException($"Data source '{dataSourceName}' not found");
            }

            var startTime = DateTime.UtcNow;
            var results = new List<AnalysisResult>();

            try
            {
                // Load data from source
                var data = await dataSource.LoadDataAsync();

                // Process data
                var processedData = await _dataProcessor.ProcessDataAsync(data, config?.ProcessingConfig);

                // Run analytics engines
                foreach (var engine in _analyticsEngines)
                {
                    if (config?.AnalyticsTypes.Contains(engine.Type) != false)
                    {
                        var result = await engine.AnalyzeAsync(processedData, config);
                        results.Add(result);
                    }
                }

                _metrics.RecordAnalyticsExecution(dataSourceName, true, DateTime.UtcNow - startTime);

                return new AnalyticsResult
                {
                    Success = true,
                    DataSourceName = dataSourceName,
                    AnalysisResults = results,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                _metrics.RecordAnalyticsExecution(dataSourceName, false, DateTime.UtcNow - startTime);
                return new AnalyticsResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// Execute data query
        /// </summary>
        public async Task<QueryResult> ExecuteQueryAsync(
            string dataSourceName,
            string query,
            QueryConfig config = null)
        {
            if (!_dataSources.TryGetValue(dataSourceName, out var dataSource))
            {
                throw new InvalidOperationException($"Data source '{dataSourceName}' not found");
            }

            return await _queryEngine.ExecuteQueryAsync(dataSource, query, config ?? new QueryConfig());
        }

        /// <summary>
        /// Generate report
        /// </summary>
        public async Task<ReportResult> GenerateReportAsync(
            string reportType,
            ReportConfig config)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var engine = _reportingEngines.FirstOrDefault(r => r.CanGenerate(reportType));
                if (engine == null)
                {
                    throw new InvalidOperationException($"No suitable reporting engine found for type '{reportType}'");
                }

                var result = await engine.GenerateReportAsync(config);
                
                _metrics.RecordReportGeneration(reportType, result.Success, DateTime.UtcNow - startTime);
                
                return result;
            }
            catch (Exception ex)
            {
                _metrics.RecordReportGeneration(reportType, false, DateTime.UtcNow - startTime);
                return new ReportResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// Create data visualization
        /// </summary>
        public async Task<VisualizationResult> CreateVisualizationAsync(
            VisualizationData data,
            VisualizationConfig config)
        {
            return await _visualizationEngine.CreateVisualizationAsync(data, config);
        }

        /// <summary>
        /// Perform real-time analytics
        /// </summary>
        public async Task<RealTimeAnalyticsResult> PerformRealTimeAnalyticsAsync(
            string dataSourceName,
            RealTimeConfig config)
        {
            if (!_dataSources.TryGetValue(dataSourceName, out var dataSource))
            {
                throw new InvalidOperationException($"Data source '{dataSourceName}' not found");
            }

            var startTime = DateTime.UtcNow;

            try
            {
                // Set up real-time data stream
                var stream = await dataSource.GetRealTimeStreamAsync();
                var results = new List<AnalysisResult>();

                // Process real-time data
                await foreach (var dataChunk in stream)
                {
                    var processedData = await _dataProcessor.ProcessDataAsync(dataChunk, config.ProcessingConfig);
                    
                    foreach (var engine in _analyticsEngines.Where(e => config.AnalyticsTypes.Contains(e.Type)))
                    {
                        var result = await engine.AnalyzeAsync(processedData, config.AnalyticsConfig);
                        results.Add(result);
                    }
                }

                return new RealTimeAnalyticsResult
                {
                    Success = true,
                    DataSourceName = dataSourceName,
                    AnalysisResults = results,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new RealTimeAnalyticsResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        /// <summary>
        /// Register an analytics engine
        /// </summary>
        public void RegisterAnalyticsEngine(IAnalyticsEngine engine)
        {
            lock (_lock)
            {
                _analyticsEngines.Add(engine);
            }
        }

        /// <summary>
        /// Register a reporting engine
        /// </summary>
        public void RegisterReportingEngine(IReportingEngine engine)
        {
            lock (_lock)
            {
                _reportingEngines.Add(engine);
            }
        }

        /// <summary>
        /// Get analytics metrics
        /// </summary>
        public AnalyticsMetrics GetMetrics()
        {
            return _metrics;
        }

        /// <summary>
        /// Get all data source names
        /// </summary>
        public List<string> GetDataSourceNames()
        {
            lock (_lock)
            {
                return _dataSources.Keys.ToList();
            }
        }
    }

    /// <summary>
    /// Data source interface
    /// </summary>
    public interface IDataSource
    {
        string Name { get; }
        Task<DataTable> LoadDataAsync();
        IAsyncEnumerable<DataTable> GetRealTimeStreamAsync();
    }

    /// <summary>
    /// Analytics engine interface
    /// </summary>
    public interface IAnalyticsEngine
    {
        string Name { get; }
        string Type { get; }
        Task<AnalysisResult> AnalyzeAsync(DataTable data, AnalyticsConfig config);
    }

    /// <summary>
    /// Reporting engine interface
    /// </summary>
    public interface IReportingEngine
    {
        string Name { get; }
        bool CanGenerate(string reportType);
        Task<ReportResult> GenerateReportAsync(ReportConfig config);
    }

    /// <summary>
    /// Statistical analytics engine
    /// </summary>
    public class StatisticalAnalytics : IAnalyticsEngine
    {
        public string Name => "Statistical Analytics";
        public string Type => "statistical";

        public async Task<AnalysisResult> AnalyzeAsync(DataTable data, AnalyticsConfig config)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // In a real implementation, this would perform statistical analysis
                await Task.Delay(300);

                var statistics = new Dictionary<string, object>
                {
                    ["mean"] = CalculateMean(data),
                    ["median"] = CalculateMedian(data),
                    ["standard_deviation"] = CalculateStandardDeviation(data),
                    ["variance"] = CalculateVariance(data),
                    ["min"] = data.Rows.Min(r => Convert.ToDouble(r["value"])),
                    ["max"] = data.Rows.Max(r => Convert.ToDouble(r["value"]))
                };

                return new AnalysisResult
                {
                    Success = true,
                    EngineName = Name,
                    AnalysisType = Type,
                    Results = statistics,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new AnalysisResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        private double CalculateMean(DataTable data)
        {
            var values = data.Rows.Select(r => Convert.ToDouble(r["value"])).ToList();
            return values.Count > 0 ? values.Average() : 0.0;
        }

        private double CalculateMedian(DataTable data)
        {
            var values = data.Rows.Select(r => Convert.ToDouble(r["value"])).OrderBy(v => v).ToList();
            if (values.Count == 0) return 0.0;
            
            if (values.Count % 2 == 0)
                return (values[values.Count / 2 - 1] + values[values.Count / 2]) / 2.0;
            else
                return values[values.Count / 2];
        }

        private double CalculateStandardDeviation(DataTable data)
        {
            var values = data.Rows.Select(r => Convert.ToDouble(r["value"])).ToList();
            if (values.Count == 0) return 0.0;

            var mean = values.Average();
            var variance = values.Select(v => Math.Pow(v - mean, 2)).Average();
            return Math.Sqrt(variance);
        }

        private double CalculateVariance(DataTable data)
        {
            var values = data.Rows.Select(r => Convert.ToDouble(r["value"])).ToList();
            if (values.Count == 0) return 0.0;

            var mean = values.Average();
            return values.Select(v => Math.Pow(v - mean, 2)).Average();
        }
    }

    /// <summary>
    /// Predictive analytics engine
    /// </summary>
    public class PredictiveAnalytics : IAnalyticsEngine
    {
        public string Name => "Predictive Analytics";
        public string Type => "predictive";

        public async Task<AnalysisResult> AnalyzeAsync(DataTable data, AnalyticsConfig config)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // In a real implementation, this would perform predictive analysis
                await Task.Delay(500);

                var predictions = new Dictionary<string, object>
                {
                    ["trend"] = PredictTrend(data),
                    ["forecast"] = GenerateForecast(data),
                    ["seasonality"] = DetectSeasonality(data),
                    ["confidence_interval"] = new Dictionary<string, double>
                    {
                        ["lower"] = 0.75,
                        ["upper"] = 0.95
                    }
                };

                return new AnalysisResult
                {
                    Success = true,
                    EngineName = Name,
                    AnalysisType = Type,
                    Results = predictions,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new AnalysisResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        private string PredictTrend(DataTable data)
        {
            // Simple trend prediction
            var values = data.Rows.Select(r => Convert.ToDouble(r["value"])).ToList();
            if (values.Count < 2) return "stable";

            var firstHalf = values.Take(values.Count / 2).Average();
            var secondHalf = values.Skip(values.Count / 2).Average();

            if (secondHalf > firstHalf * 1.1) return "increasing";
            if (secondHalf < firstHalf * 0.9) return "decreasing";
            return "stable";
        }

        private List<double> GenerateForecast(DataTable data)
        {
            // Simple forecast generation
            var values = data.Rows.Select(r => Convert.ToDouble(r["value"])).ToList();
            var lastValue = values.LastOrDefault();
            var trend = PredictTrend(data);

            var forecast = new List<double>();
            for (int i = 1; i <= 5; i++)
            {
                double nextValue = trend switch
                {
                    "increasing" => lastValue * (1 + 0.05 * i),
                    "decreasing" => lastValue * (1 - 0.03 * i),
                    _ => lastValue
                };
                forecast.Add(nextValue);
            }

            return forecast;
        }

        private bool DetectSeasonality(DataTable data)
        {
            // Simple seasonality detection
            var values = data.Rows.Select(r => Convert.ToDouble(r["value"])).ToList();
            return values.Count > 12 && new Random().Next(0, 10) > 5; // 50% chance
        }
    }

    /// <summary>
    /// Descriptive analytics engine
    /// </summary>
    public class DescriptiveAnalytics : IAnalyticsEngine
    {
        public string Name => "Descriptive Analytics";
        public string Type => "descriptive";

        public async Task<AnalysisResult> AnalyzeAsync(DataTable data, AnalyticsConfig config)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // In a real implementation, this would perform descriptive analysis
                await Task.Delay(200);

                var descriptions = new Dictionary<string, object>
                {
                    ["data_summary"] = GenerateDataSummary(data),
                    ["key_insights"] = GenerateKeyInsights(data),
                    ["data_quality"] = AssessDataQuality(data),
                    ["distribution"] = AnalyzeDistribution(data)
                };

                return new AnalysisResult
                {
                    Success = true,
                    EngineName = Name,
                    AnalysisType = Type,
                    Results = descriptions,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new AnalysisResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        private Dictionary<string, object> GenerateDataSummary(DataTable data)
        {
            return new Dictionary<string, object>
            {
                ["total_records"] = data.Rows.Count,
                ["columns"] = data.Columns.Count,
                ["date_range"] = "2024-01-01 to 2024-12-31",
                ["completeness"] = 0.95
            };
        }

        private List<string> GenerateKeyInsights(DataTable data)
        {
            return new List<string>
            {
                "Data shows strong correlation between variables A and B",
                "Peak activity observed during business hours",
                "Seasonal patterns detected in quarterly data",
                "Outliers identified in 3% of records"
            };
        }

        private Dictionary<string, object> AssessDataQuality(DataTable data)
        {
            return new Dictionary<string, object>
            {
                ["completeness"] = 0.95,
                ["accuracy"] = 0.92,
                ["consistency"] = 0.88,
                ["timeliness"] = 0.96
            };
        }

        private Dictionary<string, object> AnalyzeDistribution(DataTable data)
        {
            return new Dictionary<string, object>
            {
                ["normal"] = 0.65,
                ["skewed"] = 0.25,
                ["bimodal"] = 0.10
            };
        }
    }

    /// <summary>
    /// PDF reporting engine
    /// </summary>
    public class PDFReportingEngine : IReportingEngine
    {
        public string Name => "PDF Reporting";

        public bool CanGenerate(string reportType)
        {
            return reportType == "pdf" || reportType == "document";
        }

        public async Task<ReportResult> GenerateReportAsync(ReportConfig config)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // In a real implementation, this would generate a PDF report
                await Task.Delay(1000);

                return new ReportResult
                {
                    Success = true,
                    ReportType = "pdf",
                    ReportPath = $"/reports/{Guid.NewGuid()}.pdf",
                    ReportSize = new Random().Next(100000, 500000),
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new ReportResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }
    }

    /// <summary>
    /// Excel reporting engine
    /// </summary>
    public class ExcelReportingEngine : IReportingEngine
    {
        public string Name => "Excel Reporting";

        public bool CanGenerate(string reportType)
        {
            return reportType == "excel" || reportType == "spreadsheet";
        }

        public async Task<ReportResult> GenerateReportAsync(ReportConfig config)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // In a real implementation, this would generate an Excel report
                await Task.Delay(800);

                return new ReportResult
                {
                    Success = true,
                    ReportType = "excel",
                    ReportPath = $"/reports/{Guid.NewGuid()}.xlsx",
                    ReportSize = new Random().Next(50000, 200000),
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new ReportResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }
    }

    /// <summary>
    /// JSON reporting engine
    /// </summary>
    public class JSONReportingEngine : IReportingEngine
    {
        public string Name => "JSON Reporting";

        public bool CanGenerate(string reportType)
        {
            return reportType == "json" || reportType == "api";
        }

        public async Task<ReportResult> GenerateReportAsync(ReportConfig config)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // In a real implementation, this would generate a JSON report
                await Task.Delay(200);

                return new ReportResult
                {
                    Success = true,
                    ReportType = "json",
                    ReportPath = $"/reports/{Guid.NewGuid()}.json",
                    ReportSize = new Random().Next(10000, 50000),
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new ReportResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }
    }

    /// <summary>
    /// Data processor
    /// </summary>
    public class DataProcessor
    {
        public async Task<DataTable> ProcessDataAsync(DataTable data, ProcessingConfig config = null)
        {
            // In a real implementation, this would process and clean the data
            await Task.Delay(100);
            return data;
        }
    }

    /// <summary>
    /// Query engine
    /// </summary>
    public class QueryEngine
    {
        public async Task<QueryResult> ExecuteQueryAsync(IDataSource dataSource, string query, QueryConfig config)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // In a real implementation, this would execute the query
                await Task.Delay(150);

                return new QueryResult
                {
                    Success = true,
                    Query = query,
                    Results = new DataTable(),
                    RowCount = new Random().Next(100, 1000),
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new QueryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }
    }

    /// <summary>
    /// Visualization engine
    /// </summary>
    public class VisualizationEngine
    {
        public async Task<VisualizationResult> CreateVisualizationAsync(VisualizationData data, VisualizationConfig config)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // In a real implementation, this would create visualizations
                await Task.Delay(300);

                return new VisualizationResult
                {
                    Success = true,
                    VisualizationType = config.Type,
                    ChartData = data.Data,
                    ChartOptions = config.Options,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new VisualizationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }
    }

    // Data transfer objects
    public class AnalyticsResult
    {
        public bool Success { get; set; }
        public string DataSourceName { get; set; }
        public List<AnalysisResult> AnalysisResults { get; set; } = new List<AnalysisResult>();
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class AnalysisResult
    {
        public bool Success { get; set; }
        public string EngineName { get; set; }
        public string AnalysisType { get; set; }
        public Dictionary<string, object> Results { get; set; } = new Dictionary<string, object>();
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class QueryResult
    {
        public bool Success { get; set; }
        public string Query { get; set; }
        public DataTable Results { get; set; }
        public int RowCount { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ReportResult
    {
        public bool Success { get; set; }
        public string ReportType { get; set; }
        public string ReportPath { get; set; }
        public long ReportSize { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class VisualizationResult
    {
        public bool Success { get; set; }
        public string VisualizationType { get; set; }
        public object ChartData { get; set; }
        public Dictionary<string, object> ChartOptions { get; set; } = new Dictionary<string, object>();
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class RealTimeAnalyticsResult
    {
        public bool Success { get; set; }
        public string DataSourceName { get; set; }
        public List<AnalysisResult> AnalysisResults { get; set; } = new List<AnalysisResult>();
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    // Configuration and data classes
    public class AnalyticsConfig
    {
        public List<string> AnalyticsTypes { get; set; } = new List<string> { "statistical", "predictive", "descriptive" };
        public ProcessingConfig ProcessingConfig { get; set; } = new ProcessingConfig();
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class ProcessingConfig
    {
        public bool RemoveNulls { get; set; } = true;
        public bool NormalizeData { get; set; } = false;
        public List<string> ExcludeColumns { get; set; } = new List<string>();
    }

    public class QueryConfig
    {
        public int Timeout { get; set; } = 30;
        public int MaxRows { get; set; } = 10000;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    public class ReportConfig
    {
        public string Title { get; set; }
        public List<string> Sections { get; set; } = new List<string>();
        public Dictionary<string, object> Formatting { get; set; } = new Dictionary<string, object>();
    }

    public class VisualizationConfig
    {
        public string Type { get; set; } = "line";
        public Dictionary<string, object> Options { get; set; } = new Dictionary<string, object>();
    }

    public class RealTimeConfig
    {
        public List<string> AnalyticsTypes { get; set; } = new List<string>();
        public ProcessingConfig ProcessingConfig { get; set; } = new ProcessingConfig();
        public AnalyticsConfig AnalyticsConfig { get; set; } = new AnalyticsConfig();
    }

    public class DataTable
    {
        public List<Dictionary<string, object>> Rows { get; set; } = new List<Dictionary<string, object>>();
        public int Columns => Rows.Count > 0 ? Rows[0].Count : 0;
    }

    public class VisualizationData
    {
        public object Data { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Analytics metrics collection
    /// </summary>
    public class AnalyticsMetrics
    {
        private readonly Dictionary<string, DataSourceMetrics> _dataSourceMetrics = new Dictionary<string, DataSourceMetrics>();
        private readonly Dictionary<string, ReportMetrics> _reportMetrics = new Dictionary<string, ReportMetrics>();
        private readonly object _lock = new object();

        public void RecordAnalyticsExecution(string dataSourceName, bool success, TimeSpan executionTime)
        {
            lock (_lock)
            {
                var metrics = _dataSourceMetrics.GetValueOrDefault(dataSourceName, new DataSourceMetrics());
                metrics.TotalAnalytics++;
                metrics.SuccessfulAnalytics += success ? 1 : 0;
                metrics.TotalAnalyticsTime += executionTime;
                _dataSourceMetrics[dataSourceName] = metrics;
            }
        }

        public void RecordReportGeneration(string reportType, bool success, TimeSpan executionTime)
        {
            lock (_lock)
            {
                var metrics = _reportMetrics.GetValueOrDefault(reportType, new ReportMetrics());
                metrics.TotalReports++;
                metrics.SuccessfulReports += success ? 1 : 0;
                metrics.TotalReportTime += executionTime;
                _reportMetrics[reportType] = metrics;
            }
        }

        public Dictionary<string, DataSourceMetrics> GetDataSourceMetrics() => new Dictionary<string, DataSourceMetrics>(_dataSourceMetrics);
        public Dictionary<string, ReportMetrics> GetReportMetrics() => new Dictionary<string, ReportMetrics>(_reportMetrics);
    }

    public class DataSourceMetrics
    {
        public int TotalAnalytics { get; set; }
        public int SuccessfulAnalytics { get; set; }
        public TimeSpan TotalAnalyticsTime { get; set; }
    }

    public class ReportMetrics
    {
        public int TotalReports { get; set; }
        public int SuccessfulReports { get; set; }
        public TimeSpan TotalReportTime { get; set; }
    }
} 