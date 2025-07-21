using System;
using System.Collections.Generic;

namespace TuskLang.Parser
{
    /// <summary>
    /// Result of parsing a TuskTsk configuration file
    /// </summary>
    public class ParserResult
    {
        /// <summary>
        /// Whether the parsing was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// The parsed AST root node
        /// </summary>
        public Ast.AstNode? RootNode { get; set; }

        /// <summary>
        /// List of parsing errors
        /// </summary>
        public List<ParserError> Errors { get; set; } = new List<ParserError>();

        /// <summary>
        /// List of parsing warnings
        /// </summary>
        public List<ParserWarning> Warnings { get; set; } = new List<ParserWarning>();

        /// <summary>
        /// Processing time for the parsing operation
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }

        /// <summary>
        /// Source file that was parsed
        /// </summary>
        public string SourceFile { get; set; } = string.Empty;

        /// <summary>
        /// Raw content that was parsed
        /// </summary>
        public string RawContent { get; set; } = string.Empty;

        /// <summary>
        /// Create a successful parser result
        /// </summary>
        public static ParserResult Success(Ast.AstNode rootNode, string sourceFile, string rawContent, TimeSpan processingTime)
        {
            return new ParserResult
            {
                IsSuccess = true,
                RootNode = rootNode,
                SourceFile = sourceFile,
                RawContent = rawContent,
                ProcessingTime = processingTime
            };
        }

        /// <summary>
        /// Create a failed parser result
        /// </summary>
        public static ParserResult Failure(List<ParserError> errors, string sourceFile, string rawContent, TimeSpan processingTime)
        {
            return new ParserResult
            {
                IsSuccess = false,
                Errors = errors,
                SourceFile = sourceFile,
                RawContent = rawContent,
                ProcessingTime = processingTime
            };
        }
    }

    /// <summary>
    /// Represents a parsing error
    /// </summary>
    public class ParserError
    {
        /// <summary>
        /// Error message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Line number where the error occurred
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// Column number where the error occurred
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// Error code for categorization
        /// </summary>
        public string ErrorCode { get; set; } = string.Empty;

        /// <summary>
        /// Source file where the error occurred
        /// </summary>
        public string SourceFile { get; set; } = string.Empty;

        public ParserError(string message, int line, int column, string errorCode = "", string sourceFile = "")
        {
            Message = message;
            Line = line;
            Column = column;
            ErrorCode = errorCode;
            SourceFile = sourceFile;
        }
    }

    /// <summary>
    /// Represents a parsing warning
    /// </summary>
    public class ParserWarning
    {
        /// <summary>
        /// Warning message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Line number where the warning occurred
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// Column number where the warning occurred
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// Warning code for categorization
        /// </summary>
        public string WarningCode { get; set; } = string.Empty;

        /// <summary>
        /// Source file where the warning occurred
        /// </summary>
        public string SourceFile { get; set; } = string.Empty;

        public ParserWarning(string message, int line, int column, string warningCode = "", string sourceFile = "")
        {
            Message = message;
            Line = line;
            Column = column;
            WarningCode = warningCode;
            SourceFile = sourceFile;
        }
    }
} 