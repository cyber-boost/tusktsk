// Package operators provides TuskLang operator implementations for Go
package operators

import (
	"fmt"
	"sync"

	"github.com/cyber-boost/tusktsk/pkg/operators/core"
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
	mutex     sync.RWMutex
	core      *CoreOperators
}

// CoreOperators holds all core operator instances
type CoreOperators struct {
	Variable    *core.VariableOperator
	DateTime    *core.DateTimeOperator
	String      *core.StringOperator
	Conditional *core.ConditionalOperator
	Math        *core.MathOperator
	Array       *core.ArrayOperator
}

// New creates a new OperatorManager
func New() *OperatorManager {
	om := &OperatorManager{
		operators: make(map[string]*Operator),
		core: &CoreOperators{
			Variable:    core.NewVariableOperator(),
			DateTime:    core.NewDateTimeOperator(),
			String:      core.NewStringOperator(),
			Conditional: core.NewConditionalOperator(),
			Math:        core.NewMathOperator(),
			Array:       core.NewArrayOperator(),
		},
	}
	om.registerDefaultOperators()
	return om
}

// RegisterOperator registers a new operator
func (om *OperatorManager) RegisterOperator(op *Operator) {
	om.mutex.Lock()
	defer om.mutex.Unlock()
	om.operators[op.Name] = op
	om.operators[op.Symbol] = op
}

// GetOperator retrieves an operator by name or symbol
func (om *OperatorManager) GetOperator(name string) (*Operator, bool) {
	om.mutex.RLock()
	defer om.mutex.RUnlock()
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
	// Core Variable Operators
	om.RegisterOperator(&Operator{
		Name:   "variable",
		Symbol: "@variable",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Variable.Variable(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "env",
		Symbol: "@env",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Variable.Env(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "request",
		Symbol: "@request",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Variable.Request(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "session",
		Symbol: "@session",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Variable.Session(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "cookie",
		Symbol: "@cookie",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Variable.Cookie(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "header",
		Symbol: "@header",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Variable.Header(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "param",
		Symbol: "@param",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Variable.Param(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "query",
		Symbol: "@query",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Variable.Query(args...)
		},
	})

	// Date & Time Operators
	om.RegisterOperator(&Operator{
		Name:   "date",
		Symbol: "@date",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.DateTime.Date(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "time",
		Symbol: "@time",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.DateTime.Time(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "timestamp",
		Symbol: "@timestamp",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.DateTime.Timestamp(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "now",
		Symbol: "@now",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.DateTime.Now(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "format",
		Symbol: "@format",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.DateTime.Format(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "timezone",
		Symbol: "@timezone",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.DateTime.Timezone(args...)
		},
	})

	// String & Data Operators
	om.RegisterOperator(&Operator{
		Name:   "string",
		Symbol: "@string",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.String.String(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "regex",
		Symbol: "@regex",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.String.Regex(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "json",
		Symbol: "@json",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.String.JSON(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "base64",
		Symbol: "@base64",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.String.Base64(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "url",
		Symbol: "@url",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.String.URL(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "hash",
		Symbol: "@hash",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.String.Hash(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "uuid",
		Symbol: "@uuid",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.String.UUID(args...)
		},
	})

	// Conditional & Logic Operators
	om.RegisterOperator(&Operator{
		Name:   "if",
		Symbol: "@if",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Conditional.If(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "switch",
		Symbol: "@switch",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Conditional.Switch(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "case",
		Symbol: "@case",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Conditional.Case(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "default",
		Symbol: "@default",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Conditional.Default(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "and",
		Symbol: "@and",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Conditional.And(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "or",
		Symbol: "@or",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Conditional.Or(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "not",
		Symbol: "@not",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Conditional.Not(args...)
		},
	})

	// Math & Calculation Operators
	om.RegisterOperator(&Operator{
		Name:   "math",
		Symbol: "@math",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Math.Math(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "calc",
		Symbol: "@calc",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Math.Calc(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "min",
		Symbol: "@min",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Math.Min(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "max",
		Symbol: "@max",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Math.Max(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "avg",
		Symbol: "@avg",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Math.Avg(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "sum",
		Symbol: "@sum",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Math.Sum(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "round",
		Symbol: "@round",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Math.Round(args...)
		},
	})

	// Array & Collection Operators
	om.RegisterOperator(&Operator{
		Name:   "array",
		Symbol: "@array",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Array.Array(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "map",
		Symbol: "@map",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Array.Map(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "filter",
		Symbol: "@filter",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Array.Filter(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "sort",
		Symbol: "@sort",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Array.Sort(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "join",
		Symbol: "@join",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Array.Join(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "split",
		Symbol: "@split",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Array.Split(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "length",
		Symbol: "@length",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Array.Length(args...)
		},
	})

	// Legacy arithmetic operators for backward compatibility
	om.RegisterOperator(&Operator{
		Name:   "add",
		Symbol: "+",
		Function: func(args ...interface{}) (interface{}, error) {
			if len(args) < 2 {
				return nil, fmt.Errorf("add requires at least 2 numbers")
			}
			mathArgs := append([]interface{}{"add"}, args...)
			return om.core.Math.Math(mathArgs...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "subtract",
		Symbol: "-",
		Function: func(args ...interface{}) (interface{}, error) {
			if len(args) < 2 {
				return nil, fmt.Errorf("subtract requires at least 2 numbers")
			}
			mathArgs := append([]interface{}{"subtract"}, args...)
			return om.core.Math.Math(mathArgs...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "multiply",
		Symbol: "*",
		Function: func(args ...interface{}) (interface{}, error) {
			if len(args) < 2 {
				return nil, fmt.Errorf("multiply requires at least 2 numbers")
			}
			mathArgs := append([]interface{}{"multiply"}, args...)
			return om.core.Math.Math(mathArgs...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "divide",
		Symbol: "/",
		Function: func(args ...interface{}) (interface{}, error) {
			if len(args) < 2 {
				return nil, fmt.Errorf("divide requires at least 2 numbers")
			}
			mathArgs := append([]interface{}{"divide"}, args...)
			return om.core.Math.Math(mathArgs...)
		},
	})

	// Legacy string operators
	om.RegisterOperator(&Operator{
		Name:   "concat",
		Symbol: "++",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.String.String(args...)
		},
	})

	// Legacy comparison operators
	om.RegisterOperator(&Operator{
		Name:   "equals",
		Symbol: "==",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Conditional.Equal(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "not_equals",
		Symbol: "!=",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Conditional.NotEqual(args...)
		},
	})

	// Legacy logical operators
	om.RegisterOperator(&Operator{
		Name:   "and",
		Symbol: "&&",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Conditional.And(args...)
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "or",
		Symbol: "||",
		Function: func(args ...interface{}) (interface{}, error) {
			return om.core.Conditional.Or(args...)
		},
	})

	// Legacy array operators
	om.RegisterOperator(&Operator{
		Name:   "push",
		Symbol: "->",
		Function: func(args ...interface{}) (interface{}, error) {
			if len(args) < 2 {
				return nil, fmt.Errorf("push requires array and value")
			}
			array, ok := args[0].([]interface{})
			if !ok {
				return nil, fmt.Errorf("push first argument must be array")
			}
			result := om.core.Array.Push(array, args[1])
			return result, nil
		},
	})

	om.RegisterOperator(&Operator{
		Name:   "pop",
		Symbol: "<-",
		Function: func(args ...interface{}) (interface{}, error) {
			if len(args) < 1 {
				return nil, fmt.Errorf("pop requires array")
			}
			array, ok := args[0].([]interface{})
			if !ok {
				return nil, fmt.Errorf("pop argument must be array")
			}
			result, value := om.core.Array.Pop(array)
			return map[string]interface{}{
				"array": result,
				"value": value,
			}, nil
		},
	})
}

// ListOperators returns a list of all registered operators
func (om *OperatorManager) ListOperators() []string {
	om.mutex.RLock()
	defer om.mutex.RUnlock()
	
	var operators []string
	for name := range om.operators {
		operators = append(operators, name)
	}
	return operators
}

// GetOperatorCount returns the total number of registered operators
func (om *OperatorManager) GetOperatorCount() int {
	om.mutex.RLock()
	defer om.mutex.RUnlock()
	return len(om.operators)
}

// GetCoreOperators returns the core operators instance
func (om *OperatorManager) GetCoreOperators() *CoreOperators {
	return om.core
}

// SetRequest sets the request for request-based operators
func (om *OperatorManager) SetRequest(req interface{}) {
	// This would be implemented when we have HTTP request support
}

// SetResponseWriter sets the response writer for response-based operators
func (om *OperatorManager) SetResponseWriter(w interface{}) {
	// This would be implemented when we have HTTP response support
} 