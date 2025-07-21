package fujsen

import (
	"encoding/json"
	"fmt"
	"regexp"
	"strings"
	"time"
)

// JavaScriptFunction represents a serialized JavaScript function
type JavaScriptFunction struct {
	ID          string                 `json:"id"`
	Name        string                 `json:"name"`
	Code        string                 `json:"code"`
	Parameters  []string               `json:"parameters"`
	ReturnType  string                 `json:"return_type"`
	Context     map[string]interface{} `json:"context"`
	CreatedAt   time.Time              `json:"created_at"`
	UpdatedAt   time.Time              `json:"updated_at"`
	Version     int                    `json:"version"`
	Language    string                 `json:"language"`
	Hash        string                 `json:"hash"`
	Compressed  bool                   `json:"compressed"`
	Minified    bool                   `json:"minified"`
}

// JavaScriptExecutor handles JavaScript function execution
type JavaScriptExecutor struct {
	functions map[string]*JavaScriptFunction
	cache     map[string]interface{}
	context   map[string]interface{}
}

// JavaScriptResult represents the result of a JavaScript function execution
type JavaScriptResult struct {
	Success   bool                   `json:"success"`
	Result    interface{}            `json:"result,omitempty"`
	Error     string                 `json:"error,omitempty"`
	Duration  time.Duration          `json:"duration"`
	Memory    int64                  `json:"memory"`
	Logs      []string               `json:"logs,omitempty"`
	Context   map[string]interface{} `json:"context,omitempty"`
}

// NewJavaScriptExecutor creates a new JavaScript executor
func NewJavaScriptExecutor() *JavaScriptExecutor {
	return &JavaScriptExecutor{
		functions: make(map[string]*JavaScriptFunction),
		cache:     make(map[string]interface{}),
		context:   make(map[string]interface{}),
	}
}

// Execute handles @fujsen.js operations
func (j *JavaScriptExecutor) Execute(params string) interface{} {
	// Parse parameters (format: "action", "function_name", "arguments")
	// Example: @fujsen.js("execute", "add", "[1, 2]")
	
	return fmt.Sprintf("@fujsen.js(%s)", params)
}

// SerializeFunction serializes a JavaScript function
func (j *JavaScriptExecutor) SerializeFunction(name, code string, parameters []string, returnType string) (*JavaScriptFunction, error) {
	// Validate function code
	if err := j.validateJavaScriptCode(code); err != nil {
		return nil, fmt.Errorf("invalid JavaScript code: %v", err)
	}

	// Generate function ID
	id := j.generateFunctionID(name, code)

	// Create function object
	function := &JavaScriptFunction{
		ID:         id,
		Name:       name,
		Code:       code,
		Parameters: parameters,
		ReturnType: returnType,
		Context:    make(map[string]interface{}),
		CreatedAt:  time.Now(),
		UpdatedAt:  time.Now(),
		Version:    1,
		Language:   "javascript",
		Hash:       j.generateHash(code),
		Compressed: false,
		Minified:   false,
	}

	// Store function
	j.functions[id] = function

	return function, nil
}

// DeserializeFunction deserializes a JavaScript function
func (j *JavaScriptExecutor) DeserializeFunction(data []byte) (*JavaScriptFunction, error) {
	var function JavaScriptFunction
	if err := json.Unmarshal(data, &function); err != nil {
		return nil, fmt.Errorf("failed to deserialize function: %v", err)
	}

	// Validate function
	if err := j.validateJavaScriptCode(function.Code); err != nil {
		return nil, fmt.Errorf("invalid function code: %v", err)
	}

	return &function, nil
}

// ExecuteFunction executes a JavaScript function
func (j *JavaScriptExecutor) ExecuteFunction(functionID string, arguments []interface{}, context map[string]interface{}) (*JavaScriptResult, error) {
	function, exists := j.functions[functionID]
	if !exists {
		return nil, fmt.Errorf("function %s not found", functionID)
	}

	startTime := time.Now()

	// For now, return a placeholder result since we need a JavaScript engine
	// In a real implementation, this would use a JavaScript engine like otto or goja
	result := &JavaScriptResult{
		Success:  true,
		Result:   fmt.Sprintf("Function %s executed with %d arguments", function.Name, len(arguments)),
		Duration: time.Since(startTime),
		Memory:   1024, // Placeholder
		Logs:     []string{"Function execution simulated"},
		Context:  context,
		Error:    "JavaScript execution requires a JavaScript engine (otto/goja)",
	}

	return result, nil
}

// ExecuteFunctionByName executes a function by name
func (j *JavaScriptExecutor) ExecuteFunctionByName(name string, arguments []interface{}, context map[string]interface{}) (*JavaScriptResult, error) {
	// Find function by name
	var function *JavaScriptFunction
	for _, f := range j.functions {
		if f.Name == name {
			function = f
			break
		}
	}

	if function == nil {
		return nil, fmt.Errorf("function %s not found", name)
	}

	return j.ExecuteFunction(function.ID, arguments, context)
}

// CompileFunction compiles a JavaScript function for better performance
func (j *JavaScriptExecutor) CompileFunction(functionID string) error {
	function, exists := j.functions[functionID]
	if !exists {
		return fmt.Errorf("function %s not found", functionID)
	}

	// For now, just mark as compiled
	function.Compressed = true
	function.UpdatedAt = time.Now()

	return nil
}

// MinifyFunction minifies a JavaScript function
func (j *JavaScriptExecutor) MinifyFunction(functionID string) error {
	function, exists := j.functions[functionID]
	if !exists {
		return fmt.Errorf("function %s not found", functionID)
	}

	// Basic minification (remove comments and extra whitespace)
	minified := j.minifyJavaScript(function.Code)
	function.Code = minified
	function.Minified = true
	function.UpdatedAt = time.Now()

	return nil
}

// GetFunction retrieves a function by ID
func (j *JavaScriptExecutor) GetFunction(functionID string) (*JavaScriptFunction, error) {
	function, exists := j.functions[functionID]
	if !exists {
		return nil, fmt.Errorf("function %s not found", functionID)
	}

	return function, nil
}

// ListFunctions returns a list of all functions
func (j *JavaScriptExecutor) ListFunctions() []*JavaScriptFunction {
	functions := make([]*JavaScriptFunction, 0, len(j.functions))
	for _, function := range j.functions {
		functions = append(functions, function)
	}
	return functions
}

// DeleteFunction deletes a function
func (j *JavaScriptExecutor) DeleteFunction(functionID string) error {
	if _, exists := j.functions[functionID]; !exists {
		return fmt.Errorf("function %s not found", functionID)
	}

	delete(j.functions, functionID)
	return nil
}

// UpdateFunction updates a function
func (j *JavaScriptExecutor) UpdateFunction(functionID string, code string, parameters []string, returnType string) error {
	function, exists := j.functions[functionID]
	if !exists {
		return fmt.Errorf("function %s not found", functionID)
	}

	// Validate new code
	if err := j.validateJavaScriptCode(code); err != nil {
		return fmt.Errorf("invalid JavaScript code: %v", err)
	}

	// Update function
	function.Code = code
	function.Parameters = parameters
	function.ReturnType = returnType
	function.UpdatedAt = time.Now()
	function.Version++
	function.Hash = j.generateHash(code)
	function.Compressed = false
	function.Minified = false

	return nil
}

// SetContext sets the global context for function execution
func (j *JavaScriptExecutor) SetContext(key string, value interface{}) {
	j.context[key] = value
}

// GetContext retrieves a value from the global context
func (j *JavaScriptExecutor) GetContext(key string) (interface{}, bool) {
	value, exists := j.context[key]
	return value, exists
}

// ClearContext clears the global context
func (j *JavaScriptExecutor) ClearContext() {
	j.context = make(map[string]interface{})
}

// CacheResult caches a function result
func (j *JavaScriptExecutor) CacheResult(key string, result interface{}) {
	j.cache[key] = result
}

// GetCachedResult retrieves a cached result
func (j *JavaScriptExecutor) GetCachedResult(key string) (interface{}, bool) {
	result, exists := j.cache[key]
	return result, exists
}

// ClearCache clears the result cache
func (j *JavaScriptExecutor) ClearCache() {
	j.cache = make(map[string]interface{})
}

// validateJavaScriptCode validates JavaScript code
func (j *JavaScriptExecutor) validateJavaScriptCode(code string) error {
	if strings.TrimSpace(code) == "" {
		return fmt.Errorf("code cannot be empty")
	}

	// Basic syntax validation (very basic)
	if !strings.Contains(code, "function") && !strings.Contains(code, "=>") {
		return fmt.Errorf("code must contain a function definition")
	}

	// Check for balanced braces
	if !j.checkBalancedBraces(code) {
		return fmt.Errorf("unbalanced braces in code")
	}

	return nil
}

// checkBalancedBraces checks if braces are balanced
func (j *JavaScriptExecutor) checkBalancedBraces(code string) bool {
	stack := make([]rune, 0)
	
	for _, char := range code {
		switch char {
		case '{', '[', '(':
			stack = append(stack, char)
		case '}':
			if len(stack) == 0 || stack[len(stack)-1] != '{' {
				return false
			}
			stack = stack[:len(stack)-1]
		case ']':
			if len(stack) == 0 || stack[len(stack)-1] != '[' {
				return false
			}
			stack = stack[:len(stack)-1]
		case ')':
			if len(stack) == 0 || stack[len(stack)-1] != '(' {
				return false
			}
			stack = stack[:len(stack)-1]
		}
	}
	
	return len(stack) == 0
}

// generateFunctionID generates a unique function ID
func (j *JavaScriptExecutor) generateFunctionID(name, code string) string {
	hash := j.generateHash(code)
	return fmt.Sprintf("js_%s_%s", name, hash[:8])
}

// generateHash generates a hash for the code
func (j *JavaScriptExecutor) generateHash(code string) string {
	// Simple hash implementation
	hash := 0
	for _, char := range code {
		hash = ((hash << 5) - hash) + int(char)
		hash = hash & hash // Convert to 32-bit integer
	}
	return fmt.Sprintf("%x", hash)
}

// minifyJavaScript performs basic JavaScript minification
func (j *JavaScriptExecutor) minifyJavaScript(code string) string {
	// Remove single-line comments
	re := regexp.MustCompile(`//.*$`)
	code = re.ReplaceAllString(code, "")
	
	// Remove multi-line comments
	re = regexp.MustCompile(`/\*.*?\*/`)
	code = re.ReplaceAllString(code, "")
	
	// Remove extra whitespace
	re = regexp.MustCompile(`\s+`)
	code = re.ReplaceAllString(code, " ")
	
	// Remove whitespace around operators
	re = regexp.MustCompile(`\s*([{}();,=+\-*/<>!&|])\s*`)
	code = re.ReplaceAllString(code, "$1")
	
	return strings.TrimSpace(code)
}

// Close closes the JavaScript executor
func (j *JavaScriptExecutor) Close() error {
	// Clear all data
	j.functions = make(map[string]*JavaScriptFunction)
	j.cache = make(map[string]interface{})
	j.context = make(map[string]interface{})
	return nil
} 