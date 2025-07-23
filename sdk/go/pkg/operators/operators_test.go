package operators

import (
	"fmt"
	"testing"
)

func TestOperatorManager(t *testing.T) {
	om := New()
	
	// Test operator count
	count := om.GetOperatorCount()
	if count < 50 {
		t.Errorf("Expected at least 50 operators, got %d", count)
	}
	
	// Test listing operators
	operators := om.ListOperators()
	if len(operators) < 50 {
		t.Errorf("Expected at least 50 operators in list, got %d", len(operators))
	}
	
	fmt.Printf("✅ Total operators registered: %d\n", count)
}

func TestVariableOperators(t *testing.T) {
	om := New()
	
	// Test @variable operator
	result, err := om.ExecuteOperator("@variable", "test_var", "default_value")
	if err != nil {
		t.Errorf("@variable failed: %v", err)
	}
	if result != "default_value" {
		t.Errorf("Expected 'default_value', got %v", result)
	}
	
	// Test @env operator
	result, err = om.ExecuteOperator("@env", "PATH", "fallback")
	if err != nil {
		t.Errorf("@env failed: %v", err)
	}
	if result == nil {
		t.Errorf("Expected PATH environment variable, got nil")
	}
	
	fmt.Printf("✅ Variable operators working\n")
}

func TestDateTimeOperators(t *testing.T) {
	om := New()
	
	// Test @date operator
	result, err := om.ExecuteOperator("@date")
	if err != nil {
		t.Errorf("@date failed: %v", err)
	}
	if result == nil {
		t.Errorf("Expected date, got nil")
	}
	
	// Test @time operator
	result, err = om.ExecuteOperator("@time")
	if err != nil {
		t.Errorf("@time failed: %v", err)
	}
	if result == nil {
		t.Errorf("Expected time, got nil")
	}
	
	// Test @timestamp operator
	result, err = om.ExecuteOperator("@timestamp")
	if err != nil {
		t.Errorf("@timestamp failed: %v", err)
	}
	if result == nil {
		t.Errorf("Expected timestamp, got nil")
	}
	
	// Test @now operator
	result, err = om.ExecuteOperator("@now")
	if err != nil {
		t.Errorf("@now failed: %v", err)
	}
	if result == nil {
		t.Errorf("Expected current datetime, got nil")
	}
	
	fmt.Printf("✅ DateTime operators working\n")
}

func TestStringOperators(t *testing.T) {
	om := New()
	
	// Test @string operator
	result, err := om.ExecuteOperator("@string", "hello", "world")
	if err != nil {
		t.Errorf("@string failed: %v", err)
	}
	if result != "helloworld" {
		t.Errorf("Expected 'helloworld', got %v", result)
	}
	
	// Test @regex operator
	result, err = om.ExecuteOperator("@regex", "\\d+", "abc123def", "match")
	if err != nil {
		t.Errorf("@regex failed: %v", err)
	}
	if result != true {
		t.Errorf("Expected true for regex match, got %v", result)
	}
	
	// Test @json operator
	result, err = om.ExecuteOperator("@json", `{"name":"test","value":123}`)
	if err != nil {
		t.Errorf("@json failed: %v", err)
	}
	if result == nil {
		t.Errorf("Expected parsed JSON, got nil")
	}
	
	// Test @base64 operator
	result, err = om.ExecuteOperator("@base64", "encode", "hello world")
	if err != nil {
		t.Errorf("@base64 failed: %v", err)
	}
	if result == nil {
		t.Errorf("Expected base64 encoded string, got nil")
	}
	
	// Test @hash operator
	result, err = om.ExecuteOperator("@hash", "md5", "hello world")
	if err != nil {
		t.Errorf("@hash failed: %v", err)
	}
	if result == nil {
		t.Errorf("Expected hash, got nil")
	}
	
	// Test @uuid operator
	result, err = om.ExecuteOperator("@uuid")
	if err != nil {
		t.Errorf("@uuid failed: %v", err)
	}
	if result == nil {
		t.Errorf("Expected UUID, got nil")
	}
	
	fmt.Printf("✅ String operators working\n")
}

func TestConditionalOperators(t *testing.T) {
	om := New()
	
	// Test @if operator
	result, err := om.ExecuteOperator("@if", true, "yes", "no")
	if err != nil {
		t.Errorf("@if failed: %v", err)
	}
	if result != "yes" {
		t.Errorf("Expected 'yes', got %v", result)
	}
	
	// Test @and operator
	result, err = om.ExecuteOperator("@and", true, true, true)
	if err != nil {
		t.Errorf("@and failed: %v", err)
	}
	if result != true {
		t.Errorf("Expected true, got %v", result)
	}
	
	// Test @or operator
	result, err = om.ExecuteOperator("@or", false, false, true)
	if err != nil {
		t.Errorf("@or failed: %v", err)
	}
	if result != true {
		t.Errorf("Expected true, got %v", result)
	}
	
	// Test @not operator
	result, err = om.ExecuteOperator("@not", false)
	if err != nil {
		t.Errorf("@not failed: %v", err)
	}
	if result != true {
		t.Errorf("Expected true, got %v", result)
	}
	
	fmt.Printf("✅ Conditional operators working\n")
}

func TestMathOperators(t *testing.T) {
	om := New()
	
	// Test @math operator
	result, err := om.ExecuteOperator("@math", "add", 5, 3)
	if err != nil {
		t.Errorf("@math add failed: %v", err)
	}
	if result != 8.0 {
		t.Errorf("Expected 8.0, got %v", result)
	}
	
	// Test @calc operator
	result, err = om.ExecuteOperator("@calc", "5+3")
	if err != nil {
		t.Errorf("@calc failed: %v", err)
	}
	if result != 8.0 {
		t.Errorf("Expected 8.0, got %v", result)
	}
	
	// Test @min operator
	result, err = om.ExecuteOperator("@min", 5, 3, 8, 1)
	if err != nil {
		t.Errorf("@min failed: %v", err)
	}
	if result != 1.0 {
		t.Errorf("Expected 1.0, got %v", result)
	}
	
	// Test @max operator
	result, err = om.ExecuteOperator("@max", 5, 3, 8, 1)
	if err != nil {
		t.Errorf("@max failed: %v", err)
	}
	if result != 8.0 {
		t.Errorf("Expected 8.0, got %v", result)
	}
	
	// Test @avg operator
	result, err = om.ExecuteOperator("@avg", 2, 4, 6)
	if err != nil {
		t.Errorf("@avg failed: %v", err)
	}
	if result != 4.0 {
		t.Errorf("Expected 4.0, got %v", result)
	}
	
	// Test @sum operator
	result, err = om.ExecuteOperator("@sum", 1, 2, 3, 4, 5)
	if err != nil {
		t.Errorf("@sum failed: %v", err)
	}
	if result != 15.0 {
		t.Errorf("Expected 15.0, got %v", result)
	}
	
	// Test @round operator
	result, err = om.ExecuteOperator("@round", 3.14159, 2)
	if err != nil {
		t.Errorf("@round failed: %v", err)
	}
	if result != 3.14 {
		t.Errorf("Expected 3.14, got %v", result)
	}
	
	fmt.Printf("✅ Math operators working\n")
}

func TestArrayOperators(t *testing.T) {
	om := New()
	
	// Test @array operator
	result, err := om.ExecuteOperator("@array", "a", "b", "c")
	if err != nil {
		t.Errorf("@array failed: %v", err)
	}
	if result == nil {
		t.Errorf("Expected array, got nil")
	}
	
	// Test @map operator
	result, err = om.ExecuteOperator("@map", []interface{}{"a", "b", "c"}, "transform", "toupper")
	if err != nil {
		t.Errorf("@map failed: %v", err)
	}
	if result == nil {
		t.Errorf("Expected transformed array, got nil")
	}
	
	// Test @filter operator
	result, err = om.ExecuteOperator("@filter", []interface{}{"a", "", "c", ""}, "notempty")
	if err != nil {
		t.Errorf("@filter failed: %v", err)
	}
	if result == nil {
		t.Errorf("Expected filtered array, got nil")
	}
	
	// Test @sort operator
	result, err = om.ExecuteOperator("@sort", []interface{}{3, 1, 4, 1, 5})
	if err != nil {
		t.Errorf("@sort failed: %v", err)
	}
	if result == nil {
		t.Errorf("Expected sorted array, got nil")
	}
	
	// Test @join operator
	result, err = om.ExecuteOperator("@join", []interface{}{"a", "b", "c"}, "-")
	if err != nil {
		t.Errorf("@join failed: %v", err)
	}
	if result != "a-b-c" {
		t.Errorf("Expected 'a-b-c', got %v", result)
	}
	
	// Test @split operator
	result, err = om.ExecuteOperator("@split", "a-b-c", "-")
	if err != nil {
		t.Errorf("@split failed: %v", err)
	}
	if result == nil {
		t.Errorf("Expected split array, got nil")
	}
	
	// Test @length operator
	result, err = om.ExecuteOperator("@length", []interface{}{"a", "b", "c"})
	if err != nil {
		t.Errorf("@length failed: %v", err)
	}
	if result != 3 {
		t.Errorf("Expected 3, got %v", result)
	}
	
	fmt.Printf("✅ Array operators working\n")
}

func TestLegacyOperators(t *testing.T) {
	om := New()
	
	// Test legacy arithmetic operators
	result, err := om.ExecuteOperator("+", 5, 3)
	if err != nil {
		t.Errorf("Legacy add failed: %v", err)
	}
	if result != 8.0 {
		t.Errorf("Expected 8.0, got %v", result)
	}
	
	result, err = om.ExecuteOperator("-", 5, 3)
	if err != nil {
		t.Errorf("Legacy subtract failed: %v", err)
	}
	if result != 2.0 {
		t.Errorf("Expected 2.0, got %v", result)
	}
	
	result, err = om.ExecuteOperator("*", 5, 3)
	if err != nil {
		t.Errorf("Legacy multiply failed: %v", err)
	}
	if result != 15.0 {
		t.Errorf("Expected 15.0, got %v", result)
	}
	
	result, err = om.ExecuteOperator("/", 6, 2)
	if err != nil {
		t.Errorf("Legacy divide failed: %v", err)
	}
	if result != 3.0 {
		t.Errorf("Expected 3.0, got %v", result)
	}
	
	// Test legacy comparison operators
	result, err = om.ExecuteOperator("==", 5, 5)
	if err != nil {
		t.Errorf("Legacy equals failed: %v", err)
	}
	if result != true {
		t.Errorf("Expected true, got %v", result)
	}
	
	result, err = om.ExecuteOperator("!=", 5, 3)
	if err != nil {
		t.Errorf("Legacy not equals failed: %v", err)
	}
	if result != true {
		t.Errorf("Expected true, got %v", result)
	}
	
	// Test legacy logical operators
	result, err = om.ExecuteOperator("&&", true, true)
	if err != nil {
		t.Errorf("Legacy and failed: %v", err)
	}
	if result != true {
		t.Errorf("Expected true, got %v", result)
	}
	
	result, err = om.ExecuteOperator("||", false, true)
	if err != nil {
		t.Errorf("Legacy or failed: %v", err)
	}
	if result != true {
		t.Errorf("Expected true, got %v", result)
	}
	
	fmt.Printf("✅ Legacy operators working\n")
}

func TestOperatorPerformance(t *testing.T) {
	om := New()
	
	// Performance test for math operations
	iterations := 10000
	
	// Test @math performance
	for i := 0; i < iterations; i++ {
		_, err := om.ExecuteOperator("@math", "add", i, i+1)
		if err != nil {
			t.Errorf("Performance test failed at iteration %d: %v", i, err)
		}
	}
	
	// Test @string performance
	for i := 0; i < iterations; i++ {
		_, err := om.ExecuteOperator("@string", "test", i)
		if err != nil {
			t.Errorf("String performance test failed at iteration %d: %v", i, err)
		}
	}
	
	// Test @array performance
	testArray := []interface{}{"a", "b", "c", "d", "e"}
	for i := 0; i < iterations; i++ {
		_, err := om.ExecuteOperator("@length", testArray)
		if err != nil {
			t.Errorf("Array performance test failed at iteration %d: %v", i, err)
		}
	}
	
	fmt.Printf("✅ Performance test completed: %d iterations each\n", iterations)
}

func TestOperatorComposition(t *testing.T) {
	om := New()
	
	// Test operator composition - combining multiple operators
	// Create an array, filter it, then get the length
	array := []interface{}{"a", "", "b", "", "c"}
	
	// Filter out empty strings
	filtered, err := om.ExecuteOperator("@filter", array, "notempty")
	if err != nil {
		t.Errorf("Filter composition failed: %v", err)
	}
	
	// Get length of filtered array
	length, err := om.ExecuteOperator("@length", filtered)
	if err != nil {
		t.Errorf("Length composition failed: %v", err)
	}
	
	if length != 3 {
		t.Errorf("Expected length 3, got %v", length)
	}
	
	// Test math composition
	numbers := []interface{}{1, 2, 3, 4, 5}
	
	// Get sum
	sum, err := om.ExecuteOperator("@sum", numbers...)
	if err != nil {
		t.Errorf("Sum composition failed: %v", err)
	}
	
	// Get average
	avg, err := om.ExecuteOperator("@avg", numbers...)
	if err != nil {
		t.Errorf("Avg composition failed: %v", err)
	}
	
	if sum != 15.0 {
		t.Errorf("Expected sum 15.0, got %v", sum)
	}
	
	if avg != 3.0 {
		t.Errorf("Expected avg 3.0, got %v", avg)
	}
	
	fmt.Printf("✅ Operator composition working\n")
}

func BenchmarkOperatorExecution(b *testing.B) {
	om := New()
	
	b.Run("MathAdd", func(b *testing.B) {
		for i := 0; i < b.N; i++ {
			om.ExecuteOperator("@math", "add", i, i+1)
		}
	})
	
	b.Run("StringConcat", func(b *testing.B) {
		for i := 0; i < b.N; i++ {
			om.ExecuteOperator("@string", "test", i)
		}
	})
	
	b.Run("ArrayLength", func(b *testing.B) {
		testArray := []interface{}{"a", "b", "c", "d", "e"}
		for i := 0; i < b.N; i++ {
			om.ExecuteOperator("@length", testArray)
		}
	})
	
	b.Run("ConditionalIf", func(b *testing.B) {
		for i := 0; i < b.N; i++ {
			om.ExecuteOperator("@if", i%2 == 0, "even", "odd")
		}
	})
}

func ExampleOperatorManager() {
	om := New()
	
	// Example 1: Basic math operations
	result, _ := om.ExecuteOperator("@math", "add", 5, 3)
	fmt.Printf("5 + 3 = %v\n", result)
	
	// Example 2: String operations
	result, _ = om.ExecuteOperator("@string", "Hello", " ", "World")
	fmt.Printf("Concatenated: %v\n", result)
	
	// Example 3: Array operations
	array := []interface{}{"apple", "banana", "cherry"}
	result, _ = om.ExecuteOperator("@join", array, ", ")
	fmt.Printf("Joined array: %v\n", result)
	
	// Example 4: Conditional operations
	result, _ = om.ExecuteOperator("@if", true, "Condition is true", "Condition is false")
	fmt.Printf("Conditional result: %v\n", result)
	
	// Example 5: Date operations
	result, _ = om.ExecuteOperator("@date")
	fmt.Printf("Current datetime: %v\n", result)
	
	// Output:
	// 5 + 3 = 8
	// Concatenated: Hello World
	// Joined array: apple, banana, cherry
	// Conditional result: Condition is true
	// Current datetime: 2025-07-23
} 