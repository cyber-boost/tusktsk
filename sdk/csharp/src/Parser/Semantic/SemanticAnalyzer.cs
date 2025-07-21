using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TuskLang.Parser.Ast;

namespace TuskLang.Parser
{
    /// <summary>
    /// TuskTsk Semantic Analyzer - Validates configuration semantics and types
    /// 
    /// Performs comprehensive validation:
    /// - Type checking and compatibility
    /// - Variable reference validation
    /// - Cross-file reference validation
    /// - @ operator parameter validation
    /// - Configuration structure validation
    /// - Value range and constraint validation
    /// 
    /// Performance: Thread-safe analysis, comprehensive error reporting
    /// </summary>
    public class SemanticAnalyzer : IAstVisitor<TuskType>, IDisposable
    {
        private readonly List<SemanticError> _errors;
        private readonly List<SemanticWarning> _warnings;
        private readonly Dictionary<string, VariableInfo> _globalVariables;
        private readonly Dictionary<string, Dictionary<string, VariableInfo>> _sectionVariables;
        private readonly HashSet<string> _definedSections;
        private readonly Dictionary<string, List<string>> _crossFileReferences;
        private string _currentSection;
        private int _depth;
        private readonly SemanticAnalysisOptions _options;
        
        /// <summary>
        /// Initializes a new instance of the SemanticAnalyzer
        /// </summary>
        public SemanticAnalyzer(SemanticAnalysisOptions options = null)
        {
            _errors = new List<SemanticError>();
            _warnings = new List<SemanticWarning>();
            _globalVariables = new Dictionary<string, VariableInfo>();
            _sectionVariables = new Dictionary<string, Dictionary<string, VariableInfo>>();
            _definedSections = new HashSet<string>();
            _crossFileReferences = new Dictionary<string, List<string>>();
            _currentSection = "";
            _depth = 0;
            _options = options ?? new SemanticAnalysisOptions();
        }
        
        /// <summary>
        /// Analyze AST for semantic correctness
        /// </summary>
        public SemanticAnalysisResult Analyze(ConfigurationNode ast)
        {
            Reset();
            
            try
            {
                ast.Accept(this);
                ValidateCrossReferences();
                ValidateUnusedVariables();
            }
            catch (Exception ex)
            {
                _errors.Add(new SemanticError(
                    $"Semantic analysis failed: {ex.Message}", 
                    0, 
                    0, 
                    SemanticErrorType.InternalError));
            }
            
            return new SemanticAnalysisResult(_errors, _warnings, _globalVariables, _sectionVariables);
        }
        
        /// <summary>
        /// Reset analyzer state
        /// </summary>
        private void Reset()
        {
            _errors.Clear();
            _warnings.Clear();
            _globalVariables.Clear();
            _sectionVariables.Clear();
            _definedSections.Clear();
            _crossFileReferences.Clear();
            _currentSection = "";
            _depth = 0;
        }
        
        /// <summary>
        /// Visit configuration root
        /// </summary>
        public TuskType VisitConfiguration(ConfigurationNode node)
        {
            foreach (var statement in node.Children)
            {
                statement.Accept(this);
            }
            return TuskType.Unknown;
        }

        /// <summary>
        /// Visit comment node
        /// </summary>
        public TuskType VisitComment(CommentNode node)
        {
            // Comments don't affect semantic analysis
            return TuskType.Unknown;
        }

        /// <summary>
        /// Visit section node
        /// </summary>
        public TuskType VisitSection(SectionNode node)
        {
            _depth++;
            var previousSection = _currentSection;
            _currentSection = node.Name;

            if (_definedSections.Contains(node.Name))
            {
                AddWarning($"Duplicate section '{node.Name}'", node.Line, SemanticWarningType.DuplicateSection);
            }
            else
            {
                _definedSections.Add(node.Name);
            }

            if (!_sectionVariables.ContainsKey(node.Name))
            {
                _sectionVariables[node.Name] = new Dictionary<string, VariableInfo>();
            }

            foreach (var statement in node.Children)
            {
                statement.Accept(this);
            }

            _currentSection = previousSection;
            _depth--;
            return TuskType.Unknown;
        }

        /// <summary>
        /// Visit global variable node
        /// </summary>
        public TuskType VisitGlobalVariable(GlobalVariableNode node)
        {
            if (!IsValidVariableName(node.Name))
            {
                AddError($"Invalid variable name '{node.Name}'", node.Line, SemanticErrorType.InvalidIdentifier);
                return TuskType.Unknown;
            }

            if (_globalVariables.ContainsKey(node.Name))
            {
                AddWarning($"Variable '{node.Name}' redefined", node.Line, SemanticWarningType.VariableRedefinition);
            }

            var valueType = AnalyzeExpression(node.Value);
            _globalVariables[node.Name] = new VariableInfo
            {
                Name = node.Name,
                Type = valueType,
                Line = node.Line,
                IsGlobal = true,
                IsUsed = false,
                Section = ""
            };

            return TuskType.Unknown;
        }

        /// <summary>
        /// Visit assignment node
        /// </summary>
        public TuskType VisitAssignment(AssignmentNode node)
        {
            if (!IsValidVariableName(node.Name))
            {
                AddError($"Invalid variable name '{node.Name}'", node.Line, SemanticErrorType.InvalidIdentifier);
                return TuskType.Unknown;
            }

            var valueType = AnalyzeExpression(node.Value);
            var variableInfo = new VariableInfo
            {
                Name = node.Name,
                Type = valueType,
                Line = node.Line,
                IsGlobal = false,
                IsUsed = false,
                Section = _currentSection
            };

            if (string.IsNullOrEmpty(_currentSection))
            {
                if (_globalVariables.ContainsKey(node.Name))
                {
                    AddWarning($"Variable '{node.Name}' redefined", node.Line, SemanticWarningType.VariableRedefinition);
                }
                _globalVariables[node.Name] = variableInfo;
            }
            else
            {
                if (_sectionVariables[_currentSection].ContainsKey(node.Name))
                {
                    AddWarning($"Variable '{node.Name}' redefined in section '{_currentSection}'", node.Line, SemanticWarningType.VariableRedefinition);
                }
                _sectionVariables[_currentSection][node.Name] = variableInfo;
            }

            return TuskType.Unknown;
        }

        /// <summary>
        /// Visit include node
        /// </summary>
        public TuskType VisitInclude(IncludeNode node)
        {
            if (_options.ValidateCrossFileReferences)
            {
                if (!_crossFileReferences.ContainsKey(node.FilePath))
                {
                    _crossFileReferences[node.FilePath] = new List<string>();
                }
                _crossFileReferences[node.FilePath].Add(_currentSection);
            }
            return TuskType.Unknown;
        }
        
        /// <summary>
        /// Visit literal value
        /// </summary>
        public TuskType VisitLiteral(LiteralNode node)
        {
            return GetLiteralType(node.Value);
        }
        
        /// <summary>
        /// Visit string literal
        /// </summary>
        public TuskType VisitString(StringNode node)
        {
            if (node.IsTemplate)
            {
                // Validate template string syntax
                ValidateTemplateString(node.Value, node.Line);
            }
            
            return TuskType.String;
        }
        
        /// <summary>
        /// Visit variable reference
        /// </summary>
        public TuskType VisitVariableReference(VariableReferenceNode node)
        {
            var varName = node.Name;
            VariableInfo varInfo = null;
            
            if (node.IsGlobal)
            {
                // Global variable reference
                if (_globalVariables.TryGetValue(varName, out varInfo))
                {
                    varInfo.IsUsed = true;
                    return varInfo.Type;
                }
                else
                {
                    AddError($"Undefined global variable '${varName}'", 
                            node.Line, SemanticErrorType.UndefinedVariable);
                    return TuskType.Unknown;
                }
            }
            else
            {
                // Local variable reference - check current section first, then global
                var currentSectionVars = _sectionVariables.GetValueOrDefault(_currentSection);
                if (currentSectionVars?.TryGetValue(varName, out varInfo) == true)
                {
                    varInfo.IsUsed = true;
                    return varInfo.Type;
                }
                else if (_globalVariables.TryGetValue(varName, out varInfo))
                {
                    varInfo.IsUsed = true;
                    return varInfo.Type;
                }
                else
                {
                    AddError($"Undefined variable '{varName}'", 
                            node.Line, SemanticErrorType.UndefinedVariable);
                    return TuskType.Unknown;
                }
            }
        }
        
        /// <summary>
        /// Visit binary operator
        /// </summary>
        public TuskType VisitBinaryOperator(BinaryOperatorNode node)
        {
            var leftType = AnalyzeExpression(node.Left);
            var rightType = AnalyzeExpression(node.Right);
            
            return ValidateBinaryOperation(node.Operator, leftType, rightType, node.Line);
        }
        
        /// <summary>
        /// Visit unary operator
        /// </summary>
        public TuskType VisitUnaryOperator(UnaryOperatorNode node)
        {
            var operandType = AnalyzeExpression(node.Expression);
            
            return ValidateUnaryOperation(node.Operator, operandType, node.Line);
        }
        
        /// <summary>
        /// Visit ternary conditional
        /// </summary>
        public TuskType VisitTernary(TernaryNode node)
        {
            var conditionType = AnalyzeExpression(node.Condition);
            var trueType = AnalyzeExpression(node.TrueValue);
            var falseType = AnalyzeExpression(node.FalseValue);
            
            // Condition should be boolean-convertible
            if (!IsConvertibleToBoolean(conditionType))
            {
                AddWarning("Ternary condition may not be boolean", 
                          node.Line, SemanticWarningType.ImplicitConversion);
            }
            
            // Return common type of true and false branches
            return GetCommonType(trueType, falseType);
        }
        
        /// <summary>
        /// Visit range expression
        /// </summary>
        public TuskType VisitRange(RangeNode node)
        {
            var startType = AnalyzeExpression(node.Start);
            var endType = AnalyzeExpression(node.End);
            
            if (!IsNumericType(startType) || !IsNumericType(endType))
            {
                AddError("Range values must be numeric", 
                        node.Line, SemanticErrorType.TypeMismatch);
            }
            
            return TuskType.Range;
        }
        
        /// <summary>
        /// Visit array
        /// </summary>
        public TuskType VisitArray(ArrayNode node)
        {
            var elementTypes = new List<TuskType>();
            
            foreach (var element in node.Elements)
            {
                var elementType = AnalyzeExpression(element);
                elementTypes.Add(elementType);
            }
            
            // Check for type consistency
            if (elementTypes.Count > 1)
            {
                var firstType = elementTypes[0];
                if (!elementTypes.All(t => AreTypesCompatible(firstType, t)))
                {
                    AddWarning("Array contains mixed types", 
                              node.Line, SemanticWarningType.MixedArrayTypes);
                }
            }
            
            return TuskType.Array;
        }
        
        /// <summary>
        /// Visit object
        /// </summary>
        public TuskType VisitObject(ObjectNode node)
        {
            var duplicateKeys = new HashSet<string>();
            
            foreach (var property in node.Properties)
            {
                var key = property.Key;
                var value = property.Value;
                
                // Check for duplicate keys
                if (!duplicateKeys.Add(key))
                {
                    AddWarning($"Duplicate object key '{key}'", 
                              node.Line, SemanticWarningType.DuplicateKey);
                }
                
                // Analyze value
                AnalyzeExpression(value);
            }
            
            return TuskType.Object;
        }
        
        /// <summary>
        /// Visit named object
        /// </summary>
        public TuskType VisitNamedObject(NamedObjectNode node)
        {
            // Validate object name
            if (!IsValidIdentifier(node.Name))
            {
                AddError($"Invalid object name '{node.Name}'", 
                        node.Line, SemanticErrorType.InvalidIdentifier);
            }
            
            // Analyze as regular object
            var objectType = VisitObject(new ObjectNode(node.Properties));
            
            return objectType;
        }
        
        /// <summary>
        /// Visit @ operator
        /// </summary>
        public TuskType VisitAtOperator(AtOperatorNode node)
        {
            var operatorName = node.OperatorName;
            var args = node.Arguments;
            
            return ValidateAtOperator(operatorName, args, node.Line);
        }
        
        /// <summary>
        /// Visit cross-file operator
        /// </summary>
        public TuskType VisitCrossFileOperator(CrossFileOperatorNode node)
        {
            var fileName = node.FilePath;
            var methodName = node.MethodName;
            var args = node.Arguments;
            
            // Record cross-file reference for later validation
            if (!_crossFileReferences.ContainsKey(fileName))
            {
                _crossFileReferences[fileName] = new List<string>();
            }
            _crossFileReferences[fileName].Add($"{methodName}({args.Length} args)");
            
            // Validate method name
            if (methodName != "get" && methodName != "set" && methodName != "exists")
            {
                AddWarning($"Unknown cross-file method '{methodName}'", 
                          node.Line, SemanticWarningType.UnknownMethod);
            }
            
            // Validate arguments based on method
            switch (methodName)
            {
                case "get":
                case "exists":
                    if (args.Length != 1)
                    {
                        AddError($"Method '{methodName}' expects 1 argument, got {args.Length}", 
                                node.Line, SemanticErrorType.WrongArgumentCount);
                    }
                    else if (AnalyzeExpression(args[0]) != TuskType.String)
                    {
                        AddError($"Method '{methodName}' expects string argument", 
                                node.Line, SemanticErrorType.TypeMismatch);
                    }
                    break;
                    
                case "set":
                    if (args.Length != 2)
                    {
                        AddError($"Method 'set' expects 2 arguments, got {args.Length}", 
                                node.Line, SemanticErrorType.WrongArgumentCount);
                    }
                    else if (AnalyzeExpression(args[0]) != TuskType.String)
                    {
                        AddError("Method 'set' expects string key as first argument", 
                                node.Line, SemanticErrorType.TypeMismatch);
                    }
                    break;
            }
            
            return methodName == "exists" ? TuskType.Boolean : TuskType.Unknown;
        }
        
        /// <summary>
        /// Visit property access
        /// </summary>
        public TuskType VisitPropertyAccess(PropertyAccessNode node)
        {
            var objectType = AnalyzeExpression(node.Object);
            
            if (objectType != TuskType.Object && objectType != TuskType.Unknown)
            {
                AddError($"Cannot access property '{node.Property}' on non-object type", 
                        node.Line, SemanticErrorType.InvalidPropertyAccess);
            }
            
            return TuskType.Unknown; // Property type is unknown without schema
        }
        
        /// <summary>
        /// Visit method call
        /// </summary>
        public TuskType VisitMethodCall(MethodCallNode node)
        {
            var objectType = AnalyzeExpression(node.Object);
            
            foreach (var arg in node.Arguments)
            {
                AnalyzeExpression(arg);
            }
            
            return TuskType.Unknown; // Method return type is unknown without schema
        }
        
        /// <summary>
        /// Visit index access
        /// </summary>
        public TuskType VisitIndexAccess(IndexAccessNode node)
        {
            var objectType = AnalyzeExpression(node.Object);
            var indexType = AnalyzeExpression(node.Index);
            
            if (objectType != TuskType.Array && objectType != TuskType.Object && objectType != TuskType.Unknown)
            {
                AddError("Cannot index non-array, non-object type", 
                        node.Line, SemanticErrorType.InvalidIndexAccess);
            }
            
            if (objectType == TuskType.Array && indexType != TuskType.Integer)
            {
                AddError("Array index must be integer", 
                        node.Line, SemanticErrorType.TypeMismatch);
            }
            else if (objectType == TuskType.Object && indexType != TuskType.String)
            {
                AddError("Object index must be string", 
                        node.Line, SemanticErrorType.TypeMismatch);
            }
            
            return TuskType.Unknown;
        }
        
        /// <summary>
        /// Visit grouping
        /// </summary>
        public TuskType VisitGrouping(GroupingNode node)
        {
            return AnalyzeExpression(node.Expression);
        }

        /// <summary>
        /// Visit expression node (base case)
        /// </summary>
        public TuskType VisitExpression(ExpressionNode node)
        {
            // This is a base case that should not be called directly
            // All specific expression types should be handled by their specific visitors
            return TuskType.Unknown;
        }
        
        /// <summary>
        /// Analyze expression and return type
        /// </summary>
        private TuskType AnalyzeExpression(AstNode expr)
        {
            return expr.Accept(this);
        }
        
        /// <summary>
        /// Validate @ operator usage
        /// </summary>
        private TuskType ValidateAtOperator(string operatorName, AstNode[] args, int line)
        {
            switch (operatorName.ToLower())
            {
                case "env":
                    if (args.Length < 1 || args.Length > 2)
                    {
                        AddError($"@env expects 1-2 arguments, got {args.Length}", 
                                line, SemanticErrorType.WrongArgumentCount);
                    }
                    else
                    {
                        if (AnalyzeExpression(args[0]) != TuskType.String)
                        {
                            AddError("@env first argument must be string", 
                                    line, SemanticErrorType.TypeMismatch);
                        }
                    }
                    return TuskType.String;
                    
                case "query":
                    if (args.Length != 1)
                    {
                        AddError($"@query expects 1 argument, got {args.Length}", 
                                line, SemanticErrorType.WrongArgumentCount);
                    }
                    else if (AnalyzeExpression(args[0]) != TuskType.String)
                    {
                        AddError("@query argument must be string", 
                                line, SemanticErrorType.TypeMismatch);
                    }
                    return TuskType.Unknown; // Query result type is unknown
                    
                case "date":
                    if (args.Length > 1)
                    {
                        AddError($"@date expects 0-1 arguments, got {args.Length}", 
                                line, SemanticErrorType.WrongArgumentCount);
                    }
                    else if (args.Length == 1 && AnalyzeExpression(args[0]) != TuskType.String)
                    {
                        AddError("@date format argument must be string", 
                                line, SemanticErrorType.TypeMismatch);
                    }
                    return TuskType.String;
                    
                case "cache":
                case "metrics":
                case "optimize":
                case "learn":
                    // These operators have flexible argument types
                    foreach (var arg in args)
                    {
                        AnalyzeExpression(arg);
                    }
                    return TuskType.Unknown;
                    
                default:
                    AddWarning($"Unknown @ operator '{operatorName}'", 
                              line, SemanticWarningType.UnknownOperator);
                    return TuskType.Unknown;
            }
        }
        
        /// <summary>
        /// Validate binary operation
        /// </summary>
        private TuskType ValidateBinaryOperation(string operatorSymbol, TuskType leftType, TuskType rightType, int line)
        {
            switch (operatorSymbol)
            {
                case "Plus":
                    // String concatenation or numeric addition
                    if (leftType == TuskType.String || rightType == TuskType.String)
                    {
                        return TuskType.String;
                    }
                    if (IsNumericType(leftType) && IsNumericType(rightType))
                    {
                        return GetNumericResultType(leftType, rightType);
                    }
                    AddError($"Cannot apply '+' operator to {leftType} and {rightType}", 
                            line, SemanticErrorType.TypeMismatch);
                    return TuskType.Unknown;
                    
                case "Minus":
                case "Multiply":
                case "Divide":
                case "Modulo":
                    if (IsNumericType(leftType) && IsNumericType(rightType))
                    {
                        return GetNumericResultType(leftType, rightType);
                    }
                    AddError($"Cannot apply '{operatorSymbol}' operator to {leftType} and {rightType}", 
                            line, SemanticErrorType.TypeMismatch);
                    return TuskType.Unknown;
                    
                case "EqualEqual":
                case "BangEqual":
                    return TuskType.Boolean;
                    
                case "Greater":
                case "GreaterEqual":
                case "Less":
                case "LessEqual":
                    if (IsComparableType(leftType) && IsComparableType(rightType))
                    {
                        return TuskType.Boolean;
                    }
                    AddError($"Cannot compare {leftType} and {rightType}", 
                            line, SemanticErrorType.TypeMismatch);
                    return TuskType.Boolean;
                    
                case "AndAnd":
                case "OrOr":
                case "LogicalAnd":
                case "LogicalOr":
                    return TuskType.Boolean;
                    
                default:
                    AddWarning($"Unknown binary operator '{operatorSymbol}'", 
                              line, SemanticWarningType.UnknownOperator);
                    return TuskType.Unknown;
            }
        }
        
        /// <summary>
        /// Validate unary operation
        /// </summary>
        private TuskType ValidateUnaryOperation(string operatorSymbol, TuskType operandType, int line)
        {
            switch (operatorSymbol)
            {
                case "Minus":
                case "Plus":
                    if (IsNumericType(operandType))
                    {
                        return operandType;
                    }
                    AddError($"Cannot apply unary '{operatorSymbol}' to {operandType}", 
                            line, SemanticErrorType.TypeMismatch);
                    return TuskType.Unknown;
                    
                case "Exclamation":
                case "LogicalNot":
                    return TuskType.Boolean;
                    
                default:
                    AddWarning($"Unknown unary operator '{operatorSymbol}'", 
                              line, SemanticWarningType.UnknownOperator);
                    return TuskType.Unknown;
            }
        }
        
        /// <summary>
        /// Validate cross-file references
        /// </summary>
        private void ValidateCrossReferences()
        {
            if (!_options.ValidateCrossFileReferences) return;
            
            foreach (var reference in _crossFileReferences)
            {
                var fileName = reference.Key;
                var methods = reference.Value;
                
                // In a complete implementation, this would check if the referenced files exist
                // and if the methods are available
                AddWarning($"Cross-file reference to '{fileName}' cannot be validated statically", 
                          0, SemanticWarningType.UnvalidatedCrossReference);
            }
        }
        
        /// <summary>
        /// Validate unused variables
        /// </summary>
        private void ValidateUnusedVariables()
        {
            if (!_options.WarnUnusedVariables) return;
            
            foreach (var globalVar in _globalVariables.Values)
            {
                if (!globalVar.IsUsed)
                {
                    AddWarning($"Global variable '${globalVar.Name}' is defined but never used", 
                              globalVar.Line, SemanticWarningType.UnusedVariable);
                }
            }
            
            foreach (var section in _sectionVariables)
            {
                foreach (var variable in section.Value.Values)
                {
                    if (!variable.IsUsed)
                    {
                        AddWarning($"Variable '{variable.Name}' in section '{section.Key}' is defined but never used", 
                                  variable.Line, SemanticWarningType.UnusedVariable);
                    }
                }
            }
        }
        
        /// <summary>
        /// Validate template string syntax
        /// </summary>
        private void ValidateTemplateString(string value, int line)
        {
            // Look for ${variable} patterns
            var templatePattern = new Regex(@"\$\{([^}]+)\}");
            var matches = templatePattern.Matches(value);
            
            foreach (Match match in matches)
            {
                var varName = match.Groups[1].Value;
                if (!IsValidIdentifier(varName))
                {
                    AddError($"Invalid template variable '${{{varName}}}'", 
                            line, SemanticErrorType.InvalidIdentifier);
                }
            }
        }
        
        /// <summary>
        /// Get literal type
        /// </summary>
        private TuskType GetLiteralType(object value)
        {
            return value switch
            {
                null => TuskType.Null,
                bool => TuskType.Boolean,
                int => TuskType.Integer,
                long => TuskType.Long,
                double => TuskType.Double,
                float => TuskType.Double,
                string => TuskType.String,
                _ => TuskType.Unknown
            };
        }
        
        /// <summary>
        /// Check if type is numeric
        /// </summary>
        private bool IsNumericType(TuskType type)
        {
            return type == TuskType.Integer || type == TuskType.Long || type == TuskType.Double;
        }
        
        /// <summary>
        /// Check if type is comparable
        /// </summary>
        private bool IsComparableType(TuskType type)
        {
            return IsNumericType(type) || type == TuskType.String;
        }
        
        /// <summary>
        /// Check if type is convertible to boolean
        /// </summary>
        private bool IsConvertibleToBoolean(TuskType type)
        {
            return type == TuskType.Boolean || IsNumericType(type) || type == TuskType.String;
        }
        
        /// <summary>
        /// Get numeric result type
        /// </summary>
        private TuskType GetNumericResultType(TuskType leftType, TuskType rightType)
        {
            if (leftType == TuskType.Double || rightType == TuskType.Double)
                return TuskType.Double;
            if (leftType == TuskType.Long || rightType == TuskType.Long)
                return TuskType.Long;
            return TuskType.Integer;
        }
        
        /// <summary>
        /// Get common type of two types
        /// </summary>
        private TuskType GetCommonType(TuskType type1, TuskType type2)
        {
            if (type1 == type2) return type1;
            if (IsNumericType(type1) && IsNumericType(type2))
                return GetNumericResultType(type1, type2);
            return TuskType.Unknown;
        }
        
        /// <summary>
        /// Check if types are compatible
        /// </summary>
        private bool AreTypesCompatible(TuskType type1, TuskType type2)
        {
            if (type1 == type2) return true;
            if (IsNumericType(type1) && IsNumericType(type2)) return true;
            return false;
        }
        
        /// <summary>
        /// Check if identifier is valid
        /// </summary>
        private bool IsValidIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (!char.IsLetter(name[0]) && name[0] != '_') return false;
            return name.All(c => char.IsLetterOrDigit(c) || c == '_');
        }
        
        /// <summary>
        /// Check if variable name is valid
        /// </summary>
        private bool IsValidVariableName(string name)
        {
            return IsValidIdentifier(name);
        }
        
        /// <summary>
        /// Add semantic error
        /// </summary>
        private void AddError(string message, int line, SemanticErrorType type)
        {
            _errors.Add(new SemanticError(message, line, 0, type));
        }
        
        /// <summary>
        /// Add semantic warning
        /// </summary>
        private void AddWarning(string message, int line, SemanticWarningType type)
        {
            _warnings.Add(new SemanticWarning(message, line, 0, type));
        }
        
        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            _errors?.Clear();
            _warnings?.Clear();
            _globalVariables?.Clear();
            _sectionVariables?.Clear();
            _definedSections?.Clear();
            _crossFileReferences?.Clear();
        }
    }
    
    /// <summary>
    /// TuskTsk type system
    /// </summary>
    public enum TuskType
    {
        Unknown,
        Null,
        Boolean,
        Integer,
        Long,
        Double,
        String,
        Array,
        Object,
        Range
    }
    
    /// <summary>
    /// Variable information
    /// </summary>
    public class VariableInfo
    {
        public string Name { get; set; }
        public TuskType Type { get; set; }
        public int Line { get; set; }
        public bool IsGlobal { get; set; }
        public bool IsUsed { get; set; }
        public string Section { get; set; }
    }
    
    /// <summary>
    /// Semantic analysis options
    /// </summary>
    public class SemanticAnalysisOptions
    {
        public bool ValidateCrossFileReferences { get; set; } = false;
        public bool WarnUnusedVariables { get; set; } = true;
        public bool StrictTypeChecking { get; set; } = false;
        public bool AllowImplicitConversions { get; set; } = true;
    }
    
    /// <summary>
    /// Semantic analysis result
    /// </summary>
    public class SemanticAnalysisResult
    {
        public List<SemanticError> Errors { get; }
        public List<SemanticWarning> Warnings { get; }
        public Dictionary<string, VariableInfo> GlobalVariables { get; }
        public Dictionary<string, Dictionary<string, VariableInfo>> SectionVariables { get; }
        public bool IsValid => Errors.Count == 0;
        
        public SemanticAnalysisResult(
            List<SemanticError> errors, 
            List<SemanticWarning> warnings,
            Dictionary<string, VariableInfo> globalVariables,
            Dictionary<string, Dictionary<string, VariableInfo>> sectionVariables)
        {
            Errors = errors ?? new List<SemanticError>();
            Warnings = warnings ?? new List<SemanticWarning>();
            GlobalVariables = globalVariables ?? new Dictionary<string, VariableInfo>();
            SectionVariables = sectionVariables ?? new Dictionary<string, Dictionary<string, VariableInfo>>();
        }
    }
    
    /// <summary>
    /// Semantic error
    /// </summary>
    public class SemanticError
    {
        public string Message { get; }
        public int Line { get; }
        public int Column { get; }
        public SemanticErrorType Type { get; }
        
        public SemanticError(string message, int line, int column, SemanticErrorType type)
        {
            Message = message;
            Line = line;
            Column = column;
            Type = type;
        }
        
        public override string ToString()
        {
            return $"Semantic error at line {Line}, column {Column}: {Message}";
        }
    }
    
    /// <summary>
    /// Semantic warning
    /// </summary>
    public class SemanticWarning
    {
        public string Message { get; }
        public int Line { get; }
        public int Column { get; }
        public SemanticWarningType Type { get; }
        
        public SemanticWarning(string message, int line, int column, SemanticWarningType type)
        {
            Message = message;
            Line = line;
            Column = column;
            Type = type;
        }
        
        public override string ToString()
        {
            return $"Semantic warning at line {Line}, column {Column}: {Message}";
        }
    }
    
    /// <summary>
    /// Semantic error types
    /// </summary>
    public enum SemanticErrorType
    {
        InvalidIdentifier,
        UndefinedVariable,
        TypeMismatch,
        WrongArgumentCount,
        InvalidPropertyAccess,
        InvalidIndexAccess,
        InternalError
    }
    
    /// <summary>
    /// Semantic warning types
    /// </summary>
    public enum SemanticWarningType
    {
        VariableRedefinition,
        DuplicateSection,
        DuplicateKey,
        MixedArrayTypes,
        ImplicitConversion,
        UnknownOperator,
        UnknownMethod,
        UnusedVariable,
        UnvalidatedCrossReference
    }
} 