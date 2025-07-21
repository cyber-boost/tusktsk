using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace TuskLang
{
    /// <summary>
    /// Integration example demonstrating all three goal g4 implementations working together
    /// Shows TestingQualityAssurance, DeploymentCICD, and MonitoringAlerting in action
    /// </summary>
    public class GoalG4IntegrationExample
    {
        private readonly TestingQualityAssurance _testing;
        private readonly DeploymentCICD _deployment;
        private readonly MonitoringAlerting _monitoring;
        private readonly TSK _tsk;

        public GoalG4IntegrationExample()
        {
            _testing = new TestingQualityAssurance();
            _deployment = new DeploymentCICD();
            _monitoring = new MonitoringAlerting();
            _tsk = new TSK();
        }

        /// <summary>
        /// Execute a comprehensive quality assurance and deployment workflow
        /// </summary>
        public async Task<G4IntegrationResult> ExecuteComprehensiveQAWorkflow(
            Dictionary<string, object> inputs,
            string operationName = "comprehensive_qa_workflow")
        {
            var result = new G4IntegrationResult();
            var startTime = DateTime.UtcNow;

            try
            {
                // Step 1: Set up test suites and monitors
                await SetupTestSuitesAndMonitors();

                // Step 2: Run comprehensive testing
                var testResult = await _testing.RunAllTestsAsync(new TestExecutionConfig
                {
                    StopOnFirstFailure = false,
                    PerformanceThreshold = TimeSpan.FromSeconds(2)
                });
                result.TestingResults = testResult;

                // Step 3: Analyze code quality
                if (inputs.ContainsKey("assembly_path"))
                {
                    var qualityResult = await _testing.AnalyzeCodeQualityAsync(inputs["assembly_path"].ToString());
                    result.QualityResults = qualityResult;
                }

                // Step 4: Generate code coverage
                if (inputs.ContainsKey("test_assemblies"))
                {
                    var testAssemblies = inputs["test_assemblies"] as List<string> ?? new List<string>();
                    var coverageResult = await _testing.GenerateCodeCoverageAsync(
                        inputs.GetValueOrDefault("assembly_path", "sdk/csharp/TuskLang.dll").ToString(),
                        testAssemblies);
                    result.CoverageResults = coverageResult;
                }

                // Step 5: Execute CI/CD pipeline
                if (inputs.ContainsKey("pipeline_name"))
                {
                    var pipelineName = inputs["pipeline_name"].ToString();
                    var pipelineResult = await _deployment.ExecutePipelineAsync(pipelineName, inputs);
                    result.PipelineResults = pipelineResult;
                }

                // Step 6: Run complete CI/CD workflow
                if (inputs.ContainsKey("project_path"))
                {
                    var workflowResult = await _deployment.RunCICDWorkflowAsync(
                        inputs["project_path"].ToString(),
                        new CICDWorkflowConfig
                        {
                            RunTests = true,
                            BuildConfig = new BuildConfig { Configuration = "Release" },
                            DeploymentConfig = new DeploymentConfig { TargetEnvironment = "production" }
                        });
                    result.WorkflowResults = workflowResult;
                }

                // Step 7: Start monitoring and collect metrics
                var monitoringResult = await _monitoring.StartMonitoringAsync();
                result.MonitoringResults = monitoringResult;

                var metricsResult = await _monitoring.CollectMetricsAsync();
                result.MetricsResults = metricsResult;

                // Step 8: Perform health checks
                var healthResult = await _monitoring.PerformHealthCheckAsync("tusklang-sdk");
                result.HealthResults = healthResult;

                // Step 9: Send alerts if needed
                if (!testResult.Success || !monitoringResult.Success)
                {
                    var alert = new Alert
                    {
                        Title = "QA Workflow Alert",
                        Message = $"QA workflow completed with issues. Tests: {testResult.Success}, Monitoring: {monitoringResult.Success}",
                        Severity = AlertSeverity.Warning,
                        Timestamp = DateTime.UtcNow
                    };

                    var alertResult = await _monitoring.SendAlertAsync(alert);
                    result.AlertResults = alertResult;
                }

                // Step 10: Execute FUJSEN with QA context
                if (inputs.ContainsKey("fujsen_code"))
                {
                    var fujsenResult = await ExecuteFujsenWithQAContext(
                        inputs["fujsen_code"].ToString(),
                        inputs);
                    result.FujsenResults = fujsenResult;
                }

                result.Success = true;
                result.ExecutionTime = DateTime.UtcNow - startTime;

                // Step 11: Collect metrics from all systems
                result.Metrics = new G4IntegrationMetrics
                {
                    TestMetrics = _testing.GetMetrics(),
                    PipelineMetrics = _deployment.GetMetrics(),
                    MonitoringMetrics = _monitoring.GetMetrics()
                };

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"QA workflow failed: {ex.Message}");
                result.ExecutionTime = DateTime.UtcNow - startTime;

                return result;
            }
        }

        /// <summary>
        /// Set up test suites and monitors
        /// </summary>
        private async Task SetupTestSuitesAndMonitors()
        {
            // Register test suites
            var unitTestSuite = new TestSuite
            {
                Name = "Unit Tests",
                Tests = new List<ITest>
                {
                    new SimpleTest("Test1", "Basic functionality test"),
                    new SimpleTest("Test2", "Error handling test"),
                    new SimpleTest("Test3", "Performance test")
                },
                Config = new TestSuiteConfig { Type = TestType.Unit }
            };

            var integrationTestSuite = new TestSuite
            {
                Name = "Integration Tests",
                Tests = new List<ITest>
                {
                    new SimpleTest("Integration1", "Database integration test"),
                    new SimpleTest("Integration2", "API integration test"),
                    new SimpleTest("Integration3", "Service integration test")
                },
                Config = new TestSuiteConfig { Type = TestType.Integration }
            };

            _testing.RegisterTestSuite("unit-tests", unitTestSuite);
            _testing.RegisterTestSuite("integration-tests", integrationTestSuite);

            // Register monitors
            _monitoring.RegisterMonitor("system-monitor", new SystemMonitor());
            _monitoring.RegisterMonitor("application-monitor", new ApplicationMonitor());

            await Task.CompletedTask;
        }

        /// <summary>
        /// Execute FUJSEN with QA context
        /// </summary>
        private async Task<FujsenOperationResult> ExecuteFujsenWithQAContext(
            string fujsenCode,
            Dictionary<string, object> context)
        {
            try
            {
                // Set up TSK with the FUJSEN code and QA context
                _tsk.SetSection("qa_section", new Dictionary<string, object>
                {
                    ["fujsen"] = fujsenCode,
                    ["testing"] = _testing,
                    ["deployment"] = _deployment,
                    ["monitoring"] = _monitoring
                });

                // Execute with context injection
                var result = await _tsk.ExecuteFujsenWithContext("qa_section", "fujsen", context);

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
        /// Get comprehensive QA health report
        /// </summary>
        public async Task<G4SystemHealthReport> GetQAHealthReport()
        {
            var testMetrics = _testing.GetMetrics();
            var pipelineMetrics = _deployment.GetMetrics();
            var monitoringMetrics = _monitoring.GetMetrics();

            return new G4SystemHealthReport
            {
                Timestamp = DateTime.UtcNow,
                TestSuites = _testing.GetTestSuiteNames(),
                Pipelines = _deployment.GetPipelineNames(),
                Monitors = _monitoring.GetMonitorNames(),
                OverallHealth = CalculateG4OverallHealth(testMetrics, pipelineMetrics, monitoringMetrics)
            };
        }

        /// <summary>
        /// Execute batch QA operations
        /// </summary>
        public async Task<List<G4IntegrationResult>> ExecuteBatchQAOperations(
            List<Dictionary<string, object>> inputsList)
        {
            var tasks = inputsList.Select(inputs => ExecuteComprehensiveQAWorkflow(inputs)).ToList();
            return await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Get QA registry summary
        /// </summary>
        public async Task<QARegistrySummary> GetQARegistrySummary()
        {
            return new QARegistrySummary
            {
                TestSuiteNames = _testing.GetTestSuiteNames(),
                PipelineNames = _deployment.GetPipelineNames(),
                MonitorNames = _monitoring.GetMonitorNames()
            };
        }

        private G4SystemHealth CalculateG4OverallHealth(
            TestMetrics testMetrics,
            PipelineMetrics pipelineMetrics,
            MonitoringMetrics monitoringMetrics)
        {
            // Calculate health based on various metrics
            var testHealthScore = testMetrics.GetMetrics().Values.Count(m => m.SuccessfulExecutions > 0) / 
                                 (double)testMetrics.GetMetrics().Count;
            var pipelineHealthScore = pipelineMetrics.GetMetrics().Values.Count(m => m.SuccessfulExecutions > 0) / 
                                     (double)pipelineMetrics.GetMetrics().Count;
            var monitoringHealthScore = monitoringMetrics.GetMonitoringSessions().GetValueOrDefault("successful", 0) / 
                                       (double)(monitoringMetrics.GetMonitoringSessions().GetValueOrDefault("successful", 0) + 
                                               monitoringMetrics.GetMonitoringSessions().GetValueOrDefault("failed", 0) + 1);

            var overallHealth = (testHealthScore + pipelineHealthScore + monitoringHealthScore) / 3.0;

            if (overallHealth > 0.9)
                return G4SystemHealth.Excellent;
            else if (overallHealth > 0.7)
                return G4SystemHealth.Good;
            else if (overallHealth > 0.5)
                return G4SystemHealth.Fair;
            else
                return G4SystemHealth.Poor;
        }
    }

    /// <summary>
    /// Simple test implementation for demonstration
    /// </summary>
    public class SimpleTest : ITest
    {
        public string Name { get; }
        public string Description { get; }

        public SimpleTest(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public async Task<TestResult> ExecuteAsync()
        {
            await Task.Delay(50); // Simulate test execution

            var success = new Random().Next(0, 10) > 2; // 80% success rate

            return new TestResult
            {
                TestName = Name,
                Success = success,
                ExecutionTime = TimeSpan.FromMilliseconds(50),
                ErrorMessage = success ? null : "Test failed"
            };
        }
    }

    /// <summary>
    /// Test suite implementation
    /// </summary>
    public class TestSuite : ITestSuite
    {
        public string Name { get; set; }
        public List<ITest> Tests { get; set; } = new List<ITest>();
        public TestSuiteConfig Config { get; set; } = new TestSuiteConfig();
    }

    public class G4IntegrationResult
    {
        public bool Success { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public TimeSpan ExecutionTime { get; set; }
        public TestExecutionResult TestingResults { get; set; }
        public QualityAnalysisResult QualityResults { get; set; }
        public CodeCoverageResult CoverageResults { get; set; }
        public PipelineExecutionResult PipelineResults { get; set; }
        public CICDWorkflowResult WorkflowResults { get; set; }
        public MonitoringResult MonitoringResults { get; set; }
        public MetricsCollectionResult MetricsResults { get; set; }
        public HealthCheckResult HealthResults { get; set; }
        public AlertResult AlertResults { get; set; }
        public FujsenOperationResult FujsenResults { get; set; }
        public G4IntegrationMetrics Metrics { get; set; }
    }

    public class FujsenOperationResult
    {
        public bool Success { get; set; }
        public object Result { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class G4IntegrationMetrics
    {
        public TestMetrics TestMetrics { get; set; }
        public PipelineMetrics PipelineMetrics { get; set; }
        public MonitoringMetrics MonitoringMetrics { get; set; }
    }

    public class G4SystemHealthReport
    {
        public DateTime Timestamp { get; set; }
        public List<string> TestSuites { get; set; }
        public List<string> Pipelines { get; set; }
        public List<string> Monitors { get; set; }
        public G4SystemHealth OverallHealth { get; set; }
    }

    public class QARegistrySummary
    {
        public List<string> TestSuiteNames { get; set; }
        public List<string> PipelineNames { get; set; }
        public List<string> MonitorNames { get; set; }
    }

    public enum G4SystemHealth
    {
        Poor,
        Fair,
        Good,
        Excellent
    }
} 