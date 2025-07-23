// Package core provides core TuskLang operators
package core

import (
	"fmt"
	"reflect"
	"sort"
	"strconv"
	"strings"
)

// ArrayOperator handles array and collection operations
type ArrayOperator struct{}

// NewArrayOperator creates a new array operator
func NewArrayOperator() *ArrayOperator {
	return &ArrayOperator{}
}

// Array executes @array operator
func (ao *ArrayOperator) Array(args ...interface{}) (interface{}, error) {
	if len(args) == 0 {
		return []interface{}{}, nil
	}
	
	if len(args) == 1 {
		// Convert single value to array
		return []interface{}{args[0]}, nil
	}
	
	// Return array of all arguments
	return args, nil
}

// Map executes @map operator
func (ao *ArrayOperator) Map(args ...interface{}) (interface{}, error) {
	if len(args) < 2 {
		return nil, fmt.Errorf("@map requires at least 2 arguments")
	}
	
	array, ok := ao.toArray(args[0])
	if !ok {
		return nil, fmt.Errorf("@map first argument must be array")
	}
	
	operation, ok := args[1].(string)
	if !ok {
		return nil, fmt.Errorf("@map operation must be string")
	}
	
	switch strings.ToLower(operation) {
	case "transform":
		if len(args) < 3 {
			return nil, fmt.Errorf("@map transform requires transformation function")
		}
		return ao.transformArray(array, args[2:])
	case "keys":
		return ao.getMapKeys(array)
	case "values":
		return ao.getMapValues(array)
	case "entries":
		return ao.getMapEntries(array)
	case "flatten":
		return ao.flattenArray(array)
	case "unique":
		return ao.uniqueArray(array)
	case "reverse":
		return ao.reverseArray(array)
	case "slice":
		if len(args) < 4 {
			return nil, fmt.Errorf("@map slice requires start and end indices")
		}
		start, err := ao.toInt(args[2])
		if err != nil {
			return nil, fmt.Errorf("invalid start index: %v", err)
		}
		end, err := ao.toInt(args[3])
		if err != nil {
			return nil, fmt.Errorf("invalid end index: %v", err)
		}
		return ao.sliceArray(array, start, end)
	default:
		return nil, fmt.Errorf("unknown map operation: %s", operation)
	}
}

// Filter executes @filter operator
func (ao *ArrayOperator) Filter(args ...interface{}) (interface{}, error) {
	if len(args) < 2 {
		return nil, fmt.Errorf("@filter requires at least 2 arguments")
	}
	
	array, ok := ao.toArray(args[0])
	if !ok {
		return nil, fmt.Errorf("@filter first argument must be array")
	}
	
	condition, ok := args[1].(string)
	if !ok {
		return nil, fmt.Errorf("@filter condition must be string")
	}
	
	switch strings.ToLower(condition) {
	case "notnull":
		return ao.filterNotNull(array)
	case "notempty":
		return ao.filterNotEmpty(array)
	case "numeric":
		return ao.filterNumeric(array)
	case "string":
		return ao.filterString(array)
	case "boolean":
		return ao.filterBoolean(array)
	case "custom":
		if len(args) < 3 {
			return nil, fmt.Errorf("@filter custom requires filter function")
		}
		return ao.filterCustom(array, args[2:])
	default:
		return nil, fmt.Errorf("unknown filter condition: %s", condition)
	}
}

// Sort executes @sort operator
func (ao *ArrayOperator) Sort(args ...interface{}) (interface{}, error) {
	if len(args) < 1 {
		return nil, fmt.Errorf("@sort requires at least 1 argument")
	}
	
	array, ok := ao.toArray(args[0])
	if !ok {
		return nil, fmt.Errorf("@sort first argument must be array")
	}
	
	direction := "asc"
	if len(args) > 1 {
		if dir, ok := args[1].(string); ok {
			direction = strings.ToLower(dir)
		}
	}
	
	return ao.sortArray(array, direction)
}

// Join executes @join operator
func (ao *ArrayOperator) Join(args ...interface{}) (interface{}, error) {
	if len(args) < 1 {
		return nil, fmt.Errorf("@join requires at least 1 argument")
	}
	
	array, ok := ao.toArray(args[0])
	if !ok {
		return nil, fmt.Errorf("@join first argument must be array")
	}
	
	separator := ","
	if len(args) > 1 {
		if sep, ok := args[1].(string); ok {
			separator = sep
		}
	}
	
	return ao.joinArray(array, separator), nil
}

// Split executes @split operator
func (ao *ArrayOperator) Split(args ...interface{}) (interface{}, error) {
	if len(args) < 2 {
		return nil, fmt.Errorf("@split requires at least 2 arguments")
	}
	
	text, ok := args[0].(string)
	if !ok {
		return nil, fmt.Errorf("@split first argument must be string")
	}
	
	separator, ok := args[1].(string)
	if !ok {
		return nil, fmt.Errorf("@split second argument must be string")
	}
	
	return strings.Split(text, separator), nil
}

// Length executes @length operator
func (ao *ArrayOperator) Length(args ...interface{}) (interface{}, error) {
	if len(args) != 1 {
		return nil, fmt.Errorf("@length requires exactly 1 argument")
	}
	
	value := args[0]
	
	switch v := value.(type) {
	case string:
		return len(v), nil
	case []interface{}:
		return len(v), nil
	case []string:
		return len(v), nil
	case []int:
		return len(v), nil
	case map[string]interface{}:
		return len(v), nil
	case map[string]string:
		return len(v), nil
	default:
		// Try to convert to array
		if array, ok := ao.toArray(value); ok {
			return len(array), nil
		}
		return nil, fmt.Errorf("cannot get length of %T", value)
	}
}

// Helper methods
func (ao *ArrayOperator) toArray(value interface{}) ([]interface{}, bool) {
	switch v := value.(type) {
	case []interface{}:
		return v, true
	case []string:
		result := make([]interface{}, len(v))
		for i, item := range v {
			result[i] = item
		}
		return result, true
	case []int:
		result := make([]interface{}, len(v))
		for i, item := range v {
			result[i] = item
		}
		return result, true
	case []float64:
		result := make([]interface{}, len(v))
		for i, item := range v {
			result[i] = item
		}
		return result, true
	case string:
		// Split string into array of characters
		chars := strings.Split(v, "")
		result := make([]interface{}, len(chars))
		for i, char := range chars {
			result[i] = char
		}
		return result, true
	default:
		return nil, false
	}
}

func (ao *ArrayOperator) toInt(value interface{}) (int, error) {
	switch v := value.(type) {
	case int:
		return v, nil
	case int8:
		return int(v), nil
	case int16:
		return int(v), nil
	case int32:
		return int(v), nil
	case int64:
		return int(v), nil
	case uint:
		return int(v), nil
	case uint8:
		return int(v), nil
	case uint16:
		return int(v), nil
	case uint32:
		return int(v), nil
	case uint64:
		return int(v), nil
	case float32:
		return int(v), nil
	case float64:
		return int(v), nil
	case string:
		return strconv.Atoi(v)
	default:
		return 0, fmt.Errorf("cannot convert %T to int", value)
	}
}

func (ao *ArrayOperator) transformArray(array []interface{}, transforms []interface{}) ([]interface{}, error) {
	result := make([]interface{}, len(array))
	
	for i, item := range array {
		transformed := item
		
		// Apply transformations
		for _, transform := range transforms {
			if fn, ok := transform.(func(interface{}) interface{}); ok {
				transformed = fn(transformed)
			} else if transformStr, ok := transform.(string); ok {
				transformed = ao.applyStringTransform(transformed, transformStr)
			}
		}
		
		result[i] = transformed
	}
	
	return result, nil
}

func (ao *ArrayOperator) applyStringTransform(value interface{}, transform string) interface{} {
	switch strings.ToLower(transform) {
	case "tostring":
		return fmt.Sprintf("%v", value)
	case "toupper":
		if str, ok := value.(string); ok {
			return strings.ToUpper(str)
		}
		return value
	case "tolower":
		if str, ok := value.(string); ok {
			return strings.ToLower(str)
		}
		return value
	case "trim":
		if str, ok := value.(string); ok {
			return strings.TrimSpace(str)
		}
		return value
	case "tonumber":
		if str, ok := value.(string); ok {
			if num, err := strconv.ParseFloat(str, 64); err == nil {
				return num
			}
		}
		return value
	default:
		return value
	}
}

func (ao *ArrayOperator) getMapKeys(array []interface{}) ([]interface{}, error) {
	result := []interface{}{}
	
	for _, item := range array {
		if m, ok := item.(map[string]interface{}); ok {
			for key := range m {
				result = append(result, key)
			}
		}
	}
	
	return result, nil
}

func (ao *ArrayOperator) getMapValues(array []interface{}) ([]interface{}, error) {
	result := []interface{}{}
	
	for _, item := range array {
		if m, ok := item.(map[string]interface{}); ok {
			for _, value := range m {
				result = append(result, value)
			}
		}
	}
	
	return result, nil
}

func (ao *ArrayOperator) getMapEntries(array []interface{}) ([]interface{}, error) {
	result := []interface{}{}
	
	for _, item := range array {
		if m, ok := item.(map[string]interface{}); ok {
			for key, value := range m {
				result = append(result, map[string]interface{}{
					"key":   key,
					"value": value,
				})
			}
		}
	}
	
	return result, nil
}

func (ao *ArrayOperator) flattenArray(array []interface{}) ([]interface{}, error) {
	result := []interface{}{}
	
	for _, item := range array {
		if subArray, ok := ao.toArray(item); ok {
			result = append(result, subArray...)
		} else {
			result = append(result, item)
		}
	}
	
	return result, nil
}

func (ao *ArrayOperator) uniqueArray(array []interface{}) ([]interface{}, error) {
	seen := make(map[string]bool)
	result := []interface{}{}
	
	for _, item := range array {
		key := fmt.Sprintf("%v", item)
		if !seen[key] {
			seen[key] = true
			result = append(result, item)
		}
	}
	
	return result, nil
}

func (ao *ArrayOperator) reverseArray(array []interface{}) ([]interface{}, error) {
	result := make([]interface{}, len(array))
	
	for i, item := range array {
		result[len(array)-1-i] = item
	}
	
	return result, nil
}

func (ao *ArrayOperator) sliceArray(array []interface{}, start, end int) ([]interface{}, error) {
	if start < 0 {
		start = 0
	}
	if end > len(array) {
		end = len(array)
	}
	if start >= end {
		return []interface{}{}, nil
	}
	
	return array[start:end], nil
}

func (ao *ArrayOperator) filterNotNull(array []interface{}) ([]interface{}, error) {
	result := []interface{}{}
	
	for _, item := range array {
		if item != nil {
			result = append(result, item)
		}
	}
	
	return result, nil
}

func (ao *ArrayOperator) filterNotEmpty(array []interface{}) ([]interface{}, error) {
	result := []interface{}{}
	
	for _, item := range array {
		if !ao.isEmpty(item) {
			result = append(result, item)
		}
	}
	
	return result, nil
}

func (ao *ArrayOperator) filterNumeric(array []interface{}) ([]interface{}, error) {
	result := []interface{}{}
	
	for _, item := range array {
		if ao.isNumeric(item) {
			result = append(result, item)
		}
	}
	
	return result, nil
}

func (ao *ArrayOperator) filterString(array []interface{}) ([]interface{}, error) {
	result := []interface{}{}
	
	for _, item := range array {
		if _, ok := item.(string); ok {
			result = append(result, item)
		}
	}
	
	return result, nil
}

func (ao *ArrayOperator) filterBoolean(array []interface{}) ([]interface{}, error) {
	result := []interface{}{}
	
	for _, item := range array {
		if _, ok := item.(bool); ok {
			result = append(result, item)
		}
	}
	
	return result, nil
}

func (ao *ArrayOperator) filterCustom(array []interface{}, filters []interface{}) ([]interface{}, error) {
	result := []interface{}{}
	
	for _, item := range array {
		include := true
		
		for _, filter := range filters {
			if fn, ok := filter.(func(interface{}) bool); ok {
				if !fn(item) {
					include = false
					break
				}
			}
		}
		
		if include {
			result = append(result, item)
		}
	}
	
	return result, nil
}

func (ao *ArrayOperator) sortArray(array []interface{}, direction string) ([]interface{}, error) {
	result := make([]interface{}, len(array))
	copy(result, array)
	
	sort.Slice(result, func(i, j int) bool {
		val1 := ao.toComparable(result[i])
		val2 := ao.toComparable(result[j])
		
		if direction == "desc" {
			return val1 > val2
		}
		return val1 < val2
	})
	
	return result, nil
}

func (ao *ArrayOperator) toComparable(value interface{}) float64 {
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

func (ao *ArrayOperator) joinArray(array []interface{}, separator string) string {
	parts := make([]string, len(array))
	
	for i, item := range array {
		parts[i] = fmt.Sprintf("%v", item)
	}
	
	return strings.Join(parts, separator)
}

func (ao *ArrayOperator) isEmpty(value interface{}) bool {
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

func (ao *ArrayOperator) isNumeric(value interface{}) bool {
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

// Additional array utility methods
func (ao *ArrayOperator) Contains(array []interface{}, value interface{}) bool {
	for _, item := range array {
		if reflect.DeepEqual(item, value) {
			return true
		}
	}
	return false
}

func (ao *ArrayOperator) IndexOf(array []interface{}, value interface{}) int {
	for i, item := range array {
		if reflect.DeepEqual(item, value) {
			return i
		}
	}
	return -1
}

func (ao *ArrayOperator) LastIndexOf(array []interface{}, value interface{}) int {
	for i := len(array) - 1; i >= 0; i-- {
		if reflect.DeepEqual(array[i], value) {
			return i
		}
	}
	return -1
}

func (ao *ArrayOperator) Push(array []interface{}, value interface{}) []interface{} {
	return append(array, value)
}

func (ao *ArrayOperator) Pop(array []interface{}) ([]interface{}, interface{}) {
	if len(array) == 0 {
		return array, nil
	}
	return array[:len(array)-1], array[len(array)-1]
}

func (ao *ArrayOperator) Shift(array []interface{}) ([]interface{}, interface{}) {
	if len(array) == 0 {
		return array, nil
	}
	return array[1:], array[0]
}

func (ao *ArrayOperator) Unshift(array []interface{}, value interface{}) []interface{} {
	return append([]interface{}{value}, array...)
}

func (ao *ArrayOperator) Concat(arrays ...[]interface{}) []interface{} {
	result := []interface{}{}
	for _, array := range arrays {
		result = append(result, array...)
	}
	return result
} 