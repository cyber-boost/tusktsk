package tusklanggo

import (
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"strings"
)

// RunCLI handles CLI commands for tusklang-go
func RunCLI() {
	if len(os.Args) < 2 {
		printHelp()
		return
	}
	cmd := os.Args[1]
	switch cmd {
	case "parse":
		if len(os.Args) < 3 {
			fmt.Println("Error: parse command requires a file path")
			printHelp()
			return
		}
		parseFile(os.Args[2])
	case "validate":
		if len(os.Args) < 3 {
			fmt.Println("Error: validate command requires a file path")
			printHelp()
			return
		}
		validateFile(os.Args[2])
	case "gen":
		if len(os.Args) < 4 || os.Args[2] != "--struct" {
			fmt.Println("Error: gen command requires --struct flag and file path")
			printHelp()
			return
		}
		generateStruct(os.Args[3])
	default:
		printHelp()
	}
}

func printHelp() {
	fmt.Println(`tusk-go - Native Go TuskLang CLI
Usage:
  tusk-go parse <file>      Parse and pretty-print .tsk as JSON
  tusk-go validate <file>   Validate .tsk syntax
  tusk-go gen --struct <file>  Generate Go struct from .tsk
`)
}

func parseFile(filePath string) {
	// Check file extension
	if !strings.HasSuffix(filePath, ".tsk") {
		fmt.Printf("Warning: File %s doesn't have .tsk extension\n", filePath)
	}
	
	// Open and read file
	file, err := os.Open(filePath)
	if err != nil {
		fmt.Printf("Error opening file: %v\n", err)
		os.Exit(1)
	}
	defer file.Close()
	
	// Parse the file
	parser := NewParser(file)
	result, err := parser.Parse()
	if err != nil {
		fmt.Printf("Error parsing file: %v\n", err)
		os.Exit(1)
	}
	
	// Output as pretty JSON
	jsonData, err := json.MarshalIndent(result, "", "  ")
	if err != nil {
		fmt.Printf("Error marshaling JSON: %v\n", err)
		os.Exit(1)
	}
	
	fmt.Println(string(jsonData))
}

func validateFile(filePath string) {
	// Check file extension
	if !strings.HasSuffix(filePath, ".tsk") {
		fmt.Printf("Warning: File %s doesn't have .tsk extension\n", filePath)
	}
	
	// Open and read file
	file, err := os.Open(filePath)
	if err != nil {
		fmt.Printf("Error opening file: %v\n", err)
		os.Exit(1)
	}
	defer file.Close()
	
	// Try to parse the file
	parser := NewParser(file)
	_, err = parser.Parse()
	if err != nil {
		fmt.Printf("❌ Validation failed: %v\n", err)
		os.Exit(1)
	}
	
	fmt.Printf("✅ File %s is valid TuskLang syntax\n", filePath)
}

func generateStruct(filePath string) {
	// Check file extension
	if !strings.HasSuffix(filePath, ".tsk") {
		fmt.Printf("Warning: File %s doesn't have .tsk extension\n", filePath)
	}
	
	// Open and read file
	file, err := os.Open(filePath)
	if err != nil {
		fmt.Printf("Error opening file: %v\n", err)
		os.Exit(1)
	}
	defer file.Close()
	
	// Parse the file
	parser := NewParser(file)
	result, err := parser.Parse()
	if err != nil {
		fmt.Printf("Error parsing file: %v\n", err)
		os.Exit(1)
	}
	
	// Generate Go struct
	structName := strings.Title(strings.TrimSuffix(filepath.Base(filePath), ".tsk"))
	structCode := generateGoStruct(structName, result)
	
	fmt.Println(structCode)
}

func generateGoStruct(structName string, data map[string]interface{}) string {
	var builder strings.Builder
	
	builder.WriteString(fmt.Sprintf("type %s struct {\n", structName))
	
	for key, value := range data {
		fieldName := toCamelCase(key)
		fieldType := getGoType(value)
		builder.WriteString(fmt.Sprintf("\t%s %s `tsk:\"%s\"`\n", fieldName, fieldType, key))
	}
	
	builder.WriteString("}\n")
	return builder.String()
}

func toCamelCase(s string) string {
	parts := strings.Split(s, "_")
	for i, part := range parts {
		if i == 0 {
			parts[i] = strings.Title(part)
		} else {
			parts[i] = strings.Title(part)
		}
	}
	return strings.Join(parts, "")
}

func getGoType(value interface{}) string {
	switch v := value.(type) {
	case string:
		return "string"
	case int:
		return "int"
	case float64:
		return "float64"
	case bool:
		return "bool"
	case []interface{}:
		if len(v) == 0 {
			return "[]interface{}"
		}
		return "[]" + getGoType(v[0])
	case map[string]interface{}:
		return "map[string]interface{}"
	default:
		return "interface{}"
	}
} 