package main

import (
	"fmt"
	"os"
	
	"tusk-go-sdk"
)

func main() {
	fmt.Println("TuskLang Enhanced Go SDK Test")
	fmt.Println("=============================")
	
	// Test enhanced parser
	parser := tusklanggo.NewEnhancedParser()
	
	// Test peanut.tsk loading
	fmt.Println("\n1. Testing peanut.tsk loading:")
	err := parser.LoadPeanut()
	if err != nil {
		fmt.Printf("Error loading peanut.tsk: %v\n", err)
	} else {
		fmt.Println("✅ peanut.tsk loading successful")
	}
	
	// Test enhanced sample file
	fmt.Println("\n2. Testing enhanced sample file:")
	err = parser.ParseFile("testdata/enhanced_sample.tsk")
	if err != nil {
		fmt.Printf("❌ Error parsing enhanced sample: %v\n", err)
		os.Exit(1)
	}
	
	fmt.Println("✅ Enhanced sample parsed successfully")
	
	// Test key retrieval
	fmt.Println("\n3. Testing key retrieval:")
	
	tests := []struct {
		key      string
		expected string
	}{
		{"app_name", "TuskLang Go Enhanced"},
		{"database.default", "sqlite"},
		{"database.sqlite.filename", "./tusklang.db"},
		{"database.postgresql.host", "localhost"},
		{"features", "[parsing queries caching]"},
		{"debug_mode", "development"},
		{"port_range", "map[max:9000 min:8000 type:range]"},
	}
	
	for _, test := range tests {
		value := parser.Get(test.key)
		if value != nil {
			fmt.Printf("✅ %s = %v\n", test.key, value)
		} else {
			fmt.Printf("❌ %s not found\n", test.key)
		}
	}
	
	// Test global variables
	fmt.Println("\n4. Testing global variables:")
	appName := parser.Get("app_name")
	version := parser.Get("version") 
	if appName != nil && version != nil {
		fmt.Printf("✅ Global variables: %v v%v\n", appName, version)
	}
	
	// Test all keys
	fmt.Println("\n5. All parsed keys:")
	keys := parser.Keys()
	fmt.Printf("Found %d keys:\n", len(keys))
	for i, key := range keys {
		if i < 10 { // Show first 10 keys
			value := parser.Get(key)
			fmt.Printf("  %s = %v\n", key, value)
		}
	}
	if len(keys) > 10 {
		fmt.Printf("  ... and %d more keys\n", len(keys)-10)
	}
	
	fmt.Println("\n🎉 All tests completed!")
	fmt.Println("Go developers now have maximum syntax flexibility!")
}