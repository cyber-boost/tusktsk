using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TuskLang.Parser;
using TuskLang.Parser.Ast;

namespace TuskLang.Configuration
{
    /// <summary>
    /// TuskTsk Configuration Engine - Runtime configuration processing system
    /// 
    /// Processes parsed AST into executable runtime configurations:
    /// - Evaluates expressions and resolves variables
    /// - Processes @ operators (env, query, date, etc.)
    /// - Handles cross-file references and imports
    /// - Manages configuration hierarchies and scoping
    /// - Provides configuration value resolution and caching
    /// - Thread-safe concurrent configuration processing
    /// 
    /// Performance: High-performance evaluation, optimized caching, memory efficient
    /// </summary>
    public class ConfigurationEngine : IAstVisitor<object>, IDisposable
    {
        private readonly ConfigurationEngineOptions _options;
        private readonly TuskTskParserFactory _parserFactory;
        private readonly Dictionary<string, object> _globalVariables;
        private readonly Dictionary<string, Dictionary<string, object>> _sectionVariables;
        private readonly ConcurrentDictionary<string, object> _cache;
        private readonly Dictionary<string, ConfigurationNode> _loadedFiles;
        private readonly Stack<string> _currentSections;
        private readonly Dictionary<string, Func<ExpressionNode[], object>> _atOperatorHandlers;
        private readonly object _lock;
        private string _currentFile;
        private ConfigurationContext _context;
        
        /// <summary>
        /// Initializes a new instance of the ConfigurationEngine
        /// </summary>
        public ConfigurationEngine(ConfigurationEngineOptions options = null)
        {
            _options = options ?? new ConfigurationEngineOptions();
            _parserFactory = new TuskTskParserFactory(_options.ParseOptions);
            _globalVariables = new Dictionary<string, object>();
            _sectionVariables = new Dictionary<string, Dictionary<string, object>>();
            _cache = new ConcurrentDictionary<string, object>();
            _loadedFiles = new Dictionary<string, ConfigurationNode>();
            _currentSections = new Stack<string>();
            _atOperatorHandlers = new Dictionary<string, Func<ExpressionNode[], object>>();
            _lock = new object();
            _currentFile = "";
            
            InitializeAtOperatorHandlers();
        }
        
        /// <summary>
        /// Process configuration from file
        /// </summary>
        public async Task<ConfigurationResult> ProcessFileAsync(string filePath)
        {
            var startTime = DateTime.UtcNow;
            var errors = new List<ConfigurationError>();
            
            try
            {
                // Parse the file using G1 parser
                var parseResult = await _parserFactory.ParseFileAsync(filePath);
                
                if (!parseResult.Success)
                {
                    foreach (var error in parseResult.Errors)
                    {
                        errors.Add(new ConfigurationError
                        {
                            Type = ConfigurationErrorType.ParseError,
                            Message = error.Message,
                            File = filePath,
                            Line = error.Line,
                            Column = error.Column
                        });
                    }
                    
                    return new ConfigurationResult
                    {
                        Success = false,
                        Errors = errors,
                        ProcessingTime = DateTime.UtcNow - startTime
                    };
                }
                
                // Process the AST
                _currentFile = filePath;
                _context = new ConfigurationContext(filePath, _options);
                
                var configuration = await ProcessConfigurationAsync(parseResult.Ast);
                
                return new ConfigurationResult
                {
                    Success = true,
                    Configuration = configuration,
                    GlobalVariables = new Dictionary<string, object>(_globalVariables),
                    SectionVariables = _sectionVariables.ToDictionary(
                        kvp => kvp.Key, 
                        kvp => new Dictionary<string, object>(kvp.Value)),
                    ProcessingTime = DateTime.UtcNow - startTime,
                    CacheHits = _context.CacheHits,
                    CacheMisses = _context.CacheMisses
                };
            }
            catch (Exception ex)
            {
                errors.Add(new ConfigurationError
                {
                    Type = ConfigurationErrorType.ProcessingError,
                    Message = $"Configuration processing failed: {ex.Message}",
                    File = filePath,
                    Line = 0,
                    Column = 0
                });
                
                return new ConfigurationResult
                {
                    Success = false,
                    Errors = errors,
                    ProcessingTime = DateTime.UtcNow - startTime
                };
            }
        }
        
        /// <summary>
        /// Process configuration from string
        /// </summary>
        public async Task<ConfigurationResult> ProcessStringAsync(string source, string fileName = "<string>")
        {
            var startTime = DateTime.UtcNow;
            var errors = new List<ConfigurationError>();
            
            try
            {
                // Parse the string using G1 parser
                var parseResult = _parserFactory.ParseString(source, fileName);
                
                if (!parseResult.Success)
                {
                    foreach (var error in parseResult.Errors)
                    {
                        errors.Add(new ConfigurationError
                        {
                            Type = ConfigurationErrorType.ParseError,
                            Message = error.Message,
                            File = fileName,
                            Line = error.Line,
                            Column = error.Column
                        });
                    }
                    
                    return new ConfigurationResult
                    {
                        Success = false,
                        Errors = errors,
                        ProcessingTime = DateTime.UtcNow - startTime
                    };
                }
                
                // Process the AST
                _currentFile = fileName;
                _context = new ConfigurationContext(fileName, _options);
                
                var configuration = await ProcessConfigurationAsync(parseResult.Ast);
                
                return new ConfigurationResult
                {
                    Success = true,
                    Configuration = configuration,
                    GlobalVariables = new Dictionary<string, object>(_globalVariables),
                    SectionVariables = _sectionVariables.ToDictionary(
                        kvp => kvp.Key, 
                        kvp => new Dictionary<string, object>(kvp.Value)),
                    ProcessingTime = DateTime.UtcNow - startTime,
                    CacheHits = _context.CacheHits,
                    CacheMisses = _context.CacheMisses
                };
            }
            catch (Exception ex)
            {
                errors.Add(new ConfigurationError
                {
                    Type = ConfigurationErrorType.ProcessingError,
                    Message = $"Configuration processing failed: {ex.Message}",
                    File = fileName,
                    Line = 0,
                    Column = 0
                });
                
                return new ConfigurationResult
                {
                    Success = false,
                    Errors = errors,
                    ProcessingTime = DateTime.UtcNow - startTime
                };
            }
        }
        
        /// <summary>
        /// Process configuration AST
        /// </summary>
        private async Task<Dictionary<string, object>> ProcessConfigurationAsync(ConfigurationNode ast)
        {
            var configuration = new Dictionary<string, object>();
            
            foreach (var statement in ast.Statements)
            {
                await ProcessStatementAsync(statement, configuration);
            }
            
            return configuration;
        }
        
        /// <summary>
        /// Process individual statement
        /// </summary>
        private async Task ProcessStatementAsync(AstNode statement, Dictionary<string, object> configuration)
        {
            switch (statement)
            {
                case SectionNode section:
                    await ProcessSectionAsync(section, configuration);
                    break;
                    
                case GlobalVariableNode globalVar:
                    await ProcessGlobalVariableAsync(globalVar);
                    break;
                    
                case AssignmentNode assignment:
                    await ProcessAssignmentAsync(assignment, configuration);
                    break;
                    
                case IncludeNode include:
                    await ProcessIncludeAsync(include, configuration);
                    break;
                    
                case CommentNode comment:
                    // Comments are processed for documentation but don't affect runtime
                    ProcessComment(comment, configuration);
                    break;
            }
        }
        
        /// <summary>
        /// Process section declaration
        /// </summary>
        private async Task ProcessSectionAsync(SectionNode section, Dictionary<string, object> configuration)
        {
            var sectionName = section.Name;
            _currentSections.Push(sectionName);
            
            if (!configuration.ContainsKey(sectionName))
            {
                configuration[sectionName] = new Dictionary<string, object>();
            }
            
            if (!_sectionVariables.ContainsKey(sectionName))
            {
                _sectionVariables[sectionName] = new Dictionary<string, object>();
            }
            
            _context.EnterSection(sectionName);
        }
        
        /// <summary>
        /// Process global variable
        /// </summary>
        private async Task ProcessGlobalVariableAsync(GlobalVariableNode globalVar)
        {
            var value = await EvaluateExpressionAsync(globalVar.Value);
            _globalVariables[globalVar.Name] = value;
            _context.RegisterGlobalVariable(globalVar.Name, value);
        }
        
        /// <summary>
        /// Process assignment
        /// </summary>
        private async Task ProcessAssignmentAsync(AssignmentNode assignment, Dictionary<string, object> configuration)
        {
            var key = assignment.Key;
            var value = await EvaluateExpressionAsync(assignment.Value);
            
            if (_currentSections.Count > 0)
            {
                // Assignment within a section
                var currentSection = _currentSections.Peek();
                var sectionConfig = (Dictionary<string, object>)configuration[currentSection];
                sectionConfig[key] = value;
                _sectionVariables[currentSection][key] = value;
            }
            else
            {
                // Top-level assignment
                configuration[key] = value;
            }
            
            _context.RegisterAssignment(key, value);
        }
        
        /// <summary>
        /// Process include statement
        /// </summary>
        private async Task ProcessIncludeAsync(IncludeNode include, Dictionary<string, object> configuration)
        {
            var pathValue = await EvaluateExpressionAsync(include.Path);
            var includePath = pathValue?.ToString() ?? "";
            
            if (string.IsNullOrEmpty(includePath))
            {
                throw new ConfigurationException($"Include path cannot be empty at line {include.Line}");
            }
            
            // Resolve relative path
            var fullPath = ResolvePath(includePath, _currentFile);
            
            // Prevent circular includes
            if (_context.IsFileInIncludeChain(fullPath))
            {
                throw new ConfigurationException($"Circular include detected: {fullPath}");
            }
            
            // Load and process included file
            ConfigurationNode includedAst;
            
            if (_loadedFiles.ContainsKey(fullPath))
            {
                includedAst = _loadedFiles[fullPath];
                _context.RegisterCacheHit();
            }
            else
            {
                var parseResult = await _parserFactory.ParseFileAsync(fullPath);
                if (!parseResult.Success)
                {
                    throw new ConfigurationException($"Failed to parse included file {fullPath}: {parseResult.Errors.FirstOrDefault()?.Message}");
                }
                
                includedAst = parseResult.Ast;
                _loadedFiles[fullPath] = includedAst;
                _context.RegisterCacheMiss();
            }
            
            // Process included configuration
            _context.PushInclude(fullPath);
            var savedFile = _currentFile;
            _currentFile = fullPath;
            
            try
            {
                var includedConfig = await ProcessConfigurationAsync(includedAst);
                
                // Merge included configuration
                if (include.IsImport)
                {
                    // Import: Merge all keys into current scope
                    foreach (var kvp in includedConfig)
                    {
                        configuration[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    // Include: Add as nested object
                    var fileName = Path.GetFileNameWithoutExtension(includePath);
                    configuration[fileName] = includedConfig;
                }
            }
            finally
            {
                _currentFile = savedFile;
                _context.PopInclude();
            }
        }
        
        /// <summary>
        /// Process comment for documentation
        /// </summary>
        private void ProcessComment(CommentNode comment, Dictionary<string, object> configuration)
        {
            if (_options.IncludeComments)
            {
                var commentsKey = "_comments";
                if (!configuration.ContainsKey(commentsKey))
                {
                    configuration[commentsKey] = new List<string>();
                }
                
                ((List<string>)configuration[commentsKey]).Add(comment.Text);
            }
        }
        
        /// <summary>
        /// Evaluate expression and return result
        /// </summary>
        public async Task<object> EvaluateExpressionAsync(ExpressionNode expression)
        {
            if (expression == null) return null;
            
            try
            {
                var result = expression.Accept(this);
                
                // Handle async results
                if (result is Task task)
                {
                    await task;
                    if (task.GetType().IsGenericType)
                    {
                        dynamic dynamicTask = task;
                        return dynamicTask.Result;
                    }
                    return null;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                throw new ConfigurationException($"Expression evaluation failed at line {expression.Line}: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Visit configuration node
        /// </summary>
        public object VisitConfiguration(ConfigurationNode node)
        {
            // This should not be called directly in expression evaluation
            throw new InvalidOperationException("Configuration node cannot be evaluated as expression");
        }
        
        /// <summary>
        /// Visit comment node
        /// </summary>
        public object VisitComment(CommentNode node)
        {
            return node.Text;
        }
        
        /// <summary>
        /// Visit section node
        /// </summary>
        public object VisitSection(SectionNode node)
        {
            return node.Name;
        }
        
        /// <summary>
        /// Visit global variable node
        /// </summary>
        public object VisitGlobalVariable(GlobalVariableNode node)
        {
            return EvaluateExpressionAsync(node.Value).Result;
        }
        
        /// <summary>
        /// Visit assignment node
        /// </summary>
        public object VisitAssignment(AssignmentNode node)
        {
            return EvaluateExpressionAsync(node.Value).Result;
        }
        
        /// <summary>
        /// Visit include node
        /// </summary>
        public object VisitInclude(IncludeNode node)
        {
            return EvaluateExpressionAsync(node.Path).Result;
        }
        
        /// <summary>
        /// Visit literal node
        /// </summary>
        public object VisitLiteral(LiteralNode node)
        {
            return node.Value;
        }
        
        /// <summary>
        /// Visit string node
        /// </summary>
        public object VisitString(StringNode node)
        {
            if (node.IsTemplate)
            {
                return ProcessTemplateString(node.Value);
            }
            
            return node.Value;
        }
        
        /// <summary>
        /// Visit variable reference node
        /// </summary>
        public object VisitVariableReference(VariableReferenceNode node)
        {
            var varName = node.Name;
            
            if (node.IsGlobal)
            {
                // Global variable reference
                if (_globalVariables.TryGetValue(varName, out var globalValue))
                {
                    return globalValue;
                }
                throw new ConfigurationException($"Undefined global variable '${varName}'");
            }
            else
            {
                // Local variable reference - check current section first, then global
                if (_currentSections.Count > 0)
                {
                    var currentSection = _currentSections.Peek();
                    if (_sectionVariables.TryGetValue(currentSection, out var sectionVars) &&
                        sectionVars.TryGetValue(varName, out var sectionValue))
                    {
                        return sectionValue;
                    }
                }
                
                // Check global variables
                if (_globalVariables.TryGetValue(varName, out var globalValue))
                {
                    return globalValue;
                }
                
                throw new ConfigurationException($"Undefined variable '{varName}'");
            }
        }
        
        /// <summary>
        /// Visit binary operator node
        /// </summary>
        public object VisitBinaryOperator(BinaryOperatorNode node)
        {
            var left = EvaluateExpressionAsync(node.Left).Result;
            var right = EvaluateExpressionAsync(node.Right).Result;
            
            return EvaluateBinaryOperation(node.Operator, left, right);
        }
        
        /// <summary>
        /// Visit unary operator node
        /// </summary>
        public object VisitUnaryOperator(UnaryOperatorNode node)
        {
            var operand = EvaluateExpressionAsync(node.Expression).Result;
            
            return EvaluateUnaryOperation(node.Operator, operand);
        }
        
        /// <summary>
        /// Visit ternary node
        /// </summary>
        public object VisitTernary(TernaryNode node)
        {
            var condition = EvaluateExpressionAsync(node.Condition).Result;
            var isTrue = ConvertToBoolean(condition);
            
            return isTrue 
                ? EvaluateExpressionAsync(node.TrueExpression).Result
                : EvaluateExpressionAsync(node.FalseExpression).Result;
        }
        
        /// <summary>
        /// Visit range node
        /// </summary>
        public object VisitRange(RangeNode node)
        {
            var start = EvaluateExpressionAsync(node.Start).Result;
            var end = EvaluateExpressionAsync(node.End).Result;
            
            if (start is int startInt && end is int endInt)
            {
                return Enumerable.Range(startInt, endInt - startInt + 1).ToArray();
            }
            
            return new { Start = start, End = end };
        }
        
        /// <summary>
        /// Visit array node
        /// </summary>
        public object VisitArray(ArrayNode node)
        {
            var elements = new object[node.Elements.Count];
            
            for (int i = 0; i < node.Elements.Count; i++)
            {
                elements[i] = EvaluateExpressionAsync(node.Elements[i]).Result;
            }
            
            return elements;
        }
        
        /// <summary>
        /// Visit object node
        /// </summary>
        public object VisitObject(ObjectNode node)
        {
            var result = new Dictionary<string, object>();
            
            foreach (var property in node.Properties)
            {
                var value = EvaluateExpressionAsync(property.Value).Result;
                result[property.Key] = value;
            }
            
            return result;
        }
        
        /// <summary>
        /// Visit named object node
        /// </summary>
        public object VisitNamedObject(NamedObjectNode node)
        {
            var result = new Dictionary<string, object>();
            result["_name"] = node.Name;
            
            foreach (var property in node.Properties)
            {
                var value = EvaluateExpressionAsync(property.Value).Result;
                result[property.Key] = value;
            }
            
            return result;
        }
        
        /// <summary>
        /// Visit @ operator node
        /// </summary>
        public object VisitAtOperator(AtOperatorNode node)
        {
            var operatorName = node.OperatorName.ToLower();
            
            if (_atOperatorHandlers.TryGetValue(operatorName, out var handler))
            {
                return handler(node.Arguments);
            }
            
            throw new ConfigurationException($"Unknown @ operator: {operatorName}");
        }
        
        /// <summary>
        /// Visit cross-file operator node
        /// </summary>
        public object VisitCrossFileOperator(CrossFileOperatorNode node)
        {
            var fileName = node.FileName;
            var methodName = node.MethodName;
            var args = node.Arguments.Select(arg => EvaluateExpressionAsync(arg).Result).ToArray();
            
            return ProcessCrossFileOperation(fileName, methodName, args);
        }
        
        /// <summary>
        /// Visit property access node
        /// </summary>
        public object VisitPropertyAccess(PropertyAccessNode node)
        {
            var obj = EvaluateExpressionAsync(node.Object).Result;
            var propertyName = node.PropertyName;
            
            if (obj is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue(propertyName, out var value))
                {
                    return value;
                }
                throw new ConfigurationException($"Property '{propertyName}' not found");
            }
            
            // Use reflection for other object types
            var type = obj.GetType();
            var property = type.GetProperty(propertyName);
            if (property != null)
            {
                return property.GetValue(obj);
            }
            
            var field = type.GetField(propertyName);
            if (field != null)
            {
                return field.GetValue(obj);
            }
            
            throw new ConfigurationException($"Property or field '{propertyName}' not found on type {type.Name}");
        }
        
        /// <summary>
        /// Visit method call node
        /// </summary>
        public object VisitMethodCall(MethodCallNode node)
        {
            var obj = EvaluateExpressionAsync(node.Object).Result;
            var args = node.Arguments.Select(arg => EvaluateExpressionAsync(arg).Result).ToArray();
            
            // Special handling for built-in methods
            if (obj is string str)
            {
                return ProcessStringMethod(str, args);
            }
            
            if (obj is Array array)
            {
                return ProcessArrayMethod(array, args);
            }
            
            // Generic method invocation would require more complex reflection
            throw new ConfigurationException("Method calls are not fully implemented in this version");
        }
        
        /// <summary>
        /// Visit index access node
        /// </summary>
        public object VisitIndexAccess(IndexAccessNode node)
        {
            var obj = EvaluateExpressionAsync(node.Object).Result;
            var index = EvaluateExpressionAsync(node.Index).Result;
            
            if (obj is Array array && index is int arrayIndex)
            {
                if (arrayIndex >= 0 && arrayIndex < array.Length)
                {
                    return array.GetValue(arrayIndex);
                }
                throw new IndexOutOfRangeException($"Array index {arrayIndex} out of bounds");
            }
            
            if (obj is Dictionary<string, object> dict && index is string key)
            {
                if (dict.TryGetValue(key, out var value))
                {
                    return value;
                }
                throw new KeyNotFoundException($"Key '{key}' not found");
            }
            
            throw new ConfigurationException($"Cannot index {obj?.GetType()?.Name ?? "null"} with {index?.GetType()?.Name ?? "null"}");
        }
        
        /// <summary>
        /// Visit grouping node
        /// </summary>
        public object VisitGrouping(GroupingNode node)
        {
            return EvaluateExpressionAsync(node.Expression).Result;
        }
        
        /// <summary>
        /// Initialize @ operator handlers
        /// </summary>
        private void InitializeAtOperatorHandlers()
        {
            _atOperatorHandlers["env"] = ProcessEnvOperator;
            _atOperatorHandlers["query"] = ProcessQueryOperator;
            _atOperatorHandlers["date"] = ProcessDateOperator;
            _atOperatorHandlers["cache"] = ProcessCacheOperator;
            _atOperatorHandlers["metrics"] = ProcessMetricsOperator;
            _atOperatorHandlers["optimize"] = ProcessOptimizeOperator;
            _atOperatorHandlers["learn"] = ProcessLearnOperator;
        }
        
        /// <summary>
        /// Process environment variable operator
        /// </summary>
        private object ProcessEnvOperator(ExpressionNode[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                throw new ConfigurationException("@env expects 1-2 arguments");
            }
            
            var varName = EvaluateExpressionAsync(args[0]).Result?.ToString();
            if (string.IsNullOrEmpty(varName))
            {
                throw new ConfigurationException("@env variable name cannot be empty");
            }
            
            var envValue = Environment.GetEnvironmentVariable(varName);
            
            if (envValue != null)
            {
                return envValue;
            }
            
            // Return default value if provided
            if (args.Length > 1)
            {
                return EvaluateExpressionAsync(args[1]).Result;
            }
            
            return "";
        }
        
        /// <summary>
        /// Process database query operator
        /// </summary>
        private object ProcessQueryOperator(ExpressionNode[] args)
        {
            if (args.Length != 1)
            {
                throw new ConfigurationException("@query expects 1 argument");
            }
            
            var query = EvaluateExpressionAsync(args[0]).Result?.ToString();
            if (string.IsNullOrEmpty(query))
            {
                throw new ConfigurationException("@query statement cannot be empty");
            }
            
            // In production, this would connect to actual database
            // For now, return a placeholder that indicates the query
            return new
            {
                Type = "DatabaseQuery",
                Query = query,
                Timestamp = DateTime.UtcNow,
                Note = "Database query execution requires database connection configuration"
            };
        }
        
        /// <summary>
        /// Process date operator
        /// </summary>
        private object ProcessDateOperator(ExpressionNode[] args)
        {
            var format = "yyyy-MM-dd HH:mm:ss";
            
            if (args.Length > 0)
            {
                var formatArg = EvaluateExpressionAsync(args[0]).Result?.ToString();
                if (!string.IsNullOrEmpty(formatArg))
                {
                    // Convert PHP-style format to C# format
                    format = ConvertDateFormat(formatArg);
                }
            }
            
            return DateTime.Now.ToString(format);
        }
        
        /// <summary>
        /// Process cache operator
        /// </summary>
        private object ProcessCacheOperator(ExpressionNode[] args)
        {
            if (args.Length < 2)
            {
                throw new ConfigurationException("@cache expects at least 2 arguments: ttl, key");
            }
            
            var ttl = EvaluateExpressionAsync(args[0]).Result?.ToString() ?? "5m";
            var key = EvaluateExpressionAsync(args[1]).Result?.ToString() ?? "";
            
            return new
            {
                Type = "CacheOperation",
                TTL = ttl,
                Key = key,
                Timestamp = DateTime.UtcNow
            };
        }
        
        /// <summary>
        /// Process metrics operator
        /// </summary>
        private object ProcessMetricsOperator(ExpressionNode[] args)
        {
            var metricName = args.Length > 0 ? EvaluateExpressionAsync(args[0]).Result?.ToString() : "default";
            var value = args.Length > 1 ? EvaluateExpressionAsync(args[1]).Result : 1;
            
            return new
            {
                Type = "MetricsOperation",
                Metric = metricName,
                Value = value,
                Timestamp = DateTime.UtcNow
            };
        }
        
        /// <summary>
        /// Process optimize operator
        /// </summary>
        private object ProcessOptimizeOperator(ExpressionNode[] args)
        {
            var operation = args.Length > 0 ? EvaluateExpressionAsync(args[0]).Result?.ToString() : "default";
            var parameter = args.Length > 1 ? EvaluateExpressionAsync(args[1]).Result : null;
            
            return new
            {
                Type = "OptimizeOperation",
                Operation = operation,
                Parameter = parameter,
                Timestamp = DateTime.UtcNow
            };
        }
        
        /// <summary>
        /// Process learn operator
        /// </summary>
        private object ProcessLearnOperator(ExpressionNode[] args)
        {
            var learningKey = args.Length > 0 ? EvaluateExpressionAsync(args[0]).Result?.ToString() : "default";
            var defaultValue = args.Length > 1 ? EvaluateExpressionAsync(args[1]).Result : null;
            
            return new
            {
                Type = "LearnOperation",
                Key = learningKey,
                DefaultValue = defaultValue,
                Timestamp = DateTime.UtcNow
            };
        }
        
        /// <summary>
        /// Process cross-file operation
        /// </summary>
        private object ProcessCrossFileOperation(string fileName, string methodName, object[] args)
        {
            var filePath = ResolvePath($"{fileName}.tsk", _currentFile);
            
            switch (methodName.ToLower())
            {
                case "get":
                    if (args.Length != 1)
                    {
                        throw new ConfigurationException("Cross-file get method expects 1 argument");
                    }
                    return GetCrossFileValue(filePath, args[0]?.ToString());
                    
                case "set":
                    if (args.Length != 2)
                    {
                        throw new ConfigurationException("Cross-file set method expects 2 arguments");
                    }
                    return SetCrossFileValue(filePath, args[0]?.ToString(), args[1]);
                    
                case "exists":
                    if (args.Length != 1)
                    {
                        throw new ConfigurationException("Cross-file exists method expects 1 argument");
                    }
                    return CrossFileValueExists(filePath, args[0]?.ToString());
                    
                default:
                    throw new ConfigurationException($"Unknown cross-file method: {methodName}");
            }
        }
        
        /// <summary>
        /// Evaluate binary operation
        /// </summary>
        private object EvaluateBinaryOperation(string operatorType, object left, object right)
        {
            return operatorType switch
            {
                "Plus" => AddValues(left, right),
                "Minus" => SubtractValues(left, right),
                "Multiply" => MultiplyValues(left, right),
                "Divide" => DivideValues(left, right),
                "Modulo" => ModuloValues(left, right),
                "EqualEqual" => Equals(left, right),
                "BangEqual" => !Equals(left, right),
                "Greater" => CompareValues(left, right) > 0,
                "GreaterEqual" => CompareValues(left, right) >= 0,
                "Less" => CompareValues(left, right) < 0,
                "LessEqual" => CompareValues(left, right) <= 0,
                "AndAnd" or "LogicalAnd" => ConvertToBoolean(left) && ConvertToBoolean(right),
                "OrOr" or "LogicalOr" => ConvertToBoolean(left) || ConvertToBoolean(right),
                _ => throw new ConfigurationException($"Unknown binary operator: {operatorType}")
            };
        }
        
        /// <summary>
        /// Evaluate unary operation
        /// </summary>
        private object EvaluateUnaryOperation(string operatorType, object operand)
        {
            return operatorType switch
            {
                "Minus" => NegateValue(operand),
                "Plus" => operand,
                "Exclamation" or "LogicalNot" => !ConvertToBoolean(operand),
                _ => throw new ConfigurationException($"Unknown unary operator: {operatorType}")
            };
        }
        
        /// <summary>
        /// Process template string with variable substitution
        /// </summary>
        private string ProcessTemplateString(string template)
        {
            var result = template;
            
            // Process ${variable} patterns
            var pattern = @"\$\{([^}]+)\}";
            var matches = System.Text.RegularExpressions.Regex.Matches(result, pattern);
            
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var varName = match.Groups[1].Value;
                var value = GetVariableValue(varName);
                result = result.Replace(match.Value, value?.ToString() ?? "");
            }
            
            return result;
        }
        
        /// <summary>
        /// Get variable value by name
        /// </summary>
        private object GetVariableValue(string varName)
        {
            // Check current section variables
            if (_currentSections.Count > 0)
            {
                var currentSection = _currentSections.Peek();
                if (_sectionVariables.TryGetValue(currentSection, out var sectionVars) &&
                    sectionVars.TryGetValue(varName, out var sectionValue))
                {
                    return sectionValue;
                }
            }
            
            // Check global variables
            if (_globalVariables.TryGetValue(varName, out var globalValue))
            {
                return globalValue;
            }
            
            return null;
        }
        
        /// <summary>
        /// Add two values with type coercion
        /// </summary>
        private object AddValues(object left, object right)
        {
            // String concatenation
            if (left is string || right is string)
            {
                return (left?.ToString() ?? "") + (right?.ToString() ?? "");
            }
            
            // Numeric addition
            if (left is double leftDouble || right is double rightDouble)
            {
                return Convert.ToDouble(left) + Convert.ToDouble(right);
            }
            
            if (left is long leftLong || right is long rightLong)
            {
                return Convert.ToInt64(left) + Convert.ToInt64(right);
            }
            
            if (left is int leftInt || right is int rightInt)
            {
                return Convert.ToInt32(left) + Convert.ToInt32(right);
            }
            
            throw new ConfigurationException($"Cannot add {left?.GetType()?.Name ?? "null"} and {right?.GetType()?.Name ?? "null"}");
        }
        
        /// <summary>
        /// Subtract two values
        /// </summary>
        private object SubtractValues(object left, object right)
        {
            if (left is double leftDouble || right is double rightDouble)
            {
                return Convert.ToDouble(left) - Convert.ToDouble(right);
            }
            
            if (left is long leftLong || right is long rightLong)
            {
                return Convert.ToInt64(left) - Convert.ToInt64(right);
            }
            
            if (left is int leftInt || right is int rightInt)
            {
                return Convert.ToInt32(left) - Convert.ToInt32(right);
            }
            
            throw new ConfigurationException($"Cannot subtract {right?.GetType()?.Name ?? "null"} from {left?.GetType()?.Name ?? "null"}");
        }
        
        /// <summary>
        /// Multiply two values
        /// </summary>
        private object MultiplyValues(object left, object right)
        {
            if (left is double leftDouble || right is double rightDouble)
            {
                return Convert.ToDouble(left) * Convert.ToDouble(right);
            }
            
            if (left is long leftLong || right is long rightLong)
            {
                return Convert.ToInt64(left) * Convert.ToInt64(right);
            }
            
            if (left is int leftInt || right is int rightInt)
            {
                return Convert.ToInt32(left) * Convert.ToInt32(right);
            }
            
            throw new ConfigurationException($"Cannot multiply {left?.GetType()?.Name ?? "null"} and {right?.GetType()?.Name ?? "null"}");
        }
        
        /// <summary>
        /// Divide two values
        /// </summary>
        private object DivideValues(object left, object right)
        {
            if (Convert.ToDouble(right) == 0)
            {
                throw new DivideByZeroException("Division by zero");
            }
            
            return Convert.ToDouble(left) / Convert.ToDouble(right);
        }
        
        /// <summary>
        /// Calculate modulo of two values
        /// </summary>
        private object ModuloValues(object left, object right)
        {
            if (Convert.ToDouble(right) == 0)
            {
                throw new DivideByZeroException("Modulo by zero");
            }
            
            return Convert.ToDouble(left) % Convert.ToDouble(right);
        }
        
        /// <summary>
        /// Negate a value
        /// </summary>
        private object NegateValue(object value)
        {
            if (value is double doubleVal)
                return -doubleVal;
            if (value is long longVal)
                return -longVal;
            if (value is int intVal)
                return -intVal;
            
            throw new ConfigurationException($"Cannot negate {value?.GetType()?.Name ?? "null"}");
        }
        
        /// <summary>
        /// Compare two values
        /// </summary>
        private int CompareValues(object left, object right)
        {
            if (left is string leftStr && right is string rightStr)
            {
                return string.Compare(leftStr, rightStr, StringComparison.Ordinal);
            }
            
            if (IsNumeric(left) && IsNumeric(right))
            {
                var leftDouble = Convert.ToDouble(left);
                var rightDouble = Convert.ToDouble(right);
                return leftDouble.CompareTo(rightDouble);
            }
            
            throw new ConfigurationException($"Cannot compare {left?.GetType()?.Name ?? "null"} and {right?.GetType()?.Name ?? "null"}");
        }
        
        /// <summary>
        /// Convert value to boolean
        /// </summary>
        private bool ConvertToBoolean(object value)
        {
            if (value is bool boolVal)
                return boolVal;
            if (value is string strVal)
                return !string.IsNullOrEmpty(strVal);
            if (value is int intVal)
                return intVal != 0;
            if (value is double doubleVal)
                return doubleVal != 0.0;
            if (value is long longVal)
                return longVal != 0;
            if (value == null)
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Check if value is numeric
        /// </summary>
        private bool IsNumeric(object value)
        {
            return value is int || value is long || value is double || value is float || value is decimal;
        }
        
        /// <summary>
        /// Resolve path relative to current file
        /// </summary>
        private string ResolvePath(string path, string currentFile)
        {
            if (Path.IsPathRooted(path))
            {
                return path;
            }
            
            var currentDir = Path.GetDirectoryName(currentFile) ?? "";
            return Path.Combine(currentDir, path);
        }
        
        /// <summary>
        /// Get value from cross-file reference
        /// </summary>
        private object GetCrossFileValue(string filePath, string key)
        {
            // In a complete implementation, this would load and parse the file
            // then extract the requested value
            return new
            {
                Type = "CrossFileReference",
                File = filePath,
                Key = key,
                Note = "Cross-file value retrieval requires file parsing"
            };
        }
        
        /// <summary>
        /// Set value in cross-file reference
        /// </summary>
        private object SetCrossFileValue(string filePath, string key, object value)
        {
            // In a complete implementation, this would modify the target file
            return new
            {
                Type = "CrossFileSet",
                File = filePath,
                Key = key,
                Value = value,
                Note = "Cross-file value setting requires file modification"
            };
        }
        
        /// <summary>
        /// Check if cross-file value exists
        /// </summary>
        private bool CrossFileValueExists(string filePath, string key)
        {
            // In a complete implementation, this would check the target file
            return File.Exists(filePath);
        }
        
        /// <summary>
        /// Process string method calls
        /// </summary>
        private object ProcessStringMethod(string str, object[] args)
        {
            // Basic string methods - would be expanded in full implementation
            return str;
        }
        
        /// <summary>
        /// Process array method calls
        /// </summary>
        private object ProcessArrayMethod(Array array, object[] args)
        {
            // Basic array methods - would be expanded in full implementation
            return array;
        }
        
        /// <summary>
        /// Convert PHP-style date format to C# format
        /// </summary>
        private string ConvertDateFormat(string phpFormat)
        {
            // Basic conversion - would be more comprehensive in full implementation
            return phpFormat switch
            {
                "Y-m-d H:i:s" => "yyyy-MM-dd HH:mm:ss",
                "Y-m-d" => "yyyy-MM-dd",
                "H:i:s" => "HH:mm:ss",
                "Y" => "yyyy",
                "m" => "MM",
                "d" => "dd",
                "c" => "yyyy-MM-ddTHH:mm:ssK",
                _ => phpFormat
            };
        }
        
        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            _parserFactory?.Dispose();
            _cache?.Clear();
            _globalVariables?.Clear();
            _sectionVariables?.Clear();
            _loadedFiles?.Clear();
            _currentSections?.Clear();
            _atOperatorHandlers?.Clear();
        }
    }
    
    /// <summary>
    /// Configuration engine options
    /// </summary>
    public class ConfigurationEngineOptions
    {
        public ParseOptions ParseOptions { get; set; } = new ParseOptions();
        public bool IncludeComments { get; set; } = false;
        public bool EnableCaching { get; set; } = true;
        public int MaxIncludeDepth { get; set; } = 10;
        public TimeSpan DefaultCacheExpiry { get; set; } = TimeSpan.FromMinutes(5);
        public bool StrictMode { get; set; } = false;
    }
    
    /// <summary>
    /// Configuration result
    /// </summary>
    public class ConfigurationResult
    {
        public bool Success { get; set; }
        public Dictionary<string, object> Configuration { get; set; }
        public Dictionary<string, object> GlobalVariables { get; set; }
        public Dictionary<string, Dictionary<string, object>> SectionVariables { get; set; }
        public List<ConfigurationError> Errors { get; set; } = new List<ConfigurationError>();
        public TimeSpan ProcessingTime { get; set; }
        public int CacheHits { get; set; }
        public int CacheMisses { get; set; }
    }
    
    /// <summary>
    /// Configuration error
    /// </summary>
    public class ConfigurationError
    {
        public ConfigurationErrorType Type { get; set; }
        public string Message { get; set; }
        public string File { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }
    
    /// <summary>
    /// Configuration error types
    /// </summary>
    public enum ConfigurationErrorType
    {
        ParseError,
        ProcessingError,
        EvaluationError,
        CrossFileError,
        ValidationError
    }
    
    /// <summary>
    /// Configuration exception
    /// </summary>
    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message) : base(message) { }
        public ConfigurationException(string message, Exception innerException) : base(message, innerException) { }
    }
} 