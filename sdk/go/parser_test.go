package tusklanggo

import (
	"strings"
	"testing"
)

func TestParserBasic(t *testing.T) {
	input := `app_name: "Test App"
version: "1.0.0"
debug: true
port: 8080`

	parser := NewParser(strings.NewReader(input))
	result, err := parser.Parse()
	
	if err != nil {
		t.Fatalf("Parse failed: %v", err)
	}
	
	expected := map[string]interface{}{
		"app_name": "Test App",
		"version":  "1.0.0",
		"debug":    true,
		"port":     8080,
	}
	
	for key, expectedValue := range expected {
		if result[key] != expectedValue {
			t.Errorf("Expected %s = %v, got %v", key, expectedValue, result[key])
		}
	}
}

func TestParserNested(t *testing.T) {
	input := `database:
  host: "localhost"
  port: 5432
  name: "testdb"`

	parser := NewParser(strings.NewReader(input))
	result, err := parser.Parse()
	
	if err != nil {
		t.Fatalf("Parse failed: %v", err)
	}
	
	database, ok := result["database"].(map[string]interface{})
	if !ok {
		t.Fatal("Expected database to be a map")
	}
	
	expected := map[string]interface{}{
		"host": "localhost",
		"port": 5432,
		"name": "testdb",
	}
	
	for key, expectedValue := range expected {
		if database[key] != expectedValue {
			t.Errorf("Expected database.%s = %v, got %v", key, expectedValue, database[key])
		}
	}
}

func TestParserArray(t *testing.T) {
	input := `features:
  - logging
  - metrics
  - caching`

	parser := NewParser(strings.NewReader(input))
	result, err := parser.Parse()
	
	if err != nil {
		t.Fatalf("Parse failed: %v", err)
	}
	
	features, ok := result["features"].([]interface{})
	if !ok {
		t.Fatal("Expected features to be a slice")
	}
	
	expected := []string{"logging", "metrics", "caching"}
	
	if len(features) != len(expected) {
		t.Errorf("Expected %d features, got %d", len(expected), len(features))
	}
	
	for i, expectedFeature := range expected {
		if features[i] != expectedFeature {
			t.Errorf("Expected features[%d] = %s, got %v", i, expectedFeature, features[i])
		}
	}
}

func TestParserComments(t *testing.T) {
	input := `# This is a comment
app_name: "Test App"
# Another comment
version: "1.0.0"`

	parser := NewParser(strings.NewReader(input))
	result, err := parser.Parse()
	
	if err != nil {
		t.Fatalf("Parse failed: %v", err)
	}
	
	expected := map[string]interface{}{
		"app_name": "Test App",
		"version":  "1.0.0",
	}
	
	for key, expectedValue := range expected {
		if result[key] != expectedValue {
			t.Errorf("Expected %s = %v, got %v", key, expectedValue, result[key])
		}
	}
}

func TestParserEmptyLines(t *testing.T) {
	input := `app_name: "Test App"

version: "1.0.0"

debug: true`

	parser := NewParser(strings.NewReader(input))
	result, err := parser.Parse()
	
	if err != nil {
		t.Fatalf("Parse failed: %v", err)
	}
	
	expected := map[string]interface{}{
		"app_name": "Test App",
		"version":  "1.0.0",
		"debug":    true,
	}
	
	for key, expectedValue := range expected {
		if result[key] != expectedValue {
			t.Errorf("Expected %s = %v, got %v", key, expectedValue, result[key])
		}
	}
}

func TestParserVariableInterpolation(t *testing.T) {
	input := `base_url: "https://api.example.com"
endpoint: "$base_url/v1/users"`

	parser := NewParser(strings.NewReader(input))
	parser.SetVariable("base_url", "https://api.example.com")
	result, err := parser.Parse()
	
	if err != nil {
		t.Fatalf("Parse failed: %v", err)
	}
	
	expected := "https://api.example.com/v1/users"
	if result["endpoint"] != expected {
		t.Errorf("Expected endpoint = %s, got %v", expected, result["endpoint"])
	}
}

func TestParserComplexNested(t *testing.T) {
	input := `api:
  version: "v1"
  cors:
    allowed_origins:
      - "https://example.com"
      - "https://app.example.com"
    allowed_methods:
      - "GET"
      - "POST"`

	parser := NewParser(strings.NewReader(input))
	result, err := parser.Parse()
	
	if err != nil {
		t.Fatalf("Parse failed: %v", err)
	}
	
	api, ok := result["api"].(map[string]interface{})
	if !ok {
		t.Fatal("Expected api to be a map")
	}
	
	if api["version"] != "v1" {
		t.Errorf("Expected api.version = v1, got %v", api["version"])
	}
	
	cors, ok := api["cors"].(map[string]interface{})
	if !ok {
		t.Fatal("Expected api.cors to be a map")
	}
	
	origins, ok := cors["allowed_origins"].([]interface{})
	if !ok {
		t.Fatal("Expected allowed_origins to be a slice")
	}
	
	if len(origins) != 2 {
		t.Errorf("Expected 2 origins, got %d", len(origins))
	}
} 