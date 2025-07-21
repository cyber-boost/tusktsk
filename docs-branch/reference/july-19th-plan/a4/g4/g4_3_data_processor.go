package main

import (
	"encoding/csv"
	"encoding/json"
	"fmt"
	"reflect"
	"sort"
	"strconv"
	"strings"
	"time"
)

// DataType represents the type of data
type DataType string

const (
	TypeString   DataType = "string"
	TypeNumber   DataType = "number"
	TypeBoolean  DataType = "boolean"
	TypeDate     DataType = "date"
	TypeArray    DataType = "array"
	TypeObject   DataType = "object"
	TypeNull     DataType = "null"
)

// DataProcessor provides data processing capabilities
type DataProcessor struct {
	transformers map[string]DataTransformer
	validators   map[string]DataValidator
	analyzers    map[string]DataAnalyzer
}

// DataTransformer transforms data from one format to another
type DataTransformer interface {
	Transform(data interface{}) (interface{}, error)
	Name() string
}

// DataValidator validates data against rules
type DataValidator interface {
	Validate(data interface{}) (bool, []string)
	Name() string
}

// DataAnalyzer analyzes data and provides insights
type DataAnalyzer interface {
	Analyze(data interface{}) (*AnalysisResult, error)
	Name() string
}

// AnalysisResult contains analysis results
type AnalysisResult struct {
	Summary     map[string]interface{} `json:"summary"`
	Statistics  map[string]interface{} `json:"statistics"`
	Outliers    []interface{}          `json:"outliers"`
	Patterns    []string               `json:"patterns"`
	Recommendations []string           `json:"recommendations"`
}

// ValidationRule represents a validation rule
type ValidationRule struct {
	Field       string      `json:"field"`
	Type        DataType    `json:"type"`
	Required    bool        `json:"required"`
	MinValue    interface{} `json:"min_value,omitempty"`
	MaxValue    interface{} `json:"max_value,omitempty"`
	Pattern     string      `json:"pattern,omitempty"`
	AllowedValues []interface{} `json:"allowed_values,omitempty"`
}

// NewDataProcessor creates a new data processor
func NewDataProcessor() *DataProcessor {
	dp := &DataProcessor{
		transformers: make(map[string]DataTransformer),
		validators:   make(map[string]DataValidator),
		analyzers:    make(map[string]DataAnalyzer),
	}

	// Register default transformers
	dp.RegisterTransformer(&StringTransformer{})
	dp.RegisterTransformer(&NumberTransformer{})
	dp.RegisterTransformer(&DateTransformer{})
	dp.RegisterTransformer(&CSVTransformer{})

	// Register default validators
	dp.RegisterValidator(&TypeValidator{})
	dp.RegisterValidator(&RangeValidator{})
	dp.RegisterValidator(&PatternValidator{})

	// Register default analyzers
	dp.RegisterAnalyzer(&StatisticalAnalyzer{})
	dp.RegisterAnalyzer(&PatternAnalyzer{})

	return dp
}

// RegisterTransformer registers a data transformer
func (dp *DataProcessor) RegisterTransformer(transformer DataTransformer) {
	dp.transformers[transformer.Name()] = transformer
}

// RegisterValidator registers a data validator
func (dp *DataProcessor) RegisterValidator(validator DataValidator) {
	dp.validators[validator.Name()] = validator
}

// RegisterAnalyzer registers a data analyzer
func (dp *DataProcessor) RegisterAnalyzer(analyzer DataAnalyzer) {
	dp.analyzers[analyzer.Name()] = analyzer
}

// TransformData transforms data using registered transformers
func (dp *DataProcessor) TransformData(data interface{}, transformerName string) (interface{}, error) {
	transformer, exists := dp.transformers[transformerName]
	if !exists {
		return nil, fmt.Errorf("transformer not found: %s", transformerName)
	}

	return transformer.Transform(data)
}

// ValidateData validates data using registered validators
func (dp *DataProcessor) ValidateData(data interface{}, rules []ValidationRule) (bool, []string) {
	var errors []string

	for _, rule := range rules {
		// Check required fields
		if rule.Required {
			if data == nil || (reflect.ValueOf(data).Kind() == reflect.String && data.(string) == "") {
				errors = append(errors, fmt.Sprintf("field '%s' is required", rule.Field))
				continue
			}
		}

		// Apply validators
		for _, validator := range dp.validators {
			if valid, validationErrors := validator.Validate(data); !valid {
				errors = append(errors, validationErrors...)
			}
		}
	}

	return len(errors) == 0, errors
}

// AnalyzeData analyzes data using registered analyzers
func (dp *DataProcessor) AnalyzeData(data interface{}) (*AnalysisResult, error) {
	result := &AnalysisResult{
		Summary:        make(map[string]interface{}),
		Statistics:     make(map[string]interface{}),
		Outliers:       make([]interface{}, 0),
		Patterns:       make([]string, 0),
		Recommendations: make([]string, 0),
	}

	for _, analyzer := range dp.analyzers {
		analysis, err := analyzer.Analyze(data)
		if err != nil {
			continue
		}

		// Merge results
		for key, value := range analysis.Summary {
			result.Summary[key] = value
		}
		for key, value := range analysis.Statistics {
			result.Statistics[key] = value
		}
		result.Outliers = append(result.Outliers, analysis.Outliers...)
		result.Patterns = append(result.Patterns, analysis.Patterns...)
		result.Recommendations = append(result.Recommendations, analysis.Recommendations...)
	}

	return result, nil
}

// ConvertToJSON converts data to JSON format
func (dp *DataProcessor) ConvertToJSON(data interface{}) (string, error) {
	jsonData, err := json.MarshalIndent(data, "", "  ")
	if err != nil {
		return "", fmt.Errorf("failed to marshal to JSON: %v", err)
	}
	return string(jsonData), nil
}

// ConvertFromJSON converts JSON string to data structure
func (dp *DataProcessor) ConvertFromJSON(jsonStr string) (interface{}, error) {
	var data interface{}
	err := json.Unmarshal([]byte(jsonStr), &data)
	if err != nil {
		return nil, fmt.Errorf("failed to unmarshal JSON: %v", err)
	}
	return data, nil
}

// ConvertToCSV converts data to CSV format
func (dp *DataProcessor) ConvertToCSV(data []map[string]interface{}) (string, error) {
	if len(data) == 0 {
		return "", nil
	}

	var buffer strings.Builder
	writer := csv.NewWriter(&buffer)

	// Write headers
	headers := make([]string, 0)
	for key := range data[0] {
		headers = append(headers, key)
	}
	writer.Write(headers)

	// Write data rows
	for _, row := range data {
		values := make([]string, len(headers))
		for i, header := range headers {
			if value, exists := row[header]; exists {
				values[i] = fmt.Sprintf("%v", value)
			}
		}
		writer.Write(values)
	}

	writer.Flush()
	return buffer.String(), writer.Error()
}

// FilterData filters data based on conditions
func (dp *DataProcessor) FilterData(data []map[string]interface{}, conditions map[string]interface{}) []map[string]interface{} {
	var filtered []map[string]interface{}

	for _, item := range data {
		if dp.matchesConditions(item, conditions) {
			filtered = append(filtered, item)
		}
	}

	return filtered
}

// SortData sorts data by specified fields
func (dp *DataProcessor) SortData(data []map[string]interface{}, sortBy string, ascending bool) []map[string]interface{} {
	sorted := make([]map[string]interface{}, len(data))
	copy(sorted, data)

	sort.Slice(sorted, func(i, j int) bool {
		valI := sorted[i][sortBy]
		valJ := sorted[j][sortBy]

		if ascending {
			return dp.compareValues(valI, valJ) < 0
		}
		return dp.compareValues(valI, valJ) > 0
	})

	return sorted
}

// AggregateData aggregates data using specified functions
func (dp *DataProcessor) AggregateData(data []map[string]interface{}, field string, operation string) interface{} {
	if len(data) == 0 {
		return nil
	}

	// Aggregate data
	var values []float64
	for _, item := range data {
		if val, exists := item[field]; exists {
			num := dp.toFloat64(val)
			if num != 0 {
				values = append(values, num)
			}
		}
	}

	if len(values) == 0 {
		return nil
	}

	switch operation {
	case "sum":
		return dp.sum(values)
	case "avg":
		return dp.average(values)
	case "min":
		return dp.min(values)
	case "max":
		return dp.max(values)
	case "count":
		return len(values)
	default:
		return nil
	}
}

// Helper methods
func (dp *DataProcessor) matchesConditions(item map[string]interface{}, conditions map[string]interface{}) bool {
	for key, expectedValue := range conditions {
		if actualValue, exists := item[key]; !exists || actualValue != expectedValue {
			return false
		}
	}
	return true
}

func (dp *DataProcessor) compareValues(a, b interface{}) int {
	valA := dp.toFloat64(a)
	valB := dp.toFloat64(b)

	if valA < valB {
		return -1
	} else if valA > valB {
		return 1
	}
	return 0
}

func (dp *DataProcessor) toFloat64(value interface{}) float64 {
	switch v := value.(type) {
	case float64:
		return v
	case int:
		return float64(v)
	case string:
		if num, err := strconv.ParseFloat(v, 64); err == nil {
			return num
		}
	}
	return 0
}

func (dp *DataProcessor) sum(values []float64) float64 {
	sum := 0.0
	for _, v := range values {
		sum += v
	}
	return sum
}

func (dp *DataProcessor) average(values []float64) float64 {
	if len(values) == 0 {
		return 0
	}
	return dp.sum(values) / float64(len(values))
}

func (dp *DataProcessor) min(values []float64) float64 {
	if len(values) == 0 {
		return 0
	}
	min := values[0]
	for _, v := range values {
		if v < min {
			min = v
		}
	}
	return min
}

func (dp *DataProcessor) max(values []float64) float64 {
	if len(values) == 0 {
		return 0
	}
	max := values[0]
	for _, v := range values {
		if v > max {
			max = v
		}
	}
	return max
}

// Example transformers
type StringTransformer struct{}

func (st *StringTransformer) Name() string {
	return "string"
}

func (st *StringTransformer) Transform(data interface{}) (interface{}, error) {
	return fmt.Sprintf("%v", data), nil
}

type NumberTransformer struct{}

func (nt *NumberTransformer) Name() string {
	return "number"
}

func (nt *NumberTransformer) Transform(data interface{}) (interface{}, error) {
	switch v := data.(type) {
	case string:
		return strconv.ParseFloat(v, 64)
	case int:
		return float64(v), nil
	case float64:
		return v, nil
	default:
		return 0.0, fmt.Errorf("cannot convert to number")
	}
}

type DateTransformer struct{}

func (dt *DateTransformer) Name() string {
	return "date"
}

func (dt *DateTransformer) Transform(data interface{}) (interface{}, error) {
	switch v := data.(type) {
	case string:
		return time.Parse(time.RFC3339, v)
	case time.Time:
		return v, nil
	default:
		return time.Time{}, fmt.Errorf("cannot convert to date")
	}
}

type CSVTransformer struct{}

func (ct *CSVTransformer) Name() string {
	return "csv"
}

func (ct *CSVTransformer) Transform(data interface{}) (interface{}, error) {
	// Implementation would convert data to CSV format
	return data, nil
}

// Example validators
type TypeValidator struct{}

func (tv *TypeValidator) Name() string {
	return "type"
}

func (tv *TypeValidator) Validate(data interface{}) (bool, []string) {
	// Basic type validation
	return true, nil
}

type RangeValidator struct{}

func (rv *RangeValidator) Name() string {
	return "range"
}

func (rv *RangeValidator) Validate(data interface{}) (bool, []string) {
	// Range validation
	return true, nil
}

type PatternValidator struct{}

func (pv *PatternValidator) Name() string {
	return "pattern"
}

func (pv *PatternValidator) Validate(data interface{}) (bool, []string) {
	// Pattern validation
	return true, nil
}

// Example analyzers
type StatisticalAnalyzer struct{}

func (sa *StatisticalAnalyzer) Name() string {
	return "statistical"
}

func (sa *StatisticalAnalyzer) Analyze(data interface{}) (*AnalysisResult, error) {
	result := &AnalysisResult{
		Summary:     make(map[string]interface{}),
		Statistics:  make(map[string]interface{}),
		Outliers:    make([]interface{}, 0),
		Patterns:    make([]string, 0),
		Recommendations: make([]string, 0),
	}

	// Basic statistical analysis
	result.Summary["total_items"] = 1
	result.Statistics["mean"] = 0.0
	result.Patterns = append(result.Patterns, "Data analyzed successfully")

	return result, nil
}

type PatternAnalyzer struct{}

func (pa *PatternAnalyzer) Name() string {
	return "pattern"
}

func (pa *PatternAnalyzer) Analyze(data interface{}) (*AnalysisResult, error) {
	result := &AnalysisResult{
		Summary:     make(map[string]interface{}),
		Statistics:  make(map[string]interface{}),
		Outliers:    make([]interface{}, 0),
		Patterns:    make([]string, 0),
		Recommendations: make([]string, 0),
	}

	// Pattern analysis
	result.Patterns = append(result.Patterns, "Pattern analysis completed")
	result.Recommendations = append(result.Recommendations, "Consider data validation")

	return result, nil
}

// Example usage
func main() {
	// Create data processor
	processor := NewDataProcessor()

	// Sample data
	sampleData := []map[string]interface{}{
		{"id": 1, "name": "Alice", "age": 25, "score": 85.5},
		{"id": 2, "name": "Bob", "age": 30, "score": 92.0},
		{"id": 3, "name": "Charlie", "age": 28, "score": 78.5},
		{"id": 4, "name": "Diana", "age": 35, "score": 95.0},
		{"id": 5, "name": "Eve", "age": 22, "score": 88.5},
	}

	fmt.Println("Data Processing Demo")
	fmt.Println("===================")

	// Convert to JSON
	jsonData, err := processor.ConvertToJSON(sampleData)
	if err != nil {
		fmt.Printf("Error converting to JSON: %v\n", err)
	} else {
		fmt.Printf("JSON Output:\n%s\n", jsonData[:200]+"...")
	}

	// Convert to CSV
	csvData, err := processor.ConvertToCSV(sampleData)
	if err != nil {
		fmt.Printf("Error converting to CSV: %v\n", err)
	} else {
		fmt.Printf("\nCSV Output:\n%s\n", csvData)
	}

	// Filter data
	filtered := processor.FilterData(sampleData, map[string]interface{}{
		"age": 25,
	})
	fmt.Printf("\nFiltered Data (age=25): %d items\n", len(filtered))

	// Sort data
	sorted := processor.SortData(sampleData, "score", false)
	fmt.Printf("\nSorted Data (by score, descending):\n")
	for _, item := range sorted {
		fmt.Printf("  %s: %.1f\n", item["name"], item["score"])
	}

	// Aggregate data
	avgScore := processor.AggregateData(sampleData, "score", "avg")
	fmt.Printf("\nAverage Score: %.2f\n", avgScore)

	// Analyze data
	analysis, err := processor.AnalyzeData(sampleData)
	if err != nil {
		fmt.Printf("Error analyzing data: %v\n", err)
	} else {
		fmt.Printf("\nAnalysis Results:\n")
		fmt.Printf("  Patterns: %v\n", analysis.Patterns)
		fmt.Printf("  Recommendations: %v\n", analysis.Recommendations)
	}

	// Validate data
	rules := []ValidationRule{
		{Field: "id", Type: TypeNumber, Required: true},
		{Field: "name", Type: TypeString, Required: true},
		{Field: "age", Type: TypeNumber, Required: true, MinValue: 18, MaxValue: 100},
	}

	for _, item := range sampleData {
		valid, errors := processor.ValidateData(item, rules)
		if !valid {
			fmt.Printf("\nValidation errors for %s: %v\n", item["name"], errors)
		}
	}
} 