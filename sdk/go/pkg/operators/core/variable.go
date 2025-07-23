// Package core provides core TuskLang operators
package core

import (
	"fmt"
	"os"
	"strings"
)

// VariableOperator handles @variable operations
type VariableOperator struct {
	variables map[string]interface{}
}

// NewVariableOperator creates a new variable operator
func NewVariableOperator() *VariableOperator {
	return &VariableOperator{
		variables: make(map[string]interface{}),
	}
}

// SetVariable sets a variable
func (vo *VariableOperator) SetVariable(name string, value interface{}) {
	vo.variables[name] = value
}

// GetVariable gets a variable with fallback
func (vo *VariableOperator) GetVariable(name string, fallback ...interface{}) interface{} {
	if value, exists := vo.variables[name]; exists {
		return value
	}
	if len(fallback) > 0 {
		return fallback[0]
	}
	return nil
}

// Variable executes @variable operator
func (vo *VariableOperator) Variable(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return nil, fmt.Errorf("@variable requires at least 1 argument")
	}
	
	name, ok := args[0].(string)
	if !ok {
		return nil, fmt.Errorf("@variable first argument must be string")
	}
	
	var fallback interface{}
	if len(args) > 1 {
		fallback = args[1]
	}
	
	return vo.GetVariable(name, fallback), nil
}

// Env executes @env operator
func (vo *VariableOperator) Env(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return nil, fmt.Errorf("@env requires at least 1 argument")
	}
	
	name, ok := args[0].(string)
	if !ok {
		return nil, fmt.Errorf("@env first argument must be string")
	}
	
	value := os.Getenv(name)
	if value == "" && len(args) > 1 {
		return args[1], nil
	}
	
	return value, nil
}

// Request executes @request operator
func (vo *VariableOperator) Request(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return nil, fmt.Errorf("no request available")
	}
	
	field, ok := args[0].(string)
	if !ok {
		return nil, fmt.Errorf("@request first argument must be string")
	}
	
	switch strings.ToLower(field) {
	case "method":
		return "GET", nil
	case "url":
		return "/", nil
	case "path":
		return "/", nil
	case "query":
		return "", nil
	case "body":
		return "request_body", nil
	case "headers":
		return map[string]string{}, nil
	case "cookies":
		return []string{}, nil
	case "remote_addr":
		return "127.0.0.1", nil
	case "user_agent":
		return "Go-Operator/1.0", nil
	default:
		return nil, fmt.Errorf("unknown request field: %s", field)
	}
}

// Session executes @session operator
func (vo *VariableOperator) Session(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return map[string]interface{}{}, nil
	}
	
	if len(args) == 1 {
		key, ok := args[0].(string)
		if !ok {
			return nil, fmt.Errorf("@session key must be string")
		}
		return vo.GetVariable(key), nil
	}
	
	if len(args) == 2 {
		key, ok := args[0].(string)
		if !ok {
			return nil, fmt.Errorf("@session key must be string")
		}
		vo.SetVariable(key, args[1])
		return args[1], nil
	}
	
	return nil, fmt.Errorf("@session requires 0, 1, or 2 arguments")
}

// Cookie executes @cookie operator
func (vo *VariableOperator) Cookie(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return nil, fmt.Errorf("@cookie requires at least 1 argument")
	}
	
	_, ok := args[0].(string)
	if !ok {
		return nil, fmt.Errorf("@cookie name must be string")
	}
	
	// Get cookie
	if len(args) == 1 {
		return nil, nil
	}
	
	// Set cookie
	value := fmt.Sprintf("%v", args[1])
	return value, nil
}

// Header executes @header operator
func (vo *VariableOperator) Header(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return nil, fmt.Errorf("@header requires at least 1 argument")
	}
	
	_, ok := args[0].(string)
	if !ok {
		return nil, fmt.Errorf("@header name must be string")
	}
	
	// Get header
	if len(args) == 1 {
		return nil, nil
	}
	
	// Set header
	value := fmt.Sprintf("%v", args[1])
	return value, nil
}

// Param executes @param operator
func (vo *VariableOperator) Param(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return map[string]string{}, nil
	}
	
	if len(args) == 1 {
		key, ok := args[0].(string)
		if !ok {
			return nil, fmt.Errorf("@param key must be string")
		}
		return vo.GetVariable(key), nil
	}
	
	if len(args) == 2 {
		key, ok := args[0].(string)
		if !ok {
			return nil, fmt.Errorf("@param key must be string")
		}
		value := fmt.Sprintf("%v", args[1])
		vo.SetVariable(key, value)
		return value, nil
	}
	
	return nil, fmt.Errorf("@param requires 0, 1, or 2 arguments")
}

// Query executes @query operator
func (vo *VariableOperator) Query(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return map[string]string{}, nil
	}
	
	if len(args) == 1 {
		key, ok := args[0].(string)
		if !ok {
			return nil, fmt.Errorf("@query key must be string")
		}
		return vo.GetVariable(key), nil
	}
	
	if len(args) == 2 {
		key, ok := args[0].(string)
		if !ok {
			return nil, fmt.Errorf("@query key must be string")
		}
		value := fmt.Sprintf("%v", args[1])
		vo.SetVariable(key, value)
		return value, nil
	}
	
	return nil, fmt.Errorf("@query requires 0, 1, or 2 arguments")
} 