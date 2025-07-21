using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TuskLang.Operators.ControlFlow
{
    /// <summary>
    /// Switch Operator for TuskLang C# SDK
    /// 
    /// Provides switch statement functionality with support for:
    /// - Multiple case conditions
    /// - Default case handling
    /// - Expression evaluation
    /// - Nested switch statements
    /// - Fall-through behavior
    /// 
    /// Usage:
    /// ```csharp
    /// // Basic switch
    /// var result = @switch({
    ///   expression: "value",
    ///   cases: {
    ///     "case1": "result1",
    ///     "case2": "result2",
    ///     "default": "default_result"
    ///   }
    /// })
    /// 
    /// // Switch with expressions
    /// var result = @switch({
    ///   expression: @variable("user_role"),
    ///   cases: {
    ///     "admin": @rbac({action: "check", role: "admin"}),
    ///     "user": @rbac({action: "check", role: "user"}),
    ///     "default": false
    ///   }
    /// })
    /// ```
    /// </summary>
    public class SwitchOperator : BaseOperator
    {
        public SwitchOperator()
        {
            Version = "2.0.0";
            RequiredFields = new List<string> { "expression", "cases" };
            OptionalFields = new List<string> 
            { 
                "strict", "fall_through", "default_case", "evaluate_expressions" 
            };
            
            DefaultConfig = new Dictionary<string, object>
            {
                ["strict"] = false,
                ["fall_through"] = false,
                ["evaluate_expressions"] = true
            };
        }
        
        public override string GetName() => "switch";
        
        protected override string GetDescription() => "Switch statement operator for conditional execution with multiple case handling and expression evaluation";
        
        protected override Dictionary<string, string> GetExamples()
        {
            return new Dictionary<string, string>
            {
                ["basic"] = "@switch({expression: \"value\", cases: {\"case1\": \"result1\", \"default\": \"default\"}})",
                ["with_expressions"] = "@switch({expression: @variable(\"role\"), cases: {\"admin\": @rbac(\"admin\"), \"user\": @rbac(\"user\")}})",
                ["strict_comparison"] = "@switch({expression: 123, cases: {\"123\": \"match\", \"default\": \"no_match\"}, strict: true})",
                ["fall_through"] = "@switch({expression: \"value\", cases: {\"case1\": \"result1\", \"case2\": \"result2\"}, fall_through: true})"
            };
        }
        
        protected override Dictionary<string, string> GetErrorCodes()
        {
            return new Dictionary<string, string>
            {
                ["INVALID_EXPRESSION"] = "Invalid switch expression",
                ["INVALID_CASES"] = "Invalid cases configuration",
                ["NO_MATCH"] = "No matching case found and no default provided",
                ["EVALUATION_FAILED"] = "Case expression evaluation failed",
                ["INVALID_CASE_KEY"] = "Invalid case key format"
            };
        }
        
        protected override async Task<object> ExecuteOperatorAsync(Dictionary<string, object> config, Dictionary<string, object> context)
        {
            var expression = ResolveVariable(config["expression"], context);
            var cases = ResolveVariable(config["cases"], context);
            var strict = GetContextValue<bool>(config, "strict", false);
            var fallThrough = GetContextValue<bool>(config, "fall_through", false);
            var evaluateExpressions = GetContextValue<bool>(config, "evaluate_expressions", true);
            
            if (cases == null || !(cases is Dictionary<string, object> casesDict))
            {
                throw new ArgumentException("Cases must be a dictionary of case conditions and results");
            }
            
            // Convert expression to string for comparison
            var expressionString = expression?.ToString() ?? "";
            
            // Check for exact match first
            if (casesDict.ContainsKey(expressionString))
            {
                var result = casesDict[expressionString];
                return await EvaluateResultAsync(result, context, evaluateExpressions);
            }
            
            // Check for type-specific matches if not strict
            if (!strict)
            {
                foreach (var kvp in casesDict)
                {
                    if (kvp.Key == "default") continue;
                    
                    // Try to match by converting types
                    if (TryMatchExpression(expression, kvp.Key))
                    {
                        var result = kvp.Value;
                        return await EvaluateResultAsync(result, context, evaluateExpressions);
                    }
                }
            }
            
            // Check for default case
            if (casesDict.ContainsKey("default"))
            {
                var defaultResult = casesDict["default"];
                return await EvaluateResultAsync(defaultResult, context, evaluateExpressions);
            }
            
            // No match found
            if (fallThrough)
            {
                // Return null for fall-through behavior
                return null;
            }
            
            throw new ArgumentException($"No matching case found for expression: {expressionString}");
        }
        
        /// <summary>
        /// Try to match expression with case key
        /// </summary>
        private bool TryMatchExpression(object expression, string caseKey)
        {
            if (expression == null) return caseKey == "null";
            
            // Try exact string match
            if (expression.ToString() == caseKey) return true;
            
            // Try numeric comparison
            if (double.TryParse(caseKey, out var caseNumber))
            {
                if (double.TryParse(expression.ToString(), out var exprNumber))
                {
                    return exprNumber == caseNumber;
                }
            }
            
            // Try boolean comparison
            if (bool.TryParse(caseKey, out var caseBool))
            {
                if (bool.TryParse(expression.ToString(), out var exprBool))
                {
                    return exprBool == caseBool;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Evaluate result expression
        /// </summary>
        private async Task<object> EvaluateResultAsync(object result, Dictionary<string, object> context, bool evaluateExpressions)
        {
            if (!evaluateExpressions) return result;
            
            // If result is a string that looks like an operator call, evaluate it
            if (result is string resultString && resultString.StartsWith("@"))
            {
                try
                {
                    // Parse operator call (simplified)
                    var operatorMatch = System.Text.RegularExpressions.Regex.Match(resultString, @"^@([a-zA-Z_][a-zA-Z0-9_]*)\((.+)\)$");
                    if (operatorMatch.Success)
                    {
                        var operatorName = operatorMatch.Groups[1].Value;
                        var parameters = operatorMatch.Groups[2].Value;
                        
                        // Create a simple config for the operator
                        var operatorConfig = new Dictionary<string, object>
                        {
                            ["data"] = parameters.Trim('"', '\'')
                        };
                        
                        // Execute the operator
                        if (OperatorRegistry.HasOperator(operatorName))
                        {
                            return await OperatorRegistry.ExecuteOperatorAsync(operatorName, operatorConfig, context);
                        }
                    }
                }
                catch
                {
                    // If evaluation fails, return the original string
                    return result;
                }
            }
            
            return result;
        }
        
        protected override ValidationResult CustomValidate(Dictionary<string, object> config)
        {
            var result = new ValidationResult();
            
            if (config.TryGetValue("cases", out var cases))
            {
                if (!(cases is Dictionary<string, object>))
                {
                    result.Errors.Add("Cases must be a dictionary");
                }
            }
            
            return result;
        }
    }
} 