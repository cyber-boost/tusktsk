use thiserror::Error;

/// Result type for TuskLang operations
pub type TuskResult<T> = Result<T, TuskError>;

/// Error types for TuskLang parsing and operations
#[derive(Error, Debug, Clone, PartialEq)]
pub enum TuskError {
    #[error("Parse error at line {line}: {message}")]
    ParseError {
        line: usize,
        message: String,
    },

    #[error("Invalid indentation at line {line}: expected {expected}, got {actual}")]
    IndentationError {
        line: usize,
        expected: usize,
        actual: usize,
    },

    #[error("Unexpected token '{token}' at line {line}")]
    UnexpectedToken {
        line: usize,
        token: String,
    },

    #[error("Missing value for key '{key}' at line {line}")]
    MissingValue {
        line: usize,
        key: String,
    },

    #[error("Invalid value '{value}' at line {line}: {reason}")]
    InvalidValue {
        line: usize,
        value: String,
        reason: String,
    },

    #[error("Variable '{variable}' not found")]
    VariableNotFound {
        variable: String,
    },

    #[error("Circular reference detected for variable '{variable}'")]
    CircularReference {
        variable: String,
    },

    #[error("IO error: {message}")]
    IoError {
        message: String,
    },

    #[error("Serialization error: {message}")]
    SerializationError {
        message: String,
    },

    #[error("Type conversion error: {message}")]
    TypeConversionError {
        message: String,
    },

    #[error("Validation error: {message}")]
    ValidationError {
        message: String,
    },
}

impl TuskError {
    /// Create a parse error with line number and message
    pub fn parse_error(line: usize, message: impl Into<String>) -> Self {
        Self::ParseError {
            line,
            message: message.into(),
        }
    }

    /// Create an indentation error
    pub fn indentation_error(line: usize, expected: usize, actual: usize) -> Self {
        Self::IndentationError {
            line,
            expected,
            actual,
        }
    }

    /// Create an unexpected token error
    pub fn unexpected_token(line: usize, token: impl Into<String>) -> Self {
        Self::UnexpectedToken {
            line,
            token: token.into(),
        }
    }

    /// Create a missing value error
    pub fn missing_value(line: usize, key: impl Into<String>) -> Self {
        Self::MissingValue {
            line,
            key: key.into(),
        }
    }

    /// Create an invalid value error
    pub fn invalid_value(line: usize, value: impl Into<String>, reason: impl Into<String>) -> Self {
        Self::InvalidValue {
            line,
            value: value.into(),
            reason: reason.into(),
        }
    }

    /// Create a variable not found error
    pub fn variable_not_found(variable: impl Into<String>) -> Self {
        Self::VariableNotFound {
            variable: variable.into(),
        }
    }

    /// Create a circular reference error
    pub fn circular_reference(variable: impl Into<String>) -> Self {
        Self::CircularReference {
            variable: variable.into(),
        }
    }

    /// Create an IO error
    pub fn io_error(message: impl Into<String>) -> Self {
        Self::IoError {
            message: message.into(),
        }
    }

    /// Create a serialization error
    pub fn serialization_error(message: impl Into<String>) -> Self {
        Self::SerializationError {
            message: message.into(),
        }
    }

    /// Create a type conversion error
    pub fn type_conversion_error(message: impl Into<String>) -> Self {
        Self::TypeConversionError {
            message: message.into(),
        }
    }

    /// Create a validation error
    pub fn validation_error(message: impl Into<String>) -> Self {
        Self::ValidationError {
            message: message.into(),
        }
    }

    /// Get the line number where the error occurred
    pub fn line_number(&self) -> Option<usize> {
        match self {
            Self::ParseError { line, .. } => Some(*line),
            Self::IndentationError { line, .. } => Some(*line),
            Self::UnexpectedToken { line, .. } => Some(*line),
            Self::MissingValue { line, .. } => Some(*line),
            Self::InvalidValue { line, .. } => Some(*line),
            _ => None,
        }
    }

    /// Check if this is a parsing error
    pub fn is_parse_error(&self) -> bool {
        matches!(
            self,
            Self::ParseError { .. }
                | Self::IndentationError { .. }
                | Self::UnexpectedToken { .. }
                | Self::MissingValue { .. }
                | Self::InvalidValue { .. }
        )
    }

    /// Check if this is a variable-related error
    pub fn is_variable_error(&self) -> bool {
        matches!(
            self,
            Self::VariableNotFound { .. } | Self::CircularReference { .. }
        )
    }
}

impl From<std::io::Error> for TuskError {
    fn from(err: std::io::Error) -> Self {
        Self::io_error(err.to_string())
    }
}

impl From<serde_json::Error> for TuskError {
    fn from(err: serde_json::Error) -> Self {
        Self::serialization_error(err.to_string())
    }
}

impl From<serde_yaml::Error> for TuskError {
    fn from(err: serde_yaml::Error) -> Self {
        Self::serialization_error(err.to_string())
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_parse_error() {
        let error = TuskError::parse_error(5, "Invalid syntax");
        assert_eq!(error.line_number(), Some(5));
        assert!(error.is_parse_error());
    }

    #[test]
    fn test_variable_error() {
        let error = TuskError::variable_not_found("my_var");
        assert_eq!(error.line_number(), None);
        assert!(error.is_variable_error());
    }

    #[test]
    fn test_error_conversion() {
        let io_error = std::io::Error::new(std::io::ErrorKind::NotFound, "File not found");
        let tusk_error: TuskError = io_error.into();
        assert!(matches!(tusk_error, TuskError::IoError { .. }));
    }
} 