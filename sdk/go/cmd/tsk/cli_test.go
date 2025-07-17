package main

import (
	"os"
	"os/exec"
	"path/filepath"
	"testing"
)

func TestCLIBuild(t *testing.T) {
	// Test that the CLI builds successfully
	cmd := exec.Command("go", "build", "-o", "test-tsk", ".")
	cmd.Dir = "."
	
	if err := cmd.Run(); err != nil {
		t.Fatalf("Failed to build CLI: %v", err)
	}
	
	// Clean up
	defer os.Remove("test-tsk")
}

func TestCLIHelp(t *testing.T) {
	// Test help command
	cmd := exec.Command("go", "run", ".", "--help")
	cmd.Dir = "."
	
	output, err := cmd.Output()
	if err != nil {
		t.Fatalf("Failed to run help command: %v", err)
	}
	
	outputStr := string(output)
	if !contains(outputStr, "TuskLang CLI") {
		t.Errorf("Help output should contain 'TuskLang CLI', got: %s", outputStr)
	}
}

func TestCLIVersion(t *testing.T) {
	// Test version command
	cmd := exec.Command("go", "run", ".", "version")
	cmd.Dir = "."
	
	output, err := cmd.Output()
	if err != nil {
		t.Fatalf("Failed to run version command: %v", err)
	}
	
	outputStr := string(output)
	if !contains(outputStr, "TuskLang CLI v2.0.0") {
		t.Errorf("Version output should contain 'TuskLang CLI v2.0.0', got: %s", outputStr)
	}
}

func TestCLIParse(t *testing.T) {
	// Create a test TSK file
	testContent := `app_name: "Test App"
version: "1.0.0"
debug: true
port: 8080`
	
	testFile := "test-config.tsk"
	err := os.WriteFile(testFile, []byte(testContent), 0644)
	if err != nil {
		t.Fatalf("Failed to create test file: %v", err)
	}
	defer os.Remove(testFile)
	
	// Test parse command
	cmd := exec.Command("go", "run", ".", "parse", testFile)
	cmd.Dir = "."
	
	output, err := cmd.Output()
	if err != nil {
		t.Fatalf("Failed to run parse command: %v", err)
	}
	
	outputStr := string(output)
	if !contains(outputStr, "app_name") || !contains(outputStr, "Test App") {
		t.Errorf("Parse output should contain app_name and Test App, got: %s", outputStr)
	}
}

func TestCLIValidate(t *testing.T) {
	// Create a test TSK file
	testContent := `app_name: "Test App"
version: "1.0.0"`
	
	testFile := "test-validate.tsk"
	err := os.WriteFile(testFile, []byte(testContent), 0644)
	if err != nil {
		t.Fatalf("Failed to create test file: %v", err)
	}
	defer os.Remove(testFile)
	
	// Test validate command
	cmd := exec.Command("go", "run", ".", "validate", testFile)
	cmd.Dir = "."
	
	output, err := cmd.Output()
	if err != nil {
		t.Fatalf("Failed to run validate command: %v", err)
	}
	
	outputStr := string(output)
	if !contains(outputStr, "valid") {
		t.Errorf("Validate output should contain 'valid', got: %s", outputStr)
	}
}

func TestCLIConfigGet(t *testing.T) {
	// Create a test TSK file
	testContent := `app_name: "Test App"
database:
  host: "localhost"
  port: 5432`
	
	testFile := "test-config-get.tsk"
	err := os.WriteFile(testFile, []byte(testContent), 0644)
	if err != nil {
		t.Fatalf("Failed to create test file: %v", err)
	}
	defer os.Remove(testFile)
	
	// Test config get command
	cmd := exec.Command("go", "run", ".", "get", testFile, "app_name")
	cmd.Dir = "."
	
	output, err := cmd.Output()
	if err != nil {
		t.Fatalf("Failed to run get command: %v", err)
	}
	
	outputStr := string(output)
	if !contains(outputStr, "Test App") {
		t.Errorf("Get output should contain 'Test App', got: %s", outputStr)
	}
}

func TestCLIPeanutsCompile(t *testing.T) {
	// Create a test peanuts file
	testContent := `[server]
host: "localhost"
port: 8080`
	
	testFile := "test-peanuts.peanuts"
	err := os.WriteFile(testFile, []byte(testContent), 0644)
	if err != nil {
		t.Fatalf("Failed to create test file: %v", err)
	}
	defer os.Remove(testFile)
	defer os.Remove("test-peanuts.pnt")
	
	// Test peanuts compile command
	cmd := exec.Command("go", "run", ".", "peanuts", "compile", testFile)
	cmd.Dir = "."
	
	output, err := cmd.Output()
	if err != nil {
		t.Fatalf("Failed to run peanuts compile command: %v", err)
	}
	
	outputStr := string(output)
	if !contains(outputStr, "Compiled to") {
		t.Errorf("Peanuts compile output should contain 'Compiled to', got: %s", outputStr)
	}
	
	// Check that the binary file was created
	if _, err := os.Stat("test-peanuts.pnt"); os.IsNotExist(err) {
		t.Error("Binary file was not created")
	}
}

func TestCLIServicesStatus(t *testing.T) {
	// Test services status command
	cmd := exec.Command("go", "run", ".", "services", "status")
	cmd.Dir = "."
	
	output, err := cmd.Output()
	if err != nil {
		t.Fatalf("Failed to run services status command: %v", err)
	}
	
	outputStr := string(output)
	if !contains(outputStr, "database") || !contains(outputStr, "running") {
		t.Errorf("Services status output should contain service information, got: %s", outputStr)
	}
}

func TestCLICacheStatus(t *testing.T) {
	// Test cache status command
	cmd := exec.Command("go", "run", ".", "cache", "status")
	cmd.Dir = "."
	
	output, err := cmd.Output()
	if err != nil {
		t.Fatalf("Failed to run cache status command: %v", err)
	}
	
	outputStr := string(output)
	if !contains(outputStr, "memory_usage") || !contains(outputStr, "hit_rate") {
		t.Errorf("Cache status output should contain cache information, got: %s", outputStr)
	}
}

func TestCLILicenseCheck(t *testing.T) {
	// Test license check command
	cmd := exec.Command("go", "run", ".", "license", "check")
	cmd.Dir = "."
	
	output, err := cmd.Output()
	if err != nil {
		t.Fatalf("Failed to run license check command: %v", err)
	}
	
	outputStr := string(output)
	if !contains(outputStr, "valid") {
		t.Errorf("License check output should contain 'valid', got: %s", outputStr)
	}
}

func TestCLICSSMap(t *testing.T) {
	// Test CSS map command
	cmd := exec.Command("go", "run", ".", "css", "map")
	cmd.Dir = "."
	
	output, err := cmd.Output()
	if err != nil {
		t.Fatalf("Failed to run CSS map command: %v", err)
	}
	
	outputStr := string(output)
	if !contains(outputStr, "mh") || !contains(outputStr, "max-height") {
		t.Errorf("CSS map output should contain shortcode mappings, got: %s", outputStr)
	}
}

func TestCLIJSONOutput(t *testing.T) {
	// Create a test TSK file
	testContent := `app_name: "Test App"
version: "1.0.0"`
	
	testFile := "test-json.tsk"
	err := os.WriteFile(testFile, []byte(testContent), 0644)
	if err != nil {
		t.Fatalf("Failed to create test file: %v", err)
	}
	defer os.Remove(testFile)
	
	// Test parse command with JSON output
	cmd := exec.Command("go", "run", ".", "--json", "parse", testFile)
	cmd.Dir = "."
	
	output, err := cmd.Output()
	if err != nil {
		t.Fatalf("Failed to run parse command with JSON: %v", err)
	}
	
	outputStr := string(output)
	if !contains(outputStr, `"app_name"`) || !contains(outputStr, `"Test App"`) {
		t.Errorf("JSON output should contain JSON format, got: %s", outputStr)
	}
}

func TestCLIQuietMode(t *testing.T) {
	// Test quiet mode
	cmd := exec.Command("go", "run", ".", "--quiet", "version")
	cmd.Dir = "."
	
	output, err := cmd.Output()
	if err != nil {
		t.Fatalf("Failed to run version command in quiet mode: %v", err)
	}
	
	outputStr := string(output)
	if !contains(outputStr, "TuskLang CLI v2.0.0") {
		t.Errorf("Quiet mode should still show version, got: %s", outputStr)
	}
}

func TestCLIErrorHandling(t *testing.T) {
	// Test error handling for non-existent file
	cmd := exec.Command("go", "run", ".", "parse", "non-existent.tsk")
	cmd.Dir = "."
	
	_, err := cmd.Output()
	if err == nil {
		t.Error("Should return error for non-existent file")
	}
}

func TestCLIInvalidCommand(t *testing.T) {
	// Test invalid command
	cmd := exec.Command("go", "run", ".", "invalid-command")
	cmd.Dir = "."
	
	_, err := cmd.Output()
	if err == nil {
		t.Error("Should return error for invalid command")
	}
}

// Helper function to check if a string contains a substring
func contains(s, substr string) bool {
	return len(s) >= len(substr) && (s == substr || len(s) > len(substr) && 
		(s[:len(substr)] == substr || s[len(s)-len(substr):] == substr || 
		containsSubstring(s, substr)))
}

func containsSubstring(s, substr string) bool {
	for i := 0; i <= len(s)-len(substr); i++ {
		if s[i:i+len(substr)] == substr {
			return true
		}
	}
	return false
}

// Benchmark tests
func BenchmarkCLIParse(b *testing.B) {
	// Create a test TSK file
	testContent := `app_name: "Test App"
version: "1.0.0"
debug: true
port: 8080
database:
  host: "localhost"
  port: 5432
  name: "testdb"
features:
  - logging
  - metrics
  - caching`
	
	testFile := "benchmark-config.tsk"
	err := os.WriteFile(testFile, []byte(testContent), 0644)
	if err != nil {
		b.Fatalf("Failed to create test file: %v", err)
	}
	defer os.Remove(testFile)
	
	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		cmd := exec.Command("go", "run", ".", "parse", testFile)
		cmd.Dir = "."
		cmd.Output()
	}
}

func BenchmarkCLIValidate(b *testing.B) {
	// Create a test TSK file
	testContent := `app_name: "Test App"
version: "1.0.0"
debug: true`
	
	testFile := "benchmark-validate.tsk"
	err := os.WriteFile(testFile, []byte(testContent), 0644)
	if err != nil {
		b.Fatalf("Failed to create test file: %v", err)
	}
	defer os.Remove(testFile)
	
	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		cmd := exec.Command("go", "run", ".", "validate", testFile)
		cmd.Dir = "."
		cmd.Output()
	}
} 