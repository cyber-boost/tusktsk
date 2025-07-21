using System;
using System.Collections.Generic;

namespace TuskLang.Core
{
    /// <summary>
    /// Validation result for TuskTsk operations
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Whether the validation was successful
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// List of validation errors
        /// </summary>
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();

        /// <summary>
        /// List of validation warnings
        /// </summary>
        public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();

        /// <summary>
        /// Processing time for the validation
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }

        /// <summary>
        /// Validation timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Create a successful validation result
        /// </summary>
        public static ValidationResult Success(TimeSpan processingTime = default)
        {
            return new ValidationResult
            {
                IsValid = true,
                ProcessingTime = processingTime
            };
        }

        /// <summary>
        /// Create a failed validation result
        /// </summary>
        public static ValidationResult Failure(List<ValidationError> errors, TimeSpan processingTime = default)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = errors,
                ProcessingTime = processingTime
            };
        }

        /// <summary>
        /// Add an error to the validation result
        /// </summary>
        public void AddError(string message, string field = "", string errorCode = "")
        {
            Errors.Add(new ValidationError(message, field, errorCode));
            IsValid = false;
        }

        /// <summary>
        /// Add a warning to the validation result
        /// </summary>
        public void AddWarning(string message, string field = "", string warningCode = "")
        {
            Warnings.Add(new ValidationWarning(message, field, warningCode));
        }
    }

    /// <summary>
    /// Represents a validation error
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Error message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Field that caused the error
        /// </summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// Error code for categorization
        /// </summary>
        public string ErrorCode { get; set; } = string.Empty;

        /// <summary>
        /// Error timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public ValidationError(string message, string field = "", string errorCode = "")
        {
            Message = message;
            Field = field;
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Represents a validation warning
    /// </summary>
    public class ValidationWarning
    {
        /// <summary>
        /// Warning message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Field that caused the warning
        /// </summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// Warning code for categorization
        /// </summary>
        public string WarningCode { get; set; } = string.Empty;

        /// <summary>
        /// Warning timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public ValidationWarning(string message, string field = "", string warningCode = "")
        {
            Message = message;
            Field = field;
            WarningCode = warningCode;
        }
    }
} 