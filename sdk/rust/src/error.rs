use serde::{Deserialize, Serialize};
use std::fmt;
use std::error::Error as StdError;

/// Enhanced error types for TuskLang operations
#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum TuskError {
    /// Parse error with line number and context
    ParseError {
        line: usize,
        column: usize,
        message: String,
        context: String,
        suggestion: Option<String>,
    },
    /// Type conversion error
    TypeError {
        expected: String,
        found: String,
        context: String,
    },
    /// Variable interpolation error
    VariableError {
        variable: String,
        message: String,
        available_vars: Vec<String>,
    },
    /// File operation error
    FileError {
        path: String,
        operation: String,
        cause: String,
    },
    /// Validation error
    ValidationError {
        field: String,
        value: String,
        rule: String,
        message: String,
    },
    /// Serialization error
    SerializationError {
        format: String,
        message: String,
    },
    /// Configuration error
    ConfigError {
        section: String,
        message: String,
        details: Option<String>,
    },
    /// Generic error with context
    Generic {
        message: String,
        context: Option<String>,
        code: Option<String>,
    },
}

impl TuskError {
    /// Create a parse error with detailed context
    pub fn parse_error(line: usize, message: impl Into<String>) -> Self {
        Self::ParseError {
            line,
            column: 0,
            message: message.into(),
            context: String::new(),
            suggestion: None,
        }
    }

    /// Create a parse error with column information
    pub fn parse_error_with_context(
        line: usize,
        column: usize,
        message: impl Into<String>,
        context: impl Into<String>,
    ) -> Self {
        Self::ParseError {
            line,
            column,
            message: message.into(),
            context: context.into(),
            suggestion: None,
        }
    }

    /// Create a type error
    pub fn type_error(expected: impl Into<String>, found: impl Into<String>) -> Self {
        Self::TypeError {
            expected: expected.into(),
            found: found.into(),
            context: String::new(),
        }
    }

    /// Create a variable error
    pub fn variable_error(variable: impl Into<String>, message: impl Into<String>) -> Self {
        Self::VariableError {
            variable: variable.into(),
            message: message.into(),
            available_vars: Vec::new(),
        }
    }

    /// Create a file error
    pub fn file_error(path: impl Into<String>, operation: impl Into<String>, cause: impl Into<String>) -> Self {
        Self::FileError {
            path: path.into(),
            operation: operation.into(),
            cause: cause.into(),
        }
    }

    /// Create a validation error
    pub fn validation_error(
        field: impl Into<String>,
        value: impl Into<String>,
        rule: impl Into<String>,
        message: impl Into<String>,
    ) -> Self {
        Self::ValidationError {
            field: field.into(),
            value: value.into(),
            rule: rule.into(),
            message: message.into(),
        }
    }

    /// Get error code for programmatic handling
    pub fn error_code(&self) -> &str {
        match self {
            TuskError::ParseError { .. } => "PARSE_ERROR",
            TuskError::TypeError { .. } => "TYPE_ERROR",
            TuskError::VariableError { .. } => "VARIABLE_ERROR",
            TuskError::FileError { .. } => "FILE_ERROR",
            TuskError::ValidationError { .. } => "VALIDATION_ERROR",
            TuskError::SerializationError { .. } => "SERIALIZATION_ERROR",
            TuskError::ConfigError { .. } => "CONFIG_ERROR",
            TuskError::Generic { .. } => "GENERIC_ERROR",
        }
    }

    /// Get detailed error information for debugging
    pub fn debug_info(&self) -> String {
        match self {
            TuskError::ParseError { line, column, message, context, suggestion } => {
                let mut info = format!("Parse error at line {}, column {}: {}", line, column, message);
                if !context.is_empty() {
                    info.push_str(&format!("\nContext: {}", context));
                }
                if let Some(suggestion) = suggestion {
                    info.push_str(&format!("\nSuggestion: {}", suggestion));
                }
                info
            }
            TuskError::TypeError { expected, found, context } => {
                let mut info = format!("Type error: expected {}, found {}", expected, found);
                if !context.is_empty() {
                    info.push_str(&format!("\nContext: {}", context));
                }
                info
            }
            TuskError::VariableError { variable, message, available_vars } => {
                let mut info = format!("Variable error for '{}': {}", variable, message);
                if !available_vars.is_empty() {
                    info.push_str(&format!("\nAvailable variables: {}", available_vars.join(", ")));
                }
                info
            }
            TuskError::FileError { path, operation, cause } => {
                format!("File error during {} on '{}': {}", operation, path, cause)
            }
            TuskError::ValidationError { field, value, rule, message } => {
                format!("Validation error for field '{}' with value '{}' (rule: {}): {}", field, value, rule, message)
            }
            TuskError::SerializationError { format, message } => {
                format!("Serialization error for format '{}': {}", format, message)
            }
            TuskError::ConfigError { section, message, details } => {
                let mut info = format!("Configuration error in section '{}': {}", section, message);
                if let Some(details) = details {
                    info.push_str(&format!("\nDetails: {}", details));
                }
                info
            }
            TuskError::Generic { message, context, code } => {
                let mut info = format!("Generic error: {}", message);
                if let Some(context) = context {
                    info.push_str(&format!("\nContext: {}", context));
                }
                if let Some(code) = code {
                    info.push_str(&format!("\nCode: {}", code));
                }
                info
            }
        }
    }
}

impl fmt::Display for TuskError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            TuskError::ParseError { line, column, message, .. } => {
                write!(f, "Parse error at line {}, column {}: {}", line, column, message)
            }
            TuskError::TypeError { expected, found, .. } => {
                write!(f, "Type error: expected {}, found {}", expected, found)
            }
            TuskError::VariableError { variable, message, .. } => {
                write!(f, "Variable error for '{}': {}", variable, message)
            }
            TuskError::FileError { path, operation, cause } => {
                write!(f, "File error during {} on '{}': {}", operation, path, cause)
            }
            TuskError::ValidationError { field, message, .. } => {
                write!(f, "Validation error for field '{}': {}", field, message)
            }
            TuskError::SerializationError { format, message } => {
                write!(f, "Serialization error for format '{}': {}", format, message)
            }
            TuskError::ConfigError { section, message, .. } => {
                write!(f, "Configuration error in section '{}': {}", section, message)
            }
            TuskError::Generic { message, .. } => {
                write!(f, "Error: {}", message)
            }
        }
    }
}

impl StdError for TuskError {}

// From implementations for common error types
impl From<serde_json::Error> for TuskError {
    fn from(err: serde_json::Error) -> Self {
        TuskError::SerializationError {
            format: "JSON".to_string(),
            message: err.to_string(),
        }
    }
}

impl From<serde_yaml::Error> for TuskError {
    fn from(err: serde_yaml::Error) -> Self {
        TuskError::SerializationError {
            format: "YAML".to_string(),
            message: err.to_string(),
        }
    }
}

impl From<std::io::Error> for TuskError {
    fn from(err: std::io::Error) -> Self {
        TuskError::FileError {
            path: "unknown".to_string(),
            operation: "io".to_string(),
            cause: err.to_string(),
        }
    }
}

/// Result type for TuskLang operations
pub type TuskResult<T> = Result<T, TuskError>;

/// Error context for better debugging
#[derive(Debug, Clone)]
pub struct ErrorContext {
    pub file_path: Option<String>,
    pub line_number: Option<usize>,
    pub column_number: Option<usize>,
    pub source_line: Option<String>,
    pub stack_trace: Vec<String>,
}

impl ErrorContext {
    pub fn new() -> Self {
        Self {
            file_path: None,
            line_number: None,
            column_number: None,
            source_line: None,
            stack_trace: Vec::new(),
        }
    }

    pub fn with_file(mut self, path: impl Into<String>) -> Self {
        self.file_path = Some(path.into());
        self
    }

    pub fn with_location(mut self, line: usize, column: usize) -> Self {
        self.line_number = Some(line);
        self.column_number = Some(column);
        self
    }

    pub fn with_source_line(mut self, line: impl Into<String>) -> Self {
        self.source_line = Some(line.into());
        self
    }

    pub fn add_stack_frame(mut self, frame: impl Into<String>) -> Self {
        self.stack_trace.push(frame.into());
        self
    }
}

impl Default for ErrorContext {
    fn default() -> Self {
        Self::new()
    }
} 