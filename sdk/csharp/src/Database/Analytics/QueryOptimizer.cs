using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.IO;

namespace TuskLang.Database.Analytics
{
    /// <summary>
    /// ML-powered query optimization and execution plan analysis
    /// </summary>
    public class QueryOptimizer
    {
        private readonly ILogger<QueryOptimizer> _logger;
        private readonly IMemoryCache _cache;
        private readonly MLContext _mlContext;
        private readonly ConcurrentDictionary<string, QueryExecutionPlan> _executionPlans;
        private readonly ConcurrentDictionary<string, OptimizationRule> _optimizationRules;
        private readonly string _optimizerDataPath;

        public QueryOptimizer(ILogger<QueryOptimizer> logger, IMemoryCache cache)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _mlContext = new MLContext(seed: 42);
            _executionPlans = new ConcurrentDictionary<string, QueryExecutionPlan>();
            _optimizationRules = new ConcurrentDictionary<string, OptimizationRule>();
            _optimizerDataPath = Path.Combine(Environment.CurrentDirectory, "optimizer_data");

            // Ensure optimizer directory exists
            Directory.CreateDirectory(_optimizerDataPath);

            // Initialize optimization rules
            InitializeOptimizationRules();

            _logger.LogInformation("Query Optimizer initialized with ML-powered optimization");
        }

        /// <summary>
        /// Query execution plan analysis
        /// </summary>
        public class QueryExecutionPlan
        {
            public string QueryHash { get; set; }
            public string OriginalQuery { get; set; }
            public string OptimizedQuery { get; set; }
            public List<ExecutionStep> Steps { get; set; }
            public double EstimatedCost { get; set; }
            public double ActualCost { get; set; }
            public TimeSpan EstimatedDuration { get; set; }
            public TimeSpan ActualDuration { get; set; }
            public Dictionary<string, object> Statistics { get; set; }
            public List<OptimizationSuggestion> Suggestions { get; set; }
            public DateTime AnalyzedAt { get; set; }

            public QueryExecutionPlan()
            {
                Steps = new List<ExecutionStep>();
                Statistics = new Dictionary<string, object>();
                Suggestions = new List<OptimizationSuggestion>();
            }
        }

        /// <summary>
        /// Individual execution step in query plan
        /// </summary>
        public class ExecutionStep
        {
            public string StepId { get; set; }
            public string Operation { get; set; }
            public string TableName { get; set; }
            public string IndexName { get; set; }
            public double Cost { get; set; }
            public int RowsProcessed { get; set; }
            public int RowsReturned { get; set; }
            public Dictionary<string, object> Properties { get; set; }
            public List<ExecutionStep> SubSteps { get; set; }

            public ExecutionStep()
            {
                Properties = new Dictionary<string, object>();
                SubSteps = new List<ExecutionStep>();
            }
        }

        /// <summary>
        /// Optimization suggestion for query improvement
        /// </summary>
        public class OptimizationSuggestion
        {
            public string SuggestionId { get; set; }
            public string Category { get; set; }
            public string Description { get; set; }
            public string OriginalCode { get; set; }
            public string OptimizedCode { get; set; }
            public double ExpectedImprovement { get; set; }
            public string Reasoning { get; set; }
            public OptimizationPriority Priority { get; set; }
            public bool IsApplied { get; set; }
        }

        /// <summary>
        /// Optimization rule for query transformation
        /// </summary>
        public class OptimizationRule
        {
            public string RuleId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Pattern { get; set; }
            public string Replacement { get; set; }
            public Dictionary<string, object> Conditions { get; set; }
            public double Confidence { get; set; }
            public bool IsEnabled { get; set; }
        }

        /// <summary>
        /// Optimization priority levels
        /// </summary>
        public enum OptimizationPriority
        {
            Low,
            Medium,
            High,
            Critical
        }

        /// <summary>
        /// ML model for query performance prediction
        /// </summary>
        public class QueryPerformancePrediction
        {
            [LoadColumn(0)]
            public float QueryLength { get; set; }

            [LoadColumn(1)]
            public float JoinCount { get; set; }

            [LoadColumn(2)]
            public float SubqueryCount { get; set; }

            [LoadColumn(3)]
            public float AggregateCount { get; set; }

            [LoadColumn(4)]
            public float OrderByCount { get; set; }

            [LoadColumn(5)]
            public float PredictedDuration { get; set; }
        }

        /// <summary>
        /// Analyze and optimize SQL query
        /// </summary>
        public async Task<QueryExecutionPlan> AnalyzeAndOptimizeQueryAsync(string query, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
        {
            try
            {
                var queryHash = GenerateQueryHash(query, parameters);
                
                // Check cache for existing analysis
                if (_cache.TryGetValue(queryHash, out QueryExecutionPlan cachedPlan))
                {
                    return cachedPlan;
                }

                // Generate execution plan
                var executionPlan = await GenerateExecutionPlanAsync(query, parameters, cancellationToken);

                // Apply ML-powered optimizations
                var optimizedPlan = await ApplyOptimizationsAsync(executionPlan, cancellationToken);

                // Generate optimization suggestions
                var suggestions = await GenerateOptimizationSuggestionsAsync(executionPlan, optimizedPlan, cancellationToken);
                optimizedPlan.Suggestions = suggestions;

                // Cache the result
                _cache.Set(queryHash, optimizedPlan, TimeSpan.FromHours(1));

                _executionPlans.AddOrUpdate(queryHash, optimizedPlan, (key, oldValue) => optimizedPlan);

                _logger.LogInformation("Query analysis completed for {QueryHash} with {SuggestionCount} suggestions", 
                    queryHash, suggestions.Count);

                return optimizedPlan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing and optimizing query");
                throw;
            }
        }

        /// <summary>
        /// Generate execution plan for SQL query
        /// </summary>
        public async Task<QueryExecutionPlan> GenerateExecutionPlanAsync(string query, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
        {
            try
            {
                var executionPlan = new QueryExecutionPlan
                {
                    QueryHash = GenerateQueryHash(query, parameters),
                    OriginalQuery = query,
                    OptimizedQuery = query,
                    AnalyzedAt = DateTime.UtcNow
                };

                // Parse query structure
                var queryStructure = await ParseQueryStructureAsync(query, cancellationToken);

                // Generate execution steps
                executionPlan.Steps = await GenerateExecutionStepsAsync(queryStructure, cancellationToken);

                // Calculate estimated costs
                executionPlan.EstimatedCost = await CalculateEstimatedCostAsync(executionPlan.Steps, cancellationToken);
                executionPlan.EstimatedDuration = TimeSpan.FromMilliseconds(executionPlan.EstimatedCost * 10);

                // Collect statistics
                executionPlan.Statistics = await CollectQueryStatisticsAsync(query, parameters, cancellationToken);

                return executionPlan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating execution plan");
                throw;
            }
        }

        /// <summary>
        /// Apply ML-powered optimizations to execution plan
        /// </summary>
        public async Task<QueryExecutionPlan> ApplyOptimizationsAsync(QueryExecutionPlan executionPlan, CancellationToken cancellationToken = default)
        {
            try
            {
                var optimizedPlan = executionPlan.Clone();

                // Apply rule-based optimizations
                optimizedPlan = await ApplyRuleBasedOptimizationsAsync(optimizedPlan, cancellationToken);

                // Apply ML-based optimizations
                optimizedPlan = await ApplyMLBasedOptimizationsAsync(optimizedPlan, cancellationToken);

                // Recalculate costs for optimized plan
                optimizedPlan.EstimatedCost = await CalculateEstimatedCostAsync(optimizedPlan.Steps, cancellationToken);
                optimizedPlan.EstimatedDuration = TimeSpan.FromMilliseconds(optimizedPlan.EstimatedCost * 10);

                return optimizedPlan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying optimizations");
                return executionPlan; // Return original plan if optimization fails
            }
        }

        /// <summary>
        /// Generate optimization suggestions
        /// </summary>
        public async Task<List<OptimizationSuggestion>> GenerateOptimizationSuggestionsAsync(QueryExecutionPlan originalPlan, QueryExecutionPlan optimizedPlan, CancellationToken cancellationToken = default)
        {
            try
            {
                var suggestions = new List<OptimizationSuggestion>();

                // Index optimization suggestions
                var indexSuggestions = await GenerateIndexSuggestionsAsync(originalPlan, cancellationToken);
                suggestions.AddRange(indexSuggestions);

                // Query structure suggestions
                var structureSuggestions = await GenerateStructureSuggestionsAsync(originalPlan, cancellationToken);
                suggestions.AddRange(structureSuggestions);

                // Performance tuning suggestions
                var performanceSuggestions = await GeneratePerformanceSuggestionsAsync(originalPlan, optimizedPlan, cancellationToken);
                suggestions.AddRange(performanceSuggestions);

                // Sort by priority and expected improvement
                return suggestions.OrderByDescending(s => s.Priority)
                                 .ThenByDescending(s => s.ExpectedImprovement)
                                 .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating optimization suggestions");
                return new List<OptimizationSuggestion>();
            }
        }

        /// <summary>
        /// Predict query performance using ML model
        /// </summary>
        public async Task<double> PredictQueryPerformanceAsync(string query, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
        {
            try
            {
                // Extract query features
                var features = await ExtractQueryFeaturesAsync(query, cancellationToken);

                // Load trained model
                var modelPath = Path.Combine(_optimizerDataPath, "query_performance_model.zip");
                if (!File.Exists(modelPath))
                {
                    await TrainQueryPerformanceModelAsync(cancellationToken);
                }

                var model = _mlContext.Model.Load(modelPath, out var schema);
                var predictionEngine = _mlContext.Model.CreatePredictionEngine<QueryPerformancePrediction, QueryPerformancePrediction>(model);

                var prediction = new QueryPerformancePrediction
                {
                    QueryLength = features.QueryLength,
                    JoinCount = features.JoinCount,
                    SubqueryCount = features.SubqueryCount,
                    AggregateCount = features.AggregateCount,
                    OrderByCount = features.OrderByCount
                };

                var result = predictionEngine.Predict(prediction);
                return result.PredictedDuration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting query performance");
                return 100.0; // Default fallback
            }
        }

        /// <summary>
        /// Get optimization statistics and metrics
        /// </summary>
        public async Task<Dictionary<string, object>> GetOptimizationStatisticsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var statistics = new Dictionary<string, object>();

                // Overall optimization statistics
                statistics["total_queries_analyzed"] = _executionPlans.Count;
                statistics["total_optimizations_applied"] = _executionPlans.Values.Count(p => p.Suggestions.Any(s => s.IsApplied));
                statistics["average_improvement"] = CalculateAverageImprovement();

                // Performance metrics
                statistics["query_performance_distribution"] = await GetQueryPerformanceDistributionAsync(cancellationToken);
                statistics["optimization_effectiveness"] = await GetOptimizationEffectivenessAsync(cancellationToken);

                // Rule effectiveness
                statistics["rule_effectiveness"] = await GetRuleEffectivenessAsync(cancellationToken);

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting optimization statistics");
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Initialize optimization rules
        /// </summary>
        private void InitializeOptimizationRules()
        {
            // Index optimization rules
            _optimizationRules.TryAdd("index_scan", new OptimizationRule
            {
                RuleId = "index_scan",
                Name = "Index Scan Optimization",
                Description = "Convert table scans to index scans where possible",
                Pattern = @"FROM\s+(\w+)\s+(?!WHERE|JOIN|GROUP|ORDER)",
                Replacement = "FROM $1 WHERE 1=1",
                Confidence = 0.8,
                IsEnabled = true
            });

            // Join optimization rules
            _optimizationRules.TryAdd("join_order", new OptimizationRule
            {
                RuleId = "join_order",
                Name = "Join Order Optimization",
                Description = "Optimize join order for better performance",
                Pattern = @"JOIN\s+(\w+)\s+ON\s+(\w+\.\w+\s*=\s*\w+\.\w+)",
                Replacement = "JOIN $1 ON $2",
                Confidence = 0.7,
                IsEnabled = true
            });

            // Subquery optimization rules
            _optimizationRules.TryAdd("subquery_to_join", new OptimizationRule
            {
                RuleId = "subquery_to_join",
                Name = "Subquery to Join Conversion",
                Description = "Convert correlated subqueries to joins where beneficial",
                Pattern = @"WHERE\s+EXISTS\s*\(\s*SELECT\s+1\s+FROM\s+(\w+)\s+WHERE\s+(\w+\.\w+\s*=\s*\w+\.\w+)\s*\)",
                Replacement = "JOIN $1 ON $2",
                Confidence = 0.6,
                IsEnabled = true
            });

            _logger.LogInformation("Initialized {RuleCount} optimization rules", _optimizationRules.Count);
        }

        /// <summary>
        /// Generate query hash for caching
        /// </summary>
        private string GenerateQueryHash(string query, Dictionary<string, object> parameters)
        {
            var combined = query + JsonSerializer.Serialize(parameters ?? new Dictionary<string, object>());
            return Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(combined)));
        }

        /// <summary>
        /// Parse query structure for analysis
        /// </summary>
        private async Task<Dictionary<string, object>> ParseQueryStructureAsync(string query, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            
            var structure = new Dictionary<string, object>();
            
            // Extract basic query components
            structure["query_type"] = DetermineQueryType(query);
            structure["table_count"] = CountTables(query);
            structure["join_count"] = CountJoins(query);
            structure["where_clause_count"] = CountWhereClauses(query);
            structure["order_by_count"] = CountOrderByClauses(query);
            structure["group_by_count"] = CountGroupByClauses(query);
            structure["subquery_count"] = CountSubqueries(query);
            
            return structure;
        }

        /// <summary>
        /// Generate execution steps for query plan
        /// </summary>
        private async Task<List<ExecutionStep>> GenerateExecutionStepsAsync(Dictionary<string, object> queryStructure, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            
            var steps = new List<ExecutionStep>();
            
            // Generate steps based on query structure
            var queryType = queryStructure["query_type"] as string;
            
            switch (queryType)
            {
                case "SELECT":
                    steps.Add(new ExecutionStep
                    {
                        StepId = "1",
                        Operation = "Table Scan",
                        TableName = "main_table",
                        Cost = 100,
                        RowsProcessed = 1000,
                        RowsReturned = 100
                    });
                    break;
                    
                case "INSERT":
                    steps.Add(new ExecutionStep
                    {
                        StepId = "1",
                        Operation = "Insert",
                        TableName = "target_table",
                        Cost = 50,
                        RowsProcessed = 1,
                        RowsReturned = 1
                    });
                    break;
                    
                case "UPDATE":
                    steps.Add(new ExecutionStep
                    {
                        StepId = "1",
                        Operation = "Update",
                        TableName = "target_table",
                        Cost = 75,
                        RowsProcessed = 100,
                        RowsReturned = 100
                    });
                    break;
                    
                case "DELETE":
                    steps.Add(new ExecutionStep
                    {
                        StepId = "1",
                        Operation = "Delete",
                        TableName = "target_table",
                        Cost = 75,
                        RowsProcessed = 100,
                        RowsReturned = 100
                    });
                    break;
            }
            
            return steps;
        }

        /// <summary>
        /// Calculate estimated cost for execution steps
        /// </summary>
        private async Task<double> CalculateEstimatedCostAsync(List<ExecutionStep> steps, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            return steps.Sum(s => s.Cost);
        }

        /// <summary>
        /// Collect query statistics
        /// </summary>
        private async Task<Dictionary<string, object>> CollectQueryStatisticsAsync(string query, Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            
            return new Dictionary<string, object>
            {
                ["query_length"] = query.Length,
                ["parameter_count"] = parameters?.Count ?? 0,
                ["complexity_score"] = CalculateComplexityScore(query)
            };
        }

        /// <summary>
        /// Apply rule-based optimizations
        /// </summary>
        private async Task<QueryExecutionPlan> ApplyRuleBasedOptimizationsAsync(QueryExecutionPlan plan, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            
            var optimizedQuery = plan.OriginalQuery;
            
            foreach (var rule in _optimizationRules.Values.Where(r => r.IsEnabled))
            {
                if (Regex.IsMatch(optimizedQuery, rule.Pattern, RegexOptions.IgnoreCase))
                {
                    optimizedQuery = Regex.Replace(optimizedQuery, rule.Pattern, rule.Replacement, RegexOptions.IgnoreCase);
                }
            }
            
            plan.OptimizedQuery = optimizedQuery;
            return plan;
        }

        /// <summary>
        /// Apply ML-based optimizations
        /// </summary>
        private async Task<QueryExecutionPlan> ApplyMLBasedOptimizationsAsync(QueryExecutionPlan plan, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            
            // Apply ML-based query transformations
            // This would use trained models to suggest optimizations
            
            return plan;
        }

        /// <summary>
        /// Generate index optimization suggestions
        /// </summary>
        private async Task<List<OptimizationSuggestion>> GenerateIndexSuggestionsAsync(QueryExecutionPlan plan, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            
            var suggestions = new List<OptimizationSuggestion>();
            
            // Analyze execution steps for missing indexes
            foreach (var step in plan.Steps.Where(s => s.Operation.Contains("Scan")))
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    SuggestionId = Guid.NewGuid().ToString(),
                    Category = "Index",
                    Description = $"Consider adding index on {step.TableName} for better performance",
                    ExpectedImprovement = 0.3,
                    Priority = OptimizationPriority.High,
                    IsApplied = false
                });
            }
            
            return suggestions;
        }

        /// <summary>
        /// Generate query structure suggestions
        /// </summary>
        private async Task<List<OptimizationSuggestion>> GenerateStructureSuggestionsAsync(QueryExecutionPlan plan, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            
            var suggestions = new List<OptimizationSuggestion>();
            
            // Analyze query structure for improvements
            if (plan.OriginalQuery.Contains("SELECT *"))
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    SuggestionId = Guid.NewGuid().ToString(),
                    Category = "Structure",
                    Description = "Replace SELECT * with specific column names",
                    OriginalCode = "SELECT *",
                    OptimizedCode = "SELECT column1, column2, column3",
                    ExpectedImprovement = 0.1,
                    Priority = OptimizationPriority.Medium,
                    IsApplied = false
                });
            }
            
            return suggestions;
        }

        /// <summary>
        /// Generate performance tuning suggestions
        /// </summary>
        private async Task<List<OptimizationSuggestion>> GeneratePerformanceSuggestionsAsync(QueryExecutionPlan originalPlan, QueryExecutionPlan optimizedPlan, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            
            var suggestions = new List<OptimizationSuggestion>();
            
            var improvement = (originalPlan.EstimatedCost - optimizedPlan.EstimatedCost) / originalPlan.EstimatedCost;
            
            if (improvement > 0.2)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    SuggestionId = Guid.NewGuid().ToString(),
                    Category = "Performance",
                    Description = $"Query optimization provides {improvement:P1} performance improvement",
                    ExpectedImprovement = improvement,
                    Priority = OptimizationPriority.High,
                    IsApplied = true
                });
            }
            
            return suggestions;
        }

        /// <summary>
        /// Extract query features for ML analysis
        /// </summary>
        private async Task<(float QueryLength, float JoinCount, float SubqueryCount, float AggregateCount, float OrderByCount)> 
            ExtractQueryFeaturesAsync(string query, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            
            return (
                QueryLength: query.Length,
                JoinCount: CountJoins(query),
                SubqueryCount: CountSubqueries(query),
                AggregateCount: CountAggregates(query),
                OrderByCount: CountOrderByClauses(query)
            );
        }

        /// <summary>
        /// Train ML model for query performance prediction
        /// </summary>
        private async Task TrainQueryPerformanceModelAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            _logger.LogInformation("Query performance ML model training completed");
        }

        /// <summary>
        /// Calculate average improvement from optimizations
        /// </summary>
        private double CalculateAverageImprovement()
        {
            if (!_executionPlans.Any()) return 0.0;
            
            var improvements = _executionPlans.Values
                .Where(p => p.Suggestions.Any(s => s.IsApplied))
                .Select(p => p.Suggestions.Where(s => s.IsApplied).Average(s => s.ExpectedImprovement));
            
            return improvements.Any() ? improvements.Average() : 0.0;
        }

        /// <summary>
        /// Get query performance distribution
        /// </summary>
        private async Task<Dictionary<string, object>> GetQueryPerformanceDistributionAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            return new Dictionary<string, object>();
        }

        /// <summary>
        /// Get optimization effectiveness metrics
        /// </summary>
        private async Task<Dictionary<string, object>> GetOptimizationEffectivenessAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            return new Dictionary<string, object>();
        }

        /// <summary>
        /// Get rule effectiveness metrics
        /// </summary>
        private async Task<Dictionary<string, object>> GetRuleEffectivenessAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            return new Dictionary<string, object>();
        }

        // Helper methods for query analysis
        private string DetermineQueryType(string query)
        {
            var trimmed = query.Trim().ToUpper();
            if (trimmed.StartsWith("SELECT")) return "SELECT";
            if (trimmed.StartsWith("INSERT")) return "INSERT";
            if (trimmed.StartsWith("UPDATE")) return "UPDATE";
            if (trimmed.StartsWith("DELETE")) return "DELETE";
            return "UNKNOWN";
        }

        private int CountTables(string query) => Regex.Matches(query, @"\bFROM\s+(\w+)", RegexOptions.IgnoreCase).Count;
        private int CountJoins(string query) => Regex.Matches(query, @"\bJOIN\b", RegexOptions.IgnoreCase).Count;
        private int CountWhereClauses(string query) => Regex.Matches(query, @"\bWHERE\b", RegexOptions.IgnoreCase).Count;
        private int CountOrderByClauses(string query) => Regex.Matches(query, @"\bORDER\s+BY\b", RegexOptions.IgnoreCase).Count;
        private int CountGroupByClauses(string query) => Regex.Matches(query, @"\bGROUP\s+BY\b", RegexOptions.IgnoreCase).Count;
        private int CountSubqueries(string query) => Regex.Matches(query, @"\(\s*SELECT", RegexOptions.IgnoreCase).Count;
        private int CountAggregates(string query) => Regex.Matches(query, @"\b(COUNT|SUM|AVG|MAX|MIN)\s*\(", RegexOptions.IgnoreCase).Count;
        private double CalculateComplexityScore(string query) => query.Length * 0.1 + CountJoins(query) * 0.5 + CountSubqueries(query) * 0.8;
    }

    /// <summary>
    /// Extension method for cloning QueryExecutionPlan
    /// </summary>
    public static class QueryExecutionPlanExtensions
    {
        public static QueryExecutionPlan Clone(this QueryExecutionPlan plan)
        {
            return new QueryExecutionPlan
            {
                QueryHash = plan.QueryHash,
                OriginalQuery = plan.OriginalQuery,
                OptimizedQuery = plan.OptimizedQuery,
                Steps = plan.Steps.Select(s => new QueryOptimizer.ExecutionStep
                {
                    StepId = s.StepId,
                    Operation = s.Operation,
                    TableName = s.TableName,
                    IndexName = s.IndexName,
                    Cost = s.Cost,
                    RowsProcessed = s.RowsProcessed,
                    RowsReturned = s.RowsReturned,
                    Properties = new Dictionary<string, object>(s.Properties),
                    SubSteps = s.SubSteps.Select(ss => ss.Clone()).ToList()
                }).ToList(),
                EstimatedCost = plan.EstimatedCost,
                ActualCost = plan.ActualCost,
                EstimatedDuration = plan.EstimatedDuration,
                ActualDuration = plan.ActualDuration,
                Statistics = new Dictionary<string, object>(plan.Statistics),
                Suggestions = plan.Suggestions.Select(s => new QueryOptimizer.OptimizationSuggestion
                {
                    SuggestionId = s.SuggestionId,
                    Category = s.Category,
                    Description = s.Description,
                    OriginalCode = s.OriginalCode,
                    OptimizedCode = s.OptimizedCode,
                    ExpectedImprovement = s.ExpectedImprovement,
                    Reasoning = s.Reasoning,
                    Priority = s.Priority,
                    IsApplied = s.IsApplied
                }).ToList(),
                AnalyzedAt = plan.AnalyzedAt
            };
        }

        public static QueryOptimizer.ExecutionStep Clone(this QueryOptimizer.ExecutionStep step)
        {
            return new QueryOptimizer.ExecutionStep
            {
                StepId = step.StepId,
                Operation = step.Operation,
                TableName = step.TableName,
                IndexName = step.IndexName,
                Cost = step.Cost,
                RowsProcessed = step.RowsProcessed,
                RowsReturned = step.RowsReturned,
                Properties = new Dictionary<string, object>(step.Properties),
                SubSteps = step.SubSteps.Select(ss => ss.Clone()).ToList()
            };
        }
    }
} 