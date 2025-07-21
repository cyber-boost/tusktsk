package error

import (
	"fmt"
	"runtime"
	"strings"
	"time"
)

// ErrorHandler represents an error handler
type ErrorHandler struct {
	errors []Error
}

// New creates a new ErrorHandler instance
func New() *ErrorHandler {
	return &ErrorHandler{
		errors: []Error{},
	}
}

// Error represents a TuskLang error
type Error struct {
	Type      ErrorType
	Message   string
	File      string
	Line      int
	Column    int
	Timestamp time.Time
	Stack     string
	Context   map[string]interface{}
}

// ErrorType represents the type of error
type ErrorType int

const (
	ErrorTypeSyntax ErrorType = iota
	ErrorTypeSemantic
	ErrorTypeRuntime
	ErrorTypeCompilation
	ErrorTypeSecurity
	ErrorTypeValidation
	ErrorTypeIO
	ErrorTypeNetwork
	ErrorTypeUnknown
)

// String returns the string representation of the error type
func (et ErrorType) String() string {
	switch et {
	case ErrorTypeSyntax:
		return "Syntax"
	case ErrorTypeSemantic:
		return "Semantic"
	case ErrorTypeRuntime:
		return "Runtime"
	case ErrorTypeCompilation:
		return "Compilation"
	case ErrorTypeSecurity:
		return "Security"
	case ErrorTypeValidation:
		return "Validation"
	case ErrorTypeIO:
		return "IO"
	case ErrorTypeNetwork:
		return "Network"
	case ErrorTypeUnknown:
		return "Unknown"
	default:
		return "Unknown"
	}
}

// NewError creates a new error
func (eh *ErrorHandler) NewError(errorType ErrorType, message string, file string, line, column int) *Error {
	// Get stack trace
	stack := eh.getStackTrace()
	
	error := &Error{
		Type:      errorType,
		Message:   message,
		File:      file,
		Line:      line,
		Column:    column,
		Timestamp: time.Now(),
		Stack:     stack,
		Context:   make(map[string]interface{}),
	}
	
	eh.errors = append(eh.errors, *error)
	return error
}

// NewSyntaxError creates a new syntax error
func (eh *ErrorHandler) NewSyntaxError(message string, file string, line, column int) *Error {
	return eh.NewError(ErrorTypeSyntax, message, file, line, column)
}

// NewSemanticError creates a new semantic error
func (eh *ErrorHandler) NewSemanticError(message string, file string, line, column int) *Error {
	return eh.NewError(ErrorTypeSemantic, message, file, line, column)
}

// NewRuntimeError creates a new runtime error
func (eh *ErrorHandler) NewRuntimeError(message string, file string, line, column int) *Error {
	return eh.NewError(ErrorTypeRuntime, message, file, line, column)
}

// NewCompilationError creates a new compilation error
func (eh *ErrorHandler) NewCompilationError(message string, file string, line, column int) *Error {
	return eh.NewError(ErrorTypeCompilation, message, file, line, column)
}

// NewSecurityError creates a new security error
func (eh *ErrorHandler) NewSecurityError(message string, file string, line, column int) *Error {
	return eh.NewError(ErrorTypeSecurity, message, file, line, column)
}

// NewValidationError creates a new validation error
func (eh *ErrorHandler) NewValidationError(message string, file string, line, column int) *Error {
	return eh.NewError(ErrorTypeValidation, message, file, line, column)
}

// NewIOError creates a new IO error
func (eh *ErrorHandler) NewIOError(message string, file string, line, column int) *Error {
	return eh.NewError(ErrorTypeIO, message, file, line, column)
}

// NewNetworkError creates a new network error
func (eh *ErrorHandler) NewNetworkError(message string, file string, line, column int) *Error {
	return eh.NewError(ErrorTypeNetwork, message, file, line, column)
}

// GetErrors returns all errors
func (eh *ErrorHandler) GetErrors() []Error {
	return eh.errors
}

// GetErrorsByType returns errors of a specific type
func (eh *ErrorHandler) GetErrorsByType(errorType ErrorType) []Error {
	var filteredErrors []Error
	for _, err := range eh.errors {
		if err.Type == errorType {
			filteredErrors = append(filteredErrors, err)
		}
	}
	return filteredErrors
}

// HasErrors returns true if there are any errors
func (eh *ErrorHandler) HasErrors() bool {
	return len(eh.errors) > 0
}

// ClearErrors clears all errors
func (eh *ErrorHandler) ClearErrors() {
	eh.errors = []Error{}
}

// GetLastError returns the last error
func (eh *ErrorHandler) GetLastError() *Error {
	if len(eh.errors) == 0 {
		return nil
	}
	return &eh.errors[len(eh.errors)-1]
}

// FormatError formats a single error
func (eh *ErrorHandler) FormatError(err *Error) string {
	if err == nil {
		return "No error"
	}
	
	var contextStr string
	if len(err.Context) > 0 {
		var contextParts []string
		for key, value := range err.Context {
			contextParts = append(contextParts, fmt.Sprintf("%s=%v", key, value))
		}
		contextStr = fmt.Sprintf(" [Context: %s]", strings.Join(contextParts, ", "))
	}
	
	return fmt.Sprintf("[%s] %s at %s:%d:%d%s", 
		err.Type.String(), err.Message, err.File, err.Line, err.Column, contextStr)
}

// FormatAllErrors formats all errors
func (eh *ErrorHandler) FormatAllErrors() string {
	if len(eh.errors) == 0 {
		return "No errors"
	}
	
	var formattedErrors []string
	for _, err := range eh.errors {
		formattedErrors = append(formattedErrors, eh.FormatError(&err))
	}
	
	return strings.Join(formattedErrors, "\n")
}

// getStackTrace gets the current stack trace
func (eh *ErrorHandler) getStackTrace() string {
	var stack []string
	
	// Skip the first few frames (runtime, this function, etc.)
	for i := 2; ; i++ {
		pc, file, line, ok := runtime.Caller(i)
		if !ok {
			break
		}
		
		fn := runtime.FuncForPC(pc)
		if fn == nil {
			continue
		}
		
		stack = append(stack, fmt.Sprintf("%s:%d %s", file, line, fn.Name()))
	}
	
	return strings.Join(stack, "\n")
}

// AddContext adds context to an error
func (e *Error) AddContext(key string, value interface{}) {
	if e.Context == nil {
		e.Context = make(map[string]interface{})
	}
	e.Context[key] = value
}

// Error returns the string representation of the error
func (e *Error) Error() string {
	return fmt.Sprintf("[%s] %s at %s:%d:%d", 
		e.Type.String(), e.Message, e.File, e.Line, e.Column)
} 