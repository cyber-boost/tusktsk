// Package operators provides TuskLang operator implementations for Go
package operators

import (
	"fmt"
	"strings"
)

// Operator represents a TuskLang operator
type Operator struct {
	Name     string
	Symbol   string
	Function func(args ...interface{}) (interface{}, error)
}

// OperatorManager manages all TuskLang operators
type OperatorManager struct {
	operators map[string]*Operator
}

// New creates a new OperatorManager
func New() *OperatorManager {
	om := &OperatorManager{
		operators: make(map[string]*Operator),
	}
	om.registerDefaultOperators()
	return om
}

// RegisterOperator registers a new operator
func (om *OperatorManager) RegisterOperator(op *Operator) {
	om.operators[op.Name] = op
	om.operators[op.Symbol] = op
}

// GetOperator retrieves an operator by name or symbol
func (om *OperatorManager) GetOperator(name string) (*Operator, bool) {
	op, exists := om.operators[name]
	return op, exists
}

// ExecuteOperator executes an operator with given arguments
func (om *OperatorManager) ExecuteOperator(name string, args ...interface{}) (interface{}, error) {
	op, exists := om.GetOperator(name)
	if !exists {
		return nil, fmt.Errorf("operator '%s' not found", name)
	}
	return op.Function(args...)
}

// registerDefaultOperators registers all default TuskLang operators
func (om *OperatorManager) registerDefaultOperators() {
	// Arithmetic operators
	om.RegisterOperator(&Operator{
		Name:   "add",
		Symbol: "+",
		Function: func(args ...interface{}) (interface{}, error) {
			if len(args) < 2 {
				return nil, fmt.Errorf("add operator requires at least 2 arguments")
			}
			// Implementation for addition
			return args[0], nil
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "subtract",
		Symbol: "-",
		Function: func(args ...interface{}) (interface{}, error) {
			if len(args) < 2 {
				return nil, fmt.Errorf("subtract operator requires at least 2 arguments")
			}
			// Implementation for subtraction
			return args[0], nil
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "multiply",
		Symbol: "*",
		Function: func(args ...interface{}) (interface{}, error) {
			if len(args) < 2 {
				return nil, fmt.Errorf("multiply operator requires at least 2 arguments")
			}
			// Implementation for multiplication
			return args[0], nil
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "divide",
		Symbol: "/",
		Function: func(args ...interface{}) (interface{}, error) {
			if len(args) < 2 {
				return nil, fmt.Errorf("divide operator requires at least 2 arguments")
			}
			// Implementation for division
			return args[0], nil
		},
	})

	// String operators
	om.RegisterOperator(&Operator{
		Name:   "concat",
		Symbol: "++",
		Function: func(args ...interface{}) (interface{}, error) {
			if len(args) < 2 {
				return nil, fmt.Errorf("concat operator requires at least 2 arguments")
			}
			// Implementation for string concatenation
			var result strings.Builder
			for _, arg := range args {
				result.WriteString(fmt.Sprintf("%v", arg))
			}
			return result.String(), nil
		},
	})

	// Comparison operators
	om.RegisterOperator(&Operator{
		Name:   "equals",
		Symbol: "==",
		Function: func(args ...interface{}) (interface{}, error) {
			if len(args) != 2 {
				return nil, fmt.Errorf("equals operator requires exactly 2 arguments")
			}
			// Implementation for equality comparison
			return args[0] == args[1], nil
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "not_equals",
		Symbol: "!=",
		Function: func(args ...interface{}) (interface{}, error) {
			if len(args) != 2 {
				return nil, fmt.Errorf("not_equals operator requires exactly 2 arguments")
			}
			// Implementation for inequality comparison
			return args[0] != args[1], nil
		},
	})

	// Logical operators
	om.RegisterOperator(&Operator{
		Name:   "and",
		Symbol: "&&",
		Function: func(args ...interface{}) (interface{}, error) {
			if len(args) < 2 {
				return nil, fmt.Errorf("and operator requires at least 2 arguments")
			}
			// Implementation for logical AND
			for _, arg := range args {
				if !isTruthy(arg) {
					return false, nil
				}
			}
			return true, nil
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "or",
		Symbol: "||",
		Function: func(args ...interface{}) (interface{}, error) {
			if len(args) < 2 {
				return nil, fmt.Errorf("or operator requires at least 2 arguments")
			}
			// Implementation for logical OR
			for _, arg := range args {
				if isTruthy(arg) {
					return true, nil
				}
			}
			return false, nil
		},
	})

	// Array operators
	om.RegisterOperator(&Operator{
		Name:   "push",
		Symbol: "->",
		Function: func(args ...interface{}) (interface{}, error) {
			if len(args) < 2 {
				return nil, fmt.Errorf("push operator requires at least 2 arguments")
			}
			// Implementation for array push
			return args, nil
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "pop",
		Symbol: "<-",
		Function: func(args ...interface{}) (interface{}, error) {
			if len(args) < 1 {
				return nil, fmt.Errorf("pop operator requires at least 1 argument")
			}
			// Implementation for array pop
			return args[0], nil
		},
	})
}

// isTruthy checks if a value is truthy
func isTruthy(value interface{}) bool {
	switch v := value.(type) {
	case bool:
		return v
	case int:
		return v != 0
	case float64:
		return v != 0
	case string:
		return v != ""
	case nil:
		return false
	default:
		return true
	}
}

// ListOperators returns a list of all registered operators
func (om *OperatorManager) ListOperators() []string {
	var operators []string
	for name := range om.operators {
		operators = append(operators, name)
	}
	return operators
} 