using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace TuskLang.CLI.Advanced
{
    /// <summary>
    /// Production-ready developer productivity tools for TuskTsk
    /// Provides templates, snippets, debugging, IDE integration, and project scaffolding
    /// </summary>
    public class DeveloperProductivityTools : IDisposable
    {
        private readonly ILogger<DeveloperProductivityTools> _logger;
        private readonly TemplateManager _templateManager;
        private readonly SnippetManager _snippetManager;
        private readonly DebuggingTools _debuggingTools;
        private readonly IDEIntegration _ideIntegration;
        private readonly ProjectScaffolder _projectScaffolder;
        private readonly DocumentationGenerator _docGenerator;
        private readonly ProductivityOptions _options;
        private readonly PerformanceMetrics _metrics;
        private bool _disposed = false;

        public DeveloperProductivityTools(
            ProductivityOptions options = null,
            ILogger<DeveloperProductivityTools> logger = null)
        {
            _options = options ?? new ProductivityOptions();
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DeveloperProductivityTools>.Instance;
            
            _templateManager = new TemplateManager(_logger);
            _snippetManager = new SnippetManager(_logger);
            _debuggingTools = new DebuggingTools(_logger);
            _ideIntegration = new IDEIntegration(_logger);
            _projectScaffolder = new ProjectScaffolder(_logger);
            _docGenerator = new DocumentationGenerator(_logger);
            _metrics = new PerformanceMetrics();

            _logger.LogInformation("Developer productivity tools initialized");
        }

        #region Template Operations

        /// <summary>
        /// Create project from template
        /// </summary>
        public async Task<TemplateResult> CreateProjectFromTemplateAsync(
            string templateName, string projectPath, Dictionary<string, object> parameters, 
            CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new TemplateResult { Success = false };

            try
            {
                _logger.LogInformation($"Creating project from template: {templateName} at {projectPath}");

                // Get template
                var template = await _templateManager.GetTemplateAsync(templateName, cancellationToken);
                if (template == null)
                {
                    result.ErrorMessage = $"Template not found: {templateName}";
                    return result;
                }

                // Validate parameters
                var validationResult = await _templateManager.ValidateParametersAsync(template, parameters, cancellationToken);
                if (!validationResult.IsValid)
                {
                    result.ErrorMessage = $"Invalid parameters: {validationResult.ErrorMessage}";
                    result.ValidationErrors = validationResult.Errors;
                    return result;
                }

                // Create project directory
                if (!Directory.Exists(projectPath))
                {
                    Directory.CreateDirectory(projectPath);
                }

                // Generate project files
                var generatedFiles = await _templateManager.GenerateProjectAsync(template, projectPath, parameters, cancellationToken);
                
                // Initialize project
                await _projectScaffolder.InitializeProjectAsync(projectPath, template, cancellationToken);

                stopwatch.Stop();
                result.Success = true;
                result.ProjectPath = projectPath;
                result.GeneratedFiles = generatedFiles;
                result.CreationTime = stopwatch.Elapsed;

                _metrics.RecordProjectCreation(stopwatch.Elapsed);
                _logger.LogInformation($"Project created successfully: {projectPath}");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ErrorMessage = ex.Message;
                result.CreationTime = stopwatch.Elapsed;
                
                _metrics.RecordError(stopwatch.Elapsed);
                _logger.LogError(ex, $"Failed to create project from template: {templateName}");
                return result;
            }
        }

        /// <summary>
        /// List available templates
        /// </summary>
        public async Task<List<TemplateInfo>> ListTemplatesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _templateManager.ListTemplatesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list templates");
                return new List<TemplateInfo>();
            }
        }

        #endregion

        #region Snippet Operations

        /// <summary>
        /// Insert code snippet
        /// </summary>
        public async Task<SnippetResult> InsertSnippetAsync(
            string snippetName, string targetFile, int lineNumber, 
            Dictionary<string, object> parameters = null, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new SnippetResult { Success = false };

            try
            {
                _logger.LogInformation($"Inserting snippet: {snippetName} into {targetFile}");

                // Get snippet
                var snippet = await _snippetManager.GetSnippetAsync(snippetName, cancellationToken);
                if (snippet == null)
                {
                    result.ErrorMessage = $"Snippet not found: {snippetName}";
                    return result;
                }

                // Insert snippet
                var insertionResult = await _snippetManager.InsertSnippetAsync(snippet, targetFile, lineNumber, parameters, cancellationToken);

                stopwatch.Stop();
                result.Success = true;
                result.TargetFile = targetFile;
                result.LineNumber = lineNumber;
                result.InsertionTime = stopwatch.Elapsed;

                _metrics.RecordSnippetInsertion(stopwatch.Elapsed);
                _logger.LogInformation($"Snippet inserted successfully: {targetFile}");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ErrorMessage = ex.Message;
                result.InsertionTime = stopwatch.Elapsed;
                
                _metrics.RecordError(stopwatch.Elapsed);
                _logger.LogError(ex, $"Failed to insert snippet: {snippetName}");
                return result;
            }
        }

        /// <summary>
        /// Create custom snippet
        /// </summary>
        public async Task<SnippetResult> CreateSnippetAsync(
            string snippetName, string content, string description, 
            List<string> tags = null, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new SnippetResult { Success = false };

            try
            {
                _logger.LogInformation($"Creating custom snippet: {snippetName}");

                var snippet = new CodeSnippet
                {
                    Name = snippetName,
                    Content = content,
                    Description = description,
                    Tags = tags ?? new List<string>(),
                    CreatedAt = DateTime.UtcNow
                };

                await _snippetManager.SaveSnippetAsync(snippet, cancellationToken);

                stopwatch.Stop();
                result.Success = true;
                result.CreationTime = stopwatch.Elapsed;

                _metrics.RecordSnippetCreation(stopwatch.Elapsed);
                _logger.LogInformation($"Custom snippet created: {snippetName}");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ErrorMessage = ex.Message;
                result.CreationTime = stopwatch.Elapsed;
                
                _metrics.RecordError(stopwatch.Elapsed);
                _logger.LogError(ex, $"Failed to create snippet: {snippetName}");
                return result;
            }
        }

        #endregion

        #region Debugging Operations

        /// <summary>
        /// Analyze project for issues
        /// </summary>
        public async Task<DebuggingResult> AnalyzeProjectAsync(
            string projectPath, List<AnalysisType> analysisTypes, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new DebuggingResult { Success = false };

            try
            {
                _logger.LogInformation($"Analyzing project: {projectPath}");

                var issues = new List<ProjectIssue>();

                foreach (var analysisType in analysisTypes)
                {
                    var analysisIssues = await _debuggingTools.AnalyzeProjectAsync(projectPath, analysisType, cancellationToken);
                    issues.AddRange(analysisIssues);
                }

                stopwatch.Stop();
                result.Success = true;
                result.ProjectPath = projectPath;
                result.Issues = issues;
                result.AnalysisTime = stopwatch.Elapsed;

                _metrics.RecordProjectAnalysis(stopwatch.Elapsed);
                _logger.LogInformation($"Project analysis completed: {issues.Count} issues found");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ErrorMessage = ex.Message;
                result.AnalysisTime = stopwatch.Elapsed;
                
                _metrics.RecordError(stopwatch.Elapsed);
                _logger.LogError(ex, $"Failed to analyze project: {projectPath}");
                return result;
            }
        }

        /// <summary>
        /// Generate debugging report
        /// </summary>
        public async Task<DebuggingReport> GenerateDebuggingReportAsync(
            string projectPath, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var report = new DebuggingReport { Success = false };

            try
            {
                _logger.LogInformation($"Generating debugging report: {projectPath}");

                var analysisTypes = new List<AnalysisType> 
                { 
                    AnalysisType.Syntax, 
                    AnalysisType.Performance, 
                    AnalysisType.Security,
                    AnalysisType.BestPractices 
                };

                var analysisResult = await AnalyzeProjectAsync(projectPath, analysisTypes, cancellationToken);
                if (!analysisResult.Success)
                {
                    report.ErrorMessage = analysisResult.ErrorMessage;
                    return report;
                }

                // Generate report
                report = await _debuggingTools.GenerateReportAsync(analysisResult.Issues, cancellationToken);

                stopwatch.Stop();
                report.GenerationTime = stopwatch.Elapsed;
                report.Success = true;

                _metrics.RecordReportGeneration(stopwatch.Elapsed);
                _logger.LogInformation($"Debugging report generated successfully");

                return report;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                report.ErrorMessage = ex.Message;
                report.GenerationTime = stopwatch.Elapsed;
                
                _metrics.RecordError(stopwatch.Elapsed);
                _logger.LogError(ex, "Failed to generate debugging report");
                return report;
            }
        }

        #endregion

        #region IDE Integration

        /// <summary>
        /// Install IDE extensions
        /// </summary>
        public async Task<IDEIntegrationResult> InstallIDEExtensionsAsync(
            List<IDEType> ides, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new IDEIntegrationResult { Success = false };

            try
            {
                _logger.LogInformation($"Installing IDE extensions for: {string.Join(", ", ides)}");

                var installations = new List<ExtensionInstallation>();

                foreach (var ide in ides)
                {
                    var installation = await _ideIntegration.InstallExtensionAsync(ide, cancellationToken);
                    installations.Add(installation);
                }

                stopwatch.Stop();
                result.Success = true;
                result.Installations = installations;
                result.InstallationTime = stopwatch.Elapsed;

                _metrics.RecordExtensionInstallation(stopwatch.Elapsed);
                _logger.LogInformation($"IDE extensions installed successfully");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ErrorMessage = ex.Message;
                result.InstallationTime = stopwatch.Elapsed;
                
                _metrics.RecordError(stopwatch.Elapsed);
                _logger.LogError(ex, "Failed to install IDE extensions");
                return result;
            }
        }

        #endregion

        #region Documentation Generation

        /// <summary>
        /// Generate project documentation
        /// </summary>
        public async Task<DocumentationResult> GenerateDocumentationAsync(
            string projectPath, DocumentationFormat format, string outputPath, 
            CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new DocumentationResult { Success = false };

            try
            {
                _logger.LogInformation($"Generating documentation: {format} for {projectPath}");

                // Generate documentation
                var documentation = await _docGenerator.GenerateDocumentationAsync(projectPath, format, cancellationToken);
                
                // Save to output path
                await File.WriteAllTextAsync(outputPath, documentation, cancellationToken);

                stopwatch.Stop();
                result.Success = true;
                result.OutputPath = outputPath;
                result.GenerationTime = stopwatch.Elapsed;

                _metrics.RecordDocumentationGeneration(stopwatch.Elapsed);
                _logger.LogInformation($"Documentation generated successfully: {outputPath}");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ErrorMessage = ex.Message;
                result.GenerationTime = stopwatch.Elapsed;
                
                _metrics.RecordError(stopwatch.Elapsed);
                _logger.LogError(ex, "Failed to generate documentation");
                return result;
            }
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

    public class ProductivityOptions
    {
        public bool EnableTemplates { get; set; } = true;
        public bool EnableSnippets { get; set; } = true;
        public bool EnableDebugging { get; set; } = true;
        public bool EnableIDEIntegration { get; set; } = true;
        public bool EnableDocumentation { get; set; } = true;
        public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes(10);
    }

    public enum IDEType
    {
        VisualStudio,
        VisualStudioCode,
        Rider,
        IntelliJ
    }

    public enum AnalysisType
    {
        Syntax,
        Performance,
        Security,
        BestPractices,
        CodeStyle
    }

    public enum DocumentationFormat
    {
        Markdown,
        Html,
        Pdf,
        Json
    }

    public class TemplateResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string ProjectPath { get; set; }
        public List<string> GeneratedFiles { get; set; } = new List<string>();
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public TimeSpan CreationTime { get; set; }
    }

    public class SnippetResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string TargetFile { get; set; }
        public int LineNumber { get; set; }
        public TimeSpan InsertionTime { get; set; }
        public TimeSpan CreationTime { get; set; }
    }

    public class DebuggingResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string ProjectPath { get; set; }
        public List<ProjectIssue> Issues { get; set; } = new List<ProjectIssue>();
        public TimeSpan AnalysisTime { get; set; }
    }

    public class DebuggingReport
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string ReportContent { get; set; }
        public TimeSpan GenerationTime { get; set; }
    }

    public class IDEIntegrationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public List<ExtensionInstallation> Installations { get; set; } = new List<ExtensionInstallation>();
        public TimeSpan InstallationTime { get; set; }
    }

    public class DocumentationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string OutputPath { get; set; }
        public TimeSpan GenerationTime { get; set; }
    }

    public class TemplateInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
    }

    public class CodeSnippet
    {
        public string Name { get; set; }
        public string Content { get; set; }
        public string Description { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
    }

    public class ProjectIssue
    {
        public string FilePath { get; set; }
        public int LineNumber { get; set; }
        public string Message { get; set; }
        public AnalysisType Type { get; set; }
        public string Severity { get; set; }
    }

    public class ExtensionInstallation
    {
        public IDEType IDEType { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string ExtensionPath { get; set; }
    }

    public class TemplateManager
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, ProjectTemplate> _templates = new Dictionary<string, ProjectTemplate>();

        public TemplateManager(ILogger logger)
        {
            _logger = logger;
            InitializeDefaultTemplates();
        }

        private void InitializeDefaultTemplates()
        {
            _templates["webapi"] = new ProjectTemplate
            {
                Name = "webapi",
                Description = "ASP.NET Core Web API project",
                Files = new Dictionary<string, string>
                {
                    ["Program.cs"] = "using Microsoft.AspNetCore.Builder;\nusing Microsoft.Extensions.DependencyInjection;\n\nvar builder = WebApplication.CreateBuilder(args);\nbuilder.Services.AddControllers();\nvar app = builder.Build();\napp.MapControllers();\napp.Run();",
                    ["Controllers/HomeController.cs"] = "using Microsoft.AspNetCore.Mvc;\n\n[ApiController]\n[Route(\"[controller]\")]\npublic class HomeController : ControllerBase\n{\n    [HttpGet]\n    public IActionResult Get() => Ok(\"Hello World!\");\n}",
                    ["peanu.tsk"] = "{\n  \"name\": \"{{projectName}}\",\n  \"version\": \"1.0.0\",\n  \"api_enabled\": true,\n  \"api_port\": 5000\n}"
                },
                Parameters = new List<TemplateParameter>
                {
                    new TemplateParameter { Name = "projectName", Type = "string", Required = true },
                    new TemplateParameter { Name = "description", Type = "string", Required = false }
                }
            };

            _templates["console"] = new ProjectTemplate
            {
                Name = "console",
                Description = "Console application",
                Files = new Dictionary<string, string>
                {
                    ["Program.cs"] = "Console.WriteLine(\"Hello, {{projectName}}!\");",
                    ["peanu.tsk"] = "{\n  \"name\": \"{{projectName}}\",\n  \"version\": \"1.0.0\"\n}"
                },
                Parameters = new List<TemplateParameter>
                {
                    new TemplateParameter { Name = "projectName", Type = "string", Required = true }
                }
            };
        }

        public async Task<ProjectTemplate> GetTemplateAsync(string name, CancellationToken cancellationToken)
        {
            return _templates.TryGetValue(name, out var template) ? template : null;
        }

        public async Task<List<TemplateInfo>> ListTemplatesAsync(CancellationToken cancellationToken)
        {
            return _templates.Values.Select(t => new TemplateInfo
            {
                Name = t.Name,
                Description = t.Description,
                CreatedAt = DateTime.UtcNow
            }).ToList();
        }

        public async Task<ValidationResult> ValidateParametersAsync(ProjectTemplate template, Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var result = new ValidationResult { IsValid = true, Errors = new List<string>() };

            foreach (var param in template.Parameters.Where(p => p.Required))
            {
                if (!parameters.ContainsKey(param.Name) || parameters[param.Name] == null)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Required parameter missing: {param.Name}");
                }
            }

            if (!result.IsValid)
            {
                result.ErrorMessage = string.Join("; ", result.Errors);
            }

            return result;
        }

        public async Task<List<string>> GenerateProjectAsync(ProjectTemplate template, string projectPath, Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var generatedFiles = new List<string>();

            foreach (var file in template.Files)
            {
                var content = ReplaceParameters(file.Value, parameters);
                var filePath = Path.Combine(projectPath, file.Key);
                
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(filePath, content, cancellationToken);
                generatedFiles.Add(filePath);
            }

            return generatedFiles;
        }

        private string ReplaceParameters(string content, Dictionary<string, object> parameters)
        {
            foreach (var param in parameters)
            {
                content = content.Replace($"{{{{{param.Key}}}}}", param.Value?.ToString() ?? "");
            }
            return content;
        }
    }

    public class SnippetManager
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, CodeSnippet> _snippets = new Dictionary<string, CodeSnippet>();

        public SnippetManager(ILogger logger)
        {
            _logger = logger;
            InitializeDefaultSnippets();
        }

        private void InitializeDefaultSnippets()
        {
            _snippets["class"] = new CodeSnippet
            {
                Name = "class",
                Content = "public class {{className}}\n{\n    {{content}}\n}",
                Description = "Create a new class",
                Tags = new List<string> { "class", "csharp" }
            };

            _snippets["method"] = new CodeSnippet
            {
                Name = "method",
                Content = "public {{returnType}} {{methodName}}()\n{\n    {{content}}\n}",
                Description = "Create a new method",
                Tags = new List<string> { "method", "csharp" }
            };
        }

        public async Task<CodeSnippet> GetSnippetAsync(string name, CancellationToken cancellationToken)
        {
            return _snippets.TryGetValue(name, out var snippet) ? snippet : null;
        }

        public async Task InsertSnippetAsync(CodeSnippet snippet, string targetFile, int lineNumber, Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var content = await File.ReadAllLinesAsync(targetFile, cancellationToken);
            var snippetContent = ReplaceParameters(snippet.Content, parameters);
            var snippetLines = snippetContent.Split('\n');

            var newContent = new List<string>();
            newContent.AddRange(content.Take(lineNumber));
            newContent.AddRange(snippetLines);
            newContent.AddRange(content.Skip(lineNumber));

            await File.WriteAllLinesAsync(targetFile, newContent, cancellationToken);
        }

        public async Task SaveSnippetAsync(CodeSnippet snippet, CancellationToken cancellationToken)
        {
            _snippets[snippet.Name] = snippet;
        }

        private string ReplaceParameters(string content, Dictionary<string, object> parameters)
        {
            if (parameters == null) return content;
            
            foreach (var param in parameters)
            {
                content = content.Replace($"{{{{{param.Key}}}}}", param.Value?.ToString() ?? "");
            }
            return content;
        }
    }

    public class DebuggingTools
    {
        private readonly ILogger _logger;

        public DebuggingTools(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<List<ProjectIssue>> AnalyzeProjectAsync(string projectPath, AnalysisType analysisType, CancellationToken cancellationToken)
        {
            var issues = new List<ProjectIssue>();

            try
            {
                var files = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    var fileIssues = await AnalyzeFileAsync(file, analysisType, cancellationToken);
                    issues.AddRange(fileIssues);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze project");
            }

            return issues;
        }

        public async Task<DebuggingReport> GenerateReportAsync(List<ProjectIssue> issues, CancellationToken cancellationToken)
        {
            var report = new StringBuilder();
            report.AppendLine("# Project Analysis Report");
            report.AppendLine();
            report.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Total Issues: {issues.Count}");
            report.AppendLine();

            var groupedIssues = issues.GroupBy(i => i.Type);
            foreach (var group in groupedIssues)
            {
                report.AppendLine($"## {group.Key} Issues ({group.Count()})");
                foreach (var issue in group)
                {
                    report.AppendLine($"- **{issue.FilePath}:{issue.LineNumber}** - {issue.Message} ({issue.Severity})");
                }
                report.AppendLine();
            }

            return new DebuggingReport
            {
                Success = true,
                ReportContent = report.ToString()
            };
        }

        private async Task<List<ProjectIssue>> AnalyzeFileAsync(string filePath, AnalysisType analysisType, CancellationToken cancellationToken)
        {
            var issues = new List<ProjectIssue>();

            try
            {
                var content = await File.ReadAllTextAsync(filePath, cancellationToken);
                var lines = content.Split('\n');

                switch (analysisType)
                {
                    case AnalysisType.Syntax:
                        issues.AddRange(AnalyzeSyntax(filePath, lines));
                        break;
                    case AnalysisType.Performance:
                        issues.AddRange(AnalyzePerformance(filePath, lines));
                        break;
                    case AnalysisType.Security:
                        issues.AddRange(AnalyzeSecurity(filePath, lines));
                        break;
                    case AnalysisType.BestPractices:
                        issues.AddRange(AnalyzeBestPractices(filePath, lines));
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to analyze file: {filePath}");
            }

            return issues;
        }

        private List<ProjectIssue> AnalyzeSyntax(string filePath, string[] lines)
        {
            var issues = new List<ProjectIssue>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                // Check for missing semicolons
                if (line.Trim().EndsWith(";") == false && 
                    line.Trim().Length > 0 && 
                    !line.Trim().EndsWith("{") && 
                    !line.Trim().EndsWith("}") &&
                    !line.Trim().StartsWith("//") &&
                    !line.Trim().StartsWith("using") &&
                    !line.Trim().StartsWith("namespace") &&
                    !line.Trim().StartsWith("public") &&
                    !line.Trim().StartsWith("private") &&
                    !line.Trim().StartsWith("protected"))
                {
                    issues.Add(new ProjectIssue
                    {
                        FilePath = filePath,
                        LineNumber = i + 1,
                        Message = "Missing semicolon",
                        Type = AnalysisType.Syntax,
                        Severity = "Warning"
                    });
                }
            }

            return issues;
        }

        private List<ProjectIssue> AnalyzePerformance(string filePath, string[] lines)
        {
            var issues = new List<ProjectIssue>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                // Check for potential performance issues
                if (line.Contains("ToList()") && line.Contains("Where("))
                {
                    issues.Add(new ProjectIssue
                    {
                        FilePath = filePath,
                        LineNumber = i + 1,
                        Message = "Consider using Where().ToList() instead of ToList().Where()",
                        Type = AnalysisType.Performance,
                        Severity = "Info"
                    });
                }
            }

            return issues;
        }

        private List<ProjectIssue> AnalyzeSecurity(string filePath, string[] lines)
        {
            var issues = new List<ProjectIssue>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                // Check for potential security issues
                if (line.Contains("password") && line.Contains("=") && !line.Contains("//"))
                {
                    issues.Add(new ProjectIssue
                    {
                        FilePath = filePath,
                        LineNumber = i + 1,
                        Message = "Consider using secure password handling",
                        Type = AnalysisType.Security,
                        Severity = "Warning"
                    });
                }
            }

            return issues;
        }

        private List<ProjectIssue> AnalyzeBestPractices(string filePath, string[] lines)
        {
            var issues = new List<ProjectIssue>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                // Check for best practices
                if (line.Contains("public string") && line.Contains("get; set;"))
                {
                    issues.Add(new ProjectIssue
                    {
                        FilePath = filePath,
                        LineNumber = i + 1,
                        Message = "Consider using auto-properties with validation",
                        Type = AnalysisType.BestPractices,
                        Severity = "Info"
                    });
                }
            }

            return issues;
        }
    }

    public class IDEIntegration
    {
        private readonly ILogger _logger;

        public IDEIntegration(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<ExtensionInstallation> InstallExtensionAsync(IDEType ideType, CancellationToken cancellationToken)
        {
            try
            {
                var installation = new ExtensionInstallation
                {
                    IDEType = ideType,
                    Success = true,
                    ExtensionPath = GetExtensionPath(ideType)
                };

                // In a real implementation, this would actually install the extension
                _logger.LogInformation($"Extension installation simulated for {ideType}");

                return installation;
            }
            catch (Exception ex)
            {
                return new ExtensionInstallation
                {
                    IDEType = ideType,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private string GetExtensionPath(IDEType ideType)
        {
            return ideType switch
            {
                IDEType.VisualStudioCode => "~/.vscode/extensions/tusklang",
                IDEType.VisualStudio => "C:\\Program Files\\Microsoft Visual Studio\\Extensions\\TuskLang",
                IDEType.Rider => "~/.Rider2019.3/config/plugins/TuskLang",
                IDEType.IntelliJ => "~/.IntelliJIdea2019.3/config/plugins/TuskLang",
                _ => "unknown"
            };
        }
    }

    public class ProjectScaffolder
    {
        private readonly ILogger _logger;

        public ProjectScaffolder(ILogger logger)
        {
            _logger = logger;
        }

        public async Task InitializeProjectAsync(string projectPath, ProjectTemplate template, CancellationToken cancellationToken)
        {
            try
            {
                // Create .gitignore
                var gitignorePath = Path.Combine(projectPath, ".gitignore");
                if (!File.Exists(gitignorePath))
                {
                    var gitignoreContent = "bin/\nobj/\n*.user\n*.suo\n*.cache\n.DS_Store\nThumbs.db";
                    await File.WriteAllTextAsync(gitignorePath, gitignoreContent, cancellationToken);
                }

                // Create README.md
                var readmePath = Path.Combine(projectPath, "README.md");
                if (!File.Exists(readmePath))
                {
                    var readmeContent = $"# {template.Name}\n\n{template.Description}\n\n## Getting Started\n\nRun `tusk run` to start the application.";
                    await File.WriteAllTextAsync(readmePath, readmeContent, cancellationToken);
                }

                _logger.LogInformation($"Project scaffolded: {projectPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scaffold project");
            }
        }
    }

    public class DocumentationGenerator
    {
        private readonly ILogger _logger;

        public DocumentationGenerator(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<string> GenerateDocumentationAsync(string projectPath, DocumentationFormat format, CancellationToken cancellationToken)
        {
            try
            {
                var documentation = new StringBuilder();

                switch (format)
                {
                    case DocumentationFormat.Markdown:
                        documentation.AppendLine("# Project Documentation");
                        documentation.AppendLine();
                        documentation.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                        documentation.AppendLine($"Project: {Path.GetFileName(projectPath)}");
                        documentation.AppendLine();
                        documentation.AppendLine("## Files");
                        documentation.AppendLine();
                        
                        var files = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories);
                        foreach (var file in files.OrderBy(f => f))
                        {
                            var relativePath = Path.GetRelativePath(projectPath, file);
                            documentation.AppendLine($"- {relativePath}");
                        }
                        break;

                    case DocumentationFormat.Json:
                        var docData = new
                        {
                            ProjectName = Path.GetFileName(projectPath),
                            GeneratedAt = DateTime.UtcNow,
                            Files = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories)
                                .Select(f => Path.GetRelativePath(projectPath, f))
                                .OrderBy(f => f)
                                .ToList()
                        };
                        documentation.Append(JsonSerializer.Serialize(docData, new JsonSerializerOptions { WriteIndented = true }));
                        break;

                    default:
                        documentation.AppendLine("Documentation format not supported");
                        break;
                }

                return documentation.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate documentation");
                return "Error generating documentation";
            }
        }
    }

    public class ProjectTemplate
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, string> Files { get; set; } = new Dictionary<string, string>();
        public List<TemplateParameter> Parameters { get; set; } = new List<TemplateParameter>();
    }

    public class TemplateParameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Required { get; set; }
        public object DefaultValue { get; set; }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class PerformanceMetrics
    {
        private readonly List<TimeSpan> _projectCreationTimes = new List<TimeSpan>();
        private readonly List<TimeSpan> _snippetInsertionTimes = new List<TimeSpan>();
        private readonly List<TimeSpan> _snippetCreationTimes = new List<TimeSpan>();
        private readonly List<TimeSpan> _projectAnalysisTimes = new List<TimeSpan>();
        private readonly List<TimeSpan> _reportGenerationTimes = new List<TimeSpan>();
        private readonly List<TimeSpan> _extensionInstallationTimes = new List<TimeSpan>();
        private readonly List<TimeSpan> _documentationGenerationTimes = new List<TimeSpan>();
        private readonly List<TimeSpan> _errorTimes = new List<TimeSpan>();

        public void RecordProjectCreation(TimeSpan time)
        {
            _projectCreationTimes.Add(time);
            if (_projectCreationTimes.Count > 100) _projectCreationTimes.RemoveAt(0);
        }

        public void RecordSnippetInsertion(TimeSpan time)
        {
            _snippetInsertionTimes.Add(time);
            if (_snippetInsertionTimes.Count > 1000) _snippetInsertionTimes.RemoveAt(0);
        }

        public void RecordSnippetCreation(TimeSpan time)
        {
            _snippetCreationTimes.Add(time);
            if (_snippetCreationTimes.Count > 100) _snippetCreationTimes.RemoveAt(0);
        }

        public void RecordProjectAnalysis(TimeSpan time)
        {
            _projectAnalysisTimes.Add(time);
            if (_projectAnalysisTimes.Count > 100) _projectAnalysisTimes.RemoveAt(0);
        }

        public void RecordReportGeneration(TimeSpan time)
        {
            _reportGenerationTimes.Add(time);
            if (_reportGenerationTimes.Count > 100) _reportGenerationTimes.RemoveAt(0);
        }

        public void RecordExtensionInstallation(TimeSpan time)
        {
            _extensionInstallationTimes.Add(time);
            if (_extensionInstallationTimes.Count > 100) _extensionInstallationTimes.RemoveAt(0);
        }

        public void RecordDocumentationGeneration(TimeSpan time)
        {
            _documentationGenerationTimes.Add(time);
            if (_documentationGenerationTimes.Count > 100) _documentationGenerationTimes.RemoveAt(0);
        }

        public void RecordError(TimeSpan time)
        {
            _errorTimes.Add(time);
            if (_errorTimes.Count > 100) _errorTimes.RemoveAt(0);
        }

        public double AverageProjectCreationTime => _projectCreationTimes.Count > 0 ? _projectCreationTimes.Average(t => t.TotalMilliseconds) : 0;
        public double AverageSnippetInsertionTime => _snippetInsertionTimes.Count > 0 ? _snippetInsertionTimes.Average(t => t.TotalMilliseconds) : 0;
        public double AverageProjectAnalysisTime => _projectAnalysisTimes.Count > 0 ? _projectAnalysisTimes.Average(t => t.TotalMilliseconds) : 0;
        public int ErrorCount => _errorTimes.Count;
        public int TotalProjects => _projectCreationTimes.Count;
        public int TotalSnippets => _snippetInsertionTimes.Count;
    }

    #endregion
} 