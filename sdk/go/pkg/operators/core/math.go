// Package core provides core TuskLang operators
package core

import (
	"fmt"
	"math"
	"math/rand"
	"strconv"
	"strings"
)

// MathOperator handles mathematical and calculation operations
type MathOperator struct{}

// NewMathOperator creates a new math operator
func NewMathOperator() *MathOperator {
	return &MathOperator{}
}

// Math executes @math operator
func (mo *MathOperator) Math(args ...interface{}) (interface{}, error) {
	if len(args) < 2 {
		return nil, fmt.Errorf("@math requires at least 2 arguments")
	}
	
	operation, ok := args[0].(string)
	if !ok {
		return nil, fmt.Errorf("@math operation must be string")
	}
	
	// Convert remaining args to float64
	values := make([]float64, len(args)-1)
	for i, arg := range args[1:] {
		val, err := mo.toFloat64(arg)
		if err != nil {
			return nil, fmt.Errorf("invalid number at position %d: %v", i+1, err)
		}
		values[i] = val
	}
	
	switch strings.ToLower(operation) {
	case "add", "+":
		return mo.add(values...), nil
	case "subtract", "-":
		if len(values) < 2 {
			return nil, fmt.Errorf("@math subtract requires at least 2 numbers")
		}
		return mo.subtract(values[0], values[1:]), nil
	case "multiply", "*":
		return mo.multiply(values...), nil
	case "divide", "/":
		if len(values) < 2 {
			return nil, fmt.Errorf("@math divide requires at least 2 numbers")
		}
		return mo.divide(values[0], values[1:]), nil
	case "power", "pow", "^":
		if len(values) != 2 {
			return nil, fmt.Errorf("@math power requires exactly 2 numbers")
		}
		return math.Pow(values[0], values[1]), nil
	case "sqrt":
		if len(values) != 1 {
			return nil, fmt.Errorf("@math sqrt requires exactly 1 number")
		}
		return math.Sqrt(values[0]), nil
	case "abs":
		if len(values) != 1 {
			return nil, fmt.Errorf("@math abs requires exactly 1 number")
		}
		return math.Abs(values[0]), nil
	case "floor":
		if len(values) != 1 {
			return nil, fmt.Errorf("@math floor requires exactly 1 number")
		}
		return math.Floor(values[0]), nil
	case "ceil":
		if len(values) != 1 {
			return nil, fmt.Errorf("@math ceil requires exactly 1 number")
		}
		return math.Ceil(values[0]), nil
	case "round":
		if len(values) != 1 {
			return nil, fmt.Errorf("@math round requires exactly 1 number")
		}
		return math.Round(values[0]), nil
	case "sin":
		if len(values) != 1 {
			return nil, fmt.Errorf("@math sin requires exactly 1 number")
		}
		return math.Sin(values[0]), nil
	case "cos":
		if len(values) != 1 {
			return nil, fmt.Errorf("@math cos requires exactly 1 number")
		}
		return math.Cos(values[0]), nil
	case "tan":
		if len(values) != 1 {
			return nil, fmt.Errorf("@math tan requires exactly 1 number")
		}
		return math.Tan(values[0]), nil
	case "log":
		if len(values) != 1 {
			return nil, fmt.Errorf("@math log requires exactly 1 number")
		}
		return math.Log(values[0]), nil
	case "log10":
		if len(values) != 1 {
			return nil, fmt.Errorf("@math log10 requires exactly 1 number")
		}
		return math.Log10(values[0]), nil
	case "exp":
		if len(values) != 1 {
			return nil, fmt.Errorf("@math exp requires exactly 1 number")
		}
		return math.Exp(values[0]), nil
	case "mod", "%":
		if len(values) != 2 {
			return nil, fmt.Errorf("@math mod requires exactly 2 numbers")
		}
		return math.Mod(values[0], values[1]), nil
	default:
		return nil, fmt.Errorf("unknown math operation: %s", operation)
	}
}

// Calc executes @calc operator
func (mo *MathOperator) Calc(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return nil, fmt.Errorf("@calc requires at least 1 argument")
	}
	
	expression, ok := args[0].(string)
	if !ok {
		return nil, fmt.Errorf("@calc expression must be string")
	}
	
	// Simple expression evaluator
	return mo.evaluateExpression(expression)
}

// Min executes @min operator
func (mo *MathOperator) Min(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return nil, fmt.Errorf("@min requires at least 1 argument")
	}
	
	values := make([]float64, len(args))
	for i, arg := range args {
		val, err := mo.toFloat64(arg)
		if err != nil {
			return nil, fmt.Errorf("invalid number at position %d: %v", i, err)
		}
		values[i] = val
	}
	
	if len(values) == 0 {
		return nil, fmt.Errorf("no valid numbers provided")
	}
	
	min := values[0]
	for _, val := range values[1:] {
		if val < min {
			min = val
		}
	}
	
	return min, nil
}

// Max executes @max operator
func (mo *MathOperator) Max(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return nil, fmt.Errorf("@max requires at least 1 argument")
	}
	
	values := make([]float64, len(args))
	for i, arg := range args {
		val, err := mo.toFloat64(arg)
		if err != nil {
			return nil, fmt.Errorf("invalid number at position %d: %v", i, err)
		}
		values[i] = val
	}
	
	if len(values) == 0 {
		return nil, fmt.Errorf("no valid numbers provided")
	}
	
	max := values[0]
	for _, val := range values[1:] {
		if val > max {
			max = val
		}
	}
	
	return max, nil
}

// Avg executes @avg operator
func (mo *MathOperator) Avg(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return nil, fmt.Errorf("@avg requires at least 1 argument")
	}
	
	values := make([]float64, len(args))
	for i, arg := range args {
		val, err := mo.toFloat64(arg)
		if err != nil {
			return nil, fmt.Errorf("invalid number at position %d: %v", i, err)
		}
		values[i] = val
	}
	
	if len(values) == 0 {
		return nil, fmt.Errorf("no valid numbers provided")
	}
	
	sum := mo.add(values...)
	return sum / float64(len(values)), nil
}

// Sum executes @sum operator
func (mo *MathOperator) Sum(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return nil, fmt.Errorf("@sum requires at least 1 argument")
	}
	
	values := make([]float64, len(args))
	for i, arg := range args {
		val, err := mo.toFloat64(arg)
		if err != nil {
			return nil, fmt.Errorf("invalid number at position %d: %v", i, err)
		}
		values[i] = val
	}
	
	return mo.add(values...), nil
}

// Round executes @round operator
func (mo *MathOperator) Round(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return nil, fmt.Errorf("@round requires at least 1 argument")
	}
	
	value, err := mo.toFloat64(args[0])
	if err != nil {
		return nil, fmt.Errorf("invalid number: %v", err)
	}
	
	precision := 0
	if len(args) > 1 {
		prec, err := mo.toFloat64(args[1])
		if err != nil {
			return nil, fmt.Errorf("invalid precision: %v", err)
		}
		precision = int(prec)
	}
	
	multiplier := math.Pow(10, float64(precision))
	return math.Round(value*multiplier) / multiplier, nil
}

// Helper methods
func (mo *MathOperator) add(values ...float64) float64 {
	sum := 0.0
	for _, val := range values {
		sum += val
	}
	return sum
}

func (mo *MathOperator) subtract(first float64, rest []float64) float64 {
	result := first
	for _, val := range rest {
		result -= val
	}
	return result
}

func (mo *MathOperator) multiply(values ...float64) float64 {
	product := 1.0
	for _, val := range values {
		product *= val
	}
	return product
}

func (mo *MathOperator) divide(first float64, rest []float64) float64 {
	result := first
	for _, val := range rest {
		if val == 0 {
			return math.Inf(1) // Return infinity for division by zero
		}
		result /= val
	}
	return result
}

func (mo *MathOperator) toFloat64(value interface{}) (float64, error) {
	switch v := value.(type) {
	case int:
		return float64(v), nil
	case int8:
		return float64(v), nil
	case int16:
		return float64(v), nil
	case int32:
		return float64(v), nil
	case int64:
		return float64(v), nil
	case uint:
		return float64(v), nil
	case uint8:
		return float64(v), nil
	case uint16:
		return float64(v), nil
	case uint32:
		return float64(v), nil
	case uint64:
		return float64(v), nil
	case float32:
		return float64(v), nil
	case float64:
		return v, nil
	case string:
		return strconv.ParseFloat(v, 64)
	case bool:
		if v {
			return 1.0, nil
		}
		return 0.0, nil
	default:
		return 0.0, fmt.Errorf("cannot convert %T to float64", value)
	}
}

// Simple expression evaluator
func (mo *MathOperator) evaluateExpression(expr string) (interface{}, error) {
	// This is a simplified expression evaluator
	// For production, consider using a proper expression parser
	
	// Remove spaces
	expr = strings.ReplaceAll(expr, " ", "")
	
	// Handle simple arithmetic expressions
	// This is a basic implementation - for complex expressions, use a proper parser
	
	// Check for basic operations
	if strings.Contains(expr, "+") {
		parts := strings.Split(expr, "+")
		if len(parts) != 2 {
			return nil, fmt.Errorf("unsupported expression format")
		}
		a, err := strconv.ParseFloat(parts[0], 64)
		if err != nil {
			return nil, fmt.Errorf("invalid first operand: %v", err)
		}
		b, err := strconv.ParseFloat(parts[1], 64)
		if err != nil {
			return nil, fmt.Errorf("invalid second operand: %v", err)
		}
		return a + b, nil
	}
	
	if strings.Contains(expr, "-") {
		parts := strings.Split(expr, "-")
		if len(parts) != 2 {
			return nil, fmt.Errorf("unsupported expression format")
		}
		a, err := strconv.ParseFloat(parts[0], 64)
		if err != nil {
			return nil, fmt.Errorf("invalid first operand: %v", err)
		}
		b, err := strconv.ParseFloat(parts[1], 64)
		if err != nil {
			return nil, fmt.Errorf("invalid second operand: %v", err)
		}
		return a - b, nil
	}
	
	if strings.Contains(expr, "*") {
		parts := strings.Split(expr, "*")
		if len(parts) != 2 {
			return nil, fmt.Errorf("unsupported expression format")
		}
		a, err := strconv.ParseFloat(parts[0], 64)
		if err != nil {
			return nil, fmt.Errorf("invalid first operand: %v", err)
		}
		b, err := strconv.ParseFloat(parts[1], 64)
		if err != nil {
			return nil, fmt.Errorf("invalid second operand: %v", err)
		}
		return a * b, nil
	}
	
	if strings.Contains(expr, "/") {
		parts := strings.Split(expr, "/")
		if len(parts) != 2 {
			return nil, fmt.Errorf("unsupported expression format")
		}
		a, err := strconv.ParseFloat(parts[0], 64)
		if err != nil {
			return nil, fmt.Errorf("invalid first operand: %v", err)
		}
		b, err := strconv.ParseFloat(parts[1], 64)
		if err != nil {
			return nil, fmt.Errorf("invalid second operand: %v", err)
		}
		if b == 0 {
			return nil, fmt.Errorf("division by zero")
		}
		return a / b, nil
	}
	
	// If no operators found, try to parse as a single number
	return strconv.ParseFloat(expr, 64)
}

// Additional math utility methods
func (mo *MathOperator) Factorial(n int) int {
	if n <= 1 {
		return 1
	}
	return n * mo.Factorial(n-1)
}

func (mo *MathOperator) GCD(a, b int) int {
	for b != 0 {
		a, b = b, a%b
	}
	return a
}

func (mo *MathOperator) LCM(a, b int) int {
	return (a * b) / mo.GCD(a, b)
}

func (mo *MathOperator) IsPrime(n int) bool {
	if n < 2 {
		return false
	}
	if n == 2 {
		return true
	}
	if n%2 == 0 {
		return false
	}
	for i := 3; i*i <= n; i += 2 {
		if n%i == 0 {
			return false
		}
	}
	return true
}

func (mo *MathOperator) Fibonacci(n int) int {
	if n <= 1 {
		return n
	}
	return mo.Fibonacci(n-1) + mo.Fibonacci(n-2)
}

func (mo *MathOperator) Random(min, max float64) float64 {
	return min + (max-min)*rand.Float64()
}

func (mo *MathOperator) RandomInt(min, max int) int {
	return min + int(rand.Float64()*float64(max-min+1))
} 