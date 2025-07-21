using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TuskLang.Operators
{
    /// <summary>
    /// Operator execution context and result
    /// </summary>
    public class OperatorExecution
    {
        /// <summary>
        /// Execution ID
        /// </summary>
        public string ExecutionId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Operator name
        /// </summary>
        public string OperatorName { get; set; } = string.Empty;

        /// <summary>
        /// Execution parameters
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Execution result
        /// </summary>
        public object Result { get; set; } = new object();

        /// <summary>
        /// Whether execution was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Execution error message
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Execution start time
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Execution end time
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Execution duration
        /// </summary>
        public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : DateTime.UtcNow - StartTime;

        /// <summary>
        /// Execution status
        /// </summary>
        public ExecutionStatus Status { get; set; } = ExecutionStatus.Pending;

        /// <summary>
        /// Execution metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Execution status enumeration
    /// </summary>
    public enum ExecutionStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled
    }
} 