// PeanutConfig CLI - TuskLang Hierarchical Configuration
// Part of TuskLang Go SDK
package main

import (
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"time"

	"tusk-go-sdk/peanut"
)

func main() {
	if len(os.Args) < 2 {
		showUsage()
		return
	}

	command := os.Args[1]
	config := peanut.New(true, true)

	switch command {
	case "compile":
		if len(os.Args) < 3 {
			fmt.Fprintf(os.Stderr, "Error: Please specify input file\n")
			os.Exit(1)
		}
		if err := compileFile(config, os.Args[2]); err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}

	case "load":
		directory := "."
		if len(os.Args) > 2 {
			directory = os.Args[2]
		}
		if err := loadConfig(config, directory); err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}

	case "benchmark":
		benchmark()

	case "hierarchy":
		directory := "."
		if len(os.Args) > 2 {
			directory = os.Args[2]
		}
		if err := showHierarchy(config, directory); err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}

	default:
		fmt.Fprintf(os.Stderr, "Unknown command: %s\n", command)
		showUsage()
		os.Exit(1)
	}
}

func showUsage() {
	fmt.Println("🥜 PeanutConfig - TuskLang Hierarchical Configuration\n")
	fmt.Println("Commands:")
	fmt.Println("  compile <file>    Compile .peanuts or .tsk to binary .pnt")
	fmt.Println("  load [dir]        Load configuration hierarchy")
	fmt.Println("  hierarchy [dir]   Show config file hierarchy")
	fmt.Println("  benchmark         Run performance benchmark")
	fmt.Println("\nExample:")
	fmt.Println("  peanut compile config.peanuts")
	fmt.Println("  peanut load /path/to/project")
}

func compileFile(config *peanut.Config, inputFile string) error {
	content, err := os.ReadFile(inputFile)
	if err != nil {
		return fmt.Errorf("failed to read file: %v", err)
	}

	parsed, err := config.ParseTextConfig(string(content))
	if err != nil {
		return fmt.Errorf("failed to parse config: %v", err)
	}

	// Generate output filename
	outputFile := inputFile
	if filepath.Ext(inputFile) == ".peanuts" {
		outputFile = inputFile[:len(inputFile)-8] + ".pnt"
	} else if filepath.Ext(inputFile) == ".tsk" {
		outputFile = inputFile[:len(inputFile)-4] + ".pnt"
	} else {
		outputFile = inputFile + ".pnt"
	}

	if err := config.CompileToBinary(parsed, outputFile); err != nil {
		return fmt.Errorf("failed to compile to binary: %v", err)
	}

	fmt.Printf("✅ Compiled to %s\n", outputFile)
	return nil
}

func loadConfig(config *peanut.Config, directory string) error {
	loaded, err := config.Load(directory)
	if err != nil {
		return fmt.Errorf("failed to load config: %v", err)
	}

	data, err := json.MarshalIndent(loaded, "", "  ")
	if err != nil {
		return fmt.Errorf("failed to marshal JSON: %v", err)
	}

	fmt.Println(string(data))
	return nil
}

func showHierarchy(config *peanut.Config, directory string) error {
	hierarchy, err := config.FindConfigHierarchy(directory)
	if err != nil {
		return fmt.Errorf("failed to find hierarchy: %v", err)
	}

	fmt.Printf("Configuration hierarchy for %s:\n\n", directory)
	for i, configFile := range hierarchy {
		fmt.Printf("%d. %s (%s)\n", i+1, configFile.Path, configFile.Type)
		fmt.Printf("   Modified: %s\n", configFile.MTime.Format(time.RFC3339))
		fmt.Println()
	}

	if len(hierarchy) == 0 {
		fmt.Println("No configuration files found")
	}

	return nil
}

func benchmark() {
	config := peanut.New(true, true)
	testContent := `[server]
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

	fmt.Println("🥜 Peanut Configuration Performance Test\n")

	// Test text parsing
	start := time.Now()
	for i := 0; i < 1000; i++ {
		_, _ = config.ParseTextConfig(testContent)
	}
	textTime := time.Since(start)
	fmt.Printf("Text parsing (1000 iterations): %v\n", textTime)

	// Test binary serialization/deserialization
	parsed, _ := config.ParseTextConfig(testContent)
	
	// Test compilation performance
	start = time.Now()
	for i := 0; i < 1000; i++ {
		tempFile := fmt.Sprintf("/tmp/test_%d.pnt", i)
		_ = config.CompileToBinary(parsed, tempFile)
		os.Remove(tempFile)
	}
	compileTime := time.Since(start)
	fmt.Printf("Binary compilation (1000 iterations): %v\n", compileTime)

	// Test binary loading performance
	tempFile := "/tmp/benchmark.pnt"
	_ = config.CompileToBinary(parsed, tempFile)
	defer os.Remove(tempFile)

	start = time.Now()
	for i := 0; i < 1000; i++ {
		_, _ = config.LoadBinary(tempFile)
	}
	binaryTime := time.Since(start)
	fmt.Printf("Binary loading (1000 iterations): %v\n", binaryTime)

	improvement := float64(textTime-binaryTime) / float64(textTime) * 100
	fmt.Printf("\n✨ Binary format is %.0f%% faster than text parsing!\n", improvement)
}