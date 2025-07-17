package peanut

import (
	"time"
	"testing"
	"os"
	"path/filepath"
	"time"
)

func TestParseTextConfig(t *testing.T) {
	config := New(true, true)
	content := `[server]
host: "localhost"
port: 8080
enabled: true

[database]
connections: 10
timeout: 30.5`

	result, err := config.ParseTextConfig(content)
	if err != nil {
		t.Fatalf("Failed to parse config: %v", err)
	}

	// Check server section
	server, ok := result["server"].(map[string]interface{})
	if !ok {
		t.Fatal("Server section not found or not a map")
	}

	if server["host"] != "localhost" {
		t.Errorf("Expected host=localhost, got %v", server["host"])
	}

	if server["port"] != int64(8080) {
		t.Errorf("Expected port=8080, got %v", server["port"])
	}

	if server["enabled"] != true {
		t.Errorf("Expected enabled=true, got %v", server["enabled"])
	}

	// Check database section
	database, ok := result["database"].(map[string]interface{})
	if !ok {
		t.Fatal("Database section not found or not a map")
	}

	if database["connections"] != int64(10) {
		t.Errorf("Expected connections=10, got %v", database["connections"])
	}

	if database["timeout"] != 30.5 {
		t.Errorf("Expected timeout=30.5, got %v", database["timeout"])
	}
}

func TestParseValueTypes(t *testing.T) {
	config := New(true, true)

	tests := []struct {
		input    string
		expected interface{}
	}{
		{`"string value"`, "string value"},
		{"'single quoted'", "single quoted"},
		{"true", true},
		{"false", false},
		{"null", nil},
		{"42", int64(42)},
		{"3.14", 3.14},
		{"one,two,three", []interface{}{"one", "two", "three"}},
		{"1,2,3", []interface{}{int64(1), int64(2), int64(3)}},
	}

	for _, test := range tests {
		result := config.parseValue(test.input)
		if !compareValues(result, test.expected) {
			t.Errorf("parseValue(%q) = %v (%T), expected %v (%T)", 
				test.input, result, result, test.expected, test.expected)
		}
	}
}

func TestBinaryRoundtrip(t *testing.T) {
	config := New(true, true)
	
	// Create a temporary directory
	tmpDir, err := os.MkdirTemp("", "peanut_test")
	if err != nil {
		t.Fatalf("Failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tmpDir)

	// Create test configuration
	testConfig := map[string]interface{}{
		"string_key": "test value",
		"number_key": int64(42),
		"bool_key":   true,
		"section": map[string]interface{}{
			"nested_key": "nested value",
			"nested_num": 3.14,
		},
	}

	// Write to binary file
	binaryPath := filepath.Join(tmpDir, "test.pnt")
	err = config.CompileToBinary(testConfig, binaryPath)
	if err != nil {
		t.Fatalf("Failed to compile to binary: %v", err)
	}

	// Read back from binary file
	loaded, err := config.LoadBinary(binaryPath)
	if err != nil {
		t.Fatalf("Failed to load binary: %v", err)
	}

	// Compare
	if !compareMaps(testConfig, loaded) {
		t.Errorf("Binary roundtrip failed:\nOriginal: %+v\nLoaded: %+v", testConfig, loaded)
	}

	// Check that shell file was also created
	shellPath := filepath.Join(tmpDir, "test.shell")
	if _, err := os.Stat(shellPath); os.IsNotExist(err) {
		t.Error("Shell file was not created")
	}
}

func TestConfigHierarchy(t *testing.T) {
	// Create a temporary directory structure
	tmpDir, err := os.MkdirTemp("", "peanut_hierarchy_test")
	if err != nil {
		t.Fatalf("Failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tmpDir)

	// Create nested directories
	subDir := filepath.Join(tmpDir, "project", "config")
	err = os.MkdirAll(subDir, 0755)
	if err != nil {
		t.Fatalf("Failed to create subdirs: %v", err)
	}

	// Create config files
	rootConfig := filepath.Join(tmpDir, "peanu.tsk")
	projectConfig := filepath.Join(tmpDir, "project", "peanu.peanuts")
	configConfig := filepath.Join(subDir, "peanu.pnt")

	// Write test files
	err = os.WriteFile(rootConfig, []byte("root: true\n"), 0644)
	if err != nil {
		t.Fatalf("Failed to write root config: %v", err)
	}

	err = os.WriteFile(projectConfig, []byte("[project]\nname: test\n"), 0644)
	if err != nil {
		t.Fatalf("Failed to write project config: %v", err)
	}

	// Create a dummy binary file (won't be valid, but that's ok for hierarchy test)
	err = os.WriteFile(configConfig, []byte("PNUT"), 0644)
	if err != nil {
		t.Fatalf("Failed to write config config: %v", err)
	}

	// Test hierarchy discovery
	config := New(true, true)
	hierarchy, err := config.FindConfigHierarchy(subDir)
	if err != nil {
		t.Fatalf("Failed to find hierarchy: %v", err)
	}

	// Should find all three files
	if len(hierarchy) != 3 {
		t.Errorf("Expected 3 config files, found %d", len(hierarchy))
	}

	// Check order (should be root -> project -> config)
	expectedPaths := []string{rootConfig, projectConfig, configConfig}
	for i, expected := range expectedPaths {
		if i >= len(hierarchy) {
			t.Errorf("Missing config file at index %d", i)
			continue
		}
		if hierarchy[i].Path != expected {
			t.Errorf("Expected path %s at index %d, got %s", expected, i, hierarchy[i].Path)
		}
	}
}

func TestDeepMerge(t *testing.T) {
	config := New(true, true)

	target := map[string]interface{}{
		"key1": "value1",
		"section1": map[string]interface{}{
			"nested1": "original",
			"nested2": "keep_this",
		},
	}

	source := map[string]interface{}{
		"key2": "value2",
		"section1": map[string]interface{}{
			"nested1": "overridden",
			"nested3": "new_value",
		},
		"section2": map[string]interface{}{
			"new_section": true,
		},
	}

	result := config.deepMerge(target, source)

	// Check basic merge
	if result["key1"] != "value1" {
		t.Error("key1 should be preserved")
	}
	if result["key2"] != "value2" {
		t.Error("key2 should be added")
	}

	// Check nested merge
	section1, ok := result["section1"].(map[string]interface{})
	if !ok {
		t.Fatal("section1 should be a map")
	}

	if section1["nested1"] != "overridden" {
		t.Error("nested1 should be overridden")
	}
	if section1["nested2"] != "keep_this" {
		t.Error("nested2 should be preserved")
	}
	if section1["nested3"] != "new_value" {
		t.Error("nested3 should be added")
	}

	// Check new section
	section2, ok := result["section2"].(map[string]interface{})
	if !ok {
		t.Fatal("section2 should be added")
	}
	if section2["new_section"] != true {
		t.Error("new_section should be true")
	}
}

func TestGetValue(t *testing.T) {
	// Create a temporary directory with config
	tmpDir, err := os.MkdirTemp("", "peanut_get_test")
	if err != nil {
		t.Fatalf("Failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tmpDir)

	configContent := `[server]
host: "localhost"
port: 8080

[database]
host: "db.example.com"
pool_size: 10`

	configPath := filepath.Join(tmpDir, "peanu.peanuts")
	err = os.WriteFile(configPath, []byte(configContent), 0644)
	if err != nil {
		t.Fatalf("Failed to write config: %v", err)
	}

	config := New(true, true)

	// Test getting values
	if val := config.Get("server.host", "default", tmpDir); val != "localhost" {
		t.Errorf("Expected server.host=localhost, got %v", val)
	}

	if val := config.Get("server.port", 0, tmpDir); val != int64(8080) {
		t.Errorf("Expected server.port=8080, got %v", val)
	}

	if val := config.Get("database.pool_size", 0, tmpDir); val != int64(10) {
		t.Errorf("Expected database.pool_size=10, got %v", val)
	}

	// Test default values
	if val := config.Get("nonexistent.key", "default_value", tmpDir); val != "default_value" {
		t.Errorf("Expected default value, got %v", val)
	}
}

func BenchmarkTextParsing(b *testing.B) {
	config := New(true, true)
	content := `[server]
host: "localhost"
port: 8080
workers: 4
debug: true

[database]
driver: "postgresql"
host: "db.example.com"
port: 5432
pool_size: 10

[cache]
enabled: true
ttl: 3600
backend: "redis"`

	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		_, _ = config.ParseTextConfig(content)
	}
}

func BenchmarkBinaryLoading(b *testing.B) {
	config := New(true, true)
	content := `[server]
host: "localhost"
port: 8080
workers: 4
debug: true

[database]
driver: "postgresql"
host: "db.example.com"
port: 5432
pool_size: 10

[cache]
enabled: true
ttl: 3600
backend: "redis"`

	// Prepare binary file
	tmpDir, _ := os.MkdirTemp("", "peanut_bench")
	defer os.RemoveAll(tmpDir)

	parsed, _ := config.ParseTextConfig(content)
	binaryPath := filepath.Join(tmpDir, "bench.pnt")
	_ = config.CompileToBinary(parsed, binaryPath)

	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		_, _ = config.LoadBinary(binaryPath)
	}
}

// Helper functions
func compareValues(a, b interface{}) bool {
	if a == nil && b == nil {
		return true
	}
	if a == nil || b == nil {
		return false
	}

	switch va := a.(type) {
	case []interface{}:
		vb, ok := b.([]interface{})
		if !ok || len(va) != len(vb) {
			return false
		}
		for i := range va {
			if !compareValues(va[i], vb[i]) {
				return false
			}
		}
		return true
	case map[string]interface{}:
		return compareMaps(va, b.(map[string]interface{}))
	default:
		return a == b
	}
}

func compareMaps(a, b map[string]interface{}) bool {
	if len(a) != len(b) {
		return false
	}
	for k, va := range a {
		vb, ok := b[k]
		if !ok {
			return false
		}
		if !compareValues(va, vb) {
			return false
		}
	}
	return true
}