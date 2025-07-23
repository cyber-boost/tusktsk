// Package core provides core TuskLang operators
package core

import (
	"fmt"
	"reflect"
	"strconv"
	"strings"
)

// ConditionalOperator handles conditional and logic operations
type ConditionalOperator struct{}

// NewConditionalOperator creates a new conditional operator
func NewConditionalOperator() *ConditionalOperator {
	return &ConditionalOperator{}
}

// If executes @if operator
func (co *ConditionalOperator) If(args ...interface{}) (interface{}, error) {
	if len(args) < 2 {
		return nil, fmt.Errorf("@if requires at least 2 arguments")
	}
	
	condition := args[0]
	trueValue := args[1]
	
	if co.isTruthy(condition) {
		return trueValue, nil
	}
	
	// Check for else value
	if len(args) > 2 {
		return args[2], nil
	}
	
	return nil, nil
}

// Switch executes @switch operator
func (co *ConditionalOperator) Switch(args ...interface{}) (interface{}, error) {
	if len(args) < 2 {
		return nil, fmt.Errorf("@switch requires at least 2 arguments")
	}
	
	value := args[0]
	
	// Look for matching case
	for i := 1; i < len(args); i += 2 {
		if i+1 >= len(args) {
			break
		}
		
		caseValue := args[i]
		caseResult := args[i+1]
		
		if co.equals(value, caseValue) {
			return caseResult, nil
		}
	}
	
	// Check for default case (odd number of args after value)
	if len(args)%2 == 0 && len(args) > 2 {
		return args[len(args)-1], nil
	}
	
	return nil, nil
}

// Case executes @case operator
func (co *ConditionalOperator) Case(args ...interface{}) (interface{}, error) {
	if len(args) < 2 {
		return nil, fmt.Errorf("@case requires at least 2 arguments")
	}
	
	value := args[0]
	
	for i := 1; i < len(args); i++ {
		if co.equals(value, args[i]) {
			return true, nil
		}
	}
	
	return false, nil
}

// Default executes @default operator
func (co *ConditionalOperator) Default(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return nil, nil
	}
	
	// Return first non-nil value
	for _, arg := range args {
		if arg != nil {
			return arg, nil
		}
	}
	
	return nil, nil
}

// And executes @and operator
func (co *ConditionalOperator) And(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return true, nil
	}
	
	for _, arg := range args {
		if !co.isTruthy(arg) {
			return false, nil
		}
	}
	
	return true, nil
}

// Or executes @or operator
func (co *ConditionalOperator) Or(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return false, nil
	}
	
	for _, arg := range args {
		if co.isTruthy(arg) {
			return true, nil
		}
	}
	
	return false, nil
}

// Not executes @not operator
func (co *ConditionalOperator) Not(args ...interface{}) (interface{}, error) {
	if len(args) != 1 {
		return nil, fmt.Errorf("@not requires exactly 1 argument")
	}
	
	return !co.isTruthy(args[0]), nil
}

// isTruthy checks if a value is truthy
func (co *ConditionalOperator) isTruthy(value interface{}) bool {
	if value == nil {
		return false
	}
	
	switch v := value.(type) {
	case bool:
		return v
	case int:
		return v != 0
	case int8:
		return v != 0
	case int16:
		return v != 0
	case int32:
		return v != 0
	case int64:
		return v != 0
	case uint:
		return v != 0
	case uint8:
		return v != 0
	case uint16:
		return v != 0
	case uint32:
		return v != 0
	case uint64:
		return v != 0
	case float32:
		return v != 0
	case float64:
		return v != 0
	case string:
		return v != ""
	case []interface{}:
		return len(v) > 0
	case []string:
		return len(v) > 0
	case []int:
		return len(v) > 0
	case map[string]interface{}:
		return len(v) > 0
	case map[string]string:
		return len(v) > 0
	default:
		// For other types, check if they're zero value
		return !reflect.ValueOf(value).IsZero()
	}
}

// equals checks if two values are equal
func (co *ConditionalOperator) equals(a, b interface{}) bool {
	if a == nil && b == nil {
		return true
	}
	if a == nil || b == nil {
		return false
	}
	
	// Try direct comparison first
	if a == b {
		return true
	}
	
	// Try type conversion for numeric types
	switch va := a.(type) {
	case int:
		if vb, ok := b.(int); ok {
			return va == vb
		}
		if vb, ok := b.(float64); ok {
			return float64(va) == vb
		}
		if vb, ok := b.(string); ok {
			if vbInt, err := strconv.Atoi(vb); err == nil {
				return va == vbInt
			}
		}
	case float64:
		if vb, ok := b.(float64); ok {
			return va == vb
		}
		if vb, ok := b.(int); ok {
			return va == float64(vb)
		}
		if vb, ok := b.(string); ok {
			if vbFloat, err := strconv.ParseFloat(vb, 64); err == nil {
				return va == vbFloat
			}
		}
	case string:
		if vb, ok := b.(string); ok {
			return va == vb
		}
		if vb, ok := b.(int); ok {
			if vaInt, err := strconv.Atoi(va); err == nil {
				return vaInt == vb
			}
		}
		if vb, ok := b.(float64); ok {
			if vaFloat, err := strconv.ParseFloat(va, 64); err == nil {
				return vaFloat == vb
			}
		}
		// Case-insensitive string comparison
		if vb, ok := b.(string); ok {
			return strings.EqualFold(va, vb)
		}
	case bool:
		if vb, ok := b.(bool); ok {
			return va == vb
		}
		if vb, ok := b.(string); ok {
			vbLower := strings.ToLower(vb)
			return (va && (vbLower == "true" || vbLower == "1" || vbLower == "yes")) ||
				(!va && (vbLower == "false" || vbLower == "0" || vbLower == "no"))
		}
	}
	
	return false
}

// GreaterThan executes @gt operator
func (co *ConditionalOperator) GreaterThan(args ...interface{}) (interface{}, error) {
	if len(args) != 2 {
		return nil, fmt.Errorf("@gt requires exactly 2 arguments")
	}
	
	return co.compare(args[0], args[1]) > 0, nil
}

// LessThan executes @lt operator
func (co *ConditionalOperator) LessThan(args ...interface{}) (interface{}, error) {
	if len(args) != 2 {
		return nil, fmt.Errorf("@lt requires exactly 2 arguments")
	}
	
	return co.compare(args[0], args[1]) < 0, nil
}

// GreaterThanOrEqual executes @gte operator
func (co *ConditionalOperator) GreaterThanOrEqual(args ...interface{}) (interface{}, error) {
	if len(args) != 2 {
		return nil, fmt.Errorf("@gte requires exactly 2 arguments")
	}
	
	return co.compare(args[0], args[1]) >= 0, nil
}

// LessThanOrEqual executes @lte operator
func (co *ConditionalOperator) LessThanOrEqual(args ...interface{}) (interface{}, error) {
	if len(args) != 2 {
		return nil, fmt.Errorf("@lte requires exactly 2 arguments")
	}
	
	return co.compare(args[0], args[1]) <= 0, nil
}

// Equal executes @eq operator
func (co *ConditionalOperator) Equal(args ...interface{}) (interface{}, error) {
	if len(args) != 2 {
		return nil, fmt.Errorf("@eq requires exactly 2 arguments")
	}
	
	return co.equals(args[0], args[1]), nil
}

// NotEqual executes @ne operator
func (co *ConditionalOperator) NotEqual(args ...interface{}) (interface{}, error) {
	if len(args) != 2 {
		return nil, fmt.Errorf("@ne requires exactly 2 arguments")
	}
	
	return !co.equals(args[0], args[1]), nil
}

// compare compares two values and returns -1, 0, or 1
func (co *ConditionalOperator) compare(a, b interface{}) int {
	// Convert to comparable types
	aVal := co.toFloat(a)
	bVal := co.toFloat(b)
	
	if aVal < bVal {
		return -1
	}
	if aVal > bVal {
		return 1
	}
	return 0
}

// toFloat converts a value to float64 for comparison
func (co *ConditionalOperator) toFloat(value interface{}) float64 {
	switch v := value.(type) {
	case int:
		return float64(v)
	case int8:
		return float64(v)
	case int16:
		return float64(v)
	case int32:
		return float64(v)
	case int64:
		return float64(v)
	case uint:
		return float64(v)
	case uint8:
		return float64(v)
	case uint16:
		return float64(v)
	case uint32:
		return float64(v)
	case uint64:
		return float64(v)
	case float32:
		return float64(v)
	case float64:
		return v
	case string:
		if f, err := strconv.ParseFloat(v, 64); err == nil {
			return f
		}
		// If not a number, compare as string length
		return float64(len(v))
	case bool:
		if v {
			return 1
		}
		return 0
	default:
		return 0
	}
}

// IsEmpty checks if a value is empty
func (co *ConditionalOperator) IsEmpty(value interface{}) bool {
	if value == nil {
		return true
	}
	
	switch v := value.(type) {
	case string:
		return len(strings.TrimSpace(v)) == 0
	case []interface{}:
		return len(v) == 0
	case []string:
		return len(v) == 0
	case []int:
		return len(v) == 0
	case map[string]interface{}:
		return len(v) == 0
	case map[string]string:
		return len(v) == 0
	default:
		return reflect.ValueOf(value).IsZero()
	}
}

// IsNotEmpty checks if a value is not empty
func (co *ConditionalOperator) IsNotEmpty(value interface{}) bool {
	return !co.IsEmpty(value)
}

// IsNull checks if a value is null/nil
func (co *ConditionalOperator) IsNull(value interface{}) bool {
	return value == nil
}

// IsNotNull checks if a value is not null/nil
func (co *ConditionalOperator) IsNotNull(value interface{}) bool {
	return value != nil
}

// IsTrue checks if a value is true
func (co *ConditionalOperator) IsTrue(value interface{}) bool {
	return co.isTruthy(value)
}

// IsFalse checks if a value is false
func (co *ConditionalOperator) IsFalse(value interface{}) bool {
	return !co.isTruthy(value)
}

// IsNumber checks if a value is a number
func (co *ConditionalOperator) IsNumber(value interface{}) bool {
	switch value.(type) {
	case int, int8, int16, int32, int64, uint, uint8, uint16, uint32, uint64, float32, float64:
		return true
	case string:
		if _, err := strconv.ParseFloat(value.(string), 64); err == nil {
			return true
		}
	}
	return false
}

// IsString checks if a value is a string
func (co *ConditionalOperator) IsString(value interface{}) bool {
	_, ok := value.(string)
	return ok
}

// IsArray checks if a value is an array/slice
func (co *ConditionalOperator) IsArray(value interface{}) bool {
	switch value.(type) {
	case []interface{}, []string, []int, []float64:
		return true
	}
	return false
}

// IsMap checks if a value is a map
func (co *ConditionalOperator) IsMap(value interface{}) bool {
	switch value.(type) {
	case map[string]interface{}, map[string]string:
		return true
	}
	return false
} 